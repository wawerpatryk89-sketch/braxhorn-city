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


def door_candidates(data):
    nodes, meshes = data.get("nodes", []), data.get("meshes", [])
    parents = {}
    for i, node in enumerate(nodes):
        for child in node.get("children", []):
            if not isinstance(child, int) or not 0 <= child < len(nodes):
                raise ValueError("Niepoprawny indeks dziecka GLB")
            if child in parents:
                raise ValueError("Wezel GLB ma wielu rodzicow")
            parents[child] = i
    def geometry(index, visiting):
        if index in visiting:
            raise ValueError("Cykl w hierarchii GLB")
        node = nodes[index]
        mesh = node.get("mesh")
        own = isinstance(mesh, int) and 0 <= mesh < len(meshes) and bool(meshes[mesh].get("primitives"))
        descendants = [geometry(c, visiting | {index}) for c in node.get("children", [])]
        return own or any(descendants)
    named = {i for i, n in enumerate(nodes) if any(key in str(n.get("name", "")).lower() for key in ROOTS)}
    result = []
    for i in sorted(named):
        ancestor, seen, nested = parents.get(i), {i}, False
        while ancestor is not None:
            if ancestor in seen:
                raise ValueError("Cykl w hierarchii GLB")
            seen.add(ancestor)
            nested |= ancestor in named
            ancestor = parents.get(ancestor)
        # Imported Sketchfab door names belong to parent transforms; their
        # Object_* children hold the actual meshes. Count each moving root once.
        if not nested and geometry(i, set()):
            result.append((i, nodes[i]["name"]))
    return result


def main():
    path = pathlib.Path(sys.argv[1]) if len(sys.argv) > 1 else DEFAULT
    if not path.is_file() or path.stat().st_size < 1024:
        raise SystemExit("BRAXHORN_TRAM_GLB_ERROR: brak modelu " + str(path))
    data = load(path)
    nodes = data.get("nodes", [])
    meshes = data.get("meshes", [])
    names = [str(n.get("name", "")) for n in nodes]
    candidates = door_candidates(data)
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
            " wydzielonych grup door_left/door_right z geometria; bez tego nie mozna"
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
