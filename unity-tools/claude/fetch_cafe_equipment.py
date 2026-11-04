#!/usr/bin/env python3
"""BRAXHORN CITY / Unity 6 — tylko autentyczny model premium do SOBCZAK KAWIARNIA.

Priorytet w C#: już dostarczone prywatne modele w miasto_sklepy/miasto_wyposazenie.
Fallback wyłącznie dla brakującego ekspresu: jeden sprawdzony, niebrandowany model
GLB CC0 na stałym CDN. Bez Blender, Three.js i bez kompletnego nowego wnętrza.
Źródło: https://3dassets.dev/assets/bakery-and-cafe-lever-espresso-machine-9c0d50a4
Licencja: CC0 1.0, opis modelu: 0.862 × 0.612 × 0.661 m, ok. 4616 triangles.
"""
import json
import pathlib
import struct
import urllib.error
import urllib.request

OUT = pathlib.Path("unity-city/Assets/Source/Claude/Cafe/espresso_machine_sobczak.glb")
URL = "https://cdn.3dassets.dev/assets/22110/v1/model.glb"
MAX_BYTES = 2_000_000

def main():
    OUT.parent.mkdir(parents=True, exist_ok=True)
    req = urllib.request.Request(URL, headers={"User-Agent": "BraxhornCity-Unity6/1.0"})
    try:
        with urllib.request.urlopen(req, timeout=40) as f:
            data = f.read(MAX_BYTES + 1)
    except (OSError, urllib.error.URLError) as e:
        raise SystemExit("BRAXHORN_KAWIARNIA_BRAK_EKSPRESU: " + str(e))
    if len(data) > MAX_BYTES:
        raise SystemExit("BRAXHORN_KAWIARNIA_BLEDNY_MODEL: rozmiar ponad 2 MB")
    if len(data) < 100 or data[:4] != b"glTF":
        raise SystemExit("BRAXHORN_KAWIARNIA_BLEDNY_MODEL: nie jest GLB")
    version, declared = struct.unpack_from("<II", data, 4)
    if version != 2 or declared != len(data):
        raise SystemExit("BRAXHORN_KAWIARNIA_BLEDNY_MODEL: uszkodzona dlugosc GLB")
    chunk_size, kind = struct.unpack_from("<II", data, 12)
    if kind != 0x4E4F534A or 20 + chunk_size > len(data):
        raise SystemExit("BRAXHORN_KAWIARNIA_BLEDNY_MODEL: brak JSON GLB")
    parsed = json.loads(data[20:20 + chunk_size].rstrip(b"\\x00 ").decode("utf-8"))
    if not parsed.get("meshes"):
        raise SystemExit("BRAXHORN_KAWIARNIA_BLEDNY_MODEL: brak geometrii")
    OUT.write_bytes(data)
    print("BRAXHORN_KAWIARNIA_EKSPRES_POBRANY: CC0 GLB", OUT, "bytes=",len(data),
          "meshes=",len(parsed["meshes"]))

if __name__ == "__main__":
    main()
