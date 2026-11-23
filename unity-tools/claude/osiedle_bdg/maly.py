"""MIASTO R1 — osiedle bydgoskie: garaże, bar mleczny 34b, pawilony handlowe.

Ze zdjęć właściciela (23.11): garaże IMG_1418–1429 i IMG_1632 (rząd boksów po ~2,9 m, ściany tynkowane w kolorze
łososiowym/kremowym, białe pilastry między bramami, płaski dach z płyty z okapem, bramy drewniane dwuskrzydłowe
i brązowe uchylne); bar mleczny IMG_1631–1634 (parter 3,4 m, dach płaski z szerokim szarym okapem, witryny w białych
ramach na murku 0,9 m, dwa filary, drzwi białe, czarna tablica nad wejściem, mural Zawiszy na ścianie od strony 32a
— właściciel 15:28: „ten jeden mural Zawiszy”, drugi malunek NIE).
Układ lokalny każdego obiektu: X wzdłuż (0..L), Z w poprzek (0..W), front = ściana z = 0.
"""
import random
import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
from kit import Siatka, UP, nrm, trojkaty  # noqa: E402
import blok_32a as B  # noqa: E402

X, Z = np.array([1., 0, 0]), np.array([0., 0, 1])


def _pod(S):
    stare = (B.S, B.okno_nr[0]); B.S = S; B.okno_nr[0] = 0
    return stare


def dach_plaski(S, g, L, W, H, okap=.25, grub=.18, mat_okap="beton_daszek"):
    S.box(g, mat_okap, np.array([-okap, H, -okap]), X, UP, Z, L + 2 * okap, grub, W + 2 * okap)
    S.quad(g, "papa", np.array([-okap + .02, H + grub + .005, -okap + .02]), np.array([-okap + .02, H + grub + .005, W + okap - .02]),
           np.array([L + okap - .02, H + grub + .005, W + okap - .02]), np.array([L + okap - .02, H + grub + .005, -okap + .02]), kier=UP)


def garaze(L, W, seed=0, kolor="garaz_a"):
    """Rząd garaży: bramy od frontu (z = 0), boksy po L/n ≈ 2,9 m."""
    S = Siatka(); R = random.Random(seed); g = "garaze"; H = 2.45
    n = max(1, round(L / 2.9)); b = L / n
    bramy = []
    for i in range(n):
        u0 = i * b + .28; u1 = (i + 1) * b - .28
        bramy.append((u0, u1, .0, 2.05))
    # front: tynk z otworami bram; pilastry białe (osobne pola między bramami)
    pola = []
    for i in range(n):
        x0, x1 = i * b, (i + 1) * b
        S.sciana(g, "pilaster", np.array([x0, 0, 0]), X, .28, H, [])
        S.sciana(g, kolor, np.array([x0 + .28, 0, 0]), X, b - .56, H, [(0, b - .56, 0, 2.05)])
        S.sciana(g, "pilaster", np.array([x1 - .28, 0, 0]), X, .28, H, [])
        typ = R.choice(["brama_drewno_czerw", "brama_drewno_braz", "brama_uchylna", "brama_uchylna", "brama_drewno_braz"])
        u0, u1 = x0 + .28, x1 - .28; d = .08
        P = lambda u, v, dd=0.0: np.array([u, v, dd])
        S.quad(g, kolor, P(u0, 0, 0), P(u0, 2.05, 0), P(u0, 2.05, d), P(u0, 0, d), kier=X)
        S.quad(g, kolor, P(u1, 2.05, 0), P(u1, 0, 0), P(u1, 0, d), P(u1, 2.05, d), kier=-X)
        S.quad(g, kolor, P(u0, 2.05, 0), P(u1, 2.05, 0), P(u1, 2.05, d), P(u0, 2.05, d), kier=-UP)
        S.quad(g, typ, P(u0, 0, d), P(u1, 0, d), P(u1, 2.05, d), P(u0, 2.05, d), kier=-Z, t=-X, obrot=typ.startswith("brama_drewno"))
        if typ.startswith("brama_drewno"):
            S.box(g, "stal", P((u0 + u1) / 2 - .01, .1, d - .015), X, UP, -Z, .02, 1.85, .015)
            for (uu, vv) in ((u0 + .1, .3), (u0 + .1, 1.7), (u1 - .3, .3), (u1 - .3, 1.7)):
                S.box(g, "stal", P(uu, vv, d - .012), X, UP, -Z, .2, .04, .012)
        else:
            S.box(g, "stal", P((u0 + u1) / 2 - .1, .95, d - .02), X, UP, -Z, .2, .05, .02)
    # boki i tył: tynk (tył bywa z graffiti — naklejki dokładane osobno)
    S.sciana(g, kolor, np.array([L, 0, W]), -X, L, H, [], uv1_skala=(L, H))
    S.sciana(g, kolor, np.array([0, 0, W]), -Z, W, H, [])
    S.sciana(g, kolor, np.array([L, 0, 0]), Z, W, H, [])
    S.sciana(g, "cokol", np.array([0, -.25, 0]), X, L, .25, [])
    dach_plaski(S, g, L, W, H, okap=.15, grub=.16)
    return S


