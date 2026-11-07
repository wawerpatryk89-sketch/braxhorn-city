"""Miasto R1: preserve owner's 2K-near / camera-distance mip policy.

Source-only regression. Unity/Android and installed APK are NOT exercised.
"""
import re
import unittest
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
GRAPHICS=(ROOT/"unity-city/Assets/Claude/BraxGrafika.cs").read_text(encoding="utf-8")
BUILD=(ROOT/"unity-city/Assets/Editor/BraxGymBuild.cs").read_text(encoding="utf-8")
ZONES=(ROOT/"unity-city/Assets/Claude/BraxObszary.cs").read_text(encoding="utf-8")

class CityR1MobileStreaming(unittest.TestCase):
    def test_2048_import_nearby_without_1k_android_cap(self):
        self.assertIn("maxClosePixels=target==BuildTarget.Android?2048:1024", BUILD)
        self.assertIn("ps.maxTextureSize=maxClosePixels", BUILD)
        self.assertNotIn("ps.maxTextureSize=1024;", BUILD)

    def test_mobile_full_mip_nearby_and_distance_streaming(self):
        self.assertIn("if (Application.isMobilePlatform) n = 0;", GRAPHICS)
        self.assertIn("QualitySettings.streamingMipmapsActive = true;", GRAPHICS)
        self.assertIn("QualitySettings.streamingMipmapsAddAllCameras = true;", GRAPHICS)
        self.assertIn("SystemInfo.supportsMipStreaming", GRAPHICS)
        self.assertIn("QualitySettings.streamingMipmapsMemoryBudget =", GRAPHICS)
        self.assertNotIn("Mathf.Max(n, 1)", GRAPHICS)

    def test_loader_never_retains_lock_on_failed_scene_request(self):
        self.assertIn("try { op = SceneManager.LoadSceneAsync(o.scena, LoadSceneMode.Additive); }", ZONES)
        self.assertIn("if (op == null)", ZONES)
        self.assertIn("o.bladLadowania = true;", ZONES)
        self.assertIn("finally\n        {\n            Laduje = Mathf.Max(0, Laduje - 1);", ZONES)
        self.assertIn("!o.bladLadowania", ZONES)
        self.assertIn("if (zamrozony && player && player.body) player.body.enabled = true;", ZONES)

if __name__=="__main__":
    unittest.main()
