"""A1 source readiness must use the same clip-name matching as its Brax importer.

Source-only test; no real model, animation, scene, Unity or APK is synthesized.
"""
from pathlib import Path
import unittest

ROOT=Path(__file__).resolve().parents[2]
READINESS=ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsSourceReadiness.cs"
BRAX=ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsBrax.cs"

class BraxSourceReadinessTests(unittest.TestCase):
    def test_preflight_clip_labels_follow_real_importer_normalization(self):
        source=READINESS.read_text(encoding="utf-8")
        prod=BRAX.read_text(encoding="utf-8")
        self.assertIn('ClipKey(c.name)==ClipKey(clip)',source)
        self.assertIn('char.IsLetterOrDigit',source)
        self.assertIn('Clean(c.name) == Clean(pair.Value)',prod)
        self.assertIn('BraxChampionsSourceReadiness.VerifyBeforeScene()',(
            ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsBuild.cs").read_text(encoding="utf-8"))
    def test_original_brax_assets_still_required(self):
        source=READINESS.read_text(encoding="utf-8")
        self.assertIn('RealModel(brax,"unchanged Brax")',source)
        self.assertIn('BraxChampionsCarBuilder.OwnerCar',source)
        for name in ('shrub_03_2k_embedded.glb','quiver_tree_02_2k_embedded.glb'):
            self.assertIn(name,source)
        for name in ('Entering Car.fbx','Driving.fbx','Exiting Car.fbx'):
            self.assertIn(name,source)

if __name__=="__main__":
    unittest.main()
