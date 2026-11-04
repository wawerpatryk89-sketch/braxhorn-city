#!/usr/bin/env python3
"""Stream one encrypted-by-CI cache: imports, native compilation, exact-match generated city.

The caller pipes tar through zstd and OpenSSL; this script never handles keys.
World files are restored only when the complete input key matches. Imported and
compiled artifacts are reused across keys within the same Unity/Android/region.
"""
import argparse
import io
import json
import pathlib
import shutil
import sys
import tarfile

HEADER = 'braxhorn-cache.json'
FORMAT = 2
WORLD = ('Assets/PlayableGenerated/', 'ProjectSettings/')
EXTRA = ('Assets/Source/Premium/unity-premium-report.json',
         'Assets/Source/Premium/source-refinements.json',
         'Assets/Source/Claude/Osiedle/sky/niebo_odbicia.jpg')


def category(name):
    p = pathlib.PurePosixPath(name)
    if p.is_absolute() or '..' in p.parts or '\\' in name:
        raise ValueError('unsafe cache path')
    if name.startswith('Library/'):
        # Generated Gradle projects may contain signing passwords and whole APKs.
        if name.startswith('Library/Bee/Android/Prj/'):
            return None
        return 'library'
    if name.startswith(WORLD) or name in EXTRA or any(name == p + '.meta' for p in EXTRA):
        return 'world'
    if name.startswith('Assets/') and name.endswith('.meta'):
        return 'meta'
    return None


def pack(root, stream, scope, key):
    header = json.dumps({'format': FORMAT, 'scope': scope, 'world_key': key}).encode()
    count, total = 0, 0
    with tarfile.open(fileobj=stream, mode='w|') as archive:
        info = tarfile.TarInfo(HEADER)
        info.size = len(header)
        archive.addfile(info, io.BytesIO(header))
        for base in ('Library', 'Assets', 'ProjectSettings'):
            for path in sorted((root / base).rglob('*')):
                if path.is_symlink() or not path.is_file():
                    continue
                name = path.relative_to(root).as_posix()
                if category(name) is None:
                    continue
                archive.add(path, arcname=name, recursive=False)
                count += 1
                total += path.stat().st_size
    print(f'BRAXHORN_CACHE packed files={count} raw_bytes={total}', file=sys.stderr)


def restore(root, stream, scope, key, world_enabled=True, journal=None):
    restored, skipped = 0, 0
    with tarfile.open(fileobj=stream, mode='r|') as archive:
        header = archive.next()
        if header is None or header.name != HEADER or not header.isfile() or header.size > 4096:
            raise ValueError('invalid cache header')
        state = json.load(archive.extractfile(header))
        if state.get('format') != FORMAT or state.get('scope') != scope:
            raise ValueError('cache is for another Unity/platform/region')
        same_world = world_enabled and state.get('world_key') == key
        for member in archive:
            if member.name == HEADER:
                continue
            kind = category(member.name)
            if not member.isfile() or kind is None or (kind == 'world' and not same_world):
                skipped += 1
                continue
            target = root / member.name
            # Checkout and preparation outputs are authoritative. Never replace them.
            if target.exists():
                skipped += 1
                continue
            if target.is_symlink() or any(p.is_symlink() for p in target.parents):
                raise ValueError('cache target contains symlink')
            target.parent.mkdir(parents=True, exist_ok=True)
            if journal:
                with journal.open('a') as record:
                    record.write(member.name + '\n')
            with archive.extractfile(member) as source, target.open('xb') as dest:
                shutil.copyfileobj(source, dest, 1024 * 1024)
            restored += 1
    print(f'BRAXHORN_CACHE restored={restored} skipped={skipped} exact_world={same_world}', file=sys.stderr)
    return same_world


def rollback(root, journal):
    """Remove only files created by this restore; checkout/preparation files were skipped."""
    if not journal or not journal.exists():
        return
    for name in reversed(journal.read_text().splitlines()):
        if category(name) is None:
            raise ValueError('unexpected restore journal path')
        target = root / name
        if target.is_symlink() or any(p.is_symlink() for p in target.parents):
            raise ValueError('restore rollback contains symlink')
        target.unlink(missing_ok=True)
        parent = target.parent
        while parent != root and parent.is_relative_to(root):
            try:
                parent.rmdir()
            except OSError:
                break
            parent = parent.parent
    journal.unlink()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('mode', choices=('pack', 'restore', 'rollback'))
    parser.add_argument('--root', type=pathlib.Path, default=pathlib.Path('unity-city'))
    parser.add_argument('--scope')
    parser.add_argument('--key')
    parser.add_argument('--no-world', action='store_true')
    parser.add_argument('--journal', type=pathlib.Path)
    args = parser.parse_args()
    if args.mode == 'rollback':
        rollback(args.root, args.journal)
        return
    if not args.scope or not args.key:
        parser.error('--scope and --key are required for pack/restore')
    if args.mode == 'pack':
        pack(args.root, sys.stdout.buffer, args.scope, args.key)
    else:
        restore(args.root, sys.stdin.buffer, args.scope, args.key, not args.no_world, args.journal)


if __name__ == '__main__':
    main()
