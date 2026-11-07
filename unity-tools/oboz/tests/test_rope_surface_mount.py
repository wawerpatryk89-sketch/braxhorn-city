"""Actual rope/post surface ownership; Unity exercises the generated meshes."""
from pathlib import Path
import unittest

ROOT=Path(__file__).resolve().parents[3]
EDITOR=ROOT/'unity-city/Assets/Editor/Oboz'

class RopeSurfaceMount(unittest.TestCase):
    def test_attachment_comes_from_rendered_rope_edges_and_real_support(self):
        source=(EDITOR/'BraxObozArenaConstruction.cs').read_text()
        method=source.split('internal static bool TryRopePostSurfaceContact',1)[1].split('static void ArenaRopes',1)[0]
        for requirement in ('mesh.vertices','mesh.triangles','filter.transform.TransformPoint',
                            'post.Raycast','Vector3.Lerp(start,end','BraxObozSupportSurface.TryMeasure'):
            self.assertIn(requirement,method)
        self.assertNotIn('.ClosestPoint(',method)
        arena=source.split('static void ArenaRopes',1)[1].split('static void BuildTrainingOctagon',1)[0]
        self.assertIn('TryRopePostSurfaceContact(line,post',arena)
        self.assertIn('go.transform.InverseTransformPoint(contact)',arena)
        self.assertIn('record.suspensionPoint=null',arena)
        self.assertIn('BraxBuildQuality.Warn',arena)
        self.assertNotIn('side*line.promien',arena)

    def test_shared_rope_remains_source_of_actual_geometry(self):
        source=(EDITOR/'BraxObozRopeMountRegression.cs').read_text()
        for requirement in ('AddComponent<BraxLina>()','line.Zbuduj()',
                            'new[]{false,true}','oldDistance.distanceWorld>.05f*K',
                            'TryMeasure(ropeSurface,contact','3*K','sharedMesh=null'):
            self.assertIn(requirement,source)
        self.assertIn('BraxObozRopeMountRegression.Run()',
            (EDITOR/'BraxObozStructureRegression.cs').read_text())
        self.assertNotIn('SendMessage(',source)

if __name__=='__main__':
    unittest.main()
