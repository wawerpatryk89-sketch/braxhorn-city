"""New wheels for the owner's classics (owner 25.11.2026 03:25: "koła to trzeba wszystkie ... zrobić nowe ... ma być guma
normalnie, felga, jak powinno być"): Tripo's wheels are lumps with garbled sidewalls and smeared rims, so each car's
wheels are built here - a rubber tyre (its tread grooved, rounded shoulders, a plain sidewall, the bead) and a rim of
the car's kind, at the tyre's real size (its width and the rim's diameter from the car's tyre, its outer radius the one
the car stands on).

Rims (the owner's pictures of each car as it should look):
  stalowa      a pressed steel wheel - its face a shallow dish with the vent holes, a chrome hub cap (the stock cars)
  ramiona5     five spokes from a hub to the lip (the tuned 126p)
  ramiona      many slim spokes, a polished lip (the tuned 125p and Polonez)
  siatka       a wire-mesh face behind a deep polished lip, a chrome centre (BBS-style: the brown Trabant)
  ciezarowa    a truck's steel disc, deep, its ring of nuts (the Stars)

All share one 1024 px atlas (kola_kolor / kola_normal__lin / kola_mr__lin): the tread, the rubber, the mesh, the flat
colours of rims. A wheel is built with its axle on x, its outer face toward +x (a right-hand wheel mirrored), its
centre at the origin, in metres."""
import numpy as np
from PIL import Image, ImageDraw

A = 1024
# the flat colour patches (row v 0.5-1.0, 8 patches across): sRGB, roughness, metal
PLATY = {
    "chrom": ((214, 218, 224), .10, .60), "srebrny": ((182, 186, 192), .28, .35), "bialy": ((232, 232, 228), .30, .0),
    "czarny": ((18, 18, 20), .25, .0), "grafit": ((64, 66, 70), .28, .45), "zloty": ((168, 132, 72), .25, .50),
    "stal": ((46, 48, 52), .55, .30), "kremowy": ((226, 214, 186), .32, .0),
}
KOLEJNOSC = list(PLATY)


def _plat(nazwa):
    i = KOLEJNOSC.index(nazwa); return ((i + .5) / 8, .75)


