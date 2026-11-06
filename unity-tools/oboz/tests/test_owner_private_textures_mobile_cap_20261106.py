"""Importer Android: źródła 4K zachowane, powierzchnie 2K ASTC, murale 4K."""
from pathlib import Path
import unittest
R=Path(__file__).resolve().parents[3]
S=(R/"unity-city/Assets/Editor/Oboz/BraxObozImport.cs").read_text("utf-8")
class Android2KOwnerMaterials(unittest.TestCase):
    def test_private_4k_source_preserved_but_mobile_2k(self):
        self.assertIn("int limit=is4k?4096:2048;",S)
        self.assertIn("t.maxTextureSize=limit;",S)
        self.assertIn("mobileSource.maxTextureSize=2048;",S)
        self.assertIn("mobileSource.format=sourceNormal?TextureImporterFormat.ASTC_4x4:",S)
    def test_murals_remain_4k_and_shelter_2k(self):
        self.assertIn("androidArtwork.maxTextureSize=ownerArtwork?4096:2048;",S)
        self.assertIn("t.textureShape=TextureImporterShape.Texture2D;",S)
        self.assertIn("t.textureType=TextureImporterType.Default;",S)
