#!/usr/bin/env python3
"""Engraved gold name plates for the A1 lobby trophies (owner 9.11:
"Napis grawer na tym co stoi puchar, piękny napis").

Typography only, no generated imagery: Cinzel (Roman inscription capitals,
SIL Open Font License) from npm @fontsource/cinzel@5.1.0, woff2 -> ttf with
fontTools. Each plate: polished gold, engraved double border, deep engraved
letters with a light lower lip (cut-in look). Atlas 4 x 2 cells of 512 x 224
(plate .16 x .07 m), 2048 x 512, sRGB PNG committed with this script.

Usage: make_trophy_plaques.py <dir with cinzel-latin-700-normal.woff2 and -600->
"""
from pathlib import Path
import io
import sys

from fontTools.ttLib import TTFont
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "unity-city/Assets/Source/Mistrzowie/ArenaTrophies/trophy_plaques_atlas.png"
CELL_W, CELL_H, COLS, ROWS = 512, 224, 4, 2
# (main line, second line) - index = atlas cell, used by BraxChampionsArenaTrophies.
PLATES = [
    ("BRAXHORN", "CHAMPION OF CHAMPIONS"),
    ("MMA", "WORLD CHAMPION 2026"),
    ("BOXING", "HEAVYWEIGHT CHAMPION"),
    ("KICKBOXING", "GRAND PRIX WINNER"),
    ("FIGHT NIGHT", "KNOCKOUT OF THE NIGHT"),
    ("MISTRZ", "CHAMPION ARENA 2026"),
    ("BRAXHORN CUP", "FIRST PLACE"),
    ("CHAMPION", "UNDEFEATED"),
]


def font(path, size):
    tt = TTFont(str(path))
    tt.flavor = None
    buf = io.BytesIO()
    tt.save(buf)
    return ImageFont.truetype(io.BytesIO(buf.getvalue()), size)


def fit(draw, text, path, start, max_w):
    size = start
    while size > 10:
        f = font(path, size)
        if draw.textlength(text, font=f) <= max_w:
            return f
        size -= 2
    return font(path, 10)


def engrave(img, xy, text, f, anchor="mm"):
    # Light lower lip, then the dark cut: reads as letters cut INTO the gold.
    lip = Image.new("L", img.size, 0)
    ImageDraw.Draw(lip).text((xy[0], xy[1] + 2), text, font=f, fill=255, anchor=anchor)
    cut = Image.new("L", img.size, 0)
    ImageDraw.Draw(cut).text(xy, text, font=f, fill=255, anchor=anchor)
    img.paste((255, 236, 170), (0, 0), lip.filter(ImageFilter.GaussianBlur(.6)))
    img.paste((46, 30, 10), (0, 0), cut)


def plate(main, second, bold, semi):
    w, h = CELL_W, CELL_H
    img = Image.new("RGB", (w, h))
    px = img.load()
    for y in range(h):          # polished gold: soft vertical sheen + fine brushing
        t = y / (h - 1)
        sheen = 1.0 - .55 * abs(t - .38)
        for x in range(w):
            brush = 1.0 + .035 * (((x * 7 + y * 3) % 11) / 10 - .5)
            k = sheen * brush
            px[x, y] = (min(255, int(222 * k)), min(255, int(172 * k)), min(255, int(74 * k)))
    d = ImageDraw.Draw(img)
    for inset, wid in ((10, 3), (20, 1)):   # engraved double border
        d.rectangle([inset, inset, w - 1 - inset, h - 1 - inset], outline=(70, 46, 14), width=wid)
        d.rectangle([inset + wid, inset + wid, w - 1 - inset - wid, h - 1 - inset - wid], outline=(255, 232, 160), width=1)
    f1 = fit(d, main, bold, 74, w - 90)
    f2 = fit(d, second, semi, 30, w - 90)
    engrave(img, (w / 2, h * .40), main, f1)
    cx, cy = w / 2, h * .615             # ornament: rule - diamond - rule
    d.line([cx - 120, cy, cx - 14, cy], fill=(70, 46, 14), width=2)
    d.line([cx + 14, cy, cx + 120, cy], fill=(70, 46, 14), width=2)
    d.polygon([(cx, cy - 7), (cx + 7, cy), (cx, cy + 7), (cx - 7, cy)], fill=(70, 46, 14))
    engrave(img, (w / 2, h * .78), second, f2)
    return img


def main():
    if len(sys.argv) != 2:
        raise SystemExit(__doc__)
    src = Path(sys.argv[1])
    bold, semi = src / "cinzel-latin-700-normal.woff2", src / "cinzel-latin-600-normal.woff2"
    atlas = Image.new("RGB", (CELL_W * COLS, CELL_H * ROWS + (512 - CELL_H * ROWS)), (30, 22, 10))
    for i, (a, b) in enumerate(PLATES):
        atlas.paste(plate(a, b, bold, semi), ((i % COLS) * CELL_W, (i // COLS) * CELL_H))
    OUT.parent.mkdir(parents=True, exist_ok=True)
    atlas.save(OUT, optimize=True)
    print("BRAX_A1_TROPHY_PLAQUES", OUT.relative_to(ROOT), atlas.size, len(PLATES))


if __name__ == "__main__":
    main()
