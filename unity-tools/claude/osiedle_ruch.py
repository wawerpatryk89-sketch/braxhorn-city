"""Prepare the district cast from its approved FBXs during private staging in Blender."""

import math
import sys
import zlib
from pathlib import Path

import bpy
import numpy as np
from mathutils import Quaternion, Vector


def mesh_vertices(obj):
    result = np.empty(len(obj.data.vertices) * 3, dtype=np.float32)
    obj.data.vertices.foreach_get("co", result)
    return result.reshape(-1, 3)


def add_curves(action, path, frames, values):
    for axis, values_axis in enumerate(values.T):
        curve = action.fcurves.new(path, index=axis)
        points = curve.keyframe_points
        points.add(len(frames))
        points.foreach_set(
            "co", np.column_stack((frames, values_axis)).astype("f").ravel()
        )
        points.foreach_set("interpolation", np.ones(len(frames), dtype="i"))
        curve.update()


# The gait follows each rig's measured footfalls. Upper-body changes leave the foot plants intact.
WALK_STYLES = {
    # cycle duration scale, torso counter-rotation, arm swing scale
    "ewa": (1.06, 2.6, 0.84),
    "martyna": (1.02, 3.3, 0.92),
    "aneta": (0.97, 3.8, 0.94),
    "monika": (1.04, 2.9, 0.86),
    "imprezowiczka_0dbe3922": (0.96, 3.1, 0.90),
    "imprezowiczka_11baefe5": (1.03, 3.5, 0.88),
    "imprezowiczka_13e8d41d": (0.99, 2.8, 0.91),
}


def refine_walk(scene, rig, bone_names, action, body_axes, actor):
    duration, twist, swing = WALK_STYLES[actor]
    rig.animation_data.action = action
    lo, hi = action.frame_range
    frames = np.arange(math.ceil(lo), math.floor(hi) + 1)
    forward = Vector(body_axes[:, 2])
    up = Vector(body_axes[:, 1])
    samples = []
    for frame in frames:
        scene.frame_set(int(frame))
        feet = [
            rig.matrix_world @ rig.pose.bones[bone_names[side + "Foot"]].head
            for side in ("Left", "Right")
        ]
        samples.append((feet[0] - feet[1]).dot(forward))
    amplitude = np.ptp(samples) / 2
    if amplitude < 1e-6:
        raise ValueError(f"{actor}: walk has no alternating footfalls")
    stride = (np.array(samples) - (max(samples) + min(samples)) / 2) / amplitude
    scene.frame_set(int(frames[0]))
    for name in ("Spine1", "Spine2", "LeftArm", "RightArm", "LeftHand", "RightHand"):
        bone = rig.pose.bones[bone_names[name]]
        path = bone.path_from_id("rotation_quaternion")
        curves = [action.fcurves.find(path, index=index) for index in range(4)]
        if not all(curves):
            raise ValueError(f"{actor}: incomplete gait rotations for {name}")
        quats = [
            Quaternion([c.evaluate(float(t)) for c in curves]).normalized()
            for t in frames
        ]
        if name.startswith("Spine"):
            axis = (rig.matrix_world @ bone.matrix).to_quaternion().inverted() @ up
            angles = stride * math.radians(twist * (0.55 if name == "Spine1" else 0.45))
            updated = [q @ Quaternion(axis, float(a)) for q, a in zip(quats, angles)]
        elif name.endswith("Arm"):
            # Preserve the resting arm angle and reduce only the excursion about the midpoint.
            other = int(np.argmax(stride))
            center = quats[int(np.argmin(stride))].slerp(quats[other], 0.5)
            updated = [center.slerp(q, swing) for q in quats]
        else:
            updated = [Quaternion().slerp(q, 0.78) for q in quats]
        for previous, current in zip(updated, updated[1:]):
            current.make_compatible(previous)
        for curve in curves:
            action.fcurves.remove(curve)
        add_curves(action, path, frames, np.array(updated))
    for curve in action.fcurves:
        for point in curve.keyframe_points:
            point.co.x = lo + (point.co.x - lo) * duration
        curve.update()


