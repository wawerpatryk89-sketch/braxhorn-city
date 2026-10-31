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
    "roof_slates_03": "dach dwuspadowy (łupek grafitowy)",
    "clay_roof_tiles_02": "dach kopertowy (dachówka ceramiczna)",
    "exterior_wall_cladding_02": "klinkier elewacyjny (parter willi, pierzeja sklepów)",
    "yellow_flower_mulch": "ściółka rabat",
}
MODELS = [
    "street_lamp_01",       # latarnia parkowa
    "modular_street_seating",  # ławka parkowa (listwy + żeliwo), składana z części
    "outdoor_table_chair_set_01",
    "wooden_picnic_table",
    "grass_bermuda_01",     # kępy trawy przy płotach, drzewach i ścieżkach
    "grass_medium_02",
    "flower_gazania",       # kwiaty na rabatach
    "dandelion_01",
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
        size = 118
        f = ImageFont.truetype(font_path, size) if font_path else ImageFont.load_default()
        while font_path and dr.textlength(text, font=f) > 930 and size > 40:
            size -= 4; f = ImageFont.truetype(font_path, size)
        w = dr.textlength(text, font=f); dr.text(((1024 - w) / 2, (256 - size) / 2 - 6), text, font=f, fill=(236, 228, 214))
        im.save(ROOT / "signs" / f"{name}.png")
    report["textures"]["signs"] = "szyldy generowane (DejaVu Sans Bold)"
    print("SIGNS OK", font_path)
    # Źdźbła trawy: atlas 2048x512 = 4 odmiany kępy (świeża, ciemniejsza, z suchszymi końcówkami, gęsta niska).
    import math, random
    rnd = random.Random(31)
    g = Image.new("RGBA", (2048, 512), (0, 0, 0, 0)); dg = ImageDraw.Draw(g)
    variants = [((34, 66, 22), (118, 160, 66), 70, 1.0), ((26, 52, 18), (92, 132, 52), 80, .9),
                ((40, 66, 24), (150, 160, 86), 64, 1.0), ((34, 64, 22), (110, 152, 60), 110, .7)]
    for v, (base0, tip0, count, hs) in enumerate(variants):
        ox = v * 512
        for k in range(count):
            x0 = ox + rnd.uniform(30, 482); h = rnd.uniform(220, 500) * hs; bend = rnd.uniform(-80, 80); w0 = rnd.uniform(6, 13)
            base = tuple(max(0, min(255, c + rnd.randint(-8, 8))) for c in base0); tip = tuple(max(0, min(255, c + rnd.randint(-14, 14))) for c in tip0)
            prev = None
            for i2 in range(15):
                t = i2 / 14; y = 511 - t * h; x = x0 + bend * t * t; w = w0 * (1 - t) + .7
                if prev:
                    col = tuple(int(base[c] + (tip[c] - base[c]) * t) for c in range(3)) + (255,)
                    dg.polygon([(prev[0] - prev[2], prev[1]), (prev[0] + prev[2], prev[1]), (x + w, y), (x - w, y)], fill=col)
                prev = (x, y, w)
    (ROOT / "detail").mkdir(parents=True, exist_ok=True)
    g.save(ROOT / "detail" / "zdzbla.png")
    # Kwiaty rabat: atlas 2048x512 = 4 gatunki (pelargonie czerwone, nagietki żółte, stokrotki białe, lawenda).
    fl = Image.new("RGBA", (2048, 512), (0, 0, 0, 0)); df = ImageDraw.Draw(fl)
    def stem(ox, x0, h, col):
        bend = rnd.uniform(-30, 30); prev = None
        for i2 in range(9):
            t = i2 / 8; y = 511 - t * h; x = x0 + bend * t * t; w = 4.5 * (1 - t) + 1.5
            if prev: df.polygon([(prev[0] - prev[2], prev[1]), (prev[0] + prev[2], prev[1]), (x + w, y), (x - w, y)], fill=col)
            prev = (x, y, w)
        return prev[0], prev[1]
    def leaf(cx, cy, l, ang, col):
        pts = []
        for i2 in range(13):
            t = i2 / 12; r = math.sin(t * math.pi) * l * .32; a = ang + math.pi / 2
            px = cx + math.cos(ang) * l * t; py = cy - math.sin(ang) * l * t
            pts.append((px + math.cos(a) * r, py - math.sin(a) * r))
        for i2 in range(12, -1, -1):
            t = i2 / 12; r = math.sin(t * math.pi) * l * .32; a = ang - math.pi / 2
            px = cx + math.cos(ang) * l * t; py = cy - math.sin(ang) * l * t
            pts.append((px + math.cos(a) * r, py - math.sin(a) * r))
        df.polygon(pts, fill=col)
    green = [(46, 92, 34, 255), (58, 108, 40, 255), (38, 80, 30, 255)]
    for v in range(4):
        ox = v * 512
        for k in range(20):   # liście u podstawy
            leaf(ox + rnd.uniform(60, 452), 511 - rnd.uniform(0, 90), rnd.uniform(70, 130), rnd.uniform(.3, 2.8), rnd.choice(green))
        for k in range(26 if v != 3 else 30):
            x0 = ox + rnd.uniform(50, 462); h = rnd.uniform(250, 440) if v != 3 else rnd.uniform(320, 490)
            tx, ty = stem(ox, x0, h, rnd.choice(green))
            if v == 0:      # pelargonia: kula drobnych czerwonych kwiatków
                for q in range(16):
                    a = rnd.uniform(0, 6.283); d = rnd.uniform(0, 30); r = rnd.uniform(9, 13)
                    c = (rnd.randint(190, 225), rnd.randint(28, 52), rnd.randint(40, 60), 255)
                    df.ellipse([tx + math.cos(a) * d - r, ty + math.sin(a) * d * .8 - r, tx + math.cos(a) * d + r, ty + math.sin(a) * d * .8 + r], fill=c)
            elif v in (1, 2):   # nagietek / stokrotka: płatki promieniście + środek
                pc = (rnd.randint(235, 250), rnd.randint(160, 200), rnd.randint(20, 40), 255) if v == 1 else (rnd.randint(238, 250), rnd.randint(238, 248), rnd.randint(230, 242), 255)
                cc = (150, 80, 20, 255) if v == 1 else (230, 190, 40, 255)
                R = rnd.uniform(24, 32)
                for q in range(12):
                    a = q * 6.283 / 12 + rnd.uniform(-.1, .1)
                    df.ellipse([tx + math.cos(a) * R * .55 - R * .32, ty + math.sin(a) * R * .45 - R * .2, tx + math.cos(a) * R * .55 + R * .32, ty + math.sin(a) * R * .45 + R * .2], fill=pc)
                df.ellipse([tx - R * .3, ty - R * .25, tx + R * .3, ty + R * .25], fill=cc)
            else:               # lawenda: kłos z drobnych fioletowych kwiatków
                for q in range(14):
                    yy = ty + q * 7; r = 7 - q * .25
                    c = (rnd.randint(118, 150), rnd.randint(86, 110), rnd.randint(180, 215), 255)
                    df.ellipse([tx - r + rnd.uniform(-2, 2), yy - r, tx + r + rnd.uniform(-2, 2), yy + r], fill=c)
    fl.save(ROOT / "detail" / "kwiaty.png")
    # Kropla wody (miękka, półprzezroczysta, bez świecenia) do fontanny.
    d = Image.new("RGBA", (64, 64), (0, 0, 0, 0)); px = d.load()
    for yy in range(64):
        for xx in range(64):
            r = math.hypot(xx - 31.5, yy - 31.5) / 31.5
            if r < 1: px[xx, yy] = (235, 245, 255, int(255 * (1 - r) ** 1.6))
    d.save(ROOT / "detail" / "kropla.png")
    # Krąg na wodzie (fala) do fontanny.
    ring = Image.new("RGBA", (128, 128), (0, 0, 0, 0)); pr = ring.load()
    for yy in range(128):
        for xx in range(128):
            r = math.hypot(xx - 63.5, yy - 63.5) / 63.5
            a = max(0.0, 1 - abs(r - .82) / .12)
            if r < 1 and a > 0: pr[xx, yy] = (240, 248, 255, int(200 * a))
    ring.save(ROOT / "detail" / "fala.png")
    print("DETAIL OK")
    (ROOT / "osiedle-sources.json").write_text(json.dumps(report, indent=1, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
