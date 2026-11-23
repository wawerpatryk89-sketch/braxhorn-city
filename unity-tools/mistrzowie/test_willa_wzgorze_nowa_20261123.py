"""The hill villa rebuilt modern (owner 23.11 21:24-22:22: "Ta na wzgórzu, za areną ... kompletnie trzeba przebudowę zrobić.
Okna są chujowe, nie ma futryn, nie ma nic, nigdzie nie ma futryn w ogóle. Elewacja do zmiany, w środku tak samo", "Wzdłuż
willi mają iść grube palmy", his choice "Nowoczesna jak wyspa", his Poly Haven library "na mury, na kostki, na podłogi, na
wszystko", "garaże mają się otwierać ... z pilota nawet w samochodzie", "dojazd do willi, te grube palmy po obu stronach
drogi").

The hill builds BraxChampionsWillaWzgorze again, flat-roofed, finished from his list; every villa door has a frame; the
hill's windows real frames and sills; the beds stand against their walls (the catalogue's p is a bed's foot); thick palms
along the house and both sides of the drive; the garage doors open for a driven car; (22:32) the upper floors out 3 m
over the terrace on thin roofs, the rooms there furnished, nothing across a door or a window."""
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
        self.assertIn("ATeren(); AWille(); W1Bryla(); AWnetrze(c, tag); W1Wnetrze(c, tag); AOgrod(tag); W1Ogrod();", PLAN)
        self.assertTrue((ED / "BraxChampionsWillaWzgorzeNowa.cs.meta").exists())
        self.assertIn("BraxChampionsWillaWzgorzeNowa.cs", (TOOLS / "wille_preview/wille_preview.csproj").read_text())

    def test_flat_roofs_and_steel_frames(self):
        self.assertIn("static void DachPlaski(float x0, float x1, float z0, float z1, float e, float ovW, float ovE, float ovS, float ovN, Material lining = null)", NOWA)
        self.assertEqual(PLAN.count("DachPlaski("), 4)
        self.assertIn("static void RamaProsta(float x0, float x1, float e, float z, float outward, float y0 = 0f)", NOWA)
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
        # 23:10 "nie ma szarych ścian": the walls inside the island villa's cream (its Tynk, the same target)
        self.assertIn('renderIn = Inside(Tynk("willa1_sciany_krem", new Color(.93f, .88f, .79f), 2f), .80f);', NOWA)
        self.assertIn('Tynk("willa2_sciany_krem", new Color(.93f, .88f, .79f), 2f)', (ED / "BraxChampionsWillaWyspaPremium.cs").read_text(encoding="utf-8"))
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
        # the guest suite's west wall (the E wing's outside render) plastered cream indoors, clear of its door
        self.assertIn("foreach (var (z0, z1) in new[] { (4.28f, 10.16f), (10.24f, 11.54f), (12.56f, 13.72f) })", body)
        self.assertIn("MM(renderIn, 15.98f, 16.01f, y - .02f, top + .02f, z0, z1, false);", body)


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
        self.assertEqual(PLAN.count(".018f, .018f, T * .5f);"), 7)                               # + the links' five doorways (23:10)
        for d in ('"łącznik W", false, -8f - T * .5f, -2.4f, -1.4f, 0f, 2.4f', '"łącznik E pralnia", false, 8f + T * .5f, 9.4f, 10.4f, 0f, 2.4f',
                  '"łącznik E piętro", false, 8f + T * .5f, 7.6f, 8.5f, AF1, 2.3f'):
            self.assertIn(d, PLAN)
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


