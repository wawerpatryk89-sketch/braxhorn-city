#!/usr/bin/env python3
"""MIASTO MISTRZÓW - the owner's two Tripo quads (24.11.2026 11:31: "tu są kłady ... tor do kładów"; 11:33: "to są całe
koła ... żeby się kręciły, kierownica, żeby się kręciła tak samo"; 11:45: "minimum 4 kłady ... przemalować, żeby każdy
miał inny kolor").

Each Tripo export is ONE fused mesh (wheels grown into the suspension, the bars into the tank). This tool cuts it, in the
model's own units, into
  - the body (korpus), static;
  - four wheels: the faces inside the tyre's cylinder (the tread's knobs reach ~1.12 R; under the fender only 1.035 R)
    and outside the tyre's inner sidewall, the connected pieces that hold tread faces - each with its pivot at the hub
    (the centre of its own bounding box: the tyre is round, so it spins without wobble);
  - the handlebar (kierownica): the faces above a plane square to the steering axis that are connected to the grips,
    near the axis only above the pod's foot - so it turns about the axis with no piece of the tank;
scales it to the real machine (the models' proportions are Tripo's; one uniform scale, from the published dimensions):
  - rancher (red utility, front and rear racks): Honda FourTrax Rancher 2024 - 82.8 x 47.4 x 46.2 in, 50.0 in wheelbase,
    24 in tyres (hondanews.com 2024 FourTrax Rancher specifications): x 2.08 -> 2.03 x 1.24 x 1.17 m, 23.4 in tyres;
  - raptor (lime sport quad): Yamaha Raptor 700R - 72.8 x 46.1 x 44.5 in, 50.4 in wheelbase, AT22x7-10 / AT20x10-9
    (en.wikipedia.org/wiki/Yamaha_Raptor_700R): x 1.90 -> 1.84 x 1.28 x 1.04 m;
and repaints the paint (a hue mask on the colour map; the shading kept as the luminance ratio, in linear light):
  rancher czerwony (Tripo's) / niebieski / bialy, raptor limonkowy (Tripo's) / pomaranczowy / fioletowy - six quads, six
  colours.

Output (a folder, packed by --pack into an encrypted owner pack like every other: the repository is public):
  pojazdy_klady_01/klad_<model>_<part>.glb      real metres, glTF axes, vertices relative to the part's pivot
  pojazdy_klady_01/_tekstury/klad_<model>_kolor_<colour>.jpg, klad_<model>_normal__lin.jpg, klad_<model>_mr__lin.png
  pojazdy_klady_01/katalog.json                 pivots in UNITY axes (x negated, as glTFast imports), metres before K

    python3 przygotuj_klady.py RANCHER.glb RAPTOR.glb OUT_DIR [--pack PACK_DIR]

Never starts Unity, a build or a workflow."""
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

import numpy as np
from PIL import Image
from scipy.sparse import coo_matrix
from scipy.sparse.csgraph import connected_components

try:
    import pymeshlab                                             # pip install pymeshlab (+ libopengl0): the far LODs
except ImportError:                                              # the cut still works; the LODs are then skipped
    pymeshlab = None

TARGET = "pojazdy_klady_01"
ROOT = Path(__file__).resolve().parents[3]

