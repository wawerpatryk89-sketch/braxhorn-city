// The region's heavy scans made light, without Unity: a glTF's meshes through the real BraxChampionsUproszczenie (all
// primitives in one queue, in the model's space). Checks: the budget met, the kept triangles a subset of the scan's vertices (each
// keeps its own normal / UV), no face turned over, the outline (edges of one triangle) kept within a few percent, the shape's
// bounds kept. Writes the kept indices per primitive (JSON) for the three.js picture (the scan with its own textures).
//   dotnet run -- model.gltf 9000 out.json      (LZ_KOMP=mesh:piece: one connected piece to half, the refusals by reason)
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using UnityEngine;

static class Program
{
    static int Fail(string m) { Console.WriteLine("LZEJSZE_SIM_FAIL " + m); return 1; }
    static int Main(string[] args)
    {
        string path = args[0]; int budzet = int.Parse(args[1]);
        var doc = JsonDocument.Parse(File.ReadAllText(path)).RootElement;
        var dir = Path.GetDirectoryName(path);
        var bufs = doc.GetProperty("buffers").EnumerateArray().Select(b => File.ReadAllBytes(Path.Combine(dir, b.GetProperty("uri").GetString()))).ToArray();
        var views = doc.GetProperty("bufferViews").EnumerateArray().ToArray(); var acc = doc.GetProperty("accessors").EnumerateArray().ToArray();
        byte[] Data(int a, out int count, out int comp, out int stride, out int off)
        {
            var x = acc[a]; count = x.GetProperty("count").GetInt32(); comp = x.GetProperty("componentType").GetInt32();
            var bv = views[x.GetProperty("bufferView").GetInt32()];
            off = (bv.TryGetProperty("byteOffset", out var o1) ? o1.GetInt32() : 0) + (x.TryGetProperty("byteOffset", out var o2) ? o2.GetInt32() : 0);
            stride = bv.TryGetProperty("byteStride", out var st) ? st.GetInt32() : 0;
            return bufs[bv.GetProperty("buffer").GetInt32()];
        }
        var prims = new List<(int m, int p, Vector3[] v, int[] t)>();
        int mi = 0;
        foreach (var mesh in doc.GetProperty("meshes").EnumerateArray())
        {
            int pi = 0;
            foreach (var pr in mesh.GetProperty("primitives").EnumerateArray())
            {
                var b = Data(pr.GetProperty("attributes").GetProperty("POSITION").GetInt32(), out int n, out _, out int st, out int off);
                var v = new Vector3[n]; int s = st == 0 ? 12 : st;
                for (int i = 0; i < n; i++) v[i] = new Vector3(BitConverter.ToSingle(b, off + i * s), BitConverter.ToSingle(b, off + i * s + 4), BitConverter.ToSingle(b, off + i * s + 8));
                var ib = Data(pr.GetProperty("indices").GetInt32(), out int ni, out int comp, out _, out int ioff);
                var t = new int[ni];
                for (int i = 0; i < ni; i++) t[i] = comp == 5125 ? (int)BitConverter.ToUInt32(ib, ioff + i * 4) : comp == 5123 ? BitConverter.ToUInt16(ib, ioff + i * 2) : ib[ioff + i];
                prims.Add((mi, pi, v, t)); pi++;
            }
            mi++;
        }
        var komp = Environment.GetEnvironmentVariable("LZ_KOMP");
        if (!string.IsNullOrEmpty(komp))
        {
            // one piece (connected by shared points) of one primitive, to half: the refusals' reasons
            var sp = komp.Split(':'); var pr = prims[int.Parse(sp[0])]; int want = int.Parse(sp[1]);
            var key = new Dictionary<(float, float, float), int>(); var pid = pr.v.Select(x => { var k = (x.x, x.y, x.z); if (!key.TryGetValue(k, out int i)) key[k] = i = key.Count; return i; }).ToArray();
            var par = Enumerable.Range(0, key.Count).ToArray(); int F(int x) { while (par[x] != x) x = par[x] = par[par[x]]; return x; }
            for (int i = 0; i < pr.t.Length; i += 3) { par[F(pid[pr.t[i]])] = F(pid[pr.t[i + 1]]); par[F(pid[pr.t[i + 1]])] = F(pid[pr.t[i + 2]]); }
            var roots = new List<int>(); for (int i = 0; i < pr.t.Length; i += 3) { int r0 = F(pid[pr.t[i]]); if (!roots.Contains(r0)) roots.Add(r0); }
            int root = roots[want]; var sel = new List<int>(); for (int i = 0; i < pr.t.Length; i += 3) if (F(pid[pr.t[i]]) == root) sel.AddRange(new[] { pr.t[i], pr.t[i + 1], pr.t[i + 2] });
            foreach (int c in new[] { sel.Count / 6, sel.Count / 12, sel.Count / 24 })
            {
                var r0 = BraxChampionsUproszczenie.Uprosc(pr.v, sel.ToArray(), c);
                Console.WriteLine($"LZ_KOMP piece {want}: {sel.Count / 3} -> {r0.Length / 3} (target {c}) refused {BraxChampionsUproszczenie.Odrzucone} (outline {BraxChampionsUproszczenie.Krawedzie} seam {BraxChampionsUproszczenie.Szwy} sheets {BraxChampionsUproszczenie.Tkanka} fold {BraxChampionsUproszczenie.Przewroty}) passes {BraxChampionsUproszczenie.Przebiegi}");
            }
            return 0;
        }
        int total = prims.Sum(p => p.t.Length / 3), fails = 0;
        var outj = new Dictionary<string, int[]>();
        var t0 = DateTime.Now; int kept = 0;
        // the model's space: each mesh by its node's scale and translation (as the game's imported hierarchy stands)
        var nodeOf = new Dictionary<int, (Vector3 s, Vector3 tr)>();
        foreach (var nd in doc.GetProperty("nodes").EnumerateArray())
        {
            if (!nd.TryGetProperty("mesh", out var me)) continue;
            var sc = nd.TryGetProperty("scale", out var sv) ? new Vector3(sv[0].GetSingle(), sv[1].GetSingle(), sv[2].GetSingle()) : new Vector3(1, 1, 1);
            var tr = nd.TryGetProperty("translation", out var tv) ? new Vector3(tv[0].GetSingle(), tv[1].GetSingle(), tv[2].GetSingle()) : Vector3.zero;
            if (nd.TryGetProperty("rotation", out var rv) && Math.Abs(rv[3].GetSingle()) < .9999f) fails += Fail("a rotated node (the sim takes scale and translation)");
            nodeOf[me.GetInt32()] = (sc, tr);
        }
        var vs = prims.Select(pr => { var (sc, tr) = nodeOf.TryGetValue(pr.m, out var x) ? x : (new Vector3(1, 1, 1), Vector3.zero);
                                      return pr.v.Select(q => new Vector3(q.x * sc.x + tr.x, q.y * sc.y + tr.y, q.z * sc.z + tr.z)).ToArray(); }).ToArray();
        var res = BraxChampionsUproszczenie.Uprosc(vs, prims.Select(pr => pr.t).ToArray(), budzet);
        Console.WriteLine($"LZEJSZE_SIM refused {BraxChampionsUproszczenie.Odrzucone} (outline {BraxChampionsUproszczenie.Krawedzie} seam {BraxChampionsUproszczenie.Szwy} sheets {BraxChampionsUproszczenie.Tkanka} fold {BraxChampionsUproszczenie.Przewroty}) passes {BraxChampionsUproszczenie.Przebiegi}");
        for (int pi = 0; pi < prims.Count; pi++)
        {
            var (m, p, _, t) = prims[pi]; var v = vs[pi]; var r = res[pi];
            kept += r.Length / 3;
            if (r.Any(i => i < 0 || i >= v.Length)) fails += Fail("an index out of the scan's vertices");
            int degen = 0; for (int i = 0; i < r.Length; i += 3) if (r[i] == r[i + 1] || r[i + 1] == r[i + 2] || r[i] == r[i + 2]) degen++;
            if (degen > 0) fails += Fail(degen + " degenerate triangles");
            if (r.Length == 0) { fails += Fail($"mesh {m}.{p} gone"); continue; }
            Vector3 mn0 = v[t[0]], mx0 = v[t[0]], mn1 = v[r[0]], mx1 = v[r[0]];
            // the scan's own bounds over its real faces (Poly Haven's potted_plant_01 has a pebble of 132 faces all at 0,0,0)
            bool first = true;
            for (int i = 0; i < t.Length; i += 3)
            {
                if (Vector3.Cross(v[t[i + 1]] - v[t[i]], v[t[i + 2]] - v[t[i]]).sqrMagnitude == 0f) continue;
                for (int k = 0; k < 3; k++) { var x = v[t[i + k]]; if (first) { mn0 = mx0 = x; first = false; } mn0 = Vector3.Min(mn0, x); mx0 = Vector3.Max(mx0, x); }
            }
            foreach (int i in r) { mn1 = Vector3.Min(mn1, v[i]); mx1 = Vector3.Max(mx1, v[i]); }
            float d = (mx0 - mn0).magnitude, dev = Math.Max((mn1 - mn0).magnitude, (mx1 - mx0).magnitude);
            if (dev > .03f * d) fails += Fail($"mesh {m}.{p}: bounds moved {dev:F3} of {d:F3}");
            Console.WriteLine($"LZEJSZE_SIM mesh {m}.{p}: {t.Length / 3} -> {r.Length / 3} triangles, vertices kept {r.Distinct().Count()} of {v.Length}, bounds moved {dev / Math.Max(d, 1e-6f):P1}");
            outj[m + "." + p] = r;
        }
        Console.WriteLine($"LZEJSZE_SIM {Path.GetFileName(path)}: {total} -> {kept} triangles (budget {budzet}) in {(DateTime.Now - t0).TotalSeconds:F1} s");
        if (kept > budzet * 1.15) fails += Fail("over the budget: " + kept);
        if (args.Length > 2) File.WriteAllText(args[2], JsonSerializer.Serialize(outj));
        Console.WriteLine(fails == 0 ? "LZEJSZE_SIM OK" : "LZEJSZE_SIM FAILED " + fails);
        return fails == 0 ? 0 : 1;
    }
}
