"""MIASTO R1 — osiedle bydgoskie: budki z jedzeniem na parkingu przed marketem (właściciel 23.11, 18:32: „tam stoją budki
z jedzeniem na parkingu… Bosna stoi… burger, budka”; zdjęcie z internetu w 17:25 — przyczepa gastronomiczna przy wejściu).
Nazwy ogólne (BOSNA, BURGERY) — bez prawdziwych marek (ZASADY 7.4). Szyldy: naklejki (tekstury_32a.szyldy) na lakierze.
"""
import math
import numpy as np
from kit import UP, nrm

UV1 = [(0, 0), (1, 0), (1, 1), (0, 1)]


def _B(S, g, m, o, ex, ez, sx, sy, sz, bez=()):
    ex, ez = nrm(ex), nrm(ez)
    S.box(g, m, o - ex * sx / 2 - ez * sz / 2, ex, UP, ez, sx, sy, sz, bez=bez)


def _szyld(S, g, m, c, n, w, h):
    """Prostokąt z naklejką, środek c, przodem do n (czytelny dla patrzącego na budkę)."""
    r = nrm(np.cross(UP, -n))
    S.quad(g, m, c - r * w / 2, c + r * w / 2, c + r * w / 2 + UP * h, c - r * w / 2 + UP * h, uv1=UV1, kier=n)


def stolik(S, g, o):
    S.rura(g, "stal", o, o + UP * 1.05, .035, seg=8)
    S.rura(g, "stal", o + UP * .02, o + UP * .03, .28, seg=10)
    S.rura(g, "kiosk_blat", o + UP * 1.05, o + UP * 1.09, .32, seg=14)


def lodziarnia(S, o, t, n, g="budki"):
    """Lodziarnia (zdjęcie właściciela 18:47): żółty kontener 5,6 × 2,6 m z dużą witryną i drzwiami, markiza w paski nad
    witryną, drewniany podest ze stopniem, dwie donice z wysoką trawą ozdobną, napis z liter na dachu (bez nazwy firmy)."""
    W, D, H = 5.6, 2.6, 2.75
    c = o - n * D / 2
    _B(S, g, "kontener_zolty_rel", c + UP * .25, t, n, W, H, D)
    _B(S, g, "deska", o + n * .8 + UP * .0, t, n, W * .72, .25, 1.6)                     # podest
    _B(S, g, "deska", o + n * 1.8 + UP * .0, t, n, W * .5, .12, .5)                       # stopień
    for (u0, u1, v0, v1) in ((-2.2, .7, .55, 2.35), (.85, 2.15, .55, 2.35)):         # witryna i drzwi
        _B(S, g, "pcv", o + t * ((u0 + u1) / 2) + UP * (v0 - .06), t, n, u1 - u0 + .12, v1 - v0 + .12, .06)
        _B(S, g, "szyba_drzwi", o + t * ((u0 + u1) / 2) + n * .035 + UP * v0, t, n, u1 - u0, v1 - v0, .02)
    a = o + UP * 2.55 - t * 2.3; b = o + UP * 2.55 + t * .8
    S.quad(g, "markiza_pasy", a, b, b + n * .6 - UP * .25, a + n * .6 - UP * .25, kier=nrm(n + UP))
    S.quad(g, "markiza_pasy", a + n * .6 - UP * .25, b + n * .6 - UP * .25, b, a, kier=nrm(-n - UP))
    _szyld(S, g, "szyld_lody", o + UP * (H + .3) - n * .4, n, W * .8, .8)
    for s_ in (-1, 1):                                                                     # donice z trawą ozdobną
        p = o + n * .9 + t * s_ * (W / 2 + .2)
        _B(S, g, "kiosk_dach", p, t, n, 1.2, .55, .6)
        for k in range(14):
            f = k * 2.39; r = .12 + .3 * ((k * .618) % 1)
            q = p + UP * .5 + t * math.cos(f) * r * .9 + n * math.sin(f) * r * .4
            S.rura(g, "trawa_ozdobna", q, q + UP * (1.0 + .4 * ((k * .37) % 1)) + t * math.cos(f) * .35 + n * math.sin(f) * .2, .07, seg=4)


