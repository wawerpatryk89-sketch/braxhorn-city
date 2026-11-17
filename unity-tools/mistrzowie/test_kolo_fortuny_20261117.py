"""Owner 16.11.2026, the casinos' tables: Big Six "Koło Fortuny". The money wheel of the casinos (54 segments: 1 x24, 2 x15,
5 x7, 10 x4, 20 x2 and two specials - his BRAXHORN logo and his coin, his art 1:1 - at 40:1) with its betting table at the back
of KASYNO WAWER; one button: AKCJA at a box bets 10 on it, the croupier spins."""
import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"
RT = ROOT / "unity-city/Assets/Mistrzowie"
GAME = (RT / "BraxChampionsKoloFortuny.cs").read_text(encoding="utf-8")
KIT = (ED / "BraxChampionsKasynoKolo.cs").read_text(encoding="utf-8")
HALL = (ED / "BraxChampionsCasinoT11Hall.cs").read_text(encoding="utf-8")


def layout():
    """BraxChampionsKoloFortuny.Uklad() as written."""
    u = [0] * 54
    u[0], u[27], u[13], u[40] = 50, 51, 20, 20
    for i in (6, 20, 34, 47): u[i] = 10
    for i in (3, 10, 17, 24, 31, 38, 51): u[i] = 5
    acc, twos = 0.0, 0
    for i in range(54):
        if u[i]: continue
        acc += 15 / 39
        if acc >= 1 - 1e-4 and twos < 15: u[i] = 2; twos += 1; acc -= 1
        else: u[i] = 1
    return u


class Wheel(unittest.TestCase):
    def test_the_classic_distribution(self):
        for line in ("u[0] = Logo; u[27] = Moneta; u[13] = 20; u[40] = 20;", "foreach (int i in new[] { 6, 20, 34, 47 }) u[i] = 10;",
                     "foreach (int i in new[] { 3, 10, 17, 24, 31, 38, 51 }) u[i] = 5;", "acc += 15f / 39f;"):
            self.assertIn(line, GAME)
        u = layout()
        self.assertEqual({v: u.count(v) for v in set(u)}, {1: 24, 2: 15, 5: 7, 10: 4, 20: 2, 50: 1, 51: 1})

    def test_payouts_and_house_edge(self):
        self.assertIn("return symbol >= Logo ? b * 41 : b * (symbol + 1);", GAME)
        u = layout()
        edge = {}
        for v, pays in ((1, 1), (2, 2), (5, 5), (10, 10), (20, 20), (50, 40)):
            p = u.count(v) / 54
            edge[v] = 1 - p * (pays + 1)
        self.assertAlmostEqual(edge[1], .1111, places=3)            # as the published Big Six edges
        self.assertAlmostEqual(edge[2], .1667, places=3)
        self.assertAlmostEqual(edge[20], .2222, places=3)
        self.assertAlmostEqual(edge[50], .2407, places=3)

    def test_every_segment_equally_likely_and_the_stop(self):
        self.assertIn("wynik = los.Next(Segmenty);", GAME)
        self.assertIn("float cel = -wynik * 360f / Segmenty", GAME)               # segment i centred at i x 6.67 deg from the top
        self.assertIn("float th = Mathf.Atan2(-dx, dy);", KIT)                   # the face painted the same way
        self.assertIn("BraxInterakcje.Dodaj(this);", GAME)

    def test_his_art_and_place(self):
        self.assertIn("int ark = v == BraxChampionsKoloFortuny.Logo ? ArkLogo : ArkMonetaA;", KIT)
        self.assertIn("BraxChampionsKasynoSprzet.KoloFortuny(kas,new Vector3(0f,HallFloor+.016f,23.5f),180f);", HALL)
        for f in (RT / "BraxChampionsKoloFortuny.cs.meta", ED / "BraxChampionsKasynoKolo.cs.meta"):
            self.assertTrue(f.exists(), f)


if __name__ == "__main__":
    unittest.main()
