"""MIASTO R1 — balkon z DJ-em na Starym Rynku.

24.11, 14:09 właściciel (zdjęcie z nagrania „Stary Rynek w Bydgoszczy” + zaznaczony budynek na podglądzie 02_rynek_z_zachodu):
„zrób na tym budynku […] balkon, gdzie na balkonie jest sprzęt DJ, głośniki i muzyka leci”.

Zdjęcie: kamienica w pierzei wschodniej Rynku — 4 kondygnacje, 3 osie okien, ochra nad kremowym parterem, ozdobne
naczółki; balkon na II piętrze w środkowej osi (drzwi balkonowe), na balustradzie biały baner z napisem, za nim konsoleta
DJ, po bokach kolumny na statywach, różowo-fioletowe światła. Budynek wskazany przez właściciela: OSM w134510002
(adres OSM Mostowa 27 — kamienica przechodzi przez kwartał, front 6,9 m od Rynku).

Napis na banerze bez prawdziwej nazwy lokalu (zasada właściciela: bez prawdziwych marek i logo) — 14:36 właściciel:
„Balkon BDG”, „Zrób”; 14:38 „Chomętowo stare czasy, pod spodem daj” → „BALKON BDG — CHOMĘTOWO STARE CZASY —”.
Muzyka w grze: BraxClaudeMuzyka (ten sam utwór co dyskoteka osiedla, industrial_pulse_petla.ogg — 138 BPM),
głośniki = kolumny na balkonie, „sala” = plac przed kamienicą; światła pulsują w rytm (BraxPulsSwiatla).
"""
import math

import numpy as np
from shapely.geometry import Point

from kit import UP, nrm

OSM = "w134510002"                       # budynek wskazany przez właściciela (podgląd 02, piksel ok. 1200 × 400)
UTWOR, BPM, FAZA = "industrial_pulse_petla.ogg", 138.0, .18      # tempo i pierwsze uderzenie zmierzone z pliku (obwiednia)
P0, HK, KOND = 4.6, 3.8, 4               # parter 4,6 m, piętra 3,8 m (zdjęcie: wysoki parter z łukiem lokalu)
PIETRO = 2                               # balkon na II piętrze (zdjęcie: nad oknami I piętra, pod oknami III)


def obrys(poly):
    """Ten sam obrys co w ogolny.zbuduj (buffer 0, uproszczenie 15 cm, przeciwnie do zegara) — ściana balkonu = ściana budynku."""
    from shapely.geometry.polygon import orient
    P = poly.buffer(0)
    if P.geom_type == "MultiPolygon": P = max(P.geoms, key=lambda q: q.area)
    return orient(P.simplify(.15), 1.0)


def sciana_od_rynku(P, rynek):
    """Bok obrysu zwrócony do placu: (A, B, t, n, L) w metrach (A, B — 3D na y=0), n — normalna na zewnątrz."""
    pts = list(P.exterior.coords); best = None
    for (x0, z0), (x1, z1) in zip(pts, pts[1:]):
        L = math.hypot(x1 - x0, z1 - z0)
        if L < 3.0: continue
        t = np.array([x1 - x0, 0., z1 - z0]) / L; n = nrm(np.cross(UP, t))
        m = np.array([(x0 + x1) / 2, 0., (z0 + z1) / 2])
        if P.contains(Point(*(m + n * .3)[[0, 2]])): n = -n
        d = rynek.distance(Point(*(m + n * 4.0)[[0, 2]])) if rynek is not None else 0.0
        if best is None or d < best[0]: best = (d, np.array([x0, 0., z0]), np.array([x1, 0., z1]), t, n, L)
    return best[1:]


def krazek(S, g, mat, c, n, r, seg=16):
    """Płaski krążek (talerz odtwarzacza, membrana, soczewka) o środku c, przodem w stronę n."""
    n = nrm(n); p = nrm(np.cross(n, UP if abs(n[1]) < .9 else np.array([1., 0., 0.]))); q = np.cross(n, p)
    pts = [c] + [c + (p * math.cos(2 * math.pi * k / seg) + q * math.sin(2 * math.pi * k / seg)) * r for k in range(seg)]
    idx = []
    for k in range(seg):
        a, b = 1 + k, 1 + (k + 1) % seg
        idx += [0, a, b] if np.dot(np.cross(pts[a] - pts[0], pts[b] - pts[0]), n) > 0 else [0, b, a]
    uv = [(float(np.dot(x - c, p)), float(np.dot(x - c, q))) for x in pts]
    S.tri_list(g, mat, pts, n, p, uv, None, idx)


