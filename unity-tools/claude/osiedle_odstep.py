"""Set the resting hand clearance from each character's actual clothed silhouette."""

import bpy
import numpy as np
from mathutils import Quaternion, Vector


def hand_clearance(mesh, axes):
    groups = {g.index: g.name.rsplit(":", 1)[-1] for g in mesh.vertex_groups}
    labels = []
    for vertex in mesh.data.vertices:
        strongest = max(vertex.groups, key=lambda g: g.weight, default=None)
        labels.append(groups[strongest.group] if strongest else "")
    torso = np.array(
        [
            bool(name)
            and not any(
                part in name
                for part in ("Hand", "Arm", "Shoulder", "Head", "Neck", "Foot", "Toe")
            )
            for name in labels
        ]
    )
    hands = [
        np.array([prefix + "Hand" in name for name in labels])
        for prefix in ("Left", "Right")
    ]

    def measure():
        obj = mesh.evaluated_get(bpy.context.evaluated_depsgraph_get())
        coords = np.empty(len(labels) * 3, dtype="f")
        obj.data.vertices.foreach_get("co", coords)
        world = np.asarray(obj.matrix_world)
        coords = (coords.reshape(-1, 3) @ world[:3, :3].T + world[:3, 3]) @ axes
        gaps = []
        for hand, sign in zip(hands, (1, -1)):
            fingers = coords[hand]
            low, high = np.percentile(fingers[:, 1], [5, 90])
            strip = coords[torso & (coords[:, 1] >= low) & (coords[:, 1] <= high)]
            if not len(strip):
                raise ValueError("No clothing at the resting hand height")
            gaps.append(
                np.percentile(fingers[:, 0] * sign, 2)
                - np.percentile(strip[:, 0] * sign, 99.5)
            )
        return np.array(gaps)

    return measure


def resting_turn(phase):
    # Ease the rib cage and shoulders between relaxed resting orientations.
    # Flat tangents make both the pauses and the loop join continuous.
    times = (0.0, 0.13, 0.34, 0.54, 0.74, 0.85, 1.0)
    levels = (-0.34, -0.44, 0.92, 0.87, -0.72, -0.70, -0.34)
    phase %= 1.0
    i = min(int(np.searchsorted(times, phase, side="right")) - 1, len(times) - 2)
    u = (phase - times[i]) / (times[i + 1] - times[i])
    return levels[i] + (levels[i + 1] - levels[i]) * u * u * (3.0 - 2.0 * u)


