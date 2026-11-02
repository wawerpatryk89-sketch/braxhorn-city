#!/usr/bin/env python3
"""BRAXHORN build 14 — korona drzewa z GAŁĄZEK LIŚCI zamiast pojedynczych trójkątów.

Problem (zdjęcia właściciela z buildu 12, 22:25 i 22:56): drzewa osiedla (drzewo_verdant — park, ulice, działki) z bliska
mają „płaskie trójkątne liście”: każdy liść to romb z dwóch trójkątów wycięty z liścia w atlasie (ostre końce), karty
powiększone 1.25–2.4× przy przerzedzaniu, kolor przemalowany na jedną zieleń → z bliska wyglądają jak low-poly.

Rozwiązanie (jak w grach): karta = gałązka z kilkudziesięcioma małymi liśćmi (9–14 cm) na cienkich pędach, w atlasie
1024×1024 (4 odmiany). Liście z tego samego atlasu Verdant (island_tree/jacaranda, Poly Haven CC0), te same kolory
(średnia jasność ~ jak oryginał — shader korony przemalowuje względem średniej). Karty rozmieszczone tam, gdzie były liście
oryginału (chmura punktów liscie_0), obrócone na zewnątrz korony. Pień i gałęzie bez zmian (ten sam układ współrzędnych —
w grze podmieniane są tylko siatki liści, BraxClaudeOsiedleDrzewa.cs).

LOD (trójkąty liści): 0: ~3.5k (było 46k), 1: ~1.8k (było 25k), 2: ~0.8k (było 10k), 3: ~0.3k (było 3.2k).

Wyjście (commitowane; skrypt do odtworzenia):
  unity-city/Assets/Source/Claude/Osiedle/models/drzewo_klaster/drzewo_klaster.glb  (siatki liscie_0..3, bez tekstur)
  unity-city/Assets/Source/Claude/Osiedle/models/drzewo_klaster/galazki.png          (atlas gałązek RGBA)
  unity-city/Assets/Source/Claude/Osiedle/models/drzewo_klaster/drzewo_klaster.json  (średnie jasności atlasu, granice)
  unity-city/Assets/Source/Claude/Osiedle/models/drzewo_klaster/las.png             (4 drzewa do dalszych planów lasu)
Uruchomienie z katalogu repozytorium: python3 unity-tools/claude/drzewo_klaster.py
"""
import io
import json
import math
import pathlib
import struct

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = pathlib.Path("unity-city/Assets/Source/Claude/Osiedle/models")
SRC = ROOT / "drzewo_verdant" / "drzewo_verdant.glb"
OUT = ROOT / "drzewo_klaster"
CELL = 512


def load_glb(path):
    d = path.read_bytes()
    jl = struct.unpack("<I", d[12:16])[0]
    j = json.loads(d[20:20 + jl])
    b0 = 20 + jl + 8
    return d, j, b0


def accessor(d, j, b0, i):
    a = j["accessors"][i]
    bv = j["bufferViews"][a["bufferView"]]
    nc = {"SCALAR": 1, "VEC2": 2, "VEC3": 3, "VEC4": 4}[a["type"]]
    return np.frombuffer(d, dtype=np.float32, count=a["count"] * nc,
                         offset=b0 + bv.get("byteOffset", 0) + a.get("byteOffset", 0)).reshape(-1, nc).copy()


def image(d, j, b0, idx):
    im = j["images"][idx]
    bv = j["bufferViews"][im["bufferView"]]
    o = b0 + bv.get("byteOffset", 0)
    return Image.open(io.BytesIO(d[o:o + bv["byteLength"]])).convert("RGBA")


