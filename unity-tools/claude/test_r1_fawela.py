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
        self.assertIn("BoxF(W(cx, cz, metal, new Color(.16f, .16f, .17f)), F, sx0, sx1, sy0, sy1, -.3f, 0f);", DOMY)
        self.assertIn("BraxRegionMeble.Napis(rd, rodzaj,", DOMY)

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


if __name__ == "__main__":
    unittest.main()
