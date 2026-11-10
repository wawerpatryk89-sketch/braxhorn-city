"""Owner 10.11 19:44-19:51: "I panoramy nie widzę tylko niebo", "daj panoramę w tle" - the sea to the horizon, the coast,
hills and mountain ridges round the region, hazed into the HDR sky (BraxChampionsHorizon)."""
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"


class OwnerHorizon(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.cs = (ED / "BraxChampionsHorizon.cs").read_text()
        cls.shader = (ED / "BraxChampionsHorizon.shader").read_text()
        cls.explorer = (ED / "BraxChampionsExplorer.cs").read_text()
        cls.zasieg = (ROOT / "unity-city/Assets/Mistrzowie/BraxChampionsZasiegWidzenia.cs").read_text()

    def test_built_with_the_daylight_and_seen(self):
        self.assertIn("BraxChampionsHorizon.Generate();", self.explorer)
        self.assertLess(self.explorer.index("var hdrSky = BraxChampionsSky.Create();"), self.explorer.index("BraxChampionsHorizon.Generate();"))
        self.assertIn("camera.farClipPlane = BraxChampionsHorizon.ViewDistance;", self.explorer)
        far = float(re.search(r"public float zasieg = ([\d.]+)f;", self.zasieg).group(1))
        self.assertEqual(far, float(re.search(r"ViewDistance = ([\d.]+)f;", self.cs).group(1)))
        k = 2.33 / 1.80
        sea_out = float(re.search(r"SeaOut = ([\d.]+)f", self.cs).group(1))
        self.assertGreater(far, (sea_out - 750) * k)                       # the far sea edge is inside the view

    def test_haze_is_the_sky_at_the_horizon(self):
        self.assertIn("RenderSettings.fogMode = FogMode.ExponentialSquared;", self.cs)
        self.assertIn("new Color(.723f, .748f, .815f)", self.cs)            # kloofendal puresky 0..1 deg x 1.18, sRGB
        self.assertIn("#pragma multi_compile_fog", self.shader)
        self.assertIn("UNITY_APPLY_FOG(i.fogCoord, col);", self.shader)
        self.assertIn('Shader "Braxhorn/MistrzowieHorizonSource"', self.shader)
        self.assertTrue((ED / "BraxChampionsHorizon.shader.meta").exists() and (ED / "BraxChampionsHorizon.cs.meta").exists())

    def test_sea_meets_the_region_sea_edge_to_edge(self):
        self.assertIn("Rect(X0, CentreZ - R, X1, Z0);", self.cs)
        self.assertIn("X0 = 0f, X1 = 1200f, Z0 = -500f, Z1 = 1000f, QuayZ = -150f, SeaLevel = -2.0f", self.cs)
        self.assertIn('"/Materials/sea.mat"', self.cs)

    def test_no_brown_no_beige(self):
        for r, g, b in re.findall(r"new Color\(\.(\d+)f, \.(\d+)f, \.(\d+)f\)", self.cs):
            r, g, b = (float("." + v) for v in (r, g, b))
            self.assertFalse(r > g + .03 and g > b, (r, g, b))                  # warm (red > green > blue) = brown/beige


if __name__ == "__main__":
    unittest.main()
