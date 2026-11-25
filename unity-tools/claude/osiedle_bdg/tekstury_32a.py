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


def szyldy(out):
    """Szyldy budek (budki.py): ogólne nazwy potraw, bez marek (ZASADY 7.4)."""
    for plik, napisy, kol in (("szyld_bosna.png", [("BOSNA", .62), ("ZAPIEKANKI · HOT-DOG", .2)], (200, 30, 26, 255)),
                              ("szyld_burger.png", [("BURGERY", .58), ("100% WOŁOWINA · FRYTKI", .2)], (236, 196, 40, 255)),
                              ("szyld_zapiekanki.png", [("ZAPIEKANKI", .5), ("HOT-DOG · NAPOJE", .2)], (250, 236, 120, 255)),
                              ("szyld_lody.png", [("LODZIARNIA", .62), ("LODY TRADYCYJNE · KAWA", .2)], (236, 236, 232, 255)),
                              ("szyld_klub.png", [("KLUB", .62), ("MUZYKA · BAR · DO RANA", .2)], (255, 70, 200, 255)),
                              # 25.11 (Street View przed marketem, IMG_2145–2149): napisy ogólne, bez nazw lokali
                              ("szyld_bar.png", [("BAR", .62), ("ZAPIEKANKI · NAPOJE", .2)], (176, 176, 172, 255)),
                              ("szyld_kebab.png", [("KEBAB", .7)], (236, 236, 232, 255)),
                              ("szyld_kawa.png", [("KAWA · LODY", .5), ("CIASTA · DESERY", .2)], (236, 236, 232, 255)),
                              # 25.11 budynek 38B (IMG_2152/2153): adres na attyce, sklep spożywczy (bez logo sieci), sushi
                              ("szyld_sklep.png", [("SKLEP SPOŻYWCZY", .46), ("czynne 6–23", .22)], (255, 255, 255, 255)),
                              ("szyld_sushi.png", [("NOVA SUSHI", .62)], (255, 255, 255, 255))):   # 20:05: nazwa lokalu jak na zdjęciu   # klub w podcieniach Focha 2
        W, H = 1024, 256
        img = Image.new("RGBA", (W, H), (0, 0, 0, 0)); d = ImageDraw.Draw(img); y = int(H * .06)
        for txt, wys in napisy:
            f = ImageFont.truetype(FONT_B, int(H * wys)); w = d.textlength(txt, font=f)
            while w > W * .94: f = ImageFont.truetype(FONT_B, f.size - 4); w = d.textlength(txt, font=f)   # 25.11: napis mieści się w szyldzie
            d.text(((W - w) / 2, y), txt, font=f, fill=kol); y += int(H * wys * 1.12)
        img.save(out / plik)


def napis_38b(out):
    """Adres na attyce pawilonu 38B (IMG_2152/2153): ciemnoszare litery, proporcja tekstury = proporcja pola 12 × 0,8 m."""
    W, H = 2400, 160
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0)); d = ImageDraw.Draw(img); txt = "M. Skłodowskiej-Curie 38B"
    f = ImageFont.truetype(FONT_B, 120)
    while d.textlength(txt, font=f) > W * .96: f = ImageFont.truetype(FONT_B, f.size - 4)
    d.text(((W - d.textlength(txt, font=f)) / 2, (H - f.size) / 2 - 8), txt, font=f, fill=(52, 54, 58, 255))
    img.save(out / "napis_38b.png")


def menu_bosna(out):
    """Tablica z menu busa z bosną (zdjęcie food trucka 00:04): czarna tablica, białe i żółte napisy kredą — ogólne potrawy."""
    W, H = 512, 1024
    img = Image.new("RGBA", (W, H), (26, 28, 28, 255)); d = ImageDraw.Draw(img)
    f1 = ImageFont.truetype(FONT_B, 92); f2 = ImageFont.truetype(FONT_B, 40); f3 = ImageFont.truetype(FONT_B, 34)
    d.text((W / 2 - d.textlength("MENU", font=f1) / 2, 40), "MENU", font=f1, fill=(250, 214, 60, 255))
    y = 190
    for nazwa, cena in (("BOSNA KLASYCZNA", "16 zł"), ("BOSNA OSTRA", "17 zł"), ("BOSNA Z SEREM", "18 zł"), ("BOSNA XXL", "22 zł"),
                        ("HOT-DOG", "10 zł"), ("FRYTKI", "9 zł"), ("NAPOJE", "6 zł")):
        d.text((36, y), nazwa, font=f2, fill=(240, 240, 236, 255)); d.text((W - 36 - d.textlength(cena, font=f3), y + 4), cena, font=f3, fill=(250, 214, 60, 255))
        y += 104
    d.rectangle([20, 20, W - 20, H - 20], outline=(220, 220, 214, 255), width=6)
    img.save(out / "menu_bosna.png")


