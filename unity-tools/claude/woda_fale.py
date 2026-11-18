#!/usr/bin/env python3
"""MIASTO R1 — tekstury morza (właściciel 18.11 12:28–12:36: „nie możemy zrobić bardziej realistycznej wody?”,
„woda… nie pasek… widać nic”). Fizyczne widmo fal wiatrowych (Phillips, Tessendorf „Simulating Ocean Water” 2001) — pole
wysokości z odwrotnej transformaty Fouriera na kafelku L × L m, więc każdy kafel jest bezszwowy.

  fale      → mapa normalnych drobnej fali (różnice centralne z zawinięciem). Dwa takie kafle (inna skala i kierunek)
              przesuwane w shaderze dają falę bez powtarzalnego wzoru.
  kaustyki  → kaustyki na dnie: promienie słońca załamane na powierzchni z tego samego widma (prawo Snella, n = 1.33)
              i zebrane na płaszczyźnie dna na głębokości D (rozkład jasności światła, średnia = 1).

Układ obu tekstur: wiersz od DOŁU obrazu = z rosnące, kolumna = x (jak UV w Unity i w three.js: v = z / L).
Normalne: R = x, G = z, B = y (pion) — 0.5 + 0.5·n. Kaustyki: szarość = sqrt(jasność / 4) (jasność = szarość² · 4).
Wyjście deterministyczne (stałe ziarno).
Użycie: python3 woda_fale.py fale <wyjście.png> [L=10] [wiatr m/s=7] [ziarno=18]
        python3 woda_fale.py kaustyki <wyjście.png> [L=3] [wiatr m/s=3.5] [ziarno=5] [głębokość D m=1.2]
"""
import sys
import numpy as np
from PIL import Image

g = 9.81


def pole_wysokosci(N, L, V, seed, wiatr=(0.86, 0.51), lmin=0.0):
    rng = np.random.default_rng(seed)
    k1 = 2 * np.pi * np.fft.fftfreq(N, d=L / N)
    KX, KZ = np.meshgrid(k1, k1)                       # wiersz = z, kolumna = x
    K = np.hypot(KX, KZ); K[0, 0] = 1e-6
    Lw = V * V / g                                     # największa fala dla wiatru V
    w = np.array(wiatr)
    cosk = (KX * w[0] + KZ * w[1]) / K
    P = np.exp(-1.0 / (K * Lw) ** 2) / K ** 4 * cosk ** 2
    P *= np.exp(-(K * L / N * 1.2) ** 2)               # bez fal krótszych niż ~piksel
    P *= np.exp(-(K * lmin) ** 2)                      # tłumienie najkrótszych zmarszczek (Tessendorf: exp(−k²l²))
    P[cosk < 0] *= 0.07                                # fale idą z wiatrem
    P[0, 0] = 0
    h0 = (rng.normal(size=(N, N)) + 1j * rng.normal(size=(N, N))) * np.sqrt(P / 2)
    return np.real(np.fft.ifft2(h0))


def nachylenia(h, L):
    N = h.shape[0]; px = L / N
    dx = (np.roll(h, -1, 1) - np.roll(h, 1, 1)) / (2 * px)
    dz = (np.roll(h, -1, 0) - np.roll(h, 1, 0)) / (2 * px)
    return dx, dz


def zapisz(arr01, out):
    # wiersz 0 tablicy = z 0 → na DOLE obrazu
    a = np.flipud(np.clip(arr01, 0, 1))
    Image.fromarray((a * 255 + .5).astype(np.uint8)).save(out, optimize=True)


def fale(out, L=10.0, V=7.0, seed=18):
    N = 512
    h = pole_wysokosci(N, L, V, seed)
    h *= 0.12 / (h.std() + 1e-12)                      # odchylenie wysokości drobnej fali ~12 cm na kaflu
    dx, dz = nachylenia(h, L)
    n = np.stack([-dx, np.ones_like(h), -dz], -1)
    n /= np.linalg.norm(n, axis=-1, keepdims=True)
    zapisz(np.stack([n[..., 0], n[..., 2], n[..., 1]], -1) * 0.5 + 0.5, out)
    print("fale:", out, N, "px, kafel", L, "m, wiatr", V, "m/s, nachylenie średnie",
          round(float(np.degrees(np.arctan(np.hypot(dx, dz))).mean()), 2), "st")


