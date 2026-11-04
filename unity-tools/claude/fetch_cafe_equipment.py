#!/usr/bin/env python3
"""BRAXHORN CITY — profesjonalny ekspres i młynek CC0, tylko gdy brak modeli właściciela.

Nie ściągaj całej sceny/nowego lokalu; nie zmieniaj szyldów ani elewacji.
Źródło ekspresu: https://3dassets.dev/assets/bakery-and-cafe-lever-espresso-machine-9c0d50a4
Źródło młynka: https://3dassets.dev/packs/bakery-and-cafe
Licencja obu: CC0 1.0 Universal. Pobieranie tylko przed buildem w GitHub Actions.
"""
import hashlib
import json
import pathlib
import struct
import sys
import urllib.request

OUT = pathlib.Path("unity-city/Assets/Source/Claude/Cafe")
PRIVATE = pathlib.Path("unity-city/Assets/Source/Prywatne/Miasto")
# Exact versioned URLs published on the two asset pages, verified 2026-11-04.
# Build inputs must not depend on the provider's discovery API response shape.
URL_MACHINE = "https://cdn.3dassets.dev/assets/22110/v1/model.glb"
# https://3dassets.dev/assets/bakery-and-cafe-grinder-on-demand-4bcd3bea
URL_GRINDER = "https://cdn.3dassets.dev/assets/22111/v1/model.glb"
EXPECTED_SHA256 = {
    URL_MACHINE: "c3e09b2baa05eba3eb1fec2f460fd5b2c1fcc3da789694811fc591bea803db05",
    URL_GRINDER: "1687e650e7890f2c403803cfe11fffc199f02cddf71ec16c8689887e91ef9edd",
}
MAX_BYTES = 3_000_000
ESPRESSO = ("ekspres_kolbowy", "ekspres_do_kawy", "ekspres", "coffee_machine",
            "espresso_machine", "commercial_coffee_machine", "kawiarnia_ekspres")
GRINDER = ("mlynek_kawy", "mlynek_do_kawy", "mlynek", "coffee_grinder",
           "espresso_grinder", "kawiarnia_mlynek")


def private_present(names):
    for pack in ("miasto_sklepy", "miasto_wyposazenie"):
        for stem in names:
            f = PRIVATE / pack / (stem + ".glb")
            if f.is_file() and f.stat().st_size > 1000:
                valid_glb(f.read_bytes())
                print("BRAXHORN_KAWIARNIA_ORYGINAL_PRYWATNY", f)
                return True
    # The Unity importer also accepts a named subtree in the owner's equipment GLB.
    combined = PRIVATE / "miasto_wyposazenie" / "miasto_wyposazenie.glb"
    if combined.is_file():
        gltf = valid_glb(combined.read_bytes())
        nodes = gltf.get("nodes", [])
        def has_mesh(index, seen):
            if index in seen or not 0 <= index < len(nodes):
                return False
            seen.add(index)
            node = nodes[index]
            return "mesh" in node or any(has_mesh(i, seen) for i in node.get("children", []))
        for index, node in enumerate(nodes):
            if node.get("name", "").lower() in names and has_mesh(index, set()):
                print("BRAXHORN_KAWIARNIA_ORYGINAL_PRYWATNY_WEZEL", combined, node["name"])
                return True
    return False


def get(url, max_bytes):
    req = urllib.request.Request(url, headers={"User-Agent": "BraxhornCity-Unity6/1.0"})
    with urllib.request.urlopen(req, timeout=35) as response:
        data = response.read(max_bytes + 1)
    if len(data) > max_bytes:
        raise ValueError("Przekroczony mobilny budżet modelu: " + str(max_bytes))
    return data


def valid_glb(data):
    if len(data) < 80 or data[:4] != b"glTF":
        raise ValueError("Brak poprawnego nagłówka GLB")
    version, size = struct.unpack_from("<II", data, 4)
    if version != 2 or size != len(data):
        raise ValueError("Nagłówek GLB ma błędny rozmiar/wersję")
    js_len, kind = struct.unpack_from("<II", data, 12)
    if kind != 0x4E4F534A or 20 + js_len > len(data):
        raise ValueError("Brak prawidłowego chunku JSON")
    gltf = json.loads(data[20:20 + js_len].decode("utf-8").rstrip("\x00 "))
    if not gltf.get("meshes"):
        raise ValueError("Model GLB bez siatek")
    # glTFast: wstrzymujemy build, jeśli pobranie nie zawiera materiałów lub siatek.
    return gltf


def checked_model(data, url):
    gltf = valid_glb(data)
    if hashlib.sha256(data).hexdigest() != EXPECTED_SHA256[url]:
        raise ValueError("Suma SHA-256 modelu kawiarni niezgodna ze zweryfikowanym źródłem: " + url)
    return gltf


def download(name, url):
    dest = OUT / name
    if dest.is_file() and dest.stat().st_size > 1000:
        checked_model(dest.read_bytes(), url)
        print("BRAXHORN_KAWIARNIA_MODEL_W_CACHE", dest)
        return
    data = get(url, MAX_BYTES)
    gltf = checked_model(data, url)
    OUT.mkdir(parents=True, exist_ok=True)
    pending = dest.with_suffix(".glb.part")
    pending.write_bytes(data)
    pending.replace(dest)
    print("BRAXHORN_KAWIARNIA_CC0", dest, len(data), "bytes meshes=", len(gltf["meshes"]),
          "extensions=", gltf.get("extensionsUsed", []))


def main():
    # Najpierw rzeczywiście dostarczony przez właściciela sprzęt, nie zamieniamy go.
    if not private_present(ESPRESSO):
        download("espresso_machine_sobczak.glb", URL_MACHINE)
    if not private_present(GRINDER):
        download("coffee_grinder_sobczak.glb", URL_GRINDER)
    print("BRAXHORN_KAWIARNIA_PREFLIGHT_MODELE_ZRODLA_OK")


if __name__ == "__main__":
    try:
        main()
    except (ValueError, OSError, json.JSONDecodeError) as e:
        print("::error::BRAXHORN_KAWIARNIA_NIEGOTOWA " + str(e), file=sys.stderr)
        sys.exit(3)
