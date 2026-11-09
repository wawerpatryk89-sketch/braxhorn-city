"""MIASTO R1 — mieszkanie Braxa nad NOVA (właściciel 9.11 17:12: „Teraz mieszkanie Brax”; zdjęcia APK 9.11).

APK 9.11: „okna z elewacji 0 — ściana od ulicy pełna” (mieszkanie bez okien), ściany jak surowy beton (tekstura tynku z
plamami), szachownica płytek, stół 2.6 m z obrusem wyglądający jak łóżko w kuchni, zegar za łańcuchem worka, czarna tablica
zamiast TV. Test pilnuje zachowania naprawy (źródło, bez Unity i APK).
"""
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
REG = ROOT / "unity-city/Assets/Editor/Claude/Region"
FLAT = re.sub(r'//[^\n]*', "", (REG / "BraxRegionBraxFlat.cs").read_text(encoding="utf-8"))
FACADE = re.sub(r'//[^\n]*', "", (REG / "BraxUlicePierzeje.cs").read_text(encoding="utf-8"))


class R1MieszkanieBraxa(unittest.TestCase):
    def test_windows_come_from_the_facade_generator(self):
        self.assertIn("public static Rect[] OknaMieszkaniaNOVA", FACADE)
        self.assertIn('f.b.id == "nadbudowa_NOVA"', FACADE)
        self.assertIn("oknaMieszkaniaNOVA.Clear()", FACADE)
        self.assertIn("BraxUlicePierzeje.OknaMieszkaniaNOVA", FLAT)

    def test_walls_are_painted_not_concrete_textures(self):
        self.assertNotIn('"peeling_painted_wall"', FLAT)
        self.assertNotIn('"decrepit_wallpaper"', FLAT)
        self.assertNotIn('"worn_cracked_plaster"', FLAT)
        self.assertIn('var paint = BraxRegion.Fitting("Mieszkanie: farba ścian', FLAT)
        self.assertIn('var plaster = BraxRegion.Fitting("Klatka: farba ścian', FLAT)

    def test_no_checkerboard_tiles_and_no_deck_floor_first(self):
        self.assertNotIn('"square_tiles"', FLAT)
        self.assertIn('"marble_01"', FLAT)
        self.assertIn('M("Mieszkanie: parkiet dębowy", 1.4f, null, "wood_floor"', FLAT)

    def test_dining_table_fitted_to_four_person_size(self):
        self.assertIn("Dopasuj(stol, new Vector3(tx, y, tz), 1.4f * k, .76f * k)", FLAT)
        body = FLAT[FLAT.index("static void Dopasuj("):]
        self.assertIn("maxDl / Mathf.Max(.01f, Mathf.Max(b.size.x, b.size.z))", body)

    def test_clock_not_behind_bag_chain(self):
        bag = re.search(r"Bag\(A, new Vector3\(([\d.]+)f, Ceil, ([\d.]+)f\)\)", FLAT)
        clock = re.search(r'Prop\(A, "Salon: zegar", new Vector3\(([^,]+), y \+ 2\.0f, ([\d.]+)f\)', FLAT)
        self.assertIsNotNone(bag)
        self.assertIsNotNone(clock)
        self.assertIn("XW", clock.group(1))                     # zegar na ścianie W1, nie na W3 za workiem

    def test_tv_is_a_screen_not_a_black_board(self):
        self.assertIn("EkranTV(A,", FLAT)
        self.assertIn("listwa dźwiękowa", (REG / "BraxRegionBraxFlat.cs").read_text(encoding="utf-8"))

    def test_bathroom_and_bedroom_have_working_doors(self):
        # 9.11 17:20: „drzwi do łazienki i pokoju drugiego” — stałe skrzydło w połowie otworu → BraxCityDoor
        self.assertIn('PokojDrzwi(world, A, "BRAX_FLAT_BATH", "ŁAZIENKA"', FLAT)
        self.assertIn('PokojDrzwi(world, A, "BRAX_FLAT_BEDROOM", "SYPIALNIA"', FLAT)
        self.assertIn("Oscieznica(A, dab, chrom, ZH, bd0, bd1, true, Fl, 2.1f * k * .97f, false)", FLAT)
        self.assertIn("Oscieznica(A, dab, chrom, XS + .05f, sd0, sd1, false, Fl, 2.1f * k * .97f, false)", FLAT)
        body = FLAT[FLAT.index("static void PokojDrzwi("):]
        self.assertIn("AddComponent<BraxCityDoor>()", body)
        self.assertIn("world.doors = world.doors.Concat", body)

    def test_mirrors_have_reflection_probes(self):
        self.assertIn("AddComponent<ReflectionProbe>()", FLAT)


if __name__ == "__main__":
    unittest.main()
