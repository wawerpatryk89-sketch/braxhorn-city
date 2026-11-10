"""MIASTO R1 — fawela na wzgórzu za miastem, etap 1 (właściciel 9.11 21:40–22:56: „slamsy… z delikatnym przejściem”, „to nie ma być
teren mały”, „sala kickboxingu z siłownią — teren innego rywala”, „sklepy, apteki… wykończenie teksturą starszy typ”, „na razie
bez nazw”, „każdy detal… nic sztuczne”, „z Poly Haven wykończenia, tekstury wszędzie”, „graffiti… żeby klimat był”).
Źródło i geometria terenu liczona w Pythonie (bez Unity i APK)."""
import math
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
R = ROOT / "unity-city/Assets/Editor/Claude/Region"


def src(p):
    return re.sub(r'//[^\n]*', "", p.read_text(encoding="utf-8"))


FAW = src(R / "BraxRegionFawela.cs")
DROGI = src(R / "BraxRegionFawelaDrogi.cs")
DOMY = src(R / "BraxRegionFawelaDomy.cs")
REGION = src(R / "BraxRegion.cs")
HORYZONT = src(R / "BraxRegionHoryzont.cs")
MIASTOB = src(R / "BraxRegionMiastoB.cs")
RUCH = src(R / "BraxRegionRuchMiasta.cs")
GRANICE = src(ROOT / "unity-city/Assets/Claude/Runtime/BraxMiastoGranice.cs")
FETCH_R = (ROOT / "unity-tools/claude/fetch_region.py").read_text(encoding="utf-8")
FETCH_P = (ROOT / "unity-tools/claude/fetch_polyhaven.py").read_text(encoding="utf-8")

Z0, YC, P1, P2 = -85.45, .15, 45.0, 105.0


def s01(a, b, v):
    t = min(1.0, max(0.0, (v - a) / (b - a)))
    return t * t * (3 - 2 * t)


def gleb(x, z):
    da = math.hypot(max(0.0, x + 61.4), max(0.0, Z0 - z))
    db = math.hypot(max(0.0, -61.4 - x), max(0.0, -68 - z))
    return min(da, db)


def baza_bez_szumu(x, z):
    d = gleb(x, z)
    h = YC + .045 * min(d, P1) + .12 * min(max(d - P1, 0), P2 - P1) + .19 * max(0, d - P2)
    h = YC + (h - YC) * (1 - .85 * s01(20, 70, x))
    ax, dz = abs(x + 126), Z0 - z
    if ax < 40 and dz >= 0:
        przy = 1 - s01(22, 24, ax)
        w = (1 - s01(22, 40, ax)) * (s01(0, 18, dz) + (1 - s01(0, 18, dz)) * przy) * (1 - s01(62, 75, dz))
        garb = 13.65 - 3.4 * s01(0, 60, dz)
        h = max(h, h + (garb - h) * w)
    return h


class Kolejnosc(unittest.TestCase):
    def test_step_after_city_b_and_before_holes_traffic_horizon(self):
        i_b = REGION.index("BraxRegionMiastoB.OdbiorWyposazeniaUlic(root)")
        i_f = REGION.index("BraxRegionFawela.Apply(root)")
        self.assertLess(i_b, i_f)
        for dalej in ("BraxSilowniaZewnatrz.Apply(root)", "BraxRegionDziury.Apply(root)", "BraxClaudeOsiedle.RuchMiastaB(root)", "BraxRegionHoryzont.Apply(world, root)"):
            self.assertLess(i_f, REGION.index(dalej), dalej)

    def test_horizon_continues_favela_terrain(self):
        self.assertIn("BraxRegionFawela.WysTerenu(pfw.x, pfw.y)", HORYZONT)
        self.assertIn("public static float WysTerenu(float x, float z)", FAW)
        self.assertIn("public static bool WFaweli(float x, float z)", GRANICE)
        self.assertIn("if (WFaweli(x, z)) return true;", GRANICE)

    def test_old_hill_replaced_and_lawn_corridor_cut(self):
        self.assertIn("if (BraxRegionFawela.Wlaczona) return;", MIASTOB)
        self.assertIn('TrawaBez("L9 za warsztatami (do skarpy)", Rect.MinMaxRect(-112f, ZboczeZ, -61.4f, ParkZ0), true, korytarzFaweli);', MIASTOB)
        self.assertIn('TrawaBez("L9 na wschód od parkingu", Rect.MinMaxRect(ParkX1, ParkZ0, -61.4f, ParkZ1), true, korytarzFaweli);', MIASTOB)
        # korytarz = jezdnia 5.5 m + krawężniki 2 × 0.25 m wokół osi x −66.5
        self.assertIn("Rect.MinMaxRect(-69.6f, -82.5f, -63.4f, -59.1f)", MIASTOB)
        self.assertGreaterEqual(-63.4 - (-66.5), 5.5 / 2 + .25)


