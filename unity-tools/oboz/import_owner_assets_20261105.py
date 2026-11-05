#!/usr/bin/env python3
"""Strict local handoff for BRAXHORN Obóz. No network, Git pushes or build invocation.

Default is verification only. --extract requires explicit CLI consent and copies
private sources into unity-city/Assets/Source/Prywatne/Oboz/OwnerAssets/.
This path is ignored by public Git. No re-encode, rescale, or material changes.
"""
from __future__ import annotations

import argparse
import hashlib
import io
import json
import os
from pathlib import Path, PurePosixPath
import tempfile
import zipfile

EXPECTED_PACKAGE_SHA = "b7d1bd8dd2ed64a4e23b6fad8af2497d0355073b687ea16e48b03f045741d006"
EXPECTED_BYTES = 345_273_037
PREFIX = "BRAXHORN_OBOZ/"
MANIFEST = PREFIX + "MANIFEST_MATERIALOW.json"
MAX_NESTED_UNCOMPRESSED_BYTES = 500_000_000


def sha256_bytes(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def safe_relative(name: str) -> Path:
    posix = PurePosixPath(name)
    if not name or name.startswith("/") or "\\" in name or any(
        part in ("", ".", "..") for part in posix.parts
    ):
        raise ValueError(f"Unsafe archive member: {name!r}")
    return Path(*posix.parts)


def safe_write(destination: Path, data: bytes) -> str:
    # Never clobber a different existing asset without an explicit reviewed change.
    destination.parent.mkdir(parents=True, exist_ok=True)
    if destination.exists():
        if not destination.is_file() or sha256_bytes(destination.read_bytes()) != sha256_bytes(data):
            raise FileExistsError(f"Existing different owner asset: {destination}")
        return "already-identical"
    with tempfile.NamedTemporaryFile(dir=destination.parent, prefix=".incoming-", delete=False) as tmp:
        temporary = Path(tmp.name)
        try:
            tmp.write(data)
            tmp.flush()
            os.fsync(tmp.fileno())
        except BaseException:
            temporary.unlink(missing_ok=True)
            raise
    os.replace(temporary, destination)
    return "copied"


def verify_package(package: Path) -> tuple[dict, dict[str, bytes]]:
    if package.stat().st_size != EXPECTED_BYTES:
        raise ValueError(f"Wrong ZIP size: {package.stat().st_size}")
    if sha256_bytes(package.read_bytes()) != EXPECTED_PACKAGE_SHA:
        raise ValueError("Source archive SHA256 differs from owner handoff manifest")
    with zipfile.ZipFile(package) as archive:
        bad_member = archive.testzip()
        if bad_member:
            raise ValueError(f"CRC error: {bad_member}")
        manifest = json.loads(archive.read(MANIFEST))
        items = manifest["sourceFiles"]
        if len(items) != 30 or len(archive.namelist()) != 31:
            raise ValueError("Expected exactly 30 unique owner assets + manifest")
        expected_names = {MANIFEST}
        unpacked = {}
        for item in items:
            relative = safe_relative(item["path"])
            original_name = PREFIX + relative.as_posix()
            expected_names.add(original_name)
            data = archive.read(original_name)
            if len(data) != item["sizeBytes"] or sha256_bytes(data) != item["sha256"]:
                raise ValueError(f"Asset integrity mismatch: {original_name}")
            unpacked[relative.as_posix()] = data
        if set(archive.namelist()) != expected_names:
            raise ValueError("Archive members differ from manifest")
        return manifest, unpacked


def expand_nested(root: Path, files: dict[str, bytes]) -> list[str]:
    results = []
    for relative, compressed in files.items():
        if not relative.endswith(".zip"):
            continue
        filename = Path(relative).name
        folder = filename.removesuffix(".blend.zip").removesuffix(".zip")
        destination = root / "TEKSTURY_I_ZESTAWY" / "_EXPANDED" / folder
        total = 0
        with zipfile.ZipFile(io.BytesIO(compressed)) as nested:
            if nested.testzip() is not None:
                raise ValueError(f"Nested ZIP CRC invalid: {filename}")
            for part in nested.infolist():
                if part.is_dir():
                    continue
                # Prevent archive bombs and path traversal; no symlink extraction.
                total += part.file_size
                if total > MAX_NESTED_UNCOMPRESSED_BYTES:
                    raise ValueError(f"Nested ZIP exceeds safety budget: {filename}")
                target = destination / safe_relative(part.filename)
                data = nested.read(part)
                if len(data) != part.file_size:
                    raise ValueError("Nested file truncated")
                safe_write(target, data)
                results.append(str(target))
    return results


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("archive", type=Path, help="Original 345273037-byte owner ZIP")
    parser.add_argument("--extract", action="store_true", help="Explicit consent to copy files into private Unity staging")
    parser.add_argument("--expand-nested", action="store_true", help="After --extract, unpack nested ZIPs into private staging")
    parser.add_argument("--unity-root", type=Path, default=Path("unity-city"))
    args = parser.parse_args()
    if args.expand_nested and not args.extract:
        parser.error("--expand-nested requires --extract")
    manifest, assets = verify_package(args.archive)
    counts = {
        "glb": sum(name.endswith(".glb") for name in assets),
        "zip": sum(name.endswith(".zip") for name in assets),
        "hdr": sum(name.endswith(".hdr") for name in assets),
        "jpeg": sum(name.lower().endswith(".jpeg") for name in assets),
    }
    if counts != {"glb": 17, "zip": 7, "hdr": 1, "jpeg": 5}:
        raise ValueError(f"Unexpected owner-asset type counts: {counts}")
    output = {"verified": True, "sourceCount": len(assets), "types": counts, "extracted": False}
    if args.extract:
        private_root = args.unity_root / "Assets/Source/Prywatne/Oboz/OwnerAssets"
        for relative, data in assets.items():
            safe_write(private_root / safe_relative(relative), data)
        output["extracted"] = True
        output["privateDestination"] = str(private_root)
        if args.expand_nested:
            output["expandedCount"] = len(expand_nested(private_root, assets))
    print(json.dumps(output, indent=2, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
