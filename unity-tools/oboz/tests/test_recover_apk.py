import hashlib
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).parents[1]))
import recover_apk as recovery


class RecoveryTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.root = Path(self.tmp.name)
        self.apk = self.root / 'launcher-release.apk'
        self.apk.write_bytes(b'fixture: already signed build bytes')
        self.receipt = self.root / 'receipt.json'
        self.output = self.root / 'recovered'
        self.sha = 'a' * 40
        self.data = dict(applicationId=recovery.PACKAGE, versionCode=215967386,
                         sourceCommit=self.sha, signingCertificateSha256=recovery.CERTIFICATE,
                         apkSha256=hashlib.sha256(self.apk.read_bytes()).hexdigest())
        self.save()
        self.badging = "package: name='com.braxhorn.oboz' versionCode='215967386'"
        self.signing = 'V3.0 Signer: certificate SHA-256 digest: ' + recovery.CERTIFICATE

    def save(self):
        self.receipt.write_text(json.dumps(self.data))

    def run_recovery(self, outputs=None):
        with patch.object(recovery.subprocess, 'check_output', side_effect=outputs or [self.badging, self.signing]):
            return recovery.recover(self.apk, self.receipt, self.output, self.sha, 'aapt', 'apksigner')

    def test_recovers_identical_bytes_and_keeps_phone_unverified(self):
        result = self.run_recovery()
        self.assertEqual((self.output / 'braxhorn-oboz.apk').read_bytes(), self.apk.read_bytes())
        self.assertEqual(result['sourceCommit'], self.sha)
        self.assertFalse(result['phoneTested'])
        self.assertEqual(json.loads(self.receipt.read_text()), self.data)
        self.assertEqual(self.run_recovery()['apkSha256'], result['apkSha256'])

    def test_wrong_build_receipt_is_rejected(self):
        self.data['sourceCommit'] = 'b' * 40
        self.save()
        with self.assertRaises(ValueError): self.run_recovery()
        self.assertFalse(self.output.exists())

    def test_different_cached_apk_is_rejected_before_tools(self):
        self.apk.write_bytes(b'old APK #5')
        with patch.object(recovery.subprocess, 'check_output') as tool:
            with self.assertRaises(ValueError):
                recovery.recover(self.apk, self.receipt, self.output, self.sha, 'aapt', 'apksigner')
            tool.assert_not_called()

    def test_wrong_package_version_or_certificate_is_rejected(self):
        cases = [(self.badging.replace('com.braxhorn.oboz', 'com.braxhorn.city'), self.signing),
                 (self.badging.replace('215967386', '215958998'), self.signing),
                 (self.badging, self.signing.replace(recovery.CERTIFICATE, 'b' * 64))]
        for outputs in cases:
            with self.subTest(outputs=outputs):
                with self.assertRaises(ValueError): self.run_recovery(outputs)
                self.assertFalse(self.output.exists())

    def test_failed_crypto_verification_is_not_treated_as_success(self):
        with self.assertRaises(subprocess.CalledProcessError):
            self.run_recovery([self.badging, subprocess.CalledProcessError(1, 'apksigner')])
        self.assertFalse(self.output.exists())

    def test_missing_apk_and_symlink_are_rejected(self):
        self.apk.unlink()
        with self.assertRaises(ValueError): self.run_recovery()
        other = self.root / 'other.apk'
        other.write_bytes(b'fixture: already signed build bytes')
        self.apk.symlink_to(other)
        with self.assertRaises(ValueError): self.run_recovery()

    def test_existing_other_apk_is_preserved(self):
        self.output.mkdir()
        old = self.output / 'braxhorn-oboz.apk'
        old.write_bytes(b'valuable previous APK')
        with self.assertRaises(ValueError): self.run_recovery()
        self.assertEqual(old.read_bytes(), b'valuable previous APK')


if __name__ == '__main__':
    unittest.main()
