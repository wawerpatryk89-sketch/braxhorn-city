#!/usr/bin/env python3
"""MIASTO R1 — zabytki i miejsca ważne Starego Miasta w Bydgoszczy, budowane osobno (nie generatorem kamienic).

Skrzyżowanie Focha × Gdańska × Jagiellońska (właściciel 22:00–22:02: „to miejsce musi wyglądać idealnie”, zdjęcia
IMG_1983–1992 i stare_miasto 09–12):
  * Jagiellońska 2 (OSM w139275357) — narożnik zaokrąglony łukiem: podcienie z półkolistymi arkadami na kamiennych filarach,
    nad nimi pas wysokich okien przedzielonych białymi kolumienkami, okładzina z szarego kamienia do wysokości I piętra, wyżej
    jasny tynk, okna z tralkowymi balustradkami, gzymsy, attyka z tralkami i wazonami, czerwony dach mansardowy z lukarnami.
    Napis na fasadzie (prawdziwa nazwa) pominięty — w grze tylko BRAXHORN.
  * Kamienica Maxa Zweiningera, Focha 2 (w139749967) — zaokrąglony narożnik z wieżyczką (cegła, płaski taras z balustradą),
    podcienia z arkadami, w nich lokale (klub), na I piętrze wykusze obite zieloną miedzią, tynk piaskowy zabrudzony,
    balkony z kutą balustradą, falisty miedziany gzyms-attyka.
  * Kościół Wniebowzięcia NMP, Gdańska 2 (w89654070, kotwica układu) — gotycki, z czerwonej cegły: przypory, ostrołukowe
    okna z jasnym obramieniem, stromy dach z dachówki, okrągła wieża z cegły z pochyłym cokołem, zegary, barokowy hełm
    z miedzi (bania, latarnia, iglica z krzyżem).
Wszystko w metrach od kotwicy, te same Siatka/paleta co reszta Rynku.
"""
import math

import numpy as np
import shapely
import shapely.affinity
from shapely.geometry import Polygon, Point, LineString, box
from shapely.ops import unary_union

import ogolny
import blok_32a as B
from kit import UP, nrm

OSM_SAVOY = "w139275357"
OSM_ZWEININGER = "w139749967"
OSM_KOSCIOL = "w89654070"
OSM_BARKA = "w375204687"               # Barka Lemara — barka-muzeum zacumowana przy Starym Porcie (houseboat, na wodzie)
OSM_SPICHRZ_LUK = "w1332613698"        # spichlerz z łukowym (ostrołukowym) szczytem, Grodzka 7 (IMG_2010–2012)
OSM_SPICHRZ_CEGLA = "w681311761"       # spichlerz ceglany ze schodkowym szczytem, Grodzka 9 (IMG_2011–2012)
WLASNE = (OSM_SAVOY, OSM_ZWEININGER, OSM_KOSCIOL, OSM_BARKA, OSM_SPICHRZ_LUK, OSM_SPICHRZ_CEGLA)


# ---------------------------------------------------------------- narzędzia: ściana z dowolnymi otworami
def fasada(S, g, mat, o, t, n, L, H, otwory, gl=0.0, mat_osc=None, tyl=False, obrys=None):
    """Płaska ściana (u wzdłuż t, v w górę) z otworami o dowolnym kształcie (shapely w (u, v)): lico triangulowane,
    ościeża głębokości gl po obwodzie każdego otworu. tyl=True: dodatkowo lico od środka (podcienia: filar widać z obu stron)."""
    W = box(0, 0, L, H) if obrys is None else obrys
    for q in otwory: W = W.difference(q)
    P = lambda u, v, d=0.0: o + t * u + UP * v - n * d
    for part in getattr(W, "geoms", [W]):
        if part.is_empty or part.geom_type != "Polygon": continue
        for tri in shapely.constrained_delaunay_triangles(part).geoms:
            c = list(tri.exterior.coords)[:3]
            p = [P(u, v) for u, v in c]
            if np.dot(np.cross(p[1] - p[0], p[2] - p[0]), n) < 0: p[1], p[2] = p[2], p[1]; c[1], c[2] = c[2], c[1]
            S.tri_list(g, mat, p, n, t, [(u, v) for u, v in c], [(u / max(L, 1e-3), v / max(H, 1e-3)) for u, v in c], [0, 1, 2])
            if tyl:
                pb = [P(u, v, gl) for u, v in c][::-1]
                S.tri_list(g, mat, pb, -n, -t, [(u, v) for u, v in c][::-1], None, [0, 1, 2])
    if gl > 0:
        for q in otwory:
            r = list(shapely.geometry.polygon.orient(q, 1.0).exterior.coords)
            for (u0, v0), (u1, v1) in zip(r, r[1:]):
                if math.hypot(u1 - u0, v1 - v0) < 1e-4: continue
                a, b = P(u0, v0), P(u1, v1)
                kk = nrm(np.cross(b - a, -n))                     # do środka otworu
                q4 = [a, b, P(u1, v1, gl), P(u0, v0, gl)]
                S.quad(g, mat_osc or mat, *q4, kier=-kk if np.dot(kk, (P((u0 + u1) / 2, (v0 + v1) / 2) - P(*q.centroid.coords[0]))) > 0 else kk)


def luk_pol(u0, u1, v0, vs, seg=12):
    """Otwór z łukiem półkolistym: prostokąt u0..u1 od v0 do vs (wezgłowie) + półkole."""
    r = (u1 - u0) / 2; c = (u0 + u1) / 2
    pts = [(u0, v0), (u1, v0), (u1, vs)] + [(c + r * math.cos(a), vs + r * math.sin(a)) for a in np.linspace(0, math.pi, seg + 1)[1:-1]] + [(u0, vs)]
    return Polygon(pts)


def ostrolukowy(u0, u1, v0, vs, seg=8):
    """Okno gotyckie: prostokąt + łuk ostry równoboczny (dwa łuki o promieniu = szerokość otworu)."""
    w = u1 - u0
    prawy = [(u0 + w * math.cos(a), vs + w * math.sin(a)) for a in np.linspace(0, math.pi / 3, seg)]
    lewy = [(u1 + w * math.cos(a), vs + w * math.sin(a)) for a in np.linspace(2 * math.pi / 3, math.pi, seg)]
    return Polygon([(u0, v0), (u1, v0)] + prawy + lewy[1:])


def szyba(S, g, o, t, n, q, d, k_nr, mat="okno"):
    """Szyba (pokój za szybą) w kształcie otworu q, cofnięta o d."""
    P = lambda u, v: o + t * u + UP * v - n * d
    u0, v0, u1, v1 = q.bounds
    for tri in shapely.constrained_delaunay_triangles(q.buffer(-.04)).geoms:
        c = list(tri.exterior.coords)[:3]; p = [P(u, v) for u, v in c]
        if np.dot(np.cross(p[1] - p[0], p[2] - p[0]), n) < 0: p[1], p[2] = p[2], p[1]; c[1], c[2] = c[2], c[1]
        S.tri_list(g, mat, p, n, t, [(u, v) for u, v in c], [((u - u0) / max(u1 - u0, .5) * .8 + .1 + k_nr, (v - v0) / max(v1 - v0, .5)) for u, v in c], [0, 1, 2])


