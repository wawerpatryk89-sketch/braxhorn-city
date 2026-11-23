"""MIASTO R1 — osiedle bydgoskie przy Skłodowskiej-Curie: składanie budynków na miejscach z mapy OSM i eksport.

Układ: metry, x wschód, z północ, (0,0) = punkt zerowy wycinka mapy (w grze (-145,9; 508,8), skala K = 1,294).
Położenie i kąt każdego wieżowca dopasowane do obrysu OpenStreetMap (różnica pól 9–40 m² na 517 m²). Strona wejścia
i napisu (północ) — ze zdjęć właściciela: 28a (IMG szczyt od Leclerca), 30a (IMG_1427 w tle), 34a (zdjęcie baru), 32a.
"""
import json
import math
import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
from kit import Siatka, zapisz_bin, zapisz_glb, trojkaty  # noqa: E402
import blok_32a  # noqa: E402
import blok4  # noqa: E402
import maly  # noqa: E402
import teren  # noqa: E402
import wyposazenie  # noqa: E402
import ogolny  # noqa: E402

# Budynki z obrysu OSM (ogolny.py). Klucz: fragment nazwy z OSM albo (nr, ulica). zr = źródło liczby kondygnacji i koloru:
# OSM (building:levels), ZDJ (zdjęcie właściciela), ZAL (założenie — lista do potwierdzenia zdjęciem).
SPEC = [
    ("E.Leclerc", dict(kond=2, h_kond=3.6, parter=4.6, tynk="market_elewacja", witryny=True, okna_kier=(0, 1), okna=(3.4, 2.6, 1.5, .9), market=True,
                       attyka=1.0, pas_gorny=("market_niebieski", 1.1),
                       zr="ZDJ IMG_1577 (front od Skłodowskiej: witryny pasażu w parterze, pas okien piętra) i IMG_1730 (tył: biała hala, niebieski pas); bez nazwy i logo (decyzja 16:49)")),
    ("Pawilon Łóżkowy", dict(kond=5, h_kond=3.4, parter=3.8, tynk="szpital_krem", cokol="szpital_cegla", h_cokol=3.0, okna=(3.0, 1.6, 1.5, .9), zr="OSM 5 p., lotnicze IMG_1751 (kremowy, parter ceglasty)")),
    ("SOR", dict(kond=4, h_kond=3.8, parter=4.5, tynk="szpital_krem", okna=(2.6, 1.9, 1.5, .9), ladowisko=True, sor=True, front_kier=(0, -1), zr="ZDJ IMG_1727, IMG_1744–1751 (kremowy, szary panel, przeszklone wejście, lamele i lądowisko na dachu)")),
    ("Budynek F", dict(kond=5, h_kond=3.4, tynk="szpital_krem", cokol="szpital_cegla", h_cokol=3.0, okna=(3.2, 1.6, 1.5, .9), zr="lotnicze IMG_1751 (kremowy, 5–6 kond., parter ceglasty)")),
    ("Budynek A ", dict(kond=1, h_kond=3.6, tynk="szpital_szary", zr="OSM 1 p.")),
    ("Budynek B ", dict(kond=2, h_kond=3.4, tynk="szpital_szary", zr="ZAL 2 p.")),
    ("Budynek D ", dict(kond=4, h_kond=3.4, tynk="szpital_bialy", zr="OSM 4 p.")),
    ("Budynek C ", dict(kond=2, h_kond=3.4, tynk="szpital_szary", zr="OSM 2 p.")),
    ("Biblioteka", dict(kond=2, h_kond=3.6, tynk="tynk_szary", zr="OSM 2 p.")),
    ("Patomorfologii", dict(kond=3, h_kond=3.4, tynk="tynk_szary", zr="OSM 3 p.")),
    ("Farmaceutyczny", dict(kond=3, h_kond=3.4, tynk="tynk_kremowy", zr="ZAL 3 p.")),
    ("Katedra", dict(kond=2, h_kond=3.2, tynk="tynk_kremowy", zr="ZAL 2 p.")),
    (("38b", "Marii"), dict(kond=2, h_kond=3.2, tynk="tynk_kremowy", witryny=True, zr="OSM 2 p.")),
    (("40", "Marii"), dict(kond=5, h_kond=2.8, tynk="pole_a_1", okna=(3.0, 1.4, 1.3, .9), zr="OSM 5 p.")),
    (("30B", "Marii"), dict(kond=1, h_kond=3.6, tynk="tynk_paw_b", witryny=True, dach="koperta", dach_mat="dachowka", zr="ZDJ IMG_1596 (czerwony dach)")),
    (("36a", "Marii"), dict(kond=2, h_kond=3.2, tynk="przedszkole", okna=(2.8, 1.5, 1.3, .9), zr="ZDJ IMG_1736–1737 (2 kond., kremowe)")),
    (("7", "Aleja"), dict(kond=12, h_kond=2.8, parter=2.8, tynk="wysz_zolty", pola_kolorow=("wysz_zolty", "wysz_losos", "wysz_lawenda", "wysz_ziel"), nadbudowki=2, okna=(3.0, 1.5, 1.35, .9), zr="ZDJ IMG_1722–1726: 12 kond., pola kolorów, 2 nadbudówki")),
    (("9", "Aleja"), dict(kond=12, h_kond=2.8, parter=2.8, tynk="wysz_zolty", pola_kolorow=("wysz_zolty", "wysz_losos", "wysz_lawenda", "wysz_ziel"), nadbudowki=2, okna=(3.0, 1.5, 1.35, .9), zr="ZDJ IMG_1722–1726: 12 kond., pola kolorów, 2 nadbudówki")),
    (("11", "Aleja"), dict(kond=12, h_kond=2.8, parter=2.8, tynk="wysz_zolty", pola_kolorow=("wysz_zolty", "wysz_losos", "wysz_lawenda", "wysz_ziel"), nadbudowki=2, okna=(3.0, 1.5, 1.35, .9), zr="ZDJ IMG_1722–1726: 12 kond., pola kolorów, 2 nadbudówki")),
    (("13", "Aleja"), dict(kond=12, h_kond=2.8, parter=2.8, tynk="wysz_zolty", pola_kolorow=("wysz_zolty", "wysz_losos", "wysz_lawenda", "wysz_ziel"), nadbudowki=2, okna=(3.0, 1.5, 1.35, .9), zr="ZDJ IMG_1722–1726: 12 kond., pola kolorów, 2 nadbudówki")),
    (("15", "Żmudzka"), dict(kond=12, h_kond=2.8, tynk="tynk_bialy", okna=(3.0, 1.4, 1.3, .9), zr="OSM 12 p.")),
    (("17", "Żmudzka"), dict(kond=12, h_kond=2.8, tynk="tynk_bialy", okna=(3.0, 1.4, 1.3, .9), zr="OSM 12 p.")),
    (("19", "Żmudzka"), dict(kond=12, h_kond=2.8, tynk="tynk_bialy", okna=(3.0, 1.4, 1.3, .9), zr="OSM 12 p.")),
]
# Za Wyszyńskiego las (właściciel 23.11, 17:05: „za główną ulicą to już będzie las… tam skończymy”): linia wschodniej
# jezdni Wyszyńskiego z mapy + 15 m; na wschód od niej żadnych budynków ani ulic, gęsty las.
LAS_LINIA = [(130, -411), (246, 33), (262, 120), (239, 287), (238, 460)]


