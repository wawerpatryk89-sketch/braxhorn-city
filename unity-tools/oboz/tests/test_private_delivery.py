import copy
import hashlib
import io
import json
import os
from pathlib import Path
import subprocess
import struct
import sys
import tarfile
import tempfile
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import assets
import world


def archive(path, entries):
    with tarfile.open(path, 'w:gz') as tar:
        for name, value in entries:
            member = tarfile.TarInfo(name)
            if value is None:
                member.type = tarfile.SYMTYPE
                member.linkname = '/tmp/outside'
                tar.addfile(member)
            else:
                member.size = len(value)
                tar.addfile(member, io.BytesIO(value))


class DeliveryTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.root = Path(self.tmp.name)
        self.payload = b'original private source bytes'
        self.record = dict(path='characters/monika/model.fbx', bytes=len(self.payload),
                           sha256=hashlib.sha256(self.payload).hexdigest())
        self.data = dict(schema=1, zone='oboz', files=[self.record], packages=[],
                         encryptedPackagesPrepared=False, releaseTag=None)

    def test_inventory_rejects_metadata_duplicate_and_bad_hash(self):
        for change in ('metadata', 'duplicate', 'hash'):
            data = copy.deepcopy(self.data)
            if change == 'metadata': data['files'][0]['path'] += '.meta'
            elif change == 'duplicate': data['files'].append(copy.deepcopy(self.record))
            else: data['files'][0]['sha256'] = 'z'*64
            with self.subTest(change=change), self.assertRaises(ValueError):
                assets.validate_inventory(data)

    def test_unsafe_paths_and_symlink_escape_rejected(self):
        for name in ('../outside', '/outside', 'a/../../b', 'a\\b', '.', 'a//b'):
            with self.subTest(name=name), self.assertRaises(ValueError): assets.safe(self.root, name)
        (self.root/'link').symlink_to(self.root.parent, target_is_directory=True)
        with self.assertRaises(ValueError): assets.safe(self.root, 'link/outside')

    def test_preflight_stops_unprepared_delivery_without_unity(self):
        with patch.dict(os.environ, {'KLUCZ': 'test-only'}), self.assertRaisesRegex(ValueError, 'not been encrypted'):
            assets.require_delivery(self.data)

    def test_restore_preserves_metadata_and_exact_plaintext(self):
        private = self.root/'private'
        target = private/self.record['path']; target.parent.mkdir(parents=True)
        target.write_bytes(b'old')
        meta = target.with_suffix('.fbx.meta'); meta.write_text('guid: preserved')
        plain = self.root/'input.tar.gz'
        archive(plain, [(self.record['path'], self.payload)])
        encrypted = self.root/'pack.enc'
        with patch.dict(os.environ, {'KLUCZ':'test-only-fixture-key', 'GITHUB_REPOSITORY':'test/repo'}):
            subprocess.run(assets.OPENSSL+['-salt','-in',str(plain),'-out',str(encrypted),'-pass','env:KLUCZ'], check=True)
            self.assertEqual(encrypted.read_bytes()[:8], b'Salted__')
            data = copy.deepcopy(self.data)
            data.update(encryptedPackagesPrepared=True, releaseTag='oboz-private-012345abcdef', packages=[
                dict(name='oboz_012345abcdef_000.tar.gz.enc', bytes=encrypted.stat().st_size, sha256=assets.digest(encrypted))])
            original_run = subprocess.run
            def download(cmd, **kwargs):
                if cmd[0] == 'gh':
                    (Path(cmd[cmd.index('--dir')+1])/data['packages'][0]['name']).write_bytes(encrypted.read_bytes())
                    return subprocess.CompletedProcess(cmd, 0)
                return original_run(cmd, **kwargs)
            with patch.object(assets, 'PRIVATE', private), patch.object(assets.subprocess, 'run', side_effect=download):
                assets.restore(data)
            self.assertEqual(target.read_bytes(), self.payload)
            self.assertEqual(meta.read_text(), 'guid: preserved')
            # Valid ciphertext but wrong plaintext receipt must never replace installed sources.
            target.write_bytes(b'keep current')
            data['files'][0]['sha256'] = '0'*64
            with patch.object(assets, 'PRIVATE', private), patch.object(assets.subprocess, 'run', side_effect=download):
                with self.assertRaisesRegex(ValueError, 'plaintext'): assets.restore(data)
            self.assertEqual(target.read_bytes(), b'keep current')

    def test_wrong_member_size_hash_duplicate_and_link_rejected(self):
        expected = {self.record['path']: self.record}
        cases = [
            [('unexpected.fbx', self.payload)],
            [(self.record['path'], self.payload+b'!')],
            [(self.record['path'], b'x'*len(self.payload))],
            [(self.record['path'], self.payload)]*2,
            [(self.record['path'], None)],
        ]
        for i, entries in enumerate(cases):
            plain = self.root/f'bad{i}.tgz'; stage=self.root/f'stage{i}'; stage.mkdir()
            archive(plain, entries)
            with self.subTest(i=i), tarfile.open(plain, 'r:gz') as tar, self.assertRaises(ValueError):
                assets.extract_verified(tar, stage, expected, set())

    def test_actual_input_hash_must_match(self):
        target=self.root/self.record['path'];target.parent.mkdir(parents=True);target.write_bytes(self.payload)
        self.assertEqual(assets.verify_sources(self.data, self.root), 1)
        target.write_bytes(b'x'*len(self.payload))
        with self.assertRaises(ValueError): assets.verify_sources(self.data, self.root)

    def test_inventory_skips_incomplete_copy_files_without_changing_originals(self):
        payload=b'{}  '
        glb=struct.pack('<4sIIII',b'glTF',2,20+len(payload),len(payload),0x4e4f534a)+payload
        for name in ('environment/models/underground_bunker__unity.glb',
                     'environment/gym/braxhorn-original-gym__unity.glb'):
            path=self.root/name;path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes(glb)
        (self.root/'environment/gym/gym-collisions.json').write_text('{}')
        folder=self.root/'characters/monika';folder.mkdir(parents=True)
        (folder/'Color.png').write_bytes(b'original')
        temporary=folder/'.Color.png.partial';temporary.write_bytes(b'partial copy')
        data=assets.inventory(self.root)
        self.assertIn('characters/monika/Color.png',[f['path'] for f in data['files']])
        self.assertNotIn('characters/monika/.Color.png.partial',[f['path'] for f in data['files']])
        self.assertEqual(temporary.read_bytes(),b'partial copy')


