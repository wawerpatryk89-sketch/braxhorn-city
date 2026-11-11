#!/usr/bin/env python3
"""MIASTO R1, 11.11.2026 — LAS MIĘDZY MIASTEM A FAWELĄ: gatunki drzew z Poly Haven (CC0) w wersji na telefon.

Właściciel 14:49: „drzewa też, żeby były inne, bo mamy tylko jeden model drzew”; 15:53: „rośliny mają być tylko i wyłącznie
naturalne… Poly Haven, tylko i wyłącznie Poly Haven”. Źródła (glTF 1k z api.polyhaven.com): fir_tree_01 (jodła, 3 odmiany
14–19 m), pine_tree_01 (sosna, 3 odmiany 15–20 m), tree_small_02 (młode drzewo liściaste 4.5 m), dead_tree_trunk_02 (kłoda).
Skany mają 0.5–7 mln trójkątów igieł/liści na drzewo — w grze nie do udźwignięcia. Tu (jak drzewo_klaster.py):
  • DREWNO (pień, konary, suche gałęzie): ta sama bryła uproszczona grupowaniem wierzchołków (siatka 0.16–0.22 m), tekstura
    kory pnia ze źródła (kolor 1k);
  • KORONA: atlas 6 wycinków PRAWDZIWEJ korony (igły / liście źródła wyrysowane z ich kolorami z tekstury, cieniowanie od
    światła z góry i głębi gałęzi) + karty rozmieszczone tam, gdzie w źródle są igły/liście, zwrócone na zewnątrz korony;
  • LOD: liscie_0 / liscie_1 / liscie_2 (mniej i większe karty) — BraxRegion.TreeLods składa LODGroup (25 % / 10 % / 3 %).
Wyjście: unity-city/Assets/Source/Claude/Library/las_<gatunek>.glb (tekstury w środku, materiał korony MASK) + CREDITS.
Uruchomienie: python3 unity-tools/claude/drzewa_las.py <katalog ze źródłami glTF 1k>
"""
import io
import json
import math
import pathlib
import struct
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

REPO = pathlib.Path(__file__).resolve().parents[2]
OUT = REPO / "unity-city/Assets/Source/Claude/Library"
CELL = 256          # komórka atlasu korony (px)
KOM = 6             # komórek w atlasie (3 × 2 → 768 × 512)

GATUNKI = [
    # (plik źródła, węzeł, nazwa w grze, siatka drewna m, promień wycinka korony m, karty LOD0/1/2, rozmiar karty m)
    ("fir_tree_01", "fir_tree_01_a_LOD0", "las_jodla_a", .085, .75, (560, 240, 90), 1.55),
    ("fir_tree_01", "fir_tree_01_b_LOD0", "las_jodla_b", .085, .75, (430, 190, 75), 1.5),
    ("fir_tree_01", "fir_tree_01_c_LOD0", "las_jodla_c", .085, .75, (430, 190, 75), 1.5),
    ("pine_tree_01", "pine_tree_01_a_LOD0", "las_sosna_a", .085, .8, (520, 230, 90), 1.6),
    ("pine_tree_01", "pine_tree_01_b_LOD0", "las_sosna_b", .085, .8, (420, 190, 75), 1.55),
    ("pine_tree_01", "pine_tree_01_c_LOD0", "las_sosna_c", .085, .8, (460, 200, 80), 1.55),
    ("tree_small_02", "tree_small_02_LOD0", "las_drzewko", .09, .45, (330, 150, 60), .85),
    ("dead_tree_trunk_02", "dead_tree_trunk_02", "las_kloda", .07, 0, (0, 0, 0), 0),
]
LISCIE = ("twig", "leaves", "leaf", "needle", "foliage")
# cienkie gałązki w koronie (bark jodły/sosny, branches drzewka): po uproszczeniu zostają z nich płatki z rozciągniętą korą
# (jasne „łuski” w koronie) — pomijane, koronę dają karty; zostaje pień i suche gałęzie u dołu
POMIN_DREWNO = ("_bark", "_branches")
# docelowy średni kolor korony (sRGB) — zieleń lasu w słońcu; atlas skalowany do niego (skany wychodzą zbyt ciemne/szare)
KOLOR_KORONY = {"las_jodla": (60, 82, 42), "las_sosna": (66, 80, 44), "las_drzewko": (72, 94, 46)}


