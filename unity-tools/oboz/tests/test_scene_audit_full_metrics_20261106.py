"""Scene audit reports murals, owner dimensions, and furniture overlap candidates."""
from pathlib import Path
import unittest
R=Path(__file__).resolve().parents[3]
C=(R/"unity-city/Assets/Editor/Oboz/BraxObozSceneAudit.cs").read_text("utf-8")
class RichSceneAudit(unittest.TestCase):
    def test_all_five_quality_dimensions_exist(self):
        self.assertIn("ReviewOriginalMurals",C)
        self.assertIn("ReviewDimensions",C)
        self.assertIn("ReviewFurnitureOverlaps",C)
        self.assertIn("report.presentMurals",C)
        self.assertIn("report.scaleWarnings",C)
        self.assertIn("report.overlapCandidates",C)
        self.assertIn("report.levitating",C)
        self.assertIn("report.embedded",C)
    def test_mural_original_ratio_and_wall_detection(self):
        self.assertIn("r.transform.lossyScale.x",C)
        self.assertIn("source.width/(float)source.height",C)
        self.assertIn("w<4096",C)
        self.assertIn("Physics.RaycastAll(from,-facing",C)
    def test_furniture_overlap_warn_only(self):
        self.assertIn("Mathf.Min(ox,Mathf.Min(oy,oz))<=Tolerance",C)
        self.assertIn("Kandydat nachodzenia (>3cm AABB)",C)
        self.assertNotIn('throw new Exception("Zbyt dużo błędów jakości"',C)
