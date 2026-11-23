"""Owner 23.11 19:12-19:36 ("chcę taki piasek i fale na morzu i woda jak prawdziwa", "fale niech się wylewają na plażę
jakbyś jest normalnie", "te ścieżki usuń z plaży całkowicie", "ten piasek tu lepiej wyglądał ... taki nierówny"):

1. the beach's sand (ChampionsPiasek): Poly Haven Aerial Sand (CC0, 15 m of trampled beach sand - footprints everywhere),
   the original 4K Diffuse and GL normal (checked against the provider's size and MD5), without its tyre tracks: the map is
   put together again from patches of only its track-free areas (picked by eye on the provider's map, CZYSTE below, as
   fractions of a 2048 map), on a periodic grid (it still tiles), each patch from a random place, blended so the grain keeps
   its contrast (variance-preserving blend); the large-scale shading of the aerial photo is taken out first, so no patch
   shows as a darker square. Out: Textures/plaza_piasek_diff_4k.jpg, plaza_piasek_nor_4k.jpg, plaza-piasek.json (its linear
   mean, for the tint);

2. the noise the sea shader (ChampionsMorze) draws its foam and the light on the bed from. One tileable 512 x 512 map,
three channels:
  R - lace: the bright thin edges of a cellular pattern (Worley F2 - F1): the foam's lace on the water and the caustics
      (two of them crossed, the network of light on the sand under shallow water);
  G - a smooth noise (band-limited, periodic): where each wave breaks harder, the wiggle of the crests, the wet sand's edge;
  B - round patches (Worley F1): the foam's clumps.
Only numpy and Pillow (the build runner has no scipy); deterministic (fixed seeds)."""
from pathlib import Path
import hashlib, json, sys, time, urllib.request
import numpy as np
from PIL import Image

SAND = 'aerial_sand'
SAND_MAPS = (('Diffuse', 'diff'), ('nor_gl', 'nor_gl'))
# the track-free areas of the 2048 map (x0, y0, x1, y1 in its pixels; scaled to the map's real size)
CZYSTE = ((1408, 1280, 1920, 2048), (256, 1280, 640, 2048), (0, 256, 256, 1024), (128, 0, 640, 256), (1536, 256, 1760, 640))
PATCH, STEP = 192, 128            # on a 2048 map (x2 on the 4K one): 1.4 m patches every 0.94 m of the 15 m map

N = 512


def worley(cells, seed):
    rng = np.random.default_rng(seed)
    pts = rng.random((cells, cells, 2))
    y, x = np.mgrid[0:N, 0:N] / N * cells
    cx, cy = np.floor(x).astype(int), np.floor(y).astype(int)
    f1 = np.full((N, N), 9.0); f2 = np.full((N, N), 9.0)
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            ix, iy = (cx + dx) % cells, (cy + dy) % cells            # periodic: the map tiles without a seam
            px, py = cx + dx + pts[iy, ix, 0], cy + dy + pts[iy, ix, 1]
            d = np.hypot(px - x, py - y)
            f2 = np.where(d < f1, f1, np.minimum(f2, d)); f1 = np.minimum(f1, d)
    return f1, f2


def gladki(seed, cut):
    rng = np.random.default_rng(seed)
    F = np.fft.fft2(rng.standard_normal((N, N)))
    ky, kx = np.fft.fftfreq(N)[:, None], np.fft.fftfreq(N)[None, :]
    k = np.hypot(kx, ky)
    F *= np.exp(-(k / cut) ** 2) * (k > 0) / np.maximum(k, 1 / N) ** .5
    v = np.real(np.fft.ifft2(F))
    return (v - v.min()) / (v.max() - v.min())


def get(url):
    for attempt in range(4):
        try:
            req = urllib.request.Request(url, headers={'User-Agent': 'BRAXHORN-UnityAssetBuild/1.0'})
            with urllib.request.urlopen(req, timeout=120) as r:
                return r.read()
        except Exception:
            if attempt == 3:
                raise
            time.sleep(2 * (attempt + 1))


def fetch(dest):
    """Aerial Sand's original 4K jpg maps; a cached copy is kept only while it matches the provider's size and MD5."""
    meta = json.loads(get('https://api.polyhaven.com/files/' + SAND))
    out = {}
    for key, kind in SAND_MAPS:
        item = meta.get(key, {}).get('4k', {}).get('jpg')
        if not isinstance(item, dict) or not item.get('url'):
            raise RuntimeError(f'Poly Haven {SAND}: missing {key} 4k/jpg; available {sorted(meta)}')
        path = Path(dest) / f'{SAND}_{kind}_4k.jpg'
        data = path.read_bytes() if path.exists() else None
        ok = lambda b: b is not None and len(b) == item['size'] and hashlib.md5(b).hexdigest() == item['md5']
        if not ok(data):
            data = get(item['url'])
        if not ok(data):
            raise RuntimeError(f'Poly Haven {SAND}: invalid download size/checksum for {key} 4k/jpg')
        path.write_bytes(data)
        out[kind] = path
    return out


def box(a, r, axis):
    """a periodic box mean of width 2r+1 along an axis"""
    n = a.shape[axis]
    ap = np.concatenate([np.take(a, range(n - r - 1, n), axis=axis), a, np.take(a, range(0, r), axis=axis)], axis=axis)
    c = np.cumsum(ap, axis=axis, dtype=np.float64)
    return ((np.take(c, range(2 * r + 1, n + 2 * r + 1), axis=axis) - np.take(c, range(0, n), axis=axis)) / (2 * r + 1)).astype(np.float32)


