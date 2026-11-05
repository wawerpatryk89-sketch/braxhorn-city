"""Offline tests for importer safety: full allowlist, metadata and no clobbering."""
import hashlib
import tempfile
import unittest
import zipfile
from pathlib import Path
from unittest.mock import patch

import import_champion_assets as imp

class ChampionA1ImporterTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root=Path(self.temp.name)
        self.target=self.root/"unity-city"
        self.zip=self.root/"arena.zip"
        self.sample={name:("ORIGINAL:"+name).encode() for name in imp.ALL_SOURCE_FILES}
        self.sample.update({"ASSET-MANIFEST.json":b'{"test":true}',"IMPORT-README.md":b"original fixture"})

    def prepare(self,entries=None):
        with zipfile.ZipFile(self.zip,"w",compression=zipfile.ZIP_DEFLATED) as z:
            for name,data in sorted((entries or self.sample).items()):
                z.writestr(name,data)
        digest=hashlib.sha256(self.zip.read_bytes()).hexdigest()
        self.addCleanup(lambda:None)
        return patch.multiple(imp,DESTINATION=self.target,ARCHIVE_SHA256=digest)

    def test_full_26_original_and_metadata_paths_dry_run_without_writes(self):
        self.assertEqual(len(imp.EXPECTED),11)
        self.assertEqual(len(imp.EXPECTED_META),15)
        with self.prepare():
            found=imp.install(self.zip,True)
        self.assertEqual(len(found),26)
        self.assertTrue(all(line.startswith("DRY-RUN ") for line in found))
        self.assertFalse(self.target.exists())

    def test_install_twice_preserves_all_bytes_and_metadata(self):
        with self.prepare():
            once=imp.install(self.zip,False)
            twice=imp.install(self.zip,False)
        self.assertEqual(len(once),26)
        self.assertEqual(len(twice),26)
        self.assertTrue(all(line.startswith("UNCHANGED ") for line in twice))
        for path,data in self.sample.items():
            if path.startswith("Assets/"):
                self.assertEqual((self.target/path).read_bytes(),data)

    def test_existing_conflicting_asset_is_rejected_without_overwrite(self):
        blocked=sorted(imp.EXPECTED)[0]
        target=self.target/blocked
        target.parent.mkdir(parents=True)
        target.write_bytes(b"DO NOT OVERWRITE")
        with self.prepare(),self.assertRaisesRegex(RuntimeError,"refusing overwrite"):
            imp.install(self.zip,False)
        self.assertEqual(target.read_bytes(),b"DO NOT OVERWRITE")

    def test_missing_meta_is_a_failure(self):
        entries={k:v for k,v in self.sample.items() if k!=sorted(imp.EXPECTED_META)[0]}
        with self.prepare(entries),self.assertRaisesRegex(RuntimeError,"Missing original files"):
            imp.install(self.zip,False)

    def test_unexpected_entry_is_rejected(self):
        entries=dict(self.sample)
        entries["Assets/Source/Mistrzowie/unapproved.bin"]=b"bad"
        with self.prepare(entries),self.assertRaisesRegex(RuntimeError,"unexpected files"):
            imp.install(self.zip,False)

if __name__=="__main__":
    unittest.main()
