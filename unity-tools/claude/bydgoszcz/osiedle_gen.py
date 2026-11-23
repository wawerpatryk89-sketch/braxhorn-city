"""OSIEDLE BYDGOSKIE (M. Skłodowskiej-Curie 32a) — geometria z mapy, etap 1 (23.11.2026).

Właściciel 23.11, 02:07–02:43: odwzorować ulice i bloki przy Skłodowskiej-Curie 32a; „nie chcę lipy";
„zdjęcia do wglądu, zanim cokolwiek zaakceptuję". Układ: OpenStreetMap (osiedle_metry.json), wygląd: zdjęcia właściciela.

Całą geometrię liczy TEN skrypt i zapisuje do pliku, który gra tylko wczytuje (BraxBydgoszcz.cs) — dzięki temu
podgląd (podglad_*.png) jest rysowany z dokładnie tych samych trójkątów, które trafią do gry. Bez tekstur: kolor = barwa materiału.

    python3 osiedle_gen.py            → osiedle.bin, osiedle_opis.json, podglad_*.png

Jednostka: metr, (0,0) = blok 32a, x wschód, z północ, y w górę. Skalę K i miejsce w mieście nadaje gra.
Budynku, którego wyglądu nie ma na zdjęciach właściciela, skrypt NIE stawia — trafia na listę braków w opisie.
"""
import gzip, json, math, struct, sys
from pathlib import Path
import numpy as np
import mapbox_earcut as earcut
from shapely.geometry import LineString, Polygon, box
from shapely.geometry.polygon import orient
from shapely.ops import unary_union

TU = Path(__file__).resolve().parent
D = json.load(open(TU / "osiedle_metry.json", encoding="utf-8"))
UP = np.array([0.0, 1.0, 0.0])
MURAL = TU.parents[2] / "unity-city/Assets/Source/Claude/Bydgoszcz/mural_bar_mleczny.jpg"
try:
    from PIL import Image as _Obraz
    MURAL_ASPEKT = (lambda r: r[0] / r[1])(_Obraz.open(MURAL).size)
except Exception:
    MURAL_ASPEKT = 1.6

# materiał → (barwa do podglądu i tint w grze, rodzaj). Rodzaje czyta BraxBydgoszcz.cs.
MATS = {
    "tynk_szary": ((.70, .72, .74), "tynk"), "tynk_jasny": ((.90, .87, .78), "tynk"), "tynk_zolty": ((.90, .76, .38), "tynk"),
    "tynk_braz": ((.56, .31, .25), "tynk"), "tynk_oliwka": ((.62, .62, .36), "tynk"), "tynk_zielony": ((.60, .72, .52), "tynk"),
    "tynk_bialy": ((.94, .94, .92), "tynk"), "tynk_zoltozielony": ((.80, .80, .46), "tynk"), "tynk_pomarancz": ((.86, .60, .42), "tynk"),
    "tynk_niebieski": ((.30, .45, .72), "tynk"), "cokol_czerwony": ((.52, .27, .24), "beton"), "cokol_szary": ((.45, .46, .47), "beton"),
    "cegla": ((.60, .30, .22), "cegla"), "dach": ((.26, .26, .27), "beton"), "beton": ((.70, .69, .66), "beton"),
    "attyka_niebieska": ((.22, .30, .55), "blacha"), "attyka_pomarancz": ((.86, .42, .22), "blacha"), "brama_drewno": ((.50, .30, .20), "drewno"),
    "drzwi": ((.34, .22, .16), "drewno"), "szyba": ((.16, .21, .27), "szklo"), "rama": ((.95, .95, .95), "gladki"), "rama_ciemna": ((.20, .21, .22), "gladki"),
    "balkon_a": ((.42, .60, .44), "gladki"), "balkon_b": ((.72, .72, .70), "gladki"), "balkon_c": ((.62, .30, .24), "gladki"),
    "asfalt": ((.27, .27, .28), "asfalt"), "asfalt_stary": ((.36, .35, .34), "asfalt_stary"), "chodnik": ((.62, .60, .57), "chodnik"),
    "mural_bar_mleczny": ((.42, .12, .12), "obraz"),   # właściciel 23.11, 08:01: mural kolegi — „możesz go normalnie wstawić”
    "kraweznik": ((.66, .65, .62), "beton"), "trawa": ((.36, .52, .24), "trawa"), "ziemia": ((.30, .24, .17), "ziemia"),
}


