"""MIASTO R1 — osiedle bydgoskie: wyposażenie ulic z prawdziwych położeń OpenStreetMap (ODbL), pobranych 23.11.2026
(api.openstreetmap.org/api/0.6/map, wycinek ±200 m wokół 32a → dane/osiedle_osm_map.xml).

Z mapy: przejścia dla pieszych (highway=crossing, pasy wycinane z asfaltu w teren.py), sygnalizacje (traffic_signals),
przystanki (stop_position, wiata po stronie jezdni, na której stoi punkt), szlabany (lift_gate), ławki, paczkomaty,
kontenery do segregacji (recycling), hydranty, płoty (barrier=fence) i żywopłoty (barrier=hedge).
Wygląd ze zdjęć właściciela: szlaban — pomarańczowa skrzynka i biało-czerwone ramię (IMG_1631), wiata — granatowa rama,
szkło, ławka (IMG_1670–1675), płot szpitala — rdzawoczerwony z prętów 1,6 m (IMG_1673, IMG_1675), latarnie główne —
betonowe słupy ~10 m z wysięgnikiem (IMG_1669–1670), osiedlowe — stalowe ~6 m (IMG_1427), młode drzewka przy głównej
ulicy z trzema palikami (IMG_1669, IMG_1671). Prawdziwych szyldów i marek nie przenosimy (ZASADY 7.4).
"""
import json
import math
import random
import xml.etree.ElementTree as ET
from pathlib import Path

import numpy as np
from shapely.geometry import LineString, Point

from kit import Siatka, UP, nrm

DANE = Path(__file__).resolve().parent / "dane"
X3 = lambda x, y, z: np.array([x, y, z], float)


def rzut():
    return json.loads((DANE / "osiedle_rzut.json").read_text())


def osm():
    """Punkty i linie z wyciągu OSM w metrach osiedla (x wschód, z północ)."""
    R = rzut(); lat0, lon0, MLAT, MLON = R["lat0"], R["lon0"], R["MLAT"], R["MLON"]
    r = ET.parse(DANE / "osiedle_osm_map.xml").getroot()
    nd = {}; pkt = []
    for n in r.iter("node"):
        p = ((float(n.get("lon")) - lon0) * MLON, (float(n.get("lat")) - lat0) * MLAT); nd[n.get("id")] = p
        t = {g.get("k"): g.get("v") for g in n.iter("tag")}
        if t: pkt.append((p, t))
    linie = []
    for w in r.iter("way"):
        t = {g.get("k"): g.get("v") for g in w.iter("tag")}
        if t.get("barrier") in ("fence", "hedge", "wall"):
            linie.append(([nd[x.get("ref")] for x in w.iter("nd") if x.get("ref") in nd], t))
    return pkt, linie


def droga_przy(p, ulice, maks=12.0, klasy=None):
    """Najbliższa oś jezdni: (odległość, punkt na osi, styczna, klasa, LineString)."""
    P = Point(*p); best = None
    for u in ulice:
        if len(u["linia"]) < 2 or (klasy and u["klasa"] not in klasy): continue
        L = LineString(u["linia"]); s = L.project(P); q = L.interpolate(s)
        dd = P.distance(q)
        if dd > maks or (best and dd >= best[0]): continue
        a = L.interpolate(max(0, s - 1.0)); b = L.interpolate(min(L.length, s + 1.0))
        t = nrm([b.x - a.x, 0, b.y - a.y])
        best = (dd, np.array([q.x, q.y]), t, u["klasa"], L)
    return best


def zebry(pkt, ulice, szer):
    """Przejścia „zebra” (P-10): pasy 0,5 m co 1,0 m, długości 4 m (główna) / 3 m (osiedlowe), wzdłuż osi jezdni."""
    from shapely.geometry import Polygon
    out = []
    for (p, t) in pkt:
        if t.get("highway") != "crossing" and t.get("crossing") not in ("zebra", "marked", "uncontrolled", "traffic_signals"): continue
        dr = droga_przy(p, ulice, 4.0, tuple(szer))
        if not dr: continue
        _, q, tt, kl, _L = dr
        a = np.array([tt[0], tt[2]]); n = np.array([-a[1], a[0]]); W = szer[kl]; dl = 4.0 if kl in ("primary", "secondary", "tertiary") else 3.0
        k = -W / 2 + .35
        while k + .5 <= W / 2 - .2:
            c = q + n * (k + .25)
            out.append(Polygon([tuple(c - a * dl / 2 - n * .25), tuple(c + a * dl / 2 - n * .25), tuple(c + a * dl / 2 + n * .25), tuple(c - a * dl / 2 + n * .25)]))
            k += 1.0
    return out


