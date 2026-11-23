"""Pomiar wad ruchu na postaci — bez patrzenia.

    blender -b -P rk_mierz.py -- <postac.fbx> <wynik.json> [co_ile_klatek=3]

Dla kazdego ujecia, klatka po klatce, na ODKSZTALCONEJ siatce (tak, jak ja widac w grze):
  1. PRZENIKANIE: wierzcholki przedramion i dloni, ktore weszly pod powierzchnie tulowia,
     glowy albo ud. Glebokosc w cm.
  2. DEFORMACJA DLONI: krawedzie siatki dloni i palcow rozciagniete albo zgniecione
     wzgledem spoczynku (>1,45x lub <0,6x). Tak wyglada „pogniecona reka”.
  3. SKRET NADGARSTKA: obrot dloni wokol osi przedramienia wzgledem spoczynku (stopnie).
     Powyzej ~100° skora nadgarstka skreca sie jak papierek od cukierka.
"""
import json
import math
import sys
from collections import defaultdict
from pathlib import Path

import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree

argv = sys.argv[sys.argv.index("--") + 1:]
plik, wynik = argv[0], Path(argv[1])
krok = int(argv[2]) if len(argv) > 2 else 3
tylko = set(argv[3].split(",")) if len(argv) > 3 and argv[3] else None

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=plik, automatic_bone_orientation=False, ignore_leaf_bones=False)
s = bpy.context.scene
rig = [o for o in s.objects if o.type == "ARMATURE"][0]
siatki = [o for o in s.objects if o.type == "MESH"]
for t in (rig.animation_data.nla_tracks if rig.animation_data else []):
    t.mute = True


def krotko(n):
    return n.split(":")[-1]


RDZEN = {"Hips", "Spine", "Spine1", "Spine2", "Neck", "Head", "LeftUpLeg", "RightUpLeg",
         "LeftShoulder", "RightShoulder"}


def strona_reki(n):
    for st in ("Left", "Right"):
        if n.startswith(st + "ForeArm") or n.startswith(st + "Hand"):
            return st
    return None


def dlon(n):
    return (n.startswith("LeftHand") or n.startswith("RightHand"))


# --- klasyfikacja wierzcholkow po dominujacej kosci (z wag skory) ------------
dane = []
for m in siatki:
    grupy = {g.index: krotko(g.name) for g in m.vertex_groups}
    klasa = []
    for v in m.data.vertices:
        best, w = None, 0.0
        for g in v.groups:
            if g.weight > w and g.group in grupy:
                best, w = grupy[g.group], g.weight
        klasa.append(best or "")
    rdzen_twarze = [p.index for p in m.data.polygons if all(klasa[i] in RDZEN for i in p.vertices)]
    reka = [i for i, k in enumerate(klasa) if strona_reki(k)]
    dlon_kraw = [e.vertices[:] for e in m.data.edges if dlon(klasa[e.vertices[0]]) and dlon(klasa[e.vertices[1]])]
    dane.append((m, klasa, rdzen_twarze, reka, dlon_kraw))

# wysokosc postaci w spoczynku (do progow w procentach wzrostu)
pts = [m.matrix_world @ v.co for m in siatki for v in list(m.data.vertices)[::9]]
H = max(p.z for p in pts) - min(p.z for p in pts)
PROG = 0.012 * H            # ~2 cm przy 1,8 m: mniejsze „wejscie” to styk skory, nie przenikanie

# dlugosci krawedzi dloni w spoczynku (bez animacji)
if rig.animation_data:
    rig.animation_data.action = None
for pb in rig.pose.bones:
    pb.location = (0, 0, 0); pb.rotation_quaternion = (1, 0, 0, 0); pb.rotation_euler = (0, 0, 0); pb.scale = (1, 1, 1)
bpy.context.view_layer.update()
dg = bpy.context.evaluated_depsgraph_get()
spocz_kraw = []
for (m, klasa, rt, reka, dk) in dane:
    e = m.evaluated_get(dg); me = e.to_mesh()
    co = [m.matrix_world @ v.co for v in me.vertices]
    spocz_kraw.append([max(1e-6, (co[a] - co[b]).length) for a, b in dk])
    e.to_mesh_clear()


def os_przedr(st):
    fa = rig.pose.bones.get(next(b.name for b in rig.pose.bones if krotko(b.name) == st + "ForeArm"))
    return fa


