#!/usr/bin/env python3
"""Fast source-only gate for Patryk in the estate. Does not launch Unity."""
import json
from pathlib import Path
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]
EDITOR = ROOT / "unity-city/Assets/Editor/Claude"
PATRYK = EDITOR / "BraxClaudeOsiedlePatryk.cs"
MOTION = EDITOR / "BraxClaudePatrykHumanoid.cs"
CATALOG = ROOT / "unity-tools/claude/patryk-owner-22-manifest.json"


class PatrykOwnerContract(unittest.TestCase):
    def test_player_uses_only_own_fbx(self):
        source = PATRYK.read_text(encoding="utf-8")
        block = source.split("static readonly PatrykKlip[] KlipyPatryka =", 1)[1].split("};", 1)[0]
        sources = re.findall(r'PK\("([^"]+)"[^\n]*"([^"]+\.fbx)"\)', block)
        self.assertEqual(dict(sources), {
            "Walk": "patryk_owner/Strut Walking.fbx",
            "Run": "patryk_owner/Fast Run.fbx",
            "Jump": "patryk_owner/Jump.fbx",
            "EnteringCar": "patryk_owner/Entering Car.fbx"
        })
        self.assertNotIn("brax/", block.lower())
        self.assertNotIn("nyxa/", block.lower())
        self.assertNotIn("monika/", block.lower())
        self.assertNotIn("PatrykHumanoid(hero", source)
        self.assertNotIn("PatrykSpacerAwaryjny(hero", source)

    def test_idle_is_original_model_car_idle_original_clip(self):
        source = PATRYK.read_text(encoding="utf-8")
        motion = MOTION.read_text(encoding="utf-8")
        self.assertIn("PatrykSpokojny(hero, spoczynek, wlascicielWalk)", source)
        self.assertIn('klipy.FirstOrDefault(c => c.name == "Walk")', source)
        self.assertIn('wlascicielWalk.SampleAnimation(hero, time)', motion)
        self.assertIn('if (bestArms > 45f || bestLegs > 48f)', motion)
        self.assertNotIn("PatrykSpokojny(hero, spoczynek)", source)
        self.assertIn("PatrykCarIdleZWlasnegoWejscia(hero, spoczynek, wejsciePatryka)", source)
        self.assertIn('wejscie.SampleAnimation(hero', motion)
        self.assertNotIn("PatrykPozaStala(", source + motion)
        self.assertNotIn('PatrykHumanoidOwner', motion)
        self.assertIn('ModelImporterAnimationType.Legacy', motion)

    def test_all_22_original_motion_ids_recorded(self):
        manifest = json.loads(CATALOG.read_text(encoding="utf-8"))
        self.assertEqual(manifest["animation_archive"]["file_count"], 22)
        entries = manifest["clips"]
        self.assertEqual(len(entries), 22)
        self.assertEqual(len(set(x["file"] for x in entries)), 22)
        self.assertEqual(len(set(x["action"] for x in entries)), 22)
        self.assertTrue(all(len(x["sha256"]) == 64 for x in entries))
        self.assertEqual(manifest["verification_status"].endswith("NOT yet verified."), True)


if __name__ == "__main__":
    unittest.main()
