"""Owner 9.11 23:36-23:49, casino T11 exterior: "Szyldu nie ma nadal ... zrób zajebisty szyld, złoty", "na wjeździe do
garażu ... jakaś tabliczka stoi", "Tabliczka na środku wjazdu ma wypierdalać", "Wjazd do parkingu poprawić, kostkę
zobaczyć, czy jest równo", "od teraz renderowane zdjęcia przed buildem"."""
import subprocess
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"
TOOLS = ROOT / "unity-tools/mistrzowie"


class CasinoExterior20261109(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.casino = (ED / "BraxChampionsCasinoT11.cs").read_text(encoding="utf-8")
        cls.sign = (ED / "BraxChampionsCasinoT11Sign.cs").read_text(encoding="utf-8")
        cls.script = (TOOLS / "build_casino_sign.py").read_text(encoding="utf-8")
        cls.workflow = (ROOT / ".github/workflows/mistrzowie-apk.yml").read_text(encoding="utf-8")

    def test_gold_3d_sign_replaces_the_inward_facing_picture(self):
        # 10.11 00:22 "nie jest premium": a full composition (crown, card suits, back-lit letters, marquee bulbs)
        self.assertIn("wawer = mirror_for_unity(casino())", self.script)
        for part in ("'WAWER'", "'K A S Y N O'", "crown", "spade(", "heart(", "diamond(", "club("):
            self.assertIn(part, self.script)
        self.assertIn('Place3DSign("WAWER_kasyno",signRoot,"T11 3D gold KASYNO WAWER sign | rotunda",new Vector3(0,10.735f,-36.29f),180f,.93f)', self.casino)
        self.assertIn("SignMarquee();", self.casino)
        self.assertIn('BeveledBox(signRoot,"T11 sign marquee warm bulb",bulb', self.sign)
        # real yellow gold on every glyph face (owner photo of gold bars), not a brown mirror
        self.assertIn('BraxChampionsGold.Apply(signGold);', self.sign)
        self.assertIn('BraxChampionsGold.Apply(signGoldDeep,true);', self.sign)
        self.assertIn('m.role=="light"?led:m.role=="dark"?signBacking:m.role=="accent"?signGoldDeep:signGold', self.sign)
        self.assertIn("mesh.RecalculateNormals()", self.sign)

    def test_sign_fits_the_carrier(self):
        # composition box x +-6.6, y -.38..3.10 at .93 on a carrier 12.0 +- 1.82 high and +-8.42 wide
        lo, hi = 10.735 - .38 * .93, 10.735 + 3.10 * .93
        self.assertGreater(lo, 12.0 - 1.82 + .08)
        self.assertLess(hi, 12.0 + 1.82 - .08)
        self.assertLess(6.6 * .93, 8.42 - 1.5)
        # the glyph backs stand in front of the carrier front (-36.26) and its gold ring front (-36.285)
        self.assertLess(-36.29, -36.2725 - .0125)

    def test_sign_bytes_are_generated_in_ci_and_never_committed(self):
        self.assertIn("python3 unity-tools/mistrzowie/build_casino_sign.py", self.workflow)
        self.assertLess(self.workflow.index("build_signs.py"), self.workflow.index("build_casino_sign.py"))
        tracked = subprocess.run(["git", "ls-files", "unity-city/Assets/Source/Mistrzowie/CasinoWawerSign.bytes"],
                                 cwd=ROOT, capture_output=True, text=True).stdout.strip()
        self.assertEqual(tracked, "")
        self.assertIn("T11_3D_SIGN_MISSING", self.sign)   # a missing file is reported, the PNG fallback faces the street

    def test_board_in_the_driveway_is_gone_and_portal_has_a_3d_parking_sign(self):
        self.assertNotIn('RoadsideOriginalSign("TAXI"', self.casino)
        self.assertIn("GarageSign();", self.casino)
        self.assertIn('Place3DSign("PARKING_garage",signRoot', self.sign)
        self.assertIn("'PARKING'", self.script)
        for label in ("Garage portal graphite sign fascia", "Garage portal graphite canopy over ramp head",
                      "Garage portal canopy warm downlight line", "Garage portal pier warm light reveal",
                      "Ramp WEST wall dark stone coping", "Ramp EAST wall dark stone coping",
                      "Ramp WEST champagne guard rail", "Ramp EAST champagne guard rail"):
            self.assertIn(label, self.casino + self.sign)

    def test_driveway_is_clear_of_kerb_planter_and_bollards(self):
        # the right-hand front kerb ran across the driveway mouth x 39.55..48.9
        self.assertIn('ConcreteCurb("Front driveway NORTH concrete curb",21.875f,-37.41f,35.35f,.18f)', self.casino)
        self.assertLessEqual(21.875 + 35.35 / 2, 39.55 + 1e-6)
        self.assertIn('"Garage driveway WEST concrete curb"', self.casino)
        self.assertIn('"Garage driveway EAST concrete curb"', self.casino)
        # planter + shrub 3.4 m wide stood at x 38 = 15 cm inside the asphalt
        self.assertEqual(self.casino.count("side>0&&z<0?37.6f:side*43f"), 2)   # planter and its shrubs
        self.assertLess(37.6 + 3.4 / 2, 39.37)
        # east light bollards stood at x 48 = on the driveway and in the air over the ramp
        self.assertNotIn("led,side*48f,.62f", self.casino)
        self.assertIn("side>0?49.41f:-48f", self.casino)

    def test_owner_review_of_the_render_10_11(self):
        hall = (ED / "BraxChampionsCasinoT11Hall.cs").read_text(encoding="utf-8")
        facade = (ED / "BraxChampionsCasinoT11Facade.cs").read_text(encoding="utf-8")
        gold = (ED / "BraxChampionsGold.cs").read_text(encoding="utf-8")
        # "A co te pasy tam robią?" - zebra stripes gone
        self.assertNotIn("White zebra marking on asphalt", self.casino)
        # "Wejście do kasyna ma być otwarte ... a nie, że tam jakaś ściana stoi": real portal, real hall
        self.assertIn("W.MM(graphite, -PortalHalf, PortalHalf, PortalTop, 13.9f, -24.2f, -24.0f, true);", facade)
        self.assertNotIn("W.MM(interiorBack", facade)
        self.assertIn('Optional("kasyno T11: otwarta sala kasyna",CasinoHall);', self.casino)
        self.assertIn("W.KolizjaMM(-HallX,HallX,.05f,HallFloor,HallZ0,HallZ1);", hall)
        self.assertEqual(hall.count("W.Lampa("), 1)          # one call in a 2 x 3 loop: six vertex lamps
        # "może być czerwona, ale płytka": red marble tiles, no carpet
        self.assertIn('LibraryMarbleWhite("t11_hall_red_marble_tiles"', hall)
        self.assertNotIn("floral_jacquard", hall)
        # "jedno poprawiłeś, a drugie zepsułeś": no glowing board behind the glass
        self.assertNotIn('" interior warm backdrop"', facade)
        # "wejście zlane z szybą": gold portal, canopy, framed gold door leaves
        for label in ("T11 entrance portal solid gold jamb", "T11 entrance portal solid gold lintel",
                      "T11 entrance canopy black body", "Gold door leaf top rail", "Gold door leaf meeting stile"):
            self.assertIn(label, self.casino)
        # "A gdzie jest ten dolar złoty" / "na środku" / "jasne złoto, świecące się"
        self.assertIn("const bool RoofDollarOnOwnerBoard = true;", facade)
        self.assertIn("const float x=0f,z=-21f,roofY=14.40f,nominalHeight=15.0f;", self.casino)
        self.assertIn("mats[i]=dollarGold;", self.casino)
        # "Wszystko, co złote, ma być złote": one yellow gold, half-metallic, self-lit
        self.assertIn("Colour=new Color(1f,.80f,.20f)", gold)
        self.assertIn("public const float Metallic=.55f, Roughness=.20f;", gold)
        self.assertIn("BraxChampionsGold.Apply(gold);", self.casino)
        self.assertIn("BraxChampionsGold.Apply(pillarGold);", self.casino)
        # four shrubs fill each planter, gold rim
        self.assertIn('"Planter champagne gold rim"', self.casino)

    def test_preview_harness_exists(self):
        for f in ("casino_preview.csproj", "CasinoMain.cs", "CasinoStubs.cs", "run_preview.sh", "views.json", "mesh2quads.py", "README.md"):
            self.assertTrue((TOOLS / "casino_preview" / f).exists(), f)
        self.assertTrue((TOOLS / "apartments_preview" / "MeshDump.cs").exists())


if __name__ == "__main__":
    unittest.main()
