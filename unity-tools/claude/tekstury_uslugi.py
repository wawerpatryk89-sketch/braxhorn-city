#!/usr/bin/env python3
"""BUILD 15 „usługi” — tekstury generowane kodem (bez cudzych grafik) dla wnętrz: szpital, komisariat, kasyno, dyskoteka.

Wywołanie z fetch_osiedle.py (jedna linia, w try): tekstury_uslugi.wszystkie(<katalog signs>). Można też ręcznie:
    python3 tekstury_uslugi.py <katalog_wyjściowy>
Wszystkie pliki mają przedrostek „us_” (bez kolizji z szyldami innych etapów). Nic nie świeci — to zwykłe tekstury koloru
(ekrany monitorów / automatów mają jasne kolory, ale materiał w grze jest matowy, bez emisji; wyjątek: klub — tam
generator sam ustawia emisję). Czcionki: DejaVu (Ubuntu w CI).
"""
import math, pathlib, random, sys
from PIL import Image, ImageDraw, ImageFont, ImageFilter

_F = "/usr/share/fonts/truetype/dejavu/"
FB = next((p for p in (_F + "DejaVuSans-Bold.ttf", _F + "DejaVuSansCondensed-Bold.ttf") if pathlib.Path(p).exists()), None)
FR = next((p for p in (_F + "DejaVuSans.ttf",) if pathlib.Path(p).exists()), FB)
FC = next((p for p in (_F + "DejaVuSansCondensed-Bold.ttf",) if pathlib.Path(p).exists()), FB)
FS = next((p for p in (_F + "DejaVuSerif-Bold.ttf",) if pathlib.Path(p).exists()), FB)


def font(path, size):
    return ImageFont.truetype(path, size) if path else ImageFont.load_default()


def fit(d, text, path, size, maxw):
    f = font(path, size)
    while path and d.textlength(text, font=f) > maxw and size > 14:
        size -= 2; f = font(path, size)
    return f


def ctext(d, cx, cy, text, f, fill):
    w = d.textlength(text, font=f); bb = d.textbbox((0, 0), text, font=f)
    d.text((cx - w / 2, cy - (bb[1] + bb[3]) / 2), text, font=f, fill=fill)


# ---------------- tabliczki (szpital: biało-granatowe z niebieskim paskiem; komisariat: granat + biel) ----------------
NAVY = (18, 44, 92); BLUE = (34, 106, 186); WHITE = (246, 247, 248)


def tabliczka(out, text, sub=None, styl="szpital", w=1024, h=256):
    if styl == "policja":
        bg, fg, bar = (16, 30, 66), (240, 242, 246), (214, 178, 60)
    elif styl == "klub":
        bg, fg, bar = (12, 10, 16), (236, 226, 250), (176, 40, 210)
    else:
        bg, fg, bar = WHITE, NAVY, BLUE
    im = Image.new("RGB", (w, h), bg); d = ImageDraw.Draw(im)
    d.rectangle([0, 0, 40, h], fill=bar)
    d.rectangle([2, 2, w - 3, h - 3], outline=(150, 156, 166) if styl == "szpital" else bar, width=4)
    if sub:
        f = fit(d, text, FB, 104, w - 120); ctext(d, (w + 40) / 2, h * .40, text, f, fg)
        f2 = fit(d, sub, FR, 46, w - 120); ctext(d, (w + 40) / 2, h * .78, sub, f2, bar if styl != "szpital" else BLUE)
    else:
        f = fit(d, text, FB, 120, w - 120); ctext(d, (w + 40) / 2, h / 2, text, f, fg)
    im.save(out)


def kierunkowa(out, lewo, prawo, w=2048, h=256):
    """Tablica kierunkowa podwieszana (szpital): niebieska, białe napisy ze strzałkami."""
    im = Image.new("RGB", (w, h), (24, 84, 160)); d = ImageDraw.Draw(im)
    d.rectangle([4, 4, w - 5, h - 5], outline=(235, 240, 248), width=6)
    f = fit(d, lewo, FB, 96, w / 2 - 80); d.text((40, h / 2 - 60), lewo, font=f, fill=(250, 250, 252))
    f2 = fit(d, prawo, FB, 96, w / 2 - 80); tw = d.textlength(prawo, font=f2); d.text((w - 40 - tw, h / 2 - 60), prawo, font=f2, fill=(250, 250, 252))
    d.line([w / 2, 30, w / 2, h - 30], fill=(235, 240, 248), width=4)
    im.save(out)


