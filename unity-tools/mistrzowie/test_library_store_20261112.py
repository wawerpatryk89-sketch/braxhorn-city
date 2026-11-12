"""The import memory kept outside the 10 GB Actions cache (owner 12.11.2026 21:20: "przecież ja już od dawna ustawiałem ...
dlaczego to nie funkcjonuje cały czas?"): the 6.2 GB Library was evicted by the 5.6 GB PBR maps and the other regions'
caches before every next build."""
import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
WF = (ROOT / '.github/workflows/mistrzowie-apk.yml').read_text(encoding='utf-8')
ST = (ROOT / 'unity-tools/mistrzowie/library_store.sh').read_text(encoding='utf-8')


class LibraryStore(unittest.TestCase):
    def test_library_no_longer_in_the_actions_cache(self):
        self.assertNotIn('champions-library-android-', WF)
        self.assertNotIn('Save isolated encrypted Champions Library', WF)

    def test_restored_and_saved_in_both_jobs(self):
        self.assertEqual(WF.count('library_store.sh restore unity-city'), 2)
        self.assertEqual(WF.count('library_store.sh save unity-city'), 2)

    def test_saved_after_every_unity_run_before_the_photos(self):
        save = WF.index('library_store.sh save unity-city "${{ steps.preflight.outputs.unity }}"')
        self.assertLess(WF.index('id: apk_delivery'), save)
        self.assertLess(save, WF.index('- name: Optional real player for visual review after APK delivery'))
        block = WF[WF.rindex('- name: Save Champions import memory', 0, save):save]
        self.assertNotIn('inputs.zdjecia', block)          # photos on or off, the memory is kept
        self.assertIn("steps.unity_build.outcome == 'failure'", block)

    def test_store_is_safe(self):
        self.assertIn('split -b "${LIBRARY_PART_SIZE:-1900m}"', ST)                 # release assets < 2 GiB
        self.assertIn('sha256sum -c --quiet', ST)
        self.assertIn('profile $stored != $profile; clean import', ST)
        # the manifest goes up after the parts, old parts go only after it
        up_parts = ST.index('--clobber "$work"/champions-library-"$run".part*')
        up_manifest = ST.index('--clobber "$work/champions-library.sha256"')
        delete = ST.index('delete-asset')
        self.assertLess(up_parts, up_manifest)
        self.assertLess(up_manifest, delete)
        self.assertIn('bash "$here/library_cache.sh" save', ST)                     # encrypted as before


if __name__ == '__main__':
    unittest.main()
