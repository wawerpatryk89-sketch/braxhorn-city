"""Hull liveries for the owner's yacht (yacht.glb, Sergei, CC BY 4.0) - owner 10.11: "zrób te dwa jachty, modele ...
dużo ich zrób ... naklejki na nich narobić, coś, żeby troszeczkę się różniły".

The yacht is one mesh with one 2048 x 2048 atlas. A livery repaints ONLY the painted hull topsides in that atlas:

* the visible outer shell is found with orthographic depth buffers from both sides, the bow and the stern (only the
  first surface each ray meets - never an inner wall);
* every atlas pixel of a shell triangle gets its 3D position (texture-space rasterisation) and is repainted when it
  lies below the sheer line (3.8 m above the model datum aft, rising to 5.4 m at the bow: the flared bow
  is painted to its bulwark) on a side / stern
  facing triangle;
* only neutral paint is repainted (white / light grey, saturation < 0.12): windows, portholes, the black antifouling,
  teak and stainless keep their own pixels. The shading of the original paint is kept (colour x luminance).

UV islands of the shell share 0.8 % of their pixels with other parts (measured 10.11) - the livery cannot bleed onto
the superstructure. Output (generated in CI, never committed): Assets/Source/HarborBeach/Liveries/yacht_livery_<id>.png,
read by BraxChampionsHarbour (each of the 15 yachts: one of 5 hulls + its own 3D name on the transom).

usage: python3 build_yacht_liveries.py [out_dir]
"""
import hashlib
import io
import json
import struct
import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
GLB = ROOT / "unity-city/Assets/Source/HarborBeach/Models/yacht.glb"
OUT = ROOT / "unity-city/Assets/Source/HarborBeach/Liveries"
YACHT_GLB_SHA256 = "97b60c22214c00f07a0742a9f640b7f09fe956f9681b41416f74425e3d88416d"   # the verified original in the repository
# id -> linear-ish sRGB paint colour of the hull (the original white shading is multiplied in)
LIVERIES = {
    "navy": (.07, .12, .30),        # GRANAT
    "black": (.035, .037, .042),    # CZERŃ (piano black)
    "emerald": (.04, .20, .14),     # SZMARAGD
    "sapphire": (.08, .21, .50),    # SZAFIR
}
SHEER_AFT, SHEER_BOW = 380.0, 540.0   # model units (cm), keel datum of the GLB mesh
KEEL_FLOOR = -260.0


def load(path=GLB):
    b = path.read_bytes()
    clen = struct.unpack("<I", b[12:16])[0]
    js = json.loads(b[20:20 + clen])
    off = 20 + clen
    blen = struct.unpack("<I", b[off:off + 4])[0]
    data = b[off + 8:off + 8 + blen]

    def acc(i):
        a = js["accessors"][i]
        bv = js["bufferViews"][a["bufferView"]]
        comp = {5126: np.float32, 5123: np.uint16, 5125: np.uint32, 5121: np.uint8}[a["componentType"]]
        n = {"SCALAR": 1, "VEC2": 2, "VEC3": 3, "VEC4": 4}[a["type"]]
        o = bv.get("byteOffset", 0) + a.get("byteOffset", 0)
        size = np.dtype(comp).itemsize * n
        stride = bv.get("byteStride", 0)
        cnt = a["count"]
        if stride and stride != size:
            raw = np.frombuffer(data[o:o + stride * cnt], dtype=np.uint8).reshape(cnt, stride)[:, :size].copy()
            return raw.view(comp).reshape(cnt, n)
        return np.frombuffer(data[o:o + size * cnt], dtype=comp).reshape(cnt, n)

    if len(js["meshes"]) != 1 or len(js["meshes"][0]["primitives"]) != 1:
        raise SystemExit("yacht.glb changed: expected one mesh with one primitive")
    pr = js["meshes"][0]["primitives"][0]
    P = acc(pr["attributes"]["POSITION"]).astype(float)
    UV = acc(pr["attributes"]["TEXCOORD_0"]).astype(float)
    I = acc(pr["indices"]).reshape(-1, 3).astype(np.int64)
    tex = js["materials"][pr.get("material", 0)]["pbrMetallicRoughness"]["baseColorTexture"]["index"]
    img = js["images"][js["textures"][tex]["source"]]
    bv = js["bufferViews"][img["bufferView"]]
    png = data[bv.get("byteOffset", 0):bv.get("byteOffset", 0) + bv["byteLength"]]
    return P, UV, I, png


def _zbuffer(P2, depth, I, res):
    lo = P2.min(0)
    W = int((P2[:, 0].max() - lo[0]) / res) + 2
    H = int((P2[:, 1].max() - lo[1]) / res) + 2
    zb = np.full((H, W), -np.inf)
    tb = np.full((H, W), -1, np.int64)
    for t, tri in enumerate(I):
        q = (P2[tri] - lo) / res
        dz = depth[tri]
        x0, y0 = np.floor(q.min(0)).astype(int)
        x1, y1 = np.ceil(q.max(0)).astype(int)
        xs, ys = np.meshgrid(np.arange(x0, x1 + 1) + .5, np.arange(y0, y1 + 1) + .5)
        v0, v1 = q[1] - q[0], q[2] - q[0]
        d = v0[0] * v1[1] - v0[1] * v1[0]
        if abs(d) < 1e-9:
            continue
        px, py = xs - q[0, 0], ys - q[0, 1]
        l1 = (px * v1[1] - py * v1[0]) / d
        l2 = (v0[0] * py - v0[1] * px) / d
        l0 = 1 - l1 - l2
        m = (l0 >= 0) & (l1 >= 0) & (l2 >= 0)
        if not m.any():
            continue
        z = l0 * dz[0] + l1 * dz[1] + l2 * dz[2]
        yy, xx = (ys - .5).astype(int), (xs - .5).astype(int)
        upd = m & (z > zb[yy, xx])
        zb[yy[upd], xx[upd]] = z[upd]
        tb[yy[upd], xx[upd]] = t
    return tb


