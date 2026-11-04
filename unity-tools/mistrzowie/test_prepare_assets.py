"""Regressions for real Poly Haven key differences; no network or Unity needed."""
import hashlib
import unittest

from prepare_assets import matches_source, polyhaven_map


class PolyHavenSourceTests(unittest.TestCase):
    def entry(self, name="original"):
        return {"4k": {"exr": {"url": name}, "jpg": {"url": name}}}

    def test_provider_capitalized_roughness(self):
        self.assertEqual(polyhaven_map({"Rough": self.entry()}, "coast_sand_02", "rough", "exr")["url"], "original")

    def test_legacy_lowercase_names_remain_supported(self):
        for kind, ext in (("rough", "exr"), ("diff", "jpg"), ("nor_gl", "exr")):
            self.assertEqual(polyhaven_map({kind: self.entry()}, "example", kind, ext)["url"], "original")

    def test_fabric_uses_provider_default_not_first_colour(self):
        metadata = {"col_03": self.entry("wrong"), "col_02": self.entry("wrong"), "col_01": self.entry("default")}
        self.assertEqual(polyhaven_map(metadata, "fabric_pattern_05", "diff", "jpg")["url"], "default")

    def test_missing_map_reports_asset_and_never_uses_2k_or_jpg_normal(self):
        for metadata in ({"Rough": {"2k": {"exr": {"url": "low"}}}}, {"Rough": {"4k": {"jpg": {"url": "lossy"}}}}):
            with self.assertRaisesRegex(RuntimeError, "Poly Haven stone: missing rough 4k/exr"):
                polyhaven_map(metadata, "stone", "rough", "exr")
        with self.assertRaisesRegex(RuntimeError, "missing nor_gl"):
            polyhaven_map({"nor_dx": self.entry()}, "stone", "nor_gl", "exr")

    def test_missing_fabric_default_does_not_choose_another_colour(self):
        with self.assertRaisesRegex(RuntimeError, "fabric_pattern_05: missing diff"):
            polyhaven_map({"col_02": self.entry()}, "fabric_pattern_05", "diff", "jpg")

    def test_truncated_or_changed_cached_bytes_are_rejected(self):
        data = b"original source bytes"
        item = {"size": len(data), "md5": hashlib.md5(data).hexdigest()}
        self.assertTrue(matches_source(data, item))
        self.assertFalse(matches_source(data[:-1], item))
        self.assertFalse(matches_source(b"x" * len(data), item))


if __name__ == "__main__":
    unittest.main()