def ground_run(scene, rig, hip, action, meshes, up):
    """Keep joint articulation and flight, remove take displacement owned by the controller."""
    rig.animation_data.action = action
    start, end = action.frame_range
    frames = np.arange(math.ceil(start), math.floor(end) + 1)
    path = hip.path_from_id("location")
    curves = [action.fcurves.find(path, index=i) for i in range(3)]
    if not all(curves) or len(frames) < 3:
        raise ValueError("Incomplete pelvis translation in Run")
    values = np.array([[curve.evaluate(float(t)) for curve in curves] for t in frames])
    values -= np.linspace(0, 1, len(frames))[:, None] * (values[-1] - values[0])
    for curve in curves:
        action.fcurves.remove(curve)
    add_curves(action, path, frames, values)

    def sole_height(objects):
        levels = []
        for obj in objects:
            matrix = np.array(obj.matrix_world)
            positions = mesh_vertices(obj) @ matrix[:3, :3].T + matrix[:3, 3]
            levels.append(np.min(positions @ up))
        return min(levels)

    ground = sole_height(meshes)
    soles = []
    for frame in frames:
        scene.frame_set(int(frame))
        graph = bpy.context.evaluated_depsgraph_get()
        soles.append(sole_height([mesh.evaluated_get(graph) for mesh in meshes]))
    # Unity grounds the rest mesh once. Set only a constant height offset so the
    # lowest contact touches that same plane, preserving the two flight phases.
    hip_axes = np.array((rig.matrix_world @ hip.bone.matrix_local).to_3x3())
    offset = np.linalg.solve(hip_axes, up * (ground - min(soles)))
    for i in range(3):
        curve = action.fcurves.find(path, index=i)
        for point in curve.keyframe_points:
            point.co.y += float(offset[i])
        curve.update()


def align_planted_soles(scene, rig, hip, mesh, up, action):
    """Keep a supporting shoe on the same ground plane as running toe-off."""
    rig.animation_data.action = action
    world = np.asarray(mesh.matrix_world)
    floor = np.min((mesh_vertices(mesh) @ world[:3, :3].T + world[:3, 3]) @ up)
    hip_axes = np.asarray((rig.matrix_world @ hip.bone.matrix_local).to_3x3())
    direction = np.linalg.solve(hip_axes, up)
    lo, hi = action.frame_range
    frames = np.arange(math.ceil(lo), math.floor(hi) + 1)
    values = []
    for frame in frames:
        scene.frame_set(int(frame))
        evaluated = mesh.evaluated_get(bpy.context.evaluated_depsgraph_get())
        sole = np.min((mesh_vertices(evaluated) @ world[:3, :3].T + world[:3, 3]) @ up)
        values.append(np.asarray(hip.location).copy() + (floor - sole) * direction)
    path = hip.path_from_id("location")
    for curve in list(action.fcurves):
        if curve.data_path == path:
            action.fcurves.remove(curve)
    add_curves(action, path, frames, np.asarray(values))


