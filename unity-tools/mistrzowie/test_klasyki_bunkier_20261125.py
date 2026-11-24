"""The owner's Polish classics and their bunker under the quad track's hill (owner 24.11.2026 22:21-23:13).

    python3 -B -m pytest -p no:cacheprovider unity-tools/mistrzowie/test_klasyki_bunkier_20261125.py
"""
import json
import re
import sys
import unittest
from pathlib import Path

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"
KL = ROOT / "unity-tools/mistrzowie/klasyki"
PACK = ROOT / "unity-city/Prywatne/braxhorn-sklep-klasyki-01"
sys.path.insert(0, str(KL))


def bez_komentarzy(src):
    return "\n".join(l.split("//")[0] for l in src.splitlines())


class Narzedzie(unittest.TestCase):
    def setUp(self):
        import przygotuj_klasyki as PK
        import przepisy
        self.PK, self.P = PK, przepisy

    def test_kazdy_pojazd_ma_przepis_i_wymiary(self):
        self.assertEqual(len(self.PK.MODELE), 16)
        ids = [m["id"] for m in self.PK.MODELE.values()]
        self.assertEqual(len(ids), len(set(ids)))
        for name, m in self.PK.MODELE.items():
            self.assertIn(name, self.P.PRZEPISY, name)
            self.assertIn(m["typ"], ("auto", "ciezarowka", "motor"))
            self.assertIn(m["os"], ("x", "z"))
            # real lengths: a moped 1.7 m to a Star truck 6.5 m
            self.assertTrue(1.7 <= m["dl"] <= 6.6, name)
            if m["typ"] != "motor": self.assertIn(m["szyby"], ("malowane", "otwarte"), name)

    def test_tablice_z_epoki(self):
        """1976-2000: private 3 letters + 4 digits, state 3 letters + 3 digits + a letter, the militia MO + 4 digits; the
        food van of today its current plate"""
        src = (KL / "przepisy.py").read_text()
        tabl = re.findall(r'(?:tablice\(\w+, |\.tablica\("[LPFT]", )"([A-Z0-9 ]+)"', src)
        self.assertGreaterEqual(len(tabl), 14)
        for t in tabl:
            self.assertRegex(t, r"^([A-Z]{3} \d{4}|[A-Z]{3} \d{3}[A-Z]|MO \d{4}|[A-Z]{3} \d[A-Z]\d{2})$", t)
        self.assertIn('"MO 4052"', src); self.assertIn('"MO 7846"', src); self.assertIn('"MO 1261"', src)

    def test_napisy_montserrat_bez_logo(self):
        import tekstura as T
        self.assertTrue(all(f.exists() for f in T.FONT_FILES))
        self.assertIn("Montserrat-SemiBold", T.FONT_FILES[0].name)
        # the ogonek of Ą stays inside the letters' picture (it was cut at 1.0 em)
        _, _, h_a = T.tekst_rgba("GORACE", .1, (255, 255, 255)); _, _, h_o = T.tekst_rgba("GORĄCE", .1, (255, 255, 255))
        self.assertGreater(h_o, h_a + .1 * .12)
        # brand names only as plain words: no logo pictures are laid, only text and plates
        src = (KL / "przepisy.py").read_text()
        self.assertNotRegex(src, r"\.png|\.jpg|logo\(")

    def test_szyby_i_kola(self):
        src = (KL / "przygotuj_klasyki.py").read_text()
        self.assertIn("def szyby_malowane", src); self.assertIn("def szyba_otwarta", src)
        self.assertIn("convexHull", src)                                     # a window's outline straight, not ragged
        self.assertIn("1.01, 1.04", src)                                     # the mudguard over the hub stays on the body
        self.assertIn('"kolo_%s_%s"', src)
        # parts without pictures inside or linked (Unity reads no texture per part)
        z = src[src.index("def zapisz_glb"):src.index("# ------", src.index("def zapisz_glb"))]
        self.assertNotIn('"images"', z); self.assertNotIn("baseColorTexture", z)


