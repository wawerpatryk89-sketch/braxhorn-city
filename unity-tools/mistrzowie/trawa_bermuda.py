"""Owner 23.11 17:59-18:38 ("trawa ... nie płaska", "długa, nie jest krótka", "to jest dobra trawa"): the region's lawn
grows real 3D grass. Its card is made here from Poly Haven Grass Bermuda 01 (CC0, the photo-scanned plants of a Bermuda
lawn, Cynodon dactylon): the original 4K Diffuse and Alpha atlas (checked against the provider's size and MD5) is cut
into its plants, the green ones are set side by side on one strip with their roots on its bottom edge (three rows, the
back ones darker), stretched upward a little (a long lawn, not a fresh-cut one), and the colour under the transparent
parts is filled from the plants so the mipmaps keep no dark fringe.

Output (BraxChampionsTrawa3DBuild imports it): Textures/grass_bermuda_01_karta.png (RGBA, 2048 x 1024) and
trawa-karta.json (the card's mean linear colour, for the tint that matches it to the flat lawn under it).
Only numpy and Pillow (the build runner has no scipy); deterministic (fixed seed, no clock)."""
from pathlib import Path
import hashlib, json, random, sys, urllib.request, time
import numpy as np
from PIL import Image

SLUG = 'grass_bermuda_01'
MAPS = (('Diffuse', 'diff'), ('Alpha', 'alpha'))
W, H = 2048, 1024
# the three rows: plants, brightness (depth), height on the card (fraction of H)
ROWS = ((52, .62, .55, .80), (60, .85, .62, .90), (44, 1.0, .68, 1.0))
STRETCH = .78          # the plant's width against its height (< 1: longer, thinner blades)
GREEN_MIN = 1.0        # mean G/R of a plant: below it the dry brown leaves of the atlas (not on the lawn)
SEED = 23112026
# the shader darkens the card toward its roots (the blades shade each other): albedo x lerp(NASADA, 1, saturate(v x NASADA_PAS)),
# v = the card's height 0..1; the tint is measured with the same darkening (ChampionsTrawaKarta.shader: _Nasada, _NasadaPas)
NASADA, NASADA_PAS = .55, 1.6


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
    """The original 4K jpg maps; a cached copy is kept only when it still matches the provider's size and MD5."""
    meta = json.loads(get('https://api.polyhaven.com/files/' + SLUG))
    out = {}
    for key, kind in MAPS:
        item = meta.get(key, {}).get('4k', {}).get('jpg')
        if not isinstance(item, dict) or not item.get('url'):
            raise RuntimeError(f'Poly Haven {SLUG}: missing {key} 4k/jpg; available {sorted(meta)}')
        path = Path(dest) / f'{SLUG}_{kind}_4k.jpg'
        data = path.read_bytes() if path.exists() else None
        ok = lambda b: b is not None and len(b) == item['size'] and hashlib.md5(b).hexdigest() == item['md5']
        if not ok(data):
            data = get(item['url'])
        if not ok(data):
            raise RuntimeError(f'Poly Haven {SLUG}: invalid download size/checksum for {key} 4k/jpg')
        path.write_bytes(data)
        out[kind] = path
    return out


def label(mask):
    """Connected parts of a boolean image (4-neighbour), without scipy: (labels, count)."""
    h, w = mask.shape
    lab = np.zeros((h, w), np.int32)
    n = 0
    ys, xs = np.nonzero(mask)
    for y0, x0 in zip(ys.tolist(), xs.tolist()):
        if lab[y0, x0]:
            continue
        n += 1
        lab[y0, x0] = n
        stack = [(y0, x0)]
        while stack:
            y, x = stack.pop()
            for yy, xx in ((y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1)):
                if 0 <= yy < h and 0 <= xx < w and mask[yy, xx] and not lab[yy, xx]:
                    lab[yy, xx] = n
                    stack.append((yy, xx))
    return lab, n


