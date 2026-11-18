"""MIASTO R1 18.11 — pływanie i nurkowanie w morzu miasta (właściciel 12:36: „ta woda to jest woda, a nie pasek… Nie
zanurkujesz… i widać nic”; 13:04: „No tak, tak. Zrób tak.” — zgoda na przeniesienie pływania i nurkowania z gałęzi Mistrzów).
Kąpielisko plaży miejskiej: strefa wody z progiem głębokości, przycisk NURKUJ, kamera pod wodą, lustro od spodu (okno Snella),
światło w wodzie od kamery w shaderach dna i miasta (tylko pod lustrem — nad wodą bez mgły), boje widoczne spod wody."""
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
A = ROOT / "unity-city/Assets"


def txt(p):
    return (A / p).read_text(encoding="utf-8")


ZONE = txt("Claude/Runtime/BraxSwimmingZone.cs")
PLAYER = txt("BraxGymPlayer.cs")
KAPIEL = txt("Claude/Runtime/BraxMiastoKapiel.cs")
NUREK = txt("Claude/Runtime/BraxMiastoNurek.cs")
GLEBIA = txt("Claude/Runtime/BraxWodaGlebia.cs")
PLAZA = txt("Editor/Claude/Region/BraxNabrzezePlaza.cs")
MEBEL = txt("Editor/Claude/Region/BraxNabrzezeMebel.cs")
DNO = txt("Editor/CityRebuild/Environment/Water/BraxWodaDno.cs")
WATER = txt("Editor/CityRebuild/Environment/Water/HarbourWater.shader.txt")
BUILD = txt("Editor/CityRebuild/Environment/Water/BraxCityWaterBuild.cs")
CGINC = txt("Claude/Shaders/MiastoPodWoda.cginc")
TEREN = txt("Claude/Shaders/NabrzezeTeren.shader")
PBR = txt("CityRebuild/Shaders/CityStaticPBR.shader")
OBJ = txt("Claude/Shaders/MiastoWodaObjetosc.shader")
EKRAN = txt("Claude/Shaders/MiastoPodWodaEkran.shader")


def wektor(zrodlo, nazwa):
    m = re.search(nazwa + r" = new Vector4\(([^)]*)\)", zrodlo)
    return [float(x.strip().rstrip("f")) for x in m.group(1).split(",")]


class WspolnePlywanie(unittest.TestCase):
    """Wspólny kod z gałęzi Mistrzów (BraxSwimmingZone + pływanie w BraxGymPlayer)."""

    def test_zone_from_champions_branch(self):
        self.assertIn("public class BraxSwimmingZone : MonoBehaviour", ZONE)
        self.assertIn("if (p.vessel || p.seat) return;", ZONE)                       # nie zabiera łodzi ani siedzenia
        self.assertIn("p.EnterSwimmingZone(GetInstanceID(), waterSurface.position.y);", ZONE)
        self.assertIn("if (volume) volume.isTrigger = true;", ZONE)                   # woda nie jest podłogą
        meta = (A / "Claude/Runtime/BraxSwimmingZone.cs.meta").read_text(encoding="utf-8")
        self.assertIn("guid: 8bd18342d9cf52fda97569b63e0da981", meta)                # ten sam guid co u Mistrzów

    def test_player_swims_with_buoyancy(self):
        self.assertIn("public bool Swimming => swimmingZoneId!=0 && !vessel && !seat;", PLAYER)
        self.assertIn("float target=jumping?speed:!acting?(swimNow?naJoy*.48f:naJoy):0;", PLAYER)
        self.assertIn("float targetFeet=swimmingSurfaceY-body.height*.60f;", PLAYER)
        self.assertIn("vertical=Mathf.Clamp((targetFeet-transform.position.y)*2.8f,-1.6f,1.6f);", PLAYER)
        self.assertGreaterEqual(PLAYER.count("ClearSwimming();"), 2)                  # reset i przejście na brzeg

    def test_dive_camera_follows_under_water(self):
        self.assertIn("nurekKam=Mathf.MoveTowards(nurekKam,Swimming&&swimmingSurfaceY<transform.position.y-1f?1f:0f,(snap?1f:dt)*1.5f);", PLAYER)
        self.assertIn("pi=Mathf.Lerp(pitch,3f,nurekKam)*Mathf.Deg2Rad;", PLAYER)
        self.assertIn("float dist=Mathf.Lerp(distance,3.4f,nurekKam);", PLAYER)

    def test_depth_rescue_off_while_swimming(self):
        self.assertIn("if (gracz.Swimming) return;", GLEBIA)


