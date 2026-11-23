// The sea's shore field on the real beach, without Unity: Foundation.json's sand through the real BraxChampionsMorzeBrzeg,
// on the same grid as the game (BraxChampionsPlaza.Pole). Checks: one waterline along the whole beach; the distance grows
// to the open sea and is negative over the sand; the bed meets the sand without a step and only deepens out to sea; the
// run-up's reach on the beach. Writes the field (float32, d then bed per texel) for the three.js picture (plaza_render.js).
//   dotnet run -- Foundation.json out.f32
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
        if (args.Length > 1)
        {
            using var f = new BinaryWriter(File.Create(args[1]));
            for (int i = 0; i < b.d.Length; i++) { f.Write(b.d[i]); f.Write(b.dno[i]); }
        }
        Console.WriteLine(fails == 0 ? "MORZE_SIM OK" : "MORZE_SIM FAILED " + fails);
        return fails == 0 ? 0 : 1;
    }
}
