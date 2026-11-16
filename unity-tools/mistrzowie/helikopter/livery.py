"""BRAXHORN livery for the EC135: the ADAC paint (yellow, its letters, the orange bands, the EU flags, the coats of arms,
the red crosses) found in the texture and painted piano black (its baked shading kept); then, projected from the sides
onto the texels where the fuselage really is (positions / normals rasterised into the UV), the gold cheatline, BRAXHORN
big on the rear fuselage and tail boom (16.11 22:38) and the owner's bull emblem (his coin) on the rear cabin and the fin -
reading right on both sides."""
import io, numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy import ndimage

def rasterise(P, N, UV, I, W, H):
    """position / normal of the surface at every texel (nearest triangle cover), coverage mask"""
    pos = np.zeros((H, W, 3), np.float32); nrm = np.zeros((H, W, 3), np.float32); cov = np.zeros((H, W), bool)
    T = I.reshape(-1, 3); uv = UV * [W, H]
    for t in T:
        a, b, c = uv[t[0]], uv[t[1]], uv[t[2]]
        x0, x1 = int(max(0, np.floor(min(a[0], b[0], c[0])))), int(min(W - 1, np.ceil(max(a[0], b[0], c[0]))))
        y0, y1 = int(max(0, np.floor(min(a[1], b[1], c[1])))), int(min(H - 1, np.ceil(max(a[1], b[1], c[1]))))
        if x1 < x0 or y1 < y0: continue
        xs, ys = np.meshgrid(np.arange(x0, x1 + 1) + .5, np.arange(y0, y1 + 1) + .5)
        d = (b[1] - c[1]) * (a[0] - c[0]) + (c[0] - b[0]) * (a[1] - c[1])
        if abs(d) < 1e-12: continue
        l0 = ((b[1] - c[1]) * (xs - c[0]) + (c[0] - b[0]) * (ys - c[1])) / d
        l1 = ((c[1] - a[1]) * (xs - c[0]) + (a[0] - c[0]) * (ys - c[1])) / d
        l2 = 1 - l0 - l1; e = -.6 / max(1.0, (x1 - x0 + y1 - y0))      # a hair of overlap: no seams
        m = (l0 >= e) & (l1 >= e) & (l2 >= e)
        if not m.any(): continue
        yy, xx = np.nonzero(m); yy += y0; xx += x0; w = np.stack([l0[m], l1[m], l2[m]], 1)
        pos[yy, xx] = w @ P[t]; n = w @ N[t]; nrm[yy, xx] = n / np.maximum(1e-9, np.linalg.norm(n, axis=1, keepdims=True)); cov[yy, xx] = True
    return pos, nrm, cov

def paint_mask(rgb, cov):
    r, g, b = [rgb[..., i].astype(np.float32) for i in range(3)]
    yellow = (r > 120) & (g > 100) & (b < 120) & (r - b > 70) & (g - b > 50)
    dark_yellow = (r > 45) & (g > 38) & (b < .55 * g) & (r - b > 30) & (np.abs(r - g) < .35 * r)       # the paint in its shade
    orange = (r > 120) & (g > 20) & (g < 150) & (b < 90) & (r - g > 45)
    blue = (b > 70) & (r < 90) & (b - r > 35)
    red = (r > 120) & (g < 80) & (b < 80)
    base = yellow | dark_yellow | orange | blue | red
    filled = ndimage.binary_fill_holes(base) & cov
    # every texel of the old paint (also where no triangle shows it: the mip levels bleed), its anti-aliased rim too
    return yellow, (ndimage.binary_dilation(filled, iterations=1) & cov) | ndimage.binary_dilation(base, iterations=3)

def shading(rgb, yellow, mask):
    L = rgb.astype(np.float32) @ [.299, .587, .114]
    ref = np.percentile(L[yellow], 90) if yellow.any() else 200.0
    s = np.clip(L / ref, .30, 1.05) * yellow
    k = 9; num = ndimage.uniform_filter(s, k); den = ndimage.uniform_filter(yellow.astype(np.float32), k)
    smooth = np.where(den > .05, num / np.maximum(den, 1e-6), 1.0)
    return np.where(yellow, s, np.clip(smooth, .30, 1.05)) * mask + (~mask) * 1.0

def text_mask(text, font, cap_px):
    f = ImageFont.truetype(font, int(cap_px * 1.38))
    l, t, r, btm = f.getbbox(text); im = Image.new('L', (r - l + 40, btm - t + 40), 0)
    ImageDraw.Draw(im).text((20 - l, 20 - t), text, fill=255, font=f)
    return np.asarray(im, np.float32) / 255.0

def sample(img, u, v):
    """bilinear sample of an (h, w[, c]) image at u (0..1 left to right) and v (0..1 bottom to top); 0 outside"""
    h, w = img.shape[:2]; x = u * w - .5; y = (1 - v) * h - .5
    inside = (u >= 0) & (u <= 1) & (v >= 0) & (v <= 1)
    x0 = np.clip(np.floor(x).astype(int), 0, w - 1); y0 = np.clip(np.floor(y).astype(int), 0, h - 1)
    x1 = np.clip(x0 + 1, 0, w - 1); y1 = np.clip(y0 + 1, 0, h - 1); fx = np.clip(x - x0, 0, 1); fy = np.clip(y - y0, 0, 1)
    if img.ndim == 3: fx = fx[..., None]; fy = fy[..., None]; ins = inside[..., None]
    else: ins = inside
    o = (img[y0, x0] * (1 - fx) + img[y0, x1] * fx) * (1 - fy) + (img[y1, x0] * (1 - fx) + img[y1, x1] * fx) * fy
    return o * ins

