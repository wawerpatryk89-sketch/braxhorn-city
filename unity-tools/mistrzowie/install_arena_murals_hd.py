#!/usr/bin/env python3
"""Stage the three owner-supplied A1 PNGs, without generating or resizing art.

GitHub Release 435656828 contains the EXACT owner upload names
IMG_0266.png (entrance), IMG_0265.png (right belt), IMG_0264.png (left).
Require BOTH the exact release filename and its approved SHA-256; preserve
canonical Unity paths/GUIDs so scene references never need renaming.
This script never starts Unity, an APK build, or another workflow.
"""
from pathlib import Path
import hashlib
import json
import os
import re
import struct
import sys
import urllib.request
import uuid

ROOT = Path(__file__).resolve().parents[2]
DEST = ROOT / "unity-city/Assets/Source/Mistrzowie/ArenaMuralsHD"
OWNER_RELEASE_ID = 435656828
# Filename is the contract with the owner-uploaded GitHub Release, NOT a guess
# made from asset ordering. Canonical Unity names stay unchanged internally.
RELEASE_FILES = {
    "MURAL_WEJSCIE_CHAMPION_FIGHTER_6144x2048.png": "IMG_0266.png",
    "MURAL_LEWA_ZAWODNICY_6144x2048.png": "IMG_0264.png",
    "MURAL_PRAWA_PAS_6144x2048.png": "IMG_0265.png",
}
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
MAX_BYTES = 64 * 1024 * 1024


def guid(rel):
    return uuid.uuid5(NAMESPACE, rel).hex


def png_size(blob):
    if len(blob) < 33 or blob[:8] != b"\x89PNG\r\n\x1a\n" or blob[12:16] != b"IHDR":
        raise ValueError("Invalid PNG header")
    return struct.unpack(">II", blob[16:24])


def verify_blob(name, blob):
    if len(blob) > MAX_BYTES or hashlib.sha256(blob).hexdigest() != FILES[name]:
        raise ValueError("Approved PNG SHA-256 mismatch: " + name)
    if png_size(blob) != SIZE:
        raise ValueError("Approved PNG dimensions mismatch: " + name)


class SafeRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        redirect = super().redirect_request(req, fp, code, msg, headers, newurl)
        if redirect is not None:
            # The signed GitHub storage redirect does not need the API token.
            redirect.remove_header("Authorization")
        return redirect


def api(url, token, accept="application/vnd.github+json"):
    req = urllib.request.Request(url, headers={
        "Authorization": "Bearer " + token, "Accept": accept,
        "X-GitHub-Api-Version": "2022-11-28",
        "User-Agent": "BRAXHORN-Champions-MuralsHD/2.0"})
    with urllib.request.build_opener(SafeRedirect).open(req, timeout=60) as response:
        raw = response.read(MAX_BYTES + 1)
    if len(raw) > MAX_BYTES:
        raise ValueError("Source response exceeds the mural size limit")
    return raw


def select_assets(assets):
    """Find owner uploads by EXACT name, then confirm the expected art SHA-256."""
    if set(RELEASE_FILES) != set(FILES):
        raise ValueError("Owner mural filename mapping does not match Unity targets")
    found = {}
    for target, upload_name in RELEASE_FILES.items():
        matches = [asset for asset in assets
                   if asset.get("name") == upload_name
                   and asset.get("state") == "uploaded"]
        if len(matches) > 1:
            raise ValueError("Duplicate owner mural upload: " + upload_name)
        if not matches:
            continue
        asset = matches[0]
        digest = str(asset.get("digest", "")).removeprefix("sha256:").lower()
        if digest != FILES[target]:
            raise ValueError("Owner mural filename has wrong SHA-256: " + upload_name)
        found[target] = {"id": int(asset["id"]), "name": upload_name,
                         "sha256": FILES[target]}
    return found


def find_assets(token, repo):
    release = json.loads(api(
        f"https://api.github.com/repos/{repo}/releases/{OWNER_RELEASE_ID}", token))
    if release.get("id") != OWNER_RELEASE_ID:
        raise ValueError("Wrong owner mural release")
    return select_assets(release.get("assets", []))


