"""MIASTO R1 — blok Skłodowskiej-Curie 32a (11 kondygnacji), wzór dla osiedla. Etap 1: ściana z wejściem (północna).

Skąd wymiary (wszystko z planu OpenStreetMap i ze zdjęcia właściciela IMG_1427 „wejście do klatki”, 23.11):
- plan: dwie połówki przesunięte o 2,5 m, razem 18,6 x 30 m; połówka wschodnia wysunięta na północ (ściana z HWDP).
- zdjęcie: aparat ~15 m przed ścianą zachodniej połówki, na wysokości 2,5 m, kąt 99°. Z niego (sprawdzone
  rzutem, rozbieżność < 1 %): połówki po 9,3 m, kondygnacja 2,70 m, parter na +1,00, okno 1,40 x 1,02 (parapet +0,97
  nad podłogą), okna od lewej krawędzi ściany: środki 2,5 i 4,7 m; pas klatki 0,9 m przy uskoku; daszek 2,15 x 1,3
  m na +3,40; schody 1,3 m szerokości, 2,2 m od ściany; cokół 1,0 m (pod ścianą z napisem 0,65 m); żółty pas do 2,0 m.
Właściciel 15:04: wejście od uliczki z garażami (za plecami), bar mleczny po lewej, balkony na obu długich ścianach.

Długie ściany i ściana południowa czekają na zdjęcia (właściciel proszony 23.11, 15:00) — tu są gładkie, BEZ okien
i balkonów, opisane jako NIEWYKOŃCZONE. Nie zgadujemy.

Układ lokalny bloku: X w poprzek (0 = zachodnia krawędź, 18,6 = wschodnia), Z wzdłuż (0 = południe połówki
zachodniej), y = 0 teren przy wejściu. Obrót do mapy robi C# / podgląd (kąt osi 11,5° na wschód od północy).
"""
import math
import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
from kit import Siatka, UP, nrm, zapisz_bin, zapisz_glb, trojkaty  # noqa: E402

# ---------------- wymiary ----------------
SZ_POL = 9.3            # szerokość połówki
DL = 27.8               # długość połówki
USKOK = 2.5             # wschodnia połówka wysunięta na północ
POZIOM0 = 1.00          # podłoga parteru
KOND = 2.70             # kondygnacja
N_KOND = 11
DACH = POZIOM0 + N_KOND * KOND          # 30.70
ATTYKA = DACH + 0.55
OKNO_W, OKNO_H, PARAPET = 1.40, 1.02, 0.97
OPASKA = 0.08           # szara opaska wokół okna
GL_OSC = 0.12           # ościeże: lico ściany -> lico ramy
RAMA = 0.065            # szerokość ramy PCV w widoku
RAMA_GL = 0.07
PAS_KLATKI = 0.90       # pas z oknami klatki przy uskoku
COKOL_Z, COKOL_W = 1.00, 0.65           # cokół: ściana zachodnia / ściana z napisem
ZOLTY_DO = 2.00

S = Siatka()
okno_nr = [0]
SEED = 0


def sciana_okna(g, mat_tynk, o, t, L, H, okna, inne_otwory=(), uv1=None, kolor_pas=None):
    """Ściana tynkowana z oknami (u0,u1,v0,v1,rodzaj,pietro): wycięte opaski, ościeża, rama, skrzydła, szyba, parapet."""
    n = nrm(np.cross(UP, t))                                         # normalna na zewnątrz (t = cross(n, góra))
    dziury = [(u0 - OPASKA, u1 + OPASKA, v0, v1 + OPASKA) for (u0, u1, v0, v1, *_r) in okna] + list(inne_otwory)
    S.sciana(g, mat_tynk, o, t, L, H, dziury, uv1_skala=uv1 and uv1[0], uv1_o=uv1 and uv1[1] or (0, 0))
    P = lambda u, v, d=0.0: o + t * u + UP * v - n * d
    for (u0, u1, v0, v1, rodzaj, pietro) in okna:
        # opaska (w płaszczyźnie ściany, wycięta ze ściany — bez nakładania)
        for a, b, c, d in (((u0 - OPASKA, v0), (u0, v0), (u0, v1 + OPASKA), (u0 - OPASKA, v1 + OPASKA)),
                           ((u1, v0), (u1 + OPASKA, v0), (u1 + OPASKA, v1 + OPASKA), (u1, v1 + OPASKA)),
                           ((u0, v1), (u1, v1), (u1, v1 + OPASKA), (u0, v1 + OPASKA))):
            S.quad(g, "opaska", P(*a), P(*b), P(*c), P(*d), t=t, kier=n)
        okno(g, o, t, n, u0, u1, v0, v1, rodzaj, pietro)


