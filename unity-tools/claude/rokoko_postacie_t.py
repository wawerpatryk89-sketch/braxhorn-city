"""Blender 4.3, Rokoko Studio Live 1.4.4. Create one review .blend from a character and its T profile."""
import argparse
from hashlib import sha256
import json
import math
from pathlib import Path
import struct
import sys
from tempfile import TemporaryDirectory
sys.path.insert(0, str(Path(__file__).resolve().parent))
from paczki_ruchow_t import open_clips

import bpy


def import_fbx(path, animated):
    before = set(bpy.context.scene.objects)
    bpy.ops.import_scene.fbx(filepath=str(path), use_anim=animated, automatic_bone_orientation=animated)
    objects = set(bpy.context.scene.objects) - before
    rigs = [obj for obj in objects if obj.type == 'ARMATURE']
    if len(rigs) != 1:
        raise ValueError(f'{path} zawiera {len(rigs)} szkieletów')
    return rigs[0], objects


def named_bones(rig):
    pairs = {}
    for bone in rig.pose.bones:
        name = bone.name.split(':')[-1]
        if name in pairs:
            raise ValueError(f'Niejednoznaczna kość {name} w {rig.name}')
        pairs[name] = bone.name
    return pairs


def signature(rig, meshes):
    digest = sha256()
    for mesh in sorted(meshes, key=lambda o: o.name):
        for vertex in mesh.data.vertices:
            digest.update(struct.pack('<3f', *vertex.co))
            for group in vertex.groups:
                digest.update(struct.pack('<if', group.group, group.weight))
    for bone in rig.data.bones:
        digest.update(bone.name.encode())
        for row in bone.matrix_local:
            digest.update(struct.pack('<4f', *row))
    return digest.hexdigest()


def curves(action, path, frames, values):
    for axis in range(len(values[0])):
        curve = action.fcurves.new(path, index=axis)
        keys = curve.keyframe_points
        keys.add(len(values))
        keys.foreach_set('co', [v for frame, row in zip(frames, values) for v in (frame, row[axis])])
        for key in keys:
            key.interpolation = 'LINEAR'
        curve.update()


def apply_action(rig, proxy, hips, name):
    """The add-on applies object transforms to its target. Read its result onto the untouched FBX rig."""
    source = proxy.animation_data.action
    first, last = map(round, source.frame_range)
    bones = list(rig.pose.bones)

    def bind(armature, bone):
        return armature.matrix_world @ armature.data.bones[bone].matrix_local

    rest = {b.name: bind(rig, b.name).to_quaternion() for b in bones}
    work = {b.name: bind(proxy, b.name).to_quaternion() for b in bones}
    work_root = bind(proxy, hips).translation
    local = bind(rig, hips).to_3x3().inverted()
    values = {b.name: [] for b in bones}
    shifts = []
    for frame in range(first, last + 1):
        bpy.context.scene.frame_set(frame)
        world = {}
        for bone in bones:
            n = bone.name
            sample = proxy.matrix_world @ proxy.pose.bones[n].matrix
            world[n] = sample.to_quaternion() @ work[n].inverted() @ rest[n]
            if bone.parent:
                p = bone.parent.name
                rotation = (rest[p].inverted() @ rest[n]).inverted() @ world[p].inverted() @ world[n]
            else:
                rotation = rest[n].inverted() @ world[n]
            rotation.normalize()
            if values[n]:
                rotation.make_compatible(values[n][-1])
            values[n].append(rotation)
            if n == hips:
                shifts.append(local @ (sample.translation - work_root))

    fps = bpy.context.scene.render.fps / bpy.context.scene.render.fps_base
    frames = [1 + (frame - first) * 60 / fps for frame in range(first, last + 1)]
    action = bpy.data.actions.new(name)
    for bone in bones:
        bone.rotation_mode = 'QUATERNION'
        curves(action, bone.path_from_id('rotation_quaternion'), frames, values[bone.name])
    curves(action, rig.pose.bones[hips].path_from_id('location'), frames, shifts)
    action.use_fake_user = True
    return action


def pack_textures(meshes):
    for mesh in meshes:
        for material in mesh.data.materials:
            if not material or not material.use_nodes:
                continue
            for node in material.node_tree.nodes:
                if node.type != 'TEX_IMAGE' or not node.image:
                    continue
                image = node.image
                width, height = image.size
                if not width or not height:
                    raise ValueError(f'Brak tekstury {image.name}')
                if max(width, height) > 2048:
                    scale = 2048 / max(width, height)
                    image.scale(max(1, round(width * scale)), max(1, round(height * scale)))
                    # Pack the resized pixels in memory and preserve the original FBX and external files.
                    image.pack()
                elif not image.packed_file:
                    image.pack()


