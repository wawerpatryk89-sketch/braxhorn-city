"""BRAXHORN R1 island beach deck, lifeguard walkways, beach services and resort paths.

Static and analytic source regressions only. No Unity scene/playtest here.
Stan po 9.11: plaża premium i ośrodek domków z buildu 6.11 (commit 9d9aff5);
każdy brak = ostrzeżenie BRAXHORN_WYSPA…, build idzie dalej.
"""
from pathlib import Path
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]
SOURCE = (ROOT / "unity-city/Assets/Editor/Claude/Region/"
          "BraxMiastoWyspaDomki.cs").read_text(encoding="utf8")
BASE = (ROOT / "unity-city/Assets/Editor/Claude/Region/"
        "BraxMiastoWyspa.cs").read_text(encoding="utf8")
ARENA = (ROOT / "unity-city/Assets/Editor/Claude/Region/"
         "BraxMiastoWyspaArena.cs").read_text(encoding="utf8")


def cs_balanced(source):
    pattern = r'//[^\n]*|/\*[\s\S]*?\*/|@"(?:""|[^"])*"|"((?:\\.|[^"\\])*)"'
    stripped = re.sub(pattern, "", source)
    stack = []
    pair = {")": "(", "]": "[", "}": "{"}
    for char in stripped:
        if char in "([{":
            stack.append(char)
        elif char in ")]}":
            if not stack or stack.pop() != pair[char]:
                return False
    return len(stack) == 0


def strip_comments(source):
    return re.sub(r'//[^\n]*|/\*[\s\S]*?\*/', "", source)