def burger_kontener(S, o, t, n, g="budki"):
    """Budka z burgerami (zdjęcie 18:47): żółty kontener 4,0 × 2,4 m z blachy falistej, dwa okienka wydawcze, ciemny billboard
    z napisem na dachu, kosz, donica; obok słup z obręczą i okrągłym szyldem i girlanda żarówek.
    25.11 (właściciel 15:42: „trzynastolatki robią lepsze w Minecrafcie”): kontener z blachy falistej z ramą i narożnikami,
    okienka w ramie ze stali nierdzewnej z blatem, roletą zwiniętą w kasecie, oświetlonym wnętrzem (lada, okap, tablica menu),
    markizy w pasy na wspornikach, tablice menu obok okienek, drzwi z boku ze stopniem, dwa stoliki barowe."""
    W, D, H = 4.0, 2.4, 2.6
    c = o - n * D / 2
    r = nrm(np.cross(UP, -n))
    _B(S, g, "kontener_falisty", c + UP * .05, t, n, W, H - .05, D, bez=("+z",))
    tp = nrm(np.cross(n, UP))                                                    # ściana frontu z otworami okienek
    S.sciana(g, "kontener_falisty", o + t * (W / 2) + UP * .05, tp, W, H - .05,
             [(W / 2 - u - 1.15 / 2, W / 2 - u + 1.15 / 2, .95 - .05, 1.95 - .05) for u in (-1.0, .9)])
    for s_ in (-1, 1):                                                          # rama i narożniki kontenera
        for d_ in (.0, -D):
            _B(S, g, "kontener_rama", o + t * s_ * (W / 2 - .06) + n * (d_ + .06 if d_ == 0 else d_ + .06) - n * 0, t, n, .14, H, .14)
    for y_ in (.0, H - .12):
        _B(S, g, "kontener_rama", o + n * .02 + UP * y_, t, n, W + .02, .14, .1)
    for u in (-1.0, .9):
        w_, h0, h1 = 1.15, .95, 1.95
        _B(S, g, "kiosk_wnetrze_jasne", o + t * u - n * .9 + UP * h0, t, n, w_, h1 - h0, .04)             # tył wnętrza (oświetlone)
        _B(S, g, "tablica_menu", o + t * u - n * .86 + UP * (h0 + .45), t, n, w_ * .8, .45, .02)
        _B(S, g, "kiosk_blat", o + t * u - n * .6 + UP * (h0 - .05), t, n, w_, .9, .6)                    # lada w środku
        for (q0, q1) in ((-w_ / 2, w_ / 2),):
            S.quad(g, "kiosk_wnetrze_jasne", o + t * (u + q0) + UP * h0, o + t * (u + q0) - n * .9 + UP * h0, o + t * (u + q0) - n * .9 + UP * h1, o + t * (u + q0) + UP * h1, kier=r * 1 if False else t)
            S.quad(g, "kiosk_wnetrze_jasne", o + t * (u + q1) + UP * h0, o + t * (u + q1) + UP * h1, o + t * (u + q1) - n * .9 + UP * h1, o + t * (u + q1) - n * .9 + UP * h0, kier=-t)
            S.quad(g, "kiosk_wnetrze_jasne", o + t * (u + q0) + UP * h1, o + t * (u + q1) + UP * h1, o + t * (u + q1) - n * .9 + UP * h1, o + t * (u + q0) - n * .9 + UP * h1, kier=-UP)
        for (x0, x1, y0, y1) in ((-w_ / 2 - .06, -w_ / 2, h0 - .06, h1 + .06), (w_ / 2, w_ / 2 + .06, h0 - .06, h1 + .06),
                                 (-w_ / 2 - .06, w_ / 2 + .06, h1, h1 + .06)):
            _B(S, g, "stal_nierdzewna", o + t * (u + (x0 + x1) / 2) + n * .02 + UP * y0, t, n, x1 - x0, y1 - y0, .05)
        _B(S, g, "stal_nierdzewna", o + t * u + n * .17 + UP * (h0 - .06), t, n, w_ + .14, .05, .34)        # blat wydawczy
        _B(S, g, "kiosk_dach", o + t * u + n * .08 + UP * (h1 + .08), t, n, w_ + .14, .18, .16)            # kaseta rolety
        a = o + t * (u - .66) + UP * 2.32; b = o + t * (u + .66) + UP * 2.32
        S.quad(g, "markiza_pasy", a, b, b + n * .6 - UP * .28, a + n * .6 - UP * .28, kier=nrm(n + UP))
        S.quad(g, "markiza_pasy", a + n * .6 - UP * .28, b + n * .6 - UP * .28, b, a, kier=nrm(-n - UP))
        for s2 in (-1, 1):
            S.rura(g, "stal", o + t * (u + s2 * .62) + UP * 2.32, o + t * (u + s2 * .62) + n * .58 + UP * 2.05, .012, seg=4)
        S.rura(g, "lampa", o + t * u + n * .3 + UP * 2.16, o + t * u + n * .3 + UP * 2.1, .05, seg=8)       # lampka pod markizą
    _B(S, g, "tablica_menu", o + t * (-W / 2 + .2) + n * .03 + UP * 1.0, t, n, .3, .9, .02)                 # tablice menu przy okienkach
    _B(S, g, "tablica_menu", o + t * (W / 2 - .2) + n * .03 + UP * 1.0, t, n, .3, .9, .02)
    sd = o + t * (W / 2 + .01) - n * (D * .55)                                                           # drzwi z boku
    S.quad(g, "drzwi_stal", sd - n * .45 + UP * .1, sd + n * .45 + UP * .1, sd + n * .45 + UP * 2.1, sd - n * .45 + UP * 2.1, kier=t)
    _B(S, g, "beton_schody", sd + t * .35, n, t, 1.1, .15, .7)
    _B(S, g, "kiosk_dach", c + UP * H, t, n, W + .2, .1, D + .2)
    _B(S, g, "kiosk_dach", o + UP * (H + .1) - n * .2, t, n, W + .3, 1.3, .12)                # billboard
    _szyld(S, g, "szyld_burger", o + UP * (H + .15) - n * .13, n, W + .2, 1.2)
    S.rura(g, "opona", o + n * .5 + t * (W / 2 + .4), o + n * .5 + t * (W / 2 + .4) + UP * .95, .25, seg=10)
    for k in (-1, 1):                                                                                     # stoliki barowe
        stolik(S, g, o + n * 2.6 + t * k * 1.4)
    # słup z obręczą i okrągłym szyldem
    s0 = o + n * 4.5 - t * (W / 2 + 2.0)
    S.rura(g, "stal", s0, s0 + UP * 9.0, .14, seg=10)
    kc = s0 + UP * 10.6 - t * 1.5; R = 1.6
    for k in range(24):
        f0, f1 = 2 * math.pi * k / 24, 2 * math.pi * (k + 1) / 24
        S.rura(g, "stal", kc + t * math.cos(f0) * R + UP * math.sin(f0) * R, kc + t * math.cos(f1) * R + UP * math.sin(f1) * R, .06, seg=6)
    S.rura(g, "pole_logo", kc - n * .15, kc + n * .15, 1.05, seg=24)
    S.rura(g, "market_niebieski", kc - n * .12, kc + n * .12, 1.15, seg=24)
    # girlanda: od słupa do latarni-masztu po drugiej stronie, zwis 0,8 m, żarówki co 0,9 m
    m0 = s0 + UP * 5.5; m1 = o + n * 6 + t * (W / 2 + 9) ; S.rura(g, "stal", m1, m1 + UP * 5.5, .08, seg=8); m1 = m1 + UP * 5.5
    L_ = float(np.linalg.norm(m1 - m0)); N = int(L_ / .9)
    pp = [m0 + (m1 - m0) * (k / N) - UP * .8 * math.sin(math.pi * k / N) for k in range(N + 1)]
    for p0, p1 in zip(pp, pp[1:]): S.rura(g, "stal", p0, p1, .008, seg=3)
    for p0 in pp[1:-1]: S.rura(g, "lampa", p0 - UP * .05, p0 - UP * .14, .035, seg=6)


