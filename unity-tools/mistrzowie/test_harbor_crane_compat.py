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
from test_pool_p1_towels_uv import sample_glb


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
            self.assertIn("TEXCOORD_2+",result["reason"])
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
