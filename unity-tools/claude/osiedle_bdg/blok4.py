"""MIASTO R1 — bloki czterokondygnacyjne przy Skłodowskiej-Curie (28, 30, 32, 34, 36, 38) — wspólny wzór.

Ze zdjęć właściciela (23.11): IMG_1611 szczyt bloku 30 z napisem „M. SKŁODOWSKIEJ-CURIE 30”, IMG_1605/1603 blok 32
z pawilonem, IMG_1418–1429 bloki przy garażach (pola w kolorach, pasy klatek z małymi oknami), IMG_1590 balkony przy
szczycie. Z mapy OSM: długość 45–50 m, szerokość 10–13 m, 4 kondygnacje, w obrysach 28 i 30 trzy występy klatek
(0,4 m, co ~15 m, po jednej długiej stronie).

Układ lokalny: X wzdłuż bloku (0..L), Z w poprzek (0..W), front z wejściami = ściana z = 0 (patrzy na -Z).
Parter na +0,90, kondygnacja 2,75, dach płaski z attyką i kominami.
Czego nie ma na zdjęciach i jest przyjęte (do listy w opisie): rozkład okien tylnej ściany (powtórzony rytm frontu bez
klatek), balkony tylko przy obu szczytach z tyłu (jak na IMG_1590 i IMG_1605).
"""
import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
from kit import Siatka, UP, nrm, trojkaty  # noqa: E402
import blok_32a as B  # noqa: E402

KOND = 2.75
POZIOM0 = 0.90
N_KOND = 4
DACH = POZIOM0 + N_KOND * KOND
ATTYKA = DACH + 0.45
COKOL = 0.90
PAS_DOL = 2.10            # dolny pas (mauve) do parapetów 1. piętra
KL_W, KL_WYST = 2.70, 0.35
OKNO_H, PARAPET = 1.25, 0.95


def sciana_pola(g, o, t, L, H, okna, pola, uv1=None):
    """Ściana z oknami podzielona na pola kolorów (u0,u1,v0,v1,materiał); otwory przycięte do pola — pola stykają się
    krawędziami, nic się nie nakłada. Okna (ramy, szyby, opaski) rysowane raz."""
    S = B.S
    dziury = [(u0 - B.OPASKA, u1 + B.OPASKA, v0, v1 + B.OPASKA) for (u0, u1, v0, v1, *_r) in okna]
    for (a, b, c, d, mat) in pola:
        dz = []
        for (u0, u1, v0, v1) in dziury:
            uu0, uu1, vv0, vv1 = max(u0, a), min(u1, b), max(v0, c), min(v1, d)
            if uu1 > uu0 + 1e-4 and vv1 > vv0 + 1e-4: dz.append((uu0 - a, uu1 - a, vv0 - c, vv1 - c))
        if mat.startswith("napis"):      # naklejka napisu: UV1 0..1 na samym polu
            S.sciana(g, mat, o + t * a + UP * c, t, b - a, d - c, dz, uv1_skala=(b - a, d - c), uv1_o=(0, 0))
        else:
            S.sciana(g, mat, o + t * a + UP * c, t, b - a, d - c, dz, uv1_skala=uv1, uv1_o=(a, c))
    n = nrm(np.cross(UP, t))
    P = lambda u, v, dd=0.0: o + t * u + UP * v - n * dd
    for (u0, u1, v0, v1, rodzaj, pietro) in okna:
        for aa, bb, cc, ddd in (((u0 - B.OPASKA, v0), (u0, v0), (u0, v1 + B.OPASKA), (u0 - B.OPASKA, v1 + B.OPASKA)),
                                ((u1, v0), (u1 + B.OPASKA, v0), (u1 + B.OPASKA, v1 + B.OPASKA), (u1, v1 + B.OPASKA)),
                                ((u0, v1), (u1, v1), (u1, v1 + B.OPASKA), (u0, v1 + B.OPASKA))):
            S.quad(g, "opaska", P(*aa), P(*bb), P(*cc), P(*ddd), t=t, kier=n)
        B.okno(g, o, t, n, u0, u1, v0, v1, rodzaj, pietro)


