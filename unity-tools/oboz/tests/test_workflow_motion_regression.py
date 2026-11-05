"""Regression for camp #19: never omit source clips during the APK workflow.

Pure source check. No APK, Unity editor, decrypted assets, external network or keys.
"""
import json
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[3]
WORKFLOW = ROOT / ".github/workflows/oboz-unity-build.yml"
MANIFEST = ROOT / "unity-city/Assets/Editor/Oboz/CharacterManifest.json"


class CampAnimationWorkflowRegression(unittest.TestCase):
    def test_motion_restore_is_unconditional_and_before_unity(self):
        text = WORKFLOW.read_text(encoding="utf-8").split("  oboz-content-only:", 1)[0]
        name = "      - name: Przywroc animacje casual i Moniki (wymagane dla wszystkich 7 postaci)"
        self.assertEqual(text.count(name), 1)
        start = text.index(name)
        end = text.find("\n      - name:", start + len(name))
        self.assertNotEqual(end, -1)
        step = text[start:end]
        self.assertIn("run: python3 unity-tools/oboz/motion_candidates.py", step)
        self.assertNotIn("\n        if:", step, "motion restores must never be skipped on the rebuild branch")
        self.assertLess(start, text.index("      - name: Profil pamięci obozu"))
        self.assertLess(start, text.index("Generowanie sceny i kompilacja tylko obozu"))

    def test_manifest_clips_have_cheap_non_skippable_preflight(self):
        text = WORKFLOW.read_text(encoding="utf-8").split("  oboz-content-only:", 1)[0]
        name = "      - name: Sprawdz wszystkie klipy postaci PRZED uruchomieniem Unity"
        self.assertEqual(text.count(name), 1)
        begin = text.index(name)
        finish = text.find("\n      - name:", begin + len(name))
        self.assertNotEqual(finish, -1)
        step = text[begin:finish]
        self.assertIn("BRAXHORN_OBOZ_MISSING_ANIMATION", step)
        self.assertIn("raise SystemExit(1)", step)
        self.assertNotIn("\n        if:", step)
        self.assertLess(begin, text.index("      - name: Profil pamięci obozu"))
        manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
        self.assertEqual(len(manifest["characters"]), 7)
        self.assertTrue(all(any(c["path"].endswith("/casual/Idle.anim") for c in person["clips"]) for person in manifest["characters"]))


if __name__ == "__main__":
    unittest.main()
