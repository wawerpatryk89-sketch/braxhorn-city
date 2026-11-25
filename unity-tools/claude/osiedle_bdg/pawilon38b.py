"""MIASTO R1 — pawilon handlowy M. Skłodowskiej-Curie 38B (OSM: commercial, 2 kond., 28,5 × 13 m).

Wg zdjęć właściciela (Street View X.2025, IMG_2152/2153, 25.11 17:32): szary budynek, parter cofnięty za kwadratowymi
słupami (witryny lokali: po lewej sklep spożywczy z zielonym pasem, w środku lokal, po prawej sushi), na piętrze galeria
z balustradą z ciemnych paneli wzdłuż frontu, ściana piętra z pionowymi żebrami i wąskimi oknami, na attyce napis
z adresem, schody zewnętrzne na galerię z prawej strony; przed budynkiem słup ogłoszeniowy z kopułką.
Szyld sklepu z pliku właściciela (20:09, paczka szyfrowana), lokal sushi: napis „NOVA SUSHI” (20:05). Sklep i sushi będą później
funkcjonalne (właściciel 20:10).
Układ lokalny: X wzdłuż frontu (0..L), Z w głąb (0..W), front patrzy na −Z.
"""
import numpy as np
from kit import Siatka, UP

H1, H2, ATT = 3.4, 6.6, 7.4        # strop parteru, strop piętra, attyka
COF = 2.4                          # parter cofnięty o 2,4 m (podcień pod galerią)