class Paczka(unittest.TestCase):
    def test_paczka_zaszyfrowana(self):
        m = json.loads((PACK / "manifest.json").read_text())
        self.assertEqual(m["target"], "pojazdy_klasyki_01")
        pliki = sorted(p.name for p in PACK.iterdir())
        self.assertTrue(all(n.startswith("part-") and n.endswith(".cms") or n in ("manifest.json", "recipient.crt", "recipient.pem.enc") for n in pliki), pliki)
        for part in m["parts"]: self.assertLessEqual(part["bytes"], 90 * 1024 * 1024)
        lib = [f for f in m["files"] if f.startswith("pojazdy_klasyki_01/")]
        self.assertIn("pojazdy_klasyki_01/katalog.json", lib)
        self.assertEqual(len([f for f in m["files"] if f.startswith("oryginal/")]), 16)
        for model in ("maluch", "syrena", "fiat125p", "polonez", "star_zielony", "junak"):
            self.assertIn("pojazdy_klasyki_01/klasyk_%s_korpus.glb" % model, lib)
            self.assertIn("pojazdy_klasyki_01/_tekstury/klasyk_%s_kolor.jpg" % model, lib)
        # nothing of the owner's beside the pack in the clear
        self.assertFalse(list((ROOT / "unity-city/Assets/Source/Prywatne").glob("**/klasyk_*")) if (ROOT / "unity-city/Assets/Source/Prywatne").exists() else [])


class Unity(unittest.TestCase):
    def setUp(self):
        self.kl = (ED / "BraxChampionsKlasyki.cs").read_text()
        self.bu = (ED / "BraxChampionsBunkier.cs").read_text()
        self.tor = (ED / "BraxChampionsTorKladow.cs").read_text()
        self.las = (ED / "BraxChampionsLasKladow.cs").read_text()

    def test_klasyki_jezdza(self):
        k = bez_komentarzy(self.kl)
        self.assertIn('Paczka = "pojazdy_klasyki_01"', k)
        self.assertIn("AddComponent<BraxChampionsCar>()", k)
        self.assertIn("car.rollingWheels = toczace.ToArray(); car.steeringWheels = skretne.ToArray();", k)
        self.assertIn('_Cull", 0f', k)                                       # Tripo's thin shells: both sides
        self.assertIn("SzybaCiemna", k); self.assertIn("SzybaJasna", k)
        for f in ("BraxChampionsKlasyki.cs.meta", "BraxChampionsBunkier.cs.meta"): self.assertTrue((ED / f).exists())

    def test_bunkier_w_torze(self):
        t = bez_komentarzy(self.tor)
        self.assertIn("BraxChampionsBunkier.Zbuduj(g);", t)
        self.assertLess(t.index("BraxChampionsBunkier.Zbuduj(g);"), t.index('W.Zapisz("mistrzowie_tor_kladow")'))
        self.assertIn("BraxChampionsBunkier.Grunt(x, z, h)", t)
        self.assertIn("BraxChampionsBunkier.Dziura(", t)
        self.assertIn("BraxChampionsBunkier.Strefa(x, z)", t)
        self.assertIn("BraxChampionsBunkier.Wolne(x, z)", bez_komentarzy(self.las))

    def test_bunkier_wymiary_i_bez_migotania(self):
        b = bez_komentarzy(self.bu)
        self.assertIn("Tunel = 6f, TunelH = 4.5f", b)
        self.assertRegex(b, r"HallX0 = -445f, HallX1 = -393f, HallZ0 = 797f, HallZ1 = 827f, HallH = 6f")
        # the cut's floor ends at the headwall's face (the tunnel's floor from there): never two floors on one plane
        self.assertIn("q >= sF - .4f", b)
        self.assertIn("tunelX = p[i1].x;", b); self.assertIn("px = tunelX", b); self.assertIn("xk = HallX0 - sc", b)
        # the painted lines stand 2 cm proud without a bottom face
        for m in re.finditer(r"W\.OBox\(linie,[^;]*;", b): self.assertTrue(m.group(0).rstrip(";").endswith("false, true)"), m.group(0))
        # the caps 10 cm under the ground where the terrain's cells are kept over them
        self.assertIn("Podloga + .3f) - .1f", b)
        # the hall's roof under the ground: the floor lowered if it would not be
        self.assertIn("nad - (HallH + Strop + 1.5f)", b)

    def test_droga_nie_przecina_toru(self):
        """the forest road's knots keep 10 m from the woods' trail (its corners) and inside the west fence (x -504)"""
        rogi = [tuple(map(float, r)) for r in re.findall(r"new Vector4\((-?[\d.]+)f, (-?[\d.]+)f", self.tor)]
        wezly = [tuple(map(float, w)) for w in re.findall(r"new Vector2\((-?[\d.]+)f, (-?[\d.]+)f\)", self.bu[self.bu.index("Wezly ="):self.bu.index("};", self.bu.index("Wezly ="))])]
        self.assertGreaterEqual(len(wezly), 6)
        for (x, z) in wezly:
            self.assertGreater(x, -504 + 2.5 + 2)
            for (rx, rz) in rogi:
                self.assertGreater(((x - rx) ** 2 + (z - rz) ** 2) ** .5, 10.0, (x, z, rx, rz))


if __name__ == "__main__":
    unittest.main()
