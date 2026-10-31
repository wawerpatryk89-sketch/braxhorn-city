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
    "large_grey_tiles": "posadzka komisariatu",
    "painted_plaster_wall": "ściany wnętrz (farba)",
}
MODELS = [
    "street_lamp_01",       # latarnia parkowa
    "modular_street_seating",  # ławka parkowa (listwy + żeliwo), składana z części
    "outdoor_table_chair_set_01",
    "metal_office_desk",    # komisariat: biurka
    "dining_chair_02",      # komisariat: krzesła
    "modern_arm_chair_01",  # komisariat: poczekalnia
    "sofa_02",              # komisariat: gabinet
    "modern_ceiling_lamp_01",
    "drawer_cabinet",
    "office_notepads",
    "standing_chalkboard_01",
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
    for name, text in {"butik": "BUTIK  NYXA", "shake": "ODŻYWKI · SHAKE BAR", "kawiarnia": "KAWIARNIA", "market": "MARKET 24",
                       "kort": "KLUB TENISOWY", "ochrona": "OCHRONA", "policja": "KOMISARIAT POLICJI", "silownia": "SIŁOWNIA PLENEROWA"}.items():
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
    # Pokoje za szybami (atlas 2048x1024 = 8 tylnych ścian 512x512, 3.6 m x 3.4 m): meble na przezroczystym tle,
    # kolor ściany i boki pokoju dokłada shader okna. 0–4 mieszkania, 5–7 sklepy.
    import math, random
    R = random.Random(77)
    at = Image.new("RGBA", (2048, 1024), (0, 0, 0, 0)); da = ImageDraw.Draw(at)
    PX = 512 / 3.6; PY = 512 / 3.4
    def rect(ox, oy, x0, y0, x1, y1, col):   # metry od lewego dolnego rogu ściany
        da.rectangle([ox + x0 * PX, oy + 511 - y1 * PY, ox + x1 * PX, oy + 511 - y0 * PY], fill=col)
    def rrect(ox, oy, x0, y0, x1, y1, col, r=.06):
        da.rounded_rectangle([ox + x0 * PX, oy + 511 - y1 * PY, ox + x1 * PX, oy + 511 - y0 * PY], radius=int(r * PX), fill=col)
    def ell(ox, oy, cx, cy, rx, ry, col):
        da.ellipse([ox + (cx - rx) * PX, oy + 511 - (cy + ry) * PY, ox + (cx + rx) * PX, oy + 511 - (cy - ry) * PY], fill=col)
    def shade(c, k): return tuple(max(0, min(255, int(v * k))) for v in c[:3]) + (255,)
    def painting(ox, oy, x0, y0, w, h):
        rect(ox, oy, x0 - .04, y0 - .04, x0 + w + .04, y0 + h + .04, (40, 36, 32, 255))
        rect(ox, oy, x0, y0, x0 + w, y0 + h, (236, 230, 218, 255))
        for k in range(5):
            c = R.choice([(196, 90, 60), (60, 110, 150), (220, 180, 80), (90, 130, 90), (40, 50, 70)])
            ell(ox, oy, x0 + R.uniform(.15, w - .15), y0 + R.uniform(.15, h - .15), R.uniform(.08, .25), R.uniform(.06, .2), c + (255,))
    def plant(ox, oy, x, s=1):
        rect(ox, oy, x - .14 * s, 0, x + .14 * s, .32 * s, (150, 96, 70, 255))
        for k in range(14):
            ell(ox, oy, x + R.uniform(-.28, .28) * s, .35 * s + R.uniform(0, .7) * s, R.uniform(.06, .13) * s, R.uniform(.05, .1) * s, R.choice([(52, 104, 46), (70, 128, 58), (40, 86, 40)]) + (255,))
    def lamp(ox, oy, x):
        rect(ox, oy, x - .015, 0, x + .015, 1.45, (40, 40, 40, 255)); rect(ox, oy, x - .14, 0, x + .14, .03, (40, 40, 40, 255))
        da.polygon([(ox + (x - .2) * PX, oy + 511 - 1.4 * PY), (ox + (x + .2) * PX, oy + 511 - 1.4 * PY), (ox + (x + .13) * PX, oy + 511 - 1.72 * PY), (ox + (x - .13) * PX, oy + 511 - 1.72 * PY)], fill=(236, 226, 200, 255))
    def books(ox, oy, x0, x1, y0, y1):
        x = x0
        while x < x1 - .03:
            w = R.uniform(.03, .07); h = R.uniform(.6, .95) * (y1 - y0)
            rect(ox, oy, x, y0, min(x + w, x1), y0 + h, R.choice([(150, 40, 40), (40, 70, 120), (200, 170, 90), (60, 100, 70), (90, 60, 40), (220, 220, 210)]) + (255,)); x += w + .006
    tiles = []
    # 0 salon: kanapa, obraz, lampa, roślina, stolik
    def t0(ox, oy):
        sofa = (R.choice([(70, 86, 110), (120, 100, 80), (90, 96, 90)]) + (255,))
        rrect(ox, oy, .7, .1, 2.9, .48, shade(sofa, .85)); rrect(ox, oy, .7, .4, 2.9, .9, sofa); rrect(ox, oy, .55, .1, .85, .66, shade(sofa, .9)); rrect(ox, oy, 2.75, .1, 3.05, .66, shade(sofa, .9))
        rect(ox, oy, .7, 0, .78, .1, (40, 30, 25, 255)); rect(ox, oy, 2.82, 0, 2.9, .1, (40, 30, 25, 255))
        painting(ox, oy, 1.2, 1.35, 1.2, .8); lamp(ox, oy, .3); plant(ox, oy, 3.3)
    # 1 sypialnia: łóżko, szafa, obraz
    def t1(ox, oy):
        rect(ox, oy, .35, 0, 1.35, 2.1, (200, 186, 160, 255)); rect(ox, oy, .84, .05, .86, 2.05, (120, 106, 90, 255)); rect(ox, oy, .74, .9, .78, 1.2, (80, 70, 60, 255)); rect(ox, oy, .92, .9, .96, 1.2, (80, 70, 60, 255))
        rect(ox, oy, 1.7, 0, 3.4, .55, (230, 228, 222, 255)); rect(ox, oy, 1.7, .4, 3.4, .55, R.choice([(120, 140, 170), (170, 120, 110), (140, 150, 120)]) + (255,))
        rect(ox, oy, 1.65, 0, 3.45, 1.05, (110, 84, 64, 255)); rect(ox, oy, 1.75, .1, 3.35, .95, (128, 100, 78, 255)); rect(ox, oy, 1.7, 0, 3.4, .55, (232, 230, 224, 255)); rect(ox, oy, 1.7, .32, 3.4, .55, R.choice([(120, 140, 170), (170, 120, 110), (140, 150, 120)]) + (255,))
        painting(ox, oy, 2.1, 1.45, .9, .6)
    # 2 kuchnia: szafki dolne i górne, blat, lodówka
    def t2(ox, oy):
        front = R.choice([(236, 234, 228), (60, 70, 66), (180, 160, 130)])
        rect(ox, oy, 0, 0, 2.7, .88, front + (255,)); rect(ox, oy, 0, .88, 2.75, .93, (50, 48, 46, 255))
        for x in [0, .6, 1.2, 1.8, 2.4]: rect(ox, oy, x + .29, .1, x + .31, .8, shade(front, .8))
        rect(ox, oy, 0, 1.45, 2.7, 2.15, front + (255,))
        for x in [0, .9, 1.8]: rect(ox, oy, x + .44, 1.47, x + .46, 2.13, shade(front, .8))
        rect(ox, oy, .2, .93, 2.5, 1.45, (210, 214, 214, 255))
        rect(ox, oy, 2.8, 0, 3.5, 2.0, (200, 202, 204, 255)); rect(ox, oy, 2.8, 1.2, 3.5, 1.23, (150, 150, 150, 255)); rect(ox, oy, 2.85, 1.3, 2.88, 1.7, (120, 120, 120, 255))
    # 3 gabinet: regał z książkami, biurko, krzesło
    def t3(ox, oy):
        rect(ox, oy, .2, 0, 1.9, 2.3, (120, 92, 66, 255))
        for k in range(5):
            y = .1 + k * .44; rect(ox, oy, .26, y, 1.84, y + .03, (100, 76, 54, 255)); books(ox, oy, .3, 1.8, y + .03, y + .4)
        rect(ox, oy, 2.2, .72, 3.4, .76, (60, 50, 44, 255)); rect(ox, oy, 2.25, 0, 2.3, .72, (40, 40, 40, 255)); rect(ox, oy, 3.3, 0, 3.35, .72, (40, 40, 40, 255))
        rect(ox, oy, 2.6, .76, 3.0, 1.06, (30, 30, 34, 255)); rrect(ox, oy, 2.55, .42, 3.05, 1.0, (50, 50, 56, 255)); lamp(ox, oy, 3.45)
    # 4 jadalnia: stół, krzesła, lampa wisząca, kredens
    def t4(ox, oy):
        rect(ox, oy, .2, 0, 1.3, .9, (150, 120, 90, 255)); rect(ox, oy, .25, .9, 1.25, 1.6, (170, 140, 110, 255)); rect(ox, oy, .3, .95, .74, 1.55, (190, 200, 205, 255)); rect(ox, oy, .76, .95, 1.2, 1.55, (190, 200, 205, 255))
        rect(ox, oy, 1.6, .72, 3.3, .78, (110, 80, 56, 255)); rect(ox, oy, 1.7, 0, 1.75, .72, (90, 66, 46, 255)); rect(ox, oy, 3.15, 0, 3.2, .72, (90, 66, 46, 255))
        for x in [1.9, 2.5, 3.0]: rect(ox, oy, x, 0, x + .05, .9, (60, 50, 44, 255)); rect(ox, oy, x - .05, .42, x + .35, .46, (60, 50, 44, 255))
        rect(ox, oy, 2.44, 1.7, 2.46, 3.4, (40, 40, 40, 255)); ell(ox, oy, 2.45, 1.62, .28, .12, (230, 220, 196, 255)); painting(ox, oy, 2.0, 1.95, .9, .55)
    # 5 sklep odzieżowy: wieszaki z ubraniami, półki
    def t5(ox, oy):
        for bx in [.2, 1.9]:
            rect(ox, oy, bx, 1.55, bx + 1.5, 1.58, (50, 50, 50, 255))
            x = bx + .05
            while x < bx + 1.42:
                c = R.choice([(30, 30, 34), (200, 196, 186), (140, 40, 50), (60, 80, 120), (170, 150, 120), (90, 110, 80)])
                rect(ox, oy, x, .7, x + .09, 1.5, c + (255,)); x += .12
        rect(ox, oy, .1, 2.0, 3.5, 2.05, (200, 196, 190, 255))
        for x in [.3, 1.1, 1.9, 2.7]: rect(ox, oy, x, 2.05, x + .6, 2.35, R.choice([(220, 210, 190), (60, 60, 70), (160, 50, 50)]) + (255,))
    # 6 kawiarnia: lada, ekspres, tablica menu, półki z kubkami
    def t6(ox, oy):
        rect(ox, oy, .3, 0, 3.3, 1.05, (90, 66, 46, 255)); rect(ox, oy, .25, 1.05, 3.35, 1.1, (40, 40, 40, 255))
        rect(ox, oy, 1.2, 1.1, 1.8, 1.55, (170, 172, 176, 255)); rect(ox, oy, 1.25, 1.3, 1.75, 1.45, (60, 60, 64, 255))
        rect(ox, oy, .5, 1.9, 1.9, 2.7, (34, 36, 34, 255))
        for k in range(5): rect(ox, oy, .6, 2.0 + k * .13, R.uniform(1.2, 1.8), 2.04 + k * .13, (220, 220, 210, 255))
        for y in [1.75, 2.15, 2.55]:
            rect(ox, oy, 2.2, y, 3.3, y + .03, (120, 92, 66, 255))
            for x in range(6): rect(ox, oy, 2.28 + x * .17, y + .03, 2.38 + x * .17, y + .15, R.choice([(236, 232, 224), (190, 80, 60), (60, 90, 110)]) + (255,))
    # 7 market: regały z towarem
    def t7(ox, oy):
        for bx in [.1, 1.85]:
            rect(ox, oy, bx, 0, bx + 1.65, 2.0, (200, 200, 196, 255))
            for k in range(5):
                y = .15 + k * .38; rect(ox, oy, bx + .03, y, bx + 1.62, y + .03, (150, 150, 150, 255))
                x = bx + .06
                while x < bx + 1.55:
                    w = R.uniform(.06, .14); h = R.uniform(.15, .3)
                    rect(ox, oy, x, y + .03, x + w, y + .03 + h, R.choice([(200, 40, 40), (240, 200, 40), (40, 120, 60), (40, 80, 160), (240, 240, 230), (230, 120, 40)]) + (255,)); x += w + .01
    for k, fn in enumerate([t0, t1, t2, t3, t4, t5, t6, t7]):
        fn((k % 4) * 512, (k // 4) * 512)
    at.save(ROOT / "detail" / "pokoje.png")
    print("DETAIL OK")
    (ROOT / "osiedle-sources.json").write_text(json.dumps(report, indent=1, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
