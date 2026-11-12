"""MIASTO R1 (12.11.2026): domki na wyspie od środka i od zewnątrz — uwagi właściciela 13:14–14:05.

13:14 „wchodzisz do środka i jakby domka nie było” — ściany miały tylko lico zewnętrzne, bez sufitu, płyta cokołu przykrywała
podłogę. 14:04 „Dlaczego bierzesz takie chujowe poduszki? … twój kolega stworzył … czemu nie korzystasz”, „Czemu okno nie jest
oknem?”, 14:05 „Domki powinny być drewniane, w środku lamele”, „Zakaz poduszek kwadratowych”.
"""
import pathlib
import re
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
REG = ROOT / "unity-city/Assets/Editor/Claude/Region"
DOMKI = (REG / "BraxMiastoWyspaDomki.cs").read_text(encoding="utf-8")
MEBLE = (REG / "BraxMeble.cs").read_text(encoding="utf-8")
FETCH = (ROOT / "unity-tools/claude/fetch_region.py").read_text(encoding="utf-8")


class WnetrzeDomku(unittest.TestCase):
    def test_sciany_od_srodka_sufit_podloga(self):
        self.assertIn("static void Wnetrze(", DOMKI)
        self.assertIn('"Wyspa: domek wnętrze — boazeria sosnowa", 1.6f', DOMKI)
        self.assertIn('"hinoki_planks"', DOMKI)
        self.assertIn('"Wyspa: domek wnętrze — sufit z białych desek"', DOMKI)
        self.assertIn('"Wyspa: domek podłoga — deska dębowa szeroka", 2.4f', DOMKI)
        # listwa cokołowa jako pierścień na zewnątrz ścian (nie płyta przykrywająca podłogę od środka)
        self.assertNotIn("new Vector3(w + .06f, .14f, d + .06f)", DOMKI)

    def test_lamele_za_lozkiem(self):
        self.assertIn('"Wyspa: domek — filc pod lamelami (czarny)"', DOMKI)
        self.assertIn("xx += .042f", DOMKI)


class Poduszki(unittest.TestCase):
    def test_zakaz_kwadratowych_poduszek(self):
        # poduszki i poduchy tylko miękkie (superkwadryka), nigdy Box
        lozko = MEBLE[MEBLE.index("public static void Lozko("):MEBLE.index("public static void SzafkaNocna(")]
        self.assertGreaterEqual(lozko.count("Miekki("), 4)
        for linia in lozko.splitlines():
            if "podusz" in linia.lower() and "Miekki(" not in linia and not linia.strip().startswith("//"):
                self.assertNotRegex(linia, r"\bBox\(", linia)
        self.assertNotRegex(DOMKI, r"B\(poscielM, [^;]*\);\s*// poduszki")

    def test_kopia_od_kolegi(self):
        self.assertIn("BraxChampionsHotelSobczakT03.Soft", MEBLE)
        self.assertIn("BraxChampionsG1Meble.HBed", MEBLE)
        self.assertIn("BraxMeble.Lozko(S, new BraxMeble.Rama(", DOMKI)
        self.assertIn("BraxMeble.Krzeslo(", DOMKI)
        self.assertIn("BraxMeble.Sofa(", DOMKI)


class Okno(unittest.TestCase):
    def test_szyba_przezroczysta_rama_czarna(self):
        self.assertIn("var szyba = BraxUliceKit.SzkloPrzezroczyste;", DOMKI)
        self.assertNotIn("szyba domku (przydymiona", DOMKI)
        self.assertIn('"Wyspa: domek ramy okien (czarne aluminium)"', DOMKI)
        self.assertNotIn("z[obrob].Box(L(o.center.x, o.center.y, g)", DOMKI)

    def test_parapet_klamki_zaslony(self):
        self.assertIn("BraxMeble.Zaslona(z[zaslonaM]", DOMKI)
        self.assertIn('"hessian_230"', DOMKI)


class Drewno(unittest.TestCase):
    def test_elewacja_z_deski(self):
        self.assertIn('"Wyspa: domek elewacja — deska malowana " + paleta, 1.2f, Farba(pal.sciana)', DOMKI)
        self.assertIn('"white_planks_clean"', DOMKI)

    def test_tekstury_pobierane_w_ci(self):
        m = re.search(r"TEXTURES_WYSPA = \[(.*?)\]", FETCH, re.S)
        self.assertIsNotNone(m)
        for slug in ("white_planks_clean", "hinoki_planks", "oak_wood_planks", "roof_slates_02", "waffle_pique_cotton",
                     "wool_boucle", "hessian_230", "coast_sand_02"):
            self.assertIn('"%s"' % slug, m.group(1))
        self.assertIn("TEXTURES_DWORZEC + TEXTURES_WYSPA", FETCH)


class ZeroKlockow(unittest.TestCase):
    def test_meble_zaokraglone(self):
        self.assertIn("public static void Zaokr(", MEBLE)
        self.assertIn("static void Aneks(", DOMKI)
        # stare klockowe łóżko (rama/materac/narzuta jako Box) usunięte
        self.assertNotIn("// narzuta (zwisa na boki)", DOMKI)
        self.assertNotIn("// siedzisko sofy", DOMKI)


if __name__ == "__main__":
    unittest.main()
