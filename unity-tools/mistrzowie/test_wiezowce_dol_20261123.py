"""Downtown towers' bases and the gallery seen from far (owner 23.11 15:54-15:59 and 16:53).

- the base: glass to the ground in the tower's 1.5 m module, a band of the tower's metal with a lit soffit round the lobby's
  top, columns of the same metal from the plaza to the band (BRAXHORN TOWER: on its blades' lines), light stone lift cores;
- the forecourts: granite from the plot to the street, cut out of the lawn exactly (never granite on grass in one plane);
- no sparkle: the sun's highlight in the tilted glass units broadens with the distance;
- the gallery's far copy: whole LOD/cut-out objects compete by their size (they scored float.MaxValue and filled the copy
  with the shop fittings inside)."""
import re
import sys
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"
W = (ED / "BraxChampionsWiezowce.cs").read_text(encoding="utf-8")
F = (ED / "BraxChampionsFoundation.cs").read_text(encoding="utf-8")
L = (ED / "BraxChampionsLawnCut.cs").read_text(encoding="utf-8")
P = (ED / "BraxChampionsPodzial.cs").read_text(encoding="utf-8")
SH = (ROOT / "unity-city/Assets/Mistrzowie/BraxChampionsPBR.shader").read_text(encoding="utf-8")


def cialo(src, sygnatura):
    i = src.index(sygnatura)
    j = src.index("\n    }\n", i)
    return src[i:j]


class Podstawa(unittest.TestCase):
    def test_lobby_glass_on_the_module_full_height(self):
        g = cialo(W, "static void GroundFloor(")
        self.assertIn("Mathf.RoundToInt((s1-s0)/1.5f)", g)
        self.assertIn("Glass(s0+k*w+.03f,s0+(k+1)*w-.03f,y0,y1-.02f);", g)
        self.assertNotIn("y0+4.0f", g)                      # no transom at 4 m
        self.assertIn("W.OBox(gClear,", g)
        self.assertIn("Podium(F,front,doorsX);", g)

    def test_podium_band_and_columns(self):
        p = cialo(W, "static void Podium(")
        self.assertIn("Sides(podium,O,yb,Shaft);", p)
        self.assertIn("Q(lantern,", p)                       # the lit soffit
        self.assertIn("float h=yb+.01f-SiteY;", p)            # columns 1 cm into the band, never flush
        self.assertIn("Mathf.Abs(p.x-dx)<3.25f", p)           # never in front of a door or its canopy
        self.assertIn('podium=t.shape==Shape.Ellipse||t.shape==Shape.Stacked?bandWhite:steel;', W)
        self.assertIn("podiumEvery=t.shape==Shape.Setback?2:3;podiumW=t.shape==Shape.Setback?.30f:.45f;podiumD=t.shape==Shape.Setback?.45f:.55f;", W)

    def test_cores_in_stone(self):
        self.assertIn("if(working<0)W.MM(coreStone,core.xMin,core.xMax,y0,ceil,core.yMin,core.yMax,true);", W)
        lc = cialo(W, "static void LiftCore(")
        self.assertNotIn("W.MM(walnut,", lc)
        self.assertIn('LibraryTravertine("tower_core_travertine"', W)


