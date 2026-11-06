"""Offline C# contract: all original source imports are checked before slow world build."""
from pathlib import Path
import unittest

ROOT=Path(__file__).resolve().parents[2]

def read(path):
    return (ROOT/path).read_text(encoding="utf-8")

class RealOriginalAssetReadiness(unittest.TestCase):
    def test_fail_early_before_foundation_geometry(self):
        build=read("unity-city/Assets/Editor/Mistrzowie/BraxChampionsBuild.cs")
        for marker in [
            "BraxChampionsCasinoPbr2K.VerifyOriginalSourceMaps();",
            "BraxChampionsCasinoT11.VerifyOriginalSigns();",
            "BraxChampionsSourceReadiness.VerifyBeforeScene();"
        ]:
            self.assertIn(marker,build)
            self.assertLess(build.index(marker),build.index("BraxChampionsFoundation.Generate();"))

    def test_checks_original_assets_not_placeholders(self):
        code=read("unity-city/Assets/Editor/Mistrzowie/BraxChampionsSourceReadiness.cs")
        for item in ("RealModel(", "BraxChampionsCarBuilder.OwnerCar",
                     "shrub_03_2k_embedded.glb", "quiver_tree_02_2k_embedded.glb",
                     "BraxOriginal.glb","Entering Car.fbx","Driving.fbx",
                     "Exiting Car.fbx", "Idle","Walking","Running","Jab Cross (1)",
                     "Mma Kick (1)","Jump","AnimationUtility.GetCurveBindings"):
            self.assertIn(item,code)
        self.assertIn("GetComponentsInChildren<MeshFilter>(true)",code)
        self.assertIn("GetComponentsInChildren<SkinnedMeshRenderer>(true)",code)
        self.assertNotIn("GameObject.CreatePrimitive",code)

    def test_original_sign_artwork_dimensions_not_imported_android_size(self):
        casino=read("unity-city/Assets/Editor/Mistrzowie/BraxChampionsCasinoT11.cs")
        self.assertIn("importer.GetSourceTextureWidthAndHeight(out int width,out int height)",casino)
        self.assertIn("if(width!=2048||height!=512)",casino)
        self.assertIn("BraxBuildQuality.Warn(",casino)
        self.assertIn("LoadOwnerSign(SignPath)",casino)
        self.assertIn("LoadOwnerSign(source)",casino)
        self.assertIn("VerifyOriginalSigns()",casino)
        self.assertIn("shrub_03_2k_embedded.glb",casino)


    def test_expensive_unity_starts_only_after_verifying_real_polyhaven_files(self):
        workflow=read(".github/workflows/mistrzowie-apk.yml")
        required=[
            "python3 unity-tools/mistrzowie/prepare_casino_textures_2k.py",
            "python3 unity-tools/mistrzowie/prepare_casino_vegetation_2k.py",
            "python3 unity-tools/mistrzowie/verify_t11_real_sources.py",
            "python3 unity-tools/mistrzowie/verify_native_vegetation_glb.py",
            "name: Unity 6 - Miasto Mistrzów, Brax i APK do zwiedzania",
        ]
        for command in required:
            self.assertIn(command,workflow)
        for earlier,later in zip(required,required[1:]):
            self.assertLess(workflow.index(earlier),workflow.index(later),
                            "Source integrity must be checked before Unity: "+earlier)

if __name__=="__main__":
    unittest.main()