class Teren(unittest.TestCase):
    def test_constants_match_source(self):
        self.assertIn("public const float Pas1 = 45f, Pas2 = 105f;", FAW)
        self.assertIn("float h = YC + .045f * Mathf.Min(d, Pas1) + .12f * Mathf.Clamp(d - Pas1, 0f, Pas2 - Pas1) + .19f * Mathf.Max(0f, d - Pas2);", FAW)
        self.assertIn("* S01(40f, 160f, d);", FAW)     # rzeźba zaczyna się 40 m od miasta — przy mieście sam stok

    def test_city_edge_level_and_no_step_at_park_corner(self):
        for x in range(-470, -62, 3):
            if abs(x + 126) < 24:
                continue                                  # za murem portalu wzgórze 13.65 m (mur miasta B do 13.5 m)
            self.assertAlmostEqual(baza_bez_szumu(x, Z0), YC, places=5)
        for x in range(-61, 20, 3):
            self.assertAlmostEqual(baza_bez_szumu(x, -68), YC, places=5)
        for z in [-68.5 - k for k in range(0, 17)]:       # bok trawnika L9 (x −61.4): teren faweli zaczyna od poziomu miasta
            self.assertAlmostEqual(baza_bez_szumu(-61.4, z), YC, places=5)
            self.assertLess(abs(baza_bez_szumu(-61.0, z) - baza_bez_szumu(-61.8 + .8, z)), .05)

    def test_gentle_transition_three_belts(self):
        self.assertLess(baza_bez_szumu(-300, Z0 - 44) - YC, 2.1)          # pas 1: do 2 m na 45 m
        self.assertLess(baza_bez_szumu(-300, Z0 - 104), 9.5)
        self.assertGreater(baza_bez_szumu(-300, -350), 35)                # szczyt

    def test_hill_covers_tunnel_tube(self):
        for x in (-132, -126, -120):
            for z in range(-146, -86, 4):
                self.assertGreater(baza_bez_szumu(x, z), 7.6 + .8, (x, z))   # strop rury 7.6 m
        self.assertGreaterEqual(baza_bez_szumu(-126, Z0), 13.6)             # gzyms portalu 13.5 m

    def test_roads_cut_terrain_below_asphalt(self):
        self.assertIn("Mathf.Lerp(yd - .25f, yd + .08f, S01(.15f, 1.8f, rel))", FAW)
        self.assertIn("const float spadek = .14f;", FAW)
        self.assertIn("h = Mathf.Min(h, q.yF - .3f);", FAW)


class Plan(unittest.TestCase):
    def test_big_area_and_lots_inside_stream_cells(self):
        self.assertIn("FawelaX0 = -470f, FawelaX1 = 70f, FawelaZSzczyt = -357f", GRANICE)
        self.assertIn("Mathf.FloorToInt((x0 - 2.4f) / Kw) != Mathf.FloorToInt((x1 + 2.4f) / Kw)", FAW)
        self.assertIn("internal const float YC = .15f, Kw = 150f;", FAW)

    def test_shops_cover_what_is_needed_without_names(self):
        blok = FAW[FAW.index("RodzajeLokali ="):FAW.index("};", FAW.index("RodzajeLokali ="))]
        lokale = re.findall(r'"([^"]+)"', blok)
        self.assertEqual(len(lokale), 19)
        for potrzebne in ("SKLEP SPOŻYWCZY", "APTEKA", "PIEKARNIA", "BAR", "WARZYWA · OWOCE", "FRYZJER", "MIĘSO · WĘDLINY", "PRZYCHODNIA", "PRALNIA", "WULKANIZACJA"):
            self.assertIn(potrzebne, lokale)
        self.assertGreaterEqual(lokale.count("APTEKA"), 2)
        self.assertIn('"SALA KICKBOXINGU"', DOMY)            # bez nazwy rywala — właściciel nada

    def test_signs_are_3d_bodies(self):
        # tablica-bryła 0.30 m od nad markizą do stropu, litery przestrzenne (krój miasta), nigdy płaski napis nad paskiem
        self.assertIn("BoxF(W(cx, cz, metal, new Color(.30f, .30f, .32f)), F, sx0, sx1, sy0, sy1, -.3f, 0f);", DOMY)
        self.assertIn("float sy0 = yo + .2f, sy1 = yF + hf + .05f", DOMY)
        self.assertIn("BraxUliceKit.Napis3D(rodzaj, F.P((sx0 + sx1) * .5f, sy0 + .03f + (hTab - cap) * .5f, -.305f), F.N, cap, .04f, LiteryFarba(kolLit), metal);", DOMY)
        self.assertNotIn("BraxRegionMeble.Napis(rd, rodzaj,", DOMY)
        self.assertIn("internal static float WysWitryny => 2.1f * K;", DOMY)
        self.assertIn('BraxUliceKit.Zapisz(Grupa("Fawela: litery szyldów (przestrzenne)"), "fawela_szyldy")', FAW)

    def test_clerks_stand_on_given_floor(self):
        self.assertIn("foreach (var (pf, yawF, gdzieF) in BraxRegionFawela.Sprzedawcy)", RUCH)
        self.assertIn("pf, yawF, rnd, lada, pf.y + .02f)", RUCH)
        self.assertIn("Sprzedawcy.Add((F.P((l0 + l1) * .5f, yF, ld + 1.35f), F.Yaw,", DOMY)

    def test_floor_not_coplanar_with_foundation(self):
        self.assertIn("new Vector3(x1 + .06f, yF - .03f, z1 + .06f)", DOMY)
        self.assertIn("pos.Quad(F.P(G, yF, 0f), F.P(L - G, yF, 0f), F.P(L - G, yF, D), F.P(G, yF, D));", DOMY)


class Wykonczenia(unittest.TestCase):
    def test_every_finish_is_a_photoscan(self):
        # jedyne powierzchnie bez fotoskanu: szkło okien, lustra i źródła światła
        self.assertEqual(len(re.findall(r"BraxCityMaterials\.Surface\(", FAW + DROGI + DOMY)), 2)
        self.assertIn('szyba = BraxCityMaterials.Surface(', FAW)
        self.assertIn('lustro = BraxCityMaterials.Surface(', FAW)
        self.assertIn("DO POPRAWKI brak fotoskanów", FAW)

    def test_first_choice_textures_are_fetched(self):
        fawela = set(re.findall(r'"([a-z0-9_]+)"', FETCH_R[FETCH_R.index("TEXTURES_FAWELA_1K = ["):FETCH_R.index("]", FETCH_R.index("TEXTURES_FAWELA_1K = ["))]))
        wspolne = set(re.findall(r'"([A-Za-z0-9_]+)"', FETCH_R[FETCH_R.index("TEXTURES = ["):FETCH_R.index("]", FETCH_R.index("TEXTURES = ["))]))
        wspolne |= set(re.findall(r'"([a-z0-9_]+)"', FETCH_P[FETCH_P.index("TEXTURES = ["):FETCH_P.index("]", FETCH_P.index("TEXTURES = ["))]))
        self.assertGreaterEqual(len(fawela), 30)
        self.assertFalse(fawela & wspolne, "zestaw faweli nie może dublować wspólnego (wspólnych nie wolno zmniejszać)")
        pierwsze = re.findall(r'\bTm?\("[^"]+", [\d.]+f, [^"]*?, [\d.]+f, "([a-z0-9_]+)"', FAW + DROGI)
        self.assertGreaterEqual(len(pierwsze), 40)
        for slug in pierwsze:
            self.assertIn(slug, fawela | wspolne, slug)
        blok = FAW[FAW.index("TylkoFawela = new HashSet<string>"):FAW.index("};", FAW.index("TylkoFawela = new HashSet<string>"))]
        self.assertEqual(set(re.findall(r'"([a-z0-9_]+)"', blok)), fawela)

    def test_phone_memory_one_k_sets(self):
        self.assertIn('texture(t, report, ("1k", "2k"))', FETCH_R)
        self.assertIn("Fotoskan(nazwa, metry, tint, szorstkosc, 1024, false, ids)", FAW)
        self.assertIn("Fotoskan(nazwa, metry, tint, szorstkosc, 512, true, ids)", FAW)

    def test_graffiti_and_details(self):
        self.assertIn("static Material Graffiti()", DOMY)
        self.assertIn("KomorkaGraffiti(", DROGI)
        for detal in ("Balkon(", "Pranie(", "Przylacza.Add(", "zbiornik", "okiennica", "Towar(", "ModelPH("):
            self.assertIn(detal, DOMY, detal)
        self.assertIn("BraxRegionMiastoB.AutaWarsztatow.Add(", DROGI)


