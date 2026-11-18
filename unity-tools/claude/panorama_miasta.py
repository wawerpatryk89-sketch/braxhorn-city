#!/usr/bin/env python3
"""MIASTO R1 — PANORAMA MIASTA ZE ZDJĘĆ 360° (właściciel 18.11 11:32–11:39: „panorama… tragedia… nawet nie z Minecrafta”;
„Czy ludzie mają ładną panoramę, na przykład w obozie? … w Osiedlu”; „tam, gdzie jest morze — inna, tam, gdzie nie ma — inna,
profesjonalnie połączone, żeby nie było widać przejścia”; wybór: „numer 1, numer 2”).

Źródła (Poly Haven, CC0 — https://polyhaven.com/license), wersje „tonemapped JPG” 8192×4096:
  A = https://polyhaven.com/a/cannon        — Simon's Town z góry: ląd (góra, miasteczko, wzgórza) i CAŁE niebo
  B = https://polyhaven.com/a/dalkey_view   — morze z dalekim cyplem na horyzoncie (wstawione w sektor morza miasta)
Pobranie: https://dl.polyhaven.org/file/ph-assets/HDRIs/extra/Tonemapped%20JPG/<id>.jpg → eq_cannon.jpg, eq_dalkey_view.jpg.

Wynik: równoodległościowe 8192×4096 w UKŁADZIE MIASTA — kolumna = azymut od północy (+z) ku wschodowi (+x), wiersz = elewacja
(góra obrazu = zenit), horyzont morza obu zdjęć dokładnie na elewacji 0 (zmierzone: A −0.571°, B −0.439°).
Sektor morza miasta z prostokąta i linii brzegu (BraxMiastoGranice) na walcu 2.6 km: azymut 41…134° — A obrócone tak, że jego zatoka
leży dokładnie tam (środek 87.5°), B wstawione tylko w pas horyzontu nad wodą zatoki (48.5…128.5°, przejście 10° po bokach,
+1.1…+2.6° w niebo A, −1.9…−1.1° w wodę A) z kolorem dopasowanym rząd po rzędzie (mediana A / mediana B), więc szew leży na
otwartej wodzie i w powietrzu przy horyzoncie. Retusz A: działo z budką przy fotografie (kąt 6.8…22°, el −0.3…+4.9°) zastąpione
krzakami z tego samego zbocza (−20°). W grze: BraxRegionHoryzont (PanoramaMiasta) + MiastoTlo.cginc (_FotoHoryzont = 1).

Krok 2 — słoneczne niebo zamiast pochmurnego nieba A (18.11 16:04): panorama_miasta_niebo.py, źródło
  N = https://polyhaven.com/a/sunflowers_puresky (CC0) → eq_sunflowers_puresky.jpg w tym samym katalogu.

Użycie: python3 panorama_miasta.py <katalog z eq_cannon.jpg, eq_dalkey_view.jpg [i eq_sunflowers_puresky.jpg]> <wyjście.jpg> [szerokość=8192]
"""
import numpy as np, sys
from PIL import Image
Image.MAX_IMAGE_PIXELS = None

KAT = sys.argv[1] if len(sys.argv) > 1 else '.'
OUT = sys.argv[2] if len(sys.argv) > 2 else 'panorama_miasta_8k.jpg'
W = int(sys.argv[3]) if len(sys.argv) > 3 else 8192
H = W // 2

A_HOR, B_HOR = -0.571, -0.439          # zmierzona elewacja horyzontu morza na zdjęciach (stopnie)
A_OFF = 83.5                            # kąt zdjęcia A = azymut miasta + A_OFF (środek zatoki A → środek morza miasta 87.5°)
B_OFF = -43.5                           # kąt zdjęcia B = azymut miasta + B_OFF (cypel Howth → azymut 53–71°)
SEKTOR = (48.5, 128.5)                  # azymuty miasta, w których jest woda w A (między cyplem a Simon's Town)
BOK = 10.0                              # szerokość przejścia bocznego (stopnie, nad otwartą wodą)
B_GORA = (1.1, 2.6)                     # B pełne do +1.1° (cypel), przejście do nieba A do +2.6°
B_DOL = (-1.9, -1.1)                    # pod horyzontem B tylko pas wody do −1.9° (niżej bliskie wysepki i drzewa fotografa B)


def wczytaj(n):
    return np.asarray(Image.open(n).convert('RGB')).astype(np.float32) / 255.0


def lin(c): return np.where(c <= .04045, c / 12.92, ((c + .055) / 1.055) ** 2.4)
def srgb(c): return np.where(c <= .0031308, c * 12.92, 1.055 * np.power(np.clip(c, 0, None), 1 / 2.4) - .055)


def probka(img, kat, el):
    """Próbkowanie dwuliniowe zdjęcia równoodległościowego: kat (stopnie, dowolne), el (stopnie)."""
    h, w, _ = img.shape
    x = (np.mod(kat, 360.0) / 360.0) * w - .5
    y = (90.0 - el) / 180.0 * h - .5
    x0 = np.floor(x).astype(int); y0 = np.clip(np.floor(y).astype(int), 0, h - 2)
    fx = (x - x0)[..., None]; fy = np.clip(y - y0, 0, 1)[..., None]
    x0m = np.mod(x0, w); x1m = np.mod(x0 + 1, w)
    a = img[y0, x0m]; b = img[y0, x1m]; c = img[y0 + 1, x0m]; d = img[y0 + 1, x1m]
    return (a * (1 - fx) + b * fx) * (1 - fy) + (c * (1 - fx) + d * fx) * fy


