"""One-time public-key delivery to CI; the APK password never leaves Actions.

CMS AES-256-GCM and RSA-OAEP-SHA256 protect local input packages. CI converts
verified sources to the existing immutable, password-encrypted delivery format.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import tarfile
import tempfile
from urllib.parse import urlparse
from urllib.request import urlopen

import assets


def run(args):
    subprocess.run(args, check=True, stdout=subprocess.DEVNULL, stderr=subprocess.PIPE)


def write_json(path, data):
    Path(path).write_text(json.dumps(data, ensure_ascii=False, indent=2)+'\n')


def encrypt_cms(source, output, cert):
    run(['openssl', 'cms', '-encrypt', '-binary', '-aes-256-gcm', '-stream',
         '-in', str(source), '-out', str(output), '-outform', 'DER',
         '-recip', str(cert), '-keyopt', 'rsa_padding_mode:oaep',
         '-keyopt', 'rsa_oaep_md:sha256'])


def decrypt_cms(source, output, cert, key):
    try:
        run(['openssl', 'cms', '-decrypt', '-binary', '-in', str(source),
             '-inform', 'DER', '-inkey', str(key), '-recip', str(cert),
             '-out', str(output)])
    except Exception:
        Path(output).unlink(missing_ok=True)
        raise


def prepare_key(output):
    if not os.environ.get('KLUCZ'):
        raise ValueError('Missing CI encryption secret')
    output.mkdir(parents=True, exist_ok=False)
    tag = 'oboz-transfer-key-'+os.environ['GITHUB_RUN_ID']
    repo = os.environ['GITHUB_REPOSITORY']
    with tempfile.TemporaryDirectory(prefix='oboz-transfer-key-') as tmp:
        folder = Path(tmp)
        key, cert, wrapped = folder/'recipient.pem', output/'recipient.crt', folder/'recipient.pem.enc'
        run(['openssl', 'req', '-x509', '-newkey', 'rsa:3072', '-nodes',
             '-keyout', str(key), '-out', str(cert), '-days', '30',
             '-subj', '/CN=Braxhorn camp input transfer'])
        key.chmod(0o600)
        run(assets.OPENSSL+['-salt', '-in', str(key), '-out', str(wrapped), '-pass', 'env:KLUCZ'])
        receipt = dict(schema=1, releaseTag=tag, certificateSha256=assets.digest(cert),
                       wrappedKeySha256=assets.digest(wrapped), runId=os.environ['GITHUB_RUN_ID'])
        write_json(output/'recipient.json', receipt)
        run(['gh', 'release', 'create', tag, '--repo', repo, '--draft',
             '--target', os.environ['GITHUB_SHA'], '--title', 'Obóz — klucz odbiorcy zasobów',
             '--notes', 'Wyłącznie publiczny certyfikat i zaszyfrowany klucz transportowy. Hasło podpisu pozostaje w Actions.'])
        run(['gh', 'release', 'upload', tag, '--repo', repo, str(cert), str(wrapped), str(output/'recipient.json')])
    print('CAMP_TRANSFER_RECIPIENT_READY '+tag)


def pack(source, cert, receipt, output):
    data = json.loads(assets.MANIFEST.read_text())
    assets.verify_sources(data, source)
    if assets.digest(cert) != receipt['certificateSha256']:
        raise ValueError('Recipient certificate does not match CI receipt')
    output.mkdir(parents=True, exist_ok=False)
    groups, group, size = [], [], 0
    for entry in data['files']:
        if entry['bytes'] > 180*1024*1024:
            raise ValueError('Input exceeds transport part limit')
        if group and size+entry['bytes'] > 180*1024*1024:
            groups.append(group); group, size = [], 0
        group.append(entry); size += entry['bytes']
    if group:
        groups.append(group)
    records = []
    with tempfile.TemporaryDirectory(prefix='oboz-transfer-pack-') as tmp:
        plain = Path(tmp)/'source.tar.gz'
        for number, group in enumerate(groups):
            with tarfile.open(plain, 'w:gz', compresslevel=6) as archive:
                for item in group:
                    path = assets.safe(source, item['path'])
                    info = archive.gettarinfo(str(path), arcname=item['path'])
                    info.uid = info.gid = 0; info.uname = info.gname = ''; info.mode = 0o644
                    with path.open('rb') as stream:
                        archive.addfile(info, stream)
            target = output/f'camp-transfer-{number:03d}.cms.enc'
            encrypt_cms(plain, target, cert)
            records.append(dict(name=target.name, bytes=target.stat().st_size, sha256=assets.digest(target)))
            plain.unlink()
            print(f'CAMP_TRANSFER_ENCRYPTED part={number} bytes={target.stat().st_size}', flush=True)
    request = dict(schema=1, mode='import', recipient=receipt,
                   privateInputsSha256=assets.digest(assets.MANIFEST), packages=records)
    write_json(output/'transfer-request.json', request)


def validate_request(request):
    if request.get('schema') != 1 or request.get('mode') != 'import':
        raise ValueError('Invalid transfer request')
    if request.get('privateInputsSha256') != assets.digest(assets.MANIFEST):
        raise ValueError('Transfer inventory does not match the checkout')
    receipt = request['recipient']
    if not re.fullmatch(r'oboz-transfer-key-[0-9]+', receipt['releaseTag']):
        raise ValueError('Invalid recipient release')
    for field in ('certificateSha256', 'wrappedKeySha256'):
        if not re.fullmatch(r'[0-9a-f]{64}', receipt[field]):
            raise ValueError('Invalid recipient receipt')
    if not request.get('packages') or len(request['packages']) > 20:
        raise ValueError('Invalid transfer part count')
    seen = set()
    for p in request['packages']:
        if not re.fullmatch(r'camp-transfer-[0-9]{3}\.cms\.enc', p['name']) or p['name'] in seen:
            raise ValueError('Invalid transfer filename')
        seen.add(p['name'])
        if type(p['bytes']) is not int or not 1 <= p['bytes'] <= 200*1024*1024:
            raise ValueError('Invalid transfer size')
        if not re.fullmatch(r'[0-9a-f]{64}', p['sha256']):
            raise ValueError('Invalid transfer checksum')
        u = urlparse(p['url'])
        host = u.hostname or ''
        if u.scheme != 'https' or u.username or u.password or u.port not in (None,443) or not (
            host.endswith('.blob.core.windows.net') or host.endswith('.oaiusercontent.com')
            or host.endswith('.amazonaws.com')):
            raise ValueError('Unexpected encrypted transfer host')


def download_part(part, target):
    # URLs refer only to ciphertext. Do not print temporary bearer URLs in logs.
    h, size = hashlib.sha256(), 0
    with urlopen(part['url'], timeout=120) as source, target.open('xb') as output:
        if urlparse(source.url).scheme != 'https':
            raise ValueError('Insecure transfer redirect')
        while chunk := source.read(1024*1024):
            size += len(chunk)
            if size > part['bytes']:
                raise ValueError('Transfer exceeds declared size')
            h.update(chunk); output.write(chunk)
    if size != part['bytes'] or h.hexdigest() != part['sha256']:
        raise ValueError('Incomplete or corrupt encrypted transfer')


def accept(request, output):
    validate_request(request)
    if not os.environ.get('KLUCZ'):
        raise ValueError('Missing CI encryption secret')
    data = json.loads(assets.MANIFEST.read_text())
    assets.validate_inventory(data)
    repo, receipt = os.environ['GITHUB_REPOSITORY'], request['recipient']
    output.mkdir(parents=True, exist_ok=False)
    with tempfile.TemporaryDirectory(prefix='oboz-transfer-accept-') as tmp:
        folder = Path(tmp)
        for name in ('recipient.crt', 'recipient.pem.enc'):
            run(['gh', 'release', 'download', receipt['releaseTag'], '--repo', repo,
                 '--pattern', name, '--dir', str(folder)])
        cert, wrapped, key = folder/'recipient.crt', folder/'recipient.pem.enc', folder/'recipient.pem'
        if assets.digest(cert) != receipt['certificateSha256'] or assets.digest(wrapped) != receipt['wrappedKeySha256']:
            raise ValueError('Recipient changed after local encryption')
        run(assets.OPENSSL+['-d', '-in', str(wrapped), '-out', str(key), '-pass', 'env:KLUCZ'])
        key.chmod(0o600)
        stage = folder/'sources'; stage.mkdir()
        expected, seen = {p['path']: p for p in data['files']}, set()
        for part in request['packages']:
            cipher, plain = folder/part['name'], folder/'sources.tar.gz'
            download_part(part, cipher)
            decrypt_cms(cipher, plain, cert, key)
            with tarfile.open(plain, 'r:gz') as archive:
                assets.extract_verified(archive, stage, expected, seen)
            cipher.unlink(); plain.unlink()
        if seen != set(expected):
            raise ValueError('Incomplete transfer; nothing is installed')
        assets.verify_sources(data, stage)
        assets.encrypt(data, stage)
        tag = data['releaseTag']
        run(['gh', 'release', 'create', tag, '--repo', repo, '--draft',
             '--target', os.environ['GITHUB_SHA'], '--title', 'Obóz — zweryfikowane prywatne źródła',
             '--notes', '204 źródła; zaszyfrowane w CI dotychczasowym sekretem. Nie jest to APK.'])
        check = folder/'download-check'; check.mkdir()
        for part in data['packages']:
            path = assets.ROOT/'unity-city/Prywatne'/part['name']
            run(['gh', 'release', 'upload', tag, '--repo', repo, str(path)])
            run(['gh', 'release', 'download', tag, '--repo', repo, '--pattern', part['name'], '--dir', str(check)])
            if assets.digest(check/part['name']) != part['sha256']:
                raise ValueError('Published encrypted package failed readback')
        write_json(output/'private-inputs.json', data)
        write_json(output/'delivery-proof.json', dict(schema=1, verifiedFiles=len(expected),
            sourceBytes=sum(p['bytes'] for p in data['files']), releaseTag=tag,
            encryptedBytes=sum(p['bytes'] for p in data['packages']), readbackVerified=True))
    print('CAMP_TRANSFER_DELIVERY_VERIFIED files='+str(len(data['files'])))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('command', choices=['prepare-key', 'pack', 'accept'])
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--source', type=Path)
    parser.add_argument('--certificate', type=Path)
    parser.add_argument('--receipt', type=Path)
    parser.add_argument('--request', type=Path)
    args = parser.parse_args()
    try:
        if args.command == 'prepare-key': prepare_key(args.output)
        elif args.command == 'pack': pack(args.source, args.certificate, json.loads(args.receipt.read_text()), args.output)
        else: accept(json.loads(args.request.read_text()), args.output)
    except Exception as exc:
        # Requests/subprocesses can carry temporary URLs; report only the type.
        print('CAMP_TRANSFER_FAILED '+type(exc).__name__, flush=True)
        raise SystemExit(1)
