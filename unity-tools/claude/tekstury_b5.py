# Build 5 — tekstury generowane (bez cudzych grafik): front kolumny, front wzmacniacza, wnętrze kasyna.
# Wywołanie: python3 tex5.py <katalog_signs>. Wszystko matowe/odbijające, nic nie świeci (tylko kolor bazowy).
import sys, math, pathlib, random
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter

FB = next((p for p in ("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",) if pathlib.Path(p).exists()), None)
FS = next((p for p in ("/usr/share/fonts/truetype/dejavu/DejaVuSerif-Bold.ttf",) if pathlib.Path(p).exists()), FB)
FR = next((p for p in ("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",) if pathlib.Path(p).exists()), FB)
def font(path, size): return ImageFont.truetype(path, size) if path else ImageFont.load_default()

def radial(img, cx, cy, r, c_in, c_out, ring=None):
    """Koło z gradientem promieniowym (stożek głośnika, gałka)."""
    W, H = img.size; a = np.asarray(img).astype(np.float32)
    yy, xx = np.mgrid[0:H, 0:W]; d = np.hypot(xx - cx, yy - cy) / r
    m = d <= 1
    t = np.clip(d, 0, 1)[..., None]
    col = np.array(c_in, np.float32) * (1 - t) + np.array(c_out, np.float32) * t
    a[m] = col[m]
    return Image.fromarray(np.clip(a, 0, 255).astype(np.uint8), "RGB")

def speaker(out):
    W, H = 256, 1024
    rnd = np.random.default_rng(5)
    base = np.zeros((H, W, 3), np.float32); base[:] = (10, 10, 12)
    base += rnd.normal(0, 1.2, (H, W, 1))
    img = Image.fromarray(np.clip(base, 0, 255).astype(np.uint8), "RGB")
    def driver(cy, r, tweeter=False):
        nonlocal img
        d = ImageDraw.Draw(img)
        d.ellipse([W/2 - r - 10, cy - r - 10, W/2 + r + 10, cy + r + 10], fill=(150, 152, 158))      # pierścień aluminiowy
        d.ellipse([W/2 - r - 4, cy - r - 4, W/2 + r + 4, cy + r + 4], fill=(205, 207, 212))
        d.ellipse([W/2 - r, cy - r, W/2 + r, cy + r], fill=(30, 30, 32))                             # zawieszenie (guma)
        if tweeter:
            img = radial(img, W/2, cy, r * .78, (236, 236, 240), (120, 122, 128))                    # kopułka
        else:
            img = radial(img, W/2, cy, r * .86, (74, 74, 78), (40, 40, 44))                          # stożek
            for k in range(1, 6):                                                                     # przetłoczenia stożka
                ImageDraw.Draw(img).ellipse([W/2 - r*.86*k/6, cy - r*.86*k/6, W/2 + r*.86*k/6, cy + r*.86*k/6], outline=(58, 58, 62), width=1)
            img = radial(img, W/2, cy, r * .3, (150, 152, 158), (70, 72, 78))                         # nakładka przeciwpyłowa
    driver(150, 34, True)
    driver(390, 92); driver(640, 92)
    d = ImageDraw.Draw(img)
    for x in (W/2 - 18, W/2 + 18): d.ellipse([x - 14, 860 - 14, x + 14, 860 + 14], fill=(4, 4, 5), outline=(70, 70, 76), width=2)   # bass-reflex
    f = font(FB, 26); t = "BRAXHORN"; tw = d.textlength(t, font=f); d.text(((W - tw) / 2, 950), t, font=f, fill=(188, 190, 196))
    img.save(out)

