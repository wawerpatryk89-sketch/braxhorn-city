"""Static regression of imported owner-supplied VIP GLB contract.
These tests do not simulate Unity, glTFast or Android.
"""
from pathlib import Path
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]
VIP = ROOT / "unity-city/Assets/Editor/Mistrzowie/BraxChampionsVIPSeats.cs"
ARENA = ROOT / "unity-city/Assets/Editor/Mistrzowie/BraxChampionsArena.cs"
BUILD = ROOT / "unity-city/Assets/Editor/Mistrzowie/BraxChampionsBuild.cs"


class OriginalVipModelContract(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.vip = VIP.read_text(encoding="utf-8")
        cls.arena = ARENA.read_text(encoding="utf-8")
        cls.build = BUILD.read_text(encoding="utf-8")

    def test_original_supplied_model_and_2k_mip_bounds_are_required(self):
        self.assertIn("free_luxurious_barrel_seat_2k.glb", self.vip)
        self.assertIn("PrefabUtility.InstantiatePrefab(source)", self.vip)
        self.assertIn("map.width > 2048 || map.height > 2048", self.vip)
        self.assertIn("map.mipmapCount <= 1", self.vip)
        self.assertIn("VerifyImportedTextures(source)", self.vip)
        self.assertIn("SeatCount != 16", self.vip)

    def test_exact_sixteen_nonoverlapping_seats_and_public_aisle(self):
        cols = re.search(r"var xs\s*=\s*new\[\]\s*\{([^}]*)\}", self.vip)
        rows = re.search(r"foreach\s*\(float z in new\[\]\s*\{([^}]*)\}\s*\)", self.vip)
        self.assertIsNotNone(cols)
        self.assertIsNotNone(rows)
        x = [float(n) for n in re.findall(r"-?\d+(?:\.\d+)?", cols.group(1))]
        z = [float(n) for n in re.findall(r"-?\d+(?:\.\d+)?", rows.group(1))]
        self.assertEqual((len(x), len(z), len(x)*len(z)), (8, 2, 16))
        self.assertGreaterEqual(z[1] - z[0], 1.75)
        self.assertGreaterEqual(min(b - a for a,b in zip(x[:4],x[1:4])), 1.45 - .001)
        self.assertGreaterEqual(min(b - a for a,b in zip(x[4:7],x[5:])), 1.45 - .001)
        self.assertIn("new Vector3(1.18f, 1.28f, 1.18f)",self.vip)
        for xx in x:
            self.assertTrue(xx + .59 <= -87.55 or xx - .59 >= -84.45)

    def test_judge_workstations_are_unchanged_and_vip_only_switches_on_import(self):
        self.assertIn("SeatCount += BraxChampionsVIPSeats.Generate(root)", self.arena)
        # The source guard belongs to the VIP importer itself, not the bowl.
        # Missing original GLB must return 0 and must not invent VIP replacements.
        self.assertIn("if (!source)", self.vip)
        self.assertIn("return 0;", self.vip)
        self.assertIn("BoxMinMax(deskTop, x0 - .05f, x1 + .05f", self.arena)
        self.assertIn("Seat(leather, false, z, s > 0 ? x0 - .75f : x1 + .75f, yF, s);", self.arena)
        self.assertIn("report.arenaVipSeats=BraxChampionsVIPSeats.SeatCount;", self.build)
        self.assertIn("report.ownerVip2kSourcePresent=BraxChampionsVIPSeats.HasSource;", self.build)


if __name__ == "__main__":
    unittest.main()
