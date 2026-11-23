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
KOLORY = ["k_zolty", "k_krem", "k_szaroniebieski", "k_rozowy", "k_bialy", "k_ochra", "k_zielony", "k_terakota",
          "k_kremowy2", "k_jasnozolty", "k_losos", "k_niebieskoszary", "k_kremowy2", "k_jasnozolty", "k_losos"]


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


def dzialki(budynki, PUBL, R_):
    """Pierzeja: długi obrys OSM od ulicy (często cały ciąg kamienic jako jeden budynek) dzielony na działki 8–13 m
    prostopadle do frontu — każda działka to osobna kamienica (wysokość, kolor, dach, ozdoby). Zdjęcia 02–03: na Rynku
    kamienice mają 3–5 osi okiennych. Budynki z nazwą (urząd, kościół, opera) zostają w całości."""
    from shapely.ops import split
    from shapely.geometry.polygon import orient
    out = []
    for b in budynki:
        P = b["poly"]
        if b.get("nazwa") or P.area < 150: out.append(b); continue
        pts = list(orient(P.simplify(.15), 1.0).exterior.coords); best = None
        for (x0, z0), (x1, z1) in zip(pts, pts[1:]):
            a_ = np.array([x0, z0]); c_ = np.array([x1, z1]); L = float(np.linalg.norm(c_ - a_))
            if L < 16: continue
            t = (c_ - a_) / L; n = np.array([t[1], -t[0]])                   # CCW: zewnętrze po prawej
            if not PUBL.contains(Point(*((a_ + c_) / 2 + n * 7))): continue
            if best is None or L > best[0]: best = (L, a_, t, n)
        if not best: out.append(b); continue
        L, a_, t, n = best; ciecia = []; s_ = 0.0
        while True:
            s_ += R_.uniform(8.0, 13.0)
            if s_ > L - 6.5: break
            ciecia.append(s_)
        kawalki = [P]
        for s_ in ciecia:
            p0 = a_ + t * s_; ln = LineString([tuple(p0 + n * 5), tuple(p0 - n * 90)]); nowe = []
            for q in kawalki:
                try: nowe.extend(g_ for g_ in split(q, ln).geoms if g_.geom_type == "Polygon")
                except Exception: nowe.append(q)
            kawalki = nowe
        for q in kawalki:
            if q.area < 12: continue
            nb = dict(b); nb["poly"] = q; nb["dzialka"] = True; out.append(nb)
    return out


