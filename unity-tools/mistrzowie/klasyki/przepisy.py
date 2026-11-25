"""The restoration of each classic's colour map (owner 24.11.2026 22:54-23:07: "każde napisy, jak są źle, to zmazać i
nowe", "trzeba odrestaurować, napisy poprawić ... wyraźnie", "wszystko odrestaurować co trzeba").

Every box is in a view's metres (tekstura.WIDOKI: L the left side seen from +x with u = -z, P the right side with u = z,
F the front with u = x, T the rear with u = -x; v up from the ground) - read off the gridded orthographic pictures of
the vehicle at its real size. A recipe returns what przygotuj_klasyki needs besides the map:
  okna    the painted windows (view, box[, outlines measured off the painting]) - their faces become the tinted glass
          (przyciemniane=False: the glass clear, Tripo's painting of it kept);
  otwory  the open windows (view, box round the opening and its frame) - a pane is built in each;
  farby   the body's paints (sRGB 0-255): a window face of that colour is body, not glass;
  napisy  the lettering laid (for the catalogue).
Plates: 1976-2000, black with white characters - a private car 3 letters + 4 digits, a state vehicle 3 letters +
3 digits + a letter, the militia MO (autoblog.spidersweb.pl "polskie czarne tablice rejestracyjne"; auto-swiat.pl:
"MO - wyróżnik milicji")."""


import numpy as np


class _Skala:
    """a recipe read off views drawn at another length (the first look at a model, 4 m long): its places scaled by k
    (the views are centred on z and stand on y = 0, so u and v scale alike); a plate keeps its real size"""
    def __init__(self, mod, k): self.mod, self.k = mod, k
    def __getattr__(self, a): return getattr(self.mod, a)
    def b(self, box): return tuple(x * self.k for x in box)
    def zmaz(self, w, box, **kw): return self.mod.zmaz(w, self.b(box), **kw)
    def napis(self, w, t, u, v, cap, **kw): return self.mod.napis(w, t, u * self.k, v * self.k, cap * self.k, **kw)
    def tablica(self, w, t, u, v, *wh, **kw): return self.mod.tablica(w, t, u * self.k, v * self.k, *wh, **kw)
    def naklej(self, w, rgba, u, v, ww, hh, n=""): return self.mod.naklej(w, rgba, u * self.k, v * self.k, ww * self.k, hh * self.k, n)
    def okna(self, lst): return [(o[0], self.b(o[1])) + tuple(o[2:]) for o in lst]


def _dwa(mod, boxes, farba, progi=((.07, 4), (.06, 3))):
    """erase twice: the first pass takes the letters, the second their soft rims"""
    for prog, roz in progi:
        for w, b in boxes: mod.zmaz(w, b, farba=farba, prog=prog, rozszerz=roz)


def milicja(mod):
    """the Polonez Truck of the militia: deep blue, its white stripe at the waist straight and clean (as Tripo drew it,
    measured: 8 cm from the cab back, narrowing to 5.5 cm over the bonnet's flank), MILICJA in clean letters, MO plates"""
    NIEB = (20, 64, 100)
    Z = [("L", (-0.84, -0.06, 0.575, 0.835)), ("L", (0.24, 1.84, 0.985, 1.13)), ("L", (1.50, 1.90, 1.16, 1.54)), ("L", (0.38, 0.68, 0.60, 0.88)),
         ("P", (-1.96, -0.34, 0.985, 1.13)), ("P", (-1.90, -1.46, 1.16, 1.54)), ("P", (-0.80, -0.50, 0.60, 0.88)), ("P", (-0.04, 0.66, 0.575, 0.835)),
         ("F", (-0.62, 0.62, 1.58, 1.76)), ("T", (-0.60, 0.60, 0.985, 1.13)), ("F", (-0.22, 0.22, 1.85, 1.98)), ("T", (-0.22, 0.22, 1.85, 1.98))]
    _dwa(mod, Z, NIEB)
    mod.zmaz("T", (-0.32, 0.30, 0.40, 0.56), prog=.5)                         # the rear plate
    mod.zmaz("F", (-0.30, 0.30, 0.33, 0.49), prog=.5)                         # the front plate
    mod.zmaz("F", (-0.64, 0.64, 1.56, 1.80), farba=NIEB, prog=.035, rozszerz=3, cos=.1, glebia=.15)   # the old letters' last trace
    _zmaz_gora(mod, 1.20, 2.40, NIEB)                                          # the bonnet's garbled letters
    _zmaz_gora(mod, 0.40, 1.05, NIEB)                                          # and the cab roof's front, over its letters
    mod.zmaz("G", (-0.62, 0.62, 1.92, 2.36), farba=NIEB, prog=.07, rozszerz=2, cos=.3, glebia=.10)    # the bonnet's lip
    # the paint renewed: a fresh coat of its colour, glossy (owner 25.11 02:43: "lakier odnowić ... jak z salonu")
    nowe = _lakiery(mod, [(NIEB, (22, 60, 108), .30, 0.0), ((225, 228, 230), (236, 238, 238), .30, 0.0)], tol=16.0)
    # the stripe: its band white, the white flecks round it (and round the old letters) the blue
    dol = lambda u: np.where(u < -0.9, .85 + (u + .9) * (.05 / 1.3), .855)
    gora = lambda u: np.where(u < -0.9, .935 + (u + .9) * (.08 / 1.3), .935)
    _pas(mod, [("L", (-2.30, 2.30), dol, gora), ("P", (-2.30, 2.30), lambda u: dol(-u), lambda u: gora(-u)),
               ("T", (-0.95, 0.95), lambda u: .855 + 0 * u, lambda u: .93 + 0 * u)],
         (236, 238, 238), (22, 60, 108), wokol=.05, okna=MILICJA_OKNA, litery=[z for z in Z if z[0] in "LP"])
    for w, u in (("L", -0.42), ("P", 0.32)): mod.napis(w, "MILICJA", u, 0.71, .10)
    for w, u in (("L", 1.05), ("P", -1.15)): mod.napis(w, "MILICJA", u, 1.04, .11)
    mod.napis("F", "MILICJA", 0, 1.66, .085); mod.napis("T", "MILICJA", 0, 1.045, .075)
    for w in ("F", "T"): mod.napis(w, "MILICJA", 0, 1.915, .045)
    mod.tablica("F", "MO 1261", 0, 0.41); mod.tablica("T", "MO 1261", -0.02, 0.465)
    return dict(farby=[NIEB, (225, 228, 230)] + nowe, farby_okien=[NIEB] + nowe, napisy=["MILICJA", "MO 1261"], okna=MILICJA_OKNA)


MILICJA_OKNA = [("L", (-0.95, 0.06, 0.96, 1.44)), ("L", (0.36, 1.21, 1.10, 1.57)), ("P", (-0.04, 0.95, 0.96, 1.44)), ("P", (-1.27, -0.45, 1.10, 1.57)),
                ("F", (-0.72, 0.72, 0.98, 1.46)), ("T", (-0.62, 0.60, 1.10, 1.53))]


def _pas(mod, pasy, bialy, farba, wokol=.05, okna=(), litery=()):
    """a painted stripe made straight: [(view, (u0, u1), bottom(u), top(u))] - the faces seen there (the outer skin)
    between its edges take the stripe's colour, the white flecks within `wokol` of it (Tripo's blurred edge) and in the old
    letters' boxes the paint's; only what is paint (white or the paint's hue) - a handle, a lamp keep theirs"""
    import numpy as np
    from tekstura import WIDOKI
    B, F = np.array(bialy, np.float32) / 255, np.array(farba, np.float32) / 255
    hF = _hsv(F[None])[0, 0]; szyby = _w_oknie(mod, okna) if okna else np.zeros(len(mod.I), bool)
    nb = nf = 0
    for w, (u0, u1), dol, gora in pasy:
        f = mod.widoczne(w, (u0, u1, 0.0, 9.0), cos=.15, glebia=.03, margines=0, zewn=True) & ~szyby
        ys, xs, pts = mod.teksele(f); u, v = WIDOKI[w][1](pts)
        k = (u >= u0) & (u <= u1); ys, xs, u, v = ys[k], xs[k], u[k], v[k]
        c = mod.img[ys, xs]; h = _hsv(c)
        bial = (h[:, 1] < .25) & (h[:, 2] > .45); niebi = (np.abs(h[:, 0] - hF) < 25) & (h[:, 1] > .3) & (h[:, 2] > .12)
        lo, hi = dol(u), gora(u)
        w_pasie = (v >= lo) & (v <= hi) & (bial | niebi)
        obok = (((v < lo) & (v > lo - wokol)) | ((v > hi) & (v < hi + wokol))) & bial
        for lw, (a0, a1, b0, b1) in litery:
            if lw == w: obok |= (u >= a0) & (u <= a1) & (v >= b0) & (v <= b1) & ~((v >= lo) & (v <= hi)) & bial
        mod.img[ys[w_pasie], xs[w_pasie]] = B; mod.img[ys[obok], xs[obok]] = F
        nb += int(w_pasie.sum()); nf += int(obok.sum())
    mod.log.append("pas: %d texels białe, %d plamek zamalowanych" % (nb, nf))


def probka(mod, widok, box):
    """the paint's own colour: the median of the texels seen in a clean patch"""
    import numpy as np
    from tekstura import WIDOKI
    ys, xs, pts = mod.teksele(mod.widoczne(widok, box, zewn=True)); u, v = WIDOKI[widok][1](pts)
    m = (u >= box[0]) & (u <= box[1]) & (v >= box[2]) & (v <= box[3])
    return tuple(int(x) for x in (np.median(mod.img[ys[m], xs[m]], 0) * 255 + .5))


def tablice(mod, tekst, przod, tyl, stare=(), nowa=False):
    """the old plates erased (whatever is in their boxes), the new ones laid: przod / tyl = (u, v[, w, h])"""
    for w, b in stare: mod.zmaz(w, b, prog=.0, rozszerz=2)
    for w, t in (("F", przod), ("T", tyl)):
        if t: mod.tablica(w, tekst, t[0], t[1], *(t[2:] if len(t) > 2 else ()), nowa=nowa)


def maluch(mod):
    BEZ = probka(mod, "L", (-0.30, 0.20, 0.40, 0.60))
    _dwa(mod, [("L", (0.50, 0.64, 0.55, 0.69)), ("P", (-0.69, -0.56, 0.55, 0.69)), ("F", (0.10, 0.31, 0.51, 0.60))], BEZ)
    # the paint renewed: a fresh coat of its colour, glossy (owner 25.11 02:43: "lakier odnowić ... jak z salonu")
    # (its bumpers kept as they are: the cream coat took their light parts)
    zost, chrom = _zostaw(mod, [("F", (-0.63, 0.64, 0.37, 0.435), "chrom"), ("T", (-0.65, 0.66, 0.36, 0.45), "chrom")])
    nowe = _lakiery(mod, [(BEZ, (214, 198, 160), .32, 0.0)], okna=MALUCH_OKNA, tol=18.0, plamy=.65, kontrast=.6, zostaw=zost)
    tablice(mod, "KRA 4416", (-0.03, 0.315), (-0.06, 0.56, .36, .10),
            stare=[("F", (-0.30, 0.24, 0.26, 0.37)), ("T", (-0.26, 0.14, 0.495, 0.625))])
    mod.napis("F", "126p", 0.205, 0.555, .026, kolor=(196, 198, 200))
    # its glass clear, as the 126p's was: Tripo painted it so (the seats seen through it, the sky in the windscreen)
    return dict(farby=[BEZ] + nowe, napisy=["126p", "KRA 4416"], okna=MALUCH_OKNA, przyciemniane=False)