def bosna(S, o, t, n, g="budki"):
    """Budka z bosną 2,6 × 2,0 m, okienko wydawcze z ladą, markiza, szyld na dachu, dwa wysokie stoliki, kosz."""
    W, D, H = 2.6, 2.0, 2.45
    c = o - n * D / 2
    _B(S, g, "kiosk_bialy", c, t, n, W, .95, D)                                     # dół z ladą
    for s in (-1, 1): _B(S, g, "kiosk_bialy", c + t * s * (W / 2 - .2), t, n, .4, H, D)  # słupki boczne
    _B(S, g, "kiosk_bialy", c - n * (D / 2 - .15), t, n, W - .8, H, .3)              # tylna ściana
    _B(S, g, "kiosk_bialy", c + UP * 1.95, t, n, W - .8, .5, D)                      # nadproże
    _B(S, g, "kiosk_wnetrze", c + UP * .95 - n * .1, t, n, W - .8, 1.0, D - .5)       # wnętrze za okienkiem
    _B(S, g, "kiosk_blat", o + UP * .95 + n * .12, t, n, W - .5, .05, .42)           # lada
    _B(S, g, "kiosk_dach", c + UP * H, t, n, W + .2, .12, D + .2)
    a = o + UP * 2.3 - t * (W / 2 + .05); b = o + UP * 2.3 + t * (W / 2 + .05)     # markiza w paski
    S.quad(g, "markiza", a, b, b + n * 1.0 - UP * .45, a + n * 1.0 - UP * .45, kier=nrm(n + UP))
    S.quad(g, "markiza", a + n * 1.0 - UP * .45, b + n * 1.0 - UP * .45, b, a, kier=nrm(-n - UP))
    _B(S, g, "kiosk_dach", o + UP * (H + .12) - n * .1, t, n, W, .7, .1)             # tablica szyldu
    _szyld(S, g, "szyld_bosna", o + UP * (H + .14) - n * .04, n, W - .06, .66)
    stolik(S, g, o + n * 2.2 - t * .9); stolik(S, g, o + n * 2.4 + t * 1.1)
    S.rura(g, "kontener_zielony", o + n * 1.2 + t * (W / 2 + .5), o + n * 1.2 + t * (W / 2 + .5) + UP * .8, .22, seg=10)


