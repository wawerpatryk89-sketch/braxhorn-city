"""Source-level regressions for five reused metro stations, distinct wayfinding,
shared real geometry, fixed handrails and honest disabled train service.
These are offline tests; Unity scene/device physics must still be verified.
"""
import hashlib
import json
from pathlib import Path
import tempfile
import unittest
import numpy as np
from shapely.geometry import LineString, Point
from layout import METRO, METRO_LINE_WAYPOINTS, METRO_ROUTE_ORDER, ENTRANCE_HOLES, ENTRANCE_PATHS
from meshkit import rounded_route
from audit_layout import audit
from prepare_signage import DESTINATIONS, render_all

ROOT=Path(__file__).resolve().parents[2]
EDITOR=ROOT/"unity-city/Assets/Editor"


class FiveMetroStations(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.route=LineString(rounded_route(METRO_LINE_WAYPOINTS,60,3.0,False))
        cls.buildings=json.loads(
            (ROOT/"unity-tools/mistrzowie/plan_inventory.json").read_text())["buildings"]

    def test_five_full_size_prefabs_including_original_three(self):
        self.assertEqual([s["id"] for s in METRO],["M1","M2","M3","M4","M5"])
        self.assertEqual([(s["x"],s["z"],s["axis"]) for s in METRO[:3]],
                         [(400,359,"x"),(765,545,"z"),(765,800,"z")])
        self.assertEqual([(s["x"],s["z"]) for s in METRO[3:]],[(235,645),(965,245)])
        unity=(EDITOR/"Mistrzowie/BraxChampionsInfrastructure.cs").read_text()
        source=(EDITOR/"Mistrzowie/BraxClaudeMetro.Reuse.cs").read_text()
        self.assertIn("var template=BraxClaudeMetro.ChampionsStationTemplate()",unity)
        self.assertIn("Object.Instantiate(template)",unity)
        self.assertIn("o.transform.localScale=Vector3.one",unity)
        self.assertIn('d.metroStations.Length!=5',unity)
        self.assertIn("W.Start(podziemie)",source)
        for part in ("Antresola();","SchodyNaPeron(true);","Hala();",
                     "Tory();","Tunele(true);","Wyposazenie();"):
            self.assertIn(part,source)
        self.assertIn("gates.Length!=7",source)
        self.assertIn("a.enabled=true",source)

    def test_real_rail_line_joins_all_stations_without_self_crossing(self):
        self.assertTrue(self.route.is_simple)
        self.assertEqual(set(METRO_ROUTE_ORDER),{s["id"] for s in METRO})
        self.assertGreater(self.route.length,1000)
        for station in METRO:
            self.assertLess(self.route.distance(Point(station["x"],station["z"])),.01,station["id"])

    def test_entrances_and_plot_setbacks(self):
        self.assertEqual(len(ENTRANCE_HOLES),5)
        self.assertEqual(len(ENTRANCE_PATHS),10)
        self.assertEqual(audit(self.buildings),[])
        self.assertLess(max(x[2] for x in ENTRANCE_PATHS),120)

    def test_improved_city_coverage(self):
        def nearest(b,stations):
            return min(np.hypot(b["x"]-s["x"],b["z"]-s["z"]) for s in stations)
        old=np.mean([nearest(b,METRO[:3]) for b in self.buildings])
        new=np.mean([nearest(b,METRO) for b in self.buildings])
        self.assertLess(new,old*.75)

    def test_hand_rail_is_not_moving_with_step_belt(self):
        metro=(EDITOR/"Claude/BraxClaudeMetro.cs").read_text()
        self.assertIn("BraxMiastoA.Obiekt(W.Grupa, nazwa + \": poręcz ruchoma",metro)
        self.assertNotIn("BraxMiastoA.Obiekt(tasma, nazwa + \": poręcz ruchoma",metro)
        self.assertIn("RuraUV(pts, .04f, 10, .5f",metro)
        drive=(ROOT/"unity-city/Assets/Claude/Runtime/BraxSchodyRuchomeJazda.cs").read_text()
        self.assertIn("mpb.SetVector(ST",drive)

    def test_all_five_distinct_interior_finish_palettes(self):
        unity=(EDITOR/"Mistrzowie/BraxChampionsInfrastructure.cs").read_text()
        for token in ("var wallColors=new[]{","var floorColors=new[]{","var trimColors=new[]{",
                      "m.SetTextureScale(prop,tiling)","bool signage=","bool skirting=",
                      "bool floor=","bool ceiling=","if(wall)"):
            self.assertIn(token,unity)
        self.assertIn("new Color(.59f,.72f,.64f)",unity)
        self.assertIn("new Color(.73f,.67f,.77f)",unity)

    def test_all_five_original_atlas_sheets_with_unique_destinations(self):
        from PIL import Image
        self.assertEqual(set(DESTINATIONS),{s["id"] for s in METRO})
        with tempfile.TemporaryDirectory() as d:
            root=Path(d)
            src=root/"original_metro_atlas.png"
            Image.new("RGB",(1024,1024),(25,31,38)).save(src)
            paths=render_all(src,root/"out")
            self.assertEqual(len(paths),5)
            self.assertEqual({p.stem for p in paths},
                             {"metro_M1","metro_M2","metro_M3","metro_M4","metro_M5"})
            self.assertEqual(len({hashlib.sha256(p.read_bytes()).hexdigest()
                                  for p in paths}),5)
        text=(ROOT/"unity-tools/mistrzowie/prepare_signage.py").read_text()
        self.assertIn("POCIĄGI NIE KURSUJĄ",text)
        self.assertNotIn("PROMENADA  /  MARINA",text)

    def test_no_pretend_running_train_in_any_station(self):
        gen=(ROOT/"unity-tools/mistrzowie/build_infrastructure.py").read_text()
        self.assertIn("transit['trafficEnabled']=False",gen)
        self.assertIn("transit['trainOperationVerified']=False",gen)
        self.assertIn("metroStations=len(METRO)",gen)
        self.assertIn("metroEntrances=len(ENTRANCE_HOLES)",gen)


if __name__=="__main__":
    unittest.main()
