"""Extract exact existing house PBR maps from the secured original export.
Source bytes are verified before making 2K runtime copies. No library substitutes.
Run BEFORE city/prepare.py (which externalizes the GLB).
"""
import hashlib, io, json, pathlib, struct
import numpy as np
from PIL import Image
import os, urllib.request, zipfile
os.environ['OPENCV_IO_ENABLE_OPENEXR'] = '1'
import cv2

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

# Exact named owner-library sets, fetched as fresh official CC0 source copies.
# Their upload-archive hashes were not recorded in the previous eight-file audit:
# do not claim these bytes were transported from the owner's private Library.
def download(url, expected, filename):
    request = urllib.request.Request(url, headers={'User-Agent': 'Braxhorn-R2-verified-PBR/1.0'})
    with urllib.request.urlopen(request, timeout=180) as response:
        data = response.read()
    actual = hashlib.sha256(data).hexdigest()
    if actual != expected:
        raise RuntimeError('Official source hash mismatch: ' + filename + ' ' + actual)
    (source_dir / filename).write_bytes(data)
    return data

def save_map(key, slot, data, exr=False):
    if exr:
        array = cv2.imdecode(np.frombuffer(data, np.uint8), cv2.IMREAD_UNCHANGED)
        if array is None:
            raise RuntimeError('Real EXR normal map failed to decode')
        array = cv2.cvtColor(array, cv2.COLOR_BGR2RGB)
        im = Image.fromarray(np.rint(np.clip(array, 0, 1) * 255).astype(np.uint8))
    else:
        im = Image.open(io.BytesIO(data)).convert('RGB')
    im.thumbnail((2048, 2048), Image.Resampling.LANCZOS)
    if slot == 'MR':
        rough = np.asarray(im)[:, :, 0].astype(np.float32) / 255
        rgba = np.zeros((im.height, im.width, 4), np.uint8)
        rgba[:, :, 3] = np.rint((1 - np.maximum(rough, .75)) * 255).astype(np.uint8)
        im = Image.fromarray(rgba)
    dest = OUT / (key + slot + '.png')
    im.save(dest)
    records.append(dict(set=key, slot=slot, sourceSha256=hashlib.sha256(data).hexdigest(), runtimeSha256=hashlib.sha256(dest.read_bytes()).hexdigest(), runtimePath=str(dest.relative_to(ROOT))))

for slot, filename, ext, sha in [
    ('Albedo','white_stucco_02_diff_4k.jpg','jpg','94e56a2173851458eab45c7d54ae024b850e7deeb33059dad9fc836c8d3069e7'),
    ('Normal','white_stucco_02_nor_gl_4k.exr','exr','a91d368af8d7d9072dcdd5c0da84af50ac2f25e46c68703ec26358c83de1b9f8'),
    ('MR','white_stucco_02_rough_4k.jpg','jpg','060ce3a82aeb97a7afaad14e91859ea418cf41d82eda2b3158e62f9d6719f6f7'),
]:
    url = f'https://dl.polyhaven.org/file/ph-assets/Textures/{ext}/4k/white_stucco_02/{filename}'
    save_map('Stucco', slot, download(url, sha, filename), ext == 'exr')

paving_url = 'https://ambientcg.com/get?file=PavingStones151_2K-JPG.zip'
paving_sha = '41965f7604eda1de8866c68682815fa5c908bdc60e855a1ca3ec0991f0c00b7e'
archive = download(paving_url, paving_sha, 'PavingStones151_2K-JPG.zip')
with zipfile.ZipFile(io.BytesIO(archive)) as package:
    if package.testzip() is not None:
        raise RuntimeError('PavingStones151 CRC failure')
    for slot, suffix in [('Albedo','Color'),('Normal','NormalGL'),('MR','Roughness'),('AO','AmbientOcclusion')]:
        save_map('Paving', slot, package.read(f'PavingStones151_2K-JPG_{suffix}.jpg'))
report['maps'] = records
report['officialCC0Sources'] = [dict(set='white_stucco_02',source='https://polyhaven.com/a/white_stucco_02',license='https://polyhaven.com/license'),dict(set='PavingStones151',source='https://ambientcg.com/a/PavingStones151',license='https://docs.ambientcg.com/license/',archiveSha256=paving_sha,physicalRepeatMetres=5.4)]
report['ownerLibraryArchiveHashComparison'] = 'NOT_AVAILABLE_FOR_THESE_TWO_SETS; newly verified official source copies, not private Library transport'
(ROOT / 'city-review/r2-segment-sources.json').write_text(json.dumps(report, indent=2))
print('R2_NAMED_OFFICIAL_CC0_SOURCE_COPIES_VERIFIED', len(records))

for key, asset, maps in [
    ('WhiteBrick','whitewashed_brick',[
        ('Albedo','diff','jpg','c37b6e9c560b9aad8aadee031696f9b89414f3e619a30e2c9c844f8ac0402ad6'),
        ('Normal','nor_gl','exr','30f5c306cfefbbab52b5236a0a6f27f1df4e9741997abccf1698e2553ce889d8'),
        ('MR','rough','jpg','03c17afc9a60cf51999ac2f40b5540a2840d4b8e28f89db7e39d0ce48bb50c30')]),
    ('RedBrick','red_brick',[
        ('Albedo','diff','jpg','85b7a5eadd14766126069a6ee0033873cbe96a4a09246e8d21226613028b739c'),
        ('Normal','nor_gl','exr','df31f41ebf211789d8ff27c92dd866681639ad6ff7ece8100109a94cbc5f0eae'),
        ('MR','rough','jpg','eb84e52a2d07c7d7c195ccc4556668972fa540d381fcc828e8d485ddf5c52484')]),
]:
    for slot, kind, ext, sha in maps:
        name=f'{asset}_{kind}_4k.{ext}'
        url=f'https://dl.polyhaven.org/file/ph-assets/Textures/{ext}/4k/{asset}/{name}'
        save_map(key,slot,download(url,sha,name),ext=='exr')
    report['officialCC0Sources'].append(dict(set=asset,source='https://polyhaven.com/a/'+asset,license='https://polyhaven.com/license'))
report['maps']=records
(ROOT/'city-review/r2-segment-sources.json').write_text(json.dumps(report,indent=2))
import runpy
runpy.run_path(str(ROOT/'unity-tools/city/build_dom03_selection.py'),run_name='__main__')
