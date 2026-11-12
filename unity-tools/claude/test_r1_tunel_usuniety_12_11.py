"""MIASTO R1 (12.11.2026): tunel na osiedle usunięty (właściciel 12:54: „ten tunel trzeba wyjebać, nawet drzewa w nim rosną…
A potem się pomyśli, jak połączyć osiedle z miastem”).

Podgląd z kodu 12.11 pokazał: wzgórze nad tunelem spłaszczone do 0.15 m (Przekop zwracał YC, WysKorytarza bierze minimum),
portal stał samotnie w płaskim lesie, drzewa rosły w rurze tunelu, a tunel po 62 m kończył się przy torach. Usunięte za zgodą
właściciela: portal z napisem TUNEL · OSIEDLE, wnętrze 62 m, zawrót gracza, drogowskaz E-2, zbocze, wzgórze z murami oporowymi.
Aleja Południowa kończy się zakrętem (łuk zewnętrzny) w ul. Dworcową; za nią trawnik i las.
"""
import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
REG = ROOT / "unity-city/Assets/Editor/Claude/Region"
B = (REG / "BraxRegionMiastoB.cs").read_text(encoding="utf-8")
DW = (REG / "BraxMiastoDworzec.cs").read_text(encoding="utf-8")
TORY = (REG / "BraxMiastoDworzecTory.cs").read_text(encoding="utf-8")


class TunelUsuniety(unittest.TestCase):
    def test_no_tunnel_steps_or_geometry(self):
        for zakaz in ('Krok("2. budowle: portal tunelu i zbocze"', "static void WyjazdNaOsiedle", "static void SzlabanIZnak",
                      "static void Zbocze(", 'Napis(g, "TUNEL · OSIEDLE"', "AddComponent<BraxTunelZawrot>", 'Asfalt("tunel"',
                      '"Aleja Południowa (do tunelu)"'):
            self.assertNotIn(zakaz, B, zakaz)

    def test_no_hill_or_retaining_walls(self):
        self.assertNotIn("public static float Garb(", DW)
        self.assertNotIn("GarbX1", DW + TORY)
        self.assertNotIn("MuryGarbu", TORY)
        self.assertNotIn("filar: przedłużenie muru portalu", TORY)


class KoniecAlei(unittest.TestCase):
    def test_aleja_turns_into_dworcowa_with_outer_arc(self):
        self.assertIn('LukZew("Aleja Płd. → ul. Dworcowa (łuk zewnętrzny, koniec alei)", px, zK, 1, 1, rL);', B)
        self.assertIn("float zK = BraxMiastoDworzec.DworcowaZ - BraxMiastoDworzec.Pas, rL = 2f * BraxMiastoDworzec.Pas;", B)
        # pełny przekrój alei tylko do środka łuku (pn. krawędź wylotu ul. Dworcowej) — niżej ręcznie, bez nakładania
        self.assertIn('PrzekrojZ("Aleja Południowa (do ul. Dworcowej)", AX, AH, Ziel, Chod, cy, zW0', B)

    def test_end_sidewalk_polygon_meets_arc_fan_edge(self):
        # górna krawędź chodnika końca alei = przekątna wachlarza łuku (P → koniec R + Kr) i bok wachlarza (P → początek R + Kr)
        self.assertIn("new Vector2(cx, zK - Kr),\n                new Vector2(px, zK), new Vector2(xw1, cy) }", B)

    def test_lawn_to_forest_and_traffic_axis_end(self):
        self.assertIn('Trawa("Koniec Alei Płd. — trawnik do lasu", LicoW, LicoE, ZboczeZ, AS1);', B)
        self.assertIn("new Vector3(AX, YA, AlejaKoniecZ), AH);", B)
        self.assertNotIn("AS1 - 18f", B)

    def test_flattening_note_kept_for_future_hills(self):
        self.assertIn("każde wzniesienie pasa poza przekopem", DW)


if __name__ == "__main__":
    unittest.main()
