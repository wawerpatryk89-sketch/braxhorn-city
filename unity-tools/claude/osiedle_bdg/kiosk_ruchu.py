"""MIASTO R1 — stary kiosk Ruchu (24.11, 17:29 właściciel: „klocz [kiosk] ruchu polski stary — minimum 2 na mieście musisz
zrobić”, wzór IMG_2056; „normalnie z napisem kiosk ruch”).

Wzór: budka ok. 2,4 × 1,8 m, ściany do 2,3 m, zielony lakier, kremowe narożniki i pas pod szyldem; od frontu dolna płyta
do lady (1,0 m), nad nią wystawa w aluminiowej ramie (gazety, czasopisma, drobiazgi), okienko na środku z metalową półką
na wspornikach, nad wystawą podnoszony daszek z blachy na dwóch zastrzałach; nad ścianami pas szyldu rozchylony na
zewnątrz (dach wysunięty 24 cm) z tablicą „KIOSK RUCH” i lampą; dach płaski z niskim spadkiem; boki i tył z pionowych
przetłoczeń; z prawej drzwi z okienkiem, gablota z ogłoszeniami i tabliczka telefonu. Tytuły gazet i ogłoszenia
wymyślone (bez prawdziwych tytułów), tabliczka telefonu bez nazwy operatora, bez logo jaskółki — sam napis.
Lampa szyldu = materiał „klosz” (nocą świeci — BraxLampyNoc).

Układ: o — środek podstawy na poziomie chodnika, t — wzdłuż frontu, n — front (w stronę kupujących). Wszystko
wzdłuż frontu liczone „w prawo dla patrzącego na front” (Unity: x wschód, y góra, z północ — układ lewoskrętny).
Zasada anty-migotania: żadnych dwóch powierzchni w jednej płaszczyźnie (naklejki w miejscu pominiętej ściany pudła,
listwy bez ściany przylegającej, ≥ 1 cm odstępu między elementami w tej samej stronie).

naklejki(katalog) rysuje kp_wystawa.png, kp_szyld.png, kp_ogloszenia.png, kp_telefon.png (nazwa pliku = nazwa
materiału — tak czyta je BraxBydgoszcz).
"""
import math
import random
import sys
from pathlib import Path

import numpy as np

from kit import UP, nrm

W, D, HW, HS, NAD = 2.4, 1.8, 2.3, 2.72, .24      # szerokość, głębokość, ściany, góra szyldu, wysunięcie szyldu i dachu
UV1 = [(0, 0), (1, 0), (1, 1), (0, 1)]
LAD, WD0, WD1 = 1.0, 1.04, 2.1                     # lada (góra dolnej płyty), wystawa od–do
ZAW, DASZ, DASZ_H = 2.15, .62, 2.3                 # zawias daszka, wysięg, wysokość końca
SZYLD = (2.3, .34)                                  # tablica na pasie szyldu: szerokość, wysokość wzdłuż skosu
SZYLD_BOK = (1.75, .34 * 1.75 / 2.3)                # na bokach: ta sama naklejka, te same proporcje
DRZWI = (-.78, -.06)                               # prawy bok (wzdłuż n): drzwi, gablota, tabliczka telefonu
GABLOTA = (.1, .6, .98, 1.78)
TELEFON = (.17, .53, 1.86, 2.16)
MATERIALY = ("kp_zielony", "kp_krem", "kp_alu", "kp_cokol", "kp_drzwi", "kp_bialy", "kp_wystawa", "kp_szyld",
             "kp_ogloszenia", "kp_telefon", "witraz", "klosz")


def obrys(o, t, n, zapas=0.0, przod=0.0):
    """Prostokąt (x, z) zajęty przez kiosk z dachem (+ zapas); przod = wolne miejsce przed frontem dla kupujących."""
    from shapely.geometry import Polygon
    t = nrm(t); n = nrm(n); o = np.asarray(o, float)
    a, b0 = W / 2 + NAD + zapas, D / 2 + NAD + zapas
    b1 = max(b0, D / 2 + przod + zapas)
    return Polygon([tuple((o + t * u + n * w)[[0, 2]]) for u, w in ((-a, -b0), (a, -b0), (a, b1), (-a, b1))])


