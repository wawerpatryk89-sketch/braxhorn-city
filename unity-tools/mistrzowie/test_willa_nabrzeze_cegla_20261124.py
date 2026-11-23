"""The seafront villa in old brick with its private beach (owner 23.11 23:57-24.11 00:00: "willa nad morzem. Tam musi być tak
samo zrobione porządnie ... I mają mieć prywatną plażę", "Zrób starą cegłę elewację i w ten sposób luksusową", "do willi
palmy, zrób taki stare, te mury, ale taki luksus"; 23:10 "Pamiętaj, nie ma szarych ścian").

Old red brick walls with travertine quoins and a string course, old stone retaining walls, cream walls inside and a warm
floor, beds against their walls (the catalogue's p is a bed's foot), nothing across a door or a window, the house's doors
into the wings framed; a private beach: sand from the stair's foot to the sea between two old stone walls, a boardwalk,
a deck with a cabana and a lounge, loungers under parasols, palms; an avenue of palms along the drive."""
import json
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"
TOOLS = ROOT / "unity-tools/mistrzowie"
CEGLA = (ED / "BraxChampionsWillaNabrzezeCegla.cs").read_text(encoding="utf-8")
PLAN = (ED / "BraxChampionsWillaNabrzeze.cs").read_text(encoding="utf-8")
KLIF = (ED / "BraxChampionsWillaKlif.cs").read_text(encoding="utf-8")
KIT = (ED / "BraxChampionsWilleKitSrodziemne.cs").read_text(encoding="utf-8")
SITES = (ED / "BraxChampionsWilleDzialki.cs").read_text(encoding="utf-8")
FUND = (ED / "BraxChampionsFoundation.cs").read_text(encoding="utf-8")
PREP = (TOOLS / "prepare_assets.py").read_text(encoding="utf-8")
FIX = json.loads((TOOLS / "pbr-provider-preflight.json").read_text(encoding="utf-8"))
NEW = ("medieval_red_brick", "castle_wall_varriation")
T = .3


class Cegla(unittest.TestCase):
    def test_from_his_list_in_4k(self):
        lista = (ROOT / "MATERIALY-WLASCICIELA.md")
        for s in NEW:
            if lista.exists():
                self.assertIn(s, lista.read_text(encoding="utf-8"))
            self.assertIn("'" + s + "'", PREP)
            for key in ("Diffuse", "nor_gl", "Rough", "arm"):
                self.assertIn("4k", FIX[s][key], s)
                self.assertTrue(all("/4k/" in f["url"] for f in FIX[s][key]["4k"].values()), s)
            self.assertIn('{"' + s + '",new Vector3(', FUND)                         # its brightest 1 %
            self.assertIn('"' + s + '"', CEGLA)
        self.assertTrue((ED / "BraxChampionsWillaNabrzezeCegla.cs.meta").exists())
        self.assertIn("BraxChampionsWillaNabrzezeCegla.cs", (TOOLS / "wille_preview/wille_preview.csproj").read_text())

    def test_the_walls_in_old_brick(self):
        self.assertIn('cegla = F("willa3_cegla_stara", "medieval_red_brick", 2f,', CEGLA)
        self.assertIn("KDom(cegla, true); NSkrzydla(); N3Kamien();", PLAN)
        self.assertIn("var zew = cegla;", PLAN)
        self.assertNotIn("tynkBez", PLAN.replace("(was the beige stucco)", ""))
        # the chimneys in brick, the eaves' soffit cream (not brick), the porch's ceiling cream
        self.assertIn("MMd(nrWilli == 3 && cegla != null ? cegla : tynkKrem,", KIT)
        self.assertIn("var podbitka = cegla != null && sciana == cegla ? tynkKrem : sciana;", KIT)
        self.assertIn("b.Quad(podbitka, q[0], q[1], q[2], q[3]);", KIT)
        self.assertIn("MM(tynkKrem, -16.7f, -9.3f, NEw - .3f, NEw, -5.7f, -1.06f, false);", PLAN)

    def test_the_stone_on_the_brick(self):
        self.assertIn("static void Naroznik(float cx, float cz, float sx, float sz, float y0, float y1)", CEGLA)
        self.assertGreaterEqual(CEGLA.count("Naroznik("), 10)
        # the house's front corners over the wings' eaves (west 4.4 + its roof, east 5.0), into the cornice (20 cm under the top)
        self.assertIn("Naroznik(-9f, 0f, -1f, -1f, 4.8f, KE - .2f); Naroznik(9f, 0f, 1f, -1f, 5.4f, KE - .2f);", CEGLA)
        self.assertIn("MM(deck, -8.99f, 8.99f, KF1 - .2f, KF1 - .03f, -.04f, .01f, false);", CEGLA)  # the string course
        # the stone round the arches stays (the travertine surrounds, keystones, sills)
        self.assertIn("static Material Opaska_ => deck;", KIT)

    def test_old_stone_walls(self):
        self.assertIn('BraxChampionsFoundation.Fotoskan("willa3_mur_stary", "castle_wall_varriation", 2f,', CEGLA)
        self.assertIn("murOporowy = murStary;", CEGLA)
        self.assertIn("if (d.rodzaj == Rodzaj.Nabrzeze) mur = BraxChampionsWille.MurStaryNabrzeza();", SITES)


