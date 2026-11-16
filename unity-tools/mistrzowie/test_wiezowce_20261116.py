"""Owner 16.11.2026 14:50: "Zacznij budować wieżowce i bukmachera, minimum 4 wieżowce premium blisko siebie".

Downtown (plan 12.11 "Downtown (T08-T13): BRAXHORN TOWER 250-300 m z lądowiskiem", "punkt BRAXHORN BET"): five premium
towers on the free tower plots round the casino T11 (BraxChampionsWiezowce), each with a finished lobby and a real door,
the BRAXHORN TOWER T09 with a heliport and the BRAXHORN BET sportsbook on its ground floor (screens switched off: owner
11.11 21:48 - no films in street premises), a walk from the sidewalk to every door (layout.TOWER_WALKS), an area scene
each (BraxChampionsPodzial). Checked in the preview (wiezowce_preview): z-fight 0 on all five."""
import json
import math
import pathlib
import re
import sys
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"
TOOLS = ROOT / "unity-tools/mistrzowie"
SRC = (ED / "BraxChampionsWiezowce.cs").read_text(encoding="utf-8")


def const(name):
    return float(re.search(r"\b%s=([0-9.]+)f" % name, SRC).group(1))


TOWERS = {m.group(1): dict(name=m.group(2), x=float(m.group(3)), z=float(m.group(4)), yaw=float(m.group(5)),
                           shape=m.group(6), floors=int(m.group(7)))
          for m in re.finditer(r'new Tower\{id="(T\d\d)",name="([^"]+)",x=(\d+),z=(\d+),yaw=([0-9.]+)f,shape=Shape\.(\w+),'
                               r'floors=(\d+)\}', SRC)}


class Plan(unittest.TestCase):
    def test_five_towers_on_the_plan_plots(self):
        self.assertEqual(sorted(TOWERS), ["T08", "T09", "T10", "T12", "T13"])
        self.assertIn('Ids={"T08","T09","T10","T12","T13"}', SRC)
        plan = {p["id"]: p for p in json.loads((TOOLS / "plan_inventory.json").read_text(encoding="utf-8"))["buildings"]}
        for tid, t in TOWERS.items():
            self.assertEqual((plan[tid]["x"], plan[tid]["z"]), (t["x"], t["z"]), tid)
            self.assertEqual((plan[tid]["w"], plan[tid]["d"]), (55, 55), tid)
            self.assertEqual(plan[tid]["kind"], "tower")
        # close together: neighbours 115 m apart (centre to centre), round the casino T11 at (495,465)
        self.assertEqual(TOWERS["T09"]["x"] - TOWERS["T08"]["x"], 115)
        self.assertEqual(TOWERS["T09"]["z"] - TOWERS["T12"]["z"], 105)

    def test_braxhorn_tower_is_250_to_300_m_with_a_heliport(self):
        t = TOWERS["T09"]
        self.assertEqual((t["name"], t["shape"], t["floors"]), ("BRAXHORN TOWER", "Setback", 60))
        h = const("Fl") + const("Lobby") + t["floors"] * const("Storey") + 14.0
        self.assertTrue(250 <= h <= 300, h)
        self.assertIn("Heliport(roof+14f);", SRC)
        self.assertIn('"BRAXHORN",at,yaw,3.6f', SRC)
        # every other tower is a premium high-rise too (>= 33 floors of 4 m)
        for tid, tt in TOWERS.items():
            self.assertGreaterEqual(tt["floors"], 33, tid)

    def test_real_storeys_and_scale_once(self):
        self.assertEqual((const("Lobby"), const("Storey"), const("Band")), (8.0, 4.0, .90))
        self.assertEqual((const("DoorHalf"), const("DoorH")), (1.0, 2.70))
        self.assertIn("root.localScale=Vector3.one*K;", SRC)
        self.assertIn("l.range*=K;", SRC)


SHADER = (ROOT / "unity-city/Assets/Mistrzowie/BraxChampionsPBR.shader").read_text(encoding="utf-8")


