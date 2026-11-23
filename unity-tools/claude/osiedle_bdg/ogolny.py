"""MIASTO R1 — osiedle bydgoskie: budynek z obrysu OpenStreetMap (dowolny wielokąt) — szpital, market, szkoła, poczta,
pawilony, bloki przy Wyszyńskiego i Żmudzkiej. Ściany wzdłuż każdego boku obrysu, okna PCV we wnęce co `rozstaw`
na każdej kondygnacji (te same okna co 32a: ościeża, parapet, rama, pokój za szybą), cokół, attyka z obróbką,
płaski dach z papy (triangulacja obrysu) albo dach kopertowy z dachówki (pawilon 30B, IMG_1596).

Wymiary i kolory każdego budynku są w tabeli SPEC w osiedle.py ze źródłem (OSM building:levels albo zdjęcie właściciela).
"""
import math
import sys
from pathlib import Path

import numpy as np
import shapely
from shapely.geometry import Polygon

sys.path.insert(0, str(Path(__file__).resolve().parent))
from kit import Siatka, UP, nrm  # noqa: E402
import blok_32a as B  # noqa: E402


def zbuduj(poly, sp, nazwa="budynek"):
    """poly — obrys w metrach (shapely), sp — słownik: kond, h_kond, parter (wysokość parteru), tynk, cokol, okna
    (rozstaw, szer, wys, parapet), witryny (True = parter przeszklony), attyka, dach ('plaski'|'koperta'|'szklo'),
    pasy (materiał pasa międzyokiennego, opcjonalnie). Zwraca nową siatkę w układzie świata (metry)."""
    S = Siatka(); stare = (B.S, B.okno_nr[0], B.POZIOM0, B.KOND)
    B.S = S; B.okno_nr[0] = 0
    kond, hk = sp["kond"], sp.get("h_kond", 3.0)
    p0 = sp.get("parter", hk)
    B.POZIOM0, B.KOND = 0.0, hk
    H = p0 + (kond - 1) * hk
    att = sp.get("attyka", .6)
    try:
        P = poly.buffer(0)
        if P.geom_type == "MultiPolygon": P = max(P.geoms, key=lambda q: q.area)
        P = shapely.geometry.polygon.orient(P.simplify(.15), 1.0)          # przeciwnie do zegara: na zewnątrz po prawej
        pts = list(P.exterior.coords); fronty = []
        rozst, ow, oh, opar = sp.get("okna", (3.0, 1.4, 1.4, .9))
        for (x0, z0), (x1, z1) in zip(pts, pts[1:]):
            a = np.array([x0, 0., z0]); b = np.array([x1, 0., z1]); L = float(np.linalg.norm(b - a))
            if L < .3: continue
            t = (b - a) / L                         # wzdłuż boku; obrys CCW (x, z) -> zewnętrze po prawej
            # widz z zewnątrz patrzy na ścianę: lewa krawędź = b? sprawdzamy normalną cross(UP, t)
            n = nrm(np.cross(UP, t))
            mid = (a + b) / 2 + n * .5
            if P.contains(shapely.geometry.Point(mid[0], mid[2])):           # normalna do środka — odwróć kierunek
                a, b = b, a; t = -t; n = -n
            okna = []
            fk = sp.get("okna_kier") or sp.get("front_kier")
            czy_front = bool(fk) and n[0] * fk[0] + n[2] * fk[1] > .5
            if czy_front: fronty.append((a, b, t, n, L))
            kier_ok = sp.get("okna_kier")            # okna/witryny tylko na ścianach zwróconych w tę stronę (front marketu)
            if L >= 2.2 and sp.get("okna_on", True) and (kier_ok is None or n[0] * kier_ok[0] + n[2] * kier_ok[1] > .5):
                k = int((L - .8) // rozst)
                if k >= 1:
                    off = (L - k * rozst) / 2
                    for i in range(k):
                        c = off + (i + .5) * rozst
                        for f in range(kond):
                            fl = 0 if f == 0 else p0 + (f - 1) * hk
                            if f == 0 and sp.get("witryny"):
                                okna.append((c - rozst / 2 + .15, c + rozst / 2 - .15, .45, min(p0 - .5, 3.0), "witryna", 0))
                            else:
                                okna.append((c - ow / 2, c + ow / 2, fl + opar, fl + opar + oh, "okno", f))
            cok = sp.get("h_cokol", .5)
            S.sciana(nazwa, sp.get("cokol", "cokol4"), a + UP * -.25, t, L, cok + .25, [])
            gora = [(u0, u1, v0 - cok, v1 - cok, r, pp) for (u0, u1, v0, v1, r, pp) in okna]
            if sp.get("pola_kolorow") and L > 4:
                import blok4, random
                R = random.Random(hash((round(x0, 1), round(z0, 1))) & 0xffff)
                kol = sp["pola_kolorow"]; pola = []; u = 0.0; Hh = H + att - cok
                while u < L - .01:                      # pasy pionowe 2–3 przęsła, każdy podzielony na 2–3 pola wysokości
                    w = min(L - u, rozst * R.choice((2, 2, 3))); v = 0.0
                    cuts = sorted(R.sample([hk * k for k in range(2, kond)], min(2, max(0, kond - 2))))
                    for v1 in cuts + [Hh]:
                        if v1 - v > .5: pola.append((u, u + w, v, v1, R.choice(kol)))
                        v = v1
                    u += w
                blok4.sciana_pola(nazwa, a + UP * cok, t, L, Hh, gora, pola, uv1=(L, Hh))
            else:
                sciana(nazwa, sp["tynk"], a + UP * cok, t, n, L, H + att - cok, gora, (L, H + att))
            if sp.get("pas_gorny") and not czy_front:   # pas blachy pod dachem 5 cm przed licem (market: niebieski, IMG_1730)
                pm, ph = sp["pas_gorny"]
                S.box(nazwa, pm, a + UP * (H + att - ph) - t * .05, t, UP, n, L + .1, ph + .05, .06)
        y = H
        for k in range(sp.get("nadbudowki", 0)):          # maszynownie wind / klatki nad dachem (IMG_1722–1726)
            r = list(P.minimum_rotated_rectangle.exterior.coords)[:4]
            e1 = np.subtract(r[1], r[0]); e2 = np.subtract(r[2], r[1])
            if np.linalg.norm(e1) < np.linalg.norm(e2): r = r[1:] + r[:1]; e1, e2 = e2, e1
            Lr = np.linalg.norm(e1); ax_ = e1 / Lr; bx_ = e2 / np.linalg.norm(e2)
            c = np.array(r[0]) + ax_ * Lr * (k + 1) / (sp["nadbudowki"] + 1) + e2 / 2
            Q = shapely.geometry.Polygon([tuple(c + ax_ * i * 3.2 + bx_ * j * 4.5) for i, j in ((-1, -1), (1, -1), (1, 1), (-1, 1))])
            Sn = zbuduj(Q, dict(kond=1, h_kond=3.4, parter=3.4, tynk=sp.get("tynk_nadb", "wysz_bez"), okna_on=False, attyka=.3, h_cokol=.01), nazwa + "_n")
            for (gg, m), cz in Sn.dane().items():
                dd = S._c(gg, m); b0 = len(dd["v"])
                dd["v"] += [(vx, vy + y + att * 0, vz) for (vx, vy, vz) in cz["v"]]
                for f in ("n", "t", "uv", "uv1"): dd[f] += cz[f]
                dd["i"] += [b0 + q for q in cz["i"]]
        if sp.get("market") and fronty:
            market_front(S, nazwa, max(fronty, key=lambda f: f[4]), H, att)
        if sp.get("ladowisko"):                    # SOR (IMG_1727, IMG_1744–1751): zadaszenie z lameli nad całym dachem, lądowisko
            sor_dach(S, nazwa, P, y + att)
        if sp.get("sor") and fronty:
            sor_front(S, nazwa, max(fronty, key=lambda f: f[4]), H)
        if sp.get("dach", "plaski") == "koperta":
            dach_koperta(S, nazwa, P, y + .05, sp.get("dach_mat", "dachowka"), sp.get("spadek", 28))
        else:
            dach_plaski(S, nazwa, P, y, att, sp.get("dach_mat", "papa"))
    finally:
        B.S, B.okno_nr[0], B.POZIOM0, B.KOND = stare
    return S


def sciana(g, mat, o, t, n, L, H, okna, uv1):
    """Ściana z oknami (te same elementy co blok 32a)."""
    S = B.S
    dz = [(u0 - B.OPASKA, u1 + B.OPASKA, v0, v1 + B.OPASKA) for (u0, u1, v0, v1, r, p) in okna if r != "witryna"] + \
         [(u0, u1, v0, v1) for (u0, u1, v0, v1, r, p) in okna if r == "witryna"]
    S.sciana(g, mat, o, t, L, H, dz, uv1_skala=uv1)
    P = lambda u, v, d=0.0: o + t * u + UP * v - n * d
    for (u0, u1, v0, v1, r, pp) in okna:
        if r != "witryna":
            for aa, bb, cc, dd in (((u0 - B.OPASKA, v0), (u0, v0), (u0, v1 + B.OPASKA), (u0 - B.OPASKA, v1 + B.OPASKA)),
                                   ((u1, v0), (u1 + B.OPASKA, v0), (u1 + B.OPASKA, v1 + B.OPASKA), (u1, v1 + B.OPASKA)),
                                   ((u0, v1), (u1, v1), (u1, v1 + B.OPASKA), (u0, v1 + B.OPASKA))):
                S.quad(g, "opaska", P(*aa), P(*bb), P(*cc), P(*dd), t=t, kier=n)
            B.okno(g, o, t, n, u0, u1, v0, v1, "okno", pp)
        else:
            B.okno(g, o, t, n, u0, u1, v0, v1, "klatka", 0)        # witryna: jedno pole, bez parapetu mieszkaniowego


def sor_dach(S, g, P, y):
    """Zadaszenie z lameli 1,6 m nad dachem, wysunięte 1,5 m (rama + lamele co 1 m, widać niebo między nimi) na słupach;
    lądowisko 22 x 22 m w środku: płyta z obwódką i białym krzyżem na czerwonym polu (lotnicze IMG_1751)."""
    mr = list(P.minimum_rotated_rectangle.exterior.coords)[:4]
    e1 = np.subtract(mr[1], mr[0]); e2 = np.subtract(mr[2], mr[1])
    if np.linalg.norm(e1) < np.linalg.norm(e2): mr = mr[1:] + mr[:1]; e1, e2 = e2, e1
    L, W = float(np.linalg.norm(e1)), float(np.linalg.norm(e2)); a = np.array([e1[0], 0, e1[1]]) / L; b = np.array([e2[0], 0, e2[1]]) / W
    o = np.array([mr[0][0], 0, mr[0][1]]); yl = y + 1.6; ok = 1.5
    Q = lambda u, v, yy=yl: o + a * u + b * v + UP * yy
    for (p0, kx, dl) in ((Q(-ok, -ok), a, L + 2 * ok), (Q(-ok, W + ok - .4), a, L + 2 * ok), (Q(-ok, -ok), b, W + 2 * ok), (Q(L + ok - .4, -ok), b, W + 2 * ok)):
        ky = b if kx is a else a
        S.box(g, "lamele", p0, kx, UP, ky, dl, .35, .4)
    u = -ok + 1.0
    while u < L + ok - 1.0:
        S.box(g, "lamele", Q(u, -ok + .4) + UP * .05, a, UP, b, .08, .25, W + 2 * ok - .8); u += 1.0
    for (u, v) in ((2, 2), (L - 2, 2), (2, W - 2), (L - 2, W - 2), (L / 2, 2), (L / 2, W - 2)):
        S.rura(g, "stal", Q(u, v, y), Q(u, v, yl), .15, seg=8)
    c = (L / 2, W / 2); hw = 11.0; yh = yl + .45
    S.box(g, "ladowisko", Q(c[0] - hw, c[1] - hw, yl + .1), a, UP, b, 2 * hw, .35, 2 * hw)
    S.box(g, "ladowisko_krzyz_tlo", Q(c[0] - 4, c[1] - 4, yh), a, UP, b, 8, .02, 8, bez=("-y",))
    for (u0, v0, du, dv) in ((c[0] - 3, c[1] - .8, 6, 1.6), (c[0] - .8, c[1] - 3, 1.6, 6)):
        S.box(g, "ladowisko_krzyz", Q(u0, v0, yh + .02), a, UP, b, du, .02, dv, bez=("-y",))
    for (p0, kx, dl) in ((Q(c[0] - hw, c[1] - hw, yh), a, 2 * hw), (Q(c[0] - hw, c[1] + hw - .4, yh), a, 2 * hw)):
        S.box(g, "ladowisko_krzyz", p0, kx, UP, b, dl, .02, .4, bez=("-y",))


def sor_front(S, g, f, H):
    """Front SOR: szary panel na środkowe 60 % ściany (od 2. kondygnacji do 1,5 m pod dachem, pole na napis — bez
    prawdziwej nazwy, ZASADY 7.4), parterowy przeszklony pawilon wejścia 7 m przed fasadą z daszkiem z blachy."""
    a, b, t, n, L = f
    u0, u1 = L * .2, L * .8
    S.box(g, "szpital_panel", a + t * u0 + UP * 6.0, t, UP, n, u1 - u0, H - 7.5, .18, bez=("-z",))
    S.box(g, "market_szklo", a + t * (u0 - 2) + UP * .0, t, UP, n, u1 - u0 + 4, 4.0, 7.0, bez=("-z",))
    S.box(g, "obrobka", a + t * (u0 - 3) + UP * 4.0, t, UP, n, u1 - u0 + 6, .3, 8.5, bez=("-z",))
    for uu in np.linspace(u0 - 2.5, u1 + 2.5, 6):
        S.rura(g, "stal", a + t * uu + n * 8.0, a + t * uu + n * 8.0 + UP * 4.0, .12, seg=8)


def market_front(S, g, f, H, att):
    """Front marketu (IMG_1577, IMG_1738–1742): podcień 3 m z białymi słupami co 6 m i stropem na +4,2, rama-pergola
    na dachu wzdłuż frontu (słupki co 5 m, belka), biała wieża w narożniku (pole na logo puste — nazwa decyzją
    właściciela), przeszklona klatka schodowa obok wieży, biały walec przy drugim końcu."""
    a, b, t, n, L = f
    yk = 4.2
    S.box(g, "market_elewacja", a + UP * yk, t, UP, n, L, .35, 3.0, bez=("-z",))
    for u in np.arange(3.0, L - 1.0, 6.0):
        S.rura(g, "pcv", a + t * u + n * 2.7, a + t * u + n * 2.7 + UP * yk, .22, seg=10)
    yp = H + att
    for u in np.arange(1.0, L - 6.0, 5.0):
        S.box(g, "market_rama", a + t * u - n * .9 + UP * yp, t, UP, n, .35, 2.1, .35)
    S.box(g, "market_rama", a + t * 1.0 - n * .9 + UP * (yp + 1.7), t, UP, n, L - 7.0, .45, .4)
    # wieża w narożniku (koniec b), przeszklona klatka obok, walec na drugim końcu
    S.box(g, "market_elewacja", b - t * 6.0 - n * 4.5, t, UP, n, 6.0, H + att + 6.5, 4.6)
    S.box(g, "market_niebieski", b - t * 5.8 + n * .1 + UP * 1.5, t, UP, n, 5.6, H + att + 4.5, .12)     # rama pylonu
    S.box(g, "pole_logo", b - t * 5.3 + n * .22 + UP * 2.0, t, UP, n, 4.6, H + att + 3.5, .06)          # puste pole na logo
    S.box(g, "market_szklo", b - t * 15.0 - n * 2.0, t, UP, n, 9.0, H + att + 1.5, 2.6)
    c = a + t * 3.0 - n * 1.0
    S.rura(g, "market_elewacja", c, c + UP * (H + att + .4), 3.0, seg=20)


def dach_plaski(S, g, P, y, att, mat):
    for tri in shapely.constrained_delaunay_triangles(P).geoms:
        c = [np.array([x, y, z]) for x, z in list(tri.exterior.coords)[:3]]
        if np.cross(c[1] - c[0], c[2] - c[0])[1] < 0: c[1], c[2] = c[2], c[1]
        S.tri_list(g, mat, c, (0., 1., 0.), (1., 0., 0.), [(p[0], p[2]) for p in c], None, [0, 1, 2])
    # attyka od środka i obróbka po obwodzie
    pts = list(P.exterior.coords)
    for (x0, z0), (x1, z1) in zip(pts, pts[1:]):
        a = np.array([x0, y + att, z0]); b = np.array([x1, y + att, z1]); L = float(np.linalg.norm(b - a))
        if L < .05: continue
        t = (b - a) / L; nn = np.cross(t, UP)
        if P.contains(shapely.geometry.Point(*(((a + b) / 2 + nn * .3)[[0, 2]]))): nn = -nn
        S.quad(g, "tynk_attyka", a - nn * .25 - UP * att, b - nn * .25 - UP * att, b - nn * .25, a - nn * .25, kier=-nn)
        S.box(g, "obrobka", a - t * .04 + nn * .04 - UP * .06, t, UP, -nn, L + .08, .08, .33)


def dach_koperta(S, g, P, y, mat, spadek):
    """Dach kopertowy nad prostokątem opisanym (pawilony): kalenica wzdłuż dłuższego boku, okap 0,4 m."""
    r = list(P.minimum_rotated_rectangle.exterior.coords)[:4]
    e1 = np.subtract(r[1], r[0]); e2 = np.subtract(r[2], r[1])
    if np.linalg.norm(e1) < np.linalg.norm(e2): r = r[1:] + r[:1]; e1, e2 = e2, e1
    L, W = np.linalg.norm(e1), np.linalg.norm(e2); a = e1 / L; b = e2 / W; o = np.array(r[0])
    ok = .4; h = (W / 2 + ok) * math.tan(math.radians(spadek))
    Q = lambda u, v, yy: np.array([*(o + a * u + b * v)][:1] + [yy] + [*(o + a * u + b * v)][1:])
    c0 = Q(-ok, -ok, y); c1 = Q(L + ok, -ok, y); c2 = Q(L + ok, W + ok, y); c3 = Q(-ok, W + ok, y)
    k0 = Q(W / 2, W / 2, y + h); k1 = Q(L - W / 2, W / 2, y + h)
    for tri in ((c0, c1, k1, k0), (c2, c3, k0, k1)):
        S.quad(g, mat, *tri, kier=UP)
    for (p, q, k) in ((c1, c2, k1), (c3, c0, k0)):
        nn = np.cross(q - p, k - p)
        if nn[1] < 0: p, q = q, p
        S.tri_list(g, mat, [p, q, k], nrm(np.cross(q - p, k - p)), nrm(q - p), [(0, 0), (np.linalg.norm(q - p), 0), (np.linalg.norm(q - p) / 2, h)], None, [0, 1, 2])
    # podbitka okapu
    S.quad(g, "obrobka", c0, c3, c2, c1, kier=-UP)
