#!/usr/bin/env python3
"""THE QUAD TRACK'S FOREST: the baked cards (wypal_drzewa.js, rendered 2x over) packed into one atlas.

    python3 atlas_lasu.py <cards dir> <out png> <tree> [<tree> ...]

Each tree: <tree>_0.png and <tree>_90.png (1024 x 2048) and <tree>.json. The atlas is 8 columns x 2 rows of 512 x 1024
cells, each tree two cells side by side (its 0 and 90 degree views) in the order given (16 cells: eight trees).
  - down 2x with the colour premultiplied by alpha (no dark fringes);
  - the alpha scaled so the cut-out (0.5) covers as much of the cell as the full picture did (the needles stay as thick as
    they are: a plain average thins them out - what Unity's own "preserve coverage" does for the mip levels);
  - the colour pushed out under the transparent parts (push-pull) so no mip level of the cut-out blends in black.
Prints the C# table of the cards (BraxChampionsLasKladow.Karty)."""
import json
import os
import sys

import numpy as np
from PIL import Image

KOL, WIER, CW, CH = 8, 2, 512, 1024


def zmniejsz(rgba):
    a = rgba[..., 3:4]
    pre = np.concatenate([rgba[..., :3] * a, a], -1)
    h, w = pre.shape[:2]
    pre = pre.reshape(h // 2, 2, w // 2, 2, 4).mean((1, 3))
    a2 = pre[..., 3:4]
    rgb = np.where(a2 > 1e-6, pre[..., :3] / np.maximum(a2, 1e-6), 0.)
    return np.concatenate([rgb, a2], -1)


def pokrycie(alpha_duze, alpha):
    """The factor for alpha after which (alpha > .5) covers what (alpha_duze > .5) did."""
    cel = (alpha_duze > .5).mean()
    lo, hi = .5, 4.
    for _ in range(30):
        m = (lo + hi) / 2
        if (np.minimum(alpha * m, 1.) > .5).mean() < cel:
            lo = m
        else:
            hi = m
    return (lo + hi) / 2, cel


def wypelnij(rgba):
    """Push-pull: the transparent texels' colour from the coarser averages of the opaque ones."""
    poziomy = [rgba]
    while poziomy[-1].shape[0] > 2 and poziomy[-1].shape[1] > 2:
        poziomy.append(zmniejsz(poziomy[-1]))
    for i in range(len(poziomy) - 2, -1, -1):          # coarse to fine: each level already filled from the one above
        drob, grub = poziomy[i], poziomy[i + 1]
        up = grub.repeat(2, 0).repeat(2, 1)[:drob.shape[0], :drob.shape[1]]
        pusty = drob[..., 3:4] < 1. / 255
        drob[..., :3] = np.where(pusty, up[..., :3], drob[..., :3])
    return poziomy[0]


def main(src, out, drzewa):
    assert len(drzewa) * 2 <= KOL * WIER
    atlas = np.zeros((CH * WIER, CW * KOL, 4), np.float32)
    wiersze = []
    for t, nazwa in enumerate(drzewa):
        meta = json.load(open(os.path.join(src, nazwa + ".json")))
        for v, deg in enumerate((0, 90)):
            duze = np.asarray(Image.open(os.path.join(src, "%s_%d.png" % (nazwa, deg))).convert("RGBA"), np.float32) / 255.
            assert duze.shape[:2] == (CH * 2, CW * 2), (nazwa, duze.shape)
            male = zmniejsz(duze)
            k, cel = pokrycie(duze[..., 3], male[..., 3])
            male[..., 3] = np.minimum(male[..., 3] * k, 1.)
            c = t * 2 + v
            x, y = (c % KOL) * CW, (c // KOL) * CH
            atlas[y:y + CH, x:x + CW] = male
            print("  %-12s %3d deg: cell %2d, coverage %.3f, alpha x%.2f" % (nazwa, deg, c, cel, k), file=sys.stderr)
        wiersze.append((nazwa, meta, t * 2))
    atlas = wypelnij(atlas)
    img = Image.fromarray(np.clip(atlas * 255. + .5, 0, 255).astype(np.uint8), "RGBA")
    os.makedirs(os.path.dirname(out) or ".", exist_ok=True)
    img.save(out, optimize=True)
    print("atlas", out, img.size, os.path.getsize(out) // 1024, "KB", file=sys.stderr)
    # the C# table: name, the tree's height, the frame's height (metres; the frame is half as wide), its foot (minY), cell
    for nazwa, meta, c in wiersze:
        print('        new Karta("%s", %.2ff, %.2ff, %.2ff, %d),' % (nazwa, meta["height"], meta["frameH"], meta["minY"], c))


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2], sys.argv[3:])
