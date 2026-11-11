#!/usr/bin/env python3
"""MIASTO R1 — filmy właściciela na telewizory (11.11.2026, właściciel 21:40: „Twój kolega zrobił telewizory, które grają, filmy
odtwarzają, tak samo możesz z nich skorzystać”). Paczka reklam jest ta sama co w Mieście Mistrzów (kolega, install_owner_media.py):

  unity-city/Prywatne/mistrzowie-reklamy-20261111/   tar -> openssl cms -encrypt -aes256 (DER) do recipient.crt, części <= 90 MB,
                                                     SHA-256 w manifest.json; klucz prywatny recipient.pem.enc szyfrowany KLUCZEM

Repo jest publiczne — w gicie tylko paczka zaszyfrowana. W CI (KLUCZ = sekret BRAXHORN_KEYSTORE_PASS) wersje TV 640 × 360 bez
dźwięku trafiają do unity-city/Assets/StreamingAssets/R1/Reklamy (BraxR1ReklamyTV gra je po kolei w pętli). Bez klucza / paczki
build idzie dalej — telewizory pokazują obraz stały. Nigdy nie uruchamia Unity, buildu APK ani innego workflow. Hasło nie trafia
do logów (tylko przez zmienną środowiskową openssl -pass env:KLUCZ).
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
PACK = ROOT / "unity-city/Prywatne/mistrzowie-reklamy-20261111"
OUT = ROOT / "unity-city/Assets/StreamingAssets/R1/Reklamy"
ADS = ["reklama_1_puma.mp4", "reklama_2_alpy.mp4", "reklama_3_roboty.mp4", "reklama_4_gangsterzy.mp4", "reklama_5_fabryka.mp4"]
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


def main():
    if not os.environ.get("KLUCZ", "").strip():
        print("BRAXHORN_R1_TV_POMINIETE: brak KLUCZ — telewizory z obrazem stałym")
        return 0
    if not (PACK / "manifest.json").exists():
        print("BRAXHORN_R1_TV_POMINIETE: brak paczki " + PACK.name)
        return 0
    manifest = json.loads((PACK / "manifest.json").read_text())
    with tempfile.TemporaryDirectory() as tmp:
        tmp = Path(tmp)
        pem = tmp / "recipient.pem"
        for variant in OPENSSL_VARIANTS:
            r = subprocess.run(variant + ["-in", str(PACK / "recipient.pem.enc"), "-out", str(pem), "-pass", "env:KLUCZ"], capture_output=True)
            if r.returncode == 0 and pem.exists() and b"PRIVATE KEY" in pem.read_bytes()[:64]:
                break
        else:
            print("BRAXHORN_R1_TV_KLUCZ_ZLY: recipient.pem.enc nie otworzył się — telewizory z obrazem stałym")
            return 0
        cms = tmp / "reklamy.cms"
        with open(cms, "wb") as out:
            for part in manifest["parts"]:
                p = PACK / part["name"]
                if not p.exists() or sha256(p) != part["sha256"]:
                    print("BRAXHORN_R1_TV_CZESC_ZLA " + part["name"])
                    return 0
                out.write(p.read_bytes())
        tar = tmp / "reklamy.tar"
        r = subprocess.run(["openssl", "cms", "-decrypt", "-binary", "-inform", "DER", "-in", str(cms), "-out", str(tar),
                            "-inkey", str(pem), "-recip", str(PACK / "recipient.crt")], capture_output=True)
        if r.returncode != 0:
            print("BRAXHORN_R1_TV_ODSZYFROWANIE_NIEUDANE")
            return 0
        rozp = tmp / "rozp"
        with tarfile.open(tar) as t:
            t.extractall(rozp, filter="data")
        OUT.mkdir(parents=True, exist_ok=True)
        n = 0
        for nazwa in ADS:
            rel = "tv_640x360/" + nazwa; f = rozp / rel
            if f.exists() and manifest["files"].get(rel) == sha256(f):
                shutil.copy2(f, OUT / nazwa); n += 1; print("BRAXHORN_R1_TV_REKLAMA_OK " + nazwa)
            else:
                print("BRAXHORN_R1_TV_REKLAMA_BRAK " + nazwa)
        print("BRAXHORN_R1_TV reklam: " + str(n))
    return 0


if __name__ == "__main__":
    sys.exit(main())
