"""(tool, not run in CI) Python twin of BraxChampionsPodzial.Kawalki/Zloz (the far copy from big pieces, nothing moved) on a preview geometry
JSON: writes a geometry JSON of the far copy only (no foundation, no models, no palms) for render3d.js.
usage: far_copy.py <geometry.json> <out.json> <maxTris> [prefix to keep, default all but FOUNDATION]"""
import json, sys
import numpy as np
from scipy.sparse import coo_matrix
from scipy.sparse.csgraph import connected_components

MIN_PIECE, SLIM, WELD = 2.5, 25.0, .001
g = json.load(open(sys.argv[1])); max_tris = int(sys.argv[3])
pieces = []   # (score, mesh index, triangle indices)
total = 0
def soup(entry):
    v = np.asarray(entry[2], dtype=np.float64).reshape(-1, 3)
    if len(entry) > 3:
        return v[np.asarray(entry[3], dtype=np.int64).reshape(-1, 3)].reshape(-1, 3)
    return v
for mi, entry in enumerate(g['meshes']):
    name = entry[0]
    if name.startswith('FOUNDATION'):
        continue
    p = soup(entry)
    nt = len(p) // 3
    if nt == 0:
        continue
    total += nt
    q = np.round(p / WELD).astype(np.int64)
    _, inv = np.unique(q, axis=0, return_inverse=True)
    inv = inv.ravel(); nv = inv.max() + 1
    t = inv.reshape(-1, 3)
    rows = np.concatenate([t[:, 0], t[:, 1]]); cols = np.concatenate([t[:, 1], t[:, 2]])
    ncomp, lab = connected_components(coo_matrix((np.ones(len(rows)), (rows, cols)), shape=(nv, nv)), directed=False)
    tl = lab[t[:, 0]]
    tp = p.reshape(-1, 3, 3)
    for c in np.unique(tl):
        idx = np.nonzero(tl == c)[0]
        pts = tp[idx].reshape(-1, 3)
        sz = pts.max(0) - pts.min(0)
        s = np.sort(sz)[::-1]
        score = min(s[0], SLIM * s[1])
        if score >= MIN_PIECE:
            pieces.append((score, mi, idx))
pieces.sort(key=lambda x: -x[0] / max(1.0, len(x[2]) / 1500.0) ** .5)
kept, n, cut = [], 0, 0.0
for sc, mi, idx in pieces:
    if n + len(idx) > max_tris and kept:
        cut = max(cut, sc); continue
    kept.append((mi, idx)); n += len(idx)
out = dict(g); out['meshes'] = [e for e in g['meshes'] if e[0].startswith('FOUNDATION')]; out['models'] = []; out['palms'] = []
by = {}
for mi, idx in kept:
    by.setdefault(mi, []).append(idx)
for mi, lst in by.items():
    e = g['meshes'][mi]
    p = soup(e).reshape(-1, 9)
    sel = p[np.concatenate(lst)].reshape(-1, 3)
    out['meshes'].append([e[0], e[1], [round(float(x), 4) for x in sel.ravel()], list(range(len(sel)))])
json.dump(out, open(sys.argv[2], 'w'))
print(f"FAR_COPY source tris {total} pieces {len(pieces)} kept {len(kept)} tris {n} cut below {cut:.1f} m")