class BibliotekaIRealizm(unittest.TestCase):
    """Właściciel 9.11 23:03–23:08: towar i wyposażenie z biblioteki, zakaz białych napisów, prawdziwe szyby, jakość jak w mieście."""

    def test_goods_come_from_owner_library(self):
        for wywolanie in ("BraxSklepyTowar.Korzen(rd,", "BraxSklepyTowar.Polka(typPolek[p % typPolek.Length], tw,", "BraxSklepyTowar.Lada(typLady, tw,", "BraxSklepyTowar.Zakoncz(tw,"):
            self.assertIn(wywolanie, DOMY, wywolanie)
        towar = src(R / "BraxSklepyTowar.cs")
        blok = DOMY[DOMY.index("string[] typPolek ="):DOMY.index("Transform tw =")]
        for typ in set(re.findall(r'"([a-z_0-9]+)"', blok)):
            self.assertIn('"' + typ + '"', towar, "typ towaru spoza biblioteki: " + typ)
        # kolorowych walców i klocków jako towaru już nie ma (zostają tylko pudełka akcesoriów w naprawie telefonów — biblioteka nie ma elektroniki)
        self.assertNotIn("var ow = Color.HSVToRGB(new[] { .02f, .11f, .16f, .3f }[q]", DOMY)
        self.assertIn("Towar(F, s, y + .035f, D - .36f, r, cx, cz, tel ? 2 : 0)", DOMY)

    def test_equipment_from_library(self):
        for model in ('"miasto_sklepy", "lodowka"', '"miasto_sklepy", "menu_4"', '"miasto_sklepy", "menu_3"', '? "plakat_0" + (7 + q) : "plakat_0" + (1 + (dz.nr + q) % 3)', '"miasto_jedzenie", "wozek_jedzenie"', '"miasto_silownia_metro", "silownia_atlas"'):
            self.assertIn(model, DOMY, model)
        for pomocnik in ("BraxMiastoAssety.Klatka(tr,", "BraxMiastoAssety.Lawka(tr,", '"odz_dol", "odz_polka", "odz_gora"', '"trashbag"', '"wooden_picnic_table"'):
            self.assertIn(pomocnik, DOMY, pomocnik)

    def test_no_white_lettering(self):
        blok = DOMY[DOMY.index("ParySzyldow ="):DOMY.index("};", DOMY.index("ParySzyldow ="))]
        pary = re.findall(r"new Color\(([\d.]+)f, ([\d.]+)f, ([\d.]+)f\)", blok)
        self.assertEqual(len(pary), 12)
        for r_, g_, b_ in pary[1::2]:                       # litery: nasycona farba, nie biel / kość słoniowa
            k = sorted((float(r_), float(g_), float(b_)))
            self.assertGreater(k[2] - k[0], .2)
        self.assertNotIn("new Color(.97f, .95f, .88f)", DOMY)
        self.assertNotIn(".17f * k, new Color(.95f, .93f, .86f)", DOMY)

    def test_real_glass_rooms_on_world_grid(self):
        self.assertIn("BraxUliceKit.OknoPokoj(2f * SiatkaOkien, Hf, i * Hf / 12f, 0, 5, 4.2f, 0f)", FAW)
        self.assertIn("dz.yF = Mathf.Ceil((dz.yFront + .10f) / (Hf / 12f)) * (Hf / 12f);", FAW)
        self.assertIn("Pasek(W(cx, cz, okna[FazaOkna(dz.yF)], Color.white), F, o.xMin, o.xMax, o.yMin, o.yMax, g);", DOMY)
        self.assertIn("internal const float SiatkaOkien = 1.6f;", DOMY)
        # okno 1.1 m × K i drzwi balkonu 1.0 m × K mieszczą się w jednej komórce siatki; pokój = 2 komórki
        self.assertLess(1.1 * 1.294, 1.6)
        hf, q = 2.8 * 1.294, 2.8 * 1.294 / 12
        for y_front in (0.2, 1.33, 7.9, 22.47, 40.1):
            yf = math.ceil((y_front + .10) / q) * q
            self.assertGreaterEqual(yf - y_front, .10 - 1e-6)
            self.assertLess(yf - y_front, .10 + q + 1e-6)     # próg najwyżej 40 cm
            self.assertAlmostEqual((yf / q) - round(yf / q), 0, places=4)

    def test_sidewalk_and_frame_depth(self):
        self.assertIn("BoxF(ch, fF, -.12f, L + .12f, dz.yFront - .7f, dz.yFront + .05f, -(doDrogi - .32f), 0f, false, false);", DOMY)
        self.assertIn("void Rama(float s0, float s1, float y0, float y1) => BoxF(br, F, s0, s1, y0, y1, g - .045f, g, false, false);", DOMY)


