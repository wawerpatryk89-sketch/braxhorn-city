"""Source/geometry audits of the Miasto Mistrzów harbour, promenade edge and beach, rebuilt 10.11 for the owner
("ten port ma być lepiej zrobiony, mniejszy ... jachty mają być dobrze ustawione ... skuterki ... przy brzegu zaraz ...
promenada, barierki tam, gdzie trzeba ... plażę poprawić ... wygląda niby mokra"). Never claims Unity visuals or
device runtime as tested."""
import json
from collections import Counter
from pathlib import Path
import unittest
from shapely.geometry import box
from port_layout import (PIERS, BERTHS, PROMENADE_OPENINGS, JETSKI_X0, JETSKI_X1, shoreline, beach_height, hull,
                         SHORE_BACK, SEA_LEVEL, validate)

ROOT=Path(__file__).resolve().parents[2]
EDITOR=ROOT/"unity-city/Assets/Editor/Mistrzowie"


class PremiumPortBeach(unittest.TestCase):
    def test_owner_vessel_numbers_and_individual_berths(self):
        self.assertEqual(len(BERTHS),38)
        self.assertEqual(Counter(b["kind"] for b in BERTHS),
                         Counter(yacht=15,motorboat=10,jetski=10,cruise_ship=2,ferry=1))
        self.assertTrue(validate(PIERS,BERTHS))
        self.assertEqual(len({p["id"] for p in PIERS}),len(PIERS))

    def test_every_boat_lies_alongside_its_own_finger_bow_to_open_water(self):
        piers={p["id"]:p for p in PIERS}
        for b in BERTHS:
            if b["kind"] not in ("yacht","motorboat"):continue
            f=piers[b["dock"]]
            self.assertIn("_FINGER_",b["dock"])
            gap=hull(b).distance(box(f["x"],f["z"],f["x"]+f["w"],f["z"]+f["d"]))
            self.assertTrue(.05<gap<.7,(b["id"],gap))
            # long axis along the finger (x), bow away from the main pontoon
            self.assertIn(b["yaw"],(90.0,270.0))
            self.assertEqual(b["yaw"]==90.0,"_E_" in b["dock"])

    def test_compact_port(self):
        # the old port ran 313 m into the sea (empty cruise quay) and 260 m (yacht spine)
        # (the cruise terminal pier is as long as the owner's real 289 m ship - owner 10.11 14:46 "cienki")
        depth=max(-(p["z"]) for p in PIERS if p["role"]!="quay_cruise")-150
        self.assertLess(depth,120)
        yacht=[p for p in PIERS if p["id"]=="YACHT_MAIN_SPINE"][0]
        # 10.11 12:52 "wszystko w skalę": 15 REAL 40 m yachts need 11.6 m per berth (was 65 m for half-size models)
        self.assertLess(yacht["d"],100)

    def test_jetskis_right_at_the_promenade_quay(self):
        skis=[b for b in BERTHS if b["kind"]=="jetski"]
        self.assertTrue(all(b["z"]>-157 and b["yaw"]==180.0 for b in skis))       # bow to the sea
        self.assertTrue(all(JETSKI_X0<b["x"]<JETSKI_X1 for b in skis))
        self.assertTrue(any(abs(a-866)<.01 for a,_ in PROMENADE_OPENINGS))

    def test_cruise_berths_free_for_the_owner_ships(self):
        cruise=[b for b in BERTHS if b["kind"]=="cruise_ship"]
        self.assertEqual(len(cruise),2)
        self.assertTrue(all(b["length"]>=82 for b in cruise))
        harbour=(EDITOR/"BraxChampionsHarbour.cs").read_text()
        self.assertNotIn('new GameObject("fake cruise ship")',harbour)

    def test_original_owner_glbs_bows_and_no_floating_labels(self):
        harbour=(EDITOR/"BraxChampionsHarbour.cs").read_text()
        for model in ("yacht.glb","bateau_riva_boat.glb","jetski_01.glb","jetski_02.glb"):
            self.assertIn(model,harbour)
        self.assertIn("model.transform.RotateAround(bounds.center,Vector3.up,-90f);",harbour)
        self.assertNotIn("TextMesh",harbour)
        self.assertIn('Letters(signs,"BERTH_"+',harbour)
        self.assertIn("15 yachts",harbour)            # BraxSceneAudit "Port"
        self.assertIn("motorboats",harbour)
        self.assertIn("BraxChampionsHarbourSafety.Generate(root,piers,",harbour)

    def test_railings_where_needed(self):
        s=(EDITOR/"BraxChampionsHarbourSafety.cs").read_text()
        for token in ("GlassRun(","HandrailRun(","openings.OrderBy","quay_ramp_along","quay_cruise","quay_ferry",
                      "1149.85f","50.15f","BraxChampionsMeshBatch.Collider"):
            self.assertIn(token,s)

    def test_beach_one_golden_sand_to_the_water(self):
        f=(EDITOR/"BraxChampionsFoundation.cs").read_text()
        self.assertIn("coast_sand_02",f)
        self.assertNotIn('"sand_wet|damp_sand"',f)
        terrain=(ROOT/"unity-tools/mistrzowie/legacy_foundation.py").read_text()
        self.assertIn("role='sand_wet' if (i+.5)/32>.94 else 'sand_dry'",terrain)
        self.assertIn("(z>=-150 or (x<690 and z>=-161))",terrain)
        for x in (250,300,330,350,400,450,500):
            width=SHORE_BACK-shoreline(x)
            self.assertTrue(60<=width<=80,(x,width))
        self.assertLess(beach_height(330,shoreline(330)),SEA_LEVEL)
        self.assertAlmostEqual(beach_height(330,SHORE_BACK),.16,places=5)
        foam=(EDITOR/"BraxChampionsBeachFoam.shader").read_text()
        self.assertIn("_Time.y",foam)

    def test_beach_club_not_a_desert(self):
        # owner 10.11 10:04 (photo from the city walk): "takie wąskie ... coś tu nie gra" - 120 m of empty sand,
        # one row of dots and a 2.5 m boardwalk ending in open sand
        b=(EDITOR/"BraxChampionsBeachFurnishings.cs").read_text()
        self.assertIn("float deeper=-231f+5.0f*Mathf.Sin(x*.013f+.4f)+2.4f*Mathf.Sin(x*.034f+1.1f);",b)
        self.assertIn("deep=-231.0+5.0*math.sin(x*.013+0.4)+2.4*math.sin(x*.034+1.1)",(ROOT/"unity-tools/mistrzowie/port_layout.py").read_text())
        self.assertIn("BoardwalkHalf=2.0f,EntryHalf=6.0f",b)
        self.assertIn("float end=Shore(xb)+9f;",b)                       # boardwalk reaches the water
        self.assertIn("for(int row=0;row<4;row++)Set(m,col,x,Shore(x)+17f+row*7f,ref n);",b)
        self.assertIn("s.lie=true;s.label=\"LEŻAK\"",b)                   # every lounger: lie down
        self.assertIn("if(Loungers<180||Parasols!=Loungers/2||Seats!=Loungers)",b)
        self.assertIn("SwimBuoys(root);",b)
        self.assertNotIn("GameObject.CreatePrimitive",b)                  # batched, not hundreds of cubes
        self.assertIn('"LIFEGUARD + BEACH CLUB"',(EDITOR/"BraxSceneAudit.cs").read_text())
        self.assertNotIn("Boardwalk(",(EDITOR/"BraxChampionsPromenadeFurniture.cs").read_text())

    def test_thick_palms_along_the_beach(self):
        # owner 10.11 10:16: along the back edge of the beach only the thick palms
        terrain=(ROOT/"unity-tools/mistrzowie/legacy_foundation.py").read_text()
        self.assertIn("for x in range(65,693,15):plant(x,-155.5,'palm',0,'beach_soil')",terrain)
        self.assertIn('BraxMiastoAssety.Model("miasto_palmy","palma"),BraxMiastoAssety.Model("miasto_palmy","palma2")',
                      (EDITOR/"BraxChampionsFoundation.cs").read_text())    # variant 0 = palma (thick trunk)

    def test_dining_islands_between_kiosks(self):
        # owner 10.11 10:28: between the kiosks chairs, eating stations and palms
        import promenade_layout as pl
        self.assertEqual(len(pl.DINING),11)
        self.assertTrue(pl.validate_dining())
        terrain=(ROOT/"unity-tools/mistrzowie/legacy_foundation.py").read_text()
        self.assertIn("for x,z in DINING_BEDS:plant(x,z,'palm',0,'dining_bed')",terrain)
        self.assertIn("+PALM_BED_RECTS+DINING_BED_RECTS",terrain)          # grid lines: the beds really are soil
        f=(EDITOR/"BraxChampionsPromenadeFurniture.cs").read_text()
        for needle in ("static void Island(","BraxChampionsPromenade.Chair(m,f+new Vector3(.74f,0,0),-90f);",
                       "BraxChampionsPromenade.Parasol(m,f+Vector3.up*.76f,1.45f);","standing counters","BraxChampionsPromenade.Leather"):
            self.assertIn(needle,f)
        self.assertIn("d.dining==null || d.dining.Length!=11",(EDITOR/"BraxChampionsFoundation.cs").read_text())

    def test_real_materials_on_seats_and_cloth(self):
        # owner 10.11 10:27: wood is wood, upholstery is fabric / leather, cloth is woven cloth (no flat colour, no plaid)
        p=(EDITOR/"BraxChampionsPromenade.cs").read_text()
        self.assertIn('Canvas=Fabric("prom_parasol_canvas_white","terlenka"',p)
        self.assertIn('Cushion=Fabric("prom_seat_cushion_white","cotton_jersey"',p)
        self.assertIn('BraxChampionsFoundation.LibraryLeather("prom_bar_stool_black_leather"',p)
        b=(EDITOR/"BraxChampionsBeachFurnishings.cs").read_text()
        self.assertNotIn('"fabric_pattern_05"',b)
        self.assertNotIn('Surface("beach_parasol_canvas_white"',b)
        self.assertIn('"terlenka"',b);self.assertIn('"cotton_jersey"',b)

    def test_preview_mirrors_foundation_colours(self):
        # the preview's copy of the foundation materials must show what Unity builds (review 10.11: pink deck, orange sand)
        f=(EDITOR/"BraxChampionsFoundation.cs").read_text()
        prev=(ROOT/"unity-tools/mistrzowie/promenade_preview/PromenadeMain.cs").read_text()
        for unity,mirror in (("new Color(1.59f,1.68f,1.81f,1f)","new Color(1.59f, 1.68f, 1.81f)"),
                             ("new Color(1.30f,1.38f,1.50f,1f)","new Color(1.30f, 1.38f, 1.50f)"),
                             ("new Color(.94f,1.08f,1.18f,1f)","new Color(.94f, 1.08f, 1.18f)"),
                             ("new Color(.93f,.97f,1.15f,1f)","new Color(.93f, .97f, 1.15f)")):
            self.assertIn(unity,f);self.assertIn(mirror,prev)

    def test_generated_manifest_when_available(self):
        path=ROOT/"unity-city/Assets/Source/Mistrzowie/Foundation.json"
        if not path.is_file():
            self.skipTest("Foundation.json is generated only on CI preflight / Unity")
        data=json.loads(path.read_text())
        self.assertEqual(len(data["piers"]),len(PIERS))
        roles={m["role"] for m in data["meshes"]}
        self.assertTrue({"sand_dry","sand_wet","sea","foam","quay_wood","quay_jetski","quay_cruise","quay_ferry",
                         "quay_wall","pontoon_side","prom_paving"}.issubset(roles))
        self.assertGreaterEqual(sum(p["band"]=="promenade_bed" for p in data["plants"]),45)


if __name__=="__main__":
    unittest.main()
