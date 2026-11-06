"""Build 23 regressions: original P1 and beach remain in scene with advisory quality.
Source inspection only; never invokes Unity, downloads assets or builds an APK.
"""
from pathlib import Path
import unittest

ROOT=Path(__file__).resolve().parents[2]

def read(name):
    return (ROOT/name).read_text(encoding="utf-8")

class Build23AdvisoryRegression(unittest.TestCase):
    def test_p1_checks_all_material_slots_without_omitting_real_pool(self):
        src=read("unity-city/Assets/Editor/Mistrzowie/BraxChampionsPoolP1.cs")
        self.assertIn("r.sharedMaterials.Any",src)
        self.assertIn("unfinished.Length>0",src)
        self.assertIn("BraxBuildQuality.Warn(",src)
        self.assertNotIn('throw new InvalidOperationException("P1 has a renderer without a real finish")',src)

    def test_original_beach_tower_stays_after_collision_diagnostic(self):
        src=read("unity-city/Assets/Editor/Mistrzowie/BraxChampionsBeachFurnishings.cs")
        self.assertIn("if(count<=0)",src)
        self.assertIn("BraxBuildQuality.Warn(",src)
        self.assertIn("return tower;",src)
        self.assertNotIn("throw new InvalidOperationException(\"R1 tower imported but measured stair",src)

    def test_scene_audit_reports_every_renderer_not_only_first(self):
        src=read("unity-city/Assets/Editor/Mistrzowie/BraxChampionsBuild.cs")
        self.assertIn("var issues=new List<string>();",src)
        self.assertIn("for(int i=0;i<materials.Length;i++)",src)
        self.assertIn("scene-asset-quality.txt",src)
        self.assertIn("BRAX_MISTRZOWIE_ALL_SCENE_ISSUES",src)
        self.assertNotIn("Missing/invalid material or shader: \"+r.name",src)
        self.assertIn("BuildPipeline.BuildPlayer",src)
        self.assertIn("result.summary.result!=BuildResult.Succeeded",src)

if __name__=="__main__":
    unittest.main()
