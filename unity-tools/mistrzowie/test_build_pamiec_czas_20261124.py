"""Build #54 (24.11, run 45334082585) died at 3 h 31 min: Unity killed for memory (exit 137) in the .glb texture step, and it
could not have finished in the 6 h job anyway - every texture was imported three times. The fixes:

  - BraxChampionsUstawieniaStartu ([InitializeOnLoad], batch mode only): the colour space (Linear) and the Android graphics
    APIs (OpenGLES3 only) are the build's own values from the first domain load, before the bulk import, so a texture is
    imported once (#54: 48 min Gamma + 45 min again for Linear; #52: 88 min a third time in BuildPlayer for the APIs);
  - BraxChampionsTeksturyGlb: a .glb is no longer read whole and kept to the end of the step (the boats' file is 824 MB) -
    its JSON is read and each image copied straight from the file; the import is skipped (the scene left as in #52) when
    memory + swap are short;
  - the workflow's swap 12 -> 24 GiB (#52 ended with 200 kB of 14 GiB swap left, #54 with 1 GB)."""
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"
START = (ED / "BraxChampionsUstawieniaStartu.cs").read_text(encoding="utf-8")
GLB = (ED / "BraxChampionsTeksturyGlb.cs").read_text(encoding="utf-8")
BUILD = (ED / "BraxChampionsBuild.cs").read_text(encoding="utf-8")
WF = (ROOT / ".github/workflows/mistrzowie-apk.yml").read_text(encoding="utf-8")


class Start(unittest.TestCase):
    def test_the_build_s_own_settings_from_the_first_load(self):
        self.assertIn("[InitializeOnLoad]", START)
        self.assertIn("if (!Application.isBatchMode) return;", START)
        self.assertIn("if (PlayerSettings.colorSpace != ColorSpace.Linear) { PlayerSettings.colorSpace = ColorSpace.Linear;", START)
        self.assertIn("PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);", START)
        self.assertIn("PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });", START)
        # exactly the values the build sets later (so they change nothing then and nothing is imported again)
        self.assertIn("PlayerSettings.colorSpace=ColorSpace.Linear;", BUILD)
        self.assertIn("PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);", BUILD)
        self.assertIn("PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{GraphicsDeviceType.OpenGLES3});", BUILD)
        # the texture format is already ASTC from Unity's start
        self.assertIn("customParameters: -setDefaultPlatformTextureFormat astc", WF)
        self.assertRegex((ED / "BraxChampionsUstawieniaStartu.cs.meta").read_text(encoding="utf-8"), r"guid: [0-9a-f]{32}")

    def test_touches_nothing_else(self):
        sets = re.findall(r"PlayerSettings\.(\w+)\s*\(|PlayerSettings\.(\w+)\s*=(?!=)", START)
        names = {a or b for a, b in sets}
        self.assertEqual(names, {"colorSpace", "SetUseDefaultGraphicsAPIs", "SetGraphicsAPIs", "GetGraphicsAPIs", "GetUseDefaultGraphicsAPIs"})
        for s in ("EditorUserBuildSettings", "QualitySettings", "AssetDatabase"):
            self.assertNotIn(s, START)


class TeksturyGlb(unittest.TestCase):
    def test_no_whole_file_in_memory(self):
        self.assertNotIn("File.ReadAllBytes", GLB)
        self.assertNotIn("plik.bytes", GLB)
        self.assertIn("using var f = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);", GLB)
        self.assertIn("using (var src = new FileStream(plik.path, FileMode.Open, FileAccess.Read, FileShare.Read)) obraz = Bajty(src, o.offset, o.length);", GLB)
        # the PNG / JPEG signature still checked at the image's start
        self.assertIn("bool png = b != null && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47;", GLB)

    def test_skipped_when_memory_is_short(self):
        self.assertIn("if (wolne >= 0 && wolne < MinPamiecMB)", GLB)
        self.assertIn('if (l.StartsWith("MemAvailable:") || l.StartsWith("SwapFree:"))', GLB)
        self.assertLess(GLB.index("if (wolne >= 0 && wolne < MinPamiecMB)"), GLB.index("AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);"))
        # #54 died with 2.2 GB RAM + 1 GB swap free: the threshold is above that
        self.assertGreater(int(re.search(r"MinPamiecMB = (\d+);", GLB).group(1)), 2200 + 1000)

    def test_images_one_at_a_time_55(self):
        """#55 (run 45374837203) was killed (exit 137) in this step 18 s after the import: ~250 ASTC images loaded at once
        and kept, each decompressed for the editor's software GPU, on top of the 12 GB of the built scene"""
        krok = GLB[GLB.index("AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);"):]
        # loaded one by one, put into the copies' slots, let go again before the next
        petla = krok[krok.index("foreach (var kv in sloty)"):krok.index("// 5. the copies")]
        self.assertIn("var t = AssetDatabase.LoadAssetAtPath<Texture2D>(kv.Key);", petla)
        self.assertIn("Resources.UnloadAsset(t);", petla)
        self.assertLess(petla.index("c.SetTexture(id, t)"), petla.index("Resources.UnloadAsset(t);"))
        # nothing collects the loaded images any more
        self.assertNotIn("nowe[kv.Key] = t", GLB)
        # short of memory midway: the rest stay original, the build goes on
        self.assertIn("if (w >= 0 && w < MinPamiecMB)", petla)
        self.assertIn("break;", petla)
        # only copies that really got a compressed image are saved and used
        self.assertIn("if (kv.Value == null || !zamienione.Contains(kv.Value)) continue;", krok)
        # the originals out of memory before the player is built
        self.assertIn("EditorUtility.UnloadUnusedAssetsImmediate();", krok[krok.index("SaveScene(scene)"):])


class Swap(unittest.TestCase):
    def test_24_gib(self):
        self.assertIn('sudo fallocate -l 24G "$swap"', WF)
        self.assertNotIn("fallocate -l 12G", WF)
        self.assertIn("- name: Prepare runner disk and 24 GiB Unity swap", WF)


if __name__ == "__main__":
    unittest.main()
