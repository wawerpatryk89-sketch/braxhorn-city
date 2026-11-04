"""Synthetic snapshot fixtures; these do not simulate or certify Unity runtime."""
import copy
import hashlib
import json
import sys
import tempfile
import unittest
import zipfile
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'tools'))
from snapshot_oboz_updates import snapshot, validate


class SnapshotTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.project = self.root / 'unity-city'
        self.registry = {'schema': 1, 'zone': 'oboz', 'kind': 'authoring-snapshot',
                         'unityVersion': '6000.0.58f2', 'readyForContentUpdate': False,
                         'runtimeVerified': False, 'files': [], 'entries': [], 'packageDependencies': []}
        for name in ('Packages/manifest.json', 'Packages/packages-lock.json', 'ProjectSettings/ProjectVersion.txt'):
            self.add(name, b'fixture only\n')
        for folder in ('Assets/PlayableGenerated', 'Assets/PlayableGenerated/Oboz'):
            self.add(folder+'.meta', b'fixture folder metadata\n')
        for i, name in enumerate(('wejscie', 'monika', 'vorn', 'zyro', 'rakk', 'kade', 'gorr', 'mis')):
            path = 'Assets/PlayableGenerated/Oboz/'+name+('.unity' if i == 0 else '.prefab')
            guid = format(i+1, '032x')
            self.add(path, ('fixture '+name+'\n').encode())
            self.add(path+'.meta', ('fileFormatVersion: 2\nguid: '+guid+'\n').encode())
            self.registry['entries'].append({'address': 'oboz/'+('scenes/' if i == 0 else 'characters/')+name,
                                              'path': path, 'guid': guid, 'dependencies': [path]})
        self.output = self.root / 'private-snapshot.zip'

    def add(self, name, data):
        path = self.project/name
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(data)
        self.registry['files'] = [r for r in self.registry['files'] if r['path'] != name]
        self.registry['files'].append({'path': name, 'bytes': len(data), 'sha256': hashlib.sha256(data).hexdigest()})

    def test_archive_preserves_all_bytes_and_meta(self):
        result = snapshot(self.project, self.registry, self.output)
        self.assertFalse(result['readyForContentUpdate'])
        with zipfile.ZipFile(self.output) as z:
            for r in self.registry['files']:
                self.assertEqual(z.read('unity-city/'+r['path']), (self.project/r['path']).read_bytes())
            self.assertEqual(json.loads(z.read('registry.json')), self.registry)

    def test_stale_source_rejected_before_output(self):
        path = self.project/self.registry['entries'][0]['path']
        path.write_bytes(b'changed')
        with self.assertRaisesRegex(ValueError, 'changed export input'):
            snapshot(self.project, self.registry, self.output)
        self.assertFalse(self.output.exists())

    def test_missing_meta_record_rejected(self):
        name = self.registry['entries'][0]['path']+'.meta'
        self.registry['files'] = [r for r in self.registry['files'] if r['path'] != name]
        with self.assertRaisesRegex(ValueError, 'Missing asset metadata'):
            validate(self.project, self.registry)

    def test_missing_folder_meta_record_rejected(self):
        self.registry['files'] = [r for r in self.registry['files'] if r['path'] != 'Assets/PlayableGenerated.meta']
        with self.assertRaisesRegex(ValueError, 'Missing folder metadata'):
            validate(self.project, self.registry)

    def test_duplicate_records_rejected(self):
        self.registry['files'].append(self.registry['files'][0])
        with self.assertRaisesRegex(ValueError, 'Duplicate file'):
            validate(self.project, self.registry)

    def test_guid_change_between_exports_rejected(self):
        previous = copy.deepcopy(self.registry)
        entry = self.registry['entries'][0]
        entry['guid'] = 'f'*32
        self.add(entry['path']+'.meta', ('guid: '+entry['guid']+'\n').encode())
        with self.assertRaisesRegex(ValueError, 'Stable address/path/GUID changed'):
            validate(self.project, self.registry, previous)

    def test_content_change_keeps_address_and_guid(self):
        previous = copy.deepcopy(self.registry)
        self.add(self.registry['entries'][0]['path'], b'new scene bytes, same GUID\n')
        validate(self.project, self.registry, previous)

    def test_existing_snapshot_is_not_overwritten(self):
        self.output.write_bytes(b'old baseline')
        with self.assertRaises(FileExistsError):
            snapshot(self.project, self.registry, self.output)
        self.assertEqual(self.output.read_bytes(), b'old baseline')

    def test_path_escape_and_symlink_rejected(self):
        record = self.registry['files'][0]
        record['path'] = 'Assets/../../outside'
        with self.assertRaisesRegex(ValueError, 'Unsafe path'):
            validate(self.project, self.registry)
        record['path'] = 'Assets/escape'
        (self.root/'outside').write_bytes(b'fixture only\n')
        (self.project/'Assets/escape').symlink_to(self.root/'outside')
        with self.assertRaisesRegex(ValueError, 'Symlink leaves project'):
            validate(self.project, self.registry)

    def test_runtime_source_added_after_registry_rejected(self):
        (self.project/'Assets/NewRuntime.cs').write_text('class NewRuntime {}')
        with self.assertRaisesRegex(ValueError, 'Source added after inventory'):
            validate(self.project, self.registry)

    def test_release_claim_rejected(self):
        self.registry['readyForContentUpdate'] = True
        with self.assertRaisesRegex(ValueError, 'cannot certify'):
            validate(self.project, self.registry)

    def test_missing_dependency_rejected(self):
        self.registry['entries'][0]['dependencies'].append('Assets/missing.mat')
        with self.assertRaisesRegex(ValueError, 'Missing dependency'):
            validate(self.project, self.registry)

    def test_roster_is_exact(self):
        self.registry['entries'].pop()
        with self.assertRaisesRegex(ValueError, 'Wrong camp scene/roster'):
            validate(self.project, self.registry)


if __name__ == '__main__':
    unittest.main()
