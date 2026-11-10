#!/usr/bin/env python3
"""BRAXHORN — skrzynki z PRAWDZIWYMI owocami i warzywami do sklepów (MIASTO R1, 10.11.2026).

Właściciel 10.11 08:09: „mieliśmy normalne jedzenia, takie prawdziwe… żebyś też poszukał ich i wstawił, nie tylko te
z etykiet”. Poly Haven ma skany pojedynczych owoców i warzyw (CC0): jabłko, cytryna, limonka, cebula, batat, awokado,
kiwi, granat, gruszki, imbir, kiść bananów — każdy po 2–8 tys. trójkątów, za ciężkie, żeby na półce leżało ich po
kilkadziesiąt. Ten skrypt:
  1. pobiera skany (glTF 1K) z Poly Haven,
  2. upraszcza każdy owoc do 160–380 trójkątów (meshoptimizer, UV i szwy tekstury zachowane; tekstura i mapa normalnych
     ze skanu, zmniejszone do 512 px — z odległości sklepu nie widać różnicy),
  3. układa je w drewnianej skrzynce z listewek (50 × 30 × 22 cm, drewno rough_wood z Poly Haven) jak na straganie:
     owoce „spadają” jeden po drugim w najniższe wolne miejsce (kule), półtorej warstwy, kopczyk na środku; pod spodem
     fałszywe dno w kolorze skórki (szpary między owocami nie świecą pustką),
  4. zapisuje .glb do biblioteki unity-city/Assets/Source/Claude/Library (BraxRegion.Model je tam znajdzie: „ph:skrzynka_jablka”).
Skrzynka ok. 6–9 tys. trójkątów (cały owocowy regał w lokalu i tak znika w LOD dalej niż 25 m).

Użycie:  python3 -I unity-tools/claude/skrzynki_owocow.py [katalog_na_skany]
Wymaga: numpy, trimesh, Pillow, meshoptimizer (pip).
"""
import io
import json
import math
import pathlib
import sys
import urllib.request

import numpy as np
import trimesh
import meshoptimizer as mo
from PIL import Image, ImageDraw
from scipy import ndimage

ROOT = pathlib.Path(__file__).resolve().parents[2]
LIB = ROOT / "unity-city" / "Assets" / "Source" / "Claude" / "Library"
UA = {"User-Agent": "BraxhornCity/1.0 (fruit crates, CC0 Poly Haven)"}

OWOCE = {   # nazwa → (id Poly Haven, trójkąty na sztukę, kształt, typowa długość sztuki w metrach — skan skalowany do niej)
    "jablka": ("food_apple_01", 340, "kula", .085),
    "cytryny": ("lemon", 240, "podluzny", .09),
    "limonki": ("food_lime_01", 220, "kula", .065),
    "cebula": ("yellow_onion", 260, "kula", .08),
    "bataty": ("sweet_potato", 200, "podluzny", .17),
    "kiwi": ("food_kiwi_01", 220, "podluzny", .075),
    "granaty": ("food_pomegranate_01", 380, "kula", .10),
    "gruszki": ("food_pears_asian_01", 300, "kula", .095),
}
KISC_BANANOW = ("bananas", "bananas_bunch", 2600)   # kiść bez pojedynczych bananów obok (26 tys. → 2,6 tys.)
# skany Poly Haven za ciężkie na półkę (stoją po kilka w każdym sklepie) → lżejsze kopie w bibliotece: id → (nazwa, trójkąty)
LZEJSZE = {
    "hamburger_buns": ("bulki", 2400),            # 11,6 tys.
    "gaming_console": ("konsola", 3000),          # 31,7 tys.
    "wine_bottles_01": ("wino_butelki", 5000),    # 28,8 tys. (bary, bistro, pizzeria — zamiast butelek mleka z etykiet)
    "plastic_bottle_gallon": ("kanister_mleka", 1800),   # 13,7 tys.
    "cigarette_pack": ("papierosy", 500),         # 7,6 tys. (kiosk loterii)
}
# KWIACIARNIA (MIASTO R1, 10.11): zestawy roślin Poly Haven to kilka roślin bez doniczek w jednym pliku — pojedyncza roślina
# (węzeł zestawu, uproszczona) w doniczce ze skanu potted_plant_04 dopasowanej do rośliny: nazwa → (id, węzeł, trójkąty rośliny)
DONICZKI = {
    "doniczka_anturium_1": ("anthurium_botany_01", "anthurium_botany_06_f", 2200),
    "doniczka_anturium_2": ("anthurium_botany_01", "anthurium_botany_04_d", 2600),
    "doniczka_anturium_3": ("anthurium_botany_01", "anthurium_botany_01_c", 3000),
    "doniczka_kalatea_1": ("calathea_orbifolia_01", "calathea_orbifolia_01_e", 1500),
    "doniczka_kalatea_2": ("calathea_orbifolia_01", "calathea_orbifolia_01_c", 2400),
    "doniczka_kalatea_3": ("calathea_orbifolia_01", "calathea_orbifolia_01_a", 3000),
    "doniczka_paproc_1": ("fern_02", "fern_02_a", 784),
    "doniczka_paproc_2": ("fern_02", "fern_02_c", 1800),
}
DONICA = ("potted_plant_04", ("potted_plant_04_pot", "potted_plant_04_dirt"), 900)
# PIEKARNIA: Poly Haven nie ma bochenków ani bagietek — skan bułki (hamburger_buns: prawdziwa skórka z nacięciem) rozciągnięty
# do kształtu chleba: nazwa → (sztuk, skala x (długość), y (wysokość), z (szerokość))
PIECZYWO = {
    "chleb_bochenki": (3, 2.3, 1.25, 1.25),
    "bagietki": (3, 5.2, .72, .6),
    "chleb_okragly": (2, 1.85, 1.35, 1.85),
}
DREWNO = "raw_plank_wall"   # jasne, surowe deski jak w prawdziwych skrzynkach po owocach
BUDZET = 8500   # trójkątów owoców w jednej skrzynce


