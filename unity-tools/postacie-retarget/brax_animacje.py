"""Batch retargeting and visual review of Mixamo bipeds. Run with Blender 4.3.x."""
import argparse
import hashlib
import json
import math
import os
import re
import sys
import tempfile
import time
from pathlib import Path

import bpy
import numpy as np
from mathutils import Vector

FPS = 30
CACHE_VERSION = 2
SIDES = ("Left", "Right")
FINGERS = ("Thumb", "Index", "Middle", "Ring", "Pinky")
CORE = {"Hips", "Spine", "Neck", "Head"} | {
    side + part for side in SIDES for part in ("Arm", "ForeArm", "Hand", "UpLeg", "Leg", "Foot")
}


def sha(path):
    with path.open("rb") as src:
        return hashlib.file_digest(src, "sha256").hexdigest()


def safe_id(label):
    if not re.fullmatch(r"[a-zA-Z0-9_-]+", label):
        raise ValueError(f"Unsafe identifier: {label!r}")
    return label


def fresh_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def import_rig(path, animated):
    bpy.ops.import_scene.fbx(filepath=str(path), use_anim=animated, ignore_leaf_bones=False,
                             automatic_bone_orientation=False, use_image_search=True)
    rigs = [o for o in bpy.context.selected_objects if o.type == 'ARMATURE']
    if len(rigs) != 1:
        raise ValueError(f"{path.name}: expected one armature, got {len(rigs)}")
    return rigs[0]


def bone_map(rig):
    mapping = {}
    for bone in rig.data.bones:
        name = bone.name.split(":")[-1]
        if name in mapping:
            raise ValueError("Ambiguous bone " + name)
        mapping[name] = bone.name
    missing = CORE - mapping.keys()
    if missing:
        raise ValueError("Missing anatomical bones: " + ", ".join(sorted(missing)))
    return mapping


def unit(v):
    length = np.linalg.norm(v)
    if length < 1e-7:
        raise ValueError("Degenerate reference direction")
    return v / length


def basis(primary, secondary):
    y = unit(primary)
    x = unit(secondary - (secondary @ y) * y)
    return np.column_stack((x, y, np.cross(x, y)))


def rest_data(rig):
    names_map = bone_map(rig)
    names = list(names_map)
    index = {name: i for i, name in enumerate(names)}
    positions, rotations, parents = [], [], []
    world = rig.matrix_world
    if world.to_3x3().determinant() <= 0:
        raise ValueError("Mirrored armature object")
    scale = list(world.to_scale())
    if max(scale) / min(scale) > 1.002:
        raise ValueError("Armature uses non-uniform scale")
    for name in names:
        b = rig.data.bones[names_map[name]]
        mat = world @ b.matrix_local
        positions.append(list(mat.translation))
        rotations.append(np.array(mat.to_quaternion().to_matrix()))
        parents.append(index[b.parent.name.split(":")[-1]] if b.parent else -1)
    pos = np.array(positions)
    loc = lambda n: pos[index[n]]
    body = basis(loc("Neck") - loc("Hips"), loc("LeftArm") - loc("RightArm"))
    forward = body[:, 2]
    frames = np.repeat(body[None, :, :], len(names), axis=0)
    hand_landmarks = {s: all(s + "Hand" + f + "1" in index for f in ("Middle", "Index", "Pinky"))
                      for s in SIDES}
    for s in SIDES:
        hand = s + "Hand"
        for start, end in (("Arm", "ForeArm"), ("ForeArm", "Hand"), ("UpLeg", "Leg"), ("Leg", "Foot"), ("Foot", "ToeBase")):
            start, end = s + start, s + end
            if start in index and end in index:
                direction = loc(end) - loc(start)
                secondary = body[:, 1] if start == s + "Foot" else forward
                if abs(unit(direction) @ secondary) > .94:
                    secondary = body[:, 0]
                frames[index[start]] = basis(direction, secondary)
        if not hand_landmarks[s]:
            continue
        primary = loc(hand + "Middle1") - loc(hand)
        transverse = loc(hand + "Index1") - loc(hand + "Pinky1")
        palm = basis(primary, transverse)
        frames[index[hand]] = palm
        for finger in FINGERS:
            for j in range(1, 4):
                b = hand + finger + str(j)
                following = hand + finger + str(j + 1)
                if b not in index or following not in index:
                    continue
                direction = loc(following) - loc(b)
                secondary = palm[:, 2] if abs(unit(direction) @ palm[:, 2]) < .92 else palm[:, 0]
                frames[index[b]] = basis(direction, secondary)
    return dict(names=np.asarray(names), rest=np.asarray(rotations), positions=pos,
                parents=np.asarray(parents), body=body, anatomy=frames,
                fingers=np.asarray(list(hand_landmarks.values())))


