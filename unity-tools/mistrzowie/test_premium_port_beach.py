"""Source/geometry audits for Miasto Mistrzów premium port and golden beach.
Never claims Unity visuals, moving ships or device runtime as tested.
"""
import json
import math
from collections import Counter
from pathlib import Path
import unittest
from shapely.geometry import box
from port_layout import PIERS, BERTHS, shoreline, beach_height, SHORE_BACK, SEA_LEVEL, validate

ROOT=Path(__file__).resolve().parents[2]
EDITOR=ROOT/"unity-city/Assets/Editor/Mistrzowie"


class PremiumPortBeach(unittest.TestCase):
    def test_compact_owner_vessel_numbers_and_all_individual_berths(self):
        self.assertEqual(len(PIERS),56)
        self.assertEqual(len(BERTHS),38)
        self.assertEqual(Counter(b["kind"] for b in BERTHS),
                         Counter(yacht=15,motorboat=10,jetski=10,cruise_ship=2,ferry=1))
        self.assertTrue(validate(PIERS,BERTHS))
        self.assertEqual(len({b["id"] for b in BERTHS}),38)
        self.assertEqual(len({p["id"] for p in PIERS}),56)
        self.assertTrue(all(b["dock"] and -500<b["z"]<-150 for b in BERTHS))

    def test_one_yacht_spine_and_U_shaped_motorboat_basin(self):
        ids={p["id"] for p in PIERS}
        for name in ("YACHT_MAIN_SPINE","MOTOR_U_WEST","MOTOR_U_EAST",
                     "MOTOR_U_BASE","JETSKI_LOW_MAIN","FERRY_BOARDING_QUAY",
                     "CRUISE_PASSENGER_QUAY"):
            self.assertIn(name,ids)
        self.assertEqual(sum(p["id"]=="YACHT_MAIN_SPINE" for p in PIERS),1)
        self.assertEqual(len([p for p in PIERS if p["id"].startswith("YACHT_") and "_FINGER_" in p["id"]]),18)
        self.assertEqual(len([p for p in PIERS if p["id"].startswith("MOTOR_") and "_FINGER_" in p["id"]]),12)
        self.assertEqual(len([p for p in PIERS if p["id"].startswith("JETSKI_") and "_FINGER_" in p["id"]]),12)

    def test_650m_beach_110_to_125m_midsection(self):
        for x in (250,300,330,350,400,450,500):
            width=SHORE_BACK-shoreline(x)
            self.assertGreaterEqual(width,109.0,(x,width))
            self.assertLessEqual(width,125.0,(x,width))
        self.assertLess(SHORE_BACK-shoreline(50),4)
        self.assertLess(SHORE_BACK-shoreline(700),4)
        self.assertLess(shoreline(300),shoreline(500)-7)
        self.assertLess(beach_height(330,shoreline(330)),SEA_LEVEL)
        self.assertAlmostEqual(beach_height(330,SHORE_BACK),.16,places=5)

    def test_no_old_80_slot_port_and_no_coast_duplicate(self):
        terrain=(ROOT/"unity-tools/mistrzowie/legacy_foundation.py").read_text()
        self.assertIn("piers=[(p['x'],p['z'],p['w'],p['d'],p['role']) for p in PIERS]",terrain)
        self.assertIn("warped_quad('sea'",terrain)
        self.assertIn("warped_quad('foam'",terrain)
        self.assertIn("berths=berths,piers=PIERS",terrain)
        self.assertNotIn("80 reserved berth positions",terrain)
        self.assertNotIn("for row,(z,n,length,width)",terrain)

    def test_original_owners_GLBS_not_fake_scratch_yachts(self):
        harbour=(EDITOR/"BraxChampionsHarbour.cs").read_text()
        for model in ("yacht.glb","bateau_riva_boat.glb","jetski_01.glb","jetski_02.glb"):
            self.assertIn(model,harbour)
        self.assertIn("BraxHarborBeachBuild.Model",harbour)
        self.assertIn("boats",harbour)
        self.assertNotIn('new GameObject("fake cruise ship")',harbour)
        self.assertIn("BraxChampionsHarbourSafety.Generate(root,piers)",harbour)

    def test_ropes_protect_public_piers_without_blocking_boat_access(self):
        s=(EDITOR/"BraxChampionsHarbourSafety.cs").read_text()
        for name in ("YACHT_MAIN_SPINE","MOTOR_U_WEST","MOTOR_U_EAST",
                     "JETSKI_LOW_MAIN","FERRY_BOARDING_QUAY","CRUISE_PASSENGER_QUAY"):
            self.assertIn(name,s)
        for token in ("SplitWithGaps","new[]{-273f,-397f}","new[]{-312f}",
                      "rope.Tube","steel.Tube","BoxCollider","pier.role"):
            self.assertIn(token,s)
        self.assertNotIn("BraxLina",s.split("public static class")[1])

    def test_real_light_golden_sand_polyhaven_and_waves(self):
        f=(EDITOR/"BraxChampionsFoundation.cs").read_text()
        self.assertIn("coast_sand_02",f)
        self.assertIn("damp_sand",f)
        self.assertIn("new Color(1.52f,1.40f,1.12f,1f)",f)
        self.assertIn('materials["foam"]=FoamMaterial()',f)
        self.assertIn('m.role!="sea"&&m.role!="foam"',f)
        self.assertIn("new Color(.12f,.25f,.27f,1f)",f)
        foam=(EDITOR/"BraxChampionsBeachFoam.shader").read_text()
        self.assertIn("_Time.y",foam)
        self.assertIn("Blend SrcAlpha OneMinusSrcAlpha",foam)
        self.assertIn("ZWrite Off",foam)

    def test_original_promenade_untouched_and_safe_ship_access(self):
        self.assertTrue(all(p["x"]>=730 and p["z"]+p["d"]<=-150 for p in PIERS))
        self.assertEqual(sum(b["kind"]=="cruise_ship" for b in BERTHS),2)
        self.assertEqual(sum(b["kind"]=="ferry" for b in BERTHS),1)
        self.assertTrue(all(b["accessX"]<b["x"] for b in BERTHS if b["kind"]=="cruise_ship"))

    def test_generated_manifest_when_available(self):
        path=ROOT/"unity-city/Assets/Source/Mistrzowie/Foundation.json"
        if not path.is_file():
            self.skipTest("Foundation.json is generated only on CI preflight / Unity")
        data=json.loads(path.read_text())
        counts=Counter(b["kind"] for b in data["berths"])
        self.assertEqual(len(data["piers"]),56)
        self.assertEqual(counts,Counter(yacht=15,motorboat=10,jetski=10,cruise_ship=2,ferry=1))
        roles={m["role"] for m in data["meshes"]}
        self.assertTrue({"sand_dry","sand_wet","sea","foam",
                         "quay_wood","quay_jetski","quay_cruise",
                         "quay_ferry","quay_wall"}.issubset(roles))
        self.assertNotIn("quay",roles)
        self.assertGreater(len(data["plants"]),200)


if __name__=="__main__":
    unittest.main()
