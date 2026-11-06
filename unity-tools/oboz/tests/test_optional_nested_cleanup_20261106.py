"""Cofanie błędnej dekoracji z istniejącego domku musi być bezpieczne."""
from pathlib import Path
import unittest
ROOT=Path(__file__).resolve().parents[3]
SOURCE=(ROOT/"unity-city/Assets/Editor/Oboz/BraxObozEnvironment.cs").read_text("utf8")
class NestedOptionalCleanup(unittest.TestCase):
    def test_rolls_back_nested_new_nodes_not_approved_old_objects(self):
        self.assertIn("GetComponentsInChildren<Transform>(true)",SOURCE)
        self.assertIn("new HashSet<int>",SOURCE)
        self.assertIn("x.parent && before.Contains(x.parent.gameObject.GetInstanceID())",SOURCE)
        self.assertIn("Object.DestroyImmediate(child.gameObject)",SOURCE)
        self.assertIn("doors.RemoveRange",SOURCE)
        self.assertIn("covered.RemoveRange",SOURCE)
        self.assertIn("BraxBuildQuality.Warn(",SOURCE)
