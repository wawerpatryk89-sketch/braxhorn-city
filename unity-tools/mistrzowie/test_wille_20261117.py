"""Owner 16.11.2026 23:13: "tylko wille kozaki zrobimy na sprzedaż 3" (in Miasto Mistrzów only three villas for sale,
"naprawdę bogate, np. na wzgórzu"), sold by Monika in WAWER NIERUCHOMOŚCI. One modern hillside villa (the Los Angeles hill
houses: white volumes, grey stone, a cantilevered upper floor, an infinity pool) in three finishes, every room furnished and
working (global rule 11.11 "od razu na gotowo"; osiedle audit 22:37: houses for sale must not be empty shells)."""
import pathlib
import re
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"
RT = ROOT / "unity-city/Assets/Mistrzowie"
SRC = (ED / "BraxChampionsWille.cs").read_text(encoding="utf-8")
SZAFA = (RT / "BraxChampionsSzafa.cs").read_text(encoding="utf-8")


class Villas(unittest.TestCase):
    def test_three_finishes_one_plan(self):
        self.assertIn('public static readonly string[] Nazwy = { "WILLA 1", "WILLA 2", "WILLA 3" };', SRC)
        # 17.11 21:55: the island's Miami house white to the ground (BraxChampionsWillaWyspa) - the old plan builds villa 3 only
        self.assertIn("static Material ScianaParter(int nr) => render;", SRC)
        self.assertIn("static Material ScianaPietro(int nr) => nr == 3 ? graphite : render;", SRC)
        self.assertIn("g.localPosition = srodek * K; g.localRotation = Quaternion.Euler(0f, yaw, 0f); g.localScale = Vector3.one * K;", SRC)

    def test_infinity_pool_and_jacuzzi_you_can_swim_in(self):
        self.assertIn("PoolX0 = -13f, PoolX1 = 3f, PoolZ0 = -21.6f, PoolZ1 = -16.6f, PoolFloor = -1.6f, Water = -.06f", SRC)
        self.assertIn("PoolFloor - .2f, Water - .015f, PoolZ0 - .3f, PoolZ0);", SRC)       # the infinity wall 15 mm under the water
        self.assertEqual(SRC.count("AddComponent<BraxChampionsPoolSwimVolume>()"), 2)
        self.assertIn("BraxChampionsAquapark.Water(", SRC)
        # the jacuzzi's wall is a ring of colliders: you can climb in
        self.assertIn("for (int i = 0; i < 12; i++)", SRC)

    def test_garage_door_on_akcja(self):
        self.assertIn('drzwi.zawias = hinge; drzwi.os = Vector3.right; drzwi.katOtwarcia = 90f; drzwi.napis = "BRAMA GARAŻU";', SRC)
        self.assertIn("public Vector3 os = Vector3.up;", SZAFA)
        self.assertIn("zawias.localRotation = spoczynek * Quaternion.AngleAxis(kat, os);", SZAFA)
        self.assertIn("if (!string.IsNullOrEmpty(komunikat))", SZAFA)

    def test_every_room_furnished(self):
        for piece in ("BraxKatalog.Sofa(", "BraxKatalog.DiningSet(", "BraxKatalog.Kitchen(", "BraxKatalog.Bed(", "BraxKatalog.WcPan(",
                      "BraxKatalog.Wardrobe(", "BraxKatalog.Tv(", "BraxChampionsMirror.Wall(", "Wanna(", "Prysznic("):
            self.assertIn(piece, SRC)
        self.assertEqual(SRC.count("BraxKatalog.Bed(c,"), 3)
        self.assertGreaterEqual(SRC.count("BraxKatalog.WcPan(c,"), 4)
        self.assertGreaterEqual(len(re.findall(r'Model\("(ArenaLobby|HotelSobczak)/', SRC)), 20)
        # owner 12.11: no big kitchen walls - a low run without wall units
        self.assertIn('tag + " kuchnia", false);', SRC)
        self.assertNotIn('"lodowka"', SRC)
        self.assertNotIn('"slupek"', SRC)

    def test_doors_are_real_doors(self):
        self.assertGreaterEqual(SRC.count("Drzwi(\""), 10)
        self.assertIn('BraxChampionsInteriorDoor.Single("drzwi wejściowe"', SRC)

    def test_for_sale_by_monika(self):
        self.assertIn('W.Akcja("willa_" + nr + "_sprzedaz", Nazwy[nr - 1] + " · NA SPRZEDAŻ",', SRC)
        self.assertIn("WAWER NIERUCHOMOŚCI na parterze T12", SRC)
        # the letter kit has no middle dot: the 3D board text keeps to plain letters
        for text in re.findall(r'BraxChampionsLetters3D\.Text\(b, [^,]+, [^,]+, ("[^"]*")', SRC):
            self.assertNotIn("·", text)

    def test_preview_and_meta(self):
        self.assertTrue((ED / "BraxChampionsWille.cs.meta").exists())
        proj = (ROOT / "unity-tools/mistrzowie/wille_preview/wille_preview.csproj").read_text(encoding="utf-8")
        self.assertIn("BraxChampionsWille.cs", proj)
        self.assertIn("BraxChampionsSzafa.cs", proj)


if __name__ == "__main__":
    unittest.main()