def build_cache(source, dest):
    fresh_scene()
    rig = import_rig(source, True)
    data = rest_data(rig)
    action = rig.animation_data.action if rig.animation_data else None
    if action is None:
        raise ValueError("Source has no active action: " + str(source))
    scene = bpy.context.scene
    f0, f1 = action.frame_range
    source_rate = scene.render.fps / scene.render.fps_base
    duration = (f1 - f0) / source_rate
    if duration <= 0:
        raise ValueError("Source has no duration")
    times = np.linspace(0, duration, max(2, math.ceil(duration * FPS) + 1))
    mapping = bone_map(rig)
    ordered = [rig.pose.bones[mapping[n]] for n in data['names']]
    hips = rig.pose.bones[mapping['Hips']]
    rw = rig.matrix_world
    # Keep absolute rotations, independent of the source bone axes and object scale.
    local = np.empty((len(times), len(ordered), 3, 3), dtype=np.float32)
    root = np.empty((len(times), 3))
    for i, t in enumerate(times):
        frame = float(f0 + t * source_rate)
        integer = math.floor(frame)
        scene.frame_set(integer, subframe=frame - integer)
        for j, bone in enumerate(ordered):
            local[i, j] = np.array((rw @ bone.matrix).to_quaternion().to_matrix())
        root[i] = np.array(rw @ hips.head)
    dest.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.NamedTemporaryFile(dir=dest.parent, delete=False) as out:
        np.savez(out, **data, times=times, pose=local, root=root)
        tmp = out.name
    os.replace(tmp, dest)


def cache_path(spec, cache):
    return cache / f"v{CACHE_VERSION}_{FPS}_{spec['sha256']}.npz"


def cache_clips(plan, base, cache):
    for key, spec in plan['clips'].items():
        src = base / spec['file']
        if sha(src) != spec['sha256']:
            raise ValueError("Wrong source SHA-256: " + str(src))
        dest = cache_path(spec, cache)
        if not dest.exists():
            build_cache(src, dest)
        print("CACHED", spec.get('name', key), flush=True)


def quaternions(mats):
    # Largest diagonal component avoids loss of precision for half turns.
    m = np.asarray(mats)
    diag = np.diagonal(m, axis1=-2, axis2=-1)
    trace = diag.sum(-1)
    scores = np.column_stack([1 + trace, 1 + 2 * diag[:, 0] - trace, 1 + 2 *
                             diag[:, 1] - trace, 1 + 2 * diag[:, 2] - trace])
    out = np.empty((len(m), 4))
    for idx in range(4):
        pick = scores.argmax(axis=1) == idx
        a = m[pick]
        if not len(a):
            continue
        s = 2 * np.sqrt(np.maximum(scores[pick, idx], 1e-12))
        if idx == 0:
            values = (s / 4, (a[:, 2, 1] - a[:, 1, 2]) / s,
                      (a[:, 0, 2] - a[:, 2, 0]) / s, (a[:, 1, 0] - a[:, 0, 1]) / s)
        else:
            k = idx - 1
            j = (k + 1) % 3
            l = (k + 2) % 3
            xyz = np.empty((len(a), 3))
            xyz[:, k] = s / 4
            xyz[:, j] = (a[:, j, k] + a[:, k, j]) / s
            xyz[:, l] = (a[:, l, k] + a[:, k, l]) / s
            values = ((a[:, l, j] - a[:, j, l]) / s, *xyz.T)
        out[pick] = np.stack(values, axis=1)
    out /= np.linalg.norm(out, axis=1)[:, None]
    flips = np.sum(out[1:] * out[:-1], axis=1) < 0
    sign = np.r_[1, np.cumprod(np.where(flips, -1, 1))]
    return out * sign[:, None]


