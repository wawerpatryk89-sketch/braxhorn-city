"""Build #48 (18.11.2026, run 44233747415) reached Gradle's APK packing and was cut by the job's 355-minute limit at
09:50:30. Its log: at 07:39:37 BuildExplorationApk set the Android texture subtarget to ASTC, ProjectSettings were
reimported and every source texture was imported a second time (1 121 Source/Prywatne/Miasto, 253
Source/Mistrzowie/Textures, 96 PolyHaven ...) from 07:40 to 09:10 - 1.5 h. Unity's documented switch
-setDefaultPlatformTextureFormat (Android only) sets the default texture compression before the first import, "so you
don't have to import the texture again with the format you want".
#49 (run 44280844489) imported in 1 h 07 min (#48: 1 h 46 min) and then lost its runner at 11:59 mid-generation, on a
runner whose / is /dev/mapper/cow-root - as #47. Both died once ~80-90 GB had been written in all, about the free space
those runners start with; #43, #46, #48 ran on /dev/root. The job now checks its root first and stops in two minutes on a
copy-on-write root with under 130 GB left in its overlay; Unity logs memory, swap and disk every two minutes.
#50 (run 44324597131) ran on cow-root with 218 GB free in its overlay and died at 16:10 all the same: BRAX_RES showed Unity
at 10.2 GB, 3.9 GB of memory left and swap starting - onto the swap file on the dm-snapshot root. A copy-on-write root now
always stops the build."""
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


class Maszyna(unittest.TestCase):
    def test_a_copy_on_write_root_stops_the_build_first(self):
        job = WF[WF.index("  exploration-apk:"):]
        steps = job[job.index("    steps:"):]
        first = steps[:steps.index("      - name: Resolve the selected Champions source")]
        self.assertIn("- name: Runner disk check - a copy-on-write root stops the build in two minutes", first)
        self.assertIn("src=$(findmnt -no SOURCE / || true)", first)
        self.assertIn("sudo dmsetup status", first)
        # #50 (cow-root, 218 GB free in its overlay) still died: Unity 10.2 GB, 3.9 GB left, swap onto the dm-snapshot
        # root - a copy-on-write root now always stops the build
        self.assertNotIn('-lt 130', first)
        cow = first[first.index("*cow*)"):first.index("esac")]
        self.assertIn("exit 1 ;;", cow)
        self.assertNotIn("if ", cow)
        # it is the job's first step: nothing is downloaded or written before it
        self.assertEqual(steps.index("      - name: Runner disk check"), steps.index("      - name:"))

    def test_unity_logs_what_runs_out(self):
        src = (ROOT / "unity-city/Assets/Editor/Mistrzowie/BraxChampionsZasoby.cs").read_text(encoding="utf-8")
        self.assertTrue((ROOT / "unity-city/Assets/Editor/Mistrzowie/BraxChampionsZasoby.cs.meta").exists())
        self.assertIn("[InitializeOnLoad]", src)
        self.assertIn("if (!Application.isBatchMode) return;", src)
        self.assertIn("new System.Threading.Timer(_ => Log(), null, 0, 120000);", src)
        self.assertIn("AssemblyReloadEvents.beforeAssemblyReload", src)
        for k in ("MemAvailable:", "SwapFree:", "AvailableFreeSpace", "WorkingSet64", '"BRAX_RES "'):
            self.assertIn(k, src)


if __name__ == "__main__":
    unittest.main()
