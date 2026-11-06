#!/usr/bin/env python3
"""Murale areny A1 w HD (6144x2048): oryginały właściciela powiększone 4x
(Real-ESRGAN realesr-general-x4v3, bez dorysowywania), 6.11.2026.

Pliki leżą jako zasoby wydania GitHub (np. szkic champions-a1-20261105),
NIE w publicznym repo. Skrypt szuka ich po nazwie we wszystkich wydaniach,
sprawdza SHA-256 i wymiary PNG, kopiuje do unity-city/Assets/Source/
Mistrzowie/ArenaMuralsHD/ ze stałymi GUID.

Brak plików / tokenu / sieci = ostrzeżenie i kod 0: arena użyje starych
murali 1536x512, build idzie dalej.
"""
from pathlib import Path
import hashlib
import json
import os
import struct
import sys
import urllib.request
import uuid

ROOT = Path(__file__).resolve().parents[2]
DEST = ROOT / "unity-city/Assets/Source/Mistrzowie/ArenaMuralsHD"
FILES = {
    "MURAL_WEJSCIE_CHAMPION_FIGHTER_6144x2048.png":
        "5d30aa995bb0da043a34c282fb452743f1b04b9f4584abf2be925ea29a461a73",
    "MURAL_LEWA_ZAWODNICY_6144x2048.png":
        "0805d4170b696236dba7a37c89fc4415ee21a87b316c1d58734bac212cb11a2c",
    "MURAL_PRAWA_PAS_6144x2048.png":
        "b134858b5ba16dc85089642eb1f7432ef5c76e036a8d313805947923b7661304",
}
SIZE = (6144, 2048)
NAMESPACE = uuid.UUID("6f1c2a9e-3b7d-4c55-9a0e-b4a1d2c3e6f7")


def guid(rel):
    return uuid.uuid5(NAMESPACE, rel).hex


def png_size(blob):
    if blob[:8] != b"\x89PNG\r\n\x1a\n" or blob[12:16] != b"IHDR":
        raise ValueError("to nie jest PNG")
    return struct.unpack(">II", blob[16:24])


def api(url, token, accept="application/vnd.github+json"):
    req = urllib.request.Request(url, headers={
        "Authorization": "Bearer " + token, "Accept": accept,
        "X-GitHub-Api-Version": "2022-11-28",
        "User-Agent": "BRAXHORN-Champions-MuralsHD/1.0"})
    with urllib.request.urlopen(req, timeout=300) as r:
        return r.read()


def find_assets(token, repo):
    found = {}
    for page in range(1, 6):
        releases = json.loads(api(
            f"https://api.github.com/repos/{repo}/releases?per_page=100&page={page}", token))
        if not releases:
            break
        for rel in releases:
            for a in rel.get("assets", []):
                if a["name"] in FILES and a["name"] not in found:
                    found[a["name"]] = a["id"]
    return found


def write_meta(path, folder=False):
    rel = path.relative_to(ROOT / "unity-city").as_posix()
    meta = Path(str(path) + ".meta")
    body = "fileFormatVersion: 2\nguid: " + guid(rel) + "\n"
    if folder:
        body += "folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    meta.write_text(body, encoding="utf-8")


def main():
    token = os.environ.get("GH_TOKEN")
    repo = os.environ.get("GITHUB_REPOSITORY", "wawerpatryk89-sketch/braxhorn-city")
    if not token:
        print("BRAX_MURALE_HD_SKIP brak GH_TOKEN; arena użyje murali 1536x512")
        return 0
    try:
        found = find_assets(token, repo)
    except Exception as e:  # sieć/API: nie blokujemy buildu
        print("BRAX_MURALE_HD_SKIP błąd API:", e)
        return 0
    missing = [n for n in FILES if n not in found]
    if missing:
        print("BRAX_MURALE_HD_SKIP brak w wydaniach:", ", ".join(missing),
              "; arena użyje murali 1536x512")
        return 0
    DEST.mkdir(parents=True, exist_ok=True)
    write_meta(DEST, folder=True)
    ok = 0
    for name, digest in FILES.items():
        try:
            blob = api(f"https://api.github.com/repos/{repo}/releases/assets/{found[name]}",
                       token, accept="application/octet-stream")
            if hashlib.sha256(blob).hexdigest() != digest:
                raise ValueError("inna suma SHA-256")
            if png_size(blob) != SIZE:
                raise ValueError("wymiary %sx%s zamiast 6144x2048" % png_size(blob))
            target = DEST / name
            target.write_bytes(blob)
            write_meta(target)
            ok += 1
            print("BRAX_MURALE_HD_OK", name, len(blob))
        except Exception as e:
            print("BRAX_MURALE_HD_SKIP", name, e)
    print(f"BRAX_MURALE_HD_RESULT {ok}/3")
    return 0


if __name__ == "__main__":
    sys.exit(main())