def ramka(g, mat, P, t, n, u0, u1, v0, v1, w, df, db):
    """Rama prostokątna: lico (4 pasy na głębokości df) + wewnętrzne boki do głębokości db. Zewnętrzne boki zasłania
    ościeże — nie są rysowane (oszczędność trójkątów; budżet budynku 60 tys., ZASADY 10.2)."""
    for (a, b, c, d) in (((u0, v0), (u1, v0), (u1, v0 + w), (u0, v0 + w)), ((u0, v1 - w), (u1, v1 - w), (u1, v1), (u0, v1)),
                         ((u0, v0 + w), (u0 + w, v0 + w), (u0 + w, v1 - w), (u0, v1 - w)), ((u1 - w, v0 + w), (u1, v0 + w), (u1, v1 - w), (u1 - w, v1 - w))):
        S.quad(g, mat, P(*a, df), P(*b, df), P(*c, df), P(*d, df), t=t, kier=n)
    for (a, b, kk) in (((u0 + w, v0 + w), (u1 - w, v0 + w), UP), ((u0 + w, v1 - w), (u1 - w, v1 - w), -UP)):
        S.quad(g, mat, P(*a, df), P(*b, df), P(*b, db), P(*a, db), kier=kk)
    for (a, b, kk) in (((u0 + w, v0 + w), (u0 + w, v1 - w), t), ((u1 - w, v0 + w), (u1 - w, v1 - w), -t)):
        S.quad(g, mat, P(*a, df), P(*b, df), P(*b, db), P(*a, db), kier=kk)


def okno(g, o, t, n, u0, u1, v0, v1, rodzaj, pietro, pokoj=None):
    """Okno PCV we wnęce: ościeża, parapet blaszany z kapinosem, ościeżnica 6,5 cm, słupek, skrzydła, szyba z pokojem.
    rodzaj: okno (dwa pola, lewe szersze), drzwi (jedno pole do podłogi, bez parapetu), klatka (jedno pole)."""
    P = lambda u, v, d=0.0: o + t * u + UP * v - n * d
    S.quad(g, "opaska", P(u0, v1, 0), P(u0, v1, GL_OSC), P(u1, v1, GL_OSC), P(u1, v1, 0), t=t, kier=-UP)
    S.quad(g, "opaska", P(u0, v0, 0), P(u0, v1, 0), P(u0, v1, GL_OSC), P(u0, v0, GL_OSC), kier=t)
    S.quad(g, "opaska", P(u1, v0, 0), P(u1, v0, GL_OSC), P(u1, v1, GL_OSC), P(u1, v1, 0), kier=-t)
    if rodzaj == "drzwi":
        S.quad(g, "opaska", P(u0, v0, 0), P(u1, v0, 0), P(u1, v0, GL_OSC), P(u0, v0, GL_OSC), t=t, kier=UP)
    else:   # parapet: blacha 2 cm, 5 cm przed lico; bez spodu od ściany
        S.box(g, "parapet", P(u0 - .03, v0 - .025, GL_OSC + .005), t, UP, n, (u1 - u0) + .06, .025, GL_OSC + .055)
    d0 = GL_OSC; W = u1 - u0
    ramka(g, "pcv", P, t, n, u0, u1, v0, v1, RAMA, d0, d0 + RAMA_GL)
    pola = [(u0 + RAMA, u1 - RAMA)]
    if rodzaj == "okno":
        us = u0 + RAMA + (W - 2 * RAMA) * .58
        S.quad(g, "pcv", P(us - .04, v0 + RAMA, d0), P(us + .04, v0 + RAMA, d0), P(us + .04, v1 - RAMA, d0), P(us - .04, v1 - RAMA, d0), t=t, kier=n)
        pola = [(u0 + RAMA, us - .04), (us + .04, u1 - RAMA)]
    sk = .05; fl = POZIOM0 + pietro * KOND
    k = okno_nr[0] if pokoj is None else pokoj
    RW = max(W, 2.2) + .8; ru0 = (u0 + u1) / 2 - RW / 2
    for (a, b) in pola:                                         # skrzydło: rama 5 cm, cofnięta 1,5 cm za ościeżnicę
        ramka(g, "pcv", P, t, n, a, b, v0 + RAMA, v1 - RAMA, sk, d0 + .015, d0 + .05)
        gx0, gx1, gy0, gy1 = a + sk, b - sk, v0 + RAMA + sk, v1 - RAMA - sk
        pts = [P(gx0, gy0, d0 + .05), P(gx1, gy0, d0 + .05), P(gx1, gy1, d0 + .05), P(gx0, gy1, d0 + .05)]
        uv1 = [((x - ru0) / RW + k, (y - fl) / KOND + pietro) for x, y in ((gx0, gy0), (gx1, gy0), (gx1, gy1), (gx0, gy1))]
        S.quad(g, "okno_klatka" if rodzaj == "klatka" else "okno", *pts, uv1=uv1, t=t, kier=n)
    if pokoj is None: okno_nr[0] += 1


# ---------------- długie ściany z balkonami ----------------
# Z czterech zdjęć właściciela (IMG_1626 wieżowiec na wprost, IMG_1624 i IMG_1636 32a od wschodu, IMG_1637 z bliska):
# 5 przęseł po 5,56 m = 27,8 m. W przęśle: okno 1,40 (0,25–1,65), płyta balkonu 2,30 x 1,05 (1,75–4,05),
# drzwi balkonowe 0,80 (2,00–2,80) i okno 1,60 (2,90–4,50). Balkony na piętrach 1–10, parter same okna.
# Wypełnienie balustrad jak na zdjęciach (IMG_1626: 26 z 40 widocznych to pręty): pręty 65 %, zielona 15 %, ciemna 10 %, biała, brązowa.
PRZESLO = 5.56
BALUSTRADY = ["prety"] * 13 + ["blacha_zielona"] * 3 + ["blacha_ciemna"] * 2 + ["blacha_biala"] * 1 + ["blacha_braz"] * 1


