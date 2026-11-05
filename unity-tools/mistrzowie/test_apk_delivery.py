"""Receipt validation tests with synthetic APK bytes and mocked external tools."""
import contextlib
import hashlib
import io
import json
import os
from pathlib import Path
import tempfile
import unittest
import zipfile
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
                             bytes=len(self.data), commit="a" * 40, versionCode=8501, runId="85")
        (self.output / self.name).write_bytes(self.data)
        (self.output / "manifest.json").write_text(json.dumps(self.manifest))
        env = dict(GITHUB_REPOSITORY="owner/repo", APK_ARTIFACT_ID="123", APK_ARTIFACT_URL="https://github.com/artifact",
                   APK_RELEASE_TAG="unique-test", GITHUB_STEP_SUMMARY=str(self.root / "summary.md"),
                   GITHUB_SHA="a" * 40, GITHUB_RUN_ID="85", GITHUB_RUN_ATTEMPT="1")
        for p in (patch.object(delivery, "ROOT", self.root), patch.object(delivery, "OUT", self.output), patch.dict(os.environ, env)):
            p.start(); self.addCleanup(p.stop)
        self.remote_name, self.remote_data = self.name, self.data
        download = patch.object(delivery.subprocess, "run", side_effect=self.download)
        download.start(); self.addCleanup(download.stop)

    def download(self, args, stdout, check):
        self.assertTrue(check)
        self.assertEqual(args, ["gh", "api", "repos/owner/repo/actions/artifacts/123/zip"])
        with zipfile.ZipFile(stdout, "w") as archive:
            archive.writestr(self.remote_name, self.remote_data)

    def artifact(self):
        return dict(id=123, expired=False, size_in_bytes=len(self.data)+100,
                    workflow_run={"head_sha": self.manifest["commit"], "id": 85})

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

    def test_same_size_wrong_remote_apk_is_not_delivery(self):
        self.remote_data = b"x" * len(self.data)
        with patch.object(delivery, "api", side_effect=[self.artifact(), RuntimeError("no release")]), self.assertRaisesRegex(RuntimeError, "SHA-256 differs"):
            self.verify()

    def test_missing_apk_in_large_artifact_is_not_delivery(self):
        self.remote_name = "diagnostics.log"
        with patch.object(delivery, "api", side_effect=[self.artifact(), RuntimeError("no release")]), self.assertRaisesRegex(RuntimeError, "APK missing"):
            self.verify()

    def test_same_commit_different_run_is_rejected(self):
        artifact = self.artifact(); artifact["workflow_run"]["id"] = 84
        with patch.object(delivery, "api", side_effect=[artifact, RuntimeError("no release")]), self.assertRaisesRegex(RuntimeError, "different workflow run"):
            self.verify()

    def test_valid_release_survives_corrupt_artifact(self):
        self.remote_data = b"x" * len(self.data)
        with patch.object(delivery, "api", side_effect=[self.artifact(), self.release()]):
            report = self.verify()
        self.assertNotIn("artifactId", report["delivery"])
        self.assertIn("apkUrl", report["delivery"])

    def test_stage_checks_real_identity_and_certificate_receipts(self):
        apk = self.root / "build/Android/braxhorn-mistrzowie.apk"
        apk.parent.mkdir(parents=True); apk.write_bytes(self.data)
        project = self.root / "unity-city"; project.mkdir()
        config = json.loads(Path(__file__).with_name('unity-build.json').read_text())
        config_path = self.root/'unity-tools/mistrzowie/unity-build.json'
        config_path.parent.mkdir(parents=True); config_path.write_text(json.dumps(config))
        settings = project/'ProjectSettings'; settings.mkdir()
        (settings/'ProjectVersion.txt').write_text(f"m_EditorVersion: {config['unity']}\n"
            f"m_EditorVersionWithRevision: {config['unity']} ({config['revision']})\n")
        (project / "braxhorn.versioncode").write_text("8501")
        (project / "mistrzowie.source-commit").write_text("c" * 40 + "\n")
        cert = b"synthetic certificate DER"
        cert_sha = hashlib.sha256(cert).hexdigest()
        responses = ["package: name='com.braxhorn.mistrzowie' versionCode='8501'", cert,
                     "Signer #1 certificate SHA-256 digest: " + cert_sha,
                     "Signer #1 certificate SHA-256 digest: " + cert_sha]
        with patch.object(delivery, "sdk_tool", side_effect=lambda n:n), patch.object(delivery, "command", side_effect=responses), contextlib.redirect_stdout(io.StringIO()):
            delivery.stage()
        manifest = json.loads((self.output / "manifest.json").read_text())
        self.assertEqual(manifest["certificateSha256"], cert_sha)
        self.assertEqual(manifest["commit"], "c" * 40)
        self.assertEqual(manifest["workflowCommit"], "a" * 40)
        self.assertEqual(manifest['unity'], config['unity'])
        self.assertIn("ccccccc", manifest["file"])
        self.assertEqual((self.output / manifest["file"]).read_bytes(), self.data)
        responses[2] = "Signer #1 certificate SHA-256 digest: " + "0" * 64
        # Wrong signature must never pass without a successful correction.
        with patch.object(delivery, "sdk_tool", side_effect=lambda n:n), \
             patch.object(delivery, "command", side_effect=responses), \
             patch.object(delivery, "resign_with_existing_key",
                          side_effect=RuntimeError("existing key could not re-sign")), \
             self.assertRaisesRegex(RuntimeError, "existing key could not re-sign"):
            delivery.stage()

    def test_re_sign_uses_existing_keystore_and_checks_identity_before_replacement(self):
        source = self.output / "unverified-staged.apk"
        source.write_bytes(self.data)
        fingerprint = "b" * 64
        commands = []
        def mocked(*args, **kwargs):
            commands.append(args)
            if args[1] == "sign":
                self.assertEqual(args[args.index("--ks") + 1],
                                 str(self.root / "unity-city/braxhorn.keystore"))
                self.assertEqual(args[args.index("--ks-key-alias") + 1], "braxhorn")
                self.assertTrue(args[args.index("--ks-pass") + 1].startswith("file:"))
                # Regression: one-line repository password must not be consumed twice.
                self.assertNotIn("--key-pass", args)
                Path(args[args.index("--out") + 1]).write_bytes(b"signed APK" * 300)
                return ""
            if args[1] == "verify":
                return "Signer #1 certificate SHA-256 digest: " + fingerprint
            if args[1] == "dump":
                return "package: name='com.braxhorn.mistrzowie' versionCode='8501'"
            raise AssertionError(args)
        with patch.object(delivery, "sdk_tool", side_effect=lambda n:n), \
             patch.object(delivery, "command", side_effect=mocked):
            delivery.resign_with_existing_key(
                source, ("com.braxhorn.mistrzowie", 8501), fingerprint)
        self.assertEqual(source.read_bytes(), b"signed APK" * 300)
        self.assertEqual([args[1] for args in commands], ["sign", "verify", "dump"])

    def test_invalid_re_sign_keeps_staged_apk_unmodified(self):
        source = self.output / "unverified-staged.apk"
        source.write_bytes(self.data)
        def mocked(*args, **kwargs):
            if args[1] == "sign":
                Path(args[args.index("--out") + 1]).write_bytes(b"signed APK" * 300)
                return ""
            if args[1] == "verify":
                return "Signer #1 certificate SHA-256 digest: " + "0" * 64
            raise AssertionError(args)
        with patch.object(delivery, "sdk_tool", side_effect=lambda n:n), \
             patch.object(delivery, "command", side_effect=mocked), \
             self.assertRaisesRegex(RuntimeError, "still fails"):
            delivery.resign_with_existing_key(
                source, ("com.braxhorn.mistrzowie", 8501), "b" * 64)
        self.assertEqual(source.read_bytes(), self.data)


    def test_re_sign_rejects_original_unity_apk(self):
        source = self.root / "build/Android/braxhorn-mistrzowie.apk"
        source.parent.mkdir(parents=True)
        source.write_bytes(self.data)
        with self.assertRaisesRegex(RuntimeError, "writable staged APK"):
            delivery.resign_with_existing_key(
                source, ("com.braxhorn.mistrzowie", 8501), "b" * 64)
        self.assertEqual(source.read_bytes(), self.data)

    def test_stage_resigns_inside_writable_staging_not_readonly_unity_directory(self):
        apk = self.root / "build/Android/braxhorn-mistrzowie.apk"
        apk.parent.mkdir(parents=True)
        apk.write_bytes(self.data)
        project = self.root / "unity-city"
        project.mkdir()
        config = json.loads(Path(__file__).with_name("unity-build.json").read_text())
        config_path = self.root / "unity-tools/mistrzowie/unity-build.json"
        config_path.parent.mkdir(parents=True)
        config_path.write_text(json.dumps(config))
        settings = project / "ProjectSettings"
        settings.mkdir()
        (settings / "ProjectVersion.txt").write_text(
            f"m_EditorVersion: {config['unity']}\n"
            f"m_EditorVersionWithRevision: {config['unity']} ({config['revision']})\n")
        (project / "braxhorn.versioncode").write_text("8501")
        (project / "mistrzowie.source-commit").write_text("c" * 40 + "\n")
        signed = b"APK signed with original key" * 120
        cert = b"synthetic certificate DER"
        cert_sha = hashlib.sha256(cert).hexdigest()
        badging = "package: name='com.braxhorn.mistrzowie' versionCode='8501'"
        sign_calls = []

        def fake_command(*args, **kwargs):
            if args[0] == "keytool":
                return cert
            if args[0] == "aapt" and args[1:3] == ("dump", "badging"):
                return badging
            if args[:2] == ("apksigner", "verify"):
                fingerprint = cert_sha if Path(args[-1]).read_bytes() == signed else "0" * 64
                return "Signer #1 certificate SHA-256 digest: " + fingerprint
            if args[:2] == ("apksigner", "sign"):
                output = Path(args[args.index("--out") + 1])
                self.assertEqual(output.parent.parent.resolve(), self.output.resolve())
                self.assertEqual(args[args.index("--ks-key-alias") + 1], "braxhorn")
                output.write_bytes(signed)
                sign_calls.append(output)
                return ""
            raise AssertionError(args)

        # A root-owned build directory cannot be used as a signing temp folder.
        apk.parent.chmod(0o555)
        try:
            with patch.object(delivery, "sdk_tool", side_effect=lambda n: n), \
                 patch.object(delivery, "command", side_effect=fake_command), \
                 contextlib.redirect_stdout(io.StringIO()):
                delivery.stage()
        finally:
            apk.parent.chmod(0o755)
        self.assertEqual(len(sign_calls), 1)
        self.assertEqual(apk.read_bytes(), self.data)
        manifest = json.loads((self.output / "manifest.json").read_text())
        self.assertEqual((self.output / manifest["file"]).read_bytes(), signed)
        self.assertEqual(manifest["certificateSha256"], cert_sha)

if __name__ == "__main__":
    unittest.main()

