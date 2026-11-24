"""MIASTO R1 — osiedle bydgoskie, wzór bloku 32a (23.11.2026): generator, budżet, zdjęcia właściciela w repo zaszyfrowane."""
import importlib.util
import json
import sys
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
BDG = ROOT / "unity-tools/claude/osiedle_bdg"
PACZKA = ROOT / "unity-city/Prywatne/braxhorn-zdjecia-bydgoszcz-01"
MA_NUMPY = importlib.util.find_spec("numpy") is not None


class ZdjeciaWlasciciela(unittest.TestCase):
    def test_paczka_zaszyfrowana_i_kompletna(self):
        man = json.loads((PACZKA / "manifest.json").read_text(encoding="utf-8"))
        pliki = man["pliki"]
        self.assertGreaterEqual(sum(k.startswith("zdjecia/osiedle/") for k in pliki), 60)
        self.assertGreaterEqual(sum(k.startswith("zdjecia/stare_miasto/") for k in pliki), 13)
        for c in man["czesci"]:
            self.assertTrue((PACZKA / c["name"]).is_file())
        self.assertFalse(PACZKA.name.startswith("braxhorn-sklep-"), "wzór nie może trafiać do gry przez instalator sklepu")

    def test_zadnych_jawnych_zdjec(self):
        for p in BDG.rglob("*"):
            if p.parent.name == "plan": continue          # nasze rysunki planu (nie zdjęcia właściciela)
            self.assertNotIn(p.suffix.lower(), (".png", ".jpg", ".jpeg", ".webp"), p)


