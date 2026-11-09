"""Floor plans (and a painter's fallback renderer) for Hotel Sobczak T03 (stub dump hotel_geometry.json).

Flat colours, painter's algorithm, Unity's left-handed axes. The guest floor template is drawn
at every guest level (as the 26 prefab instances are in Unity); lift cabins wait at PARTER.
Usage: python3 render.py [hotel_geometry.json]  -> T03_*.png
"""
import json
import math
import sys

import matplotlib
import numpy as np

matplotlib.use("Agg")
import matplotlib.pyplot as plt  # noqa: E402
from matplotlib.collections import LineCollection, PolyCollection  # noqa: E402

COL = [("tower_glass", (.42, .52, .60, .55)), ("clear_glass", (.80, .90, .95, .25)), ("pool_water", (.25, .55, .68, .8)),
       ("warm_architectural_light", (1, .86, .55, 1)), ("led_strip", (1, .95, .80, 1)), ("polished_brass", (.86, .66, .30, 1)),
       ("dark_bronze", (.13, .11, .10, 1)), ("brushed_stainless", (.66, .68, .70, 1)), ("polished_mirror", (.80, .84, .88, 1)),
       ("sanitary_porcelain", (.97, .97, .96, 1)), ("brick_facade", (.62, .27, .19, 1)), ("podium_granite", (.80, .78, .74, 1)),
       ("dark_granite", (.28, .29, .31, 1)), ("sidewalk_paving", (.74, .74, .72, 1)), ("drive_asphalt", (.22, .22, .23, 1)),
       ("granite_kerb", (.58, .59, .61, 1)), ("teak_dark", (.36, .23, .15, 1)), ("teak_veneer", (.72, .48, .30, 1)),
       ("cream_marble", (.94, .92, .88, 1)), ("bath_marble", (.88, .87, .84, 1)), ("black_marble", (.10, .10, .11, 1)),
       ("room_oak", (.70, .55, .38, 1)), ("corridor_carpet", (.35, .29, .26, 1)), ("room_rug", (.66, .62, .56, 1)),
       ("spa_terrazzo", (.88, .87, .85, 1)), ("kitchen_tile", (.82, .82, .82, 1)), ("wall_paint", (.92, .89, .84, 1)),
       ("ceiling_stucco", (.96, .96, .95, 1)), ("technical_concrete", (.62, .61, .58, 1)), ("brown_leather", (.36, .21, .13, 1)),
       ("white_leather", (.95, .94, .91, 1)), ("bed_linen", (.98, .97, .95, 1)), ("sheer_curtain", (.93, .89, .80, 1)),
       ("planter_lawn", (.30, .52, .22, 1)), ("grass", (.30, .52, .22, 1))]


def colour(m):
    for k, c in COL:
        if k in m:
            return c
    return (1, 0, 1, 1)


def load(path, floors=True, only=None):
    d = json.load(open(path))
    lv = d["levels"]
    polys, mats, groups = [], [], []
    tmpl = "HOTEL SOBCZAK | GUEST FLOOR TEMPLATE"
    for q in d["quads"]:
        g, m = q[0], q[1]
        v = np.array(q[3:], float).reshape(4, 3)
        if g.startswith(tmpl):
            if not floors:
                continue
            for f in (floors if isinstance(floors, list) else range(2, 28)):
                w = v.copy(); w[:, 1] += lv[f]
                polys.append(w); mats.append(m); groups.append("F%d" % f)
            continue
        if g.startswith("ELEVATOR") or g.startswith("cabin door"):
            v = v.copy(); v[:, 1] += lv[0]
        polys.append(v); mats.append(m); groups.append(g)
    for name, m, verts, tris in d["meshes"]:
        p = np.array(verts, float).reshape(-1, 3); t = np.array(tris, int).reshape(-1, 3)
        for a, b, c in t:
            polys.append(np.stack([p[a], p[b], p[c], p[c]])); mats.append(m); groups.append(name)
    P = np.stack(polys)
    if only is not None:
        keep = np.array([only(g) for g in groups]); P = P[keep]; mats = [m for m, k in zip(mats, keep) if k]
    return P, mats, lv


def look(eye, target, up=(0, 1, 0)):
    f = np.array(target, float) - eye; f /= np.linalg.norm(f)
    r = np.cross(up, f); r /= np.linalg.norm(r); u = np.cross(f, r)  # Unity is left-handed
    return r, u, f


