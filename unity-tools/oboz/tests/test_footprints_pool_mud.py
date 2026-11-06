"""Ślady stóp obozu: zachowanie, pula i błoto, bez Unity/APK."""
from pathlib import Path
import unittest
ROOT=Path(__file__).resolve().parents[3]
A=(ROOT/"unity-city/Assets/Editor/Oboz/BraxObozActors.cs").read_text("utf-8")
T=(ROOT/"unity-city/Assets/Oboz/BraxObozFootprintTrail.cs").read_text("utf-8")
class FootprintsContract(unittest.TestCase):
    def test_pool_life_and_original_terrain_mask(self):
        self.assertIn("public const int PoolSize=40",T)
        self.assertIn("public const float LifetimeSeconds=20f",T)
        self.assertIn("MudAt(hits[0])<.28f",T)
        self.assertIn("hit.triangleIndex",T)
        self.assertIn("mesh.colors",T)
        self.assertIn("quad.SetActive(false)",T)
        self.assertIn("if(age>=LifetimeSeconds)",T)
    def test_player_and_all_npcs_and_no_new_4k_texture(self):
        self.assertIn("new Texture2D(w,h,TextureFormat.RGBA32",T)
        self.assertIn("npcs??Array.Empty<BraxObozNpc>()",T)
        self.assertIn("AddComponent<BraxObozFootprintTrail>().Configure(player,npcs)",A)
        self.assertIn("MaterialPropertyBlock",T)
