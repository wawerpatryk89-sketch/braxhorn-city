"""Prepare one animated character with the same Unity face controls as Patryk.

blender -b -P przygotuj_mimike.py -- character.fbx Color.png target/ --id character
For a new face first use --portrait head.png, then pass --profile landmarks.json.
Coordinates are pixels in the 256x256 portrait. The JSON holds two arrays:
[[[eyeX,eyeY,halfWidth,halfHeight,slope], ...], [lipX,lipY,halfWidth,cornerDrop,opening,existingEnamel]].
Use an empty eye list for opaque sunglasses. Output belongs in a separate directory.
The output folder holds the FBX plus Color, Normal, MetalSmooth and Mimika PNGs.
Supply Normal.png and MetalSmooth.png (or Metal.png + Rough.png) alongside Color.png.
For an unanimated Mixamo rig, also specify --motion-plan, --motion-cache and
--retarget-tool (unity-tools/postacie-retarget/brax_animacje.py from the project's
codex/postacie-retarget-20261115 branch). --id must match the character in the plan.
Existing motion curves, skin weights, original UVs and authored correctives are retained.
"""
import argparse
import shutil
import subprocess
import array
import sys
import json
import tempfile
from pathlib import Path
import bpy
import numpy as np
from io_scene_fbx.parse_fbx import FBXElem, parse

sys.path.insert(0, str(Path(__file__).resolve().parent))
from osiedle_twarz import add_face, face_profile, head_projection, png
from osiedle_morf import write_tree


def child(parent, tag):
    return next(item for item in parent.elems if item.id == tag)


def element(tag, types="", *values, children=()):
    return FBXElem(tag.encode(), list(values), bytearray(types.encode()), list(children))


def values(kind, data):
    return array.array(kind, np.asarray(data).ravel())


def import_skin(source, selected_mesh=None):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(source), use_anim=False, use_image_search=False,
                             automatic_bone_orientation=False, ignore_leaf_bones=False)
    candidates = []
    for obj in bpy.context.scene.objects:
        if obj.type != "MESH" or (selected_mesh and obj.name != selected_mesh):
            continue
        rig = obj.find_armature()
        if rig and any(g.name.split(":")[-1] == "Head" for g in obj.vertex_groups):
            candidates.append((obj, rig))
    if len(candidates) != 1:
        raise ValueError(f"Expected one head skin. Use --mesh to select among {[m.name for m, _ in candidates]}")
    mesh, rig = candidates[0]
    bones = {b.name.split(":")[-1]: np.array(rig.matrix_world @ b.head_local) for b in rig.data.bones}
    up = bones["Neck"] - bones["Hips"]
    up /= np.linalg.norm(up)
    side = bones["LeftArm"] - bones["RightArm"]
    side -= up * (side @ up)
    side /= np.linalg.norm(side)
    return mesh, np.column_stack((side, up, np.cross(side, up))), bones["Head"]


def geometry(objects, mesh):
    co = np.array([v.co[:] for v in mesh.vertices])
    loops = np.array([v.vertex_index for v in mesh.loops])
    for item in objects.elems:
        if item.id != b"Geometry" or item.props[-1] != b"Mesh":
            continue
        points = np.asarray(child(item, b"Vertices").props[0]).reshape(-1, 3)
        if points.shape != co.shape or not np.allclose(points, co, atol=1e-6, rtol=1e-6):
            continue
        indices = np.asarray(child(item, b"PolygonVertexIndex").props[0])
        if np.array_equal(np.where(indices < 0, -indices-1, indices), loops):
            return item, co
    raise ValueError("FBX vertex or corner order differs from the imported head mesh")


