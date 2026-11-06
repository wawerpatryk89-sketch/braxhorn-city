"""Łaźnia: prawdziwa cegła z Poly Haven i poziomy UKŁAD spoin."""
from pathlib import Path
import unittest
R=Path(__file__).resolve().parents[3]
S=(R/"unity-city/Assets/Editor/Oboz/BraxObozServices.cs").read_text("utf-8")
M=(R/"unity-tools/oboz/facility_public_materials.py").read_text("utf-8")
class BrickFacade(unittest.TestCase):
    def test_official_polyhaven_source(self):
        self.assertIn('"red_brick_03":',M)
        self.assertIn("https://api.polyhaven.com/files/",M)
        self.assertIn('BraxObozMaterials.PublicPbr("red_brick_03",1f)',S)
    def test_horizontal_world_uv_not_rotated(self):
        self.assertIn('new Vector2(metre.z,metre.y)',S)
        self.assertIn('new Vector2(metre.x,metre.y)',S)
        self.assertIn("Ceglana elewacja PBR — południe",S)
        self.assertIn("Ceglana elewacja PBR — zachód",S)
        self.assertIn("Ceglana elewacja PBR — wschód",S)
    def test_persistent_map_guids(self):
        for role in ('Color','NormalGL','Roughness','AmbientOcclusion'):
            p=R/f"unity-city/Assets/Source/Public/Oboz/red_brick_03/{role}.jpg.meta"
            self.assertTrue(p.is_file())
            self.assertIn('guid:',p.read_text('utf-8'))
