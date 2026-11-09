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


def plots():
    out = {}
    for m in re.finditer(r'new Plot\{id="(T\d+)",name="([^"]+)",x=(\d+),z=(\d+),yaw=([\d.]+)f,halfW=([\d.]+)f,'
                         r'halfD=([\d.]+)f,floors=(\d+),style=Style\.(\w+),loggia=([\d.]+)f,walk=(-[\d.]+)f\}', SRC):
        out[m.group(1)] = dict(name=m.group(2), x=int(m.group(3)), z=int(m.group(4)), yaw=float(m.group(5)),
                               halfW=float(m.group(6)), halfD=float(m.group(7)), floors=int(m.group(8)),
                               style=m.group(9), loggia=float(m.group(10)), walk=float(m.group(11)))
    return out


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

    def test_buildings_fit_their_plots_and_face_the_road(self):
        inv = {b["id"]: b for b in json.loads((ROOT / "unity-tools/mistrzowie/plan_inventory.json").read_text(encoding="utf-8"))["buildings"]}
        designs = plots()
        # one building at a time, A-Z, in the agreed order
        self.assertEqual(list(designs), ["T04", "T05", "T06", "T07"])
        self.assertIn('Ids={"T04","T05","T06","T07"}', SRC)
        self.assertEqual([d["style"] for d in designs.values()], ["CurvedWrap", "Loggia", "MarbleGold", "MarbleCorner"])
        half = const("PlotHalf")
        for pid, d in designs.items():
            plot = inv[pid]
            self.assertEqual((d["name"], d["x"], d["z"]), (plot["name"], plot["x"], plot["z"]), pid)
            self.assertEqual((plot["w"], plot["d"]), (60, 60))
            self.assertEqual(half * 2, plot["w"])
            # body + roof overhang (1.4 sides, 2.6 front) inside the 60 x 60 plot
            self.assertLess(d["halfW"] + 1.4, half, pid)
            self.assertLess(d["halfD"] + 2.6, half, pid)
            # local -Z front turned to its road; the path ends on that road's sidewalk (|road-x| = 17)
            if d["yaw"] == 180.0:          # front +Z: road Z650, sidewalk from z 633
                self.assertAlmostEqual(plot["z"] - d["walk"], 633.2, places=3)
            elif d["yaw"] == 90.0:         # front -X: road X800, sidewalk to x 817
                self.assertAlmostEqual(plot["x"] + d["walk"], 816.8, places=3)
            elif d["yaw"] == 270.0:        # front +X: road X1130, sidewalk from x 1113
                self.assertAlmostEqual(plot["x"] - d["walk"], 1113.2, places=3)
            else:
                self.fail(pid + " unexpected yaw")
        # T06 must not face Z330: plot H3 MMA (830..920, 360..400) lies between it and that road
        h3 = inv["H3"]
        self.assertLess(h3["z"] + h3["d"] / 2, inv["T06"]["z"] - 30)
        self.assertEqual(designs["T06"]["yaw"], 90.0)

    def test_dog_leg_stair_fits_the_core_with_headroom(self):
        lane_a0, lane_a1, lane_b0, lane_b1 = const("LaneA0"), const("LaneA1"), const("LaneB0"), const("LaneB1")
        self.assertAlmostEqual(lane_a1 - lane_a0, 1.30, places=3)
        self.assertAlmostEqual(lane_b1 - lane_b0, 1.30, places=3)
        z0 = const("StairZ0")
        back = min(d["halfD"] for d in plots().values()) - .30
        for f in range(max(d["floors"] for d in plots().values()) - 1):
            n = risers(f)
            r = (level(f + 1) - level(f)) / n
            self.assertTrue(.15 <= r <= .175 + 1e-6, (f, r))
            flight = n // 2 * const("Tread")
            self.assertGreaterEqual(back - (z0 + flight), 1.20, f)   # half landing depth
            # flight above flight: one full storey of headroom
            self.assertGreaterEqual(level(f + 1) - level(f) - .20, 2.0)

    def test_finished_and_empty_interiors(self):
        for finish in ('"laminate_floor_02"', '"marble_01"', '"terlenka"', '"smoked_walnut_veneer"', '"white_stucco_02"', "LibraryMarbleWhite("):
            self.assertIn(finish, SRC)
        for part in ("static float Apartment(", "static void Stair(", "WallX(grasscloth,LiftZ", "Leaf(true,zs",
                     "Leaf(false,X(uHall)", "Ceiling(Lo(X(lu),X(w)),Hi(X(lu),X(w)),az0,az1,cy)", "W.Lampa(\"corridor light floor \""):
            self.assertIn(part, SRC)
        # Owner: "Bez wyposażenia" - no furniture, kitchens or bathroom fittings.
        for banned in ("PlaceLobbyModel", "sofa", "bed_", "kitchen_unit", "toilet", "BraxMeble", "Siedzisko("):
            self.assertNotIn(banned, SRC)
        # four apartments per floor, ground + 3
        self.assertEqual(SRC.count("Apartment(f,") + SRC.count("Apartment(0,"), 6)
        self.assertEqual([d["floors"] for d in plots().values()], [4, 4, 5, 4])

    def test_accessible_with_collisions(self):
        self.assertIn("W.Rampa(", SRC)                          # stairs and entrance ramp walkable
        self.assertIn("Wedge(deck", SRC)                        # light granite ramp (black granite read as a hole)
        self.assertIn("sliding leaf open by half", SRC)        # entrance and balcony doors open
        self.assertIn("panes+=Ribbon(poly,.06f,deckTop,deckTop+Balustrade,.012f,clearGlass,true,Outer)", SRC)
        self.assertIn("top floor: no flight up -> guard the stairwell", SRC)

    def test_owner_9_11_evening_fixes(self):
        # owner photos/videos 9.11 22:15-22:31: mauve interiors, flickering corner, black entrance, "0.4", pop-in
        light = (EDITOR / "BraxChampionsInteriorLight.cs").read_text(encoding="utf-8")
        self.assertIn('m.SetColor("_GroundColor",Ground)', light)
        self.assertIn("BraxChampionsInteriorLight.Apply(ceiling,BraxChampionsInteriorLight.Ceilings)", SRC)
        self.assertIn("foreach(var m in new[]{render,panel,granite,timber,deck})BraxChampionsInteriorLight.Facade(m);", SRC)
        self.assertIn("static void Skirting(bool alongX,float plane,float t,float a,float b,float y0)", SRC)
        self.assertGreaterEqual(SRC.count("Skirting("), 5)
        self.assertIn('string N()=>(f*4+(++no)).ToString("00");', SRC)
        self.assertNotIn('f+"."+(++no)', SRC)
        self.assertIn("Box(pier,x-.41f,x+.41f,Ffl0,CeilingOf(0),-HalfD-.05f,-HalfD+.85f,true);", SRC)
        self.assertIn("Box(deck,-HalfW,HalfW,SiteY,Ffl0,-HalfD-2.6f,-HalfD,true);", SRC)
        self.assertNotIn("Box(granite,-HalfW,HalfW,SiteY,Ffl0", SRC)
        self.assertIn("interiorLod.SetLODs(new[]{new LOD(.03f,interiorRenderers)", SRC)
        self.assertIn('var vis=lamps.gameObject.AddComponent<BraxWidocznoscWnetrza>();', SRC)
        self.assertIn("static void Lobby(float y0,float zFrontIn)", SRC)
        self.assertIn('stoneFloor=BraxChampionsFoundation.LibraryMarbleWhite("apt_white_marble_floor"', SRC)
        self.assertIn('paint=M("apt_wall_white","white_stucco_02"', SRC)          # owner 23:01: same beige everywhere
        self.assertNotIn('"beige_wall_001"', SRC)
        self.assertNotIn('"oak_wood_planks"', SRC)
        self.assertNotIn('"terrazzo_tiles"', SRC)
        self.assertTrue((ROOT / "unity-tools/mistrzowie/apartments_preview/zfight.py").exists())

    def test_k_once_lod_and_integration(self):
        self.assertIn("root.localScale=Vector3.one*K;", SRC)
        self.assertIn("l.range*=K;", SRC)
        self.assertIn("o.transform.localScale=Vector3.one;", SRC)   # palms under the K root
        self.assertIn("new LOD(.02f,detailRenderers)", SRC)   # 10.11: details no longer jump in at 6%
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



