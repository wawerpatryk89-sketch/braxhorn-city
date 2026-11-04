"""Import the owner's club models using the existing CI envelope key.

Plain models remain under ignored Assets/Source/Prywatne. No new CI secret.
"""
import hashlib
import io
import json
import os
from pathlib import Path
import struct
import subprocess
import tempfile
import zipfile

ROOT = Path(__file__).resolve().parents[2]
TRANSFER = ROOT / 'unity-city/Prywatne/import-club-20261104'
ENCRYPTED_KEY = ROOT / 'unity-city/Prywatne/import-patryk-20261103/import-private.pem.enc'
OPENSSL = ['openssl', 'enc', '-aes-256-cbc', '-pbkdf2', '-iter', '200000', '-md', 'sha256']
NAMES = ('raised-seating-booth.glb', 'polyhaven-sofa-02.glb', 'bar-table-booths.glb')


def unpack_verified(raw, manifest):
    if len(raw) != manifest['zip_bytes'] or hashlib.sha256(raw).hexdigest() != manifest['zip_sha256']:
        raise ValueError('Club archive integrity failure')
    with zipfile.ZipFile(io.BytesIO(raw)) as archive:
        if sorted(archive.namelist()) != sorted(NAMES):
            raise ValueError('Unexpected club model names')
        result = {}
        for name in NAMES:
            data = archive.read(name)
            if len(data) < 20 or data[:4] != b'glTF' or struct.unpack_from('<II', data, 4) != (2, len(data)):
                raise ValueError('Invalid GLB: ' + name)
            if hashlib.sha256(data).hexdigest() != manifest['files'][name]:
                raise ValueError('Club model integrity failure: ' + name)
            result[name] = data
    return result


def install(destination, transfer=TRANSFER, encrypted_key=ENCRYPTED_KEY):
    if not os.environ.get('KLUCZ', '').strip():
        raise RuntimeError('Missing existing CI key for club assets')
    transfer = Path(transfer)
    manifest = json.loads((transfer / 'manifest.json').read_text())
    encrypted = (transfer / 'models.zip.enc').read_bytes()
    if hashlib.sha256(encrypted).hexdigest() != manifest['encrypted_sha256']:
        raise ValueError('Club encrypted archive integrity failure')
    with tempfile.TemporaryDirectory(prefix='brax-club-import-') as directory:
        key = Path(directory) / 'key.pem'
        subprocess.run(OPENSSL + ['-d', '-in', str(encrypted_key), '-out', str(key), '-pass', 'env:KLUCZ'],
                       check=True, capture_output=True)
        key.chmod(0o600)
        password = subprocess.run(['openssl', 'pkeyutl', '-decrypt', '-inkey', str(key),
            '-in', str(transfer / 'package-key.rsa'), '-pkeyopt', 'rsa_padding_mode:oaep',
            '-pkeyopt', 'rsa_oaep_md:sha256'], check=True, capture_output=True).stdout.decode('ascii')
        env = dict(os.environ, BRAX_CLUB_PASSWORD=password)
        raw = subprocess.run(OPENSSL + ['-d', '-pass', 'env:BRAX_CLUB_PASSWORD'],
                             input=encrypted, env=env, check=True, capture_output=True).stdout
    files = unpack_verified(raw, manifest)
    destination = Path(destination)
    destination.mkdir(parents=True, exist_ok=True)
    for name, data in files.items():
        target = destination / name
        with tempfile.NamedTemporaryFile(dir=destination, delete=False) as stream:
            tmp = Path(stream.name)
            stream.write(data)
        tmp.replace(target)  # preserve existing .meta files
    print('BRAXHORN_KLUB_IMPORT verified original models: 3')
    return list(files)


if __name__ == '__main__':
    install(ROOT / 'unity-city/Assets/Source/Prywatne/Klub20261104')
