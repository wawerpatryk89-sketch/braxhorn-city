"""EC135 -> braxhorn_ec135.glb (+ _tekstury/braxhorn_ec135/*.png): the parts baked static, the rotors on their own
pivots, the pilots and the stretcher out, the BRAXHORN livery on the fuselage, textures sized for the phone"""
import os, sys, io, json, struct
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np
from PIL import Image
import bake, livery

src, sheet, font, out = sys.argv[1:5]
g, parts, piv, s = bake.load(src); js = g.js
texdir = os.path.join(out, '_tekstury', 'braxhorn_ec135'); os.makedirs(texdir, exist_ok=True)
def img(i): return Image.open(io.BytesIO(g.image(js['textures'][i]['source'] if 'source' in js['textures'][i] else i)))
# the owner's emblem: his coin (16.11 20:49 sheet, the bull with BRAXHORN CASINO)
sh = Image.open(sheet).convert('RGBA'); emblem = np.asarray(sh.crop((43, 17, 43 + 319, 17 + 320)).resize((512, 512), Image.LANCZOS), np.float32)
# per material: the size of its maps (base, metal-roughness, normal) on the phone
SIZES = {0: (1024, 512, 1024), 2: (1024, 512, 1024), 3: (2048, 1024, 1024), 4: (1024, 512, 1024), 6: (2048, 1024, 1024), 7: (1024, 512, 512)}
NAMES = {0: 'wirniki', 2: 'fotele', 3: 'kadlub_tyl', 4: 'kabina', 6: 'kadlub', 7: 'kokpit'}
painted = {6: 'kadlub', 3: 'kadlub_tyl'}
mats_out, images, textures = [], [], []
mat_index = {}
def add_image(arr_or_img, name, size, lin):
    im = arr_or_img if isinstance(arr_or_img, Image.Image) else Image.fromarray(arr_or_img)
    im = im.convert('RGB').resize((size, size), Image.LANCZOS)
    fn = 'braxhorn_ec135_%s%s.png' % (name, '__lin' if lin else '')
    im.save(os.path.join(texdir, fn), optimize=True)
    images.append({'uri': '_tekstury/braxhorn_ec135/' + fn, 'mimeType': 'image/png'}); textures.append({'source': len(images) - 1})
    return len(textures) - 1
for mi, m in enumerate(js['materials']):
    used = [p for p in parts.values() if p['mat'] == mi]
    if not used: continue
    pbr = m.get('pbrMetallicRoughness', {}); om = {'name': 'BRAXHORN EC135 ' + NAMES.get(mi, m['name']), 'pbrMetallicRoughness': {}}
    if mi in SIZES:
        bs, ms, ns = SIZES[mi]
        base = img(pbr['baseColorTexture']['index']).convert('RGB'); mr = img(pbr['metallicRoughnessTexture']['index']).convert('RGB')
        nm = img(m['normalTexture']['index']).convert('RGB')
        if mi in painted:
            p = parts[painted[mi]]; W, H = base.size
            pos, nrm, cov = livery.rasterise(p['P'], p['N'], p['UV'], p['I'], W, H)
            mrA = np.asarray(mr.resize((W, H)), np.uint8)
            nb, nmr = livery.paint(np.asarray(base, np.uint8), mrA, pos, nrm, cov, emblem, font, painted[mi])
            base, mr = Image.fromarray(nb), Image.fromarray(nmr)
        elif mi == 4: base = Image.fromarray(livery.recolour(np.asarray(base, np.uint8)))
        om['pbrMetallicRoughness'] = {'baseColorTexture': {'index': add_image(base, NAMES[mi], bs, False)},
                                      'metallicRoughnessTexture': {'index': add_image(mr, NAMES[mi] + '_mr', ms, True)}}
        om['normalTexture'] = {'index': add_image(nm, NAMES[mi] + '_normal', ns, True)}
    else:
        # the glass: its colour and transparency as authored, a smooth surface
        om['pbrMetallicRoughness'] = {'baseColorFactor': [.12, .16, .20, .55], 'metallicFactor': 0.0, 'roughnessFactor': .05}
        om['alphaMode'] = 'BLEND'; om['doubleSided'] = True
    mat_index[mi] = len(mats_out); mats_out.append(om)
