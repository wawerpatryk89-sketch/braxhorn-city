"""Offline delivery regressions. No GitHub request, Unity or production key."""
import copy
import hashlib
import io
import json
import os
from pathlib import Path
import shutil
import sys
import tarfile
import tempfile
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import assets
import motion_candidates as motion
import transfer


class MotionDeliveryTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.temp = tempfile.TemporaryDirectory()
        cls.root = Path(cls.temp.name)
        cls.key, cls.cert = cls.root/'key.pem', cls.root/'recipient.crt'
        motion.run(['openssl', 'req', '-x509', '-newkey', 'rsa:2048', '-nodes',
                    '-keyout', str(cls.key), '-out', str(cls.cert), '-days', '1',
                    '-subj', '/CN=Offline motion test'])

    @classmethod
    def tearDownClass(cls):
        cls.temp.cleanup()

    def setUp(self):
        temp = tempfile.TemporaryDirectory()
        self.addCleanup(temp.cleanup)
        self.folder = Path(temp.name)
        self.private = self.folder/'private'
        self.private.mkdir()
        self.env = patch.dict(os.environ, {'KLUCZ': 'offline-fixture', 'GITHUB_REPOSITORY': 'test/camp'})
        self.env.start(); self.addCleanup(self.env.stop)
        self.wrapped = self.folder/'recipient.pem.enc'
        motion.run(assets.OPENSSL+['-salt', '-in', str(self.key), '-out', str(self.wrapped), '-pass', 'env:KLUCZ'])
        self.base = self.package('monika-casual-motion-20261104.cms.enc',
                                 ['animations/casual/'+n+'.anim' for n in ('Idle', 'Walk', 'Run')])
        self.owner = self.package('monika-owner-motion-20261104.cms.enc',
                                  ['animations/monika-owner/'+n for n in ('Brutal To Happy Walking.fbx', 'Running.fbx', 'Jump.fbx')])
        self.real_run = motion.run

    def package(self, name, names):
        plain = self.folder/(name+'.tar.gz')
        records = []
        with tarfile.open(plain, 'w:gz') as archive:
            for path in names:
                data = ('original fixture: '+path).encode()
                info = tarfile.TarInfo(path); info.size = len(data)
                archive.addfile(info, io.BytesIO(data))
                records.append(dict(path=path, bytes=len(data), sha256=hashlib.sha256(data).hexdigest()))
        cipher = self.folder/name
        transfer.encrypt_cms(plain, cipher, self.cert)
        return dict(schema=1, releaseTag='oboz-transfer-key-123',
                    certificateSha256=assets.digest(self.cert), wrappedKeySha256=assets.digest(self.wrapped),
                    package=dict(name=name, bytes=cipher.stat().st_size, sha256=assets.digest(cipher)), files=records)

    def fake_run(self, command):
        if command[0] != 'gh':
            return self.real_run(command)
        self.assertEqual(command[1:3], ['release', 'download'])
        name = command[command.index('--pattern')+1]
        source = self.cert if name == 'recipient.crt' else self.wrapped if name == 'recipient.pem.enc' else self.folder/name
        shutil.copyfile(source, Path(command[command.index('--dir')+1])/name)

    def restore(self):
        with patch.object(motion, 'PACKAGES', self.folder), patch.object(assets, 'PRIVATE', self.private), patch.object(motion, 'run', self.fake_run):
            motion.restore(self.base, self.owner)

    def test_roundtrip_preserves_bytes_model_and_meta(self):
        model = self.private/'model.fbx'; model.write_bytes(b'unchanged model')
        run = self.private/'animations/monika-owner/Running.fbx'
        run.parent.mkdir(parents=True); run.write_bytes(b'previous')
        meta = run.with_suffix('.fbx.meta'); meta.write_text('guid: unchanged')
        self.restore()
        self.assertEqual(model.read_bytes(), b'unchanged model')
        self.assertEqual(meta.read_text(), 'guid: unchanged')
        self.assertEqual(assets.verify_sources(dict(schema=1, zone='oboz', files=self.base['files']+self.owner['files']), self.private), 6)

    def test_missing_owner_fails_before_any_external_command(self):
        (self.folder/self.owner['package']['name']).unlink()
        with patch.object(motion, 'PACKAGES', self.folder), patch.object(motion, 'run') as run, self.assertRaises(ValueError):
            motion.restore(self.base, self.owner)
        run.assert_not_called()

    def test_wrong_hash_fails_before_any_external_command(self):
        self.owner['package']['sha256'] = '0'*64
        with patch.object(motion, 'PACKAGES', self.folder), patch.object(motion, 'run') as run, self.assertRaises(ValueError):
            motion.restore(self.base, self.owner)
        run.assert_not_called()

    def test_bad_owner_file_hash_leaves_existing_idle_untouched(self):
        idle = self.private/'animations/casual/Idle.anim'
        idle.parent.mkdir(parents=True); idle.write_bytes(b'previous idle')
        self.owner['files'][0]['sha256'] = '0'*64
        with self.assertRaises(ValueError): self.restore()
        self.assertEqual(idle.read_bytes(), b'previous idle')
        self.assertFalse((self.private/'animations/monika-owner').exists())

    def test_tampered_cipher_fails_even_with_updated_outer_checksum(self):
        cipher = self.folder/self.owner['package']['name']
        data = bytearray(cipher.read_bytes()); data[-20] ^= 1; cipher.write_bytes(data)
        self.owner['package']['sha256'] = assets.digest(cipher)
        with self.assertRaises(Exception): self.restore()
        self.assertFalse((self.private/'animations').exists())

    def test_rejects_car_clip_duplicates_traversal_and_other_recipient(self):
        variants = []
        bad = copy.deepcopy(self.owner); bad['files'][0]['path'] = 'animations/monika-owner/Entering Car.fbx'; variants.append(bad)
        bad = copy.deepcopy(self.owner); bad['files'][0] = bad['files'][1]; variants.append(bad)
        bad = copy.deepcopy(self.owner); bad['package']['name'] = '../monika-owner-motion-20261104.cms.enc'; variants.append(bad)
        bad = copy.deepcopy(self.owner); bad['certificateSha256'] = '0'*64; variants.append(bad)
        for bad in variants:
            with self.subTest(data=bad), self.assertRaises(ValueError): motion.validate_owner(bad, self.base)


if __name__ == '__main__':
    unittest.main()
