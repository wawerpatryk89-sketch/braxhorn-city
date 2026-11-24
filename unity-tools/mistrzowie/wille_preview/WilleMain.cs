// Runs the real villa generator (BraxChampionsWille) on the stubs and dumps its meshes in pad-local nominal metres.
using System;
using System.Linq;
using UnityEngine;
public static class WilleProgram
{
    // 24.11 the seafront's sea (BraxChampionsNabrzezeMorze) on the real code, k = 1 (nominal metres): the far land's mesh
    // (BraxChampionsHorizon.Lad = Land's triangles) never over the private beach; the field from the ground one sees has a
    // waterline, between the walls the distance only falls and the bed only rises toward the beach; the water faces up, every
    // inner edge shared by two triangles, its outer edge the window's four sides, its inner edge only round dry land
    static int NabrzezeMorze()
    {
        var d = BraxChampionsWilleDzialki.Dzialki[2]; int fails = 0;
        int Fail(string m) { Console.WriteLine("NABRZEZE_MORZE_FAIL " + m); return 1; }
        float x0 = d.x0, z0 = d.z0, x1 = d.x1, z1 = -99f, sea = BraxChampionsWilleDzialki.SeaY;           // BraxChampionsNabrzezeMorze.Gora
        float bx0 = 1272f + BraxChampionsWille.N3PlazaX0, bx1 = 1272f + BraxChampionsWille.N3PlazaX1, bz = -98f + BraxChampionsWille.N3PlazaZ;
        // the far land's mesh as the game builds it (pressed under the sites) over the beach's sand
        BraxChampionsHorizon.Docisk = BraxChampionsWilleDzialki.Docisk; var land = BraxChampionsHorizon.Land(1f); BraxChampionsHorizon.Docisk = null; int checkedPts = 0, over = 0; float maxOver = 0f;
        var tris = new System.Collections.Generic.List<(Vector3 a, Vector3 b, Vector3 c)>();
        foreach (var ch in land)
            for (int i = 0; i < ch.t.Count; i += 3)
            {
                var a = ch.v[ch.t[i]]; var b = ch.v[ch.t[i + 1]]; var c = ch.v[ch.t[i + 2]];
                if (Math.Max(a.x, Math.Max(b.x, c.x)) >= x0 - 50 && Math.Min(a.x, Math.Min(b.x, c.x)) <= x1 + 50 && Math.Max(a.z, Math.Max(b.z, c.z)) >= z0 - 50 && Math.Min(a.z, Math.Min(b.z, c.z)) <= z1 + 50) tris.Add((a, b, c));
            }
        float Mesh(float x, float z)
        {
            foreach (var (a, b, c) in tris)
            {
                float den = (b.z - c.z) * (a.x - c.x) + (c.x - b.x) * (a.z - c.z);
                float l1 = ((b.z - c.z) * (x - c.x) + (c.x - b.x) * (z - c.z)) / den, l2 = ((c.z - a.z) * (x - c.x) + (a.x - c.x) * (z - c.z)) / den, l3 = 1 - l1 - l2;
                if (l1 >= -1e-4f && l2 >= -1e-4f && l3 >= -1e-4f) return l1 * a.y + l2 * b.y + l3 * c.y;
            }
            return float.NegativeInfinity;
        }
        int lad = 0; float ladWorst = 0f;
        for (float x = x0; x <= x1; x += 1f)
            for (float z = z0; z <= z1; z += 1f)
            {
                float m = Mesh(x, z), l = BraxChampionsHorizon.Lad(x, z, BraxChampionsWilleDzialki.Docisk);
                if (!float.IsInfinity(m) && !float.IsInfinity(l) && Math.Abs(m - l) > .005f) { lad++; ladWorst = Math.Max(ladWorst, Math.Abs(m - l)); }
                if (x <= bx0 + .5f || x >= bx1 - .5f || z >= bz) continue;
                float g = BraxChampionsWilleDzialki.Grunt(d, x, z); checkedPts++;
                if (m > g + .03f && m > sea - .05f) { over++; maxOver = Math.Max(maxOver, m - g); }
            }
        if (lad > 0) fails += Fail(lad + " points where Lad is not the far land's mesh (worst " + ladWorst + " m)");
        if (over > 0) fails += Fail(over + " points of the beach under the far land's mesh (up to " + maxOver.ToString("F2") + " m)");
        Console.WriteLine("NABRZEZE_MORZE far land: Lad = its mesh; over the beach " + over + " of " + checkedPts + " points");
        // the field and the water, as BraxChampionsNabrzezeMorze.Zbuduj (k = 1)
        Func<float, float, float> grunt = (x, z) => BraxChampionsWilleDzialki.Widoczny(d, x, z);
        var pole = BraxChampionsWyspaWoda.Pole(x0, z0, x1, z1, BraxChampionsWyspaMorze.Krok, 1f, sea, BraxChampionsWyspaMorze.PoleN, grunt, sea - 15f, BraxChampionsWyspaMorze.RantPas);
        if (pole.Odcinki < 50) fails += Fail("waterline only " + pole.Odcinki + " segments");
        int dRise = 0, bedFall = 0; float zw0 = float.MaxValue, zw1 = float.MinValue;
        for (int ix = 0; ix < pole.w; ix++)
        {
            float x = pole.x0 + (ix + .5f) * pole.cw; if (x < bx0 + 1f || x > bx1 - 1f) continue;
            float pd = float.MaxValue, pb = float.MinValue; bool wl = false;
            for (int iz = 0; iz < pole.h; iz++)
            {
                int q = iz * pole.w + ix; float dd = pole.d[q], bb = pole.dno[q], z = pole.z0 + (iz + .5f) * pole.ch;
                if (z > bz) break;                                                                    // the beach's back edge
                if (dd > pd + .75f * Math.Max(pole.cw, pole.ch)) dRise++;
                if (dd > 6f && bb < pb - .05f) bedFall++;
                if (!wl && dd <= 0f) { wl = true; zw0 = Math.Min(zw0, z); zw1 = Math.Max(zw1, z); }
                pd = Math.Min(pd, dd); pb = Math.Max(pb, bb);
            }
            if (!wl) fails += Fail("no waterline at x " + x);
        }
        if (dRise > 0) fails += Fail(dRise + " texels where the distance grows toward the beach");
        if (bedFall > 8) fails += Fail(bedFall + " texels where the bed falls toward the beach");
        Console.WriteLine("NABRZEZE_MORZE field " + pole.w + "x" + pole.h + " waterline=" + pole.Odcinki + " on the beach at z " + zw0.ToString("F1") + ".." + zw1.ToString("F1") + " (loungers at " + (-98f - 45f) + ")");
        if (zw1 > -98f - 45f - 4f) fails += Fail("the waterline within 4 m of the loungers");
        // the beach's sounds play from the waterline chained along the shore (BraxChampionsMorzeBrzeg.Brzeg)
        var brzeg = pole.Brzeg(8f); float skok = 0f; int naPlazy = 0;
        for (int i = 1; i < brzeg.Count; i++) skok = Math.Max(skok, Vector3.Distance(brzeg[i], brzeg[i - 1]));
        foreach (var q in brzeg) if (q.x > bx0 && q.x < bx1) naPlazy++;
        if (naPlazy < 92 / 8 - 1) fails += Fail("only " + naPlazy + " sound points along the private beach");
        if (skok > 20f) fails += Fail("the waterline chain jumps " + skok.ToString("F1") + " m");
        Console.WriteLine("NABRZEZE_MORZE sounds: " + brzeg.Count + " waterline points (" + naPlazy + " on the private beach), longest step " + skok.ToString("F1") + " m");
        var v = new System.Collections.Generic.List<Vector3>(); var t = new System.Collections.Generic.List<int>();
        int dry = BraxChampionsWyspaWoda.Siatka(x0, z0, x1, z1, BraxChampionsWyspaMorze.Krok, BraxChampionsWyspaMorze.Pas, 1f, sea, (x, z) => grunt(x, z) > sea + BraxChampionsWyspaMorze.Suchy, v, t);
        int down = 0, degenerate = 0, outer = 0, inner = 0, innerWet = 0; var uses = new System.Collections.Generic.Dictionary<long, int>();
        void Edge(int a, int c) { long key = a < c ? (long)a * 1000000 + c : (long)c * 1000000 + a; uses[key] = uses.TryGetValue(key, out int u) ? u + 1 : 1; }
        for (int i = 0; i < t.Count; i += 3)
        {
            var n = Vector3.Cross(v[t[i + 1]] - v[t[i]], v[t[i + 2]] - v[t[i]]);
            if (n.y < 0f) down++; if (Math.Abs(n.y) < 1e-6f) degenerate++;
            Edge(t[i], t[i + 1]); Edge(t[i + 1], t[i + 2]); Edge(t[i + 2], t[i]);
        }
        var corners = new[] { new Vector3(x0, sea, z0), new Vector3(x1, sea, z0), new Vector3(x1, sea, z1), new Vector3(x0, sea, z1) };
        foreach (var kv in uses)
        {
            if (kv.Value != 1) continue;
            var a = v[(int)(kv.Key / 1000000)]; var c = v[(int)(kv.Key % 1000000)];
            if (corners.Contains(a) && corners.Contains(c)) { outer++; continue; }
            inner++; if (grunt(a.x, a.z) <= sea + BraxChampionsWyspaMorze.Suchy || grunt(c.x, c.z) <= sea + BraxChampionsWyspaMorze.Suchy) innerWet++;
        }
        if (down > 0 || degenerate > 0) fails += Fail(down + " water triangles face down, " + degenerate + " degenerate");
        if (uses.Values.Any(u => u > 2)) fails += Fail("water edges shared by more than two triangles");
        if (outer != 4) fails += Fail("the water's outer edge in " + outer + " pieces, not the window's four sides");
        if (innerWet > 0) fails += Fail(innerWet + " open water edges off dry land");
        Console.WriteLine("NABRZEZE_MORZE water: triangles=" + t.Count / 3 + " dryCells=" + dry + " outer=" + outer + " inner=" + inner);
        Console.WriteLine(fails == 0 ? "NABRZEZE_MORZE_OK" : "NABRZEZE_MORZE_FAILS " + fails);
        return fails == 0 ? 0 : 1;
    }

