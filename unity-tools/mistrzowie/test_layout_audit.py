import copy,json,unittest
from pathlib import Path
from audit_layout import audit

class LayoutTests(unittest.TestCase):
    def setUp(self):self.buildings=json.loads(Path(__file__).with_name('plan_inventory.json').read_text())['buildings']
    def test_existing_reservations_keep_roads_and_metro_open(self):self.assertEqual(audit(self.buildings),[])
    def test_detects_overlapping_buildings(self):
        b=copy.deepcopy(self.buildings);b[1]['x']=b[0]['x'];b[1]['z']=b[0]['z']
        self.assertTrue(any('Overlapping' in e for e in audit(b)))
    def test_detects_building_in_road(self):
        b=copy.deepcopy(self.buildings);b[0]['x']=400
        self.assertTrue(any('Road/sidewalk' in e for e in audit(b)))
