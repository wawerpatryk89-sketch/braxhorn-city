"""Restore three encrypted animation candidates for Monika's diagnostic only.

Uses the existing camp recipient and source verifier. No private key or raw clip
is published, and this command does not change the production delivery manifest.
"""
import json
import os
from pathlib import Path
import re
import subprocess
import tarfile
import tempfile

import assets


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


def restore(data):
    validate(data)
    if not os.environ.get('KLUCZ'):
        raise ValueError('Missing CI encryption binding')
    repo = os.environ['GITHUB_REPOSITORY']
    with tempfile.TemporaryDirectory(prefix='monika-motion-') as tmp:
        folder = Path(tmp)
        for name in ('recipient.crt', 'recipient.pem.enc', data['package']['name']):
            run(['gh', 'release', 'download', data['releaseTag'], '--repo', repo,
                 '--pattern', name, '--dir', str(folder)])
        cert, wrapped, key = folder/'recipient.crt', folder/'recipient.pem.enc', folder/'recipient.pem'
        cipher, plain = folder/data['package']['name'], folder/'sources.tar.gz'
        if assets.digest(cert) != data['certificateSha256'] or assets.digest(wrapped) != data['wrappedKeySha256']:
            raise ValueError('Recipient changed')
        if cipher.stat().st_size != data['package']['bytes'] or assets.digest(cipher) != data['package']['sha256']:
            raise ValueError('Damaged encrypted candidates')
        key.touch(mode=0o600)
        run(assets.OPENSSL+['-d', '-in', str(wrapped), '-out', str(key), '-pass', 'env:KLUCZ'])
        run(['openssl', 'cms', '-decrypt', '-binary', '-in', str(cipher), '-inform', 'DER',
             '-inkey', str(key), '-recip', str(cert), '-out', str(plain)])
        stage = folder/'sources'; stage.mkdir()
        extract(plain, stage, data['files'])
        for record in data['files']:
            target = assets.safe(assets.PRIVATE, record['path'])
            target.parent.mkdir(parents=True, exist_ok=True)
            os.replace(assets.safe(stage, record['path']), target)
    print('MONIKA_MOTION_CANDIDATES verified=3 production_selected=false')


if __name__ == '__main__':
    data=json.loads((Path(__file__).with_name('motion-candidates.json')).read_text())
    restore(data)