# the cut, in the Tripo model's units (y up, front +z, glTF x: +x is the machine's LEFT - glTFast negates x)
MODELE = {
    "rancher": {
        "skala": 2.08, "zrodlo": "Honda FourTrax Rancher 2024 (2.10 x 1.20 x 1.17 m, 24 in tyres)", "farba_hue": 350.0,
        "kolory": {"czerwony": None, "niebieski": (28, 78, 196), "bialy": (236, 236, 232)},
        "kola": [("kolo_tyl_lewe", -0.31, 0.15, 0.15, 0.195, 1, 1.035), ("kolo_tyl_prawe", -0.31, 0.15, 0.15, 0.195, -1, 1.035),
                 ("kolo_przod_lewe", 0.3115, 0.143, 0.1407, 0.19, 1, 1.035), ("kolo_przod_prawe", 0.3115, 0.143, 0.1407, 0.19, -1, 1.035)],
        # the pod (headlight, gauges) turns with the bars on a utility quad; its foot, 26 mm up the axis, stays on the tank
        "kierownica": {"C": (0.0, 0.445, 0.205), "pochylenie": 8.0, "h_min": 0.026, "h_wolne": 0.035, "r_podstawa": 0.05,
                       "z": (0.05, 0.30), "x_chwyt": 0.17},
    },
    "raptor": {
        "skala": 1.90, "zrodlo": "Yamaha Raptor 700R (1.85 x 1.17 x 1.13 m, 22 / 20 in tyres)", "farba_hue": 72.0,
        "kolory": {"limonkowy": None, "pomaranczowy": (255, 106, 0), "fioletowy": (112, 48, 196)},
        "kola": [("kolo_tyl_lewe", -0.249, 0.141, 0.131, 0.215, 1, 1.12), ("kolo_tyl_prawe", -0.249, 0.141, 0.131, 0.215, -1, 1.12),
                 ("kolo_przod_lewe", 0.3597, 0.135, 0.1244, 0.235, 1, 1.12), ("kolo_przod_prawe", 0.3597, 0.135, 0.1244, 0.235, -1, 1.12)],
        "kierownica": {"C": (0.0, 0.415, 0.26), "pochylenie": 12.0, "h_min": 0.0, "h_wolne": 0.035, "r_podstawa": 0.06,
                       "z": (0.10, 0.34), "x_chwyt": 0.17},
    },
}
CZESCI = ["korpus", "kolo_przod_lewe", "kolo_przod_prawe", "kolo_tyl_lewe", "kolo_tyl_prawe", "kierownica"]
# the far versions (a LODGroup): the body at 25 % / 12 % of its triangles, a wheel and the bars at 35 % / 15 % - six quads
# stand at the rental booth, 300 thousand triangles at full detail; from 15 m on a phone's screen nobody sees the difference
LODY = {"korpus": (.25, .12), "kola": (.35, .15)}


def uprosc(pos, tri, uv, cel):
    """MeshLab's quadric edge collapse WITH the texture (the UV seams kept as wedges, the atlas islands not torn): the mesh
    welded by position, its UVs per face corner, through an OBJ; back unwelded per (vertex, uv), smooth normals"""
    key = np.round(pos / 1e-6).astype(np.int64); _, w = np.unique(key, axis=0, return_inverse=True); w = w.ravel()
    pw = np.zeros((w.max() + 1, 3)); pw[w] = pos; fw = w[tri]
    with tempfile.TemporaryDirectory() as d:
        Path(d, "m.mtl").write_text("newmtl m\nmap_Kd t.png\n"); Image.new("RGB", (4, 4)).save(Path(d, "t.png"))
        with open(Path(d, "in.obj"), "w") as o:
            o.write("mtllib m.mtl\n"); o.write("".join("v %.6f %.6f %.6f\n" % tuple(p) for p in pw))
            o.write("".join("vt %.7f %.7f\n" % tuple(t) for t in uv)); o.write("usemtl m\n")
            o.write("".join("f %d/%d %d/%d %d/%d\n" % (a + 1, ta + 1, b + 1, tb + 1, c + 1, tc + 1) for (a, b, c), (ta, tb, tc) in zip(fw, tri)))
        ms = pymeshlab.MeshSet(); ms.load_new_mesh(str(Path(d, "in.obj")))
        ms.meshing_decimation_quadric_edge_collapse_with_texture(targetfacenum=int(cel), preserveboundary=True, preservenormal=True,
                                                                 planarquadric=True, qualitythr=.4, extratcoordw=1.0)
        ms.compute_normal_per_vertex()
        mm = ms.current_mesh()
        V = mm.vertex_matrix(); Nv = mm.vertex_normal_matrix(); F = mm.face_matrix(); WT = mm.wedge_tex_coord_matrix().reshape(-1, 3, 2)
    klucz = {}; P2, N2, U2, T2 = [], [], [], []
    for fi in range(len(F)):
        for c in range(3):
            v = int(F[fi, c]); t = (v, round(float(WT[fi, c, 0]), 6), round(float(WT[fi, c, 1]), 6))
            if t not in klucz:
                klucz[t] = len(P2); P2.append(V[v]); N2.append(Nv[v]); U2.append(WT[fi, c])
            T2.append(klucz[t])
    return np.array(P2), np.array(N2), np.array(U2), np.array(T2).reshape(-1, 3)


