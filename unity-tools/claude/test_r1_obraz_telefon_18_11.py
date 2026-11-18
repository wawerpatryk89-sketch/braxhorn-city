"""MIASTO R1 — 18.11 (właściciel 10:38–10:43, 20 zdjęć z APK 4f317df): „migota prawie wszystko, mocniej lub delikatniej”, „gra
zacina, zamula, szczególnie w siłowni”, „dziwne, wielkie przedmioty są powstawiane prawie w każdym budynku”.
Przyczyny z danych APK: telefon na poziomie jakości „Medium” z antiAliasing 0 (BraxGrafika.Msaa na telefonie nic nie robi),
pełnoekranowe przejście koloru co klatkę, lustro siłowni 80 m / 24 Hz, kontrola skali przeskalowała w mieście 494 meble
(stolik piekarni × 5, 3.2 m w podłodze). Test pilnuje naprawy w źródle (bez Unity)."""
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


def code(rel):
    return re.sub(r'//[^\n]*|/\*[\s\S]*?\*/', "", (ROOT / rel).read_text(encoding="utf-8"))


OBRAZ = code("unity-city/Assets/Editor/Claude/Region/BraxR1Obraz.cs")
SKALA = code("unity-city/Assets/Editor/Claude/BraxKontrolaSkali.cs")


class R1ObrazTelefon(unittest.TestCase):
    def test_msaa_on_every_quality_level(self):
        self.assertIn("for (int q = 0; q < QualitySettings.names.Length; q++)", OBRAZ)
        self.assertIn("QualitySettings.SetQualityLevel(q, false);", OBRAZ)
        self.assertIn("if (QualitySettings.antiAliasing < 4) { QualitySettings.antiAliasing = 4; msaaPoziomy++; }", OBRAZ)
        self.assertIn("QualitySettings.SetQualityLevel(poziomBylo, false);", OBRAZ)

    def test_no_fullscreen_colour_pass_on_phone(self):
        self.assertIn("if (telefon) { if (kol) Object.DestroyImmediate(kol); }", OBRAZ)
        self.assertIn("UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.Android", OBRAZ)

    def test_gym_mirror_cheaper(self):
        self.assertIn("lu.reflectionDistance = Mathf.Min(lu.reflectionDistance, 25f); lu.refreshRate = Mathf.Min(lu.refreshRate, 15f);", OBRAZ)

    def test_soft_shadows_and_bias(self):
        self.assertIn("var cienie = LightShadows.Soft;", OBRAZ)              # twarde „pełzają” = migotanie krawędzi cieni
        self.assertIn("sun.shadowBias = .05f; sun.shadowNormalBias = .4f;", OBRAZ)

    def test_depth_precision_near_plane(self):
        self.assertIn("bk.daleko = Mathf.Max(bk.daleko, .5f);", OBRAZ)

    def test_city_scale_check_report_only(self):
        self.assertIn('if (BraxKontrola.Obszar == "miasto") { w.poza = true; w.pomiar += " (miasto: tylko raport)"; return w; }', SKALA)
        a = SKALA.index('if (BraxKontrola.Obszar == "miasto") { w.poza = true;')
        self.assertLess(a, SKALA.index("j.Skaluj(f, j.Podstawa);"))          # przed jakimkolwiek skalowaniem


if __name__ == "__main__":
    unittest.main()
