"""Offline regression: Obóz updates the same BRAXHORN CITY content baseline.

This is static preflight only. Does NOT assert Unity compile, installed APK
compatibility, baseline on phone or runtime/device success.
"""
import json
from pathlib import Path
import re
import unittest

ROOT = Path(__file__).resolve().parents[3]
def load(relative):
    return (ROOT / relative).read_text(encoding="utf-8")


class ContentUpdateContract(unittest.TestCase):
    def test_package_reuses_existing_project_and_shared_addressables(self):
        packages = json.loads(load("unity-city/Packages/manifest.json"))["dependencies"]
        self.assertEqual(packages["com.unity.addressables"], "2.7.6")
        self.assertEqual(packages["com.unity.modules.assetbundle"], "1.0.0")
        self.assertEqual(packages["com.unity.modules.unitywebrequestassetbundle"], "1.0.0")
        self.assertEqual(packages["com.unity.cloud.gltfast"], "6.20.0")

    def test_shared_update_runtime_has_rollback_and_hash_verification(self):
        runtime=load("unity-city/Assets/Updates/BraxContent.cs")
        manifest=load("unity-city/Assets/Updates/BraxContentManifest.cs")
        for token in ("BraxContentBase", "Addressables.InitializeAsync",
                      "LoadContentCatalogAsync", 'ReadPointer("previous")',
                      'ReadPointer("pending")', 'Path.Combine(root, "trial")',
                      "OnUpdatePicked", "BraxContentPicker"):
            self.assertIn(token,runtime)
        for token in ("SHA256.Create()", "Compatible(", "ReadZipManifest(", "Extract("):
            self.assertIn(token,manifest)

    def test_existing_startup_preserves_legacy_and_opt_in_update_ui(self):
        startup=load("unity-city/Assets/Claude/BraxStart.cs")
        runtime=load("unity-city/Assets/Updates/BraxContent.cs")
        self.assertIn('public string regionTitle = "";',startup)
        self.assertIn("yield return BraxContent.Initialize();",startup)
        self.assertIn("BraxContent.Begin(nazwa, LoadSceneMode.Single, false)",startup)
        self.assertIn('en ? "IMPORT UPDATE" : "WCZYTAJ AKTUALIZACJĘ"',startup)
        self.assertIn("if (BraxContent.Enabled)",startup)
        self.assertIn('if (!Enabled) { var old = SceneManager.LoadSceneAsync(name, mode)',runtime)
        self.assertIn("Resources.Load<TextAsset>(\"BraxContentBase\")",runtime)

    def test_content_only_export_never_builds_player_or_changes_application_id(self):
        source=load("unity-city/Assets/Editor/Oboz/BraxObozContentBase.cs")
        self.assertIn('public const string Zone = "oboz";',source)
        self.assertIn("BraxObozEnvironment.Prepare();",source)
        self.assertIn("BraxObozActors.Assemble();",source)
        self.assertIn('BraxContentBuild.Baseline(Zone, new[] { BraxObozActors.ScenePath });',source)
        self.assertIn("BraxObozUpdateRegistry.Export();",source)
        self.assertFalse(re.search(r"(?m)^\s*BuildPipeline\.BuildPlayer\(",source))
        self.assertNotIn("PlayerSettings.SetApplicationIdentifier",source)
        self.assertIn("Directory.Exists(BraxContentBuild.Base)",source)
        self.assertIn('File.Exists("content-patch.json")',source)
        meta=load("unity-city/Assets/Editor/Oboz/BraxObozContentBase.cs.meta")
        self.assertRegex(meta,r"(?m)^guid: [0-9a-f]{32}$")

    def test_manual_update_pins_same_camp_branch_and_guards_cross_zone_edits(self):
        pipeline=load(".github/workflows/oboz-content-update.yml")
        self.assertIn("workflow_dispatch:",pipeline)
        self.assertIn("refs/heads/codex/oboz-przebudowa-pbr-20261105",pipeline)
        self.assertIn("oboz-[0-9a-f]{32}",pipeline)
        self.assertIn("Assets/PlayableGenerated/Oboz/BraxhornObozReview.unity",pipeline)
        self.assertIn("content_snapshot.py restore-base",pipeline)
        self.assertIn("BraxContentBuild.UpdateFromBaseline",pipeline)
        self.assertIn("content_snapshot.py publish-update",pipeline)
        self.assertNotIn("BuildAndroidReview",pipeline)
        self.assertNotIn("BuildPipeline.BuildPlayer",pipeline)

    def test_content_baseline_is_not_misrepresented_as_ready_phone_update(self):
        registry=load("unity-city/Assets/Editor/Oboz/BraxObozUpdateRegistry.cs")
        self.assertIn("readyForContentUpdate = false",registry)
        self.assertIn("runtimeVerified = false",registry)
        stage=load(".github/workflows/oboz-content-base.yml")
        self.assertIn("BraxObozContentBase.Export",stage)
        self.assertIn("BRAXHORN",stage)
        self.assertNotIn("BraxObozBuild.BuildAndroidReview",stage)
        self.assertIn("no APK",stage)
        self.assertIn("BraxContentPicker.jar",stage)

    def test_legacy_standalone_workflow_explicitly_blocked_on_update_branch(self):
        workflow=load(".github/workflows/oboz-unity-build.yml")
        self.assertIn("Blokada osobnego APK na gałęzi aktualizacji obozu",workflow)
        self.assertIn("exit 1",workflow)
        self.assertIn("com.braxhorn.oboz",workflow)

    def test_bunker_not_brought_back_by_update_integration(self):
        env=load("unity-city/Assets/Editor/Oboz/BraxObozEnvironment.cs")
        self.assertNotIn("SourceBunker(",env)
        self.assertNotIn("underground_bunker__unity.glb",env)


if __name__=="__main__":
    unittest.main()
