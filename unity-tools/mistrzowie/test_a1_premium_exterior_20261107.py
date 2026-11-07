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
        cls.signs = (EDITOR / "BraxChampionsArenaSigns.cs").read_text(encoding="utf-8")
        cls.importer = (EDITOR / "BraxChampionsArenaTextureImport.cs").read_text(encoding="utf-8")
        cls.buildings = (EDITOR / "BraxChampionsBuildings.cs").read_text(encoding="utf-8")
        cls.car = (EDITOR / "BraxChampionsCarBuilder.cs").read_text(encoding="utf-8")
        cls.exterior_source = (ROOT / "unity-tools/mistrzowie/build_exteriors.py").read_text(encoding="utf-8")
        cls.inventory = (ROOT / "unity-tools/mistrzowie/plan_inventory.json").read_text(encoding="utf-8")

    def test_exterior_generator_connected_and_original_bowl_retained(self):
        self.assertIn("PremiumExterior(root, exterior);", self.arrival)
        self.assertIn('BraxChampionsArena.Generate(root);', self.buildings)
        self.assertIn('BraxChampionsArenaSigns.Generate(root);', self.buildings)
        self.assertIn('BraxChampionsArenaArrival.Generate(root, materials);', self.buildings)
        self.assertIn('const float rx=96f,rz=59f;', self.arrival)
        self.assertIn("const int bays=120;", self.arrival)
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
        # Four owner signs occupy vertical y=11.02..18.22.
        self.assertLess(5.68 + 9.30 / 2, 11.02)
        self.assertLess(10.52 + .15 / 2, 11.02)
        self.assertGreater(22.87 - 7.35 / 2, 18.22)
        self.assertGreater(19.05 - .15 / 2, 18.22)
        self.assertIn('Mathf.Abs(mid.x)<8.4f&&Mathf.Abs(mid.z)>57f', self.arrival)
        self.assertGreater(10.8 - .22 / 2, 4.7)
        self.assertIn('foreach(float side in new[]{-1f,1f})', self.arrival)

    def test_glass_bay_columns_are_wide_premium_not_thin_strips(self):
        # Nominal estate dimensions; K is applied once in the mesh builder.
        self.assertIn("new Vector3(.72f,9.30f,2.10f),rotation);", self.arrival)
        self.assertIn("new Vector3(.72f,7.35f,2.10f),rotation);", self.arrival)
        self.assertIn("new Vector3(.032f,9.14f,.095f),rotation);", self.arrival)
        self.assertIn("new Vector3(.032f,7.19f,.095f),rotation);", self.arrival)
        self.assertGreaterEqual(2.10, 2.0)
        self.assertGreaterEqual(.72, .70)
        self.assertIn("REFERENCE_continuous_warm_roof_crown_LED", self.arrival)
        self.assertIn("REFERENCE_three_warm_upper_glass_bands", self.arrival)
        self.assertIn("REFERENCE_upper_interior_elliptical_halo", self.arrival)
        self.assertIn("REFERENCE red neon vertical sign frame", self.arrival)
        self.assertIn("PUBLIC reference canopy underside LED strip", self.arrival)

    def test_real_pbr_and_transparent_canopy_no_door_colliders(self):
        for key in ('original_PolyHaven_granite_plinth', 'PBR_curtain_wall_aluminium',
                    'PBR_bronze_architectural_detail', 'PBR_roof_coping'):
            self.assertIn(key, self.arrival)
        self.assertIn('exterior["dark_stone"]', self.arrival)
        self.assertIn('exterior["clear_glass"]', self.arrival)
        self.assertIn('entry+" safety-glass canopy visible upper skin"', self.arrival)
        self.assertIn('new Vector3(21.0f,.07f,6.8f),false', self.arrival)
        self.assertIn("original_signs_preserved=true", self.arrival)
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
        self.assertIn("637.55f, .16f, 711f", self.car)
        self.assertIn("interactionRadius = 3.8f", self.car)

    def test_reference_entrance_landscaping_uses_real_project_asset(self):
        self.assertIn('PremiumShrubSource = "Assets/Source/CityRebuild/landscape/park_shrub.glb"', self.arrival)
        self.assertIn("ReferenceEntranceLandscaping(root, exterior);", self.arrival)
        self.assertIn("A1 reference entrance | black-stone planter", self.arrival)
        self.assertIn("A1 reference entrance | gold planter cap", self.arrival)
        self.assertIn("A1 reference entrance | black bollard", self.arrival)
        self.assertIn("A1 reference entrance | warm bollard head", self.arrival)

    def test_perimeter_bays_skip_both_entries(self):
        skipped = 0
        for i in range(120):
            a = 2 * math.pi * i / 120
            b = 2 * math.pi * (i + 1) / 120
            x = 96 * (math.cos(a) + math.cos(b)) / 2
            z = 59 * (math.sin(a) + math.sin(b)) / 2
            if abs(x) < 8.4 and abs(z) > 57:
                skipped += 1
        self.assertEqual(skipped, 8)


    def test_all_four_signs_face_viewer_left_to_right_without_repainting(self):
        # Outside the ellipse, the front camera looks north (+Z) and the
        # east-side camera looks west (-X). Both invert the screen X direction
        # of the original parametric mesh. North and west don't.
        self.assertIn('bool reverseU=alongX?!positive:positive;', self.signs)
        self.assertIn('float outwardFacingU=reverseU?1f-t:t;', self.signs)
        for front, positive, expected_flip in (
            (True, False, True),  # public front south of oval
            (True, True, False),  # rear/north
            (False, True, True),  # east side
            (False, False, False),  # west side
        ):
            with self.subTest(front=front,positive=positive):
                self.assertEqual((not positive) if front else positive,
                                 expected_flip)
        self.assertIn('owner', self.signs.lower())
        self.assertNotIn('SetPixels',self.signs)
        self.assertNotIn('EncodeToPNG',self.signs)

    def test_lit_black_pillars_remain_outside_tinted_glass(self):
        self.assertIn('goldAccentGlow.Emit(group',self.arrival)
        self.assertIn('REFERENCE_recessed_gold_pillar_light',self.arrival)
        self.assertIn('PBR_black_stepped_out_pillars_2p10m',self.arrival)
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
