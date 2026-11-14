"""Apply a pedestrian Walk to existing Blender character files in a batch.

blender -b --python-exit-code 1 -P spacer_mixamo.py -- Walking.fbx out/ Gitara.blend Kiler.blend
"""
from argparse import ArgumentParser
from hashlib import sha256
import math
from pathlib import Path
import sys

import bpy
import numpy as np
from mathutils import Matrix, Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
from rokoko_postacie_t import curves, import_fbx, named_bones, pack_textures, signature


def basis(primary, guide):
    primary = primary.normalized()
    side = (guide - primary * guide.dot(primary)).normalized()
    if side.length < 0.9:
        raise ValueError('Nie można wyznaczyć płaszczyzny kości')
    return Matrix((side, primary, side.cross(primary))).transposed().to_quaternion()


def rig_geometry(rig, pose_reference=False):
    names = named_bones(rig)
    bind = {n: rig.matrix_world @ (rig.pose.bones[real].matrix if pose_reference else rig.data.bones[real].matrix_local)
            for n, real in names.items()}
    coords = {n: m.translation for n, m in bind.items()}
    needed = ['Hips', 'Spine', 'Head'] + [side + item for side in ('Left', 'Right')
              for item in ('UpLeg', 'Leg', 'Foot', 'ToeBase', 'Arm', 'ForeArm', 'Hand',
                           'HandMiddle1', 'HandIndex1', 'HandPinky1')]
    missing = [key for key in needed if key not in coords]
    if missing:
        raise ValueError('Brak kości ' + ', '.join(missing))
    lateral = coords['LeftUpLeg'] - coords['RightUpLeg']
    # Blender imports both files in a Z-up world. A forward-leaning neck does not define the floor normal.
    frame = basis(Vector((0, 0, 1)), lateral)
    for side, sign in [('Left', 1), ('Right', -1)]:
        arm = coords[side+'Hand'] - coords[side+'Arm']
        if math.degrees(arm.angle(sign*lateral)) > 25:
            raise ValueError('Źródło lub model nie ma zgodnej referencji T: ' + side)
    leg = sum((coords['Left' + a] - coords['Left' + b]).length
              for a, b in [('UpLeg', 'Leg'), ('Leg', 'Foot')])
    return names, bind, coords, frame, leg


def segment_map(names):
    children = {'Hips': 'Spine', 'Spine': 'Spine1', 'Spine1': 'Spine2', 'Spine2': 'Neck',
                'Neck': 'Head', 'Head': 'HeadTop_End'}
    for side in ('Left', 'Right'):
        for chain in [('Shoulder', 'Arm', 'ForeArm', 'Hand', 'HandMiddle1'),
                      ('UpLeg', 'Leg', 'Foot', 'ToeBase', 'Toe_End')]:
            children.update({side + a: side + b for a, b in zip(chain, chain[1:])})
        for finger in ('Thumb', 'Index', 'Middle', 'Ring', 'Pinky'):
            children.update({side + 'Hand' + finger + str(i): side + 'Hand' + finger + str(i+1)
                             for i in range(1, 4)})
    return {n: c for n, c in children.items() if n in names and c in names}


def read_source(path, walk_track=None, reference_track=None):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    if path.suffix.lower() == '.glb':
        if not walk_track or not reference_track:
            raise ValueError('Podaj nazwy klipu chodu i referencyjnej pozy T z pliku GLB')
        scene.render.fps = 60
        bpy.ops.import_scene.gltf(filepath=str(path))
    else:
        bpy.ops.import_scene.fbx(filepath=str(path), use_anim=True)
    rigs = [o for o in bpy.context.scene.objects if o.type == 'ARMATURE' and o.animation_data and o.animation_data.action]
    if len(rigs) != 1:
        raise ValueError('Źródło musi mieć jeden animowany szkielet')
    rig = rigs[0]
    if reference_track:
        # Read Blender's imported tracks by their original glTF animation names.
        tracks = {t.name: t.strips[0].action for t in rig.animation_data.nla_tracks if len(t.strips) == 1}
        reference, walk = tracks[reference_track], tracks[walk_track]
        for track in rig.animation_data.nla_tracks:
            track.mute = True
        rig.animation_data.action = reference
        scene.frame_set(round(reference.frame_range[0]))
    names, bind, points, axes, leg = rig_geometry(rig, bool(reference_track))
    if reference_track:
        rig.animation_data.action = walk
    lo, hi = map(round, rig.animation_data.action.frame_range)
    fps = scene.render.fps / scene.render.fps_base
    root = rig.pose.bones[names['Hips']]
    samples = []
    for frame in range(lo, hi+1):
        scene.frame_set(frame)
        rotations = {n: (rig.matrix_world @ rig.pose.bones[real].matrix).to_quaternion() @ bind[n].to_quaternion().inverted()
                     for n, real in names.items()}
        samples.append((rig.matrix_world @ root.head - points['Hips'], rotations))
    return points, axes, leg, samples, fps


