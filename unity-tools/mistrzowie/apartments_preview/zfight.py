"""Find z-fighting in a preview geometry dump: faces facing the same way, in (nearly) the same plane, overlapping,
with different materials. These flicker in the game (owner video IMG_0786, 9.11: black granite pier vs white render
on the apartment corner).

* Exterior faces are seen from far away, where a mobile depth buffer loses millimetres: planes closer than 6 mm fight.
  Interior faces are seen from a few metres: 1.5 mm.
* A face pressed against an opposite face in the same plane (a box resting on a slab, a lining on a wall) is hidden
  and cannot flicker; such faces are skipped.

usage: python3 zfight.py T04_geometry.json [more.json ...]   -> prints every visible overlap, exit 1 if any
"""
import json
import sys
from collections import defaultdict

import numpy as np
from shapely.geometry import Polygon
from shapely.ops import unary_union
from shapely.strtree import STRtree

TOL_OUT, TOL_IN = .006, .0015
MIN_AREA = 2e-4   # m^2


def faces(path):
    d = json.load(open(path))
    for q in d["quads"]:
        v = np.array(q[3:15], float).reshape(4, 3)
        n = np.cross(v[1] - v[0], v[2] - v[0])
        if np.linalg.norm(n) < 1e-12:
            n = np.cross(v[2] - v[0], v[3] - v[0])
        ln = np.linalg.norm(n)
        if ln < 1e-12:
            continue
        yield q[0], q[1], bool(q[2]), v, n / ln


def frame(n):
    a = np.cross(n, [0, 1, 0] if abs(n[1]) < .9 else [1, 0, 0]); a /= np.linalg.norm(a)
    return a, np.cross(n, a)


def gap(fi, fj):
    """Distance between the planes of two (nearly) parallel faces, measured locally: the other face's centre from this
    face's plane and back, averaged. (n . v at world coordinates of ~1 km amplified the tiny float32 differences between
    the normals of tilted faces into millimetres - false pairs on the beach loungers.)"""
    ni, ci, nj, cj = fi[5], fi[6], fj[5], fj[6]
    return .5 * (abs(ni @ (cj - ci)) + abs(nj @ (ci - cj)))


def main(paths):
    bad = 0
    for path in paths:
        buckets = defaultdict(list)
        for g, m, inside, v, n in faces(path):
            buckets[tuple(np.round(n, 2))].append((g, m, inside, v, float(n @ v[0]), n, v.mean(0)))
        # 2D polygons in a frame shared by a normal and its opposite (so contact can be tested)
        prepared = {}
        for key, items in buckets.items():
            n = np.array(key); n /= np.linalg.norm(n)
            canon = n if (n[0], n[1], n[2]) > (0, 0, 0) else -n
            a, b = frame(canon)
            polys = [Polygon([(p @ a, p @ b) for p in v]).buffer(0) for _, _, _, v, _, _, _ in items]
            prepared[key] = (items, polys, STRtree(polys), np.array([t[4] for t in items]))

        def hidden(key, i):
            opp = tuple(-k if k != 0 else 0.0 for k in key)
            opp = next((k for k in prepared if np.allclose(k, opp)), None)
            if opp is None:
                return False
            items, polys, tree, ds = prepared[key]
            oitems, opolys, otree, ods = prepared[opp]
            p = polys[i]
            cover = [opolys[j] for j in otree.query(p) if gap(items[i], oitems[j]) < .0015]
            return bool(cover) and p.difference(unary_union(cover)).area < .02 * max(p.area, 1e-9)

        hits = defaultdict(lambda: [0, 0.0, None])
        for key, (items, polys, tree, ds) in prepared.items():
            cache = {}
            for i, (g, m, inside, v, d, _, _) in enumerate(items):
                for j in tree.query(polys[i]):
                    if j <= i or items[j][1] == m:
                        continue
                    tol = TOL_IN if (inside and items[j][2]) else TOL_OUT
                    if gap(items[i], items[j]) > tol:
                        continue
                    area = polys[i].intersection(polys[j]).area
                    if area < MIN_AREA:
                        continue
                    for k in (i, j):
                        if k not in cache:
                            cache[k] = hidden(key, k)
                    if cache[i] or cache[j]:
                        continue
                    k2 = tuple(sorted((m, items[j][1]))) + (g.split(" >> ")[-1][:24], items[j][0].split(" >> ")[-1][:24],
                                                             "n=" + ",".join("%g" % c for c in np.round(key, 1)))
                    h = hits[k2]; h[0] += 1; h[1] += area
                    if h[2] is None:
                        h[2] = v.mean(0).round(2).tolist()
        for (m1, m2, g1, g2, nk), (count, area, at) in sorted(hits.items(), key=lambda kv: -kv[1][1]):
            print(f"ZFIGHT {path}: {m1.split('|')[0]}  vs  {m2.split('|')[0]}  {nk}  x{count}  {area:.2f} m2  at {at}  [{g1} | {g2}]")
        print(f"ZFIGHT_SUMMARY {path} pairs={len(hits)} faces={sum(h[0] for h in hits.values())}")
        bad += len(hits)
    return 1 if bad else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
