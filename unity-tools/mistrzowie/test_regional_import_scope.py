"""Champions import diagnostics must never attribute the main City port to A1/T11."""
from pathlib import Path
import json
import unittest

ROOT=Path(__file__).resolve().parents[2]
BUILD=ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsBuild.cs"
PACKAGES=ROOT/"unity-city/Packages/manifest.json"

class ChampionsSourceScope(unittest.TestCase):
    def test_diagnostics_scope_and_audit_file(self):
        source=BUILD.read_text(encoding="utf-8")
        code=source.split("static int RecordSourceImportDiagnostics(string reports)",1)[1].split("static void BuildExplorationApk()",1)[0]
        self.assertIn('p.StartsWith("Assets/Source/Mistrzowie/",StringComparison.Ordinal)',code)
        self.assertIn('p=="Assets/Source/Claude/Metro/metro_train.glb"',code)
        self.assertNotIn('p.StartsWith("Assets/Source/")',code)
        self.assertIn('File.WriteAllText(Path.Combine(reports,"source-imports.json")',code)

    def test_compressed_original_car_decoders_present(self):
        manifest=json.loads(PACKAGES.read_text(encoding="utf-8"))["dependencies"]
        self.assertEqual(manifest.get("com.unity.meshopt.decompress"),"0.2.0-exp.1")
        self.assertEqual(manifest.get("com.unity.cloud.ktx"),"3.6.3")
        self.assertEqual(manifest.get("com.unity.cloud.gltfast"),"6.20.0")
        car=(ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsCarBuilder.cs").read_text(encoding="utf-8")
        self.assertIn('"Assets/Source/Mistrzowie/Vehicles/car_rev.glb"',car)

if __name__=="__main__":
    unittest.main()
