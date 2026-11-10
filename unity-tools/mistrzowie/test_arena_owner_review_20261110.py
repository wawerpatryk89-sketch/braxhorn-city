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

    def test_all_three_murals_use_one_backlit_tiled_print(self):
        self.assertIn("public const int TileCount=3;", self.printer)
        self.assertIn('TilePath(string source,int i)=>source.Substring(0,source.Length-4)+"_TILE"+i+".png"', self.printer)
        # back-lit: the picture is its own light, independent of the dim hall ambient
        glow = float(re.search(r"Glow=([\d.]+)f", self.printer).group(1))
        self.assertGreaterEqual(glow, .8)
        self.assertIn('m.SetTexture("_EmissionMap",picture);', self.printer)
        # never an empty wall: whole-picture fallback, and a loud FAIL only when the owner source itself is missing
        self.assertIn("whole 6144x2048 picture on one panel", self.printer)
        self.assertIn("NO substitute image", self.printer)
        self.assertEqual(self.mural.count("BraxChampionsArenaMuralPrint.Hang("), 1)
        self.assertEqual(self.side.count("BraxChampionsArenaMuralPrint.Hang("), 1)   # called for LEFT and RIGHT
        self.assertIn("bool left=Side(originalBowl,PickRequiredHd(LeftFightersHd", self.side)
        self.assertIn("bool right=Side(originalBowl,PickRequiredHd(RightBeltHd", self.side)

    def test_tiles_are_a_lossless_crop_made_in_ci(self):
        script = (TOOLS / "split_arena_murals.py").read_text(encoding="utf-8")
        for sha in ("5d30aa995bb0da043a34c282fb452743f1b04b9f4584abf2be925ea29a461a73",
                    "0805d4170b696236dba7a37c89fc4415ee21a87b316c1d58734bac212cb11a2c",
                    "b134858b5ba16dc85089642eb1f7432ef5c76e036a8d313805947923b7661304"):
            self.assertIn(sha, script)
        self.assertIn("check.convert(\"RGB\").tobytes() != tile.tobytes()", script)
        self.assertIn("python3 unity-tools/mistrzowie/split_arena_murals.py", self.workflow)
        self.assertLess(self.workflow.index("pillow=="), self.workflow.index("split_arena_murals.py"))
        tracked = subprocess.run(["git", "ls-files", "unity-city/Assets/Source/Mistrzowie/ArenaMuralsHD"],
                                 cwd=ROOT, capture_output=True, text=True).stdout
        self.assertNotIn("_TILE", tracked)

    def test_tile_split_keeps_every_pixel(self):
        try:
            from PIL import Image
        except ImportError:
            self.skipTest("Pillow not installed")
        import importlib.util, tempfile, hashlib
        spec = importlib.util.spec_from_file_location("split_arena_murals", TOOLS / "split_arena_murals.py")
        mod = importlib.util.module_from_spec(spec); spec.loader.exec_module(mod)
        with tempfile.TemporaryDirectory() as d:
            picture = Image.radial_gradient("L").resize((6144, 2048)).convert("RGB")
            picture.putpixel((2047, 5), (255, 0, 0)); picture.putpixel((2048, 5), (0, 255, 0)); picture.putpixel((6143, 2047), (0, 0, 255))
            name = "MURAL_TEST_6144x2048.png"; picture.save(Path(d) / name)
            mod.SOURCES = {name: hashlib.sha256((Path(d) / name).read_bytes()).hexdigest()}
            self.assertEqual(len(mod.split(d)), 3)
            tiles = [Image.open(Path(d) / mod.tile_name(name, i)).convert("RGB") for i in range(3)]
            self.assertEqual([t.size for t in tiles], [(2048, 2048)] * 3)
            self.assertEqual(tiles[0].getpixel((2047, 5)), (255, 0, 0))
            self.assertEqual(tiles[1].getpixel((0, 5)), (0, 255, 0))
            self.assertEqual(tiles[2].getpixel((2047, 2047)), (0, 0, 255))
            joined = b"".join(t.tobytes() for t in tiles)
            self.assertEqual(len(joined), 6144 * 2048 * 3)

    def test_entrance_mural_is_whole_above_the_door(self):
        w = float(re.search(r"MuralWidth=([\d.]+)f", self.mural).group(1))
        h = float(re.search(r"MuralHeight=([\d.]+)f", self.mural).group(1))
        b = float(re.search(r"MuralBottom=([\d.]+)f", self.mural).group(1))
        self.assertAlmostEqual(w / h, 3.0, places=6)
        self.assertGreater(b - .16, 4.20 + .18)                 # frame above the door's gold portal
        self.assertLess(b + h + .16, 27.35 - .30 - .165)        # under the lobby roof
        self.assertIn("WHOLE_ABOVE_DOOR=true doorCut=false", self.mural)
        self.assertNotIn("atmospheric edge", self.mural)
        self.assertIn("BraxChampionsArenaMuralPrint.Frame(walls,\"Entrance\"", self.mural)

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

    def test_preview_harness_exists(self):
        for f in ("arena_preview.csproj", "ArenaMain.cs", "ArenaStubs.cs", "run_preview.sh", "views.json", "README.md"):
            self.assertTrue((TOOLS / "arena_preview" / f).exists(), f)


if __name__ == "__main__":
    unittest.main()
