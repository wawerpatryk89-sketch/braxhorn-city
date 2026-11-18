"""MIASTO R1 18.11 — realistyczna woda (właściciel 12:28–13:02: „nie możemy zrobić bardziej realistycznej wody?”, „ta woda to
jest woda, a nie pasek… widać nic”, „każdą jedną wodę bardziej realistyczną”). Jeden materiał wody (port, morze za kamerą,
akwen wyspy): fale z widma FFT, odbicie panoramy miasta, dno z piasku przez załamanie z kaustykami, pochłanianie w wodzie."""
import hashlib
import importlib.util
import re
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
WODA = ROOT / "unity-city/Assets/Editor/CityRebuild/Environment/Water"
SHADER = (WODA / "HarbourWater.shader.txt").read_text(encoding="utf-8")
BUILD = (WODA / "BraxCityWaterBuild.cs").read_text(encoding="utf-8")
DNO = (WODA / "BraxWodaDno.cs").read_text(encoding="utf-8")
HOR = (ROOT / "unity-city/Assets/Editor/Claude/Region/BraxRegionHoryzont.cs").read_text(encoding="utf-8")
SEA = (ROOT / "unity-city/Assets/Editor/Claude/Region/BraxRegionSea.cs").read_text(encoding="utf-8")
TEX = ROOT / "unity-city/Assets/Source/Claude/Miasto/woda"


def sha(p):
    return hashlib.sha256(Path(p).read_bytes()).hexdigest()


class Shader(unittest.TestCase):
    def test_properties_for_waves_sky_seabed(self):
        for p in ("_FaleNormal", "_FaleSkala", "_Kaustyki", "_DnoTex", "_DnoProstokat", "_DnoMax", "_DnoAlbedo", "_DnoKolor",
                  "_Pochlanianie", "_NieboTex", "_NieboObrot", "_NieboJest", "_SlonceKolor", "_Otoczenie",
                  "_RippleStrength", "_SwellStrength", "_NearRectangle"):          # dwie ostatnie: łodzie i akwen wyspy
            self.assertIn(p + " (", SHADER, p)

    def test_physics(self):
        self.assertIn("float F = 0.02 + 0.98 * pow(1.0 - NdotV, 5.0);", SHADER)           # Fresnel wody n = 1.33
        self.assertIn("refract(-V, N, 0.7519)", SHADER)                                      # 1 / 1.33
        self.assertIn("sqrt(1.0 - (1.0 - Ly * Ly) / 1.7689)", SHADER)                       # promień słońca załamany (1.33²)
        self.assertIn("half3 Tv = exp(-k * path);", SHADER)                                  # pochłanianie po drodze w wodzie
        self.assertIn("pod = albedo * swiatloDna * Tv + woda * (1.0h - Tv);", SHADER)
        self.assertIn("half3 color = lerp(pod, sky, F * _ReflectionStrength);", SHADER)

    def test_sky_reflection_same_mapping_as_city_sky(self):
        tlo = (ROOT / "unity-city/Assets/Claude/Shaders/MiastoTlo.cginc").read_text(encoding="utf-8")
        self.assertIn("float lon = atan2(d.z, d.x) - obrot;", tlo)
        self.assertIn("float lon = atan2(r.z, r.x) - _NieboObrot;", SHADER)
        self.assertIn("float2 uv = float2(frac(0.5 - lon / W_2PI), 0.5 + lat / W_PI);", SHADER)
        self.assertIn("return float2(frac(0.5 - lon / MT_2PI), 0.5 + lat / MT_PI);", tlo)

    def test_mobile_safe(self):
        self.assertEqual(SHADER.count("Pass\n"), 2)                                         # z góry + od spodu (nurkowanie)
        self.assertIn('"RenderType"="Opaque"', SHADER)
        for zle in ("GrabPass", "_CameraDepthTexture", "_GrabTexture"):
            self.assertNotIn(zle, SHADER)
        # w gałęzi dynamicznej tylko próbki z jawnym gradientem / LOD (bez pochodnych w warunku)
        a = SHADER.index("UNITY_BRANCH"); b = SHADER.index("// 6) Fresnel")
        self.assertNotIn("tex2D(", SHADER[a:b])
        self.assertIn("float2 dX = ddx(xz), dY = ddy(xz);", SHADER[:a])

    def test_swell_unchanged_for_boats(self):
        # BraxVessel liczy wysokość łodzi z tych samych faz fali
        self.assertIn("dot(q,float2(0.84,0.5426))*0.84", SHADER)
        self.assertIn("- _Time.y * float2(0.65,0.92);", SHADER)
        self.assertIn("world.y += dot(sin(phase),float2(0.10,0.06)) * SwellEnvelope(world.xz).x;", SHADER)


