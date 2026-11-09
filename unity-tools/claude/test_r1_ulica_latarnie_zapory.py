"""MIASTO R1: ulica główna — źródłowe latarnie (dubel z nowymi) i pachołki-zapora wyłączane PO NUMERACH CityMesh;
ZaporyNaJezdni nie kasuje oznakowania ani scalonych słupów znaków.

Właściciel 9.11 11:41: „Na ulicy w mieście latarnie dublują się, źle ustawione znaki, latarnie na środku drogi,
przed rondem ogrodzenie na drodze z siatki i szlaban usunąć”. Log APK 8.11: ULICE_R1_ODBIOR stare_zrodlo=0 (manifest ma
puste originalName — rozpoznanie po nazwie nie działa); BRAXHORN_DROGI_ZAPORY skasował słupy znaków (163,8 m) i farbę
jezdni (645 m, 691 m). Source-only; bez Unity i APK.
"""
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ENV = (ROOT / "unity-city/Assets/Editor/CityRebuild/Environment/BraxCityEnvironment.cs").read_text(encoding="utf-8")
REBUILD = (ROOT / "unity-city/Assets/Editor/CityRebuild/BraxCityRebuild.cs").read_text(encoding="utf-8")
RUCH = (ROOT / "unity-city/Assets/Editor/Claude/Region/BraxRegionRuchMiasta.cs").read_text(encoding="utf-8")


def ints(name, src):
    m = re.search(name + r"\s*=\s*\{([^}]*)\}", src, re.S)
    return [int(x) for x in re.findall(r"\d+", m.group(1))] if m else []


class R1MainStreetSourceFixtures(unittest.TestCase):
    def test_source_lamps_listed_by_mesh_number_in_full_triplets(self):
        ids = ints("SourceMainStreetLampMeshes", ENV)
        self.assertEqual(len(ids), 48)           # 16 latarni × (stopa, słup, wysięgnik)
        self.assertEqual(len(set(ids)), 48)
        self.assertEqual(ids[:3], [45, 46, 47])
        self.assertEqual(ids[-3:], [96, 97, 98])
        # latarnie promenady (69–74, 99–104) zostają
        for i in (69, 70, 71, 72, 73, 74, 99, 100, 101, 102, 103, 104):
            self.assertNotIn(i, ids)

    def test_viaduct_bollards_listed_by_mesh_number(self):
        self.assertEqual(ints("SourceViaductBollardMeshes", ENV), [157, 158, 159, 160, 161, 162])

    def test_rebuild_disables_them_and_removes_round_colliders(self):
        self.assertIn("Disable(original,BraxCityEnvironment.SourceMainStreetLampMeshes)", REBUILD)
        self.assertIn("Disable(original,BraxCityEnvironment.SourceViaductBollardMeshes)", REBUILD)
        self.assertIn("RemoveRoundColliders(physics,BraxCityEnvironment.SourceMainStreetLampFeet", REBUILD)
        self.assertIn("RemoveRoundColliders(physics,BraxCityEnvironment.SourceViaductBollardFeet", REBUILD)
        self.assertIn('t.name.StartsWith("Original round collider"', REBUILD)

    def test_barrier_remover_spares_paint_signposts_and_long_meshes(self):
        body = RUCH[RUCH.index("static void ZaporyNaJezdni"):]
        body = body[:body.index("// ---- MIEJSCA LUDZI")]
        self.assertIn("if (L > 16f) continue;", body)
        self.assertRegex(body, r'b\.size\.y < \.10f \|\| n\.Contains\("oznakowanie"\)')
        self.assertIn('n.Contains("słupy znaków")', body)


if __name__ == "__main__":
    unittest.main()
