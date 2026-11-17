"""Owner 16.11.2026, the casinos' tables: "bakarat". Punto banco as the casinos deal it (eight decks, the third card by the
house tableau, GRACZ 1:1, BANKIER 1:1 less 5 %, REMIS 8:1), playable with one button (the player has no decision in
baccarat): AKCJA at a betting box bets 20 and the croupier deals. Two tables in KASYNO WAWER under his BAKARAT sign."""
import itertools
import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"
RT = ROOT / "unity-city/Assets/Mistrzowie"
GAME = (RT / "BraxChampionsBakarat.cs").read_text(encoding="utf-8")
KIT = (ED / "BraxChampionsKasynoBakarat.cs").read_text(encoding="utf-8")
HALL = (ED / "BraxChampionsCasinoT11Hall.cs").read_text(encoding="utf-8")
SPRZET = (ED / "BraxChampionsKasynoSprzet.cs").read_text(encoding="utf-8")


def banker_draws(b, p3):
    """The same tableau as BraxChampionsBakarat.BankierDobiera."""
    if p3 < 0: return b <= 5
    if b <= 2: return True
    if b == 3: return p3 != 8
    if b == 4: return 2 <= p3 <= 7
    if b == 5: return 4 <= p3 <= 7
    if b == 6: return p3 in (6, 7)
    return False


def odds():
    """Exact outcome probabilities with an infinite shoe (card points 0 x4/13, 1..9 x1/13)."""
    pts = {0: 4 / 13, **{v: 1 / 13 for v in range(1, 10)}}
    res = {"P": 0.0, "B": 0.0, "T": 0.0}
    for p1, p2, b1, b2 in itertools.product(pts, repeat=4):
        w = pts[p1] * pts[p2] * pts[b1] * pts[b2]
        p, b = (p1 + p2) % 10, (b1 + b2) % 10
        outcomes = []
        if p >= 8 or b >= 8:
            outcomes.append((w, p, b))
        else:
            if p <= 5:
                for p3 in pts:
                    pp = (p + p3) % 10; w3 = w * pts[p3]
                    if banker_draws(b, p3):
                        for b3 in pts: outcomes.append((w3 * pts[b3], pp, (b + b3) % 10))
                    else: outcomes.append((w3, pp, b))
            elif banker_draws(b, -1):
                for b3 in pts: outcomes.append((w * pts[b3], p, (b + b3) % 10))
            else:
                outcomes.append((w, p, b))
        for ww, pp, bb in outcomes:
            res["P" if pp > bb else "B" if bb > pp else "T"] += ww
    return res


class Rules(unittest.TestCase):
    def test_the_tableau_is_the_casinos(self):
        for line in ("if (p3 < 0) return b <= 5;", "if (b <= 2) return true;", "if (b == 3) return p3 != 8;",
                     "if (b == 4) return p3 >= 2 && p3 <= 7;", "if (b == 5) return p3 >= 4 && p3 <= 7;",
                     "if (b == 6) return p3 == 6 || p3 == 7;", "public static bool GraczDobiera(int gracz) => gracz <= 5;"):
            self.assertIn(line, GAME)
        self.assertIn("public static int Punkty(int c) { int r = c % 13; return r == 12 ? 1 : r >= 8 ? 0 : r + 2; }", GAME)
        self.assertIn("if (g2 < 8 && b2 < 8)", GAME)                                        # naturals stand

    def test_house_edge_as_published(self):
        o = odds()
        self.assertAlmostEqual(o["B"], .4586, places=3)          # banker wins 45.86 %, player 44.62 %, tie 9.52 %
        self.assertAlmostEqual(o["P"], .4462, places=3)
        self.assertAlmostEqual(o["T"], .0952, places=3)
        banker_edge = -(o["B"] * .95 - o["P"])                      # 1.06 % with the 5 % commission
        player_edge = -(o["P"] - o["B"])                            # 1.24 %
        self.assertAlmostEqual(banker_edge, .0106, places=3)
        self.assertAlmostEqual(player_edge, .0124, places=3)

    def test_payouts(self):
        self.assertIn("if (wygral == Remis) return pole == Remis ? b * 9 : b;", GAME)
        self.assertIn("return pole == Gracz ? b * 2 : b + b * 95 / 100;", GAME)
        self.assertIn("bk.stawka = 2 * Stawka;", SPRZET)                                   # 20: the commission is whole (19)
        self.assertIn("s.but = new int[416]; for (int i = 0; i < 416; i++) s.but[i] = i % 52;", GAME)   # eight decks

    def test_one_button_and_his_things(self):
        self.assertIn('return BraxChampionsKasyno.Kredyt >= stawka ? "POSTAW " + stawka + " · " + Pola[pole] : "BRAK KREDYTU";', GAME)
        self.assertIn("siatki = BraxChampionsKarty.Siatki();", GAME)
        self.assertIn('string[] napis = { "BANKIER", "REMIS", "GRACZ" }, kurs = { "0,95 : 1", "8 : 1", "1 : 1" };', KIT)
        self.assertIn("Arkusz[ArkLogo, 0]", KIT)
        self.assertIn("BraxChampionsKasynoSprzet.Bakarat(kas,new Vector3(bx,HallFloor+.016f,6.0f),180f);", HALL)
        self.assertIn("BraxChampionsKasynoSprzet.ArkBakarat", HALL)
        for f in (RT / "BraxChampionsBakarat.cs.meta", RT / "BraxChampionsKarty.cs.meta", ED / "BraxChampionsKasynoBakarat.cs.meta"):
            self.assertTrue(f.exists(), f)


if __name__ == "__main__":
    unittest.main()
