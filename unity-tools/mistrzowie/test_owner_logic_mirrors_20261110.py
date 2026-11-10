"""Owner 10.11 12:33-12:38 (APK 37): the ring entrance belongs to the fighters' side, every mirror is a real mirror."""
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"


def src(name):
    return (ED / name).read_text(encoding="utf-8")


class WalkoutLogic(unittest.TestCase):
    def setUp(self):
        self.arena = src("BraxChampionsArena.cs")

    def test_gate_and_steps_face_the_fighters_door(self):
        self.assertIn("public const int GateSide=5;", self.arena)             # V5-V6 faces -z, the fighters' door Z0
        self.assertIn("if (i == GateSide)", self.arena)
        self.assertIn("float zs = cz - ap;", self.arena)
        self.assertNotIn("float zs = cz + ap;", self.arena)

    def test_public_carpet_never_reaches_the_cage(self):
        self.assertIn("W.MM(carpet, -87.3f, -84.7f, yF, yF + .012f, Z0 + .05f, gateFoot - .03f);", self.arena)
        self.assertIn("W.MM(carpet, -87.3f, -84.7f, yF, yF + .012f, standFront, Z1 - .05f);", self.arena)
        self.assertIn("W.MM(carpet, cx - 12.65f, cx + 12.65f, yF, yF + .012f, crossZ0, crossZ1);", self.arena)
        self.assertNotIn("zs + 1.37f, Z1 - .05f", self.arena)


class RealMirrors(unittest.TestCase):
    def test_no_flat_mirror_plates_left(self):
        for name in ("BraxChampionsArenaAthletes.cs", "BraxChampionsHotelSobczakT03.cs", "BraxChampionsApartments.cs"):
            text = src(name)
            self.assertNotIn("Box(M.mirror", text, name)
            self.assertNotIn('LuxSolid("mirror"', text, name)

    def test_live_planar_mirror_everywhere(self):
        helper = src("BraxChampionsMirror.cs")
        self.assertIn("go.AddComponent<BraxGymPlanarMirror>()", helper)
        self.assertIn('Shader.Find("Braxhorn/GymPlanarMirror")', helper)
        self.assertIn("go.AddComponent<BraxChampionsMirrorCamera>()", helper)
        hotel = src("BraxChampionsHotelSobczakT03.cs")
        self.assertEqual(hotel.count("BraxChampionsMirror.Wall("), 6)        # lifts, lobby bar, toilets, restaurant bar, rooms, garage lift (10.11)
        self.assertIn("BraxChampionsMirror.Wall(", src("BraxChampionsArenaAthletes.cs"))
        runtime = (ROOT / "unity-city/Assets/Mistrzowie/BraxChampionsMirrorCamera.cs").read_text(encoding="utf-8")
        self.assertIn("mirror.sourceCamera = cam;", runtime)
        self.assertTrue((ED / "BraxChampionsMirror.cs.meta").exists())


if __name__ == "__main__":
    unittest.main()
