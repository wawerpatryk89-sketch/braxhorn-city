#!/usr/bin/env python3
"""BRAXHORN GYM boxing ring for every gym and training hall in the game (owner 12.11 01:29, Tripo ring:
"on zrobił chujowy napis naszej firmy ... popraw go. Jak będzie trzeba, zamaskował. Globalnie.").

The owner's Tripo ring keeps its body (platform, corner posts, pads, steps, bulls and gold trims), but:

* the broken BRAXHORN / GYM prints on the front and back aprons (second R came out as an A, the back one in pieces)
  are masked out of the colour, normal and metal-roughness maps and replaced by REAL 3D lettering in the region's
  sign typeface (Montserrat SemiBold, SIL OFL, unity-tools/mistrzowie/fonts): BRAXHORN in polished steel, GYM in gold
  between two gold bars, bevelled solids with a graphite return (build_signs.Geometry, the same as every sign);
* the shapeless canvas logo and the wobbly octagon are masked, and the whole canvas top gets its own 4096 px print
  (≈2 mm per pixel): the owner's mark (byk_braxhorn.png traced exactly as on the A1 roof), silver plates with the
  green glow, the BRAXHORN wordmark under it, a true gold octagon;
* real metres: scale 8.0 puts the top rope 1.32 m above the canvas (a professional ring), canvas 0.86 m above the
  floor, about 7.85 x 7.8 m overall, origin at the floor in the middle of the platform, front (+Z) = the main apron.

usage: ring_braxhorn.py <owner ring .glb> <out dir>   ->  <out dir>/ring_bokserski_braxhorn.glb + _tekstury/
Python: numpy, Pillow, OpenCV, shapely, matplotlib, contourpy (same as the sign builders).
"""
import json
import math
import struct
import sys
from pathlib import Path

import cv2
import numpy as np
from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE.parent / "mistrzowie"))
from split_sheet import read_glb                                   # noqa: E402
from build_signs import Geometry, polygons                         # noqa: E402
from build_harbour_signs import sans_shape                          # noqa: E402
import build_arena_roof_bull as roof_bull                           # noqa: E402

NAME = "ring_bokserski_braxhorn"
SCALE = 8.0                       # model units -> metres (top rope 1.32 m above the canvas)
CANVAS_Y = (.095, .122)           # canvas top band (model units)
CANVAS_PX = 4096
TEXT_BOX = dict(x=.182, y0=.022, y1=.0995)   # front/back apron print to mask (model units, around x = 0)


def image_of(g, image, texinfo):
    from io import BytesIO
    return Image.open(BytesIO(image(g["textures"][texinfo["index"]]["source"])))


def raster(P, UV, I, R):
    """Rasterize every triangle in UV space: per texel 3D position, face normal (model units)."""
    T = P[I]
    n = np.cross(T[:, 1] - T[:, 0], T[:, 2] - T[:, 0]); n /= np.linalg.norm(n, axis=1)[:, None] + 1e-12
    pos = np.full((R, R, 3), np.nan, np.float32); nor = np.zeros((R, R, 3), np.float32)
    U = UV[I] * R
    for t in range(len(I)):
        a, b, c = U[t]
        x0 = max(int(math.floor(min(a[0], b[0], c[0]))) - 1, 0); x1 = min(int(math.ceil(max(a[0], b[0], c[0]))) + 1, R - 1)
        y0 = max(int(math.floor(min(a[1], b[1], c[1]))) - 1, 0); y1 = min(int(math.ceil(max(a[1], b[1], c[1]))) + 1, R - 1)
        if x1 < x0 or y1 < y0:
            continue
        xs, ys = np.meshgrid(np.arange(x0, x1 + 1) + .5, np.arange(y0, y1 + 1) + .5)
        v0 = b - a; v1 = c - a; den = v0[0] * v1[1] - v1[0] * v0[1]
        if abs(den) < 1e-9:
            continue
        px = xs - a[0]; py = ys - a[1]
        l1 = (px * v1[1] - v1[0] * py) / den; l2 = (v0[0] * py - px * v0[1]) / den; l0 = 1 - l1 - l2
        k = (l0 >= -.02) & (l1 >= -.02) & (l2 >= -.02)
        if k.any():
            pos[(ys[k] - .5).astype(int), (xs[k] - .5).astype(int)] = l0[k, None] * T[t, 0] + l1[k, None] * T[t, 1] + l2[k, None] * T[t, 2]
            nor[(ys[k] - .5).astype(int), (xs[k] - .5).astype(int)] = n[t]
    return pos, nor


