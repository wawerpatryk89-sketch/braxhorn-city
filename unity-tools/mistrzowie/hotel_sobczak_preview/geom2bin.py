"""hotel_geometry.json -> binary triangle buffers for render3d.js (real depth buffer, no painter's sorting).

Unity is left-handed (front face: Cross(b-a,c-a) toward the viewer); three.js is right-handed.
Mirroring z flips the handedness, so every triangle is written as (a, c, b).
The guest floor template is placed at all 26 guest levels; lift cabins wait at PARTER.
"""
import json
import sys
from pathlib import Path

import numpy as np

from render import colour

TEMPLATE = "HOTEL SOBCZAK | GUEST FLOOR TEMPLATE"


def main(src, out):
    d = json.load(open(src)); lv = d["levels"]; out = Path(out); out.mkdir(parents=True, exist_ok=True)
    tris, cols = [], []

    def add(t, m):  # t: (n,3,3) Unity coordinates
        t = t.copy(); t[..., 2] *= -1; t = t[:, [0, 2, 1]]
        tris.append(t); c = np.array(colour(m), np.float32); cols.append(np.repeat(c[None], len(t), 0))

    quads = {}
    for q in d["quads"]:
        g, m = q[0], q[1]; v = np.array(q[3:], np.float32).reshape(4, 3)
        if g.startswith("ELEVATOR") or g.startswith("cabin door"):
            v = v.copy(); v[:, 1] += lv[0]
        quads.setdefault((g.startswith(TEMPLATE), m), []).append(v)
    for (tmpl, m), vs in quads.items():
        V = np.stack(vs); T = np.concatenate([V[:, [0, 1, 2]], V[:, [0, 2, 3]]])
        if tmpl:
            for f in range(2, 28):
                W = T.copy(); W[..., 1] += lv[f]; add(W, m)
        else:
            add(T, m)
    for name, m, verts, tri in d["meshes"]:
        p = np.array(verts, np.float32).reshape(-1, 3); t = np.array(tri, int).reshape(-1, 3)
        add(p[t], m)
    T = np.concatenate(tris).astype(np.float32); C = np.concatenate(cols).astype(np.float32)
    opaque = C[:, 3] > .99
    for tag, sel in (("o", opaque), ("t", ~opaque)):
        T[sel].tofile(out / f"pos_{tag}.bin")
        np.repeat(C[sel][:, None, :3], 3, 1).astype(np.float32).tofile(out / f"col_{tag}.bin")
        np.repeat(C[sel][:, None, 3:], 3, 1).astype(np.float32).tofile(out / f"alpha_{tag}.bin")
    print("triangles", len(T), "opaque", int(opaque.sum()), "transparent", int((~opaque).sum()))


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else "hotel_geometry.json", sys.argv[2] if len(sys.argv) > 2 else "bin")