def przystanki(out):
    """Przystanki (24.11): tablice z nazwą (granat, biały napis), znak D-15 (niebieski kwadrat, białe pole, autobus),
    rozkład jazdy (biała kartka z nagłówkiem i tabelą godzin)."""
    import paleta
    for k, nazwa in enumerate(paleta.PRZYSTANKI):
        W, H = 1600, 130
        img = Image.new("RGBA", (W, H), (24, 44, 96, 255)); d = ImageDraw.Draw(img)
        f = ImageFont.truetype(FONT_B, 76 if len(nazwa) < 30 else 58); w = d.textlength(nazwa.upper(), font=f)
        d.text(((W - w) / 2, (H - (76 if len(nazwa) < 30 else 58)) / 2 - 6), nazwa.upper(), font=f, fill=(255, 255, 255, 255))
        img.save(out / ("tabl_przyst_%d.png" % k))
    S_ = 512; img = Image.new("RGBA", (S_, S_), (0, 88, 168, 255)); d = ImageDraw.Draw(img)
    d.rectangle([12, 12, S_ - 12, S_ - 12], outline=(255, 255, 255, 255), width=10)
    d.rectangle([90, 90, S_ - 90, S_ - 90], fill=(255, 255, 255, 255))
    d.rounded_rectangle([130, 170, S_ - 130, 340], radius=26, fill=(20, 20, 22, 255))         # autobus z boku
    for x in range(150, S_ - 160, 52): d.rectangle([x, 190, x + 38, 250], fill=(255, 255, 255, 255))
    for x in (185, S_ - 185): d.ellipse([x - 30, 315, x + 30, 375], fill=(20, 20, 22, 255)); d.ellipse([x - 12, 333, x + 12, 357], fill=(255, 255, 255, 255))
    img.save(out / "znak_d15.png")
    img = Image.new("RGBA", (512, 700), (246, 244, 238, 255)); d = ImageDraw.Draw(img)
    f1 = ImageFont.truetype(FONT_B, 40); f2 = ImageFont.truetype(FONT_B, 22)
    d.rectangle([0, 0, 512, 70], fill=(24, 44, 96, 255)); d.text((22, 14), "ROZKŁAD JAZDY", font=f1, fill=(255, 255, 255, 255))
    for i in range(18):
        y = 90 + i * 33; d.line([20, y + 28, 492, y + 28], fill=(200, 200, 196, 255), width=2)
        d.text((24, y), "%02d" % (5 + i), font=f2, fill=(20, 20, 22, 255))
        d.text((80, y), "  ".join("%02d" % m for m in (2, 14, 26, 38, 50)[: 3 + (i % 3)]), font=f2, fill=(60, 60, 64, 255))
    img.save(out / "rozklad.png")


def banery(out):
    """Banery nad podcieniem marketu: reklamy BRAXHORN MARKET (bez prawdziwych marek, ZASADY 7.4)."""
    teksty = [("ŚWIEŻE CODZIENNIE", "pieczywo · warzywa · mięso"), ("NISKIE CENY", "każdego dnia"),
              ("ZAKUPY Z DOSTAWĄ", "zamów online — dowieziemy"), ("PARKING DLA KLIENTÓW", "pierwsze 2 h gratis")]
    f1 = ImageFont.truetype(FONT_B, 92); f2 = ImageFont.truetype(FONT_B, 40); f3 = ImageFont.truetype(FONT_B, 34)
    for k, (a, b) in enumerate(teksty):
        img = Image.new("RGBA", (1024, 286), (24, 64, 150, 255)); d = ImageDraw.Draw(img)
        d.rectangle((0, 226, 1024, 286), fill=(236, 236, 232, 255))
        d.text((40, 30), a, font=f1, fill=(255, 255, 255, 255)); d.text((44, 140), b, font=f2, fill=(255, 168, 40, 255))
        d.text((40, 236), "BRAXHORN MARKET", font=f3, fill=(24, 64, 150, 255))
        img.save(out / ("baner_%d.png" % (k + 1)))


