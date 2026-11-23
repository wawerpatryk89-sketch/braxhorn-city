"""Zblizenia dloni: kazda dlon z przodu i z boku, w podanych klatkach ujecia.
   blender -b -P rk_dlonie.py -- <plik.fbx> <kat_wyj> <Color.png> <ujecie> <klatka,klatka,...>"""
import sys, math
from pathlib import Path
import bpy
from mathutils import Vector
plik, wyj, kolor, ujecie, klatki = sys.argv[sys.argv.index("--") + 1:][:5]
wyj = Path(wyj); wyj.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=plik, automatic_bone_orientation=False, ignore_leaf_bones=False)
s = bpy.context.scene
rig = [o for o in s.objects if o.type == "ARMATURE"][0]
siatki = [o for o in s.objects if o.type == "MESH"]
for t in rig.animation_data.nla_tracks: t.mute = True
mat = bpy.data.materials.new("p"); mat.use_nodes = True
b = mat.node_tree.nodes["Principled BSDF"]; b.inputs["Roughness"].default_value = 0.8
tex = mat.node_tree.nodes.new("ShaderNodeTexImage"); tex.image = bpy.data.images.load(kolor)
mat.node_tree.links.new(tex.outputs["Color"], b.inputs["Base Color"])
for m in siatki: m.data.materials.clear(); m.data.materials.append(mat)
pts = [m.matrix_world @ v.co for m in siatki for v in list(m.data.vertices)[::9]]
H = max(p.z for p in pts) - min(p.z for p in pts)
a = [x for x in bpy.data.actions if x.name.rsplit("|", 1)[-1] == ujecie and any("pose.bones" in c.data_path for c in x.fcurves)][0]
rig.animation_data.action = a
kb = lambda n: next(p for p in rig.pose.bones if p.name.split(":")[-1] == n)
cd = bpy.data.cameras.new("k"); cd.type = "ORTHO"; cd.ortho_scale = H * 0.2
cam = bpy.data.objects.new("k", cd); s.collection.objects.link(cam); s.camera = cam
sl = bpy.data.lights.new("s", "SUN"); sl.energy = 3.0
so = bpy.data.objects.new("s", sl); s.collection.objects.link(so); so.rotation_euler = (math.radians(40), 0, math.radians(-25))
s.world = bpy.data.worlds.new("w"); s.world.use_nodes = True
s.world.node_tree.nodes["Background"].inputs[0].default_value = (0.55, 0.6, 0.66, 1)
s.render.engine = "CYCLES"; s.cycles.device = "CPU"; s.cycles.samples = 12; s.cycles.use_denoising = False
s.render.resolution_x = s.render.resolution_y = 300
s.render.image_settings.file_format = "JPEG"
for f in [int(x) for x in klatki.split(",")]:
    s.frame_set(f)
    hips = rig.matrix_world @ kb("Hips").head
    for st in ("Left", "Right"):
        h = rig.matrix_world @ kb(st + "Hand").head
        t = rig.matrix_world @ kb(st + "HandMiddle2").head
        c = (h + t) / 2
        boczny = c - hips; boczny.z = 0; boczny.normalize()
        for nazwa, kier in (("przod", Vector((0, -1, 0.15))), ("bok", boczny + Vector((0, 0, 0.15)))):
            kier = kier.normalized()
            cam.location = c + kier * H
            cam.rotation_euler = (c - cam.location).to_track_quat("-Z", "Y").to_euler()
            s.render.filepath = str(wyj / ("%s_%04d_%s_%s.jpg" % (ujecie, f, st, nazwa)))
            bpy.ops.render.render(write_still=True)
