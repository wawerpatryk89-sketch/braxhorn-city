// The sea's shore field on the real beach, without Unity: Foundation.json's sand through the real BraxChampionsMorzeBrzeg,
// on the same grid as the game (BraxChampionsPlaza.Pole). Checks: one waterline along the whole beach; the distance grows
// to the open sea and is negative over the sand; the bed meets the sand without a step and only deepens out to sea; the
// run-up's reach on the beach. Writes the field (float32, d then bed per texel) for the three.js picture (plaza_render.js).
//   dotnet run -- Foundation.json out.f32
// The villa island (23.11, owner: "ta wyspa w ogóle ma być oddalona kawałek"): its ground grid (float32: x0 z0 x1 z1 krok k
// sea, then the heights, nominal metres, x-major) through the real BraxChampionsWyspaWoda: its field (one closed waterline
// round the island, the distance grows every way out, the bed deep at the rim) and its water (faces up, every inner edge
// shared by two triangles, its outer edge the patch's four sides corner to corner - the far sea meets it there - and its
// inner edge only round dry land).
//   dotnet run -- --wyspa grunt.f32 out_field.f32 out_water.json
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using UnityEngine;

static class Program
{
    static int Fail(string m) { Console.WriteLine("MORZE_SIM_FAIL " + m); return 1; }
    static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--wyspa") return Wyspa(args);
        var doc = JsonDocument.Parse(File.ReadAllText(args[0])).RootElement;
        float sea = float.NaN;
        foreach (var m in doc.GetProperty("meshes").EnumerateArray())
            if (m.GetProperty("role").GetString() == "sea") { sea = m.GetProperty("positions")[1].GetSingle(); break; }
        // the grid as BraxChampionsPlaza.Pole: the sea's extent plus a margin, 1024 x 512
        float sx0 = float.MaxValue, sz0 = float.MaxValue, sx1 = float.MinValue, sz1 = float.MinValue;
        foreach (var m in doc.GetProperty("meshes").EnumerateArray())
        {
            if (m.GetProperty("role").GetString() != "sea") continue;
            var p = m.GetProperty("positions").EnumerateArray().Select(e => e.GetSingle()).ToArray();
            for (int i = 0; i < p.Length; i += 3) { sx0 = Math.Min(sx0, p[i]); sx1 = Math.Max(sx1, p[i]); sz0 = Math.Min(sz0, p[i + 2]); sz1 = Math.Max(sz1, p[i + 2]); }
        }
        var b = new BraxChampionsMorzeBrzeg(sx0 - 8f, sz0 - 8f, sx1 + 8f, sz1 + 40f, 1024, 512, sea);
        var t0 = DateTime.Now;
        foreach (var m in doc.GetProperty("meshes").EnumerateArray())
        {
            string role = m.GetProperty("role").GetString();
            if (role != "sand_dry" && role != "sand_wet") continue;
            var p = m.GetProperty("positions").EnumerateArray().Select(e => e.GetSingle()).ToArray();
            var t = m.GetProperty("triangles").EnumerateArray().Select(e => e.GetInt32()).ToArray();
            Vector3 V(int i) => new Vector3(p[i * 3], p[i * 3 + 1], p[i * 3 + 2]);
            for (int i = 0; i + 2 < t.Length; i += 3) b.Piasek(V(t[i]), V(t[i + 1]), V(t[i + 2]));
        }
        b.Licz();
        double sec = (DateTime.Now - t0).TotalSeconds;
        Console.WriteLine($"MORZE_SIM sea={sea} grid={b.w}x{b.h} rect=({b.x0:F1},{b.z0:F1})-({b.x1:F1},{b.z1:F1}) sandTriangles={b.trojkaty} waterline={b.Odcinki} " +
                          $"sandTexels={b.komorekPiasku} landTexels={b.komorekLadu} d={b.d.Min():F1}..{b.d.Max():F1} seconds={sec:F1}");
        int fails = 0;
        if (b.Odcinki < 200) fails += Fail("waterline too short: " + b.Odcinki);
        // across the beach at x = 420: d grows to the sea, the bed falls, the sand and the bed meet
        int ix = (int)((420f - b.x0) / b.cw); int steps = 0;
        for (int iz = b.h - 1; iz > 0; iz--)
        {
            float z = b.z0 + (iz + .5f) * b.ch; if (z > -320f || z < -640f) continue;
            int i = iz * b.w + ix, j = (iz - 1) * b.w + ix;                                    // j: one texel to the south (seaward)
            if (b.d[j] < b.d[i] - 1e-3f) fails += Fail($"d falls seaward at z={z:F1}: {b.d[i]:F2} -> {b.d[j]:F2}");
            if (b.dno[j] > b.dno[i] + .03f) { steps++; if (steps < 4) Console.WriteLine($"  bed rises seaward at z={z:F1}: {b.dno[i]:F3} -> {b.dno[j]:F3}"); }
        }
        if (steps > 0) fails += Fail(steps + " places where the bed rises toward the sea by more than 3 cm a texel");
        // the run-up's reach: how far over the sand a rise of the water by A climbs (along x = 420)
        foreach (float A in new[] { .15f, .27f })
        {
            float reach = 0f;
            for (int iz = 0; iz < b.h; iz++) { int i = iz * b.w + ix; if (b.d[i] < 0f && b.dno[i] < sea + A) reach = Math.Max(reach, -b.d[i]); }
            Console.WriteLine($"MORZE_SIM runUp A={A} reaches {reach:F1} units ({reach / 1.2944f:F1} m) over the sand at x=420");
        }
        // 24.11 the beach's sounds play from the waterline chained along the shore (BraxChampionsMorzeBrzeg.Brzeg, 8 units)
        {
            var brzeg = b.Brzeg(8f); int skoki = 0; float naj = 0f;
            for (int i = 1; i < brzeg.Count; i++) { float s = MathF.Sqrt((brzeg[i].x - brzeg[i - 1].x) * (brzeg[i].x - brzeg[i - 1].x) + (brzeg[i].z - brzeg[i - 1].z) * (brzeg[i].z - brzeg[i - 1].z)); if (s > 20f) skoki++; else naj = Math.Max(naj, s); }
            Console.WriteLine($"MORZE_SIM sounds: {brzeg.Count} waterline points, {skoki} jumps between pieces of shore, longest step within a piece {naj:F1} units");
            if (brzeg.Count < 50) fails += Fail("too few sound points: " + brzeg.Count);
        }
        if (args.Length > 1)
        {
            using var f = new BinaryWriter(File.Create(args[1]));
            for (int i = 0; i < b.d.Length; i++) { f.Write(b.d[i]); f.Write(b.dno[i]); }
        }
        Console.WriteLine(fails == 0 ? "MORZE_SIM OK" : "MORZE_SIM FAILED " + fails);
        return fails == 0 ? 0 : 1;
    }

    static int Wyspa(string[] args)
    {
        var raw = File.ReadAllBytes(args[1]); var f = new float[raw.Length / 4]; Buffer.BlockCopy(raw, 0, f, 0, raw.Length);
        float x0 = f[0], z0 = f[1], x1 = f[2], z1 = f[3], krok = f[4], k = f[5], sea = f[6];
        int nx = (int)Math.Round((x1 - x0) / krok), nz = (int)Math.Round((z1 - z0) / krok);
        if (f.Length != 7 + (nx + 1) * (nz + 1)) return Fail("ground grid size " + f.Length);
        float Grunt(float x, float z)
        {
            int i = (int)Math.Round((x - x0) / krok), j = (int)Math.Round((z - z0) / krok);
            if (Math.Abs(x0 + i * krok - x) > 1e-3f || Math.Abs(z0 + j * krok - z) > 1e-3f) throw new Exception($"off the grid {x},{z}");
            return f[7 + i * (nz + 1) + j];
        }
        const float Dno = 20f, Rant = 20f, Pas = 10f, Suchy = .5f; const int N = 512;
        float deep = (sea - Dno - 5f) * k;
        var t0 = DateTime.Now;
        var b = BraxChampionsWyspaWoda.Pole(x0, z0, x1, z1, krok, k, sea, N, Grunt, deep, Rant * k);
        Console.WriteLine($"WYSPA_SIM field={b.w}x{b.h} waterline={b.Odcinki} landTexels={b.komorekLadu} d={b.d.Min():F1}..{b.d.Max():F1} bed={b.dno.Min():F1}..{b.dno.Max():F1} seconds={(DateTime.Now - t0).TotalSeconds:F1}");
        int fails = 0;
        if (b.Odcinki < 100) fails += Fail("waterline too short: " + b.Odcinki);
        // the centre of the land: the texel with the most negative d; from it 16 rays out to the rim
        int c = Array.IndexOf(b.d, b.d.Min()); int cx = c % b.w, cz = c / b.w;
        int rises = 0, dFalls = 0; float minRim = float.MaxValue, maxRimBed = float.MinValue;
        for (int a = 0; a < 16; a++)
        {
            double ang = a * Math.PI / 8; bool wet = false; float pd = float.MinValue, pb = float.MaxValue;
            for (float r = 0; ; r += .5f)
            {
                int ix = (int)Math.Round(cx + r * Math.Cos(ang)), iz = (int)Math.Round(cz + r * Math.Sin(ang));
                if (ix < 0 || iz < 0 || ix >= b.w || iz >= b.h) break;
                int i = iz * b.w + ix; float d = b.d[i], bed = b.dno[i];
                if (d > 0f) wet = true;
                if (wet)
                {
                    if (d < pd - .75f * Math.Max(b.cw, b.ch)) dFalls++;
                    if (d > 6f && bed > pb + .05f) rises++;
                    pd = Math.Max(pd, d); pb = Math.Min(pb, bed);
                }
                if (ix == 0 || iz == 0 || ix == b.w - 1 || iz == b.h - 1) { minRim = Math.Min(minRim, d); maxRimBed = Math.Max(maxRimBed, bed); }
            }
            if (!wet) fails += Fail("ray " + a + " never reaches the sea");
        }
        if (dFalls > 0) fails += Fail(dFalls + " places where the distance to the island falls going out");
        if (rises > 8) fails += Fail(rises + " places where the bed rises going out by more than 5 cm a texel");
        if (minRim < 60f) fails += Fail("the rim only " + minRim + " units off the island's waterline (the surf reaches the far sea)");
        if (maxRimBed > deep + .5f) fails += Fail("the bed at the rim " + maxRimBed + " not the open sea's depth " + deep);
        Console.WriteLine($"WYSPA_SIM rim: d>={minRim:F1} units, bed<={maxRimBed:F1} (open sea {deep:F1}); bed rises going out: {rises}");
        // the run-up's reach on the island's beach (as on the region's beach)
        foreach (float A in new[] { .15f, .27f })
        {
            float reach = 0f; for (int i = 0; i < b.d.Length; i++) if (b.d[i] < 0f && b.dno[i] < sea * k + A) reach = Math.Max(reach, -b.d[i]);
            Console.WriteLine($"WYSPA_SIM runUp A={A} climbs up to {reach:F1} units ({reach / k:F1} m) over its sand");
        }
        // ---- the water
        var v = new System.Collections.Generic.List<Vector3>(); var t = new System.Collections.Generic.List<int>();
        int dry = BraxChampionsWyspaWoda.Siatka(x0, z0, x1, z1, krok, Pas, k, sea * k, (x, z) => Grunt(x, z) > sea + Suchy, v, t);
        int down = 0, flat = 0;
        var uses = new System.Collections.Generic.Dictionary<long, int>();
        void Edge(int a, int c2) { long key = a < c2 ? (long)a * 1000000 + c2 : (long)c2 * 1000000 + a; uses[key] = uses.TryGetValue(key, out int u) ? u + 1 : 1; }
        for (int i = 0; i < t.Count; i += 3)
        {
            var n = Vector3.Cross(v[t[i + 1]] - v[t[i]], v[t[i + 2]] - v[t[i]]);
            if (n.y < 0f) down++; if (Math.Abs(n.y) < 1e-6f) flat++;
            Edge(t[i], t[i + 1]); Edge(t[i + 1], t[i + 2]); Edge(t[i + 2], t[i]);
        }
        if (down > 0) fails += Fail(down + " water triangles face down");
        if (flat > 0) fails += Fail(flat + " degenerate water triangles");
        int over = uses.Values.Count(u => u > 2), outer = 0, inner = 0, innerWet = 0;
        var corners = new[] { new Vector3(x0 * k, sea * k, z0 * k), new Vector3(x1 * k, sea * k, z0 * k), new Vector3(x1 * k, sea * k, z1 * k), new Vector3(x0 * k, sea * k, z1 * k) };
        foreach (var kv in uses)
        {
            if (kv.Value != 1) continue;
            var a = v[(int)(kv.Key / 1000000)]; var c2 = v[(int)(kv.Key % 1000000)];
            bool ca = corners.Contains(a), cc = corners.Contains(c2);
            if (ca && cc) { outer++; continue; }
            inner++;
            if (Grunt(a.x / k, a.z / k) <= sea + Suchy || Grunt(c2.x / k, c2.z / k) <= sea + Suchy) innerWet++;
        }
        if (over > 0) fails += Fail(over + " water edges shared by more than two triangles");
        if (outer != 4) fails += Fail("the water's outer edge is " + outer + " pieces, not the patch's four sides (the far sea would meet it mid-edge)");
        if (innerWet > 0) fails += Fail(innerWet + " open water edges off dry land (a hole in the water)");
        Console.WriteLine($"WYSPA_SIM water: vertices={v.Count} triangles={t.Count / 3} dryCells={dry} outerEdges={outer} innerEdges={inner} (round the island)");
        if (args.Length > 2)
        {
            using var w = new BinaryWriter(File.Create(args[2]));
            for (int i = 0; i < b.d.Length; i++) { w.Write(b.d[i]); w.Write(b.dno[i]); }
        }
        if (args.Length > 3)
            File.WriteAllText(args[3], "{\"p\":[" + string.Join(",", v.Select(q => $"{q.x:R},{q.y:R},{q.z:R}")) + "],\"t\":[" + string.Join(",", t) + "]}");
        Console.WriteLine(fails == 0 ? "WYSPA_SIM OK" : "WYSPA_SIM FAILED " + fails);
        return fails == 0 ? 0 : 1;
    }
}
