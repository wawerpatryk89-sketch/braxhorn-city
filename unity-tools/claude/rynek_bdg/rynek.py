#!/usr/bin/env python3
"""MIASTO R1 — Stary Rynek, Mostowa, Focha, Opera i Brda (Bydgoszcz): etap 1 — bryły kamienic z obrysów OpenStreetMap,
płyta Rynku, ulice, Brda z nabrzeżami i most na Mostowej. Te same elementy co osiedle (osiedle_bdg: kit, paleta, ogolny).

Źródła: dane/rynek_osm.json (OSM, ODbL; pobierz_osm.py) i zdjęcia właściciela (paczka braxhorn-zdjecia-bydgoszcz-01,
zdjecia/stare_miasto): kamienice Rynku 3–4 kondygnacje, pastelowe tynki (żółty, kremowy, szaroniebieski, różowy, biały),
strome dachy z czerwonej dachówki, gzymsy; płyta Rynku z jasnych płyt granitowych; Focha — wysokie kamienice 4–5 kond.
Wysokości bez znacznika w OSM: ZAŁOŻENIE wg zdjęć (opis w gen/rynek_opis.json, pole „zrodlo”).

Układ: metry od kotwicy (dane/rynek_rzut.json); w grze PunktZerowy = gra_a, × K.
    rynek.py <katalog wyjściowy>      → rynek.glb (podgląd), rynek.bin (gra, BYD2), rynek_opis.json
"""
import json
import math
import random
import sys
from pathlib import Path

import numpy as np
import shapely
from shapely.geometry import LineString, Point, Polygon, box
from shapely.ops import unary_union

TU = Path(__file__).resolve().parent
sys.path.insert(0, str(TU.parent / "osiedle_bdg"))
from kit import Siatka, UP, nrm, zapisz_bin, zapisz_glb, trojkaty, scal_wg_materialu, scal_wg_budynku  # noqa: E402
import ogolny  # noqa: E402
import paleta  # noqa: E402

R = json.loads((TU / "dane" / "rynek_rzut.json").read_text())
K = R["K"]; O = np.array(R["gra_a"])
# okno etapu 1: Rynek, Mostowa z mostem, spichrze, Opera, Focha (metry od kotwicy; Rynek ~ (−180…−60, −230…−150))
OKNO = box(-420, -340, 160, 160)
KOLORY = ["k_zolty", "k_krem", "k_szaroniebieski", "k_rozowy", "k_bialy", "k_ochra", "k_zielony", "k_terakota"]


def wczytaj():
    d = json.loads((TU / "dane" / "rynek_osm.json").read_text(encoding="utf-8"))
    for b in d["budynki"]:
        try: b["poly"] = Polygon(b["obrys"]).buffer(0)
        except Exception: b["poly"] = Polygon()
    return d


def woda(d):
    """Brda i Młynówka: pełne multipoligony wody z API OSM (dane/woda_rel_*.xml — relacje z mapy wycinka miały ucięte drogi
    na krawędzi), pierścienie sklejone z dróg członkowskich (outer minus inner), przycięte do okna."""
    import xml.etree.ElementTree as ET
    from shapely.ops import polygonize
    la, lo, MLAT, MLON = R["lat_a"], R["lon_a"], R["MLAT"], R["MLON"]
    zew, wew = [], []
    for plik in sorted((TU / "dane").glob("woda_rel_*.xml")):
        r = ET.parse(plik).getroot()
        nd = {n.get("id"): ((float(n.get("lon")) - lo) * MLON, (float(n.get("lat")) - la) * MLAT) for n in r.iter("node")}
        ways = {w.get("id"): [nd[x.get("ref")] for x in w.iter("nd") if x.get("ref") in nd] for w in r.iter("way")}
        for rel in r.iter("relation"):
            for rola, lista in (("outer", zew), ("inner", wew)):
                linie = [LineString(ways[m.get("ref")]) for m in rel.iter("member") if m.get("type") == "way" and m.get("role") == rola and len(ways.get(m.get("ref"), [])) > 1]
                if linie: lista.extend(polygonize(unary_union(linie)))
    W = unary_union(zew).difference(unary_union(wew)) if zew else Polygon()
    return W.intersection(OKNO)


