"""Quality-only errors cannot block scene build, but do not conceal them."""
from pathlib import Path
import unittest
R=Path(__file__).resolve().parents[3]
E=(R/"unity-city/Assets/Editor/Oboz/BraxObozEnvironment.cs").read_text("utf8")
S=(R/"unity-city/Assets/Editor/Oboz/BraxObozServices.cs").read_text("utf8")
class OptionalBuilderContract(unittest.TestCase):
    def test_partial_section_cleaned_and_errors_reported(self):
        self.assertIn("static void Optional(string label,Action action)",E)
        self.assertIn("Object.DestroyImmediate(child.gameObject)",E)
        self.assertIn("BraxBuildQuality.Warn(",E)
        for item in ('Domek 1','Domek 2','Domek 3','Domek 4',
            'Ring 1','las i murawa','oryginalne murale'):
            self.assertIn('Optional("'+item+'"',E)
        self.assertIn("Terrain(); // grunt jest kluczowy",E)
        self.assertIn("if(!EditorSceneManager.SaveScene",E)
    def test_per_building_cleanup_without_ignoring_failure(self):
        self.assertIn("static void OptionalBuilding(string label,Transform parent,Action action)",S)
        self.assertIn("OptionalBuilding(item.Item2,parent",S)
        self.assertIn("OptionalBuilding(\"Murowane WC/ŁAŹNIA/UMYWALNIA\"",S)
        self.assertIn("OBÓZ NIEZBUDOWANE",S)
        self.assertIn("ex.ToString()",S)
