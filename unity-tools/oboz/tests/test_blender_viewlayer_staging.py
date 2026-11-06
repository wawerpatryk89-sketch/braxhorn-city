"""Regression for Blender 4.5: owner meshes can be outside the active ViewLayer.

Uses a tiny bpy fake; never starts Unity or touches private owner assets.
"""
import contextlib
import io
import json
from pathlib import Path
import runpy
import sys
import tempfile
import types
import unittest
from unittest.mock import patch

SCRIPT = Path(__file__).resolve().parents[1] / "owner_blender_export_bpy.py"


class BlenderViewLayerStaging(unittest.TestCase):
    def test_mesh_from_other_collection_becomes_selectable_without_source_save(self):
        visible = {}

        class Objects:
            active = None

            def link(self, obj):
                visible[obj.name] = obj

            def get(self, name):
                return visible.get(name)

        class Original:
            def __init__(self, name, kind="MESH", parent=None):
                self.name = name
                self.type = kind
                self.parent = parent
                self.hide_render = False
                self.hide_viewport = True
                self.hide_select = True
                self.selected = False
                self.data = types.SimpleNamespace(vertices=[1, 2], polygons=[1])

            def hide_set(self, hidden):
                if self.name not in visible:
                    raise RuntimeError("Object is outside the active ViewLayer")
                self.hidden = hidden

            def select_set(self, selected):
                if self.name not in visible:
                    raise RuntimeError("Object cannot be selected outside ViewLayer")
                self.selected = selected

        parent = Original("source_parent", "EMPTY")
        mesh = Original("grass_medium_02_a_geo", parent=parent)
        objects = Objects()
        staged = []
        updated = []

        def perform_export(**kwargs):
            self.assertTrue(mesh.selected)
            self.assertTrue(kwargs["use_selection"])
            self.assertIn(parent.name, visible)
            Path(kwargs["filepath"]).write_bytes(b"glTF" + bytes(2048))

        fake_bpy = types.SimpleNamespace(
            data=types.SimpleNamespace(
                images=[],
                objects=[mesh],
                collections=types.SimpleNamespace(
                    new=lambda name: types.SimpleNamespace(objects=objects)
                ),
            ),
            context=types.SimpleNamespace(
                scene=types.SimpleNamespace(
                    collection=types.SimpleNamespace(
                        children=types.SimpleNamespace(link=lambda collection: staged.append(collection))
                    )
                ),
                view_layer=types.SimpleNamespace(
                    objects=objects, update=lambda: updated.append(True)
                ),
            ),
            ops=types.SimpleNamespace(
                wm=types.SimpleNamespace(open_mainfile=lambda **kwargs: None),
                object=types.SimpleNamespace(select_all=lambda **kwargs: None),
                export_scene=types.SimpleNamespace(gltf=perform_export),
            ),
            path=types.SimpleNamespace(abspath=lambda p: p),
        )

        with tempfile.TemporaryDirectory() as folder:
            source = Path(folder) / "grass_medium_02_2k.blend"
            source.write_bytes(b"owner-test-input")
            output = Path(folder) / "grass_medium_02_2k.glb"
            argv = ["blender", "--", str(source), str(output), folder]
            with patch.dict(sys.modules, {"bpy": fake_bpy}), patch.object(sys, "argv", argv):
                with contextlib.redirect_stdout(io.StringIO()):
                    runpy.run_path(str(SCRIPT), run_name="__main__")
            self.assertTrue(output.is_file())
            self.assertEqual(source.read_bytes(), b"owner-test-input")
            self.assertTrue(updated)
            self.assertTrue(staged)
            self.assertIs(objects.active, mesh)
            report = json.loads(output.with_suffix(".export-audit.json").read_text())
            self.assertEqual(report["sourceMeshObjects"], 1)
            self.assertFalse(report["decimationApplied"])
            self.assertFalse(report["originalBlenderFilesChanged"])


if __name__ == "__main__":
    unittest.main()
