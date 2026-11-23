import sys, bpy
from collections import defaultdict
argv = sys.argv[sys.argv.index("--") + 1:]
plik, ujecie = argv[0], argv[1]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=plik, automatic_bone_orientation=False, ignore_leaf_bones=False)
s = bpy.context.scene
rig = [o for o in s.objects if o.type == "ARMATURE"][0]
m = [o for o in s.objects if o.type == "MESH"][0]
k = lambda n: n.split(":")[-1]
grupy = {g.index: k(g.name) for g in m.vertex_groups}
klasa = []
for v in m.data.vertices:
    best, w = "", 0
    for g in v.groups:
        if g.weight > w and g.group in grupy: best, w = grupy[g.group], g.weight
    klasa.append(best)
kr = [e.vertices[:] for e in m.data.edges if klasa[e.vertices[0]].startswith(("LeftHand","RightHand")) and klasa[e.vertices[1]].startswith(("LeftHand","RightHand"))]
rig.animation_data.action = None
for pb in rig.pose.bones:
    pb.rotation_quaternion=(1,0,0,0); pb.location=(0,0,0)
s.frame_set(1); dg=bpy.context.evaluated_depsgraph_get()
e=m.evaluated_get(dg); me=e.to_mesh(); r0=[((me.vertices[a].co-me.vertices[b].co).length) for a,b in kr]; e.to_mesh_clear()
akc=[a for a in bpy.data.actions if a.name.endswith(ujecie)][0]
rig.animation_data.action=akc
naj=defaultdict(lambda: 9.0); mx=defaultdict(lambda: 0.0)
for f in range(1,int(akc.frame_range[1])+1,4):
    s.frame_set(f); dg=bpy.context.evaluated_depsgraph_get(); e=m.evaluated_get(dg); me=e.to_mesh()
    for (a,b),l0 in zip(kr,r0):
        r=(me.vertices[a].co-me.vertices[b].co).length/max(l0,1e-7)
        para=tuple(sorted((klasa[a],klasa[b])))
        if r<naj[para]: naj[para]=r
        if r>mx[para]: mx[para]=r
    e.to_mesh_clear()
for para,r in sorted(naj.items(), key=lambda x:x[1])[:14]:
    print("GDZIE %-40s min x%.2f" % ("/".join(para), r))

for para,r in sorted(mx.items(), key=lambda x:-x[1])[:10]:
    print("ROZC %-40s max x%.2f" % ("/".join(para), r))
