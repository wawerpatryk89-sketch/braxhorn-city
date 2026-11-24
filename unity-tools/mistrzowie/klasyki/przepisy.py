"""The restoration of each classic's colour map (owner 24.11.2026 22:54-23:07: "każde napisy, jak są źle, to zmazać i
nowe", "trzeba odrestaurować, napisy poprawić ... wyraźnie", "wszystko odrestaurować co trzeba").

Every box is in a view's metres (tekstura.WIDOKI: L the left side seen from +x with u = -z, P the right side with u = z,
F the front with u = x, T the rear with u = -x; v up from the ground) - read off the gridded orthographic pictures of
the vehicle at its real size. A recipe returns what przygotuj_klasyki needs besides the map:
  okna    the painted windows (view, box) - their faces become the tinted glass;
  otwory  the open windows (view, box round the opening and its frame) - a pane is built in each;
  farby   the body's paints (sRGB 0-255): a window face of that colour is body, not glass;
  napisy  the lettering laid (for the catalogue).
Plates: 1976-2000, black with white characters - a private car 3 letters + 4 digits, a state vehicle 3 letters +
3 digits + a letter, the militia MO (autoblog.spidersweb.pl "polskie czarne tablice rejestracyjne"; auto-swiat.pl:
"MO - wyróżnik milicji")."""


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
    NIEB = (20, 64, 100)
    Z = [("L", (-0.84, -0.06, 0.575, 0.835)), ("L", (0.24, 1.84, 0.985, 1.13)), ("L", (1.50, 1.90, 1.16, 1.54)), ("L", (0.38, 0.68, 0.60, 0.88)),
         ("P", (-1.96, -0.34, 0.985, 1.13)), ("P", (-1.90, -1.46, 1.16, 1.54)), ("P", (-0.80, -0.50, 0.60, 0.88)), ("P", (-0.04, 0.66, 0.575, 0.835)),
         ("F", (-0.62, 0.62, 1.58, 1.76)), ("T", (-0.60, 0.60, 0.985, 1.13)), ("F", (-0.22, 0.22, 1.85, 1.98)), ("T", (-0.22, 0.22, 1.85, 1.98))]
    _dwa(mod, Z, NIEB)
    mod.zmaz("T", (-0.32, 0.30, 0.40, 0.56), prog=.5)                         # the rear plate
    mod.zmaz("F", (-0.30, 0.30, 0.33, 0.49), prog=.5)                         # the front plate
    mod.odnow([NIEB, (225, 228, 230)], tol=.16)
    for w, u in (("L", -0.42), ("P", 0.32)): mod.napis(w, "MILICJA", u, 0.71, .10)
    for w, u in (("L", 1.05), ("P", -1.15)): mod.napis(w, "MILICJA", u, 1.04, .11)
    mod.zmaz("F", (-0.64, 0.64, 1.56, 1.80), farba=NIEB, prog=.035, rozszerz=3, cos=.1, glebia=.15)   # the old letters' last trace
    mod.napis("F", "MILICJA", 0, 1.66, .085); mod.napis("T", "MILICJA", 0, 1.045, .075)
    for w in ("F", "T"): mod.napis(w, "MILICJA", 0, 1.915, .045)
    mod.tablica("F", "MO 1261", 0, 0.41); mod.tablica("T", "MO 1261", -0.02, 0.465)
    return dict(farby=[NIEB, (225, 228, 230)], farby_okien=[NIEB], napisy=["MILICJA", "MO 1261"],
                okna=[("L", (-0.95, 0.06, 0.96, 1.44)), ("L", (0.36, 1.21, 1.10, 1.57)),
                      ("P", (-0.04, 0.95, 0.96, 1.44)), ("P", (-1.27, -0.45, 1.10, 1.57)),
                      ("F", (-0.72, 0.72, 0.98, 1.46)), ("T", (-0.62, 0.60, 1.10, 1.53))])


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
    tablice(mod, "KRA 4416", (-0.03, 0.315), (-0.06, 0.56, .36, .10),
            stare=[("F", (-0.30, 0.24, 0.26, 0.37)), ("T", (-0.26, 0.14, 0.495, 0.625))])
    mod.odnow([BEZ], tol=.18)
    mod.napis("F", "126p", 0.205, 0.555, .026, kolor=(196, 198, 200))
    return dict(farby=[BEZ], napisy=["126p", "KRA 4416"],
                okna=[("L", (-0.57, 0.39, 0.85, 1.25)), ("L", (0.47, 1.07, 0.85, 1.24)),
                      ("P", (-0.45, 0.42, 0.85, 1.22)), ("P", (-1.12, -0.52, 0.85, 1.22)),
                      ("F", (-0.57, 0.57, 0.86, 1.26)), ("T", (-0.53, 0.47, 0.87, 1.27))])


