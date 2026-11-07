"""R1 source contracts: manual Unity 6 scene-only run, never an APK build.

These tests validate routing and safety, not the Unity-generated world itself.
The actual scene audit requires workflow_dispatch(scene_only=true).
"""
from pathlib import Path
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]
BUILD = (ROOT / "unity-city/Assets/Editor/BraxGymBuild.cs").read_text(encoding="utf-8")
QA = (ROOT / "unity-city/Assets/Editor/Claude/BraxR1SceneAcceptance.cs").read_text(encoding="utf-8")
YML = (ROOT / ".github/workflows/claude-region-build.yml").read_text(encoding="utf-8")
SHOP = (ROOT / "unity-city/Assets/Editor/Claude/Region/BraxSupermarket.cs").read_text(encoding="utf-8")


def brackets_balanced(code):
    code = re.sub(r'//[^\n]*|/\*[\s\S]*?\*/|@"(?:""|[^"])*"|"((?:\\.|[^"\\])*)"', "", code)
    stack = []
    matching = {")": "(", "]": "[", "}": "{"}
    for ch in code:
        if ch in "([{":
            stack.append(ch)
        elif ch in ")]}":
            if not stack or stack.pop() != matching[ch]:
                return False
    return not stack


class R1UnitySceneOnlySourceContract(unittest.TestCase):
    def test_brackets_editor_and_store(self):
        self.assertTrue(brackets_balanced(BUILD))
        self.assertTrue(brackets_balanced(QA))
        self.assertTrue(brackets_balanced(SHOP))

    def test_real_city_generator_is_reused_not_faked(self):
        self.assertIn("public static void SceneOnlyCityR1()", BUILD)
        self.assertIn("BuildInternal(true,true,true,BuildTarget.WebGL,true)", BUILD)
        self.assertIn("BraxCityBuild.AttachCity(runtime,integrated,renovation)", BUILD)
        self.assertIn("BraxSupermarket.Wykonaj(scene,2)", BUILD)
        self.assertIn("EditorSceneManager.SaveScene(scene,scenePath)", BUILD)

    def test_scene_only_bypasses_stale_cache(self):
        self.assertIn('string rejon=sceneOnly?"miasto"',BUILD)
        self.assertIn('if(!sceneOnly&&rejon!="oba"&&BraxRejonBuildu.ZPamieci', BUILD)

    def test_editor_receipt_before_scene_partition(self):
        self.assertIn("BraxR1SceneAcceptance.Run(scene)", BUILD)
        self.assertLess(BUILD.index("BraxR1SceneAcceptance.Run(scene)"),
                        BUILD.index('EditorSceneManager.SaveScene(scene,scenePath)'))

    def test_scene_only_returns_before_any_player_package(self):
        end = BUILD.index('if(sceneOnly)\n        {')
        pkg = BUILD.index('Paczka(rejon,fullCity,integrated,renovation,target,scenePath,scenePaths,sourceCommit,report,t0,false);',end)
        self.assertLess(end, pkg)
        self.assertIn("return;", BUILD[end:pkg])
        self.assertIn("BraxBrakujaceSkrypty.Sprawdz(scenePaths)",BUILD[end:pkg])
        self.assertIn("Assets/PlayableGenerated/R1SceneAcceptance", QA)

    def test_apk_disabled_in_manual_scene_only_dispatch(self):
        self.assertIn("scene_only:", YML)
        self.assertIn("default: false",YML)
        self.assertIn("github.event.inputs.scene_only == 'false'", YML)
        self.assertIn("github.event.inputs.scene_only == 'true'", YML)
        self.assertIn("buildMethod: BraxGymBuild.SceneOnlyCityR1",YML)
        self.assertIn("targetPlatform: WebGL",YML)

    def test_manual_only_workflow_is_one_entrypoint(self):
        self.assertIn("  workflow_dispatch:",YML)
        self.assertNotIn("  push:",YML[:YML.index("jobs:")])
        self.assertNotIn("  pull_request:",YML[:YML.index("jobs:")])
        self.assertNotIn("auto-approve",YML)

    def test_actual_unity_receipt_reports_specific_failures(self):
        self.assertIn("public static void Run(Scene scene)",QA)
        self.assertIn("r1-transport-sklep.json",QA)
        self.assertIn("Physics.OverlapCapsule(",QA)
        self.assertIn("BraxRuchAutSym.Prost(",QA)
        self.assertIn("t.AuditDoorNodes()",QA)
        self.assertIn("northParkingSpots=parking",QA)
        self.assertIn("pierwszy_bląd=",QA)
        self.assertIn("NIEGOTOWE",QA)

    def test_static_source_cannot_claim_unity_was_run(self):
        self.assertIn("PlayMode/telefon niepotwierdzony",QA)
        self.assertIn("bez_APK=true bez_WebGL=true",BUILD)
        self.assertNotIn("BuildPipeline.BuildPlayer(", BUILD[BUILD.index('if(sceneOnly)\n        {'):BUILD.index('Paczka(rejon,fullCity,integrated,renovation,target,scenePath,scenePaths,sourceCommit,report,t0,false);')])

    def test_false_tree_detection_does_not_match_street(self):
        matcher = re.search(r"bool IsTree\(string x\)\s*=>\s*(.*);", SHOP)
        self.assertIsNotNone(matcher)
        self.assertNotIn('"tree",', matcher.group(1))
        self.assertIn('"tree_"',matcher.group(1))
        self.assertIn('"drzewo"',matcher.group(1))
        self.assertIn('"palm_"',matcher.group(1))


if __name__ == "__main__":
    unittest.main()
