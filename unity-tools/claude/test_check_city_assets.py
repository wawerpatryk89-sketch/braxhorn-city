from pathlib import Path
from types import SimpleNamespace
import tempfile
import unittest
from unittest.mock import Mock
from check_city_assets import check

class AggregateTests(unittest.TestCase):
    def test_reports_all_missing_inputs_and_runs_both_model_checks(self):
        with tempfile.TemporaryDirectory() as tmp:
            runner=Mock(side_effect=[SimpleNamespace(returncode=3),SimpleNamespace(returncode=2)])
            errors=check(Path(tmp),runner)
        self.assertEqual(len(errors),33)
        self.assertEqual(runner.call_count,2)
        self.assertTrue(any('lek_24' in x for x in errors))
        self.assertTrue(any('check_metro' in x for x in errors))
    def test_complete_inputs_pass_and_either_sky_resolution_is_accepted(self):
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp); source=root/'unity-city/Assets/Source'
            files=['Claude/Osiedle/detail/pokoje.png','Claude/PolyHaven/sky/kloofendal_8k.jpg',
                   'Prywatne/Miasto/miasto_wyposazenie/miasto_wyposazenie.glb']
            files += [f'Prywatne/Miasto/miasto_sklepy/lek_{i:02d}.glb' for i in range(1,25)]
            files += [f'Prywatne/Miasto/miasto_sklepy/odz_whey_{i}.glb' for i in range(1,5)]
            for file in files:
                p=source/file;p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(b'fixture')
            self.assertEqual(check(root,Mock(return_value=SimpleNamespace(returncode=0))),[])

if __name__=='__main__':unittest.main()
