"""Owner 17.11.2026 ("Na wzgórzu jedna, druga mała wyspa z willą. Trzecia gdzieś tam"): the three villas for sale on their
own land - WILLA 1 on the hill north of the arena A1 (a private road from road Z935), WILLA 2 on a small island in the bay
(a jetty for the boats), WILLA 3 on the seafront east of the promenade (a private road from road X1130)."""
import math
import pathlib
import sys
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "unity-tools/mistrzowie"))
import layout  # noqa: E402

ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"
SITES = (ED / "BraxChampionsWilleDzialki.cs").read_text(encoding="utf-8")
HORIZON = (ED / "BraxChampionsHorizon.cs").read_text(encoding="utf-8")
BUILDINGS = (ED / "BraxChampionsBuildings.cs").read_text(encoding="utf-8")
OFFICE = (ED / "BraxChampionsKasynoBiuro.cs").read_text(encoding="utf-8")
LEGACY = (ROOT / "unity-tools/mistrzowie/legacy_foundation.py").read_text(encoding="utf-8")


class Sites(unittest.TestCase):
    def test_the_three_places(self):
        self.assertIn('nr = 1, nazwa = "WZGÓRZE", rodzaj = Rodzaj.Wzgorze, srodek = new Vector2(650f, 1190f), yaw = 0f, padY = 24f', SITES)
        self.assertIn('nr = 2, nazwa = "WYSPA", rodzaj = Rodzaj.Wyspa, srodek = new Vector2(1080f, -400f), yaw = 180f, padY = 3.5f', SITES)
        self.assertIn('nr = 3, nazwa = "NABRZEŻE", rodzaj = Rodzaj.Nabrzeze, srodek = new Vector2(1272f, -98f), yaw = 0f, padY = 3.0f', SITES)
        # the island in the region's own sea, clear of the marina (its piers end at z -266.8) and 250 m off the quay (z -150)
        self.assertIn("x0 = 950f, x1 = 1210f, z0 = -530f, z1 = -270f, woda = true", SITES)
        self.assertGreater(-150 - (-400), 240)
        self.assertIn('BraxChampionsFoundation.Optional("wille na sprzedaż: wzgórze, wyspa, nabrzeże",()=>BraxChampionsWilleDzialki.Zbuduj(null));', BUILDINGS)

    def test_roads_leave_the_city_by_street_mouths(self):
        self.assertEqual(layout.WILLE_DRIVE_MOUTHS, [("WILLA1", 594.0, 945.0, 607.0, 945.0), ("WILLA3", 1140.0, -3.0, 1140.0, 10.0)])
        for r in [(597.0, 945.0, 7.0, 7.0), (597.0, 952.0, 7.0, 48.0), (1140.0, 0.0, 7.0, 7.0), (1147.0, 0.0, 53.0, 7.0)]:
            self.assertIn(r, layout.MOUTH_CUTS)
        for r in layout.WILLE_DRIVE_MOUTHS:
            self.assertIn(r, layout.DRIVE_MOUTHS)
        self.assertIn(" if any(inside((x,z),r) for r in MOUTH_CUTS):return None", LEGACY)
        self.assertIn("u0 = 597f, u1 = 604f, R = 3f, depth = 7f, topEnd = .16f", SITES)
        self.assertIn("alongX = false, edge = 1140f, dir = 1f,\n            u0 = 0f, u1 = 7f, R = 3f, depth = 7f, topEnd = .16f", SITES)

    def test_the_hill_road_grade(self):
        # straight 259 m, R 25 quarter, 13.5 m, R 20 quarter; flat 48 m in the region, then up to the pad (24 m)
        length = (1211 - 952) + math.pi / 2 * 25 + (639 - 625.5) + math.pi / 2 * 20
        self.assertAlmostEqual(length, 343.2, delta=.5)
        self.assertIn("new[] { (0f, .16f), (48f, .16f), (298f, 24f) }, 24f);", SITES)
        grade = (24 - .16) / (298 - 48)
        self.assertLess(grade, .10)                                          # the hill roads of Los Angeles: 10-15 %
        self.assertIn("new[] { (0f, .16f), (53f, .16f), (198.5f, 3.0f) }, 3.0f);", SITES)

    def test_the_road_ends_square_at_the_gate(self):
        # the hill's road ends on an R 20 arc: its last chord (32 steps, 2.8 deg) is 1.4 deg off the tangent - the end built
        # on the chord pushed an 8 cm sliver of asphalt over the drive's paving (z-fight); 1.5 c1 - 0.5 c2 is the tangent
        self.assertIn("var dir = Kierunek(p, i);", SITES)
        self.assertIn("return (1.5f * c1.normalized - .5f * c2.normalized).normalized;", SITES)
        n = math.ceil(math.radians(90) * 20)
        pts = [(639 + 20 * math.cos(math.radians(90 - 90 * i / n)), 1216 + 20 * math.sin(math.radians(90 - 90 * i / n))) for i in range(n + 1)]
        def unit(a, b):
            d = (b[0] - a[0], b[1] - a[1]); l = math.hypot(*d); return (d[0] / l, d[1] / l)
        c1, c2 = unit(pts[-2], pts[-1]), unit(pts[-3], pts[-2])
        t = (1.5 * c1[0] - .5 * c2[0], 1.5 * c1[1] - .5 * c2[1])
        self.assertLess(abs(math.degrees(math.atan2(t[0], -t[1]))), .01)    # due south into the gate at z 1216
        self.assertGreater(abs(math.degrees(math.atan2(c1[0], -c1[1]))), 1.3)

    def test_the_ground_never_lets_the_player_fall(self):
        self.assertIn("go.AddComponent<MeshCollider>().sharedMesh = mesh;", SITES)    # ground, road
        self.assertIn("W.Kolizja(new Vector3(mid.x, (y + y1) * .5f + 1.5f, mid.y) * K, new Vector3(.2f, 3f + Mathf.Abs(y1 - y), l) * K", SITES)  # behind the fences
        self.assertIn("float top = SeaY - .9f, bot = SeaY - 11f", SITES)                 # the underwater wall: boats pass over it
        self.assertIn("public float groundProbeReach = 2.5f, maxGroundStep = .80f", (ROOT / "unity-city/Assets/Mistrzowie/BraxChampionsCar.cs").read_text(encoding="utf-8"))
        # the boats see land only above waterY - .8 (BraxVessel.Lad): the wall's top .9 m under the water is not land
        self.assertIn("hit.point.y > waterY - .8f", (ROOT / "unity-city/Assets/Claude/BraxVessel.cs").read_text(encoding="utf-8"))

    def test_the_far_land_goes_under(self):
        self.assertIn("if (Docisk != null && !Inside(x, z)) h = MathF.Min(h, Docisk(x, z));", HORIZON)
        self.assertIn("BraxChampionsHorizon.Docisk = Docisk;", SITES)
        self.assertIn("if (DoKrawedzi(d, x, z) < RimBand && dr > 35f) continue;", SITES)

    def test_the_villa_on_its_pad(self):
        self.assertIn("BraxChampionsWille.Willa(g, d.nr, new Vector3(d.srodek.x, d.padY, d.srodek.y), d.yaw);", SITES)
        self.assertIn("public const float Przod = -3.9f;", SITES)                    # the front wall's foot (-4) buried
        # each villa its own pool (17.11: the hill's rebuilt villa, BraxChampionsWillaWzgorze)
        self.assertIn("if (l.x > pool.x - .6f && l.x < pool.y + .6f && l.y < pool.z + .6f) return Mathf.Min(d.padY - 2.2f, PrzyPadzie(d, l.y) - .3f);", SITES)

    def test_monika_sells_the_three_places(self):
        self.assertIn('"MAKIETA · TRZY WILLE: WZGÓRZE, WYSPA, NABRZEŻE"', OFFICE)
        self.assertIn("na wzgórzu nad areną, na własnej wyspie w zatoce i nad samym morzem", OFFICE)
        self.assertTrue((ED / "BraxChampionsWilleDzialki.cs.meta").exists())


if __name__ == "__main__":
    unittest.main()
