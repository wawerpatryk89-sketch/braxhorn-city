"""Fast regression: original owner Blender archives contain texture files too.
Pure Python test, NO Unity and NO owners' private meshes needed.
"""
import gzip
import hashlib
import io
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch
import zipfile

sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
import owner_blender_extract as importer


class BlenderNestedZipPreflight(unittest.TestCase):
    def test_blend_with_valid_original_textures_is_accepted(self):
        original=b"BLENDER-v4\\x00" + b"test original geometry" * 16
        with tempfile.TemporaryDirectory() as tmp:
            inner=io.BytesIO()
            with zipfile.ZipFile(inner,"w") as z:
                z.writestr("grass_test.blend",gzip.compress(original))
                z.writestr("textures/grass_test_diff_2k.jpg",b"JPEG")
                z.writestr("textures/grass_test_alpha_2k.png",b"PNG")
                z.writestr("textures/grass_test_nor_gl_2k.exr",b"EXR")
            package=Path(tmp)/"owner.zip"
            with zipfile.ZipFile(package,"w") as z:
                z.writestr("ROOT/grass_test.blend.zip",inner.getvalue())
            sha=hashlib.sha256(original).hexdigest()
            with patch.dict(importer.HASHES,{"grass_test":(len(original),sha)},clear=True):
                found=importer.retrieve([package])
            self.assertEqual(found["grass_test"],original)

    def test_unknown_member_must_be_rejected(self):
        original=b"BLENDER-v4\\x00" + b"source" * 16
        with tempfile.TemporaryDirectory() as tmp:
            inner=io.BytesIO()
            with zipfile.ZipFile(inner,"w") as z:
                z.writestr("grass_test.blend",gzip.compress(original))
                z.writestr("unexpected.exe",b"not owner texture")
            package=Path(tmp)/"owner.zip"
            with zipfile.ZipFile(package,"w") as z:
                z.writestr("ROOT/grass_test.blend.zip",inner.getvalue())
            sha=hashlib.sha256(original).hexdigest()
            with patch.dict(importer.HASHES,{"grass_test":(len(original),sha)},clear=True):
                with self.assertRaises(ValueError):
                    importer.retrieve([package])


if __name__=="__main__":
    unittest.main()
