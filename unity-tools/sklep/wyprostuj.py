#!/usr/bin/env python3
"""BRAXHORN — products that stand askew (owner 12.11.2026 15:24 "laptopy to ja nie wiem, czy się nadają ... wszystko sprawdzaj,
poprawiaj"). Tripo draws a sheet in three-quarter view, so a cut laptop or tablet keeps the sheet's turn: on a shelf it
stands some 25 degrees off the shelf's edge, every one of them the same way - it reads as carelessly put down.

Each named model is turned about the vertical so that its long side in plan (the hinge of a laptop, the long edge of a
tablet) runs along x - the smaller of the two possible turns, so its front stays its front - and is put back with its base
centre at the origin. Vertices and normals only; UVs, triangles and textures are untouched. katalog.json sizes follow.

usage: wyprostuj.py <decrypted pack folder> <model>[:extra degrees] [...]
(an open laptop is deeper in plan than it is wide, its long side is NOT its hinge: laptop_x:90 turns it a quarter more,
keyboard to the front; laptop_x:ekran then sets its screen square to the front; the result is looked at on a review sheet)
Only on a decrypted copy of a pack; the pack is rebuilt afterwards (tar -> openssl cms, manifest.json hashes). Needs numpy."""
import json
import struct
import sys
from pathlib import Path

import numpy as np


def straighten(path, extra=0.):
    raw = open(path, 'rb').read(); n = struct.unpack_from('<I', raw, 12)[0]; j = json.loads(raw[20:20 + n]); blob = bytearray(raw[20 + n + 8:])
    pr = j['meshes'][0]['primitives'][0]

    def where(i):
        a = j['accessors'][i]; bv = j['bufferViews'][a['bufferView']]; return bv.get('byteOffset', 0) + a.get('byteOffset', 0), a
    po, pa = where(pr['attributes']['POSITION']); no, _ = where(pr['attributes']['NORMAL']); io, ia = where(pr['indices'])
    P = np.frombuffer(bytes(blob), '<f4', pa['count'] * 3, po).reshape(-1, 3).astype(np.float64)
    N = np.frombuffer(bytes(blob), '<f4', pa['count'] * 3, no).reshape(-1, 3).astype(np.float64)
    I = np.frombuffer(bytes(blob), '<u2' if ia['componentType'] == 5123 else '<u4', ia['count'], io).reshape(-1, 3).astype(np.int64)
    T = P[I]; area = np.linalg.norm(np.cross(T[:, 1] - T[:, 0], T[:, 2] - T[:, 0]), axis=1) / 2; c = T.mean(1)[:, [0, 2]]
    mean = (c * area[:, None]).sum(0) / area.sum(); d = c - mean; cov = (d[:, :, None] * d[:, None, :] * area[:, None, None]).sum(0)
    w, v = np.linalg.eigh(cov); a = v[:, 1]                                  # the long side in plan
    ang = np.arctan2(a[1], a[0]); ang = (ang + np.pi / 2) % np.pi - np.pi / 2   # -90..90: the smaller turn
    if extra == 'ekran':
        # an open screen: the faces of the upper half that look forward - their mean normal, in plan, is turned to +z exactly
        fn = np.cross(T[:, 1] - T[:, 0], T[:, 2] - T[:, 0]); up = T.mean(1)[:, 1] > P[:, 1].min() + .45 * np.ptp(P[:, 1])
        k = up & (fn[:, 2] > .5 * np.linalg.norm(fn, axis=1)); nrm = fn[k].sum(0); t = np.pi / 2 - np.arctan2(nrm[2], nrm[0])
    else:
        t = -ang + np.radians(float(extra or 0))
    ca, sa = np.cos(t), np.sin(t)

    def turn(V):
        o = V.copy(); o[:, 0] = V[:, 0] * ca - V[:, 2] * sa; o[:, 2] = V[:, 0] * sa + V[:, 2] * ca; return o
    used = np.unique(I); Q = turn(P); lo, hi = Q[used].min(0), Q[used].max(0); Q -= [(lo[0] + hi[0]) / 2, lo[1], (lo[2] + hi[2]) / 2]
    blob[po:po + Q.size * 4] = Q.astype('<f4').tobytes(); blob[no:no + N.size * 4] = turn(N).astype('<f4').tobytes()
    pa['min'] = Q[used].min(0).tolist(); pa['max'] = Q[used].max(0).tolist()
    js = json.dumps(j, separators=(',', ':')).encode(); js += b' ' * (-len(js) % 4)
    open(path, 'wb').write(struct.pack('<4sII', b'glTF', 2, 28 + len(js) + len(blob)) + struct.pack('<I4s', len(js), b'JSON') + js
                           + struct.pack('<I4s', len(blob), b'BIN\0') + bytes(blob))
    return np.degrees(t), Q[used].max(0) - Q[used].min(0)


def main():
    base = Path(sys.argv[1]); kat = base / 'katalog.json'; k = json.loads(kat.read_text(encoding='utf-8'))
    for arg in sys.argv[2:]:
        name, _, extra = arg.partition(':'); deg, size = straighten(base / (name + '.glb'), extra)
        for it in k['items']:
            if it['name'] == name: it['size_m'] = [round(float(x), 3) for x in size]
        print(f'  {name:28s} turned {deg:6.1f} deg  ->  {size[0]:.3f} x {size[1]:.3f} x {size[2]:.3f} m')
    kat.write_text(json.dumps(k, indent=1, ensure_ascii=False) + '\n', encoding='utf-8')


if __name__ == '__main__':
    main()
