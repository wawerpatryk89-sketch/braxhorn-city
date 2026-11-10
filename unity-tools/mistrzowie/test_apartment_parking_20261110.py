"""Owner 10.11 15:25 ("Po trzecie, apartamentowce mają parkingi zrobione?") - chose "Parking naziemny" (15:31).
Every apartment plot T04-T07 has a residents' car park and a drive to its road; at the road the kerbs turn in arcs
(ZASADA-DROGI-NAROZNIKI-ZAOKRAGLONE 10.11) and the city kerb is open across the mouth."""
import re
import unittest
from pathlib import Path

from layout import (APT_PARKING, APT_PARK_KERB, APT_AISLE_X0, APT_AISLE_X1, APT_DRIVE_R, APT_DRIVE_CUTS,
                    APT_DRIVE_MOUTHS, BUS_STOPS, TRAM_STOPS, apt_world)

ROOT = Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"
TOOLS = ROOT / "unity-tools/mistrzowie"


class ApartmentParking(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.apt = (ED / "BraxChampionsApartments.cs").read_text(encoding="utf-8")

    def test_csharp_matches_the_layout(self):
        f = lambda v: (str(v)[1:] if 0 < v < 1 else str(v)) + "f"
        self.assertIn(f"public const float ParkKerb={f(APT_PARK_KERB)}, AisleX0={f(APT_AISLE_X0)}, AisleX1={f(APT_AISLE_X1)},", self.apt)
        self.assertIn(f"public const float DriveR={APT_DRIVE_R}f,", self.apt)
        for pid, p in APT_PARKING.items():
            row = re.search(r'new Plot\{id="' + pid + r'".*?\}', self.apt).group(0)
            self.assertIn(f"x={p['x']}", row)
            self.assertIn(f"z={p['z']}", row)
            self.assertIn(f"yaw={p['yaw']}f", row)
            self.assertIn(f"walk={p['walk']}f", row)
            self.assertIn(f"park={p['side']}" + "}", row)

    def test_real_car_park(self):
        self.assertIn("public const int Bays=20;", self.apt)
        self.assertIn("BayW=2.5f", self.apt)
        self.assertIn("int bays=Parking();", self.apt)
        # lines inlaid in the asphalt (flush, between the bay strips), never painted on top
        self.assertIn("if(i>0)PBox(parkPaint,AisleX1,BayX1,y0,top,z0-.05f,z0+.05f,true);", self.apt)
        # the lawn is cut round the car park, nothing under the asphalt
        self.assertNotIn("Box(grass,-PlotHalf,PlotHalf,PlotBaseY,SiteY,-PlotHalf,PlotHalf,true);", self.apt)
        self.assertEqual(self.apt.count("PBox(grass,"), 4)
        self.assertIn('W.Lampa("apt car park LED"', self.apt)
        self.assertIn('BraxChampionsLetters3D.Text(nameLetters,nameFace,black,"P",', self.apt)

    def test_rounded_mouth_and_open_city_kerb(self):
        self.assertIn("Vector2 Arc(float r,float t)", self.apt)
        self.assertIn("W.Rampa(", self.apt)
        infra = (TOOLS / "build_infrastructure.py").read_text(encoding="utf-8")
        self.assertIn("if in_mouth(*p):continue", infra)
        legacy = (TOOLS / "legacy_foundation.py").read_text(encoding="utf-8")
        self.assertIn("if any(inside((x,z),r) for r in APT_DRIVE_CUTS):return None", legacy)
        self.assertIn("+A1_PARKING_ENTRANCES+APT_DRIVE_CUTS+", legacy)
        self.assertEqual(len(APT_DRIVE_CUTS), 8)
        self.assertEqual(len(APT_DRIVE_MOUTHS), 4)

    def test_mouths_clear_of_stops_and_crossings(self):
        for pid, a, b, c, d in APT_DRIVE_MOUTHS:
            mx, mz = (a + c) / 2, (b + d) / 2
            half = max(c - a, d - b) / 2
            for _, x, z, axis in BUS_STOPS + TRAM_STOPS:
                # a stop occupies about +-25 m along its road (shelters, bus bays, tram platform 42 m)
                if axis == "x" and abs(mz - z) < 18 and abs(mx - x) < half + 25:
                    self.fail(f"{pid} mouth next to stop at {x},{z}")
                if axis == "z" and abs(mx - x) < 18 and abs(mz - z) < half + 25:
                    self.fail(f"{pid} mouth next to stop at {x},{z}")

    def test_drive_runs_to_the_road_edge(self):
        for pid, p in APT_PARKING.items():
            rx, rz = apt_world(p, (APT_AISLE_X0 + APT_AISLE_X1) / 2, p["walk"] - 6.8)
            # the road edge is 10 m from a road centre line
            from layout import X_ROADS, Z_ROADS
            self.assertTrue(any(abs(abs(rx - x) - 10) < 1e-6 for x in X_ROADS) or any(abs(abs(rz - z) - 10) < 1e-6 for z in Z_ROADS), pid)


if __name__ == "__main__":
    unittest.main()
