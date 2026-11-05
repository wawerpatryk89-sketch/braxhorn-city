"""Pre-Unity regression: cosmetic VIP GLB warnings must not abort test APK.

This checks source contracts only. It neither imports original art nor claims
Unity/Android, navigation, or device verification.
"""
from pathlib import Path
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]
VIP = ROOT / "unity-city/Assets/Editor/Mistrzowie/BraxChampionsVIPSeats.cs"
ARENA = ROOT / "unity-city/Assets/Editor/Mistrzowie/BraxChampionsArena.cs"
BUILD = ROOT / "unity-city/Assets/Editor/Mistrzowie/BraxChampionsBuild.cs"
QUALITY = ROOT / "unity-city/Assets/Editor/BraxBuildQuality.cs"


class ChampionsTestApkQualityGates(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.vip = VIP.read_text(encoding="utf-8")
        cls.arena = ARENA.read_text(encoding="utf-8")
        cls.build = BUILD.read_text(encoding="utf-8")
        cls.quality = QUALITY.read_text(encoding="utf-8")

    def test_vip_mipmap_and_resolution_checks_are_advisory(self):
        self.assertRegex(self.vip, r"if\s*\(map\.mipmapCount\s*<=\s*1\)\s*BraxBuildQuality\.Warn\(")
        self.assertRegex(self.vip, r"if\s*\(map\.width\s*>\s*2048\s*\|\|\s*map\.height\s*>\s*2048\)\s*BraxBuildQuality\.Warn\(")
        self.assertRegex(self.vip, r"if\s*\(seen\s*==\s*0\)\s*BraxBuildQuality\.Warn\(")
        self.assertRegex(self.vip, r"if\s*\(SeatCount\s*!=\s*16\)\s*BraxBuildQuality\.Warn\(")

    def test_original_model_and_physical_colliders_still_exist(self):
        self.assertIn('free_luxurious_barrel_seat_2k.glb', self.vip)
        self.assertIn("PrefabUtility.InstantiatePrefab(source)", self.vip)
        self.assertIn("BraxChampionsVIPSeats.Generate(root)", self.arena)
        self.assertIn("new Vector3(.96f, 1.08f, .96f)", self.vip)
        self.assertIn("AddComponent<BoxCollider>()", self.vip)
        self.assertIn('throw new InvalidOperationException("Cannot instantiate original VIP seat GLB")', self.vip)
        self.assertIn("if(!skipWalkChecks)CheckExteriorAccess()", self.build)

    def test_asset_failures_still_reported_not_hidden(self):
        self.assertIn('report.sourceImportErrors=RecordSourceImportDiagnostics(reports)', self.build)
        self.assertIn('result.issues.Add(new ImportIssue', self.build)
        self.assertIn('File.WriteAllText(Path.Combine(reports,"source-imports.json")', self.build)
        self.assertIn("if (!Advisory) throw problem;", self.quality)
        self.assertIn("Debug.LogWarning", self.quality)


if __name__ == "__main__":
    unittest.main()