def pola_fasady(L, H, kl, nr_kol, przes=0.0):
    """Pola kolorów jak na zdjęciach: cokół szary do 0,9, dolny pas (pas_dol) do 2,1, wyżej beż z żółtymi polami
    schodkowo przesuniętymi co kondygnację; pasy klatek osobno (materiał klatka)."""
    pola = [(0, L, 0, COKOL, "cokol4")]
    granice = sorted({0.0, L} | {x for (a, b) in kl for x in (a, b)})
    for u0, u1 in zip(granice, granice[1:]):
        if any(abs(u0 - a) < 1e-6 and abs(u1 - b) < 1e-6 for a, b in kl):
            pola.append((u0, u1, COKOL, H, "klatka4")); continue
        pola.append((u0, u1, COKOL, PAS_DOL, "pas_dol_%d" % nr_kol))
        # schodkowe pola: na zmianę beż / żółty, podział w połowie odcinka przesunięty o 1/4 co kondygnację
        v = PAS_DOL
        for k in range(1, N_KOND + 1):
            v1 = POZIOM0 + k * KOND if k < N_KOND else H
            m = u0 + (u1 - u0) * (.5 + (.18 if (k + przes) % 2 else -.18))
            pola.append((u0, m, v, v1, "pole_a_%d" % nr_kol)); pola.append((m, u1, v, v1, "pole_b_%d" % nr_kol))
            v = v1
    return pola


def front(L, kl_c, nr_kol):
    """Ściana z wejściami (z = 0, patrzy na -Z; t = +X... widz od południa patrzy na północ: prawa ręka = +X)."""
    S = B.S
    o = np.array([0., 0, 0]); t = np.array([1., 0, 0])
    kl = [(c - KL_W / 2, c + KL_W / 2) for c in kl_c]
    okna = []
    for k in range(N_KOND):
        fl = POZIOM0 + k * KOND
        for (a, b) in kl:
            for strona in (-1, 1):
                kraw = a if strona < 0 else b
                # przy klatce: para wąskich okien (kuchnia, łazienka), dalej dwa okna pokoi
                for (d0, w) in ((.55, .6), (1.35, .6), (2.7, 1.3), (4.6, 1.3)):
                    u0 = kraw - d0 - w if strona < 0 else kraw + d0
                    if 0.6 < u0 and u0 + w < L - .6:
                        okna.append((u0, u0 + w, fl + PARAPET + (.45 if w < 1 else 0), fl + PARAPET + OKNO_H, "okno" if w > 1 else "klatka", k))
    okna = sorted(set(okna))
    H = ATTYKA
    pola = pola_fasady(L, H, kl, nr_kol)
    sciana_pola("front", o, t, L, H, okna, pola, uv1=(L, H))
    for c in kl_c: klatka4("front", o, t, c)
    return okna


