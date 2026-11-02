#!/usr/bin/env python3
"""BRAXHORN build 14 — PANORAMA DOOKOŁA OSIEDLA (właściciel: „za płotami ma być realna panorama zajebista — góry, lasy,
pola, z każdej strony inny widok”).

Źródło: prawdziwe zdjęcie 360° Poly Haven „Alps Field” (Andreas Mischok, CC0): dolina alpejska — zalesione zbocza,
skały, łąki, wieś z kościołem, ośnieżony szczyt w głębi doliny. Z panoramy wycinany jest pas horyzontu (−10°…+35°,
4096 × 512 px, kanał alfa = ziemia/niebo). Niebo nad górami zostaje NASZE (chmury płyną, słońce gry) — shader nieba
skleja oba po masce. W kierunku tunelu (północ) w głębi doliny dorysowana sylwetka naszego miasta (wieżowce za
wzgórzem tunelu, mgiełka powietrza), tanio — w samej teksturze.

Kierunki (shader: u = 0.25 + azymut/360 + przesunięcie 0.02): północ = dolina z ośnieżonym szczytem + miasto,
wschód = zalesiona góra, południe = dolina z wsią i polami, zachód = zalesiona góra ze skałami. Słońce zdjęcia
(azymut ~114°) blisko słońca gry (145°) — oświetlenie gór zgadza się ze scenią.

Wyjście: <root>/sky/panorama_pasmo.png (RGBA), <root>/sky/panorama_pasmo.json (zakres, średni kolor horyzontu).
"""
import io
import json
import math
import pathlib

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

W, H = 4096, 512
EL_TOP, EL_BOT = 35.0, -10.0          # zakres elewacji pasa (stopnie)
OFFSET = 0.02                          # przesunięcie u (musi być równe _PasmoU0 w shaderze / BraxClaudeOsiedleKraniec.cs)


def el_of_row(r):
    return EL_TOP - (r + .5) * (EL_TOP - EL_BOT) / H


def row_of_el(e):
    return (EL_TOP - e) * H / (EL_TOP - EL_BOT) - .5


def band_from_equirect(eq):
    """eq: obraz równoodległościowy (dowolny rozmiar, 2:1) → pas W×H (RGB float 0..1)."""
    eq = eq.convert("RGB")
    EW, EH = eq.size
    top = int((90 - EL_TOP) / 180 * EH)
    bot = int(math.ceil((90 - EL_BOT) / 180 * EH))
    crop = eq.crop((0, top, EW, bot))
    return np.asarray(crop.resize((W, H), Image.LANCZOS)).astype(np.float32) / 255.0


def sky_mask(band):
    """Maska ziemi: dla każdej kolumny linia horyzontu (od dołu w górę: pierwsze 5 pikseli z rzędu jasnego błękitu)."""
    r, g, b = band[..., 0] * 255, band[..., 1] * 255, band[..., 2] * 255
    lum = .3 * r + .59 * g + .11 * b
    sky = (b >= 168) & (b - r >= 24) & (b >= g) & (lum >= 140)
    start = int(row_of_el(-1.0))
    line = np.zeros(W, np.float32)
    for x in range(W):
        run, found = 0, 0
        for y in range(start, 0, -1):
            if sky[y, x]:
                run += 1
                if run >= 5:
                    found = y + 5
                    break
            else:
                run = 0
        line[x] = found
    # wygładzenie: mediana 7 kolumn (pojedyncze błędne kolumny), krawędź miękka ±1.2 px
    pad = np.concatenate([line[-3:], line, line[:3]])
    med = np.array([np.median(pad[i:i + 7]) for i in range(W)])
    rows = np.arange(H)[:, None].astype(np.float32)
    alpha = np.clip((rows - (med[None, :] - .6)) / 1.2 + .5, 0, 1)
    return alpha, med


