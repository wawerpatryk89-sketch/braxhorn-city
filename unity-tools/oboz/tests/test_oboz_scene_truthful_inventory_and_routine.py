"""Dynamic status and failure recovery never claim complete from source constants."""
import unittest
from pathlib import Path
R=Path(__file__).resolve().parents[3]
A=(R/"unity-city/Assets/Editor/Oboz/BraxObozActors.cs").read_text("utf8")
E=(R/"unity-city/Assets/Editor/Oboz/BraxObozEnvironment.cs").read_text("utf8")
class SceneTruthfulInventory(unittest.TestCase):
    def test_counts_come_from_scene_arrays(self):
        self.assertIn('doors.Count',E)
        self.assertIn('bags.Count',E)
        self.assertIn('covered.Count',E)
        self.assertIn('arenas.Count(r=>r',E)
        self.assertIn('completeCamp',E)
        self.assertNotIn(r'\"cabins\":4,\"bags\":48',E)
    def test_optional_training_does_not_kill_main_scene(self):
        self.assertIn('try { BraxObozTrainingSetup.Configure(info); }',A)
        self.assertIn('catch(Exception ex)',A)
        self.assertIn('BraxBuildQuality.Warn("OBOZ_ROUTINE_SKIPPED',A)
        self.assertIn('NavMeshAgent',A)
        self.assertIn('EditorSceneManager.SaveScene(scene,ScenePath)',A)
