"""Every street of the city lined with thick palms (owner 24.11 00:50: "w każdej drodze, przy drogach, jak są gwiazdy, te
grube palmy ... tylko i wyłącznie grube palmy"; his answers: every street of the city; off the streets - the aquapark, the
hotels, the promenade - mixed as on 10.11 08:48).

legacy_foundation.py: both sides of every XROAD and ZROAD, the owner's thick-trunk palma (variant 0) on the grass 4 m past the
sidewalk (21 m off the axis), every 20 m, clear of the junctions; not on the towers' forecourts nor the arena's front lawn
(their own generators plant there); no tree and no thin palm along a street. The far palms of a long row drop sooner."""
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
TOOLS = ROOT / "unity-tools/mistrzowie"
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"
SRC = (TOOLS / "legacy_foundation.py").read_text(encoding="utf-8")
FOUND = (ED / "BraxChampionsFoundation.cs").read_text(encoding="utf-8")


class UlicePalmy(unittest.TestCase):
    def test_every_street_both_sides_thick_only(self):
        self.assertIn("STREET_PALM_STEP,STREET_PALM_OFF,STREET_PALM_JUNCTION=20,21,25", SRC)
        self.assertIn(" plant(x,z,'palm',0,'street_grass')", SRC)
        self.assertIn("for x in XROAD:\n for side in (-1,1):\n  for z in range(-70+STREET_PALM_JUNCTION,936,STREET_PALM_STEP):", SRC)
        self.assertIn("   street_palm(x+side*STREET_PALM_OFF,z)", SRC)
        self.assertIn("for zr in ZROAD:\n for side in (-1,1):\n  for x in range(70+STREET_PALM_JUNCTION,1131,STREET_PALM_STEP):", SRC)
        self.assertIn("   street_palm(x,zr+side*STREET_PALM_OFF)", SRC)
        # the old mixed rows (trees west of x 800, the variant alternating) are gone from the streets
        self.assertNotIn("plant(x+side*21,z,'palm' if x>=800 else 'tree',len(plants)%2,'street_grass')", SRC)
        street = [l for l in SRC.splitlines() if "'street_grass'" in l and "plant(" in l]
        self.assertEqual(street, [" plant(x,z,'palm',0,'street_grass')"])

    def test_on_the_grass_past_the_sidewalk(self):
        # the asphalt 10 m off the axis, the sidewalk to 17 m: the trunk 21 m off it, 4 m into the grass (root disk 2 m)
        off = int(re.search(r"STREET_PALM_STEP,STREET_PALM_OFF,STREET_PALM_JUNCTION=(\d+),(\d+),(\d+)", SRC).group(2))
        self.assertGreaterEqual(off - 17, 2 + 1)
        self.assertIn("r=1.5 if band in DESIGNED_BEDS else 2.0", SRC)                  # plant() keeps its grass root disk
        xroad = [int(v) for v in re.search(r"XROAD=\[([^\]]+)\]", SRC).group(1).split(",")]
        zroad = [int(v) for v in re.search(r"ZROAD=\[([^\]]+)\]", SRC).group(1).split(",")]
        self.assertEqual(xroad, [70, 400, 800, 1130])
        self.assertEqual(zroad, [-70, 70, 330, 650, 935])

    def test_not_on_the_forecourts_nor_the_arena_lawn(self):
        self.assertIn("if any(inside((x,z),r) for r in STREET_PALM_SKIP):return", SRC)
        self.assertIn("assert len(STREET_PALM_SKIP)==6,'three north towers, two south towers, the arena'", SRC)
        self.assertIn("633.022", SRC)                                                    # road Z650's sidewalk edge (the towers' forecourts)
        self.assertIn("347.012", SRC)                                                    # road Z330's
        self.assertIn("STREET_PALM_SKIP+=[(600-185,655,370,70)]", SRC)                   # the arena's own street row
        # the skip list exists before the rows are planted
        self.assertLess(SRC.index("STREET_PALM_SKIP=["), SRC.index("for x in XROAD:\n for side in (-1,1):\n  for z in range(-70+STREET_PALM_JUNCTION"))

    def test_mixed_off_the_streets(self):
        # the promenade and the plots keep their mixed palms (owner 10.11 08:48 still holds off the streets)
        self.assertIn("for x in range(65,1146,25):plant(x,-115,'palm',len(plants)%2,'promenade_soil')", SRC)

    def test_far_palms_of_a_long_row_drop_sooner(self):
        self.assertIn('BraxMiastoAssety.LodyZWezlow(o,p.band=="street_grass"?new[]{.25f,.08f,.018f}:new[]{.25f,.08f,.012f});report.palms++;', FOUND)
        self.assertIn("o=BraxChampionsModel.Postaw(vegetation,palms[p.variant%palms.Length],p.id+\" Palma \"+p.band,at,p.yaw,1f,false,true);", FOUND)


if __name__ == "__main__":
    unittest.main()
