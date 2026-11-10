"""E1, owner 10.11 21:57: "całkowity brak wykończenia ... masz wykończyć wszystko tak, jak się wykańcza mieszkanie normalnie",
"co to za ściany, co to za elewacje". Every room is finished: wall finishes, skirting, door frames, ceilings with fittings,
services (exit signs, extinguishers, hose reels, switches, sockets), toilet and bar fittings, plants; the facade is clad
and the front planted like the board."""
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"


class E1Finish(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.e1 = (ED / "BraxChampionsE1.cs").read_text(encoding="utf-8")
        cls.fin = (ED / "BraxChampionsE1Finish.cs").read_text(encoding="utf-8")

    def test_wired(self):
        self.assertTrue((ED / "BraxChampionsE1Finish.cs.meta").exists())
        self.assertIn("public static partial class BraxChampionsE1", self.e1)
        self.assertIn('Finish();Flush("E1 interior | finish', self.e1)
        self.assertIn("FinishMaterials();", self.e1)
        self.assertIn("doorList.Add((x,y,z,yaw,width,height,glassLeaf,label));", self.e1)
        for step in ("Linings();", "Skirting();", "DoorFrames();", "Ceilings();", "Services();", "ToiletFittings();",
                     "BarFittings();", "Plants();", "Rooms();", "Outside();"):
            self.assertIn(step, self.fin)

    def test_real_finishes_no_rough_render_no_gold(self):
        self.assertIn('blackMatte=Inside(Tex("interior_paint_graphite","beige_wall_001"', self.e1)   # smooth paint, not stucco
        self.assertNotIn('"interior_dark_plaster","white_stucco_02"', self.e1)
        self.assertIn('cladding=Tex("facade_fibre_cement_graphite","concrete_wall_009"', self.fin)
        self.assertGreaterEqual(self.e1.count("B(cladding,"), 3)                                    # front, sides, back
        self.assertNotRegex(re.sub(r"//.*", "", self.e1 + self.fin), r"\bgold\b|BraxChampionsGold")
        for mat in ("acousticFelt", "wcFloor", "terrazzo", "wallWhite", "ceilWhite"):
            self.assertIn(mat + "=", self.fin)

    def test_every_room_and_every_door(self):
        self.assertIn("foreach(var d in doorList)", self.fin.split("static void DoorFrames()")[1])
        self.assertIn("ExitSign(", self.fin.split("static void Services()")[1])
        for f in ("Downlight(", "Diffuser(", "Sprinkler(", "Smoke(", "Panel("):
            self.assertIn("static void " + f, self.fin)
        self.assertGreaterEqual(self.fin.count("Extinguisher(new Vector3"), 8)
        self.assertGreaterEqual(self.fin.count("HoseCabinet(new Vector3"), 4)
        self.assertIn("potted_plant_01", self.fin)
        self.assertIn("BraxMiastoAssety.Model(\"miasto_palmy\",\"palma\")", self.fin)
        self.assertIn("BraxChampionsPalmTrunk.OnSpot(o,at);", self.fin)                             # palms stand in beds


if __name__ == "__main__":
    unittest.main()
