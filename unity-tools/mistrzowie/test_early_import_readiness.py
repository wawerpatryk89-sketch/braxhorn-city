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

if __name__=="__main__":
    unittest.main()
