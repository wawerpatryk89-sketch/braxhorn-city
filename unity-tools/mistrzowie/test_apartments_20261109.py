#!/usr/bin/env python3
"""Owner 9.11: apartment buildings T04-T07, one at a time, finished A-Z, empty.

Static contract for the generator (Unity is not available here): real-size
dimensions from norms, plot fit, stair geometry, finished-but-empty interiors,
integration through Optional and the distance LOD of exterior details.
"""
from pathlib import Path
import json
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]
EDITOR = ROOT / "unity-city/Assets/Editor/Mistrzowie"
SRC = (EDITOR / "BraxChampionsApartments.cs").read_text(encoding="utf-8")


def const(name):
    m = re.search(r"\b" + name + r"=(-?[\d.]+)f?\b", SRC)
    if not m:
        raise AssertionError("constant missing: " + name)
    return float(m.group(1))


def level(f):
    ground, storey, ffl0 = const("GroundStorey"), const("Storey"), const("Ffl0")
    return ffl0 if f <= 0 else ffl0 + ground + (f - 1) * storey


def risers(f):
    n = round((level(f + 1) - level(f)) / const("Riser"))
    return n + 1 if n % 2 else n


class Apartments20261109(unittest.TestCase):
    def test_real_dimensions_from_norms(self):
        self.assertEqual(const("Storey"), 3.15)            # 2.80 clear + slab + finish
        self.assertEqual(const("GroundStorey"), 4.20)
        self.assertEqual(const("Slab"), .30)
        self.assertEqual(const("Balustrade"), 1.10)        # WT 2002 par. 298
        riser, tread = const("Riser"), const("Tread")
        self.assertTrue(.60 <= 2 * riser + tread <= .65)   # step rule, WT par. 68
        self.assertEqual((const("DoorW"), const("DoorH")), (.90, 2.10))
        clear = const("Storey") - const("Slab") - .012 - .01
        self.assertGreaterEqual(clear, 2.80)

    def test_building_fits_its_plot_and_faces_the_road(self):
        inv = json.loads((ROOT / "unity-tools/mistrzowie/plan_inventory.json").read_text(encoding="utf-8"))
        t04 = next(b for b in inv["buildings"] if b["id"] == "T04")
        self.assertIn('new Plot{id="T04",name="APARTAMENTY 1",x=870,z=570,yaw=180f}', SRC)
        self.assertEqual((t04["x"], t04["z"], t04["w"], t04["d"]), (870, 570, 60, 60))
        half = const("PlotHalf")
        self.assertEqual(half * 2, t04["w"])
        # body + roof overhang (1.4 sides, 2.6 front) inside the 60 x 60 plot
        self.assertLess(const("HalfW") + 1.4, half)
        self.assertLess(const("HalfD") + 2.6, half)
        # yaw 180: local -Z front -> world +Z, the road Z650 side (plot edge 600, sidewalk 633)
        self.assertIn("-63.2f", SRC)
        self.assertEqual(t04["z"] + 63.2, 633.2)

    def test_dog_leg_stair_fits_the_core_with_headroom(self):
        lane_a0, lane_a1, lane_b0, lane_b1 = const("LaneA0"), const("LaneA1"), const("LaneB0"), const("LaneB1")
        self.assertAlmostEqual(lane_a1 - lane_a0, 1.30, places=3)
        self.assertAlmostEqual(lane_b1 - lane_b0, 1.30, places=3)
        z0, back = const("StairZ0"), 8.5 - .30
        for f in range(const_int("Floors") - 1):
            n = risers(f)
            r = (level(f + 1) - level(f)) / n
            self.assertTrue(.15 <= r <= .175 + 1e-6, (f, r))
            flight = n // 2 * const("Tread")
            self.assertGreaterEqual(back - (z0 + flight), 1.20, f)   # half landing depth
            # flight above flight: one full storey of headroom
            self.assertGreaterEqual(level(f + 1) - level(f) - .20, 2.0)

    def test_finished_and_empty_interiors(self):
        for finish in ('"oak_wood_planks"', '"tiled_floor_001"', '"marble_01"', '"terrazzo_tiles"', '"beige_wall_001"'):
            self.assertIn(finish, SRC)
        for part in ("static float Apartment(", "static void Stair(", "WallX(paint,LiftZ", "Leaf(true,zs",
                     "Leaf(false,X(uHall)", "Ceiling(x0,x1,az0,az1,cy)", "W.Lampa(\"corridor light floor \""):
            self.assertIn(part, SRC)
        # Owner: "Bez wyposażenia" - no furniture, kitchens or bathroom fittings.
        for banned in ("PlaceLobbyModel", "sofa", "bed_", "kitchen_unit", "toilet", "BraxMeble", "Siedzisko("):
            self.assertNotIn(banned, SRC)
        # four apartments per floor, ground + 3
        self.assertEqual(SRC.count("Apartment(f,") + SRC.count("Apartment(0,"), 6)
        self.assertEqual(const_int("Floors"), 4)

    def test_accessible_with_collisions(self):
        self.assertIn("W.Rampa(", SRC)                          # stairs and entrance ramp walkable
        self.assertIn("Wedge(granite", SRC)
        self.assertIn("sliding leaf open by half", SRC)        # entrance and balcony doors open
        self.assertIn("panes+=Ribbon(poly,.06f,deckTop,deckTop+Balustrade,.012f,clearGlass,true,Outer)", SRC)
        self.assertIn("top floor: no flight up -> guard the stairwell", SRC)

    def test_k_once_lod_and_integration(self):
        self.assertIn("root.localScale=Vector3.one*K;", SRC)
        self.assertIn("l.range*=K;", SRC)
        self.assertIn("o.transform.localScale=Vector3.one;", SRC)   # palms under the K root
        self.assertIn("new LOD(.06f,detailRenderers)", SRC)
        self.assertIn("BraxWidocznoscWnetrza", SRC)
        self.assertIn("Physics.Raycast(", SRC)
        self.assertNotIn("W.Napis(plot.name", SRC)                 # no flat façade sign (ZASADA-SZYLDY-3D)
        for slug in re.findall(r'M\("apt_[a-z_]+","([a-z_0-9]+)"', SRC):
            self.assertFalse(slug.startswith("sobczak"))
        self.assertFalse(any(i.startswith("sobczak") for i in re.findall(r'M\("([a-z_]+)"', SRC)))
        buildings = (EDITOR / "BraxChampionsBuildings.cs").read_text(encoding="utf-8")
        self.assertRegex(buildings, r'Optional\("apartamentowiec "\+apt,\(\)=>\s*\{\s*BraxChampionsApartments.Generate\(apt,materials,Out\);')
        self.assertIn("d.buildings.Length+(casinoGenerated?1:0)+apartmentBuildings", buildings)
        meta = (EDITOR / "BraxChampionsApartments.cs.meta").read_text(encoding="utf-8")
        guid = re.search(r"guid: ([0-9a-f]{32})", meta).group(1)
        hits = [p for p in (ROOT / "unity-city/Assets").rglob("*.meta") if guid in p.read_text(encoding="utf-8", errors="ignore")]
        self.assertEqual(len(hits), 1)


def const_int(name):
    return int(re.search(r"\b" + name + r"=(\d+);", SRC).group(1))


if __name__ == "__main__":
    unittest.main()