def leaf_sprites(atlas):
    """Pojedyncze liście z atlasu Verdant (składowe alfy > 0.42), pionowo, ogonek na dole."""
    from scipy import ndimage
    a = np.asarray(atlas)
    mask = a[..., 3] > 107
    lab, n = ndimage.label(mask)
    out = []
    for k, sl in enumerate(ndimage.find_objects(lab), 1):
        h, w = sl[0].stop - sl[0].start, sl[1].stop - sl[1].start
        if h * w < 4000:
            continue
        crop = a[sl[0].start:sl[0].stop, sl[1].start:sl[1].stop].copy()
        crop[..., 3] = np.where(lab[sl] == k, crop[..., 3], 0)
        out.append(Image.fromarray(crop))
    return out


def build_atlas(leaves, seed=7):
    rng = np.random.default_rng(seed)
    atlas = Image.new("RGBA", (CELL * 2, CELL * 2), (0, 0, 0, 0))
    for cell in range(4):
        # rysunek na większym płótnie (podstawa pędu 320, 630), potem dopasowany do komórki 512 bez ucinania liści
        img = Image.new("RGBA", (640, 640), (0, 0, 0, 0))
        dr = ImageDraw.Draw(img)
        # pęd główny od dołu środka ku górze (lekki łuk), 4–6 pędów bocznych
        bend = rng.uniform(-50, 50)
        main = [(320 + bend * math.sin(t * math.pi) * .6 + rng.uniform(-6, 6), 630 - t * 430) for t in np.linspace(0, 1, 12)]
        twigs = [(main, 6.0)]
        for k in range(int(rng.integers(6, 9))):
            t = rng.uniform(.12, .85)
            i = int(t * (len(main) - 1))
            bx, by = main[i]
            side = -1 if k % 2 == 0 else 1
            ang = math.radians(rng.uniform(32, 70)) * side
            ln = rng.uniform(130, 215) * (1.1 - t * .45)
            pts = []
            for s in np.linspace(0, 1, 7):
                a = ang * (1 - .25 * s)
                pts.append((bx + math.sin(a) * ln * s, by - math.cos(a) * ln * s - 18 * s * s))
            twigs.append((pts, 3.5))
        bark = (78, 62, 46, 255)
        for pts, w in twigs:
            for q in range(len(pts) - 1):
                ww = max(1.5, w * (1 - q / len(pts) * .7))
                dr.line([pts[q], pts[q + 1]], fill=bark, width=int(round(ww)))
        # liście wzdłuż pędów, naprzemiennie, ogonkiem przy pędzie
        placed = 0
        for pts, w in twigs:
            L = len(pts)
            steps = np.linspace(.12, 1.0, int(rng.integers(9, 13)))
            for si, s in enumerate(steps):
                f = s * (L - 1)
                i0 = min(int(f), L - 2)
                fr = f - i0
                px = pts[i0][0] * (1 - fr) + pts[i0 + 1][0] * fr
                py = pts[i0][1] * (1 - fr) + pts[i0 + 1][1] * fr
                dx, dy = pts[i0 + 1][0] - pts[i0][0], pts[i0 + 1][1] - pts[i0][1]
                tw = math.degrees(math.atan2(dx, -dy))       # kierunek pędu (0 = w górę)
                side = 1 if si % 2 == 0 else -1
                ang = tw + side * rng.uniform(28, 68) if s < .97 else tw + rng.uniform(-12, 12)
                lf = leaves[int(rng.integers(len(leaves)))]
                length = rng.uniform(62, 96)
                sc = length / lf.height
                spr = lf.resize((max(2, int(lf.width * sc * rng.uniform(.85, 1.1))), max(2, int(lf.height * sc))), Image.LANCZOS)
                # jasność i lekki odcień każdego liścia osobno (światło, wiek liścia)
                arr = np.asarray(spr).astype(np.float32)
                k = rng.uniform(.82, 1.22)
                arr[..., :3] *= np.array([k * rng.uniform(.95, 1.06), k, k * rng.uniform(.9, 1.05)])
                spr = Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8))
                rot = spr.rotate(-ang, resample=Image.BICUBIC, expand=True)
                # ogonek (dół liścia przed obrotem) przy pędzie: przesunięcie o pół długości w kierunku liścia
                ox = px + math.sin(math.radians(ang)) * length * .5 - rot.width / 2
                oy = py - math.cos(math.radians(ang)) * length * .5 - rot.height / 2
                img.alpha_composite(rot, (int(round(ox)), int(round(oy))))
                placed += 1
        # cień własny: liście bliżej nasady pędu ciemniejsze (głębia gałązki)
        arr = np.asarray(img).astype(np.float32)
        yy, xx = np.mgrid[0:640, 0:640]
        dshade = 1 - .22 * np.clip(1 - np.hypot(xx - 320, yy - 600) / 380, 0, 1)
        arr[..., :3] *= dshade[..., None]
        img = Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8))
        # dopasowanie: podstawa pędu na środku dolnej krawędzi komórki, wszystko w 500 × 500 px
        ys, xs = np.nonzero(np.asarray(img)[..., 3] > 8)
        half = max(320 - xs.min(), xs.max() - 320, 1)
        tall = max(632 - ys.min(), 1)
        sc = min(250.0 / half, 504.0 / tall, 1.0)
        big = img.resize((int(640 * sc), int(640 * sc)), Image.LANCZOS)
        img = Image.new("RGBA", (CELL, CELL), (0, 0, 0, 0))
        img.paste(big, (int(round(256 - 320 * sc)), int(round(508 - 632 * sc))), big)
        atlas.paste(img, ((cell % 2) * CELL, (cell // 2) * CELL))
    # kolor pod przezroczystymi pikselami = kolor najbliższych liści (bez ciemnych obwódek w mipmapach)
    a = np.asarray(atlas).astype(np.float32)
    rgb, al = a[..., :3], a[..., 3:4] / 255.0
    acc, wsum = rgb * al, al.copy()
    for r in (2, 4, 8, 16, 32):
        accb = np.asarray(Image.fromarray(np.clip(acc, 0, 255).astype(np.uint8)).filter(ImageFilter.BoxBlur(r))).astype(np.float32)
        wb = np.asarray(Image.fromarray((wsum[..., 0] * 255).astype(np.uint8)).filter(ImageFilter.BoxBlur(r))).astype(np.float32)[..., None] / 255.0
        fill = np.where(wb > 1e-3, accb / np.maximum(wb, 1e-3), 0)
        acc = np.where(wsum > 1e-3, acc, fill * np.minimum(wb * 4, 1))
        wsum = np.where(wsum > 1e-3, wsum, np.minimum(wb * 4, 1))
    out = np.where(al > 0, rgb, np.where(wsum > 1e-3, acc / np.maximum(wsum, 1e-3), rgb))
    final = np.concatenate([np.clip(out, 0, 255), a[..., 3:4]], axis=2).astype(np.uint8)
    return Image.fromarray(final)


def atlas_stats(img):
    a = np.asarray(img).astype(np.float32) / 255
    m = a[..., 3] > .42
    g = (a[..., 0] * .3 + a[..., 1] * .59 + a[..., 2] * .11)[m].mean()
    lin = np.where(a[..., :3] <= .04045, a[..., :3] / 12.92, ((a[..., :3] + .055) / 1.055) ** 2.4)
    li = (lin[..., 0] * .3 + lin[..., 1] * .59 + lin[..., 2] * .11)[m].mean()
    return float(g), float(li), float(m.mean())


def leaf_cloud(d, j, b0):
    names = {n.get("name"): n for n in j["nodes"]}
    p = j["meshes"][names["liscie_0"]["mesh"]]["primitives"][0]
    pos = accessor(d, j, b0, p["attributes"]["POSITION"]).reshape(-1, 3, 3).mean(1)
    return pos


def blue_noise(points, n, rng):
    """Mniej więcej n punktów rozłożonych równo po chmurze (jeden losowy punkt na woksel)."""
    lo, hi = .02, 1.0
    for _ in range(30):
        v = (lo + hi) / 2
        k = np.unique(np.floor(points / v).astype(np.int64), axis=0).shape[0]
        if k > n:
            lo = v
        else:
            hi = v
    v = (lo + hi) / 2
    keys = np.floor(points / v).astype(np.int64)
    order = rng.permutation(len(points))
    _, first = np.unique(keys[order], axis=0, return_index=True)
    return points[order[first]]


def cards(points, n, size, center, radius, rng):
    pts = blue_noise(points, n, rng)
    V, U, N, I = [], [], [], []
    up = np.array([0.0, 1.0, 0.0])
    for p in pts:
        rad = (p - center) / radius
        rl = np.linalg.norm(rad)
        rad = rad / rl if rl > 1e-4 else up
        rnd = rng.normal(size=3)
        rnd /= np.linalg.norm(rnd)
        nrm = rad * .75 + rnd * .85
        nrm /= np.linalg.norm(nrm)
        u = rad + up * .45 + rng.normal(size=3) * .25
        u = u - nrm * np.dot(u, nrm)
        if np.linalg.norm(u) < 1e-3:
            u = up - nrm * np.dot(up, nrm)
        u /= np.linalg.norm(u)
        r = np.cross(nrm, u)
        s = size * rng.uniform(.85, 1.15)
        base = p - u * s * .55 * min(1.0, rl)       # nasada gałązki bliżej pnia (korona nie rośnie poza obrys oryginału)
        c0 = base - r * s * .5
        c1 = base + r * s * .5
        c2 = base + r * s * .5 + u * s
        c3 = base - r * s * .5 + u * s
        cell = int(rng.integers(4))
        ux, vy = (cell % 2) * .5, (cell // 2) * .5
        k = len(V)
        V += [c0, c1, c2, c3]
        # UV glTF (początek w lewym górnym rogu obrazu): dół gałązki = dół komórki
        U += [(ux, vy + .5), (ux + .5, vy + .5), (ux + .5, vy), (ux, vy)]
        N += [nrm] * 4
        I += [k, k + 1, k + 2, k, k + 2, k + 3]
    return np.array(V, np.float32), np.array(U, np.float32), np.array(N, np.float32), np.array(I, np.uint32)


def write_glb(path, meshes):
    blob = bytearray()
    views, accs, gm, nodes = [], [], [], []

    def view(b, target):
        while len(blob) % 4:
            blob.append(0)
        views.append({"buffer": 0, "byteOffset": len(blob), "byteLength": len(b), "target": target})
        blob.extend(b)
        return len(views) - 1

    def acc(arr, typ, ct=5126, target=34962):
        a = {"bufferView": view(np.ascontiguousarray(arr).tobytes(), target), "componentType": ct, "count": int(arr.shape[0]), "type": typ}
        if typ == "VEC3":
            a["min"] = arr.min(0).tolist()
            a["max"] = arr.max(0).tolist()
        accs.append(a)
        return len(accs) - 1

    for name, (V, U, N, I) in meshes:
        prim = {"attributes": {"POSITION": acc(V, "VEC3"), "NORMAL": acc(N, "VEC3"), "TEXCOORD_0": acc(U, "VEC2")},
                "indices": acc(I, "SCALAR", 5125, 34963), "material": 0}
        gm.append({"name": name, "primitives": [prim]})
        nodes.append({"name": name, "mesh": len(gm) - 1})
    gltf = {"asset": {"version": "2.0", "generator": "braxhorn drzewo_klaster.py"},
            "scene": 0, "scenes": [{"nodes": list(range(len(nodes)))}], "nodes": nodes, "meshes": gm,
            "materials": [{"name": "galazki", "alphaMode": "MASK", "alphaCutoff": .42, "doubleSided": True,
                           "pbrMetallicRoughness": {"metallicFactor": 0, "roughnessFactor": 1}}],
            "buffers": [{"byteLength": len(blob)}], "bufferViews": views, "accessors": accs}
    js = json.dumps(gltf, separators=(",", ":")).encode()
    while len(js) % 4:
        js += b" "
    while len(blob) % 4:
        blob.append(0)
    out = struct.pack("<III", 0x46546C67, 2, 12 + 8 + len(js) + 8 + len(blob))
    out += struct.pack("<II", len(js), 0x4E4F534A) + js + struct.pack("<II", len(blob), 0x004E4942) + bytes(blob)
    path.write_bytes(out)


def las_impostor(atlas, mean_lin, seed=31):
    """Obrazy drzew do dalszych planów lasu (las.png, 2048×512: 4 drzewa liściaste po 512×512): pień z konarami i korona
    złożona z ~240 gałązek z atlasu; kolor jak korony w grze (ta sama zieleń i detal), światło od góry/słońca, cień w dole."""
    rng = np.random.default_rng(seed)
    a = np.asarray(atlas).astype(np.float32) / 255
    lin = np.where(a[..., :3] <= .04045, a[..., :3] / 12.92, ((a[..., :3] + .055) / 1.055) ** 2.4)
    lum = lin @ np.array([.3, .59, .11])
    k = np.clip(1 + (lum / mean_lin - 1) * .65, .3, 2.2)
    cells = []
    for c in range(4):
        x0, y0 = (c % 2) * CELL, (c // 2) * CELL
        cells.append((k[y0:y0 + CELL, x0:x0 + CELL], a[y0:y0 + CELL, x0:x0 + CELL, 3]))
    greenA = np.array([.310, .522, .147]) ** 2.2
    greenB = np.array([.408, .553, .164]) ** 2.2
    out = np.zeros((512, 2048, 4), np.float32)
    for t in range(4):
        img = np.zeros((512, 512, 4), np.float32)
        hue = rng.uniform(0, 1)
        green = greenA * (1 - hue) + greenB * hue
        cx, cy = 256 + rng.uniform(-10, 10), rng.uniform(200, 225)
        rx, ry = rng.uniform(170, 210), rng.uniform(165, 190)
        # pień i konary (kora: brąz w cieniu korony)
        bark = Image.new("RGBA", (512, 512), (0, 0, 0, 0)); d = ImageDraw.Draw(bark)
        d.polygon([(248, 511), (264, 511), (261, cy + 60), (251, cy + 60)], fill=(70, 56, 44, 255))
        for k2 in range(3):
            ang = rng.uniform(-50, 50); ln = rng.uniform(60, 110)
            d.line([(256, cy + 90 - k2 * 25), (256 + math.sin(math.radians(ang)) * ln, cy + 90 - k2 * 25 - math.cos(math.radians(ang)) * ln)], fill=(64, 52, 40, 255), width=6)
        b = np.asarray(bark).astype(np.float32) / 255
        b[..., :3] = np.where(b[..., :3] <= .04045, b[..., :3] / 12.92, ((b[..., :3] + .055) / 1.055) ** 2.4) * .55
        img = b.copy()
        # korona z 5–8 płatów (nieregularny obrys jak u prawdziwego drzewa), w płatach gałązki gęściej przy brzegu
        lobes = [(rng.uniform(-.45, .45), rng.uniform(-.45, .35), rng.uniform(.38, .62)) for _ in range(int(rng.integers(5, 9)))]
        lobes.append((0.0, 0.0, .7))
        pts = []
        while len(pts) < 300:
            lx, ly, lr = lobes[int(rng.integers(len(lobes)))]
            ang = rng.uniform(0, 2 * math.pi); rr = lr * math.sqrt(rng.uniform(.15, 1))
            px, py = lx + math.cos(ang) * rr, ly + math.sin(ang) * rr * .9
            if px * px + py * py <= 1.05:
                pts.append((px, py))
        pts.sort(key=lambda q: -q[1] * .3 + rng.uniform(-.5, .5))      # dół i środek najpierw (dalej), góra na wierzchu
        for (px, py) in pts:
            cell = int(rng.integers(4))
            kk, al = cells[cell]
            size = int(rng.uniform(70, 110))
            ang = math.degrees(math.atan2(px, -py)) + rng.uniform(-50, 50)
            spr_k = Image.fromarray((np.clip(kk / 2.2, 0, 1) * 255).astype(np.uint8)).resize((size, size), Image.LANCZOS).rotate(-ang, resample=Image.BICUBIC)
            spr_a = Image.fromarray((al * 255).astype(np.uint8)).resize((size, size), Image.LANCZOS).rotate(-ang, resample=Image.BICUBIC)
            sk = np.asarray(spr_k).astype(np.float32) / 255 * 2.2
            sa = np.asarray(spr_a).astype(np.float32) / 255
            ox = int(cx + px * rx - size / 2); oy = int(cy + py * ry - size / 2)
            # światło: góra i strona słońca jasne, dół i środek korony w głębokim cieniu (kontrast jak na zdjęciu)
            r = min(1.0, math.hypot(px, py))
            light = .16 + .52 * (.5 - .5 * py) ** 1.3 + .22 * max(0, px * .6 - py * .4) + .2 * r ** 2
            col = green[None, None, :] * .8 * sk[..., None] * light * rng.uniform(.85, 1.12)
            xa, ya = max(0, ox), max(0, oy); xb, yb = min(512, ox + size), min(512, oy + size)
            if xb <= xa or yb <= ya:
                continue
            sub_a = sa[ya - oy:yb - oy, xa - ox:xb - ox, None]
            sub_c = col[ya - oy:yb - oy, xa - ox:xb - ox]
            img[ya:yb, xa:xb, :3] = img[ya:yb, xa:xb, :3] * (1 - sub_a) + sub_c * sub_a
            img[ya:yb, xa:xb, 3:4] = np.maximum(img[ya:yb, xa:xb, 3:4], sub_a)
        out[:, t * 512:(t + 1) * 512] = img
    rgb = np.where(out[..., :3] <= .0031308, out[..., :3] * 12.92, 1.055 * np.power(np.clip(out[..., :3], 0, 1), 1 / 2.4) - .055)
    res = np.concatenate([rgb, out[..., 3:4]], axis=2)
    # kolor pod przezroczystymi pikselami: średnia zieleń (bez ciemnych obwódek w mipmapach)
    m = res[..., 3:4] > .05
    mean = res[..., :3][m[..., 0]].mean(0) if m.any() else np.array([.2, .3, .1])
    res[..., :3] = np.where(m, res[..., :3], mean)
    return Image.fromarray((np.clip(res, 0, 1) * 255).astype(np.uint8), "RGBA")


def pien_lod(d, j, b0, cell=.13):
    """Pień + konary uproszczone (grupowanie wierzchołków w siatce 13 cm modelu ≈ 25 cm w grze) do dalszych poziomów LOD:
    ~10× mniej trójkątów, ten sam obrys z daleka (pełny pień zostaje w LOD 0–1)."""
    names = {n.get("name"): n for n in j["nodes"]}
    p = j["meshes"][names["pien"]["mesh"]]["primitives"][0]
    P = accessor(d, j, b0, p["attributes"]["POSITION"]).astype(np.float64)
    N = accessor(d, j, b0, p["attributes"]["NORMAL"]).astype(np.float64)
    UV = accessor(d, j, b0, p["attributes"]["TEXCOORD_0"]).astype(np.float64)
    if "indices" in p:
        a = j["accessors"][p["indices"]]
        I = accessor_idx(d, j, b0, p["indices"]).reshape(-1, 3)
    else:
        I = np.arange(len(P)).reshape(-1, 3)
    key = np.floor(P / cell).astype(np.int64)
    _, cl, cnt = np.unique(key, axis=0, return_inverse=True, return_counts=True)
    cl = cl.reshape(-1)
    k = cl.max() + 1
    def avg(A):
        s = np.zeros((k, A.shape[1])); np.add.at(s, cl, A); return s / cnt[:, None]
    P2, N2, UV2 = avg(P), avg(N), avg(UV)
    N2 /= np.maximum(np.linalg.norm(N2, axis=1, keepdims=True), 1e-6)
    I2 = cl[I]
    ok = (I2[:, 0] != I2[:, 1]) & (I2[:, 1] != I2[:, 2]) & (I2[:, 0] != I2[:, 2])
    I2 = I2[ok]
    I2 = np.unique(np.sort(I2, axis=1), axis=0, return_index=True)[1]
    I2 = cl[I][ok][np.sort(I2)]
    return P2.astype(np.float32), UV2.astype(np.float32), N2.astype(np.float32), I2.astype(np.uint32).reshape(-1), len(I)


def accessor_idx(d, j, b0, i):
    a = j["accessors"][i]
    bv = j["bufferViews"][a["bufferView"]]
    dt = {5125: np.uint32, 5123: np.uint16, 5121: np.uint8}[a["componentType"]]
    return np.frombuffer(d, dtype=dt, count=a["count"], offset=b0 + bv.get("byteOffset", 0) + a.get("byteOffset", 0)).astype(np.int64)


def main():
    d, j, b0 = load_glb(SRC)
    mats = j["materials"]
    leaf_mat = next(i for i, m in enumerate(mats) if m.get("alphaMode") == "MASK")
    tex_idx = j["textures"][mats[leaf_mat]["pbrMetallicRoughness"]["baseColorTexture"]["index"]]["source"]
    leaves = leaf_sprites(image(d, j, b0, tex_idx))
    print("liście w atlasie:", len(leaves))
    atlas = build_atlas(leaves)
    OUT.mkdir(parents=True, exist_ok=True)
    atlas.save(OUT / "galazki.png", optimize=True)
    g, li, cov = atlas_stats(atlas)
    print(f"atlas: średnia jasność gamma {g:.4f}, liniowo {li:.4f}, pokrycie {cov:.2f}")
    las_impostor(atlas, li).save(OUT / "las.png", optimize=True)
    pts = leaf_cloud(d, j, b0)
    center = pts.mean(0)
    radius = (pts.max(0) - pts.min(0)) / 2
    rng = np.random.default_rng(1414)
    levels = [("liscie_0", 1750, .42), ("liscie_1", 900, .55), ("liscie_2", 420, .76), ("liscie_3", 170, 1.15)]
    meshes = []
    stats = {}
    for name, n, s in levels:
        m = cards(pts, n, s, center, radius, rng)
        meshes.append((name, m))
        stats[name] = {"karty": len(m[3]) // 6, "trojkaty": len(m[3]) // 3}
        print(name, stats[name])
    pl = pien_lod(d, j, b0)
    meshes.append(("pien_lod", pl[:4]))
    stats["pien_lod"] = {"trojkaty": len(pl[3]) // 3, "bylo": int(pl[4])}
    print("pien_lod", stats["pien_lod"])
    write_glb(OUT / "drzewo_klaster.glb", meshes)
    (OUT / "drzewo_klaster.json").write_text(json.dumps({"meanGamma": g, "meanLinear": li, "pokrycie": cov,
                                                          "srodek": center.tolist(), "promien": radius.tolist(), "lod": stats}, indent=1))
    (OUT / "CREDITS.md").write_text(
        "# Drzewo „klaster” (gra)\n\nKorona z gałązek liści: liście z atlasu drzewa Verdant (island_tree / jacaranda, Poly Haven CC0),\n"
        "rozmieszczenie kart wg liści oryginalnej korony (drzewo_verdant). Skrypt: unity-tools/claude/drzewo_klaster.py.\n")


if __name__ == "__main__":
    main()
