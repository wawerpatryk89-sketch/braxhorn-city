"""7 Nov: lightweight APK + external Obóz content package; static, no Unity."""
from pathlib import Path
import unittest
ROOT=Path(__file__).resolve().parents[3]
ASSETS=ROOT/"unity-city/Assets"
BUILD=(ASSETS/"Editor/Updates/BraxContentBuild.cs").read_text("utf-8")
RUNTIME=(ASSETS/"Updates/BraxContent.cs").read_text("utf-8")
MANIFEST=(ASSETS/"Updates/BraxContentManifest.cs").read_text("utf-8")
START=(ASSETS/"Claude/BraxStart.cs").read_text("utf-8")
OBOZ=(ASSETS/"Editor/Oboz/BraxObozBuild.cs").read_text("utf-8")
WORKFLOW=(ROOT/".github/workflows/oboz-unity-build.yml").read_text("utf-8")
class ExternalBaseDelivery(unittest.TestCase):
    def test_player_omits_full_camp_scene_when_external_base_exists(self):
        self.assertIn("return new[] { StartScene };",OBOZ)
        self.assertIn("scenes = playerScenes",OBOZ)
        self.assertIn("contentUpdatesReady = !sceneOnly && playerScenes.Length == 1",OBOZ)
    def test_baseline_bundles_are_not_copied_back_into_apk(self):
        self.assertIn('!f.name.EndsWith(".bundle"',BUILD)
        self.assertIn("PackageBase(m, BasePackagePath(zone))",BUILD)
        self.assertIn("ExtractBase(zip, check, basis)",BUILD)
        self.assertIn("public bool externalBase;",MANIFEST)
    def test_runtime_requires_verified_base_before_loading_scene(self):
        self.assertIn("RequiresBaseInstall",RUNTIME)
        self.assertIn("ChooseBasePackage",RUNTIME)
        self.assertIn("ExtractBase(path, staging, basis)",RUNTIME)
        self.assertIn("Wybierz paczkę danych obozu",START)
    def test_workflow_uses_java_source_once_and_external_base_before_apk(self):
        self.assertIn("Assets/Updates/Android/BraxContentPicker.java",WORKFLOW)
        self.assertIn("rm -f unity-city/Assets/Plugins/Android/BraxContentPicker.jar",WORKFLOW)
        self.assertNotIn("/project/unity-tools/oboz/build_content_picker.sh",WORKFLOW)
        self.assertLess(WORKFLOW.index("BraxObozContentBase.ExportPreparedExternalInCI"),WORKFLOW.index("BraxObozBuild.BuildAndroidReview"))
        self.assertIn("heavy bundle leaked back into APK",WORKFLOW)
        self.assertIn("oboz-base.braxbase.zip",WORKFLOW)
if __name__=="__main__":
    unittest.main()