def add_curves(action, path, frames, values):
    for k, v in enumerate(values.T):
        curve = action.fcurves.new(path, index=k)
        points = curve.keyframe_points
        points.add(len(frames))
        points.foreach_set("co", np.column_stack((frames, v)).astype('f').ravel())
        points.foreach_set("interpolation", np.full(len(frames), 1, dtype='i'))  # LINEAR
        curve.update()


def bake(rig, target, source, state, window=None):
    names = target['names']
    smap = {n: i for i, n in enumerate(source['names'])}
    if (set(names) & CORE) - smap.keys():
        raise ValueError("Incomplete source body")
    if state in ('Point', 'Wave', 'Talk') and (not all(target['fingers']) or not all(source['fingers'])):
        raise ValueError("Missing index, middle or pinky landmarks for the hand gesture")
    if state == 'Point':
        for side in SIDES:
            for finger in ('Index', 'Middle'):
                for j in (1, 2, 3):
                    n = side + 'Hand' + finger + str(j)
                    if n not in names or n not in smap:
                        raise ValueError("Missing pointing joint " + n)
    body = target['body'] @ source['body'].T
    times = source['times']
    lo, hi = window if window is not None else (0, 1)
    if not 0 <= lo < hi <= 1:
        raise ValueError("Invalid clip window")
    i0 = round((len(times) - 1) * lo)
    i1 = min(len(times), max(i0 + 2, round((len(times) - 1) * hi) + 1))
    if i1 - i0 < 2:
        raise ValueError("Clip window contains fewer than two source samples")
    times = times[i0:i1]
    frames = (times - times[0]) * FPS + 1
    rotations = source['pose'][i0:i1]
    target_rest = target['rest']
    src_rest = source['rest']
    src_frames = source['anatomy']
    target_frames = target['anatomy']
    parents = target['parents']
    mapped = bone_map(rig)
    goals = {}
    action = bpy.data.actions.new(state)
    action.use_fake_user = True
    for j, n in enumerate(names):
        bone = rig.pose.bones[mapped[n]]
        p = parents[j]
        parent_rest = target_rest[p] if p >= 0 else np.asarray(rig.matrix_world.to_quaternion().to_matrix())
        parent_pose = goals[p] if p >= 0 else parent_rest
        if n not in smap:
            goals[j] = parent_pose @ parent_rest.T @ target_rest[j]
            continue
        i = smap[n]
        # Map anatomical directions before converting into each target bone's rest axes.
        correction = src_rest[i].T @ src_frames[i] @ target_frames[j].T @ target_rest[j]
        goal = body @ rotations[:, i] @ correction
        local = (target_rest[j].T @ parent_rest) @ np.swapaxes(parent_pose, -1, -2) @ goal
        goals[j] = goal
        bone.rotation_mode = 'QUATERNION'
        add_curves(action, bone.path_from_id('rotation_quaternion'), frames, quaternions(local))
    hi_src = smap['Hips']
    hi_dst = list(names).index('Hips')

    def leg_length(data):
        nm = list(data['names'])
        pts = data['positions']
        return np.mean([sum(np.linalg.norm(pts[nm.index(s + a)] - pts[nm.index(s + b)]) for a, b in (('UpLeg', 'Leg'), ('Leg', 'Foot'))) for s in SIDES])
    ratio = leg_length(target) / leg_length(source)
    world_motion = (source['root'][i0:i1] - source['positions'][hi_src]) @ body.T * ratio
    # Remove horizontal origin of the take, keep pelvis height and displacement.
    up = target['body'][:, 1]
    world_motion -= world_motion[0] - (world_motion[0] @ up) * up
    hip_bone = rig.data.bones[mapped['Hips']]
    local_linear = np.asarray((rig.matrix_world @ hip_bone.matrix_local).to_3x3())
    locations = world_motion @ np.linalg.inv(local_linear).T
    add_curves(action, rig.pose.bones[mapped['Hips']].path_from_id('location'), frames, locations)
    return action


