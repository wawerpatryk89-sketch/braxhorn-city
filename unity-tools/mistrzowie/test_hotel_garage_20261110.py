"""Owner 10.11 15:25-15:26 (Hotel Sobczak: "coś tam mówiłeś o parkingu, że zostawisz na później ... do teraz parkingu
nie ma", "Nie ma nic na później"): the B1 garage of T03, 60 bays, with a ramp off the drop-off lane and a stair + lift
pavilion on the plaza."""
import re
import unittest
from pathlib import Path

from layout import HOTEL_CENTRE, HOTEL_RAMP_WALLS, HOTEL_GARAGE_PORTAL_Z, HOTEL_CORE_OUTER, HOTEL_GARAGE_CUTS

ROOT = Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"
TOOLS = ROOT / "unity-tools/mistrzowie"


def const(src, name):
    return float(re.search(r"\b" + name + r"=(-?[\d.]+)f", src).group(1))


class HotelGarage(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.h = (ED / "BraxChampionsHotelSobczakT03.cs").read_text(encoding="utf-8")

    def test_layout_matches_the_generator(self):
        self.assertEqual((const(self.h, "CentreX"), const(self.h, "CentreZ")), HOTEL_CENTRE)
        self.assertEqual((const(self.h, "RampWall0"), const(self.h, "RampWall1")), HOTEL_RAMP_WALLS)
        self.assertAlmostEqual(const(self.h, "GarageZ0") - const(self.h, "GarageWallT"), HOTEL_GARAGE_PORTAL_Z)
        self.assertEqual(tuple(const(self.h, n) for n in ("CoreOuterX0", "CoreOuterX1", "CoreOuterZ0", "CoreOuterZ1")), HOTEL_CORE_OUTER)
        legacy = (TOOLS / "legacy_foundation.py").read_text(encoding="utf-8")
        self.assertIn("if any(inside((x,z),r) for r in HOTEL_GARAGE_CUTS):return None", legacy)
        self.assertIn("+APT_DRIVE_CUTS+HOTEL_GARAGE_CUTS+", legacy)
        self.assertEqual(len(HOTEL_GARAGE_CUTS), 3)

    def test_sixty_real_bays(self):
        self.assertIn("public const int BaysPerRow=20,GarageBays=60;", self.h)
        self.assertIn("int garageBays=Garage();", self.h)
        self.assertIn('" garageBays="+garageBays+', self.h)
        self.assertEqual(const(self.h, "BayW"), 2.5)
        self.assertEqual(const(self.h, "BayD"), 5.0)
        self.assertEqual(const(self.h, "AisleW"), 6.0)
        # lines inlaid (flush with the asphalt), wheel stops, white walls with black column bases, LED, lights
        self.assertIn("if(i>0)Box(M.whitePaint,x0-.05f,x0+.05f,f0,f1,zb0,zb1,true);", self.h)
        self.assertIn('"T03 garage light"', self.h)
        self.assertIn('garageWhite=BraxChampionsFoundation.SourceMaterial("t03_garage_white_paint","white_stucco_02"', self.h)
        # clear height under the downstand beams >= 2.30 m
        floor, ceil, drop = const(self.h, "GarageFloorY"), const(self.h, "GarageCeilY"), const(self.h, "BeamDrop")
        self.assertGreaterEqual(ceil - drop - floor, 2.30)

    def test_ramp_profile_is_drivable(self):
        # 8 % / 13 % / 8 %: the steepest part under 15 %, the ends at the drive and the floor
        top, low = .20 - .24, -3.60 + .24
        self.assertLess((top - low) / 25.5, .15)
        self.assertIn("if(z<=-28.5f)return DriveY;", self.h)
        self.assertIn("if(z<=RampBottomZ)return low-.08f*z;", self.h)
        self.assertIn("W.Rampa(new Vector3((RampRoad0+RampRoad1)*.5f,y0,z)", self.h)
        # the drop-off lane runs on to the ramp, the plaza kerb is open at its mouth, parapets on the plaza
        self.assertIn("Box(M.asphalt,16f,RampWall1,SiteY,DriveY,-34f,-PlotHalf,true);", self.h)
        self.assertIn("Box(M.kerb,1.8f,RampWall0,DriveY-.05f,PlazaY+.005f,-PlotHalf,-PlotHalf+.18f,true);", self.h)
        self.assertIn("PlazaY+1.05f", self.h)

    def test_people_get_out_by_stair_and_lift(self):
        self.assertIn('BraxChampionsInteriorDoor.Single("T03_GARAGE_STAIR_DOOR"', self.h)
        self.assertIn('BraxChampionsInteriorDoor.Single("T03_GARAGE_PAVILION_DOOR"', self.h)
        self.assertIn('c.pietraNazwy=new[]{"B1","PLAC"};', self.h)
        self.assertIn("GarageLiftConfigure();", self.h)
        # the lift is configured after the root is scaled (world stop heights)
        self.assertLess(self.h.index("root.localScale=Vector3.one*K;"), self.h.index("        GarageLiftConfigure();"))
        self.assertIn('BraxChampionsLetters3D.Text(letters,M.steel,M.stoneDark,"PARKING",', self.h)
        # pavilion glass: black round the door, clear only on the door leaf
        self.assertIn('"garage pavilion door",false,px1-.05f,doorA,doorB,deck1,2.18f,M.clearGlass,M.steel);', self.h)


if __name__ == "__main__":
    unittest.main()
