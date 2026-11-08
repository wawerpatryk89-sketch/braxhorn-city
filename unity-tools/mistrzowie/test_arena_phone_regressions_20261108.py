"""Phone-proof regressions for A1 arena fixes requested 2026-11-08.

Static checks only: they lock the exact source changes seen wrong in APK #28.
They do not claim Unity rendering or Android device verification.
"""
from pathlib import Path
import unittest

ROOT=Path(__file__).resolve().parents[2]

class ArenaPhoneRegressions(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.arena=(ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsArena.cs").read_text(encoding="utf-8")
        cls.arrival=(ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsArenaArrival.cs").read_text(encoding="utf-8")
        cls.buildings=(ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsBuildings.cs").read_text(encoding="utf-8")
        cls.showcase=(ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsBeltShowcase.cs").read_text(encoding="utf-8")
        cls.vip=(ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsVIPSeats.cs").read_text(encoding="utf-8")
        cls.mural=(ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsArenaMural.cs").read_text(encoding="utf-8")
        cls.side=(ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsArenaSideMurals.cs").read_text(encoding="utf-8")

    def test_internal_signs_are_not_mirrored(self):
        self.assertIn("Vector3.Cross(n,Vector3.up)*width*.5f",self.arena)
        self.assertNotIn("Vector3.Cross(Vector3.up,n)*width*.5f",self.arena)

    def test_inside_carpet_has_gold_posts_and_soft_red_ropes(self):
        self.assertIn('słupki VIP złote',self.arena)
        self.assertIn("W.Rura(rope",self.arena)
        self.assertIn("ropeSegments=6",self.arena)
        self.assertNotIn("BoxMinMax(rope, x - .025f",self.arena)

    def test_facade_is_smoked_and_not_overgridded(self):
        self.assertIn('new Color(.22f,.25f,.29f,.30f)',self.buildings)
        self.assertIn("else if(i%3==0)",self.arrival)
        self.assertIn("ISSUE27_black_belt_360_y10p85_to18p55",self.arrival)

    def test_showcase_matches_owner_corrections(self):
        self.assertIn("phoneProofFrontFlip=true",self.showcase)
        self.assertIn("bestRotation=bestRotation*Quaternion.Euler(0,180f,0)",self.showcase)
        self.assertIn("A1_SHOWCASE_PREMIUM",self.showcase)
        self.assertIn("tallBackPlate=false",self.showcase)
        self.assertIn('new Vector3(2.20f,.20f,.90f)',self.showcase)
        self.assertIn('new Vector2(1.20f,.40f)',self.showcase)
        self.assertIn("RIGHT_SIDE_B_CREST",self.showcase)

    def test_vip_sideways_failure_path_is_replaced(self):
        self.assertIn("CHAMPIONS_VIP_OWNER_GLB_UPRIGHT",self.vip)
        self.assertIn("uprightRatio=",self.vip)
        self.assertIn("backFacingFlip=",self.vip)
        self.assertNotIn("feet.Average",self.vip)
        self.assertNotIn("CHAMPIONS_VIP_UPRIGHT_UNCERTAIN",self.vip)

    def test_hd_murals_are_not_crushed_to_2k(self):
        self.assertIn("android.maxTextureSize=8192",self.mural)
        self.assertIn("TextureImporterFormat.ASTC_4x4",self.mural)
        self.assertIn("MURAL_WEJSCIE_CHAMPION_FIGHTER_6144x2048.png",self.mural)
        self.assertIn("android.maxTextureSize=8192",self.side)
        self.assertIn("MURAL_LEWA_ZAWODNICY_6144x2048.png",self.side)
        self.assertIn("MURAL_PRAWA_PAS_6144x2048.png",self.side)
        self.assertIn("NO FALLBACK",self.side)

if __name__=="__main__":
    unittest.main()
