#!/usr/bin/env python3
"""BRAXHORN — garbled lettering off the owner's Tripo models (owner 12.11.2026 13:41 about the heavy bag: "ten napis ... trzeba
wyciąć, ale żeby nie było śladu po nim"; 13:58 "przejrzeć te modele, czy wszystko się zgadza, żeby nie było lipy").

Tripo draws the big BRAXHORN of a jersey right, but every SMALL print comes out as scrambled letters ("DRAXRUNV" on the
basketball, "BRAJHUH" on a glove's cuff, the badge of a track jacket). The prints are not geometry: they are painted in
the pack's shared texture atlas, on the model's own UV islands. This tool takes them off without a trace:

  * a print is named the way it is SEEN on the review sheet (unity-tools/sklep/przeglad_modeli.js): the model, the view
    (przod / tyl / lewy / prawy / gora / spod) and a rectangle in that view's tile, 0..1 from its top left corner;
  * the model's texels that are seen in that rectangle (in front in that view, not hidden behind another part) are the
    print's patch; inside the patch the letters are the texels far from the patch's own ground colour (its median);
  * the letters (grown a little: their soft edges) are filled in from the ground round them AS SEEN in the view - Tripo's
    atlas is thousands of scraps, neighbours on the model are not neighbours in the texture - so a shaded fold or a
    seam under the print continues; the ground's own grain is put back;
  * the normal and metal/roughness maps get the same patch filled in (an embossed print is in them too).

No triangle and no UV is touched; no other model of the atlas is touched (a texel belongs to one model's island).

usage: napisy_precz.py <decrypted pack folder> <spec.json>
spec: {"pack": "sklep_walka_01", "prints": [{"model": "kask_bokserski_czerwony", "view": "przod", "rect": [.30, .28, .72, .40]}, ...]}
optional per print: "prog": colour distance that makes a texel a letter (default: found between ground and letters),
"rosnie": cells of the seen picture the letters grow by (default 2), "caly": true = the whole rectangle is filled in from
the ground round it (a print with a shadow or a badge under it), "tylko": "jasne" / "ciemne" = only white / only black
texels are letters (a gold stripe in the rectangle stays), "kula": true = the model's vertices in the rectangle are put back
on the sphere of the body round them (a print pressed into a round body: the kettlebell); "kula": "normalne" = only their
normals are taken from that sphere (the shading evened out round the patch, no vertex moved). The result is looked at on a new review sheet (przeglad_modeli.js).
Only on a decrypted copy of a pack; the pack is rebuilt afterwards (tar -> openssl cms, manifest.json hashes). Needs numpy, pillow."""
import json
import struct
import sys
from pathlib import Path

import numpy as np
from PIL import Image

# the review sheet's views: the camera's direction (from the model), its right and its up
VIEWS = {'przod': ((0, 0, 1), (1, 0, 0), (0, 1, 0)), 'tyl': ((0, 0, -1), (-1, 0, 0), (0, 1, 0)),
         'lewy': ((-1, 0, 0), (0, 0, 1), (0, 1, 0)), 'prawy': ((1, 0, 0), (0, 0, -1), (0, 1, 0)),
         'gora': ((0, 1, 0), (1, 0, 0), (0, 0, -1)), 'spod': ((0, -1, 0), (1, 0, 0), (0, 0, 1))}
Z = 512          # the depth picture of the view (what is in front)


def load(path):
    raw = open(path, 'rb').read(); n = struct.unpack_from('<I', raw, 12)[0]; j = json.loads(raw[20:20 + n]); blob = raw[20 + n + 8:]

    def acc(i):
        a = j['accessors'][i]; bv = j['bufferViews'][a['bufferView']]; k = {'SCALAR': 1, 'VEC2': 2, 'VEC3': 3}[a['type']]
        dt = {5126: '<f4', 5125: '<u4', 5123: '<u2'}[a['componentType']]
        return np.frombuffer(blob, dt, a['count'] * k, bv.get('byteOffset', 0) + a.get('byteOffset', 0)).reshape(a['count'], k)
    pr = j['meshes'][0]['primitives'][0]
    return (acc(pr['attributes']['POSITION']).astype(np.float64), acc(pr['attributes']['TEXCOORD_0']).astype(np.float64),
            acc(pr['indices']).astype(np.int64).reshape(-1, 3), j)


