"""Prevent prior Unity estate build failures before the expensive Android import."""
from pathlib import Path
import sys
import unittest

ROOT = Path(__file__).resolve().parents[2]


class EstateCIRegressions(unittest.TestCase):
    def test_club_light_allocated_in_awake_not_constructor(self):
        source = (ROOT / "unity-city/Assets/Claude/Runtime/BraxKlubSwiatlo.cs").read_text()
        self.assertNotIn("readonly MaterialPropertyBlock block=new MaterialPropertyBlock()", source)
        awake = source.split("void Awake()", 1)[1].split("void Apply(", 1)[0]
        self.assertIn("block = new MaterialPropertyBlock()", awake)

    def test_unity_version_pinned_consistently(self):
        version = (ROOT / "unity-city/ProjectSettings/ProjectVersion.txt").read_text()
        workflow = (ROOT / ".github/workflows/claude-region-build.yml").read_text()
        self.assertIn("m_EditorVersion: 6000.0.85f2", version)
        self.assertIn("m_EditorVersionWithRevision: 6000.0.85f2 (b028303d8e7d)", version)
        self.assertIn("unityVersion: 6000.0.85f2", workflow)
        self.assertNotIn("unityVersion: 6000.0.58f2", workflow)
        self.assertIn("Verify pinned patched Unity 6 editor image", workflow)

    def test_cache_uses_real_editor_version(self):
        sys.path.insert(0, str(ROOT / "unity-tools/updates"))
        from unity_cache import project_unity_version, profile
        from durable_cache import project_unity_version as durable_project_unity_version
        self.assertEqual(project_unity_version(ROOT / "unity-city"), "6000.0.85f2")
        self.assertEqual(durable_project_unity_version(ROOT / "unity-city"), "6000.0.85f2")
        current = profile(ROOT / "unity-city", "osiedle", project_unity_version(ROOT / "unity-city"))
        old = profile(ROOT / "unity-city", "osiedle", "6000.0.58f2")
        self.assertNotEqual(current["key"], old["key"])

    def test_failed_il2cpp_retains_verified_imports_without_bypassing_signing(self):
        workflow = (ROOT / ".github/workflows/claude-region-build.yml").read_text()
        self.assertIn("steps.unity_android.outcome == 'failure'", workflow)
        self.assertIn("unity-city/Library/Bee/artifacts/Android/il2cppOutput", workflow)
        self.assertIn("BRAXHORN_KEYSTORE_B64", workflow)
        self.assertIn("BRAK STAŁEGO KLUCZA", workflow)


if __name__ == "__main__":
    unittest.main()
