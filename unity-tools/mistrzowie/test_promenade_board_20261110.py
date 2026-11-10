"""Owner 10.11 18:12 board "PROMENADA NADMORSKA - PROJEKT ROZBUDOWY" and 18:31 ("koło mariny też jakiś parking"):
recreation zones, two fountain plazas, the board's street furniture along the promenade, two public car parks."""
import collections
import math
import unittest
from pathlib import Path

from shapely.geometry import box

import layout
import promenade_layout as P

ROOT = Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"


class PromenadeBoard(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.zones_cs = (ED / "BraxChampionsPromenadeZones.cs").read_text()
        cls.park_cs = (ED / "BraxChampionsPromenadeParking.cs").read_text()
        cls.furn_cs = (ED / "BraxChampionsPromenadeFurniture.cs").read_text()
        cls.found_cs = (ED / "BraxChampionsFoundation.cs").read_text()
        cls.legacy = (Path(__file__).with_name("legacy_foundation.py")).read_text()

    def test_zones_of_the_board(self):
        kinds = collections.Counter(z["kind"] for z in P.ZONES)
        self.assertEqual(kinds, {"plaza": 2, "relax": 1, "gym": 1, "playground": 1})
        shapes = [box(a, c, a + w, c + d) for a, c, w, d in P.ZONE_RECTS + P.ZONE_PATHS]
        for i, s in enumerate(shapes):
            self.assertTrue(all(s.intersection(t).area == 0 for t in shapes[i + 1:]))
            self.assertGreaterEqual(s.bounds[1], P.Z1 - 1e-6)
        for z, (px, pz, pw, pd) in zip(P.ZONES, P.ZONE_PATHS):
            self.assertAlmostEqual(px + pw / 2, z["x"])
            self.assertAlmostEqual(pz + pd, z["z"] - z["d"] / 2)

    def test_board_furniture_counts(self):
        line = collections.Counter(k for k, _, _ in P.LINE_FURNITURE)
        palm = collections.Counter(k for k, _, _ in P.FURNITURE)
        self.assertEqual(line["bike_rack"] * 5, 25)                       # 20-30 bike stands (5 hoops per rack)
        self.assertTrue(5 <= line["bike_repair"] <= 8)
        self.assertTrue(8 <= line["water_fountain"] <= 12)
        self.assertTrue(4 <= line["bike_rack"] <= 6)                      # e-bike chargers: one per rack
        self.assertEqual(line["binoculars"], 6)
        zone_benches = 4 * 2 + 2 + 2 + 3                                   # plazas, relax deck, gym, playground
        zone_lamps = 4 * 2 + 4 + 2 + 4
        self.assertIn("foreach(float a in new[]{45f,135f,225f,315f})", self.zones_cs)
        self.assertTrue(90 <= palm["bench"] + line["bench"] + zone_benches <= 120)
        self.assertTrue(110 <= palm["lamp"] + line["lamp"] + zone_lamps <= 140)
        for k, x, z in P.LINE_FURNITURE:
            if z == P.LINE_Z:
                for a, _, w, _ in P.ZONE_PATHS + P.PARKING_PATHS:
                    self.assertFalse(a - 1 <= x <= a + w + 1, (k, x))

    def test_planters_flowers_are_real_and_culled(self):
        self.assertIn('Flower(models,flower,new Vector3(x-.08f,soil,z-.17f),1.62f);', self.furn_cs)
        self.assertIn("new LOD(.015f,", self.furn_cs)
        self.assertIn('"flower_gazania"', self.zones_cs)

    def test_zone_builders(self):
        self.assertIn('Optional("promenada: strefa relaksu, place z fontannami, siłownia plenerowa, plac zabaw",'
                      '()=>BraxChampionsPromenadeZones.Build(root,d.zones));', self.found_cs)
        for s in ("static void Fountain(", "static void Pergola(", "static void SlideTower(", "static void SwingSet(",
                  "static void SpringRider(", "static void Sandpit(", "static void Fence(", "static void Gym("):
            self.assertIn(s, self.zones_cs)
        self.assertIn('"piaskownica piasek | "', self.zones_cs)            # takes footprints (BraxChampionsSandSteps)
        self.assertIn('"coast_sand_02"', self.zones_cs)
        # no brown: granite_tile_02 / 03 read terracotta / warm in the renders
        self.assertNotIn('"granite_tile_02"', self.zones_cs)
        self.assertNotIn('"granite_tile_03"', self.zones_cs)
        self.assertIn("plazaStone=walk;", self.zones_cs)

    def test_car_parks(self):
        self.assertEqual([p["id"] for p in P.PARKINGS], ["PARKING_PLAZA", "PARKING_MARINA"])
        marina = P.PARKINGS[1]
        self.assertTrue(700 <= marina["x0"] and marina["x1"] <= 1150)      # by the marina
        for p, m in zip(P.PARKINGS, layout.PARKING_DRIVE_MOUTHS):
            self.assertEqual(m, (p["id"], p["driveX0"] - 3.0, -80.0, p["driveX1"] + 3.0, -80.0))
            self.assertIn(m, layout.DRIVE_MOUTHS)
            self.assertIn((p["driveX0"], -87.0, p["driveX1"] - p["driveX0"], 7.0), layout.MOUTH_CUTS)
            # bays: 2 rows of 2.5 m between the border, minus the drive gap
            inner = p["x1"] - p["x0"] - .5
            n = math.floor((inner - .12) / 2.5)
            self.assertGreaterEqual(2 * n - 4, 30)
            # the drive avoids the zebra ramps of road Z-70 (x = X +- 22 for X in 400, 800)
            for X in (400, 800):
                for c in (X - 22, X + 22):
                    self.assertTrue(p["driveX1"] + 3 < c - 2.5 or p["driveX0"] - 3 > c + 2.5)
        self.assertIn(" if any(inside((x,z),r) for r in PARKING_RECTS+PARKING_DRIVES+PARKING_PATHS):return None", self.legacy)
        self.assertIn("rects+=ZONE_RECTS+ZONE_PATHS+PARKING_RECTS+PARKING_DRIVES+PARKING_PATHS", self.legacy)
        self.assertIn("BraxChampionsDriveMouth.Build(g,new BraxChampionsDriveMouth.Spec{", self.park_cs)
        self.assertIn('()=>BraxChampionsPromenadeParking.Build(root,d.parkings));', self.found_cs)
        self.assertIn("static void Row(", self.park_cs)                     # lines cut into the asphalt, not on it
        self.assertTrue(P.validate_parkings())


if __name__ == "__main__":
    unittest.main()
