"""NOVA source-contract and numerical tests. Not a Unity compile/scene/phone test.

The box subtraction test evaluates the six Add expressions read from the actual
C# implementation, rather than a separately hard-coded list of slabs.
Run from unity-tools/claude: python -m unittest test_nova_entrance_finish -v
"""
import itertools
import math
from pathlib import Path
import random
import re
import unittest
from dataclasses import dataclass

ROOT = Path(__file__).resolve().parents[2]
EDITOR = ROOT / "unity-city/Assets/Editor/Claude"
SOURCE = (EDITOR / "Region/BraxNovaEntranceFinish.cs").read_text(encoding="utf-8")
HOOK = (EDITOR / "BraxClaudeSiatki.cs").read_text(encoding="utf-8")


@dataclass(frozen=True)
class V:
    x: float
    y: float
    z: float

    def values(self):
        return (self.x, self.y, self.z)


@dataclass(frozen=True)
class B:
    min: V
    max: V

    @property
    def volume(self):
        return math.prod(max(0, b-a) for a, b in zip(self.min.values(), self.max.values()))

    def contains(self, p):
        return all(a <= q <= b for a, q, b in zip(self.min.values(), p.values(), self.max.values()))


def vec_min(a, b):
    return V(*map(min, a.values(), b.values()))


def vec_max(a, b):
    return V(*map(max, a.values(), b.values()))


def overlaps(a, b):
    # Same interval test as the C# code: flat mesh planes inside count as blockers.
    return all(x < q-1e-5 and y > p+1e-5 for x, y, p, q in
               zip(a.min.values(), a.max.values(), b.min.values(), b.max.values()))


def bounds_constant(name):
    match = re.search(rf"Bounds {name} = Box\(([^;]+)\);", SOURCE)
    assert match, name
    args = re.sub(r"(?<=[0-9.])f\b", "", match.group(1)).replace("Floor", ".15")
    # Fixed numeric expressions from the checked-in constants only.
    assert re.fullmatch(r"[0-9.,+\-\s]+", args)
    x0, x1, y0, y1, z0, z1 = eval(f"({args})", {"__builtins__": {}}, {})
    return B(V(x0, y0, z0), V(x1, y1, z1))


PASSAGE = bounds_constant("Passage")
SIGN = bounds_constant("OldLetters")


def subtract(b, cut):
    if not overlaps(b, cut):
        return [b]
    lo, hi = vec_max(b.min, cut.min), vec_min(b.max, cut.max)
    code = SOURCE.split("internal static List<Bounds> Remainders", 1)[1].split("static bool CutBox", 1)[0]
    expressions = re.findall(r"^        Add\((.+)\);$", code, re.M)
    if len(expressions) != 6:
        raise AssertionError("Expected six disjoint outside slabs in C# implementation")
    output = []
    for expression in expressions:
        expression = expression.replace("new Vector3", "V")
        if not re.fullmatch(r"[a-zA-Z0-9_.,()\s]+", expression):
            raise AssertionError("Unexpected expression in C# slab algorithm")
        a, z = eval(f"({expression})", {"__builtins__": {}}, dict(b=b, lo=lo, hi=hi, V=V))
        if all(y-x > 1e-5 for x, y in zip(a.values(), z.values())):
            output.append(B(a, z))
    return output


def intersection(a, b):
    return B(vec_max(a.min, b.min), vec_min(a.max, b.max))