def spec(b, R_):
    """Kamienica: kondygnacje z OSM albo założenie wg zdjęć (Rynek 3–4, Focha/Gdańska 4–5) ± 1 między sąsiednimi
    działkami; wysokie kondygnacje kamienic (3,6–4,1 m), parter 4,3–4,9 m; wąskie wysokie okna (zdjęcie 02: ok. 1,1 × 2,0 m
    co 2,0–2,4 m); kolor tynku, odcień dachówki, boniowany parter i ozdoby losowane, stałe dla działki."""
    P = b["poly"]; nazwa = b.get("nazwa") or ""
    pi = b.get("pietra"); zr = "OSM %s p." % pi if pi and pi.isdigit() else None
    if zr: kond = int(pi)
    else:
        ul = (b.get("ulica") or "")
        kond = 5 if ul.startswith(("Marszałka Focha", "Gdańska", "Jagiellońska")) else 4 if P.area > 180 else 3
        kond += R_.choice((-1, 0, 0, 0, 1)) if b.get("dzialka") else 0
        zr = "ZAŁ %d kond. wg zdjęć (%s)" % (kond, ul or "bez adresu")
    kond = max(2, min(kond, 7))
    hk = round(R_.uniform(3.6, 4.1), 2); p0 = round(R_.uniform(4.3, 4.9), 2)
    oh = round(min(hk - 1.35, R_.uniform(1.9, 2.25)), 2)
    okna = (round(R_.uniform(1.95, 2.4), 2), round(R_.uniform(1.05, 1.25), 2), oh, .85)
    tynk = R_.choice(KOLORY); duzy = P.area >= 5000                  # tylko hale/bloki płasko; duże kamienice i urząd — dach stromy
    oz = dict(trojkat=R_.random() < .5, plyciny=R_.random() < .6, lizeny=R_.random() < .45,
              wykusz=R_.random() < .22, balkon=R_.random() < .35)
    return dict(kond=kond, h_kond=hk, parter=p0, tynk=tynk, cokol="cokol4", h_cokol=.6, okna=okna,
                witryny=bool(b.get("ulica")) or bool(b.get("dzialka")), gzymsy=True, gzyms_mat="opaska",
                dach="plaski" if duzy else "kamienica", dach_mat=R_.choice(("dachowka", "dachowka", "dachowka_braz", "dachowka_braz", "dachowka_stara")),
                spadek=R_.choice((38, 42, 45, 48)), attyka=.9 if duzy else .35, kalenica_max=6.5 if P.area < 500 else 4.2, zr=zr,
                ozdoby=None if duzy else (dict(oz, wykusz=False) if P.area >= 1600 else oz), parter_mat=("kb" + tynk[1:]) if R_.random() < .7 else None,
                lukarny=R_.random() < .75, kominy=True)


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
    # krawężnik granitowy 15 cm między jezdnią (0) a chodnikiem/płytą (+0,12), wierzch +0,14, czoła do jezdni
    KRAW = JEZ.buffer(.15, join_style=2).difference(JEZ).intersection(unary_union([CHOD, PLAC]))
    CHOD = CHOD.difference(KRAW); PLAC = PLAC.difference(KRAW)
    # płyta Rynku (zdjęcie 02): jasne płyty z ukośnymi pasami ciemniejszych płyt co 9 m
    c_ = PLAC.centroid if not PLAC.is_empty else Point(0, 0); pasy = []
    for k in range(-40, 41):
        o_ = np.array([c_.x, c_.y]) + np.array([1, -1]) / math.sqrt(2) * k * 9.0; d_ = np.array([1, 1]) / math.sqrt(2) * 400
        pasy.append(LineString([tuple(o_ - d_), tuple(o_ + d_)]).buffer(.45, cap_style=2))
    PAS = PLAC.intersection(unary_union(pasy)); PLAC = PLAC.difference(PAS)
    plaski("asfalt", JEZ.simplify(.05), 0.0)
    plaski("rynek_plyty", PLAC.simplify(.05), .12)
    plaski("rynek_pas", PAS.simplify(.05), .12)
    plaski("kostka", CHOD.simplify(.05), .12)
    plaski("krawoznik", KRAW.simplify(.02), .14)
    plaski("nabrzeze", NABRZEZE.simplify(.05), .12)
    plaski("trawa", TRAWA.simplify(.1), .11)
    plaski("woda_brda", W.simplify(.1), -1.6)
    for q in getattr(KRAW.simplify(.02), "geoms", [KRAW]):                 # czoła krawężnika (0 → +0,14)
        if q.is_empty or q.geom_type != "Polygon": continue
        for ring in [q.exterior] + list(q.interiors):
            pts = list(ring.coords)
            for (x0, z0), (x1, z1) in zip(pts, pts[1:]):
                a = np.array([x0, 0., z0]); b_ = np.array([x1, 0., z1]); L = float(np.linalg.norm(b_ - a))
                if L < .03: continue
                t = (b_ - a) / L; n = nrm(np.cross(UP, t))
                if q.buffer(.001).contains(Point(*(((a + b_) / 2 + n * .05)[[0, 2]]))): n = -n
                if not JEZ.buffer(.02).contains(Point(*(((a + b_) / 2 + n * .05)[[0, 2]]))): continue   # tylko od jezdni
                S.quad("teren", "krawoznik", a, b_, b_ + UP * .14, a + UP * .14, kier=n)
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
                S.quad("teren", "mur_nabrzeza", a + UP * -1.7, b_ + UP * -1.7, b_ + UP * .12, a + UP * .12, kier=n)
                # krawędź z granitu 25 cm + żeliwna balustrada (zdjęcia m14, m34–m36), słupki co ok. 2,4 m
                S.box("teren", "mur_nabrzeza", a + UP * .12 - t * .01, t, UP, -n, L + .02, .25, .4)
                e = a - n * .2 + UP * .37
                S.quad("teren", "balustrada_kuta", e, e + t * L, e + t * L + UP * 1.0, e + UP * 1.0, uv_o=e, kier=n)
                for k_ in np.arange(0, L, 2.4):
                    S.box("teren", "latarnia_czarna", e + t * k_ - t * .04 - n * .04, t, UP, n, .08, 1.06, .08)
    return S, {"jezdnie": round(JEZ.area), "rynek": round(PLAC.area), "chodniki": round(CHOD.area), "trawa": round(TRAWA.area), "woda": round(W.area)}, mosty, (PLAC.union(PAS), CHOD, NABRZEZE)


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
            e = a_ - n * .12 + UP * .17                                       # żeliwna balustrada jak na nabrzeżach (m14, m34–m36)
            S.quad("mosty", "balustrada_kuta", e, e + t * L, e + t * L + UP * 1.0, e + UP * 1.0, uv_o=e, kier=n)
            S.box("mosty", "latarnia_czarna", e + UP * 1.0 - n * .04, t, UP, n, L, .05, .08)
            for k in np.arange(0, L, 2.0): S.box("mosty", "latarnia_czarna", e + t * k - t * .04 - n * .04, t, UP, n, .08, 1.05, .08)
    return n_


