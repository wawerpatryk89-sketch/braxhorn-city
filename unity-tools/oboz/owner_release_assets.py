#!/usr/bin/env python3
"""Private owner asset delivery gate for Oboz Unity 6.

Requires exactly two owner assets on *draft* GitHub release
    oboz-owner-remodel-20261105
  BRAXHORN_OBOZ_WYKONAWCA_ZASOBY_20261105.zip
  BRAXHORN_BULL_RONDO_4X_ORYGINALNE_TEKSTURY.glb

Never publishes source to a public git branch, never changes actors.
Only a workflow invoked deliberately by the owner should call restore.
"""
from __future__ import annotations
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile

TAG = "oboz-owner-remodel-20261105"
SOURCE = "BRAXHORN_OBOZ_WYKONAWCA_ZASOBY_20261105.zip"
BULL = "BRAXHORN_BULL_RONDO_4X_ORYGINALNE_TEKSTURY.glb"
EXPECTED = {
    SOURCE: (345273037, "b7d1bd8dd2ed64a4e23b6fad8af2497d0355073b687ea16e48b03f045741d006"),
    BULL: (2232460, "425788876b8fe2dfaba90157d74c6631d231184f96659759cdafade794a5352f"),
}
REQUIRED_PATHS = [
    "leather_punching_bag.glb",
    "metal_cabinet_low_poly.glb",
    "kitchen_-_assets.glb",
    "glass-food-display-showcase-with-trays.glb",
    "picnic_set_1.glb",
    "close-coupled-toilet.glb",
    "wall-urinal.glb",
    "trough-washbasin.glb",
    "shower-tray-square.glb",
    "towel-rail-with-towel.glb",
    "Panoramy/montorfano_4k.hdr",
    "slab_tiles_2k/textures/slab_tiles_diff_2k.jpg",
    "slab_tiles_2k/textures/slab_tiles_nor_gl_2k.exr",
    "slab_tiles_2k/textures/slab_tiles_rough_2k.exr",
    "rubberized_track_2k/textures/rubberized_track_diff_2k.jpg",
    "rubberized_track_2k/textures/rubberized_track_nor_gl_2k.exr",
    "rubberized_track_2k/textures/rubberized_track_rough_2k.exr",
    "Przystanek/Meshy_AI_The_object_is_a_recta_1104160815_texture.fbx",
    "Murale/44B8F5D6-7F27-4EF3-A804-9BC8C4F462D0.jpeg",
]
ROOT = Path(__file__).resolve().parents[2]
TARGET = ROOT / "unity-city/Assets/Source/Prywatne/Oboz/OwnerAssets"
INTAKE = ROOT / "unity-tools/oboz/owner_assets_intake.py"


def digest(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as source:
        for block in iter(lambda: source.read(8 * 1024 * 1024), b""):
            h.update(block)
    return h.hexdigest()


def remote_preflight(repo: str) -> None:
    if not repo or "/" not in repo:
        raise ValueError("GITHUB_REPOSITORY must be owner/repo")
    proc = subprocess.run(
        ["gh", "release", "view", TAG, "--repo", repo, "--json", "isDraft,assets"],
        check=True, capture_output=True, text=True)
    release = json.loads(proc.stdout)
    if release.get("isDraft") is not True:
        raise ValueError("Owner asset release must be a private DRAFT, never published")
    names = {item["name"]: item["size"] for item in release["assets"]}
    for name, (size, _) in EXPECTED.items():
        if names.get(name) != size:
            raise ValueError(f"Owner delivery missing or wrong size: {name}, expected {size}")
    print("BRAXHORN_OWNER_RELEASE_PREFLIGHT=PASS; all assets present in draft; no Unity started")


def restore(repo: str) -> None:
    remote_preflight(repo)
    TARGET.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="brx-owner-delivery-") as tmp:
        temp = Path(tmp)
        for name, (size, sha) in EXPECTED.items():
            subprocess.run(["gh", "release", "download", TAG, "--repo", repo,
                            "--pattern", name, "--dir", str(temp)],
                           check=True)
            source = temp / name
            if not source.is_file() or source.stat().st_size != size or digest(source) != sha:
                raise ValueError("Owner asset failed SHA256/byte verification: " + name)
        subprocess.run([sys.executable, str(INTAKE),
                        "--package", str(temp / SOURCE),
                        "--target", str(TARGET), "--apply"],
                       cwd=ROOT, check=True)
        original = temp / BULL
        dest = TARGET / BULL
        if dest.exists():
            if digest(dest) != EXPECTED[BULL][1]:
                raise ValueError("Existing original bull differs from owner SHA256")
        else:
            with dest.open("xb") as file:
                with original.open("rb") as src:
                    shutil.copyfileobj(src, file, 8 * 1024 * 1024)
        absent = [p for p in REQUIRED_PATHS if not (TARGET / p).is_file()]
        if absent:
            raise ValueError("Required original Unity assets absent after intake: " + ", ".join(absent))
        if digest(dest) != EXPECTED[BULL][1]:
            raise ValueError("Bull checksum corrupted on stage")
        # Stable Unity GUID for the bull after verifying unchanged GLB bytes.
        from owner_assets_intake import ensure_stable_meta
        ensure_stable_meta(TARGET, Path(BULL))
    print("BRAXHORN_OWNER_RELEASE_RESTORE=PASS originalGLB=untouched guid=stable characterModels=untouched")


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("operation", choices=("preflight", "restore"))
    ap.add_argument("--repo", default=os.environ.get("GITHUB_REPOSITORY", ""))
    args = ap.parse_args()
    try:
        if args.operation == "preflight":
            remote_preflight(args.repo)
        else:
            restore(args.repo)
    except (OSError, ValueError, subprocess.CalledProcessError, KeyError, json.JSONDecodeError) as exc:
        print("::error::BRAXHORN_OWNER_RELEASE_FAIL " + str(exc), file=sys.stderr)
        return 2
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
