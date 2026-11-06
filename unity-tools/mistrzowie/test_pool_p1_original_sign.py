"""P1 original BASEN + SPA sign contract; offline static regression only."""
from pathlib import Path
import unittest
import re

ROOT=Path(__file__).resolve().parents[2]
SIGN=ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsPoolP1OriginalSign.cs"
POOL=ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsPoolP1.cs"
IMPORT=ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsCitySignsTextureImport.cs"
PACK=ROOT/"unity-tools/mistrzowie/import_pool_p1_owner_assets.py"
WORKFLOW=ROOT/".github/workflows/mistrzowie-apk.yml"


class PoolP1OriginalSignTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.sign=SIGN.read_text(encoding="utf-8")
        cls.pool=POOL.read_text(encoding="utf-8")
        cls.pack=PACK.read_text(encoding="utf-8")

    def test_exact_two_owner_signs_not_generated_text(self):
        s=self.sign
        self.assertIn('const string Root="Assets/Source/Mistrzowie/CitySigns/01_GLOWNE/"',s)
        self.assertIn('P1_BASEN_BASEN_MIEJSKI.png',s)
        self.assertIn('P1_SPA_SPA_WELLNESS.png',s)
        self.assertIn('public const string PoolSign=Root+',s)
        self.assertIn('public const string SpaSign=Root+',s)
        self.assertIn('texture.width!=2048||texture.height!=512',s)
        self.assertIn('art.SetTexture("_MainTex",texture)',s)
        self.assertIn('ImageConversion.LoadImage',s)
        self.assertIn('var result=sum/count;result.a=1f;return result;',s)
        self.assertNotIn('IndexOf("BASEN"',s)
        self.assertNotIn('GeneratedOwnerLogo',s)
        self.assertNotIn('Sign("BASEN MIEJSKI + SPA"',self.pool)
        self.assertNotIn('Sign("BRAXHORN CITY"',self.pool)

    def test_two_physical_carriers_mount_away_from_public_glass(self):
        s=self.sign
        # Structural metal case and real wall standoffs, no image stretching:
        # height is always width/4 and art quad is 4:1.
        self.assertIn('var h=width/4f;',s)
        self.assertIn('new Vector3(width,h,1f)*K',s)
        self.assertIn('Owner colour-matched deep 3D sign case',s)
        self.assertIn('Steel rear wall mount',s)
        self.assertIn('Warm facade edge LED top',s)
        self.assertIn('Warm facade edge LED bottom',s)
        self.assertIn('Build(root,"BASEN_MIEJSKI",PoolSign,0f,6.80f,-28.40f,10.0f)',s)
        self.assertIn('Build(root,"SPA_WELLNESS",SpaSign,15.2f,5.37f,-28.40f,4.40f)',s)
        # Public front canopy top ~5.14m, P1 SPA roof starts ~8.2m.
        basen_centre=6.80; basen_h=10./4
        self.assertGreater(basen_centre-(basen_h+.18)/2,5.14)
        self.assertLess(basen_centre+(basen_h+.18)/2,8.20)
        self.assertEqual(self.pool.count('BraxChampionsPoolP1OriginalSign.Generate(root);'),1)

    def test_sign_manifest_source_integrity_and_stable_meta(self):
        p=self.pack
        self.assertIn('ARCHIVE_NAME="P1_BASEN_SPA_ORIGINALNE_WYPOSAZENIE_20261106.zip"',p)
        self.assertIn('ARCHIVE_SHA256="5dd31e8422b2bddc046a0692448ce8522f7a22aae3a5fa617e9a0d74a74f1263"',p)
        self.assertIn('if len(records)!=16',p)
        self.assertIn('meta=path+".meta"',p)
        self.assertIn('if meta not in assets',p)
        self.assertIn('for name in SOURCE_SIGN_NAMES:',p)
        self.assertIn('Refuse to replace an existing Unity source/GUID',p)
        workflow=WORKFLOW.read_text(encoding="utf-8")
        self.assertIn("p1-basen-spa-20261106",workflow)
        self.assertIn("import_pool_p1_owner_assets.py",workflow)

    def test_source_citysign_texture_import_mobile(self):
        importer=IMPORT.read_text(encoding="utf-8")
        self.assertIn('t.alphaSource=TextureImporterAlphaSource.FromInput',importer)
        self.assertIn('t.maxTextureSize=2048',importer)
        self.assertIn('t.sRGBTexture=true',importer)
        self.assertIn('t.streamingMipmaps=true',importer)
        self.assertIn('android.format=TextureImporterFormat.ASTC_4x4',importer)


if __name__=="__main__":
    unittest.main()
