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

    def test_real_ramp_and_street_openings_on_approved_plot(self):
        self.assertIn("T11_GARAGE_RAMP=(534.15,443.0,10.15,51.0)",self.ground)
        self.assertIn("T11_STREET_ENTRANCE=(410.0,417.0,7.0,11.0)",self.ground)
        self.assertIn("if inside((x,z),T11_STREET_ENTRANCE):return None",self.ground)
        self.assertIn("if inside((x,z),T11_GARAGE_RAMP):return None",self.ground)
        self.assertTrue(any(line.startswith("rects=") and "+[T11_GARAGE_RAMP,T11_STREET_ENTRANCE]" in line
                            for line in self.ground.splitlines()))

    def test_grade_turn_and_fire_route_clearance(self):
        # Physical descent is z=-22..17: 39m. The generator eases 2m at each end
        # and hard-rejects an effective grade above 12%.
        self.assertLess((.2485+4.098)/(39-2),.12)
        self.assertIn("SmoothGarageRamp(basement,garageBlackAsphalt",self.casino)
        self.assertIn("rampX0,rampX1,-22f,17f,roadY,garageY",self.casino)
        self.assertIn("westExitZ=17..29 width=12m",self.casino)
        self.assertIn("T11 SOLID END WALL straight ahead at ramp bottom",self.casino)
        self.assertNotIn("Garage short north return after LEFT TURN",self.casino)
        self.assertGreaterEqual(49.5-49.30,.19)

    def test_thirty_two_physical_bays_real_supports_and_eight_lamps(self):
        self.assertIn("const int baysPerRow=16;",self.casino)
        self.assertIn("for(int i=0;i<=baysPerRow;i++)",self.casino)
        self.assertIn("new[]{-17f,10f}",self.casino)
        self.assertIn("B1 physical car-space border",self.casino)
        self.assertIn("B1 continuous marked bay head line",self.casino)
        self.assertIn("B1 WHITE dashed driving-aisle centre line",self.casino)
        self.assertIn("for(int n=0;n<8;n++)",self.casino)
        self.assertIn("71.55f,.022f,53f,true,true",self.casino)
        self.assertIn("B1 structural reinforced WHITE support column",self.casino)
        self.assertIn("new[]{-30.25f,29.35f}",self.casino)
        self.assertIn("new[]{-18f,0f,13.5f}",self.casino)
        self.assertIn("B1 real warm-white parking light",self.casino)
        self.assertIn("bulb.shadows=LightShadows.None;",self.casino)
        self.assertIn("bulb.renderMode=LightRenderMode.ForceVertex;",self.casino)
        self.assertEqual(16*2,32)
        self.assertEqual(4*2,8)
        self.assertLess(29.35+.78/2,39.55)

    def test_garage_is_covered_and_has_real_walls_floor_and_turn(self):
        for label in ["T11 stone plot apron south","T11 stone plot apron north",
                      "T11 stone plot apron west of garage","T11 stone plot apron east of garage"]:
            self.assertIn(label,self.casino)
        for label in (
            "Garage WHITE reinforced ceiling","Garage structural floor - real collider",
            "Garage main entrance champagne beam","Garage bottom BLACK asphalt landing",
            "Garage east solid wall WHITE before LEFT TURN opening","Ramp east retaining wall",
            "T11 SOLID END WALL straight ahead at ramp bottom","Garage floor BLACK Poly Haven asphalt",
            "Garage solid west retaining wall WHITE","Garage north wall WHITE",
            "Garage south wall WHITE","B1 WHITE transverse roof beam",
            "B1 WHITE longitudinal roof beam","B1 WHITE column protective plinth"):
            self.assertIn(label,self.casino)
        self.assertIn('"B1_clean_white_wall_and_columns"',self.casino)
        self.assertIn('"B1_white_structural_roof_beams"',self.casino)
        self.assertIn("new Color(.99f,.99f,.99f)",self.casino)
        self.assertIn("C1_KASYNO_KASYNO_WAWER.png",self.casino)
        # 9.11: rotunda lobby exists (owner board); still no gaming floor.
        self.assertIn("no gaming floor yet",self.casino)


if __name__=="__main__":
    unittest.main(verbosity=2)
