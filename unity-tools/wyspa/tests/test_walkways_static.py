"""Short static regression checks for R1 island walking paths (NO Unity or APK).

These checks prove source wiring, NOT physical passage. The nonblocking Unity
physics sweep in BraxMiastoWyspa.AudytCalejWyspy runs at scene generation.
"""
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[3]
DOMKI = ROOT / "unity-city/Assets/Editor/Claude/Region/BraxMiastoWyspaDomki.cs"
WYSPA = ROOT / "unity-city/Assets/Editor/Claude/Region/BraxMiastoWyspa.cs"
ARENA = ROOT / "unity-city/Assets/Editor/Claude/Region/BraxMiastoWyspaArena.cs"


class WholeIslandWalkwaysSourceTest(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.domki = DOMKI.read_text(encoding="utf-8")
        cls.wyspa = WYSPA.read_text(encoding="utf-8")
        cls.arena = ARENA.read_text(encoding="utf-8")

    def test_resort_avenues_and_cottage_links_exist(self):
        for name in (
            '"aleja A1"', '"aleja A2 (plaża ↔ domki)"',
            '"aleja A3"', '"łącznik z placu areny"',
            '"dojście do recepcji"', '"dojście do sklepu"',
            '"dojście do domku "',
        ):
            self.assertIn(name, self.domki)

    def test_intersection_edges_trim_to_submetre_boundaries(self):
        self.assertIn("float Granica(float lo, float hi, bool stanNaLewo)", self.domki)
        self.assertIn("float zmiana = Granica(prev, teraz, przykryte);", self.domki)
        self.assertIn("Fragment(pocz, zmiana);", self.domki)
        self.assertNotIn("!WInnym(pm, ja, 0f, true)", self.domki)

    def test_only_existing_pavement_has_collision(self):
        self.assertIn("void Fragment(float t0, float t1)", self.domki)
        self.assertIn('"Wyspa chodnik fizyka " + nazwa', self.domki)
        self.assertIn("new Vector3(szer, .3f, v.magnitude + .012f)", self.domki)
        self.assertIn("float RawY(Vector2 p)", self.domki)
        self.assertIn("float TopY(float t)", self.domki)

    def test_beach_walkway_has_physical_lifeguard_gangways(self):
        self.assertIn("KladkaZajmuje(Vector3 punkt", self.domki)
        self.assertIn('KolRot(kol, "Wyspa kładka do ratownika (fizyka)"', self.domki)
        self.assertIn("plazaDeptak.AddRange(pts);", self.domki)
        self.assertIn("!KladkaZajmuje(lp, 1.4f)", self.domki)

    def test_reception_shop_beach_and_bins_are_connected_to_plan(self):
        self.assertIn('"łącznik A2 do promenady plażowej"', self.domki)
        self.assertIn('"wyspa_polaczenie_plaza_a2"', self.domki)
        self.assertIn('!WInnym(kp, -1, .65f, false)', self.domki)
        self.assertIn('if (WInnym(c, -1, Mathf.Max(w, d)', self.domki)

    def test_dock_joint_starts_at_deck_level(self):
        self.assertIn("prof.Count == 0 ? Mathf.Max(deck, mx + .12f)", self.wyspa)
        self.assertIn("WYSPA_POMOST_STYK", self.wyspa)

    def test_full_route_qa_only_after_all_props(self):
        self.assertIn("internal static void AudytCalejWyspy(Transform wyspa)", self.domki)
        self.assertIn("WYSPA_CALA_TRASA_AUDYT", self.domki)
        self.assertIn("AudytCalejWyspy(root)", self.wyspa)
        self.assertLess(self.wyspa.index("int palm = Palmy(), krz = Krzewy();"),
                        self.wyspa.index("AudytCalejWyspy(root)"))
        self.assertIn("BraxBuildQuality.Warn(wynik", self.domki)

    def test_optional_entrance_plants_cannot_abort_apk(self):
        self.assertIn('BraxBuildQuality.Warn("BRAXHORN_WYSPA: brak oryginalnych roślin', self.arena)
        self.assertNotIn('if (n == 0) throw new System.InvalidOperationException("BRAXHORN_WYSPA', self.arena)


if __name__ == "__main__":
    unittest.main()
