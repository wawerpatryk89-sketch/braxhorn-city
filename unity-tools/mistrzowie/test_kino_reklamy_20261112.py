"""G1 cinema pre-show (owner 12.11 02:49: three short films "możesz dać przed kinem jako reklama").

Inside the hall only (11.11 rule: no films on streets): the ads run before every film, rotated, AKCJA skips them, a missing
ad is skipped. The clips live only encrypted (public repo) with the real brand marks blurred."""
import hashlib
import json
import subprocess
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
KINO = ROOT / 'unity-city/Assets/StreamingAssets/Mistrzowie/Kino'
PACK = ROOT / 'unity-city/Prywatne/mistrzowie-kino-reklamy-20261112'


class KinoReklamy(unittest.TestCase):
    def test_repertoire_lists_the_ads(self):
        rep = json.loads((KINO / 'repertuar.json').read_text())
        self.assertEqual(rep['reklamy'], ['reklama_01.mp4', 'reklama_02.mp4', 'reklama_03.mp4'])
        self.assertEqual(len(rep['filmy']), 2)

    def test_pack_and_staging_agree(self):
        m = json.loads((PACK / 'manifest.json').read_text())
        for part in m['parts']:
            self.assertLess(part['bytes'], 100 * 1024 * 1024)
            self.assertEqual(hashlib.sha256((PACK / part['name']).read_bytes()).hexdigest(), part['sha256'])
        self.assertTrue((PACK / 'recipient.crt').exists() and (PACK / 'recipient.pem.enc').exists())
        inst = (ROOT / 'unity-tools/mistrzowie/install_owner_media.py').read_text()
        self.assertIn('open_pack("mistrzowie-kino-reklamy-20261112"', inst)
        for n in ('reklama_01.mp4', 'reklama_02.mp4', 'reklama_03.mp4'):
            self.assertIn('"%s": "%s"' % (n, m['files']['kino/' + n]), inst)
        for k in ('maski/maski_6923.json', 'maski/maski_6924.json', 'maski/maski_6925.json'):
            self.assertIn(k, m['files'])                                      # the blurred brand marks are reproducible

    def test_runtime_plays_them_first(self):
        s = (ROOT / 'unity-city/Assets/Mistrzowie/BraxChampionsKino.cs').read_text()
        self.assertIn('public string[] reklamy', s)
        self.assertIn('kolejka.Add(adres)', s)                                # the film after the ads
        self.assertIn('void Dalej()', s)
        self.assertIn('if (Reklama) { Dalej(); return; }', s)                # end of an ad -> next item
        self.assertIn('"KINO · POMIŃ REKLAMY"', s)

    def test_no_raw_clips_in_git(self):
        tracked = subprocess.run(['git', 'ls-files', '*.mp4'], cwd=ROOT, capture_output=True, text=True).stdout.split()
        self.assertFalse([t for t in tracked if 'reklama_0' in t or '692' in t])
        self.assertIn('*.mp4', (KINO / '.gitignore').read_text())


if __name__ == '__main__':
    unittest.main()
