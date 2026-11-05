"""No-network regression tests for single-file 148-sign + 3-mural handoff."""
from pathlib import Path
import hashlib
import json
import tempfile
import unittest
from unittest.mock import patch
import zipfile

import unpack_owner_graphics as package

class OwnerGraphicsOneFileTests(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.root=Path(self.tmp.name)
        self.destination=self.root/"staged"
        self.outer=self.root/package.ARCHIVE_NAME
        self.items={name:b"original-inner-"+str(i).encode()
                    for i,name in enumerate(package.FILES)}
        self.files={name:(len(data),hashlib.sha256(data).hexdigest())
                    for name,data in self.items.items()}
        manifest={
            "signs":148,"murals":3,
            "sources":[{"filename":k,"size":v[0],"sha256":v[1]}
                       for k,v in self.files.items()]
        }
        with zipfile.ZipFile(self.outer,"w",compression=zipfile.ZIP_STORED) as z:
            for name,data in self.items.items():
                z.writestr(name,data,compress_type=zipfile.ZIP_STORED)
            z.writestr("MANIFEST.json",json.dumps(manifest),compress_type=zipfile.ZIP_STORED)
        self.digest=hashlib.sha256(self.outer.read_bytes()).hexdigest()

    def test_one_package_has_all_three_original_archives(self):
        self.assertEqual(len(package.FILES),3)
        self.assertEqual(set(package.FILES),{
            "MIASTO_MISTRZOW_SZYLDY_01_06_UNITY.zip",
            "MIASTO_MISTRZOW_SZYLDY_07_13_UNITY.zip",
            "ARENA_A1_TRZY_MURALE_ORIGINALY.zip"
        })
        with patch.object(package,"FILES",self.files),patch.object(package,"ARCHIVE_SHA256",self.digest):
            report=package.unpack(self.outer,self.destination,dry_run=True)
        self.assertEqual(report["status"],"DRY_RUN_PASS")
        self.assertEqual(report["signs"],148)
        self.assertEqual(report["murals"],3)
        self.assertFalse(self.destination.exists())

    def test_extracts_unchanged_source_bytes_and_is_idempotent(self):
        with patch.object(package,"FILES",self.files),patch.object(package,"ARCHIVE_SHA256",self.digest):
            first=package.unpack(self.outer,self.destination)
            second=package.unpack(self.outer,self.destination)
        self.assertEqual(first["status"],"UNPACKED_AND_VERIFIED")
        self.assertEqual(second["status"],"UNPACKED_AND_VERIFIED")
        for name,data in self.items.items():
            self.assertEqual((self.destination/name).read_bytes(),data)

    def test_refuses_differing_existing_file_without_overwrite(self):
        self.destination.mkdir(parents=True)
        name=next(iter(self.files))
        (self.destination/name).write_bytes(b"changed by another worker")
        with patch.object(package,"FILES",self.files),patch.object(package,"ARCHIVE_SHA256",self.digest):
            with self.assertRaisesRegex(ValueError,"Refusing overwrite"):
                package.unpack(self.outer,self.destination)
        self.assertEqual((self.destination/name).read_bytes(),b"changed by another worker")

    def test_refuses_wrong_checksum_before_writing(self):
        with patch.object(package,"FILES",self.files),patch.object(package,"ARCHIVE_SHA256","0"*64):
            with self.assertRaisesRegex(ValueError,"SHA-256 mismatch"):
                package.unpack(self.outer,self.destination)
        self.assertFalse(self.destination.exists())

if __name__=="__main__":
    unittest.main()
