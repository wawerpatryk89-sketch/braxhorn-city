"""Short offline regression: no visual changes to a crane with material UV2+.

Synthetic glTF binary only: no owner assets, no Unity editor, no APK.
"""
import hashlib
import json
import os
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

import prepare_harbor_crane_compat as crane
import struct


# Synthetic GLB builder (moved here 9.11 when the pool P1 tests were removed).
def glb(doc):
    source=json.dumps(doc,separators=(",",":"),ensure_ascii=False).encode("utf-8")
    source+=b" "*((-len(source))%4)
    binary=b"12345678"
    body=struct.pack("<I4s",len(source),b"JSON")+source
    body+=struct.pack("<I4s",len(binary),b"BIN\x00")+binary
    return struct.pack("<4sII",b"glTF",2,12+len(body))+body



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





class CraneImportSafetyTests(unittest.TestCase):
    def setup_paths(self, root):
        return (root/"crane.glb", root/"owner-original.glb", root/"report.json")

    def test_referenced_uv2_is_advisory_and_never_rewritten(self):
        original = sample_glb(extra=True, referenced=True)
        with tempfile.TemporaryDirectory() as directory:
            source, backup, report_path = self.setup_paths(Path(directory))
            source.write_bytes(original)
            with patch.object(crane,"SOURCE",source), patch.object(crane,"BACKUP",backup), patch.object(crane,"REPORT",report_path):
                result=crane.prepare(apply_runner_only=False)
            self.assertEqual(result["status"],"MANUAL_UV_MAPPING_REQUIRED_SOURCE_UNCHANGED")
            self.assertFalse(result["sourceImportCompatible"])
            self.assertFalse(result["unityImporterTested"])
            self.assertFalse(result["appliedToRunnerWorkspace"])
            self.assertIn("change appearance",result["reason"])
            self.assertIn("UV",result["reason"])
            self.assertEqual(source.read_bytes(),original)
            self.assertFalse(backup.exists())
            saved=json.loads(report_path.read_text())
            self.assertEqual(saved["originalSha256"],hashlib.sha256(original).hexdigest())
            self.assertEqual(saved["derivedSha256"],saved["originalSha256"])

    def test_apply_runner_only_still_refuses_unsafe_uv_stripping(self):
        original=sample_glb(extra=True,referenced=True)
        with tempfile.TemporaryDirectory() as directory:
            source, backup, report_path=self.setup_paths(Path(directory))
            source.write_bytes(original)
            with patch.object(crane,"SOURCE",source),patch.object(crane,"BACKUP",backup),patch.object(crane,"REPORT",report_path),patch.dict(os.environ,{"GITHUB_ACTIONS":"true"}):
                result=crane.prepare(apply_runner_only=True)
            self.assertEqual(source.read_bytes(),original)
            self.assertFalse(backup.exists())
            self.assertFalse(result["appliedToRunnerWorkspace"])

    def test_only_unreferenced_extra_uv_can_be_safely_omitted(self):
        original=sample_glb(extra=True,referenced=False)
        with tempfile.TemporaryDirectory() as directory:
            source, backup, report_path=self.setup_paths(Path(directory))
            source.write_bytes(original)
            with patch.object(crane,"SOURCE",source),patch.object(crane,"BACKUP",backup),patch.object(crane,"REPORT",report_path),patch.dict(os.environ,{"GITHUB_ACTIONS":"true"}):
                result=crane.prepare(apply_runner_only=True)
            self.assertEqual(result["status"],"RUNNER_ONLY_COMPATIBLE_IMPORT_COPY_READY")
            self.assertEqual(backup.read_bytes(),original)
            self.assertNotEqual(source.read_bytes(),original)
            self.assertEqual(hashlib.sha256(source.read_bytes()).hexdigest(),result["derivedSha256"])
            self.assertTrue(result["originalMaterialAndTextureDefinitionsPreserved"])
            self.assertFalse(result["unityImporterTested"])


if __name__ == "__main__":
    unittest.main(verbosity=2)