def przestrzen_publiczna(d, W):
    """Ulice, place, bulwary i woda (to, co widać z ulicy): okno/witryna patrzy tu → pełny detal; inaczej podwórze."""
    szer = {"primary": 14, "secondary": 12, "tertiary": 10, "residential": 7, "service": 5, "unclassified": 6, "living_street": 6,
            "pedestrian": 6, "footway": 2.5, "path": 2.0, "cycleway": 2.0, "steps": 2.0}
    g = [LineString(u["linia"]).buffer(szer[u["klasa"]] / 2 + 3.0) for u in d["ulice"]
         if len(u["linia"]) > 1 and u["klasa"] in szer and not (u["klasa"] in ("service", "footway", "path") and not u.get("nazwa"))]
    g += [Polygon(t["linia"]).buffer(0) for t in d["tereny"] if t["rodzaj"] in ("square", "park", "parking") and len(t["linia"]) > 3 and t["linia"][0] == t["linia"][-1]]
    g += [Polygon(u["linia"]).buffer(0) for u in d["ulice"] if u.get("obszar") and len(u["linia"]) > 3 and u["linia"][0] == u["linia"][-1]]
    return unary_union(g + [W.buffer(6.0)])


def spec(b, R_):
    """Kamienica: kondygnacje z OSM albo założenie wg zdjęć (Rynek 3–4, Focha/Gdańska 4–5); kolor tynku losowy z palety
    zdjęć, ale stały dla budynku; dach stromy z dachówki dla obrysów prawie prostokątnych, płaski dla nieregularnych."""
    P = b["poly"]; nazwa = b.get("nazwa") or ""
    pi = b.get("pietra"); zr = "OSM %s p." % pi if pi and pi.isdigit() else None
    if zr: kond = int(pi)
    else:
        ul = (b.get("ulica") or "")
        kond = 5 if ul.startswith(("Marszałka Focha", "Gdańska", "Jagiellońska")) else 4 if P.area > 180 else 3
        zr = "ZAŁ %d kond. wg zdjęć (%s)" % (kond, ul or "bez adresu")
    prost = P.area / max(P.minimum_rotated_rectangle.area, 1e-6)
    return dict(kond=max(1, min(kond, 7)), h_kond=3.5, parter=4.3, tynk=R_.choice(KOLORY), cokol="cokol4", h_cokol=.6,
                okna=(2.3, 1.2, 1.9, .9), witryny=bool(b.get("ulica")), gzymsy=True, gzyms_mat="opaska",
                dach="kamienica" if P.area < 1600 else "plaski", dach_mat="dachowka", spadek=42, attyka=.9 if P.area >= 1600 else .35,
                kalenica_max=6.5, zr=zr)


