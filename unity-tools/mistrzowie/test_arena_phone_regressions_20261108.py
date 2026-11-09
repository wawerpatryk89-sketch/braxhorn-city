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
        # Approved 8 Nov: darker glossy graphite glazing on BOTH A1 levels.
        self.assertIn('new Color(.18f,.20f,.23f,.48f)',self.buildings)
        self.assertIn('glass.SetFloat("_ReflectionStrength",.62f);',self.buildings)
        self.assertIn('var clear=Surface("clear_glass",new Color(.78f,.88f,.93f,.19f)',self.buildings)
        # Owner render 9.11: black mullions every 2nd bay (~2.8 m), belt 3.8 m.
        self.assertIn("else if(i%2==0)",self.arrival)
        self.assertIn("black_belt_360_owner_render_y7p60_to11p40",self.arrival)
        self.assertNotIn("interiorWarmBands",self.arrival)

    def test_showcase_matches_owner_corrections(self):
        # 8.11 phone proof: embossed face was backwards -> flip about local up.
        self.assertIn("phoneProofFrontFlip=true",self.showcase)
        self.assertIn("bestRotation=bestRotation*Quaternion.Euler(0,180f,0)",self.showcase)
        # 9.11 owner photo replaces the low base: tall wall vitrine, B crest
        # from the owner's atlas on the brass plaque, belt real-size.
        self.assertIn("A1_VITRINE_OWNER_PHOTO",self.showcase)
        self.assertIn("VITRINE_B_CREST",self.showcase)
        self.assertIn("public const float BeltLength=1.27f;",self.showcase)

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