def bar_mleczny(L, W, mural=True):
    """Bar mleczny 34b. Front z witrynami (z = 0), mural na ścianie x = 0 (od strony 32a), tablica „BAR MLECZNY”."""
    S = Siatka(); stare = _pod(S); g = "bar"; H = 3.35
    try:
        o = np.array([0., 0, 0]); t = X; n = -Z
        # front: murek 0,9 + witryny w białych ramach; dwa filary 0,45 m
        fil = [L * .38, L * .72]
        okna = []
        segs = [(.45, fil[0] - .22), (fil[0] + .22, fil[1] - .22), (fil[1] + .22, L - .45)]
        drzwi = (segs[0][0] + 1.6, segs[0][0] + 2.6)
        for (a, b) in segs:
            k = max(1, round((b - a) / 1.7)); w = (b - a) / k
            for i in range(k):
                u0, u1 = a + i * w + .03, a + (i + 1) * w - .03
                if u0 < drzwi[1] and u1 > drzwi[0]:
                    continue
                okna.append((u0, u1, .9, 2.55, "witryna", 0))
                okna.append((u0, u1, 2.62, 3.0, "klatka", 0))
        dz = [(u0 - .0, u1 + .0, v0, v1) for (u0, u1, v0, v1, *_r) in okna] + [(drzwi[0], drzwi[1], 0, 2.55)]
        S.sciana(g, "tynk_bar", o, t, L, H, dz, uv1_skala=(L, H))
        for (u0, u1, v0, v1, r, p) in okna:
            B.okno(g, o, t, nrm(np.cross(UP, t)), u0, u1, v0, v1, "klatka" if r == "klatka" else "witryna", 0)
        nn = nrm(np.cross(UP, t))
        B.okno(g, o, t, nn, drzwi[0], drzwi[1], .02, 2.55, "drzwi", 0)
        # filary wystające 0,12 m
        for f in fil:
            S.box(g, "tynk_bar", np.array([f - .22, 0, -.12]), X, UP, Z, .44, H, .12, bez=("+z",))
        # tablica nad wejściem: czarna płyta z napisem
        S.box(g, "tablica_bar", np.array([fil[0] - 2.2, 2.75, -.05]), X, UP, Z, 2.0, .48, .05, bez=("+z",))
        # boki i tył
        # mural na ścianie x = L (patrząc na nią, front z witrynami jest po lewej — jak na IMG_1631); zajmuje 64 %
        # długości ściany od frontu (pomiar perspektywiczny z IMG_1631: 1/h liniowe wzdłuż ściany), reszta gładki tynk
        S.sciana(g, "mural_zawisza" if mural else "tynk_bar", np.array([L, 0, 0]), Z, W, H, [], uv1_skala=(W, H))
        S.sciana(g, "tynk_bar", np.array([0, 0, W]), -Z, W, H, [(W * .5 - 1.0, W * .5, 0, 2.1)], uv1_skala=(W, H))
        S.quad(g, "drzwi_stal", np.array([.06, 0, W * .5]), np.array([.06, 0, W * .5 + 1.0]), np.array([.06, 2.1, W * .5 + 1.0]), np.array([.06, 2.1, W * .5]), kier=-X)
        S.sciana(g, "tynk_bar", np.array([L, 0, W]), -X, L, H, [(L * .3, L * .3 + 1.2, 1.5, 2.4)], uv1_skala=(L, H))
        B.okno(g, np.array([L, 0, W]), -X, Z, L * .3, L * .3 + 1.2, 1.5, 2.4, "okno", 0)
        S.sciana(g, "cokol", np.array([0, -.2, 0]), X, L, .2, [])
        # dach z szerokim okapem z szarej blachy (pochylony lekko w stronę frontu)
        ok = .65
        S.box(g, "okap_blacha", np.array([-ok, H, -ok]), X, UP, Z, L + 2 * ok, .22, W + 2 * ok)
        S.quad(g, "papa", np.array([-ok + .02, H + .225, -ok + .02]), np.array([-ok + .02, H + .225, W + ok - .02]),
               np.array([L + ok - .02, H + .225, W + ok - .02]), np.array([L + ok - .02, H + .225, -ok + .02]), kier=UP)
    finally:
        B.S, B.okno_nr[0] = stare
    return S


