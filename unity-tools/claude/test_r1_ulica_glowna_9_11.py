"""MIASTO R1 — ulica główna z zewnątrz (właściciel 9.11 16:58: „cała ulica… latarnie, szyldy prawidłowe, wszystko od A do Z”).

Pilnuje zachowania napraw z 9.11 (źródło, bez Unity i APK):
- nad zabytkiem nic nie wisi: nawisy brył poza obrysem parteru przycinane z geometrii,
- pod drzewami ulicy nie ma zielonych klocków (walców bylin) — kępy trawy z modeli,
- w hali supermarketu i w wejściu nie zostaje obcy obiekt stojący (rozpoznanie geometrią, nie nazwą),
- tarcze znaków: lico dwustronne nie zeruje normalnej,
- rondo bez „rzeźby” z walców.
"""
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
REG = ROOT / "unity-city/Assets/Editor/Claude/Region"


def code(name):
    return re.sub(r'//[^\n]*', "", (REG / name).read_text(encoding="utf-8"))


class R1UlicaGlowna(unittest.TestCase):
    def test_overhangs_trimmed_from_geometry(self):
        c = code("BraxRegionMiastoBDzielnice.cs")
        self.assertIn("NdPrzytnijNawisy();", c)
        body = c[c.index("static void NdPrzytnijNawisy()"):]
        self.assertIn("p.y > y0 + Parter", body)                      # obrys parteru z wierzchołków
        self.assertIn("BraxCityMeshCut.Subtract(bud.transform", body)  # cięcie precyzyjne, nie wyłączanie rendererów
        self.assertIn("poleNawis > poleWszystko * .25", body)          # parter na słupach — bez cięcia, ostrzeżenie
        self.assertNotIn("SetActive(false)", body)

    def test_airspace_above_heritage_is_free(self):
        c = code("BraxRegionMiastoBDzielnice.cs")
        body = c[c.index("static void NdPrzytnijNawisy()"):]
        self.assertIn('q.name == "Budynek KINNAIRD"', body)
        self.assertIn("kb.max.y + 400f", body)
        self.assertIn('"nad_kinnaird"', body)
        self.assertIn("r.transform.IsChildOf(kin.transform)", body)            # sam zabytek nietknięty
        self.assertIn("granica = działka z planu", body)                       # duży nawis → granica działki, nie „pomiń”

    def test_no_green_block_perennials_under_street_trees(self):
        c = code("BraxMiastoUlica.cs")
        self.assertNotIn("byliny misy", c)
        self.assertIn('BraxRegion.Model("grass_medium_01", "grass_medium_02")', c)

    def test_supermarket_clears_foreign_objects_by_geometry(self):
        c = code("BraxSupermarket.cs")
        body = c[c.index("static void OczyscWejscie"):]
        self.assertIn("rb.size.y < .6f || rb.size.x > 12f", body)
        self.assertIn("GetComponentInParent<LODGroup>()", body)
        self.assertIn("r is SkinnedMeshRenderer", body)

    def test_supermarket_hall_is_cut_free_of_lawn_fence_and_trees(self):
        # 9.11 17:30: „sklep funkcjonalny, drzewa usunąć z niego, dojście ma być, wszystko” — trawnik i płot w hali
        c = code("BraxSupermarket.cs")
        self.assertIn("OczyscHale(scena, root.transform, t, i + 1)", c)
        body = c[c.index("static void OczyscHale("):]
        self.assertIn('BraxCityMeshCut.Subtract(go.transform, hala', body)    # siatki otoczenia wycięte w obrysie hali
        self.assertIn("t.IsChildOf(shopRoot)", body)                          # sam sklep nietknięty
        self.assertIn("BraxClaudeTrawa", body)                                # kępy trawy z hali usunięte z danych
        self.assertIn("(poza halą)", body)                                    # kolizje przycięte, nie niewidzialne ściany
        b = code("BraxRegionMiastoB.cs")
        self.assertIn('Chodnik("SUPERMARKET BRAXHORN: dojście od parkingu"', b)

    def test_sign_face_normal_handles_double_sided_face(self):
        c = code("BraxZnakiPL.cs")
        self.assertIn("Vector3.Dot(cr, wzor) < 0f ? -cr : cr", c)

    def test_roundabouts_have_no_square_island_plate(self):
        # 9.11 17:28: „ronda mają być okrągłe wszystkie” — kwadratowa płyta pod wyspą wystawała narożnikami
        c = code("BraxRegionMiastoB.cs")
        self.assertNotIn('Plyta("Rondo: podłoże wyspy"', c)
        self.assertNotIn('Plyta("Pętla ZACHÓD: podłoże wyspy"', c)
        self.assertIn('Pierscien(kraw, c, RWyspa - Kr, RWyspa, YC, 64)', c)
        self.assertIn('Pierscien(kraw, c, PetlaWyspa - Kr, PetlaWyspa, YC, 48)', c)

    def test_roundabout_crude_sculpture_not_placed(self):
        c = code("BraxRegionMiastoB.cs")
        self.assertNotIn('Pozniej("rzeźba"', c)


if __name__ == "__main__":
    unittest.main()
