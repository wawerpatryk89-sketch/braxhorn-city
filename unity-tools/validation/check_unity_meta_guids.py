#!/usr/bin/env python3
"""Fast, read-only Unity .meta GUID preflight. No Editor, downloads, assets or APK.

Unity ignores C# files if their .meta GUID is malformed. A bad 30-character GUID
caused CS0103 for BraxChampionsBeachFurnishings on 2026-11-06. This check runs
before expensive imports in the owner's manually authorized APK workflow.
"""
from __future__ import annotations

import argparse
from pathlib import Path
import re
import sys

GUID_LINE = re.compile(r"^guid:\s*(\S+)\s*$", re.MULTILINE)
GUID = re.compile(r"[0-9a-fA-F]{32}\Z")

CHAMPIONS_CLASSES = {
    "Editor/Mistrzowie/BraxChampionsBeachFurnishings.cs":
        "public static class BraxChampionsBeachFurnishings",
    "Editor/Mistrzowie/BraxChampionsFoundation.cs":
        "public static class BraxChampionsFoundation",
    "Editor/Mistrzowie/BraxChampionsCasinoT11.cs":
        "public static class BraxChampionsCasinoT11",
    "Editor/Mistrzowie/BraxChampionsBuildings.cs":
        "public static class BraxChampionsBuildings",
}

def inspect(root: Path, champions: bool = False):
    errors = []
    seen = {}
    metas = sorted(root.rglob("*.meta"))
    for meta in metas:
        try:
            source = meta.read_text(encoding="utf-8")
        except (UnicodeError, OSError) as exc:
            errors.append(f"{meta}: unreadable metadata: {exc}")
            continue
        match = GUID_LINE.search(source)
        token = match.group(1) if match else None
        if not token or GUID.fullmatch(token) is None:
            errors.append(f"{meta}: invalid GUID {token!r}; Unity may ignore its asset")
            continue
        key = token.lower()
        previous = seen.setdefault(key, meta)
        if previous != meta:
            errors.append(f"Duplicate Unity GUID {token}: {previous} AND {meta}")
    if champions:
        for path, declaration in CHAMPIONS_CLASSES.items():
            source_file = root / path
            meta_file = root / (path + ".meta")
            if not source_file.is_file():
                errors.append(f"Missing required Champions C# source: {source_file}")
            elif not any(form in source_file.read_text(encoding="utf-8") for form in
                         # 9.11: the casino class is split over two files (partial); same class, same GUID.
                         (declaration, declaration.replace("static class", "static partial class"))):
                errors.append(f"Missing or changed declaration in {source_file}: {declaration}")
            if not meta_file.is_file():
                errors.append(f"Missing pinned Champions Unity GUID: {meta_file}")
        script = root / "Editor/Mistrzowie/BraxChampionsFoundation.cs"
        if script.is_file() and "BraxChampionsBeachFurnishings.Build(root)" not in script.read_text(encoding="utf-8"):
            errors.append("Foundation no longer creates the real beach furnishings")
    return len(metas), errors

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", default="unity-city/Assets")
    parser.add_argument("--champions", action="store_true")
    args = parser.parse_args()
    count, errors = inspect(Path(args.root), args.champions)
    print(f"BRAX_UNITY_META_GUID_PREFLIGHT files={count} errors={len(errors)} editor=NOT_RUN")
    for error in errors:
        print("ERROR: " + error, file=sys.stderr)
    if errors:
        raise SystemExit(1)

if __name__ == "__main__":
    main()