# the 126p's windows as Tripo painted them, measured on its gridded views (the outer edge of the rubber; the door's vent
# window parted from the drop glass by its post; the windscreen's and the back window's rounded corners)
MALUCH_OKNA = [
    ("L", (-0.40, 0.42, 0.84, 1.23), [[(-0.22, 1.21, .02), (-0.165, 1.21, 0), (-0.165, 0.862, 0), (-0.357, 0.862, .02), (-0.352, 0.93, .02)],
                                      [(-0.165, 1.21, 0), (0.395, 1.21), (0.395, 0.862), (-0.165, 0.862, 0)]]),
    ("L", (0.48, 1.10, 0.85, 1.23), [[(0.51, 1.2025, .04), (0.87, 1.2025), (1.07, 0.935, .04), (1.06, 0.8775, .04), (0.51, 0.8775, .04)]]),
    ("P", (-0.43, 0.47, 0.84, 1.23), [[(0.16, 1.2075, 0), (0.20, 1.2075, .02), (0.44, 0.90, .02), (0.44, 0.8675, .02), (0.16, 0.8675, 0)],
                                      [(-0.4025, 1.2075), (0.16, 1.2075, 0), (0.16, 0.8675, 0), (-0.4025, 0.8675)]]),
    ("P", (-1.11, -0.49, 0.84, 1.23), [[(-0.895, 1.20), (-0.517, 1.20, .04), (-0.517, 0.868, .04), (-1.07, 0.868, .04), (-1.085, 0.94, .04)]]),
    ("F", (-0.60, 0.62, 0.86, 1.30), [[(-0.50, 1.28, .05), (0.50, 1.28, .05), (0.595, 0.89, .04), (-0.565, 0.89, .04)]]),
    ("T", (-0.50, 0.49, 0.87, 1.25), [[(-0.44, 1.225, .12), (0.43, 1.225, .12), (0.465, 0.895, .05), (-0.475, 0.895, .05)]]),
]


def syrena(mod):
    """the two-tone Syrena 104: white over its shoulders (bonnet, roof, boot, the front and rear panels), mustard below -
    the line between them as Tripo drew it, smoothed (its rust made it ragged); its chrome (bumpers, grille, sills) and its
    lamps kept as they are (a white coat took them as its own)"""
    MUS = probka(mod, "L", (0.20, 0.60, 0.45, 0.60)); BIA = probka(mod, "F", (-0.40, -0.15, 0.75, 0.90))
    _dwa(mod, [("F", (-0.05, 0.05, 0.76, 0.86)), ("F", (0.05, 0.36, 0.65, 0.75)), ("T", (-0.05, 0.05, 0.72, 0.82))], BIA)
    zost, chrom = _zostaw(mod, SYRENA_ZOSTAW)
    reg = _obszary(mod, [lambda c: (lambda h: (h[:, 1] < .18) & (h[:, 2] > .40))(_hsv(c)),
                         lambda c: (lambda h: (h[:, 0] >= 22) & (h[:, 0] <= 52) & (h[:, 1] > .30) & (h[:, 2] > .20))(_hsv(c))], nie=zost)
    # the paint renewed: a fresh coat of its colour, glossy (owner 25.11 02:43: "lakier odnowić ... jak z salonu")
    nowe = _lakiery(mod, [(MUS, (214, 164, 64), .32, 0.0, (reg == 1).astype("float32")), (BIA, (236, 234, 226), .32, 0.0, (reg == 0).astype("float32"))],
                    tol=22.0, plamy=.82, kontrast=.5, odcien=(8, 55), szare=True, zostaw=zost, obszary=True)
    _chrom(mod, chrom)
    for w, b in (("L", (1.52, 1.66, 0.08, 0.33)), ("P", (-1.66, -1.52, 0.08, 0.33))): _pomaluj(mod, w, b, (22, 22, 22))   # the mud flaps' rubber
    tablice(mod, "WAB 7491", (0.0, 0.335), (-0.01, 0.63),
            stare=[("F", (-0.25, 0.25, 0.27, 0.40)), ("T", (-0.25, 0.23, 0.555, 0.70))])
    mod.napis("F", "SYRENA", 0.0, 0.70, .028, kolor=(206, 208, 210))
    return dict(farby=[MUS, BIA] + nowe, napisy=["SYRENA", "WAB 7491"],
                otwory=[("L", (-0.62, 0.10, 0.98, 1.42)), ("L", (0.08, 0.96, 0.98, 1.42)),
                        ("P", (-0.10, 0.62, 0.98, 1.42)), ("P", (-0.98, -0.06, 0.98, 1.42)),
                        ("F", (-0.66, 0.66, 1.02, 1.47)), ("T", (-0.64, 0.64, 1.02, 1.47))])


def _pz(pola):
    """the left side's boxes for the right too (view P: u mirrored)"""
    return pola + [("P", (-b[1], -b[0], b[2], b[3]), k) for w, b, k in pola if w == "L"]


# the Syrena's chrome and lamps, measured on its views: the headlamps' rings, the indicators, the grille, the bumpers
# (their ends round the corners), the sills' chrome; the tail lamps
SYRENA_ZOSTAW = _pz([
    ("F", (-0.68, -0.43, 0.57, 0.82), "chrom_o"), ("F", (0.42, 0.67, 0.57, 0.82), "chrom_o"),
    ("F", (-0.645, -0.49, 0.39, 0.54), "lampa_o"), ("F", (0.49, 0.645, 0.39, 0.54), "lampa_o"),
    ("F", (-0.46, 0.47, 0.425, 0.645), "chrom"), ("F", (-0.74, 0.74, 0.27, 0.42), "chrom"),
    ("T", (-0.63, -0.49, 0.50, 0.69), "lampa"), ("T", (0.48, 0.62, 0.50, 0.69), "lampa"), ("T", (-0.76, 0.76, 0.27, 0.47), "chrom"),
    ("L", (-2.10, -1.80, 0.27, 0.43), "chrom"), ("L", (1.78, 2.10, 0.33, 0.48), "chrom"), ("L", (-0.90, 0.80, 0.27, 0.35), "chrom"),
])


