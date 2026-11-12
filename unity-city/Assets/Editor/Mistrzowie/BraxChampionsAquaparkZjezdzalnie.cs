using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// AQUAPARK BRAXHORN (BraxChampionsAquapark): the slide tower and the three slides of the owner's picture.
// * TOWER 10 x 10 m (local x 23..33, z 53..63): black steel frame, platforms at 4 / 8 / 12 / 16 m in anti-slip deck, a
//   switchback stair (rise 18 cm, tread 28 cm) in the north-east well, glass balustrades with black rails, a flat roof at
//   19.4 m with warm downlights and a LED edge; the STONE COLUMN on its front with the owner's BRAXHORN bull mark (polished
//   stainless, the mark's green glow - the region's rule: the picture's yellow metal is light, never metal).
// * CZERWONY BYK: red / black fibreglass tube (1.2 m inside) from the 16 m platform, 2.5 turns round a central steel mast,
//   a long run south into the lagoon. ŻÓŁTY WIR: yellow / black tube from 12 m, 1.5 turns, into the lagoon's north-east bay.
//   NIEBIESKI WYŚCIG: three blue racer lanes from 8 m straight down with a camel hump into the north bay.
// Every slide carries a BraxChampionsZjezdzalnia (AKCJA at its gate = the ride) with its own riding line.
public static partial class BraxChampionsAquapark
{
    public const float L1 = DY + 4f, L2 = DY + 8f, L3 = DY + 12f, L4 = DY + 16f, RoofY = DY + 19.4f;
    const float WellX0 = 25.4f, WellX1 = 29.8f, WellZ0 = 59.4f, WellZ1 = 62.7f;
    static Material redFrp, redIn, yellowFrp, yellowIn, blueFrp, blueIn, antiSlip, flange, bullSteel, bullGlow, signalRed, signalGreen;

    static void SlideMaterials()
    {
        redFrp = S("slide_red_gelcoat", new Color(.80f, .05f, .06f), .14f, .02f);
        redIn = S("slide_red_inside", new Color(.92f, .20f, .20f), .10f, .02f);
        yellowFrp = S("slide_yellow_gelcoat", new Color(1.0f, .76f, .04f), .14f, .02f);
        yellowIn = S("slide_yellow_inside", new Color(1.0f, .86f, .32f), .10f, .02f);
        blueFrp = S("slide_blue_gelcoat", new Color(.04f, .30f, .86f), .14f, .02f);
        blueIn = S("slide_blue_inside", new Color(.22f, .55f, .98f), .10f, .02f);
        flange = S("slide_flange_black", new Color(.03f, .03f, .035f), .25f, .30f);
        antiSlip = S("tower_deck_grey_antislip", new Color(.40f, .42f, .45f), .82f, 0f);
        bullSteel = S("tower_bull_polished_steel", new Color(.86f, .88f, .90f), .18f, .9f, new Color(.10f, .10f, .10f));
        bullGlow = S("tower_bull_green_led", new Color(.32f, .95f, .22f), .35f, 0f, new Color(.40f, 1.45f, .28f));
        signalRed = S("slide_signal_red", new Color(.9f, .1f, .1f), .3f, 0f, new Color(1.4f, .12f, .10f));
        signalGreen = S("slide_signal_green", new Color(.1f, .9f, .2f), .3f, 0f, new Color(.15f, 1.3f, .25f));
    }