def visible_shell(P, I, res=4.0):
    """Triangles seen first from port, starboard, bow and stern (model: x = length, y = up, z = beam)."""
    vis = np.zeros(len(I), bool)
    for axis, sign in ((2, 1), (2, -1), (0, 1), (0, -1)):
        other = [k for k in (0, 1, 2) if k != axis]
        tb = _zbuffer(P[:, other], sign * P[:, axis], I, res)
        vis[np.unique(tb[tb >= 0])] = True
    return vis


def texture_positions(P, UV, I, W, H):
    """3D position, face normal and triangle of every atlas pixel (last triangle wins on a shared pixel)."""
    pos = np.full((H, W, 3), np.nan, np.float32)
    nrm = np.zeros((H, W, 3), np.float32)
    tri = np.full((H, W), -1, np.int64)
    TN = np.cross(P[I][:, 1] - P[I][:, 0], P[I][:, 2] - P[I][:, 0])
    TN /= np.linalg.norm(TN, axis=1)[:, None] + 1e-12
    for t, (a, b, c) in enumerate(I):
        uv = UV[[a, b, c]] * [W, H]
        x0, y0 = np.maximum(np.floor(uv.min(0)).astype(int), 0)
        x1, y1 = np.minimum(np.ceil(uv.max(0)).astype(int), [W - 1, H - 1])
        if x1 < x0 or y1 < y0:
            continue
        xs, ys = np.meshgrid(np.arange(x0, x1 + 1) + .5, np.arange(y0, y1 + 1) + .5)
        v0, v1 = uv[1] - uv[0], uv[2] - uv[0]
        d = v0[0] * v1[1] - v0[1] * v1[0]
        if abs(d) < 1e-9:
            continue
        px, py = xs - uv[0, 0], ys - uv[0, 1]
        l1 = (px * v1[1] - py * v1[0]) / d
        l2 = (v0[0] * py - v0[1] * px) / d
        l0 = 1 - l1 - l2
        m = (l0 >= -.02) & (l1 >= -.02) & (l2 >= -.02)
        if not m.any():
            continue
        w = np.stack([l0, l1, l2], -1)[m]
        yy, xx = (ys[m] - .5).astype(int), (xs[m] - .5).astype(int)
        pos[yy, xx] = w @ P[[a, b, c]]
        nrm[yy, xx] = TN[t]
        tri[yy, xx] = t
    return pos, nrm, tri


def hull_mask(P, UV, I, W, H):
    vis = visible_shell(P, I)
    pos, nrm, tri = texture_positions(P, UV, I, W, H)
    ok = (tri >= 0) & vis[np.where(tri >= 0, tri, 0)]
    x, y = np.nan_to_num(pos[..., 0]), np.nan_to_num(pos[..., 1])
    x0, x1 = P[:, 0].min(), P[:, 0].max()
    t = np.clip((x - (x0 + .65 * (x1 - x0))) / (.35 * (x1 - x0)), 0, 1)
    sheer = SHEER_AFT + (SHEER_BOW - SHEER_AFT) * t * t * (3 - 2 * t)
    return ok & (np.abs(nrm[..., 1]) < .7) & (y < sheer) & (y > KEEL_FLOOR)


def repaint(base, mask, colour):
    a = np.asarray(base.convert("RGB")).astype(np.float32) / 255
    lum = a @ np.array([.299, .587, .114], np.float32)
    sat = a.max(-1) - a.min(-1)
    sel = mask & (lum > .45) & (sat < .12)
    out = a.copy()
    out[sel] = np.clip(np.array(colour, np.float32)[None, :] * (lum[sel] / .80)[:, None], 0, 1)
    return Image.fromarray((out * 255 + .5).astype(np.uint8)), int(sel.sum())


def build(out_dir=OUT):
    out_dir = Path(out_dir)
    if not GLB.exists():
        raise SystemExit("missing " + str(GLB))
    sha = hashlib.sha256(GLB.read_bytes()).hexdigest()
    if sha != YACHT_GLB_SHA256:
        raise SystemExit("yacht.glb is not the verified original (" + sha + ")")
    P, UV, I, png = load()
    base = Image.open(io.BytesIO(png))
    W, H = base.size
    mask = hull_mask(P, UV, I, W, H)
    out_dir.mkdir(parents=True, exist_ok=True)
    made = {}
    for name, colour in LIVERIES.items():
        img, painted = repaint(base, mask, colour)
        if painted < 150000:
            raise SystemExit(f"livery {name}: only {painted} hull pixels repainted - the shell was not found")
        path = out_dir / f"yacht_livery_{name}.png"
        img.save(path, optimize=True)
        made[name] = painted
    print("YACHT_LIVERIES", json.dumps(made), "atlas", W, "x", H)
    return made


if __name__ == "__main__":
    build(sys.argv[1] if len(sys.argv) > 1 else OUT)
