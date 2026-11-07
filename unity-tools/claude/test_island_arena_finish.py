"""BRAXHORN R1 island arena. Static contract checks, NOT Unity scene or phone proof.

Run: python -m unittest test_island_arena_finish -v
"""
from pathlib import Path
import math
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]
REGION = ROOT / "unity-city" / "Assets" / "Editor" / "Claude" / "Region"
ISLAND = (REGION / "BraxMiastoWyspa.cs").read_text(encoding="utf8")
ARENA = (REGION / "BraxMiastoWyspaArena.cs").read_text(encoding="utf8")


def delimiters_balanced(source: str) -> bool:
    masked = re.sub(r'//[^\n]*|/\*[\s\S]*?\*/|@"(?:""|[^"])*"|"((?:\\.|[^"\\])*)"', "", source)
    stack = []
    opening = {")": "(", "]": "[", "}": "{"}
    for char in masked:
        if char in "([{":
            stack.append(char)
        elif char in ")]}":
            if not stack or stack.pop() != opening[char]:
                return False
    return not stack


class ArenaIslandFinish(unittest.TestCase):
    def test_octagon_is_preserved_but_both_outdoor_rings_are_removed(self):
        self.assertIn('Krok("klatka", ApKlatka)', ARENA)
        self.assertIn('Krok("trybuny"', ARENA)
        self.assertNotIn("Ringi();", ISLAND)
        self.assertNotIn("static void Ringi()", ISLAND)
        self.assertNotIn('"ring plenerowy"', ISLAND)

    def test_paving_is_part_of_original_terrain_not_overlay(self):
        self.assertIn("var tChodnik = new List<int>();", ISLAND)
        self.assertIn("mesh.subMeshCount = 3;", ISLAND)
        self.assertIn("mesh.SetTriangles(tChodnik, 2);", ISLAND)
        self.assertIn("r.sharedMaterials = new[] { mT, mP, mCh };", ISLAND)
        self.assertIn("const int fine = 5;", ISLAND)
        self.assertIn("tSub.Add(aa)", ISLAND)
        self.assertIn("concrete_pavers_03", ISLAND)

    def test_circular_path_and_bridge_connector_fit_flat_arena_area(self):
        self.assertIn("d >= 26f && d <= 31.5f", ISLAND)
        self.assertIn("Mathf.Abs(zz - Plac.y) < 2.5f", ISLAND)
        match = re.search(r"const float PlacR = ([0-9.]+)f", ISLAND)
        self.assertIsNotNone(match)
        self.assertGreater(float(match.group(1)), 31.5)
        self.assertIn("odAreny > 23f && odAreny < 35f", ISLAND)

    def test_furniture_is_outside_annular_paver_path(self):
        self.assertIn("34.5f", ARENA)
        self.assertIn("var b = p + styczna * 2.6f", ARENA)
        self.assertIn("var l = p - styczna * 2.6f", ARENA)
        for k in range(3):
            for side in (-1, 1):
                theta = side * (0.25 + k * 0.35)
                radial = (-math.cos(theta), math.sin(theta))
                tangent = (-radial[1], radial[0])
                for offset in (0, -2.6, 2.6):
                    radius = math.hypot(34.5 * radial[0] + offset * tangent[0],
                                        34.5 * radial[1] + offset * tangent[1])
                    self.assertGreater(radius, 31.5 + 1.0)

    def test_boardwalk_uses_one_smooth_grade_and_continuous_colliders(self):
        self.assertIn("maxRisePerMetre = .10f", ISLAND)
        self.assertIn("prof[0] = new Vector2(prof[0].x, deck)", ISLAND)
        self.assertIn("prof[prof.Count - 1].x, placLevel", ISLAND)
        self.assertIn('new GameObject("kolizja: deptak")', ISLAND)
        self.assertNotIn('new GameObject("kolizja: pochylnia deptaka")', ISLAND)
        self.assertIn("BRAXHORN_WYSPA_DEPTAK wymaga dopasowania terenu", ISLAND)

    def test_poly_haven_wall_and_ceiling_reuse_real_maps(self):
        self.assertIn("BraxRegion.HasTexture(slug)", ARENA)
        self.assertIn("BraxRegion.Pbr(nazwa, metry, kolor, slug)", ARENA)
        for material in ("apAkust", "apStrop", "apTynk", "apSufitM"):
            self.assertIn(f"{material} = ApPolyFinish(", ARENA)
        self.assertIn("concrete_wall_008", ARENA)
        self.assertIn("white_plaster_02", ARENA)
        self.assertIn("return bazowy;", ARENA)

    def test_hanging_roof_and_scoreboard_cables_not_generated(self):
        self.assertNotIn("yC, .008f, 4);", ARENA)
        self.assertNotIn("yC, tc.z + oz * (hw - .3f)), .01f, 4);", ARENA)
        self.assertNotIn("apY0 + ApSufit, tp.z + zz", ARENA)
        self.assertIn("st.Box(new Vector3(tc.x - .16f", ARENA)
        self.assertIn("st.Rura(Dn(0), Dn(1), .1f, 8)", ARENA)

    def test_original_arena_cage_and_video_board_retained(self):
        for phrase in ('Krok("klatka", ApKlatka)',
                       'Krok("rampa i telebim", ApRampa)',
                       'Krok("hol (kasy, bramki)", ApHol)'):
            self.assertIn(phrase, ARENA)

    def test_csharp_lexical_delimiters(self):
        self.assertTrue(delimiters_balanced(ISLAND))
        self.assertTrue(delimiters_balanced(ARENA))


if __name__ == "__main__":
    unittest.main()
