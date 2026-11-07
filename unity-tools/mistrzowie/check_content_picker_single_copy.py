#!/usr/bin/env python3
from __future__ import annotations

import io
import sys
from pathlib import Path
from zipfile import ZipFile, BadZipFile

ROOT = Path(__file__).resolve().parents[2]
ASSETS = ROOT / "unity-city" / "Assets"
SOURCE = ROOT / "unity-tools" / "mistrzowie" / "android" / "BraxContentPicker.java"
EXPECTED_JAR = ASSETS / "Plugins" / "Android" / "BraxContentPicker.jar"
CLASS = "com/braxhorn/content/BraxContentPicker.class"


def jar_has_picker(path: Path) -> bool:
    try:
        with ZipFile(path) as z:
            return CLASS in z.namelist()
    except BadZipFile:
        return False


def aar_has_picker(path: Path) -> bool:
    try:
        with ZipFile(path) as z:
            for name in z.namelist():
                if name == CLASS:
                    return True
                if name.endswith(".jar"):
                    try:
                        with ZipFile(io.BytesIO(z.read(name))) as nested:
                            if CLASS in nested.namelist():
                                return True
                    except BadZipFile:
                        pass
    except BadZipFile:
        return False
    return False


def main() -> int:
    require_jar = "--require-jar" in sys.argv
    if not SOURCE.is_file():
        raise SystemExit(f"Missing canonical picker source outside Assets: {SOURCE}")

    source_text = SOURCE.read_text(encoding="utf-8")
    if "package com.braxhorn.content;" not in source_text or "class BraxContentPicker" not in source_text:
        raise SystemExit("Canonical BraxContentPicker source is malformed")

    java_inside = []
    binary_providers = []
    for path in ASSETS.rglob("*"):
        if not path.is_file():
            continue
        suffix = path.suffix.lower()
        if suffix == ".java":
            try:
                text = path.read_text(encoding="utf-8", errors="ignore")
            except OSError:
                continue
            if "package com.braxhorn.content;" in text and "class BraxContentPicker" in text:
                java_inside.append(path)
        elif suffix == ".jar" and jar_has_picker(path):
            binary_providers.append(path)
        elif suffix == ".aar" and aar_has_picker(path):
            binary_providers.append(path)

    if java_inside:
        raise SystemExit("Duplicate picker Java source inside Unity Assets: " + ", ".join(map(str, java_inside)))

    if require_jar:
        expected = EXPECTED_JAR.resolve()
        resolved = [p.resolve() for p in binary_providers]
        if resolved != [expected]:
            raise SystemExit(
                "BraxContentPicker must have exactly one Android binary provider: "
                + (", ".join(map(str, binary_providers)) or "none")
            )

    print(
        "BRAX_MISTRZOWIE_PICKER_SINGLE_COPY_PASS "
        f"javaInsideAssets={len(java_inside)} binaryProviders={len(binary_providers)} "
        f"canonicalSource={SOURCE.relative_to(ROOT)}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
