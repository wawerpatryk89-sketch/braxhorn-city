"""Prepare the owner's five pedestrian walk choices together on one T-pose review model."""
import argparse
import hashlib
import json
from pathlib import Path
import sys
from tempfile import TemporaryDirectory
from zipfile import ZipFile

import bpy

sys.path.insert(0, str(Path(__file__).resolve().parent))
import spacer_mixamo as spacer


def prepare(model, archive, result):
    if model.suffix.lower() not in {'.fbx', '.blend'} or result.suffix.lower() != '.blend' or result.exists():
        raise ValueError('Podaj model FBX lub BLEND i nowy plik wynikowy BLEND')
    catalog = Path(__file__).resolve().parents[2] / 'Postacie/ruchy/chody_T_wlasciciela.json'
    selection = json.loads(catalog.read_text())
    with archive.open('rb') as source:
        if hashlib.file_digest(source, 'sha256').hexdigest() != selection['archive_sha256']:
            raise ValueError('To nie jest wskazana paczka pięciu chodów')
    with TemporaryDirectory() as temp, ZipFile(archive) as package:
        temp = Path(temp)
        actions = []
        for index, clip in enumerate(selection['clips']):
            raw = package.read(clip['file'])
            if hashlib.sha256(raw).hexdigest() != clip['sha256']:
                raise ValueError('Błędna suma kontrolna ' + clip['file'])
            local = temp / 'source.fbx'
            local.write_bytes(raw)
            source = spacer.read_source(local)
            project = temp / f'variant-{index}.blend'
            name = Path(clip['file']).stem
            spacer.apply_walk(model, project, source, clip['sha256'])
            action = bpy.data.actions['Walk']
            action.name = name
            if action.name != name:
                raise ValueError('W modelu istnieje już akcja ' + name)
            action['file'] = clip['file']
            bpy.context.preferences.filepaths.save_version = 0
            bpy.ops.wm.save_as_mainfile(filepath=str(project), compress=True)
            actions.append((project, name))
        bpy.ops.wm.open_mainfile(filepath=str(actions[0][0]))
        for project, name in actions[1:]:
            with bpy.data.libraries.load(str(project)) as (_, target):
                target.actions = [name]
            target.actions[0].use_fake_user = True
        result.parent.mkdir(parents=True, exist_ok=True)
        bpy.ops.wm.save_as_mainfile(filepath=str(result), compress=True)


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('model', type=Path)
    parser.add_argument('paczka', type=Path)
    parser.add_argument('wynik', type=Path)
    args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
    prepare(args.model.resolve(), args.paczka.resolve(), args.wynik.resolve())
