"""Owner 10.11 14:56-14:57 on the cruise terminal pier: "pomost też z jakiejś tekstury, zrób coś tak porządnie ... żeby
wyglądał zajebiście ... a nie tylko beton wylany" / "most wykończ". The fixed passenger quays (cruise + ferry) are
finished like the promenade: two stones cut edge to edge in one deck, palm rows in kerbed beds, benches facing the
ships, the promenade lamps, a granite-clad edge beam and a harbour light at the head of the cruise pier."""
import math
import unittest
from pathlib import Path

from port_layout import (PIERS, QUAY_APRONS, QUAY_BEDS, QUAY_BED_RECTS, QUAY_FURNITURE, QUAY_BED, QUAY_BAND, QUAY_BED_SIZES,
                         QUAY_APRON_CRUISE, QUAY_APRON_FERRY)

ROOT = Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"
TOOLS = ROOT / "unity-tools/mistrzowie"


def quay(role):
    return next(p for p in PIERS if p["role"] == role)


def inside(x, z, r, pad=0.0):
    return r[0] - pad <= x <= r[0] + r[2] + pad and r[1] - pad <= z <= r[1] + r[3] + pad


class PremiumQuayLayout(unittest.TestCase):
    def test_berth_aprons_run_the_full_length_of_every_berth_face(self):
        c, f = quay("quay_cruise"), quay("quay_ferry")
        self.assertIn((c["x"], c["z"], QUAY_APRON_CRUISE, c["d"]), QUAY_APRONS)
        self.assertIn((c["x"] + c["w"] - QUAY_APRON_CRUISE, c["z"], QUAY_APRON_CRUISE, c["d"]), QUAY_APRONS)
        self.assertIn((f["x"] + f["w"] - QUAY_APRON_FERRY, f["z"], QUAY_APRON_FERRY, f["d"]), QUAY_APRONS)
        bands = [a for a in QUAY_APRONS if abs(a[3] - QUAY_BAND) < 1e-6]
        self.assertGreaterEqual(len(bands), 20)
        # the dark granite pieces never overlap each other (each is its own piece of the one deck)
        for i, a in enumerate(QUAY_APRONS):
            for b in QUAY_APRONS[i + 1:]:
                ox = min(a[0] + a[2], b[0] + b[2]) - max(a[0], b[0])
                oz = min(a[1] + a[3], b[1] + b[3]) - max(a[1], b[1])
                self.assertFalse(ox > 1e-6 and oz > 1e-6, (a, b))

    def test_every_bed_gets_benches_and_lamps_clear_of_each_other(self):
        benches = [(x, z, k) for k, x, z in QUAY_FURNITURE if k.startswith("quay_bench_")]
        lamps = [(x, z) for k, x, z in QUAY_FURNITURE if k == "lamp"]
        self.assertEqual(len(benches), 2 * len(QUAY_BEDS))
        self.assertGreaterEqual(len(QUAY_BEDS), 25)
        for x, z, _ in benches:
            for r in QUAY_BED_RECTS:
                self.assertFalse(inside(x, z, r, 1.6 + .14), (x, z))    # bench + bin keep 1.6 m off the kerbs
            for lx, lz in lamps:
                self.assertGreater(math.hypot(x - lx, z - lz), 3.0)
        for lx, lz in lamps:
            for r in QUAY_BED_RECTS:
                self.assertFalse(inside(lx, lz, r, .5), (lx, lz))
        # palms at least 10 m apart (the foundation's planting rule) - the two rows are 10.4 m apart
        for i, (x, z) in enumerate(QUAY_BEDS):
            for x2, z2 in QUAY_BEDS[i + 1:]:
                self.assertGreater(math.hypot(x - x2, z - z2), 10.0)

    def test_beds_are_generous_and_inside_the_walk(self):
        # owner 10.11 15:48: "palmy się ledwo mieszczą w tej trawie albo wystają poza"
        c, f = quay("quay_cruise"), quay("quay_ferry")
        for (x, z), b, r in zip(QUAY_BEDS, QUAY_BED_SIZES, QUAY_BED_RECTS):
            self.assertGreaterEqual(b, 3.6)
            p = c if c["x"] < x < c["x"] + c["w"] else f
            apron = QUAY_APRON_CRUISE if p is c else 0.0
            self.assertGreater(r[0] - .14, p["x"] + apron + .3)                       # kerb clear of the apron / railing
            self.assertLess(r[0] + r[2] + .14, p["x"] + p["w"] - (QUAY_APRON_CRUISE if p is c else QUAY_APRON_FERRY) - .3)
        self.assertEqual(QUAY_BED, 4.4)

    def test_benches_face_their_ship_and_the_walk_stays_clear(self):
        c = quay("quay_cruise")
        cx = c["x"] + c["w"] / 2
        for k, x, z in QUAY_FURNITURE:
            if c["x"] < x < c["x"] + c["w"] and c["z"] < z < c["z"] + c["d"]:
                self.assertGreater(abs(x - cx), 3.4, (k, x, z))         # 6.9 m clear walk on the centre line
                if k.startswith("quay_bench_"):
                    self.assertEqual(k, "quay_bench_east" if x > cx else "quay_bench_west")
                # everything stands on the passenger walk, never on the working apron
                self.assertTrue(c["x"] + QUAY_APRON_CRUISE + .5 < x < c["x"] + c["w"] - QUAY_APRON_CRUISE - .5)
        f = quay("quay_ferry")
        for k, x, z in QUAY_FURNITURE:
            if f["x"] < x < f["x"] + f["w"] and f["z"] < z < f["z"] + f["d"] and k.startswith("quay_bench_"):
                self.assertEqual(k, "quay_bench_east")                    # the ferry lies east


