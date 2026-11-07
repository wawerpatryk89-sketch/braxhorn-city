"""Wyrównanie NPC do 1m pomostu i maty przy 1.04m; bez apk."""
import unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
A=(ROOT/"unity-city/Assets/Editor/Oboz/BraxObozActors.cs").read_text("utf8")
T=(ROOT/"unity-city/Assets/Editor/Oboz/BraxObozTrainingSetup.cs").read_text("utf8")
E=(ROOT/"unity-city/Assets/Editor/Oboz/BraxObozArenaConstruction.cs").read_text("utf8")
class RingHeight(unittest.TestCase):
    def test_ring_player_and_actors_agree(self):
        self.assertIn('new Vector3(0,1.02f,0)',E)
        self.assertIn('new Vector3(-12,1.04f,51)',A)
        self.assertIn('new Vector3(-24,1.04f,15)',A)
        self.assertIn('i<2?.38f:1.04f',T)
        self.assertIn('NavMesh.SamplePosition(place.position',T)
