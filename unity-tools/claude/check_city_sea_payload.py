"""Reject the build-58 missing-field defect in a completed city APK before upload.

This is structural validation of the actual player data, not a gameplay test.
It supports both the legacy loose Android player files and Unity's LZ4
``data.unity3d`` archive created by BuildOptions.CompressWithLz4.

Requires the pinned UnityPy version used by the Android serialization regression.
Compressed players additionally require the pinned official UnityDataTool supplied
by CI through ``UNITY_DATA_TOOL``.
"""
import json
import math
import os
from pathlib import Path
import re
import shutil
import struct
import subprocess
import sys
import tempfile
import zipfile
import UnityPy


PLAYER_FILES = ("level1", "globalgamemanagers.assets")


def decode_payload(data):
    if len(data) != 68:
        raise ValueError(f'BraxMorzeZaKamera has {len(data)} bytes; expected 68 with four serialized fields')
    material_file, material_id, cell, half_grid, x, y, width, height = struct.unpack('<iqfi4f', data[32:])
    if material_id == 0 or not all(math.isfinite(v) for v in (cell, x, y, width, height)) or cell <= 0 or half_grid <= 0 or width <= 0 or height <= 0:
        raise ValueError('Sea settings/material reference are invalid')
    return dict(bytes=len(data), material_file=material_file, material_id=material_id,
                cell=cell, half_grid=half_grid, port_rectangle=[x, y, width, height])


def copy_zip_parts(apk, destination, name):
    parts = []
    pattern = re.compile(r'assets/bin/Data/' + re.escape(name) + r'(?:\.split(\d+))?')
    for item in apk.infolist():
        match = pattern.fullmatch(item.filename)
        if match:
            parts.append((int(match[1]) if match[1] is not None else -1, item))
    if not parts:
        return False
    parts.sort(key=lambda item: item[0])
    indices = [number for number, _ in parts]
    if indices != [-1] and indices != list(range(len(parts))):
        raise ValueError('Incomplete city player data: ' + name)
    destination.parent.mkdir(parents=True, exist_ok=True)
    with destination.open('wb') as out:
        for _, item in parts:
            with apk.open(item) as source:
                shutil.copyfileobj(source, out, length=4 * 1024 * 1024)
    return True


def find_extracted(root, name):
    direct = root / name
    if direct.is_file():
        return direct
    matches = [path for path in root.rglob(name) if path.is_file() and path.name == name]
    if len(matches) != 1:
        raise ValueError(f'UnityDataTool extracted {len(matches)} candidates for {name}; expected exactly one player file')
    return matches[0]


def extract_archive_file(tool, archive_path, extracted_root, name):
    result = subprocess.run(
        [tool, 'archive', 'extract', str(archive_path), '-o', str(extracted_root), '--filter', name],
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        text=True,
        check=False,
    )
    if result.returncode:
        tail = (result.stdout or '')[-4000:]
        raise ValueError(f'UnityDataTool failed extracting {name} from data.unity3d: {tail}')


def extract_compressed_player(apk, workspace, player_root):
    archive_path = workspace / 'data.unity3d'
    if not copy_zip_parts(apk, archive_path, 'data.unity3d'):
        raise ValueError('Missing city player data: level1/globalgamemanagers.assets and data.unity3d')
    tool = os.environ.get('UNITY_DATA_TOOL') or shutil.which('UnityDataTool')
    if not tool or not Path(tool).is_file():
        raise ValueError('Compressed city player requires pinned UnityDataTool; UNITY_DATA_TOOL is missing')
    extracted_root = workspace / 'archive'
    extracted_root.mkdir(parents=True, exist_ok=True)
    for name in PLAYER_FILES:
        extract_archive_file(tool, archive_path, extracted_root, name)
        shutil.copy2(find_extracted(extracted_root, name), player_root / name)
    return 'data.unity3d'


def inspect(apk_path):
    with tempfile.TemporaryDirectory(prefix='city-apk-check-') as tmp:
        workspace = Path(tmp)
        player_root = workspace / 'player'
        player_root.mkdir()
        with zipfile.ZipFile(apk_path) as apk:
            loose = [copy_zip_parts(apk, player_root / name, name) for name in PLAYER_FILES]
            if all(loose):
                storage = 'loose-player-files'
            else:
                for name in PLAYER_FILES:
                    (player_root / name).unlink(missing_ok=True)
                storage = extract_compressed_player(apk, workspace, player_root)
        env = UnityPy.load(str(player_root))
        matches = []
        for obj in env.objects:
            if Path(obj.assets_file.name).name != 'level1' or obj.type.name != 'MonoBehaviour':
                continue
            head = obj.parse_monobehaviour_head()
            script = head.m_Script.deref_parse_as_object()
            if script.m_ClassName != 'BraxMorzeZaKamera':
                continue
            matches.append(dict(path_id=obj.path_id, **decode_payload(obj.get_raw_data())))
        if len(matches) != 1:
            raise ValueError(f'Expected one city sea component, found {len(matches)}')
        return dict(check='city-sea-payload', valid=True, storage=storage, components=matches,
                    scope='APK serialization, not device runtime validation')


if __name__ == '__main__':
    try:
        result = inspect(sys.argv[1])
    except Exception as exc:
        print(json.dumps(dict(check='city-sea-payload', valid=False, error=str(exc))))
        sys.exit(1)
    print(json.dumps(result, indent=2))