def rama(S, g, o, t, n, q, d, w=.07, mat="pcv", slupki=(), rygle=()):
    """Rama w licu szyby: obwód otworu (pas szerokości w) + słupki/rygle (pozycje u / v)."""
    P = lambda u, v: o + t * u + UP * v - n * d
    pas = q.difference(q.buffer(-w))
    u0, v0, u1, v1 = q.bounds
    for u in slupki: pas = pas.union(box(u - w / 2, v0, u + w / 2, v1).intersection(q))
    for v in rygle: pas = pas.union(box(u0, v - w / 2, u1, v + w / 2).intersection(q))
    for part in getattr(pas, "geoms", [pas]):
        if part.is_empty or part.geom_type != "Polygon": continue
        for tri in shapely.constrained_delaunay_triangles(part).geoms:
            c = list(tri.exterior.coords)[:3]; p = [P(u, v) for u, v in c]
            if np.dot(np.cross(p[1] - p[0], p[2] - p[0]), n) < 0: p[1], p[2] = p[2], p[1]; c[1], c[2] = c[2], c[1]
            S.tri_list(g, mat, p, n, t, [(u, v) for u, v in c], None, [0, 1, 2])


def przeprobkuj(P, front, krok):
    """Obrys z frontem przepróbkowanym tak, by każdy odcinek frontu był jednym przęsłem (łuk narożnika → odcinki co krok)."""
    pts = list(shapely.geometry.polygon.orient(P, 1.0).exterior.coords)[:-1]
    nowe = []
    nk = len(pts)
    i = 0
    while i < nk:
        a = pts[i]; b = pts[(i + 1) % nk]
        if front(a, b):
            j = i; lan = [a]
            while j < nk and front(pts[j % nk], pts[(j + 1) % nk]): lan.append(pts[(j + 1) % nk]); j += 1
            ln = LineString(lan); m = max(1, round(ln.length / krok))
            nowe += [ln.interpolate(k / m, normalized=True).coords[0] for k in range(m)]
            i = j
        else:
            nowe.append(a); i += 1
    return Polygon(nowe).buffer(0)


def krawedzie(P):
    pts = list(shapely.geometry.polygon.orient(P, 1.0).exterior.coords)
    for (x0, z0), (x1, z1) in zip(pts, pts[1:]):
        a = np.array([x0, 0., z0]); b = np.array([x1, 0., z1]); L = float(np.linalg.norm(b - a))
        if L < .05: continue
        t = (b - a) / L; n = nrm(np.cross(UP, t))
        if P.contains(Point(*((a + b) / 2 + n * .3)[[0, 2]])): a, b, t, n = b, a, -t, -n
        yield a, b, t, n, L


# ---------------------------------------------------------------- budynek z podcieniami (oba narożniki skrzyżowania)
def podcienia(S, g, a, t, n, L, h_luk, mat_filar, gl_pod=3.0, pier=1.0, k_nr=0, szyld=None):
    """Jedno przęsło podcieni: ściana z półkolistą arkadą (filary po pier/2 z obu stron), ościeże 0,6 m, za nią korytarz
    gl_pod z sufitem, posadzką i witryną lokalu na ścianie tylnej. Zwraca wysokość sufitu."""
    u0, u1 = pier / 2, L - pier / 2; r = (u1 - u0) / 2; vs = max(1.2, h_luk - r)
    otw = luk_pol(u0, u1, 0.0, vs)
    hc = h_luk + .45
    fasada(S, g, mat_filar, a, t, n, L, hc, [otw], gl=.6, mat_osc=mat_filar, tyl=True)
    P = lambda u, v, d=0.0: a + t * u + UP * v - n * d
    S.quad(g, "opaska", P(0, hc, .6), P(L, hc, .6), P(L, hc, gl_pod), P(0, hc, gl_pod), kier=-UP)          # sufit podcieni
    S.quad(g, "rynek_plyty", P(0, .12, 0), P(L, .12, 0), P(L, .12, gl_pod), P(0, .12, gl_pod), kier=UP)     # posadzka
    # ściana tylna z witryną lokalu
    w0, w1 = .5, L - .5; wq = box(w0, .45, w1, min(hc - .6, 3.4))
    o_t = P(0, 0, gl_pod)
    fasada(S, g, "kb_kremowy2" if False else mat_filar, o_t, t, n, L, hc, [wq], gl=.12, mat_osc="rama_witryny")
    szyba(S, g, o_t, t, n, wq, .1, k_nr)
    rama(S, g, o_t, t, n, wq, .06, w=.08, mat="rama_witryny", slupki=[(w0 + w1) / 2], rygle=[wq.bounds[3] - .55])
    if szyld:
        S.quad(g, szyld, P(w0 + .2, wq.bounds[3] + .08, gl_pod - .04), P(w1 - .2, wq.bounds[3] + .08, gl_pod - .04),
               P(w1 - .2, wq.bounds[3] + .5, gl_pod - .04), P(w0 + .2, wq.bounds[3] + .5, gl_pod - .04),
               uv1=[(0, 0), (1, 0), (1, 1), (0, 1)], kier=n)
    return hc


def tralki(S, g, a, t, n, u0, u1, v, d=-.05, h=.7, mat="opaska"):
    """Balustradka tralkowa (zdjęcia Jagiellońska 2): płyta, pochwyt, tralki 12 cm co 0,22 m (ośmioboczne pręty)."""
    P = lambda u, vv, dd=0.0: a + t * u + UP * vv - n * dd
    S.box(g, mat, P(u0, v, d + .02), t, UP, n, u1 - u0, .08, .26, bez=("-z",))
    S.box(g, mat, P(u0, v + h - .08, d + .02), t, UP, n, u1 - u0, .08, .24, bez=("-z",))
    for u in np.arange(u0 + .14, u1 - .08, .22):
        c = P(u, v + .08, d - .11)
        S.rura(g, mat, c, c + UP * (h - .16), .05, seg=6)


