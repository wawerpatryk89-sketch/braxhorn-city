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


class Lazienki(unittest.TestCase):
    def test_wall_hung_toilet_and_running_tap(self):
        # 20:00 „woda w kranie leci… wszystko funkcjonalne”; miska WC z kodu zamiast dwóch poduszek
        c = src(R / "BraxCeramika.cs")
        for s in ["public static void MiskaWiszaca(", "public static void Bateria(", "AddComponent<BraxKran>()", "deska i klapa WC"]:
            self.assertIn(s, c)
        k = src(RT / "BraxKran.cs")
        self.assertIn("strumien.enabled = b;", k)
        self.assertIn("FindAnyObjectByType<BraxGymPlayer>()", k)
        w = src(R / "BraxMiastoDworzecWindyWC.cs")
        self.assertIn("BraxCeramika.MiskaWiszaca(new Vector3(WcX1, Y0, mz), Vector3.forward, Vector3.left);", w)
        self.assertIn("BraxCeramika.Bateria(root,", w)


class LazienkiMiasta(unittest.TestCase):
    def test_city_bathrooms_use_shared_ceramics(self):
        # 19:59 „Proszę sprawdzić całe miasto… toalety… żeby były ceramiczne… wystarczy to poprzynosić”
        nova = src(R / "BraxMiastoNova.cs")
        self.assertIn("BraxCeramika.MiskaWiszaca(new Vector3(c.x, F, z0), Vector3.right, Vector3.forward);", nova)
        self.assertIn("BraxCeramika.Pisuar(new Vector3(x0, F, uz), Vector3.forward, Vector3.right);", nova)
        self.assertIn("BraxCeramika.Umywalka(new Vector3(x1 - .3f * K, F + .88f * K, uz));", nova)
        sport = src(R / "BraxMiastoSport.cs")
        self.assertIn("BraxCeramika.MiskaWiszaca(new Vector3(c.x, F, z1), Vector3.right, Vector3.back);", sport)
        self.assertIn('BraxCeramika.Lustro(root, "szatnia"', sport)
        flat = src(R / "BraxRegionBraxFlat.cs")
        self.assertIn("BraxCeramika.MiskaWiszaca(new Vector3(wcx, y, lz1), Vector3.right, Vector3.back);", flat)
        self.assertIn('BraxUliceKit.Zapisz(A, "mieszkanie_ceramika");', flat)
        tyly = src(R / "BraxRegionTylyB.cs")
        self.assertIn("BraxCeramika.ZestawUmywalkowy(F,", tyly)
        self.assertIn("BraxCeramika.ZestawUmywalkowy(A,", tyly)
        self.assertNotIn("wc.Cyl(cm, cm + Vector3.up * .4f * K", tyly)


