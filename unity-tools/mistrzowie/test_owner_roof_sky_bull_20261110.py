"""Owner 10.11 19:09-19:22 (photos from the Hotel Sobczak roof): the roof sign hung in the air, no sky ("nieba brak",
"panorama realistyczna HDR 360"), the BRAXHORN bull on the A1 roof, E1 by the promenade."""
import json
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"
TOOLS = Path(__file__).resolve().parent


class OwnerRoofSkyBull(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.hotel = (ED / "BraxChampionsHotelSobczakT03.cs").read_text()
        cls.sky = (ED / "BraxChampionsSky.cs").read_text()
        cls.explorer = (ED / "BraxChampionsExplorer.cs").read_text()
        cls.bull = (ED / "BraxChampionsArenaRoofBull.cs").read_text()
        cls.buildings = (ED / "BraxChampionsBuildings.cs").read_text()
        cls.prepare = (TOOLS / "prepare_assets.py").read_text()
        cls.workflow = (ROOT / ".github/workflows/mistrzowie-apk.yml").read_text()

    def test_no_roof_signs(self):
        # owner 10.11 20:09 (photo of the facade sign): "ten u góry jest niepotrzebny w ogóle, z obu stron"
        for gone in ("roof front", "roof back", "RoofSignLegs", "RoofSignFrames", "SignLeg"):
            self.assertNotIn(gone, self.hotel)
        sign = self.hotel[self.hotel.index("    static int Sign()"):]
        sign = sign[:sign.index("\n    }\n") + 6]
        self.assertIn('(name:"facade"', sign)
        self.assertIn('(name:"reception wall"', sign)
        self.assertNotIn("Box(", sign)                                   # nothing added to W after W.Zapisz

    def test_roof_finishes_are_what_brax_stands_on(self):
        roof = self.hotel[self.hotel.index("    static void Roof()"):self.hotel.index("    static void Lounger(")]
        self.assertIn("Box(M.paving,-e,e,y,y+.02f,-f,TechZ0,true);", roof)
        self.assertIn("Box(M.teakDark,-15.2f,15.2f,y+.02f,y+.045f,-15.0f,-9.4f,true);", roof)

    def test_hdr_360_sky(self):
        self.assertIn("SKY_HDRIS=('kloofendal_48d_partly_cloudy_puresky',)", self.prepare)
        self.assertIn("metadata['hdri']['4k']['hdr']", self.prepare)
        self.assertIn('Shader.Find("Skybox/Panoramic")', self.sky)
        self.assertIn("TextureImporterFormat.ASTC_HDR_6x6", self.sky)
        self.assertIn("Rotation = 20.8f", self.sky)
        self.assertIn("var hdrSky = BraxChampionsSky.Create();", self.explorer)
        # the HDRI sun (u .595, elevation 47.9) on the region sun Euler(48,-35): azimuth -55.0 -> rotation 20.8
        import math
        fwd = (math.sin(math.radians(-35)) * math.cos(math.radians(48)), -math.sin(math.radians(48)),
               math.cos(math.radians(-35)) * math.cos(math.radians(48)))
        sun_az = math.degrees(math.atan2(-fwd[2], -fwd[0]))
        hdri_az = math.degrees((0.5 - 0.595) * 2 * math.pi)
        self.assertAlmostEqual(hdri_az - sun_az, 20.8, delta=.3)
        self.assertIn("unity-city/Assets/Source/Mistrzowie/Sky/", (ROOT / ".gitignore").read_text())

    def test_arena_roof_bull(self):
        import build_arena_roof_bull as b
        sign, plates, band = b.build()
        self.assertGreaterEqual(len(plates), 5)
        xs = [p.bounds for p in plates]
        width = max(x[2] for x in xs) - min(x[0] for x in xs)
        self.assertAlmostEqual(width, b.WIDTH, delta=1.0)
        self.assertEqual({m["role"] for m in sign["meshes"]}, {"steel", "steel_edge", "led"})
        # fits the A1 roof (ellipse 96 x 59 m) after the 90 deg turn (logo width along z)
        h = max(x[3] for x in xs) - min(x[1] for x in xs)
        for X, Z in ((h / 2 + 1, b.WIDTH / 2 + 1),):
            self.assertLess((X / 96) ** 2 + (Z / 59) ** 2, 1)
        self.assertIn("python3 unity-tools/mistrzowie/build_arena_roof_bull.py", self.workflow)
        self.assertIn('()=>BraxChampionsArenaRoofBull.Generate(root));', self.buildings)
        self.assertIn("RoofTop = 27.35f", self.bull)
        self.assertNotIn("cv2", (TOOLS / "build_arena_roof_bull.py").read_text())    # CI has numpy/shapely/pillow/mpl only

    def test_e1_by_the_promenade(self):
        e1 = next(b for b in json.loads((TOOLS / "plan_inventory.json").read_text())["buildings"] if b["id"] == "E1")
        self.assertEqual((e1["x"], e1["z"], e1["w"], e1["d"], e1["height"]), (530, -13, 120, 50, 15))
        self.assertGreaterEqual(e1["z"] - e1["d"] / 2, -53)                # behind the sidewalk of road Z-70
        from audit_layout import audit
        self.assertEqual(audit(json.loads((TOOLS / "plan_inventory.json").read_text())["buildings"]), [])


if __name__ == "__main__":
    unittest.main()