def miejsce(srodek, R, linie, tryb, dozwolone, zakaz, y=.12, krok=.5, przod=1.6, zapas=.3, odl_min=0.0):
    """Najbliższe `srodek` miejsce na kiosk: obrys z dachem i wolnym polem przed frontem w całości w `dozwolone`
    (przygotowana geometria), bez części wspólnej z `zakaz`. Front od najbliższej linii (tryb „od” — tyłem do jezdni)
    albo do niej („do” — przodem do osi deptaka). Zwraca (o, t, n) albo None."""
    from shapely.geometry import Point
    ax, az = srodek; k = int(R / krok)
    pts = [(ax + i * krok, az + j * krok) for i in range(-k, k + 1) for j in range(-k, k + 1)]
    pts = sorted((p for p in pts if odl_min <= math.hypot(p[0] - ax, p[1] - az) <= R), key=lambda p: math.hypot(p[0] - ax, p[1] - az))
    for (x, z) in pts:
        P = Point(x, z)
        if not dozwolone.contains(P): continue
        L = min(linie, key=lambda l: l.distance(P))
        q = L.interpolate(L.project(P)); v = np.array([x - q.x, 0., z - q.y]); dl = float(np.linalg.norm(v))
        if dl < .3: continue
        n = v / dl * (1.0 if tryb == "od" else -1.0); t = np.array([-n[2], 0., n[0]]); o = np.array([x, y, z])
        F = obrys(o, t, n, zapas, przod)
        if dozwolone.contains(F) and not zakaz.intersects(F): return o, t, n
    return None


