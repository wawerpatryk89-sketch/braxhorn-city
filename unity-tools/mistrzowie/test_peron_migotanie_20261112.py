"""Owner 12.11 11:39-11:45: no lifts on the D2 platforms (they stood in front of every stair), nothing may flicker (all
white lines / stripes), no static figures."""
import pathlib
import re
import unittest

ED = pathlib.Path(__file__).resolve().parents[2] / 'unity-city' / 'Assets' / 'Editor' / 'Mistrzowie'


def src(name):
    return (ED / name).read_text(encoding='utf-8')


class PeronMigotanie(unittest.TestCase):
    def test_no_platform_lifts(self):
        k = src('BraxChampionsD2Kolej.cs')
        self.assertNotIn('Lift(lx0', k)
        self.assertNotIn('static void Lift(', k)
        holes = re.search(r'static IEnumerable<\(float x0,float x1\)> Holes\(float xa\)\{(.*?)\}', k).group(1)
        self.assertEqual(holes.count('yield return'), 1)
        self.assertNotIn('LiftCar(', src('BraxChampionsD2Autobusy.cs'))
        # the lifts inside the station building (hall <-> first floor) stay
        self.assertIn('BuildingLift(', src('BraxChampionsD2Pietro.cs'))

    def test_platform_lines_and_nosings_inlaid(self):
        k = src('BraxChampionsD2Kolej.cs')
        self.assertNotIn('PlatformTop+.012f', k)
        self.assertIn('B(paintWhite,PlatX0,PlatX1,PlatformTop-.25f,PlatformTop', k)
        self.assertIn('B(tactile,PlatX0,PlatX1,PlatformTop-.25f,PlatformTop', k)
        self.assertNotIn('Flat(paintYellow,x,x+.05f', k)
        self.assertIn('B(paintYellow,x,x+.05f,y-.05f,y', k)
        self.assertNotIn('Flat(paintYellow', src('BraxChampionsD2Pietro.cs'))

    def test_all_paint_has_depth_offset(self):
        self.assertIn('public static Material Nakladka(Material m)', src('BraxChampionsHarbour.cs'))
        self.assertIn('"_OffsetFactor",-1f', src('BraxChampionsHarbour.cs'))
        for f, needle in [('BraxChampionsD2.cs', 'paintWhite=BraxChampionsHarbour.Nakladka('),
                          ('BraxChampionsD2.cs', 'tactile=BraxChampionsHarbour.Nakladka('),
                          ('BraxChampionsInfrastructure.cs', '{"paint",BraxChampionsHarbour.Nakladka('),
                          ('BraxChampionsInfrastructure.cs', '{"tactile",tactileTop}'),
                          ('BraxChampionsArenaArrival.cs', 'Nakladka(Paint("arena_parking_markings"'),
                          ('BraxChampionsCasinoT11.cs', 'parkingPaint=BraxChampionsHarbour.Nakladka('),
                          ('BraxChampionsCasinoT11.cs', 'garageParkingPaint=BraxChampionsHarbour.Nakladka('),
                          ('BraxChampionsE1.cs', 'lineWhite=BraxChampionsHarbour.Nakladka('),
                          ('BraxChampionsApartments.cs', 'parkPaint=BraxChampionsHarbour.Nakladka('),
                          ('BraxChampionsHotelSobczakT03.cs', 'Box(zebra,-1.7f,1.7f,DriveY,DriveY+.012f')]:
            self.assertIn(needle, src(f), f)

    def test_no_static_figures(self):
        p = src('BraxChampionsPostacie.cs')
        self.assertNotIn('PodKlubem(', p)
        self.assertIn('AddComponent<BraxPieszy>()', p)


if __name__ == '__main__':
    unittest.main()
