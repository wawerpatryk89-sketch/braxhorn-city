"""Receipt validation tests with synthetic APK bytes and mocked external tools."""
import contextlib
import hashlib
import io
import json
import os
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

import apk_delivery as delivery


class ApkDeliveryTests(unittest.TestCase):
    def setUp(self):
        tmp = tempfile.TemporaryDirectory()
        self.addCleanup(tmp.cleanup)
        self.root = Path(tmp.name)
        self.output = self.root / "mistrzowie-apk"
        self.output.mkdir()
        self.data = b"synthetic APK" * 200
        self.name = "braxhorn-mistrzowie-test.apk"
        self.manifest = dict(file=self.name, sha256=hashlib.sha256(self.data).hexdigest(),
                             bytes=len(self.data), commit="a" * 40, versionCode=8501)
        (self.output / self.name).write_bytes(self.data)
        (self.output / "manifest.json").write_text(json.dumps(self.manifest))
        env = dict(GITHUB_REPOSITORY="owner/repo", APK_ARTIFACT_ID="123", APK_ARTIFACT_URL="https://github.com/artifact",
                   APK_RELEASE_TAG="unique-test", GITHUB_STEP_SUMMARY=str(self.root / "summary.md"),
                   GITHUB_SHA="a" * 40, GITHUB_RUN_ID="85", GITHUB_RUN_ATTEMPT="1")
        for p in (patch.object(delivery, "ROOT", self.root), patch.object(delivery, "OUT", self.output), patch.dict(os.environ, env)):
            p.start(); self.addCleanup(p.stop)

    def artifact(self):
        return dict(id=123, expired=False, size_in_bytes=len(self.data)+100,
                    workflow_run={"head_sha": self.manifest["commit"]})

    def release(self):
        return dict(html_url="https://github.com/release", assets=[dict(name=self.name, size=len(self.data),
                    digest="sha256:" + self.manifest["sha256"], browser_download_url="https://github.com/apk")])

    def verify(self):
        with contextlib.redirect_stdout(io.StringIO()):
            delivery.verify()
        return json.loads((self.root / "mistrzowie-evidence/apk-delivery.json").read_text())

    def test_both_deliveries_confirmed_with_hash_and_commit(self):
        with patch.object(delivery, "api", side_effect=[self.artifact(), self.release()]):
            report = self.verify()
        self.assertEqual(report["delivery"]["artifactId"], 123)
        self.assertEqual(report["delivery"]["apkUrl"], "https://github.com/apk")
        self.assertEqual(report["warnings"], [])

    def test_release_survives_artifact_failure(self):
        with patch.object(delivery, "api", side_effect=[RuntimeError("upload failed"), self.release()]):
            report = self.verify()
        self.assertIn("apkUrl", report["delivery"])
        self.assertNotIn("artifactId", report["delivery"])
        self.assertEqual(len(report["warnings"]), 1)

    def test_launcher_commit_can_differ_from_game_commit(self):
        self.manifest["workflowCommit"] = "b" * 40
        (self.output / "manifest.json").write_text(json.dumps(self.manifest))
        artifact = self.artifact()
        artifact["workflow_run"]["head_sha"] = "b" * 40
        with patch.object(delivery, "api", side_effect=[artifact, RuntimeError("release failed")]):
            report = self.verify()
        self.assertEqual(report["delivery"]["artifactId"], 123)
        self.assertEqual(report["commit"], "a" * 40)
        self.assertEqual(report["workflowCommit"], "b" * 40)
        # The game SHA cannot stand in for the launcher's run identity.
        with patch.object(delivery, "api", side_effect=[self.artifact(), RuntimeError("release failed")]), self.assertRaisesRegex(RuntimeError, "delivery failed"):
            self.verify()

    def test_wrong_commit_and_wrong_hash_cannot_count_as_delivery(self):
        artifact, release = self.artifact(), self.release()
        artifact["workflow_run"]["head_sha"] = "b" * 40
        release["assets"][0]["digest"] = "sha256:" + "0" * 64
        with patch.object(delivery, "api", side_effect=[artifact, release]), self.assertRaisesRegex(RuntimeError, "delivery failed"):
            self.verify()

    def test_mutated_local_apk_rejected_before_network(self):
        (self.output / self.name).write_bytes(b"changed")
        with patch.object(delivery, "api") as api, self.assertRaisesRegex(RuntimeError, "changed after validation"):
            self.verify()
        api.assert_not_called()

    def test_stage_checks_real_identity_and_certificate_receipts(self):
        apk = self.root / "build/Android/braxhorn-mistrzowie.apk"
        apk.parent.mkdir(parents=True); apk.write_bytes(self.data)
        project = self.root / "unity-city"; project.mkdir()
        (project / "braxhorn.versioncode").write_text("8501")
        (project / "mistrzowie.source-commit").write_text("c" * 40 + "\n")
        cert = b"synthetic certificate DER"
        cert_sha = hashlib.sha256(cert).hexdigest()
        responses = ["package: name='com.braxhorn.mistrzowie' versionCode='8501'", cert,
                     "Signer #1 certificate SHA-256 digest: " + cert_sha]
        with patch.object(delivery, "sdk_tool", side_effect=lambda n:n), patch.object(delivery, "command", side_effect=responses), contextlib.redirect_stdout(io.StringIO()):
            delivery.stage()
        manifest = json.loads((self.output / "manifest.json").read_text())
        self.assertEqual(manifest["certificateSha256"], cert_sha)
        self.assertEqual(manifest["commit"], "c" * 40)
        self.assertEqual(manifest["workflowCommit"], "a" * 40)
        self.assertIn("ccccccc", manifest["file"])
        self.assertEqual((self.output / manifest["file"]).read_bytes(), self.data)
        responses[2] = "Signer #1 certificate SHA-256 digest: " + "0" * 64
        with patch.object(delivery, "sdk_tool", side_effect=lambda n:n), patch.object(delivery, "command", side_effect=responses), self.assertRaisesRegex(RuntimeError, "existing repository certificate"):
            delivery.stage()


if __name__ == "__main__":
    unittest.main()
