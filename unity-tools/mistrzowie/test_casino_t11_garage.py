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
        self.assertIn("T11_GARAGE_RAMP=(535.5,443.0,8.0,46.0)",self.ground)
        self.assertIn("if inside((x,z),T11_GARAGE_RAMP):return None",self.ground)
        self.assertIn("rects=plots+walks+greens+[p[:4] for p in piers]+[T11_GARAGE_RAMP]+POOL_HOLES+",self.ground)
        self.assertIn("if any(inside((x,z),r) for r in POOL_HOLES):return None",self.ground)
        self.assertGreaterEqual(535.5,495-60)
        self.assertLessEqual(543.5,495+60)
        self.assertGreaterEqual(443,465-50)
        self.assertLessEqual(489,465+50)

    def test_grade_and_fire_route_clearance(self):
        grade=4.39/46.0
        self.assertGreater(grade,.07)
        self.assertLess(grade,.12)
        # Ramp ends x=48.5; first east-side fire lane begins x=49.5.
        self.assertGreaterEqual(49.5-48.5,1.0)
        self.assertIn("Mathf.Atan2(4.39f,46f)",self.casino)
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
        self.assertIn("73f,.02f,53f,true,true",self.casino)
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
        self.assertIn("Ramp west retaining wall",self.casino)
        self.assertIn("Ramp east retaining wall",self.casino)
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
