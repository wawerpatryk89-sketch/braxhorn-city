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


class R1Mural19OnGymWall(unittest.TestCase):
    """Właściciel 6.11: „cała szara ściana, grafika 19, otwór na drzwi”; 9.11: „mural przed siłownią — to ma być na ścianie”.
    Pomiar modelu siłowni: szara elewacja Gym_Mesh_37 ma lico z = 11.6 (drzwi i szyld w tej samej płaszczyźnie), a z = 20.6 to
    otwarty wylot dziedzińca — stary mural wisiał tam 9 m PRZED ścianą. Mural ma powstawać z pomiaru geometrii (promienie od
    ulicy na tymczasowe kolizje siatek), nie ze stałych współrzędnych planu."""
    SRC = (ROOT / "unity-city/Assets/Editor/Claude/Region/BraxMiastoBillboardy.cs").read_text(encoding="utf-8")

    def body(self):
        code = re.sub(r'//[^\n]*', "", self.SRC)
        return code[code.index("static void MuralSilowni"):]

    def test_no_planned_street_line_coordinates(self):
        b = self.body()
        self.assertNotIn("planZ", b)
        self.assertNotIn("20.60f", b)
        self.assertNotIn("x0 = -.80f", b)

    def test_wall_measured_from_real_geometry(self):
        b = self.body()
        self.assertIn("AddComponent<MeshCollider>()", b)          # tymczasowe kolizje siatek (ściana siłowni nie ma kolizji siatki)
        self.assertIn("Physics.RaycastAll(", b)                    # promienie od ulicy
        self.assertIn('StartsWith("Gym_Mesh_"', b)                # ściana = siatka siłowni
        self.assertIn("DestroyImmediate(s)", b)                    # sondy usunięte po pomiarze

    def test_door_and_sign_in_wall_plane_stay_open(self):
        b = self.body()
        self.assertIn("dz >= -.05f && dz < .30f", b)              # drzwi / szyld w płaszczyźnie ściany → otwór
        self.assertIn("otwory++", b)

    def test_only_wall_sized_objects_hide_the_mural(self):
        b = self.body()
        self.assertIn("SzerokiMur(", b)
        self.assertIn("(x - .75f, y), (x + .75f, y), (x, y + 1f)", b)

    def test_no_fake_wall_colliders(self):
        self.assertNotIn("AddComponent<BoxCollider>()", self.body())

    def test_not_placed_without_wall(self):
        self.assertIn("mural NIE postawiony", self.body())


class R1SupermarketAtParking(unittest.TestCase):
    """Właściciel 9.11: „rondo koło sklepu precz ma być parking dla sklepu, w sklepie stoi drzewo i nie można wejść, chodnik źle”.
    APK 8.11: parking (−126,104), a sklep #1 na (−108,132) — 22 m luki. Teraz sklep #1 stoi na stałe przed parkingiem, drzwi w osi alei."""
    SHOP = (ROOT / "unity-city/Assets/Editor/Claude/Region/BraxSupermarket.cs").read_text(encoding="utf-8")

    def test_store_one_pinned_in_front_of_parking(self):
        body = self.SHOP[self.SHOP.index("public static void Wykonaj"):]
        self.assertIn("BraxRegionMiastoB.PetlaZ + BraxRegionMiastoB.PetlaPlac + 5.5f", body)
        self.assertIn("c = new Vector3(BraxRegionMiastoB.AX, y, frontZ)", body)

    def test_store_one_door_on_avenue_axis(self):
        self.assertIn("static float WejscieX(bool lustro, int nr) => nr == 1 ? 0f :", self.SHOP)
        self.assertIn("xe = WejscieX(lustro, nr)", self.SHOP)
        self.assertIn("entryX = WejscieX(mirror, nr)", self.SHOP)

    def test_tree_recognition_includes_trunk_colliders(self):
        self.assertRegex(self.SHOP, r'bool IsTree\(string x\) => Named\(x,[^)]*"pień"[^)]*"trunk"')
