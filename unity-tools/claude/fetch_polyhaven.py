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
    "granite_tile",              # cokół / obrzeża tarasu, front baru
    "marble_01",                 # blat baru w klubie NOVA
    "wood_planks",               # tylna zabudowa baru / podest DJ
]
MODELS = [
    "planter_box_01",
    "potted_plant_02",
    "potted_plant_04",
    "outdoor_table_chair_set_01",
    "modular_street_seating",
    "bar_chair_round_01",        # hokery w klubie NOVA
    "tree_small_02",             # prawdziwe drzewa zamiast „kulek” (odchudzane gltfpack w buildzie)
    # "island_tree_01",  # skan (mln trójkątów) — w grze drzewo_verdant
    # "island_tree_02",  # skan (mln trójkątów) — w grze drzewo_verdant
    # "jacaranda_tree",  # skan (mln trójkątów) — w grze drzewo_verdant
]


def get(url: str) -> bytes:
    # 31.10: build APK 41203048575 padł na ambientcg.com (4 × 180 s timeout = 12 min). Krótszy timeout, więcej prób;
    # a przede wszystkim: pliki, które już są (pamięć podręczna CI), nie są pobierane ponownie (SKIP niżej).
    for attempt in range(6):
        try:
            req = urllib.request.Request(url, headers=UA)
            with urllib.request.urlopen(req, timeout=60) as r:
                return r.read()
        except Exception as exc:  # noqa: BLE001
            if attempt == 5:
                raise
            print(f"retry {attempt + 1} {url}: {exc}")
            time.sleep(5 * (attempt + 1))
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
        d = ROOT / "textures" / t
        if all((d / o).exists() for o in ("diffuse.jpg", "normal.jpg", "arm.jpg")):
            report["textures"][t] = "cached"; print("TEX", t, "SKIP (jest)"); continue
        f = files(t)
        d.mkdir(parents=True, exist_ok=True)
        for key, out in (("Diffuse", "diffuse.jpg"), ("nor_gl", "normal.jpg"), ("arm", "arm.jpg")):
            url = pick(f[key], "2k")["jpg"]["url"]
            data = get(url)
            (d / out).write_bytes(data)
            report["textures"].setdefault(t, {})[out] = len(data)
        print("TEX", t, "OK")
    for m in MODELS:
        d = ROOT / "models" / m
        if (d / f"{m}.gltf").exists() and len([q for q in d.rglob("*") if q.is_file()]) > 1:
            report["models"][m] = "cached"; print("MODEL", m, "SKIP (jest)"); continue
        f = files(m)
        g = pick(f["gltf"], "1k", "2k")["gltf"]
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
    # Prawdziwa trawa: ambientCG Grass004 (CC0), 2K JPG -> diffuse/normal/arm (R=AO, G=roughness).
    import io, zipfile
    from PIL import Image
    d = ROOT / "textures" / "grass004"
    if all((d / o).exists() for o in ("diffuse.jpg", "normal.jpg", "arm.jpg")):
        print("TEX grass004 SKIP (jest)"); z = None
    else:
        z = zipfile.ZipFile(io.BytesIO(get("https://ambientcg.com/get?file=Grass004_2K-JPG.zip")))
    names = z.namelist() if z else []
    def member(token):
        for n in names:
            if token in n and n.lower().endswith((".jpg", ".png")):
                return Image.open(io.BytesIO(z.read(n)))
        return None
    if z:
        color, normal, rough, ao = member("_Color"), member("_NormalGL"), member("_Roughness"), member("_AmbientOcclusion")
        if color is None or normal is None or rough is None:
            raise RuntimeError("Grass004: brak map w paczce: " + ", ".join(names))
        d.mkdir(parents=True, exist_ok=True)
        color.convert("RGB").save(d / "diffuse.jpg", quality=92)
        normal.convert("RGB").save(d / "normal.jpg", quality=95)
        r = rough.convert("L")
        a = ao.convert("L").resize(r.size) if ao is not None else Image.new("L", r.size, 255)
        Image.merge("RGB", (a, r, Image.new("L", r.size, 0))).save(d / "arm.jpg", quality=92)
        report["textures"]["grass004"] = {"source": "ambientCG Grass004 (CC0)", "ao": ao is not None}
        print("TEX grass004 (ambientCG) OK")
    # Niebo: to samo co dziś w mieście (kloofendal_48d_partly_cloudy_puresky z biblioteki właściciela),
    # ale ostre: 4096x2048 zamiast 2048x1024 (wersja tonemapped JPG z Poly Haven).
    sky_id = "kloofendal_48d_partly_cloudy_puresky"
    d = ROOT / "sky"
    if (d / "kloofendal_4k.jpg").exists():
        print("SKY SKIP (jest)")
    else:
        f = files(sky_id)
        url = f["tonemapped"]["url"]
        d.mkdir(parents=True, exist_ok=True)
        raw = d / "sky_full.jpg"
        raw.write_bytes(get(url))
        im = Image.open(raw).convert("RGB")
        im = im.resize((4096, 2048), Image.LANCZOS)
        im.save(d / "kloofendal_4k.jpg", quality=92)
        raw.unlink()
    report["sky"] = {"id": sky_id, "size": [4096, 2048]}
    print("SKY", sky_id, "4096x2048 OK")
    (ROOT / "polyhaven-receipt.json").write_text(json.dumps(report, indent=1))
    (ROOT / "LICENSE-CC0.txt").write_text(
        "Wszystkie pliki w tym katalogu pochodzą z https://polyhaven.com i są na licencji CC0.\n")
    return 0


if __name__ == "__main__":
    sys.exit(main())