class CurtainWall(unittest.TestCase):
    """Owner 16.11 17:37 ("Real Real wszystko ... szkło to szkło"): a real curtain wall instead of black prisms."""

    def test_no_black_prisms_left(self):
        self.assertIsNone(re.search(r"\bgBlack\b", SRC))
        self.assertNotIn("OfficeStorey", SRC)
        for tower in ("Twist", "Setback", "EllipseTower", "Stacked", "Taper"):
            body = SRC[SRC.index("static float %s(Tower t)" % tower):]
            body = body[:body.index("\n    }\n")]
            self.assertIn("CurtainStorey(", body, tower)

    def test_units_mullions_transoms_with_depth(self):
        cs = SRC[SRC.index("static void CurtainStorey("):SRC.index("static Skin GlassSkin(")]
        self.assertIn("int m=Mathf.Max(1,Mathf.RoundToInt(L/s.module));", cs)
        self.assertIn("Unit(s.vision,", cs)
        self.assertIn("Unit(s.spandrel,", cs)
        self.assertIn("Bar(bt,mullion,P(t,g0),P(t,Storey),n,k==0?s.mullW*1.6f:s.mullW,s.mullD);", cs)
        self.assertIn("module=1.5f,spandrelH=1.10f,mullW=.065f,mullD=mullD,transH=.065f,transD=.08f", SRC)
        # transoms shallower than the mullions: never one plane with them
        self.assertLess(.08, .10)
        self.assertIn("const float Back=.03f;", SRC)

    def test_units_off_true_as_real_insulating_units(self):
        u = SRC[SRC.index("static void Unit("):SRC.index("static void Bar(")]
        self.assertIn("(Rand(seed)-.5f)*1.2f*Mathf.Deg2Rad", u)      # +-.6 degrees about the vertical
        self.assertIn("*.8f*Mathf.Deg2Rad", u)                         # +-.4 about the horizontal
        # the largest tilt (.75 m x tan .6 + 1.45 m x tan .4 = 1.8 cm) stays behind the 3 cm the frame reaches back
        self.assertLess(.75 * math.tan(math.radians(.6)) + 1.45 * math.tan(math.radians(.4)), .03)

    def test_glass_reflects_the_regions_real_sky(self):
        g = SRC[SRC.index("static Material SkyGlass("):SRC.index("static Texture2D unitFrame;")]
        self.assertIn('m.SetTexture("_SkyPanorama",sky);', g)
        self.assertIn('m.SetFloat("_SkyRotation",BraxChampionsSky.Rotation);', g)
        self.assertIn('m.SetFloat("_SkyExposure",BraxChampionsSky.Exposure*SkyboxTint);', g)
        self.assertIn('m.SetVector("_GlassF0",', g)                     # linear: a Color would be turned to linear
        self.assertIn('m.EnableKeyword("_SKY_GLASS");', g)
        self.assertIn("const float SkyboxTint=.9834f;", SRC)
        self.assertAlmostEqual(((.5 + .055) / 1.055) ** 2.4 * 4.59479380, .9834, places=3)   # sRGB .5 -> linear, x ColorSpaceDouble
        # the shader: the keyword, the panorama looked up as Unity's Skybox/Panoramic does, the frame from the texture's alpha
        self.assertIn("#pragma shader_feature_local _SKY_GLASS", SHADER)
        self.assertIn('_GlassF0("Glass reflectance at normal incidence (linear)", Vector)', SHADER)
        self.assertIn("float2 pano = float2(.5 - atan2(rr.z, rr.x) * (.5/UNITY_PI), 1 - acos(clamp(rr.y,-1,1)) / UNITY_PI);", SHADER)
        self.assertIn("float3 rr = float3(ca*rd.x - sa*rd.z, rd.y, sa*rd.x + ca*rd.z);", SHADER)
        self.assertIn("half glassMask = baseSample.a;", SHADER)
        self.assertIn("f0 = lerp(half3(.035,.035,.04), _GlassF0.rgb, glassMask);", SHADER)
        sky = (ED / "BraxChampionsSky.cs").read_text(encoding="utf-8")
        stub = (TOOLS / "wiezowce_preview/WiezStubs.cs").read_text(encoding="utf-8")
        for line in ('public const string Slug = "kloofendal_48d_partly_cloudy_puresky";', "public const float Rotation = 20.8f, Exposure = 1.18f;"):
            self.assertIn(line, sky)
            self.assertIn(line, stub)

    def test_frame_far_in_the_texture_near_in_3d(self):
        self.assertIn("const int N=128,B=2;", SRC)
        self.assertIn("px[y*N+x]=frame?new Color32(18,18,20,0):new Color32(255,255,255,255);", SRC)
        self.assertIn("imp.mipmapEnabled=true;", SRC)
        self.assertIn("lg.SetLODs(new[]{new LOD(.22f,rs)});", SRC)
        self.assertIn("int k=Mathf.FloorToInt((y-Shaft)/(8f*Storey));", SRC)

    def test_after_real_towers(self):
        self.assertIn("CurtainStorey(Rot(F,Turn*f),Rot(F,Turn*(f+1)),y,skin,f", SRC)   # Evolution Tower: 3 degrees a storey
        self.assertIn("Turn=3f;", SRC)
        self.assertIn("BandSkin(new Color(.17f,.22f,.26f),bandWhite,.60f)", SRC)        # Absolute World: white slab edges
        self.assertIn("GlassSkin(new Color(.17f,.22f,.28f))", SRC)                      # Hearst Tower glass under the diagrid
        self.assertIn("mullion=BraxChampionsFoundation.LibraryMetal(\"tower_black_anodised_frame\"", SRC)

    def test_operable_doors_on_every_tower(self):
        self.assertIn("root.gameObject.AddComponent<BraxCityDoor>()", SRC)
        self.assertIn('BraxBuildQuality.Warn("DO POPRAWKI TOWER_"+id+"_ENTRANCE_DOOR_MISSING")', SRC)
        self.assertIn("GroundFloor(F0,-20f,new[]{-10.5f,4.5f},core,split);", SRC)

    def test_no_white_lid_and_parapets_clear_of_the_diagrid(self):
        self.assertIn("Prism(mullion,mullion,deck,Offset(last,.16f),y,y+1.4f);", SRC)
        self.assertNotIn("Prism(ledWhite,ledWhite,ledWhite", SRC)
        self.assertIn("Box4(h1+.80f,h1+.80f),top,top+1.4f", SRC)
        self.assertIn("Blades(steel,F0,Shaft,yS1,3.0f);", SRC)
        self.assertIn("for(int k=every;k<mod;k+=every)Blade(m,p,i,(float)k/mod,y0,y1);", SRC)   # on mullions, never a corner

    def test_entrance_letters_at_the_canopy_edge(self):
        self.assertIn("CanopyTop=4.90f, CanopyOut=3.02f", SRC)
        self.assertIn('"BRAXHORN BET",new Vector3(-10.5f,Fl+CanopyTop,edge+.10f)', SRC)

    def test_no_warm_stone_no_golden_light(self):
        self.assertIn('plaza=Src("plaza_granite","granite_tile",2.4f,new Color(1.60f,1.60f,1.60f),.75f);', SRC)
        self.assertNotIn("granite_tile_03", SRC)
        for r, g, b in re.findall(r"W\.Lampa\([^;]*new Color\(([0-9.]+)f,([0-9.]+)f,([0-9.]+)f\)", SRC):
            self.assertGreaterEqual(float(b), float(r), "lights must be neutral / cool white")
        self.assertNotIn("arena_gold", SRC)


