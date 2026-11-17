"""Owner 17.11.2026 14:55-14:56: on the gabled villa "To nie wygląda jak willa, tak jakieś baraki wygląda", then three photos
from the image search and "No bo dobra, zrób takie". WILLA 1 on the hill = his photo "A luxurious Mediterranean villa perched
on a scenic cliff". 15:17 on the first try ("No tu nic z tego nie przypomina, co ci pokazałem") - rebuilt as the photo: ivory
stucco, a U round the pool terrace (the two-storey house in the middle with three arches and a balcony over them, the wings
coming forward with their arched loggias, roofs at three heights), the pool on the edge of a real 9 m rock face, palms and
the region's trees round it."""
import math
import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"
PLAN = (ED / "BraxChampionsWillaKlif.cs").read_text(encoding="utf-8")
KIT = (ED / "BraxChampionsWilleKitSrodziemne.cs").read_text(encoding="utf-8")
WKIT = (ED / "BraxChampionsWilleKit.cs").read_text(encoding="utf-8")
VILLA = (ED / "BraxChampionsWille.cs").read_text(encoding="utf-8")
SITES = (ED / "BraxChampionsWilleDzialki.cs").read_text(encoding="utf-8")
PREP = (ROOT / "unity-tools/mistrzowie/prepare_assets.py").read_text(encoding="utf-8")


