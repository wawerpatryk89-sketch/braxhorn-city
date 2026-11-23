"""The island villa finished (owner 23.11 20:11-20:13, his 11 screenshots: "za mało premium", "elewacja jakaś, kostka ...
cegła czy coś na ścianę", "w środku zrób porządek", "Wszystko wygląda jakby było gipsowane, ściany i sufity", "na niektórych
ścianach jakoś można dać cegiełkę ozdobną").

Every new surface from his own Poly Haven list (MATERIALY-WLASCICIELA), downloaded in 4K with the provider's checksums; the
linings start inside the wall they cover (no coplanar faces); the layout faults seen in his pictures are gone (a print and a
credenza before the WC's door, a print over the pantry's door, the stair's panes rising the wrong way); the potted plant scans
(70 000 / 176 000 triangles each, nine of them in the house: 1.05 million) cut to three of the lighter one."""
import json
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"
TOOLS = ROOT / "unity-tools/mistrzowie"
PREM = (ED / "BraxChampionsWillaWyspaPremium.cs").read_text(encoding="utf-8")
PLAN = (ED / "BraxChampionsWillaWyspa.cs").read_text(encoding="utf-8")
VILLA = (ED / "BraxChampionsWille.cs").read_text(encoding="utf-8")
SITES = (ED / "BraxChampionsWilleDzialki.cs").read_text(encoding="utf-8")
FUND = (ED / "BraxChampionsFoundation.cs").read_text(encoding="utf-8")
PREP = (TOOLS / "prepare_assets.py").read_text(encoding="utf-8")
FIX = json.loads((TOOLS / "pbr-provider-preflight.json").read_text(encoding="utf-8"))

# the owner's list (MATERIALY-WLASCICIELA.md on main, 23.11): the ones this house uses
LISTA = ("plastered_wall_05", "whitewashed_brick", "rustic_stone_wall", "dark_paneled_wood", "wooden_floor_01", "wooden_floor_02",
         "square_tiles_03", "rock_wall_16", "stone_wall_04", "rock_tile_floor", "rough_linen")


class Materialy(unittest.TestCase):
    def test_from_his_list_in_4k_with_the_providers_checksums(self):
        for s in LISTA:
            self.assertIn("'" + s + "'", PREP)
            for key in ("Diffuse", "nor_gl", "Rough", "arm"):
                self.assertIn("4k", FIX[s][key], s)
        self.assertTrue(PREP.rstrip().endswith("'painted_concrete_02')") or "'painted_concrete_02')" in PREP)
        for s in LISTA[:-1]:
            self.assertIn('"' + s + '"', PREM + FUND)
        # each photo-scan brought to its colour by its measured mean, its brightest 1 % kept from clipping
        for s in ("whitewashed_brick", "rustic_stone_wall", "dark_paneled_wood", "wooden_floor_01", "wooden_floor_02", "square_tiles_03",
                  "rock_wall_16", "stone_wall_04", "rock_tile_floor"):
            self.assertIn('{"' + s + '",new Vector3(', FUND)

    def test_no_more_plain_paint(self):
        # the old walls and ceilings: Beige Wall 001, a paint with no grain (its brightest 1 % is its mean) - the "gips"
        self.assertIn('{"beige_wall_001",new Vector3(159,141,121)}', FUND)
        self.assertIn('renderIn = Inside(Tynk("willa2_sciany_tynk"', PREM)
        self.assertIn('ceiling = Inside(Tynk("willa2_sufit_tynk"', PREM)
        self.assertIn("WPremiumMaterialy();", PLAN)
        self.assertLess(PLAN.index("WPremiumMaterialy();"), PLAN.index("Teren(); WTeren();"))
        self.assertIn("Wnetrze(2); Wyposazenie(2); WPremiumWnetrze(); Ogrod(2);", PLAN)

    def test_the_outside(self):
        self.assertIn('stone = F("willa2_elewacja_kamien", "rock_wall_16"', PREM)          # the ground floor and the entrance
        self.assertIn('deck = F("willa2_taras_granit", "rock_tile_floor"', PREM)
        self.assertIn("murOporowy = KamienMuruWyspy();", PREM)
        self.assertIn("var mur = d.rodzaj == Rodzaj.Wyspa ? BraxChampionsWille.KamienMuruWyspy() : kamien;", SITES)
        self.assertIn('steel.SetFloat("_ReflectionStrength", 1f);', PREM)                  # stainless, not black


