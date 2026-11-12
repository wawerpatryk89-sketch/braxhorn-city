"""MIASTO R1 (12.11.2026): wyspa — drzwi działające (zamknięte, na zawiasach) i WC plażowe premium (właściciel 13:51, 15:27).

13:51 „WC stoją na chodnikach… wszystko przejrzyj”; 15:27 „Co z tymi drzwiami do WC — rozpierdolone” (skrzydła stały na sztywno
uchylone pod kątem); 15:29 „skoro gdzieś tam stworzyłeś lustro, to dlaczego go nie przeniesiesz” — lustra, miski, pisuary
i umywalki to ta sama wspólna biblioteka R1 (BraxCeramika / BraxLustro), co w WC dworca.
"""
import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
REG = ROOT / "unity-city/Assets/Editor/Claude/Region"
ARENA = (REG / "BraxMiastoWyspaArena.cs").read_text(encoding="utf-8")
DOMKI = (REG / "BraxMiastoWyspaDomki.cs").read_text(encoding="utf-8")


def fn(src, name, nxt):
    i = src.index(name)
    return src[i:src.index(nxt, i)]


class DrzwiDzialajace(unittest.TestCase):
    D = fn(ARENA, "internal static void DrzwiObrotowe(", "static void ApSufitPom(")

    def test_skrzydlo_na_zawiasie_z_braxcitydoor(self):
        self.assertIn("AddComponent<BraxCityDoor>()", self.D)
        self.assertIn("d.leaves = new[] { z }; d.sides = new[] { dir };", self.D)
        self.assertIn("AddComponent<BoxCollider>()", self.D)          # kolizja jedzie ze skrzydłem
        self.assertIn('d.id = "wyspa_drzwi_" + BraxUliceKit.Bezpieczna(label)', self.D)

    def test_klamki_po_obu_stronach(self):
        self.assertIn("foreach (float sz in new[] { -1f, 1f })", self.D)
        self.assertIn("kl.Rura(", self.D)

    def test_skrzydla_areny_zamkniete_w_osi_sciany(self):
        sk = fn(ARENA, "static void ApSkrzydlo(", "static void ApSufitPom(")
        self.assertIn("DrzwiObrotowe(", sk)
        self.assertNotIn("Quaternion.Euler(0, kier", sk)                # dawne skrzydło uchylone na sztywno

    def test_kabiny_wc_areny(self):
        t = fn(ARENA, "static void ApToalety()", "// TUNEL ZAWODNIKÓW")
        self.assertIn('DrzwiObrotowe(nz + " kabina " + (k + 1)', t)


class WcPlazowe(unittest.TestCase):
    W = fn(DOMKI, "// WC PLAŻOWE PREMIUM", "// WYPOŻYCZALNIA")

    def test_pawilon_za_deptakiem_z_dojsciem(self):
        self.assertIn("var c = Zaplecze(ang, 50f)", self.W)
        self.assertIn('"Dojście do WC plażowego"', self.W)

    def test_ceramika_i_lustro_wspolne_r1(self):
        for s in ("BraxCeramika.MiskaWiszaca(", "BraxCeramika.Pisuar(", "BraxCeramika.ZestawUmywalkowy("):
            self.assertIn(s, self.W)

    def test_materialy(self):
        for slug in ('"long_white_tiles"', '"large_floor_tiles_02"', '"teak_veneer"', '"granite_tile"'):
            self.assertIn(slug, self.W)
        self.assertNotIn("zlot", self.W.lower())

    def test_drzwi_dzialajace(self):
        self.assertEqual(self.W.count("DrzwiObrotowe("), 2)             # wejście (×2 w pętli) i kabina (×2 w pętli)

    def test_zero_migotania(self):
        self.assertIn("Bx(posW, -hw2 + tw, hw2 - tw, -.03f, .025f, -hd2 + tw, hd2 + .03f);", self.W)   # posadzka zatopiona w cokole
        self.assertIn("hd2 + .6f, hd2 + .63f);", self.W)                                              # opaska 3 cm przed płytą dachu


if __name__ == "__main__":
    unittest.main()