ZIELEN = src(R / "BraxRegionFawelaZielen.cs")
SUPERMARKET = src(R / "BraxSupermarket.cs")
LIB = ROOT / "unity-city/Assets/Source/Claude/Library"


class SalaIBiblioteka(unittest.TestCase):
    """Właściciel 9.11 23:18–23:50: stara sala bokserska realnie, worki z biblioteki, wszystko co się przydaje z biblioteki."""

    def test_owner_library_models_in_repo(self):
        for plik in ("leather_punching_bag.glb", "rope_coil.glb", "medicine_ball_rack.glb", "boxing_headgear.glb"):
            self.assertTrue((LIB / plik).exists(), plik)
            self.assertIn(plik[:-4], DOMY, plik)
        self.assertIn("oboz-owner-remodel-20261105", (LIB / "CREDITS.md").read_text(encoding="utf-8"))
        self.assertNotIn("tractor_tire", DOMY)   # prywatny asset obozu — poza Unity

    def test_hall_is_old_boxing_gym(self):
        for czesc in ('new[] { "leather_punching_bag" }', "podlogaSali", "matySali", "void Lamperia(Fr F, float len", "for (float x = s.xMin + 2.25f; x < s.xMax - 1f; x += 4.5f)",
                      '"ceiling_fan"', '"hanging_industrial_lamp"', '"standing_chalkboard_01"', '"wall_clock"', "PlakatWewn(F, s0,", "static Material Plakaty()"):
            self.assertIn(czesc, DOMY, czesc)
        self.assertNotIn("Bieznia(", DOMY)   # stara sala bokserska nie ma bieżni

    def test_hall_roof_open_and_windows_glazed_both_sides(self):
        # 9.11 23:40 (podgląd): pełna płyta wieńca zasłaniała kratownice, a szyby okien widać było tylko z zewnątrz
        self.assertIn("wn.Box(new Vector3(s.xMin - .05f, y + hs, s.yMin - .05f), new Vector3(s.xMax + .05f, y + hs + .3f, s.yMin + G + .02f), true);", DOMY)
        self.assertNotIn("W(cx, cz, betonZbr, Color.white).Box(new Vector3(s.xMin - .05f, y + hs, s.yMin - .05f), new Vector3(s.xMax + .05f, y + hs + .3f, s.yMax + .05f), true);", DOMY)
        self.assertIn("sz.Quad2(F.P(o.xMin, o.yMin, gs), F.P(o.xMax, o.yMin, gs), F.P(o.xMax, o.yMax, gs), F.P(o.xMin, o.yMax, gs));", DOMY)

    def test_exact_scale_and_variant_for_library_models(self):
        self.assertIn("bool dokladnie = false, string czesc = null)", DOMY)
        self.assertIn("o.transform.localScale *= maxH * K / b0.size.y;", DOMY)
        self.assertIn('r.Next(3) == 0 ? "!rusted" : "rusted"', DOMY)   # klimatyzator: jeden wariant z zestawu, nie dwa obok siebie

    def test_fetch_lists_cover_library_models(self):
        for m in ("exterior_aircon_unit", "plastic_monobloc_chair_01", "CashRegister_01", "BarberShopChair_01", "ceiling_fan", "hanging_industrial_lamp",
                  "shrub_02", "shrub_03", "boulder_01", "wall_clock", "korean_fire_extinguisher_01", "multi_cleaner_bottle", "bleach_bottle"):
            self.assertIn('"' + m + '"', FETCH_R, m)
        # podłoga starej sali: stare deski i czarna guma (nowa sosna / czerwona bieżnia wyglądały jak nowa hala — podgląd 10.11)
        for t in ("wood_floor_worn", "rubberized_track"):
            self.assertNotIn('"' + t + '"', FETCH_R, t); self.assertNotIn('"' + t + '"', FAW, t)
        self.assertIn('"old_planks_02", "wood_planks_grey", "wood_planks");', FAW)


class RealizmPoPodgladzie(unittest.TestCase):
    """Poprawki po przeglądzie 25 podglądów (9.11 23:40)."""

    def test_raw_side_walls(self):
        self.assertIn("bool bokiSurowe = tynkowany && r.NextDouble() < (dz.pas == 1 ? .45 : .7)", DOMY)
        self.assertIn("b = bokiSurowe ? BS(i) : BW(i); Sciana(b, fb,", DOMY)

    def test_shop_interior_light_and_fittings(self):
        self.assertIn('BraxRegion.Warm(rd, "Fawela: światło lokalu " + rodzaj', DOMY)
        self.assertIn('new[] { "BarberShopChair_01" }, F.P(G + 1.25f, yF, d + .3f), F.Yaw - 90f', DOMY)   # fotel przodem do lustra
        self.assertNotIn("BoxF(bl, F, L - G - 1.9f, L - G - .2f, yF + .42f * k, yF + .47f * k, .6f, 1.0f, true);", DOMY)   # wisząca deska „ławki”

    def test_wild_greenery_on_slope(self):
        self.assertIn('Krok("zieleń dzika stoku: drzewa, krzewy, kamienie", Zielen);', FAW)
        self.assertIn("const int MaxDrzew = 80, MaxKrzewow = 260, MaxKamieni = 52;", ZIELEN)
        for m in ('"drzewo_verdant"', '"park_shrub"', '"park_rock"'):
            self.assertIn(m, ZIELEN, m)
        self.assertIn("DoDzialek(px, pz) - 1.8f, DoDrog(px, pz, 10f) - 3.2f", ZIELEN)

    def test_terrain_material_line_not_commented_out(self):
        linia = next(l for l in (R / "BraxRegionFawelaDrogi.cs").read_text(encoding="utf-8").splitlines() if '"_KTrawa"' in l)
        self.assertIn('m.SetColor("_KSkala"', linia.split("//")[0])


