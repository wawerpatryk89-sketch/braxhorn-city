#!/usr/bin/env python3
"""Import owner's THREE unchanged Mixamo FBX clips before Unity 6.

Fatal only for corrupt/wrong source animations or a changed Unity GUID.
Does not run Unity, create a car, retarget bones, or modify original imagery.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import zipfile

ROOT = Path(__file__).resolve().parents[2]
BASE = "Assets/Source/Mistrzowie/VehicleAnimations/"
NAMES = ("Driving.fbx", "Entering Car.fbx", "Exiting Car.fbx")
LOCK = {
    "Driving.fbx": "74ccdfa8557ecf510be804b973b09606a2cc5a5b0048a4c0e5832a30451ed7e9",
    "Entering Car.fbx": "96f31bd107e2f1118ed2a4cd100865041303cd2286b074903c9f5c581ed3e8e3",
    "Exiting Car.fbx": "efe2353cd8199cef2fef2ea28a5a4099360f01aba0209ec2a19f6d5fe5991ddf",
}
ARCHIVE_SHA256 = "58957dd74e815b2ea81bd84e6174ca05abf57a3156364945215b50b34a16a101"


def sha(data):
    return hashlib.sha256(data).hexdigest()


def inspect(archive):
    if not archive.is_file():
        raise FileNotFoundError("Owner animation ZIP is missing: " + str(archive))
    with archive.open("rb") as handle:
        value = hashlib.sha256()
        for block in iter(lambda: handle.read(1024 * 1024), b""):
            value.update(block)
    if value.hexdigest() != ARCHIVE_SHA256:
        raise ValueError("Wrong owner's 3-Mixamo-FBX bundle, SHA-256 mismatch")
    with zipfile.ZipFile(archive) as z:
        if z.testzip() is not None:
            raise ValueError("Mixamo ZIP CRC error")
        expected = {BASE+n for n in NAMES} | {BASE+n+".meta" for n in NAMES} | {"MANIFEST.json"}
        if set(z.namelist()) != expected or len(z.namelist()) != len(expected):
            raise ValueError("Extra, missing or duplicated Mixamo ZIP members")
        manifest = json.loads(z.read("MANIFEST.json"))
        records = manifest.get("records", [])
        if manifest.get("animations") != 3 or {r.get("path") for r in records} != expected-{"MANIFEST.json"} or len(records) != 6:
            raise ValueError("Original animation manifest differs")
        result = {}
        for entry in records:
            name = entry["path"]
            data = z.read(name)
            if sha(data) != entry["sha256"] or len(data) != entry["bytes"]:
                raise ValueError("Owner clip changed: " + name)
            if name.endswith(".fbx"):
                if sha(data) != LOCK[Path(name).name]:
                    raise ValueError("Owner original FBX modified: " + name)
                if not data.startswith(b"Kaydara FBX Binary") or b"mixamorig:Hips" not in data:
                    raise ValueError("Owner FBX is not an original Mixamo skeleton: " + name)
            elif not re.search(rb"^guid:\s*[0-9a-f]{32}\s*$", data, re.M):
                raise ValueError("Unity stable animation GUID missing: " + name)
            result[name] = data
    return result


def install(files, dry_run):
    target = ROOT / "unity-city"
    missing = []
    for name in sorted(files):
        dest = target / name
        if not dest.resolve().is_relative_to(target.resolve()):
            raise ValueError("Unsafe owner FBX path")
        if dest.exists() and sha(dest.read_bytes()) != sha(files[name]):
            raise ValueError("Refusing to overwrite a different owner animation/meta: " + name)
        if not dest.exists():
            missing.append((dest, files[name]))
    if dry_run:
        print("BRAX_MIXAMO_3_OWNER_FBX_PREFLIGHT_PASS pending=" + str(len(missing)))
        return
    for dest, data in missing:
        dest.parent.mkdir(parents=True, exist_ok=True)
        tmp = dest.with_name(dest.name + ".tmp")
        try:
            tmp.write_bytes(data)
            os.replace(tmp, dest)
        finally:
            tmp.unlink(missing_ok=True)
    print("BRAX_MIXAMO_3_OWNER_FBX_IMPORTED originals=UNCHANGED clips=3 created=" + str(len(missing)))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("archive", type=Path)
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args()
    install(inspect(args.archive), args.dry_run)
