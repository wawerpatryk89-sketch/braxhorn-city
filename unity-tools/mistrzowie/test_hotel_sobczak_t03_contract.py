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
        self.assertIn("final flight to TECH y=117.6", self.generator)

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
        self.assertIn("if(elevatorsBuilt!=4||stairsBuilt!=2)", self.generator)
        self.assertIn("calls[n]=usable[n]?", self.generator)
        self.assertIn("Bathroom 0.80m door header", self.generator)

    def test_main_entrance_is_physically_open(self):
        self.assertIn("Real 3.60m-wide main entrance opening", self.generator)
        self.assertIn('B(root,"Main entrance glass left"', self.generator)
        self.assertIn('B(root,"Main entrance glass right"', self.generator)
        # Door leaves are visual/non-solid; no hidden normal WindowBay in centre.
        self.assertIn("if(baseY==0f && zsign<0f && i==3)", self.generator)

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

    def test_owner_sign_is_hotel_sobczak(self):
        self.assertIn('AddBlockSign("HOTEL SOBCZAK"', self.generator)
        self.assertNotIn("HOTEL WAWER", self.generator)
        self.assertNotIn("BRAXHORN HOTEL", self.generator)

if __name__ == "__main__":
    unittest.main()