def relax_arms(scene, rig, mesh, bones, axes, height, action, animate_torso=False):
    from osiedle_ruch import add_curves

    measure = hand_clearance(mesh, axes)
    rig.animation_data.action = action
    side, up, front = (Vector(v) for v in axes.T)
    frames = np.arange(int(action.frame_range[0]), int(action.frame_range[1]) + 1)
    names = (
        "Hips",
        "Spine",
        "Neck",
        "Head",
        "LeftArm",
        "RightArm",
        "LeftForeArm",
        "RightForeArm",
        "LeftUpLeg",
        "RightUpLeg",
        "LeftLeg",
        "RightLeg",
        "LeftFoot",
        "RightFoot",
    )
    members = {n: rig.pose.bones[bones[n]] for n in names}
    tracks = {n: [] for n in names}
    for name in (
        "Spine1",
        "Spine2",
        "LeftShoulder",
        "RightShoulder",
        "LeftHand",
        "RightHand",
    ):
        members[name] = rig.pose.bones[bones[name]]
        tracks[name] = []
    positions = []
    hips = members["Hips"]
    head = members["Head"]
    rest_head = (rig.matrix_world @ head.bone.matrix_local).to_quaternion()
    phase_offset = sum(ord(c) for c in mesh.name) * 0.091

    def world_rotation(bone):
        return (rig.matrix_world @ bone.matrix).to_quaternion()

    def point(bone):
        return rig.matrix_world @ bone.head

    def world_turn(bone, rotation):
        basis = world_rotation(bone)
        bone.rotation_quaternion = (
            bone.rotation_quaternion @ basis.inverted() @ rotation @ basis
        )

    def turn(bone, axis, radians):
        world_turn(bone, Quaternion(axis, float(radians)))

    def update():
        bpy.context.view_layer.update()

    for frame in frames:
        scene.frame_set(int(frame))
        cycle = (frame - frames[0]) / (frames[-1] - frames[0]) * 2 * np.pi
        sway = np.sin(cycle + phase_offset)
        breath = np.sin(2 * cycle + 0.8)
        feet = {
            k: (point(members[k + "Foot"]), world_rotation(members[k + "Foot"]))
            for k in ("Left", "Right")
        }
        hip_basis = (
            rig.matrix_world @ hips.matrix
        ).to_3x3() @ hips.matrix_basis.to_3x3().inverted()
        shift = (
            side * float(0.009 * sway)
            + front * float(0.003 * np.sin(cycle - 0.4))
            - up * float(0.004 + 0.0015 * breath)
        ) * float(height)
        load = delayed = 0.0
        if animate_torso:
            phase = cycle / (2 * np.pi)
            load = resting_turn(phase)
            delayed = resting_turn(phase - 0.047)
        hips.location += hip_basis.inverted() @ shift
        turn(hips, front, np.deg2rad(0.95 * sway))
        update()
        turn(
            members["Spine"],
            front,
            np.deg2rad(3.4 * delayed if animate_torso else -1.65 * sway),
        )
        turn(members["Spine"], side, np.deg2rad(0.65 * breath))
        update()

        # A quiet shift of balance moves the chest and shoulders before the weight reaches its peak.
        turn(members["Spine1"], up, float(np.deg2rad(2.7 * np.sin(cycle + 0.42))))
        turn(members["Spine2"], side, float(np.deg2rad(1.2 * breath)))
        for sign, prefix in ((1, "Left"), (-1, "Right")):
            turn(
                members[prefix + "Shoulder"],
                front,
                float(
                    np.deg2rad(
                        sign * (1.9 * breath + 1.1 * np.sin(cycle + 0.15 * sign))
                    )
                ),
            )
        update()
        if animate_torso:
            # Distribute the turn of the upper body while keeping the head level.
            turn(members["Spine1"], up, np.deg2rad(-4.1 * delayed))
            turn(members["Spine1"], side, np.deg2rad(1.8 * breath - 1.2 * delayed))
            update()
            turn(members["Spine2"], front, np.deg2rad(-2.25 * load))
            for sign, prefix in ((1, "Left"), (-1, "Right")):
                turn(members[prefix + "Shoulder"], front, np.deg2rad(-1.7 * delayed))
                turn(members[prefix + "Shoulder"], up, np.deg2rad(sign * 1.8 * breath))
            update()
        # Solve each knee about its original bend plane so a load shift never drags a planted foot.
        for prefix in ("Left", "Right"):
            thigh, shin, foot = (members[prefix + n] for n in ("UpLeg", "Leg", "Foot"))
            ankle, foot_rotation = feet[prefix]
            a, k, f = point(thigh), point(shin), point(foot)
            direction = (ankle - a).normalized()
            upper, lower = (k - a).length, (f - k).length
            dist = min((ankle - a).length, upper + lower - 1e-7)
            along = (upper * upper - lower * lower + dist * dist) / (
                2 * max(dist, 1e-7)
            )
            pole = (k - a) - direction * (k - a).dot(direction)
            if pole.length < 1e-6:
                pole = front - direction * front.dot(direction)
            knee = (
                a
                + direction * along
                + pole.normalized()
                * float(np.sqrt(max(0.0, upper * upper - along * along)))
            )
            world_turn(thigh, (k - a).rotation_difference(knee - a))
            update()
            k, f = point(shin), point(foot)
            world_turn(shin, (f - k).rotation_difference(ankle - k))
            update()
            world_turn(foot, foot_rotation @ world_rotation(foot).inverted())
            update()

        neck = members["Neck"]
        aim = world_rotation(head) @ rest_head.inverted() @ front
        pitch = np.arcsin(np.clip(aim.dot(up), -1, 1))
        turn(neck, side, 0.55 * pitch)
        update()
        # Express head motion around its authored, upright T-pose gaze.
        yaw = np.deg2rad(
            2.3 * np.sin(cycle - 0.35) + 1.6 * np.sin(2 * cycle + phase_offset)
        )
        pitch = np.deg2rad(1.3 + 0.95 * breath)
        desired = (
            Quaternion(up, float(yaw))
            @ Quaternion(side, float(-pitch))
            @ Quaternion(front, float(np.deg2rad(0.9 * sway)))
            @ rest_head
        )
        world_turn(head, desired @ world_rotation(head).inverted())
        update()

        for sign, prefix in ((1, "Left"), (-1, "Right")):
            arm, elbow = members[prefix + "Arm"], members[prefix + "ForeArm"]
            ray = point(elbow) - point(arm)
            current = np.arctan2(ray.dot(side) * sign, -ray.dot(up))
            goal = np.deg2rad(
                13.2 + sign * 1.1 + 2.1 * np.sin(cycle + sign * 0.65) + 0.9 * breath
            )
            turn(arm, front, float(sign * (goal - current)))
            if animate_torso:
                turn(
                    arm,
                    side,
                    np.deg2rad(sign * 3.1 * (load - delayed) + sign * 2.1 * delayed),
                )
        update()
        gaps = measure() / height
        for i, (sign, prefix) in enumerate(((1, "Left"), (-1, "Right"))):
            elbow = members[prefix + "ForeArm"]
            wrist = rig.pose.bones[bones[prefix + "Hand"]]
            lever = max(0.10, (point(elbow) - point(wrist)).dot(up) / height)
            target = 0.020 + 0.002 * np.sin(cycle + sign * 0.8)
            adjust = float(np.clip((target - gaps[i]) / lever, -0.085, 0.14))
            turn(elbow, front, sign * adjust)
        update()
        for sign, prefix in ((1, "Left"), (-1, "Right")):
            elbow = members[prefix + "ForeArm"]
            wrist = members[prefix + "Hand"]
            # Delayed forearm supination, a gentle wrist arc and a soft change of elbow angle.
            fore_axis = (point(wrist) - point(elbow)).normalized()
            turn(
                elbow,
                fore_axis,
                float(np.deg2rad(sign * (4 + 4.5 * np.sin(cycle + 1.2 * sign)))),
            )
            turn(elbow, side, float(np.deg2rad(2.6 * np.sin(cycle - 0.55 * sign))))
            if animate_torso:
                turn(elbow, side, np.deg2rad(sign * 3.4 * delayed))
            update()
            turn(wrist, side, float(np.deg2rad(3.4 * np.sin(cycle + 0.3 * sign))))
            turn(
                wrist, front, float(np.deg2rad(sign * 2.8 * np.sin(cycle + 0.9 * sign)))
            )
        update()
        for name, bone in members.items():
            q = bone.rotation_quaternion.copy().normalized()
            if tracks[name] and np.dot(tracks[name][-1], q) < 0:
                q.negate()
            tracks[name].append(np.asarray(q).copy())
        positions.append(tuple(hips.location))

    def replace(bone, prop, values):
        path = bone.path_from_id(prop)
        for curve in list(action.fcurves):
            if curve.data_path == path:
                action.fcurves.remove(curve)
        add_curves(action, path, frames, np.asarray(values))

    for name, rotations in tracks.items():
        replace(members[name], "rotation_quaternion", rotations)
    replace(hips, "location", positions)