def balkon(g, o, t, n, u0, u1, fl, typ):
    G = 1.05; H = 1.05; T = .14
    P = lambda u, v, d=0.0: o + t * u + UP * v - n * d
    S.box(g, "beton_balkon", P(u0, fl - T, 0), t, UP, n, u1 - u0, T, G, bez=("-z",))
    S.box(g, "obrobka", P(u0 - .02, fl - .005, -(G + .02)), t, UP, n, (u1 - u0) + .04, .03, .05)   # kapinos przodu
    sl = .045
    for uu in (u0 + .02, u1 - .02 - sl):                          # słupki narożne
        S.box(g, "stal_balustrada", P(uu, fl, -(G - .03)), t, UP, n, sl, H, sl)
    S.box(g, "stal_balustrada", P(u0 + .02, fl + H - .04, -(G - .03)), t, UP, n, (u1 - u0) - .04, .04, .05)   # pochwyt
    for uu in (u0 + .02, u1 - .02 - sl):                          # pochwyty boczne
        S.box(g, "stal_balustrada", P(uu, fl + H - .04, -(G - .03)), t, UP, -n, sl, .04, G - .08)
    mat = "balustrada_" + typ
    dd = -(G - .03 - sl / 2); v0, v1 = fl + .06, fl + H - .04     # d < 0 = przed licem ściany
    for (a, b, kk) in ((P(u0 + .065, v0, dd), P(u1 - .065, v0, dd), n), (P(u1 - .065, v0, dd), P(u0 + .065, v0, dd), -n)):
        up_ = UP * (v1 - v0)
        S.quad(g, mat, a, b, b + up_, a + up_, t=t if kk is n else -t, kier=kk)
    for uu in (u0 + .02 + sl / 2, u1 - .02 - sl / 2):              # boki
        a = P(uu, v0, -.05); b = P(uu, v0, dd + .02); up_ = UP * (v1 - v0)
        S.quad(g, mat, a, b, b + up_, a + up_, kier=t)
        S.quad(g, mat, b, a, a + up_, b + up_, kier=-t)


def sciana_dluga(g, o, t, seed):
    import random
    R = random.Random(seed)
    n = nrm(np.cross(UP, t))
    S.sciana(g, "cokol", o + UP * -0.3, t, DL, COKOL_Z + .3,
             [(q + .4, q + 1.3, .55, .95) for q in np.arange(0, DL - 1, PRZESLO)])
    piwnica(o + UP * -0.3, t, [(q + .4, q + 1.3, .55, .95) for q in np.arange(0, DL - 1, PRZESLO)], n=n)
    okna = []
    for k in range(N_KOND):
        fl = POZIOM0 + k * KOND
        for b in range(5):
            q = b * PRZESLO
            okna.append((q + .25, q + 1.65, fl + PARAPET, fl + PARAPET + OKNO_H, "okno", k))
            if k == 0:
                okna.append((q + 2.30, q + 4.50, fl + PARAPET, fl + PARAPET + OKNO_H, "okno", k))
            else:
                okna.append((q + 2.00, q + 2.75, fl + .05, fl + PARAPET + OKNO_H, "drzwi", k))
                okna.append((q + 2.92, q + 4.50, fl + PARAPET, fl + PARAPET + OKNO_H, "okno", k))
    gora = [(u0, u1, v0 - COKOL_Z, v1 - COKOL_Z, r, p) for (u0, u1, v0, v1, r, p) in okna]
    sciana_okna(g, "tynk", o + UP * COKOL_Z, t, DL, ATTYKA - COKOL_Z, gora, (), uv1=((DL, ATTYKA), (0, COKOL_Z)))
    for k in range(1, N_KOND):
        for b in range(5):
            q = b * PRZESLO
            balkon(g, o, t, n, q + 1.75, q + 4.05, POZIOM0 + k * KOND, R.choice(BALUSTRADY))


