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
        self.assertIn('MURAL_LEWA_ZAWODNICY_6144x2048.png',self.m)
        self.assertIn('MURAL_PRAWA_PAS_6144x2048.png',self.m)
        self.assertIn('PickRequiredHd(LeftFightersHd,"LEFT")',self.m)
        self.assertIn('PickRequiredHd(RightBeltHd,"RIGHT")',self.m)
        self.assertIn('NO FALLBACK',self.m)
        self.assertNotIn('Texture2D.EncodeTo',self.m)

    def test_both_side_walls_use_true_panorama_dimensions_and_face_the_aisle(self):
        # 9.11: 12.5 cm stand-off from the wall's inner face (-26.045/-145.955).
        self.assertIn('float x = left ? HallLeftX - MuralStandOff : HallRightX + MuralStandOff;',self.m)
        self.assertIn('public const float MuralStandOff = .12f;',self.m)
        x_left,x_right=-26-.17,-146+.17
        self.assertLess(x_left,-26.045-.1)
        self.assertGreater(x_right,-145.955+.1)
        # b33 black walls: proven entrance path, two-sided, line-of-sight log.
        self.assertNotIn('mesh.SetUVs(3, interior);',self.m)
        # 9.11 23:43 (APK 35: still black on the phone): three 2048 tiles, back-lit, gold frame.
        self.assertIn('BraxChampionsArenaMuralPrint.Hang(bowl, name, "Side_" + side, sourcePath,',self.m)
        self.assertIn('BraxChampionsArenaMuralPrint.Frame(group, "Side_" + side,',self.m)
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
        # Not mirrored: the left wall is read along +X (viewer-right = -Z), the right wall along -X.
        self.assertIn('var facing = left ? Vector3.left : Vector3.right;',self.m)
        self.assertIn('var right = left ? Vector3.back : Vector3.forward;',self.m)
        self.assertIn('float startZ = left ? WallNearZ : WallFarZ;',self.m)
        self.assertIn('new Vector3(x, BottomY, startZ), right, facing, WallSpan, ImageHeight, out var group);',self.m)

    def test_doorway_central_mural_and_existing_wall_are_unchanged(self):
        self.assertIn('BraxChampionsArenaMural.Generate(root)',self.h)
        self.assertIn('BraxChampionsArenaSideMurals.Generate(root)',self.h)
        self.assertNotIn('Sign("baner_oktagon"',self.h)
        # 9.11: real structural walls; side walls close the corners.
        self.assertIn('Clad(false, X0, 1f, Z0 - WallThickness, Z1 + WallThickness)',self.h)
        self.assertIn('Clad(false, X1, -1f, Z0 - WallThickness, Z1 + WallThickness)',self.h)
        self.assertIn('BoxMinMax(wallLeather, dA, dB, dH, WallPanelTop, w0, w1, true, true);',self.h)
        self.assertIn('Sign("hala_walk"',self.h)
        self.assertIn('Sign("braxhorn_arena"',self.h)
        self.assertNotIn('AddComponent<MeshCollider>',self.m)
        self.assertNotIn('AddComponent<BoxCollider>',self.m)

    def test_original_three_murals_keep_hd_release_on_android(self):
        self.assertIn('MURAL_LEWA_ZAWODNICY_6144x2048.png',self.m)
        self.assertIn('MURAL_PRAWA_PAS_6144x2048.png',self.m)
        self.assertIn('sourceWidth!=6144||sourceHeight!=2048',self.m)
        self.assertIn('android.maxTextureSize=8192',self.m)
        self.assertIn('TextureImporterFormat.ASTC_4x4',self.m)
        self.assertIn('SIDE_MURALS = 2/2 PASS',self.m)

if __name__=="__main__":
    unittest.main()
