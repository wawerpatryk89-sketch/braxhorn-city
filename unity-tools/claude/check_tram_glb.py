#!/usr/bin/env python3
"""Braxhorn City: szybka kontrola binarnego tramwaj_caf.glb przed kosztownym buildem Unity 6.

Nie potrzebuje Unity/Blendera ani pobierania modeli. Nie modyfikuje pliku.
Sprawdza, czy oryginalny model ma osobne siatki skrzydel drzwi potrzebne do
animacji i obsadzania tramwaju prawdziwymi pasażerami Rocketbox.
"""
import json
import pathlib
import struct
import sys

DEFAULT = pathlib.Path("unity-city/Assets/Source/Prywatne/Miasto/miasto_pojazdy/tramwaj_caf.glb")
ROOTS = ("door_left", "door_right", "drzwi_lew", "drzwi_praw")


def load(path: pathlib.Path):
    with path.open("rb") as f:
        header = f.read(12)
        if len(header) != 12:
            raise ValueError("Niepoprawny naglowek GLB (za krotki)")
        magic, version, length = struct.unpack("<4sII", header)
        if magic != b"glTF" or version != 2 or length != path.stat().st_size:
            raise ValueError("Niepoprawny magic/version/length; to nie GLB2")
        chunk_head = f.read(8)
        if len(chunk_head) != 8:
            raise ValueError("GLB bez JSON chunk")
        chunk_size, kind = struct.unpack("<II", chunk_head)
        if kind != 0x4E4F534A or chunk_size > 20_000_000:
            raise ValueError("Pierwszy chunk nie jest poprawnym JSON")
        data = json.loads(f.read(chunk_size).decode("utf-8").rstrip(" \x00"))
        return data


def main():
    path = pathlib.Path(sys.argv[1]) if len(sys.argv) > 1 else DEFAULT
    if not path.is_file() or path.stat().st_size < 1024:
        raise SystemExit("BRAXHORN_TRAM_GLB_ERROR: brak modelu " + str(path))
    data = load(path)
    nodes = data.get("nodes", [])
    meshes = data.get("meshes", [])
    names = [str(n.get("name", "")) for n in nodes]
    candidates = [
        (i, n["name"])
        for i, n in enumerate(nodes)
        if isinstance(n.get("name"), str)
        and any(key in n["name"].lower() for key in ROOTS)
        and isinstance(n.get("mesh"), int)
    ]
    diagnostics = {
        "plik": str(path),
        "bajty": path.stat().st_size,
        "wszystkie_wezly": len(nodes),
        "wszystkie_siatki": len(meshes),
        "podejrzane_drzwiczki": candidates,
        "moga_animowac": len(candidates) >= 2,
        "nazwy_przykladowych_wezlow": names[:30],
    }
    print("BRAXHORN_TRAM_GLTF_AUDYT " + json.dumps(diagnostics, ensure_ascii=False))
    if len(candidates) < 2:
        print(
            "::error::TRAM_T1_DRZWI_NIEGOTOWE: oryginalny CAF nie ma minimum dwoch"
            " wydzielonych siatek door_left/door_right; bez tego nie mozna"
            " realistycznie otwierac drzwi ani przepuszczac NPC."
        )
        return 3
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except (ValueError, OSError, json.JSONDecodeError) as exc:
        print(f"BRAXHORN_TRAM_GLTF_ERROR: {exc}", file=sys.stderr)
        sys.exit(2)
