#!/usr/bin/env python3
"""Audit actual source image dimensions before Unity Android import.

Only reads the files already present in the checked out R1 asset library.
A file name or 2048 importer override does not prove a 2K source exists.
Never resamples images and never changes the user's visual source.
"""
from __future__ import annotations

import argparse
import json
from pathlib import Path

ROOTS = (
    "Source/City", "Source/CityRebuild", "Source/Claude/PolyHaven",
    "Source/HarborBeach", "Source/Prywatne/Miasto",
    "Source/Prywatne/Grafiki", "PlayableGenerated/CityRebuild",
)
SUFFIXES = {".png", ".jpg", ".jpeg", ".webp", ".tga", ".tif", ".tiff", ".bmp", ".exr", ".hdr"}
CHARACTER_MARKERS = ("/character/", "/characters/", "/hero/", "/patryk/", "/npc/", "/rocketbox/")
ART_MARKERS = ("/mural", "/brand/", "/logo", "/ui/", "/resources/", "/billboard", "/sign", "/skybox")
MIN_2K = 2048


def scan(assets: Path):
    try:
        from PIL import Image, UnidentifiedImageError
    except ImportError:
        return {"schema": 1, "area": "miasto-r1", "status": "PILLOW_NOT_AVAILABLE",
                "inspected": 0, "below2k": 0, "shortfalls": [],
                "error": "Source inspection needs Pillow; no asset was changed"}
    images, errors, shortfalls = [], [], []
    for directory in ROOTS:
        root = assets / directory
        if not root.is_dir():
            continue
        for path in root.rglob("*"):
            if not path.is_file() or path.suffix.lower() not in SUFFIXES:
                continue
            relative = path.relative_to(assets).as_posix()
            check = "/" + relative.lower()
            if any(k in check for k in CHARACTER_MARKERS):
                continue
            art = any(k in check for k in ART_MARKERS)
            try:
                with Image.open(path) as im:
                    w, h = im.size
                record = {"path": relative, "sourceWidth": w, "sourceHeight": h,
                          "originalFileBytes": path.stat().st_size, "artworkException": art}
                images.append(record)
                if max(w, h) < MIN_2K and not art:
                    shortfalls.append(record)
            except (OSError, ValueError, UnidentifiedImageError) as ex:
                errors.append({"path": relative, "reason": type(ex).__name__})
    return {
        "schema": 1, "area": "miasto-r1", "status": "SOURCE_ONLY_NOT_DEVICE_VERIFIED",
        "maxDimensionExpectedForEnvironment": MIN_2K,
        "inspected": len(images), "below2k": len(shortfalls),
        "shortfalls": sorted(shortfalls, key=lambda x: x["originalFileBytes"], reverse=True),
        "failedToRead": errors, "artworkExceptions": sum(1 for row in images if row["artworkException"]),
        "limitations": "Embedded GLB textures or earlier offline-downsampled packages may hide higher-resolution originals. Values do not prove runtime residency or image sharpness. Nothing resized.",
    }


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--assets-root", type=Path, default=Path("unity-city/Assets"))
    parser.add_argument("--output", type=Path, default=Path("city-review/r1-source-textures.json"))
    opts = parser.parse_args()
    report = scan(opts.assets_root)
    opts.output.parent.mkdir(parents=True, exist_ok=True)
    opts.output.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print("BRAXHORN_R1_SOURCE_2K inspected={inspected} below2k={below2k} status={status} report={out}".format(
        **report, out=opts.output))
    for row in report["shortfalls"][:20]:
        print("::warning::R1_IMAGE_SOURCE_UNDER_2K {} {}x{} — require original 2K, DO NOT UPSCALE".format(
            row["path"], row["sourceWidth"], row["sourceHeight"]))


if __name__ == "__main__":
    main()