def kiosk(S, o, t, n, g="kioski"):
    t = nrm(t); n = nrm(n); o = np.asarray(o, float)
    tr = t if np.dot(np.cross(UP, -n), t) > 0 else -t          # w prawo dla patrzącego na front
    Q = lambda u, v, w: o + tr * u + UP * v + n * w
    w2, d2 = W / 2, D / 2; ww = d2 - .05                          # wystawa cofnięta 5 cm
    zi, kr, al = "kp_zielony", "kp_krem", "kp_alu"

    def F(u0, u1, v0, v1, m):                                     # płyta frontu
        S.quad(g, m, Q(u0, v0, d2), Q(u1, v0, d2), Q(u1, v1, d2), Q(u0, v1, d2), kier=n)

    # cokół 16 cm (1 cm poza obrysem) i bryła ścian (front składany osobno, górę zamyka pas pod szyldem)
    S.box(g, "kp_cokol", Q(-w2 - .01, -.02, -d2 - .01), tr, UP, n, W + .02, .16, D + .02, bez=("-y",))
    S.box(g, zi, Q(-w2, .12, -d2), tr, UP, n, W, HW - .12, D, bez=("-y", "+y", "+z"))
    F(-w2, w2, .12, LAD, zi); F(-w2, -w2 + .1, LAD, 2.14, zi); F(w2 - .1, w2, LAD, 2.14, zi)
    F(-w2 + .1, w2 - .1, LAD, WD0, zi); F(-w2 + .1, w2 - .1, WD1, 2.14, zi); F(-w2, w2, 2.14, HW, zi)
    # wystawa (naklejka: półki z gazetami i czasopismami, okienko) i ościeża wnęki
    S.quad(g, "kp_wystawa", Q(-w2 + .1, WD0, ww), Q(w2 - .1, WD0, ww), Q(w2 - .1, WD1, ww), Q(-w2 + .1, WD1, ww), kier=n, uv1=UV1)
    S.quad(g, zi, Q(-w2 + .1, WD0, ww), Q(-w2 + .1, WD0, d2), Q(-w2 + .1, WD1, d2), Q(-w2 + .1, WD1, ww), kier=tr)
    S.quad(g, zi, Q(w2 - .1, WD0, ww), Q(w2 - .1, WD0, d2), Q(w2 - .1, WD1, d2), Q(w2 - .1, WD1, ww), kier=-tr)
    S.quad(g, zi, Q(-w2 + .1, WD1, ww), Q(w2 - .1, WD1, ww), Q(w2 - .1, WD1, d2), Q(-w2 + .1, WD1, d2), kier=-UP)
    S.quad(g, al, Q(-w2 + .1, WD0, ww), Q(w2 - .1, WD0, ww), Q(w2 - .1, WD0, d2), Q(-w2 + .1, WD0, d2), kier=UP)
    # aluminiowa rama wystawy (przechodzi przez płaszczyznę naklejki, czoło 2 cm za licem): obwód, dwa słupki okienka,
    # górna krawędź okienka; ściany przy ościeżach pominięte
    hr = WD1 - WD0 - .08
    for (u0, v0, du, dv, bez) in ((-w2 + .1, WD0, W - .2, .04, ("-y", "-x", "+x")), (-w2 + .1, WD1 - .04, W - .2, .04, ("+y", "-x", "+x")),
                                  (-w2 + .1, WD0 + .04, .04, hr, ("-x", "-y", "+y")), (w2 - .14, WD0 + .04, .04, hr, ("+x", "-y", "+y")),
                                  (-.38, WD0 + .04, .04, hr, ("-y", "+y")), (.34, WD0 + .04, .04, hr, ("-y", "+y")),
                                  (-.34, 1.36, .68, .035, ("-x", "+x"))):
        S.box(g, al, Q(u0, v0, ww - .005), tr, UP, n, du, dv, .035, bez=bez)
    S.box(g, al, Q(-w2 - .02, .98, d2 - .02), tr, UP, n, W + .04, .04, .14)                    # parapet lady
    S.box(g, al, Q(-.32, 1.035, d2 - .04), tr, UP, n, .64, .025, .32)                          # półka okienka
    for s_ in (-1, 1):                                                                         # wsporniki półki
        A = Q(s_ * .26, .84, d2 + .002); B_ = Q(s_ * .26, 1.035, d2 + .24); dl = float(np.linalg.norm(B_ - A)); tt = (B_ - A) / dl
        S.box(g, al, A - tr * .012, tt, nrm(np.cross(tr, tt)), tr, dl, .02, .024)
    # kremowe narożniki (8 × 8 cm, 1,5 cm poza lico) i pas pod szyldem (zamyka górę bryły)
    for su in (-1, 1):
        for sw in (-1, 1):
            S.box(g, kr, Q(-w2 - .015 if su < 0 else w2 - .065, .14, -d2 - .015 if sw < 0 else d2 - .065), tr, UP, n, .08, HW - .06 - .14, .08, bez=("-y",))
    S.box(g, kr, Q(-w2 - .015, HW - .06, -d2 - .015), tr, UP, n, W + .03, .06, D + .03, bez=("-y",))
    # boki i tył: pionowe przetłoczenia co 12 cm (2 × 1,5 cm), bez ściany przy licu; na prawym boku omijają drzwi i gablotę
    sciany = ((Q(-w2, 0, -d2), n, D, -tr, False), (Q(w2, 0, -d2), n, D, tr, True), (Q(-w2, 0, -d2), tr, W, -n, False))
    for (a0, wzdl, dl_, na_zewn, prawy) in sciany:
        for s_ in np.arange(.16, dl_ - .12, .12):
            if prawy:
                w_ = s_ - d2
                if DRZWI[0] - .04 < w_ < DRZWI[1] + .04 or GABLOTA[0] - .04 < w_ < GABLOTA[1] + .04: continue
            S.box(g, zi, a0 + wzdl * (s_ - .01) + UP * .2, wzdl, UP, na_zewn, .02, HW - .32, .015, bez=("-z",))
    # prawy bok: drzwi z okienkiem i klamką, gablota z ogłoszeniami (tło, rama), tabliczka telefonu
    P = lambda w, v, dd=0.0: o + tr * (w2 + dd) + UP * v + n * w
    S.box(g, "kp_drzwi", P(DRZWI[0], .14), n, UP, tr, DRZWI[1] - DRZWI[0], 1.95, .03, bez=("-z", "-y"))
    wc = (DRZWI[0] + DRZWI[1]) / 2
    S.quad(g, "witraz", P(wc - .2, 1.3, .04), P(wc + .2, 1.3, .04), P(wc + .2, 1.86, .04), P(wc - .2, 1.86, .04), kier=tr)
    S.box(g, al, P(DRZWI[1] - .18, 1.0, .03), n, UP, tr, .12, .025, .045)
    prawo_b = nrm(np.cross(UP, -tr))                                    # w prawo dla patrzącego na prawy bok

    def tabl(m, w0, w1, v0, v1, dd):
        a, b = (P(w0, v0, dd), P(w1, v0, dd)) if np.dot(prawo_b, n) > 0 else (P(w1, v0, dd), P(w0, v0, dd))
        S.quad(g, m, a, b, b + UP * (v1 - v0), a + UP * (v1 - v0), kier=tr, uv1=UV1)
    g0, g1, gv0, gv1 = GABLOTA
    S.box(g, al, P(g0, gv0), n, UP, tr, g1 - g0, gv1 - gv0, .02, bez=("-z", "+z"))
    tabl("kp_ogloszenia", g0, g1, gv0, gv1, .02)
    for (w0, v0, dw, dv) in ((g0, gv0, .03, gv1 - gv0), (g1 - .03, gv0, .03, gv1 - gv0), (g0 + .03, gv0, g1 - g0 - .06, .03), (g0 + .03, gv1 - .03, g1 - g0 - .06, .03)):
        S.box(g, al, P(w0, v0, .02), n, UP, tr, dw, dv, .025, bez=("-z",))
    t0, t1, tv0, tv1 = TELEFON
    S.box(g, "kp_bialy", P(t0, tv0), n, UP, tr, t1 - t0, tv1 - tv0, .025, bez=("-z", "+z"))
    tabl("kp_telefon", t0, t1, tv0, tv1, .025)
    # pas szyldu rozchylony na zewnątrz: od ścian (2,3 m) do 2,72 m, wysunięty 24 cm — cztery skośne płyty
    a, b = w2 + .01, d2 + .01; hs = HS - HW
    dol = [Q(-a, HW, -b), Q(a, HW, -b), Q(a, HW, b), Q(-a, HW, b)]
    gor = [Q(-a - NAD, HS, -b - NAD), Q(a + NAD, HS, -b - NAD), Q(a + NAD, HS, b + NAD), Q(-a - NAD, HS, b + NAD)]
    for k in range(4):
        k2 = (k + 1) % 4
        kk = (dol[k] + dol[k2]) / 2 - o; kk[1] = 0; kk = nrm(kk)
        S.quad(g, zi, dol[k], dol[k2], gor[k2], gor[k], kier=nrm(kk * hs - UP * NAD))
    # tablice „KIOSK RUCH” na wszystkich czterech skosach (z przodu i z tyłu 2,3 m, na bokach 1,75 m — ta sama naklejka,
    # te same proporcje); 2 cm przed płytą, naklejka w miejscu pominiętego czoła pudła
    dlug = math.hypot(NAD, hs)
    for (kk, prawo, odl, sz) in ((n, tr, b, SZYLD), (-n, -tr, b, SZYLD), (tr, -n, a, SZYLD_BOK), (-tr, n, a, SZYLD_BOK)):
        sl = nrm(kk * NAD + UP * hs); nn = nrm(kk * hs - UP * NAD); s0 = (dlug - sz[1]) / 2
        A = o + UP * HW + kk * odl + sl * s0 - prawo * sz[0] / 2
        S.box(g, kr, A, prawo, sl, nn, sz[0], sz[1], .02, bez=("-z", "+z"))
        A = A + nn * .02
        S.quad(g, "kp_szyld", A, A + prawo * sz[0], A + prawo * sz[0] + sl * sz[1], A + sl * sz[1], kier=nn, uv1=UV1)
    # dach: płyta 5 cm (1 cm poza szyld) i niski spadek na cztery strony (12 cm)
    a1, b1 = a + NAD + .01, b + NAD + .01; y1 = HS + .045
    S.box(g, zi, Q(-a1, HS - .005, -b1), tr, UP, n, 2 * a1, .05, 2 * b1)
    r0 = [Q(-a1 + .02, y1, -b1 + .02), Q(a1 - .02, y1, -b1 + .02), Q(a1 - .02, y1, b1 - .02), Q(-a1 + .02, y1, b1 - .02)]
    r1 = [Q(-a1 + .62, y1 + .12, -b1 + .62), Q(a1 - .62, y1 + .12, -b1 + .62), Q(a1 - .62, y1 + .12, b1 - .62), Q(-a1 + .62, y1 + .12, b1 - .62)]
    for k in range(4):
        k2 = (k + 1) % 4
        S.quad(g, zi, r0[k], r0[k2], r1[k2], r1[k], kier=UP)
    S.quad(g, zi, r1[0], r1[1], r1[2], r1[3], kier=UP)
    # lampa nad szyldem z prawej (ramię, klosz od spodu — nocą świeci)
    ul, wl = a + NAD - .2, b + NAD
    S.box(g, al, Q(ul - .01, HS - .045, wl - .03), tr, UP, n, .02, .02, .3)
    S.box(g, al, Q(ul - .1, HS - .1, wl + .2), tr, UP, n, .2, .07, .14, bez=("-y",))
    S.quad(g, "klosz", Q(ul - .1, HS - .1, wl + .2), Q(ul + .1, HS - .1, wl + .2), Q(ul + .1, HS - .1, wl + .34), Q(ul - .1, HS - .1, wl + .34), kier=-UP)
    # daszek z blachy nad wystawą: listwa zawiasu, płyta podniesiona o 15 cm na 62 cm wysięgu, rant, żeberka, zastrzały
    S.box(g, al, Q(-w2, ZAW - .03, d2), tr, UP, n, W, .035, .03, bez=("-z",))
    A0, A1 = Q(-w2 - .02, ZAW, d2 + .02), Q(w2 + .02, ZAW, d2 + .02)
    B0, B1 = Q(-w2 - .02, DASZ_H, d2 + DASZ), Q(w2 + .02, DASZ_H, d2 + DASZ)
    kd = nrm(B0 - A0); ny = nrm(np.cross(tr, kd)); ny = ny if ny[1] > 0 else -ny
    S.quad(g, al, A0, A1, B1, B0, kier=ny)
    S.quad(g, al, A0 - ny * .006, A1 - ny * .006, B1 - ny * .006, B0 - ny * .006, kier=-ny)
    S.box(g, al, B0 - UP * .05 - n * .003, tr, UP, n, W + .04, .05, .006, bez=("+y",))
    dl = float(np.linalg.norm(B0 - A0))
    for u in np.linspace(-w2 + .25, w2 - .25, 5):
        S.box(g, al, A0 + tr * (u + w2 + .02 - .015), tr, ny, kd, .03, .02, dl - .02, bez=("-y",))
    for s_ in (-1, 1):
        A = Q(s_ * (w2 - .04), 1.75, d2 + .016)
        B_ = A0 + (B0 - A0) * .82 + tr * (s_ * (w2 - .04) + w2 + .02) - ny * .012
        dl2 = float(np.linalg.norm(B_ - A)); tt = (B_ - A) / dl2
        S.box(g, al, A - tr * .008, tt, nrm(np.cross(tr, tt)), tr, dl2, .015, .016)


