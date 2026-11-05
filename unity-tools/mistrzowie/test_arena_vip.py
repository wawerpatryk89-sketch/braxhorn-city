"""Static geometry regression for the A1 judge-side VIP seating.

This verifies the authored spacing and preservation of the judges' equipment.
It is NOT a Unity render, physics, or Android device test.
"""
import re
import unittest
from pathlib import Path

SOURCE = (Path(__file__).resolve().parents[2] /
          "unity-city/Assets/Editor/Mistrzowie/BraxChampionsArena.cs")


class ChampionVipSeatsTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.code = SOURCE.read_text(encoding="utf-8")
        # Read current positions from the actual generator instead of guessing
        # them from comments or copying fixed data into the test.
        rows = re.search(r'foreach\(float zr in new\[\]\{([^}]+)\}\)', cls.code)
        banks = re.search(r'foreach\(var \(a0,a1\) in new\[\]\{([^}]+)\}\)', cls.code)
        rule = re.search(
            r'for\(float x=a0\+([.0-9]+)f;x<=a1-([.0-9]+)f;x\+=([.0-9]+)f\)', cls.code)
        if not rows or not banks or not rule:
            raise AssertionError("VIP geometry layout missing or changed: review its regression tests")
        cls.rows = [float(s.rstrip('f')) for s in rows.group(1).split(',')]
        cls.banks = [(float(a), float(b)) for a, b in re.findall(
            r'\((-?\d+(?:\.\d+)?)f,(-?\d+(?:\.\d+)?)f\)', banks.group(1))]
        cls.start, cls.end, cls.pitch = (float(x) for x in rule.groups())

    def test_luxury_armchairs_are_vip_only_and_judges_keep_desks(self):
        self.assertIn("LuxuryVipArmchair(leather,new Vector3(x,yF,z),180f)", self.code)
        self.assertIn("SeatCount++;", self.code)
        self.assertIn("BoxMinMax(deskTop, x0 - .05f, x1 + .05f", self.code)
        self.assertIn("Seat(leather, false, z, s > 0 ? x0 - .75f : x1 + .75f, yF, s);", self.code)
        self.assertIn("LibraryLeather", self.code)
        self.assertIn("LibraryMetal", self.code)

    def test_seat_dimensions_do_not_overlap_and_center_aisle_is_open(self):
        self.assertEqual(len(self.rows), 2)
        self.assertEqual(len(self.banks), 2)
        # Luxury armchairs have their widest left/right armrest at approx
        # ±(.49 + .085) = ±0.575 nominal metres.
        max_width = 2 * (.49 + .085)
        max_depth = 1.04
        self.assertGreater(self.pitch, max_width)
        self.assertGreater(min(b - a for a, b in zip(self.rows, self.rows[1:])), max_depth)
        positions = []
        for row in self.rows:
            for left, right in self.banks:
                x = left + self.start
                while x <= right - self.end + 0.0001:
                    self.assertGreater(x - max_width/2, left)
                    self.assertLess(x + max_width/2, right)
                    self.assertTrue(x + max_width/2 < -87.55 or x - max_width/2 > -84.45,
                                    "Seat blocks the red-carpet walk between the VIP banks")
                    positions.append((x,row))
                    x += self.pitch
        self.assertEqual(len(positions), 16)

    def test_judges_remain_separate_from_vip_seat_rows(self):
        self.assertGreater(min(self.rows), 65.7 + .95)
        self.assertIn("new[] { 61.8f, 63.3f, 64.8f }", self.code)
        self.assertIn("s > 0 ? x1 + .02f : x0 - .02f", self.code)


if __name__ == '__main__':
    unittest.main()
