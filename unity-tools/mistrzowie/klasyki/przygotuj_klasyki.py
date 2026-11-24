#!/usr/bin/env python3
"""MIASTO MISTRZÓW - the owner's Polish classics from Tripo (24.11.2026 22:21-23:11, three zips: "Samochody i motory.
Klasyki ... koła zrobić, żeby się kręciły. Szyby trzeba powymieniać ... napisy ... poprawić"; "każde napisy, jak są źle,
to zmazać i nowe"; "wszystko odrestaurować co trzeba"; their place: a lit bunker under the hill by the quad track).

Each Tripo export is ONE fused mesh in its own units, with one 2048 atlas. Per vehicle this tool
  - turns it front +z (Tripo gives some with the front at -x) and scales it to the real vehicle's length (MODELE: the
    published dimensions), its wheels standing on y = 0; loose pieces Tripo left beside it are dropped (MODELE usun);
  - restores the paint and the lettering in its colour map (tekstura.py: everything placed on the body in metres, never
    straight into the atlas): rust, dirt and Tripo's smears taken back to the paint; garbled letters, fake badges and
    plates erased; new lettering (Montserrat SemiBold, the region's typeface; brand names only as plain words, no logos)
    and registration plates of the time (1976-2000: black, white characters; private 3 letters + 4 digits, state
    3 letters + 3 digits + letter, the militia MO);
  - replaces the glass:
      * painted windows (Tripo painted the glass and a blurred inside onto a closed shell): those faces become the glass
        part, drawn with the region's tinted car glass, glossy, opaque (there is no inside behind them);
      * open windows (Tripo left the openings, the inside modelled): new panes are built in each opening - the frame's
        surface fitted round it, the pane 1.5 cm inside it, tucked 2 cm under the frame - drawn transparent;
  - cuts the wheels so they roll: the faces inside the tyre's cylinder and outside its inner sidewall, connected to the
    tread, each wheel with its pivot at its hub (the centre of its own faces' box, so it spins without wobble); a car's
    front wheels also steer; a truck's rear twin tyres are one wheel; a motorcycle has two in its middle plane;
  - writes the parts (real metres, glTF axes, each relative to its pivot) with two far LODs (MeshLab's quadric collapse
    with the texture), the maps, and katalog.json (pivots in UNITY axes: x negated, as glTFast imports);
  - packs it (--pack) as an encrypted owner pack like every other: the repository is public.

    python3 przygotuj_klasyki.py SRC_DIR OUT_DIR [--tylko a,b] [--pack PACK_DIR] [--podglad DIR]

SRC_DIR holds the owner's tripo_convert_<id>.glb files (any sub-folder). Never starts Unity, a build or a workflow."""
import hashlib
import io
import json
import shutil
import struct
import subprocess
import sys
import tarfile
import tempfile
from pathlib import Path

sys.dont_write_bytecode = True
import cv2                                                       # noqa: E402
import numpy as np                                               # noqa: E402
from PIL import Image                                            # noqa: E402
from scipy import ndimage                                        # noqa: E402
from scipy.sparse import coo_matrix                              # noqa: E402
from scipy.sparse.csgraph import connected_components            # noqa: E402

sys.path.insert(0, str(Path(__file__).resolve().parent))
sys.path.insert(1, str(Path(__file__).resolve().parents[1] / "klady"))
import przygotuj_klady as KL                                     # noqa: E402  (read_glb, zapisz_glb, uprosc)
import tekstura as T                                             # noqa: E402
import przepisy                                                  # noqa: E402  (the per-vehicle texture recipes)

TARGET = "pojazdy_klasyki_01"
ROOT = Path(__file__).resolve().parents[3]

# the vehicles: Tripo id, the front axis in the source (x: front at -x), the real length (m), the kind, the real wheelbase,
# the tyre radius prior (m), the glass (painted: kept faces become glass; open: panes are built), what to drop
MODELE = {
    "milicja": dict(id="17e66447", nazwa="WARSZAWA MILICJA", os="z", dl=4.586, typ="auto", wb=2.816, R=.32,
                    wzor="FSO Warszawa 223 (4586 x 1695 mm, rozstaw osi 2700 mm; 6.40-15)", szyby="malowane"),
    "maluch": dict(id="3420733b", nazwa="FIAT 126p", os="z", dl=3.054, typ="auto", wb=1.84, R=.263,
                   wzor="Polski Fiat 126p (3054 x 1378 mm, rozstaw osi 1840 mm; 135/80 R12)", szyby="malowane"),
    "syrena": dict(id="45ca6f11", nazwa="SYRENA 105", os="z", dl=4.03, typ="auto", wb=2.30, R=.33,
                   wzor="FSO Syrena 105 (4030 x 1500 mm, rozstaw osi 2300 mm; 5.60-15)", szyby="otwarte"),
    "bosna": dict(id="419d345c", nazwa="BOSNA BAR", os="x", dl=5.413, typ="auto", wb=3.45, R=.341,
                  wzor="dostawczy furgon L2H2 (5413 x 2050 mm, rozstaw osi 3450 mm; 215/70 R15)", szyby="malowane"),
    "fiat125p": dict(id="dc963363", nazwa="FIAT 125p", os="z", dl=4.23, typ="auto", wb=2.505, R=.30,
                     wzor="Polski Fiat 125p (4230 x 1630 mm, rozstaw osi 2505 mm; 175 R13)", szyby="otwarte"),
    "polonez": dict(id="4e4a64e9", nazwa="POLONEZ CARO", os="z", dl=4.369, typ="auto", wb=2.509, R=.29, uprosc=90000,
                    wzor="FSO Polonez Caro (4369 x 1720 mm, rozstaw osi 2509 mm; 175/70 R13)", szyby="otwarte"),
    "fiat125p_tuning": dict(id="12fdc32f", nazwa="FIAT 125p TUNING", os="x", dl=4.23, typ="auto", wb=2.505, R=.29,
                            wzor="Polski Fiat 125p (4230 mm, rozstaw osi 2505 mm), tuning", szyby="otwarte"),
    "polonez_tuning": dict(id="877f0cef", nazwa="POLONEZ TUNING", os="x", dl=4.272, typ="auto", wb=2.509, R=.29,
                           wzor="FSO Polonez 1500 (4272 mm, rozstaw osi 2509 mm), tuning", szyby="otwarte"),
    "polonez_milicja": dict(id="dac08c4a", nazwa="POLONEZ MILICJA", os="z", dl=4.272, typ="auto", wb=2.509, R=.29,
                            wzor="FSO Polonez 1500 MO (4272 x 1650 mm, rozstaw osi 2509 mm; 175 SR13)", szyby="malowane",
                            usun=[("x<", -1.05)]),
    "nysa_milicja": dict(id="9478a4b6", nazwa="NYSA MILICJA", os="z", dl=4.4, typ="auto", wb=2.7, R=.35,
                         wzor="FSD Nysa 522 MO (rozstaw osi 2700 mm; 6.40-15)", szyby="malowane"),
    "star_zielony": dict(id="62df2e43", nazwa="STAR 200", os="z", dl=6.455, typ="ciezarowka", wb=3.6, R=.50,
                         wzor="Star 200 skrzyniowy (6455 x 2420 mm; 9.00-20)", szyby="otwarte"),
    "star_pomaranczowy": dict(id="75282eb7", nazwa="STAR 28", os="z", dl=6.43, typ="ciezarowka", wb=3.5, R=.50,
                              wzor="Star 28 skrzyniowy (6430 x 2400 mm; 9.00-20)", szyby="otwarte"),
    "komar": dict(id="53166df6", nazwa="KOMAR", os="x", dl=1.8, typ="motor", wb=1.175, R=.30,
                  wzor="Romet Komar (1800 mm, rozstaw osi 1175 mm; 2.25-19)", szyby=None),
    "jawa": dict(id="9eab6f88", nazwa="JAWA", os="x", dl=2.101, typ="motor", wb=1.369, R=.31,
                 wzor="Jawa 350 (2101 mm, rozstaw osi 1369 mm; 3.25-18)", szyby=None),
    "romet": dict(id="f48f7800", nazwa="ROMET", os="x", dl=1.78, typ="motor", wb=None, R=.267,
                  wzor="Romet motorower (1780 mm)", szyby=None),
    "junak": dict(id="f120e989", nazwa="JUNAK M10", os="x", dl=2.172, typ="motor", wb=1.417, R=.33,
                  wzor="Junak M10 (2172 mm, rozstaw osi 1417 mm; 3.25-19)", szyby=None),
}
SZKLO = (9, 12, 14)          # the tinted car glass seen from outside (sRGB): the glass part's own colour in the game too
# the far versions: the body at 30 % / 12 % of its triangles, a wheel at 35 % / 15 % (the glass is a few panes: none)
LODY = {"korpus": (.30, .12), "kolo": (.35, .15)}