def replace_uv(geo, mesh):
    uv = np.empty(2*len(mesh.loops), np.float32)
    mesh.uv_layers["Mimika"].data.foreach_get("uv", uv)
    coords, indices = np.unique(uv.reshape(-1, 2), axis=0, return_inverse=True)
    existing = next((c for c in geo.elems if c.id == b"LayerElementUV" and c.props == [1]), None)
    if existing and child(existing, b"Name").props != [b"Mimika"]:
        raise ValueError("UV2 already contains another map")
    layer = element("LayerElementUV","I",1, children=[
        element("Version","I",101),element("Name","S",b"Mimika"),
        element("MappingInformationType","S",b"ByPolygonVertex"),
        element("ReferenceInformationType","S",b"IndexToDirect"),
        element("UV","d",values("d",coords)),element("UVIndex","i",values("i",indices))])
    if existing:
        geo.elems[geo.elems.index(existing)] = layer
    else:
        geo.elems.append(layer)
        slot = next((c for c in geo.elems if c.id == b"Layer" and c.props == [1]), None)
        if slot is None:
            slot = element("Layer","I",1, children=[element("Version","I",100)])
            geo.elems.append(slot)
        slot.elems.append(element("LayerElement", children=[
            element("Type","S",b"LayerElementUV"),element("TypedIndex","I",1)]))


def bind_shapes(root, objects, geo, mesh, co):
    links = child(root, b"Connections")
    ids = {o.props[0]:o for o in objects.elems if o.props}
    next_id = max(ids)+1
    defs = child(root, b"Definitions")

    def new_object(tag, name, subtype, content, parent):
        nonlocal next_id
        new = element(tag,"LSS",next_id,name,subtype,children=content)
        next_id += 1
        objects.elems.append(new)
        links.elems.append(element("C","SLL",b"OO",new.props[0],parent))
        entry = next(e for e in defs.elems if e.id == b"ObjectType" and e.props == [tag.encode()])
        child(entry,b"Count").props[0] += 1
        child(defs,b"Count").props[0] += 1
        return new

    def linked(target, typ):
        return [ids[c.props[1]] for c in links.elems if len(c.props)>=3 and c.props[2]==target
                and c.props[1] in ids and ids[c.props[1]].props[-1]==typ]

    groups = linked(geo.props[0],b"BlendShape")
    group = groups[0] if groups else new_object("Deformer",b"BraxMimika\x00\x01Deformer",b"BlendShape",
                                               [element("Version","I",100)],geo.props[0])
    channels = {c.props[1].split(b"\x00")[0]:c for g in groups for c in linked(g.props[0],b"BlendShapeChannel")}
    for name in ("BraxJaw","BraxRound"):
        key = mesh.shape_keys.key_blocks[name]
        points = np.empty(co.size,np.float32)
        key.data.foreach_get("co",points)
        delta = points.reshape(-1,3)-co
        changed = np.flatnonzero(np.any(delta != 0,axis=1))
        if not len(changed):
            raise ValueError("No displacement in "+name)
        encoded = name.encode()
        data = [element("Version","I",100),element("Indexes","i",values("i",changed)),
                element("Vertices","d",values("d",delta[changed]))]
        if encoded in channels:
            channel = channels[encoded]
            shape, = linked(channel.props[0],b"Shape")
            previous = child(shape,b"Indexes").props[0]
            if np.array_equal(previous,changed) and np.allclose(child(shape,b"Vertices").props[0],delta[changed].ravel(),
                                                                  rtol=1e-5,atol=1e-8):
                continue
            shape.elems[:] = data
            child(channel,b"FullWeights").props[0]=values("d",np.full(len(changed),100.))
        else:
            channel = new_object("Deformer",encoded+b"\x00\x01SubDeformer",b"BlendShapeChannel",
                                 [element("Version","I",100),element("DeformPercent","D",0.),
                                  element("FullWeights","d",values("d",np.full(len(changed),100.)))],group.props[0])
            new_object("Geometry",encoded+b"\x00\x01Geometry",b"Shape",data,channel.props[0])


def copy_maps(color, folder):
    shutil.copyfile(color, folder / "Color.png")
    shutil.copyfile(color.with_name("Normal.png"), folder / "Normal.png")
    packed = color.with_name("MetalSmooth.png")
    if packed.is_file():
        shutil.copyfile(packed, folder / packed.name)
        return

    def red_channel(path):
        image = bpy.data.images.load(str(path), check_existing=False)
        try:
            image.colorspace_settings.name = "Non-Color"
            pixels = np.empty(len(image.pixels), np.float32)
            image.pixels.foreach_get(pixels)
            return np.uint8(np.clip(pixels[::4].reshape(image.size[1], image.size[0])[::-1] * 255 + .5, 0, 255))
        finally:
            bpy.data.images.remove(image)

    metal = red_channel(color.with_name("Metal.png"))
    rough = red_channel(color.with_name("Rough.png"))
    if metal.shape != rough.shape:
        raise ValueError("Metal.png and Rough.png have different dimensions")
    png(folder / "MetalSmooth.png", np.stack((metal, metal, metal, 255-rough), axis=2), clear_transparent=False)