def read_glb(path):
    b = Path(path).read_bytes()
    jl = struct.unpack("<I", b[12:16])[0]; g = json.loads(b[20:20 + jl]); bo = 20 + jl + 8

    def acc(i):
        a = g["accessors"][i]; v = g["bufferViews"][a["bufferView"]]
        n = {"SCALAR": 1, "VEC2": 2, "VEC3": 3, "VEC4": 4}[a["type"]]
        dt = {5126: np.float32, 5125: np.uint32, 5123: np.uint16, 5121: np.uint8}[a["componentType"]]
        o = bo + v.get("byteOffset", 0) + a.get("byteOffset", 0)
        x = np.frombuffer(b, dt, a["count"] * n, o)
        return x.reshape(a["count"], n) if n > 1 else x

    def image(i):
        v = g["bufferViews"][g["images"][i]["bufferView"]]; o = bo + v.get("byteOffset", 0); return b[o:o + v["byteLength"]]
    return g, acc, image


def podziel(path, m):
    """faces -> part index (CZESCI order); the wheels' refined hubs"""
    g, acc, image = read_glb(path)
    pr = g["meshes"][0]["primitives"][0]
    P = acc(pr["attributes"]["POSITION"]).astype(np.float64) + np.array(g["nodes"][0].get("translation", [0, 0, 0]))
    N = acc(pr["attributes"]["NORMAL"]).astype(np.float64); U = acc(pr["attributes"]["TEXCOORD_0"]).astype(np.float64)
    I = acc(pr["indices"]).reshape(-1, 3).astype(np.int64)
    key = np.round(P / 1e-5).astype(np.int64); _, w = np.unique(key, axis=0, return_inverse=True); w = w.ravel()
    Fw = w[I]; nf = len(I)
    VF = coo_matrix((np.ones(nf * 3), (Fw.ravel(), np.repeat(np.arange(nf), 3))), shape=(w.max() + 1, nf)).tocsr()
    FF = (VF.T @ VF).tocsr()

    def skladowe(mask):
        idx = np.nonzero(mask)[0]
        k, lab = connected_components(FF[idx][:, idx], directed=False)
        full = -np.ones(nf, int); full[idx] = lab; return k, full
    T = P[I]
    kola = {}
    for przebieg in (0, 1):                       # 2nd pass: the cylinder round each wheel's own measured hub
        part = np.zeros(nf, int)
        for nazwa, zc, yc, R, xin, bok, rg in m["kola"]:
            if przebieg: zc, yc, R = kola[nazwa]["z"], kola[nazwa]["y"], kola[nazwa]["R"]
            r = np.hypot(T[:, :, 2] - zc, T[:, :, 1] - yc); x = T[:, :, 0] * bok
            sn = np.clip((T[:, :, 1] - yc) / np.maximum(r, 1e-9), 0, 1)
            rmax = R * (1.12 - (1.12 - rg) * sn ** 2)
            cand = np.all((x >= xin - .012) & (r <= rmax), axis=1) & (part == 0)
            k, lab = skladowe(cand)
            seed = cand & np.all(r > .88 * R, axis=1)
            sizes = np.bincount(lab[cand], minlength=k)
            keep = [c for c in np.unique(lab[seed]) if sizes[c] >= 20]
            sel = cand & np.isin(lab, keep)
            part[sel] = CZESCI.index(nazwa)
            v = P[np.unique(I[sel])]; lo, hi = v.min(0), v.max(0)
            kola[nazwa] = {"x": (lo[0] + hi[0]) / 2, "y": (lo[1] + hi[1]) / 2, "z": (lo[2] + hi[2]) / 2,
                           "R": ((hi[1] - lo[1]) + (hi[2] - lo[2])) / 4, "szer": hi[0] - lo[0]}
    k = m["kierownica"]; C = np.array(k["C"]); t = np.radians(k["pochylenie"]); d = np.array([0.0, np.cos(t), -np.sin(t)])
    rel = T - C; h = rel @ d; radial = np.linalg.norm(rel - h[..., None] * d, axis=2)
    cand = np.all((h > k["h_min"]) & ((h > k["h_wolne"]) | (radial < k["r_podstawa"])) & (T[:, :, 2] > k["z"][0]) & (T[:, :, 2] < k["z"][1]), axis=1) & (part == 0)
    kk, lab = skladowe(cand)
    seed = cand & np.all((np.abs(T[:, :, 0]) > k["x_chwyt"]) & (h > k["h_wolne"]), axis=1)
    part[cand & np.isin(lab, np.unique(lab[seed]))] = CZESCI.index("kierownica")
    return dict(P=P, N=N, U=U, I=I, part=part, kola=kola, C=C, d=d, g=g, image=image)