class Generator(unittest.TestCase):
    def test_material_gets_textures_and_depth(self):
        self.assertIn("var dno=BraxWodaDno.Tekstura(out float plytkoM2);", BUILD)
        self.assertIn('BraxZapisAssetu.Utworz(dno,Output+"/dno_glebokosc.asset");', BUILD)
        for p in ('"_DnoTex",dno', '"_FaleNormal",fale', '"_Kaustyki",kaustyki', '"_DnoAlbedo",piasek', '"_DnoProstokat",BraxWodaDno.Prostokat'):
            self.assertIn(p, BUILD)
        self.assertIn('BraxNabrzeze.Piaski+"damp_sand_diff_2k.jpg"', BUILD)                  # ten sam piasek co plaża
        self.assertIn('public const string Fale = "Assets/Source/Claude/Miasto/woda/fale_normal.png";', BUILD)
        self.assertIn('public const string Kaustyki = "Assets/Source/Claude/Miasto/woda/kaustyki.png";', BUILD)
        self.assertIn("if(patches.Count!=11)", BUILD)                                       # siatka bez zmian

    def test_sky_set_by_horizon_for_every_water_material(self):
        self.assertIn("BraxCityWaterBuild.UstawNiebo(nieboMat);", HOR)
        self.assertLess(HOR.index("RenderSettings.skybox = nieboMat;"), HOR.index("BraxCityWaterBuild.UstawNiebo(nieboMat);"))
        self.assertIn("FindObjectsByType<BraxMorzeZaKamera>", BUILD)                         # morze za kamerą i akwen wyspy
        self.assertIn('m.shader.name=="Braxhorn/HarbourWaterMobile"', BUILD)
        self.assertIn('m.SetFloat("_NieboObrot",niebo.GetFloat("_NieboObrot"));', BUILD)

    def test_seabed_sources(self):
        self.assertIn("BraxNabrzezePlaza.Wysokosc(x, z)", DNO)                               # piasek plaży = prawdziwy teren
        self.assertIn("BraxMiastoWyspa.H(lx, lz)", DNO)                                      # wyspa
        self.assertIn("public const float DnoKolizjiY = -7f;", DNO)
        self.assertIn("bc.center = new Vector3(492f, -7.5f, -25f); bc.size = new Vector3(816f, 1f, 990f);", SEA)   # wierzch kolizji y −7
        self.assertIn("public static readonly Vector4 Prostokat = new Vector4(60f, -452f, 1468f, 572f);", DNO)
        self.assertIn("TextureFormat.R8", DNO)
        self.assertIn("Mathf.Sqrt(d / MaxGlebokosc) * 255f", DNO)
        self.assertIn("return e * e * _DnoMax;", SHADER)                                     # dekodowanie jak zapis

    def test_far_water_matches_sky_sea(self):
        # od 700 m woda przechodzi w morze nieba (tryb zdjęcia) — bez krawędzi siatki morza za kamerą (1.6 km)
        self.assertIn("half3 dal = _DalekoKolor.rgb * lerp(0.78h, 1.0h, frd);", SHADER)
        tlo = (ROOT / "unity-city/Assets/Claude/Shaders/MiastoTlo.cginc").read_text(encoding="utf-8")
        self.assertIn("c = _MorzeKolor.rgb * lerp(0.78, 1.0, fres);", tlo)
        self.assertIn("half wz = 1.0h - smoothstep(0.010, 0.040, -d.y);", SHADER)
        self.assertIn('m.SetFloat("_DalekoOd",700f);m.SetFloat("_DalekoDo",foto?1500f:0f);', BUILD)
        self.assertIn('m.SetColor("_DalekoKolor",niebo.GetColor("_MorzeKolor"))', BUILD)

    def test_linear_values_reach_shader(self):
        self.assertIn('material.SetColor("_DeepColor",new Color(.010f,.052f,.070f).gamma);', BUILD)
        self.assertIn('material.SetColor("_SlonceKolor",new Color(1.24f,1.12f,.92f).gamma);', BUILD)

    def test_quay_walls_and_seams(self):
        self.assertIn("if (m >= prog) px[j * TexW + i] = m;", DNO)                          # mur: woda przy murze głęboka
        self.assertIn("float plaza = Mathf.Min(s < 14f ? .09f * s : 1.26f + .05f * (s - 14f), port);", DNO)
        self.assertIn("+ 1.5f * s;", DNO)                                                     # przejście port/plaża rośnie od brzegu

    def test_textures_exist(self):
        for f in ("fale_normal.png", "kaustyki.png"):
            self.assertTrue((TEX / f).is_file(), f)


class Tekstury(unittest.TestCase):
    # generator tekstur potrzebuje numpy i Pillow — w szybkim sprawdzaczu CI (tylko tree-sitter) ich nie ma: test pomijany
    @unittest.skipUnless(importlib.util.find_spec("numpy") and importlib.util.find_spec("PIL"), "brak numpy / Pillow")
    def test_reproducible_from_script(self):
        skrypt = ROOT / "unity-tools/claude/woda_fale.py"
        with tempfile.TemporaryDirectory() as tmp:
            subprocess.run([sys.executable, str(skrypt), "fale", tmp + "/f.png", "10", "7", "18"], check=True, capture_output=True)
            subprocess.run([sys.executable, str(skrypt), "kaustyki", tmp + "/k.png", "3", "3.5", "5", "2.4"], check=True, capture_output=True)
            self.assertEqual(sha(tmp + "/f.png"), sha(TEX / "fale_normal.png"))
            self.assertEqual(sha(tmp + "/k.png"), sha(TEX / "kaustyki.png"))


if __name__ == "__main__":
    unittest.main()
