"""Offline regression: encrypted Champions APK checkpoint and recovery.

All credentials and APK data are disposable fixtures; Unity is never launched.
"""
import json
import os
from pathlib import Path
import shutil
import tempfile
import unittest
from unittest.mock import patch

import apk_rescue as rescue


class ChampionsApkRescueTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.root = Path(self.tmp.name)
        self.apk = self.root / "build/Android/braxhorn-mistrzowie.apk"
        self.checkpoint = self.root / "mistrzowie-rescue/raw.json"
        self.cipher = self.root / "mistrzowie-rescue/braxhorn-mistrzowie.apk.enc"
        self.version = self.root / "unity-city/braxhorn.versioncode"
        self.source = self.root / "unity-city/mistrzowie.source-commit"
        self.apk.parent.mkdir(parents=True)
        self.version.parent.mkdir(parents=True)
        self.data = b"APK source bytes fixture - no secrets" * 100
        self.apk.write_bytes(self.data)
        self.apk.with_suffix(".gotowe").write_text("5001")
        self.version.write_text("5001")
        self.source.write_text("a" * 40 + "\n")
        self.env = patch.dict(os.environ, {
            "GITHUB_SHA": "b" * 40, "GITHUB_RUN_ID": "123",
            "GITHUB_RUN_ATTEMPT": "1", "GITHUB_REPOSITORY": "owner/repo",
            "KLUCZ": "disposable-test-secret", "RECOVERY_RUN_ID": "123"
        })
        self.env.start()
        self.addCleanup(self.env.stop)
        for key, value in (
            ("ROOT", self.root), ("RAW_APK", self.apk), ("CHECKPOINT", self.checkpoint),
            ("ENCRYPTED_APK", self.cipher), ("VERSION", self.version),
            ("SOURCE", self.source)
        ):
            mocked = patch.object(rescue, key, value)
            mocked.start()
            self.addCleanup(mocked.stop)

    def test_checkpoint_encrypts_raw_apk_and_verifies_unmodified_bytes(self):
        if not shutil.which("openssl"):
            self.skipTest("openssl unavailable")
        record = rescue.save()
        self.assertTrue(self.cipher.exists())
        self.assertNotIn(self.data, self.cipher.read_bytes())
        self.assertNotEqual(rescue.sha256(self.cipher), rescue.sha256(self.apk))
        self.assertEqual(record["sha256"], rescue.sha256(self.apk))
        self.assertEqual(record["cipherSha256"], rescue.sha256(self.cipher))
        self.assertEqual(record["cipherBytes"], self.cipher.stat().st_size)
        self.assertEqual(record["status"], "QUARANTINED_APK_NOT_VERIFIED_NOT_READY_FOR_INSTALL")

    def test_recovery_restores_identical_apk_and_rejects_altered_archive(self):
        if not shutil.which("openssl"):
            self.skipTest("openssl unavailable")
        record = rescue.save()
        original_index = json.loads(self.checkpoint.read_text())
        cipher = self.cipher.read_bytes()
        self.apk.unlink()
        self.checkpoint.unlink()
        self.cipher.unlink()
        run = dict(path=rescue.WORKFLOW, status="completed", conclusion="failure",
                   head_sha="b" * 40, repository={"full_name": "owner/repo"})
        artifact = dict(artifacts=[dict(
            name="UNVERIFIED-mistrzowie-123-1-" + "a" * 40,
            expired=False, workflow_run={"id": 123, "head_sha": "b" * 40}
        )])

        def download(*args):
            self.assertEqual(args[:3], ("gh", "run", "download"))
            self.cipher.parent.mkdir(parents=True, exist_ok=True)
            self.cipher.write_bytes(cipher)
            self.checkpoint.write_text(json.dumps(original_index))
            return ""

        with patch.object(rescue, "api", side_effect=[run, artifact]), \
             patch.object(rescue, "run_command", side_effect=download):
            recovered = rescue.restore()
        self.assertEqual(self.apk.read_bytes(), self.data)
        self.assertEqual(recovered["sha256"], record["sha256"])

        self.apk.unlink()
        self.checkpoint.unlink()
        self.cipher.unlink()
        altered = bytearray(cipher)
        altered[-1] ^= 1

        def broken_download(*args):
            self.cipher.write_bytes(altered)
            self.checkpoint.write_text(json.dumps(original_index))
            return ""

        with patch.object(rescue, "api", side_effect=[run, artifact]), \
             patch.object(rescue, "run_command", side_effect=broken_download), \
             self.assertRaisesRegex(RuntimeError, "encrypted APK artifact"):
            rescue.restore()
        self.assertFalse(self.apk.exists())


if __name__ == "__main__":
    unittest.main()