def budynek_podcienia(S, g, P, publ, sp):
    """Wspólny budynek z podcieniami: sp = dict(kond, p0, hk, krok, tynk, kamien, h_kamien, kolumny, tralki, wykusze,
    miedz_attyka, wiezyczka, dach_mat, spadek, kal_max, szyld)."""
    front = lambda a, b: publ.contains(Point((a[0] + b[0]) / 2 + (b[1] - a[1]) * 6 / max(math.hypot(b[0] - a[0], b[1] - a[1]), 1e-6),
                                             (a[1] + b[1]) / 2 - (b[0] - a[0]) * 6 / max(math.hypot(b[0] - a[0], b[1] - a[1]), 1e-6)))
    Q = przeprobkuj(P, front, sp["krok"])
    kond, p0, hk = sp["kond"], sp["p0"], sp["hk"]; H = p0 + (kond - 1) * hk; hkam = sp.get("h_kamien", p0)
    k_nr = 0; B.POZIOM0, B.KOND = 0.0, hk
    fr = []
    for (a, b, t, n, L) in krawedzie(Q):
        jest_front = publ.contains(Point(*((a + b) / 2 + n * 6)[[0, 2]]))
        if not jest_front:                                         # tył: gładki tynk z prostymi oknami
            ogolny.sciana(g, sp["tynk"], a, t, n, L, H + .5, [], (L, H))
            continue
        fr.append((a, b, t, n, L))
        hc = podcienia(S, g, a, t, n, L, sp["h_luk"], sp["kamien"], k_nr=k_nr, szyld=sp.get("szyld") if L > 3 and k_nr % 3 == 1 else None)
        k_nr += 1
        Pf = lambda u, v, d=0.0: a + t * u + UP * v - n * d
        # pas kamienny nad arkadą do hkam z oknami antresoli (kolumny) albo zwykłymi oknami
        otw = []
        if sp.get("kolumny"):
            q = box(.45, hc + .5, L - .45, hkam - .35); otw.append(q)
        else:
            q = box(L / 2 - .6, hc + .6, L / 2 + .6, hkam - .4); otw.append(q)
        fasada(S, g, sp["kamien"], Pf(0, hc), t, n, L, hkam - hc, [box(q.bounds[0], q.bounds[1] - hc, q.bounds[2], q.bounds[3] - hc)], gl=.25, mat_osc=sp["kamien"])
        q0 = box(q.bounds[0], q.bounds[1] - hc, q.bounds[2], q.bounds[3] - hc); o_q = Pf(0, hc)
        szyba(S, g, o_q, t, n, q0, .22, k_nr)
        if sp.get("kolumny"):
            m = max(2, int((L - .9) / 1.0)); us = [q0.bounds[0] + (q0.bounds[2] - q0.bounds[0]) * k / m for k in range(1, m)]
            rama(S, g, o_q, t, n, q0, .2, w=.06, mat="rama_witryny", rygle=[q0.bounds[3] - .5])
            for u in us:                                          # białe kolumienki przed szybą
                c = Pf(u, hc + q0.bounds[1], .05)
                S.rura(g, "opaska", c, c + UP * (q0.bounds[3] - q0.bounds[1]), .14, seg=10)
        else:
            rama(S, g, o_q, t, n, q0, .18, w=.07, mat="pcv", slupki=[(q0.bounds[0] + q0.bounds[2]) / 2], rygle=[q0.bounds[3] - .45])
        S.box(g, "opaska", Pf(-.02, hkam - .1, 0), t, UP, n, L + .04, .3, .28, bez=("-z",))                # gzyms nad kamieniem
        # piętra w tynku
        okn = []
        for f in range(max(1, int(round((hkam - p0) / hk)) + 1), kond):
            fl = p0 + (f - 1) * hk
            if fl < hkam - .2: continue
            ow = min(1.35, L - 1.1)
            if ow < .6: continue
            okn.append((L / 2 - ow / 2, L / 2 + ow / 2, fl + .8, fl + .8 + min(2.1, hk - 1.3), f))
        Ht = H + .9 - hkam
        otw2 = [box(u0, v0 - hkam, u1, v1 - hkam) for (u0, u1, v0, v1, f) in okn]
        fasada(S, g, sp["tynk"], Pf(0, hkam), t, n, L, Ht, otw2, gl=.2, mat_osc=sp["tynk"])
        for (u0, u1, v0, v1, f), q2 in zip(okn, otw2):
            o2 = Pf(0, hkam)
            szyba(S, g, o2, t, n, q2, .19, k_nr)
            rama(S, g, o2, t, n, q2, .14, w=.07, mat="pcv", slupki=[(u0 + u1) / 2], rygle=[q2.bounds[3] - .5])
            S.box(g, "opaska", Pf(u0 - .18, v1, 0), t, UP, n, u1 - u0 + .36, .16, .16, bez=("-z",))           # naczółek
            S.box(g, "opaska", Pf(u0 - .1, v0 - .06, -.0), t, UP, n, u1 - u0 + .2, .06, .14, bez=("-z",))      # parapet
            if sp.get("tralki") and f in sp["tralki"]:
                tralki(S, g, a, t, n, u0 - .1, u1 + .1, v0 - .7)
        for f in range(1, kond):                                     # gzymsy międzypiętrowe
            yk = p0 + (f - 1) * hk
            if yk > hkam + .3: S.box(g, "opaska", Pf(-.02, yk - .08, 0), t, UP, n, L + .04, .16, .1, bez=("-z",))
        S.box(g, "opaska", Pf(-.05, H + .25, 0), t, UP, n, L + .1, .4, .45, bez=("-z",))                    # gzyms koronujący
        if sp.get("attyka_tralki"):
            tralki(S, g, a, t, n, 0, L, H + .65, d=-.2, h=.9)
        if sp.get("miedz_attyka"):                                   # falista attyka z miedzi (Focha 2)
            S.box(g, "blacha_miedz", Pf(-.05, H + .65, -.05), t, UP, n, L + .1, .9, .25, bez=("-z",))
        if sp.get("wykusze") and L > 3.0 and (k_nr % 3 == 0):        # wykusz z zieloną miedzią na I piętrze
            y0 = p0 + .2; y1 = p0 + hk - .4; D = .7
            S.box(g, "blacha_miedz", Pf(.35, y0, 0), t, UP, n, L - .7, y1 - y0, D)
            for f_ in range(1):
                q3 = box(.6, .5, L - .6, y1 - y0 - .3); o3 = Pf(.35, y0, -D) - t * .0
                szyba(S, g, Pf(0, y0, -D - .01), t, n, box(.6, .4, L - .6, y1 - y0 - .3), 0, k_nr, mat="okno")
                rama(S, g, Pf(0, y0, -D - .02), t, n, box(.6, .4, L - .6, y1 - y0 - .3), 0, w=.08, mat="pcv", slupki=[L / 2])
    # dach mansardowy (lukarny od ulicy) albo płaski
    ulica = []
    for (x0, z0), (x1, z1) in zip(list(shapely.geometry.polygon.orient(Q, 1.0).exterior.coords), list(shapely.geometry.polygon.orient(Q, 1.0).exterior.coords)[1:]):
        ulica.append(front((x0, z0), (x1, z1)))
    Qo = shapely.geometry.polygon.orient(Q, 1.0); ex = list(Qo.exterior.coords)
    def na_obrysie(m_):
        for k_, ((xa, za), (xb, zb)) in enumerate(zip(ex, ex[1:])):
            if LineString([(xa, za), (xb, zb)]).distance(Point(m_)) < .05: return k_
        return None
    for Qc in ogolny.rozklad_wypukly(Qo):                          # wklęsły obrys → części wypukłe (bez „nurkujących” połaci)
        Qc = shapely.geometry.polygon.orient(Qc, 1.0); eq = list(Qc.exterior.coords)
        kk = [na_obrysie(((xa + xb) / 2, (za + zb) / 2)) for (xa, za), (xb, zb) in zip(eq, eq[1:])]
        ogolny.dach_kamienica(S, g, Qc, [k_ is not None for k_ in kk], H + .9, sp["dach_mat"], sp["spadek"], sp["tynk"],
                              sp["kal_max"], [k_ is not None and ulica[k_] for k_ in kk] if sp.get("lukarny") else None, sp["krok"], True)
    return Q, H, fr


