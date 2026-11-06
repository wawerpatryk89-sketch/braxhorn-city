#!/usr/bin/env python3
"""Restore exactly 16 additional owner's Patryk FBX files to Unity from ONE encrypted archive.

Upload only the encrypted package to unity-city/Prywatne/. Never upload plaintext
FBX files or the owner's archive to the public GitHub repository.
This script is used by the existing private-assets preparation step before Unity.
"""
from __future__ import annotations

import hashlib
import io
import json
import os
from pathlib import Path
import subprocess
import tempfile
import zipfile

ROOT = Path(__file__).resolve().parents[2]
BUNDLE = ROOT / "unity-city/Prywatne/BRAXHORN_PATRYK_EXTRA16_ENCRYPTED_20261106.zip"
SHORT_BUNDLE = ROOT / "unity-city/Prywatne/Patryk16.zip"
PRIVATE_KEY_ENCRYPTED = ROOT / "unity-city/Prywatne/import-patryk-20261103/import-private.pem.enc"
ORIGINALS_MANIFEST = ROOT / "unity-tools/claude/patryk-owner-22-manifest.json"
BUNDLE_SHA256 = "8c6b367f6e780780bd20b008b92b24a6c43ee11b282b68314198d06dd6f542e3"
OPENSSL = ["openssl", "enc", "-aes-256-cbc", "-pbkdf2", "-iter", "200000", "-md", "sha256"]
FIRST_SIX = {
    "Entering Car.fbx", "Fast Run.fbx", "Goalkeeper Catch.fbx",
    "Goalkeeper Diving Save.fbx", "Jump.fbx", "Strut Walking.fbx",
}


def sha256(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def _run(args: list[str], *, input_data: bytes | None = None, env: dict | None = None) -> bytes:
    # Never write decrypted ZIP, password, or private key to logs.
    process = subprocess.run(args, input=input_data, capture_output=True, check=True, env=env)
    return process.stdout


def validate_originals(raw: bytes, manifest: dict, approved: dict) -> dict[str, bytes]:
    if sha256(raw) != manifest["payload_plain_sha256"]:
        raise ValueError("Additional Patryk archive differs from owner's original upload")
    expected = set(approved).difference(FIRST_SIX)
    result: dict[str, bytes] = {}
    with zipfile.ZipFile(io.BytesIO(raw)) as z:
        names = z.namelist()
        if len(names) != 16 or len(set(names)) != 16 or set(names) != expected:
            raise ValueError("Patryk: required 16 FBX not present in private package")
        for name in names:
            if Path(name).name != name or not name.endswith(".fbx"):
                raise ValueError("Unexpected path in owner's Patryk transfer")
            data = z.read(name)
            if not data.startswith(b"Kaydara FBX Binary") or sha256(data) != approved[name]:
                raise ValueError("Patryk FBX bytes differ from the user's latest 22-file archive: " + name)
            result[name] = data
    return result


def install(destination: Path, bundle: Path = BUNDLE) -> list[Path]:
    # The same verified encrypted archive is available with a short iPhone-friendly
    # file name. Both names are byte-identical, verified by the same SHA256 below.
    if not bundle.exists() and bundle == BUNDLE and SHORT_BUNDLE.exists():
        bundle = SHORT_BUNDLE
    if not bundle.exists():
        raise FileNotFoundError(
            "Patryk: missing encrypted ORIGINAL 16 FBX package. Upload "
            "Patryk16.zip (or BRAXHORN_PATRYK_EXTRA16_ENCRYPTED_20261106.zip) "
            "to unity-city/Prywatne/ on the estate repair branch. "
            "Do not substitute old/cross-character clips."
        )
    if sha256(bundle.read_bytes()) != BUNDLE_SHA256:
        raise ValueError("Patryk: encrypted import package is changed/corrupted")
    if not os.environ.get("KLUCZ", "").strip():
        raise RuntimeError("Patryk owner-only import needs the existing KLUCZ CI secret")

    approved_manifest = json.loads(ORIGINALS_MANIFEST.read_text(encoding="utf-8"))
    approved = {entry["file"]: entry["sha256"] for entry in approved_manifest["clips"]}
    if len(approved) != 22:
        raise ValueError("Patryk: exactly 22 original animation hashes are required")

    with zipfile.ZipFile(bundle) as package:
        if set(package.namelist()) != {"manifest.json", "payload.aes", "password.rsa"}:
            raise ValueError("Patryk: unexpected encrypted transfer entries")
        manifest = json.loads(package.read("manifest.json"))
        encrypted = package.read("payload.aes")
        wrapped_password = package.read("password.rsa")
    if manifest.get("format") != "patryk-extra16-private-v1" or manifest.get("additional_clip_count") != 16:
        raise ValueError("Patryk: invalid encrypted transfer metadata")
    if manifest.get("source_sha256") != approved_manifest["animation_archive"]["sha256"]:
        raise ValueError("Patryk: package does not come from the latest supplied archive")
    if sha256(encrypted) != manifest["payload_encrypted_sha256"] or len(encrypted) != manifest["payload_encrypted_bytes"]:
        raise ValueError("Patryk: AES encrypted payload integrity check failed")
    if sha256(wrapped_password) != manifest["wrapped_key_sha256"] or len(wrapped_password) != manifest["wrapped_key_bytes"]:
        raise ValueError("Patryk: RSA wrapped password integrity check failed")

    with tempfile.TemporaryDirectory(prefix="patryk-extra16-") as tmp:
        key_path = Path(tmp) / "private.pem"
        _run(OPENSSL + ["-d", "-in", str(PRIVATE_KEY_ENCRYPTED),
            "-out", str(key_path), "-pass", "env:KLUCZ"])
        key_path.chmod(0o600)
        password = _run(["openssl", "pkeyutl", "-decrypt",
            "-inkey", str(key_path), "-pkeyopt", "rsa_padding_mode:oaep",
            "-pkeyopt", "rsa_oaep_md:sha256"], input_data=wrapped_password).decode("ascii")
        env = dict(os.environ, PATRYK_EXTRA16_PASSWORD=password)
        raw = _run(OPENSSL + ["-d", "-pass", "env:PATRYK_EXTRA16_PASSWORD"],
                   input_data=encrypted, env=env)
        originals = validate_originals(raw, manifest, approved)

    target_folder = Path(destination) / "patryk_owner"
    target_folder.mkdir(parents=True, exist_ok=True)
    installed: list[Path] = []
    for name, data in sorted(originals.items()):
        target = target_folder / name
        if target.exists() and sha256(target.read_bytes()) == sha256(data):
            installed.append(target)
            continue
        with tempfile.NamedTemporaryFile(dir=target_folder, delete=False) as stream:
            temporary = Path(stream.name)
            stream.write(data)
        temporary.replace(target)
        installed.append(target)
    print("BRAXHORN_PATRYK_OWNER_22: imported 16 verified additional source FBX, no substitutes")
    return installed


if __name__ == "__main__":
    import argparse
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--destination", type=Path,
        default=ROOT / "unity-city/Assets/Source/Prywatne/ruchy")
    install(parser.parse_args().destination)
