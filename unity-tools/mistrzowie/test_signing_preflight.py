"""Exercise real keytool against disposable test keys; never repository secrets."""
import contextlib
import hashlib
import io
import json
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest
from unittest.mock import patch

import apk_delivery as delivery


@unittest.skipUnless(shutil.which('keytool'), 'keytool unavailable')
class SigningPreflightTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.tmp = tempfile.TemporaryDirectory()
        cls.addClassCleanup(cls.tmp.cleanup)
        cls.base = Path(cls.tmp.name)/'test.jks'
        subprocess.run(['keytool','-genkeypair','-alias','braxhorn','-keystore',str(cls.base),
                        '-storetype','JKS','-storepass','temporary-test-key','-keypass','temporary-test-key',
                        '-keyalg','RSA','-keysize','2048','-validity','1','-dname','CN=Disposable CI fixture'],
                       check=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)

    def setUp(self):
        tmp = tempfile.TemporaryDirectory()
        self.addCleanup(tmp.cleanup)
        self.root = Path(tmp.name)
        project = self.root/'unity-city'; project.mkdir()
        self.key = project/'braxhorn.keystore'; shutil.copyfile(self.base, self.key)
        self.password = project/'braxhorn.keystore.pass'
        self.password.write_text('temporary-test-key')
        p = patch.object(delivery, 'ROOT', self.root); p.start(); self.addCleanup(p.stop)

    def run_preflight(self):
        # Android binaries are absent locally; keytool stays real.
        original = delivery.command
        def command(*args, **kwargs):
            if args[0] in ('test-aapt', 'test-apksigner'):
                self.assertEqual(args[1:], ('version',))
                return 'test tool version\n'
            return original(*args, **kwargs)
        with patch.object(delivery,'sdk_tool',side_effect=lambda n:'test-'+n), \
             patch.object(delivery,'command',side_effect=command), contextlib.redirect_stdout(io.StringIO()):
            delivery.preflight()

    def test_private_key_is_usable_and_keystore_bytes_unchanged(self):
        before = hashlib.sha256(self.key.read_bytes()).hexdigest()
        self.run_preflight()
        report = json.loads((self.root/'mistrzowie-evidence/signing-preflight.json').read_text())
        self.assertEqual(report['certificateSha256'], delivery.certificate_sha256())
        self.assertEqual(before, hashlib.sha256(self.key.read_bytes()).hexdigest())
        self.assertIn('APK_NOT_BUILT', report['status'])

    def test_wrong_password_is_rejected_before_unity(self):
        self.password.write_text('wrong-test-password')
        with self.assertRaises(subprocess.CalledProcessError):
            self.run_preflight()
        self.assertFalse((self.root/'mistrzowie-evidence/signing-preflight.json').exists())

    def test_missing_alias_is_rejected(self):
        subprocess.run(['keytool','-changealias','-alias','braxhorn','-destalias','different',
                        '-keystore',str(self.key),'-storepass','temporary-test-key','-keypass','temporary-test-key'],
                       check=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        with self.assertRaises(subprocess.CalledProcessError):
            self.run_preflight()

    def test_certificate_without_private_key_is_rejected(self):
        cert = self.root/'public.der'
        cert.write_bytes(delivery.command('keytool','-exportcert','-alias','braxhorn',
            '-keystore',str(self.key),'-storepass:file',str(self.password),binary=True))
        self.key.unlink()  # This is the disposable fixture, not a repository key.
        subprocess.run(['keytool','-importcert','-noprompt','-alias','braxhorn','-keystore',str(self.key),
                        '-storepass','temporary-test-key','-file',str(cert)],
                       check=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        with self.assertRaises(subprocess.CalledProcessError):
            self.run_preflight()

    def test_missing_android_tool_is_reported_before_key_use(self):
        with patch.object(delivery,'sdk_tool',side_effect=RuntimeError('Android SDK tool unavailable')), \
             patch.object(delivery,'command') as command, self.assertRaisesRegex(RuntimeError,'SDK tool'):
            delivery.preflight()
        command.assert_not_called()


if __name__ == '__main__':
    unittest.main()