def tablica_info(out):
    """Tablica informacyjna szpitala 1024×640: plan, numery sal, godziny."""
    W, H = 1024, 640; im = Image.new("RGB", (W, H), (250, 251, 252)); d = ImageDraw.Draw(im)
    d.rectangle([0, 0, W, 120], fill=NAVY)
    ctext(d, W / 2, 46, "SZPITAL BRAXHORN", font(FB, 62), WHITE)
    ctext(d, W / 2, 98, "INFORMACJA DLA PACJENTÓW", font(FR, 32), (190, 210, 240))
    rows = [("REJESTRACJA", "parter, przy wejściu", "7:00–19:00"), ("GABINET 1, GABINET 2", "poczekalnia", "8:00–18:00"),
            ("GABINET ZABIEGOWY", "korytarz poprzeczny", "całą dobę"), ("SALE CHORYCH 1–5", "oddział", "odwiedziny 14–19"),
            ("DYŻURKA PIELĘGNIAREK", "korytarz główny", "całą dobę"), ("WC · ŁAZIENKA", "poczekalnia · oddział", "")]
    fb, fr = font(FB, 34), font(FR, 30)
    for i, (a, b, c) in enumerate(rows):
        y = 150 + i * 76
        d.rectangle([30, y, 46, y + 50], fill=BLUE)
        d.text((64, y + 2), a, font=fb, fill=NAVY); d.text((64, y + 40 - 6), b, font=font(FR, 24), fill=(90, 98, 110))
        tw = d.textlength(c, font=fr); d.text((W - 40 - tw, y + 8), c, font=fr, fill=(40, 60, 90))
        d.line([30, y + 66, W - 30, y + 66], fill=(210, 214, 220), width=2)
    im.save(out)