def get(u):
    with urllib.request.urlopen(urllib.request.Request(u, headers=UA), timeout=120) as r:
        return r.read()


def pobierz_model(i, d):
    p = d / i / (i + ".gltf")
    if p.exists():
        return p
    f = json.loads(get(f"https://api.polyhaven.com/files/{i}"))
    g = f["gltf"]["1k"]["gltf"]
    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_bytes(get(g["url"]))
    for rel, info in (g.get("include") or {}).items():
        q = p.parent / rel
        q.parent.mkdir(parents=True, exist_ok=True)
        q.write_bytes(get(info["url"]))
    return p


def pobierz_teksture(i, d):
    out = {}
    f = json.loads(get(f"https://api.polyhaven.com/files/{i}"))
    for mapa, klucz in (("kolor", "Diffuse"), ("normalna", "nor_gl")):
        q = d / i / (klucz + ".jpg")
        if not q.exists():
            q.parent.mkdir(parents=True, exist_ok=True)
            q.write_bytes(get(f[klucz]["1k"]["jpg"]["url"]))
        out[mapa] = Image.open(q).convert("RGB")
    return out


def czesci(plik, tylko=None):
    """Węzły glTF jako osobne kawałki (pozycje w metrach, normalne, UV, trójkąty) + tekstury materiału."""
    sc = trimesh.load(str(plik), force="scene")
    wynik, mat = [], None
    for node in sc.graph.nodes_geometry:
        if tylko and node != tylko:
            continue
        T, g = sc.graph[node]
        m = sc.geometry[g]
        v = trimesh.transformations.transform_points(m.vertices, T)
        n = m.vertex_normals @ T[:3, :3].T
        n /= np.linalg.norm(n, axis=1, keepdims=True) + 1e-9
        wynik.append((v.astype(np.float32), n.astype(np.float32), np.asarray(m.visual.uv, np.float32), np.asarray(m.faces, np.uint32)))
        mat = mat or m.visual.material
    return wynik, mat


def skladowe(v, f):
    """Spójne kawałki po POZYCJACH (szwy UV rozdzielają wierzchołki — nie mogą rozcinać owocu)."""
    klucz = np.round(v / 1e-5).astype(np.int64)
    _, kan = np.unique(klucz, axis=0, return_inverse=True)
    kan = kan.reshape(-1)
    rodzic = np.arange(kan.max() + 1)

    def znajdz(a):
        while rodzic[a] != a:
            rodzic[a] = rodzic[rodzic[a]]
            a = rodzic[a]
        return a
    for t in f:
        a, b, c = znajdz(kan[t[0]]), znajdz(kan[t[1]]), znajdz(kan[t[2]])
        rodzic[b] = a
        rodzic[znajdz(c)] = a
    grupa = np.array([znajdz(kan[t[0]]) for t in f])
    return [f[grupa == g] for g in np.unique(grupa)]


def uprosc(v, n, uv, f, cel):
    """meshoptimizer: indeksy uproszczone, wierzchołki te same (UV/normalne zachowane), potem zbędne wierzchołki precz."""
    ind = f.reshape(-1).astype(np.uint32)
    if len(f) > cel:
        dest = np.zeros_like(ind)
        for err in (.01, .03, .08, .2, .5):
            k = mo.simplify(dest, ind, np.ascontiguousarray(v), target_index_count=cel * 3, target_error=err)
            if k <= cel * 3 * 1.15:
                break
        if k > cel * 3 * 1.5:
            k = mo.simplify_sloppy(dest, ind, np.ascontiguousarray(v), target_index_count=cel * 3, target_error=.5)
        ind = dest[:k].copy()
    uzyte, nowe = np.unique(ind, return_inverse=True)
    return v[uzyte], n[uzyte], uv[uzyte], nowe.reshape(-1, 3).astype(np.uint32)


def obrot_losowy(rng):
    q = rng.normal(size=4)
    q /= np.linalg.norm(q)
    w, x, y, z = q
    return np.array([[1 - 2 * (y * y + z * z), 2 * (x * y - z * w), 2 * (x * z + y * w)],
                     [2 * (x * y + z * w), 1 - 2 * (x * x + z * z), 2 * (y * z - x * w)],
                     [2 * (x * z - y * w), 2 * (y * z + x * w), 1 - 2 * (x * x + y * y)]], np.float32)