def zapiekanki(S, o, t, n, g="budki"):
    """Przyczepa gastronomiczna 4,2 × 2,1 m na dwóch kołach (jak przy wejściu do marketu na zdjęciu): klapa uniesiona
    jako daszek, okno wydawcze z ladą, szyld na dachu, dyszel z podporą, podpory narożne."""
    L, D, H, y0 = 4.2, 2.1, 2.3, .55
    c = o - n * D / 2 + UP * y0
    _B(S, g, "przyczepa_biala", c, t, n, L, .9, D)                                    # dół
    _B(S, g, "przyczepa_czerwona", c + UP * .9 + n * (D / 2 - .03), t, n, L, .18, .06)  # pas
    for s in (-1, 1): _B(S, g, "przyczepa_biala", c + t * s * (L / 2 - .25), t, n, .5, H - .1, D)
    _B(S, g, "przyczepa_biala", c - n * (D / 2 - .1), t, n, L - 1.0, H - .1, .2)
    _B(S, g, "przyczepa_biala", c + UP * 1.85, t, n, L - 1.0, .35, D)
    _B(S, g, "kiosk_wnetrze", c + UP * .9 - n * .15, t, n, L - 1.0, .95, D - .5)
    _B(S, g, "kiosk_blat", o + UP * (y0 + .9) + n * .1, t, n, L - .8, .05, .35)
    _B(S, g, "przyczepa_czerwona", c + UP * (H - .1), t, n, L + .05, .14, D + .05)   # dach
    k = o + UP * (y0 + H - .1)                                                         # klapa uniesiona 60°
    kk = nrm(n * math.cos(math.radians(25)) + UP * math.sin(math.radians(25)))
    _B(S, g, "przyczepa_czerwona", k + kk * .55 - t * 0, t, nrm(np.cross(t, UP) * 0 + kk), L - .9, .04, 1.1)
    _B(S, g, "kiosk_dach", o + UP * (y0 + H + .04) - n * .1, t, n, L - .4, .6, .1)
    _szyld(S, g, "szyld_zapiekanki", o + UP * (y0 + H + .06) - n * .04, n, L - .46, .56)
    for s in (-1, 1):                                                                  # koła i podpory
        w = o - n * D / 2 + t * s * .35 + UP * .33
        S.rura(g, "opona", w + n * (D / 2 + .02), w + n * (D / 2 + .24), .33, seg=12)
        S.rura(g, "opona", w - n * (D / 2 + .24), w - n * (D / 2 + .02), .33, seg=12)
        for e in (-1, 1): S.rura(g, "stal", o - n * D / 2 + t * s * (L / 2 - .2) + n * e * (D / 2 - .15), o - n * D / 2 + t * s * (L / 2 - .2) + n * e * (D / 2 - .15) + UP * y0, .04, seg=6)
    d0 = o - n * D / 2 - t * (L / 2) + UP * .45
    S.rura(g, "stal", d0, d0 - t * 1.3, .05, seg=6); S.rura(g, "stal", d0 - t * 1.2, d0 - t * 1.2 - UP * .45, .04, seg=6)
    stolik(S, g, o + n * 2.3 - t * 1.2); stolik(S, g, o + n * 2.6 + t * .9)


