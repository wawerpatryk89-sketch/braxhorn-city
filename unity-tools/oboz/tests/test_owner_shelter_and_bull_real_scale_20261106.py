"""A-02 real original dimensions of shelter and owner's 4X bull."""
from pathlib import Path
import unittest
ROOT=Path(__file__).resolve().parents[3]
S=(ROOT/"unity-city/Assets/Editor/Oboz/BraxObozCampus.cs").read_text("utf8")
class OwnerSourceDimensions(unittest.TestCase):
    def test_shelter_keeps_native_metre_scale(self):
        self.assertIn('NativeMetres(model.transform,renderers,\n            "wiata FBX właściciela")',S)
        self.assertIn('BRAXHORN_OBOZ_SHELTER_FOOTPRINT_REVIEW',S)
        self.assertNotIn('Mathf.Min(3.4f*K/b.size.x',S)
    def test_bull_4x_not_fitted_to_4_8m_box(self):
        self.assertIn('BRAXHORN_BULL_RONDO_4X_ORYGINALNE_TEKSTURY.glb',S)
        self.assertIn('NativeMetres(model.transform,renderers,\n            "oryginalny byk 4X")',S)
        self.assertNotIn('(4.8f*K)/longest',S)
        self.assertIn('BRAXHORN_OBOZ_BULL_CLEARANCE_REVIEW',S)