def retarget(rig, source_file, label):
    scene = bpy.context.scene
    existing_actions = set(bpy.data.actions)
    source, objects = import_fbx(source_file, True)
    result = None
    proxy = rig.copy()
    proxy.data = rig.data.copy()
    proxy.animation_data_clear()
    scene.collection.objects.link(proxy)
    try:
        scene.rsl_retargeting_armature_source = source
        scene.rsl_retargeting_armature_target = proxy
        scene.rsl_retargeting_auto_scaling = True
        scene.rsl_retargeting_use_pose = 'REST'
        if bpy.ops.rsl.build_bone_list() != {'FINISHED'}:
            raise ValueError('Rokoko nie odczytało szkieletów')
        target_names = named_bones(rig)
        mapped = set()
        for entry in scene.rsl_retargeting_bone_list:
            name = entry.bone_name_source.split(':')[-1]
            entry.bone_name_target = target_names.get(name, '')
            if entry.bone_name_target:
                mapped.add(name)
        required = {'Hips', 'Spine', 'Head'}
        required.update(side + part for side in ('Left', 'Right') for part in
                        ('Arm', 'ForeArm', 'Hand', 'HandIndex1', 'HandPinky1', 'UpLeg', 'Leg', 'Foot', 'ToeBase'))
        # Unkeyed/resting bones need no animation binding. Explicit names complete Rokoko's animated-only list.
        source_names = named_bones(source)
        for name in sorted(required - mapped):
            if name not in source_names or name not in target_names:
                raise ValueError(f'{label} nie ma kości {name}')
            entry = scene.rsl_retargeting_bone_list.add()
            entry.bone_name_source = source_names[name]
            entry.bone_name_target = target_names[name]
        if bpy.ops.rsl.retarget_animation() != {'FINISHED'} or not proxy.animation_data.action:
            raise ValueError(f'Rokoko nie przygotowało {label}')
        result = apply_action(rig, proxy, target_names['Hips'], label)
        return result
    finally:
        bpy.data.objects.remove(proxy, do_unlink=True)
        for obj in objects:
            bpy.data.objects.remove(obj, do_unlink=True)
        for action in set(bpy.data.actions) - existing_actions:
            if action != result:
                bpy.data.actions.remove(action)
        bpy.data.orphans_purge(do_recursive=True)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('model', type=Path)
    parser.add_argument('paczka_t', type=Path)
    parser.add_argument('katalog', type=Path)
    parser.add_argument('postac')
    parser.add_argument('wynik', type=Path)
    parser.add_argument('--ruchy', nargs='+')
    args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
    if args.wynik.suffix != '.blend' or args.wynik.exists():
        raise ValueError('Podaj nowy plik .blend')
    model = args.model.resolve()
    catalog = json.loads(args.katalog.read_text())
    profile = catalog['profiles'][args.postac]
    bpy.ops.wm.read_factory_settings(use_empty=True)
    if not hasattr(bpy.types.Scene, 'rsl_retargeting_bone_list'):
        raise RuntimeError('Włącz oficjalny dodatek Rokoko Studio Live 1.4.4 w Blenderze')
    rig, objects = import_fbx(model, False)
    meshes = [obj for obj in objects if obj.type == 'MESH']
    if not meshes:
        raise ValueError('Brak modelu ze skórą')
    original = signature(rig, meshes)
    actions = []
    with open_clips(catalog, args.paczka_t) as read, TemporaryDirectory() as tmp:
        for state in args.ruchy or profile.keys():
            clip = catalog['clips'][profile[state]]
            data = read(clip)
            clone = Path(tmp) / 'source.fbx'
            clone.write_bytes(data)
            action = retarget(rig, clone, state)
            if signature(rig, meshes) != original:
                raise ValueError('Zmienione wiązanie lub geometria modelu ' + state)
            actions.append(action)
    rig.animation_data_create().action = actions[0]
    scene = bpy.context.scene
    scene.render.fps = 60
    scene.render.fps_base = 1
    scene.frame_start = 1
    scene.frame_end = math.ceil(actions[0].frame_range[1])
    scene.frame_set(1)
    pack_textures(meshes)
    args.wynik.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(args.wynik.resolve()), compress=True)


if __name__ == '__main__':
    main()