def x_lasu(z):
    pts = LAS_LINIA
    if z <= pts[0][1]: return pts[0][0] + 15
    for (x0, z0), (x1, z1) in zip(pts, pts[1:]):
        if z0 <= z <= z1: return x0 + (x1 - x0) * (z - z0) / (z1 - z0) + 15
    return pts[-1][0] + 15


def w_lesie(p):
    return p.x > x_lasu(p.y)


def las_poly():
    zs = np.linspace(-260, 360, 63)
    pts = [(x_lasu(z), z) for z in zs]
    return Polygon(pts + [(400, 360), (400, -260)])


DOMYSLNY = dict(kond=2, h_kond=3.0, tynk="tynk_szary", zr="ZAL 2 p. (brak zdjęcia)")
DOMYSLNY_MALY = dict(kond=1, h_kond=3.0, tynk="tynk_szary", zr="ZAL 1 p. (brak zdjęcia)")


def spec(b):
    for k, v in SPEC:
        if isinstance(k, str):
            if k in (b.get("nazwa") or ""): return v
        elif b.get("nr") == k[0] and (b.get("ulica") or "").startswith(k[1]): return v
    if b.get("rodzaj") == "school" or (b.get("ulica") or "").startswith("Jana Pest") and b.get("nr") == "18":
        return dict(kond=3, h_kond=3.3, tynk="szkola_ziel", okna=(2.6, 2.0, 1.45, .9), zr="ZDJ IMG_1729–1735 (3 kond., jasnozielona, szerokie okna)")
    p = b.get("pietra")
    if p and p.isdigit(): return dict(kond=int(p), h_kond=3.0, tynk="tynk_szary", zr="OSM %s p." % p)
    return DOMYSLNY if b["pole_m2"] >= 250 else DOMYSLNY_MALY


