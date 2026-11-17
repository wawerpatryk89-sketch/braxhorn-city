"""Owner 17.11.2026 ("A tą willę trzeba konkretniejszą zrobić", "każda ma być inna, zobacz na internecie", "Te ściany są
wykończone?"): WILLA 1 on the hill rebuilt after a real house - 1471 Forest Knoll, Los Angeles (Standard Architecture):
three parallel white volumes under gabled dark metal roofs, the double-height central hall panelled in wood, glazed gable
ends in big steel frames, the stairs in lower links between them, a garage with a bedroom over it, a cantilevered
infinity pool. The facades in the scanned exterior stucco."""
import math
import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"
PLAN = (ED / "BraxChampionsWillaWzgorze.cs").read_text(encoding="utf-8")
KIT = (ED / "BraxChampionsWilleKit.cs").read_text(encoding="utf-8")
VILLA = (ED / "BraxChampionsWille.cs").read_text(encoding="utf-8")
SITES = (ED / "BraxChampionsWilleDzialki.cs").read_text(encoding="utf-8")


class WillaWzgorze(unittest.TestCase):
    def test_the_hill_villa_is_its_own_plan(self):
        self.assertIn("public static partial class BraxChampionsWille", VILLA)
        self.assertIn("if (nr == 1) PlanWzgorze();", VILLA)
        self.assertIn("1471 Forest Knoll, Los Angeles (Standard Architecture", PLAN)
        for f in ("BraxChampionsWilleKit.cs.meta", "BraxChampionsWillaWzgorze.cs.meta"):
            self.assertTrue((ED / f).exists(), f)
        proj = (ROOT / "unity-tools/mistrzowie/wille_preview/wille_preview.csproj").read_text()
        self.assertIn("BraxChampionsWilleKit.cs", proj)
        self.assertIn("BraxChampionsWillaWzgorze.cs", proj)

    def test_three_gabled_volumes_and_the_garage(self):
        self.assertIn("const float AE = 6.6f, AF1 = 3.6f, APitch = 30f;", PLAN)
        for roof in ("Dach2(-16f, -8f, -8f, 14f, AE, APitch);", "Dach2(-5f, 5f, -10f, 14f, AE, APitch, soffit);",
                     "Dach2(8f, 16f, -8f, 14f, AE, APitch);", "Dach2(16f, 26f, 4f, 14f, AE, APitch);"):
            self.assertIn(roof, PLAN)
        # the central hall ~9.5 m to the ridge (Forest Knoll: 30 ft); the side volumes lower
        self.assertAlmostEqual(6.6 + 5 * math.tan(math.radians(30)), 9.49, delta=.01)
        self.assertLess(6.6 + 4 * math.tan(math.radians(30)), 9.0)
        self.assertIn("SzczytSzyba(-5f, 5f, AE, rC, -9.85f);", PLAN)
        self.assertEqual(PLAN.count("RamaSzczytu("), 2)          # C's own and the one in AFront (W and E)
        self.assertEqual(PLAN.count('BramaGarazu("brama'), 3)
        self.assertIn("standing-seam", KIT)
        # the links' flat roofs stay under the eaves' overhang (6.6 - .35 tan 30 = 6.40)
        self.assertLess(6.32, 6.6 - .35 * math.tan(math.radians(30)))

    def test_finished_walls(self):
        # owner 17.11 "Te ściany są wykończone?": the scanned exterior stucco, no painted inner skin in the facade
        self.assertIn('elewacja = BraxChampionsFoundation.SourceMaterial("willa_tynk_elewacja", "white_stucco_02"', VILLA)
        self.assertIn("static Vector4 RogZ(float z0, float z1, bool przodSzklo)", KIT)
        self.assertIn("static Vector4 RogX(float x0, float x1)", KIT)
        self.assertNotIn("ScianaO(elewacja", PLAN.replace("ScianaOE(elewacja", ""))
        self.assertIn("the wall as a grid of cells on every opening's edges", KIT)
        self.assertIn("static void MMd(Material m", KIT)
        self.assertIn('BraxChampionsFoundation.SourceMaterial("willa_tynk_elewacja"', VILLA)

    def test_the_cantilevered_pool(self):
        self.assertIn("APoolX0 = -10f, APoolX1 = 10f, APoolZ0 = -25.6f, APoolZ1 = -19.6f;", PLAN)
        pad_front = -22.6
        self.assertAlmostEqual(pad_front - -25.6, 3.0)                     # 3 m past the pad's edge
        self.assertIn('BasenWiszacy("willa_1_basen", APoolX0, APoolX1, APoolZ0, APoolZ1);', PLAN)
        self.assertIn("MM(poolTile, x0, x1, pf, Water - .015f, z0 - sk, z0);", KIT)  # the infinity edge 15 mm under the water
        self.assertIn("sw.basinId = id;", KIT)
        # the site keeps the ground under each villa's own pool
        self.assertIn("var pool = BraxChampionsWille.StrefaBasenu(d.nr);", SITES)
        self.assertIn("nr == 1 ? new Vector3(APoolX0, APoolX1, APoolZ1)", PLAN)
        # the retaining wall's foot under the site's under-pad ground (-4.2)
        self.assertIn("MM(stone, PadX0, APoolX0 - .33f, -4.5f, -s, PadZ0, PadZ0 + .3f);", PLAN)

    def test_every_room(self):
        for room in ('tag + " kuchnia"', 'tag + " kino"', 'tag + " salon TV"', '"sypialnia główna"', '"apartament gościnny"',
                     '"łazienka 1"', '"łazienka 2"', '"łazienka główna"', '"łazienka gościnna"', 'Drzwi("WC"', '" siłownia lustro"', 'tag + " pralnia"'):
            self.assertIn(room, PLAN)
        self.assertEqual(PLAN.count("Schody(floorGf"), 1)                 # in the loop over both links
        self.assertIn("W.Rampa(", KIT)
        self.assertIn('TablicaSprzedazy(1, new Vector3(2.0f, 0f, 23.6f), Vector3.forward,', PLAN)
        self.assertIn("garaż na trzy auta, cztery sypialnie z łazienkami", PLAN)
        self.assertIn("pietro = AF1;", PLAN)
        self.assertIn("float y0 = at.y >= pietro ? pietro : 0f", VILLA)


if __name__ == "__main__":
    unittest.main()
