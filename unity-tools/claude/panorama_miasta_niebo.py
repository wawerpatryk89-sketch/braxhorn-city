#!/usr/bin/env python3
"""MIASTO R1 — panorama miasta, krok 2: SŁONECZNE NIEBO zamiast pochmurnego (właściciel 18.11 16:04, film z Osiedla:
„kolorystyka jak w osiedlu… nie może być gorzej… minimum… i zobacz, jaka jest panorama”; odpowiedź 16:2x na propozycję
nieba z Osiedla: „Zrób tak, żeby było dobrze, a inaczej jak na osiedlu”).

Sklejka z kroku 1 (panorama_miasta.py: ląd „cannon” + morze „dalkey_view”) ma niebo ze zdjęcia „cannon” — pochmurne, szare,
płaskie światło. Ten krok zostawia ląd i morze zdjęć 1 i 2, a niebo bierze z innego prawdziwego zdjęcia 360°:
  N = https://polyhaven.com/a/sunflowers_puresky   — letnie niebo z cumulusami (Poly Haven, CC0; INNE niż na Osiedlu,
      gdzie jest kloofendal_48d_partly_cloudy_puresky)
  kolor wody przy słońcu z https://polyhaven.com/a/sundowner_overlook (CC0, ocean ze wzgórza przy czystym niebie) —
      zmierzone mediany wody pod horyzontem (tablica PROF).
Pobranie: https://dl.polyhaven.org/file/ph-assets/HDRIs/extra/Tonemapped%20JPG/<id>.jpg (eq_sunflowers_puresky.jpg).

Jak:
 1) linia nieba w każdej kolumnie (4096 × 2048, pas −1…+18°): klasyfikator z tego samego zdjęcia (mieszaniny Gaussa:
    niebo = elewacje ≥ 12°, ląd = tuż pod horyzontem poza zatoką; cechy L*, a*, b*, faktura), granica z programowania
    dynamicznego (jedna na kolumnę, kara za skok), otwarcie profilu 0.7° (bez „słupów” starych chmur);
    nad otwartą wodą zatoki granica na horyzoncie, wyżej tylko ląd ciemniejszy od powietrza tego wiersza (cypel, wysepki);
    mgła i jasna obwódka HDR przy grzbietach → niebo;
 2) niebo N obrócone tak, że jego słońce leży na azymucie słońca miasta (CityStaticPBR, BraxCityMaterials.SunDirection);
    błękit podbity wokół luminancji (liniowo; chmury ×1.15, czysty błękit do ×NAS, przy horyzoncie bez podbicia);
 3) ląd zdjęć: nasycenie ×1.2; woda zatoki: ten sam obraz fal, kolor wiersz po wierszu jak woda przy słońcu (PROF);
 4) wtopienie: na lądzie 2 px, nad wodą 0.5°.
Wynik: ten sam plik 8192 × 4096 (układ miasta: kolumna = azymut od +z ku +x, horyzont na elewacji 0).

Użycie: python3 panorama_miasta_niebo.py <sklejka.jpg> <eq_sunflowers_puresky.jpg> <wyjście.jpg> [azymut słońca] [nasycenie]
"""
import sys

import cv2
import numpy as np
from PIL import Image

Image.MAX_IMAGE_PIXELS = None

SLONCE_AZ = 226.97          # azymut słońca miasta: atan2(x, z) kierunku (−.45, .78, −.42) (od +z ku +x)
NAS = 1.9                   # podbicie czystego błękitu (Osiedle: 2.1 na całym niebie)
# woda przy czystym niebie (sundowner_overlook, horyzont −0.593°): stopnie pod horyzontem → mediana sRGB
PROF = np.array([[-.2, 151, 178, 209], [-.4, 137, 168, 202], [-.7, 122, 159, 196], [-1., 114, 154, 192],
                 [-1.5, 108, 153, 189], [-2., 107, 154, 188], [-45., 107, 154, 188]], np.float32)
ZATOKA = (40.0, 135.0)      # otwarta woda zatoki w układzie miasta (sektor morza 41…134°)