def zbuduj(d, G, szer, okno, drzewa, latarnie):
    """Siatka wyposażenia + lista nazw (opis). G = geometria terenu z teren.zbuduj (jezdnie, chodniki, budynki)."""
    S = Siatka(); R = random.Random(23); opis = {}
    pkt, linie = osm()
    W = lambda p: okno.buffer(-1).contains(Point(*p))
    ulice = d["ulice"]

    def ile(k): opis[k] = opis.get(k, 0) + 1

    def ustaw(p, dr):
        """Układ lokalny: o na ziemi w p, t wzdłuż jezdni, n od jezdni (w stronę chodnika)."""
        _, q, tt, _, _ = dr
        n = nrm(np.array([p[0] - q[0], 0, p[1] - q[1]]))
        if np.linalg.norm([p[0] - q[0], p[1] - q[1]]) < .3: n = np.array([-tt[2], 0, tt[0]])
        return X3(p[0], .12, p[1]), tt, n

    for (p, t) in pkt:
        if not W(p): continue
        hw, am, ba = t.get("highway"), t.get("amenity"), t.get("barrier")
        if hw == "traffic_signals":
            dr = droga_przy(p, ulice, 15)
            if not dr: continue
            _, q, tt, kl, _ = dr
            for strona in (1, -1):        # sygnalizator po obu stronach jezdni, na krawędzi chodnika
                n = np.array([-tt[2], 0, tt[0]]) * strona
                o = X3(q[0], .12, q[1]) + n * (szer.get(kl, 6) / 2 + .6)
                sygnalizator(S, o, tt * strona, -n); ile("sygnalizacje")
        elif t.get("public_transport") == "stop_position" or (hw == "bus_stop" and t.get("shelter") == "yes"):
            dr = droga_przy(p, ulice, 10, ("primary", "secondary", "tertiary"))
            if not dr: continue
            _, q, tt, kl, _ = dr
            n = nrm(np.array([p[0] - q[0], 0, p[1] - q[1]]))
            o = X3(q[0], .12, q[1]) + n * (szer.get(kl, 7) / 2 + 2.4)
            wiata(S, o, tt, n); ile("przystanki")
        elif ba == "lift_gate":
            dr = droga_przy(p, ulice, 8)
            if not dr: continue
            _, q, tt, kl, _ = dr
            n = np.array([-tt[2], 0, tt[0]]); w = szer.get(kl, 4.6)
            szlaban(S, X3(q[0], .12, q[1]) - n * (w / 2 + .45), n, tt, w + .3); ile("szlabany")
        elif am == "bench":
            dr = droga_przy(p, ulice + [], 25)
            tt = dr[2] if dr else np.array([1., 0, 0])
            lawka(S, X3(p[0], .12, p[1]), tt, np.array([-tt[2], 0, tt[0]])); ile("lawki")
        elif am == "parcel_locker":
            dr = droga_przy(p, ulice, 30); tt = dr[2] if dr else np.array([1., 0, 0])
            paczkomat(S, X3(p[0], .12, p[1]), tt); ile("paczkomaty")
        elif am == "recycling":
            dr = droga_przy(p, ulice, 30); tt = dr[2] if dr else np.array([1., 0, 0])
            kontenery(S, X3(p[0], .12, p[1]), tt); ile("kontenery")
        elif t.get("emergency") == "fire_hydrant":
            hydrant(S, X3(p[0], .12, p[1])); ile("hydranty")
    for (pts, t) in linie:
        L = LineString(pts).intersection(okno)
        if L.is_empty: continue
        geoms = [L] if L.geom_type == "LineString" else [g for g in getattr(L, "geoms", []) if g.geom_type == "LineString"]
        for g in geoms:
            if t.get("barrier") == "hedge": zywoplot(S, list(g.coords), R); ile("zywoploty")
            else: plot(S, list(g.coords)); ile("ploty")
    for (x, z, typ) in latarnie:
        dr = droga_przy((x, z), ulice, 20)
        tt = dr[2] if dr else np.array([1., 0, 0])
        n = nrm(np.array([dr[1][0] - x, 0, dr[1][1] - z])) if dr else np.array([0., 0, 1])
        latarnia(S, X3(x, .12, z), n, typ == "ulica"); ile("latarnie")
    for (x, z, typ) in drzewa:
        if typ == "mlode": paliki(S, X3(x, .08, z)); ile("paliki")
    return S, opis


