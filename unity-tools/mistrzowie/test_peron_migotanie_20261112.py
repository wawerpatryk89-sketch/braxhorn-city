"""Owner 12.11 11:39-11:56: no lifts on the D2 platforms (they stood in front of every stair), nothing may flicker (all
white lines / stripes), no static figures, the rigged girls walk only in the casino and on the beach, no 9:16 films."""
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
        # owner 12.11 12:00 "żadnych wind tam nie ma być": no lift anywhere in the station (the G1 gallery keeps its own)
        self.assertNotIn('BuildingLift("D2', src('BraxChampionsD2Pietro.cs'))
        self.assertNotIn('29.94f', src('BraxChampionsD2Budynek.cs'))
        self.assertIn('BuildingLift(', src('BraxChampionsG1Wnetrze.cs'))

    def test_train_has_a_locomotive_at_each_end(self):
        s = src('BraxChampionsD2Sklad.cs')
        self.assertIn('Locomotive(front-TLocoLoB,front);', s)
        self.assertIn('Locomotive(xw-TLocoLoB,xw,"BX 160-02");', s)
        self.assertIn('float len=2f*TLocoLoB+TrainCoaches*TCoachLoB;', s)

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

    def test_walkers_only_casino_and_beach(self):
        p = src('BraxChampionsPostacie.cs')
        self.assertIn('465f + salaZ[k]', p)                       # inside the casino hall (plot 495/465)
        self.assertIn('(112f, 222f), (238f, 522f), (538f, 640f)', p)  # the beach stretches between the club decks
        self.assertNotIn('public static int Aquapark(', p)
        self.assertNotIn('-128.6f', p)                            # no promenade lanes
        self.assertNotIn('BraxChampionsPostacie.Aquapark', src('BraxChampionsAquapark.cs'))

    def test_no_portrait_films(self):
        tv = (ED.parents[1] / 'BraxhornWideo' / 'BraxTelewizor.cs').read_text(encoding='utf-8')
        # owner 12.11 12:25: only his music video stays 9:16, on the G1 totems where it played
        self.assertIn('public static readonly string[] Pion = { "pion_10_teledysk.mp4" };', tv)
        lista = re.search(r'public static readonly string\[\] Tv =\s*\{(.*?)\};', tv, re.S).group(1)
        self.assertEqual(re.findall(r'"([^"]+)"', lista), ['tv_01_puma.mp4', 'tv_02_alpy.mp4', 'tv_03_roboty.mp4',
                                                          'tv_04_gangsterzy.mp4', 'tv_05_fabryka.mp4'])
        g1 = src('BraxChampionsG1Wnetrze.cs')
        self.assertIn('G1AdTotem(x,y,z);', g1)
        for f in ED.glob('*.cs'):
            if f.name != 'BraxChampionsG1Wnetrze.cs':      # the only portrait screens: the G1 totems (the teledysk)
                self.assertIsNone(re.search(r'WorkingScreen\([^;]*,\s*true\s*,', f.read_text(encoding='utf-8')), f.name)

    def test_tv_plays_where_it_stands_and_only_in_sight(self):
        tv = (ED.parents[1] / 'BraxhornWideo' / 'BraxTelewizor.cs').read_text(encoding='utf-8')
        # owner 12.11 12:21/12:25: sound in the corridor, no film on any TV, no button, no choice of film
        self.assertIn('srodek = transform.TransformPoint(srodekLokalny)', tv)
        self.assertIn('s.glos.transform.position = srodek;', tv)
        self.assertIn('if (!t.Widac(p) && !(zGracza && t.Widac(oko))) continue;', tv)
        self.assertIn('"NASTĘPNY FILM · "', tv)
        self.assertIn('"WŁĄCZ TV"', tv)
        self.assertIn('"WYŁĄCZ TV"', tv)
        self.assertNotIn('s.glos.transform.position = transform.position', tv)
        kat = src('BraxKatalog.cs')
        self.assertIn('tv.maMiejsce = true; tv.srodekLokalny = centre * u;', kat)


if __name__ == '__main__':
    unittest.main()
