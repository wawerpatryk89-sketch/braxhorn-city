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
        for wywolanie in ("BraxSklepyTowar.Korzen(rd,", "BraxSklepyTowar.Polka(typPolek[p], tw,", "BraxSklepyTowar.Lada(typLady, tw,", "BraxSklepyTowar.Zakoncz(tw,"):
            self.assertIn(wywolanie, DOMY, wywolanie)
        towar = src(R / "BraxSklepyTowar.cs")
        blok = DOMY[DOMY.index("string[] typPolek ="):DOMY.index("Transform tw =")]
        for typ in set(re.findall(r'"([a-z_0-9]+)"', blok)):
            self.assertIn('"' + typ + '"', towar, "typ towaru spoza biblioteki: " + typ)
        # kolorowych walców i klocków jako towaru już nie ma (zostają tylko pudełka akcesoriów w naprawie telefonów — biblioteka nie ma elektroniki)
        self.assertNotIn("var ow = Color.HSVToRGB(new[] { .02f, .11f, .16f, .3f }[q]", DOMY)
        self.assertIn("Towar(F, s, y + .035f, D - .36f, r, cx, cz, tel ? 2 : 0)", DOMY)

    def test_equipment_from_library(self):
        for model in ('"miasto_sklepy", "lodowka"', '"miasto_sklepy", "menu_4"', '"miasto_sklepy", "plakat_0"', '"miasto_jedzenie", "wozek_jedzenie"', '"miasto_silownia_metro", "silownia_atlas"'):
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
    """Właściciel 10.11 00:04: „wieżowce zrobić murowane, jak policja / szpital… mają być wysokie”."""

    def test_centre_units_replaced_by_masonry_towers(self):
        self.assertIn('if (BraxRegionFawela.WiezeMurowaneWlaczone && b.paczka == "miasto_centrum")', DZIELNICE)
        self.assertIn("BraxRegionFawela.WiezaMurowana(g, b.id, b.x, Podloze(b.x, b.z, YC), b.z, b.w, b.d);", DZIELNICE)
        self.assertEqual(len(re.findall(r'new Bud\("C5_U\d\d", "miasto_centrum_c5"', DZIELNICE)), 14)
        self.assertIn("public static readonly bool WiezeMurowaneWlaczone = true;", WIEZE)

    def test_towers_are_tall_and_stepped(self):
        self.assertIn("int N = dlugi ? 22 + r.Next(4) : 24 + r.Next(8);", WIEZE)      # 22–31 kondygnacji po 4.4 m
        self.assertIn("var pietra = new[] { (R0, 0, n1), (R1, n1, n2), (R2, n2, n3), (R3, n3, N) };", WIEZE)

    def test_five_masonry_styles_from_photoscans(self):
        for t in ("red_brick", "brick_wall_12", "dark_brick_wall", "factory_brick", "sandstone_blocks_08", "marble_01", "granite_tile_03", "granite_tile_04"):
            self.assertIn('"' + t + '"', WIEZE, t); self.assertIn('"' + t + '"', FETCH_R, t)
        self.assertEqual(WIEZE.count("new StylWiezy {"), 5)

    def test_windows_on_world_grid_with_room_behind_glass(self):
        self.assertIn("const float WG = 3.6f;", WIEZE)
        self.assertIn("BraxUliceKit.OknoPokoj(WG, kondygnacja, floor0, 0, 5, 4.6f, 0f)", WIEZE)
        self.assertIn("float s = (j * WG - a0) / kier;", WIEZE)

    def test_base_crown_and_roof_details(self):
        for czesc in ("PortalWiezy(F, o, WB, st, oknaW, rama, id, si)", "bool namiot = nr % 3 == 1 && !dlugi;", "float yKor = ostatni ? Y(f1 - 2) : y1 + att;"):
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


if __name__ == "__main__":
    unittest.main()
