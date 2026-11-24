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

# materiały okna na czas jednego budynku (sp["osc_mat"], sp["rama_mat"]); spichrze: ciemne drewno zamiast białych opasek
STYL = {"opaska": "opaska", "rama": "pcv"}


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
    stary_styl = dict(STYL)
    STYL["opaska"] = sp.get("osc_mat", "opaska"); STYL["rama"] = sp.get("rama_mat", "pcv")
    try:
        P = poly.buffer(0)
        if P.geom_type == "MultiPolygon": P = max(P.geoms, key=lambda q: q.area)
        P = shapely.geometry.polygon.orient(P.simplify(.15), 1.0)          # przeciwnie do zegara: na zewnątrz po prawej
        pts = list(P.exterior.coords); fronty = []
        rozst, ow, oh, opar = sp.get("okna", (3.0, 1.4, 1.4, .9))
        Lmax_front = 0.0
        if sp.get("market"):                       # najdłuższy front: tam drzwi do hali sklepu (BraxSupermarket.WOsiedlu)
            fk_ = sp.get("okna_kier")
            for (x0_, z0_), (x1_, z1_) in zip(pts, pts[1:]):
                a_ = np.array([x0_, 0., z0_]); b_ = np.array([x1_, 0., z1_]); L_ = float(np.linalg.norm(b_ - a_))
                if L_ < .3: continue
                t_ = (b_ - a_) / L_; n_ = nrm(np.cross(UP, t_))
                if P.contains(shapely.geometry.Point(*(((a_ + b_) / 2 + n_ * .5)[[0, 2]]))): n_ = -n_
                if n_[0] * fk_[0] + n_[2] * fk_[1] > .5: Lmax_front = max(Lmax_front, L_)
        wolne = []                                  # krawędzie obrysu bez sąsiada (dach kamienicy: okap tylko na nich)
        ulica_kr = []                               # krawędzie od ulicy/placu (lukarny, ozdoby)
        for (x0, z0), (x1, z1) in zip(pts, pts[1:]):
            a = np.array([x0, 0., z0]); b = np.array([x1, 0., z1]); L = float(np.linalg.norm(b - a))
            wolne.append(True)
            if L < .3: ulica_kr.append(False); continue
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
            sl = sp.get("slepe")                     # kamienice w pierzei: ściany przy sąsiednim budynku bez okien i gzymsów
            slepy = (lambda u: sl.contains(shapely.geometry.Point(*((a + t * u + n * .8)[[0, 2]])))) if sl is not None else (lambda u: False)
            udz_slepe = sum(slepy(u) for u in np.linspace(.3, L - .3, 5)) / 5 if sl is not None else 0.0
            wolne[-1] = udz_slepe < .5
            ul_w = sp.get("ulica_geom")
            pub = ul_w is None or ul_w.contains(shapely.geometry.Point(*(((a + b) / 2 + n * 7.0)[[0, 2]])))
            ulica_kr.append(pub and udz_slepe < .5)
            oz = sp.get("ozdoby") if (pub and udz_slepe < .5) else None
            wyk_osie, balk_os, k, off = set(), None, 0, 0.0
            if L >= 2.2 and sp.get("okna_on", True) and (kier_ok is None or n[0] * kier_ok[0] + n[2] * kier_ok[1] > .5):
                k = int((L - .8) // rozst)
                if oz and k >= 3 and kond >= 3 and oz.get("wykusz"):         # wykusz na środkowych osiach (1 albo 2)
                    wyk_osie = {k // 2 - 1, k // 2} if k % 2 == 0 else {k // 2}
                elif oz and k >= 2 and kond >= 2 and oz.get("balkon"):
                    balk_os = k // 2
                if k >= 1:
                    off = (L - k * rozst) / 2
                    for i in range(k):
                        c = off + (i + .5) * rozst
                        if slepy(c - rozst / 2 + .2) or slepy(c) or slepy(c + rozst / 2 - .2): continue
                        ul_ = sp.get("ulica_geom")             # kamienice: od ulicy/placu pełne okno, od podwórza uproszczone
                        od_ulicy = ul_ is None or ul_.contains(shapely.geometry.Point(*((a + t * c + n * 7.0)[[0, 2]])))
                        rodz = ("okno_l" if od_ulicy else "okno_t") if sp.get("okno_lekkie") else "okno"
                        for f in range(kond):
                            fl = 0 if f == 0 else p0 + (f - 1) * hk
                            if f == 0 and sp.get("witryny") and od_ulicy:
                                drzwi = sp.get("market") and abs(L - Lmax_front) < .01 and abs(c - L / 2) < rozst / 2
                                rw = "otwor" if drzwi else ("witryna_k" if sp.get("okno_lekkie") else "witryna")
                                okna.append((c - rozst / 2 + .15, c + rozst / 2 - .15, .45 if not drzwi else cok + .02, min(p0 - .5, 3.0), rw, 0))
                            elif f >= 1 and i in wyk_osie:
                                continue                           # okno na froncie wykusza (ozdoby_sciany)
                            elif f == 1 and i == balk_os:              # drzwi balkonowe
                                okna.append((c - ow / 2, c + ow / 2, fl + .05, fl + opar + oh, rodz, f))
                            elif sp.get("drzwi_bal") and (f, i) == sp["drzwi_bal"][:2] and k == sp["drzwi_bal"][2] and \
                                    n[0] * sp["drzwi_bal"][3][0] + n[2] * sp["drzwi_bal"][3][1] > .8:
                                # 24.11: drzwi na balkon z DJ-em (rynek_bdg/balkon_dj.py) — kondygnacja, oś, liczba osi, ściana
                                okna.append((c - ow / 2, c + ow / 2, fl + .05, fl + opar + oh, rodz, f))
                            else:
                                okna.append((c - ow / 2, c + ow / 2, fl + opar, fl + opar + oh, rodz, f))
            cok = sp.get("h_cokol", .5)
            S.sciana(nazwa, sp.get("cokol", "cokol4"), a + UP * -.25, t, L, cok + .25,
                     [(u0, u1, .25 + .12, cok + .25) for (u0, u1, v0, v1, r, pp) in okna if r == "otwor"])   # próg wejścia na posadzce
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
            elif sp.get("parter_mat") and pub and udz_slepe < .5 and p0 - cok > 1.0:   # boniowany parter od ulicy
                hp = p0 - cok - .1
                sciana(nazwa, sp["parter_mat"], a + UP * cok, t, n, L, hp, [o_ for o_ in gora if o_[3] <= hp + 1e-6], (L, hp))
                sciana(nazwa, sp["tynk"], a + UP * (cok + hp), t, n, L, H + att - cok - hp,
                       [(u0, u1, v0 - hp, v1 - hp, r, pp) for (u0, u1, v0, v1, r, pp) in gora if v0 >= hp - 1e-6], (L, H + att))
            elif udz_slepe >= .5 and sp.get("szczyt_mat"):         # ściana ogniowa przy sąsiedzie: stara cegła (widać ją nad niższym)
                sciana(nazwa, sp["szczyt_mat"], a + UP * cok, t, n, L, H + att - cok, gora, (L, H + att))
            else:
                sciana(nazwa, sp["tynk"], a + UP * cok, t, n, L, H + att - cok, gora, (L, H + att), "szklo" if sp.get("market") else None)
            if sp.get("mur_pruski") and L >= 1.0:
                mur_pruski(S, nazwa, a, t, n, L, okna, p0, hk, kond, H + att, rozst, off, k)
            if oz:
                ozdoby_sciany(S, nazwa, a, t, n, L, okna, sp, oz, p0, hk, kond, H, att, rozst, ow, oh, opar,
                              [off + (i + .5) * rozst for i in sorted(wyk_osie)] if wyk_osie else None,
                              (off + (balk_os + .5) * rozst) if balk_os is not None else None,
                              [off + i * rozst for i in range(1, k)] if L >= 2.2 and k >= 2 else [])
            if sp.get("gzymsy") and L > 1.0 and udz_slepe < .5:   # kamienice (Stary Rynek): gzymsy międzykondygnacyjne i wieńczący
                dz_ = .02 * (len(fronty) % 2)            # sąsiednie boki na innej wysokości o 2 cm — bez wspólnych płaszczyzn w narożach
                for k in range(kond - 1):                # linie stropów: nad parterem i nad każdym piętrem poza ostatnim
                    yk_ = p0 + k * hk
                    S.box(nazwa, sp.get("gzyms_mat", "opaska"), a + UP * (yk_ - .12 + dz_) - t * .06, t, UP, n, L + .12, .22, .1, bez=("-z",))
                S.box(nazwa, sp.get("gzyms_mat", "opaska"), a + UP * (H + att - .55 + dz_) - t * .25, t, UP, n, L + .5, .45, .32, bez=("-z",))
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
        if sp.get("market"):                       # posadzka w całym obrysie (+0,12) — wejście i hala sklepu stoją na niej
            for tri in shapely.constrained_delaunay_triangles(P).geoms:
                c3 = [np.array([x, .12, z]) for x, z in list(tri.exterior.coords)[:3]]
                if np.cross(c3[1] - c3[0], c3[2] - c3[0])[1] < 0: c3[1], c3[2] = c3[2], c3[1]
                S.tri_list(nazwa, "posadzka", c3, (0., 1., 0.), (1., 0., 0.), [(q[0], q[2]) for q in c3], None, [0, 1, 2])
        if sp.get("market") and fronty:
            gl = max(fronty, key=lambda f: f[4])
            for k_, f_ in enumerate(sorted(fronty, key=lambda f: -f[4])):
                if f_[4] >= 8.0: market_front(S, nazwa, f_, H, att, f_ is gl, k_)
            # przedsionek: od drzwi w witrynie do frontu hali sklepu (4,8 m w głąb) — ściany i strop widoczne od środka
            # (ściany budynku są jednostronne; bez tego z przedsionka widać przez nie na zewnątrz)
            a_, b_, t_, n_, L_ = gl; u0, u1, gl_, hh = L_ / 2 - 1.8, L_ / 2 + 1.8, 4.8, 3.3
            Pq = lambda u, d, y: a_ + t_ * u - n_ * d + UP * y
            S.quad(nazwa, "kiosk_bialy", Pq(u0, .05, .12), Pq(u0, gl_, .12), Pq(u0, gl_, hh), Pq(u0, .05, hh), kier=t_)
            S.quad(nazwa, "kiosk_bialy", Pq(u1, .05, .12), Pq(u1, gl_, .12), Pq(u1, gl_, hh), Pq(u1, .05, hh), kier=-t_)
            S.quad(nazwa, "posadzka", Pq(u0, .05, hh), Pq(u1, .05, hh), Pq(u1, gl_, hh), Pq(u0, gl_, hh), kier=-UP)
            S.quad(nazwa, "kiosk_bialy", Pq(u0, .05, hh), Pq(u1, .05, hh), Pq(u1, .05, 4.4), Pq(u0, .05, 4.4), kier=-n_)
        if sp.get("ladowisko"):                    # SOR: cofnięta kondygnacja, płyta lądowiska z siatką (IMG_1744–1753)
            Qd = sor_dach(S, nazwa, P, y, sp.get("front_kier", (0, -1)))
        if sp.get("sor") and fronty:
            sor_front(S, nazwa, max(fronty, key=lambda f: f[4]), H, sp.get("drogi"))
        if sp.get("dach") == "kamienica":
            # obrys wklęsły (kamienica z oficyną, L/U): części wypukłe (Hertel–Mehlhorn), każda z własnym dachem;
            # krawędź wewnętrzna podziału = szczyt (jak przy sąsiedzie). Wypukła część: połacie bez „nurkowania” pod okap.
            ex = list(P.exterior.coords)
            def wolna_w(m_):
                for k_, ((xa, za), (xb, zb)) in enumerate(zip(ex, ex[1:])):
                    if shapely.geometry.LineString([(xa, za), (xb, zb)]).distance(shapely.geometry.Point(m_)) < .05: return wolne[k_]
                return False
            for Qc in rozklad_wypukly(P):
                Qc = shapely.geometry.polygon.orient(Qc, 1.0); eq = list(Qc.exterior.coords)
                wq = [wolna_w(((xa + xb) / 2, (za + zb) / 2)) for (xa, za), (xb, zb) in zip(eq, eq[1:])]
                uq = [False] * len(wq)
                for kq, ((xa, za), (xb, zb)) in enumerate(zip(eq, eq[1:])):
                    for k_, ((xc, zc), (xd, zd)) in enumerate(zip(ex, ex[1:])):
                        if k_ < len(ulica_kr) and ulica_kr[k_] and shapely.geometry.LineString([(xc, zc), (xd, zd)]).distance(shapely.geometry.Point(((xa + xb) / 2, (za + zb) / 2))) < .05:
                            uq[kq] = True
                if sp.get("dwuspad"):                       # spichrze: dach dwuspadowy — krótkie boki to szczyty
                    dl = [np.hypot(xb - xa, zb - za) for (xa, za), (xb, zb) in zip(eq, eq[1:])]
                    wq = [w_ and d_ > .7 * max(dl) for w_, d_ in zip(wq, dl)]
                dach_kamienica(S, nazwa, Qc, wq, H + att + .02, sp.get("dach_mat", "dachowka"), sp.get("spadek", 42), sp["tynk"],
                               sp.get("kalenica_max", 7.5), uq if sp.get("lukarny") else None, rozst, sp.get("kominy", True),
                               sp.get("szczyt_mat") or sp["tynk"], "belka_ciemna" if sp.get("mur_pruski") else None, sp.get("lukarny_rzedy", 1))
        elif sp.get("dach", "plaski") == "koperta":
            dach_koperta(S, nazwa, P, y + .05, sp.get("dach_mat", "dachowka"), sp.get("spadek", 28))
        else:
            dach_plaski(S, nazwa, P, y, att, sp.get("dach_mat", "papa"))
    finally:
        B.S, B.okno_nr[0], B.POZIOM0, B.KOND = stare
        STYL.update(stary_styl)
    return S


def sciana(g, mat, o, t, n, L, H, okna, uv1, styl=None):
    """Ściana z oknami (te same elementy co blok 32a). styl="szklo": market — witryny i okna jako szkło w białej
    ramie aluminiowej ze szprosami, ościeża 12 cm, bez firanek (zdjęcia nocne 18:47)."""
    S = B.S
    if styl == "szklo":
        S.sciana(g, mat, o, t, L, H, [(u0, u1, v0, v1) for (u0, u1, v0, v1, r, p) in okna], uv1_skala=uv1)
        P = lambda u, v, d=0.0: o + t * u + UP * v - n * d
        for (u0, u1, v0, v1, r, pp) in okna:
            g_ = .12
            for q, kk in (((P(u0, v0), P(u0, v1), P(u0, v1, g_), P(u0, v0, g_)), t), ((P(u1, v0), P(u1, v1), P(u1, v1, g_), P(u1, v0, g_)), -t),
                          ((P(u0, v1), P(u1, v1), P(u1, v1, g_), P(u0, v1, g_)), -UP), ((P(u0, v0), P(u1, v0), P(u1, v0, g_), P(u0, v0, g_)), UP)):
                S.quad(g, mat, *q, kier=kk)
            if r == "otwor":                       # wejście: bez szyby, tylko obramienie (przejście do hali sklepu)
                S.box(g, "market_rama", P(u0 - .03, v1, .02), t, UP, n, u1 - u0 + .06, .08, .1); continue
            S.quad(g, "market_szklo", P(u0, v0, g_), P(u1, v0, g_), P(u1, v1, g_), P(u0, v1, g_), t=t, kier=n)
            fr = "market_rama"; d0 = g_ - .02
            for (a_, b_) in (((u0, v0), (u1, v0)), ((u0, v1), (u1, v1))):
                S.box(g, fr, P(a_[0], a_[1] - .03, d0) + n * 0, t, UP, n, b_[0] - a_[0], .06, .06)
            for u in [u0, u1] + list(np.arange(u0, u1, 1.25 if r == "witryna" else 1.5))[1:]:
                if u1 - u < .3 and u != u1: continue
                S.box(g, fr, P(u - .03, v0, d0), t, UP, n, .06, v1 - v0, .06)
            if r == "witryna": S.box(g, fr, P(u0, v0 + 2.35, d0), t, UP, n, u1 - u0, .06, .06)          # nadświetle
        return
    dz = [(u0 - B.OPASKA, u1 + B.OPASKA, v0, v1 + B.OPASKA) for (u0, u1, v0, v1, r, p) in okna if r not in ("witryna", "okno_t", "witryna_k")] + \
         [(u0, u1, v0, v1) for (u0, u1, v0, v1, r, p) in okna if r in ("witryna", "okno_t", "witryna_k")]
    S.sciana(g, mat, o, t, L, H, dz, uv1_skala=uv1)
    P = lambda u, v, d=0.0: o + t * u + UP * v - n * d
    for (u0, u1, v0, v1, r, pp) in okna:
        if r == "witryna_k":                                       # kamienica: witryna sklepu/lokalu na parterze
            witryna_kamienica(g, P, t, n, u0, u1, v0, v1, pp); continue
        if r == "okno_t":                                          # od podwórza: ościeże, parapet, szyba — bez opaski
            okno_lekkie(g, P, t, n, u0, u1, v0, v1, pp, False); continue
        if r != "witryna":
            for aa, bb, cc, dd in (((u0 - B.OPASKA, v0), (u0, v0), (u0, v1 + B.OPASKA), (u0 - B.OPASKA, v1 + B.OPASKA)),
                                   ((u1, v0), (u1 + B.OPASKA, v0), (u1 + B.OPASKA, v1 + B.OPASKA), (u1, v1 + B.OPASKA)),
                                   ((u0, v1), (u1, v1), (u1, v1 + B.OPASKA), (u0, v1 + B.OPASKA))):
                S.quad(g, STYL["opaska"], P(*aa), P(*bb), P(*cc), P(*dd), t=t, kier=n)
            if r == "okno_l": okno_lekkie(g, P, t, n, u0, u1, v0, v1, pp, True)
            else: B.okno(g, o, t, n, u0, u1, v0, v1, "okno", pp)
        else:
            B.okno(g, o, t, n, u0, u1, v0, v1, "klatka", 0)        # witryna: jedno pole, bez parapetu mieszkaniowego


def okno_lekkie(g, P, t, n, u0, u1, v0, v1, pietro, rama):
    """Okno kamienicy (Stary Rynek, zdjęcia ref_stare): ościeże 12 cm, parapet blaszany (góra + czoło), rama w kształcie T
    (słupek + ślemię z naświetlem, typowe okno skrzynkowe kamienic) tylko licem, szyba 3 cm za ramą. ~30 trójkątów zamiast
    ~80 okna bloku 32a — kamienic jest 249 i ~25 tys. okien. rama=False (podwórze): bez ramy, sama szyba w ościeżu."""
    S = B.S; G = B.GL_OSC; w = B.RAMA
    S.quad(g, STYL["opaska"], P(u0, v1, 0), P(u0, v1, G), P(u1, v1, G), P(u1, v1, 0), t=t, kier=-UP)
    S.quad(g, STYL["opaska"], P(u0, v0, 0), P(u0, v1, 0), P(u0, v1, G), P(u0, v0, G), kier=t)
    S.quad(g, STYL["opaska"], P(u1, v0, 0), P(u1, v0, G), P(u1, v1, G), P(u1, v1, 0), kier=-t)
    S.quad(g, "parapet", P(u0 - .03, v0, -.05), P(u1 + .03, v0, -.05), P(u1 + .03, v0, G), P(u0 - .03, v0, G), t=t, kier=UP)
    S.quad(g, "parapet", P(u0 - .03, v0 - .03, -.05), P(u1 + .03, v0 - .03, -.05), P(u1 + .03, v0, -.05), P(u0 - .03, v0, -.05), t=t, kier=n)
    d0 = G; vs = v0 + (v1 - v0) * .72; us = (u0 + u1) / 2
    if rama:
        pasy = [((u0, v0), (u1, v0 + w)), ((u0, v1 - w), (u1, v1)), ((u0, v0 + w), (u0 + w, v1 - w)), ((u1 - w, v0 + w), (u1, v1 - w)),
                ((us - .035, v0 + w), (us + .035, vs - .035)), ((u0 + w, vs - .035), (u1 - w, vs + .035))]
        for (a_, b_) in pasy:
            S.quad(g, STYL["rama"], P(a_[0], a_[1], d0), P(b_[0], a_[1], d0), P(b_[0], b_[1], d0), P(a_[0], b_[1], d0), t=t, kier=n)
    fl = B.POZIOM0 + pietro * B.KOND; k = B.okno_nr[0]; W = u1 - u0
    RW = max(W, 2.2) + .8; ru0 = (u0 + u1) / 2 - RW / 2
    gx0, gx1, gy0, gy1 = u0 + .02, u1 - .02, v0 + .02, v1 - .02
    uv1 = [((x - ru0) / RW + k, (y - fl) / B.KOND + pietro) for x, y in ((gx0, gy0), (gx1, gy0), (gx1, gy1), (gx0, gy1))]
    S.quad(g, "okno", P(gx0, gy0, d0 + .03), P(gx1, gy0, d0 + .03), P(gx1, gy1, d0 + .03), P(gx0, gy1, d0 + .03), uv1=uv1, t=t, kier=n)
    B.okno_nr[0] += 1


def witryna_kamienica(g, P, t, n, u0, u1, v0, v1, pietro):
    """Witryna lokalu w parterze kamienicy (zdjęcia ref_stare: Rynek, Mostowa, Focha): ościeże 20 cm, cokolik pod szybą
    z płyty granitowej, rama drewniana/aluminiowa w kolorze grafitu z naświetlem 0,5 m i jednym słupkiem, szyba z wnętrzem
    lokalu (ten sam materiał „okno” co mieszkania — pokój za szybą zamiast pasków klatki schodowej)."""
    S = B.S; G = .20; w = .07; d0 = G - .06
    S.quad(g, "opaska", P(u0, v1, 0), P(u0, v1, G), P(u1, v1, G), P(u1, v1, 0), t=t, kier=-UP)
    S.quad(g, "opaska", P(u0, v0, 0), P(u0, v1, 0), P(u0, v1, G), P(u0, v0, G), kier=t)
    S.quad(g, "opaska", P(u1, v0, 0), P(u1, v0, G), P(u1, v1, G), P(u1, v1, 0), kier=-t)
    S.quad(g, "cokol4", P(u0, v0, 0), P(u1, v0, 0), P(u1, v0, G), P(u0, v0, G), t=t, kier=UP)          # próg / podokiennik
    vt = v1 - .55; us = (u0 + u1) / 2
    pasy = [((u0, v0), (u1, v0 + w)), ((u0, v1 - w), (u1, v1)), ((u0, v0 + w), (u0 + w, v1 - w)), ((u1 - w, v0 + w), (u1, v1 - w)),
            ((u0 + w, vt - w / 2), (u1 - w, vt + w / 2)), ((us - w / 2, v0 + w), (us + w / 2, vt - w / 2))]
    for (a_, b_) in pasy:
        S.quad(g, "rama_witryny", P(a_[0], a_[1], d0), P(b_[0], a_[1], d0), P(b_[0], b_[1], d0), P(a_[0], b_[1], d0), t=t, kier=n)
    for (a_, b_, kk) in (((u0 + w, v0 + w), (u1 - w, v0 + w), UP), ((u0 + w, v1 - w), (u1 - w, v1 - w), -UP)):   # grubość ramy
        S.quad(g, "rama_witryny", P(a_[0], a_[1], d0), P(b_[0], b_[1], d0), P(b_[0], b_[1], d0 + .05), P(a_[0], a_[1], d0 + .05), kier=kk)
    k = B.okno_nr[0]; RW = (u1 - u0) + .8; ru0 = us - RW / 2
    for (y0, y1, pi) in ((v0 + w, vt, 0), (vt, v1 - w, 0)):
        uv1 = [((x - ru0) / RW + k, (y - 0.0) / max(v1 - v0, 1.0) + pi) for x, y in ((u0 + w, y0), (u1 - w, y0), (u1 - w, y1), (u0 + w, y1))]
        S.quad(g, "okno", P(u0 + w, y0, d0 + .05), P(u1 - w, y0, d0 + .05), P(u1 - w, y1, d0 + .05), P(u0 + w, y1, d0 + .05), uv1=uv1, t=t, kier=n)
    B.okno_nr[0] += 1


def mur_pruski(S, g, a, t, n, L, okna, p0, hk, kond, Hs, rozst, off, k, mat="belka_ciemna"):
    """Spichrze nad Brdą (zdjęcia m22–m26, m33–m35): ciemna konstrukcja ryglowa — słupy 16 cm co ok. 0,85 m (przerwane przy
    oknach), rygle na wysokości stropów, parapetów i nadproży, zastrzały w skrajnych polach każdej kondygnacji.
    Belki 3 cm przed licem (bez wspólnej płaszczyzny z wypełnieniem)."""
    P = lambda u, v, d=0.0: a + t * u + UP * v - n * d
    w = .19; e = .03
    pietra = [(.3, p0)] + [(p0 + f * hk, min(p0 + (f + 1) * hk, Hs)) for f in range(kond - 1)]
    m = max(1, int(round(L / 1.05)))
    slupy = [min(max(i * L / m - w / 2, 0), L - w) for i in range(m + 1)]
    for (y0, y1) in pietra:
        okn = [(u0, u1) for (u0, u1, v0, v1, r, pp) in okna if v0 >= y0 - .05 and v1 <= y1 + .05]
        for u in slupy:
            if any(u0 - w < u < u1 for (u0, u1) in okn): continue
            S.box(g, mat, P(u, y0, 0), t, UP, n, w, y1 - y0, e, bez=("-z",))
        for (u0, u1) in okn:                                       # słupki przy oknach
            for u in (u0 - w, u1):
                if 0 <= u <= L - w: S.box(g, mat, P(u, y0, -.002), t, UP, n, w, y1 - y0, e + .002, bez=("-z",))
    poz = [.3] + [p0 + f * hk for f in range(kond - 1)] + [Hs - w]
    for (u0, u1, v0, v1, r, pp) in okna:
        poz += [v0 - w, v1]
    for v in sorted(set(round(x, 2) for x in poz)):
        if v < .2 or v > Hs - w + .01: continue
        S.box(g, mat, P(0, v, -.005), t, UP, n, L, w, e + .005, bez=("-z",))
    # zastrzały (24.11, IMG_2010 Grodzka 11 i IMG_2013 spichrz holenderski): regularna siatka, zastrzały tylko w skrajnych
    # polach każdej kondygnacji („/” przy lewym narożniku, „\” przy prawym), na długich ścianach (>12 pól) dodatkowo w co
    # szóstym polu; pola z oknem bez zastrzałów. Belki zastrzałów 1 cm przed słupami i ryglami (bez wspólnych płaszczyzn).
    for f_, (y0, y1) in enumerate(pietra):
        okn = [(u0, u1) for (u0, u1, v0, v1, r, pp) in okna if v0 >= y0 - .05 and v1 <= y1 + .05]
        pola_ = [(slupy[i] + w, slupy[i + 1]) for i in range(len(slupy) - 1)]
        for i, (ua, ub) in enumerate(pola_):
            if ub - ua < .3 or y1 - y0 < .8: continue
            if any(u0 - w < ub and u1 > ua for (u0, u1) in okn): continue
            if not (i == 0 or i == len(pola_) - 1 or (len(pola_) > 12 and i % 6 == 3)): continue
            lewo = i < len(pola_) / 2
            ya, yb = (y0 + w, y1 - w) if lewo else (y1 - w, y0 + w)
            A = P(ua, ya, -.01); Bq = P(ub, yb, -.01); dl = float(np.linalg.norm(Bq - A)); tt = (Bq - A) / dl
            nn = nrm(np.cross(tt, n)); S.box(g, mat, A - nn * w / 2, tt, nn, n, dl, w, e + .01, bez=("-z",))


def lukarna(S, g, p2, tt, nin, y, tg, ok, mat, tynk, hmin, dF=.7):
    """Lukarna dwuspadowa na połaci od ulicy: ścianka czołowa z oknem 0,8 × 0,95 m 0,7 m za okapem, policzki do połaci,
    daszek z dachówki z kalenicą prostopadłą do okapu. Pomijana, gdy połać jest za krótka (kopertowy narożnik)."""
    w, hl, dF = 1.3, 1.45, .7
    hF = y + tg * (dF + ok) - .04; yT = hF + hl
    dB = (yT + .5 - y) / tg - ok + .05
    pB = p2 + nin * (dB + .3)
    if hmin(*pB) < yT + .45: return
    n3 = np.array([-nin[0], 0., -nin[1]]); t3 = np.array([-n3[2], 0., n3[0]])          # cross(UP, t3) = n3
    c3 = np.array([p2[0], 0., p2[1]]) - n3 * dF
    FL = c3 - t3 * w / 2 + UP * hF
    P = lambda u, v, d=0.0: FL + t3 * u + UP * v - n3 * d
    S.sciana(g, tynk, FL, t3, w, hl, [(.25, 1.05, .3, 1.25)], uv1_skala=(w, hl))
    okno_lekkie(g, P, t3, n3, .25, 1.05, .3, 1.25, 0, True)
    dS = (yT - y) / tg - ok - dF                                   # policzki: trójkąty od czoła do połaci
    for u_, kk in ((0., -t3), (w, t3)):
        a = P(u_, 0); b = P(u_, hl); c = P(u_, hl, dS)
        if np.dot(np.cross(b - a, c - a), kk) < 0: b, c = c, b
        S.tri_list(g, tynk, [a, b, c], kk, n3, [(0, 0), (0, hl), (dS, hl)], None, [0, 1, 2])
    # daszek: kalenica 0,5 m nad czołem, okap 12 cm poza ściankami
    hr = .5; e = .12
    dR = (yT + hr - y) / tg - ok - dF; dE = (yT - .04 - y) / tg - ok - dF
    R0 = P(w / 2, hl + hr, -e); R1 = P(w / 2, hl + hr, dR)
    for s_ in (-1, 1):
        u_e = w / 2 + s_ * (w / 2 + e)
        E0 = P(u_e, hl - .04, -e); E1 = P(u_e, hl - .04, dE)
        q = [E0, R0, R1, E1] if s_ < 0 else [R0, E0, E1, R1]
        nn = nrm(np.cross(q[1] - q[0], q[3] - q[0]))
        if nn[1] < 0: q = q[::-1]
        S.quad(g, mat, *q, kier=UP)
    a = P(0, hl); b = P(w, hl); c = P(w / 2, hl + hr - .02)              # trójkąt szczytu nad oknem
    S.tri_list(g, tynk, [a, b, c] if np.dot(np.cross(b - a, c - a), n3) > 0 else [a, c, b], n3, t3, [(0, 0), (w, 0), (w / 2, hr)], None, [0, 1, 2])


def ozdoby_sciany(S, g, a, t, n, L, okna, sp, oz, p0, hk, kond, H, att, rozst, ow, oh, opar, wyk, balk, osie):
    """Fasada kamienicy od ulicy (zdjęcia 02–03 Rynek): naczółki nad oknami pięter (na I piętrze trójkątne), płyciny pod
    oknami, lizeny między osiami, wykusz na środkowych osiach albo balkon z kutą balustradą na I piętrze. Wszystko
    wysunięte z lica (≥ 3 cm) — bez wspólnych płaszczyzn z tynkiem."""
    P = lambda u, v, d=0.0: a + t * u + UP * v - n * d
    OP = B.OPASKA; mo = "opaska"
    for (u0, u1, v0, v1, r, pp) in okna:
        if r != "okno_l" or pp < 1: continue
        S.box(g, mo, P(u0 - OP - .06, v1 + OP, 0), t, UP, n, (u1 - u0) + 2 * OP + .12, .13, .13, bez=("-z",))   # naczółek prosty
        if pp == 1 and oz.get("trojkat"):                                                                    # trójkątny na I p.
            W_ = (u1 - u0) + 2 * OP + .12; y0 = v1 + OP + .13; d_ = -.1
            A = P(u0 - OP - .06, y0, d_); Bq = P(u0 - OP - .06 + W_, y0, d_); C = P((u0 + u1) / 2, y0 + .36, d_)
            S.tri_list(g, mo, [A, Bq, C] if np.dot(np.cross(Bq - A, C - A), n) > 0 else [A, C, Bq], n, t, [(0, 0), (W_, 0), (W_ / 2, .36)], None, [0, 1, 2])
            for (p_, q_) in ((A, C), (C, Bq)):
                S.quad(g, mo, p_, q_, q_ - n * .1, p_ - n * .1, kier=UP)
        if oz.get("plyciny") and v0 - .55 > 0:
            S.box(g, mo, P(u0 - .04, v0 - .52, 0), t, UP, n, (u1 - u0) + .08, .42, .03, bez=("-z",))
    if oz.get("lizeny") and kond >= 2:
        for u in osie:
            if wyk and any(abs(u - c) < rozst * 1.1 for c in wyk): continue
            S.box(g, mo, P(u - .18, p0 + .12, 0), t, UP, n, .36, H + att - p0 - .7, .07, bez=("-z",))
    if wyk:                                                            # wykusz: piętra 1…ostatnie, wysunięty 0,8 m
        ub0 = min(wyk) - rozst / 2 + .15; ub1 = max(wyk) + rozst / 2 - .15; Wb = ub1 - ub0; D = .8
        y0 = p0 + .25; y1 = H - .3
        o_b = P(ub0, y0, -D); okn = []
        for f in range(1, kond):
            fl = p0 + (f - 1) * hk
            for c in wyk: okn.append((c - ow / 2 - ub0, c + ow / 2 - ub0, fl + opar - y0, fl + opar + oh - y0, "okno_l", f))
        sciana(g, sp["tynk"], o_b, t, n, Wb, y1 - y0, okn, (Wb, y1 - y0))
        for (u_, kk) in ((ub0, -t), (ub1, t)):
            S.quad(g, sp["tynk"], P(u_, y0, 0), P(u_, y0, -D), P(u_, y1, -D), P(u_, y1, 0), kier=kk)
        S.quad(g, mo, P(ub0, y0, 0), P(ub1, y0, 0), P(ub1, y0, -D), P(ub0, y0, -D), kier=-UP)
        S.box(g, "blacha_miedz", P(ub0 - .08, y1, 0), t, UP, n, Wb + .16, .18, D + .1)                      # daszek wykusza
        S.box(g, mo, P(ub0 - .05, y0 - .22, 0), t, UP, n, Wb + .1, .22, D + .06)                           # gzyms podstawy
        for (u_, kk) in ((ub0 + .15, 0), (ub1 - .45, 0)):                                                # wsporniki
            S.box(g, mo, P(u_, y0 - .9, 0), t, UP, n, .3, .68, D - .1)
    if balk is not None:                                               # balkon I piętra: płyta, wsporniki, kuta balustrada
        wb = ow + 1.2; D = .85; fl = p0
        S.box(g, "opaska", P(balk - wb / 2, fl - .16, 0), t, UP, n, wb, .16, D)
        for u_ in (balk - wb / 2 + .15, balk + wb / 2 - .45):
            S.box(g, "opaska", P(u_, fl - .7, 0), t, UP, n, .3, .54, D - .15)
        y_ = fl; hr = 1.0
        rog = [P(balk - wb / 2 + .05, y_, -.05), P(balk - wb / 2 + .05, y_, -(D - .05)), P(balk + wb / 2 - .05, y_, -(D - .05)), P(balk + wb / 2 - .05, y_, -.05)]
        for (p_, q_) in zip(rog, rog[1:]):
            Lq = float(np.linalg.norm(q_ - p_)); tq = (q_ - p_) / Lq; nq = nrm(np.cross(UP, tq))
            S.box(g, "stal_balustrada", p_ + UP * hr - nq * .025, tq, UP, nq, Lq, .05, .05)
            S.box(g, "stal_balustrada", p_ + UP * .12 - nq * .015, tq, UP, nq, Lq, .03, .03)
            for k_ in np.arange(.06, Lq, .12): S.rura(g, "stal_balustrada", p_ + tq * k_, p_ + tq * k_ + UP * hr, .011, seg=4)


def rozklad_wypukly(P):
    """Podział wielokąta na części wypukłe: trójkąty z triangulacji z ograniczeniami łączone parami, dopóki suma jest
    wypukła (Hertel–Mehlhorn). Kamienice w pierzei to zwykle 1–3 części."""
    if P.convex_hull.area <= P.area * 1.02: return [P]
    cz = [t for t in shapely.constrained_delaunay_triangles(P).geoms if t.area > 1e-4]
    zmiana = True
    while zmiana:
        zmiana = False
        for i in range(len(cz)):
            for j in range(i + 1, len(cz)):
                wsp = cz[i].intersection(cz[j])
                if wsp.length < .05: continue
                u = cz[i].union(cz[j]).simplify(.01)
                if u.geom_type == "Polygon" and u.convex_hull.area <= u.area * 1.005:
                    cz[i] = u; cz.pop(j); zmiana = True; break
            if zmiana: break
    return cz


def dach_kamienica(S, g, P, wolne, y, mat, spadek, tynk, kal_max=7.5, ulica=None, rozst=2.2, kominy=True, szczyt=None, belki=None, rzedy=1):
    """Dach kamienicy w pierzei: połacie tylko od krawędzi wolnych (ulica, podwórze), nad ścianami przy sąsiedzie szczyt
    (ściana ogniowa w tynku budynku) — jak na zdjęciach Rynku i Focha. Wysokość połaci = y + tg(spadek) · odległość od
    krawędzi (okap 0,35 m poza licem), dach = minimum połaci; powyżej kal_max płaski taras (mansardy dużych kamienic).
    Jedna wolna krawędź → dach pulpitowy (oficyny), wszystkie wolne → kopertowy. Bez wspólnych płaszczyzn (szczyt 2 cm
    w głąb od lica, połacie przecięte dokładnie)."""
    from shapely.geometry import Polygon as Pg, LineString as Ls
    tg = math.tan(math.radians(spadek)); ok = .35
    pts = list(P.exterior.coords)[:-1]; nkr = len(pts)
    kr = []                                                   # (punkt na krawędzi, normalna do środka, wolna)
    for i in range(nkr):
        a_ = np.array(pts[i]); b_ = np.array(pts[(i + 1) % nkr]); L = np.linalg.norm(b_ - a_)
        if L < 1e-6: continue
        tt = (b_ - a_) / L; nin = np.array([-tt[1], tt[0]])           # CCW: do środka po lewej
        kr.append((a_, b_, nin, wolne[i] if i < len(wolne) else True))
    wl = [(a_, nin) for (a_, b_, nin, f) in kr if f]
    if not wl or P.area < 8:
        dach_plaski(S, g, P, y - .02, .3, "papa"); return
    # dziedzina: obrys poszerzony o okap tylko po wolnych stronach
    D = P
    for (a_, b_, nin, f) in kr:
        if f:
            tt = (b_ - a_) / max(np.linalg.norm(b_ - a_), 1e-6)
            D = D.union(Pg([tuple(a_ - tt * ok), tuple(b_ + tt * ok), tuple(b_ + tt * ok - nin * ok), tuple(a_ - tt * ok - nin * ok)]))
    D = D.buffer(0).simplify(.02)
    if D.geom_type != "Polygon": D = max(getattr(D, "geoms", [D]), key=lambda q: q.area)
    hcap = kal_max
    dist = lambda k_, p: float(np.dot(np.asarray(p) - wl[k_][0], wl[k_][1])) + ok        # odległość od okapu krawędzi k
    R_ = 1e4; c0 = np.array(P.centroid.coords[0])

    def pol(nv, c):                                           # półpłaszczyzna {p: nv·p <= c} jako duży wielokąt
        L_ = np.linalg.norm(nv)
        if L_ < 1e-9: return Pg([(c0[0] - R_, c0[1] - R_), (c0[0] + R_, c0[1] - R_), (c0[0] + R_, c0[1] + R_), (c0[0] - R_, c0[1] + R_)]) if c >= 0 else Pg()
        nv = nv / L_; c = c / L_; p0 = nv * c; tt = np.array([-nv[1], nv[0]])
        return Pg([tuple(p0 + tt * R_), tuple(p0 + tt * R_ - nv * R_ * 2), tuple(p0 - tt * R_ - nv * R_ * 2), tuple(p0 - tt * R_)])
    # płaszczyzny: k-ta połać h = tg·(n_k·p − n_k·a_k + ok); taras h = hcap
    lin = [(wl[k_][1] * tg, tg * (ok - float(np.dot(wl[k_][1], wl[k_][0])))) for k_ in range(len(wl))] + [(np.zeros(2), hcap)]
    sciany_h = []
    for i, (nv_i, c_i) in enumerate(lin):
        F = D
        for j, (nv_j, c_j) in enumerate(lin):
            if j == i or F.is_empty: continue
            F = F.intersection(pol(nv_i - nv_j, c_j - c_i))      # h_i <= h_j
        for q in getattr(F, "geoms", [F]):
            if q.is_empty or q.geom_type != "Polygon" or q.area < 1e-4: continue
            for tri in shapely.constrained_delaunay_triangles(q).geoms:
                c3 = [np.array([x, y + float(np.dot(nv_i, (x, z))) + c_i, z]) for x, z in list(tri.exterior.coords)[:3]]
                if np.cross(c3[1] - c3[0], c3[2] - c3[0])[1] < 0: c3[1], c3[2] = c3[2], c3[1]
                nn = nrm(np.cross(c3[1] - c3[0], c3[2] - c3[0])); t3 = nrm(np.cross(UP, nn)) if abs(nn[1]) < .999 else np.array([1., 0., 0.])
                uvq = [(float(np.dot(p_, t3)), float(np.dot(p_, nrm(np.cross(nn, t3))))) for p_ in c3]
                S.tri_list(g, mat if i < len(wl) else "papa", c3, nn, t3, uvq, None, [0, 1, 2])
            sciany_h.append((q, nv_i, c_i))
    # podbitka okapu (pasek D − P) i szczyty nad ścianami przy sąsiedzie
    for q in getattr(D.difference(P), "geoms", [D.difference(P)]):
        if q.is_empty or q.geom_type != "Polygon": continue
        for tri in shapely.constrained_delaunay_triangles(q).geoms:
            c3 = [np.array([x, y - .01, z]) for x, z in list(tri.exterior.coords)[:3]]
            if np.cross(c3[1] - c3[0], c3[2] - c3[0])[1] > 0: c3[1], c3[2] = c3[2], c3[1]
            S.tri_list(g, "deska_podbitka", c3, (0., -1., 0.), (1., 0., 0.), [(p_[0], p_[2]) for p_ in c3], None, [0, 1, 2])
    hmin = lambda x, z: y + min(float(np.dot(nv, (x, z))) + c_ for (nv, c_) in lin)
    # lukarny od ulicy (zdjęcia 02–03: rząd lukarn w dachu nad fasadą od Rynku)
    if ulica is not None:
        for i_k, (a_, b_, nin, f) in enumerate(kr):
            if not f or i_k >= len(ulica) or not ulica[i_k]: continue
            Lk = float(np.linalg.norm(b_ - a_)); tt = (b_ - a_) / max(Lk, 1e-6)
            nk = max(0, int((Lk - 2.4) // (2 * rozst)) + 1)
            for rz in range(rzedy):                               # spichrze (IMG_2009): kilka rzędów lukarn, mijankowo
                for j in range(nk + (rz % 2)):
                    s_ = Lk / 2 + (j - (nk - 1 + rz % 2) / 2) * 2 * rozst
                    if s_ < 1.4 or s_ > Lk - 1.4: continue
                    lukarna(S, g, a_ + tt * s_, tt, nin, y, tg, ok, mat, tynk, hmin, dF=.7 + rz * 2.3)
    # kominy z cegły przy ścianach sąsiadów (i jeden na kalenicy w budynku wolnostojącym)
    if kominy:
        pk = [(a_ + b_) / 2 + nin * .6 for (a_, b_, nin, f) in kr if not f and np.linalg.norm(b_ - a_) > 3]
        if not pk: pk = [np.array(P.representative_point().coords[0])]
        szczyt_h = max(hmin(*p_) for p_ in [np.array(q_) for q_ in P.exterior.coords] + [np.array(P.centroid.coords[0])])
        for p_ in pk[:3]:
            if not P.buffer(-.4).contains(shapely.geometry.Point(*p_)): continue
            h0 = hmin(*p_); top = max(h0 + 1.0, szczyt_h + .7)
            o3 = np.array([p_[0] - .35, h0 - .4, p_[1] - .25])
            S.box(g, "cegla_komin", o3, (1., 0., 0.), UP, (0., 0., 1.), .7, top - h0 + .4, .5)
            S.box(g, "obrobka", o3 + np.array([-.06, top - h0 + .4, -.06]), (1., 0., 0.), UP, (0., 0., 1.), .82, .07, .62)
    for (a_, b_, nin, f) in kr:
        if f: continue
        a2 = a_ + nin * .02; b2 = b_ + nin * .02                   # szczyt 2 cm w głąb (bez wspólnej płaszczyzny z sąsiadem)
        odc = Ls([tuple(a2), tuple(b2)])
        for (q, nv, c_) in sciany_h:
            cz = odc.intersection(q.buffer(.03))
            for seg in getattr(cz, "geoms", [cz]):
                if seg.is_empty or seg.geom_type != "LineString": continue
                (x0, z0), (x1, z1) = seg.coords[0], seg.coords[-1]
                h0 = y + float(np.dot(nv, (x0, z0))) + c_ - .01; h1 = y + float(np.dot(nv, (x1, z1))) + c_ - .01
                if max(h0, h1) - y < .03: continue
                p0, p1 = np.array([x0, y - .05, z0]), np.array([x1, y - .05, z1])
                q0, q1 = np.array([x0, h0, z0]), np.array([x1, h1, z1])
                S.quad(g, szczyt or tynk, p0, p1, q1, q0, kier=np.array([-nin[0], 0., -nin[1]]))
                S.quad(g, szczyt or tynk, p1, p0, q0, q1, kier=np.array([nin[0], 0., nin[1]]))      # od strony strychu/sąsiada (niższy sąsiad)
                if belki:                                                  # szczyt spichrza: słupy co 1,1 m do połaci, rygle co 2,6 m
                    nz = np.array([-nin[0], 0., -nin[1]]); dl = float(np.hypot(x1 - x0, z1 - z0)); tz = (p1 - p0) / max(dl, 1e-6)
                    for u in np.arange(.0, dl, .85):
                        # 24.11: wysokość słupa liczona po niższej krawędzi (u albo u + 0,16) — bez wystawania za połać
                        hu = h0 + (h1 - h0) * min(u, u + .16, key=lambda x: h0 + (h1 - h0) * x / max(dl, 1e-6)) / max(dl, 1e-6) - .08
                        if hu - y < .3: continue
                        S.box(g, belki, p0 + tz * u + UP * .05 + nz * .0, tz, UP, nz, .16, hu - y, .03, bez=("-z",))
                    for v in np.arange(y + 1.3, max(h0, h1), 1.3):
                        us_ = [u for u in np.linspace(0, dl, 60) if h0 + (h1 - h0) * u / max(dl, 1e-6) > v + .2]
                        if len(us_) < 2: continue
                        S.box(g, belki, p0 + tz * us_[0] + UP * (v - y - .05) - nz * .0, tz, UP, nz, us_[-1] - us_[0], .16, .035, bez=("-z",))
                    # 24.11 (IMG_2010): ciemna belka 0,24 m wzdłuż połaci nad końcami słupów (krawędź szczytu jak na zdjęciu,
                    # bez schodków z końców słupów); 1,5 cm przed słupami i ryglami
                    A0 = np.array([x0, h0 - .02, z0]); B0 = np.array([x1, h1 - .02, z1]); dd0 = float(np.linalg.norm(B0 - A0))
                    if dd0 > .3:
                        tt0 = (B0 - A0) / dd0; nn0 = nrm(np.cross(tt0, nz))
                        if nn0[1] < 0: nn0 = -nn0
                        S.box(g, belki, A0 - nn0 * .24 + nz * .015, tt0, nn0, nz, dd0, .24, .035, bez=("-z",))
                    # 24.11 (IMG_2010, Grodzka 11): zastrzały w szczycie — w każdym pasie między ryglami w skrajnym pełnym polu
                    # z lewej („/”) i z prawej („\”); 1 cm przed licem, 1 cm przed ryglami (bez wspólnych płaszczyzn)
                    sl_ = list(np.arange(.0, dl, .85))
                    hu_ = lambda u: h0 + (h1 - h0) * u / max(dl, 1e-6)
                    for v in np.arange(y, max(h0, h1) - 1.3, 1.3):
                        pola_ = [(ua, ub) for ua, ub in zip(sl_, sl_[1:]) if min(hu_(ua), hu_(ub)) > v + 1.3 + .25]
                        if len(pola_) < 3: continue
                        for (ua, ub), lewo in ((pola_[0], True), (pola_[-1], False)):
                            va, vb = (v + .06, v + 1.3 - .16) if lewo else (v + 1.3 - .16, v + .06)
                            A = p0 + UP * .05 + tz * (ua + .16) + UP * (va - y); Bq = p0 + UP * .05 + tz * ub + UP * (vb - y)
                            dd_ = float(np.linalg.norm(Bq - A)); tt_ = (Bq - A) / dd_; nn_ = nrm(np.cross(tt_, nz))
                            S.box(g, belki, A - nn_ * .07 + nz * .01, tt_, nn_, nz, dd_, .14, .035, bez=("-z",))


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


def market_front(S, g, f, H, att, glowny=True, k_=0):
    """Front marketu (IMG_1577, IMG_1738–1742): podcień 3 m z białymi słupami co 6 m i stropem na +4,2, rama-pergola
    na dachu wzdłuż frontu (słupki co 5 m, belka), biała wieża w narożniku (pole na logo puste — nazwa decyzją
    właściciela), przeszklona klatka schodowa obok wieży, biały walec przy drugim końcu."""
    a, b, t, n, L = f
    yk = 4.2 + .03 * k_                                   # kolejne fronty minimalnie wyżej — bez wspólnych płaszczyzn na narożach
    S.box(g, "market_elewacja", a + UP * yk, t, UP, n, L, .35, 3.0, bez=("-z",))
    S.box(g, "slup_grafit", a + UP * (yk - .02) + n * 2.95, t, UP, n, L, .4, .08)          # obróbka krawędzi podcienia (grafit)
    for u in np.arange(3.0, L - 1.0, 6.0):                  # słupy podcienia: grafit (właściciel 20:25 — odróżnić od jasnej elewacji)
        S.rura(g, "slup_grafit", a + t * u + n * 2.7, a + t * u + n * 2.7 + UP * yk, .22, seg=16)
        S.rura(g, "stal_ocynk", a + t * u + n * 2.7 + UP * .12, a + t * u + n * 2.7 + UP * .42, .26, seg=16)   # stalowa stopa
    yp = H + att
    # pergola z lameli na dachu wzdłuż frontu (zdjęcia 18:47): wsporniki co 2,5 m za attyką, belka, 5 lameli
    # równoległych do elewacji, pochylonych 30°, cofniętych od lica (nic nie wystaje w powietrze przed elewację)
    for u in np.arange(1.0, L - .9, 2.5):
        S.box(g, "market_rama", a + t * u - n * 1.6 + UP * yp, t, UP, n, .18, 1.5, 1.4)
    S.box(g, "market_rama", a + t * 1.0 - n * 1.62 + UP * (yp + 1.5), t, UP, n, L - 2.0, .14, 1.44)
    nach = nrm(-n * math.cos(math.radians(30)) + UP * math.sin(math.radians(30)))
    for j in range(5):
        S.box(g, "lamele", a + t * 1.0 - n * (.3 + j * .28) + UP * (yp + 1.66), t, nrm(np.cross(nach, t)) if np.dot(np.cross(nach, t), UP) > 0 else -nrm(np.cross(nach, t)), nach, L - 2.0, .03, .3)
    for kk, u in enumerate(np.arange(4.0 if glowny else 2.5, L - (22.0 if glowny else 4.0), 6.8)):   # banery na każdym froncie
        p0 = a + t * u + n * .06 + UP * 4.62; w = 3.4
        S.quad(g, "baner_%d" % ((kk + k_) % 4 + 1), p0, p0 + t * w, p0 + t * w + UP * .8, p0 + UP * .8, uv1=[(0, 0), (1, 0), (1, 1), (0, 1)], kier=n)
        S.box(g, "market_rama", p0 - t * .04 - n * .04 - UP * .04, t, UP, n, w + .08, .04, .08)
    if not glowny:
        for u in np.arange(2.0, L - 2.0, 1.6):
            S.rura(g, "slupek_zolty", a + t * u + n * 3.45, a + t * u + n * 3.45 + UP * .9, .08, seg=8)
        return
    # wieża w narożniku (koniec b), przeszklona klatka obok, walec na drugim końcu
    S.box(g, "market_elewacja", b - t * 6.0 - n * 4.5, t, UP, n, 6.0, H + att + 6.5, 4.6)
    S.box(g, "market_niebieski", b - t * 5.8 + n * .1 + UP * 1.5, t, UP, n, 5.6, H + att + 4.5, .12)     # rama pylonu
    S.box(g, "pole_logo", b - t * 5.3 + n * .22 + UP * 2.0, t, UP, n, 4.6, H + att + 3.5, .06)          # puste pole na logo
    S.box(g, "market_szklo", b - t * 15.0 - n * 2.0, t, UP, n, 9.0, H + att + 1.5, 2.6)
    c = a + t * 3.0 - n * 1.0
    S.rura(g, "market_elewacja", c, c + UP * (H + att + .4), 3.0, seg=32)
    # zaokrąglony narożnik (zdjęcia nocne 18:47): pas okien na piętrze i przeszklony parter od frontu i od końca budynku
    for (y0, y1) in ((.35, 3.7), (5.3, 7.3)):
        S.luk(g, "market_szklo", c, t, n, 3.04, 15, 205, y0, y1, seg=20)
        for f in np.linspace(15, 205, 9):
            d = t * math.cos(math.radians(f)) + n * math.sin(math.radians(f))
            S.rura(g, "market_rama", c + d * 3.07 + UP * y0, c + d * 3.07 + UP * y1, .045, seg=6)
        S.luk(g, "market_rama", c, t, n, 3.08, 15, 205, y1, y1 + .12, seg=20)
    S.luk(g, "pas_zielony", c, t, n, 3.07, 40, 180, 4.2, 5.0, seg=16)       # pas szyldu (pusty, bez nazwy)
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
