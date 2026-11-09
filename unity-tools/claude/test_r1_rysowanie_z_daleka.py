"""MIASTO R1 — rysowanie z daleka (właściciel 9.11 17:42–17:45: „zobacz jak to się rysuje… tak samo wyspa jak do niej płynę —
powinno być globalnie poprawnie”, „jak pokazują się obiekty”).

Przyczyny (APK c63d1b48): zasięg kamery 220/420/700 m na telefonie przy wyspie ~1 km od portu, kwadraty miasta doczytywane
120 m przed krawędzią bez niczego dalej (albo szare pudła), wyspa — sam teren z ośmiokątem do 200 m, tekstury połówkowe na
telefonie, budżet streamingu 384/512 MB z redukcją do 1/8, niskie lodBias, cienie Średni twarde 35 m (sprzeczne z zasadą 1 D).
Test pilnuje zachowania naprawy (źródło, bez Unity i APK).
"""
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


def code(rel):
    return re.sub(r'//[^\n]*|/\*[\s\S]*?\*/', "", (ROOT / rel).read_text(encoding="utf-8"))


GRAF = code("unity-city/Assets/Claude/BraxGrafika.cs")
BUILD = code("unity-city/Assets/Editor/BraxGymBuild.cs")
OBSZ = code("unity-city/Assets/Editor/Claude/BraxClaudeObszary.cs")
HLOD = code("unity-city/Assets/Editor/Claude/BraxClaudeZDaleka.cs")
RUN = code("unity-city/Assets/Claude/Runtime/BraxObszarZDaleka.cs")


def tryb(nazwa, nastepny):
    a = GRAF.index("default:" if nazwa == "Wysoka" else "case Tryb." + nazwa)
    b = GRAF.index(nastepny, a + 1) if nastepny else len(GRAF)
    return GRAF[a:b]


class R1RysowanieZDaleka(unittest.TestCase):
    def test_draw_distance_reaches_island_in_every_mode(self):
        for z in re.findall(r'Zasieg\((\d+)f\)', GRAF):
            self.assertGreaterEqual(int(z), 1100, "zasięg kamery musi sięgać wyspy (~1 km od portu) w każdym trybie")

    def test_shadows_follow_rule_one_in_every_mode(self):
        n, s = tryb("Niska", "case Tryb.Srednia"), tryb("Srednia", "default:")
        w = tryb("Wysoka", None)
        self.assertIn("ShadowQuality.HardOnly", n); self.assertIn("shadowDistance = 40f", n); self.assertIn("shadowCascades = 1", n)
        self.assertIn("ShadowQuality.All", s); self.assertIn("shadowDistance = 80f", s); self.assertIn("shadowCascades = 2", s)
        self.assertEqual(w.count("shadowDistance = 150f"), 2); self.assertEqual(w.count("shadowCascades = 4"), 2)
        self.assertNotIn("ShadowQuality.Disable", GRAF)

    def test_lod_bias_raised(self):
        self.assertIn("lodBias = .7f", tryb("Niska", "case Tryb.Srednia"))
        self.assertIn("lodBias = 1.25f", tryb("Srednia", "default:"))
        self.assertIn("lodBias = 2.0f", tryb("Wysoka", None))

    def test_island_loads_early_and_has_full_far_copy(self):
        self.assertRegex(BUILD, r'"BraxhornCity_Wyspa",BraxClaudeObszary\.PoNazwach\(scene,"CLAUDE wyspa\*"\),450f,600f,Generated,1\.5f,150000\)')

    def test_every_city_square_and_waterfront_leave_a_far_copy(self):
        self.assertIn("Wydziel(scene,sectorName,w,120f,250f,Generated,.5f,100000)", BUILD)
        self.assertIn('"CLAUDE stacja metra BRAXHORN NABRZEŻE"),45f,110f,Generated,.5f,120000)', BUILD)
        self.assertIn("BraxClaudeZDaleka.Utworz(main, nazwaSceny, wezly, zDalekaKomorka, zDalekaMaxTris, folder)", OBSZ)
        self.assertIn("float zDalekaKomorka = 0f", OBSZ)                       # osiedle (inny rejon) bez zmian
        self.assertIn("typeof(BraxObszarZDaleka)", OBSZ)                      # kopia nigdy nie odchodzi do kwadratu

    def test_old_box_silhouettes_retired(self):
        self.assertIn("GetComponentsInChildren<BraxSylwetkiZDaleka>(true)", BUILD)
        self.assertIn("sylZD.sylwetka.gameObject.SetActive(false)", BUILD)

    def test_far_copy_builder(self):
        self.assertIn("KlasaNormalnej(n)", HLOD)                               # przeciwne ściany się nie sklejają
        self.assertIn("if (a == b || b == c || a == c) continue;", HLOD)       # zdegenerowane trójkąty odpadają
        self.assertIn("lods.Length - 1", HLOD)                                 # LODGroup: ostatni poziom, nie grupowany
        self.assertIn('FindProperty("m_IsReadable")', HLOD)                    # tylko GPU
        self.assertIn("ShadowCastingMode.On", HLOD)
        self.assertIn("GetComponentInParent<Rigidbody>(true)", HLOD)            # rzeczy w ruchu poza kopią
        self.assertIn("if (tris <= maxTris) break;", HLOD)

    def test_runtime_switch_in_load_frame(self):
        self.assertIn("SceneManager.sceneLoaded += Wczytano;", RUN)
        self.assertIn("SceneManager.sceneUnloaded += Zwolniono;", RUN)
        self.assertIn("r.enabled = !wczytany", RUN)


if __name__ == "__main__":
    unittest.main()
