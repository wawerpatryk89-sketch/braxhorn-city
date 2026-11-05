"""Offline fixture validation of the real camp-only Addressables baseline receipt."""
import hashlib
import json
from pathlib import Path
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import content_candidate as review


def sha(data: bytes):
    return hashlib.sha256(data).hexdigest()


class CampContentCandidateTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.project = Path(self.tmp.name) / "unity-city"
        self.base = self.project / "BraxContentBuild/base"
        self.content = self.project / "Assets/StreamingAssets/BraxContent"
        self.base.mkdir(parents=True)
        self.content.mkdir(parents=True)
        self.scene = self.project / review.SCENE
        self.scene.parent.mkdir(parents=True, exist_ok=True)
        self.scene.write_text("%YAML 1.1", encoding="utf-8")
        (self.scene.parent / (self.scene.name + ".meta")).write_text(
            "fileFormatVersion: 2\nguid: " + "d"*32 + "\n")
        entries = []
        for name, data in [("catalog_xyz.json", b'{"m_LocatorId": "oboz"}'),
                           ("oboz_scene_abc.bundle", b"content bundle 1234")]:
            (self.content / name).write_bytes(data)
            entries.append(dict(name=name, sha256=sha(data), bytes=len(data)))
        self.manifest = dict(format=1, zone="oboz", baseId="oboz-"+"a"*32,
                             runtimeId="b"*64, version="base",
                             catalog="catalog_xyz.json", firstScene="BraxhornObozReview",
                             scenes=["BraxhornObozReview"], files=entries)
        (self.base / "manifest.json").write_text(json.dumps(self.manifest))
        (self.base / "addressables_content_state.bin").write_bytes(b"content-state-"*20)
        (self.base / "scene-guids.txt").write_text(review.SCENE + "="+"d"*32 + "\n")
        resources = self.project / "Assets/Resources"
        resources.mkdir(parents=True)
        (resources / "BraxContentBase.json").write_text(json.dumps(self.manifest))

    def test_accepts_one_scene_with_matching_guids_bundles_and_catalog(self):
        report = review.verify(self.project)
        self.assertEqual(report["zone"], "oboz")
        self.assertEqual(report["files"], 2)
        self.assertFalse(report["apkBuilt"])
        self.assertFalse(report["baseApkIntegrated"])
        self.assertEqual(report["status"], "ADDRESSABLES_BASE_CANDIDATE_ONLY")

    def test_rejects_foreign_region_or_changed_scene(self):
        self.manifest["zone"] = "osiedle"
        (self.base / "manifest.json").write_text(json.dumps(self.manifest))
        with self.assertRaisesRegex(ValueError, "oboz"):
            review.verify(self.project)

    def test_rejects_corrupted_assetbundle(self):
        (self.content / "oboz_scene_abc.bundle").write_bytes(b"broken")
        with self.assertRaisesRegex(ValueError, "corrupt"):
            review.verify(self.project)

    def test_rejects_missing_state_file(self):
        (self.base / "addressables_content_state.bin").unlink()
        with self.assertRaisesRegex(ValueError, "content_state"):
            review.verify(self.project)

    def test_rejects_changed_original_scene_guid(self):
        (self.base / "scene-guids.txt").write_text(review.SCENE + "="+"q"*32)
        with self.assertRaisesRegex(ValueError, "GUID"):
            review.verify(self.project)

    def test_rejects_other_regions_in_scene_list(self):
        self.manifest["scenes"].append("BraxhornEstate")
        (self.base / "manifest.json").write_text(json.dumps(self.manifest))
        with self.assertRaisesRegex(ValueError, "scene"):
            review.verify(self.project)

    def test_rejects_manifest_resource_mismatch(self):
        self.manifest["runtimeId"] = "c"*64
        (self.base / "manifest.json").write_text(json.dumps(self.manifest))
        with self.assertRaisesRegex(ValueError, "mismatch"):
            review.verify(self.project)


if __name__ == "__main__":
    unittest.main()
