"""MIASTO R1 — jedna aplikacja miasta (właściciel 10.11 15:06: „żebyśmy dobrze robili robotę na jednym… zrobić porządek”).
Każdy build rejonu `miasto` musi instalować się pod tym samym identyfikatorem (com.braxhorn.miastogl, „Braxhorn Miasto GL”)
i nadpisywać poprzednią wersję. com.braxhorn.city („Braxhorn City”) jest zarezerwowany na wspólną grę. Zasada w projekcie:
ZASADA-APLIKACJE-JEDNA-NA-REJON-10-11."""
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
BUILD = (ROOT / "unity-city/Assets/Editor/BraxGymBuild.cs").read_text(encoding="utf-8")
WF = (ROOT / ".github/workflows/claude-region-build.yml").read_text(encoding="utf-8")


class JednaAplikacjaMiasta(unittest.TestCase):
    def test_city_region_builds_only_miastogl(self):
        self.assertIn('else if(rejon=="miasto")wybrane=new[]{"miastogl"}', BUILD)

    def test_miastogl_identifier_is_fixed(self):
        self.assertIn('new{tag="miastogl",scenes=Pack(cityAll),gl=true,id="com.braxhorn.miastogl",name="Braxhorn Miasto GL"}', BUILD)

    def test_city_identifier_reserved_for_whole_game(self):
        ids = dict(re.findall(r'new\{tag="([a-z]+)",scenes=.*?,id="(com\.braxhorn\.[a-z]+)",name=', BUILD))
        self.assertEqual(ids.get("calosc"), "com.braxhorn.city")      # tylko wspólna gra (miasto + osiedle)
        self.assertEqual(ids.get("miastogl"), "com.braxhorn.miastogl")
        self.assertEqual(len(set(ids.values())), len(ids))            # każdy wariant — inny, stały identyfikator

    def test_region_workflow_default_is_city(self):
        self.assertIn("default: miasto", WF)


if __name__ == "__main__":
    unittest.main()
