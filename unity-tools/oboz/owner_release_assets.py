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
import zipfile

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
from owner_assets_intake import SPLIT_PARTS

# iPhone-safe verified independent ZIPs. ZIP #1 includes the *original* 4x
# bull GLB, eliminating the need to download/upload an unsupported .glb file.
# Six small ZIPs reconstruct BRAXHORN_MODELE_CZESC_01_Z_05.zip byte-for-byte.
MINI_PARTS = {
    "BRAXHORN_OBOZ_MALE_01_01_Z_06.zip": (16396829, "e0f113fe622c906e0e5103a3b58156b184b1d48e66c0b52fefae0516c8761903"),
    "BRAXHORN_OBOZ_MALE_01_02_Z_06.zip": (14164201, "fab2bc89964edf6ba0f38d8b6c838bf5356c49cf226c4c0cbc308f1778833310"),
    "BRAXHORN_OBOZ_MALE_01_03_Z_06.zip": (14164201, "8fd1f0b5e42b6193d7e3cb7da8c78042abc6e5cd563b05addb8d4cbddf02bc4a"),
    "BRAXHORN_OBOZ_MALE_01_04_Z_06.zip": (14164201, "144cd694f01c0d5a72f31556636c52740ec8e0684e5567ba253941c093955180"),
    "BRAXHORN_OBOZ_MALE_01_05_Z_06.zip": (14164201, "4c33f3303bcc739b761bac44885bb810ae07221d996a503973fc31c7e4c5648b"),
    "BRAXHORN_OBOZ_MALE_01_06_Z_06.zip": (14164200, "66bbe60a9a7a8a7676e306272fe7f113608d9028f688de3afd77bccff46bb125"),
}

def reconstruct_first_mobile_part(staging: Path) -> Path:
    """Never trust renamed/extraneous files; exact ZIP names, CRC and SHA256.

    Returns a reassembled, unmodified source part 01 ZIP and extracts the GLB
    from mini ZIP 01 to the staging directory.
    """
    destination = staging / "BRAXHORN_MODELE_CZESC_01_Z_05.zip"
    with destination.open("xb") as output:
        for index, name in enumerate(MINI_PARTS, 1):
            chunk_file = staging / name
            content_name = f"BRAXHORN_MODELE_CZESC_01_Z_05.zip.part{index:02d}"
            allow = {content_name, BULL} if index == 1 else {content_name}
            with zipfile.ZipFile(chunk_file) as archive:
                if set(archive.namelist()) != allow or len(archive.namelist()) != len(allow):
                    raise ValueError("Unexpected content in small mobile ZIP: " + name)
                damaged = archive.testzip()
                if damaged:
                    raise ValueError("CRC failed in small mobile ZIP: " + damaged)
                with archive.open(content_name) as payload:
                    shutil.copyfileobj(payload, output, length=2 * 1024 * 1024)
                if index == 1:
                    with archive.open(BULL) as src, (staging / BULL).open("xb") as dest:
                        shutil.copyfileobj(src, dest, length=2 * 1024 * 1024)
    src_size, src_sha = SPLIT_PARTS[destination.name]
    if destination.stat().st_size != src_size or digest(destination) != src_sha:
        raise ValueError("Reconstructed original source part 01 fails SHA-256")
    bull_size, bull_sha = EXPECTED[BULL]
    bull = staging / BULL
    if bull.stat().st_size != bull_size or digest(bull) != bull_sha:
        raise ValueError("Mini ZIP embedded original bull GLB fails SHA-256")
    return destination




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
    bull_present = names.get(BULL) == EXPECTED[BULL][0]
    if bull_present and names.get(SOURCE) == EXPECTED[SOURCE][0]:
        mode = "full"
    elif bull_present and all(names.get(name) == expected[0] for name, expected in SPLIT_PARTS.items()):
        mode = "five_parts"
    elif all(names.get(name) == expected[0] for name, expected in MINI_PARTS.items()) and all(
            names.get(name) == size for name, (size, _) in SPLIT_PARTS.items()
            if name != "BRAXHORN_MODELE_CZESC_01_Z_05.zip"):
        mode = "six_small"
    else:
        required = {name: size for name, (size, _) in MINI_PARTS.items()}
        required.update({name: size for name, (size, _) in SPLIT_PARTS.items()
                         if name != "BRAXHORN_MODELE_CZESC_01_Z_05.zip"})
        missing = [name for name, size in required.items() if names.get(name) != size]
        raise ValueError("Incomplete owner delivery. Upload six mobile ZIPs; keep existing"
                         " parts 02-05. Missing or wrong-size: " + ", ".join(missing))
    print("BRAXHORN_OWNER_RELEASE_PREFLIGHT=PASS mode=" + mode +
          " assets present in draft; no Unity started")
    return mode


def restore(repo: str) -> None:
    mode = remote_preflight(repo)
    TARGET.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="brx-owner-delivery-") as tmp:
        temp = Path(tmp)
        if mode == "full":
            source_names = [SOURCE, BULL]
        elif mode == "five_parts":
            source_names = [*SPLIT_PARTS, BULL]
        else:
            # Four source ZIPs are already on the owner GitHub draft.
            # Upload only six small replacement files (bull inside first).
            source_names = [*MINI_PARTS,
                            *(name for name in SPLIT_PARTS
                              if name != "BRAXHORN_MODELE_CZESC_01_Z_05.zip")]
        for name in source_names:
            if name in EXPECTED:
                size, sha = EXPECTED[name]
            elif name in MINI_PARTS:
                size, sha = MINI_PARTS[name]
            else:
                size, sha = SPLIT_PARTS[name]
            subprocess.run(["gh", "release", "download", TAG, "--repo", repo,
                            "--pattern", name, "--dir", str(temp)],
                           check=True)
            source = temp / name
            if not source.is_file() or source.stat().st_size != size or digest(source) != sha:
                raise ValueError("Owner asset failed SHA256/byte verification: " + name)
        if mode == "six_small":
            reconstruct_first_mobile_part(temp)
        if mode == "full":
            inputs = ["--package", str(temp / SOURCE)]
        else:
            inputs = ["--parts", *(str(temp / n) for n in SPLIT_PARTS)]
        subprocess.run([sys.executable, str(INTAKE), *inputs,
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
