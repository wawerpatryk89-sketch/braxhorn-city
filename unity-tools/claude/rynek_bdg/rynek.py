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
# okno: Rynek, Mostowa z mostem, spichrze, Opera, Focha (metry od kotwicy; Rynek ~ (−180…−60, −230…−150)). 24.11 (właściciel:
# „najpierw Stary Rynek” — podpięty do gry): okno sięga na południe do szwu z osiedlem (z 897 j. = północna krawędź okna
# osiedla, osiedle_bdg/teren.OKNO), x −420 m = granica miasta X0 −872 j. (BraxMiastoGranice).
SZEW_Z = 897.0                                    # jednostki gry: osiedle kończy się na z 508,8 + 300 m × K
Y_SZEW = (SZEW_Z - O[1]) / K                      # ok. −376,6 m
OKNO = box(-420, Y_SZEW, 160, 160)
Y_LACZNIK = -344.0                                # oś ulicy łączącej wzdłuż szwu (północ budynków osiedla wystających za szew)
SKRZYZOWANIE = (-30.0, -15.0)          # Focha × Gdańska × Jagiellońska (zdjęcia IMG_1983–1992): pasy, tory, słupki z łańcuchami
CEGLY = ["c_czerwona", "c_sredniowieczna", "c_ceglasta", "c_mieszana", "c_stara", "c_bielona"]
KAMIENIE = ["s_piaskowiec", "s_szary", "s_zamkowy", "s_grafit", "s_ochra", "s_rozowy"]
DACHY = ["dachowka", "dachowka_braz", "dachowka_stara", "dachowka_03", "dachowka_ceram", "dachowka_karpiowka", "lupek"]
KOLORY = ["k_zolty", "k_krem", "k_szaroniebieski", "k_rozowy", "k_bialy", "k_ochra", "k_zielony", "k_terakota",
          "k_kremowy2", "k_jasnozolty", "k_losos", "k_niebieskoszary", "k_kremowy2", "k_jasnozolty", "k_losos"]
# 24.11 (zdjęcia 01–03): pierzeje samego Rynku są tylko tynkowane, w pastelach — żółty i ochra, jasnoniebieski,
# niebieskoszary, krem, róż, biel; cegła i kamień zostają w uliczkach dookoła
# udziały policzone na zdjęciach 01–03 (17 frontów): żółte ok. 35%, niebieskie/szare 25%, krem 20%, białe 15%, róż 5%
KOLORY_RYNEK = ["k_zolty", "k_zolty", "k_jasnozolty", "k_jasnozolty", "k_ochra", "k_krem", "k_kremowy2", "k_bialy", "k_bialy",
                "k_szaroniebieski", "k_szaroniebieski", "k_niebieskoszary", "k_rozowy"]
RYNEK_PIERZEJE = [None]                    # obszar placu Rynku (ustawiany w main) — budynki do 3 m od niego to pierzeje


def wczytaj():
    d = json.loads((TU / "dane" / "rynek_osm.json").read_text(encoding="utf-8"))
    for b in d["budynki"]:
        try: b["poly"] = Polygon(b["obrys"]).buffer(0)
        except Exception: b["poly"] = Polygon()
    return d


def osiedle_przy_szwie():
    """Szew z osiedlem (24.11): budynki osiedla, które wystają za jego okno na północ (szpital: Patomorfologia do z 929 j.,
    Budynek C, Budynek D), w metrach Rynku — tu Rynek nie stawia nic (ani budynku, ani trawy pod nimi); końce dróg osiedla
    na szwie (x w metrach Rynku) — do nich dochodzą łączniki z ulicy wzdłuż szwu."""
    import shapely.affinity
    import osiedle as OS
    import teren as TO
    d = OS.wczytaj(); OS.WCZYTANE.update(d)
    zo = np.asarray(OS.O, float)
    dx, dz = (zo - O) / K                                       # metry osiedla → metry Rynku (ta sama skala K)
    bud = []
    for b in d["budynki"]:
        P = b["poly"]
        if P.is_empty or P.area < 40 or not TO.OKNO.contains(P.centroid) or OS.w_lesie(P.centroid): continue
        Q = shapely.affinity.translate(P, dx, dz)
        if Q.bounds[3] > Y_SZEW: bud.append(Q)
    konce = []
    y_os = Y_SZEW - dz                                          # szew w metrach osiedla (300 m)
    for u in d["ulice"]:
        if u["klasa"] not in ("service", "residential", "unclassified", "tertiary") or len(u["linia"]) < 2: continue
        I = LineString(u["linia"]).intersection(box(-1e4, y_os - 1.0, 1e4, y_os + .5))
        if I.is_empty: continue
        x = I.centroid.x + dx
        if -415 < x < 155 and not any(Q.buffer(4.0).intersects(LineString([(x, Y_SZEW), (x, Y_LACZNIK)])) for Q in bud): konce.append(round(x, 2))
    return bud, sorted(set(konce))


