"""Owner 10.11 09:06-09:07: buildings appear only when coming closer ("stały problem").
BraxGrafika (shared, R1) cuts the camera at 220/420/700 units on phones; the region camera keeps the whole region."""
from pathlib import Path
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]
COMP = ROOT / "unity-city/Assets/Mistrzowie/BraxChampionsZasiegWidzenia.cs"
EXPLORER = ROOT / "unity-city/Assets/Editor/Mistrzowie/BraxChampionsExplorer.cs"
GRAFIKA = ROOT / "unity-city/Assets/Claude/BraxGrafika.cs"


class ViewDistanceTests(unittest.TestCase):
    def test_region_camera_keeps_whole_region_in_view(self):
        src = COMP.read_text(encoding="utf-8")
        far = float(re.search(r"public float zasieg = ([\d.]+)f;", src).group(1))
        k = 2.33 / 1.80
        diagonal = (1200 ** 2 + 1500 ** 2) ** .5 * k   # region 1200 x 1500 nominal metres, K once
        self.assertGreaterEqual(far, diagonal * .99)
        self.assertIn("void LateUpdate()", src)
        self.assertIn("kamera.farClipPlane = zasieg;", src)
        self.assertTrue((COMP.parent / (COMP.name + ".meta")).exists())

    def test_component_is_on_the_region_camera_only(self):
        self.assertIn("camera.gameObject.AddComponent<BraxChampionsZasiegWidzenia>();", EXPLORER.read_text(encoding="utf-8"))

    def test_shared_graphics_modes_are_what_cut_the_view(self):
        g = GRAFIKA.read_text(encoding="utf-8")
        cuts = [float(x) for x in re.findall(r"Zasieg\((\d+)f\)", g)]
        self.assertTrue(cuts and max(cuts[:3]) < 1000, cuts)   # phone modes stay below the region size -> override needed


if __name__ == "__main__":
    unittest.main()