def teren(d, bud, W):
    """Płyta Rynku (place/pedestrian area), ulice (bufory wg klasy), chodniki, trawniki, woda Brdy z nabrzeżami."""
    S = Siatka()
    szer = {"primary": 14, "secondary": 12, "tertiary": 10, "residential": 7, "service": 5, "unclassified": 6, "living_street": 6,
            "pedestrian": 6, "footway": 2.5, "path": 2.0, "cycleway": 2.0, "steps": 2.0}
    jezd, piesze, place, mosty = [], [], [], []
    for u in d["ulice"]:
        if len(u["linia"]) < 2 or u["klasa"] not in szer: continue
        ln = LineString(u["linia"])
        if u.get("obszar") and u["linia"][0] == u["linia"][-1] and len(u["linia"]) > 3:
            place.append(Polygon(u["linia"]).buffer(0)); continue
        g = ln.buffer(szer[u["klasa"]] / 2, cap_style=2, join_style=1)
        if u.get("most") in ("yes", "movable", "viaduct"): mosty.append((ln, szer[u["klasa"]], u)); continue
        (piesze if u["klasa"] in ("pedestrian", "footway", "path", "cycleway", "steps") else jezd).append(g)
    for t in d["tereny"]:
        if t["rodzaj"] in ("square",) and len(t["linia"]) > 3: place.append(Polygon(t["linia"]).buffer(0))
    JEZ = unary_union(jezd).intersection(OKNO).difference(W)
    PLAC = unary_union(place).intersection(OKNO).difference(W).difference(bud)
    CHOD = unary_union(piesze + [bud.buffer(2.2, join_style=2), JEZ.buffer(2.6, join_style=2)]).intersection(OKNO).difference(JEZ).difference(W).difference(bud).difference(PLAC)
    NABRZEZE = W.buffer(3.0, join_style=2).difference(W).intersection(OKNO).difference(bud)
    TRAWA = OKNO.difference(unary_union([JEZ, PLAC, CHOD, W, bud, NABRZEZE]))

    def plaski(mat, g, y):
        for q in getattr(g, "geoms", [g]):
            if q.is_empty or q.geom_type != "Polygon" or q.area < .05: continue
            for tri in shapely.constrained_delaunay_triangles(q).geoms:
                c = [np.array([x, y, z]) for x, z in list(tri.exterior.coords)[:3]]
                if np.cross(c[1] - c[0], c[2] - c[0])[1] < 0: c[1], c[2] = c[2], c[1]
                S.tri_list("teren", mat, c, (0., 1., 0.), (1., 0., 0.), [(p[0], p[2]) for p in c], None, [0, 1, 2])
    plaski("asfalt", JEZ.simplify(.05), 0.0)
    plaski("rynek_plyty", PLAC.simplify(.05), .12)
    plaski("kostka", CHOD.simplify(.05), .12)
    plaski("nabrzeze", NABRZEZE.simplify(.05), .15)
    plaski("trawa", TRAWA.simplify(.1), .08)
    plaski("woda_brda", W.simplify(.1), -1.6)
    # mur nabrzeża: od wody (−1,6) do bulwaru (+0,15), granitowy, po obwodzie wody (bez odcinków na krawędzi okna)
    for q in getattr(W, "geoms", [W]):
        if q.is_empty: continue
        for ring in [q.exterior] + list(q.interiors):
            pts = list(ring.coords)
            for (x0, z0), (x1, z1) in zip(pts, pts[1:]):
                a = np.array([x0, 0., z0]); b_ = np.array([x1, 0., z1]); L = float(np.linalg.norm(b_ - a))
                if L < .05 or not OKNO.buffer(-.5).contains(Point((x0 + x1) / 2, (z0 + z1) / 2)): continue
                t = (b_ - a) / L; n = nrm(np.cross(UP, t)); m = (a + b_) / 2 + n * .3
                if not W.contains(Point(m[0], m[2])): n = -n                       # ściana patrzy na wodę
                S.quad("teren", "mur_nabrzeza", a + UP * -1.7, b_ + UP * -1.7, b_ + UP * .15, a + UP * .15, kier=n)
                S.box("teren", "mur_nabrzeza", a + UP * .15 - n * .05, t, UP, -n, L, .9, .35)          # murek-balustrada
    return S, {"jezdnie": round(JEZ.area), "rynek": round(PLAC.area), "chodniki": round(CHOD.area), "trawa": round(TRAWA.area), "woda": round(W.area)}, mosty


def mosty_scal(S, mosty, W):
    """Mosty nad Brdą: wszystkie drogi bridge=* (Mostowa + jej chodniki jako osobne drogi OSM, kładka Kiepury) scalone w jeden
    pomost na zespół — bez nakładających się płyt (zero migotania). Pomost 0,62 m, wierzch +0,17 (2 cm nad bulwarem +0,15),
    kostka; balustrada stalowa (pochwyt + pręty co 0,25 m) tylko na krawędziach nad wodą; przyczółki z granitu."""
    if not mosty: return 0
    POM = unary_union([ln.buffer(w / 2 + 1.0, cap_style=2, join_style=2) for (ln, w, u) in mosty]).intersection(OKNO)
    n_ = 0
    for q in getattr(POM, "geoms", [POM]):
        if q.geom_type != "Polygon" or q.area < 5: continue
        q = q.simplify(.1); n_ += 1
        for tri in shapely.constrained_delaunay_triangles(q).geoms:
            c = [np.array([x, .17, z]) for x, z in list(tri.exterior.coords)[:3]]
            if np.cross(c[1] - c[0], c[2] - c[0])[1] < 0: c[1], c[2] = c[2], c[1]
            S.tri_list("mosty", "kostka", c, (0., 1., 0.), (1., 0., 0.), [(p_[0], p_[2]) for p_ in c], None, [0, 1, 2])
            S.tri_list("mosty", "mur_nabrzeza", [p_ + UP * -.62 for p_ in c[::-1]], (0., -1., 0.), (1., 0., 0.), [(p_[0], p_[2]) for p_ in c[::-1]], None, [0, 1, 2])
        pts = list(shapely.geometry.polygon.orient(q, 1.0).exterior.coords)
        Wb = W.buffer(.5)
        for (x0, z0), (x1, z1) in zip(pts, pts[1:]):
            a_ = np.array([x0, 0., z0]); b_ = np.array([x1, 0., z1]); L = float(np.linalg.norm(b_ - a_))
            if L < .05: continue
            t = (b_ - a_) / L; n = nrm(np.cross(UP, t))                      # CCW: zewnętrze po prawej = n
            if q.contains(Point(*((a_ + b_) / 2 + n * .3)[[0, 2]])): n = -n
            S.quad("mosty", "mur_nabrzeza", a_ + UP * -.45, b_ + UP * -.45, b_ + UP * .17, a_ + UP * .17, kier=n)   # czoło pomostu
            if not Wb.contains(Point((x0 + x1) / 2, (z0 + z1) / 2)): continue  # balustrada tylko nad wodą
            e = a_ - n * .1
            S.box("mosty", "stal_balustrada", e + UP * 1.05, t, UP, n, L, .06, .08)
            S.box("mosty", "stal_balustrada", e + UP * .27, t, UP, n, L, .04, .05)
            for k in np.arange(.12, L, .25): S.rura("mosty", "stal_balustrada", e + t * k + UP * .17, e + t * k + UP * 1.05, .012, seg=4)
    return n_


