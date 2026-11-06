"""Execute the actual building recipes with a geometry recorder.

No Blender/Unity result is inferred from these fast tests. They check the
dimensions emitted by the source; real GLB/physics checks run separately.
"""
from pathlib import Path
import importlib.util
import sys
import types
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[3]
SCRIPT = ROOT / "tools/build_oboz_service_models.py"


def recipe_module():
    spec = importlib.util.spec_from_file_location("camp_service_recipe", SCRIPT)
    module = importlib.util.module_from_spec(spec)
    with patch.dict(sys.modules, {"bpy": types.ModuleType("bpy"),
                                 "mathutils": types.SimpleNamespace(Vector=tuple)}):
        spec.loader.exec_module(module)
    return module


RECIPE = recipe_module()


class RecordingBuilder(RECIPE.Builder):
    def __init__(self, name="test"):
        self.name = name
        self.colliders = []
        self.doors = []
        self.seats = []
        self.facilities = []
        self.furniture = []
        self.missing_models = []
        self.geometry = []
        self.roots = []

    def assembly(self, name, at):
        node = types.SimpleNamespace(name=name, at=tuple(at))
        self.roots.append(node)
        return node

    def box(self, name, at, size, material="wood", yaw=0, solid=False,
            parent=None, bevel=.015):
        self.geometry.append(dict(name=name, at=tuple(at), size=tuple(size),
                                  parent=parent, material=material))
        if solid:
            self.colliders.append(dict(position=list(at), size=list(size), yaw=yaw))

    def rod(self, name, a, b, r=.04, material="metal", parent=None):
        pass

    def shell(self, width, depth):
        self.shell_size = (width, depth)
        self.missing_models.append("interior-lamps")


def extents(box, axis):
    return box["at"][axis] - box["size"][axis] / 2, box["at"][axis] + box["size"][axis] / 2


