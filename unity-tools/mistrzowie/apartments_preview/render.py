"""Preview renderer for the stub-generated apartment geometry (painter's algorithm)."""
import json, sys, math
import numpy as np
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib.collections import PolyCollection

COL = [("glass", (.55, .68, .78, .45)), ("clear_glass", (.80, .90, .95, .30)), ("light", (1, .86, .55, 1)), ("arena_gold", (.83, .66, .30, 1)),
       ("white_render", (.95, .95, .93, 1)), ("grey_panel", (.55, .56, .58, 1)), ("granite_tile_04", (.82, .82, .80, 1)),
       ("granite", (.30, .31, .33, 1)), ("timber", (.80, .55, .33, 1)), ("walnut", (.42, .28, .19, 1)),
       ("arena_black", (.08, .08, .09, 1)), ("steel", (.65, .66, .68, 1)), ("lawn", (.35, .55, .25, 1)),
       ("paving", (.78, .78, .76, 1)), ("oak", (.78, .62, .44, 1)), ("wall_paint", (.93, .90, .85, 1)),
       ("sufit", (.97, .97, .97, 1)), ("bath_floor", (.85, .85, .85, 1)), ("bath_marble", (.92, .92, .94, 1)),
       ("terrazzo", (.88, .87, .85, 1)), ("Marble016", (.10, .10, .11, 1))]

def colour(m):
    for k, c in COL:
        if k in m: return c
    return (1, 0, 1, 1)

def load(path, groups=None):
    d = json.load(open(path)); out = []
    for q in d["quads"]:
        g, m, ins = q[0], q[1], q[2]
        if groups and not any(g.startswith(x) for x in groups): continue
        v = np.array(q[3:], dtype=float).reshape(4, 3)
        out.append((g, m, v))
    return out

def look(eye, target, up=(0, 1, 0)):
    f = np.array(target, float) - eye; f /= np.linalg.norm(f)
    # Unity is left-handed: right = up x forward, up' = forward x right
    r = np.cross(up, f); r /= np.linalg.norm(r); u = np.cross(f, r)
    return r, u, f

def render(quads, eye, target, fov, out, title, size=(16, 9), clip=None, light=(-.4, .8, -.45)):
    eye = np.array(eye, float); r, u, f = look(eye, target)
    L = np.array(light); L /= np.linalg.norm(L)
    polys, cols, depth = [], [], []
    k = 1 / math.tan(math.radians(fov) / 2)
    for g, m, v in quads:
        if clip and not clip(v): continue
        n = np.cross(v[1] - v[0], v[2] - v[0]); nn = np.linalg.norm(n)
        if nn < 1e-9: continue
        n /= nn
        c = v.mean(0)
        if np.dot(n, eye - c) <= 0: continue           # back-face cull
        rel = v - eye; z = rel @ f
        if (z < .3).any(): continue
        p = np.stack([(rel @ r) / z * k, (rel @ u) / z * k], 1)
        base = colour(m); s = .55 + .45 * max(0, float(np.dot(n, L)))
        if "light" in m: s = 1
        polys.append(p); cols.append((base[0] * s, base[1] * s, base[2] * s, base[3])); depth.append(np.linalg.norm(c - eye))
    order = np.argsort(depth)[::-1]
    fig = plt.figure(figsize=size, dpi=110); ax = fig.add_axes([0, 0, 1, 1]); ax.set_facecolor((.62, .76, .90))
    ax.add_collection(PolyCollection([polys[i] for i in order], facecolors=[cols[i] for i in order], edgecolors="none"))
    a = size[0] / size[1]; ax.set_xlim(-a, a); ax.set_ylim(-1, 1); ax.set_aspect("equal"); ax.axis("off")
    ax.text(-a + .03, .93, title, fontsize=13, color="black", bbox=dict(facecolor="white", alpha=.8))
    fig.savefig(out); plt.close(fig)

def plan(quads, y0, y1, out, title, half=(20, 13)):
    fig = plt.figure(figsize=(16, 9), dpi=110); ax = fig.add_axes([0, 0, 1, 1]); ax.set_facecolor("white")
    items = []
    for g, m, v in quads:
        if g.startswith("exterior details"): continue
        if v[:, 1].min() < y0 - .02 or v[:, 1].min() > y1: continue
        n = np.cross(v[1] - v[0], v[2] - v[0])
        if np.linalg.norm(n) < 1e-9: continue
        if v[:, 1].max() > y1:                                     # cut walls at the section height
            if abs(n[1]) > 1e-6: continue
            items.append((y1, v[:, [0, 2]], colour(m)))
        else:
            if n[1] <= 0: continue
            items.append((v[:, 1].max(), v[:, [0, 2]], colour(m)))
    items.sort(key=lambda t: t[0])
    ax.add_collection(PolyCollection([i[1] for i in items], facecolors=[i[2] for i in items], edgecolors=[(0, 0, 0, .15)], linewidths=.2))
    from matplotlib.collections import LineCollection
    cut = (y0 + y1) * .5 + .3
    segs, cl = [], []
    for g, m, v in quads:
        if g.startswith("exterior details"): continue
        if not (v[:, 1].min() < cut < v[:, 1].max()): continue
        n = np.cross(v[1] - v[0], v[2] - v[0])
        if np.linalg.norm(n) < 1e-9 or abs(n[1]) / np.linalg.norm(n) > .1: continue
        xz = v[:, [0, 2]]; a, b = xz.min(0), xz.max(0)
        if np.linalg.norm(b - a) < .02: continue
        segs.append([a, b])
        c = colour(m); cl.append((c[0] * .45, c[1] * .45, c[2] * .45, max(.5, c[3])))
    ax.add_collection(LineCollection(segs, colors=cl, linewidths=1.6))
    # true top view of a left-handed scene, turned so the front (-z) is at the top: +x on the left
    ax.set_xlim(half[0], -half[0]); ax.set_ylim(half[1], -half[1]); ax.set_aspect("equal"); ax.axis("off")
    ax.text(half[0] - .3, -half[1] + 1, title, fontsize=13, bbox=dict(facecolor="white", alpha=.8))
    fig.savefig(out); plt.close(fig)

if __name__ == "__main__":
    bid = sys.argv[1]; d = f"{bid}_geometry.json"
    allq = load(d)
    render(allq, (-24, 6.5, -44), (0, 6, -2), 50, f"{bid}_front34.png", f"{bid} front 3/4 (preview from generator code)")
    render(allq, (22, 9, 40), (0, 6, 2), 50, f"{bid}_rear34.png", f"{bid} rear 3/4")
    level1 = 4.5
    plan(allq, level1, level1 + 1.25, f"{bid}_plan_floor1.png", f"{bid} floor 1 plan cut at 1.25 m (front = top)")
    plan(allq, .30, .30 + 1.25, f"{bid}_plan_ground.png", f"{bid} ground floor plan cut at 1.25 m (front = top)")
