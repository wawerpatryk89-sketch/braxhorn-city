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
import import_patryk_animations as module

class OwnerAnimationTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory(); self.addCleanup(self.tmp.cleanup)
        self.root = Path(self.tmp.name)
        self.files = {name: b'Kaydara FBX Binary  \x00\x1a\x00' + name.encode() for name in module.NAMES}
        out = io.BytesIO()
        with zipfile.ZipFile(out, 'w') as archive:
            for name, data in self.files.items(): archive.writestr(name, data)
        self.raw = out.getvalue()
        self.manifest = {'zip_bytes': len(self.raw), 'zip_sha256': hashlib.sha256(self.raw).hexdigest(),
                         'files': {name: hashlib.sha256(data).hexdigest() for name, data in self.files.items()}}
        p = patch.object(module, 'ZIP_SHA256', self.manifest['zip_sha256']); p.start(); self.addCleanup(p.stop)

    def test_exact_bytes(self):
        self.assertEqual(module.unpack_verified(self.raw, self.manifest), self.files)

    def test_corruption_rejected(self):
        with self.assertRaises(ValueError): module.unpack_verified(self.raw[:-1], self.manifest)

    def test_wrong_source_hash_rejected(self):
        self.manifest['files']['Jump.fbx'] = '0' * 64
        with self.assertRaises(ValueError): module.unpack_verified(self.raw, self.manifest)

    def test_old_clips_and_guids_preserved(self):
        old = self.root / 'brax/Idle.fbx'; old.parent.mkdir(); old.write_bytes(b'old idle')
        owner = self.root / 'patryk_owner'; owner.mkdir()
        meta = owner / 'Jump.fbx.meta'; meta.write_text('guid: preserved')
        paths = module.install_files(module.unpack_verified(self.raw, self.manifest), self.root)
        self.assertEqual(len(paths), 6)
        self.assertEqual(old.read_bytes(), b'old idle'); self.assertEqual(meta.read_text(), 'guid: preserved')
        self.assertEqual({p.name:p.read_bytes() for p in paths}, self.files)

    def test_missing_key_no_writes(self):
        with patch.dict(os.environ, {'KLUCZ': ''}):
            with self.assertRaises(RuntimeError): module.install(self.root / 'output')
        self.assertFalse((self.root / 'output').exists())

    def test_complete_encrypted_import_and_tamper(self):
        # Disposable test key, never the repository private key or signing password.
        transfer = self.root / 'transfer'; transfer.mkdir()
        key = self.root / 'test.pem'
        subprocess.run(['openssl','genpkey','-algorithm','RSA','-pkeyopt','rsa_keygen_bits:2048','-out',str(key)],check=True,capture_output=True)
        env = dict(os.environ, KLUCZ='disposable-test-password')
        enc = lambda data: subprocess.run(module.OPENSSL+['-salt','-pass','env:KLUCZ'],input=data,env=env,check=True,capture_output=True).stdout
        (transfer/'import-private.pem.enc').write_bytes(enc(key.read_bytes()))
        (transfer/'package-key.rsa').write_bytes(subprocess.run(['openssl','pkeyutl','-encrypt','-inkey',str(key),'-pkeyopt','rsa_padding_mode:oaep','-pkeyopt','rsa_oaep_md:sha256'],input=env['KLUCZ'].encode(),check=True,capture_output=True).stdout)
        data=enc(self.raw);(transfer/'part-000.enc').write_bytes(data)
        self.manifest['chunks']=[{'file':'part-000.enc','bytes':len(data),'sha256':hashlib.sha256(data).hexdigest()}]
        (transfer/'manifest.json').write_text(json.dumps(self.manifest))
        with patch.dict(os.environ, {'KLUCZ':env['KLUCZ']}):
            paths=module.install(self.root/'output',transfer)
            self.assertEqual({p.name:p.read_bytes() for p in paths},self.files)
            (transfer/'part-000.enc').write_bytes(data[:-1]+bytes([data[-1]^1]))
            with self.assertRaises(ValueError):module.install(self.root/'bad',transfer)
        self.assertFalse((self.root/'bad').exists())

if __name__ == '__main__': unittest.main()
