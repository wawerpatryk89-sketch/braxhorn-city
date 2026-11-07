"""MIASTO R1: black massing -> premium existing-building exterior, heritage preserved.

These are source/analytic regressions, not a generated Unity scene or APK walkthrough.
"""
import math
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SOURCE = (ROOT / "unity-city/Assets/Editor/Claude/Region/"
          "BraxRegionMiastoBDzielnice.cs").read_text(encoding="utf-8")


def balanced(source):
    text = re.sub(r'//[^\n]*|/\*[\s\S]*?\*/|@"(?:""|[^"])*"|"((?:\\.|[^"\\])*)"', "", source)
    stack = []
    mate = {")": "(", "]": "[", "}": "{"}
    for char in text:
        if char in "([{":
            stack.append(char)
        elif char in ")]}":
            if not stack or stack.pop() != mate[char]:
                return False
    return not stack


def modern_names():
    lines = re.findall(r'new Bud\("([^"]+)",\s*"([^"]+)"', SOURCE)
    return [ident for ident, model in lines if model in (
        "miasto_centrum_c5", "wiezowiec_london", "wiezowiec_atlanta")]


class R1PremiumDistrictFacades(unittest.TestCase):
    def test_source_is_syntactically_balanced(self):
        self.assertTrue(balanced(SOURCE))

    def test_scope_preserves_historic_buildings(self):
        self.assertIn('b.plik == "miasto_centrum_c5"', SOURCE)
        self.assertIn('b.plik == "wiezowiec_london"', SOURCE)
        self.assertIn('b.plik == "wiezowiec_atlanta"', SOURCE)
        self.assertNotIn('b.plik == "miasto_kamienice_c3" ||', SOURCE)
        self.assertIn('if (!NdNowoczesny(b)) return null;', SOURCE)
        self.assertIn('new Bud("KINNAIRD"', SOURCE)
        self.assertGreaterEqual(len(modern_names()), 12)

    def test_modern_buildings_are_clad_not_replaced(self):
        self.assertIn("BraxMiastoAssety.Przygotuj(bud.gameObject", SOURCE)
        self.assertIn("var originalBounds = BraxRegion.Bounds(bud.gameObject)", SOURCE)
        self.assertIn("NdPremiumFasada(b, bud, g, originalBounds)", SOURCE)
        self.assertIn("var gr = originalBounds;", SOURCE)
        self.assertIn("group.SetParent(bud,true);", SOURCE)

    def test_upper_facades_dont_block_street_entrances(self):
        self.assertIn("Mathf.Max(original.min.y, bud.position.y) + 3.5f * K", SOURCE)
        self.assertIn("group,glazed,false,false", SOURCE)
        self.assertIn("group,glazedAlt,false,false", SOURCE)
        self.assertIn("group,stone,false,false", SOURCE)
        self.assertIn("group,aluminum,false,false", SOURCE)
        self.assertGreater(3.5 * 1.294, 4.0)

    def test_actual_glazing_pbr_mullions_and_floor_spandrels(self):
        for term in ("new BraxRegionMeble.Bud()", "PREMIUM szkło refleksyjne",
                     "PREMIUM okna naprzemienne", "PREMIUM ceramiczne pasy z PBR",
                     "PREMIUM lizeny i rygle aluminium"):
            self.assertIn(term, SOURCE)
        self.assertIn('BraxRegion.Pbr("Miasto: premium pasy stropowe Poly Haven PBR"', SOURCE)
        self.assertIn('BraxRegion.HasTexture("concrete_wall_008")', SOURCE)
        self.assertIn("Panel(spandrels", SOURCE)
        self.assertIn("Panel(frames", SOURCE)

    def test_windows_cover_four_facades_without_corner_overlap(self):
        for v in ("Vector3.right,Vector3.back,b.w",
                  "Vector3.left,Vector3.forward,b.w",
                  "Vector3.back,Vector3.left,b.d",
                  "Vector3.forward,Vector3.right,b.d"):
            self.assertIn(v, SOURCE)
        self.assertIn("float span=(length-.64f)/bays;", SOURCE)
        self.assertIn("float left=.32f+bay*span", SOURCE)
        self.assertIn("Mathf.Clamp(Mathf.CeilToInt", SOURCE)
        # analytic check: window inset 0.32m on each side
        for length in (12.0, 37.53, 78.74, 125.37):
            bays = min(36, max(3, math.ceil(length / (3.2 * 1.294))))
            width = (length - .64) / bays
            self.assertGreater(width, 2.0)
            self.assertAlmostEqual(.32 + bays * width, length - .32, places=5)

    def test_lod_contains_authored_panels_and_glb_original(self):
        self.assertIn("bryla = bryla.Concat(fasada.GetComponentsInChildren<Renderer>(true)).ToArray()", SOURCE)
        self.assertIn("new LOD(.006f, bryla)", SOURCE)

    def test_historic_cleanup_only_removes_isolated_modern_overhang(self):
        self.assertIn('p.id == "KINNAIRD" || p.plik == "miasto_kamienice_c3"', SOURCE)
        self.assertIn('x.name == "Budynek " + old.id', SOURCE)
        self.assertIn("areaX < 10f || areaZ < 5f", SOURCE)
        self.assertIn("owner == monument ? 31f : 25f", SOURCE)
        self.assertIn("bound.size.y > 16f", SOURCE)
        self.assertIn("if (!modern || rend.transform.childCount != 0)", SOURCE)
        self.assertIn("rend.gameObject.SetActive(false)", SOURCE)
        self.assertIn("owner in ndBudynki.Where(x => x)", SOURCE)
        self.assertNotIn("DestroyImmediate(monument", SOURCE)
        self.assertNotIn("DestroyImmediate(owner", SOURCE)

    def test_new_glazing_is_not_a_static_occluder(self):
        self.assertIn("StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic", SOURCE)
        self.assertIn("group.GetComponentsInChildren<Renderer>(true)", SOURCE)

    def test_no_guess_if_original_overhang_is_not_identifiable(self):
        self.assertIn("brak osobnej siatki nowoczesnego nadwieszenia", SOURCE)
        self.assertIn("historyczna_bryla_zachowana=tak", SOURCE)
        self.assertIn("DO POPRAWKI: brak gotowej tekstury Poly Haven PBR", SOURCE)


if __name__ == "__main__":
    unittest.main()