def skret_nadgarstka(st):
    """Kat obrotu dloni wokol osi przedramienia, wzgledem spoczynku."""
    fa = next(b for b in rig.pose.bones if krotko(b.name) == st + "ForeArm")
    ha = next(b for b in rig.pose.bones if krotko(b.name) == st + "Hand")
    # obrot dloni wzgledem przedramienia: poza vs spoczynek
    rel_p = (fa.matrix.to_quaternion().inverted() @ ha.matrix.to_quaternion())
    rel_r = (fa.bone.matrix_local.to_quaternion().inverted() @ ha.bone.matrix_local.to_quaternion())
    d = rel_r.inverted() @ rel_p
    os_l = Vector((0, 1, 0))   # os Y kosci przedramienia = jej dlugosc (konwencja Blendera)
    os_l = (fa.bone.matrix_local.to_quaternion().inverted() @ ha.bone.matrix_local.to_quaternion()) @ os_l
    # skret = skladowa obrotu wokol osi kosci (rozklad swing-twist)
    q = d
    p = Vector((q.x, q.y, q.z))
    proj = os_l * p.dot(os_l)
    tw_w, tw_v = q.w, proj
    n = math.sqrt(tw_w * tw_w + tw_v.length_squared)
    if n < 1e-9:
        return 0.0
    kat = 2 * math.acos(min(1.0, abs(tw_w) / n))
    return math.degrees(kat)


def wewnatrz(bvh, p, nrm):
    """Potwierdzenie promieniem: dalej w glab musi byc druga scianka trafiona od srodka.
    Inaczej dziury w platku tulowia (pachy, lopatki) daja falszywe przenikanie."""
    kier = -nrm
    hit = bvh.ray_cast(p + kier * (0.002 * H), kier, 0.6 * H)
    return hit[0] is not None and hit[1].dot(kier) > 0


raport = {"postac": Path(plik).stem, "wzrost_m": round(H, 3), "prog_cm": round(PROG * 100, 1), "ujecia": {}}
akcje = [a for a in bpy.data.actions if any("pose.bones" in c.data_path for c in a.fcurves)]
for a in akcje:
    nazwa = a.name.rsplit("|", 1)[-1]
    if tylko and nazwa not in tylko:
        continue
    rig.animation_data.action = a
    f0, f1 = (int(round(x)) for x in a.frame_range)
    klatki = list(range(f0, f1 + 1, krok))
    maks_gleb = 0.0; klatek_przen = 0; gdzie = defaultdict(int)
    maks_rozc = 1.0; min_sc = 1.0; klatek_def = 0; maks_skret = 0.0
    najgorsza = None
    for f in klatki:
        s.frame_set(f)
        dg = bpy.context.evaluated_depsgraph_get()
        gleb_f = 0.0; zle_kraw = 0
        for ix, (m, klasa, rt, reka, dk) in enumerate(dane):
            e = m.evaluated_get(dg); me = e.to_mesh()
            mw = m.matrix_world
            co = [mw @ v.co for v in me.vertices]
            if rt:
                polys = [list(me.polygons[i].vertices) for i in rt]
                bvh = BVHTree.FromPolygons(co, polys, all_triangles=False)
                for i in reka[::2]:
                    loc, nrm, idx, dist = bvh.find_nearest(co[i])
                    if loc is None:
                        continue
                    pod = -(co[i] - loc).dot(nrm)
                    if pod > PROG and dist < 0.15 * H and wewnatrz(bvh, co[i], nrm):
                        if pod > gleb_f:
                            gleb_f = pod
                        gdzie[strona_reki(klasa[i])] += 1
            sp = spocz_kraw[ix]
            for (a_, b_), l0 in zip(dk, sp):
                r = (co[a_] - co[b_]).length / l0
                if r > 1.45 or r < 0.6:
                    zle_kraw += 1
                if r > maks_rozc: maks_rozc = r
                if r < min_sc: min_sc = r
            e.to_mesh_clear()
        if gleb_f > 0:
            klatek_przen += 1
        if gleb_f > maks_gleb:
            maks_gleb = gleb_f; najgorsza = f
        if zle_kraw > 6:
            klatek_def += 1
        for st in ("Left", "Right"):
            sk = skret_nadgarstka(st)
            if sk > maks_skret:
                maks_skret = sk
    raport["ujecia"][nazwa] = {
        "klatek_badanych": len(klatki),
        "przenikanie_klatek_proc": round(100 * klatek_przen / len(klatki), 1),
        "przenikanie_max_cm": round(100 * maks_gleb, 1),
        "przenikanie_najgorsza_klatka": najgorsza,
        "przenikanie_reka": dict(gdzie),
        "dlon_zdeformowana_klatek_proc": round(100 * klatek_def / len(klatki), 1),
        "dlon_max_rozciagniecie": round(maks_rozc, 2),
        "dlon_max_zgniecenie": round(min_sc, 2),
        "skret_nadgarstka_max_st": round(maks_skret, 0),
    }
    u = raport["ujecia"][nazwa]
    print("BRAX_MIARA %-14s %-20s przenika %5.1f%% klatek, max %4.1f cm | dlon zdeform. %5.1f%% (x%.2f / x%.2f) | skret %3.0f°"
          % (raport["postac"], nazwa, u["przenikanie_klatek_proc"], u["przenikanie_max_cm"],
             u["dlon_zdeformowana_klatek_proc"], u["dlon_max_rozciagniecie"], u["dlon_max_zgniecenie"],
             u["skret_nadgarstka_max_st"]), flush=True)
wynik.write_text(json.dumps(raport, ensure_ascii=False, indent=1), encoding="utf-8")
