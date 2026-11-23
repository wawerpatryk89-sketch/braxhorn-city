// The 3D grass on the real lawn, without Unity: the region's Foundation.json (the lawn and every other ground surface), its
// trees and the soil mask go through the real mask (BraxChampionsTrawa3DMaska) and the real clump maker
// (BraxChampionsTrawa3DDane.Kepy). Checks: coplanar neighbours (paths, cycle lanes, quays) never block the lawn; no grass
// near a trunk; every clump stands on a lawn cell; a piece made twice is identical; the data size. Writes the clumps
// around one tree (and the ground there) for the three.js picture (trawa_render.js).
//   dotnet run -- Foundation.json soil.raw out_dir
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using UnityEngine;

static class Program
{
    static int Fail(string m) { Console.WriteLine("TRAWA_SIM_FAIL " + m); return 1; }

    static int Main(string[] args)
    {
        var doc = JsonDocument.Parse(File.ReadAllText(args[0])).RootElement;
        var soil = File.ReadAllBytes(args[1]);                     // 1500 rows (z = -500..1000 nominal) x 1200 (x), unflipped
        string outDir = args[2];
        float K = doc.GetProperty("scaleK").GetSingle();
        var meshes = new List<(string role, Vector3[] v, int[] t)>();
        foreach (var m in doc.GetProperty("meshes").EnumerateArray())
        {
            var p = m.GetProperty("positions").EnumerateArray().Select(e => e.GetSingle()).ToArray();
            var v = new Vector3[p.Length / 3]; for (int i = 0; i < v.Length; i++) v[i] = new Vector3(p[i * 3], p[i * 3 + 1], p[i * 3 + 2]);
            meshes.Add((m.GetProperty("role").GetString(), v, m.GetProperty("triangles").EnumerateArray().Select(e => e.GetInt32()).ToArray()));
        }
        var trees = doc.GetProperty("plants").EnumerateArray().Select(e => (x: e.GetProperty("x").GetSingle() * K, z: e.GetProperty("z").GetSingle() * K)).ToList();
        float minx = float.MaxValue, minz = float.MaxValue, maxx = float.MinValue, maxz = float.MinValue;
        foreach (var m in meshes.Where(m => m.role == "grass")) foreach (var v in m.v) { minx = Math.Min(minx, v.x); maxx = Math.Max(maxx, v.x); minz = Math.Min(minz, v.z); maxz = Math.Max(maxz, v.z); }
        var maska = new BraxChampionsTrawa3DMaska(minx, minz, maxx, maxz, 8f, .8f);
        var t0 = DateTime.Now;
        foreach (var m in meshes.Where(m => m.role == "grass"))
            for (int i = 0; i + 2 < m.t.Length; i += 3) maska.Trawnik(m.v[m.t[i]], m.v[m.t[i + 1]], m.v[m.t[i + 2]], true);
        int lawnCells = maska.komorekTrawnika;
        foreach (var m in meshes.Where(m => m.role != "grass"))
            for (int i = 0; i + 2 < m.t.Length; i += 3) maska.Przeszkoda(m.v[m.t[i]], m.v[m.t[i + 1]], m.v[m.t[i + 2]]);
        int groundBlocks = maska.trojkatyPrzeszkod;
        // the ground's own surfaces: only the foam strip on the beach (11.7 m2 over the lawn) may touch it
        foreach (var (x, z) in trees)
        {
            float r = .3f * K, y0 = .2071f, y1 = y0 + 4f * K;
            Vector3 p0 = new Vector3(x - r, y0, z - r), p1 = new Vector3(x + r, y0, z - r), p2 = new Vector3(x + r, y1, z + r), p3 = new Vector3(x - r, y1, z + r);
            maska.Przeszkoda(p0, p1, p2); maska.Przeszkoda(p0, p2, p3);
        }
        float Soil(float x, float z)
        {
            // Texture2D.GetPixelBilinear on the imported mask: texel centres at (i + .5) / size, clamped
            float u = x / (1200f * K) * 1200f - .5f, v = (z + 500f * K) / (1500f * K) * 1500f - .5f;
            int i0 = (int)Math.Floor(u), j0 = (int)Math.Floor(v); float fu = u - i0, fv = v - j0;
            float S(int i, int j) { i = Math.Clamp(i, 0, 1199); j = Math.Clamp(j, 0, 1499); return soil[j * 1200 + i] / 255f; }
            return (S(i0, j0) * (1 - fu) + S(i0 + 1, j0) * fu) * (1 - fv) + (S(i0, j0 + 1) * (1 - fu) + S(i0 + 1, j0 + 1) * fu) * fv;
        }
        maska.Gleba((x, z) => Soil(x, z) > .3f);
        maska.Zakoncz();
        var d = new BraxChampionsTrawa3DDane();
        int latki = maska.Zapisz(d, out int pelne, out int czesciowe);
        double sec = (DateTime.Now - t0).TotalSeconds;
        Console.WriteLine($"TRAWA_SIM mask lawnCells={lawnCells} groundObstacleTriangles={groundBlocks} blocked={maska.komorekZablokowanych} soil={maska.komorekGleby} " +
                          $"edge={maska.komorekKrawedzi} grass={maska.komorekTrawy} patches={latki} full={pelne} partial={czesciowe} bytes={d.bity.Length} " +
                          $"grid={d.nx}x{d.nz} y={maska.minY:F4}..{maska.maxY:F4} seconds={sec:F1}");
        int fails = 0;
        // coplanar ground (cycle lanes, quays, sand at the lawn's height) must not block: only the foam may
        float lawnArea = lawnCells * maska.komorka * maska.komorka, blockedArea = maska.komorekZablokowanych * maska.komorka * maska.komorka;
        if (blockedArea > 60f + trees.Count * 4f * K * K) fails += Fail($"blocked {blockedArea:F0} units2 - the coplanar ground blocks the lawn");
        if (maska.komorekTrawy < lawnCells * .85f) fails += Fail($"grass on {maska.komorekTrawy} of {lawnCells} lawn cells");
        // the clumps: every patch, every clump on a lawn cell, none near a trunk, the same twice
        var buf = new List<Matrix4x4>(); long clumps = 0; float krok = .22f * K; int nearTrunk = 0, offLawn = 0;
        var treeCells = new HashSet<long>(); foreach (var (x, z) in trees) treeCells.Add(((long)Math.Floor(x / 4f) << 32) ^ (uint)Math.Floor(z / 4f));
        for (int l = 0; l < d.Latek; l++)
        {
            buf.Clear(); d.Kepy(l, krok, .85f, 1.2f, buf); clumps += buf.Count;
            foreach (var m in buf)
            {
                int gx = (int)Math.Floor((m.m03 - d.x0) / d.Komorka), gz = (int)Math.Floor((m.m23 - d.z0) / d.Komorka);
                int k = d.latkaKawalek[l]; if (gx / 32 != k % d.nx || gz / 32 != k / d.nx || !d.Trawa(l, gx % 32, gz % 32)) offLawn++;
                if (Math.Abs(m.m13 - (d.latkaY[l] - .01f)) > 1e-4f) offLawn++;
            }
        }
        foreach (var (x, z) in trees)
            for (int l = 0; l < d.Latek; l++)
            {
                int k = d.latkaKawalek[l]; float cx = d.x0 + (k % d.nx + .5f) * d.kawalek, cz = d.z0 + (k / d.nx + .5f) * d.kawalek;
                if (Math.Abs(cx - x) > 8f || Math.Abs(cz - z) > 8f) continue;
                buf.Clear(); d.Kepy(l, krok, .85f, 1.2f, buf);
                foreach (var m in buf) if (Math.Sqrt((m.m03 - x) * (m.m03 - x) + (m.m23 - z) * (m.m23 - z)) < .3f * K + .25f) nearTrunk++;
            }
        if (offLawn > 0) fails += Fail($"{offLawn} clumps off their lawn cell");
        if (nearTrunk > 0) fails += Fail($"{nearTrunk} clumps inside a trunk");
        var a1 = new List<Matrix4x4>(); var a2 = new List<Matrix4x4>(); d.Kepy(d.Latek / 2, krok, .85f, 1.2f, a1); d.Kepy(d.Latek / 2, krok, .85f, 1.2f, a2);
        if (a1.Count != a2.Count || a1.Where((m, i) => m.m03 != a2[i].m03 || m.m23 != a2[i].m23 || m.m00 != a2[i].m00).Any()) fails += Fail("a piece made twice differs");
        float area = maska.komorekTrawy * maska.komorka * maska.komorka;
        Console.WriteLine($"TRAWA_SIM clumps={clumps} perUnit2={clumps / Math.Max(1f, area):F1} near15units~{Math.PI * 15 * 15 * clumps / Math.Max(1f, area):F0} " +
                          $"offLawn={offLawn} nearTrunk={nearTrunk} lawnArea={lawnArea:F0} grassArea={area:F0}");
        // the view: the clumps within 20 units of the first tree, and the ground triangles within 45
        if (args.Length > 3)
        {
            var (tx, tz) = trees[int.Parse(args[3])];
            var inst = new List<float[]>();
            for (int l = 0; l < d.Latek; l++)
            {
                int k = d.latkaKawalek[l]; float cx = d.x0 + (k % d.nx + .5f) * d.kawalek, cz = d.z0 + (k / d.nx + .5f) * d.kawalek;
                if (Math.Abs(cx - tx) > 30f || Math.Abs(cz - tz) > 30f) continue;
                buf.Clear(); d.Kepy(l, krok, .85f, 1.2f, buf);
                foreach (var m in buf) if (Math.Abs(m.m03 - tx) < 24f && Math.Abs(m.m23 - tz) < 24f)
                    inst.Add(new[] { m.m00, m.m01, m.m02, m.m03, m.m10, m.m11, m.m12, m.m13, m.m20, m.m21, m.m22, m.m23 });
            }
            var ground = new List<object>();
            foreach (var m in meshes)
            {
                var tri = new List<float>();
                for (int i = 0; i + 2 < m.t.Length; i += 3)
                {
                    var a = m.v[m.t[i]]; var b = m.v[m.t[i + 1]]; var c = m.v[m.t[i + 2]];
                    float mx = Math.Min(a.x, Math.Min(b.x, c.x)), Mx = Math.Max(a.x, Math.Max(b.x, c.x)), mz = Math.Min(a.z, Math.Min(b.z, c.z)), Mz = Math.Max(a.z, Math.Max(b.z, c.z));
                    if (Mx < tx - 60 || mx > tx + 60 || Mz < tz - 60 || mz > tz + 60) continue;
                    tri.AddRange(new[] { a.x - tx, a.y, a.z - tz, b.x - tx, b.y, b.z - tz, c.x - tx, c.y, c.z - tz });
                }
                if (tri.Count > 0) ground.Add(new { role = m.role, tri });
            }
            File.WriteAllText(Path.Combine(outDir, "trawa_widok.json"), JsonSerializer.Serialize(new
            {
                K, tree = new[] { tx, tz }, inst = inst.Select(a => new[] { a[0], a[1], a[2], a[3] - tx, a[4], a[5], a[6], a[7], a[8], a[9], a[10], a[11] - tz }),
                ground, trees = trees.Where(t => Math.Abs(t.x - tx) < 60 && Math.Abs(t.z - tz) < 60).Select(t => new[] { t.x - tx, t.z - tz })
            }));
            Console.WriteLine($"TRAWA_SIM view tree=({tx:F1},{tz:F1}) clumps={inst.Count}");
        }
        Console.WriteLine(fails == 0 ? "TRAWA_SIM OK" : "TRAWA_SIM FAILED " + fails);
        return fails == 0 ? 0 : 1;
    }
}
