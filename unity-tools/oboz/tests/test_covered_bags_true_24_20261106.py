"""Behavioral count contract for physical canopy and bags; no Unity/APK."""
from pathlib import Path
import re
import unittest

ROOT=Path(__file__).resolve().parents[3]
ENV=(ROOT/"unity-city/Assets/Editor/Oboz/BraxObozEnvironment.cs").read_text("utf-8")
CAMP=(ROOT/"unity-city/Assets/Editor/Oboz/BraxObozCampus.cs").read_text("utf-8")
VAL=(ROOT/"unity-city/Assets/Editor/Oboz/BraxObozValidation.cs").read_text("utf-8")

class CanopyCount(unittest.TestCase):
    def test_48_bags_24_under_actual_roofs(self):
        covered=re.search(r"Bags\(-23,\s*-40,\s*true,\s*(\d+)\)",ENV)
        open_air=re.search(r"Bags\(23,\s*-40,\s*false,\s*(\d+)\)",ENV)
        self.assertIsNotNone(covered)
        self.assertIsNotNone(open_air)
        main_covered=2*int(covered.group(1))
        main_open=2*int(open_air.group(1))
        pavilion=re.search(r"Bags\(12,51,false,(\d+)\);",CAMP)
        self.assertIsNotNone(pavilion)
        pavilion_covered=2*int(pavilion.group(1))
        self.assertIn("foreach(var bag in bags.Skip(first)) covered.Add(bag.transform);",CAMP)
        self.assertEqual(main_covered+pavilion_covered,24)
        self.assertEqual(main_open,24)
        self.assertEqual(main_covered+pavilion_covered+main_open,48)
        self.assertIn("scene.coveredBags.Length != 24",VAL)
        self.assertIn("scene.bags.Length != 48",VAL)
