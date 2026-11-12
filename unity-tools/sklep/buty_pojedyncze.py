#!/usr/bin/env python3
"""BRAXHORN — single shoes out of the owner's Tripo shoe sheet (owner 12.11.2026 13:58: "te buty ... To jest tragedia. Czy to
da radę naprawić? ... Tu ci podsyłam jeszcze paczkę butów").

The first two shoe sheets came out as PAIRS grown together (heels or whole inner sides one lump): a pair cannot be parted
without a torn inner side, so those packs are no longer put out. The third sheet is one mesh of 37 loose pieces: each piece
is a connected island of the mesh, so it is cut along the islands without touching a single triangle. Only the pieces
named in the spec are kept (a clean single shoe: no leg, no second shoe grown in, no logo of a real maker); each one is

  * laid on its sole: the face of its convex hull that it would really rest on (centre of mass over the support, the
    normal nearest to the sheet's "down"), turned to the floor - the sheet drew the shoes at every angle;
  * turned toe to +x, its profile to the model's front (+z: the side a customer sees on a wall shelf);
  * scaled to a real length (spec "dl": 0.29 m a man's 43, 0.25 m a woman's 38) and put with its base centre at 0.

Output: <out>/<name>.glb (real metres, pivot = base centre) + <out>/_tekstury/<pack>_kolor.jpg|_normal.jpg|_mr.png (the
sheet's own atlas, shared by every shoe) + <out>/katalog.json, the layout every braxhorn-sklep-* pack has.

usage: buty_pojedyncze.py <sheet.glb> <spec.json> <out dir>
spec: {"pack": "sklep_buty_03", "min_verts": 150, "items": [{"island": 3, "name": "but_czarny_biala_podeszwa", "kind": "...", "dl": 0.29}]}
(islands are numbered as this tool prints them: rows of the sheet top to bottom, left to right)
"""
import json
import struct
import sys
from pathlib import Path

import numpy as np
from scipy.spatial import ConvexHull

COMP = {5126: np.float32, 5125: np.uint32, 5123: np.uint16}
WIDTH = {'SCALAR': 1, 'VEC2': 2, 'VEC3': 3}


def read(path):
    raw = open(path, 'rb').read(); n = struct.unpack_from('<I', raw, 12)[0]
    return json.loads(raw[20:20 + n]), raw[20 + n + 8:]


def accessor(j, blob, i):
    a = j['accessors'][i]; bv = j['bufferViews'][a['bufferView']]; k = WIDTH[a['type']]
    v = np.frombuffer(blob, COMP[a['componentType']], a['count'] * k, bv.get('byteOffset', 0) + a.get('byteOffset', 0))
    return v.reshape(a['count'], k) if k > 1 else v


def islands(P, I, min_verts):
    """connected pieces of the mesh (vertices welded by position), in reading order of the sheet"""
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
    comp = np.array([find(x) for x in range(len(par))])[weld]
    ids, cnt = np.unique(comp, return_counts=True); rows = []
    for i, c in zip(ids, cnt):
        if c >= min_verts:
            q = P[comp == i]; rows.append((int(i), q.min(0), q.max(0)))
    rows.sort(key=lambda r: (-round(float((r[1][1] + r[2][1]) / 2) / .15), float(r[1][0])))
    return comp, [r[0] for r in rows]


def rotation_to(a, b):
    """the rotation matrix that turns unit vector a onto unit vector b"""
    v = np.cross(a, b); c = float(a @ b)
    if c < -.999999:
        return -np.eye(3) + 2 * np.outer([1, 0, 0], [1, 0, 0])
    k = np.array([[0, -v[2], v[1]], [v[2], 0, -v[0]], [-v[1], v[0], 0]])
    return np.eye(3) + k + k @ k / (1 + c)