def pawilon(L, W, H=3.3, front_mat="tynk_paw_a", pas=None, witryny=.7, seed=0):
    """Pawilon handlowy parterowy: witryny na froncie, pas reklamowy (bez prawdziwych szyldów), dach płaski."""
    S = Siatka(); stare = _pod(S); g = "pawilon"
    try:
        o = np.array([0., 0, 0]); t = X; nn = nrm(np.cross(UP, t))
        k = max(1, round(L * witryny / 2.0)); w = L * witryny / k; a0 = (L - L * witryny) / 2
        okna = [(a0 + i * w + .05, a0 + (i + 1) * w - .05, .45, 2.45, "witryna", 0) for i in range(k)]
        di = k // 2; u0, u1 = okna[di][0], okna[di][1]; okna[di] = (u0, u1, .02, 2.45, "drzwi", 0)
        S.sciana(g, front_mat, o, t, L, H, [(a, b, c, d) for (a, b, c, d, *_r) in okna], uv1_skala=(L, H))
        for (a, b, c, d, r, p) in okna: B.okno(g, o, t, nn, a, b, c, d, r, p)
        if pas: S.box(g, pas, np.array([0, 2.6, -.06]), X, UP, Z, L, .6, .06, bez=("+z",))
        S.sciana(g, front_mat, np.array([L, 0, W]), -X, L, H, [], uv1_skala=(L, H))
        S.sciana(g, front_mat, np.array([0, 0, W]), -Z, W, H, [])
        S.sciana(g, front_mat, np.array([L, 0, 0]), Z, W, H, [])
        S.sciana(g, "cokol", np.array([0, -.2, 0]), X, L, .2, [])
        dach_plaski(S, g, L, W, H, okap=.2, grub=.25, mat_okap="obrobka")
    finally:
        B.S, B.okno_nr[0] = stare
    return S


if __name__ == "__main__":
    from kit import zapisz_glb
    out = Path(sys.argv[1] if len(sys.argv) > 1 else "."); out.mkdir(parents=True, exist_ok=True)
    zapisz_glb(garaze(26.4, 6.9, 3), out / "garaze.glb")
    zapisz_glb(bar_mleczny(14.3, 13.1), out / "bar.glb")
    zapisz_glb(pawilon(19.7, 13.0, pas="pas_pomarancz"), out / "pawilon.glb")
    print("ok", trojkaty(garaze(26.4, 6.9, 3)), trojkaty(bar_mleczny(14.3, 13.1)), trojkaty(pawilon(19.7, 13.0)))
