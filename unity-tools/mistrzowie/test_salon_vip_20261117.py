"""Owner 16.11.2026, the queue: "Salon VIP i złote monety właściciela w kasynach" (his SALON VIP sign). KASYNO WAWER's
private salon in the hall's rear west corner: his sign over a gold portal, red velvet ropes, the hostess; inside the high
limits - BLACKJACK VIP 100, BAKARAT VIP 200 (the floor's games at their own stakes) - a bar with its barman, a lounge, the
vitrine of his gold coins (his coin art 1:1) and his logo."""
import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"
RT = ROOT / "unity-city/Assets/Mistrzowie"
KIT = (ED / "BraxChampionsKasynoSalonVip.cs").read_text(encoding="utf-8")
HALL = (ED / "BraxChampionsCasinoT11Hall.cs").read_text(encoding="utf-8")
BJ = (RT / "BraxChampionsBlackjack.cs").read_text(encoding="utf-8")
BK = (RT / "BraxChampionsBakarat.cs").read_text(encoding="utf-8")


class SalonVip(unittest.TestCase):
    def test_the_block_in_the_rear_west_corner(self):
        self.assertIn("const float VpX0=-33.0f,VpX1=-25.0f,VpZ0=15.4f,VpZ1=27.35f", HALL)
        self.assertIn('BraxChampionsFoundation.Optional("T11 salon VIP",()=>', HALL)
        self.assertIn("if(vipStoly!=2)BraxBuildQuality.Warn(", HALL)
        self.assertIn("W.MM(marble,VpX0,VpX1,HallFloor+.022f,y,VpZ0,VpZ1);", HALL)      # over the tiles' gold edge, no shared plane

    def test_high_limits_are_the_floor_games_at_their_own_stakes(self):
        self.assertIn("public const int VipBlackjack = 100, VipBakarat = 200;", KIT)
        self.assertIn('k.blackjack[bj].stawka = VipBlackjack; k.blackjack[bj].nazwa = "BLACKJACK VIP";', KIT)
        self.assertIn('k.bakarat[bk].stawka = VipBakarat; k.bakarat[bk].nazwa = "BAKARAT VIP";', KIT)
        for src in (BJ, BK):
            self.assertIn("int St(Stol t) => t != null && t.stawka > 0 ? t.stawka : stawka;", src)
            self.assertIn("BraxChampionsKasyno.Wydaj(St(t))", src)
        self.assertIn("Wyplata(s.gracz, s.krupier, st)", BJ)
        self.assertIn("wyplata = Wyplata(s.pole, w, st)", BK)

    def test_his_things(self):
        self.assertIn("Obraz(b, szyldyArk[ArkVip]", KIT)                           # his SALON VIP sign, 1:1
        self.assertIn("Obraz(b, szyldyArk[ArkLogo]", KIT)                          # his logo over the bar
        self.assertIn("Moneta(b, v + new Vector3(-.56f + c * .14f", KIT)          # his coins (his coin art) in the vitrine
        self.assertIn("Liny(k, new[]", KIT)
        for rola in ("barman salonu VIP", "hostessa salonu VIP"):
            self.assertIn(rola, KIT)

    def test_real_furniture(self):
        for m in ("bar_chair_round_01", "sofa_02", "modern_arm_chair_01", "coffee_table_round_01"):
            self.assertIn(m, KIT)
        self.assertIn('W.Siedzisko(p + Vector3.up * .78f, Vector3.forward, "HOKER");', KIT)
        self.assertTrue((ED / "BraxChampionsKasynoSalonVip.cs.meta").exists())


if __name__ == "__main__":
    unittest.main()
