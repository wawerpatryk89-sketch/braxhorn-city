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
         "square_tiles_03", "rock_tile_floor", "rough_linen")


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
                  "rock_tile_floor"):
            self.assertIn('{"' + s + '",new Vector3(', FUND)

    def test_no_more_plain_paint(self):
        # the old walls and ceilings: Beige Wall 001, a paint with no grain (its brightest 1 % is its mean) - the "gips"
        self.assertIn('{"beige_wall_001",new Vector3(159,141,121)}', FUND)
        self.assertIn('renderIn = Inside(Tynk("willa2_sciany_krem"', PREM)
        self.assertIn('ceiling = Inside(Tynk("willa2_sufit_cieply"', PREM)
        self.assertIn("WPremiumMaterialy();", PLAN)
        self.assertLess(PLAN.index("WPremiumMaterialy();"), PLAN.index("Teren(); WTeren();"))
        self.assertIn("Wnetrze(2); Wyposazenie(2); WPremiumWnetrze(); Ogrod(2);", PLAN)

    def test_the_outside(self):
        self.assertIn('stone = F("willa2_elewacja_piaskowiec", "rustic_stone_wall"', PREM)  # the ground floor and the entrance
        self.assertIn("deck = GranitTarasu();", PREM)
        self.assertIn("murOporowy = KamienMuruWyspy();", PREM)
        self.assertIn("var mur = d.rodzaj == Rodzaj.Wyspa ? BraxChampionsWille.KamienMuruWyspy() : kamien;", SITES)
        self.assertIn('steel.SetFloat("_ReflectionStrength", 1f);', PREM)                  # stainless, not black


class NieSzare(unittest.TestCase):
    """Owner 23.11 21:02: "Te szare ściany mi się kompletnie nie podobają." No grey wall left in the island villa."""

    def test_warm_walls_ceilings_and_stone(self):
        m = re.search(r'renderIn = Inside\(Tynk\("willa2_sciany_krem", new Color\(([\d.]+)f, ([\d.]+)f, ([\d.]+)f\)', PREM)
        r, g, b = map(float, m.groups())
        self.assertGreater(r - b, .1)                                                       # ivory, not white or grey
        self.assertGreaterEqual(min(r, g, b), .75)                                          # light
        m = re.search(r'ceiling = Inside\(Tynk\("willa2_sufit_cieply", new Color\(([\d.]+)f, ([\d.]+)f, ([\d.]+)f\)', PREM)
        r, g, b = map(float, m.groups())
        self.assertGreater(r - b, .04)
        self.assertGreater(min(r, g, b), .88)
        # the stone: warm sandstone (the fireplace wall's) on the facade, the retaining walls and the stair's cheeks
        self.assertIn('"willa2_mur_piaskowiec", "rustic_stone_wall"', PREM)
        for grey in ("rock_wall_16", "stone_wall_04", "MeanRockWall16", "MeanStoneWall04"):
            self.assertNotIn(grey, PREM + FUND + PREP)
            self.assertNotIn(grey, FIX)

    def test_the_preview_draws_the_real_tint(self):
        # the region's preview capped a tint at 2.2: the ivory plaster (tint 3.1, 2.8, 2.4) was drawn blue-grey
        js = (TOOLS / "hotel_sobczak_preview/render3d.js").read_text(encoding="utf-8")
        self.assertIn("process.env.PREVIEW_COLOUR_CAP", js)
        self.assertIn("Math.min(c[0],CAP)", js)


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
        self.assertIn("Obrazek(nr + 2, new Vector3(12.0f, y + 2.05f, 1f - .06f - .045f)", VILLA)   # on the bed walls' lining
        self.assertNotIn("desk_lamp_arm_01", VILLA)
        self.assertIn("LampkaBiurko(new Vector3(2.55f, y + .76f, -1.62f));", VILLA)
        self.assertIn("Dywan(new Vector3(-10.4f, 0f, -.6f), 4.4f, 3.8f);", VILLA)
        self.assertIn("nrWilli == 2 && wGranit ? wGranit : BraxKatalog.StoneDark", VILLA)  # the shower tray

    def test_the_heavy_plants(self):
        body = VILLA[VILLA.index("static void Bryla(int nr)"):VILLA.index("static float fxHearth")]
        self.assertEqual(body.count("potted_plant_01_1k.gltf"), 0)                        # 176 000 triangles each
        self.assertLessEqual(body.count("potted_plant_02_1k.gltf"), 3)                    # 70 000 each


