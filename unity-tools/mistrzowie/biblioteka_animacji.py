#!/usr/bin/env python3
"""THE ANIMATION LIBRARY, LISTED (owner 12.11.2026 22:55: "prześledź sam, jakie są ... najpierw zobacz, które brakują,
prześledź bibliotekę na repo"). The owner's Mixamo packs in the repository are encrypted (unity-city/Prywatne/*.tar.gz.enc,
key = the repository secret, only in Actions), so their file NAMES were known to nobody outside a build. This script, run
in the Unity-free job `etap=biblioteka` of mistrzowie-apk.yml, opens them in memory and writes ONLY the list:
the pack, the file, its size, whether the character mesh is inside (With Skin) and the frame rate. Nothing is extracted to
disk, nothing but names leaves the runner.
usage: KLUCZ=... python3 biblioteka_animacji.py <out.json> <out.md>
"""
import io
import json
import pathlib
import re
import subprocess
import sys
import tarfile

ROOT = pathlib.Path(__file__).resolve().parents[2]
PACKS = ROOT / "unity-city/Prywatne"
NAMES = ["ruchy_brax", "ruchy_wlasciciela", "nina_1", "nina_2", "nina_3", "postacie_wzory"]
OPENSSL = ["openssl", "enc", "-aes-256-cbc", "-pbkdf2", "-iter", "200000", "-md", "sha256", "-d", "-pass", "env:KLUCZ"]


def describe(data):
    """with skin = the FBX carries a mesh (Geometry with vertices); fps from the FBX time mode / custom frame rate"""
    skin = b"Geometry" in data and b"Vertices" in data
    fps = None
    m = re.search(rb"CustomFrameRate.{0,40}?([0-9]{2})", data[:400000], re.S)
    if m:
        fps = int(m.group(1))
    return skin, fps


def main():
    out_json, out_md = sys.argv[1], sys.argv[2]
    rows, packs = [], {}
    for name in NAMES:
        f = PACKS / (name + ".tar.gz.enc")
        if not f.exists():
            packs[name] = "missing"
            continue
        r = subprocess.run(OPENSSL + ["-in", str(f)], capture_output=True)
        if r.returncode != 0 or not r.stdout:
            packs[name] = "wrong key or damaged"
            continue
        n = 0
        with tarfile.open(fileobj=io.BytesIO(r.stdout), mode="r:gz") as tar:
            for m in tar.getmembers():
                if not m.isfile() or not m.name.lower().endswith(".fbx"):
                    continue
                data = tar.extractfile(m).read()
                skin, fps = describe(data)
                rows.append({"pack": name, "file": m.name, "mb": round(m.size / 1048576, 2), "withSkin": skin, "fps": fps})
                n += 1
        packs[name] = n
    rows.sort(key=lambda x: (x["pack"], x["file"].lower()))
    json.dump({"packs": packs, "files": rows}, open(out_json, "w"), ensure_ascii=False, indent=1)
    with open(out_md, "w") as md:
        md.write("# Biblioteka animacji (paczki zaszyfrowane w repo)\n\n")
        md.write("Paczki: " + ", ".join(f"{k} = {v}" for k, v in packs.items()) + "\n\n")
        md.write("| paczka | plik | MB | z postacią | fps |\n|---|---|---|---|---|\n")
        for x in rows:
            md.write(f"| {x['pack']} | {x['file']} | {x['mb']} | {'tak' if x['withSkin'] else 'nie'} | {x['fps'] or ''} |\n")
    print("BRAX_ANIMATION_LIBRARY " + json.dumps(packs) + " files=" + str(len(rows)))
    for x in rows:
        print("BRAX_ANIMATION " + x["pack"] + " | " + x["file"])
    return 0 if rows else 1


if __name__ == "__main__":
    sys.exit(main())