def meshes_of(rig):
    return [o for o in bpy.context.scene.objects if o.type == 'MESH' and any(m.type == 'ARMATURE' and m.object == rig for m in o.modifiers)]


def mesh_vertices(o):
    mesh = o.data
    verts = np.empty(len(mesh.vertices) * 3, dtype='f')
    mesh.vertices.foreach_get('co', verts)
    return verts.reshape(-1, 3)


def rest_edges(meshes):
    edges = []
    for o in meshes:
        pts = mesh_vertices(o)
        ids = np.empty(len(o.data.edges) * 2, dtype='i')
        o.data.edges.foreach_get('vertices', ids)
        ids = ids.reshape(-1, 2)
        lengths = np.linalg.norm(pts[ids[:, 0]] - pts[ids[:, 1]], axis=1)
        mask = lengths > max(np.linalg.norm(np.ptp(pts, axis=0)) * 1e-5, 1e-10)
        if mask.any():
            edges.append((o, ids[mask], lengths[mask]))
    if not edges:
        raise ValueError("No skinned edges")
    return edges


def scene_camera(meshes):
    s = bpy.context.scene
    s.render.engine = 'BLENDER_WORKBENCH'
    s.display.shading.light = 'STUDIO'
    s.display.shading.color_type = 'TEXTURE'
    s.display.shading.show_shadows = False
    s.world = bpy.data.worlds.new("Studio")
    s.world.color = (.76, .79, .82)
    s.display.shading.background_type = 'WORLD'
    s.render.resolution_x = 288
    s.render.resolution_y = 340
    s.render.resolution_percentage = 100
    s.display.render_aa = 'OFF'
    s.render.image_settings.file_format = 'JPEG'
    s.render.image_settings.quality = 85
    for o in meshes:
        for slot in o.material_slots:
            mat = slot.material
            if not mat or not mat.use_nodes:
                continue
            colors = [n for n in mat.node_tree.nodes if n.type == 'TEX_IMAGE' and n.image and any(
                k in (n.image.name + ' ' + n.image.filepath).lower() for k in ('color', 'diffuse', 'albedo'))]
            if colors:
                mat.node_tree.nodes.active = colors[0]
    data = bpy.data.cameras.new("Review")
    cam = bpy.data.objects.new("Review", data)
    s.collection.objects.link(cam)
    s.camera = cam
    data.type = 'ORTHO'
    data.clip_start = .0001
    return cam


def capture(rig, meshes, cam, body, path, angle):
    deps = bpy.context.evaluated_depsgraph_get()
    pts = []
    for obj in meshes:
        ev = obj.evaluated_get(deps)
        p = mesh_vertices(ev)
        m = np.array(ev.matrix_world)
        pts.append(p @ m[:3, :3].T + m[:3, 3])
    pts = np.concatenate(pts)
    side = body[:, 0]
    up = body[:, 1]
    front = body[:, 2]
    # Camera faces the person's front. Rotate around the model for the second view.
    direction = unit(front * math.cos(angle) + side * math.sin(angle))
    right = unit(np.cross(direction, up))
    center = sum((np.max(pts @ ax) + np.min(pts @ ax)) * ax * .5 for ax in (right, up, direction))
    height = max(np.ptp(pts @ up), np.ptp(pts @ right) * 340 / 288) * 1.11
    cam.location = Vector(center + direction * height * 3)
    cam.rotation_euler = Vector(-direction).to_track_quat('-Z', 'Y').to_euler()
    cam.data.ortho_scale = height
    cam.data.clip_end = height * 8
    bpy.context.scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)