PUNKTY = {}


def bosna_otwarta(S, P, c, s, g="budki"):
    """Otwarty bus z bosną (właściciel 24.11, 00:03–00:04, zdjęcie food trucka): klapa okienka podniesiona jako daszek
    (blacha, 12° nad poziom, na dwóch rozpórkach), lada ze stali pod okienkiem, ciemne wnętrze z blatem, tablica z menu
    i dwie doniczki z ziołami na ladzie. Model busa jest z paczki właściciela; to tylko dodatki w miejscu okienka.
    P — punkt busa na ziemi, c — kierunek kabiny, s — bok z okienkiem (bok +z modelu). Okienko w układzie modelu:
    x 0,72…2,40 (tył), y 0,95…1,91, lico burty z = 0,975 (pomiar rzutu bocznego busa)."""
    c, s = nrm(c), nrm(s)
    Wp = lambda xm, y, zm: P - c * xm + s * zm + UP * y
    ex = -c; zs = .975; xa, xb, ya, yb = .72, 2.40, .95, 1.91
    # wnętrze za okienkiem (00:16: „na środku miejsce na osobę, żeby mógł sprzedawać”): okienko wycięte z burty modelu
    # (wytnij_okienko.py), w środku komora 2,0 × 1,75 m: ściany i sufit białe, podłoga, lada wewnętrzna pod okienkiem,
    # zaplecze z grillem do bosny, lodówka z napojami, półka; między ladami 0,9 m na sprzedawcę (twarzą do okienka)
    xi0, xi1, zi0, zi1, yf, yc = .55, 2.55, -.82, .96, .5, 2.25          # podłoga na wysokości podłogi busa (nie widać jej spod nadwozia)
    Q = lambda xm, y, zm: Wp(xm, y, zm)
    S.quad(g, "kiosk_bialy", Q(xi0, yf, zi0), Q(xi1, yf, zi0), Q(xi1, yc, zi0), Q(xi0, yc, zi0), kier=s)              # tylna ściana
    S.quad(g, "kiosk_bialy", Q(xi0, yf, zi0), Q(xi0, yf, zi1), Q(xi0, yc, zi1), Q(xi0, yc, zi0), kier=ex)                                                                  # bok od kabiny
    S.quad(g, "kiosk_bialy", Q(xi1, yf, zi0), Q(xi1, yf, zi1), Q(xi1, yc, zi1), Q(xi1, yc, zi0), kier=-ex)             # bok od tyłu
    S.quad(g, "posadzka", Q(xi0, yf, zi0), Q(xi1, yf, zi0), Q(xi1, yf, zi1), Q(xi0, yf, zi1), kier=UP)
    S.quad(g, "kiosk_bialy", Q(xi0, yc, zi0), Q(xi1, yc, zi0), Q(xi1, yc, zi1), Q(xi0, yc, zi1), kier=-UP)
    for (u0, u1, v0, v1) in ((xi0, xa, yf, yc), (xb, xi1, yf, yc), (xa, xb, yf, ya), (xa, xb, yb, yc)):             # przód od środka
        S.quad(g, "kiosk_bialy", Q(u0, v0, zi1 - .03), Q(u1, v0, zi1 - .03), Q(u1, v1, zi1 - .03), Q(u0, v1, zi1 - .03), kier=-s)
    S.box(g, "kiosk_wnetrze", Q(xa, yf, .62), ex, UP, s, xb - xa, ya - yf - .04, .3)                                 # lada wewn.
    S.box(g, "kiosk_blat", Q(xa, ya - .04, .55), ex, UP, s, xb - xa, .04, .42)
    S.box(g, "kiosk_wnetrze", Q(.75, yf, zi0 + .02), ex, UP, s, 1.15, .72, .52)                                     # zaplecze
    S.box(g, "kiosk_blat", Q(.75, yf + .72, zi0 + .02), ex, UP, s, 1.15, .04, .54)
    S.box(g, "latarnia_czarna", Q(.85, yf + .76, zi0 + .1), ex, UP, s, .55, .12, .38)                               # grill do bosny
    S.box(g, "stal_ocynk", Q(1.5, yf + .76, zi0 + .1), ex, UP, s, .3, .3, .3)                                       # opiekacz
    S.box(g, "kiosk_bialy", Q(2.0, yf, zi0 + .02), ex, UP, s, .5, 1.6, .55)                                         # lodówka
    S.quad(g, "okno_klatka", Q(2.04, yf + .45, zi0 + .58), Q(2.46, yf + .45, zi0 + .58), Q(2.46, yf + 1.52, zi0 + .58), Q(2.04, yf + 1.52, zi0 + .58), kier=s)
    S.box(g, "stal_ocynk", Q(.75, 1.72, zi0 + .02), ex, UP, s, 1.15, .03, .3)                                       # półka
    PUNKTY["sprzedawca"] = Q((xa + xb) / 2, yf, .1); PUNKTY["klient"] = Q((xa + xb) / 2, 0., zs + 1.1); PUNKTY["s"] = s
    for (x0_, y0_, x1_, y1_) in ((xa - .04, ya - .04, xb + .04, ya), (xa - .04, yb, xb + .04, yb + .04), (xa - .04, ya, xa, yb), (xb, ya, xb + .04, yb)):
        S.box(g, "stal_ocynk", Wp(x0_, y0_, zs), ex, UP, s, x1_ - x0_, y1_ - y0_, .03)
    # lada 0,32 m na zewnątrz, wsporniki
    S.box(g, "kiosk_blat", Wp(xa - .02, ya - .05, zs), ex, UP, s, xb - xa + .04, .04, .34)
    for xm in (xa + .15, xb - .15):
        S.rura(g, "stal_ocynk", Wp(xm, ya - .4, zs + .01), Wp(xm, ya - .05, zs + .3), .012, seg=4)
    # klapa-daszek: zawias na górnej krawędzi, 1,0 m, 12° w górę
    a = math.radians(12); ez = s * math.cos(a) + UP * math.sin(a); ey = nrm(np.cross(ez, ex))
    if ey[1] < 0: ey = -ey
    o = Wp(xa - .05, yb + .04, zs)
    S.box(g, "stal_ocynk", o, ex, ey, ez, xb - xa + .1, .03, 1.0)
    for xm in (xa + .03, xb - .03):                                   # rozpórki
        S.rura(g, "stal_ocynk", Wp(xm, ya + .45, zs + .01), Wp(xm, yb + .04, zs) + ez * .95, .011, seg=4)
    # tablica z menu (stoi na ladzie przy przedniej krawędzi okienka, jak na zdjęciu) i doniczki z ziołami
    m0 = Wp(xa + .08, ya - .01, zs + .14); m1 = Wp(xa + .46, ya - .01, zs + .14)
    S.quad(g, "menu_bosna", m0, m1, m1 + UP * .82, m0 + UP * .82, uv1=UV1, kier=s)
    S.box(g, "latarnia_czarna", m0 - UP * .0 - ex * .02 + s * -.015, ex, UP, s, .42, .86, .012)
    for xm in (1.55, 1.85, 2.15):
        q = Wp(xm, ya - .01, zs + .18)
        S.box(g, "donica", q - ex * .07 - s * .07, ex, UP, s, .14, .14, .14)
        for k in range(2):
            w_ = nrm(ex + s) if k == 0 else nrm(ex - s); cc = q + UP * .14
            S.quad(g, "zywoplot_galazki", cc - w_ * .14, cc + w_ * .14, cc + w_ * .14 + UP * .26, cc - w_ * .14 + UP * .26, uv_o=cc - w_ * .14)


