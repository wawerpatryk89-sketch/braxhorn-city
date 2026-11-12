import hashlib
import json
import os
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import durable_cache as durable


class FakeGitHub:
    def __init__(self, expected):
        self.body = dict(schema=1, profile=expected, current=None, previous=None)
        self.assets = {}
        self.draft = True
        self.fail_upload = False

    def __call__(self, *args):
        command = args[1]
        if command == "view":
            return json.dumps(dict(isDraft=self.draft, body=json.dumps(self.body),
                assets=[dict(name=k, size=len(v)) for k, v in self.assets.items()]))
        if command == "upload":
            if self.fail_upload:
                raise OSError("synthetic upload interruption")
            p = Path(args[3])
            self.assets[p.name] = p.read_bytes()
        elif command == "download":
            name = args[args.index("--pattern") + 1]
            folder = Path(args[args.index("--dir") + 1])
            (folder / name).write_bytes(self.assets[name])
        elif command == "edit":
            self.body = json.loads(Path(args[args.index("--notes-file") + 1]).read_text())
        elif command == "delete-asset":
            del self.assets[args[3]]
        else:
            raise AssertionError(args)
        return ""


class DurableCacheTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.root = Path(self.tmp.name)
        self.expected = {"key": "library-android-v2-osiedle-example"}
        self.fake = FakeGitHub(self.expected)
        self.api = patch.object(durable, "gh", self.fake)
        self.api.start()
        self.env = patch.dict(os.environ, {"GITHUB_RUN_ID": "100", "GITHUB_RUN_ATTEMPT": "1", "GITHUB_SHA": "a" * 40})
        self.env.start()

    def tearDown(self):
        self.env.stop()
        self.api.stop()
        self.tmp.cleanup()

    def archive(self, data):
        path = self.root / "archive.enc"
        path.write_bytes(data)
        path.with_name(path.name + ".json").write_text(json.dumps(dict(
            profile=self.expected, bytes=len(data), sha256=hashlib.sha256(data).hexdigest())))
        return path

    def test_split_round_trip_and_keep_two_snapshots(self):
        with patch.object(durable, "PART_BYTES", 8):
            for value in (b"11111111111111", b"22222222222222", b"33333333333333"):
                durable.upload("owner/repo", "cache-tag", self.archive(value), self.expected)
            target = self.root / "restored.enc"
            durable.download("owner/repo", "cache-tag", target, self.expected)
        self.assertEqual(target.read_bytes(), b"33333333333333")
        self.assertEqual(len(self.fake.assets), 4)
        self.assertEqual(len(self.fake.body["previous"]["parts"]), 2)

    def test_interrupted_upload_keeps_usable_index(self):
        durable.upload("owner/repo", "cache-tag", self.archive(b"first"), self.expected)
        previous = json.dumps(self.fake.body)
        self.fake.fail_upload = True
        with self.assertRaises(OSError):
            durable.upload("owner/repo", "cache-tag", self.archive(b"second"), self.expected)
        self.assertEqual(json.dumps(self.fake.body), previous)

    def test_corrupt_current_download_falls_back_to_previous(self):
        durable.upload("owner/repo", "cache-tag", self.archive(b"first"), self.expected)
        durable.upload("owner/repo", "cache-tag", self.archive(b"second"), self.expected)
        name = self.fake.body["current"]["parts"][0]["name"]
        self.fake.assets[name] = b"BROKEN"
        target = self.root / "restored.enc"
        durable.download("owner/repo", "cache-tag", target, self.expected)
        self.assertEqual(target.read_bytes(), b"first")

    def test_public_release_is_never_modified(self):
        self.fake.draft = False
        with self.assertRaises(ValueError):
            durable.upload("owner/repo", "cache-tag", self.archive(b"secret"), self.expected)
        self.assertEqual(self.fake.assets, {})

    def test_profile_and_branch_isolation(self):
        self.assertNotEqual(durable.release_tag(self.expected, "branch-a"), durable.release_tag(self.expected, "branch-b"))
        self.fake.body["profile"] = {"key": "wrong-zone"}
        with self.assertRaises(ValueError):
            durable.download("owner/repo", "cache-tag", self.root / "restored.enc", self.expected)

    # 12.11: trwała kopia Library poprzedniego profilu pakietów (ten sam rejon / Unity / wariant, zmieniony tylko manifest).
    def test_earlier_profile_snapshot_requires_explicit_key_and_same_variant(self):
        old = dict(schema=2, zone="miasto", unity="6000.0.58f2", platform="android", variant="arm64-il2cpp",
                   inputs={"Packages/manifest.json": "a" * 64}, key="library-android-v2-miasto-" + "1" * 24)
        new = {**old, "inputs": {"Packages/manifest.json": "b" * 64}, "key": "library-android-v2-miasto-" + "2" * 24}
        self.expected = old
        self.fake.body = dict(schema=1, profile=old, current=None, previous=None)
        durable.upload("owner/repo", "old-tag", self.archive(b"library of the earlier profile"), old)
        target = self.root / "restored.enc"
        with self.assertRaises(ValueError):                       # bez wskazania — profil niezgodny
            durable.download("owner/repo", "old-tag", target, new)
        with self.assertRaises(ValueError):                       # wskazany inny klucz
            durable.download("owner/repo", "old-tag", target, new, "library-android-v2-miasto-" + "3" * 24)
        with self.assertRaises(ValueError):                       # inny wariant kompilacji — nigdy
            durable.download("owner/repo", "old-tag", target, {**new, "variant": "x86"}, old["key"])
        self.assertFalse(target.exists())
        durable.download("owner/repo", "old-tag", target, new, old["key"])
        self.assertEqual(target.read_bytes(), b"library of the earlier profile")

    def test_earlier_profile_uses_its_own_release_tag_on_the_same_branch(self):
        new = {"key": "library-android-v2-miasto-30deee51ef41bf0b90818095"}
        ref = "refs/heads/claude/miasto-opus-20261031"
        self.assertEqual(durable.release_tag(new, ref, "library-android-v2-miasto-2a1cb92dc336cbc2aebbffc0"),
                         "unity-ci-cache-miasto-2a1cb92dc336cbc2aebbffc0-c0d662288568")
        self.assertEqual(durable.release_tag(new, ref), "unity-ci-cache-miasto-30deee51ef41bf0b90818095-c0d662288568")


if __name__ == "__main__":
    unittest.main()
