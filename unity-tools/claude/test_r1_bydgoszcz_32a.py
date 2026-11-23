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
    assert "public const float Z1 = 900f;" in gr
    raw = gzip.open(root / "unity-tools/claude/osiedle_bdg/gen/osiedle_trawa_maska.bin").read()
    assert raw[:4] == b"BTM1"; x0, z0, krok, y, w, h = struct.unpack("<4f2i", raw[4:28])
    assert len(raw) - 28 == w * h // 8 and krok < .4 and x0 < -500 and z0 < 170 and z0 + h * krok > 880
