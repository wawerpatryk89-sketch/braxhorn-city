"""Miasto R1: prevent false APK failure due to root-owned build/Android.

This is a cheap static check, not an Android build or a Unity play test.
"""
from pathlib import Path
import unittest

ROOT=Path(__file__).resolve().parents[2]
WORKFLOW=(ROOT/".github/workflows/claude-region-build.yml").read_text(encoding="utf-8")
VALIDATOR=(ROOT/"unity-tools/claude/check_city_sea_payload.py").read_text(encoding="utf-8")


class R1AndroidReceiptSafety(unittest.TestCase):
    def test_watcher_writes_validation_to_runner_temp(self):
        self.assertIn(
            'check_city_sea_payload.py "$f" > "$RUNNER_TEMP/wyslane/$tag-validation.json"',
            WORKFLOW,
        )
        self.assertNotIn('> "build/Android/$tag-validation.json"', WORKFLOW)

    def test_final_delivery_still_validates_real_apk_data(self):
        self.assertIn('[ -f "$RUNNER_TEMP/wyslane/$tag" ] &&', WORKFLOW)
        self.assertIn('report="$RUNNER_TEMP/wyslane/$tag-validation.json"', WORKFLOW)
        self.assertIn('if ! python3 unity-tools/claude/check_city_sea_payload.py "$f" > "$report"; then', WORKFLOW)
        self.assertIn('cat "$report" || true', WORKFLOW)
        self.assertIn('exit 1', WORKFLOW)

    def test_invalid_payload_is_still_rejected(self):
        self.assertIn("len(data) != 68", VALIDATOR)
        self.assertIn("Expected one city sea component", VALIDATOR)
        self.assertIn("sys.exit(1)", VALIDATOR)

    def test_lz4_player_archive_is_inspected_not_skipped(self):
        self.assertIn("data.unity3d", VALIDATOR)
        self.assertIn("UnityDataTool", VALIDATOR)
        self.assertIn("'archive', 'extract'", VALIDATOR)
        self.assertIn("--filter", VALIDATOR)
        self.assertIn("UNITY_DATA_TOOL=", WORKFLOW)
        self.assertIn("v2.4.0/UnityDataTool-linux-x64.zip", WORKFLOW)
        self.assertIn("0534036123353a2b69a93e7da9658f319e4056d9cb6ec45926d5449ab65bf22f", WORKFLOW)

    def test_receipts_are_kept_as_job_artifacts(self):
        self.assertIn('${{ runner.temp }}/wyslane/*-validation.json', WORKFLOW)


if __name__=="__main__":
    unittest.main()