def blok(z_terenem=True):
    ex, ez = np.array([1., 0, 0]), np.array([0., 0, 1])
    # ---- ściana północna połówki zachodniej (Z = DL), widok z północy: u rośnie na zachód (t = -X) ----
    o = np.array([SZ_POL, 0.0, DL]); t = -ex
    okna = []
    for k in range(N_KOND):
        fl = POZIOM0 + k * KOND
        for c in (2.5, 4.7):
            okna.append((c - OKNO_W / 2, c + OKNO_W / 2, fl + PARAPET, fl + PARAPET + OKNO_H, "okno", k))
    # pas klatki (wnęka 12 cm) od podłogi parteru do attyki, cokół/żółty pas pod nim zostają
    pas = (0.0, PAS_KLATKI, COKOL_Z, DACH)
    # strefy: cokół 0..1.0 (osobny materiał), żółty 1.0..2.0, tynk 2.0..attyka
    S.sciana("sciana_N_zach", "cokol", o + UP * -0.3, t, SZ_POL, COKOL_Z + .3,
             [(u - .45, u + .45, .45, .9) for u in (2.5, 4.7)] + [(0.0, PAS_KLATKI, .3 + .0, COKOL_Z + .3)] +
             [(5.45, 6.35, 0.0, .3 + (.12 - 11 * .18) + 1.95)])                    # 25.11: góra drzwi piwnicy (klatka: zejście)
    piwnica(o + UP * -0.3, t, [(u - .45, u + .45, .45, .9) for u in (2.5, 4.7)])
    zolte = [w for w in okna if w[2] < ZOLTY_DO]
    gora = [(u0, u1, v0 - COKOL_Z, v1 - COKOL_Z, r, p) for (u0, u1, v0, v1, r, p) in okna]
    sciana_okna("sciana_N_zach", "tynk", o + UP * COKOL_Z, t, SZ_POL, ATTYKA - COKOL_Z, gora,
                [(0.0, PAS_KLATKI, 0, DACH - COKOL_Z)], uv1=((SZ_POL, ATTYKA), (0, COKOL_Z)))
    # okna parteru wchodzą w żółty pas: dorabiam ich dolną część (parapet + 0,03 poniżej ZOLTY_DO)
    klatka(o, t)
    # ---- ściana północna połówki wschodniej (Z = DL + USKOK): ślepa, napis, graffiti ----
    o2 = np.array([2 * SZ_POL, 0.0, DL + USKOK])
    S.sciana("sciana_N_wsch", "cokol", o2 + UP * -0.3, t, SZ_POL, COKOL_W + .3,
             [(u - .07, u + .07, .3 + .3, .3 + .4) for u in np.arange(.9, SZ_POL - .5, 1.3)])
    for u in np.arange(.9, SZ_POL - .5, 1.3):                     # kratki wentylacyjne piwnicy
        P = lambda uu, v, d=0.0: o2 + t * uu + UP * v + ez * -d
        S.quad("sciana_N_wsch", "kratka", P(u - .07, .3, .05), P(u + .07, .3, .05), P(u + .07, .4, .05), P(u - .07, .4, .05), t=t, kier=ez)
        for (a, b, c, d), kk in (((P(u - .07, .3, 0), P(u - .07, .4, 0), P(u - .07, .4, .05), P(u - .07, .3, .05)), t),
                                 ((P(u + .07, .3, 0), P(u + .07, .3, .05), P(u + .07, .4, .05), P(u + .07, .4, 0)), -t),
                                 ((P(u - .07, .4, 0), P(u + .07, .4, 0), P(u + .07, .4, .05), P(u - .07, .4, .05)), -UP),
                                 ((P(u - .07, .3, 0), P(u - .07, .3, .05), P(u + .07, .3, .05), P(u + .07, .3, 0)), UP)):
            S.quad("sciana_N_wsch", "cokol", a, b, c, d, kier=kk)
    S.sciana("sciana_N_wsch", "graffiti_32a", o2 + UP * COKOL_W, t, SZ_POL, ZOLTY_DO + .1 - COKOL_W, [],
             uv1_skala=(SZ_POL, ZOLTY_DO + .1 - COKOL_W))
    NAP = (2.0, 7.6, 20.4, 23.4)                                   # panel z adresem (u0,u1,v0,v1) — patrz tekstury
    S.sciana("sciana_N_wsch", "tynk_brudny", o2 + UP * (ZOLTY_DO + .1), t, SZ_POL, ATTYKA - ZOLTY_DO - .1,
             [(NAP[0], NAP[1], NAP[2] - ZOLTY_DO - .1, NAP[3] - ZOLTY_DO - .1)], uv1_skala=(SZ_POL, ATTYKA), uv1_o=(0, ZOLTY_DO + .1))
    S.sciana("sciana_N_wsch", "napis_32a", o2 + t * NAP[0] + UP * NAP[2], t, NAP[1] - NAP[0], NAP[3] - NAP[2], [],
             uv1_skala=(NAP[1] - NAP[0], NAP[3] - NAP[2]))
    skrzynka_gaz(o2, t)
    # ---- uskok północny: ściana zachodnia połówki wschodniej (X = SZ_POL, Z od DL-0.12 do DL+USKOK) ----
    # 25.11: było t = +Z → normalna na wschód (do środka) — ściana niewidoczna z zewnątrz, prześwit na wylot przez blok
    ou = np.array([SZ_POL, -0.3, DL + USKOK]); tu = -ez                       # patrzy na zachód: t = −Z
    S.sciana("uskok_N", "cokol", ou, tu, USKOK + .12, COKOL_Z + .3, [])
    S.sciana("uskok_N", "tynk", ou + UP * (COKOL_Z + .3), tu, USKOK + .12, ATTYKA - COKOL_Z, [])
    # ---- długie ściany z balkonami: zachodnia (połówka zachodnia), wschodnia (połówka wschodnia) ----
    sciana_dluga("sciana_W", np.array([0., 0, DL]), -ez, 11 + SEED)
    sciana_dluga("sciana_E", np.array([2 * SZ_POL, 0, USKOK]), ez, 12 + SEED)
    # ---- południe: NIEWYKOŃCZONE (brak zdjęć — właściciel nie może podejść; prośba o widok „Z lotu ptaka”) ----
    for (oo, tt, L, nazwa) in ((np.array([0., 0, 0]), ex, SZ_POL, "sciana_S_NIEWYKONCZONA"),
                               (np.array([SZ_POL, 0, USKOK]), ex, SZ_POL, "sciana_S_NIEWYKONCZONA"),
                               (np.array([SZ_POL, 0, 0]), ez, USKOK, "sciana_S_NIEWYKONCZONA")):
        S.sciana(nazwa, "cokol", oo + UP * -.3, tt, L, COKOL_Z + .3, [])
        S.sciana(nazwa, "tynk", oo + UP * COKOL_Z, tt, L, ATTYKA - COKOL_Z, [])
    dach()
    maszynownia()
    if z_terenem: teren()


