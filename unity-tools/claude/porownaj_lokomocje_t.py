"""Porównaj pozycję kończyn w Unity z tym samym momentem oryginalnego FBX.

blender -b -P unity-tools/claude/porownaj_lokomocje_t.py -- KontrolaPostaciT Pozycja_T_animacje_20261114.zip
"""
import csv
import hashlib
import json
import math
import sys
import tempfile
from pathlib import Path
from zipfile import ZipFile

import bpy
from mathutils import Vector


SEGMENTS = {
    "Leg": ("UpLeg", "Leg"),
    "Foot": ("Leg", "Foot"),
    "ToeBase": ("Foot", "ToeBase"),
}


def source_directions(path, phases):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(path))
    rig, = [item for item in bpy.context.scene.objects if item.type == "ARMATURE"]
    joints = {bone.name.rsplit(":", 1)[-1]: bone for bone in rig.pose.bones}
    start, end = rig.animation_data.action.frame_range
    samples = {}

    def point(name):
        return rig.matrix_world @ joints[name].head

    for index in range(phases):
        t = start + (end - start) * index / phases
        bpy.context.scene.frame_set(math.floor(t), subframe=t % 1)
        right = point("RightUpLeg") - point("LeftUpLeg")
        up = (point("Neck") - point("Hips")).normalized()
        # Blender and Unity have opposite handedness. Use anatomical axes in both.
        forward = up.cross(right).normalized()
        right = forward.cross(up).normalized()
        for side in ("Left", "Right"):
            for segment, (parent, child) in SEGMENTS.items():
                direction = (point(side + child) - point(side + parent)).normalized()
                samples[index, side, segment] = Vector(tuple(direction.dot(axis) for axis in (right, up, forward)))
    return samples


def quantile(values, fraction):
    values = sorted(values)
    k = (len(values) - 1) * fraction
    lo = math.floor(k)
    return values[lo] + (values[math.ceil(k)] - values[lo]) * (k - lo)


def compare(folder, archive):
    root = Path(__file__).resolve().parents[2]
    catalog = json.loads((root / "Postacie/ruchy/mixamo.json").read_text())
    groups = sorted(folder.glob("*_wektory.csv"))
    if not groups:
        raise ValueError("Brak pomiarów kończyn z Unity")
    cache = {}
    summary = []
    with ZipFile(archive) as z, tempfile.TemporaryDirectory() as temp:
        for file in groups:
            character, state, _ = file.stem.rsplit("_", 2)
            source = catalog["profiles"][character][state]
            clip = catalog["clips"][source]
            with file.open() as f:
                rows = list(csv.DictReader(f))
            phases = max(int(row["faza"]) for row in rows) + 1
            key = (source, phases)
            if key not in cache:
                data = z.read(clip["path"])
                if hashlib.sha256(data).hexdigest() != clip["sha256"]:
                    raise ValueError("Niepoprawna suma źródła " + clip["path"])
                path = Path(temp) / Path(clip["path"]).name
                path.write_bytes(data)
                cache[key] = source_directions(path, phases)
            original = cache[key]
            samples = {}
            for row in rows:
                sample = (int(row["faza"]), row["strona"], row["odcinek"])
                if sample in samples:
                    raise ValueError("Powtórzona próbka " + str(sample))
                v = Vector(tuple(float(row["postac_" + axis]) for axis in ("prawo", "gora", "przod")))
                samples[sample] = math.degrees(v.angle(original[sample]))
            if samples.keys() != original.keys():
                raise ValueError("Niepełny zestaw próbek " + file.name)
            for side in ("Left", "Right"):
                for name in SEGMENTS:
                    angles = [samples[i, side, name] for i in range(phases)]
                    summary.append((character, state, side, name, clip["name"], phases,
                                    round(quantile(angles, .5), 2), round(quantile(angles, .95), 2),
                                    round(max(angles), 2)))
    out = folder / "lokomocja_wzgledem_FBX.csv"
    with out.open("w", newline="") as f:
        writer = csv.writer(f)
        writer.writerow(("postac", "stan", "strona", "koniec_odcinka", "plik_zrodlowy",
                         "liczba_faz", "mediana_stopnie", "p95_stopnie", "max_stopnie"))
        writer.writerows(summary)
    print("BRAXHORN PORÓWNANIE FBX", out, len(summary))


if __name__ == "__main__":
    folder, archive = map(Path, sys.argv[sys.argv.index("--") + 1:])
    compare(folder, archive)
