"""Owner 17.11.2026: 14:02 "w tej na małej wyspie kort tenisowy, mały pomostek, miejsce na motorówkę, nie wiem, na mały
jacht", 14:56 his photo of a white modern Miami waterfront house ("No bo dobra, zrób takie"), 21:55 "dokończ wille".
WILLA 2 on the island: two floors of black-framed glass, the white slab band with the glass balustrade, the thin white roof
far over the walls in two heights, dark vertical slats, the covered dining with a bar, the raised mosaic spa at the pool's
end; the ITF tennis court; the jetty with his Riva."""
import math
import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"
PLAN = (ED / "BraxChampionsWillaWyspa.cs").read_text(encoding="utf-8")
VILLA = (ED / "BraxChampionsWille.cs").read_text(encoding="utf-8")
HILL = (ED / "BraxChampionsWillaWzgorze.cs").read_text(encoding="utf-8")
SITES = (ED / "BraxChampionsWilleDzialki.cs").read_text(encoding="utf-8")
HARBOUR = (ED / "BraxChampionsHarbour.cs").read_text(encoding="utf-8")
PREP = (ROOT / "unity-tools/mistrzowie/prepare_assets.py").read_text(encoding="utf-8")


class WillaWyspa(unittest.TestCase):
    def test_built_on_the_island(self):
        self.assertIn("else if (nr == 2) PlanWyspa();", VILLA)
        self.assertTrue((ED / "BraxChampionsWillaWyspa.cs.meta").exists())
        self.assertIn("BraxChampionsWillaWyspa.cs", (ROOT / "unity-tools/mistrzowie/wille_preview/wille_preview.csproj").read_text())
        # the pad grows west for the court; the site's ground, walls and pool zone follow each villa's own
        self.assertIn("public const float WPadX0 = -48f;", PLAN)
        self.assertIn("public static Vector4 Pad(int nr) => new Vector4(nr == 2 ? WPadX0 : PadX0, PadX1, PadZ0, PadZ1);", HILL)
        self.assertIn("if (nr == 2) return new Vector3(PoolX0, SX1 + .2f, PoolZ1);", HILL)

    def test_as_his_miami_photo(self):
        self.assertIn("if (nr == 2) render = elewacja;", VILLA)                 # the scanned stucco, white to the ground
        self.assertIn("static Material ScianaParter(int nr) => render;", VILLA)
        # the thin roof in two heights far over the walls
        self.assertIn("MM(render, UX0 - 2.2f, 2f, r0, r0 + .25f, UZ0 - 2.4f, UZ1 + 1.4f);", PLAN)
        self.assertIn("MM(render, 2f, UX1 + 2.2f, r0 + step, r0 + step + .25f, UZ0 - 2.4f, UZ1 + 1.4f);", PLAN)
        # the slats: before the upper west glass (clear of the balcony's door x -12..-9) and along the west side
        self.assertIn("Lamele(true, UGlass - .16f, UX0 + T + .1f, -12.2f, FFL1 + .02f, Roof - .02f);", PLAN)
        self.assertIn("Lamele(false, UX0 - .06f, UGlass + .1f, UZ1 - .1f, FFL1 + .02f, Roof - .02f);", PLAN)
        self.assertIn("new Vector2(-12.0f, -9.0f)", VILLA)                        # that door in the upper glass
        # the white slab band
        self.assertIn("MM(render, UX0 - .02f, UX1 + .02f, H0, FFL1, UZ0 - .02f, UZ0, false);", PLAN)
        # no garage on an island: the covered dining (no empty planters)
        self.assertNotIn("Garaz()", PLAN)
        self.assertIn('BraxKatalog.DiningSet(c8, new Vector3(19.0f, .012f, -.4f), Vector3.right, "krzesło");', PLAN)
        self.assertIn("if (nr != 2) foreach (float x in new[] { -20f, 22.5f })", VILLA)
        self.assertIn('nr == 2 ? "MONIKA: Willa na własnej wyspie w zatoce, jak z Miami', VILLA)

    def test_the_raised_spa(self):
        self.assertIn("const float SX0 = 3.3f, SX1 = 6.7f, SZ0 = -20.8f, SZ1 = -17.4f, SpaRim = .45f, SpaWater = .40f, SpaFloor = -.75f;", PLAN)
        self.assertAlmostEqual(.40 - -.75, 1.15)                                  # deep enough to sit in (seats 45 cm under)
        self.assertIn('sw.basinId = "willa_2_spa";', PLAN)
        # the deck round it, not under it
        self.assertIn("MM(deck, PoolX1, SX0, -s, 0f, PadZ0 + .3f, PoolZ1); MM(deck, SX1, 24f, -s, 0f, PadZ0 + .3f, PoolZ1);", VILLA)

    def test_the_itf_tennis_court(self):
        self.assertIn("const float KCX = -37.1f, KCZ = 3.0f, KHX = 9.145f, KHZ = 18.285f;", PLAN)
        # ITF: 23.77 x 10.97, 6.40 m behind the baselines, 3.66 m beside - 36.57 x 18.29
        self.assertAlmostEqual(2 * 18.285, 23.77 + 2 * 6.40, delta=.001)
        self.assertAlmostEqual(2 * 9.145, 10.97 + 2 * 3.66, delta=.001)
        for line in ("-5.485f, -5.435f, -4.115f, -4.065f, -.025f, .025f", "-11.885f, -11.785f, -11.685f, -6.45f, -6.40f, 6.40f"):
            self.assertIn(line, PLAN)
        self.assertAlmostEqual(11.885 * 2, 23.77)
        self.assertAlmostEqual(5.485 * 2, 10.97)
        self.assertAlmostEqual(4.115 * 2, 8.23)
        self.assertIn("float Top(float x) => .914f + (1.07f - .914f) * (x / px) * (x / px);", PLAN)
        self.assertIn("float px = 5.485f + .914f;", PLAN)
        # the court on the pad, clear of the house (its upper floor from x -16)
        self.assertGreater(-37.1 - 9.145, -48)
        self.assertLess(-37.1 + 9.145, -16)
        self.assertGreater(3.0 - 18.285, -22.6)
        self.assertLess(3.0 + 18.285, 26)
        self.assertIn("'painted_concrete_02')", PREP)
        self.assertIn('"painted_concrete_02", 4f', PLAN)

    def test_the_stair_and_the_riva(self):
        self.assertIn("const float WStX0 = 19f, WStX1 = 21f, WStDrop = 3.95f, WStTread = .30f; const int WStN = 23;", PLAN)
        self.assertLess(3.95 / 23, .18)
        self.assertLess(19, 20)
        self.assertLess(20, 21)                                                    # the jetty's root (PomostX 20) at its foot
        self.assertIn("public static float PomostX(int nr) => 20f;", HILL)
        self.assertIn("Balustrada(true, PadZ0 + .15f, WStX1 + .1f, PadX1 - .2f, 0f);", VILLA)
        self.assertIn("if (d.rodzaj == Rodzaj.Wyspa) { Pomost(b, d); Lodz(g, d); }", SITES)
        self.assertIn('BraxChampionsHarbour.PrywatnaLodz(g, "W2", "motorboat", s.x + 3.0f, zb - 9f, 0f, 2.6f, s.x + .9f, zb - 9f, PomostY)', SITES)
        self.assertIn('catch (Exception e) { BraxBuildQuality.Warn("DO POPRAWKI WILLA_2_RIVA: " + e.Message); }', SITES)
        self.assertIn("return PlaceBoat(parent,bay,sources[kind],pierY);", HARBOUR)
        self.assertIn('b.kind=="motorboat"?8.5f', HARBOUR)                          # real size

    def test_palms_on_the_island(self):
        self.assertIn("int maxP = hill ? 26 : 22, maxD = hill ? 40 : 8;", SITES)
        self.assertIn("if (!hill && (Grunt(d, w.x, w.y) < SeaY + .55f || (Mathf.Abs(w.x - ps.x) < 6f && w.y > ps.y - 30f))) continue;", SITES)


if __name__ == "__main__":
    unittest.main()
