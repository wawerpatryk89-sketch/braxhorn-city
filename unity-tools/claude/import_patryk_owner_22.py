#!/usr/bin/env python3
"""Import ONLY the user's verified Patryk anima(6).zip (22 Mixamo FBX).

CI: RSA-wrapped, AES-encrypted transfer at unity-city/Prywatne/import-patryk-animations-20261106.
Local validation: --owner-zip path/to/"Patryk anima(6).zip".
The original files are never modified. No other character's animations are used.
"""
import argparse
import hashlib
import io
import json
import lzma
import os
from pathlib import Path
import subprocess
import tempfile
import zipfile

ROOT = Path(__file__).resolve().parents[2]
CATALOG = ROOT / "unity-tools/claude/patryk-owner-22-manifest.json"
TRANSFER = ROOT / "unity-city/Prywatne/import-patryk-animations-20261106"
DESTINATION = ROOT / "unity-city/Assets/Source/Prywatne/ruchy"
OWNER_UPLOAD = ROOT / "unity-city/Prywatne/Patryk_22_oryginalne_ruchy.zip"  # original owner upload; remove after encrypted transfer
OPENSSL = ["openssl", "enc", "-aes-256-cbc", "-pbkdf2", "-iter", "200000", "-md", "sha256"]


def digest(raw):
    return hashlib.sha256(raw).hexdigest()


def verified_files(raw):
    owner = json.loads(CATALOG.read_text(encoding="utf-8"))
    assert owner["animation_archive"]["name"] == "Patryk anima(6).zip"
    assert len(raw) > 0 and digest(raw) == owner["animation_archive"]["sha256"], "Wrong Patryk archive (not last user upload)"
    expected = {entry["file"]: entry["sha256"] for entry in owner["clips"]}
    if len(expected) != 22:
        raise ValueError("Original owner catalog must have exactly 22 FBX")
    with zipfile.ZipFile(io.BytesIO(raw)) as archive:
        actual = archive.namelist()
        if len(actual) != 22 or len(set(actual)) != 22 or set(actual) != set(expected):
            raise ValueError("Patryk ZIP missing original 22 FBX or includes unexpected files")
        files = {}
        for name in actual:
            binary = archive.read(name)
            if not binary.startswith(b"Kaydara FBX Binary") or digest(binary) != expected[name]:
                raise ValueError("FBX changed or not from last user upload: " + name)
            files[name] = binary
    return files


def decrypt_transfer(folder):
    folder = Path(folder)
    manifest_path = folder / "manifest.json"
    if not manifest_path.is_file():
        raise FileNotFoundError("Required encrypted transfer of all 22 FBX missing: " + str(manifest_path))
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    reference = json.loads(CATALOG.read_text(encoding="utf-8"))
    if manifest["zip_sha256"] != reference["animation_archive"]["sha256"]:
        raise ValueError("Encrypted archive does not match user-supplied Patryk anima(6).zip")
    if manifest["source"] != "Patryk anima(6).zip" or manifest["archive_compression"] != "lzma":
        raise ValueError("Encrypted transfer contains wrong original source or compression")
    encrypted = bytearray()
    chunks = manifest["chunks"]
    for i, part in enumerate(chunks):
        filename = "part-%03d.enc" % i
        if part["file"] != filename:
            raise ValueError("Out-of-order encrypted chunk")
        content = (folder / filename).read_bytes()
        if len(content) != part["bytes"] or digest(content) != part["sha256"]:
            raise ValueError("Damaged encrypted chunk: " + filename)
        encrypted.extend(content)
    if not os.environ.get("KLUCZ", "").strip():
        raise RuntimeError("CI KLUCZ required to decrypt the user's exact 22 FBX")
    with tempfile.TemporaryDirectory(prefix="patryk-owner22-decrypt-") as temp:
        key = Path(temp) / "key.pem"
        subprocess.run(OPENSSL + [
            "-d", "-in", str(folder / "import-private.pem.enc"),
            "-out", str(key), "-pass", "env:KLUCZ",
        ], check=True, capture_output=True)
        key.chmod(0o600)
        password = subprocess.run([
            "openssl", "pkeyutl", "-decrypt", "-inkey", str(key),
            "-in", str(folder / "package-key.rsa"),
            "-pkeyopt", "rsa_padding_mode:oaep",
            "-pkeyopt", "rsa_oaep_md:sha256",
        ], check=True, capture_output=True).stdout.decode("ascii")
        env = dict(os.environ, PATRYK_OWNER_22_PASSWORD=password)
        compressed = subprocess.run(
            OPENSSL + ["-d", "-pass", "env:PATRYK_OWNER_22_PASSWORD"],
            input=encrypted, capture_output=True, check=True, env=env
        ).stdout
    raw = lzma.decompress(compressed)
    if len(raw) != manifest["zip_bytes"] or digest(raw) != manifest["zip_sha256"]:
        raise ValueError("Decrypted ZIP does not match original owner archive")
    return raw


def install(destination=DESTINATION, transfer=TRANSFER, owner_zip=None):
    # Only the latest owner's 22-byte-identical FBX. The verified original
    # uploaded to GitHub is temporarily accepted until CI seals it to the
    # encrypted transfer. Never use old six-clip packages or borrowed motions.
    if owner_zip is not None:
        raw = Path(owner_zip).read_bytes()
    elif Path(transfer, "manifest.json").is_file():
        raw = decrypt_transfer(transfer)
    elif OWNER_UPLOAD.is_file():
        raw = OWNER_UPLOAD.read_bytes()
        print("BRAXHORN_PATRYK_OWNER_22: WARNING raw user upload present in PUBLIC repo; seal via dedicated workflow")
    else:
        raise FileNotFoundError("Patryk: no verified encrypted 22-FBX transfer or latest exact owner ZIP")
    verified = verified_files(raw)
    destination = Path(destination) / "patryk_owner"
    destination.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="patryk-owner22-stage-") as tempdir:
        temp = Path(tempdir)
        for name, content in verified.items():
            (temp / name).write_bytes(content)
        # Validate everything before replacing files. Existing Unity .meta GUIDs survive.
        for name, expected in ((n, digest(b)) for n, b in verified.items()):
            if digest((temp / name).read_bytes()) != expected:
                raise ValueError("Staging corruption: " + name)
        for stale in destination.glob("*.fbx"):
            if stale.name not in verified:
                stale.unlink()
                # Remove only an unused importer's .meta, never a source archive.
                meta = Path(str(stale) + ".meta")
                if meta.exists():
                    meta.unlink()
        for name in sorted(verified):
            (temp / name).replace(destination / name)
    if len(list(destination.glob("*.fbx"))) != 22:
        raise ValueError("Owner FBX installation incomplete")
    print("BRAXHORN_PATRYK_OWNER_22: PASS; exactly 22 original FBX from Patryk anima(6).zip; no borrowed motions")
    return sorted(destination.glob("*.fbx"))


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--owner-zip", type=Path, help="Exact uploaded original ZIP for local validation only")
    parser.add_argument("--destination", type=Path, default=DESTINATION)
    parser.add_argument("--transfer", type=Path, default=TRANSFER)
    args = parser.parse_args()
    install(destination=args.destination, owner_zip=args.owner_zip, transfer=args.transfer)