def wczytaj(gltf_path):
    j = json.loads(gltf_path.read_text())
    bufs = [(gltf_path.parent / b["uri"]).read_bytes() for b in j["buffers"]]
    return j, bufs


def acc(j, bufs, i):
    a = j["accessors"][i]
    bv = j["bufferViews"][a["bufferView"]]
    dt = {5126: np.float32, 5125: np.uint32, 5123: np.uint16, 5121: np.uint8}[a["componentType"]]
    nc = {"SCALAR": 1, "VEC2": 2, "VEC3": 3, "VEC4": 4}[a["type"]]
    off = bv.get("byteOffset", 0) + a.get("byteOffset", 0)
    stride = bv.get("byteStride", 0)
    item = np.dtype(dt).itemsize * nc
    if stride and stride != item:
        raw = np.frombuffer(bufs[bv["buffer"]], dtype=np.uint8, count=stride * (a["count"] - 1) + item, offset=off)
        out = np.lib.stride_tricks.as_strided(raw, shape=(a["count"], item), strides=(stride, 1)).copy()
        return out.view(dt).reshape(-1, nc)
    return np.frombuffer(bufs[bv["buffer"]], dtype=dt, count=a["count"] * nc, offset=off).reshape(-1, nc).copy()


def obraz(j, src_dir, mat, klucz="baseColorTexture"):
    t = mat.get("pbrMetallicRoughness", {}).get(klucz)
    if not t:
        return None
    im = j["images"][j["textures"][t["index"]]["source"]]
    return Image.open(src_dir / im["uri"]).convert("RGB")


def czesci(j, bufs, wezel):
    node = next(n for n in j["nodes"] if n.get("name") == wezel)
    T = np.array(node.get("translation", [0, 0, 0]), np.float64)
    drewno, korona = [], []
    for p in j["meshes"][node["mesh"]]["primitives"]:
        mat = j["materials"][p["material"]]
        P = acc(j, bufs, p["attributes"]["POSITION"]).astype(np.float64) + T
        N = acc(j, bufs, p["attributes"]["NORMAL"]).astype(np.float64)
        UV = acc(j, bufs, p["attributes"]["TEXCOORD_0"]).astype(np.float64) if "TEXCOORD_0" in p["attributes"] else np.zeros((len(P), 2))
        I = acc(j, bufs, p["indices"]).astype(np.int64).reshape(-1, 3)
        nm = mat["name"].lower()
        if any(k in nm for k in LISCIE):
            korona.append((mat, P, N, UV, I))
        elif not any(nm.endswith(k) for k in POMIN_DREWNO) or len(j["meshes"][node["mesh"]]["primitives"]) == 1:
            drewno.append((mat, P, N, UV, I))
    return node, drewno, korona


def upros(P, N, UV, I, cell):
    """Grupowanie wierzchołków w siatce cell (ta sama bryła, ~10–40× mniej trójkątów)."""
    key = np.floor(P / cell).astype(np.int64)
    _, cl, cnt = np.unique(key, axis=0, return_inverse=True, return_counts=True)
    cl = cl.reshape(-1); k = cl.max() + 1

    def avg(A):
        s = np.zeros((k, A.shape[1])); np.add.at(s, cl, A); return s / cnt[:, None]
    P2, N2, UV2 = avg(P), avg(N), avg(UV)
    N2 /= np.maximum(np.linalg.norm(N2, axis=1, keepdims=True), 1e-6)
    I2 = cl[I]
    ok = (I2[:, 0] != I2[:, 1]) & (I2[:, 1] != I2[:, 2]) & (I2[:, 0] != I2[:, 2])
    I2 = I2[ok]
    _, first = np.unique(np.sort(I2, axis=1), axis=0, return_index=True)
    I2 = I2[np.sort(first)]
    return P2.astype(np.float32), N2.astype(np.float32), UV2.astype(np.float32), I2.astype(np.uint32).reshape(-1)


