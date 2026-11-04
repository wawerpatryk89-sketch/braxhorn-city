import contextlib
import importlib.util
import io
import pathlib
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('region', pathlib.Path(__file__).with_name('rejon_buildu.py'))
region = importlib.util.module_from_spec(spec)
spec.loader.exec_module(region)


class CityKeyTests(unittest.TestCase):
    def test_bootstrap_does_not_regenerate_world_but_gameplay_and_assets_do(self):
        with tempfile.TemporaryDirectory() as tmp, contextlib.redirect_stdout(io.StringIO()):
            root = pathlib.Path(tmp)
            start = root / 'Assets/Claude/BraxStart.cs'
            game = root / 'Assets/Claude/BraxObszary.cs'
            texture = root / 'Assets/Source/wall.png'
            start.parent.mkdir(parents=True)
            texture.parent.mkdir(parents=True)
            start.write_text('public class BraxStart {}')
            game.write_text('public class BraxObszary {}')
            texture.write_bytes(b'original texture')
            def key():
                return region.klucz(root, 'miasto', 'android', '6000.0.58f2')
            initial = key()
            start.write_text('public class BraxStart { void Start() {} }')
            self.assertEqual(initial, key())
            game.write_text('public class BraxObszary { public float distance = 100; }')
            gameplay = key()
            self.assertNotEqual(initial, gameplay)
            texture.write_bytes(b'changed texture')
            self.assertNotEqual(gameplay, key())


if __name__ == '__main__':
    unittest.main()