# geometry
blob = bytearray(); views, accs, meshes, nodes = [], [], [], []
def put(arr, target=None, minmax=False):
    arr = np.ascontiguousarray(arr); off = len(blob); blob.extend(arr.tobytes())
    while len(blob) % 4: blob.append(0)
    v = {'buffer': 0, 'byteOffset': off, 'byteLength': arr.nbytes}
    if target: v['target'] = target
    views.append(v)
    ct = {np.dtype(np.float32): 5126, np.dtype(np.uint32): 5125, np.dtype(np.uint16): 5123}[arr.dtype]
    tp = {1: 'SCALAR', 2: 'VEC2', 3: 'VEC3'}[1 if arr.ndim == 1 else arr.shape[1]]
    a = {'bufferView': len(views) - 1, 'componentType': ct, 'count': len(arr), 'type': tp}
    if minmax: a['min'] = arr.min(0).tolist(); a['max'] = arr.max(0).tolist()
    accs.append(a); return len(accs) - 1
order = ['kadlub', 'kadlub_tyl', 'kabina', 'szyby', 'kokpit', 'fotel_pilota', 'fotel_pilota_2', 'fotele_tyl', 'wirnik_glowny', 'wirnik_ogonowy']
children = []
for name in order:
    p = parts[name]; P = p['P'].copy(); t = [0.0, 0.0, 0.0]
    if name in piv: P -= piv[name]; t = piv[name].tolist()
    idx = p['I'].astype(np.uint32 if len(P) > 65535 else np.uint16)
    prim = {'attributes': {'POSITION': put(P.astype(np.float32), 34962, True), 'NORMAL': put(p['N'].astype(np.float32), 34962),
                           'TEXCOORD_0': put(p['UV'].astype(np.float32), 34962)}, 'indices': put(idx, 34963), 'material': mat_index[p['mat']]}
    meshes.append({'name': name, 'primitives': [prim]})
    nodes.append({'name': name, 'mesh': len(meshes) - 1, 'translation': t}); children.append(len(nodes) - 1)
nodes.append({'name': 'BRAXHORN EC135', 'children': children})
gl = {'asset': {'version': '2.0', 'generator': 'BRAXHORN helikopter (unity-tools/mistrzowie/helikopter)',
                'extras': {'title': 'EC 135', 'author': 'GRIP420 (https://sketchfab.com/GRIP420)', 'license': 'CC-BY-4.0',
                           'source': 'https://sketchfab.com/3d-models/ec-135-ec30f802c6ac43a8843948fbadb674d2',
                           'changes': 'rescue crew and stretcher removed, parts baked static, rotors on pivots, metres, BRAXHORN livery (ADAC/EU/Eurocopter marks removed)'}},
      'scene': 0, 'scenes': [{'nodes': [len(nodes) - 1]}], 'nodes': nodes, 'meshes': meshes, 'materials': mats_out,
      'textures': textures, 'images': images, 'samplers': [{'magFilter': 9729, 'minFilter': 9987, 'wrapS': 10497, 'wrapT': 10497}],
      'accessors': accs, 'bufferViews': views, 'buffers': [{'byteLength': len(blob)}]}
for t in gl['textures']: t['sampler'] = 0
j = json.dumps(gl, separators=(',', ':')).encode()
while len(j) % 4: j += b' '
with open(os.path.join(out, 'braxhorn_ec135.glb'), 'wb') as f:
    f.write(struct.pack('<4sII', b'glTF', 2, 12 + 8 + len(j) + 8 + len(blob)))
    f.write(struct.pack('<I4s', len(j), b'JSON')); f.write(j); f.write(struct.pack('<I4s', len(blob), b'BIN\x00')); f.write(bytes(blob))
print('tris', sum(len(parts[n]['I']) // 3 for n in order), 'pivots', {k: [round(x, 3) for x in v] for k, v in piv.items()}, 'images', len(images))