class ServiceBuildingGeometryTests(unittest.TestCase):
    def test_table_feet_floor_and_seat_height_are_measured_from_floor(self):
        model = RecordingBuilder()
        model.table(-2.7, -1.8, 3.6, 1.0)
        model.bench(-2.7, -2.73, 3.6)
        boards = [p for p in model.geometry if p["name"] == "Table board"]
        legs = [p for p in model.geometry if p["name"] == "Table leg"]
        slats = [p for p in model.geometry if p["name"] == "Bench slat"]
        supports = [p for p in model.geometry if p["name"] == "Bench trestle"]
        self.assertEqual(len(legs), 4)
        self.assertTrue(all(.15 <= p["size"][2] <= .20 for p in boards))
        for part in legs + supports:
            self.assertAlmostEqual(extents(part, 1)[0], .16)
        for part in boards:
            self.assertAlmostEqual(extents(part, 1)[1] - .16, .75)
        for part in slats:
            self.assertAlmostEqual(extents(part, 1)[1] - .16, .45)
        self.assertAlmostEqual(extents(legs[0], 1)[1], extents(boards[0], 1)[0])
        self.assertAlmostEqual(extents(supports[0], 1)[1], extents(slats[0], 1)[0])
        self.assertTrue(all(abs(s["position"][1] - .61) < 1e-8 for s in model.seats))
        self.assertEqual(model.colliders, [], "Furniture colliders are made from the measured Unity assembly")

    def test_canteen_has_one_plank_set_and_no_fake_dishes_or_counter(self):
        with patch.object(RECIPE, "Builder", RecordingBuilder):
            model = RECIPE.canteen()
        self.assertEqual(model.shell_size, (12, 9))
        self.assertEqual(sum(f["kind"] == "Table" for f in model.furniture), 4)
        self.assertEqual(sum(f["kind"] == "Bench" for f in model.furniture), 8)
        self.assertEqual(len(model.seats), 24)
        self.assertEqual({tuple(f["position"][::2]) for f in model.furniture if f["kind"] == "Table"},
                         {(-2.7, -1.8), (2.7, -1.8), (-2.7, 1.4), (2.7, 1.4)})
        self.assertTrue(all(p["parent"] is not None for p in model.geometry))
        self.assertTrue(all(p["name"] in {"Table board", "Table leg", "Bench slat", "Bench trestle"}
                            for p in model.geometry))
        self.assertEqual(set(model.missing_models), {"canteen-bowls", "canteen-mugs", "canteen-chairs", "interior-lamps"})

    def test_door_geometry_hinge_and_collider_match_real_dimensions(self):
        model = RecordingBuilder()
        model.door(-4.5)
        boards = [p for p in model.geometry if p["name"] == "Door plank"]
        self.assertEqual(len(boards), 6)
        self.assertAlmostEqual(min(extents(p, 0)[0] for p in boards), -.45)
        self.assertAlmostEqual(max(extents(p, 0)[1] for p in boards), .45)
        self.assertAlmostEqual(min(extents(p, 1)[0] for p in boards), .16)
        self.assertAlmostEqual(max(extents(p, 1)[1] for p in boards), 2.26)
        self.assertEqual(model.roots[0].at, (-.45, .16, -4.52))
        self.assertEqual(model.doors[0]["colliderCenter"], [.45, 1.05, 0])
        self.assertEqual(model.doors[0]["colliderSize"], [.9, 2.1, .10])
        ordered = sorted(boards, key=lambda p: p["at"][0])
        for previous, following in zip(ordered, ordered[1:]):
            self.assertAlmostEqual(extents(following, 0)[0] - extents(previous, 0)[1], .004)

    def test_front_colliders_leave_90cm_opening_and_close_above_210cm(self):
        def contains(collider, x, y):
            return all(abs(v - collider["position"][axis]) < collider["size"][axis] / 2 - 1e-7
                       for axis, v in [(0, x), (1, y)])
        for width in (7, 8, 12):
            with self.subTest(width=width):
                model = RecordingBuilder()
                model.front_wall(width, -3)
                self.assertTrue(all(all(d > 0 for d in p["size"]) for p in model.geometry))
                for x in (-.449, 0, .449):
                    for y in (.17, 1.16, 2.259):
                        self.assertFalse(any(contains(c, x, y) for c in model.colliders))
                self.assertTrue(any(contains(c, .451, 1.16) for c in model.colliders))
                self.assertTrue(any(contains(c, -.451, 1.16) for c in model.colliders))
                self.assertTrue(any(contains(c, 0, 2.261) for c in model.colliders))

    def test_sauna_missing_heater_and_tub_are_reported_without_fake_action(self):
        with patch.object(RECIPE, "Builder", RecordingBuilder):
            model = RECIPE.sauna()
        self.assertEqual(model.shell_size, (7, 6))
        self.assertEqual(set(model.missing_models), {"sauna-heater", "cold-plunge-tub", "interior-lamps"})
        self.assertEqual(model.facilities, [])
        self.assertTrue(all(p["name"] in {"Sauna bench slat", "Sauna bench support"} for p in model.geometry))
        for part in model.geometry:
            if part["name"] == "Sauna bench support":
                self.assertAlmostEqual(extents(part, 1)[0], .16)

    def test_generated_files_have_deterministic_unique_metadata(self):
        folder = ROOT / "unity-city/Assets/PlayableGenerated/Oboz/ServiceModels"
        guids = []
        for model in ("canteen", "sauna", "medical", "storage"):
            for extension in ("glb", "json"):
                text = (folder / f"{model}.{extension}.meta").read_text()
                guid = text.split("guid: ", 1)[1].splitlines()[0]
                self.assertRegex(guid, r"^[0-9a-f]{32}$")
                guids.append(guid)
        self.assertEqual(len(guids), len(set(guids)))

    def test_medical_has_no_placeholder_beds_cabinet_or_first_aid_case(self):
        with patch.object(RECIPE, "Builder", RecordingBuilder):
            model = RECIPE.medical()
        self.assertEqual(model.shell_size, (8, 6))
        self.assertTrue(all(p["name"] in {"Table board", "Table leg"} for p in model.geometry))
        self.assertEqual(model.facilities, [], "No treatment without an original first-aid model")
        self.assertIn("first-aid-case", model.missing_models)
        self.assertFalse(hasattr(RECIPE.Builder, "bed"))

    def test_storage_is_shelving_with_grounded_legs_and_no_box_crates(self):
        with patch.object(RECIPE, "Builder", RecordingBuilder):
            model = RECIPE.storage()
        self.assertEqual(model.shell_size, (8, 6))
        self.assertEqual(len(model.furniture), 4)
        self.assertTrue(all(p["name"] in {"Storage shelf", "Rack upright"} for p in model.geometry))
        self.assertIn("storage-crates", model.missing_models)
        self.assertEqual(model.colliders, [])
        for p in model.geometry:
            if p["name"] == "Rack upright":
                self.assertAlmostEqual(extents(p, 1)[0], .16)
                self.assertAlmostEqual(extents(p, 1)[1] - .16, 2.05)

    def test_unused_procedural_showers_are_not_exported(self):
        self.assertFalse(hasattr(RECIPE, "showers"))


if __name__ == "__main__":
    unittest.main()
