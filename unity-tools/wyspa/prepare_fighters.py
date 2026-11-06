"""Copy RAKK (owner's Pitbull) and ZYRO (owner's Doberman) from the pinned camp delivery; never restore camp scenery.

No Unity invocation. Plaintext models/animations stay in ignored private Assets.
Existing encrypted release inputs and hashes are reused without publishing assets.
"""
import argparse
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tarfile
import tempfile

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'unity-tools/oboz'))
import assets
import motion_candidates

IDS = ('rakk', 'zyro')


def selection():
    manifest = json.loads((ROOT / 'unity-city/Assets/Editor/Oboz/CharacterManifest.json').read_text())
    data = json.loads((ROOT / 'unity-tools/oboz/private-inputs.json').read_text())
    motion = json.loads((ROOT / 'unity-tools/oboz/motion-candidates.json').read_text())
    assets.validate_inventory(data)
    motion_candidates.validate(motion)
    characters = [next(c for c in manifest['characters'] if c['id'] == key) for key in IDS]
    names = set()
    prefix = 'Assets/Source/Prywatne/Oboz/'
    for character in characters:
        # Owner's Pitbull (RAKK from pitbul.zip) has no gloves in the
        # signed source manifest. Do not swap him for a different fighter or
        # invent glove geometry. Doberman/ZYRO has confirmed gloves.
        if not character['glovesVerified']:
            raise ValueError('Fighter glove source review not complete: ' + character['id'])
        if character['id'] == 'zyro' and not character['hasGloves']:
            raise ValueError('Doberman model was expected to retain its own gloves')
        paths = [character[k] for k in ('modelPath', 'colorPath', 'normalPath', 'roughnessPath', 'metallicPath')]
        paths += [c['path'] for c in character['clips']]
        for path in paths:
            if not path.startswith(prefix):
                raise ValueError('Fighter source outside camp inventory')
            name = path[len(prefix):]
            assets.safe(assets.PRIVATE, name)
            names.add(name)
    records = {r['path']: r for r in data['files'] + motion['files']}
    if not names <= set(records):
        raise ValueError('Untracked fighter input: ' + str(names - set(records)))
    selected = {name: records[name] for name in sorted(names)}
    # The original camp packer uses ordered 180 MiB groups. Reproduce its index,
    # so only packages containing these two fighters are downloaded.
    groups, batch, size = [], [], 0
    for record in data['files']:
        if batch and size + record['bytes'] > 180 * 1024 * 1024:
            groups.append(batch); batch, size = [], 0
        batch.append(record); size += record['bytes']
    if batch:
        groups.append(batch)
    if len(groups) != len(data['packages']):
        raise ValueError('Camp package index changed')
    chosen = [(pack, group) for pack, group in zip(data['packages'], groups)
              if any(r['path'] in selected for r in group)]
    return data, motion, selected, chosen


def verify(records, root):
    assets.verify_sources(dict(schema=1, zone='oboz', files=list(records.values())), root)


def extract_selected(archive, stage, group, selected, seen):
    expected = {r['path']: r for r in group}
    members = set()
    with tarfile.open(archive, 'r:gz') as tar:
        for member in tar:
            if not member.isfile() or member.name not in expected or member.name in members:
                raise ValueError('Unexpected/duplicate camp package member')
            members.add(member.name)
            record = expected[member.name]
            if member.size != record['bytes']:
                raise ValueError('Changed camp source size')
            if member.name not in selected:
                continue
            if member.name in seen:
                raise ValueError('Duplicate selected source')
            path = assets.safe(stage, member.name); path.parent.mkdir(parents=True, exist_ok=True)
            with tar.extractfile(member) as source, path.open('xb') as output:
                shutil.copyfileobj(source, output)
            if assets.digest(path) != record['sha256']:
                raise ValueError('Changed camp source bytes')
            seen.add(member.name)
        if members != set(expected):
            raise ValueError('Incomplete original camp package')


def restore(local_source=None):
    data, motion, selected, chosen = selection()
    try:
        verify(selected, assets.PRIVATE)
        print('BRAXHORN_WYSPA_FIGHTERS cached=verified characters=rakk,zyro'); return
    except (ValueError, OSError):
        pass
    with tempfile.TemporaryDirectory(prefix='wyspa-fighters-') as tmp:
        folder = Path(tmp); stage = folder / 'selected'; stage.mkdir()
        if local_source:
            verify(selected, local_source)
            for name in selected:
                target = assets.safe(stage, name); target.parent.mkdir(parents=True, exist_ok=True)
                shutil.copyfile(assets.safe(local_source, name), target)
        else:
            assets.require_delivery(data)
            repo = os.environ.get('GITHUB_REPOSITORY', 'wawerpatryk89-sketch/braxhorn-city')
            seen = set()
            for pack, group in chosen:
                subprocess.run(['gh', 'release', 'download', data['releaseTag'], '--repo', repo,
                                '--pattern', pack['name'], '--dir', str(folder)], check=True)
                archive = folder / pack['name']
                if archive.stat().st_size != pack['bytes'] or assets.digest(archive) != pack['sha256']:
                    raise ValueError('Encrypted camp package checksum mismatch')
                with archive.open('rb') as stream:
                    if stream.read(8) != b'Salted__':
                        raise ValueError('Camp package is not encrypted')
                plain = folder / 'sources.tar.gz'
                subprocess.run(assets.OPENSSL + ['-d', '-in', str(archive), '-out', str(plain), '-pass', 'env:KLUCZ'], check=True)
                extract_selected(plain, stage, group, selected, seen)
                archive.unlink(); plain.unlink()
            # Reuse exactly the calibrated Idle/Walk/Run used by the current camp.
            # Temporarily target staging so a failed restore cannot install half a set.
            original = assets.PRIVATE
            try:
                assets.PRIVATE = stage
                motion_candidates.restore(motion)
            finally:
                assets.PRIVATE = original
        verify(selected, stage)
        for name in selected:
            target = assets.safe(assets.PRIVATE, name); target.parent.mkdir(parents=True, exist_ok=True)
            os.replace(assets.safe(stage, name), target)
    print('BRAXHORN_WYSPA_FIGHTERS characters=kade,zyro verified=' + str(len(selected)))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check', action='store_true', help='Validate manifests only; no downloads or Unity')
    parser.add_argument('--source-root', type=Path, help='Already restored private camp source folder')
    args = parser.parse_args()
    if args.check:
        _, _, selected, packs = selection()
        print(json.dumps(dict(characters=list(IDS), files=len(selected), source_bytes=sum(r['bytes'] for r in selected.values()),
                              encrypted_packages=[p['name'] for p, _ in packs], unity_run=False)))
    else:
        restore(args.source_root)
