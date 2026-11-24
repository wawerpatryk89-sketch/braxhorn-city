"""The seafront villa's private beach gets the real sea (owner 24.11 00:50: "dałaś nam ten lepszy piasek na tej plaży?").

Its sand was already the city beach's (the site's sand is sand_dry.mat, which BraxChampionsPlaza.Piasek turns into
ChampionsPiasek before the villas are built), but its sea was the flat far sea: no surf, no run-up. Now the island's way
(BraxChampionsWyspaMorze): its own shore field and water over a window of the site from its south rim to the far sea's east
part's top; the far sea leaves a hole of exactly the window. The field sees the ground one sees (in the site's rim band the
far land's mesh where it is higher); the surf fades at the window's two sides (where it ends on the far land's coast); the
beach's west wall moved out of the rim band (the far land stood up to 0.6 m over its sand); the loungers up the beach, out
of the run-up. wille_preview "nabrzeze_morze" runs the real code (far land, field, water) and must say NABRZEZE_MORZE_OK."""
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"
RT = ROOT / "unity-city/Assets/Mistrzowie"
TOOLS = ROOT / "unity-tools/mistrzowie"

NAB = (ED / "BraxChampionsNabrzezeMorze.cs").read_text(encoding="utf-8")
SITES = (ED / "BraxChampionsWilleDzialki.cs").read_text(encoding="utf-8")
HOR = (ED / "BraxChampionsHorizon.cs").read_text(encoding="utf-8")
MORZE = (RT / "ChampionsMorze.shader").read_text(encoding="utf-8")
CEGLA = (ED / "BraxChampionsWillaNabrzezeCegla.cs").read_text(encoding="utf-8")
STUBS = (TOOLS / "wille_preview/WilleStubs.cs").read_text(encoding="utf-8")
MAIN = (TOOLS / "wille_preview/WilleMain.cs").read_text(encoding="utf-8")
PROJ = (TOOLS / "wille_preview/wille_preview.csproj").read_text(encoding="utf-8")
WYSPA = (ED / "BraxChampionsWyspaMorze.cs").read_text(encoding="utf-8")


def stala(src, name):
    return float(re.search(name + r" = (-?[\d.]+)f", src).group(1))


