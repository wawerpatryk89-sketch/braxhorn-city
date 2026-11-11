#!/usr/bin/env python3
"""BRAXHORN — the TV film library for EVERY region's build (rule docs/zasady/ZASADA_TELEWIZORY_FILMY_20261111.md).

Run before Unity in any APK workflow, in the step that has the KLUCZ secret:

    python3 unity-tools/wideo/install_braxhorn_wideo.py

It opens the encrypted pack unity-city/Prywatne/braxhorn-wideo-20261111 (openssl cms to the owner's CI transfer
certificate; its private key recipient.pem.enc is protected by KLUCZ), checks every part and file against manifest.json
and stages the films BraxTelewizor plays (the names in BraxWideo.Tv / BraxWideo.Pion,
unity-city/Assets/BraxhornWideo/BraxTelewizor.cs) into unity-city/Assets/StreamingAssets/BraxhornWideo/{tv,pion}.
No KLUCZ / no pack = the build goes on and the TVs show their still pictures. Never starts Unity, a build or a workflow.
No dependencies beyond Python 3 and openssl.
"""
from pathlib import Path
import hashlib
import json
import os
import re
import shutil
import subprocess
import sys
import tarfile
import tempfile

ROOT = Path(__file__).resolve().parents[2]
PACK = ROOT / "unity-city/Prywatne/braxhorn-wideo-20261111"
OUT = ROOT / "unity-city/Assets/StreamingAssets/BraxhornWideo"
TELEWIZOR = ROOT / "unity-city/Assets/BraxhornWideo/BraxTelewizor.cs"
OPENSSL_VARIANTS = [
    ["openssl", "enc", "-d", "-aes-256-cbc", "-pbkdf2", "-iter", "200000", "-md", "sha256"],
    ["openssl", "enc", "-d", "-aes-256-cbc", "-pbkdf2", "-iter", "100000", "-md", "sha256"],
    ["openssl", "enc", "-d", "-aes-256-cbc", "-pbkdf2", "-md", "sha256"],
    ["openssl", "enc", "-d", "-aes-256-cbc", "-md", "sha256"],
]


def sha256(path):
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for block in iter(lambda: f.read(1 << 20), b""):
            h.update(block)
    return h.hexdigest()


def catalogue(src=TELEWIZOR):
    """the film names BraxTelewizor plays: {"tv": [...], "pion": [...]}"""
    text = Path(src).read_text(encoding="utf-8")
    out = {}
    for key, field in (("tv", "Tv"), ("pion", "Pion")):
        m = re.search(r"public static readonly string\[\] " + field + r"\s*=\s*\{(.*?)\};", text, re.S)
        out[key] = re.findall(r'"([^"]+\.mp4)"', m.group(1)) if m else []
    return out


def open_pack(pack, tmp):
    """decrypts and verifies the pack into tmp; returns the folder or None"""
    manifest = json.loads((pack / "manifest.json").read_text())
    pem = Path(tmp) / "recipient.pem"
    for variant in OPENSSL_VARIANTS:
        r = subprocess.run(variant + ["-in", str(pack / "recipient.pem.enc"), "-out", str(pem), "-pass", "env:KLUCZ"], capture_output=True)
        if r.returncode == 0 and b"PRIVATE KEY" in pem.read_bytes()[:64]:
            break
    else:
        print("BRAX_WIDEO_KEY_FAILED: recipient.pem.enc did not open with KLUCZ")
        return None
    cms = Path(tmp) / "pack.cms"
    with open(cms, "wb") as out:
        for part in manifest["parts"]:
            p = pack / part["name"]
            if not p.exists() or sha256(p) != part["sha256"]:
                print("BRAX_WIDEO_PART_BAD " + part["name"])
                return None
            out.write(p.read_bytes())
    tar = Path(tmp) / "pack.tar"
    r = subprocess.run(["openssl", "cms", "-decrypt", "-binary", "-inform", "DER", "-in", str(cms), "-out", str(tar),
                        "-inkey", str(pem), "-recip", str(pack / "recipient.crt")], capture_output=True)
    cms.unlink()
    pem.unlink()
    if r.returncode != 0:
        print("BRAX_WIDEO_DECRYPT_FAILED: " + r.stderr.decode(errors="replace")[:200])
        return None
    out = Path(tmp) / "wideo"
    with tarfile.open(tar) as t:
        t.extractall(out, filter="data")
    tar.unlink()
    for rel, digest in manifest["files"].items():
        f = out / rel
        if not f.exists() or sha256(f) != digest:
            print("BRAX_WIDEO_FILE_BAD " + rel)
            return None
    return out


def install(pack=PACK, out_dir=OUT):
    if not os.environ.get("KLUCZ", "").strip():
        print("BRAX_WIDEO_SKIPPED: no KLUCZ (the TVs show their still pictures)")
        return 0
    if not (pack / "manifest.json").exists():
        print("BRAX_WIDEO_SKIPPED: no pack " + str(pack))
        return 0
    with tempfile.TemporaryDirectory() as tmp:
        lib = open_pack(pack, tmp)
        if not lib:
            return 0
        n = 0
        for sub, names in catalogue().items():
            (out_dir / sub).mkdir(parents=True, exist_ok=True)
            for name in names:
                if (lib / sub / name).exists():
                    shutil.copy2(lib / sub / name, out_dir / sub / name); n += 1
                    print("BRAX_WIDEO_OK " + sub + "/" + name)
                else:
                    print("BRAX_WIDEO_MISSING " + sub + "/" + name + " (BraxTelewizor skips it)")
        print("BRAX_WIDEO_STAGED " + str(n))
    return 0


if __name__ == "__main__":
    sys.exit(install())
