#!/usr/bin/env python3
"""Verify the owner artwork bytes before the costly Unity import/build."""
import hashlib
import json
from pathlib import Path
from PIL import Image

ROOT=Path(__file__).resolve().parents[2]
manifest=json.loads((ROOT/'unity-city/Docs/AI/CityBrandSources.json').read_text())
for item in manifest['items']:
    path=ROOT/item['path']
    data=path.read_bytes()
    if len(data)!=item['bytes'] or hashlib.sha256(data).hexdigest()!=item['sha256']:
        raise ValueError('Owner artwork bytes changed: '+item['path'])
    with Image.open(path) as image:
        dimensions=image.size
        image.verify()
    meta=path.with_name(path.name+'.meta').read_text()
    if 'TextureImporter:' not in meta or 'textureShape: 1' not in meta:
        raise ValueError('Explicit 2D texture importer is missing: '+str(path))
    print('OWNER_ARTWORK_VERIFIED',path.name,len(data),dimensions,item['sha256'])
