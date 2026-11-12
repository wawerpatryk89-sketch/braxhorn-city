"""Owner 12.11 02:47-02:56: AQUAPARK BRAXHORN (lagoon, the slide tower of his picture with rides, kids' zone with his slides,
SPA with his hot tubs / saunas / massage tables, loungers you lie on). Static checks only (no Unity)."""
import json
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ED = ROOT / 'unity-city/Assets/Editor/Mistrzowie'
FILES = ['BraxChampionsAquapark', 'BraxChampionsAquaparkTeren', 'BraxChampionsAquaparkWyposazenie', 'BraxChampionsAquaparkZjezdzalnie', 'BraxChampionsAquaparkBudynek']


def src():
    return '\n'.join((ED / (f + '.cs')).read_text(encoding='utf8') for f in FILES)


class Aquapark(unittest.TestCase):
    def test_wired_and_metas(self):
        b = (ED / 'BraxChampionsBuildings.cs').read_text(encoding='utf8')
        self.assertIn('BraxChampionsAquapark.Generate()', b)
        for f in FILES: self.assertTrue((ED / (f + '.cs.meta')).exists(), f)
        self.assertTrue((ROOT / 'unity-city/Assets/Mistrzowie/BraxChampionsZjezdzalnia.cs.meta').exists())

    def test_ground_cut_matches_the_lot(self):
        lf = (ROOT / 'unity-tools/mistrzowie/legacy_foundation.py').read_text(encoding='utf8')
        m = re.search(r'AQUAPARK_SITE=\(([\d.]+),([\d.]+),([\d.]+),([\d.]+)\)', lf)
        self.assertIsNotNone(m)
        x, z, w, d = map(float, m.groups())
        s = src()
        for name, val in (('X0', x), ('X1', x + w), ('Z0', z), ('Z1', z + d)):
            self.assertRegex(s, r'\b%s=%sf' % (name, int(val)), name)
        self.assertIn('if inside((x,z),AQUAPARK_SITE):return None', lf)
        self.assertIn('rects+=[AQUAPARK_SITE]', lf)

    def test_everything_functional(self):
        s = src()
        self.assertGreaterEqual(s.count('Ride("'), 3)                              # red, yellow, the racers
        self.assertIn('AddComponent<BraxChampionsZjezdzalnia>()', s)
        self.assertIn('AddComponent<BraxChampionsPoolSwimVolume>()', s)              # swimming in the lagoon
        self.assertIn('s.lie = true; s.label = "LEŻAK"', s)                           # loungers you lie on
        self.assertIn('s.label = "JACUZZI"', s)
        self.assertIn('s.label = "STÓŁ DO MASAŻU"', s)
        self.assertIn('AddComponent<BraxCityDoor>()', s)
        self.assertIn('MeshCollider', s)                                              # walkable decks, shells, stairs
        self.assertIn('AddComponent<BraxZrodloPetli>()', s)                           # the sound of the waterfall and fountains
        r = (ROOT / 'unity-city/Assets/Mistrzowie/BraxChampionsZjezdzalnia.cs').read_text(encoding='utf8')
        for k in ('IBraxInterakcja', 'BraxInterakcje.Dodaj(this)', '"ZJEDŹ · "', 'siedzisko.Use(gracz)', 'siedzisko.Leave()'):
            self.assertIn(k, r)

    def test_owner_models_and_rules(self):
        s = src()
        for plik in ('zjezdzalnia_dzieci_palma', 'zjezdzalnia_dzieci_wieza', 'jacuzzi_wanna', 'sauna_kabina', 'prysznic_kabina', 'lawka_szatnia',
                     'umywalka_podwojna', 'ratownik_krzeslo', 'stol_masaz_drewno', 'stol_masaz_kosmetyczny', 'stol_masaz_skladany_czarny',
                     'lezanka_masaz_tajski', 'stol_masaz_skladany_zielony'):
            self.assertIn('"%s"' % plik, s, plik)
        mf = json.loads((ROOT / 'unity-city/Prywatne/braxhorn-sklep-basen-01/manifest.json').read_text(encoding='utf8'))
        self.assertEqual(mf['target'], 'basen_01')
        self.assertNotIn('gold', s.lower().replace('golden horn', ''))                # the region's rule: no gold, gold = light
        self.assertNotIn('BraxChampionsGold', s)
        self.assertIn('Braxhorn/ChampionsP1PoolWater', s)
        self.assertIn('AQUAPARK_TRIANGULATE_AREA', s)                                # every surface checked for gaps / overlaps


if __name__ == '__main__':
    unittest.main()
