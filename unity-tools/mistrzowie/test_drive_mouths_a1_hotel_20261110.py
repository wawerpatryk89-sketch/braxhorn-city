"""Owner 10.11 17:38-17:40 ("A drogi też zrobiłeś wszędzie zaokrąglone zakręty?"): the arena A1 car-park drives and the
Hotel Sobczak drop-off lanes cross the sidewalk as street mouths - city kerb open, R 3 m kerb arcs, corner squares cut
from the foundation and built by BraxChampionsDriveMouth."""
import unittest
from pathlib import Path

from layout import (A1_PARKING_ENTRANCES, A1_DRIVE_MOUTHS, A1_MOUTH_CORNERS, HOTEL_DRIVE_STRIPS, HOTEL_DRIVE_MOUTHS,
                    HOTEL_MOUTH_CORNERS, DRIVE_MOUTHS, MOUTH_CUTS, DRIVE_MOUTH_R, BUS_STOPS, TRAM_STOPS, X_ROADS)

ED = Path(__file__).resolve().parents[2] / "unity-city/Assets/Editor/Mistrzowie"


class DriveMouths(unittest.TestCase):
    def test_every_mouth_is_open_and_cut(self):
        for m in A1_DRIVE_MOUTHS + HOTEL_DRIVE_MOUTHS:
            self.assertIn(m, DRIVE_MOUTHS)
        for r in A1_MOUTH_CORNERS + HOTEL_MOUTH_CORNERS + HOTEL_DRIVE_STRIPS:
            self.assertIn(r, MOUTH_CUTS)
        self.assertEqual(len(A1_MOUTH_CORNERS), 2 * len(A1_PARKING_ENTRANCES))
        self.assertEqual(len(HOTEL_MOUTH_CORNERS), 4)

    def test_mouths_clear_of_stops_and_junction_arcs(self):
        for pid, a, b, c, d in A1_DRIVE_MOUTHS + HOTEL_DRIVE_MOUTHS:
            mx, half = (a + c) / 2, (c - a) / 2
            for _, x, z, axis in BUS_STOPS + TRAM_STOPS:
                if axis == "x" and abs(b - z) < 18:
                    self.assertGreater(abs(mx - x), half + 20, (pid, x, z))
            for X in X_ROADS:                                   # junction arcs reach 16 m from a junction centre
                self.assertGreater(min(abs(a - X), abs(c - X)), 16 + 1, pid)

    def test_generators_build_them(self):
        arrival = (ED / "BraxChampionsArenaArrival.cs").read_text(encoding="utf-8")
        athletes = (ED / "BraxChampionsArenaAthletes.cs").read_text(encoding="utf-8")
        hotel = (ED / "BraxChampionsHotelSobczakT03.cs").read_text(encoding="utf-8")
        mouth = (ED / "BraxChampionsDriveMouth.cs").read_text(encoding="utf-8")
        self.assertIn(f"public const float MouthR={DRIVE_MOUTH_R}f;", arrival)
        self.assertIn("BraxChampionsDriveMouth.Build(parent,", arrival)
        self.assertIn("alongX=true, edge=125f, dir=-1f, u0=AthleteDriveX0, u1=AthleteDriveX1, R=MouthR, depth=7f, topEnd=.20f", athletes)
        self.assertIn('foreach(var (u0,u1,id) in new[]{(-16f,-10f,"in"),(10f,16f,"out")})', hotel)
        self.assertIn("R=3f,depth=edge+50f,topEnd=DriveY,unit=1f", hotel)
        for x0, z0, w, d in HOTEL_DRIVE_STRIPS:
            self.assertAlmostEqual(z0 - 710.0, -50.0)              # hotel-local road edge
            self.assertAlmostEqual(d, 6.8)                         # up to the hotel's own lanes at local z -43.2
            self.assertIn(round(x0 - 1030.0), (-16, 10))
        self.assertIn("public float H(float v)=>v<=R?SidewalkY*Mathf.Max(0f,v)/R:", mouth)


if __name__ == "__main__":
    unittest.main()
