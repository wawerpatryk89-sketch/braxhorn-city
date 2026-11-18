"""Owner 18.11.2026 04:40-04:47 on the villas: "Co to za wykończenie jak to wygląda jak zabawki", his zoom of villa 3's facade
(bare arched holes in the stucco with a thin black square in them, the stucco running straight into the tiles) - "Zobacz
jak ten ok a wyglądają elewacja wykończenie premium proszę" - and the loungers and the sofa ("Ty to mierzysz skalujesz?":
the sizes were right, the build was a plank on four sticks and a block). The premium finish: stone surrounds, keystones and
sills round every arched opening, walnut casements with glazing bars, the arcades' archivolts, a cornice and the barrel
tiles' round ends at the eaves, wrought-iron rails, real teak loungers and sofas in oiled teak and cotton, light
travertine retaining walls; on the island the east wall clad in teak, a deep canopy on square columns."""
import math
import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"
KIT = (ED / "BraxChampionsWilleKitSrodziemne.cs").read_text(encoding="utf-8")
VILLA = (ED / "BraxChampionsWille.cs").read_text(encoding="utf-8")
WKIT = (ED / "BraxChampionsWilleKit.cs").read_text(encoding="utf-8")
SEA = (ED / "BraxChampionsWillaNabrzeze.cs").read_text(encoding="utf-8")
ISLAND = (ED / "BraxChampionsWillaWyspa.cs").read_text(encoding="utf-8")
HILL = (ED / "BraxChampionsWillaKlif.cs").read_text(encoding="utf-8")
SITES = (ED / "BraxChampionsWilleDzialki.cs").read_text(encoding="utf-8")


def body(src, sig):
    i = src.index(sig)
    j = src.find("\n    static ", i + len(sig))
    return src[i:] if j < 0 else src[i:j]


class Elewacja(unittest.TestCase):
    def test_every_arched_opening_has_its_stone_surround(self):
        self.assertIn("const float OpW = .16f, OpD = .045f;", KIT)
        okno = body(KIT, "static void OknoLuk(")
        self.assertIn("Opaska(alongX, plane, outward, a0, a1, fanlight ? 0f : sill, ys, opW);", okno)
        self.assertIn("if (szklo && !fanlight && sill > .05f) Parapet(alongX, plane, outward, a0, a1, sill, opW);", okno)
        opaska = body(KIT, "static void Opaska(")
        # the jambs, the band round the arch, the keystone on the full surround
        self.assertEqual(opaska.count("WB(Opaska_"), 3)
        self.assertIn("Pierscien(Opaska_, alongX, plane, outward, am, ys, r, r + w, 0f, Mathf.PI, OpD, 20);", opaska)
        self.assertIn("if (w >= OpW)", opaska)
        # the oculi: a 10 cm ring; the arcades' archivolts
        self.assertIn("float opW = .1f)", KIT)
        self.assertIn("if (wew == zew && zew != renderIn) { Out_(true); Pierscien(Opaska_,", KIT)
        # the surround stone is the terrace's travertine (Poly Haven), not a flat colour
        self.assertIn("static Material Opaska_ => deck;", KIT)
        self.assertIn('deck = BraxChampionsFoundation.LibraryTravertine("willa_taras_trawertyn"', VILLA)

    def test_no_surround_face_on_a_wall_face(self):
        # 18.11 z-fight check: the sill stops 1.2 cm short of the wall's two skins' joint, the jambs and the inner rims start
        # on the wall's face (the opening's own reveal goes on from there), the keystone sits on the face
        self.assertIn("sill - .07f, sill + .01f, T * .5f - .012f, -(OpD + .035f));", KIT)
        self.assertIn("WB(Opaska_, alongX, plane, outward, a0 - w, a0, yb, ys, 0f, -OpD);", KIT)
        self.assertIn("A(r0, u1, 0f), A(r0, u0, 0f), -radial);", KIT)
        self.assertIn("ys + r + w + .08f, 0f, -(OpD + .025f));", KIT)
        # the cornice's bead (7 cm) stands clear of the oculi's rings (4.5 cm)
        self.assertIn("mid + outN * .035f + Vector3.down * .13f, Sz(.07f,", KIT)
        self.assertGreater(.07 - .045, .006)

    def test_walnut_casements_with_glazing_bars(self):
        sk = body(KIT, "static void Skrzydla(")
        self.assertIn("if (a1 - a0 > .85f) R(am - .045f, am + .045f", sk)          # two leaves over 85 cm
        self.assertIn("Mathf.RoundToInt((y1 - y0 - .11f) / .5f)", sk)              # panes about half a metre
        self.assertIn("MM(walnut,", sk)
        okno = body(KIT, "static void OknoLuk(")
        self.assertNotIn("Szyba(", okno)                                          # no black aluminium square
        self.assertIn("foreach (float deg in new[] { 45f, 90f, 135f })", okno)     # the fanlight's spokes

    def test_the_eaves(self):
        dach = body(KIT, "static float Dach4(")
        self.assertIn("var gzyms = sciana == tynkKrem ? deck : tynkKrem;", dach)
        self.assertIn("b.Quad(gzyms, fq[0], fq[1], fq[2], fq[3]);", dach)          # the fascia
        # the barrel tiles' ends: two of the texture's 23 courses per 2.5 m
        self.assertIn('dachowka = BraxChampionsFoundation.SourceMaterial("willa_dachowka", "clay_roof_tiles_02", 2.5f,', VILLA)
        self.assertIn("for (float t = .3f; t <= len - .3f; t += .217f)", dach)
        self.assertAlmostEqual(2 * 2.5 / 23, .217, places=3)
        self.assertIn("const float R = .075f, Ls = .2f;", dach)
        for side in ("Gasiory(c00, c10, Vector3.back);", "Gasiory(c11, c01, Vector3.forward);", "Gasiory(c01, c00, Vector3.left);", "Gasiory(c10, c11, Vector3.right);"):
            self.assertIn(side, dach)

    def test_the_finish_clears_the_eaves(self):
        def corbel_bottom(e, pitch, ov=.6):
            return e + .2 - ov * math.tan(math.radians(pitch)) - .16 - .2
        # the house's upper windows (crown KF1 + 2.7) with band and keystone under the house's corbels
        self.assertLess(4.2 + 2.7 + .16 + .08, corbel_bottom(7.8, 25))
        # the wings' ground windows and arcades (crowns 2.8 and 3.9 with the band)
        self.assertLess(2.8 + .16 + .08, corbel_bottom(4.4, 21))
        # villa 3's big east door: a 10 cm band, no keystone, under its oculus (ry 4.28, rr .37) which has no ring
        self.assertIn("OknoLuk(true, -5f, 11.6f, 14.4f, 2.38f, 2.4f, 3.8f, -1f, zew, true, .1f); OknoOkragle(true, -5f, 13f, ry, rr, -1f, zew, 0f);", SEA)
        self.assertLess(3.8 + .1, 4.28 - .37)

    def test_wrought_iron_rails(self):
        rail = body(SEA, "static void Balkonik(")
        self.assertIn("b.Tube(stalCzarna, P(l0, sill + 1.0f), P(l1, sill + 1.0f), .022f, 10);", rail)
        self.assertIn("b.Surface(stalCzarna, Torus, 14, 4); b.Surface(stalCzarna, Torus, 14, 4, true);", rail)
        self.assertIn("b.Lathe(stalCzarna,", rail)


