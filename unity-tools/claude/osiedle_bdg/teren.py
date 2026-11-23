"""MIASTO R1 — osiedle bydgoskie: teren z mapy OpenStreetMap (ODbL) — jezdnie, parkingi, chodniki, krawężniki,
oznakowanie głównej ulicy, trawniki, boiska; punkty drzew i latarni do wstawienia modelami w grze.

Szerokości jezdni ze zdjęć właściciela: Skłodowskiej-Curie 2 pasy po 3,5 m (IMG_1609, IMG_1598), uliczki osiedlowe
4,5–5 m (IMG_1427, IMG_1630), chodniki 2 m z kostki (IMG_1427), krawężnik 15 cm. Poziomy: jezdnia i parking 0,
chodnik +0,12, trawnik +0,08. Linie i przejścia są WYCIĘTE z asfaltu (nic nie leży na asfalcie — zasada migotania).
Drzewa i latarnie: szpalery i rzędy z OSM (tree_row) + rozmieszczenie wzdłuż głównej ulicy i uliczek jak na zdjęciach
(co ~8 m drzewa, co ~30 m latarnie) — położenie pojedynczych drzew jest przybliżone (lista braków).
"""
import math
import sys
from pathlib import Path

import numpy as np
import shapely
from shapely.geometry import Polygon, MultiPolygon, LineString, box
from shapely.ops import unary_union

sys.path.insert(0, str(Path(__file__).resolve().parent))
from kit import Siatka, UP  # noqa: E402

SZER = {"primary": 7.4, "secondary": 7.0, "tertiary": 6.5, "tertiary_link": 5.0, "residential": 5.5, "service": 4.6,
        "unclassified": 5.0}
SZER_PIESZE = {"footway": 2.0, "path": 1.6, "pedestrian": 3.0, "steps": 2.0, "cycleway": 2.0}
OKNO = box(-195, -195, 190, 190)          # wycinek osiedla w metrach (jak plan)
Y_JEZDNIA, Y_CHODNIK, Y_TRAWA = 0.0, 0.12, 0.08


def polys(g):
    if g.is_empty: return []
    if isinstance(g, Polygon): return [g]
    if isinstance(g, MultiPolygon): return list(g.geoms)
    return [p for p in getattr(g, "geoms", []) if isinstance(p, Polygon)]


def plaski(S, grupa, mat, geom, y, kier=1):
    """Poziome trójkąty (ograniczona triangulacja Delaunaya), UV w metrach (x, z)."""
    for p in polys(geom):
        if p.area < .05: continue
        tri = shapely.constrained_delaunay_triangles(p)
        for t in tri.geoms:
            c = list(t.exterior.coords)[:3]
            a, b, d = (np.array([x, y, z]) for x, z in c)
            if np.cross(b - a, d - a)[1] < 0: b, d = d, b
            c = S._c(grupa, mat); base = len(c["v"])
            for p3 in (a, b, d):
                c["v"].append(tuple(p3)); c["n"].append((0., 1., 0.)); c["t"].append((1., 0., 0., -1.))
                c["uv"].append((p3[0], p3[2])); c["uv1"].append((0., 0.))
            c["i"] += [base, base + 1, base + 2]


def boki(S, grupa, mat, geom, y_gora, y_dol):
    """Pionowe ścianki po obwodzie (krawężnik, obrzeże), skierowane na zewnątrz wielokąta."""
    for p in polys(geom):
        for ring in [p.exterior] + list(p.interiors):
            pts = list(ring.coords)
            ccw = ring.is_ccw
            for (x0, z0), (x1, z1) in zip(pts, pts[1:]):
                a = np.array([x0, y_dol, z0]); b = np.array([x1, y_dol, z1])
                e = b - a; L = np.linalg.norm(e)
                if L < 1e-3: continue
                n = np.array([e[2], 0, -e[0]]) / L          # prawa normalna kierunku krawędzi (x,z)
                if ring is p.exterior and ccw: n = -n
                if ring is not p.exterior and not ccw: n = -n
                S.quad(grupa, mat, a, b, b + UP * (y_gora - y_dol), a + UP * (y_gora - y_dol), kier=n)


def linie_glownej(d, jezdnie_glowne):
    """Podwójna ciągła na osi Skłodowskiej-Curie (IMG_1598, IMG_1609) i przejścia „zebra” przy skrzyżowaniach."""
    znaki = []
    for u in d["ulice"]:
        if u["klasa"] != "primary" or len(u["linia"]) < 2: continue
        L = LineString(u["linia"]).intersection(OKNO)
        if L.is_empty: continue
        for off in (-.12, .12):
            try: znaki.append(L.parallel_offset(off, "left").buffer(.06, cap_style=2))
            except Exception: pass
    linie = unary_union(znaki).intersection(jezdnie_glowne) if znaki else Polygon()
    return linie


