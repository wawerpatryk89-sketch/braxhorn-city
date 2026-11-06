#!/usr/bin/env python3
"""Tiny offline integrity tests; no Unity startup, GitHub writes or confidential keys."""
import hashlib
import io
import sys
from pathlib import Path
import unittest
import zipfile

sys.path.insert(0, str(Path(__file__).resolve().parent))
from import_patryk_extra16 import validate_originals, FIRST_SIX


def sha(b):
    return hashlib.sha256(b).hexdigest()


def package():
    names = [f"Owner Original {i:02}.fbx" for i in range(16)]
    data = {name: b"Kaydara FBX Binary " + name.encode() for name in names}
    b = io.BytesIO()
    with zipfile.ZipFile(b, "w", compression=zipfile.ZIP_DEFLATED) as archive:
        for name, contents in data.items():
            archive.writestr(name, contents)
    raw = b.getvalue()
    approved = {k: sha(v) for k, v in data.items()}
    approved.update({x: sha(x.encode()) for x in FIRST_SIX})
    return raw, {"payload_plain_sha256": sha(raw)}, approved, data


class OwnerPrivateExtra16(unittest.TestCase):
    def test_exact_16_original_fbx_accepted(self):
        raw, metadata, approved, data = package()
        self.assertEqual(validate_originals(raw, metadata, approved), data)

    def test_forged_archive_rejected(self):
        raw, metadata, approved, _ = package()
        corrupted = raw[:-1] + bytes([raw[-1] ^ 1])
        with self.assertRaises(ValueError):
            validate_originals(corrupted, metadata, approved)

    def test_wrong_owner_hash_rejected(self):
        raw, metadata, approved, _ = package()
        first = next(x for x in approved if x not in FIRST_SIX)
        approved[first] = sha(b"Some other FBX")
        with self.assertRaises(ValueError):
            validate_originals(raw, metadata, approved)

    def test_missing_one_fbx_rejected(self):
        raw, metadata, approved, data = package()
        with self.assertRaises(ValueError):
            validate_originals(raw, metadata, {k: v for k, v in approved.items() if k != next(iter(data))})

    def test_plaintext_archive_not_in_repo(self):
        # Contract: import file contains RSA wrapped password + AES ciphertext only,
        # is not the owner's original ZIP and never placed in a public Git tree.
        importer = Path(__file__).with_name("import_patryk_extra16.py").read_text()
        self.assertIn("PRIVATE_KEY_ENCRYPTED", importer)
        self.assertIn('set(package.namelist()) != {"manifest.json", "payload.aes", "password.rsa"}', importer)
        self.assertIn("Path(name).name != name", importer)


if __name__ == "__main__":
    unittest.main()