def inne(d, dst):
    """Wszystkie pozostałe budynki w wycinku — z obrysu, wg SPEC. Garaże i szopy (< 40 m²) pomijane."""
    from shapely.ops import unary_union
    zrobione = unary_union(MODELE) if MODELE else Polygon()
    opis = {}
    for i, b in enumerate(d["budynki"]):
        P = b["poly"]
        if P.is_empty or P.area < 40 or not teren.OKNO.contains(P.centroid) or w_lesie(P.centroid): continue
        if b.get("rodzaj") in ("garages", "garage", "roof"): continue
        if P.intersection(zrobione).area > .3 * P.area: continue
        sp = spec(b)
        S = ogolny.zbuduj(P, sp, "o%d" % i)
        przenies(S, dst, "o%d" % i, 0.0, 0.0, 0.0); MODELE.append(P)
        opis["o%d" % i] = {"nazwa": b.get("nazwa") or ("%s %s" % (b.get("ulica") or "", b.get("nr") or "")).strip(),
                           "kond": sp["kond"], "zrodlo": sp["zr"], "trojkatow": sum(len(c["i"]) // 3 for c in S.dane().values())}
    return opis
from shapely.geometry import Polygon, LineString, Point  # noqa: E402

K = 1.294
O = np.array([-145.9, 508.8])
DANE = Path(__file__).resolve().parent / "dane" / "osiedle-sklodowskiej-32a.json"


def wczytaj():
    """Mapa osiedla w metrach. Najpierw pełny wycinek (pobierz_osm.py: do ronda i szpitala), inaczej wycinek kolegi (gra)."""
    pelny = DANE.parent / "osiedle_osm.json"
    if pelny.exists():
        d = json.loads(pelny.read_text(encoding="utf-8"))
        for b in d["budynki"]:
            b["poly"] = Polygon([tuple(p) for p in b["obrys"]]).buffer(0); b["pole_m2"] = round(b["poly"].area)
        for u in d["ulice"]: u["linia"] = [tuple(p) for p in u["linia"]]
        for t in d["tereny"]: t["linia"] = [tuple(p) for p in t["linia"]]
        return d
    d = json.loads(DANE.read_text(encoding="utf-8"))
    for b in d["budynki"]:
        b["poly"] = Polygon([((x - O[0]) / K, (z - O[1]) / K) for x, z in b["obrys_xz"]]).buffer(0)
    for u in d["ulice"]:
        u["linia"] = [((x - O[0]) / K, (z - O[1]) / K) for x, z in u["linia_xz"]]
    for t in d["tereny"]:
        t["linia"] = [((x - O[0]) / K, (z - O[1]) / K) for x, z in t["linia_xz"]]
    return d


def prostokat(poly):
    """Prostokąt najmniejszego pola: środek, oś długa (wektor jednostkowy), długość, szerokość."""
    r = list(poly.minimum_rotated_rectangle.exterior.coords)[:4]
    e1 = np.subtract(r[1], r[0]); e2 = np.subtract(r[2], r[1])
    if np.linalg.norm(e1) < np.linalg.norm(e2): e1, e2 = e2, e1
    c = np.mean(r, axis=0)
    return c, e1 / np.linalg.norm(e1), float(np.linalg.norm(e1)), float(np.linalg.norm(e2))


def kat_frontu(f):
    """Kąt obrotu (osiedle.przenies) taki, że front obiektu (ściana lokalna z = 0, patrzy na -Z) patrzy w kierunku f."""
    return math.degrees(math.atan2(-f[0], -f[1]))


def ustaw(c, a, L, W, f):
    """Z osi długiej a, kierunku frontu f: kąt i narożnik lokalny (0,0). Oś X lokalna wzdłuż ±a, Z = -f."""
    k = kat_frontu(f); cr, sr = math.cos(math.radians(k)), math.sin(math.radians(k))
    X = np.array([cr, -sr]); Zv = np.array([sr, cr])
    o = np.asarray(c) - X * L / 2 - Zv * W / 2
    return k, float(o[0]), float(o[1]), X


def front_ze_wystepow(poly, c, a):
    """Strona z występami klatek (więcej wierzchołków obrysu po jednej długiej stronie) albo None."""
    n = np.array([-a[1], a[0]])
    pts = np.array(poly.exterior.coords)[:-1] - c
    s = pts @ n
    lewo, prawo = (s > 0).sum(), (s < 0).sum()
    if abs(lewo - prawo) >= 3: return n if lewo > prawo else -n
    return None


def front_do_drogi(d, c, a, klasy=("service", "residential", "tertiary")):
    n = np.array([-a[1], a[0]]); p = Point(*c); best = None
    for u in d["ulice"]:
        if u["klasa"] not in klasy or len(u["linia"]) < 2: continue
        q = LineString(u["linia"]).interpolate(LineString(u["linia"]).project(p))
        dist = p.distance(q)
        if best is None or dist < best[0]: best = (dist, np.array([q.x, q.y]) - c)
    v = best[1]
    return n if v @ n > 0 else -n

# nr: (kąt osi w stopniach na wschód od północy, przesunięcie x, z narożnika lokalnego (0,0) w metrach), ziarno balkonów
WIEZOWCE = {"28a": (11.5, -141.55, -53.05, 1), "30a": (12.25, -68.99, -33.14, 2), "32a": (11.5, -12.29, -13.70, 0),
            "34a": (11.0, 21.03, -56.82, 3)}


def przenies(src, dst, prefiks, kat, tx, tz):
    c, s = math.cos(math.radians(kat)), math.sin(math.radians(kat))
    R = np.array([[c, 0, s], [0, 1, 0], [-s, 0, c]])
    for (g, m), cz in src.dane().items():
        k = (prefiks + "/" + g, m)
        v = np.array(cz["v"]) @ R.T + np.array([tx, 0, tz]); n = np.array(cz["n"]) @ R.T
        t = np.array(cz["t"]); t3 = t[:, :3] @ R.T
        d = dst._c(*k); b = len(d["v"])
        d["v"] += [tuple(x) for x in v]; d["n"] += [tuple(x) for x in n]
        d["t"] += [tuple(a) + (w,) for a, w in zip(t3, t[:, 3])]
        d["uv"] += cz["uv"]; d["uv1"] += cz["uv1"]; d["i"] += [b + i for i in cz["i"]]


def wiezowce(dst):
    opis = {}
    for nr, (kat, tx, tz, seed) in WIEZOWCE.items():
        S, okien = blok_32a.zbuduj(nr, seed)
        przenies(S, dst, "w" + nr, kat, tx, tz)
        B = blok_32a
        # ściana wejściowa (północ połówki zachodniej) i ślepa ściana z napisem — przed nimi kostka do uliczki (IMG_1427)
        PRZEDPOLA.append((swiat(kat, tx, tz, 0, B.DL), swiat(kat, tx, tz, B.SZ_POL, B.DL), kier(kat, 0, 1), 16.0, "kostka"))
        # doświetle piwnicy przed ścianą wejściową (x lokalne −0,4…7,85, do 1,39 m od ściany): teren je omija
        MODELE.append(Polygon([tuple(swiat(kat, tx, tz, x, z)) for x, z in ((-.4, B.DL), (7.85, B.DL), (7.85, B.DL + 1.39), (-.4, B.DL + 1.39))]))
        PRZEDPOLA.append((swiat(kat, tx, tz, B.SZ_POL, B.DL + B.USKOK), swiat(kat, tx, tz, 2 * B.SZ_POL, B.DL + B.USKOK), kier(kat, 0, 1), 14.0, "kostka"))
        opis[nr] = {"trojkatow": trojkaty(S), "okien": okien, "kat": kat, "x": tx, "z": tz}
    return opis


# bloki czterokondygnacyjne: numer -> paleta (paleta.py, ze zdjęć). Strona wejść: z występów klatek w OSM (28, 30),
# dla pozostałych — do najbliższej drogi osiedlowej (ZAŁOŻENIE, na liście do potwierdzenia przez właściciela).
BLOKI4 = {"28": 4, "30": 1, "32": 2, "34": 3, "36": 3, "38": 1}
PN = np.array([0., 1.])
MODELE = []      # obrysy budynków postawionych modelem (teren je omija)
PRZEDPOLA = []   # (A, B, normalna na zewnątrz, maks. głębokość, materiał) — kostka od ściany wejściowej do jezdni


def swiat(kat, tx, tz, x, z):
    c, s_ = math.cos(math.radians(kat)), math.sin(math.radians(kat))
    return np.array([c * x + s_ * z + tx, -s_ * x + c * z + tz])


def kier(kat, x, z):
    c, s_ = math.cos(math.radians(kat)), math.sin(math.radians(kat))
    return np.array([c * x + s_ * z, -s_ * x + c * z])


def boiska_asfalt(d):
    """Plac za 32a (OSM: playground 846 m²) — właściciel 23.11, 16:26: „małe boisko na asfalcie z bramkami, placu zabaw
    nie musisz” (zdjęcia stamtąd nie ma). Wybór: plac zabaw z OSM najbliżej 32a (< 60 m)."""
    c32 = Point(*swiat(*WIEZOWCE["32a"][:3], 9.3, 15.0)); out = []
    for t in d["tereny"]:
        if t["rodzaj"] != "playground" or len(t["linia"]) < 3: continue
        p = Polygon(t["linia"]).buffer(0)
        if p.centroid.distance(c32) < 60: t["rodzaj"] = "boisko_asfalt"; out.append(p)
    return out


def bloki(d, dst):
    opis = {}
    for b in d["budynki"]:
        nr = b.get("nr"); ul = (b.get("ulica") or "")
        if not ul.startswith("Marii") or nr not in BLOKI4 or b["pole_m2"] < 300 or w_lesie(b["poly"].centroid): continue
        c, a, L, W = prostokat(b["poly"])
        f = front_ze_wystepow(b["poly"], c, a); zrodlo = "występy klatek w OSM"
        if f is None: f = front_do_drogi(d, c, a); zrodlo = "ZAŁOŻENIE: do najbliższej drogi"
        k, ox, oz, X = ustaw(c, a, L, W, f)
        strona = "W" if (-X) @ PN > 0 else "E"           # napis na szczycie od głównej ulicy (północ)
        S, okien = blok4.zbuduj(L, W, nr, BLOKI4[nr], klatki=3, seed=int(nr), napis_szczyt="napis_blok_" + nr, napis_strona=strona)
        przenies(S, dst, "b" + nr, k, ox, oz); MODELE.append(b["poly"])
        PRZEDPOLA.append((swiat(k, ox, oz, 0, 0), swiat(k, ox, oz, L, 0), kier(k, 0, -1), 12.0, "kostka"))
        opis[nr] = {"L": round(L, 2), "W": round(W, 2), "kat": round(k, 2), "front": zrodlo, "trojkatow": trojkaty(S)}
    return opis


def male(d, dst):
    opis = {}
    for i, b in enumerate(d["budynki"]):
        nr = b.get("nr"); rodzaj = b.get("rodzaj")
        if w_lesie(b["poly"].centroid) or not teren.OKNO.contains(b["poly"].centroid): continue
        c, a, L, W = prostokat(b["poly"])
        if rodzaj == "garages":
            f = front_do_drogi(d, c, a, ("service",)); k, ox, oz, X = ustaw(c, a, L, W, f)
            S = maly.garaze(L, W, seed=i, kolor="garaz_a" if i % 2 else "garaz_b"); nazwa = "garaze_%d" % i
        elif nr == "34b":
            n = np.array([-a[1], a[0]]); f = n if n @ PN > 0 else -n       # front na północ (uliczka), mural na zachód
            k, ox, oz, X = ustaw(c, a, L, W, f); S = maly.bar_mleczny(L, W); nazwa = "bar_34b"
        elif nr == "34B" or (rodzaj == "retail" and nr in ("32B",)) or (rodzaj == "retail" and b["pole_m2"] < 160 and nr is None):
            n = np.array([-a[1], a[0]]); f = n if n @ PN > 0 else -n
            k, ox, oz, X = ustaw(c, a, L, W, f)
            S = maly.pawilon(L, W, pas="pas_pomarancz" if nr == "34B" else "pas_granat", front_mat="tynk_paw_a"); nazwa = "pawilon_%s" % (nr or i)
        else:
            continue
        przenies(S, dst, nazwa, k, ox, oz); MODELE.append(b["poly"])
        if nazwa.startswith("garaze"):
            PRZEDPOLA.append((swiat(k, ox, oz, 0, 0), swiat(k, ox, oz, L, 0), kier(k, 0, -1), 10.0, "plac_garazy"))
        else:   # bar i pawilony: kostka dookoła (IMG_1631–1634)
            for (a0, a1, nn) in (((0, 0), (L, 0), (0, -1)), ((L, 0), (L, W), (1, 0)), ((0, W), (0, 0), (-1, 0)), ((L, W), (0, W), (0, 1))):
                PRZEDPOLA.append((swiat(k, ox, oz, *a0), swiat(k, ox, oz, *a1), kier(k, *nn), 6.0 if nn == (0, -1) else 3.0, "kostka"))
        opis[nazwa] = {"L": round(L, 2), "W": round(W, 2), "kat": round(k, 2), "trojkatow": trojkaty(S)}
    return opis


if __name__ == "__main__":
    out = Path(sys.argv[1] if len(sys.argv) > 1 else "."); out.mkdir(parents=True, exist_ok=True)
    D = Siatka(); d = wczytaj()
    opis = {"wiezowce": wiezowce(D), "bloki": bloki(d, D), "male": male(d, D)}
    for b in d["budynki"]:
        if b.get("nr") in WIEZOWCE and (b.get("ulica") or "").startswith("Marii"): MODELE.append(b["poly"])
    opis["inne"] = inne(d, D)
    pkt_osm, _ = wyposazenie.osm()
    T, pola, G = teren.zbuduj(d, MODELE, PRZEDPOLA, boiska_asfalt(d), wyposazenie.zebry(pkt_osm, d["ulice"], teren.SZER), las=las_poly())
    przenies(T, D, "teren", 0.0, 0.0, 0.0)
    drzewa, latarnie = teren.punkty(d, G)
    opis["teren"] = pola; opis["drzew"] = len(drzewa); opis["latarni"] = len(latarnie)
    Wy, opis["wyposazenie"] = wyposazenie.zbuduj(d, G, teren.SZER, teren.OKNO, drzewa, latarnie)
    przenies(Wy, D, "wyposazenie", 0.0, 0.0, 0.0)
    (out / "osiedle_punkty.json").write_text(json.dumps({"drzewa": drzewa, "latarnie": latarnie}, ensure_ascii=False), encoding="utf-8")
    from kit import scal_wg_materialu, scal_wg_budynku
    zapisz_glb(scal_wg_materialu(D), out / "osiedle.glb")
    import paleta
    G = scal_wg_budynku(D)
    uzyte = sorted({m for _, m in G.dane()})
    brak = [m for m in uzyte if m not in paleta.P and m not in ("okno", "okno_klatka")]
    if brak: raise SystemExit("materiały bez palety: " + ", ".join(brak))
    zapisz_bin(G, {m: paleta.P.get(m, {"okno": True}) for m in uzyte}, out / "osiedle.bin",
               {"punkt_zerowy_gry": [-145.9, 508.8], "K": K, "jednostka": "metr"}, "osiedle Skłodowskiej-Curie, Bydgoszcz (OSM ODbL)")
    opis["trojkatow"] = trojkaty(D)
    (out / "osiedle_opis.json").write_text(json.dumps(opis, ensure_ascii=False, indent=1), encoding="utf-8")
    print(json.dumps(opis, ensure_ascii=False))
