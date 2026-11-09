"""Offline T11 2K source contract tests; never download or launch Unity."""
import hashlib
import io
import unittest
from pathlib import Path

from PIL import Image
from prepare_casino_textures_2k import MAPS, SLUGS, check, select

ROOT=Path(__file__).resolve().parents[2]

class CasinoTexture2KTests(unittest.TestCase):
    def test_eight_approved_polyhaven_materials_three_native_2k_channels(self):
        self.assertEqual(len(SLUGS),9)  # + floor_tiles_04 (owner board 9.11 cream)
        self.assertEqual(MAPS,(("diff","jpg"),("nor_gl","png"),("arm","png")))
        self.assertIn("clean_asphalt",SLUGS)
        self.assertIn("concrete_pavement_02",SLUGS)
        self.assertIn("concrete_brick_wall_001",SLUGS)
        self.assertNotIn("rectangular_facade_tiles_02",SLUGS)

    def test_selector_never_falls_back_to_4k(self):
        data={"Diffuse":{"4k":{"jpg":{"url":"wrong-four-k"}},
                         "2k":{"jpg":{"url":"correct-native-two-k"}}}}
        self.assertEqual(select(data,"granite_tile","diff","jpg")["url"],"correct-native-two-k")
        with self.assertRaisesRegex(RuntimeError,"native 2K"):
            select({"Diffuse":{"4k":{"jpg":{"url":"wrong-four-k"}}}},
                   "granite_tile","diff","jpg")

    def test_2k_dimensions_and_provider_checksum(self):
        image=Image.new("RGB",(2048,2048),(120,130,140))
        buffer=io.BytesIO()
        image.save(buffer,format="PNG")
        raw=buffer.getvalue()
        meta={"md5":hashlib.md5(raw).hexdigest(),"size":len(raw)}
        self.assertEqual(check(raw,meta,"test_2k.png"),hashlib.sha256(raw).hexdigest())
        with self.assertRaisesRegex(ValueError,"size mismatch"):
            check(raw,{"size":len(raw)+1},"test_2k.png")
        with self.assertRaisesRegex(ValueError,"checksum mismatch"):
            check(raw,{"md5":"0"*32},"test_2k.png")

    def test_casino_does_not_use_historic_4k_material_source(self):
        src=(ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsCasinoT11.cs").read_text()
        helper=(ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsCasinoPbr2K.cs").read_text()
        self.assertNotIn('BraxChampionsFoundation.SourceMaterial("t11_',src)
        self.assertEqual(src.count("BraxChampionsCasinoPbr2K.Create(output,"),8)  # sandstone no longer used
        self.assertIn('"clean_asphalt",2.1f,new Color(.40f,.40f,.41f)',src)
        self.assertIn('+"_2k."+extension',helper)
        self.assertIn("ASTC_4x4",helper)

if __name__=="__main__":
    unittest.main()
