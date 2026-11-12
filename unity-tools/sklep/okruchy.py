#!/usr/bin/env python3
"""BRAXHORN — loose crumbs of the owner's Tripo models (owner 12.11.2026 14:03 "trzeba zrobić przegląd tych rzeczy z 3D ...
co się nadaje, zostawiamy"): a sheet cut into products leaves, here and there, a sliver of a neighbour or a flake of the
model's own surface hanging in the air next to it (the kettlebell's flake by its handle, the gold chips by the earrings).

A crumb = a connected piece of the mesh (vertices welded by position) that is small (under SHARE of the model's
triangles and under CAP triangles) AND does not touch any bigger piece (its box, grown by GAP, meets none of theirs).
Small pieces that sit on the model (a button, a stone, a lace end) touch it and stay.
Only products cut from a Tripo sheet are looked at (one mesh, no node transform); a model built of separate parts on
purpose (the boxing ring's rope spacers) is not a sheet product - read the report before --usun.

usage: okruchy.py <pack folder or .glb> [...]            report only
       okruchy.py --usun <pack folder or .glb> [...]     rewrite the .glb files without their crumbs (indices only:
                                                         vertices, UVs and textures stay as they are)
Only on a decrypted copy of a pack; the pack is rebuilt afterwards (tar -> openssl cms, manifest.json hashes)."""
import json
import struct
import sys
from pathlib import Path

import numpy as np

SHARE, CAP, GAP = .04, 120, .004      # of the model's triangles; triangles; metres


def crumbs(P, I):
    _, weld = np.unique(np.round(P, 5), axis=0, return_inverse=True); weld = weld.ravel(); par = np.arange(weld.max() + 1)

    def find(x):
        while par[x] != x:
            par[x] = par[par[x]]; x = par[x]
        return x
    for a, b, c in weld[I]:
        ra, rb = find(a), find(b)
        if ra != rb: par[rb] = ra
        ra, rc = find(a), find(c)
        if ra != rc: par[rc] = ra
    tri = np.array([find(x) for x in weld[I[:, 0]]]); ids, cnt = np.unique(tri, return_counts=True)
    box = {i: (P[I[tri == i]].reshape(-1, 3).min(0), P[I[tri == i]].reshape(-1, 3).max(0)) for i in ids}
    big = [i for i, c in zip(ids, cnt) if not (c < SHARE * len(I) and c < CAP)]
    out = []
    for i, c in zip(ids, cnt):
        if i in big: continue
        lo, hi = box[i]
        if any(np.all(lo - GAP <= box[b][1]) and np.all(hi + GAP >= box[b][0]) for b in big): continue      # it sits on the model
        out.append((i, int(c), (lo + hi) / 2, hi - lo))
    return tri, out


def one(path, remove):
    raw = open(path, 'rb').read(); n = struct.unpack_from('<I', raw, 12)[0]; j = json.loads(raw[20:20 + n]); blob = bytearray(raw[20 + n + 8:])
    found = 0
    if len(j['meshes']) != 1 or any(k in nd for nd in j.get('nodes', []) for k in ('matrix', 'rotation', 'scale')):
        return 0                      # not a product cut from a sheet (a library model with its own node tree): left alone
    for mesh in j['meshes']:
        for pr in mesh['primitives']:
            if pr.get('mode', 4) != 4 or 'indices' not in pr: continue          # triangles only
            a = j['accessors'][pr['attributes']['POSITION']]; bv = j['bufferViews'][a['bufferView']]
            P = np.frombuffer(bytes(blob), '<f4', a['count'] * 3, bv.get('byteOffset', 0) + a.get('byteOffset', 0)).reshape(-1, 3).astype(np.float64)
            ia = j['accessors'][pr['indices']]; ib = j['bufferViews'][ia['bufferView']]; dt = '<u2' if ia['componentType'] == 5123 else '<u4'
            off = ib.get('byteOffset', 0) + ia.get('byteOffset', 0)
            I = np.frombuffer(bytes(blob), dt, ia['count'], off).reshape(-1, 3).astype(np.int64)
            tri, out = crumbs(P, I)
            for _, c, centre, size in out:
                print(f'  {Path(path).name}: crumb of {c} triangles at {np.round(centre, 3).tolist()}, {np.round(size * 1000).astype(int).tolist()} mm')
            found += len(out)
            if remove and out:
                keep = I[~np.isin(tri, [o[0] for o in out])]
                blob[off:off + keep.size * np.dtype(dt).itemsize] = keep.astype(dt).tobytes(); ia['count'] = int(keep.size)
                used = P[np.unique(keep)]; a['min'] = used.min(0).tolist(); a['max'] = used.max(0).tolist()
    if remove and found:
        js = json.dumps(j, separators=(',', ':')).encode(); js += b' ' * (-len(js) % 4)
        open(path, 'wb').write(struct.pack('<4sII', b'glTF', 2, 28 + len(js) + len(blob)) + struct.pack('<I4s', len(js), b'JSON') + js
                               + struct.pack('<I4s', len(blob), b'BIN\0') + bytes(blob))
    return found


def main():
    args = sys.argv[1:]; remove = '--usun' in args; total = 0
    for a in [x for x in args if x != '--usun']:
        files = sorted(Path(a).glob('*.glb')) if Path(a).is_dir() else [Path(a)]
        for f in files: total += one(str(f), remove)
    print(('removed' if remove else 'found'), total, 'crumbs')


if __name__ == '__main__':
    main()