# ---------------- elementy ----------------
def B(S, g, m, o, ex, ez, sx, sy, sz, bez=()):
    """Skrzynka: o = środek podstawy, ex wzdłuż, ez w głąb (oba poziome)."""
    ex, ez = nrm(ex), nrm(ez)
    c = o - ex * sx / 2 - ez * sz / 2
    S.box(g, m, c, ex, UP, ez, sx, sy, sz, bez=bez)


def sygnalizator(S, o, t, n):
    g = "wyposazenie"
    S.rura(g, "sygnal_szary", o, o + UP * 3.2, .055, seg=8)
    h = o + UP * 2.2 + n * .12
    B(S, g, "sygnal_czarny", h, t, n, .32, .95, .22)
    B(S, g, "sygnal_czarny", h - n * .005 + UP * -.02, t, n, .52, 1.0, .02)                 # tarcza tła (z tyłu)
    for k, m in enumerate(("swiatlo_czerwone", "swiatlo_zolte", "swiatlo_zielone")):
        B(S, g, m, h + UP * (.72 - k * .28) + n * .12, t, n, .2, .2, .03)


def wiata(S, o, t, n):
    """Wiata przystankowa 4,6 x 1,5 x 2,5 m: granatowa rama, szkło z tyłu i z boków, dach, ławka (IMG_1670, IMG_1672)."""
    g = "wyposazenie"; L, G, H = 4.6, 1.5, 2.5
    back = o + n * G
    for a in (-L / 2, L / 2):
        for b in (0, G):
            S.rura(g, "wiata_rama", o + t * a + n * b, o + t * a + n * b + UP * H, .05, seg=6)
    B(S, g, "wiata_rama", o + n * (G / 2) + UP * H, t, n, L + .3, .12, G + .3)
    B(S, g, "szyba_wiata", back - n * .02 + UP * .15, t, n, L - .1, 2.1, .02)
    for a in (-L / 2, L / 2):
        B(S, g, "szyba_wiata", o + t * a + n * (G / 2 + .2) + UP * .15, n, t, G - .4, 2.1, .02)
    B(S, g, "wiata_rama", back - n * .3 + UP * .45, t, n, L * .7, .05, .38)
    for a in (-L * .3, L * .3):
        B(S, g, "wiata_rama", back - n * .3 + UP * 0, t, n, .05, .45, .05)


def szlaban(S, o, kier, t, dl):
    """Słupek-skrzynka pomarańczowy, ramię biało-czerwone przez jezdnię (IMG_1631)."""
    g = "wyposazenie"
    B(S, g, "szlaban_pomarancz", o, kier, t, .32, 1.05, .32)
    a0 = o + UP * .9 + t * .2
    seg = .5; k = 0; x = .1
    while x < dl:
        B(S, g, "szlaban_czerwony" if k % 2 == 0 else "szlaban_bialy", a0 + kier * (x + min(seg, dl - x) / 2), kier, t, min(seg, dl - x), .07, .06)
        x += seg; k += 1
    S.rura(g, "stal", o + kier * dl + UP * 0, o + kier * dl + UP * .85, .03, seg=6)


def lawka(S, o, t, n):
    g = "wyposazenie"
    for a in (-.7, .7):
        B(S, g, "beton_lawka", o + t * a, t, n, .12, .42, .45)
    for k in range(3):
        B(S, g, "lawka_drewno", o + UP * .42 + n * (-.15 + k * .15), t, n, 1.8, .04, .12)
    for k in range(2):
        B(S, g, "lawka_drewno", o + UP * (.62 + k * .16) + n * .25, t, n, 1.8, .12, .035)


def paczkomat(S, o, t):
    g = "wyposazenie"; n = np.array([-t[2], 0, t[0]])
    B(S, g, "paczkomat", o, t, n, 2.4, 1.9, .65)
    B(S, g, "paczkomat_ekran", o + UP * 1.05 - n * .33, t, n, .35, .45, .02)


def kontenery(S, o, t):
    g = "wyposazenie"; n = np.array([-t[2], 0, t[0]])
    for k, m in enumerate(("kontener_zielony", "kontener_zolty", "kontener_niebieski")):
        B(S, g, m, o + t * (k - 1) * 1.7, t, n, 1.5, 1.6, 1.5)
        B(S, g, m, o + t * (k - 1) * 1.7 + UP * 1.6, t, n, 1.3, .12, 1.3)


