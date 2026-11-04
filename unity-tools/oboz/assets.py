"""Camp-only private input inventory, encrypted delivery and fail-closed restore."""
import argparse
import hashlib
import json
import os
import re
from pathlib import Path, PurePosixPath
import shutil
import struct
import subprocess
import tarfile
import tempfile
from urllib.parse import unquote, urlparse

ROOT = Path(__file__).resolve().parents[2]
PRIVATE = ROOT / 'unity-city/Assets/Source/Prywatne/Oboz'
MANIFEST = ROOT / 'unity-tools/oboz/private-inputs.json'
OPENSSL = ['openssl', 'enc', '-aes-256-cbc', '-pbkdf2', '-iter', '200000', '-md', 'sha256']


def digest(path):
    with Path(path).open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def safe(root, name):
    p = PurePosixPath(name)
    if name in ('', '.') or p.is_absolute() or '..' in p.parts or '\\' in name or p.as_posix() != name:
        raise ValueError('Unsafe source path')
    target = root / name
    if not target.resolve().is_relative_to(root.resolve()):
        raise ValueError('Source path leaves private folder')
    return target


def inventory(root=PRIVATE):
    root = root.resolve()
    selected = set()
    for folder in ('characters', 'animations', 'environment/materials', 'environment/forest-ground', 'environment/services', 'environment/vegetation/billboards'):
        selected.update(p for p in (root/folder).rglob('*') if p.is_file() and p.suffix != '.meta'
                        and not any(part.startswith('.') for part in p.relative_to(root).parts))
    selected.update((root/'environment/vegetation').glob('*__unity.glb'))
    selected.update([root/'environment/models/underground_bunker__unity.glb',
                     root/'environment/gym/braxhorn-original-gym__unity.glb', root/'environment/gym/gym-collisions.json'])
    # Derived GLBs use original external image bytes. Resolve their actual dependencies;
    # unused original exports remain preserved locally and are not imported twice in CI.
    for model in list(selected):
        if model.suffix != '.glb':
            continue
        with model.open('rb') as stream:
            magic, version, total, size, kind = struct.unpack('<4sIIII', stream.read(20))
            if magic != b'glTF' or version != 2 or kind != 0x4e4f534a or total != model.stat().st_size:
                raise ValueError('Invalid GLB: ' + model.name)
            data = json.loads(stream.read(size))
        for image in data.get('images', []) + data.get('buffers', []):
            uri = image.get('uri')
            if not uri or uri.startswith('data:'):
                continue
            if urlparse(uri).scheme or uri.startswith('//'):
                raise ValueError('Remote GLB dependency is not permitted')
            target = (model.parent/unquote(uri)).resolve()
            if not target.is_relative_to(root):
                raise ValueError('GLB dependency leaves camp sources')
            selected.add(target)
    records = []
    for path in sorted(selected):
        if path.is_symlink() or not path.is_file():
            raise ValueError('Missing source: ' + str(path))
        records.append(dict(path=path.relative_to(root).as_posix(), bytes=path.stat().st_size, sha256=digest(path)))
    return dict(schema=1, zone='oboz', files=records, releaseTag=None, packages=[], encryptedPackagesPrepared=False)


def validate_inventory(data):
    if data.get('schema') != 1 or data.get('zone') != 'oboz' or not data.get('files'):
        raise ValueError('Missing camp source inventory')
    seen = set()
    for item in data['files']:
        safe(PRIVATE, item['path'])
        if type(item['bytes']) is not int or item['bytes'] < 0 or not re.fullmatch(r'[0-9a-f]{64}', item['sha256']):
            raise ValueError('Invalid source record')
        if item['path'].endswith('.meta'):
            raise ValueError('Source delivery cannot overwrite Unity metadata')
        if item['path'] in seen:
            raise ValueError('Duplicate source')
        seen.add(item['path'])
    return len(seen)


def verify_sources(data, root=PRIVATE):
    count = validate_inventory(data)
    for item in data['files']:
        path = safe(root, item['path'])
        if path.is_symlink() or not path.is_file() or path.stat().st_size != item['bytes'] or digest(path) != item['sha256']:
            raise ValueError('Missing/changed source: ' + item['path'])
    return count


def require_delivery(data):
    validate_inventory(data)
    if data.get('encryptedPackagesPrepared') is not True or not data.get('packages') or not data.get('releaseTag'):
        raise ValueError('Private camp packages have not been encrypted and uploaded; Unity will not start')
    if not re.fullmatch(r'oboz-private-[0-9a-f]{12}', data['releaseTag']):
        raise ValueError('Invalid camp delivery release tag')
    if not os.environ.get('KLUCZ'):
        raise ValueError('Missing encryption key binding KLUCZ')
    names = set()
    for p in data['packages']:
        if not re.fullmatch(r'oboz_[0-9a-f]{12}_[0-9]{3}\.tar\.gz\.enc', p['name']) or p['name'] in names:
            raise ValueError('Invalid package name')
        if type(p['bytes']) is not int or not 16 <= p['bytes'] <= 200*1024*1024 or not re.fullmatch(r'[0-9a-f]{64}', p['sha256']):
            raise ValueError('Invalid encrypted package receipt')
        names.add(p['name'])