def zapisz_glb(path, name, pos, nrm, uv, idx):
    """a part as glTF 2.0 binary: positions, normals, UVs, triangles and a plain material - no pictures inside or linked
    (the game builds the materials from _tekstury/ itself: Unity's import reads no texture per part)"""
    pos = pos.astype(np.float32); nrm = nrm.astype(np.float32); uv = uv.astype(np.float32)
    idx = idx.astype(np.uint32 if len(pos) > 65535 else np.uint16)
    blobs, views, accs = [], [], []

    def add(arr, target, typ, ctype, minmax=False):
        data = arr.tobytes(); off = sum(len(x) for x in blobs); blobs.append(data + b"\0" * ((-len(data)) % 4))
        views.append({"buffer": 0, "byteOffset": off, "byteLength": len(data), "target": target})
        a = {"bufferView": len(views) - 1, "componentType": ctype, "count": len(arr), "type": typ}
        if minmax: a["min"] = arr.min(0).tolist(); a["max"] = arr.max(0).tolist()
        accs.append(a); return len(accs) - 1
    ip = add(pos, 34962, "VEC3", 5126, True); inn = add(nrm, 34962, "VEC3", 5126); iu = add(uv, 34962, "VEC2", 5126)
    ii = add(idx.reshape(-1), 34963, "SCALAR", 5125 if idx.dtype == np.uint32 else 5123)
    g = {"asset": {"version": "2.0", "generator": "BRAXHORN przygotuj_klasyki.py"},
         "scene": 0, "scenes": [{"nodes": [0]}], "nodes": [{"name": name, "mesh": 0}],
         "meshes": [{"name": name, "primitives": [{"attributes": {"POSITION": ip, "NORMAL": inn, "TEXCOORD_0": iu}, "indices": ii, "material": 0}]}],
         "materials": [{"name": "braxhorn " + name, "doubleSided": True, "pbrMetallicRoughness": {"metallicFactor": 0.0, "roughnessFactor": 0.6}}],
         "bufferViews": views, "accessors": accs, "buffers": [{"byteLength": sum(len(x) for x in blobs)}]}
    js = json.dumps(g, separators=(",", ":")).encode(); js += b" " * ((-len(js)) % 4); bin_ = b"".join(blobs)
    Path(path).write_bytes(struct.pack("<III", 0x46546C67, 2, 28 + len(js) + len(bin_)) + struct.pack("<II", len(js), 0x4E4F534A) + js
                           + struct.pack("<II", len(bin_), 0x004E4942) + bin_)


# ---------------------------------------------------------------------------------------------------------------- load
def znajdz(src, mid):
    f = sorted(Path(src).rglob("tripo_convert_%s*.glb" % mid))
    if not f: raise SystemExit("BRAK %s w %s" % (mid, src))
    return f[0]


def wczytaj(path, m):
    """the vehicle in metres: front +z, up +y, +x its LEFT (glTF), wheels on y = 0, the length centred on z = 0"""
    g, acc, image = KL.read_glb(path)
    pr = g["meshes"][0]["primitives"][0]
    P = acc(pr["attributes"]["POSITION"]).astype(np.float64)
    N = acc(pr["attributes"]["NORMAL"]).astype(np.float64)
    U = acc(pr["attributes"]["TEXCOORD_0"]).astype(np.float64)
    I = acc(pr["indices"]).reshape(-1, 3).astype(np.int64)
    if m["os"] == "x":                                 # front at -x: (x, y, z) -> (z, y, -x), a rotation (no mirror)
        P = np.stack([P[:, 2], P[:, 1], -P[:, 0]], 1); N = np.stack([N[:, 2], N[:, 1], -N[:, 0]], 1)
    s = m["dl"] / (P[:, 2].max() - P[:, 2].min()); P = P * s
    P[:, 2] -= (P[:, 2].max() + P[:, 2].min()) / 2; P[:, 1] -= P[:, 1].min()
    mat = g["materials"][0]

    def obraz(ref):
        return Image.open(io.BytesIO(image(g["textures"][ref["index"]]["source"]))) if ref else None
    base = obraz(mat["pbrMetallicRoughness"]["baseColorTexture"]).convert("RGB")
    normal = obraz(mat.get("normalTexture")); mr = obraz(mat["pbrMetallicRoughness"].get("metallicRoughnessTexture"))
    if normal is None: normal = Image.new("RGB", base.size, (128, 128, 255))
    if mr is None: mr = Image.new("RGB", base.size, (255, 140, 0))             # no map: half rough, no metal
    for kind, val in m.get("usun", []):                                          # Tripo's loose pieces beside the vehicle
        cen = P[I].mean(1); ax = "xyz".index(kind[0])
        keep = ~((cen[:, ax] < val) if kind[1] == "<" else (cen[:, ax] > val)); I = I[keep]
    used = np.unique(I)                                                          # only the vertices still in use
    if len(used) < len(P):
        remap = -np.ones(len(P), np.int64); remap[used] = np.arange(len(used))
        P, N, U, I = P[used], N[used], U[used], remap[I]
    if m.get("uprosc") and len(I) > m["uprosc"] * 1.5:                           # a 2-million-triangle source
        P2, N2, U2, I2 = KL.uprosc(P, I, U, m["uprosc"])
        P, N, U, I = P2, N2, U2, I2.astype(np.int64)
    return dict(P=P, N=N, U=U, I=I, skala=s, base=base, normal=normal.convert("RGB"), mr=mr.convert("RGB"))


