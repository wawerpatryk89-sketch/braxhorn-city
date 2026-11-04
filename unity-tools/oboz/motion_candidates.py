"""Restore encrypted Monika motion sources; this command does not run Unity.

Uses the existing camp recipient and source verifier. No private key or raw clip
is published. Owner Walk/Run/Jump replace only those selections; the original
casual package remains available for Idle and comparison. Source integrity is
not a claim that the owner's new clips have passed a Unity or phone review.
"""
import json
import os
from pathlib import Path
import re
import subprocess
import tarfile
import tempfile

import assets

PACKAGES = Path(__file__).with_name('motion-packages')


def run(args):
    subprocess.run(args, check=True, stdout=subprocess.DEVNULL, stderr=subprocess.PIPE)


def validate(data):
    if data.get('schema') != 1 or not re.fullmatch(r'oboz-transfer-key-[0-9]+', data['releaseTag']):
        raise ValueError('Invalid candidate recipient')
    for key in ('certificateSha256', 'wrappedKeySha256'):
        if not re.fullmatch(r'[0-9a-f]{64}', data[key]):
            raise ValueError('Invalid recipient checksum')
    package = data['package']
    if not re.fullmatch(r'monika-casual-motion-[0-9]{8}\.cms\.enc', package['name']):
        raise ValueError('Invalid candidate filename')
    if not 1 <= package['bytes'] <= 10*1024*1024 or not re.fullmatch(r'[0-9a-f]{64}', package['sha256']):
        raise ValueError('Invalid candidate receipt')
    expected = {'animations/casual/'+name+'.anim' for name in ('Idle', 'Walk', 'Run')}
    if len(data['files']) != 3 or {f['path'] for f in data['files']} != expected:
        raise ValueError('Candidate delivery must contain exactly Idle, Walk and Run')
    assets.validate_inventory(dict(schema=1, zone='oboz', files=data['files']))


def extract(plain, stage, records):
    expected = {r['path']: r for r in records}
    seen = set()
    with tarfile.open(plain, 'r:gz') as archive:
        assets.extract_verified(archive, stage, expected, seen)
    if seen != set(expected):
        raise ValueError('Incomplete candidate delivery')
    assets.verify_sources(dict(schema=1, zone='oboz', files=records), stage)


def validate_owner(data, base):
    if data.get('schema') != 1:
        raise ValueError('Invalid owner motion schema')
    for key in ('releaseTag', 'certificateSha256', 'wrappedKeySha256'):
        if data.get(key) != base[key]:
            raise ValueError('Owner motion must use the existing camp recipient')
    package = data['package']
    if not re.fullmatch(r'monika-owner-motion-[0-9]{8}\.cms\.enc', package['name']):
        raise ValueError('Invalid owner motion filename')
    if (type(package['bytes']) is not int or not 1 <= package['bytes'] <= 10*1024*1024
            or not re.fullmatch(r'[0-9a-f]{64}', package['sha256'])):
        raise ValueError('Invalid owner motion receipt')
    expected = {'animations/monika-owner/'+name for name in
                ('Brutal To Happy Walking.fbx', 'Running.fbx', 'Jump.fbx')}
    if len(data['files']) != 3 or {f['path'] for f in data['files']} != expected:
        raise ValueError('Owner delivery must contain only Walk, Run and Jump')
    assets.validate_inventory(dict(schema=1, zone='oboz', files=data['files']))


def verify_package(path, receipt):
    if (path.is_symlink() or not path.is_file() or path.stat().st_size != receipt['bytes']
            or assets.digest(path) != receipt['sha256']):
        raise ValueError('Missing/damaged encrypted motion package: '+receipt['name'])


def restore(data, owner):
    validate(data)
    validate_owner(owner, data)
    owner_cipher = PACKAGES/owner['package']['name']
    verify_package(owner_cipher, owner['package'])
    if not os.environ.get('KLUCZ'):
        raise ValueError('Missing CI encryption binding')
    repo = os.environ['GITHUB_REPOSITORY']
    with tempfile.TemporaryDirectory(prefix='monika-motion-') as tmp:
        folder = Path(tmp)
        for name in ('recipient.crt', 'recipient.pem.enc', data['package']['name']):
            run(['gh', 'release', 'download', data['releaseTag'], '--repo', repo,
                 '--pattern', name, '--dir', str(folder)])
        cert, wrapped, key = folder/'recipient.crt', folder/'recipient.pem.enc', folder/'recipient.pem'
        if assets.digest(cert) != data['certificateSha256'] or assets.digest(wrapped) != data['wrappedKeySha256']:
            raise ValueError('Recipient changed')
        key.touch(mode=0o600)
        run(assets.OPENSSL+['-d', '-in', str(wrapped), '-out', str(key), '-pass', 'env:KLUCZ'])
        stage = folder/'sources'; stage.mkdir()
        # Verify BOTH deliveries before replacing any existing source. There is
        # no silent fallback to the old movement if the owner's delivery fails.
        for index, (receipt, cipher) in enumerate(((data, folder/data['package']['name']), (owner, owner_cipher))):
            verify_package(cipher, receipt['package'])
            plain = folder/('sources-'+str(index)+'.tar.gz')
            run(['openssl', 'cms', '-decrypt', '-binary', '-in', str(cipher), '-inform', 'DER',
                 '-inkey', str(key), '-recip', str(cert), '-out', str(plain)])
            extract(plain, stage, receipt['files'])
        for record in data['files'] + owner['files']:
            target = assets.safe(assets.PRIVATE, record['path'])
            target.parent.mkdir(parents=True, exist_ok=True)
            os.replace(assets.safe(stage, record['path']), target)
    print('MONIKA_MOTION_SOURCE integrity_verified=6 character=monika owner_clips=Walk,Run,Jump owner_unity_tested=false')


if __name__ == '__main__':
    data=json.loads((Path(__file__).with_name('motion-candidates.json')).read_text())
    owner=json.loads((Path(__file__).with_name('monika-owner-motion.json')).read_text())
    restore(data, owner)