@unittest.skipUnless(MA_NUMPY, "brak numpy")
class Blok32a(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        sys.path.insert(0, str(BDG))
        spec = importlib.util.spec_from_file_location("blok_32a", BDG / "blok_32a.py")
        cls.m = importlib.util.module_from_spec(spec); spec.loader.exec_module(cls.m)
        cls.m.blok()
        from kit import trojkaty
        cls.tri = trojkaty(cls.m.S)

    def test_budzet_budynku(self):
        self.assertLess(self.tri, 60000)          # ZASADY 10.2: budynek <= 60 tys.

    def test_wymiary_ze_zdjec(self):
        m = self.m
        self.assertAlmostEqual(m.KOND, 2.70); self.assertEqual(m.N_KOND, 11)
        self.assertAlmostEqual(m.PRZESLO * 5, m.DL, places=2)

    def test_balkony_i_okna(self):
        grupy = {g for (g, _m) in self.m.S.dane()}
        self.assertTrue({"sciana_W", "sciana_E", "sciana_N_zach", "sciana_N_wsch", "klatka"} <= grupy)
        bal = sum(len(c["i"]) for (g, mat), c in self.m.S.dane().items() if mat == "beton_balkon")
        self.assertEqual(bal // 3, 2 * 5 * 10 * 10)  # 100 płyt po 10 trójkątów (bez tyłu przy ścianie)
        self.assertGreater(self.m.okno_nr[0], 300)

    def test_niewykonczone_nazwane(self):
        grupy = {g for (g, _m) in self.m.S.dane()}
        self.assertIn("sciana_S_NIEWYKONCZONA", grupy)   # południe czeka na zdjęcia — nie zgadujemy


MA_SHAPELY = importlib.util.find_spec("shapely") is not None


@unittest.skipUnless(MA_NUMPY and MA_SHAPELY, "brak numpy / shapely")
class Osiedle(unittest.TestCase):
    """Całe osiedle z mapy: 4 wieżowce, 6 bloków, garaże, bar z muralem, pawilony, teren; budżet sceny ZASADY 10.2."""
    @classmethod
    def setUpClass(cls):
        sys.path.insert(0, str(BDG))
        import osiedle as OS
        from kit import Siatka, trojkaty
        import teren
        cls.OS = OS; d = OS.wczytaj(); D = Siatka(); OS.MODELE.clear()
        cls.opis = {"wiezowce": OS.wiezowce(D), "bloki": OS.bloki(d, D), "male": OS.male(d, D)}
        for b in d["budynki"]:
            if b.get("nr") in OS.WIEZOWCE: OS.MODELE.append(b["poly"])
        T, cls.pola, G = teren.zbuduj(d, OS.MODELE); cls.tri = trojkaty(D) + trojkaty(T)
        cls.drzewa, cls.latarnie = teren.punkty(d, G)

    def test_budynki(self):
        self.assertEqual(set(self.opis["wiezowce"]), {"28a", "30a", "32a", "34a"})
        self.assertEqual(set(self.opis["bloki"]), {"28", "30", "32", "34", "36", "38"})
        self.assertIn("bar_34b", self.opis["male"])
        self.assertGreaterEqual(sum(k.startswith("garaze") for k in self.opis["male"]), 7)

    def test_strona_wejsc(self):
        self.assertEqual(self.opis["bloki"]["28"]["front"], "występy klatek w OSM")
        self.assertEqual(self.opis["bloki"]["30"]["front"], "występy klatek w OSM")

    def test_budzet_sceny(self):
        self.assertLess(self.tri, 1500000)          # ZASADY 10.2: scena-obszar <= 1,5 mln

    def test_teren(self):
        for k in ("jezdnie", "parkingi", "chodniki", "trawa", "krawezniki"):
            self.assertGreater(self.pola[k], 100, k)
        self.assertGreater(len(self.latarnie), 30)


if __name__ == "__main__":
    unittest.main()


def test_sklep_w_markecie_i_maska_trawy():
    """23.11 wieczór: hala sklepu (BraxSupermarket.WOsiedlu) w markecie — stałe w BraxBydgoszcz.cs zgadzają się z obrysem OSM;
    maska trawnika dla kęp 3D istnieje i pokrywa osiedle; osiedle włączone, a granica miasta odsunięta za nie."""
    import gzip, re, struct
    import numpy as np
    from shapely.geometry import Polygon, Point
    from shapely.geometry.polygon import orient
    root = Path(__file__).resolve().parents[2]
    d = json.loads((root / "unity-tools/claude/osiedle_bdg/dane/osiedle_osm.json").read_text(encoding="utf-8"))
    b = [x for x in d["budynki"] if "Leclerc" in (x.get("nazwa") or "")][0]
    P = orient(Polygon(b["obrys"]).buffer(0).simplify(.15), 1.0); pts = list(P.exterior.coords); fr = []
    for (x0, z0), (x1, z1) in zip(pts, pts[1:]):
        a = np.array([x0, z0]); bb = np.array([x1, z1]); L = np.linalg.norm(bb - a)
        if L < .3: continue
        t = (bb - a) / L; n = np.array([-t[1], t[0]])
        if P.contains(Point(*((a + bb) / 2 + n * .5))): a, bb, t, n = bb, a, -t, -n
        if n[1] > .5: fr.append((a, bb, t, n, L))
    a, bb, t, n, L = max(fr, key=lambda f: f[4]); f = a + t * L / 2 - n * 4.8
    cs = (root / "unity-city/Assets/Editor/Claude/Region/BraxBydgoszcz.cs").read_text(encoding="utf-8")
    m = re.search(r"SklepFront = new Vector3\((-?[\d.]+)f, .*?, (-?[\d.]+)f\)", cs); yaw = float(re.search(r"SklepYaw = ([\d.]+)f", cs).group(1))
    assert abs(float(m.group(1)) - (-145.9 + f[0] * 1.294)) < .1 and abs(float(m.group(2)) - (508.8 + f[1] * 1.294)) < .1
    assert abs(yaw - np.degrees(np.arctan2(-n[0], -n[1]))) < .2
    assert "public static readonly bool Wlaczone = true;" in cs
    gr = (root / "unity-city/Assets/Claude/Runtime/BraxMiastoGranice.cs").read_text(encoding="utf-8")
    assert "public const float Z1Osiedla = 897f;" in gr       # 24.11: za osiedlem Stary Rynek (Z1 — test_stary_rynek_podpiety)
    raw = gzip.open(root / "unity-tools/claude/osiedle_bdg/gen/osiedle_trawa_maska.bin").read()
    assert raw[:4] == b"BTM1"; x0, z0, krok, y, w, h = struct.unpack("<4f2i", raw[4:28])
    assert len(raw) - 28 == w * h // 8 and krok < .4 and x0 < -500 and z0 < 170 and z0 + h * krok > 880


def test_bus_z_bosna_wlasciciela_na_parkingu_marketu():
    """22:44: model właściciela (bus z bosną) na parkingu przed marketem — punkt w osiedle_punkty.json, paczka szyfrowana
    z celem bydgoszcz_bosna (oryginał tylko w CMS), loader w BraxBydgoszcz.ModeleWlasciciela."""
    import json
    from pathlib import Path
    R = Path(__file__).resolve().parents[2]
    pk = json.loads((R / "unity-tools/claude/osiedle_bdg/gen/osiedle_punkty.json").read_text(encoding="utf-8"))
    m = [w for w in pk.get("modele", []) if w[0] == "bydgoszcz_bosna" and w[1] == "bosna_van"]
    assert len(m) == 1 and abs(m[0][4] ** 2 + m[0][5] ** 2 - 1) < 1e-3
    man = json.loads((R / "unity-city/Prywatne/braxhorn-sklep-bydgoszcz-bosna-01/manifest.json").read_text(encoding="utf-8"))
    assert man["target"] == "bydgoszcz_bosna" and "bydgoszcz_bosna/bosna_van.glb" in man["files"]
    assert not list((R / "unity-city/Prywatne/braxhorn-sklep-bydgoszcz-bosna-01").glob("*.glb"))      # nic jawnego
    cs = (R / "unity-city/Assets/Editor/Claude/Region/BraxBydgoszcz.cs").read_text(encoding="utf-8")
    assert "ModeleWlasciciela(root, Swiat, punkty)" in cs and "Mathf.Atan2(-kz, kx)" in cs and 'Obok(sciezka, przedrostek, "punkty.json")' in cs


def test_bus_z_bosna_okienko_i_miejsce_sprzedawcy():
    """00:16: punkt KUP BOSNĘ przy okienku i miejsce sprzedawcy za ladą (osiedle_akcje.json → BraxBydgoszcz.Akcje)."""
    import json, math
    from pathlib import Path
    R = Path(__file__).resolve().parents[2]
    a = {x["id"]: x for x in json.loads((R / "unity-tools/claude/osiedle_bdg/gen/osiedle_akcje.json").read_text(encoding="utf-8"))["akcje"]}
    assert a["bosna_kup"]["mowca"] == "bosna_sprzedawca" and a["bosna_kup"]["etykieta"] == "KUP BOSNĘ"
    d = math.hypot(a["bosna_kup"]["x"] - a["bosna_sprzedawca"]["x"], a["bosna_kup"]["z"] - a["bosna_sprzedawca"]["z"])
    assert 1.5 < d < 2.5                                            # klient przed ladą, sprzedawca za nią
    cs = (R / "unity-city/Assets/Editor/Claude/Region/BraxBydgoszcz.cs").read_text(encoding="utf-8")
    assert 'Akcje(root, Swiat, Obok(sciezka, przedrostek, "akcje.json"))' in cs and '"osiedle", Out)' in cs   # gen/osiedle_akcje.json


def test_kazdy_mur_i_tynk_z_tekstura():
    """24.11, 01:14: „czy wszędzie każdy mur ma teksturę, cegłę” — każdy materiał ścian (tynki k_, parter kb_, cegły c_,
    kamień s_, mur pruski, wrota) ma teksturę, a każda tekstura palety jest pobierana albo liczona w fetch_region.py."""
    import re
    from pathlib import Path
    R = Path(__file__).resolve().parents[2]
    sys.path.insert(0, str(BDG))
    import paleta
    sciany = [k for k in paleta.P if k.split("_")[0] in ("k", "kb", "c", "s") or k in ("belka_ciemna", "deska_ciemna", "cokol4")]
    assert len(sciany) > 30
    bez = [k for k in sciany if not paleta.P[k].get("tex")]
    assert not bez, bez
    fr = (R / "unity-tools/claude/fetch_region.py").read_text(encoding="utf-8")
    fp = (R / "unity-tools/claude/fetch_polyhaven.py").read_text(encoding="utf-8")
    for k in sciany:
        t = paleta.P[k]["tex"]
        assert f'"{t}"' in fr or f'"{t}"' in fp, (k, t)
    assert paleta.P["k_szarobez"]["tex"] == paleta.P["k_szarobez_dol"]["tex"] == paleta.P["k_wypelnienie"]["tex"] == "tynk_stary"
    assert "tynk_stary" in paleta.SREDNIE and "tynk_plamy" in paleta.SREDNIE
    assert re.search(r'def tynk_plamy\(report', fr) and "tynk_plamy(report)" in fr


@unittest.skipUnless(MA_NUMPY, "brak numpy")
def test_mur_ogrodzeniowy_z_cegly():
    """barrier=wall z OSM to mur z cegły (filary, czapa z betonu), nie płot z prętów — osiedle i Stary Rynek."""
    from pathlib import Path
    sys.path.insert(0, str(BDG))
    import wyposazenie
    from kit import Siatka
    S = Siatka(); wyposazenie.mur(S, [(0, 0), (7, 0), (7, 5)], 2.0, "c_stara")
    mats = {m for (_g, m) in S.dane()}
    assert mats == {"c_stara", "cokol4"}
    R = Path(__file__).resolve().parents[2]
    assert "wyposazenie.mur(" in (R / "unity-tools/claude/rynek_bdg/rynek.py").read_text(encoding="utf-8")


def test_stary_rynek_podpiety_do_gry():
    """24.11 (właściciel: „najpierw Stary Rynek”, potem jeden build): rynek.bin w gen, ten sam punkt zerowy w generatorze,
    pliku i wczytywaczu C#, okno Rynku = granica miasta (X0, RynekX1, Z1) od szwu z osiedlem; figura linoskoczka z paczki
    szyfrowanej; skrót gen w Assets (klucz pamięci rejonu w CI widzi zmianę plików gen)."""
    import gzip, json, re, struct
    from pathlib import Path
    R = Path(__file__).resolve().parents[2]
    rz = json.loads((R / "unity-tools/claude/rynek_bdg/dane/rynek_rzut.json").read_text(encoding="utf-8"))
    K = rz["K"]; gx, gz = rz["gra_a"]
    b = gzip.open(R / "unity-tools/claude/rynek_bdg/gen/rynek.bin").read(1 << 16)
    assert b[:4] == b"BYD2"
    n = struct.unpack("<i", b[4:8])[0]; nag = b[8:8 + n].decode("utf-8")
    assert re.search(r'"punkt_zerowy_gry":\s*\[\s*%s,\s*%s\s*\]' % (re.escape(repr(gx)), re.escape(repr(gz))), nag), nag[:200]
    cs = (R / "unity-city/Assets/Editor/Claude/Region/BraxBydgoszcz.cs").read_text(encoding="utf-8")
    assert "PunktZerowyRynku = new Vector2(%sf, %sf)" % (gx, gz) in cs and 'rynek_bdg/gen/rynek.bin' in cs and "Wczytaj(cityRoot, PlikRynku" in cs
    gr = (R / "unity-city/Assets/Claude/Runtime/BraxMiastoGranice.cs").read_text(encoding="utf-8")
    liczba = lambda nazwa: float(re.search(r"\b%s = (-?[\d.]+)f" % nazwa, gr).group(1))
    assert abs(liczba("X0") - (gx - 420 * K)) < .1                        # zachodnia krawędź okna = granica miasta
    assert abs(liczba("RynekX1") - (gx + 160 * K)) < .1
    assert abs(liczba("Z1") - (gz + 160 * K)) < .1                        # północ okna = nowa granica
    assert abs(liczba("Z1Osiedla") - (508.8 + 300 * K)) < .1               # szew = północ okna osiedla
    assert "Rect.MinMaxRect(RynekX1, Z1Osiedla, OsiedleX1, Z1)" in gr and "Rect.MinMaxRect(X0, Z1Miasta, OsiedleX0, Z1Osiedla)" in gr
    pk = json.loads((R / "unity-tools/claude/rynek_bdg/gen/rynek_punkty.json").read_text(encoding="utf-8"))
    assert list(pk)[-1] == "modele"                                       # wczytywacz czyta listę modeli do „]]”
    fig = [m for m in pk["modele"] if m[0] == "bydgoszcz_linoskoczek"]
    assert len(fig) == 1 and 5 < fig[0][6] < 12                            # na linie, kilka metrów nad wodą
    kx, kz = pk["linoskoczek_kier"]; assert abs(fig[0][4] * kx + fig[0][5] * kz) < 1e-3   # drąg w poprzek liny
    pak = R / "unity-city/Prywatne/braxhorn-sklep-bydgoszcz-linoskoczek-01"
    man = json.loads((pak / "manifest.json").read_text(encoding="utf-8"))
    assert man["target"] == "bydgoszcz_linoskoczek" and "bydgoszcz_linoskoczek/linoskoczek.glb" in man["files"]
    assert not list(pak.glob("*.glb"))
    assert pk["bariery"] and all(len(x) == 6 for x in pk["bariery"])
    sys.path.insert(0, str(R / "unity-tools/claude"))
    import bydgoszcz_skrot
    assert json.loads(bydgoszcz_skrot.CEL.read_text(encoding="utf-8")) == bydgoszcz_skrot.skrot(), "uruchom bydgoszcz_skrot.py po skopiowaniu gen"


def test_build_45304516914_poprawki():
    """Build 24.11 (run 45304516914): osiedle i Stary Rynek nie weszły do świata — CreateAsset w nieistniejącym folderze
    ClaudeRegion/Bydgoszcz (wyjątek na pierwszej siatce), a tekstury pochodne (lakier_blacha, tynki) nie powstały, bo w jobie
    cc0-assets nie było numpy. Pilnujemy: folder przed zapisem, osobne części (błąd osiedla nie zatrzymuje Rynku), numpy w CI."""
    from pathlib import Path
    R = Path(__file__).resolve().parents[2]
    cs = (R / "unity-city/Assets/Editor/Claude/Region/BraxBydgoszcz.cs").read_text(encoding="utf-8")
    assert "static void Folder(string sciezka)" in cs and "AssetDatabase.CreateFolder(rodzic, nazwa)" in cs
    assert cs.index("Folder(katalogSiatek);") < cs.index("BraxZapisAssetu.Utworz(mesh")
    assert 'Bezpiecznie("osiedle"' in cs and 'Bezpiecznie("Stary Rynek"' in cs
    wf = (R / ".github/workflows/claude-region-build.yml").read_text(encoding="utf-8")
    i = wf.index("python3 unity-tools/claude/fetch_region.py"); krok = wf[wf.rindex("- name:", 0, i):i]
    assert "numpy" in krok


def test_balkon_dj_na_rynku():
    """24.11, 14:09 (właściciel: balkon na zaznaczonej kamienicy, sprzęt DJ, głośniki i muzyka leci): wpis muzyki w punktach
    Rynku (utwór istnieje, plac przed kamienicą, dwie kolumny na wysokości balkonu), C# tworzy BraxClaudeMuzyka i puls diod,
    diody świecą (emisja), baner z naklejką w Assets, drzwi balkonowe tylko na ścianie od Rynku."""
    import json
    from pathlib import Path
    R = Path(__file__).resolve().parents[2]
    pk = json.loads((R / "unity-tools/claude/rynek_bdg/gen/rynek_punkty.json").read_text(encoding="utf-8"))
    assert list(pk)[-1] == "modele" and len(pk["muzyka"]) == 1
    m = pk["muzyka"][0]
    assert (R / "unity-city/Assets/Claude/Audio" / m[0]).exists() and len(m) == 24
    # 14:40 lasery: dwie głowice na balustradzie (wysokość balkonu + ok. 1,1 m), kierunek frontu jednostkowy
    assert 9 < m[17] < 11 and 9 < m[20] < 11 and abs(m[22] ** 2 + m[23] ** 2 - 1) < 1e-3
    x0, z0, x1, z1 = m[1:5]; assert x1 - x0 > 20 and z1 - z0 > 20                  # plac przed kamienicą
    assert 9 < m[8] < 11 and 9 < m[11] < 11                                         # kolumny na balkonie II piętra
    assert m[13] == "dj_led" and 120 < m[14] < 150
    import sys; sys.path.insert(0, str(R / "unity-tools/claude/osiedle_bdg")); import paleta
    assert paleta.P["dj_led"]["emisja"] > 0 and paleta.P["dj_baner"]["nakl"] == "dj_baner.png"
    assert (R / "unity-city/Assets/Source/Claude/Bydgoszcz/dj_baner.png").exists()
    cs = (R / "unity-city/Assets/Editor/Claude/Region/BraxBydgoszcz.cs").read_text(encoding="utf-8")
    assert "Muzyka(root, Swiat, punkty, maty, katalogSiatek)" in cs and "AddComponent<BraxClaudeMuzyka>()" in cs and "BraxPulsSwiatla" in cs
    assert "d.emisja > 0f" in cs and 'mat.StartsWith("dj_")' in cs
    assert (R / "unity-city/Assets/Claude/Runtime/BraxPulsSwiatla.cs").exists()
    las = (R / "unity-city/Assets/Claude/Runtime/BraxLaseryBalkonu.cs").read_text(encoding="utf-8")
    assert "JestNoc()" in las and "slonceNoc" in las and "AddComponent<BraxLaseryBalkonu>()" in cs and "Braxhorn/WnetrzaPromien" in cs
    assert (R / "unity-city/Assets/Claude/Shaders/BraxWnetrzaPromien.shader").exists()


def test_klucz_rejonu_bez_pokwitowan_pobierania(tmp_path):
    """24.11 (run 45369837017): drugi przebieg z tego samego commita dostał inny klucz rejonu, bo pokwitowania pobierania
    tekstur (region-fetch.json, polyhaven-receipt.json) zapisują stan przebiegu („ok”/„already”) — zapisany świat nie
    pasował. Klucz nie może zależeć od ich treści (etap 1 i etap 2 muszą mieć ten sam klucz)."""
    import importlib.util
    from pathlib import Path
    spec = importlib.util.spec_from_file_location("rejon_buildu_t", Path(__file__).resolve().parent / "rejon_buildu.py")
    rb = importlib.util.module_from_spec(spec); spec.loader.exec_module(rb)
    uc = tmp_path / "unity-city"; ph = uc / "Assets/Source/Claude/PolyHaven"; ph.mkdir(parents=True)
    (ph / "bricks/diffuse.jpg").parent.mkdir(); (ph / "bricks/diffuse.jpg").write_bytes(b"jpg")
    for n in ("region-fetch.json", "polyhaven-receipt.json"): (ph / n).write_text('{"textures": {"a": "ok", "b": 123}}')
    k1 = rb.klucz(uc, "miasto", "android", "6000.0.58f2")
    for n in ("region-fetch.json", "polyhaven-receipt.json"): (ph / n).write_text('{"textures": {"a": "already"}}')
    assert rb.klucz(uc, "miasto", "android", "6000.0.58f2") == k1
    (ph / "bricks/diffuse.jpg").write_bytes(b"jpg2")                      # prawdziwe wejście dalej zmienia klucz
    assert rb.klucz(uc, "miasto", "android", "6000.0.58f2") != k1