# ---- 25.11 (właściciel 17:06: „cały market na około plus budki z jedzeniem, jak są ustawione i jakie są”; Street View
# IMG_2145–2149): w rzędzie wzdłuż ulicy na zachód od przystanku — kiosk „BAR”, kosze, przyczepa z burgerami, kontener
# z kebabem, drewniany pawilon z kawą i lodami; girlanda żarówek na słupach, płotek z palet, hydrant, szafka energetyczna.
# Napisy ogólne, bez nazw lokali. Układ: o = środek podstawy, t wzdłuż ulicy, n przodem do ulicy.

def kiosk_bar(S, o, t, n, g="budki"):
    """Kiosk (IMG_2145/2148): granatowe płyty z pomarańczowymi listwami, 3 okienka z białymi roletami, drzwi z boku,
    płaski biały daszek z pasem napisu „BAR”. 4,2 × 3,0 × 2,6 m."""
    L, D, H = 4.2, 3.0, 2.55
    _B(S, g, "kiosk_granat", o, t, n, L, H, D)
    for k, u in enumerate((-1.35, -.2, .95)):                                   # okienka z roletami od ulicy
        c = o + t * u + n * (D / 2 + .01)
        _B(S, g, "roleta_biala", c + UP * .9, t, n, 1.0, 1.3, .03)
        for (du, dv, w, h) in ((-.53, .85, .06, 1.42), (.53, .85, .06, 1.42), (0, .85, 1.12, .06), (0, 2.21, 1.12, .06)):
            _B(S, g, "kiosk_listwa", c + t * du + UP * dv, t, n, w, h, .05)
    d = o + t * (L / 2 + .01) + n * .6                                              # drzwi na boku
    _B(S, g, "kiosk_granat", d + UP * .05, n, t, .9, 2.05, .04)
    _B(S, g, "kiosk_listwa", d + UP * 2.1, n, t, 1.0, .06, .05)
    _B(S, g, "kiosk_bialy", o + UP * H, t, n, L + .6, .55, D + .6)                 # daszek z pasem
    _szyld(S, g, "szyld_bar", o + UP * (H + .05) + n * (D / 2 + .31), n, L * .7, .45)