def wiezyczka(S, g, c, r, y0, h, mat="c_czerwona"):
    """Wieżyczka narożna (Focha 2): walec z cegły, gzyms, płaski taras z żeliwną balustradą."""
    for k in range(16):
        a0 = k / 16 * 2 * math.pi; a1 = (k + 1) / 16 * 2 * math.pi
        p0 = c + np.array([math.cos(a0) * r, y0, math.sin(a0) * r]); p1 = c + np.array([math.cos(a1) * r, y0, math.sin(a1) * r])
        kk = nrm(np.array([math.cos((a0 + a1) / 2), 0, math.sin((a0 + a1) / 2)]))
        S.quad(g, mat, p0, p1, p1 + UP * h, p0 + UP * h, kier=kk)
        e0 = c + np.array([math.cos(a0) * (r + .18), y0 + h, math.sin(a0) * (r + .18)]); e1 = c + np.array([math.cos(a1) * (r + .18), y0 + h, math.sin(a1) * (r + .18)])
        S.quad(g, "opaska", p0 + UP * (h - .25) + (e0 - p0 - UP * h) * 0, p1 + UP * (h - .25), e1, e0, kier=nrm(kk + UP))
        S.quad(g, "balustrada_kuta", e0 + UP * .02, e1 + UP * .02, e1 + UP * 1.0, e0 + UP * 1.0, uv_o=e0, kier=kk)
    top = [c + np.array([math.cos(k / 16 * 2 * math.pi) * (r + .18), y0 + h + .01, math.sin(k / 16 * 2 * math.pi) * (r + .18)]) for k in range(16)]
    for k in range(1, 15):
        q = [top[0], top[k], top[k + 1]]
        if np.cross(q[1] - q[0], q[2] - q[0])[1] < 0: q[1], q[2] = q[2], q[1]
        S.tri_list(g, "papa", q, UP, (1., 0., 0.), [(p[0], p[2]) for p in q], None, [0, 1, 2])


