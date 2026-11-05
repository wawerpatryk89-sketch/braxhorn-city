"""Fast, network-free Champions content-update contract regression checks.

Runs before the costly Unity build. Does not claim that a device test passed.
"""
import json
from pathlib import Path
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]


class ChampionsContentModeTests(unittest.TestCase):
    def code(self, relative):
        return (ROOT / relative).read_text(encoding="utf-8")

    def test_unity6_addressables_package_pinned(self):
        packages = json.loads(self.code("unity-city/Packages/manifest.json"))["dependencies"]
        self.assertEqual(packages.get("com.unity.addressables"), "2.7.6")
        self.assertEqual(packages.get("com.unity.modules.assetbundle"), "1.0.0")

    def test_first_apk_contains_bootstrap_and_content_baseline(self):
        builder = self.code("unity-city/Assets/Editor/Mistrzowie/BraxChampionsBuild.cs")
        self.assertIn('BraxContentBuild.Baseline("mistrzowie", new[] { BraxChampionsExplorer.ScenePath })', builder)
        self.assertIn('scenes=new[]{startPath},target=BuildTarget.Android', builder)
        self.assertLess(builder.index('BraxContentBuild.Baseline("mistrzowie"'),
                        builder.index('BuildPipeline.BuildPlayer(new BuildPlayerOptions'))

    def test_first_scene_loads_addressables_or_legacy_and_imports_zip(self):
        start = self.code("unity-city/Assets/Claude/BraxStart.cs")
        self.assertIn("yield return BraxContent.Initialize()", start)
        self.assertIn("BraxContent.Begin(nazwa, LoadSceneMode.Single, false)", start)
        self.assertIn("yield return op.Activate()", start)
        self.assertIn("BraxContent.ChooseUpdate()", start)
        picker = self.code("unity-city/Assets/Updates/Android/BraxContentPicker.java")
        self.assertIn("Intent.ACTION_OPEN_DOCUMENT", picker)
        self.assertIn("UnityPlayer.UnitySendMessage", picker)

    def test_content_only_builder_never_builds_apk(self):
        builder = self.code("unity-city/Assets/Editor/Updates/BraxContentBuild.cs")
        section = builder.split("public static void UpdateFromBaseline()",1)[1].split("static void Check(",1)[0]
        self.assertIn("ContentUpdateScript.BuildContentUpdate", section)
        self.assertNotIn("BuildPipeline.BuildPlayer", section)
        self.assertIn("RuntimeId() != basis.runtimeId", section)
        self.assertIn('"/scene-guids.txt"', builder)

    def test_baseline_and_updates_reject_cross_zone_bundles(self):
        manifest = self.code("unity-city/Assets/Updates/BraxContentManifest.cs")
        self.assertIn("basis.zone != update.zone", manifest)
        self.assertIn("basis.baseId != update.baseId", manifest)
        self.assertIn("basis.runtimeId != update.runtimeId", manifest)
        self.assertIn("SHA256.Create()", manifest)

    def test_baseline_snapshot_does_not_publish_secrets_or_replace_guids(self):
        snap = self.code("unity-tools/updates/content_snapshot.py")
        self.assertIn("mistrzowie", snap)
        self.assertIn("AndroidKeystorePass", snap)
        self.assertIn("Assets/AddressableAssetsData/", snap)
        self.assertIn("addressables_content_state.bin", snap)
        self.assertIn("source code excluded", self.code("unity-tools/updates/test_content_snapshot.py"))

    def test_content_workflow_does_not_launch_build_player(self):
        workflow = self.code(".github/workflows/mistrzowie-apk.yml")
        self.assertIn("inputs.etap != 'aktualizacja'", workflow)
        self.assertIn("inputs.etap == 'aktualizacja'", workflow)
        section = workflow.split("  champions-content-update:", 1)[1]
        self.assertIn("BraxContentBuild.UpdateFromBaseline", section)
        self.assertNotIn("BraxChampionsBuild.BuildAndroid", section)
        self.assertNotIn("BuildPlayer", section.split("      - name: Build changed Addressables bundles ONLY", 1)[1])
        self.assertIn("content_snapshot.py restore-base", section)
        self.assertIn("content_snapshot.py publish-update", section)


if __name__ == "__main__":
    unittest.main()