# ---------------- naklejki ----------------
FONT_B = "/usr/share/fonts/truetype/dejavu/DejaVuSansCondensed-Bold.ttf"
FONT_S = "/usr/share/fonts/truetype/dejavu/DejaVuSerif-Bold.ttf"
FONT_R = "/usr/share/fonts/truetype/dejavu/DejaVuSansCondensed.ttf"
GAZETY = ["WIEŚCI ZNAD BRDY", "KURIER RYNKOWY", "GŁOS CHOMĘTOWA", "GAZETA SPICHRZOWA", "DZIENNIK MOSTOWY", "WIECZÓR NAD BRDĄ",
          "TYGODNIK GRODZKI", "SPORT DNIA"]
PISMA = ["KRZYŻÓWKI", "PROGRAM TV", "MOTOR I SZOSA", "ŚWIAT ZAGADEK", "OGRÓD I DOM", "WĘDKARZ", "KUCHNIA DOMOWA", "HOBBY",
         "PANI DOMU", "ZDROWIE", "AUTO KLASYK", "PIŁKA"]


def _font(p, s):
    from PIL import ImageFont
    return ImageFont.truetype(p, max(6, int(s)))


def _tekst_w(d, xy, txt, f, kol, w_max):
    """Tekst ściśnięty do szerokości w_max (jak winieta gazety)."""
    from PIL import Image, ImageDraw
    w = d.textlength(txt, font=f)
    if w <= w_max: d.text(xy, txt, font=f, fill=kol); return
    asc, desc = f.getmetrics(); im = Image.new("RGBA", (int(w) + 2, asc + desc + 2), (0, 0, 0, 0))
    ImageDraw.Draw(im).text((0, 0), txt, font=f, fill=kol)
    im = im.resize((max(1, int(w_max)), im.height), Image.LANCZOS); d.obraz.paste(im, (int(xy[0]), int(xy[1])), im)


