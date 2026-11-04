#!/usr/bin/env python3
"""Wczesna statyczna bramka źródeł miasta (nie jest kompilatorem Unity 6).

Weryfikuje istnienie skryptów i GUID komponentów runtime oraz
wyłapuje CS0819: użycie 'var' przy wielu deklaracjach w jednym zdaniu.
"""
from pathlib import Path
import re
import sys

ROOT = Path("unity-city/Assets")
VAR_START = re.compile(r"^\s*var\s+[A-Za-z_]\w*\s*=", re.ASCII)
ANOTHER_NAME = re.compile(r"^\s*[A-Za-z_]\w*\s*=", re.ASCII)
REQUIRED = (
    "Editor/Claude/BraxClaudeMetro.cs",
    "Claude/Runtime/BraxMetroM1Ruch.cs",
    "Claude/Runtime/BraxMetroM1Pasazerowie.cs",
    "Claude/Runtime/BraxTramwajT1.cs",
    "Claude/Runtime/BraxTramPasazerowie.cs",
    "Editor/Claude/Region/BraxMiastoOdbior.cs",
)

def multiple_var(line: str) -> bool:
    match = VAR_START.match(line)
    if not match:
        return False
    stack = []
    in_string = None
    escaped = False
    for i in range(match.end(), len(line)):
        c = line[i]
        if in_string is not None:
            if escaped:
                escaped = False
            elif c == "\\":
                escaped = True
            elif c == in_string:
                in_string = None
            continue
        if c in ("'", '"'):
            in_string = c
        elif c in "([{":
            stack.append(c)
        elif c in ")]}":
            if stack:
                stack.pop()
        elif c == "," and not stack and ANOTHER_NAME.match(line[i+1:]):
            return True
        elif c == "/" and line[i:i+2] == "//":
            break
        elif c == ";" and not stack:
            break
    return False


def test_detector():
    cases = (
        ("var a = material, b = other;", True),
        ("var a = Build(1, 2);", False),
        ('var a = "x, y=1";', False),
        ("var a = Build(x, y = 1);", False),
        ("Material a = material, b = other;", False),
        ("var a = new Dictionary<int, string>();", False),
    )
    for source, expected in cases:
        assert multiple_var(source) == expected, f"Self-test detektora: {source}"


def main() -> int:
    test_detector()
    errors = []
    for name in REQUIRED:
        path = ROOT / name
        if not path.is_file():
            errors.append(f"Brak wymaganego skryptu: {path}")
        elif name != "Editor/Claude/BraxClaudeMetro.cs" and not path.with_name(path.name + ".meta").is_file():
            # Istniejący skrypt edytora nie ma wersjonowanego meta w źródłach.
            # Nie generujemy nowego GUID ani nie unieważniamy scen.
            errors.append(f"Brak GUID meta komponentu: {path}.meta")
    n = 0
    for path in ROOT.rglob("*.cs"):
        n += 1
        for lineno, line in enumerate(path.read_text(encoding="utf-8-sig", errors="replace").splitlines(), 1):
            if multiple_var(line):
                errors.append(f"{path}:{lineno} — wielokrotna deklaracja var (CS0819)")
    if errors:
        for item in errors:
            print("::error::BRAXHORN_CS_PREFLIGHT " + item)
        return 1
    print(f"BRAXHORN_CS_PREFLIGHT_OK files={n}; kompilacja Unity nadal obowiązkowa")
    return 0


if __name__ == "__main__":
    sys.exit(main())