class BezSzarego(unittest.TestCase):
    def test_cream_walls_warm_floor_brick_fireplace(self):
        self.assertIn('renderIn = Inside(Tynk("willa3_sciany_krem", new Color(.93f, .88f, .79f), 2f), .80f);', CEGLA)
        self.assertIn('floorGf = Inside(F("willa3_podloga_parter", "wooden_floor_02",', CEGLA)
        self.assertIn('stoneIn = Inside(F("willa3_kominek_cegla", "medieval_red_brick",', CEGLA)
        self.assertIn("MM(marbleIn, -8.5f, -7.9f, 1.25f, 1.4f, 1.2f, 5.0f);", PLAN)
        # the arches' soffits: their inner half the room's plaster (the brick showed inside round the windows)
        self.assertEqual(KIT.count("Q(wew, P(u0, y0, t * .5f), P(u1, y1, t * .5f), P(u1, y1, t), P(u0, y0, t), centre - c, true);"), 2)

    def test_the_guest_suites_brick_wall_plastered(self):
        self.assertIn("MM(renderIn, 9.001f, 9.012f, .001f, NEe - .28f, .012f, 3.96f, false);", PLAN)


class Wnetrza(unittest.TestCase):
    """BraxKatalog.Bed: p the middle of the bed's FOOT, the head 2.17 m behind it (the headboard 1.48 m tall)."""

    def test_upstairs_beds_against_their_walls(self):
        self.assertIn("BraxKatalog.Bed(c, new Vector3(-5.05f, y + .012f, 4.64f - .03f - 2.17f), Vector3.back, 1.8f", KLIF)
        self.assertIn("BraxKatalog.Bed(c, new Vector3(1.6f + .06f + .03f + 2.17f, y + .012f, 2.4f), Vector3.right, 1.6f", KLIF)
        # the master's bed (1.8: with its bedside tables 3.2 m) between the bathroom's and the dressing room's doors
        lo, hi = -5.05 - 1.6, -5.05 + 1.6
        for d0, d1 in ((-7.8, -7.0), (-3.1, -2.3), (.5, 1.4)):
            self.assertTrue(hi < d0 - .08 - .1 or lo > d1 + .08 + .1, (d0, d1))
        self.assertIn("new Vector2(-7.8f, -7.0f), new Vector2(-3.1f, -2.3f), new Vector2(.5f, 1.4f), new Vector2(3.3f, 4.2f), new Vector2(5.5f, 6.3f)", KLIF)

    def test_the_partition_clear_of_the_balcony_door_and_the_well_closed(self):
        self.assertIn("Scianka(false, 1.6f, .3f, 4.7f, y, top);", KLIF)
        self.assertGreater(1.6 - .06, .8)                                         # the balcony's French door x -.8..0.8
        self.assertIn("Scianka(true, 4.7f, -8.7f, 8.7f, y, top,", KLIF)          # bedroom 2 closed from the stair's well

    def test_no_tv_or_wardrobe_before_a_window(self):
        # bedroom 2's east window z 2..3.1: the TV (55 in, 1.22 m) at z 1.2, the wardrobe on the back wall
        self.assertIn('BraxKatalog.Tv(c, new Vector3(8.7f - .02f, y + 1.35f, 1.2f), Vector3.left, 55f, 1, false, tag + " sypialnia 2 TV");', KLIF)
        self.assertLess(1.2 + .61, 2.0)
        self.assertIn('BraxKatalog.Wardrobe(c, new Vector3(7.55f, y, 4.64f - .61f), Vector3.back, 2.0f,', KLIF)
        self.assertGreater(7.55 - 1.0, 6.3 + .082)                                # clear of the bathroom door's architrave
        self.assertNotIn('Vector3(-8.68f, y + 1.35f, 2.4f)', KLIF)               # the master's TV was on its west window

    def test_the_salon_tv_over_the_fireplace(self):
        self.assertIn('BraxKatalog.Tv(c, new Vector3(-8.5f + .02f, 2.2f, 3.1f), Vector3.right, 65f, 2, false, tag + " salon TV");', PLAN)
        self.assertIn("Obrazek(3, new Vector3(2.8f, 2.0f, 7.54f), Vector3.back, 2.2f, 1.3f);", PLAN)
        self.assertGreater(2.8 - 1.1 - .04, 1.2)                                  # between the openings 1.2 .. 4.4
        self.assertLess(2.8 + 1.1 + .04, 4.4)

    def test_the_guest_suite(self):
        self.assertIn('BraxKatalog.Bed(c, new Vector3(9.012f + .03f + 2.17f, .012f, 1.9f), Vector3.right, 1.6f, "łóżko");', PLAN)
        self.assertIn('BraxKatalog.Wardrobe(c, new Vector3(12.17f, 0f, 3.94f - .61f), Vector3.back, 2.0f,', PLAN)
        self.assertGreater(12.17 - 1.0, 10.9 + .082)                              # clear of the garage door
        # the dining room's door into the suite closed; the house's doors into the wings framed
        self.assertNotIn("new Vector4(1f, 2f, 0f, 2.25f), new Vector4(7.5f, 8.4f, 0f, 2.25f)", KLIF)
        self.assertIn('Drzwi("kuchnia", false, -9f + T * .5f, 5f, 6.2f, 0f, 2.25f, .018f, .018f, T * .5f);', KLIF)
        self.assertIn('Drzwi("garaż z domu", false, 9f - T * .5f, 7.5f, 8.4f, 0f, 2.25f, .018f, .018f, T * .5f);', KLIF)


