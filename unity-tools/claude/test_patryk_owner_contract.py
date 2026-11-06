#!/usr/bin/env python3
"""Short, read-only static regression gate for owner Patryk on the estate.

No Unity, no APK, no model rewriting. Actual animation skinning still needs Play Mode.
"""
import json
from pathlib import Path
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]
EDITOR = ROOT / "unity-city/Assets/Editor/Claude"
PATRYK = EDITOR / "BraxClaudeOsiedlePatryk.cs"
MOTION = EDITOR / "BraxClaudePatrykHumanoid.cs"
SCENE = EDITOR / "BraxClaudeOsiedleAktualizacja.cs"
CAR = EDITOR / "BraxClaudeAutoPatryka.cs"
SOURCE_IMPORT = ROOT / "unity-tools/claude/prywatne.py"
CATALOG = ROOT / "unity-tools/claude/patryk-owner-22-manifest.json"


class PatrykOwnerContract(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.player = PATRYK.read_text(encoding="utf-8")
        cls.motion = MOTION.read_text(encoding="utf-8")
        cls.scene = SCENE.read_text(encoding="utf-8")
        cls.car = CAR.read_text(encoding="utf-8")
        cls.original = SOURCE_IMPORT.read_text(encoding="utf-8")
        cls.manifest = json.loads(CATALOG.read_text(encoding="utf-8"))

    def test_exact_22_owner_actions_in_code(self):
        archive = self.manifest["animation_archive"]
        self.assertEqual(archive["file_count"], 22)
        entries = self.manifest["clips"]
        self.assertEqual(len(entries), 22)
        expected = {entry["action"]: "patryk_owner/" + entry["file"] for entry in entries}
        self.assertEqual(len(expected), 22)
        self.assertEqual(len({e["sha256"] for e in entries}), 22)
        listed = {}
        for heading in ("KlipyPatryka", "DodatkoweKlipyPatryka"):
            part = self.player.split("static readonly PatrykKlip[] " + heading + " =", 1)[1].split("};", 1)[0]
            for action, source in re.findall(
                r'PK\("([^"]+)"[^\n]*"(patryk_owner/[^"]+\.fbx)"\)', part
            ):
                self.assertNotIn(action, listed, "Duplicate owner action: " + action)
                listed[action] = source
        self.assertEqual(listed, expected)
        for s in listed.values():
            self.assertNotIn("/brax/", s.lower())
            self.assertNotIn("/nyxa/", s.lower())
            self.assertNotIn("/monika/", s.lower())

    def test_no_substitute_animation_and_original_idle(self):
        self.assertIn("PatrykSpokojny(hero, spoczynek, wlascicielWalk)", self.player)
        self.assertIn('PatrykCarIdleZWlasnegoWejscia(hero, spoczynek, wejsciePatryka)', self.player)
        self.assertIn('wejscie.SampleAnimation(hero', self.motion)
        self.assertIn("PatrykWlasnyFBX(hero, kosci, spoczynek, k", self.player)
        self.assertNotIn("PatrykSpacerAwaryjny(", self.player + self.motion)
        self.assertNotIn("PatrykHumanoid(hero", self.player)
        self.assertNotIn("PatrykPozaStala(", self.player + self.motion)

    def test_original_rig_relative_animation_without_changing_model(self):
        self.assertIn("ModelImporterAnimationType.Legacy", self.motion)
        self.assertIn("sourceClip.SampleAnimation(sourceInstance, t)", self.motion)
        self.assertIn("Quaternion.Inverse(sourceRest[name])", self.motion)
        self.assertIn("PatrykSpoczynek(spoczynek)", self.motion)
        self.assertIn('new Vector3(\n                    spec.wMiejscu ? 0f : delta.x', self.motion)
        self.assertIn('if (names.Length < 60)', self.motion)

    def test_original_4k_visual_and_single_player(self):
        self.assertIn('4096', self.original)
        self.assertIn('Patryk_BaseColor.png', self.original)
        self.assertIn('Patryk_Normal.png', self.original)
        self.assertIn('Patryk_ORM.png', self.original)
        self.assertIn('4096', self.player)
        self.assertIn('2048', self.player)
        self.assertIn('patryki.Length != 1', self.scene)
        self.assertIn('samochodyGracza.Length != 1', self.scene)

    def test_no_partial_owner_zip_and_no_old_six_file_fallback(self):
        installer = (ROOT / "unity-tools/claude/import_patryk_owner_22.py").read_text(encoding="utf-8")
        self.assertIn("from import_patryk_owner_22 import install as install_patryk_owner_22", self.original)
        self.assertIn("Patryk anima(6).zip", installer)
        self.assertIn("if len(actual) != 22", installer)
        self.assertIn('if (KlipyPatryka.Length + additionalReady != 22)', self.player)
        self.assertNotIn("additionalMissing++", self.player)
        self.assertIn("if (all.Length != 22", self.motion)
        self.assertIn("actual != Patryk22Hashes[name]", self.motion)
        self.assertNotIn("from import_patryk_animations import install", self.original)

    def test_missing_cabin_or_car_not_silently_accepted(self):
        self.assertIn("brak wolnego stanowiska pod areną", self.car)
        self.assertIn("nie znaleziono modelu", self.car)
        self.assertIn("oryginalny FBX", self.player)


if __name__ == "__main__":
    unittest.main()
