"""Camp stride calibration wiring; actual sampled behavior is checked in Unity."""
from pathlib import Path
import json
import re
import unittest

R=Path(__file__).resolve().parents[3]
EDITOR=R/"unity-city/Assets/Editor/Oboz"
RUNTIME=R/"unity-city/Assets/Oboz"
C=(EDITOR/"BraxObozLocomotionCalibration.cs").read_text("utf8")
M=(RUNTIME/"BraxObozStrideMatch.cs").read_text("utf8")
A=(EDITOR/"BraxObozActors.cs").read_text("utf8")
REG=(EDITOR/"BraxObozRuntimeRegression.cs").read_text("utf8")

class CampStrideContract(unittest.TestCase):
    def test_owner_height_table_is_used_for_every_camp_actor(self):
        manifest=json.loads((EDITOR/"CharacterManifest.json").read_text("utf8"))
        actors=manifest["characters"]
        self.assertEqual(len(actors),7)
        self.assertEqual({actor["realHeight"] for actor in actors},{1.8})

    def test_measure_original_clips_on_disposable_visual_clone(self):
        self.assertIn("Object.Instantiate(sourceVisual.gameObject",C)
        self.assertIn("clip.SampleAnimation(model",C)
        self.assertIn("TryGetSolePosition",C)
        self.assertIn("AssetDatabase.GetAssetDependencyHash",C)
        self.assertIn("Object.DestroyImmediate(probe)",C)
        self.assertNotIn("clip.SetCurve",C)
        self.assertNotIn("SaveAsPrefabAsset",C)
        self.assertIn("oboz-krok-postaci.json",C)
        self.assertIn("BraxObozLocomotionCalibration.ConfigureAll(info)",A)

    def test_runtime_uses_measured_values_and_actual_movement(self):
        self.assertRegex(M,r"\[SerializeField\]\s+float\s+walkWorldSpeed,runWorldSpeed")
        self.assertIn("position-previousPosition",M)
        self.assertIn("actualSpeed/measuredSpeed",M)
        self.assertNotIn("body.velocity",M)
        for expensive in ("SampleAnimation(","BakeMesh(","FindObjectsByType","SetCurve("):
            self.assertNotIn(expensive,M)
        for fixed_target in ("speed/2f","speed/5f","walkSpeed =","walkSpeed="):
            self.assertNotIn(fixed_target,M)

    def test_pause_jump_action_and_blend_exclusions(self):
        for guard in ("player.Paused","body.isGrounded","npc.Busy","BraxObozArena.AnyActive",
                      "ActionActive()","state.weight>=.999f","Active(walk)&&Active(run)"):
            self.assertIn(guard,M)
        self.assertIn("ReleaseRates()",M)
        self.assertIn("[DefaultExecutionOrder(100)]",M)

    def test_unity_fixture_exercises_known_speed_and_serialized_pool(self):
        self.assertIn("BraxObozLocomotionCalibration.Measure",REG)
        self.assertIn("AnimationCurve.Linear(0,0,1,-2f)",REG)
        self.assertIn("EditorJsonUtility.FromJsonOverwrite",REG)
        self.assertIn('Call(trail,"Start")',REG)
        self.assertIn('Call(trail,"LateUpdate")',REG)
        self.assertIn('Call(mat,"ApplyDeformation"',REG)
        self.assertIn("Physics.Raycast",REG)

    def test_new_scripts_have_unique_stable_meta_guids(self):
        files=[EDITOR/"BraxObozLocomotionCalibration.cs",EDITOR/"BraxObozRuntimeRegression.cs",
               RUNTIME/"BraxObozStrideMatch.cs"]
        guids=[]
        for file in files:
            match=re.search(r"^guid: ([0-9a-f]{32})$",Path(str(file)+".meta").read_text(),re.M)
            self.assertIsNotNone(match)
            guids.append(match[1])
        self.assertEqual(len(set(guids)),len(files))
