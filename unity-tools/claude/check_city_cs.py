#!/usr/bin/env python3
"""Wczesna kontrola C# miasta. Nie udaje pełnej kompilacji Unity 6.

Zatrzymuje znane, niepoprawne deklaracje 'var a=..., b=...' zanim
CI pobierze gigabajty zasobów i uruchomi obraz Unity.
"""
from pathlib import Path
import re
import sys

ROOT = Path("unity-city/Assets")
MULTI_VAR = re.compile(r"^\s*var\s+[A-Za-z_]\w*\s*=.*?,\s*[A-Za-z_]\w*\s*=", re.ASCII)
REQUIRED = (
    "Editor/Claude/BraxClaudeMetro.cs",
    "Claude/Runtime/BraxMetroM1Ruch.cs",
    "Claude/Runtime/BraxMetroM1Pasazerowie.cs",
    "Claude/Runtime/BraxTramwajT1.cs",
    "Claude/Runtime/BraxTramPasazerowie.cs",
    "Editor/Claude/Region/BraxMiastoOdbior.cs",
)

def main():
    errors = []
    for name in REQUIRED:
        path = ROOT / name
        if not path.is_file():
            errors.append(f"Brak wymaganego skryptu: {path}")
        elif not path.with_name(path.name + ".meta").is_file():
            errors.append(f"Brak GUID meta: {path}.meta")
    n = 0
    for path in ROOT.rglob("*.cs"):
        n += 1
        for no, line in enumerate(path.read_text(encoding="utf-8-sig").splitlines(), 1):
            if MULTI_VAR.match(line) and not line.lstrip().startswith("//"):
                errors.append(f"{path}:{no} — niedozwolona wielokrotna deklaracja var (CS0819)")
    if errors:
        for item in errors:
            print("::error::BRAXHORN_CS_PREFLIGHT " + item)
        return 1
    print(f"BRAXHORN_CS_PREFLIGHT_OK checked_files={n}; pełna kompilacja Unity nadal wymagana")
    return 0

if __name__ == "__main__":
    sys.exit(main())