class Plac(unittest.TestCase):
    def test_forecourts_reach_the_sidewalks(self):
        self.assertIn("public const float SidewalkNorthZ=633.022f,SidewalkSouthZ=347.012f;", W)
        f = cialo(W, "public static List<Vector4> Forecourts()")
        self.assertIn("t.yaw==180f?new Vector4(t.x-PlotHalf,t.x+PlotHalf,t.z+PlotHalf,SidewalkNorthZ)", f)
        fc = cialo(W, "static void Forecourt(Tower t)")
        self.assertIn("W.MM(plaza,-PlotHalf,PlotHalf,PlotBaseY,SiteY,z0,z1,true);", fc)
        self.assertIn("if(t.shape==Shape.Setback)Fountain(", fc)

    def test_lawn_cut_in_the_foundation(self):
        self.assertIn('if(m.role=="grass"&&BraxChampionsLawnCut.Cut(ref v,ref uv,ref tri,BraxChampionsWiezowce.Forecourts(),K))', F)
        self.assertIn("mesh.triangles=tri;", F)
        self.assertIn("public static bool Cut(ref Vector3[] v,ref Vector2[] uv,ref int[] tri,List<Vector4> rects,float K)", L)

    def test_lawn_cut_geometry(self):
        """The same Sutherland-Hodgman split in Python: the parts kept never overlap the rectangle and cover the rest."""
        def clip(poly, axis, c, keep_less):
            out = []
            for i in range(len(poly)):
                a, b = poly[i], poly[(i + 1) % len(poly)]
                ia = a[axis] <= c if keep_less else a[axis] >= c
                ib = b[axis] <= c if keep_less else b[axis] >= c
                if ia:
                    out.append(a)
                if ia != ib:
                    s = (c - a[axis]) / (b[axis] - a[axis])
                    out.append((a[0] + (b[0] - a[0]) * s, a[1] + (b[1] - a[1]) * s))
            return out

        def area(p):
            return abs(sum(p[i][0] * p[(i + 1) % len(p)][1] - p[(i + 1) % len(p)][0] * p[i][1] for i in range(len(p)))) / 2

        tri = [(0.0, 0.0), (10.0, 0.0), (0.0, 10.0)]
        x0, x1, z0, z1 = 2.0, 5.0, 1.0, 4.0
        rest = tri; parts = []
        w = clip(rest, 0, x0, True); parts.append(w); rest = clip(rest, 0, x0, False)
        e = clip(rest, 0, x1, False); parts.append(e); rest = clip(rest, 0, x1, True)
        s = clip(rest, 1, z0, True); parts.append(s); rest = clip(rest, 1, z0, False)
        n = clip(rest, 1, z1, False); parts.append(n); inside = clip(rest, 1, z1, True)
        kept = sum(area(p) for p in parts if len(p) >= 3)
        self.assertAlmostEqual(kept + area(inside), area(tri), places=6)
        self.assertAlmostEqual(area(inside), 9.0, places=6)    # the 3 x 3 rectangle lies fully in the triangle
        for p in parts:
            for (x, z) in p:
                self.assertFalse(x0 + 1e-9 < x < x1 - 1e-9 and z0 + 1e-9 < z < z1 - 1e-9)
        # and the C# does the same four cuts in the same order
        for line in ("var w=Clip(rest,0,r.x,true);", "var e=Clip(rest,0,r.y,false);", "var s=Clip(rest,1,r.z,true);", "var n=Clip(rest,1,r.w,false);"):
            self.assertIn(line, L)


class Szklo(unittest.TestCase):
    def test_no_sparkle_far(self):
        self.assertIn("sunRoughness = max(roughness, lerp(.085, .32, saturate((glassDist - 30) / 250)));", SH)
        self.assertIn("float a = sunRoughness*sunRoughness, a2 = a*a;", SH)
        # the sky reflection keeps the glass's own roughness: the towers are not darker (owner 23.11 17:22)
        self.assertIn("colour += environment*viewFresnel*(1-.65*roughness)*_ReflectionStrength*ambientAO;", SH)
        self.assertNotIn("roughness = max(roughness, lerp(", SH)


class Gora(unittest.TestCase):
    def test_lit_crowns_and_calm_twist(self):
        self.assertIn("static float CrownLantern(List<Vector2> p,float y,float h)", W)
        self.assertEqual(W.count("CrownLantern(Offset(last,-2.0f),y+1.4f,3.6f)"), 1)        # T08
        self.assertEqual(W.count("CrownLantern(Offset(A,-2.5f),y+1.2f,3.2f)"), 1)           # T12
        self.assertEqual(W.count("CrownLantern(Box4(h1-2f,h1-2f),top+1.4f,3.6f)"), 1)       # T13
        self.assertNotIn("Sides(ledWhite,Offset(last,.18f),y+.95f,y+1.20f)", W)            # the thin light line is gone
        self.assertIn("unitTilt=.33f;", W)
        self.assertIn("*1.2f*unitTilt*Mathf.Deg2Rad", W)


class GaleriaZDaleka(unittest.TestCase):
    def test_whole_objects_compete_by_size(self):
        k = cialo(P, "static void Kawalki(")
        self.assertNotIn("wynik = float.MaxValue", k)
        self.assertIn("k.wynik = Mathf.Min(e1, Smuklosc * e2);", k)
        self.assertIn("if (k.wynik >= MinKawalek * BraxClaudeOsiedle.K) wynik.Add(k);", k)


if __name__ == "__main__":
    unittest.main()
