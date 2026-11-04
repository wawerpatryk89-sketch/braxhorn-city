"""Encrypted camp authoring checkpoint; never a player/content update package."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import tarfile
import tempfile
from assets import OPENSSL, safe, digest


def permitted(name):
    return (name.startswith('Assets/PlayableGenerated/Oboz/') or
            name in ('Assets/PlayableGenerated.meta', 'Assets/PlayableGenerated/Oboz.meta') or
            (name.startswith('Assets/Source/') and name.endswith('.meta')))


def restore_plain(plain, project):
    with tempfile.TemporaryDirectory(dir=project.parent) as tmp:
        stage = Path(tmp); seen = set()
        with tarfile.open(plain, 'r:gz') as tar:
            for member in tar:
                if not member.isfile() or not permitted(member.name) or member.name in seen:
                    raise ValueError('Unexpected checkpoint member')
                dest = safe(stage, member.name); dest.parent.mkdir(parents=True, exist_ok=True)
                with tar.extractfile(member) as src, dest.open('xb') as out:
                    shutil.copyfileobj(src, out)
                os.utime(dest, (member.mtime, member.mtime))
                seen.add(member.name)
        if 'Assets/PlayableGenerated/Oboz/build-state.json' not in seen:
            raise ValueError('Missing completed camp state')
        for name in sorted(seen):
            target = safe(project, name); target.parent.mkdir(parents=True, exist_ok=True)
            # Checked-in/current metadata always wins over old checkpoint metadata.
            if name.endswith('.meta') and target.exists():
                continue
            os.replace(safe(stage, name), target)


def run(mode, project, archive):
    if not os.environ.get('KLUCZ'):
        raise ValueError('Missing encryption key binding')
    project = project.resolve(); archive = archive.resolve()
    archive.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(dir=archive.parent) as tmp:
        plain = Path(tmp)/'world.tar.gz'
        if mode == 'pack':
            state = project/'Assets/PlayableGenerated/Oboz/build-state.json'
            if not state.is_file():
                raise ValueError('No completed camp generation')
            paths = sorted(p for p in (project/'Assets').rglob('*') if p.is_file() and permitted(p.relative_to(project).as_posix()))
            with tarfile.open(plain, 'w:gz', compresslevel=3, dereference=True) as tar:
                for path in paths:
                    if path.is_symlink(): raise ValueError('Symlink checkpoint source')
                    tar.add(path, arcname=path.relative_to(project).as_posix(), recursive=False)
            candidate = Path(tmp)/'world.enc'
            subprocess.run(OPENSSL+['-salt', '-in', str(plain), '-out', str(candidate), '-pass', 'env:KLUCZ'], check=True)
            if archive.exists(): raise FileExistsError('Immutable checkpoint already exists')
            os.replace(candidate, archive)
            archive.with_suffix(archive.suffix+'.json').write_text(json.dumps({'sha256':digest(archive),'bytes':archive.stat().st_size})+'\n')
        else:
            subprocess.run(OPENSSL+['-d', '-in', str(archive), '-out', str(plain), '-pass', 'env:KLUCZ'], check=True)
            restore_plain(plain, project)
    print('BRAXHORN_OBOZ_CHECKPOINT '+mode+'=ok; generation dependencies are checked again in Unity')


if __name__ == '__main__':
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('mode',choices=['pack','restore']);p.add_argument('--project',type=Path,default=Path('unity-city'));p.add_argument('--archive',type=Path,required=True)
    a=p.parse_args();run(a.mode,a.project,a.archive)