def syrena(mod):
    MUS = probka(mod, "L", (0.20, 0.60, 0.45, 0.60)); BIA = probka(mod, "F", (-0.40, -0.15, 0.75, 0.90))
    _dwa(mod, [("F", (-0.05, 0.05, 0.76, 0.86)), ("F", (0.05, 0.36, 0.65, 0.75)), ("T", (-0.05, 0.05, 0.72, 0.82))], BIA)
    tablice(mod, "WAB 7491", (0.0, 0.335), (-0.01, 0.63),
            stare=[("F", (-0.25, 0.25, 0.27, 0.40)), ("T", (-0.25, 0.23, 0.555, 0.70))])
    mod.odnow([MUS, BIA], tol=.18)
    mod.napis("F", "SYRENA", 0.0, 0.70, .028, kolor=(206, 208, 210))
    return dict(farby=[MUS, BIA], napisy=["SYRENA", "WAB 7491"],
                otwory=[("L", (-0.62, 0.10, 0.98, 1.42)), ("L", (0.08, 0.96, 0.98, 1.42)),
                        ("P", (-0.10, 0.62, 0.98, 1.42)), ("P", (-0.98, -0.06, 0.98, 1.42)),
                        ("F", (-0.66, 0.66, 1.02, 1.47)), ("T", (-0.64, 0.64, 1.02, 1.47))])


def fiat125p(mod):
    CZE = probka(mod, "L", (0.20, 0.60, 0.62, 0.80))
    _dwa(mod, [("F", (-0.06, 0.05, 0.65, 0.76)), ("F", (0.05, 0.34, 0.65, 0.75))], (14, 14, 16))
    tablice(mod, "WAR 1252", (0.0, 0.40), (-0.05, 0.81), stare=[("T", (-0.29, 0.19, 0.73, 0.89))])
    mod.odnow([CZE], tol=.18)
    mod.napis("F", "125p", 0.19, 0.70, .03, kolor=(206, 208, 210))
    return dict(farby=[CZE], napisy=["125p", "WAR 1252"], opony=.66,
                okna=[("F", (-0.78, 0.74, 1.02, 1.54)), ("T", (-0.74, 0.66, 1.06, 1.54))],
                otwory=[("L", (-0.80, 0.04, 1.00, 1.52)), ("L", (0.03, 0.86, 1.00, 1.52)),
                        ("P", (-0.04, 0.80, 1.00, 1.52)), ("P", (-0.86, -0.03, 1.00, 1.52))])


