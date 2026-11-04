"""Animation transfer behavior with synthetic files; no owner keys or assets."""
from pathlib import Path
import tempfile
import unittest

from PIL import Image
from restore_player import REQUIRED, copy_animations, przygotuj_patryka, pack_orm


class PlayerTransferTests(unittest.TestCase):
    def setUp(self):
        tmp = tempfile.TemporaryDirectory()
        self.addCleanup(tmp.cleanup)
        self.root = Path(tmp.name)
        self.source, self.target = self.root / "source", self.root / "target"
        self.source.mkdir()

    def fixture(self, fallback=False):
        result = {}
        for clip, candidates in REQUIRED.items():
            path = candidates[-1 if fallback else 0]
            source = self.source / path
            source.parent.mkdir(parents=True, exist_ok=True)
            data = ("synthetic " + clip).encode()
            source.write_bytes(data)
            result[path] = data
        return result

    def test_primary_animations_keep_exact_bytes_and_relative_paths(self):
        expected = self.fixture()
        files = copy_animations(self.source, self.target)
        self.assertEqual({str(p.relative_to(self.target)): p.read_bytes() for p in files}, expected)

    def test_estate_fallback_animations_are_accepted(self):
        expected = self.fixture(fallback=True)
        files = copy_animations(self.source, self.target)
        self.assertEqual({str(p.relative_to(self.target)): p.read_bytes() for p in files}, expected)

    def test_walk_cannot_silently_replace_missing_run(self):
        self.fixture()
        (self.source / REQUIRED["Run"][0]).unlink()
        with self.assertRaisesRegex(RuntimeError, "Run"):
            copy_animations(self.source, self.target)
        self.assertFalse(self.target.exists())

    def test_wrong_model_is_rejected_before_import(self):
        (self.source / "patryk").mkdir()
        (self.source / "patryk/old.fbx").write_bytes(b"wrong model")
        with self.assertRaisesRegex(ValueError, "stary model"):
            przygotuj_patryka(self.source)

    def test_orm_preserves_full_resolution_and_every_data_texel(self):
        # High-frequency asymmetric values detect downsampling, channel swaps,
        # gamma conversion and accidental use of the alpha channel.
        size = (4096, 4096)
        rough = Image.frombytes("L", size, bytes(range(256)) * (4096 * 16))
        metal = Image.frombytes("L", size, bytes(reversed(range(256))) * (4096 * 16))
        rgba = Image.merge("RGBA", (rough, rough, rough, Image.new("L", size, 7)))
        result = pack_orm(rgba, metal)
        self.assertEqual(result.size, size)
        r, g, b = result.split()
        self.assertEqual(r.getextrema(), (255, 255))
        self.assertEqual(g.tobytes(), rough.tobytes())
        self.assertEqual(b.tobytes(), metal.tobytes())

    def test_orm_rejects_mismatched_or_colored_maps(self):
        with self.assertRaisesRegex(ValueError, "dimensions"):
            pack_orm(Image.new("L", (4, 4)), Image.new("L", (2, 2)))
        with self.assertRaisesRegex(ValueError, "grayscale"):
            pack_orm(Image.new("RGB", (4, 4), (10, 20, 30)), Image.new("L", (4, 4)))


if __name__ == "__main__":
    unittest.main()