class Morze(unittest.TestCase):
    def test_built_with_the_site(self):
        self.assertIn("if (d.rodzaj == Rodzaj.Nabrzeze) BraxChampionsNabrzezeMorze.Zbuduj(g, d);", SITES)
        meta = (ED / "BraxChampionsNabrzezeMorze.cs.meta").read_text(encoding="utf-8")
        self.assertRegex(meta, r"guid: [0-9a-f]{32}")

    def test_the_island_s_way(self):
        self.assertIn('public const string Nazwa = "WILLA 3 | morze przy prywatnej plaży (fale na jej piasek)";', NAB)
        self.assertIn("var pole = BraxChampionsWyspaWoda.Pole(d.x0, d.z0, d.x1, Gora, BraxChampionsWyspaMorze.Krok, K, sea, BraxChampionsWyspaMorze.PoleN,", NAB)
        self.assertIn("(x, z) => BraxChampionsWilleDzialki.Widoczny(d, x, z), (sea - Dno) * K, BraxChampionsWyspaMorze.RantPas * K);", NAB)
        self.assertIn('BraxChampionsPlaza.MorzeZPola(pole, Out + "/Textures/nabrzeze_morze_brzeg.asset", Out + "/Materials/morze_nabrzeze.mat", "Mistrzowie morze przy prywatnej plaży", 0f);', NAB)
        self.assertIn("int dry = BraxChampionsWyspaWoda.Siatka(d.x0, d.z0, d.x1, Gora, BraxChampionsWyspaMorze.Krok, BraxChampionsWyspaMorze.Pas, K, sea * K,", NAB)
        self.assertIn("(x, z) => BraxChampionsWilleDzialki.Widoczny(d, x, z) > sea + BraxChampionsWyspaMorze.Suchy, v, t);", NAB)
        self.assertIn("r.shadowCastingMode = ShadowCastingMode.Off;", NAB)
        self.assertNotIn("Collider", NAB)
        self.assertIn('if (pole.Odcinki == 0) BraxBuildQuality.Warn("DO POPRAWKI NABRZEZE_MORZE_NO_WATERLINE', NAB)
        # its own assets (not the island's)
        for s in ("wille_wyspa_morze.asset", "wyspa_morze_brzeg.asset", "morze_wyspa.mat"):
            self.assertNotIn(s, NAB)
        self.assertIn('string path = folder + "/wille_nabrzeze_morze.asset";', NAB)

    def test_the_window_on_the_two_metre_grid_over_the_far_sea_s_east_part(self):
        gora, krok = stala(NAB, "Gora"), 2.0
        m = re.search(r"x0 = (\d+)f, x1 = (\d+)f, z0 = (-?\d+)f, z1 = (-?\d+)f, woda = true \};\n\s*// east from the street mouth", SITES)
        x0, x1, z0 = float(m.group(1)), float(m.group(2)), float(m.group(3))
        self.assertEqual((x0, x1, z0), (1200.0, 1350.0, -215.0))
        for a in ((x1 - x0) / krok, (gora - z0) / krok):
            self.assertEqual(a, int(a))                                              # whole 2 m cells, as Pole and Siatka round them
        top = float(re.search(r"R = SeaOut, top = (-?[\d.]+)f;", HOR).group(1))
        self.assertGreaterEqual(gora, top)                                           # up to (over) the east part's top: clipped there
        self.assertLess(gora - top, 2.0)                                             # and on the land just past it
        self.assertIn("public static Vector4 Otwor { get { var d = Nabrzeze(); return new Vector4(d.x0, d.z0, d.x1, Gora); } }", NAB)

    def test_the_far_sea_leaves_the_hole(self):
        self.assertIn("public static List<(Vector3 a, Vector3 b, Vector3 c, Vector3 d)> Sea(float k, Vector4? otwor = null, Vector4? wschod = null)", HOR)
        self.assertIn("Vector4? wschod = BraxChampionsNabrzezeMorze.JestWScenie() ? BraxChampionsNabrzezeMorze.Otwor : (Vector4?)null;", HOR)
        self.assertIn("var q = Sea(k, otwor, wschod);", HOR)
        self.assertIn("else Rect(X1, CentreZ - R, CentreX + R, top);         // east, under the east coast", HOR)
        # the island's lines as they were
        self.assertIn("float[] xs = { X0, o.x, o.z, X1 }, zs = { CentreZ - R, o.y, o.w, Z0 };", HOR)
        self.assertIn("else Rect(X0, CentreZ - R, X1, Z0);", HOR)
        self.assertIn("Vector4? otwor = BraxChampionsWyspaMorze.JestWScenie() ? BraxChampionsWyspaMorze.Otwor : (Vector4?)null;", HOR)

    def test_the_east_part_minus_the_window_exactly(self):
        # Sea()'s east split, in numbers: the pieces cover the east part less the window, no overlap, no sliver
        cx, cz, R = 600.0, 250.0, stala(HOR, "SeaOut")
        X1, top = 1200.0, -100.0
        e = (1200.0, -215.0, 1350.0, stala(NAB, "Gora"))
        self.assertIn("if (wschod is Vector4 e && e.x >= X1 && e.x < e.z && e.z < CentreX + R && e.y > CentreZ - R && e.y < Mathf.Min(e.w, top))", HOR)
        self.assertIn("float[] xs = { X1, e.x, e.z, CentreX + R }, zs = { CentreZ - R, e.y, Mathf.Min(e.w, top), top };", HOR)
        self.assertIn("if ((i != 1 || j != 1) && xs[i + 1] - xs[i] > .01f && zs[j + 1] - zs[j] > .01f) Rect(xs[i], zs[j], xs[i + 1], zs[j + 1]);", HOR)
        xs = [X1, e[0], e[2], cx + R]; zs = [cz - R, e[1], min(e[3], top), top]
        pieces = [(xs[i], zs[j], xs[i + 1], zs[j + 1]) for i in range(3) for j in range(3)
                  if (i != 1 or j != 1) and xs[i + 1] - xs[i] > .01 and zs[j + 1] - zs[j] > .01]
        area = sum((b - a) * (d - c) for a, c, b, d in pieces)
        hole = (e[2] - e[0]) * (min(e[3], top) - e[1])
        self.assertAlmostEqual(area + hole, (cx + R - X1) * (top - (cz - R)), delta=1.0)
        self.assertEqual(len(pieces), 3)                                             # the window on the part's west edge and its top

    def test_the_field_sees_the_ground_one_sees(self):
        self.assertIn("public static float Widoczny(Dzialka d, float x, float z) => Mathf.Max(Grunt(d, x, z), BraxChampionsHorizon.Lad(x, z, Docisk));", SITES)
        self.assertIn("public static float Lad(float x, float z, Func<float, float, float> docisk = null)", HOR)
        # Lad: Land()'s polar cell, its two triangles, Height at the corners, the same keep rule
        self.assertIn("float ang = cc * 2 * MathF.PI / Columns, rad = RingIn * MathF.Pow(RingOut / RingIn, rr / (float)Rings);", HOR)
        self.assertIn("float ang = (ci * per + a) * 2 * MathF.PI / Columns;", HOR)
        self.assertIn("foreach (var (a, b, e) in new[] { (p00, p10, p01), (p01, p10, p11) })", HOR)
        self.assertIn("ch.t.Add(i00); ch.t.Add(i10); ch.t.Add(i01);", HOR)                 # Land()'s two triangles of a cell
        self.assertIn("ch.t.Add(i01); ch.t.Add(i10); ch.t.Add(i11);", HOR)
        self.assertIn("int Keep(Vector3 q) => Inside(q.x, q.z) ? 0 : q.y < SeaLevel - .5f ? -1 : 1;", HOR)
        self.assertIn("keep[a, ri] = Inside(x, z) ? 0 : h < SeaLevel - .5f ? -1 : 1;", HOR)

    def test_the_surf_fades_at_the_window_s_sides(self):
        self.assertIn('_Boki ("Przybój gaśnie przy bokach x pola (j.; 0 = bez)", Float) = 0', MORZE)
        self.assertIn("return _Boki > 0 ? smoothstep(0, _Boki, min(u, 1 - u) / _BrzegRect.z) : 1;", MORZE)
        self.assertIn("float env = 1 - smoothstep(0, _Fala.w, d);\n                env *= Boki(w.xz);", MORZE)
        self.assertIn("float boki = Boki(w); surf *= boki;", MORZE)
        self.assertIn("zone *= boki;", MORZE)
        self.assertIn('mat.SetFloat("_Boki", Boki * K); EditorUtility.SetDirty(mat);', NAB)
        # the fade ends before the private beach's walls (0 on the city beach and the island: the default)
        boki = stala(NAB, "Boki")
        west = 1272 + stala(CEGLA, "N3PlazaX0") - 1200
        east = 1350 - (1272 + stala(CEGLA, r"N3PlazaX1"))
        self.assertLessEqual(boki, min(west, east))
        self.assertNotIn("_Boki", (ED / "BraxChampionsPlaza.cs").read_text(encoding="utf-8"))
        self.assertNotIn("_Boki", WYSPA)