class Siatki:
    def __init__(s):
        s.m = {}; s.boksy = []; s.napisy = []
    def quad(s, g, mat, pts, hint):
        pts = [np.asarray(p, float) for p in pts]
        n = np.cross(pts[1] - pts[0], pts[2] - pts[0]); dl = np.linalg.norm(n)
        if dl < 1e-9: return
        n /= dl
        if np.dot(n, hint) < 0: pts = pts[::-1]; n = -n
        v, i = s.m.setdefault((g, mat), ([], []))
        b = len(v)
        for p in pts:
            ax, ay, az = abs(n)
            uv = (p[0], p[2]) if ay >= ax and ay >= az else ((p[2], p[1]) if ax >= az else (p[0], p[1]))
            v.append((*p, *n, *uv))
        for k in range(1, len(pts) - 1): i.extend((b, b + k, b + k + 1))
    def obraz(s, g, mat, p0, d, s0, s1, y0, y1):
        """Obraz na ścianie w wycięciu (bez nakładki): UV 0…1, czytany z zewnątrz od lewej do prawej."""
        n = np.cross(d, UP); v, i = s.m.setdefault((g, mat), ([], [])); b = len(v)
        for q, y, uv in ((s0, y0, (1, 0)), (s1, y0, (0, 0)), (s1, y1, (0, 1)), (s0, y1, (1, 1))): v.append((*(p0 + d * q + UP * y), *n, *uv))
        i.extend((b, b + 1, b + 2, b, b + 2, b + 3))
    def plaszcz(s, g, mat, wielokat, y, hint=UP, skrot=None):
        """Wielokąt poziomy (z otworami) na wysokości y."""
        for p in (wielokat.geoms if hasattr(wielokat, "geoms") else [wielokat]):
            if p.is_empty or p.area < .05: continue
            pier = [np.array(p.exterior.coords)[:-1]] + [np.array(r.coords)[:-1] for r in p.interiors]
            w = np.vstack(pier); konce = np.cumsum([len(r) for r in pier]).astype(np.uint32)
            tr = earcut.triangulate_float64(w, konce).reshape(-1, 3)
            for a, b, c in tr:
                gg = g if skrot is None else "%s_%d_%d" % (g, int((w[a][0] + 200) // 100), int((w[a][1] + 200) // 100))
                s.quad(gg, mat, [(w[a][0], y, w[a][1]), (w[b][0], y, w[b][1]), (w[c][0], y, w[c][1])], hint)
    def fartuch(s, g, mat, wielokat, y0, y1):
        """Pionowe lico wzdłuż wszystkich krawędzi wielokąta (krawężnik, skraj trawnika, bok płyty)."""
        for p in (wielokat.geoms if hasattr(wielokat, "geoms") else [wielokat]):
            if p.is_empty: continue
            p = orient(p, -1.0)   # zgodnie z ruchem wskazówek: na zewnątrz = kierunek × góra
            for r in [p.exterior] + list(p.interiors):
                c = list(r.coords)
                for (ax, az), (bx, bz) in zip(c[:-1], c[1:]):
                    d = np.array([bx - ax, 0, bz - az]); dl = np.linalg.norm(d)
                    if dl < .02: continue
                    s.quad(g, mat, [(ax, y0, az), (bx, y0, bz), (bx, y1, bz), (ax, y1, az)], np.cross(d / dl, UP))
    def plyta(s, g, mat, wielokat, y0, y1):
        s.plaszcz(g, mat, wielokat, y1); s.plaszcz(g, mat, wielokat, y0, -UP); s.fartuch(g, mat, wielokat, y0, y1)
    def skrzynka(s, g, mat, o, u, n, a0, a1, y0, y1, b0, b1):
        """Prostopadłościan w układzie: o + u·a + n·b (u, n — wektory poziome)."""
        P = lambda a, y, b: o + u * a + n * b + UP * y
        for pts, h in (([P(a0, y0, b1), P(a1, y0, b1), P(a1, y1, b1), P(a0, y1, b1)], n), ([P(a0, y0, b0), P(a1, y0, b0), P(a1, y1, b0), P(a0, y1, b0)], -n),
                       ([P(a1, y0, b0), P(a1, y0, b1), P(a1, y1, b1), P(a1, y1, b0)], u), ([P(a0, y0, b0), P(a0, y0, b1), P(a0, y1, b1), P(a0, y1, b0)], -u),
                       ([P(a0, y1, b0), P(a1, y1, b0), P(a1, y1, b1), P(a0, y1, b1)], UP), ([P(a0, y0, b0), P(a1, y0, b0), P(a1, y0, b1), P(a0, y0, b1)], -UP)):
            s.quad(g, mat, pts, h)
    def sciana(s, g, p0, d, dl, y0, y1, baza, otwory=(), pola=()):
        """Ściana z PRAWDZIWYMI otworami (bez nakładek). p0 — początek, d — kierunek, na zewnątrz = d × góra.
        otwory: (s0, s1, v0, v1, rodzaj); pola: (s0, s1, v0, v1, materiał); baza(s, v) → materiał tynku."""
        n = np.cross(d, UP); R = .12
        P = lambda q, v, gl=0.0: p0 + d * q + UP * v - n * gl
        ss = sorted({0.0, dl} | {x for o in list(otwory) + list(pola) for x in o[:2] if 0 < x < dl})
        vs = sorted({y0, y1} | {x for o in list(otwory) + list(pola) for x in o[2:4] if y0 < x < y1} | {x for x in baza.progi if y0 < x < y1})
        for a, b in zip(ss[:-1], ss[1:]):
            for c, e in zip(vs[:-1], vs[1:]):
                sm, vm = (a + b) / 2, (c + e) / 2
                if any(o[0] < sm < o[1] and o[2] < vm < o[3] for o in otwory): continue
                mat = next((o[4] for o in pola if o[0] < sm < o[1] and o[2] < vm < o[3]), None) or baza(sm, vm)
                if mat == "__puste": continue
                s.quad(g, mat, [P(a, c), P(b, c), P(b, e), P(a, e)], n)
        for a, b, c, e, rodzaj in otwory:
            oscieze = baza((a + b) / 2, (c + e) / 2)
            s.quad(g, oscieze, [P(a, c), P(a, c, R), P(a, e, R), P(a, e)], d); s.quad(g, oscieze, [P(b, c), P(b, c, R), P(b, e, R), P(b, e)], -d)
            s.quad(g, oscieze, [P(a, c), P(b, c), P(b, c, R), P(a, c, R)], UP); s.quad(g, oscieze, [P(a, e), P(b, e), P(b, e, R), P(a, e, R)], -UP)
            if rodzaj in ("drzwi", "brama"):
                s.quad(g, "drzwi" if rodzaj == "drzwi" else "brama_drewno", [P(a, c, R), P(b, c, R), P(b, e, R), P(a, e, R)], n); continue
            s.quad(g, "szyba", [P(a, c, R), P(b, c, R), P(b, e, R), P(a, e, R)], n)
            rama, t, gl = ("rama_ciemna" if rodzaj == "witryna" else "rama"), .07, R - .04
            for q in ((a, a + t, c, e), (b - t, b, c, e), (a + t, b - t, c, c + t), (a + t, b - t, e - t, e), ((a + b) / 2 - t / 2, (a + b) / 2 + t / 2, c + t, e - t)):
                s.quad(g, rama, [P(q[0], q[2], gl), P(q[1], q[2], gl), P(q[1], q[3], gl), P(q[0], q[3], gl)], n)


def Baza(f, progi=()):
    f.progi = tuple(progi); return f


def okna_w_przeslach(dl, przeslo, szer, margines=1.2):
    """Środki przęseł rozłożone symetrycznie na długości ściany."""
    ile = int((dl - 2 * margines) // przeslo)
    if ile < 1: return []
    start = (dl - ile * przeslo) / 2
    return [start + przeslo * (i + .5) for i in range(ile)]


# ---------------------------------------------------------------------------------------------- typy budynków (ze zdjęć)
WIEZOWCE_SZARE = {"28a", "30a", "32a", "34a"}
BLOKI4 = {"28", "30", "32", "34", "36", "38"}

def typ_budynku(b):
    ul, nr, rodz = b["ulica"] or "", (b["nr"] or ""), b["rodzaj"]
    if rodz in ("garages", "garage"): return "garaze"
    if "Skłodowskiej" in ul and nr in WIEZOWCE_SZARE: return "wiezowiec_szary"
    if "Wyszyńskiego" in ul and b["pole_m2"] > 450: return "wiezowiec_kolor"
    if "Skłodowskiej" in ul and nr in BLOKI4 and b["dlugosc_m"] > 40: return "blok4"
    if nr == "34b" and b.get("uzytek") == "restaurant": return "bar"
    if rodz == "retail" or ("Skłodowskiej" in ul and nr in ("32", "32B", "34B") and b["pole_m2"] < 320): return "pawilon"
    if "Pestalozziego" in ul or rodz == "school": return "szkola"
    return None


def buduj(S, b, typ, nr_kol, drogi):
    g = "bud_%s_%s" % (typ, b["osm"])
    poly = orient(Polygon(b["obrys"]).buffer(0).simplify(.15), -1.0)
    c = list(poly.exterior.coords)
    sr = np.array([poly.centroid.x, 0, poly.centroid.y])
    cel = min(drogi, key=lambda q: (q[0] - sr[0]) ** 2 + (q[1] - sr[2]) ** 2)
    front = np.array([cel[0] - sr[0], 0, cel[1] - sr[2]]); front /= max(np.linalg.norm(front), 1e-6)
    a = math.radians(b["kat"]); U = np.array([math.cos(a), 0, math.sin(a)]); N = np.array([-U[2], 0, U[0]])
    if abs(np.dot(front, N)) >= abs(np.dot(front, U)): front = N * np.sign(np.dot(front, N))    # front = dłuższy bok od strony drogi
    else: front = N if np.dot(N, (-1, 0, -.3)) > 0 else -N
    pietra = {"wiezowiec_szary": 11, "wiezowiec_kolor": 11, "blok4": 4, "szkola": 3 if b["szerokosc_m"] >= 10 else 1}.get(typ, 1)
    hk = {"wiezowiec_szary": 2.75, "wiezowiec_kolor": 2.75, "blok4": 2.9, "szkola": 3.4, "garaze": 2.5, "pawilon": 3.6, "bar": 3.8}[typ]
    cok = {"wiezowiec_szary": .6, "wiezowiec_kolor": .9, "blok4": 1.0, "szkola": .5}.get(typ, .15)
    H = cok + pietra * hk
    zachod = np.array([-1, 0, -.3]) / math.hypot(1, .3)
    drzwi_dane = False; napis_dany = False; mural = None
    if typ == "bar":      # mural: najdłuższa ściana bez wejścia, najbliżej uliczki (zdjęcie właściciela: ściana przy chodniku)
        kand = []
        for j, ((ax, az), (bx, bz)) in enumerate(zip(c[:-1], c[1:])):
            dd = np.array([bx - ax, 0, bz - az]); L = float(np.linalg.norm(dd))
            if L < 6 or float(np.dot(np.cross(dd / L, UP), front)) > .5: continue
            nn = np.cross(dd / L, UP); mx, mz = (ax + bx) / 2 + nn[0] * 2, (az + bz) / 2 + nn[2] * 2
            kand.append((min((q[0] - mx) ** 2 + (q[1] - mz) ** 2 for q in drogi), j))
        mural = min(kand)[1] if kand else None
    for j, ((ax, az), (bx, bz)) in enumerate(zip(c[:-1], c[1:])):
        mur = None
        p0 = np.array([ax, 0, az]); d = np.array([bx - ax, 0, bz - az]); dl = float(np.linalg.norm(d))
        if dl < .3: continue
        d /= dl; n = np.cross(d, UP); cf = float(np.dot(n, front)); dluga = dl > 0.6 * b["dlugosc_m"]
        otw, pola, progi = [], [], [cok]
        if typ == "wiezowiec_szary":
            baza = Baza(lambda q, v: "cokol_szary" if v < cok else "tynk_szary", progi)
            balkony = dluga and float(np.dot(n, zachod)) > .5
            if dl > 6 and abs(cf) > .5 or dluga:
                for i, q in enumerate(okna_w_przeslach(dl, 3.0, 1.5)):
                    for k in range(pietra):
                        y = cok + k * hk
                        if balkony and i % 2 == 0:
                            otw.append((q - 1.05, q + 1.05, y + .12, y + 2.2, "okno"))
                            S.skrzynka(g, "beton", p0, d, n, q - 1.5, q + 1.5, y - .06, y + .1, 0, 1.25)
                            pan = ("balkon_a", "balkon_b", "balkon_c")[(i + k + nr_kol) % 3]
                            S.skrzynka(g, pan, p0, d, n, q - 1.5, q + 1.5, y + .1, y + 1.12, 1.2, 1.25)
                            S.skrzynka(g, pan, p0, d, n, q - 1.5, q - 1.45, y + .1, y + 1.12, 0, 1.2); S.skrzynka(g, pan, p0, d, n, q + 1.45, q + 1.5, y + .1, y + 1.12, 0, 1.2)
                        elif not (k == 0 and not balkony and not drzwi_dane and dluga and abs(q - dl / 2) < 1.6):
                            otw.append((q - .7, q + .7, y + .95, y + 2.35, "okno"))
                if dluga and not balkony and not drzwi_dane:
                    otw.append((dl / 2 - .8, dl / 2 + .8, .15, 2.35, "drzwi")); drzwi_dane = True
                    S.skrzynka(g, "beton", p0, d, n, dl / 2 - 1.6, dl / 2 + 1.6, 2.5, 2.65, 0, 1.6); S.skrzynka(g, "beton", p0, d, n, dl / 2 - 1.4, dl / 2 + 1.4, 0, .15, 0, 1.2)
            elif dl > 8 and not napis_dany:      # ślepy szczyt: pole oliwkowe z adresem (zdjęcie 28a)
                napis_dany = True; y = H - 9.5
                pola.append((dl / 2 - 3.6, dl / 2 + 3.6, y, y + 6.2, "tynk_oliwka"))
                srod = p0 + d * (dl / 2) + n * .02
                for tekst, yy, wys in ((b["nr"], y + 4.6, 1.5), ("M. SKŁODOWSKIEJ", y + 2.9, .62), ("-CURIE", y + 1.9, .62)):
                    S.napisy.append((tekst, *(srod + UP * yy), *n, wys, .12, .12, .13))
        elif typ == "wiezowiec_kolor":
            def kolor(q, v, dl=dl):
                if v < cok: return "cokol_szary"
                k = int((v - cok) // hk)
                if abs(v - (cok + 6 * hk)) < .35 or (dl > 12 and abs(q - dl * .62) < .45 and 4 <= k < 8): return "tynk_niebieski"
                return "tynk_zoltozielony" if k < 4 else ("tynk_szary" if k < 8 else "tynk_pomarancz")
            baza = Baza(kolor, [cok] + [cok + k * hk for k in (4, 8)] + [cok + 6 * hk - .35, cok + 6 * hk + .35])
            if dl > 6:
                if dl > 12: pola.append((dl * .62 - .45, dl * .62 + .45, cok + 4 * hk, cok + 8 * hk, "tynk_niebieski"))
                for i, q in enumerate(okna_w_przeslach(dl, 2.9, 1.5)):
                    sz = 1.1 if i % 2 == 0 else .62
                    if abs(q - dl * .62) < sz + .5: continue
                    for k in range(pietra):
                        y = cok + k * hk
                        if k == 0 and dluga and cf > .5 and not drzwi_dane and abs(q - dl / 2) < 1.5:
                            otw.append((q - .8, q + .8, .15 - cok + cok, 2.4, "drzwi")); drzwi_dane = True
                            S.skrzynka(g, "beton", p0, d, n, q - 1.7, q + 1.7, 2.55, 2.7, 0, 1.5)
                        else: otw.append((q - sz, q + sz, y + .95, y + 2.35, "okno"))
        elif typ == "blok4":
            baza = Baza(lambda q, v: "cokol_czerwony" if v < cok else "tynk_jasny", progi)
            if dluga:
                srodki = okna_w_przeslach(dl, 3.3, 1.5)
                for i, q in enumerate(srodki):
                    klatka = cf > .5 and i % 4 == 2
                    if klatka:
                        pola.append((q - 1.25, q + 1.25, 0, H, "tynk_braz"))
                        otw.append((q - .6, q + .6, .18, 2.3, "drzwi")); S.skrzynka(g, "beton", p0, d, n, q - 1.2, q + 1.2, 2.45, 2.57, 0, 1.1)
                        for k in range(1, pietra): otw.append((q - .45, q + .45, cok + k * hk - .55, cok + k * hk + .45, "okno"))
                        continue
                    for k in range(pietra):
                        y = cok + k * hk
                        naroze = cf < -.5 and i in (0, len(srodki) - 1)
                        if naroze:
                            otw.append((q - 1.0, q + 1.0, y + .12, y + 2.25, "okno"))
                            S.skrzynka(g, "beton", p0, d, n, q - 1.5, q + 1.5, y - .06, y + .1, 0, 1.3)
                            S.skrzynka(g, "balkon_c", p0, d, n, q - 1.5, q + 1.5, y + .1, y + 1.1, 1.25, 1.3)
                        else:
                            otw.append((q - .75, q + .75, y + .95, y + 2.4, "okno"))
                            if i % 2 == 0 and i + 1 < len(srodki): pola.append((q + .75, q + 3.3 - .75, y + .95, y + 2.4, "tynk_zolty"))
            elif dl > 8:
                for k in range(pietra): otw.append((dl / 2 - .7, dl / 2 + .7, cok + k * hk + .95, cok + k * hk + 2.4, "okno"))
                if not napis_dany and float(np.dot(n, (0, 0, 1))) > -.2:
                    napis_dany = True; srod = p0 + d * (dl * .5) + n * .02
                    S.napisy.append(("M. SKŁODOWSKIEJ", *(srod + UP * (H - 1.5)), *n, .55, .12, .12, .13)); S.napisy.append(("-CURIE " + b["nr"], *(srod + UP * (H - 2.3)), *n, .55, .12, .12, .13))
        elif typ == "szkola":
            baza = Baza(lambda q, v: "cokol_szary" if v < cok else "tynk_zielony", progi)
            if dl > 6:
                for q in okna_w_przeslach(dl, 3.2, 2.0):
                    for k in range(pietra):
                        if k == 0 and dluga and cf > .5 and not drzwi_dane and abs(q - dl / 2) < 1.7 and pietra == 3 and b["dlugosc_m"] > 60:
                            otw.append((q - 1.3, q + 1.3, cok, cok + 2.6, "witryna")); drzwi_dane = True
                            S.skrzynka(g, "beton", p0, d, n, q - 5, q + 5, cok + 3.0, cok + 3.25, 0, 3.0); S.skrzynka(g, "beton", p0, d, n, q - 5, q + 5, 0, cok, 0, 3.0)
                            for t in (-4.6, -1.6, 1.6, 4.6): S.skrzynka(g, "tynk_bialy", p0, d, n, q + t - .2, q + t + .2, cok, cok + 3.0, 2.5, 2.9)
                        else: otw.append((q - 1.0, q + 1.0, cok + k * hk + .9, cok + k * hk + 2.8, "okno"))
        elif typ == "garaze":
            kol = ("tynk_bialy", "tynk_jasny", "tynk_szary")
            baza = Baza(lambda q, v, dl=dl: kol[int(q // 2.9) % 3] if dl > 8 else "tynk_bialy", [])
            if cf > .5 and dl > 5:
                ile = max(1, int(dl // 2.9)); st = (dl - ile * 2.9) / 2
                pola += [(st + 2.9 * i, st + 2.9 * (i + 1), 0, H, kol[(i + nr_kol) % 3]) for i in range(ile)]
                otw += [(st + 2.9 * i + .25, st + 2.9 * (i + 1) - .25, .04, 2.1, "brama") for i in range(ile)]
        else:   # pawilon, bar
            tynk, att = ("tynk_zolty", None) if typ == "bar" else ("tynk_bialy", ("attyka_niebieska", "attyka_pomarancz")[nr_kol % 2])
            baza = Baza(lambda q, v, tynk=tynk: "cokol_szary" if v < cok else tynk, progi)
            if att: pola.append((0, dl, H - .85, H, att))
            if cf > .5 and dl > 5:
                sr_ok = okna_w_przeslach(dl, 3.4, 2.6, .8)
                for i, q in enumerate(sr_ok):
                    if i == len(sr_ok) // 2: otw.append((q - .9, q + .9, cok, 2.45, "witryna" if typ == "pawilon" else "drzwi"))
                    elif typ == "bar": otw.append((q - .9, q + .9, 1.0, 2.5, "okno"))
                    else: otw.append((q - 1.3, q + 1.3, .5, 2.6, "witryna"))
                if typ == "bar" and not napis_dany:
                    napis_dany = True; S.napisy.append(("BAR MLECZNY", *(p0 + d * (dl / 2) + n * .03 + UP * 3.15), *n, .5, .45, .12, .10))
            elif typ == "bar" and dl > 5 and j != mural:
                for q in okna_w_przeslach(dl, 3.4, 1.8): otw.append((q - .9, q + .9, 1.0, 2.5, "okno"))
        if typ == "bar" and j == mural:
            szer = min(3.0 * MURAL_ASPEKT, dl - .6); wys = szer / MURAL_ASPEKT
            mur = (dl / 2 - szer / 2, dl / 2 + szer / 2, .45, .45 + wys); pola.append((*mur, "__puste"))
        S.sciana(g, p0, d, dl, 0, H, baza, otw, pola)
        if mur: S.obraz(g, "mural_bar_mleczny", p0, d, *mur)
    okap = {"garaze": .25, "bar": .7, "blok4": .35, "pawilon": .1}.get(typ, 0)
    S.plyta(g, "dach", poly.buffer(okap, join_style=2) if okap else poly, H, H + .14)
    if typ == "blok4":
        for t in np.linspace(-.38, .38, 5): S.skrzynka(g, "cegla", sr + U * (t * b["dlugosc_m"]), U, N, -.6, .6, H + .14, H + 1.5, -.35, .35)
    if typ.startswith("wiezowiec"): S.skrzynka(g, "tynk_szary" if typ == "wiezowiec_szary" else "tynk_pomarancz", sr, U, N, -3, 3, H + .14, H + 2.7, -2.2, 2.2)
    S.boksy.append((sr[0], H / 2, sr[2], b["dlugosc_m"], H, b["szerokosc_m"], -b["kat"]))
    return {"osm": b["osm"], "adres": ((b["ulica"] or "") + " " + (b["nr"] or "")).strip(), "typ": typ, "kondygnacje": pietra, "wysokosc_m": round(H, 2)}


def grunt(S):
    okno = box(*[D["okno"][i] for i in (0, 2, 1, 3)])
    pas = lambda kl, pol: unary_union([LineString(u["linia"]).buffer(pol, cap_style=2, join_style=2) for u in D["ulice"] if u["klasa"] in kl and len(u["linia"]) > 1] or [Polygon()])
    glowne = pas(("primary", "secondary", "tertiary"), 3.5).union(pas(("residential", "unclassified"), 2.75))
    osiedlowe = pas(("service",), 2.25).union(unary_union([Polygon(t["linia"]).buffer(0) for t in D["tereny"] if t["rodzaj"] == "parking" and t["zamkniety"] and len(t["linia"]) > 3] or [Polygon()]))
    asf = glowne.intersection(okno).simplify(.08); stary = osiedlowe.difference(glowne).intersection(okno).simplify(.08)
    chod = pas(("footway", "path", "pedestrian", "steps", "cycleway"), 1.0).difference(glowne.union(osiedlowe)).intersection(okno).simplify(.08)
    trawa = okno.difference(unary_union([glowne, osiedlowe, chod])).simplify(.08)
    for g, mat, w, y in (("grunt_asfalt", "asfalt", asf, 0), ("grunt_asfalt_stary", "asfalt_stary", stary, 0), ("grunt_chodnik", "chodnik", chod, .12), ("grunt_trawa", "trawa", trawa, .05)):
        S.plaszcz(g, mat, w, y, skrot=True)
    S.fartuch("grunt_kraweznik", "kraweznik", chod, 0, .12); S.fartuch("grunt_skraj", "ziemia", trawa, 0, .05)
    return {n: round(w.area) for n, w in (("asfalt_m2", asf), ("asfalt_osiedlowy_m2", stary), ("chodnik_m2", chod), ("trawa_m2", trawa))}


def zapisz(S, sciezka):
    nazwy = sorted({m for _, m in S.m}); out = bytearray(b"BYD1")
    def txt(t): b = t.encode("utf-8"); return struct.pack("<i", len(b)) + b
    out += struct.pack("<i", len(nazwy))
    for n in nazwy: out += txt(n) + txt(MATS[n][1]) + struct.pack("<3f", *MATS[n][0])
    out += struct.pack("<i", len(S.m))
    for (g, m), (v, i) in sorted(S.m.items()):
        a = np.asarray(v, "<f4"); flagi = (1 if g.startswith("grunt_") and m != "kraweznik" and m != "ziemia" else 0) | (2 if m == "trawa" else 0) | (4 if MATS[m][1] == "obraz" else 0)
        out += txt(g) + struct.pack("<iBii", nazwy.index(m), flagi, len(a), len(i)) + a.tobytes() + np.asarray(i, "<i4").tobytes()
    out += struct.pack("<i", len(S.boksy)) + b"".join(struct.pack("<7f", *b) for b in S.boksy)
    out += struct.pack("<i", len(S.napisy)) + b"".join(txt(n[0]) + struct.pack("<10f", *n[1:]) for n in S.napisy)
    sciezka.write_bytes(gzip.compress(bytes(out), 9, mtime=0)); return len(out), sciezka.stat().st_size


def podglad(S, plik, oko, cel, kat=62, rozm=(1600, 900)):
    import matplotlib; matplotlib.use("Agg")
    import matplotlib.pyplot as plt
    from matplotlib.collections import PolyCollection
    oko, cel = np.array(oko, float), np.array(cel, float); f = cel - oko; f /= np.linalg.norm(f)
    r = np.cross(UP, f); r /= np.linalg.norm(r); u = np.cross(f, r); slonce = np.array([.45, .8, .4]); slonce /= np.linalg.norm(slonce)
    wiel, kol, gl = [], [], []; BL = .6
    for (g, m), (v, i) in S.m.items():
        a = np.asarray(v); P = a[:, :3] - oko; T = np.asarray(i).reshape(-1, 3)
        C = np.column_stack([P @ r, P @ u, P @ f])                      # współrzędne kamery
        nn = a[T[:, 0], 3:6]; przodem = np.einsum("ij,ij->i", nn, a[T[:, 0], :3] - oko) < 0
        jasn = .45 + .55 * np.clip(nn @ slonce, 0, 1)
        for t, j in zip(T[przodem], jasn[przodem]):
            w = C[t]
            if (w[:, 2] <= BL).all(): continue
            if (w[:, 2] <= BL).any():                                   # przycięcie do bliskiej płaszczyzny (trójkąt przechodzi za kamerę)
                nowe = []
                for k in range(3):
                    p1, p2 = w[k], w[(k + 1) % 3]
                    if p1[2] > BL: nowe.append(p1)
                    if (p1[2] > BL) != (p2[2] > BL): nowe.append(p1 + (p2 - p1) * (BL - p1[2]) / (p2[2] - p1[2]))
                w = np.array(nowe)
            wiel.append(w[:, :2] / w[:, 2:3]); gl.append(w[:, 2].mean() + (.08 if m == "szyba" else 0)); kol.append(np.clip(np.array(MATS[m][0]) * j, 0, 1))
    kol_nieba = (.66, .78, .92); mg = np.clip((np.array(gl) - 150) / 900, 0, .35)[:, None]
    kol = np.array(kol) * (1 - mg) + np.array(kol_nieba) * mg; por = np.argsort(gl)[::-1]
    t = math.tan(math.radians(kat / 2)); fig = plt.figure(figsize=(rozm[0] / 100, rozm[1] / 100), dpi=100, facecolor=kol_nieba); ax = fig.add_axes([0, 0, 1, 1])
    ax.add_collection(PolyCollection([wiel[i] for i in por], facecolors=kol[por], edgecolors=kol[por], linewidths=.7))
    ax.set_xlim(-t * rozm[0] / rozm[1], t * rozm[0] / rozm[1]); ax.set_ylim(-t, t); ax.axis("off")
    ax.text(.01, .02, "PODGLĄD GEOMETRII, bez tekstur — to nie jest zdjęcie z gry", transform=ax.transAxes, fontsize=9, color="#222", bbox=dict(fc="#ffffffb0", ec="none"))
    fig.savefig(plik, facecolor=kol_nieba); plt.close(fig)


def main():
    S = Siatki(); pow_ = grunt(S)
    drogi = [tuple(p) for u in D["ulice"] if u["klasa"] in ("service", "residential", "tertiary", "unclassified") for p in u["linia"]]
    zbudowane, braki = [], []
    for nr_kol, b in enumerate(sorted(D["budynki"], key=lambda q: q["osm"])):
        typ = typ_budynku(b)
        if typ is None:
            braki.append({"osm": b["osm"], "adres": ((b["ulica"] or "") + " " + (b["nr"] or "")).strip() or "bez adresu w mapie", "rodzaj_w_mapie": b["rodzaj"], "wymiar_m": [b["dlugosc_m"], b["szerokosc_m"]], "srodek": b["srodek"]})
            continue
        zbudowane.append(buduj(S, b, typ, nr_kol, drogi))
    surowe, spak = zapisz(S, TU / "osiedle.bin")
    tr = {}; razem = 0
    for (g, m), (v, i) in S.m.items(): tr[g.split("_")[0] if g.startswith("grunt") else g] = tr.get(g.split("_")[0] if g.startswith("grunt") else g, 0) + len(i) // 3; razem += len(i) // 3
    opis = {"trojkatow_razem": razem, "trojkatow_grunt": tr.get("grunt", 0), "najciezszy_budynek": max((v, k) for k, v in tr.items() if k != "grunt"),
            "siatek": len(S.m), "plik_bajtow": spak, "powierzchnie": pow_, "budynkow_zbudowanych": len(zbudowane), "wg_typu": {t: sum(1 for z in zbudowane if z["typ"] == t) for t in sorted({z["typ"] for z in zbudowane})},
            "zbudowane": zbudowane, "braki_bez_zdjecia": braki, "napisy": [n[0] for n in S.napisy]}
    (TU / "osiedle_opis.json").write_text(json.dumps(opis, ensure_ascii=False, indent=1), encoding="utf-8")
    print({k: v for k, v in opis.items() if k not in ("zbudowane", "braki_bez_zdjecia")}); print("braki:", len(braki))
    if "--bez-podgladu" not in sys.argv:
        podglad(S, TU / "podglad_1_z_gory.png", (-95, 150, -175), (5, 0, 30), kat=58)
        from shapely.geometry import Point
        zajete = unary_union([Polygon(q["obrys"]).buffer(4) for q in D["budynki"]])
        def stan(cx, cz, r0, r1, warunek=lambda x, z: True):       # punkt na uliczce osiedlowej, poza budynkami, r0…r1 m od celu
            kand = [q for u in D["ulice"] if u["klasa"] == "service" for q in u["linia"]
                    if r0 < math.hypot(q[0] - cx, q[1] - cz) < r1 and warunek(*q) and not zajete.contains(Point(q))]
            return min(kand, key=lambda q: math.hypot(q[0] - cx, q[1] - cz))
        k2 = stan(0, 0, 38, 70, lambda x, z: x < -8); k3 = stan(-54, 18, 16, 40, lambda x, z: z < 16)
        podglad(S, TU / "podglad_2_ulica_32a.png", (k2[0], 1.9, k2[1]), (0, 13, 0), kat=74)
        podglad(S, TU / "podglad_3_blok_i_garaze.png", (k3[0], 1.9, k3[1]), (-54, 4, 30), kat=74)
        opis["kamery_podgladu"] = {"2": k2, "3": k3}
    return S, opis


if __name__ == "__main__":
    main()
