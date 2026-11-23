"""The villa island moved out of the harbour (owner 23.11 19:53: "zobacz, gdzie ty zrobiłeś tą wyspę ... Wyspa jest na
statek ... ta wyspa w ogóle ma być oddalona kawałek"; 20:11 "tak daleko"; "plaża, woda, tak jak zrobiłeś prędzej").

It stood at (1080, -400): the 296 m cruise quay and the 289 m ship through it. Now alone in the open sea ~420 m off the
shore, its sea the beach's real sea with its own shore field. Here: the clearances from every pier, boat and the beach, its
patch in the far sea (a hole of exactly the patch, met at its corners only), the boats' sea limits, the wiring and the
numbers that must agree. morze_sim --wyspa runs the real field and water code on the island's ground."""
import math
import re
import sys
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"
RT = ROOT / "unity-city/Assets/Mistrzowie"
TOOLS = ROOT / "unity-tools/mistrzowie"
sys.path.insert(0, str(TOOLS))
import port_layout  # noqa: E402

SITES = (ED / "BraxChampionsWilleDzialki.cs").read_text(encoding="utf-8")
WYSPA = (ED / "BraxChampionsWyspaMorze.cs").read_text(encoding="utf-8")
WODA = (ED / "BraxChampionsWyspaWoda.cs").read_text(encoding="utf-8")
HOR = (ED / "BraxChampionsHorizon.cs").read_text(encoding="utf-8")
PLAZA = (ED / "BraxChampionsPlaza.cs").read_text(encoding="utf-8")
MORZE = (RT / "ChampionsMorze.shader").read_text(encoding="utf-8")
PAD = (ED / "BraxChampionsWillaWzgorze.cs").read_text(encoding="utf-8")
VILLA = (ED / "BraxChampionsWille.cs").read_text(encoding="utf-8")
PLAN = (ED / "BraxChampionsWillaWyspa.cs").read_text(encoding="utf-8")
K = 2.33 / 1.80

m = re.search(r'nr = 2, nazwa = "WYSPA", rodzaj = Rodzaj\.Wyspa, srodek = new Vector2\(([-\d.]+)f, ([-\d.]+)f\), yaw = 180f, padY = 3\.5f, spad = 18f,\s*'
              r'x0 = ([-\d.]+)f, x1 = ([-\d.]+)f, z0 = ([-\d.]+)f, z1 = ([-\d.]+)f, woda = true', SITES)
CX, CZ, X0, X1, Z0, Z1 = (float(v) for v in m.groups())


def const(text, name):
    return float(re.search(r"\b" + name + r" = ([-\d.]+)f", text).group(1))


# the island at the water: its pad (local x -48..27, z -22.6..26, turned 180 deg) + the lawn (4 + 18 m) + the beach (14 m)
# + the shore's wander (7 m)
PADX0, PADX1, PADZ0, PADZ1 = const(PLAN, "WPadX0"), const(VILLA, "PadX1"), const(VILLA, "PadZ0"), const(VILLA, "PadZ1")
REACH = 4 + 18 + 14 + 7
ISLAND = (CX - PADX1 - REACH, CZ - PADZ1 - REACH, CX - PADX0 + REACH, CZ - PADZ0 + REACH)   # x0, z0, x1, z1


def rect_gap(a, b):
    dx = max(b[0] - a[2], a[0] - b[2], 0); dz = max(b[1] - a[3], a[1] - b[3], 0)
    return math.hypot(dx, dz)