class WorldTests(unittest.TestCase):
    STATE = 'Assets/PlayableGenerated/Oboz/build-state.json'
    META = 'Assets/PlayableGenerated/Oboz/model.asset.meta'

    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory(); self.addCleanup(self.tmp.cleanup)
        self.root=Path(self.tmp.name);self.project=self.root/'unity-city';self.project.mkdir()

    def test_checkpoint_keeps_current_guid(self):
        target=self.project/self.META;target.parent.mkdir(parents=True);target.write_bytes(b'current guid')
        plain=self.root/'world.tgz'
        archive(plain, [(self.STATE,b'{}'),(self.META,b'old guid')])
        world.restore_plain(plain,self.project)
        self.assertEqual(target.read_bytes(),b'current guid')
        self.assertEqual((self.project/self.STATE).read_bytes(),b'{}')

    def test_invalid_checkpoint_never_installs_partial_state(self):
        cases = [
            [(self.STATE,b'{}'),('Assets/PlayableGenerated/Oboz/../../outside',b'bad')],
            [(self.STATE,b'{}'),('Assets/Source/private.fbx',b'private')],
            [(self.META,b'guid')],
            [(self.STATE,b'{}'),(self.META,None)],
            [(self.STATE,b'{}')]*2,
        ]
        for i, entries in enumerate(cases):
            plain=self.root/f'bad{i}.tgz';archive(plain,entries)
            with self.subTest(i=i),self.assertRaises(ValueError):world.restore_plain(plain,self.project)
            self.assertFalse((self.project/self.STATE).exists())

    def test_encrypted_round_trip_and_immutable_output(self):
        state=self.project/self.STATE;state.parent.mkdir(parents=True);state.write_text('{}')
        meta=self.project/self.META;meta.write_text('guid: stable')
        os.utime(meta,(1500000000,1500000000))
        package=self.root/'world.enc'
        other=self.root/'restored';other.mkdir()
        with patch.dict(os.environ,{'KLUCZ':'test-only-fixture-key'}):
            world.run('pack',self.project,package)
            self.assertEqual(package.read_bytes()[:8],b'Salted__')
            world.run('restore',other,package)
            self.assertEqual((other/self.META).read_text(),'guid: stable')
            self.assertEqual((other/self.META).stat().st_mtime,1500000000)
            before=assets.digest(package)
            with self.assertRaises(FileExistsError):world.run('pack',self.project,package)
            self.assertEqual(assets.digest(package),before)


if __name__ == '__main__':
    unittest.main()