class Sypialnie(unittest.TestCase):
    """Owner 23.11 21:12: "A te łóżka w sypialni widziałeś, jak były wstawione?" """

    def test_no_bedside_table_before_a_door(self):
        # BraxKatalog.Bed: headboard Wb + .30 wide, a bedside table each side centred at its half + .30, .50 wide
        x3 = float(re.search(r"const float Lozko3X = (-?[\d.]+)f;", VILLA).group(1))
        beds = [(-11.4, 1.6, (-9.0, -8.1)), (x3, 1.6, (-1.5, -.6))]                 # the bed, its width, its room's door
        for x, wb, (d0, d1) in beds:
            half = (wb + .30) * .5 + .30 + .25
            self.assertGreaterEqual(min(abs(x + half - d0), abs(x - half - d1)), .9, (x, d0, d1))
            self.assertTrue(x + half < d0 or x - half > d1)
        self.assertIn("BraxKatalog.Bed(c, new Vector3(Lozko3X, y + .012f, -1.30f), Vector3.back, 1.6f", VILLA)

    def test_prints_clear_of_the_headboards_and_rooms_furnished(self):
        # the headboard's top: 1.48 m; each print's lower edge (its frame 4 cm) over it
        for m in re.finditer(r"Obrazek\(nr \+ [234], new Vector3\([^,]+, y \+ ([\d.]+)f, 1f - \.06f - \.045f\), Vector3\.back, [\d.]+f, ([\d.]+)f\)", VILLA):
            cy, h = float(m.group(1)), float(m.group(2))
            self.assertGreater(cy - h / 2 - .04, 1.48 + .05)
        self.assertEqual(len(re.findall(r"Obrazek\(nr \+ [234], new Vector3", VILLA)), 3)
        self.assertIn("LawkaLozka(c, new Vector3(x, y, -1.30f - .08f - .22f)", VILLA)
        self.assertEqual(VILLA.count('BraxKatalog.Dresser(c, new Vector3(-7f'), 2)

    def test_one_tv_in_each_room_no_more(self):
        # 21:23 "wszędzie telewizory, żeby nie było też za dużo telewizorów": the living room, the master, bedrooms 2 and 3
        tvs = re.findall(r'BraxKatalog\.Tv\(c, .*?tag \+ " ([^"]+)"\);', VILLA)
        self.assertEqual(sorted(tvs), sorted(["salon TV", "sypialnia TV", "sypialnia 2 TV", "sypialnia 3 TV"]))
        self.assertNotIn("Obrazek(nr + 5", VILLA)                                         # the TV over the chest, not a print


class Wykonczenie(unittest.TestCase):
    def test_curtains_wall_lights_and_the_beach(self):
        self.assertIn('wLen = Inside(F("willa2_zaslony_len", "rough_linen", .27f', PREM)               # his Rough Linen, 27 cm
        self.assertIn("b.Surface(wLen, P, folds * 6, 1); b.Surface(wLen, P, folds * 6, 1, true);", PREM)  # both sides
        self.assertEqual(PREM.count("Zaslona(a, c, "), 2)
        self.assertIn("foreach (float x in new[] { -14.3f, -10.0f, -5.0f, .6f })", PREM)              # clear of the doors and the print
        # the island's beach: the path stair-to-jetty, loungers on the dry sand, parasols, a shower, lights
        self.assertIn("PlazaWyspy(b, d); }", SITES)
        self.assertIn("if (h < SeaY + .62f) { cz = z; break; }", SITES)                              # over the run-up's wet sand
        self.assertIn("float z1 = Mathf.Min(z + 1f, ps.y - .1f), top = float.MinValue;", SITES)        # slabs edge to edge
        self.assertIn('W.Siedzisko(P(0f, h + .10f, 0f) * K, Vector3.forward, "LEŻAK", true);', SITES)


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
