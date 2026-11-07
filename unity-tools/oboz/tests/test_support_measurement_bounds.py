"""Contracts for exact support surfaces and construction-relative heights."""
from pathlib import Path
import unittest

ROOT=Path(__file__).resolve().parents[3]
EDITOR=ROOT/'unity-city/Assets/Editor/Oboz'

class SupportMeasurementBounds(unittest.TestCase):
    def test_height_uses_exact_owner_and_captured_ranges(self):
        source=(EDITOR/'BraxObozStructureAudit.cs').read_text()
        body=source.split('static SurfaceHeight Height(',1)[1].split('public static Result Inspect(Scene scene)',1)[0]
        self.assertIn('g.owner==record',body)
        self.assertIn('record.Contains(r)',body)
        self.assertIn('own.Min(g=>g.vertices.Min(p=>p.y))',body)
        self.assertIn('surfaces.Max(g=>g.vertices.Max(p=>p.y))',body)
        self.assertIn('supportVerified=own.All(g=>g.entry.verified)',body)
        self.assertNotIn('GetComponentsInChildren',body)
        self.assertNotIn('ground.surface.Raycast',body)

    def test_batch_budget_is_distinct_from_island_budget(self):
        source=(EDITOR/'BraxObozStructureAudit.cs').read_text()
        self.assertIn('MaximumTrianglesPerBatch=250000',source)
        self.assertIn('MaximumTrianglesPerIsland=16000',source)
        self.assertIn('MaximumTrianglePairs=2000000',source)
        self.assertLess(source.index('var islands='),source.index('if(list.Count>MaximumTrianglesPerIsland)'))
        for field in ('batchesOverBudget','islandsOverBudget','budgetExceeded="island"','budgetExceeded="batch"'):
            self.assertIn(field,source)

    def test_nonconvex_queries_use_actual_mesh_triangles(self):
        source=(EDITOR/'BraxObozSupportSurface.cs').read_text()
        self.assertIn('if(support is MeshCollider mesh)return MeshSurface',source)
        self.assertIn('collider.sharedMesh',source)
        self.assertIn('collider.transform.TransformPoint(vertices[i])',source)
        self.assertIn('Triangle(point,world[a],world[b],world[c])',source)
        self.assertNotIn('.ClosestPoint(',source)
        self.assertIn('distanceWorld=-1f',source)
        self.assertIn('!support.enabled',source)

    def test_generator_and_existing_bag_regression_use_the_same_measurement(self):
        for filename in ('BraxObozBagAssembly.cs','BraxObozTrainingRegression.cs'):
            source=(EDITOR/filename).read_text()
            self.assertIn('BraxObozSupportSurface.TryMeasure(beam,attachment',source)
            self.assertNotIn('.ClosestPoint(',source)

    def test_real_regressions_are_wired_and_preserve_unknowns(self):
        source=(EDITOR/'BraxObozSupportMeasurementRegression.cs').read_text()
        for case in ('new Vector3(0,.11f,8)*K','islandCount=1400','batch.CombineMeshes',
                     'Grid(90)','new int[250001*3]','!report.heights[0].supportVerified',
                     'surface.convex=false','surface.bounds.Contains(query)','actual.distanceWorld==-1'):
            self.assertIn(case,source)
        self.assertIn('BraxObozSupportMeasurementRegression.Run()',
            (EDITOR/'BraxObozStructureRegression.cs').read_text())
        self.assertNotIn('SendMessage(',source)

if __name__=='__main__':
    unittest.main()
