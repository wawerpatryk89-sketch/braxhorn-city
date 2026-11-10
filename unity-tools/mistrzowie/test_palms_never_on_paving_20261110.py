"""Owner 10.11 12:39 (APK 37): "Palma na chodniku", "palma w kostce rośnie". Gates for every tree placement source."""
from pathlib import Path
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]
TOOLS = ROOT / "unity-tools/mistrzowie"
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"


class PalmsNeverOnPaving(unittest.TestCase):
    def test_foundation_root_disk_and_generator_frontage(self):
        src = (TOOLS / "legacy_foundation.py").read_text(encoding="utf-8")
        self.assertIn("r=1.5 if band in DESIGNED_BEDS else 2.0", src)
        self.assertIn("for k in range(8)]", src)                         # 8 directions, not 4
        self.assertIn("if any(inside((x,z),r) for r in GENERATOR_FRONTAGE):return", src)
        self.assertIn("GENERATOR_FRONTAGE=[(1030-16-2.5,710-43.2-2.5,32+5,13.2+5)]", src)   # hotel drop-off + fountains
        # the frontage list is defined before the first plant() call
        self.assertLess(src.index("GENERATOR_FRONTAGE=["), src.index("for x,z in PALM_BEDS:plant("))

    def test_casino_palms_stand_in_their_planters(self):
        casino = (ED / "BraxChampionsCasinoT11.cs").read_text(encoding="utf-8")
        palms = re.search(r"foreach\(var at in new\[\]\{(.*?)\}\)\s*\{\s*var location=shell", casino, re.S).group(1)
        spots = [(float(a), float(b)) for a, b in re.findall(r"Vector2\((-?[\d.]+)f?,(-?[\d.]+)\)", palms)]
        planters = {(-43, -32), (37.6, -32), (-43, 33), (43, 33), (-26, -33), (26, -33)}
        self.assertEqual(set(spots), planters)
        self.assertIn("new Vector3(at.x,.818f,at.y)*K", casino)          # on the lawn bed, not the stone apron (.22)
        self.assertIn('"Planter lawn bed"', casino)


if __name__ == "__main__":
    unittest.main()