class Meble(unittest.TestCase):
    def test_real_surfaces(self):
        self.assertIn('teak = BraxChampionsFoundation.SourceMaterial("willa_tik_olejowany", "teak_veneer", 1.0f,', VILLA)
        self.assertIn('cushion = BraxChampionsFoundation.SourceMaterial("willa_poduchy_bawelna", "cotton_jersey", .6f,', VILLA)
        self.assertIn('recznik = BraxChampionsFoundation.SourceMaterial("willa_recznik_granat", "cotton_jersey", .6f,', VILLA)
        self.assertNotIn('BraxChampionsHarbour.Surface("willa_tik"', VILLA)
        self.assertNotIn('BraxChampionsHarbour.Surface("willa_poduchy_lezakow"', VILLA)

    def test_the_lounger_built_as_a_real_one(self):
        lz = body(WKIT, "static void Lezaki(")
        # the sizes stay real: 2.00 x .72 m, the cushion top ~ .35
        self.assertIn("new Vector3(.05f, .1f, 2.0f)", lz)
        self.assertIn("for (int k = 0; k < 13; k++) b.Box(teak,", lz)                 # the slatted seat
        self.assertIn("var qb = q * Quaternion.Euler(-40f, 0f, 0f);", lz)             # the backrest at 40 degrees
        self.assertIn("b.Cylinder(BraxKatalog.BlackMatt,", lz)                        # wheels at the head
        self.assertIn("b.Tube(teak,", lz)                                             # its struts
        self.assertIn("if (i % 2 == 0) b.Cylinder(recznik,", lz)
        self.assertEqual(lz.count("b.RoundedBox(cushion,"), 3)                         # seat, back, head pillow

    def test_the_sofa_built_as_a_real_one(self):
        lw = body(VILLA, "static void Lawka(")
        self.assertIn("D = .78f", lw)                                                   # within the old .8 footprint
        self.assertIn("b.RoundedBox(cushion, L(x, .345f, -.34f)", lw)
        self.assertIn("b.RoundedBox(BraxKatalog.Linen,", lw)
        self.assertIn("foreach (float y in new[] { .32f, .44f, .56f })", lw)          # the slatted arms
        self.assertIn("W.Siedzisko(", lw)


class Wyspa(unittest.TestCase):
    def test_the_east_wall_and_the_canopy(self):
        jad = body(ISLAND, "static void WJadalnia(")
        self.assertIn("MM(teak, cb, ce, nadDrzwiami ? 2.3f : 0f, H0 - .02f, zc - .05f, zc + .05f, false);", jad)
        self.assertIn("MM(render, xa, x1, y, y + .4f, z0, z1);", jad)                  # 40 cm roof
        self.assertIn("MM(stalCzarna, x1 - .4f, x1 - .2f, 0f, y - .02f, zc - .1f, zc + .1f);", jad)
        self.assertIn("MM(gravel, xa, x1 - .06f, y + .4f, y + .42f, z0 + .06f, z1 - .06f, false);", jad)
        self.assertNotIn("b.Cylinder(stalCzarna", jad)                                 # no more sticks
        self.assertIn("Lamele(false, UX1 + .06f, UGlass + .1f, UZ1 - .1f, FFL1 + .02f, Roof - .02f);", ISLAND)
        self.assertIn('new Color(1.42f, 1.41f, 1.38f));   // 18.11', VILLA)

    def test_light_retaining_walls(self):
        self.assertIn('murOporowy = BraxChampionsFoundation.LibraryTravertine("willa_mur_oporowy_trawertyn"', VILLA)
        for src in (VILLA, ISLAND, SEA):
            self.assertNotIn("MM(stone, PadX0, PoolX0, -4f", src)
            self.assertNotIn("MM(stone, PadX0, PadX1, -4f", src)
            self.assertNotIn("MM(stone, WPadX0, PadX0, -4f", src)
        self.assertIn('kamien = BraxChampionsFoundation.LibraryTravertine("wille_mur_kamien_trawertyn"', SITES)


if __name__ == "__main__":
    unittest.main()
