"""7 Nov rollback: owner requires one normal Obóz APK, no mandatory external base package."""
from pathlib import Path
import unittest

ROOT=Path(__file__).resolve().parents[3]
ASSETS=ROOT/"unity-city/Assets"
START=(ASSETS/"Claude/BraxStart.cs").read_text("utf-8")
RUNTIME=(ASSETS/"Updates/BraxContent.cs").read_text("utf-8")
BUILD=(ASSETS/"Editor/Oboz/BraxObozBuild.cs").read_text("utf-8")
WORKFLOW=(ROOT/".github/workflows/oboz-unity-build.yml").read_text("utf-8")

class SingleApkDelivery(unittest.TestCase):
    def test_start_has_no_required_external_base_picker(self):
        self.assertNotIn("RequiresBaseInstall",START)
        self.assertNotIn("ChooseBasePackage",START)
        self.assertNotIn(".braxbase.zip",START)
        self.assertNotIn("RequiresBaseInstall",RUNTIME)

    def test_full_camp_scenes_are_in_normal_player(self):
        self.assertIn("static readonly string[] Scenes = { StartScene, BraxObozActors.ScenePath };",BUILD)
        self.assertIn("scenes = Scenes",BUILD)
        self.assertIn("BRAXHORN_REUSE_PREPARED_SCENE",BUILD)

    def test_normal_apk_lane_has_no_external_base_and_no_long_scene_only(self):
        android=WORKFLOW.split("  android-oboz:",1)[1].split("  oboz-scene-only:",1)[0]
        self.assertNotIn("ExportPreparedExternalInCI",android)
        self.assertNotIn("oboz-base.braxbase.zip",android)
        self.assertNotIn("--content-base",android)
        self.assertNotIn("PrepareAndroidReviewScene",android)
        self.assertIn('BRAXHORN_REUSE_PREPARED_SCENE: "1"',android)

if __name__=="__main__":
    unittest.main()
