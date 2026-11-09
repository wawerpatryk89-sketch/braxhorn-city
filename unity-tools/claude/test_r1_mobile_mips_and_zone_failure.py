"""Miasto R1: budżet tekstur i pakowania APK na telefon (decyzja właściciela 9.11: „te apk każda jedna ma straszną wagę”).

Stan odniesienia: build 874f194 (3.11) = 743 MB. Zmiany 7.11 (2K na elewacjach, LZ4, mip 0 na telefonie) dały 1349 MB
i zostały cofnięte 9.11. Test pilnuje ZACHOWANIA (limity i opcje), nie dokładnego brzmienia kodu.
Source-only regression. Unity/Android and installed APK are NOT exercised.
"""
import re
import unittest
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
GRAPHICS=(ROOT/"unity-city/Assets/Claude/BraxGrafika.cs").read_text(encoding="utf-8")
BUILD=(ROOT/"unity-city/Assets/Editor/BraxGymBuild.cs").read_text(encoding="utf-8")
ZONES=(ROOT/"unity-city/Assets/Claude/BraxObszary.cs").read_text(encoding="utf-8")
MANIFEST=(ROOT/"unity-city/Packages/manifest.json").read_text(encoding="utf-8")

def strip_comments(src):
    return re.sub(r'//[^\n]*|/\*[\s\S]*?\*/', '', src)

class CityR1MobileTextureBudget(unittest.TestCase):
    def test_facade_textures_capped_at_1k_on_import(self):
        code=strip_comments(BUILD)
        m=re.search(r'int\s+maxClosePixels\s*=\s*([^;]+);',code)
        self.assertIsNotNone(m,"CapFacadeTextures musi ustawiać maxClosePixels")
        self.assertNotIn("2048",m.group(1),"elewacje/bruk/PBR Androida: limit 1K (9.11), nie 2K")
        self.assertIn("1024",m.group(1))
        self.assertIn("ps.maxTextureSize=maxClosePixels",code)

    def test_extracted_glb_and_city_pack_textures_capped_at_1k(self):
        extracted=strip_comments((ROOT/"unity-city/Assets/Editor/Claude/BraxClaudeGlbTekstury.cs").read_text(encoding="utf-8"))
        self.assertRegex(extracted,r'ps\.maxTextureSize\s*=\s*1024\s*;')
        self.assertNotRegex(extracted,r'ps\.maxTextureSize\s*=\s*platform\s*==\s*"Android"\s*\?\s*2048')

    def test_owner_graphics_only_murals_above_1k(self):
        bill=strip_comments((ROOT/"unity-city/Assets/Editor/Claude/BraxGrafikiImport.cs").read_text(encoding="utf-8"))
        # mural 19 (cała szara ściana siłowni, właściciel) = jedyny 4K; murale z bliska / ekran = 2K; reszta billboardów = 1K
        self.assertRegex(bill,r'plik\s*==\s*"19"\s*\?\s*4096')
        self.assertRegex(bill,r'zBliska\s*\?\s*2048\s*:\s*1024')

    def test_android_player_not_packed_with_lz4(self):
        code=strip_comments(BUILD)
        self.assertNotIn("CompressWithLz4",code,"LZ4 pogarsza kompresję APK (55 % → 76 %); zdjęte 9.11")
        self.assertNotIn("CompressWithLz4HC",code)

    def test_mobile_keeps_half_resolution_mip_clamp(self):
        code=strip_comments(GRAPHICS)
        m=re.search(r'static\s+void\s+Mip\s*\(\s*int\s+n\s*\)\s*\{([^}]*)\}',code)
        self.assertIsNotNone(m)
        body=m.group(1)
        self.assertIn("Mathf.Max(n, 1)",body,"telefon: limit mip ≥ 1 (zabezpieczenie po zamykaniu gry 31.10)")
        self.assertNotRegex(body,r'n\s*=\s*0\s*;')

    def test_no_unused_addressables_package(self):
        self.assertNotIn("com.unity.addressables",MANIFEST,"pakiet bez użycia w C# — wydłuża import i zmienia klucz pamięci Library")

    def test_loader_never_retains_lock_on_failed_scene_request(self):
        self.assertIn("try { op = SceneManager.LoadSceneAsync(o.scena, LoadSceneMode.Additive); }", ZONES)
        self.assertIn("if (op == null)", ZONES)
        self.assertIn("o.bladLadowania = true;", ZONES)
        self.assertIn("finally\n        {\n            Laduje = Mathf.Max(0, Laduje - 1);", ZONES)
        self.assertIn("!o.bladLadowania", ZONES)
        self.assertIn("if (zamrozony && player && player.body) player.body.enabled = true;", ZONES)

if __name__=="__main__":
    unittest.main()
