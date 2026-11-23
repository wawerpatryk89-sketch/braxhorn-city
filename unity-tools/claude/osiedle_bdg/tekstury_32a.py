"""MIASTO R1 — tekstury nakładane na tynk 32a (UV1, 0..1 na całą ścianę/pas), rysowane skryptem, bez zdjęć Google.

- graffiti_32a.png  — „PDW FRYZJER HWDP” sprayem na żółtym pasie ściany z wejściem (IMG_1427; właściciel: HWDP można),
                      litery 0,85–2,08 m nad terenem, od 0,75 do 7,7 m od wschodniego narożnika, mała niebieska wrzutka.
- napis_32a.png     — „32a / M. SKŁODOWSKIEJ-CURIE” ciemną farbą na jaśniejszym polu (zdjęcie szczytu 32a).
- brud_*.png        — zacieki i zawilgocenia (mnożnik jasności, 1 = czysto): pionowe zacieki spod attyki, ciemny pas
                      w połowie wysokości jak na zdjęciu, jaśniejsze plamy; ściana z wejściem lżej, ślepa ściana mocniej.
Czcionki: DejaVu (system) — napis adresowy jest prostym bezszeryfowym pismem, jak na zdjęciu.
"""
import math
import random
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

FONT_B = "/usr/share/fonts/truetype/dejavu/DejaVuSansCondensed-Bold.ttf"


def graffiti(out, W_m=9.3, H_m=1.45, v0=0.65, px_m=220):
    W, H = int(W_m * px_m), int(H_m * px_m)
    R = random.Random(32)
    ink = Image.new("L", (W, H), 0); d = ImageDraw.Draw(ink)
    # litery prowadzone kreską markera: rysuję tekst konturem fontu cienkim (pismo odręczne), potem drgania
    words = [("PDW", .75, 2.55), ("FRYZJER", 2.85, 5.85), ("HWDP", 6.05, 7.70)]
    top, bot = 2.08 - v0, 0.86 - v0                                # w metrach od dołu pasa
    for txt, a, b in words:
        size = int((top - bot) * px_m * 1.32)
        f = ImageFont.truetype(FONT_B, size)
        tw = d.textlength(txt, font=f)
        lay = Image.new("L", (int(tw) + 40, size + 60), 0); dl = ImageDraw.Draw(lay)
        dl.text((20, 10), txt, font=f, fill=255)
        lay = lay.crop(lay.getbbox())                                # litera wypełnia całą wysokość pola (jak na zdjęciu)
        lay = lay.resize((int((b - a) * px_m), int((top - bot) * px_m)), Image.LANCZOS)
        # odręczne pismo markerem: oś litery (szkielet) pogrubiona do ~6 cm, z drganiem ręki
        from skimage.morphology import skeletonize
        arr = np.array(lay, np.float32) / 255 > .5
        sk = skeletonize(arr).astype(np.float32)
        hh, ww = sk.shape
        yy, xx = np.mgrid[0:hh, 0:ww]
        dx = (np.sin(yy / 23.0 + R.random() * 6) * 2.5 + np.sin(xx / 41.0) * 1.5).astype(np.float32)
        dy = (np.sin(xx / 29.0 + R.random() * 6) * 2.5).astype(np.float32)
        import cv2
        sk = cv2.remap(sk, (xx + dx).astype(np.float32), (yy + dy).astype(np.float32), cv2.INTER_LINEAR)
        r = max(2, int(.028 * px_m))
        kreska = cv2.dilate((sk > .2).astype(np.uint8) * 255, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (2 * r + 1, 2 * r + 1)))
        kreska = cv2.GaussianBlur(kreska, (0, 0), 1.2).astype(np.float32) / 255
        sh = Image.fromarray((kreska * 255).astype(np.uint8)).transform(lay.size, Image.AFFINE, (1, R.uniform(-.08, .03), 0, 0, 1, 0), Image.BICUBIC)
        ink.paste(sh, (int(a * px_m), int(H - top * px_m)), sh)
    a = np.array(ink, np.float32) / 255
    # zacieki farby w dół z części kresek
    rng = np.random.default_rng(5)
    for _ in range(26):
        x = int(rng.uniform(.8, 7.6) * px_m); ys = np.nonzero(a[:, x] > .5)[0]
        if len(ys) == 0: continue
        y0 = ys.max(); L = int(rng.uniform(.03, .16) * px_m)
        for y in range(y0, min(H - 1, y0 + L)):
            a[y, x - 1:x + 2] = np.maximum(a[y, x - 1:x + 2], 1 - (y - y0) / L * .7)
    # mgiełka sprayu
    a = np.maximum(a, np.array(Image.fromarray((a * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(4)), np.float32) / 255 * .35)
    rgba = np.zeros((H, W, 4), np.uint8); rgba[..., :3] = (18, 18, 20); rgba[..., 3] = (np.clip(a, 0, 1) * 235).astype(np.uint8)
    # niebieska wrzutka (mazaj) na „FRYZJER”
    tag = Image.new("L", (W, H), 0); dt = ImageDraw.Draw(tag)
    pts = [(3.55 + .25 * math.sin(i * .9) + i * .03, .55 + .18 * math.cos(i * 1.7)) for i in range(28)]
    dt.line([(int(x * px_m), int(H - y * px_m)) for x, y in pts], fill=255, width=7)
    ta = np.array(tag, np.float32) / 255
    m = ta > .1
    rgba[m, :3] = (70, 90, 170); rgba[m, 3] = np.maximum(rgba[m, 3], (ta[m] * 200).astype(np.uint8))
    Image.fromarray(rgba, "RGBA").save(out)


def napis(out, W_m=5.6, H_m=3.0, px_m=200, nr="32a"):
    W, H = int(W_m * px_m), int(H_m * px_m)
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0)); d = ImageDraw.Draw(img)
    # jaśniejsze pole (stara farba), lekko nierówne brzegi
    pole = Image.new("L", (W, H), 0); dp = ImageDraw.Draw(pole)
    dp.rectangle((int(.1 * px_m), int(.1 * px_m), W - int(.1 * px_m), H - int(.1 * px_m)), fill=150)
    pole = pole.filter(ImageFilter.GaussianBlur(6))
    rgba = np.zeros((H, W, 4), np.uint8); rgba[..., :3] = (226, 216, 190); rgba[..., 3] = np.array(pole)
    img = Image.fromarray(rgba, "RGBA"); d = ImageDraw.Draw(img)
    f1 = ImageFont.truetype(FONT_B, int(1.15 * px_m)); f2 = ImageFont.truetype(FONT_B, int(.46 * px_m))
    t1 = nr; w1 = d.textlength(t1, font=f1)
    d.text((W - int(.35 * px_m) - w1, int(.15 * px_m)), t1, font=f1, fill=(28, 28, 30, 245))
    t2 = "M. SKŁODOWSKIEJ-CURIE"; w2 = d.textlength(t2, font=f2)
    d.text(((W - w2) / 2, int(1.85 * px_m)), t2, font=f2, fill=(28, 28, 30, 245))
    img.save(out)


