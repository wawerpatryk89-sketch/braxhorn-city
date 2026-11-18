"""Transfer the forward jump already supplied in the T-pose cast onto the player's own skeleton."""
import math
import bpy
import numpy as np
from mathutils import Matrix, Quaternion, Vector


def body(rig, bones):
    pos = lambda name: rig.matrix_world @ bones[name].bone.head_local
    up = (pos("Neck") - pos("Hips")).normalized()
    side = pos("LeftArm") - pos("RightArm")
    side = (side - up * side.dot(up)).normalized()
    return Matrix((side, up, side.cross(up))).transposed().to_quaternion()


def read_take(path):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.scene.render.fps = 30
    bpy.ops.import_scene.fbx(filepath=str(path))
    rig = next(o for o in bpy.context.scene.objects if o.type == "ARMATURE")
    bones = {b.name.split(":")[-1]: b for b in rig.pose.bones}
    basis = body(rig, bones)
    for tr in rig.animation_data.nla_tracks:
        tr.mute = True
    action = next(a for a in bpy.data.actions if a.name.rsplit("|", 1)[-1] == "Jump" and any("pose.bones" in c.data_path for c in a.fcurves))
    rig.animation_data.action = action
    names = list(bones)
    rest = {n: (rig.matrix_world @ bones[n].bone.matrix_local).to_quaternion() for n in names}
    poses, hips = [], []
    for frame in range(math.ceil(action.frame_range[0]), math.floor(action.frame_range[1])+1):
        bpy.context.scene.frame_set(frame)
        poses.append({n: basis.inverted() @ (rig.matrix_world @ bones[n].matrix).to_quaternion() @ rest[n].inverted() @ basis for n in names})
        hips.append(basis.inverted() @ (rig.matrix_world @ bones["Hips"].head))
    if hips[-1].z <= hips[0].z:
        raise ValueError("Expected forward travel in the supplied jump")
    return poses


def replace_jump(rig, action, take, mesh):
    from osiedle_ruch import add_curves, mesh_vertices
    bones = {b.name.split(":")[-1]: b for b in rig.pose.bones}
    basis = body(rig, bones)
    rest = {n: (rig.matrix_world @ b.bone.matrix_local).to_quaternion() for n, b in bones.items()}
    parents = {n: b.parent.name.split(":")[-1] if b.parent else None for n, b in bones.items()}
    identity = Quaternion()
    frames = np.arange(1, len(take)+1)
    for curve in list(action.fcurves):
        action.fcurves.remove(curve)
    for n, bone in bones.items():
        values = []
        for pose in take:
            parent_delta = pose.get(parents[n], identity)
            delta = parent_delta.inverted() @ pose.get(n, parent_delta)
            q = rest[n].inverted() @ basis @ delta @ basis.inverted() @ rest[n]
            if values and q.dot(values[-1]) < 0:
                q.negate()
            values.append(q)
        add_curves(action, bone.path_from_id("rotation_quaternion"), frames, np.asarray(values))
    # The controller supplies the whole trajectory. Preserve limb articulation,
    # start/end shoe contact and vertical landing compression in the take.
    rig.animation_data.action = action
    up = np.asarray(basis @ Vector((0,1,0)))
    mt = np.asarray(mesh.matrix_world)
    def sole(obj):
        return float(((mesh_vertices(obj) @ mt[:3,:3].T + mt[:3,3]) @ up).min())
    floor = sole(mesh)
    graph = bpy.context.evaluated_depsgraph_get()
    hip = bones["Hips"]
    vertical = np.linalg.solve(np.asarray((rig.matrix_world @ hip.bone.matrix_local).to_3x3()),up)
    offsets = []
    for i,frame in enumerate(frames):
        bpy.context.scene.frame_set(int(frame))
        hip.location = (0,0,0)
        bpy.context.view_layer.update()
        phase = i / (len(frames)-1)
        # Ground the supporting shoe during takeoff and landing, leaving the
        # original lifted knee and the airborne stride between the two contacts.
        weight = np.clip(max((.24-phase)/.11, (phase-.65)/.13),0,1)
        weight = weight*weight*(3-2*weight)
        offsets.append(vertical * (floor-sole(mesh.evaluated_get(graph))) * weight)
    add_curves(action, hip.path_from_id("location"), frames, np.asarray(offsets))