def hydrant(S, o):
    g = "wyposazenie"
    S.rura(g, "hydrant", o, o + UP * .75, .09, seg=8)
    S.rura(g, "hydrant", o + UP * .75, o + UP * .85, .06, seg=8)
    S.rura(g, "hydrant", o + UP * .45 + np.array([-.16, 0, 0]), o + UP * .45 + np.array([.16, 0, 0]), .04, seg=6)


def plot(S, pts):
    """Płot z prętów 1,6 m (rdzawoczerwony jak przy szpitalu): słupki co 2,5 m, wypełnienie z tekstury prętów (alfa)."""
    g = "wyposazenie"; H = 1.6
    for (x0, z0), (x1, z1) in zip(pts, pts[1:]):
        a, b = X3(x0, .1, z0), X3(x1, .1, z1); L = float(np.linalg.norm(b - a))
        if L < .2: continue
        t = (b - a) / L; n = np.array([-t[2], 0, t[0]])
        for s in np.arange(0, L, 2.5):
            S.rura(g, "plot_rama", a + t * s, a + t * s + UP * H, .03, seg=6)
        for y in (.12, H - .05):
            S.rura(g, "plot_rama", a + UP * y, b + UP * y, .02, seg=6)
        S.quad(g, "plot_prety", a + UP * .1, b + UP * .1, b + UP * H, a + UP * H, t=t, kier=n)
        S.quad(g, "plot_prety", b + UP * .1, a + UP * .1, a + UP * H, b + UP * H, t=-t, kier=-n)


def zywoplot(S, pts, R):
    """Żywopłot strzyżony: rdzeń z liści (nieprzezroczysty) + zewnętrzna warstwa liści wycinana 6 cm dalej (nierówna
    sylwetka), odcinki ~1 m o wysokości 1,0–1,35 m i szerokości 0,8–1,0 m (IMG_1631, IMG_1637)."""
    g = "zielen"
    for (x0, z0), (x1, z1) in zip(pts, pts[1:]):
        a, b = X3(x0, .08, z0), X3(x1, .08, z1); L = float(np.linalg.norm(b - a))
        if L < .3: continue
        t = (b - a) / L; n = np.array([-t[2], 0, t[0]])
        s = 0.0
        while s < L:
            dl = min(L - s, R.uniform(.8, 1.3)); h = R.uniform(1.0, 1.35); w = R.uniform(.8, 1.0)
            c = a + t * (s + dl / 2)
            B(S, g, "zywoplot", c, t, n, dl + .02, h, w, bez=("-y",))
            B(S, g, "zywoplot_brzeg", c, t, n, dl + .14, h + .07, w + .12, bez=("-y",))
            s += dl


def latarnia(S, o, ku_jezdni, glowna):
    """Główna: betonowy słup 10 m, wysięgnik 1,8 m, oprawa (IMG_1669–1670). Osiedlowa: stalowy słup 6 m, krótki wysięgnik."""
    g = "wyposazenie"
    H, r0, r1, wys, mat = (10.0, .13, .08, 1.8, "latarnia_beton") if glowna else (6.0, .07, .05, .6, "latarnia_stal")
    S.rura(g, mat, o, o + UP * H * .5, r0, seg=8)
    S.rura(g, mat, o + UP * H * .5, o + UP * H, r1, seg=8)
    k = nrm(ku_jezdni); top = o + UP * H
    S.rura(g, "latarnia_stal", top - UP * .3, top + k * wys, .04, seg=6)
    B(S, g, "oprawa", top + k * (wys + .25) - UP * .05, k, np.array([-k[2], 0, k[0]]), .65, .14, .28)


def paliki(S, o):
    """Trzy paliki z poprzeczkami przy młodym drzewku (IMG_1669, IMG_1671)."""
    g = "zielen"
    for k in range(3):
        f = 2 * math.pi * k / 3
        p = o + np.array([math.cos(f) * .45, 0, math.sin(f) * .45])
        S.rura(g, "palik", p, p + UP * 1.8, .035, seg=5)
    for k in range(3):
        f0, f1 = 2 * math.pi * k / 3, 2 * math.pi * (k + 1) / 3
        p0 = o + np.array([math.cos(f0) * .45, 1.2, math.sin(f0) * .45]); p1 = o + np.array([math.cos(f1) * .45, 1.2, math.sin(f1) * .45])
        S.rura(g, "palik", p0, p1, .025, seg=5)
