import io
import json
import os
from pathlib import Path
import tarfile
import tempfile
import unittest
from unittest.mock import patch
import content_snapshot as snap


class ContentSnapshotTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.root = Path(self.tmp.name)
        self.source = self.root/'source'; self.source.mkdir()
        self.manifest = dict(baseId='osiedle-'+'a'*32, zone='osiedle', runtimeId='b'*64)
        self.expected = snap.identity(self.manifest)
        values = {
            'BraxContentBuild/base/manifest.json': json.dumps(self.manifest),
            'BraxContentBuild/base/addressables_content_state.bin': 'original state',
            'BraxContentBuild/base/scene-guids.txt': 'scene=guid',
            'Assets/Resources/BraxContentBase.json': json.dumps(self.manifest),
            'Assets/PlayableGenerated/estate.unity': 'original scene',
            'Assets/PlayableGenerated/estate.unity.meta': 'stable guid',
            'Assets/AddressableAssetsData/settings.asset': 'groups',
            'Assets/Source/model.glb': 'private source',
            'Assets/Source/model.glb.meta': 'model guid',
            'Assets/Source/Runtime.cs': 'source code excluded',
            'Assets/Source/Runtime.cs.meta': 'runtime guid',
            'Assets/StreamingAssets/BraxContent/base.bundle': 'APK bytes excluded',
            'ProjectSettings/ProjectSettings.asset': 'AndroidKeystorePass: \nAndroidKeyaliasPass: \n',
            'Packages/packages-lock.json': '{}',
            'braxhorn.keystore': 'never export signing key',
        }
        for name, data in values.items():
            path=self.source/name; path.parent.mkdir(parents=True,exist_ok=True);path.write_text(data)
        self.archive=self.root/'snapshot.enc'
        self.env=patch.dict(os.environ, {'KLUCZ':'test-only-password'});self.env.start()

    def tearDown(self):
        self.env.stop();self.tmp.cleanup()

    def test_encrypted_roundtrip_preserves_scene_guids_and_source_changes(self):
        snap.pack(self.source,self.archive,self.expected)
        target=self.root/'target';(target/'Assets/Source').mkdir(parents=True)
        (target/'Assets/Source/model.glb').write_text('new authored model')
        snap.restore(target,self.archive,self.expected)
        self.assertEqual((target/'Assets/PlayableGenerated/estate.unity.meta').read_text(),'stable guid')
        self.assertEqual((target/'Assets/Source/model.glb').read_text(),'new authored model')
        self.assertTrue((target/'Assets/Source/Runtime.cs.meta').exists())
        self.assertFalse((target/'Assets/Source/Runtime.cs').exists())
        self.assertFalse((target/'braxhorn.keystore').exists())
        self.assertFalse((target/'Assets/StreamingAssets/BraxContent/base.bundle').exists())
        self.assertEqual((target/'BraxContentBuild/base/addressables_content_state.bin').read_text(),'original state')

    def test_mistrzowie_has_separate_immutable_baseline_identity(self):
        manifest = dict(self.manifest, baseId='mistrzowie-' + 'c' * 32, zone='mistrzowie')
        identity = snap.identity(manifest)
        self.assertEqual(identity['base'], manifest['baseId'])
        self.assertEqual(identity['zone'], 'mistrzowie')
        self.assertNotEqual(identity['base'], self.expected['base'])

    def test_wrong_base_does_not_install(self):
        snap.pack(self.source,self.archive,self.expected)
        target=self.root/'target';target.mkdir()
        with self.assertRaises(ValueError):snap.restore(target,self.archive,dict(self.expected,base='other'))
        self.assertEqual(list(target.iterdir()),[])

    def test_signing_password_is_rejected(self):
        (self.source/'ProjectSettings/ProjectSettings.asset').write_text('AndroidKeystorePass: secret\n')
        with self.assertRaises(ValueError):snap.pack(self.source,self.archive,self.expected)

    def test_wrong_key_does_not_install(self):
        snap.pack(self.source,self.archive,self.expected)
        target=self.root/'target';target.mkdir()
        with patch.dict(os.environ,{'KLUCZ':'wrong'}),self.assertRaises((ValueError,tarfile.TarError,EOFError)):
            snap.restore(target,self.archive,self.expected)
        self.assertEqual(list(target.iterdir()),[])

    def test_immutable_picker_jar_is_snapshot_only(self):
        self.assertTrue(snap.allowed('Assets/Plugins/Android/BraxContentPicker.jar'))
        self.assertFalse(snap.allowed('Assets/Plugins/Android/other.jar'))
        self.assertFalse(snap.allowed('Assets/Source/custom.jar'))

    def test_unsafe_paths_and_code_are_rejected(self):
        for name in ('/etc/x','Assets/../../x','Assets\\x','Assets/Source/evil.cs','braxhorn.keystore','Library/secret'):
            self.assertFalse(snap.allowed(name),name)

    def test_symlink_archive_is_rejected(self):
        data=io.BytesIO()
        with tarfile.open(fileobj=data,mode='w') as tar:
            payload=json.dumps(self.expected).encode();h=tarfile.TarInfo(snap.HEADER);h.size=len(payload);tar.addfile(h,io.BytesIO(payload))
            item=tarfile.TarInfo('Assets/link');item.type=tarfile.SYMTYPE;item.linkname='/tmp';tar.addfile(item)
        data.seek(0)
        with self.assertRaises(ValueError):snap.extract(data,self.root,self.expected)

    def test_existing_baseline_cannot_be_overwritten(self):
        snap.pack(self.source,self.archive,self.expected)
        with self.assertRaises(ValueError):snap.restore(self.source,self.archive,self.expected)

if __name__=='__main__':unittest.main()
