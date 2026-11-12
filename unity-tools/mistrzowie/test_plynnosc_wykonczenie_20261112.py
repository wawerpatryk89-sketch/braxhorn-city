"""Owner 12.11 12:21-13:13: the game stutters and loads 5 minutes (again after every exit), no kitchen runs in the flats,
walls and ceilings must not read as concrete, a seat must not put Brax under a ceiling."""
import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
ASSETS = ROOT / 'unity-city' / 'Assets'
ED = ASSETS / 'Editor' / 'Mistrzowie'


def src(p):
    return (ASSETS / p).read_text(encoding='utf-8')


class PlynnoscWykonczenie(unittest.TestCase):
    def test_bundles_per_folder_not_per_asset(self):
        b = src('Editor/Updates/BraxContentBuild.cs')
        self.assertIn('Group(settings, bucket, true)', b)                      # 10 038 bundles in APK 39 -> a few dozen
        self.assertIn('BundlePackingMode.PackTogether', b)
        self.assertIn('public static string Bucket(string path)', b)

    def test_region_performance_manager(self):
        w = src('Mistrzowie/BraxChampionsWydajnosc.cs')
        self.assertIn('if (x.enabled != blisko) x.enabled = blisko;', w)       # lamps on only near the camera
        self.assertIn('if (czynne) czynne.enabled = true;', w)                 # one live mirror at a time, in sight
        self.assertIn('kamera.layerCullSpherical = true;', w)
        e = src('Editor/Mistrzowie/BraxChampionsExplorer.cs')
        self.assertIn('rig.AddComponent<BraxChampionsWydajnosc>().kamera = camera;', e)
        self.assertIn('SmallThingsLayers(scene);', e)
        self.assertTrue((ASSETS / 'Mistrzowie' / 'BraxChampionsWydajnosc.cs.meta').exists())

    def test_seats_read_their_height_where_they_finally_stand(self):
        e = src('Editor/Mistrzowie/BraxChampionsExplorer.cs')
        self.assertIn('SeatsOnTheirSurfaces(scene);', e)
        self.assertIn('s.surfaceY = y;', e)

    def test_interior_walls_are_smooth_paint(self):
        f = src('Editor/Mistrzowie/BraxChampionsFoundation.cs')
        self.assertIn('public const string InteriorPaintSlug="beige_wall_001";', f)
        for name, ident in [('BraxChampionsApartments.cs', 'apt_wall_white'), ('BraxChampionsApartments.cs', 'apt_ceiling_white_paint'),
                            ('BraxChampionsHotelSobczakMaterials.cs', 'wall_paint_white'), ('BraxChampionsG1.cs', 'g1_white_plaster'),
                            ('BraxChampionsG1.cs', 'g1_ceiling_plaster'), ('BraxChampionsCasinoT11Hall.cs', 't11_hall_cream_coffered_ceiling'),
                            ('BraxChampionsArenaFront.cs', 'A1_lobby_ceiling_warm_white_plaster')]:
            self.assertIn('"%s",BraxChampionsFoundation.InteriorPaintSlug' % ident, src('Editor/Mistrzowie/' + name).replace(', ', ','), name)

    def test_no_kitchen_run_in_the_flats(self):
        self.assertNotIn('BraxKatalog.Kitchen(', src('Editor/Mistrzowie/BraxChampionsApartments.cs'))


if __name__ == '__main__':
    unittest.main()
