"""Keep authored FBX blendshape curves during Blender's all-actions export."""

from collections import defaultdict
from io_scene_fbx import encode_bin as enc
from io_scene_fbx.parse_fbx import parse


def channels(path):
    root, ver = parse(str(path))
    objects = next(e for e in root.elems if e.id == b"Objects")
    links = next(e for e in root.elems if e.id == b"Connections")
    ob = {v.props[0]: v for v in objects.elems if v.props}
    parent = defaultdict(list)
    for l in links.elems:
        typ, child, par, *v = l.props
        parent[child].append((par, v))

    def ref(i, typ):
        return next(z for z, v in parent[i] if z in ob and ob[z].id == typ)

    curves = {}
    for i, e in ob.items():
        if e.id != b"AnimationCurve":
            continue
        node = ref(i, b"AnimationCurveNode")
        if not any(x in ob and ob[x].id == b"Deformer" for x, v in parent[node]):
            continue
        name = (
            ob[ref(ref(node, b"AnimationLayer"), b"AnimationStack")]
            .props[1]
            .split(b"\x00")[0]
            .decode()
            .split("|")[-1]
        )
        channel = ref(node, b"Deformer")
        channel_name = ob[channel].props[1].split(b"\x00")[0].decode()
        axis = next(tuple(v) for k, v in parent[i] if k == node)
        curves[(name, channel_name, axis)] = e
    return root, ver, curves


def restore(source, output):
    _, _, original = channels(source)
    root, version, baked = channels(output)
    if set(original) != {k for k in baked if k[1] not in ("BraxJaw","BraxRound")}:
        raise ValueError("Mismatched corrective blendshape channels")
    for action, curve in original.items():
        if action[0] != "Idle":
            baked[action].elems[:] = curve.elems
    methods = dict(
        B="bool",
        C="char",
        Z="int8",
        Y="int16",
        I="int32",
        L="int64",
        F="float32",
        D="float64",
        R="bytes",
        S="string",
        i="int32_array",
        l="int64_array",
        f="float32_array",
        d="float64_array",
        b="bool_array",
        c="byte_array",
    )

    def copy(e):
        v = enc.FBXElem(e.id)
        for typ, val in zip(e.props_type, e.props):
            getattr(v, "add_" + methods[chr(typ)])(val)
        v.elems.extend(copy(x) for x in e.elems)
        return v

    with enc.FBXElem.enable_multithreading_cm():
        tree = copy(root)
    temp = output.with_suffix(".morph.tmp")
    enc.write(str(temp), tree, version)
    temp.replace(output)
