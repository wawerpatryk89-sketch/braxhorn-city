#!/usr/bin/env python3
"""BRAXHORN sklep_sprzet_01: the heavy bag without its lettering (owner 12.11.2026 13:41: "ten napis 3D z worka trzeba
wyciąć, ale żeby nie było śladu po nim").

The lettering and its plate are not geometry: they are painted and embossed in the pack's shared texture atlas, on the
bag's own UV islands. Every texel of the bag that lies in the lettering's height band on the model (0.575-0.675 m) gets a
flat normal and the leather's metal / roughness; the letters and the plate's dark outline (texels far from the leather's
brightness, plus 3 texels round them) get the leather's colour. Loose slivers of the model in that band are dropped.
No other model of the atlas is touched (each island belongs to one model).

usage: worek_bez_napisu.py <folder with worek_bokserski.glb and _tekstury/> [y0 y1]     (needs numpy, pillow, trimesh)
The pack is then rebuilt as before: tar -> openssl cms -encrypt -aes256 (DER) to recipient.crt, manifest.json hashes.
Never run in CI and never on anything but a decrypted copy of the pack."""
import json, struct, sys
import numpy as np
from PIL import Image
import trimesh

base = sys.argv[1].rstrip('/') + '/'
Y0, Y1 = (float(sys.argv[2]), float(sys.argv[3])) if len(sys.argv) > 3 else (.56, .69)
raw = open(base + 'worek_bokserski.glb', 'rb').read()
jl = struct.unpack_from('<I', raw, 12)[0]; j = json.loads(raw[20:20 + jl]); bin0 = bytearray(raw[20 + jl + 8:])


def acc(i, dt, n):
    a = j['accessors'][i]; bv = j['bufferViews'][a['bufferView']]
    return np.frombuffer(bytes(bin0), dtype=dt, count=a['count'] * n, offset=bv['byteOffset'] + a.get('byteOffset', 0)).reshape(-1, n)


P = acc(0, '<f4', 3); UV = acc(2, '<f4', 2); I = acc(3, '<u2', 1).reshape(-1, 3)
mm = trimesh.Trimesh(P.copy(), I.copy(), process=False); mm.merge_vertices()
labels = trimesh.graph.connected_component_labels(mm.face_adjacency, node_count=len(mm.faces)); main = np.bincount(labels).argmax()
col = np.asarray(Image.open(base + '_tekstury/sklep_sprzet_01_kolor.jpg').convert('RGB')).copy()
nor = np.asarray(Image.open(base + '_tekstury/sklep_sprzet_01_normal.png').convert('RGB')).copy()
mr = np.asarray(Image.open(base + '_tekstury/sklep_sprzet_01_mr.png').convert('RGB')).copy()
H, W = col.shape[:2]
mask = np.zeros((H, W), bool); leather = np.zeros((H, W), bool); whole = np.zeros((H, W), bool)
for f, (a, b, c) in enumerate(I):
    ys = P[[a, b, c], 1]
    if ys.max() < Y0 - .08 or ys.min() > Y1 + .08: continue
    uv = UV[[a, b, c]] * [W, H]
    x0, y0 = np.floor(uv.min(0)).astype(int) - 1; x1, y1 = np.ceil(uv.max(0)).astype(int) + 1
    x0, y0, x1, y1 = max(x0, 0), max(y0, 0), min(x1, W - 1), min(y1, H - 1)
    if x1 < x0 or y1 < y0: continue
    gx, gy = np.meshgrid(np.arange(x0, x1 + 1) + .5, np.arange(y0, y1 + 1) + .5)
    d = (uv[1, 1] - uv[2, 1]) * (uv[0, 0] - uv[2, 0]) + (uv[2, 0] - uv[1, 0]) * (uv[0, 1] - uv[2, 1])
    if abs(d) < 1e-9: continue
    w0 = ((uv[1, 1] - uv[2, 1]) * (gx - uv[2, 0]) + (uv[2, 0] - uv[1, 0]) * (gy - uv[2, 1])) / d
    w1 = ((uv[2, 1] - uv[0, 1]) * (gx - uv[2, 0]) + (uv[0, 0] - uv[2, 0]) * (gy - uv[2, 1])) / d
    w2 = 1 - w0 - w1
    inside = (w0 >= -.15) & (w1 >= -.15) & (w2 >= -.15)                 # a texel's margin round the island (bilinear bleed)
    y3 = w0 * ys[0] + w1 * ys[1] + w2 * ys[2]
    band = inside & (y3 >= Y0) & (y3 <= Y1)
    lum = col[y0:y1 + 1, x0:x1 + 1].astype(float) @ np.array([.299, .587, .114])
    mask[y0:y1 + 1, x0:x1 + 1] |= band & ((lum > 40) | (lum < 23))      # the letters and the plate's dark outline
    leather[y0:y1 + 1, x0:x1 + 1] |= band & (lum >= 26) & (lum <= 36)
    whole[y0:y1 + 1, x0:x1 + 1] |= band