def klatka4(g, o, t, c):
    """Pas klatki: wysunięty 0,35 m, małe okna spocznikowe (2 na kondygnację), drzwi z naświetlem, daszek, schody."""
    S = B.S; n = nrm(np.cross(UP, t))
    a, b = c - KL_W / 2, c + KL_W / 2; d = KL_WYST
    P = lambda u, v, dd=0.0: o + t * u + UP * v - n * dd
    # boki wysunięcia
    S.quad(g, "klatka4", P(a, 0, 0), P(a, 0, -d), P(a, ATTYKA, -d), P(a, ATTYKA, 0), kier=-t)
    S.quad(g, "klatka4", P(b, 0, -d), P(b, 0, 0), P(b, ATTYKA, 0), P(b, ATTYKA, -d), kier=t)
    o2 = o - n * d
    okna = []
    for k in range(1, N_KOND):
        fl = POZIOM0 + k * KOND - KOND / 2
        for dv in (.55, 1.15):
            okna.append((c - .45, c + .45, fl + dv, fl + dv + .42, "klatka", k))
    okna.append((c - .45, c + .45, POZIOM0 + 2.35, POZIOM0 + 2.7, "klatka", 0))   # naświetle nad drzwiami
    dz = [(u0 - B.OPASKA, u1 + B.OPASKA, v0, v1 + B.OPASKA) for (u0, u1, v0, v1, *_r) in okna] + [(c - .55, c + .55, POZIOM0, POZIOM0 + 2.2)]
    S.sciana(g, "klatka4", o2 + t * a, t, KL_W, ATTYKA, [(u0 - a, u1 - a, v0, v1) for (u0, u1, v0, v1) in dz])
    for (u0, u1, v0, v1, r, p) in okna:
        for aa, bb, cc, ddd in (((u0 - B.OPASKA, v0), (u0, v0), (u0, v1 + B.OPASKA), (u0 - B.OPASKA, v1 + B.OPASKA)),
                                ((u1, v0), (u1 + B.OPASKA, v0), (u1 + B.OPASKA, v1 + B.OPASKA), (u1, v1 + B.OPASKA)),
                                ((u0, v1), (u1, v1), (u1, v1 + B.OPASKA), (u0, v1 + B.OPASKA))):
            S.quad(g, "opaska", o2 + t * aa[0] + UP * aa[1], o2 + t * bb[0] + UP * bb[1], o2 + t * cc[0] + UP * cc[1], o2 + t * ddd[0] + UP * ddd[1], t=t, kier=n)
        B.okno(g, o2, t, n, u0, u1, v0, v1, r, p)
    # drzwi: wnęka 0,25 m, drzwi brązowe z szybą (jak IMG_1424)
    Q = lambda u, v, dd=0.0: o2 + t * u + UP * v - n * dd
    for (pa, pb, kk) in (((c - .55, POZIOM0), (c - .55, POZIOM0 + 2.2), t), ((c + .55, POZIOM0 + 2.2), (c + .55, POZIOM0), -t)):
        S.quad(g, "klatka4", Q(*pa, 0), Q(*pb, 0), Q(*pb, .25), Q(*pa, .25), kier=kk)
    S.quad(g, "klatka4", Q(c - .55, POZIOM0 + 2.2, 0), Q(c - .55, POZIOM0 + 2.2, .25), Q(c + .55, POZIOM0 + 2.2, .25), Q(c + .55, POZIOM0 + 2.2, 0), kier=-UP)
    S.quad(g, "drzwi_braz", Q(c - .55, POZIOM0, .25), Q(c + .55, POZIOM0, .25), Q(c + .55, POZIOM0 + 2.2, .25), Q(c - .55, POZIOM0 + 2.2, .25), t=t, kier=n)
    S.quad(g, "szyba_drzwi", Q(c - .4, POZIOM0 + 1.0, .24), Q(c + .4, POZIOM0 + 1.0, .24), Q(c + .4, POZIOM0 + 2.0, .24), Q(c - .4, POZIOM0 + 2.0, .24), t=t, kier=n)
    # daszek i schody (4 stopnie po 0,225 do podestu)
    S.box(g, "beton_daszek", Q(c - 1.2, POZIOM0 + 2.75, -1.2), t, UP, n, 2.4, .14, 1.2)
    S.box(g, "obrobka", Q(c - 1.22, POZIOM0 + 2.89, -1.22), t, UP, n, 2.44, .03, 1.22)
    S.box(g, "beton_schody", Q(c - .8, -.2, -1.0), t, UP, n, 1.6, POZIOM0 + .2, 1.0)
    for i in range(1, 4):
        S.box(g, "beton_schody", Q(c - .8, -.2, -1.0 - .28 * i), t, UP, n, 1.6, POZIOM0 + .2 - .225 * i, .28)
    S.rura(g, "stal", Q(c + .85, POZIOM0 + .9, -.1), Q(c + .85, .9, -1.0 - .28 * 3), .022)
    S.rura(g, "stal", Q(c + .85, .0, -1.0 - .28 * 3), Q(c + .85, .9, -1.0 - .28 * 3), .022)
    S.rura(g, "stal", Q(c + .85, POZIOM0, -.1), Q(c + .85, POZIOM0 + .9, -.1), .022)
    S.box(g, "tabliczka", Q(c + .65, POZIOM0 + 2.3, -.015), t, UP, n, .22, .16, .015)


