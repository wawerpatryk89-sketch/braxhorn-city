import sys,unittest,math
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'tools'))
from prepare_oboz_forest_layout import make,excluded

class ForestLayoutTests(unittest.TestCase):
    def test_repeatable_and_keeps_access_routes_clear(self):
        d=make();self.assertEqual(d,make());self.assertGreater(len(d['trees']),1000)
        for t in d['trees']:
            self.assertFalse(excluded(t['x'],t['z'],2.79));self.assertLess(abs(t['x']),210);self.assertLess(abs(t['z']),210)
            self.assertIn(t['prototype'],range(6))
        for p in d['grass']:self.assertFalse(excluded(p['x'],p['z'],.19))
    def test_surround_forest_has_no_walkable_collision_claim(self):
        for t in make()['trees']:
            if abs(t['x'])>146 or abs(t['z'])>146:self.assertFalse(t['solid'])

if __name__=='__main__':unittest.main()