def raster(tri, W, H, margin=0.):
    """the pixels of a 2D triangle (pixel centres) with their barycentric weights; margin lets it bleed past its edges"""
    x0, y0 = np.floor(tri.min(0)).astype(int) - 1; x1, y1 = np.ceil(tri.max(0)).astype(int) + 1
    x0, y0, x1, y1 = max(x0, 0), max(y0, 0), min(x1, W - 1), min(y1, H - 1)
    if x1 < x0 or y1 < y0: return None
    d = (tri[1, 1] - tri[2, 1]) * (tri[0, 0] - tri[2, 0]) + (tri[2, 0] - tri[1, 0]) * (tri[0, 1] - tri[2, 1])
    if abs(d) < 1e-12: return None
    gx, gy = np.meshgrid(np.arange(x0, x1 + 1) + .5, np.arange(y0, y1 + 1) + .5)
    w0 = ((tri[1, 1] - tri[2, 1]) * (gx - tri[2, 0]) + (tri[2, 0] - tri[1, 0]) * (gy - tri[2, 1])) / d
    w1 = ((tri[2, 1] - tri[0, 1]) * (gx - tri[2, 0]) + (tri[0, 0] - tri[2, 0]) * (gy - tri[2, 1])) / d
    w2 = 1 - w0 - w1
    return (slice(y0, y1 + 1), slice(x0, x1 + 1)), w0, w1, w2, (w0 >= -margin) & (w1 >= -margin) & (w2 >= -margin)


def view_of(P, I, view):
    """every vertex in the review sheet's tile of `view` (u, v, depth toward the camera), the tile's half size and the
    depth picture of the model in that view (what is in front)"""
    d, rt, up = (np.array(v, float) for v in VIEWS[view]); lo, hi = P.min(0), P.max(0); c = (lo + hi) / 2; r = (hi - lo).max() * .58
    S = np.c_[.5 + (P - c) @ rt / (2 * r), .5 - (P - c) @ up / (2 * r), (P - c) @ d]
    zb = np.full((Z, Z), -1e9)
    for a, b, cc in I:
        got = raster(S[[a, b, cc], :2] * Z, Z, Z)
        if got is None: continue
        sl, w0, w1, w2, ins = got; dep = w0 * S[a, 2] + w1 * S[b, 2] + w2 * S[cc, 2]
        zb[sl] = np.where(ins & (dep > zb[sl]), dep, zb[sl])
    return S, r, zb


def patch(P, UV, I, view, rect, W, H):
    """the texels seen in `rect` of the review sheet's `view` tile: their mask and where each one is seen (tile u, v)"""
    S, r, zb = view_of(P, I, view)
    eps = .012 * r; m = np.zeros((H, W), bool); SU = np.zeros((H, W), np.float32); SV = np.zeros((H, W), np.float32); u0, v0, u1, v1 = rect
    for a, b, cc in I:
        s3 = S[[a, b, cc]]
        if s3[:, 0].max() < u0 or s3[:, 0].min() > u1 or s3[:, 1].max() < v0 or s3[:, 1].min() > v1: continue
        got = raster(UV[[a, b, cc]] * [W, H], W, H, .12)
        if got is None: continue
        sl, w0, w1, w2, ins = got
        su = w0 * s3[0, 0] + w1 * s3[1, 0] + w2 * s3[2, 0]; sv = w0 * s3[0, 1] + w1 * s3[1, 1] + w2 * s3[2, 1]; sd = w0 * s3[0, 2] + w1 * s3[1, 2] + w2 * s3[2, 2]
        zi = zb[np.clip((sv * Z).astype(int), 0, Z - 1), np.clip((su * Z).astype(int), 0, Z - 1)]
        k = ins & (su >= u0) & (su <= u1) & (sv >= v0) & (sv <= v1) & (sd >= zi - eps)
        m[sl] |= k; SU[sl] = np.where(k, su, SU[sl]); SV[sl] = np.where(k, sv, SV[sl])
    return m, SU, SV


