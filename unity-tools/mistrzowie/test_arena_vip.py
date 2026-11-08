"""No-Unity regression: actual supplied GLB VIP seating and preserved judges."""
import itertools
import re
import unittest
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
VIP=ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsVIPSeats.cs"
ARENA=ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsArena.cs"

class ChampionVipSeatsTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.vip=VIP.read_text(encoding="utf-8")
        cls.arena=ARENA.read_text(encoding="utf-8")
        x=re.search(r"var xs\s*=\s*new\[\]\s*\{([^}]*)\}",cls.vip,re.S)
        z=re.search(r"foreach\s*\(float z in new\[\]\s*\{([^}]*)\}",cls.vip)
        if not x or not z:
            raise AssertionError("Actual VIP GLB placement coordinates missing")
        cls.xs=[float(v) for v in re.findall(r"-?\d+(?:\.\d+)?(?=f)",x.group(1))]
        cls.zs=[float(v) for v in re.findall(r"-?\d+(?:\.\d+)?(?=f)",z.group(1))]

    def test_real_glb_is_used_and_fake_chairs_removed(self):
        self.assertIn("BraxChampionsVIPSeats.Generate(root)",self.arena)
        self.assertNotIn("LuxuryVipArmchair(",self.arena)
        self.assertIn("free_luxurious_barrel_seat_2k.glb",self.vip)
        self.assertIn("PrefabUtility.InstantiatePrefab(source)",self.vip)
        self.assertIn("VerifyImportedTextures(source)",self.vip)
        self.assertIn("map.mipmapCount <= 1",self.vip)
        self.assertIn("map.width > 2048 || map.height > 2048",self.vip)

    def test_16_original_seats_do_not_overlap_each_other_or_central_aisle(self):
        self.assertEqual(len(self.xs),8)
        self.assertEqual(len(self.zs),2)
        self.assertEqual(len(set(self.xs)),8)
        self.assertEqual(len(set(self.zs)),2)
        # Approved premium envelope is 1.18 x 1.18 x 1.28m, uniform model scale.
        self.assertIn("new Vector3(1.18f, 1.28f, 1.18f)",self.vip)
        self.assertIn('"leather_white"',self.vip)
        self.assertIn("SolidBlackMetal()",self.vip)
        self.assertIn('source.GetFloat(property)>.25f',self.vip)
        self.assertIn("A1_VIP_MATERIAL_MAPPING",self.vip)
        footprints=[(x,z) for x in self.xs for z in self.zs]
        self.assertEqual(len(footprints),16)
        for (x,z),(xx,zz) in itertools.combinations(footprints,2):
            self.assertTrue(abs(x-xx)>=1.18 or abs(z-zz)>=1.18,
                            "VIP collider footprints overlap")
        for x,z in footprints:
            self.assertTrue(x+.59 < -87.55 or x-.59 > -84.45,
                            "Original VIP chair intersects centre carpet")
            self.assertGreater(z-.59,65.7)
        self.assertIn("SeatCount != 16",self.vip)

    def test_judges_and_commentators_unchanged(self):
        self.assertIn("new[] { (-94.0f, -93.3f, 1f), (-78.7f, -78.0f, -1f) }",self.arena)
        self.assertIn("BoxMinMax(deskTop, x0 - .05f, x1 + .05f",self.arena)
        self.assertIn("Seat(leather, false, z, s > 0 ? x0 - .75f : x1 + .75f, yF, s);",self.arena)
        self.assertIn("new[] { 61.8f, 63.3f, 64.8f }",self.arena)

if __name__=="__main__":
    unittest.main()