def _gazeta(d, R, x, y, w, h, tytul):
    papier = R.choice([(236, 232, 218), (230, 226, 210), (240, 238, 228)])
    d.rectangle([x, y, x + w, y + h], fill=papier, outline=(150, 146, 136))
    kol = R.choice([(170, 28, 34), (24, 40, 92), (20, 20, 22), (14, 84, 60)])
    hb = int(h * .16)
    if R.random() < .5:
        d.rectangle([x + 3, y + 3, x + w - 3, y + 3 + hb], fill=kol); _tekst_w(d, (x + 6, y + 4), tytul, _font(FONT_S, hb * .78), (250, 248, 240), w - 12)
    else:
        _tekst_w(d, (x + 5, y + 3), tytul, _font(FONT_S, hb * .82), kol, w - 10)
        d.line([x + 4, y + hb + 5, x + w - 4, y + hb + 5], fill=(60, 60, 60), width=1)
    yy = y + hb + 9
    d.rectangle([x + 5, yy, x + w - 5, yy + int(h * .1)], fill=(36, 34, 32))           # tytuł artykułu (gruba czcionka)
    yy += int(h * .14); fw = int(w * .48); ph = int(h * .3)
    d.rectangle([x + 5, yy, x + 5 + fw, yy + ph], fill=tuple(R.randint(70, 150) for _ in range(3)))   # zdjęcie
    for k in range(int(ph / 6)):
        d.line([x + fw + 10, yy + 3 + k * 6, x + w - 6, yy + 3 + k * 6], fill=(120, 118, 112), width=2)
    yy += ph + 6
    while yy < y + h - 8:
        d.line([x + 5, yy, x + w - 6, yy], fill=(128, 126, 120), width=2); yy += 6