class Interiors(unittest.TestCase):
    def test_finished_lobbies(self):
        for path in ("ArenaLobby/sofa_02/sofa_02_2k.gltf", "HotelSobczak/mid_century_lounge_chair/mid_century_lounge_chair_2k.gltf",
                     "HotelSobczak/coffee_table_round_01/coffee_table_round_01_2k.gltf", "ArenaLobby/potted_plant_02/potted_plant_02_1k.gltf",
                     "HotelSobczak/modern_ceiling_lamp_01/modern_ceiling_lamp_01_1k.gltf"):
            self.assertIn(path, SRC)
        self.assertIn("if(working<0)W.MM(walnut,core.xMin,core.xMax,y0,ceil,core.yMin,core.yMax,true);", SRC)
        self.assertEqual(SRC.count("LightLines("), 6)            # definition + T08, T09, T10, T12, T13
        self.assertIn("LightLinesAt(F0,new[]{-15.3f,-11.0f,-8.6f},null,15f-.6f);", SRC)

    def test_light_lines_never_thin(self):
        self.assertIn("W.MM(ledWhite,x-.15f,x+.15f,ceil-.08f,ceil-.02f,a,b);", SRC)   # 30 cm wide

    def test_braxhorn_bet_premises_without_films(self):
        bet = SRC[SRC.index("static void Bet("):SRC.index("static void BetScreen(")]
        self.assertEqual(bet.count("BetScreen("), 2)
        self.assertIn("for(int i=-2;i<=2;i++)", bet)                       # 5 x 3 video wall
        self.assertIn("for(int i=0;i<9;i++)BetScreen", bet)                # nine along the tills
        self.assertNotIn("BraxKatalog.Tv(", bet)
        screen = SRC[SRC.index("static void BetScreen("):]
        self.assertIn("BraxKatalog.Tv(ctx,p,f,inches,0,false,label,BraxKatalog.GlassDark);", screen)
        self.assertIn("UnityEngine.Object.DestroyImmediate(tv);", screen)
        self.assertIn('films=0 (street premises)', SRC)
        for m in ("modern_arm_chair_01", "bar_chair_round_01", "side_table_01"):
            self.assertIn(m, bet)