class SupermarketNaZiemi(unittest.TestCase):
    """Właściciel 9.11 23:47: „nadal to wisi nad historycznym budynkiem” — supermarket nr 2 stał na dachu zabytku (y = 43.6 m)."""

    def test_shop_only_at_street_level(self):
        self.assertIn("const float MaksGrunt = 4f;", SUPERMARKET)
        self.assertIn("BraxRegion.Ground(q.x, q.y, -20f, MaksGrunt, out var h0)", SUPERMARKET)
        self.assertIn("if (!BraxRegion.Ground(p.x, p.z, -20f, MaksGrunt, out var h)) return false;", SUPERMARKET)
        self.assertIn("if (BraxRegion.Ground(p.x, p.z, MaksGrunt, 400f, out _)) return false;", SUPERMARKET)
        pasuje = SUPERMARKET[SUPERMARKET.index("static bool Pasuje("):SUPERMARKET.index("static float WejscieX(")]
        self.assertNotIn("-20f, 80f", pasuje)

    def test_shop_not_on_favela_or_outside_city(self):
        self.assertIn("!BraxMiastoGranice.Grywalny(p.x, p.z) || BraxMiastoGranice.WFaweli(p.x, p.z)", SUPERMARKET)


WIEZE = src(R / "BraxRegionFawelaWieze.cs")
DZIELNICE = src(R / "BraxRegionMiastoBDzielnice.cs")
SHADERY = ROOT / "unity-city/Assets/Claude/Shaders"
LUSTRO_CS = (ROOT / "unity-city/Assets/Claude/Runtime/BraxLustro.cs").read_text(encoding="utf-8")


class WiezowceMurowane(unittest.TestCase):
    """Właściciel 10.11 00:04–00:34: „mogą być takie i takie” (szklane i murowane), wszystkie wysokie, wykończenie CZYSTE jak
    policja / szpital („co to jest, w stylu slamsie” — cegła odrzucona), „tekstura ma być ładna, z Poly Haven”, „okna mają być oknami”."""

    def test_centre_units_replaced_by_code_towers(self):
        self.assertIn('if (BraxRegionFawela.WiezeMurowaneWlaczone && b.paczka == "miasto_centrum")', DZIELNICE)
        self.assertIn("BraxRegionFawela.WiezaMurowana(g, b.id, b.x, Podloze(b.x, b.z, YC), b.z, b.w, b.d);", DZIELNICE)
        self.assertEqual(len(re.findall(r'new Bud\("C5_U\d\d", "miasto_centrum_c5"', DZIELNICE)), 14)
        self.assertIn("public static readonly bool WiezeMurowaneWlaczone = true;", WIEZE)

    def test_mix_of_glass_and_solid_towers(self):
        m = re.search(r"SzklaneNr = new HashSet<int> \{([^}]*)\}", WIEZE)
        szklane = {int(x) for x in m.group(1).split(",")}
        self.assertEqual(len(szklane), 6); self.assertTrue(szklane < set(range(14)))      # 6 szklanych, 8 murowanych
        self.assertIn("bool szklana = SzklaneNr.Contains(nr);", WIEZE)
        self.assertIn("BraxRegionFawela.wiezMurowanych", DZIELNICE); self.assertIn("BraxRegionFawela.wiezSzklanych", DZIELNICE)

    def test_all_towers_are_tall_and_stepped(self):
        # szklane 26–36 kondygnacji, murowane 22–31; kondygnacja 4.4 m → 100–160 m
        self.assertIn("int N = szklana ? (dlugi ? 26 + r.Next(4) : 30 + r.Next(7)) : (dlugi ? 22 + r.Next(4) : 24 + r.Next(8));", WIEZE)
        self.assertIn("new[] { (R0, 0, 2), (R1, 2, n3), (R2, n3, N) } : new[] { (R0, 0, n1), (R1, n1, n2), (R2, n2, n3), (R3, n3, N) };", WIEZE)

    def test_clean_finishes_from_polyhaven_and_no_brick(self):
        for t in ("white_stucco", "marble_01", "sandstone_blocks_08", "exterior_wall_cladding_03", "granite_tile", "granite_tile_03"):
            self.assertIn('"' + t + '"', WIEZE, t); self.assertIn('"' + t + '"', FETCH_R, t)
        self.assertNotIn("brick", WIEZE)                                                   # cegła = „slamsy”
        self.assertEqual(WIEZE.count("new StylWiezy {"), 7)                               # 5 stylów murowanych + 2 podia wież szklanych
        self.assertNotIn("Zaciek", WIEZE)

    def test_windows_are_real_glass_on_world_grid(self):
        self.assertIn("const float WG = 3.6f;", WIEZE)
        self.assertIn("float s = (j * WG - a0) / kier;", WIEZE)
        self.assertIn('BraxSzkloFasady.Mat("wieże — okna " + kl, WG, kondygnacja, floor0,', WIEZE)
        self.assertIn("float ww = 2.5f, wh = 2.7f, par = .8f;", WIEZE)                    # duże okna
        self.assertNotIn("OknoPokoj", WIEZE)

    def test_curtain_wall_variants(self):
        self.assertIn('BraxSzkloFasady.Mat("wieże — ściana kurtynowa " + kl, 1.8f, WHk, floor0, barwy[wariant % 4]', WIEZE)
        self.assertIn("WB(kurtyna).K(Color.white).Quad(F.P(0f, y0, 0f), F.P(L, y0, 0f), F.P(L, y1, 0f), F.P(0f, y1, 0f));", WIEZE)
        self.assertEqual(len(re.findall(r"case \d: U\(|default: U\(", WIEZE)), 4)
        barwy = re.search(r"var barwy = new\[\] \{(.*?)\};", WIEZE).group(1)
        for r_, g_, b_ in re.findall(r"new Color\(([.\d]+)f, ([.\d]+)f, ([.\d]+)f\)", barwy):
            self.assertGreaterEqual(float(b_), float(r_), barwy)                            # żadnego brązowego szkła (z góry wyglądało jak cegła)

    def test_base_crown_and_roof_details(self):
        for czesc in ("PortalWiezy(F, o, WB, st, oknaW, rama, id, si)", "bool namiot = !szklana && !dlugi && (st == stylWiez[1] || st == stylWiez[4]);",
                      "float yKor = ostatni ? Y(f1 - 2) : y1 + att;"):
            self.assertIn(czesc, WIEZE, czesc)

    def test_collider_and_lod(self):
        self.assertIn("col.size = new Vector3(w + 2f * Wys, top - yBase, d + 2f * Wys);", WIEZE)
        self.assertIn("lod.SetLODs(new[] { new LOD(.004f, rend.ToArray()) });", WIEZE)


