#!/usr/bin/env python3
"""Immutable, encrypted content authoring baseline in an authenticated draft.

The APK carries the initial bundles. The private snapshot retains authored scenes,
source assets, GUIDs, Addressables settings and ORIGINAL content state for later
content-only builds. Neither signing keys nor Library are part of this snapshot.
"""
import argparse
import io
import json
import os
from pathlib import Path, PurePosixPath
import re
import shutil
import subprocess
import tarfile
import tempfile

from unity_cache import digest, require_key, output
from durable_cache import upload, download, view, gh

HEADER = 'content-snapshot.json'
ID = re.compile(r'(osiedle|miasto|miastogl|oboz|mistrzowie|smoke)-[a-f0-9]{32}\Z')
CODE = {'.cs', '.java', '.jslib', '.dll', '.so', '.aar', '.jar', '.asmdef', '.asmref'}


def identity(manifest):
    base = manifest['baseId']
    if not ID.fullmatch(base) or base.split('-')[0] != manifest['zone']:
        raise ValueError('Invalid base identity')
    return dict(key='content-' + base, base=base, zone=manifest['zone'], runtime=manifest['runtimeId'], schema=1)


def allowed(name):
    p = PurePosixPath(name)
    if p.is_absolute() or '..' in p.parts or '\\' in name or not p.parts:
        return False
    if name.startswith('Assets/StreamingAssets/BraxContent/'):
        return False  # Baseline bundles already live in the APK.
    # This SINGLE reproducible jar is compiled into the first APK. Preserve
    # its exact bytes in the private encrypted authoring snapshot, so later
    # Addressables-only builds cannot change Java runtime code by accident.
    # Never put it into the .braxupdate.zip package.
    if name == 'Assets/Plugins/Android/BraxContentPicker.jar':
        return True
    if name.startswith('Assets/'):
        return p.suffix not in CODE
    return name.startswith('BraxContentBuild/base/') or name.startswith('ProjectSettings/') or name == 'Packages/packages-lock.json'


def pack(project, archive, expected):
    require_key()
    files = []
    for root in ('Assets', 'ProjectSettings', 'BraxContentBuild/base', 'Packages'):
        for base, dirs, names in os.walk(project / root, followlinks=False):
            dirs[:] = sorted(d for d in dirs if not (Path(base) / d).is_symlink())
            for name in sorted(names):
                path = Path(base) / name
                rel = path.relative_to(project).as_posix()
                if path.is_symlink():
                    raise ValueError('Symlink in baseline')
                if path.is_file() and allowed(rel):
                    if root == 'ProjectSettings' and re.search(rb'(AndroidKeystorePass|AndroidKeyaliasPass):[ \t]*[^ \t\r\n]+', path.read_bytes()):
                        raise ValueError('Signing password found in ProjectSettings')
                    files.append((path, rel))
    if not files:
        raise ValueError('Empty snapshot')
    archive.parent.mkdir(parents=True, exist_ok=True)
    temporary = archive.with_suffix('.partial')
    encrypt = compress = None
    try:
        with temporary.open('wb') as dst:
            encrypt = subprocess.Popen(['openssl','enc','-aes-256-cbc','-pbkdf2','-salt','-pass','env:KLUCZ'], stdin=subprocess.PIPE, stdout=dst)
            compress = subprocess.Popen(['zstd','-q','-T2','-3','-c'], stdin=subprocess.PIPE, stdout=encrypt.stdin)
            encrypt.stdin.close()
            with tarfile.open(fileobj=compress.stdin, mode='w|', dereference=True) as tar:
                payload = json.dumps(expected, sort_keys=True).encode()
                header = tarfile.TarInfo(HEADER); header.size = len(payload)
                tar.addfile(header, io.BytesIO(payload))
                for path, rel in files:
                    tar.add(path, arcname=rel, recursive=False)
            compress.stdin.close()
            if compress.wait() or encrypt.wait():
                raise ValueError('Snapshot compression/encryption failed')
        temporary.replace(archive)
    finally:
        for proc in (compress, encrypt):
            if proc and proc.poll() is None:
                proc.kill(); proc.wait()
        temporary.unlink(missing_ok=True)
    archive.with_name(archive.name+'.json').write_text(json.dumps(dict(profile=expected,sha256=digest(archive),bytes=archive.stat().st_size)))


