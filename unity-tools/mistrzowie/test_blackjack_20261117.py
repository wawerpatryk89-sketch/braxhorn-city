"""Owner 16.11.2026: "blackjack grywalny w obu kasynach". Two playable tables in KASYNO WAWER (T11) and two in the SKY KASYNO
(T09): the real rules (six decks, the cut card at three quarters, the dealer stands on every 17, blackjack pays 3 to 2, a
push returns the bet), his cards (the card atlas with his J / Q / K and back), his chips, his BLACKJACK sign; the felt's
print is in the felt. One button: AKCJA bets / hits, five seconds still = stand."""
import pathlib
import re
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"
RT = ROOT / "unity-city/Assets/Mistrzowie"
GAME = (RT / "BraxChampionsBlackjack.cs").read_text(encoding="utf-8")
KIT = (ED / "BraxChampionsKasynoBlackjack.cs").read_text(encoding="utf-8")
SPRZET = (ED / "BraxChampionsKasynoSprzet.cs").read_text(encoding="utf-8")
HALL = (ED / "BraxChampionsCasinoT11Hall.cs").read_text(encoding="utf-8")
TOWER = (ED / "BraxChampionsWiezowce.cs").read_text(encoding="utf-8")


def value(c):
    r = c % 13
    return 11 if r == 12 else 10 if r >= 8 else r + 2


def total(hand):
    s = sum(value(c) for c in hand); aces = sum(1 for c in hand if c % 13 == 12)
    while s > 21 and aces:
        s -= 10; aces -= 1
    return s


class Rules(unittest.TestCase):
    def test_card_values_as_the_game(self):
        self.assertIn("public static int Wartosc(int c) { int r = c % 13; return r == 12 ? 11 : r >= 8 ? 10 : r + 2; }", GAME)
        # the atlas order (BraxChampionsAutomatyGry.PokerGra): rank 0 = 2 .. 8 = 10, 9 J, 10 Q, 11 K, 12 A
        self.assertEqual([value(c) for c in range(13)], [2, 3, 4, 5, 6, 7, 8, 9, 10, 10, 10, 10, 11])
        self.assertEqual(total([12, 12, 7]), 21)                           # A A 9 (soft)
        self.assertEqual(total([12, 11, 4]), 17)                           # A K 6

    def test_house_rules(self):
        self.assertIn("s.but = new int[312]; for (int i = 0; i < 312; i++) s.but[i] = i % 52;", GAME)   # six decks
        self.assertIn("if (s.wBucie > 234) Tasuj(s);", GAME)                                           # cut card at 3/4
        self.assertIn("public static bool KrupierDobiera(List<int> krupier) => Suma(krupier, out _) < 17;", GAME)
        self.assertIn("if (gbj) return b * 5 / 2;", GAME)                                              # 3 to 2
        self.assertIn("if (gbj && kbj) return b;", GAME)
        self.assertIn("return g == k ? b : 0;", GAME)

    def test_one_button(self):
        self.assertIn('return "DOBIERZ · " + Suma(s.gracz, out _);', GAME)
        self.assertIn("if (Time.time >= s.czas) { s.faza = Krupier;", GAME)                            # still = stand
        self.assertIn('" · AKCJA = DOBIERZ · STÓJ ZA "', GAME)
        self.assertIn("BraxInterakcje.Dodaj(this);", GAME)
        # a seat at the table would take the AKCJA button (BraxCityWorld: seats first): the chairs are no seats
        self.assertNotIn("W.Siedzisko", KIT)

    def test_his_cards_chips_sign_and_felt(self):
        self.assertIn("BraxChampionsAutomatyGry.PokerGra.KartaPx(c, out int px, out int py);", GAME)
        self.assertIn("BraxChampionsAutomatyGry.PokerGra.RewersPx(out int rx, out int ry);", GAME)
        self.assertIn('kartyMat.GetTexture("_MainTex")', KIT)
        self.assertIn("Zeton(b, r % 5,", KIT)
        self.assertIn("Arkusz[ArkLogo, 0]", KIT)
        self.assertIn('"BLACKJACK PŁACI 3 DO 2"', KIT)
        self.assertIn("public const float KartaSz = .063f, KartaDl = .088f;", GAME)                    # 63 x 88 mm

    def test_in_both_casinos(self):
        self.assertIn("BraxChampionsKasynoSprzet.Blackjack(kas,new Vector3(bx,HallFloor+.016f,-8.5f),180f);", HALL)
        self.assertIn("BraxChampionsKasynoSprzet.SzyldWiszacy(kas,new Vector3(bx,4.6f,-8.9f),BraxChampionsKasynoSprzet.ArkBlackjack,1.5f,HallCeiling);", HALL)
        self.assertIn('if(blackjacks!=2)BraxBuildQuality.Warn(', HALL)
        self.assertIn("foreach(float x in new[]{-6.6f,6.6f})BraxChampionsKasynoSprzet.Blackjack(kas,new Vector3(x,y,-9.6f),180f);", TOWER)
        self.assertIn("var bj = k.root.gameObject.AddComponent<BraxChampionsBlackjack>();", SPRZET)
        self.assertTrue((RT / "BraxChampionsBlackjack.cs.meta").exists())
        for proj in ("casino_preview/casino_preview.csproj", "wiezowce_preview/wiezowce_preview.csproj"):
            text = (ROOT / "unity-tools/mistrzowie" / proj).read_text(encoding="utf-8")
            self.assertIn("BraxChampionsKasynoBlackjack.cs", text)
            self.assertIn("BraxChampionsBlackjack.cs", text)


if __name__ == "__main__":
    unittest.main()
