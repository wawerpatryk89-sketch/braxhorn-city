"""Texture work on a Tripo model's colour map, placed in 3D (owner 24.11.2026 22:54-23:06: "każde napisy, jak są źle, to
zmazać i nowe", "trzeba odrestaurować, napisy poprawić ... wyraźnie").

Tripo paints one 2048 atlas whose islands lie at any angle, mirrored or not; a lettering painted straight into the atlas
would come out crooked or mirrored. So everything here is placed on the vehicle, as a sign-writer would: a view (L the
left side seen from +x, P the right side from -x, F the front from +z, T the rear from -z), a box in that view's metres
(u to the viewer's right, v up - the same numbers as the gridded orthographic pictures of unity-tools/mistrzowie/klasyki),
and every texel of the faces seen there is mapped back to its point on the body (barycentric, per face):
  - zmaz:   what differs from the panel's own paint in the box (letters, crosses, stickers) is inpainted from the paint
            round it (OpenCV Telea) - the panel's shading stays;
  - napis:  crisp letters (Montserrat SemiBold, SIL OFL - the region's typeface, unity-tools/mistrzowie/fonts) laid on the
            panel, antialiased at the atlas' own texel size;
  - tablica: a registration plate of the time (1976-2000: black, white characters, 520 x 120 mm);
  - odnow:  the restoration - rust, dirt and scratches in the paint taken back to the paint's own colour, its shading
            (the low frequencies of the luminance) kept, the panel lines and trim left alone.
Only faces facing the viewer and in front (a depth test on a 2 cm grid) take paint: nothing goes through to the far side
or onto the inside of a panel."""
from pathlib import Path

import cv2
import numpy as np
from fontTools.ttLib import TTFont
from PIL import Image, ImageDraw, ImageFont

FONTS = Path(__file__).resolve().parents[1] / "fonts"
FONT_FILES = [FONTS / "Montserrat-SemiBold-latin.ttf", FONTS / "Montserrat-SemiBold-latin-ext.ttf"]
_CMAPS = [TTFont(str(f)).getBestCmap() for f in FONT_FILES]

# a view: the direction to the viewer, (u, v) from a body point
WIDOKI = {
    "L": (np.array([1.0, 0, 0]), lambda p: (-p[..., 2], p[..., 1])),
    "P": (np.array([-1.0, 0, 0]), lambda p: (p[..., 2], p[..., 1])),
    "F": (np.array([0, 0, 1.0]), lambda p: (p[..., 0], p[..., 1])),
    "T": (np.array([0, 0, -1.0]), lambda p: (-p[..., 0], p[..., 1])),
    "G": (np.array([0, 1.0, 0]), lambda p: (-p[..., 0], p[..., 2])),      # from above: u the vehicle's right... v forward
}


def lin(c): return np.where(c <= .04045, c / 12.92, ((c + .055) / 1.055) ** 2.4)
def srgb(c): return np.where(c <= .0031308, c * 12.92, 1.055 * np.power(np.maximum(c, 0), 1 / 2.4) - .055)