def extract(stream, stage, expected):
    seen = set()
    with tarfile.open(fileobj=stream, mode='r|') as tar:
        for entry in tar:
            if not entry.isfile() or entry.name in seen:
                raise ValueError('Invalid or duplicate snapshot member')
            seen.add(entry.name)
            if entry.name == HEADER:
                if len(seen) != 1 or entry.size > 16384 or json.load(tar.extractfile(entry)) != expected:
                    raise ValueError('Snapshot identity mismatch')
                continue
            if HEADER not in seen or not allowed(entry.name):
                raise ValueError('Forbidden snapshot path')
            if entry.size < 0 or entry.size > shutil.disk_usage(stage).free - 64 * 1024 * 1024:
                raise ValueError('Insufficient disk space for snapshot')
            path = stage / entry.name
            path.parent.mkdir(parents=True, exist_ok=True)
            with tar.extractfile(entry) as src, path.open('wb') as dst:
                shutil.copyfileobj(src, dst, 1024 * 1024)
            os.utime(path, (entry.mtime, entry.mtime))
    required = ('BraxContentBuild/base/manifest.json', 'BraxContentBuild/base/addressables_content_state.bin',
                'BraxContentBuild/base/scene-guids.txt', 'Assets/Resources/BraxContentBase.json')
    if any(name not in seen for name in required):
        raise ValueError('Incomplete baseline snapshot')
    if identity(json.loads((stage / required[0]).read_text())) != expected:
        raise ValueError('Manifest and snapshot differ')


def restore(project, archive, expected):
    require_key()
    if (project/'BraxContentBuild/base').exists() or (project/'Assets/AddressableAssetsData').exists():
        raise ValueError('Restore requires a clean content workspace')
    decrypt = decompress = None
    with tempfile.TemporaryDirectory(dir=project.parent) as tmp:
        stage = Path(tmp)
        try:
            decrypt = subprocess.Popen(['openssl','enc','-d','-aes-256-cbc','-pbkdf2','-pass','env:KLUCZ','-in',str(archive)], stdout=subprocess.PIPE)
            decompress = subprocess.Popen(['zstd','-q','-d','-c'], stdin=decrypt.stdout, stdout=subprocess.PIPE)
            decrypt.stdout.close()
            extract(decompress.stdout, stage, expected)
            decompress.stdout.close()
            if decompress.wait() or decrypt.wait():
                raise ValueError('Snapshot decryption failed')
            # Validate destinations before installing any files. Checked-in source
            # changes win; generated authoring state/settings come from the baseline.
            copies = []
            for source in stage.rglob('*'):
                if not source.is_file() or source.name == HEADER:
                    continue
                relative = source.relative_to(stage)
                destination = project / relative
                if not destination.resolve().is_relative_to(project.resolve()):
                    raise ValueError('Destination symlink escapes project')
                generated = str(relative).startswith(('Assets/PlayableGenerated/', 'Assets/AddressableAssetsData/', 'BraxContentBuild/', 'ProjectSettings/'))
                if generated or not destination.exists():
                    copies.append((source, destination))
            for source, destination in copies:
                destination.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(source, destination)
        finally:
            for proc in (decompress, decrypt):
                if proc:
                    if proc.poll() is None:
                        proc.kill(); proc.wait()
                    if proc.stdout and not proc.stdout.closed:
                        proc.stdout.close()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('command', choices=('publish-base','restore-base','publish-update'))
    parser.add_argument('--project', type=Path, default=Path('unity-city'))
    parser.add_argument('--base')
    args = parser.parse_args()
    project, repo = args.project, os.environ['GITHUB_REPOSITORY']
    archive = project.parent / 'content-baseline.tar.zst.enc'
    if args.command == 'publish-base':
        manifest = json.loads((project/'BraxContentBuild/base/manifest.json').read_text())
        expected = identity(manifest); tag = 'content-base-' + expected['base']
        try:
            existing = view(repo, tag)
        except subprocess.CalledProcessError:
            existing = None
        if existing is not None:
            index = json.loads(existing['body'])
            if index.get('profile') != expected or index.get('current'):
                raise ValueError('Immutable base already exists; refusing overwrite')
        pack(project, archive, expected)
        upload(repo, tag, archive, expected)
        gh('release','edit',tag,'--repo',repo,'--title','Braxhorn private content baseline: '+expected['base'])
        output('base', expected['base'])
        archive.unlink()
        archive.with_name(archive.name+'.json').unlink(missing_ok=True)
    else:
        if not args.base or not ID.fullmatch(args.base):
            raise ValueError('Explicit base ID required')
        tag = 'content-base-' + args.base
        data = view(repo, tag)
        expected = json.loads(data['body'])['profile']
        if expected.get('base') != args.base or expected.get('key') != 'content-' + args.base:
            raise ValueError('Unexpected baseline release')
        if args.command == 'restore-base':
            download(repo, tag, archive, expected)
            restore(project, archive, expected)
            archive.unlink()
        else:
            packages = list((project/'BraxContentBuild').glob('*.braxupdate.zip'))
            if len(packages) != 1 or packages[0].stat().st_size >= 2 * 1024**3:
                raise ValueError('Expected one update below 2 GiB; subdivide oversized content')
            # This is a private draft attachment, downloaded by the owner and
            # imported via the system file picker. It is NOT an anonymous CDN.
            gh('release','upload',tag,str(packages[0]),'--repo',repo)
            print('PRIVATE_UPDATE_READY', packages[0].name, digest(packages[0]))


if __name__ == '__main__':
    main()
