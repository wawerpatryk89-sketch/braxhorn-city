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
        if sp.get("ladowisko"):                    # SOR: cofnięta kondygnacja, płyta lądowiska z siatką (IMG_1744–1753)
            Qd = sor_dach(S, nazwa, P, y, sp.get("front_kier", (0, -1)))
        if sp.get("sor") and fronty:
            sor_front(S, nazwa, max(fronty, key=lambda f: f[4]), H, sp.get("drogi"))
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


def _zewn(Q, a, b):
    """Normalna zewnętrzna boku a–b wielokąta Q (x, z)."""
    t = nrm(b - a); n = nrm(np.cross(UP, t))
    m = (a + b) / 2 + n * .3
    return t, (-n if Q.contains(shapely.geometry.Point(m[0], m[2])) else n)


def _boki(Q):
    pts = list(Q.exterior.coords)
    for (x0, z0), (x1, z1) in zip(pts, pts[1:]):
        a = np.array([x0, 0., z0]); b = np.array([x1, 0., z1]); L = float(np.linalg.norm(b - a))
        if L > .2:
            t, n = _zewn(Q, a, b); yield a, b, t, n, L


def _plyta(S, g, Q, y0, h, m_gora, m_bok, m_spod):
    """Płyta o obrysie Q (x, z) od y0 do y0+h: góra, spód, boki."""
    for tri in shapely.constrained_delaunay_triangles(Q).geoms:
        c = [np.array([x, 0., z]) for x, z in list(tri.exterior.coords)[:3]]
        if np.cross(c[1] - c[0], c[2] - c[0])[1] < 0: c[1], c[2] = c[2], c[1]
        S.tri_list(g, m_gora, [q + UP * (y0 + h) for q in c], (0., 1., 0.), (1., 0., 0.), [(q[0], q[2]) for q in c], None, [0, 1, 2])
        S.tri_list(g, m_spod, [q + UP * y0 for q in (c[0], c[2], c[1])], (0., -1., 0.), (1., 0., 0.), [(q[0], q[2]) for q in (c[0], c[2], c[1])], None, [0, 1, 2])
    for a, b, t, n, L in _boki(Q):
        S.quad(g, m_bok, a + UP * y0, b + UP * y0, b + UP * (y0 + h), a + UP * (y0 + h), t=t, kier=n)


def _przod(P, fk, glebokosc):
    """Część obrysu P do `glebokosc` m od najbardziej wysuniętego punktu w kierunku fk (front)."""
    f = np.array(fk, float); f /= np.linalg.norm(f); pr = np.array([-f[1], f[0]])
    s0 = max(float(np.dot(p, f)) for p in P.exterior.coords)
    A = f * (s0 + 20); B_ = f * (s0 - glebokosc)
    return shapely.geometry.Polygon([tuple(A + pr * 900), tuple(A - pr * 900), tuple(B_ - pr * 900), tuple(B_ + pr * 900)])