def polonez(mod):
    CZE = probka(mod, "L", (0.20, 0.60, 0.70, 0.85))
    _dwa(mod, [("F", (-0.12, 0.03, 0.66, 0.78)), ("F", (0.10, 0.32, 0.66, 0.78)), ("T", (-0.13, -0.02, 0.93, 1.04)),
               ("T", (0.31, 0.55, 0.80, 0.87))], CZE)
    tablice(mod, "WAX 6230", (-0.02, 0.51), (-0.06, 0.83),
            stare=[("F", (-0.34, 0.29, 0.44, 0.585)), ("T", (-0.31, 0.19, 0.76, 0.895))])
    mod.odnow([CZE], tol=.18)
    mod.napis("F", "CARO", 0.21, 0.715, .026, kolor=(206, 208, 210))
    mod.napis("T", "POLONEZ", 0.43, 0.835, .024, kolor=(206, 208, 210))
    return dict(farby=[CZE], napisy=["CARO", "POLONEZ", "WAX 6230"], opony=.66,
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
    tablice(mod, "MO 7846", (0.28, 0.41), (-0.335, 0.735, .40, .10),
            stare=[("F", (0.08, 0.48, 0.31, 0.52)), ("T", (-0.55, -0.12, 0.675, 0.80)), ("T", (-0.53, -0.16, 0.39, 0.585))])
    mod.odnow([GRA, BIA], tol=.16)
    mod.napis("L", "MILICJA", -0.45, 0.70, .085); mod.napis("P", "MILICJA", 0.33, 0.70, .085)
    return dict(farby=[GRA, BIA], farby_okien=[GRA], napisy=["MILICJA", "MO 7846"], opony=.66,
                okna=[("L", (-0.52, 0.10, 0.97, 1.49)), ("L", (0.12, 0.78, 0.97, 1.49)), ("L", (0.77, 1.40, 0.97, 1.47)),
                      ("P", (-0.16, 0.52, 0.97, 1.49)), ("P", (-0.88, -0.20, 0.97, 1.49)), ("P", (-1.52, -0.88, 0.97, 1.47)),
                      ("F", (-0.48, 0.93, 0.98, 1.49)), ("T", (-0.97, 0.30, 1.10, 1.44))])


def nysa_milicja(mod):
    GRA = probka(mod, "L", (1.40, 1.80, 0.75, 1.00)); BIA = probka(mod, "L", (0.80, 1.20, 1.21, 1.28))
    _dwa(mod, [("L", (-0.27, 0.76, 0.86, 1.19)), ("L", (-1.09, -0.76, 0.79, 1.17)), ("P", (0.32, 0.41, 0.95, 1.04)),
               ("T", (-0.47, -0.28, 0.65, 0.77))], GRA)
    tablice(mod, "MO 4052", (-0.03, 0.49), (0.0, 0.62), stare=[("F", (-0.30, 0.24, 0.43, 0.55))])
    mod.odnow([GRA, BIA], tol=.16)
    mod.napis("L", "MILICJA", 0.24, 1.04, .13); mod.napis("P", "MILICJA", -0.24, 1.04, .13)
    for w in ("F", "T"): mod.napis(w, "MILICJA", 0.0, 2.33, .05, kolor=GRA)
    return dict(farby=[GRA, BIA], farby_okien=[GRA], napisy=["MILICJA", "MO 4052"], opony=.66,
                okna=[("L", (-1.26, -0.58, 1.34, 1.90)), ("L", (-0.16, 0.60, 1.34, 1.90)), ("L", (0.64, 1.34, 1.34, 1.90)),
                      ("L", (1.35, 1.97, 1.34, 1.90)),
                      ("P", (-2.04, -1.43, 1.36, 1.90)), ("P", (-1.41, -0.71, 1.36, 1.90)), ("P", (-0.66, 0.07, 1.36, 1.90)),
                      ("P", (0.54, 1.20, 1.36, 1.90)),
                      ("F", (-0.95, 0.90, 1.32, 2.02)), ("T", (-0.78, 0.73, 1.36, 1.88))])


def fiat125p_tuning(mod):
    """the owner's black-green 125p racer (roll cage, wire wheels): its plates, the paint evened, glass in the openings"""
    s = _Skala(mod, 4.23 / 4.0)                                  # read off its first 4 m views
    LAK = probka(mod, "L", s.b((0.20, 0.60, 0.50, 0.80)))
    tablice(s, "WAP 1125", (0.0, 0.37), (0.0, 0.60))
    mod.odnow([LAK], tol=.14)
    return dict(farby=[LAK], napisy=["WAP 1125"], opony=.72,
                otwory=s.okna([("L", (-0.60, 0.08, 0.90, 1.38)), ("L", (0.05, 0.84, 0.90, 1.38)),
                               ("P", (-0.08, 0.60, 0.90, 1.38)), ("P", (-0.84, -0.05, 0.90, 1.38)),
                               ("F", (-0.66, 0.66, 0.94, 1.42)), ("T", (-0.66, 0.66, 0.94, 1.42))]))


def polonez_tuning(mod):
    s = _Skala(mod, 4.272 / 4.0)                                 # read off its first 4 m views
    LAK = probka(mod, "L", s.b((0.20, 0.60, 0.40, 0.65)))
    _dwa(s, [("T", (-0.24, 0.24, 0.585, 0.65))], LAK)
    tablice(s, "WAT 1987", (0.0, 0.30), (0.0, 0.375), stare=[("T", (-0.21, 0.21, 0.315, 0.435))])
    mod.odnow([LAK], tol=.14)
    return dict(farby=[LAK], napisy=["WAT 1987"], opony=.72,
                otwory=s.okna([("L", (-0.62, 0.14, 0.76, 1.20)), ("L", (0.14, 0.90, 0.76, 1.20)),
                               ("P", (-1.00, -0.30, 0.76, 1.22)), ("P", (-0.22, 0.50, 0.76, 1.22)),
                               ("F", (-0.66, 0.66, 0.86, 1.20)), ("T", (-0.62, 0.62, 0.86, 1.20))]))


def star_zielony(mod):
    """Star 200: the cab's door windows open (panes built), its small rear side windows, windscreen and back window painted"""
    ZIE = probka(mod, "L", (-2.60, -2.00, 1.20, 1.50))
    _dwa(mod, [("F", (-0.38, 0.36, 1.52, 1.62)), ("F", (-0.10, 0.08, 1.65, 1.78)), ("F", (-0.12, 0.02, 1.33, 1.43))], ZIE)
    tablice(mod, "WAB 412T", (0.0, 0.80), (0.0, 0.86))
    # no restoration pass on the trucks: their wooden beds, the chassis and the tyres lie by the paint in the atlas and
    # have its warm hues - the cleaning would paint them green
    mod.napis("F", "STAR", 0.0, 1.57, .06, kolor=(206, 208, 210))
    return dict(farby=[ZIE], napisy=["STAR", "WAB 412T"], opony=.62,
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


PRZEPISY = {"komar": komar, "jawa": jawa, "romet": romet, "junak": junak, "star_zielony": star_zielony, "star_pomaranczowy": star_pomaranczowy, "fiat125p_tuning": fiat125p_tuning, "polonez_tuning": polonez_tuning, "milicja": milicja, "maluch": maluch, "syrena": syrena, "fiat125p": fiat125p, "polonez": polonez, "bosna": bosna,
            "polonez_milicja": polonez_milicja, "nysa_milicja": nysa_milicja}
