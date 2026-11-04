import io
import json
import os
from pathlib import Path
import tarfile
import tempfile
import unittest
from unittest.mock import patch

import unity_cache as cache


class CacheTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.root = Path(self.tmp.name)
        self.project = self.root / "project"
        self.project.mkdir()
        self.write("Packages/manifest.json", '{"dependencies":{}}')
        self.write("Library/Bee/artifacts/compiled.o", "already compiled")
        self.write("Library/PlayerDataCache/data", "already serialized")
        self.write("Library/Artifacts/model", "already imported")
        self.write("Assets/Source/model.fbx.meta", "guid: stable-model")
        self.write("Assets/Source/private.fbx", "do not archive the source")
        self.write("Assets/PlayableGenerated/old.unity.meta", "orphan meta excluded")
        self.write("braxhorn.keystore.pass", "not included")
        self.expected = cache.profile(self.project, "osiedle", "6000.0.58f2")
        self.env = patch.dict(os.environ, {"KLUCZ": "synthetic-test-password"})
        self.env.start()

    def tearDown(self):
        self.env.stop()
        self.tmp.cleanup()

    def write(self, name, content):
        path = self.project / name
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(content)
        return path

    def target(self):
        target = self.root / "target"
        target.mkdir()
        return target

    def malicious(self, name, kind=tarfile.REGTYPE, include_header=True):
        stream = io.BytesIO()
        with tarfile.open(fileobj=stream, mode="w") as tar:
            if include_header:
                data = json.dumps({"profile": self.expected}).encode()
                info = tarfile.TarInfo(cache.HEADER)
                info.size = len(data)
                tar.addfile(info, io.BytesIO(data))
            entry = tarfile.TarInfo(name)
            entry.type = kind
            entry.linkname = "/tmp/should-not-exist"
            entry.size = 1 if kind == tarfile.REGTYPE else 0
            tar.addfile(entry, io.BytesIO(b"x") if entry.size else None)
        stream.seek(0)
        return stream

    def test_round_trip_keeps_compilation_and_import_results(self):
        archive = self.root / "cache.enc"
        receipt = cache.pack(self.project, archive, self.expected)
        target = self.target()
        result = cache.restore(target, archive, self.expected, sha256=receipt["sha256"])
        self.assertTrue(result["bee"])
        self.assertEqual((target / "Library/Bee/artifacts/compiled.o").read_text(), "already compiled")
        self.assertEqual((target / "Library/PlayerDataCache/data").read_text(), "already serialized")
        self.assertEqual((target / "Assets/Source/model.fbx.meta").read_text(), "guid: stable-model")
        self.assertFalse((target / "Assets/Source/private.fbx").exists())
        self.assertFalse((target / "braxhorn.keystore.pass").exists())
        self.assertFalse((target / "Assets/PlayableGenerated/old.unity.meta").exists())

    def test_existing_source_guid_wins(self):
        archive = self.root / "cache.enc"
        cache.pack(self.project, archive, self.expected)
        target = self.target()
        meta = target / "Assets/Source/model.fbx.meta"
        meta.parent.mkdir(parents=True)
        meta.write_text("guid: edited-in-source")
        cache.restore(target, archive, self.expected)
        self.assertEqual(meta.read_text(), "guid: edited-in-source")

    def test_code_change_does_not_throw_away_library(self):
        self.write("Assets/Fix.cs", "class Fix { }")
        self.assertEqual(self.expected, cache.profile(self.project, "osiedle", "6000.0.58f2"))

    def test_profiles_isolate_zone_unity_packages_and_variant(self):
        for args in (("miasto", "6000.0.58f2", "arm64-il2cpp"),
                     ("osiedle", "6000.0.59f1", "arm64-il2cpp"),
                     ("osiedle", "6000.0.58f2", "different-renderer")):
            other = cache.profile(self.project, args[0], args[1], variant=args[2])
            self.assertNotEqual(self.expected["key"], other["key"])
        self.write("Packages/manifest.json", '{"dependencies":{"new":"1.0"}}')
        self.assertNotEqual(self.expected["key"], cache.profile(self.project, "osiedle", "6000.0.58f2")["key"])

    def test_profile_mismatch_leaves_project_untouched(self):
        archive = self.root / "cache.enc"
        cache.pack(self.project, archive, self.expected)
        target = self.target()
        with self.assertRaises(ValueError):
            cache.restore(target, archive, {**self.expected, "zone": "oboz"})
        self.assertEqual(list(target.iterdir()), [])

    def test_checksum_failure_is_detected_before_extract(self):
        archive = self.root / "cache.enc"
        cache.pack(self.project, archive, self.expected)
        target = self.target()
        with self.assertRaisesRegex(ValueError, "checksum"):
            cache.restore(target, archive, self.expected, sha256="0" * 64)
        self.assertEqual(list(target.iterdir()), [])

    def test_rejects_traversal_and_private_source_data(self):
        for name in ("../escaped", "/tmp/escaped", "Library/../../escaped",
                     "Library\\..\\escaped", "Assets/private.fbx", "braxhorn.keystore"):
            with self.subTest(name=name), tempfile.TemporaryDirectory() as tmp:
                with self.assertRaises(ValueError):
                    cache.extract(self.malicious(name), Path(tmp), self.expected)

    def test_rejects_symlinks_and_hardlinks(self):
        for kind in (tarfile.SYMTYPE, tarfile.LNKTYPE):
            with tempfile.TemporaryDirectory() as tmp, self.assertRaises(ValueError):
                cache.extract(self.malicious("Library/evil", kind), Path(tmp), self.expected)

    def test_legacy_requires_explicit_migration(self):
        with tempfile.TemporaryDirectory() as tmp:
            with self.assertRaises(ValueError):
                cache.extract(self.malicious("Library/old", include_header=False), Path(tmp), self.expected)
            cache.extract(self.malicious("Library/old", include_header=False), Path(tmp), self.expected, legacy=True)
            self.assertEqual((Path(tmp) / "Library/old").read_text(), "x")

    def test_archive_cannot_follow_source_symlink(self):
        outside = self.root / "outside"
        outside.mkdir()
        (outside / "password").write_text("never archive")
        (self.project / "Library/link").symlink_to(outside, target_is_directory=True)
        self.assertFalse(any("link" in name for _, name in cache.members(self.project)))

    def test_failed_pack_does_not_replace_previous_archive(self):
        archive = self.root / "cache.enc"
        archive.write_bytes(b"previous-cache")
        with patch.dict(os.environ, {"KLUCZ": ""}), self.assertRaises(ValueError):
            cache.pack(self.project, archive, self.expected)
        self.assertEqual(archive.read_bytes(), b"previous-cache")

    def test_restore_preserves_executable_and_modification_time(self):
        tool = self.write("Library/Bee/gradlew", "#!/bin/sh\nexit 0\n")
        tool.chmod(0o755)
        os.utime(tool, (1700000000, 1700000000))
        archive = self.root / "cache.enc"
        cache.pack(self.project, archive, self.expected)
        target = self.target()
        cache.restore(target, archive, self.expected)
        restored = target / "Library/Bee/gradlew"
        self.assertEqual(restored.stat().st_mode & 0o777, 0o755)
        self.assertEqual(restored.stat().st_mtime, 1700000000)

    def test_source_hardlinks_are_stored_as_safe_regular_files(self):
        os.link(self.project / "Library/Bee/artifacts/compiled.o", self.project / "Library/Bee/artifacts/second.o")
        archive = self.root / "cache.enc"
        cache.pack(self.project, archive, self.expected)
        target = self.target()
        cache.restore(target, archive, self.expected)
        self.assertEqual((target / "Library/Bee/artifacts/second.o").read_text(), "already compiled")

    def test_wrong_password_does_not_install_partial_library(self):
        archive = self.root / "cache.enc"
        cache.pack(self.project, archive, self.expected)
        target = self.target()
        with patch.dict(os.environ, {"KLUCZ": "wrong-password"}):
            with self.assertRaises((ValueError, tarfile.TarError)):
                cache.restore(target, archive, self.expected)
        self.assertEqual(list(target.iterdir()), [])


if __name__ == "__main__":
    unittest.main()
