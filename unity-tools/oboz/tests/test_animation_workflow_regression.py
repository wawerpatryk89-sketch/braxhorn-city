"""Regression: each camp scene generator restores verified motion files before Unity.

Offline checks only: no Unity, private asset downloads, source changes or APK.
"""
import json
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[3]
APK_WORKFLOW = ROOT / ".github/workflows/oboz-unity-build.yml"
CONTENT_WORKFLOW = ROOT / ".github/workflows/oboz-content-base.yml"
MANIFEST = ROOT / "unity-city/Assets/Editor/Oboz/CharacterManifest.json"
RESTORE_STEP = "      - name: Przywroc animacje casual i Moniki (wymagane dla wszystkich 7 postaci)\n"
CHECK_STEP = "      - name: Sprawdz wszystkie klipy postaci PRZED uruchomieniem Unity\n"


def step_text(source: str, name: str) -> str:
    if source.count(name) != 1:
        raise AssertionError("Expected exactly one step: " + name.strip())
    return source.split(name, 1)[1].split("\n      - name: ", 1)[0]


class CampAnimationWorkflowRegression(unittest.TestCase):
    def test_seven_characters_declare_original_idle(self):
        data = json.loads(MANIFEST.read_text(encoding="utf-8"))
        self.assertEqual(len(data["characters"]), 7)
        for person in data["characters"]:
            clips = {c["name"]: c["path"] for c in person["clips"]}
            self.assertTrue({"Idle", "Walk", "Run"} <= clips.keys(), person["id"])
            self.assertEqual(clips["Idle"], "Assets/Source/Prywatne/Oboz/animations/casual/Idle.anim")
            self.assertTrue(clips["Walk"] and clips["Run"])

    def assert_restoration_and_guard(self, source: str, entry: str, conditional: bool):
        restore = step_text(source, RESTORE_STEP)
        check = step_text(source, CHECK_STEP)
        self.assertIn("run: python3 unity-tools/oboz/motion_candidates.py", restore)
        self.assertIn("KLUCZ:", restore)
        self.assertIn("BRAXHORN_KEYSTORE_PASS", restore)
        self.assertIn("BRAXHORN_OBOZ_MISSING_ANIMATION", check)
        self.assertIn("p.stat().st_size==0", check)
        self.assertIn("raise SystemExit(1)", check)
        condition = "if: inputs.content_action == 'export-candidate'"
        if conditional:
            self.assertIn(condition, restore)
            self.assertIn(condition, check)
        else:
            self.assertNotIn("\n        if:", restore)
            self.assertNotIn("\n        if:", check)
        self.assertLess(source.index(RESTORE_STEP), source.index(CHECK_STEP))
        self.assertLess(source.index(CHECK_STEP), source.index(entry))

    def test_apk_checks_all_clips_before_buildplayer(self):
        source = APK_WORKFLOW.read_text(encoding="utf-8").split("  oboz-content-only:", 1)[0]
        self.assert_restoration_and_guard(source, "buildMethod: BraxObozBuild.BuildAndroidReview", False)

    def test_content_export_checks_all_clips_before_unity(self):
        source = APK_WORKFLOW.read_text(encoding="utf-8").split("  oboz-content-only:", 1)[1]
        self.assert_restoration_and_guard(source, "buildMethod: BraxObozContentBase.ExportInCI", True)

    def test_content_base_checks_all_clips_before_unity(self):
        source = CONTENT_WORKFLOW.read_text(encoding="utf-8")
        self.assert_restoration_and_guard(source, "buildMethod: BraxObozContentBase.ExportInCI", False)

    def test_sparse_checkouts_include_workflow_fixtures(self):
        # This test is executed inside each of these sparse-checkout jobs.
        # Without the actual YAML fixtures, a regression check would fail
        # before examining the restored motion-clip gate.
        apk = APK_WORKFLOW.read_text(encoding="utf-8")
        content_only = apk.split("  oboz-content-only:", 1)[1]
        baseline = CONTENT_WORKFLOW.read_text(encoding="utf-8")
        self.assertIn("            .github/workflows/\n", content_only)
        self.assertIn("            .github/workflows/\n", baseline)


if __name__ == "__main__":
    unittest.main()
