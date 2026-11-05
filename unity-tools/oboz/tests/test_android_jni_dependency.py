"""Guard Android JNI API dependency required by camp content picker.

The Android player compiles the UNITY_ANDROID block separately from the editor.
Parsing C# source alone cannot catch a missing UnityEngine.AndroidJNIModule.
No Unity process or private assets are used here.
"""
import json
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[3]
PACKAGES = ROOT / "unity-city/Packages/manifest.json"
CONTENT = ROOT / "unity-city/Assets/Updates/BraxContent.cs"


class AndroidJniDependency(unittest.TestCase):
    def test_android_java_picker_has_required_builtin_module(self):
        source = CONTENT.read_text(encoding="utf-8")
        self.assertIn("#if UNITY_ANDROID && !UNITY_EDITOR", source)
        self.assertIn("new AndroidJavaClass(", source)
        deps = json.loads(PACKAGES.read_text(encoding="utf-8"))["dependencies"]
        self.assertEqual(deps.get("com.unity.modules.androidjni"), "1.0.0")


if __name__ == "__main__":
    unittest.main()