def resting(Q, down):
    """the shoe's own sole plane: of the convex hull's faces the one it stands on stably (its centre over the supporting
    patch) whose normal is nearest to `down`; returns that outward normal"""
    hull = ConvexHull(Q); com = Q[hull.vertices].mean(0); best = None; tol = .02 * np.ptp(Q, axis=0).max()      # a sole is not a plane
    for eq in hull.equations:
        n, d = eq[:3], eq[3]
        if n @ down < .55: continue
        near = Q[hull.vertices][np.abs(Q[hull.vertices] @ n + d) < tol]
        if len(near) < 3: continue
        u = np.cross(n, [1, 0, 0] if abs(n[0]) < .9 else [0, 1, 0]); u /= np.linalg.norm(u); w = np.cross(n, u)
        pts = np.c_[near @ u, near @ w]; c2 = np.array([com @ u, com @ w])
        try:
            h2 = ConvexHull(pts)
        except Exception:
            continue
        if not np.all(h2.equations[:, :2] @ c2 + h2.equations[:, 2] <= 1e-9): continue        # the centre is not over this patch
        score = float(n @ down) + .2 * h2.volume / (np.ptp(pts[:, 0]) * np.ptp(pts[:, 1]) + 1e-9)
        if best is None or score > best[0]: best = (score, n)
    return best[1] if best else down


def shoe(P, N, down):
    """vertices and normals of one island -> on its sole, toe to +x, profile to +z, base centre at the origin, length 1"""
    R = rotation_to(resting(P, down), np.array([0., -1., 0.])); Q = P @ R.T; M = N @ R.T
    q = Q[:, [0, 2]]; c = q.mean(0); _, _, vt = np.linalg.svd(q - c, full_matrices=False); a = vt[0]; t = (q - c) @ a
    L = np.ptp(t); mid = (t.min() + t.max()) / 2
    if Q[t > mid + L * .25, 1].max() > Q[t < mid - L * .25, 1].max(): a = -a      # the high end (the heel, the collar) goes to -x, the toe to +x
    ang = np.arctan2(a[1], a[0]); ca, sa = np.cos(ang), np.sin(ang)
    Y = np.array([[ca, 0, sa], [0, 1, 0], [-sa, 0, ca]])            # about y: (a.x, a.z) -> (1, 0)
    Q = Q @ Y.T; M = M @ Y.T; lo, hi = Q.min(0), Q.max(0)
    Q = (Q - [(lo[0] + hi[0]) / 2, lo[1], (lo[2] + hi[2]) / 2]) / (hi[0] - lo[0])
    return Q, M


