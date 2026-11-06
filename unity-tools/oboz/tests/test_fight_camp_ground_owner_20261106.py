"""Obóz etap C — ściółka i błoto w polanie; bez fałszywego miejskiego trawnika."""
from pathlib import Path
import unittest

ROOT=Path(__file__).resolve().parents[3]
EDITOR=ROOT/"unity-city/Assets/Editor/Oboz"

class FightCampGroundTest(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.env=(EDITOR/"BraxObozEnvironment.cs").read_text("utf-8")
        cls.campus=(EDITOR/"BraxObozCampus.cs").read_text("utf-8")
        cls.grass=(EDITOR/"BraxObozVegetation.cs").read_text("utf-8")

    def test_mud_near_rings_bags_and_centre(self):
        self.assertIn("Fight camp: centrum jest UBITYM GRUNTEM",self.env)
        self.assertIn("new Vector2(-23,-40)",self.env)
        self.assertIn("new Vector2(-12,51)",self.env)
        self.assertIn("new Vector2(12,51)",self.env)
        self.assertIn("float mud=Mathf.Clamp01",self.env)
        self.assertIn("mud,1f",self.env)

    def test_no_grass_on_walking_working_ground(self):
        self.assertIn("FightCampBareGround(p.x,p.z)",self.grass)
        self.assertIn("FightCampBareGround(x,z)",self.grass)
        self.assertIn("BraxObozCampusLayout.FenceDistance(x,z)>22f",self.grass)
        self.assertIn("grass.zasiegMurawy=25f;",self.grass)
        self.assertIn("grass.zasiegLaki=25f;",self.grass)
        shader=(ROOT/"unity-city/Assets/Oboz/ObozGrassMedium.shader").read_text("utf-8")
        self.assertIn("IN.worldPos.xz-_WorldSpaceCameraPos.xz",shader)
        self.assertIn("_FadeEnd",shader)

    def test_owner_path_material_and_wooden_edge(self):
        self.assertIn("OwnerSlabTilesPbr()",self.campus)
        self.assertIn("Drewniane obrzeże ścieżki",self.campus)
        self.assertIn("IntersectsRunningTrack",self.campus)
        self.assertNotIn('Box(t,"Krawężnik poza bieżnią"',self.campus)
