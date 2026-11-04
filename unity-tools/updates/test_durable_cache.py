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


if __name__ == "__main__":
    unittest.main()
