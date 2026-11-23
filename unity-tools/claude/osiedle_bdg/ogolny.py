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
            if sp.get("okna_kier") and n[0] * sp["okna_kier"][0] + n[2] * sp["okna_kier"][1] > .5: fronty.append((a, b, t, n, L))
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
            if sp.get("pas_gorny"):                 # pas blachy pod dachem 5 cm przed licem (market: niebieski, IMG_1730)
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
        if sp.get("ladowisko"):                    # lądowisko na dachu SOR (IMG_1727): płyta wysunięta 1 m, 1,5 m nad attyką, słupy
            c0 = P.centroid; mr = list(P.minimum_rotated_rectangle.exterior.coords)[:2]
            ang = math.atan2(mr[1][1] - mr[0][1], mr[1][0] - mr[0][0]); ca, sa = math.cos(ang), math.sin(ang); hw = 12.0
            r = Polygon([(c0.x + ca * u - sa * v, c0.y + sa * u + ca * v) for u, v in ((-hw, -hw), (hw, -hw), (hw, hw), (-hw, hw))])
            yl = y + att + 1.5                      # płyta 24 x 24 m nad środkiem dachu (IMG_1727)
            for tri in shapely.constrained_delaunay_triangles(r).geoms:
                c = [np.array([x, yl, z]) for x, z in list(tri.exterior.coords)[:3]]
                if np.cross(c[1] - c[0], c[2] - c[0])[1] < 0: c[1], c[2] = c[2], c[1]
                S.tri_list(nazwa, "ladowisko", c, (0., 1., 0.), (1., 0., 0.), [(q[0], q[2]) for q in c], None, [0, 1, 2])
                d_ = [q - UP * .35 for q in c]; d_[1], d_[2] = d_[2], d_[1]
                S.tri_list(nazwa, "ladowisko", d_, (0., -1., 0.), (1., 0., 0.), [(q[0], q[2]) for q in d_], None, [0, 1, 2])
            pr = list(r.exterior.coords)
            for (x0, z0), (x1, z1) in zip(pr, pr[1:]):
                a_ = np.array([x0, yl - .35, z0]); b_ = np.array([x1, yl - .35, z1]); L_ = float(np.linalg.norm(b_ - a_))
                t_ = (b_ - a_) / L_; n_ = np.cross(t_, UP)
                if r.contains(shapely.geometry.Point(*(((a_ + b_) / 2 + n_ * .3)[[0, 2]]))): n_ = -n_
                S.quad(nazwa, "ladowisko", a_, b_, b_ + UP * .35, a_ + UP * .35, kier=n_)
                S.rura(nazwa, "stal", a_ + UP * 1.4, b_ + UP * 1.4, .03, seg=6)
            for q in list(r.buffer(-3.0, join_style=2).exterior.coords)[:4]:
                S.rura(nazwa, "stal", np.array([q[0], y, q[1]]), np.array([q[0], yl - .35, q[1]]), .18, seg=8)
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
    S.box(g, "market_elewacja", b - t * 5.5 - n * 4.5, t, UP, n, 5.5, H + att + 5.5, 4.6)
    S.box(g, "market_szklo", b - t * 9.5 - n * 4.0, t, UP, n, 4.0, H + att + 2.0, 4.0)
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