    // ================================================================ TOWER
    static void Tower()
    {
        // columns (6) from the deck to the roof, square hollow sections 40 cm
        foreach (var (x, z) in new[] { (TowerX0, TowerZ0), (TowerX1, TowerZ0), (TowerX0, TowerZ1), (TowerX1, TowerZ1), (28f, TowerZ1) })
            B(black, CX + x - .2f, CX + x + .2f, DY, RoofY + .3f, CZ + z - .2f, CZ + z + .2f, true);
        // platforms: whole footprint minus the stair well, 30 cm deep with a black edge beam, anti-slip deck on top
        var foot = W(new[] { new Vector2(TowerX0, TowerZ0), new Vector2(TowerX1, TowerZ0), new Vector2(TowerX1, TowerZ1), new Vector2(TowerX0, TowerZ1) });
        var well = W(new[] { new Vector2(WellX0, WellZ0), new Vector2(WellX1, WellZ0), new Vector2(WellX1, WellZ1), new Vector2(WellX0, WellZ1) });
        foreach (float y in new[] { L1, L2, L3, L4 })
        {
            Fill(walk, antiSlip, foot, new List<List<Vector2>> { well }, y);
            Fill(m, black, foot, new List<List<Vector2>> { well }, y - .30f, true);
            Skin(m, black, Ccw(foot), y - .30f, y, false);
            Skin(m, black, well, y - .30f, y, true);
            // a warm LED line along the platform's edge and downlights under it (the picture's lit soffits)
            Skin(m, led, Ccw(Offset(Ccw(foot), .006f)), y - .30f, y - .26f, false);
            for (float x = TowerX0 + 1.2f; x < TowerX1; x += 2.4f) for (float z = TowerZ0 + 1.2f; z < TowerZ1; z += 2.4f)
                if (!(x > WellX0 && x < WellX1 && z > WellZ0 && z < WellZ1)) m.Cylinder(led, V(x, y - .312f, z), .10f, .01f, 12);
        }
        // the stair: per storey two flights (lane A north, westward; lane B south, eastward) and the half landing
        float[] levels = { DY, L1, L2, L3, L4 };
        for (int s = 0; s < 4; s++)
        {
            float y0 = levels[s], y1 = levels[s + 1], mid = (y0 + y1) * .5f;
            Flight(29.8f, 27.0f, 61.1f, 62.7f, y0, mid);            // lane A: up westward
            Flight(27.0f, 29.8f, 59.4f, 61.0f, mid, y1);            // lane B: up eastward
            // half landing x 25.4..27.0 across both lanes
            var land = W(new[] { new Vector2(25.4f, 59.4f), new Vector2(27.0f, 59.4f), new Vector2(27.0f, 62.7f), new Vector2(25.4f, 62.7f) });
            Fill(walk, antiSlip, land, null, mid); Fill(m, black, land, null, mid - .2f, true); Skin(m, black, Ccw(land), mid - .2f, mid, false);
            // wall-side stringer posts at the landing
            B(black, CX + 25.4f, CX + 25.55f, DY, mid, CZ + 59.4f, CZ + 59.55f); B(black, CX + 25.4f, CX + 25.55f, DY, mid, CZ + 62.55f, CZ + 62.7f);
        }
        // glass balustrades round every platform and the well, openings at the slide gates and the stair arrivals
        foreach (float y in new[] { L1, L2, L3, L4 })
        {
            Rail(V(TowerX0, y, TowerZ0), V(TowerX1, y, TowerZ0), y == L2 ? (CX + 23.7f, CX + 28.7f) : (0f, 0f));
            Rail(V(TowerX1, y, TowerZ0), V(TowerX1, y, TowerZ1), y == L3 ? (CZ + 57.1f, CZ + 58.9f) : (0f, 0f));
            Rail(V(TowerX1, y, TowerZ1), V(TowerX0, y, TowerZ1), (0f, 0f));
            Rail(V(TowerX0, y, TowerZ1), V(TowerX0, y, TowerZ0), y == L4 ? (CZ + 57.1f, CZ + 58.9f) : (0f, 0f));
            Rail(V(WellX0, y, WellZ0), V(WellX1, y, WellZ0), (0f, 0f));        // the well's south side (the flights rise north of it)
            Rail(V(WellX0, y, WellZ0), V(WellX0, y, WellZ1), (0f, 0f));
        }
        // ground: the stair's start, the column bases on the deck
        // roof: 40 cm slab with a 60 cm black fascia, warm downlights, a warm LED line round it
        var roof = W(new[] { new Vector2(TowerX0 - 1.2f, TowerZ0 - 1.2f), new Vector2(TowerX1 + 1.2f, TowerZ0 - 1.2f), new Vector2(TowerX1 + 1.2f, TowerZ1 + 1.2f), new Vector2(TowerX0 - 1.2f, TowerZ1 + 1.2f) });
        Fill(m, black, roof, null, RoofY + .6f); Fill(m, white, roof, null, RoofY, true);
        Skin(m, black, Ccw(roof), RoofY, RoofY + .6f, false);
        Skin(m, led, Ccw(Offset(Ccw(roof), .005f)), RoofY + .02f, RoofY + .06f, false);
        for (float x = TowerX0 + 1.2f; x < TowerX1; x += 2.6f)
            for (float z = TowerZ0 + 1.2f; z < TowerZ1; z += 2.6f)
                m.Cylinder(led, V(x, RoofY - .012f, z), .16f, .012f, 16);
        // the stone column on the front (east of the racers) to above the roof, the bull mark on its south face
        float sx0 = CX + 29.4f, sx1 = CX + 32.9f, sz0 = CZ + 51.6f, sz1 = CZ + 54.6f;
        B(stone, sx0, sx1, DY, RoofY + 1.4f, sz0, sz1, true);
        B(black, sx0 - .06f, sx1 + .06f, RoofY + 1.4f, RoofY + 1.6f, sz0 - .06f, sz1 + .06f);
        Bull(V(31.15f, DY + 12.4f, 51.6f - .01f), 3.2f);
        // warm uplights at the column foot
        foreach (float x in new[] { 29.9f, 32.4f }) m.Cylinder(led, V(x, DY, 51.2f), .12f, .04f, 14);
    }