def przedpole(A, B, n, maks, jezdnie):
    """Kostka od ściany do jezdni: głębokość = mediana odległości do jezdni wzdłuż normalnej (z 7 promieni), najwyżej maks."""
    A, B, n = np.asarray(A, float), np.asarray(B, float), np.asarray(n, float)
    gl = []
    for t in np.linspace(.05, .95, 7):
        p = A + (B - A) * t
        ray = LineString([tuple(p), tuple(p + n * maks)])
        x = ray.intersection(jezdnie)
        gl.append(maks if x.is_empty else max(1.2, shapely.geometry.Point(*p).distance(x) + .3))
    d = float(np.median(gl))
    return Polygon([tuple(A - (B - A) * .02), tuple(B + (B - A) * .02), tuple(B + n * d), tuple(A + n * d)])


def zbuduj(d, budynki_poly, przedpola=()):
    S = Siatka()
    jezdnie, glowne, piesze, parkingi, boiska, place = [], [], [], [], [], []
    for u in d["ulice"]:
        if len(u["linia"]) < 2: continue
        ln = LineString(u["linia"])
        if u["klasa"] in SZER:
            g = ln.buffer(SZER[u["klasa"]] / 2, cap_style=2, join_style=1)
            jezdnie.append(g)
            if u["klasa"] == "primary": glowne.append(g)
        elif u["klasa"] in SZER_PIESZE:
            piesze.append(ln.buffer(SZER_PIESZE[u["klasa"]] / 2, cap_style=1, join_style=1))
    for t in d["tereny"]:
        if len(t["linia"]) < 3: continue
        try: p = Polygon(t["linia"]).buffer(0)
        except Exception: continue
        if t["rodzaj"] == "parking": parkingi.append(p)
        elif t["rodzaj"] == "pitch": boiska.append(p)
        elif t["rodzaj"] == "playground": place.append(p)
    bud = unary_union(budynki_poly)
    JEZ = unary_union(jezdnie).intersection(OKNO)
    GLOW = unary_union(glowne).intersection(OKNO)
    PARK = unary_union(parkingi).difference(JEZ).intersection(OKNO)
    POZIOM0 = unary_union([JEZ, PARK])
    # chodnik: ścieżki z mapy + opaska 1,2 m wokół budynków (kostka pod ścianami jak na zdjęciach) + 2 m wzdłuż głównej
    opaska = bud.buffer(1.2, join_style=2).difference(bud)
    wzdluz = GLOW.buffer(2.3, join_style=2).difference(GLOW)
    # chodniki 1,8 m wzdłuż uliczek osiedlowych po obu stronach (IMG_1427, IMG_1630: kostka przy każdej uliczce)
    uliczki = [LineString(u["linia"]).buffer(SZER[u["klasa"]] / 2 + 1.8, cap_style=2, join_style=1)
               for u in d["ulice"] if u["klasa"] in ("service", "residential") and len(u["linia"]) >= 2]
    przy_uliczkach = unary_union(uliczki).difference(JEZ) if uliczki else Polygon()
    prz_k = [przedpole(A, B, n, m, POZIOM0) for (A, B, n, m, mat) in przedpola if mat == "kostka"]
    prz_g = [przedpole(A, B, n, m, POZIOM0) for (A, B, n, m, mat) in przedpola if mat == "plac_garazy"]
    PLAC_G = unary_union(prz_g).difference(POZIOM0).difference(bud).intersection(OKNO) if prz_g else Polygon()
    CHOD = unary_union(piesze + [opaska, wzdluz, przy_uliczkach] + prz_k).difference(POZIOM0).difference(bud).difference(PLAC_G).intersection(OKNO)
    BOI = unary_union(boiska).difference(POZIOM0).difference(CHOD).difference(bud).intersection(OKNO)
    PLAC = unary_union(place).difference(POZIOM0).difference(CHOD).difference(bud).difference(BOI).intersection(OKNO)
    TRAWA = OKNO.difference(unary_union([POZIOM0, CHOD, BOI, PLAC, PLAC_G, bud]))
    # krawężnik: pas 0,15 m po stronie chodnika/trawnika wzdłuż krawędzi jezdni i parkingów
    KRAW = POZIOM0.buffer(.15, join_style=2).difference(POZIOM0).difference(bud).intersection(OKNO)
    CHOD = CHOD.difference(KRAW); TRAWA = TRAWA.difference(KRAW)
    LIN = linie_glownej(d, GLOW)
    ASF = JEZ.difference(LIN)
    plaski(S, "teren", "asfalt", ASF.simplify(.05), Y_JEZDNIA)
    plaski(S, "teren", "linia_biala", LIN.simplify(.01), Y_JEZDNIA)
    plaski(S, "teren", "asfalt_parking", PARK.simplify(.05), Y_JEZDNIA)
    plaski(S, "teren", "asfalt_parking", PLAC_G.difference(KRAW_PRE := POZIOM0.buffer(.15, join_style=2)).simplify(.05), Y_JEZDNIA + .01)
    plaski(S, "teren", "kostka", CHOD.simplify(.05), Y_CHODNIK)
    plaski(S, "teren", "kraweznik", KRAW.simplify(.02), Y_CHODNIK + .01)
    boki(S, "teren", "kraweznik", KRAW.simplify(.02), Y_CHODNIK + .01, Y_JEZDNIA - .02)
    boki(S, "teren", "kostka", CHOD.simplify(.05), Y_CHODNIK, Y_TRAWA - .02)
    plaski(S, "teren", "boisko", BOI.simplify(.05), Y_CHODNIK - .02)
    plaski(S, "teren", "plac_zabaw", PLAC.simplify(.05), Y_TRAWA + .01)
    plaski(S, "trawa", "trawa", TRAWA.simplify(.1), Y_TRAWA)
    pola = {k: round(v.area) for k, v in (("jezdnie", JEZ), ("parkingi", PARK), ("chodniki", CHOD), ("trawa", TRAWA),
                                           ("boiska", BOI), ("place_zabaw", PLAC), ("krawezniki", KRAW))}
    return S, pola, {"JEZ": JEZ, "GLOW": GLOW, "PARK": PARK, "CHOD": CHOD, "TRAWA": TRAWA, "bud": bud}


