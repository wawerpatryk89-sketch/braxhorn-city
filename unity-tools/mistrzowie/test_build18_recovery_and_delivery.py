"""Build #18 source-only regressions: no Unity, APK, asset import or network.

Safeguards for preserving original T11 art and delivering the signed APK
before OPTIONAL content-baseline/cache publication.
"""
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]

def read(path):
    return (ROOT / path).read_text(encoding="utf-8")


class Build18Regression(unittest.TestCase):
    def test_original_2k_maps_preserved_and_bounded_recovery(self):
        loader = read("unity-city/Assets/Editor/Mistrzowie/BraxChampionsCasinoPbr2K.cs")
        imported = read("unity-city/Assets/Editor/Mistrzowie/BraxChampionsCasinoT11TextureImport.cs")
        downloader = read("unity-tools/mistrzowie/prepare_casino_textures_2k.py")
        self.assertIn("GetSourceTextureWidthAndHeight(out int sourceWidth,out int sourceHeight)", loader)
        self.assertIn("sourceWidth!=2048 || sourceHeight!=2048", loader)
        self.assertIn("ImportAssetOptions.ForceUpdate", loader)
        self.assertIn("ImportAssetOptions.ForceSynchronousImport", loader)
        self.assertIn("AssetDatabase.LoadMainAssetAtPath(path)", loader)
        self.assertIn("BRAX_T11_IMPORT_RECOVERY", loader)
        self.assertIn("importer.SaveAndReimport()", loader)
        self.assertIn("android.format=TextureImporterFormat.ASTC_4x4", loader)
        self.assertIn("importer.streamingMipmaps=true", loader)
        self.assertIn('Assets/Source/Mistrzowie/CasinoT11/Textures2K/', imported)
        self.assertIn("TextureImporterCompression.CompressedHQ", imported)
        self.assertIn("MAPS = ((\"diff\", \"jpg\"), (\"nor_gl\", \"png\"), (\"arm\", \"png\"))", downloader)
        self.assertNotIn("File.Copy(", loader, "Do not silently replace owner-approved native 2K originals")

    def test_p1_2k_source_preview_does_not_kill_real_apk(self):
        loader = read("unity-city/Assets/Editor/Mistrzowie/BraxChampionsPoolP1Pbr2K.cs")
        original = read("unity-city/Assets/Editor/Mistrzowie/BraxChampionsPoolP1TextureImport.cs")
        self.assertIn("sourceWidth!=2048 || sourceHeight!=2048", loader)
        self.assertIn("ImportAssetOptions.ForceUpdate", loader)
        self.assertIn("BRAX_P1_IMPORT_RECOVERY", loader)
        self.assertIn("importer.SaveAndReimport()", loader)
        self.assertIn("AssetDatabase.LoadMainAssetAtPath(path)", loader)
        self.assertIn("android.format=TextureImporterFormat.ASTC_4x4", loader)
        self.assertIn("BraxBuildQuality.Warn(", loader)
        self.assertNotIn("if(!texture || texture.width!=2048", loader)
        self.assertNotIn("File.Copy(", loader)
        self.assertIn("Assets/Source/Mistrzowie/PoolP1/Textures2K/", original)

    def test_apk_is_delivered_before_optional_content_publication(self):
        wf = read(".github/workflows/mistrzowie-apk.yml")
        names = (
            "Validate APK identity and existing certificate; stage immutable output",
            "Immediately save APK as Actions artifact",
            "Independently save APK as a unique test release",
            "Confirm at least one real APK delivery before optional reports",
            "Archive immutable generated Champions scene and Addressables baseline",
            "Activate immutable Addressables base only after signed APK delivery",
        )
        locations = [wf.index("      - name: " + name) for name in names]
        self.assertEqual(locations, sorted(locations))
        self.assertIn("steps.apk_delivery.outcome == 'success' && hashFiles('unity-city/BraxContentBuild/base/manifest.json')", wf)
        self.assertNotIn("steps.content_baseline.outcome == 'success' && hashFiles('build/Android/braxhorn-mistrzowie.gotowe')", wf)
        self.assertIn("      - name: Archive immutable generated Champions scene and Addressables baseline\n        id: content_baseline\n        continue-on-error: true", wf)
        self.assertIn("      - name: Activate immutable Addressables base only after signed APK delivery\n        continue-on-error: true", wf)
        self.assertIn("if-no-files-found: error", wf, "Real APK must remain a required deliverable")

    def test_quality_is_report_only_but_compilation_failures_are_not_masked(self):
        quality = read("unity-city/Assets/Editor/BraxBuildQuality.cs")
        build = read("unity-city/Assets/Editor/Mistrzowie/BraxChampionsBuild.cs")
        self.assertIn("public static bool Advisory => !File.Exists(", quality)
        self.assertIn("BRAXHORN_QUALITY_ADVISORY", quality)
        self.assertIn("if (!Advisory) throw problem;", quality)
        self.assertIn("BraxChampionsBuild.BuildAndroidWithoutWalkChecks", read(".github/workflows/mistrzowie-apk.yml"))
        self.assertIn("BuildPipeline.BuildPlayer", build)
        self.assertIn("result.summary.result!=BuildResult.Succeeded", build)
        self.assertIn("BraxBuildQuality.Issue", build)

    def test_source_only_never_starts_unity_or_signing(self):
        wf = read(".github/workflows/mistrzowie-cheap-code-audit-20261106.yml")
        self.assertIn("BEZ UNITY", wf)
        self.assertIn("timeout-minutes: 2", wf)
        self.assertNotIn("game-ci/unity-builder", wf)
        self.assertNotIn("apk_delivery.py stage", wf)


if __name__ == "__main__":
    unittest.main(verbosity=2)
