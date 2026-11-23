"""Roznica obrotu wnetrza dloni (wokol osi przedramienia) miedzy nasza postacia a nagraniem, w spoczynku,
po wyrownaniu kierunku przedramienia. To o tyle dlon w ruchu jest obrocona „nie w ta strone”."""
import sys, math
from pathlib import Path
import bpy
sys.path.insert(0, str(Path(__file__).resolve().parent))
import rk_przenies as R
model, klip = sys.argv[sys.argv.index("--") + 1:][:2]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=model, automatic_bone_orientation=False, ignore_leaf_bones=False)
cel = [o for o in bpy.context.scene.objects if o.type == "ARMATURE"][0]
szk_c = R.Szkielet(cel)
zr, akcja, nowe, _ = R.wczytaj_zrodlo(Path(klip), set(bpy.context.scene.objects) - set(), set(bpy.data.actions.keys()))
szk_z = R.Szkielet(zr)
C = (szk_c.os @ szk_z.os.transposed()).to_quaternion()
for st in ("Left", "Right"):
    k, d = st + "ForeArm", st + "Hand"
    kc = (szk_c.glowy[d] - szk_c.glowy[k]).normalized()
    kz = C @ (szk_z.glowy[d] - szk_z.glowy[k]).normalized()
    Af = kc.rotation_difference(kz)
    nc = Af @ R.dlon_spoczynek(szk_c, st)[0]
    nz = C @ R.dlon_spoczynek(szk_z, st)[0]
    # skladowe prostopadle do osi przedramienia zrodla
    a = (nc - kz * nc.dot(kz)).normalized(); b = (nz - kz * nz.dot(kz)).normalized()
    kat = math.degrees(math.atan2(a.cross(b).dot(kz), a.dot(b)))
    print("BRAX_SKRET %s %s: wnetrze dloni obrocone o %.0f st. wokol przedramienia" % (Path(model).stem, st, kat))