def lacznik(d, konce):
    """Ulica wzdłuż szwu z osiedlem (bez nazwy, asfalt, chodniki — jak reszta ulic) od zachodniej do wschodniej krawędzi
    okna: łączy ucięte na szwie ulice Starego Miasta (Jezuicka, Długa, Batorego, Magdzińskiego, Przy Zamczysku…)
    i łącznikami (droga dojazdowa 5 m) końce dróg osiedla. Wpisy jak z OSM — teren, chodniki i latarnie robi reszta kodu."""
    d["ulice"].append({"osm": "lacznik", "klasa": "residential", "nazwa": None, "most": None, "warstwa": None, "nawierzchnia": "asphalt",
                       "pasy": None, "jednokier": None, "obszar": None, "tory": None, "linia": [[-420.0, Y_LACZNIK], [160.0, Y_LACZNIK]]})
    for k, x in enumerate(konce):
        d["ulice"].append({"osm": "lacznik_%d" % k, "klasa": "service", "nazwa": None, "most": None, "warstwa": None, "nawierzchnia": "asphalt",
                           "pasy": None, "jednokier": None, "obszar": None, "tory": None, "linia": [[x, Y_SZEW - 1.0], [x, Y_LACZNIK]]})


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
    # 21:56–21:59 właściciel: część budynków ze starej cegły (różnej), gdzieniegdzie elewacja kamienna, reszta tynk z fakturą
    los = R_.random(); rodzaj = "cegla" if los < .32 else "kamien" if los < .44 else "tynk"
    tynk = R_.choice(CEGLY if rodzaj == "cegla" else KAMIENIE if rodzaj == "kamien" else KOLORY)
    pastel = None
    if RYNEK_PIERZEJE[0] is not None and P.distance(RYNEK_PIERZEJE[0]) < 3.0:      # pierzeja Rynku: tylko pastelowy tynk
        R_r = random.Random(round(P.centroid.x * 10) * 7919 + round(P.centroid.y * 10))   # osobne losowanie — reszta bez zmian
        pastel = (R_r.choice(KOLORY_RYNEK), R_r.random() < .7)
    duzy = P.area >= 5000                                            # tylko hale/bloki płasko; duże kamienice i urząd — dach stromy
    oz = dict(trojkat=R_.random() < .5, plyciny=R_.random() < .6, lizeny=R_.random() < .45,
              wykusz=R_.random() < .22, balkon=R_.random() < .35)
    nz = nazwa.lower()
    if "spichlerz" in nz and "grodzkiej 11" in nz:
        # Grodzka 11 (24.11, IMG_2010 + Wikipedia „Grodzka Street, Bydgoszcz”: „has only three storey”): trzy kondygnacje
        # muru pruskiego (ok. 9 m do okapu), szczyt od ulicy w murze pruskim, dach dwuspadowy 58°, jeden rząd małych lukarn
        return dict(kond=3, h_kond=2.9, parter=3.2, tynk="k_wypelnienie", cokol="cokol4", h_cokol=.4,
                    okna=(2.3, .75, 1.0, 1.1), witryny=False, gzymsy=False, dach="kamienica", dach_mat="dachowka_03", spadek=58,
                    attyka=.15, kalenica_max=40.0, zr="ZAŁ spichlerz Grodzka 11 wg IMG_2010 (3 kondygnacje)", ozdoby=None,
                    parter_mat=None, lukarny=True, lukarny_rzedy=1, kominy=False, mur_pruski=True, dwuspad=True, szczyt_mat=None,
                    okno_lekkie=True, rama_mat="deska_ciemna", osc_mat="deska_ciemna")   # IMG_2010: okna w ciemnym drewnie
    if "spichlerz" in nz:                                             # spichrze nad Brdą: mur pruski, dach dwuspadowy 58°
        # IMG_2009 (Grodzka 8): niskie ściany podłużne (ok. 3,3 m) w murze pruskim, ogromny stromy dach z rzędami lukarn,
        # wysokie szczyty w murze pruskim
        return dict(kond=1, h_kond=3.3, parter=3.3, tynk="k_wypelnienie", cokol="cokol4", h_cokol=.4,
                    okna=(1.9, .7, .95, 1.2), witryny=False, gzymsy=False, dach="kamienica", dach_mat="dachowka_03", spadek=62,
                    attyka=.15, kalenica_max=40.0, zr="ZAŁ spichlerz wg zdjęć m22–m26, m33–m35", ozdoby=None, parter_mat=None,
                    lukarny=True, lukarny_rzedy=3, kominy=False, mur_pruski=True, dwuspad=True, szczyt_mat=None)
    if "katedra" in nz or "kościół" in nz:
        return dict(kond=1, h_kond=12.0, parter=12.0, tynk="c_sredniowieczna", cokol="cokol4", h_cokol=.6, okna=(4.2, 1.5, 6.0, 2.6),
                    witryny=False, gzymsy=False, dach="kamienica", dach_mat="dachowka_03", spadek=55, attyka=.2, kalenica_max=40.0,
                    zr="ZAŁ kościół gotycki z cegły", ozdoby=None, parter_mat=None, lukarny=False, kominy=False, szczyt_mat="c_sredniowieczna")
    if "poczta" in nz:
        tynk, rodzaj = "c_czerwona", "cegla"
    elif "urząd miasta" in nz or "opera" in nz or "lloyd" in nz:
        tynk, rodzaj = "k_bialy", "tynk"
    sp_ = dict(kond=kond, h_kond=hk, parter=p0, tynk=tynk, cokol="cokol4", h_cokol=.6, okna=okna,
                witryny=bool(b.get("ulica")) or bool(b.get("dzialka")), gzymsy=True, gzyms_mat="opaska",
                dach="plaski" if duzy else "kamienica", dach_mat=R_.choice(DACHY),
                spadek=R_.choice((38, 42, 45, 48)), attyka=.9 if duzy else .35, kalenica_max=6.5 if P.area < 500 else 4.2, zr=zr,
                ozdoby=None if duzy else (dict(oz, wykusz=False) if P.area >= 1600 else oz),
                parter_mat=("kb" + tynk[1:]) if rodzaj == "tynk" and R_.random() < .7 else (R_.choice(("s_piaskowiec", "s_szary", "s_zamkowy")) if rodzaj == "cegla" and R_.random() < .5 else None),
                lukarny=R_.random() < .75, kominy=True,
                szczyt_mat=tynk if rodzaj != "tynk" else R_.choice(("c_stara", "c_sredniowieczna", "c_czerwona", "c_mieszana", tynk)))
    if pastel and not ("urząd miasta" in nz or "opera" in nz or "lloyd" in nz or "spichlerz" in nz):
        sp_["tynk"] = pastel[0]; sp_["parter_mat"] = ("kb" + pastel[0][1:]) if pastel[1] else None
        if sp_["szczyt_mat"] == tynk: sp_["szczyt_mat"] = pastel[0]
    return sp_


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
    piesze += d.get("fartuchy", [])                       # utwardzenia wokół zabytków (Opera Nova: płyty dookoła kręgów, m44)
    JEZ = unary_union(jezd).intersection(OKNO).difference(W)
    PLAC = unary_union(place).intersection(OKNO).difference(W).difference(bud)
    CHOD = unary_union(piesze + [bud.buffer(2.2, join_style=2), JEZ.buffer(2.6, join_style=2)]).intersection(OKNO).difference(JEZ).difference(W).difference(bud).difference(PLAC)
    NABRZEZE = W.buffer(3.0, join_style=2).difference(W).intersection(OKNO).difference(bud)
    TRAWA = OKNO.difference(unary_union([JEZ, PLAC, CHOD, W, bud, NABRZEZE]))
    # w śródmieściu wąskie i małe skrawki zieleni między chodnikiem a jezdnią to w rzeczywistości chodnik (zdjęcia Focha, Gdańska)
    skrawki = [q for q in getattr(TRAWA, "geoms", [TRAWA]) if q.geom_type == "Polygon" and (q.area < 150 or q.area / max(q.length, 1e-6) < 2.0)]
    if skrawki:
        S_ = unary_union(skrawki); TRAWA = TRAWA.difference(S_); CHOD = CHOD.union(S_)

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
    # płyta Rynku (24.11, zdjęcia 01–04): duże jasne płyty granitowe 0,9 m na drobną fugę, gdzieniegdzie prostokąty
    # z ciemniejszych płyt (2–5 × 1–3 płyty) — bez ceglanych pasów. Prostokąty po fugach tekstury granite_tile_04
    # (kafel 4,5 m = 5 płyt; fugi w x co 0,9 m od 0,43, w z co 0,9 m od 0)
    R_p = random.Random(23); pasy = []
    if not PLAC.is_empty:
        bx0, bz0, bx1, bz1 = PLAC.bounds
        for i in range(int((bx1 - bx0) // .9) + 2):
            for j in range(int((bz1 - bz0) // .9) + 2):
                if R_p.random() > .012: continue
                x0_ = .43 + .9 * (math.floor((bx0 - .43) / .9) + i); z0_ = .9 * (math.floor(bz0 / .9) + j)
                pasy.append(box(x0_, z0_, x0_ + .9 * R_p.randint(2, 5), z0_ + .9 * R_p.randint(1, 3)))
    PAS = PLAC.intersection(unary_union(pasy)) if pasy else Polygon(); PLAC = PLAC.difference(PAS)
    GLOWNE = unary_union([LineString(u["linia"]).buffer(szer[u["klasa"]] / 2 + .3, cap_style=2, join_style=1) for u in d["ulice"]
                          if len(u["linia"]) > 1 and u["klasa"] in ("primary", "secondary", "tertiary")])
    BRUK = unary_union([LineString(u["linia"]).buffer(szer[u["klasa"]] / 2, cap_style=1, join_style=1) for u in d["ulice"]
                        if len(u["linia"]) > 1 and u["klasa"] in ("residential", "living_street", "unclassified", "pedestrian") and not u.get("obszar")
                        and (u.get("nazwa") or "")]).intersection(JEZ).difference(GLOWNE)     # bez bruku na głównych (22:44)
    ZEB = []
    J = Point(*SKRZYZOWANIE)
    for u in d["ulice"]:
        if u["klasa"] not in ("primary", "secondary", "tertiary") or len(u["linia"]) < 2: continue
        ln = LineString(u["linia"])
        if ln.distance(J) > 20: continue
        for odl in (14.0,):
            s0 = ln.project(J)
            for s_ in (s0 - odl, s0 + odl):
                if s_ < 0 or s_ > ln.length: continue
                p = np.array(ln.interpolate(s_).coords[0]); p2 = np.array(ln.interpolate(min(ln.length, s_ + .5)).coords[0]) if s_ + .5 <= ln.length else p + (p - np.array(ln.interpolate(s_ - .5).coords[0]))
                tt = (p2 - p) / max(np.linalg.norm(p2 - p), 1e-6); nn = np.array([-tt[1], tt[0]])
                for k in range(-12, 13):                           # pasy 0,5 m co 1,0 m w poprzek jezdni, długość 4 m
                    c = p + nn * k * 1.0
                    ZEB.append(Polygon([tuple(c - nn * .25 - tt * 2), tuple(c + nn * .25 - tt * 2), tuple(c + nn * .25 + tt * 2), tuple(c - nn * .25 + tt * 2)]))
    ZEBRA = unary_union(ZEB).intersection(JEZ) if ZEB else Polygon()
    JEZ_PELNA = JEZ                                   # cała jezdnia (z zebrami i przejazdami) — do czół krawężnika
    JEZ = JEZ.difference(ZEBRA)
    # 24.11 (zdjęcia 11 i 13, Focha): drogi rowerowe z czerwonego asfaltu — na chodniku i przejazdy przez jezdnię
    # (OSM highway=cycleway, szer. 2 m); wycięte z asfaltu, zebry i kostki (bez nakładania płaszczyzn)
    ROW = unary_union([LineString(u["linia"]).buffer(1.0, cap_style=2, join_style=1) for u in d["ulice"]
                       if u["klasa"] == "cycleway" and len(u["linia"]) > 1 and not u.get("most")]).intersection(OKNO).difference(W) \
        if any(u["klasa"] == "cycleway" for u in d["ulice"]) else Polygon()
    ROW_J = ROW.intersection(JEZ_PELNA); ROW_C = ROW.intersection(CHOD)
    ZEBRA = ZEBRA.difference(ROW); JEZ = JEZ.difference(ROW_J); CHOD = CHOD.difference(ROW_C); BRUK = BRUK.difference(ROW_J)
    plaski("droga_rowerowa", ROW_J.simplify(.05), 0.0)
    plaski("droga_rowerowa", ROW_C.simplify(.05), .12)
    plaski("linia_biala", ZEBRA.simplify(.01), 0.0)
    plaski("asfalt", JEZ.difference(BRUK).simplify(.05), 0.0)
    plaski("bruk", BRUK.simplify(.05), 0.0)
    plaski("rynek_plyty", PLAC.simplify(.05), .12)
    plaski("rynek_pas", PAS.simplify(.05), .12)
    plaski("kostka", CHOD.simplify(.05), .12)
    plaski("krawoznik", KRAW.simplify(.02), .14)
    plaski("nabrzeze", NABRZEZE.simplify(.05), .12)
    plaski("trawa", TRAWA.simplify(.1), .11)
    TRAWNIK[0] = TRAWA
    plaski("woda_brda", W.simplify(.1), POZIOM_WODY)
    for q in getattr(KRAW.simplify(.02), "geoms", [KRAW]):                 # czoła krawężnika (0 → +0,14)
        if q.is_empty or q.geom_type != "Polygon": continue
        for ring in [q.exterior] + list(q.interiors):
            pts = list(ring.coords)
            for (x0, z0), (x1, z1) in zip(pts, pts[1:]):
                a = np.array([x0, 0., z0]); b_ = np.array([x1, 0., z1]); L = float(np.linalg.norm(b_ - a))
                if L < .03: continue
                t = (b_ - a) / L; n = nrm(np.cross(UP, t))
                if q.buffer(.001).contains(Point(*(((a + b_) / 2 + n * .05)[[0, 2]]))): n = -n
                if not JEZ_PELNA.buffer(.02).contains(Point(*(((a + b_) / 2 + n * .05)[[0, 2]]))): continue   # tylko od jezdni (też przy zebrze i przejeździe)
                S.quad("teren", "krawoznik", a, b_, b_ + UP * .14, a + UP * .14, kier=n)
    # mur nabrzeża: od wody (−1,6) do bulwaru (+0,15), granitowy, po obwodzie wody (bez odcinków na krawędzi okna).
    # 24.11, 11:24 (właściciel, zdjęcie z Mostowej: „zabarykadowana droga?”): pod mostami i przy ich przyczółkach bez
    # granitowej krawędzi, balustrady i pochwytu — inaczej przegradzały jezdnię mostu w poprzek (a pochwyt z kolizją w grze)
    POD_MOSTEM = unary_union([ln.buffer(w / 2 + 1.0 + 1.2, cap_style=2, join_style=2) for (ln, w, u) in mosty]) if mosty else Polygon()
    for q in getattr(W, "geoms", [W]):
        if q.is_empty: continue
        for ring in [q.exterior] + list(q.interiors):
            pts = list(ring.coords)
            for (x0, z0), (x1, z1) in zip(pts, pts[1:]):
                a = np.array([x0, 0., z0]); b_ = np.array([x1, 0., z1]); L = float(np.linalg.norm(b_ - a))
                if L < .05 or not OKNO.buffer(-.5).contains(Point((x0 + x1) / 2, (z0 + z1) / 2)): continue
                t = (b_ - a) / L; n = nrm(np.cross(UP, t)); m = (a + b_) / 2 + n * .3
                if not W.contains(Point(m[0], m[2])): n = -n                       # ściana patrzy na wodę
                S.quad("teren", "mur_nabrzeza", a + UP * (POZIOM_WODY - .1), b_ + UP * (POZIOM_WODY - .1), b_ + UP * .12, a + UP * .12, kier=n)
                reszta = LineString([(x0, z0), (x1, z1)]).difference(POD_MOSTEM)       # odcinki poza mostami
                for cz in getattr(reszta, "geoms", [reszta]):
                    if cz.is_empty or cz.geom_type != "LineString" or cz.length < .3: continue
                    (u0, w0), (u1, w1) = cz.coords[0], cz.coords[-1]
                    a = np.array([u0, 0., w0]); Lc = float(math.hypot(u1 - u0, w1 - w0))
                    # krawędź z granitu 25 cm + żeliwna balustrada (zdjęcia m14, m34–m36), słupki co ok. 2,4 m
                    S.box("teren", "mur_nabrzeza", a + UP * .12 - t * .01, t, UP, -n, Lc + .02, .25, .4)
                    e = a - n * .2 + UP * .37
                    S.quad("teren", "balustrada_kuta", e, e + t * Lc, e + t * Lc + UP * 1.0, e + UP * 1.0, uv_o=e, kier=n)
                    # pochwyt (24.11, gra: kolizja wzdłuż nabrzeża — postać nie wpada do Brdy między słupkami)
                    S.box("teren", "latarnia_czarna", e + UP * 1.0 - n * .03 - t * .01, t, UP, n, Lc + .02, .05, .06)
                    for k_ in np.arange(0, Lc, 2.4):
                        S.box("teren", "latarnia_czarna", e + t * k_ - t * .04 - n * .04, t, UP, n, .08, 1.06, .08)
    return S, {"jezdnie": round(JEZ.area), "rynek": round(PLAC.area), "chodniki": round(CHOD.area), "trawa": round(TRAWA.area), "woda": round(W.area)}, mosty, (PLAC.union(PAS), CHOD, NABRZEZE)


POZIOM_WODY = -2.4                    # lustro Brdy 2,5 m pod bulwarem (zdjęcia m15–m16, m28: wysokie nabrzeża, prześwit pod mostem)
TRAWNIK = [None]                      # pole trawy z teren() — maska kęp trawy 3D w grze


def zapisz_maske_trawy(trawa, drzewa, plik, y=.11, krok=.32):
    """Maska trawnika Rynku dla trawy 3D w grze (BraxClaudeTrawaMaska, ten sam format co osiedle_bdg/osiedle.py:
    'BTM1', float32 x0 z0 krok y, int32 szer wys, bity od z0, bit = x % 8), bez pierścieni ziemi pod drzewami (0,8 m)."""
    import gzip, struct
    from PIL import Image, ImageDraw
    x0m, z0m, x1m, z1m = trawa.bounds
    gx0, gz0 = O[0] + x0m * K, O[1] + z0m * K
    W_ = int(math.ceil((x1m - x0m) * K / krok)) + 1; H = int(math.ceil((z1m - z0m) * K / krok)) + 1
    img = Image.new("1", (W_, H), 0); dr = ImageDraw.Draw(img)
    px = lambda x, z: ((O[0] + x * K - gx0) / krok, (O[1] + z * K - gz0) / krok)
    for g in getattr(trawa, "geoms", [trawa]):
        if g.is_empty or g.geom_type != "Polygon": continue
        dr.polygon([px(x, z) for x, z in g.exterior.coords], fill=1)
        for r in g.interiors: dr.polygon([px(x, z) for x, z in r.coords], fill=0)
    for (x, z, _) in drzewa:
        cx, cz = px(x, z); rr = .8 * K / krok; dr.ellipse((cx - rr, cz - rr, cx + rr, cz + rr), fill=0)
    bity = np.packbits(np.asarray(img, dtype=np.uint8), axis=1, bitorder="little")
    with gzip.open(plik, "wb") as f:
        f.write(b"BTM1" + struct.pack("<4f2i", gx0, gz0, krok, y * K, bity.shape[1] * 8, H)); f.write(bity.tobytes())
    return W_, H


def mosty_scal(S, mosty, W, d=None):
    """Mosty nad Brdą (zdjęcia m14, m28–m38). Wszystkie drogi bridge=* zespołu (Mostowa + chodniki jako osobne drogi OSM)
    scalone w jeden pomost — bez nakładających się płyt. Wierzch +0,17; jezdnia mostu Staromiejskiego (Mostowa) z kostki
    granitowej, chodniki z płyt; czoło: jasna żelbetowa belka z łagodnym łukiem spodu (1,3 m przy przyczółkach, 0,8 m
    w środku, m28); żeliwna balustrada ze wzorem trójkątów i kół (m37–m38), słupki co 2 m; latarnie na obu końcach i w środku."""
    if not mosty: return 0
    POM = unary_union([ln.buffer(w / 2 + 1.0, cap_style=2, join_style=2) for (ln, w, u) in mosty]).intersection(OKNO)
    n_ = 0
    for q in getattr(POM, "geoms", [POM]):
        if q.geom_type != "Polygon" or q.area < 5: continue
        q = q.simplify(.1); n_ += 1
        # oś mostu: najdłuższa droga zespołu; łuk spodu wg położenia wzdłuż osi
        osie = [ln for (ln, w, u) in mosty if ln.intersects(q)]
        os_ = max(osie, key=lambda l: l.length) if osie else LineString(q.minimum_rotated_rectangle.exterior.coords[:2])
        Lb = max(os_.length, 1.0)
        spod = lambda x, z: .17 - (.8 + .5 * (2 * abs(os_.project(Point(x, z)) / Lb - .5)) ** 2)
        mostowa = any((u.get("nazwa") or "") == "Mostowa" for (ln, w, u) in mosty if ln.intersects(q))
        JEZ_M = unary_union([ln.buffer(3.0, cap_style=2) for (ln, w, u) in mosty if ln.intersects(q) and u["klasa"] in ("pedestrian", "residential", "tertiary", "secondary")]).intersection(q) if mostowa else Polygon()
        # 24.11 (m34–m36, IMG_2013): most Staromiejski cały w jasnych płytach granitowych — jezdnia też, odcięta od chodników
        # dwoma pasami ciemniejszego granitu 0,3 m (bez kostki i bez bruku); pozostałe mosty bez zmian
        if mostowa and not JEZ_M.is_empty:
            PAS_M = JEZ_M.boundary.buffer(.15, cap_style=2).intersection(q)
            czesci = ((JEZ_M.difference(PAS_M), "rynek_plyty"), (PAS_M, "rynek_pas"), (q.difference(JEZ_M).difference(PAS_M), "rynek_plyty"))
        else:
            czesci = ((JEZ_M, "bruk"), (q.difference(JEZ_M) if not JEZ_M.is_empty else q, "kostka"))
        for (cz, mat) in czesci:
            for part in getattr(cz, "geoms", [cz]):
                if part.is_empty or part.geom_type != "Polygon": continue
                for tri in shapely.constrained_delaunay_triangles(part).geoms:
                    c = [np.array([x, .17, z]) for x, z in list(tri.exterior.coords)[:3]]
                    if np.cross(c[1] - c[0], c[2] - c[0])[1] < 0: c[1], c[2] = c[2], c[1]
                    S.tri_list("mosty", mat, c, (0., 1., 0.), (1., 0., 0.), [(p_[0], p_[2]) for p_ in c], None, [0, 1, 2])
        for tri in shapely.constrained_delaunay_triangles(q).geoms:          # spód: łuk
            c = [np.array([x, spod(x, z), z]) for x, z in list(tri.exterior.coords)[:3]]
            if np.cross(c[1] - c[0], c[2] - c[0])[1] > 0: c[1], c[2] = c[2], c[1]
            S.tri_list("mosty", "beton_most", c, (0., -1., 0.), (1., 0., 0.), [(p_[0], p_[2]) for p_ in c], None, [0, 1, 2])
        pts = list(shapely.geometry.polygon.orient(q, 1.0).exterior.coords)
        Wb = W.buffer(.5); lat_ = []
        for (x0, z0), (x1, z1) in zip(pts, pts[1:]):
            a_ = np.array([x0, 0., z0]); b_ = np.array([x1, 0., z1]); L = float(np.linalg.norm(b_ - a_))
            if L < .05: continue
            t = (b_ - a_) / L; n = nrm(np.cross(UP, t))                      # CCW: zewnętrze po prawej = n
            if q.contains(Point(*((a_ + b_) / 2 + n * .3)[[0, 2]])): n = -n
            m = max(1, int(L // 1.5))                                         # czoło belki z łukiem spodu
            for k in range(m):
                p0 = a_ + t * L * k / m; p1 = a_ + t * L * (k + 1) / m
                S.quad("mosty", "beton_most", p0 + UP * spod(p0[0], p0[2]), p1 + UP * spod(p1[0], p1[2]), p1 + UP * .17, p0 + UP * .17, kier=n)
            if not Wb.contains(Point((x0 + x1) / 2, (z0 + z1) / 2)): continue  # balustrada tylko nad wodą
            if not W.contains(Point(*((a_ + b_) / 2 + n * .8)[[0, 2]])): continue  # i tylko na bokach (za krawędzią woda), nie w poprzek wjazdu
            e = a_ - n * .12 + UP * .17
            S.quad("mosty", "balustrada_most", e, e + t * L, e + t * L + UP * 1.0, e + UP * 1.0, uv_o=e, kier=n)
            S.box("mosty", "latarnia_czarna", e + UP * 1.0 - n * .05, t, UP, n, L, .06, .1)
            for k in np.arange(0, L, 2.0): S.box("mosty", "latarnia_czarna", e + t * k - t * .05 - n * .05, t, UP, n, .1, 1.06, .1)
            if L > 6: lat_ += [(a_ - n * .5, math.atan2(t[2], t[0])), ((a_ + b_) / 2 - n * .5, math.atan2(t[2], t[0]))]
        zaj = []
        for (p_, ob) in lat_:                                                 # latarnie na moście (końce i środek)
            if any(np.hypot(p_[0] - z_[0], p_[2] - z_[2]) < 10 for z_ in zaj): continue
            zaj.append(p_); latarnia(S, p_[0], p_[2], .17, ob)
    # „Przechodzący przez rzekę” (OSM, 25 m od mostu): lina nad Brdą między dwoma masztami na nabrzeżach; figura —
    # model właściciela (paczka szyfrowana) wstawiana w grze w punkcie liny (rynek_punkty.json → linoskoczek)
    return n_


def lina_linoskoczka(S, d, W):
    """Lina nad Brdą w punkcie OSM „Przechodzący przez rzekę”, równoległa do osi mostu Mostowa, 7,5 m nad bulwarem."""
    pk = [p for p in d["punkty"] if (p["tagi"].get("name") or "").startswith("Przechodzący")]
    most = [u for u in d["ulice"] if (u.get("nazwa") or "") == "Mostowa" and u.get("most")]
    if not pk or not most: return None
    c = np.array(pk[0]["xz"]); m0, m1 = np.array(most[0]["linia"][0]), np.array(most[0]["linia"][-1]); kier = (m1 - m0) / np.linalg.norm(m1 - m0)
    ln = LineString([tuple(c - kier * 60), tuple(c + kier * 60)]); cz = ln.intersection(W)
    if cz.is_empty: return None
    seg = max(getattr(cz, "geoms", [cz]), key=lambda g_: g_.distance(Point(*c)) * -1)
    (x0, z0), (x1, z1) = seg.coords[0], seg.coords[-1]
    a = np.array([x0, 0., z0]) - np.array([kier[0], 0, kier[1]]) * 1.5; b = np.array([x1, 0., z1]) + np.array([kier[0], 0, kier[1]]) * 1.5
    for p_ in (a, b):
        S.rura("lina", "latarnia_czarna", p_ + UP * .12, p_ + UP * 8.2, .11, seg=8)
    S.rura("lina", "szyna", a + UP * 7.6, b + UP * 7.6, .015, seg=4)
    mid = (a + b) / 2
    return [round(mid[0], 2), 7.6, round(mid[2], 2)], [round(float(kier[0]), 3), round(float(kier[1]), 3)]


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
    # tory tramwajowe (OSM railway=tram, rozstaw 1000 mm jak w Bydgoszczy), szyny 2 cm ponad jezdnią; sieć trakcyjna 5,8 m
    for u in d["ulice"]:
        if u["klasa"] != "tory_tram" or len(u["linia"]) < 2: continue
        ln = LineString(u["linia"]).intersection(OKNO)
        for part in getattr(ln, "geoms", [ln]):
            if part.is_empty or part.geom_type != "LineString": continue
            pts = list(part.coords)
            for (x0, z0), (x1, z1) in zip(pts, pts[1:]):
                a = np.array([x0, 0., z0]); b_ = np.array([x1, 0., z1]); L = float(np.linalg.norm(b_ - a))
                if L < .05: continue
                t = (b_ - a) / L; nn = nrm(np.cross(UP, t))
                for s_ in (-.5, .5):
                    S.box("tory", "szyna", a + nn * (s_ - .035) - UP * .005 - t * .01, t, UP, nn, L + .02, .025, .07, bez=("-y",))
                S.rura("tory", "szyna", a + UP * 5.8, b_ + UP * 5.8, .008, seg=3)
            for s_ in np.arange(10, part.length, 32):               # maszty trakcji przy krawędzi jezdni
                p = np.array(part.interpolate(s_).coords[0]); p2 = np.array(part.interpolate(min(part.length, s_ + .5)).coords[0])
                tt = nrm(np.array([p2[0] - p[0], 0., p2[1] - p[1]])); nn = nrm(np.cross(UP, tt))
                for sg in (-1, 1):
                    q = np.array([p[0], 0., p[1]]) + nn * sg * 5.2
                    if CHOD.contains(Point(q[0], q[2])):
                        S.rura("tory", "latarnia_czarna", q, q + UP * 7.5, .09, seg=8)
                        S.rura("tory", "szyna", q + UP * 6.6, np.array([p[0], 5.8, p[1]]), .006, seg=3)
                        break
    # słupki żeliwne z łańcuchami wzdłuż krawężników przy skrzyżowaniu (zdjęcia IMG_1983–1988), do 90 m od środka
    J = np.array(SKRZYZOWANIE); nsl = 0
    for u in d["ulice"]:
        if u["klasa"] not in ("primary", "secondary", "tertiary") or len(u["linia"]) < 2: continue
        ln = LineString(u["linia"]).intersection(OKNO)
        if ln.is_empty or ln.geom_type != "LineString" or ln.distance(Point(*J)) > 90: continue
        w = {"primary": 14, "secondary": 12, "tertiary": 10}[u["klasa"]]
        for sg in (-1, 1):
            prev = None
            for s_ in np.arange(0, ln.length, 1.8):
                p = np.array(ln.interpolate(s_).coords[0])
                if np.hypot(*(p - J)) > 90 or np.hypot(*(p - J)) < 11: prev = None; continue
                p2 = np.array(ln.interpolate(min(ln.length, s_ + .3)).coords[0]); tt = nrm(np.array([p2[0] - p[0], 0., p2[1] - p[1]]))
                q = np.array([p[0], .12, p[1]]) + nrm(np.cross(UP, tt)) * sg * (w / 2 + .45)
                if not CHOD.contains(Point(q[0], q[2])): prev = None; continue
                S.rura("slupki", "latarnia_czarna", q, q + UP * .85, .065, seg=8)
                S.rura("slupki", "latarnia_czarna", q + UP * .85, q + UP * .95, .045, seg=6)
                if prev is not None:                                  # łańcuch: zwis 12 cm
                    a0 = prev + UP * .72; a1 = q + UP * .72; m_ = (a0 + a1) / 2 - UP * .12
                    S.rura("slupki", "latarnia_czarna", a0, m_, .012, seg=3); S.rura("slupki", "latarnia_czarna", m_, a1, .012, seg=3)
                prev = q; nsl += 1
    zaj = []
    for (q, ob) in lat:                                            # bez dwóch latarni bliżej niż 8 m
        if any(np.hypot(q[0] - z_[0], q[2] - z_[2]) < 8 for z_ in zaj): continue
        zaj.append(q); latarnia(S, q[0], q[2], .12, ob)
    lawki = 0
    for pk in d["punkty"]:
        tg = pk["tagi"]
        if tg.get("amenity") == "bench" and OKNO.contains(Point(*pk["xz"])):
            lawka(S, pk["xz"][0], pk["xz"][1], float(tg.get("direction", "0") or 0) * math.pi / 180 if str(tg.get("direction", "0")).replace(".", "").isdigit() else 0.0); lawki += 1
    # ogrodzenia z OSM (24.11 01:14 „każdy mur ma mieć teksturę, cegłę”): mury ze starej cegły z filarami i czapą,
    # płoty żeliwne jak balustrady nabrzeży, żywopłoty jak na osiedlu
    import wyposazenie
    budy = unary_union([b["poly"] for b in bud_lista]).buffer(.2)
    nmur = nplot = nzyw = 0
    for t_ in d["tereny"]:
        if not t_["rodzaj"].startswith("ogrodzenie:") or len(t_["linia"]) < 2: continue
        L_ = LineString(t_["linia"]).intersection(OKNO).difference(budy).difference(W)
        for g_ in ([L_] if L_.geom_type == "LineString" else [q for q in getattr(L_, "geoms", []) if q.geom_type == "LineString"]):
            if g_.length < .5: continue
            pts = list(g_.coords); rodz = t_["rodzaj"].split(":")[1]
            if rodz == "wall":
                wyposazenie.mur(S, pts, R_.choice((1.8, 2.0, 2.2)), R_.choice(("c_stara", "c_sredniowieczna", "c_czerwona", "c_mieszana")), .12, g="mury"); nmur += 1
            elif rodz == "fence":
                for (x0, z0), (x1, z1) in zip(pts, pts[1:]):
                    a = np.array([x0, .12, z0]); b_ = np.array([x1, .12, z1]); Ls = float(np.linalg.norm(b_ - a))
                    if Ls < .1: continue
                    tt = (b_ - a) / Ls; nn = nrm(np.cross(UP, tt))
                    S.quad("ploty", "balustrada_kuta", a, b_, b_ + UP * 1.2, a + UP * 1.2, uv_o=a, kier=nn)
                    S.quad("ploty", "balustrada_kuta", b_, a, a + UP * 1.2, b_ + UP * 1.2, uv_o=b_, kier=-nn)
                    for k in np.arange(0, Ls, 2.4): S.box("ploty", "latarnia_czarna", a + tt * k - tt * .04 - nn * .04, tt, UP, nn, .08, 1.26, .08)
                nplot += 1
            elif rodz == "hedge":
                wyposazenie.zywoplot(S, pts, R_); nzyw += 1
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
    return S, {"latarnie": len(zaj), "lawki": lawki, "drzewa": len(drzewa), "slupki": nsl, "mury": nmur, "ploty": nplot, "zywoploty": nzyw}, {"drzewa": drzewa, "latarnie": [[round(q[0], 2), round(q[2], 2), "stylowa"] for q in zaj]}


def main(out):
    out = Path(out); out.mkdir(parents=True, exist_ok=True)
    d = wczytaj(); R_ = random.Random(11)
    OBCE, konce = osiedle_przy_szwie()
    lacznik(d, konce)
    W = woda(d)
    D = Siatka(); opis = {}
    ulica_szwu = LineString([(-420, Y_LACZNIK), (160, Y_LACZNIK)]).buffer(3.5 + 2.6 + .5, cap_style=2)
    zajete = unary_union(OBCE + [ulica_szwu] + [LineString(u["linia"]).buffer(3.1, cap_style=2) for u in d["ulice"] if str(u["osm"]).startswith("lacznik_")])
    budynki = [b for b in d["budynki"] if not b["poly"].is_empty and b["poly"].area > 25 and OKNO.contains(b["poly"].centroid) and not b.get("czesc")
               and not b["poly"].intersects(zajete)]
    import zabytki
    wlasne = {b["osm"]: b["poly"] for b in budynki if b["osm"] in zabytki.WLASNE}
    # Opera Nova (24.11): płyty dookoła kręgów (m44) i schody-widownia nad Brdą (obszar OSM „theatre” przy Operze)
    d["fartuchy"] = [wlasne[k].buffer(12.0 if k == zabytki.OSM_OPERA else 7.0, join_style=1) for k in (zabytki.OSM_OPERA, zabytki.OSM_OPERA_IV) if k in wlasne]
    for t in d["tereny"]:
        if t["rodzaj"] == "theatre" and len(t["linia"]) > 3 and zabytki.OSM_OPERA in wlasne:
            A = Polygon(t["linia"]).buffer(0)
            if A.distance(wlasne[zabytki.OSM_OPERA]) < 40:
                # środek łuków: punkt, od którego oba łuki obrysu są w stałej odległości (nad pomostem, od strony rzeki)
                xs = np.linspace(A.bounds[0], A.bounds[2], 41); zs = np.linspace(A.bounds[1] - 30, A.bounds[1], 61)
                pts = [np.array(p) for p in A.exterior.coords]
                def rozrzut(x, z):
                    dd = sorted(math.hypot(p[0] - x, p[1] - z) for p in pts)
                    k = len(dd) // 2; return np.std(dd[:k]) + np.std(dd[k:])
                sx, sz = min(((x, z) for x in xs for z in zs), key=lambda q: rozrzut(*q))
                wlasne[zabytki.OSM_AMFITEATR] = (A, (sx, sz))
        if t["rodzaj"] == "pier" and len(t["linia"]) > 3 and zabytki.OSM_OPERA in wlasne:
            A = Polygon(t["linia"]).buffer(0)
            if A.distance(wlasne[zabytki.OSM_OPERA]) < 50: wlasne["pomost_opery"] = A
    budynki = [b for b in budynki if b["osm"] not in zabytki.WLASNE]
    PUBL0 = przestrzen_publiczna(d, W)
    budynki = dzialki(budynki, PUBL0, R_)
    # pierzeje Rynku: obszary pieszych w promieniu 75 m od środka obszaru „Stary Rynek” (plac składa się z kilku obszarów)
    rynek_ = [Polygon(u["linia"]).buffer(0) for u in d["ulice"] if u.get("obszar") and u.get("nazwa") == "Stary Rynek" and len(u["linia"]) > 3]
    if rynek_:
        c_r = unary_union(rynek_).centroid
        RYNEK_PIERZEJE[0] = unary_union([Polygon(u["linia"]).buffer(0) for u in d["ulice"] if u.get("obszar") and len(u["linia"]) > 3
                                         and u["klasa"] == "pedestrian" and Polygon(u["linia"]).buffer(0).centroid.distance(c_r) < 75])
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
    Z = zabytki.zbuduj(wlasne, PUBL0, POZIOM_WODY)                                  # skrzyżowanie Focha × Gdańska × Jagiellońska, kościół
    for (g, m), c in Z.dane().items():
        dd = D._c(g, m); b0 = len(dd["v"])
        for f in ("v", "n", "t", "uv", "uv1"): dd[f] += c[f]
        dd["i"] += [b0 + q for q in c["i"]]
    bud = unary_union([b["poly"] for b in budynki] + [q for k, q in wlasne.items() if k not in (zabytki.OSM_BARKA, zabytki.OSM_AMFITEATR, "pomost_opery")] + [Q.buffer(.3) for Q in OBCE])
    T, pola, mosty, (PLAC_, CHOD_, NABRZ_) = teren(d, bud, W)
    Dt, pola_d, punkty = detale(d, budynki, PLAC_, CHOD_, NABRZ_, W, random.Random(5))
    # drzewa na skarpie między Operą a Brdą (m44–m45: kilkanaście drzew na trawniku przed kręgami)
    if zabytki.OSM_OPERA in wlasne and TRAWNIK[0] is not None:
        op = wlasne[zabytki.OSM_OPERA]; Rd = random.Random(44)
        pas = TRAWNIK[0].intersection(op.buffer(40).difference(op.buffer(13))).intersection(box(op.bounds[0] - 10, -80, op.bounds[2] + 5, op.centroid.y))
        stare = [Point(x, z) for x, z, _ in punkty["drzewa"]]
        for x in np.arange(pas.bounds[0], pas.bounds[2], 7.5):
            for z in np.arange(pas.bounds[1], pas.bounds[3], 7.5):
                q = Point(x + Rd.uniform(-2, 2), z + Rd.uniform(-2, 2))
                if pas.buffer(-2.5).contains(q) and all(q.distance(o) > 6 for o in stare):
                    punkty["drzewa"].append([round(q.x, 2), round(q.y, 2), "duze" if Rd.random() < .35 else "srednie"]); stare.append(q)
    pola.update(pola_d)
    for (g, m), c in Dt.dane().items():
        dd = D._c(g, m); b0 = len(dd["v"])
        for f in ("v", "n", "t", "uv", "uv1"): dd[f] += c[f]
        dd["i"] += [b0 + q for q in c["i"]]
    pola["mosty_zespoly"] = mosty_scal(T, mosty, W)
    lina = lina_linoskoczka(T, d, W)
    if lina:
        punkty["linoskoczek"] = [lina[0]]; punkty["linoskoczek_kier"] = lina[1]
        # figura w grze (paczka szyfrowana braxhorn-sklep-bydgoszcz-linoskoczek-01): [paczka, plik, x, z, kier_x, kier_z, y];
        # kier prostopadły do liny (drąg w poprzek), przód modelu (+z) wzdłuż liny — BraxBydgoszcz.ModeleWlasciciela
        (lx, ly, lz), (kx, kz) = lina[0], lina[1]
        punkty["modele"] = [["bydgoszcz_linoskoczek", "linoskoczek", lx, lz, round(kz, 4), round(-kx, 4), ly]]
    # bariery (gra): Brda ucięta krawędzią okna = granicą miasta — tam nie ma muru nabrzeża; niewidzialna ściana od lustra
    # wody − 1 m do +3 m wzdłuż odcinka [x0, z0, x1, z1, y0, y1] (metry), żeby z tafli nie wyjść pod płotem granicy
    bar = []
    brzeg_okna = OKNO.exterior
    for q in getattr(W, "geoms", [W]):
        if q.is_empty: continue
        pts = list(q.exterior.coords)
        for (x0, z0), (x1, z1) in zip(pts, pts[1:]):
            if math.hypot(x1 - x0, z1 - z0) > .2 and brzeg_okna.distance(Point((x0 + x1) / 2, (z0 + z1) / 2)) < .3:
                bar.append([round(x0, 2), round(z0, 2), round(x1, 2), round(z1, 2), POZIOM_WODY - 1.0, 3.0])
    punkty["bariery"] = bar
    if "modele" in punkty: punkty["modele"] = punkty.pop("modele")              # modele na końcu (wczytywacz C# czyta do „]]”)
    (out / "rynek_punkty.json").write_text(json.dumps(punkty, ensure_ascii=False), encoding="utf-8")
    if TRAWNIK[0] is not None: zapisz_maske_trawy(TRAWNIK[0], punkty.get("drzewa", []), out / "rynek_trawa_maska.bin")
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