class Chlodnie(unittest.TestCase):
    def test_code_fridges_replace_owner_models(self):
        # 19:44 „mięsa są na półkach. A mają być w lodówkach… lodówki szklane… w kodzie”; 20:53 „te lodówki z napojami… puste,
        # dziwne, te modele 3D… lepiej jak ty stworzysz taką lodówkę z napojami”
        c = src(R / "BraxChlodnie.cs")
        for s in ["public static Transform LodowkaNapojow(", "public static Transform SzafaMiesna(", "public static Transform LadaChlodnicza(",
                  '"ZIMNE NAPOJE"', "ProfilPet(", "ProfilPuszka(", "ProfilSzklo(", "BraxSklepyTowar.Polka(typ[k], towar", '"ch_dol"',
                  "Szyba(p[szklo]", "w.siatki.All(m => m)", "BraxLiteryBebas.Napis(p[napisM], p[czarny]"]:
            self.assertIn(s, c)
        d = src(R / "BraxRegionFawelaDomy.cs")
        self.assertNotIn('"miasto_sklepy", "lodowka"', d)
        for s in ["BraxChlodnie.LodowkaNapojow(", "BraxChlodnie.SzafaMiesna(", "BraxChlodnie.LadaChlodnicza(", "int nPol = kebab || pizza || mieso ? 0"]:
            self.assertIn(s, d)
        self.assertNotIn('if (mieso) BraxSklepyTowar.Polka("ch_dol", tw, F.P(l0', d)   # mięso już nie leży na ladzie
        sm = src(R / "BraxSupermarket.cs")
        self.assertNotIn('"miasto_sklepy", "lodowka"', sm)
        self.assertIn("BraxChlodnie.LodowkaNapojow(wyp,", sm)
        self.assertIn("BraxChlodnie.LodowkaNapojow(g,", src(R / "BraxMiastoFitness.cs"))
        self.assertIn("BraxChlodnie.LodowkaNapojow(root,", src(R / "BraxMiastoWyspaDomki.cs"))
        self.assertIn('"chlodnie"', src(ROOT / "unity-tools" / "claude" / "kontrola_geometrii" / "Program.cs"))

    def test_drinks_have_named_labels(self):
        # 21:27 „te napoje też dobrze stworzyłeś… tylko musiałbyś nazwy jakieś im dać… nazwać jakoś fajnie” — marki z gry
        c = src(R / "BraxChlodnie.cs")
        for s in ["static Material AtlasEtykiet()", "PasEtykiety(p[etykiety]", "BraxLiteryBebas.Trojkaty(tekst", '"/etykiety_napojow.png"',
                  'E("BRAXHORN", "COLA"', 'E("WAWER", "WODA"', 'E("SOBCZAK", "ORANŻADA"', 'E("WAWER", "LAGER"']:
            self.assertIn(s, c)
        self.assertEqual(c.count('        E("'), 16)   # 16 komórek atlasu 4 × 4


class Optyk(unittest.TestCase):
    def test_glasses_wall_from_code(self):
        # 20:05 „skala… okulary u optyka za małe”: ściana oprawek z kodu (skala × K) zamiast małych modeli na półkach
        u = src(R / "BraxUlicePierzeje.cs")
        for s in ["internal static void Okulary(Vector3 c, Vector3 przod, Color32 kolor, float k, int model = 0, int przyc = 0)", "optyk: plecy ekspozycji (podświetlone)",
                  "lok.typ == \"optyk\" ? 8 : 5", "Okulary(W(ladaS0 + .3f + i * .3f * k", "LosujOkulary(rng, sl,",
                  "// 4 pilotki", "// 5 kocie oko", "// 6 motylkowe", "// 7 sportowe owijane", "// 8 kwadratowe duże", "okulary: szkła przeciwsłoneczne (przyciemniane)"]:
            self.assertIn(s, u)
        t = src(R / "BraxSklepyTowar.cs")
        self.assertNotIn("round_spectacles", t[t.index('"optyk" =>'):t.index("\n", t.index('"optyk" =>'))])


