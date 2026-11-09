#!/usr/bin/env python3
"""Cut each verified owner mural (6144 x 2048) into three 2048 x 2048 tiles for BraxChampionsArenaMuralPrint.

Owner 9.11 23:43: "Dwóch murali kolejnych nie ma, nadal nie ma ... mają być wszystkie 3." APK 35 applied all three, yet
the two hall murals were black on the phone: one 6144 x 2048 picture is padded by Unity to 8192 x 2048 for ASTC and is
sampled four or five mip levels down across a 70 m wall. 6144 = 3 x 2048, so every mural becomes three ordinary
square power-of-two textures. A tile is a lossless crop: no pixel of the owner's picture is changed, resized or
recompressed (PNG, RGB). The originals stay where install_arena_murals_hd.py staged and verified them.

Runs after install_arena_murals_hd.py; never starts Unity or a build. Tiles are generated, never committed.
"""
import hashlib
import json
import sys
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
DEST = ROOT / "unity-city/Assets/Source/Mistrzowie/ArenaMuralsHD"
SOURCES = {
    "MURAL_WEJSCIE_CHAMPION_FIGHTER_6144x2048.png": "5d30aa995bb0da043a34c282fb452743f1b04b9f4584abf2be925ea29a461a73",
    "MURAL_LEWA_ZAWODNICY_6144x2048.png": "0805d4170b696236dba7a37c89fc4415ee21a87b316c1d58734bac212cb11a2c",
    "MURAL_PRAWA_PAS_6144x2048.png": "b134858b5ba16dc85089642eb1f7432ef5c76e036a8d313805947923b7661304",
}
TILE = 2048
Image.MAX_IMAGE_PIXELS = 6144 * 2048 * 2


def tile_name(source, index):
    return source[:-4] + f"_TILE{index}.png"


def split(folder=DEST):
    folder = Path(folder)
    report = []
    for name, sha in SOURCES.items():
        source = folder / name
        if not source.is_file():
            raise FileNotFoundError("Owner mural not staged: " + name)
        if hashlib.sha256(source.read_bytes()).hexdigest() != sha:
            raise ValueError("Owner mural is not the approved original: " + name)
        with Image.open(source) as picture:
            if picture.size != (3 * TILE, TILE):
                raise ValueError(f"Owner mural must be 6144x2048, got {picture.size}: {name}")
            rgb = picture.convert("RGB")
            for index in range(3):
                tile = rgb.crop((index * TILE, 0, (index + 1) * TILE, TILE))
                target = folder / tile_name(name, index)
                partial = target.with_suffix(".png.partial")
                tile.save(partial, format="PNG", optimize=False, compress_level=6)
                partial.replace(target)
                # the crop is lossless: the tile's pixels are the original's pixels
                with Image.open(target) as check:
                    if check.size != (TILE, TILE) or check.convert("RGB").tobytes() != tile.tobytes():
                        raise ValueError("Tile differs from the original pixels: " + target.name)
                report.append(target.name)
    return report


def main():
    try:
        tiles = split(sys.argv[1] if len(sys.argv) > 1 else DEST)
    except Exception as error:
        print("BRAX_MURALE_TILES_ERROR", error, "; the build falls back to one 6144x2048 panel per mural")
        return 1
    print("BRAX_MURALE_TILES_RESULT " + json.dumps(dict(tiles=len(tiles), size="2048x2048", lossless=True, names=tiles)))
    return 0


if __name__ == "__main__":
    sys.exit(main())
