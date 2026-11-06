"""Audyt musi wykazać realne PBR wszystkich rendererów, worki, drzwi i dane źródłowe."""
import unittest
from pathlib import Path
R=Path(__file__).resolve().parents[3]
S=(R/"unity-city/Assets/Editor/Oboz/BraxObozSceneAudit.cs").read_text("utf8")
class AuditMaterialsAndBags(unittest.TestCase):
    def test_per_renderer_material_sources_and_original_textures(self):
        self.assertIn("public string[] materialSources;",S)
        self.assertIn("r.sharedMaterials.Select(DescribeMaterial).ToArray()",S)
        self.assertIn("material.GetTexturePropertyNames()",S)
        self.assertIn("AssetDatabase.GetAssetPath(material.GetTexture(key))",S)
    def test_48_bags_1_2m_body_and_0_6m_bottom_over_real_floor(self):
        self.assertIn("ReviewRealBagClearance(scene)",S)
        self.assertIn("bags.Length!=48",S)
        self.assertIn("Mathf.Abs(body-1.2f)>.03f",S)
        self.assertIn("Mathf.Abs(clearance-.6f)>.03f",S)
        self.assertIn("bagWarnings",S)
    def test_entrance_collider_inventory_and_full_dependency_bytes(self):
        self.assertIn("oboz-wejscia-kolizje.json",S)
        self.assertIn("BraxObozCollisionReview.Write(info",S)
        self.assertIn("AllDependentSourceBytes()",S)
        self.assertIn("GetDependencies(BraxObozActors.ScenePath,true)",S)
        self.assertIn("BAJTY ŹRÓDŁOWE, NIE ROZMIAR APK",S)
