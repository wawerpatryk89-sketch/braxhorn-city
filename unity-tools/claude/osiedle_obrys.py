"""Refine curved outlines on a scanned character without moving original vertices."""

import bpy
import numpy as np


def refine_outline(obj, up):
    old = obj.data
    old.calc_loop_triangles()
    tris = np.asarray([t.vertices[:] for t in old.loop_triangles])
    loops = np.asarray([t.loops[:] for t in old.loop_triangles])
    vertices = np.asarray([v.co[:] for v in old.vertices])
    normals = np.asarray([v.normal[:] for v in old.vertices])
    world = np.asarray(obj.matrix_world)
    levels = (vertices @ world[:3, :3].T + world[:3, 3]) @ up
    height = np.ptp(levels)
    levels = (levels - levels.min()) / height
    groups = [{g.group: g.weight for g in v.groups} for v in old.vertices]
    names = [obj.vertex_groups[max(g, key=g.get)].name if g else "" for g in groups]
    digits = np.array(["Hand" in n for n in names])
    seat = (
        np.array([any(x in n for x in ("Hips", "Spine", "UpLeg")) for n in names])
        & (levels > 0.415)
        & (levels < 0.592)
    )
    arms = np.array(["Arm" in n for n in names])
    local_height = height / np.mean(obj.matrix_world.to_scale())
    positions = vertices.tolist()
    uv_layers = [
        (layer.name, np.asarray([x.uv[:] for x in layer.data]))
        for layer in old.uv_layers
    ]
    midpoints = {}
    for e in old.edges:
        a, b = sorted(e.vertices[:])
        threshold = (
            0.996
            if digits[a] and digits[b]
            else 0.982 if seat[a] and seat[b] else 0.90 if arms[a] and arms[b] else -1.0
        )
        if normals[a] @ normals[b] >= threshold:
            continue
        edge = vertices[b] - vertices[a]
        # A cubic Hermite edge matches both neighbouring tangent planes. The displacement
        # is bounded so pocket corners and scan measurements retain their original shape.
        offset = (
            (edge @ normals[b]) * normals[b] - (edge @ normals[a]) * normals[a]
        ) / 8
        limit = min(
            np.linalg.norm(edge) * 0.16,
            local_height * (0.00085 if digits[a] else 0.0016),
        )
        offset *= min(1.0, limit / max(np.linalg.norm(offset), 1e-12))
        midpoints[a, b] = len(positions)
        positions.append((0.5 * (vertices[a] + vertices[b]) + offset).tolist())
        blend = {
            k: 0.5 * (groups[a].get(k, 0.0) + groups[b].get(k, 0.0))
            for k in groups[a].keys() | groups[b].keys()
        }
        strongest = sorted(blend, key=blend.get, reverse=True)[:4]
        total = sum(blend[k] for k in strongest)
        groups.append({k: blend[k] / total for k in strongest} if total else {})

    # Slots 0..2 refer to the parent triangle, slots 3..5 to its edge midpoints.
    # Use a conforming triangulation even at the boundary of the refined patch.
    patterns = {
        0: ((0, 1, 2),),
        1: ((0, 3, 2), (3, 1, 2)),
        2: ((1, 4, 0), (4, 2, 0)),
        4: ((2, 5, 1), (5, 0, 1)),
        3: ((1, 4, 3), (0, 3, 4), (0, 4, 2)),
        6: ((2, 5, 4), (1, 4, 5), (1, 5, 0)),
        5: ((0, 3, 5), (2, 5, 3), (2, 3, 1)),
        7: ((0, 3, 5), (3, 1, 4), (5, 4, 2), (3, 4, 5)),
    }
    faces, parents, slots = [], [], []
    for idx, (a, b, c) in enumerate(tris):
        mids = [midpoints.get(tuple(sorted(pair))) for pair in ((a, b), (b, c), (c, a))]
        pattern = sum(1 << i for i, m in enumerate(mids) if m is not None)
        ids = [a, b, c, *mids]
        for face in patterns[pattern]:
            faces.append(tuple(ids[i] for i in face))
            parents.append(idx)
            slots.append(face)
    # Some scans have two source polygon corners on the same triangle with distinct UVs.
    # Blender's FBX reader discards the second polygon if its three vertex IDs match.
    seen = set()
    seam_vertices = []
    for i, face in enumerate(faces):
        signature = tuple(sorted(face))
        if signature in seen:
            a, b, c = face
            faces[i] = (len(positions), b, c)
            positions.append(positions[a])
            groups.append(groups[a].copy())
            seam_vertices.append(a)
        seen.add(signature)
    new = bpy.data.meshes.new(old.name + "_Obrys")
    new.from_pydata(positions, [], faces)
    new.update()
    for mat in old.materials:
        new.materials.append(mat)
    polys = np.asarray([t.polygon_index for t in old.loop_triangles])[parents]
    new.polygons.foreach_set(
        "material_index",
        np.asarray([old.polygons[i].material_index for i in polys], dtype="i"),
    )
    new.polygons.foreach_set(
        "use_smooth",
        np.asarray([old.polygons[i].use_smooth for i in polys], dtype=bool),
    )
    parents, slots = np.asarray(parents), np.asarray(slots)

    def interpolate(data):
        corners = data[loops]
        values = np.concatenate(
            (corners, (corners + np.roll(corners, -1, axis=1)) * 0.5), axis=1
        )
        return values[parents[:, None], slots]

    for layer_name, values in uv_layers:
        layer = new.uv_layers.new(name=layer_name)
        layer.data.foreach_set("uv", interpolate(values).astype("f").ravel())
    if old.uv_layers.active:
        new.uv_layers.active = new.uv_layers[old.uv_layers.active.name]
    corner_normals = interpolate(
        np.asarray([n.vector[:] for n in old.corner_normals])
    ).reshape(-1, 3)
    corner_normals /= np.maximum(np.linalg.norm(corner_normals, axis=1)[:, None], 1e-12)
    new.normals_split_custom_set(corner_normals.tolist())
    group_names = [g.name for g in obj.vertex_groups]
    obj.data = new
    if old.shape_keys:
        # Preserve the original corrective deltas at every original vertex and
        # interpolate them at inserted edge vertices. FBX take curves are restored on export.
        edges = np.asarray(list(midpoints), dtype=int).reshape(-1, 2)
        keys = old.shape_keys.key_blocks
        for src in keys:
            key = obj.shape_key_add(name=src.name, from_mix=False)
            delta = np.array([v.co[:] for v in src.data]) - vertices
            expanded = np.concatenate((delta, delta[edges].mean(axis=1)))
            if seam_vertices:
                expanded = np.concatenate((expanded, expanded[seam_vertices]))
            key.data.foreach_set("co", (np.asarray(positions) + expanded).astype("f").ravel())
            key.slider_min, key.slider_max, key.value = src.slider_min, src.slider_max, src.value
        for src in keys:
            new.shape_keys.key_blocks[src.name].relative_key = new.shape_keys.key_blocks[src.relative_key.name]
    for name in group_names:
        obj.vertex_groups.new(name=name)
    for idx, influences in enumerate(groups):
        for group, weight in influences.items():
            if weight > 0:
                obj.vertex_groups[group].add([idx], weight, "REPLACE")
    return len(midpoints)
