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
    # 3.11 (właściciel: „panorama w HD, 360 stopni, zajebista”, „niebo kwadratowe”): w grze było 2048x1024 (stary skybox),
    # teraz HD 8192x4096 (albo pełna szerokość pliku tonemapped, jeśli mniejsza) + 4096x2048 jako zapas.
    sky_id = "kloofendal_48d_partly_cloudy_puresky"
    d = ROOT / "sky"
    if (d / "kloofendal_8k.jpg").exists() and (d / "kloofendal_4k.jpg").exists():
        print("SKY SKIP (jest)")
    else:
        f = files(sky_id)
        url = f["tonemapped"]["url"]
        d.mkdir(parents=True, exist_ok=True)
        raw = d / "sky_full.jpg"
        raw.write_bytes(get(url))
        Image.MAX_IMAGE_PIXELS = None
        im = Image.open(raw).convert("RGB")
        print("SKY źródło tonemapped", im.size)
        w8 = min(8192, im.size[0] // 2 * 2)
        im.resize((w8, w8 // 2), Image.LANCZOS).save(d / "kloofendal_8k.jpg", quality=95, subsampling=0)
        im.resize((4096, 2048), Image.LANCZOS).save(d / "kloofendal_4k.jpg", quality=92)
        raw.unlink()
    with Image.open(d / "kloofendal_8k.jpg") as hd:
        report["sky"] = {"id": sky_id, "hd": list(hd.size), "zapas": [4096, 2048]}
        print("SKY", sky_id, "HD", hd.size, "OK")
    # 3.11 (właściciel: „znaki normalne prawdziwe na drogach… nie można tego pobrać?”): polskie znaki drogowe — wzory urzędowe
    # z Wikimedia Commons („PL road sign <kod>.svg”, domena publiczna), PNG 960 px (standardowa szerokość miniatury Commons) z przezroczystym tłem (kształt tarczy w alfie).
    # Brak któregoś = ostrzeżenie (gra stawia wtedy znak z kodu i loguje DO POPRAWKI), build nie pada.
    dz = ROOT / "znaki_pl"
    dz.mkdir(parents=True, exist_ok=True)
    znaki_ok, znaki_brak = [], []
    for kod in ["A-7", "A-16", "B-1", "B-2", "B-20", "B-36", "C-2", "C-9", "C-12", "D-1", "D-3", "D-6", "D-18"]:
        out = dz / f"{kod}.png"
        if out.exists() and out.stat().st_size > 1500:
            znaki_ok.append(kod); continue
        url = f"https://commons.wikimedia.org/wiki/Special:FilePath/PL_road_sign_{kod}.svg?width=960"
        data = None
        for proba in range(3):
            try:
                with urllib.request.urlopen(urllib.request.Request(url, headers=UA), timeout=60) as r:
                    data = r.read()
                break
            except Exception as exc:  # noqa: BLE001
                print(f"ZNAK {kod} próba {proba + 1}: {exc}")
                time.sleep(4 * (proba + 1))
        try:
            im = Image.open(io.BytesIO(data)).convert("RGBA") if data else None
        except Exception as exc:  # noqa: BLE001
            print(f"ZNAK {kod}: nie obraz ({exc})"); im = None
        if im is None or im.getextrema()[3][0] > 10:   # bez przezroczystego tła = nie wzór tarczy
            znaki_brak.append(kod); continue
        im.save(out)
        znaki_ok.append(kod)
    (dz / "ZRODLO.txt").write_text("Polskie znaki drogowe: Wikimedia Commons, pliki „PL road sign <kod>.svg” (wzory urzędowe, domena publiczna).\n")
    report["znaki_pl"] = {"ok": znaki_ok, "brak": znaki_brak}
    print("ZNAKI_PL ok", len(znaki_ok), "brak", znaki_brak)
    (ROOT / "polyhaven-receipt.json").write_text(json.dumps(report, indent=1))
    (ROOT / "LICENSE-CC0.txt").write_text(
        "Pliki w tym katalogu pochodzą z https://polyhaven.com (CC0); wyjątek: znaki_pl/ — Wikimedia Commons, domena publiczna (znaki_pl/ZRODLO.txt).\n")
    return 0


if __name__ == "__main__":
    sys.exit(main())
