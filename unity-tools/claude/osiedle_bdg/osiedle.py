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
from shapely.geometry import Polygon, LineString, Point  # noqa: E402

K = 1.294
O = np.array([-145.9, 508.8])
DANE = Path(__file__).resolve().parent / "dane" / "osiedle-sklodowskiej-32a.json"


def wczytaj():
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
        opis[nr] = {"trojkatow": trojkaty(S), "okien": okien, "kat": kat, "x": tx, "z": tz}
    return opis


# bloki czterokondygnacyjne: numer -> paleta (paleta.py, ze zdjęć). Strona wejść: z występów klatek w OSM (28, 30),
# dla pozostałych — do najbliższej drogi osiedlowej (ZAŁOŻENIE, na liście do potwierdzenia przez właściciela).
BLOKI4 = {"28": 4, "30": 1, "32": 2, "34": 3, "36": 3, "38": 1}
PN = np.array([0., 1.])
MODELE = []      # obrysy budynków postawionych modelem (teren je omija)


def bloki(d, dst):
    opis = {}
    for b in d["budynki"]:
        nr = b.get("nr"); ul = (b.get("ulica") or "")
        if not ul.startswith("Marii") or nr not in BLOKI4 or b["pole_m2"] < 300: continue
        c, a, L, W = prostokat(b["poly"])
        f = front_ze_wystepow(b["poly"], c, a); zrodlo = "występy klatek w OSM"
        if f is None: f = front_do_drogi(d, c, a); zrodlo = "ZAŁOŻENIE: do najbliższej drogi"
        k, ox, oz, X = ustaw(c, a, L, W, f)
        strona = "W" if (-X) @ PN > 0 else "E"           # napis na szczycie od głównej ulicy (północ)
        S, okien = blok4.zbuduj(L, W, nr, BLOKI4[nr], klatki=3, seed=int(nr), napis_szczyt="napis_blok_" + nr, napis_strona=strona)
        przenies(S, dst, "b" + nr, k, ox, oz); MODELE.append(b["poly"])
        opis[nr] = {"L": round(L, 2), "W": round(W, 2), "kat": round(k, 2), "front": zrodlo, "trojkatow": trojkaty(S)}
    return opis


def male(d, dst):
    opis = {}
    for i, b in enumerate(d["budynki"]):
        nr = b.get("nr"); rodzaj = b.get("rodzaj")
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
        opis[nazwa] = {"L": round(L, 2), "W": round(W, 2), "kat": round(k, 2), "trojkatow": trojkaty(S)}
    return opis


if __name__ == "__main__":
    out = Path(sys.argv[1] if len(sys.argv) > 1 else "."); out.mkdir(parents=True, exist_ok=True)
    D = Siatka(); d = wczytaj()
    opis = {"wiezowce": wiezowce(D), "bloki": bloki(d, D), "male": male(d, D)}
    for b in d["budynki"]:
        if b.get("nr") in WIEZOWCE and (b.get("ulica") or "").startswith("Marii"): MODELE.append(b["poly"])
    T, pola, G = teren.zbuduj(d, MODELE)
    przenies(T, D, "teren", 0.0, 0.0, 0.0)
    drzewa, latarnie = teren.punkty(d, G)
    opis["teren"] = pola; opis["drzew"] = len(drzewa); opis["latarni"] = len(latarnie)
    (out / "osiedle_punkty.json").write_text(json.dumps({"drzewa": drzewa, "latarnie": latarnie}, ensure_ascii=False), encoding="utf-8")
    from kit import scal_wg_materialu
    zapisz_glb(scal_wg_materialu(D), out / "osiedle.glb")
    opis["trojkatow"] = trojkaty(D)
    (out / "osiedle_opis.json").write_text(json.dumps(opis, ensure_ascii=False, indent=1), encoding="utf-8")
    print(json.dumps(opis, ensure_ascii=False))
