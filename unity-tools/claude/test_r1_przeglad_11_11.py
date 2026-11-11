"""MIASTO R1 — przegląd miasta na gotowo (właściciel 11.11.2026, 19:33–20:11). Kontrole kodu generatorów."""
import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
R = ROOT / "unity-city" / "Assets" / "Editor" / "Claude" / "Region"
RT = ROOT / "unity-city" / "Assets" / "Claude" / "Runtime"


def src(p):
    return p.read_text(encoding="utf-8")


class Windy(unittest.TestCase):
    def test_tower_lifts_removed_from_shops(self):
        # 19:33 „w sklepach, w piekarniach windy są powstawiane”; 20:08 „wszystkie te windy, które wchodzą do sklepu… wypierdalają”
        u = src(R / "BraxUlicePierzeje.cs")
        self.assertIn("ZdejmijWindyWiezowcow();", u)
        for bid in ["apothecary", "corner_store", "restaurant", "hotel", "estate_corner", "gallery", "apartments_1", "estate_apartments_2"]:
            self.assertIn('"' + bid + '"', u[u.index("WindyWLokalach ="):u.index("static void ZdejmijWindyWiezowcow")])
        self.assertIn('t.name.StartsWith("REAL SKYSCRAPER ELEVATOR ")', u)
        self.assertIn("swiat.additionalTowerElevators = swiat.additionalTowerElevators.Where(e => e).ToArray();", u)
        self.assertNotIn('"quay_corner", "', u[u.index("WindyWLokalach ="):u.index("static void ZdejmijWindyWiezowcow")])


class Ruch(unittest.TestCase):
    def test_nova_effects_measure_from_hall_not_world_origin(self):
        # 19:35 „światełka w klubie Nowa… stoją jak martwe”: korzeń klubu w (0,0,0), sala ~66 m dalej → efekty nigdy się nie włączały
        ef = src(RT / "BraxNovaEfekty.cs")
        self.assertIn("var c = maSrodekSali ? srodekSali : transform.position;", ef)
        nova = src(R / "BraxMiastoNova.cs")
        self.assertIn("ef.srodekSali = new Vector3((HX0 + HX1) * .5f, 1.5f, (HZ0 + HZ1) * .5f); ef.maSrodekSali = true;", nova)
        self.assertIn("\n        ef.lasery = lasery.ToArray(); ef.glowice = glowice.ToArray(); ef.migacze = migacze.ToArray();", nova)

    def test_nova_room_darker(self):
        nova = src(R / "BraxMiastoNova.cs")
        self.assertIn('"NOVA światło parkietu " + (i + 1), p, 1.0f, 9f)', nova)
        self.assertIn("l.intensity = (.55f + .65f * puls) * aktywny;", src(RT / "BraxNovaEfekty.cs"))

    def test_rotisseries_rotate_from_player_distance(self):
        ob = src(RT / "BraxObrot.cs")
        self.assertIn("FindAnyObjectByType<BraxGymPlayer>()", ob)
        self.assertIn("public float stopniNaSekunde = 30f;", ob)


class Ulica(unittest.TestCase):
    def test_trees_only_on_lawns_no_tree_grates(self):
        # 19:40 „Na chodnikach nie ma nic być”; 20:05 „kwadratowe zielone… w środku trawa”
        b = src(R / "BraxRegionMiastoB.cs")
        d = src(R / "BraxRegionMiastoBDzielnice.cs")
        self.assertIn("if (NaTrawniku(q)) DrzewoTeraz(q); else drzewaZChodnika++;", b)
        self.assertIn("trawniki.Contains(top)", b)
        for kod in (b, d):
            self.assertNotIn("BraxRegionMeble.KrataDrzewa(root, p); DrzewoTeraz(p);", kod)


class Lokale(unittest.TestCase):
    def test_premium_doors_three_models(self):
        # 19:33–19:45 „drzwi straszne… z przerwami… konkretne… klamki”; 20:28 „chociaż trzy modele… mogą być nie szklane”
        d = src(R / "BraxDrzwiLokalu.cs")
        for s in ["public const int Szklane = 0, Drewniane = 1, Stalowe = 2, Przesuwne = 3;", "AddComponent<BraxDrzwiPrzesuwne>()", "AddComponent<BraxCityDoor>()", "d.sides = new[] { 1f, -1f };",
                  "KLAMKA: szyld", "zawiasy (3 na skrzydło)", "z.localPosition = new Vector3(hx, 0f, -.1f);", "oak_veneer_01"]:
            self.assertIn(s, d)
        u = src(R / "BraxUlicePierzeje.cs")
        self.assertIn("BraxDrzwiLokalu.UsunStare(id);", u)
        self.assertIn("BraxDrzwiLokalu.ModelDla(lok.typ)", u)
        self.assertIn('|| n.StartsWith("Drzwi lokalu:", System.StringComparison.Ordinal)', src(R / "BraxMiastoUlica.cs"))

    def test_tenement_shops_real_interiors(self):
        # FRYZJER z książkami, KEBAB z półkami — w kamienicach miasta B był tylko obrazek pokoju za szybą
        b = src(R / "BraxRegionMiastoB.cs")
        self.assertIn('"FRYZJER" => "fryzjer", "KEBAB" => "bistro"', b)
        k = src(R / "BraxRegionKamienica.cs")
        self.assertIn("B(M[realny ? BraxUliceKit.SzkloPrzezroczyste : st.witryna]", k)
        self.assertIn("BraxUlicePierzeje.WnetrzeKamienicy(go,", k)
        self.assertIn('Kol("bryły nad lokalem"', k)
        u = src(R / "BraxUlicePierzeje.cs")
        self.assertIn('if (t == "fryzjer")', u)
        self.assertIn("BraxCeramika.FotelFryzjerski(", u)
        self.assertIn("UlSiatka.ZaokraglenieBox = .008f;", u)
        self.assertIn("public static float ZaokraglenieBox = 0f;", src(R / "BraxUliceKit.cs"))


class Narzedzia(unittest.TestCase):
    def test_compile_check_tool_present(self):
        k = src(ROOT / "unity-tools" / "claude" / "kontrola_kompilacji" / "kompiluj.sh")
        self.assertIn("unityengine.modules 2021.3.33", k)
        self.assertIn("WZORCE_UNITY6=", k)


if __name__ == "__main__":
    unittest.main()