def avoid_clothing(scene, rig, mesh, bones, axes, action):
    """Keep the narrow arm silhouette while removing actual finger/clothing intersections."""
    from mathutils.bvhtree import BVHTree
    from osiedle_ruch import add_curves

    sides = ("Left", "Right")
    front = Vector(axes[:, 2])
    labels = {g.index: g.name.split(":")[-1] for g in mesh.vertex_groups}
    owners = []
    for v in mesh.data.vertices:
        g = max(v.groups, key=lambda item: item.weight, default=None)
        owners.append(labels[g.group] if g else "")
    mesh.data.calc_loop_triangles()
    triangles = np.array([t.vertices[:] for t in mesh.data.loop_triangles])

    def subset(keep):
        return triangles[np.all(np.array(keep)[triangles], axis=1)].tolist()

    cloth = subset(
        [
            bool(n)
            and not any(
                part in n
                for part in ("Hand", "Arm", "Shoulder", "Head", "Neck", "Foot", "Toe")
            )
            for n in owners
        ]
    )
    fingers = [subset([side + "Hand" in n for n in owners]) for side in sides]
    graph = bpy.context.evaluated_depsgraph_get()

    def collides():
        evaluated = mesh.evaluated_get(graph)
        xyz = np.empty(3 * len(owners), dtype="f")
        evaluated.data.vertices.foreach_get("co", xyz)
        verts = xyz.reshape(-1, 3).tolist()
        body = BVHTree.FromPolygons(verts, cloth, all_triangles=True)
        return [
            bool(body.overlap(BVHTree.FromPolygons(verts, hand, all_triangles=True)))
            for hand in fingers
        ]

    members = [
        [rig.pose.bones[bones[s + part]] for part in ("Arm", "ForeArm")] for s in sides
    ]

    def adjusted(i, bone, part, strength):
        sign = 1 if i == 0 else -1
        local = (rig.matrix_world @ bone.matrix).to_quaternion().inverted() @ front
        # Distribute the small offset across upper arm and forearm instead of flaring the shoulder.
        return bone.rotation_quaternion @ Quaternion(
            local, float(np.deg2rad(sign * (0.75 if part == 0 else 1.35) * strength))
        )

    needed = [0, 0]
    lo, hi = map(int, action.frame_range)
    for frame in np.linspace(lo, hi, 31):
        scene.frame_set(int(frame), subframe=float(frame % 1))
        hit = collides()
        for strength in range(1, 9):
            if not any(hit):
                break
            for i in range(2):
                if hit[i]:
                    needed[i] = max(needed[i], strength)
                    for part, bone in enumerate(members[i]):
                        bone.rotation_quaternion = adjusted(i, bone, part, 1)
            bpy.context.view_layer.update()
            hit = collides()
        if any(hit):
            raise ValueError(mesh.name + " unresolved hand/clothing collision")
    if not any(needed):
        return
    frames = np.arange(lo, hi + 1)
    sample = {}
    for frame in frames:
        scene.frame_set(int(frame))
        for i, strength in enumerate(needed):
            if not strength:
                continue
            for part, bone in enumerate(members[i]):
                path = bone.path_from_id("rotation_quaternion")
                rot = adjusted(i, bone, part, strength + 0.35)
                values = sample.setdefault(path, [])
                if values and np.dot(values[-1], rot) < 0:
                    rot.negate()
                values.append(tuple(rot))
    for path, values in sample.items():
        for c in list(action.fcurves):
            if c.data_path == path:
                action.fcurves.remove(c)
        add_curves(action, path, frames, np.asarray(values))