def render(P, mats, eye, target, fov, out, title, box=None, size=(16, 9), light=(-.4, .8, -.45), near=.25):
    eye = np.array(eye, float); r, u, f = look(eye, target)
    if box is not None:
        lo, hi = np.array(box[0]), np.array(box[1]); c = P.mean(1)
        sel = np.all((c >= lo) & (c <= hi), 1); P = P[sel]; mats = [m for m, s in zip(mats, sel) if s]
    n = np.cross(P[:, 1] - P[:, 0], P[:, 2] - P[:, 0]); nn = np.linalg.norm(n, axis=1); ok = nn > 1e-9
    n[ok] /= nn[ok, None]; c = P.mean(1)
    front = np.einsum("ij,ij->i", n, eye - c) > 0
    rel = P - eye; z = rel @ f
    vis = ok & front & np.all(z > near, 1)
    k = 1 / math.tan(math.radians(fov) / 2)
    L = np.array(light, float); L /= np.linalg.norm(L)
    idx = np.where(vis)[0]
    sx = (rel[idx] @ r) / z[idx] * k; sy = (rel[idx] @ u) / z[idx] * k
    depth = np.linalg.norm(c[idx] - eye, axis=1)
    order = np.argsort(depth)[::-1]
    shade = .55 + .45 * np.clip(n[idx] @ L, 0, 1)
    polys, cols = [], []
    for j in order:
        i = idx[j]; base = colour(mats[i]); s = 1 if ("light" in mats[i] or "led" in mats[i]) else shade[j]
        polys.append(np.stack([sx[j], sy[j]], 1)); cols.append((base[0] * s, base[1] * s, base[2] * s, base[3]))
    fig = plt.figure(figsize=size, dpi=110); ax = fig.add_axes([0, 0, 1, 1]); ax.set_facecolor((.62, .76, .90))
    ax.add_collection(PolyCollection(polys, facecolors=cols, edgecolors="none"))
    a = size[0] / size[1]; ax.set_xlim(-a, a); ax.set_ylim(-1, 1); ax.set_aspect("equal"); ax.axis("off")
    ax.text(-a + .03, .93, title, fontsize=13, color="black", bbox=dict(facecolor="white", alpha=.85))
    fig.savefig(out); plt.close(fig)
    print("wrote", out, len(polys), "faces")


def plan(P, mats, cut, out, title, half=(32, 46), floor_from=None, top=None):
    floor_from = cut - 2.6 if floor_from is None else floor_from
    top = half[0] if top is None else top
    fig = plt.figure(figsize=(16, 16 * (half[1] + top) / (2 * half[0])), dpi=110); ax = fig.add_axes([0, 0, 1, 1]); ax.set_facecolor("white")
    n = np.cross(P[:, 1] - P[:, 0], P[:, 2] - P[:, 0]); nn = np.linalg.norm(n, axis=1) + 1e-12; n /= nn[:, None]
    ymin, ymax = P[:, :, 1].min(1), P[:, :, 1].max(1)
    up = (n[:, 1] > .5) & (ymax <= cut) & (ymin >= floor_from)
    idx = np.where(up)[0]; idx = idx[np.argsort(ymax[idx])]
    ax.add_collection(PolyCollection([P[i][:, [0, 2]] for i in idx], facecolors=[colour(mats[i]) for i in idx],
                                     edgecolors=[(0, 0, 0, .12)], linewidths=.2))
    wall = (np.abs(n[:, 1]) < .2) & (ymin < cut) & (ymax > cut)
    segs, cols = [], []
    for i in np.where(wall)[0]:
        xz = P[i][:, [0, 2]]
        d = np.linalg.norm(xz[:, None] - xz[None], axis=2); a, b = np.unravel_index(d.argmax(), d.shape)
        segs.append([xz[a], xz[b]]); m = mats[i]
        cols.append((.2, .5, .9, 1) if "glass" in m else (0, 0, 0, 1))
    ax.add_collection(LineCollection(segs, colors=cols, linewidths=1.1))
    ax.set_xlim(-half[0], half[0]); ax.set_ylim(-half[1], top); ax.set_aspect("equal"); ax.axis("off")
    ax.text(.01, .97, title, transform=ax.transAxes, fontsize=13, bbox=dict(facecolor="white", alpha=.9))
    fig.savefig(out); plt.close(fig)
    print("wrote", out)


if __name__ == "__main__":
    path = sys.argv[1] if len(sys.argv) > 1 else "hotel_geometry.json"
    # perspective views: render3d.js (depth buffer); this script draws the floor plans only
    Pi, Mi, lv = load(path, floors=[2])
    plan(Pi, Mi, 1.5, "T03_plan_ground.png", half=(31, 44), top=31, title="PARTER (cut 1.5 m): wiatrolap, lobby, recepcja, bar, toalety, windy, schody, poczekalnia")
    plan(Pi, Mi, lv[1] + 1.2, "T03_plan_floor1.png", half=(21.5, 20), top=20, title="1 PIETRO: restauracja, kuchnia, SPA, zaplecze")
    plan(Pi, Mi, lv[2] + 1.2, "T03_plan_guest.png", half=(17.5, 16.5), top=16.5, title="PIETRO GOSCINNE (szablon x26): 10 pokoi, 2 korytarze 2.4 m, 3 windy, 2 klatki")