def blur(a, r):
    for _ in range(3):
        a = box(box(a, r, 0), r, 1)
    return a


def zszyj(srcs, n_out, patch, step, seed=23112026):
    """srcs: float maps [H, W, C] of one size; each put together again (n_out x n_out) from the same track-free patches"""
    k = srcs[0].shape[0] / 2048.0
    rects = [tuple(int(v * k) for v in r) for r in CZYSTE]
    rng = np.random.default_rng(seed)
    u = (np.arange(patch) + .5) / patch
    win = np.sin(np.pi * u) ** 2
    w2 = np.outer(win, win)[..., None].astype(np.float32)
    outs = [np.zeros((n_out, n_out, s.shape[2]), np.float32) for s in srcs]
    wsq = np.zeros((n_out, n_out, 1), np.float32)
    means = [s.reshape(-1, s.shape[2]).mean(0) for s in srcs]
    n, off = n_out // step, (patch - step) // 2
    for gy in range(n):
        for gx in range(n):
            x0, y0, x1, y1 = rects[rng.integers(len(rects))]
            sx, sy = rng.integers(x0, x1 - patch + 1), rng.integers(y0, y1 - patch + 1)
            ys, xs = (np.arange(patch) + gy * step - off) % n_out, (np.arange(patch) + gx * step - off) % n_out
            for o, s, m in zip(outs, srcs, means):
                o[np.ix_(ys, xs)] += w2 * (s[sy:sy + patch, sx:sx + patch] - m)
            wsq[np.ix_(ys, xs)] += w2 ** 2
    return [m + o / np.sqrt(np.maximum(wsq, 1e-9)) for o, m in zip(outs, means)]


def piasek_z(diff, nor, out_diff, out_nor, out_json):
    d = np.asarray(Image.open(diff).convert('RGB')).astype(np.float32) / 255
    nm = np.asarray(Image.open(nor).convert('RGB')).astype(np.float32) / 255
    if d.shape != nm.shape or d.shape[0] != d.shape[1]:
        raise RuntimeError(f'{SAND}: maps of different or non-square sizes {d.shape} {nm.shape}')
    lo = blur(d, max(1, d.shape[0] // 42))                         # the aerial photo's large-scale shading out
    d = d - lo + lo.reshape(-1, 3).mean(0)
    k = d.shape[0] // 2048
    od, on = zszyj([d, nm], d.shape[0], PATCH * k, STEP * k)
    v = on * 2 - 1
    v[..., 2] = np.sqrt(np.clip(1 - v[..., 0] ** 2 - v[..., 1] ** 2, 0, 1))
    on = v * .5 + .5
    od = np.clip(od, 0, 1)
    Image.fromarray((od * 255).astype(np.uint8)).save(out_diff, quality=95)
    Image.fromarray((np.clip(on, 0, 1) * 255).astype(np.uint8)).save(out_nor, quality=95)
    lin = np.where(od <= .04045, od / 12.92, ((od + .055) / 1.055) ** 2.4).reshape(-1, 3).mean(0)
    info = dict(source=f'Poly Haven {SAND} (CC0) 4K Diffuse + GL normal, tyre tracks left out', size=int(d.shape[0]),
                patches=len(CZYSTE), linear=[round(float(x), 5) for x in lin])
    Path(out_json).write_text(json.dumps(info, indent=2))
    return info


def szum(out):
    f1, f2 = worley(18, 1)
    lace = 1 - np.clip((f2 - f1) / .16, 0, 1) ** .6
    b1, _ = worley(7, 2)
    blobs = np.clip(1 - b1 / .75, 0, 1)
    smooth = gladki(3, .02)
    img = np.dstack([lace, smooth, blobs])
    Image.fromarray((img * 255).astype(np.uint8), 'RGB').save(out)
    return dict(lace=round(float(lace.mean()), 4), smooth=round(float(smooth.mean()), 4), blobs=round(float(blobs.mean()), 4))


def main(dest, source_dir):
    info = szum(Path(dest) / 'morze_szum.png')
    print('BRAX_MISTRZOWIE_MORZE_SZUM ' + ' '.join(f'{k}={v}' for k, v in info.items()), flush=True)
    maps = fetch(dest)
    sand = piasek_z(maps['diff'], maps['nor_gl'], Path(dest) / 'plaza_piasek_diff_4k.jpg', Path(dest) / 'plaza_piasek_nor_4k.jpg',
                    Path(source_dir) / 'plaza-piasek.json')
    print('BRAX_MISTRZOWIE_PLAZA_PIASEK size=%d linear=%s (tyre tracks left out)' % (sand['size'], sand['linear']), flush=True)
    return info, sand


if __name__ == '__main__':
    if len(sys.argv) == 6:                                        # offline: diff nor out_diff out_nor out_json
        print(piasek_z(*sys.argv[1:]))
    elif len(sys.argv) == 2:
        print(szum(sys.argv[1]))
    else:
        root = Path(__file__).resolve().parents[2] / 'unity-city/Assets/Source/Mistrzowie'
        main(root / 'Textures', root)
