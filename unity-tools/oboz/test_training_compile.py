"""Fast regression guard for the camp sparring CS0103 reported in APK #17."""
from pathlib import Path
import re
import unittest


ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "unity-city/Assets/Oboz/BraxObozTrainingRoutine.cs"
ACTORS = ROOT / "unity-city/Assets/Editor/Oboz/BraxObozActors.cs"
ENVIRONMENT = ROOT / "unity-city/Assets/Editor/Oboz/BraxObozEnvironment.cs"


class TrainingCompileRegression(unittest.TestCase):
    def test_sparring_scale_is_declared_and_consistent_with_scene(self):
        source = SOURCE.read_text(encoding="utf-8")
        actors = ACTORS.read_text(encoding="utf-8")
        environment = ENVIRONMENT.read_text(encoding="utf-8")
        for text in (source, actors, environment):
            self.assertRegex(text, r"\bconst float K\s*=\s*1\.294f\s*;")
        for expression in (
            "float reach=1.60f*K;",
            "Vector3 hitFrom=from.transform.position+Vector3.up*(.95f*K);",
            "Vector3 hitTo=to.transform.position+Vector3.up*(.95f*K);",
            "if (gap.magnitude > contactDistance*K)",
        ):
            with self.subTest(expression=expression):
                self.assertIn(expression, source)

    def test_physical_collision_and_navigation_are_not_disabled(self):
        source = SOURCE.read_text(encoding="utf-8")
        actors = ACTORS.read_text(encoding="utf-8")
        environment = ENVIRONMENT.read_text(encoding="utf-8")
        self.assertIn("Physics.Linecast(", source)
        self.assertIn("CharacterController", actors)
        self.assertIn("NavMesh.AddNavMeshData", source)
        self.assertIn("MeshCollider", environment)
        self.assertIn("ColliderBox(", environment)


if __name__ == "__main__":
    unittest.main()
