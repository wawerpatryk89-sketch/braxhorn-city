#!/usr/bin/env python3
"""Copy exactly the original estate car_rev.glb into A1 before Unity.

No remodel, recolour, placeholder, other-branch edit or network download.
The source file is already tracked at public/assets/cars-src/car_rev.glb.
"""
from pathlib import Path
import hashlib
import json
import os
import struct
import uuid

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "public/assets/cars-src/car_rev.glb"
OUTPUT = ROOT / "unity-city/Assets/Source/Mistrzowie/Vehicles/car_rev.glb"
GIT_BLOB_SHA1 = "78245d0968fbb91ee205af8cc5f88a981015933f"
SOURCE_SIZE = 1545948
NS = uuid.UUID("93ae6172-879d-4ea9-9dda-11f20578c2d5")


def exact_git_blob(data):
    return hashlib.sha1(b"blob " + str(len(data)).encode("ascii") + b"\0" + data).hexdigest()


def stable_meta(target):
    rel = target.relative_to(ROOT / "unity-city").as_posix()
    guid = uuid.uuid5(NS, rel).hex
    return ("fileFormatVersion: 2\n"
            "guid: " + guid + "\n").encode("utf-8")


def prepare(dry_run=False):
    if not SOURCE.is_file():
        raise FileNotFoundError("Original public car_rev.glb absent; source checkout incomplete")
    data = SOURCE.read_bytes()
    if len(data) != SOURCE_SIZE or exact_git_blob(data) != GIT_BLOB_SHA1:
        raise ValueError("Original estate source car_rev.glb changed; refuse substitution")
    if len(data) < 20 or data[:4] != b"glTF" or struct.unpack_from("<I", data, 4)[0] != 2:
        raise ValueError("Owner's original estate car is not glTF v2 GLB")
    if struct.unpack_from("<I", data, 8)[0] != len(data):
        raise ValueError("Original estate GLB declared length mismatch")
    json_size, json_type = struct.unpack_from("<II", data, 12)
    if json_type != 0x4E4F534A or json_size < 16 or json_size + 20 > len(data):
        raise ValueError("Original estate GLB has no valid JSON chunk")
    doc = json.loads(data[20:20 + json_size])
    if len(doc.get("meshes", [])) < 1 or len(doc.get("nodes", [])) < 1:
        raise ValueError("Original estate car has no renderable geometry")
    planned = {}
    planned[OUTPUT] = data
    # Stable Unity import identity, including the parent folder. Never
    # overwrite an existing .meta or a different approved original source.
    for target in (OUTPUT.parent, OUTPUT):
        planned[Path(str(target) + ".meta")] = stable_meta(target)
    for target, raw in planned.items():
        if target.is_file() and target.read_bytes() != raw:
            raise ValueError("Existing Unity car/model/meta differs; approved bytes preserved: " + str(target))
    if not dry_run:
        for target, raw in planned.items():
            target.parent.mkdir(parents=True, exist_ok=True)
            if not target.is_file():
                temp = target.with_name(target.name + ".partial")
                try:
                    temp.write_bytes(raw)
                    os.replace(temp, target)
                finally:
                    temp.unlink(missing_ok=True)
    print("A1_ORIGINAL_ESTATE_CAR_" + ("PREFLIGHT" if dry_run else "IMPORT")
          + "_PASS original=car_rev.glb meshes=" + str(len(doc["meshes"]))
          + " nodes=" + str(len(doc["nodes"])) + " bytes=" + str(len(data))
          + " modelUnchanged=true")


if __name__ == "__main__":
    import argparse
    p = argparse.ArgumentParser()
    p.add_argument("--dry-run", action="store_true")
    args = p.parse_args()
    prepare(args.dry_run)
