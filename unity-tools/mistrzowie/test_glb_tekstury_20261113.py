"""Owner 13.11.2026 09:54 ("dlaczego to tak dużo waży"): APK 43 had ~2 GB of uncompressed textures embedded in .glb files.
The build swaps them for ASTC copies of the same images before the area split; a failure never costs the build."""
import pathlib
import re
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
PASS = ROOT / "unity-city/Assets/Editor/Mistrzowie/BraxChampionsTeksturyGlb.cs"
BUILD = ROOT / "unity-city/Assets/Editor/Mistrzowie/BraxChampionsBuild.cs"


class GlbTextureCompressionTests(unittest.TestCase):
    def setUp(self):
        self.src = PASS.read_text(encoding="utf-8")
        self.build = BUILD.read_text(encoding="utf-8")

    def test_meta_exists_with_unique_guid(self):
        meta = (PASS.parent / (PASS.name + ".meta")).read_text()
        guid = re.search(r"guid: ([0-9a-f]{32})", meta).group(1)
        hits = [p for p in (ROOT / "unity-city/Assets").rglob("*.meta") if guid in p.read_text(errors="ignore")]
        self.assertEqual(len(hits), 1)

    def test_runs_before_the_split_in_both_paths(self):
        self.assertEqual(self.build.count("CompressGlbTextures(scene,report);\n                SplitIntoAreas(report);"), 2)

    def test_failure_keeps_the_scene_and_the_build(self):
        body = self.build[self.build.index("static void CompressGlbTextures"):self.build.index("static void SplitIntoAreas")]
        self.assertIn("catch(Exception e)", body)
        self.assertIn("BRAX_GLB_TEXTURES_FAILED", body)
        self.assertNotIn("throw", body)
        # the scene is saved only at the very end, after every swap
        self.assertLess(self.src.index("r.sharedMaterials = mats;"), self.src.index("EditorSceneManager.SaveScene(scene)"))

    def test_only_uncompressed_glb_textures_and_the_original_image_bytes(self):
        self.assertIn("TextureFormat.RGBA32", self.src)
        self.assertIn("TextureFormat.RGB24", self.src)
        self.assertIn('EndsWith(".glb"', self.src)
        self.assertIn("0x89 && b[start + 1] == 0x50", self.src)   # PNG signature checked
        self.assertIn("b[start] == 0xFF && b[start + 1] == 0xD8", self.src)  # JPEG signature checked
        self.assertIn('"image_" + i', self.src)                      # glTFast texture naming
        self.assertIn("plik.poNazwie[nazwa] = null", self.src)        # ambiguous names stay original

    def test_import_settings_astc_mipmaps_no_cpu_copy_full_resolution(self):
        imp = self.src[self.src.index("class BraxChampionsTeksturyGlbImport"):]
        for needle in ("TextureImporterFormat.ASTC_6x6", "mipmapEnabled = true", "isReadable = false",
                       "maxTextureSize = 4096", 'n.Contains("__lin")', "compressionQuality = 50"):
            self.assertIn(needle, imp)

    def test_stable_guid_so_the_import_memory_is_reused(self):
        self.assertIn("StalyGuid(", self.src)
        self.assertIn('".meta"', self.src)

    def test_generated_folders_only(self):
        self.assertIn('"Assets/PlayableGenerated/Mistrzowie/TeksturyGlb"', self.src)
        self.assertIn('"Assets/PlayableGenerated/Mistrzowie/MaterialyGlb"', self.src)
        self.assertNotIn("SaveAndReimport", self.src)                 # no .glb importer is touched


if __name__ == "__main__":
    unittest.main()
