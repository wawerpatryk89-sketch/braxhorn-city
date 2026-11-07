"""Numerical source contracts for the specific measured A-scene defects.

The real polygon clipper, volume mesh and collision winding are exercised by
BraxObozConstructionRegression in Unity; these tests do not claim that run.
"""
import ast
import json
import math
from pathlib import Path
import re
import unittest

ROOT = Path(__file__).resolve().parents[3]
EDITOR = ROOT / 'unity-city/Assets/Editor/Oboz'
ARENA = (EDITOR / 'BraxObozArenaConstruction.cs').read_text()
TRACK = (EDITOR / 'BraxObozTrackGeometry.cs').read_text()
TRAINING = (EDITOR / 'BraxObozTrainingSetup.cs').read_text()
CAMPUS = (EDITOR / 'BraxObozCampus.cs').read_text()
ENVIRONMENT = (EDITOR / 'BraxObozEnvironment.cs').read_text()
K = 1.294


def constant(source, name):
    expression = re.search(r'const (?:float|int) ' + name + r'\s*=\s*([^;]+);', source).group(1)
    expression = re.sub(r'(?<=[\d.])f\b', '', expression)
    def read(node):
        if isinstance(node, ast.Expression): return read(node.body)
        if isinstance(node, ast.Constant): return node.value
        if isinstance(node, ast.Name): return constant(source, node.id)
        if isinstance(node, ast.UnaryOp) and isinstance(node.op, ast.USub): return -read(node.operand)
        if isinstance(node, ast.BinOp):
            a, b = read(node.left), read(node.right)
            if isinstance(node.op, ast.Add): return a + b
            if isinstance(node.op, ast.Sub): return a - b
            if isinstance(node.op, ast.Mult): return a * b
            if isinstance(node.op, ast.Div): return a / b
        raise ValueError('Non-numeric C# constant: ' + expression)
    return read(ast.parse(expression, mode='eval'))


