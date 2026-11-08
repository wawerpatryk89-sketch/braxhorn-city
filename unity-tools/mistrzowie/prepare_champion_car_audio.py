#!/usr/bin/env python3
"""Verify the committed BRAXHORN Issue #30 engine-audio derivatives.

No network, no download, no APK and no source mutation. The two small OGG files
are committed in Unity and must match the audited hashes/byte counts in
champion_car_audio_sources.json.
"""
from pathlib import Path
import hashlib
import json

ROOT = Path(__file__).resolve().parents[2]
MANIFEST = ROOT / "unity-tools/mistrzowie/champion_car_audio_sources.json"

def sha256(path):
    h = hashlib.sha256()
    with path.open("rb") as f:
        for block in iter(lambda: f.read(1024 * 1024), b""):
            h.update(block)
    return h.hexdigest()

def main():
    doc = json.loads(MANIFEST.read_text("utf-8"))
    if doc.get("status") != "BINARIES_COMMITTED_AND_HASHED":
        raise ValueError("Issue #30 car-audio manifest is not final")
    if doc.get("source", {}).get("source_sha256") != "15a9fcf82f53c710f489b7d888557b4937ae3f50d968c22f2be342d29df398b6":
        raise ValueError("Unexpected audited source recording hash")
    for clip in doc["clips"]:
        path = ROOT / clip["unity_path"]
        if not path.is_file():
            raise FileNotFoundError("Committed car audio missing: " + str(path))
        data = path.read_bytes()
        if len(data) != int(clip["bytes"]):
            raise ValueError("Car audio byte count mismatch: " + clip["role"])
        digest = sha256(path)
        if digest != clip["sha256"]:
            raise ValueError("Car audio SHA-256 mismatch: " + clip["role"])
        if not data.startswith(b"OggS") or b"vorbis" not in data[:8192]:
            raise ValueError("Car audio is not Ogg/Vorbis: " + clip["role"])
        if b"CC BY 3.0 Unported" not in data[:8192] or b"Antti Makkonen" not in data[:8192]:
            raise ValueError("Embedded CC BY attribution missing: " + clip["role"])
        print("A1_CAR_AUDIO_VERIFIED role=%s bytes=%d sha256=%s" %
              (clip["role"], len(data), digest))
    print("A1_CAR_AUDIO_FINAL_PASS clips=2 real_recording=true network=false apk=false")

if __name__ == "__main__":
    main()
