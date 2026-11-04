import json
from pathlib import Path
import tempfile
import unittest
import world_checkpoint as checkpoint


class CheckpointTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.root = Path(self.tmp.name)
        self.data = dict(rejon="osiedle", klucz="source-key", platforma="Android",
            sceny=["Assets/PlayableGenerated/BraxhornEstate.unity"],
            zaleznosci=["Assets/PlayableGenerated/material.mat"],
            ustawieniaNaDysku=True, ustawienia={"przestrzenBarw": 1})
        for p in self.data["sceny"] + self.data["zaleznosci"]:
            self.write(p, "synthetic asset")
            self.write(p + ".meta", "guid: stable")
        self.write("ProjectSettings/ProjectSettings.asset", "settings")
        self.write("ProjectSettings/QualitySettings.asset", "settings")
        self.save()

    def tearDown(self):
        self.tmp.cleanup()

    def write(self, name, value):
        p = self.root / name
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_text(value)

    def save(self):
        self.write("Assets/PlayableGenerated/pamiec-rejonu.json", json.dumps(self.data))

    def test_complete_generation_accepted_without_apk(self):
        self.assertEqual(checkpoint.validate(self.root, "osiedle", "source-key"), (1, 1))

    def test_stale_and_other_zone_checkpoint_rejected(self):
        for zone, key in (("miasto", "source-key"), ("osiedle", "changed-key")):
            with self.assertRaises(ValueError):
                checkpoint.validate(self.root, zone, key)

    def test_missing_dependency_rejected(self):
        (self.root / self.data["zaleznosci"][0]).unlink()
        with self.assertRaises(ValueError):
            checkpoint.validate(self.root, "osiedle", "source-key")

    def test_missing_guid_rejected(self):
        (self.root / (self.data["sceny"][0] + ".meta")).unlink()
        with self.assertRaises(ValueError):
            checkpoint.validate(self.root, "osiedle", "source-key")

    def test_missing_settings_rejected(self):
        self.data["ustawieniaNaDysku"] = False
        self.save()
        with self.assertRaises(ValueError):
            checkpoint.validate(self.root, "osiedle", "source-key")

    def test_path_escape_rejected(self):
        self.data["zaleznosci"] = ["Assets/../../secret"]
        self.save()
        with self.assertRaises(ValueError):
            checkpoint.validate(self.root, "osiedle", "source-key")


if __name__ == "__main__":
    unittest.main()