def main():
    sheet, spec_path, out = sys.argv[1], sys.argv[2], Path(sys.argv[3])
    spec = json.loads(Path(spec_path).read_text(encoding='utf-8')); pack = spec['pack']
    j, blob = read(sheet); pr = j['meshes'][0]['primitives'][0]
    P = accessor(j, blob, pr['attributes']['POSITION']).astype(np.float64); N = accessor(j, blob, pr['attributes']['NORMAL']).astype(np.float64)
    U = accessor(j, blob, pr['attributes']['TEXCOORD_0']).astype(np.float32); I = accessor(j, blob, pr['indices']).astype(np.int64).reshape(-1, 3)
    comp, order = islands(P, I, spec.get('min_verts', 150))
    print(f'{Path(sheet).name}: {len(P)} vertices, {len(I)} triangles, {len(order)} pieces of {spec.get("min_verts", 150)}+ vertices')
    (out / '_tekstury').mkdir(parents=True, exist_ok=True)
    mat = j['materials'][0]; roles = {'kolor': mat['pbrMetallicRoughness']['baseColorTexture']['index'], 'normal': mat['normalTexture']['index'],
                                      'mr': mat['pbrMetallicRoughness']['metallicRoughnessTexture']['index']}
    uris = {}
    for role, ti in roles.items():
        im = j['images'][j['textures'][ti]['source']]; bv = j['bufferViews'][im['bufferView']]
        uris[role] = f'_tekstury/{pack}_{role}.{"png" if im["mimeType"] == "image/png" else "jpg"}'
        (out / uris[role]).write_bytes(blob[bv.get('byteOffset', 0):bv.get('byteOffset', 0) + bv['byteLength']])
    items = []
    for it in spec['items']:
        m = comp == order[it['island']]; F = I[m[I[:, 0]]]; used = np.unique(F); re = np.full(len(P), -1); re[used] = np.arange(len(used)); F = re[F]
        Q, M = shoe(P[used], N[used], np.array([0., -1., 0.])); Q = Q * it['dl']
        size = Q.max(0) - Q.min(0)
        vb, nb, ub, ib = Q.astype('<f4').tobytes(), M.astype('<f4').tobytes(), U[used].astype('<f4').tobytes(), F.astype('<u2').tobytes()
        ib += b'\0' * (-len(ib) % 4); off = [0, len(vb), len(vb) + len(nb), len(vb) + len(nb) + len(ub)]
        g = {'asset': {'version': '2.0', 'generator': 'BRAXHORN buty_pojedyncze.py'}, 'scene': 0, 'scenes': [{'nodes': [0]}],
             'nodes': [{'name': it['name'], 'mesh': 0}],
             'meshes': [{'name': it['name'], 'primitives': [{'attributes': {'POSITION': 0, 'NORMAL': 1, 'TEXCOORD_0': 2}, 'indices': 3, 'material': 0}]}],
             'materials': [{'name': f'braxhorn {pack}_kolor', 'doubleSided': True, 'normalTexture': {'index': 1},
                            'pbrMetallicRoughness': {'baseColorTexture': {'index': 0}, 'metallicRoughnessTexture': {'index': 2}, 'metallicFactor': 1.0, 'roughnessFactor': 1.0}}],
             'textures': [{'sampler': 0, 'source': 0}, {'sampler': 0, 'source': 1}, {'sampler': 0, 'source': 2}],
             'images': [{'uri': uris['kolor']}, {'uri': uris['normal']}, {'uri': uris['mr']}],
             'samplers': [{'magFilter': 9729, 'minFilter': 9987, 'wrapS': 33071, 'wrapT': 33071}],
             'buffers': [{'byteLength': len(vb) + len(nb) + len(ub) + len(ib)}],
             'bufferViews': [{'buffer': 0, 'byteOffset': off[0], 'byteLength': len(vb), 'target': 34962}, {'buffer': 0, 'byteOffset': off[1], 'byteLength': len(nb), 'target': 34962},
                             {'buffer': 0, 'byteOffset': off[2], 'byteLength': len(ub), 'target': 34962}, {'buffer': 0, 'byteOffset': off[3], 'byteLength': F.size * 2, 'target': 34963}],
             'accessors': [{'bufferView': 0, 'componentType': 5126, 'count': len(Q), 'type': 'VEC3', 'min': Q.min(0).tolist(), 'max': Q.max(0).tolist()},
                           {'bufferView': 1, 'componentType': 5126, 'count': len(Q), 'type': 'VEC3'}, {'bufferView': 2, 'componentType': 5126, 'count': len(Q), 'type': 'VEC2'},
                           {'bufferView': 3, 'componentType': 5123, 'count': int(F.size), 'type': 'SCALAR'}]}
        js = json.dumps(g, separators=(',', ':')).encode(); js += b' ' * (-len(js) % 4); bb = vb + nb + ub + ib
        (out / f'{it["name"]}.glb').write_bytes(struct.pack('<4sII', b'glTF', 2, 28 + len(js) + len(bb)) + struct.pack('<I4s', len(js), b'JSON') + js
                                               + struct.pack('<I4s', len(bb), b'BIN\0') + bb)
        items.append({'name': it['name'], 'kind': it['kind'], 'tris': int(len(F)), 'size_m': [round(float(x), 3) for x in size], 'pivot': 'base'})
        print(f'  {it["island"]:2d} {it["name"]:38s} {len(F):4d} tris  {size[0]:.3f} x {size[1]:.3f} x {size[2]:.3f} m')
    (out / 'katalog.json').write_text(json.dumps({'pack': pack, 'items': items}, indent=1, ensure_ascii=False) + '\n', encoding='utf-8')


if __name__ == '__main__':
    main()