class Telewizory(unittest.TestCase):
    def test_tvs_play_owner_films(self):
        # 20:56 „jak jest telewizor, to w telewizorach lecą filmiki moje”; 21:40 „Twój kolega zrobił telewizory, które grają…
        # tak samo możesz z nich skorzystać, żeby w każdym pomieszczeniu były”
        tv = src(R / "BraxTelewizory.cs")
        for s in ["public static Transform NaScianie(", "public static Transform NaSzafce(", '"R1: ekran TV (filmy właściciela)"', "AddComponent<BraxR1ReklamyTV>()",
                  '"reklama_1_puma.mp4"', "sK.Box(new Vector3(-w * .5f - bz + .003f"]:
            self.assertIn(s, tv)
        rt = src(RT / "BraxR1ReklamyTV.cs")
        for s in ["VideoAudioOutputMode.None", "FindAnyObjectByType<BraxGymPlayer>()", 'katalog = "R1/Reklamy"', "Camera.main" if False else "zasieg"]:
            self.assertIn(s, rt)
        self.assertNotIn("Camera.main", rt)
        # 21:48 „na ulicach żadnych filmów… zakazane globalnie… W mieszkaniach, w hotelach, w apartamentach”
        u = src(R / "BraxUlicePierzeje.cs")
        self.assertIn('if (t == "hotel")\n            BraxTelewizory.NaScianie(rodzic, f.lok.szyld', u)
        self.assertEqual(u.count("BraxTelewizory."), 1)
        self.assertNotIn("BraxTelewizory.", src(R / "BraxRegionFawelaDomy.cs"))
        self.assertIn('BraxTelewizory.NaScianie(A, "mieszkanie Braxa', src(R / "BraxRegionBraxFlat.cs"))
        self.assertIn("BraxTelewizory.NaSzafce(A, \"apartament Tower", src(R / "BraxRegionTylyB.cs"))
        wf = src(ROOT / ".github" / "workflows" / "claude-region-build.yml")
        self.assertEqual(wf.count("python3 unity-tools/claude/r1_reklamy_tv.py"), 3)
        self.assertTrue((ROOT / "unity-city" / "Prywatne" / "mistrzowie-reklamy-20261111" / "manifest.json").exists())
        ins = src(ROOT / "unity-tools" / "claude" / "r1_reklamy_tv.py")
        self.assertIn('"-pass", "env:KLUCZ"', ins)                       # hasło tylko przez zmienną środowiskową
        self.assertIn("*.mp4", src(ROOT / "unity-city" / "Assets" / "StreamingAssets" / "R1" / "Reklamy" / ".gitignore"))


class Drogowskaz(unittest.TestCase):
    def test_tunnel_sign_premium_3d(self):
        # lista ze zdjęć pkt 1: płaski napis drogowskazu do tunelu → tablica E-2 z kodu, obwódka i strzałka 3D, litery Bebas
        b = src(R / "BraxRegionMiastoB.cs")
        self.assertIn('BraxChlodnie.Wez("znak_E2_osiedle_tunel"', b)
        self.assertIn('BraxLiteryBebas.Napis(q[bialyZ], q[bialyZ], "OSIEDLE"', b)
        self.assertNotIn('BraxRegionMeble.Napis(g, "OSIEDLE  ↓  TUNEL"', b)


class NapisyR1(unittest.TestCase):
    def test_signs_are_3d_not_ghost_textmesh(self):
        # zdjęcie właściciela: „FRYZJER — napis-duch na szybie”; zasada SZYLDY 3D — BraxRegionMeble.Napis = litery Bebas 3D
        m = src(R / "BraxRegionMeble.cs")
        blok = m[m.index("public static void Napis(Transform parent"):m.index("\n    }", m.index("public static void Napis(Transform parent"))]
        self.assertIn("BraxLiteryBebas.Napis(sl, sb, tekst", blok)
        self.assertNotIn("BraxCityArchitecture.Sign(", blok)