class LustroPrawdziwe(unittest.TestCase):
    """Właściciel 10.11 00:15: „lustro ma być lustrem, a nie naklejką na ścianie”."""

    def test_runtime_planar_reflection(self):
        for czesc in ("public sealed class BraxLustro : MonoBehaviour", "void OnWillRenderObject()", "kam.worldToCameraMatrix = cam.worldToCameraMatrix * odb;",
                      "cam.CalculateObliqueMatrix(", "GL.invertCulling = !odwr;", "const int MaksNaKlatke = 2;", "finally"):
            self.assertIn(czesc, LUSTRO_CS, czesc)

    def test_shader_shows_reflection_with_probe_fallback(self):
        sh = (SHADERY / "Lustro.shader").read_text(encoding="utf-8")
        self.assertIn('Shader "Braxhorn/Lustro"', sh)
        self.assertIn("tex2D(_OdbicieTex, suv)", sh); self.assertIn("unity_SpecCube0", sh); self.assertIn("saturate(_OdbicieJest)", sh)

    def test_favela_mirrors_use_it(self):
        self.assertEqual(DOMY.count("LustroTafla("), 4)      # definicja + fryzjer stanowiska + fryzjer umywalka + sala
        self.assertIn("g.AddComponent<BraxLustro>();", DOMY)
        self.assertIn('Shader.Find("Braxhorn/Lustro")', FAW)
        self.assertNotIn("W(cx, cz, lustro, Color.white)", DOMY)


class SzkloFasadArchitektura(unittest.TestCase):
    def test_facade_types_and_pane_tilt(self):
        sh = (SHADERY / "SzkloFasady.shader").read_text(encoding="utf-8")
        for czesc in ("_Typ (", "_FilarCo (", "_Tafla (", "float3 nT = normalize(n +", "half pelnyPas = step(.5, _Typ);"):
            self.assertIn(czesc, sh, czesc)
        self.assertEqual(sh.count("{"), sh.count("}"))


class SalaStolarka(unittest.TestCase):
    def test_lockers_clear_of_side_door_and_doors_have_leaves(self):
        self.assertIn("float szaf0 = bd1 + .55f + 4.5f < D - .5f ? bd1 + .55f : bd0 - .55f - 4.4f;", DOMY)
        self.assertIn("BoxF(skrz, fL, bd0 - .045f, bd0, y + .02f, yd - .06f, -(bd1 - bd0 - .06f), .1f, true);", DOMY)
        self.assertIn("float grub = .22f, float podzial = -1f)", DOMY)


