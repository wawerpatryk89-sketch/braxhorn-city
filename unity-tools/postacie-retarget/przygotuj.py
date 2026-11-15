"""Build a working plan from the owner's FBX files/ZIPs without changing them."""
import argparse
import hashlib
import json
import os
import shutil
import tempfile
import zipfile
from pathlib import Path, PurePosixPath


def digest(stream):
    return hashlib.file_digest(stream, 'sha256').hexdigest()


def safe_member(name):
    path = PurePosixPath(name.replace('\\', '/'))
    if path.is_absolute() or '..' in path.parts or any(':' in p for p in path.parts):
        raise ValueError("Unsafe archive path: " + name)
    return Path(*path.parts)


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--wejscie', type=Path, required=True)
    p.add_argument('--projekt', type=Path, required=True)
    p.add_argument('--katalog', type=Path, default=Path(__file__).with_name('obsada.json'))
    p.add_argument('--postacie', nargs='+')
    p.add_argument('--nowe', type=Path, help='JSON array of id, model (loose FBX) and role')
    a = p.parse_args()
    raw = a.wejscie.resolve()
    final = a.projekt.resolve()
    if not raw.is_dir() or final.exists() or final.is_relative_to(raw):
        p.error('Use an existing input directory and a new project path outside it.')
    cat = json.loads(a.katalog.read_text(encoding='utf-8'))
    known = cat['characters']
    chosen = a.postacie if a.postacie else ([] if a.nowe else list(known))
    chars = {k: dict(known[k]) for k in chosen}
    new = []
    if a.nowe:
        for row in json.loads(a.nowe.read_text(encoding='utf-8')):
            k = row['id']
            role = row['role']
            file = (a.nowe.resolve().parent / row['model']).resolve()
            if k in chars:
                raise ValueError('Duplicate character ' + k)
            chars[k] = {'profile': cat['profiles'][role], 'original_name': file.name}
            if file.suffix.lower() != '.fbx':
                raise ValueError('New character must be a rigged FBX')
            with file.open('rb') as src:
                chars[k]['sha256'] = digest(src)
            new.append(file)
    if not chars:
        raise ValueError('No characters')
    wanted = {entry if isinstance(entry, str) else entry['clip']
              for c in chars.values() for entry in c['profile'].values()}
    clips = {k: dict(v) for k, v in cat['clips'].items() if k in wanted}
    by_hash = {}
    for k, c in clips.items():
        for h in c['accepted_hashes']:
            by_hash.setdefault(h, []).append(k)
    models = {}
    for k, c in chars.items():
        if not k or not all(x.isascii() and (x.isalnum() or x in '_-') for x in k):
            raise ValueError('Invalid character ID: ' + k)
        models.setdefault(c['sha256'], []).append(k)
    final.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(dir=final.parent) as temp:
        out = Path(temp) / 'projekt'
        out.mkdir()
        (out / 'clips').mkdir()

        def accept(h, entry, file, z):
            for k in by_hash.get(h, []):
                spec = clips[k]
                if 'file' in spec:
                    continue
                target = out / 'clips' / (k + '.fbx')
                if z:
                    with z.open(entry) as src, target.open('wb') as dst:
                        shutil.copyfileobj(src, dst)
                else:
                    shutil.copyfile(file, target)
                spec.update(file=str(target.relative_to(out)), sha256=h)
            for k in models.get(h, []):
                if 'model' in chars[k]:
                    continue
                folder = out / 'models' / k
                folder.mkdir(parents=True)
                if z:
                    modelpath = safe_member(entry)
                    orig = modelpath.parent
                    for name in z.namelist():
                        path = safe_member(name)
                        if path.parent != orig and orig not in path.parents:
                            continue
                        if name != entry and path.suffix.lower() not in ('.png', '.jpg', '.jpeg', '.tga', '.tif', '.tiff', '.bmp'):
                            continue
                        dst = folder / path.relative_to(orig)
                        dst.parent.mkdir(exist_ok=True, parents=True)
                        with z.open(name) as src, dst.open('wb') as stream:
                            shutil.copyfileobj(src, stream)
                    target = folder / modelpath.name
                else:
                    target = folder / file.name
                    shutil.copyfile(file, target)
                    # FBX texture sidecar and loose textures adjacent to this model.
                    fbm = file.with_suffix('.fbm')
                    if fbm.is_dir():
                        shutil.copytree(fbm, folder / fbm.name)
                    for tex in file.parent.iterdir():
                        if tex.is_file() and tex.suffix.lower() in ('.png', '.jpg', '.jpeg', '.tga', '.tif', '.tiff', '.bmp'):
                            shutil.copyfile(tex, folder / tex.name)
                chars[k]['model'] = str(target.relative_to(out))
        for file in dict.fromkeys(new + sorted(raw.rglob('*'))):
            if file.suffix.lower() == '.fbx' and file.is_file():
                with file.open('rb') as f:
                    h = digest(f)
                accept(h, file.name, file, None)
            elif file.suffix.lower() == '.zip' and file.is_file():
                with zipfile.ZipFile(file) as z:
                    for name in z.namelist():
                        if not name.lower().endswith('.fbx') or '__MACOSX/' in name:
                            continue
                        with z.open(name) as f:
                            h = digest(f)
                        accept(h, name, file, z)
            if all('model' in c for c in chars.values()) and all('file' in c for c in clips.values()):
                break
        missing = [k for k, c in chars.items() if 'model' not in c] + [c['name']
                                                                       for c in clips.values() if 'file' not in c]
        if missing:
            raise ValueError('Missing source models or clips: ' + ', '.join(missing))
        (out / 'plan.json').write_text(json.dumps({'characters': chars,
                                                   'clips': clips}, indent=2, ensure_ascii=False), encoding='utf-8')
        out.rename(final)
    print('Prepared', len(chars), 'characters and', len(clips), 'sources:', final / 'plan.json')


if __name__ == '__main__':
    main()