class IslandBeachAndResort(unittest.TestCase):
    def test_csharp_source_lexically_valid(self):
        self.assertTrue(cs_balanced(SOURCE))
        self.assertIn("internal static partial class BraxMiastoWyspa", SOURCE)

    def test_beach_deck_sits_on_joists_with_colliders_and_no_steps(self):
        # deptak plażowy ~38 m od wody, 25 cm nad piaskiem, wygładzony, na legarach i palach, z kolizją każdego odcinka
        self.assertIn("var p = new Vector3(CX, 0, CZ) + d * (rs - 38f);", SOURCE)
        self.assertIn("p.y = yMax + .25f; pts.Add(p);", SOURCE)
        self.assertIn("(pts[i - 1].y + pts[i + 1].y) * .5f", SOURCE)
        self.assertIn('BraxNabrzeze.KolRot(kol, "Deptak plażowy"', SOURCE)
        self.assertIn("plazaDeptak.AddRange(pts);", SOURCE)

    def test_lifeguard_walkways_share_level_with_deck_and_limit_grade(self):
        self.assertIn("height[0] = Mathf.Max(height[0], promenadeY);", SOURCE)
        self.assertIn("height[i] = Mathf.Max(height[i], height[i - 1] - .12f * step);", SOURCE)
        self.assertIn("height[i] = Mathf.Max(height[i], height[i + 1] - .12f * step);", SOURCE)
        self.assertIn('BraxNabrzeze.KolRot(kol, "Wyspa kładka do ratownika (fizyka)"', SOURCE)
        self.assertIn('Ostrzez(wynikKladki + " — ostrzeżenie bez blokowania APK");', SOURCE)

    def test_beach_services_are_preserved(self):
        self.assertIn("nPrzebieralnie++;", SOURCE)
        self.assertIn('Miejsca["wyspa_wypozyczalnia"]', SOURCE)
        self.assertIn('BarPlazowy(bc, "wyspa_bar_plaza", "BAR PLAŻOWY")', SOURCE)
        self.assertIn('Miejsca["wyspa_wc"]', SOURCE)
        self.assertIn("PunktWidokowy()", BASE)

    def test_wc_has_real_walls_doors_and_recorded_entrances(self):
        # WC murowane: cztery ściany z ościeżami, przegroda, podłoga, dwoje drzwi 0.9 × 2.1 m × K otwartych na zewnątrz
        self.assertIn("float szerWC = .9f * K, wysWC = 2.1f * K", SOURCE)
        self.assertIn('ScianaWC("tyl"', SOURCE)
        self.assertIn('ScianaWC("przegroda"', SOURCE)
        self.assertIn('BraxNabrzeze.Kol(kol, "WC plazy podloga"', SOURCE)
        self.assertIn('BraxNabrzeze.KolRot(kol, "WC plazy otwarte drzwi"', SOURCE)
        self.assertIn('wejsciaDomkow.Add((o < 0 ? "WC DAMSKI" : "WC MESKI"', SOURCE)
        self.assertGreater(2.1 * 1.294, 2.25)  # Brax controller height

    def test_resort_paths_are_slabs_with_plinth_grade_limit_and_physics(self):
        self.assertIn("static void Chodnik(BraxNabrzeze.Zbior z, int ja)", SOURCE)
        self.assertIn("return W + y + .09f;", SOURCE)
        self.assertIn("profil[i] = Mathf.Max(profil[i], profil[i - 1] - krok * .12f);", SOURCE)
        self.assertIn("profil[i] = Mathf.Max(profil[i], profil[i + 1] - krok * .12f);", SOURCE)
        self.assertIn('BraxNabrzeze.KolRot(kol, "Wyspa chodnik fizyka " + nazwa', SOURCE)
        self.assertIn("Zajmij(S3(pm, 0f), hw + .9f);", SOURCE)
        for aleja in ('"aleja A1"', '"aleja A2 (plaża ↔ domki)"', '"aleja A3"',
                      '"łącznik z placu areny"', '"łącznik A2 do promenady plażowej"'):
            self.assertIn(aleja, SOURCE)
        self.assertIn("concrete_pavers_03", SOURCE)

    def test_cottages_have_plinth_open_door_and_entrance_records(self):
        self.assertIn("if (hMin < 1.6f || hMax - hMin > 2.2f) return null;", SOURCE)
        self.assertIn("float y0 = W + hMax + .6f, yMin = W + hMin - .3f;", SOURCE)
        self.assertIn('BraxNabrzeze.KolRot(kol, "Domek " + tabliczka + " otwarte skrzydlo"', SOURCE)
        self.assertIn('BraxNabrzeze.Kol(kol, "Domek " + tabliczka + " podloga"', SOURCE)
        self.assertIn("wejsciaDomkow.Add((tabliczka, osDrzwi + fw * .75f, osDrzwi - fw * .85f));", SOURCE)
        self.assertIn("if (Zajete(CX + px, CZ + pz, 1.6f)) continue", BASE)

    def test_failures_are_warnings_not_build_stops(self):
        code = strip_comments(SOURCE)
        self.assertEqual(code.count("throw new"), 0)
        self.assertNotIn("BraxBuildQuality", SOURCE)
        for phrase in ('Ostrzez("BRAXHORN_WYSPA_DO_POPRAWKI bar plażowy (pominięty): " + e);',
                       'Ostrzez("BRAXHORN_WYSPA_DO_POPRAWKI plac zabaw (pominięty): " + e);',
                       'Ostrzez("BRAXHORN_WYSPA_DO_POPRAWKI chodnik " + plan[k].nazwa + " (pominięty): " + e);',
                       'brak materiału otwartego morza (HarbourWater)'):
            self.assertIn(phrase, SOURCE)
        # kroki wyspy przez odbiór, który nie rzuca dalej
        for krok in ('Krok("plaża premium"', 'Krok("ośrodek domków"', 'Krok("plaże"', 'Krok("bar"',
                     'Krok("palmy"', 'Krok("morze otwarte"', 'Krok("ambient"', 'Krok("ludzie (miejsca)"'):
            self.assertIn("BraxWyspaOdbior." + krok, BASE)

    def test_pbr_finishes_use_region_textures(self):
        for slug in ("white_plaster_02", "concrete_wall_008", "granite_tile_02", "wood_plank_wall", "roof_tiles"):
            self.assertIn(slug, SOURCE)
        self.assertIn("internal static void Toczenie(UlSiatka", ARENA)
        self.assertIn("WC 1, wypożyczalnia 1", SOURCE)


if __name__ == "__main__":
    unittest.main()
