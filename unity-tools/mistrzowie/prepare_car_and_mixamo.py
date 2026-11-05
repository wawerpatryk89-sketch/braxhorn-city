#!/usr/bin/env python3
"""Import the SAME original estate supercar model and THREE owner Mixamo clips.

This is an offline pre-Unity source preparation step. It does not alter
the estate, recreate car meshes, change character rig/animations, start Unity,
or build an APK. Source ZIP/FBX bytes are preserved exactly.
"""
import argparse
import hashlib
import os
from pathlib import Path
import shutil
import uuid
import zipfile

ROOT = Path(__file__).resolve().parents[2]
SOURCE_CAR = ROOT / "public/assets/cars-src/car_rev.glb"
DEST = ROOT / "unity-city/Assets/Source/Mistrzowie/Vehicles"
DEST_ANIM = DEST / "Animations"
ZIP_SHA256 = "288eea539c78e2c0a46961cba02e837075ac020ea1a685a888c54ce1dcfe80ff"
CAR_GIT_BLOB = "78245d0968fbb91ee205af8cc5f88a981015933f"
ORIGINALS = {
    "Entering Car.fbx": "96f31bd107e2f1118ed2a4cd100865041303cd2286b074903c9f5c581ed3e8e3",
    "Driving.fbx": "74ccdfa8557ecf510be804b973b09606a2cc5a5b0048a4c0e5832a30451ed7e9",
    "Exiting Car.fbx": "efe2353cd8199cef2fef2ea28a5a4099360f01aba0209ec2a19f6d5fe5991ddf",
}
GUID_NS = uuid.UUID("13e78c05-d65f-4c1c-81db-1c7f186fa98a")


def digest(data):
    return hashlib.sha256(data).hexdigest()


def git_blob_sha(data):
    return hashlib.sha1(b"blob " + str(len(data)).encode() + b"\x00" + data).hexdigest()


def ensure_meta(target):
    relative = target.relative_to(ROOT / "unity-city").as_posix()
    wanted = uuid.uuid5(GUID_NS, relative).hex
    meta = Path(str(target) + ".meta")
    if meta.exists():
        import re
        match = re.search(r"^guid:\s*([0-9a-fA-F]{32})", meta.read_text(), re.M)
        if not match or match.group(1).lower() != wanted:
            raise ValueError("Do not change existing Unity asset GUID: " + str(meta))
    else:
        meta.parent.mkdir(parents=True, exist_ok=True)
        meta.write_text("fileFormatVersion: 2\nguid: " + wanted + "\n")


def verify_archive(archive):
    if not SOURCE_CAR.is_file():
        raise FileNotFoundError("Existing original estate model car_rev.glb was not checked out")
    car = SOURCE_CAR.read_bytes()
    if git_blob_sha(car) != CAR_GIT_BLOB or car[:4] != b"glTF":
        raise ValueError("Existing estate source car mesh changed: " + str(SOURCE_CAR))
    if not archive.is_file():
        raise FileNotFoundError("Owner's original 3 Mixamo animations archive missing: " + str(archive))
    if digest(archive.read_bytes()) != ZIP_SHA256:
        raise ValueError("Owner three-animation ZIP does not match the original SHA-256")
    with zipfile.ZipFile(archive) as z:
        if z.testzip() is not None or set(z.namelist()) != set(ORIGINALS):
            raise ValueError("Missing, corrupt or additional FBX in owner Mixamo archive")
        originals = {}
        for filename, expected in ORIGINALS.items():
            raw = z.read(filename)
            if digest(raw) != expected or not raw.startswith(b"Kaydara FBX Binary"):
                raise ValueError("Original Mixamo FBX changed: " + filename)
            if raw.count(b"mixamorig:") < 55 or b"Hips" not in raw:
                raise ValueError("Expected Mixamo bone hierarchy missing: " + filename)
            originals[filename] = raw
    return car, originals


def install(archive, dry_run=False):
    car, animation = verify_archive(archive)
    assets = {DEST / "car_rev.glb": car}
    assets.update({DEST_ANIM / filename: raw for filename, raw in animation.items()})
    for target, original in assets.items():
        if target.exists() and digest(target.read_bytes()) != digest(original):
            raise ValueError("Existing imported source differs - NO overwrite: " + str(target))
    if dry_run:
        print("CHAMPIONS_DRIVEABLE_CAR_SOURCE_PREFLIGHT_PASS model=estate-car_rev clips=3 unchanged APK_NOT_BUILT")
        return
    for folder in (DEST, DEST_ANIM):
        folder.mkdir(parents=True, exist_ok=True)
        ensure_meta(folder)
    for target, raw in assets.items():
        if not target.exists():
            temporary = target.with_suffix(target.suffix + ".partial")
            try:
                temporary.write_bytes(raw)
                os.replace(temporary, target)
            finally:
                temporary.unlink(missing_ok=True)
        ensure_meta(target)
    print("CHAMPIONS_DRIVEABLE_CAR_SOURCE_IMPORTED estate-car_rev=unchanged"
          " entering=original driving=original exiting=original GUIDS=STABLE")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("archive", type=Path, help="Owner's brax (2).zip as delivered")
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args()
    install(args.archive, args.dry_run)
