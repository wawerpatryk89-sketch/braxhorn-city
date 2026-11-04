#!/usr/bin/env python3
"""Metro M1: szybki audyt oryginalnego, zoptymalizowanego wagonu kane_sk06.

Uruchamiane po checkout, przed kosztownym Unity buildem. Oryginał w galerii
218.29 MB; w repo jest oficjalnie opisana zoptymalizowana kopia ~25 MB.
Nie pobiera niczego i nie przerabia tekstur bez zgody właściciela.
"""
import json
from pathlib import Path
import struct
import sys

P = Path("unity-city/Assets/Source/Claude/Metro/metro_train.glb")


def main():
    if not P.exists() or P.stat().st_size < 1_000_000:
        print("::error::BRAXHORN_METRO_M1_SOURCE_BRAK nie ma zoptymalizowanego train.glb")
        return 2
    if P.stat().st_size > 35_000_000:
        print("::error::BRAXHORN_METRO_M1_SOURCE_ZA_DUZY model >35MB, nie używaj oryginału 218MB w APK")
        return 2
    with P.open("rb") as f:
        head = f.read(20)
        if len(head) < 20:
            print("::error::BRAXHORN_METRO_M1_GLTF niepoprawny nagłówek")
            return 2
        magic, version, size, chunk, kind = struct.unpack("<4sIIII", head)
        if magic != b"glTF" or version != 2 or size != P.stat().st_size or kind != 0x4E4F534A:
            print("::error::BRAXHORN_METRO_M1_GLTF nieprawidłowy GLB2")
            return 2
        if chunk > 16_000_000:
            print("::error::BRAXHORN_METRO_M1_GLTF niebezpieczny rozmiar JSON")
            return 2
        gltf = json.loads(f.read(chunk).decode("utf-8").rstrip("\x00 "))
    meshes = gltf.get("meshes", [])
    if not meshes:
        print("::error::BRAXHORN_METRO_M1_GLTF brak geometrii")
        return 2
    tris = 0
    for mesh in meshes:
        for p in mesh.get("primitives", []):
            accessor_idx = p.get("indices")
            if accessor_idx is not None:
                count = gltf.get("accessors", [])[accessor_idx].get("count", 0)
                tris += count // 3
    names = [str(n.get("name", "")) for n in gltf.get("nodes", [])]
    doors = [n for n in names if "door_left" in n.lower() or "door_right" in n.lower()]
    print("BRAXHORN_METRO_M1_MODEL_OK size_MB=", round(P.stat().st_size / 1e6, 2),
          "meshes=", len(meshes), "triangles≈", tris, "textures=", len(gltf.get("textures", [])),
          "named_real_doors=", len(doors))
    if doors:
        print("BRAXHORN_METRO_M1_UWAGA: prawdziwe drzwi do dalszego opracowania, teraz jedynie inscenizacja paneli.")
    else:
        print("BRAXHORN_METRO_M1_SCENICZNE_DRZWI: brak osobnych skrzydeł w GLB, tylko efekt peronowy.")
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except (OSError, ValueError, json.JSONDecodeError, IndexError) as exc:
        print("::error::BRAXHORN_METRO_M1_MODEL_NIEGOTOWY " + str(exc))
        sys.exit(2)