def obrot_lezacy(v, rng):
    """Najdłuższa oś poziomo (owoc leży), losowy obrót wokół niej i losowy kierunek w poziomie."""
    c = v - v.mean(0)
    _, _, vt = np.linalg.svd(c, full_matrices=False)
    os_d = vt[0]
    # baza: dł. oś → X, potem obrót wokół X (przetaczanie) i wokół Y (kierunek)
    a = os_d / np.linalg.norm(os_d)
    b = np.cross(a, [0, 1, 0]) if abs(a[1]) < .9 else np.cross(a, [1, 0, 0])
    b /= np.linalg.norm(b)
    cc = np.cross(a, b)
    B = np.stack([a, b, cc])                          # wiersze: nowe osie X, Y, Z w starym układzie
    t = rng.uniform(0, 2 * math.pi)
    Rx = np.array([[1, 0, 0], [0, math.cos(t), -math.sin(t)], [0, math.sin(t), math.cos(t)]])
    s = rng.uniform(0, 2 * math.pi)
    Ry = np.array([[math.cos(s), 0, math.sin(s)], [0, 1, 0], [-math.sin(s), 0, math.cos(s)]])
    return (Ry @ Rx @ B).astype(np.float32)


def deska(V, N, UV, F, x0, x1, y0, y1, z0, z1, ux=1.0):
    """Prostopadłościan (listewka) z UV wzdłuż najdłuższego boku (słoje wzdłuż listewki)."""
    p = np.array([[x0, y0, z0], [x1, y0, z0], [x1, y1, z0], [x0, y1, z0], [x0, y0, z1], [x1, y0, z1], [x1, y1, z1], [x0, y1, z1]], np.float32)
    sciany = [((0, 3, 2, 1), (0, 0, -1)), ((4, 5, 6, 7), (0, 0, 1)), ((0, 4, 7, 3), (-1, 0, 0)), ((1, 2, 6, 5), (1, 0, 0)), ((3, 7, 6, 2), (0, 1, 0)), ((0, 1, 5, 4), (0, -1, 0))]
    dl = np.argmax([x1 - x0, y1 - y0, z1 - z0])
    off = np.random.default_rng(int(abs(x0 * 1e4 + y0 * 1e3 + z0 * 1e2))).uniform(0, 1, 2)
    for idx, nn in sciany:
        k = len(V)
        for i in idx:
            q = p[i]
            a = q[dl]
            bb = q[(dl + 1) % 3] + q[(dl + 2) % 3]
            V.append(q); N.append(nn); UV.append((a * ux * 1.6 + off[0], bb * 1.6 + off[1]))
        F += [(k, k + 1, k + 2), (k, k + 2, k + 3)]


def skrzynka(L=.50, W=.30, H=.22):
    """Drewniana skrzynka po owocach: 4 słupki, boki z 3 listew ze szparami, czoła z 2 listew, dno z 4 listew."""
    V, N, UV, F = [], [], [], []
    t, s = .009, .022            # grubość listwy, słupek
    hx, hz = L / 2, W / 2
    for sx in (-1, 1):
        for sz in (-1, 1):
            deska(V, N, UV, F, min(sx * hx, sx * (hx - s)), max(sx * hx, sx * (hx - s)), 0, H, min(sz * hz, sz * (hz - s)), max(sz * hz, sz * (hz - s)))
    for (y0, y1) in ((.012, .067), (.087, .142), (.162, .217)):
        for sz in (-1, 1):
            z0, z1 = (hz - t, hz) if sz > 0 else (-hz, -hz + t)
            deska(V, N, UV, F, -hx + .002, hx - .002, y0, y1, z0, z1)
    for (y0, y1) in ((.012, .1), (.13, .217)):
        for sx in (-1, 1):
            x0, x1 = (hx - t, hx) if sx > 0 else (-hx, -hx + t)
            deska(V, N, UV, F, x0, x1, y0, y1, -hz + s, hz - s)
    for z0 in np.linspace(-hz + s, hz - s - .06, 4):
        deska(V, N, UV, F, -hx + s, hx - s, .004, .004 + t, z0, z0 + .06)
    return np.array(V, np.float32), np.array(N, np.float32), np.array(UV, np.float32), np.array(F, np.uint32)


