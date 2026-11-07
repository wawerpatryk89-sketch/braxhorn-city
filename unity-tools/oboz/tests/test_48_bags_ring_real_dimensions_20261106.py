"""Obóz etap E: 48 prawdziwych worków, ring 6x6m, podest 1m."""
import json
import unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
ENV=(ROOT/"unity-city/Assets/Editor/Oboz/BraxObozEnvironment.cs").read_text("utf-8")
VALIDATION=(ROOT/"unity-city/Assets/Editor/Oboz/BraxObozValidation.cs").read_text("utf-8")

class ArenaRequirements(unittest.TestCase):
    def test_48_owner_bags_and_24_covered(self):
        self.assertIn("Bags(-23, -40, true,10)",ENV)
        self.assertIn("Bags(23, -40, false,12)",ENV)
        self.assertIn("Bags(12,51,false,2)",(ROOT/"unity-city/Assets/Editor/Oboz/BraxObozCampus.cs").read_text("utf-8"))
        self.assertIn("scene.bags.Length != 48",VALIDATION)
        self.assertIn("scene.coveredBags.Length != 24",VALIDATION)
        self.assertIn("halfBeam=(columns-1)*1.1f+1.8f",ENV)

    def test_ring_six_metres_rope_and_one_metre_stage(self):
        arena=(ROOT/"unity-city/Assets/Editor/Oboz/BraxObozArenaConstruction.cs").read_text("utf8")
        # The numerical clear-width/step checks are in test_arena_construction_dimensions.
        # Do not require the old solid 8x1x8 box that the owner rejected.
        self.assertIn("RingPlatformHeight=1f",arena)
        self.assertIn("RingRopeAxis=3f+RingRopeRadius",arena)
        self.assertIn("new[]{1.35f,1.70f,2.05f,2.40f}",arena)
        self.assertIn("ring.position.y+y*K",arena)
        self.assertIn("Deska podestu",arena)

    def test_bag_bottom_and_body_length(self):
        measured=json.loads((ROOT/"unity-tools/oboz/OWNER_ASSETS/TRAINING_GEOMETRY_MEASUREMENTS_20261106.json").read_text("utf8"))["bag"]
        factor=1.2/measured["sourceBodyHeightMetres"]
        self.assertAlmostEqual(measured["sourceBodyHeightMetres"]*factor,1.2,places=6)
        self.assertAlmostEqual(measured["sourceAssemblyHeightMetres"]*factor+.6,measured["targetMountMetres"],places=6)
        self.assertNotRegex(ENV,r"targetHeight\s*=\s*2\.65")
        component=(ROOT/"unity-city/Assets/Oboz/BraxObozBagGeometry.cs").read_text("utf8")
        self.assertIn("TryMeasureBodyBounds",component)
        self.assertIn("mesh.vertices",component)