def zapisz_glb(path, name, pos, nrm, uv, idx, tex):
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
    g = {"asset": {"version": "2.0", "generator": "BRAXHORN przygotuj_klady.py"},
         "scene": 0, "scenes": [{"nodes": [0]}], "nodes": [{"name": name, "mesh": 0}],
         "meshes": [{"name": name, "primitives": [{"attributes": {"POSITION": ip, "NORMAL": inn, "TEXCOORD_0": iu}, "indices": ii, "material": 0}]}],
         "materials": [{"name": "braxhorn " + name, "doubleSided": True,
                        "pbrMetallicRoughness": {"baseColorTexture": {"index": 0}, "metallicRoughnessTexture": {"index": 2},
                                                 "metallicFactor": 1.0, "roughnessFactor": 1.0},
                        "normalTexture": {"index": 1}}],
         "samplers": [{"magFilter": 9729, "minFilter": 9987, "wrapS": 33071, "wrapT": 33071}],
         "images": [{"uri": tex["base"]}, {"uri": tex["normal"]}, {"uri": tex["mr"]}],
         "textures": [{"sampler": 0, "source": i} for i in range(3)],
         "bufferViews": views, "accessors": accs, "buffers": [{"byteLength": sum(len(x) for x in blobs)}]}
    js = json.dumps(g, separators=(",", ":")).encode(); js += b" " * ((-len(js)) % 4); bin_ = b"".join(blobs)
    Path(path).write_bytes(struct.pack("<III", 0x46546C67, 2, 28 + len(js) + len(bin_)) + struct.pack("<II", len(js), 0x4E4F534A) + js
                           + struct.pack("<II", len(bin_), 0x004E4942) + bin_)


def lin(c): return np.where(c <= .04045, c / 12.92, ((c + .055) / 1.055) ** 2.4)
def srgb(c): return np.where(c <= .0031308, c * 12.92, 1.055 * np.power(np.maximum(c, 0), 1 / 2.4) - .055)


def maska_farby(rgb8, hue):
    """the paint: its hue (+-28 deg, soft to +-40), saturation over .40 (soft from .30), not near black"""
    a = rgb8.astype(np.float32) / 255; mx = a.max(2); mn = a.min(2); s = (mx - mn) / np.maximum(mx, 1e-6)
    hsv = np.asarray(Image.fromarray(rgb8).convert("HSV")).astype(np.float32); h = hsv[..., 0] * 360 / 255
    dh = np.abs((h - hue + 180) % 360 - 180)
    w = np.clip((40 - dh) / 12, 0, 1) * np.clip((s - .30) / .10, 0, 1) * np.clip((mx - .08) / .07, 0, 1)
    return w


def przemaluj(rgb8, hue, cel):
    """the paint's shading kept: new = target x (luminance / the paint's median luminance), linear light"""
    w = maska_farby(rgb8, hue)
    a = lin(rgb8.astype(np.float32) / 255)
    L = a @ np.array([.2126, .7152, .0722], np.float32)
    ref = float(np.median(L[w > .9]))
    t = lin(np.array(cel, np.float32) / 255)
    nowy = t[None, None, :] * (L / ref)[..., None]
    wynik = a * (1 - w[..., None]) + nowy * w[..., None]
    return (np.clip(srgb(np.clip(wynik, 0, 1)), 0, 1) * 255 + .5).astype(np.uint8), float((w > .5).mean())


