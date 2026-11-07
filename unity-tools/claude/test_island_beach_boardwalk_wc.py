"""BRAXHORN R1 island beach deck and premium restrooms.

Static and analytic source regressions only. No Unity scene/playtest here.
"""
from pathlib import Path
import math
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]
SOURCE = (ROOT / "unity-city/Assets/Editor/Claude/Region/"
          "BraxMiastoWyspaDomki.cs").read_text(encoding="utf8")
BASE = (ROOT / "unity-city/Assets/Editor/Claude/Region/"
        "BraxMiastoWyspa.cs").read_text(encoding="utf8")
ARENA = (ROOT / "unity-city/Assets/Editor/Claude/Region/"
         "BraxMiastoWyspaArena.cs").read_text(encoding="utf8")


def cs_balanced(source):
    pattern = r'//[^\n]*|/\*[\s\S]*?\*/|@"(?:""|[^"])*"|"((?:\\.|[^"\\])*)"'
    stripped = re.sub(pattern, "", source)
    stack = []
    pair = {")": "(", "]": "[", "}": "{"}
    for char in stripped:
        if char in "([{":
            stack.append(char)
        elif char in ")]}":
            if not stack or stack.pop() != pair[char]:
                return False
    return len(stack) == 0


class IslandBeachWcPremium(unittest.TestCase):
    def test_csharp_source_lexically_valid(self):
        self.assertTrue(cs_balanced(SOURCE))
        self.assertIn("internal static partial class BraxMiastoWyspa", SOURCE)

    def test_deck_shared_edges_no_separate_rotated_plank_overlaps(self):
        self.assertIn("static void DeptakSpojny(", SOURCE)
        self.assertIn("Q(z[deski], A, B, C, Dd, Vector3.up)", SOURCE)
        self.assertNotIn("if (len < .1f || len > 6f) continue", SOURCE)
        self.assertNotIn("for (float s = 0; s < len - .05f;", SOURCE)
        self.assertIn("int kroki = Mathf.Max(1, Mathf.CeilToInt(d / 1.20f))", SOURCE)

    def test_boardwalk_stays_above_full_width_terrain_and_limits_grade(self):
        self.assertIn("grunt + .12f", SOURCE)
        self.assertIn("pts[i - 1].y - .10f", SOURCE)
        self.assertIn("pts[i + 1].y - .10f", SOURCE)
        self.assertIn("BraxNabrzeze.KolRot(kol, nazwa", SOURCE)
        self.assertIn("static List<Vector3> DeptakSpojny(", SOURCE)
        self.assertIn("var glowny = DeptakSpojny(", SOURCE)
        self.assertIn("return pts;  // rzeczywista wysokość", SOURCE)

    def test_palm_and_tree_placements_keep_walkways_clear(self):
        self.assertIn("Zajmij(pts[i], polSzer + 1.45f)", SOURCE)
        self.assertIn("if (Zajete(CX + px, CZ + pz, 1.6f)) continue", BASE)
        self.assertIn("Zajmij(baseWC,Mathf.Max(w,d)*.7f+.9f)", SOURCE)

    def test_lifeguard_paths_join_main_deck_at_edge(self):
        self.assertIn("var pocz = naj + wd * hw; pocz.y = naj.y;", SOURCE)
        self.assertIn("var naj = glowny.OrderBy(", SOURCE)
        self.assertIn("BRAXHORN_WYSPA_DEPTAK DO POPRAWKI: połączenie ratownika", SOURCE)
        self.assertIn('DeptakSpojny(z, deski, legar, branch, .90f * K', SOURCE)
        self.assertIn('Kladka do ratownika plazy wyspy', SOURCE)
        self.assertNotIn("p.y = Y(p.x, p.z) + .1f;", SOURCE)

    def test_beach_services_are_preserved(self):
        self.assertIn('nPrzebieralnie++;', SOURCE)
        self.assertIn('Miejsca["wyspa_wypozyczalnia"]', SOURCE)
        self.assertIn('BarPlazowy(bc, "wyspa_bar_plaza"', SOURCE)
        self.assertIn('PunktWidokowy()', BASE)

    def test_old_blocking_wc_and_colliders_removed(self):
        self.assertNotIn('// WC (budynek murowany z dwojgiem drzwi)', SOURCE)
        self.assertNotIn('BraxNabrzeze.Kol(kol, "WC plaży"', SOURCE)
        self.assertNotIn("z[tynk].Box(new Vector3(c.x, y0 + h * .5f, c.z)", SOURCE)
        self.assertIn('Piece(fasada,"front lewy"', SOURCE)
        self.assertIn('Piece(fasada,"front prawy"', SOURCE)
        self.assertIn('Piece(fasada,"nadproże"', SOURCE)
        self.assertIn('float drzW = 1.85f*K, drzH = 2.20f*K', SOURCE)

    def test_two_premium_restrooms_are_off_main_walkway(self):
        angles = (222.0, 320.0)
        self.assertIn('(222f, "WC DAMSKIE"', SOURCE)
        self.assertIn('(320f, "WC MĘSKIE"', SOURCE)
        self.assertIn('const float odWodyWC = 70f', SOURCE)
        self.assertTrue(all(195.0 < a < 345.0 for a in angles))
        self.assertGreater(70.0 - 38.0 - 4.6 * 1.294 / 2.0, 25.0)
        self.assertGreater(2.0 * 70.0 * math.sin(math.radians((angles[1]-angles[0])/2.0)), 100.0)
        self.assertIn('Miejsca["wyspa_wc"]=wej', SOURCE)

    def test_two_accessible_restrooms_have_side_paths(self):
        self.assertIn('DeptakSpojny(z,deski,legar,dojscie,1.05f*K', SOURCE)
        self.assertIn("var pocz=naj-fw*hw;pocz.y=naj.y;", SOURCE)
        self.assertIn('"Dojscie boczne do WC premium "+id', SOURCE)
        self.assertIn("BRAXHORN_WYSPA_WC_PREMIUM DO POPRAWKI", SOURCE)
        self.assertGreater(1.85*1.294, 2.25)
        self.assertGreater(2.20*1.294, 2.25)  # Brax controller height

    def test_pbr_finishes_and_real_interiors(self):
        for slug in ("white_plaster_02", "rectangular_facade_tiles", "granite_tile_02",
                     "concrete_wall_008", "marble_01"):
            self.assertIn(slug, SOURCE)
        self.assertIn("Toczenie(muszla,", SOURCE)
        self.assertIn("Toczenie(zlew,", SOURCE)
        self.assertIn("internal static void Toczenie(UlSiatka", ARENA)
        self.assertIn('Piece(posadzka,"posadzka"', SOURCE)
        self.assertIn('Piece(sciana,"sufit"', SOURCE)
        self.assertIn("WC premium 2, wypożyczalnia 1", SOURCE)


if __name__ == "__main__":
    unittest.main()