def sor_dach(S, g, P, y, fk):
    """Dach SOR (IMG_1727, IMG_1744–1753, lotnicze IMG_1751, zdjęcie z internetu w 17:27): nad przednią częścią budynku
    cofnięta o 2 m kondygnacja techniczna — ciemne przeszklenie z pionowymi szprosami, nad nią płyta lądowiska
    wysunięta 1,2 m poza obrys, jasna; po obwodzie siatka bezpieczeństwa z lameli odchylonych na zewnątrz (to, co z ulicy
    wygląda jak „grzebień”). Oznakowanie lądowiska przyszpitalnego wg ICAO zał. 14: biały krzyż z czerwonym H, biała
    obwódka pola, żółty okrąg przyziemienia. Tylna część budynku zostaje z płaskim dachem z papy."""
    przod = _przod(P, fk, 46.0)
    Qc = P.buffer(-2.0, join_style=2).intersection(przod)
    if Qc.geom_type != "Polygon": Qc = max(Qc.geoms, key=lambda q: q.area)
    Qc = Qc.simplify(.2)
    hc = 3.4
    for a, b, t, n, L in _boki(Qc):                       # ściany kondygnacji technicznej: szkło + szprosy co 1,5 m
        S.quad(g, "market_szklo", a + UP * y, b + UP * y, b + UP * (y + hc), a + UP * (y + hc), t=t, kier=n)
        for u in np.arange(.75, L - .3, 1.5):
            S.box(g, "stal", a + t * (u - .04) + UP * y, t, UP, n, .08, hc, .1, bez=("-z", "-y", "+y"))
        S.box(g, "obrobka", a + UP * y, t, UP, n, L, .35, .12, bez=("-z", "-y"))
    yd = y + hc
    Qd = P.buffer(1.2, join_style=2).intersection(przod)
    if Qd.geom_type != "Polygon": Qd = max(Qd.geoms, key=lambda q: q.area)
    Qd = Qd.simplify(.3)
    _plyta(S, g, Qd, yd, .5, "ladowisko", "szpital_bialy", "beton_daszek")
    yt = yd + .5
    # siatka bezpieczeństwa: lamele co 0,5 m, 1,5 m, odchylone 25° na zewnątrz, rama zewnętrzna
    for a, b, t, n, L in _boki(Qd):
        d = nrm(n * math.cos(math.radians(25)) + UP * math.sin(math.radians(25)))
        a0 = a + UP * (yt - .05)
        for u in np.arange(.25, L, .5):
            S.box(g, "lamele", a0 + t * (u - .02), t, d, n, .04, 1.5, .02, bez=("-y",))
        S.rura(g, "lamele", a0 + d * 1.5, a0 + t * L + d * 1.5, .035, seg=6)
    # oznakowanie na środku płyty
    c = np.array([Qd.centroid.x, 0., Qd.centroid.y]); f = np.array([fk[0], 0., fk[1]], float); f = nrm(f); r = nrm(np.cross(UP, f))
    m = min(14.0, math.sqrt(Qd.area) / 2 - 3.0)
    Kw = lambda u, v, du, dv, mat, dy: S.box(g, mat, c + r * u + f * v + UP * (yt + dy), r, UP, f, du, .012, dv, bez=("-y",))
    for (u0, v0, du, dv) in ((-m, -m, 2 * m, .5), (-m, m - .5, 2 * m, .5), (-m, -m + .5, .5, 2 * m - 1), (m - .5, -m + .5, .5, 2 * m - 1)):
        Kw(u0, v0, du, dv, "ladowisko_krzyz", .01)
    R0, R1 = m * .72, m * .72 + .9                             # żółty okrąg przyziemienia
    kat = np.linspace(0, 2 * math.pi, 49)
    for f0, f1 in zip(kat, kat[1:]):
        P_ = lambda rr, q: c + r * (rr * math.cos(q)) + f * (rr * math.sin(q)) + UP * (yt + .014)
        S.quad(g, "ladowisko_okrag", P_(R0, f0), P_(R0, f1), P_(R1, f1), P_(R1, f0), kier=UP)
    k = m * .45; w = k * .42                                  # biały krzyż (ramiona szer. 0,84k) z czerwonym H
    Kw(-k, -w, 2 * k, 2 * w, "ladowisko_krzyz", .02)
    Kw(-w, -k, 2 * w, k - w, "ladowisko_krzyz", .02); Kw(-w, w, 2 * w, k - w, "ladowisko_krzyz", .02)
    hh, hw = w * .8, w * .6; sw = .3 * hw                    # czerwone H w środku krzyża
    Kw(-hw, -hh, sw, 2 * hh, "ladowisko_krzyz_tlo", .03); Kw(hw - sw, -hh, sw, 2 * hh, "ladowisko_krzyz_tlo", .03)
    Kw(-hw + sw, -sw / 2, 2 * hw - 2 * sw, sw, "ladowisko_krzyz_tlo", .03)
    return Qd