def grow(m, n):
    for _ in range(n):
        g = m.copy(); g[1:] |= m[:-1]; g[:-1] |= m[1:]; g[:, 1:] |= m[:, :-1]; g[:, :-1] |= m[:, 1:]; m = g
    return m


class Seen:
    """The print as it is SEEN: a small picture of the rectangle (with a margin round it) in the view. Tripo's atlas is
    thousands of scraps of a few texels each, so neighbours on the model are not neighbours in the texture: the letters are
    marked, grown and filled in this picture, where the surface is whole, and every texel then takes its own place's colour."""

    def __init__(self, rect, cells, margin):
        u0, v0, u1, v1 = rect; self.u0, self.v0 = u0 - margin, v0 - margin; w, h = u1 - u0 + 2 * margin, v1 - v0 + 2 * margin
        self.step = np.sqrt(w * h / max(cells, 64)); self.nx, self.ny = int(np.ceil(w / self.step)) + 1, int(np.ceil(h / self.step)) + 1

    def cell(self, su, sv):
        return np.clip(((sv - self.v0) / self.step).astype(int), 0, self.ny - 1), np.clip(((su - self.u0) / self.step).astype(int), 0, self.nx - 1)

    def marks(self, su, sv, n):
        """cells that hold a letter's texel, grown by n cells"""
        g = np.zeros((self.ny, self.nx), bool); g[self.cell(su, sv)] = True
        return grow(g, n)

    def picture(self, su, sv, colours, hole):
        """the ground's picture: the mean colour of the ground texels of every cell; the letters' cells and the cells no texel
        fell into flow in from the cells round them"""
        cy, cx = self.cell(su, sv); keep = ~hole[cy, cx]; acc = np.zeros((self.ny, self.nx, colours.shape[1])); cnt = np.zeros((self.ny, self.nx))
        np.add.at(acc, (cy[keep], cx[keep]), colours[keep]); np.add.at(cnt, (cy[keep], cx[keep]), 1)
        known = cnt > 0; med = np.median(colours[keep], axis=0) if keep.any() else np.median(colours, axis=0)
        img = np.where(known[..., None], acc / np.maximum(cnt, 1)[..., None], med)
        # a logo, a stripe or a panel of another colour next to the print is not ground: it feeds nothing (and is not touched)
        far = np.linalg.norm(img - med, axis=2); spread = np.median(far[known]) if known.any() else 0.
        known &= far <= max(60., 4. * spread)
        for _ in range(4 * max(self.nx, self.ny) + 40):
            s = np.zeros_like(img); n = np.zeros(known.shape)
            for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                sh = np.roll(img, (dy, dx), (0, 1)); ok = np.ones(known.shape)
                if dy == 1: ok[0] = 0
                if dy == -1: ok[-1] = 0
                if dx == 1: ok[:, 0] = 0
                if dx == -1: ok[:, -1] = 0
                s += sh * ok[..., None]; n += ok
            img = np.where(known[..., None], img, s / n[..., None])
        return img, keep