def prepare(source, color, destination, actor, profile=None, portrait=None, mesh_name=None):
    obj, axes, head = import_skin(source, mesh_name)
    if portrait:
        front = head_projection(obj,axes,head,color)[-2]
        png(portrait,np.uint8(np.clip(np.dstack((front,np.ones(front.shape[:2])))*255+.5,0,255)))
        return
    if source.parent == destination or color.parent == destination:
        raise ValueError("Choose a separate output directory")
    pins = face_profile(actor,profile)
    destination.mkdir(exist_ok=True,parents=True)
    with tempfile.TemporaryDirectory(dir=destination) as tmp:
        tmp=Path(tmp)
        add_face(actor,obj,axes,head,color,tmp/"Mimika.png",pins)
        root, version = parse(str(source))
        objects=child(root,b"Objects")
        states = {item.props[1].split(b"\x00")[0].decode().rsplit("|", 1)[-1]
                  for item in objects.elems if item.id == b"AnimationStack"}
        missing = {"Idle", "Walk", "Point", "Talk", "Wave"} - states
        if missing:
            raise ValueError(f"Missing body clips {sorted(missing)}. Retarget the source rig first.")
        geo, co=geometry(objects,obj.data)
        replace_uv(geo,obj.data)
        bind_shapes(root,objects,geo,obj.data,co)
        write_tree(root,version,tmp/source.name)
        copy_maps(color, tmp)
        for ready in tmp.iterdir():
            ready.replace(destination / ready.name)


if __name__=="__main__":
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("fbx",type=Path)
    parser.add_argument("color",type=Path)
    parser.add_argument("output",type=Path)
    parser.add_argument("--id")
    parser.add_argument("--profile",type=Path)
    parser.add_argument("--portrait",type=Path)
    parser.add_argument("--mesh")
    parser.add_argument("--motion-plan", type=Path)
    parser.add_argument("--motion-cache", type=Path)
    parser.add_argument("--retarget-tool", type=Path)
    args=parser.parse_args(sys.argv[sys.argv.index("--")+1:])
    actor = args.id or args.fbx.stem
    source = args.fbx.resolve()
    profile = json.loads(args.profile.read_text(encoding="utf-8")) if args.profile else None
    motion = (args.motion_plan, args.motion_cache, args.retarget_tool)
    if any(motion) and not all(motion):
        parser.error("Use --motion-plan, --motion-cache and --retarget-tool together")
    if args.output.resolve() in {source.parent, args.color.resolve().parent} and not args.portrait:
        parser.error("Choose a separate output directory")
    with tempfile.TemporaryDirectory() as work:
        if all(motion) and not args.portrait:
            plan = json.loads(args.motion_plan.read_text(encoding="utf-8"))
            entry = plan["characters"][actor]
            if (args.motion_plan.parent / entry["model"]).resolve() != source:
                parser.error("The exact FBX must match the source in the motion plan")
            if not set(("Idle", "Walk", "Point", "Talk", "Wave")).issubset(
                    plan["profiles"][entry["profile"]] if isinstance(entry["profile"], str) else entry["profile"]):
                parser.error("The motion plan must include Idle, Walk, Point, Talk and Wave")
            subprocess.run([bpy.app.binary_path, "-b", "--factory-startup", "--disable-autoexec",
                            "--python-exit-code", "1", "-P", str(args.retarget_tool.resolve()), "--",
                            "--plan", str(args.motion_plan.resolve()), "--cache", str(args.motion_cache.resolve()),
                            "--mode", "batch", "--characters", actor, "--output", work, "--fbx"], check=True)
            source = Path(work) / actor / (actor + ".fbx")
        prepare(source, args.color.resolve(), args.output.resolve(), actor, profile, args.portrait, args.mesh)
        if all(motion) and not args.portrait:
            shutil.copyfile(source.parent / "audit.json", args.output / "audyt_ruchu.json")
