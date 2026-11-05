"""Fast regression guard for the camp sparring CS0103 reported in APK #17."""
from pathlib import Path
import re
import unittest


SOURCE = Path(__file__).resolve().parents[2] / "unity-city/Assets/Oboz/BraxObozTrainingRoutine.cs"


class TrainingCompileRegression(unittest.TestCase):
    def test_sparring_distances_do_not_reference_undefined_K(self):
        source = SOURCE.read_text(encoding="utf-8")
        self.assertNotRegex(source, r"\bK\b", "CS0103: K is not defined in the training routine")
        for expression in (
            "float reach=1.60f;",
            "Vector3 hitFrom=from.transform.position+Vector3.up*.95f;",
            "Vector3 hitTo=to.transform.position+Vector3.up*.95f;",
            "if (gap.magnitude > contactDistance)",
        ):
            with self.subTest(expression=expression):
                self.assertIn(expression, source)


if __name__ == "__main__":
    unittest.main()
