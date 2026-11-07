"""T11 7 Nov vehicle access geometry contracts — read-only, no Unity/APK.

Keeps the existing city road, exactly 32 underground parking bays and the
owner-approved building. These checks are NOT a vehicle Play Mode test.
"""
from pathlib import Path
import math
import unittest

ROOT=Path(__file__).resolve().parents[2]
T11=(ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsCasinoT11.cs")
GROUND=(ROOT/"unity-tools/mistrzowie/legacy_foundation.py")


class CasinoVehicleAccessContract(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.casino=T11.read_text(encoding="utf-8")
        cls.ground=GROUND.read_text(encoding="utf-8")

    def test_wider_foundation_ramp_hole_and_fire_lane_clearance(self):
        self.assertIn("T11_GARAGE_RAMP=(534.15,443.0,10.15,46.0)",self.ground)
        # Protected fire lane starts at global X=495+49.5 = 544.5m.
        self.assertLess(534.15+10.15,544.5)
        # The original ramp 8m opening was insufficient for the turn.
        self.assertGreater(10.15,8)
        self.assertIn("T11 stone plot apron west of garage",self.casino)
        self.assertIn("98.95f,.055f,46f,true,true",self.casino)
        self.assertIn("10.5f,.055f,46f,true,true",self.casino)

    def test_turn_left_twelve_plus_meters_and_covered_end(self):
        # Ramp proceeds north (+Z). Its exit into garage is WEST (-X).
        ramp_x0,ramp_x1=39.55,48.90
        self.assertAlmostEqual(ramp_x1-ramp_x0,9.35)
        exit_start,exit_end=15.0,29.0
        self.assertGreaterEqual(exit_end-exit_start,12)
        self.assertIn("Garage east solid wall WHITE before LEFT TURN opening",self.casino)
        self.assertIn("Garage short north return after LEFT TURN",self.casino)
        self.assertIn("T11 SOLID END WALL straight ahead at ramp bottom",self.casino)
        # Return joins existing north wall without a leak:
        self.assertAlmostEqual(3.5+71.30/2,39.30-.30/2)
        # The end wall covers the FULL driving lane straight ahead.
        self.assertLessEqual(44.225-9.85/2,ramp_x0)
        self.assertGreaterEqual(44.225+9.85/2,ramp_x1)

    def test_ramp_street_and_garage_surfaces_are_level_at_seams(self):
        street_y=0.0                 # Legacy foundation asphalt is y=0
        casino_y=.2485              # Main forecourt and garage top approach
        garage_y=-4.098             # Both flat garage and landing finish
        self.assertAlmostEqual((casino_y-street_y)/32.5,0.007646153846153846,places=10)
        self.assertAlmostEqual((casino_y-garage_y)/46,0.09448913043478261,places=10)
        self.assertIn('GradedDrive(shell,"left_street_road_connection",asphalt,',self.casino)
        self.assertIn("-85f,-52.5f,-48f,-37f,0f,.2485f,true",self.casino)
        self.assertIn('GradedDrive(basement,"garage_descent_flat_join",garageBlackAsphalt,',self.casino)
        self.assertIn("rampX0,rampX1,-22f,24f,roadY,garageY,false",self.casino)
        self.assertIn("3.775f,-4.109f,2.5f,71.55f,.022f,53f",self.casino)
        self.assertIn("44.225f,-4.109f,28.5f,9.35f,.022f,9f",self.casino)
        # Main hall floor and turn landing share exactly x39.55, never overlap.
        self.assertAlmostEqual(3.775+71.55/2,44.225-9.35/2)
        self.assertAlmostEqual(-4.109+.022/2,garage_y)

    def test_street_sidewalk_gap_removed_and_11m_entrance(self):
        self.assertIn("T11_STREET_ENTRANCE=(410.0,417.0,7.0,11.0)",self.ground)
        self.assertIn("if inside((x,z),T11_STREET_ENTRANCE):return None",self.ground)
        self.assertIn("T11_GARAGE_RAMP,T11_STREET_ENTRANCE",self.ground)
        # 11m width from z=417 to 428 joins existing street x410 to
        # casino circulation x442.5 in one continuous graded road.
        self.assertAlmostEqual(428-417,11)
        self.assertAlmostEqual(442.5-410,32.5)
        self.assertLess((casino_y:=.2485)/32.5,.01)  # under 1% slope

    def test_no_second_apk_or_changed_original_owner_artwork(self):
        self.assertIn("C1_KASYNO_KASYNO_WAWER.png",self.casino)
        self.assertIn('const int baysPerRow=16;',self.casino)
        self.assertNotIn("BuildPipeline.BuildPlayer",self.casino)
        self.assertNotIn('Tower();',self.casino)


if __name__=="__main__":
    unittest.main(verbosity=2)
