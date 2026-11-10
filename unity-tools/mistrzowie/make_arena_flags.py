#!/usr/bin/env python3
"""Owner 10.11 08:26: "a możesz jakieś flagi zrobić, żeby powiewały? przed areną, jakieś takie premium".

Two tall vertical arena banners (1.5 x 6.0 m, swallowtail) in one 1024 x 2048 atlas (two 512 x 2048 cells):
  cell 0 - deep bordeaux velvet, cell 1 - black; both with a double gold border, the GOLD BULL from the owner's own
  entrance mural (MURAL_WEJSCIE_CHAMPION_FIGHTER_6144x2048.png, background keyed out), "CHAMPION ARENA" in Cinzel
  (SIL OFL, the trophy plaques' font), five gold stars and B R A X H O R N set vertically.
The alpha channel cuts the swallowtail. Output is committed (the mural source is a release asset, not in git).

usage: make_arena_flags.py <MURAL_WEJSCIE_...png> <Cinzel.ttf> [out.png]
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "unity-city/Assets/Source/Mistrzowie/ArenaFlags/arena_flags.png"
W, H = 512, 2048
GOLD_HI, GOLD_LO = np.array([250, 222, 140]), np.array([176, 128, 44])


def gold_fill(mask):
    """Vertical gold gradient through a mask (L image) -> RGBA."""
    h = mask.size[1]
    t = np.linspace(0, 1, h)[:, None, None]
    band = .5 + .5 * np.cos(t * np.pi * 3.0)                     # two highlights over the height = metallic
    rgb = (GOLD_LO + (GOLD_HI - GOLD_LO) * band).astype(np.uint8)
    rgb = np.broadcast_to(rgb, (h, mask.size[0], 3))
    return Image.fromarray(np.dstack([rgb, np.asarray(mask)]).astype(np.uint8), "RGBA")


def bull(mural):
    src = Image.open(mural).convert("RGB").crop((3070, 0, 4290, 760))
    a = np.asarray(src).astype(np.float32) / 255
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    gold = np.clip((np.minimum(r, g) - b - .05) * 7, 0, 1)        # warm metal, not grey stone nor red paint
    white = np.clip((np.min(a, axis=2) - .72) * 8, 0, 1)           # the glowing eyes
    alpha = np.maximum(gold, white) * np.clip((np.mean(a, axis=2) - .12) * 6, 0, 1)
    img = Image.fromarray(np.dstack([np.asarray(src), (alpha * 255).astype(np.uint8)]), "RGBA")
    return img.filter(ImageFilter.SMOOTH)


def text(draw_img, s, font, cy, spacing=0):
    m = Image.new("L", (W, H), 0)
    d = ImageDraw.Draw(m)
    tw = d.textlength(s, font=font) + spacing * (len(s) - 1)
    x = (W - tw) / 2
    for ch in s:
        d.text((x, cy), ch, font=font, fill=255, anchor="lm")
        x += d.textlength(ch, font=font) + spacing
    shadow = m.filter(ImageFilter.GaussianBlur(4))
    draw_img.alpha_composite(Image.merge("RGBA", [Image.new("L", (W, H), 0)] * 3 + [shadow.point(lambda v: v * .8)]))
    draw_img.alpha_composite(gold_fill(m))


def star(d, cx, cy, r):
    pts = []
    for i in range(10):
        a = -np.pi / 2 + i * np.pi / 5
        rr = r if i % 2 == 0 else r * .42
        pts.append((cx + rr * np.cos(a), cy + rr * np.sin(a)))
    d.polygon(pts, fill=255)


def banner(field_top, field_bottom, emblem, cinzel):
    t = np.linspace(0, 1, H)[:, None]
    x = np.linspace(-1, 1, W)[None, :]
    shade = (1 - .35 * x ** 2) * (1 - .15 * t)                     # velvet: darker at the edges and toward the tail
    col = (np.array(field_top)[None, None, :] * (1 - t[..., None]) + np.array(field_bottom)[None, None, :] * t[..., None])
    rgb = np.clip(col * shade[..., None], 0, 255).astype(np.uint8)
    img = Image.fromarray(rgb, "RGB").convert("RGBA")
    # swallowtail: notch 230 px deep in the middle of the bottom edge
    notch = 230
    outline = [(0, 0), (W, 0), (W, H), (W / 2, H - notch), (0, H)]
    shape = Image.new("L", (W, H), 0)
    ImageDraw.Draw(shape).polygon(outline, fill=255)
    # double gold border following the outline (inset 16 and 34 px)
    border = Image.new("L", (W, H), 0)
    bd = ImageDraw.Draw(border)
    for inset, width in ((16, 9), (34, 4)):
        p = [(inset, 70), (W - inset, 70), (W - inset, H - inset * 1.6), (W / 2, H - notch - inset * 1.1), (inset, H - inset * 1.6)]
        bd.line(p + [p[0]], fill=255, width=width, joint="curve")
    # rod pocket at the top: dark band with a gold stitch line
    pocket = Image.new("L", (W, H), 0)
    ImageDraw.Draw(pocket).rectangle((0, 0, W, 58), fill=255)
    img.alpha_composite(Image.merge("RGBA", [Image.new("L", (W, H), 18)] * 3 + [pocket.point(lambda v: v * .85)]))
    stitch = Image.new("L", (W, H), 0)
    sd = ImageDraw.Draw(stitch)
    for xx in range(10, W - 10, 22):
        sd.line((xx, 62, xx + 12, 62), fill=255, width=3)
    img.alpha_composite(gold_fill(border))
    img.alpha_composite(gold_fill(stitch))
    # bull emblem
    e = emblem.copy()
    e.thumbnail((430, 430), Image.LANCZOS)
    img.alpha_composite(e, ((W - e.width) // 2, 150))
    f_small = ImageFont.truetype(str(cinzel), 62)
    f_big = ImageFont.truetype(str(cinzel), 104)
    text(img, "CHAMPION", f_small, 520, spacing=4)
    text(img, "ARENA", f_big, 615, spacing=6)
    stars = Image.new("L", (W, H), 0)
    sd = ImageDraw.Draw(stars)
    for i in range(5):
        star(sd, W / 2 + (i - 2) * 56, 712, 20)
    rule = ImageDraw.Draw(stars)
    rule.line((70, 768, W / 2 - 26, 768), fill=255, width=4)
    rule.line((W / 2 + 26, 768, W - 70, 768), fill=255, width=4)
    rule.polygon([(W / 2, 750), (W / 2 + 16, 768), (W / 2, 786), (W / 2 - 16, 768)], fill=255)
    img.alpha_composite(gold_fill(stars))
    f_v = ImageFont.truetype(str(cinzel), 92)
    for i, ch in enumerate("BRAXHORN"):
        text(img, ch, f_v, 880 + i * 112)
    img.putalpha(shape)
    return img


def main():
    mural, cinzel = Path(sys.argv[1]), Path(sys.argv[2])
    out = Path(sys.argv[3]) if len(sys.argv) > 3 else OUT
    emblem = bull(mural)
    atlas = Image.new("RGBA", (1024, 2048), (0, 0, 0, 0))
    atlas.paste(banner((122, 12, 22), (70, 6, 12), emblem, cinzel), (0, 0))
    atlas.paste(banner((20, 20, 22), (6, 6, 7), emblem, cinzel), (512, 0))
    out.parent.mkdir(parents=True, exist_ok=True)
    atlas.save(out, optimize=True)
    print("A1_FLAGS_ATLAS", out, atlas.size)


if __name__ == "__main__":
    main()