def uloz(kawalki, ksztalt, rng, L=.50, W=.30, H=.22):
    """Owoce „spadają” do skrzynki: kula o promieniu r staje na najniższym miejscu (z 40 losowych prób)."""
    s = .03
    x0, x1, z0, z1 = -L / 2 + s, L / 2 - s, -W / 2 + s, W / 2 - s
    r_sr = np.mean([np.median(np.ptp(k[0], 0)) for k in kawalki]) * .5
    dno = H - 2.3 * r_sr                               # fałszywe dno — półtorej warstwy pod górą skrzynki
    stoi = []                                          # (x, y, z, r)
    out = []

    def kopiec(x, z):
        return .035 * max(0, 1 - (x / (L / 2)) ** 2) * max(0, 1 - (z / (W / 2)) ** 2)
    for _ in range(400):
        k = kawalki[rng.integers(len(kawalki))]
        v = k[0] - (k[0].min(0) + k[0].max(0)) / 2
        R = obrot_lezacy(v, rng) if ksztalt == "podluzny" else obrot_losowy(rng)
        vr = v @ R.T
        roz = np.ptp(vr, 0) / 2
        rh = .5 * (roz[0] + roz[2]) * .92                # promień w poziomie
        ry = roz[1]                                      # połowa wysokości
        naj = None
        for _ in range(40):
            mx, mz = min(roz[0] * .9, (x1 - x0) * .45), min(roz[2] * .9, (z1 - z0) * .45)   # nie wystaje za listwy
            x = rng.uniform(x0 + mx, x1 - mx)
            z = rng.uniform(z0 + mz, z1 - mz)
            y = dno + ry
            for (sx, sy, sz, sr, sry) in stoi:
                d = math.hypot(x - sx, z - sz)
                rr = rh + sr
                if d < rr:
                    y = max(y, sy + math.sqrt(max(0.0, rr * rr - d * d)) * (ry + sry) / max(1e-6, rr))
            if naj is None or y < naj[1]:
                naj = (x, y, z)
        x, y, z = naj
        if y - ry * .35 > H + kopiec(x, z):
            continue
        stoi.append((x, y, z, rh, ry))
        out.append((k, R, np.array([x, y, z], np.float32)))
        if len(out) > 140 or sum(len(o[0][3]) for o in out) > BUDZET:
            break
    return out, dno


def dno_owocowe(kawalki, dno, L=.50, W=.30):
    """Fałszywe dno w kolorze skórki: siatka 6×4 z UV ze skrawka tekstury przy boku owocu."""
    v, _, uv, _ = kawalki[0]
    i = int(np.argmin(v[:, 0]))
    u0 = uv[i]
    xs = np.linspace(-L / 2 + .03, L / 2 - .03, 7)
    zs = np.linspace(-W / 2 + .03, W / 2 - .03, 5)
    V, N, UV, F = [], [], [], []
    for a in xs:
        for b in zs:
            V.append((a, dno + .006, b)); N.append((0, 1, 0)); UV.append(u0 + np.array([a, b]) * .06)
    nz = len(zs)
    for ia in range(len(xs) - 1):
        for ib in range(nz - 1):
            p = ia * nz + ib
            F += [(p, p + 1, p + nz + 1), (p, p + nz + 1, p + nz)]
    return np.array(V, np.float32), np.array(N, np.float32), np.array(UV, np.float32), np.array(F, np.uint32)


def jpg(im, rozmiar, q=86):
    """Zmniejszona tekstura jako JPEG (trimesh zapisuje do .glb w formacie obrazu — bez tego PNG, 4× większy)."""
    b = io.BytesIO()
    im.resize((rozmiar, rozmiar), Image.LANCZOS).save(b, "JPEG", quality=q)
    b.seek(0)
    return Image.open(b)


def material(tex, nrm, rough=.62, rozmiar=512):
    return trimesh.visual.material.PBRMaterial(
        baseColorTexture=jpg(tex, rozmiar),
        normalTexture=jpg(nrm, rozmiar, 92) if nrm is not None else None,
        metallicFactor=0.0, roughnessFactor=rough)


def siatka(V, N, UV, F, mat):
    m = trimesh.Trimesh(vertices=V, faces=F, vertex_normals=N, process=False,
                        visual=trimesh.visual.TextureVisuals(uv=UV, material=mat))
    return m


def zapisz(nazwa, czesci_sceny):
    sc = trimesh.Scene()
    for nm, m in czesci_sceny:
        sc.add_geometry(m, node_name=nm, geom_name=nm)
    LIB.mkdir(parents=True, exist_ok=True)
    p = LIB / (nazwa + ".glb")
    p.write_bytes(trimesh.exchange.gltf.export_glb(sc, include_normals=True))
    tri = sum(len(m.faces) for _, m in czesci_sceny)
    print(f"{p.name}: {tri} trójkątów, {p.stat().st_size // 1024} KB")
    return tri


def wyspy(uv_fs, rozmiar):
    """Maska wysp UV (gdzie tekstura ma treść) — z trójkątów oryginalnego skanu."""
    m = Image.new("L", (rozmiar, rozmiar), 0)
    d = ImageDraw.Draw(m)
    for uv, f in uv_fs:
        for t in f:
            d.polygon([(float(uv[i, 0] % 1) * rozmiar, (1 - float(uv[i, 1] % 1)) * rozmiar) for i in t], fill=255)
    return np.asarray(m) > 0


def rozlej(im, maska):
    """Kolor wysp UV rozlany na tło tekstury (czarne tło + uproszczenie siatki = czarne rysy na owocach)."""
    a = np.asarray(im.convert("RGB"))
    if maska.shape != a.shape[:2]:
        maska = np.asarray(Image.fromarray(maska.astype(np.uint8) * 255).resize(a.shape[1::-1], Image.NEAREST)) > 0
    maska = ndimage.binary_erosion(maska, iterations=2)
    _, (iy, ix) = ndimage.distance_transform_edt(~maska, return_indices=True)
    return Image.fromarray(a[iy, ix])