def sasiedztwo(P, I):
    key = np.round(P / 1e-5).astype(np.int64); _, w = np.unique(key, axis=0, return_inverse=True); w = w.ravel()
    nf = len(I); Fw = w[I]
    VF = coo_matrix((np.ones(nf * 3), (Fw.ravel(), np.repeat(np.arange(nf), 3))), shape=(w.max() + 1, nf)).tocsr()
    return (VF.T @ VF).tocsr()


# ---------------------------------------------------------------------------------------------------------------- wheels
def kola_wstepnie(P, m):
    """each wheel from where it meets the ground: its contact z (the lowest points), its radius from the tyre's chord
    above the ground (c^2 = 2Ry - y^2 at y = 6, 9, 12 cm - only the tyre stands that low), its x band"""
    H = P[:, 1].max(); W = P[:, 0].max() - P[:, 0].min(); dl = m["dl"]; out = []
    boki = (1, -1) if m["typ"] != "motor" else (0,)
    for bok in boki:
        band = P[(P[:, 0] * bok > .45 * W / 2) & (P[:, 1] < .55 * H)] if bok else P[np.abs(P[:, 0]) < .07]
        for pol in (1, -1):
            b = band[band[:, 2] * pol > .12 * dl]
            low = b[b[:, 1] < (b[:, 1].min() if len(b) else 0) + .02]     # its own lowest (a Tripo tyre can float a little)
            if len(low) < 5 or low[:, 1].min() > .08: raise SystemExit("%s: no ground contact (side %d, %s)" % (m["nazwa"], bok, "front" if pol > 0 else "rear"))
            zc = float(np.median(low[:, 2])); Rs = []
            y00 = float(low[:, 1].min())
            for y in (.06, .09, .12):
                z = np.sort(b[np.abs(b[:, 1] - y00 - y) < .006][:, 2])
                if len(z) < 4: continue
                i = np.searchsorted(z, zc); lo = hi = min(max(i, 0), len(z) - 1)
                while lo > 0 and z[lo] - z[lo - 1] < .02: lo -= 1
                while hi < len(z) - 1 and z[hi + 1] - z[hi] < .02: hi += 1
                c = (z[hi] - z[lo]) / 2; Rs.append((c * c + y * y) / (2 * y))
            R = float(np.median(Rs)) if Rs else m["R"]
            if not (.7 * m["R"] < R < 1.35 * m["R"]): R = m["R"]
            ring = b[np.abs(np.hypot(b[:, 2] - zc, b[:, 1] - y00 - R) - R) < .02]
            if bok: xin, xout = float(np.percentile(np.abs(ring[:, 0]), 4)), float(np.percentile(np.abs(ring[:, 0]), 96))
            else: xin, xout = 0.0, float(np.percentile(np.abs(ring[:, 0]), 96))
            out.append(dict(bok=bok, pol=pol, z=zc, y=y00 + R, R=R, x_in=xin, x_out=xout))
    return out


def tnij_kola(P, I, kola, typ, FF):
    """faces -> wheel index (+1; 0 the body): inside the tyre's cylinder (1.04 R: a road tyre's smooth tread) and
    outside its inner sidewall, the connected pieces that hold tread faces; twice (the second round the measured hub)"""
    T3 = P[I]; nf = len(I)

    def skladowe(mask):
        idx = np.nonzero(mask)[0]
        k, lab = connected_components(FF[idx][:, idx], directed=False)
        full = -np.ones(nf, int); full[idx] = lab; return k, full
    for przebieg in (0, 1):
        part = np.zeros(nf, int)
        for i, k in enumerate(kola):
            r = np.hypot(T3[:, :, 2] - k["z"], T3[:, :, 1] - k["y"])
            if k["bok"]:
                x = T3[:, :, 0] * k["bok"]; inx = (x >= k["x_in"] - .015) & (x <= k["x_out"] + .04)
            else:
                inx = np.abs(T3[:, :, 0]) <= k["x_out"] + .012
            # over the hub the mudguard hugs the tyre (a motorcycle's by 2-3 cm): 1.01 R there, 1.04 R below
            rmax = np.where(T3[:, :, 1] > k["y"], 1.01, 1.04) * k["R"]
            cand = np.all(inx & (r <= rmax), axis=1) & (part == 0)
            n, lab = skladowe(cand)
            seed = cand & np.all(r > .86 * k["R"], axis=1)
            sizes = np.bincount(lab[cand], minlength=n)
            keep = [c for c in np.unique(lab[seed]) if sizes[c] >= 20]
            sel = cand & np.isin(lab, keep)
            if sel.sum() < 50: raise SystemExit("wheel %d: %d faces" % (i, sel.sum()))
            part[sel] = i + 1
            v = P[np.unique(I[sel])]; lo, hi = v.min(0), v.max(0)
            # the radius from the lower half (nothing but the tyre stands there): its length / 2; the hub that high
            dol = v[v[:, 1] < k["y"]]
            Rm = (dol[:, 2].max() - dol[:, 2].min()) / 2 if len(dol) > 10 else max(hi[1] - lo[1], hi[2] - lo[2]) / 2
            k.update(hz=(dol[:, 2].max() + dol[:, 2].min()) / 2 if len(dol) > 10 else (lo[2] + hi[2]) / 2,
                     hy=lo[1] + Rm, hx=(lo[0] + hi[0]) / 2, szer=hi[0] - lo[0], Rm=Rm)
            if przebieg == 0:                                    # round its own hub the second time (the tread's box)
                k["z"], k["y"] = k["hz"], k["hy"]; k["R"] = float(np.clip(k["Rm"], .85 * k["R"], 1.15 * k["R"]))
    return part


