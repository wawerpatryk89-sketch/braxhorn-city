#!/usr/bin/env python3
"""Fail early when a project-owned WebGL native import has no JS provider.

This source check does not replace the Unity linker or runtime validation.
It covers the BX-prefixed bridges used by this project's player and venues.
"""

from pathlib import Path
import re
import sys


ASSETS = Path(__file__).resolve().parents[1] / "unity-city" / "Assets"
NATIVE_IMPORT = re.compile(
    r'\[DllImport\(\s*"__Internal"\s*\)\]\s*'
    r'(?:(?:public|private|protected|internal|static)\s+)*'
    r'extern\s+[\w<>\[\]]+\s+(BX\w+)\s*\('
)
JS_PROVIDER = re.compile(r'\b(BX\w+)\s*:\s*function\s*\(')


def main():
    imports = {}
    providers = {}
    for source in sorted(ASSETS.rglob("*.cs")):
        for symbol in NATIVE_IMPORT.findall(source.read_text(encoding="utf-8")):
            imports.setdefault(symbol, []).append(source.relative_to(ASSETS))
    for source in sorted(ASSETS.rglob("*.jslib")):
        for symbol in JS_PROVIDER.findall(source.read_text(encoding="utf-8")):
            providers.setdefault(symbol, []).append(source.relative_to(ASSETS))
    if not imports:
        print("WEBGL_BRIDGE_ERROR no project-owned BX imports found", file=sys.stderr)
        return 1
    errors = []
    for symbol, sources in sorted(imports.items()):
        matches = providers.get(symbol, [])
        if len(matches) != 1:
            errors.append(f"{symbol}: {len(matches)} providers; imports={sources}; providers={matches}")
        else:
            print(f"WEBGL_BRIDGE_SOURCE_OK {symbol} <- {matches[0]}")
    for error in errors:
        print(f"WEBGL_BRIDGE_ERROR {error}", file=sys.stderr)
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