def plot_prety(out, px=256):
    """Płot panelowy z prętów (szpital, IMG_1673): pręty pionowe 2 cm co 12 cm, kafel 1 x 1 m, przezroczyste tło."""
    a = np.zeros((px, px), np.uint8)
    for k in range(8):
        x = int((k * .125 + .05) * px); a[:, x:x + max(2, int(.02 * px))] = 255
    rgba = np.zeros((px, px, 4), np.uint8); rgba[..., :3] = (255, 255, 255); rgba[..., 3] = a
    Image.fromarray(rgba, "RGBA").save(out)


def zywoplot_galazki(dst):
    """Karty żywopłotu: atlas gałązek drzewo_klaster/galazki.png (repo, Poly Haven CC0 — liście island_tree/jacaranda)
    przemalowany jak shader koron (OsiedleLiscie: luminancja / średnia × zieleń) na ciemniejszą zieleń strzyżonego ligustru."""
    import numpy as np
    src = Path(__file__).resolve().parents[3] / "unity-city/Assets/Source/Claude/Osiedle/models/drzewo_klaster/galazki.png"
    a = np.asarray(Image.open(src).convert("RGBA"), float) / 255
    lin = a[..., :3] ** 2.2; lum = lin @ np.array([.2126, .7152, .0722])
    L = np.array([.26, .44, .13]) ** 2.2
    o = np.clip(.85 * L[None, None, :] * (lum / .014248)[..., None] + .15 * lin, 0, 1) ** (1 / 2.2)
    Image.fromarray((np.dstack([o, a[..., 3:]]) * 255).astype(np.uint8), "RGBA").save(dst)


def zywoplot(liscie_rgba, out_lisc, out_brzeg, px=1024, seed=7):
    """Liście żywopłotu złożone z prawdziwego skanu liści (Poly Haven shrub_02, CC0 — atlas z park_shrub.glb biblioteki
    właściciela): ok. 2600 listków 4–7 cm, losowy obrót, trzy warstwy z cieniowaniem w głąb. Kafel 1 x 1 m.
    out_lisc — nieprzezroczysty (rdzeń), out_brzeg — wycinany (zewnętrzna warstwa, nierówna sylwetka)."""
    R = random.Random(seed)
    atl = Image.open(liscie_rgba).convert("RGBA")
    W = atl.width
    # pojedyncze liście z atlasu: spójne obszary alfy w prawej części (lewa to kora)
    import cv2
    al = np.array(atl)[..., 3]; al[:, :int(W * .2)] = 0
    n, lab, st, _ = cv2.connectedComponentsWithStats((al > 128).astype(np.uint8))
    liscie = []
    for i in range(1, n):
        x, y, w, h, area = st[i]
        if area < 2000: continue
        liscie.append(atl.crop((x, y, x + w, y + h)))
    def warstwa(gestosc, jasn, rozm):
        im = Image.new("RGBA", (px, px), (0, 0, 0, 0))
        for _ in range(gestosc):
            l = R.choice(liscie); dl = int(px * R.uniform(*rozm))
            l = l.resize((max(2, int(dl * l.width / l.height * 2.3)), dl), Image.LANCZOS).rotate(R.uniform(0, 360), expand=True, resample=Image.BICUBIC)   # owalne jak ligustr
            arr = np.array(l).astype(np.float32); arr[..., :3] *= jasn * R.uniform(.85, 1.1); arr[..., 0] *= .82; arr[..., 1] *= 1.22; arr[..., 2] *= .7
            l = Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8), "RGBA")
            x, y = R.randrange(px), R.randrange(px)
            for dx in (0, -px):        # zawijanie kafla
                for dy in (0, -px):
                    im.alpha_composite(l, (x + dx, y + dy)) if 0 <= x + dx < px and 0 <= y + dy < px else im.paste(l, (x + dx, y + dy), l)
        return im
    tlo = Image.new("RGBA", (px, px), (28, 40, 20, 255))
    for g, j in ((1400, .45, ), (1100, .7), (900, 1.0)) if False else ((1400, .45), (1100, .7), (900, 1.0)):
        tlo.alpha_composite(warstwa(g, j, (.04, .07)))
    tlo.convert("RGB").save(out_lisc)
    warstwa(1300, 1.0, (.05, .08)).save(out_brzeg)


