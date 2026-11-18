"""MIASTO R1 — panorama miasta ze zdjęć 360° (właściciel 18.11 11:32–11:39: „panorama… tragedia… nawet nie z Minecrafta”,
„tam, gdzie jest morze — inna, gdzie nie ma — inna, profesjonalnie połączone”; wybór zdjęć „numer 1, numer 2”).
Test pilnuje źródła (bez Unity i APK): plik sklejki w repo, generator, tryb zdjęcia w shaderze i w budowie horyzontu."""
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


def code(rel):
    return re.sub(r'//[^\n]*|/\*[\s\S]*?\*/', "", (ROOT / rel).read_text(encoding="utf-8"))


HOR = code("unity-city/Assets/Editor/Claude/Region/BraxRegionHoryzont.cs")
TLO = code("unity-city/Assets/Claude/Shaders/MiastoTlo.cginc")
NIEBO = code("unity-city/Assets/Claude/Shaders/MiastoNiebo.shader")
GEN = (ROOT / "unity-tools/claude/panorama_miasta.py").read_text(encoding="utf-8")
PLIK = ROOT / "unity-city/Assets/Source/Claude/Miasto/sky/panorama_miasta_8k.jpg"


class R1PanoramaMiasta(unittest.TestCase):
    def test_photo_file_in_repo_8k(self):
        self.assertTrue(PLIK.is_file())
        d = PLIK.read_bytes()
        self.assertEqual(d[:2], b"\xff\xd8")                               # JPEG
        i = d.find(b"\xff\xc0") if d.find(b"\xff\xc0") > 0 else d.find(b"\xff\xc2")
        h, w = int.from_bytes(d[i + 5:i + 7], "big"), int.from_bytes(d[i + 7:i + 9], "big")
        self.assertEqual((w, h), (8192, 4096))

    def test_generator_sources_and_seam(self):
        for s in ("polyhaven.com/a/cannon", "polyhaven.com/a/dalkey_view", "CC0"):
            self.assertIn(s, GEN)
        self.assertIn("A_HOR, B_HOR = -0.571, -0.439", GEN)                 # oba horyzonty na elewacji 0
        self.assertIn("SEKTOR = (48.5, 128.5)", GEN)                        # szew tylko nad wodą zatoki
        self.assertIn("np.median(a, 0) / np.maximum(np.median(b, 0), 1e-4)", GEN)   # kolor rząd po rzędzie
        self.assertIn("retusz(A, 6.8, 22.0, -0.3, 4.9, -20.0, .7)", GEN)    # działo z budką fotografa

    def test_shader_photo_mode(self):
        self.assertIn("half _FotoHoryzont;", TLO)
        self.assertIn("if (morze && _FotoHoryzont > 0.5)", TLO)
        self.assertIn("c = _MorzeKolor.rgb * lerp(0.78, 1.0, fres);", TLO)    # morze z koloru wody zdjęcia, bez lustra lądu
        self.assertIn("* 0.55 * (1.0 - saturate(_FotoHoryzont))", TLO)       # bez mgiełki przy horyzoncie na zdjęciu
        self.assertIn("MT_NieboProbka(d, _NieboObrot, katPiksela)", TLO)      # tuż pod horyzontem to samo zdjęcie
        self.assertIn('_FotoHoryzont ("Zdjęcie 360° w układzie miasta (1 = tak)", Float) = 0', NIEBO)

    def test_horizon_uses_photo_without_procedural_ridges(self):
        self.assertIn('PanoramaMiasta = "Assets/Source/Claude/Miasto/sky/panorama_miasta_8k.jpg"', HOR)
        self.assertIn("nieboTex = NieboHD(PanoramaMiasta);", HOR)
        a = HOR.index("if (fotoPanorama)\n"); b = HOR.index("return;", a)
        blok = HOR[a:b]
        self.assertIn('m.SetFloat("_FotoHoryzont", 1f);', blok)
        self.assertIn('m.SetFloat("_NieboObrot", -Mathf.PI * .5f);', blok)   # u = azymut / 360°
        self.assertIn('m.SetFloat("_NieboChmury", 0f);', blok)               # skały zdjęcia się nie obracają
        self.assertIn('m.SetFloat("_PasmoJest", 0f);', blok)                 # bez pasa Alps
        self.assertIn('new Vector4(R1, R2, 0f, 0f)', blok)                    # grzbiety z profilu = 0 m
        self.assertIn('m.SetFloat("_MgielkaGestosc", 0f);', blok)
        self.assertIn('m.SetColor("_MorzeKolor", MorzeZdjecia);', blok)


if __name__ == "__main__":
    unittest.main()
