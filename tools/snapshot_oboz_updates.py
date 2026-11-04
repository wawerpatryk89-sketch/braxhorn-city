"""Preserve Unity-exported camp files and .meta. This is NOT a content update build."""
import argparse
import hashlib
import json
import re
import subprocess
import tempfile
import zipfile
from pathlib import Path, PurePosixPath


def digest(path):
    h = hashlib.sha256()
    with path.open('rb') as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b''):
            h.update(chunk)
    return h.hexdigest()


def local_path(project, name):
    p = PurePosixPath(name)
    if not name or '\\' in name or p.is_absolute() or '..' in p.parts or p.as_posix() != name:
        raise ValueError('Unsafe path: ' + name)
    if p.parts[0] not in {'Assets', 'ProjectSettings', 'Packages'}:
        raise ValueError('Outside project inputs: ' + name)
    target = (project / name).resolve()
    if not target.is_relative_to(project.resolve()):
        raise ValueError('Symlink leaves project: ' + name)
    if p.suffix.lower() in {'.keystore', '.jks', '.p12', '.pem', '.key'}:
        raise ValueError('Signing/credential file must not be exported: ' + name)
    return target


def validate(project, registry, previous=None):
    if (registry.get('schema'), registry.get('zone'), registry.get('kind')) != (1, 'oboz', 'authoring-snapshot'):
        raise ValueError('Unsupported camp authoring registry')
    if registry.get('readyForContentUpdate') is not False or registry.get('runtimeVerified') is not False:
        raise ValueError('Authoring inventory cannot certify runtime/content updates')
    if registry.get('unityVersion') != '6000.0.58f2':
        raise ValueError('Unexpected Unity version')
    records = {}
    for record in registry['files']:
        name = record['path']
        if name in records:
            raise ValueError('Duplicate file: ' + name)
        path = local_path(project, name)
        if not path.is_file() or path.stat().st_size != record['bytes'] or digest(path) != record['sha256']:
            raise ValueError('Missing/changed export input: ' + name)
        records[name] = record
    for required in ('Packages/manifest.json', 'Packages/packages-lock.json', 'ProjectSettings/ProjectVersion.txt'):
        if required not in records:
            raise ValueError('Missing pinned configuration: ' + required)
    source_extensions = {'.cs', '.shader', '.cginc', '.hlsl', '.asmdef', '.asmref', '.rsp'}
    for path in (project / 'Assets').rglob('*'):
        if path.is_file() and path.suffix in source_extensions and path.relative_to(project).as_posix() not in records:
            raise ValueError('Source added after inventory: ' + str(path))
    for path in (project / 'ProjectSettings').rglob('*'):
        if path.is_file() and path.suffix in {'.asset', '.txt'} and path.relative_to(project).as_posix() not in records:
            raise ValueError('Settings added after inventory: ' + str(path))
    for name in records:
        if name.startswith('Assets/') and not name.endswith('.meta'):
            if name + '.meta' not in records:
                raise ValueError('Missing asset metadata: ' + name)
            folder = PurePosixPath(name).parent
            while str(folder) != 'Assets':
                if str(folder) + '.meta' not in records:
                    raise ValueError('Missing folder metadata: ' + str(folder))
                folder = folder.parent
    entries = {}
    guids = set()
    for entry in registry['entries']:
        address, name, guid = entry['address'], entry['path'], entry['guid']
        if (not re.fullmatch(r'oboz/(scenes|characters)/[a-z0-9_-]+', address) or address in entries or
                not re.fullmatch(r'[a-f0-9]{32}', guid) or guid in guids):
            raise ValueError('Invalid/duplicate address or GUID: ' + address)
        if not name.startswith('Assets/PlayableGenerated/Oboz/') or name not in records or name+'.meta' not in records:
            raise ValueError('Missing camp scene/prefab: ' + name)
        meta = (project / (name + '.meta')).read_text(encoding='utf-8')
        if not re.search(r'^guid: ' + guid + r'\s*$', meta, re.MULTILINE):
            raise ValueError('GUID differs from .meta: ' + name)
        for dep in entry['dependencies']:
            if dep.startswith('Assets/') and dep not in records:
                raise ValueError('Missing dependency: ' + dep)
            if dep.startswith('Packages/') and dep not in registry.get('packageDependencies', []):
                raise ValueError('Missing package dependency: ' + dep)
        entries[address] = entry
        guids.add(guid)
    expected = {'oboz/scenes/wejscie'} | {'oboz/characters/' + c for c in ('monika', 'vorn', 'zyro', 'rakk', 'kade', 'gorr', 'mis')}
    if set(entries) != expected:
        raise ValueError('Wrong camp scene/roster addresses')
    if previous:
        for before in previous['entries']:
            after = entries.get(before['address'])
            if not after or (before['path'], before['guid']) != (after['path'], after['guid']):
                raise ValueError('Stable address/path/GUID changed: ' + before['address'])
    return records


def snapshot(project, registry, destination, previous=None):
    project, destination = project.resolve(), destination.resolve()
    records = validate(project, registry, previous)
    if destination.exists():
        raise FileExistsError('Snapshot is immutable: ' + str(destination))
    destination.parent.mkdir(parents=True, exist_ok=True)
    # Write a private temporary archive; only a complete verified snapshot is handed off.
    with tempfile.TemporaryDirectory(prefix='oboz-snapshot-', dir=destination.parent) as temp:
        candidate = Path(temp) / 'snapshot.zip'
        with zipfile.ZipFile(candidate, 'w', zipfile.ZIP_DEFLATED, allowZip64=True) as archive:
            for name in sorted(records):
                archive.write(local_path(project, name), 'unity-city/' + name)
            archive.writestr('registry.json', json.dumps(registry, ensure_ascii=False, sort_keys=True, indent=2)+'\n')
        # Check the bytes actually archived, not only the source before copying.
        with zipfile.ZipFile(candidate) as archive:
            for name, record in records.items():
                with archive.open('unity-city/' + name) as stream:
                    h = hashlib.sha256()
                    for block in iter(lambda: stream.read(1024 * 1024), b''):
                        h.update(block)
                if h.hexdigest() != record['sha256']:
                    raise ValueError('Input changed during export: ' + name)
        # Exclusive create prevents accidental replacement of any earlier baseline.
        try:
            with destination.open('xb') as out, candidate.open('rb') as src:
                for block in iter(lambda: src.read(1024 * 1024), b''):
                    out.write(block)
        except FileExistsError:
            raise
        except BaseException:
            destination.unlink(missing_ok=True)
            raise
    return {'file': str(destination), 'sha256': digest(destination), 'bytes': destination.stat().st_size,
            'files': len(records), 'readyForContentUpdate': False, 'requiresPrivateStorage': True}


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project', type=Path, default=Path(__file__).resolve().parents[1] / 'unity-city')
    parser.add_argument('--previous', type=Path, help='Previous registry for stable GUID/address checks')
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    data = json.loads((args.project/'ObozUpdateExport/registry.json').read_text())
    try:
        data['localSourceCommit'] = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=args.project, text=True).strip()
        data['workingTreeDirty'] = bool(subprocess.check_output(['git', 'status', '--porcelain'], cwd=args.project, text=True).strip())
    except subprocess.CalledProcessError as exc:
        parser.error('Cannot record source revision: ' + str(exc))
    previous = json.loads(args.previous.read_text()) if args.previous else None
    print(json.dumps(snapshot(args.project, data, args.output, previous), ensure_ascii=False))
