"""Extract exact existing house PBR maps from the secured original export.
Source bytes are verified before making 2K runtime copies. No library substitutes.
Run BEFORE city/prepare.py (which externalizes the GLB).
"""
import hashlib, io, json, pathlib, struct
import numpy as np
from PIL import Image

ROOT = pathlib.Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'unity-city/Assets/Source/City/braxhorn-estate.glb'
OUT = ROOT / 'unity-city/Assets/Source/City/R2Segment'
OUT.mkdir(parents=True, exist_ok=True)
EXPECTED = json.loads((ROOT / 'unity-tools/city/r2-segment-source-hashes.json').read_text())
with SOURCE.open('rb') as f:
    assert f.read(4) == b'glTF'
    f.seek(12)
    length = struct.unpack('<I', f.read(4))[0]
    f.read(4)
    gltf = json.loads(f.read(length))
    base = 28 + length
    records = []
    for role in (108, 113, 117):
        material = next(m for m in gltf['materials'] if m['name'] == f'estate_lit_{role}')
        pbr = material['pbrMetallicRoughness']
        for label, slot in [('Albedo', pbr['baseColorTexture']), ('Normal', material['normalTexture']), ('MR', pbr['metallicRoughnessTexture'])]:
            image = gltf['images'][gltf['textures'][slot['index']]['source']]
            view = gltf['bufferViews'][image['bufferView']]
            f.seek(base + view.get('byteOffset', 0))
            raw = f.read(view['byteLength'])
            sha = hashlib.sha256(raw).hexdigest()
            assert sha == EXPECTED[str(role)][label], (role, label, sha)
            source_dir = ROOT / 'r2-source-archive'
            source_dir.mkdir(exist_ok=True)
            (source_dir / f'{role}-{label}.bin').write_bytes(raw)
            with Image.open(io.BytesIO(raw)) as im:
                im = im.convert('RGB')
                im.thumbnail((2048, 2048), Image.Resampling.LANCZOS)
                if label == 'MR':
                    # glTF G=roughness; Unity Standard A=smoothness. Nonmetal plaster/brick/stone.
                    rough = np.asarray(im)[:, :, 1].astype(np.float32) / 255
                    data = np.zeros((im.height, im.width, 4), np.uint8)
                    data[:, :, 3] = np.rint((1 - np.maximum(rough, .72)) * 255).astype(np.uint8)
                    im = Image.fromarray(data)
                target = OUT / f'{role}{label}.png'
                im.save(target)
            records.append(dict(role=role, slot=label, sourceSha256=sha, runtimeSha256=hashlib.sha256(target.read_bytes()).hexdigest(), runtimePath=str(target.relative_to(ROOT))))
report = dict(status='SOURCE_MAPS_VERIFIED__UNITY_BINDINGS_PENDING', originalExportSha256=hashlib.sha256(SOURCE.read_bytes()).hexdigest(), maps=records, sceneApproved=False)
(ROOT / 'city-review').mkdir(exist_ok=True)
(ROOT / 'city-review/r2-segment-sources.json').write_text(json.dumps(report, indent=2))
print('R2_SEGMENT_EXACT_ORIGINAL_PBR_MAPS_VERIFIED', len(records))
