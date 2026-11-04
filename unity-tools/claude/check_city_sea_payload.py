"""Reject the build-58 missing-field defect in a completed city APK before upload.

This is structural validation of the actual player data, not a gameplay test.
Requires the pinned UnityPy version used by the Android serialization regression.
"""
import json
import math
from pathlib import Path
import re
import struct
import sys
import tempfile
import zipfile
import UnityPy


def decode_payload(data):
    if len(data) != 68:
        raise ValueError(f'BraxMorzeZaKamera has {len(data)} bytes; expected 68 with four serialized fields')
    material_file, material_id, cell, half_grid, x, y, width, height = struct.unpack('<iqfi4f', data[32:])
    if material_id == 0 or not all(math.isfinite(v) for v in (cell, x, y, width, height)) or cell <= 0 or half_grid <= 0 or width <= 0 or height <= 0:
        raise ValueError('Sea settings/material reference are invalid')
    return dict(bytes=len(data), material_file=material_file, material_id=material_id,
                cell=cell, half_grid=half_grid, port_rectangle=[x, y, width, height])


def inspect(apk_path):
    with tempfile.TemporaryDirectory(prefix='city-apk-check-') as tmp:
        root = Path(tmp)
        with zipfile.ZipFile(apk_path) as apk:
            for name in ('level1', 'globalgamemanagers.assets'):
                parts = []
                for item in apk.infolist():
                    match = re.fullmatch(r'assets/bin/Data/' + re.escape(name) + r'(?:\.split(\d+))?', item.filename)
                    if match:
                        parts.append((int(match[1]) if match[1] is not None else -1, item))
                if not parts:
                    raise ValueError('Missing city player data: ' + name)
                parts.sort(key=lambda item: item[0])
                indices = [n for n, _ in parts]
                if indices != [-1] and indices != list(range(len(parts))):
                    raise ValueError('Incomplete city player data: ' + name)
                with (root / name).open('wb') as out:
                    for _, item in parts:
                        out.write(apk.read(item))
        env = UnityPy.load(str(root))
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
        return dict(check='city-sea-payload', valid=True, components=matches,
                    scope='APK serialization, not device runtime validation')


if __name__ == '__main__':
    try:
        result = inspect(sys.argv[1])
    except Exception as exc:
        print(json.dumps(dict(check='city-sea-payload', valid=False, error=str(exc))))
        sys.exit(1)
    print(json.dumps(result, indent=2))