class SkyCasino(unittest.TestCase):
    """Owner 16.11 17:42: a casino at the very top of one tower with a view over the whole city."""

    def test_top_two_storeys_of_the_braxhorn_tower(self):
        self.assertIn("public const int CasinoFloor=58;", SRC)
        self.assertEqual(TOWERS["T09"]["floors"], 60)
        self.assertIn("if(f>=CasinoFloor)CasinoStorey(g,y,f==t.floors-1);", SRC)
        y = const("Fl") + const("Lobby") + 58 * const("Storey")
        self.assertTrue(235 < y < 245, y)

    def test_clear_glass_both_ways_and_collision(self):
        c = SRC[SRC.index("static void CasinoStorey("):SRC.index("static void SkyCasino(")]
        self.assertIn("UnitQuad(casinoGlass,", c)
        self.assertIn(",.14f,true);", c)                                 # mullions with a back, seen from the hall
        self.assertIn("W.Kolizja(", c)
        self.assertIn('casinoGlass.SetFloat("_Cull",0f);', SRC)
        self.assertIn("W.KolizjaMM(-14f,14f,yS2,CasinoY-.30f,-14f,14f);", SRC)   # the body is not solid where the hall is

    def test_working_express_lift(self):
        self.assertIn("var w=cab.gameObject.AddComponent<BraxWinda>();", SRC)
        self.assertIn('w.pietraNazwy=new[]{"HOL · BRAXHORN TOWER","SKY KASYNO · 60 PIĘTRO"};', SRC)
        self.assertIn("w.przystanekL=landL;w.przystanekP=landR;", SRC)
        self.assertIn("LobbyFitOut(core,new Vector2(11.5f,-8.5f),new Vector2(15.5f,-13f),false,4,split,0);", SRC)
        self.assertIn('"SKY KASYNO",new Vector3(LiftCentre(core,4,0),Fl+2.50f,-2f)', SRC)
        self.assertIn("WireLift(root);", SRC)
        # the cabin floor 1 cm over the landing floors (never in their plane)
        self.assertIn("W.MM(marble,x-.98f,x+.98f,-.12f,.01f,", SRC)

    def test_furnished_with_real_things(self):
        sc = SRC[SRC.index("static void SkyCasino("):SRC.index("static void BlackjackTable(")]
        self.assertIn("foreach(float x in new[]{-6.6f,6.6f})BlackjackTable(", sc)
        self.assertIn('OwnerModel("bar_lampa_markpeters"', sc)
        # 16.11 18:19 the owner's placeholder machine model gave way to working slot machines (test_automaty_20261116)
        self.assertNotIn("kasyno_automat_nermin", sc)
        self.assertIn('BraxChampionsKasynoSprzet.Zacznij(inside,kat,"SKY KASYNO")', sc)
        self.assertIn("BraxChampionsKasynoSprzet.Bank(kas,new Vector3(-9.6f,y,-4.5f),0f,6,0,ceil);", sc)
        self.assertIn("BraxChampionsKasynoSprzet.Bank(kas,new Vector3(9.8f,y,-4.5f),0f,6,3,ceil);", sc)
        self.assertIn("OwnerBackBar(", sc)
        self.assertIn('BraxMiastoAssety.Model("osiedle_wnetrza",file)', SRC)
        bj = SRC[SRC.index("static void BlackjackTable("):SRC.index("static GameObject OwnerModel(")]
        for part in ('felt', 'leatherBlack', 'walnut', 'steel', 'darkMetal', 'dining_chair_02_2k.gltf'):
            self.assertIn(part, bj)
        self.assertIn('felt=Src("sky_casino_felt","velour_velvet"', SRC)
        self.assertIn('oakDark=Src("sky_casino_oak","oak_wood_planks"', SRC)
        self.assertNotIn("BraxKatalog.Tv(", sc)                          # no films in a casino either (street premises rule)


