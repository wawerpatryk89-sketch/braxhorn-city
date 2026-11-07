"""Regression contract: the camp remains one BRAXHORN CITY content-update lane.

Static tests only. This is NOT evidence of Unity compilation or Android install.
"""
import json
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[3]
EDITOR = ROOT / "unity-city/Assets/Editor/Oboz"
UPDATES = ROOT / "unity-city/Assets/Updates"
SHARED = ROOT / "unity-city/Assets/Editor/Updates"
WORKFLOW = ROOT / ".github/workflows/oboz-unity-build.yml"
START = ROOT / "unity-city/Assets/Claude/BraxStart.cs"


class CampContentIntegrationContract(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.workflow = WORKFLOW.read_text("utf-8")
        cls.base = (EDITOR / "BraxObozContentBase.cs").read_text("utf-8")
        cls.start = START.read_text("utf-8")
        cls.shared = (SHARED / "BraxContentBuild.cs").read_text("utf-8")

    def test_only_addressables_dependencies_added_for_update_system(self):
        p = ROOT / "unity-city/Packages/manifest.json"
        dep = json.loads(p.read_text("utf-8"))["dependencies"]
        self.assertEqual(dep["com.unity.addressables"], "2.7.6")
        self.assertEqual(dep["com.unity.cloud.gltfast"], "6.20.0")
        self.assertIn("com.unity.modules.assetbundle", dep)

    def test_runtime_and_importer_have_original_guid_meta(self):
        for p in [UPDATES / "BraxContent.cs", UPDATES / "BraxContentManifest.cs",
                  SHARED / "BraxContentBuild.cs", SHARED / "BraxContentEdits.cs",
                  EDITOR / "BraxObozContentBase.cs"]:
            self.assertTrue(p.is_file(), str(p))
            meta = Path(str(p) + ".meta")
            self.assertTrue(meta.is_file(), str(meta))
            self.assertIn("guid:", meta.read_text("utf-8"))

    def test_legacy_game_look_and_existing_entrypoint_stay_intact(self):
        self.assertIn('public string regionTitle = "";', self.start)
        self.assertIn("BraxEkranStartowy.RysujObraz(!ustawienia)", self.start)
        self.assertIn("yield return BraxContent.Initialize();", self.start)
        self.assertIn("var op = BraxContent.Begin(nazwa, LoadSceneMode.Single, false);", self.start)
        self.assertIn("if (BraxContent.Enabled)", self.start)
        self.assertIn("BraxContent.ChooseUpdate();", self.start)

    def test_export_generates_content_without_building_an_apk(self):
        self.assertIn("BraxObozEnvironment.Prepare();", self.base)
        self.assertIn("BraxObozActors.Assemble();", self.base)
        self.assertIn("BraxContentBuild.Baseline(Zone, new[] { BraxObozActors.ScenePath }, true);", self.base)
        self.assertIn("ExportPreparedExternalInCI", self.base)
        self.assertIn("BraxContentBuild.UpdateFromBaseline();", self.base)
        self.assertNotIn("BuildPipeline.BuildPlayer(", self.base)
        self.assertIn("EditorApplication.Exit(0);", self.base)

    def test_manual_in_place_update_from_latest_compatible_apk(self):
        legacy = self.workflow.split("  android-oboz:", 1)[1].split("    runs-on:", 1)[0]
        self.assertIn("codex/oboz-przebudowa-pbr-20261105", legacy)
        self.assertIn("github.event_name == 'workflow_dispatch'", legacy)
        self.assertIn("inputs.first_install != true", legacy)
        self.assertIn("inputs.content_action == 'none'", legacy)
        # #22 already exists. Keep #18 as a minimum compatible baseline,
        # never require an obsolete run/commit while preparing the next APK.
        self.assertIn('code<216024998', self.workflow)
        self.assertIn('info.get("applicationId")!="com.braxhorn.oboz"', self.workflow)
        self.assertIn('signingCertificateSha256', self.workflow)
        self.assertIn('next_version<=code', self.workflow)
        self.assertNotIn('info.get("sourceRun")==41983757537', self.workflow)
        self.assertNotIn('info.get("versionCode")==216024998', self.workflow)
        self.assertIn('unity-tools/claude/prywatne.py', self.workflow)
        self.assertIn("  oboz-content-only:", self.workflow)
        self.assertIn("  oboz-content-update:", self.workflow)
        for action in ("preflight", "export-candidate", "update-from-base"):
            self.assertIn("          - " + action, self.workflow)
        self.assertIn("buildMethod: BraxObozContentBase.ExportInCI", self.workflow)
        self.assertIn("buildMethod: BraxObozContentBase.UpdateInCI", self.workflow)

    def test_update_is_pinned_to_base_and_scene_guids(self):
        self.assertIn("ContentUpdateScript.BuildContentUpdate", self.shared)
        self.assertIn("addressables_content_state.bin", self.shared)
        self.assertIn("Zmienił się kod, pakiety lub ustawienia silnika", self.shared)
        self.assertIn("AssetDatabase.AssetPathToGUID(pair[0]) != pair[1]", self.shared)
        registry = (EDITOR / "BraxObozUpdateRegistry.cs").read_text("utf-8")
        self.assertIn("readyForContentUpdate = false", registry)

    def test_original_bull_and_five_zip_parts_preserved(self):
        importer = (ROOT / "unity-tools/oboz/owner_release_assets.py").read_text("utf-8")
        self.assertIn('ORIGINAL_BULL = "A_charging_bull_statu_0725030443_refine.glb"', importer)
        self.assertIn('mode = "five_parts"', importer)
        self.assertIn("digest(source) != sha", importer)
        self.assertIn("from owner_assets_intake import ensure_stable_meta", importer)


if __name__ == "__main__":
    unittest.main()