def klatka(o, t):
    """Pas klatki schodowej (wnęka 12 cm): drzwi wejściowe z naświetlem, okna spocznikowe z szarymi płycinami,
    brązowe listwy po bokach; daszek, schody, poręcz, lampa, numer, domofon."""
    g = "klatka"; n = np.array([0., 0, 1]); W = PAS_KLATKI; G = .12
    P = lambda u, v, d=0.0: o + t * u + UP * v - n * d
    # boczne ościeża wnęki (prawa strona; lewa to przedłużenie uskoku)
    S.quad(g, "tynk", P(W, COKOL_Z, 0), P(W, COKOL_Z, G), P(W, DACH, G), P(W, DACH, 0), kier=-t)
    # listwy brązowe na krawędziach pasa (od parteru po dach)
    for u in (0.0, W - .05):
        S.box(g, "listwa_braz", P(u, COKOL_Z + 2.5, G - .005), t, UP, n, .05, DACH - COKOL_Z - 2.5, .02)
    # parter: drzwi 0,9 x 2,05 + naświetle do 2,45 nad podłogą
    D0 = COKOL_Z; S.quad(g, "tynk", P(0, D0 + 2.45, G), P(W, D0 + 2.45, G), P(W, D0 + 2.5, G), P(0, D0 + 2.5, G), t=t, kier=n)
    drzwi(g, P, t, n, G, D0)
    # piętra: okno spocznikowe 0,6 x 1,02 na poziomie okien mieszkań, reszta pasa — szare płyciny
    for k in range(N_KOND):
        fl = POZIOM0 + k * KOND
        v_dol = fl + 2.5 if k == 0 else fl
        if k == 0:
            # nad daszkiem do parapetu okna piętra 1: płycina
            continue
        u0, u1 = .15, .75; w0, w1 = fl + PARAPET, fl + PARAPET + OKNO_H
        S.sciana(g, "plycina", P(0, fl - KOND + 2.5 if k == 1 else fl, G), t, W, (w0 - (fl - KOND + 2.5 if k == 1 else fl)), [])
        S.sciana(g, "plycina", P(0, w0, G), t, u0, OKNO_H, [])
        S.sciana(g, "plycina", P(u1, w0, G), t, W - u1, OKNO_H, [])
        S.sciana(g, "plycina", P(0, w1, G), t, W, (fl + KOND) - w1 if k < N_KOND - 1 else DACH - w1, [])
        okno(g, o - n * G, t, n, u0, u1, w0, w1, "klatka", k)
    # daszek: płyta 2,15 x 1,3 x 0,15 na +3,40 nad terenem, z obróbką
    S.box(g, "beton_daszek", P(-.0, 3.40, 0), t, UP, n, 2.15, .15, 1.30)
    S.box(g, "parapet", P(-.0, 3.55, 0), t, UP, n, 2.17, .03, 1.32)
    S.box(g, "lampa", P(.75, 3.33, -.45), t, UP, n, .3, .07, .2)
    # numer i domofon po prawej od drzwi
    S.box(g, "tabliczka_32a", P(1.05, 2.95, 0), t, UP, n, .22, .16, .015)
    S.box(g, "domofon", P(1.05, 2.15, 0), t, UP, n, .14, .26, .03)
    # schody: 6 stopni od terenu do podestu +1,00; podest 1,0 m, szer. 1,3; dalej 5 stopni po 0,24
    st = 6; h = COKOL_Z / st
    S.box(g, "beton_schody", P(-.0, 0 - .3, 0), t, UP, n, 1.30, COKOL_Z + .3, 1.0)
    for i in range(1, st):
        S.box(g, "beton_schody", P(0, -.3, -1.0 - .24 * (i - 1)), t, UP, n, 1.30, COKOL_Z - h * i + .3, .24)
    # poręcz po prawej (rura 4 cm): ze schodów w dół + pozioma wzdłuż cokołu (barierka nad doświetlem piwnicy)
    a = P(1.38, COKOL_Z + .9, -.3); b = P(1.38, h + .9, -1.0 - .24 * (st - 1) - .1)
    S.rura(g, "stal", a, b, .022)
    S.rura(g, "stal", P(1.38, .42, -.3), a, .022)          # słupek na murku doświetla
    S.rura(g, "stal", b - UP * .95, b, .022)
    # ZEJŚCIE DO PIWNICY (właściciel 25.11, 15:35: „wejście do piwnicy, takie w dół co schodzi, koło tych poręczy”; 16:50:
    # „tam do piwnicy jest zejście w dół. Idzie i powinny być drzwi”): wykop wzdłuż ściany wejściowej za murkiem z barierką
    # (IMG_1427: murek 14 cm, 30 cm nad chodnikiem, barierka na murku). Od zachodniego końca 11 stopni po 18 × 28 cm w dół,
    # dalej spocznik na −1,86 i stalowe drzwi piwnicy w ścianie. Ściany wykopu do dna — nic na wylot. Teren omija wykop
    # (osiedle.wiezowce: MODELE).
    bar_d = 1.25; mur = .42; U0, U1 = 1.45, SZ_POL + .40
    S.box(g, "beton_schody", P(U0, -.3, -bar_d), t, UP, n, U1 - U0, mur + .3, .14)                 # murek od chodnika
    S.box(g, "beton_schody", P(1.30, -.3, 0), t, UP, n, .15, mur + .3, bar_d)                    # murek przy schodach klatki
    for y in (mur + .85, mur + .45):
        S.rura(g, "stal", P(1.38, y, -bar_d - .07), P(U1 - .05, y, -bar_d - .07), .02)
    for u in list(np.arange(1.38, U1 - .1, 1.6)) + [U1 - .05]:
        S.rura(g, "stal", P(u, mur, -bar_d - .07), P(u, mur + .85, -bar_d - .07), .02)
    S.rura(g, "stal", P(1.38, COKOL_Z + .9, -.3), P(1.38, mur + .85, -bar_d - .07), .022)
    st, hs, ds = 11, .18, .28; Y0 = .12; Yd = Y0 - st * hs                                      # dno −1,86
    us = U1 - st * ds                                                                           # koniec biegu (6,62)
    for k in range(st):                                                                         # stopnie (od góry, z zachodu)
        S.box(g, "beton_schody", P(U1 - (k + 1) * ds, Yd, 0), t, UP, n, ds, Y0 - (k + 1) * hs - Yd + hs * 0, bar_d)
    S.quad(g, "beton_schody", P(U0, Yd, 0), P(us, Yd, 0), P(us, Yd, -bar_d), P(U0, Yd, -bar_d), kier=UP)   # spocznik
    # ściany wykopu (lico do środka): od chodnika, przy schodach klatki, przy ścianie budynku (z otworem drzwi)
    S.quad(g, "beton_schody", P(U0, Yd, -bar_d), P(U1, Yd, -bar_d), P(U1, Y0 + .01, -bar_d), P(U0, Y0 + .01, -bar_d), kier=-n)
    S.quad(g, "beton_schody", P(U0, Yd, 0), P(U0, Yd, -bar_d), P(U0, Y0 + .01, -bar_d), P(U0, Y0 + .01, 0), kier=t)
    D0, D1, DH = 5.45, 6.35, 1.95                                                               # drzwi piwnicy 0,9 × 1,95
    S.sciana(g, "cokol", P(U0, Yd, 0), t, U1 - U0, -.3 - Yd, [(D0 - U0, D1 - U0, 0, -.3 - Yd)])
    S.quad(g, "drzwi_stal", P(D0, Yd, .06), P(D1, Yd, .06), P(D1, Yd + DH, .06), P(D0, Yd + DH, .06), t=t, kier=n)
    for (a_, b_, kk) in (((D0, Yd), (D0, Yd + DH), t), ((D1, Yd + DH), (D1, Yd), -t)):          # ościeża drzwi
        S.quad(g, "cokol", P(*a_, 0), P(*b_, 0), P(*b_, .06), P(*a_, .06), kier=kk)
    S.quad(g, "cokol", P(D0, Yd + DH, 0), P(D0, Yd + DH, .06), P(D1, Yd + DH, .06), P(D1, Yd + DH, 0), kier=-UP)
    S.box(g, "stal", P(D1 - .16, Yd + 1.0, .02), t, UP, n, .1, .03, .05)                         # klamka