def masked_csharp(code):
    # Lexical delimiter check only. Deliberately NOT advertised as C# compilation.
    return re.sub(r'//[^\n]*|/\*[\s\S]*?\*/|@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'', '', code)


class NovaGeometryTests(unittest.TestCase):
    def test_floor_and_door_frame_are_outside_cut(self):
        floor = B(V(38.2, -.15, 35.9), V(45, .15, 41.6))
        left = B(V(34.36, .15, 35.95), V(40.8, 5.05, 36.5))
        right = B(V(43.2, .15, 35.95), V(52, 5.05, 36.5))
        lintel = B(V(40.8, 3.13, 35.95), V(43.2, 5.05, 36.5))
        for b in (floor, left, right, lintel):
            self.assertFalse(overlaps(b, PASSAGE))
            self.assertEqual(subtract(b, PASSAGE), [b])

    def test_cloakroom_and_ticket_counter_are_preserved(self):
        for b in (B(V(39.5, .15, 37.2), V(40.11, 1.509, 40.8)),
                  B(V(44.25, .15, 37), V(45, 1.509, 38.8))):
            self.assertEqual(subtract(b, PASSAGE), [b])

    def test_flat_mesh_barrier_is_not_missed(self):
        wall = B(V(40.8, .15, 36.4), V(43.2, 3, 36.4))
        self.assertEqual(wall.volume, 0)
        self.assertTrue(overlaps(wall, PASSAGE))
        self.assertIn('a.min.z < b.max.z', SOURCE)
        self.assertIn('a.max.z > b.min.z', SOURCE)

    def test_fully_enclosed_invisible_box_is_removed(self):
        blocker = B(V(41, .3, 36.15), V(43, 2.8, 36.45))
        self.assertEqual(subtract(blocker, PASSAGE), [])

    def test_large_collision_preserves_every_outside_slab(self):
        b = B(V(32, -.2, 35.8), V(52, 5.1, 36.7))
        parts = subtract(b, PASSAGE)
        self.assertEqual(len(parts), 4)
        self.assertAlmostEqual(sum(p.volume for p in parts) + intersection(b, PASSAGE).volume, b.volume, places=8)
        self.assertTrue(all(not overlaps(p, PASSAGE) for p in parts))

    def test_random_subtractions_conserve_volume_and_are_disjoint(self):
        rng = random.Random(20261106)
        for _ in range(600):
            lo = V(rng.uniform(37, 42), rng.uniform(-2, 2), rng.uniform(30, 40))
            hi = V(lo.x+rng.uniform(.01, 12), lo.y+rng.uniform(.01, 9), lo.z+rng.uniform(.01, 16))
            b = B(lo, hi)
            pieces = subtract(b, PASSAGE)
            removed = intersection(b, PASSAGE).volume if overlaps(b, PASSAGE) else 0
            self.assertAlmostEqual(sum(p.volume for p in pieces) + removed, b.volume, delta=1e-7)
            for p in pieces:
                self.assertTrue(b.contains(p.min) and b.contains(p.max))
                self.assertFalse(overlaps(p, PASSAGE))
            for p, q in itertools.combinations(pieces, 2):
                self.assertLessEqual(intersection(p, q).volume, 1e-8)

    def test_boundary_contacts_do_not_change_colliders(self):
        touching = B(V(PASSAGE.max.x, 1, 36), V(PASSAGE.max.x + 2, 3, 37))
        self.assertFalse(overlaps(touching, PASSAGE))
        self.assertEqual(subtract(touching, PASSAGE), [touching])

    def test_repair_is_geometrically_idempotent(self):
        b = B(V(32, -.2, 33), V(52, 5.1, 45))
        first = subtract(b, PASSAGE)
        second = [q for p in first for q in subtract(p, PASSAGE)]
        self.assertEqual(first, second)

    def test_known_old_letter_envelope_is_inside_sign_cut(self):
        for x, y, z in itertools.product((38.7, 45.3), (3.86, 4.70), (35.79, 35.91)):
            self.assertTrue(SIGN.contains(V(x, y, z)))
        self.assertFalse(SIGN.contains(V(46.7, 2.8, 41.6)))  # WC letters


class NovaSourceContracts(unittest.TestCase):
    def test_only_city_and_exact_nova_root_enable_pass(self):
        self.assertRegex(HOOK, r'if\s*\(label == "miasto"\)\s*try\s*\{\s*BraxNovaEntranceFinish.Apply\(scene\)')
        self.assertIn('t.name == NovaRoot', SOURCE)
        self.assertIn('if (clubs.Length == 0) return;', SOURCE)
        self.assertIn('if (clubs.Length != 1)', SOURCE)

    def test_hook_is_before_mesh_readability_changes(self):
        self.assertLess(HOOK.index('BraxNovaEntranceFinish.Apply'), HOOK.index('var keep ='))
        self.assertLess(HOOK.index('BraxNovaEntranceFinish.Apply'), HOOK.index('p.boolValue = false'))

    def test_new_sign_and_source_assets_are_not_modified(self):
        material_predicate = SOURCE.split('static bool LetterMaterial', 1)[1].split('static void RemoveOldLetters', 1)[0]
        self.assertEqual(re.findall(r'm.name.Contains\("([^"]+)"\)', material_predicate),
                         ['NOVA: litery szyldu (podświetlane)', 'NOVA: boki liter (grafit)'])
        self.assertIn('Object.Instantiate(source)', SOURCE)
        for forbidden in ('SetTexture(', 'SetColor(', 'source.SetTriangles(', 'DestroyImmediate(club', 'Napis3D('):
            self.assertNotIn(forbidden, SOURCE)

    def test_only_passage_volume_is_used_for_collision_cut(self):
        self.assertIn('Remainders(c.bounds, Passage)', SOURCE)
        self.assertIn('BraxCityMeshCut.Subtract(temp.transform, Passage, Out', SOURCE)
        self.assertIn('if (!Overlaps(c.bounds, Passage)', SOURCE)
        self.assertNotIn('Physics.IgnoreLayerCollision(', SOURCE)
        self.assertNotIn('DestroyImmediate(c.gameObject)', SOURCE)

    def test_npcs_triggers_and_camera_collisions_are_not_cut(self):
        for contract in ('c.isTrigger', 'c.gameObject.layer == 29', 'c is CharacterController',
                         'c.attachedRigidbody', 'GetComponentInParent<BraxGymPlayer>',
                         'GetComponentInParent<Animator>'):
            self.assertIn(contract, SOURCE)

    def test_automatic_doors_are_not_forced_permanently_open(self):
        self.assertNotIn('zawszeOtwarte =', SOURCE)
        self.assertIn('d.GetComponentsInChildren<Collider>(true)', SOURCE)
        self.assertIn('door.skrzydla[i].localPosition = leaves[i];', SOURCE)
        self.assertIn('cols[i].enabled = enabled[i];', SOURCE)
        probe = SOURCE.split('static void Probe', 1)[1]
        self.assertIn('finally', probe)
        self.assertIn('body.radius', probe)
        self.assertIn('body.height', probe)
        self.assertIn('Physics.OverlapCapsule', probe)
        self.assertIn('Physics.CapsuleCastAll', probe)
        self.assertIn('Vector3.forward, 9.4f', probe)
        self.assertIn('Vector3.back, 9.4f', probe)

    def test_failed_scene_probe_cannot_report_pass(self):
        status = SOURCE.split('rep.status =', 1)[1].split(';', 1)[0]
        for requirement in ('rep.doorConfigured', 'rep.pathSamples > 0', 'rep.remainingLetterTriangles == 0',
                            'rep.blockers.Count == 0', 'rep.groundGaps == 0', 'rep.warnings.Count == 0'):
            self.assertIn(requirement, status)
        self.assertIn('phoneValidated = false', SOURCE)
        self.assertIn('"REQUIRES_REVIEW"', status)

    def test_generated_partial_objects_are_cleaned_up_on_failure(self):
        self.assertIn('Object.DestroyImmediate(temp)', SOURCE)
        self.assertIn('Object.DestroyImmediate(replacement)', SOURCE)
        self.assertIn('foreach (var p in created)', SOURCE)
        self.assertIn('EditorUtility.CopySerialized(c, p)', SOURCE)

    def test_old_sign_cleanup_scans_entire_scene_and_protects_owner(self):
        self.assertIn("club.gameObject.scene.GetRootGameObjects()", SOURCE)
        self.assertIn('text.Contains("CLUB NOVA")', SOURCE)
        self.assertNotIn('text.Contains("NOVA CLUB")', SOURCE)
        self.assertIn('n.StartsWith("nova szyld oryginalny właściciela")', SOURCE)
        self.assertIn('n == "mural club_nova"', SOURCE)
        self.assertIn("ApprovedNovaSign(r.transform)", SOURCE)

    def test_unlabelled_old_sign_is_clipped_only_under_new_panel(self):
        match = re.search(r"var stripe = Box\(([^)]*)\);", SOURCE)
        self.assertIsNotNone(match)
        x0, x1, y0, y1, z0, z1 = [
            float(item.strip().rstrip("f")) for item in match.group(1).split(",")
        ]
        self.assertGreaterEqual(x0, 38.4)
        self.assertLessEqual(x1, 45.6)
        self.assertGreaterEqual(y0, 3.62)
        self.assertLessEqual(y1, 4.86)
        self.assertGreater(y0, 2.98)  # above club doorway and player collision
        self.assertIn('BraxCityMeshCut.Subtract(temp.transform, stripe', SOURCE)
        self.assertIn('r.transform.IsChildOf(club)', SOURCE)
        self.assertIn('club.IsChildOf(r.transform)', SOURCE)
        self.assertIn('finally { Object.DestroyImmediate(temp); }', SOURCE)

    def test_csharp_lexical_delimiters_balanced(self):
        for code in (SOURCE, HOOK):
            stack = []
            for ch in masked_csharp(code):
                if ch in '([{':
                    stack.append(ch)
                elif ch in ')]}':
                    self.assertTrue(stack, 'Unexpected closing delimiter')
                    self.assertEqual(stack.pop(), {')': '(', ']': '[', '}': '{'}[ch])
            self.assertEqual(stack, [])


if __name__ == '__main__':
    unittest.main()