def fill(img, mask, weight, k=41, grain=0.0, seed=7):
    """Normalized convolution: masked texels take the local mean of the weight texels only (no colour from the
    neighbouring UV charts); grain adds back the material's fine noise."""
    out = img.astype(np.float32).copy()
    w = weight.astype(np.float32)
    num = cv2.GaussianBlur(out * w[..., None], (k, k), 0); den = cv2.GaussianBlur(w, (k, k), 0)[..., None]
    for kk in (k * 3 + 1, k * 9 + 1):          # wide fallback where the window had no source texel
        kk |= 1
        bad = den[..., 0] < 1e-3
        if not bad.any():
            break
        num2 = cv2.GaussianBlur(out * w[..., None], (kk, kk), 0); den2 = cv2.GaussianBlur(w, (kk, kk), 0)[..., None]
        num[bad] = num2[bad]; den[bad] = den2[bad]
    mean = num / np.maximum(den, 1e-6)
    if grain:
        rng = np.random.default_rng(seed)
        noise = cv2.GaussianBlur(rng.normal(0, grain, img.shape[:2]).astype(np.float32), (3, 3), 0)[..., None]
        mean = mean + noise
    out[mask] = mean[mask]
    return np.clip(out, 0, 255).astype(np.uint8)


def luma(a):
    return a[..., 0] * .299 + a[..., 1] * .587 + a[..., 2] * .114


def mask_prints(col, nrm, mr, pos, nor):
    """Masks the broken apron prints (front and back) and the canvas logo + octagon in all three maps."""
    ok = np.isfinite(pos[..., 0])
    x, y, z = pos[..., 0], pos[..., 1], pos[..., 2]
    L = luma(col.astype(np.float32))
    bright = (L > 52) | ((col[..., 0].astype(int) - col[..., 2]) > 45)            # white letters and gold
    masks, weights = [], []
    for sg in (1, -1):
        region = ok & (nor[..., 2] * sg > .5) & (z * sg > .3) & (y < .101)
        box = region & (np.abs(x) < TEXT_BOX["x"]) & (y > TEXT_BOX["y0"]) & (y < TEXT_BOX["y1"])
        m = cv2.dilate((box & bright).astype(np.uint8), np.ones((5, 5), np.uint8)).astype(bool) & box
        masks.append(m); weights.append(region & ~bright & ~m)
    cv_ = ok & (nor[..., 1] > .9) & (y > CANVAS_Y[0]) & (y < CANVAS_Y[1])
    inner = cv_ & (np.abs(x) < .345) & (z > -.30) & (z < .325)
    m = cv2.dilate((inner & bright).astype(np.uint8), np.ones((7, 7), np.uint8)).astype(bool) & inner
    masks.append(m); weights.append(cv_ & ~bright & ~m)
    dark_std = float(np.std(L[weights[0] | weights[1]]))
    for m, w in zip(masks, weights):
        col = fill(col, m, w, grain=min(dark_std, 6.0))
        nrm = fill(nrm, m, w)
        mr = fill(mr, m, w)
    return col, nrm, mr, masks


def bull_print(draw_px, cx, cy, width_px, steel=(196, 198, 202), seam=(18, 18, 20), glow=(120, 214, 40)):
    """The owner's mark exactly as on the A1 roof (traced byk_braxhorn.png): silver plates, dark seams, green band."""
    plates = roof_bull.mark_plates()                       # metres, 80 m across
    from shapely.ops import unary_union
    from shapely.geometry import Polygon
    sil = unary_union(plates).buffer(1.6, join_style="round").buffer(-1.6, join_style="round")
    sil = unary_union([Polygon(p.exterior) for p in polygons(sil)])
    band = sil.buffer(roof_bull.LED_GAP + roof_bull.LED_W, quad_segs=8).difference(sil.buffer(roof_bull.LED_GAP, quad_segs=8))
    f = width_px / roof_bull.WIDTH

    def pts(ring):
        return [(cx + px * f, cy - py * f) for px, py in ring.coords]
    for q in polygons(band):
        draw_px.polygon(pts(q.exterior), fill=glow)
        for h in q.interiors:
            draw_px.polygon(pts(h), fill=seam)
    for q in plates:
        draw_px.polygon(pts(q.exterior), fill=steel)
        for h in q.interiors:
            draw_px.polygon(pts(h), fill=seam)


