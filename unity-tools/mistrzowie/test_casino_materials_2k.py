"""Offline contract: only new native 2K Poly Haven maps for Casino T11."""
import importlib.util
import unittest
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
SCRIPT=Path(__file__).with_name("prepare_casino_textures_2k.py")
GENERATOR=ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsCasinoT11.cs"
MATERIAL=ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsCasinoPbr2K.cs"
IMPORTER=ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsCasinoT11TextureImport.cs"

spec=importlib.util.spec_from_file_location("t11_pbr_2k",SCRIPT)
source=importlib.util.module_from_spec(spec)
spec.loader.exec_module(source)

class NativeCasino2KTests(unittest.TestCase):
    def test_all_casino_textures_are_native_2k(self):
        self.assertEqual(len(source.SLUGS),9)
        self.assertIn("concrete_brick_wall_001",source.SLUGS)
        self.assertNotIn("rectangular_facade_tiles_02",source.SLUGS)
        self.assertEqual(source.MAPS,(("diff","jpg"),("nor_gl","png"),("arm","png")))
        self.assertEqual(source.DEST.parts[-2:],("CasinoT11","Textures2K"))
        self.assertTrue(all("4k" not in slug and "1k" not in slug for slug in source.SLUGS))
        self.assertEqual(source.COLOUR_VARIANT["fabric_pattern_05"],"col_01")

    def test_selects_two_k_from_provider_only(self):
        mock={"nor_gl":{"2k":{"png":{"url":"https://polyhaven.invalid/2k/normal.png"}},"4k":{"png":{"url":"WRONG"}}}}
        self.assertEqual(source.select(mock,"granite_tile","nor_gl","png")["url"],
                         "https://polyhaven.invalid/2k/normal.png")
        with self.assertRaises(RuntimeError):
            source.select({"nor_gl":{"4k":{"png":{"url":"WRONG"}}}},
                          "granite_tile","nor_gl","png")

    def test_native_pbr_applied_without_four_k_conversion(self):
        code=GENERATOR.read_text(encoding="utf-8")
        pbr=MATERIAL.read_text(encoding="utf-8")
        importer=IMPORTER.read_text(encoding="utf-8")
        self.assertEqual(code.count("BraxChampionsCasinoPbr2K.Create("),8)
        self.assertIn('"t11_B1_black_asphalt"',code)
        self.assertIn('"clean_asphalt",2.1f,new Color(.40f,.40f,.41f)',code)
        self.assertNotIn("BraxChampionsFoundation.SourceMaterial(",code)
        self.assertIn('const string Source="Assets/Source/Mistrzowie/CasinoT11/Textures2K/"',pbr)
        self.assertIn('Load("nor_gl","png",false)',pbr)
        self.assertIn('Load("arm","png",false)',pbr)
        self.assertIn("importer.GetSourceTextureWidthAndHeight(out int sourceWidth,out int sourceHeight)",pbr)
        self.assertIn("if(sourceWidth!=2048 || sourceHeight!=2048)",pbr)
        self.assertIn("BraxBuildQuality.Warn(",pbr)
        self.assertIn("VerifyOriginalSourceMaps()",pbr)
        self.assertIn("importer.streamingMipmaps=true",importer)
        self.assertIn("TextureImporterFormat.ASTC_4x4",importer)

if __name__=="__main__":
    unittest.main()
