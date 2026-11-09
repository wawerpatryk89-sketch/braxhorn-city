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
        self.assertIn("build('WAWER', 'kasyno')", self.script)
        self.assertIn("mirror_for_unity", self.script)
        self.assertIn('Place3DSign("WAWER_kasyno",signRoot,"T11 3D gold KASYNO WAWER sign | rotunda",new Vector3(0,10.855f,-36.29f),180f,1.05f)', self.casino)
        # solid gold on every glyph face, bright in shade: half-metallic + faint warm glow, not a brown mirror
        self.assertIn('LibraryMetal("T11_3D_sign_bright_gold",new Color(1f,.80f,.42f))', self.sign)
        self.assertIn('signGold.SetFloat("_Metallic",.62f)', self.sign)
        self.assertIn('signGold.SetColor("_EmissionColor",new Color(.22f,.15f,.05f))', self.sign)
        self.assertIn('m.role=="light"?led:m.role=="dark"?signBacking:m.role=="accent"?signGoldDeep:signGold', self.sign)
        self.assertIn("mesh.RecalculateNormals()", self.sign)

    def test_sign_fits_the_carrier(self):
        # glyph box x +-5.02, y -.53..2.71 at 1.05 on a carrier 12.0 +- 1.82 high and +-8.42 wide
        lo, hi = 10.855 - .53 * 1.05, 10.855 + 2.71 * 1.05
        self.assertGreater(lo, 12.0 - 1.82 + .08)
        self.assertLess(hi, 12.0 + 1.82 - .08)
        self.assertLess(5.02 * 1.05, 8.42 - 1.5)
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
        self.assertEqual(self.casino.count("side>0&&z<0?37.6f:side*43f"), 2)
        self.assertLess(37.6 + 3.4 / 2, 39.37)
        # east light bollards stood at x 48 = on the driveway and in the air over the ramp
        self.assertNotIn("led,side*48f,.62f", self.casino)
        self.assertIn("side>0?49.41f:-48f", self.casino)

    def test_preview_harness_exists(self):
        for f in ("casino_preview.csproj", "CasinoMain.cs", "CasinoStubs.cs", "run_preview.sh", "views.json", "mesh2quads.py", "README.md"):
            self.assertTrue((TOOLS / "casino_preview" / f).exists(), f)
        self.assertTrue((TOOLS / "apartments_preview" / "MeshDump.cs").exists())


if __name__ == "__main__":
    unittest.main()
