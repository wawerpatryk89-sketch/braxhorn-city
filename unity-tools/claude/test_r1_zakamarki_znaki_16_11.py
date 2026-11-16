"""MIASTO R1 16.11.2026 — przegląd zakamarków i oznakowanie w teksturze. Źródło, bez Unity i APK.

Właściciel 13:32–13:37: „samochody w powietrzu wiszą, las, slumsy — masz przejrzeć każdy zakamarek… sklepy: z zewnątrz mur, w środku
szkło albo pustka… nowe zasady parametrów rysowania, kontrast — dostosować… każde wykończenie, każdy detal”.
Właściciel 13:57: „1. Linie węższe niż 25 cm — w teksturę, nie w geometrię (tekstura ma mipmapy, geometria nie). 2. Żadnej farby
wiszącej nad powierzchnią. Licznik nakladki=0 przed oddaniem gałęzi.”
"""
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor"
REG = ED / "Claude/Region"


def src(p, komentarze=False):
    t = Path(p).read_text(encoding="utf-8")
    return t if komentarze else re.sub(r"//[^\n]*", "", t)


SHADER = src(ROOT / "unity-city/Assets/CityRebuild/Shaders/CityStaticPBR.shader")
ZNAKI = src(REG / "BraxZnakiPoziome.cs")
MB = src(REG / "BraxRegionMiastoB.cs")
ND = src(REG / "BraxRegionMiastoBDzielnice.cs")
WAR = src(REG / "BraxRegionMiastoBWarsztaty.cs")
FAW = src(REG / "BraxRegionFawelaDrogi.cs")
PORT = src(REG / "BraxMiastoPort.cs")
DWD = src(REG / "BraxMiastoDworzecDrogi.cs")
DWT = src(REG / "BraxMiastoDworzecTory.cs")
GYM = src(ED / "BraxGymBuild.cs")
CUT = src(ED / "CityRebuild/BraxCityMeshCut.cs")
REGION = src(REG / "BraxRegion.cs")
ZAK = src(ED / "Claude/BraxKontrolaZakamarkow.cs")
OBRAZ = src(REG / "BraxR1Obraz.cs")
RUCH = src(REG / "BraxRegionRuchMiasta.cs")
MEBLE = src(REG / "BraxRegionMeble.cs")
FAWELA = src(REG / "BraxRegionFawela.cs")
ULICE = src(REG / "BraxUliceKit.cs")


class LinieWTeksturze(unittest.TestCase):
    def test_shader_has_mipmapped_line_mask_on_uv2(self):
        self.assertIn("#pragma shader_feature_local _ _BRAX_ZNAKI", SHADER)
        self.assertIn("float2 znaki : TEXCOORD2;", SHADER)          # UV2 siatki
        self.assertIn("float2 znaki : TEXCOORD6;", SHADER)          # wolny interpolator (5 = cienie)
        self.assertIn('_ZnakiTex("Line mask across the strip (linear, mipmapped, clamp)", 2D) = "black" {}', SHADER)
        self.assertIn("tex2D(_ZnakiTex, float2(i.znaki.x, .5))", SHADER)
        self.assertIn("tex2D(_ZnakiTex, float2(i.znaki.y, .5))", SHADER)
        self.assertIn("roughness = lerp(roughness, _ZnakiSzorstkosc, znak);", SHADER)

    def test_mask_texture_and_threshold(self):
        self.assertIn("public const float Prog = .25f;", ZNAKI)
        self.assertIn("public const float Wypelnienie = .3f;", ZNAKI)
        self.assertIn("t.Apply(true, false);", ZNAKI)                   # mipmapy
        self.assertIn("filterMode = FilterMode.Trilinear", ZNAKI)
        self.assertIn("wrapModeU = TextureWrapMode.Clamp", ZNAKI)
        self.assertIn('m.EnableKeyword("_BRAX_ZNAKI")', ZNAKI)

    def test_builders_write_uv2(self):
        for nazwa, s in (("BraxRegionMeble.Bud", MEBLE), ("BraxRegionFawela.Bud", FAWELA), ("UlSiatka", ULICE)):
            self.assertIn("SetUVs(2, uv2)", s, nazwa)

    def test_miasto_b_thin_holes_become_texture_strips(self):
        self.assertIn("var h = otwory.Where(o => !BraxZnakiPoziome.Cienka(o))", MB)
        self.assertIn("var pasy = new BraxZnakiPoziome.Pasy(otwory.Where(o => BraxZnakiPoziome.Cienka(o)", MB)
        self.assertIn("bTop.Znaki(s0, BraxZnakiPoziome.Pasy.Uv2(px, pz))", MB)
        self.assertIn('top.Obiekt("Jezdnie miasta B (asfalt)", root, AsfaltLinie(top), false, false);', MB)
        self.assertNotIn('top.Obiekt("Jezdnie miasta B (asfalt)", root, asfalt, false, false);', MB)
        self.assertIn("AsfaltLinie(bTop)", ND)
        self.assertIn("pasy.Komorka(c, out var px, out var pz)", ND)
        self.assertIn("AsfaltLinie(pt)", WAR)                            # plac ustawek
        self.assertIn("new BraxZnakiPoziome.Pasy(linie)", WAR)           # linie BHP w posadzce
        self.assertNotIn("linie BHP (w wycięciach)", WAR)

    def test_main_street_old_floating_paint_replaced(self):
        # „Centre line dash” (.006–.012 nad asfaltem .003) i „Daylight road paint” (.008–.016) — zdjęte, oznakowanie w łatach nawierzchni
        self.assertIn('t.name != "Centre line dash"', MB)
        self.assertIn('t.name != "Daylight road paint"', MB)
        self.assertIn("UnityEngine.Object.DestroyImmediate(t.gameObject); stare++;", MB)
        self.assertIn('Wstawki("Ulica główna: łata oznakowania", R, .003f', MB)
        self.assertIn("new BraxZnakiPoziome.Wypalenie(ulica, R)", MB)
        self.assertNotIn("pA.Quad(new Vector3(q.xMin, .003f", MB)

    def test_favela_axis_in_texture_not_polygon(self):
        self.assertIn("bA.ZnakiUV(s0, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f))", FAW)
        self.assertNotIn("bF.Quad(V(i, .06f, .012f)", FAW)              # farba 1.2 cm nad asfaltem
        self.assertNotIn("bF.Quad(V(i, o6[j], 0f)", FAW)
        self.assertIn('"Fawela: progi zwalniające (żółto-czarne)"', FAW)

    def test_port_and_station_lines_in_texture(self):
        self.assertIn("pasy.Dodaj(biale.Where(BraxZnakiPoziome.Cienka), 0); pasy.Dodaj(linie.Where(BraxZnakiPoziome.Cienka), 1);", PORT)
        self.assertIn("foreach (var r in linie.Where(r => !BraxZnakiPoziome.Cienka(r)))", PORT)
        self.assertIn("sz.Znaki(s0, q => new Vector2(uf(XZ(q)), 0f))", DWD)
        self.assertIn("(.85f, 1.13f, plytyLinia)", DWT)
        self.assertNotIn("(.95f, 1.03f, farba)", DWT)


