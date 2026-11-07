from pathlib import Path
import unittest

ROOT=Path(__file__).resolve().parents[3]
EDITOR=ROOT/"unity-city/Assets/Editor/Oboz"
CAMPUS=(EDITOR/"BraxObozCampus.cs").read_text("utf8")
SERVICES=(EDITOR/"BraxObozServices.cs").read_text("utf8")
IMPORT=(EDITOR/"BraxObozImport.cs").read_text("utf8")
MATERIALS=(EDITOR/"BraxObozMaterials.cs").read_text("utf8")

class FinalGPolicy(unittest.TestCase):
    def test_bull_is_frozen_after_owner_instruction(self):
        self.assertNotIn("GroundBuiltStructure(monument",CAMPUS)
        self.assertIn("BYK/POMNIK ZAMROŻONY",CAMPUS)
        self.assertIn("RondoBull(rondo);",CAMPUS)

    def test_large_canteen_assets_use_only_original_mesh_lods(self):
        self.assertIn("ConfigureOriginalMeshLod(display,\"witryna stołówki\")",SERVICES)
        self.assertIn("ConfigureOriginalMeshLod(group.gameObject,\"kuchnia stołówki\")",SERVICES)
        self.assertIn("originalGeometryOnly=true",SERVICES)
        self.assertNotIn("ConfigureOriginalMeshLod(model,\"IKEA",SERVICES)
        self.assertNotIn("ConfigureOriginalMeshLod(model,\"worek",SERVICES)

    def test_android_environment_policy_is_2k_astc(self):
        self.assertIn("mobileSource.maxTextureSize=2048",IMPORT)
        self.assertIn("TextureImporterFormat.ASTC_6x6",IMPORT)
        self.assertIn("TextureImporterFormat.ASTC_4x4",IMPORT)
        self.assertIn("androidLimit = 2048",MATERIALS)

    def test_texture_audit_remains_non_phone_estimate(self):
        self.assertIn("phoneMeasured=false",MATERIALS)
        self.assertIn("estimatedAndroidTextureBytesWithMips",MATERIALS)

if __name__=="__main__":
    unittest.main()