class SalaRingIPrzejscia(unittest.TestCase):
    """Właściciel 10.11: 00:27–00:28 „drzwi zastawione”, „ławeczka za krótka”, „tam wejścia nie ma na ring”, „liny mają pracować”;
    00:59 „ring do zmiany… stary, ale realnie wyglądać”, „worki na czym wiszą”; 07:13–07:20 „obić deskami z zewnątrz, starszymi”,
    „u góry normalnie matę”, „narożniki okrągłe”, „na około prawdziwe linki”, „stare, ale realistyczne”."""

    LINA = (ROOT / "unity-city/Assets/Claude/Runtime/BraxLinaSplot.cs").read_text(encoding="utf-8")
    GEOM = (ROOT / "unity-city/Assets/Claude/Runtime/BraxLinaSplotGeom.cs").read_text(encoding="utf-8")

    def test_ring_ropes_are_twisted_and_reactive(self):
        # liny ze splotek (3 splotki śrubowo) — fizyka jak BraxLina (napór, cios, kop); BraxLina z sali osiedla nietknięta
        self.assertIn("var l = go.AddComponent<BraxLinaSplot>();", DOMY)
        self.assertIn("l.Zbuduj();", DOMY)                                   # (komentarze wycięte — wywołanie nie może siedzieć za //)
        self.assertIn("GameObjectUtility.SetStaticEditorFlags(go, 0);", DOMY)
        self.assertEqual(len(re.findall(r'BokLin\(PR\(', DOMY)), 5)
        self.assertIn("for (int j = 0; j < 3; j++)", self.GEOM)              # 3 splotki
        self.assertIn("float x = L * s / odc, th = w * x + fj;", self.GEOM)  # skręt śrubowy
        self.assertIn("BraxGymPlayer.OnCios += Cios;", self.LINA)
        self.assertIn("acc = (cel[i] - u[i]) * 2500f - pr[i] * 60f;", self.LINA)
        self.assertNotIn("rough_linen", FAW[FAW.index("linyRingu = "):FAW.index("linyRingu = ") + 200])   # niebieski len — liny były szare

    def test_ring_platform_planks_mat_round_corners(self):
        self.assertIn('"weathered_brown_planks"', FAW); self.assertIn('"weathered_brown_planks"', FETCH_R)
        self.assertIn("var pd = W(cx, cz, deskiRingu);", DOMY)                # podest obity starymi deskami
        self.assertIn("var mt = W(cx, cz, mataRingu);", DOMY)                 # mata ringowa z płótna na górze
        self.assertIn('mataRingu = T("mata ringu', FAW)
        self.assertIn("pad.Cyl(new Vector3(c0.x, yP0, c0.z), new Vector3(c0.x, yP1, c0.z), rp, 16);", DOMY)   # okrągłe ochraniacze
        self.assertNotIn("leather_red_03", FAW)                               # na Poly Haven bez mapy koloru
        self.assertNotIn("fart.Quad", DOMY); self.assertNotIn("BoxRot(c0 + Vector3.up * .95f * k", DOMY)   # stary czerwony fartuch i płaskie narożniki

    def test_ring_has_gate_where_steps_lead(self):
        self.assertIn("float ql = rs - .02f, wej = .85f,", DOMY)
        self.assertIn('BokLin(PR(-ql, -ql), PR(-ql, -wej), Vector3.left, "W1", rPad, rPadF); BokLin(PR(-ql, wej), PR(-ql, ql), Vector3.left, "W2", rPadF, rPad);', DOMY)
        self.assertIn("float xs0 = rcx - P - .03f, szS = .7f;", DOMY)
        self.assertIn("Kol(tr, new Vector3(xa, y, rcz - szS), new Vector3(xs0, yt, rcz + szS));", DOMY)
        self.assertIn("BraxLinaSplotGeom.Lina(gL, pkt,", DOMY)                 # odpięte liny furtki zwisają

    def test_bags_hang_on_floor_gantry_with_chains(self):
        for czesc in ("var rama = W(cx, cz, metal,", "float yB = y + 3.0f, xRa = s.xMin + 1.2f, xRb = s.xMin + 8.1f;", "rama.Cyl(st, gr, .055f, 4);",
                      "void Lanc(Bud bb, Vector3 gora, Vector3 dol,", "Lanc(stalC, new Vector3(bx, yB - .085f, bz),"):
            self.assertIn(czesc, DOMY, czesc)
        self.assertNotIn("new Vector3(xr, yDol, bz), .025f, 5)", DOMY)          # pręty 2.5 cm do kratownicy (belka „w powietrzu”)
        # rama z dala od drzwi wejściowych: drzwi x ∈ [xMin + L/2 − 3 − 1.7K, xMin + L/2 − 3], pas boczny 0.5 m
        L_, K_ = 28.0, 1.294
        self.assertLess(8.1 + .06, (L_ / 2 - 3 - 1.7 * K_) - .5)

    def test_passages_clear(self):
        self.assertIn("float lx = s.xMax - G - .45f; var lawkiZ = new[] { s.yMin + 1.7f, s.yMin + 4.1f };", DOMY)
        self.assertIn("float bx = s.xMin + 2.35f + q * 2.35f, dol = y + .5f * k;", DOMY)
        self.assertIn('"standing_chalkboard_01" }, new Vector3(s.xMax - G - .55f,', DOMY)   # tablica nie przed schodami

    def test_bench_press_full_length(self):
        self.assertIn("dlL = 1.4f * k;", DOMY)
        self.assertIn("zb - dlL + .25f), new Vector3(wx + .16f * k, y + .46f * k, zb + .25f), true);", DOMY)

    def test_tyre_shop_has_tyres_wheels_and_machines(self):
        # 10.11 01:39: „wulkanizacja — opony, koła gdzie?”
        blok = DOMY[DOMY.index("if (wulk)\n"):DOMY.index("if (przych)")]
        for czesc in ('"old_tyre"', '"rusted_wheel_rim_01"', '"bench_vice_01"', '"metal_tool_chest"', "Opona(F.P(sR0 + .18f + q * .26f, yl - .005f, dS), F.R,",
                      "Kolo(F.P(sm, yS + .05f, dm), Vector3.up,", "Stos(F.P(G + .52f, yF, 2.05f), 4,",
                      'var o = Rekw(rd, new[] { "old_tyre" }, p, yaw, .6f, false, 28f, nazwa, true);'):   # model stoi: wysokość = średnica 0.6 m
            self.assertIn(czesc, blok, czesc)
        self.assertNotIn("KolaWarsztatow", blok)
        for m in ("old_tyre", "rusted_wheel_rim_01", "bench_vice_01", "metal_tool_chest"):
            self.assertIn('"' + m + '"', FETCH_R, m)

    def test_laundry_machines_are_built_from_parts_not_boxes(self):
        # 10.11: pralki-pudła odrzucone (atrapy), 00:54 „pralki mogłyby być ładniejsze”
        blok = DOMY[DOMY.index("if (pralnia)"):DOMY.index("if (wulk)")]
        for czesc in ("float sF = G + .72f, yT = yF + .86f * k;", "void Drzwi(Vector3 c, Vector3 n, Vector3 a, float rO, bool zPraniem, int nr)",
                      "W(cx, cz, szyba, Color.white).Cyl(c + n * .052f, c + n * .085f, rO * .79f, 20, rO * .5f);",   # wypukła szyba, za nią bęben i pranie
                      "// wrzutnik monet", "for (int t2 = 0; t2 < 2; t2++)", '"wicker_basket_01", "wicker_basket_02"', '"bleach_bottle"', '"plastic_monobloc_chair_01"'):
            self.assertIn(czesc.replace("// wrzutnik monet", "d1 - .13f, d1 - .03f, true);"), blok, czesc)
        self.assertIn('emalia = T("pralki i suszarki — biała emalia (stara)", 1.6f, Color.white, .3f, "white_stucco"', FAW)
        self.assertIn("KolF(rd, F, G, sF + .06f, yF, yT + .03f, 1.5f, 1.5f + 4 * .82f);", blok)     # 1.5 m w głąb — wejście wolne

    def test_no_statement_after_trailing_comment(self):
        pat = re.compile(r'(\b(var|float|int|bool|string)\s+\w+\s*=)|(\b\w+(\.\w+)*\([^\n]*\)\s*;\s*$)')
        pliki = sorted(R.glob("BraxRegionFawela*.cs")) + sorted((ROOT / "unity-city/Assets/Claude/Runtime").glob("BraxLinaSplot*.cs"))
        zle = []
        for f in pliki:
            for i, l in enumerate(f.read_text(encoding="utf-8").splitlines(), 1):
                j = l.find("//")
                if j < 0 or l[:j].count('"') % 2 == 1 or not l[:j].strip(): continue
                if pat.search(l[j + 2:]): zle.append(f.name + ":" + str(i))
        self.assertEqual(zle, [])


if __name__ == "__main__":
    unittest.main()