def _pismo(d, R, x, y, w, h, tytul):
    tlo = R.choice([(200, 36, 40), (30, 90, 170), (240, 200, 40), (36, 140, 90), (236, 236, 236), (120, 40, 120), (230, 120, 30), (20, 20, 24)])
    d.rectangle([x, y, x + w, y + h], fill=tlo, outline=(40, 40, 40))
    kt = (20, 20, 24) if sum(tlo) > 450 else (250, 248, 240)
    c = tuple(min(255, max(0, v + R.randint(-90, 90))) for v in tlo)
    d.ellipse([x + w * .15, y + h * .3, x + w * .9, y + h * .95], fill=c)              # „zdjęcie” okładki
    for k in range(3):
        yy = y + h * (.32 + k * .16)
        d.rectangle([x + 4, yy, x + 4 + w * R.uniform(.25, .45), yy + h * .07], fill=R.choice([(250, 230, 40), (250, 250, 250), (220, 30, 30)]))
    _tekst_w(d, (x + 4, y + 3), tytul, _font(FONT_B, h * .14), kt, w - 8)


def wystawa(out, px_m=500):
    from PIL import Image, ImageDraw, ImageFilter
    Wm, Hm = W - .2, WD1 - WD0
    Wp, Hp = int(Wm * px_m), int(Hm * px_m)
    R = random.Random(2056)
    im = Image.new("RGB", (Wp, Hp), (58, 54, 48)); d = ImageDraw.Draw(im); d.obraz = im
    X = lambda u: int((u + Wm / 2) * px_m)              # u od środka (m)
    Y = lambda v: int(Hp - v * px_m)                    # v od dołu wystawy (m)
    for k in range(Hp):                                 # wnętrze: jaśniej u góry (lampa w kiosku)
        c = int(78 - 30 * k / Hp); d.line([0, k, Wp, k], fill=(c, c - 4, c - 10))
    for (u0, u1) in ((-Wm / 2 + .04, -.38), (.38, Wm / 2 - .04)):   # boczne pola: trzy półki z gazetami i czasopismami
        for (v0, v1, rodz) in ((.04, .36, "gaz"), (.38, .70, "pis"), (.72, 1.02, "gaz")):
            d.rectangle([X(u0), Y(v0) - 6, X(u1), Y(v0)], fill=(176, 172, 160))          # półka
            x = X(u0) + R.randint(2, 10); y1 = Y(v0) - 6
            while x < X(u1) - 30:
                w = int(R.uniform(.13, .19) * px_m) if rodz == "gaz" else int(R.uniform(.1, .14) * px_m)
                h = int((v1 - v0 - .02) * px_m * R.uniform(.86, 1.0))
                w = min(w, X(u1) - x - 2)
                if w < 30: break
                (_gazeta if rodz == "gaz" else _pismo)(d, R, x, y1 - h, w, h, R.choice(GAZETY if rodz == "gaz" else PISMA))
                x += int(w * R.uniform(.72, .9)) if rodz == "pis" else w + R.randint(2, 6)
    # środek nad okienkiem: tablica „BILETY”, pod nią rząd krzyżówek
    u0, u1 = -.34, .34
    d.rectangle([X(u0), Y(.36) - 5, X(u1), Y(.36)], fill=(176, 172, 160))
    x = X(u0) + 4
    while x < X(u1) - 40:
        w = int(.11 * px_m); _pismo(d, R, x, Y(.36) - int(.26 * px_m), w, int(.25 * px_m), R.choice(PISMA)); x += int(w * .8)
    d.rectangle([X(-.3), Y(.98), X(.3), Y(.68)], fill=(246, 244, 236), outline=(30, 30, 30), width=3)
    f = _font(FONT_B, .1 * px_m); txt = "BILETY"; tw = d.textlength(txt, font=f)
    d.text((X(0) - tw / 2, Y(.95)), txt, font=f, fill=(170, 26, 30))
    f2 = _font(FONT_R, .045 * px_m); txt = "PRASA · ZNACZKI · KARTY"; tw = d.textlength(txt, font=f2)
    d.text((X(0) - tw / 2, Y(.79)), txt, font=f2, fill=(30, 30, 30))
    # okienko (dół środkowego pola, pod poprzeczką 0,32 m): ciemniejsze wnętrze, lada, drobiazgi
    d.rectangle([X(u0), Y(.32), X(u1), Y(0)], fill=(34, 32, 30))
    d.rectangle([X(u0), Y(.08), X(u1), Y(0)], fill=(120, 114, 104))
    for k in range(9):                                  # zapalniczki i gumy na ladzie
        x = X(u0) + 14 + k * 34; c = R.choice([(220, 40, 40), (40, 120, 220), (240, 200, 30), (40, 170, 80), (240, 240, 240)])
        d.rectangle([x, Y(.16), x + 20, Y(.08)], fill=c)
    im = im.filter(ImageFilter.GaussianBlur(.6))
    # szyba: odbicie nieba u góry i ukośne refleksy
    nak = Image.new("L", (Wp, Hp), 0); dn = ImageDraw.Draw(nak)
    for k in range(Hp // 3): dn.line([0, k, Wp, k], fill=int(70 * (1 - k / (Hp / 3))))
    for x0 in (int(Wp * .12), int(Wp * .58), int(Wp * .86)):
        dn.polygon([(x0, 0), (x0 + 60, 0), (x0 - 160, Hp), (x0 - 220, Hp)], fill=46)
    nak = nak.filter(ImageFilter.GaussianBlur(14))
    im = Image.composite(Image.new("RGB", (Wp, Hp), (206, 222, 236)), im, nak)
    im.save(out / "kp_wystawa.png", optimize=True)


def szyld(out, px_m=500):
    from PIL import Image, ImageDraw
    Wp, Hp = int(SZYLD[0] * px_m), int(SZYLD[1] * px_m)
    im = Image.new("RGB", (Wp, Hp), (241, 237, 224)); d = ImageDraw.Draw(im)
    d.rectangle([4, 4, Wp - 5, Hp - 5], outline=(32, 88, 58), width=7)
    txt = "KIOSK RUCH"; f = _font(FONT_B, Hp * .72); sp = Hp * .07                     # litery rozstrzelone
    szer = lambda f: sum(d.textlength(c, font=f) for c in txt) + sp * (len(txt) - 1)
    while szer(f) > Wp * .9: f = _font(FONT_B, f.size - 2)
    x = (Wp - szer(f)) / 2; bb = d.textbbox((0, 0), "K", font=f)
    y = (Hp - (bb[3] - bb[1])) / 2 - bb[1]
    for c in txt:
        d.text((x, y), c, font=f, fill=(196, 30, 38)); x += d.textlength(c, font=f) + sp
    im.save(out / "kp_szyld.png", optimize=True)


def ogloszenia(out, px_m=600):
    from PIL import Image, ImageDraw, ImageFilter
    g0, g1, v0, v1 = GABLOTA
    Wp, Hp = int((g1 - g0) * px_m), int((v1 - v0) * px_m)
    R = random.Random(56)
    im = Image.new("RGB", (Wp, Hp), (150, 108, 70)); d = ImageDraw.Draw(im)
    for _ in range(Wp * Hp // 18):                      # korek
        x, y = R.randrange(Wp), R.randrange(Hp); c = R.randint(-30, 30)
        d.point((x, y), fill=(150 + c, 108 + c, 70 + c // 2))
    im = im.filter(ImageFilter.GaussianBlur(.7)); d = ImageDraw.Draw(im); d.obraz = im
    m = int(.03 * px_m)
    kartki = [("GODZINY OTWARCIA", ["PN–PT 6:00–18:00", "SOBOTA 7:00–14:00", "NIEDZIELA nieczynne"], (250, 250, 246), True),
              ("SPRZEDAM ROWER", ["damka, 3 biegi", "stan dobry", "tel. 600 ··· ···"], (252, 250, 236), False),
              ("KOREPETYCJE", ["matematyka", "fizyka", "tel. 512 ··· ···"], (236, 244, 252), False),
              ("ZAGINĄŁ KOT", ["rudy, biała łapka", "okolice Rynku"], (252, 240, 226), False),
              ("WYNAJMĘ POKÓJ", ["blisko centrum", "studentce"], (246, 252, 236), False)]
    pos = [(m + 6, m + 6, Wp - 2 * m - 12, int(Hp * .2)), (m + 6, int(Hp * .27), int(Wp * .46), int(Hp * .3)),
           (int(Wp * .53), int(Hp * .25), Wp - m - 6 - int(Wp * .53), int(Hp * .28)), (m + 10, int(Hp * .6), int(Wp * .44), int(Hp * .3)),
           (int(Wp * .52), int(Hp * .57), Wp - m - 8 - int(Wp * .52), int(Hp * .3))]
    for (tyt, linie, papier, druk), (x, y, w, h) in zip(kartki, pos):
        d.rectangle([x + 3, y + 3, x + w + 3, y + h + 3], fill=(90, 64, 40))
        d.rectangle([x, y, x + w, y + h], fill=papier, outline=(190, 186, 176))
        _tekst_w(d, (x + 5, y + 4), tyt, _font(FONT_B, h * (.15 if druk else .13)), (30, 30, 34) if druk else (24, 40, 120), w - 10)
        f2 = _font(FONT_R, h * .11)
        for k, l in enumerate(linie):
            _tekst_w(d, (x + 6, y + 8 + int(h * (.2 + k * .15))), l, f2, (40, 40, 44), w - 12)
        if not druk:                                     # paski do odrywania
            for k in range(6):
                xx = x + 3 + k * (w - 6) / 6; d.line([xx, y + h * .78, xx, y + h], fill=(170, 166, 156), width=1)
        d.ellipse([x + w / 2 - 4, y - 2, x + w / 2 + 4, y + 6], fill=R.choice([(200, 30, 30), (30, 90, 200), (240, 200, 30)]))   # pinezka
    d.rectangle([0, 0, Wp - 1, Hp - 1], outline=(176, 178, 180), width=m)
    im.save(out / "kp_ogloszenia.png", optimize=True)


def telefon(out, px_m=1000):
    from PIL import Image, ImageDraw
    t0, t1, v0, v1 = TELEFON
    Wp, Hp = int((t1 - t0) * px_m), int((v1 - v0) * px_m)
    im = Image.new("RGB", (Wp, Hp), (244, 244, 240)); d = ImageDraw.Draw(im)
    nieb = (22, 70, 150)
    d.rectangle([6, 6, Wp - 7, Hp - 7], outline=nieb, width=6)
    fz = _font("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", Hp * .62); zn = "☎"      # aparat telefoniczny (znak ☎)
    bb = d.textbbox((0, 0), zn, font=fz)
    d.text(((Wp - (bb[2] - bb[0])) / 2 - bb[0], Hp * .38 - (bb[3] - bb[1]) / 2 - bb[1]), zn, font=fz, fill=nieb)
    f = _font(FONT_B, Hp * .17); txt = "TELEFON"; tw = d.textlength(txt, font=f)
    d.text(((Wp - tw) / 2, Hp * .72), txt, font=f, fill=nieb)
    im.save(out / "kp_telefon.png", optimize=True)


def naklejki(out):
    out = Path(out); out.mkdir(parents=True, exist_ok=True)
    wystawa(out); szyld(out); ogloszenia(out); telefon(out)


if __name__ == "__main__":
    naklejki(sys.argv[1] if len(sys.argv) > 1 else Path(__file__).resolve().parents[3] / "unity-city" / "Assets" / "Source" / "Claude" / "Bydgoszcz")
