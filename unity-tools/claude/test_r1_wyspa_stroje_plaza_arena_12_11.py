"""MIASTO R1 (12.11.2026, właściciel 16:34–16:36): „ludzi rozdzielić: golasy na plażę, a ubrani do areny”.

Dawniej cała wyspa (x > 900) liczyła się jako plaża, więc postacie w strojach kąpielowych (Sports_Male_01 — 93 % skóry,
Sports_Female_01 — 87 %) trafiały na trybuny, do narożników klatki i szatni. Teraz obsada wyspy mówi wprost, które miejsce
jest plażowe; reszta wyspy jest zawsze ubrana, a na plaży przechodnie dostają strój kąpielowy.
"""
import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
LUDZIE = (ROOT / "unity-city/Assets/Editor/Claude/BraxClaudeOsiedleLudzie.cs").read_text(encoding="utf-8")
RUCH = (ROOT / "unity-city/Assets/Editor/Claude/Region/BraxRegionRuchMiasta.cs").read_text(encoding="utf-8")


class StrojePlazaArena(unittest.TestCase):
    def test_wyspa_nie_jest_cala_plaza(self):
        self.assertIn("static bool NaPlazy(Vector3 p) => strojPlazowy ?? (p.x > 84f && p.x < 190f && p.z > 100f && p.z < 178f);", LUDZIE)
        self.assertNotIn("|| p.x > 900f;", LUDZIE)

    def test_obsada_wyspy_wskazuje_miejsca_plazowe(self):
        self.assertIn('gdzie.StartsWith("Plaża wyspy", StringComparison.Ordinal)', RUCH)
        self.assertIn('gdzie.StartsWith("Woda przy plaży", StringComparison.Ordinal)', RUCH)
        self.assertIn('gdzie.StartsWith("Brzeg plaży", StringComparison.Ordinal)', RUCH)
        self.assertIn("strojPlazowy = plazowe;", RUCH)
        self.assertIn("finally { strojPlazowy = null; }", RUCH)

    def test_na_plazy_stroj_kapielowy_tylko_dla_przechodniow(self):
        self.assertIn("if (miasto && strojPlazowy == true && rola == RolaPrzechodzien && p.skora <= ProgKapielowy)", LUDZIE)
        self.assertIn("const float ProgKapielowy = .7f;", LUDZIE)

    def test_poza_plaza_zamiana_na_ubranych_zostaje(self):
        self.assertIn("if (miasto && !NaPlazy(pos) && p.skora > ProgSkory)", LUDZIE)

    def test_raport(self):
        self.assertIn('" plaza_kapielowe=" + strojePlaza', LUDZIE)
        self.assertIn('" w_tym_plaza_stroje_kapielowe=" + naPlazy', RUCH)


if __name__ == "__main__":
    unittest.main()