def main(out):
    out = Path(out); out.mkdir(parents=True, exist_ok=True)
    d = wczytaj(); R_ = random.Random(11)
    W = woda(d)
    D = Siatka(); opis = {}
    budynki = [b for b in d["budynki"] if not b["poly"].is_empty and b["poly"].area > 25 and OKNO.contains(b["poly"].centroid) and not b.get("czesc")]
    from shapely.strtree import STRtree
    from shapely.prepared import prep
    drzewo = STRtree([b["poly"] for b in budynki])
    PUBL = prep(przestrzen_publiczna(d, W))
    for i, b in enumerate(budynki):
        sp = spec(b, R_)
        sas = [budynki[j]["poly"] for j in drzewo.query(b["poly"].buffer(1.2)) if j != i]
        sp["slepe"] = prep(unary_union(sas).buffer(.15)) if sas else None      # ściany przy sąsiedzie (pierzeja) — ślepe
        sp["ulica_geom"] = PUBL; sp["okno_lekkie"] = True
        S = ogolny.zbuduj(b["poly"], sp, "k%d" % i)
        for (g, m), c in S.dane().items():
            dd = D._c(g, m); b0 = len(dd["v"])
            for f in ("v", "n", "t", "uv", "uv1"): dd[f] += c[f]
            dd["i"] += [b0 + q for q in c["i"]]
        opis["k%d" % i] = {"nazwa": b.get("nazwa") or ("%s %s" % (b.get("ulica") or "", b.get("nr") or "")).strip(), "kond": sp["kond"], "zrodlo": sp["zr"]}
    bud = unary_union([b["poly"] for b in budynki])
    T, pola, mosty = teren(d, bud, W)
    pola["mosty_zespoly"] = mosty_scal(T, mosty, W)
    for (g, m), c in T.dane().items():
        dd = D._c(g, m); b0 = len(dd["v"])
        for f in ("v", "n", "t", "uv", "uv1"): dd[f] += c[f]
        dd["i"] += [b0 + q for q in c["i"]]
    zapisz_glb(scal_wg_materialu(D), out / "rynek.glb")
    G = scal_wg_budynku(D)
    uzyte = sorted({m for _, m in G.dane()})
    brak = [m for m in uzyte if m not in paleta.P and m not in ("okno", "okno_klatka")]
    if brak: raise SystemExit("materiały bez palety: " + ", ".join(brak))
    zapisz_bin(G, {m: paleta.P.get(m, {"okno": True}) for m in uzyte}, out / "rynek.bin",
               {"punkt_zerowy_gry": list(O), "K": K, "jednostka": "metr"}, "Stary Rynek, Mostowa, Focha, Brda — Bydgoszcz (OSM ODbL)")
    opis_all = {"budynki": opis, "teren": pola, "mostow": len(mosty), "trojkatow": trojkaty(D)}
    (out / "rynek_opis.json").write_text(json.dumps(opis_all, ensure_ascii=False, indent=1), encoding="utf-8")
    print(json.dumps({k: v for k, v in opis_all.items() if k != "budynki"} | {"budynkow": len(opis)}, ensure_ascii=False))


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else TU / "gen")
