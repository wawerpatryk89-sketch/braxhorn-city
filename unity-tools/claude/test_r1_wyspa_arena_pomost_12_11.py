"""MIASTO R1 (12.11.2026): wyspa — WC areny od nowa, szatnie z natryskami i wejście zawodników od tyłu, pomost dla motorówek
i skuterów, deptak plażowy gładki, krawędź placu areny bez ząbków (właściciel 13:35 i 13:51).

13:35 „kible w arenie… tragedia”, „szatnie dla zawodników… od tyłu wejście, tak jak powinno być”, „pomościk… żeby można było
podpływać motorówką i skuterami wodnymi… powiększyć”; 13:51 „deptaki na koło plaży zjebane… WC stoją na chodnikach”.
"""
import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
REG = ROOT / "unity-city/Assets/Editor/Claude/Region"
ARENA = (REG / "BraxMiastoWyspaArena.cs").read_text(encoding="utf-8")
WYSPA = (REG / "BraxMiastoWyspa.cs").read_text(encoding="utf-8")
DOMKI = (REG / "BraxMiastoWyspaDomki.cs").read_text(encoding="utf-8")
MEBEL = (REG / "BraxNabrzezeMebel.cs").read_text(encoding="utf-8")
FETCH = (ROOT / "unity-tools/claude/fetch_region.py").read_text(encoding="utf-8")


def fn(src, name, nxt):
    i = src.index(name)
    return src[i:src.index(nxt, i)]


class ToaletyAreny(unittest.TestCase):
    T = fn(ARENA, "static void ApToalety()", "// TUNEL ZAWODNIKÓW")

    def test_ceramika_wspolna_r1(self):
        self.assertIn("BraxCeramika.MiskaWiszaca(", self.T)
        self.assertIn("BraxCeramika.Pisuar(", self.T)
        self.assertIn("BraxCeramika.ZestawUmywalkowy(", self.T)   # umywalka, bateria z wodą, lustro z odbiciem

    def test_bez_starych_klockow(self):
        self.assertNotIn("Toczenie(D(apCeram)", self.T)              # dawna miska toczona
        self.assertNotIn("D(apLustro).OBox", self.T)                 # dawne płaskie „lustro”
        self.assertIn('"long_white_tiles"', ARENA)
        self.assertNotIn('"square_tiles", "rectangular_facade_tiles", "granite_tile");', ARENA)

    def test_pomieszczenie_poza_trybuna(self):
        self.assertIn("xi1 = apC.x - 18.75f", self.T)
        self.assertIn("zF = apC.z + su * 8.75f", self.T)


class SzatnieIWejscie(unittest.TestCase):
    def test_wejscie_zawodnikow_od_tylu(self):
        self.assertIn("static void ApWejscieZawodnikow(", ARENA)
        self.assertIn('"WEJŚCIE ZAWODNIKÓW"', ARENA)
        self.assertIn('BraxMiastoA.DrzwiPrzesuwne(root, "ARENA — wejście zawodników"', ARENA)
        self.assertIn("if (sluz) ApWejscieZawodnikow(", ARENA)
        self.assertNotIn('"DRZWI SŁUŻBOWE (tunel zawodników)"', ARENA)

    def test_strefa_mokra(self):
        self.assertIn("static void ApStrefaMokra(", ARENA)
        mokra = fn(ARENA, "static void ApStrefaMokra(", "// drzwi zamknięte")
        self.assertIn("BraxCeramika.MiskaWiszaca(", mokra)
        self.assertIn('"anti_skid_tiles"', mokra)
        self.assertIn("SzkloPrzezroczyste", mokra)

    def test_lustro_z_odbiciem(self):
        self.assertIn('BraxCeramika.Lustro(root, kto + " lustro"', ARENA)


class Pomost(unittest.TestCase):
    P = fn(WYSPA, "static float Przystan(", "// DEPTAK z desek na palach")

    def test_glowica_wieksza(self):
        self.assertIn("float tx0 = x0 - 5.0f * K, tz0 = pz - 22f, tz1 = pz + 22f;", self.P)

    def test_motorowki_i_skutery(self):
        self.assertIn('"palec pomostu (motorówki)"', self.P)
        self.assertIn('"pomost skuterów wodnych"', self.P)
        for m in ("wyspa_cumowanie_motorowki", "wyspa_cumowanie_skutera_", "wyspa_pomost_skuterow"):
            self.assertIn(m, self.P)


class DeptakPlazowy(unittest.TestCase):
    D = fn(DOMKI, "// 1) DEPTAK Z DESEK wzdłuż plaży", "// 2) KŁADKI do ratowników")

    def test_gladka_os_i_deski_prostopadle(self):
        self.assertIn("a += .25f", self.D)                        # gęste próbki (było .9°)
        self.assertIn("Quaternion.LookRotation(tt, Vector3.up)", self.D)
        self.assertIn("void Tasma(", self.D)                      # ciągłe deski czołowe i legary
        self.assertIn('"teak_veneer"', self.D)

    def test_lacznik_wchodzi_na_deptak(self):
        self.assertIn("if (Vector2.Distance(q, end) < 1.5f * K + 1.3f) continue;", DOMKI)


class ZeroMigotania(unittest.TestCase):
    def test_schody_beton_pod_stopnica(self):
        self.assertIn("(top - .04f + fundament) * .5f", MEBEL)

    def test_listwy_2_cm(self):
        self.assertIn("sd * (t * .5f + .011f)), R, Vector3.up, Nn * sd, o.Item1 - k0, .08f, .022f, true);", ARENA)

    def test_bez_brazowego_granitu_na_wyspie(self):
        self.assertNotIn('"granite_tile_02"', WYSPA)
        self.assertNotIn('"granite_tile_02", "rough_block_wall"', ARENA)
        self.assertIn('"Wyspa: krawężnik i cokół chodnika (granit szary)"', DOMKI)

    def test_krawedz_placu_drobna(self):
        self.assertIn("odKrawedziPlacu < 2.5f", WYSPA)


class Tekstury(unittest.TestCase):
    def test_nowe_w_ci(self):
        for slug in ("long_white_tiles", "large_floor_tiles_02"):
            self.assertIn('"%s"' % slug, FETCH)


if __name__ == "__main__":
    unittest.main()