def lin(c): return np.where(c <= .04045, c / 12.92, ((c + .055) / 1.055) ** 2.4)
def srgb(c): return np.where(c <= .0031308, c * 12.92, 1.055 * np.power(np.clip(c, 0, None), 1 / 2.4) - .055)


def linia_nieba(P):
    """Wiersz granicy nieba (w obrazie 2× mniejszym) dla każdej kolumny 4096."""
    from scipy.ndimage import grey_opening
    from sklearn.mixture import GaussianMixture
    H, W, _ = P.shape; h2, w2 = H // 2, W // 2
    p2 = cv2.resize(P, (w2, h2), interpolation=cv2.INTER_AREA)
    r0, r1 = int(round((90 - 18) / 180 * h2)), int(round((90 + 1.0) / 180 * h2))
    el = 90 - (np.arange(r0, r1) + .5) / h2 * 180
    band = np.ascontiguousarray(p2[r0:r1])
    LAB = cv2.cvtColor(band, cv2.COLOR_RGB2LAB).astype(np.float32)
    L = LAB[..., 0] * 100 / 255
    lap = cv2.blur(np.abs(cv2.Laplacian(cv2.cvtColor(band, cv2.COLOR_RGB2GRAY).astype(np.float32), cv2.CV_32F, ksize=3)), (3, 3))
    az = (np.arange(w2) + .5) / w2 * 360
    zatoka = (az > ZATOKA[0]) & (az < ZATOKA[1])
    F = np.stack([L, LAB[..., 1] - 128, LAB[..., 2] - 128, np.log1p(lap) * 12], -1)
    rng = np.random.default_rng(3)
    Fs = F[el > 12].reshape(-1, 4); Fl = F[np.ix_((el < -.25) & (el > -1.0), ~zatoka)].reshape(-1, 4)
    Fs = Fs[rng.choice(len(Fs), min(60000, len(Fs)), replace=False)]; Fl = Fl[rng.choice(len(Fl), min(60000, len(Fl)), replace=False)]
    Fs = Fs.astype(np.float64) + rng.normal(0, .5, Fs.shape); Fl = Fl.astype(np.float64) + rng.normal(0, .5, Fl.shape)
    gs = GaussianMixture(8, covariance_type='full', reg_covar=1.0, random_state=0).fit(Fs)
    gl = GaussianMixture(10, covariance_type='full', reg_covar=1.0, random_state=0).fit(Fl)
    F64 = F.reshape(-1, 4).astype(np.float64)
    ziem = 1 / (1 + np.exp(np.clip(gs.score_samples(F64) - gl.score_samples(F64), -30, 30))).reshape(L.shape)
    ziem = cv2.blur(ziem.astype(np.float32), (3, 3))
    ziem[el < 0] = np.maximum(ziem[el < 0], .5)                               # pod horyzontem nigdy niebo
    ziem[np.ix_(el > 1.3, zatoka)] = 0                                          # nad otwartą wodą ląd najwyżej do +1.3°
    nr = len(el); LAM = .12
    C = np.vstack([np.zeros((1, w2), np.float32), np.cumsum(ziem, 0)]) + \
        np.vstack([np.cumsum((1 - ziem)[::-1], 0)[::-1], np.zeros((1, w2), np.float32)])
    koszt = C[:, 0].copy(); skad = np.zeros((nr + 1, w2), np.int32); idx = np.arange(nr + 1)
    for x in range(1, w2):                                                      # min po b' z koszt[b'] + λ|b − b'|
        k = koszt.copy(); arg = idx.copy()
        for i in range(1, nr + 1):
            if k[i - 1] + LAM < k[i]: k[i] = k[i - 1] + LAM; arg[i] = arg[i - 1]
        for i in range(nr - 1, -1, -1):
            if k[i + 1] + LAM < k[i]: k[i] = k[i + 1] + LAM; arg[i] = arg[i + 1]
        skad[:, x] = arg; koszt = k + C[:, x]
    gr = np.zeros(w2, np.int32); gr[-1] = int(np.argmin(koszt))
    for x in range(w2 - 1, 0, -1): gr[x - 1] = skad[gr[x], x]
    wys = grey_opening((nr - gr).astype(np.float32), size=int(round(.7 / 360 * w2)) | 1, mode='wrap')
    gr = (nr - wys).astype(np.float32)
    # zatoka: granica na horyzoncie, wyżej tylko ląd ciemniejszy od powietrza tego wiersza (mediana nad otwartą wodą)
    ih = int(np.argmax(el < 0))
    Lrow = np.median(L[:, (az > 50) & (az < 130)], 1)
    ciemne = L < (Lrow[:, None] - 6)
    zat2 = (az > 26) & (az < 150)
    for x in np.nonzero(zat2)[0]:
        b0 = int(gr[x])
        if b0 < ih and ciemne[b0:ih, x].mean() > .6 and el[b0] > 1.3: continue   # szczyt cypla, stok góry
        i = ih
        while i > 0 and el[i - 1] < 1.3 and ciemne[i - 1, x]: i -= 1
        gr[x] = i
    # mgła na szczytach (gładka, jasna, bez barwy) i jasna obwódka HDR przy ciemnym grzbiecie → niebo
    mgla = (L > 72) & (np.hypot(LAB[..., 1] - 128, LAB[..., 2] - 128) < 9) & (lap < 4)
    for x in range(w2):
        i = int(gr[x])
        while i < ih and mgla[i, x]: i += 1
        k = 0; kmax = 16 if zat2[x] else 4
        while i < ih and k < kmax and L[i, x] > 66: i += 1; k += 1
        gr[x] = i
    gr = cv2.medianBlur(gr.reshape(1, -1).astype(np.float32), 5).reshape(-1)
    eg = el[np.clip(gr.astype(int), 0, nr - 1)]
    print('linia nieba: elewacja min %.2f°, max %.2f°, mediana %.2f°' % (eg.min(), eg.max(), np.median(eg)))
    return gr + r0


