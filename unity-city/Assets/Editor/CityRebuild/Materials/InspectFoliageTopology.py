"""Inspect a realised source LOD before choosing a leaf-preserving mobile reduction.

Blender --background --disable-autoexec SOURCE.blend --python SCRIPT --
    --object tree_small_02_LOD1 --output foliage-topology.json
Reads mesh/material/UV data only. No modifier evaluation, decimation or source save.
"""
import argparse
from array import array
from collections import Counter, defaultdict
import hashlib
import json
from pathlib import Path
import sys

import bpy
import numpy as np


def args():
    parser = argparse.ArgumentParser()
    parser.add_argument("--object", required=True)
    parser.add_argument("--output", required=True)
    return parser.parse_args(sys.argv[sys.argv.index("--") + 1:])


def sha256(path):
    result = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            result.update(block)
    return result.hexdigest()


def quantiles(values):
    if not values:
        return None
    return {key: float(value) for key, value in zip(
        ("min", "p50", "p90", "p95", "p99", "max"), np.percentile(values, (0, 50, 90, 95, 99, 100)))}


def main():
    options = args()
    obj = bpy.data.objects.get(options.object)
    if obj is None or obj.type != "MESH":
        raise RuntimeError("The exact realised source mesh is missing: " + options.object)
    if any(modifier.show_render for modifier in obj.modifiers):
        raise RuntimeError("Topology inspection requires a realised mesh without active modifiers")
    mesh = obj.data
    mesh.calc_loop_triangles()
    source = Path(bpy.data.filepath)
    positions = np.empty((len(mesh.vertices), 3), dtype=np.float64)
    mesh.vertices.foreach_get("co", positions.reshape(-1))
    uv_layers = {}
    for layer in mesh.uv_layers:
        uv = np.empty((len(mesh.loops), 2), dtype=np.float64)
        layer.data.foreach_get("uv", uv.reshape(-1))
        uv_layers[layer.name] = uv
    if not uv_layers:
        raise RuntimeError("Realised foliage has no UV layers")
    active = mesh.uv_layers.active.name
    uv = uv_layers[active]
    material_counts = Counter(triangle.material_index for triangle in mesh.loop_triangles)
    leaf_slots = [i for i, material in enumerate(mesh.materials)
                  if material and "leaves" in material.name.lower()]
    if len(leaf_slots) != 1:
        raise RuntimeError("Expected one explicitly named leaf material, found " + str(leaf_slots))
    leaf_slot = leaf_slots[0]
    leaf_faces = [polygon.index for polygon in mesh.polygons if polygon.material_index == leaf_slot]
    parent = array("i", range(len(mesh.vertices)))

    def find(vertex):
        while parent[vertex] != vertex:
            parent[vertex] = parent[parent[vertex]]
            vertex = parent[vertex]
        return vertex

    def union(a, b):
        a, b = find(a), find(b)
        if a != b:
            parent[b] = a

    for index in leaf_faces:
        polygon = mesh.polygons[index]
        vertices = polygon.vertices
        for vertex in vertices[1:]:
            union(vertices[0], vertex)
    islands = defaultdict(list)
    for index in leaf_faces:
        polygon = mesh.polygons[index]
        islands[find(polygon.vertices[0])].append(index)
    topology = []
    error_rows = []
    for island, face_indices in islands.items():
        loops = [loop for index in face_indices for loop in mesh.polygons[index].loop_indices]
        vertex_indices = [mesh.loops[loop].vertex_index for loop in loops]
        points, coords = positions[vertex_indices], uv[loops]
        low, high = coords.min(axis=0), coords.max(axis=0)
        extent = high - low
        triangle_count = sum(max(0, len(mesh.polygons[index].vertices) - 2) for index in face_indices)
        diagonal = float(np.linalg.norm(points.max(axis=0) - points.min(axis=0)))
        row = {"id": island, "vertices": len(set(vertex_indices)), "triangles": triangle_count,
               "diagonal_m": diagonal, "uv_min": low.tolist(), "uv_max": high.tolist()}
        if float(extent.min()) < 1e-7:
            row["error"] = "degenerate UV extent"
            error_rows.append(row)
        else:
            q = (coords - low) / extent
            u, v = q[:, 0], q[:, 1]
            # A quadratic surface can retain a photographed leaf's shallow curl.
            # This fits actual source positions; it is not a replacement silhouette.
            design = np.column_stack((np.ones(len(q)), u, v, u*v, u*u, v*v))
            coefficients, _, rank, _ = np.linalg.lstsq(design, points, rcond=None)
            residual = np.linalg.norm(design @ coefficients - points, axis=1)
            row.update({"fit_rank": int(rank), "quadratic_max_error_m": float(residual.max()),
                        "quadratic_rms_error_m": float(np.sqrt(np.mean(residual*residual)))})
            if rank < 6:
                row["error"] = "rank deficient UV-to-surface fit"
                error_rows.append(row)
        topology.append(row)
    node_info = []
    for material in mesh.materials:
        item = {"name": material.name, "uv_nodes": [], "image_vector_links": []}
        if material.node_tree:
            for node in material.node_tree.nodes:
                if node.type == "UVMAP":
                    item["uv_nodes"].append({"name": node.name, "uv_map": node.uv_map})
                if node.type == "TEX_IMAGE" and node.image:
                    item["image_vector_links"].append({"image": node.image.filepath,
                        "source": [{"node": link.from_node.name, "type": link.from_node.type,
                                    "socket": link.from_socket.name} for link in node.inputs["Vector"].links]})
        node_info.append(item)
    valid = [row for row in topology if "quadratic_max_error_m" in row]
    report = {"schema_version": 1, "source_object": options.object,
              "source_sha256": sha256(source), "source_bytes": source.stat().st_size,
              "blender": bpy.app.version_string, "source_modified": False,
              "vertices": len(mesh.vertices), "triangles": len(mesh.loop_triangles),
              "material_triangles": [{"slot": i, "name": material.name,
                                     "triangles": material_counts[i]} for i, material in enumerate(mesh.materials)],
              "uv_active": active, "uv_layers": [{"name": layer.name, "active_render": layer.active_render}
                                                  for layer in mesh.uv_layers],
              "material_uv_sources": node_info, "leaf_islands": len(topology),
              "leaf_triangle_quantiles": quantiles([row["triangles"] for row in topology]),
              "leaf_diagonal_m_quantiles": quantiles([row["diagonal_m"] for row in topology]),
              "quadratic_max_error_m_quantiles": quantiles([row["quadratic_max_error_m"] for row in valid]),
              "quadratic_rms_error_m_quantiles": quantiles([row["quadratic_rms_error_m"] for row in valid]),
              "invalid_islands": error_rows[:20], "invalid_island_count": len(error_rows),
              "largest_error_examples": sorted(valid,key=lambda row:row["quadratic_max_error_m"],reverse=True)[:12],
              "triangle_count_histogram": dict(sorted(Counter(row["triangles"] for row in topology).items())),
              "visual_acceptance": "pending; fitting error is not a silhouette/alpha/mobile performance pass"}
    output = Path(options.output)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, indent=2) + "\n")
    print("FOLIAGE_TOPOLOGY " + json.dumps(report, separators=(",", ":")), flush=True)


if __name__ == "__main__":
    main()