def fiat125p(mod):
    CZE = probka(mod, "L", (0.20, 0.60, 0.62, 0.80))
    _dwa(mod, [("F", (-0.06, 0.05, 0.65, 0.76)), ("F", (0.05, 0.34, 0.65, 0.75))], (14, 14, 16))
    # the paint renewed: a fresh coat of its colour, glossy (owner 25.11 02:43: "lakier odnowić ... jak z salonu")
    nowe = _lakiery(mod, [(CZE, (176, 28, 30), .28, 0.0)], tol=18.0)
    # its rear bumper chrome as the front one (the 125p of 1975: Tripo painted the bar red), its rubber overriders kept
    # (by the body's space: the bar's every face - its top, its face, its ends round the corners - in its 6.6 cm)
    c = mod.cen; z0 = c[:, 2].min()
    ys, xs, pts = mod.teksele((c[:, 2] < z0 + .16) & (c[:, 1] > .54) & (c[:, 1] < .63))
    x, y, z = pts[:, 0], pts[:, 1], pts[:, 2]
    bar = (y >= .552) & (y <= .618) & (z < z0 + .14) & ~((np.abs(x) > .44) & (np.abs(x) < .54) & (z < z0 + .06))
    ys, xs = ys[bar], xs[bar]; mod.img[ys, xs] = np.array((198, 200, 204), np.float32) / 255
    mp = mod.mapy["mr"]; hh, ww = mp.shape[:2]; yy = np.clip(ys * hh // mod.h, 0, hh - 1); xx = np.clip(xs * ww // mod.w, 0, ww - 1)
    mp[yy, xx, 1] = 56; mp[yy, xx, 2] = 115
    mod.log.append("zderzak tylny chrom: %d texels" % len(ys))
    for w, b in (("T", (-0.78, -0.42, 0.10, 0.37)), ("T", (0.42, 0.78, 0.10, 0.37))): _pomaluj(mod, w, b, (22, 22, 22), glebia=.06)  # the mud flaps' rubber
    tablice(mod, "WAR 1252", (0.0, 0.40), (-0.05, 0.81), stare=[("T", (-0.29, 0.19, 0.73, 0.89))])
    mod.napis("F", "125p", 0.19, 0.70, .03, kolor=(206, 208, 210))
    return dict(farby=[CZE] + nowe, napisy=["125p", "WAR 1252"], opony=.66,
                okna=[("F", (-0.78, 0.74, 1.02, 1.54)), ("T", (-0.74, 0.66, 1.06, 1.54))],
                otwory=[("L", (-0.80, 0.04, 1.00, 1.52)), ("L", (0.03, 0.86, 1.00, 1.52)),
                        ("P", (-0.04, 0.80, 1.00, 1.52)), ("P", (-0.86, -0.03, 1.00, 1.52))])


def polonez(mod):
    CZE = probka(mod, "L", (0.20, 0.60, 0.70, 0.85))
    _dwa(mod, [("F", (-0.12, 0.03, 0.66, 0.78)), ("F", (0.10, 0.32, 0.66, 0.78)), ("T", (-0.13, -0.02, 0.93, 1.04)),
               ("T", (0.31, 0.55, 0.80, 0.87))], CZE)
    # the paint renewed: a fresh coat of its colour, glossy (owner 25.11 02:43: "lakier odnowić ... jak z salonu")
    # every red of its body the new coat with its neighbours' light (Tripo's rust streaks along the doors), its tail
    # lamps kept (red too)
    zost, _ = _zostaw(mod, [("T", (-0.88, -0.74, 0.70, 0.94), "lampa"), ("T", (0.70, 0.84, 0.70, 0.94), "lampa"),
                            ("L", (2.05, 2.30, 0.70, 0.94), "lampa"), ("P", (-2.30, -2.05, 0.70, 0.94), "lampa")])
    nowe = _lakiery(mod, [(CZE, (168, 18, 36), .26, .10, _maska(mod))], tol=18.0, plamy=.65, kontrast=.3, odcien=(330, 20),
                    zostaw=zost, obszary=True)
    tablice(mod, "WAX 6230", (-0.02, 0.51), (-0.06, 0.83),
            stare=[("F", (-0.34, 0.29, 0.44, 0.585)), ("T", (-0.31, 0.19, 0.76, 0.895))])
    mod.napis("F", "CARO", 0.21, 0.715, .026, kolor=(206, 208, 210))
    mod.napis("T", "POLONEZ", 0.43, 0.835, .024, kolor=(206, 208, 210))
    return dict(farby=[CZE] + nowe, napisy=["CARO", "POLONEZ", "WAX 6230"], opony=.66,
                otwory=[("L", (-0.86, 0.20, 0.98, 1.52)), ("L", (0.16, 0.94, 0.98, 1.52)), ("L", (0.88, 1.55, 0.98, 1.50)),
                        ("P", (-0.20, 0.86, 0.98, 1.52)), ("P", (-0.94, -0.16, 0.98, 1.52)), ("P", (-1.55, -0.88, 0.98, 1.50)),
                        ("F", (-0.74, 0.72, 1.00, 1.55)), ("T", (-0.72, 0.72, 1.02, 1.54))])


def bosna(mod):
    """a food van of today (BOSNA BAR at the quad track): its plates of today; the Tripo brand marks and garbled lines
    off, the big BOSNA and B-A-R kept (the van's own name)"""
    NIE = probka(mod, "P", (-2.2, -1.0, 1.0, 1.2))
    _dwa(mod, [("L", (-0.82, -0.19, 2.06, 2.30)), ("L", (-0.66, -0.07, 1.97, 2.08)), ("L", (-0.30, 0.55, 1.11, 1.28)),
               ("L", (-1.22, -0.76, 0.63, 0.97)), ("L", (0.14, 1.24, 0.60, 0.78)),
               ("P", (-0.44, 0.71, 2.10, 2.21)), ("P", (-0.44, 0.39, 1.12, 1.24)), ("P", (-2.39, -1.94, 2.09, 2.34)),
               ("T", (-0.77, 0.59, 2.10, 2.23))], NIE)
    tablice(mod, "WGM 4K21", (0.0, 0.55, .52, .114), (0.0, 0.84, .52, .114), nowa=True)
    mod.odnow([NIE], tol=.16)
    mod.napis("L", "GORĄCE BOSNY", 0.12, 1.19, .075, kolor=(244, 196, 48))
    mod.napis("P", "BOSNA BAR", -1.50, 1.62, .15, kolor=(244, 196, 48))
    mod.napis("P", "GORĄCE BOSNY", -1.50, 1.38, .075, kolor=(240, 240, 236))
    return dict(farby=[NIE], napisy=["BOSNA BAR", "GORĄCE BOSNY", "WGM 4K21"], opony=.66, szyby_w_teksturze=True,
                okna=[("L", (-1.75, -0.92, 1.15, 1.98)), ("P", (0.84, 1.66, 1.20, 1.98)), ("P", (-0.50, 0.72, 1.24, 1.98)),
                      ("F", (-0.92, 0.88, 1.18, 2.02)), ("T", (-0.80, 0.73, 1.24, 2.03))])


def polonez_milicja(mod):
    """Tripo's model stands 0.3 m off its own centre line (its plates, windows: u read off the views as they are)"""
    GRA = probka(mod, "L", (1.0, 1.4, 0.60, 0.78)); BIA = probka(mod, "L", (1.0, 1.4, 0.875, 0.935))
    import numpy as np
    pas = np.zeros((40, 40, 4), np.float32); pas[..., :3] = np.array(BIA, np.float32) / 255; pas[..., 3] = 1
    for w, (a, b), (e0, e1) in (("L", (-0.74, -0.14), (-0.53, -0.22)), ("P", (0.08, 0.60), (0.15, 0.45))):
        # the garbled word and the badge under the white stripe: the panel filled in from round them, then the stripe
        # (v 0.865 - 0.955 along the whole car) laid again over the letters that ran across it
        for box in ((a, b, 0.755, 0.866), (e0, e1, 0.48, 0.77)): mod.zmaz(w, box, farba=GRA, prog=.06, rozszerz=3, cos=.1, glebia=.15, plasko=True)
        mod.naklej(w, pas, (a + b) / 2, 0.910, b - a, 0.088, "pas")
    # Tripo's garble on the bonnet (a square of scribble before the windscreen, a smear beside it)
    for box in ((-0.08, 0.26, 1.36, 1.66), (0.27, 0.44, 1.34, 1.66)): mod.zmaz("G", box, farba=GRA, prog=.07, rozszerz=3, cos=.3, glebia=.10)
    # the paint renewed: a fresh coat of its colour, glossy (owner 25.11 02:43: "lakier odnowić ... jak z salonu")
    nowe = _lakiery(mod, [(GRA, (22, 60, 108), .30, 0.0), (BIA, (236, 238, 238), .30, 0.0)], tol=16.0)
    # the stripe straight along the waist (measured on its side: it follows the line of the body)
    dol = lambda u: np.interp(u, [-2.05, -1.55, -0.95, -0.75, 0.65, 1.25, 1.65, 2.10], [.82, .845, .865, .87, .87, .865, .855, .84])
    gora = lambda u: np.interp(u, [-2.05, -1.55, -0.95, -0.75, 0.65, 1.05, 1.45, 1.85, 2.10], [.875, .92, .945, .95, .95, .94, .93, .91, .89])
    _pas(mod, [("L", (-2.20, 2.20), dol, gora), ("P", (-2.20, 2.20), lambda u: dol(-u), lambda u: gora(-u))],
         (236, 238, 238), (22, 60, 108), wokol=.05, okna=POLONEZ_MILICJA_OKNA,
         litery=[("L", (-0.74, -0.14, 0.60, 0.866)), ("P", (0.08, 0.60, 0.60, 0.866))])
    tablice(mod, "MO 7846", (0.28, 0.41), (-0.335, 0.735, .40, .10),
            stare=[("F", (0.08, 0.48, 0.31, 0.52)), ("T", (-0.55, -0.12, 0.675, 0.80)), ("T", (-0.53, -0.16, 0.39, 0.585))])
    mod.napis("L", "MILICJA", -0.45, 0.70, .085); mod.napis("P", "MILICJA", 0.33, 0.70, .085)
    return dict(farby=[GRA, BIA] + nowe, farby_okien=[GRA] + nowe, napisy=["MILICJA", "MO 7846"], opony=.66,
                okna=POLONEZ_MILICJA_OKNA)


POLONEZ_MILICJA_OKNA = [("L", (-0.52, 0.10, 0.97, 1.49)), ("L", (0.12, 0.78, 0.97, 1.49)), ("L", (0.77, 1.40, 0.97, 1.47)),
                        ("P", (-0.16, 0.52, 0.97, 1.49)), ("P", (-0.88, -0.20, 0.97, 1.49)), ("P", (-1.52, -0.88, 0.97, 1.47)),
                        ("F", (-0.48, 0.93, 0.98, 1.49)), ("T", (-0.97, 0.30, 1.10, 1.44))]


def nysa_milicja(mod):
    GRA = probka(mod, "L", (1.40, 1.80, 0.75, 1.00)); BIA = probka(mod, "L", (0.80, 1.20, 1.21, 1.28))
    _dwa(mod, [("L", (-0.27, 0.76, 0.86, 1.19)), ("L", (-1.09, -0.76, 0.79, 1.17)), ("P", (0.32, 0.41, 0.95, 1.04)),
               ("T", (-0.47, -0.28, 0.65, 0.77))], GRA)
    # the paint renewed: a fresh coat of its colour, glossy (owner 25.11 02:43: "lakier odnowić ... jak z salonu")
    nowe = _lakiery(mod, [(GRA, (22, 60, 108), .30, 0.0), (BIA, (236, 238, 238), .30, 0.0)], tol=16.0)
    # the stripe straight round the van (measured: 12 cm on the sides, 11 cm over the cab's front)
    gora = lambda u: np.where(u < -1.30, 1.29, 1.31)
    _pas(mod, [("L", (-2.20, 2.20), lambda u: 1.185 + 0 * u, gora), ("P", (-2.20, 2.20), lambda u: 1.185 + 0 * u, lambda u: gora(-u)),
               ("F", (-1.05, 1.00), lambda u: 1.18 + 0 * u, lambda u: 1.29 + 0 * u)],
         (236, 238, 238), (22, 60, 108), wokol=.05, okna=NYSA_MILICJA_OKNA,
         litery=[("L", (-0.27, 0.76, 0.86, 1.19)), ("L", (-1.09, -0.76, 0.79, 1.17))])
    tablice(mod, "MO 4052", (-0.03, 0.49), (0.0, 0.62), stare=[("F", (-0.30, 0.24, 0.43, 0.55))])
    mod.napis("L", "MILICJA", 0.24, 1.04, .13); mod.napis("P", "MILICJA", -0.24, 1.04, .13)
    for w in ("F", "T"): mod.napis(w, "MILICJA", 0.0, 2.33, .05, kolor=GRA)
    return dict(okna_prostokatne=True, farby=[GRA, BIA] + nowe, farby_okien=[GRA] + nowe, napisy=["MILICJA", "MO 4052"], opony=.66,
                okna=NYSA_MILICJA_OKNA)


NYSA_MILICJA_OKNA = [("L", (-1.26, -0.58, 1.34, 1.90)), ("L", (-0.16, 0.60, 1.34, 1.90)), ("L", (0.64, 1.34, 1.34, 1.90)),
                     ("L", (1.35, 1.97, 1.34, 1.90)),
                     ("P", (-2.04, -1.43, 1.36, 1.90)), ("P", (-1.41, -0.71, 1.36, 1.90)), ("P", (-0.66, 0.07, 1.36, 1.90)),
                     ("P", (0.54, 1.20, 1.36, 1.90)),
                     ("F", (-0.95, 0.90, 1.32, 2.02)), ("T", (-0.78, 0.73, 1.36, 1.88))]


def fiat125p_tuning(mod):
    """the owner's black-green 125p racer (roll cage, wire wheels): its plates, the paint evened, glass in the openings"""
    s = _Skala(mod, 4.23 / 4.0)                                  # read off its first 4 m views
    LAK = probka(mod, "L", s.b((0.20, 0.60, 0.50, 0.80)))
    # its near-black paint (owner 25.11 02:18: the colours ugly) a deep racing green, glossy
    farby = _lakiery(mod, [(LAK, (14, 70, 40), .20, .12)], tol=22.0, plamy=.5, kontrast=.6)
    tablice(s, "WAP 1125", (0.0, 0.37), (0.0, 0.60))
    return dict(farby=farby + [LAK], napisy=["WAP 1125"], opony=.72,
                otwory=s.okna([("L", (-0.60, 0.08, 0.90, 1.38)), ("L", (0.05, 0.84, 0.90, 1.38)),
                               ("P", (-0.08, 0.60, 0.90, 1.38)), ("P", (-0.84, -0.05, 0.90, 1.38)),
                               ("F", (-0.66, 0.66, 0.94, 1.42)), ("T", (-0.64, 0.62, 1.00, 1.34))]))


def polonez_tuning(mod):
    s = _Skala(mod, 4.272 / 4.0)                                 # read off its first 4 m views
    LAK = probka(mod, "L", s.b((0.20, 0.60, 0.40, 0.65)))
    _dwa(s, [("T", (-0.24, 0.24, 0.585, 0.65))], LAK)
    s.zmaz("T", (-0.28, 0.28, 0.43, 0.585), farba=LAK, prog=.0, rozszerz=2, plasko=True)     # (the old plate's green recess: the paint)
    # its near-black paint (owner 25.11 02:18: Tripo's colours bad) a deep graphite metallic, glossy - a light coat over
    # Tripo's black took its lamps and trim with it (all of them as dark as the paint), a graphite keeps them apart
    farby = _lakiery(mod, [(LAK, (50, 53, 58), .18, .15)], tol=14.0, kontrast=.5)
    tablice(s, "WAT 1987", (0.0, 0.30), (0.0, 0.375), stare=[("T", (-0.21, 0.21, 0.315, 0.435))])
    return dict(farby=farby + [LAK], napisy=["WAT 1987"], opony=.72,
                otwory=s.okna([("L", (-0.62, 0.14, 0.76, 1.20)), ("L", (0.14, 0.90, 0.76, 1.20)),
                               ("P", (-1.00, -0.30, 0.76, 1.22)), ("P", (-0.22, 0.50, 0.76, 1.22)),
                               ("F", (-0.66, 0.66, 0.86, 1.20))]))     # (its back window is closed: Tripo painted it)


def star_zielony(mod):
    """Star 200: the cab's door windows open (panes built), its small rear side windows, windscreen and back window painted"""
    ZIE = probka(mod, "L", (-2.60, -2.00, 1.20, 1.50))
    _dwa(mod, [("F", (-0.38, 0.36, 1.52, 1.62)), ("F", (-0.10, 0.08, 1.65, 1.78)), ("F", (-0.12, 0.02, 1.33, 1.43))], ZIE)
    tablice(mod, "WAB 412T", (0.0, 0.80), (0.0, 0.86))
    # no restoration pass on the trucks: their wooden beds, the chassis and the tyres lie by the paint in the atlas and
    # have its warm hues - the cleaning would paint them green
    mod.napis("F", "STAR", 0.0, 1.57, .06, kolor=(206, 208, 210))
    return dict(okna_prostokatne=True, farby=[ZIE], napisy=["STAR", "WAB 412T"], opony=.62,
                okna=[("L", (-1.75, -1.34, 1.92, 2.50)), ("P", (1.27, 1.66, 1.92, 2.50)), ("F", (-1.00, 0.93, 1.88, 2.60)),
                      ("T", (-0.90, 0.83, 2.05, 2.48))],
                otwory=[("L", (-2.45, -1.86, 1.92, 2.50)), ("P", (1.80, 2.38, 1.94, 2.50))])


def star_pomaranczowy(mod):
    """Star 28: every cab window open - panes built in them (the back one is seen only past the bed: left open)"""
    POM = probka(mod, "L", (-2.90, -2.40, 1.20, 1.60))
    tablice(mod, "KRB 288S", (0.0, 0.77), (0.0, 0.86))
    mod.napis("F", "STAR", 0.0, 1.66, .05, kolor=(60, 62, 66))
    return dict(farby=[POM], napisy=["STAR", "KRB 288S"], opony=.62,
                otwory=[("L", (-2.68, -2.10, 1.82, 2.44)), ("L", (-1.99, -1.70, 1.82, 2.44)),
                        ("P", (1.98, 2.50, 1.80, 2.43)), ("P", (1.60, 1.90, 1.80, 2.43)),
                        ("F", (-1.02, 0.99, 1.80, 2.48))])            # (the cab's back window: behind the bed's racks)


def _bez_farby(mod, boxes):
    """small lettering on a tank or a side cover: the panel's own colour is the box's median"""
    for prog, roz in ((.10, 4), (.08, 3)):
        for w, b in boxes: mod.zmaz(w, b, prog=prog, rozszerz=roz)


# the motorcycles: no restoration pass (chrome, rubber and paint lie side by side in their atlases); the tanks' Tripo
# scripts and badges off, the names laid in plain letters; their plates where Tripo put one (a moped has none)
def komar(mod):
    _bez_farby(mod, [("L", (-0.30, 0.00, 0.58, 0.72)), ("P", (0.03, 0.33, 0.58, 0.72))])
    mod.napis("L", "KOMAR", -0.15, 0.65, .032, kolor=(240, 240, 236)); mod.napis("P", "KOMAR", 0.17, 0.65, .032, kolor=(240, 240, 236))
    return dict(napisy=["KOMAR"], opony=.80)


def jawa(mod):
    _bez_farby(mod, [("L", (-0.34, -0.06, 0.72, 0.87)), ("L", (0.16, 0.37, 0.59, 0.68)),
                     ("P", (0.00, 0.30, 0.72, 0.87)), ("P", (-0.44, -0.21, 0.59, 0.68))])
    for w, a, b in (("L", -0.20, 0.265), ("P", 0.15, -0.33)):
        mod.napis(w, "JAWA", a, 0.80, .034, kolor=(240, 240, 236)); mod.napis(w, "350", b, 0.635, .028, kolor=(240, 240, 236))
    tablice(mod, "WAJ 3501", None, (-0.04, 0.525, .20, .10), stare=[("T", (-0.14, 0.07, 0.465, 0.585))])
    return dict(napisy=["JAWA", "350", "WAJ 3501"], opony=.80)


def romet(mod):
    _bez_farby(mod, [("L", (-0.33, -0.07, 0.69, 0.79)), ("P", (0.06, 0.31, 0.69, 0.79))])
    mod.napis("L", "ROMET", -0.20, 0.74, .026, kolor=(240, 240, 236)); mod.napis("P", "ROMET", 0.185, 0.74, .026, kolor=(240, 240, 236))
    tablice(mod, "KRA 1983", None, (-0.06, 0.54, .22, .11), stare=[("T", (-0.22, 0.09, 0.455, 0.625))])
    return dict(napisy=["ROMET", "KRA 1983"], opony=.80)


def junak(mod):
    """Junak M10: the plate on its front mudguard (as the time had them) and the rear one; the tank's name in gold"""
    s = _Skala(mod, 2.172 / 4.0)                                 # read off its first 4 m views
    _bez_farby(s, [("L", (-0.64, -0.35, 1.43, 1.58)), ("P", (0.12, 0.40, 1.43, 1.58)), ("T", (-0.15, 0.12, 1.52, 1.66))])
    for w in ("L", "P"): s.zmaz(w, (-1.77, -1.31, 1.32, 1.47) if w == "L" else (1.25, 1.68, 1.32, 1.47), prog=0, rozszerz=2)
    s.tablica("L", "WAJ 1958", -1.545, 1.39, .22, .06); s.tablica("P", "WAJ 1958", 1.465, 1.39, .22, .06)
    tablice(s, "WAJ 1958", None, (-0.04, 1.045, .19, .10), stare=[("T", (-0.31, 0.23, 0.91, 1.18))])
    s.napis("L", "JUNAK", -0.495, 1.50, .06, kolor=(226, 200, 140)); s.napis("P", "JUNAK", 0.26, 1.50, .06, kolor=(226, 200, 140))
    return dict(napisy=["JUNAK", "WAJ 1958"], opony=.80)


# ---------------------------------------------------------------------------------------------------------- the second set
# (owner 25.11 02:14-02:17: his pictures of how each should look, "lakiery mają być ładne, wszystko ma być piękne")

def _w_oknie(mod, okna):
    """the faces inside the measured windows' outlines (seen in their views, 2 cm round them for the rubber)"""
    import numpy as np
    from przygotuj_klasyki import zaokraglij
    from tekstura import WIDOKI
    out = np.zeros(len(mod.I), bool)
    for o in okna:
        if len(o) < 3: continue
        w, box = o[0], o[1]; op = o[3] if len(o) > 3 else {}
        vis = mod.widoczne(w, (box[0] - .05, box[1] + .05, box[2] - .05, box[3] + .05), cos=.02, glebia=.12, margines=0,
                           do=op.get("do"), gesto=op.get("gesto", False))
        u, v = WIDOKI[w][1](mod.cen)
        for pol in o[2]:
            h = zaokraglij(pol); a = h; e = np.roll(h, -1, 0) - h
            znak = np.sign(np.sum(a[:, 0] * np.roll(a[:, 1], -1) - np.roll(a[:, 0], -1) * a[:, 1])) or 1.0
            nrm = np.stack([-e[:, 1], e[:, 0]], -1) / np.maximum(np.linalg.norm(e, axis=1), 1e-9)[:, None] * znak
            dd = (u[:, None] - a[None, :, 0]) * nrm[None, :, 0] + (v[:, None] - a[None, :, 1]) * nrm[None, :, 1]
            out |= vis & (dd.min(1) > -.02)
    return out


def _sciany(mod, okna=(), nad=None, pod=None):
    """the body's faces: off the windows' glass and off the wheels, between the heights given"""
    import numpy as np
    f = ~_w_oknie(mod, okna); c = mod.cen
    for k in getattr(mod, "kola", []):
        # (the tyre itself: the arch's lip and the door's foot beside it are the body's)
        r = np.hypot(c[:, 2] - k["z"], c[:, 1] - k["y"]); f &= ~((r < 1.02 * k["R"]) & (np.abs(c[:, 0]) > k["x_in"] - .03))
    if nad is not None: f &= c[:, 1] >= nad
    if pod is not None: f &= c[:, 1] < pod
    return f


def _maska(mod, okna=(), nad=None, pod=None):
    """the body's texels to paint: its faces off the windows' glass and off the wheels, between the heights given"""
    import numpy as np
    import cv2
    ys, xs, _ = mod.teksele(_sciany(mod, okna, nad, pod)); m = np.zeros((mod.h, mod.w), np.uint8); m[ys, xs] = 1
    return (cv2.dilate(m, np.ones((3, 3), np.uint8)) > 0).astype(np.float32)


def _zostaw(mod, pola):
    """the trim and the lamps as Tripo made them - the paint never on them (a white coat took a chrome bumper, a grey
    lamp, as its own): [(view, box, kind)] - kind "chrom" / "lampa" the box, "chrom_o" / "lampa_o" the ellipse in it (a
    round lamp with its chrome ring); the texels of the faces seen there. Returns the mask and the chrome's texels"""
    import numpy as np
    from tekstura import WIDOKI
    m = np.zeros((mod.h, mod.w), np.float32); chrom = np.zeros((mod.h, mod.w), bool)
    for w, (u0, u1, v0, v1), rodzaj in pola:
        ys, xs, pts = mod.teksele(mod.widoczne(w, (u0, u1, v0, v1), cos=.02, glebia=.08, margines=0)); u, v = WIDOKI[w][1](pts)
        if rodzaj.endswith("_o"): k = ((u - (u0 + u1) / 2) / ((u1 - u0) / 2)) ** 2 + ((v - (v0 + v1) / 2) / ((v1 - v0) / 2)) ** 2 <= 1
        else: k = (u >= u0) & (u <= u1) & (v >= v0) & (v <= v1)
        # (chrome is grey: the coloured paint round it in the box, and its rust, are the body's)
        if rodzaj.startswith("chrom"): h = _hsv(mod.img[ys, xs]); k &= ~((h[:, 1] > .30) & (h[:, 2] > .12))
        m[ys[k], xs[k]] = 1
        if rodzaj.startswith("chrom"): chrom[ys[k], xs[k]] = True
    mod.log.append("zostawione (chrom, lampy): %d texels" % int(m.sum()))
    return m, chrom


def _pomaluj(mod, widok, box, kolor, szorstkosc=.75, metal=0.0, glebia=.30):
    """a part made one colour (a rubber mud flap Tripo painted with the body, a bumper chrome): the faces seen in the
    view's box"""
    import numpy as np
    from tekstura import WIDOKI
    ys, xs, pts = mod.teksele(mod.widoczne(widok, box, cos=.02, glebia=glebia, margines=0)); u, v = WIDOKI[widok][1](pts)
    k = (u >= box[0]) & (u <= box[1]) & (v >= box[2]) & (v <= box[3]); ys, xs = ys[k], xs[k]
    mod.img[ys, xs] = np.array(kolor, np.float32) / 255
    if "mr" in mod.mapy:
        mp = mod.mapy["mr"]; hh, ww = mp.shape[:2]; yy = np.clip(ys * hh // mod.h, 0, hh - 1); xx = np.clip(xs * ww // mod.w, 0, ww - 1)
        mp[yy, xx, 1] = int(szorstkosc * 255); mp[yy, xx, 2] = int(metal * 255)
    if "normal" in mod.mapy:                                    # (Tripo's relief of what it painted there off)
        mp = mod.mapy["normal"]; hh, ww = mp.shape[:2]
        mp[np.clip(ys * hh // mod.h, 0, hh - 1), np.clip(xs * ww // mod.w, 0, ww - 1)] = (128, 128, 255)
    mod.log.append("pomalowane %s %s: %d texels" % (widok, box, len(ys)))


def _chrom(mod, chrom):
    """the chrome kept made chrome: its light, neutral texels a touch brighter, glossy, half metal (the city's shade
    turns a full metal black)"""
    import numpy as np
    c = mod.img; mx = c.max(2); mn = c.min(2)
    jas = chrom & ((mx - mn) / np.maximum(mx, 1e-6) < .2) & (mx > .30)
    mod.img[jas] = np.clip(c[jas] * 1.08 + .04, 0, 1)
    if "mr" in mod.mapy:
        mp = mod.mapy["mr"]; hh, ww = mp.shape[:2]; ys, xs = np.nonzero(jas)
        yy = np.clip(ys * hh // mod.h, 0, hh - 1); xx = np.clip(xs * ww // mod.w, 0, ww - 1)
        mp[yy, xx, 1] = 56; mp[yy, xx, 2] = 110
    mod.log.append("chrom: %d texels" % int(jas.sum()))


def _obszary(mod, klasy, nie=None, krok=.02, r=2.5):
    """a two-tone body's regions as Tripo painted them, its ragged edges smoothed: each texel of the body classed by its
    colour (klasy: functions of the colours (n, 3) giving bool), the votes summed on a 2 cm grid of the body's space and
    smoothed over r cells (a Gaussian, 5 cm), each texel's region the class most voted round it (-1: no votes); nie: the texels
    left out of the votes (the trim kept)"""
    import numpy as np
    from scipy import ndimage
    ys, xs, pts = mod.teksele(_sciany(mod))
    if nie is not None: k = nie[ys, xs] <= 0; ys, xs, pts = ys[k], xs[k], pts[k]
    c = mod.img[ys, xs]; lo = pts.min(0) - 3 * krok; g = ((pts - lo) / krok).astype(int); ksz = tuple(g.max(0) + 4)
    # (the votes smoothed by a Gaussian of r cells and read between the cells: the line between the colours a smooth
    # curve, not the grid's 2 cm steps)
    S = []
    for kl in klasy:
        a = np.zeros(ksz, np.float32); s_ = kl(c); np.add.at(a, tuple(g[s_].T), 1)
        S.append(ndimage.gaussian_filter(a, r, mode="constant"))
    q = ((pts - lo) / krok - .5).T
    V = np.stack([ndimage.map_coordinates(x, q, order=1, mode="nearest") for x in S])
    reg = np.full((mod.h, mod.w), -1, np.int8); ok = V.max(0) > 0
    reg[ys[ok], xs[ok]] = V[:, ok].argmax(0)
    mod.log.append("obszary: %s texels" % [int((reg == i).sum()) for i in range(len(klasy))])
    return reg


def _hsv(c):
    import numpy as np, cv2
    return cv2.cvtColor(np.clip(c, 0, 1).reshape(-1, 1, 3).astype(np.float32), cv2.COLOR_RGB2HSV).reshape(-1, 3)


def _zmaz_gora(mod, z0, z1, farba, x=9.0):
    """Tripo's garbled light letters on a bonnet or a roof (faces looking up, z0 < z < z1, |x| < x): the paint's colour"""
    import numpy as np
    c = mod.cen; f = (mod.n[:, 1] > .6) & (c[:, 2] > z0) & (c[:, 2] < z1) & (np.abs(c[:, 0]) < x)
    ys, xs, _ = mod.teksele(f); a = mod.img[ys, xs]; mx = a.max(1); mn = a.min(1)
    jas = (mx > .45) & ((mx - mn) / np.maximum(mx, 1e-6) < .35)
    mod.img[ys[jas], xs[jas]] = np.array(farba, np.float32) / 255
    # (and their relief: Tripo's normal map keeps the letters embossed - a ghost of them in the light)
    if "normal" in mod.mapy and jas.any():
        import cv2
        mp = mod.mapy["normal"]; hh, ww = mp.shape[:2]; mk = np.zeros((hh, ww), np.uint8)
        mk[np.clip(ys[jas] * hh // mod.h, 0, hh - 1), np.clip(xs[jas] * ww // mod.w, 0, ww - 1)] = 1
        mk = cv2.dilate(mk, np.ones((5, 5), np.uint8)) > 0; mp[mk] = (128, 128, 255)
    mod.log.append("zmaz z góry z %.2f-%.2f: %d texels" % (z0, z1, int(jas.sum())))


def _lakiery(mod, pary, okna=(), zostaw=None, **kw):
    """each paint a clean new coat: [(sampled paint, its new colour, roughness, metal[, texel mask]), ...] - the dirt off
    first; never on the glass Tripo painted (the measured windows) nor on the wheels, nor on the trim kept (zostaw: the
    mask of _zostaw). With obszary=True each paint's mask is its region: every texel there of any of the paints takes it
    (Tripo's ragged edge between two colours), the dirt cleaned in each region by its own paint"""
    import numpy as np
    baza = _maska(mod, okna)
    # its headlamps (measured for the night) never under the paint: a light coat took a lamp's grey glass as its own
    lampy = REFLEKTORY.get(getattr(mod, "nazwa", ""), [])
    if lampy:
        zl, _ = _zostaw(mod, [("F", (l[0] - 1.05 * l[2], l[0] + 1.05 * l[2], l[1] - 1.05 * l[2], l[1] + 1.05 * l[2]), "lampa_o") if len(l) == 3 else
                              ("F", (l[0] - .525 * l[2], l[0] + .525 * l[2], l[1] - .525 * l[3], l[1] + .525 * l[3]), "lampa") for l in lampy])
        zostaw = zl if zostaw is None else np.maximum(zostaw, zl)
    if zostaw is not None: baza = baza * (zostaw <= 0)
    if kw.get("obszary"):
        for p in pary: mod.odnow([p[0]], tol=.16, maska_ciala=(baza * (p[4] if len(p) > 4 else 1.0)) > 0)
    else:
        mod.odnow([p[0] for p in pary], tol=.16, maska_ciala=baza > 0)
    mod.lakiery([(p[0], p[1], p[2], p[3], baza * (p[4] if len(p) > 4 else 1.0)) for p in pary], **kw)
    _bez_metalu(mod, baza)
    return [p[1] for p in pary]


def _bez_metalu(mod, baza):
    """the body not metal where Tripo's map says so: the city's shader lights a metal only by the sun and a faint sky
    (no ambient on it), so a body Tripo marked metal turns black in the shade - its paint is a clear coat over colour
    (metal at most 0.15), chrome (light and grey) at most 0.6"""
    import numpy as np
    import cv2
    if "mr" not in mod.mapy: return
    mp = mod.mapy["mr"]; h, w = mp.shape[:2]
    m = cv2.resize(baza, (w, h), interpolation=cv2.INTER_NEAREST) > 0
    a = cv2.resize(mod.img, (w, h), interpolation=cv2.INTER_AREA); mx = a.max(2); mn = a.min(2)
    chrom = ((mx - mn) / np.maximum(mx, 1e-6) < .12) & (mx > .55)
    lim = np.where(chrom, 153, 38).astype(np.uint8)
    b = mp[..., 2]; nb = np.where(m, np.minimum(b, lim), b); n = int((nb != b).sum()); mp[..., 2] = nb
    mod.log.append("metal z nadwozia: %d texels" % n)


def trabant(mod):
    """the stock Trabant 601 (the owner's "Kto chociaż raz jechał Trabantem?"): cream body, white roof, clear glass"""
    BEZ = probka(mod, "L", (-0.30, 0.20, 0.60, 0.85)); DACH = probka(mod, "G", (-0.30, 0.30, -0.30, 0.30))
    farby = _lakiery(mod, [(BEZ, (198, 184, 152), .34, 0.0, _maska(mod, pod=1.452)), (DACH, (238, 236, 228), .34, 0.0, _maska(mod, nad=1.452))],
                     okna=TRABANT_OKNA, tol=24.0)
    tablice(mod, "KTA 6015", (-0.01, 0.455), (-0.01, 0.672),
            stare=[("F", (-0.27, 0.25, 0.36, 0.52)), ("T", (-0.26, 0.24, 0.60, 0.74))])
    return dict(farby=farby, napisy=["KTA 6015"], okna=TRABANT_OKNA, przyciemniane=False, opony=.66)


def trabant_tuning_braz(mod):
    """the owner's brown Trabant on wire wheels (his picture "Tuning show"): chocolate brown metallic, black roof, the
    black plate of the time (the owner 03:02: every old car its black plate with white letters); Tripo left its windows
    open (panes built), its pillar stickers and garbled badge off"""
    LAK = probka(mod, "L", (-0.40, 0.20, 0.55, 0.80)); DACH = probka(mod, "G", (-0.30, 0.30, -0.40, 0.20))
    _dwa(mod, [("L", (0.68, 0.88, 1.02, 1.22)), ("P", (-0.88, -0.68, 1.02, 1.22)), ("T", (-0.62, -0.50, 0.98, 1.18)),
               ("T", (0.50, 0.62, 0.98, 1.18)), ("T", (0.30, 0.56, 0.47, 0.62))], LAK)
    farby = _lakiery(mod, [(LAK, (88, 52, 36), .22, .15, _maska(mod, pod=1.25)), (DACH, (24, 24, 26), .28, .05, _maska(mod, nad=1.25))],
                     tol=24.0)
    # the front plate on the apron under the bumper (as on the 601), the rear one where Tripo's yellow one was
    tablice(mod, "KRA 7601", (-0.01, 0.185), (-0.065, 0.515), stare=[("T", (-0.37, 0.29, 0.42, 0.61))])
    # (Tripo's yellow plate sat in a recess: its sides and its back too, deeper and aslant)
    mod.zmaz("T", (-0.40, 0.34, 0.40, 0.63), farba=LAK, prog=.0, rozszerz=3, cos=.0, glebia=.30, plasko=True)
    mod.napis("T", "TRABANT", 0.43, 0.575, .026, kolor=(206, 208, 210))
    for b in ((-0.62, -0.50, 1.01, 1.21), (0.49, 0.61, 1.01, 1.21)): _pomaluj(mod, "T", b, (34, 35, 38), szorstkosc=.5, glebia=.05)   # the C-pillars' vents
    return dict(farby=farby, napisy=["TRABANT", "KRA 7601"],
                # (its back window Tripo's crumpled sheet over a hole: all of it in the measured outline dark glass)
                okna=[("T", (-0.60, 0.60, 0.88, 1.26), [[(-0.47, 1.215, .06), (0.46, 1.215, .06), (0.50, 0.915, .04), (-0.51, 0.915, .04)]],
                       {"cale": True, "gesto": True})],
                otwory=[("L", (-0.52, 0.42, 0.92, 1.26)), ("L", (0.38, 1.02, 0.92, 1.26)), ("P", (-0.25, 0.60, 0.92, 1.26)),
                        ("P", (-1.02, -0.38, 0.92, 1.26)), ("F", (-0.60, 0.60, 0.88, 1.26)),
                        # (its back window a show car's tinted glass: the cage and Tripo's grey garble behind it)
                        ("T", (-0.60, 0.60, 0.92, 1.26), {"ciemna": True})])


def trabant_tuning_czarny(mod):
    """the owner's dark Trabant with the roll cage (his picture from the front): deep brown metallic, the black plates of
    the time, the open windows given panes, the painted rear side glass and pillar stickers made
    clean"""
    LAK = probka(mod, "L", (-0.40, 0.20, 0.50, 0.80))
    _dwa(mod, [("L", (0.70, 0.95, 1.05, 1.40)), ("P", (-0.98, -0.70, 1.05, 1.40)), ("T", (-0.70, -0.46, 1.05, 1.40)),
               ("T", (0.46, 0.70, 1.05, 1.40)), ("T", (0.30, 0.50, 0.54, 0.62))], LAK)
    farby = _lakiery(mod, [(LAK, (66, 40, 28), .20, .15)], okna=TRABANT_CZARNY_OKNA, tol=24.0)
    tablice(mod, "RZA 6012", (0.0, 0.354), (-0.005, 0.553), stare=[("F", (-0.28, 0.28, 0.29, 0.42)), ("T", (-0.28, 0.26, 0.48, 0.63))])
    mod.napis("T", "TRABANT", 0.40, 0.58, .024, kolor=(206, 208, 210))
    for b in ((-0.58, -0.46, 1.10, 1.32), (0.46, 0.59, 1.10, 1.32)): _pomaluj(mod, "T", b, (34, 35, 38), szorstkosc=.5, glebia=.05)   # the C-pillars' vents
    return dict(farby=farby, napisy=["TRABANT", "RZA 6012"],
                okna=TRABANT_CZARNY_OKNA + [("T", (-0.58, 0.58, 0.95, 1.36), [[(-0.45, 1.295, .06), (0.47, 1.295, .06), (0.51, 0.98, .04), (-0.49, 0.98, .04)]],
                                             {"cale": True, "gesto": True})],
                otwory=[("L", (-0.50, 0.24, 0.94, 1.37)), ("P", (-0.24, 0.50, 0.94, 1.37)),
                        ("F", (-0.60, 0.60, 1.00, 1.40)), ("T", (-0.58, 0.58, 1.00, 1.40), {"ciemna": True})])


TRABANT_CZARNY_OKNA = [
    ("L", (0.24, 0.86, 0.92, 1.34), [[(0.29, 1.30, .03), (0.72, 1.30, .03), (0.80, 1.00, .03), (0.79, 0.975, .02), (0.29, 0.975, .03)]]),
    ("P", (-0.84, -0.22, 0.92, 1.34), [[(-0.70, 1.30, .03), (-0.27, 1.30, .03), (-0.27, 0.96, .03), (-0.79, 0.96, .03)]]),
]


def syrena_niebieska(mod):
    """the owner's blue Syrena: a deep glossy blue, the plates of the time, its badge's garble off (the name in its
    place), panes built in its open windows"""
    NIE = probka(mod, "L", (-0.40, 0.20, 0.55, 0.80))
    _dwa(mod, [("F", (-0.09, 0.07, 0.66, 0.79))], NIE)
    farby = _lakiery(mod, [(NIE, (22, 68, 132), .30, 0.0)], tol=22.0)
    tablice(mod, "KTB 1057", (-0.025, 0.345), (-0.025, 0.58), stare=[("F", (-0.31, 0.27, 0.27, 0.42)), ("T", (-0.25, 0.20, 0.50, 0.70))])
    mod.napis("F", "SYRENA", -0.01, 0.72, .028, kolor=(206, 208, 210))
    return dict(farby=farby, napisy=["SYRENA", "KTB 1057"], opony=.66,
                otwory=[("L", (-0.72, 0.26, 0.98, 1.42)), ("L", (0.27, 1.02, 0.98, 1.42)), ("P", (-0.30, 0.62, 0.98, 1.42)),
                        ("P", (-1.08, -0.28, 0.98, 1.42)), ("F", (-0.62, 0.62, 1.00, 1.46)), ("T", (-0.62, 0.62, 1.00, 1.46))])


def fiat125p_granat(mod):
    """the owner's lowered blue 125p on polished wheels (his picture "klasyczny"): blue metallic, the plates of the
    time, the bonnet's garbled decal off, panes in its open windows"""
    LAK = probka(mod, "L", (-0.40, 0.20, 0.50, 0.75))
    _dwa(mod, [("F", (-0.28, 0.28, 0.66, 0.86)), ("G", (-0.50, 0.50, 1.20, 2.10))], LAK)
    farby = _lakiery(mod, [(LAK, (28, 72, 128), .22, .15)], tol=22.0, plamy=.65, kontrast=.5)
    tablice(mod, "WRA 6125", (-0.005, 0.175), (-0.03, 0.58), stare=[("F", (-0.30, 0.30, 0.10, 0.25)), ("T", (-0.26, 0.20, 0.52, 0.66))])
    return dict(farby=farby, napisy=["WRA 6125"], opony=.70,
                # its back window Tripo's white garble over a closed sheet: dark glass in its measured outline
                okna=[("T", (-0.68, 0.68, 0.86, 1.27), [[(-0.56, 1.225, .06), (0.57, 1.225, .06), (0.60, 0.905, .04), (-0.60, 0.905, .04)]],
                       {"cale": True, "gesto": True})],
                otwory=[("L", (-0.82, 0.10, 0.82, 1.26)), ("L", (0.07, 1.02, 0.82, 1.26)), ("P", (-0.10, 0.82, 0.82, 1.26)),
                        ("P", (-1.02, -0.07, 0.82, 1.26)), ("F", (-0.65, 0.65, 0.90, 1.30))])


def polonez_caro_zielony(mod):
    """the owner's green Caro Plus (his picture of one by a palace): a clean glossy green, the black plate of the time,
    Tripo's garbled CARO off its rear pillar (FSO POLONEZ on the tailgate instead), panes in its open windows"""
    ZIE = probka(mod, "L", (-0.40, 0.20, 0.50, 0.75))
    _dwa(mod, [("L", (1.30, 1.78, 0.92, 1.12))], ZIE)
    farby = _lakiery(mod, [(ZIE, (16, 118, 58), .28, 0.0)], tol=22.0)
    tablice(mod, "CTR 4712", (-0.01, 0.495), (-0.005, 0.752), stare=[("F", (-0.30, 0.28, 0.43, 0.56)), ("T", (-0.24, 0.22, 0.69, 0.82))])
    return dict(farby=farby, napisy=["CTR 4712"], opony=.70,
                # its tailgate's glass Tripo painted (white glare): tinted glass with its rubber, measured (raked and coarse:
                # its faces' corners mark the glass' surface, the boot under it not seen through)
                okna=[("T", (-0.66, 0.62, 1.04, 1.46), [[(-0.53, 1.425, .04), (0.51, 1.425, .04), (0.58, 1.085, .03), (-0.62, 1.085, .03)]],
                       {"gesto": True})],
                otwory=[("L", (-0.85, 0.12, 0.93, 1.42)), ("L", (0.14, 0.84, 0.93, 1.42)), ("L", (0.82, 1.50, 0.93, 1.42)),
                        ("P", (-0.16, 0.80, 0.93, 1.42)), ("P", (-0.86, -0.18, 0.93, 1.42)), ("P", (-1.55, -0.84, 0.93, 1.42)),
                        ("F", (-0.62, 0.62, 1.00, 1.45))])


def _pokazowa(mod, tekst, przod, tyl, stare=()):
    """a show car's plate (its name on the period black plate, as the owner's pictures have it): the old one off"""
    for w, b in stare: mod.zmaz(w, b, prog=.0, rozszerz=2)
    for w, t in (("F", przod), ("T", tyl)):
        if t: mod.tablica(w, tekst, t[0], t[1], .52, .12)


def maluch_tuning_czerwony(mod):
    """the owner's red 126p with the kit and the roof spoiler (his picture "Maluch"): a deep metallic red, its MALUCH
    show plates made clean, panes in its open windows, the windscreen and the back window tinted"""
    CZE = probka(mod, "L", (-0.40, 0.20, 0.45, 0.70))
    _dwa(mod, [("F", (-0.08, 0.08, 0.52, 0.60))], CZE)
    farby = _lakiery(mod, [(CZE, (148, 14, 22), .20, .12)], tol=22.0)
    _pokazowa(mod, "MALUCH", (-0.05, 0.32), (-0.015, 0.60), stare=[("F", (-0.32, 0.22, 0.26, 0.38)), ("T", (-0.27, 0.24, 0.54, 0.66))])
    return dict(farby=farby, napisy=["MALUCH"],
                okna=[("F", (-0.58, 0.58, 0.80, 1.16)), ("T", (-0.58, 0.58, 0.80, 1.16))],
                otwory=[("L", (-0.72, 0.34, 0.80, 1.17)), ("L", (0.37, 0.98, 0.80, 1.17)), ("P", (-0.44, 0.62, 0.80, 1.17)),
                        ("P", (-1.08, -0.38, 0.80, 1.17))])


def maluch_tuning_kremowy(mod):
    """the owner's cream wide 126p with the splitter (his picture of one on white five-spoke wheels): cream, glossy,
    the black plates of the time in place of the kit maker's, panes in its open windows, the windscreen tinted"""
    KRE = probka(mod, "L", (-0.40, 0.20, 0.40, 0.65))
    farby = _lakiery(mod, [(KRE, (226, 212, 188), .22, 0.0)], tol=20.0)
    tablice(mod, "WEA 1266", (-0.05, 0.32), (-0.05, 0.485), stare=[("F", (-0.34, 0.25, 0.26, 0.38)), ("T", (-0.27, 0.16, 0.43, 0.54))])
    return dict(farby=farby, napisy=["WEA 1266"],
                okna=[("F", (-0.62, 0.62, 0.77, 1.22), [[(-0.51, 1.175, .05), (0.515, 1.175, .05), (0.59, 0.80, .04), (-0.59, 0.80, .04)]])],
                otwory=[("L", (-0.55, 0.38, 0.74, 1.20)), ("L", (0.36, 1.02, 0.74, 1.20)), ("P", (-0.43, 0.50, 0.74, 1.20)),
                        ("P", (-1.00, -0.37, 0.74, 1.20)), ("T", (-0.55, 0.55, 0.85, 1.22))])


def maluch_tuning_szary(mod):
    """the owner's grey-blue wide 126p with the cage (his picture "126P MALUCH" render, the kit's): grey-blue metallic,
    the black plates of the time in place of the kit maker's name, panes in its open side windows, the windscreen and back tinted"""
    SZA = probka(mod, "L", (-0.40, 0.20, 0.40, 0.65))
    _dwa(mod, [("T", (-0.24, 0.18, 0.28, 0.46))], SZA)
    farby = _lakiery(mod, [(SZA, (78, 94, 112), .22, .15)], tol=22.0)
    tablice(mod, "WES 1263", (-0.05, 0.33), (-0.04, 0.51), stare=[("F", (-0.30, 0.20, 0.27, 0.39)), ("T", (-0.25, 0.17, 0.46, 0.56))])
    return dict(farby=farby, napisy=["WES 1263"],
                # (the windscreen and the back window measured: Tripo's garble in them off)
                okna=[("F", (-0.62, 0.62, 0.80, 1.28), [[(-0.50, 1.255, .05), (0.50, 1.255, .05), (0.595, 0.83, .04), (-0.59, 0.83, .04)]]),
                      ("T", (-0.62, 0.62, 0.84, 1.27), [[(-0.505, 1.24, .05), (0.505, 1.24, .05), (0.57, 0.865, .04), (-0.58, 0.865, .04)]])],
                otwory=[("L", (-0.72, 0.36, 0.76, 1.25)), ("L", (0.40, 1.00, 0.76, 1.25)), ("P", (-0.46, 0.64, 0.76, 1.25)),
                        ("P", (-1.10, -0.48, 0.76, 1.25))])


def _lustro(okna):
    """the left side's windows (view L) for the right (view P: u mirrored) - Tripo's cars are symmetric"""
    out = []
    for o in okna:
        if o[0] != "L": continue
        b = o[1]; pol = [[(-p[0], p[1]) + tuple(p[2:]) for p in reversed(q)] for q in o[2]]
        out.append(("P", (-b[1], -b[0], b[2], b[3]), pol) + tuple(o[3:]))
    return okna + out


def fiat125p_tuning_granat(mod):
    """the owner's navy wide 125p with the wing (his picture "125p TUNED"): a deep navy metallic, its show plates made
    clean ("125p"), the garbled marks on its rear wings off, the glass tinted with its chrome frames kept"""
    GRA = probka(mod, "L", (-0.40, 0.20, 0.50, 0.75))
    _dwa(mod, [("L", (1.50, 1.95, 0.58, 0.80)), ("P", (-1.95, -1.50, 0.58, 0.80))], GRA)
    farby = _lakiery(mod, [(GRA, (22, 38, 92), .18, .15)], okna=FIAT125P_TUNING_OKNA, tol=22.0)
    _pokazowa(mod, "125p", (-0.01, 0.35), (-0.02, 0.645), stare=[("F", (-0.28, 0.26, 0.28, 0.42)), ("T", (-0.26, 0.22, 0.58, 0.71))])
    return dict(farby=farby, napisy=["125p"], okna=FIAT125P_TUNING_OKNA)


FIAT125P_TUNING_OKNA = _lustro([
    ("L", (-0.72, -0.44, 0.93, 1.31), [[(-0.52, 1.285, .02), (-0.47, 1.285, 0), (-0.47, 0.955, 0), (-0.69, 0.955, .02)]]),
    ("L", (-0.47, 0.06, 0.93, 1.31), [[(-0.445, 1.285, .01), (0.03, 1.285, .02), (0.03, 0.955, .02), (-0.445, 0.955, .01)]]),
    ("L", (0.09, 0.66, 0.93, 1.31), [[(0.12, 1.285, .02), (0.625, 1.285, .01), (0.625, 0.955, .01), (0.12, 0.955, .02)]]),
    ("L", (0.65, 0.95, 0.93, 1.31), [[(0.675, 1.285, .01), (0.77, 1.285, .03), (0.905, 0.955, .02), (0.675, 0.955, .01)]]),
    ("F", (-0.72, 0.72, 0.92, 1.34), [[(-0.58, 1.315, .06), (0.58, 1.315, .06), (0.68, 0.955, .05), (-0.68, 0.955, .05)]]),
    # (the wing on its struts stands 70 cm behind the back window: left out of the rear view)
    ("T", (-0.68, 0.66, 0.92, 1.34), [[(-0.56, 1.31, .06), (0.55, 1.31, .06), (0.62, 0.955, .05), (-0.63, 0.955, .05)]], {"do": 1.5}),
])


def polonez_tuning_bordo(mod):
    """the owner's wine-red Polonez on the wide kit with the wing: a deep metallic wine red (Tripo's speckle smoothed),
    the black plates of the time, the glass tinted"""
    BOR = probka(mod, "L", (-0.40, 0.20, 0.50, 0.75))
    farby = _lakiery(mod, [(BOR, (112, 10, 26), .20, .15)], okna=POLONEZ_TUNING_OKNA, tol=24.0, sigma=4.0, plamy=.45)
    tablice(mod, "WXA 1987", (0.0, 0.40), (0.02, 0.745), stare=[("T", (-0.26, 0.30, 0.68, 0.81))])
    # its rear lamps (Tripo's dark boxes, painted over with the body): the Polonez's wide clusters, amber outside
    for uc in (-0.47, 0.47): mod.lampa("T", uc, 0.765, .40, .12, "zespolona")
    return dict(farby=farby, napisy=["WXA 1987"], okna=POLONEZ_TUNING_OKNA)


POLONEZ_TUNING_OKNA = _lustro([
    ("L", (-0.72, 0.08, 0.93, 1.30), [[(-0.36, 1.265, .02), (0.06, 1.265, .02), (0.06, 0.955, .01), (-0.67, 0.955, .02)]]),
    ("L", (0.15, 0.95, 0.93, 1.30), [[(0.18, 1.265, .02), (0.74, 1.265, .02), (0.915, 0.955, .01), (0.18, 0.955, .01)]]),
    ("L", (0.86, 1.40, 0.93, 1.28), [[(0.895, 1.245, .02), (0.995, 1.245, .02), (1.36, 0.968, .03), (1.05, 0.968, .02)]]),
    ("F", (-0.72, 0.72, 0.90, 1.34), [[(-0.60, 1.31, .05), (0.60, 1.31, .05), (0.68, 0.93, .04), (-0.68, 0.93, .04)]]),
    # (its wing stands 80 cm behind the raked hatch glass: left out of the rear view)
    ("T", (-0.68, 0.68, 0.90, 1.32), [[(-0.60, 1.29, .05), (0.60, 1.29, .05), (0.64, 0.93, .04), (-0.64, 0.93, .04)]], {"do": 1.92, "gesto": True}),
])


# ------------------------------------------------------------------------------- the third set (owner 25.11 02:46)
def warszawa_m20(mod):
    """the owner's Warszawa M20 "garbus": Tripo painted its sides in two blues (teal before, navy behind) - one deep
    teal-green coat over both, its chrome kept, the plates of the time, panes in its open windows"""
    PRZ = probka(mod, "L", (-1.60, -1.20, 0.75, 0.95)); TYL = probka(mod, "L", (1.20, 1.60, 0.80, 1.00))
    _dwa(mod, [("F", (-0.10, 0.10, 0.92, 1.02))], PRZ)
    farby = _lakiery(mod, [(PRZ, (28, 84, 86), .30, 0.0), (TYL, (28, 84, 86), .30, 0.0)], tol=22.0, plamy=.5, odcien=(165, 250),
                     kontrast=.35)
    tablice(mod, "WAR 2045", (0.0, 0.385), (-0.05, 0.97), stare=[("F", (-0.30, 0.30, 0.32, 0.45)), ("T", (-0.34, 0.24, 0.90, 1.05))])
    return dict(farby=farby + [PRZ, TYL], napisy=["WAR 2045"], opony=.66,
                # its small back window Tripo painted (a light glare): tinted glass in its measured outline
                okna=[("T", (-0.52, 0.50, 1.34, 1.60), [[(-0.41, 1.548, .07), (0.39, 1.548, .07), (0.435, 1.395, .05), (-0.455, 1.395, .05)]],
                       {"gesto": True})],
                otwory=[("L", (-0.62, 0.12, 1.14, 1.58)), ("L", (0.22, 0.98, 1.14, 1.58)), ("P", (-0.12, 0.62, 1.14, 1.58)),
                        ("P", (-0.98, -0.22, 1.14, 1.58)), ("F", (-0.66, 0.66, 1.26, 1.68))])


def zuk_bus(mod):
    """the owner's Żuk minibus: Tripo painted one side beige, the other grey - both its beige again (the navy stripes
    kept), the plates of the time, panes in its open windows"""
    BEZ = probka(mod, "L", (-1.80, -1.30, 0.80, 1.10)); SZA = probka(mod, "P", (-1.50, -0.60, 1.18, 1.40))
    PAS = probka(mod, "P", (-1.50, -0.60, 1.08, 1.11))
    # Tripo's navy stripes came out broken on one side: painted over (only on the sides and the back, at their height)
    # and laid again whole - three navy bands down both sides and across the back
    import numpy as np
    c = mod.cen; bok = ((np.abs(c[:, 0]) > .80) | (c[:, 2] < -2.10)) & (c[:, 1] > .62) & (c[:, 1] < 1.18)
    ys, xs, _ = mod.teksele(bok); pm = np.zeros((mod.h, mod.w), np.float32); pm[ys, xs] = 1
    farby = _lakiery(mod, [(BEZ, (210, 184, 140), .34, 0.0), (SZA, (210, 184, 140), .34, 0.0), (PAS, (210, 184, 140), .34, 0.0, pm)],
                     tol=22.0, kontrast=.6)
    # the three bands laid by the body's own space (its sides |x| > 0.8 from the sliding door back, its back z < -2.1):
    # their edges straight across the swage lines (a picture laid in a view frayed where the panel turned from it)
    ys, xs, pts = mod.teksele(((np.abs(c[:, 0]) > .78) & (c[:, 2] > -2.24) & (c[:, 2] < .60)) | (c[:, 2] < -2.08))
    x, y, z = pts[:, 0], pts[:, 1], pts[:, 2]; bok = np.abs(x) > .78
    pas = np.zeros(len(ys), bool)
    for v in (1.095, 0.895, 0.71):
        w_pasie = (y >= v - .03) & (y <= v + .03)
        tyl = (z < -2.08) & ((np.abs(x) < .90) if v > .8 else ((np.abs(x) > .34) & (np.abs(x) < .90)))   # (the plate's place left clear)
        pas |= w_pasie & ((bok & (z > -2.24) & (z < .60)) | tyl)
    mod.img[ys[pas], xs[pas]] = np.array((28, 38, 78), np.float32) / 255
    # its front: the grille panel and the bumper Tripo rusted, painted the period's black-grey (the headlamps kept)
    zost, _ = _zostaw(mod, [("F", (-0.64, -0.45, 0.81, 1.005), "lampa_o"), ("F", (0.44, 0.635, 0.81, 1.005), "lampa_o")])
    zy, zx = np.nonzero(zost > 0); kopia = mod.img[zy, zx].copy()
    km = {k: (mp, mp[np.clip(zy * mp.shape[0] // mod.h, 0, mp.shape[0] - 1), np.clip(zx * mp.shape[1] // mod.w, 0, mp.shape[1] - 1)].copy()) for k, mp in mod.mapy.items()}
    _pomaluj(mod, "F", (-0.67, 0.67, 0.765, 1.115), (36, 37, 40), szorstkosc=.45, glebia=.06)
    _pomaluj(mod, "F", (-0.88, 0.87, 0.495, 0.63), (42, 43, 46), szorstkosc=.50, glebia=.06)
    mod.img[zy, zx] = kopia
    for k, (mp, v0) in km.items(): mp[np.clip(zy * mp.shape[0] // mod.h, 0, mp.shape[0] - 1), np.clip(zx * mp.shape[1] // mod.w, 0, mp.shape[1] - 1)] = v0
    tablice(mod, "LUB 4507", (-0.03, 0.465), (0.0, 0.67), stare=[("F", (-0.32, 0.26, 0.40, 0.53))])
    return dict(farby=farby + [BEZ, SZA, PAS], napisy=["LUB 4507"], opony=.66,
                otwory=[("L", (-1.48, -1.00, 1.45, 2.05)), ("L", (-1.05, -0.28, 1.45, 2.05)), ("L", (-0.34, 0.42, 1.45, 2.05)),
                        ("L", (0.44, 1.22, 1.45, 2.05)), ("L", (1.24, 1.96, 1.45, 2.05)),
                        ("P", (1.00, 1.48, 1.45, 2.05)), ("P", (0.28, 1.05, 1.45, 2.05)), ("P", (-0.42, 0.34, 1.45, 2.05)),
                        ("P", (-1.22, -0.44, 1.45, 2.05)), ("P", (-1.96, -1.24, 1.45, 2.05)),
                        ("F", (-0.78, 0.78, 1.48, 2.08)), ("T", (-0.55, 0.55, 1.48, 2.00))])


def nysa_bus(mod):
    """the owner's Nysa 522 minibus: turquoise with its white roof, both a fresh coat; the glass Tripo painted kept clear
    (its inside seen) with its rubber, panes in the windows it left open, the plates of the time"""
    TUR = probka(mod, "L", (-0.80, -0.20, 0.80, 1.30)); BIA = probka(mod, "G", (-0.30, 0.30, -0.50, 0.50))
    farby = _lakiery(mod, [(TUR, (18, 124, 142), .30, 0.0, _maska(mod, pod=2.04)), (BIA, (240, 240, 236), .34, 0.0, _maska(mod, nad=2.04))],
                     tol=22.0)
    tablice(mod, "OPA 5221", (-0.03, 0.65), (0.0, 0.80), stare=[("F", (-0.34, 0.29, 0.57, 0.73))])
    return dict(farby=farby + [TUR, BIA], farby_okien=[TUR, BIA], napisy=["OPA 5221"], opony=.66, przyciemniane=False,
                okna=[("L", (-1.56, -1.19, 1.37, 2.08)), ("L", (-1.14, -0.35, 1.48, 2.00)), ("L", (0.43, 1.24, 1.48, 2.00)),
                      ("L", (1.22, 1.99, 1.48, 2.00)),
                      ("P", (-2.06, -1.30, 1.50, 2.04)), ("P", (-1.32, -0.52, 1.50, 2.04)), ("P", (0.28, 1.05, 1.50, 2.04)),
                      ("P", (1.12, 1.59, 1.37, 2.08)),
                      ("T", (-0.84, -0.03, 1.51, 2.05)), ("T", (-0.02, 0.78, 1.51, 2.05))],
                otwory=[("L", (-0.34, 0.45, 1.45, 2.04)), ("P", (-0.54, 0.27, 1.45, 2.04)), ("F", (-0.95, 0.90, 1.36, 2.08))])


def wsk(mod):
    """the owner's black WSK M06: the tank's badge Tripo garbled off, WSK in plain letters on it; the paint kept"""
    CZA = probka(mod, "L", (-0.15, -0.05, 0.73, 0.79))
    for w, b in (("L", (-0.325, -0.215, 0.715, 0.825)), ("P", (0.255, 0.365, 0.715, 0.825))):
        mod.zmaz(w, b, farba=CZA, prog=.06, rozszerz=3, plasko=True)
    # its name in the cream of the tank's pinstripe, where the badge was
    mod.napis("L", "WSK", -0.235, 0.765, .034, kolor=(222, 214, 188)); mod.napis("P", "WSK", 0.275, 0.765, .034, kolor=(222, 214, 188))
    return dict(napisy=["WSK"], opony=.80)


def osa(mod):
    """the owner's red WFM Osa scooter: a fresh glossy red (its cream seat and black vents kept), OSA on its sides"""
    CZE = probka(mod, "L", (-0.10, 0.25, 0.45, 0.62))
    farby = _lakiery(mod, [(CZE, (156, 18, 26), .24, 0.0)], tol=20.0)
    mod.napis("L", "OSA", 0.36, 0.61, .040, kolor=(206, 208, 210)); mod.napis("P", "OSA", -0.36, 0.61, .040, kolor=(206, 208, 210))
    return dict(farby=farby, napisy=["OSA"], opony=.78)


TRABANT_OKNA = [
    ("L", (-0.55, 0.40, 1.03, 1.47), [[(-0.285, 1.433), (0.35, 1.433), (0.35, 1.063, .02), (-0.50, 1.063, .02), (-0.49, 1.17, .02)]]),
    ("L", (0.36, 1.10, 1.02, 1.47), [[(0.397, 1.437), (0.915, 1.437, .05), (1.075, 1.15, .05), (1.06, 1.05, .04), (0.397, 1.05)]]),
    ("P", (-0.37, 0.56, 1.03, 1.48), [[(-0.342, 1.45), (0.30, 1.44), (0.525, 1.063, .02), (-0.342, 1.063, .02)]]),
    ("P", (-1.10, -0.35, 1.02, 1.47), [[(-0.91, 1.44, .05), (-0.383, 1.44), (-0.383, 1.058), (-1.05, 1.058, .04), (-1.075, 1.15, .05)]]),
    ("F", (-0.68, 0.68, 1.05, 1.47), [[(-0.55, 1.44, .06), (0.58, 1.44, .06), (0.645, 1.085, .05), (-0.625, 1.085, .05)]]),
    ("T", (-0.58, 0.57, 1.08, 1.47), [[(-0.54, 1.445, .06), (0.53, 1.445, .06), (0.53, 1.105, .05), (-0.54, 1.105, .05)]]),
]


# the headlamps for the night (measured on each vehicle's front view, in its own unshifted metres: u, v, radius - or u, v,
# width, height for a rectangular lens); the game lights them at night through the light map
REFLEKTORY = {
    "milicja": [(-0.59, .70, .08), (-0.39, .70, .07), (0.385, .70, .07), (0.58, .70, .08)],
    "maluch": [(-0.455, .62, .19, .11), (0.45, .62, .19, .11)],
    "syrena": [(-0.555, .665, .085), (0.555, .665, .085)],
    "bosna": [(-0.735, 1.10, .30, .22), (0.735, 1.10, .30, .22)],
    "fiat125p": [(-0.575, .695, .075), (-0.415, .695, .075), (0.42, .695, .075), (0.59, .695, .075)],
    "polonez": [(-0.54, .75, .32, .11), (0.57, .75, .32, .11)],
    "fiat125p_tuning": [(-0.57, .67, .075), (-0.395, .67, .075), (0.385, .67, .075), (0.56, .67, .075)],
    "polonez_tuning": [(-0.50, .545, .055), (0.50, .545, .055)],
    "polonez_milicja": [(-0.30, .69, .075), (-0.125, .69, .075), (0.725, .69, .075), (0.905, .69, .075)],
    "nysa_milicja": [(-0.735, .86, .11), (0.74, .86, .11)],
    "star_zielony": [(-0.715, 1.15, .14), (0.705, 1.15, .14)],
    "star_pomaranczowy": [(-0.74, 1.03, .11), (0.74, 1.03, .11)],
    "trabant": [(-0.63, .82, .08), (0.64, .82, .08)],
    "trabant_tuning_braz": [(-0.57, .665, .085), (0.58, .665, .085)],
    "trabant_tuning_czarny": [(-0.60, .735, .095), (0.60, .735, .095)],
    "syrena_niebieska": [(-0.605, .745, .10), (0.595, .745, .10)],
    "fiat125p_granat": [(-0.585, .52, .07), (-0.42, .52, .07), (0.415, .52, .07), (0.585, .52, .07)],
    "fiat125p_tuning_granat": [(-0.595, .56, .07), (-0.435, .56, .07), (0.43, .56, .07), (0.59, .56, .07)],
    "polonez_caro_zielony": [(-0.52, .73, .40, .12), (0.52, .73, .40, .12)],
    "polonez_tuning_bordo": [(-0.575, .605, .07), (-0.405, .605, .07), (0.41, .605, .07), (0.575, .605, .07)],
    "maluch_tuning_czerwony": [(-0.455, .60, .19, .11), (0.46, .60, .19, .11)],
    "maluch_tuning_kremowy": [(-0.48, .545, .20, .12), (0.49, .545, .20, .12)],
    "maluch_tuning_szary": [(-0.49, .525, .22, .14), (0.505, .525, .22, .14)],
    "warszawa_m20": [(-0.725, .85, .10), (0.725, .85, .10)],
    "nysa_bus": [(-0.745, .94, .12), (0.755, .94, .12)],
    "zuk_bus": [(-0.575, .885, .08), (0.54, .885, .08)],
}


# the owner's 3D badges where the real cars wear them (measured on each one's views: view, u, v of the badge's centre) -
# FSO's round emblem on the Polonez's and the late 125p's grille, the Sachsenring S on the Trabant's front, POLSKI FIAT
# on the early 125p's grille, FSO POLONEZ across the Caro's tailgate
ZNACZKI_NA_AUTACH = {
    "polonez": [("fso", "F", 0.0, .745)],
    "polonez_milicja": [("fso", "F", 0.30, .70)],
    "polonez_tuning": [("fso", "F", 0.0, .53 * 4.272 / 4.0)],
    "polonez_caro_zielony": [("fso", "F", 0.0, .715), ("fso_polonez", "T", 0.0, .865)],
    "polonez_tuning_bordo": [("fso", "F", 0.0, .61)],
    "fiat125p": [("fso", "F", 0.0, .715)],
    "fiat125p_granat": [("fso", "F", 0.0, .52)],
    "milicja": [("fso", "F", 0.0, .70)],
    "fiat125p_tuning_granat": [("polski_fiat", "F", 0.0, .565)],
    "trabant": [("trabant_s", "F", 0.0, .875)],
    "trabant_tuning_braz": [("trabant_s", "F", 0.0, .47)],
    "trabant_tuning_czarny": [("trabant_s", "F", 0.0, .58)],
}


PRZEPISY = {"trabant": trabant, "warszawa_m20": warszawa_m20, "zuk_bus": zuk_bus, "nysa_bus": nysa_bus, "wsk": wsk, "osa": osa,
            "fiat125p_tuning_granat": fiat125p_tuning_granat, "polonez_tuning_bordo": polonez_tuning_bordo,
            "maluch_tuning_czerwony": maluch_tuning_czerwony, "maluch_tuning_kremowy": maluch_tuning_kremowy,
            "maluch_tuning_szary": maluch_tuning_szary, "fiat125p_granat": fiat125p_granat, "polonez_caro_zielony": polonez_caro_zielony, "syrena_niebieska": syrena_niebieska, "trabant_tuning_czarny": trabant_tuning_czarny, "trabant_tuning_braz": trabant_tuning_braz, "komar": komar, "jawa": jawa, "romet": romet, "junak": junak, "star_zielony": star_zielony, "star_pomaranczowy": star_pomaranczowy, "fiat125p_tuning": fiat125p_tuning, "polonez_tuning": polonez_tuning, "milicja": milicja, "maluch": maluch, "syrena": syrena, "fiat125p": fiat125p, "polonez": polonez, "bosna": bosna,
            "polonez_milicja": polonez_milicja, "nysa_milicja": nysa_milicja}
