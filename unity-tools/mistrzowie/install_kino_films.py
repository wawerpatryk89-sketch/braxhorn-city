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
    # 12.11 (owner 01:03 "więcej w tej samej wadze"): both re-encoded 1280x720 H.264 High ~1 Mb/s + AAC 96k, faststart
    # (210 + 372 MB -> 120 + 213 MB). The encrypted pack unity-city/Prywatne/mistrzowie-kino-filmy-20261111 carries them
    # (install_owner_media.py); the old release files no longer match and are rejected here.
    # bezprawie.mp4: the owner's "BEZ PRAWIE FILM AI.mp4"
    "bezprawie.mp4": "3776901578c7599ee00240a51714d2983a6e7ba7347daafe15046e0e3adb8925",
    # bez_granic_1.mp4: the owner's "BEZ GRANIC epizod 1 (film AI).mp4"
    "bez_granic_1.mp4": "98f207add8ea2d77d6b5f8bbfb21bd4ed46310f5b2f89b81cd167ec6990228ac",
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
