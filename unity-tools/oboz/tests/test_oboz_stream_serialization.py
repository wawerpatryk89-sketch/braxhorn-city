"""Prevent camp #37 level1 scene serialization crash due to missing scripts on 14 streams.

Source regression only: requires Unity 6 scene build before declaring the APK healthy.
"""
from pathlib import Path
import re
import unittest

ROOT=Path(__file__).resolve().parents[3]
RUNTIME=ROOT/"unity-city/Assets/Oboz"
EDITOR=ROOT/"unity-city/Assets/Editor/Oboz"

class CampRiverMonoScriptSerialization(unittest.TestCase):
    def test_unique_component_file_and_unmodified_fixtures(self):
        ground=(RUNTIME/"BraxObozStructureGround.cs").read_text(encoding="utf8")
        fluid=(RUNTIME/"BraxObozFluidSurface.cs").read_text(encoding="utf8")
        self.assertIn("public sealed class BraxObozStructureGround : MonoBehaviour",ground)
        self.assertNotIn("class BraxObozFluidSurface",ground)
        self.assertIn("public sealed class BraxObozFluidSurface : MonoBehaviour",fluid)
        for field in ("public Renderer surface;", "public MeshCollider bed;",
                      "public float minimumDepthMetres=.05f;",
                      "public float maximumDepthMetres=1.25f;"):
            self.assertIn(field,fluid)
        meta=(RUNTIME/"BraxObozFluidSurface.cs.meta").read_text(encoding="utf8")
        self.assertRegex(meta,r"(?m)^guid: [a-f0-9]{32}$")

    def test_no_second_monobehaviour_in_single_runtime_file(self):
        for file in sorted(RUNTIME.glob("*.cs")):
            source=file.read_text(encoding="utf8")
            classes=re.findall(r"\\bclass\\s+([A-Za-z_]\\w*)\\s*:\\s*MonoBehaviour\\b",source)
            with self.subTest(filename=file.name):
                self.assertLessEqual(len(classes),1,(file,classes))
                if len(classes)==1:
                    self.assertEqual(classes[0],file.stem)

    def test_editor_validation_prevents_shipping_missing_river_scripts(self):
        code=(EDITOR/"BraxObozBuild.cs").read_text(encoding="utf8")
        self.assertIn("OBOZ_SCENE_SERIALIZATION_RIVER_OK=14",code)
        self.assertIn("GameObjectUtility.GetMonoBehavioursWithMissingScriptCount",code)
        self.assertIn("MonoScript",code)
        self.assertIn("streamNodes.Length!=14",code)
        self.assertIn("typeof(BraxObozFluidSurface)",code)
        # Existing scene and materials remain original. No feature disabled.
        scene=(EDITOR/"BraxObozEnvironment.cs").read_text(encoding="utf8")
        self.assertIn('Group("Strumień — płynąca tafla"',scene)
        self.assertIn("stream.AddComponent<BraxObozFluidSurface>();",scene)

if __name__=="__main__":
    unittest.main()