class Kapielisko(unittest.TestCase):
    def test_zone_with_depth_threshold_and_dive(self):
        self.assertIn("public sealed class BraxMiastoKapiel : BraxSwimmingZone", KAPIEL)
        self.assertIn("public float wejscie = .9f, wyjscie = .7f;", KAPIEL)
        self.assertIn("p.EnterSwimmingZone(GetInstanceID(), lustro - BraxMiastoNurek.Glebokosc);", KAPIEL)

    def test_dive_button_and_keys(self):
        self.assertIn("const float W_Dol = 40f;", NUREK)
        self.assertIn('"NURKUJ"', NUREK)
        self.assertIn("Input.GetKey(KeyCode.C)", NUREK)
        self.assertIn("[DefaultExecutionOrder(10000)]", NUREK)                        # po kamerze: stan z tej samej klatki

    def test_beach_scene(self):
        self.assertIn("const int NX = 196, NZ = 78;", PLAZA)                          # dno z piasku do x 260
        self.assertIn("float xb = 196f, zs = 114.5f, zn = 164f;", PLAZA)              # boje za rewą: ok. 3.5 m wody
        self.assertIn('var gk = new GameObject("Kąpielisko: woda (pływanie i nurkowanie)");', PLAZA)
        self.assertIn('kap.basinId = "plaza_miejska";', PLAZA)
        self.assertIn("nurek.ekran = EkranPodWoda();", PLAZA)
        self.assertIn("public static readonly Rect Plaza = Rect.MinMaxRect(64f, 100f, 260f, 178f);", DNO)   # dno wody = teren

    def test_buoys_whole_sphere_visible_from_below(self):
        self.assertIn(".22f * BraxNabrzeze.K, true);", PLAZA)
        self.assertIn("public static void Boja(BraxNabrzeze.Zbior z, Material kolor, Vector3 at, float r, bool calaKula = false)", MEBEL)
        self.assertIn("float od = calaKula ? -1f : -.25f;", MEBEL)


class SwiatloWWodzie(unittest.TestCase):
    def test_only_below_surface_no_fog_above(self):
        self.assertIn("if (_BraxPodWodaJest < 0.5 || cam.y >= _BraxPodWodaLustro) return c;", CGINC)
        self.assertIn("float3 T = exp(-_BraxPodWodaPochlanianie.rgb * (length(d) * wWodzie));", CGINC)
        self.assertIn("Shader.SetGlobalFloat(IdJest, jest ? 1f : 0f);", NUREK)
        for nazwa in ("_BraxPodWodaJest", "_BraxPodWodaLustro", "_BraxPodWodaPochlanianie", "_BraxPodWodaRozproszenie"):
            self.assertIn('"' + nazwa + '"', NUREK)
            self.assertIn(nazwa, CGINC)

    def test_same_coefficients_as_water_shader(self):
        self.assertEqual(wektor(NUREK, "Pochlanianie"), [.45, .09, .075, 0.0])
        self.assertIn('material.SetVector("_Pochlanianie",new Vector4(.45f,.09f,.075f,0f));', BUILD)
        # rozproszone = _DeepColor × 1.6 (głębia lustra od spodu, fragPod)
        self.assertIn('material.SetColor("_DeepColor",new Color(.010f,.052f,.070f).gamma);', BUILD)
        for r, d in zip(wektor(NUREK, "Rozproszenie")[:3], (.010, .052, .070)):
            self.assertAlmostEqual(r, d * 1.6, places=3)
        self.assertIn("half3 glebia = _DeepColor.rgb * 1.6h;", WATER)

    def test_seabed_and_city_shaders_use_it(self):
        self.assertIn("#pragma surface surf Standard fullforwardshadows vertex:vert finalcolor:PodWoda", TEREN)
        self.assertIn('#include "MiastoPodWoda.cginc"', TEREN)
        self.assertIn("color.rgb = BraxPodWoda(IN.worldPos, color.rgb, false);", TEREN)   # przebieg dodatkowy: bez rozproszenia
        self.assertIn('#include "Assets/Claude/Shaders/MiastoPodWoda.cginc"', PBR)
        a = PBR.index("colour = BraxPodWoda(i.worldPos, colour, true);")
        self.assertLess(PBR.index("_BraxMgielkaGestosc > 0"), a)
        self.assertLess(a, PBR.index("colour = LinearToGammaSpace(max(colour,0));"))
        self.assertEqual(PBR.count("BraxPodWoda("), 1)

    def test_no_seabed_skin(self):
        # skóra 5 cm nad dnem dawała jasną linię na grzbiecie rewy — dno tłumi shader terenu
        self.assertNotIn('Obiekt("skóra dna"', PLAZA)
        self.assertIn("renderQueue = 3005", PLAZA)                                     # zostaje odwrócone pudło akwenu
        self.assertIn('m.SetVector("_Pochlanianie", BraxMiastoNurek.Pochlanianie);', PLAZA)

    def test_volume_and_veil_shaders(self):
        self.assertIn("if (_WorldSpaceCameraPos.y >= _Lustro) discard;", OBJ)         # nad wodą nic
        self.assertIn("Blend Zero SrcColor", OBJ)
        self.assertIn("Blend One One", OBJ)
        self.assertIn('Shader "Braxhorn/MiastoPodWodaEkran"', EKRAN)


class LustroOdSpodu(unittest.TestCase):
    def test_two_passes_snell_window(self):
        self.assertEqual(WATER.count("Pass\n"), 2)
        dol = WATER[WATER.rindex("Pass\n"):]
        self.assertIn("Cull Front", dol)
        self.assertIn("#pragma fragment fragPod", dol)
        self.assertIn("float3 T = refract(-V, N, 1.33);", dol)                        # z wody w powietrze
        self.assertIn("float3 N = -NormalnaWody(xz, _Time.y);", dol)
        gora = WATER[WATER.index("Pass\n"):WATER.rindex("Pass\n")]
        self.assertIn("Cull Back", gora)
        for zle in ("GrabPass", "_CameraDepthTexture", "_GrabTexture"):
            self.assertNotIn(zle, WATER)


if __name__ == "__main__":
    unittest.main()