def atlas():
    """the wheels' atlas: colour (sRGB), normal (tangent space, +Y), metal-roughness (R 1, G roughness, B metal)"""
    kol = np.zeros((A, A, 3), np.uint8); mr = np.zeros((A, A, 3), np.uint8); mr[..., 0] = 255
    h = np.zeros((A, A), np.float32)                                   # the relief whose slope is the normal map
    # the tread (u 0-0.5 once round, v 0-0.25 shoulder to shoulder): ribs and zig-zag grooves, the shoulders' blocks
    yy, xx = np.mgrid[0:A // 4, 0:A // 2].astype(np.float32)
    s = yy / (A / 4); t = xx / (A / 2)
    g = np.zeros_like(s)
    for c in (.3, .5, .7):                                             # three circumferential grooves, zig-zagging
        g = np.maximum(g, (np.abs(s - c - .012 * np.sign(np.sin(t * 2 * np.pi * 60))) < .022).astype(np.float32))
    bl = ((s < .18) | (s > .82)) & (np.mod(t * 90, 1) < .14)             # the shoulders' cross grooves
    g = np.maximum(g, bl.astype(np.float32))
    sip = (np.mod(t * 180 + s * 3, 1) < .05) & (s > .3) & (s < .7)       # the sipes in the middle ribs
    g = np.maximum(g, .5 * sip.astype(np.float32))
    k = np.clip(28 - g * 14, 8, 40).astype(np.uint8)
    kol[:A // 4, :A // 2] = np.stack([k, k, k + 1], -1); h[:A // 4, :A // 2] = 1 - g
    mr[:A // 4, :A // 2, 1] = 225; mr[:A // 4, :A // 2, 2] = 0
    # the sidewall's rubber (u 0.5-1, v 0-0.25): plain, a fine ring near the bead
    kol[:A // 4, A // 2:] = (24, 24, 26); mr[:A // 4, A // 2:, 1] = 200
    kol[A // 8 - 4:A // 8 + 4, A // 2:] = (30, 30, 32)
    # the wire mesh (u 0-0.5, v 0.25-0.5): a cross-hatch of polished wires on a dark back
    yy, xx = np.mgrid[0:A // 4, 0:A // 2].astype(np.float32)
    w = (np.abs(np.mod((xx + yy) / 18, 1) - .5) < .12) | (np.abs(np.mod((xx - yy) / 18, 1) - .5) < .12)
    kol[A // 4:A // 2, :A // 2] = np.where(w[..., None], np.array((200, 204, 210), np.uint8), np.array((22, 22, 24), np.uint8))
    mr[A // 4:A // 2, :A // 2, 1] = np.where(w, 30, 160); mr[A // 4:A // 2, :A // 2, 2] = np.where(w, 150, 0)
    h[A // 4:A // 2, :A // 2] = w.astype(np.float32)
    # the steel wheel's face (u 0.5-1, v 0.25-0.5): left plain (its colour from a patch), a darker ring for the vents
    kol[A // 4:A // 2, A // 2:] = (40, 40, 44); mr[A // 4:A // 2, A // 2:, 1] = 150
    # the flat patches
    for i, nazwa in enumerate(KOLEJNOSC):
        c, r, m = PLATY[nazwa]; x0, x1 = i * A // 8, (i + 1) * A // 8
        kol[A // 2:, x0:x1] = c; mr[A // 2:, x0:x1, 1] = int(r * 255); mr[A // 2:, x0:x1, 2] = int(m * 255)
    gy, gx = np.gradient(h); sila = 3.0
    n = np.stack([-gx * sila, gy * sila, np.ones_like(h)], -1); n /= np.linalg.norm(n, axis=2, keepdims=True)
    nm = ((n * .5 + .5) * 255 + .5).astype(np.uint8)
    return Image.fromarray(kol), Image.fromarray(nm), Image.fromarray(mr)


class _Siatka:
    def __init__(self): self.P, self.N, self.U, self.I = [], [], [], []

    def obrot(self, prof, uv, n=64, uv_kolo=None):
        """a profile [(x, r), ...] turned round the x axis: its normals from the profile (left of the way it runs);
        uv: a constant (u, v), or 'bieznik' / 'bok' / 'tarcza' for the atlas' tread, sidewall, steel face"""
        prof = np.array(prof, float); m = len(prof)
        tg = np.zeros_like(prof)
        tg[1:-1] = prof[2:] - prof[:-2]; tg[0] = prof[1] - prof[0]; tg[-1] = prof[-1] - prof[-2]
        tg /= np.maximum(np.linalg.norm(tg, axis=1, keepdims=True), 1e-12)
        n2 = np.stack([-tg[:, 1], tg[:, 0]], 1)                     # (dx, dr) of the normal
        b = len(self.P) and sum(len(p) for p in self.P)
        ang = np.linspace(0, 2 * np.pi, n + 1)
        P, N, U = [], [], []
        dl = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(prof, axis=0), axis=1))]); dl /= max(dl[-1], 1e-9)
        for j, a in enumerate(ang):
            ca, sa = np.cos(a), np.sin(a)
            for i in range(m):
                x, r = prof[i]; P.append((x, r * ca, r * sa)); N.append((n2[i, 0], n2[i, 1] * ca, n2[i, 1] * sa))
                if uv == "bieznik": U.append((.5 * j / n, .25 * dl[i]))
                elif uv == "bok": U.append((.5 + .5 * j / n, .25 * dl[i]))
                elif uv == "tarcza": U.append((.5 + .5 * j / n, .25 + .25 * dl[i]))
                elif uv == "siatka": U.append((.25 + .24 * (r / uv_kolo) * ca, .375 + .12 * (r / uv_kolo) * sa))
                else: U.append(uv)
        I = []
        for j in range(n):
            for i in range(m - 1):
                a0 = b + j * m + i; a1 = a0 + 1; c0 = a0 + m; c1 = c0 + 1
                I += [(a0, c0, c1), (a0, c1, a1)]
        self._dodaj(P, N, U, I)

    def _dodaj(self, P, N, U, I):
        self.P.append(np.array(P, float)); self.N.append(np.array(N, float)); self.U.append(np.array(U, float)); self.I.append(np.array(I, np.int64))

    def pryzma(self, quad, x0, x1, uv):
        """a spoke: its outline (4 corners (r, angle)) as a face at x1 (+x) with its sides back to x0"""
        c = [(r * np.cos(a), r * np.sin(a)) for r, a in quad]
        b = sum(len(p) for p in self.P); P, N, U, I = [], [], [], []
        for y, z in c: P.append((x1, y, z)); N.append((1, 0, 0)); U.append(uv)
        I += [(b, b + 1, b + 2), (b, b + 2, b + 3)]
        for k in range(4):
            (y0, z0), (y1, z1) = c[k], c[(k + 1) % 4]; nn = np.array((0, z1 - z0, -(y1 - y0)), float); nn /= max(np.linalg.norm(nn), 1e-12)
            q = len(P) + b
            for x, y, z in ((x1, y0, z0), (x1, y1, z1), (x0, y1, z1), (x0, y0, z0)): P.append((x, y, z)); N.append(tuple(nn)); U.append(uv)
            I += [(q, q + 1, q + 2), (q, q + 2, q + 3)]
        self._dodaj(P, N, U, I)

    def wynik(self):
        P = np.concatenate(self.P); N = np.concatenate(self.N); U = np.concatenate(self.U); I = np.concatenate(self.I)
        # the winding by the normals (glTF counter-clockwise seen from the front)
        t = P[I]; fn = np.cross(t[:, 1] - t[:, 0], t[:, 2] - t[:, 0]); nv = N[I].sum(1)
        flip = (fn * nv).sum(1) < 0; I[flip] = I[flip][:, ::-1]
        N /= np.maximum(np.linalg.norm(N, axis=1, keepdims=True), 1e-12)
        return P, N, U, I


def kolo(R, szer, cale, styl, kolor="srebrny", n=48):
    """one wheel: tyre outer radius R, its width szer, the rim's diameter in inches; its outer face toward +x"""
    S = _Siatka(); Rr = cale * .0254 / 2 + .012; w = szer; h = R - Rr
    # the tyre: bead, sidewall, rounded shoulder, the tread across, and back down the outer side
    prof = [(-w * .42, Rr), (-w * .48, Rr + h * .22), (-w * .505, Rr + h * .55), (-w * .48, Rr + h * .84), (-w * .43, R - h * .04),
            (-w * .36, R - .002), (w * .36, R - .002), (w * .43, R - h * .04), (w * .48, Rr + h * .84), (w * .505, Rr + h * .55),
            (w * .48, Rr + h * .22), (w * .42, Rr)]
    S.obrot(prof[:5], "bok", n); S.obrot(prof[4:8], "bieznik", n); S.obrot(prof[7:], "bok", n)
    # inside the tyre: the rim's barrel (seen through the spokes), dark steel, and a brake drum
    S.obrot([(w * .40, Rr * .99), (-w * .40, Rr * .99)], _plat("stal"), n)
    S.obrot([(-w * .05, Rr * .70), (-w * .05, Rr * .18), (-w * .30, Rr * .18)], _plat("stal"), n)
    c = _plat(kolor); ch = _plat("chrom")
    if styl == "stalowa":
        # the pressed face: the lip, a shallow dish (the vents in it), the centre, a domed chrome hub cap
        S.obrot([(w * .44, Rr * 1.03), (w * .40, Rr * .96), (w * .30, Rr * .88), (w * .27, Rr * .70)], c, n)
        S.obrot([(w * .27, Rr * .70), (w * .26, Rr * .52)], _plat("grafit") if kolor != "czarny" else c, n)
        S.obrot([(w * .26, Rr * .52), (w * .30, Rr * .50), (w * .38, Rr * .40), (w * .44, Rr * .22), (w * .46, 0.0)], ch, n)
    elif styl == "ciezarowa":
        S.obrot([(w * .42, Rr * 1.02), (w * .36, Rr * .95), (w * .10, Rr * .80), (w * .06, Rr * .45)], c, n)
        S.obrot([(w * .06, Rr * .45), (w * .14, Rr * .38), (w * .16, Rr * .20), (w * .24, 0.0)], _plat("stal"), n)
        for k in range(8):                                          # the wheel nuts
            a = k * 2 * np.pi / 8; d = .022
            S.pryzma([(Rr * .30 - d, a - .09), (Rr * .30 + d, a - .07), (Rr * .30 + d, a + .07), (Rr * .30 - d, a + .09)], w * .06, w * .14, ch)
    else:
        gl = {"ramiona5": .30, "ramiona": .32, "siatka": .14}[styl]  # how deep the face lies behind the lip (a dish)
        S.obrot([(w * .46, Rr * 1.03), (w * .44, Rr * .97), (w * gl, Rr * .92)], ch if styl != "ramiona5" else c, n)   # the lip
        if styl == "siatka":
            S.obrot([(w * gl, Rr * .92), (w * gl - .004, Rr * .30)], "siatka", n, uv_kolo=Rr * .92)
            S.obrot([(w * gl, Rr * .30), (w * gl + .02, Rr * .26), (w * gl + .035, Rr * .12), (w * gl + .04, 0.0)], ch, n)
        else:
            ile = 5 if styl == "ramiona5" else 10
            S.obrot([(w * gl, Rr * .30), (w * gl + .012, Rr * .24), (w * gl + .018, Rr * .10), (w * gl + .02, 0.0)], c, n)
            for k in range(ile):
                a = k * 2 * np.pi / ile; sz0 = (.30 if ile == 5 else .14); sz1 = (.16 if ile == 5 else .07)
                S.pryzma([(Rr * .27, a - sz0), (Rr * .93, a - sz1), (Rr * .93, a + sz1), (Rr * .27, a + sz0)], w * gl - .02, w * gl, c)
    return S.wynik()


def zamontuj(P, N, U, I, hx, hy, hz, prawe):
    """the wheel at its hub: a right-hand wheel (its outer face to -x) mirrored, its winding kept"""
    P = P.copy(); N = N.copy(); I = I.copy()
    if prawe: P[:, 0] = -P[:, 0]; N[:, 0] = -N[:, 0]; I = I[:, ::-1]
    return P + np.array([hx, hy, hz]), N, U, I
