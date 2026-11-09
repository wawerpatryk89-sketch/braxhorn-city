"""MIASTO R1 — port: miejsce wejścia metra S1 (właściciel 9.11 17:54, zdjęcie spod bramy: „usunąć te auta i ten domek, bo tu będzie
stacja — wejście do metra”; plan M1 zaakceptowany: S1 PORT, wejście x 66–74, z 6–17). Źródło, bez Unity i APK."""
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PORT = re.sub(r'//[^\n]*', "", (ROOT / "unity-city/Assets/Editor/Claude/Region/BraxMiastoPort.cs").read_text(encoding="utf-8"))


class R1PortMiejsceMetra(unittest.TestCase):
    def test_office_and_terminal_tractors_removed(self):
        self.assertNotIn("static void Biuro()", PORT)
        self.assertNotIn("static void Ciagniki()", PORT)
        self.assertNotIn("Biuro();", PORT)
        self.assertNotIn("Ciagniki();", PORT)
        self.assertNotIn('"port_biuro"', PORT)

    def test_square_paved_where_office_stood(self):
        self.assertNotIn("Rect.MinMaxRect(66.3f, 6.5f, 74.3f, 16.5f)", PORT)
        self.assertIn("Plaszczyzna(BraxUliceKit.S(beton), X0, 83.3f, Z0, Z1, Y, wszystkie)", PORT)

    def test_gate_stays_without_office_reference(self):
        self.assertIn("Brama(); Maszty();", PORT)
        self.assertNotIn("Biuro spedycji", PORT)


if __name__ == "__main__":
    unittest.main()