def zbuduj(S, P, rynek, g="wyposazenie_balkon_dj"):
    """Balkon, balustrada, baner, konsoleta, kolumny i światła. Zwraca wpis "muzyka" do rynek_punkty.json."""
    A, B, t, n, L = sciana_od_rynku(P, rynek)
    y2 = P0 + (PIETRO - 1) * HK                                   # poziom podłogi II piętra (drzwi balkonowe od y2 + 0,05)
    c = A + t * L / 2
    Q = lambda u, v, w: c + t * u + UP * v + n * w
    W, D = 2.6, .95                                               # balkon 2,6 × 0,95 m
    # płyta (wierzch 4 cm pod progiem drzwi) i dwa wsporniki schodkowe
    S.box(g, "opaska", Q(-W / 2, y2 - .16, 0), t, UP, n, W, .2, D, bez=("-z",))
    for u in (-W / 2 + .3, W / 2 - .3):
        S.box(g, "opaska", Q(u - .11, y2 - .72, 0), t, UP, n, .22, .56, .52, bez=("-z", "+y"))
        S.box(g, "opaska", Q(u - .09, y2 - 1.0, 0), t, UP, n, .18, .28, .3, bez=("-z", "+y"))
    # balustrada kuta: front 7 cm od krawędzi płyty, boki; pochwyt i dolna listwa
    yb, hb, wf = y2 + .04, .98, D - .07
    S.quad(g, "balustrada_kuta", Q(-W / 2 + .05, yb, wf), Q(W / 2 - .05, yb, wf), Q(W / 2 - .05, yb + hb, wf), Q(-W / 2 + .05, yb + hb, wf), kier=n, uv_o=Q(-W / 2, yb, wf))
    for s in (-1, 1):
        u = s * (W / 2 - .05)
        S.quad(g, "balustrada_kuta", Q(u, yb, .05), Q(u, yb, wf), Q(u, yb + hb, wf), Q(u, yb + hb, .05), kier=t * s, uv_o=Q(u, yb, 0))
        S.box(g, "latarnia_czarna", Q(u - .025, yb + hb, .03), t, UP, n, .05, .05, wf - .075)         # pochwyt boczny (do słupka)
        S.box(g, "latarnia_czarna", Q(u - .03, yb, wf - .03), t, UP, n, .06, hb + .06, .06)          # słupek narożny
    S.box(g, "latarnia_czarna", Q(-W / 2 + .03, yb + hb + .005, wf - .035), t, UP, n, W - .06, .05, .07)   # pochwyt (1 cm w słupkach)
    S.box(g, "latarnia_czarna", Q(-W / 2 + .05, yb + .06, wf - .02), t, UP, n, W - .1, .04, .04)        # dolna listwa
    # baner na balustradzie od placu (płótno, napis na UV1 0..1); tył gładki
    bu, bv0, bv1 = 1.15, yb + .22, yb + .88
    # +u w prawo dla patrzącego z placu (napis nie w lustrze). Układ gry jak Unity (lewoskrętny: x wschód, y góra, z północ),
    # więc „w prawo” przy patrzeniu w stronę f to UP × f (podgląd odwraca z i pokazuje to samo)
    sp = 1.0 if np.dot(t, np.cross(UP, -n)) > 0 else -1.0
    S.quad(g, "dj_baner", Q(-bu * sp, bv0, wf + .045), Q(bu * sp, bv0, wf + .045), Q(bu * sp, bv1, wf + .045), Q(-bu * sp, bv1, wf + .045),
           kier=n, uv1=[(0, 0), (1, 0), (1, 1), (0, 1)])
    S.quad(g, "dj_plotno", Q(-bu, bv0, wf + .04), Q(bu, bv0, wf + .04), Q(bu, bv1, wf + .04), Q(-bu, bv1, wf + .04), kier=-n)
    for u in (-bu + .05, bu - .05):                                # opaski mocujące do pochwytu
        S.box(g, "dj_czarny", Q(u - .02, bv1 - .02, wf - .03), t, UP, n, .04, yb + hb - bv1 + .08, .085)
    # konsoleta: stół 1,5 × 0,5 m, wys. 0,95 (front z czarnej tkaniny), na blacie dwa odtwarzacze z talerzami i mikser z diodami
    ub, w0, w1, hs = .75, .22, .72, .95
    S.box(g, "dj_czarny", Q(-ub, yb, w0), t, UP, n, 2 * ub, hs, w1 - w0, bez=("-y",))
    S.quad(g, "dj_led", Q(-ub + .04, yb + hs - .1, w1 + .006), Q(ub - .04, yb + hs - .1, w1 + .006), Q(ub - .04, yb + hs - .06, w1 + .006),
           Q(-ub + .04, yb + hs - .06, w1 + .006), kier=n)                                          # listwa LED na froncie
    top = yb + hs
    for s in (-1, 1):
        S.box(g, "dj_szary", Q(s * .5 - .17, top, w0 + .06), t, UP, n, .34, .06, .38, bez=("-y",))
        krazek(S, g, "dj_metal", Q(s * .5, top + .062, w0 + .27), UP, .13)
        krazek(S, g, "dj_led", Q(s * .5, top + .064, w0 + .27), UP, .035, seg=10)
    S.box(g, "dj_szary", Q(-.13, top, w0 + .07), t, UP, n, .26, .075, .36, bez=("-y",))
    for k in range(4):                                             # wskaźniki poziomu na mikserze
        u = -.075 + k * .05
        S.quad(g, "dj_led", Q(u - .012, top + .077, w0 + .16), Q(u + .012, top + .077, w0 + .16), Q(u + .012, top + .077, w0 + .34),
               Q(u - .012, top + .077, w0 + .34), kier=UP)
    # kolumny na statywach po bokach konsolety, przodem do placu; na nich głowice świateł
    glosniki = []
    for s in (-1, 1):
        u = s * .98; ws = .55
        S.rura(g, "dj_metal", Q(u, yb, ws), Q(u, yb + 1.2, ws), .025, seg=8)
        for k in range(3):                                         # trzy nogi statywu
            f = 2 * math.pi * k / 3 + .5
            S.rura(g, "dj_metal", Q(u, yb + .45, ws), Q(u + math.cos(f) * .26, yb + .01, ws + math.sin(f) * .2), .015, seg=6)
        o = Q(u - .21, yb + 1.2, ws - .19)
        S.box(g, "dj_czarny", o, t, UP, n, .42, .68, .38)
        krazek(S, g, "dj_membrana", Q(u, yb + 1.2 + .25, ws + .19 + .006), n, .15)
        krazek(S, g, "dj_metal", Q(u, yb + 1.2 + .25, ws + .19 + .008), n, .045, seg=10)
        krazek(S, g, "dj_membrana", Q(u, yb + 1.2 + .55, ws + .19 + .006), n, .06, seg=12)
        S.box(g, "dj_czarny", Q(u - .1, yb + 1.88, ws - .1), t, UP, n, .2, .22, .2, bez=("-y",))      # głowica światła
        krazek(S, g, "dj_led", Q(u, yb + 1.99, ws + .1 + .006), n, .07, seg=12)
        glosniki.append([round(float(x), 2) for x in Q(u, yb + 1.5, ws)])
    # „sala” muzyki: wschodnia połowa placu przed kamienicą — do 45 m od frontu, ±22 m wzdłuż (dalej i za budynkami
    # stłumiona, cichnie do 40 m); prostokąt osiowy (x, z) w metrach
    rog = [c + t * a + n * b for a in (-22, 22) for b in (.5, 45)]
    x0, x1 = min(p[0] for p in rog), max(p[0] for p in rog); z0, z1 = min(p[2] for p in rog), max(p[2] for p in rog)
    # lasery (24.11, 14:40 właściciel: „daj lasery, że w nocy będą odpalać, ruszające się i migające”): dwie głowice
    # laserowe na narożnych słupkach balustrady od placu, czarna obudowa 0,22 × 0,14 × 0,3 m, okienko wylotu ku placowi;
    # wachlarze promieni, ruch i miganie liczy gra (BraxLaseryBalkonu) — tylko nocą (słońce nisko albo zgaszone)
    lasery = []
    for s_ in (-1, 1):
        u = s_ * (W / 2 - .05)
        o = Q(u - .11, yb + hb + .065, wf - .15)                       # 5 mm nad słupkiem (bez wspólnej płaszczyzny)
        S.box(g, "dj_czarny", o, t, UP, n, .22, .14, .3)
        S.quad(g, "dj_led", Q(u - .05, yb + hb + .09, wf + .156), Q(u + .05, yb + hb + .09, wf + .156), Q(u + .05, yb + hb + .17, wf + .156),
               Q(u - .05, yb + hb + .17, wf + .156), kier=n)
        lasery.append([round(float(x), 2) for x in Q(u, yb + hb + .13, wf + .16)])
    wpis = [UTWOR, round(x0, 2), round(z0, 2), round(x1, 2), round(z1, 2), 14.0, .7] + glosniki[0] + glosniki[1] + ["dj_led", BPM, FAZA] \
        + lasery[0] + lasery[1] + [round(float(n[0]), 4), round(float(n[2]), 4)]
    return wpis, (A, B, t, n, L)
