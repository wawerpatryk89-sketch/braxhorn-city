"""hotel_geometry.json -> per-material buffers + scene.json for render3d.js (textures, models, lights).

Unity is left-handed; three.js is right-handed: z is mirrored and every triangle is written as (a, c, b).
UVs follow the W kit (planar, metres): floor/ceiling (x, z), walls facing x (z*sign, y), walls facing z (-x*sign, y),
divided by the material's tile size, exactly like BraxChampionsFoundation.MaterialFor. Materials whose stub name is
'id|slug|metres' get the real Poly Haven maps; surfaces (brass, glass, LED...) keep their real colour.
The guest-floor template is drawn at all 26 levels (models and lights on floor 2, where the interior views are).
"""
import json
import sys
from pathlib import Path

import numpy as np

TEMPLATE = "HOTEL SOBCZAK | GUEST FLOOR TEMPLATE"


def texture_of(name):
    """'id|slug|metres' (a textured material) -> (slug, metres); anything else -> (None, 1)."""
    parts = (name[5:] if name.startswith("mesh:") else name).split("|")
    if len(parts) == 3:
        try:
            return parts[1], float(parts[2])
        except ValueError:
            pass
    return None, 1.0


def planar(v, n):
    ax, ay, az = abs(n[0]), abs(n[1]), abs(n[2])
    if ay >= ax and ay >= az:
        return np.stack([v[:, 0], v[:, 2]], 1)
    if ax >= az:
        return np.stack([v[:, 2] * np.sign(n[0]), v[:, 1]], 1)
    return np.stack([-v[:, 0] * np.sign(n[2]), v[:, 1]], 1)


def main(src, out, ground=None):
    d = json.load(open(src)); lv = d["levels"]; out = Path(out); out.mkdir(parents=True, exist_ok=True)
    colours = d.get("colours", {}); ambient = d.get("ambient", {})
    groups = {}

    def add(m, tris, uvs):
        t = tris.copy(); t[..., 2] *= -1; t = t[:, [0, 2, 1]]; u = uvs[:, [0, 2, 1]]
        g = groups.setdefault(m, ([], []))
        g[0].append(t.astype(np.float32)); g[1].append(u.astype(np.float32))

    for q in d["quads"]:
        g, m = q[0], q[1]
        v = np.array(q[3:15], np.float32).reshape(4, 3)
        n = np.cross(v[1] - v[0], v[2] - v[0])
        if np.linalg.norm(n) < 1e-9:
            n = np.cross(v[2] - v[0], v[3] - v[0])
        uv = np.array(q[15:23], np.float32).reshape(4, 2) if len(q) >= 23 else planar(v, n) / texture_of(m)[1]   # W.QuadUV: the quad's own UVs
        T = np.stack([v[[0, 1, 2]], v[[0, 2, 3]]]); U = np.stack([uv[[0, 1, 2]], uv[[0, 2, 3]]])
        leaf = g.split(" >> ")[-1]
        if g.startswith(TEMPLATE):
            for f in range(2, 28):
                W = T.copy(); W[..., 1] += lv[f]; add(m, W, U)
        else:
            if "ELEVATOR" in g and ("cabin" in g):
                T = T.copy(); T[..., 1] += lv[0]
            add(m, T, U)
    for entry in d["meshes"]:
        name, m, verts, tri = entry[:4]
        p = np.array(verts, np.float32).reshape(-1, 3); t = np.array(tri, int).reshape(-1, 3)
        if not len(t):
            continue
        P = p[t]; tile = texture_of(m)[1]
        if len(entry) > 4:                      # a picture (sign, mural): the mesh's own UVs
            U = np.array(entry[4], np.float32).reshape(-1, 2)[t]
        else:                                   # metre-planar UVs per triangle, like the W kit and the casino's Tile()
            n = np.cross(P[:, 1] - P[:, 0], P[:, 2] - P[:, 0]); a = np.abs(n)
            top = (a[:, 1] >= a[:, 0]) & (a[:, 1] >= a[:, 2]); xs = ~top & (a[:, 0] >= a[:, 2]); zs = ~top & ~xs
            U = np.zeros((len(P), 3, 2), np.float32)
            U[top] = P[top][:, :, [0, 2]]
            U[xs] = np.stack([P[xs][:, :, 2] * np.sign(n[xs, 0])[:, None], P[xs][:, :, 1]], -1)
            U[zs] = np.stack([-P[zs][:, :, 0] * np.sign(n[zs, 2])[:, None], P[zs][:, :, 1]], -1)
            U /= tile
        add("mesh:" + m, P, U)
    mats = []
    for i, (m, (ts, us)) in enumerate(groups.items()):
        T = np.concatenate(ts); U = np.concatenate(us)
        T.tofile(out / f"m{i}_pos.bin"); U.tofile(out / f"m{i}_uv.bin")
        slug = texture_of(m)[0]
        c = colours.get(m, [1, 1, 1, 1]) if not m.startswith("mesh:") else colours.get(m[5:], [1, 1, 1, 1])
        amb = ambient.get(m[5:] if m.startswith("mesh:") else m, [.60, .68, .78, .42, .40, .36, .65])
        mats.append(dict(id=i, name=m, slug=slug, colour=c, ambient=amb, count=len(T), pbr=d.get("pbr", {}).get(m[5:] if m.startswith("mesh:") else m)))
    models = []
    for mo in d.get("models", []):
        x, y, z = mo["pos"]
        floors = [2] if mo["template"] else [None]
        for f in floors:
            yy = y + (lv[f] if f is not None else 0)
            models.append(dict(path=mo["path"].replace("Assets/Source/Mistrzowie/", ""), pos=[x, yy, -z], rotY=(180 - mo["yaw"]) * np.pi / 180,
                               scale=mo.get("scale", 1), material=mo.get("material")))
    lights = []
    for x, y, z, rng, inten, tmpl in d.get("lights", []):
        if tmpl:
            y += lv[2]
        lights.append([x, y, -z, rng, inten])
    palms = [[x, y, -z] for x, y, z in d.get("palms", [])]
    json.dump(dict(materials=mats, models=models, lights=lights, ground=ground, palms=palms), open(out / "scene.json", "w"))
    print("materials", len(mats), "triangles", sum(m["count"] for m in mats), "models", len(models), "lights", len(lights),
          "textured", sum(1 for m in mats if m["slug"]))


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else "hotel_geometry.json", sys.argv[2] if len(sys.argv) > 2 else "bin",
         float(sys.argv[3]) if len(sys.argv) > 3 else None)      # optional: height of a plain ground plane around the plot