class BezSzarychScian(unittest.TestCase):
    """23:10 "Pamiętaj, nie ma szarych ścian": every wall seen indoors in the cream plaster - the links' and the guest
    suite's walls were the wings' outside render (grey indoors)."""

    def test_the_links_plastered(self):
        self.assertIn("static void Tynkuj(float px, float s, float z0, float z1, float y0, float y1, params Vector4[] holes)", NOWA)
        self.assertIn("float a = px + s * .001f, c = px + s * .012f;", NOWA)                   # 1 mm off the face, 1.1 cm thick
        self.assertEqual(NOWA.count("Tynkuj(wing, inW,"), 3)
        self.assertEqual(NOWA.count("Tynkuj(hall, inH,"), 2)
        self.assertIn("MM(ceiling, xa + .004f, xb - .004f, 5.98f, 6.0f, -2.80f, 11.80f, false);", NOWA)
        self.assertIn("Oczka(xa + 1.5f, xa + 1.5f, -1.8f, 10.2f, 5.98f, 2.4f);", PLAN)
        # the holes the walls' own openings (ScianaOE), the hall's 3 m openings' reveals lined
        self.assertIn("new Vector4(5.6f, 8.6f, 0f, 3.0f)", NOWA)
        self.assertIn("new Vector4(7.5f, 10.5f, 0f, 3.0f)", NOWA)
        self.assertIn("ScianaOE(elewacja, false, -5f, -10f, 14f, 0f, AE, -1f, false, RogZ(-10f, 14f, true), new Vector4(5.6f, 8.6f, 0f, 3.0f));", PLAN)
        self.assertIn("ScianaOE(elewacja, false, 5f, -10f, 14f, 0f, AE, 1f, false, RogZ(-10f, 14f, true), new Vector4(7.5f, 10.5f, 0f, 3.0f));", PLAN)
        self.assertIn("MM(renderIn, r0, r1, oh - .012f, oh - .001f, o0 + .001f, o1 - .001f, false);", NOWA)

    def test_the_guest_suites_west_wall_plastered(self):
        self.assertIn("MM(renderIn, 15.98f, 16.01f, y + 2.30f, top + .01f, 11.50f, 12.60f, false);", NOWA)


