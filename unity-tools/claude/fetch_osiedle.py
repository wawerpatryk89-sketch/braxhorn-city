#!/usr/bin/env python3
"""BRAXHORN R2 (osiedle): prawdziwe materiały CC0 do rozbudowy i remontu.

Tekstury Poly Haven 2K -> Assets/Source/Claude/Osiedle/textures/<id>/
  albedo.jpg (kolor, sRGB), normal.jpg (OpenGL), mg.png (R=metal, A=gładkość), ao.jpg
Modele Poly Haven glTF 1K -> Assets/Source/Claude/Osiedle/models/<id>/<id>.gltf (+bin, tekstury)
Trawa ambientCG Grass004 (CC0) w tym samym układzie.
Brak któregokolwiek pliku = błąd buildu (żadnych zamienników).
"""
import io
import json
import pathlib
import time
import urllib.request
import zipfile

from PIL import Image

UA = {"User-Agent": "BraxhornCity-UnityBuild/1.0 (github.com/wawerpatryk89-sketch/braxhorn-city)"}
ROOT = pathlib.Path("unity-city/Assets/Source/Claude/Osiedle")

TEXTURES = {
    "asphalt_04": "jezdnia (gładki, ciemny asfalt osiedlowy)",
    "concrete_pavers_02": "chodniki",
    "concrete_pavement_02": "krawężniki, płyty ścieżek, obrzeże basenu",
    "herringbone_brick_02": "podjazdy",
    "concrete_pavers_03": "aleje parku",
    "white_stucco_02": "tynk elewacji",
    "weathered_planks": "deski elewacji i płotów",
    "white_sandstone_blocks_02": "kamień elewacji",
    "rough_block_wall": "cokoły, słupki i murki płotów",
    "bicolour_gravel": "żwir dachów płaskich",
    "wood_floor_deck": "tarasy",
    "blue_floor_tiles_01": "niecka basenu",
}
MODELS = [
    "street_lamp_02",
    "painted_wooden_bench",
    "metal_trash_can",
    "planter_box_02",
    "outdoor_table_chair_set_01",
    "wooden_picnic_table",
]


def get(url: str) -> bytes:
    for attempt in range(5):
        try:
            req = urllib.request.Request(url, headers=UA)
            with urllib.request.urlopen(req, timeout=240) as r:
                return r.read()
        except Exception as exc:  # noqa: BLE001
            if attempt == 4:
                raise
            print(f"retry {attempt + 1} {url}: {exc}")
            time.sleep(4 * (attempt + 1))
    raise RuntimeError(url)


def files(asset: str) -> dict:
    return json.loads(get(f"https://api.polyhaven.com/files/{asset}"))


def pick(res: dict, *keys):
    for k in keys:
        if k in res:
            return res[k]
    raise KeyError(f"brak rozdzielczości {keys} w {list(res)}")


def img(data: bytes) -> Image.Image:
    return Image.open(io.BytesIO(data))


def save_set(d: pathlib.Path, color: Image.Image, normal: Image.Image, rough: Image.Image,
             metal: Image.Image | None, ao: Image.Image | None) -> None:
    d.mkdir(parents=True, exist_ok=True)
    size = color.size
    color.convert("RGB").save(d / "albedo.jpg", quality=92)
    normal.convert("RGB").resize(size).save(d / "normal.jpg", quality=95)
    r = rough.convert("L").resize(size)
    smooth = r.point(lambda v: 255 - v)
    m = metal.convert("L").resize(size) if metal is not None else Image.new("L", size, 0)
    Image.merge("RGBA", (m, m, m, smooth)).save(d / "mg.png", optimize=True)
    (ao.convert("L").resize(size) if ao is not None else Image.new("L", size, 255)).save(d / "ao.jpg", quality=92)


