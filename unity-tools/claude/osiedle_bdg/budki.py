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
    """Budka z burgerami (zdjęcie 18:47): żółty kontener 4,0 × 2,4 m, dwa okienka wydawcze, ciemny billboard z napisem na
    dachu, kosz, donica; obok słup z obręczą i okrągłym szyldem (pusty — logo BRAXHORN później) i girlanda żarówek."""
    W, D, H = 4.0, 2.4, 2.6
    c = o - n * D / 2
    _B(S, g, "kontener_zolty", c, t, n, W, H, D)
    for u in (-1.0, .9):
        _B(S, g, "kiosk_wnetrze", o + t * u + n * .02 + UP * 1.0, t, n, 1.1, .9, .04)
        _B(S, g, "kiosk_blat", o + t * u + n * .15 + UP * .98, t, n, 1.2, .04, .3)
        a = o + t * (u - .6) + UP * 2.05; b = o + t * (u + .6) + UP * 2.05
        S.quad(g, "markiza_pasy", a, b, b + n * .45 - UP * .2, a + n * .45 - UP * .2, kier=nrm(n + UP))
        S.quad(g, "markiza_pasy", a + n * .45 - UP * .2, b + n * .45 - UP * .2, b, a, kier=nrm(-n - UP))
    _B(S, g, "kiosk_dach", c + UP * H, t, n, W + .2, .1, D + .2)
    _B(S, g, "kiosk_dach", o + UP * (H + .1) - n * .2, t, n, W + .3, 1.3, .12)                # billboard
    _szyld(S, g, "szyld_burger", o + UP * (H + .15) - n * .13, n, W + .2, 1.2)
    S.rura(g, "opona", o + n * .5 + t * (W / 2 + .4), o + n * .5 + t * (W / 2 + .4) + UP * .95, .25, seg=10)
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