    // a straight flight from (xa, ya) to (xb, yb) along x between z0..z1: risers 18 cm, solid steps with anti-slip treads
    static void Flight(float xa, float xb, float z0, float z1, float ya, float yb)
    {
        int n = Mathf.Max(2, Mathf.RoundToInt((yb - ya) / .18f)); float rise = (yb - ya) / n, run = (xb - xa) / n;
        for (int i = 0; i < n; i++)
        {
            float x0 = CX + xa + run * i, x1 = CX + xa + run * (i + 1), top = ya + rise * (i + 1);
            float lo = Mathf.Min(x0, x1), hi = Mathf.Max(x0, x1);
            walk.BoxMinMax(antiSlip, lo, hi, top - .05f, top, CZ + z0, CZ + z1);
            m.BoxMinMax(black, lo, hi, top - .25f, top - .05f, CZ + z0, CZ + z1);
        }
        // stringers and a stainless handrail on the open side
        var a = V(xa, ya, z0); var b = V(xb, yb, z0);
        m.Tube(steel, a + Vector3.up * .95f, b + Vector3.up * .95f, .022f, 10);
        m.Tube(black, a + Vector3.up * .05f + Vector3.back * .04f, b + Vector3.up * .05f + Vector3.back * .04f, .06f, 8);
        float len = Mathf.Abs(xb - xa), dy = yb - ya, pitch = Mathf.Atan2(dy, xb - xa) * Mathf.Rad2Deg;
        var c = new Vector3(CX + (xa + xb) * .5f, (ya + yb) * .5f - .1f, CZ + (z0 + z1) * .5f);
        ColRot(c, new Vector3(Mathf.Sqrt(len * len + dy * dy), .2f, z1 - z0), Quaternion.Euler(0, 0, pitch), "schody wieży");
    }

    // glass balustrade 1.1 m (12 mm glass in a black base shoe, black top rail), optional opening (a, b) along its run
    static void Rail(Vector3 a, Vector3 b, (float a, float b) gap)
    {
        bool alongX = Mathf.Abs(b.x - a.x) > Mathf.Abs(b.z - a.z);
        float u0 = alongX ? a.x : a.z, u1 = alongX ? b.x : b.z; if (u0 > u1) { var t = u0; u0 = u1; u1 = t; }
        var spans = new List<(float, float)>();
        if (gap.a > 0f && gap.b > gap.a) { spans.Add((u0, gap.a)); spans.Add((gap.b, u1)); } else spans.Add((u0, u1));
        float y = a.y;
        foreach (var (s0, s1) in spans)
        {
            if (s1 - s0 < .05f) continue;
            if (alongX)
            {
                B(glass, s0, s1, y + .08f, y + 1.04f, a.z - .006f, a.z + .006f); B(black, s0, s1, y, y + .08f, a.z - .04f, a.z + .04f);
                B(black, s0, s1, y + 1.04f, y + 1.10f, a.z - .035f, a.z + .035f); Col(s0, s1, y, y + 1.1f, a.z - .06f, a.z + .06f, "balustrada");
            }
            else
            {
                B(glass, a.x - .006f, a.x + .006f, y + .08f, y + 1.04f, s0, s1); B(black, a.x - .04f, a.x + .04f, y, y + .08f, s0, s1);
                B(black, a.x - .035f, a.x + .035f, y + 1.04f, y + 1.10f, s0, s1); Col(a.x - .06f, a.x + .06f, y, y + 1.1f, s0, s1, "balustrada");
            }
        }
    }