# grow the letters' mask by 2 texels inside the band's islands (anti-aliased edges)
grown = mask.copy()
for _ in range(3):
    g = grown.copy(); g[1:] |= grown[:-1]; g[:-1] |= grown[1:]; g[:, 1:] |= grown[:, :-1]; g[:, :-1] |= grown[:, 1:]; grown = g
grown &= whole | mask
def at(tex, m):
    """the colour-resolution mask m at the resolution of tex (the normal and metal/roughness maps are smaller)"""
    h, w = tex.shape[:2]
    if (h, w) == m.shape: return m
    return m.reshape(h, m.shape[0] // h, w, m.shape[1] // w).any(axis=(1, 3))


lc = np.median(col[leather], axis=0); lm = np.median(mr[at(mr, leather) & ~at(mr, mask)], axis=0)
rng = np.random.default_rng(7); n = int(grown.sum())
col[grown] = np.clip(lc + rng.normal(0, 2.0, (n, 1)), 0, 255).astype(np.uint8)
# the embossing lives in the normal and metal/roughness maps: the whole band is plain leather there
nor[at(nor, whole | grown)] = (128, 128, 255); mr[at(mr, whole | grown)] = lm.astype(np.uint8)
print('letters texels', int(mask.sum()), 'repainted', n, 'leather colour', lc, 'mr', lm)
Image.fromarray(col).save(base + '_tekstury/sklep_sprzet_01_kolor.jpg', quality=95)
Image.fromarray(nor).save(base + '_tekstury/sklep_sprzet_01_normal.png'); Image.fromarray(mr).save(base + '_tekstury/sklep_sprzet_01_mr.png')
# the letters' loose slivers: small components whose centre lies in the band (a little wider: their own thickness)
C = P[I].mean(1); drop = np.zeros(len(I), bool)
for lab in np.unique(labels):
    if lab == main: continue
    fs = labels == lab; cy = C[fs, 1].mean()
    if Y0 - .02 <= cy <= Y1 + .02: drop |= fs
keep = I[~drop].astype('<u2')
a = j['accessors'][3]; bv = j['bufferViews'][a['bufferView']]; off = bv['byteOffset'] + a.get('byteOffset', 0)
bin0[off:off + keep.size * 2] = keep.tobytes(); a['count'] = int(keep.size)
print('faces', len(I), '->', len(keep), 'dropped slivers', int(drop.sum()))
js = json.dumps(j, separators=(',', ':')).encode(); js += b' ' * (-len(js) % 4)
out = struct.pack('<4sII', b'glTF', 2, 12 + 8 + len(js) + 8 + len(bin0)) + struct.pack('<I4s', len(js), b'JSON') + js + struct.pack('<I4s', len(bin0), b'BIN\0') + bytes(bin0)
open(base + 'worek_bokserski.glb', 'wb').write(out)