def drzwi(g, P, t, n, G, D0):
    W = PAS_KLATKI; d = G + .06
    fr = .06
    for (u0, u1, v0, v1) in ((0, fr, D0, D0 + 2.45), (W - fr, W, D0, D0 + 2.45), (0, W, D0 + 2.45 - fr, D0 + 2.45),
                             (0, W, D0 + 2.03, D0 + 2.03 + fr)):
        S.box(g, "drzwi_czerwone", P(u0, v0, d + .05), t, UP, n, u1 - u0, v1 - v0, .07, bez=("-z",))
    # skrzydło: rama 8 cm, dół płyta do 0,9 m, góra dwie szyby zbrojone
    a, b, c, e = fr, W - fr, D0, D0 + 2.03
    S.box(g, "drzwi_czerwone", P(a, c, d + .03), t, UP, n, b - a, .9, .04, bez=("-z",))
    for (u0, u1, v0, v1) in ((a, a + .08, c + .9, e), (b - .08, b, c + .9, e), (a, e - .08, c + .9, e), ((a + b) / 2 - .03, (a + b) / 2 + .03, c + .9, e - .08)):
        S.box(g, "drzwi_czerwone", P(u0, v0, d + .03), t, UP, n, u1 - u0, v1 - v0, .045, bez=("-z",))
    S.box(g, "drzwi_czerwone", P(a, e - .08, d + .03), t, UP, n, b - a, .08, .045, bez=("-z",))
    S.quad(g, "szyba_drzwi", P(a + .08, c + .9, d + .05), P((a + b) / 2 - .03, c + .9, d + .05), P((a + b) / 2 - .03, e - .08, d + .05), P(a + .08, e - .08, d + .05), t=t, kier=n)
    S.quad(g, "szyba_drzwi", P((a + b) / 2 + .03, c + .9, d + .05), P(b - .08, c + .9, d + .05), P(b - .08, e - .08, d + .05), P((a + b) / 2 + .03, e - .08, d + .05), t=t, kier=n)
    S.quad(g, "szyba_drzwi", P(fr, D0 + 2.03 + fr, d + .08), P(W - fr, D0 + 2.03 + fr, d + .08), P(W - fr, D0 + 2.45 - fr, d + .08), P(fr, D0 + 2.45 - fr, d + .08), t=t, kier=n)
    S.box(g, "stal", P(b - .14, c + 1.0, d - .01), t, UP, n, .1, .03, .05)          # klamka
    # ościeża wnęki drzwi (głębokość d) — boki
    S.quad(g, "tynk", P(W, D0, 0), P(W, D0, d), P(W, D0 + 2.45, d), P(W, D0 + 2.45, 0), kier=-t)
    S.box(g, "beton_schody", P(0, D0 - .02, d + .12), t, UP, n, W, .02, d + .12)   # próg


