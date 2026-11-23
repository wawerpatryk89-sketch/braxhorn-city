"""Owner 17.11.2026 14:56: his photo of a warm beige Mediterranean villa ("No bo dobra, zrób takie") for the seafront,
21:55 "dokończ wille". WILLA 3: peach-beige stucco, clay hip roofs at three heights, the front door's arch with arched
French windows and iron rails either side, the balcony over the door on two white columns, oculi upstairs and in the tall
east wing with its big arched walnut door, the porch wing west, chimneys, the formal pool in a dark border (all water:
18.11 04:48 "W basenie drzewo" - no island, no tree in it), a stair to the beach."""
import math
import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"
PLAN = (ED / "BraxChampionsWillaNabrzeze.cs").read_text(encoding="utf-8")
KLIF = (ED / "BraxChampionsWillaKlif.cs").read_text(encoding="utf-8")
KIT = (ED / "BraxChampionsWilleKitSrodziemne.cs").read_text(encoding="utf-8")
ISLAND = (ED / "BraxChampionsWillaWyspa.cs").read_text(encoding="utf-8")
VILLA = (ED / "BraxChampionsWille.cs").read_text(encoding="utf-8")
HILL = (ED / "BraxChampionsWillaWzgorze.cs").read_text(encoding="utf-8")


class WillaNabrzeze(unittest.TestCase):
    def test_built_on_the_seafront(self):
        self.assertIn("else if (nr == 3) PlanNabrzeze();", VILLA)
        self.assertTrue((ED / "BraxChampionsWillaNabrzeze.cs.meta").exists())
        self.assertIn("BraxChampionsWillaNabrzeze.cs", (ROOT / "unity-tools/mistrzowie/wille_preview/wille_preview.csproj").read_text())
        self.assertIn("if (nr == 3) return new Vector3(NPX0 - .5f, NPX1 + .5f, NPZ1 + .5f);", HILL)
        # each villa its own plan now: the hill's, the island's, the seafront's
        for plan in ("if (nr == 1) PlanWzgorze();", "else if (nr == 2) PlanWyspa();", "else if (nr == 3) PlanNabrzeze();"):
            self.assertIn(plan, VILLA)

    def test_his_beige_and_the_shared_shell(self):
        self.assertIn('tynkBez = BraxChampionsFoundation.SourceMaterial("willa_tynk_bezowy", "white_stucco_02", 2.0f, new Color(1.34f, 1.12f, .88f));', VILLA)
        # 24.11 (owner: "Zrób starą cegłę elewację i w ten sposób luksusową"): the shell in old brick (test_willa_nabrzeze_cegla_20261124)
        self.assertIn("NTeren(); NBasen(); KDom(cegla, true); NSkrzydla(); N3Kamien(); NWnetrze(c, tag); KPietro(c, tag); NOgrod(tag); N3Plaza();", PLAN)
        self.assertIn("KTeren(); KDom(tynkKrem, false); KSkrzydla(); KWieza(); KWnetrze(c, tag); KPietro(c, tag); KOgrod(tag);", KLIF)
        self.assertIn("if (nabrzeze) { NDomFront(zew); KDomReszta(zew, true); return; }", KLIF)
        # no tower on the seafront: a window where the hill's upper corridor opens to it
        self.assertIn("new Vector4(9.9f, 11f, KF1 + .9f, KF1 + 2.7f)", KLIF)
        self.assertIn("Nazwy[nrWilli - 1]", KLIF)

    def test_the_front_as_his_photo(self):
        # the door's arch r 1: springing 2.4, crown 3.4; the leaves under the glazed fanlight
        self.assertIn("OknoLuk(true, 0f, -1f, 1f, 2.38f, 2.4f, 3.4f, -1f, zew);", PLAN)
        self.assertEqual(PLAN.count("BraxChampionsInteriorDoor.Single("), 4)              # two double doors
        self.assertIn("Kolumna(new Vector3(-2.45f, 0f, -2.1f), KF1 - .22f, .22f); Kolumna(new Vector3(2.45f, 0f, -2.1f), KF1 - .22f, .22f);", PLAN)
        self.assertIn("Tralki(true, -2.27f, -2.8f, 2.8f, KF1);", PLAN)
        self.assertGreaterEqual(PLAN.count("Balkonik("), 6)
        self.assertGreaterEqual(PLAN.count("OknoOkragle("), 5)
        self.assertIn("static void OknoOkragle(bool alongX, float plane, float ac, float yc, float r, float outward, Material zew, float opW = .1f)", KIT)
        self.assertIn("static void LukDol(", KIT)

    def test_three_roof_heights(self):
        self.assertIn("const float NEw = 4.4f, NEe = 5.0f;", PLAN)
        self.assertIn("Dach4(-17f, -9f, -6f, 8f, NEw, KPitchW, zew, .6f, 2);", PLAN)
        self.assertIn("Dach4(9f, 17f, -5f, 13f, NEe, KPitchW, zew, .6f, 1);", PLAN)
        self.assertIn("Dach4(-9f, 9f, 0f, 13f, KE, KPitch, zew);", KLIF)
        # the house's upper side windows (sill KF1 + .9 = 5.1) stay over the wings' roofs where they meet its walls
        self.assertGreater(4.2 + .9, 5.0)
        self.assertGreater(4.2 + .9, 4.4)
        # the east wing's round windows between its door's crown (3.8) and its ceiling (4.7)
        self.assertIn("float ry = 4.28f, rr = .37f;", PLAN)
        self.assertGreater(4.28 - .37, 3.8)
        self.assertLess(4.28 + .37, 5.0 - .3)

    def test_the_formal_pool_all_water(self):
        self.assertIn("public const float NPX0 = -7f, NPX1 = 7f, NPZ0 = -20.2f, NPZ1 = -13.7f;", PLAN)
        self.assertIn("MM(stone, NPX0 - .45f, NPX1 + .45f, -.3f, 0f, NPZ0 - .45f, NPZ0);", PLAN)     # the dark border
        self.assertIn('sw.basinId = "willa_3_basen";', PLAN)
        # 18.11 04:48 "W basenie drzewo": no island and no tree in the water
        pool = PLAN[PLAN.index("static void NBasen()"):PLAN.index("static void", PLAN.index("static void NBasen()") + 20)]
        self.assertNotIn("drzewo_verdant", pool)
        self.assertNotIn("wysep", PLAN)
        self.assertNotIn("MM(stone, -.8f, .8f, fl, .32f", pool)

    def test_the_stair_to_the_beach_and_the_garage(self):
        self.assertIn("SchodyPlaza(NSt0, NSt1);", PLAN)
        self.assertIn("SchodyPlaza(WStX0, WStX1);", ISLAND)
        self.assertIn('BramaGarazu("brama", 10.2f, 15.8f, 13f, 2.6f);', PLAN)
        self.assertIn("MM(paving, 6f, 18f, -s, 0f, 13f, PadZ1);", PLAN)                        # the drive from the back gate (x 9)

    def test_no_wing_face_on_a_wall_face(self):
        # the ceilings and the porch's partition stop inside the wings' outer walls (17.11 z-fight check)
        self.assertIn("MM(ceiling, 9.3f, 16.7f, NEe - .3f, NEe, -4.7f, 0f, false);", PLAN)
        self.assertIn("MM(ceiling, -16.7f, -9.3f, NEw - .3f, NEw, -.94f, 0f, false);", PLAN)
        self.assertIn("Scianka(true, -1f, -16.7f, -9.3f, 0f, NEw - .3f,", PLAN)


if __name__ == "__main__":
    unittest.main()