def latarnia(S, x, z, y=.12, obrot=0.0):
    """Latarnia stylowa jak na Rynku i nabrzeżach (zdjęcia 02, m14, m34): czarny słup z cokołem, poprzeczka na 4,0 m
    z dwiema latarniami (klosz w ramce, daszek)."""
    g = "latarnie"; p = np.array([x, y, z]); t = np.array([math.cos(obrot), 0., math.sin(obrot)]); n = nrm(np.cross(UP, t))
    S.rura(g, "latarnia_czarna", p, p + UP * .7, .13, seg=8)
    S.rura(g, "latarnia_czarna", p + UP * .7, p + UP * 4.15, .065, seg=8)
    S.box(g, "latarnia_czarna", p + UP * 3.95 - t * .7 - n * .03, t, UP, n, 1.4, .06, .06)
    for s_ in (-1, 1):
        c = p + t * s_ * .62 + UP * 3.5
        rog = lambda r, h: [c + UP * h + (t * i + n * j) * r for (i, j) in ((-1, -1), (1, -1), (1, 1), (-1, 1))]
        d0, d1, d2 = rog(.09, 0), rog(.16, .46), rog(.21, .52)            # klosz: wąski u dołu, szeroki u góry
        for k in range(4):
            a0, a1 = d0[k], d0[(k + 1) % 4]; b0, b1 = d1[k], d1[(k + 1) % 4]; kk = nrm((a0 + a1) / 2 - c - UP * (a0[1] - c[1]))
            S.quad(g, "klosz", a0, a1, b1, b0, kier=kk)
            S.box(g, "latarnia_czarna", a0 - (t + n) * .012, t, UP, n, .024, .48, .024)     # narożne pręty
        S.quad(g, "latarnia_czarna", *d0[::-1], kier=-UP)
        top = c + UP * .78
        for k in range(4):                                                  # daszek: ostrosłup z okapem
            a0, a1 = d2[k], d2[(k + 1) % 4]; kk = nrm((a0 + a1) / 2 - c - UP * .52) + UP
            S.tri_list(g, "latarnia_czarna", [a0, a1, top] if np.dot(np.cross(a1 - a0, top - a0), kk) > 0 else [a0, top, a1], nrm(kk), nrm(a1 - a0), [(0, 0), (.4, 0), (.2, .3)], None, [0, 1, 2])
        S.quad(g, "latarnia_czarna", *d2[::-1], kier=-UP)
        S.box(g, "latarnia_czarna", c - t * .02 - n * .02 + UP * .52, t, UP, n, .04, .45, .04)   # wieszak do poprzeczki


