"""Animation transfer behavior with synthetic files; no owner keys or assets."""
from pathlib import Path
import tempfile
import unittest

from restore_player import REQUIRED, copy_animations, przygotuj_patryka


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


if __name__ == "__main__":
    unittest.main()