class Miejsce(unittest.TestCase):
    def test_alone_in_the_open_sea(self):
        # ~300 m from every pier and every moored boat (the cruise ships, the ferry, the yachts, the Rivas, the jetskis)
        near = min(rect_gap(ISLAND, (p["x"], p["z"], p["x"] + p["w"], p["z"] + p["d"])) for p in port_layout.PIERS)
        boats = min(rect_gap(ISLAND, port_layout.hull(b).bounds) for b in port_layout.BERTHS)
        self.assertGreater(near, 250); self.assertGreater(boats, 250)
        # the old place was on the cruise quay itself
        old = (1080 - PADX1 - REACH, -400 - PADZ1 - REACH, 1080 - PADX0 + REACH, -400 - PADZ0 + REACH)
        self.assertEqual(min(rect_gap(old, (p["x"], p["z"], p["x"] + p["w"], p["z"] + p["d"])) for p in port_layout.PIERS), 0)
        # ~420 m off the shore: the beach's waterline, the quay (z -150)
        shore = max(port_layout.shoreline(x) for x in range(50, 701, 5))
        self.assertGreater(min(port_layout.shoreline(x) for x in range(50, 701, 5)) - ISLAND[3], 300)
        self.assertGreater(-150 - ISLAND[3], 400)
        self.assertLess(-150 - ISLAND[3], 480)                                     # "kawałek": seen from the promenade
        self.assertLess(shore, -150)

    def test_its_patch_in_the_far_sea(self):
        # wholly south of the region's own sea (z -500) and inside the far sea's south piece (x 0..1200)
        self.assertTrue(0 < X0 < X1 < 1200 and Z1 < -500)
        # the island and the surf round it inside the patch: ~70 m of water to its rim
        for a, b in ((ISLAND[0], X0), (X1, ISLAND[2]), (ISLAND[1], Z0), (Z1, ISLAND[3])):
            self.assertGreater(a - b, 40)
        # the grid: whole 2 m cells, the band whole cells
        krok, pas = const(WYSPA, "Krok"), const(WYSPA, "Pas")
        for span in (X1 - X0, Z1 - Z0, pas):
            self.assertAlmostEqual(span / krok, round(span / krok))

    def test_the_boats_get_there(self):
        # the boats' sea (BraxAkwen, the city R1's, game units): the fog loop at the south, the "strong current" band along
        # its coast line south of z -340 - the island and a 30 m lane round it stay out of both
        akwen = (ROOT / "unity-city/Assets/Claude/Runtime/BraxAkwen.cs").read_text(encoding="utf-8")
        granice = (ROOT / "unity-city/Assets/Claude/Runtime/BraxMiastoGranice.cs").read_text(encoding="utf-8")
        petla = float(re.search(r"PetlaS = ([-\d.]+)f", akwen).group(1))
        miekki = float(re.search(r"BrzegMiekki = ([-\d.]+)f", akwen).group(1))
        poludnie = float(re.search(r"BrzegPoludnie = ([-\d.]+)f", granice).group(1))
        luk = float(re.search(r"BrzegLukPoludnieZ = ([-\d.]+)f", granice).group(1))
        nach = float(re.search(r"BrzegLukNachylenie = ([-\d.]+)f", granice).group(1))
        lane = 30
        self.assertGreater((ISLAND[1] - lane) * K, petla + 60)
        for z in (ISLAND[1] - lane, ISLAND[3] + lane):
            brzeg = poludnie + max(0, luk - z * K) * nach
            self.assertGreater((ISLAND[0] - lane) * K - brzeg, miekki)

    def test_the_jetty_reaches_the_boats(self):
        self.assertIn("public const float PomostDl = 56f, PomostY = SeaY + 1.0f;", SITES)
        self.assertIn("if (e <= 58f) return Mathf.Lerp(SeaY - 1.2f, SeaY - 2.2f, Smooth(30f, 58f, e));", SITES)   # the lagoon
        self.assertIn("if (d.rodzaj == Rodzaj.Wyspa) bot = SeaY - WyspaDno - 1f;", SITES)                          # the wall to its bed


