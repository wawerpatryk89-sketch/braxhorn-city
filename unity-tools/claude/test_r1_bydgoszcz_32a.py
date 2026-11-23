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


if __name__ == "__main__":
    unittest.main()
