"""Camp #37 failed before the 90% scene-load marker on Motorola.

Source-only diagnostic regression; this cannot prove Android launch success.
"""
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[3]
LOGGER = (ROOT / "unity-city/Assets/Claude/BraxStartLog.cs").read_text(encoding="utf-8")
ASSEMBLER = (ROOT / "unity-city/Assets/Editor/Oboz/BraxObozActors.cs").read_text(encoding="utf-8")


class CampAndroidCrashDiagnostics(unittest.TestCase):
    def test_camp_boot_scene_is_not_a_game_scene(self):
        self.assertIn('sceneName == "BraxhornObozStart"', LOGGER)
        self.assertIn('sceneName == "BraxhornStart"', LOGGER)
        self.assertIn("!IsLoadingScene(SceneManager.GetActiveScene().name)", LOGGER)
        self.assertIn("if (!IsLoadingScene(s.name) && m == LoadSceneMode.Single)", LOGGER)

    def test_os_exit_reason_is_read_only_and_optional(self):
        self.assertIn("#if UNITY_ANDROID && !UNITY_EDITOR", LOGGER)
        self.assertIn('"getHistoricalProcessExitReasons"', LOGGER)
        self.assertIn("BraxStart.PowodZamkniecia(reason)", LOGGER)
        self.assertIn("catch (System.Exception ex)", LOGGER)
        self.assertIn("androidExitChecked = true", LOGGER)
        self.assertNotIn("PlayerPrefs.DeleteAll(", LOGGER)

    def test_early_awake_marker_on_real_camp_scene(self):
        self.assertIn("info.gameObject.AddComponent<BraxStartZnacznik>();", ASSEMBLER)
        self.assertIn('public const string ScenePath = "Assets/PlayableGenerated/Oboz/BraxhornObozReview.unity";', ASSEMBLER)


if __name__ == "__main__":
    unittest.main()
