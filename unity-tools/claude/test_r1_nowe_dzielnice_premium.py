"""MIASTO R1 — nowa część miasta „premium” (właściciel 9.11 18:13–18:21: z tarasu Tower „czarne bryły budynków”, „każdy budynek
premium, budynek po budynku”, tramwaj: „drzwi się źle otwierają, nie łamie się na zakrętach, ma elegancko kursować”).
Źródło, bez Unity i APK."""
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
REG = ROOT / "unity-city/Assets/Editor/Claude/Region"


def code(p):
    return re.sub(r'//[^\n]*', "", (ROOT / p).read_text(encoding="utf-8"))


SHADER = (ROOT / "unity-city/Assets/Claude/Shaders/SzkloFasady.shader").read_text(encoding="utf-8")


class R1SzkloFasad(unittest.TestCase):
    def test_glass_reflects_real_sky_with_fresnel_rooms_and_mullions(self):
        self.assertIn('Shader "Braxhorn/SzkloFasady"', SHADER)
        self.assertIn('#include "MiastoTlo.cginc"', SHADER)
        self.assertIn("MT_Niebo(R, kat * 3)", SHADER)                 # odbicie nieba gry, nie stały kolor
        self.assertIn("pow(1 - cosv, 5)", SHADER)                     # Fresnel
        self.assertIn("float t = min(min(tx, ty), tz);", SHADER)      # pokój za szybą
        self.assertIn("fwidth(u)", SHADER)                            # siatka bez mory
        self.assertIn('UsePass "Braxhorn/CityStaticPBR/SHADOWCASTER"', SHADER)
        self.assertIn("_BraxMgielkaGestosc", SHADER)

    def test_new_districts_and_towers_use_facade_glass(self):
        nd = code("unity-city/Assets/Editor/Claude/Region/BraxRegionMiastoBDzielnice.cs")
        self.assertIn('ndSzklo = BraxSzkloFasady.Mat("nowe dzielnice"', nd)
        ty = code("unity-city/Assets/Editor/Claude/Region/BraxRegionTylyB.cs")
        self.assertIn('BraxSzkloFasady.Mat("wieżowiec " + d.nazwa', ty)

    def test_sky_parameters_copied_after_sky_is_built(self):
        hz = code("unity-city/Assets/Editor/Claude/Region/BraxRegionHoryzont.cs")
        a = hz.index('Krok("niebo i panorama 360°", Niebo);')
        b = hz.index("BraxSzkloFasady.UstawTlo(UstawTlo)")
        self.assertLess(a, b)


if __name__ == "__main__":
    unittest.main()