def sor_front(S, g, f, H, sp_drogi=None):
    """Front SOR (IMG_1727, IMG_1744–1751): dwa kremowe skrzydła wysunięte 2,5 m na pełną wysokość, między nimi
    wklęsła, łukowa ściana z szarego panelu (bez prawdziwej nazwy, ZASADY 7.4) i pasem okien nad nią; w parterze
    przeszklony pawilon wejścia pod ciemnym dachem czterospadowym ściętym u góry; przed nim podjazd dla karetek —
    pętla z asfaltu między betonowymi murkami wokół ogrodu (lotnicze IMG_1751)."""
    a, b, t, n, L = f
    w = min(6.0, L * .24); wys = 2.5
    for u in (0.0, L - w):                                    # skrzydła
        S.box(g, "szpital_krem", a + t * u - UP * .25, t, UP, n, w, H + .85, wys, bez=("-z",))
        S.box(g, "obrobka", a + t * (u - .04) + UP * (H + .6) - n * .04, t, UP, n, w + .08, .08, wys + .12)
    u0, u1 = w, L - w; um = (u0 + u1) / 2; pol = (u1 - u0) / 2
    d = lambda u: .25 + (wys - .25) * ((u - um) / pol) ** 2     # wklęsły łuk: przy skrzydłach 2,5 m, w środku 0,25 m
    us = np.linspace(u0, u1, 25)
    dd = lambda u: 2 * (wys - .25) * (u - um) / pol ** 2          # pochodna łuku
    luk = np.concatenate([[0.], np.cumsum([math.hypot(q - p, d(q) - d(p)) for p, q in zip(us, us[1:])])])
    for (v0, v1, mat) in ((7.2, H - 3.4, "szpital_panel"), (H - 3.4, H - 1.6, "market_szklo"), (H - 1.6, H + .6, "szpital_krem")):
        pts, nn, tt, uv = [], [], [], []
        for k_, u in enumerate(us):
            T_ = nrm(t + n * dd(u)); N_ = nrm(n - t * dd(u))
            for v in (v0, v1):
                pts.append(a + t * u + n * d(u) + UP * v); nn.append(N_); tt.append(T_); uv.append((luk[k_], v))
        idx = []
        for k_ in range(len(us) - 1):
            i0, i1, i2, i3 = 2 * k_, 2 * k_ + 2, 2 * k_ + 3, 2 * k_ + 1
            if np.dot(np.cross(pts[i1] - pts[i0], pts[i3] - pts[i0]), nn[i0]) < 0: idx += [i0, i3, i2, i0, i2, i1]
            else: idx += [i0, i1, i2, i0, i2, i3]
        S.tri_list(g, mat, pts, nn, tt, uv, None, idx)
    for p, q in zip(us, us[1:]):                              # spód i góra łuku
        A = a + t * p; B_ = a + t * q
        for yy, kk in ((7.2, -UP), (H + .6, UP)):
            S.quad(g, "obrobka" if kk[1] > 0 else "szpital_krem", A + UP * yy, B_ + UP * yy, B_ + n * d(q) + UP * yy, A + n * d(p) + UP * yy, kier=kk)
    for p in np.linspace(u0 + 1.2, u1 - 1.2, 6):                # szprosy pasa okien
        S.box(g, "stal", a + t * (p - .04) + n * (d(p) + .02) + UP * (H - 3.4), t, UP, n, .08, 1.8, .06)
    # przeszklony pawilon wejścia
    S.box(g, "market_szklo", a + t * (u0 - 1) - UP * .25, t, UP, n, u1 - u0 + 2, 4.25, 7.0, bez=("-z",))
    for p in np.linspace(u0 - 1, u1 + 1, 7):
        S.box(g, "stal", a + t * (p - .05) + n * 7.0, t, UP, n, .1, 4.0, .06)
    # ciemny dach czterospadowy ścięty u góry, okap na +5,0, szczyt +6,1, wysunięty 9,5 m
    x0, x1, zz = u0 - 3.5, u1 + 3.5, 9.5; y0, y1, ins = 5.0, 6.1, 2.6
    E = lambda u, v, yy: a + t * u + n * v + UP * yy
    e0, e1, e2, e3 = E(x0, 0, y0), E(x1, 0, y0), E(x1, zz, y0), E(x0, zz, y0)
    g0, g1, g2, g3 = E(x0 + ins, 0, y1), E(x1 - ins, 0, y1), E(x1 - ins, zz - ins, y1), E(x0 + ins, zz - ins, y1)
    S.quad(g, "dach_ciemny", e3, e2, g2, g3, kier=nrm(n + UP))
    S.quad(g, "dach_ciemny", e2, e1, g1, g2, kier=nrm(t + UP))
    S.quad(g, "dach_ciemny", e0, e3, g3, g0, kier=nrm(-t + UP))
    S.quad(g, "dach_ciemny", g0, g3, g2, g1, kier=UP)
    S.quad(g, "beton_daszek", e0, e1, e2, e3, kier=-UP)
    for (p, q, nn) in ((e3, e2, n), (e2, e1, t), (e0, e3, -t)):        # blenda okapu 0,35 m
        S.quad(g, "obrobka", p - UP * .35, q - UP * .35, q, p, kier=nn)
    for uu in np.linspace(x0 + .5, x1 - .5, 6):
        S.rura(g, "stal", E(uu, zz - .6, 0), E(uu, zz - .6, y0 - .35), .14, seg=8)
    # pod dachem jezdnia dojazdowa z asfaltu do samych drzwi (nie trawnik, nie drzewa) — teren wycięty
    S.quad(g, "asfalt", E(x0, -.05, .005), E(x1, -.05, .005), E(x1, 9.6, .005), E(x0, 9.6, .005), kier=UP)
    S.wyciecia = getattr(S, "wyciecia", []) + [shapely.geometry.Polygon([(q[0], q[2]) for q in (E(x0, -.05, 0), E(x1, -.05, 0), E(x1, 9.6, 0), E(x0, 9.6, 0))])]
    # podjazd: półpierścień R 14–20 m, murki 1,0 m po obu stronach (przerwy tam, gdzie dochodzą drogi z OSM), ogród w środku;
    # teren pod podjazdem i ogrodem wycięty (S.wyciecia -> osiedle.MODELE), żeby jezdnie i pasy nie prześwitywały
    c = a + t * (L / 2) + n * 9.5; R0, R1 = 14.0, 20.0
    kat = np.linspace(0, math.pi, 28)
    P_ = lambda r, f_: c + t * (r * math.cos(f_)) + n * (r * math.sin(f_))
    drogi = sp_drogi or []
    przy_drodze = lambda q: any(dr.distance(shapely.geometry.Point(q[0], q[2])) < 3.2 for dr in drogi)
    for f0, f1 in zip(kat, kat[1:]):
        S.quad(g, "asfalt", P_(R0 - .25, f0) + UP * .005, P_(R0 - .25, f1) + UP * .005, P_(R1 + .25, f1) + UP * .005, P_(R1 + .25, f0) + UP * .005, kier=UP)
        for r in (R0 - .25, R1):
            A_, B_ = P_(r, f0), P_(r, f1); tt = nrm(B_ - A_)
            if przy_drodze((A_ + B_) / 2): continue
            S.box(g, "beton_daszek", A_ - tt * .02, tt, UP, nrm(np.cross(tt, UP)), float(np.linalg.norm(B_ - A_)) + .04, .95, .25, bez=("-y",))
        A_, B_ = P_(R0 - .25, f0) + UP * .08, P_(R0 - .25, f1) + UP * .08
        tri = [c + UP * .08, A_, B_]
        if np.cross(tri[1] - tri[0], tri[2] - tri[0])[1] < 0: tri = [tri[0], tri[2], tri[1]]
        S.tri_list(g, "trawa", tri, (0., 1., 0.), (1., 0., 0.), [(q[0], q[2]) for q in tri], None, [0, 1, 2])
    S.wyciecia = getattr(S, "wyciecia", []) + [shapely.geometry.Polygon([(q[0], q[2]) for q in [P_(R1 + .25, f_) for f_ in kat] + [c]]).buffer(0)]


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
    for u in np.arange(4.5, L - 17.0, 1.6):                  # żółte słupki przed podcieniem (zdjęcia 17:25, IMG_1761–1762)
        S.rura(g, "slupek_zolty", a + t * u + n * 3.45, a + t * u + n * 3.45 + UP * .9, .08, seg=8)


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
