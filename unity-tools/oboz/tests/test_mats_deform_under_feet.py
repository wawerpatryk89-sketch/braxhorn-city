"""Mat geometry and source contracts; rendered motion still requires Unity Play Mode."""
from pathlib import Path
import re
import unittest

R=Path(__file__).resolve().parents[3]
MAT=(R/"unity-city/Assets/Oboz/BraxObozDeformableMat.cs").read_text("utf-8")
ENV=(R/"unity-city/Assets/Editor/Oboz/BraxObozEnvironment.cs").read_text("utf-8")
CAMP=(R/"unity-city/Assets/Editor/Oboz/BraxObozCampus.cs").read_text("utf-8")
CONTACT=(R/"unity-city/Assets/Oboz/BraxObozIdleFootContact.cs").read_text("utf-8")

class DeformableMatContract(unittest.TestCase):
    def test_owner_radius_depth_and_measured_contacts(self):
        radius=re.search(r"const\s+float\s+ContactRadius\s*=\s*([.\d]+)f\s*\*\s*K",MAT)
        depth=re.search(r"const\s+float\s+MaxDepth\s*=\s*([.\d]+)f\s*\*\s*K",MAT)
        self.assertEqual(float(radius[1]),.4)
        self.assertEqual(float(depth[1]),.03)
        self.assertIn("mesh.MarkDynamic()",MAT)
        self.assertIn("dynamicMesh.vertices=liveVertices",MAT)
        self.assertIn("TryGetSolePosition",MAT)
        self.assertIn("contact.body.isGrounded",MAT)
        self.assertIn("transform.InverseTransformPoint(world)",MAT)
        self.assertNotIn("feet.Where",MAT)
        self.assertNotIn(".ToArray()",MAT)
        self.assertIn("Mathf.Max(deepest",MAT,"Foot overlap may not add beyond 3 cm")
        self.assertIn("TryGetSurfaceHeight",MAT)
        self.assertIn("floorMat.TryGetSurfaceHeight",CONTACT)
        self.assertIn("GroundedCorrection(RenderedGroundHeight(ground),soleY)",CONTACT)

    def test_actual_side_triangles_face_outward(self):
        calls=re.findall(r"Face\((new Vector3\(.*?\))\);",MAT,re.S)
        self.assertEqual(len(calls),5)
        for call in calls:
            points=[tuple(float(v.removesuffix('f')) for v in m.split(','))
                    for m in re.findall(r"new Vector3\(([-.\df,]+)\)",call)]
            self.assertEqual(len(points),4)
            a,b,c,d=points
            ab=[b[i]-a[i] for i in range(3)];ac=[c[i]-a[i] for i in range(3)]
            normal=(ab[1]*ac[2]-ab[2]*ac[1],ab[2]*ac[0]-ab[0]*ac[2],ab[0]*ac[1]-ab[1]*ac[0])
            center=[sum(p[i] for p in points)/4 for i in range(3)]
            self.assertGreater(sum(normal[i]*center[i] for i in range(3)),0)

    def test_surface_applied_and_original_mesh_owned_safely(self):
        self.assertIn("ringMat.AddComponent<BraxObozDeformableMat>()",ENV)
        self.assertIn("gymMat.AddComponent<BraxObozDeformableMat>()",CAMP)
        self.assertIn("mat.AddComponent<BraxObozDeformableMat>()",CAMP)
        self.assertIn("originalMesh=filter.sharedMesh",MAT)
        self.assertIn("filter.sharedMesh=originalMesh",MAT)
        self.assertIn("Destroy(dynamicMesh)",MAT)
        self.assertIn("mesh.RecalculateTangents()",MAT)
        self.assertNotIn("Destroy(GetComponent<BoxCollider>())",MAT)
        self.assertNotIn("sharedMesh.vertices",MAT)
