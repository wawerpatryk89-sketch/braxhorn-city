#!/usr/bin/env python3
"""Install already-downloaded, owner-approved BRAXHORN car recordings.

This tool NEVER downloads from the network and is not part of the APK build.
Download each file manually from the official Pixabay page in
champion_car_audio_sources.json, place it at source_path, then run this tool.
It computes SHA-256/byte count, copies the exact source bytes into Unity, and
updates the manifest. A different existing Unity audio file is never overwritten.
"""
from pathlib import Path
import hashlib
import json
import os

ROOT = Path(__file__).resolve().parents[2]
MANIFEST = ROOT / "unity-tools/mistrzowie/champion_car_audio_sources.json"

def sha256(path):
    h = hashlib.sha256()
    with path.open("rb") as f:
        for block in iter(lambda: f.read(1024 * 1024), b""):
            h.update(block)
    return h.hexdigest()

def atomic_copy(source, target):
    data = source.read_bytes()
    if target.exists():
        if target.read_bytes() != data:
            raise ValueError("Refusing to overwrite different approved Unity audio: " + str(target))
        return False
    target.parent.mkdir(parents=True, exist_ok=True)
    temp = target.with_name(target.name + ".partial")
    try:
        temp.write_bytes(data)
        os.replace(temp, target)
    finally:
        temp.unlink(missing_ok=True)
    return True

def main():
    doc = json.loads(MANIFEST.read_text("utf-8"))
    missing = []
    changed = 0
    for record in doc["files"]:
        source = ROOT / record["source_path"]
        if not source.is_file():
            missing.append(str(source.relative_to(ROOT)))
            continue
        if source.suffix.lower() not in {".mp3", ".ogg", ".wav"}:
            raise ValueError("Unsupported car audio source extension: " + str(source))
        size = source.stat().st_size
        if size < 4096:
            raise ValueError("Car audio source is implausibly small: " + str(source))
        digest = sha256(source)
        target = ROOT / record["unity_path"]
        changed += int(atomic_copy(source, target))
        record["sha256"] = digest
        record["bytes"] = size
        record["binary_present"] = True
    if missing:
        print("A1_CAR_AUDIO_PENDING missing=" + " | ".join(missing))
        raise SystemExit(2)
    doc["status"] = "BINARIES_PRESENT_AND_HASHED"
    MANIFEST.write_text(json.dumps(doc, ensure_ascii=False, indent=2) + "\n", "utf-8")
    print("A1_CAR_AUDIO_PREPARED files=2 created=" + str(changed))
    for record in doc["files"]:
        print(record["role"] + " sha256=" + record["sha256"] + " bytes=" + str(record["bytes"]))

if __name__ == "__main__":
    main()
