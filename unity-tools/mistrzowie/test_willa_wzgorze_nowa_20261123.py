"""The hill villa rebuilt modern (owner 23.11 21:24-22:22: "Ta na wzgórzu, za areną ... kompletnie trzeba przebudowę zrobić.
Okna są chujowe, nie ma futryn, nie ma nic, nigdzie nie ma futryn w ogóle. Elewacja do zmiany, w środku tak samo", "Wzdłuż
willi mają iść grube palmy", his choice "Nowoczesna jak wyspa", his Poly Haven library "na mury, na kostki, na podłogi, na
wszystko", "garaże mają się otwierać ... z pilota nawet w samochodzie", "dojazd do willi, te grube palmy po obu stronach
drogi").

The hill builds BraxChampionsWillaWzgorze again, flat-roofed, finished from his list; every villa door has a frame; the
hill's windows real frames and sills; the beds stand against their walls (the catalogue's p is a bed's foot); thick palms
along the house and both sides of the drive; the garage doors open for a driven car."""
import json
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"
RT = ROOT / "unity-city/Assets/Mistrzowie"
TOOLS = ROOT / "unity-tools/mistrzowie"
NOWA = (ED / "BraxChampionsWillaWzgorzeNowa.cs").read_text(encoding="utf-8")
PLAN = (ED / "BraxChampionsWillaWzgorze.cs").read_text(encoding="utf-8")
VILLA = (ED / "BraxChampionsWille.cs").read_text(encoding="utf-8")
KIT = (ED / "BraxChampionsWilleKit.cs").read_text(encoding="utf-8")
KLIF = (ED / "BraxChampionsWillaKlif.cs").read_text(encoding="utf-8")
SITES = (ED / "BraxChampionsWilleDzialki.cs").read_text(encoding="utf-8")
FUND = (ED / "BraxChampionsFoundation.cs").read_text(encoding="utf-8")
PREP = (TOOLS / "prepare_assets.py").read_text(encoding="utf-8")
FIX = json.loads((TOOLS / "pbr-provider-preflight.json").read_text(encoding="utf-8"))
PILOT = (RT / "BraxChampionsBramaPilot.cs").read_text(encoding="utf-8")
SZAFA = (RT / "BraxChampionsSzafa.cs").read_text(encoding="utf-8")

NEW = ("rock_wall_04", "pavement_02", "herringbone_concrete_tile_03", "stone_pathway_02", "coated_pine", "old_wooden_floor_03",
       "wood_table", "brick_wall_02", "lacquered_cherry_wood")


class Plan(unittest.TestCase):
    def test_the_hill_builds_the_modern_plan(self):
        self.assertIn("if (nr == 1) PlanWzgorze();", VILLA)
        self.assertIn("W1Materialy();", PLAN)
        self.assertIn("ATeren(); AWille(); AWnetrze(c, tag); W1Wnetrze(c, tag); AOgrod(tag); W1Ogrod();", PLAN)
        self.assertTrue((ED / "BraxChampionsWillaWzgorzeNowa.cs.meta").exists())
        self.assertIn("BraxChampionsWillaWzgorzeNowa.cs", (TOOLS / "wille_preview/wille_preview.csproj").read_text())

    def test_flat_roofs_and_steel_frames(self):
        self.assertIn("static void DachPlaski(float x0, float x1, float z0, float z1, float e, float ovW, float ovE, float ovS, float ovN, Material lining = null)", NOWA)
        self.assertEqual(PLAN.count("DachPlaski("), 4)
        self.assertIn("static void RamaProsta(float x0, float x1, float e, float z, float outward)", NOWA)
        # the hall's roof 2.5 m on over the terrace on pine, downlights in it; beams under its flat oak ceiling
        self.assertIn("DachPlaski(-5f, 5f, -10f, 14f, AE, .5f, .5f, 2.5f, .5f, soffit);", PLAN)
        self.assertIn("float vault = AE - .035f;", PLAN)

    def test_the_rock_face_and_the_pool(self):
        self.assertIn("static void KlifSciana(bool alongX, float plane, float a0, float a1, float outward, System.Func<float, float> foot, int seed, float top = -.3f)", KLIF)
        self.assertIn("KlifSciana(true, PadZ0, APoolX0 - sk, APoolX1 + sk, -1f, a => foot, 4, pod);", NOWA)
        self.assertIn("var skin = nrWilli == 1 ? stone : elewacja;", KIT)


class Materialy(unittest.TestCase):
    def test_from_his_list_in_4k(self):
        for s in NEW:
            self.assertIn("'" + s + "'", PREP)
            for key in ("Diffuse", "nor_gl", "Rough", "arm"):
                self.assertIn("4k", FIX[s][key], s)
            self.assertIn('{"' + s + '",new Vector3(', FUND)                         # its brightest 1 %
            self.assertIn('"' + s + '"', NOWA)

    def test_the_facade(self):
        self.assertIn('elewacja = Tynk("willa1_elewacja_biel"', NOWA)
        self.assertIn('stone = F("willa1_kamien_mur", "rock_wall_04"', NOWA)
        self.assertIn('soffit = F("willa1_drewno_sosna", "coated_pine"', NOWA)
        self.assertGreaterEqual(PLAN.count("ScianaOE(stone,"), 5)                    # the ground floors' ends, the garage's side
        self.assertIn("ScianaOE(soffit, true, 14f, -5f, 5f, 0f, AE", PLAN)            # the hall's entrance front in pine

    def test_inside(self):
        self.assertIn('floorGf = Inside(F("willa1_podloga_parter", "old_wooden_floor_03"', NOWA)
        self.assertIn('floorUf = Inside(F("willa1_podloga_pietro", "wood_table"', NOWA)
        body = NOWA[NOWA.index("static void W1Wnetrze"):]
        self.assertGreaterEqual(body.count("MM(wPanel,"), 4)
        self.assertGreaterEqual(body.count("MM(wCegla,"), 4)