def amplifier(out):
    W, H = 1024, 303
    rnd = np.random.default_rng(7)
    streak = rnd.normal(0, 1, (1, W)).repeat(H, 0) * 6 + rnd.normal(0, 1, (H, W)) * 2      # aluminium szczotkowane poziomo
    a = np.zeros((H, W, 3), np.float32); a[:] = (176, 178, 182); a += streak[..., None]
    img = Image.fromarray(np.clip(a, 0, 255).astype(np.uint8), "RGB"); d = ImageDraw.Draw(img)
    d.rectangle([0, 0, W - 1, H - 1], outline=(110, 112, 118), width=4)
    # dwa wskaźniki VU (kremowa tarcza, skala, czarna wskazówka) w czarnych okienkach — rysowane w okienku i wklejane
    for cx in (210, 470):
        x0, y0, x1, y1 = cx - 115, 50, cx + 115, 200
        d.rectangle([x0 - 8, y0 - 8, x1 + 8, y1 + 8], fill=(18, 18, 20))
        m = Image.new("RGB", (x1 - x0, y1 - y0), (236, 224, 190)); md = ImageDraw.Draw(m)
        px, py = (x1 - x0) / 2, (y1 - y0) + 30
        for k in range(11):
            ang = math.radians(-45 + k * 9); r0, r1 = 120, 130 if k % 2 else 140
            col = (176, 30, 30) if k >= 8 else (30, 30, 30)
            md.line([px + r0 * math.sin(ang), py - r0 * math.cos(ang), px + r1 * math.sin(ang), py - r1 * math.cos(ang)], fill=col, width=3)
        md.arc([px - 120, py - 120, px + 120, py + 120], 225, 315, fill=(40, 40, 40), width=2)
        ang = math.radians(-14 if cx < 300 else -8)
        md.line([px, py, px + 136 * math.sin(ang), py - 136 * math.cos(ang)], fill=(12, 12, 12), width=4)
        md.text((px - 16, (y1 - y0) - 42), "VU", font=font(FB, 22), fill=(60, 60, 60))
        img.paste(m, (x0, y0)); d = ImageDraw.Draw(img)
    # gałka głośności (duża), dwie małe, wyłącznik
    img = radial(img, 790, 140, 96, (226, 228, 232), (120, 122, 128)); d = ImageDraw.Draw(img)
    d.ellipse([790 - 98, 140 - 98, 790 + 98, 140 + 98], outline=(80, 82, 88), width=3)
    d.line([790, 140 - 80, 790, 140 - 50], fill=(30, 30, 30), width=6)
    for cx in (640, 940):
        img = radial(img, cx, 250, 30, (210, 212, 216), (110, 112, 118)); d = ImageDraw.Draw(img)
    d.rectangle([40, 236, 80, 266], fill=(24, 24, 26))
    f = font(FB, 30); t = "BRAXHORN  A-900"; d.text((150, 238), t, font=f, fill=(40, 40, 44))
    img.save(out)