def plants(diff, alpha):
    """The atlas' green plants: (rgb float array, coverage 0..1), each cut to its own box."""
    d = np.asarray(Image.open(diff).convert('RGB')).astype(np.float32)
    a = np.asarray(Image.open(alpha).convert('L')).astype(np.float32)
    f = 4                                                          # labelled at a quarter of the size (1K), cut at 4K
    small = a[:a.shape[0] // f * f, :a.shape[1] // f * f].reshape(a.shape[0] // f, f, a.shape[1] // f, f).mean(axis=(1, 3))
    lab, n = label(small > 40)
    big = np.kron(lab, np.ones((f, f), np.int32))
    big = np.pad(big, ((0, a.shape[0] - big.shape[0]), (0, a.shape[1] - big.shape[1])), mode='edge')
    out = []
    for i in range(1, n + 1):
        ys, xs = np.nonzero(lab == i)
        if len(ys) == 0:
            continue
        y0, y1, x0, x1 = ys.min() * f, (ys.max() + 1) * f, xs.min() * f, (xs.max() + 1) * f
        h, w = y1 - y0, x1 - x0
        if h * w < 10000 or h > w * 6:                             # specks; the bare stems
            continue
        m = (big[y0:y1, x0:x1] == i) * (a[y0:y1, x0:x1] / 255.0)
        c = d[y0:y1, x0:x1]
        solid = m > .5
        mean = c[solid].mean(axis=0)
        if mean[1] / max(mean[0], 1.0) < GREEN_MIN:                # the dry, brown leaves
            continue
        out.append((c, m))
    return out


def fill(rgb, al):
    """Push-pull: the colour under the transparent pixels from the nearest plants (no dark fringe in the mipmaps)."""
    levels = [(rgb * al[..., None], al.copy())]
    while min(levels[-1][1].shape) > 1:
        c, w = levels[-1]
        h2, w2 = c.shape[0] // 2 * 2, c.shape[1] // 2 * 2
        c = c[:h2, :w2].reshape(h2 // 2, 2, w2 // 2, 2, 3).sum(axis=(1, 3))
        w = w[:h2, :w2].reshape(h2 // 2, 2, w2 // 2, 2).sum(axis=(1, 3))
        levels.append((c, w))
    c, w = levels[-1]
    col = c / np.maximum(w, 1e-6)[..., None]
    for c, w in reversed(levels[:-1]):
        up = np.repeat(np.repeat(col, 2, axis=0), 2, axis=1)
        up = np.pad(up, ((0, c.shape[0] - up.shape[0]), (0, c.shape[1] - up.shape[1]), (0, 0)), mode='edge')
        own = c / np.maximum(w, 1e-6)[..., None]
        k = np.clip(w, 0, 1)[..., None]
        col = own * k + up * (1 - k)
    return col


def karta(diff, alpha, out_png, out_json):
    sprites = plants(diff, alpha)
    if len(sprites) < 6:
        raise RuntimeError(f'{SLUG}: only {len(sprites)} green plants in the atlas')
    rgb = np.zeros((H, W, 3), np.float32)
    al = np.zeros((H, W), np.float32)
    rnd = random.Random(SEED)

    def put(sp, x, height, ciemn, flip):
        c, m = sp
        nh = max(4, int(height))
        nw = max(4, int(m.shape[1] * height / m.shape[0] * STRETCH))
        ci = np.asarray(Image.fromarray(c.astype(np.uint8)).resize((nw, nh), Image.LANCZOS)).astype(np.float32)
        mi = np.asarray(Image.fromarray((m * 255).astype(np.uint8)).resize((nw, nh), Image.LANCZOS)).astype(np.float32) / 255
        if flip:
            ci, mi = ci[:, ::-1], mi[:, ::-1]
        y1 = H - rnd.randint(0, 10)
        y0 = y1 - nh
        x0 = int(x - nw / 2)
        sy0, dy0 = max(0, -y0), max(0, y0)
        sx0, dx0 = max(0, -x0), max(0, x0)
        dx1, dy1 = min(W, x0 + nw), y1
        if dx1 <= dx0 or dy1 <= dy0:
            return
        sc = ci[sy0:sy0 + dy1 - dy0, sx0:sx0 + dx1 - dx0] * ciemn
        sm = mi[sy0:sy0 + dy1 - dy0, sx0:sx0 + dx1 - dx0]
        rgb[dy0:dy1, dx0:dx1] = rgb[dy0:dy1, dx0:dx1] * (1 - sm[..., None]) + sc * sm[..., None]
        al[dy0:dy1, dx0:dx1] = np.maximum(al[dy0:dy1, dx0:dx1], sm)

    for cnt, ciemn, hmin, hmax in ROWS:
        for _ in range(cnt):
            sp = sprites[rnd.randrange(len(sprites))]
            put(sp, rnd.uniform(30, W - 30), rnd.uniform(hmin, hmax) * H * .96, ciemn, rnd.random() < .5)
    rgb = fill(rgb, al)
    img = np.dstack([np.clip(rgb, 0, 255), np.clip(al * 255, 0, 255)]).astype(np.uint8)
    Image.fromarray(img, 'RGBA').save(out_png, optimize=True)
    solid = al > .5
    lin_all = (rgb / 255.0) ** 2.2
    lin = lin_all[solid].mean(axis=0)
    v = 1.0 - (np.arange(H) + .5) / H
    ao = NASADA + (1.0 - NASADA) * np.clip(v * NASADA_PAS, 0, 1)
    lin_ao = (lin_all * ao[:, None, None])[solid].mean(axis=0)
    info = dict(source=f'Poly Haven {SLUG} (CC0) 4K Diffuse + Alpha', plants=len(sprites), width=W, height=H,
                coverage=round(float(solid.mean()), 4), linear=[round(float(x), 5) for x in lin],
                linear_nasada=[round(float(x), 5) for x in lin_ao], nasada=NASADA, nasada_pas=NASADA_PAS)
    Path(out_json).write_text(json.dumps(info, indent=2))
    return info


def main(dest, source_dir):
    maps = fetch(dest)
    info = karta(maps['diff'], maps['alpha'], Path(dest) / f'{SLUG}_karta.png', Path(source_dir) / 'trawa-karta.json')
    print('BRAX_MISTRZOWIE_TRAWA_KARTA plants=%d coverage=%.3f linear=%s withRoots=%s' % (info['plants'], info['coverage'], info['linear'],
          info['linear_nasada']), flush=True)
    return info


if __name__ == '__main__':
    if len(sys.argv) == 5:                                        # offline: diff alpha out.png out.json
        print(karta(*sys.argv[1:]))
    else:
        root = Path(__file__).resolve().parents[2] / 'unity-city/Assets/Source/Mistrzowie'
        main(root / 'Textures', root)
