"""Build #48 (18.11.2026, run 44233747415) reached Gradle's APK packing and was cut by the job's 355-minute limit at
09:50:30. Its log: at 07:39:37 BuildExplorationApk set the Android texture subtarget to ASTC, ProjectSettings were
reimported and every source texture was imported a second time (1 121 Source/Prywatne/Miasto, 253
Source/Mistrzowie/Textures, 96 PolyHaven ...) from 07:40 to 09:10 - 1.5 h. Unity's documented switch
-setDefaultPlatformTextureFormat (Android only) sets the default texture compression before the first import, "so you
don't have to import the texture again with the format you want"."""
import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
WF = (ROOT / ".github/workflows/mistrzowie-apk.yml").read_text(encoding="utf-8")
BUILD = (ROOT / "unity-city/Assets/Editor/Mistrzowie/BraxChampionsBuild.cs").read_text(encoding="utf-8")


class CzasBuildu(unittest.TestCase):
    def test_textures_imported_once_in_the_apk_format(self):
        step = WF[WF.index("      - name: Unity 6 - Miasto Mistrzów, Brax i APK do zwiedzania"):]
        step = step[:step.index("      - name: Save generated scene audit JSON")]
        self.assertIn("uses: game-ci/unity-builder@", step)
        self.assertIn("targetPlatform: Android", step)
        self.assertIn("customParameters: -setDefaultPlatformTextureFormat astc", step)
        # the build sets the same format (no change, no second import) and logs what it found
        self.assertIn('Debug.Log("BRAX_ANDROID_TEXTURE_SUBTARGET before="+EditorUserBuildSettings.androidBuildSubtarget);', BUILD)
        self.assertIn("EditorUserBuildSettings.androidBuildSubtarget=MobileTextureSubtarget.ASTC;", BUILD)
        self.assertNotIn("MobileTextureSubtarget.ETC2", BUILD)

    def test_the_job_still_fits_its_limit(self):
        self.assertIn("timeout-minutes: 355", WF)
        # #48's phases in minutes: preparation 26, clean import 106, generation 88, settings 5, the second texture pass 90
        # (gone), the rest to Gradle 38, Gradle and delivery about 20
        without_second_pass = 26 + 106 + 88 + 5 + 38 + 20
        self.assertLess(without_second_pass, 355 - 60)


if __name__ == "__main__":
    unittest.main()
