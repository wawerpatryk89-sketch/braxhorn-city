import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).parents[1]))
import publish_apk as publisher


class PublishTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.root = Path(self.tmp.name)
        self.folder = self.root / 'apk'
        self.folder.mkdir()
        self.apk = self.folder / 'braxhorn-oboz.apk'
        self.apk.write_bytes(b'new verified APK')
        self.dialogue = self.root / 'camp-dialogue.zip'
        self.dialogue.write_bytes(b'verified audio')
        self.world = self.root / 'oboz-world.tar.gz.enc'
        self.world.write_bytes(b'encrypted world')
        self.sha = 'a' * 40
        self.data = dict(applicationId=publisher.PACKAGE, versionCode=215970000,
                         sourceCommit=self.sha, signingCertificateSha256=publisher.CERTIFICATE,
                         apkSha256=publisher.digest(self.apk))
        self.receipt = self.folder / 'release-identity.json'
        self.receipt.write_text(json.dumps(self.data))
        for name in ('build-receipt', 'scene-validation'):
            (self.folder / (name + '.json')).write_text('{}')

    def publish(self, draft=True, assets=(), fail_upload=False):
        commands = []
        def run(cmd, **kwargs):
            commands.append(cmd)
            if fail_upload: raise subprocess.CalledProcessError(1, cmd)
            for file in cmd[cmd.index('--repo')+2:]:
                if file != '--clobber': self.assertTrue(Path(file).is_file())
        with patch.object(publisher.subprocess, 'check_output', return_value=json.dumps(dict(isDraft=draft, assets=list(assets)))), \
                patch.object(publisher.subprocess, 'run', side_effect=run):
            data = publisher.publish(self.folder, self.dialogue, self.world, 'owner/repo', self.sha)
        return data, commands

    def test_assets_are_immutable_and_pointer_moves_last_without_create(self):
        data, commands = self.publish()
        self.assertEqual(len(commands), 2)
        self.assertTrue(all(cmd[1:3] == ['release', 'upload'] for cmd in commands))
        self.assertNotIn('--clobber', commands[0])
        self.assertIn('--clobber', commands[1])
        self.assertTrue(commands[1][-1].endswith('/release-identity.json'))
        self.assertEqual(data['apkAssetName'], 'braxhorn-oboz-215970000-aaaaaaa.apk')
        self.assertEqual(data['releaseTag'], 'oboz-review-latest')
        self.assertFalse(data['phoneTested'])

    def test_failed_upload_keeps_previous_pointer_data(self):
        before = self.receipt.read_bytes()
        with self.assertRaises(subprocess.CalledProcessError): self.publish(fail_upload=True)
        self.assertEqual(self.receipt.read_bytes(), before)

    def test_collision_never_overwrites_existing_version(self):
        with self.assertRaises(ValueError):
            self.publish(assets=[{'name': 'braxhorn-oboz-215970000-aaaaaaa.apk'}])

    def test_public_release_is_rejected(self):
        with self.assertRaises(ValueError): self.publish(draft=False)

    def test_mutated_apk_is_rejected(self):
        self.apk.write_bytes(b'different APK')
        with self.assertRaises(ValueError): self.publish()

    def test_missing_world_does_not_claim_checkpoint(self):
        self.world.unlink()
        data, _ = self.publish()
        self.assertNotIn('worldSha256', data)
        self.assertNotIn('worldAssetName', data)

    def test_incorrect_identity_is_rejected(self):
        for key, value in [('sourceCommit', 'b'*40), ('applicationId', 'other.app'),
                           ('signingCertificateSha256', 'b'*64)]:
            with self.subTest(key=key):
                self.receipt.write_text(json.dumps(dict(self.data, **{key: value})))
                with self.assertRaises(ValueError): self.publish()


if __name__ == '__main__': unittest.main()
