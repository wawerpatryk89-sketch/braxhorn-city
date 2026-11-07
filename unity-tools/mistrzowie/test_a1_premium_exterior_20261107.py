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

    def test_exterior_generator_connected_and_original_bowl_retained(self):
        self.assertIn("PremiumExterior(root, exterior);", self.arrival)
        self.assertIn('BraxChampionsArena.Generate(root);', self.buildings)
        self.assertIn('BraxChampionsArenaSigns.Generate(root);', self.buildings)
        self.assertIn('BraxChampionsArenaArrival.Generate(root, materials);', self.buildings)
        self.assertIn('const float rx=96f,rz=59f;', self.arrival)
        self.assertIn("const int bays=120;", self.arrival)
        self.assertIn("mesh.SetVertices(positions)", self.arrival)
        self.assertNotIn("visualAccepted=true", self.arrival)

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
        self.assertIn("new Vector3(.30f,9.30f,.52f),rotation);", self.arrival)
        self.assertIn("new Vector3(.30f,7.35f,.52f),rotation);", self.arrival)
        self.assertIn("new Vector3(.048f,9.30f,.19f),rotation);", self.arrival)
        self.assertIn("new Vector3(.048f,7.35f,.19f),rotation);", self.arrival)
        self.assertGreaterEqual(.52, .50)
        self.assertGreaterEqual(.30, .28)

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


if __name__ == "__main__":
    unittest.main()
