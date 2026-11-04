"""Real encrypted cache round trips; never touch a user's Unity project."""
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest

SCRIPT = Path(__file__).with_name('library_cache.sh')

@unittest.skipUnless(all(shutil.which(x) for x in ('bash', 'openssl', 'zstd', 'tar')), 'cache tools unavailable')
class CacheTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.root = Path(self.tmp.name)
        self.project = self.root / 'project'
        self.project.mkdir()
        self.archive = self.root / 'cache.enc'
        self.env = dict(os.environ, KLUCZ='test-only-cache-password')
        self.put('Assets/source.meta', 'original-guid')
        self.put('Assets/PlayableGenerated/Mistrzowie/scene.unity.meta', 'generated-guid')
        self.put('Library/imported', 'import-data')
        self.put('Library/Bee/discard', 'bee')
        self.put('Library/Il2cppBuildCache/discard', 'il2cpp')
        self.put('braxhorn.keystore', 'test-key-not-real')

    def put(self, path, value):
        p = self.project / path
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_text(value)

    def run_cache(self, mode):
        return subprocess.run(['bash', str(SCRIPT), mode, str(self.project), str(self.archive)], env=self.env, capture_output=True)

    def test_roundtrip_retains_current_guid_and_excludes_generated_outputs(self):
        result = self.run_cache('save')
        self.assertEqual(result.returncode, 0, result.stderr)
        shutil.rmtree(self.project / 'Library')
        shutil.rmtree(self.project / 'Assets/PlayableGenerated')
        (self.project / 'braxhorn.keystore').unlink()
        self.put('Assets/source.meta', 'new-current-guid')
        result = self.run_cache('restore')
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual((self.project / 'Assets/source.meta').read_text(), 'new-current-guid')
        self.assertEqual((self.project / 'Library/imported').read_text(), 'import-data')
        for path in ('Library/Bee', 'Library/Il2cppBuildCache', 'Assets/PlayableGenerated', 'braxhorn.keystore'):
            self.assertFalse((self.project / path).exists(), path)

    def test_restores_missing_guid(self):
        self.assertEqual(self.run_cache('save').returncode, 0)
        (self.project / 'Assets/source.meta').unlink()
        self.assertEqual(self.run_cache('restore').returncode, 0)
        self.assertEqual((self.project / 'Assets/source.meta').read_text(), 'original-guid')

    def test_corrupt_cache_does_not_change_project(self):
        self.archive.write_bytes(b'broken-cache')
        self.assertNotEqual(self.run_cache('restore').returncode, 0)
        self.assertEqual((self.project / 'Library/imported').read_text(), 'import-data')
        self.assertEqual((self.project / 'Assets/source.meta').read_text(), 'original-guid')

    def test_cache_miss_allows_clean_import(self):
        self.assertEqual(self.run_cache('restore').returncode, 0)
        self.assertEqual((self.project / 'Assets/source.meta').read_text(), 'original-guid')

if __name__ == '__main__':
    unittest.main()