def kaustyki(out, L=3.0, V=3.5, seed=5, D=1.2):
    N, M = 512, 1024                                   # tekstura N², promienie z siatki M² (4 na piksel)
    h = pole_wysokosci(M, L, V, seed, wiatr=(0.6, 0.8), lmin=0.035)
    h *= 0.02 / (h.std() + 1e-12)                      # zmarszczki ~2 cm
    dx, dz = nachylenia(h, L)
    n = np.stack([-dx, np.ones_like(h), -dz], -1)
    n /= np.linalg.norm(n, axis=-1, keepdims=True)
    # promień z góry (0, −1, 0) załamany do wody: t = η·i + (η·c − sqrt(1 − η²(1 − c²)))·n, c = −n·i = n.y
    eta = 1 / 1.33
    c = n[..., 1]
    k = 1 - eta * eta * (1 - c * c)
    t = eta * np.array([0, -1, 0]) + (eta * c - np.sqrt(k))[..., None] * n
    s = D / -t[..., 1]
    jj, ii = np.meshgrid(np.arange(M), np.arange(M))   # jj = kolumna (x), ii = wiersz (z)
    x = (jj + .5) * L / M + t[..., 0] * s
    z = (ii + .5) * L / M + t[..., 2] * s
    # rozrzut dwuliniowy na siatkę N² z zawinięciem (kafel bezszwowy)
    fx = x / L * N - .5; fz = z / L * N - .5
    x0 = np.floor(fx).astype(int); z0 = np.floor(fz).astype(int); ax = fx - x0; az = fz - z0
    I = np.zeros((N, N))
    for ox, wx in ((0, 1 - ax), (1, ax)):
        for oz, wz in ((0, 1 - az), (1, az)):
            np.add.at(I, ((z0 + oz) % N, (x0 + ox) % N), wx * wz)
    # lekkie rozmycie (σ = 0.8 px): bez szumu rozrzutu, linie zostają ostre
    f = np.fft.fftfreq(N)
    FX, FZ = np.meshgrid(f, f)
    I = np.real(np.fft.ifft2(np.fft.fft2(I) * np.exp(-2 * (np.pi * 0.8) ** 2 * (FX ** 2 + FZ ** 2))))
    I /= I.mean()
    zapisz(np.sqrt(np.clip(I / 4, 0, 1)), out)
    q = np.sqrt(np.clip(I / 4, 0, 1)) ** 2 * 4
    # średnia z min(dwóch próbek) jak w shaderze (druga próbka: skala ×1.37, przesunięcie) — do normalizacji
    yy, xx = np.meshgrid(np.arange(N), np.arange(N), indexing='ij')
    q2 = q[((yy * 1.37 + 0.57 * N).astype(int)) % N, ((xx * 1.37 + 0.31 * N).astype(int)) % N]
    print("kaustyki:", out, N, "px, kafel", L, "m, wiatr", V, "m/s, dno", D, "m, jasność max", round(float(I.max()), 2),
          "p99", round(float(np.percentile(I, 99)), 2), "średnia min(2 próbek)", round(float(np.minimum(q, q2).mean()), 3))


if __name__ == "__main__":
    tryb, out = sys.argv[1], sys.argv[2]
    a = [float(x) for x in sys.argv[3:]]
    if tryb == "fale":
        fale(out, *(a[:2]), *([int(a[2])] if len(a) > 2 else []))
    elif tryb == "kaustyki":
        kaustyki(out, *(a[:2]), *([int(a[2])] if len(a) > 2 else []), *(a[3:4]))
    else:
        raise SystemExit("tryb: fale | kaustyki")