class SklepyPelne(unittest.TestCase):
    """10.11 (właściciel 07:52–08:09): sklepy, apteki, wszystko pełne; kebab, pizza; prawdziwe jedzenie, nie tylko etykiety."""
    LIB = ROOT / "unity-city" / "Assets" / "Source" / "Claude" / "Library"

    def test_real_food_models_in_library(self):
        for nazwa in ("skrzynka_jablka", "skrzynka_cytryny", "skrzynka_limonki", "skrzynka_cebula", "skrzynka_bataty", "skrzynka_kiwi",
                      "skrzynka_granaty", "skrzynka_gruszki", "banany_kisc", "bulki", "chleb_bochenki", "bagietki", "chleb_okragly",
                      "kanister_mleka", "papierosy", "konsola", "doniczka_anturium_1", "doniczka_kalatea_3", "doniczka_paproc_2"):
            p = self.LIB / (nazwa + ".glb")
            self.assertTrue(p.exists(), nazwa)
            self.assertLess(p.stat().st_size, 900_000, nazwa + ": tekstury 512 px JPEG, nie PNG 1K")
        self.assertIn("CC0", (self.LIB / "skrzynki_owocow.CREDITS.md").read_text(encoding="utf-8"))
        self.assertTrue((ROOT / "unity-tools" / "claude" / "skrzynki_owocow.py").exists())

    def test_shop_types_use_real_food(self):
        towar = src(R / "BraxSklepyTowar.cs")
        for typ, musi in (("warzywniak", "ph:skrzynka_jablka"), ("piekarnia", "ph:chleb_bochenki"), ("pieczywo", "ph:bagietki"),
                          ("wedliny", "sk:kielbaski_paczka"), ("kebab", "sk:kebab"), ("pizzeria", "sk:pizza"), ("kwiaty", "ph:doniczka_"),
                          ("elektronika", "ph:television_02"), ("dom", "ph:pot_enamel_01"), ("kiosk", "ph:papierosy")):
            linia = [l for l in towar.splitlines() if l.strip().startswith('"' + typ + '" =>')]
            self.assertTrue(linia, typ)
            self.assertIn(musi, linia[0], typ)
        self.assertNotIn('"ph:bananas"', towar, "kiść bananów ze skanu 47 tys. trójkątów — w bibliotece lżejsza banany_kisc")

    def test_favela_shops_full(self):
        self.assertIn('bool kebab = rodzaj == "KEBAB", pizza = rodzaj.StartsWith("PIZZA")', DOMY)
        self.assertIn('"gastro_blat"', DOMY)                                  # kebab / pizza-kebab: blat z prawdziwym jedzeniem, nie półki z butelkami
        self.assertIn('"KEBAB"', src(R / "BraxRegionFawela.cs")); self.assertIn('"PIZZA · KEBAB"', src(R / "BraxRegionFawela.cs"))
        self.assertIn("int nPol = kebab || pizza ? 0 : apteka ? 7 : 4", DOMY)   # apteka: gęste półki, leki w dwóch warstwach
        self.assertIn("string[] regalBoczny = apteka ?", DOMY)              # drugi regał w sklepach
        self.assertIn('BraxSklepyTowar.Polka("wedliny"', DOMY)              # gablota z wędlinami w spożywczym
        self.assertIn("float zs0 = w1 + .2f", DOMY)                         # warzywniak: stragan obok wejścia, nie w drzwiach
        self.assertIn("bool piec = pizza", DOMY); self.assertIn("zarPieca", DOMY)

    def test_city_every_shop_has_interior(self):
        u = src(R / "BraxUlicePierzeje.cs")
        self.assertIn('if (!(wiez && lok.typ == "fitness"))', u)                  # także Bar 24 (ETAP-GALERIA p. 4)
        self.assertIn('"ZAKŁADY"', u); self.assertIn('"CashRegister_01"', u)
        self.assertIn('(f.wiezowiec && f.lok.typ == "fitness") || !f.front ? BraxUliceKit.OknoPokoj(', u)
        for co in ("static void Wykonczenie(", "static void Wyposazenie(", '"RECEPCJA"', '"KUPNO  SPRZEDAŻ"', "static void Rower(", "static void KoloRoweru(", "string TypPolki("):
            self.assertIn(co, u, co)
        towar = src(R / "BraxSklepyTowar.cs")
        for typ in ("kwiaty", "optyk", "poczta", "warsztat"):
            self.assertIn('"' + typ + '" =>', towar, typ)

    def test_new_poly_haven_models_are_fetched(self):
        fetch = (ROOT / "unity-tools" / "claude" / "fetch_region.py").read_text(encoding="utf-8")
        towar = src(R / "BraxSklepyTowar.cs") + src(R / "BraxUlicePierzeje.cs") + DOMY
        lib = {p.stem for p in self.LIB.glob("*.glb")}
        for ph in set(re.findall(r'"ph:([A-Za-z0-9_]+)', towar)):
            if ph in lib: continue
            self.assertIn('"' + ph + '"', fetch, "model Poly Haven bez pobierania w CI: " + ph)

    def test_rotating_doner_and_chicken_rotisserie(self):
        """10.11 09:10: kebab, który się kręci (pręt widać tylko nad i pod mięsem); kurczak właściciela na rożnie z grzałką."""
        self.assertTrue((self.LIB / "kebab_mieso.glb").exists())
        obrot = (ROOT / "unity-city" / "Assets" / "Claude" / "Runtime" / "BraxObrot.cs").read_text(encoding="utf-8")
        self.assertIn("transform.Rotate(0f, stopniNaSekunde * Time.deltaTime, 0f, Space.World)", obrot)
        u = src(R / "BraxUlicePierzeje.cs")
        for kod in (DOMY, u):
            self.assertIn('BraxRegion.Model("kebab_mieso")', kod)
            self.assertIn("AddComponent<BraxObrot>()", kod)
            self.assertIn("(StaticEditorFlags)0", kod)                  # kręcący się obiekt nie może być statyczny
            self.assertIn('Model("miasto_sklepy", "kurczak")', kod)