def przyczepa_burger(S, o, t, n, g="budki"):
    """Przyczepa gastronomiczna (IMG_2147): żółta, górny pas w czarne pionowe paski, duże okno od boku, na dachu
    billboard z napisem i okap wyciągu; koła. 6,0 × 2,5 × 2,4 m na wysokości 0,6 m."""
    L, D, H, Y = 6.0, 2.5, 2.4, .6
    _B(S, g, "przyczepa_zolta", o + UP * Y, t, n, L, H, D)
    for k in range(int(L / .35)):                                                   # paski u góry obu długich boków
        u = -L / 2 + .2 + k * .35
        for s in (1, -1):
            _B(S, g, "pas_czarny", o + t * u + n * s * (D / 2 + .01) + UP * (Y + H - .75), t, n, .14, .6, .02)
    _B(S, g, "szyba_wiata", o + t * 1.3 + n * (D / 2 + .02) + UP * (Y + .7), t, n, 2.4, 1.2, .02)
    _B(S, g, "kiosk_wnetrze", o + t * 1.3 + n * (D / 2 - .05) + UP * (Y + .7), t, n, 2.3, 1.15, .04)
    for u in (-1.2, -.4):
        S.rura(g, "opona", o + t * u - n * (D / 2 + .1) + UP * .35, o + t * u + n * (D / 2 + .1) + UP * .35, .35, seg=12)
    _B(S, g, "stal", o + t * 1.8 + UP * (Y + H), t, n, 1.1, .5, .8)                # okap wyciągu
    _B(S, g, "stal", o - t * .8 + UP * (Y + H), t, n, .08, 1.0, .08)
    c = o - t * .8 + UP * (Y + H + 1.0) + n * .05
    _B(S, g, "pas_czarny", c, t, n, 3.0, 1.5, .08)
    _szyld(S, g, "szyld_burger", c + UP * .05 + n * .05, n, 2.9, 1.4)


