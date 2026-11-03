"""Synthetic encrypted transfer tests; no owner assets or production keys required."""
import hashlib
import io
import json
import os
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest.mock import patch
import zipfile

import import_patryk_transfer as transfer
import prywatne


class TransferTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.root = Path(self.tmp.name)
        self.env = patch.dict(os.environ, KLUCZ="synthetic-test-only", PATRYK_TRANSFER_PASSWORD="synthetic-package-only")
        self.env.start()
        self.addCleanup(self.env.stop)
        self.run_ssl(["genpkey", "-algorithm", "RSA", "-pkeyopt", "rsa_keygen_bits:2048", "-out", str(self.root / "key.pem")])
        self.run_ssl(["pkey", "-in", str(self.root / "key.pem"), "-pubout", "-out", str(self.root / "public.pem")])
        subprocess.run(prywatne.OPENSSL + ["-in", str(self.root / "key.pem"), "-out", str(self.root / "import-private.pem.enc"), "-pass", "env:KLUCZ"], check=True, capture_output=True)
        wrapped = self.run_ssl(["pkeyutl", "-encrypt", "-pubin", "-inkey", str(self.root / "public.pem"), "-pkeyopt", "rsa_padding_mode:oaep", "-pkeyopt", "rsa_oaep_md:sha256"], b"synthetic-package-only")
        (self.root / "package-key.rsa").write_bytes(wrapped)

    @staticmethod
    def run_ssl(args, data=None):
        return subprocess.run(["openssl"] + args, input=data, check=True, capture_output=True).stdout

    def make_package(self, name="model/test.fbx"):
        buf = io.BytesIO()
        with zipfile.ZipFile(buf, "w") as z:
            z.writestr(name, b"synthetic FBX fixture" * 60)
        data = buf.getvalue()
        self.sha = hashlib.sha256(data).hexdigest()
        chunks = []
        for i, start in enumerate(range(0, len(data), 256)):
            encrypted = subprocess.run(prywatne.OPENSSL + ["-salt", "-pass", "env:PATRYK_TRANSFER_PASSWORD"], input=data[start:start + 256], check=True, capture_output=True).stdout
            filename = f"part-{i:03d}.enc"
            (self.root / filename).write_bytes(encrypted)
            chunks.append({"file": filename, "bytes": len(encrypted), "sha256": hashlib.sha256(encrypted).hexdigest()})
        self.manifest = {"zip_sha256": self.sha, "zip_bytes": len(data), "chunks": chunks}
        self.save_manifest()

    def save_manifest(self):
        (self.root / "manifest.json").write_text(json.dumps(self.manifest))

    def restore(self):
        with patch.object(transfer, "ZIP_SHA256", self.sha):
            transfer.restore(self.root)

    def test_roundtrip_reencrypts_original_bytes(self):
        self.make_package()
        files = {}
        def collect(name, source):
            self.assertEqual(name, "patryk")
            files.update({str(p.relative_to(source)): p.read_bytes() for p in source.rglob("*") if p.is_file()})
        with patch.object(prywatne, "szyfruj", side_effect=collect):
            self.restore()
        self.assertEqual(files, {"patryk/model/test.fbx": b"synthetic FBX fixture" * 60})

    def test_damaged_chunk_is_rejected(self):
        self.make_package()
        p = self.root / "part-001.enc"
        p.write_bytes(p.read_bytes()[:-1] + b"!")
        with self.assertRaisesRegex(ValueError, "Uszkodzona"):
            self.restore()

    def test_incorrect_archive_size_is_rejected(self):
        self.make_package()
        self.manifest["zip_bytes"] += 1
        self.save_manifest()
        with self.assertRaisesRegex(ValueError, "Suma całej"):
            self.restore()

    def test_outside_archive_path_is_rejected(self):
        self.make_package("../outside.fbx")
        with self.assertRaisesRegex(ValueError, "Ścieżka poza"):
            self.restore()

    def test_city_does_not_restore_estate_replacement(self):
        with patch.dict(os.environ, WEJSCIE_REJON="miasto"), patch.object(transfer, "restore") as restore, \
             patch.object(prywatne, "odszyfruj", return_value=None), \
             patch.object(prywatne, "przygotuj_patryka_miasto", return_value=False) as city, \
             patch.object(prywatne, "przygotuj_patryka") as estate:
            prywatne.patryk_i_ruchy()
        restore.assert_not_called()
        estate.assert_not_called()
        city.assert_called_once()


if __name__ == "__main__":
    unittest.main()