def apply_walk(path, out, source, source_sha):
    if path.suffix.lower() == '.fbx':
        bpy.ops.wm.read_factory_settings(use_empty=True)
        import_fbx(path, False)
        bpy.context.scene.render.fps = 60
        bpy.context.scene.render.fps_base = 1
    else:
        bpy.ops.wm.open_mainfile(filepath=str(path))
    scene = bpy.context.scene
    rigs = [o for o in scene.objects if o.type == 'ARMATURE']
    if len(rigs) != 1:
        raise ValueError('Oczekiwano jednego szkieletu na plik')
    rig = rigs[0]
    names, bind, points, axes, leg = rig_geometry(rig)
    meshes = [o for o in scene.objects if o.type == 'MESH' and o.find_armature() == rig]
    if not meshes:
        raise ValueError('Brak siatki przypiętej do szkieletu')
    before = signature(rig, meshes)
    src, src_axes, src_leg, samples, fps = source
    body = axes @ src_axes.inverted()
    children = segment_map(set(names) & set(src))
    corrections = {}
    for n, child in children.items():
        dest_dir = points[child] - points[n]
        src_dir = body @ (src[child] - src[n])
        if min(dest_dir.length, src_dir.length) < 1e-7:
            continue
        if n in ('LeftHand', 'RightHand'):
            pa, pb = n + 'Index1', n + 'Pinky1'
            corrections[n] = basis(src_dir, body @ (src[pa] - src[pb])) @ basis(dest_dir, points[pa] - points[pb]).inverted()
        else:
            corrections[n] = dest_dir.rotation_difference(src_dir)
    rest = {n: m.to_quaternion() for n,m in bind.items()}
    parents = {n: rig.pose.bones[real].parent.name.split(':')[-1] if rig.pose.bones[real].parent else None
               for n,real in names.items()}
    rotations = {n: [] for n in names}
    hips = []
    for translation, delta in samples:
        world = {}
        for n in names:
            parent = parents[n]
            if n in delta and n in corrections:
                world[n] = body @ delta[n] @ body.inverted() @ corrections[n] @ rest[n]
            elif parent:
                world[n] = world[parent] @ rest[parent].inverted() @ rest[n]
            else:
                world[n] = rest[n]
            q = ((rest[parent].inverted() @ rest[n]).inverted() @ world[parent].inverted() @ world[n]
                 if parent else rest[n].inverted() @ world[n]).normalized()
            if rotations[n]:
                q.make_compatible(rotations[n][-1])
            rotations[n].append(q)
        hips.append(bind['Hips'].to_3x3().inverted() @ (body @ translation) * (leg/src_leg))

    # Keep existing active/NLA bindings to the Walk action intact and retain every other action.
    action = bpy.data.actions.get('Walk') or bpy.data.actions.new('Walk')
    for curve in list(action.fcurves):
        action.fcurves.remove(curve)
    scene_rate = scene.render.fps / scene.render.fps_base
    times = [1 + i * scene_rate / fps for i in range(len(samples))]
    for n, real in names.items():
        bone = rig.pose.bones[real]
        bone.rotation_mode = 'QUATERNION'
        curves(action, bone.path_from_id('rotation_quaternion'), times, rotations[n])
    curves(action, rig.pose.bones[names['Hips']].path_from_id('location'), times, hips)
    action.use_fake_user = True
    rig.animation_data_create().action = action
    scene.frame_start = 1
    scene.frame_end = math.ceil(times[-1])
    scene.frame_set(1)
    if signature(rig, meshes) != before:
        raise ValueError('Zmiana geometrii lub szkieletu')
    # A walking step always has at least one supporting foot. Use the evaluated soles
    # to correct only the vertical pelvis translation for this character's own shoes.
    loc = rig.pose.bones[names['Hips']].path_from_id('location')
    root_curves = [action.fcurves.find(loc, index=i) for i in range(3)]
    local_up = bind['Hips'].to_3x3().inverted() @ Vector((0, 0, 1))
    soles = []
    for mesh in meshes:
        groups = {g.index for g in mesh.vertex_groups if g.name.split(':')[-1] in
                  ('LeftFoot', 'RightFoot', 'LeftToeBase', 'RightToeBase')}
        indices = [v.index for v in mesh.data.vertices if sum(g.weight for g in v.groups if g.group in groups) > 0.5]
        if indices:
            soles.append((mesh, indices, np.zeros(len(mesh.data.vertices)*3, dtype=np.float32)))
    if not soles:
        raise ValueError('Brak wierzchołków stóp do pomiaru podparcia')
    shifts = []
    for t in times:
        scene.frame_set(int(t), subframe=t - int(t))
        heights = []
        for mesh, idx, buffer in soles:
            posed = mesh.evaluated_get(bpy.context.evaluated_depsgraph_get())
            posed.data.vertices.foreach_get('co', buffer)
            row = np.asarray(posed.matrix_world[2])
            heights.append(np.min(buffer.reshape(-1, 3)[idx] @ row[:3] + row[3]))
        shifts.append(float(min(heights)))
    for axis, curve in enumerate(root_curves):
        for key, offset in zip(curve.keyframe_points, shifts):
            key.co.y -= offset * local_up[axis]
        curve.update()
    scene.frame_set(1)
    if path.suffix.lower() == '.fbx':
        pack_textures(meshes)
    action['source_sha256'] = source_sha
    bpy.ops.wm.save_as_mainfile(filepath=str(out), compress=True)


if __name__ == '__main__':
    args = ArgumentParser()
    args.add_argument('chod', type=Path)
    args.add_argument('katalog_wynikowy', type=Path)
    args.add_argument('postacie', nargs='+', type=Path)
    args.add_argument('--ruch', help='nazwa animacji chodu w GLB')
    args.add_argument('--poza', help='nazwa animacji referencyjnej T w GLB')
    opt = args.parse_args(sys.argv[sys.argv.index('--')+1:])
    dest = opt.katalog_wynikowy.resolve()
    jobs = [(p.resolve(), dest / (p.stem + '.blend')) for p in opt.postacie]
    if len({out for _,out in jobs}) != len(jobs) or any(out.exists() or not p.is_file() for p,out in jobs):
        raise ValueError('Podaj istniejące modele i nowe, jednoznaczne ścieżki wynikowe')
    source_sha = sha256(opt.chod.read_bytes()).hexdigest()
    source = read_source(opt.chod.resolve(), opt.ruch, opt.poza)
    dest.mkdir(parents=True, exist_ok=True)
    for path,out in jobs:
        apply_walk(path, out, source, source_sha)
