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


SHADER = (ROOT / "unity-city/Assets/CityRebuild/Shaders/CityStaticPBR.shader").read_text(encoding="utf-8")


class R1CienieIMgielka(unittest.TestCase):
    """9.11: „na średnim cieni nie ma” (miasto nie miało ani słońca z cieniami, ani przebiegu ShadowCaster w shaderze miasta);
    „zajebista realna panorama z dachu Tower” — perspektywa powietrzna zgodna z tłem nieba."""

    def test_city_shader_receives_and_casts_sun_shadows(self):
        self.assertIn('#include "AutoLight.cginc"', SHADER)
        self.assertIn("#pragma multi_compile_fwdbase", SHADER)
        self.assertIn("UNITY_SHADOW_COORDS(5)", SHADER)
        self.assertIn("UNITY_TRANSFER_SHADOW(o, v.uv1);", SHADER)
        self.assertIn("UNITY_LIGHT_ATTENUATION(cienSlonca, i, i.worldPos);", SHADER)
        self.assertIn("min(saturate(i.baked.x), cienSlonca)", SHADER)
        self.assertIn('Tags { "LightMode"="ShadowCaster" }', SHADER)
        self.assertIn("TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)", SHADER)
        self.assertIn("SHADOW_CASTER_FRAGMENT(i)", SHADER)
        self.assertIn("clip(0.5 - _Transparent);", SHADER)

    def test_city_sun_casts_soft_shadows_except_gym_hall(self):
        self.assertIn("light.transform.rotation=Quaternion.LookRotation(-BraxCityMaterials.SunDirection);light.shadows=LightShadows.Soft;", BUILD)
        self.assertIn("RenderSettings.sun=light;", BUILD)
        self.assertIn("AddComponent<BraxSlonceWnetrza>()", BUILD)
        run = code("unity-city/Assets/Claude/Runtime/BraxSlonceWnetrza.cs")
        self.assertIn("slonce.shadows = w ? LightShadows.None : cienie;", run)

    def test_aerial_haze_matches_sky(self):
        self.assertIn("_BraxMgielkaGestosc", SHADER)
        self.assertIn("1.0 - exp(-odl * _BraxMgielkaGestosc)", SHADER)
        self.assertIn('Shader.SetGlobalFloat("_BraxMgielkaGestosc", .00042f);', GRAF)
        sky = (ROOT / "unity-city/Assets/Editor/Claude/Region/BraxRegionHoryzont.cs").read_text(encoding="utf-8")
        self.assertIn('m.SetFloat("_MgielkaGestosc", .00042f)', sky)

    def test_ring_island_replaced_by_real_far_copy(self):
        self.assertIn('trZD.name.StartsWith("Nasza wyspa (z daleka")', BUILD)


class R1HoryzontZostaje(unittest.TestCase):
    def test_horizon_terrain_never_streamed_out(self):
        # 9.11 18:13 (zdjęcie z tarasu Tower): teren za granicą znikał razem z kwadratami
        self.assertIn('tH.name=="CLAUDE horyzont: teren za granicą miasta, granica, wiadukt dalej"', BUILD)
        self.assertIn("if(!wH.IsChildOf(horyzontZD)){wynH.Add(wH);continue;}", BUILD)
        self.assertIn("GetComponentsInChildren<LODGroup>(true)", BUILD)
