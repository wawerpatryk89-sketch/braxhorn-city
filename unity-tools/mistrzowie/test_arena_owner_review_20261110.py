"""Owner 9.11 23:43-23:46 on the arena (APK 35 on the phone): "ona nie jest w ogóle, nic nie jest wykończone. Szyby
miały być przyciemnione z zewnątrz. Mural jest ucięty, ten pierwszy. Dwóch murali kolejnych nie ma ... mają być
wszystkie 3. W środku ma być wykończone wszystko. Ten pas niech wisi na ścianie." / "co ten szyld robi w środku, ten
mój z zewnątrz ... z drugiej strony też jest szyld, złoty" / "ściany mają być wykończone, te trybuny z boku,
wszystko. Sufit tak samo." and 10.11 00:37 "Wszystko, co złote, ma być złote"."""
import re
import subprocess
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"
TOOLS = ROOT / "unity-tools/mistrzowie"


def src(name):
    return (ED / name).read_text(encoding="utf-8")


class ArenaOwnerReview20261110(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.arena, cls.mural, cls.side = src("BraxChampionsArena.cs"), src("BraxChampionsArenaMural.cs"), src("BraxChampionsArenaSideMurals.cs")
        cls.printer, cls.signs, cls.front = src("BraxChampionsArenaMuralPrint.cs"), src("BraxChampionsArenaSigns.cs"), src("BraxChampionsArenaFront.cs")
        cls.belt = src("BraxChampionsBeltShowcase.cs")
        cls.workflow = (ROOT / ".github/workflows/mistrzowie-apk.yml").read_text(encoding="utf-8")

    def test_all_three_murals_use_one_backlit_whole_print(self):
        # owner 16.11 13:52-13:53: "to dzielone ... dajesz nowe co ci dałem" - the owner's whole picture, never split
        self.assertNotIn("TileCount", self.printer)
        self.assertNotIn("_TILE", self.printer)
        self.assertIn('Quad(group,name+" | owner picture 6144x2048 whole",id+"_whole"', self.printer)
        self.assertIn("split=NO tangents=YES", self.printer)
        # back-lit: the picture is its own light, independent of the dim hall ambient
        glow = float(re.search(r"Glow=([\d.]+)f", self.printer).group(1))
        self.assertGreaterEqual(glow, .8)
        self.assertIn('m.SetTexture("_EmissionMap",picture);', self.printer)
        # a loud FAIL only when the owner source itself is missing; never a substitute
        self.assertIn("NO substitute image", self.printer)
        self.assertEqual(self.mural.count("BraxChampionsArenaMuralPrint.Hang("), 1)
        # owner 10.11 13:20 ("czemu zrobiłeś duplikaty?"): each mural once - LEFT and RIGHT side walls only
        self.assertEqual(self.side.count("BraxChampionsArenaMuralPrint.Hang("), 1)
        self.assertNotIn("static bool End(", self.side)
        self.assertIn("bool left=Side(originalBowl,PickRequiredHd(LeftFightersHd", self.side)
        self.assertIn("bool right=Side(originalBowl,PickRequiredHd(RightBeltHd", self.side)

    def test_ci_no_longer_splits_the_murals(self):
        self.assertNotIn("python3 unity-tools/mistrzowie/split_arena_murals.py", self.workflow)
        # the owner originals are still staged and checked byte for byte (his attachments have the same SHA-256)
        self.assertIn("python3 unity-tools/mistrzowie/install_arena_murals_hd.py", self.workflow)
        self.assertIn("python3 unity-tools/mistrzowie/verify_arena_three_murals.py", self.workflow)
        tracked = subprocess.run(["git", "ls-files", "unity-city/Assets/Source/Mistrzowie/ArenaMuralsHD"],
                                 cwd=ROOT, capture_output=True, text=True).stdout
        self.assertNotIn("_TILE", tracked)

    def test_mural_panel_never_takes_the_black_derivative_path(self):
        # the panel has real tangents and white colours -> the shader's supplied-tangent branch
        self.assertIn("mesh.RecalculateTangents();", self.printer)
        self.assertIn("mesh.tangents=new[]{tan,tan,tan,tan};", self.printer)
        self.assertIn("mesh.colors=new[]{Color.white,Color.white,Color.white,Color.white};", self.printer)
        # and the region shader's derivative frame is built in full precision (no 1e-12 floor that mediump flushes to 0)
        shader = (ROOT / "unity-city/Assets/Mistrzowie/BraxChampionsPBR.shader").read_text(encoding="utf-8")
        self.assertNotIn("rsqrt(max(max(dot(t,t),dot(b,b)), 1e-12))", shader)
        self.assertIn("float inv = largest > 0.0 ? 1.0 / largest : 0.0;", shader)
        self.assertIn("float frameScale = rsqrt(max(max(dot(tF,tF),dot(bF,bF)), 1.0));", shader)

    def test_entrance_mural_is_whole_above_the_door(self):
        w = float(re.search(r"MuralWidth=([\d.]+)f", self.mural).group(1))
        h = float(re.search(r"MuralHeight=([\d.]+)f", self.mural).group(1))
        b = float(re.search(r"MuralBottom=([\d.]+)f", self.mural).group(1))
        self.assertAlmostEqual(w / h, 3.0, places=6)
        self.assertGreater(b - .16, 4.20 + .18)                 # frame above the door's gold portal
        self.assertLess(b + h + .16, 27.35 - .30 - .165)        # under the lobby roof
        self.assertIn("WHOLE_ABOVE_DOOR=true doorCut=false", self.mural)
        self.assertNotIn("atmospheric edge", self.mural)
        # owner 12.11 12:06: "murale bez żadnych ramek" - no frame round any mural
        self.assertNotIn("BraxChampionsArenaMuralPrint.Frame(", self.mural)

    def test_every_wall_ceiling_and_stand_side_is_finished(self):
        for needle in ("BoxMinMax(marble,a0,a1,yF,WallPlinthTop,g0,g1,true);",
                       "BoxMinMax(wallLeather,a0,a1,WallPlinthTop,WallPanelTop,g0,g1,true);",
                       "BoxMinMax(acoustic,a0,a1,WallPanelTop,yC,g0,g1,true);",
                       "HallCeiling(X0,X1,Z0,Z1,cx,cz,acoustic,beamBlack,warmLine,interiorGold);",
                       "LobbyWallMarble(X0,X1,Z1,yF,dA,dB,dH,lobbyMarble,interiorGold,warmLine);",
                       "void StandSide(bool alongX,float front,float s,float edge,float into,int rows)",
                       "BackBox(marble,yF,StandStoneTop);BackBox(marble,StandStoneTop+.06f,topAll);",
                       "// curved back of the corner bowl"):
            self.assertIn(needle, self.arena)
        self.assertNotIn("BoxMinMax(wallConcrete", self.arena)             # no bare concrete wall left
        self.assertEqual(self.arena.count("StandSide(true,"), 2)            # 2 calls x 2 sides = four aisle faces
        # the designed ceiling hangs under the exterior roof slab (underside 26.885 in bowl space)
        ceiling = float(re.search(r"CeilingY=([\d.]+)f", self.arena).group(1))
        self.assertLess(ceiling + .04, 26.885)
        self.assertGreater(ceiling - .62, 2.65 + 70 / 3)                   # beams stay above the side murals
        self.assertIn('"arena_lobby_wall_black_marble"', self.arena)

    def test_signs_red_outside_none_inside_and_dark_glass(self):
        self.assertIn("Curved(t,1,MakeMat(1,0),true,true,36f,11.42f,6f);", self.signs)
        self.assertIn("Curved(t,3,MakeMat(3,2),false,false,34f,11.7f,5.667f);", self.signs)
        self.assertNotIn("Curved(t,1,MakeMat(1),", self.signs)
        self.assertNotIn("\n        HallBanners(t,body);", self.signs)
        alpha = float(re.search(r'arenaGlass\.SetColor\("_Color",new Color\([^)]*,(\.\d+)f\)\)', self.front).group(1))
        # owner 10.11 00:48 "lekko przyciemnione", superseded on the phone (APK 37) at 12:28: "tylko i wyłącznie drzwi
        # mają być jasne ze szkła ... a naokoło ta szyba ma być czarna" and 12:50 "zakaz szarego ... albo czarne"
        self.assertGreaterEqual(alpha, .90)
        self.assertIn("doors=clear_glass_leaves_only", self.front)

    def test_belt_hangs_on_the_wall(self):
        self.assertNotIn('"A1 VITRINE | black marble pillar"', self.belt)
        self.assertIn('"A1 VITRINE | wall back panel"', self.belt)
        self.assertIn('"A1 VITRINE | gold wall frame side"', self.belt)
        self.assertIn("WallMuralFaceZ=63.3f-(BraxChampionsArena.FrontWallLobbyFaceZ+BraxChampionsArena.LobbyMarbleDepth);", self.belt)

    def test_gold_is_gold_everywhere(self):
        self.assertIn("BraxChampionsGold.Apply(interiorGold);", self.arena)
        self.assertIn("BraxChampionsGold.Apply(vipGold);", self.arena)
        self.assertIn("BraxChampionsGold.Apply(gold);", self.mural)
        self.assertIn("BraxChampionsGold.Apply(FrameMaterial(", self.printer)
        self.assertIn("BraxChampionsGold.Apply(SolidPbr(\"A1_belt_showcase_clean_gold\"", self.belt)
        buildings = src("BraxChampionsBuildings.cs")
        self.assertIn('BraxChampionsGold.Apply(Surface("arena_premium_gold"', buildings)

    def test_less_gold_in_the_hall(self):
        # owner 10.11 15:22-15:31 (APK photo): "te pionowe kreski ... na tym pikowanym ... wszystkie w pizdu", "najebane
        # tego złota"; asked which hall gold goes: "Poręcze schodów trybun". The lobby (mural, trophies) is unchanged.
        self.assertNotIn("for(float a=a0+3.30f;a<a1-.30f;a+=3.30f)", self.arena)
        self.assertNotIn("35mm physical gold divisions", self.arena)
        self.assertIn('var railBlack = Plain("trybuny poręcze czarna stal satyna"', self.arena)
        self.assertIn("W.Rura(railBlack,foot,head,.022f,8);", self.arena)
        self.assertNotIn("W.Rura(interiorGold,foot,head", self.arena)

    def test_preview_harness_exists(self):
        for f in ("arena_preview.csproj", "ArenaMain.cs", "ArenaStubs.cs", "run_preview.sh", "views.json", "README.md"):
            self.assertTrue((TOOLS / "arena_preview" / f).exists(), f)


if __name__ == "__main__":
    unittest.main()
