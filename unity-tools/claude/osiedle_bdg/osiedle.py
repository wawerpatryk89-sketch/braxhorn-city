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
from shapely.geometry import box, LineString, Point, Polygon  # noqa: E402
from shapely.ops import unary_union  # noqa: E402

sys.path.insert(0, str(Path(__file__).resolve().parent))
from kit import Siatka, UP, nrm, zapisz_bin, zapisz_glb, trojkaty  # noqa: E402
import blok_32a  # noqa: E402
import blok4  # noqa: E402
import maly  # noqa: E402
import teren  # noqa: E402
import wyposazenie  # noqa: E402
import ogolny  # noqa: E402

# Budynki z obrysu OSM (ogolny.py). Klucz: fragment nazwy z OSM albo (nr, ulica). zr = źródło liczby kondygnacji i koloru:
# OSM (building:levels), ZDJ (zdjęcie właściciela), ZAL (założenie — lista do potwierdzenia zdjęciem).
SPEC = [
    # 25.11 (właściciel 15:41: „niebieskie gówna”, „okien nie ma”): bez niebieskiego pasa pod attyką, okna piętra od ulic
    ("E.Leclerc", dict(kond=2, h_kond=3.6, parter=4.6, tynk="market_elewacja", witryny=True, okna_kier=(0, 1), okna=(3.6, 3.0, 1.7, .9), market=True,
                       attyka=1.0, okna_ulica=True,
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
    """Las za Wyszyńskiego (17:05) + pas lasu między miastem a osiedlem (z −254…−214 m), poza korytarzami dróg (wjazdy)."""
    zs = np.linspace(-260, 360, 63)
    pts = [(x_lasu(z), z) for z in zs]
    las = Polygon(pts + [(400, 360), (400, -260)])
    pas = box(-330, -270, 240, -214)
    kor = [LineString(u["linia"]).buffer({"primary": 16, "secondary": 14, "tertiary": 12}.get(u["klasa"], 8))
           for u in WCZYTANE.get("ulice", []) if u["klasa"] in ("primary", "secondary", "tertiary", "residential", "service")
           and len(u["linia"]) > 1 and LineString(u["linia"]).intersects(pas)]
    return unary_union([las, pas.difference(unary_union(kor))]) if kor else unary_union([las, pas])


WCZYTANE = {}


def wjazd1(d):
    """Wjazd 1 z miasta (właściciel 19:51–19:52): ulica z działki po starym supermarkecie (oś x −126 j.) na północ do
    najbliższej drogi osiedla; przekrój jak uliczka osiedlowa (kostka, chodniki)."""
    p0 = (teren.WJAZD1_X, -268.0)
    best = None
    for u in d["ulice"]:
        if u["klasa"] not in ("residential", "service", "tertiary", "living_street", "unclassified") or len(u["linia"]) < 2: continue
        L = LineString(u["linia"]).intersection(box(-330, -200, 250, 300))
        for g in getattr(L, "geoms", [L]):
            if g.is_empty or g.geom_type != "LineString": continue
            q = g.interpolate(g.project(Point(p0[0], -150)))
            dd = Point(p0).distance(q)
            if best is None or dd < best[0]: best = (dd, (q.x, q.y))
    cel = best[1] if best else (p0[0], -150)
    d["ulice"].append({"osm": "wjazd1", "klasa": "residential", "nazwa": "wjazd z miasta 1", "pasy": None, "jednokier": None,
                       "rondo": False, "nawierzchnia": "asphalt",
                       "szer": 2 * 4.5 / 1.294,   # 25.11: jezdnia jak Aleja Północna miasta (2 × AH = 9 j.), styk bez uskoku
                       "chodnik": (12.0 - 4.5) / 1.294,   # chodnik do lica alei (12 j. od osi) — przedłużenie chodników alei
                       "linia": [p0, (p0[0], -212.0), (cel[0] * .5 + p0[0] * .5, (cel[1] - 212) * .5), cel]})
    return cel


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
        # 25.11 (Street View IMG_2134–2137, plan OSM): o92 przyklejony od południa do hali marketu to zaplecze — biała
        # hala ok. 6 m z szarym cokołem, rampy i bramy od placu rozładunku (ogolny.market_tyl), bez okien mieszkalnych
        lec = [q_["poly"] for q_ in d["budynki"] if "Leclerc" in (q_.get("nazwa") or "")]
        if lec and not (b.get("nazwa") or "") and P.distance(lec[0]) < .5 and P.area > 1000:
            sp = dict(kond=1, h_kond=6.0, parter=6.0, tynk="market_elewacja", cokol="cokol", h_cokol=.9, okna=(30.0, 1.2, .8, 3.6),
                      okna_kier=(0, -1), attyka=.6, zaplecze=True,
                      zr="ZDJ IMG_2134–2137 (zaplecze marketu: biała hala, rampy, bramy segmentowe, schody stalowe, czarny płot)")
        if sp.get("sor"):                       # drogi przy SOR: przerwy w murkach podjazdu
            sp = dict(sp, drogi=[LineString(u["linia"]) for u in d["ulice"] if len(u["linia"]) > 1 and LineString(u["linia"]).distance(P) < 40])
        # 25.11: wejście od strony najbliższej drogi albo chodnika (ogolny.wejscie)
        bl = [LineString(u["linia"]) for u in d["ulice"] if len(u["linia"]) > 1 and LineString(u["linia"]).distance(P) < 60]
        sp = dict(sp, dojscia=unary_union(bl) if bl else None)
        if (b.get("nr") or "").lower() == "38b" and (b.get("ulica") or "").startswith("Marii"):
            # 25.11 (właściciel 17:32, IMG_2152/2153): pawilon 38B wg zdjęć — pawilon38b.py, front do najbliższej drogi
            import pawilon38b
            c_, a_, L_, W_ = prostokat(P); f_ = front_do_drogi(d, c_, a_); k_, ox_, oz_, X_ = ustaw(c_, a_, L_, W_, f_)
            S = pawilon38b.zbuduj(L_, W_); przenies(S, dst, "o%d" % i, k_, ox_, oz_); MODELE.append(P)
            PRZEDPOLA.append((swiat(k_, ox_, oz_, 0, 0), swiat(k_, ox_, oz_, L_, 0), kier(k_, 0, -1), 6.0, "kostka"))
            opis["o%d" % i] = {"nazwa": "M. Skłodowskiej-Curie 38B", "kond": 2, "zrodlo": "ZDJ IMG_2152/2153 (pawilon38b.py)",
                               "trojkatow": sum(len(c["i"]) // 3 for c in S.dane().values())}
            continue
        S = ogolny.zbuduj(P, sp, "o%d" % i)
        if sp.get("market"):        # 25.11 (IMG_2139–2141): pod podcieniem bocznym i do uliczki — kostka od ściany
            from shapely.geometry.polygon import orient as _or
            pp_ = list(_or(P.buffer(0).simplify(.15), 1.0).exterior.coords)
            for (x0_, z0_), (x1_, z1_) in zip(pp_, pp_[1:]):
                e_ = np.array([x1_ - x0_, z1_ - z0_]); L_ = float(np.linalg.norm(e_))
                if L_ < 15: continue
                n_ = np.array([e_[1], -e_[0]]) / L_
                if P.contains(Point(*((np.array([x0_ + x1_, z0_ + z1_]) / 2 + n_ * .5)))): n_ = -n_
                if n_[0] < -.7: PRZEDPOLA.append((np.array([x0_, z0_]), np.array([x1_, z1_]), n_, 10.0, "kostka"))
        if sp.get("zaplecze"):      # 25.11 (IMG_2135–2137): plac rozładunku z kostki za zapleczem — do jezdni
            c_, a_, L_, W_ = prostokat(P); nn_ = np.array([-a_[1], a_[0]]); nn_ = nn_ if nn_[1] < 0 else -nn_
            A_ = np.asarray(c_) - a_ * L_ / 2 + nn_ * W_ / 2; B_ = np.asarray(c_) + a_ * L_ / 2 + nn_ * W_ / 2
            PRZEDPOLA.append((A_, B_, nn_, 30.0, "plac_garazy"))
        przenies(S, dst, "o%d" % i, 0.0, 0.0, 0.0); MODELE.append(P)
        MODELE.extend(getattr(S, "wyciecia", []))
        opis["o%d" % i] = {"nazwa": b.get("nazwa") or ("%s %s" % (b.get("ulica") or "", b.get("nr") or "")).strip(),
                           "kond": sp["kond"], "zrodlo": sp["zr"], "trojkatow": sum(len(c["i"]) // 3 for c in S.dane().values())}
    return opis
from shapely.geometry import Polygon, LineString, Point  # noqa: E402

K = 1.294
O = np.array([-145.9, 508.8])
DANE = Path(__file__).resolve().parent / "dane" / "osiedle-sklodowskiej-32a.json"


def budki_market(d, dst):
    """Budki z jedzeniem na parkingu przed frontem marketu (właściciel 18:32, zdjęcia nocne 18:47): przyczepa z
    zapiekankami przy wejściu, budka z burgerami przy słupie z szyldem i girlandą, lodziarnia z podestem — bliżej ulicy."""
    import budki
    from shapely.geometry.polygon import orient
    for b in d["budynki"]:
        if "Leclerc" not in (b.get("nazwa") or ""): continue
        P = orient(b["poly"].simplify(.15), 1.0); pts = list(P.exterior.coords); fr = []
        for (x0, z0), (x1, z1) in zip(pts, pts[1:]):
            a = np.array([x0, 0., z0]); bb = np.array([x1, 0., z1]); L = float(np.linalg.norm(bb - a))
            if L < 3: continue
            t = (bb - a) / L; n = np.array([-t[2], 0., t[0]])
            if P.contains(Point(*((a + bb) / 2 + n * .5)[[0, 2]])): a, bb, t, n = bb, a, -t, -n
            if n[2] > .5: fr.append((a, bb, t, n, L))
        if not fr: return 0
        a, bb, t, n, L = max(fr, key=lambda f: f[4])
        S = Siatka()
        # 25.11 (właściciel 17:06, Street View IMG_2145–2149): budki stoją w rzędzie wzdłuż Skłodowskiej, na zachód od
        # przystanku przed marketem, między chodnikiem a parkingiem — kiosk „BAR”, kosze, przyczepa z burgerami (głębiej,
        # na trawie), kontener z kebabem, drewniany pawilon kawa/lody z pergolą; girlanda żarówek, palety, hydrant, szafka.
        # (Wcześniejsze zapiekanki/kontener/lodziarnia na parkingu były ustawione na oko — usunięte.)
        ul = [LineString(u["linia"]) for u in d["ulice"] if (u.get("nazwa") or "").startswith("Marii") and len(u["linia"]) > 1]
        P0 = Point(-201.2, 57.7)                                                   # przystanek (OSM stop_position)
        L0 = min(ul, key=lambda l: l.distance(P0)); s0 = L0.project(P0)
        q0, q1 = L0.interpolate(max(0, s0 - 2)), L0.interpolate(min(L0.length, s0 + 2))
        ts = nrm(np.array([q1.x - q0.x, 0., q1.y - q0.y])); ns = np.array([-ts[2], 0., ts[0]])
        c_m = b["poly"].centroid
        if ns @ np.array([c_m.x - P0.x, 0., c_m.y - P0.y]) < 0: ns = -ns       # ns: od ulicy w stronę marketu
        if ts[0] > 0: ts = -ts                                                   # ts: na zachód (od przystanku)
        q = L0.interpolate(s0); B0 = np.array([q.x, Y_KOSTKI, q.y]) + ns * 9.0
        R = lambda s_, g_=0.0: B0 + ts * s_ + ns * g_
        budki.kiosk_bar(S, R(8.0), -ts, -ns)
        budki.przyczepa_burger(S, R(15.5, 2.5), -ts, -ns)
        budki.kontener_kebab(S, R(23.0, 1.0), -ts, -ns)
        budki.pawilon_kawa(S, R(31.5, 2.0), -ts, -ns)
        budki.girlanda(S, R(19.0, -1.5), R(27.0, -1.5)); budki.girlanda(S, R(27.0, -1.5), R(36.0, -1.5))
        budki.palety(S, R(27.5, -.8), ts, ns, 4.8)
        przenies(S, dst, "budki", 0.0, 0.0, 0.0)
        # 22:44 właściciel: bus z bosną (jego model Tripo, paczka szyfrowana bydgoszcz_bosna); 24.11 00:02 „postaw go na
        # parkingu” — pole E4 siatki parkingu (32 m przed frontem marketu), wzdłuż frontu, okienko (bok +z modelu) w stronę
        # wejścia do marketu (−n): kabina w stronę −t
        p_ = a + t * 11.6 + n * 31.75
        MODELE_WL.append(["bydgoszcz_bosna", "bosna_van", round(float(p_[0]), 2), round(float(p_[2]), 2), round(float(-t[0]), 4), round(float(-t[2]), 4)])
        S2 = Siatka(); budki.bosna_otwarta(S2, p_ + UP * Y_KOSTKI, -t, -n)     # otwarte okienko z daszkiem (00:03–00:04)
        przenies(S2, dst, "budki_bosna", 0.0, 0.0, 0.0)
        sp_, kl_, s_ = budki.PUNKTY["sprzedawca"], budki.PUNKTY["klient"], budki.PUNKTY["s"]
        AKCJE.append({"id": "bosna_kup", "etykieta": "KUP BOSNĘ", "odpowiedz": "Sprzedawca: Bosna klasyczna, ostra czy z serem? Z musztardą i cebulką?",
                      "x": round(float(kl_[0]), 2), "y": 0.0, "z": round(float(kl_[2]), 2), "promien": 1.8, "mowca": "bosna_sprzedawca"})
        AKCJE.append({"id": "bosna_sprzedawca", "etykieta": "MIEJSCE SPRZEDAWCY", "odpowiedz": "Za ladą busa z bosną — tu stoi sprzedawca.",
                      "x": round(float(sp_[0]), 2), "y": round(float(sp_[1]), 2), "z": round(float(sp_[2]), 2), "promien": .9,
                      "kier_x": round(float(s_[0]), 4), "kier_z": round(float(s_[2]), 4)})
        return 4
    return 0


Y_KOSTKI = 0.0
AKCJE = []              # punkty akcji (osiedle_akcje.json → BraxBydgoszcz): kupno, miejsce sprzedawcy
MODELE_WL = []          # modele właściciela z paczek szyfrowanych: [paczka, plik, x, z, kier_x, kier_z] (metry; kier = oś kabiny)


def zapisz_maske_trawy(trawa, drzewa, plik, krok=.32):
    """Maska trawnika dla trawy 3D w grze (BraxClaudeTrawaMaska): 1 bit na krok × krok j. gry, bez pierścieni ziemi pod
    drzewami (0,8 m). Format (gzip): 'BTM1', float32 x0 z0 krok y, int32 szer wys, bity (wiersze od z0, bit = x % 8)."""
    import gzip, struct
    from PIL import Image, ImageDraw
    x0m, z0m, x1m, z1m = trawa.bounds
    gx0, gz0 = O[0] + x0m * K, O[1] + z0m * K
    W = int(math.ceil((x1m - x0m) * K / krok)) + 1; H = int(math.ceil((z1m - z0m) * K / krok)) + 1
    img = Image.new("1", (W, H), 0); dr = ImageDraw.Draw(img)
    px = lambda x, z: ((O[0] + x * K - gx0) / krok, (O[1] + z * K - gz0) / krok)
    for g in getattr(trawa, "geoms", [trawa]):
        if g.is_empty: continue
        dr.polygon([px(x, z) for x, z in g.exterior.coords], fill=1)
        for r in g.interiors: dr.polygon([px(x, z) for x, z in r.coords], fill=0)
    for (x, z, _) in drzewa:
        cx, cz = px(x, z); rr = .8 * K / krok; dr.ellipse((cx - rr, cz - rr, cx + rr, cz + rr), fill=0)
    bity = np.packbits(np.asarray(img, dtype=np.uint8), axis=1, bitorder="little")
    with gzip.open(plik, "wb") as f:
        f.write(b"BTM1" + struct.pack("<4f2i", gx0, gz0, krok, teren.Y_TRAWA * K, bity.shape[1] * 8, H)); f.write(bity.tobytes())
    return W, H


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


_JEZDNE = None


def front_garazy(d, c, a, L, W):
    """25.11 (właściciel 15:19: „garaże chyba powinny być w drugą stronę […] nie ma dojazdu do garaży”): bramy na ten dłuższy bok,
    przed którym jest bliżej do jezdni albo parkingu (mediana z 5 promieni prostopadłych do boku, do 40 m); plac przed bramami
    sięga do jezdni (przedpole do 25 m) — dojazd."""
    global _JEZDNE
    if _JEZDNE is None:
        g = [LineString(u["linia"]).buffer((u.get("szer") or teren.SZER.get(u["klasa"], 4.6)) / 2) for u in d["ulice"]
             if u["klasa"] in ("service", "residential", "living_street", "unclassified", "tertiary", "secondary") and len(u["linia"]) > 1]
        for t in d["tereny"]:
            if t["rodzaj"] == "parking" and len(t["linia"]) > 2:
                try: g.append(Polygon(t["linia"]).buffer(0))
                except Exception: pass
        _JEZDNE = unary_union(g)
    n = np.array([-a[1], a[0]]); best = None
    for s_ in (1.0, -1.0):
        nn = n * s_; dl = []
        for u in np.linspace(-.4, .4, 5):
            p0 = np.asarray(c) + a * (u * L) + nn * (W / 2 + .3)
            x = LineString([tuple(p0), tuple(p0 + nn * 40)]).intersection(_JEZDNE)
            dl.append(40.0 if x.is_empty else Point(*p0).distance(x))
        m = float(np.median(dl))
        if best is None or m < best[0]: best = (m, nn)
    return best[1]


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
        # zejście do piwnicy przed ścianą wejściową (blok_32a.klatka, 25.11): wykop z dnem i ścianami — teren go omija
        x0_, x1_ = B.SZ_POL - B.ZEJ_U1, B.SZ_POL - B.ZEJ_U0                  # u → x lokalne (t = −X od narożnika SZ_POL)
        MODELE.append(Polygon([tuple(swiat(kat, tx, tz, x, z)) for x, z in ((x0_, B.DL), (x1_, B.DL), (x1_, B.DL + B.ZEJ_D), (x0_, B.DL + B.ZEJ_D))]))
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
            f = front_garazy(d, c, a, L, W); k, ox, oz, X = ustaw(c, a, L, W, f)
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
            PRZEDPOLA.append((swiat(k, ox, oz, 0, 0), swiat(k, ox, oz, L, 0), kier(k, 0, -1), 25.0, "plac_garazy"))   # do jezdni (dojazd)
        else:   # bar i pawilony: kostka dookoła (IMG_1631–1634)
            for (a0, a1, nn) in (((0, 0), (L, 0), (0, -1)), ((L, 0), (L, W), (1, 0)), ((0, W), (0, 0), (-1, 0)), ((L, W), (0, W), (0, 1))):
                PRZEDPOLA.append((swiat(k, ox, oz, *a0), swiat(k, ox, oz, *a1), kier(k, *nn), 6.0 if nn == (0, -1) else 3.0, "kostka"))
        opis[nazwa] = {"L": round(L, 2), "W": round(W, 2), "kat": round(k, 2), "trojkatow": trojkaty(S)}
    return opis


if __name__ == "__main__":
    out = Path(sys.argv[1] if len(sys.argv) > 1 else "."); out.mkdir(parents=True, exist_ok=True)
    D = Siatka(); d = wczytaj(); WCZYTANE.update(d); wjazd1_cel = wjazd1(d)
    opis = {"wiezowce": wiezowce(D), "bloki": bloki(d, D), "male": male(d, D)}
    for b in d["budynki"]:
        if b.get("nr") in WIEZOWCE and (b.get("ulica") or "").startswith("Marii"): MODELE.append(b["poly"])
    opis["inne"] = inne(d, D)
    opis["budki"] = budki_market(d, D)
    pkt_osm, _ = wyposazenie.osm()
    T, pola, G = teren.zbuduj(d, MODELE, PRZEDPOLA, boiska_asfalt(d), wyposazenie.zebry(pkt_osm, d["ulice"], teren.SZER), las=las_poly())
    przenies(T, D, "teren", 0.0, 0.0, 0.0)
    drzewa, latarnie = teren.punkty(d, G)
    # 25.11 (właściciel 15:16: „chodniki zarośnięte”): trawa 3D 30 cm od krawędzi chodników, jezdni i obrzeży — kępy nie wchodzą na kostkę
    opis["teren"] = pola; opis["drzew"] = len(drzewa); opis["latarni"] = len(latarnie)
    Wy, opis["wyposazenie"] = wyposazenie.zbuduj(d, G, teren.SZER, teren.OKNO, drzewa, latarnie)
    # 25.11 (15:43: wiata „zarośnięta trawą”): kępy trawy 3D nie rosną na płycie pod wiatą
    zapisz_maske_trawy(G["TRAWA"].buffer(-.3).difference(wyposazenie.plyty_wiat()), drzewa, out / "osiedle_trawa_maska.bin")
    przenies(Wy, D, "wyposazenie", 0.0, 0.0, 0.0)
    (out / "osiedle_punkty.json").write_text(json.dumps({"drzewa": drzewa, "latarnie": latarnie, "modele": MODELE_WL}, ensure_ascii=False), encoding="utf-8")
    (out / "osiedle_akcje.json").write_text(json.dumps({"akcje": AKCJE}, ensure_ascii=False, indent=1), encoding="utf-8")
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