def write_meta(path, folder=False):
    meta = Path(str(path) + ".meta")
    if meta.exists():
        match=re.search(r"^guid:\s*([a-fA-F0-9]{32})\s*$", meta.read_text(encoding="utf-8"), re.M)
        if not match:
            raise ValueError("Existing Unity GUID is invalid: " + str(meta))
        rel = path.relative_to(ROOT / "unity-city").as_posix()
        expected=guid(rel)
        if match.group(1).lower()!=expected:
            raise ValueError("Existing Unity GUID differs from deterministic approved GUID: "+str(meta))
        return  # Do not erase TextureImporter settings or replace an existing GUID.
    rel = path.relative_to(ROOT / "unity-city").as_posix()
    body = "fileFormatVersion: 2\nguid: " + guid(rel) + "\n"
    if folder:
        body += "folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    meta.write_text(body, encoding="utf-8")


def stage(blobs):
    # Validate ALL three before changing any asset. Never leave a mixed-size set.
    if set(blobs) != set(FILES):
        raise ValueError("Exactly three approved mural images are required")
    for name, blob in blobs.items():
        verify_blob(name, blob)
        target = DEST / name
        if target.exists() and target.read_bytes() != blob:
            raise ValueError("Refuse to replace an edited owner source: " + name)
        meta = Path(str(target) + ".meta")
        if meta.exists():
            match=re.search(r"^guid:\s*([a-fA-F0-9]{32})\s*$",meta.read_text(encoding="utf-8"),re.M)
            expected_guid=guid(target.relative_to(ROOT/"unity-city").as_posix())
            if not match or match.group(1).lower()!=expected_guid:
                raise ValueError("Existing Unity GUID mismatch for approved mural: "+name)
    DEST.mkdir(parents=True, exist_ok=True)
    write_meta(DEST, folder=True)
    for name, blob in blobs.items():
        target = DEST / name
        if not target.exists():
            temporary = target.with_suffix(target.suffix + ".partial")
            try:
                temporary.write_bytes(blob)
                os.replace(temporary, target)
            finally:
                temporary.unlink(missing_ok=True)
        write_meta(target)


def main():
    report = {"status": "HD_NOT_STAGED", "expected": 3, "staged": 0,
              "releaseId": OWNER_RELEASE_ID, "releaseNames": dict(RELEASE_FILES),
              "unityTested": False, "images": []}
    try:
        blobs = {}
        for name in FILES:
            target = DEST / name
            if target.is_file():
                raw = target.read_bytes()
                verify_blob(name, raw)
                blobs[name] = raw
        if len(blobs) != len(FILES):
            token = os.environ.get("GH_TOKEN")
            if not token:
                raise ValueError("GH_TOKEN missing and not all three approved PNGs are staged")
            repo = os.environ.get("GITHUB_REPOSITORY", "wawerpatryk89-sketch/braxhorn-city")
            if not re.fullmatch(r"[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+", repo):
                raise ValueError("Invalid repository name")
            found = find_assets(token, repo)
            for name in FILES:
                if name in blobs:
                    continue
                if name not in found:
                    raise ValueError("Owner mural not found under exact release filename: " + RELEASE_FILES[name])
                asset = found[name]
                blobs[name] = api(f"https://api.github.com/repos/{repo}/releases/assets/{asset['id']}",
                                  token, accept="application/octet-stream")
                report["images"].append({"target": name, **asset})
        stage(blobs)
        report.update(status="THREE_OWNER_HD_PNGS_STAGED", staged=3)
        report["verifiedSha256"] = dict(FILES)
        print("BRAX_MURALE_HD_RESULT 3/3 bytes_verified=true source_pixels_unchanged=true "
              + " ".join(RELEASE_FILES[target] + "->" + target for target in FILES))
    except Exception as error:
        report["error"] = str(error)
        print("BRAX_MURALE_HD_ERROR", report["error"], "; exact 3/3 owner HD murals are mandatory")
        evidence = ROOT / "mistrzowie-evidence/arena-murals-hd.json"
        evidence.parent.mkdir(parents=True, exist_ok=True)
        evidence.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        return 1
    evidence = ROOT / "mistrzowie-evidence/arena-murals-hd.json"
    evidence.parent.mkdir(parents=True, exist_ok=True)
    evidence.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    return 0


if __name__ == "__main__":
    sys.exit(main())