def round_off(path, view, rect, normals_only=False):
    """a print pressed into a round body (the kettlebell's dented plate): the vertices seen in the rectangle are put back on
    the sphere that fits the body round it; their normals follow"""
    raw = open(path, 'rb').read(); n = struct.unpack_from('<I', raw, 12)[0]; j = json.loads(raw[20:20 + n]); blob = bytearray(raw[20 + n + 8:])
    pr = j['meshes'][0]['primitives'][0]

    def where(i):
        a = j['accessors'][i]; bv = j['bufferViews'][a['bufferView']]; return bv.get('byteOffset', 0) + a.get('byteOffset', 0), a
    po, pa = where(pr['attributes']['POSITION']); no, _ = where(pr['attributes']['NORMAL']); io, ia = where(pr['indices'])
    P = np.frombuffer(bytes(blob), '<f4', pa['count'] * 3, po).reshape(-1, 3).astype(np.float64).copy()
    N = np.frombuffer(bytes(blob), '<f4', pa['count'] * 3, no).reshape(-1, 3).astype(np.float64).copy()
    I = np.frombuffer(bytes(blob), '<u2' if ia['componentType'] == 5123 else '<u4', ia['count'], io).reshape(-1, 3).astype(np.int64)
    S, r, zb = view_of(P, I, view); u0, v0, u1, v1 = rect; mg = .5 * min(u1 - u0, v1 - v0)
    front = S[:, 2] >= zb[np.clip((S[:, 1] * Z).astype(int), 0, Z - 1), np.clip((S[:, 0] * Z).astype(int), 0, Z - 1)] - .03 * r
    inside = front & (S[:, 0] >= u0) & (S[:, 0] <= u1) & (S[:, 1] >= v0) & (S[:, 1] <= v1)
    ring = front & ~inside & (S[:, 0] >= u0 - mg) & (S[:, 0] <= u1 + mg) & (S[:, 1] >= v0 - mg) & (S[:, 1] <= v1 + mg)
    if ring.sum() < 12 or not inside.any(): return 0
    A = np.c_[2 * P[ring], np.ones(ring.sum())]; sol = np.linalg.lstsq(A, (P[ring] ** 2).sum(1), rcond=None)[0]      # |p|^2 = 2 p.c + (R^2 - |c|^2)
    c = sol[:3]; R = np.sqrt(sol[3] + c @ c); d = P[inside] - c; d /= np.linalg.norm(d, axis=1)[:, None]
    if not normals_only: P[inside] = c + d * R
    N[inside] = d
    blob[po:po + P.size * 4] = P.astype('<f4').tobytes(); blob[no:no + N.size * 4] = N.astype('<f4').tobytes()
    js = json.dumps(j, separators=(',', ':')).encode(); js += b' ' * (-len(js) % 4)
    open(path, 'wb').write(struct.pack('<4sII', b'glTF', 2, 28 + len(js) + len(blob)) + struct.pack('<I4s', len(js), b'JSON') + js
                           + struct.pack('<I4s', len(blob), b'BIN\0') + bytes(blob))
    return int(inside.sum())


def to_file(t, f):
    if f.suffix == '.jpg': Image.fromarray(t).save(f, quality=95, subsampling=0)
    else: Image.fromarray(t).save(f)


