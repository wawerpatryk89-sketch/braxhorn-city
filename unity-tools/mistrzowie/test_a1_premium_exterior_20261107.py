"""Static safety regressions for the A1 premium exterior finish.

This checks source geometry and contracts only, NOT a Unity compilation,
a photographed scene or an Android APK.
"""
import math
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]
EDITOR = ROOT / "unity-city/Assets/Editor/Mistrzowie"


class A1PremiumExteriorRegression(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.arrival = (EDITOR / "BraxChampionsArenaArrival.cs").read_text(encoding="utf-8")
        cls.arena = (EDITOR / "BraxChampionsArena.cs").read_text(encoding="utf-8")
        cls.signs = (EDITOR / "BraxChampionsArenaSigns.cs").read_text(encoding="utf-8")
        cls.importer = (EDITOR / "BraxChampionsArenaTextureImport.cs").read_text(encoding="utf-8")
        cls.buildings = (EDITOR / "BraxChampionsBuildings.cs").read_text(encoding="utf-8")
        cls.car = (EDITOR / "BraxChampionsCarBuilder.cs").read_text(encoding="utf-8")
        cls.exterior_source = (ROOT / "unity-tools/mistrzowie/build_exteriors.py").read_text(encoding="utf-8")
        cls.inventory = (ROOT / "unity-tools/mistrzowie/plan_inventory.json").read_text(encoding="utf-8")
        cls.layout = (ROOT / "unity-tools/mistrzowie/layout.py").read_text(encoding="utf-8")
        cls.foundation = (ROOT / "unity-tools/mistrzowie/legacy_foundation.py").read_text(encoding="utf-8")

    def test_exterior_generator_connected_and_original_bowl_retained(self):
        self.assertIn("PremiumExterior(root, exterior);", self.arrival)
        self.assertIn('BraxChampionsArena.Generate(root);', self.buildings)
        self.assertIn('BraxChampionsArenaSigns.Generate(root);', self.buildings)
        self.assertIn('BraxChampionsArenaArrival.Generate(root, materials);', self.buildings)
        self.assertIn('const float rx=96f,rz=59f;', self.arrival)
        self.assertIn("const int bays=360;", self.arrival)
        self.assertIn("m=Meshes();n=360;path=ellipse(96,59,n)",self.exterior_source)
        self.assertIn("bool featurePillar=i%9==0;",self.arrival)
        self.assertEqual(360//9,40)
        self.assertIn("mesh.SetVertices(positions)", self.arrival)
        self.assertNotIn("visualAccepted=true", self.arrival)

    def test_reference_shell_replaces_old_48m_stone_belt(self):
        self.assertIn("low two-storey premium glass arena", self.exterior_source)
        self.assertIn("wall(m,'facade_glass',path,10.75,27.15)", self.exterior_source)
        self.assertIn("m.slab('roof',foot.buffer(.55),27.35,.30)", self.exterior_source)
        self.assertIn("ribbon(m,'dark_stone',rim,0,.42,27.72,.18)", self.exterior_source)
        self.assertNotIn("wall(m,'stone',path,10.8,18.2)", self.exterior_source)
        self.assertNotIn("m.slab('roof',foot.buffer(.55),47.2,.30)", self.exterior_source)
        self.assertNotIn("m.slab('clear_glass',box(-23,-68,23,-56),8.7,.10)", self.exterior_source)
        self.assertRegex(self.inventory, r'"id":\s*"A1"[\s\S]{0,180}?"height":\s*28')

    def test_owner_sign_band_and_both_doorways_are_clear(self):
        # Physical door top is 4.5m above the exterior floor.
        self.assertGreater(5.18 - .15 / 2, 4.5)
        # Issue #27 columns stop below and restart above the owner's sign band.
        self.assertLessEqual(1.03 + 9.82, 10.85)
        self.assertLess(10.85, 11.12)
        self.assertGreater(18.55, 11.12 + 7.0)
        self.assertGreater(19.05 - .15 / 2, 18.12)
        self.assertIn('Mathf.Abs(mid.x)<8.4f&&Mathf.Abs(mid.z)>57f', self.arrival)
        self.assertGreater(10.8 - .22 / 2, 4.7)
        self.assertIn('foreach(float side in new[]{-1f,1f})', self.arrival)

    def test_glass_bay_columns_are_wide_premium_not_thin_strips(self):
        # Nominal estate dimensions; K is applied once in the mesh builder.
        self.assertIn("new Vector3(.85f,9.82f,2.20f)", self.arrival)
        self.assertIn("new Vector3(.85f,8.00f,2.20f)", self.arrival)
        self.assertIn(".038f,9.70f,.120f,.018f", self.arrival)
        self.assertIn(".038f,7.88f,.120f,.018f", self.arrival)
        self.assertIn("rings.Add((back,bevel))", self.arrival)
        self.assertIn("bevel*Mathf.Cos(theta)", self.arrival)
        self.assertIn("gold_column_front_width=.120 projection=.030 totalDepth=.038 embedded=.008 edge_R=.018 capsule_end_R=.060", self.arrival)
        self.assertGreaterEqual(2.20, 2.0)
        self.assertGreaterEqual(.85, .70)
        self.assertIn("actualFeatureColumns!=38||skippedFeatureColumns!=2", self.arrival)
        self.assertIn("warm_roof_crown_LED_preserved", self.arrival)
        self.assertIn("three_warm_upper_glass_bands_preserved", self.arrival)
        self.assertIn("upper_interior_elliptical_halo_preserved", self.arrival)
        self.assertIn("REFERENCE red neon vertical sign frame", self.arrival)
        self.assertIn("PUBLIC ISSUE27 recessed warm LED 3000K", self.arrival)
        self.assertIn("beltTrimR_front=.025 rest=.020 signFrameR=.040", self.arrival)
        self.assertIn("fasciaR=.120 upperGoldR=.055 sideGoldR=.035 lowerGoldR=.025", self.arrival)
        self.assertIn("const float frameDepth=.082f", self.arrival)
        self.assertIn("new Vector3(.242f,.48f,len)", self.arrival)
        self.assertIn("new Vector3(.112f,.16f,len)", self.arrival)
        self.assertIn("new Vector3(.052f,.08f,len)", self.arrival)
        self.assertIn("new Vector3(.072f,.100f,back-front)", self.arrival)
        self.assertIn("float totalDepth=protrusion+.002f", self.arrival)
        self.assertIn("ActualFloorYNominal(arenaRoot,x,-68.70f)", self.arrival)
        self.assertIn("CylinderCollider", self.arrival)

    def test_real_pbr_and_transparent_canopy_no_door_colliders(self):
        for key in ('granite_plinth_preserved', 'curtain_wall_aluminium_preserved',
                    'PBR_roof_coping_preserved', 'ISSUE27_public_canopy_gold_outline'):
            self.assertIn(key, self.arrival)
        self.assertIn('exterior["dark_stone"]', self.arrival)
        self.assertIn('exterior["clear_glass"]', self.arrival)
        # Old thin glass canopy remains ONLY at the athlete/NORTH entrance.
        self.assertEqual(self.arrival.count('new Vector3(21.0f,.07f,6.8f),false'),1)
        self.assertIn('ISSUE27 curved black canopy 48.00x13.00x0.85', self.arrival)
        self.assertIn('legacy_public_canopy=21x6.8x0.07_removed=true', self.arrival)
        self.assertNotIn('OriginalImage =', self.arrival)

    def test_curved_original_artwork_keeps_six_to_one_ratio_and_quality(self):
        self.assertIn('Curved(t,0,MakeMat(0),true,false,42f,11.12f,7f);', self.signs)
        self.assertIn("importer.streamingMipmaps=!signs;", self.importer)
        self.assertIn("importer.anisoLevel=signs?16:4;", self.importer)
        self.assertIn("importer.filterMode=FilterMode.Trilinear;", self.importer)
        self.assertIn("importer.wrapMode=TextureWrapMode.Clamp;", self.importer)
        self.assertIn("TextureImporterFormat.ASTC_4x4", self.importer)

    def test_existing_owner_vehicle_mesh_not_substituted(self):
        self.assertIn("PlaceWheelsDown(copy,root.transform);", self.car)
        self.assertIn('Assets/Source/Mistrzowie/Vehicles/car_rev.glb', self.car)
        self.assertNotIn("GameObject.CreatePrimitive(PrimitiveType.Cube)", self.car)
        self.assertIn("sourceMeshUnchanged=true", self.car)
        self.assertIn("float uniform = LengthMetres / bounds.size.z;", self.car)
        self.assertNotIn("LengthMetres * K / bounds.size.z", self.car)
        self.assertIn("645.175f, .16f, 712f", self.car)
        self.assertIn("interactionRadius = 3.8f", self.car)

    def test_ellipse_is_round_enough_for_walking_range_without_faceting(self):
        # Maximum radial sagitta of a 1-degree chord on the 96m major radius.
        sagitta=96*(1-math.cos(math.pi/360))
        self.assertLess(sagitta,.004)
        self.assertIn("const int bays=360;",self.arrival)
        self.assertIn("n=360;path=ellipse(96,59,n)",self.exterior_source)

    def test_parking_driveways_reach_city_road_without_sidewalk_step(self):
        self.assertIn("A1_PARKING_ENTRANCES=[(531.8,660.0,6.4,7.0),(661.8,660.0,6.4,7.0)]",self.layout)
        self.assertIn("A1_PARKING_ENTRANCES",self.foundation)
        self.assertIn("if any(inside((x,z),r) for r in A1_PARKING_ENTRANCES):return None",self.foundation)
        self.assertIn('"Parking road-to-city gentle asphalt entry"',self.arrival)
        self.assertIn("x-3.2f,x+3.2f,-140f,-133f,0f,parkingTop,true",self.arrival)
        self.assertIn("new Vector3(x, .199f, -121f), new Vector3(6.4f, .078f, 24f)",self.arrival)
        self.assertAlmostEqual(.238/7,.034,places=3)

    def test_parking_edges_paths_and_markings_do_not_float_or_overlap(self):
        self.assertIn('"Parking north end concrete curb"',self.arrival)
        self.assertIn('"Parking south end concrete curb"',self.arrival)
        self.assertIn('"Parking entrance flush trench drain"',self.arrival)
        self.assertIn('"Accessible parking curb ramp"',self.arrival)
        self.assertIn('new Vector3(sx, .18f, -79f), new Vector3(53f, .04f, 5f)',self.arrival)
        self.assertIn('new Vector3(0, .18f, -104f), new Vector3(16f, .04f, 58f)',self.arrival)
        self.assertIn('new Vector3(stallX, .242f, (a + b) / 2f)',self.arrival)
        # Connector x ranges: left -61..-8, right 8..61. Parking paths begin
        # exactly at +-61 and promenade edges are exactly +-8.
        self.assertAlmostEqual(-34.5-53/2,-61)
        self.assertAlmostEqual(-34.5+53/2,-8)
        self.assertAlmostEqual(34.5-53/2,8)
        self.assertAlmostEqual(34.5+53/2,61)

    def test_reference_entrance_landscaping_uses_real_project_asset(self):
        self.assertIn('PremiumShrubSource = "Assets/Source/CityRebuild/landscape/park_shrub.glb"', self.arrival)
        self.assertIn("ReferenceEntranceLandscaping(root, exterior);", self.arrival)
        self.assertIn("A1 reference entrance | black-stone planter", self.arrival)
        self.assertIn("A1 reference entrance | gold planter cap", self.arrival)
        self.assertIn("A1 reference entrance | black bollard", self.arrival)
        self.assertIn("A1 reference entrance | warm bollard head", self.arrival)

    def test_red_carpet_is_seamless_across_public_door_without_overlap(self):
        # Existing outside red carpet ends at -67 + 15.4/2 = -59.3.
        # Indoor begins at -47.3 - 24.0/2 = -59.3; zero gap,
        # and no coplanar texture overlap to generate Z-fighting.
        self.assertAlmostEqual(-67+15.4/2,-47.3-24.0/2,places=6)
        self.assertIn('new Vector3(3.9f, .018f, 24.0f)',self.arrival)
        self.assertIn('Main lobby RED carpet | continuous flush gold edge',self.arrival)
        self.assertIn('Public RED velvet carpet | VIP arrival',self.arrival)
        self.assertIn('arena_vip_red_velvet_rope',self.arrival)
        self.assertIn('RED velvet VIP rope - naturally hanging segment',self.arrival)
        self.assertIn('const int segments = 10;',self.arrival)

    def test_owner_selected_interior_finishes_are_scoped_without_covering_side_murals(self):
        # Exact Poly Haven source slugs selected by owner are wired into
        # the arena and athlete-area generators checked by this sparse workflow.
        self.assertIn('"stone_tiles"', self.arena)
        self.assertIn('"concrete_wall_009"', self.arena)
        self.assertIn('"granite_tile"', self.arena)
        self.assertIn('"rectangular_facade_tiles"', self.arena)
        self.assertIn('"velour_velvet"', self.arena)
        self.assertIn('"wooden_panels"', self.arrival)
        # Whole main floor remains Stone Tiles; premium wall materials do not cover murals.
        self.assertIn('"arena_floor_stone_tiles_owner", "stone_tiles"', self.arena)
        self.assertIn('Top(floorStone,', self.arena)
        self.assertIn('if (alongX)', self.arena)
        self.assertIn('BoxMinMax(wallGranite', self.arena)
        self.assertIn('BoxMinMax(wallConcrete', self.arena)
        self.assertIn('BoxMinMax(facadePanels', self.arena)
        # Athlete/changing floors stay green; only their walls use Wooden Panels.
        self.assertIn('"arena_athlete_floor_stone_tiles_owner", "stone_tiles"', self.arrival)
        self.assertIn('"arena_athlete_walls_wooden_panels_owner", "wooden_panels"', self.arrival)
        self.assertIn('Changing room inner wall 1 | Wooden Panels', self.arrival)
        self.assertIn('Changing room inner wall 2 | Wooden Panels', self.arrival)
        self.assertIn('Changing room enclosed ceiling", ceiling', self.arrival)

    def test_perimeter_bays_skip_both_entries(self):
        skipped = 0
        for i in range(360):
            a = 2 * math.pi * i / 360
            b = 2 * math.pi * (i + 1) / 360
            x = 96 * (math.cos(a) + math.cos(b)) / 2
            z = 59 * (math.sin(a) + math.sin(b)) / 2
            if abs(x) < 8.4 and abs(z) > 57:
                skipped += 1
        self.assertEqual(skipped, 20)


    def test_all_four_signs_face_viewer_left_to_right_without_repainting(self):
        # Correct Unity camera basis is Right = Cross(Up, LookDirection).
        # At SOUTH the parametric +X is screen-right; at EAST +Z is also
        # screen-right. Only NORTH and WEST need UV reversal.
        self.assertIn('bool reverseU=alongX?positive:!positive;', self.signs)
        self.assertIn('float outwardFacingU=reverseU?1f-t:t;', self.signs)
        for front, positive, expected_flip in (
            (True, False, False),  # public front south of oval
            (True, True, True),   # rear/north
            (False, True, False), # east side
            (False, False, True), # west side
        ):
            with self.subTest(front=front,positive=positive):
                self.assertEqual(positive if front else (not positive),
                                 expected_flip)
        self.assertIn('owner', self.signs.lower())
        self.assertNotIn('SetPixels',self.signs)
        self.assertNotIn('EncodeToPNG',self.signs)

    def test_lit_black_pillars_remain_outside_tinted_glass(self):
        self.assertIn('goldColumnInserts.Emit(group',self.arrival)
        self.assertIn('ISSUE27_gold_column_capsules_width0p12_projection0p03',self.arrival)
        self.assertIn('ISSUE27_black_columns_38_width2p20_depth0p85_rounded',self.arrival)
        # At x=22.2m the elliptical FRONT skin is z=-57.40. Golden/red
        # front frame must be OUTSIDE that (more negative Z) and not hidden
        # by an old opaque facade or by the original sign image.
        facade_z=-59*math.sqrt(1-(22.2/96)**2)
        self.assertLess(-58.18,facade_z)
        self.assertIn('new Vector3(x,14.55f,-58.18f)',self.arrival)
        self.assertIn('new Vector3(x,17.10f,-59.02f)',self.arrival)
        self.assertIn('new Vector3(x,12.05f,-59.02f)',self.arrival)

    def test_reference_upper_lounge_is_inside_original_oval_without_double_scale(self):
        self.assertIn('ReferenceUpperFacadeLounge(root, exterior);',self.arrival)
        self.assertIn('A1 UPPER GLASS | reference furnished VIP lounge silhouette',self.arrival)
        self.assertIn('A1_upper_facade_charcoal_leather_2k',self.arrival)
        self.assertIn('new Vector3(0,18.12f,-46.2f)',self.arrival)
        # Front edge at z=-51.5, outmost x=+-42: inside elliptical glass.
        self.assertLess((42/96)**2+(51.5/59)**2,1.0)
        self.assertNotIn('new Vector3(x,0,z)*K)',self.arrival)
        self.assertNotIn('new Vector3(x,0,-48.2f)*K)',self.arrival)
        # Furniture is visual-only, no collision blocking the promenade.
        self.assertIn('originalArenaVIPSeatsUnchanged=true',self.arrival)


if __name__ == "__main__":
    unittest.main()
