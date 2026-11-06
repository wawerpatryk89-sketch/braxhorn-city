"""Ring and gym mats sink 3cm within 0.4m (runtime mesh only)."""
from pathlib import Path
import unittest

R=Path(__file__).resolve().parents[3]
MAT=(R/"unity-city/Assets/Oboz/BraxObozDeformableMat.cs").read_text("utf-8")
ENV=(R/"unity-city/Assets/Editor/Oboz/BraxObozEnvironment.cs").read_text("utf-8")
CAMP=(R/"unity-city/Assets/Editor/Oboz/BraxObozCampus.cs").read_text("utf-8")
class DeformableMatContract(unittest.TestCase):
    def test_metre_radius_depth_and_topology(self):
        self.assertIn("const float ContactRadius=.4f*K",MAT)
        self.assertIn("const float MaxDepth=.03f*K",MAT)
        self.assertIn("mesh.MarkDynamic()",MAT)
        self.assertIn("dynamicMesh.vertices=liveVertices",MAT)
        self.assertIn("Mathf.Clamp01(rim)",MAT)
        self.assertIn("FindObjectsByType<BraxObozNpc>",MAT)
        self.assertIn("FindObjectsByType<BraxGymPlayer>",MAT)
    def test_surface_applied_to_all_expected_zones(self):
        self.assertIn("ringMat.AddComponent<BraxObozDeformableMat>()",ENV)
        self.assertIn("gymMat.AddComponent<BraxObozDeformableMat>()",CAMP)
        self.assertIn("mat.AddComponent<BraxObozDeformableMat>()",CAMP)
        self.assertNotIn("Destroy(GetComponent<BoxCollider>())",MAT)