def przygotuj(zrodla, out):
    out = Path(out) / TARGET; tex = out / "_tekstury"
    if out.exists(): shutil.rmtree(out)
    tex.mkdir(parents=True)
    katalog = {"zrodlo": "Tripo właściciela 24.11.2026 (dwa kłady), pocięte przez unity-tools/mistrzowie/klady/przygotuj_klady.py",
               "osie": "Unity: +x w prawo, +y w górę, +z przód; metry rzeczywiste (przed K); pivot = punkt obrotu części",
               "modele": []}
    for model, path in zrodla.items():
        m = MODELE[model]; s = m["skala"]
        r = podziel(path, m); g, image = r["g"], r["image"]; mat = g["materials"][0]

        def obraz(ti): return Image.open(io.BytesIO(image(g["textures"][ti["index"]]["source"])))
        base = np.asarray(obraz(mat["pbrMetallicRoughness"]["baseColorTexture"]).convert("RGB"))
        warianty = []
        for kolor, cel in m["kolory"].items():
            nazwa = "klad_%s_kolor_%s" % (model, kolor)
            if cel is None: img, udzial = base, float((maska_farby(base, m["farba_hue"]) > .5).mean())
            else: img, udzial = przemaluj(base, m["farba_hue"], cel)
            Image.fromarray(img).save(tex / (nazwa + ".jpg"), quality=92)
            warianty.append({"kolor": kolor, "tekstura": nazwa, "farba_udzial": round(udzial, 4)})
        ni = g["textures"][mat["normalTexture"]["index"]]["source"]
        (tex / ("klad_%s_normal__lin.jpg" % model)).write_bytes(image(ni)) if image(ni)[:2] == b"\xff\xd8" else \
            obraz(mat["normalTexture"]).convert("RGB").save(tex / ("klad_%s_normal__lin.jpg" % model), quality=95)
        mr = np.asarray(obraz(mat["pbrMetallicRoughness"]["metallicRoughnessTexture"]).convert("RGB")).copy()
        mr[..., 0] = 255                                                   # no AO in Tripo's map: R = 1
        Image.fromarray(mr).save(tex / ("klad_%s_mr__lin.png" % model), optimize=True)
        uris = {"base": "_tekstury/%s.jpg" % warianty[0]["tekstura"], "normal": "_tekstury/klad_%s_normal__lin.jpg" % model,
                "mr": "_tekstury/klad_%s_mr__lin.png" % model}
        czesci = {}; trojkaty = 0
        for i, nazwa in enumerate(CZESCI):
            f = r["I"][r["part"] == i]
            if len(f) == 0: raise SystemExit("EMPTY PART %s %s" % (model, nazwa))
            used = np.unique(f); remap = -np.ones(len(r["P"]), int); remap[used] = np.arange(len(used))
            if nazwa.startswith("kolo"): kp = r["kola"][nazwa]; piv = np.array([kp["x"], kp["y"], kp["z"]])
            elif nazwa == "kierownica": piv = r["C"]
            else: piv = np.zeros(3)
            plik = "klad_%s_%s" % (model, nazwa)
            pos = (r["P"][used] - piv) * s; nrm = r["N"][used]; uv = r["U"][used]; tri = remap[f]
            zapisz_glb(out / (plik + ".glb"), plik, pos, nrm, uv, tri, uris)
            u = [-piv[0] * s, piv[1] * s, piv[2] * s]                     # Unity axes
            e = {"nazwa": nazwa, "plik": plik, "pivot": [round(v, 5) for v in u], "trojkaty": int(len(f)), "promien": 0.0, "szerokosc": 0.0,
                 "os": [0.0, 1.0, 0.0], "lody": []}
            if pymeshlab is not None:
                for k, udzial in enumerate(LODY["korpus" if nazwa == "korpus" else "kola"], 1):
                    p3, n3, u3, f3 = uprosc(pos, tri, uv, len(tri) * udzial)
                    zapisz_glb(out / ("%s_lod%d.glb" % (plik, k)), "%s_lod%d" % (plik, k), p3, n3, u3, f3, uris)
                    e["lody"].append({"plik": "%s_lod%d" % (plik, k), "trojkaty": int(len(f3))})
            if nazwa.startswith("kolo"):
                e["promien"] = round(r["kola"][nazwa]["R"] * s, 4); e["szerokosc"] = round(r["kola"][nazwa]["szer"] * s, 4)
            if nazwa == "kierownica": e["os"] = [round(-r["d"][0], 5), round(r["d"][1], 5), round(r["d"][2], 5)]
            czesci[nazwa] = e; trojkaty += int(len(f))
        lo, hi = r["P"].min(0) * s, r["P"].max(0) * s
        kp = czesci["kolo_przod_lewe"]["pivot"]; kt = czesci["kolo_tyl_lewe"]["pivot"]
        # the saddle: the seat's top on the centre line, 0.27 model units (~0.55 m) behind the steering axis
        body = r["P"][np.unique(r["I"][r["part"] == 0])]; zs = r["C"][2] - .27
        near = body[(np.abs(body[:, 0]) < .04) & (np.abs(body[:, 2] - zs) < .025)]
        siodlo = [0.0, round(float(near[:, 1].max()) * s, 4), round(zs * s, 4)]
        katalog["modele"].append({
            "model": model, "skala": s, "wzor": m["zrodlo"], "czesci": list(czesci.values()), "warianty": warianty, "trojkaty": trojkaty,
            "siodlo": siodlo,
            "normal": "klad_%s_normal__lin" % model, "mr": "klad_%s_mr__lin" % model,
            "dlugosc": round(float(hi[2] - lo[2]), 3), "szerokosc": round(float(hi[0] - lo[0]), 3), "wysokosc": round(float(hi[1] - lo[1]), 3),
            "rozstaw_osi": round(kp[2] - kt[2], 3), "rozstaw_kol": round(abs(czesci["kolo_przod_prawe"]["pivot"][0] - kp[0]), 3)})
        k = katalog["modele"][-1]
        print("KLADY %s: %s x %s x %s m, rozstaw osi %s m, kola R %s/%s m, siodlo %s, %d trojkatow (LOD %s), farba %s" % (
            model, k["dlugosc"], k["szerokosc"], k["wysokosc"], k["rozstaw_osi"], czesci["kolo_przod_lewe"]["promien"],
            czesci["kolo_tyl_lewe"]["promien"], siodlo, trojkaty, "/".join(str(sum(l["trojkaty"] for c in k["czesci"] for l in c["lody"][j:j + 1])) for j in range(2)),
            ", ".join("%s %.1f%%" % (w["kolor"], 100 * w["farba_udzial"]) for w in warianty)))
    (out / "katalog.json").write_text(json.dumps(katalog, ensure_ascii=False, indent=1))
    return out


