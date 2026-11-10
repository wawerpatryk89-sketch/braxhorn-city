"""Owner 10.11 15:59 ("Piasek na plaży ma być zapadający się pod nogami, zostawiając ślady ... globalnie na każdą
plażę") and 16:39 ("Znikają po ok. 1 min"): BraxChampionsSandSteps (docs/zasady/PIASEK_ZAPADANIE_SLADY_20261110.md)."""
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / "unity-city/Assets/Mistrzowie/BraxChampionsSandSteps.cs"


class SandSteps(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.s = SRC.read_text(encoding="utf-8")

    def test_attached_to_the_player_without_touching_it(self):
        self.assertIn("[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]", self.s)
        self.assertIn("Object.FindAnyObjectByType<BraxGymPlayer>()", self.s)
        self.assertIn("if (visual == transform) visual = null;", self.s)        # never the body with the controller
        self.assertTrue((SRC.parent / (SRC.name + ".meta")).exists())

    def test_sink_and_a_minute_of_prints(self):
        self.assertIn("public const float Sink = .04f, Life = 60f, Settle = 10f;", self.s)
        self.assertIn("public const int MaxPrints = 160;", self.s)
        self.assertIn('return s.Contains("sand") || s.Contains("piasek") || s.Contains("piask");', self.s)

    def test_real_print_never_lying_on_the_sand(self):
        # sole 10 mm over the sand, crest 24 mm, the ridge's outer foot 10 mm UNDER it: no surface 1-5 mm apart
        heights = [float(h) for h in re.findall(r"At\(outline\[i\], [\d.]+f, (-?[\d.]+)f\)", self.s)]
        self.assertEqual(heights, [.010, .024, -.010])
        for h in heights:
            self.assertFalse(.001 <= abs(h) <= .005)
        self.assertNotIn("_Transparent", self.s)
        self.assertIn("p.t.position = p.at - p.up * (.034f * t * p.t.lossyScale.y);", self.s)   # settles under the sand

    def test_rule_written_down(self):
        rule = (ROOT / "docs/zasady/PIASEK_ZAPADANIE_SLADY_20261110.md").read_text(encoding="utf-8")
        self.assertIn("globalnie na każdą plażę", rule)
        self.assertIn("Znikają po ok. 1 min", rule)


if __name__ == "__main__":
    unittest.main()
