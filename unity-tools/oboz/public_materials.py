#!/usr/bin/env python3
"""Restore pinned CC0 camp maps; verify bytes before Unity import."""
import hashlib
import json
from pathlib import Path
import subprocess
import tempfile
from urllib.parse import urlparse

ROOT = Path(__file__).resolve().parents[2]
MANIFEST = Path(__file__).with_name('public-materials.json')


def valid_destination(entry, root=ROOT):
    rel = Path(entry['path'])
    if rel.is_absolute() or '..' in rel.parts or not str(rel).startswith('unity-city/Assets/Source/Public/Oboz/'):
        raise ValueError('Invalid public material path')
    url = urlparse(entry['url'])
    if url.scheme != 'https' or url.netloc != 'dl.polyhaven.org' or not url.path.startswith('/file/ph-assets/Textures/'):
        raise ValueError('Unexpected material host')
    target = root / rel
    if not target.resolve().is_relative_to((root/'unity-city/Assets/Source/Public/Oboz').resolve()):
        raise ValueError('Material escapes output directory')
    return target


def matches(path, entry):
    return path.is_file() and path.stat().st_size == entry['bytes'] and hashlib.sha256(path.read_bytes()).hexdigest() == entry['sha256']


def restore(entry, root=ROOT):
    target = valid_destination(entry, root)
    target.parent.mkdir(parents=True, exist_ok=True)
    if not matches(target, entry):
        with tempfile.TemporaryDirectory(dir=target.parent) as tmp:
            part = Path(tmp)/'download'
            subprocess.run(['curl','--fail','--silent','--show-error','--location','--proto','=https','--proto-redir','=https',
                            '--retry','2','--max-time','180',entry['url'],'--output',str(part)],check=True)
            if not matches(part, entry):
                raise ValueError('Public material checksum mismatch: '+entry['path'])
            part.replace(target)
    # Deterministic GUID; retain existing imported metadata on subsequent runs.
    meta=target.with_suffix(target.suffix+'.meta')
    if not meta.exists():
        guid=hashlib.sha256(('braxhorn-public:'+entry['path']).encode()).hexdigest()[:32]
        meta.write_text('fileFormatVersion: 2\nguid: '+guid+'\n')


def main():
    data=json.loads(MANIFEST.read_text())
    if data['schema'] != 1 or data['license'] != 'CC0-1.0':
        raise ValueError('Unsupported public materials manifest')
    for entry in data['files']:
        restore(entry)
    print('OBOZ_PUBLIC_MATERIALS verified='+str(len(data['files'])))


if __name__ == '__main__':
    main()
