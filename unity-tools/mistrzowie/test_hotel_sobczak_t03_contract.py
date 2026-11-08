import json
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
TOOLS = ROOT / "unity-tools" / "mistrzowie"
EDITOR = ROOT / "unity-city" / "Assets" / "Editor" / "Mistrzowie"

class HotelSobczakT03Contract(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.plan = json.loads((TOOLS / "plan_inventory.json").read_text(encoding="utf-8"))
        cls.spec = json.loads((TOOLS / "hotel_sobczak_t03_spec.json").read_text(encoding="utf-8"))
        cls.generator = (EDITOR / "BraxChampionsHotelSobczakT03.cs").read_text(encoding="utf-8")
        cls.materials = (EDITOR / "BraxChampionsHotelSobczakMaterials.cs").read_text(encoding="utf-8")
        cls.buildings = (EDITOR / "BraxChampionsBuildings.cs").read_text(encoding="utf-8")
        cls.build = (EDITOR / "BraxChampionsBuild.cs").read_text(encoding="utf-8")
        cls.prepare = (TOOLS / "prepare_assets.py").read_text(encoding="utf-8")
        cls.streamer = (ROOT / "unity-city" / "Assets" / "Mistrzowie" / "BraxHotelSobczakFloorStreamer.cs").read_text(encoding="utf-8")
        cls.visual_review = (ROOT / "unity-city" / "Assets" / "Mistrzowie" / "BraxChampionsVisualReview.cs").read_text(encoding="utf-8")

    def test_plot_exact(self):
        t03 = next(b for b in self.plan["buildings"] if b["id"] == "T03")
        self.assertEqual(t03["name"], "HOTEL SOBCZAK / 5*")
        self.assertEqual((t03["x"], t03["z"], t03["w"], t03["d"], t03["height"]), (1030, 710, 64, 42, 130))
        self.assertEqual(self.spec["bounds_m"], {"x": [998, 1062], "z": [689, 731]})
        self.assertEqual(self.spec["site_reserve_m"], {"width": 86, "depth": 76, "x": [987, 1073], "z": [672, 748]})

    def test_vertical_schedule_exact(self):
        self.assertEqual(self.spec["physical_levels"], 34)
        floors = self.spec["floors"]
        self.assertEqual(floors[0]["y"], [-7.2, -3.6])
        self.assertEqual(floors[1]["y"], [-3.6, 0])
        self.assertEqual(floors[2]["y"], [0, 6])
        self.assertEqual(floors[3]["y"], [6, 9.6])
        self.assertEqual(floors[4]["count"], 30)
        self.assertEqual(floors[4]["y"], [9.6, 117.6])
        self.assertEqual(floors[5]["y"], [117.6, 130])
        self.assertIn("Height=130f", self.generator)
        self.assertIn("GuestTop=117.6f", self.generator)
        self.assertIn("RoofTop=130f", self.generator)
        self.assertIn("BuildStairInterval(s,x,stairZ,GuestBase+(GuestFloors-1)*3.6f,GuestTop", self.generator)

    def test_room_math_and_layout(self):
        rooms = self.spec["rooms"]
        self.assertEqual(rooms["floors"] * rooms["per_floor"], 420)
        self.assertEqual(rooms["layout"], "7_south+7_north")
        self.assertEqual(rooms["bay_m"], {"width": 6, "depth": 19.4})
        self.assertEqual(rooms["sleeping_zone_m"], {"width": 3.5, "depth": 7})
        self.assertEqual(rooms["corridor_m"], 2.4)
        self.assertIn("GuestFloors=30,RoomsPerFloor=14,TotalRooms=420", self.generator)
        self.assertIn("if(roomsBuilt!=TotalRooms)", self.generator)
        self.assertIn('Group(p,"ROOM "+number+" | bay 6.00x19.40 | sleep module 3.50x7.00")', self.generator)

    def test_premium_furniture_is_not_single_block_proxy(self):
        for token in [
            "PremiumSofa(", "PremiumCoffeeTable(", "DiningChair(", "BarStool(",
            "PremiumDesk(", "Restaurant brass pedestal", "Dining chair brass leg",
            "Cafe stool leather back", "Room side table brass pedestal",
            "SPA treatment bed leg", "SPA lounger brass base"
        ]:
            self.assertIn(token, self.generator)
        self.assertNotIn('"Lobby leather sofa",M.leather', self.generator)
        self.assertNotIn('"Cafe brass stool",M.brass', self.generator)

    def test_public_area_math(self):
        expected = {
            "reception": 60, "lobby": 350, "bar_cafe": 180, "wc_m": 40, "wc_f": 40,
            "baggage": 50, "concierge": 40, "restaurant": 250, "kitchen": 180,
            "spa": 350, "fitness": 250,
        }
        self.assertEqual(self.spec["public_areas_m2"], expected)
        for token in ["RECEPCJA 60m2", "LOBBY 350m2", "BAR CAFE LOUNGE 180m2",
                      "TOALETA M 40m2", "TOALETA K 40m2", "BAGAZ 50m2",
                      "CONCIERGE 40m2", "RESTAURACJA 250m2", "KUCHNIA 180m2",
                      "SPA 350m2", "FITNESS 250m2"]:
            self.assertIn(token, self.generator)

    def test_site_has_real_greenery_and_warm_bollards(self):
        self.assertIn("ornamental_foliage", self.materials)
        self.assertIn("T03 premium topiary shrub", self.generator)
        self.assertIn("T03 ornamental tree trunk", self.generator)
        self.assertIn("T03 ornamental tree crown", self.generator)
        self.assertIn("T03 warm entrance bollard emissive cap", self.generator)
        self.assertNotIn('premium shrub",M.stucco', self.generator)

    def test_parking_exact(self):
        p = self.spec["parking"]
        self.assertEqual((p["B1"], p["B2"], p["total"]), (40, 40, 80))
        self.assertEqual(p["space_m"], [2.6, 5.2])
        self.assertEqual(p["aisle_m"], 6)
        self.assertEqual(p["ramp_width_m"], 6.5)
        self.assertEqual(p["max_slope"], 0.15)
        self.assertIn("ParkingPerBasement=40", self.generator)
        self.assertIn("if(parkingBuilt!=80)", self.generator)
        self.assertEqual(p["B1_standard"], 36)
        self.assertEqual(p["B1_accessible"], 4)
        self.assertEqual(p["ramp_side"], "EAST")
        self.assertEqual(p["ramp_incline_length_m"], 24)
        self.assertEqual(p["transition_each_m"], 1.5)
        self.assertIn('EAST ramp G -> B1",36.25f,-23.0f,-1.80f,6.5f,24.0f,8.5308f', self.generator)
        self.assertIn('EAST ramp B1 -> B2",36.25f,0.0f,-5.40f,6.5f,24.0f,-8.5308f', self.generator)
        self.assertIn("B1 EAST internal turning apron", self.generator)

    def test_vertical_cores_are_real(self):
        self.assertEqual(self.spec["cores"]["elevators"], 4)
        self.assertEqual(self.spec["cores"]["stairs"], 2)
        self.assertIn("AddComponent<BraxWinda>()", self.generator)
        self.assertIn("controller.pietraY=levels.Select", self.generator)
        self.assertIn("controller.przystanekL=landL", self.generator)
        self.assertIn("foreach(float x in new[]{-26.3f,26.3f})", self.generator)
        self.assertIn("const float stairZ=12.70f", self.generator)
        self.assertIn("if(elevatorsBuilt!=4||stairsBuilt!=2)", self.generator)
        self.assertIn("calls[n]=usable[n]?", self.generator)
        self.assertIn("Bathroom 0.80m door header", self.generator)

    def test_main_entrance_is_physically_open(self):
        self.assertIn("float side=(6f-3.6f)*.5f", self.generator)
        self.assertIn('B(root,"Main entrance glass left"', self.generator)
        self.assertIn('B(root,"Main entrance glass right"', self.generator)
        # Door leaves are visual/non-solid; no hidden normal WindowBay in centre.
        self.assertIn("if(baseY==0f && zsign<0f && i==3)", self.generator)

    def test_ground_plinth_and_arch_sill_align(self):
        self.assertEqual(self.spec["facade"]["plinth_height_m"], 1.2)
        self.assertIn("baseY==0f?1.20f", self.generator)
        self.assertIn("ground_arch_m", self.spec["facade"])

    def test_ground_arches_are_physical_openings_not_flat_texture(self):
        self.assertIn("Ground arch masonry infill", self.generator)
        self.assertIn("Mathf.Sqrt(Mathf.Max(0f,r*r-radial*radial))", self.generator)
        self.assertIn("Ground arch 3D segment", self.generator)

    def test_parking_stair_clearance_and_full_width_ramp_connection(self):
        self.assertIn("stair opening x=-29.00..-23.60, z=8.50..17.00", self.generator)
        self.assertIn("const float stairZ=12.70f", self.generator)
        self.assertIn('B1 EAST ramp connector",M.antiSlip,35.65f', self.generator)
        self.assertIn('B2 EAST ramp connector",M.antiSlip,35.65f', self.generator)
        self.assertIn("Full-width connectors span from east wall", self.generator)

    def test_roof_has_mansard_dormers_and_sub_130_geometry(self):
        self.assertIn("lowerAngle=67f", self.generator)
        self.assertIn("lowerRise=7.60f", self.generator)
        self.assertIn("upperRise=4.70f", self.generator)
        self.assertIn("Mansard dormer body", self.generator)
        self.assertIn("Side mansard dormer body", self.generator)
        self.assertIn("Hidden roof technical deck", self.generator)
        self.assertIn("ridge_max_y", self.spec["roof"])

    def test_material_matrix_complete_and_downloaded_by_pipeline(self):
        required = set(self.spec["materials"].values())
        # Some spec keys map to existing project materials that are already in prepare_assets.
        for slug in required:
            self.assertIn(slug, self.prepare, f"missing source slug in prepare_assets.py: {slug}")
        for slug in [
            "beige_wall_001","sandstone_blocks_08","white_stucco_02","roof_slates_03","marble_01",
            "oak_wood_planks","herringbone_parquet","terrazzo_tiles","rubber_tiles","poly_wool_herringbone",
            "tiled_floor_001","granite_tile_04","anti_slip_concrete","garage_floor","concrete_wall_003",
            "brown_leather","leather_white","clean_asphalt"
        ]:
            self.assertIn(f'"{slug}"', self.materials)
        self.assertIn('metallic', self.materials.lower())
        self.assertIn('clear_graphite_glass', self.materials)

    def test_android_texture_contract(self):
        android = self.spec["android"]
        self.assertEqual(android["max_texture"], 2048)
        self.assertEqual(android["format"], "ASTC_4x4")
        self.assertTrue(android["mipmaps"])
        self.assertTrue(android["streaming"])
        texture_import = (EDITOR / "BraxChampionsTextureImport.cs").read_text(encoding="utf-8")
        self.assertIn("maxTextureSize=2048", texture_import)
        self.assertIn("TextureImporterFormat.ASTC_4x4", texture_import)
        self.assertIn("mipmapEnabled=true", texture_import)
        self.assertIn("streamingMipmaps=true", texture_import)

    def test_t03_is_mandatory_in_building_generation(self):
        self.assertEqual(self.buildings.count("BraxChampionsHotelSobczakT03.Generate();"), 1)
        at = self.buildings.index("BraxChampionsHotelSobczakT03.Generate();")
        near = self.buildings[max(0, at-350):at+250]
        self.assertNotIn('optional("hotel', near.lower())
        self.assertIn("hotelGenerated=true", near)
        self.assertIn("BraxChampionsHotelSobczakMaterials.VerifySources();", self.build)

    def test_mobile_streaming_exists(self):
        self.assertIn("guestInteriors", self.streamer)
        self.assertIn("neighbourFloors = 1", self.streamer)
        self.assertIn("SetActive(on)", self.streamer)
        self.assertIn("BraxHotelSobczakFloorStreamer", self.generator)
        self.assertIn("fg.gameObject.SetActive(false)", self.generator)
        self.assertIn("basementInteriors", self.streamer)
        self.assertIn("ApplyBasements(false)", self.streamer)
        self.assertIn("g.gameObject.SetActive(false)", self.generator)


    def test_structural_slabs_leave_real_core_openings(self):
        self.assertIn("SlabWithCoreOpenings", self.generator)
        self.assertIn('central 42m zone",mat,0,y,0,42.00f', self.generator)
        self.assertIn('float[] xs={22.45f,25.25f,28.05f,-26.30f}', self.generator)
        self.assertIn('BuildElevator(i,xs[i],1.50f)', self.generator)
        self.assertNotIn('B(g,"Ground structural slab",M.marble,0,.15f,0,63.2f,.30f,41.2f', self.generator)
        self.assertIn('SlabWithCoreOpenings(g,"Ground structural slab"', self.generator)
        self.assertIn('SlabWithCoreOpenings(g,"Level1 structural slab"', self.generator)
        self.assertIn('SlabWithCoreOpenings(guestExterior,"Guest slab "', self.generator)
        self.assertNotIn('SlabWithCoreOpenings(g,id+" ceiling"', self.generator)


    def test_central_corridor_is_not_blocked_by_vertical_cores(self):
        self.assertIn('Central corridor wool runner",M.carpet,0,.022f,0,42.0f,.035f,2.40f', self.generator)
        self.assertNotIn('float[] xs={-4.35f,-1.45f,1.45f,4.35f}', self.generator)
        self.assertIn('Passenger bank in EAST core, service lift in WEST core', self.generator)

    def test_guest_party_walls_are_not_duplicated(self):
        self.assertNotIn("Room west party wall", self.generator)
        self.assertNotIn("Room east party wall", self.generator)
        self.assertIn('foreach(float wallX in new[]{-21f,-15f,-9f,-3f,3f,9f,15f,21f})', self.generator)

    def test_service_and_room_doors_have_real_clear_openings(self):
        self.assertIn("doorH=2.20f", self.generator)
        self.assertIn("Room oak door leaf OPEN", self.generator)
        self.assertIn("Bathroom oak door leaf OPEN", self.generator)


    def test_real_three_level_exterior_lod_exists(self):
        self.assertIn("BuildExteriorLOD();", self.generator)
        self.assertIn("new LOD(.12f,lod0)", self.generator)
        self.assertIn("new LOD(.04f,l1.GetComponentsInChildren<Renderer>(true))", self.generator)
        self.assertIn("new LOD(.006f,l2.GetComponentsInChildren<Renderer>(true))", self.generator)
        self.assertIn("LOD2 | silhouette + roof + main divisions", self.generator)

    def test_lighting_is_limited_and_temperature_controlled(self):
        self.assertIn("useColorTemperature=true", self.generator)
        self.assertIn("2850f", self.generator)
        self.assertIn("3000f", self.generator)
        self.assertIn("2700f", self.generator)
        self.assertIn("3500f", self.generator)
        self.assertIn("3800f", self.generator)
        self.assertIn("Bedside warm emissive", self.generator)
        self.assertNotIn("AddComponent<Light>()", self.generator.split("static void BuildGuestRoom")[1].split("// ---------------------------------------------------------------- STAIRS")[0])

    def test_owner_floor_datum_ramp_and_entry_clearance(self):
        self.assertIn('SlabWithCoreOpenings(g,id+" floor",M.garage,y-.15f,.30f,3f)', self.generator)
        self.assertNotIn('SlabWithCoreOpenings(g,id+" ceiling",M.concrete', self.generator)
        self.assertIn('slopeLength=horizontalRun/Mathf.Cos', self.generator)
        self.assertIn('east wall S1",M.concrete,31.8f,y+1.8f,-17.15f', self.generator)
        self.assertIn('east wall N1",M.concrete,31.8f,y+1.8f,18.40f', self.generator)
        self.assertIn('PARK line L",M.parkingPaint,x-w*.5f+.05f,y+.020f,z,.10f', self.generator)
        self.assertIn('Classical sandstone plinth front WEST', self.generator)
        self.assertIn('Classical sandstone plinth front EAST', self.generator)
        self.assertNotIn('Classical sandstone plinth front",M.sandstone,0,.60f,-21.15f,64f', self.generator)
        self.assertIn('gz=z+outward*.05f', self.generator)
        self.assertIn('gz=zwall+outward*.05f', self.generator)

    def test_stairs_clear_parking_and_landings_do_not_overlap(self):
        self.assertIn('const float stairZ=12.70f', self.generator)
        self.assertIn('stair opening x=-29.00..-23.60, z=8.50..17.00', self.generator)
        self.assertIn('float lowerLandingZ=centreZ-dir*(run*.5f+.75f)', self.generator)
        self.assertIn('float midLandingZ=centreZ+dir*(run*.5f+.75f)', self.generator)
        self.assertIn('Stair floor landing 1.50m', self.generator)
        self.assertIn('Stair TECH landing 1.50m', self.generator)
        self.assertNotIn('Stair mid landing 1.50m",M.granite,centreX,y0+rise*.5f,centreZ', self.generator)

    def test_public_cores_are_clear(self):
        self.assertIn('Area(g,"TOALETA M 40m2",M.marble,-15f,.022f,17.5f,8f,5f)', self.generator)
        self.assertIn('Area(g,"TOALETA K 40m2",M.marble,15f,.022f,17.5f,8f,5f)', self.generator)
        self.assertIn('Area(g,"SPA 350m2",M.terrazzo,8.5f,6.022f,9.25f,25f,14f)', self.generator)
        self.assertIn('float clearH=h-.30f,wallY=baseY+clearH*.5f', self.generator)
        self.assertIn('float clearH=h-.30f,y=clearH*.5f', self.generator)
        self.assertIn('Bathroom shower tray 1.20x0.90', self.generator)

    def test_decorative_roof_has_no_heavy_colliders(self):
        self.assertIn('Mansard dormer body",M.stucco,x,123.35f,z,3.0f,2.8f,1.35f,false', self.generator)
        self.assertIn('Upper roof front",M.roof', self.generator)
        self.assertNotIn('Upper roof front",M.roof,0,127.55f,-upperStartZ*.5f,56f,.16f,upperLen,true', self.generator)

    def test_owner_21_frame_visual_acceptance_plan(self):
        required = [
            "01-front-day","02-front-night","03-three-quarter-left","04-three-quarter-right",
            "05-side","06-rear","07-entrance-portico","08-lobby","09-reception","10-bar",
            "11-restaurant","12-spa","13-fitness","14-typical-corridor","15-standard-room",
            "16-bathroom","17-elevators","18-stair","19-b1","20-b2","21-roof-mansard"
        ]
        for label in required:
            self.assertIn(f'"{label}"', self.visual_review)
        self.assertIn("nightFrames[1]=true", self.visual_review)
        self.assertIn("player.transform.position=positions[i]", self.visual_review)
        self.assertIn("labels.Length==21", self.visual_review)
        self.assertIn("visual.gameObject.SetActive(false)", self.visual_review)
        self.assertNotIn("visual.SetActive(false)", self.visual_review)

    def test_owner_sign_is_hotel_sobczak(self):
        self.assertIn('AddBlockSign("HOTEL SOBCZAK"', self.generator)
        self.assertNotIn("HOTEL WAWER", self.generator)
        self.assertNotIn("BRAXHORN HOTEL", self.generator)

if __name__ == "__main__":
    unittest.main()