def piwnica(o, t, otwory, n=None):
    n = np.array([0., 0, 1]) if n is None else n; P = lambda u, v, d=0.0: o + t * u + UP * v - n * d
    for (u0, u1, v0, v1) in otwory:
        dd = .15
        for (a, b, c, e), kk in (((P(u0, v1, 0), P(u0, v1, dd), P(u1, v1, dd), P(u1, v1, 0)), -UP),
                                 ((P(u0, v0, 0), P(u0, v1, 0), P(u0, v1, dd), P(u0, v0, dd)), t),
                                 ((P(u1, v0, 0), P(u1, v0, dd), P(u1, v1, dd), P(u1, v1, 0)), -t),
                                 ((P(u0, v0, 0), P(u1, v0, 0), P(u1, v0, dd), P(u0, v0, dd)), UP)):
            S.quad("piwnica", "cokol", a, b, c, e, kier=kk)
        S.quad("piwnica", "szyba_piwnica", P(u0, v0, dd), P(u1, v0, dd), P(u1, v1, dd), P(u0, v1, dd), t=t, kier=n)
        for u in np.linspace(u0 + .1, u1 - .1, 5):
            S.rura("piwnica", "stal", P(u, v0, .04), P(u, v1, .04), .009, seg=6)


def skrzynka_gaz(o2, t):
    n = np.array([0., 0, 1]); P = lambda u, v, d=0.0: o2 + t * u + UP * v - n * d
    S.box("sciana_N_wsch", "skrzynka_gaz", P(4.4, .32, 0), t, UP, n, .5, .55, .24, bez=("-z",))
    S.rura("sciana_N_wsch", "rura_gaz", P(4.95, -.2, -.08), P(4.95, .62, -.08), .025)
    S.rura("sciana_N_wsch", "rura_gaz", P(4.9, .62, -.08), P(5.2, .62, -.08), .025)


def dach():
    g = "dach"
    for (x0, x1, z0, z1) in ((0, SZ_POL, 0, DL), (SZ_POL, 2 * SZ_POL, USKOK, DL + USKOK)):
        S.quad(g, "papa", np.array([x0, DACH, z0]), np.array([x0, DACH, z1]), np.array([x1, DACH, z1]), np.array([x1, DACH, z0]), kier=UP)
    # obróbka attyki: blacha 5 cm wysunięta 4 cm, po obwodzie (od zewnątrz)
    obwod = [(0, 0), (0, DL), (SZ_POL, DL), (SZ_POL, DL + USKOK), (2 * SZ_POL, DL + USKOK), (2 * SZ_POL, USKOK), (SZ_POL, USKOK), (SZ_POL, 0), (0, 0)]
    for (xa, za), (xb, zb) in zip(obwod, obwod[1:]):
        a = np.array([xa, ATTYKA, za]); b = np.array([xb, ATTYKA, zb]); tt = nrm(b - a); nn = np.cross(tt, UP)
        L = float(np.linalg.norm(b - a))
        S.box(g, "obrobka", a - tt * .04 + nn * .04 - UP * .06, tt, UP, -nn, L + .08, .08, .45)