    public static void Main(string[] args)
    {
        if (args.Length > 2 && args[0] == "grunt")
        {
            var d = BraxChampionsWilleDzialki.Dzialki[int.Parse(args[1]) - 1]; float x = float.Parse(args[2]);
            for (float z = float.Parse(args[3]); z <= float.Parse(args[4]); z += 1f) Console.WriteLine("GRUNT x=" + x + " z=" + z + " h=" + BraxChampionsWilleDzialki.Grunt(d, x, z).ToString("F2"));
            return;
        }
        if (args.Length > 0 && args[0] == "nabrzeze_morze") { Environment.Exit(NabrzezeMorze()); return; }
        if (args.Length > 1 && args[0] == "dzialka")
        {
            // a villa's site in world nominal metres: its ground, road, walls, fence or jetty and the villa (BraxChampionsWilleDzialki)
            var d = BraxChampionsWilleDzialki.Dzialki[int.Parse(args[1]) - 1];
            var root = new GameObject("preview sites").transform;
            var s = BraxChampionsWilleDzialki.Jedna(root, d);
            if (d.woda)
            {
                // the sea round the site (the region's sea or the far sea in the game), for the picture only
                float y = BraxChampionsWilleDzialki.SeaY * BraxClaudeOsiedle.K, k = BraxClaudeOsiedle.K;
                var sea = new GameObject("preview sea").transform; sea.SetParent(root, false);
                var b = new BraxChampionsMeshBatch();
                b.Quad(BraxChampionsHarbour.Surface("morze", new Color(.16f, .40f, .48f), .08f, 0f),
                    new Vector3(d.x0 - 400f, BraxChampionsWilleDzialki.SeaY, d.z0 - 400f), new Vector3(d.x0 - 400f, BraxChampionsWilleDzialki.SeaY, d.z1 + 400f),
                    new Vector3(d.x1 + 400f, BraxChampionsWilleDzialki.SeaY, d.z1 + 400f), new Vector3(d.x1 + 400f, BraxChampionsWilleDzialki.SeaY, d.z0 - 400f));
                b.Build(sea, "preview sea", "Assets/PlayableGenerated/Mistrzowie/Meshes", false, k);
            }
            MeshDump.Write("wille_geometry.json", root, BraxClaudeOsiedle.K);   // world (x K) back to nominal metres
            Console.WriteLine("DUMP site " + d.nr + " " + d.nazwa + " colliders=" + BraxChampionsWilleDzialki.Kolizje + " fence=" + BraxChampionsWilleDzialki.Ogrodzenie + " quads=" + Rec.Quads.Count);
            return;
        }
        int nr = args.Length > 0 ? int.Parse(args[0]) : 1;
        var parent = new GameObject("preview root").transform;
        var g = BraxChampionsWille.Willa(parent, nr, Vector3.zero, 0f);
        g.SetParent(null, false); g.localPosition = Vector3.zero; g.localRotation = Quaternion.identity; g.localScale = Vector3.one;
        MeshDump.Write("wille_geometry.json", g, 1f);
        Console.WriteLine("DUMP villa " + nr + " colliders=" + BraxChampionsWille.Kolizje + " seats=" + BraxChampionsWille.Siedziska + " quads=" + Rec.Quads.Count);
    }
}