def punkty(d, G, seed=5):
    """Drzewa i latarnie: wzdłuż głównej ulicy po obu stronach (pas zieleni, co 8 m drzewa, co 32 m latarnie — IMG_1598,
    IMG_1602), wzdłuż uliczek osiedlowych latarnie co 28 m, szpalery z OSM; drzewa w trawnikach tam, gdzie są na zdjęciach
    (gęsto przy blokach) — rozmieszczenie przybliżone, min. 4 m od ścian i 2,5 m od jezdni."""
    import random
    R = random.Random(seed)
    drzewa, latarnie = [], []
    trawa = G["TRAWA"]; bud = G["bud"]; jez = unary_union([G["JEZ"], G["PARK"]])
    for u in d["ulice"]:
        if len(u["linia"]) < 2: continue
        ln = LineString(u["linia"]).intersection(OKNO)
        if ln.is_empty or ln.geom_type != "LineString": continue
        if u["klasa"] == "primary":
            for strona in ("left", "right"):
                try: lo = ln.parallel_offset(SZER["primary"] / 2 + 3.2, strona)
                except Exception: continue
                if lo.geom_type != "LineString": continue
                s = 4.0
                while s < lo.length:
                    p = lo.interpolate(s)
                    if trawa.buffer(-.5).contains(p): drzewa.append((p.x, p.y, "mlode"))
                    s += 8.0
                s = 10.0
                while s < lo.length:
                    p = lo.interpolate(s)
                    if not bud.buffer(1).contains(p) and not jez.contains(p): latarnie.append((p.x, p.y, "ulica"))
                    s += 32.0
        elif u["klasa"] in ("service", "residential"):
            try: lo = ln.parallel_offset(SZER.get(u["klasa"], 4.6) / 2 + .8, "left")
            except Exception: continue
            if lo.geom_type != "LineString": continue
            s = 6.0
            while s < lo.length:
                p = lo.interpolate(s)
                if not bud.buffer(.8).contains(p) and not jez.contains(p): latarnie.append((p.x, p.y, "osiedle"))
                s += 28.0
    for t in d["tereny"]:
        if t["rodzaj"] == "tree_row" and len(t["linia"]) >= 2:
            ln = LineString(t["linia"]); s = 2.0
            while s < ln.length:
                p = ln.interpolate(s)
                if trawa.contains(p): drzewa.append((p.x, p.y, "duze"))
                s += 7.0
    # drzewa w trawnikach: siatka z rozrzutem, gęstość ~1 na 180 m² w pasie 6–25 m od bloków (jak na zdjęciach)
    pas = bud.buffer(25).difference(bud.buffer(5)).intersection(trawa.buffer(-2.5))
    minx, minz, maxx, maxz = pas.bounds; krok = 13.5
    x = minx
    while x < maxx:
        z = minz
        while z < maxz:
            px, pz = x + R.uniform(-4, 4), z + R.uniform(-4, 4)
            p = shapely.geometry.Point(px, pz)
            if pas.contains(p) and all((px - a) ** 2 + (pz - b) ** 2 > 36 for a, b, _ in drzewa[-200:]):
                drzewa.append((px, pz, R.choice(["duze", "duze", "srednie", "iglaste"])))
            z += krok
        x += krok
    return [(round(a, 2), round(b, 2), c) for a, b, c in drzewa], [(round(a, 2), round(b, 2), c) for a, b, c in latarnie]