def zamien(we, niebo, wy, slonce_az=SLONCE_AZ, nas=NAS):
    P = np.asarray(Image.open(we).convert('RGB')); H, W, _ = P.shape
    gr = linia_nieba(P)
    # alfa nieba 8k: na lądzie granica 3 px w głąb, wtopienie 2 px; nad wodą niebo pełne od horyzontu, wtopienie 0.5°
    yy = np.arange(H)[:, None]; g8 = np.interp(np.arange(W) / 2.0, np.arange(W // 2), gr * 2.0)[None, :]
    az8 = (np.arange(W) + .5) / W * 360
    woda = ((az8 > ZATOKA[0]) & (az8 < ZATOKA[1])).astype(np.float32)
    woda = np.convolve(np.pad(woda, 64, mode='wrap'), np.ones(129) / 129, 'same')[64:-64]
    mi = (2.0 + woda * (.5 / 180 * H - 2.0))[None, :]
    g8 = g8 + woda[None, :] * (.25 / 180 * H) + (1 - woda[None, :]) * 3.0
    A = np.clip((g8 - yy) / mi + .5, 0, 1).astype(np.float32)[..., None]
    # niebo N: słońce zdjęcia (najjaśniejsze piksele górnej półkuli) na azymucie słońca miasta
    N = np.asarray(Image.open(niebo).convert('RGB'))
    if N.shape[1] != W: N = cv2.resize(N, (W, H), interpolation=cv2.INTER_AREA)
    sl = cv2.cvtColor(cv2.resize(N, (2048, 1024), interpolation=cv2.INTER_AREA), cv2.COLOR_RGB2GRAY)[:512]
    ys_, xs_ = np.nonzero(sl >= sl.max() - 4); u_s = float(np.angle(np.exp(2j * np.pi * xs_ / 2048).mean()) / (2 * np.pi)) % 1
    print('słońce zdjęcia nieba: u %.4f, elewacja %.1f° → azymut miasta %.1f°' % (u_s, 90 - ys_.mean() / 1024 * 180, slonce_az))
    N = np.roll(N, int(round((slonce_az / 360.0 - u_s) * W)) % W, axis=1)
    n = lin(N.astype(np.float32) / 255)
    Y = (n @ np.array([.2126, .7152, .0722], np.float32))[..., None]
    nieb = np.clip((n[..., 2] - n[..., 0]) / (Y[..., 0] * .35 + 1e-4), 0, 1)[..., None]     # udział błękitu (B ponad R)
    nieb = nieb * np.clip((90 - (np.arange(H) + .5) / H * 180)[:, None, None] / 15.0, 0, 1)   # przy horyzoncie bez podbicia
    n = np.clip(Y + (n - Y) * (1.0 + (min(nas, 1.15) - 1.0) + (nas - min(nas, 1.15)) * nieb), 0, None) * 1.05
    # ląd zdjęć: nasycenie ×1.2 (pas krajobrazu Osiedla 1.2–1.25)
    p = lin(P.astype(np.float32) / 255)
    Y = (p @ np.array([.2126, .7152, .0722], np.float32))[..., None]
    p = np.clip(Y + (p - Y) * 1.2, 0, None)
    # woda zatoki (spójna z wodą tuż pod horyzontem; molo, łodzie, domy bez zmian): kolor wiersz po wierszu jak PROF
    EL8 = 90 - (np.arange(H) + .5) / H * 180
    w_r0, w_r1 = int((90 + .3) / 180 * H), int((90 + 45.0) / 180 * H)
    blk = p[w_r0:w_r1]; b8 = (np.clip(srgb(blk), 0, 1) * 255).astype(np.uint8)
    lab_ = cv2.cvtColor(b8, cv2.COLOR_RGB2LAB).astype(np.float32)
    gl_ = cv2.blur(np.abs(cv2.Laplacian(cv2.cvtColor(b8, cv2.COLOR_RGB2GRAY).astype(np.float32), cv2.CV_32F, ksize=3)), (7, 7))
    kol_w = (az8 > 24) & (az8 < 172)
    wpx = (gl_ < 70) & (lab_[..., 2] - 128 <= 2) & (lab_[..., 0] >= 100) & kol_w[None, :]
    wpx = cv2.morphologyEx(wpx.astype(np.uint8), cv2.MORPH_CLOSE, np.ones((5, 5), np.uint8))
    _, lab_cc = cv2.connectedComponents(wpx, connectivity=8)
    wm = np.isin(lab_cc, np.unique(lab_cc[:3][lab_cc[:3] > 0])).astype(np.float32)
    bok = np.convolve(np.pad(kol_w.astype(np.float32), 64, mode='wrap'), np.ones(129) / 129, 'same')[64:-64]
    wm = cv2.GaussianBlur(wm, (0, 0), 2.0) * bok[None, :]
    el_b = EL8[w_r0:w_r1]
    T = lin(np.stack([np.interp(-el_b, -PROF[:, 0], PROF[:, c]) for c in (1, 2, 3)], 1) / 255)
    med_r = np.array([np.median(blk[r][wm[r] > .9], 0) if (wm[r] > .9).sum() > 50 else [np.nan] * 3 for r in range(blk.shape[0])])
    ok = ~np.isnan(med_r[:, 0])
    for c in range(3): med_r[~ok, c] = np.interp(np.nonzero(~ok)[0], np.nonzero(ok)[0], med_r[ok, c])
    gain = T / np.maximum(med_r, 1e-4)
    gain = np.stack([np.convolve(np.pad(gain[:, c], 15, mode='edge'), np.ones(31) / 31, 'valid') for c in range(3)], 1)[:, None, :]
    p[w_r0:w_r1] = blk * (1 - wm[..., None]) + blk * gain * wm[..., None]
    print('woda zatoki: pikseli', int((wm > .5).sum()), '| kolor −1°: sRGB', np.round(srgb(T[int(np.argmin(np.abs(el_b + 1.0)))]) * 255))
    out = p * (1 - A) + n * A
    Image.fromarray((np.clip(srgb(out), 0, 1) * 255 + .5).astype(np.uint8)).save(wy, quality=92)
    print('zapisano', wy, W, H)


if __name__ == '__main__':
    zamien(sys.argv[1], sys.argv[2], sys.argv[3], float(sys.argv[4]) if len(sys.argv) > 4 else SLONCE_AZ,
           float(sys.argv[5]) if len(sys.argv) > 5 else NAS)
