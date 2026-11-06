"""Offline P1 original owner sign contract. No Unity, no APK, no network."""
from pathlib import Path
import unittest
import re

ROOT=Path(__file__).resolve().parents[2]
SIG=(ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsPoolP1OriginalSign.cs")
POOL=(ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsPoolP1.cs")
IMPORT=(ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsCitySignsTextureImport.cs")


class PoolP1OriginalSignTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.sign=SIG.read_text(encoding="utf-8")
        cls.pool=POOL.read_text(encoding="utf-8")

    def test_only_original_owner_citysigns_and_exact_pixels(self):
        s=self.sign
        self.assertIn('const string Signs="Assets/Source/Mistrzowie/CitySigns"',s)
        self.assertIn('P1_BASEN_BASEN_MIEJSKI.png',s)
        self.assertIn('P1_SPA_SPA_WELLNESS.png',s)
        self.assertNotIn('IndexOf("BASEN"',s)
        self.assertNotIn('candidates.Length!=1',s)
        self.assertIn('sourceTexture.width!=2048 || sourceTexture.height!=512',s)
        self.assertIn('artMat.SetTexture("_MainTex",sourceTexture);',s)
        self.assertIn('ImageConversion.LoadImage',s)
        self.assertIn('if(string.IsNullOrEmpty(source))',s)
        self.assertIn('P1 ORIGINAL BASEN SIGN | SOURCE ZIP NOT YET IMPORTED',s)
        self.assertNotIn('GeneratedOwnerLogo',s)
        self.assertIn('no crop | no recolour | no generated text',s)

    def test_4_to_1_dimensions_and_canopy_clearance(self):
        s=self.sign
        values=re.search(r"const float W=([\d.]+)f,H=([\d.]+)f;",s)
        self.assertIsNotNone(values)
        w,h=map(float,values.groups())
        self.assertEqual(w/h,4.)
        centre=re.search(r"const float cy=([\d.]+)f,cz=([-\d.]+)f;",s)
        self.assertIsNotNone(centre)
        cy=float(centre.group(1))
        self.assertGreater(cy-(h+.22)/2,5.14)  # canopy top
        self.assertLess(cy+(h+.22)/2,8.5)  # SPA roof top
        self.assertIn('Quaternion.Euler(0f,180f,0f)',s)

    def test_real_structure_mounts_and_no_duplicate_signage(self):
        s=self.sign
        self.assertIn('physical colour-matched 3D signage casing',s)
        self.assertIn('concealed wall standoff mount',s)
        self.assertIn('aluminium vertical extrusion',s)
        self.assertIn('architectural warm edge LED',s)
        self.assertIn('Part(frame.transform',s)
        self.assertIn('BraxChampionsPoolP1OriginalSign.Generate(root);',self.pool)
        self.assertNotIn('Sign("BASEN MIEJSKI + SPA"',self.pool)
        self.assertNotIn('Sign("BRAXHORN CITY"',self.pool)

    def test_source_citysign_texture_import_keeps_original(self):
        importer=IMPORT.read_text(encoding="utf-8")
        self.assertIn('t.alphaSource=TextureImporterAlphaSource.FromInput',importer)
        self.assertIn('t.maxTextureSize=2048',importer)
        self.assertIn('t.sRGBTexture=true',importer)


if __name__=="__main__":
    unittest.main()
