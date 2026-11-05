#!/usr/bin/env python3
"""Read-only update-compatibility gate against signed Champions APK #14.

The already delivered APK is preserved. Only Android identity/signing/version
regressions are fatal; game visuals, walk clearance and physics stay advisory.
Previous identity was independently read from GitHub Actions run 42028827233,
artifact mistrzowie-validation-42028827233-1/apk-delivery.json.
"""
import json
from pathlib import Path
from apk_delivery import APP_ID

ROOT = Path(__file__).resolve().parents[2]
BEFORE = {
    "runId": "42028827233",
    "commit": "6eca26ef55cf1ff5f11ca98e4bcd3181122c0dbb",
    "applicationId": "com.braxhorn.mistrzowie",
    "versionCode": 316045507,
    "certificateSha256": "f3829d496e1bf4890ec64af78170c7e8b8696d1e49d4122df2b4fc1c80bd5ffe",
    "apkSha256": "2ba08f7d6e4b6ad4e2bc2ed426bfb8570e578188d83093f0c476b5a617f7eee3"
}


def check():
    receipt = json.loads((ROOT / "mistrzowie-evidence/signing-preflight.json").read_text())
    version = int((ROOT / "unity-city/braxhorn.versioncode").read_text().strip())
    if APP_ID != BEFORE["applicationId"]:
        raise ValueError("This would create a SEPARATE Android app, not an update")
    actual = receipt.get("certificateSha256")
    smoke = receipt.get("smokeApkCertificateSha256")
    if actual != BEFORE["certificateSha256"] or smoke != BEFORE["certificateSha256"]:
        raise ValueError("Existing key differs from the installed Champions #14 APK; do not build")
    if version <= BEFORE["versionCode"]:
        raise ValueError("APK versionCode must exceed last signed Champions #14")
    out = dict(status="IN_PLACE_ANDROID_APK_IDENTITY_PREFLIGHT_PASS",
               previous=BEFORE, newVersionCode=version,
               applicationId=APP_ID, certificateSha256=actual,
               newApkSha256=None, built=False,
               visualsReviewed=False, phoneUpdateTested=False,
               originalApkPreserved=True)
    dest = ROOT / "mistrzowie-evidence/previous-compatibility.json"
    dest.parent.mkdir(exist_ok=True)
    dest.write_text(json.dumps(out, indent=2) + "\n")
    print("MISTRZOWIE_PREVIOUS_APK_COMPATIBLE "
          + APP_ID + " previousVersion=" + str(BEFORE["versionCode"])
          + " newVersion=" + str(version) + " cert=UNCHANGED APK_NOT_BUILT")
    return out


if __name__ == "__main__":
    check()