def balustrada_prety(out, px=256):
    a = np.zeros((px, px), np.uint8)
    for k in range(9):
        x = int((k * .111 + .03) * px); a[:, x:x + max(2, int(.016 * px))] = 255
    rgba = np.zeros((px, px, 4), np.uint8); rgba[..., :3] = (58, 60, 62); rgba[..., 3] = a
    Image.fromarray(rgba, "RGBA").save(out)


def balustrada_kuta(out, px=512):
    """Balustrada nabrzeży Brdy i mostów (zdjęcia m14, m34–m36): czarne żeliwo — pochwyt, dwa rygle, pręty co 12 cm,
    pas kół między ryglami u góry. Kafel 1 × 1 m (v = 0 na dole), przezroczyste tło."""
    from PIL import ImageDraw
    im = Image.new("L", (px, px), 0); d = ImageDraw.Draw(im); m = px / 1.0
    Y = lambda v: int((1 - v) * px)                               # v w metrach od dołu
    for (v0, v1) in ((.96, 1.0), (.78, .81), (.05, .09)):          # pochwyt, rygiel górny, rygiel dolny
        d.rectangle([0, Y(v1), px, Y(v0)], fill=255)
    for k in range(9):                                            # pręty co ~0,111 m (9 na metr — kafel bez szwu)
        x = (k + .5) / 9 * px; w = .016 * m
        d.rectangle([x - w / 2, Y(.78), x + w / 2, Y(.09)], fill=255)
    for k in range(9):                                            # koła między ryglami
        x = (k + .5) / 9 * px; r = .07 * m; yc = Y(.87)
        d.ellipse([x - r, yc - r, x + r, yc + r], outline=255, width=max(2, int(.012 * m)))
    rgba = np.zeros((px, px, 4), np.uint8); rgba[..., :3] = (34, 36, 36); rgba[..., 3] = np.asarray(im)
    Image.fromarray(rgba, "RGBA").save(out)


def balustrada_most(out, px=512):
    """Balustrada mostu Staromiejskiego (zdjęcia m37–m38): czarne żeliwo — pochwyt, rygle, rząd trójkątów (zygzak) z kołami
    w środku, nad nim pas łuczków. Kafel 1 × 1 m (v = 0 na dole), przezroczyste tło."""
    from PIL import ImageDraw
    im = Image.new("L", (px, px), 0); d = ImageDraw.Draw(im); m = px
    Y = lambda v: int((1 - v) * px); X = lambda u: int(u * px); grub = max(3, int(.018 * m))
    for (v0, v1) in ((.96, 1.0), (.76, .80), (.07, .11)):
        d.rectangle([0, Y(v1), px, Y(v0)], fill=255)
    for k in range(4):                                            # zygzak: 4 trójkąty na metr
        u0 = k * .25; d.line([X(u0), Y(.11), X(u0 + .125), Y(.76)], fill=255, width=grub); d.line([X(u0 + .125), Y(.76), X(u0 + .25), Y(.11)], fill=255, width=grub)
        r = .042; uc = u0 + .125; vc = .33
        d.ellipse([X(uc - r), Y(vc + r), X(uc + r), Y(vc - r)], outline=255, width=grub)
    for k in range(8):                                            # łuczki między ryglami u góry
        u0 = k * .125; d.arc([X(u0), Y(.80 + .11), X(u0 + .125), Y(.80 - .05)], 180, 360, fill=255, width=grub)
    rgba = np.zeros((px, px, 4), np.uint8); rgba[..., :3] = (30, 32, 32); rgba[..., 3] = np.asarray(im)
    Image.fromarray(rgba, "RGBA").save(out)