def lawka(S, x, z, kier, y=.12):
    """Ławka parkowa: żeliwne boki, siedzisko i oparcie z desek."""
    g = "lawki"; p = np.array([x, y, z]); t = np.array([math.cos(kier), 0., math.sin(kier)]); n = nrm(np.cross(UP, t))
    for s_ in (-.8, .74):
        S.box(g, "latarnia_czarna", p + t * s_ - n * .25, t, UP, n, .06, .45, .5)
        S.box(g, "latarnia_czarna", p + t * s_ - n * .3 + UP * .45, t, UP, n, .06, .45, .06)
    S.box(g, "lawka_drewno", p - t * .85 - n * .25 + UP * .45, t, UP, n, 1.7, .05, .48)
    S.box(g, "lawka_drewno", p - t * .85 - n * .32 + UP * .55, t, UP, -n, 1.7, .38, .04)


def ogrodek(S, a, t, n, L, R_):
    """Ogródek kawiarni przed kamienicą (zdjęcia 02–03): pas 1,6–6,2 m od fasady, parasole z płótna (część złożonych),
    stoliki z krzesłami, niski płotek z żeliwnej balustrady i drewniane donice z krzewami na rogach."""
    g = "ogrodki"; d0, d1 = 1.6, 6.2; y = .12
    P = lambda u, d: a + t * u + n * d + UP * y
    m = max(1, int((L - 1.0) // 3.6))
    for i in range(m):
        u = (L - (m - 1) * 3.6) / 2 + i * 3.6; c = P(u, (d0 + d1) / 2)
        otw = R_.random() < .55; h = 2.55 if otw else 2.9
        S.rura(g, "meble_ogrodka", c, c + UP * h, .025, seg=6)
        if otw:                                                    # czasza 8-kątna, 3 × 3 m, spadek 0,45 m
            r = 1.55
            for k in range(8):
                a0 = k * math.pi / 4; a1 = (k + 1) * math.pi / 4
                p0 = c + UP * (h - .45) + np.array([math.cos(a0) * r, 0, math.sin(a0) * r])
                p1 = c + UP * (h - .45) + np.array([math.cos(a1) * r, 0, math.sin(a1) * r])
                tp = c + UP * h
                for kk in (UP, -UP):
                    q = [p0, p1, tp] if np.dot(np.cross(p1 - p0, tp - p0), kk) > 0 else [p0, tp, p1]
                    S.tri_list(g, "parasol", q, kk, nrm(p1 - p0), [(0, 0), (1.2, 0), (.6, 1.6)], None, [0, 1, 2])
        else:                                                      # złożony: wydłużony stożek wokół masztu
            for k in range(6):
                a0 = k * math.pi / 3; a1 = (k + 1) * math.pi / 3
                p0 = c + UP * 1.1 + np.array([math.cos(a0) * .14, 0, math.sin(a0) * .14])
                p1 = c + UP * 1.1 + np.array([math.cos(a1) * .14, 0, math.sin(a1) * .14])
                tp = c + UP * (h - .1); kk = nrm(np.array([math.cos((a0 + a1) / 2), 0, math.sin((a0 + a1) / 2)]))
                q = [p0, p1, tp] if np.dot(np.cross(p1 - p0, tp - p0), kk) > 0 else [p0, tp, p1]
                S.tri_list(g, "parasol", q, kk, nrm(p1 - p0), [(0, 0), (.3, 0), (.15, 1.6)], None, [0, 1, 2])
        for s_ in (-1, 1):                                         # stoliki z krzesłami po obu stronach masztu
            st = c + t * s_ * 1.0
            S.rura(g, "meble_ogrodka", st, st + UP * .72, .03, seg=4)
            S.box(g, "meble_ogrodka", st - t * .35 - n * .35 + UP * .72, t, UP, n, .7, .03, .7)
            for kk in (-1, 1):
                ch = st + n * kk * .62
                S.box(g, "meble_ogrodka", ch - t * .2 - n * .2 + UP * .44, t, UP, n, .4, .04, .4)
                S.box(g, "meble_ogrodka", ch - t * .2 + n * kk * .2 - n * .02 + UP * .48, t, UP, n, .4, .42, .04)
                for (du, dd) in ((-.18, -.18), (.16, -.18), (-.18, .16), (.16, .16)):
                    S.box(g, "meble_ogrodka", ch + t * du + n * dd, t, UP, n, .03, .44, .03)
    e = P(.3, d1)
    S.quad(g, "balustrada_kuta", e, e + t * (L - .6), e + t * (L - .6) + UP * .8, e + UP * .8, uv_o=e, kier=n)
    for u in (.3, L - .3):                                         # donice na rogach
        for dd in (d0 + .1, d1 - .7):
            q = P(u, dd) - t * .3
            S.box(g, "donica", q, t, UP, n, .6, .55, .6)
            for kk in range(2):                                    # krzew: dwie skrzyżowane karty
                w_ = nrm(t + n) if kk == 0 else nrm(t - n); c_ = q + t * .3 + n * .3 + UP * .55
                S.quad(g, "zywoplot_galazki", c_ - w_ * .45, c_ + w_ * .45, c_ + w_ * .45 + UP * .8, c_ - w_ * .45 + UP * .8, uv_o=c_ - w_ * .45)


def detale(d, bud_lista, PLAC_, CHOD_, NABRZ_, W, R_):
    """Latarnie (Rynek, ulice, nabrzeża), ławki (OSM amenity=bench), ogródki kawiarni na płycie Rynku przed kamienicami,
    drzewa z OSM (natural=tree) do listy punktów (w grze BraxBydgoszcz: te same drzewa co osiedle)."""
    S = Siatka(); PLAC, CHOD, NABRZ = PLAC_, CHOD_, NABRZ_
    lat = []
    # Rynek: latarnie wzdłuż ogródków (6,8 m od fasad), co ok. 16 m
    for b in bud_lista:
        if b.get("nazwa"): continue                                    # urząd, kościół — bez ogródków
        P = shapely.geometry.polygon.orient(b["poly"].simplify(.15), 1.0); pts = list(P.exterior.coords)
        for (x0, z0), (x1, z1) in zip(pts, pts[1:]):
            a = np.array([x0, 0., z0]); c = np.array([x1, 0., z1]); L = float(np.linalg.norm(c - a))
            if L < 5.5: continue
            t = (c - a) / L; n = nrm(np.cross(UP, t))
            if P.contains(Point(*(((a + c) / 2 + n * .3)[[0, 2]]))): n = -n
            pr = [Point(*((a + t * u + n * dd)[[0, 2]])) for u in (.5, L / 2, L - .5) for dd in (1.0, 6.5)]
            if all(PLAC.contains(q) for q in pr):
                ogrodek(S, a, t, n, L, R_)
                for u in np.arange(L / 2 % 16, L, 16): lat.append((a + t * u + n * 7.4, math.atan2(t[2], t[0])))
    # ulice: co ok. 24 m na chodnikach (OSM highway z nazwą), naprzemiennie po stronach
    szer = {"primary": 14, "secondary": 12, "tertiary": 10, "residential": 7, "living_street": 6, "pedestrian": 6}
    for u in d["ulice"]:
        if u["klasa"] not in szer or len(u["linia"]) < 2 or u.get("most"): continue
        ln = LineString(u["linia"]).intersection(OKNO)
        if ln.is_empty or ln.geom_type != "LineString": continue
        for i, s_ in enumerate(np.arange(8, ln.length, 24)):
            p = np.array(ln.interpolate(s_).coords[0]); p2 = np.array(ln.interpolate(min(ln.length, s_ + .5)).coords[0])
            tt = nrm(np.array([p2[0] - p[0], 0., p2[1] - p[1]])); nn = nrm(np.cross(UP, tt)) * (1 if i % 2 else -1)
            q = np.array([p[0], 0., p[1]]) + nn * (szer[u["klasa"]] / 2 + 1.1)
            if CHOD.contains(Point(q[0], q[2])) or PLAC.contains(Point(q[0], q[2])): lat.append((q, math.atan2(tt[2], tt[0])))
    # nabrzeża: co 20 m po stronie lądu
    for q in getattr(W, "geoms", [W]):
        ring = q.exterior
        for s_ in np.arange(5, ring.length, 20):
            p = np.array(ring.interpolate(s_).coords[0]); p2 = np.array(ring.interpolate(s_ + .5).coords[0])
            tt = nrm(np.array([p2[0] - p[0], 0., p2[1] - p[1]])); nn = nrm(np.cross(UP, tt))
            q3 = np.array([p[0], 0., p[1]]) + nn * 1.4
            if not NABRZ.contains(Point(q3[0], q3[2])): q3 = np.array([p[0], 0., p[1]]) - nn * 1.4
            if NABRZ.contains(Point(q3[0], q3[2])): lat.append((q3, math.atan2(tt[2], tt[0])))
    zaj = []
    for (q, ob) in lat:                                            # bez dwóch latarni bliżej niż 8 m
        if any(np.hypot(q[0] - z_[0], q[2] - z_[2]) < 8 for z_ in zaj): continue
        zaj.append(q); latarnia(S, q[0], q[2], .12, ob)
    lawki = 0
    for pk in d["punkty"]:
        tg = pk["tagi"]
        if tg.get("amenity") == "bench" and OKNO.contains(Point(*pk["xz"])):
            lawka(S, pk["xz"][0], pk["xz"][1], float(tg.get("direction", "0") or 0) * math.pi / 180 if str(tg.get("direction", "0")).replace(".", "").isdigit() else 0.0); lawki += 1
    drzewa = []
    for pk in d["punkty"]:
        tg = pk["tagi"]
        if tg.get("natural") == "tree" and OKNO.contains(Point(*pk["xz"])):
            x, z = pk["xz"]; blisko_wody = W.distance(Point(x, z)) < 12
            if PLAC.contains(Point(x, z)) or CHOD.contains(Point(x, z)):         # drzewo w płycie: granitowe obrzeże i kora
                o_ = np.array([x - .9, .12, z - .9]); X, Z = np.array([1., 0, 0]), np.array([0, 0, 1.])
                for (oo, ax, az, sx, sz) in ((o_, X, Z, 1.8, .14), (o_ + Z * 1.66, X, Z, 1.8, .14), (o_ + Z * .14, X, Z, .14, 1.52), (o_ + X * 1.66 + Z * .14, X, Z, .14, 1.52)):
                    S.box("detale", "krawoznik", oo, ax, UP, az, sx, .1, sz)
                S.quad("detale", "zwir", o_ + X * .14 + Z * .14 + UP * .02, o_ + X * 1.66 + Z * .14 + UP * .02, o_ + X * 1.66 + Z * 1.66 + UP * .02, o_ + X * .14 + Z * 1.66 + UP * .02, kier=UP)
            drzewa.append([round(x, 2), round(z, 2), "duze" if blisko_wody or "Salix" in (tg.get("genus", "") + tg.get("species", "")) else "srednie"])
    return S, {"latarnie": len(zaj), "lawki": lawki, "drzewa": len(drzewa)}, {"drzewa": drzewa, "latarnie": [[round(q[0], 2), round(q[2], 2), "stylowa"] for q in zaj]}


def main(out):
    out = Path(out); out.mkdir(parents=True, exist_ok=True)
    d = wczytaj(); R_ = random.Random(11)
    W = woda(d)
    D = Siatka(); opis = {}
    budynki = [b for b in d["budynki"] if not b["poly"].is_empty and b["poly"].area > 25 and OKNO.contains(b["poly"].centroid) and not b.get("czesc")]
    PUBL0 = przestrzen_publiczna(d, W)
    budynki = dzialki(budynki, PUBL0, R_)
    from shapely.strtree import STRtree
    from shapely.prepared import prep
    drzewo = STRtree([b["poly"] for b in budynki])
    PUBL = prep(PUBL0)
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
    T, pola, mosty, (PLAC_, CHOD_, NABRZ_) = teren(d, bud, W)
    Dt, pola_d, punkty = detale(d, budynki, PLAC_, CHOD_, NABRZ_, W, random.Random(5))
    pola.update(pola_d)
    for (g, m), c in Dt.dane().items():
        dd = D._c(g, m); b0 = len(dd["v"])
        for f in ("v", "n", "t", "uv", "uv1"): dd[f] += c[f]
        dd["i"] += [b0 + q for q in c["i"]]
    (out / "rynek_punkty.json").write_text(json.dumps(punkty, ensure_ascii=False), encoding="utf-8")
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
