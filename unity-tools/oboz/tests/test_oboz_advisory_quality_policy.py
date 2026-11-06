"""Owner rule: collision/route/visual QA must be reports, NOT late APK gates.

Static regression only: a green test does not prove Unity compiled or Android ran.
"""
from pathlib import Path
import unittest

ROOT=Path(__file__).resolve().parents[3]
EDITOR=ROOT/"unity-city/Assets/Editor/Oboz"
WORKFLOW=ROOT/".github/workflows/oboz-unity-build.yml"


def read(path):
    return path.read_text(encoding="utf-8")


class CampAdvisoryQualityContract(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.build=read(EDITOR/"BraxObozBuild.cs")
        cls.training=read(EDITOR/"BraxObozTrainingSetup.cs")
        cls.report=read(EDITOR/"BraxObozCollisionReview.cs")
        cls.validation=read(EDITOR/"BraxObozValidation.cs")
        cls.workflow=read(WORKFLOW)

    def test_scene_review_never_throws_on_quality(self):
        self.assertIn("BraxObozCollisionReview.Write(campInfo,destination)",self.build)
        self.assertIn("BraxBuildQuality.Warn(",self.build)
        self.assertNotIn("if (!report.structuralPassed) BraxBuildQuality.Issue",self.build)
        self.assertIn('File.WriteAllText(Path.Combine(output, "scene-validation.json")',self.build)
        self.assertIn('"review-assembly.json"',read(EDITOR/"BraxObozActors.cs"))

    def test_navigation_quality_report_not_build_gate(self):
        self.assertIn('navigation-review.json',self.training)
        self.assertIn("NavMesh.CalculatePath(",self.training)
        self.assertNotIn("if(problems.Length!=0)throw",self.training)
        self.assertIn('BraxBuildQuality.Warn("NAVMESH_CONFIG',self.training)
        self.assertNotIn("BraxBuildQuality.Issue(",self.training)

    def test_collision_inventory_is_not_a_pass_claim(self):
        self.assertIn('ADVISORY_ONLY__DEVICE_REVIEW_REQUIRED',self.report)
        self.assertIn('phoneTested = false',self.report)
        self.assertIn("GetComponentsInChildren<Collider>",self.report)
        self.assertIn("File.WriteAllText(destination",self.report)
        self.assertNotIn("throw new InvalidOperationException",self.report)
        self.assertIn('catch (Exception e)',self.build)

    def test_artifacts_preserved_before_release_upload(self):
        marker='- name: Zapis niezmiennych plików APK do istniejącego draft release'
        self.assertIn(marker,self.workflow)
        upload=self.workflow.split(marker,1)[1].split('- name: Zaszyfrowany zapis scen obozu',1)[0]
        self.assertIn('continue-on-error: true',upload)
        self.assertIn("steps.apk_backup.outcome == 'success'",upload)
        self.assertLess(self.workflow.index('id: apk_backup'),self.workflow.index(marker))
        self.assertIn('build/ObozAndroid/collider-review.json',self.workflow)
        self.assertIn('build/CampLifeReview/navigation-review.json',self.workflow)
        self.assertIn('build/QualityReview/*.json',self.workflow)

    def test_hard_signing_and_compilation_still_verifiable(self):
        self.assertIn('BraxObozBuild.BuildAndroidReview',self.workflow)
        self.assertIn('release_identity.py verify',self.workflow)
        self.assertIn('if-no-files-found: error',self.workflow)
        self.assertIn('BuildPipeline.BuildPlayer(',self.build)


if __name__=="__main__":
    unittest.main()
