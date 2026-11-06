"""Original owner murals keep source dimensions and visual geometry.

Android imported textures may be reduced; a cosmetic quality check cannot
stop the requested exploratory test APK.
"""
from pathlib import Path
import unittest

ROOT=Path(__file__).resolve().parents[2]

class MuralsReviewNotBuildGates(unittest.TestCase):
    def test_original_source_dimensions_not_imported_android_size(self):
        for name in ("BraxChampionsArenaMural.cs","BraxChampionsArenaSideMurals.cs"):
            src=(ROOT/"unity-city/Assets/Editor/Mistrzowie"/name).read_text()
            self.assertIn("GetSourceTextureWidthAndHeight",src)
            self.assertIn("BraxBuildQuality.Warn(",src)
            self.assertIn("1536",src)
            self.assertIn("512",src)
        original=(ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsArenaMural.cs").read_text()
        self.assertIn("AddSegment(walls,m",original)
        self.assertIn("DoorLeft",original)
        side=(ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsArenaSideMurals.cs").read_text()
        self.assertIn("LeftFighters",side)
        self.assertIn("RightBelt",side)
        self.assertIn("no collider",side)

if __name__=="__main__":
    unittest.main()