def finger_pose(rig, names, side):
    def bend(finger):
        keys = [side + "Hand" + finger + str(i) for i in (1, 2, 3)]
        if any(key not in names for key in keys):
            return None
        a, b, c = [rig.pose.bones[names[key]].head for key in keys]
        return math.degrees((b - a).angle(c - b, 0.0))
    index, middle, ring = [bend(finger) for finger in ("Index", "Middle", "Ring")]
    if None in (index, middle, ring):
        return None
    other = (middle + ring) / 2
    return index <= 30 and other >= 40 and other - index >= 25


def review(rig, action, state, edges, meshes, cam, body, path, shots, samples):
    scene = bpy.context.scene
    rig.animation_data_create().action = action
    a, b = action.frame_range
    peaks = []
    gestures = []
    hand_bones = bone_map(rig) if state.startswith("Point") else None
    peak_frame = a
    for fraction in np.linspace(0, 1, samples or max(2, math.ceil(b - a) + 1)):
        f = float(a + (b - a) * fraction)
        whole = math.floor(f)
        scene.frame_set(whole, subframe=f - whole)
        deps = bpy.context.evaluated_depsgraph_get()
        if hand_bones is not None:
            gestures.append(tuple(finger_pose(rig, hand_bones, side) for side in SIDES))
        ratios = []
        for obj, ids, lengths in edges:
            v = mesh_vertices(obj.evaluated_get(deps))
            ratios.append(np.linalg.norm(v[ids[:, 0]] - v[ids[:, 1]], axis=1) / lengths)
        all_ratios = np.concatenate(ratios)
        if not np.isfinite(all_ratios).all():
            raise ValueError("Non-finite skinned vertices")
        peaks.append((float(np.quantile(all_ratios, .99)), float(np.max(all_ratios)), float(f)))
    p99, maxr, peak_frame = max(peaks, key=lambda x: x[0])
    if shots:
        # Normal gesture phase plus an oblique view of the most stretched sampled phase.
        for suffix, frame, angle in [('front', (a + b) / 2, 0), ('oblique', peak_frame, math.radians(43))]:
            scene.frame_set(math.floor(frame), subframe=frame - math.floor(frame))
            capture(rig, meshes, cam, body, path / (state + '_' + suffix + '.jpg'), angle)
    stats = {'samples': len(peaks), 'p99_edge_stretch': round(p99, 4), 'max_edge_stretch': round(max(x[1] for x in peaks), 4),
            'peak_frame': round(peak_frame, 2), 'duration_s': round((b - a) / FPS, 3),
            'status': 'CHECK_STRETCH' if p99 > 1.5 or max(x[1] for x in peaks) > 5 else 'VISUAL_REVIEW'}
    if gestures:
        if any(None in pair for pair in gestures):
            stats['hand_pattern'] = 'MISSING_FINGER_CHAIN'
        elif any(left and right for left, right in gestures):
            stats['hand_pattern'] = 'BOTH_INDEX_FINGERS'
        elif any(left or right for left, right in gestures):
            stats['hand_pattern'] = 'SINGLE_INDEX_FINGER'
        else:
            stats['hand_pattern'] = 'NO_DISTINCT_INDEX'
    return stats