class Wspornik(unittest.TestCase):
    """22:32 "Coś mi tam brakuje płaski prostokąt ... zobacz na internecie jak to robią": the upper floors out over the
    terrace, the thin roofs, louvres, outdoor rooms; 22:43 "teraz to zaczyna wyglądać"."""

    def test_the_upper_floors_cantilevered_3_m(self):
        self.assertIn("const float WS = 3f;", PLAN)
        for side in ("ScianaOE(elewacja, false, -16f, -8f - WS, 14f, AF1, AE", "ScianaOE(elewacja, false, 8f, -8f - WS, 14f, AF1, AE"):
            self.assertIn(side, PLAN)
        self.assertIn("float i0 = x0 + T, i1 = x1 - T, m = (x0 + x1) * .5f, zu = z - WS;", PLAN)
        # the slab's band to the walls' line only (its ends had run 5 cm along the ground walls' faces: z-fighting)
        self.assertIn("MM(elewacja, x0, x1, AF1 - .3f, AF1 - .04f, zu, z); MM(elewacja, i0, i1, AF1 - .3f, AF1 - .04f, z, z + .05f);", PLAN)
        self.assertNotIn("MM(elewacja, x0, i0, AF1 - .04f, AF1, zu, z + .05f", PLAN)
        self.assertIn("RamaProsta(x0, x1, AE, zu, -1f, AF1 - .3f);", PLAN)

    def test_thin_roofs_louvres_and_outdoor_rooms(self):
        self.assertIn("const float Dach = .32f;", NOWA)
        body = NOWA[NOWA.index("static void DachPlaski"):NOWA.index("static void RamaProsta")]
        self.assertNotIn("parapet", body.split("{", 1)[1].lower())
        self.assertIn("Lamele(-16f, -1f, -8f - WS + .1f, 14f - .1f, new Vector2(-6.3f, -2.1f), new Vector2(7.5f, 9.3f));", NOWA)
        self.assertIn("for (float z = z0 + .03f; z < z1 - .03f; z += .16f)", NOWA)
        self.assertIn("foreach (float cx in new[] { -12f, 12f })", NOWA)
        self.assertIn("Lezaki(new Vector3(18.0f, y + .04f, 12.3f), 3, 2.2f, Vector3.back);", NOWA)

    def test_the_roof_terrace_rails_off_the_decks_edges(self):
        self.assertIn("MM(soffit, 16.4f, 26.1f, y, y + .04f, 3.9f, 14.1f, false);", NOWA)
        self.assertIn("Balustrada(true, 3.9f + .06f, 16.42f, 26.06f, y + .04f);", NOWA)
        self.assertIn("Balustrada(false, 26.1f - .06f, 3.93f, 14.07f, y + .04f);", NOWA)
        # the long runs' ends (26.06) inside the east run's base (26.1 - .06 +- .04), its ends inside theirs
        self.assertTrue(26.1 - .06 - .04 < 26.06 < 26.1 - .06 + .04)
        self.assertTrue(3.9 + .06 - .04 < 3.93 < 3.9 + .06 + .04 and 14.1 - .06 - .04 < 14.07 < 14.1 - .06 + .04)

    def test_the_rooms_out_over_the_terrace_furnished(self):
        # bedroom 1: a bench at the bed's foot, two lounge chairs and a table at the glass; the master: its sofa moved out
        self.assertIn("LawkaLozka(c, new Vector3(-13.6f, y, -2.80f - .30f), 1.5f);", PLAN)
        self.assertIn("LawkaLozka(c, new Vector3(12.2f, y, -1.90f - .30f), 1.5f);", PLAN)
        self.assertEqual(PLAN.count('"fotel przy szybie"'), 3)                                    # the living room's, bedroom 1's, the master's
        self.assertIn('BraxKatalog.Sofa(c, new Vector3(12.0f, y + .012f, -7.6f), Vector3.back, 2.4f, 1, "sofa");', PLAN)
        self.assertIn("Oczka(-14.6f, -9.8f, -9.2f, -1.6f, top);", PLAN)
        self.assertIn("Oczka(9.4f, 14.2f, -9.2f, -.8f, top);", PLAN)

    def test_no_wardrobe_across_a_door_or_a_window(self):
        # the master's walk-in: the bathroom's door x 13.6..14.5 on its back wall; wardrobes either side of it
        self.assertIn('Lazienka(10.26f, 15.7f, 4.66f, 9.94f, y, new Vector3(1f, 13.6f, 14.5f), 0f, 0f);', PLAN)
        runs = [(float(cx), float(w)) for cx, w in re.findall(r"BraxKatalog\.Wardrobe\(c, new Vector3\(([\d.]+)f, y, 3\.94f\), Vector3\.back, ([\d.]+)f", PLAN)]
        self.assertEqual(len(runs), 2)
        for cx, w in runs:
            self.assertTrue(cx + w / 2 < 13.6 - .15 or cx - w / 2 > 14.5 + .15, (cx, w))
        # the guest suite: its east window z 5.5..9 (AF1 + .8 .. AE - .6); the wardrobe north of it
        self.assertIn("ScianaOE(elewacja, false, 26f, 4f, 14f, AF1, AE, 1f, true, RogZ(4f, 14f, false), new Vector4(5.5f, 9f, AF1 + .8f, AE - .6f));", PLAN)
        m = re.search(r"BraxKatalog\.Wardrobe\(c, new Vector3\(25\.08f, y, ([\d.]+)f\), Vector3\.left, ([\d.]+)f", PLAN)
        self.assertIsNotNone(m)
        self.assertGreater(float(m.group(1)) - float(m.group(2)) / 2, 9.0)
        self.assertLess(float(m.group(1)) + float(m.group(2)) / 2, 10.2 - .06 - .04)
        self.assertLessEqual(25.08 + .61, 25.7)                                                  # its back (D .60 + the top's 1 cm) at the wall


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
