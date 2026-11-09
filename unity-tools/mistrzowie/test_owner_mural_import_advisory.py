"""Original owner murals keep source dimensions and visual geometry.

Android imported textures may be reduced; a cosmetic quality check cannot
stop the requested exploratory test APK.
"""
from pathlib import Path
import unittest

ROOT=Path(__file__).resolve().parents[2]

class MuralsReviewNotBuildGates(unittest.TestCase):
    def test_original_source_dimensions_not_imported_android_size(self):
        original=(ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsArenaMural.cs").read_text()
        side=(ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsArenaSideMurals.cs").read_text()
        importer=(ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsArenaMuralTextureImport.cs").read_text()
        for src in (original,side):
            self.assertIn("BraxBuildQuality.Warn(",src)
        self.assertIn("GetSourceTextureWidthAndHeight",side)
        # 9.11 23:43: the entrance mural is hung by BraxChampionsArenaMuralPrint (3:1 enforced there).
        printer=(ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsArenaMuralPrint.cs").read_text()
        self.assertIn("A1 mural must stay 3:1",printer)
        self.assertIn("BraxChampionsArenaMuralPrint.Hang(",original)
        self.assertIn("sourceWidth!=6144||sourceHeight!=2048",side)
        self.assertIn("MURAL_WEJSCIE_CHAMPION_FIGHTER_6144x2048.png",original)
        self.assertIn("MURAL_LEWA_ZAWODNICY_6144x2048.png",side)
        self.assertIn("MURAL_PRAWA_PAS_6144x2048.png",side)
        self.assertNotIn("BRAXHORN_CHAMPION_FIGHTER_ORIGINAL.jpg",original)
        self.assertNotIn("BRAXHORN_A1_FIGHTERS_LEFT_ORIGINAL.jpg",side)
        self.assertNotIn("BRAXHORN_A1_BELT_RIGHT_ORIGINAL.jpg",side)
        self.assertIn("android.maxTextureSize=8192",importer)
        self.assertIn("android.maxTextureSize=8192",side)
        self.assertIn("TextureImporterFormat.ASTC_4x4",importer)
        self.assertIn("TextureImporterFormat.ASTC_4x4",side)
        self.assertNotIn("AddSegment(walls,m",original)   # 9.11 23:43: whole picture above the door, no wall segments
        self.assertIn("DoorLeft",original)
        self.assertIn("LeftFightersHd",side)
        self.assertIn("RightBeltHd",side)
        self.assertNotIn("AddComponent<MeshCollider>",side)
        self.assertNotIn("AddComponent<BoxCollider>",side)


    def test_original_belt_case_plaque_survives_android_import_resize(self):
        source=(ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsBeltShowcase.cs").read_text()
        self.assertIn("importer.GetSourceTextureWidthAndHeight(out int sourceWidth,out int sourceHeight)",source)
        self.assertIn("if(sourceWidth!=1536||sourceHeight!=512)",source)
        self.assertIn("A1_SHOWCASE_OWNER_IMAGE_WRONG_SOURCE_SIZE",source)
        # Source identity is checked from importer metadata, not the Android-resized runtime Texture2D.
        self.assertNotIn("if (image.width != 1536 || image.height != 512)",source)

if __name__=="__main__":
    unittest.main()
