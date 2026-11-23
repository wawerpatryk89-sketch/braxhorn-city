"""Ktore wierzcholki przenikaja w danej klatce: klasa, glebokosc, w co weszly."""
import bpy, sys
from collections import Counter
from mathutils.bvhtree import BVHTree
p, akcja, klatka = sys.argv[sys.argv.index("--") + 1:][:3]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=p, automatic_bone_orientation=False, ignore_leaf_bones=False)
s = bpy.context.scene
rig = [o for o in s.objects if o.type == "ARMATURE"][0]
m = [o for o in s.objects if o.type == "MESH"][0]
for t in rig.animation_data.nla_tracks: t.mute = True
a = [x for x in bpy.data.actions if x.name.rsplit("|", 1)[-1] == akcja and any("pose.bones" in c.data_path for c in x.fcurves)][0]
rig.animation_data.action = a
k = lambda n: n.split(":")[-1]
RDZEN = {"Hips", "Spine", "Spine1", "Spine2", "Neck", "Head", "LeftUpLeg", "RightUpLeg", "LeftShoulder", "RightShoulder"}
gr = {g.index: k(g.name) for g in m.vertex_groups}
kl = []
for v in m.data.vertices:
    b, w = "", 0
    for g in v.groups:
        if g.weight > w and g.group in gr: b, w = gr[g.group], g.weight
    kl.append(b)
pts = [m.matrix_world @ v.co for v in m.data.vertices]
H = max(p.z for p in pts) - min(p.z for p in pts)
s.frame_set(int(klatka))
dg = bpy.context.evaluated_depsgraph_get()
e = m.evaluated_get(dg); me = e.to_mesh()
co = [m.matrix_world @ v.co for v in me.vertices]
rt = [pl.index for pl in me.polygons if all(kl[i] in RDZEN for i in pl.vertices)]
polys = [list(me.polygons[i].vertices) for i in rt]
bvh = BVHTree.FromPolygons(co, polys, all_triangles=False)
wyn = []
for i, c in enumerate(kl):
    if not (c.startswith(("LeftForeArm", "RightForeArm", "LeftHand", "RightHand"))): continue
    loc, nrm, idx, dist = bvh.find_nearest(co[i])
    if loc is None: continue
    pod = -(co[i] - loc).dot(nrm)
    kier = -nrm
    hit = bvh.ray_cast(co[i] + kier * (0.002 * H), kier, 0.6 * H)
    sr = hit[0] is not None and hit[1].dot(kier) > 0
    if pod > 0.012 * H and dist < 0.15 * H and (sr or "--surowo" in sys.argv):
        cel = Counter(kl[j] for j in polys[idx]).most_common(1)[0][0]
        wyn.append((pod, c, cel, dist))
wyn.sort(reverse=True)
print("BRAX_GDZIE wierzcholkow", len(wyn), "H", round(H, 3))
print("BRAX_GDZIE pary", Counter((c, cel) for _, c, cel, _ in wyn).most_common(8))
for pod, c, cel, dist in wyn[:8]:
    print("BRAX_GDZIE %.1f cm  %s -> %s  (odl. %.1f cm)" % (pod * 100, c, cel, dist * 100))
