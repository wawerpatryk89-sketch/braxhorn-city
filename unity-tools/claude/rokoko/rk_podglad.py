"""Arkusz zdjec: kazda akcja postaci w kilku klatkach.
   blender -b -P rk_podglad.py -- <plik.blend|.fbx> <katalog_wyj> <kolor.png|-> [klatek=6] [akcje,po,przecinku]"""
import sys, math, os
from pathlib import Path
import bpy
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:]
plik, wyj, kolor = argv[0], Path(argv[1]), argv[2]
ile = int(argv[3]) if len(argv) > 3 else 6
tylko = argv[4].split(",") if len(argv) > 4 and argv[4] else None
wyj.mkdir(parents=True, exist_ok=True)
if plik.endswith(".blend"):
    bpy.ops.wm.open_mainfile(filepath=plik)
else:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=plik, automatic_bone_orientation=False, ignore_leaf_bones=False)
s = bpy.context.scene
rig = [o for o in s.objects if o.type == "ARMATURE"][0]
siatki = [o for o in s.objects if o.type == "MESH"]
for t in (rig.animation_data.nla_tracks if rig.animation_data else []):
    t.mute = True

mat = bpy.data.materials.new("podglad"); mat.use_nodes = True
bsdf = mat.node_tree.nodes["Principled BSDF"]
bsdf.inputs["Roughness"].default_value = 0.85
if kolor != "-":
    tex = mat.node_tree.nodes.new("ShaderNodeTexImage"); tex.image = bpy.data.images.load(kolor)
    mat.node_tree.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
else:
    bsdf.inputs["Base Color"].default_value = (0.62, 0.55, 0.5, 1)
for m in siatki:
    m.data.materials.clear(); m.data.materials.append(mat)

pts = [m.matrix_world @ v.co for m in siatki for v in list(m.data.vertices)[::7]]
lo = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
hi = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
H = hi.z - lo.z
srodek = Vector(((lo.x + hi.x) / 2, (lo.y + hi.y) / 2, lo.z + H * 0.52))

bpy.ops.mesh.primitive_plane_add(size=H * 12, location=(srodek.x, srodek.y, lo.z))
podl = bpy.context.active_object
mp = bpy.data.materials.new("podloga"); mp.use_nodes = True
mp.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.30, 0.31, 0.33, 1)
mp.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 0.9
podl.data.materials.append(mp)

cam_d = bpy.data.cameras.new("k"); cam_d.type = "ORTHO"; cam_d.ortho_scale = H * 1.95
cam = bpy.data.objects.new("k", cam_d); s.collection.objects.link(cam); s.camera = cam
kat = math.radians(float(os.environ.get("RK_KAT", "24")))
kier = Vector((math.sin(kat), -math.cos(kat), 0.22)).normalized()
cam.location = srodek + kier * H * 6
cam.rotation_euler = (srodek - cam.location).to_track_quat("-Z", "Y").to_euler()
sl = bpy.data.lights.new("s", "SUN"); sl.energy = 3.2; sl.angle = math.radians(6)
so = bpy.data.objects.new("s", sl); s.collection.objects.link(so)
so.rotation_euler = (math.radians(48), 0, math.radians(-32))
s.world = bpy.data.worlds.new("w"); s.world.use_nodes = True
bg = s.world.node_tree.nodes["Background"]; bg.inputs[0].default_value = (0.62, 0.66, 0.72, 1); bg.inputs[1].default_value = 0.9

s.render.engine = "CYCLES"; s.cycles.device = "CPU"; s.cycles.samples = 10
s.cycles.use_denoising = False; s.cycles.max_bounces = 2
import os
DUZE = os.environ.get("RK_DUZE") == "1"
s.render.resolution_x, s.render.resolution_y = (760, 960) if DUZE else (330, 420)
if DUZE:
    s.cycles.samples = 24
    cam_d.ortho_scale = H * 1.45
s.render.image_settings.file_format = "JPEG"; s.render.image_settings.quality = 88
s.render.threads_mode = "AUTO"

akcje = [a for a in bpy.data.actions if any("pose.bones" in c.data_path for c in a.fcurves)]
akcje.sort(key=lambda a: (a.name.rsplit("|", 1)[-1] not in ("Idle", "Walk"), a.name))
nr = 0
for a in akcje:
    nazwa = a.name.rsplit("|", 1)[-1]
    if tylko and nazwa not in tylko:
        continue
    rig.animation_data.action = a
    f0, f1 = a.frame_range
    for i in range(ile):
        f = f0 + (f1 - f0) * (i + 0.5) / ile
        s.frame_set(int(f))
        if os.environ.get("RK_SLEDZ", "1") == "1":
            hb = next(b for b in rig.pose.bones if b.name.split(":")[-1] == "Hips")
            hp = rig.matrix_world @ hb.head
            cel_k = Vector((hp.x, hp.y, srodek.z))
            cam.location = cel_k + kier * H * 6
        s.render.filepath = str(wyj / ("%02d_%s_%d.jpg" % (nr, nazwa, i)))
        bpy.ops.render.render(write_still=True)
    print("PODGLAD", nazwa, "klatki", int(f0), int(f1), flush=True)
    nr += 1
