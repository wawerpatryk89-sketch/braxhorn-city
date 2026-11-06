#!/usr/bin/env python3
"""Seal the already-uploaded original 22 Patryk FBX for GitHub CI.

Input: exactly the user's 2026-11-06 ZIP, already committed on the estate branch.
Output: encrypted, chunked transfer compatible with import_patryk_owner_22.py.
Never logs or commits plaintext FBX or decrypted keys.
"""
from __future__ import annotations

import hashlib
import json
import lzma
import os
from pathlib import Path
import secrets
import shutil
import subprocess
import tempfile

from import_patryk_owner_22 import (ROOT, OPENSSL, TRANSFER, OWNER_UPLOAD,
                                     verified_files, decrypt_transfer)

PRIVATE_KEY = ROOT / "unity-city/Prywatne/import-patryk-20261103/import-private.pem.enc"
CHUNK = 1024 * 1024


def digest(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def run(args: list[str], *, data: bytes | None = None,
        env: dict[str, str] | None = None) -> bytes:
    return subprocess.run(args, input=data, env=env, check=True,
                          stdout=subprocess.PIPE, stderr=subprocess.PIPE).stdout


def seal(source: Path = OWNER_UPLOAD, destination: Path = TRANSFER) -> dict:
    if not os.environ.get("KLUCZ", "").strip():
        raise RuntimeError("Missing existing KLUCZ CI secret; do not produce unencrypted fallback")
    original = Path(source).read_bytes()
    clips = verified_files(original)  # hard-fails unless EXACT 22 owner FBX and SHA256 match
    if destination.exists():
        if (destination / "manifest.json").is_file():
            existing = decrypt_transfer(destination)
            if existing == original:
                return {"status": "already-encrypted", "clips": len(clips)}
        raise RuntimeError("Encrypted transfer directory exists but does not match the owner's ZIP")
    if not PRIVATE_KEY.is_file():
        raise FileNotFoundError("Missing existing encrypted private key")
    # Key has never been published in cleartext. Decrypt only in ephemeral CI memory.
    with tempfile.TemporaryDirectory(prefix="owner22-seal-") as name:
        temporary = Path(name)
        private_key = temporary / "private.pem"
        public_key = temporary / "public.pem"
        run(OPENSSL + ["-d", "-in", str(PRIVATE_KEY), "-out", str(private_key),
                       "-pass", "env:KLUCZ"])
        private_key.chmod(0o600)
        run(["openssl", "pkey", "-in", str(private_key),
             "-pubout", "-out", str(public_key)])
        password = secrets.token_hex(32)
        wrapped = run(["openssl", "pkeyutl", "-encrypt", "-pubin",
                       "-inkey", str(public_key), "-pkeyopt", "rsa_padding_mode:oaep",
                       "-pkeyopt", "rsa_oaep_md:sha256"], data=password.encode("ascii"))
        compressed = lzma.compress(original, preset=3)
        env = dict(os.environ, PATRYK_OWNER_22_PASSWORD=password)
        encrypted = run(OPENSSL + ["-salt", "-pass", "env:PATRYK_OWNER_22_PASSWORD"],
                        data=compressed, env=env)
        new = temporary / "transfer"
        new.mkdir()
        shutil.copyfile(PRIVATE_KEY, new / "import-private.pem.enc")
        (new / "package-key.rsa").write_bytes(wrapped)
        parts = []
        for i, offset in enumerate(range(0, len(encrypted), CHUNK)):
            raw = encrypted[offset:offset + CHUNK]
            filename = "part-%03d.enc" % i
            (new / filename).write_bytes(raw)
            parts.append({"file": filename, "bytes": len(raw), "sha256": digest(raw)})
        manifest = {
            "schema": 1, "source": "Patryk anima(6).zip",
            "zip_sha256": digest(original), "zip_bytes": len(original),
            "archive_compression": "lzma",
            "cipher": "openssl aes-256-cbc with salt, pbkdf2, sha256, 200000 iterations",
            "package_key": "RSA-OAEP-SHA256, existing CI KLUCZ",
            "chunks": parts,
            "files": {name: digest(data) for name, data in clips.items()},
            "encrypted_from": "owner-upload-on-estate-repair-branch",
        }
        (new / "manifest.json").write_text(
            json.dumps(manifest, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
        # End-to-end test with the SAME decryptor used in the real Unity pipeline.
        restored = decrypt_transfer(new)
        if restored != original or len(verified_files(restored)) != 22:
            raise RuntimeError("Encrypted transfer failed the round-trip owner integrity check")
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.move(str(new), str(destination))
        return {"status": "encrypted-and-verified", "clips": len(clips),
                "encrypted_chunks": len(parts)}


if __name__ == "__main__":
    result = seal()
    print("BRAXHORN_PATRYK_SEAL: " + json.dumps(result, ensure_ascii=False))
