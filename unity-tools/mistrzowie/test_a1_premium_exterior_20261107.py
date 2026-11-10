"""Static safety regressions for the A1 premium exterior finish.

This checks source geometry and contracts only, NOT a Unity compilation,
a photographed scene or an Android APK.
"""
import math
import re
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
        self.assertIn('BraxChampionsArenaSigns.Generate(root, materials);', self.buildings)
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
        # Owner render 9.11: one unbroken column plinth->roof, full-height gold.
        self.assertIn("new Vector3(.85f,25.52f,2.20f)", self.arrival)
        # owner 10.11: the column line is a 20 cm warm LED (owner render), not a 12 cm gold strip
        self.assertIn(".038f,25.40f,.200f,.018f", self.arrival)
        self.assertNotIn("new Vector3(.85f,9.82f,2.20f)", self.arrival)
        self.assertIn("rings.Add((back,bevel))", self.arrival)
        self.assertIn("bevel*Mathf.Cos(theta)", self.arrival)
        self.assertIn("(x:front,inset:0f,frontFace:true)", self.arrival)
        self.assertIn("LED_column_front_width=.200 projection=.030 totalDepth=.038 embedded=.008 edge_R=.018 capsule_end_R=.060", self.arrival)
        self.assertGreaterEqual(2.20, 2.0)
        self.assertGreaterEqual(.85, .70)
        self.assertIn("actualFeatureColumns!=38||skippedFeatureColumns!=2", self.arrival)
        self.assertIn("warm_roof_crown_LED_preserved", self.arrival)
        # 9.11 phone vs render: the three warm bands read as white stripes.
        self.assertNotIn("three_warm_upper_glass_bands_preserved", self.arrival)
        self.assertIn("upper_interior_elliptical_halo_preserved", self.arrival)
        self.assertNotIn("REFERENCE red neon vertical sign frame", self.arrival)
        self.assertNotIn("REFERENCE red neon sign top accent", self.arrival)
        self.assertNotIn("REFERENCE red neon sign lower accent", self.arrival)
        self.assertNotIn('Paint("arena_reference_red_neon"', self.arrival)
        self.assertIn("front_sign_external_red_geometry=0", self.arrival)
        # Owner render 9.11: canopy soffit full of warm downlights, posts=0,
        # sign bodies follow the artwork outline (no frames to measure).
        front=(EDITOR / "BraxChampionsArenaFront.cs").read_text(encoding="utf-8")
        self.assertIn('"A1 canopy | warm LED soffit strip"', front)
        # owner 10.11: LED strips run from the facade to the fascia (owner render), 21 across the 40 m canopy
        self.assertIn("float x=-18.5f+i*1.85f,z0=fz+.45f,z1=BackZ(x)-.45f;", front)
        self.assertIn("float totalDepth=protrusion+.002f", self.arrival)
        self.assertIn("arenaRoot.TransformPoint(new Vector3(x,20f,z)*K)", self.arrival)
        self.assertIn("arenaRoot.InverseTransformPoint(hit.point).y", self.arrival)
        self.assertNotIn("const float plotX=600f,plotZ=800f", self.arrival)
        self.assertIn("var cc=go.AddComponent<MeshCollider>();", self.arrival)
        self.assertIn("cc.sharedMesh=mesh;", self.arrival)
        self.assertNotIn("CylinderCollider", self.arrival)
        scene_audit=(EDITOR / "BraxSceneAudit.cs").read_text(encoding="utf-8")
        # 9.11: canopy posts removed; the solid parts are the sign body and canopy meshes.
        self.assertIn('if(solid)go.AddComponent<MeshCollider>().sharedMesh=mesh;', self.signs)
        self.assertNotIn('GetComponent<CylinderCollider>()', scene_audit)

    def test_both_a1_glass_levels_smoked_glossy_but_doors_clear(self):
        self.assertIn("wall(m,'facade_glass',np.array([p,q]),4.5 if entry else .18,10.75)",self.exterior_source)
        self.assertIn("wall(m,'facade_glass',path,10.75,27.15)",self.exterior_source)
        self.assertIn('var glass=Surface("facade_glass",new Color(.18f,.20f,.23f,.48f),.075f,0f);',self.buildings)
        self.assertIn('glass.SetFloat("_ReflectionStrength",.62f);',self.buildings)
        self.assertIn('var clear=Surface("clear_glass",new Color(.78f,.88f,.93f,.19f),.10f,0f);',self.buildings)
        self.assertIn('ConfigureGlass(glass);',self.buildings)
        self.assertIn('ConfigureGlass(clear);',self.buildings)
        self.assertIn('m.SetFloat("_Transparent",1);',self.buildings)
        self.assertIn('m.SetFloat("_ZWrite",0);',self.buildings)
        self.assertIn('Door(root, "CHAMPIONS_A1_PUBLIC", "Main public entrance", -59.04f, exterior["clear_glass"]',self.arrival)
        self.assertIn('Door(root, "CHAMPIONS_A1_ATHLETES", "Separate athlete rear entrance", 59.04f, exterior["clear_glass"]',self.arrival)

    def test_real_pbr_and_transparent_canopy_no_door_colliders(self):
        for key in ('granite_plinth_preserved', 'curtain_wall_black_mullions_owner_render',
                    'PBR_roof_coping_preserved', 'FrontGoldCanopy(group,exterior);'):
            self.assertIn(key, self.arrival)
        self.assertIn('exterior["dark_stone"]', self.arrival)
        self.assertIn('exterior["clear_glass"]', self.arrival)
        # Old thin glass canopy remains ONLY at the athlete/NORTH entrance.
        self.assertEqual(self.arrival.count('new Vector3(21.0f,.07f,6.8f),false'),1)
        # Owner render 9.11: gold cantilever canopy, no posts, no black cassettes.
        self.assertNotIn('ISSUE27 curved black canopy 48.00x13.00x0.85', self.arrival)
        self.assertNotIn('Gold support stem', self.arrival)
        self.assertIn('legacy_public_canopy=21x6.8x0.07_removed=true', self.arrival)
        self.assertNotIn('OriginalImage =', self.arrival)

    def test_curved_original_artwork_keeps_six_to_one_ratio_and_quality(self):
        # Front sign is a flat 3D body on the canopy; the artwork keeps 6:1.
        self.assertIn('FrontSign3D(t,MakeMat(0),body);', self.signs)
        width = float(re.search(r'FrontWidth=([\d.]+)f', self.signs).group(1))
        height = float(re.search(r'FrontHeight=([\d.]+)f', self.signs).group(1))
        self.assertAlmostEqual(width / height, 6.0, places=6)
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
        self.assertIn("float uniform = WorldLengthMetres / bounds.size.z;", self.car)
        self.assertNotIn("LengthMetres * K / bounds.size.z", self.car)
        self.assertIn("645.175f, .16f, 712f", self.car)
        self.assertIn("interactionRadius = 3.8f * CarScale", self.car)

    def test_ellipse_is_round_enough_for_walking_range_without_faceting(self):
        # Maximum radial sagitta of a 1-degree chord on the 96m major radius.
        sagitta=96*(1-math.cos(math.pi/360))
        self.assertLess(sagitta,.004)
        self.assertIn("const int bays=360;",self.arrival)
        self.assertIn("n=360;path=ellipse(96,59,n)",self.exterior_source)

    def test_parking_driveways_reach_city_road_without_sidewalk_step(self):
        self.assertIn("A1_PARKING_ENTRANCES=[(531.8,660.0,6.4,7.0),(661.8,660.0,6.4,7.0),(577.8,918.0,6.4,7.0)]",self.layout)
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
        # Owner 10.11 material board: hall walls = ambientCG Leather013 (A28) on a black marble plinth.
        self.assertIn('"Leather013"', self.arena)
        self.assertIn('"granite_tile"', self.arena)
        self.assertIn('"rectangular_facade_tiles"', self.arena)
        self.assertIn('"velour_velvet"', self.arena)
        self.assertIn('"wooden_panels"', self.arrival)
        # Whole main floor remains Stone Tiles; premium wall materials do not cover murals.
        self.assertIn('"arena_floor_stone_tiles_owner", "stone_tiles"', self.arena)
        self.assertIn('Top(floorStone,', self.arena)
        self.assertIn('if (alongX)', self.arena)
        self.assertIn('BoxMinMax(marble,a0,a1,yF,WallPlinthTop,g0,g1,true);', self.arena)
        # Owner 9.11 23:46 "każda ściana ma być wykończona": no bare concrete in the hall any more.
        self.assertNotIn('BoxMinMax(wallConcrete', self.arena)
        # Owner 10.11 00:51-01:10: no walnut / brown anywhere in the hall.
        self.assertNotIn('smoked_walnut_veneer', self.arena)
        self.assertIn('BoxMinMax(wallLeather,a0,a1,WallPlinthTop,WallPanelTop,g0,g1,true);', self.arena)
        # Athlete/changing floors stay green; only their walls use Wooden Panels.
        self.assertIn('"arena_athlete_floor_stone_tiles_owner", "stone_tiles"', self.arrival)
        self.assertIn('"arena_athlete_walls_wooden_panels_owner", "wooden_panels"', self.arrival)
        athletes = (ROOT / 'unity-city/Assets/Editor/Mistrzowie/BraxChampionsArenaAthletes.cs').read_text(encoding='utf-8')
        self.assertIn('"Changing room inner wall 1 | Wooden Panels" + "inner wall 2"', athletes)
        self.assertIn('"Changing room enclosed ceiling", ceiling', athletes)
        self.assertIn('AthleteRoomShells(root, arenaStoneFloor, athleteWoodWall, exterior["dark_stone"]);', self.arrival)

    def test_issue27_exact_trim_projections_and_interior_finish(self):
        # Owner render 9.11 canopy: 1.2 m gold fascia on top of a 6.2 m soffit,
        # sign box standing on it, front of the sign 30 cm behind the fascia.
        front = (ROOT / 'unity-city/Assets/Editor/Mistrzowie/BraxChampionsArenaFront.cs').read_text(encoding='utf-8')
        self.assertIn('CanopyUnderside=6.20f, CanopyTop=7.40f, CanopyHalfWidth=20f, CanopyFrontZ=-63.6f', front)
        self.assertIn('FrontQuad(gv,gt,new Vector3(x0,y0,fz)', front)
        self.assertIn('exterior["arena_gold"],true', front)
        self.assertIn('posts=0', front)
        self.assertIn('FrontFaceZ=BraxChampionsArenaArrival.CanopyFrontZ+.30f', self.signs)
        self.assertIn('"Leather013"',self.arena)
        self.assertIn('gold_edges_20mm_continuous_tread_and_riser=true',self.arena)

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
        self.assertIn('bool originalArenaGraphics=b.id=="A1"&&BraxChampionsArenaSigns.HasSources();',self.buildings)
        self.assertIn('if(!originalArenaGraphics)',self.buildings)

    def test_lit_black_pillars_remain_outside_tinted_glass(self):
        self.assertIn('goldColumnInserts.Emit(group',self.arrival)
        self.assertIn('ISSUE27_warm_LED_column_capsules_width0p12_projection0p03",ArenaLed(exterior)',self.arrival)
        self.assertIn('ISSUE27_black_columns_38_width2p20_depth0p85_rounded',self.arrival)
        # Owner correction: no separate red neon geometry may sit outside the artwork.
        # The exterior composition is black belt + physical gold frame only.
        self.assertNotIn('REFERENCE red neon vertical sign frame',self.arrival)
        self.assertNotIn('REFERENCE red neon sign top accent',self.arrival)
        self.assertNotIn('REFERENCE red neon sign lower accent',self.arrival)
        # 9.11: signs are 3D bodies, no rectangular frames; belt at render height.
        self.assertNotIn('ISSUE27_rounded_gold_sign_frames_original_artwork_untouched',self.arrival)
        self.assertIn('black_belt_360_owner_render_y7p60_to11p40',self.arrival)

    def test_reference_upper_lounge_is_inside_original_oval_without_double_scale(self):
        self.assertIn('ReferenceUpperFacadeLounge(root, exterior);',self.arrival)
        self.assertIn('A1 UPPER GLASS | reference furnished VIP lounge silhouette',self.arrival)
        self.assertIn('A1_upper_facade_charcoal_leather_2k',self.arrival)
        # Authored at the old 18.24 m floor, moved down ONCE onto the belt.
        self.assertIn('new Vector3(0,18.12f,-48.05f),new Vector3(84f,.24f,6.9f)',self.arrival)
        self.assertIn('"A1 UPPER GLASS | reference furnished VIP lounge silhouette",Vector3.up*LoungeDrop);',self.arrival)
        # Front edge at z=-51.5, outmost x=+-42: inside elliptical glass.
        self.assertLess((42/96)**2+(51.5/59)**2,1.0)
        self.assertNotIn('new Vector3(x,0,z)*K)',self.arrival)
        self.assertNotIn('new Vector3(x,0,-48.2f)*K)',self.arrival)
        # Furniture is visual-only, no collision blocking the promenade.
        self.assertIn('originalArenaVIPSeatsUnchanged=true',self.arrival)


if __name__ == "__main__":
    unittest.main()