def main() -> int:
    report = {"source": "Poly Haven + ambientCG (CC0)", "textures": {}, "models": {}}
    for t, role in TEXTURES.items():
        f = files(t)
        def m(key):
            return img(get(pick(f[key], "2k")["jpg"]["url"]))
        arm = m("arm") if "arm" in f else None
        if arm is not None:
            ao_c, rough_c, metal_c = arm.convert("RGB").split()
        else:
            ao_c, rough_c, metal_c = (m("AO") if "AO" in f else None), m("Rough"), None
        save_set(ROOT / "textures" / t, m("Diffuse"), m("nor_gl"), rough_c, metal_c, ao_c)
        report["textures"][t] = role
        print("TEX", t, "OK", role)
    for mid in MODELS:
        f = files(mid)
        g = pick(f["gltf"], "1k", "2k")["gltf"]
        d = ROOT / "models" / mid
        d.mkdir(parents=True, exist_ok=True)
        (d / f"{mid}.gltf").write_bytes(get(g["url"]))
        n = 1
        for rel, info in (g.get("include") or {}).items():
            p = d / rel
            p.parent.mkdir(parents=True, exist_ok=True)
            p.write_bytes(get(info["url"]))
            n += 1
        report["models"][mid] = n
        print("MODEL", mid, "files", n)
    z = zipfile.ZipFile(io.BytesIO(get("https://ambientcg.com/get?file=Grass004_2K-JPG.zip")))
    names = z.namelist()
    def member(token):
        for n in names:
            if token in n and n.lower().endswith((".jpg", ".png")):
                return img(z.read(n))
        return None
    color, normal, rough, ao = member("_Color"), member("_NormalGL"), member("_Roughness"), member("_AmbientOcclusion")
    if color is None or normal is None or rough is None:
        raise RuntimeError("Grass004: brak map: " + ", ".join(names))
    save_set(ROOT / "textures" / "grass004", color, normal, rough, None, ao)
    report["textures"]["grass004"] = "trawniki (ambientCG)"
    print("TEX grass004 OK")
    # Niebo dzienne z chmurami (to samo co w mieście): Poly Haven kloofendal_48d_partly_cloudy_puresky,
    # wersja tonemapped JPG przeskalowana do 4096x2048.
    sky = files("kloofendal_48d_partly_cloudy_puresky")["tonemapped"]["url"]
    (ROOT / "sky").mkdir(parents=True, exist_ok=True)
    img(get(sky)).convert("RGB").resize((4096, 2048), Image.LANCZOS).save(ROOT / "sky" / "niebo_4k.jpg", quality=92)
    report["textures"]["sky"] = "kloofendal_48d_partly_cloudy_puresky (Poly Haven CC0)"
    print("SKY OK")
    # Szyldy sklepów (bez świecenia): grafit, ciepłe białe litery, cienka ramka. Czcionka DejaVu (Ubuntu).
    from PIL import ImageDraw, ImageFont
    font_path = next((p for p in ("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",
                                  "/usr/share/fonts/truetype/dejavu/DejaVuSansCondensed-Bold.ttf") if pathlib.Path(p).exists()), None)
    (ROOT / "signs").mkdir(parents=True, exist_ok=True)
    for name, text in {"butik": "BUTIK  NYXA", "shake": "SHAKE BAR", "kawiarnia": "KAWIARNIA", "market": "MARKET 24",
                       "kort": "KLUB TENISOWY", "ochrona": "OCHRONA"}.items():
        im = Image.new("RGB", (1024, 256), (34, 37, 40)); dr = ImageDraw.Draw(im)
        dr.rectangle([10, 10, 1013, 245], outline=(176, 150, 104), width=6)
        f = ImageFont.truetype(font_path, 118) if font_path else ImageFont.load_default()
        w = dr.textlength(text, font=f); dr.text(((1024 - w) / 2, 58), text, font=f, fill=(236, 228, 214))
        im.save(ROOT / "signs" / f"{name}.png")
    report["textures"]["signs"] = "szyldy generowane (DejaVu Sans Bold)"
    print("SIGNS OK", font_path)
    (ROOT / "osiedle-sources.json").write_text(json.dumps(report, indent=1, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
