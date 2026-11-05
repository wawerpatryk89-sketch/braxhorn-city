"""Six small iPhone-safe ZIPs reconstitute exactly original owner ZIP + bull.

Runs offline using synthetic small data; actual source SHA-256 verified in
the runner restore step before Unity. Does not alter characters, scene or APK.
"""
from pathlib import Path
from hashlib import sha256
from unittest import mock
import io
import json
import sys
import tempfile
import unittest
import zipfile

OBOS=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(OBOS))
import owner_release_assets as owner


def hash_bytes(raw):
    return sha256(raw).hexdigest()


class OwnerSixSmallTests(unittest.TestCase):
    def test_six_zip_unchanged_reconstruction_and_embedded_bull(self):
        raw=bytes(range(256))*22
        bull=b"glTF" + b"\x02\x00\x00\x00" + b"original owner GLB content"
        with tempfile.TemporaryDirectory() as work:
            dest=Path(work)
            size=(len(raw)+5)//6
            mini={}
            for index in range(1,7):
                name=f"BRAXHORN_OBOZ_MALE_01_{index:02d}_Z_06.zip"
                chunk=raw[(index-1)*size:index*size]
                with zipfile.ZipFile(dest/name,"w",zipfile.ZIP_STORED) as output:
                    output.writestr(f"BRAXHORN_MODELE_CZESC_01_Z_05.zip.part{index:02d}",chunk)
                    if index==1:
                        output.writestr(owner.BULL,bull)
                data=(dest/name).read_bytes()
                mini[name]=(len(data),hash_bytes(data))
            with mock.patch.dict(owner.MINI_PARTS,mini,clear=True),\
                 mock.patch.dict(owner.SPLIT_PARTS,{
                     "BRAXHORN_MODELE_CZESC_01_Z_05.zip":(len(raw),hash_bytes(raw))
                 },clear=True),\
                 mock.patch.dict(owner.EXPECTED,{
                     owner.BULL:(len(bull),hash_bytes(bull))
                 },clear=True):
                merged=owner.reconstruct_first_mobile_part(dest)
                self.assertEqual(merged.read_bytes(),raw)
                self.assertEqual((dest/owner.BULL).read_bytes(),bull)

    def test_missing_small_part_is_rejected(self):
        with tempfile.TemporaryDirectory() as work:
            dest=Path(work)
            with self.assertRaises((OSError,ValueError)):
                owner.reconstruct_first_mobile_part(dest)

    def test_corrupt_small_archive_is_rejected(self):
        with tempfile.TemporaryDirectory() as work:
            dest=Path(work)
            with (dest/"BRAXHORN_OBOZ_MALE_01_01_Z_06.zip").open("wb") as out:
                out.write(b"NOT_A_ZIP")
            with self.assertRaises((OSError,ValueError,zipfile.BadZipFile)):
                owner.reconstruct_first_mobile_part(dest)


if __name__=="__main__":
    unittest.main()
