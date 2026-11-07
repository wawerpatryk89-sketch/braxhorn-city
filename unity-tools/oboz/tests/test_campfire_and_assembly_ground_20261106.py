"""Obóz F — ognisko z rzeczywistego GLB i tablica planu dnia."""
import unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
ENV=(ROOT/"unity-city/Assets/Editor/Oboz/BraxObozEnvironment.cs").read_text("utf-8")
class OwnerCampClimateTest(unittest.TestCase):
    def test_original_rocks_and_log_benches(self):
        self.assertIn("CampfireFromOriginalRocks();",ENV)
        self.assertIn("Assets/Source/Claude/Library/park_rock.glb",ENV)
        helper=(ROOT/"unity-city/Assets/Editor/Oboz/BraxObozCampfireSupport.cs").read_text("utf-8")
        self.assertIn("CampfireLogBenches(camp)",ENV)
        self.assertIn('Cylinder(bench,"Siedzisko ławy z bala"',helper)
        self.assertIn("CampfireFloor",helper)
        self.assertIn("float targetTop=centreFloor.point.y+.45f*K",helper)
        self.assertIn("placement.heightAboveFloorMetres=.45f",helper)
        self.assertIn("placement.suspensionSupport=supportColliders[0]",helper)
        self.assertIn("ParticleSystem",ENV)
        self.assertNotIn("CreatePrimitive(PrimitiveType.Cube); // rock",ENV)
    def test_assembly_with_real_wood_and_missing_owner_flags_reported(self):
        self.assertIn("AssemblyGroundWithSchedule();",ENV)
        self.assertIn("PLAN DNIA",ENV)
        self.assertIn("BRAXHORN_OBOZ_OWNER_ASSETS_PENDING",ENV)
        self.assertIn("LightShadows.None",ENV)