def zbuduj(L, W):
    S = Siatka(); g = "p38b"
    ex, ez = np.array([1., 0, 0]), np.array([0., 0, 1])
    P = lambda x, y, z: np.array([x, y, z], float)
    # bryła: piętro i attyka na pełnym obrysie, parter cofnięty od frontu
    S.box(g, "p38_elewacja", P(0, H1, 0), ex, UP, ez, L, ATT - H1, W, bez=("-y",))
    S.box(g, "p38_elewacja", P(0, 0, COF), ex, UP, ez, L, H1, W - COF, bez=("+y", "-y"))
    S.quad(g, "p38_sufit", P(0, H1, 0), P(L, H1, 0), P(L, H1, COF), P(0, H1, COF), kier=-UP)       # strop podcienia
    S.box(g, "obrobka", P(-.05, ATT, -.05), ex, UP, ez, L + .1, .06, W + .1)
    # słupy podcienia co ~3,6 m
    n_sl = max(2, int(round(L / 3.6)))
    for k in range(n_sl + 1):
        x = min(L - .45, k * L / n_sl - (.225 if 0 < k < n_sl else 0)); x = max(0, x)
        S.box(g, "p38_slup", P(x, 0, .15), ex, UP, ez, .45, H1, .45)
    # witryny parteru (szkło w ramach) i drzwi; lokale: sklep (lewa 40 %), środek, sushi (prawa)
    xs = [.3, L * .40, L * .66, L - .3]
    for (x0, x1) in zip(xs, xs[1:]):
        S.box(g, "rama_ciemna", P(x0, .12, COF - .06), ex, UP, ez, x1 - x0, .1, .08)
        S.box(g, "rama_ciemna", P(x0, 2.6, COF - .06), ex, UP, ez, x1 - x0, .08, .08)
        S.quad(g, "market_szklo", P(x0, .22, COF - .03), P(x1, .22, COF - .03), P(x1, 2.6, COF - .03), P(x0, 2.6, COF - .03), t=ex, kier=-ez)
        for x in np.arange(x0, x1, 1.6):
            S.box(g, "rama_ciemna", P(x, .12, COF - .06), ex, UP, ez, .06, 2.5, .08)
        S.box(g, "drzwi_stal", P((x0 + x1) / 2 - .5, .12, COF - .1), ex, UP, ez, 1.0, 2.2, .04)
    for (x0, x1, m, h) in ((xs[0], xs[1], "pas_sklep", .62), (xs[2], xs[3], "szyld_sushi", .7)):
        S.box(g, m if m == "pas_sklep" else "p38_szyld_tlo", P(x0, 2.7, COF - .12), ex, UP, ez, x1 - x0, h, .06)
    xm = (xs[0] + xs[1]) / 2          # 20:09: szyld sklepu z pliku właściciela (paczka szyfrowana), płyta 2,4 × 0,72 m
    S.box(g, "rama_ciemna", P(xm - 1.25, 2.66, COF - .17), ex, UP, ez, 2.5, .8, .05)
    S.quad(g, "szyld_zabka", P(xm - 1.2, 2.7, COF - .18), P(xm + 1.2, 2.7, COF - .18), P(xm + 1.2, 3.42, COF - .18), P(xm - 1.2, 3.42, COF - .18),
           uv1=[(0, 0), (1, 0), (1, 1), (0, 1)], kier=-ez)
    xm = (xs[2] + xs[3]) / 2
    S.quad(g, "szyld_sushi", P(xm - 2.4, 2.72, COF - .13), P(xm + 2.4, 2.72, COF - .13), P(xm + 2.4, 3.36, COF - .13), P(xm - 2.4, 3.36, COF - .13),
           uv1=[(0, 0), (1, 0), (1, 1), (0, 1)], kier=-ez)
    # galeria piętra: płyta wysunięta 0,3 m przed lico, balustrada z ciemnych paneli 1,1 m
    S.box(g, "p38_elewacja", P(-.1, H1 - .25, -.3), ex, UP, ez, L + .2, .25, .3)
    S.box(g, "rama_ciemna", P(-.1, H1 + 1.05, -.32), ex, UP, ez, L + .2, .05, .06)
    for x in np.arange(-.1, L + .1, 1.4):
        S.box(g, "rama_ciemna", P(x, H1, -.3), ex, UP, ez, .05, 1.1, .05)
        S.quad(g, "balustrada_panel", P(x + .05, H1 + .1, -.29), P(min(x + 1.4, L + .1), H1 + .1, -.29), P(min(x + 1.4, L + .1), H1 + .95, -.29), P(x + .05, H1 + .95, -.29), t=ex, kier=-ez)
    # piętro: pionowe żebra co 1,8 m (wysunięte 0,35), między nimi wąskie okna
    for x in np.arange(.9, L - .5, 1.8):
        S.box(g, "p38_zebro", P(x - .15, H1, -.35), ex, UP, ez, .3, ATT - H1 - 1.0, .35)
        S.quad(g, "market_szklo", P(x + .35, H1 + 1.0, -.01), P(x + 1.3, H1 + 1.0, -.01), P(x + 1.3, H2 - .3, -.01), P(x + .35, H2 - .3, -.01), t=ex, kier=-ez)
    # napis z adresem na attyce
    x0n = max(.5, L / 2 - 6.0); x1n = min(L - .5, L / 2 + 6.0)
    S.quad(g, "napis_38b", P(x0n, ATT - .9, -.02), P(x1n, ATT - .9, -.02), P(x1n, ATT - .1, -.02), P(x0n, ATT - .1, -.02),
           uv1=[(0, 0), (1, 0), (1, 1), (0, 1)], kier=-ez)
    # schody zewnętrzne na galerię z prawej (wzdłuż boku +X)
    n_st = 18; hs = H1 / n_st; ds = .28
    for k in range(n_st):
        S.box(g, "beton_schody", P(L + .1, (k + 1) * hs - .15, 1.2 + (n_st - 1 - k) * ds), ex, UP, ez, 1.3, .15, ds)
    for z0, z1, y0, y1 in ((1.2 + n_st * ds, 1.2, 1.0, H1 + 1.0),):
        S.rura(g, "rama_ciemna", P(L + 1.4, y0, z0), P(L + 1.4, y1, z1), .025, seg=6)
    S.box(g, "beton_schody", P(L + .1, H1 - .2, -.3), ex, UP, ez, 1.3, .2, 1.5)
    # słup ogłoszeniowy z kopułką przed lewym narożnikiem
    c = P(-2.5, 0, -5.0)
    S.rura(g, "slup_ogloszen", c, c + UP * 2.6, .6, seg=20)
    S.rura(g, "rama_ciemna", c + UP * 2.6, c + UP * 2.75, .68, seg=20)
    S.rura(g, "rama_ciemna", c + UP * 2.75, c + UP * 3.05, .45, seg=16)
    return S
