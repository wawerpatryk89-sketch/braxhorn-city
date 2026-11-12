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

    def test_both_murals_hang_left_and_right_of_the_fighters_door_facing_the_hall(self):
        # owner 12.11 00:30-01:55: "W arenie. Po prawej i po lewej", "między nimi chodzi" - on the wall every visitor faces
        self.assertIn('public const float DoorWallZ = 28.3f, DoorCentreX = -86f;',self.m)
        self.assertIn('float z = DoorWallZ + .045f + MuralStandOff;',self.m)
        self.assertIn('public const float MuralStandOff = .12f;',self.m)
        self.assertIn('var facing = Vector3.forward;',self.m)
        self.assertIn('var right = Vector3.left;',self.m)
        self.assertIn('float xc = DoorCentreX + (left ? WallMuralCentreOffset : -WallMuralCentreOffset);',self.m)
        self.assertIn('BraxChampionsArenaMuralPrint.Hang(bowl, name, "Side_" + side, sourcePath,',self.m)
        self.assertIn('BraxChampionsArenaMuralPrint.Frame(group, "Side_" + side,',self.m)
        w,h,y0,off=52.5,17.5,8.0,28.5
        self.assertAlmostEqual(w,3*h)                       # the owner's 6144 x 2048 untouched
        x0,x1=-146,-26                                       # hall side walls
        dA,dB=-88.05,-83.95                                  # fighters' door
        for xc in (-86+off,-86-off):
            lo,hi=xc-w/2-.16,xc+w/2+.16                      # with the gold frame
            self.assertGreater(lo,x0); self.assertLess(hi,x1)
            self.assertTrue(hi<dA or lo>dB)                  # never over the door
        self.assertGreater(y0-.16,4.2+.35+1.8)               # over the door frame and the HALA WALK sign (4.75 +- .9)
        self.assertGreater(y0-.16,16*.4+.055+.95)            # over the top row of the end stands and its rail
        self.assertLess(y0+h+.16,26.62-.62+.6)               # under the ceiling beams
        self.assertNotIn('HallLeftX',self.m)                 # the short walls keep their leather (each mural once)

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