class ArenaConstructionDimensions(unittest.TestCase):
    def test_ring_risers_fit_existing_navmesh_and_controller(self):
        climb = float(re.search(r'settings.agentClimb=([.\d]+)f', TRAINING).group(1))
        top = constant(ARENA, 'RingPlatformHeight') + constant(ARENA, 'RingMatThickness')
        rise = top / constant(ARENA, 'RingStairCount')
        self.assertGreater(.25 * K, climb, 'The historical failure must be reproduced numerically')
        self.assertLess(rise * K, climb - .02)
        self.assertLess(rise, .25, 'The current CharacterController stepOffset is .25*K')
        self.assertLess(math.degrees(math.atan(rise / constant(ARENA, 'RingStairRun'))), 40)
        self.assertAlmostEqual(top, 1.04)

    def test_clear_ring_dimension_and_actual_entry_width(self):
        clear = 2 * constant(ARENA, 'RingRopeAxis') - 2 * constant(ARENA, 'RingRopeRadius')
        self.assertAlmostEqual(clear, 6.0)
        radius = float(re.search(r'settings.agentRadius=([.\d]+)f', TRAINING).group(1))
        entry = (2 * constant(ARENA, 'ArenaEntryHalf') - .13) * K
        stairs = constant(ARENA, 'RingStairWidth') * K
        self.assertGreater(entry, 2 * radius + .2)
        self.assertGreater(stairs, 2 * radius + .2)

    def test_side_stair_does_not_run_into_west_spectator_benches(self):
        # Independent physical clearance: stair z spans +/-.70; the nearest
        # tangent log bench starts at 2.70 - 1.20 = 1.50 m on either side.
        half_stair = constant(ARENA, 'RingStairWidth') / 2
        self.assertGreater(2.7 - 1.2 - half_stair, .5)
        self.assertIn('Ring("Ring 1", -23, 15)', ENVIRONMENT)
        self.assertIn('Ring("Ring 2",-12,51)', CAMPUS)
        self.assertIn('Octagon(0, -5)', ENVIRONMENT)

    def test_track_layers_have_grounded_base_and_flush_crossing(self):
        surface = constant(TRACK, 'TrackSurfaceY')
        wear = constant(TRACK, 'TrackWearThickness')
        walkway = constant(TRACK, 'WalkwaySurfaceY')
        run = constant(TRACK, 'TrackApproachRun')
        self.assertEqual(surface, .09, 'Retained approach road top')
        self.assertGreater(surface - wear, 0)
        self.assertGreater(wear, 0)
        self.assertAlmostEqual(walkway, .016, msg='Original connected walkway level')
        self.assertLess((surface - walkway) / run, .08)
        self.assertAlmostEqual(constant(TRACK, 'actualLengthMetres'), 100)
        self.assertAlmostEqual(constant(TRACK, 'actualLaneWidthMetres'), 1.5)

    def test_sprint_distance_markers_fit_supported_width(self):
        lane_width = constant(TRACK, 'actualLaneWidthMetres')
        half = 4 * lane_width / 2
        centre = half - .20
        marker_width = .23
        self.assertLess(centre + marker_width / 2, half)
        self.assertGreater(centre - marker_width / 2, half - lane_width)

    def test_actual_clipper_has_independent_area_and_mesh_regression(self):
        regression = (EDITOR / 'BraxObozConstructionRegression.cs').read_text()
        cases = re.findall(r'\("[^"]+",[-.\df,]+\)', regression)
        self.assertEqual(len(cases), 10)
        # Those expected results use a separate halfspace-intersection method;
        # the CI regression calls production code and measures its real mesh.
        for call in ('OutsideRunningTracks(polygon)', 'PolygonVolume(frame.transform',
                     'volume.GetComponent<MeshFilter>().sharedMesh', 'mesh.triangles',
                     'ClipOvalAtSprint(meetingSector)', 'CreateSprintSurface(sprintFrame,material)',
                     'sprintBounds.size.z-100f*k'):
            self.assertIn(call, regression)

    def test_sprint_is_100_before_k_and_overlapping_oval_is_clipped(self):
        self.assertIn('length=actualLengthMetres,', TRACK)
        self.assertNotIn('actualLengthMetres/K', TRACK)
        self.assertIn('foreach(var piece in ClipOvalAtSprint(polygon))', TRACK)
        self.assertIn('SubtractConvex(polygon,straightInOval)', TRACK)
        self.assertIn('halfLength=50f,halfWidth=3f', CAMPUS)
        self.assertIn('P(68,0,-24)', TRACK)
        # The corners are inside a convex ellipse, so the full rectangle is.
        for x in (65,71):
            for z in (-74,26):
                self.assertLess((x/90)**2+((z+22)/103)**2, 1)
        layout=json.loads((EDITOR/'CampusLayout.json').read_text())
        for kind in ('clearances','lawnClearances'):
            sprint=next(zone for zone in layout[kind] if zone['id']=='bieznia-100m')
            self.assertEqual((sprint['x'],sprint['z']),(68,-24))
            self.assertGreaterEqual(sprint['depth'],102)
            self.assertGreaterEqual(sprint['width'],7)
        # The sign's two .12 m log feet also remain clear of both surfaces.
        self.assertIn('new Vector3(61,2f,-70)',TRACK)
        for post_x in (61-6*.42,61+6*.42):
            self.assertLess(post_x+.12,65)
            self.assertLess(((post_x+.12)/81.2)**2+((-70+.08-.12+22)/94.2)**2,1)

    def test_manual_placement_records_snapshot_their_actual_members(self):
        self.assertIn('record.SetMembers(go.GetComponents<Renderer>())',ARENA)
        self.assertIn('ovalRecord.SetMembers(oval.GetComponentsInChildren<Renderer>(true))',TRACK)
        self.assertIn('straightRecord.SetMembers(straight.GetComponentsInChildren<Renderer>(true))',TRACK)

    def test_asymmetric_grounding_preserves_plan_and_cylinder_support_is_queryable(self):
        self.assertIn('new Vector3(bounds.center.x,floorWorldY,bounds.center.z)',ARENA)
        self.assertNotIn('OnGround(ring,visible,P(x,0,z)',ARENA)
        self.assertIn('contact.convex=true',ARENA)
        regression=(EDITOR/'BraxObozConstructionRegression.cs').read_text()
        self.assertIn('Mathf.Abs(after.x-before.x)',regression)
        self.assertIn('Mathf.Abs(after.z-before.z)',regression)

    def test_no_model_substitute_added_for_missing_arena_equipment(self):
        self.assertIn('OBÓZ BRAK MODELU — H: beczki z ogniem, gong', ARENA)
        self.assertIn('OBÓZ BRAK MODELU — H: oprawa reflektora areny', ARENA)
        for placeholder in ('Box(cage,"Beczka', 'Box(cage,"Gong', 'Box(group,"Oprawa'):
            self.assertNotIn(placeholder, ARENA)


if __name__ == '__main__':
    unittest.main()
