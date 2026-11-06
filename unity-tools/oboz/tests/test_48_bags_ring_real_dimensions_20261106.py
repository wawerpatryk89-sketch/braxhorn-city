"""Obóz etap E: 48 prawdziwych worków, ring 6x6m, podest 1m."""
import unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
ENV=(ROOT/"unity-city/Assets/Editor/Oboz/BraxObozEnvironment.cs").read_text("utf-8")
VALIDATION=(ROOT/"unity-city/Assets/Editor/Oboz/BraxObozValidation.cs").read_text("utf-8")

class ArenaRequirements(unittest.TestCase):
    def test_48_owner_bags_and_24_covered(self):
        self.assertIn("Bags(-23, -40, true,12)",ENV)
        self.assertIn("Bags(23, -40, false,10)",ENV)
        self.assertIn("Bags(12,51,false,2)",(ROOT/"unity-city/Assets/Editor/Oboz/BraxObozCampus.cs").read_text("utf-8"))
        self.assertIn("scene.bags.Length != 48",VALIDATION)
        self.assertIn("scene.coveredBags.Length != 24",VALIDATION)
        self.assertIn("halfBeam=(columns-1)*1.1f+1.8f",ENV)

    def test_ring_six_metres_rope_and_one_metre_stage(self):
        self.assertIn("new Vector3(8.0f,1.0f,8.0f)",ENV)
        self.assertIn("new Vector3(-3,0,-3)",ENV)
        self.assertIn("new Vector3(3,0,3)",ENV)
        self.assertIn("new[]{1.35f*K,1.70f*K,2.05f*K,2.40f*K}",ENV)

    def test_bag_bottom_and_body_length(self):
        self.assertIn("float targetHeight=2.65f*K",ENV)
        self.assertIn("collider.height=1.2f*K",ENV)
        self.assertIn("bag.dol=2.65f*K",ENV)