def kolory_trojkatow(korona, src_dir, j):
    """Środki, normalne i kolory (z tekstury w środku UV) trójkątów korony."""
    C, Nn, K, T3 = [], [], [], []
    for mat, P, N, UV, I in korona:
        img = obraz(j, src_dir, mat)
        a = np.asarray(img).astype(np.float32) / 255 if img else None
        tri = P[I]
        c = tri.mean(1)
        n = np.cross(tri[:, 1] - tri[:, 0], tri[:, 2] - tri[:, 0])
        n /= np.maximum(np.linalg.norm(n, axis=1, keepdims=True), 1e-9)
        nv = N[I].mean(1)
        n = np.where((np.sum(n * nv, 1) < 0)[:, None], -n, n)
        if a is not None:
            uv = UV[I].mean(1)
            h, w = a.shape[:2]
            x = np.clip((uv[:, 0] % 1) * (w - 1), 0, w - 1).astype(np.int64)
            y = np.clip((uv[:, 1] % 1) * (h - 1), 0, h - 1).astype(np.int64)
            col = a[y, x, :3]
        else:
            col = np.tile(np.array([[.2, .3, .12]], np.float32), (len(c), 1))
        C.append(c); Nn.append(n); K.append(col); T3.append(tri)
    return np.concatenate(C), np.concatenate(Nn), np.concatenate(K), np.concatenate(T3)