def shape_px(draw_px, g, cx, cy, f, colour, bg):
    for q in polygons(g):
        draw_px.polygon([(cx + px * f, cy - py * f) for px, py in q.exterior.coords], fill=colour)
        for h in q.interiors:
            draw_px.polygon([(cx + px * f, cy - py * f) for px, py in h.coords], fill=bg)


def canvas_print(col, nrm, mr, UV, P, I, canvas_tris, rope_c, square):
    """New 4096 px canvas: the masked original resampled through its own UVs (cloth, edge trims, corner pads), then the
    true octagon, the bull and the wordmark drawn at 4x supersampling."""
    T = P[I[canvas_tris]]
    x0, x1 = T[..., 0].min(), T[..., 0].max(); z0, z1 = T[..., 2].min(), T[..., 2].max()
    W = CANVAS_PX; H = int(round(W * (z1 - z0) / (x1 - x0)))
    mapx = np.full((H, W), -1, np.float32); mapy = np.full((H, W), -1, np.float32)
    R = col.shape[0]
    for t in canvas_tris:
        tri = P[I[t]]; uv = UV[I[t]] * R - .5
        a = np.c_[(tri[:, 0] - x0) / (x1 - x0) * W, (tri[:, 2] - z0) / (z1 - z0) * H]
        xa = max(int(a[:, 0].min()) - 1, 0); xb = min(int(a[:, 0].max()) + 2, W - 1)
        ya = max(int(a[:, 1].min()) - 1, 0); yb = min(int(a[:, 1].max()) + 2, H - 1)
        xs, ys = np.meshgrid(np.arange(xa, xb + 1) + .5, np.arange(ya, yb + 1) + .5)
        v0 = a[1] - a[0]; v1 = a[2] - a[0]; den = v0[0] * v1[1] - v1[0] * v0[1]
        if abs(den) < 1e-9:
            continue
        px = xs - a[0, 0]; py = ys - a[0, 1]
        l1 = (px * v1[1] - v1[0] * py) / den; l2 = (v0[0] * py - px * v0[1]) / den; l0 = 1 - l1 - l2
        k = (l0 >= -.01) & (l1 >= -.01) & (l2 >= -.01)
        yy = (ys[k] - .5).astype(int); xx = (xs[k] - .5).astype(int)
        mapx[yy, xx] = l0[k] * uv[0, 0] + l1[k] * uv[1, 0] + l2[k] * uv[2, 0]
        mapy[yy, xx] = l0[k] * uv[0, 1] + l1[k] * uv[1, 1] + l2[k] * uv[2, 1]
    hole = mapx < 0
    if hole.any():                                    # texels between triangles: nearest covered texel
        from scipy.ndimage import distance_transform_edt
        _, (iy, ix) = distance_transform_edt(hole, return_indices=True)
        mapx = mapx[iy, ix]; mapy = mapy[iy, ix]
    maps = [cv2.remap(np.ascontiguousarray(a), mapx, mapy, cv2.INTER_CUBIC, borderMode=cv2.BORDER_REPLICATE) for a in (col, nrm, mr)]
    # inside the ropes the canvas is one clean stretched cloth (no ghost of the old print in colour or relief)
    gx = x0 + (np.arange(W) + .5) / W * (x1 - x0); gz = z0 + (np.arange(H) + .5) / H * (z1 - z0)
    X, Z = np.meshgrid(gx, gz)
    rx, zb, zf = square
    inset = .006
    dist = np.minimum.reduce([rx - inset - np.abs(X), Z - (zb + inset), (zf - inset) - Z]) * W / (x1 - x0)   # px inside
    a = np.clip(dist / 8.0, 0, 1)[..., None]
    inside = dist > 0
    cloth = np.median(maps[0][inside & (luma(maps[0].astype(np.float32)) < 45)], axis=0)
    rng = np.random.default_rng(11)
    low = cv2.GaussianBlur(rng.normal(0, 1, (H // 64 + 1, W // 64 + 1)).astype(np.float32), (3, 3), 0)
    low = cv2.resize(low, (W, H), interpolation=cv2.INTER_CUBIC)[..., None] * 2.2
    fine = cv2.GaussianBlur(rng.normal(0, 2.0, (H, W)).astype(np.float32), (3, 3), 0)[..., None]
    clean = np.clip(cloth[None, None, :] + low + fine, 0, 255)
    maps[0] = (maps[0] * (1 - a) + clean * a).astype(np.uint8)
    flat = np.array([128, 128, 255], np.float32) + np.concatenate([fine * .6, fine * .6, np.zeros_like(fine)], 2)
    maps[1] = np.clip(maps[1] * (1 - a) + flat * a, 0, 255).astype(np.uint8)
    maps[2] = (maps[2] * (1 - a) + np.array([255, 178, 0], np.float32) * a).astype(np.uint8)   # cloth: rough .7, no metal
    base = Image.fromarray(maps[0])
    # artwork at 4x, downsampled (clean anti-aliased edges)
    SS = 2
    art = Image.new("RGBA", (W * SS, H * SS), (0, 0, 0, 0)); d = ImageDraw.Draw(art)
    upm = W * SS / (x1 - x0)                              # pixels per model unit
    cx = (rope_c[0] - x0) * upm; cy = (rope_c[1] - z0) * upm
    gold = (214, 160, 52, 255)
    r_out = .262 * upm; lw = .0115 * upm
    oct_out = [(cx + r_out * math.cos(math.radians(22.5 + 45 * i)), cy + r_out * math.sin(math.radians(22.5 + 45 * i))) for i in range(8)]
    r_in = r_out - lw / math.cos(math.radians(22.5))
    oct_in = [(cx + r_in * math.cos(math.radians(22.5 + 45 * i)), cy + r_in * math.sin(math.radians(22.5 + 45 * i))) for i in range(8)]
    d.polygon(oct_out, fill=gold); d.polygon(oct_in, fill=(0, 0, 0, 0))
    # bull, read from the front apron (+Z, image bottom): head up = toward -Z = image top
    bull_print(d, cx, cy - .035 * upm, .30 * upm, steel=(200, 202, 206, 255), seam=(0, 0, 0, 0), glow=(118, 210, 38, 255))
    word = sans_shape("BRAXHORN", 1.0, 99.0, .16)
    wx0, wy0, wx1, wy1 = word.bounds
    f = .27 * upm / (wx1 - wx0)
    shape_px(d, word, cx, cy + .175 * upm, f, (200, 202, 206, 255), (0, 0, 0, 0))
    art = art.resize((W, H), Image.LANCZOS)
    base.paste(art, (0, 0), art)
    ink = np.asarray(art)[..., 3] > 8
    n = maps[1].copy(); n[ink] = (n[ink].astype(np.float32) * .25 + np.array([128, 128, 255]) * .75).astype(np.uint8)
    m = maps[2].copy(); m[ink, 1] = 120; m[ink, 2] = 0                       # printed vinyl: satin, not metal
    return base, Image.fromarray(n), Image.fromarray(m), (x0, x1, z0, z1)


def letters(face_z_front, face_z_back):
    """3D lettering for both aprons, metres. Returns {role: (pos, nrm, idx)}; roles steel, gold, dark."""
    out = {}

    def add(geo, rot180, zface):
        for role, m in geo.roles.items():
            p = np.array(m["positions"], np.float64).reshape(-1, 3); nn = np.array(m["normals"], np.float64).reshape(-1, 3)
            p[:, 2] += zface
            if rot180:
                p[:, 0] *= -1; p[:, 2] *= -1; nn[:, 0] *= -1; nn[:, 2] *= -1
            t = np.array(m["triangles"], np.int64).reshape(-1, 3)
            r = {"face": "steel", "gold": "gold", "dark": "dark"}.get(role, role)
            o = out.setdefault(r, [[], [], [], 0])
            o[0].append(p); o[1].append(nn); o[2].append(t + o[3]); o[3] += len(p)
    for rot, zf in ((False, face_z_front), (True, face_z_back)):
        g1 = Geometry(); g2 = Geometry()
        word = sans_shape("BRAXHORN", .26, 99.0, .16)
        word = _translate(word, 0, .50)
        g1.solid(word, -.012, .022, front="face", side="dark", bevel=.004, bevel_role="face")
        gym = sans_shape("GYM", .135, 99.0, .30)
        gym = _translate(gym, 0, .285)
        g2.solid(gym, -.012, .018, front="gold", side="dark", bevel=.003, bevel_role="gold")
        gw = gym.bounds[2] - gym.bounds[0]
        from shapely.geometry import box
        for s in (-1, 1):
            xa = s * (gw / 2 + .14); xb = s * (gw / 2 + .14 + .78)
            g2.solid(box(min(xa, xb), .335, max(xa, xb), .372), -.012, .014, front="gold", side="dark", bevel=.003, bevel_role="gold")
        add(g1, rot, zf); add(g2, rot, zf)
    return {r: (np.vstack(v[0]), np.vstack(v[1]), np.vstack(v[2])) for r, v in out.items()}


ROPE_R, ROPE_SAG = .031, .010   # padded rope cover 6.2 cm; heights come from the owner's model (its rope ends at the posts)
ROPE_ROLES = ("rope_gold", "rope_black", "rope_gold", "rope_black")   # top rope first


def tube(a, b, r, sag=0.0, seg=20, steps=10):
    """Closed-ring tube from a to b (no caps: the ends sit inside the corner pads), smooth normals."""
    a = np.asarray(a, float); b = np.asarray(b, float)
    d = b - a; L = np.linalg.norm(d); t = d / L
    up = np.array([0, 1.0, 0]); side = np.cross(t, up); side /= np.linalg.norm(side); up2 = np.cross(side, t)
    P, N, T = [], [], []
    for i in range(steps + 1):
        u = i / steps
        c = a + d * u - up * sag * 4 * u * (1 - u)
        for j in range(seg):
            ang = 2 * math.pi * j / seg
            n = side * math.cos(ang) + up2 * math.sin(ang)
            P.append(c + n * r); N.append(n)
    for i in range(steps):
        for j in range(seg):
            a0 = i * seg + j; a1 = i * seg + (j + 1) % seg; b0 = a0 + seg; b1 = a1 + seg
            T += [(a0, b0, a1), (a1, b0, b1)]
    return np.array(P), np.array(N), np.array(T)


def box_mesh(c, hx, hy, hz, axis_x):
    """Axis-aligned box in a frame whose x is axis_x (horizontal), flat normals."""
    ax = np.asarray(axis_x, float); ax /= np.linalg.norm(ax); ay = np.array([0, 1.0, 0]); az = np.cross(ax, ay)
    P, N, T = [], [], []
    for n, (u, v, w) in ((ax, (ay, az, hx)), (-ax, (az, ay, hx)), (ay, (az, ax, hy)), (-ay, (ax, az, hy)), (az, (ax, ay, hz)), (-az, (ay, ax, hz))):
        hu = hy if u is ay else hz if u is az else hx
        hv = hy if v is ay else hz if v is az else hx
        base = len(P)
        for su, sv in ((-1, -1), (1, -1), (1, 1), (-1, 1)):
            P.append(np.asarray(c) + n * w + u * su * hu + v * sv * hv); N.append(n)
        T += [(base, base + 1, base + 2), (base, base + 2, base + 3)]
    return np.array(P), np.array(N), np.array(T)


def ropes(rx, zb, zf, canvas_m, heights):
    """Four real ropes per side (metres), alternate gold and black vinyl, two leather rope ties per side."""
    out = {}

    def put(role, mesh):
        o = out.setdefault(role, [[], [], [], 0])
        p, n, t = mesh; o[0].append(p); o[1].append(n); o[2].append(t + o[3]); o[3] += len(p)
    X, ZB, ZF = rx * SCALE, zb * SCALE, zf * SCALE
    corners = [(-X, ZF), (X, ZF), (X, ZB), (-X, ZB)]
    for k in range(4):
        (ax_, az_), (bx_, bz_) = corners[k], corners[(k + 1) % 4]
        for h, role in zip(sorted(heights, reverse=True), ROPE_ROLES):
            y = canvas_m + h
            put(role, tube((ax_, y, az_), (bx_, y, bz_), ROPE_R, ROPE_SAG))
        d = np.array([bx_ - ax_, 0, bz_ - az_]); L = np.linalg.norm(d); t = d / L
        outward = np.cross(t, [0, 1.0, 0]); outward /= np.linalg.norm(outward)
        mid = np.array([(ax_ + bx_) / 2, 0, (az_ + bz_) / 2])
        if np.dot(outward, mid) < 0:
            outward = -outward
        for f in (-1 / 6, 1 / 6):
            c = mid + t * L * f
            ys = [canvas_m + h for h in heights]
            y0, y1 = min(ys) - .045, max(ys) + .045
            put("strap", box_mesh(c + outward * (ROPE_R + .006) + np.array([0, (y0 + y1) / 2, 0]), .024, (y1 - y0) / 2, .005, t))
            for y in ys:
                sag = ROPE_SAG * 4 * (.5 + f) * (.5 - f)
                put("strap", tube(c - t * .025 + np.array([0, y - sag, 0]), c + t * .025 + np.array([0, y - sag, 0]), ROPE_R + .005, 0, 20, 1))
    return {r: (np.vstack(v[0]), np.vstack(v[1]), np.vstack(v[2])) for r, v in out.items()}


def old_ropes(P, I, rx, zb, zf):
    """Triangles of the Tripo ropes and their blob spacers (between the corner pads, rope band only)."""
    V = P[I]
    y = V[..., 1]; x = V[..., 0]; z = V[..., 2]
    band = (y > .124).all(1) & (y < .292).all(1)
    front = (np.abs(z - zf) < .03).all(1) & (np.abs(x) < .332).all(1)
    back = (np.abs(z - zb) < .03).all(1) & (np.abs(x) < .332).all(1)
    sides = (np.abs(np.abs(x) - rx) < .03).all(1) & (z > -.279).all(1) & (z < .279).all(1)
    return band & (front | back | sides)


def _translate(g, dx, dy):
    from shapely.affinity import translate
    return translate(g, dx, dy)


def write(path, prims, materials, images):
    """GLB with several primitives; images are external URIs (as split_sheet.py)."""
    blobs, views, accs = [], [], []

    def add(arr, target, typ, ctype, minmax=False):
        data = arr.tobytes(); pad = (-len(data)) % 4; off = sum(len(x) for x in blobs)
        blobs.append(data + b"\0" * pad)
        views.append({"buffer": 0, "byteOffset": off, "byteLength": len(data), "target": target})
        a = {"bufferView": len(views) - 1, "componentType": ctype, "count": len(arr), "type": typ}
        if minmax:
            a["min"] = arr.min(0).tolist(); a["max"] = arr.max(0).tolist()
        accs.append(a); return len(accs) - 1
    gp = []
    for pos, nrm, uv, idx, mat in prims:
        att = {"POSITION": add(pos.astype(np.float32), 34962, "VEC3", 5126, True),
               "NORMAL": add(nrm.astype(np.float32), 34962, "VEC3", 5126)}
        if uv is not None:
            att["TEXCOORD_0"] = add(uv.astype(np.float32), 34962, "VEC2", 5126)
        ii = add(idx.reshape(-1).astype(np.uint32), 34963, "SCALAR", 5125)
        gp.append({"attributes": att, "indices": ii, "material": mat})
    textures = [{"sampler": 0, "source": i} for i in range(len(images))]
    g = {"asset": {"version": "2.0", "generator": "BRAXHORN ring_braxhorn.py"}, "scene": 0, "scenes": [{"nodes": [0]}],
         "nodes": [{"name": NAME, "mesh": 0}], "meshes": [{"name": NAME, "primitives": gp}], "materials": materials,
         "samplers": [{"magFilter": 9729, "minFilter": 9987, "wrapS": 33071, "wrapT": 33071}],
         "images": [{"uri": u} for u in images], "textures": textures,
         "bufferViews": views, "accessors": accs, "buffers": [{"byteLength": sum(len(x) for x in blobs)}]}
    js = json.dumps(g, separators=(",", ":")).encode(); js += b" " * ((-len(js)) % 4)
    b = b"".join(blobs)
    Path(path).write_bytes(struct.pack("<III", 0x46546C67, 2, 28 + len(js) + len(b)) + struct.pack("<II", len(js), 0x4E4F534A) + js
                          + struct.pack("<II", len(b), 0x004E4942) + b)


def main(src, out_dir):
    out = Path(out_dir); (out / "_tekstury").mkdir(parents=True, exist_ok=True)
    g, acc, image = read_glb(src)
    pr = g["meshes"][0]["primitives"][0]
    P = acc(pr["attributes"]["POSITION"]).astype(np.float64); N = acc(pr["attributes"]["NORMAL"]).astype(np.float64)
    UV = acc(pr["attributes"]["TEXCOORD_0"]).astype(np.float64); I = acc(pr["indices"]).reshape(-1, 3).astype(np.int64)
    off = np.array(g["nodes"][0].get("translation", [0, 0, 0]))
    P = P + off
    mat = g["materials"][0]
    col = np.asarray(image_of(g, image, mat["pbrMetallicRoughness"]["baseColorTexture"]).convert("RGB"))
    nrm = np.asarray(image_of(g, image, mat["normalTexture"]).convert("RGB"))
    mr = np.asarray(image_of(g, image, mat["pbrMetallicRoughness"]["metallicRoughnessTexture"]).convert("RGB").resize(col.shape[1::-1]))
    R = col.shape[0]
    pos, nor = raster(P, UV, I, R)
    col, nrm, mr, masks = mask_prints(col, nrm, mr, pos, nor)

    T = P[I]; c = T.mean(1)
    fn = np.cross(T[:, 1] - T[:, 0], T[:, 2] - T[:, 0]); fn /= np.linalg.norm(fn, axis=1)[:, None] + 1e-12
    canvas = np.nonzero((fn[:, 1] > .95) & (c[:, 1] > CANVAS_Y[0]) & (c[:, 1] < CANVAS_Y[1]))[0]
    q = (c[:, 1] > .13) & (c[:, 1] < .29)
    zf = float(np.median(c[q & (np.abs(c[:, 0]) > .15) & (np.abs(c[:, 0]) < .30) & (c[:, 2] > .25) & (c[:, 2] < .40), 2]))
    zb = float(np.median(c[q & (np.abs(c[:, 0]) > .15) & (np.abs(c[:, 0]) < .30) & (c[:, 2] < -.25) & (c[:, 2] > -.40), 2]))
    rx = float(np.median(np.abs(c[q & (np.abs(c[:, 2]) > .10) & (np.abs(c[:, 2]) < .25) & (np.abs(c[:, 0]) > .33) & (np.abs(c[:, 0]) < .42), 0])))
    rz = (zf + zb) / 2
    cbase, cnrm, cmr, (x0, x1, z0, z1) = canvas_print(col, nrm, mr, UV, P, I, canvas, (0.0, rz), (rx, zb, zf))

    # apron faces where the letters stand
    def face(sg):
        k = np.isfinite(pos[..., 0]) & (nor[..., 2] * sg > .8) & (np.abs(pos[..., 0]) < .15) & (pos[..., 1] > .03) & (pos[..., 1] < .095)
        return float(np.percentile(pos[k][:, 2] * sg, 50)) * SCALE
    af, ab = face(1), face(-1)
    L = letters(af, ab)

    # body without the canvas triangles + canvas with planar UVs, metres
    keep = np.ones(len(I), bool); keep[canvas] = False
    gone = old_ropes(P, I, rx, zb, zf); keep &= ~gone
    canvas_m = float(np.percentile(c[canvas, 1], 50)) * SCALE
    sel = (np.abs(P[:, 0]) > .18) & (np.abs(P[:, 0]) < .28) & (np.abs(P[:, 2] - zf) < .03) & (P[:, 1] > .124) & (P[:, 1] < .292)
    gy = np.sort(P[sel, 1])                        # rope vertices between the ties: four bands with clear gaps
    cuts = np.nonzero(np.diff(gy) > .012)[0]
    bands = np.split(gy, cuts + 1)
    heights = tuple(round(float(np.median(b)) * SCALE - canvas_m, 3) for b in bands if len(b) > 20)
    assert len(heights) == 4, heights
    RP = ropes(rx, zb, zf, canvas_m, heights)
    Pm = P * SCALE
    body = I[keep]
    cvI = I[canvas]
    used = np.unique(cvI); remap = -np.ones(len(P), np.int64); remap[used] = np.arange(len(used))
    cvP = Pm[used]; cvN = N[used]
    cvUV = np.c_[(P[used, 0] - x0) / (x1 - x0), (P[used, 2] - z0) / (z1 - z0)]
    pref = NAME
    Image.fromarray(col).save(out / "_tekstury" / (pref + "_kolor.jpg"), quality=92)
    Image.fromarray(nrm).save(out / "_tekstury" / (pref + "_normal.png"), optimize=True)
    Image.fromarray(mr).resize((1024, 1024), Image.LANCZOS).save(out / "_tekstury" / (pref + "_mr.png"), optimize=True)
    cbase.save(out / "_tekstury" / (pref + "_mata_kolor.jpg"), quality=92)
    cnrm.resize((2048, int(2048 * cnrm.size[1] / cnrm.size[0])), Image.LANCZOS).save(out / "_tekstury" / (pref + "_mata_normal.png"), optimize=True)
    cmr.resize((1024, int(1024 * cmr.size[1] / cmr.size[0])), Image.LANCZOS).save(out / "_tekstury" / (pref + "_mata_mr.png"), optimize=True)
    images = ["_tekstury/" + pref + s for s in ("_kolor.jpg", "_normal.png", "_mr.png", "_mata_kolor.jpg", "_mata_normal.png", "_mata_mr.png")]
    mf = mat["pbrMetallicRoughness"].get("metallicFactor", 1.0)
    materials = [
        {"name": "braxhorn ring", "doubleSided": True, "normalTexture": {"index": 1},
         "pbrMetallicRoughness": {"baseColorTexture": {"index": 0}, "metallicRoughnessTexture": {"index": 2}, "metallicFactor": mf, "roughnessFactor": 1.0}},
        {"name": "braxhorn ring mata", "doubleSided": False, "normalTexture": {"index": 4},
         "pbrMetallicRoughness": {"baseColorTexture": {"index": 3}, "metallicRoughnessTexture": {"index": 5}, "metallicFactor": 1.0, "roughnessFactor": 1.0}},
        {"name": "braxhorn ring litery stal", "pbrMetallicRoughness": {"baseColorFactor": [.93, .93, .94, 1], "metallicFactor": .35, "roughnessFactor": .28}},
        {"name": "braxhorn ring litery zloto", "pbrMetallicRoughness": {"baseColorFactor": [.95, .70, .26, 1], "metallicFactor": .55, "roughnessFactor": .30}},
        {"name": "braxhorn ring liny zloto", "pbrMetallicRoughness": {"baseColorFactor": [.78, .52, .12, 1], "metallicFactor": .35, "roughnessFactor": .30}},
        {"name": "braxhorn ring liny czarne", "pbrMetallicRoughness": {"baseColorFactor": [.018, .018, .02, 1], "metallicFactor": 0.0, "roughnessFactor": .26}},
        {"name": "braxhorn ring pasy skora", "pbrMetallicRoughness": {"baseColorFactor": [.02, .018, .018, 1], "metallicFactor": 0.0, "roughnessFactor": .42}},
        {"name": "braxhorn ring litery grafit", "pbrMetallicRoughness": {"baseColorFactor": [.06, .06, .065, 1], "metallicFactor": .5, "roughnessFactor": .45}},
    ]
    prims = [(Pm, N, UV, body, 0), (cvP, cvN, cvUV, remap[cvI], 1)]
    for role, mi in (("steel", 2), ("gold", 3), ("dark", 7)):
        if role in L:
            p, n, t = L[role]; prims.append((p, n, None, t, mi))
    for role, mi in (("rope_gold", 4), ("rope_black", 5), ("strap", 6)):
        p, n, t = RP[role]; prims.append((p, n, None, t, mi))
    write(out / (NAME + ".glb"), prims, materials, images)
    size = (Pm.max(0) - Pm.min(0)).round(2).tolist()
    kat = {"pack": "silownia_ring_01", "items": [{"name": NAME, "size_m": size, "canvas_m": round(float(np.percentile(c[canvas, 1], 50)) * SCALE, 3),
           "ropes_square_m": [round(2 * rx * SCALE, 2), round((zf - zb) * SCALE, 2)], "rope_heights_m": list(heights),
           "old_rope_triangles_removed": int(gone.sum()), "front": "+Z (BRAXHORN GYM)",
           "masked_texels": int(sum(m.sum() for m in masks))}]}
    (out / "katalog.json").write_text(json.dumps(kat, ensure_ascii=False, indent=1) + "\n")
    print(json.dumps(kat, ensure_ascii=False))


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