class Wnetrze(unittest.TestCase):
    def linings(self):
        body = PREM[PREM.index("static void WPremiumWnetrze()"):PREM.index("static void LampkaBiurko")]
        return re.findall(r"Okl\((\w+), ([^;]*)\);", body)

    def test_feature_walls(self):
        mats = [m for m, _ in self.linings()]
        self.assertGreaterEqual(mats.count("wCegla"), 9)                                    # his decorative brick
        self.assertGreaterEqual(mats.count("wKamien"), 3)                                   # the fireplace wall, the tub
        self.assertGreaterEqual(mats.count("wPanel"), 7)                                    # the stair, beds, study, corridor
        self.assertIn("Okl(wDrewnoSufit, wx, GX1 - T, H0 - WSufitDol, H0 + .01f, GZ0 + .12f, zf);", PREM)

    def test_linings_start_inside_their_walls(self):
        # every vertical lining runs from 2 cm inside its wall: wall faces at x -13.7 / z 4.94 / z .94 / z 1.06 / z 2.94 / x 2.06
        self.assertIn("Okl(wKamien, wx - .02f, wx + .04f, -.02f, H0", PREM)
        self.assertIn("zs0 = 1f - .06f - .04f, zs1 = 1f - .06f + .02f", PREM)
        self.assertIn("float f0 = polnoc ? 1f + .06f - .02f : 3f - .06f - .04f, f1 = polnoc ? 1f + .06f + .04f : 3f - .06f + .02f;", PREM)
        self.assertIn("Okl(wKamien, 10.4f, 14.6f, y + .005f, y + 2.62f, 1.068f, 1.10f);", PREM)   # into the bath's tiles (1.064..1.076)
        # the oak ceiling 25 mm under the plaster, its downlights with it
        self.assertIn("public const float WSufitDol = .025f;", PREM)
        self.assertIn("float dol = z < 5f - .06f ? WSufitDol : 0f;", VILLA)

    def test_order_in_the_rooms(self):
        self.assertNotIn("Obrazek(nr, new Vector3(-4.25f, 1.75f, 5f - .06f)", VILLA)      # a print over the WC's door
        self.assertNotIn("Obrazek(nr + 1, new Vector3(11.3f, 1.75f, 5f - .06f)", VILLA)   # ... over the pantry's door
        self.assertNotIn("new Vector3(-4.25f, .38f, 5f - .06f - .23f)", VILLA)            # a credenza before the WC's door
        self.assertIn("b.RoundedBox(walnut, new Vector3(5.5f, .38f, GZ1 - T - .25f)", VILLA)
        self.assertIn("Obrazek(nr + 2, new Vector3(12.0f, y + 1.85f, 1f - .06f - .045f)", VILLA)   # on the bed walls' lining
        self.assertNotIn("desk_lamp_arm_01", VILLA)
        self.assertIn("LampkaBiurko(new Vector3(2.55f, y + .76f, -1.62f));", VILLA)
        self.assertIn("Dywan(new Vector3(-10.4f, 0f, -.6f), 4.4f, 3.8f);", VILLA)
        self.assertIn("nrWilli == 2 && wGranit ? wGranit : BraxKatalog.StoneDark", VILLA)  # the shower tray

    def test_the_heavy_plants(self):
        body = VILLA[VILLA.index("static void Bryla(int nr)"):VILLA.index("static float fxHearth")]
        self.assertEqual(body.count("potted_plant_01_1k.gltf"), 0)                        # 176 000 triangles each
        self.assertLessEqual(body.count("potted_plant_02_1k.gltf"), 3)                    # 70 000 each


class Schody(unittest.TestCase):
    def test_the_stair_to_the_beach(self):
        self.assertIn("const float WStX0 = 18f, WStX1 = 22f", PLAN)
        self.assertIn("if (nrWilli == 2) { SchodyPolicze(x0, x1, riser, zFoot, slope, len); return; }", PLAN)
        # +pitch turns +z down; the stair rises to +z: every slope piece pitched by -slope
        self.assertNotIn("Quaternion.Euler(slope, 0f, 0f)", PLAN)
        self.assertEqual(PLAN.count("Quaternion.Euler(-slope, 0f, 0f)"), 5)
        self.assertIn("MM(murOporowy, a, c, -WStDrop - .35f, stepTop + h, za, za + WStTread, false);", PLAN)   # edge to edge


if __name__ == "__main__":
    unittest.main()