# ---------------------------------------------------------------- kościół z wieżą
def kosciol(S, g, P):
    """Gotycki kościół z cegły (Gdańska 2, zdjęcia IMG_1991–1992): mury 11 m z przyporami, ostrołukowe okna z jasnym
    obramieniem i okulusy między nimi, stromy dach (58°) z czerwonej dachówki nad nawą, szczyty z pinaklami; okrągła wieża
    z cegły od skrzyżowania (r 2,6 m, 31 m) z pochyłym cokołem do 6 m, parami okienek, zegarami, barokowym hełmem z miedzi
    (bęben, bania, latarnia z arkadami, mała bania, iglica z krzyżem)."""
    Hm = 11.0
    for (a, b, t, n, L) in krawedzie(P.simplify(.25)):
        otw = []
        if L > 3.5:
            m = max(1, int(L // 4.4))
            for k in range(m):
                c = (k + .5) * L / m
                otw.append(ostrolukowy(c - .75, c + .75, 2.6, 7.4))
                if k < m - 1: otw.append(Point((k + 1) * L / m, 8.9).buffer(.42, 8))          # okulus
        fasada(S, g, "c_ceglasta", a, t, n, L, Hm, otw, gl=.45, mat_osc="opaska")
        for k, q in enumerate(otw):
            if q.area < 1.0:
                szyba(S, g, a, t, n, q, .4, k, mat="witraz"); continue
            ram = q.difference(q.buffer(-.12))                         # jasne obramienie w licu (zdjęcie: kremowe opaski)
            for part in getattr(ram, "geoms", [ram]):
                for tri in shapely.constrained_delaunay_triangles(part).geoms:
                    cc = list(tri.exterior.coords)[:3]; pp = [a + t * u + UP * v + n * .03 for u, v in cc]
                    if np.dot(np.cross(pp[1] - pp[0], pp[2] - pp[0]), n) < 0: pp[1], pp[2] = pp[2], pp[1]
                    S.tri_list(g, "opaska", pp, n, t, [(u, v) for u, v in cc], None, [0, 1, 2])
            szyba(S, g, a, t, n, q, .4, k, mat="witraz")
            rama(S, g, a, t, n, q, .38, w=.06, mat="opaska", slupki=[(q.bounds[0] + q.bounds[2]) / 2], rygle=[4.6, 6.4])
        S.box(g, "opaska", a + UP * (Hm - .35) - t * .05, t, UP, n, L + .1, .35, .2, bez=("-z",))
        if L > 6:                                                   # przypory co ~4,4 m
            m = int(L // 4.4)
            for k in range(1, m):
                u = k * L / m
                S.box(g, "c_ceglasta", a + t * (u - .45), t, UP, n, .9, Hm - 2.4, .9, bez=("-z",))
                A0 = a + t * (u - .45) + UP * (Hm - 2.4) + n * .9; A1 = a + t * (u + .45) + UP * (Hm - 2.4) + n * .9
                S.quad(g, "dachowka", A0, A1, A1 - n * .9 + UP * 1.2, A0 - n * .9 + UP * 1.2, kier=nrm(n + UP))   # daszek przypory
                for (p_, q_) in ((A0, A0 - n * .9 + UP * 1.2), (A1, A1 - n * .9 + UP * 1.2)):
                    tri = [p_, q_, p_ - n * .9]
                    kk = -t if p_ is A0 else t
                    S.tri_list(g, "c_ceglasta", tri if np.dot(np.cross(tri[1] - tri[0], tri[2] - tri[0]), kk) > 0 else tri[::-1], kk, n, [(0, 0), (1, 1), (1, 0)], None, [0, 1, 2])
    # dach dwuspadowy nad prostokątem opisanym (nawa), szczyty na krótkich bokach z pinaklami
    R = shapely.geometry.polygon.orient(P.minimum_rotated_rectangle.buffer(-.3, join_style=2), 1.0); er = list(R.exterior.coords)
    dl = [math.hypot(xb - xa, zb - za) for (xa, za), (xb, zb) in zip(er, er[1:])]
    spadek = 58; tg = math.tan(math.radians(spadek))
    ogolny.dach_kamienica(S, g, R, [d_ > .7 * max(dl) for d_ in dl], Hm, "dachowka", spadek, "c_ceglasta", 30.0, None, 4.0, False)
    for ((xa, za), (xb, zb)), d_ in zip(zip(er, er[1:]), dl):
        if d_ > .7 * max(dl): continue
        a3 = np.array([xa, 0., za]); b3 = np.array([xb, 0., zb]); tt = (b3 - a3) / d_; nn = nrm(np.cross(UP, tt))
        if R.contains(Point(*((a3 + b3) / 2 + nn * .3)[[0, 2]])): nn = -nn
        for f in (0, .2, .4, .5, .6, .8, 1.0):
            h = Hm + tg * (min(f, 1 - f) * d_) - .2
            p0 = a3 + tt * (f * d_) - tt * .28 + nn * .05 + UP * h
            S.box(g, "c_ceglasta", p0, tt, UP, nn, .56, 1.5 if f not in (0, 1.0) else 1.0, .56)
            top = p0 + tt * .28 + nn * .28 + UP * (2.4 if f not in (0, 1.0) else 1.8)
            base = p0 + UP * (1.5 if f not in (0, 1.0) else 1.0)
            rog = [base, base + tt * .56, base + tt * .56 + nn * .56, base + nn * .56]
            for k in range(4):
                q = [rog[k], rog[(k + 1) % 4], top]
                kk = nrm((rog[k] + rog[(k + 1) % 4]) / 2 - (base + tt * .28 + nn * .28)) + UP * .3
                S.tri_list(g, "dachowka", q if np.dot(np.cross(q[1] - q[0], q[2] - q[0]), kk) > 0 else [q[0], q[2], q[1]], nrm(kk), tt, [(0, 0), (.56, 0), (.28, .8)], None, [0, 1, 2])
    # wieża: zachodni kraniec obrysu (od skrzyżowania)
    xs = [p[0] for p in P.exterior.coords]
    c = np.array([min(xs) + 3.2, 0., float(np.mean([z for x, z in P.exterior.coords if x < min(xs) + 6]))])
    r = 2.6; seg = 20
    def walec(r0, r1, y0, y1, mat, sg=seg):
        for k in range(sg):
            a0 = k / sg * 2 * math.pi; a1 = (k + 1) / sg * 2 * math.pi
            p = [c + np.array([math.cos(a) * rr, y, math.sin(a) * rr]) for (a, rr, y) in ((a0, r0, y0), (a1, r0, y0), (a1, r1, y1), (a0, r1, y1))]
            kk = nrm(np.array([math.cos((a0 + a1) / 2), (r0 - r1) / max(y1 - y0, .01), math.sin((a0 + a1) / 2)]))
            if np.linalg.norm(p[3] - p[2]) < 1e-4:
                S.tri_list(g, mat, [p[0], p[1], p[2]] if np.dot(np.cross(p[1] - p[0], p[2] - p[0]), kk) > 0 else [p[0], p[2], p[1]], kk, nrm(p[1] - p[0]), [(0, 0), (1, 0), (.5, 1)], None, [0, 1, 2])
            else:
                S.quad(g, mat, *p, kier=kk)
    walec(r + 1.25, r, 0, 6.0, "c_ceglasta")                  # pochyły cokół (zdjęcie: skośna „spódnica” do ok. 6 m)
    walec(r, r, 6.0, 31.0, "c_ceglasta")
    walec(r + .2, r + .2, 30.7, 31.2, "opaska")                       # gzyms
    for k in range(4):                                              # zegary, pary okienek
        an = k * math.pi / 2 + .3; kk = np.array([math.cos(an), 0, math.sin(an)]); tt = np.array([-kk[2], 0, kk[0]])
        zc = c + kk * (r + .03) + UP * 27.0
        for j in range(12):
            b0 = j / 12 * 2 * math.pi; b1 = (j + 1) / 12 * 2 * math.pi
            q = [zc, zc + (tt * math.cos(b0) + UP * math.sin(b0)) * .75, zc + (tt * math.cos(b1) + UP * math.sin(b1)) * .75]
            S.tri_list(g, "kiosk_bialy", q if np.dot(np.cross(q[1] - q[0], q[2] - q[0]), kk) > 0 else [q[0], q[2], q[1]], kk, tt, [(0, 0), (1, 0), (0, 1)], None, [0, 1, 2])
        S.box(g, "latarnia_czarna", zc + kk * .02 - tt * .03, tt, UP, kk, .06, .6, .02)
        for yy in (11.0, 17.5, 23.5, 29.0):
            for du in ((-.4, .4) if yy > 20 else (0,)):
                o_ = c + kk * (r + .02) + tt * du + UP * yy
                S.quad(g, "witraz", o_ - tt * .2, o_ + tt * .2, o_ + tt * .2 + UP * 1.1, o_ - tt * .2 + UP * 1.1, kier=kk)
    # hełm barokowy z miedzi (brąz): bęben, bania, latarnia z arkadami, mała bania, iglica z krzyżem
    walec(r - .15, r - .15, 31.2, 32.4, "miedz_braz")
    profil = [(r - .15, 32.4), (r + .05, 33.1), (r - .3, 34.1), (r * .62, 35.0), (r * .38, 35.5), (r * .34, 35.8)]
    for (r0, y0), (r1, y1) in zip(profil, profil[1:]): walec(r0, r1, y0, y1, "miedz_braz")
    for k in range(8):                                               # latarnia: 8 słupków z arkadami
        an = k / 8 * 2 * math.pi; p_ = c + np.array([math.cos(an), 0, math.sin(an)]) * (r * .34)
        S.box(g, "miedz_braz", p_ + UP * 35.8 - np.array([.08, 0, .08]), (1., 0, 0), UP, (0, 0, 1.), .16, 1.9, .16)
    walec(r * .28, r * .28, 35.8, 37.7, "witraz", 8)
    walec(r * .42, r * .42, 37.7, 37.95, "miedz_braz", 12)
    profil2 = [(r * .42, 37.95), (r * .5, 38.5), (r * .34, 39.3), (r * .12, 39.9), (r * .05, 40.3), (.03, 42.3)]
    for (r0, y0), (r1, y1) in zip(profil2, profil2[1:]): walec(r0, r1, y0, y1, "miedz_braz", 12)
    S.box(g, "miedz_braz", c + UP * 41.6 + np.array([-.3, 0, -.02]), (1., 0, 0), UP, (0, 0, 1.), .6, .05, .04)   # krzyż
    S.box(g, "miedz_braz", c + UP * 41.2 + np.array([-.025, 0, -.02]), (1., 0, 0), UP, (0, 0, 1.), .05, .9, .04)
    return c


def barka(S, g, P, woda_y):
    """Barka na Brdzie (OSM houseboat, zdjęcia m21–m25): stalowy kadłub (czarny z czerwonym pasem) 1,3 m nad wodą,
    pokład z desek, nadbudówka z oknami na 60% długości, sterówka, reling. Nie budynek."""
    R = P.minimum_rotated_rectangle; r = list(R.exterior.coords)[:4]
    e1 = np.subtract(r[1], r[0]); e2 = np.subtract(r[2], r[1])
    if np.linalg.norm(e1) < np.linalg.norm(e2): r = r[1:] + r[:1]; e1, e2 = e2, e1
    L, Wd = float(np.linalg.norm(e1)), float(np.linalg.norm(e2)); ax = np.array([e1[0], 0, e1[1]]) / L; bx = np.array([e2[0], 0, e2[1]]) / Wd
    o = np.array([r[0][0], 0, r[0][1]])
    y0, y1 = woda_y - .6, woda_y + 1.3
    # kadłub: obrys z zaokrąglonym dziobem (dziób = 3 m), ściany pionowe
    pts = []
    for k in range(9):                                                # dziób (półokrąg spłaszczony)
        a = math.pi / 2 - k / 8 * math.pi
        pts.append((L - 3.0 + 3.0 * math.cos(a), Wd / 2 + Wd / 2 * math.sin(a)))
    pts = [(.0, .0)] + pts[::-1] + [(.0, Wd)]
    pts = [(0.0, 0.0), (L - 3.0, 0.0)] + [(L - 3.0 + 3.0 * math.cos(a), Wd / 2 + Wd / 2 * math.sin(a)) for a in np.linspace(-math.pi / 2, math.pi / 2, 9)][1:-1] + [(L - 3.0, Wd), (0.0, Wd)]
    Q = lambda u, v, y: o + ax * u + bx * v + UP * y
    for (u0, v0), (u1, v1) in zip(pts, pts[1:] + pts[:1]):
        a3, b3 = Q(u0, v0, 0), Q(u1, v1, 0); t3 = b3 - a3
        if np.linalg.norm(t3) < 1e-4: continue
        n3 = nrm(np.cross(UP, t3)); c3 = (a3 + b3) / 2
        if Polygon(pts).contains(Point(*((np.array([np.dot(c3 + n3 * .2 - o, ax), np.dot(c3 + n3 * .2 - o, bx)])))) ): n3 = -n3
        S.quad(g, "kadlub", a3 + UP * y0, b3 + UP * y0, b3 + UP * (y1 - .35), a3 + UP * (y1 - .35), kier=n3)
        S.quad(g, "kadlub_pas", a3 + UP * (y1 - .35), b3 + UP * (y1 - .35), b3 + UP * y1, a3 + UP * y1, kier=n3)
    poly = Polygon([tuple(np.array([u, v])) for u, v in pts])
    for tri in shapely.constrained_delaunay_triangles(poly).geoms:     # pokład
        c = [Q(u, v, y1) for u, v in list(tri.exterior.coords)[:3]]
        if np.cross(c[1] - c[0], c[2] - c[0])[1] < 0: c[1], c[2] = c[2], c[1]
        S.tri_list(g, "deska", c, (0., 1., 0.), tuple(ax), [(float(np.dot(p_ - o, ax)), float(np.dot(p_ - o, bx))) for p_ in c], None, [0, 1, 2])
    # nadbudówka (salon) i sterówka
    u0, u1 = L * .12, L * .72; v0, v1 = .6, Wd - .6; h = 2.3
    for (pa, pb, kk) in ((Q(u0, v0, y1), Q(u1, v0, y1), -bx), (Q(u1, v1, y1), Q(u0, v1, y1), bx), (Q(u1, v0, y1), Q(u1, v1, y1), ax), (Q(u0, v1, y1), Q(u0, v0, y1), -ax)):
        Ls = float(np.linalg.norm(pb - pa)); ts = (pb - pa) / Ls
        m = max(1, int(Ls // 1.6)); otw = [box((k + .5) * Ls / m - .5, .8, (k + .5) * Ls / m + .5, 1.8) for k in range(m)] if Ls > 2 else []
        fasada(S, g, "kabina_biala", pa, ts, kk, Ls, h, otw, gl=.08, mat_osc="kabina_biala")
        for q in otw: szyba(S, g, pa, ts, kk, q, .06, 0, mat="witraz")
    S.box(g, "kadlub", Q(u0 - .1, v0 - .1, y1 + h), ax, UP, bx, u1 - u0 + .2, .12, v1 - v0 + .2)
    s0 = L * .74
    S.box(g, "kabina_biala", Q(s0, Wd / 2 - 1.2, y1), ax, UP, bx, 2.2, 2.6, 2.4)
    S.box(g, "kadlub", Q(s0 - .1, Wd / 2 - 1.3, y1 + 2.6), ax, UP, bx, 2.4, .12, 2.6)
    for (pa, pb) in ((Q(.2, .15, y1), Q(L - 3.2, .15, y1)), (Q(L - 3.2, Wd - .15, y1), Q(.2, Wd - .15, y1))):
        tt = nrm(pb - pa); Lr = float(np.linalg.norm(pb - pa))
        S.box(g, "latarnia_czarna", pa + UP * .9, tt, UP, nrm(np.cross(UP, tt)), Lr, .04, .04)
        for k in np.arange(0, Lr, 1.5): S.rura(g, "latarnia_czarna", pa + tt * k, pa + tt * k + UP * .9, .02, seg=4)


def _prostokat(P):
    """Prostokąt opisany: narożnik o, oś długa ax (L), krótka bx (W)."""
    r = list(P.minimum_rotated_rectangle.exterior.coords)[:4]
    e1 = np.subtract(r[1], r[0]); e2 = np.subtract(r[2], r[1])
    if np.linalg.norm(e1) < np.linalg.norm(e2): r = r[1:] + r[:1]; e1, e2 = e2, e1
    L, Wd = float(np.linalg.norm(e1)), float(np.linalg.norm(e2))
    return np.array([r[0][0], 0, r[0][1]]), np.array([e1[0], 0, e1[1]]) / L, np.array([e2[0], 0, e2[1]]) / Wd, L, Wd


def okiennica(S, g, o, t, n, u0, u1, v0, v1):
    """Drewniana okiennica z deskami i zastrzałem „Z” (IMG_2010), 4 cm przed licem."""
    P = lambda u, v, d: o + t * u + UP * v - n * d
    S.box(g, "deska_ciemna", P(u0, v0, -.02), t, UP, n, u1 - u0, v1 - v0, .04, bez=("-z",))
    for v in (v0 + .12, v1 - .22):
        S.box(g, "deska_ciemna", P(u0 + .04, v, -.06), t, UP, n, u1 - u0 - .08, .1, .03, bez=("-z",))
    A = P(u0 + .08, v0 + .2, -.06); Bq = P(u1 - .08, v1 - .3, -.06); dl = float(np.linalg.norm(Bq - A)); tt = (Bq - A) / dl
    S.box(g, "deska_ciemna", A - nrm(np.cross(tt, n)) * .05, tt, nrm(np.cross(tt, n)), n, dl, .1, .03)


def okno_lukowe(S, g, o, t, n, uc, v0, w, h, k_nr, gl=.25):
    """Małe okno z łukiem odcinkowym (spichrze): otwór, szyba, rama z krzyżem."""
    q = Polygon([(uc - w / 2, v0), (uc + w / 2, v0), (uc + w / 2, v0 + h - .12)] + [(uc + w / 2 * math.cos(a), v0 + h - .12 + .12 * math.sin(a) / max(math.sin(math.pi / 2), 1e-6)) for a in np.linspace(0, math.pi, 7)[1:-1]] + [(uc - w / 2, v0 + h - .12)])
    return q


def spichrz_lukowy(S, g, P, k0=0):
    """Spichlerz z łukowym szczytem (Grodzka 7, IMG_2009–2012): ściany boczne 3,2 m (mur pruski: czarne belki, białe
    wypełnienie), dach o przekroju ostrołuku do ok. 14 m z dachówki z rzędami małych lukarn, szczyty otynkowane (beż,
    zabrudzony dół), ciemna deska wiatrowa po łuku, małe okna z łukiem odcinkowym, okiennice „Z”, drewniane wrota."""
    o, ax, bx, L, Wd = _prostokat(P)
    a = Wd / 2; y0 = 3.2; H = min(11.5, Wd * 1.15); R = (H * H + a * a) / (2 * a)
    xs = np.linspace(a, 0, 13)
    prawa = [(x, y0 + math.sqrt(max(R * R - (x - (a - R)) ** 2, 0))) for x in xs]
    profil = [(-x, y) for x, y in prawa[::-1][:-1]][::-1] if False else [(-x, y) for x, y in prawa] [::-1][:-1] + prawa[::-1]
    profil = [(-x, y) for (x, y) in prawa] + [(x, y) for (x, y) in prawa[::-1][1:]]   # od lewego okapu przez szczyt do prawego
    profil = sorted(set(profil), key=lambda p: p[0])
    c0 = o + bx * a                                                      # oś podłużna (środek szerokości)
    Q = lambda s, x, y: c0 + ax * s + bx * x + UP * y
    # szczyty (oba końce): ściana o obrysie ostrołuku z oknami
    for (s, kier) in ((0.0, -ax), (L, ax)):
        t_ = bx if kier is ax else -bx
        sx = 1 if kier is ax else -1
        orig = Q(s, -a * sx, 0)
        obrys = Polygon([(0, 0), (Wd, 0)] + [((a + x * sx), y) for (x, y) in profil[::-1 if sx > 0 else 1]] if False else
                        [(0, 0), (Wd, 0)] + [(a + x, y) for (x, y) in sorted(profil, key=lambda p: -p[0])]).buffer(0)
        if sx < 0: obrys = shapely.affinity.scale(obrys, -1, 1, origin=(a, 0))
        otw = [box(a - 1.0, 0, a + 1.0, 3.0), okno_lukowe(S, g, orig, t_, kier, a - 2.6, .6, .9, 1.2, 0), okno_lukowe(S, g, orig, t_, kier, a + 2.6, .6, .9, 1.2, 0),
               okno_lukowe(S, g, orig, t_, kier, a - 2.3, 3.9, .9, 1.25, 0), okno_lukowe(S, g, orig, t_, kier, a + 2.3, 3.9, .9, 1.25, 0),
               box(a - .7, 3.9, a + .7, 5.2), okno_lukowe(S, g, orig, t_, kier, a - 1.0, 7.0, .8, 1.1, 0), okno_lukowe(S, g, orig, t_, kier, a + 1.0, 7.0, .8, 1.1, 0),
               okno_lukowe(S, g, orig, t_, kier, a, 10.0, .8, 1.0, 0)]
        otw = [q for q in otw if obrys.buffer(-.4).contains(q)]
        # pas: dół do 6,2 m ciemniejszy i zabrudzony, wyżej jaśniejszy tynk (IMG_2010–2011); granica falista ±15 cm
        fala = Polygon([(-1, -1), (Wd + 1, -1)] + [(Wd + 1 - k * (Wd + 2) / 12, 6.2 + .15 * math.sin(k * 1.7)) for k in range(13)])
        dol, gora = obrys.intersection(fala), obrys.difference(fala)
        fasada(S, g, "k_szarobez_dol", orig, t_, kier, Wd, y0 + H, [q for q in otw if q.intersects(dol)], gl=.25, mat_osc="k_szarobez_dol", obrys=dol.difference(unary_union([q for q in otw if q.intersects(dol) and q.intersects(gora)])) if False else dol)
        fasada(S, g, "k_szarobez", orig, t_, kier, Wd, y0 + H, [q for q in otw if q.intersects(gora)], gl=.0, obrys=gora)
        for k, q in enumerate(otw):
            u0, v0, u1, v1 = q.bounds
            if v0 < .1:                                                       # wrota dwuskrzydłowe
                S.box(g, "deska_ciemna", orig + t_ * u0 + UP * .0 - kier * .2, t_, UP, kier, u1 - u0, v1 - v0, .05)
                continue
            szyba(S, g, orig, t_, kier, q, .22, k0 + k, mat="witraz")
            rama(S, g, orig, t_, kier, q, .2, w=.06, mat="deska_ciemna", slupki=[(u0 + u1) / 2], rygle=[(v0 + v1) / 2 + .1])
            if 3.5 < v0 < 5 and (u1 - u0) < 1.0:                              # okiennice przy oknach I piętra
                okiennica(S, g, orig, t_, kier, u1 + .05, u1 + .75, v0, v1); okiennica(S, g, orig, t_, kier, u0 - .75, u0 - .05, v0, v1)
        # deska wiatrowa po łuku (ciemna, 0,35 m, 6 cm przed szczytem)
        for (x0_, y0_), (x1_, y1_) in zip(profil, profil[1:]):
            A = Q(s, x0_, y0_) + kier * .06; Bq = Q(s, x1_, y1_) + kier * .06
            tt = nrm(Bq - A); nn = nrm(np.cross(kier, tt))
            if nn[1] < 0: nn = -nn
            S.quad(g, "belka_ciemna", A - nn * .5, Bq - nn * .5, Bq + nn * .08, A + nn * .08, kier=kier)
    # dach: wyciągnięty ostrołuk (okap 0,3 m na szczytach)
    for (x0_, y0_), (x1_, y1_) in zip(profil, profil[1:]):
        p = [Q(-.3, x0_, y0_ + .02), Q(-.3, x1_, y1_ + .02), Q(L + .3, x1_, y1_ + .02), Q(L + .3, x0_, y0_ + .02)]
        nn = nrm(bx * (x0_ + x1_) / 2 + UP * ((y0_ + y1_) / 2 - y0 + .5))
        S.quad(g, "dachowka", *p, kier=nn)
    # ściany boczne 3,2 m z murem pruskim i małymi oknami
    for (sx) in (-1, 1):
        a3 = Q(0 if sx > 0 else L, sx * a, 0); t_ = ax if sx > 0 else -ax; n_ = bx * sx
        okn = [(u - .35, u + .35, .9, 1.9, "okno_t", 0) for u in np.arange(1.6, L - 1.0, 2.6)]
        fasada(S, g, "k_wypelnienie", a3, t_, n_, L, y0 + .2, [box(u0, v0, u1, v1) for (u0, u1, v0, v1, r, pp) in okn], gl=.15, mat_osc="k_wypelnienie")
        for k, (u0, u1, v0, v1, r, pp) in enumerate(okn):
            szyba(S, g, a3, t_, n_, box(u0, v0, u1, v1), .14, k0 + 20 + k, mat="witraz")
        B.S = S
        ogolny.mur_pruski(S, g, a3, t_, n_, L, okn, y0, 3.0, 1, y0, 2.6, 0.0, int(L // 2.6))
        # lukarny w rzędach na połaci (IMG_2009: 4 rzędy)
        for j, yy in enumerate((y0 + 2.0, y0 + 4.4, y0 + 6.6, y0 + 8.6)):
            if yy > y0 + H - 1.2: break
            xx = next((x for x, y in prawa if y >= yy), a * .3)
            for s in np.arange(1.4 + (j % 2) * 1.1, L - 1.0, 2.2):
                F = Q(s, sx * (xx + .02), yy); tt = ax; nn = bx * sx
                w, h = .9, .8
                S.quad(g, "deska_ciemna", F - tt * w / 2, F + tt * w / 2, F + tt * w / 2 + UP * h, F - tt * w / 2 + UP * h, kier=nn)
                S.quad(g, "witraz", F - tt * .28 + nn * .01 + UP * .15, F + tt * .28 + nn * .01 + UP * .15, F + tt * .28 + nn * .01 + UP * .62, F - tt * .28 + nn * .01 + UP * .62, kier=nn)
                T = F + UP * (h + .35) - nn * .3
                for (p0, p1) in ((F - tt * (w / 2 + .12) + UP * (h - .05) + nn * .15, T), (T, F + tt * (w / 2 + .12) + UP * (h - .05) + nn * .15)):
                    pass
                L0 = F - tt * (w / 2 + .12) + UP * (h - .05) + nn * .15; R0 = F + tt * (w / 2 + .12) + UP * (h - .05) + nn * .15
                L1 = L0 - nn * 1.3 + UP * .1; R1 = R0 - nn * 1.3 + UP * .1; T0 = F + UP * (h + .32) + nn * .15; T1 = T0 - nn * 1.3
                S.quad(g, "dachowka", L0, T0, T1, L1, kier=nrm(UP - tt * .6)); S.quad(g, "dachowka", T0, R0, R1, T1, kier=nrm(UP + tt * .6))
                S.tri_list(g, "deska_ciemna", [F - tt * w / 2 + UP * h, F + tt * w / 2 + UP * h, F + UP * (h + .3) + nn * .01], nn, tt, [(0, 0), (1, 0), (.5, .3)], None, [0, 1, 2])


def spichrz_ceglany(S, g, P, k0=0):
    """Spichlerz ceglany ze schodkowym szczytem (Grodzka 9, IMG_2011–2012): dwie dolne kondygnacje w beżowym tynku
    z drewnianymi wrotami-okiennicami, wyżej czerwona cegła z małymi oknami z łukiem odcinkowym, gzyms, szczyt schodkowy
    z kotwami „X”, dach 58° z dachówki."""
    o, ax, bx, L, Wd = _prostokat(P)
    a = Wd / 2; Hw = 10.8; tg = math.tan(math.radians(58)); Hs = a * tg
    c0 = o + bx * a
    Q = lambda s, x, y: c0 + ax * s + bx * x + UP * y
    # ściany boczne (przy sąsiadach — gładka cegła, bez okien)
    for sx in (-1, 1):
        a3 = Q(0 if sx > 0 else L, sx * a, 0); t_ = ax if sx > 0 else -ax
        fasada(S, g, "c_czerwona", a3, t_, bx * sx, L, Hw, [])
    for (s, kier) in ((0.0, -ax), (L, ax)):
        t_ = bx if kier is ax else -bx; sx = 1 if kier is ax else -1
        orig = Q(s, -a * sx, 0)
        # schodkowy obrys szczytu: stopnie co 1,4 m wysokości
        pts = [(0, 0), (Wd, 0), (Wd, Hw)]
        stopnie = int(Hs // 1.4)
        for k in range(1, stopnie + 1):
            y = Hw + k * 1.4; x_in = (y - Hw) / tg
            pts += [(Wd - x_in + .25, y - 1.4 + .0), (Wd - x_in + .25, y)] if False else [(Wd - max(x_in - .35, 0), Hw + (k - 1) * 1.4 + .0), (Wd - max(x_in - .35, 0), y)]
        szczyt = Hw + Hs + .4
        pts += [(a + .45, szczyt), (a - .45, szczyt)]
        lewa = [(Wd - x, y) for (x, y) in pts[3:-2]][::-1]
        pts += lewa + [(0, Hw)]
        obrys = Polygon(pts).buffer(0)
        dol = box(0, 0, Wd, 5.6)
        otw = [box(.6, 0, 2.2, 3.2), box(Wd - 2.2, 0, Wd - .6, 3.2), luk_pol(a - .7, a + .7, 0, 2.6, 8)]
        for v, n_ in ((3.9, 4), (6.6, 4), (9.0, 4), (11.8, 4), (14.2, 3), (16.4, 1)):
            for k in range(n_):
                uc = a + (k - (n_ - 1) / 2) * (Wd / 5.2)
                q = okno_lukowe(S, g, orig, t_, kier, uc, v, .85, 1.25, 0)
                if obrys.buffer(-.5).contains(q): otw.append(q)
        gora = obrys.difference(dol)
        fasada(S, g, "c_czerwona", orig, t_, kier, Wd, szczyt, [q for q in otw if q.bounds[1] >= 5.6], gl=.25, mat_osc="c_czerwona", obrys=gora)
        fasada(S, g, "k_szarobez_dol", orig, t_, kier, Wd, 5.6, [q for q in otw if q.bounds[1] < 5.6], gl=.25, mat_osc="k_szarobez_dol", obrys=dol)
        S.box(g, "c_czerwona", orig + t_ * -.04 + UP * 5.55, t_, UP, -kier, Wd + .08, .18, .1, bez=("-z",))    # gzyms nad tynkiem
        S.box(g, "c_czerwona", orig + t_ * -.05 + UP * (Hw - .05), t_, UP, -kier, Wd + .1, .25, .15, bez=("-z",))   # gzyms
        for k, q in enumerate(otw):
            u0, v0, u1, v1 = q.bounds
            if v0 < .1:
                S.box(g, "deska_ciemna", orig + t_ * u0 - kier * .2, t_, UP, kier, u1 - u0, v1 - v0, .05); continue
            szyba(S, g, orig, t_, kier, q, .22, k0 + k, mat="witraz")
            rama(S, g, orig, t_, kier, q, .2, w=.06, mat="kiosk_bialy", slupki=[(u0 + u1) / 2])
        for (u, v) in ((a - 2.6, Hw + 1.2), (a + 2.6, Hw + 1.2), (a, Hw + 3.6), (.9, 9.8), (Wd - .9, 9.8), (a - 1.2, 7.9), (a + 1.2, 7.9)):
            p_ = orig + t_ * u + UP * v - kier * -.03                           # kotwy „X”
            for sgn in (1, -1):
                A = p_ - t_ * .25 - UP * .25 * sgn; Bq = p_ + t_ * .25 + UP * .25 * sgn; tt = nrm(Bq - A)
                S.box(g, "latarnia_czarna", A - nrm(np.cross(tt, kier)) * .025 * 0, tt, nrm(np.cross(kier, tt)), -kier, .72, .05, .03)
    # dach dwuspadowy
    for sx in (-1, 1):
        p = [Q(-.05, sx * (a + .3), Hw - .3 * tg), Q(L + .05, sx * (a + .3), Hw - .3 * tg), Q(L + .05, 0, Hw + Hs), Q(-.05, 0, Hw + Hs)]
        S.quad(g, "dachowka", *p, kier=nrm(bx * sx + UP))


SAVOY = dict(kond=6, p0=5.4, hk=3.5, krok=3.9, tynk="k_bialy", kamien="s_jasny", h_kamien=9.2, h_luk=4.9, kolumny=True,
             tralki=(3, 4), attyka_tralki=True, dach_mat="dachowka", spadek=60, kal_max=3.2, lukarny=True)
ZWEININGER = dict(kond=4, p0=5.0, hk=3.8, krok=4.2, tynk="k_piaskowy", kamien="kb_piaskowy", h_kamien=5.0, h_luk=4.4, kolumny=False,
                  wykusze=True, miedz_attyka=True, dach_mat="dachowka", spadek=50, kal_max=3.5, lukarny=False, szyld="szyld_klub")


def zbuduj(budynki_osm, publ, woda_y=-2.4):
    """Siatka trzech budynków skrzyżowania. budynki_osm: słownik osm → shapely (metry od kotwicy)."""
    from kit import Siatka
    S = Siatka(); B.S, stare = S, B.S
    try:
        if OSM_SAVOY in budynki_osm: budynek_podcienia(S, "savoy", budynki_osm[OSM_SAVOY], publ, SAVOY)
        if OSM_ZWEININGER in budynki_osm:
            Q, H, fr = budynek_podcienia(S, "zweininger", budynki_osm[OSM_ZWEININGER], publ, ZWEININGER)
            # wieżyczka nad zaokrąglonym narożnikiem (najbliżej środka skrzyżowania)
            c = min((np.array(p) for p in Q.exterior.coords), key=lambda p: np.hypot(p[0] + 30, p[1] + 15))
            cc = np.array([c[0], 0., c[1]]); kier = nrm(np.array([Q.centroid.x - c[0], 0., Q.centroid.y - c[1]]))
            wiezyczka(S, "zweininger", cc + kier * 2.6, 2.2, H + .65, 4.2)
        if OSM_KOSCIOL in budynki_osm: kosciol(S, "kosciol", budynki_osm[OSM_KOSCIOL])
        if OSM_BARKA in budynki_osm: barka(S, "barka", budynki_osm[OSM_BARKA], woda_y)
        if OSM_SPICHRZ_LUK in budynki_osm: spichrz_lukowy(S, "spichrz7", budynki_osm[OSM_SPICHRZ_LUK])
        if OSM_SPICHRZ_CEGLA in budynki_osm: spichrz_ceglany(S, "spichrz9", budynki_osm[OSM_SPICHRZ_CEGLA])
    finally:
        B.S = stare
    return S