def brud(out, W_m, H_m, sila, pas_srodek=None, seed=1, px_m=24):
    W, H = int(W_m * px_m), int(H_m * px_m)
    rng = np.random.default_rng(seed)
    m = np.ones((H, W), np.float32)
    y = np.arange(H)[:, None] / px_m                                 # metry od góry
    # zacieki spod attyki: pionowe smugi o różnej długości
    for _ in range(int(W_m * 3)):
        x = rng.uniform(0, W_m); L = rng.uniform(2, 14); s = rng.uniform(.04, .22) * sila
        w = rng.uniform(.15, .6)
        xs = np.arange(W)[None, :] / px_m
        prof = np.exp(-((xs - x) / w) ** 2) * np.clip(1 - y / L, 0, 1) ** 1.5
        m -= s * prof
    # ciemniejszy pas zawilgocenia na wysokości złącza płyt (jak na zdjęciu, ok. połowy ściany wejściowej)
    if pas_srodek:
        yc, hh = pas_srodek
        noise = np.array(Image.fromarray((rng.random((H // 8 + 1, W // 8 + 1)) * 255).astype(np.uint8)).resize((W, H), Image.BICUBIC), np.float32) / 255
        band = np.exp(-((y - yc) / hh) ** 2) * (.5 + .5 * noise)
        m -= .25 * sila * band
        for _ in range(int(W_m * 4)):                               # smugi w dół z pasa
            x = rng.uniform(0, W_m); L = rng.uniform(.5, 4); xs = np.arange(W)[None, :] / px_m
            m -= .12 * sila * np.exp(-((xs - x) / .08) ** 2) * (np.clip((y - yc) / L, 0, 1) > 0) * np.clip(1 - (y - yc) / L, 0, 1)
    # drobne plamy
    noise = np.array(Image.fromarray((rng.random((H // 4 + 1, W // 4 + 1)) * 255).astype(np.uint8)).resize((W, H), Image.BICUBIC), np.float32) / 255
    m -= .06 * sila * noise
    m = np.clip(m, .45, 1)
    Image.fromarray((m * 255).astype(np.uint8), "L").save(out)


def napis_blok(out, nr, W_m=5.2, H_m=2.1, px_m=200):
    """Napis na szczycie bloku czterokondygnacyjnego (IMG_1611: „M. SKŁODOWSKIEJ / -CURIE 30” czarną farbą, bez pola)."""
    W, H = int(W_m * px_m), int(H_m * px_m)
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0)); d = ImageDraw.Draw(img)
    f = ImageFont.truetype(FONT_B, int(.62 * px_m))
    for i, txt in enumerate(("M. SKŁODOWSKIEJ", "-CURIE " + nr)):
        w = d.textlength(txt, font=f)
        lay = Image.new("RGBA", (int(w) + 10, int(.8 * px_m)), (0, 0, 0, 0)); ImageDraw.Draw(lay).text((0, 0), txt, font=f, fill=(30, 30, 32, 240))
        lay = lay.resize((min(W - 20, int(w * .82)), lay.height))
        img.alpha_composite(lay, (W - lay.width - 10, int((.15 + i * .9) * px_m)))
    img.save(out)


def mural_zawisza(out, plik, W_m=13.1, H_m=3.35, udzial=.64, px_m=150):
    """Mural Zawiszy na ścianie baru (plik od właściciela, mural jego kolegi; zgoda 23.11, 08:01 i 15:28). Ściana W_m x H_m,
    mural od frontu przez `udzial` długości, na całą wysokość od 0,1 do 3,25 m. Rozciągnięcie w poziomie wynika z pomiaru
    na zdjęciu IMG_1631 (plik jest węższy niż malunek na ścianie); reszta ściany przezroczysta = gładki tynk."""
    W, H = int(W_m * px_m), int(H_m * px_m)
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    m = Image.open(plik).convert("RGB")
    mw, mh = int(udzial * W_m * px_m), int((3.25 - .10) * px_m)
    m = m.resize((mw, mh), Image.LANCZOS)
    a = Image.new("L", (mw, mh), 255)
    img.paste(m, (0, H - int(3.25 * px_m)), a)
    img.save(out)


def tablica_bar(out, px=400):
    img = Image.new("RGBA", (px * 2, int(px * .48)), (0, 0, 0, 0)); d = ImageDraw.Draw(img)
    f = ImageFont.truetype(FONT_B, int(px * .2)); txt = "BAR MLECZNY"; w = d.textlength(txt, font=f)
    d.text(((img.width - w) / 2, int(px * .12)), txt, font=f, fill=(236, 236, 230, 255)); img.save(out)


def balustrada_prety(out, px=256):
    a = np.zeros((px, px), np.uint8)
    for k in range(9):
        x = int((k * .111 + .03) * px); a[:, x:x + max(2, int(.016 * px))] = 255
    rgba = np.zeros((px, px, 4), np.uint8); rgba[..., :3] = (58, 60, 62); rgba[..., 3] = a
    Image.fromarray(rgba, "RGBA").save(out)


if __name__ == "__main__":
    out = Path(sys.argv[1] if len(sys.argv) > 1 else "."); out.mkdir(parents=True, exist_ok=True)
    graffiti(out / "graffiti_32a.png")
    for nr in ("28a", "30a", "32a", "34a"):
        napis(out / ("napis_%s.png" % nr), nr=nr)
    brud(out / "brud_32a_slepa.png", 9.3, 31.25, 1.0, pas_srodek=(31.25 - 4.8, .9), seed=3)
    brud(out / "brud_32a_wejscie.png", 9.3, 31.25, .45, seed=4)
    balustrada_prety(out / "balustrada_prety.png")
    tablica_bar(out / "tablica_bar.png")
    for nr in ("28", "30", "32", "34", "36", "38"):
        napis_blok(out / ("napis_blok_%s.png" % nr), nr)
    print("ok")
