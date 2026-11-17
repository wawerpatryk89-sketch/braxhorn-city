"""Owner 16.11.2026, the casinos' tables: Sic Bo. Three dice under a glass dome on the dealer's side of a walnut table in
KASYNO WAWER; one button: AKCJA at a box (MAŁE 4-10, DOWOLNA TRÓJKA, DUŻE 11-17) bets 10, the shaker throws three fair dice.
MAŁE / DUŻE pay 1:1 and lose on any triple, DOWOLNA TRÓJKA pays 30:1 - the real game's odds."""
import itertools
import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"
RT = ROOT / "unity-city/Assets/Mistrzowie"
GAME = (RT / "BraxChampionsSicBo.cs").read_text(encoding="utf-8")
KIT = (ED / "BraxChampionsKasynoSicBo.cs").read_text(encoding="utf-8")
HALL = (ED / "BraxChampionsCasinoT11Hall.cs").read_text(encoding="utf-8")
MALE, TROJKA, DUZE = 0, 1, 2


def wyplata(pole, a, b, c, st):
    """BraxChampionsSicBo.Wyplata as written (the stake back included)."""
    trojka = a == b == c
    s = a + b + c
    if pole == TROJKA:
        return st * 31 if trojka else 0
    if trojka:
        return 0
    if pole == MALE:
        return st * 2 if 4 <= s <= 10 else 0
    return st * 2 if 11 <= s <= 17 else 0


class SicBo(unittest.TestCase):
    def test_the_payout_as_written(self):
        for line in ("if (pole == Trojka) return trojka ? st * 31 : 0;", "if (trojka) return 0;",
                     "if (pole == Male) return s >= 4 && s <= 10 ? st * 2 : 0;", "return s >= 11 && s <= 17 ? st * 2 : 0;"):
            self.assertIn(line, GAME)

    def test_the_house_edge_over_all_216_throws(self):
        rzuty = list(itertools.product(range(1, 7), repeat=3))
        self.assertEqual(len(rzuty), 216)
        edge = {p: 1 - sum(wyplata(p, *r, 1) for r in rzuty) / 216 for p in (MALE, TROJKA, DUZE)}
        self.assertAlmostEqual(edge[MALE], 6 / 216)                 # 2.78 %, as the published Small / Big
        self.assertAlmostEqual(edge[DUZE], 6 / 216)
        self.assertAlmostEqual(edge[TROJKA], 30 / 216)              # 13.89 %, as the published Any Triple at 30:1

    def test_fair_dice_and_real_faces(self):
        self.assertIn("wynik[i] = 1 + los.Next(6);", GAME)
        self.assertIn("{ Vector3.up, Vector3.forward, Vector3.right, Vector3.left, Vector3.back, Vector3.down }", GAME)
        # opposite faces sum to 7: 1 up / 6 down, 2 forward / 5 back, 3 right / 4 left
        pary = {(1, 6), (2, 5), (3, 4)}
        self.assertTrue(all(a + b == 7 for a, b in pary))
        self.assertIn("Quaternion.FromToRotation(Sciany[v - 1], Vector3.up)", GAME)     # the thrown value shows up
        self.assertIn("(v == 1 || v == 4) ? new Color32(196, 18, 30, 255)", KIT)       # the casino dice: one and four red

    def test_the_table_and_its_place(self):
        self.assertIn("BraxChampionsKasynoSprzet.SicBo(kas,new Vector3(0f,HallFloor+.016f,13.5f),180f);", HALL)
        self.assertIn("BraxInterakcje.Dodaj(this);", GAME)
        self.assertIn('string[] gora = { "MAŁE", "DOWOLNA TRÓJKA", "DUŻE" }', KIT)
        self.assertIn('kurs = { "1 : 1", "30 : 1", "1 : 1" }', KIT)
        self.assertIn("Wklej(px, w, h, src, sw, sh, Arkusz[ArkLogo, 0]", KIT)             # his logo in the felt, his art 1:1
        self.assertIn("gra.stawka = Stawka;", KIT)
        for f in (RT / "BraxChampionsSicBo.cs.meta", ED / "BraxChampionsKasynoSicBo.cs.meta"):
            self.assertTrue(f.exists(), f)


if __name__ == "__main__":
    unittest.main()
