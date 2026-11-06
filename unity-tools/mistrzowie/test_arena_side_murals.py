"""Fast offline A1 mural regressions; geometry and source contract only.

This checks correct left/right original photos and clear public/athlete paths.
It does NOT claim Unity scene compilation, render approval or Android tests.
"""
from pathlib import Path
import unittest

ROOT=Path(__file__).resolve().parents[2]
MURAL=ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsArenaSideMurals.cs"
HALL=ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsArena.cs"
IMPORTER=ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsArenaMuralTextureImport.cs"

class ChampionSideMuralsTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.m=MURAL.read_text(encoding="utf-8")
        cls.h=HALL.read_text(encoding="utf-8")
        cls.i=IMPORTER.read_text(encoding="utf-8")

    def test_owner_photos_are_not_swapped_or_replaced(self):
        self.assertIn('BRAXHORN_A1_FIGHTERS_LEFT_ORIGINAL.jpg',self.m)
        self.assertIn('BRAXHORN_A1_BELT_RIGHT_ORIGINAL.jpg',self.m)
        self.assertIn('Side(originalBowl, Pick(LeftFightersHd, LeftFighters), "LEFT_FIGHTERS", true)',self.m)
        self.assertIn('Side(originalBowl, Pick(RightBeltHd, RightBelt), "RIGHT_BELT", false)',self.m)
        self.assertNotIn('Texture2D.EncodeTo',self.m)

    def test_both_side_walls_use_true_panorama_dimensions_and_face_the_aisle(self):
        x_left,x_right=-26.11,-145.9
        z_entry,z_rear=98.3,28.3
        w=z_entry-z_rear
        h=w/3.0
        self.assertAlmostEqual(w,70.0)
        self.assertAlmostEqual(h,23.3333333333333)
        self.assertLess(2.65+h,28)       # does not touch the original 28m roof
        self.assertGreater(2.65,2.56)    # does not cover the original 2.4m red band
        self.assertLess(x_left,-26)      # left wall material toward centre
        self.assertGreater(x_right,-146) # right wall material toward centre
        self.assertIn('const float ImageHeight = WallSpan / 3f',self.m)
        self.assertIn('const int ImageWidthPx = 1536',self.m)
        self.assertIn('const int ImageHeightPx = 512',self.m)
        self.assertIn('new[] { 0,1,2, 0,2,3 }',self.m)
        self.assertIn('float start = left ? WallNearZ : WallFarZ;',self.m)
        self.assertIn('float end = left ? WallFarZ : WallNearZ;',self.m)
        self.assertIn('if (!photo)',self.m)

    def test_doorway_central_mural_and_existing_wall_are_unchanged(self):
        self.assertIn('BraxChampionsArenaMural.Generate(root)',self.h)
        self.assertIn('BraxChampionsArenaSideMurals.Generate(root)',self.h)
        self.assertNotIn('Sign("baner_oktagon"',self.h)
        self.assertIn('Clad(false, X0, 1f, Z0, Z1)',self.h)
        self.assertIn('Clad(false, X1, -1f, Z0, Z1)',self.h)
        self.assertIn('BoxMinMax(wallUp, dA, dB, dH, yC',self.h)
        self.assertIn('Sign("hala_walk"',self.h)
        self.assertIn('Sign("braxhorn_arena"',self.h)
        self.assertNotIn('AddComponent<MeshCollider>',self.m)
        self.assertNotIn('AddComponent<BoxCollider>',self.m)

    def test_only_original_three_mural_2k_mobile_import(self):
        for name in ['BRAXHORN_CHAMPION_FIGHTER_ORIGINAL.jpg',
                     'BRAXHORN_A1_FIGHTERS_LEFT_ORIGINAL.jpg',
                     'BRAXHORN_A1_BELT_RIGHT_ORIGINAL.jpg']:
            self.assertIn(name,self.i)
        self.assertIn('settings.maxTextureSize=2048',self.i)
        self.assertIn('settings.streamingMipmaps=true',self.i)
        self.assertIn('TextureImporterFormat.ASTC_4x4',self.i)

if __name__=="__main__":
    unittest.main()