class OknaIDrzwi(unittest.TestCase):
    def test_window_frames_and_sills(self):
        self.assertIn("bool rama = nrWilli == 1;", VILLA)
        self.assertIn("float fw = rama ? .045f : .03f, fd = rama ? .07f : .05f, fh = rama ? .09f : .05f;", VILLA)
        self.assertIn("if (nrWilli == 1 && o.z > y0 + .2f) Parapety(alongX, plane, outward, o.x, o.y, o.z);", KIT)
        # clear of the wall's skins' joint (T/2 +- 4 mm): no face on a skin's
        self.assertIn("P(oknoParapet, -.05f, T * .5f - .012f,", NOWA)
        self.assertIn("P(oak, T * .5f + .012f, T + .04f,", NOWA)

    def test_every_door_has_a_frame(self):
        self.assertIn("Futryna(alongX, plane, a0, a1, y0, h, proudMinus, proudPlus, grub);", VILLA)
        self.assertIn("static void Futryna(bool alongX, float plane, float a0, float a1, float y0, float h, float proudMinus, float proudPlus, float t = .06f)", VILLA)
        # over a 4 cm lining and the wainscot's rail (5.5 cm): the architraves stand further out than both
        self.assertIn("const float NaOkl = .065f;", VILLA)
        self.assertGreater(.065, .04 + .015)
        # an outside wall's doors: the frame the wall's full depth
        self.assertIn('Drzwi("do garażu", false, GX1 - T * .5f, 6.5f, 7.45f, 0f, 2.25f, .018f, .018f, T * .5f);', VILLA)
        self.assertEqual(PLAN.count(".018f, .018f, T * .5f);"), 2)
        for d in ('"sypialnia 2", true, 1f, -9.0f, -8.1f, y, 2.25f, NaOkl, NaOkl', '"garderoba 1", true, -.5f, -11.4f, -10.6f, y, 2.25f, NaOkl'):
            self.assertIn(d, VILLA + PLAN)


class Sypialnie(unittest.TestCase):
    """BraxKatalog.Bed: p is the middle of the bed's FOOT; the head (the headboard's back) 2.17 m behind it."""

    def bed(self, x):
        m = re.search(r"BraxKatalog\.Bed\(c, new Vector3\(" + re.escape(x) + r"f, y \+ \.012f, (-?[\d.]+)f\), Vector3\.(back|forward)", PLAN)
        return float(m.group(1)), m.group(2)

    def test_heads_against_their_walls(self):
        # (the bed's x, the face its head goes to: a lining's .04 off the partition's .06, or the bare face)
        for x, face in (("-13.6", -.5 - .06 - .04), ("12.2", .4 - .06 - .04), ("21.0", 10.2 - .06 - .04), ("-13.0", 2.8 + .06)):
            z, f = self.bed(x)
            head = z + 2.17 if f == "back" else z - 2.17
            self.assertAlmostEqual(abs(head - face), .03, delta=.015, msg=x)

    def test_no_door_behind_a_bed(self):
        # the head walls' doors: bedroom 1 (-11.4..-10.6, -9.8..-8.9), the master (8.8..9.7, 14.7..15.6); the bath 1 through
        # the walk-in (its door on the walk-in's west wall)
        for x, wb, doors in ((-13.6, 1.8, ((-11.4, -10.6), (-9.8, -8.9))), (12.2, 1.8, ((8.8, 9.7), (14.7, 15.6)))):
            half = (wb + .30) * .5 + .30 + .25
            for d0, d1 in doors:
                self.assertTrue(x + half < d0 - .5 or x - half > d1 + .5, (x, d0, d1))
        self.assertIn('Drzwi("łazienka 1", false, -12.4f, 1.3f, 2.1f, y);', PLAN)
        self.assertIn("if (door.x == 2f)", VILLA)

    def test_a_tv_in_every_bedroom(self):
        for tv in ('" sypialnia 1 TV"', '" sypialnia 2 TV"', '" sypialnia główna TV"', '" sypialnia gościnna TV"'):
            self.assertIn(tv, PLAN)


class Ogrod(unittest.TestCase):
    def test_thick_palms_along_the_house_and_the_drive(self):
        self.assertIn("foreach (float z in new[] { -20f, -14f, -8f, -2f, 4f, 10f, 22f }) { Palma(-21.6f, z); W1Palmy++; }", NOWA)
        self.assertIn('var palm = BraxMiastoAssety.Model("miasto_palmy", "palma");', SITES)       # the thick one (palm_hp_4)
        self.assertIn("float next = 10f, s = 0f, off = RoadHalf + 2.4f;", SITES)
        self.assertIn("next += 12f;", SITES)
        self.assertIn("MISTRZOWIE_WILLA_ALEJA palms=", SITES)

    def test_the_garage_opens_for_a_driven_car(self):
        self.assertTrue((RT / "BraxChampionsBramaPilot.cs.meta").exists())
        self.assertIn("public void Ustaw(bool otworz)", SZAFA)
        self.assertIn("var pilot = holder.gameObject.AddComponent<BraxChampionsBramaPilot>();", KIT)
        self.assertIn("if (car.Driver && (Plasko(p - a) < zasieg * s || Plasko(p - b) < 6f * s)) jedzie = true;", PILOT)
        self.assertIn("!wDrzwiach) { brama.Ustaw(false); pilotem = false; }", PILOT)       # never onto a car in the doorway
        self.assertIn("if (!car || car.helikopter) continue;", PILOT)


if __name__ == "__main__":
    unittest.main()
