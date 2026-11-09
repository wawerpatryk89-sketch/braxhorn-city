"""Build #18 source-only regressions: no Unity, APK, asset import or network.

Safeguards for preserving original T11 art and delivering the signed APK
before OPTIONAL content-baseline/cache publication.
"""
import os
import subprocess
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]

def read(path):
    """Read the worktree first, or the same local HEAD when sparse checkout omits it."""
    relative = Path(path)
    if relative.is_absolute() or ".." in relative.parts:
        raise ValueError("Regression source must be relative to the repository")
    try:
        return (ROOT / relative).read_text(encoding="utf-8")
    except FileNotFoundError as missing:
        # The APK's sparse checkout omits the cheap-audit workflow, although
        # that tracked file exists in the selected commit. Keep the assertion,
        # read that exact local Git object, and never fetch another branch.
        env = dict(os.environ, GIT_NO_LAZY_FETCH="1", GIT_TERMINAL_PROMPT="0")
        result = subprocess.run(
            ["git", "-c", "protocol.allow=never", "show", "HEAD:" + relative.as_posix()],
            cwd=ROOT, capture_output=True, text=True, encoding="utf-8",
            timeout=5, check=False, env=env,
        )
        if result.returncode:
            raise FileNotFoundError(
                "Regression source missing from both worktree and local HEAD: " + path
            ) from missing
        return result.stdout


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

    def test_all_geometry_and_walk_quality_reports_are_nonblocking(self):
        wf = read(".github/workflows/mistrzowie-apk.yml")
        preparation = wf[wf.index("      - name: Geometry and source preparation"):
                         wf.index("      - name: Prepare durable Addressables content baseline mode")]
        self.assertIn("prepare_casino_textures_2k.py", preparation)
        self.assertIn("build_infrastructure.py", preparation)
        self.assertIn("build_exteriors.py", preparation)
        for check in ("audit_metro.py", "audit_metro_network.py",
                      "validate_contract.py", "audit_layout.py",
                      "unittest discover"):
            self.assertNotIn(check, preparation)
        advisory = wf[wf.index("      - name: Advisory geometry and code reports"):
                      wf.index("      - name: Archive immutable generated Champions scene")]
        self.assertLess(wf.index("      - name: Confirm at least one real APK delivery"),
                        wf.index("      - name: Advisory geometry and code reports"))
        self.assertIn("continue-on-error: true", advisory)
        self.assertIn("'blocksApk':False", advisory)
        for check in ("audit_metro.py", "audit_metro_network.py",
                      "validate_contract.py", "audit_layout.py",
                      "'champions-source-tests'", "'content-snapshot-tests'",
                      "'validation-tests'"):
            self.assertIn(check, advisory)
        self.assertIn("status='PASS' if code==0 else 'WARNING'", advisory)

    def test_owner_sign_dimensions_are_verified_early_to_protect_original_artwork(self):
        casino = read("unity-city/Assets/Editor/Mistrzowie/BraxChampionsCasinoT11.cs")
        build = read("unity-city/Assets/Editor/Mistrzowie/BraxChampionsBuild.cs")
        self.assertIn("if(width!=2048||height!=512)", casino)
        self.assertIn('throw new InvalidOperationException("Original owner sign dimensions changed:', casino)
        self.assertIn("C1 KASYNO WAWER source SHA-256 differs from owner manifest", casino)
        self.assertIn("BraxChampionsCasinoT11.VerifyOriginalSigns();", build)
        self.assertLess(build.index("BraxChampionsCasinoT11.VerifyOriginalSigns();"),
                        build.index("BraxChampionsFoundation.Generate();"))


if __name__ == "__main__":
    unittest.main(verbosity=2)