    // the owner's BRAXHORN bull mark (the A1 roof relief, CI-built ArenaRoofBull.bytes) at `width`, polished steel with its
    // green LED outline, flat on a wall facing -z; the plain stainless 3D name under it when the mark is not there
    [System.Serializable] sealed class MarkFile { public float scaleK; public MarkSign[] signs; }
    [System.Serializable] sealed class MarkSign { public string id; public BraxChampionsFoundation.MeshData[] meshes; }
    static void Bull(Vector3 centre, float width, float yaw = 0f)
    {
        string src = "Assets/Source/Mistrzowie/ArenaRoofBull.bytes";
        MarkFile data = null;
        try
        {
            if (System.IO.File.Exists(src))
                using (var f = System.IO.File.OpenRead(src)) using (var zip = new System.IO.Compression.GZipStream(f, System.IO.Compression.CompressionMode.Decompress)) using (var r = new System.IO.StreamReader(zip))
                    data = JsonUtility.FromJson<MarkFile>(r.ReadToEnd());
        }
        catch (System.Exception e) { BraxBuildQuality.Warn("AQUAPARK_BULL_MARK " + e.Message); }
        if (data?.signs == null || data.signs.Length == 0)
        {
            BraxChampionsLetters3D.Text(m, BraxChampionsHarbour.SignFace, BraxChampionsHarbour.SignDark, "BRAXHORN", centre, 180f + yaw, .55f, .10f, .12f, width);
            return;
        }
        // the relief is K-scaled metres, 80 m across the horns, +Z its front (BraxChampionsArenaRoofBull lays it on the roof)
        var all = data.signs[0].meshes.Where(x => x.positions != null && x.positions.Length >= 3).ToList();
        float minx = float.MaxValue, maxx = float.MinValue, miny = float.MaxValue, maxy = float.MinValue, maxz = float.MinValue;
        foreach (var mm in all) for (int i = 0; i < mm.positions.Length; i += 3) { minx = Mathf.Min(minx, mm.positions[i]); maxx = Mathf.Max(maxx, mm.positions[i]); miny = Mathf.Min(miny, mm.positions[i + 1]); maxy = Mathf.Max(maxy, mm.positions[i + 1]); maxz = Mathf.Max(maxz, mm.positions[i + 2]); }
        float s = width / Mathf.Max(.01f, maxx - minx); float cxm = (minx + maxx) * .5f, cym = (miny + maxy) * .5f;
        foreach (var mm in all)
        {
            var mat = mm.role == "led" ? bullGlow : mm.role == "steel_edge" ? black : bullSteel;
            int n = mm.positions.Length / 3;
            var q = Quaternion.Euler(0f, yaw, 0f);
            Vector3 Pt(int i) => centre + q * new Vector3(-(mm.positions[3 * i] - cxm) * s, (mm.positions[3 * i + 1] - cym) * s, -(mm.positions[3 * i + 2]) * s * 2.2f);
            for (int t = 0; t + 2 < mm.triangles.Length; t += 3)
            {
                Vector3 p0 = Pt(mm.triangles[t]), p1 = Pt(mm.triangles[t + 1]), p2 = Pt(mm.triangles[t + 2]);
                // x and z both turned (a half turn about y): the winding stays
                m.Triangle(mat, p0, p1, p2);
            }
        }
    }