class ApartmentT05Loggias(unittest.TestCase):
    """Owner photo 2: white frame grid, grey panels, recessed loggias, two timber strips."""

    def test_loggia_geometry(self):
        t05 = plots()["T05"]
        self.assertEqual(t05["loggia"], 2.0)                    # recessed balcony depth
        self.assertIn("static int FrontAndRoofT05()", SRC)
        self.assertIn("float zfL=zf+s*livingRecess", SRC)       # living façade behind the loggia
        self.assertIn("Apartment(f,-HalfW+.3f,-.1f,zFrontIn,zc,true,N(),lights,Loggia)", SRC)
        self.assertIn("Box(clearGlass,la,lb,y0,y0+Balustrade,-HalfD+.05f,-HalfD+.062f,true)", SRC)
        self.assertIn("soffit downlights", SRC)
        # loggia inner edge = face of the living-room partition (lu = round(.45 w, 1))
        w = (t05["halfW"] - .3) - .1
        lu = round(.45 * w * 10) / 10
        self.assertAlmostEqual(-(t05["halfW"] - .3) + lu - .06, -9.26, places=3)
        self.assertIn("const float xi=9.26f;", SRC)

    def test_frames_panels_timber_roof(self):
        self.assertIn('panel=M("apt_sandstone_panel","rectangular_facade_tiles"', SRC)   # owner 23:28: less grey
        self.assertIn("Seg.Panel", SRC)
        self.assertIn("two vertical timber strips", SRC)
        self.assertEqual(SRC.count("for(int i=0;i<20;i++){float x=a+.075f+i*.15f;"), 1)
        self.assertIn(".90 m white parapet with black coping", SRC)
        self.assertIn("white frame grid", SRC)


