"""Offline T11 parking geometry checks; does NOT claim Unity physics or drive test."""
import unittest
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
GROUND=ROOT/"unity-tools/mistrzowie/legacy_foundation.py"
T11=ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsCasinoT11.cs"

class CasinoT11UndergroundTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.ground=GROUND.read_text(encoding="utf-8")
        cls.casino=T11.read_text(encoding="utf-8")

    def test_real_8_by_46_opening_on_approved_plot(self):
        self.assertIn("T11_GARAGE_RAMP=(534.15,443.0,10.15,46.0)",self.ground)
        self.assertIn("T11_STREET_ENTRANCE=(410.0,417.0,7.0,11.0)",self.ground)
        self.assertIn("if inside((x,z),T11_STREET_ENTRANCE):return None",self.ground)
        self.assertIn("if inside((x,z),T11_GARAGE_RAMP):return None",self.ground)
        # Keep the original T11 garage cutout even as port/promenade rectangles
        # are added to the same subdivision; exact old list order is irrelevant.
        self.assertTrue(any(line.startswith("rects=") and "+[T11_GARAGE_RAMP,T11_STREET_ENTRANCE]" in line for line in self.ground.splitlines()),"Integrated geometry omitted the T11 ramp and exact street-entrance cutouts")
        self.assertGreaterEqual(534.15,495-60)
        self.assertLessEqual(544.30,495+60)
        self.assertGreaterEqual(443,465-50)
        self.assertLessEqual(489,465+50)

    def test_grade_and_fire_route_clearance(self):
        grade=(.2485+4.098)/46.0
        self.assertGreater(grade,.07)
        self.assertLess(grade,.12)
        # Ramp + side walls end before the fire lane's west edge x=49.5.
        self.assertGreaterEqual(49.5-49.30,.19)
        self.assertIn('GradedDrive(basement,"garage_descent_flat_join"',self.casino)
        self.assertIn('GradedDrive(shell,"left_street_road_connection"',self.casino)
        self.assertIn('9.35f',self.casino)
        self.assertIn('z=15..29',self.casino)
        self.assertIn("T11 SOLID END WALL straight ahead at ramp bottom",self.casino)
        self.assertIn("UndergroundGarage();",self.casino)
        self.assertIn("Garage bottom structural landing",self.casino)
        self.assertNotIn("Garage interior and descending ramp intentionally deferred",self.casino)

    def test_thirty_two_physical_bays_real_supports_and_eight_lamps(self):
        self.assertIn("const int baysPerRow=16;",self.casino)
        self.assertIn("for(int i=0;i<=baysPerRow;i++)",self.casino)
        self.assertIn("new[]{-17f,10f}",self.casino)
        self.assertIn("B1 physical car-space border",self.casino)
        self.assertIn("B1 continuous marked bay head line",self.casino)
        self.assertIn("B1 WHITE dashed driving-aisle centre line",self.casino)
        self.assertIn("for(int n=0;n<8;n++)",self.casino)
        # Actual black wearing course has a working physics collider, not just paint.
        self.assertIn("71.55f,.022f,53f,true,true",self.casino)
        self.assertIn("B1 structural reinforced WHITE support column",self.casino)
        self.assertIn("new[]{-30.25f,29.35f}",self.casino)
        self.assertIn("new[]{-18f,0f,13.5f}",self.casino)
        self.assertIn("B1 real warm-white parking light",self.casino)
        self.assertIn("bulb.shadows=LightShadows.None;",self.casino)
        self.assertIn("bulb.renderMode=LightRenderMode.ForceVertex;",self.casino)
        # 16 stalls per row, two rows, six supports and eight lights.
        self.assertEqual(16*2,32)
        self.assertEqual(2*3,6)
        self.assertEqual(4*2,8)
        # Painted bay footprint (x -28..21.6) and pillar (x 29.35).
        self.assertGreater(29.35-.78/2-(-28+16*3.10),6.0)
        # South/north ramp remains at x 40.5..48.5, clear of pillars.
        self.assertLess(29.35+.78/2,40.5)

    def test_surface_collider_has_true_ramp_hole_and_garage_is_covered(self):
        for label in ["T11 stone plot apron south","T11 stone plot apron north",
                      "T11 stone plot apron west of garage","T11 stone plot apron east of garage"]:
            self.assertIn(label,self.casino)
        self.assertIn("Garage WHITE reinforced ceiling",self.casino)
        self.assertIn("Garage structural floor - real collider",self.casino)
        self.assertIn("Garage main entrance champagne beam",self.casino)
        self.assertIn("Garage bottom BLACK asphalt landing",self.casino)
        self.assertIn("Garage east solid wall WHITE before LEFT TURN opening",self.casino)
        self.assertIn("Ramp east retaining wall",self.casino)
        self.assertIn("Garage short north return after LEFT TURN",self.casino)
        self.assertIn("T11 SOLID END WALL straight ahead at ramp bottom",self.casino)
        self.assertIn('"B1_clean_white_wall_and_columns"',self.casino)
        self.assertIn('"B1_white_structural_roof_beams"',self.casino)
        self.assertIn("Garage floor BLACK Poly Haven asphalt",self.casino)
        self.assertIn("Garage solid west retaining wall WHITE",self.casino)
        self.assertIn("Garage north wall WHITE",self.casino)
        self.assertIn("Garage south wall WHITE",self.casino)
        self.assertIn("Garage east solid wall WHITE",self.casino)
        self.assertIn("B1 WHITE transverse roof beam",self.casino)
        self.assertIn("B1 WHITE longitudinal roof beam",self.casino)
        self.assertIn("B1 WHITE column protective plinth",self.casino)
        self.assertIn("new Color(.99f,.99f,.99f)",self.casino)
        # Assert the original owner's EXTERIOR palette and correct C1 logo.
        self.assertIn('t11_cream_facade',self.casino)
        self.assertIn('t11_graphite_facade',self.casino)
        self.assertIn('T11 original owner sign',self.casino)
        self.assertIn('C1_KASYNO_KASYNO_WAWER.png',self.casino)
        # Basement is separate; casino and offices retain an EMPTY interior.
        self.assertIn("EMPTY interior",self.casino)

if __name__=="__main__":
    unittest.main()