def process_character(key, record, plan, args):
    fresh_scene()
    path = args.base / record['model']
    if record.get('sha256') and sha(path) != record['sha256']:
        raise ValueError("Model differs from the planned SHA-256")
    rig = import_rig(path, False)
    target = rest_data(rig)
    meshes = meshes_of(rig)
    if not meshes:
        raise ValueError("Model has no mesh bound to its armature")
    if rig.animation_data:
        rig.animation_data_clear()
    extras = [o for o in bpy.context.scene.objects if o.type == 'MESH' and o not in meshes]
    for o in extras:
        o.hide_render = True
    edges = rest_edges(meshes)
    scene = bpy.context.scene
    scene.render.fps = FPS
    scene.render.fps_base = 1
    cam = scene_camera(meshes) if args.photos else None
    out = args.output / safe_id(key)
    out.mkdir(parents=True, exist_ok=False)
    result = {'character': key, 'model': path.name, 'model_sha256': sha(path), 'bones': len(target['names']),
              'skinned_vertices': sum(len(o.data.vertices) for o in meshes), 'states': {}}
    profile = record.get('profile', {})
    if isinstance(profile, str):
        profile = plan['profiles'][profile]
    for state, entry in profile.items():
        if args.states and state not in args.states:
            continue
        safe_id(state)
        clip_id = entry if isinstance(entry, str) else entry['clip']
        spec = plan['clips'][clip_id]
        cache = cache_path(spec, args.cache)
        if not cache.is_file():
            raise FileNotFoundError("Run --mode cache first: " + str(cache))
        window = None if isinstance(entry, str) else entry.get('window')
        started = time.monotonic()
        try:
            with np.load(cache, allow_pickle=False) as cached:
                src = dict(cached.items())
            action = bake(rig, target, src, state, window)
            action['source_name'] = spec.get('name', clip_id)
            stats = review(rig, action, state, edges, meshes, cam,
                           target['body'], out, args.photos, args.samples)
            stats.update(clip=spec.get('name', clip_id), source_sha256=spec['sha256'])
            result['states'][state] = stats
        except Exception as exc:
            result['states'][state] = {'status': 'ERROR',
                                       'clip': spec.get('name', clip_id), 'error': str(exc)}
        print("STATE", key, state, result['states'][state]['status'],
              round(time.monotonic() - started, 2), flush=True)
        (out / 'audit.json').write_text(json.dumps(result, indent=2, ensure_ascii=False))
    if not result['states']:
        raise ValueError("No requested states in this profile")
    complete = all(v['status'] != 'ERROR' for v in result['states'].values())
    if complete and args.fbx:
        bpy.ops.object.select_all(action='DESELECT')
        for item in [rig] + meshes:
            item.select_set(True)
        bpy.context.view_layer.objects.active = rig
        bpy.ops.export_scene.fbx(filepath=str(out / (safe_id(key) + '.fbx')), use_selection=True,
                                 object_types={'ARMATURE', 'MESH'}, add_leaf_bones=False, bake_anim=True,
                                 bake_anim_use_nla_strips=False, bake_anim_use_all_actions=True,
                                 bake_anim_simplify_factor=0.0, path_mode='COPY', embed_textures=True)
    if complete and args.blend:
        bpy.ops.file.pack_all()
        if rig.animation_data and rig.animation_data.action:
            scene.frame_start = 1
            scene.frame_end = max(2, math.ceil(rig.animation_data.action.frame_range[1]))
            scene.frame_set(1)
        bpy.ops.wm.save_as_mainfile(filepath=str(out / (safe_id(key) + '.blend')), compress=True)
    return complete


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--plan', type=Path, required=True)
    parser.add_argument('--mode', choices=['cache', 'batch', 'all'], default='all')
    parser.add_argument('--cache', type=Path, required=True)
    parser.add_argument('--output', type=Path)
    parser.add_argument('--characters', nargs='+')
    parser.add_argument('--states', nargs='+')
    parser.add_argument('--photos', action='store_true')
    parser.add_argument('--samples', type=int, default=9,
                        help='Mesh checks per action. 0 checks every output frame.')
    parser.add_argument('--blend', action='store_true')
    parser.add_argument('--fbx', action='store_true')
    args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
    if args.samples < 0 or args.samples == 1:
        parser.error('--samples must be 0 or at least 2')
    args.base = args.plan.resolve().parent
    args.cache = args.cache.resolve()
    plan = json.loads(args.plan.read_text())
    if args.mode in ('cache', 'all'):
        cache_clips(plan, args.base, args.cache)
    if args.mode == 'cache':
        return
    if args.output is None:
        parser.error('--output is required for batch or all')
    args.output = args.output.resolve()
    args.output.mkdir(parents=True, exist_ok=True)
    ok = True
    for key in args.characters or plan['characters']:
        try:
            ok = process_character(key, plan['characters'][key], plan, args) and ok
        except Exception as exc:
            print('CHARACTER ERROR', key, repr(exc), flush=True)
            ok = False
    if not ok:
        raise RuntimeError("Review errors above and the per-character audit files")


if __name__ == '__main__':
    main()
