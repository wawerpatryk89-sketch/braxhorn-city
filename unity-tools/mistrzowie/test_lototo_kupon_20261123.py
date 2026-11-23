"""LOTOTO: the coupon on the whole screen (owner 23.11 15:54 and 16:59): AKCJA at the coupon desk opens the coupon, DUŻY
LOTEK (10 of 60, 3 zł) or MAŁY LOTEK (5 of 60, 2 zł, the same draw), numbers crossed with a finger, CHYBIŁ TRAFIŁ,
WYCZYŚĆ, DO KASY; the till takes the money and prints the receipt. The rules are proven in automaty_sim (Lototo)."""
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
A = ROOT / "unity-city/Assets"
KUP = (A / "Mistrzowie/BraxChampionsLototoKupon.cs").read_text(encoding="utf-8")
LOT = (A / "Mistrzowie/BraxChampionsLototo.cs").read_text(encoding="utf-8")
GEN = (A / "Editor/Mistrzowie/BraxChampionsKasynoLototo.cs").read_text(encoding="utf-8")
SIM = (ROOT / "unity-tools/mistrzowie/automaty_sim/Program.cs").read_text(encoding="utf-8")


class Zasady(unittest.TestCase):
    def test_small_lotto_on_the_same_draw(self):
        self.assertIn("malyTypy = 5, malyCena = 2, malyWygrane = new[] { 0, 0, 0, 2, 10, 200 },", LOT)
        self.assertIn("public int Wygrana(int[] kupon, int trafienia, DateTime termin)", LOT)
        self.assertIn("suma += Z.Wygrana(c.liczby, h, termin);", LOT)
        self.assertIn("kumulacja |= !Z.Maly(c.liczby) && h >= Z.typy;", LOT)       # the kumulacja only for ten of ten

    def test_simulator_covers_the_flow(self):
        for s in ('"lototo: DUŻY LOTEK bought"', '"lototo: MAŁY LOTEK bought"', '"lototo: paid at the till, kept"',
                  '"lototo: an unfinished coupon is not sold"', '"lototo: a coupon with a number twice is refused"'):
            self.assertIn(s, SIM)


class Ekran(unittest.TestCase):
    def test_opens_at_the_desk_and_the_till(self):
        self.assertIn('if (Odl(pulpit, g) < 1.3f) return Z.nazwa + " · WYPEŁNIJ KUPON";', LOT)
        self.assertIn("if (Odl(pulpit, g) < 1.3f) { BraxChampionsLototoKupon.Otworz(this, false); return true; }", LOT)
        self.assertIn("BraxChampionsLototoKupon.Otworz(this, true);", LOT)
        self.assertIn("lot.pulpit = pp;", GEN)

    def test_the_game_waits_and_nothing_under_reacts(self):
        self.assertIn("if (tc && tc.Gracz) { tc.Pauza(on);", KUP)
        self.assertIn("if (Event.current.isMouse) Event.current.Use();", KUP)
        self.assertIn("GUI.depth = -25;", KUP)
        self.assertIn("if (otwarty && Input.GetKeyDown(KeyCode.Escape)) Zamknij();", KUP)

    def test_buttons_and_games(self):
        for t in ('"CHYBIŁ TRAFIŁ"', '"WYCZYŚĆ"', '"ZAMKNIJ"', '"KUPUJĘ · "', '"DO KASY · "', '"DUŻY LOTEK · "', '"MAŁY LOTEK · "'):
            self.assertIn(t, KUP)
        self.assertIn("else if (skreslone.Count < typy) skreslone.Add(n);", KUP)   # never more than the game's numbers
        self.assertIn("bool gotowy = zostalo == 0;", KUP)                          # paid only when complete

    def test_receipt(self):
        self.assertIn("BraxChampionsLototoKupon.Paragon(new[]", LOT)
        self.assertIn('"POWODZENIA!"', LOT)
        self.assertIn("KUPON NR", LOT)


if __name__ == "__main__":
    unittest.main()
