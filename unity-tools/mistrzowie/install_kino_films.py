#!/usr/bin/env python3
"""Stage the owner's two cinema films for G1 KINO MULTIPLEX (owner 11.11 19:00-19:09: "tylko dwa filmy damy", "żebym
sobie wszedł do kina i mógł odpalić ... te filmy").

The films are 210 / 372 MB (GitHub keeps no file over 100 MB in the repository), so they live as assets of the release
mistrzowie-kino-filmy with the EXACT names below and the approved SHA-256; this script puts them next to repertuar.json
in StreamingAssets/Mistrzowie/Kino (they go into the APK as they are, the VideoPlayer plays them from there).
If the release or a file is missing the build goes on and the hall's screen says BRAK PLIKU FILMU (BraxChampionsKino):
nothing else depends on the films. This script never starts Unity, an APK build or another workflow.
"""
from pathlib import Path
import hashlib
import os
import shutil
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[2]
DEST = ROOT / "unity-city/Assets/StreamingAssets/Mistrzowie/Kino"
RELEASE = "mistrzowie-kino-filmy"
FILMS = {
    # bezprawie.mp4: the owner's "BEZ PRAWIE FILM AI.mp4" (1278x720) scaled to 1280x720 H.264 High + AAC, faststart
    "bezprawie.mp4": "5952685b1193f1b3f9f29b319a055e767d45a336fc41766a1db527a99a0656b1",
    # bez_granic_1.mp4: the owner's "BEZ GRANIC epizod 1 (film AI).mp4" unchanged (1280x720 H.264 High + AAC)
    "bez_granic_1.mp4": "3c879a1428b12e0e6cbf68ae95b4c4a5b0caf5295c3e8cde0db30bccdea56742",
}


def sha256(path):
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for block in iter(lambda: f.read(1 << 20), b""):
            h.update(block)
    return h.hexdigest()


def main():
    DEST.mkdir(parents=True, exist_ok=True)
    ok = 0
    for name, digest in FILMS.items():
        target = DEST / name
        if target.exists() and sha256(target) == digest:
            print("BRAX_KINO_FILM_OK " + name + " (already staged)")
            ok += 1
            continue
        with tempfile.TemporaryDirectory() as tmp:
            r = subprocess.run(["gh", "release", "download", RELEASE, "--pattern", name, "--dir", tmp, "--clobber"],
                               capture_output=True, text=True)
            got = Path(tmp) / name
            if r.returncode != 0 or not got.exists():
                print("BRAX_KINO_FILM_MISSING " + name + " (release " + RELEASE + "): " + (r.stderr or "").strip()[:200])
                continue
            if sha256(got) != digest:
                print("BRAX_KINO_FILM_REJECTED " + name + ": SHA-256 differs from the approved file")
                continue
            shutil.move(str(got), target)
            print("BRAX_KINO_FILM_OK " + name + " %.1f MB" % (target.stat().st_size / 1048576))
            ok += 1
    print("BRAX_KINO_FILMS staged=%d/%d" % (ok, len(FILMS)))
    return 0


if __name__ == "__main__":
    sys.exit(main())