class Muzeum(unittest.TestCase):
    def test_museum_in_historic_building(self):
        # 20:06 „ten zabytkowy budynek zrobił funkcjonalny… można było wejść, jakieś takie muzeum… Poly Haven pod to”
        m = src(R / "BraxMuzeum.cs")
        for s in ["static void Muzeum()", 'q.name == "Budynek KINNAIRD"', "if (otwor.Contains(c)) { zmiana = true;", "BraxDrzwiLokalu.Drewniane",
                  '"MUZEUM BRAXHORN"', "static void Gablota(", '"marble_bust_01"', '"horse_statue_01"', "KOTWICA PORTOWA", 'Kol("bryła nad salą"']:
            self.assertIn(s, m)
        self.assertIn("Krok(\"2. budynki: MUZEUM BRAXHORN w zabytku KINNAIRD", src(R / "BraxRegionMiastoB.cs"))
        self.assertIn('"marble_bust_01", "horse_statue_01"', src(ROOT / "unity-tools" / "claude" / "fetch_region.py"))

    def test_door_flush_with_facade_and_portal(self):
        # 23:09 „Muzeum Miasta drzwi zjebałeś… one są w środku, jakoś dziwnie są” — drzwi w licu, portal, kinkiety (nie latarnie)
        m = src(R / "BraxMuzeum.cs")
        self.assertIn('new Vector3(b.x, y0 + .04f, zf + .07f), Vector3.forward, dw', m)
        self.assertNotIn("zf - .35f", m)
        self.assertNotIn("BraxRegionMeble.Latarnia(", m)
        for s in ["// trzon", "// cokół", "// kapitel", "// fryz", "// gzyms", "BraxMuzeumKod.Kinkiet(g, \"portal muzeum\""]:
            self.assertIn(s, m)
        k = src(R / "BraxMuzeumKod.cs")
        self.assertIn("public static Transform Kinkiet(", k)
        self.assertNotIn("mosiądz", k.split("internal static class")[1])

    def test_name_and_3d_lettering(self):
        # 12.11 00:04 „Muzeum Braxhorn, nie miasta”; 00:05 „napis zawsze w 3D… nie napis zwykły w ścianie”
        m, k = src(R / "BraxMuzeum.cs"), src(R / "BraxMuzeumKod.cs")
        for f in ["BraxMuzeum.cs", "BraxMuzeumKod.cs", "BraxRegionMiastoB.cs"]:
            self.assertNotIn("MUZEUM MIASTA", src(R / f))
        self.assertIn('BraxRegionMeble.Napis(g, "MUZEUM BRAXHORN", new Vector3(b.x, yN + .02f, zf + .3f), Vector3.forward, capP / .72f, new Color(.8f, .81f, .83f), .08f);', m)
        self.assertIn('"muzeum: pas szyldu (grafit)"', m)
        self.assertIn('"muzeum: linia LED pod literami (ciepła biel)"', m)
        self.assertIn("float glebokosc = 0f)", src(R / "BraxRegionMeble.cs"))
        self.assertNotIn("TekstAtlasu", k)                      # zero tekstu w teksturach plansz — litery 3D
        self.assertIn("BraxRegionMeble.Napis(h, t, Pkt(", k)

    def test_museum_exhibits_from_code(self):
        # 23:09 „Je wyposażenie. Zrób muzeum.” — eksponaty z kodu, bez pustych gablot, bez złota/mosiądzu
        m, k = src(R / "BraxMuzeum.cs"), src(R / "BraxMuzeumKod.cs")
        for f in ["Zaglowiec(", "Armata(", "KoloSterowe(", "HelmNurka(", "Gramofon(", "Bicykl(", "Globus(", "Obraz(", "Plansza(", "Kinkiet("]:
            self.assertIn("public static Transform " + f, k)
            self.assertIn("BraxMuzeumKod." + f, m)
        self.assertIn("if (!model && kod == null) return;", m)
        self.assertNotIn("brass_vase", m)
        self.assertNotIn("mosiądz", k.split("internal static class")[1])
        self.assertIn("LiczbaObrazow", k)

    def test_museum_filled_whole_hall(self):
        # 12.11 00:31 „wyposażenie musi zrobić większe, bo tam nic nie ma” — hol, PORT, TECHNIKA, SZTUKA zapełnione z kodu
        m, k = src(R / "BraxMuzeum.cs"), src(R / "BraxMuzeumKod.cs")
        for f in ["Lodz(", "Ladunek(", "Dzwon(", "Luneta(", "Kamera(", "MaszynaParowa(", "Rzezba(", "Amfory(", "LampyNaftowe(", "Szatnia(", "Sklepik("]:
            self.assertIn("public static Transform " + f, k)
            self.assertIn("BraxMuzeumKod." + f, m)
        self.assertIn('Vector3.forward, 1); muzEksponatow++;', m)          # bryg
        self.assertIn('Vector3.forward, 2); muzEksponatow++;', m)          # kuter
        self.assertNotIn("Mathf.Pow(Mathf.Cos(a)", k)                      # bez NaN w kadłubach (cos < 0 na burcie)
        self.assertIn("wierzchołki NaN", src(ROOT / "unity-tools" / "claude" / "kontrola_geometrii" / "Program.cs"))

    def test_original_books_in_museum_shop(self):
        # 12.11 00:53 „oryginalne książki mamy w bibliotece… dołączyłeś oryginalne, czy te podróby twoje?” — paczka miasto_sklepy
        k = src(R / "BraxMuzeumKod.cs")
        self.assertIn('BraxSklepyTowar.Ma("ksiegarnia")', k)
        self.assertIn('BraxSklepyTowar.Polka("ksiegarnia"', k)
        self.assertIn("BraxSklepyTowar.Lada(", k)

    def test_poly_haven_antiques(self):
        # 12.11 00:53 „jak coś możesz ściągnąć z Polyheaven zabytkowego, to ściągnij do tej muzeum” + 01:10 „jeżeli coś się nie nadaje… wyrzucaj”
        m, k = src(R / "BraxMuzeum.cs"), src(R / "BraxMuzeumKod.cs")
        f = src(ROOT / "unity-tools" / "claude" / "fetch_region.py")
        ids = ["dutch_ship_medium", "Lantern_01", "wooden_lantern_01", "gothic_statue", "bronze_whale_statue", "bronze_shark_statue",
               "horse_head", "bull_head", "antique_estoc", "antique_katana_01", "ornate_war_hammer", "kite_shield", "ornate_medieval_mace",
               "ornate_medieval_dagger", "Camera_01", "filmstrip_projector_8mm", "vintage_video_camera", "old_gas_mask", "binoculars",
               "vintage_radio_transceiver", "vintage_telephone_wall_clock", "GothicCabinet_01", "ClassicConsole_01", "mantel_clock_01",
               "ornate_mirror_01", "spinning_wheel_01"]
        for i in ids:
            self.assertIn('"' + i + '"', f, i)
            self.assertIn('"' + i + '"', m, i)
        for wyrzucone in ["cannon_01", "fancy_picture_frame_01", "fancy_picture_frame_02", "Chandelier_02", "vintage_oil_lamp"]:
            self.assertNotIn('"' + wyrzucone + '"', f, wyrzucone)          # szkieletowa armata, złote ramy, mosiężny żyrandol i lampa
            self.assertNotIn('"' + wyrzucone + '"', m, wyrzucone)
        self.assertEqual(f.count('"ArmChair_01"'), 1)
        for f2 in ["GablotaDuza(", "GablotaWysoka("]:
            self.assertIn("public static Transform " + f2, k)
            self.assertIn("BraxMuzeumKod." + f2, m)
        self.assertIn("(hg - .08f) / k", m)                                  # limit wysokości w metrach realnych (bez podwójnego K)
        for zakaz in ["brass", "gold", "pocket_watch"]:                       # zasada 10.11 — bez złota i mosiądzu
            self.assertNotIn(zakaz, m.lower())


class BezMosiadzu(unittest.TestCase):
    def test_no_brass_or_gold_materials_in_r1(self):
        # zasada 10.11 („całkowity brak złotego koloru… okucia ze stali”), 12.11 01:10 „jeżeli coś się nie nadaje… wyrzucaj”
        import re
        for f in sorted(R.glob("*.cs")):
            s = f.read_text(encoding="utf-8")
            for m in re.finditer(r'Mat\("[^"]*(mosiądz|złot)[^"]*"|Fitting\("[^"]*(mosiądz|złot)[^"]*"|Surface\("[^"]*(mosiądz|złot)[^"]*"', s):
                self.fail(f.name + ": " + m.group(0))
            self.assertNotIn("brass_vase", s, f.name)
            self.assertNotIn("Color32(196, 160, 70", s, f.name)


class Narzedzia(unittest.TestCase):
    def test_compile_check_tool_present(self):
        k = src(ROOT / "unity-tools" / "claude" / "kontrola_kompilacji" / "kompiluj.sh")
        self.assertIn("unityengine.modules 2021.3.33", k)
        self.assertIn("WZORCE_UNITY6=", k)


if __name__ == "__main__":
    unittest.main()
