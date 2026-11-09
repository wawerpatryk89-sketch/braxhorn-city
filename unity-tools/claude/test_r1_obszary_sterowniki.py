"""MIASTO R1: podział na obszary nie rozdziela poddrzew ze skryptem (sygnalizacja #97/#98 z 5.11).

Sterownik BraxSwiatloDrogowe zostawał w scenie głównej, a jego szkła szły do kwadratu — po zapisie odwołania
między scenami gasły (światła martwe). Poddrzewo z MonoBehaviour albo LODGroup jest niepodzielne: całe w obszarze
(warunek Contains min/max wyżej) albo całe zostaje. Source-only; bez Unity i APK.
"""
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SPLIT = (ROOT / "unity-city/Assets/Editor/Claude/BraxClaudeObszary.cs").read_text(encoding="utf-8")
LIGHT = (ROOT / "unity-city/Assets/Claude/Runtime/BraxSwiatloDrogowe.cs").read_text(encoding="utf-8")


def strip(source):
    return re.sub(r'//[^\n]*|/\*[\s\S]*?\*/', "", source)


class R1AreasKeepScriptedSubtreesWhole(unittest.TestCase):
    def test_scripted_subtree_is_indivisible_on_boundary(self):
        code = strip(SPLIT)
        self.assertRegex(code, r"t\.GetComponent<LODGroup>\(\)\s*\|\|\s*t\.GetComponent<MonoBehaviour>\(\)\)\s*return;")
        self.assertLess(code.index("obszar.Contains(b.min)"), code.index("GetComponent<MonoBehaviour>()"))
        self.assertIn("foreach (Transform c in t) Zbierz(c)", code)

    def test_traffic_signal_holds_renderer_references_in_one_component(self):
        self.assertIn("public sealed class BraxSwiatloDrogowe : MonoBehaviour", LIGHT)
        self.assertRegex(LIGHT, r"public Renderer\s+czerwoneA, zolteA, zieloneA")

    def test_cross_scene_references_are_reported_not_fatal(self):
        # AGENTS.md: błędy wizualne/odwołań → ostrzeżenie w logu, nie przerwanie buildu
        code = strip(SPLIT)
        self.assertIn("EditorSceneManager.DetectCrossSceneReferences(main)", code)
        self.assertNotIn('throw new InvalidOperationException("BRAXHORN_OBSZAR_REFERENCJE', code)


if __name__ == "__main__":
    unittest.main()
