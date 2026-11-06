"""Etap D — obóz w lesie, nie miejskie prostokątne obiekty."""
from pathlib import Path
import unittest

ROOT=Path(__file__).resolve().parents[3]
EDITOR=ROOT/"unity-city/Assets/Editor/Oboz"

class CabinAndMirrorTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.env=(EDITOR/"BraxObozEnvironment.cs").read_text("utf-8")
        cls.campus=(EDITOR/"BraxObozCampus.cs").read_text("utf-8")
        cls.services=(EDITOR/"BraxObozServices.cs").read_text("utf-8")

    def test_real_log_shell_and_front_porch(self):
        self.assertIn("Bal tylnej elewacji — okrągły",self.env)
        self.assertIn('Cylinder(t,"Bal bocznej ściany — okrągły"',self.env)
        self.assertIn("Deski pomostu wejściowego",self.campus)
        self.assertIn("Pochwyt boczny z drewna",self.campus)
        self.assertNotIn('Box(t,"Deska bocznej elewacji"',self.env)

    def test_gate_owner_artwork_has_two_log_posts_and_thick_beam(self):
        self.assertIn("Jednolity bal ponad bramą — 9,4 m",self.campus)
        self.assertIn("Dwa grube pionowe słupy z bali",self.campus)
        self.assertNotIn("Stalowy wspornik do widocznej krawędzi szyldu",self.campus)

    def test_murals_ray_on_architecture_and_safely_report_failure(self):
        self.assertIn("BRAXHORN_OBOZ_MURAL_NO_WALL",self.env)
        self.assertIn("GetSourceTextureWidthAndHeight",self.env)
        self.assertIn("BRAXHORN_OBOZ_MURAL_MISSING",self.env)

    def test_mirror_reflection_probe(self):
        self.assertIn("ReflectionProbeMode.Baked",self.services)
        self.assertIn("ReflectionProbeRefreshMode.ViaScripting",self.services)
