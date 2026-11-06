"""Fast build-readiness regression: current signed APK base, no old #18 pin.

Source-only test. This is NOT APK verification and does not dispatch a build.
"""
from pathlib import Path
import unittest

ROOT=Path(__file__).resolve().parents[3]
WF=(ROOT/".github/workflows/oboz-unity-build.yml").read_text("utf-8")
ASSET=(ROOT/"unity-tools/oboz/owner_geometry_preflight.py").read_text("utf-8")
SERVICES=(ROOT/"unity-city/Assets/Editor/Oboz/BraxObozServices.cs").read_text("utf-8")
VALIDATION=(ROOT/"unity-city/Assets/Editor/Oboz/BraxObozValidation.cs").read_text("utf-8")


class BeforeFiveHourBuildTests(unittest.TestCase):
    def test_accepts_current_compatible_signed_base_not_stale_18(self):
        block=WF.split("Zweryfikuj poprzednie APK obozu",1)[1].split("      - name:",1)[0]
        self.assertIn('applicationId',block)
        self.assertIn('signingCertificateSha256',block)
        self.assertIn('versionCode',block)
        self.assertIn('sourceCommit',block)
        self.assertIn('sourceRun',block)
        self.assertIn('next_version<=code',block)
        self.assertIn('code<216024998',block) # #18 minimum, not exact pin.
        self.assertNotIn('info.get("versionCode")==216024998',block)
        self.assertNotIn('info.get("sourceRun")==41983757537',block)
        self.assertNotIn('info.get("sourceCommit")=="e2ee759',block)

    def test_real_original_cabin_cooking_parts_preflight(self):
        groups=("CookingTable_Bolts1.001_","CookingTable_Bolts2.001_",
                "CookingTable_Legs.001_","CookingTable_Shelf.001_",
                "PortableStove_Burners.001_","PortableStove_Grate_L_LP.001_",
                "PortableStove_Grate_R_LP.001_","PortableStove_Tumbler_LP.001_",
                "PortableStove_Wire.001_","Faucet_LP.001_",
                "Faucet_Tap_LP.001_","Sink_Drain_LP.001_","Sink_Legs_LP.001_")
        for item in groups:
            self.assertIn(item,ASSET)
            self.assertIn(item,SERVICES)
        self.assertIn('var originalSink=Assembly(new[]{',SERVICES)
        self.assertIn('CloneOriginalParts(prefab,table.transform,new[]{"CookingTable.001_"',SERVICES)
        self.assertIn('CloneOriginalParts(source,carrier.transform,prefixes)',SERVICES)
        # Required fittings are nested under their source roots. Copying a
        # parent and its child independently duplicated legs, bolts and taps.
        self.assertIn('other!=t && t.IsChildOf(other)',SERVICES)

    def test_cabin_sinks_not_falsely_counted_as_bathhouse_sinks(self):
        self.assertIn('var sanitaryControls=sanitary.Length==1',VALIDATION)
        self.assertIn('sanitary[0].GetComponentsInChildren<BraxObozFacility>(true)',VALIDATION)
        self.assertNotIn('var sanitaryControls=scene.GetComponentsInChildren<BraxObozFacility>(true)',VALIDATION)

    def test_signing_artifact_still_hard_and_optional_quality_soft(self):
        self.assertIn('release_identity.py verify',WF)
        self.assertIn('id: apk_backup',WF)
        self.assertIn('if-no-files-found: error',WF)
        self.assertIn('continue-on-error: true',WF)
        self.assertIn('default: false',WF)
        self.assertIn('BraxObozBuild.BuildAndroidReview',WF)
        self.assertIn('owner_geometry_preflight.py',WF)


if __name__=="__main__":
    unittest.main()
