#!/usr/bin/env python3
"""BRAXHORN CITY — profesjonalny ekspres i młynek CC0, tylko gdy brak modeli właściciela.

Nie ściągaj całej sceny/nowego lokalu; nie zmieniaj szyldów ani elewacji.
Źródło ekspresu: https://3dassets.dev/assets/bakery-and-cafe-lever-espresso-machine-9c0d50a4
Źródło młynka: https://3dassets.dev/packs/bakery-and-cafe
Licencja obu: CC0 1.0 Universal. Pobieranie tylko przed buildem w GitHub Actions.
"""
import json
import pathlib
import struct
import sys
import urllib.parse
import urllib.request

OUT = pathlib.Path("unity-city/Assets/Source/Claude/Cafe")
PRIVATE = pathlib.Path("unity-city/Assets/Source/Prywatne/Miasto")
URL_MACHINE = "https://cdn.3dassets.dev/assets/22110/v1/model.glb"
URL_PACK = "https://3dassets.dev/api/v1/packs/bakery-and-cafe"
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
                print("BRAXHORN_KAWIARNIA_ORYGINAL_PRYWATNY", f)
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


def find_grinder_url(manifest):
    # Manifest dostawcy może być listą albo obiektem assets/models/items;
    # szukamy WYŁĄCZNIE indywidualnego modelu "on demand espresso grinder".
    def walk(obj):
        if isinstance(obj, dict):
            label = " ".join(str(obj.get(k, "")) for k in ("name", "title", "slug", "description")).lower()
            if ("on demand espresso grinder" in label or "doser espresso grinder" in label
                    or "espresso-grinder" in label or "espresso_grinder" in label):
                for key in ("cdnUrl", "cdn_url", "modelUrl", "model_url", "url", "downloadUrl"):
                    url = obj.get(key)
                    if isinstance(url, str) and url.startswith("https://cdn.3dassets.dev/assets/") and url.endswith("/model.glb"):
                        return url
                for v in obj.values():
                    if isinstance(v, str) and v.startswith("https://cdn.3dassets.dev/assets/") and v.endswith("/model.glb"):
                        return v
            for x in obj.values():
                result = walk(x)
                if result:
                    return result
        if isinstance(obj, list):
            for item in obj:
                result = walk(item)
                if result:
                    return result
        return None
    return walk(manifest)


def download(name, url):
    dest = OUT / name
    if dest.is_file() and dest.stat().st_size > 1000:
        valid_glb(dest.read_bytes())
        print("BRAXHORN_KAWIARNIA_MODEL_W_CACHE", dest)
        return
    data = get(url, MAX_BYTES)
    gltf = valid_glb(data)
    OUT.mkdir(parents=True, exist_ok=True)
    dest.write_bytes(data)
    print("BRAXHORN_KAWIARNIA_CC0", dest, len(data), "bytes meshes=", len(gltf["meshes"]),
          "extensions=", gltf.get("extensionsUsed", []))


def main():
    # Najpierw rzeczywiście dostarczony przez właściciela sprzęt, nie zamieniamy go.
    if not private_present(ESPRESSO):
        download("espresso_machine_sobczak.glb", URL_MACHINE)
    if not private_present(GRINDER):
        manifest = json.loads(get(URL_PACK, 5_000_000).decode("utf-8"))
        grinder_url = find_grinder_url(manifest)
        if not grinder_url:
            raise ValueError("Manifest CC0 nie zawiera osobnego młynka espresso — nie zastępujemy go atrapą")
        download("coffee_grinder_sobczak.glb", grinder_url)
    print("BRAXHORN_KAWIARNIA_PREFLIGHT_MODELE_ZRODLA_OK")


if __name__ == "__main__":
    try:
        main()
    except (ValueError, OSError, json.JSONDecodeError) as e:
        print("::error::BRAXHORN_KAWIARNIA_NIEGOTOWA " + str(e), file=sys.stderr)
        sys.exit(3)
