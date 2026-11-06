"""AssetPostprocessor does not throw; texture/model quality logs warnings."""
from pathlib import Path
import unittest
ROOT=Path(__file__).resolve().parents[3]
C=(ROOT/"unity-city/Assets/Editor/Oboz/BraxObozImport.cs").read_text("utf-8")
class NonBlockingImport(unittest.TestCase):
    def test_texture_handler_warns_instead_of_throw(self):
        start=C.split("void OnPreprocessTexture()",1)[1].split("void ApplyCampTextureSettings()",1)[0]
        self.assertIn("try { ApplyCampTextureSettings(); }",start)
        self.assertIn("catch(Exception ex)",start)
        self.assertIn("UnityEngine.Debug.LogWarning",start)
        self.assertNotIn("throw new",start)
    def test_model_handler_warns_instead_of_throw(self):
        start=C.split("void OnPreprocessModel()",1)[1].split("void ApplyCampModelSettings()",1)[0]
        self.assertIn("try { ApplyCampModelSettings(); }",start)
        self.assertIn("catch(Exception ex)",start)
        self.assertIn("UnityEngine.Debug.LogWarning",start)
        self.assertNotIn("throw new",start)