def prepare(source, output):
    actor = source.stem
    if actor == "patryk":
        from osiedle_skok import read_take, replace_jump
        jump = read_take(source.parent.parent / "ewa" / "ewa.fbx")
    bpy.ops.wm.read_factory_settings(use_empty=True)
    s = bpy.context.scene
    s.render.fps = 30
    bpy.ops.import_scene.fbx(
        filepath=str(source), automatic_bone_orientation=False, ignore_leaf_bones=False
    )
    (r,) = [o for o in bpy.context.selected_objects if o.type == "ARMATURE"]
    for track in r.animation_data.nla_tracks:
        track.mute = True
    ms = [
        o
        for o in s.objects
        if o.type == "MESH"
        and any(m.type == "ARMATURE" and m.object == r for m in o.modifiers)
    ]
    nm = {bone.name.split(":")[-1]: bone.name for bone in r.data.bones}
    names = list(nm)
    ind = {name: i for i, name in enumerate(names)}
    P = np.array(
        [list(r.matrix_world @ r.data.bones[nm[name]].head_local) for name in names]
    )
    up = P[ind["Neck"]] - P[ind["Hips"]]
    up /= np.linalg.norm(up)
    side = P[ind["LeftArm"]] - P[ind["RightArm"]]
    side -= up * (up @ side)
    side /= np.linalg.norm(side)
    axes = np.column_stack((side, up, np.cross(side, up)))
    from osiedle_obrys import refine_outline

    refine_outline(ms[0], up)

    # WYGŁADZENIE WAG NA STAWACH. Zmierzone na paczce wejściowej: 43–68 % wierzchołków
    # trzyma jedna albo dwie kości, więc na zgięciu robi się załamanie zamiast fałdy.
    # Szczegóły i liczby w osiedle_wagi.py.
    from osiedle_wagi import wygladz_wagi

    for mesh in ms:
        wygladz_wagi(mesh, r)
    acts = {
        a.name.rsplit("|", 1)[-1]: a
        for a in bpy.data.actions
        if any("pose.bones" in c.data_path for c in a.fcurves)
    }
    for k, a in acts.items():
        a.name = k
    if actor == "patryk":
        replace_jump(r, acts["Jump"], jump, ms[0])
    if "Run" in acts:
        ground_run(s, r, r.pose.bones[nm["Hips"]], acts["Run"], ms, axes[:, 1])
    if actor in WALK_STYLES:
        refine_walk(s, r, nm, acts["Walk"], axes, actor)
    original = acts["Idle"]
    r.animation_data.action = original
    lo, hi = original.frame_range
    h = np.ptp(
        np.concatenate(
            [np.array([m.matrix_world @ v.co for v in m.data.vertices]) for m in ms]
        )
        @ axes[:, 1]
    )
    watch = ["Hips", "Head", "LeftHand", "RightHand", "LeftFoot", "RightFoot"]
    sample = np.arange(lo, hi, 5)
    xyz = []
    for t in sample:
        s.frame_set(int(t), subframe=t - int(t))
        xyz.append([list(r.matrix_world @ r.pose.bones[nm[k]].head) for k in watch])
    xyz = np.asarray(xyz) / h
    seed = zlib.crc32(actor.encode())
    span = 30 + seed % 13
    # DŁUGOŚĆ OKNA DOPASOWANA DO NAGRANIA. Okno ma 30–42 próbek co 5 klatek, czyli
    # potrzebuje ponad 150 klatek materiału. Nagrania z globalnej biblioteki ruchów
    # bywają krótsze i wtedy pętla niżej nie wykonywała się ANI RAZU, a candidate[...]
    # wywalało się na pustej liście (build 44287649268: Brax i Nyxa, IndexError bez
    # słowa wyjaśnienia). Przy krótkim nagraniu bierzemy najdłuższe okno, jakie wchodzi.
    if len(sample) - span - 1 <= 0:
        span = max(4, len(sample) - 2)
    candidate = []
    for j in range(len(sample) - span - 1):
        v = xyz[j : j + span + 1]
        feet = np.linalg.norm(np.ptp(v[:, 4:], axis=0), axis=1).max()
        wrists = v[:, 2:4] - v[:, :1]
        travel = np.linalg.norm(np.ptp(wrists, axis=0), axis=1)
        startmotion = np.linalg.norm(np.ptp(wrists[:7], axis=0), axis=1).mean()
        seam = np.linalg.norm(v[-1] - v[0], axis=1).mean()
        score = (
            feet * 2.3
            + seam * 1.8
            + abs(travel.mean() - 0.090) * 0.62
            + abs(startmotion - 0.027) * 1.05
        )
        candidate.append((score, j))
    candidate.sort()
    if not candidate:
        raise ValueError(
            "%s: nagranie %s ma tylko %d probek co 5 klatek — za krotkie na petle chodu"
            % (actor, original.name, len(sample)))
    j = candidate[seed % min(10, len(candidate))][1]
    ts = np.arange(sample[j], sample[j + span] + 1)
    n = len(ts)
    phase = np.linspace(0, 1, n)
    # Blend the final quarter to the initial posture in joint space. The source interval is selected to keep both feet planted.
    blend = np.clip((phase - 0.72) / 0.28, 0, 1)
    blend = blend * blend * (3 - 2 * blend)
    tt = np.arange(1, n + 1, dtype=float)
    new = bpy.data.actions.new("_spoczynek")
    new.use_fake_user = True
    cache = {(c.data_path, c.array_index): c for c in original.fcurves}

    def sample_curves(path, default):
        return np.stack(
            [
                (
                    np.array([c.evaluate(float(t)) for t in ts])
                    if (c := cache.get((path, i)))
                    else np.full(n, x)
                )
                for i, x in enumerate(default)
            ],
            axis=1,
        )

    s.frame_set(int(ts[n // 2]))

    def world_point(k):
        return np.asarray(r.matrix_world @ r.pose.bones[nm[k]].head)

    phase0 = (seed % 113) * 2 * np.pi / 113

    def secondary(q, bone, name):
        key = (
            name.split("Left")[-1]
            if name.startswith("Left")
            else name.split("Right")[-1] if name.startswith("Right") else name
        )
        sign = 1 if name.startswith("Left") else -1
        cycle = np.sin(2 * np.pi * phase + phase0)
        breath = np.sin(4 * np.pi * phase + phase0 * 0.5)
        if key == "ForeArm":
            prefix = "Left" if sign == 1 else "Right"
            u = world_point(prefix + "ForeArm") - world_point(prefix + "Arm")
            v = world_point(prefix + "Hand") - world_point(prefix + "ForeArm")
            u /= np.linalg.norm(u)
            v /= np.linalg.norm(v)
            axis = np.cross(v, axes[:, 2])
            bend = np.rad2deg(np.arccos(np.clip(u @ v, -1, 1)))
            amount = (
                np.clip((32 - bend) * 0.47, 2, 13) + sign * 2.8 * cycle + 1.6 * breath
            )
        elif key == "Shoulder":
            axis = axes[:, 2]
            amount = sign * (0.85 * breath + 0.65 * cycle)
        elif key == "Spine1":
            axis = axes[:, 1]
            amount = 2.1 * cycle
        elif key == "Spine2":
            axis = axes[:, 0]
            amount = 1.4 * breath + 0.4 * cycle
        elif key == "Neck":
            axis = axes[:, 1]
            amount = -1.25 * cycle + 0.5 * breath
        else:
            return q
        if np.linalg.norm(axis) < 1e-6:
            return q
        axis = axis / np.linalg.norm(axis)
        rotation = (r.matrix_world @ bone.matrix).to_quaternion().inverted()
        local = np.asarray(rotation @ Vector(axis))
        return [
            v @ Quaternion(Vector(local), math.radians(float(a)))
            for v, a in zip(q, amount)
        ]

    # Bind poses from the scans can have palms facing down, forward or back.
    # Resolve the inside of each relaxed hand in the recorded standing pose.
    palmar_normals = {}
    for prefix in ("Left", "Right"):
        wrist = prefix + "Hand"
        if not all(wrist + key in ind for key in ("Middle1", "Index1", "Pinky1")):
            continue
        along = P[ind[wrist + "Middle1"]] - P[ind[wrist]]
        across = P[ind[wrist + "Index1"]] - P[ind[wrist + "Pinky1"]]
        normal = np.cross(along, across)
        normal /= max(np.linalg.norm(normal), 1e-8)
        hand = r.pose.bones[nm[wrist]]
        posed = (r.matrix_world @ hand.matrix).to_quaternion()
        bind = (r.matrix_world @ hand.bone.matrix_local).to_quaternion()
        facing = np.asarray(posed @ bind.inverted() @ Vector(normal))
        toward_thigh = world_point("Hips") - world_point(wrist)
        toward_thigh -= axes[:, 1] * (toward_thigh @ axes[:, 1])
        if facing @ toward_thigh < 0:
            normal = -normal
        palmar_normals[wrist] = normal

    for bone in r.pose.bones:
        name = bone.name.split(":")[-1]
        path = bone.path_from_id("rotation_quaternion")
        q = sample_curves(path, [1.0, 0, 0, 0])
        q = [Quaternion(tuple(row)).normalized() for row in q]
        if "Hand" in name and name in ind:
            jn = ind[name]
            ref = (r.matrix_world @ bone.bone.matrix_local).to_quaternion()
            # Relax the grip in standing only. Individual fingers stay curved; index and pinky do not close together.
            seg = name[-1]
            if seg in "123" and not name.endswith("Hand"):
                wrist = name.split("Hand")[0] + "Hand"
                fingers = ("Thumb", "Index", "Middle", "Ring", "Pinky")
                digit = next((f for f in fingers if wrist + f + seg == name), None)
                if digit is None:
                    continue
                index = fingers.index(digit)
                joint = int(seg) - 1
                ray = np.asarray(r.matrix_world @ bone.bone.tail_local) - P[jn]
                ray /= max(np.linalg.norm(ray), 1e-8)
                palm = palmar_normals[wrist]
                inward = P[ind[wrist + "Middle1"]] - P[ind[wrist + "Thumb1"]]
                inward /= np.linalg.norm(inward)
                toward = 0.70 * inward + 0.52 * palm if digit == "Thumb" else palm
                closing_axis = np.cross(ray, toward)
                closing_axis /= max(1e-8, np.linalg.norm(closing_axis))
                local_axis = ref.inverted() @ Vector(closing_axis)
                neutral = [
                    (10, 11, 8),
                    (13, 15, 8),
                    (17, 19, 12),
                    (21, 24, 15),
                    (25, 27, 17),
                ][index][joint]
                amplitude = [(5, 4, 3), (7, 7, 4), (7, 6, 4), (8, 7, 5), (8, 7, 5)][
                    index
                ][joint]
                # Keep the original recorded joint motion and add a resting cascade:
                # the trailing fingers curl more and open later than the index.
                sign = 1 if name.startswith("Left") else -1
                ph = phase0 * 0.45 + sign * 1.25 + index * 0.39 + joint * 0.15
                roll = np.sin(2 * np.pi * phase + ph) + 0.24 * np.sin(
                    4 * np.pi * phase + ph
                )
                q = [
                    Quaternion(
                        local_axis, math.radians(neutral + amplitude * float(z))
                    ).slerp(src, 0.18)
                    for z, src in zip(roll, q)
                ]
            elif name.endswith("Hand"):
                q = [Quaternion().slerp(v, 0.75) for v in q]
        gap = q[-1].rotation_difference(q[0])
        q = [v @ Quaternion().slerp(gap, float(weight)) for v, weight in zip(q, blend)]
        q = secondary(q, bone, name)
        ar = np.array(q)
        sg = np.r_[1, np.cumprod(np.where(np.sum(ar[1:] * ar[:-1], axis=1) < 0, -1, 1))]
        add_curves(new, path, tt, ar * sg[:, None])
        lp = bone.path_from_id("location")
        if any((lp, i) in cache for i in range(3)):
            v = sample_curves(lp, [0.0, 0.0, 0.0])
            v += blend[:, None] * (v[0] - v[-1])
            add_curves(new, lp, tt, v)
    r.animation_data.action = new
    bpy.data.actions.remove(original)
    new.name = "Idle"
    acts["Idle"] = new

    from osiedle_odstep import relax_arms, avoid_clothing

    relax_arms(s, r, ms[0], nm, axes, h, new, animate_torso=True)
    avoid_clothing(s, r, ms[0], nm, axes, new)
    if actor == "patryk":
        # SPOCZYNEK WYJETY Z PLANTOWANIA PODESZW (16.11, 20:34 — wlasciciel: „Patryk ma zjebana
        # poze spoczynkowa”). To jedyny krok w calym potoku stosowany WYLACZNIE do Patryka, i to
        # wlasnie on wyglada zle, podczas gdy reszta obsady stoi normalnie. Zmierzone w APK:
        # wszystkie siatki maja dolna krawedz dokladnie w Y=0, wiec w pozie spoczynkowej stoja na
        # podlodze — unosi je dopiero nagranie. Chod i mowa zostaja, bo tam plantowanie pomaga
        # (stopa nie slizga sie po ziemi), a stanie w miejscu go nie potrzebuje.
        for planted in (acts["Walk"], acts["Talk"]):
            align_planted_soles(
                s, r, r.pose.bones[nm["Hips"]], ms[0], axes[:, 1], planted
            )
        r.animation_data.action = new
    from osiedle_twarz import add_face

    if len(ms) != 1:
        raise ValueError(actor + " requires one authored skin mesh")
    has_correctives = bool(ms[0].data.shape_keys)
    add_face(
        actor,
        ms[0],
        axes,
        P[ind["Head"]],
        source.parent / "Color.png",
        output.parent / "Mimika.png",
    )
    s.frame_set(1)
    for a in acts.values():
        a.use_fake_user = True
    bpy.ops.object.select_all(action="DESELECT")
    for ob in [r] + ms:
        ob.select_set(True)
    bpy.context.view_layer.objects.active = r
    bpy.ops.export_scene.fbx(
        filepath=str(output),
        use_selection=True,
        object_types={"ARMATURE", "MESH"},
        use_mesh_modifiers=False,
        add_leaf_bones=False,
        bake_anim_use_nla_strips=False,
        bake_anim_use_all_actions=True,
        bake_anim_simplify_factor=0.02,
        path_mode="STRIP",
        embed_textures=False,
    )
    if has_correctives:
        import osiedle_morf

        osiedle_morf.restore(source, output)
    print(
        "BRAXHORN_POSTAC",
        actor,
        "klipy",
        len(acts),
        "wierzcholki",
        len(ms[0].data.vertices),
        "klatki_idle",
        n,
    )


if __name__ == "__main__":
    sys.path.insert(0, str(Path(__file__).resolve().parent))
    source, output = [Path(v).resolve() for v in sys.argv[sys.argv.index("--") + 1 :]]
    prepare(source, output)
