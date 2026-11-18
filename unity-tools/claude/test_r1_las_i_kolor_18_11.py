"""MIASTO R1 18.11 — las przy kolei zielony, nie szary (14:56: „Ten twój las koło kolei… nie jest zielony, tylko matowy, jakiś
szary”) i kolor jak na Osiedlu także na telefonie (14:55: „głębia kolorów, kontrast jak na osiedlu?”)."""
import struct
import json
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
REG = ROOT / "unity-city/Assets/Editor/Claude/Region"
REGION = (REG / "BraxRegion.cs").read_text(encoding="utf-8")
LAS = (REG / "BraxMiastoDworzecLas.cs").read_text(encoding="utf-8")
OBRAZ = (REG / "BraxR1Obraz.cs").read_text(encoding="utf-8")
PBR = (ROOT / "unity-city/Assets/CityRebuild/Shaders/CityStaticPBR.shader").read_text(encoding="utf-8")
LIB = ROOT / "unity-city/Assets/Source/Claude/Library"


class Las(unittest.TestCase):
    def test_foliage_without_sky_sheen(self):
        # przyczyna szarości: odbicie nieba × Fresnel na kartach koron pod ostrym kątem
        self.assertIn("colour += environment*viewFresnel*(1-.65*roughness)*_ReflectionStrength*ambientAO*ekspozycja;", PBR)
        self.assertIn('if (c.HasProperty("_ReflectionStrength")) c.SetFloat("_ReflectionStrength", 0f);', REGION)
        self.assertIn('c.SetFloat("_Roughness", 1f);', REGION)
        self.assertIn("if (mode == 1) LiscieMatowe(src, c);", REGION)                 # glTF MASK
        self.assertIn("if (cutout) LiscieMatowe(src, c2);", REGION)                   # Standard / URP z wycięciem
        self.assertIn('"korona|lisc|liść|liscie|liście|leaf|leaves|foliage|needle|twig|fern|paproc|shrub|bush|krzew|grass|trawa|ivy|palm|frond|igl"', REGION)

    def test_forest_crowns_tinted(self):
        self.assertIn("OdcienKoronyLasu = new Vector3(1.22f, 1.34f, 1.0f);", REGION)
        self.assertIn('if (src.name.StartsWith("las_") && src.name.Contains("_korona"))', REGION)
        self.assertIn("BraxRegion.koronyLasu", LAS)
        # materiały koron w modelach lasu: maska alfa i nazwa „…_korona” (trafiają w poprawkę)
        for f in ("las_jodla_a", "las_sosna_a", "las_drzewko"):
            b = (LIB / (f + ".glb")).read_bytes(); jl = struct.unpack("<I", b[12:16])[0]; J = json.loads(b[20:20 + jl])
            kor = [m for m in J["materials"] if m["name"].endswith("_korona")]
            self.assertEqual(len(kor), 1, f)
            self.assertEqual(kor[0].get("alphaMode"), "MASK", f)
            self.assertTrue(kor[0]["name"].startswith("las_"), f)


class KolorJakOsiedle(unittest.TestCase):
    def test_colour_pass_on_phone_too(self):
        self.assertNotIn("if (telefon) { if (kol) Object.DestroyImmediate(kol); }", OBRAZ)
        self.assertIn("if (!kol) kol = cam.gameObject.AddComponent<BraxClaudeKolor>();", OBRAZ)
        self.assertIn('kol.shader = Shader.Find("Braxhorn/OsiedleKolor"); kol.nasycenie = 1.15f; kol.kontrast = 1.06f;', OBRAZ)


if __name__ == "__main__":
    unittest.main()
