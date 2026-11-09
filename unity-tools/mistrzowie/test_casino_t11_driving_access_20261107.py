"""T11 casino road/garage geometry contracts — source-only, no Unity/APK."""
from pathlib import Path
import unittest

ROOT=Path(__file__).resolve().parents[2]
T11=ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsCasinoT11.cs"
GROUND=ROOT/"unity-tools/mistrzowie/legacy_foundation.py"


class CasinoVehicleAccessContract(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.casino=T11.read_text(encoding="utf-8")
        cls.ground=GROUND.read_text(encoding="utf-8")

    def test_foundation_openings_and_fire_lane_clearance(self):
        self.assertIn("T11_GARAGE_RAMP=(534.15,443.0,10.15,51.0)",self.ground)
        self.assertIn("T11_STREET_ENTRANCE=(410.0,417.0,7.0,11.0)",self.ground)
        self.assertIn("if inside((x,z),T11_GARAGE_RAMP):return None",self.ground)
        self.assertIn("if inside((x,z),T11_STREET_ENTRANCE):return None",self.ground)
        # East fire lane west edge is world x=544.5; ramp cut ends at 544.30.
        self.assertLess(534.15+10.15,544.5)

    def test_bottom_turn_is_level_left_and_closed_straight_ahead(self):
        self.assertIn("Local turn pocket z=17..29",self.casino)
        self.assertIn("Garage east solid wall WHITE before LEFT TURN opening",self.casino)
        self.assertIn("T11 SOLID END WALL straight ahead at ramp bottom",self.casino)
        self.assertNotIn("Garage short north return after LEFT TURN",self.casino)
        self.assertIn("rampX0=39.55f,rampX1=48.90f",self.casino)
        self.assertIn("rampX0,rampX1,-22f,17f,roadY,garageY",self.casino)
        self.assertAlmostEqual(29-17,12)
        self.assertAlmostEqual(48.90-39.55,9.35)

    def test_surface_asphalt_is_level_edge_to_edge_and_not_duplicated(self):
        self.assertIn("const float roadY=.228f, roadH=.041f, roadTop=.2485f;",self.casino)
        self.assertIn('"Front VIP/TAXI circulating driveway",asphalt,0,roadY,-42.5f,99f,roadH,10f',self.casino)
        self.assertIn('"Fire service route",asphalt,side*53f,roadY,3.25f,7.0f,roadH,81.5f',self.casino)
        self.assertIn('"Rear service and deliveries",asphalt,0,roadY,46.7f,99f,roadH,5.4f',self.casino)
        self.assertIn('"Delivery corridor to rear loading door",asphalt,-20f,roadY,36f,6.4f,roadH,16f',self.casino)
        self.assertNotIn('side<0?"VIP arrival":"TAXI arrival"',self.casino)
        # Front/rear roads meet fire lanes at x=+-49.5, never overlap them.
        self.assertAlmostEqual(99/2,49.5)
        self.assertAlmostEqual(53-7/2,49.5)
        # Delivery corridor ends exactly where rear road begins.
        self.assertAlmostEqual(36+16/2,46.7-5.4/2)

    def test_surface_markings_curbs_and_drains_are_seated(self):
        for label in (
            "Front driveway NORTH concrete curb","Front driveway SOUTH concrete curb",
            "Fire lane INNER concrete curb","Fire lane OUTER concrete curb",
            "Rear service SOUTH concrete curb left","Rear service NORTH concrete curb",
            "Delivery corridor WEST concrete curb","Delivery corridor EAST concrete curb",
            "Garage approach flush trench drain","Rear loading flush trench drain"):
            self.assertIn(label,self.casino)
        self.assertIn("roadTop+.004f,-43.8f",self.casino)
        self.assertIn("roadTop+.004f,-46.55f",self.casino)
        # Owner 10.11 00:22: the six short zebra stripes beside the red carpet are removed.
        self.assertNotIn("White zebra marking on asphalt",self.casino)
        self.assertIn('"Rear loading bay white line",parkingPaint',self.casino)

    def test_city_road_connector_is_continuous_and_gentle(self):
        self.assertIn('GradedDrive(shell,"left_street_road_connection",asphalt,',self.casino)
        self.assertIn("-85f,-49.5f,-48f,-37f,0f,roadTop,true",self.casino)
        self.assertLess(.2485/35.5,.01)
        self.assertAlmostEqual(495-85,410)
        self.assertAlmostEqual(495-49.5,445.5)

    def test_garage_top_approach_and_owner_signage(self):
        self.assertIn('"Garage entrance from VIP/TAXI circulation",asphalt',self.casino)
        self.assertIn("44.225f,.228f,-29.75f,9.35f,.041f,15.5f",self.casino)
        self.assertNotIn('RoadsideOriginalSign("PARKING"',self.casino)
        # Owner 9.11 23:46: the TAXI board stood in the middle of the garage driveway - removed.
        self.assertNotIn('RoadsideOriginalSign("TAXI"',self.casino)
        self.assertIn("C1_KASYNO_KASYNO_WAWER.png",self.casino)
        self.assertNotIn("BuildPipeline.BuildPlayer",self.casino)
        self.assertNotIn("Tower();",self.casino)


if __name__=="__main__":
    unittest.main(verbosity=2)