def atlas_korony(C, Nn, K, T3, rho, rng):
    """Atlas KOM wycinków prawdziwej korony: rzut prostokątny z zewnątrz korony, malarz od tyłu, cieniowanie światłem z góry."""
    os_xz = np.array([0.0, 0.0])
    r = np.linalg.norm(C[:, [0, 2]] - os_xz, axis=1)
    y0, y1 = np.percentile(C[:, 1], [15, 85])
    kand = np.where((C[:, 1] > y0) & (C[:, 1] < y1) & (r > np.percentile(r, 55)))[0]
    W = CELL * 2
    atlas = Image.new("RGBA", (CELL * 3, CELL * 2), (0, 0, 0, 0))
    srednie = []
    for k in range(KOM):
        p = C[kand[rng.integers(len(kand))]]
        out = np.array([p[0], 0.0, p[2]]); out /= max(np.linalg.norm(out), 1e-6)
        up = np.array([0.0, 1.0, 0.0]); rt = np.cross(up, out)
        d = C - p
        u, v, w = d @ rt, d @ up, d @ out
        sel = np.where((np.abs(u) < rho) & (np.abs(v) < rho) & (np.abs(w) < rho * 1.4))[0]
        img = Image.new("RGBA", (W, W), (0, 0, 0, 0)); dr = ImageDraw.Draw(img)
        order = sel[np.argsort(w[sel])]                       # od najdalszego (w głąb korony) do najbliższego
        L = np.array([.35, .85, .4]); L /= np.linalg.norm(L)
        for t in order:
            tri = T3[t] - p
            xs = (tri @ rt / rho * .5 + .5) * W; ys = (1 - (tri @ up / rho * .5 + .5)) * W
            if xs.max() < 0 or xs.min() > W or ys.max() < 0 or ys.min() > W:
                continue
            n = Nn[t]
            sw = .68 + .32 * max(0.0, float(n @ L))
            gl = .74 + .26 * min(1.0, max(0.0, (w[t] + rho * 1.4) / (rho * 2.4)))      # głębiej w gałęzi = ciemniej
            col = np.clip(K[t] * sw * gl * 1.08, 0, 1)
            dr.polygon(list(zip(xs.tolist(), ys.tolist())), fill=(int(col[0] * 255), int(col[1] * 255), int(col[2] * 255), 255))
        img = img.resize((CELL, CELL), Image.BOX)          # uśrednianie pola (bez prześwietlonych obwódek LANCZOS)
        a = np.asarray(img).astype(np.float32)
        al = a[..., 3:4] / 255
        m = al[..., 0] > .35
        sr = a[..., :3][m].mean(0) if m.any() else np.array([50, 70, 35.0])
        srednie.append(sr)
        a[..., :3] = np.where(al > .02, a[..., :3], sr)   # PIL zmniejsza RGBA z alfą przemnożoną — kolor już bez obwódek; pod przezroczystymi średnia (mipmapy)
        # brzeg komórki wygaszony kołowo (kępa gałązek zamiast prostych krawędzi karty — bez „kanciastych płatów” na tle nieba)
        yy, xx = np.mgrid[0:CELL, 0:CELL]; rr = np.hypot(xx - CELL / 2 + .5, yy - CELL / 2 + .5) / (CELL / 2)
        maska = np.clip((1.0 - rr) / .32, 0, 1)
        a[..., 3] = np.clip((al[..., 0] * maska - .1) / .6, 0, 1) * 255
        atlas.paste(Image.fromarray(np.clip(a, 0, 255).astype(np.uint8), "RGBA"), ((k % 3) * CELL, (k // 3) * CELL))
    return atlas, np.mean(srednie, 0)


def blue_noise(points, n, rng):
    if len(points) <= n:
        return points
    lo, hi = .02, 5.0
    for _ in range(32):
        v = (lo + hi) / 2
        k = np.unique(np.floor(points / v).astype(np.int64), axis=0).shape[0]
        lo, hi = (v, hi) if k > n else (lo, v)
    v = (lo + hi) / 2
    keys = np.floor(points / v).astype(np.int64)
    order = rng.permutation(len(points))
    _, first = np.unique(keys[order], axis=0, return_index=True)
    return points[order[first]]


def karty(C, n, size, rng, stozek=False):
    probka = C[rng.choice(len(C), min(len(C), 120000), replace=False)] if len(C) > 120000 else C   # chmura igieł/liści (podpróbka)
    pts = blue_noise(probka.astype(np.float64), n, rng)
    y0, y1 = float(C[:, 1].min()), float(C[:, 1].max())
    up = np.array([0.0, 1.0, 0.0])
    V, U, N, I = [], [], [], []
    for p in pts:
        rad = np.array([p[0], 0.0, p[2]]); rl = np.linalg.norm(rad)
        rad = rad / rl if rl > 1e-3 else np.array([1.0, 0, 0])
        nrm = rad * .8 + rng.normal(size=3) * .55 + up * .25; nrm /= np.linalg.norm(nrm)
        u = up * .9 + rad * .35 + rng.normal(size=3) * .2
        u -= nrm * (u @ nrm); u /= max(np.linalg.norm(u), 1e-6)
        r = np.cross(nrm, u)
        s = size * rng.uniform(.85, 1.2)
        if stozek:                                            # jodła, sosna: mniejsze karty ku wierzchołkowi (korona stożkowa)
            s *= .5 + .5 * (1 - (p[1] - y0) / max(y1 - y0, 1e-3))
        c = p
        c0, c1, c2, c3 = c - r * s * .5 - u * s * .5, c + r * s * .5 - u * s * .5, c + r * s * .5 + u * s * .5, c - r * s * .5 + u * s * .5
        cell = int(rng.integers(KOM)); ux, vy = (cell % 3) / 3, (cell // 3) / 2
        k = len(V)
        V += [c0, c1, c2, c3]
        U += [(ux, vy + .5), (ux + 1 / 3, vy + .5), (ux + 1 / 3, vy), (ux, vy)]
        N += [nrm] * 4
        I += [k, k + 1, k + 2, k, k + 2, k + 3]
    return np.array(V, np.float32), np.array(U, np.float32), np.array(N, np.float32), np.array(I, np.uint32)


def zapisz_glb(path, siatki, obrazy, materialy):
    """siatki: [(nazwa, (V, U, N, I), nr materiału)], obrazy: [(bajty, mime)], materialy: [dict glTF]."""
    blob = bytearray(); views, accs, gm, nodes = [], [], [], []

    def view(b, target=None):
        while len(blob) % 4:
            blob.append(0)
        v = {"buffer": 0, "byteOffset": len(blob), "byteLength": len(b)}
        if target:
            v["target"] = target
        views.append(v); blob.extend(b); return len(views) - 1

    def acc_(arr, typ, ct=5126, target=34962):
        a = {"bufferView": view(np.ascontiguousarray(arr).tobytes(), target), "componentType": ct, "count": int(arr.shape[0]), "type": typ}
        if typ == "VEC3":
            a["min"] = arr.min(0).tolist(); a["max"] = arr.max(0).tolist()
        accs.append(a); return len(accs) - 1
    for nazwa, (V, U, N, I), m in siatki:
        if len(I) == 0:
            continue
        prim = {"attributes": {"POSITION": acc_(V, "VEC3"), "NORMAL": acc_(N, "VEC3"), "TEXCOORD_0": acc_(U, "VEC2")},
                "indices": acc_(I, "SCALAR", 5125, 34963), "material": m}
        gm.append({"name": nazwa, "primitives": [prim]}); nodes.append({"name": nazwa, "mesh": len(gm) - 1})
    images = [{"bufferView": view(b), "mimeType": mime} for b, mime in obrazy]
    g = {"asset": {"version": "2.0", "generator": "braxhorn drzewa_las.py (Poly Haven CC0)"}, "scene": 0,
         "scenes": [{"nodes": list(range(len(nodes)))}], "nodes": nodes, "meshes": gm, "materials": materialy,
         "images": images, "samplers": [{"magFilter": 9729, "minFilter": 9987, "wrapS": 10497, "wrapT": 10497}],
         "textures": [{"source": i, "sampler": 0} for i in range(len(images))],
         "buffers": [{"byteLength": len(blob)}], "bufferViews": views, "accessors": accs}
    js = json.dumps(g, separators=(",", ":")).encode()
    while len(js) % 4:
        js += b" "
    while len(blob) % 4:
        blob.append(0)
    out = struct.pack("<III", 0x46546C67, 2, 12 + 8 + len(js) + 8 + len(blob))
    out += struct.pack("<II", len(js), 0x4E4F534A) + js + struct.pack("<II", len(blob), 0x004E4942) + bytes(blob)
    path.write_bytes(out)


def jpg(img, q=86):
    b = io.BytesIO(); img.save(b, "JPEG", quality=q, optimize=True); return b.getvalue()


def png(img):
    b = io.BytesIO(); img.save(b, "PNG", optimize=True); return b.getvalue()


def main():
    src = pathlib.Path(sys.argv[1])
    OUT.mkdir(parents=True, exist_ok=True)
    raport = {}
    cache = {}
    for plik, wezel, nazwa, cell, rho, ile, rozm in GATUNKI:
        rng = np.random.default_rng(sum(map(ord, nazwa)) * 7919)
        if plik not in cache:
            cache.clear(); cache[plik] = wczytaj(src / plik / (plik + ".gltf"))
        j, bufs = cache[plik]
        node, drewno, korona = czesci(j, bufs, wezel)
        # pień na środku, podstawa na y = 0
        wsz = np.concatenate([d[1] for d in drewno])
        sr = np.array([np.median(wsz[:, 0]), wsz[:, 1].min(), np.median(wsz[:, 2])])
        if drewno:
            pien_mat = max(drewno, key=lambda d: len(d[4]))[0]
        Pd, Nd, Ud, Id = [], [], [], []
        off = 0
        for mat, P, N, UV, I in drewno:
            q = upros(P - sr, N, UV, I, cell if "trunk" in mat["name"] or len(drewno) == 1 else cell * 1.1)
            Pd.append(q[0]); Nd.append(q[1]); Ud.append(q[2]); Id.append(q[3] + off); off += len(q[0])
        pien = (np.concatenate(Pd), np.concatenate(Ud), np.concatenate(Nd), np.concatenate(Id).astype(np.uint32))
        img_kora = obraz(j, src / plik, pien_mat)
        obrazy = [(jpg(img_kora.resize((512, 512), Image.LANCZOS)), "image/jpeg")]
        materialy = [{"name": nazwa + "_kora", "pbrMetallicRoughness": {"baseColorTexture": {"index": 0}, "metallicFactor": 0, "roughnessFactor": .95}}]
        siatki = [("pien", pien, 0)]
        st = {"zrodlo": plik + " / " + wezel, "drewno_trojkatow": int(len(pien[3]) // 3),
              "drewno_zrodlo": int(sum(len(d[4]) for d in drewno)), "wysokosc_m": float(wsz[:, 1].max() - wsz[:, 1].min())}
        if korona:
            C, Nn, K, T3 = kolory_trojkatow([(m, (P - sr).astype(np.float32), N.astype(np.float32), UV.astype(np.float32), I) for m, P, N, UV, I in korona], src / plik, j)
            atlas, srednia = atlas_korony(C, Nn, K, T3, rho, rng)
            cel = next((v for k, v in KOLOR_KORONY.items() if nazwa.startswith(k)), None)
            if cel is not None:
                a = np.asarray(atlas).astype(np.float32)
                m = a[..., 3] > 128
                gain = np.clip(np.array(cel, np.float32) / np.maximum(a[..., :3][m].mean(0), 1), .8, 2.4)
                a[..., :3] = np.clip(a[..., :3] * gain, 0, 255)
                atlas = Image.fromarray(a.astype(np.uint8), "RGBA")
            obrazy.append((png(atlas), "image/png"))
            materialy.append({"name": nazwa + "_korona", "alphaMode": "MASK", "alphaCutoff": .45, "doubleSided": True,
                              "pbrMetallicRoughness": {"baseColorTexture": {"index": 1}, "metallicFactor": 0, "roughnessFactor": .9}})
            for lv, (n, s) in enumerate(zip(ile, (rozm, rozm * 1.45, rozm * 2.2))):
                kk = karty(C, n, s, rng, nazwa.startswith(("las_jodla", "las_sosna")))
                siatki.append((f"liscie_{lv}", (kk[0], kk[1], kk[2], kk[3]), 1))
                st[f"liscie_{lv}_trojkatow"] = int(len(kk[3]) // 3)
            st["korona_zrodlo_trojkatow"] = int(len(C))
            if len(sys.argv) > 2: atlas.save(pathlib.Path(sys.argv[2]) / (nazwa + "_atlas.png"))
        zapisz_glb(OUT / (nazwa + ".glb"), siatki, obrazy, materialy)
        st["plik_kB"] = (OUT / (nazwa + ".glb")).stat().st_size // 1024
        raport[nazwa] = st
        print(nazwa, st, flush=True)
    (OUT / "las_drzewa.json").write_text(json.dumps(raport, indent=1, ensure_ascii=False))
    (OUT / "las_drzewa.CREDITS.md").write_text(
        "# Las między miastem a fawelą (MIASTO R1, 11.11.2026)\n\n"
        "Drzewa z Poly Haven (CC0, https://polyhaven.com): fir_tree_01 (jodła, odmiany a/b/c), pine_tree_01 (sosna, a/b/c),\n"
        "tree_small_02 (młode drzewo liściaste), dead_tree_trunk_02 (kłoda). Wersja na telefon: drewno uproszczone, korona z kart\n"
        "z wycinkami prawdziwej korony źródła. Skrypt: unity-tools/claude/drzewa_las.py.\n", encoding="utf-8")


if __name__ == "__main__":
    main()