def tyl(L, W, nr_kol, seed):
    S = B.S
    o = np.array([L, 0., W]); t = np.array([-1., 0, 0])
    okna = []; bal = []
    przesla = int(L // 3.9); krok = L / przesla
    for k in range(N_KOND):
        fl = POZIOM0 + k * KOND
        for i in range(przesla):
            c = (i + .5) * krok
            if i in (0, przesla - 1):          # balkony przy szczytach: drzwi + okno
                okna.append((c - 1.25, c - .45, fl + .05, fl + PARAPET + OKNO_H, "drzwi", k))
                okna.append((c - .30, c + 1.15, fl + PARAPET, fl + PARAPET + OKNO_H, "okno", k))
                bal.append((c - 1.4, c + 1.3, fl))
            else:
                okna.append((c - .7, c + .7, fl + PARAPET, fl + PARAPET + OKNO_H, "okno", k))
    pola = pola_fasady(L, ATTYKA, [], nr_kol, przes=1)
    sciana_pola("tyl", o, t, L, ATTYKA, okna, pola, uv1=(L, ATTYKA))
    import random
    R = random.Random(seed)
    n = nrm(np.cross(UP, t))
    for (a, b, fl) in bal:
        B.balkon("tyl", o, t, n, a, b, fl, R.choice(B.BALUSTRADY))


def szczyt(L, W, strona, nr_kol, napis=None):
    S = B.S
    if strona == "W": o = np.array([0., 0, W]); t = np.array([0., 0, -1])
    else: o = np.array([L, 0., 0]); t = np.array([0., 0, 1])
    okna = []
    for k in range(N_KOND):
        fl = POZIOM0 + k * KOND
        okna.append((W * .36 - .45, W * .36 + .45, fl + PARAPET, fl + PARAPET + OKNO_H * .85, "klatka", k))
    pola = [(0, W, 0, COKOL, "cokol4"), (0, W, COKOL, PAS_DOL, "pas_dol_%d" % nr_kol)]
    if napis:
        NAP = (W * .5, W - .5, DACH - 2.6, DACH - .5)
        pola += [(0, W, PAS_DOL, NAP[2], "szczyt4_%d" % nr_kol), (0, NAP[0], NAP[2], NAP[3], "szczyt4_%d" % nr_kol),
                 (NAP[0], NAP[1], NAP[2], NAP[3], napis), (NAP[1], W, NAP[2], NAP[3], "szczyt4_%d" % nr_kol),
                 (0, W, NAP[3], ATTYKA, "szczyt4_%d" % nr_kol)]
    else:
        pola.append((0, W, PAS_DOL, ATTYKA, "szczyt4_%d" % nr_kol))
    sciana_pola("szczyt_" + strona, o, t, W, ATTYKA, okna, pola, uv1=(W, ATTYKA))


def dach4(L, W, seed):
    S = B.S; g = "dach"
    S.quad(g, "papa", np.array([0., DACH, 0]), np.array([0., DACH, W]), np.array([L, DACH, W]), np.array([L, DACH, 0]), kier=UP)
    for (xa, za), (xb, zb) in zip([(0, 0), (0, W), (L, W), (L, 0), (0, 0)], [(0, W), (L, W), (L, 0), (0, 0), (0, W)]):
        a = np.array([xa, ATTYKA, za]); b = np.array([xb, ATTYKA, zb]); tt = nrm(b - a); nn = np.cross(tt, UP)
        S.box(g, "obrobka", a - tt * .04 + nn * .04 - UP * .06, tt, UP, -nn, float(np.linalg.norm(b - a)) + .08, .08, .35)
    # kominy z daszkami (IMG_1420: rząd kominów wzdłuż kalenicy, co ~4 m)
    import random
    R = random.Random(seed)
    for x in np.arange(3.0, L - 2, 4.1):
        z = W * (.42 + .16 * R.random())
        S.box(g, "komin", np.array([x - .35, DACH, z - .3]), np.array([1., 0, 0]), UP, np.array([0., 0, 1]), .7, 1.25, .6)
        S.box(g, "obrobka", np.array([x - .42, DACH + 1.55, z - .37]), np.array([1., 0, 0]), UP, np.array([0., 0, 1]), .84, .04, .74)
        for xx in (x - .38, x + .32):
            S.box(g, "stal", np.array([xx, DACH + 1.25, z - .3]), np.array([1., 0, 0]), UP, np.array([0., 0, 1]), .06, .3, .06)


def zbuduj(L, W, nr, nr_kol, klatki=3, seed=0, napis_szczyt=None, napis_strona="W"):
    """Nowa siatka bloku. Kolory: pas_dol_<k>, pole_a_<k>, pole_b_<k>, szczyt4_<k> (paleta w podglądzie / materiały)."""
    stare = (B.POZIOM0, B.KOND, B.S, B.okno_nr[0])
    B.POZIOM0, B.KOND = POZIOM0, KOND
    B.S = Siatka(); B.okno_nr[0] = 0
    try:
        kl_c = [L * (2 * i + 1) / (2 * klatki) for i in range(klatki)]
        front(L, kl_c, nr_kol); tyl(L, W, nr_kol, seed)
        szczyt(L, W, "W", nr_kol, napis_szczyt if napis_strona == "W" else None)
        szczyt(L, W, "E", nr_kol, napis_szczyt if napis_strona == "E" else None)
        dach4(L, W, seed)
        S = B.S; okien = B.okno_nr[0]
    finally:
        B.POZIOM0, B.KOND, B.S, B.okno_nr[0] = stare
    return S, okien


if __name__ == "__main__":
    from kit import zapisz_glb
    S, n = zbuduj(47.0, 11.6, "30", 1, napis_szczyt="napis_blok_30")
    out = Path(sys.argv[1] if len(sys.argv) > 1 else "."); out.mkdir(parents=True, exist_ok=True)
    zapisz_glb(S, out / "blok4.glb")
    print("trojkatow", trojkaty(S), "okien", n, sorted({m for _, m in S.dane()}))
