"""Braxhorn obóz: bal 3D + realne PBR kory + metrowy UV."""
from pathlib import Path
import unittest
ROOT=Path(__file__).resolve().parents[3]
E=(ROOT/"unity-city/Assets/Editor/Oboz/BraxObozEnvironment.cs").read_text("utf-8")
M=(ROOT/"unity-tools/oboz/facility_public_materials.py").read_text("utf-8")
class RealBark(unittest.TestCase):
    def test_real_bark_not_wood_stretch(self):
        self.assertIn('PublicPbr("bark_brown_01",1f)',E)
        self.assertIn('"bark_brown_01":',M)
        self.assertIn('new Vector2(uv[i].x*circumference,(vertices[i].y*.5f+.5f)*length)',E)
        text=E[E.index("static void Cabin("):E.index("static void ColliderBox")]
        self.assertGreaterEqual(text.count("bark,false"),4)
        self.assertIn("Bal tylnej elewacji",text)
    def test_persistent_guid_assets(self):
        for name in ["Color","NormalGL","Roughness","AmbientOcclusion"]:
            p=ROOT/f"unity-city/Assets/Source/Public/Oboz/bark_brown_01/{name}.jpg.meta"
            self.assertTrue(p.exists())
            self.assertIn("guid:",p.read_text("utf-8"))
