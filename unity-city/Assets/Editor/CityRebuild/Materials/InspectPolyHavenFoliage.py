"""Read-only Blender inventory of authored Poly Haven static/LOD collections.

Run with Blender --background --disable-autoexec SOURCE.blend --python SCRIPT --
    --slug tree_small_02 --output /path/tree_small_02-lods.json
No geometry-node realization, source save, texture download, or automatic decimation.
"""

import argparse
import hashlib
import json
import re
import sys
from pathlib import Path

import bpy
from mathutils import Vector


def arguments():
    tail = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--slug", required=True)
    parser.add_argument("--output", required=True)
    return parser.parse_args(tail)


def sha256(path):
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def value(socket):
    if socket is None:
        return None
    if socket.is_linked:
        return {"linked": True, "from": [link.from_node.name for link in socket.links]}
    raw = getattr(socket, "default_value", None)
    if isinstance(raw, (str, int, float, bool)) or raw is None:
        return raw
    try:
        return list(raw)
    except TypeError:
        return str(raw)


def material_info(material):
    if material is None:
        return None
    result = {"name": material.name, "use_nodes": material.use_nodes,
              "surface_render_method": getattr(material, "surface_render_method", None),
              "blend_method": getattr(material, "blend_method", None),
              "alpha_threshold": getattr(material, "alpha_threshold", None),
              "backface_culling": material.use_backface_culling, "images": [], "principled": []}
    if material.node_tree:
        for node in material.node_tree.nodes:
            if node.type == "TEX_IMAGE" and node.image:
                result["images"].append({"node": node.name, "image": node.image.name,
                                         "path": node.image.filepath,
                                         "colour_space": node.image.colorspace_settings.name})
            elif node.type == "BSDF_PRINCIPLED":
                result["principled"].append({key: value(node.inputs.get(key)) for key in
                    ("Base Color", "Roughness", "Metallic", "Alpha", "Normal", "Transmission Weight")})
    return result


def mesh_info(obj):
    mesh = obj.data
    mesh.calc_loop_triangles()
    corners = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
    bounds = {"min": [min(corner[axis] for corner in corners) for axis in range(3)],
              "max": [max(corner[axis] for corner in corners) for axis in range(3)]}
    return {"name": obj.name, "mesh": mesh.name, "vertices": len(mesh.vertices),
            "triangles": len(mesh.loop_triangles), "polygons": len(mesh.polygons),
            "uv_layers": [layer.name for layer in mesh.uv_layers],
            "vertex_colour_layers": [layer.name for layer in mesh.color_attributes],
            "materials": [slot.material.name if slot.material else None for slot in obj.material_slots],
            "modifiers": [{"name": modifier.name, "type": modifier.type,
                           "show_viewport": modifier.show_viewport, "show_render": modifier.show_render,
                           "node_group": getattr(getattr(modifier, "node_group", None), "name", None)}
                          for modifier in obj.modifiers],
            "scale": list(obj.scale), "bounds_world": bounds,
            "hide_render": obj.hide_render, "hide_viewport": obj.hide_viewport}


def main():
    args = arguments()
    source = Path(bpy.data.filepath)
    if not source.is_file():
        raise RuntimeError("Load the actual source .blend before running this inventory")
    objects = {obj.name: mesh_info(obj) for obj in bpy.data.objects if obj.type == "MESH"}
    collections = []
    for collection in bpy.data.collections:
        mesh_names = sorted({obj.name for obj in collection.all_objects if obj.type == "MESH"})
        lod_match = re.search(r"(?:^|_)lod([0-9]+)(?:$|[._])", collection.name, re.IGNORECASE)
        kind = "lod" if lod_match else "static" if collection.name.lower().endswith("_static") else "other"
        rows = [objects[name] for name in mesh_names]
        has_nodes = any(mod["type"] == "NODES" and mod["show_render"]
                        for row in rows for mod in row["modifiers"])
        has_other_modifiers = any(mod["show_render"] for row in rows for mod in row["modifiers"])
        collections.append({"name": collection.name, "kind": kind,
                            "lod": int(lod_match.group(1)) if lod_match else None,
                            "objects": mesh_names, "vertices": sum(row["vertices"] for row in rows),
                            "base_mesh_triangles": sum(row["triangles"] for row in rows),
                            "triangle_count_is_final": not has_other_modifiers,
                            "geometry_nodes_present": has_nodes,
                            "hide_render": collection.hide_render,
                            "hide_viewport": collection.hide_viewport})
    source_lods = sorted((row for row in collections if row["kind"] == "lod"),
                         key=lambda row: (row["lod"], row["name"]))
    report = {"schema_version": 1, "slug": args.slug,
              "source": {"filename": source.name, "bytes": source.stat().st_size,
                         "sha256": sha256(source), "blender": bpy.app.version_string},
              "scene_unit_system": bpy.context.scene.unit_settings.system,
              "scene_unit_scale": bpy.context.scene.unit_settings.scale_length,
              "authored_lods_found": bool(source_lods), "authored_lods": source_lods,
              "collections": collections, "meshes": list(objects.values()),
              "materials": [material_info(material) for material in bpy.data.materials],
              "images": [{"name": image.name, "path": image.filepath,
                          "packed": bool(image.packed_file), "size": list(image.size)}
                         for image in bpy.data.images],
              "geometry_nodes_evaluated": False, "source_modified": False,
              "visual_review": "pending: inventory is not a visual or mobile performance pass",
              "next_step": "Prefer existing authored LODs; inspect leaves/UV/materials before mobile derivation."
              }
    output = Path(args.output)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, indent=2, ensure_ascii=False) + "\n")
    print("FOLIAGE_SOURCE_INVENTORY " + json.dumps({"slug": args.slug, "output": str(output),
          "authored_lods": [{key: row[key] for key in
              ("name", "lod", "base_mesh_triangles", "triangle_count_is_final", "geometry_nodes_present")}
                            for row in source_lods]}))


if __name__ == "__main__":
    main()
