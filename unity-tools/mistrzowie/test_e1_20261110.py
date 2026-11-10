"""Owner 10.11 19:18-20:12: E1 "CENTRUM ROZRYWKI" from the board (docs/zlecenia/plansze/MISTRZOWIE_E1_CENTRUM_ROZRYWKI_20261110.png)
1:1 by the promenade - 120 x 50 x 15 m, 24 bowling lanes, 18 billiard tables, games/VR, bar/lounge, B1 garage ("parking
podziemny najlepiej"), taxi lay-by in front ("na taksówki przed E1")."""
import json
import math
import re
import unittest
from pathlib import Path

import layout as L

ROOT = Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"


def const(src, name):
    return float(re.search(r"\b" + name + r"=(-?[\d.]+)f", src).group(1))


class E1(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.cs = (ED / "BraxChampionsE1.cs").read_text()
        cls.buildings = (ED / "BraxChampionsBuildings.cs").read_text()
        cls.legacy = (Path(__file__).with_name("legacy_foundation.py")).read_text()

    def test_board_numbers(self):
        e1 = next(b for b in json.loads(Path(__file__).with_name("plan_inventory.json").read_text())["buildings"] if b["id"] == "E1")
        self.assertEqual((const(self.cs, "CentreX"), const(self.cs, "CentreZ")), (e1["x"], e1["z"]))
        self.assertEqual((2 * const(self.cs, "HW"), 2 * const(self.cs, "HD")), (e1["w"], e1["d"]))
        self.assertEqual(const(self.cs, "PortalTop"), e1["height"])
        self.assertIn("public const int Lanes=24,Tables=18;", self.cs)
        # 24 lanes in 12 pairs of real lanes (1.0668 m + 2 gutters) fit the 60 m bowling zone west of the hall
        pitch, x0 = const(self.cs, "PairPitch"), const(self.cs, "LaneX0")
        self.assertGreaterEqual(pitch, 2 * (1.067 + 2 * .235) + .08)
        self.assertLessEqual(x0 + 12 * pitch, -15.0)
        self.assertGreaterEqual(x0, -59.7)
        self.assertEqual(const(self.cs, "LaneLen"), 18.29)                    # foul line to the head pin
        for s in ("static void Table(", "static void Bowling()", "static void Billiards()", "static void BackRooms()",
                  "static void Garage()", "static void Lift()", "static void TaxiBay()", "static void Ramp()", "static void Mural()"):
            self.assertIn(s, self.cs)
        for t in ('"E1"', '"CENTRUM ROZRYWKI"', '("KRĘGLE",', '("BILARD",', '("GRY",', '("BAR",', '("LOUNGE",', '"TAXI"'):
            self.assertIn(t, self.cs)

    def test_site_and_cuts_match_the_layout(self):
        cx, cz = const(self.cs, "CentreX"), const(self.cs, "CentreZ")
        x0, x1, z0, z1 = (const(self.cs, n) for n in ("SiteX0", "SiteX1", "SiteZ0", "SiteZ1"))
        self.assertEqual(L.E1_SITE, (cx + x0, cz + z0, x1 - x0, z1 - z0))
        self.assertEqual(cz + z0, -53.0)                                         # Z-70 north sidewalk inner edge
        self.assertEqual(cz + z1, 53.0)                                          # Z70 south sidewalk inner edge
        # taxi lay-by: road edge z -60 (local -47), bay 3 m + kerb .18, straight x BayX0..BayX1, S-tapers 7.94 m (R 6)
        self.assertEqual(cz + const(self.cs, "RoadEdgeS"), -60.0)
        th = math.acos(1 - const(self.cs, "BayDepth") / (2 * const(self.cs, "BayR")))
        self.assertAlmostEqual(2 * const(self.cs, "BayR") * math.sin(th), const(self.cs, "BayTaper"), delta=.01)
        ax0 = cx + const(self.cs, "BayX0") - const(self.cs, "BayTaper"); ax1 = cx + const(self.cs, "BayX1") + const(self.cs, "BayTaper")
        self.assertEqual(L.E1_TAXI_MOUTH, ("E1_taxi", ax0, -60.0, ax1, -60.0))
        self.assertAlmostEqual(L.E1_TAXI_CUT[3], const(self.cs, "BayDepth") + const(self.cs, "KerbW"))
        self.assertIn(L.E1_TAXI_MOUTH, L.DRIVE_MOUTHS)
        # the bus stop U14 (x 470 on road Z-70, bay markings x 454..486) stays clear of the lay-by
        self.assertGreater(ax0, 486 + 10)
        # garage drive from road Z70 (south edge z 60 = local 73) across its 7 m sidewalk, R 3 m mouth
        self.assertEqual(cz + const(self.cs, "RoadEdgeN"), 60.0)
        rx0, rx1 = cx + const(self.cs, "RampX0"), cx + const(self.cs, "RampX1")
        self.assertEqual(L.E1_GARAGE_DRIVE, (rx0, 53.0, rx1 - rx0, 7.0))
        self.assertEqual(L.E1_GARAGE_MOUTH, ("E1_garage", rx0 - 3.0, 60.0, rx1 + 3.0, 60.0))
        for r in [L.E1_SITE, L.E1_TAXI_CUT, L.E1_GARAGE_DRIVE] + L.E1_GARAGE_CORNERS:
            self.assertIn(r, L.MOUTH_CUTS)
        # the ramp stays between the back pilasters at x 40 and 50 (local)
        self.assertGreater(const(self.cs, "RampX0") - .3, 40.3)
        self.assertLess(const(self.cs, "RampX1") + .3, 49.7)
        # ramp: 6 % / <= 15 % / 6 % from grade at z 58 to B1 at z 27
        b1, y0 = const(self.cs, "B1"), const(self.cs, "Y0")
        self.assertLessEqual(((y0 - .24) - (b1 + .24)) / (54 - 31), .15)

    def test_lift_stair_and_doors(self):
        self.assertIn("var w=cab.gameObject.AddComponent<BraxWinda>();", self.cs)
        self.assertIn('w.pietraNazwy=new[]{"B1","HOL"};', self.cs)
        self.assertIn("var d=group.gameObject.AddComponent<BraxCityDoor>();", self.cs)
        self.assertIn("GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);", self.cs)   # moving parts never static-batched
        risers = int(re.search(r"public const int Risers=(\d+);", self.cs).group(1))
        rise = (const(self.cs, "FFL") - const(self.cs, "B1")) / risers
        self.assertTrue(.15 <= rise <= .18)
        self.assertGreaterEqual(const(self.cs, "StairRun"), .28)

    def test_no_brown_no_beige_and_glyphs(self):
        # warm maps are tinted to neutral (garage_floor, concrete_floor_02, granite forecourt, pale maple lanes)
        for slug, tint in (("garage_floor", "1.0f,1.14f,1.43f"), ("concrete_floor_02", "1.0f,1.09f,1.32f"),
                           ("granite_tile_03", ".96f,1.08f,1.26f"), ("laminate_floor_02", "1.32f,1.45f,1.62f")):
            self.assertIn('"' + slug + '",', self.cs)
            self.assertIn(tint, self.cs)
        self.assertNotIn("·", re.sub(r"//.*", "", self.cs))                    # the letter kit has no middle dot
        self.assertNotIn("granite_tile_02", self.cs)

    def test_built_with_the_buildings(self):
        self.assertIn('BraxChampionsFoundation.Optional("E1 centrum rozrywki",()=>', self.buildings)
        self.assertIn("BraxChampionsE1.Generate(Out);", self.buildings)
        self.assertIn(" if any(inside((x,z),r) for r in MOUTH_CUTS):return None", self.legacy)
        self.assertTrue((ED / "BraxChampionsE1.cs.meta").exists())


if __name__ == "__main__":
    unittest.main()
