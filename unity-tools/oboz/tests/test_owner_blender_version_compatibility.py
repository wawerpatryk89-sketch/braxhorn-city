"""Pre-Unity regression: original owner's Grass Medium was saved with Blender 4.4.

On ubuntu-24.04, apt blender is 4.0.2 and must NOT be used for these files.
This checks build wiring, binary pinning and version guard, not Blender execution.
"""
from pathlib import Path
import unittest

ROOT=Path(__file__).resolve().parents[3]
WF=(ROOT/".github/workflows/oboz-unity-build.yml").read_text("utf-8")
EXPORT=(ROOT/"unity-tools/oboz/owner_blender_export.py").read_text("utf-8")


class Blender44OwnerAssetGuard(unittest.TestCase):
    def test_official_supported_blender_45_pinned_and_hashed(self):
        self.assertIn("VERSION=4.5.15",WF)
        self.assertIn("https://download.blender.org/release/Blender4.5",WF)
        self.assertIn("blender-4.5.15-linux-x64.tar.xz",WF)
        self.assertIn("0d2b8bb2efe97e6c8eb57de6b7322f8800e1984e3125b60decca5f6771e4773e",WF)
        self.assertIn("sha256sum --check blender-linux.sha256",WF)
        self.assertIn('>> "$GITHUB_PATH"',WF)
        self.assertNotIn("install --no-install-recommends -y blender zstd",WF)

    def test_original_file_header_checked_before_export(self):
        self.assertIn('with source.open("rb") as stream:header=stream.read(12)',EXPORT)
        self.assertIn('source_version=int(header[9:12])',EXPORT)
        self.assertIn('if source_version>blender_version:',EXPORT)
        self.assertIn('if blender_version<404:',EXPORT)
        self.assertIn('match=re.search(r"Blender (\\d+)\\.(\\d+)",info)',EXPORT)

    def test_exports_and_hash_checks_before_unity(self):
        self.assertIn("owner_blender_export.py",WF)
        self.assertIn("owner_geometry_preflight.py",WF)
        self.assertIn("buildMethod: BraxObozBuild.BuildAndroidReview",WF)
        self.assertLess(WF.index("run: python3 unity-tools/oboz/owner_blender_export.py"),
                        WF.index("run: python3 unity-tools/oboz/owner_geometry_preflight.py"))
        self.assertLess(WF.index("run: python3 unity-tools/oboz/owner_geometry_preflight.py"),
                        WF.index("buildMethod: BraxObozBuild.BuildAndroidReview"))


if __name__=="__main__":
    unittest.main()