def main():
    args = sys.argv[1:]
    base, spec = Path(args[0]), json.loads(Path(args[1]).read_text(encoding='utf-8')); tex = {}
    for k, pr in enumerate(spec['prints']):
        P, UV, I, j = load(base / (pr['model'] + '.glb')); mat = j['materials'][0]; imgs = [im['uri'] for im in j['images']]
        names = {'kolor': imgs[j['textures'][mat['pbrMetallicRoughness']['baseColorTexture']['index']]['source']]}
        if 'normalTexture' in mat: names['normal'] = imgs[j['textures'][mat['normalTexture']['index']]['source']]
        if 'metallicRoughnessTexture' in mat['pbrMetallicRoughness']: names['mr'] = imgs[j['textures'][mat['pbrMetallicRoughness']['metallicRoughnessTexture']['index']]['source']]
        for n in names.values():
            if n not in tex: tex[n] = np.asarray(Image.open(base / n).convert('RGB')).copy()
        col = tex[names['kolor']]; H, W = col.shape[:2]; rect = pr['rect']; margin = .35 * min(rect[2] - rect[0], rect[3] - rect[1])
        wide = [rect[0] - margin, rect[1] - margin, rect[2] + margin, rect[3] + margin]
        m, SU, SV = patch(P, UV, I, pr['view'], wide, W, H)
        inner = m & (SU >= rect[0]) & (SU <= rect[2]) & (SV >= rect[1]) & (SV <= rect[3])
        if not inner.any(): print(f'  {pr["model"]} {pr["view"]} {rect}: NOTHING SEEN THERE'); continue
        med = np.median(col[inner].astype(np.float64), axis=0); dist = np.linalg.norm(col.astype(np.float64) - med, axis=2)
        if pr.get('caly'): letters = inner.copy(); thr = 0.
        else:
            dm = dist[inner]; thr = pr.get('prog')
            if thr is None:                                   # Otsu between the ground and the letters, never under the ground's own spread
                hist, edges = np.histogram(dm, 64); mids = (edges[:-1] + edges[1:]) / 2; best = (-1, 0)
                for t in range(1, 64):
                    w0, w1 = hist[:t].sum(), hist[t:].sum()
                    if w0 == 0 or w1 == 0: continue
                    m0, m1 = (hist[:t] * mids[:t]).sum() / w0, (hist[t:] * mids[t:]).sum() / w1; v = w0 * w1 * (m0 - m1) ** 2
                    if v > best[0]: best = (v, edges[t])
                thr = max(best[1], 3.5 * np.median(dm) + 6)
            letters = inner & (dist > thr)
            c = col.astype(np.int32)
            if pr.get('tylko') == 'jasne': letters &= c.min(axis=2) > 140                    # white letters only (a gold stripe in the rectangle stays)
            if pr.get('tylko') == 'ciemne': letters &= c.max(axis=2) < 70
        seen = Seen(rect, inner.sum() / 2.0, margin); hole = seen.marks(SU[letters], SV[letters], pr.get('rosnie', 0 if pr.get('caly') else 2))
        filled = 0
        for kind, name in names.items():
            t = tex[name]; h, w = t.shape[:2]
            mk, su, sv = (m, SU, SV) if (h, w) == (H, W) else patch(P, UV, I, pr['view'], wide, w, h)
            ys, xs = np.nonzero(mk); tu, tv = su[ys, xs], sv[ys, xs]; cols = t[ys, xs].astype(np.float64)
            img, ground = seen.picture(tu, tv, cols, hole); cy, cx = seen.cell(tu, tv); fill = ~ground; new = img[cy, cx]
            if kind == 'kolor':
                res = (cols - new)[ground]; lim = 2.5 * np.median(np.abs(res)) + 1; res = res[np.all(np.abs(res) <= lim, axis=1)]      # the ground's own grain
                if len(res): new[fill] += res[np.random.default_rng(7 + k).integers(0, len(res), int(fill.sum()))] * .8
                filled = int(fill.sum())
            t[ys[fill], xs[fill]] = np.clip(new[fill], 0, 255).astype(np.uint8)
            if pr.get('caly'):
                # slivers: triangles thinner than a texel never cover a texel's centre, yet the texel under them is what
                # they show (a bright scratch left on the filled patch) - the texels under their corners are filled too
                Sv, rr, zb = view_of(P, I, pr['view']); tri = Sv[I]; cu, cv, cd = tri[:, :, 0], tri[:, :, 1], tri[:, :, 2]
                ok = (cu.min(1) >= rect[0]) & (cu.max(1) <= rect[2]) & (cv.min(1) >= rect[1]) & (cv.max(1) <= rect[3])
                zi = zb[np.clip((cv.mean(1) * Z).astype(int), 0, Z - 1), np.clip((cu.mean(1) * Z).astype(int), 0, Z - 1)]
                ok &= cd.mean(1) >= zi - .012 * rr
                for corner in range(3):
                    uvp = UV[I[ok, corner]]; tx = np.clip((uvp[:, 0] * w).astype(int), 0, w - 1); ty = np.clip((uvp[:, 1] * h).astype(int), 0, h - 1)
                    cy2, cx2 = seen.cell(cu[ok, corner], cv[ok, corner]); t[ty, tx] = np.clip(img[cy2, cx2], 0, 255).astype(np.uint8)
        if pr.get('kula'): print(f'    {round_off(base / (pr["model"] + ".glb"), pr["view"], rect, pr["kula"] == "normalne")} vertices put back on the body\'s sphere')
        print(f'  {pr["model"]:44s} {pr["view"]:6s} seen {int(inner.sum()):6d} texels, letters {int(letters.sum()):6d} (threshold {thr:5.1f}), filled {filled:6d}')
    for n, t in tex.items(): to_file(t, base / n)
    print('saved', sorted(tex))


if __name__ == '__main__':
    main()
