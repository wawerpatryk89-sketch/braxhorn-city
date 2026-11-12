#!/usr/bin/env python3
"""BRAXHORN — flat glass on the owner's Tripo laptops and tablets (owner 12.11.2026 15:24 "laptopy to ja nie wiem, czy się
nadają ... wszystko sprawdzaj, poprawiaj"). The screens are geometrically flat (within 2 mm) but Tripo baked a lumpy relief
into the normal map over them: seen head-on under a light the glass shows wavy blotches, like a crumpled foil.

For each named model the screen is found as the faces above its base that look forward and lie in one plane (within
3 mm of the plane fitted to them, at most 15 degrees off it - a case's bumpers and a keyboard folio stay out). Their texels
in the shared normal map become flat (128, 128, 255) and their vertex normals the plane's normal. Positions, UVs, colours
and every other model of the atlas stay as they are.

usage: ekran_plaski.py <decrypted pack folder> <model>[:lowest share of the height[:plane tolerance mm[:tilt deg]]] [...]
(a tablet in a keyboard folio: tablet_z_klawiatura:0.15:10:30 - the folio's lip below the screen also looks forward and
the glass of that model sags a few mm at its lower edge; what is left: tablet_z_klawiatura@0.10,0.15,0.90,0.74 - the
glass as seen from the front in that rectangle of the review sheet's front tile)
Only on a decrypted copy of a pack; the pack is rebuilt afterwards (tar -> openssl cms, manifest.json hashes).
Needs numpy, pillow."""
import json
import struct
import sys
from pathlib import Path

import numpy as np
from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent))
from napisy_precz import raster, to_file, patch, load   # noqa: E402  (the same rasteriser, view patch and texture writer)


def flatten(path, nor, low=.05, tol=.003, tilt=15.):
    raw = open(path, 'rb').read(); n = struct.unpack_from('<I', raw, 12)[0]; j = json.loads(raw[20:20 + n]); blob = bytearray(raw[20 + n + 8:])
    pr = j['meshes'][0]['primitives'][0]

    def where(i):
        a = j['accessors'][i]; bv = j['bufferViews'][a['bufferView']]; return bv.get('byteOffset', 0) + a.get('byteOffset', 0), a
    po, pa = where(pr['attributes']['POSITION']); no, _ = where(pr['attributes']['NORMAL']); uo, ua = where(pr['attributes']['TEXCOORD_0']); io, ia = where(pr['indices'])
    P = np.frombuffer(bytes(blob), '<f4', pa['count'] * 3, po).reshape(-1, 3).astype(np.float64)
    N = np.frombuffer(bytes(blob), '<f4', pa['count'] * 3, no).reshape(-1, 3).astype(np.float64).copy()
    U = np.frombuffer(bytes(blob), '<f4', ua['count'] * 2, uo).reshape(-1, 2).astype(np.float64)
    I = np.frombuffer(bytes(blob), '<u2' if ia['componentType'] == 5123 else '<u4', ia['count'], io).reshape(-1, 3).astype(np.int64)
    T = P[I]; fn = np.cross(T[:, 1] - T[:, 0], T[:, 2] - T[:, 0]); ar = np.linalg.norm(fn, axis=1); fn /= np.maximum(ar, 1e-12)[:, None]; c = T.mean(1)
    cand = (c[:, 1] > P[:, 1].min() + low * np.ptp(P[:, 1])) & (fn[:, 2] > .7)        # the keyboard deck looks up, not forward
    nn = (fn[cand] * ar[cand, None]).sum(0); nn /= np.linalg.norm(nn)
    for _ in range(3):                                                # the plane of the glass: the faces that agree with it
        core = cand & (fn @ nn > np.cos(np.radians(10)))
        nn = (fn[core] * ar[core, None]).sum(0); nn /= np.linalg.norm(nn)
    d = c @ nn; off = np.median(d[core])
    screen = cand & (np.abs(d - off) < tol) & (fn @ nn > np.cos(np.radians(tilt)))
    H, W = nor.shape[:2]; flat = np.zeros((H, W), bool)
    for a, b, cc in I[screen]:
        got = raster(U[[a, b, cc]] * [W, H], W, H, .05)
        if got is not None: flat[got[0]] |= got[4]
    nor[flat] = (128, 128, 255)
    N[np.unique(I[screen])] = nn
    blob[no:no + N.size * 4] = N.astype('<f4').tobytes()
    js = json.dumps(j, separators=(',', ':')).encode(); js += b' ' * (-len(js) % 4)
    open(path, 'wb').write(struct.pack('<4sII', b'glTF', 2, 28 + len(js) + len(blob)) + struct.pack('<I4s', len(js), b'JSON') + js
                           + struct.pack('<I4s', len(blob), b'BIN\0') + bytes(blob))
    return int(screen.sum()), int(flat.sum())


def main():
    base = Path(sys.argv[1]); maps = {}
    for arg in sys.argv[2:]:
        if '@' in arg:
            # name@u0,v0,u1,v1: the glass as SEEN from the front in that rectangle of the review sheet's tile - every texel
            # of it gets a flat normal (a screen whose lower edge sags out of its plane)
            name, rect = arg.split('@'); g = base / (name + '.glb'); P, UV, I, j = load(g)
            uri = j['images'][j['textures'][j['materials'][0]['normalTexture']['index']]['source']]['uri']
            if uri not in maps: maps[uri] = np.asarray(Image.open(base / uri).convert('RGB')).copy()
            t = maps[uri]; m = patch(P, UV, I, 'przod', [float(x) for x in rect.split(',')], t.shape[1], t.shape[0])[0]; t[m] = (128, 128, 255)
            print(f'  {name:28s} glass seen in {rect}: flat normal texels {int(m.sum()):7d}'); continue
        name, *opt = arg.split(':'); g = base / (name + '.glb'); opt = [float(x) for x in opt]; raw = open(g, 'rb').read(); n = struct.unpack_from('<I', raw, 12)[0]; j = json.loads(raw[20:20 + n])
        uri = j['images'][j['textures'][j['materials'][0]['normalTexture']['index']]['source']]['uri']
        if uri not in maps: maps[uri] = np.asarray(Image.open(base / uri).convert('RGB')).copy()
        faces, texels = flatten(g, maps[uri], *(opt[:1] or [.05]), *([opt[1] / 1000] if len(opt) > 1 else []), *(opt[2:3]))
        print(f'  {name:28s} screen faces {faces:5d}, flat normal texels {texels:7d}')
    for uri, t in maps.items(): to_file(t, base / uri)


if __name__ == '__main__':
    main()