def kontener_kebab(S, o, t, n, g="budki"):
    """Kontener (IMG_2146/2149): czarny, czerwone pasy u dołu i u góry, drzwi i okienko wydawcze, na dachu napis KEBAB.
    5,0 × 2,4 × 2,5 m."""
    L, D, H = 5.0, 2.4, 2.5
    _B(S, g, "kontener_czarny", o, t, n, L, H, D)
    for (y, h) in ((.25, .35), (H - .45, .45)):
        _B(S, g, "przyczepa_czerwona", o + UP * y, t, n, L + .02, h, D + .02)
    _B(S, g, "kiosk_wnetrze", o + t * -.8 + n * (D / 2 + .01) + UP * 1.0, t, n, 1.6, .9, .03)
    _B(S, g, "stal", o + t * -.8 + n * (D / 2 + .12) + UP * .98, t, n, 1.7, .04, .25)
    _B(S, g, "kontener_czarny", o + t * 1.6 + n * (D / 2 + .01), t, n, .9, 2.05, .03)
    _szyld(S, g, "szyld_kebab", o + UP * H + n * (D / 2 - .2), n, 3.2, .8)


def pawilon_kawa(S, o, t, n, g="budki"):
    """Drewniany pawilon z kawą i lodami (IMG_2149, 2146): deski, przeszklony front, okrągłe logo na boku, pergola
    z drewnianych słupów przed frontem, napis na dachu. 5,5 × 3,5 × 2,8 m + pergola 3,0 m."""
    L, D, H = 5.5, 3.5, 2.8
    _B(S, g, "drewno_pawilon", o, t, n, L, H, D)
    _B(S, g, "szyba_wiata", o + n * (D / 2 + .01) + UP * .4, t, n, L - 1.0, 2.1, .02)
    _B(S, g, "kiosk_wnetrze", o + n * (D / 2 - .05) + UP * .4, t, n, L - 1.1, 2.05, .04)
    S.rura(g, "pas_czarny", o + t * (L / 2) + UP * 1.5, o + t * (L / 2 + .03) + UP * 1.5, .7, seg=20)
    _B(S, g, "drewno_pawilon", o + UP * H, t, n, L + .4, .2, D + .4)
    for u in (-L / 2 + .1, 0, L / 2 - .1):                                              # pergola przed frontem
        _B(S, g, "drewno_pawilon", o + t * u + n * (D / 2 + 2.9), t, n, .14, 2.6, .14)
    for u in (-L / 2 + .1, L / 2 - .1):
        _B(S, g, "drewno_pawilon", o + t * u + n * (D / 2 + 1.5) + UP * 2.5, n, t, 3.0, .14, .14)
    _B(S, g, "drewno_pawilon", o + n * (D / 2 + 2.9) + UP * 2.5, t, n, L, .14, .14)
    _szyld(S, g, "szyld_kawa", o + UP * (H + .2) + n * (D / 2 - .1), n, 3.4, .7)


def girlanda(S, a, b, g="budki"):
    """Słupy 4,5 m i sznur żarówek (IMG_2146/2149) z ugięciem 0,6 m."""
    for p in (a, b):
        S.rura(g, "stal", p, p + UP * 4.5, .05, seg=8)
    L = float(np.linalg.norm(b - a)); k = max(6, int(L / .8))
    for i in range(k + 1):
        s = i / k; p = a + (b - a) * s + UP * (4.35 - .6 * 4 * s * (1 - s))
        S.rura(g, "zarowka", p - UP * .06, p + UP * .02, .045, seg=6)


def palety(S, o, t, n, L, g="budki"):
    """Płotek z palet 0,8 m (IMG_2149)."""
    for k in range(int(L / 1.2)):
        c = o + t * (-L / 2 + .6 + k * 1.2)
        for y in (.1, .35, .6):
            _B(S, g, "paleta_drewno", c + UP * y, t, n, 1.15, .12, .1)
        _B(S, g, "paleta_drewno", c + UP * 0, t, n, .1, .8, .12)
