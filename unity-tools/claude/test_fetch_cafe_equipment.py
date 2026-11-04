import hashlib
import importlib.util
import json
from pathlib import Path
import struct
import tempfile
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('cafe', Path(__file__).with_name('fetch_cafe_equipment.py'))
cafe = importlib.util.module_from_spec(spec)
spec.loader.exec_module(cafe)


def glb():
    data = json.dumps({'asset': {'version': '2.0'}, 'meshes': [{'primitives': []}], 'extras': 'x' * 1100}).encode()
    data += b' ' * (-len(data) % 4)
    return b'glTF' + struct.pack('<IIII', 2, 20 + len(data), len(data), 0x4E4F534A) + data


class CafeTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        root = Path(self.tmp.name)
        self.out = root / 'out'
        self.private = root / 'private'
        for name, value in [('OUT', self.out), ('PRIVATE', self.private), ('EXPECTED_SHA256', {url: hashlib.sha256(glb()).hexdigest() for url in [cafe.URL_MACHINE, cafe.URL_GRINDER]})]:
            p = patch.object(cafe, name, value); p.start(); self.addCleanup(p.stop)

    def test_download_uses_only_published_glb_urls(self):
        with patch.object(cafe, 'get', return_value=glb()) as get:
            cafe.main()
        self.assertEqual([c.args[0] for c in get.call_args_list], [cafe.URL_MACHINE, cafe.URL_GRINDER])
        self.assertEqual(len(list(self.out.glob('*.glb'))), 2)
        self.assertEqual(list(self.out.glob('*.part')), [])

    def test_valid_cache_works_without_metadata_api_or_network(self):
        self.out.mkdir()
        for name in ['espresso_machine_sobczak.glb', 'coffee_grinder_sobczak.glb']:
            (self.out / name).write_bytes(glb())
        with patch.object(cafe, 'get', side_effect=AssertionError('network must not be needed')):
            cafe.main()

    def test_owner_models_take_priority(self):
        folder = self.private / 'miasto_sklepy'; folder.mkdir(parents=True)
        for name in ['ekspres', 'mlynek']:
            (folder / (name + '.glb')).write_bytes(glb())
        with patch.object(cafe, 'get', side_effect=AssertionError('owner model replaced')):
            cafe.main()
        self.assertFalse(self.out.exists())

    def test_equipment_subtree_matches_unity_private_lookup(self):
        folder = self.private / 'miasto_wyposazenie'; folder.mkdir(parents=True)
        source = json.loads(glb()[20:])
        source['nodes'] = [{'name': 'EKSPRES', 'children': [1]}, {'mesh': 0}, {'name': 'mlynek', 'mesh': 0}]
        data = json.dumps(source).encode(); data += b' ' * (-len(data) % 4)
        (folder / 'miasto_wyposazenie.glb').write_bytes(b'glTF' + struct.pack('<IIII', 2, 20 + len(data), len(data), 0x4E4F534A) + data)
        with patch.object(cafe, 'get', side_effect=AssertionError('private subtree replaced')):
            cafe.main()

    def test_wrong_model_with_valid_glb_is_rejected(self):
        data = glb().replace(b'xxx', b'yyy', 1)
        with patch.object(cafe, 'get', return_value=data):
            with self.assertRaisesRegex(ValueError, 'SHA-256'): cafe.main()
        self.assertFalse(self.out.exists())

    def test_html_response_does_not_create_model(self):
        with patch.object(cafe, 'get', return_value=b'<html>unavailable</html>'):
            with self.assertRaises(ValueError): cafe.main()
        self.assertFalse(self.out.exists())

    def test_corrupt_cache_is_not_accepted(self):
        self.out.mkdir(); (self.out / 'espresso_machine_sobczak.glb').write_bytes(b'x' * 1200)
        with self.assertRaises(ValueError): cafe.main()

    def test_invalid_glb_size_rejected(self):
        with self.assertRaises(ValueError): cafe.valid_glb(glb() + b'truncated transfer')


if __name__ == '__main__': unittest.main()
