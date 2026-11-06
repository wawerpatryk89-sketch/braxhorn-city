#!/usr/bin/env python3
"""Restore owner gym/tire originals from a PRIVATE GitHub draft; never public git.

Source package is created from the owner's ChatGPT Library originals.
Nothing is silently replaced or downloaded from an unknown third-party host.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import subprocess
import sys
import tempfile
import zipfile

ROOT = Path(__file__).resolve().parents[2]
TARGET = ROOT / "unity-city/Assets/Source/Prywatne/Oboz/OwnerAssets"
RELEASE_ID = 435486352
TAG = "oboz-owner-remodel-20261105"
NAME = "oboz_silownia_opony.zip"  # exact uploaded owner filename
BED_NAME = "ikea_idanas_single_bed.glb"
BED_BYTES = 5374568
BED_SHA256 = "26efbc9f32157c909d1cd9bde9b1692e4734728a0dcde7466739fe2ce5ff449a"
BED_REL = Path("OwnerExtras/Bedroom") / BED_NAME
BYTES = 7298373
SHA256 = "8fee0543521a8ae6043a0c4179e26f2bcd69f1cd778904e2d7b177f7cb22661f"
ACTIVE = {
    "OwnerExtras/Gym/gym_environment.fbx",
    "OwnerExtras/Gym/gym_environment.png",
    "OwnerExtras/tractor_tire.glb",
}

def hash_bytes(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()

def hash_file(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as file:
        for data in iter(lambda: file.read(1024 * 1024), b""):
            h.update(data)
    return h.hexdigest()

def checked_package(path: Path) -> dict[Path, bytes]:
    if not path.is_file() or path.stat().st_size != BYTES or hash_file(path) != SHA256:
        raise ValueError("Owner extra ZIP size/SHA256 mismatch; do not import")
    with zipfile.ZipFile(path) as z:
        members = z.infolist()
        names = [m.filename for m in members]
        if len(names) != len(set(names)) or "MANIFEST.json" not in names:
            raise ValueError("Missing manifest or duplicate ZIP entries")
        manifest = json.loads(z.read("MANIFEST.json"))
        entries = manifest["entries"]
        expected = {item["path"] for item in entries} | {"MANIFEST.json"}
        if set(names) != expected or len(names) != len(expected):
            raise ValueError("Owner ZIP does not match exact signed-off inventory")
        approved = set(manifest["approvedForScene"])
        if approved != ACTIVE or manifest.get("schema") != 1:
            raise ValueError("Unapproved owner assets or schema")
        payloads: dict[Path, bytes] = {}
        for entry in entries:
            name = entry["path"]
            rel = PurePosixPath(name)
            if rel.is_absolute() or ".." in rel.parts or not rel.parts:
                raise ValueError("Unsafe owner asset path: " + name)
            raw = z.read(name)  # checks CRC
            if len(raw) != entry["bytes"] or hash_bytes(raw) != entry["sha256"]:
                raise ValueError("Owner extra file differs from source: " + name)
            if name in ACTIVE:
                payloads[Path(name)] = raw
        if set(str(x).replace("\\", "/") for x in payloads) != ACTIVE:
            raise ValueError("Owner gym/tire active assets incomplete")
        return payloads

def verified_bed(path: Path) -> bytes:
    if not path.is_file() or path.stat().st_size != BED_BYTES or hash_file(path) != BED_SHA256:
        raise ValueError("Original owner single bed GLB missing or SHA-256 mismatch")
    with path.open("rb") as f:
        header=f.read(20)
    if (len(header) != 20 or header[:4] != b"glTF" or
        int.from_bytes(header[4:8],"little") != 2 or
        int.from_bytes(header[8:12],"little") != BED_BYTES or
        header[16:20] != b"JSON"):
        raise ValueError("Owner bed is not a valid GLB 2.0 JSON chunk")
    return path.read_bytes()


def remote_info(repo: str) -> None:
    if not repo or "/" not in repo:
        raise ValueError("Expected GITHUB_REPOSITORY owner/name")
    proc = subprocess.run(["gh", "api", f"repos/{repo}/releases/{RELEASE_ID}"],
                          text=True, capture_output=True, check=True)
    release = json.loads(proc.stdout)
    if release.get("id") != RELEASE_ID or release.get("tag_name") != TAG or not release.get("draft"):
        raise ValueError("Incorrect owner private DRAFT release")
    for filename,bytes_expected,sha_expected in (
        (NAME,BYTES,SHA256),(BED_NAME,BED_BYTES,BED_SHA256)):
        match=[a for a in release["assets"] if a["name"]==filename]
        if len(match)!=1 or match[0]["size"]!=bytes_expected:
            raise ValueError("Owner material missing in private release before Unity: "+filename)
        # GitHub publishes the actual SHA-256 digest of each uploaded asset.
        # Verify when provided; the downloaded bytes are ALWAYS hashed below.
        digest=match[0].get("digest")
        if digest and digest != "sha256:"+sha_expected:
            raise ValueError("Owner material GitHub digest mismatch: "+filename)

def install(package: Path, target: Path, bed: Path | None=None) -> None:
    items = checked_package(package)
    if bed is not None:
        items[BED_REL]=verified_bed(bed)
    target.mkdir(parents=True, exist_ok=True)
    for rel, source in sorted(items.items()):
        destination = target / rel
        destination.parent.mkdir(parents=True, exist_ok=True)
        if destination.exists():
            if hash_file(destination) != hash_bytes(source):
                raise ValueError("Existing Unity asset mismatches owner original: " + rel.as_posix())
        else:
            with destination.open("xb") as file:
                file.write(source)
    # Preserve existing GUIDs, deterministically initialize new assets/folders.
    from owner_assets_intake import stamp_unity_metadata
    stamp_unity_metadata(target, list(items.items()))
    if bed is not None and BED_REL not in items:
        raise ValueError("Owner bed not installed")
    print("BRAXHORN_OBOZ_OWNER_GYM_EXTRA_RESTORE=PASS gym_models=3 bed="+
          str(int(bed is not None))+" originals_UNCHANGED=true")

def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("command", choices=("preflight", "restore"))
    ap.add_argument("--local", type=Path, help="Local private ZIP, for offline integrity tests")
    ap.add_argument("--bed-local", type=Path, help="Local original bed GLB for integrity tests")
    ap.add_argument("--repo", default=os.environ.get("GITHUB_REPOSITORY", ""))
    ap.add_argument("--target", type=Path, default=TARGET)
    args = ap.parse_args()
    try:
        if args.local:
            checked_package(args.local)
            if args.bed_local is not None: verified_bed(args.bed_local)
            if args.command == "restore":
                if args.bed_local is None:
                    raise ValueError("Local restore must include --bed-local")
                install(args.local, args.target, args.bed_local)
            else:
                print("BRAXHORN_OBOZ_OWNER_GYM_EXTRA_PREFLIGHT=PASS local="+
                      str(args.bed_local is not None))
            return 0
        remote_info(args.repo)
        if args.command == "preflight":
            print("BRAXHORN_OBOZ_OWNER_GYM_EXTRA_PREFLIGHT=PASS private_draft=True")
            return 0
        with tempfile.TemporaryDirectory(prefix="brx-gym-private-") as tmp:
            for filename in (NAME,BED_NAME):
                subprocess.run(["gh", "release", "download", TAG,
                                "--repo", args.repo, "--pattern", filename,
                                "--dir", tmp], check=True)
            install(Path(tmp) / NAME, args.target, Path(tmp) / BED_NAME)
        return 0
    except (OSError, ValueError, KeyError, zipfile.BadZipFile,
            subprocess.CalledProcessError, json.JSONDecodeError) as e:
        print("::error::BRAXHORN_OBOZ_OWNER_GYM_EXTRA_FAIL " + str(e), file=sys.stderr)
        return 2

if __name__ == "__main__":
    raise SystemExit(main())