    // ================================================================ SLIDES
    sealed class Frame { public Vector3 p, t, u, r; }
    // centre line -> parallel-transport frames (u: the tube's "up", r: right)
    static List<Frame> Frames(List<Vector3> pts)
    {
        var f = new List<Frame>(pts.Count); Vector3 up = Vector3.up;
        for (int i = 0; i < pts.Count; i++)
        {
            var t = (pts[Mathf.Min(i + 1, pts.Count - 1)] - pts[Mathf.Max(i - 1, 0)]).normalized;
            up = Vector3.ProjectOnPlane(i == 0 ? Vector3.up : up, t).normalized;
            // keep "up" near the sky (the slide never rolls over): pull it back toward world up
            var wu = Vector3.ProjectOnPlane(Vector3.up, t).normalized; up = Vector3.Slerp(up, wu, .25f).normalized;
            f.Add(new Frame { p = pts[i], t = t, u = up, r = Vector3.Cross(up, t).normalized });
        }
        return f;
    }
    static List<Vector3> Resample(List<Vector3> pts, float ds)
    {
        var o = new List<Vector3> { pts[0] }; float carry = 0f;
        for (int i = 1; i < pts.Count; i++)
        {
            var a = pts[i - 1]; var b = pts[i]; float L = (b - a).magnitude; float d = ds - carry;
            while (d <= L) { o.Add(Vector3.Lerp(a, b, d / L)); d += ds; }
            carry = L - (d - ds);
        }
        if ((o[o.Count - 1] - pts[pts.Count - 1]).magnitude > ds * .3f) o.Add(pts[pts.Count - 1]); else o[o.Count - 1] = pts[pts.Count - 1];
        return o;
    }
    static List<Vector3> Spline(List<Vector3> c, float ds)
    {
        var o = new List<Vector3>();
        for (int i = 0; i + 1 < c.Count; i++)
        {
            Vector3 p0 = c[Mathf.Max(0, i - 1)], p1 = c[i], p2 = c[i + 1], p3 = c[Mathf.Min(c.Count - 1, i + 2)];
            int n = Mathf.Max(2, Mathf.CeilToInt((p2 - p1).magnitude / (ds * .5f)));
            for (int k = 0; k < n; k++)
            {
                float t = k / (float)n, t2 = t * t, t3 = t2 * t;
                o.Add(.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3));
            }
        }
        o.Add(c[c.Count - 1]); return Resample(o, ds);
    }
    static List<Vector3> Helix(Vector3 c, float r, float th0, float th1, float y0, float y1, float ds)
    {
        var o = new List<Vector3>(); float len = Mathf.Abs(th1 - th0) * r; int n = Mathf.Max(8, Mathf.CeilToInt(len / ds));
        for (int k = 0; k <= n; k++) { float t = k / (float)n, th = Mathf.Lerp(th0, th1, t); o.Add(new Vector3(c.x + r * Mathf.Cos(th), Mathf.Lerp(y0, y1, t), c.z + r * Mathf.Sin(th))); }
        return o;
    }

    // a tube slide: outer shell in sections (colour / black, flange rings at the joints), the inside in the lighter colour,
    // a film of running water along its floor, open bell mouth at the exit
    static void Tube(List<Frame> f, Material shellA, Material shellB, Material inside, float rin, float rout)
    {
        const int sides = 22; float run = 0f; int section = 0;
        for (int i = 0; i + 1 < f.Count; i++)
        {
            var a = f[i]; var b = f[i + 1]; float seg = (b.p - a.p).magnitude; run += seg;
            if (run > 3.0f) { run = 0f; section++; Flange(b, rout); }
            var shell = section % 2 == 0 ? shellA : shellB;
            for (int j = 0; j < sides; j++)
            {
                float a0 = j * 2f * Mathf.PI / sides, a1 = (j + 1) * 2f * Mathf.PI / sides;
                Vector3 da0 = a.u * Mathf.Cos(a0) + a.r * Mathf.Sin(a0), da1 = a.u * Mathf.Cos(a1) + a.r * Mathf.Sin(a1);
                Vector3 db0 = b.u * Mathf.Cos(a0) + b.r * Mathf.Sin(a0), db1 = b.u * Mathf.Cos(a1) + b.r * Mathf.Sin(a1);
                Q(m, shell, a.p + da0 * rout, b.p + db0 * rout, b.p + db1 * rout, a.p + da1 * rout, (da0 + da1 + db0 + db1).normalized);
                Q(m, inside, a.p + da0 * rin, b.p + db0 * rin, b.p + db1 * rin, a.p + da1 * rin, -(da0 + da1 + db0 + db1).normalized);
            }
            // the running water: a 50 cm film 1 cm above the inner floor
            Vector3 wa = a.p - a.u * (rin - .012f), wb = b.p - b.u * (rin - .012f);
            Q(m, water, wa - a.r * .25f, wb - b.r * .25f, wb + b.r * .25f, wa + a.r * .25f, a.u);
        }
        // the open ends: a rolled lip ring
        foreach (var e in new[] { f[0], f[f.Count - 1] }) Flange(e, rout + .02f);
    }
    static void Flange(Frame e, float r)
    {
        var rot = Quaternion.LookRotation(e.t, e.u);
        var ring = new List<Vector2>(); for (int k = 0; k <= 8; k++) { float a = Mathf.PI * k / 8f; ring.Add(new Vector2(r + .05f + .04f * Mathf.Sin(a), -.05f + .1f * k / 8f)); }
        m.Lathe(flange, e.p, ring.ToArray(), 22, rot * Quaternion.Euler(90f, 0, 0), false, false);
    }

    // a racer lane: an open U channel (50 cm flat floor, 35 cm round corners, sides to 45 cm), rolled lips, outside shell
    static void Lane(List<Frame> f, float off, Material shell, Material inside)
    {
        var prof = new List<Vector2>();                                                   // (across, up) of the riding surface
        prof.Add(new Vector2(-.60f, .45f)); prof.Add(new Vector2(-.60f, .35f));
        for (int k = 0; k <= 6; k++) { float a = Mathf.PI * (1f + .5f * k / 6f); prof.Add(new Vector2(-.25f + .35f * Mathf.Cos(a), .35f + .35f * Mathf.Sin(a))); }
        for (int k = 0; k <= 6; k++) { float a = Mathf.PI * (1.5f + .5f * k / 6f); prof.Add(new Vector2(.25f + .35f * Mathf.Cos(a), .35f + .35f * Mathf.Sin(a))); }
        prof.Add(new Vector2(.60f, .35f)); prof.Add(new Vector2(.60f, .45f));
        for (int i = 0; i + 1 < f.Count; i++)
        {
            var a = f[i]; var b = f[i + 1];
            Vector3 A(Vector2 q, float d = 0f) => a.p + a.r * (q.x + off) + a.u * (q.y + d);
            Vector3 Bq(Vector2 q, float d = 0f) => b.p + b.r * (q.x + off) + b.u * (q.y + d);
            for (int k = 0; k + 1 < prof.Count; k++)
            {
                var q0 = prof[k]; var q1 = prof[k + 1];
                var nIn = Vector3.Cross(b.p - a.p, (a.r * (q1.x - q0.x) + a.u * (q1.y - q0.y))).normalized;
                if (Vector3.Dot(nIn, a.u) < 0f && Mathf.Abs(q1.x - q0.x) > .05f) nIn = -nIn;
                Q(m, inside, A(q0), Bq(q0), Bq(q1), A(q1), nIn);
                Q(m, shell, A(q0, -.04f), Bq(q0, -.04f), Bq(q1, -.04f), A(q1, -.04f), -nIn);
            }
            m.Tube(shell, A(prof[0]), Bq(prof[0]), .04f, 8); m.Tube(shell, A(prof[prof.Count - 1]), Bq(prof[prof.Count - 1]), .04f, 8);
            Vector3 wa = a.p + a.r * off + a.u * .012f, wb = b.p + b.r * off + b.u * .012f;
            Q(m, water, wa - a.r * .24f, wb - b.r * .24f, wb + b.r * .24f, wa + a.r * .24f, a.u);
        }
    }

    // a steel post from the deck up under the slide (none in the water), with a cradle
    static void Post(Vector3 under, float halfW)
    {
        var g = new Vector2(under.x, under.z);
        if (Inside(lagoon.cop, g)) return;
        float top = under.y; if (top - DY < .8f) return;
        B(black, under.x - .14f, under.x + .14f, DY, top, under.z - .14f, under.z + .14f, true);
        m.Box(black, new Vector3(under.x, top - .06f, under.z), new Vector3(halfW * 2f, .12f, .24f));
        m.Cylinder(graphite, new Vector3(under.x, DY, under.z), .32f, .06f, 16);
    }

    // the ride along a riding line (world nominal) with its surface normals
    static void Ride(string name, List<Vector3> line, List<Vector3> normals, Vector3 gate, Vector3 exit, float reach = 1.9f)
    {
        var g = new GameObject("ZJEŻDŻALNIA " + name + " (AKCJA przy bramce = zjazd)"); g.transform.SetParent(root, false);
        var z = g.AddComponent<BraxChampionsZjezdzalnia>();
        z.nazwa = name; z.trasa = line.Select(p => p * K).ToArray(); z.normalne = normals.ToArray();
        z.start = gate * K; z.wyjscie = exit * K; z.g = 9.81f * K; z.vStart = 2.4f * K; z.vMin = 3.0f * K; z.vMax = 13.5f * K;
        z.zasieg = reach * K; z.lezenie = .14f * K;
        var seat = new GameObject(name + " | siedzisko jazdy"); seat.transform.SetParent(g.transform, false);
        var s = seat.AddComponent<BraxSeat>(); s.lie = true; s.label = "ZJEŻDŻALNIA"; seat.SetActive(false);
        z.siedzisko = s;
        Slides++;
    }

    // the gate on a platform: stainless portal, the name in 3D, a red / green signal
    static void Gate(Vector3 at, Vector3 dir, string name, float halfW = .78f)
    {
        var rot = Quaternion.LookRotation(dir); var r = rot * Vector3.right;
        foreach (float s in new[] { -1f, 1f }) m.Box(steel, at + r * halfW * s + Vector3.up * 1.2f, new Vector3(.08f, 2.4f, .08f), rot);
        m.Box(black, at + Vector3.up * 2.55f, new Vector3(halfW * 2f + .34f, .42f, .14f), rot);
        BraxChampionsLetters3D.Text(m, BraxChampionsHarbour.SignFace, BraxChampionsHarbour.SignDark, name, at + Vector3.up * 2.43f - dir * .075f, BraxChampionsLetters3D.YawFacing(-dir), .20f, .04f, .10f, 1.7f);
        m.Cylinder(signalRed, at + r * halfW + Vector3.up * 1.9f - dir * .05f, .06f, .03f, 12, Quaternion.FromToRotation(Vector3.up, -dir), true, false);
        m.Cylinder(signalGreen, at + r * halfW + Vector3.up * 1.72f - dir * .05f, .06f, .03f, 12, Quaternion.FromToRotation(Vector3.up, -dir), true, false);
    }

    static void SlidesAll()
    {
        float ds = .5f;
        // ---------------- CZERWONY BYK (red tube from 16 m)
        {
            var c = V(10f, 0f, 62f); float r = 6.5f, ri = .60f, ro = .64f;
            var pts = new List<Vector3>();
            pts.AddRange(Spline(new List<Vector3> { V(23.6f, L4 + .70f, 58.0f), V(21.0f, L4 + .55f, 58.6f), V(18.4f, L4 + .25f, 60.4f), new Vector3(c.x + r, L4 - .15f, c.z) }, ds));
            pts.AddRange(Helix(c, r, 0f, 5f * Mathf.PI, L4 - .15f, DY + 3.6f, ds).Skip(1));
            pts.AddRange(Spline(new List<Vector3> { new Vector3(c.x - r, DY + 3.6f, c.z), V(3.5f, DY + 3.2f, 56.0f), V(3.6f, DY + 2.3f, 46.0f), V(3.7f, DY + 1.5f, 37.0f), V(3.8f, lagoon.water + .78f, 30.6f) }, ds).Skip(1));
            var f = Frames(Resample(pts, ds));
            Tube(f, redFrp, flange, redIn, ri, ro);
            // the central mast and the arms every quarter turn, posts under the run
            B(black, c.x - .25f, c.x + .25f, DY, L4 + .5f, c.z - .25f, c.z + .25f, true);
            foreach (var e in f.Where((x, i) => i % 18 == 0))
            {
                var d = new Vector2(e.p.x - c.x, e.p.z - c.z);
                if (d.magnitude > r - 1f && d.magnitude < r + 1f && e.p.y > DY + 2.5f)
                {
                    var under = e.p - Vector3.up * (ro + .06f); var dir = new Vector3(d.x, 0, d.y).normalized;
                    m.Box(black, new Vector3((c.x + under.x) * .5f, under.y - .1f, (c.z + under.z) * .5f), new Vector3(.18f, .22f, r), Quaternion.LookRotation(dir));
                    m.Box(black, under - Vector3.up * .02f, new Vector3(1.1f, .1f, .3f), Quaternion.LookRotation(e.t));
                }
                else Post(e.p - Vector3.up * (ro + .02f), .55f);
            }
            var line = f.Select(e => e.p - e.u * (ri - .02f)).ToList(); var nrm = f.Select(e => e.u).ToList();
            Ride("CZERWONY BYK", line, nrm, V(24.4f, L4, 58.0f), new Vector3(V(3.8f, 0, 28.6f).x, lagoon.water - .45f, V(0, 0, 28.6f).z));
            Gate(V(23.15f, L4, 58.0f), Vector3.left, "CZERWONY BYK");
        }
        // ---------------- ŻÓŁTY WIR (yellow tube from 12 m)
        {
            var c = V(43f, 0f, 60f); float r = 5.5f, ri = .60f, ro = .64f;
            var pts = new List<Vector3>();
            pts.AddRange(Spline(new List<Vector3> { V(32.4f, L3 + .70f, 58.0f), V(34.6f, L3 + .55f, 58.6f), V(36.6f, L3 + .3f, 59.5f), new Vector3(c.x - r, L3 - .1f, c.z) }, ds));
            pts.AddRange(Helix(c, r, Mathf.PI, -2f * Mathf.PI, L3 - .1f, DY + 3.4f, ds).Skip(1));
            pts.AddRange(Spline(new List<Vector3> { new Vector3(c.x + r, DY + 3.4f, c.z), V(48.4f, DY + 3.0f, 54.0f), V(46.4f, DY + 2.2f, 44.0f), V(42.2f, DY + 1.3f, 34.5f), V(40.0f, lagoon.water + .78f, 27.6f) }, ds).Skip(1));
            var f = Frames(Resample(pts, ds));
            Tube(f, yellowFrp, flange, yellowIn, ri, ro);
            B(black, c.x - .25f, c.x + .25f, DY, L3 + .5f, c.z - .25f, c.z + .25f, true);
            foreach (var e in f.Where((x, i) => i % 16 == 0))
            {
                var d = new Vector2(e.p.x - c.x, e.p.z - c.z);
                if (d.magnitude > r - 1f && d.magnitude < r + 1f && e.p.y > DY + 2.5f)
                {
                    var under = e.p - Vector3.up * (ro + .06f); var dir = new Vector3(d.x, 0, d.y).normalized;
                    m.Box(black, new Vector3((c.x + under.x) * .5f, under.y - .1f, (c.z + under.z) * .5f), new Vector3(.18f, .22f, r), Quaternion.LookRotation(dir));
                    m.Box(black, under - Vector3.up * .02f, new Vector3(1.1f, .1f, .3f), Quaternion.LookRotation(e.t));
                }
                else Post(e.p - Vector3.up * (ro + .02f), .55f);
            }
            var line = f.Select(e => e.p - e.u * (ri - .02f)).ToList(); var nrm = f.Select(e => e.u).ToList();
            Ride("ŻÓŁTY WIR", line, nrm, V(31.6f, L3, 58.0f), new Vector3(V(39.4f, 0, 26.0f).x, lagoon.water - .45f, V(0, 0, 26.0f).z));
            Gate(V(32.85f, L3, 58.0f), Vector3.right, "ŻÓŁTY WIR");
        }
        // ---------------- NIEBIESKI WYŚCIG (three racer lanes from 8 m)
        {
            float z0 = 53.0f, zEnd = 35.6f, zOut = 32.4f, y0 = L2 + .05f, y1 = lagoon.water + .30f;
            var pts = new List<Vector3>();
            for (float z = z0 + .8f; z > z0; z -= .4f) pts.Add(V(26.2f, y0, z));
            for (int k = 0; k <= 60; k++)
            {
                float t = k / 60f, z = Mathf.Lerp(z0, zEnd, t);
                float s = t - Mathf.Sin(2f * Mathf.PI * t) / (2f * Mathf.PI);                  // cycloid: flat at both ends
                float hump = .55f * Mathf.Exp(-Mathf.Pow((t - .72f) / .07f, 2f));               // the camel hump
                pts.Add(V(26.2f, Mathf.Lerp(y0, y1, s) + hump, z));
            }
            for (float z = zEnd - .4f; z >= zOut; z -= .4f) pts.Add(V(26.2f, y1 - (zEnd - z) * .02f, z));
            var f = Frames(Resample(pts, .4f));
            foreach (float off in new[] { -1.62f, 0f, 1.62f }) Lane(f, off, blueFrp, blueIn);
            // bents every 3 m: two posts and a cross beam under the three lanes
            for (int i = 4; i < f.Count; i += 8)
            {
                var e = f[i]; if (Inside(lagoon.cop, new Vector2(e.p.x, e.p.z)) || e.p.y - DY < 1.0f) continue;
                foreach (float s in new[] { -2.5f, 2.5f }) { var b = e.p + e.r * s; B(black, b.x - .12f, b.x + .12f, DY, e.p.y - .06f, b.z - .12f, b.z + .12f, true); }
                m.Box(black, e.p - e.u * .10f, new Vector3(5.2f, .14f, .2f), Quaternion.LookRotation(e.t, e.u));
            }
            int lane = 0;
            foreach (float off in new[] { -1.62f, 0f, 1.62f })
            {
                var line = f.Select(e => e.p + e.r * off + e.u * .03f).ToList(); var nrm = f.Select(e => e.u).ToList();
                // the lane's side is the frame's right (the racers run south: right = west)
                var side = new Vector3(f[0].r.x, 0f, f[0].r.z).normalized * off;
                var gate = V(26.2f, L2, 53.6f) + side;
                Ride("NIEBIESKI WYŚCIG " + (++lane), line, nrm, gate, V(26.2f, lagoon.water - .45f, 29.6f) + side, .75f);
            }
            Gate(V(26.2f, L2, 53.25f), Vector3.back, "NIEBIESKI WYŚCIG", 2.45f);
        }
    }
}
