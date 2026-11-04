"""Owner's six clips, shared by Patryk generators; never overwrite other rigs."""
import hashlib
import io
import json
import os
from pathlib import Path
import subprocess
import tempfile
import zipfile

ROOT = Path(__file__).resolve().parents[2]
TRANSFER = ROOT / 'unity-city/Prywatne/import-patryk-animations-20261104'
ZIP_SHA256 = '24878e281548000edb2bbf51bc8bbf5cccedf0eb9a26ad76b168dd9537c78556'
NAMES = ('Entering Car.fbx', 'Goalkeeper Diving Save.fbx', 'Goalkeeper Catch.fbx',
         'Jump.fbx', 'Strut Walking.fbx', 'Fast Run.fbx')
OPENSSL = ['openssl', 'enc', '-aes-256-cbc', '-pbkdf2', '-iter', '200000', '-md', 'sha256']


def unpack_verified(raw, manifest):
    if hashlib.sha256(raw).hexdigest() != ZIP_SHA256 or len(raw) != manifest['zip_bytes']:
        raise ValueError('Patryk animation ZIP checksum/size mismatch')
    result = {}
    with zipfile.ZipFile(io.BytesIO(raw)) as archive:
        if len(archive.namelist()) != len(NAMES) or set(archive.namelist()) != set(NAMES):
            raise ValueError('Unexpected Patryk animation entries')
        for name in NAMES:
            data = archive.read(name)
            if not data.startswith(b'Kaydara FBX Binary') or hashlib.sha256(data).hexdigest() != manifest['files'][name]:
                raise ValueError('Invalid original animation: ' + name)
            result[name] = data
    return result


def install_files(files, destination):
    # Fixed names, no archive-controlled paths; preserve existing Unity GUIDs and all old clips.
    destination = Path(destination) / 'patryk_owner'
    destination.mkdir(parents=True, exist_ok=True)
    for name in NAMES:
        target = destination / name
        with tempfile.NamedTemporaryFile(dir=destination, delete=False) as stream:
            temp = Path(stream.name)
            stream.write(files[name])
        temp.replace(target)
    return [destination / name for name in NAMES]


def install(destination, transfer=TRANSFER):
    if not os.environ.get('KLUCZ', '').strip():
        raise RuntimeError('Missing CI key for owner Patryk animations')
    transfer = Path(transfer)
    manifest = json.loads((transfer / 'manifest.json').read_text())
    if manifest['zip_sha256'] != ZIP_SHA256:
        raise ValueError('Unexpected Patryk animation package')
    encrypted = bytearray()
    for i, chunk in enumerate(manifest['chunks']):
        name = 'part-%03d.enc' % i
        if chunk['file'] != name:
            raise ValueError('Invalid encrypted chunk sequence')
        data = (transfer / name).read_bytes()
        if len(data) != chunk['bytes'] or hashlib.sha256(data).hexdigest() != chunk['sha256']:
            raise ValueError('Damaged animation chunk: ' + name)
        encrypted.extend(data)
    with tempfile.TemporaryDirectory(prefix='patryk-animation-import-') as directory:
        key = Path(directory) / 'key.pem'
        subprocess.run(OPENSSL + ['-d', '-in', str(transfer / 'import-private.pem.enc'),
                       '-out', str(key), '-pass', 'env:KLUCZ'], capture_output=True, check=True)
        key.chmod(0o600)
        password = subprocess.run(['openssl', 'pkeyutl', '-decrypt', '-inkey', str(key),
            '-in', str(transfer / 'package-key.rsa'), '-pkeyopt', 'rsa_padding_mode:oaep',
            '-pkeyopt', 'rsa_oaep_md:sha256'], capture_output=True, check=True).stdout.decode('ascii')
        env = dict(os.environ, PATRYK_ANIM_PASSWORD=password)
        raw = subprocess.run(OPENSSL + ['-d', '-pass', 'env:PATRYK_ANIM_PASSWORD'],
                             input=encrypted, capture_output=True, check=True, env=env).stdout
    files = unpack_verified(raw, manifest)
    paths = install_files(files, destination)
    print('BRAXHORN_PATRYK_ANIM owner clips verified: 6; existing sources preserved')
    return paths

if __name__ == '__main__':
    import argparse
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--destination', type=Path, default=ROOT / 'unity-city/Assets/Source/Prywatne/ruchy')
    install(parser.parse_args().destination)