def carpet(out):
    """Dywan kasyna 1024² (kafel bez szwu, 2 × 2 m): bordo, złote wachlarze art déco w siatce rombów, czarne obwódki."""
    W = 1024; img = Image.new("RGB", (W, W), (92, 10, 22)); d = ImageDraw.Draw(img)
    gold, dark, hi = (196, 150, 64), (30, 4, 8), (150, 22, 36)
    n = 4; c = W // n
    for j in range(-1, 2 * n + 2):
        for i in range(-1, n + 2):
            x, y = i * c + (c // 2 if j % 2 else 0), j * c // 2
            d.polygon([(x, y - c // 2), (x + c // 2, y), (x, y + c // 2), (x - c // 2, y)], outline=dark, fill=None)
            d.polygon([(x, y - c // 2 + 10), (x + c // 2 - 10, y), (x, y + c // 2 - 10), (x - c // 2 + 10, y)], outline=gold)
            for k in range(5):                                   # wachlarz
                r = 18 + k * 14
                d.arc([x - r, y - r, x + r, y + r], 200, 340, fill=gold if k % 2 == 0 else hi, width=4)
            d.ellipse([x - 6, y - 6, x + 6, y + 6], fill=gold)
    a = np.asarray(img).astype(np.float32)
    a *= (1 + np.random.default_rng(3).normal(0, .05, (W, W, 1)))   # runo
    Image.fromarray(np.clip(a, 0, 255).astype(np.uint8)).save(out)

def slot_screen(out):
    """Ekran automatu 512×384: trzy bębny z symbolami (7, BAR, wiśnie, dzwonek, romb), linia wygranej. Matowy (nie świeci)."""
    W, H = 512, 384; img = Image.new("RGB", (W, H), (20, 14, 40)); d = ImageDraw.Draw(img)
    d.rectangle([0, 0, W - 1, H - 1], outline=(196, 150, 64), width=8)
    syms = [["7", "BAR", "♥"], ["BAR", "7", "♦"], ["♣", "7", "BAR"]]
    fb = font(FB, 64); fs = font(FR, 64); fbar = font(FB, 44)
    for r in range(3):
        x0 = 30 + r * 155; d.rectangle([x0, 40, x0 + 140, H - 40], fill=(244, 240, 228))
        for k, sym in enumerate(syms[r]):
            y = 60 + k * 100 + (10 if sym == "BAR" else 0); f = fbar if sym == "BAR" else fb if sym == "7" else fs
            col = (200, 20, 30) if sym in ("7", "♥", "♦") else (20, 20, 24)
            tw = d.textlength(sym, font=f); d.text((x0 + 70 - tw / 2, y), sym, font=f, fill=col)
    d.line([20, H // 2, W - 20, H // 2], fill=(220, 40, 40), width=4)
    img.save(out)

def slot_top(out):
    W, H = 512, 192; img = Image.new("RGB", (W, H), (120, 8, 18)); d = ImageDraw.Draw(img)
    d.rectangle([6, 6, W - 7, H - 7], outline=(220, 176, 84), width=8)
    f = font(FS, 70); t = "JACKPOT"; tw = d.textlength(t, font=f); d.text(((W - tw) / 2, 18), t, font=f, fill=(240, 200, 96))
    f2 = font(FB, 34); t2 = "BRAXHORN"; tw = d.textlength(t2, font=f2); d.text(((W - tw) / 2, 112), t2, font=f2, fill=(250, 240, 220))
    img.save(out)

def bet_board(out):
    """Tablica zakładów 1024×640 (walki z mastera §6, bez wymyślonych postaci): czarna, złote nagłówki, kursy."""
    W, H = 1024, 640; img = Image.new("RGB", (W, H), (12, 12, 14)); d = ImageDraw.Draw(img)
    d.rectangle([8, 8, W - 9, H - 9], outline=(200, 160, 72), width=8)
    f = font(FS, 64); t = "ZAKŁADY  —  WALKI"; tw = d.textlength(t, font=f); d.text(((W - tw) / 2, 30), t, font=f, fill=(232, 192, 96))
    d.line([60, 120, W - 60, 120], fill=(200, 160, 72), width=3)
    fr = font(FB, 44); fo = font(FB, 44)
    rows = [("BRAX", "KROV", "1.65", "2.30"), ("NYXA", "VEXA", "1.90", "1.95"), ("BRAX", "MIŚ", "2.10", "1.75"), ("BRAX / NYXA", "DEMON", "3.40", "1.30")]
    for i, (a, b, k1, k2) in enumerate(rows):
        y = 160 + i * 108
        d.text((70, y), a, font=fr, fill=(240, 236, 226)); d.text((420, y), "vs", font=font(FR, 40), fill=(170, 170, 176)); d.text((500, y), b, font=fr, fill=(240, 236, 226))
        for x, k in ((760, k1), (890, k2)):
            d.rectangle([x - 10, y - 6, x + 110, y + 56], fill=(30, 26, 18), outline=(150, 120, 60), width=2)
            d.text((x, y), k, font=fo, fill=(232, 192, 96))
    img.save(out)

def gold_sign(out, text, w=1024, h=256):
    img = Image.new("RGB", (w, h), (10, 10, 12)); d = ImageDraw.Draw(img)
    d.rectangle([10, 10, w - 11, h - 11], outline=(212, 172, 84), width=8); d.rectangle([26, 26, w - 27, h - 27], outline=(150, 110, 48), width=2)
    size = 120; f = font(FS, size)
    while d.textlength(text, font=f) > w * .82 and size > 40: size -= 6; f = font(FS, size)
    tw = d.textlength(text, font=f); d.text(((w - tw) / 2, (h - size) / 2 - 8), text, font=f, fill=(236, 196, 100))
    img.save(out)

def _grad(w, h, top, bot):
    a = np.zeros((h, w, 3), np.float32)
    t = np.linspace(0, 1, h)[:, None, None]
    a[:] = np.array(top, np.float32) * (1 - t) + np.array(bot, np.float32) * t
    return Image.fromarray(np.clip(a, 0, 255).astype(np.uint8), "RGB")

def _fit(d, text, path, size, maxw):
    f = font(path, size)
    while d.textlength(text, font=f) > maxw and size > 20: size -= 4; f = font(path, size)
    return f, size

def police_sign(out):
    """Komisariat 2048×384 (12 × 2.25 m): granat w gradiencie, srebrna ramka, odznaka (gwiazda w okręgu, złoto-srebrna) po lewej,
    POLICJA białymi literami, KOMISARIAT mniejszy pod spodem, niebieski pas odblaskowy u dołu. Ogólny znak — nie godło państwowe."""
    W, H = 2048, 384
    img = _grad(W, H, (16, 32, 78), (6, 14, 40)); d = ImageDraw.Draw(img)
    d.rectangle([8, 8, W - 9, H - 9], outline=(200, 206, 216), width=10); d.rectangle([28, 28, W - 29, H - 29], outline=(120, 130, 150), width=3)
    for k in range(6):                                                     # pas szachownicy (odblask) u dołu
        pass
    cx, cy, R = 230, H // 2, 140
    img = radial(img, cx, cy, R, (238, 214, 140), (150, 112, 40)); d = ImageDraw.Draw(img)
    d.ellipse([cx - R, cy - R, cx + R, cy + R], outline=(250, 236, 180), width=6)
    d.ellipse([cx - R + 22, cy - R + 22, cx + R - 22, cy + R - 22], outline=(110, 80, 24), width=4)
    pts = []
    for i in range(10):                                                    # gwiazda pięcioramienna
        a = -math.pi / 2 + i * math.pi / 5; r = (R - 34) if i % 2 == 0 else (R - 34) * .42
        pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    d.polygon(pts, fill=(214, 218, 226), outline=(90, 96, 110))
    d.ellipse([cx - 26, cy - 26, cx + 26, cy + 26], fill=(16, 32, 78), outline=(230, 232, 238), width=4)
    f, size = _fit(d, "POLICJA", FB, 210, W - 600)
    tw = d.textlength("POLICJA", font=f); x0 = 430 + (W - 470 - tw) / 2
    d.text((x0 + 4, 36 + 4), "POLICJA", font=f, fill=(0, 0, 0)); d.text((x0, 36), "POLICJA", font=f, fill=(250, 250, 252))
    f2 = font(FB, 64); t2 = "KOMISARIAT"; tw2 = d.textlength(t2, font=f2)
    d.text((430 + (W - 470 - tw2) / 2, 270), t2, font=f2, fill=(170, 200, 255))
    img.save(out)

def hospital_sign(out):
    """Szpital 2048×384: biała tablica, niebieski kwadrat z białym H po lewej, zielony krzyż medyczny po prawej, SZPITAL na niebiesko."""
    W, H = 2048, 384
    img = _grad(W, H, (252, 252, 250), (226, 232, 236)); d = ImageDraw.Draw(img)
    d.rectangle([8, 8, W - 9, H - 9], outline=(20, 78, 160), width=12)
    s0 = 70; d.rounded_rectangle([60, s0, 60 + H - 2 * s0, H - s0], radius=26, fill=(18, 84, 176))
    fh = font(FB, 210); hw = d.textlength("H", font=fh); d.text((60 + (H - 2 * s0 - hw) / 2, s0 - 4), "H", font=fh, fill=(255, 255, 255))
    gx, gy, a, b2 = W - 230, H // 2, 120, 40                                # zielony krzyż medyczny
    d.rounded_rectangle([gx - b2, gy - a, gx + b2, gy + a], radius=10, fill=(20, 150, 80)); d.rounded_rectangle([gx - a, gy - b2, gx + a, gy + b2], radius=10, fill=(20, 150, 80))
    f, size = _fit(d, "SZPITAL", FB, 220, W - 760)
    tw = d.textlength("SZPITAL", font=f); d.text(((W - tw) / 2, 50), "SZPITAL", font=f, fill=(18, 70, 150))
    f2 = font(FB, 56); t2 = "IZBA PRZYJĘĆ  ·  24 h"; tw2 = d.textlength(t2, font=f2); d.text(((W - tw2) / 2, 286), t2, font=f2, fill=(60, 70, 80))
    img.save(out)

def arena_sign(out):
    """Hala walk 2048×384: czerń z czerwonym gradientem, ośmiokąt po bokach, HALA WALK białym, OKTAGON na czerwono."""
    W, H = 2048, 384
    img = _grad(W, H, (12, 10, 12), (40, 6, 10)); d = ImageDraw.Draw(img)
    d.rectangle([8, 8, W - 9, H - 9], outline=(200, 30, 36), width=10)
    for cx in (200, W - 200):
        R = 130; pts = [(cx + R * math.cos(math.pi / 8 + k * math.pi / 4), H / 2 + R * math.sin(math.pi / 8 + k * math.pi / 4)) for k in range(8)]
        d.polygon(pts, fill=(30, 30, 34), outline=(230, 230, 236)); d.line(pts + [pts[0]], fill=(200, 30, 36), width=8)
    f, size = _fit(d, "HALA WALK", FB, 200, W - 620)
    tw = d.textlength("HALA WALK", font=f); d.text(((W - tw) / 2, 40), "HALA WALK", font=f, fill=(250, 250, 250))
    f2 = font(FB, 80); t2 = "O K T A G O N"; tw2 = d.textlength(t2, font=f2); d.text(((W - tw2) / 2, 262), t2, font=f2, fill=(226, 40, 46))
    img.save(out)

def _octagon(d, cx, cy, R, **kw):
    pts = [(cx + R * math.cos(math.pi / 8 + k * math.pi / 4), cy + R * math.sin(math.pi / 8 + k * math.pi / 4)) for k in range(8)]
    d.polygon(pts, **kw); return pts

def octagon_mat(out):
    """Mata oktagonu 2048² (cały kwadrat opisany na ośmiokącie): płótno w odcieniu kości słoniowej z delikatną fakturą,
    czerwona linia przy krawędzi, w środku koło z logo BRAXHORN (czarne litery, czerwony pierścień), napisy BRAXHORN CITY."""
    W = 2048; rnd = np.random.default_rng(11)
    a = np.zeros((W, W, 3), np.float32); a[:] = (226, 222, 212)
    a += rnd.normal(0, 3.0, (W, W, 1)); a += (np.sin(np.arange(W) * .9)[None, :, None] * 1.5)
    img = Image.fromarray(np.clip(a, 0, 255).astype(np.uint8), "RGB"); d = ImageDraw.Draw(img)
    c = W / 2
    pts = _octagon(d, c, c, W * .485, outline=None)
    d.line(pts + [pts[0]], fill=(150, 20, 26), width=14)
    pts2 = [(c + (x - c) * .93, c + (y - c) * .93) for x, y in pts]; d.line(pts2 + [pts2[0]], fill=(30, 30, 34), width=4)
    R = 420
    d.ellipse([c - R, c - R, c + R, c + R], fill=(18, 18, 20)); d.ellipse([c - R + 18, c - R + 18, c + R - 18, c + R - 18], outline=(196, 26, 32), width=16)
    f, size = _fit(d, "BRAXHORN", FB, 190, 2 * R - 120); tw = d.textlength("BRAXHORN", font=f)
    d.text((c - tw / 2, c - size * .72), "BRAXHORN", font=f, fill=(246, 246, 244))
    f2 = font(FB, 70); t2 = "O C T A G O N"; tw2 = d.textlength(t2, font=f2); d.text((c - tw2 / 2, c + size * .45), t2, font=f2, fill=(214, 40, 46))
    f3 = font(FB, 92)
    for t, y in (("BRAXHORN CITY", 330), ("BRAXHORN CITY", W - 330 - 92)):
        tw3 = d.textlength(t, font=f3); d.text((c - tw3 / 2, y), t, font=f3, fill=(70, 70, 76))
    img.save(out)

def octagon_apron(out):
    """Fartuch podestu 1024×128 na jeden bok: czerń, czerwone linie, BRAXHORN białym."""
    W, H = 1024, 128
    img = _grad(W, H, (22, 22, 24), (6, 6, 8)); d = ImageDraw.Draw(img)
    d.rectangle([0, 6, W, 12], fill=(190, 24, 30)); d.rectangle([0, H - 13, W, H - 7], fill=(190, 24, 30))
    f, size = _fit(d, "BRAXHORN", FB, 78, W * .7); tw = d.textlength("BRAXHORN", font=f)
    d.text(((W - tw) / 2, (H - size) / 2 - 6), "BRAXHORN", font=f, fill=(244, 244, 244))
    img.save(out)

def chain_link(out):
    """Siatka oktagonu 128×128 RGBA (1 kafel = 0.3 m): romby z drutu w czarnej powłoce, przezroczyste oczka."""
    W = 128; img = Image.new("RGBA", (W, W), (0, 0, 0, 0)); d = ImageDraw.Draw(img)
    col = (38, 40, 44, 255)
    for o in (-W, 0, W):
        d.line([(o, 0), (o + W, W)], fill=col, width=7); d.line([(o + W, 0), (o, W)], fill=col, width=7)
        d.line([(o + W // 2, 0), (o + W + W // 2, W)], fill=col, width=7); d.line([(o + W // 2 + W, 0), (o + W // 2, W)], fill=col, width=7)
    img.save(out)

def octagon_banner(out):
    """Baner pionowy 512×1024: czerń → bordo, ośmiokąt u góry, BRAXHORN pionowo, OCTAGON u dołu."""
    W, H = 512, 1024
    img = _grad(W, H, (14, 12, 14), (70, 8, 14)); d = ImageDraw.Draw(img)
    d.rectangle([10, 10, W - 11, H - 11], outline=(200, 30, 36), width=8)
    pts = _octagon(d, W / 2, 170, 110, fill=(24, 24, 28), outline=(236, 236, 240)); d.line(pts + [pts[0]], fill=(200, 30, 36), width=8)
    fb = font(FB, 64); tb = "B"; d.text((W / 2 - d.textlength(tb, font=fb) / 2, 170 - 38), tb, font=fb, fill=(246, 246, 246))
    txt = Image.new("RGBA", (680, 150), (0, 0, 0, 0)); dt = ImageDraw.Draw(txt)
    f, size = _fit(dt, "BRAXHORN", FB, 130, 660); tw = dt.textlength("BRAXHORN", font=f); dt.text(((680 - tw) / 2, 4), "BRAXHORN", font=f, fill=(248, 248, 248, 255))
    rot = txt.rotate(90, expand=True); img.paste(rot, (int(W / 2 - rot.width / 2), 320), rot)
    f2 = font(FB, 48); t2 = "OCTAGON"; d.text(((W - d.textlength(t2, font=f2)) / 2, H - 90), t2, font=f2, fill=(226, 40, 46))
    img.save(out)

def brand_sign(out, w=1024, h=256):
    """Logo BRAXHORN do hali walk (telebim, stoły, ściana): czerń, białe litery, czerwona linia i ośmiokąty po bokach."""
    img = _grad(w, h, (16, 16, 18), (4, 4, 6)); d = ImageDraw.Draw(img)
    f, size = _fit(d, "BRAXHORN", FB, 150, w * .66); tw = d.textlength("BRAXHORN", font=f)
    d.text(((w - tw) / 2, (h - size) / 2 - 18), "BRAXHORN", font=f, fill=(248, 248, 248))
    d.rectangle([(w - tw) / 2, h - 52, (w + tw) / 2, h - 44], fill=(206, 28, 34))
    for cx in (70, w - 70):
        pts = _octagon(d, cx, h / 2, 44, fill=(28, 28, 32)); d.line(pts + [pts[0]], fill=(206, 28, 34), width=6)
    img.save(out)

def packaging(out):
    """Opakowania towarów 512² (kafel 0.32 m, kolor z materiału): jasne tło, etykiety z napisami, kod kreskowy, paski —
    produkty w sklepach mają nadruk zamiast gładkich klocków (kolor towaru mnoży jasne tło)."""
    W = 512; rnd = random.Random(7)
    img = Image.new("RGB", (W, W), (236, 236, 232)); d = ImageDraw.Draw(img)
    words = ["BRAX", "FIT", "PROTEIN", "100%", "MLEKO", "SOK", "KAWA", "BIO", "ZERO", "MAX"]
    for row in range(4):
        y0 = row * 128
        d.rectangle([0, y0 + 8, W, y0 + 14], fill=(120, 120, 122))
        for col in range(4):
            x0 = col * 128 + 10
            d.rounded_rectangle([x0, y0 + 30, x0 + 104, y0 + 92], radius=10, fill=(250, 250, 248), outline=(40, 40, 44), width=3)
            w = words[rnd.randrange(len(words))]; f, _ = _fit(d, w, FB, 26, 92); tw = d.textlength(w, font=f)
            d.text((x0 + 52 - tw / 2, y0 + 44), w, font=f, fill=(24, 24, 28))
            for k in range(14):
                bx = x0 + 18 + k * 5; d.rectangle([bx, y0 + 100, bx + rnd.choice((1, 2, 3)), y0 + 118], fill=(30, 30, 32))
    img.save(out)

def name_plaque(out, text="RODZINA WAWER", w=1024, h=512):
    """Tabliczka na drzwi / słupek bramy: mosiądz szczotkowany z ciemnymi wygrawerowanymi literami, ramka, wkręty."""
    rnd = np.random.default_rng(3)
    a = np.zeros((h, w, 3), np.float32); a[:] = (196, 160, 84)
    a += rnd.normal(0, 4, (h, 1, 1)) + np.linspace(-14, 14, w)[None, :, None]
    img = Image.fromarray(np.clip(a, 0, 255).astype(np.uint8), "RGB"); d = ImageDraw.Draw(img)
    d.rounded_rectangle([18, 18, w - 19, h - 19], radius=26, outline=(110, 80, 30), width=10)
    for x, y in ((50, 50), (w - 50, 50), (50, h - 50), (w - 50, h - 50)): d.ellipse([x - 14, y - 14, x + 14, y + 14], fill=(150, 118, 56), outline=(90, 66, 24), width=3)
    words = text.split(" ", 1)
    f1, s1 = _fit(d, words[0], FS, 120, w - 200); tw = d.textlength(words[0], font=f1); d.text(((w - tw) / 2, 90), words[0], font=f1, fill=(46, 30, 10))
    if len(words) > 1:
        f2, s2 = _fit(d, words[1], FS, 150, w - 200); tw = d.textlength(words[1], font=f2); d.text(((w - tw) / 2, 250), words[1], font=f2, fill=(46, 30, 10))
    img.save(out)

def address_plate(out, street="POZNAŃSKA", number="2", w=1024, h=512):
    """Tabliczka adresowa na elewację: granat, biała ramka, nazwa ulicy i duży numer."""
    img = Image.new("RGB", (w, h), (22, 46, 104)); d = ImageDraw.Draw(img)
    d.rounded_rectangle([14, 14, w - 15, h - 15], radius=30, outline=(246, 246, 246), width=12)
    f1, _ = _fit(d, street, FB, 110, w - 160); tw = d.textlength(street, font=f1); d.text(((w - tw) / 2, 60), street, font=f1, fill=(250, 250, 250))
    f2 = font(FB, 230); tw = d.textlength(number, font=f2); d.text(((w - tw) / 2, 190), number, font=f2, fill=(250, 250, 250))
    img.save(out)

if __name__ == "__main__":
    out = pathlib.Path(sys.argv[1]); out.mkdir(parents=True, exist_ok=True)
    speaker(out / "glosnik.png"); amplifier(out / "wzmacniacz.png")
    print("WIEŻA OK")
    carpet(out / "dywan_kasyna.png"); slot_screen(out / "automat_ekran.png"); slot_top(out / "automat_gora.png")
    bet_board(out / "tablica_zakladow.png"); gold_sign(out / "zaklady.png", "ZAKŁADY"); gold_sign(out / "kasa_zetony.png", "KASA  ·  ŻETONY")
    gold_sign(out / "bar_kasyna.png", "BAR"); print("KASYNO WNĘTRZE OK")
    police_sign(out / "policja_odznaka.png"); hospital_sign(out / "szpital_h.png"); arena_sign(out / "hala_walk.png"); print("SZYLDY BUDYNKÓW OK")
    octagon_mat(out / "oktagon_mata.png"); octagon_apron(out / "oktagon_fartuch.png"); chain_link(out / "siatka_oktagonu.png"); octagon_banner(out / "baner_oktagon.png"); brand_sign(out / "braxhorn_arena.png"); gold_sign(out / "recepcja_braxhorn.png", "BRAXHORN"); packaging(out / "opakowania.png"); name_plaque(out / "rodzina_wawer.png"); address_plate(out / "poznanska_2.png"); print("OKTAGON OK")