class ApartmentT06MarbleGold(unittest.TestCase):
    """Owner photo 3: black marble, gold curved balconies with LED, central glass core, ground + 4."""

    def test_marble_gold_glass_core(self):
        t06 = plots()["T06"]
        self.assertEqual((t06["floors"], t06["style"]), (5, "MarbleGold"))
        self.assertIn("static int FrontAndRoofT06()", SRC)
        self.assertIn("skin=marble?marbleBlack:render;pier=marble?marbleBlack:timber;edge=marble?gold:render;", SRC)
        self.assertIn("static void EntranceCanopy()", SRC)                          # owner 23:28: "może jakiś daszek"
        self.assertIn("EntranceCanopy();", SRC)
        self.assertIn("Prism(poly,deckTop-.30f,deckTop,deck,gold,timber,true);", SRC)       # gold curved balconies, teak soffit
        self.assertIn("glass crown of the core above the roof, gold cap", SRC)
        self.assertIn("if(f==0||style==Style.MarbleGold)", SRC)                            # core lounge every floor
        self.assertIn("front parapet stops at the core", SRC)
        # five storeys: roof at 17.10 m, still inside the 44 m interior-visibility zone
        self.assertAlmostEqual(level(5), 17.10, places=3)


class ApartmentT07MarbleCorner(unittest.TestCase):
    """Owner photo 5 (+ details of 4): white body, black marble corner, timber slats, wood soffits."""

    def test_marble_corner_and_slats(self):
        t07 = plots()["T07"]
        self.assertEqual((t07["floors"], t07["style"], t07["yaw"]), (4, "MarbleCorner", 270.0))
        self.assertIn("static int FrontAndRoofT07()", SRC)
        self.assertIn("black marble corner volume: 30 cm proud frame", SRC)
        self.assertIn("new Part(m0,10.2f,Seg.Marble)", SRC)
        # marble corner starts at the right living partition (X(lu) = 16.7 - 7.5)
        w = (t07["halfW"] - .3) - .1
        self.assertAlmostEqual((t07["halfW"] - .3) - round(.45 * w * 10) / 10, 9.2, places=3)
        self.assertIn("const float m0=9.2f;", SRC)
        self.assertIn("for(int i=0;i<50;i++){float x=.975f+i*.15f;", SRC)          # 50 slats @ 150 mm
        self.assertIn("Prism(poly,deckTop-.25f,deckTop,deck,render,timber,true);", SRC)  # timber soffits


if __name__ == "__main__":
    unittest.main()
