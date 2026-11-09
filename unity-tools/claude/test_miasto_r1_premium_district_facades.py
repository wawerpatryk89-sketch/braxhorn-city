"""MIASTO R1: nowe dzielnice — budynki historyczne nietknięte, bez płaskiej fasady-nakładki.

Decyzja właściciela 9.11: „Historyczny budynek brak tekstur i coś na nim wisi” → NdPremiumFasada (płaska szyba
na prostokącie w×d bez obrotu budynku, wystająca nad KINNAIRD) i NdZdejmijNadwieszenieZabytku (wyłączanie
rendererów po nazwie materiału glass|black|metal — trafiało w zabytek) USUNIĘTE. Premium nowej części = osobne
zlecenie po nagraniu. Test pilnuje zachowania, nie brzmienia kodu. Source-only; bez Unity i APK.
"""
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SOURCE = (ROOT / "unity-city/Assets/Editor/Claude/Region/"
          "BraxRegionMiastoBDzielnice.cs").read_text(encoding="utf-8")


def strip(source):
    return re.sub(r'//[^\n]*|/\*[\s\S]*?\*/', "", source)


def balanced(source):
    text = re.sub(r'//[^\n]*|/\*[\s\S]*?\*/|@"(?:""|[^"])*"|"((?:\\.|[^"\\])*)"', "", source)
    stack = []
    mate = {")": "(", "]": "[", "}": "{"}
    for char in text:
        if char in "([{":
            stack.append(char)
        elif char in ")]}":
            if not stack or stack.pop() != mate[char]:
                return False
    return not stack


class R1NewDistrictBuildings(unittest.TestCase):
    def test_source_is_syntactically_balanced(self):
        self.assertTrue(balanced(SOURCE))

    def test_historic_buildings_still_in_layout(self):
        self.assertIn('new Bud("KINNAIRD"', SOURCE)
        self.assertGreaterEqual(len(re.findall(r'new Bud\("C3_U\d+", "miasto_kamienice_c3"', SOURCE)), 4)

    def test_no_flat_overlay_facade_generator(self):
        code = strip(SOURCE)
        self.assertNotIn("NdPremiumFasada(", code, "płaska nakładka szklana (7.11) wystawała nad zabytek — usunięta 9.11")
        self.assertNotIn("PREMIUM — przeszklona elewacja", code)

    def test_no_material_name_heuristic_on_heritage(self):
        code = strip(SOURCE)
        self.assertNotIn("NdZdejmijNadwieszenieZabytku", code)
        # NdSzklo z 3.11 (zamiana materiału czarnego szkła na refleksyjne) zostaje; nie wolno natomiast
        # WYŁĄCZAĆ rendererów/kolizji budynku po podciągu nazwy materiału (to zjadło teksturę zabytku)
        self.assertNotRegex(code, r'IndexOf\("(black|metal|szkło)"')
        self.assertNotIn("rend.gameObject.SetActive(false)", code)

    def test_glass_fix_from_3_11_kept(self):
        # właściciel 3.11: „czarne budynki, gdzie są okna” → NdSzklo zostaje
        self.assertIn("NdSzklo(", SOURCE)
        self.assertIn("nSzklo", SOURCE)

    def test_building_collider_uses_source_bounds(self):
        self.assertIn("var originalBounds = BraxRegion.Bounds(bud.gameObject)", SOURCE)
        self.assertIn("var gr = originalBounds;", SOURCE)


if __name__ == "__main__":
    unittest.main()