class PrywatnaPlaza(unittest.TestCase):
    def test_sand_from_the_stairs_foot_to_the_sea(self):
        self.assertIn("public const float N3PlazaX0 = -56f, N3PlazaX1 = 46f, N3PlazaZ = PadZ0 - 4.4f;", CEGLA)
        self.assertIn("f = Mathf.Min(f, l.y - BraxChampionsWille.N3PlazaZ);", SITES)
        self.assertIn("float hp = Pole(vl[p]), hq = Pole(vl[q]), t = Mathf.Clamp01(hp / (hp - hq));", SITES)
        # the stair's foot (23 treads of 30 cm) on the sand
        self.assertLess(-22.6 - 23 * .30, -22.6 - 4.4)

    def test_furnished(self):
        self.assertIn("N3Plaza();", PLAN)
        for s in ("MM(teak, sx - .8f, sx + .8f,", "MM(teak, N3DeckX0, N3DeckX1, dn - .3f, dt, N3DeckZ0, N3DeckZ1);",
                  'W.Siedzisko(new Vector3(x, dt + .5f, -34.6f), Vector3.back, "LEŻANKA");', "Lawka(new Vector3(-17f, dt, -33.0f), Vector3.back, 2.4f);",
                  "Lezaki(new Vector3(x, y, z), 2, 1.1f, Vector3.back);", "N3Parasol(x + .55f, z + 1.6f);"):
            self.assertIn(s, CEGLA)
        loungers = re.search(r"foreach \(float x in new\[\] \{ ([^}]+) \}\)\s*\{\s*float z = -50f", CEGLA)
        self.assertEqual(len(loungers.group(1).split(",")), 6)
        palms = re.search(r"foreach \(float x in new\[\] \{ ([^}]+) \}\)[^\n]*\n\s*\{ Palma\(x, -29f", CEGLA)
        xs = [float(v.strip().rstrip("f")) for v in palms.group(1).split(",")]
        for x in xs:
            self.assertTrue(x < -26 - 2 or x > -14 + 2, x)                        # clear of the deck and the stair
            self.assertTrue(abs(x) >= 9.5, x)                                     # none before the house (x -9..9)
            self.assertTrue(-56 < x < 46, x)

    def test_closed_by_old_stone_walls_into_the_sea(self):
        self.assertIn("N3Mur(N3PlazaX0, 88f, N3Morze, 1.8f, morze);", CEGLA)
        self.assertIn("N3Mur(N3PlazaX1, N3PlazaZ, N3Morze, 1.2f, morze);", CEGLA)
        self.assertIn("const float N3Morze = -76f;", CEGLA)
        self.assertIn("top = Mathf.Max(N3Max(x - .45f, x + .45f, za, z) + h, morze + .9f);", CEGLA)
        self.assertIn('"PLAŻA PRYWATNA"', CEGLA)
        # the west wall stops short of the drive (the road along local z 101.5, 3.5 m half width)
        self.assertLess(88, 101.5 - 3.5 - 4)


class Palmy(unittest.TestCase):
    def test_an_avenue_along_the_drive(self):
        self.assertIn("if ((hill || shore) && d.droga != null && palm)", SITES)
        self.assertNotIn("if (d.rodzaj == Rodzaj.Nabrzeze) return;\n        // the hill: a garden", SITES)
        self.assertIn("MISTRZOWIE_WILLA_ALEJA_NABRZEZE palms=", SITES)
        self.assertIn("public static int AlejaNabrzeze;", SITES)

    def test_no_garden_palms_on_the_beach(self):
        self.assertIn("(l.y < BraxChampionsWille.N3PlazaZ + 2f && l.x > BraxChampionsWille.N3PlazaX0 - 1f && l.x < BraxChampionsWille.N3PlazaX1 + 1f)", SITES)


if __name__ == "__main__":
    unittest.main()
