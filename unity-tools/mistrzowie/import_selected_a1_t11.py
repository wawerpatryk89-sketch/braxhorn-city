#!/usr/bin/env python3
"""Pre-Unity import of ONLY the approved A1/T11 original photos.

Archive contains 3 A1 murals, 3 T11 roadside/casino signs and the owner's
exact 1536x512 BRA XHORN ARENA case plaque. Preserves raw image bytes and
provided Unity GUIDs. No cropping, re-rendering, resizing, placeholders,
Unity run, APK build, repo mutation or replacement of approved originals.
"""
from pathlib import Path
import argparse
import hashlib
import json
import os
import re
import struct
import zipfile

ROOT = Path(__file__).resolve().parents[2]
PROJECT = ROOT / "unity-city"
ARCHIVE_SHA256 = "5d2c3bd341772c1f61d695a898a99c2d9e55b383bfa2bece0753cfaa9f502d55"
PREFIX = "Assets/Source/Mistrzowie/"
MURALS = (
    PREFIX + "ArenaMurals/BRAXHORN_CHAMPION_FIGHTER_ORIGINAL.jpg",
    PREFIX + "ArenaMurals/BRAXHORN_A1_FIGHTERS_LEFT_ORIGINAL.jpg",
    PREFIX + "ArenaMurals/BRAXHORN_A1_BELT_RIGHT_ORIGINAL.jpg",
)
SIGNS = (
    PREFIX + "CitySigns/01_GLOWNE/C1_KASYNO_KASYNO_WAWER.png",
    PREFIX + "CitySigns/03_TRANSPORT_KURORT/TAXI_TAXI.png",
    PREFIX + "CitySigns/03_TRANSPORT_KURORT/PARKING_PARKING.png",
)
PLAQUE = PREFIX + "Trophies/BRAXHORN_ARENA_CASE_PLAQUE.jpg"
IMAGES = set(MURALS + SIGNS + (PLAQUE,))
FOLDERS = {
    PREFIX + "ArenaMurals.meta",
    PREFIX + "CitySigns.meta",
    PREFIX + "CitySigns/01_GLOWNE.meta",
    PREFIX + "CitySigns/03_TRANSPORT_KURORT.meta",
}
EXPECTED = IMAGES | {p + ".meta" for p in IMAGES} | FOLDERS
ORIGINAL_PLAQUE_SHA256 = "33df322fdfb51ea59d9e6f07ba784ca39eaabe44f14ec72dabc4f389160bb213"


def digest(data):
    return hashlib.sha256(data).hexdigest()


def validate(path):
    if not path.is_file():
        raise FileNotFoundError("Owner A1/T11 7 originals ZIP not found: " + str(path))
    h = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            h.update(chunk)
    if h.hexdigest() != ARCHIVE_SHA256:
        raise ValueError("Selected ORIGINAL artwork archive SHA-256 mismatch")
    with zipfile.ZipFile(path) as archive:
        if archive.testzip() is not None:
            raise ValueError("ZIP CRC failed")
        names = archive.namelist()
        if len(names) != len(set(names)) or set(names) != EXPECTED | {"MANIFEST.json"}:
            raise ValueError("Unexpected, duplicate or missing A1/T11 original asset paths")
        info = archive.infolist()
        if any(i.is_dir() or (i.external_attr >> 16) & 0o170000 == 0o120000 for i in info):
            raise ValueError("Directory/symlink entries forbidden")
        manifest = json.loads(archive.read("MANIFEST.json"))
        if manifest.get("signs") != 3 or manifest.get("murals") != 3 or manifest.get("plaque") != 1:
            raise ValueError("A1/T11 owners' graphic selection differs")
        if manifest.get("plaqueSha256") != ORIGINAL_PLAQUE_SHA256:
            raise ValueError("Owner logo art was changed in manifest")
        records = manifest.get("files")
        if not isinstance(records, list) or {x.get("path") for x in records} != EXPECTED:
            raise ValueError("A1/T11 manifest does not list all originals exactly once")
        for record in records:
            name = record["path"]
            raw = archive.read(name)
            if len(raw) != record["bytes"] or digest(raw) != record["sha256"]:
                raise ValueError("Original image or Unity .meta was modified: " + name)
            if name in IMAGES:
                if name.endswith(".png"):
                    if raw[:8] != b"\x89PNG\r\n\x1a\n" or struct.unpack(">II", raw[16:24]) != (2048, 512):
                        raise ValueError("Original PNG needs 2048x512: " + name)
                elif raw[:2] != b"\xff\xd8" or raw[-2:] != b"\xff\xd9":
                    raise ValueError("Corrupt owner original JPEG: " + name)
            if name.endswith(".meta") and not re.search(rb"^guid:\s*[0-9a-f]{32}\s*$", raw, re.M):
                raise ValueError("Invalid original stable Unity GUID: " + name)
        if digest(archive.read(PLAQUE)) != ORIGINAL_PLAQUE_SHA256:
            raise ValueError("Owner BRAXHORN ARENA case image differs from supplied artwork")
        data = {name: archive.read(name) for name in EXPECTED}
    return data


def install(files, dry_run=False):
    # A path is installed only if already absent or byte-identical. Never
    # replace an approved owner photo or Unity meta with a different file.
    actions = []
    for name in sorted(files):
        target = PROJECT / name
        if not target.resolve().is_relative_to(PROJECT.resolve()):
            raise ValueError("Invalid A1/T11 destination: " + name)
        if target.exists():
            if digest(target.read_bytes()) != digest(files[name]):
                raise ValueError("Existing owner asset differs; overwrite prohibited: " + name)
            continue
        actions.append((target, files[name]))
    if dry_run:
        print("A1_T11_SELECTED_PREFLIGHT_PASS pending=" + str(len(actions))
              + " signs=3 murals=3 plaque=1")
        return
    for target, data in actions:
        target.parent.mkdir(parents=True, exist_ok=True)
        temporary = target.with_name(target.name + ".temporary")
        try:
            temporary.write_bytes(data)
            os.replace(temporary, target)
        finally:
            temporary.unlink(missing_ok=True)
    print("A1_T11_SELECTED_IMPORT_PASS installed=" + str(len(actions))
          + " signs=3 murals=3 plaque=1 originals=UNCHANGED")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("archive", type=Path)
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args()
    install(validate(args.archive), args.dry_run)