class Plaza(unittest.TestCase):
    def test_the_west_wall_off_the_site_s_rim_band(self):
        rim = stala(SITES, "RimBand")
        x0 = stala(CEGLA, "N3PlazaX0")
        self.assertEqual(x0, -46.0)
        self.assertGreater(1272 + x0 - .45, 1200 + rim)                            # the wall's face clear of the band (x 1225)
        self.assertIn('BraxChampionsLetters3D.Text(b, steel, black, "PLAŻA PRYWATNA", new Vector3(N3PlazaX0 - .45f, ty, -31f),', CEGLA)
        self.assertIn("l.x < BraxChampionsWille.N3PlazaX0 + 2f ||", SITES)               # the lawns' palms inside it

    def test_loungers_out_of_the_run_up(self):
        z = float(re.search(r"\{ -40f, -32f, -6f, 6f, 18f, 30f \}\)\s*\{\s*float z = (-?\d+)f", CEGLA).group(1))
        self.assertEqual(z, -45.0)
        # the waterline on the beach at world z -150.5..-154.4 (wille_preview nabrzeze_morze); the run-up 0.21 m up a 6 % beach
        self.assertGreater(-98 + z, -150.5 + .21 / .06 + 2)


class Sprawdzenie(unittest.TestCase):
    def test_preview_runs_the_real_code(self):
        self.assertIn('if (args.Length > 0 && args[0] == "nabrzeze_morze") { Environment.Exit(NabrzezeMorze()); return; }', MAIN)
        self.assertIn("BraxChampionsHorizon.Docisk = BraxChampionsWilleDzialki.Docisk; var land = BraxChampionsHorizon.Land(1f);", MAIN)
        self.assertIn('Console.WriteLine(fails == 0 ? "NABRZEZE_MORZE_OK"', MAIN)
        for f in ("BraxChampionsWyspaWoda.cs", "BraxChampionsMorzeBrzeg.cs", "BraxChampionsHorizon.cs", "BraxChampionsWilleDzialki.cs"):
            self.assertIn('Include="../../../unity-city/Assets/Editor/Mistrzowie/' + f + '"', PROJ)
        self.assertIn("public static class BraxChampionsNabrzezeMorze { public static void Zbuduj(", STUBS)
        # the stub's numbers are the real island sea's
        for name in ("Krok", "Pas", "Suchy", "RantPas"):
            self.assertEqual(stala(STUBS, name), stala(WYSPA, name), name)
        self.assertIn("public const int PoleN = 512;", STUBS)
        self.assertIn("public const int PoleN = 512;", WYSPA)


if __name__ == "__main__":
    unittest.main()