class Model:
    """P (n, 3) the body in metres (front +z, up +y, +x the left), I (m, 3), U (n, 2) glTF UVs (origin top left)"""

    def __init__(self, P, I, U, img, normal=None, mr=None):
        self.P, self.I, self.U = P, I, U
        self.img = np.asarray(img.convert("RGB")).astype(np.float32) / 255
        # the other maps: Tripo bakes the letters' relief into the normal map and their gloss into the metallic-roughness
        # map - an erased lettering that stayed there would still show as a ghost in the light
        self.mapy = {k: np.asarray(v.convert("RGB")).copy() for k, v in (("normal", normal), ("mr", mr)) if v is not None}
        self.h, self.w = self.img.shape[:2]
        T = P[I]; self.cen = T.mean(1)
        n = np.cross(T[:, 1] - T[:, 0], T[:, 2] - T[:, 0]); self.area = np.linalg.norm(n, axis=1) / 2
        self.n = n / np.maximum(2 * self.area[:, None], 1e-12)
        self.log = []
        self.ramki = []                                      # (kind, view, box): what was done where (the recipe's picture)
        self.swiatla = []                                    # (view, box, kind): lamps a recipe laid or marked
        self.tablice3d = []                                  # (text, kind, view, u, v, w, h): the plates, as parts

    # ---- which faces are seen in a view's box
    def widoczne(self, widok, box, cos=.35, glebia=.06, margines=.05, zewn=False, do=None, gesto=False):
        d, uv = WIDOKI[widok]; u, v = uv(self.cen); dep = self.cen @ d
        u0, u1, v0, v1 = box
        inb = (u > u0 - margines) & (u < u1 + margines) & (v > v0 - margines) & (v < v1 + margines)
        # do: what stands nearer the viewer than that depth left out (a wing on its struts before the back window)
        if do is not None: inb &= dep < do
        nd = self.n @ d
        # Tripo's shells are two layers ~1 cm apart: the outer facing out, the inner facing in. Both take the same paint
        # (the inner one shows through wherever the layers cross); only what lies at the front-most surface does
        ok = inb & ((nd > cos) if zewn else (np.abs(nd) > cos))   # zewn: the outer layer only (a door's inside is its card)
        if not ok.any(): return ok
        # depth: the front-most outward surface on a 2 cm grid (the panel, not what lies behind it)
        gu = np.floor((u - u0) / .02).astype(int); gv = np.floor((v - v0) / .02).astype(int)
        key = gu * 100000 + gv; best = {}
        out = inb & (nd > cos)
        for k, z in zip(key[out], dep[out]):
            if z > best.get(k, -1e9): best[k] = z
        if gesto:
            # a coarse surface (faces of 4-5 cm over a 2 cm grid: a cell without a face's centre would see through it to
            # the inside): its faces' corners and edges' middles mark the cells too
            t = self.P[self.I[out]]; pts = np.concatenate([t[:, 0], t[:, 1], t[:, 2], (t[:, 0] + t[:, 1]) / 2, (t[:, 1] + t[:, 2]) / 2,
                                                          (t[:, 2] + t[:, 0]) / 2, (t[:, 0] * 2 + t[:, 1] + t[:, 2]) / 4,
                                                          (t[:, 0] + t[:, 1] * 2 + t[:, 2]) / 4, (t[:, 0] + t[:, 1] + t[:, 2] * 2) / 4])
            pu, pv = uv(pts); pd = pts @ d
            kk = np.floor((pu - u0) / .02).astype(int) * 100000 + np.floor((pv - v0) / .02).astype(int)
            for k, z in zip(kk, pd):
                if z > best.get(k, -1e9): best[k] = z
        idx = np.nonzero(ok)[0]
        front = np.zeros(len(dep), bool)
        front[idx] = [dep[i] > best.get(key[i], 1e9) - glebia for i in idx]
        return ok & front

    # ---- the texels of faces: their atlas pixels and body points
    def teksele(self, faces):
        ys, xs, pts = [], [], []
        for f in np.nonzero(faces)[0]:
            tri = self.I[f]; t = self.U[tri] * [self.w, self.h]; p = self.P[tri]
            x0, y0 = np.floor(t.min(0)).astype(int); x1, y1 = np.ceil(t.max(0)).astype(int)
            x0, y0 = max(x0, 0), max(y0, 0); x1, y1 = min(x1, self.w - 1), min(y1, self.h - 1)
            if x1 < x0 or y1 < y0: continue
            gx, gy = np.meshgrid(np.arange(x0, x1 + 1) + .5, np.arange(y0, y1 + 1) + .5)
            a, b, c = t; den = (b[1] - c[1]) * (a[0] - c[0]) + (c[0] - b[0]) * (a[1] - c[1])
            if abs(den) < 1e-12: continue
            l1 = ((b[1] - c[1]) * (gx - c[0]) + (c[0] - b[0]) * (gy - c[1])) / den
            l2 = ((c[1] - a[1]) * (gx - c[0]) + (a[0] - c[0]) * (gy - c[1])) / den
            l3 = 1 - l1 - l2; m = (l1 >= -.02) & (l2 >= -.02) & (l3 >= -.02)
            if not m.any(): continue
            ys.append(gy[m].astype(int)); xs.append(gx[m].astype(int))
            pts.append(l1[m, None] * p[0] + l2[m, None] * p[1] + l3[m, None] * p[2])
        if not ys: return np.zeros(0, int), np.zeros(0, int), np.zeros((0, 3))
        return np.concatenate(ys), np.concatenate(xs), np.concatenate(pts)

    # ---- erase: what differs from the panel's paint in the box, inpainted from round it
    def zmaz(self, widok, box, prog=.16, rozszerz=3, farba=None, cos=.2, glebia=.08, plasko=False):
        self.ramki.append(("zmaz", widok, box))
        f = self.widoczne(widok, box, cos=cos, glebia=glebia)
        ys, xs, pts = self.teksele(f)
        u, v = WIDOKI[widok][1](pts); u0, u1, v0, v1 = box
        m = (u >= u0) & (u <= u1) & (v >= v0) & (v <= v1)
        ys, xs = ys[m], xs[m]
        if len(ys) == 0: self.log.append("zmaz %s %s: nothing seen" % (widok, box)); return 0
        c = self.img[ys, xs]
        ref = np.array(farba, np.float32) / 255 if farba is not None else np.median(c, 0)
        dev = np.linalg.norm(c - ref, axis=1) > prog
        mask = np.zeros((self.h, self.w), np.uint8); mask[ys[dev], xs[dev]] = 255
        if rozszerz: mask = cv2.dilate(mask, np.ones((2 * rozszerz + 1, 2 * rozszerz + 1), np.uint8))
        region = np.zeros((self.h, self.w), np.uint8); region[ys, xs] = 255
        region = cv2.dilate(region, np.ones((3, 3), np.uint8)); mask &= region
        sel = mask > 0
        if plasko and farba is not None:                       # a big badge: the paint itself (the restoration shades it)
            self.img[sel] = ref
        else:
            src = (np.clip(self.img, 0, 1) * 255).astype(np.uint8)
            out = cv2.inpaint(src, mask, 9, cv2.INPAINT_TELEA)
            self.img[sel] = out[sel].astype(np.float32) / 255
        for k, mp in self.mapy.items():                       # the same texels in the relief and the gloss
            mh, mw = mp.shape[:2]
            mk = mask if (mh, mw) == (self.h, self.w) else cv2.resize(mask, (mw, mh), interpolation=cv2.INTER_NEAREST)
            mk = cv2.dilate(mk, np.ones((3, 3), np.uint8))
            o2 = cv2.inpaint(mp, mk, 9, cv2.INPAINT_TELEA); s2 = mk > 0; mp[s2] = o2[s2]
        self.log.append("zmaz %s %s: %d texels" % (widok, box, int(sel.sum())))
        return int(sel.sum())

    # ---- paint a picture (RGBA, its own metres w x h) centred at (uc, vc) in a view
    def naklej(self, widok, rgba, uc, vc, w, h, nazwa=""):
        box = (uc - w / 2, uc + w / 2, vc - h / 2, vc + h / 2)
        self.ramki.append(("napis", widok, box))
        f = self.widoczne(widok, box)
        ys, xs, pts = self.teksele(f)
        u, v = WIDOKI[widok][1](pts)
        px = (u - box[0]) / w * rgba.shape[1]; py = (box[3] - v) / h * rgba.shape[0]
        m = (px >= 0) & (px < rgba.shape[1] - 1) & (py >= 0) & (py < rgba.shape[0] - 1)
        ys, xs, px, py = ys[m], xs[m], px[m], py[m]
        x0, y0 = np.floor(px).astype(int), np.floor(py).astype(int); fx, fy = (px - x0)[:, None], (py - y0)[:, None]
        s = (rgba[y0, x0] * (1 - fx) * (1 - fy) + rgba[y0, x0 + 1] * fx * (1 - fy) + rgba[y0 + 1, x0] * (1 - fx) * fy + rgba[y0 + 1, x0 + 1] * fx * fy)
        a = s[:, 3:4]
        self.img[ys, xs] = self.img[ys, xs] * (1 - a) + s[:, :3] * a
        # under paint the surface is smooth: the relief there goes flat (a sign-writer's paint, a plate's flat face)
        if "normal" in self.mapy:
            mp = self.mapy["normal"]; mh, mw = mp.shape[:2]
            yy = np.clip((ys * mh) // self.h, 0, mh - 1); xx = np.clip((xs * mw) // self.w, 0, mw - 1); aa = a[:, 0][:, None]
            mp[yy, xx] = (mp[yy, xx] * (1 - aa) + np.array([128, 128, 255]) * aa).astype(np.uint8)
        self.log.append("naklej %s %s at (%.2f, %.2f) %.2fx%.2f m: %d texels" % (widok, nazwa, uc, vc, w, h, int((a > .5).sum())))

    def napis(self, widok, tekst, uc, vc, cap, kolor=(240, 240, 236), szer_max=None, ppm=2400):
        rgba, w, h = tekst_rgba(tekst, cap, kolor, ppm)
        if szer_max and w > szer_max: s = szer_max / w; rgba, w, h = tekst_rgba(tekst, cap * s, kolor, ppm)
        self.naklej(widok, rgba, uc, vc, w, h, tekst)

    def lampa(self, widok, uc, vc, w, h, uklad="czerwona"):
        """a rear lamp painted where Tripo guessed it wrong (it never saw a car's back): a glossy lens with fine ribs -
        uklad "czerwona" red; "zespolona" red with an amber third outside and a white reversing light inside (its side
        by the sign of uc); marked for the night's light map"""
        W, H = max(8, int(w * 600)), max(8, int(h * 600)); a = np.zeros((H, W, 4), np.float32); a[..., 3] = 1
        a[..., :3] = np.array((176, 16, 20), np.float32) / 255
        kawalki = [(0.0, 1.0, "tyl")]
        if uklad == "zespolona":
            na_zewn = uc > 0 if widok == "T" else uc < 0
            third = W // 3; amb = slice(W - third, W) if na_zewn else slice(0, third); bia = slice(0, W // 5) if na_zewn else slice(W - W // 5, W)
            a[:, amb, :3] = np.array((226, 118, 18), np.float32) / 255; a[:, bia, :3] = np.array((214, 214, 208), np.float32) / 255
        rib = (np.arange(H) % max(3, H // 7)) == 0; a[rib, :, :3] *= .72                   # the lens' ribs
        a[:2, :, :3] = a[-2:, :, :3] = a[:, :2, :3] = a[:, -2:, :3] = .08                  # its black rim
        self.naklej(widok, a, uc, vc, w, h, "lampa")
        self.swiatla.append((widok, (uc - w / 2, uc + w / 2, vc - h / 2, vc + h / 2), "tyl"))

    def reflektor(self, widok, uc, vc, r, h=None):
        """a headlamp marked for the night's light map: round (radius r) or rectangular (width r, height h)"""
        if h is None: self.swiatla.append((widok, (uc - r, uc + r, vc - r, vc + r), "przod_o"))
        else: self.swiatla.append((widok, (uc - r / 2, uc + r / 2, vc - h / 2, vc + h / 2), "przod_p"))

    def tablica(self, widok, tekst, uc, vc, w=.52, h=.12, nowa=False):
        """a plate - a flat plate of its own, screwed on where (uc, vc) is in the view (owner 25.11 08:17: painted onto
        Tripo's curved panels the plates bent and broke): nowa False the black plate of 1976-2000, True today's white,
        "zabytkowa" today's yellow"""
        self.tablice3d.append((tekst, "stara" if not nowa else "zabytkowa" if nowa == "zabytkowa" else "nowa", widok, uc, vc, w, h))
        self.ramki.append(("tablica", widok, (uc - w / 2, uc + w / 2, vc - h / 2, vc + h / 2)))

    # ---- the restoration: the paint's own colour where rust, dirt and scratches are, its shading kept
    def odnow(self, farby, tol=.20, rdza=.10, sigma=10, maska_ciala=None):
        a = self.img; L = lin(a) @ np.array([.2126, .7152, .0722], np.float32)
        out = a.copy(); total = 0
        for c in farby:
            c = np.array(c, np.float32) / 255
            d = np.linalg.norm(a - c, axis=2)
            core = (d < tol).astype(np.uint8)                                       # the paint as it is
            near = cv2.dilate(core, np.ones((15, 15), np.uint8)) > 0                # its panels (spots inside them)
            if maska_ciala is not None: near &= maska_ciala
            # what to clean: in the paint's panels, off its colour but not dark trim (panel lines, rubber: under 15 %)
            # nor chrome or glass (low saturation, bright): rust is warm and darker, dirt is greyer and darker
            mx = a.max(2); mn = a.min(2); sat = (mx - mn) / np.maximum(mx, 1e-6)
            # nor grey trim (a bumper, a hub cap: unsaturated and far from the paint) - rust and dirt keep some colour
            brud = near & (d >= tol) & (mx > .15) & ~((sat < .12) & (mx > .55)) & (d < .55) & ((sat > .22) | (d < .28))
            # the shading: the paint's own luminance, smoothed over its panels (normalised convolution)
            w = core.astype(np.float32); Ls = cv2.GaussianBlur(L * w, (0, 0), sigma) / np.maximum(cv2.GaussianBlur(w, (0, 0), sigma), 1e-4)
            Lc = float(lin(c) @ np.array([.2126, .7152, .0722], np.float32))
            czysta = srgb(np.clip(lin(c)[None, None, :] * (Ls / max(Lc, 1e-4))[..., None], 0, 1))
            sel = brud & (cv2.GaussianBlur(w, (0, 0), sigma) > .05)
            out[sel] = czysta[sel]
            # the paint itself: its speckle (fine dirt) evened half-way to the clean colour
            pc = core > 0; out[pc] = a[pc] * .5 + czysta[pc] * .5
            total += int(sel.sum())
        self.img = out
        self.log.append("odnow %d colours: %d texels cleaned" % (len(farby), total))
        return total

    def lakiery(self, pary, tol=14.0, sigma=2.5, maska=None, plamy=None, odcien=None, kontrast=1.0, obszary=False, szare=False):
        """new coats: [(paint sampled (sRGB 0-255), its new colour, roughness, metal), ...] - each texel to the paint
        nearest it (Lab, half-weight lightness) when under tol (soft at the edge), taking the new colour while keeping its
        own light and shade (the luminance against the old paint's, smoothed over a few texels: Tripo's speckle off, the
        panels' shading kept); glossy as a car's paint (the gloss map's roughness, metal for a metallic)"""
        a = self.img; Y = np.array([.2126, .7152, .0722], np.float32)
        lab = cv2.cvtColor(np.clip(a, 0, 1).astype(np.float32), cv2.COLOR_RGB2LAB).reshape(-1, 3)
        D = []
        for zr, *_r in pary:
            q = cv2.cvtColor((np.array(zr, np.float32) / 255).reshape(1, 1, 3), cv2.COLOR_RGB2LAB).reshape(3)
            dd = lab - q; dd[:, 0] *= .5; D.append(np.sqrt((dd ** 2).sum(1)).reshape(a.shape[:2]))
        D = np.stack(D)
        Lz = np.array([max(float(lin(np.array(p[0], np.float32) / 255) @ Y), 1e-4) for p in pary], np.float32)
        if obszary:
            # each paint over its region (the masks): a texel there of any of the paints takes it, its light against the
            # paint it was (Tripo's ragged edge of the other colour made this one)
            zrodlo = D.argmin(0); dmin = D.min(0); Lsrc = Lz[zrodlo]
            kto = np.full(a.shape[:2], -1)
            for i, (zr, cel, ch, me, *mk) in enumerate(pary):
                if mk and mk[0] is not None: kto[mk[0] > 0] = i
        else:
            for i, (zr, cel, ch, me, *mk) in enumerate(pary):                   # a paint only where its mask lets it
                if mk and mk[0] is not None: D[i][mk[0] <= 0] = np.inf
            kto = D.argmin(0); dmin = D.min(0); Lsrc = None
        if odcien:
            # a body Tripo painted in two shades of one hue: every texel of that hue (degrees, saturated) is paint
            hsv = cv2.cvtColor(np.clip(a, 0, 1).astype(np.float32), cv2.COLOR_RGB2HSV)
            h0, h1 = odcien; hh = hsv[..., 0]
            w_odcieniu = ((hh >= h0) & (hh <= h1)) if h0 <= h1 else ((hh >= h0) | (hh <= h1))      # (a red wraps round 0)
            dmin = np.where(w_odcieniu & (hsv[..., 1] > .18) & (hsv[..., 2] > .04), 0.0, dmin)
        if szare:
            # a white coat's region: Tripo's grey dirt and reflections painted on it are the paint too (the trim kept apart)
            hsv = cv2.cvtColor(np.clip(a, 0, 1).astype(np.float32), cv2.COLOR_RGB2HSV)
            biale = [i for i, p in enumerate(pary) if (max(p[0]) - min(p[0])) / max(max(p[0]), 1) < .15]
            dmin = np.where((hsv[..., 1] < .22) & (hsv[..., 2] > .25) & np.isin(kto, biale), 0.0, dmin)
        L = lin(a) @ Y; out = a.copy(); maski = []
        for i, (zr, cel, ch, me, *mk) in enumerate(pary):
            w = (np.clip((tol - dmin) / 5.0, 0, 1) * (kto == i)).astype(np.float32)
            if maska is not None: w *= maska
            if mk and mk[0] is not None: w *= mk[0]
            Lc = Lsrc if Lsrc is not None else Lz[i]
            r = np.clip(L / Lc, .12, 2.4)
            if obszary:
                # what was not this paint in its region (the other colour's ragged edge, Tripo's rust along the line
                # between them): the light of this paint round it
                obcy = (w > 0) & ((zrodlo != i) | (D.min(0) >= tol))
                dobre = (w > .5) & ~obcy
                rf = cv2.GaussianBlur(r * dobre, (0, 0), 8) / np.maximum(cv2.GaussianBlur(dobre.astype(np.float32), (0, 0), 8), 1e-4)
                r = np.where(obcy, np.where(rf > 0, rf, 1.0), r)
            if plamy:
                # Tripo's dark blotches in the paint (patches, not the thin panel gaps): taken in and given the light of
                # the paint round them - a gap of 1-3 texels keeps its shade
                ciemne = (r < plamy)
                if mk and mk[0] is not None: ciemne &= mk[0] > 0
                if maska is not None: ciemne &= maska > 0
                blisko = cv2.dilate((w > .5).astype(np.uint8), np.ones((15, 15), np.uint8)) > 0
                grube = cv2.morphologyEx((ciemne & blisko).astype(np.uint8), cv2.MORPH_OPEN, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (5, 5))) > 0
                dobre = (w > .5) & ~grube
                rf = cv2.GaussianBlur(r * dobre, (0, 0), 8) / np.maximum(cv2.GaussianBlur(dobre.astype(np.float32), (0, 0), 8), 1e-4)
                r = np.where(grube, rf, r); w = np.where(grube, 1.0, w).astype(np.float32)
                self.log.append("lakier %s: %d texels of blotches" % (tuple(zr), int(grube.sum())))
            rs = cv2.GaussianBlur(r * w, (0, 0), sigma) / np.maximum(cv2.GaussianBlur(w, (0, 0), sigma), 1e-4)
            rs = np.where(w > 0, rs, r)
            if kontrast != 1.0:                                     # Tripo's baked mottling halved (the renderer shades)
                rs = np.exp(np.log(np.maximum(rs, 1e-3)) * kontrast)
            nowy = srgb(np.clip(lin(np.array(cel, np.float32) / 255)[None, None, :] * rs[..., None], 0, 1))
            out = out * (1 - w[..., None]) + nowy * w[..., None]
            if "mr" in self.mapy:
                mp = self.mapy["mr"]; ww = cv2.resize(w, (mp.shape[1], mp.shape[0]), interpolation=cv2.INTER_LINEAR)
                mp[..., 1] = np.clip(mp[..., 1] * (1 - ww) + ch * 255 * ww, 0, 255).astype(np.uint8)
                mp[..., 2] = np.clip(mp[..., 2] * (1 - ww) + me * 255 * ww, 0, 255).astype(np.uint8)
            self.log.append("lakier %s -> %s: %d texels" % (tuple(zr), tuple(cel), int((w > .5).sum())))
            maski.append(w)
        self.img = out.astype(np.float32)
        lm = np.max(np.stack(maski), 0) if maski else np.zeros(a.shape[:2], np.float32)
        self.lakier_maska = np.maximum(getattr(self, "lakier_maska", np.zeros_like(lm)), lm)
        return maski

    def obraz(self): return Image.fromarray((np.clip(self.img, 0, 1) * 255 + .5).astype(np.uint8))
    def mapa(self, k): return Image.fromarray(self.mapy[k])


def _font_for(ch):
    for i, c in enumerate(_CMAPS):
        if ord(ch) in c: return i
    return 0


def tekst_rgba(tekst, cap, kolor, ppm=2400):
    """letters (cap height in metres) as an RGBA float image and its size in metres"""
    px = max(8, int(round(cap * ppm / .70)))                  # Montserrat's cap height ~ 0.70 em
    fonts = [ImageFont.truetype(str(f), px) for f in FONT_FILES]
    widths = [fonts[_font_for(ch)].getlength(ch) for ch in tekst]
    tracking = px * .04
    W = int(sum(widths) + tracking * (len(tekst) - 1) + px * .2); H = int(px * 1.35)   # room for Ą Ę's ogonek
    im = Image.new("L", (W, H), 0); d = ImageDraw.Draw(im); x = px * .1
    for ch, wd in zip(tekst, widths):
        d.text((x, px * .12), ch, font=fonts[_font_for(ch)], fill=255); x += wd + tracking
    a = np.asarray(im).astype(np.float32) / 255
    ys, xs = np.nonzero(a > .02)
    a = a[max(ys.min() - 4, 0):ys.max() + 5, max(xs.min() - 4, 0):xs.max() + 5]
    rgba = np.zeros(a.shape + (4,), np.float32); rgba[..., :3] = np.array(kolor, np.float32) / 255; rgba[..., 3] = a
    return rgba, a.shape[1] / ppm, a.shape[0] / ppm


def tablica_obraz(tekst, rodzaj, w, h, ppm=2400):
    """a plate's picture by its kind (stara / nowa / zabytkowa): a PIL image"""
    if rodzaj == "zabytkowa": a = tablica_nowa_rgba(tekst, w, h, ppm=ppm, tlo=(246, 200, 24))
    else: a = (tablica_nowa_rgba if rodzaj == "nowa" else tablica_rgba)(tekst, w, h, ppm=ppm)
    return Image.fromarray((np.clip(np.asarray(a)[..., :3], 0, 1) * 255 + .5).astype(np.uint8)) if isinstance(a, np.ndarray) else a


def tablica_rgba(tekst, w=.52, h=.12, ppm=2400):
    """a plate of 1976-2000: black, a thin white rim, white characters"""
    W, H = int(w * ppm), int(h * ppm)
    im = Image.new("RGBA", (W, H), (14, 14, 16, 255)); d = ImageDraw.Draw(im)
    r = int(.006 * ppm); d.rectangle([r, r, W - r - 1, H - r - 1], outline=(236, 236, 232, 255), width=max(2, int(.004 * ppm)))
    t, tw, th = tekst_rgba(tekst, h * .62, (236, 236, 232), ppm)
    s = min(1.0, (w * .9) / tw); t = np.asarray(Image.fromarray((t * 255).astype(np.uint8)).resize((max(1, int(t.shape[1] * s)), max(1, int(t.shape[0] * s))), Image.LANCZOS)).astype(np.float32) / 255
    base = np.asarray(im).astype(np.float32) / 255
    y0 = (H - t.shape[0]) // 2; x0 = (W - t.shape[1]) // 2
    reg = base[y0:y0 + t.shape[0], x0:x0 + t.shape[1]]; a = t[..., 3:4]
    reg[..., :3] = reg[..., :3] * (1 - a) + t[..., :3] * a
    return base


def tablica_nowa_rgba(tekst, w=.52, h=.114, ppm=2400, tlo=(240, 240, 236)):
    """a plate of today (since 2006): white, black characters, the blue band at the left with PL (no flag drawn); a
    historic vehicle's the same on yellow (tlo)"""
    W, H = int(w * ppm), int(h * ppm)
    im = Image.new("RGBA", (W, H), tuple(tlo) + (255,)); d = ImageDraw.Draw(im)
    d.rectangle([0, 0, W - 1, H - 1], outline=(20, 20, 22, 255), width=max(2, int(.004 * ppm)))
    bw = int(.040 * ppm); d.rectangle([0, 0, bw, H - 1], fill=(0, 51, 153, 255))
    pl, pw, ph = tekst_rgba("PL", h * .22, (240, 240, 236), ppm)
    base = np.asarray(im).astype(np.float32) / 255
    y0 = int(H * .62); x0 = max(0, (bw - pl.shape[1]) // 2)
    reg = base[y0:y0 + pl.shape[0], x0:x0 + pl.shape[1]]; a = pl[:reg.shape[0], :reg.shape[1], 3:4]
    reg[..., :3] = reg[..., :3] * (1 - a) + pl[:reg.shape[0], :reg.shape[1], :3] * a
    t, tw, th = tekst_rgba(tekst, h * .58, (16, 16, 18), ppm)
    s = min(1.0, (w * .84) / tw)
    t = np.asarray(Image.fromarray((t * 255).astype(np.uint8)).resize((max(1, int(t.shape[1] * s)), max(1, int(t.shape[0] * s))), Image.LANCZOS)).astype(np.float32) / 255
    y0 = (H - t.shape[0]) // 2; x0 = bw + (W - bw - t.shape[1]) // 2
    reg = base[y0:y0 + t.shape[0], x0:x0 + t.shape[1]]; a = t[..., 3:4]
    reg[..., :3] = reg[..., :3] * (1 - a) + t[..., :3] * a
    return base