def plakat_rece(out):
    """Plakat „MYJ RĘCE” 512×720: 6 kroków (schematyczne dłonie z elips), napisy."""
    W, H = 512, 720; im = Image.new("RGB", (W, H), (244, 248, 252)); d = ImageDraw.Draw(im)
    d.rectangle([0, 0, W, 96], fill=(0, 132, 196)); ctext(d, W / 2, 50, "MYJ RĘCE", font(FB, 58), WHITE)
    skin = (232, 196, 170); fs = font(FR, 22)
    for k in range(6):
        cx = 128 + (k % 2) * 256; cy = 190 + (k // 2) * 190
        d.ellipse([cx - 70, cy - 70, cx + 70, cy + 70], fill=(214, 236, 248))
        a = k * 30
        for s in (-1, 1):
            x = cx + s * 18 * math.cos(math.radians(a)); y = cy + s * 18 * math.sin(math.radians(a))
            d.ellipse([x - 32, y - 46, x + 32, y + 46], fill=skin, outline=(170, 130, 110), width=2)
        ctext(d, cx, cy + 86, str(k + 1) + ". " + ["dłonie", "grzbiety", "palce", "kciuki", "opuszki", "nadgarstki"][k], fs, (30, 50, 80))
    ctext(d, W / 2, H - 30, "min. 30 sekund", font(FB, 28), (0, 110, 170))
    im.save(out)


def monitor_ekg(out):
    W, H = 512, 384; im = Image.new("RGB", (W, H), (6, 10, 12)); d = ImageDraw.Draw(im)
    d.rectangle([0, 0, W - 1, H - 1], outline=(70, 76, 80), width=10)
    def fala(y0, col, amp, okres, faza):
        pts = []
        for x in range(20, 360):
            t = ((x + faza) % okres) / okres
            v = 0.0
            if .30 < t < .34: v = -.25
            elif .34 < t < .38: v = 1.0
            elif .38 < t < .42: v = -.45
            elif .55 < t < .68: v = .22 * math.sin((t - .55) / .13 * math.pi)
            pts.append((x, y0 - v * amp))
        d.line(pts, fill=col, width=3)
    fala(90, (60, 230, 90), 50, 120, 0); fala(200, (250, 220, 60), 26, 120, 30)
    pts = [(x, 300 - 20 * math.sin(x / 18.0)) for x in range(20, 360)]; d.line(pts, fill=(80, 190, 250), width=3)
    for y, lab, val, col in ((70, "HR", "72", (60, 230, 90)), (180, "SpO2", "98", (250, 220, 60)), (290, "NIBP", "124/78", (80, 190, 250))):
        d.text((380, y - 30), lab, font=font(FR, 22), fill=col); d.text((380, y - 6), val, font=font(FB, 46 if len(val) < 4 else 30), fill=col)
    im.save(out)


def ekran_pc(out, styl="szpital"):
    W, H = 512, 320; im = Image.new("RGB", (W, H), (236, 240, 246)); d = ImageDraw.Draw(im)
    top = (18, 44, 92) if styl != "policja" else (16, 30, 66)
    d.rectangle([0, 0, W, 34], fill=top); d.text((12, 6), "SZPITAL BRAXHORN · e-rejestracja" if styl == "szpital" else "KSIP · POLICJA", font=font(FB, 18), fill=WHITE)
    rnd = random.Random(7 if styl == "szpital" else 9)
    for i in range(9):
        y = 46 + i * 28
        d.rectangle([10, y, W - 10, y + 22], fill=(255, 255, 255) if i % 2 else (226, 232, 240))
        for x0, x1 in ((16, 120), (130, 300), (310, 420), (430, 500)):
            d.rectangle([x0, y + 8, x0 + int((x1 - x0) * rnd.uniform(.4, .9)), y + 13], fill=(90, 100, 120))
    d.rectangle([0, 0, W - 1, H - 1], outline=(30, 30, 34), width=6)
    im.save(out)


def automat_napoje(out):
    """Front automatu z napojami 512×1024: szyba z rzędami butelek/puszek, panel z klawiaturą i wrzutnikiem."""
    W, H = 512, 1024; im = Image.new("RGB", (W, H), (30, 30, 34)); d = ImageDraw.Draw(im)
    d.rectangle([0, 0, W, 130], fill=(0, 104, 170)); ctext(d, W / 2 - 40, 64, "NAPOJE", font(FB, 70), WHITE)
    d.rectangle([20, 150, 360, 980], fill=(16, 20, 26))
    rnd = random.Random(3); kol = [(200, 30, 36), (20, 120, 200), (250, 190, 30), (40, 160, 70), (240, 240, 240), (230, 110, 20)]
    for r in range(6):
        y = 170 + r * 134
        d.rectangle([24, y + 112, 356, y + 120], fill=(120, 124, 130))
        for c in range(5):
            x = 40 + c * 64; col = kol[(r * 2 + c) % len(kol)]
            if r % 2 == 0:
                d.rounded_rectangle([x, y + 10, x + 40, y + 110], radius=12, fill=col); d.rectangle([x + 12, y, x + 28, y + 14], fill=(230, 230, 232))
                d.rectangle([x + 4, y + 50, x + 36, y + 76], fill=(250, 250, 250))
            else:
                d.rectangle([x, y + 40, x + 44, y + 110], fill=col); d.rectangle([x, y + 60, x + 44, y + 74], fill=(250, 250, 250))
            d.text((x + 8, y + 122 - 30), str(r + 1) + str(c + 1), font=font(FR, 14), fill=(200, 200, 200))
    d.rectangle([376, 150, 496, 980], fill=(48, 50, 56))
    d.rectangle([390, 170, 482, 230], fill=(10, 40, 20)); d.text((398, 186), "4,50 zł", font=font(FB, 22), fill=(90, 240, 120))
    for i in range(12):
        x = 392 + (i % 3) * 32; y = 260 + (i // 3) * 40
        d.rectangle([x, y, x + 26, y + 30], fill=(210, 212, 216)); d.text((x + 7, y + 4), "123456789*0#"[i], font=font(FB, 18), fill=(20, 20, 24))
    d.rectangle([400, 450, 470, 470], fill=(12, 12, 14)); d.rectangle([396, 520, 474, 600], fill=(20, 20, 22))
    d.rectangle([60, 990, 330, 1018], fill=(12, 12, 14))
    im.save(out)


def zegar(out):
    W = 512; im = Image.new("RGB", (W, W), (240, 240, 238)); d = ImageDraw.Draw(im)
    d.ellipse([8, 8, W - 8, W - 8], fill=(252, 252, 250), outline=(30, 30, 34), width=18)
    for k in range(60):
        a = math.radians(k * 6); r0 = 210 if k % 5 else 190
        d.line([W / 2 + r0 * math.sin(a), W / 2 - r0 * math.cos(a), W / 2 + 226 * math.sin(a), W / 2 - 226 * math.cos(a)], fill=(30, 30, 34), width=8 if k % 5 == 0 else 3)
    f = font(FB, 44)
    for h in range(1, 13):
        a = math.radians(h * 30); ctext(d, W / 2 + 160 * math.sin(a), W / 2 - 160 * math.cos(a), str(h), f, (30, 30, 34))
    for ang, ln, wd in ((300, 120, 14), (60, 190, 9)):
        a = math.radians(ang); d.line([W / 2, W / 2, W / 2 + ln * math.sin(a), W / 2 - ln * math.cos(a)], fill=(20, 20, 24), width=wd)
    d.ellipse([W / 2 - 14, W / 2 - 14, W / 2 + 14, W / 2 + 14], fill=(200, 30, 30))
    im.save(out)


def kolejka(out):
    W, H = 1024, 384; im = Image.new("RGB", (W, H), (10, 12, 16)); d = ImageDraw.Draw(im)
    d.rectangle([0, 0, W - 1, H - 1], outline=(60, 64, 70), width=14)
    fr = font(FB, 52); fn = font(FB, 120)
    d.text((40, 30), "NUMER", font=fr, fill=(240, 200, 60)); d.text((560, 30), "GABINET", font=fr, fill=(240, 200, 60))
    d.text((40, 110), "A027", font=fn, fill=(250, 250, 250)); d.text((620, 110), "1", font=fn, fill=(250, 250, 250))
    d.text((40, 260), "A028", font=font(FB, 70), fill=(150, 156, 166)); d.text((620, 260), "2", font=font(FB, 70), fill=(150, 156, 166))
    im.save(out)


def zaluzja(out):
    W = 256; im = Image.new("RGB", (W, W), (226, 222, 212)); d = ImageDraw.Draw(im)
    for y in range(0, W, 16):
        d.rectangle([0, y + 12, W, y + 15], fill=(176, 172, 162)); d.rectangle([0, y, W, y + 2], fill=(242, 240, 234))
    im.save(out)


def poszukiwani(out):
    W, H = 1024, 640; im = Image.new("RGB", (W, H), (196, 160, 112)); d = ImageDraw.Draw(im)
    rnd = random.Random(5)
    for i in range(6):
        x = 40 + i * 162; y = 60 + (i % 2) * 40; rot = rnd.uniform(-3, 3)
        k = Image.new("RGB", (140, 200), (246, 244, 236)); dk = ImageDraw.Draw(k)
        dk.text((14, 6), "POSZUKIWANY" if i % 3 else "ZAGINIONA", font=font(FB, 15), fill=(160, 20, 20))
        dk.rectangle([20, 32, 120, 140], fill=(206, 206, 204)); dk.ellipse([46, 44, 94, 96], fill=(120, 120, 124)); dk.rectangle([36, 96, 104, 140], fill=(120, 120, 124))
        for j in range(4): dk.rectangle([14, 150 + j * 11, 14 + rnd.randint(60, 112), 155 + j * 11], fill=(90, 90, 96))
        k = k.rotate(rot, expand=True, fillcolor=(196, 160, 112)); im.paste(k, (x, y))
        d.ellipse([x + 62, y - 4, x + 76, y + 10], fill=(200, 30, 30))
    d.rectangle([40, 360, 984, 600], fill=(246, 244, 236)); d.text((60, 372), "KOMUNIKATY", font=font(FB, 40), fill=(16, 30, 66))
    for j in range(6): d.rectangle([60, 430 + j * 26, 60 + rnd.randint(500, 900), 440 + j * 26], fill=(90, 90, 100))
    d.rectangle([0, 0, W - 1, H - 1], outline=(110, 80, 50), width=14)
    im.save(out)


def cctv(out):
    W, H = 512, 320; im = Image.new("RGB", (W, H), (8, 8, 10)); d = ImageDraw.Draw(im); rnd = random.Random(11)
    for i in range(4):
        x0 = 8 + (i % 2) * 250; y0 = 8 + (i // 2) * 154
        base = rnd.randint(50, 90)
        for y in range(y0, y0 + 146, 2):
            g = base + int(20 * math.sin(y / 9.0 + i)); d.line([x0, y, x0 + 244, y], fill=(g, g, g + 4))
        d.rectangle([x0 + 40, y0 + 60, x0 + 120, y0 + 140], fill=(base - 30,) * 3)
        d.text((x0 + 6, y0 + 4), "KAM " + str(i + 1), font=font(FB, 16), fill=(230, 230, 230))
        d.text((x0 + 150, y0 + 128), "02.11 22:4" + str(i), font=font(FR, 13), fill=(230, 230, 230))
    im.save(out)


# ---------------- kasyno ----------------
RED = (176, 18, 26); BLACK = (16, 16, 18); GREEN = (16, 108, 52); FELT = (18, 96, 52); GOLD = (220, 186, 100)
CZERWONE = {1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 23, 25, 27, 30, 32, 34, 36}
KOLO = [0, 32, 15, 19, 4, 21, 2, 25, 17, 34, 6, 27, 13, 36, 11, 30, 8, 23, 10, 5, 24, 16, 33, 1, 20, 14, 31, 9, 22, 18, 29, 7, 28, 12, 35, 3, 26]


def sukno_ruletki(out):
    """Sukno ruletki europejskiej (widok z góry) 2048×1024: 0, siatka 3×12, kolumny 2:1, tuziny, zakłady proste."""
    W, H = 2048, 1024; im = Image.new("RGB", (W, H), FELT); d = ImageDraw.Draw(im)
    for y in range(0, H, 4):   # delikatne włókno sukna
        d.line([0, y, W, y], fill=(FELT[0], FELT[1] + (3 if (y // 4) % 2 else -2), FELT[2]))
    x0, y0, cw, ch = 300, 120, 120, 170          # siatka liczb: 12 kolumn × 3 wiersze
    lw = 5; wh = (238, 234, 222); fn = font(FB, 62)
    # zero (pięciokąt)
    d.polygon([(x0, y0), (x0, y0 + 3 * ch), (x0 - 150, y0 + 1.5 * ch)], fill=GREEN, outline=wh)
    d.line([(x0, y0), (x0 - 150, y0 + 1.5 * ch), (x0, y0 + 3 * ch)], fill=wh, width=lw)
    ctext(d, x0 - 60, y0 + 1.5 * ch, "0", fn, wh)
    for c in range(12):
        for r in range(3):
            n = c * 3 + (3 - r)
            xa, ya = x0 + c * cw, y0 + r * ch
            d.rectangle([xa, ya, xa + cw, ya + ch], outline=wh, width=lw)
            col = RED if n in CZERWONE else BLACK
            d.ellipse([xa + 14, ya + 30, xa + cw - 14, ya + ch - 30], fill=col)
            ctext(d, xa + cw / 2, ya + ch / 2, str(n), font(FB, 54 if n < 10 else 46), wh)
    for r in range(3):   # 2 do 1
        xa, ya = x0 + 12 * cw, y0 + r * ch
        d.rectangle([xa, ya, xa + 150, ya + ch], outline=wh, width=lw); ctext(d, xa + 75, ya + ch / 2, "2 : 1", font(FB, 40), wh)
    for k, t in enumerate(("1st 12", "2nd 12", "3rd 12")):
        xa, ya = x0 + k * 4 * cw, y0 + 3 * ch
        d.rectangle([xa, ya, xa + 4 * cw, ya + 120], outline=wh, width=lw); ctext(d, xa + 2 * cw, ya + 60, t, font(FB, 52), wh)
    labels = ("1–18", "EVEN", "RED", "BLACK", "ODD", "19–36")
    for k, t in enumerate(labels):
        xa, ya = x0 + k * 2 * cw, y0 + 3 * ch + 120
        d.rectangle([xa, ya, xa + 2 * cw, ya + 120], outline=wh, width=lw)
        if t == "RED": d.polygon([(xa + cw, ya + 18), (xa + 2 * cw - 30, ya + 60), (xa + cw, ya + 102), (xa + 30, ya + 60)], fill=RED)
        elif t == "BLACK": d.polygon([(xa + cw, ya + 18), (xa + 2 * cw - 30, ya + 60), (xa + cw, ya + 102), (xa + 30, ya + 60)], fill=BLACK)
        else: ctext(d, xa + cw, ya + 60, t, font(FB, 46), wh)
    d.rectangle([4, 4, W - 5, H - 5], outline=GOLD, width=10)
    ctext(d, W - 240, H - 70, "BRAXHORN", font(FS, 54), GOLD)
    im.save(out)


def kolo_ruletki(out):
    """Tarcza koła ruletki (widok z góry) 1024×1024: drewniany wieniec, 37 przegródek w kolejności europejskiej,
    numery, środkowy stożek. Obracana w grze jako jeden krążek."""
    W = 1024; c = W / 2; im = Image.new("RGB", (W, W), (60, 30, 14)); d = ImageDraw.Draw(im)
    d.ellipse([0, 0, W, W], fill=(92, 46, 20))
    for r in range(500, 430, -6): d.ellipse([c - r, c - r, c + r, c + r], outline=(110 + (r % 12) * 3, 56, 24), width=3)
    R1, R2, R3 = 430, 330, 250
    for k, n in enumerate(KOLO):
        a0 = math.radians(k * 360 / 37 - 90 - 360 / 74); a1 = math.radians((k + 1) * 360 / 37 - 90 - 360 / 74)
        col = GREEN if n == 0 else RED if n in CZERWONE else BLACK
        poly = [(c + R1 * math.cos(a0), c + R1 * math.sin(a0)), (c + R1 * math.cos(a1), c + R1 * math.sin(a1)), (c + R3 * math.cos(a1), c + R3 * math.sin(a1)), (c + R3 * math.cos(a0), c + R3 * math.sin(a0))]
        d.polygon(poly, fill=col)
        d.line([poly[0], poly[3]], fill=(214, 200, 150), width=4)   # przegródki (metal)
        am = (a0 + a1) / 2; tx, ty = c + (R1 + R2) / 2 * math.cos(am), c + (R1 + R2) / 2 * math.sin(am)
        t = Image.new("RGBA", (80, 60), (0, 0, 0, 0)); ImageDraw.Draw(t).text((40 - (12 if n < 10 else 22), 4), str(n), font=font(FB, 40), fill=(240, 236, 220, 255))
        t = t.rotate(-math.degrees(am) - 90, expand=True); im.paste(t, (int(tx - t.width / 2), int(ty - t.height / 2)), t)
    d.ellipse([c - R2, c - R2, c + R2, c + R2], outline=(214, 200, 150), width=5)
    for r in range(R3, 40, -10): d.ellipse([c - r, c - r, c + r, c + r], fill=(120 + (R3 - r) // 3, 64 + (R3 - r) // 6, 26))
    for k in range(4):
        a = math.radians(k * 90); d.line([c, c, c + 230 * math.cos(a), c + 230 * math.sin(a)], fill=(220, 196, 120), width=14)
    d.ellipse([c - 40, c - 40, c + 40, c + 40], fill=(226, 204, 130))
    im.save(out)


def sukno_blackjack(out):
    W, H = 2048, 1024; im = Image.new("RGB", (W, H), FELT); d = ImageDraw.Draw(im)
    wh = (238, 234, 222)
    d.arc([60, -900, W - 60, H - 40], 20, 160, fill=GOLD, width=8)
    ctext(d, W / 2, 210, "BLACKJACK PAYS 3 TO 2", font(FS, 76), GOLD)
    ctext(d, W / 2, 300, "Dealer must stand on 17 and must draw to 16", font(FB, 40), wh)
    ctext(d, W / 2, 360, "INSURANCE PAYS 2 TO 1", font(FB, 36), wh)
    cx, cy, R = W / 2, -260, 1060
    for k in range(7):
        a = math.radians(150 - k * 20); x = cx + R * math.cos(a); y = cy + R * math.sin(a)
        d.ellipse([x - 70, y - 70, x + 70, y + 70], outline=wh, width=6)
    d.rectangle([W / 2 - 220, 20, W / 2 + 220, 120], outline=wh, width=5)
    for k in range(5): d.rectangle([W / 2 - 200 + k * 82, 32, W / 2 - 132 + k * 82, 108], fill=(80 + k * 20, 10, 20) if k % 2 else (20, 20, 22))
    d.rectangle([4, 4, W - 5, H - 5], outline=(10, 70, 36), width=8)
    im.save(out)


def sukno_poker(out):
    W, H = 2048, 1024; im = Image.new("RGB", (W, H), (20, 80, 120)); d = ImageDraw.Draw(im)
    d.rounded_rectangle([120, 120, W - 120, H - 120], radius=380, outline=(230, 220, 190), width=8)
    ctext(d, W / 2, H / 2, "BRAXHORN  POKER", font(FS, 90), (200, 220, 236))
    for k in range(5): d.rectangle([W / 2 - 330 + k * 140, H / 2 + 90, W / 2 - 230 + k * 140, H / 2 + 230], outline=(230, 220, 190), width=4)
    im.save(out)


# ---------------- zestaw ----------------
def wszystkie(dst):
    dst = pathlib.Path(dst); dst.mkdir(parents=True, exist_ok=True)
    T = lambda n: dst / ("us_" + n + ".png")
    for n, t, s in (("rejestracja", "REJESTRACJA", None), ("poczekalnia", "POCZEKALNIA", None), ("gabinet1", "GABINET 1", "lekarz rodzinny"),
                    ("gabinet2", "GABINET 2", "internista"), ("zabiegowy", "GABINET ZABIEGOWY", None), ("dyzurka_piel", "DYŻURKA", "pielęgniarki"),
                    ("lazienka", "ŁAZIENKA", "dla pacjentów"), ("wc_damskie", "WC  DAMSKIE", None), ("wc_meskie", "WC  MĘSKIE", None),
                    ("sala1", "SALA 1", None), ("sala2", "SALA 2", None), ("sala3", "SALA 3", None), ("sala4", "SALA 4", None), ("sala5", "SALA 5", None),
                    ("wyjscie", "WYJŚCIE", None), ("magazyn", "MAGAZYN", "tylko personel")):
        tabliczka(T(n), t, s)
    for n, t, s in (("dyzurny", "DYŻURNY", "przyjęcia zgłoszeń"), ("przesluchania", "POKÓJ PRZESŁUCHAŃ", None), ("komendant", "KOMENDANT", None),
                    ("szatnia_pol", "SZATNIA", "tylko funkcjonariusze"), ("wc_pol", "WC", None), ("cela1", "CELA 1", None), ("cela2", "CELA 2", None)):
        tabliczka(T(n), t, s, "policja")
    for n, t, s in (("szatnia_klub", "SZATNIA", "garderoba"), ("wc_klub_d", "WC  DAMSKIE", None), ("wc_klub_m", "WC  MĘSKIE", None), ("vip", "VIP", "loże")):
        tabliczka(T(n), t, s, "klub")
    kierunkowa(T("kier_oddzial"), "← SALE 1–3", "SALE 4–5 · ZABIEGOWY →")
    kierunkowa(T("kier_hol"), "← GABINETY · WC", "REJESTRACJA →")
    tablica_info(T("tablica_info")); plakat_rece(T("plakat_rece")); monitor_ekg(T("ekg")); ekran_pc(T("ekran_pc")); ekran_pc(T("ekran_pc_pol"), "policja")
    automat_napoje(T("automat_napoje")); zegar(T("zegar")); kolejka(T("kolejka")); zaluzja(T("zaluzja")); poszukiwani(T("poszukiwani")); cctv(T("cctv"))
    sukno_ruletki(T("sukno_ruletki")); kolo_ruletki(T("kolo_ruletki")); sukno_blackjack(T("sukno_blackjack")); sukno_poker(T("sukno_poker"))
    n = len(list(dst.glob("us_*.png")))
    print("TEKSTURY USLUGI OK", n)
    return n


if __name__ == "__main__":
    wszystkie(sys.argv[1] if len(sys.argv) > 1 else "us_out")
