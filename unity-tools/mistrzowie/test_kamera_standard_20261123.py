"""Camera closer to the character (ZASADY 10.8, owner 23.11 15:48): the numbers come from Assets/Claude/BraxKameraStandard.cs
(branch standard) and BraxGymPlayer reads them - on foot 3.6 (was 4.2), behind the car 5.5-7.5 by speed (was 8.4)."""
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
STD = (ROOT / "unity-city/Assets/Claude/BraxKameraStandard.cs").read_text(encoding="utf-8")
GRACZ = (ROOT / "unity-city/Assets/BraxGymPlayer.cs").read_text(encoding="utf-8")


def bez_komentarzy(t):
    return "\n".join(l.split("//")[0] for l in t.splitlines())


class Kamera(unittest.TestCase):
    def test_standard_numbers(self):
        self.assertIn("public const float Pieszo = 3.6f;", STD)
        self.assertIn("public const float AutoMin = 5.5f;", STD)
        self.assertIn("public const float AutoMaks = 7.5f;", STD)

    def test_player_reads_the_standard(self):
        g = bez_komentarzy(GRACZ)
        self.assertEqual(g.count("BraxKameraStandard.Pieszo"), 2)
        self.assertIn("float dist=championsCar?BraxKameraStandard.Auto(championsCar.Speed):", g)
        # rule K1 of main's checker: no distance written as a number
        self.assertIsNone(re.search(r"\bdistance\s*=\s*\d*\.?\d+f?\s*[;,]", g))
        self.assertNotIn("championsCar?8.4f", g)


if __name__ == "__main__":
    unittest.main()
