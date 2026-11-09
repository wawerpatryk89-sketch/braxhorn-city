#!/usr/bin/env python3
"""Build #34 (9.11) stopped 23 s in: the GUID preflight required the literal
'public static class BraxChampionsCasinoT11' after the casino class became
partial (two files). The same preflight must pass on the current sources."""
from pathlib import Path
import importlib.util
import unittest

ROOT = Path(__file__).resolve().parents[2]


class PreflightMetaGuids20261109(unittest.TestCase):
    def test_champions_preflight_accepts_partial_classes(self):
        spec = importlib.util.spec_from_file_location(
            "check_unity_meta_guids", ROOT / "unity-tools/validation/check_unity_meta_guids.py")
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        _, errors = module.inspect(ROOT / "unity-city/Assets", champions=True)
        self.assertEqual(errors, [])
        casino = (ROOT / "unity-city/Assets/Editor/Mistrzowie/BraxChampionsCasinoT11.cs").read_text(encoding="utf-8")
        self.assertIn("public static partial class BraxChampionsCasinoT11", casino)


if __name__ == "__main__":
    unittest.main()