# ---------------------------------------------------------------------------------------------------------------- glass
def roznica(c, farby):
    """how far colours (n x 3, sRGB 0-1) are from the nearest paint: CIE Lab with the lightness at half weight (a panel's
    shading changes its lightness, not its hue) - a neutral grey glass is far from a beige or blue paint, a lighter roof
    of the same paint is near it"""
    if not farby or len(c) == 0: return np.full(len(c), 100.0)
    lab = cv2.cvtColor(np.clip(c, 0, 1).astype(np.float32).reshape(-1, 1, 3), cv2.COLOR_RGB2LAB).reshape(-1, 3)
    out = np.full(len(c), 1e9)
    for f in farby:
        q = cv2.cvtColor(np.array(f, np.float32).reshape(1, 1, 3), cv2.COLOR_RGB2LAB).reshape(3)
        dd = lab - q; dd[:, 0] *= .5; out = np.minimum(out, np.sqrt((dd ** 2).sum(1)))
    return out


def szyby_malowane(mod, okna, farby, cien=.10, krok=.01):
    """the painted glass: in each window box (view, u0, u1, v0, v1) the faces seen there (both of Tripo's layers) whose
    colour is not the body's paint and not the black rubber round the glass - then the window made whole in the view
    (its gaps closed over 4 cm, its holes filled: Tripo's light streaks and reflections in the glass are glass too)"""
    sel = np.zeros(len(mod.I), bool)
    uc = mod.U[mod.I].mean(1); px = np.clip((uc[:, 0] * mod.w).astype(int), 0, mod.w - 1); py = np.clip((uc[:, 1] * mod.h).astype(int), 0, mod.h - 1)
    kol = cv2.GaussianBlur(mod.img, (5, 5), 0)[py, px]
    far = [np.array(f, np.float32) / 255 for f in farby]
    d = roznica(kol, far)
    for widok, box in okna:
        vis = mod.widoczne(widok, box, cos=.15, glebia=.12, margines=0)
        u, v = T.WIDOKI[widok][1](mod.cen)
        vis &= (u >= box[0]) & (u <= box[1]) & (v >= box[2]) & (v <= box[3])
        f = vis & (d > 10) & (kol.max(1) > cien)
        u0, u1, v0, v1 = box; nu, nv = int(np.ceil((u1 - u0) / krok)) + 1, int(np.ceil((v1 - v0) / krok)) + 1
        # the glass faces drawn whole into the view's grid (a low-poly van has faces of a decimetre)
        M8 = np.zeros((nv, nu), np.uint8)
        tu, tv = T.WIDOKI[widok][1](mod.P[mod.I[f]])
        tri = np.stack([(tu - u0) / krok, (v1 - tv) / krok], -1)
        if len(tri): cv2.fillPoly(M8, list(np.round(tri * 4).astype(np.int32)), 1, shift=2)
        M = ndimage.binary_dilation(M8 > 0, iterations=1)
        M = ndimage.binary_closing(M, structure=np.ones((5, 5)), iterations=1)
        M = ndimage.binary_fill_holes(M)
        M = ndimage.binary_erosion(M, iterations=1)
        lab, n = ndimage.label(M)
        # each window as the glass is: its outline straight and clean (the convex hull of what Tripo painted there - a
        # car's window is convex), not the ragged edge of the painting; the window itself, not a speck beside it
        H = np.zeros(M.shape, np.uint8)
        for i in range(1, n + 1):
            yx = np.argwhere(lab == i)
            if len(yx) * krok * krok < .02: continue
            cv2.fillPoly(H, [cv2.convexHull(yx[:, ::-1].astype(np.int32))], 1)
        M = H > 0
        Min = ndimage.binary_erosion(M, iterations=max(1, int(round(.015 / krok))))
        iu = np.clip(((u - u0) / krok).astype(int), 0, nu - 1); iv = np.clip(((v1 - v) / krok).astype(int), 0, nv - 1)
        sel |= vis & ((M[iv, iu] & (d > 6)) | Min[iv, iu])
        # the texels too: the window's own painting (the blurred inside, the streaks) becomes the glass' dark tint, smooth
        # and not metal - a face left on the body at the window's edge then looks the same as the glass beside it
        ys, xs, pts = mod.teksele(vis)
        pu, pv = T.WIDOKI[widok][1](pts)
        ju = np.clip(((pu - u0) / krok).astype(int), 0, nu - 1); jv = np.clip(((v1 - pv) / krok).astype(int), 0, nv - 1)
        inbox = (pu >= u0) & (pu <= u1) & (pv >= v0) & (pv <= v1)
        dt = roznica(mod.img[ys, xs], far)
        t = inbox & (Min[jv, ju] | (M[jv, ju] & (dt > 8)))
        # and 2 texels past the islands' edges where the glass' painting is (the atlas seams would show as light lines)
        mk = np.zeros((mod.h, mod.w), np.uint8); mk[ys[t], xs[t]] = 1
        rim = (cv2.dilate(mk, np.ones((5, 5), np.uint8)) > 0) & (mk == 0)
        ry, rx = np.nonzero(rim); rr = roznica(mod.img[ry, rx], far) > 8
        Y = np.concatenate([ys[t], ry[rr]]); X = np.concatenate([xs[t], rx[rr]])
        mod.img[Y, X] = np.array(SZKLO, np.float32) / 255
        for k, mp in mod.mapy.items():
            hh, ww = mp.shape[:2]; yy = np.clip(Y * hh // mod.h, 0, hh - 1); xx = np.clip(X * ww // mod.w, 0, ww - 1)
            mp[yy, xx] = (128, 128, 255) if k == "normal" else (255, 24, 0)
        mod.log.append("szyba %s %s: %d texels" % (widok, box, int(t.sum())))
    return sel


def zmaz_opone(mod, faces, k, rim=.66, prog=.12):
    """a tyre's outer sidewall cleaned of Tripo's lettering: the texels of the wheel's faces between the rim (rim x R) and
    the tread, on its outer side, that are not the rubber's own black - inpainted from the rubber round them"""
    ys, xs, pts = mod.teksele(faces)
    if len(ys) == 0: return
    r = np.hypot(pts[:, 2] - k["hz"], pts[:, 1] - k["hy"])
    out = (pts[:, 0] - k["hx"]) * (np.sign(k["hx"]) if k["bok"] else 1)
    ring = (r > rim * k["R"]) & (r < 1.0 * k["R"]) & ((out > 0) if k["bok"] else np.ones(len(r), bool))
    c = mod.img[ys, xs]; guma = np.median(c[ring], 0) if ring.any() else np.array([.1, .1, .1])
    dev = ring & (np.linalg.norm(c - guma, axis=1) > prog)
    mask = np.zeros((mod.h, mod.w), np.uint8); mask[ys[dev], xs[dev]] = 255
    mask = cv2.dilate(mask, np.ones((5, 5), np.uint8))
    reg = np.zeros_like(mask); reg[ys[ring], xs[ring]] = 255; mask &= cv2.dilate(reg, np.ones((3, 3), np.uint8))
    src = (np.clip(mod.img, 0, 1) * 255).astype(np.uint8); o = cv2.inpaint(src, mask, 7, cv2.INPAINT_TELEA)
    sel = mask > 0; mod.img[sel] = o[sel].astype(np.float32) / 255
    mod.log.append("opona z%.2f x%.2f: %d texels" % (k["hz"], k["hx"], int(sel.sum())))


def raster_glebi(mod, widok, box, krok=.01):
    """the front-most surface seen in a view's box on a krok grid: (u, v) -> depth toward the viewer (NaN: nothing)"""
    d, uvf = T.WIDOKI[widok]; u0, u1, v0, v1 = box
    nu, nv = int(np.ceil((u1 - u0) / krok)), int(np.ceil((v1 - v0) / krok))
    D = np.full((nv, nu), -np.inf)
    u, v = uvf(mod.cen); f = np.nonzero((u > u0 - .3) & (u < u1 + .3) & (v > v0 - .3) & (v < v1 + .3))[0]
    if len(f) == 0: return np.full((nv, nu), np.nan)
    T3 = mod.P[mod.I[f]]
    e = np.max(np.linalg.norm(T3 - np.roll(T3, 1, axis=1), axis=2), 1)
    n = np.clip(np.ceil(e / (krok * .5)).astype(int), 1, 60)
    for k in np.unique(n):
        tri = T3[n == k]
        a, b = np.meshgrid(np.arange(k + 1), np.arange(k + 1)); m = a + b <= k
        l1 = a[m] / k; l2 = b[m] / k; l3 = 1 - l1 - l2
        p = l1[None, :, None] * tri[:, None, 0] + l2[None, :, None] * tri[:, None, 1] + l3[None, :, None] * tri[:, None, 2]
        p = p.reshape(-1, 3); pu, pv = uvf(p); dep = p @ d
        iu = np.floor((pu - u0) / krok).astype(int); iv = np.floor((v1 - pv) / krok).astype(int)
        ok = (iu >= 0) & (iu < nu) & (iv >= 0) & (iv < nv)
        np.maximum.at(D, (iv[ok], iu[ok]), dep[ok])
    D[~np.isfinite(D)] = np.nan
    return D


def z_widoku(widok, u, v, dep):
    """a view's (u, v, depth) back to the body's x, y, z"""
    if widok == "L": return np.stack([dep, v, -u], -1)
    if widok == "P": return np.stack([-dep, v, u], -1)
    if widok == "F": return np.stack([u, v, dep], -1)
    if widok == "T": return np.stack([-u, v, -dep], -1)
    raise ValueError(widok)


def szyba_otwarta(mod, widok, box, krok=.01, wglab=.015, zakladka=2, prog=.05, min_pole=.012, rama=False):
    """a pane in an open window: the frame's surface fitted round the opening (a quadric in u, v through the box's rim,
    the deep points rejected), the opening = what lies over 5 cm deeper than it or nothing at all; the pane follows the
    fitted surface 1.5 cm inside and reaches 2 cm under the frame. Returns (positions, normals, triangles) or None."""
    D = raster_glebi(mod, widok, box, krok); nv, nu = D.shape; u0, u1, v0, v1 = box
    uu = u0 + (np.arange(nu) + .5) * krok; vv = v1 - (np.arange(nv) + .5) * krok; Ug, Vg = np.meshgrid(uu, vv)
    rim = np.zeros_like(D, bool); r = max(2, int(.03 / krok)); rim[:r] = rim[-r:] = True; rim[:, :r] = rim[:, -r:] = True
    ok = rim & np.isfinite(D)
    if ok.sum() < 20: return None
    # the frame's surface: a quadric in u, v (a windscreen curves across and bends at its foot, a side window curves down)
    baza = (lambda U_, V_: np.stack([np.ones_like(U_), U_, V_, U_ * U_, U_ * V_, V_ * V_], -1))
    A = baza(Ug, Vg)
    use = ok.copy()
    for _ in range(6):
        c, *_ = np.linalg.lstsq(A[use], D[use], rcond=None)
        res = D - A @ c; use = ok & (res > -.03)
    fit = A @ c; lo_d, hi_d = float(D[use].min()), float(D[use].max())
    # the sky seen past the body (above the roof, beside a pillar) is open to the box's rim: not an opening. What is seen
    # through the car (both windows open) is enclosed by the frame: it is.
    # (the box's top border is above the roof; from the front or the rear its sides are beside the pillars - from the
    # side its edges may cross the opening itself)
    nic = ~np.isfinite(D); lab0, n0 = ndimage.label(nic)
    brzegi = [lab0[0]] + ([lab0[:, 0], lab0[:, -1]] if widok in "FT" else [])
    zew = np.unique(np.concatenate(brzegi)); zew = zew[zew > 0]
    hole = (nic & ~np.isin(lab0, zew)) | (np.isfinite(D) & (D < fit - prog))
    hole[rim] = False
    # then the frame itself: the surface fitted again to the 1 - 4 cm round the opening found (the window's own frame,
    # not the box's edge - a box edge on the bonnet would tilt the pane over it), the opening found again against it
    for _ in range(2 if rama else 0):
        big = ndimage.binary_opening(hole, iterations=1)
        lab1, n1 = ndimage.label(big)
        if n1 == 0: break
        pole1 = ndimage.sum(np.ones_like(D), lab1, range(1, n1 + 1)) * krok * krok
        big = np.isin(lab1, [i + 1 for i in range(n1) if pole1[i] >= min_pole])
        ram = ndimage.binary_dilation(big, iterations=4) & ~ndimage.binary_dilation(big, iterations=1) & np.isfinite(D)
        if ram.sum() < 20: break
        use = ram.copy()
        for _ in range(5):
            c, *_ = np.linalg.lstsq(A[use], D[use], rcond=None)
            res = D - A @ c; use = ram & (res > -.03)
        fit = A @ c; lo_d, hi_d = float(D[use].min()), float(D[use].max())
        hole = ((nic & ~np.isin(lab0, zew)) | (np.isfinite(D) & (D < fit - prog))) & ndimage.binary_dilation(big, iterations=3)
        hole[rim] = False
    hole = ndimage.binary_opening(hole, iterations=1)
    lab, n = ndimage.label(hole)
    if n == 0: return None
    pole = ndimage.sum(np.ones_like(D), lab, range(1, n + 1)) * krok * krok
    keep = np.isin(lab, [i + 1 for i in range(n) if pole[i] >= min_pole])
    if not keep.any(): return None
    keep = ndimage.binary_fill_holes(keep)
    keep = ndimage.binary_dilation(keep, iterations=zakladka)
    # the pane's surface: a membrane stretched over the frame round the opening (each cell the mean of its neighbours,
    # the frame's own depth at the edge) - it meets the windscreen's foot at the scuttle, the side glass at the sill,
    # whatever the box's edge lies on; the fitted quadric only starts it
    ram = ndimage.binary_dilation(keep, iterations=1) & ~keep & np.isfinite(D) & (D >= fit - prog)
    Z = np.where(keep, fit, np.where(ram, D, np.nan))
    if widok in "FT":
        # a windscreen or a rear window lies between its two pillars: in each row, from the left pillar's depth to the
        # right one's (the rows smoothed over 9 cm); a row without a pillar takes its neighbours'
        Lr = np.full(nv, np.nan); Rr = np.full(nv, np.nan); J0 = np.zeros(nv, int); J1 = np.zeros(nv, int)
        for i in range(nv):
            jj = np.nonzero(keep[i])[0]
            if len(jj) == 0: continue
            J0[i], J1[i] = jj[0], jj[-1]
            a = D[i, max(0, jj[0] - 5):jj[0]]; b = D[i, jj[-1] + 1:jj[-1] + 6]
            a = a[np.isfinite(a)]; b = b[np.isfinite(b)]
            if len(a): Lr[i] = a.max()
            if len(b): Rr[i] = b.max()
        Lr = np.where(np.isnan(Lr), Rr, Lr); Rr = np.where(np.isnan(Rr), Lr, Rr)
        ok_r = np.isfinite(Lr)
        if ok_r.sum() >= 3:
            ii = np.arange(nv)
            Lr = np.interp(ii, ii[ok_r], ndimage.median_filter(Lr[ok_r], 9, mode="nearest"))
            Rr = np.interp(ii, ii[ok_r], ndimage.median_filter(Rr[ok_r], 9, mode="nearest"))
            Lr = ndimage.uniform_filter1d(Lr, 9, mode="nearest"); Rr = ndimage.uniform_filter1d(Rr, 9, mode="nearest")
            jg = np.arange(nu)[None, :]; t = np.clip((jg - J0[:, None]) / np.maximum(J1 - J0, 1)[:, None], 0, 1)
            Z = np.where(keep, Lr[:, None] + (Rr - Lr)[:, None] * t, Z)
    elif ram.sum() >= 12:
        wsp = keep | ram
        for _ in range(600):
            Zp = np.pad(np.where(wsp, Z, 0), 1); Wp = np.pad(wsp.astype(float), 1)
            sm = Zp[:-2, 1:-1] + Zp[2:, 1:-1] + Zp[1:-1, :-2] + Zp[1:-1, 2:]
            wn = Wp[:-2, 1:-1] + Wp[2:, 1:-1] + Wp[1:-1, :-2] + Wp[1:-1, 2:]
            Z = np.where(keep & (wn > 0), sm / np.maximum(wn, 1), Z)
    # the mesh: 2 cm cells over the opening (any covered 1 cm cell), corners on the fitted surface
    s = 2; mk = np.zeros(((nv + s - 1) // s, (nu + s - 1) // s), bool)
    for a in range(s):
        for b in range(s):
            sub = keep[a::s, b::s]; mk[:sub.shape[0], :sub.shape[1]] |= sub
    cv_, cu_ = np.nonzero(mk)
    corners = {}; Pv, tri = [], []

    def idx(i, j):
        k = (i, j)
        if k not in corners:
            u = u0 + j * krok * s; v = v1 - i * krok * s
            corners[k] = len(Pv); Pv.append((u, v))
        return corners[k]
    for i, j in zip(cv_, cu_):
        a, b, c_, d_ = idx(i, j), idx(i, j + 1), idx(i + 1, j + 1), idx(i + 1, j)
        tri += [(a, d_, c_), (a, c_, b)]
    uv = np.array(Pv); U_, V_ = uv[:, 0], uv[:, 1]
    # a corner's depth: the mean of the membrane's cells round it
    ci = np.round((v1 - V_) / krok).astype(int); cj = np.round((U_ - u0) / krok).astype(int)
    acc = np.zeros(len(U_)); cnt = np.zeros(len(U_))
    for di in (-1, 0):
        for dj in (-1, 0):
            ii = np.clip(ci + di, 0, nv - 1); jj = np.clip(cj + dj, 0, nu - 1); ok2 = keep[ii, jj] & np.isfinite(Z[ii, jj])
            acc[ok2] += Z[ii, jj][ok2]; cnt[ok2] += 1
    dep = np.where(cnt > 0, acc / np.maximum(cnt, 1), baza(U_, V_) @ c)
    dep = np.clip(dep, lo_d - .02, hi_d) - wglab                     # never past the frame's own depths
    pos = z_widoku(widok, U_, V_, dep)
    tri = np.array(tri)
    t = pos[tri]; fn = np.cross(t[:, 1] - t[:, 0], t[:, 2] - t[:, 0])       # smooth normals from the faces
    nrm = np.zeros_like(pos)
    for k in range(3): np.add.at(nrm, tri[:, k], fn)
    nrm /= np.maximum(np.linalg.norm(nrm, axis=1, keepdims=True), 1e-12)
    if np.mean(nrm @ T.WIDOKI[widok][0]) < 0: nrm = -nrm
    # the winding: its face normal toward the viewer (glTF counter-clockwise front faces)
    t = pos[tri]; fn = np.cross(t[:, 1] - t[:, 0], t[:, 2] - t[:, 0])
    if np.mean(fn @ T.WIDOKI[widok][0]) < 0: tri = tri[:, ::-1]
    return pos, nrm, tri, float(keep.sum() * krok * krok)


# ---------------------------------------------------------------------------------------------------------------- one vehicle
def przygotuj_model(name, m, src, out, podglad=None):
    d = wczytaj(znajdz(src, m["id"]), m)
    P, N, U, I = d["P"], d["N"], d["U"], d["I"]
    mod = T.Model(P, I, U, d["base"], d["normal"], d["mr"])
    przepis = przepisy.PRZEPISY.get(name)
    info = przepis(mod) if przepis else {}
    info = info or {}
    log = list(mod.log)
    # the glass
    szklo = np.zeros(len(I), bool); panes = []
    if info.get("okna"):
        szklo = szyby_malowane(mod, info["okna"], info.get("farby_okien", info.get("farby", [])))
        if info.get("szyby_w_teksturze"): szklo[:] = False       # a low-poly van: decimetre faces would zigzag the edge
        log.append("szyby malowane: %d faces" % szklo.sum())
    if info.get("otwory"):
        for o in info["otwory"]:
            widok, box = o[0], o[1]
            r = szyba_otwarta(mod, widok, box, **(o[2] if len(o) > 2 else {}))
            if r is None: log.append("szyba %s %s: no opening found" % (widok, box)); continue
            panes.append(r); log.append("szyba %s %s: %.2f m2, %d tris" % (widok, box, r[3], len(r[2])))
    # the body on its own centre line (a Tripo model can stand off it; the recipes read the views as they are): moved
    cx = (P[:, 0].max() + P[:, 0].min()) / 2
    if abs(cx) > .005:
        P[:, 0] -= cx
        panes = [(p[0] - np.array([cx, 0, 0]),) + tuple(p[1:]) for p in panes]
        log.append("środek: przesunięty o %.3f m w x" % -cx)
    # the wheels (the glass and the body first: a wheel never takes glass)
    FF = sasiedztwo(P, I)
    kola = kola_wstepnie(P, m)
    part = tnij_kola(P, I, kola, m["typ"], FF)
    part[szklo & (part == 0)] = -1
    if info.get("opony"):                                        # the tyres' sidewalls: Tripo's garbled letters off
        for i, k in enumerate(kola): zmaz_opone(mod, part == i + 1, k, info["opony"])
        log += [l for l in mod.log[-len(kola):]]
    nazwy = []
    for k in kola:
        if k["bok"] == 0: nazwy.append("kolo_przod" if k["pol"] > 0 else "kolo_tyl")
        else: nazwy.append("kolo_%s_%s" % ("przod" if k["pol"] > 0 else "tyl", "lewe" if k["bok"] > 0 else "prawe"))
    return dict(name=name, m=m, P=P, N=N, U=U, I=I, mod=mod, part=part, kola=kola, nazwy=nazwy, panes=panes, info=info, log=log, skala=d["skala"])


def zapisz_model(r, out):
    name, m, mod = r["name"], r["m"], r["mod"]
    tex = out / "_tekstury"; tex.mkdir(parents=True, exist_ok=True)
    base = "klasyk_%s" % name
    mod.obraz().save(tex / (base + "_kolor.jpg"), quality=92)
    mod.mapa("normal").save(tex / (base + "_normal__lin.jpg"), quality=95)
    mr = np.asarray(mod.mapa("mr")).copy(); mr[..., 0] = 255                   # no AO in Tripo's map: R = 1
    Image.fromarray(mr).save(tex / (base + "_mr__lin.jpg"), quality=95)
    P, N, U, I, part = r["P"], r["N"], r["U"], r["I"], r["part"]
    czesci = []

    def czesc(nazwa, faces, piv, lod_key, extra=None):
        used = np.unique(faces); remap = -np.ones(len(P), int); remap[used] = np.arange(len(used))
        plik = "%s_%s" % (base, nazwa); pos = P[used] - piv; tri = remap[faces]; uv = U[used]
        zapisz_glb(out / (plik + ".glb"), plik, pos, N[used], uv, tri)
        e = {"nazwa": nazwa, "plik": plik, "pivot": [round(-piv[0], 5), round(piv[1], 5), round(piv[2], 5)], "trojkaty": int(len(faces)),
             "promien": 0.0, "szerokosc": 0.0, "lody": []}
        if lod_key and KL.pymeshlab is not None:
            for k, udzial in enumerate(LODY[lod_key], 1):
                p3, n3, u3, f3 = KL.uprosc(pos, tri, uv, max(60, len(tri) * udzial))
                zapisz_glb(out / ("%s_lod%d.glb" % (plik, k)), "%s_lod%d" % (plik, k), p3, n3, u3, f3)
                e["lody"].append({"plik": "%s_lod%d" % (plik, k), "trojkaty": int(len(f3))})
        if extra: e.update(extra)
        czesci.append(e); return e
    czesc("korpus", I[part == 0], np.zeros(3), "korpus")
    for i, (k, nazwa) in enumerate(zip(r["kola"], r["nazwy"])):
        czesc(nazwa, I[part == i + 1], np.array([k["hx"], k["hy"], k["hz"]]), "kolo",
              {"promien": round(float(k["R"]), 4), "szerokosc": round(float(k["szer"]), 4)})
    szyby = None
    if (part == -1).any():
        e = czesc("szyby", I[part == -1], np.zeros(3), None); szyby = {"plik": e["plik"], "rodzaj": "przyciemniane"}
    if r["panes"]:
        pos = np.concatenate([p[0] for p in r["panes"]]); nrm = np.concatenate([p[1] for p in r["panes"]])
        off = np.cumsum([0] + [len(p[0]) for p in r["panes"]])[:-1]
        tri = np.concatenate([p[2] + o for p, o in zip(r["panes"], off)])
        tri = np.concatenate([tri, tri[:, ::-1]])                                  # both sides (the inside looks out)
        plik = "%s_szyby_otwarte" % base
        zapisz_glb(out / (plik + ".glb"), plik, pos, nrm, np.zeros((len(pos), 2)), tri)
        czesci.append({"nazwa": "szyby_otwarte", "plik": plik, "pivot": [0.0, 0.0, 0.0], "trojkaty": int(len(tri)), "promien": 0.0, "szerokosc": 0.0, "lody": []})
        szyby = dict(szyby or {}, otwarte=plik)
    lo, hi = P.min(0), P.max(0)
    kp = [k for k in r["kola"] if k["pol"] > 0]; kt = [k for k in r["kola"] if k["pol"] < 0]
    rozstaw = float(np.mean([k["hz"] for k in kp]) - np.mean([k["hz"] for k in kt]))
    return {"model": name, "nazwa": m["nazwa"], "typ": m["typ"], "wzor": m["wzor"], "skala_tripo": round(r["skala"], 5),
            "czesci": czesci, "szyby": szyby or {}, "kolor": base + "_kolor", "normal": base + "_normal__lin", "mr": base + "_mr__lin",
            "dlugosc": round(float(hi[2] - lo[2]), 3), "szerokosc": round(float(hi[0] - lo[0]), 3), "wysokosc": round(float(hi[1] - lo[1]), 3),
            "rozstaw_osi": round(rozstaw, 3), "trojkaty": int(sum(c["trojkaty"] for c in czesci)), "napisy": r["info"].get("napisy", []),
            "log": r["log"]}


def podglad_ciecia(r, path):
    """the cut seen from both sides: the body grey, each wheel its own colour, the glass blue, the new panes cyan"""
    import matplotlib; matplotlib.use("Agg"); import matplotlib.pyplot as plt
    P, I, part = r["P"], r["I"], r["part"]; cen = P[I].mean(1)
    kol = np.array([[.6, .6, .6], [1, 0, 0], [0, .7, 0], [1, .5, 0], [.7, 0, .9], [.2, .2, .2]])
    fig, axs = plt.subplots(1, 3, figsize=(24, 5), gridspec_kw={"width_ratios": [3, 3, 1.5]})
    for ax, sgn in zip(axs[:2], (1, -1)):
        s = cen[:, 0] * sgn > -.05 if r["m"]["typ"] != "motor" else np.ones(len(cen), bool)
        c = np.where(part[:, None] == -1, [[.1, .3, 1]], kol[np.clip(part, 0, 5)])
        o = np.argsort(cen[s, 0] * sgn)
        ax.scatter(cen[s][o, 2] * sgn, cen[s][o, 1], s=.3, c=c[s][o]); ax.set_aspect("equal")
        for p in r["panes"]: ax.scatter(p[0][:, 2] * sgn, p[0][:, 1], s=.2, c="c")
        for k in r["kola"]:
            if k["bok"] * sgn >= 0: ax.add_patch(plt.Circle((k["hz"] * sgn, k["hy"]), k["R"], fill=False, color="k", lw=.6))
        ax.set_title("%s %s" % (r["name"], "lewy bok" if sgn > 0 else "prawy bok (lustro)"))
    s = cen[:, 2] > 0; o = np.argsort(cen[s, 2])
    c = np.where(part[:, None] == -1, [[.1, .3, 1]], kol[np.clip(part, 0, 5)])
    axs[2].scatter(cen[s][o, 0], cen[s][o, 1], s=.3, c=c[s][o]); axs[2].set_aspect("equal"); axs[2].set_title("przód")
    fig.savefig(path, dpi=80, bbox_inches="tight"); plt.close(fig)


def sha256(p):
    h = hashlib.sha256()
    with open(p, "rb") as f:
        for b in iter(lambda: f.read(1 << 20), b""): h.update(b)
    return h.hexdigest()


def spakuj(lib, zrodla, pack_dir, opis):
    """tar(oryginal/ + pojazdy_klasyki_01/) -> openssl cms -encrypt -aes256 (DER) to the CI transfer certificate -> 90 MB
    parts + manifest.json: the format install_sklep_modele.py opens (as przygotuj_klady.spakuj)"""
    wzor = ROOT / "unity-city/Prywatne/braxhorn-sklep-motory-01"
    pack = Path(pack_dir); pack.mkdir(parents=True, exist_ok=True)
    for f in ("recipient.crt", "recipient.pem.enc"): shutil.copy2(wzor / f, pack / f)
    with tempfile.TemporaryDirectory() as tmp:
        st = Path(tmp) / "st"; (st / "oryginal").mkdir(parents=True)
        for model, p in zrodla.items(): shutil.copy2(p, st / "oryginal" / ("%s_tripo.glb" % model))
        shutil.copytree(lib, st / TARGET)
        files = {str(f.relative_to(st)): sha256(f) for f in sorted(st.rglob("*")) if f.is_file()}
        tar = Path(tmp) / "pack.tar"
        with tarfile.open(tar, "w") as t:
            for rel in files: t.add(st / rel, arcname=rel)
        cms = Path(tmp) / "pack.cms"
        subprocess.run(["openssl", "cms", "-encrypt", "-binary", "-aes256", "-outform", "DER", "-in", str(tar), "-out", str(cms),
                        str(pack / "recipient.crt")], check=True)
        for old in pack.glob("part-*.cms"): old.unlink()
        data = cms.read_bytes(); parts = []; S = 90 * 1024 * 1024
        for i in range(0, len(data), S):
            name = "part-%03d.cms" % (i // S); (pack / name).write_bytes(data[i:i + S])
            parts.append({"name": name, "bytes": len(data[i:i + S]), "sha256": sha256(pack / name)})
        manifest = {"source": opis, "target": TARGET,
                    "staging": "unity-city/Assets/Source/Prywatne/Miasto/" + TARGET + " (BraxMiastoAssety.Model)",
                    "format": "tar -> openssl cms -encrypt -aes256 (DER) to recipient.crt (Braxhorn camp input transfer; its private key recipient.pem.enc is protected by KLUCZ in Actions) -> split 90 MB",
                    "tarSha256": sha256(tar), "cmsSha256": sha256(cms), "parts": parts, "files": files,
                    "recipientCertificateSha256": sha256(pack / "recipient.crt")}
        (pack / "manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=1) + "\n")
    print("KLASYKI_PACK %s: %d files, %d parts" % (pack, len(files), len(parts)))


def main(a):
    if len(a) < 2: raise SystemExit(__doc__)
    src, out = Path(a[0]), Path(a[1]) / TARGET
    tylko = a[a.index("--tylko") + 1].split(",") if "--tylko" in a else list(MODELE)
    podglad = Path(a[a.index("--podglad") + 1]) if "--podglad" in a else None
    if out.exists() and "--tylko" not in a: shutil.rmtree(out)
    out.mkdir(parents=True, exist_ok=True)
    kat_path = out / "katalog.json"
    katalog = json.loads(kat_path.read_text()) if kat_path.exists() else {
        "zrodlo": "Tripo właściciela 24.11.2026 22:21-23:09 (trzy paczki klasyków), przygotowane przez unity-tools/mistrzowie/klasyki/przygotuj_klasyki.py",
        "osie": "Unity: +x w prawo, +y w górę, +z przód; metry rzeczywiste (przed K); pivot = punkt obrotu części", "modele": []}
    for name in tylko:
        m = MODELE[name]
        r = przygotuj_model(name, m, src, out)
        e = zapisz_model(r, out)
        if podglad: podglad.mkdir(parents=True, exist_ok=True); podglad_ciecia(r, podglad / ("ciecie_%s.png" % name))
        katalog["modele"] = [x for x in katalog["modele"] if x["model"] != name] + [e]
        kola = [c for c in e["czesci"] if c["nazwa"].startswith("kolo")]
        print("KLASYK %s: %s x %s x %s m, rozstaw osi %s m (wzór %s), koła %s, %d trójkątów, szyby %s" % (
            name, e["dlugosc"], e["szerokosc"], e["wysokosc"], e["rozstaw_osi"], m["wb"],
            " ".join("%s R%.3f szer%.3f %dtri" % (c["nazwa"][5:], c["promien"], c["szerokosc"], c["trojkaty"]) for c in kola),
            e["trojkaty"], e["szyby"] or "-"))
        for line in e["log"]: print("   ", line)
    katalog["modele"].sort(key=lambda x: list(MODELE).index(x["model"]))
    kat_path.write_text(json.dumps(katalog, ensure_ascii=False, indent=1))
    if "--pack" in a:
        zrodla = {n: znajdz(src, MODELE[n]["id"]) for n in MODELE}
        spakuj(out, zrodla, a[a.index("--pack") + 1],
               "Właściciel 24.11.2026 22:21-23:11: polskie klasyki z Tripo (auta, ciężarówki Star, motory) - odnowione, nowe "
               "napisy i tablice, wymienione szyby, koła osobno (kręcą się, przednie skręcają), prawdziwe wymiary; miejsce: "
               "oświetlony bunkier pod górką przy torze kładów (wjazd od lasu)")


if __name__ == "__main__":
    main(sys.argv[1:])
