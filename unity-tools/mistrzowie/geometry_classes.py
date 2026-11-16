"""(tool, not run in CI) Triangle statistics of a preview geometry dump: per section (mesh name before ' | material') and per piece size class
(pieces = triangles joined by welded corners; size = longest side of the piece's box, nominal metres): big >= 4 m,
small 1.2-4 m, tiny < 1.2 m - the classes BraxChampionsWydajnosc culls at 18 km / 260 / 90 world units.
usage: geostat.py <geometry.json> [prefix to skip]"""
import sys, collections
import ijson
import numpy as np
from scipy.sparse import coo_matrix
from scipy.sparse.csgraph import connected_components

skip = sys.argv[2] if len(sys.argv) > 2 else 'FOUNDATION'
sec = collections.defaultdict(lambda: np.zeros(3, dtype=np.int64))
tot = np.zeros(3, dtype=np.int64)
with open(sys.argv[1], 'rb') as f:
    for e in ijson.items(f, 'meshes.item', use_float=True):
        name = e[0]
        if name.startswith(skip):
            continue
        v = np.asarray(e[2], dtype=np.float64).reshape(-1, 3)
        p = v[np.asarray(e[3], dtype=np.int64).reshape(-1, 3)].reshape(-1, 3) if len(e) > 3 else v
        nt = len(p) // 3
        if nt == 0:
            continue
        q = np.round(p / .001).astype(np.int64)
        _, inv = np.unique(q, axis=0, return_inverse=True); inv = inv.ravel(); nv = inv.max() + 1
        t = inv.reshape(-1, 3)
        rows = np.concatenate([t[:, 0], t[:, 1]]); cols = np.concatenate([t[:, 1], t[:, 2]])
        _, lab = connected_components(coo_matrix((np.ones(len(rows), dtype=np.int8), (rows, cols)), shape=(nv, nv)), directed=False)
        tl = lab[t[:, 0]]
        tp = p.reshape(-1, 3, 3)
        order = np.argsort(tl, kind='stable'); tls = tl[order]
        bounds = np.flatnonzero(np.diff(tls)) + 1
        starts = np.concatenate([[0], bounds]); ends = np.concatenate([bounds, [len(tls)]])
        mn = np.minimum.reduceat(tp[order].min(1), starts, axis=0); mx = np.maximum.reduceat(tp[order].max(1), starts, axis=0)
        size = (mx - mn).max(1); cnt = ends - starts
        cls = np.where(size >= 4, 0, np.where(size >= 1.2, 1, 2))
        s = np.bincount(cls, weights=cnt, minlength=3).astype(np.int64)
        key = name.split(' | ')[0] + ' | ' + (name.split(' | ')[1] if ' | ' in name else '')
        sec[key[:70]] += s; tot += s
print("TOTAL tris big %d small %d tiny %d (sum %d)" % (tot[0], tot[1], tot[2], tot.sum()))
for k, s in sorted(sec.items(), key=lambda x: -x[1].sum())[:30]:
    print("%9d  big %8d small %8d tiny %8d  %s" % (s.sum(), s[0], s[1], s[2], k))
