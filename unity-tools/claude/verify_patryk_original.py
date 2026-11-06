#!/usr/bin/env python3
"""Verify the user's original Patryk FBX bundle before any Unity import.

No FBX, model or texture bytes are modified. This tool does NOT validate
anatomical retargeting or phone rendering: those require Unity Play Mode.
"""
import argparse
import hashlib
import json
from pathlib import Path
import zipfile

MANIFEST = Path(__file__).with_name("patryk-owner-22-manifest.json")


def sha256_file(path):
    h = hashlib.sha256()
    with open(path, "rb") as source:
        for chunk in iter(lambda: source.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()


def verify(animation_zip, model_zip=None):
    manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
    source = Path(animation_zip)
    assert source.is_file(), f"Missing original animation archive: {source}"
    actual = sha256_file(source)
    expected = manifest["animation_archive"]["sha256"]
    if actual != expected:
        raise ValueError(f"Animation ZIP differs from approved original: expected {expected}, got {actual}")
    expected_files = {x["file"]: x["sha256"] for x in manifest["clips"]}
    with zipfile.ZipFile(source) as archive:
        names = archive.namelist()
        if len(names) != manifest["animation_archive"]["file_count"] or set(names) != set(expected_files) or len(set(names)) != len(names):
            raise ValueError("Unexpected/missing/duplicate FBX entries in Patryk archive")
        for name in names:
            raw = archive.read(name)
            if not raw.startswith(b"Kaydara FBX Binary"):
                raise ValueError(f"Not an original binary FBX: {name}")
            digest = hashlib.sha256(raw).hexdigest()
            if digest != expected_files[name]:
                raise ValueError(f"FBX bytes differ from owner supplied source: {name}")
    model_ok = None
    if model_zip:
        original = Path(model_zip)
        if not original.is_file():
            raise ValueError(f"Missing original model archive: {original}")
        if sha256_file(original) != manifest["model_archive"]["sha256"]:
            raise ValueError("Patryk original model has been substituted/changed")
        model_ok = True
    return {
        "animation_zip_exact_match": True,
        "original_FBX_count": len(expected_files),
        "model_zip_exact_match": model_ok,
        "rig_and_unity_animation_verified": False,
        "phone_build_verified": False
    }


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--animation-zip", required=True, type=Path)
    p.add_argument("--model-zip", type=Path)
    args = p.parse_args()
    print(json.dumps(verify(args.animation_zip, args.model_zip), indent=2, ensure_ascii=False))


if __name__ == "__main__":
    main()
