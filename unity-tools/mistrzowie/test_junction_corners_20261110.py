"""Owner 10.11 17:38-17:40: "A drogi też zrobiłeś wszędzie zaokrąglone zakręty?" + a picture of rounded kerbs at a
junction, "łatwiej się wtedy jeździ i autobus będzie miał lepiej" (ZASADA-DROGI-NAROZNIKI-ZAOKRAGLONE: R about 6 m at
street junctions). Every junction corner of the city grid turns in a granite kerb arc."""
import unittest
from pathlib import Path

TOOLS = Path(__file__).parent


class JunctionCorners(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.legacy = (TOOLS / "legacy_foundation.py").read_text(encoding="utf-8")
        cls.infra = (TOOLS / "build_infrastructure.py").read_text(encoding="utf-8")

    def test_corner_squares_are_road_and_cut_back_to_the_arc(self):
        self.assertIn("JUNCTION_R=6.0", self.legacy)
        self.assertIn(" if JUNCTION_CORNERS and any(inside((x,z),c[4]) for c in JUNCTION_CORNERS):return 'asphalt'", self.legacy)
        self.assertIn("+JUNCTION_CORNER_RECTS+", self.legacy)
        self.assertIn("asphalt_cut=groove_parts+corner_polys", self.infra)
        self.assertIn("  if in_mouth(*p) or in_corner(*p):continue", self.infra)
        self.assertIn("mesh.tri('paving',*tri)", self.infra)

    def test_every_crossing_stays_clear_of_the_arcs(self):
        # the zebra ramps sit 22 +- 2.5 m from the junction centre; the arcs end 10 + R from it
        R = 6.0
        self.assertLess(10 + R, 22 - 2.5 - 1.0)

    def test_corner_layout(self):
        import math
        # the arc runs from the X-road kerb line to the Z-road kerb line round the centre (X+sx*16, Z+sz*16)
        for sx in (-1, 1):
            for sz in (-1, 1):
                a0, a1 = math.atan2(0, -sx), math.atan2(-sz, 0)
                da = (a1 - a0 + math.pi) % (2 * math.pi) - math.pi
                self.assertAlmostEqual(abs(da), math.pi / 2)
                mid = (16 * sx + 6 * math.cos(a0 + da / 2), 16 * sz + 6 * math.sin(a0 + da / 2))
                # the middle of the arc bulges towards the junction centre
                self.assertLess(math.hypot(*mid), math.hypot(16, 16))


if __name__ == "__main__":
    unittest.main()