def sha256(p):
    h = hashlib.sha256()
    with open(p, "rb") as f:
        for b in iter(lambda: f.read(1 << 20), b""): h.update(b)
    return h.hexdigest()


def spakuj(lib, zrodla, pack_dir, opis):
    """tar(oryginal/ + pojazdy_klady_01/) -> openssl cms -encrypt -aes256 (DER) to the CI transfer certificate -> parts,
    manifest.json - the format install_sklep_modele.py opens (unity-tools/wideo/install_braxhorn_wideo.open_pack)"""
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
        data = cms.read_bytes(); parts = []
        for i in range(0, len(data), 90 * 1024 * 1024):
            name = "part-%03d.cms" % (i // (90 * 1024 * 1024)); (pack / name).write_bytes(data[i:i + 90 * 1024 * 1024])
            parts.append({"name": name, "bytes": len(data[i:i + 90 * 1024 * 1024]), "sha256": sha256(pack / name)})
        manifest = {"source": opis, "target": TARGET,
                    "staging": "unity-city/Assets/Source/Prywatne/Miasto/" + TARGET + " (BraxMiastoAssety.Model)",
                    "format": "tar -> openssl cms -encrypt -aes256 (DER) to recipient.crt (Braxhorn camp input transfer; its private key recipient.pem.enc is protected by KLUCZ in Actions) -> split 90 MB",
                    "tarSha256": sha256(tar), "cmsSha256": sha256(cms), "parts": parts, "files": files,
                    "recipientCertificateSha256": sha256(pack / "recipient.crt")}
        (pack / "manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=1) + "\n")
    print("KLADY_PACK %s: %d files, %d parts" % (pack, len(files), len(parts)))


if __name__ == "__main__":
    a = sys.argv[1:]
    if len(a) < 3: raise SystemExit(__doc__)
    zrodla = {"rancher": a[0], "raptor": a[1]}
    lib = przygotuj(zrodla, a[2])
    if "--pack" in a:
        spakuj(lib, zrodla, a[a.index("--pack") + 1],
               "Właściciel 24.11.2026 11:31-11:45: dwa kłady z Tripo (czerwony użytkowy, limonkowy sportowy) - tor do kładów na "
               "zachodzie; koła i kierownica osobno (kręcą się, przednie skręcają), prawdziwe wymiary (Honda Rancher, Yamaha "
               "Raptor 700R), 6 kolorów (minimum 4 kłady, każdy inny kolor)")
