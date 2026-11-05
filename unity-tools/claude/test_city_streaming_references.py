"""Guard city #97/#98: never split scripted signal controllers across scenes.

Static guard complements the real Unity preflight in BraxGymBuild.
No Unity runner is required to reject accidental source regressions.
"""
import pathlib
import re
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
SPLIT = ROOT / "unity-city/Assets/Editor/Claude/BraxClaudeObszary.cs"
CITY = ROOT / "unity-city/Assets/Editor/BraxGymBuild.cs"
LIGHT = ROOT / "unity-city/Assets/Claude/Runtime/BraxSwiatloDrogowe.cs"


class CityStreamingReferences(unittest.TestCase):
    def test_scripted_subtree_is_indivisible_on_boundary(self):
        source = SPLIT.read_text(encoding="utf-8")
        self.assertRegex(source, r"t\.GetComponent<LODGroup>\(\).*t\.GetComponent<MonoBehaviour>\(\)")
        self.assertLess(source.index("obszar.Contains(b.min)"), source.index("GetComponent<MonoBehaviour>()"))
        self.assertIn("foreach (Transform c in t) Zbierz(c)", source)

    def test_traffic_signal_keeps_all_renderer_references(self):
        source = LIGHT.read_text(encoding="utf-8")
        self.assertIn("public sealed class BraxSwiatloDrogowe : MonoBehaviour", source)
        self.assertRegex(source, r"public Renderer\s+czerwoneA, zolteA, zieloneA")
        self.assertIn("BRAXHORN_STREAMING_TEST_LOST_REFERENCES",
                      SPLIT.read_text(encoding="utf-8"))

    def test_actual_unity_preflight_runs_before_expensive_city_generation(self):
        source = CITY.read_text(encoding="utf-8")
        test_at = source.index("BraxClaudeObszary.SprawdzNiepodzielnoscSterownikow(scene);")
        city_at = source.index("BraxCityBuild.AttachCity(runtime,integrated,renovation)")
        self.assertLess(test_at, city_at)
        self.assertIn("if (fullCity && renovation && !osiedle)", source[:test_at][-100:])

    def test_cross_scene_references_still_block_corrupt_build(self):
        source = SPLIT.read_text(encoding="utf-8")
        self.assertIn("EditorSceneManager.DetectCrossSceneReferences(main)", source)
        self.assertIn("EditorSceneManager.DetectCrossSceneReferences(part)", source)
        self.assertIn("throw new InvalidOperationException(\"BRAXHORN_OBSZAR_REFERENCJE_MIEDZY_SCENAMI", source)


if __name__ == "__main__":
    unittest.main()