class WillaKlif(unittest.TestCase):
    def test_built_on_the_hill(self):
        self.assertIn("if (nr == 1) PlanKlif();", VILLA)
        for f in ("BraxChampionsWilleKitSrodziemne.cs.meta", "BraxChampionsWillaKlif.cs.meta"):
            self.assertTrue((ED / f).exists(), f)
        proj = (ROOT / "unity-tools/mistrzowie/wille_preview/wille_preview.csproj").read_text()
        self.assertIn("BraxChampionsWillaKlif.cs", proj)
        self.assertIn("BraxChampionsWilleKitSrodziemne.cs", proj)

    def test_real_clay_tiles_ivory_stucco_and_rock(self):
        self.assertIn("'clay_roof_tiles_02','cliff_side')", PREP)          # Poly Haven, fetched by the build
        self.assertIn('dachowka = BraxChampionsFoundation.SourceMaterial("willa_dachowka", "clay_roof_tiles_02", 2.5f', VILLA)
        self.assertIn('tynkKrem = BraxChampionsFoundation.SourceMaterial("willa_tynk_kremowy", "white_stucco_02", 2.0f, new Color(1.46f, 1.41f, 1.28f));', VILLA)
        self.assertIn('skala = BraxChampionsFoundation.SourceMaterial("willa_skala", "cliff_side", 1.83f', VILLA)
        # tiles run down the slope (u along the eave, v down the slope, in metres)
        self.assertIn("Vector2 UV(Vector3 q) => new Vector2(Vector3.Dot(q, eave), Vector3.Dot(q, upSlope) / Mathf.Cos(a));", KIT)

    def test_the_u_and_its_roofs_at_three_heights(self):
        self.assertIn("const float KE = 7.8f, KF1 = 4.2f, KEw = 4.4f, KPitch = 25f, KPitchW = 21f;", PLAN)
        self.assertIn("Dach4(-9f, 9f, 0f, 13f, KE, KPitch, zew);", PLAN)                    # the house
        self.assertIn("Dach4(-21f, -9f, -12f, 8f, KEw, KPitchW, zew, .6f, 2);", PLAN)        # the west wing, against the house
        self.assertIn("Dach4(9f, 21f, -10f, 10f, KEw, KPitchW, zew, .6f, 1);", PLAN)         # the east wing
        self.assertIn("Dach4(x0, x1, z0, z1, top, KPitch, zew, .55f);", PLAN)               # the belvedere tower
        t, tw = math.tan(math.radians(25)), math.tan(math.radians(21))
        wing_ridge = 4.4 + 6 * tw + .2                                    # 12 m wide wings: the ridge, its tube
        self.assertLess(wing_ridge, 7.8 - .6 * t)                         # under the house's eave
        self.assertGreater(11.0 + 2.5 * t, 7.8 + 6.5 * t)                 # the tower's ridge over the house's

    def test_the_photo_front(self):
        # three arches on columns before the salon, the balcony across over them
        self.assertIn("Kolumna(new Vector3(-2f, 0f, .15f), ys, .25f); Kolumna(new Vector3(2f, 0f, .15f), ys, .25f);", PLAN)
        self.assertGreaterEqual(PLAN.count("OknoLuk("), 19)
        self.assertIn('SchodySpiralne(new Vector3(-11.5f, 0f, 10.5f), KF1, bf, 22, 1.5f, 250f, -330f);', PLAN)
        self.assertIn("Schody(floorGf, 7.36f, 8.7f, 5.0f, 0f, KF1, 23, .27f, 7.36f);", PLAN)
        self.assertAlmostEqual(23 * .27, 6.21, delta=.01)                 # up 4.2 m over 6.2 m
        for room in ('"łazienka główna"', '"garderoba"', '"sypialnia główna"', '"sypialnia 2"', '"łazienka 2"', '"łazienka gościnna"',
                     'Drzwi("kuchnia"', 'tag + " salon TV"', 'BramaGarazu("brama 1"', 'BramaGarazu("brama 2"', 'Fontanna(', 'Komin('):
            self.assertIn(room, PLAN)
        self.assertIn("TablicaSprzedazy(1,", PLAN)
        self.assertIn("pietro = KF1;", PLAN)

    def test_the_pool_on_the_cliff(self):
        self.assertIn('BasenKsztalt("willa_1_basen", KBasen, new Vector4(-18.5f, 16.5f, PadZ0, -12.5f));', PLAN)
        self.assertNotIn("Ton(c.x", KIT)                                   # no box water body in a free-form pool
        self.assertIn("Tralki(true, PadZ0 + .15f, PadX0 + .2f, PadX1 - .2f, 0f);", PLAN)
        # the site drops the ground before the pad 9 m; the rock faces there are the villa's, the stone side walls not built
        self.assertIn("public const float PrzodKlif = -9f;", SITES)
        self.assertIn("return Mathf.Lerp(nr == 1 ? PrzodKlif : Przod, -.05f, Smooth(z0, z0 + 8f, lz));", SITES)
        self.assertIn("if (d.nr == 1) return;", SITES)
        self.assertIn("KlifSciana(true, PadZ0, PadX0 - 1f, PadX1 + 1f, -1f, a => BraxChampionsWilleDzialki.PrzodKlif - .6f, 3);", PLAN)
        self.assertEqual(PLAN.count("KlifSciana(false, "), 2)
        # its relief out to 1.5 m, flush under the terrace's edge; the terrace's paving gets a fascia to its top
        self.assertIn("float amp = 1.5f * Mathf.Clamp01((top - y) / 1.6f);", PLAN)
        self.assertIn("W.Kolizja(c, alongX ? new Vector3(e - a, h, .6f) : new Vector3(.6f, h, e - a), Quaternion.identity);", PLAN)
        self.assertIn("the pool terrace's edge over the cliff (its paving has no thickness): a fascia of its stone", PLAN)
        # the pool's zone under the pad never lifts the ground over the cliff's foot (a vertex on the edge rounds inside)
        self.assertIn("return Mathf.Min(d.padY - 2.2f, PrzyPadzie(d, l.y) - .3f);", SITES)

    def test_the_greenery_is_real(self):
        # owner 11.11: plants only natural (Poly Haven), no stand-ins; the region's palms and its drzewo_verdant
        self.assertIn('var t = BraxRegion.Model("drzewo_verdant");', WKIT)
        self.assertIn('BraxRegion.Put(g, t, "WILLA | drzewo (drzewo_verdant)", new Vector3(x, y, z), (x * 31f + z * 17f) % 360f, 1f, false, 0f, 0f, false)', WKIT)
        self.assertIn('BraxRegion.Put(g, tree, "WILLA 1 | wzgórze | drzewo", at, yaw, 1f, false, 6.5f * K)', SITES)
        self.assertIn("for (int i = 0; i < 6000 && (palmy < 26 || drzewa < 40); i++)", SITES)
        self.assertIn("if (Blisko(d, w, RoadHalf + 4.5f)) continue;", SITES)
        self.assertIn("Zielen(g, d);", SITES)
        self.assertGreaterEqual(PLAN.count("Palma("), 2)
        self.assertGreaterEqual(PLAN.count("Drzewo("), 2)
        self.assertNotIn("park_shrub", PLAN + VILLA + SITES)              # the sparse shrub read as weeds
        self.assertIn("static void Palma(float x, float z, float y = 0f)", WKIT)

    def test_arch_glazing_bead_is_smooth(self):
        self.assertIn("the curved glazing bead: a walnut strip on the glass's edge", KIT)
        self.assertIn("b.Surface(walnut, (u, v) =>", KIT)


if __name__ == "__main__":
    unittest.main()
