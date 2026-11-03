import importlib.util
import pathlib
import struct
import tempfile
import unittest
import zipfile

spec = importlib.util.spec_from_file_location('checker', pathlib.Path(__file__).with_name('check_apk_scene_bounds.py'))
checker = importlib.util.module_from_spec(spec)
spec.loader.exec_module(checker)


def scene(start=0, size=8, type_id=0, declared_size=520, trees=False):
    meta = b'6000.0.58f2\0' + struct.pack('<iBi', 13, trees, 1)
    meta += struct.pack('<iBh', 1, 0, -1) + bytes(16)
    if trees:
        meta += struct.pack('<ii', 1, 4) + bytes(32) + b'foo\0' + struct.pack('<i', 0)
    meta += struct.pack('<i', 1)
    meta += bytes((-(48 + len(meta))) % 4)
    meta += struct.pack('<qqIi', 123, start, size, type_id)
    header = struct.pack('>IIIIB3sIqqq', 0, 0, 22, 0, 0, bytes(3), len(meta), declared_size, 512, 0)
    return (header + meta).ljust(520, b'\0')


class BoundsTest(unittest.TestCase):
    def test_valid_with_and_without_type_trees(self):
        for trees in (False, True):
            report = checker.inspect_scene(scene(trees=trees), 520)
            self.assertEqual(report['objects'], 1)
            self.assertEqual(report['type_trees'], trees)

    def test_object_extends_past_file(self):
        with self.assertRaisesRegex(ValueError, 'extends outside'):
            checker.inspect_scene(scene(size=9), 520)

    def test_negative_start(self):
        with self.assertRaisesRegex(ValueError, 'extends outside'):
            checker.inspect_scene(scene(start=-1), 520)

    def test_missing_type(self):
        with self.assertRaisesRegex(ValueError, 'type'):
            checker.inspect_scene(scene(type_id=1), 520)

    def test_truncated_file(self):
        with self.assertRaisesRegex(ValueError, 'invalid header'):
            checker.inspect_scene(scene(), 519)

    def test_unsupported_format(self):
        data = bytearray(scene())
        struct.pack_into('>I', data, 8, 23)
        with self.assertRaisesRegex(ValueError, 'unsupported'):
            checker.inspect_scene(data, 520)

    def test_zip_integration_and_missing_scene(self):
        with tempfile.TemporaryDirectory() as tmp:
            path = pathlib.Path(tmp) / 'test.apk'
            with zipfile.ZipFile(path, 'w') as apk:
                apk.writestr('assets/bin/Data/level1', scene())
            self.assertEqual(checker.inspect_apk(path)['errors'], [])
            with zipfile.ZipFile(path, 'w') as apk:
                apk.writestr('assets/bin/Data/level0', scene())
            self.assertIn('level1 not successfully inspected', checker.inspect_apk(path)['errors'])

    def test_android_split_files_and_gap(self):
        with tempfile.TemporaryDirectory() as tmp:
            path = pathlib.Path(tmp) / 'test.apk'
            data = scene()
            with zipfile.ZipFile(path, 'w') as apk:
                for i in reversed(range(13)):
                    apk.writestr(f'assets/bin/Data/level1.split{i}', data[i*40:(i+1)*40])
            report = checker.inspect_apk(path)
            self.assertEqual(report['errors'], [])
            self.assertEqual(report['scenes']['assets/bin/Data/level1']['segments'], 13)
            with zipfile.ZipFile(path, 'w') as apk:
                apk.writestr('assets/bin/Data/level1.split0', data[:256])
                apk.writestr('assets/bin/Data/level1.split2', data[256:])
            self.assertTrue(any('missing, duplicated' in error for error in checker.inspect_apk(path)['errors']))


if __name__ == '__main__':
    unittest.main()