class Morze(unittest.TestCase):
    def test_its_own_shore_field_and_water(self):
        self.assertIn("if (d.rodzaj == Rodzaj.Wyspa) { Pomost(b, d); Lodz(g, d); BraxChampionsWyspaMorze.Zbuduj(g, d); PlazaWyspy(b, d); }", SITES)
        self.assertIn("BraxChampionsWyspaWoda.Pole(d.x0, d.z0, d.x1, d.z1, Krok, K, sea, PoleN, (x, z) => BraxChampionsWilleDzialki.Grunt(d, x, z),", WYSPA)
        self.assertIn('BraxChampionsPlaza.MorzeZPola(pole, Out + "/Textures/wyspa_morze_brzeg.asset", Out + "/Materials/morze_wyspa.mat", "Mistrzowie morze wokół wyspy", 1f);', WYSPA)
        self.assertIn('var m = MorzeZPola(pole, Output + "/Textures/morze_brzeg.asset", Output + "/Materials/morze.mat", "Mistrzowie morze", 0f);', PLAZA)
        self.assertIn('m.SetFloat("_WzdluzZ", wzdluzZ);', PLAZA)
        self.assertIn("r.shadowCastingMode = ShadowCastingMode.Off;", WYSPA)
        self.assertNotIn("Collider", WYSPA)                                                       # the boats sail on it
        # the island's bed at its rim as deep as the open sea round it (one colour across the edge)
        self.assertIn("(sea - BraxChampionsWilleDzialki.WyspaDno - 5f) * K, RantPas * K);", WYSPA)
        self.assertIn("public const float WyspaDno = 20f;", SITES)

    def test_no_crack_with_the_far_sea(self):
        # the water's outer band fans to the patch's corners; the far sea's south piece is cut on the hole's lines into eight,
        # every piece meets the island's water at its corners only
        self.assertIn("Fan(south, sw, se); Fan(north, nw, ne); Fan(west, sw, nw); Fan(east, se, ne);", WODA)
        self.assertIn("int sw = v.Count; v.Add(new Vector3(x0 * k, y, z0 * k));", WODA)
        self.assertIn("float[] xs = { X0, o.x, o.z, X1 }, zs = { CentreZ - R, o.y, o.w, Z0 };", HOR)
        self.assertIn("if (i != 1 || j != 1) Rect(xs[i], zs[j], xs[i + 1], zs[j + 1]);", HOR)
        self.assertIn("else Rect(X0, CentreZ - R, X1, Z0);", HOR)
        # only when the island's water is in the scene
        self.assertIn("Vector4? otwor = BraxChampionsWyspaMorze.JestWScenie() ? BraxChampionsWyspaMorze.Otwor : (Vector4?)null;", HOR)
        self.assertIn("public static Vector4 Otwor { get { var d = Wyspa(); return new Vector4(d.x0, d.z0, d.x1, d.z1); } }", WYSPA)
        self.assertIn("public const float SeaY = BraxChampionsHorizon.SeaLevel;", SITES)              # one sea level, both meshes

    def test_the_shader_flat_at_the_rims_waves_every_way(self):
        self.assertIn("return smoothstep(9, 60, min(r.x, r.y));", MORZE)                         # the beach's field: 8 units past its sea
        self.assertIn("* smoothstep(15, 60, d) * Rant(w.xz);", MORZE)
        self.assertIn("float Faza(float2 w) { return Wzdluz(w.x) + _WzdluzZ * Wzdluz(w.y * 1.13 + 77); }", MORZE)
        self.assertIn("float p = frac(d / _Fala.x + _Time.y / _Fala.y + Faza(w.xz));", MORZE)
        self.assertIn("float ph0 = d / _Fala.x + _Time.y / _Fala.y + Faza(w) + wig;", MORZE)
        self.assertIn('_WzdluzZ ("Fale różne wzdłuż z (wyspa: brzeg z każdej strony)", Float) = 0', MORZE)
        self.assertIn("var pole = new BraxChampionsMorzeBrzeg(sx0 - 8f, sz0 - 8f, sx1 + 8f, sz1 + 40f, PoleW, PoleH, sea);", PLAZA)

    def test_simulator_runs_the_real_code(self):
        sim = TOOLS / "morze_sim"
        self.assertIn('Include="../../../unity-city/Assets/Editor/Mistrzowie/BraxChampionsWyspaWoda.cs"', (sim / "morze_sim.csproj").read_text(encoding="utf-8"))
        prog = (sim / "Program.cs").read_text(encoding="utf-8")
        self.assertIn("var b = BraxChampionsWyspaWoda.Pole(x0, z0, x1, z1, krok, k, sea, N, Grunt, deep, Rant * k);", prog)
        self.assertIn('if (outer != 4) fails += Fail(', prog)

    def test_new_files_have_their_own_guids(self):
        seen = set()
        for f in ED.glob("*.meta"):
            g = re.search(r"guid: ([0-9a-f]{32})", f.read_text(encoding="utf-8"))
            if g:
                self.assertNotIn(g.group(1), seen, f.name); seen.add(g.group(1))
        for f in ("BraxChampionsWyspaMorze.cs.meta", "BraxChampionsWyspaWoda.cs.meta"):
            self.assertTrue((ED / f).exists())


if __name__ == "__main__":
    unittest.main()
