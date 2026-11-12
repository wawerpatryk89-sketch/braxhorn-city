"""The region read in parts like the city R1 (owner 12.11.2026 20:42-20:58: "długo się ładuje ... wywala ... masz zobaczyć,
jak miasto R1 jest zrobione, i zrobić tak samo ... jedna ta sama gra, tylko że podzielona")."""
import pathlib
import re
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
A = ROOT / 'unity-city/Assets'


def src(rel):
    return (A / rel).read_text(encoding='utf-8')


class Loader(unittest.TestCase):
    def test_loads_through_the_content_bundles_one_at_a_time(self):
        s = src('Mistrzowie/BraxChampionsObszary.cs')
        self.assertIn('BraxContent.Begin(o.scena, LoadSceneMode.Additive)', s)
        self.assertIn('yield return BraxContent.Unload(sc);', s)
        self.assertIn('if (zajety || Time.unscaledTime < nastepne) return;', s)
        self.assertNotIn('SceneManager.LoadSceneAsync', s)             # the APK's scenes are in the content bundles
        self.assertIn('Resources.UnloadUnusedAssets()', s)

    def test_nearest_first_and_ahead_of_a_fast_player(self):
        s = src('Mistrzowie/BraxChampionsObszary.cs')
        self.assertIn('o.granice.SqrDistance(p + naprzod)', s)
        self.assertIn('o.odleglosc < doWczytania.odleglosc', s)
        self.assertIn('public float wczytaj = 150f, zwolnij = 280f;', s)

    def test_never_locked_never_left_frozen(self):
        s = src('Mistrzowie/BraxChampionsObszary.cs')
        self.assertEqual(s.count('finally { Laduje = Mathf.Max(0, Laduje - 1); zajety = false; }'), 2)
        self.assertIn('o.blad = true;', s)
        self.assertIn('if (zamrozony && gracz && gracz.body) gracz.body.enabled = true;', s)
        self.assertIn('else if (!wSrodku && !start && zamrozony) { gracz.body.enabled = true; zamrozony = false; }', s)

    def test_far_copy_swaps_in_the_frame_the_scene_arrives(self):
        s = src('Mistrzowie/BraxChampionsObszarZDaleka.cs')
        self.assertIn('SceneManager.sceneLoaded += Wczytano;', s)
        self.assertIn('r.enabled = nowy == 0;', s)

    def test_lamps_and_mirrors_follow_the_scenes(self):
        s = src('Mistrzowie/BraxChampionsWydajnosc.cs')
        self.assertIn('SceneManager.sceneLoaded += ScenaDoszla;', s)
        self.assertIn('if (skanuj || lampy == null) Przeskanuj();', s)

    def test_scripts_have_meta(self):
        guids = set()
        for n in ('BraxChampionsObszary', 'BraxChampionsObszarZDaleka'):
            m = re.search(r'guid: ([0-9a-f]{32})', (A / 'Mistrzowie' / (n + '.cs.meta')).read_text())
            self.assertTrue(m, n)
            guids.add(m.group(1))
        self.assertEqual(len(guids), 2)
        self.assertTrue((A / 'Editor/Mistrzowie/BraxChampionsPodzial.cs.meta').exists())


class Split(unittest.TestCase):
    def test_every_big_building_is_an_area(self):
        s = src('Editor/Mistrzowie/BraxChampionsPodzial.cs')
        for start in ('T03 | HOTEL SOBCZAK', 'G1 | GALERIA', 'A1 | BRAXHORN CHAMPION ARENA', 'D2 | DWORZEC', 'E1 | CENTRUM ROZRYWKI',
                      'T11 | KASYNO WAWER', 'T04 | APARTAMENTY', 'T05 | APARTAMENTY', 'T06 | APARTAMENTY', 'T07 | APARTAMENTY',
                      'AQUAPARK BRAXHORN'):
            self.assertIn('"' + start + '"', s)
        # the names the generators give their roots
        self.assertIn('new GameObject("G1 | GALERIA', src('Editor/Mistrzowie/BraxChampionsG1.cs'))
        self.assertIn('new GameObject("AQUAPARK BRAXHORN', src('Editor/Mistrzowie/BraxChampionsAquapark.cs'))
        names = re.findall(r'Strefa\("(Mistrzowie_[A-Za-z0-9_]+)"', s)
        self.assertEqual(len(names), len(set(names)))
        self.assertGreaterEqual(len(names), 16)

    def test_no_reference_crosses_a_border(self):
        s = src('Editor/Mistrzowie/BraxChampionsPodzial.cs')
        self.assertIn('wSrodku.Contains(k.z) != wSrodku.Contains(k.d)', s)
        self.assertIn('EditorSceneManager.DetectCrossSceneReferences(part)', s)
        self.assertIn('SceneManager.MoveGameObjectToScene(wezly[i].gameObject, main);', s)      # moved back on a hit
        self.assertIn('catch (Exception e) { pominiete++;', s)                                  # one area never costs the build

    def test_player_boats_car_and_walkers_stay(self):
        s = src('Editor/Mistrzowie/BraxChampionsPodzial.cs')
        for t in ('BraxGymPlayer', 'BraxCityWorld', 'BraxVessel', 'BraxChampionsCar', 'BraxPieszy'):
            self.assertIn('typeof(' + t + ')', s)

    def test_build_packs_the_area_scenes(self):
        b = src('Editor/Mistrzowie/BraxChampionsBuild.cs')
        self.assertEqual(b.count('SplitIntoAreas(report);'), 2)                                 # scene check and APK
        self.assertLess(b.index('SplitIntoAreas(report);\n                BuildExplorationApk();'), b.index('static void SplitIntoAreas'))
        self.assertIn('new[] { BraxChampionsExplorer.ScenePath }.Concat(areaScenes).ToArray()', b)
        self.assertIn('scenes=new[]{BraxChampionsExplorer.ScenePath}.Concat(BraxChampionsPodzial.Sceny()).ToArray()', b)
        self.assertIn('BraxChampionsPodzial.Wyczysc()', b)                                      # a failed split leaves one whole scene

    def test_review_player_proves_every_area_loads(self):
        r = src('Mistrzowie/BraxChampionsVisualReview.cs')
        self.assertIn('BraxChampionsObszary.Wszystko=true;', r)
        self.assertIn('CHAMPIONS_REVIEW_AREAS loaded=', r)


if __name__ == '__main__':
    unittest.main()