def napis_sor(out, W=1536, H=652):
    """25.11 (właściciel 15:44: „nie ma napisane, że to jest Jurasza Szpital […] tak jak jest oryginalnie”): napis na szarym
    szklanym panelu frontu SOR — jak na zdjęciu ulicy (o95, Skłodowskiej-Curie 9): białe wersaliki, u góry nazwa szpitala,
    u dołu nazwa oddziału. Przezroczyste tło (wypiekane na barwę panelu)."""
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    for txt, y, frac in (("SZPITAL UNIWERSYTECKI NR 1 im. dr. A. JURASZA", .13, .80), ("SZPITALNY ODDZIAŁ RATUNKOWY", .74, .62)):
        size = 200
        while size > 10:
            f = ImageFont.truetype(FONT_B, size)
            if d.textlength(txt, font=f) <= W * frac: break
            size -= 2
        w = d.textlength(txt, font=f)
        d.text(((W - w) / 2, int(H * y)), txt, font=f, fill=(246, 247, 248, 255))
    im.save(out)


def wypiecz(src, dst_gra, dst_prywatne):
    """Do gry: naklejka nałożona na barwę ściany (paleta) — jedna tekstura na UV2. Mural (plik właściciela) do katalogu
    prywatnego (paczka szyfrowana), reszta (nasze napisy i graffiti) do Assets/Source/Claude/Bydgoszcz."""
    import paleta
    src, dst_gra, dst_prywatne = Path(src), Path(dst_gra), Path(dst_prywatne)
    dst_gra.mkdir(parents=True, exist_ok=True); dst_prywatne.mkdir(parents=True, exist_ok=True)
    for nazwa, v in paleta.P.items():
        if not v.get("nakl") or v.get("prywatny"): continue      # szyldy właściciela: tylko w paczce szyfrowanej
        nk = Image.open(src / v["nakl"]).convert("RGBA")
        tlo = Image.new("RGBA", nk.size, tuple(v["kol"]) + (255,))
        tlo.alpha_composite(nk)
        (dst_prywatne if nazwa == "mural_zawisza" else dst_gra).joinpath(nazwa + ".png").parent.mkdir(parents=True, exist_ok=True)
        tlo.convert("RGB").save((dst_prywatne if nazwa == "mural_zawisza" else dst_gra) / (nazwa + ".png"))
    for f in ("balustrada_prety.png", "plot_prety.png", "zywoplot_lisc.png", "zywoplot_brzeg.png", "zywoplot_galazki.png", "balustrada_kuta.png", "balustrada_most.png"):
        Image.open(src / f).save(dst_gra / f)


if __name__ == "__main__":
    out = Path(sys.argv[1] if len(sys.argv) > 1 else "."); out.mkdir(parents=True, exist_ok=True)
    graffiti(out / "graffiti_32a.png")
    for nr in ("28a", "30a", "32a", "34a"):
        napis(out / ("napis_%s.png" % nr), nr=nr)
    brud(out / "brud_32a_slepa.png", 9.3, 31.25, 1.0, pas_srodek=(31.25 - 4.8, .9), seed=3)
    brud(out / "brud_32a_wejscie.png", 9.3, 31.25, .45, seed=4)
    balustrada_prety(out / "balustrada_prety.png")
    balustrada_kuta(out / "balustrada_kuta.png")
    balustrada_most(out / "balustrada_most.png")
    tablica_bar(out / "tablica_bar.png")
    plot_prety(out / "plot_prety.png")
    import sys as _s
    if len(_s.argv) > 2: zywoplot(_s.argv[2], out / "zywoplot_lisc.png", out / "zywoplot_brzeg.png")
    zywoplot_galazki(out / "zywoplot_galazki.png")
    szyldy(out)
    menu_bosna(out)
    przystanki(out)
    banery(out)
    for nr in ("28", "30", "32", "34", "36", "38"):
        napis_blok(out / ("napis_blok_%s.png" % nr), nr)
    print("ok")