def tekstury(mat, uv_fs=None):
    nrm = getattr(mat, "normalTexture", None)
    tex, nrm = mat.baseColorTexture.convert("RGB"), (nrm.convert("RGB") if nrm is not None else None)
    if uv_fs:
        mk = wyspy(uv_fs, tex.size[0])
        tex = rozlej(tex, mk)
        nrm = rozlej(nrm, mk) if nrm is not None else None
    return tex, nrm


def szum2(n, skala, oktawy, rng):
    a = np.zeros((n, n))
    for o in range(oktawy):
        m = rng.random((max(2, skala * 2 ** o),) * 2)
        a += np.asarray(Image.fromarray((m * 255).astype(np.uint8)).resize((n, n), Image.BICUBIC), float) / 255 / 2 ** o
    return (a - a.min()) / (a.max() - a.min() + 1e-9)


def rozen_kebaba():
    """MIĘSO KEBABA NA PIONOWYM PRĘCIE (właściciel 10.11 09:10: „kebaba, który się kręci… badyl widać tylko, gdzie wystaje”).
    Stożek mięsa z warstw (nieregularny obwód, płaskie miejsca po ścinaniu), pręt widoczny tylko pod mięsem i nad nim, dociskowy
    talerzyk u góry. Oś obrotu = oś Y modelu (x = z = 0), spód pręta y = 0 — gra kręci tym węzłem (BraxObrot). Tekstura i mapa
    normalnych z kodu (przypieczone warstwy, tłuszcz, zwęglone brzegi)."""
    rng = np.random.default_rng(1977)
    n = 512
    s1, s2, s3 = szum2(n, 6, 4, rng), szum2(n, 24, 3, rng), szum2(n, 64, 2, rng)
    y = np.linspace(0, 1, n)[:, None]
    warstwy = (np.sin(y * 2 * math.pi * 31 + s1 * 9 + s2 * 4) * .5 + .5) ** 6                # warstwy nieregularne, słabe
    x = np.linspace(0, 1, n)[None, :]
    zacieki = (np.sin(x * 2 * math.pi * 23 + s1 * 3) * .5 + .5) ** 8 * (s2 > .4)            # pionowe zacieki tłuszczu
    baza = np.array([104, 52, 24], float)
    a = baza * (.7 + .5 * s2[..., None]) * (.85 + .3 * s3[..., None])
    a -= warstwy[..., None] * np.array([22, 14, 7])
    a += zacieki[..., None] * np.array([40, 26, 12])
    a += ((s3 > .7) * (s2 > .5))[..., None] * np.array([70, 50, 28])               # tłuszcz i jaśniejsze włókna
    a *= 1 - .55 * ((s1 > .74) * (s3 > .45))[..., None]                            # zwęglone brzegi
    kol = Image.fromarray(np.clip(a, 0, 255).astype(np.uint8))
    h = (s3 * .5 + s2 * .3 + warstwy * .2)
    gy, gx = np.gradient(h)
    nrm = np.dstack([-gx * 6, -gy * 6, np.ones_like(h)]); nrm /= np.linalg.norm(nrm, axis=2, keepdims=True)
    nrm_im = Image.fromarray(((nrm * .5 + .5) * 255).astype(np.uint8))
    mat_m = material(kol, nrm_im, .55, 512)
    # stożek: pierścienie co 2 cm, obwód nieregularny (szum po kącie i wysokości), 2 płaskie „ścięcia” od strony grzałki
    V, N, UV, F = [], [], [], []
    seg, pier = 36, 26
    y0, y1 = .07, .6
    ks = rng.random(seg + 1)
    for j in range(pier + 1):
        t = j / pier; yy = y0 + (y1 - y0) * t
        rb = .075 + .06 * t + .025 * math.sin(math.pi * t) - .03 * max(0, t - .9) * 10 * .3   # szerszy u góry, zwęża się w dół (jak prawdziwy kebab)
        for i in range(seg + 1):
            a_ = 2 * math.pi * i / seg
            r = rb * (1 + .07 * math.sin(a_ * 3 + t * 9) + .05 * (ks[i % seg] - .5) + .03 * math.sin(t * 47 + i))
            V.append((math.cos(a_) * r, yy, math.sin(a_) * r)); UV.append((i / seg * 2, t * 1.6))
    for j in range(pier):
        for i in range(seg):
            q = j * (seg + 1) + i
            F += [(q, q + seg + 1, q + 1), (q + 1, q + seg + 1, q + seg + 2)]
    V = np.array(V, np.float32)
    m = trimesh.Trimesh(vertices=V, faces=np.array(F), process=False)
    Nn = m.vertex_normals.astype(np.float32)
    # dekle stożka (góra i dół, ta sama tekstura — przekrój mięsa)
    for yy, rr, odwr in ((y0, None, True), (y1, None, False)):
        jr = 0 if odwr else pier
        k0 = len(V); c = np.array([[0, yy, 0]], np.float32)
        ring = V[jr * (seg + 1): (jr + 1) * (seg + 1)]
        V = np.vstack([V, c, ring]); Nn = np.vstack([Nn, np.tile([0, -1 if odwr else 1, 0], (seg + 2, 1)).astype(np.float32)])
        UV += [(.5, .5)] + [(.5 + .4 * math.cos(2 * math.pi * i / seg), .5 + .4 * math.sin(2 * math.pi * i / seg)) for i in range(seg + 1)]
        for i in range(seg):
            F.append((k0, k0 + 1 + i, k0 + 2 + i) if odwr else (k0, k0 + 2 + i, k0 + 1 + i))
    mieso = siatka(V, Nn, np.array(UV, np.float32), np.array(F, np.uint32), mat_m)
    # pręt (stal) — widoczny tylko pod i nad mięsem; talerzyk dociskowy nad mięsem
    stal = trimesh.visual.material.PBRMaterial(baseColorFactor=[190, 192, 196, 255], metallicFactor=.9, roughnessFactor=.25)
    czesci = []
    for (ya, yb, r) in ((0.0, y0 + .01, .007), (y1 - .01, .76, .007), (y1, y1 + .012, .045)):
        c = trimesh.creation.cylinder(radius=r, height=yb - ya, sections=12)
        c.apply_translation((0, 0, 0)); c.apply_transform(trimesh.transformations.rotation_matrix(-math.pi / 2, (1, 0, 0)))
        c.apply_translation((0, (ya + yb) / 2, 0))
        czesci.append(c)
    pret = trimesh.util.concatenate(czesci)
    pret = siatka(np.asarray(pret.vertices, np.float32), np.asarray(pret.vertex_normals, np.float32), np.zeros((len(pret.vertices), 2), np.float32), np.asarray(pret.faces, np.uint32), stal)
    zapisz("kebab_mieso", [("mieso", mieso), ("pret", pret)])