def city_skyline(band, alpha, rng):
    """Sylwetka miasta w głębi doliny na północy (azymut −21°…+21°, ~2.5–3 km): trzy plany zabudowy (dalszy niższy
    i bardziej zamglony), wieżowce z uskokami, iglicami i skośnymi dachami, szkło odbijające niebo (jaśniejsza góra,
    pionowy refleks), piętra i słupki fasad, strona południowa w słońcu, boczna w cieniu; u podstawy mgiełka doliny.
    Rysowane w 3× rozdzielczości i zmniejszane (gładkie krawędzie)."""
    SS = 3
    def u_of_az(az):
        return (0.25 + az / 360.0 + OFFSET) % 1.0
    x0, x1 = int(u_of_az(-21) * W), int(u_of_az(21) * W)
    cw, chh = (x1 - x0) * SS, H * SS
    lay = np.zeros((chh, cw, 4), np.float32)
    haze = np.median(band[int(row_of_el(5)):int(row_of_el(2)), x0:x1].reshape(-1, 3), axis=0)   # powietrze nad doliną
    sky_hi = np.median(band[int(row_of_el(14)):int(row_of_el(10)), x0:x1].reshape(-1, 3), axis=0)
    base_el = .3
    def R(e):
        return row_of_el(e) * SS
    base = R(base_el)
    palette = [(.60, .67, .74), (.50, .58, .68), (.72, .73, .74), (.82, .77, .69), (.40, .48, .60), (.66, .70, .72), (.58, .56, .53), (.86, .84, .80), (.34, .42, .52)]
    n_all = 0
    for layer, (fog, wmin, wmax, hmin, hmax, step) in enumerate([(.62, 6, 15, .9, 3.6, .55), (.48, 8, 20, 1.8, 8.5, .9), (.36, 10, 24, 3.0, 12.5, 1.4)]):
        x = float(rng.uniform(0, 6))
        while x < (x1 - x0) - 4:
            t = x / (x1 - x0)
            prof = math.exp(-((t - .5) / .2) ** 2) * .85 + math.exp(-((t - .2) / .08) ** 2) * .35   # centrum + mniejsze skupisko
            w = float(rng.uniform(wmin, wmax))
            if rng.random() > .25 + .75 * prof and layer > 0:
                x += w * step
                continue
            h_el = hmin + (hmax - hmin) * (prof * rng.uniform(.6, 1.0) + rng.uniform(0, .25))
            col = np.array(palette[int(rng.integers(len(palette)))]) * rng.uniform(.9, 1.08)
            glass = rng.random() < .6
            style = rng.random()
            X0, X1 = x * SS, (x + w) * SS
            top = R(h_el)
            side = max(SS * 2, (X1 - X0) * rng.uniform(.18, .3))
            def face(xa, xb, ya, k):
                xa, xb, ya = int(max(0, xa)), int(min(cw, xb)), int(max(0, ya))
                if xb <= xa or ya >= base:
                    return
                hh = int(base) - ya
                yy = np.arange(hh)[:, None] / max(1, hh)
                c = col[None, None, :] * k * (1.12 - .25 * yy[..., None])           # jaśniejsza góra, ciemniejszy dół
                if glass:
                    c = c * .75 + sky_hi[None, None, :] * .25 * (1.2 - yy[..., None])   # szkło odbija niebo
                    xr = np.arange(xb - xa)[None, :] / max(1, xb - xa)
                    c = c + .08 * np.exp(-((xr - .35) / .12) ** 2)[..., None] * k      # pionowy refleks
                rows = (np.arange(hh) // SS) % 3 == 0                                    # piętra
                c[rows] *= .9
                if glass:
                    cols = (np.arange(xb - xa) // SS) % 4 == 0                           # słupki fasady
                    c[:, cols] *= .93
                f = fog + (1 - fog) * .55 * yy[..., None] ** 2                           # mgiełka doliny u podstawy
                c = c * (1 - f) + haze[None, None, :] * f
                lay[ya:ya + hh, xa:xb, :3] = c
                lay[ya:ya + hh, xa:xb, 3] = 1
            if style < .25 and h_el > 3:                                                # uskok: górna część węższa
                cut = top + (base - top) * rng.uniform(.18, .35)
                face(X0, X1 - side, cut, 1.0); face(X1 - side, X1, cut + SS, .74)
                inset = (X1 - X0) * rng.uniform(.15, .3)
                face(X0 + inset, X1 - side - inset * .3, top, 1.03); face(X1 - side - inset * .3, X1 - inset * .3, top + SS, .76)
            else:
                face(X0, X1 - side, top, 1.0); face(X1 - side, X1, top + SS, .74)
                if .25 <= style < .37 and h_el > 5:                                     # iglica / maszt
                    xm = int((X0 + X1 - side) / 2)
                    hs = int(rng.uniform(5, 14) * SS)
                    lay[int(top) - hs:int(top), xm - SS // 2:xm + SS // 2 + 1, :3] = col * .85 * (1 - fog) + haze * fog
                    lay[int(top) - hs:int(top), xm - SS // 2:xm + SS // 2 + 1, 3] = 1
                elif .37 <= style < .47 and h_el > 3:                                   # skośny dach
                    hh = int((X1 - X0) * .35)
                    for q in range(hh):
                        a, b = int(X0 + (X1 - X0) * q / hh * .5), int(X1 - (X1 - X0) * q / hh * .5)
                        y = int(top) - q
                        if y >= 0 and b > a:
                            lay[y, a:b, :3] = col * 1.05 * (1 - fog) + haze * fog
                            lay[y, a:b, 3] = 1
            n_all += 1
            x += w * step + float(rng.uniform(-2, 3))
    # zmniejszenie 3× (krawędzie wygładzone), lekka mgiełka odległości
    big = Image.fromarray((np.clip(lay, 0, 1) * 255).astype(np.uint8), "RGBA")
    small = big.resize((x1 - x0, H), Image.LANCZOS).filter(ImageFilter.GaussianBlur(.35))
    city = np.asarray(small).astype(np.float32) / 255
    # miasto stoi ZA łąką fotografa: w każdej kolumnie zasłania je bliska łąka (od −1° w górę, dopóki piksel jest jasną
    # zielenią pola); powyżej — dalsza dolina, góry, niebo
    seg = band[:, x0:x1] * 255
    field = (seg[..., 1] > seg[..., 2] + 18) & (seg[..., 1] >= seg[..., 0] - 4) & ((.3 * seg[..., 0] + .59 * seg[..., 1] + .11 * seg[..., 2]) > 88)
    near = np.zeros((H, x1 - x0), np.float32)
    r_start = int(row_of_el(-1.0))
    for c in range(x1 - x0):
        r = r_start
        while r > int(row_of_el(4.0)) and field[r, c]:
            r -= 1
        near[r + 1:, c] = 1
    near = np.asarray(Image.fromarray((near * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(.8))).astype(np.float32) / 255
    city[..., 3] *= 1 - near
    a_c = city[..., 3:4]
    rgb = band.copy()
    rgb[:, x0:x1] = rgb[:, x0:x1] * (1 - a_c) + city[..., :3] * a_c
    al = alpha.copy()
    al[:, x0:x1] = np.maximum(al[:, x0:x1], a_c[..., 0])
    return rgb, al, n_all


def usun_przedmioty(band):
    """Alps Field: drewniana rama i biały słupek na łące przed fotografem (u≈0.23 i 0.339, tuż nad horyzontem) —
    zamalowane łąką dookoła (inpaint), żeby nie stały „w niebie” nad wzgórzem tunelu."""
    try:
        import cv2
    except Exception:
        return band
    img = (np.clip(band, 0, 1) * 255).astype(np.uint8)
    mask = np.zeros(img.shape[:2], np.uint8)
    for (u0, r0, u1, r1) in [(926, 400, 956, 460), (1383, 352, 1398, 377)]:
        mask[r0:r1, int(u0 * W / 4096):int(u1 * W / 4096)] = 255
    out = cv2.inpaint(img, mask, 5, cv2.INPAINT_TELEA)
    return out.astype(np.float32) / 255


def build(eq_image, out_png, out_json, seed=2026):
    band = band_from_equirect(eq_image)
    band = usun_przedmioty(band)
    alpha, line = sky_mask(band)
    rgb, alpha, n_city = city_skyline(band, alpha, np.random.default_rng(seed))
    # pod przezroczystymi pikselami (niebo) kolor ziemi tuż pod linią — bez jasnej obwódki przy filtrowaniu
    out = np.concatenate([np.clip(rgb, 0, 1), alpha[..., None]], axis=2)
    Image.fromarray((out * 255 + .5).astype(np.uint8), "RGBA").save(out_png, optimize=True)
    r1 = int(row_of_el(1.5)); r0 = int(row_of_el(-.5))
    hor = rgb[r1:r0].reshape(-1, 3).mean(0)
    info = {"zrodlo": "Poly Haven alps_field (CC0, Andreas Mischok)", "szerokosc": W, "wysokosc": H, "elewacja_gora": EL_TOP,
            "elewacja_dol": EL_BOT, "przesuniecie_u": OFFSET, "wiezowce_miasta": n_city,
            "horyzont_srgb": [round(float(v), 4) for v in hor],
            "linia_max_st": round(float(el_of_row(line.min())), 1), "linia_min_st": round(float(el_of_row(line.max())), 1)}
    pathlib.Path(out_json).write_text(json.dumps(info, indent=1, ensure_ascii=False))
    (pathlib.Path(out_png).parent / "CREDITS.md").write_text(
        "# Niebo i panorama osiedla\n\nNiebo z chmurami: Poly Haven „Kloofendal 48d Partly Cloudy (Pure Sky)” (CC0).\n"
        "Panorama gór, lasów i pól: Poly Haven „Alps Field”, autor Andreas Mischok (CC0); dorysowana sylwetka miasta.\n", encoding="utf-8")
    return info


def znak_wkrotce(out, w=1024, h=384):
    """Tablica na szlabanie tunelu: biała, czerwona ramka, MIASTO · WKRÓTCE (koniec rejonu w aplikacji „osiedle”)."""
    from tekstury_b5 import font, FB, _fit
    img = Image.new("RGB", (w, h), (246, 246, 244)); d = ImageDraw.Draw(img)
    d.rectangle([8, 8, w - 9, h - 9], outline=(196, 24, 30), width=22)
    f1, s1 = _fit(d, "MIASTO", FB, 150, w - 160); tw = d.textlength("MIASTO", font=f1)
    d.text(((w - tw) / 2, 52), "MIASTO", font=f1, fill=(20, 20, 22))
    f2, s2 = _fit(d, "WKRÓTCE", FB, 110, w - 200); tw = d.textlength("WKRÓTCE", font=f2)
    d.text(((w - tw) / 2, 222), "WKRÓTCE", font=f2, fill=(196, 24, 30))
    img.save(out)


def znak_zakaz(out, s=512):
    """Znak B-1 „zakaz ruchu w obu kierunkach”: białe koło w czerwonej obwódce (tło przezroczyste)."""
    img = Image.new("RGBA", (s, s), (0, 0, 0, 0)); d = ImageDraw.Draw(img)
    d.ellipse([6, 6, s - 7, s - 7], fill=(200, 26, 32, 255))
    d.ellipse([int(s * .14), int(s * .14), int(s * .86), int(s * .86)], fill=(248, 248, 246, 255))
    img.save(out)


if __name__ == "__main__":
    import sys
    Image.MAX_IMAGE_PIXELS = None
    print(build(Image.open(sys.argv[1]), sys.argv[2], sys.argv[3]))
