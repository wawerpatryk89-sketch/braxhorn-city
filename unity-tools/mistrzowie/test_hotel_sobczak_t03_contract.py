"""HOTEL SOBCZAK T03 — static contract of the 9.11.2026 rebuild (owner board 'MUROWANY MODERN BRICK').

Owner 19:57: real entrance (it was walk-through glass), no trees in front, real sidewalks, a real reception,
every floor finished with rooms, corridors and lifts; leather lobby sofas. 19:58: a proper (3D) sign.
These checks read the generator source; the measured scene contract lives in BraxSceneAudit.AuditHotelSobczak.
"""
import json
import re
import subprocess
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
TOOLS = ROOT / "unity-tools" / "mistrzowie"
EDITOR = ROOT / "unity-city" / "Assets" / "Editor" / "Mistrzowie"
RUNTIME = ROOT / "unity-city" / "Assets" / "Mistrzowie"


def const(src, name):
    m = re.search(r"\b" + name + r"\s*=\s*(-?[0-9.]+)f?\b", src)
    if not m:
        raise AssertionError("constant not found: " + name)
    return float(m.group(1))


class HotelSobczakT03Contract(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.plan = json.loads((TOOLS / "plan_inventory.json").read_text(encoding="utf-8"))
        cls.spec = json.loads((TOOLS / "hotel_sobczak_t03_spec.json").read_text(encoding="utf-8"))
        cls.gen = (EDITOR / "BraxChampionsHotelSobczakT03.cs").read_text(encoding="utf-8")
        cls.materials = (EDITOR / "BraxChampionsHotelSobczakMaterials.cs").read_text(encoding="utf-8")
        cls.build = (EDITOR / "BraxChampionsBuild.cs").read_text(encoding="utf-8")
        cls.audit = (EDITOR / "BraxSceneAudit.cs").read_text(encoding="utf-8")
        cls.prepare = (TOOLS / "prepare_assets.py").read_text(encoding="utf-8")
        cls.streamer = (RUNTIME / "BraxHotelSobczakFloorStreamer.cs").read_text(encoding="utf-8")
        cls.review = (RUNTIME / "BraxChampionsVisualReview.cs").read_text(encoding="utf-8")
        cls.workflow = (ROOT / ".github" / "workflows" / "mistrzowie-apk.yml").read_text(encoding="utf-8")
        cls.sign_script = (TOOLS / "build_sobczak_sign.py").read_text(encoding="utf-8")

    # ---------------------------------------------------------------- board dimensions
    def test_plot_is_the_owner_board_60x60(self):
        t03 = next(b for b in self.plan["buildings"] if b["id"] == "T03")
        self.assertEqual((t03["x"], t03["z"], t03["w"], t03["d"], t03["height"]), (1030, 710, 60, 60, 110))
        self.assertEqual(const(self.gen, "CentreX"), 1030)
        self.assertEqual(const(self.gen, "CentreZ"), 710)
        self.assertEqual(const(self.gen, "PlotHalf"), 30)
        self.assertEqual(self.spec["plot_m"], {"width": 60, "depth": 60, "x": [1000, 1060], "z": [680, 740]})

    def test_podium_42x38_and_brick_tower_34x32(self):
        self.assertEqual((const(self.gen, "PodiumHalfW"), const(self.gen, "PodiumHalfD")), (21, 19))
        self.assertEqual((const(self.gen, "TowerHalfW"), const(self.gen, "TowerHalfD")), (17, 16))
        self.assertIn('s.brick=P("brick_facade","exterior_wall_cladding_02"', self.materials)
        self.assertIn('s.podiumStone=P("podium_granite","granite_tile_03"', self.materials)

    def test_vertical_schedule_is_110_m(self):
        g, f1, gs, rz = (const(self.gen, n) for n in ("GroundStorey", "FirstStorey", "GuestStorey", "RoofZone"))
        floors = const(self.gen, "GuestFloors")
        self.assertEqual((g, f1, gs, floors, rz), (6.0, 5.0, 3.5, 26, 8.0))
        self.assertAlmostEqual(g + f1 + floors * gs + rz, 110.0)
        self.assertEqual(self.spec["height_m"], 110)

    def test_260_rooms_fill_the_tower_exactly(self):
        self.assertEqual((const(self.gen, "RoomsPerFloor"), const(self.gen, "TotalRooms")), (10, 260))
        room_w, depth = const(self.gen, "RoomW"), const(self.gen, "RoomDepth")
        self.assertAlmostEqual(5 * room_w, 2 * const(self.gen, "TowerHalfW"))          # 5 bays across 34 m
        wall_z = const(self.gen, "RoomWallZ")
        self.assertAlmostEqual(wall_z + .1 + depth, const(self.gen, "TowerHalfD") - .3)  # room reaches the window wall
        self.assertIn("roomsBuilt!=TotalRooms", self.gen)

    def test_corridor_is_2_4_m(self):
        core, room = const(self.gen, "CoreWallZ"), const(self.gen, "RoomWallZ")
        self.assertAlmostEqual((room - .1) - (core + .1), const(self.gen, "CorridorW"))
        self.assertAlmostEqual(const(self.gen, "CorridorW"), 2.4)

    # ---------------------------------------------------------------- owner audit 19:57
    def test_entrance_is_real_automatic_doors_with_collision(self):
        self.assertEqual(self.gen.count('AutoDoors("T03 MAIN ENTRANCE |'), 2)
        self.assertIn("AddComponent<BraxDrzwiAutomatyczne>()", self.gen)
        body = self.gen[self.gen.index("static Transform DoorLeafGlass"):]
        body = body[:body.index("return t;")]
        self.assertRegex(body, r"Box\(M\.clearGlass,[^;]*,true\);")      # glass leaf blocks when closed
        vest = self.gen[self.gen.index("static void Entrance()"):self.gen.index("static void AutoDoors")]
        # owner 10.11 12:48-12:50: fixed vestibule panes black, only the door leaves clear, black portal
        self.assertRegex(vest, r"Box\(M\.blackGlass,sx\*4f-\.01f,sx\*4f\+\.01f,Ffl0,top,z0,z1,true\)")
        self.assertNotIn("M.clearGlass", vest)
        self.assertIn("black door portal", vest)
        self.assertIn("z0-.10f", vest)                                     # leaves never run through the fixed glass

    def test_no_trees_real_sidewalks_kerbs_and_drop_off(self):
        site = self.gen[self.gen.index("static void Site()"):self.gen.index("static void Planter(")]
        code = re.sub(r"//[^\n]*", "", site).lower()                      # comments may say "no trees"
        self.assertNotRegex(code, r"tree|palm|foliage|trunk|crown")
        for needle in ("M.paving", "M.kerb", "M.asphalt", "Wedge(M.paving", "M.whitePaint"):
            self.assertIn(needle, site)
        self.assertIn("PlazaY=.30f", self.gen)
        self.assertEqual(const(self.gen, "Ffl0"), const(self.gen, "PlazaY"))  # plaza flush with the lobby floor

    def test_reception_lobby_and_leather_models(self):
        ground = self.gen[self.gen.index("static void GroundFloor()"):self.gen.index("static void Toilets(")]
        for needle in ("reception", "lobby lounge", "lobby bar", "concierge", "luggage room", "waiting lounge", "back office"):
            self.assertIn(needle, ground)
        self.assertGreaterEqual(ground.count('A+"sofa_02/sofa_02_2k.gltf"'), 4)
        self.assertGreaterEqual(ground.count('A+"modern_arm_chair_01/modern_arm_chair_01_2k.gltf"'), 4)
        for model in ("coffee_table_round_01", "bar_chair_round_01", "mid_century_lounge_chair", "modern_coffee_table_01",
                      "vintage_suitcase", "wall_clock", "fancy_picture_frame_01"):
            self.assertIn(model, self.gen)
        self.assertIn("HasTextures(source)", self.gen)
        self.assertIn("ProceduralSofa", self.gen)                          # leather stand-in if a model is missing

    def test_every_room_is_finished_and_furnished(self):
        room = self.gen[self.gen.index("static void Room(Transform t"):self.gen.index("static void Renumber(")]
        for needle in ("king bed", "nightstands", "TV wall", "desk", "lounge chair", "wardrobe", "vanity", "WC", "shower",
                       "Leaf(true,wall,d0,d1,0,s)", '"ROOM NUMBER "+number', "M.oak", "M.marbleWet"):
            self.assertIn(needle, room)

    def test_corridors_and_core_are_finished(self):
        tmpl = self.gen[self.gen.index("static Transform GuestFloorTemplate()"):self.gen.index("static void Skirting(")]
        for needle in ("M.carpet", "Skirting(", "M.warmLight", "LiftFront(0,h,M.grasscloth)", "CoreBack(0,h,M.grasscloth)",
                       "StairWalls(0,h,false,M.grasscloth)", "WallZ(M.grasscloth,-TowerHalfW+i*RoomW", "Stair(sx,0,h,1)"):
            self.assertIn(needle, tmpl)

    def test_three_lifts_29_stops_pocket_landing_doors(self):
        self.assertEqual((const(self.gen, "Elevators"), const(self.gen, "Stairs")), (3, 2))
        self.assertIn("Stops=GuestFloors+3", self.gen)
        self.assertIn("static readonly float[] LiftX={-2.7f,0f,2.7f};", self.gen)
        self.assertIn("new Vector3(LiftX[l]-LiftDoorHalf*.5f,y+1.1f,-CoreWallZ)", self.gen)   # in the wall plane
        self.assertIn('StopName(int s)=>s==0?"PARTER":s==Stops-1?"TARAS"', self.gen)
        self.assertIn("var c=cab.gameObject.AddComponent<BraxWinda>();", self.gen)

    def test_stairs_are_comfortable_and_reach_the_roof(self):
        riser, tread = const(self.gen, "Riser"), const(self.gen, "Tread")
        self.assertTrue(.60 <= 2 * riser + tread <= .65)
        self.assertIn("Stair(-1f,Level(0),Level(1),2)", self.gen)
        self.assertIn("Stair(-1f,Level(1),Level(2),2)", self.gen)
        roof = self.gen[self.gen.index("static void Roof()"):self.gen.index("static void Lounger(")]
        self.assertIn("guard the open well", roof)
        self.assertIn("LiftFront(t0,t1)", roof)

    def test_moving_parts_are_never_static(self):
        self.assertIn("GameObjectUtility.SetStaticEditorFlags(t.gameObject,0)", self.gen)
        self.assertIn("root.GetComponentsInChildren<BraxDrzwiAutomatyczne>(true))   // moving parts are never static", self.gen)

    # ---------------------------------------------------------------- owner 19:58: proper 3D sign
    def test_sign_is_a_3d_body_mirrored_for_unity(self):
        self.assertIn("HotelSobczakSign.bytes", self.gen)
        self.assertIn("build('SOBCZAK', 'hotel')", self.sign_script)
        self.assertIn("def mirror_for_unity", self.sign_script)
        self.assertIn("python3 unity-tools/mistrzowie/build_sobczak_sign.py", self.workflow)
        self.assertLess(self.workflow.index("build_signs.py"), self.workflow.index("build_sobczak_sign.py"))
        tracked = subprocess.run(["git", "ls-files", "unity-city/Assets/Source/Mistrzowie/HotelSobczakSign.bytes"],
                                 cwd=ROOT, capture_output=True, text=True).stdout.strip()
        self.assertEqual(tracked, "", "the sign geometry is generated in CI, never committed")

    def test_sign_geometry_reads_from_the_front(self):
        import sys
        sys.path.insert(0, str(TOOLS))
        try:
            from build_signs import build
            from build_sobczak_sign import mirror_for_unity
        except Exception as error:  # fonts/shapely missing on a minimal runner
            self.skipTest("sign generator unavailable: %s" % error)
        sign = mirror_for_unity(build("SOBCZAK", "hotel"))
        gold = next(m for m in sign["meshes"] if m["role"] == "gold")
        p, t = gold["positions"], gold["triangles"]
        def normal_z(i):
            a, b, c = (p[3 * t[i + k]:3 * t[i + k] + 3] for k in range(3))
            u = [b[j] - a[j] for j in range(3)]; v = [c[j] - a[j] for j in range(3)]
            return u[0] * v[1] - u[1] * v[0]
        front = sum(1 for i in range(0, len(t), 3) if normal_z(i) > 1e-9)
        self.assertGreater(front, 0)                       # caps still face +Z after the mirror

    # ---------------------------------------------------------------- materials, mobile, audit
    def test_board_materials_are_downloaded_by_the_pipeline(self):
        slugs = re.findall(r'"([a-z0-9_]+)"', self.materials[self.materials.index("RequiredSlugs"):self.materials.index("};", self.materials.index("RequiredSlugs"))])
        for board in self.spec["materials_board"]:
            self.assertIn(board, slugs)
        for slug in slugs:
            self.assertIn("'" + slug + "'", self.prepare, slug)

    def test_mobile_template_streaming_and_scale_once(self):
        self.assertIn('SaveAsPrefabAsset(template.gameObject,Out+"/GuestFloorTemplate.prefab")', self.gen)
        self.assertIn("for(int f=2;f<GuestFloors+2;f++)", self.gen)
        self.assertIn("streamer.metreScale=1f;", self.gen)
        self.assertIn("root.localScale=Vector3.one*K;", self.gen)
        self.assertEqual(self.gen.count("root.localScale="), 1)
        self.assertIn("InverseTransformPoint(world)/metreScale", self.streamer)
        self.assertIn("LODGroup", self.gen)

    def test_build_report_and_scene_audit_describe_the_rebuild(self):
        self.assertNotIn("420 rooms", self.build)
        self.assertIn("260 finished rooms", self.build)
        for needle in ('"podium_granite"', '"brick_facade"', '"10 ROOMS"', '"6.80x10.50"', "BraxWinda", "BraxDrzwiAutomatyczne",
                       '"SOBCZAK HOTEL 3D sign"', "treesInPlot", "h.liftLandingDoors==3*29*2"):
            self.assertIn(needle, self.audit)

    def test_owner_21_frame_review_matches_the_new_layout(self):
        labels = re.search(r"labels=new\[\]\{(.*?)\};", self.review, re.S).group(1)
        names = re.findall(r'"([0-9]{2}-[a-z0-9-]+)"', labels)
        self.assertEqual(len(names), 21)
        self.assertEqual(names[-1], "21-roof-terrace")
        positions = re.search(r"positions=new\[\]\{(.*?)\};", self.review, re.S).group(1)
        targets = re.search(r"targets=new\[\]\{(.*?)\};", self.review, re.S).group(1)
        self.assertEqual(positions.count("HotelWorld("), 21)
        self.assertEqual(targets.count("HotelWorld("), 21)
        self.assertIn('labels[20]=="21-roof-terrace"', self.review)


    # ---------------------------------------------------------------- premium finish (owner 9.11 21:03)
    def test_furniture_is_real_poly_haven_models_pinned_by_md5(self):
        prep = (TOOLS / "prepare_hotel_sobczak_models.py").read_text(encoding="utf-8")
        for slug in ("dining_chair_02", "mid_century_lounge_chair", "Ottoman_01", "side_table_01", "coffee_table_round_01",
                     "modern_ceiling_lamp_01", "fancy_picture_frame_01", "bar_chair_round_01", "desk_lamp_arm_01"):
            self.assertIn("'%s': (" % slug, prep)
            self.assertIn('H+"%s/' % slug, self.gen)
        self.assertNotIn("hanging_picture_frame_01", prep + self.gen)          # branded placeholder poster, never used
        self.assertIn("python3 unity-tools/mistrzowie/prepare_hotel_sobczak_models.py", self.workflow)
        importer = (EDITOR / "BraxChampionsArenaLobbyTextureImport.cs").read_text(encoding="utf-8")
        self.assertIn('HotelFolder="/Source/Mistrzowie/HotelSobczak/"', importer)

    def test_restaurant_chairs_have_four_legs_upholstery_and_lod(self):
        floor1 = self.gen[self.gen.index("static void FirstFloor()"):self.gen.index("static void FinishChairs()")]
        self.assertIn('H+"dining_chair_02/dining_chair_02_2k.gltf"', floor1)
        proto = floor1[floor1.index("chairProto=G("):floor1.index("diningChairs.Clear();")]
        self.assertEqual(proto.count("Box(M.walnutDark,dx-.018f"), 1)          # 4 legs from the dx/dz loop
        self.assertIn("M.cognac", proto)                                        # padded leather seat and back
        self.assertNotIn("Box(M.brass,cx-.02f,cx+.02f", self.gen)               # the old single brass leg is gone
        finish = self.gen[self.gen.index("static void FinishChairs()"):]
        self.assertIn("AddComponent<LODGroup>()", finish)
        self.assertIn("FinishChairs();", self.gen)

    def test_every_surface_uses_poly_haven_maps(self):
        for slug in ("laminate_floor_02", "curly_teddy_natural", "cotton_jersey", "terlenka", "crepe_satin", "floral_jacquard",
                     "smoked_walnut_veneer", "fabric_leather_02", "herringbone_parquet",
                     "long_white_tiles", "anti_skid_tiles"):
            self.assertIn('"%s"' % slug, self.materials)
            self.assertIn("'%s'" % slug, self.prepare)
        self.assertNotIn("W.Mat.Plain(", self.gen + self.materials)
        apartments = (EDITOR / "BraxChampionsApartments.cs").read_text(encoding="utf-8")
        self.assertIn('ceiling=M("apt_ceiling_stucco","white_stucco_02"', apartments)

    def test_room_furniture_is_streamed_per_room_and_shell_stays_on_the_floor(self):
        room = self.gen[self.gen.index("static void Room(Transform t"):self.gen.index("static void Renumber(")]
        self.assertIn('var fg=G(t,"ROOM "+number+" | furniture (drawn when inside)");', room)
        tail = room[room.index('W.Lampa("room "+number+" bath light"'):]
        self.assertIn("In(t,true);", tail)                                   # regression: walls after the rooms stay on the floor
        self.assertIn("AddComponent<BraxWidocznoscWnetrza>()", self.gen)
        self.assertIn('" | furniture (drawn when inside)"', self.gen[self.gen.index("int roomZones=0;"):])

    def test_interiors_get_interior_ambient_not_the_city_sky(self):
        # owner photos 9.11: beige walls x blue city sky ambient read mauve, ceilings dark brown-grey
        light = (EDITOR / "BraxChampionsInteriorLight.cs").read_text(encoding="utf-8")
        self.assertIn("public static void Apply(Material m,float ambient)", light)
        self.assertIn('m.SetColor("_GroundColor",Ground)', light)
        block = self.materials[self.materials.index("foreach(var m in new[]{s.ceiling"):]
        block = block[:block.index("BraxChampionsInteriorLight.Apply(m,")]
        for name in ("s.ceiling", "s.grasscloth", "s.carpet", "s.oak", "s.parquet", "s.rugRoom", "s.rugLobby", "s.walnut"):
            self.assertIn(name, block)
        for exterior in ("s.brick", "s.podiumStone", "s.paving", "s.asphalt", "s.kerb"):
            self.assertNotIn(exterior + ",", block)

    def test_rugs_are_real_rugs_with_a_bound_edge(self):
        self.assertIn("static void Rug(Material field,Material edge", self.gen)
        self.assertEqual(self.gen.count("Rug(M.rug"), 4)                     # lobby, 2 window lounges (loop), waiting, rooms
        self.assertNotIn("Box(M.rugRoom", self.gen)
        self.assertNotIn("Box(M.rugLobby,-10f", self.gen)

    def test_owner_review_9_11_night_soft_bed_white_marble_and_palette(self):
        # owner 23:00-23:02: marble read as beige bathroom tiles, flat brown carpet, tan corridor, box bed, square pillows
        self.assertIn("static void Soft(Material m,Vector3 c,Vector3 size,Quaternion q,float edge,float corner", self.gen)
        room = self.gen[self.gen.index("static void Room(Transform t"):self.gen.index("static void Renumber(")]
        self.assertGreaterEqual(room.count("Soft(M."), 7)                    # divan, mattress, duvet, runner, pillows, cushions
        self.assertNotIn("W.OBox(M.linenWhite", room)                        # no box pillows
        self.assertNotIn("W.OBox(M.velvet", room)
        self.assertIn('LibraryMarbleWhite("t03_white_marble"', self.materials)
        self.assertIn('s.grasscloth=P("linen_wallcovering","terlenka"', self.materials)
        self.assertIn('s.carpet=P("corridor_carpet","floral_jacquard"', self.materials)
        self.assertIn('LibraryMetal("t03_brushed_stainless"', self.materials)
        self.assertIn("('Marble021',['Color','NormalGL','Roughness'])", self.prepare)
        self.assertIn("'laminate_floor_02'", self.prepare)
        foundation = (EDITOR / "BraxChampionsFoundation.cs").read_text(encoding="utf-8")
        self.assertIn('prefix="Marble021_4K-JPG_"', foundation)
        self.assertIn('"lobby centre plant"', self.gen)
        self.assertIn('"roof bar stool, leather"', self.gen)
        # owner 9.11 23:29 travertine -> 10.11 12:48 (beige on the phone): white marble podium; 3D sign on the roof
        self.assertIn('LibraryMarbleWhite("t03_podium_white_marble"', self.materials)
        self.assertIn('s.paint=P("wall_paint_white","white_stucco_02"', self.materials)   # 12:54: no beige walls
        self.assertIn("('Travertine009',['Color','NormalGL','Roughness','AmbientOcclusion'])", self.prepare)
        self.assertIn('(name:"roof front",pos:new Vector3(0,RoofDeck+1.95f,-TowerHalfD+.6f),yaw:180f,scale:1.6f)', self.gen)
        self.assertIn("h.signPass=h.signBodies==4;", self.audit)


if __name__ == "__main__":
    unittest.main()
