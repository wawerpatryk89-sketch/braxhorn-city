"""Fast, offline synthetic-GLB regression for the owner's original P1 towel UV issue.

No Unity, no private ZIP download, no scene rebuild, no APK and no network.
"""
import json
import struct
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

from repair_pool_p1_towels import (
    UnsafeTowelUvConversion, _parse_glb, derive_safe_uv_import, sha256
)
import import_pool_p1_owner_assets as importer


def sample_glb(extra=True, referenced=False, compressed=False):
    attrs={"POSITION":0,"NORMAL":1,"TEXCOORD_0":2,"TEXCOORD_1":3}
    if extra:
        attrs.update({"TEXCOORD_2":4,"TEXCOORD_3":5})
    primitive={"attributes":attrs,"indices":6,"material":0}
    if compressed:
        primitive["extensions"]={"KHR_draco_mesh_compression":{"bufferView":1,"attributes":{"POSITION":0}}}
    mat={"pbrMetallicRoughness":{
        "baseColorTexture":{"index":0,"texCoord":2 if referenced else 0}
    }}
    doc={
        "asset":{"version":"2.0","generator":"BRAXHORN synthetic test"},
        "scene":0,"scenes":[{"nodes":[0]}],
        "nodes":[{"mesh":0,"name":"Original towel stack"}],
        "meshes":[{"primitives":[primitive]}],
        "materials":[mat],
        "textures":[{"source":0}],"images":[{"bufferView":0,"mimeType":"image/png"}],
        "bufferViews":[{"buffer":0,"byteOffset":0,"byteLength":8}],
        "buffers":[{"byteLength":8}],
        "accessors":[{"bufferView":0,"componentType":5126,"count":1,"type":"VEC3"} for _ in range(7)],
    }
    return glb(doc)


def glb(doc):
    source=json.dumps(doc,separators=(",",":"),ensure_ascii=False).encode("utf-8")
    source+=b" "*((-len(source))%4)
    binary=b"12345678"
    body=struct.pack("<I4s",len(source),b"JSON")+source
    body+=struct.pack("<I4s",len(binary),b"BIN\x00")+binary
    return struct.pack("<4sII",b"glTF",2,12+len(body))+body


class TowelUvCompatibility(unittest.TestCase):
    def test_strip_only_unused_uv2_and_uv3_and_keep_original_geometry(self):
        raw=sample_glb()
        original,chunks=_parse_glb(raw)
        derived,report=derive_safe_uv_import(raw)
        converted,after_chunks=_parse_glb(derived)
        self.assertEqual(report["status"],"UNUSED_UV_CHANNELS_REMOVED_FROM_IMPORT_COPY")
        self.assertEqual(len(report["removedUnusedUvAttributes"]),2)
        attrs=converted["meshes"][0]["primitives"][0]["attributes"]
        self.assertEqual(set(attrs),{"POSITION","NORMAL","TEXCOORD_0","TEXCOORD_1"})
        self.assertEqual(after_chunks[1:],chunks[1:])
        for field in ("asset","materials","nodes","textures","images","bufferViews","buffers","accessors"):
            self.assertEqual(converted[field],original[field],field)
        self.assertEqual(converted["meshes"][0]["primitives"][0]["indices"],6)
        self.assertEqual(report["originalSha256"],sha256(raw))
        self.assertEqual(report["importSha256"],sha256(derived))

    def test_do_not_remove_uv_channels_used_by_material(self):
        with self.assertRaisesRegex(UnsafeTowelUvConversion,"change appearance"):
            derive_safe_uv_import(sample_glb(referenced=True))

    def test_do_not_modify_draco_compressed_uv_mesh(self):
        with self.assertRaisesRegex(UnsafeTowelUvConversion,"Draco"):
            derive_safe_uv_import(sample_glb(compressed=True))

    def test_no_extra_uv_is_byte_for_byte_identical(self):
        original=sample_glb(extra=False)
        result,report=derive_safe_uv_import(original)
        self.assertEqual(result,original)
        self.assertEqual(report["status"],"ALREADY_COMPATIBLE")

    def test_reject_malformed_header(self):
        with self.assertRaises(UnsafeTowelUvConversion):
            derive_safe_uv_import(b"not glTF")

    def test_installer_archives_original_and_is_idempotent(self):
        original=sample_glb()
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp)
            sources={
                importer.TOWEL_ASSET:original,
                importer.TOWEL_ASSET+".meta":b"fileFormatVersion: 2\nguid: 1234567890abcdef1234567890abcdef\n",
            }
            with patch.object(importer,"verify",return_value=sources):
                importer.install(Path(importer.ARCHIVE_NAME),destination=root)
                native=(root/importer.TOWEL_BACKUP)
                working=(root/importer.TOWEL_ASSET)
                self.assertEqual(native.read_bytes(),original)
                self.assertNotEqual(working.read_bytes(),original)
                self.assertTrue((root/(importer.TOWEL_ASSET+".meta")).exists())
                report=json.loads((root/importer.TOWEL_REPORT).read_text())
                self.assertEqual(report["status"],"UNUSED_UV_CHANNELS_REMOVED_FROM_IMPORT_COPY")
                self.assertFalse(report["unityTested"])
                importer.install(Path(importer.ARCHIVE_NAME),destination=root)
                self.assertEqual(native.read_bytes(),original)

    def test_installer_refuses_to_alter_material_uv2_source(self):
        original=sample_glb(referenced=True)
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp)
            with patch.object(importer,"verify",return_value={importer.TOWEL_ASSET:original}):
                importer.install(Path(importer.ARCHIVE_NAME),destination=root)
                self.assertEqual((root/importer.TOWEL_ASSET).read_bytes(),original)
                self.assertEqual((root/importer.TOWEL_BACKUP).read_bytes(),original)
                report=json.loads((root/importer.TOWEL_REPORT).read_text())
                self.assertIn("MANUAL_UV_REVIEW_REQUIRED",report["status"])


if __name__=="__main__":
    unittest.main(verbosity=2)