def encrypt(data, source=PRIVATE):
    if not os.environ.get('KLUCZ'):
        raise ValueError('Bind KLUCZ securely before encrypting; never put the key in a file or chat')
    verify_sources(data, source)
    helper = ROOT/'unity-tools/claude/prywatne.py'
    if not helper.is_file():
        raise ValueError('Missing original shared encryption tool')
    groups, batch, size = [], [], 0
    for entry in data['files']:
        if batch and size + entry['bytes'] > 180*1024*1024:
            groups.append(batch); batch, size = [], 0
        batch.append(entry); size += entry['bytes']
    if batch:
        groups.append(batch)
    packages = []
    identity = hashlib.sha256(json.dumps(data['files'], sort_keys=True).encode()).hexdigest()[:12]
    for i, group in enumerate(groups):
        name = f'oboz_{identity}_{i:03d}'
        path = ROOT/'unity-city/Prywatne'/f'{name}.tar.gz.enc'
        if path.exists():
            raise FileExistsError('Existing encrypted input is immutable: '+path.name)
        with tempfile.TemporaryDirectory(prefix='oboz-encrypt-') as tmp:
            stage = Path(tmp)
            for entry in group:
                dest = safe(stage, entry['path']); dest.parent.mkdir(parents=True, exist_ok=True)
                shutil.copyfile(safe(source, entry['path']), dest)
            subprocess.run(['python3', str(helper), 'szyfruj', name, str(stage)], cwd=ROOT, check=True)
        with path.open('rb') as stream:
            if stream.read(8) != b'Salted__':
                raise ValueError('Invalid encrypted package header')
        if path.stat().st_size > 200*1024*1024:
            raise ValueError('Encrypted package exceeds 200 MiB')
        packages.append(dict(name=path.name, sha256=digest(path), bytes=path.stat().st_size))
    data.update(packages=packages, releaseTag='oboz-private-'+identity, encryptedPackagesPrepared=True)
    MANIFEST.write_text(json.dumps(data, ensure_ascii=False, indent=2)+'\n')
    print('Encrypted packages prepared; upload to the named private draft release before build.')


def extract_verified(tar, destination, expected, seen):
    for item in tar:
        if not item.isfile() or item.name not in expected or item.name in seen:
            raise ValueError('Unexpected/duplicate archive member')
        record = expected[item.name]
        if item.size != record['bytes']:
            raise ValueError('Wrong source size')
        path = safe(destination, item.name); path.parent.mkdir(parents=True, exist_ok=True)
        h = hashlib.sha256()
        with tar.extractfile(item) as source, path.open('xb') as output:
            for chunk in iter(lambda: source.read(1024*1024), b''):
                h.update(chunk); output.write(chunk)
        if h.hexdigest() != record['sha256']:
            raise ValueError('Wrong plaintext source hash')
        seen.add(item.name)


def restore(data):
    require_delivery(data)
    repo = os.environ['GITHUB_REPOSITORY']
    expected = {r['path']: r for r in data['files']}
    if len(expected) != len(data['files']):
        raise ValueError('Duplicate source records')
    PRIVATE.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix='oboz-restore-', dir=PRIVATE.parent) as tmp:
        stage = Path(tmp)/'sources'; stage.mkdir(); seen = set()
        for pack in data['packages']:
            archive = Path(tmp)/pack['name']
            subprocess.run(['gh', 'release', 'download', data['releaseTag'], '--repo', repo, '--pattern', pack['name'], '--dir', tmp], check=True)
            if archive.stat().st_size != pack['bytes'] or digest(archive) != pack['sha256']:
                raise ValueError('Encrypted package damaged or incomplete')
            with archive.open('rb') as stream:
                if stream.read(8) != b'Salted__':
                    raise ValueError('Expected encrypted archive')
            plain = Path(tmp)/'source.tar.gz'
            subprocess.run(OPENSSL+['-d', '-in', str(archive), '-out', str(plain), '-pass', 'env:KLUCZ'], check=True)
            with tarfile.open(plain, 'r:gz') as tar:
                extract_verified(tar, stage, expected, seen)
            archive.unlink(); plain.unlink()
        if seen != set(expected):
            raise ValueError('Incomplete camp asset delivery')
        verify_sources(data, stage)
        # Install only after the complete set is checked. Existing Unity .meta files stay intact.
        for name in sorted(seen):
            target = safe(PRIVATE, name); target.parent.mkdir(parents=True, exist_ok=True)
            os.replace(safe(stage, name), target)
    print('BRAXHORN_OBOZ_SOURCES verified='+str(len(expected)))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('command', choices=['inventory', 'verify', 'preflight', 'encrypt', 'restore'])
    args = parser.parse_args()
    if args.command == 'inventory':
        data = inventory(); MANIFEST.write_text(json.dumps(data, ensure_ascii=False, indent=2)+'\n')
        print(json.dumps(dict(files=len(data['files']), bytes=sum(x['bytes'] for x in data['files']), encryptedPackagesPrepared=False)))
    else:
        data = json.loads(MANIFEST.read_text())
        if args.command == 'verify': print('Verified '+str(verify_sources(data))+' camp input files')
        elif args.command == 'preflight': require_delivery(data)
        elif args.command == 'encrypt': encrypt(data)
        else: restore(data)
