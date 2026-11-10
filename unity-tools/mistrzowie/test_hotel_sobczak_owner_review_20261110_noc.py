"""Hotel Sobczak, owner 10.11 22:43-22:45 (photos from the game): "sufity beton" - smooth white ceiling paint with a
neutral ground ambient; "coś stoi, blokuje dojście ... wszystko ma być funkcjonalne" - no bench across the hall in front
of the wardrobe, and the wardrobe opens (hollow carcass, rail, two doors on BraxCityDoor). Source checks only."""
import re
import unittest
from pathlib import Path

ED = Path(__file__).resolve().parents[2] / "unity-city/Assets/Editor/Mistrzowie"


def code(name):
    return re.sub(r"//.*", "", (ED / name).read_text(encoding="utf-8"))


class HotelOwnerReviewNight(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.mat = code("BraxChampionsHotelSobczakMaterials.cs")
        cls.t03 = code("BraxChampionsHotelSobczakT03.cs")

    def test_ceiling_smooth_white_paint(self):
        self.assertIn('s.ceiling=Surface("ceiling_paint_white_smooth"', self.mat)
        self.assertNotIn('s.ceiling=P("ceiling_stucco","white_stucco_02"', self.mat)
        self.assertIn('s.ceiling.SetColor("_GroundColor",new Color(.96f,.96f,.97f));', self.mat)

    def test_no_bench_blocking_the_wardrobe(self):
        self.assertNotIn("Box(M.cognac,bx1+.28f,b-.28f,.36f,.46f,Z(1.98f),Z(2.47f),true);", self.t03)
        self.assertNotIn("Box(M.walnutDark,bx1+.3f,b-.3f,0,.36f,Z(2.0f),Z(2.45f),true);", self.t03)

    def test_wardrobe_opens(self):
        self.assertIn('BraxChampionsInteriorDoor.Single("T03_WARDROBE_"+number+"_L"', self.t03)
        self.assertIn('BraxChampionsInteriorDoor.Single("T03_WARDROBE_"+number+"_R"', self.t03)
        self.assertIn("cd.sides=new[]{-1f}", self.t03)
        self.assertNotIn("Box(M.walnut,bx1+.08f,b-.04f,0,2.6f,Z(.02f),Z(.64f),true);", self.t03)   # no solid block


if __name__ == "__main__":
    unittest.main()
