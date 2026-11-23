"""Owner 16.11.2026, the casinos' tables: poker Casino Hold'em. Two tables in KASYNO WAWER under his POKER sign: ANTE 10,
the flop, one decision (AKCJA = CALL of twice the ante, 5 s without it = PAS); the dealer plays from a pair of fours; the ante
pays 100/20/10/3/2/1 : 1 (royal flush .. a straight or less), the call 1:1 - the rules of the real game."""
import pathlib
import random
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"
RT = ROOT / "unity-city/Assets/Mistrzowie"
GAME = (RT / "BraxChampionsHoldem.cs").read_text(encoding="utf-8")
KIT = (ED / "BraxChampionsKasynoHoldem.cs").read_text(encoding="utf-8")
HALL = (ED / "BraxChampionsCasinoT11Hall.cs").read_text(encoding="utf-8")
SPRZET = (ED / "BraxChampionsKasynoSprzet.cs").read_text(encoding="utf-8")


def pakuj(uklad, *czesci):
    r = []
    for c in czesci:
        r.extend(c if isinstance(c, list) else [c])
    v = uklad
    for i in range(5):
        v = v << 4 | (r[i] + 1 if i < len(r) else 0)
    return v


def strit(m):
    for hi in range(12, 3, -1):
        if (m >> (hi - 4)) & 31 == 31:
            return hi
    return 3 if m & 0b1000000001111 == 0b1000000001111 else -1


def gorne(m, n):
    return [i for i in range(12, -1, -1) if m >> i & 1][:n]


def ocena(karty):
    """BraxChampionsHoldem.Ocena as written."""
    ile, wk, mk, maska = [0] * 13, [0] * 4, [0] * 4, 0
    for c in karty:
        r, k = c % 13, c // 13
        ile[r] += 1; wk[k] += 1; mk[k] |= 1 << r; maska |= 1 << r
    kolor = max([k for k in range(4) if wk[k] >= 5], default=-1)
    if kolor >= 0:
        sf = strit(mk[kolor])
        if sf >= 0:
            return pakuj(9 if sf == 12 else 8, sf)
    kareta = next((r for r in range(12, -1, -1) if ile[r] == 4), -1)
    trojki = [r for r in range(12, -1, -1) if ile[r] == 3]
    pary = [r for r in range(12, -1, -1) if ile[r] == 2]
    if kareta >= 0:
        return pakuj(7, kareta, gorne(maska & ~(1 << kareta), 1))
    if trojki:
        p = max(trojki[1] if len(trojki) > 1 else -1, pary[0] if pary else -1)
        if p >= 0:
            return pakuj(6, trojki[0], p)
    if kolor >= 0:
        return pakuj(5, gorne(mk[kolor], 5))
    st = strit(maska)
    if st >= 0:
        return pakuj(4, st)
    if trojki:
        return pakuj(3, trojki[0], gorne(maska & ~(1 << trojki[0]), 2))
    if len(pary) > 1:
        return pakuj(2, pary[0], pary[1], gorne(maska & ~(1 << pary[0]) & ~(1 << pary[1]), 1))
    if pary:
        return pakuj(1, pary[0], gorne(maska & ~(1 << pary[0]), 3))
    return pakuj(0, gorne(maska, 5))


def kwalifikuje(o):
    return o >= pakuj(1, 2)


def mnoznik(u):
    return {9: 100, 8: 20, 7: 10, 6: 3, 5: 2}.get(u, 1)


def wyplata(ante, g, k):
    call, a = 2 * ante, ante * (1 + mnoznik(g >> 20))
    if not kwalifikuje(k):
        return a + call
    if g > k:
        return a + 2 * call
    if g == k:
        return ante + call
    return 0


def C(kolor, ranga):
    return kolor * 13 + ranga


