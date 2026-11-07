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
            self.assertIn("GetSourceTextureWidthAndHeight",src)
            self.assertIn("BraxBuildQuality.Warn(",src)
        self.assertIn("const int SourceWidth=1536",original)
        self.assertIn("const int SourceHeight=512",original)
        self.assertIn("6144x2048",side)
        self.assertIn("sourceWidth != sourceHeight * 3",side)
        self.assertIn("BRAXHORN_A1_FIGHTERS_LEFT_ORIGINAL.jpg",side)
        self.assertIn("BRAXHORN_A1_BELT_RIGHT_ORIGINAL.jpg",side)
        self.assertIn("settings.maxTextureSize=2048",importer)
        self.assertIn("hd.maxTextureSize=8192",importer)
        self.assertIn("AddSegment(walls,m",original)
        self.assertIn("DoorLeft",original)
        self.assertIn("LeftFighters",side)
        self.assertIn("RightBelt",side)
        self.assertNotIn("AddComponent<MeshCollider>",side)\n        self.assertNotIn("AddComponent<BoxCollider>",side)


    def test_original_belt_case_plaque_survives_android_import_resize(self):
        source=(ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsBeltShowcase.cs").read_text()
        self.assertIn("importer.GetSourceTextureWidthAndHeight(out int sourceWidth, out int sourceHeight)",source)
        self.assertIn("if (sourceWidth != 1536 || sourceHeight != 512)",source)
        self.assertIn("A1_SHOWCASE_OWNER_IMAGE_ANDROID_IMPORT",source)
        # Wrong original bytes may be rejected, but a resized Unity import must
        # not make the owner-approved plaque disappear.
        start=source.index("if (image.width != 1536 || image.height != 512)")
        end=source.index("var shader = Shader.Find",start)
        self.assertNotIn("return;",source[start:end])

if __name__=="__main__":
    unittest.main()
