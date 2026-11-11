#!/usr/bin/env python3
"""Stage the owner's media for Miasto Mistrzów before Unity (CI): the G1 cinema's two films and the short ads that loop on
the TVs (owner 11.11 19:00-19:09 films, 20:46-20:47 ads: "w każdym jednym telewizorze, żeby coś leciało ... niech się
zapętli"). The repository is public, so the files live only encrypted (ZASADA: assety od właściciela pakować od razu):

  unity-city/Prywatne/mistrzowie-kino-filmy-20261111/   bezprawie.mp4, bez_granic_1.mp4 (as approved, SHA-256 below)
  unity-city/Prywatne/mistrzowie-reklamy-20261111/      the five ads: originals + the TV versions (640x360, no sound)

Each pack: tar -> openssl cms -encrypt -aes256 (DER) to recipient.crt (the owner's CI transfer certificate "Braxhorn camp
input transfer", release oboz-transfer-key-41749904696) -> parts <= 90 MB with SHA-256 in manifest.json. The private key
recipient.pem.enc is itself encrypted with the KLUCZ secret (openssl enc, Salted__), so only Actions can open the packs.
Missing key / pack = the build goes on: the cinema says BRAK PLIKU FILMU, the TVs keep their still pictures. Never starts
Unity, an APK build or another workflow.
"""
from pathlib import Path
import hashlib
import json
import os
import shutil
import subprocess
import sys
import tarfile
import tempfile

ROOT = Path(__file__).resolve().parents[2]
PACKS = ROOT / "unity-city/Prywatne"
SA = ROOT / "unity-city/Assets/StreamingAssets/Mistrzowie"
OPENSSL_VARIANTS = [
    ["openssl", "enc", "-d", "-aes-256-cbc", "-pbkdf2", "-iter", "200000", "-md", "sha256"],
    ["openssl", "enc", "-d", "-aes-256-cbc", "-pbkdf2", "-iter", "100000", "-md", "sha256"],
    ["openssl", "enc", "-d", "-aes-256-cbc", "-pbkdf2", "-md", "sha256"],
    ["openssl", "enc", "-d", "-aes-256-cbc", "-md", "sha256"],
]
FILMS = {
    "bezprawie.mp4": "5952685b1193f1b3f9f29b319a055e767d45a336fc41766a1db527a99a0656b1",
    "bez_granic_1.mp4": "3c879a1428b12e0e6cbf68ae95b4c4a5b0caf5295c3e8cde0db30bccdea56742",
}
ADS = ["reklama_1_puma.mp4", "reklama_2_alpy.mp4", "reklama_3_roboty.mp4", "reklama_4_gangsterzy.mp4", "reklama_5_fabryka.mp4"]


def sha256(path):
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for block in iter(lambda: f.read(1 << 20), b""):
            h.update(block)
    return h.hexdigest()


def private_key(pack, tmp):
    pem = Path(tmp) / "recipient.pem"
    for variant in OPENSSL_VARIANTS:
        r = subprocess.run(variant + ["-in", str(pack / "recipient.pem.enc"), "-out", str(pem), "-pass", "env:KLUCZ"], capture_output=True)
        if r.returncode == 0 and b"PRIVATE KEY" in pem.read_bytes()[:64]:
            return pem
    return None


def open_pack(name, tmp):
    pack = PACKS / name
    manifest = json.loads((pack / "manifest.json").read_text())
    pem = private_key(pack, tmp)
    if pem is None:
        print("BRAX_OWNER_MEDIA_KEY_FAILED " + name + ": recipient.pem.enc did not open with KLUCZ")
        return None
    cms = Path(tmp) / (name + ".cms")
    with open(cms, "wb") as out:
        for part in manifest["parts"]:
            p = pack / part["name"]
            if not p.exists() or sha256(p) != part["sha256"]:
                print("BRAX_OWNER_MEDIA_PART_BAD " + name + "/" + part["name"])
                return None
            out.write(p.read_bytes())
    tar = Path(tmp) / (name + ".tar")
    r = subprocess.run(["openssl", "cms", "-decrypt", "-binary", "-inform", "DER", "-in", str(cms), "-out", str(tar),
                        "-inkey", str(pem), "-recip", str(pack / "recipient.crt")], capture_output=True)
    cms.unlink()
    if r.returncode != 0:
        print("BRAX_OWNER_MEDIA_DECRYPT_FAILED " + name + ": " + r.stderr.decode(errors="replace")[:200])
        return None
    out = Path(tmp) / name
    with tarfile.open(tar) as t:
        t.extractall(out, filter="data")
    tar.unlink()
    for rel, digest in manifest["files"].items():
        f = out / rel
        if not f.exists() or sha256(f) != digest:
            print("BRAX_OWNER_MEDIA_FILE_BAD " + name + "/" + rel)
            return None
    return out


def main():
    if not os.environ.get("KLUCZ", "").strip():
        print("BRAX_OWNER_MEDIA_SKIPPED: no KLUCZ (the cinema shows BRAK PLIKU FILMU, the TVs their still pictures)")
        return 0
    with tempfile.TemporaryDirectory() as tmp:
        films = open_pack("mistrzowie-kino-filmy-20261111", tmp)
        if films:
            (SA / "Kino").mkdir(parents=True, exist_ok=True)
            for n, d in FILMS.items():
                if sha256(films / n) == d:
                    shutil.copy2(films / n, SA / "Kino" / n); print("BRAX_KINO_FILM_OK " + n)
        ads = open_pack("mistrzowie-reklamy-20261111", tmp)
        if ads:
            (SA / "Reklamy").mkdir(parents=True, exist_ok=True)
            for n in ADS:
                shutil.copy2(ads / "tv_640x360" / n, SA / "Reklamy" / n); print("BRAX_REKLAMA_OK " + n)
    return 0


if __name__ == "__main__":
    sys.exit(main())
