#!/usr/bin/env python3
"""Pobiera prawdziwe materiały i modele CC0 z Poly Haven do testowego buildu Unity.

Tekstury: 2K (kolor, normalna GL, ARM).  Modele: glTF z teksturami 1K (telefon).
Wynik: unity-city/Assets/Source/Claude/PolyHaven/{textures,models}/<id>/
Brak któregokolwiek pliku = błąd (żadnych zamienników).
"""
import json
import pathlib
import sys
import time
import urllib.request

UA = {"User-Agent": "BraxhornCity-UnityBuild/1.0 (github.com/wawerpatryk89-sketch/braxhorn-city)"}
ROOT = pathlib.Path("unity-city/Assets/Source/Claude/PolyHaven")

TEXTURES = [
    "wood_floor_deck",           # taras na dachu wieżowca
    "rectangular_facade_tiles",  # elewacja wieżowca (wariant A)
    "exterior_wall_cladding",    # elewacja wieżowca (wariant B)
    "granite_tile",              # cokół / obrzeża tarasu
]
MODELS = [
    "planter_box_01",
    "potted_plant_02",
    "potted_plant_04",
    "outdoor_table_chair_set_01",
    "modular_street_seating",
]


def get(url: str) -> bytes:
    for attempt in range(4):
        try:
            req = urllib.request.Request(url, headers=UA)
            with urllib.request.urlopen(req, timeout=180) as r:
                return r.read()
        except Exception as exc:  # noqa: BLE001
            if attempt == 3:
                raise
            print(f"retry {attempt + 1} {url}: {exc}")
            time.sleep(3 * (attempt + 1))
    raise RuntimeError(url)


def files(asset: str) -> dict:
    return json.loads(get(f"https://api.polyhaven.com/files/{asset}"))


def pick(res: dict, *keys):
    for k in keys:
        if k in res:
            return res[k]
    raise KeyError(f"brak rozdzielczości {keys} w {list(res)}")


def main() -> int:
    report = {"source": "Poly Haven (CC0)", "textures": {}, "models": {}}
    for t in TEXTURES:
        f = files(t)
        d = ROOT / "textures" / t
        d.mkdir(parents=True, exist_ok=True)
        for key, out in (("Diffuse", "diffuse.jpg"), ("nor_gl", "normal.jpg"), ("arm", "arm.jpg")):
            url = pick(f[key], "2k")["jpg"]["url"]
            data = get(url)
            (d / out).write_bytes(data)
            report["textures"].setdefault(t, {})[out] = len(data)
        print("TEX", t, "OK")
    for m in MODELS:
        f = files(m)
        g = pick(f["gltf"], "1k", "2k")["gltf"]
        d = ROOT / "models" / m
        d.mkdir(parents=True, exist_ok=True)
        (d / f"{m}.gltf").write_bytes(get(g["url"]))
        n = 1
        for rel, info in (g.get("include") or {}).items():
            p = d / rel
            p.parent.mkdir(parents=True, exist_ok=True)
            p.write_bytes(get(info["url"]))
            n += 1
        report["models"][m] = n
        print("MODEL", m, "files", n)
    (ROOT / "polyhaven-receipt.json").write_text(json.dumps(report, indent=1))
    (ROOT / "LICENSE-CC0.txt").write_text(
        "Wszystkie pliki w tym katalogu pochodzą z https://polyhaven.com i są na licencji CC0.\n")
    return 0


if __name__ == "__main__":
    sys.exit(main())
