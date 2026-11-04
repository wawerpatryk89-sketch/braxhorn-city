#!/usr/bin/env python3
"""Restore pinned CC0 camp maps; verify bytes before Unity import."""
import hashlib
import json
from pathlib import Path
import subprocess
import tempfile
from urllib.parse import urlparse, parse_qs
import zipfile

ROOT = Path(__file__).resolve().parents[2]
MANIFEST = Path(__file__).with_name('public-materials.json')


def material_path(entry, root=ROOT):
    rel = Path(entry['path'])
    if rel.is_absolute() or '..' in rel.parts or not str(rel).startswith('unity-city/Assets/Source/Public/Oboz/'):
        raise ValueError('Invalid public material path')
    target = root / rel
    if not target.resolve().is_relative_to((root/'unity-city/Assets/Source/Public/Oboz').resolve()):
        raise ValueError('Material escapes output directory')
    return target


def valid_destination(entry, root=ROOT):
    url = urlparse(entry['url'])
    if url.scheme != 'https' or url.netloc != 'dl.polyhaven.org' or not url.path.startswith('/file/ph-assets/Textures/'):
        raise ValueError('Unexpected material host')
    return material_path(entry, root)


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
    ensure_meta(target, entry)


def ensure_meta(target, entry):
    # Deterministic GUID; retain existing imported metadata on subsequent runs.
    meta=target.with_suffix(target.suffix+'.meta')
    if not meta.exists():
        guid=hashlib.sha256(('braxhorn-public:'+entry['path']).encode()).hexdigest()[:32]
        meta.write_text('fileFormatVersion: 2\nguid: '+guid+'\n')


def restore_archive(entry, root=ROOT, local_archive=None):
    url = urlparse(entry['url'])
    if (entry.get('license') != 'CC0-1.0' or url.scheme != 'https' or
            url.netloc != 'ambientcg.com' or url.path != '/get' or
            parse_qs(url.query) != {'file': [entry['asset']+'_4K-JPG.zip']}):
        raise ValueError('Unexpected public archive source')
    files = entry['files']
    targets = [material_path(f, root) for f in files]
    if not targets or len(set(targets)) != len(targets):
        raise ValueError('Empty or duplicated archive destinations')
    if all(matches(t, f) for t, f in zip(targets, files)):
        for t, f in zip(targets, files):
            ensure_meta(t, f)
        return
    # Verify the archive and every selected member before replacing any map.
    with tempfile.TemporaryDirectory() as tmp:
        archive = Path(local_archive) if local_archive else Path(tmp)/'source.zip'
        if not local_archive:
            subprocess.run(['curl','--fail','--silent','--show-error','--location',
                            '--proto','=https','--proto-redir','=https','--retry','2',
                            '--max-time','300',entry['url'],'--output',str(archive)],check=True)
        if not matches(archive, entry):
            raise ValueError('Public archive checksum mismatch: '+entry['asset'])
        staged = []
        with zipfile.ZipFile(archive) as source:
            for i, f in enumerate(files):
                name = f['member']
                if Path(name).name != name or not name.endswith('.jpg'):
                    raise ValueError('Invalid archive member')
                members = [m for m in source.infolist() if m.filename == name]
                if len(members) != 1 or members[0].file_size != f['bytes']:
                    raise ValueError('Unexpected archive member size or count')
                part = Path(tmp)/str(i)
                part.write_bytes(source.read(members[0]))
                if not matches(part, f):
                    raise ValueError('Public map checksum mismatch: '+name)
                staged.append(part)
        for part, target, f in zip(staged, targets, files):
            target.parent.mkdir(parents=True, exist_ok=True)
            # Stage on the destination filesystem for an atomic final rename.
            with tempfile.NamedTemporaryFile(dir=target.parent, delete=False) as out:
                pending = Path(out.name)
                out.write(part.read_bytes())
            try:
                pending.replace(target)
            finally:
                pending.unlink(missing_ok=True)
            ensure_meta(target, f)


def main():
    data=json.loads(MANIFEST.read_text())
    if data['schema'] != 1 or data['license'] != 'CC0-1.0':
        raise ValueError('Unsupported public materials manifest')
    for entry in data['files']:
        restore(entry)
    for entry in data.get('archives', []):
        restore_archive(entry)
    count = len(data['files']) + sum(len(a['files']) for a in data.get('archives', []))
    print('OBOZ_PUBLIC_MATERIALS verified='+str(count))


if __name__ == '__main__':
    main()

