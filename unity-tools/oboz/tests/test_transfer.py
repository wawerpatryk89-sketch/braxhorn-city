import copy
import hashlib
import json
from pathlib import Path
import sys
import shutil
import os
import tarfile
import tempfile
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import assets
import transfer


class TransferTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.tmp = tempfile.TemporaryDirectory()
        cls.root = Path(cls.tmp.name)
        cls.key, cls.cert = cls.root/'key.pem', cls.root/'cert.pem'
        transfer.run(['openssl', 'req', '-x509', '-newkey', 'rsa:3072', '-nodes',
            '-keyout', str(cls.key), '-out', str(cls.cert), '-days', '1', '-subj', '/CN=Test fixture'])

    @classmethod
    def tearDownClass(cls):
        cls.tmp.cleanup()

    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.folder = Path(self.temp.name)
        self.source = self.folder/'source'; self.source.mkdir()
        (self.source/'model.bin').write_bytes(b'original model bytes'*1000)
        path = self.source/'model.bin'
        self.data = dict(schema=1, zone='oboz', files=[dict(path='model.bin', bytes=path.stat().st_size,
            sha256=assets.digest(path))], releaseTag=None, packages=[], encryptedPackagesPrepared=False)
        self.manifest = self.folder/'private-inputs.json'
        transfer.write_json(self.manifest, self.data)
        self.receipt = dict(releaseTag='oboz-transfer-key-123', certificateSha256=assets.digest(self.cert),
                            wrappedKeySha256='a'*64, runId='123')

    def packed_request(self):
        with patch.object(assets, 'MANIFEST', self.manifest):
            transfer.pack(self.source, self.cert, self.receipt, self.folder/'output')
        request = json.loads((self.folder/'output/transfer-request.json').read_text())
        for part in request['packages']:
            part['url'] = 'https://example.blob.core.windows.net/encrypted/'+part['name']
        return request

    def test_public_key_roundtrip_preserves_original_bytes(self):
        request = self.packed_request()
        cipher = self.folder/'output'/request['packages'][0]['name']
        plain = self.folder/'plain.tar.gz'
        transfer.decrypt_cms(cipher, plain, self.cert, self.key)
        with tarfile.open(plain, 'r:gz') as tar:
            self.assertEqual(tar.getnames(), ['model.bin'])
            self.assertEqual(tar.extractfile('model.bin').read(), (self.source/'model.bin').read_bytes())

    def test_modified_ciphertext_is_rejected_and_plaintext_removed(self):
        request = self.packed_request()
        cipher = self.folder/'output'/request['packages'][0]['name']
        blob = bytearray(cipher.read_bytes()); blob[-20] ^= 1; cipher.write_bytes(blob)
        plain = self.folder/'plain'
        with self.assertRaises(Exception): transfer.decrypt_cms(cipher, plain, self.cert, self.key)
        self.assertFalse(plain.exists())

    def test_unrelated_recipient_is_rejected(self):
        bad = dict(self.receipt, certificateSha256='0'*64)
        with patch.object(assets, 'MANIFEST', self.manifest), self.assertRaises(ValueError):
            transfer.pack(self.source, self.cert, bad, self.folder/'output')

    def test_manifest_change_rejected(self):
        request = self.packed_request()
        self.manifest.write_text(self.manifest.read_text()+' ')
        with patch.object(assets, 'MANIFEST', self.manifest), self.assertRaises(ValueError):
            transfer.validate_request(request)

    def test_github_release_delivery_is_accepted_and_mixed_sources_rejected(self):
        request = self.packed_request()
        part = request['packages'][0]
        part.pop('url'); part['releaseTag']='oboz-transfer-key-123'
        with patch.object(assets, 'MANIFEST', self.manifest):
            transfer.validate_request(request)
            part['url']='https://example.blob.core.windows.net/a'
            with self.assertRaises(ValueError): transfer.validate_request(request)

    def test_transfer_rejects_unsafe_host_name_size_and_hash(self):
        request = self.packed_request()
        with patch.object(assets, 'MANIFEST', self.manifest):
            transfer.validate_request(request)
            for field, value in [('name','../private.pem'), ('bytes',201*1024*1024),
                                 ('sha256','0'), ('url','http://example.blob.core.windows.net/a'),
                                 ('url','https://evil.example/a'), ('url','https://user:pass@example.blob.core.windows.net/a')]:
                bad=copy.deepcopy(request); bad['packages'][0][field]=value
                with self.subTest(field=field,value=value), self.assertRaises(ValueError):
                    transfer.validate_request(bad)
            bad=copy.deepcopy(request); bad['packages']*=2
            with self.assertRaises(ValueError): transfer.validate_request(bad)

    def test_pack_requires_exact_source_hash(self):
        (self.source/'model.bin').write_bytes(b'changed')
        with patch.object(assets, 'MANIFEST', self.manifest), self.assertRaises(ValueError):
            transfer.pack(self.source, self.cert, self.receipt, self.folder/'output')

    def test_ci_receives_reencrypts_and_reads_back_complete_delivery(self):
        request = self.packed_request()
        repo = self.folder/'repo'; repo.mkdir()
        helper = repo/'unity-tools/claude/prywatne.py'; helper.parent.mkdir(parents=True)
        shutil.copyfile(assets.ROOT/'unity-tools/claude/prywatne.py', helper)
        wrapped = self.folder/'recipient.pem.enc'
        real_run = transfer.run
        with patch.dict(os.environ, {'KLUCZ':'test-fixture-only', 'GITHUB_REPOSITORY':'test/camp', 'GITHUB_SHA':'0'*40}):
            real_run(assets.OPENSSL+['-salt','-in',str(self.key),'-out',str(wrapped),'-pass','env:KLUCZ'])
            request['recipient']['wrappedKeySha256']=assets.digest(wrapped)
            published={}
            def fake_run(cmd):
                if cmd[0]!='gh': return real_run(cmd)
                if cmd[2]=='create': return
                if cmd[2]=='upload':
                    src=Path(cmd[-1]); published[src.name]=src.read_bytes(); return
                name=cmd[cmd.index('--pattern')+1]
                dest=Path(cmd[cmd.index('--dir')+1])/name
                if name=='recipient.crt': dest.write_bytes(self.cert.read_bytes())
                elif name=='recipient.pem.enc': dest.write_bytes(wrapped.read_bytes())
                else: dest.write_bytes(published[name])
            def fake_download(part,target):
                shutil.copyfile(self.folder/'output'/part['name'],target)
            with patch.object(assets,'ROOT',repo), patch.object(assets,'MANIFEST',self.manifest), \
                 patch.object(transfer,'run',fake_run), patch.object(transfer,'download_part',fake_download):
                transfer.accept(request,self.folder/'proof')
            proof=json.loads((self.folder/'proof/delivery-proof.json').read_text())
            self.assertEqual(proof['verifiedFiles'],1)
            self.assertTrue(proof['readbackVerified'])
            manifest=json.loads((self.folder/'proof/private-inputs.json').read_text())
            encrypted=repo/'unity-city/Prywatne'/manifest['packages'][0]['name']
            plain=self.folder/'final.tar.gz'
            real_run(assets.OPENSSL+['-d','-in',str(encrypted),'-out',str(plain),'-pass','env:KLUCZ'])
            with tarfile.open(plain,'r:gz') as tar:
                self.assertEqual(tar.extractfile('model.bin').read(),(self.source/'model.bin').read_bytes())

    def test_pack_does_not_overwrite_existing_output(self):
        self.packed_request()
        with patch.object(assets, 'MANIFEST', self.manifest), self.assertRaises(FileExistsError):
            transfer.pack(self.source, self.cert, self.receipt, self.folder/'output')


if __name__ == '__main__': unittest.main()
