"""Owner-reviewed real-scale regressions for shelter and 4X bull."""
from pathlib import Path
import unittest
ROOT=Path(__file__).resolve().parents[3]
S=(ROOT/"unity-city/Assets/Editor/Oboz/BraxObozCampus.cs").read_text("utf8")
class OwnerSourceDimensions(unittest.TestCase):
    def test_shelter_restores_owner_reviewed_uniform_envelope(self):
        self.assertIn("const float targetWidth=3.4f,targetHeight=3.2f,targetDepth=8.6f;",S)
        self.assertIn("model.transform.localScale*=scale;",S)
        self.assertNotIn('NativeMetres(model.transform,renderers,\n            "wiata FBX właściciela")',S)
        self.assertIn("WŁAŚCICIEL 7.11",S)
    def test_bull_4x_not_fitted_to_4_8m_box(self):
        self.assertIn("BRAXHORN_BULL_RONDO_4X_ORYGINALNE_TEKSTURY.glb",S)
        self.assertIn('NativeMetres(model.transform,renderers,\n            "oryginalny byk 4X")',S)
        self.assertNotIn("(4.8f*K)/longest",S)
        self.assertIn("BRAXHORN_OBOZ_BULL_CLEARANCE_REVIEW",S)
