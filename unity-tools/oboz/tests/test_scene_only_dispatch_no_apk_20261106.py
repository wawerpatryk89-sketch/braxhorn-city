"""Jedna ręczna akcja obozu: quality_review=true uruchamia scenę, nie APK."""
from pathlib import Path
import unittest
ROOT=Path(__file__).resolve().parents[3]
W=(ROOT/".github/workflows/oboz-unity-build.yml").read_text("utf-8")
AUDIT=(ROOT/"unity-city/Assets/Editor/Oboz/BraxObozSceneAudit.cs").read_text("utf-8")
class SceneOnlyDispatchContract(unittest.TestCase):
    def test_owner_dispatch_mode_cannot_build_android(self):
        self.assertIn("  oboz-scene-only:",W)
        self.assertIn("inputs.quality_review == true",W)
        self.assertIn("inputs.quality_review != true",W)
        self.assertIn("buildMethod: BraxObozSceneAudit.GenerateWithoutApk",W)
        scene=W.split("  oboz-scene-only:",1)[1].split("  oboz-content-only:",1)[0]
        self.assertNotIn("buildMethod: BraxObozBuild.BuildAndroidReview",scene)
        self.assertNotIn("BuildPipeline.BuildPlayer",scene)
        self.assertIn("test ! -e build/ObozAndroid/braxhorn-oboz.apk",scene)
    def test_saved_batch_editor_scene_and_text_only_artifacts(self):
        self.assertIn("SaveUntitledSceneInBatch();",AUDIT)
        scene=W.split("  oboz-scene-only:",1)[1].split("  oboz-content-only:",1)[0]
        self.assertIn("audit/oboz-scena.json",scene)
        self.assertIn("audit/oboz-scena.md",scene)
        self.assertNotIn("*.png",scene)
        self.assertNotIn("*.mp4",scene)