def ss(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1); return t * t * (3 - 2 * t)


A = lin(wczytaj(KAT + '/eq_cannon.jpg')); B = lin(wczytaj(KAT + '/eq_dalkey_view.jpg'))
def retusz(img, k0, k1, e0, e1, przes, mięk=1.2):
    # zastąpienie fragmentu zdjęcia (kąty k0..k1, elewacje e0..e1) łatką z tego samego zdjęcia przesuniętą o „przes” stopni
    h, w, _ = img.shape
    ys = np.arange(int((90 - e1 - 2 * mięk) / 180 * h), int((90 - e0 + 2 * mięk) / 180 * h))
    xs = np.arange(int((k0 - 2 * mięk) / 360 * w), int((k1 + 2 * mięk) / 360 * w))
    KK, EE = np.meshgrid(xs / w * 360.0, 90.0 - ys / h * 180.0)
    m = (ss(k0 - mięk, k0, KK) * (1 - ss(k1, k1 + mięk, KK)) * ss(e0 - mięk, e0, EE) * (1 - ss(e1, e1 + mięk, EE)))[..., None]
    lat = probka(img, KK + przes, EE)
    blk = img[np.ix_(ys, np.mod(xs, w))]
    img[np.ix_(ys, np.mod(xs, w))] = blk * (1 - m) + lat * m
retusz(A, 6.8, 22.0, -0.3, 4.9, -20.0, .7)     # działo z budką (fotograf stał przy nim) — z daleka byłoby „budynkiem” na grzbiecie
az = ((np.arange(W) + .5) / W * 360.0).astype(np.float32)
el = (90.0 - (np.arange(H) + .5) / H * 180.0).astype(np.float32)
s0, s1 = SEKTOR
kol = (az >= s0 + BOK) & (az <= s1 - BOK)
# dopasowanie kolorów B do A wiersz po wierszu (elewacja): mnożnik = mediana A / mediana B w sektorze wody, wygładzony
rows = np.nonzero((el >= -2.5) & (el <= 5.0))[0]
mn = np.ones((H, 3), np.float32)
for r in rows:
    a = probka(A, az[kol] + A_OFF, np.full(kol.sum(), el[r] + A_HOR, np.float32))
    b = probka(B, az[kol] + B_OFF, np.full(kol.sum(), el[r] + B_HOR, np.float32))
    mn[r] = np.median(a, 0) / np.maximum(np.median(b, 0), 1e-4)
k = 9
pad = np.pad(mn[rows], ((k // 2, k // 2), (0, 0)), mode='edge')
mn[rows] = np.stack([np.convolve(pad[:, c], np.ones(k) / k, 'valid') for c in range(3)], 1)
print('mnożnik koloru B: horyzont', mn[int(H / 2)].round(3), 'woda −1°', mn[int((90 + 1.0) / 180 * H)].round(3))
wynik = np.zeros((H, W, 3), np.uint8)
for y0 in range(0, H, 256):
    y1 = min(H, y0 + 256)
    AZ, EL = np.meshgrid(az, el[y0:y1])
    out = probka(A, AZ + A_OFF, EL + A_HOR).astype(np.float32)
    lat = ss(s0, s0 + BOK, AZ) * (1 - ss(s1 - BOK, s1, AZ))
    pion = (1 - ss(B_GORA[0], B_GORA[1], EL)) * ss(B_DOL[0], B_DOL[1], EL)
    wB = (lat * pion)[..., None].astype(np.float32)
    m = wB[..., 0] > 1e-4
    if m.any():
        b = probka(B, AZ[m] + B_OFF, EL[m] + B_HOR) * mn[y0 + np.nonzero(m)[0]]
        out[m] = out[m] * (1 - wB[m]) + np.clip(b, 0, None) * wB[m]
    wynik[y0:y1] = (np.clip(srgb(out), 0, 1) * 255 + .5).astype(np.uint8)
Image.fromarray(wynik).save(OUT, quality=92)
print('zapisano', OUT, W, H)

# Krok 2 (właściciel 18.11 16:04 „kolorystyka jak w osiedlu… zobacz, jaka jest panorama”, 16:2x „żeby było dobrze, a inaczej
# jak na osiedlu”): niebo „cannon” jest pochmurne — słoneczne niebo z cumulusami (Poly Haven sunflowers_puresky, CC0) nad
# lądem i morzem zdjęć 1 i 2, woda zatoki w kolorze wody przy słońcu (panorama_miasta_niebo.py). Bez pliku nieba — tylko krok 1.
import os
if os.path.exists(KAT + '/eq_sunflowers_puresky.jpg'):
    del A, B, wynik, out, AZ, EL, lat, pion, wB, m                    # pamięć dla kroku 2 (8K × float32)
    sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
    import panorama_miasta_niebo
    panorama_miasta_niebo.zamien(OUT, KAT + '/eq_sunflowers_puresky.jpg', OUT)