class PremiumQuaySources(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.found = (ED / "BraxChampionsFoundation.cs").read_text(encoding="utf-8")
        cls.harbour = (ED / "BraxChampionsHarbour.cs").read_text(encoding="utf-8")
        cls.furn = (ED / "BraxChampionsPromenadeFurniture.cs").read_text(encoding="utf-8")
        cls.legacy = (TOOLS / "legacy_foundation.py").read_text(encoding="utf-8")
        cls.preview = (TOOLS / "promenade_preview/PromenadeMain.cs").read_text(encoding="utf-8")

    def test_deck_is_promenade_stone_and_dark_granite(self):
        self.assertIn('maps["quay_cruise"]=maps["prom_paving"];', self.found)
        self.assertIn('maps["quay_ferry"]=maps["prom_paving"];', self.found)
        self.assertIn('maps["quay_apron"]=new MapSet{', self.found)
        self.assertIn('materials["quay_apron"].SetColor("_Color",new Color(1.15f,1.15f,1.15f,1f));', self.found)
        self.assertNotIn('materials["quay_cruise"].SetColor', self.found)   # would tint the promenade (shared material)
        self.assertIn('case "prom_paving": case "quay_cruise": case "quay_ferry":', self.preview)
        self.assertIn('case "quay_apron": return new Material { name = T("fnd_quay_apron", "granite_tile", 2.4f), color = new Color(1.15f, 1.15f, 1.15f)', self.preview)

    def test_beds_and_aprons_are_cut_from_the_deck(self):
        beds = self.legacy.index("if any(inside((x,z),r) for r in QUAY_BED_RECTS):return 'grass'")
        aprons = self.legacy.index("if any(inside((x,z),r) for r in QUAY_APRONS):return 'quay_apron'")
        piers = self.legacy.index(" for a,b,w,d,role in piers:\n  if inside((x,z),(a,b,w,d)):return role")
        self.assertLess(beds, aprons)
        self.assertLess(aprons, piers)
        self.assertIn("+QUAY_APRONS+QUAY_BED_RECTS\n", self.legacy)               # grid lines on every cut
        self.assertIn("if role in ('quay_cruise','quay_ferry','quay_apron'):\n  return .16", self.legacy)
        # no quay wall round a bed: the bed cells count as quay when outer walls are traced
        self.assertIn("if not role.startswith('quay_') and not any(inside(((a+c)/2,(b+d)/2),r) for r in QUAY_BED_RECTS):continue", self.legacy)
        self.assertIn("for x,z in QUAY_BEDS:plant(x,z,'palm',0,'quay_bed')", self.legacy)
        self.assertIn("DESIGNED_BEDS=('promenade_bed','dining_bed','quay_bed')", self.legacy)
        self.assertIn("zip(PALM_BEDS+QUAY_BEDS,PALM_BED_RECTS+QUAY_BED_RECTS)", self.legacy)
        self.assertIn("FURNITURE+QUAY_FURNITURE", self.legacy)

    def test_furniture_edge_and_harbour_light(self):
        self.assertIn('else if(f.kind=="quay_bench_east")QuayBench(m,colliders,f.x,f.z,1f);', self.furn)
        self.assertIn('else if(f.kind=="quay_bench_west")QuayBench(m,colliders,f.x,f.z,-1f);', self.furn)
        self.assertNotIn("Lamp(m,new Vector3(cx,QuayY,z),7f,90f,true)", self.harbour)   # stood inside the shelter
        self.assertIn('SourceMaterial("harbour_quay_granite_facing","granite_tile_03",2.4f,new Color(1.0f,1.10f,1.22f))', self.harbour)
        self.assertNotIn('"harbour_quay_concrete_edge"', self.harbour)
        self.assertIn("HarbourLight(harbour,m,edge,new Vector3(cx,QuayY,z0+5.5f));", self.harbour)
        self.assertIn("granite_tile_03", (TOOLS / "prepare_assets.py").read_text(encoding="utf-8"))

    def test_chain_posts_between_apron_and_walk_match_the_layout(self):
        # owner 10.11 15:31 (barriers: "Fartuch od alei")
        self.assertIn("ApronFence(m,cruise,x0,x1,z0,z1);", self.harbour)
        self.assertIn(f"public const float ApronCruise={QUAY_APRON_CRUISE}f,ApronFerry={QUAY_APRON_FERRY}f,QuayStep=24f;", self.harbour)
        self.assertIn("float firstLamp=cruise?z1-40f+QuayStep*.5f:z1-20f+QuayStep*.5f;", self.harbour)
        src = (TOOLS / "port_layout.py").read_text(encoding="utf-8")
        self.assertIn("first=z1-40.0", src)
        self.assertIn("first=z1-20.0", src)
        self.assertIn("QUAY_BED,QUAY_STEP=4.4,24.0", src)
        self.assertIn("first+QUAY_STEP/2-QUAY_STEP*k", src)


if __name__ == "__main__":
    unittest.main()
