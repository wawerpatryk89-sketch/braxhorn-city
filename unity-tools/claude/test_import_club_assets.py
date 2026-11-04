import hashlib
import io
import json
import os
from pathlib import Path
import struct
import subprocess
import tempfile
import unittest
from unittest.mock import patch
import zipfile
import import_club_assets as module


class ClubImportTests(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory();self.addCleanup(self.tmp.cleanup)
        self.root=Path(self.tmp.name)
        # A minimal GLB JSON chunk, no owner data or production keys.
        payload=b'{"asset":{"version":"2.0"}} ';
        payload+=b' ' * (-len(payload)%4)
        data=b'glTF'+struct.pack('<II',2,20+len(payload))+struct.pack('<II',len(payload),0x4e4f534a)+payload
        self.files={name:data for name in module.NAMES}
        self.pack()

    def pack(self):
        out=io.BytesIO()
        with zipfile.ZipFile(out,'w') as archive:
            for name,data in self.files.items():archive.writestr(name,data)
        self.raw=out.getvalue()
        self.manifest={'zip_bytes':len(self.raw),'zip_sha256':hashlib.sha256(self.raw).hexdigest(),
            'files':{name:hashlib.sha256(data).hexdigest() for name,data in self.files.items()}}

    def test_exact_original_bytes(self):
        self.assertEqual(module.unpack_verified(self.raw,self.manifest),self.files)

    def test_missing_truncated_or_damaged_file(self):
        with self.assertRaises(ValueError):module.unpack_verified(self.raw[:-1],self.manifest)
        self.manifest['files'][module.NAMES[0]]='0'*64
        with self.assertRaises(ValueError):module.unpack_verified(self.raw,self.manifest)

    def test_path_traversal_rejected(self):
        self.files['../outside.glb']=self.files.pop(module.NAMES[0]);self.pack()
        with self.assertRaises(ValueError):module.unpack_verified(self.raw,self.manifest)

    def test_glb_header_rejected_even_when_hash_matches(self):
        self.files[module.NAMES[0]]=b'not a GLB file';self.pack()
        with self.assertRaises(ValueError):module.unpack_verified(self.raw,self.manifest)

    def test_missing_key_does_not_write(self):
        with patch.dict(os.environ,{'KLUCZ':''}):
            with self.assertRaises(RuntimeError):module.install(self.root/'output')
        self.assertFalse((self.root/'output').exists())

    def test_encrypted_roundtrip_preserves_guid_and_rejects_tamper(self):
        key=self.root/'synthetic.pem'
        subprocess.run(['openssl','genpkey','-algorithm','RSA','-pkeyopt','rsa_keygen_bits:2048','-out',str(key)],check=True,capture_output=True)
        env=dict(os.environ,KLUCZ='synthetic-club-test-only')
        def enc(data):return subprocess.run(module.OPENSSL+['-salt','-pass','env:KLUCZ'],input=data,env=env,check=True,capture_output=True).stdout
        encrypted_key=self.root/'key.enc';encrypted_key.write_bytes(enc(key.read_bytes()))
        wrapped=subprocess.run(['openssl','pkeyutl','-encrypt','-inkey',str(key),'-pkeyopt','rsa_padding_mode:oaep','-pkeyopt','rsa_oaep_md:sha256'],input=env['KLUCZ'].encode(),check=True,capture_output=True).stdout
        (self.root/'package-key.rsa').write_bytes(wrapped)
        encrypted=enc(self.raw);(self.root/'models.zip.enc').write_bytes(encrypted)
        self.manifest['encrypted_sha256']=hashlib.sha256(encrypted).hexdigest()
        (self.root/'manifest.json').write_text(json.dumps(self.manifest))
        target=self.root/'output';target.mkdir();meta=target/(module.NAMES[0]+'.meta');meta.write_text('guid: unchanged')
        with patch.dict(os.environ,{'KLUCZ':env['KLUCZ']}):
            module.install(target,self.root,encrypted_key)
            self.assertEqual({p.name:p.read_bytes() for p in target.glob('*.glb')},self.files)
            self.assertEqual(meta.read_text(),'guid: unchanged')
            (self.root/'models.zip.enc').write_bytes(encrypted[:-1]+bytes([encrypted[-1]^1]))
            with self.assertRaises(ValueError):module.install(self.root/'bad',self.root,encrypted_key)
        self.assertFalse((self.root/'bad').exists())


if __name__=='__main__':unittest.main()