def main():
    src = pathlib.Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT / ".skany_poly_haven"
    drewno = pobierz_teksture(DREWNO, src)
    mat_d = material(drewno["kolor"], drewno["normalna"], .85, 512)
    wynik = {}
    for nazwa, (ph, cel, ksztalt, dlug) in OWOCE.items():
        rng = np.random.default_rng(sum(map(ord, nazwa)) * 7919)
        cz, mat = czesci(pobierz_model(ph, src))
        kaw = []
        for v, n, uv, f in cz:
            for ff in skladowe(v, f):
                kv, kn, kuv, kf = uprosc(v, n, uv, ff, cel)
                kv = kv - (kv.min(0) + kv.max(0)) / 2
                kaw.append((kv, kn, kuv, kf))
        skala = dlug / np.median([np.ptp(k[0], 0).max() for k in kaw])     # skan w złej skali (imbir 57 cm) → typowa wielkość
        kaw = [(k[0] * skala, k[1], k[2], k[3]) for k in kaw]
        tex, nrm = tekstury(mat, [(c[2], c[3]) for c in cz])
        ul, dno = uloz(kaw, ksztalt, rng)
        V, N, UV, F = [], [], [], []
        k0 = 0
        for (v, n, uv, f), R, p in ul:
            vv = (v - (v.min(0) + v.max(0)) / 2) @ R.T + p
            V.append(vv); N.append(n @ R.T); UV.append(uv); F.append(f + k0); k0 += len(v)
        mat_o = material(tex, nrm, .55, 512)
        owoce = siatka(np.vstack(V), np.vstack(N), np.vstack(UV), np.vstack(F), mat_o)
        skr = siatka(*skrzynka(), mat_d)
        # fałszywe dno: kolor skórki w cieniu (owoce niżej, w półmroku skrzynki) — osobny materiał 8 px
        mk = wyspy([(c[2], c[3]) for c in cz], 256)
        kol = np.asarray(tex.resize((256, 256)))[mk].mean(0) * .42
        cien = Image.new("RGB", (8, 8), tuple(int(x) for x in kol))
        dno_m = siatka(*dno_owocowe(kaw, dno), trimesh.visual.material.PBRMaterial(baseColorTexture=jpg(cien, 8), metallicFactor=0.0, roughnessFactor=.9))
        wynik[nazwa] = zapisz("skrzynka_" + nazwa, [("skrzynka", skr), (nazwa, owoce), ("dno", dno_m)])
        print(f"   {len(ul)} szt. ({ph}, {len(kaw)} kawałków w skanie, po ok. {np.mean([len(k[3]) for k in kaw]):.0f} trójkątów)")
    # kiść bananów (sama kiść, uproszczona)
    ph, wezel, cel = KISC_BANANOW
    cz, mat = czesci(pobierz_model(ph, src), tylko=wezel)
    v, n, uv, f = cz[0]
    v, n, uv, f = uprosc(v, n, uv, f, cel)
    Rz = np.array([[0, -1, 0], [1, 0, 0], [0, 0, 1]], np.float32)    # skan wisi pionowo — kiść leży na półce na boku
    v, n = v @ Rz.T, n @ Rz.T
    v = v - np.array([(v[:, 0].min() + v[:, 0].max()) / 2, v[:, 1].min(), (v[:, 2].min() + v[:, 2].max()) / 2], np.float32)
    tex, nrm = tekstury(mat, [(c[2], c[3]) for c in cz])
    zapisz("banany_kisc", [("banany", siatka(v, n, uv, f, material(tex, nrm, .5, 512)))])
    for ph, (nazwa, cel) in LZEJSZE.items():
        sc = trimesh.load(str(pobierz_model(ph, src)), force="scene")
        wezly = [(node, *sc.graph[node]) for node in sc.graph.nodes_geometry]
        razem = sum(len(sc.geometry[g].faces) for _, _, g in wezly)
        mats, czesci_s, wyspy_mat = {}, [], {}
        for node, T, g in wezly:   # wyspy UV wszystkich węzłów z tym samym materiałem (wspólny atlas — inaczej rozlanie zamaluje sąsiada)
            m = sc.geometry[g]
            wyspy_mat.setdefault(id(m.visual.material), []).append((np.asarray(m.visual.uv, np.float32), np.asarray(m.faces, np.uint32)))
        for node, T, g in wezly:
            m = sc.geometry[g]
            v = trimesh.transformations.transform_points(m.vertices, T).astype(np.float32)
            n = (m.vertex_normals @ T[:3, :3].T).astype(np.float32)
            uv, f = np.asarray(m.visual.uv, np.float32), np.asarray(m.faces, np.uint32)
            mat = m.visual.material
            if id(mat) not in mats:
                rough = float(getattr(mat, "roughnessFactor", None) or .6)
                if getattr(mat, "baseColorTexture", None) is None:
                    mats[id(mat)] = trimesh.visual.material.PBRMaterial(baseColorFactor=mat.baseColorFactor, metallicFactor=0.0, roughnessFactor=rough)
                else:
                    tex, nrm = tekstury(mat, wyspy_mat[id(mat)])
                    mats[id(mat)] = material(tex, nrm, rough, 512)
                    # szkło butelek: w skanie przezroczyste (transmission) — w grze nieprzezroczyste, więc barwa szkła w kolorze
                    # (bez tego jasna tekstura = „butelki mleka”); zielone, brązowe, bursztynowe, przezroczyste lekko szare
                    nm = (getattr(mat, "name", "") or "").lower()
                    if "glass" in nm:
                        kol = (.10, .26, .10) if "green" in nm else (.42, .20, .06) if "orange" in nm else (.50, .40, .12) if "yellow" in nm else (.45, .5, .48)
                        mats[id(mat)].baseColorFactor = [int(c * 255) for c in kol] + [255]
                        mats[id(mat)].roughnessFactor = .12
            v2, n2, uv2, f2 = uprosc(v, n, uv, f, max(60, int(cel * len(f) / razem)))
            czesci_s.append((node, (v2, n2, uv2, f2, mats[id(mat)])))
        allv = np.vstack([c[1][0] for c in czesci_s])
        o = np.array([(allv[:, 0].min() + allv[:, 0].max()) / 2, allv[:, 1].min(), (allv[:, 2].min() + allv[:, 2].max()) / 2], np.float32)
        zapisz(nazwa, [(nm, siatka(v - o, n, uv, f, mt)) for nm, (v, n, uv, f, mt) in czesci_s])
    # pieczywo: bułki ze skanu rozciągnięte w bochenki / bagietki / chleb okrągły (skórka i nacięcia prawdziwe)
    scb = trimesh.load(str(pobierz_model("hamburger_buns", src)), force="scene")
    bulki_s, wysp_b = [], []
    for node in scb.graph.nodes_geometry:
        T, g = scb.graph[node]; m = scb.geometry[g]
        v = trimesh.transformations.transform_points(m.vertices, T).astype(np.float32); n = (m.vertex_normals @ T[:3, :3].T).astype(np.float32)
        uv, f = np.asarray(m.visual.uv, np.float32), np.asarray(m.faces, np.uint32)
        wysp_b.append((uv, f)); mat_bu = m.visual.material
        v, n, uv, f = uprosc(v, n, uv, f, 700)
        bulki_s.append((v - np.array([(v[:, 0].min() + v[:, 0].max()) / 2, v[:, 1].min(), (v[:, 2].min() + v[:, 2].max()) / 2], np.float32), n, uv, f))
    texb, nrmb = tekstury(mat_bu, wysp_b)
    mat_b = material(texb, nrmb, .7, 512)
    for nazwa, (ile, sx, sy, sz) in PIECZYWO.items():
        rng = np.random.default_rng(sum(map(ord, nazwa)))
        V, N, UV, F, k0, x = [], [], [], [], 0, 0.0
        for k in range(ile):
            v, n, uv, f = bulki_s[k % len(bulki_s)]
            # dłuższy wymiar bułki wzdłuż X, potem rozciągnięcie; normalne przez odwrotność skali
            if np.ptp(v[:, 2]) > np.ptp(v[:, 0]): v = v[:, [2, 1, 0]] * np.array([1, 1, -1], np.float32); n = n[:, [2, 1, 0]] * np.array([1, 1, -1], np.float32)
            S = np.array([sx, sy, sz], np.float32) * rng.uniform(.93, 1.07, 3).astype(np.float32)
            vv = v * S; nn = n / S; nn /= np.linalg.norm(nn, axis=1, keepdims=True) + 1e-9
            szer = np.ptp(vv[:, 2])
            if sx > 4:   # bagietki leżą wzdłuż półki, jedna za drugą w głąb
                vv[:, 2] += x + szer / 2; x += szer + .008
            else:        # bochenki poprzecznie do półki, obok siebie
                vv = vv[:, [2, 1, 0]] * np.array([-1, 1, 1], np.float32); nn = nn[:, [2, 1, 0]] * np.array([-1, 1, 1], np.float32)
                vv[:, 0] += x + szer / 2; x += szer + .012
            V.append(vv); N.append(nn); UV.append(uv); F.append(f + k0); k0 += len(vv)
        V = np.vstack(V); V[:, 0] -= (V[:, 0].min() + V[:, 0].max()) / 2; V[:, 2] -= (V[:, 2].min() + V[:, 2].max()) / 2
        zapisz(nazwa, [(nazwa, siatka(V, np.vstack(N), np.vstack(UV), np.vstack(F), mat_b))])
    rozen_kebaba()
    # rośliny w doniczkach (kwiaciarnia)
    pd, wezly_d, cel_d = DONICA
    sc_d = trimesh.load(str(pobierz_model(pd, src)), force="scene")
    don = []
    for node in wezly_d:
        T, g = sc_d.graph[node]; m = sc_d.geometry[g]
        v = trimesh.transformations.transform_points(m.vertices, T).astype(np.float32); n = (m.vertex_normals @ T[:3, :3].T).astype(np.float32)
        don.append(uprosc(v, n, np.asarray(m.visual.uv, np.float32), np.asarray(m.faces, np.uint32), cel_d // len(wezly_d)))
        mat_don = m.visual.material
    tex, nrm = tekstury(mat_don, [(np.asarray(sc_d.geometry[sc_d.graph[w][1]].visual.uv, np.float32), np.asarray(sc_d.geometry[sc_d.graph[w][1]].faces, np.uint32)) for w in sc_d.graph.nodes_geometry])
    mat_don = material(tex, nrm, .8, 512)
    dv = np.vstack([d[0] for d in don]); d_min, d_max = dv.min(0), dv.max(0); d_sr = (d_min + d_max) / 2; d_fi = max(d_max[0] - d_min[0], d_max[2] - d_min[2])
    for nazwa, (ph, wezel, cel) in DONICZKI.items():
        sc = trimesh.load(str(pobierz_model(ph, src)), force="scene")
        T, g = sc.graph[wezel]; m = sc.geometry[g]
        v = trimesh.transformations.transform_points(m.vertices, T).astype(np.float32); n = (m.vertex_normals @ T[:3, :3].T).astype(np.float32)
        v, n, uv, f = uprosc(v, n, np.asarray(m.visual.uv, np.float32), np.asarray(m.faces, np.uint32), cel)
        mn, mx = v.min(0), v.max(0)
        fi = float(np.clip(.5 * min(mx[0] - mn[0], mx[2] - mn[2]), .13, .3))          # średnica doniczki do rośliny
        s_d = fi / d_fi; h_d = (d_max[1] - d_min[1]) * s_d
        czesci_p = []
        for i, (dv2, dn2, duv2, df2) in enumerate(don):
            vv = (dv2 - np.array([d_sr[0], d_min[1], d_sr[2]], np.float32)) * s_d
            czesci_p.append(("doniczka" if i == 0 else "ziemia", siatka(vv, dn2, duv2, df2, mat_don)))
        vp = v - np.array([(mn[0] + mx[0]) / 2, mn[1], (mn[2] + mx[2]) / 2], np.float32) + np.array([0, h_d * .9, 0], np.float32)
        img = m.visual.material.baseColorTexture.convert("RGBA").resize((512, 512), Image.LANCZOS)
        nr = getattr(m.visual.material, "normalTexture", None)
        mat_r = trimesh.visual.material.PBRMaterial(baseColorTexture=img, normalTexture=jpg(nr.convert("RGB"), 512, 92) if nr is not None else None,
                                                     metallicFactor=0.0, roughnessFactor=.7, alphaMode="MASK", alphaCutoff=.5, doubleSided=True)
        czesci_p.append(("roslina", siatka(vp, n, uv, f, mat_r)))
        zapisz(nazwa, czesci_p)
    (LIB / "skrzynki_owocow.CREDITS.md").write_text(
        "# Skrzynki z owocami i warzywami, kiść bananów, lżejsze bułki / konsola / kanister / papierosy / wino, rośliny w doniczkach, mięso kebaba na pręcie (MIASTO R1, 10.11.2026)\n\n"
        "Zrobione skryptem `unity-tools/claude/skrzynki_owocow.py` ze skanów **Poly Haven (CC0)**: "
        + ", ".join(sorted({v[0] for v in OWOCE.values()} | {"bananas", DONICA[0], "hamburger_buns"} | set(LZEJSZE) | {v[0] for v in DONICZKI.values()})) +
        "; drewno skrzynki: tekstura `" + DREWNO + "` (Poly Haven, CC0). Owoce uproszczone (meshoptimizer), tekstury 512 px.\n"
        "Licencja CC0 — można rozpowszechniać w publicznym repozytorium.\n", encoding="utf-8")


if __name__ == "__main__":
    main()