def side_uv(pos, nrm, zc, yc, w, h, side_min=.35):
    """decal coordinates of a side projection centred at (z, y) = (zc, yc), w x h metres; per side reading right
    (glTF +x side: the viewer's right is -z; -x side: +z); 0..1 inside, the side's texels only"""
    z, y, nx = pos[..., 2], pos[..., 1], nrm[..., 0]
    right = np.where(nx > 0, -(z - zc), (z - zc))
    u = right / w + .5; v = (y - yc) / h + .5
    ok = np.abs(nx) > side_min
    return np.where(ok, u, -1), np.where(ok, v, -1)

# 16.11 22:38 (owner: "słabo widoczny ... napis"): a brighter satin gold (vinyl, half metallic) - the full metal gold
# mirrored the sky and read dark
GOLD_HI, GOLD_LO, PAINT = np.array([255, 228, 146.]), np.array([206, 160, 64.]), np.array([13, 13, 15.])

def gold(v):
    t = np.clip(v, 0, 1)[..., None]; return GOLD_LO * (1 - t) + GOLD_HI * t

def cheatline(z):
    zs = [3.4, 2.0, -1.0, -2.2, -3.0, -4.6]; ys = [1.18, 1.00, 1.00, 1.62, 1.97, 2.03]
    return np.interp(-z, [-v for v in zs], ys)

def paint(rgb, mr, pos, nrm, cov, emblem, font, part):
    yellow, mask = paint_mask(rgb, cov)
    sh = shading(rgb, yellow, mask)[..., None]
    out = rgb.astype(np.float32).copy()
    out[mask] = (PAINT * sh)[mask]
    gm = np.zeros(mask.shape, np.float32); gcol = np.zeros(rgb.shape, np.float32)
    side = cov & (np.abs(nrm[..., 0]) > .45)
    # the cheatline: a 7 cm gold band and a 2.5 cm line 9 cm over it, from the nose sweeping up to the tail boom's root
    yc = cheatline(pos[..., 2]); dy = pos[..., 1] - yc
    band = side & (pos[..., 2] > -2.30) & (np.abs(pos[..., 0]) < .95) & ((np.abs(dy) < .035) | (np.abs(dy - .09) < .0125))
    gm = np.maximum(gm, band.astype(np.float32)); gcol[band] = gold(.5 + dy[band] * 4)
    # BRAXHORN big on both sides of the rear fuselage and the tail boom (as POLIZEI / ADAC on real EC135s): 30 cm
    # capitals over 2.6 m, centred on the boom's side (its flat part y 1.76..2.25)
    tm = text_mask('BRAXHORN', font, 400); tw = 2.60; th = tw * tm.shape[0] / tm.shape[1]
    u, v = side_uv(pos, nrm, -3.15, 2.00, tw, th, .22)
    a = sample(tm, u, v) * cov * ((pos[..., 2] < -1.80) & (pos[..., 2] > -4.50) & (np.abs(pos[..., 0]) < .80))
    gm = np.maximum(gm, a); gcol = np.where(a[..., None] > 0, gold(v), gcol)
    # the owner's emblem (his coin) on the rear cabin and, smaller, on the fin
    for zc, ycc, d in ((-0.95, 1.85, .66), (-6.62, 3.22, .40)):
        u, v = side_uv(pos, nrm, zc, ycc, d, d, .30)
        e = sample(emblem, u, v) * cov[..., None]
        ea = e[..., 3] / 255.0
        gm2 = ea > 0; gcol = np.where(gm2[..., None], e[..., :3], gcol); gm = np.maximum(gm, ea)
    ga = gm[..., None]
    out = out * (1 - ga) + gcol * ga
    # the paint glossy (roughness .22, dielectric), the gold satin (roughness .37, metallic .43)
    mr = mr.astype(np.float32).copy()
    if mr.shape[:2] != mask.shape:
        mr = np.asarray(Image.fromarray(mr.astype(np.uint8)).resize(mask.shape[::-1], Image.BILINEAR), np.float32)
    mr[..., 1] = np.where(mask, 56, mr[..., 1]); mr[..., 2] = np.where(mask, 40, mr[..., 2])
    mr[..., 1] = mr[..., 1] * (1 - gm) + 95 * gm; mr[..., 2] = mr[..., 2] * (1 - gm) + 110 * gm
    print(part, 'paint texels', int(mask.sum()), 'gold texels', int((gm > .5).sum()))
    return np.clip(out, 0, 255).astype(np.uint8), np.clip(mr, 0, 255).astype(np.uint8)


def recolour(rgb):
    """the cabin lining: the old paint's yellow seams dark grey"""
    r, g, b = [rgb[..., i].astype(np.float32) for i in range(3)]
    y = (r > 45) & (g > 38) & (b < .6 * g) & (r - b > 25)
    y = ndimage.binary_dilation(y, iterations=1)
    out = rgb.copy(); L = (rgb.astype(np.float32) @ [.299, .587, .114]) * .35
    out[y] = np.stack([L, L, L * 1.05], -1)[y].clip(0, 255).astype(np.uint8)
    return out
