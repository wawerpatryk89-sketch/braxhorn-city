"""Owner 12.11 01:59-02:37: club girls at every club, rigged girls walking the city with gestures, and the BRAXHORN
NUTRITION posters with his two friends in gyms, nutrition/sport shops and entertainment. Static checks only (no Unity)."""
import json
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ED = ROOT / 'unity-city/Assets/Editor/Mistrzowie'


class PostaciePlakaty(unittest.TestCase):
    def test_packs_encrypted_and_complete(self):
        for pack, target, need in (('braxhorn-sklep-postacie-01', 'postacie_01',
                                    ['klub_czarna_sukienka.glb', 'klub_czerwona_sukienka.glb', 'klub_biala_sukienka.glb', 'klub_panterka.glb',
                                     'klub_rozowa_sukienka.glb', 'chodzi_czerwona_bluzka.glb', 'chodzi_niebieska_bluzka.glb',
                                     'chodzi_czarne_bikini.glb', 'chodzi_biale_bikini.glb', 'chodzi_czarne_bikini.json']),
                                   ('braxhorn-sklep-plakaty-01', 'plakaty_01',
                                    ['plakat_braxhorn_nutrition_whey.jpg', 'plakat_braxhorn_nutrition_kreatyna.jpg'])):
            d = ROOT / 'unity-city/Prywatne' / pack
            mf = json.loads((d / 'manifest.json').read_text())
            self.assertEqual(mf['target'], target)
            for n in need: self.assertIn(target + '/' + n, mf['files'])
            self.assertFalse(any(p.suffix in ('.glb', '.png', '.jpg', '.json') and p.name != 'manifest.json' for p in d.iterdir()))

    def test_generators_wired(self):
        b = (ED / 'BraxChampionsBuildings.cs').read_text()
        self.assertIn('BraxChampionsPostacie.Generate()', b)
        self.assertIn('BraxChampionsPlakaty.Generate()', b)
        p = (ED / 'BraxChampionsPostacie.cs').read_text()
        for s in ('AddComponent<BraxPieszy>()', 'updateWhenOffscreen = true',
                  'Kl("Taunt")', 'Kl("Warming Up")', 'Kl("Head Hit")', 'speed_units_per_s', 'legacy = true'):
            self.assertIn(s, p)
        # owner 12.11 11:45: no standing (static) figures anywhere - only the walking ones
        self.assertNotIn('PodKlubem(', p)
        self.assertNotIn('klub_czarna_sukienka', p)
        q = (ED / 'BraxChampionsPlakaty.cs').read_text()
        self.assertIn('public static void Plakat(', q)
        self.assertIn('plakat_braxhorn_nutrition_whey', q)


if __name__ == '__main__':
    unittest.main()