def maszynownia():
    """Nadbudówka maszynowni windy (zdjęcia z lotu ptaka właściciela IMG_1638/1639): na styku połówek, 4,8 x 12 m, 3 m nad
    dachem, tynk, attyka z obróbką; kominki wentylacyjne 0,6 x 0,6 w rzędzie wzdłuż osi."""
    g = "dach"; x0, x1 = SZ_POL - 2.4, SZ_POL + 2.4; zc = (DL + USKOK) / 2; z0, z1 = zc - 6, zc + 6; H = 3.0
    for (a, b, t) in (((x0, z0), (x1, z0), np.array([1., 0, 0])), ((x1, z0), (x1, z1), np.array([0., 0, 1])),
                      ((x1, z1), (x0, z1), np.array([-1., 0, 0])), ((x0, z1), (x0, z0), np.array([0., 0, -1]))):
        L = float(np.hypot(b[0] - a[0], b[1] - a[1]))
        S.sciana(g, "tynk", np.array([a[0], DACH, a[1]]), t, L, H, [])
    S.quad(g, "papa", np.array([x0, DACH + H, z0]), np.array([x0, DACH + H, z1]), np.array([x1, DACH + H, z1]), np.array([x1, DACH + H, z0]), kier=UP)
    for (xa, za), (xb, zb) in zip([(x0, z0), (x0, z1), (x1, z1), (x1, z0), (x0, z0)], [(x0, z1), (x1, z1), (x1, z0), (x0, z0), (x0, z1)]):
        a = np.array([xa, DACH + H, za]); b = np.array([xb, DACH + H, zb]); tt = nrm(b - a); nn = np.cross(tt, UP)
        S.box(g, "obrobka", a - tt * .04 + nn * .04 - UP * .06, tt, UP, -nn, float(np.linalg.norm(b - a)) + .08, .1, .3)
    S.box(g, "drzwi_stal", np.array([x0 - .001, DACH, zc - .45]), np.array([0., 0, 1]), UP, np.array([1., 0, 0]), .9, 2.0, .001)
    for k, zz in enumerate(np.linspace(2.5, DL + USKOK - 2.5, 6)):
        if z0 - .5 < zz < z1 + .5: continue
        xx = SZ_POL * (.5 if k % 2 else 1.5)
        S.box(g, "komin", np.array([xx - .3, DACH, zz - .3]), np.array([1., 0, 0]), UP, np.array([0., 0, 1]), .6, .9, .6)
        S.box(g, "obrobka", np.array([xx - .35, DACH + .9, zz - .35]), np.array([1., 0, 0]), UP, np.array([0., 0, 1]), .7, .05, .7)


def teren():
    """Tylko do podglądu wejścia: chodnik z kostki 4,2 m, krawężnik, jezdnia uliczki. Teren osiedla z mapy — osobno."""
    g = "teren_podglad"
    S.quad(g, "kostka", np.array([-4, 0, DL]), np.array([-4, 0, DL + 6.2]), np.array([24, 0, DL + 6.2]), np.array([24, 0, DL]), kier=UP)
    S.quad(g, "kostka", np.array([2 * SZ_POL, 0, DL + USKOK - .01]), np.array([2 * SZ_POL, 0, DL]), np.array([24, 0, DL]), np.array([24, 0, DL + USKOK - .01]), kier=UP)
    S.box(g, "kraweznik", np.array([-4, -.2, DL + 6.2]), np.array([1., 0, 0]), UP, np.array([0, 0, 1.]), 28, .32, .15)
    S.quad(g, "asfalt", np.array([-4, -.02, DL + 6.35]), np.array([-4, -.02, DL + 22]), np.array([24, -.02, DL + 22]), np.array([24, -.02, DL + 6.35]), kier=UP)
    S.quad(g, "trawa_podglad", np.array([-20, -.01, 0]), np.array([-20, -.01, DL]), np.array([-4, -.01, DL]), np.array([-4, -.01, 0]), kier=UP)


def zbuduj(nr, seed=0):
    """Nowa siatka wieżowca tego typu (28a/30a/32a/34a). Materiały kolorowe dostają przyrostek numeru, np. tynk_28a;
    pasek z graffiti i napis adresowy mają własne tekstury numeru. Zwraca (siatka, liczba okien)."""
    global S, SEED
    S = Siatka(); okno_nr[0] = 0; SEED = seed
    blok(z_terenem=False)
    zmiana = {"tynk": "tynk_" + nr, "tynk_brudny": "szczyt_" + nr, "napis_32a": "napis_" + nr,
              "graffiti_32a": "graffiti_" + nr if nr == "32a" else "szczyt_" + nr, "tabliczka_32a": "tabliczka"}
    nowe = {}
    for (g, m), c in S.dane().items():
        k = (g, zmiana.get(m, m))
        if k in nowe:
            b = len(nowe[k]["v"])
            for f in ("v", "n", "t", "uv", "uv1"): nowe[k][f] += c[f]
            nowe[k]["i"] += [b + x for x in c["i"]]
        else: nowe[k] = c
    S.cz = nowe
    return S, okno_nr[0]


if __name__ == "__main__":
    blok()
    out = Path(sys.argv[1] if len(sys.argv) > 1 else ".")
    out.mkdir(parents=True, exist_ok=True)
    zapisz_glb(S, out / "blok_32a.glb")
    print("trojkatow:", trojkaty(S), "okien:", okno_nr[0], "grup:", len(S.dane()))
