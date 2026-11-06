"""Source contract: complete report, explicit no APK build and no CI screenshots."""
from pathlib import Path
import unittest
ROOT=Path(__file__).resolve().parents[3]
EDITOR=ROOT/"unity-city/Assets/Editor/Oboz"

class SceneOnlyAuditContract(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.audit=(EDITOR/"BraxObozSceneAudit.cs").read_text("utf-8")
        cls.imp=(EDITOR/"BraxObozImport.cs").read_text("utf-8")
        cls.env=(EDITOR/"BraxObozEnvironment.cs").read_text("utf-8")

    def test_scene_no_apk_and_full_per_renderer_json(self):
        self.assertIn("BraxObozEnvironment.Prepare();",self.audit)
        self.assertIn("BraxObozActors.Assemble();",self.audit)
        self.assertIn("Physics.RaycastAll",self.audit)
        self.assertIn("GetComponentsInChildren<Renderer>",self.audit)
        self.assertIn("oboz-scena.json",self.audit)
        self.assertIn("oboz-scena.md",self.audit)
        self.assertNotIn("BuildPipeline.BuildPlayer",self.audit.replace("// Bez BuildPipeline.BuildPlayer, bez APK, bez zdjec w CI.",""))

    def test_no_false_apk_bytes(self):
        self.assertIn("BAJTY ŹRÓDŁOWE, NIE ROZMIAR APK",self.audit)
        self.assertIn("apkBuilt=false,phoneVerified=false",self.audit)
        self.assertIn('"cabins',self.env)
        self.assertIn('bags.Count',self.env)
        self.assertIn('covered.Count',self.env)
        self.assertIn('completeCamp',self.env)

    def test_mobile_foliage_cap(self):
        self.assertIn("androidFoliage.maxTextureSize = 2048",self.imp)
        self.assertIn("int androidMax = hero ? 4096 : 2048",self.imp)
