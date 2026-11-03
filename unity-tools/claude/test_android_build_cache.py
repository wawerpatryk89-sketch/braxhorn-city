import importlib.util
import io
import pathlib
import tarfile
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('cache', pathlib.Path(__file__).with_name('android_build_cache.py'))
cache = importlib.util.module_from_spec(spec)
spec.loader.exec_module(cache)


class CacheTests(unittest.TestCase):
    def prepare(self, base):
        files = {'Library/Bee/artifacts/Android/obj.o': b'native',
                 'Library/Il2cppBuildCache/object': b'compiled',
                 'Library/Bee/Android/Prj/IL2CPP/Gradle/gradle.properties': b'password',
                 'Assets/PlayableGenerated/City.unity': b'scene',
                 'Assets/PlayableGenerated/City.unity.meta': b'generated-guid',
                 'Assets/Source/a.png.meta': b'guid',
                 'Assets/Source/private.png': b'private source',
                 'ProjectSettings/ProjectSettings.asset': b'settings',
                 'braxhorn.keystore': b'private key'}
        for name, data in files.items():
            p = base / name
            p.parent.mkdir(parents=True, exist_ok=True)
            p.write_bytes(data)
        stream = io.BytesIO()
        cache.pack(base, stream, '6000.0.58f2-Android-miasto', 'world-a')
        return stream.getvalue()

    def test_matching_world_and_native_objects_preserved(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = pathlib.Path(tmp)
            data = self.prepare(root / 'before')
            after = root / 'after'
            self.assertTrue(cache.restore(after, io.BytesIO(data), '6000.0.58f2-Android-miasto', 'world-a'))
            self.assertEqual((after / 'Library/Bee/artifacts/Android/obj.o').read_bytes(), b'native')
            self.assertTrue((after / 'Assets/PlayableGenerated/City.unity').exists())
            self.assertFalse((after / 'Library/Bee/Android/Prj').exists())
            self.assertFalse((after / 'braxhorn.keystore').exists())
            self.assertFalse((after / 'Assets/Source/private.png').exists())

    def test_changed_input_reuses_compile_but_not_world(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = pathlib.Path(tmp)
            data = self.prepare(root / 'before')
            after = root / 'after'
            self.assertFalse(cache.restore(after, io.BytesIO(data), '6000.0.58f2-Android-miasto', 'world-b'))
            self.assertTrue((after / 'Library/Bee/artifacts/Android/obj.o').exists())
            self.assertFalse((after / 'Assets/PlayableGenerated').exists())
            self.assertFalse((after / 'ProjectSettings').exists())

    def test_checkout_wins_and_force_generation(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = pathlib.Path(tmp)
            data = self.prepare(root / 'before')
            after = root / 'after'
            meta = after / 'Assets/Source/a.png.meta'
            meta.parent.mkdir(parents=True)
            meta.write_bytes(b'checkout-guid')
            cache.restore(after, io.BytesIO(data), '6000.0.58f2-Android-miasto', 'world-a', False)
            self.assertEqual(meta.read_bytes(), b'checkout-guid')
            self.assertFalse((after / 'Assets/PlayableGenerated').exists())

    def test_wrong_platform_and_path_traversal_rejected(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = pathlib.Path(tmp)
            data = self.prepare(root / 'before')
            with self.assertRaisesRegex(ValueError, 'another Unity'):
                cache.restore(root / 'after', io.BytesIO(data), 'WebGL', 'world-a')
            for path in ('../escape', '/absolute', 'Library/../../escape'):
                with self.assertRaisesRegex(ValueError, 'unsafe'):
                    cache.category(path)


if __name__ == '__main__':
    unittest.main()
