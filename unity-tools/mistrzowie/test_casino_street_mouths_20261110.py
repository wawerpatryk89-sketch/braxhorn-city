"""Owner 10.11 17:15 ("Kasyno nie ma dojazdu z ulicy, już wspominałem"): both casino drives are real street mouths -
the city kerb open over the drive and 3.5 m kerb arcs (ZASADA-DROGI-NAROZNIKI-ZAOKRAGLONE), the corner squares cut
from the sidewalk and built by BraxChampionsCasinoT11Street, the drive rising from the road to the sidewalk level."""
import re
import unittest
from pathlib import Path

from layout import T11_MOUTH_R, T11_DRIVE_MOUTHS, T11_MOUTH_CORNERS, DRIVE_MOUTHS, APT_DRIVE_MOUTHS, PAVING_Y, TRAM_STOPS

ROOT = Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"
TOOLS = Path(__file__).parent
LOT = (495.0, 465.0)


class CasinoStreetMouths(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.street = (ED / "BraxChampionsCasinoT11Street.cs").read_text(encoding="utf-8")
        cls.casino = (ED / "BraxChampionsCasinoT11.cs").read_text(encoding="utf-8")

    def test_layout_matches_the_generator(self):
        self.assertIn(f"public const float MouthR={T11_MOUTH_R}f,MouthEdging=.25f,SidewalkY={str(PAVING_Y)[1:]}f;", self.street)
        self.assertIn("StreetMouths();", self.casino)
        self.assertNotIn('GradedDrive(shell,"south_street_road_connection",asphalt,', self.casino)
        # south: local x -23.5..-16.5 from road edge local z -125 (world z 340)
        self.assertIn('id="south_street_road_connection",alongX=true,edge=-125f,u0=-23.5f,u1=-16.5f', self.street)
        self.assertIn('id="left_street_road_connection",alongX=false,edge=-85f,u0=-48f,u1=-37f', self.street)
        (_, wa, wb, wc, wd), (_, sa, sb, sc, sd) = T11_DRIVE_MOUTHS
        self.assertEqual((wa, wc), (LOT[0] - 85, LOT[0] - 85))
        self.assertEqual((wb, wd), (LOT[1] - 48 - T11_MOUTH_R, LOT[1] - 37 + T11_MOUTH_R))
        self.assertEqual((sb, sd), (LOT[1] - 125, LOT[1] - 125))
        self.assertEqual((sa, sc), (LOT[0] - 23.5 - T11_MOUTH_R, LOT[0] - 16.5 + T11_MOUTH_R))
        self.assertEqual(DRIVE_MOUTHS[:len(APT_DRIVE_MOUTHS) + 2], APT_DRIVE_MOUTHS + T11_DRIVE_MOUTHS)
        self.assertEqual(len(T11_MOUTH_CORNERS), 4)

    def test_city_kerb_open_and_corners_cut(self):
        infra = (TOOLS / "build_infrastructure.py").read_text(encoding="utf-8")
        self.assertIn("for _,a,b,c,d in DRIVE_MOUTHS)", infra)
        self.assertIn(" for _,ma,mb,mc,md in DRIVE_MOUTHS:", infra)
        legacy = (TOOLS / "legacy_foundation.py").read_text(encoding="utf-8")
        self.assertIn(" if any(inside((x,z),r) for r in MOUTH_CUTS):return None", legacy)
        self.assertIn("+MOUTH_CUTS+", legacy)
        from layout import MOUTH_CUTS
        for r in T11_MOUTH_CORNERS:
            self.assertIn(r, MOUTH_CUTS)

    def test_clear_of_the_tram_zebra(self):
        # TR01's zebra crosses Z330 at x+25 with a 5 m dropped kerb: the south mouth must end before it
        for _, x, z, axis in TRAM_STOPS:
            if axis == "x" and abs(z - 330) < 1:
                zebra0 = x + 25 - 2.5
                self.assertLessEqual(T11_DRIVE_MOUTHS[1][3], zebra0 - .4)

    def test_drive_profile_and_corners(self):
        self.assertIn("if(v<=MouthR)return SidewalkY*Mathf.Max(0f,v)/MouthR;", self.street)
        self.assertIn("float Y(Vector2 q)=>SidewalkY*Mathf.Clamp01(q.y/MouthR);", self.street)
        self.assertIn('TriMesh(m.id+" corner kerb arc "+s,kerbStone,kerb,true);', self.street)
        self.assertIn('Prism(m,"T11 drive granite edging "+m.id,kerbStone,', self.street)


if __name__ == "__main__":
    unittest.main()
