#!/usr/bin/env python3
"""Import exactly three owner-supplied Mixamo FBXs for Brax, without touching mesh/rig.

Called before Unity. Must use the archived ORIGINAL bytes and preserve stable GUIDs.
Neither a build launcher nor an animation synthesizer.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import zipfile

ROOT = Path(__file__).resolve().parents[2]
PROJECT = ROOT / "unity-city"
PREFIX = "Assets/Source/Mistrzowie/Brax/CarAnimations/"
SOURCE = "BRAXHORN_BRAX_3_ANIMACJE_MIXAMO_ORYGINALY.zip"
ARCHIVE_SHA256 = "509a6379fad404edec798e2999f3cdff37776c5399e92839a16ef05687f65bd3"
ORIGINAL = {
    "Driving.fbx": "74ccdfa8557ecf510be804b973b09606a2cc5a5b0048a4c0e5832a30451ed7e9",
    "Entering Car.fbx": "96f31bd107e2f1118ed2a4cd100865041303cd2286b074903c9f5c581ed3e8e3",
    "Exiting Car.fbx": "efe2353cd8199cef2fef2ea28a5a4099360f01aba0209ec2a19f6d5fe5991ddf",
}


def checksum(payload):
    return hashlib.sha256(payload).hexdigest()


def inspect(archive):
    if archive.name != SOURCE or not archive.is_file():
        raise ValueError("The expected three original Brax Mixamo animations ZIP is missing")
    h = hashlib.sha256()
    with archive.open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    if h.hexdigest() != ARCHIVE_SHA256:
        raise ValueError("Owner's three animation source bytes were changed")
    with zipfile.ZipFile(archive) as z:
        if z.testzip():
            raise ValueError("Corrupt Mixamo source ZIP (CRC failed)")
        members = z.infolist()
        expected = set(PREFIX + name for name in ORIGINAL)
        expected.update(path + ".meta" for path in list(expected))
        expected.add(PREFIX.rstrip("/") + ".meta")
        expected.add("MANIFEST.json")
        names = [i.filename for i in members]
        if len(names) != len(set(names)) or set(names) != expected:
            raise ValueError("Missing/extra/duplicate original FBX or GUID files")
        if any(i.is_dir() or (i.external_attr >> 16) & 0o170000 == 0o120000 for i in members):
            raise ValueError("ZIP entry is not a plain file")
        manifest = json.loads(z.read("MANIFEST.json"))
        if manifest.get("count") != 3 or len(manifest.get("files", [])) != len(expected)-1:
            raise ValueError("Original Mixamo manifest is incomplete")
        m = {i["path"]: i for i in manifest["files"]}
        if set(m) != expected - {"MANIFEST.json"}:
            raise ValueError("Unexpected owner animation manifest paths")
        installed = {}
        for name in expected - {"MANIFEST.json"}:
            raw = z.read(name)
            if len(raw) != m[name]["bytes"] or checksum(raw) != m[name]["sha256"]:
                raise ValueError("Owner source or stable GUID changed: " + name)
            if name.endswith(".fbx"):
                expected_sha = ORIGINAL[Path(name).name]
                if checksum(raw) != expected_sha or not raw.startswith(b"Kaydara FBX Binary  "):
                    raise ValueError("Incorrect Mixamo original " + name)
            if name.endswith(".meta") and b"guid: " not in raw:
                raise ValueError("Unity stable GUID metadata missing " + name)
            installed[name] = raw
    return installed


def import_assets(files, dry_run):
    staged = []
    for name, raw in sorted(files.items()):
        dest = PROJECT / name
        if not dest.resolve().is_relative_to(PROJECT.resolve()):
            raise ValueError("Import path escapes Unity project: " + name)
        if dest.exists():
            if checksum(dest.read_bytes()) != checksum(raw):
                raise ValueError("Existing Unity source differs; refusing overwrite: " + name)
            continue
        staged.append((dest, raw))
    if dry_run:
        print("BRAX_MIXAMO_CAR_PREFLIGHT_PASS originals=3 pendingFiles=" + str(len(staged)))
        return
    for dest, raw in staged:
        dest.parent.mkdir(parents=True, exist_ok=True)
        temp = dest.with_suffix(dest.suffix + ".temporary")
        try:
            temp.write_bytes(raw)
            os.replace(temp, dest)
        finally:
            temp.unlink(missing_ok=True)
    print("BRAX_MIXAMO_CAR_IMPORT_PASS originals=3 installedFiles=" + str(len(staged)))


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("archive", type=Path)
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args()
    import_assets(inspect(args.archive), args.dry_run)
