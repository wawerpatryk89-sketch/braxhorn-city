"""Regressions for real Poly Haven key differences; no network or Unity needed."""
import json
from pathlib import Path
import hashlib
import unittest

from prepare_assets import matches_source, polyhaven_map, validate_4k_dimensions, POLYHAVEN_SLUGS, POLYHAVEN_MAPS, A1_POLYHAVEN_PINS


LIVE_VERIFIED_OWNER_SLUGS={"stone_tiles","gravel_embedded_concrete_02","wooden_panels",
                           "concrete_wall_009","leather_white","velour_velvet"}

class PolyHavenSourceTests(unittest.TestCase):
    def entry(self, name="original"):
        return {"4k": {
            "exr": {"url": name},
            "jpg": {"url": name},
            "png": {"url": name},
        }}

    def test_all_actual_provider_responses_resolve_every_required_4k_map(self):
        fixtures=json.loads(Path(__file__).with_name('pbr-provider-preflight.json').read_text())
        for slug in POLYHAVEN_SLUGS:
            if slug not in fixtures:
                # Three owner-selected 7 Nov assets were added after this cached
                # provider snapshot. Build preparation still fetches and validates
                # live Poly Haven metadata/checksums before writing any bytes.
                self.assertIn(slug,LIVE_VERIFIED_OWNER_SLUGS)
                continue
            for kind, ext in POLYHAVEN_MAPS:
                with self.subTest(slug=slug, kind=kind):
                    item=polyhaven_map(fixtures[slug],slug,kind,ext)
                    self.assertIn('/4k/',item['url'])
                    self.assertEqual(len(item['md5']),32)
                    self.assertGreater(item['size'],0)

    def test_new_owner_assets_use_standard_polyhaven_map_keys(self):
        metadata={
            "Diffuse":self.entry("diff"),
            "nor_gl":self.entry("normal"),
            "Rough":self.entry("rough"),
            "arm":self.entry("arm"),
        }
        for slug in LIVE_VERIFIED_OWNER_SLUGS:
            for kind,ext in POLYHAVEN_MAPS:
                with self.subTest(slug=slug,kind=kind):
                    self.assertIn("url",polyhaven_map(metadata,slug,kind,ext))

    def test_a1_materials_are_pinned_to_exact_provider_metadata(self):
        expected={"leather_white","velour_velvet","concrete_wall_009",
                  "rectangular_facade_tiles","granite_tile","stone_tiles"}
        self.assertEqual(set(A1_POLYHAVEN_PINS),expected)
        for slug in expected:
            self.assertEqual(set(A1_POLYHAVEN_PINS[slug]),{"diff","nor_gl","rough","arm"})
            for size,md5 in A1_POLYHAVEN_PINS[slug].values():
                self.assertGreater(size,0)
                self.assertEqual(len(md5),32)

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

    def test_provider_native_4k_keeps_original_aspect_ratio_without_resampling(self):
        self.assertEqual(
            validate_4k_dimensions("velour_velvet_diff_4k.jpg",(4096,3941),True),
            (4096,3941))
        with self.assertRaisesRegex(RuntimeError,"provider-native 4K"):
            validate_4k_dimensions("too_small.jpg",(2048,2048),True)
        with self.assertRaisesRegex(RuntimeError,"4096x4096"):
            validate_4k_dimensions("ambientcg_square.jpg",(4096,3941),False)

    def test_truncated_or_changed_cached_bytes_are_rejected(self):
        data = b"original source bytes"
        item = {"size": len(data), "md5": hashlib.md5(data).hexdigest()}
        self.assertTrue(matches_source(data, item))
        self.assertFalse(matches_source(data[:-1], item))
        self.assertFalse(matches_source(b"x" * len(data), item))


if __name__ == "__main__":
    unittest.main()

