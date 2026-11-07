"""MIASTO R1 island pier: preserve end connection and non-overlapping decking.

Source-only geometric arithmetic. No Unity editor, no APK, no asset changes.
"""
import math
import unittest
from pathlib import Path

SOURCE=(Path(__file__).resolve().parents[2]/
        "unity-city/Assets/Editor/Claude/Region/BraxMiastoWyspa.cs").read_text(encoding="utf-8")


class IslandPierWalkwayBoundary(unittest.TestCase):
    def test_terminates_exactly_at_arena_platform(self):
        self.assertIn("int sections = Mathf.CeilToInt(Mathf.Max(0f, xa - x1));",SOURCE)
        self.assertIn("float x = step == sections ? xa : x1 + step;",SOURCE)
        self.assertIn("if (sections < 1)",SOURCE)
        self.assertNotIn("for (float x = x1; x <= xa + .01f; x += 1f)",SOURCE)
        for length in (0.2, 0.8, 1, 8.01, 25.4, 66.97):
            n=math.ceil(length)
            samples=[length if k==n else k for k in range(n+1)]
            self.assertAlmostEqual(samples[0],0.0)
            self.assertAlmostEqual(samples[-1],length)
            self.assertTrue(all(0<samples[i+1]-samples[i]<=1 for i in range(n)))

    def test_deck_boards_never_protrude_past_next_segment(self):
        self.assertIn("float segment = b.x - a.x;",SOURCE)
        self.assertIn("float boardWidth = Mathf.Min(.14f, segment - t);",SOURCE)
        self.assertIn("float middle = (t + boardWidth * .5f) / segment;",SOURCE)
        self.assertNotIn("t < 1f; t += .15f",SOURCE)
        for segment in (0.06,0.14,0.2,0.8,1):
            positions=[i*.15 for i in range(math.ceil(segment/.15))]
            boards=[(p, min(.14,segment-p)) for p in positions if p<segment-.001]
            self.assertTrue(all(width>0 and start+width<=segment+1e-8 for start,width in boards))

    def test_original_structure_and_materials_untouched(self):
        self.assertIn("float x0 = xs - 52f, x1 = xs + 1f, hw = 1.6f * K;",SOURCE)
        self.assertIn("float tx0 = x0 - 4.0f * K, tz0 = pz - 17f, tz1 = pz + 17f;",SOURCE)
        self.assertIn("BraxMiastoA.Kol(kol, \"pomost wyspy\"",SOURCE)
        self.assertIn("BraxMiastoA.Kol(kol, \"głowica pomostu\"",SOURCE)


if __name__=="__main__":
    unittest.main()