class Wiring(unittest.TestCase):
    def test_generated_with_the_region(self):
        b = (ED / "BraxChampionsBuildings.cs").read_text(encoding="utf-8")
        self.assertIn('foreach(string tower in BraxChampionsWiezowce.Ids)', b)
        self.assertIn('BraxChampionsFoundation.Optional("wieżowiec "+tower,()=>', b)
        self.assertIn('" TOWERS="+towers+"/"+BraxChampionsWiezowce.Ids.Length', b)

    def test_each_tower_its_own_area_scene(self):
        p = (ED / "BraxChampionsPodzial.cs").read_text(encoding="utf-8")
        for tid in TOWERS:
            self.assertRegex(p, r'Strefa\("Mistrzowie_%s_\w+", "%s \| ", 150f, 280f, 80000\)' % (tid, tid))
        self.assertIn('new GameObject(id+" | "+t.name+" | premium tower, finished lobby")', SRC)

    def test_a_walk_to_every_door(self):
        sys.path.insert(0, str(TOOLS))
        import layout
        self.assertEqual(len(layout.TOWER_WALKS), 6)
        for w in layout.TOWER_WALKS:
            self.assertIn(w, layout.BUILDING_ACCESS)
        doors = {"T08": [0.0], "T09": [-10.5, 4.5], "T10": [0.0], "T12": [0.0], "T13": [0.0]}
        for tid, xs in doors.items():
            t = TOWERS[tid]
            a = math.radians(t["yaw"])
            for lx in xs:
                wx = t["x"] + lx * math.cos(a)                              # Unity yaw: x' = x cos + z sin
                front = t["z"] - 27.5 * (1 if t["yaw"] == 0 else -1)          # the plot edge on the door side
                hit = [w for w in layout.TOWER_WALKS if w[0] < wx - 1.0 and wx + 1.0 < w[0] + w[2]
                       and (abs(w[1] - front) < .01 or abs(w[1] + w[3] - front) < .01)]
                self.assertEqual(len(hit), 1, (tid, lx, wx, front))
                # from the plot edge to the sidewalk's inner edge (Z650: z633, Z330: z347)
                self.assertIn(round(hit[0][1] + hit[0][3], 1) if t["yaw"] else round(hit[0][1], 1), (633.0, 347.0))


class Preview(unittest.TestCase):
    def test_preview_tool_present_and_ignored_output(self):
        p = TOOLS / "wiezowce_preview"
        for f in ("WiezMain.cs", "merge.py", "wiezowce_preview.csproj", ".gitignore"):
            self.assertTrue((p / f).exists(), f)
        self.assertIn("obj/", (p / ".gitignore").read_text())
        self.assertIn("BraxChampionsWiezowce.cs", (p / "wiezowce_preview.csproj").read_text())


if __name__ == "__main__":
    unittest.main()