class LicznikNakladek(unittest.TestCase):
    def test_counter_runs_after_all_city_checks(self):
        i_k = GYM.index('BraxKontrola.Wszystko(scene,"miasto")')
        i_n = GYM.index("BraxZnakiPoziome.Kontrola(scene)")
        self.assertLess(i_k, i_n)
        self.assertIn('"BRAXHORN_NAKLADKI nakladki="', ZNAKI)
        self.assertIn("cienkie_geometria=", ZNAKI)
        self.assertIn('Debug.LogWarning("DO POPRAWKI: " + sb)', ZNAKI)
        self.assertIn("if (dy < -.0005f || dy > .06f) continue;", ZNAKI)  # 0–6 cm pod farbą albo ta sama wysokość

    def test_mesh_cut_keeps_baked_light(self):
        self.assertIn("public Vector4 b3, b7;", CUT)
        self.assertIn("source.GetUVs(3, b3)", CUT)
        self.assertIn("mesh.SetUVs(7, l)", CUT)
        self.assertEqual(CUT.count("ZapiszWypalenie(mesh"), 2)          # Subtract i SubtractMany


class Zakamarki(unittest.TestCase):
    def test_region_steps(self):
        i_z = REGION.index("BraxKontrolaZakamarkow.Wykonaj(world, root)")
        i_o = REGION.index("BraxR1Obraz.Zastosuj(root)")
        i_f = REGION.index("BraxRegionFlicker.Apply(root)")
        self.assertLess(i_z, i_o); self.assertLess(i_o, i_f)

    def test_supermarket_interiors_checked_after_it_is_built(self):
        i_s = GYM.index("BraxSupermarket.Wykonaj(scene,2)")
        i_p = GYM.index('BraxKontrolaZakamarkow.WnetrzaPoBudowie(scene,"supermarket")')
        self.assertLess(i_s, i_p)
        self.assertIn("public static void WnetrzaPoBudowie(", ZAK)
        self.assertIn('id.Contains("srodek")', ZAK)

    def test_supermarket_cars_on_road_level(self):
        self.assertIn("MiejscaParkingu.Add((new Vector3(AX - 10.0f, YA, z)", MB)
        self.assertIn("MiejscaParkingu.Add((new Vector3(AX + 10.0f, YA, z)", MB)
        self.assertIn("internal static float NawierzchniaPod(Vector3 p, float zapas)", RUCH)

    def test_image_standard(self):
        self.assertIn("public const float Ekspozycja = 1.62f;", OBRAZ)
        self.assertIn("sun.intensity = 1.24f;", OBRAZ)
        self.assertIn("RenderSettings.fogDensity = .00042f;", OBRAZ)
        self.assertIn("kol.nasycenie = 1.15f; kol.kontrast = 1.06f;", OBRAZ)
        self.assertIn("half3 colour = (direct + albedo*(1-metallic)*ambient*ambientAO) * ekspozycja;", SHADER)


if __name__ == "__main__":
    unittest.main()
