"""Static-only regression for BRAXHORN sign and owner mural mounting.

Does not run Unity, validate visual fidelity on Android, or modify characters.
"""
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[3]
EDITOR = ROOT / "unity-city/Assets/Editor/Oboz"
CAMPUS = (EDITOR / "BraxObozCampus.cs").read_text("utf-8")
ENV = (EDITOR / "BraxObozEnvironment.cs").read_text("utf-8")


class GateAndMuralsTest(unittest.TestCase):
    def test_genuine_three_dimensional_original_sign(self):
        self.assertIn("indexFormat = UnityEngine.Rendering.IndexFormat.UInt32", CAMPUS)
        self.assertIn("metalDepth=0.26m stickerUV=1:1", CAMPUS)
        self.assertIn("var solidMesh = new Mesh", CAMPUS)
        self.assertIn("new Vector2(0,0),new Vector2(0,1)", CAMPUS)
        self.assertIn("BraxObozStableMesh", (EDITOR / "BraxObozStableMesh.cs").read_text("utf-8"))
        self.assertIn("OwnerGateSign(gate);", CAMPUS)
        self.assertIn("44B8F5D6-7F27-4EF3-A804-9BC8C4F462D0.jpeg", CAMPUS)
        self.assertIn("GetPixels32()", CAMPUS)
        self.assertIn("CopySerialized(material, savedMaterial)", CAMPUS)
        self.assertIn("Tylna okleina jest również oryginalną grafiką",CAMPUS)
        self.assertIn("backRenderer.sharedMaterial = material;",CAMPUS)
        self.assertIn("new Vector2(1,0),new Vector2(0,0)",CAMPUS)

    def test_no_support_inside_entrance(self):
        self.assertIn("new[] {-4.45f,4.45f}", CAMPUS)
        self.assertNotIn("new[]{-3.7f,3.7f}", CAMPUS)
        self.assertIn("new Vector3(9.4f,.20f,.38f)", CAMPUS)
        self.assertIn('P(0,0,-125)', CAMPUS)
        self.assertIn("gate.gameObject.AddComponent<BraxObozSlidingGate>()", CAMPUS)

    def test_murals_placed_after_buildings_not_free_standing(self):
        self.assertLess(ENV.index("ServiceBuildings();"), ENV.index("MuralBoards(); // Po budynkach"))
        self.assertIn('var cabin1 = buildings.FirstOrDefault(t => t.name == "Domek 1")', ENV)
        self.assertIn('OriginalMural(cabin1,"WOJOWNIKOW"', ENV)
        self.assertIn('OriginalMural(pavilion,"FIGHT_ZONE"', ENV)
        self.assertIn("new Vector3(-20.05f,2.7f,0),Quaternion.Euler(0,90,0)", ENV)
        self.assertNotIn("new Vector3(-15f,2.7f,-7.5f),Quaternion.identity,8.8f,true)",ENV)
        self.assertNotIn("Bariera końca odcinka drogi",CAMPUS)
        self.assertIn("Stalowy słup od gruntu do szczytu muralu", ENV)
        self.assertIn('OriginalMural(t,"DROGA_MISTRZA"', ENV)
        self.assertIn('OriginalMural(gym,"TRAINING_CAMP"', CAMPUS)
        self.assertNotIn('OriginalMural(root,"WOJOWNIKOW"', ENV)
        self.assertNotIn('OriginalMural(root,"FIGHT_ZONE"', ENV)

    def test_no_character_source_edit_in_stage(self):
        """Scope reminder; compare against baseline SHA before release."""
        self.assertIn("BraxObozCharacters.ReadManifest();", ENV)


if __name__ == "__main__":
    unittest.main()
