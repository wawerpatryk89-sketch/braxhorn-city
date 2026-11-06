"""Obóz etap A — test zachowania skali i mocowania modeli (bez Unity/APK)."""
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[3]
EDITOR = ROOT / "unity-city/Assets/Editor/Oboz"

class RealPlacementContract(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.place = (EDITOR / "BraxObozPlacement.cs").read_text("utf-8")
        cls.services = (EDITOR / "BraxObozServices.cs").read_text("utf-8")
        cls.campus = (EDITOR / "BraxObozCampus.cs").read_text("utf-8")
        cls.player = (ROOT / "unity-city/Assets/Oboz/BraxObozIdleFootContact.cs").read_text("utf-8")

    def test_single_reference_axis_no_box_fit(self):
        self.assertIn("metres * K / measured", self.place)
        self.assertIn("RealDimension(", self.services)
        for source in (self.services, self.campus):
            self.assertNotIn("Mathf.Min(targetSize.x/bounds.size.x", source)
            self.assertNotIn("Mathf.Min(max.x/bounds.size.x", source)
            self.assertNotIn("Mathf.Min(kx,Mathf.Min(ky,kz))", source)
            self.assertNotIn("Mathf.Min(26f*K/bounds.size.x", source)

    def test_true_references_and_ground_rays(self):
        self.assertIn("RealDimension(model.transform,renderers,2f", self.services)
        # .75/.45 above the floor are exercised by the actual building recipe
        # in test_service_building_geometry; Unity binds its measured assemblies.
        self.assertIn("RealDimension(part,visible,item.height", self.services)
        # A sink is measured to its basin; the original faucet remains above
        # it. Measuring the complete GLB height would shorten its legs/body.
        self.assertIn("StandingSurfaceHeight(carrier.transform,renderers,", self.services)
        self.assertIn('SourceBody(carrier,"Sink.001_"),.85f,label)', self.services)
        self.assertIn("Physics.RaycastAll", self.place)
        self.assertIn("OnGround(", self.services)
        self.assertIn("ToWall(", self.services)
        self.assertIn("BRAXHORN_OBOZ_NO_FLOOR_RAYCAST", self.place)
        self.assertIn("BRAXHORN_OBOZ_NO_WALL_RAYCAST", self.place)

    def test_monika_contact_when_grounded(self):
        self.assertIn("bool grounded=body.isGrounded;", self.player)
        self.assertIn("if(grounded)", self.player)
        self.assertIn("TryGetSoleHeight", self.player)
        self.assertIn("baselineLocalPosition", self.player)
        self.assertNotIn("skin.bounds.min.y", self.player)
        self.assertNotIn('IsPlaying("Idle")', self.player)
