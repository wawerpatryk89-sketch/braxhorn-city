"""Fail-closed preflight of the real owner original bull and 5 GitHub ZIP parts.

No remote calls in tests, no Unity, no characters or APK modifications.
"""
import json
from pathlib import Path
import sys
import unittest
from unittest import mock
from types import SimpleNamespace

TOOLS = Path(__file__).resolve().parents[1]
sys.path.insert(0,str(TOOLS))
import owner_release_assets as loader

SOURCE = Path(__file__).resolve().parents[3] / "unity-city/Assets/Editor/Oboz/BraxObozCampus.cs"


class BullDeliveryTests(unittest.TestCase):
    def make_release(self, include_original=True, scaled=False, include_parts=True):
        assets = []
        if include_parts:
            assets = [{"name":name, "size":size}
                      for name,(size,_) in loader.SPLIT_PARTS.items()]
        if include_original:
            assets.append({"name":loader.ORIGINAL_BULL,
                           "size":loader.EXPECTED[loader.ORIGINAL_BULL][0]})
        if scaled:
            assets.append({"name":loader.BULL,
                           "size":loader.EXPECTED[loader.BULL][0]})
        return json.dumps({"draft":True,"tag_name":loader.TAG,"id":loader.RELEASE_ID,"assets":assets})

    def mock_gh(self, payload):
        return mock.patch.object(loader.subprocess,"run",
                                 return_value=SimpleNamespace(returncode=0, stdout=payload, stderr=""))

    def test_original_bull_five_parts_preflight_passes(self):
        self.assertEqual(loader.EXPECTED[loader.ORIGINAL_BULL],
                         (2232404,"05667f880b5beaf720dec95ac480ce32c0e91e1e2ab318a28d34f36e475f4cf0"))
        with self.mock_gh(self.make_release()):
            self.assertEqual(loader.remote_preflight("wawerpatryk89-sketch/braxhorn-city"),
                             ("five_parts",loader.ORIGINAL_BULL))

    def test_scaled_bull_legacy_compatibility(self):
        with self.mock_gh(self.make_release(include_original=False,scaled=True)):
            self.assertEqual(loader.remote_preflight("wawerpatryk89-sketch/braxhorn-city"),
                             ("five_parts",loader.BULL))

    def test_prefer_owner_original_when_both_present(self):
        with self.mock_gh(self.make_release(scaled=True)):
            self.assertEqual(loader.remote_preflight("wawerpatryk89-sketch/braxhorn-city"),
                             ("five_parts",loader.ORIGINAL_BULL))

    def test_missing_bull_fails_before_unity(self):
        with self.mock_gh(self.make_release(include_original=False)):
            with self.assertRaises(ValueError):
                loader.remote_preflight("wawerpatryk89-sketch/braxhorn-city")

    def test_public_release_is_never_accepted(self):
        data=json.loads(self.make_release())
        data["draft"]=False
        with self.mock_gh(json.dumps(data)):
            with self.assertRaisesRegex(ValueError,"DRAFT"):
                loader.remote_preflight("wawerpatryk89-sketch/braxhorn-city")

    def test_mismatched_draft_release_identity_rejected(self):
        data=json.loads(self.make_release())
        data["id"]=99999999
        with self.mock_gh(json.dumps(data)):
            with self.assertRaisesRegex(ValueError,"identity/tag mismatch"):
                loader.remote_preflight("wawerpatryk89-sketch/braxhorn-city")

    def test_rest_release_endpoint_used_instead_of_graphql(self):
        with mock.patch.object(loader.subprocess,"run",
                return_value=SimpleNamespace(returncode=0,stdout=self.make_release(),stderr="")) as runner:
            loader.remote_preflight("wawerpatryk89-sketch/braxhorn-city")
        self.assertEqual(runner.call_args.args[0],
            ["gh","api",f"repos/wawerpatryk89-sketch/braxhorn-city/releases/{loader.RELEASE_ID}"])

    def test_original_geometry_is_used_without_edit(self):
        source=SOURCE.read_text(encoding="utf-8")
        self.assertIn('A_charging_bull_statu_0725030443_refine.glb',source)
        self.assertIn('BRAXHORN_BULL_RONDO_4X_ORYGINALNE_TEKSTURY.glb',source)
        self.assertIn('model.transform.localScale *= (4.8f*K)/longest;',source)
        self.assertIn('PrefabUtility.InstantiatePrefab(source)',source)


if __name__=="__main__":
    unittest.main()
