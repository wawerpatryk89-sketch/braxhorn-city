"""Source contracts. Actual mesh/physics cases run in StructureRegression in Unity."""
from pathlib import Path
import re
import unittest

ROOT=Path(__file__).resolve().parents[3]
EDITOR=ROOT/'unity-city/Assets/Editor/Oboz'
RUNTIME=ROOT/'unity-city/Assets/Oboz'


class PhysicalConstructionSupport(unittest.TestCase):
    def read(self,name,folder=EDITOR):
        return (folder/name).read_text()

    def test_renderer_membership_is_explicit_and_not_hierarchy_inherited(self):
        record=self.read('BraxObozPlacementRecord.cs',RUNTIME)
        self.assertIn('void SetMembers(Renderer[] renderers)',record)
        self.assertIn('bool Contains(Renderer renderer)',record)
        self.assertNotIn('GetComponentsInChildren',record)
        placement=self.read('BraxObozPlacement.cs')
        self.assertEqual(placement.count('BraxObozPlacementRecord.For(model)'),
                         placement.count('record.SetMembers(renderers)'))

    def test_construction_has_no_name_or_parent_support_exemptions(self):
        audit=self.read('BraxObozStructureAudit.cs')
        self.assertNotIn('GetComponentInParent',audit)
        self.assertNotIn('name.Contains',audit)
        self.assertNotIn('name.StartsWith',audit)
        self.assertIn('TriangleDistanceSquared',audit)
        self.assertIn('SegmentHitsTriangle',audit)
        self.assertIn('CompleteCoverage(renderer,records)',audit)

    def test_saved_records_refer_to_actual_final_mesh_ranges(self):
        record=self.read('BraxObozStructureRecord.cs',RUNTIME)
        for field in ('MeshFilter filter','firstVertex','vertexCount','firstIndex','indexCount','rangesVerified'):
            self.assertIn(field,record)
        authoring=self.read('BraxObozStructureAuthoring.cs')
        self.assertIn('targetVertices',authoring)
        self.assertIn('targetIndices',authoring)
        self.assertIn('part.rangesVerified &= matches',authoring)
        campus=self.read('BraxObozCampus.cs')
        begin=campus.index('static void CombineDecoration')
        remap=campus.index('RemapCombined',begin)
        destroy=campus.index('Object.DestroyImmediate(f.GetComponent<MeshRenderer>())',begin)
        self.assertLess(remap,destroy)

    def test_ground_is_an_explicit_matching_mesh_and_collider(self):
        audit=self.read('BraxObozStructureAudit.cs')
        self.assertIn('ground.surface.sharedMesh==ground.geometry.sharedMesh',audit)
        self.assertIn('ground.surface.Raycast',audit)
        self.assertIn('ground.surface.transform==ground.geometry.transform',audit)
        self.assertIn('RegisterGround(g.GetComponent<MeshCollider>())',self.read('BraxObozEnvironment.cs'))

    def test_unknowns_and_disconnected_parts_are_retained(self):
        audit=self.read('BraxObozStructureAudit.cs')
        for metric in ('unmeasuredPieces','unsupportedPieces','embeddedPieces','invalidGrounds',
                       'unmeasuredPairChecks','completeTriangleCoverage','heightWarnings'):
            self.assertIn(metric,audit)
        self.assertIn('islands',audit)
        self.assertIn('parentPiece=current',audit)
        self.assertIn('sceneGeometryMeasured=true,phoneVerified=false',audit)
        self.assertNotRegex(audit,r'(unsupportedPieces|embeddedPieces|unmeasuredPieces)\s*=\s*0\s*;')

    def test_campfire_uses_real_floor_and_lower_log_rays(self):
        source=self.read('BraxObozCampfireSupport.cs')
        self.assertIn('Physics.RaycastAll',source)
        self.assertIn('log.Raycast(ray,out var hit',source)
        self.assertIn('contacts<2',source)
        self.assertIn('CampfireSolidLog',source)
        self.assertIn('solid.sharedMesh=log.GetComponent<MeshFilter>().sharedMesh',source)
        self.assertIn('foreach(float z in new[]{-.34f,0f,.34f})',source)
        self.assertIn('foreach(float x in new[]{-.34f,.34f})',source)

    def test_bench_height_and_supports_use_measured_geometry(self):
        source=self.read('BraxObozCampfireSupport.cs')
        self.assertIn('floorY+.45f*K-seat.GetComponent<Renderer>().bounds.max.y',source)
        self.assertIn('bottom=seat.GetComponent<Renderer>().bounds.min.y',source)
        self.assertIn('groundHits[end].point.y',source)
        self.assertIn('record.expectedTopMetres=.45f',source)
        self.assertIn('record.heightMembers=new[]{seat.GetComponent<Renderer>()}',source)
        env=self.read('BraxObozEnvironment.cs')
        self.assertIn('CampfireWoodStack(camp)',env)
        self.assertIn('CampfireLogBenches(camp)',env)

    def test_unity_regression_covers_real_meshes_and_serialization(self):
        source=self.read('BraxObozStructureRegression.cs')
        self.assertIn('JsonUtility.FromJsonOverwrite',source)
        self.assertNotIn('EditorJsonUtility.FromJsonOverwrite(',source)
        self.assertIn('combined.CombineMeshes',source)
        self.assertIn('bounds.Intersects',source)
        self.assertIn('unsupportedPieces==1',source)
        self.assertIn('minimumGroundGapMetres<-.09f',source)
        self.assertIn('invalidGrounds==1',source)
        self.assertNotIn('SendMessage(',source)
        self.assertNotIn('BuildPipeline',source)


if __name__=='__main__':
    unittest.main()