class Holdem(unittest.TestCase):
    def test_the_evaluator_as_written(self):
        for line in ("if (kolor >= 0) { int sf = NajwyzszyStrit(maskaKoloru[kolor]); if (sf >= 0) return Pakuj(sf == 12 ? PokerKrolewski : Poker, sf); }",
                     "if (p >= 0) return Pakuj(Full, trojki[0], p);",
                     "if (pary.Count > 1) return Pakuj(DwiePary, pary[0], pary[1], Gorne(maska & ~(1 << pary[0]) & ~(1 << pary[1]), 1));",
                     "return (m & 0b1_0000_0000_1111) == 0b1_0000_0000_1111 ? 3 : -1;"):
            self.assertIn(line, GAME)

    def test_hands(self):
        koło = ocena([C(0, 12), C(1, 0), C(2, 1), C(3, 2), C(0, 3), C(1, 9), C(2, 10)])
        self.assertEqual(koło >> 20, 4)                                          # A-2-3-4-5: a straight, five high
        self.assertLess(koło, ocena([C(0, 0), C(1, 1), C(2, 2), C(3, 3), C(0, 4)]))   # 2-3-4-5-6 beats it
        self.assertEqual(ocena([C(0, 5), C(1, 5), C(2, 5), C(0, 9), C(1, 9), C(2, 9), C(3, 0)]) >> 20, 6)   # two trips: a full house
        trzy_pary = ocena([C(0, 5), C(1, 5), C(0, 9), C(1, 9), C(0, 11), C(1, 11), C(2, 2)])
        self.assertEqual(hex(trzy_pary), "0x2ca600")                              # KK JJ and the third pair's 7 the kicker
        self.assertEqual(ocena([C(2, 8), C(2, 9), C(2, 10), C(2, 11), C(2, 12), C(0, 0), C(1, 1)]) >> 20, 9)
        self.assertGreater(ocena([C(3, i) for i in (0, 2, 4, 6, 8)]), ocena([C(i % 4, i + 3) for i in range(5)]))   # flush > straight

    def test_five_card_frequencies_on_a_sample(self):
        # the exact counts (all 2 598 960 hands, checked with the C# itself) are 1302540 / 1098240 / 123552 / 54912 / 10200 /
        # 5108 / 3744 / 624 / 36 / 4; here the mirror on 60 000 random hands, for speed
        talia = [C(k, r) for k in range(4) for r in range(13)]
        rnd = random.Random(5)
        licz = [0] * 10
        for _ in range(60000):
            licz[ocena(rnd.sample(talia, 5)) >> 20] += 1
        self.assertAlmostEqual(licz[0] / 60000, 1302540 / 2598960, delta=.01)
        self.assertAlmostEqual(licz[1] / 60000, 1098240 / 2598960, delta=.01)
        self.assertAlmostEqual(licz[2] / 60000, 123552 / 2598960, delta=.004)

    def test_the_dealer_and_the_payouts(self):
        self.assertIn("public static bool Kwalifikuje(int krupier) => krupier >= Pakuj(Para, 2);", GAME)
        self.assertIn("uklad == PokerKrolewski ? 100 : uklad == Poker ? 20 : uklad == Kareta ? 10 : uklad == Full ? 3 : uklad == Kolor ? 2 : 1", GAME)
        for line in ("if (!Kwalifikuje(krupier)) return a + call;", "if (gracz > krupier) return a + 2 * call;",
                     "if (gracz == krupier) return ante + call;"):
            self.assertIn(line, GAME)
        para4 = ocena([C(0, 2), C(1, 2), C(2, 10), C(3, 8), C(0, 6), C(1, 4), C(2, 0)])
        para3 = ocena([C(0, 1), C(1, 1), C(2, 10), C(3, 8), C(0, 6), C(1, 4), C(2, 0)])
        self.assertTrue(kwalifikuje(para4)); self.assertFalse(kwalifikuje(para3))
        poker_krol = ocena([C(2, 8), C(2, 9), C(2, 10), C(2, 11), C(2, 12), C(0, 0), C(1, 1)])
        self.assertEqual(wyplata(10, poker_krol, para3), 10 * 101 + 20)          # not qualified: the ante by the table, the call back
        self.assertEqual(wyplata(10, poker_krol, para4), 10 * 101 + 40)          # qualified and beaten: the call 1:1 too
        self.assertEqual(wyplata(10, para4, para4), 30)                          # a tie: both back
        self.assertEqual(wyplata(10, para3, para4), 0)

    def test_one_deck_every_hand_and_the_decision(self):
        self.assertIn("Sprzatnij(s); Tasuj(s);", GAME)
        self.assertIn("public float filcY = .80f, decyzja = 5f;", GAME)
        self.assertIn("case Decyzja: if (!s.siedzi && Time.time >= s.czas) Pas(t, s); break;", GAME)   # no clock at the chair (23.11)
        self.assertIn("if (!BraxChampionsKasyno.Wydaj(2 * s.ante))", GAME)                              # the call: twice the hand's ante

    def test_the_tables_and_their_place(self):
        self.assertIn("BraxChampionsKasynoSprzet.Holdem(kas,new Vector3(px,HallFloor+.016f,15.5f),180f);", HALL)
        self.assertIn("BraxChampionsKasynoSprzet.ArkPoker", HALL)
        self.assertIn("hp.karty = kartyStol; hp.filcY = BjTop;", SPRZET)
        self.assertIn('string[] uklad = { "POKER KRÓLEWSKI", "POKER", "KARETA", "FULL", "KOLOR", "STRIT I NIŻEJ" }', KIT)
        self.assertIn('"KRUPIER GRA OD PARY CZWÓREK"', KIT)
        self.assertNotIn("×", KIT.split("static Color32[] FilcHoldem()")[1])        # the letter kit has no ×: never printed
        for f in (RT / "BraxChampionsHoldem.cs.meta", ED / "BraxChampionsKasynoHoldem.cs.meta"):
            self.assertTrue(f.exists(), f)


if __name__ == "__main__":
    unittest.main()
