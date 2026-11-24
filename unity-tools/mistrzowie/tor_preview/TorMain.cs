using System;
using System.Globalization;
using UnityEngine;

// The quad track without Unity (owner 9.11 23:49: "od teraz renderowane zdjęcia przed buildem"): the site's geometry dumped
// for the three.js render, the centre line and the obstacles printed, the ground checked against the far land.
public static class TorMain
{
    static string F(float v) => v.ToString("0.00", CultureInfo.InvariantCulture);
    public static void Main(string[] args)
    {
        var o = BraxChampionsTorKladow.Linia();
        if (args.Length > 0 && args[0] == "profil")
        {
            // the centre line every 0.5 m: s, x, z, the base, the obstacles, the bank (for the jump check)
            using var w = new System.IO.StreamWriter("tor_profil.csv");
            w.WriteLine("s,x,z,tor,kr");
            for (int i = 0; i < o.N; i++) w.WriteLine(F(o.s[i]) + "," + F(o.x[i]) + "," + F(o.z[i]) + "," + F(BraxChampionsTorKladow.Tor(i, 0f)) + "," + o.kr[i].ToString("0.0000", CultureInfo.InvariantCulture));
            foreach (var p in BraxChampionsTorKladow.Ulozone) Console.WriteLine("PRZESZKODA " + p.typ + " prosta=" + p.prosta + " s0=" + F(p.s0) + " h=" + p.h + " dl=" + F(p.dl));
            Console.WriteLine("TOR length=" + F(o.dlugosc) + " samples=" + o.N);
            Console.WriteLine("FREESTYLE y=" + F(BraxChampionsTorKladow.FreestyleY));
            foreach (var r in BraxChampionsTorKladow.Rampy) Console.WriteLine("RAMPA z=" + r.z + " h=" + r.h + " kat=" + r.kat + " luka=" + r.luka + " dl=" + F(BraxChampionsTorKladow.RampaDl(r)) + " vmin=" + F(BraxChampionsTorKladow.VMin(r) * 3.6f) + "km/h");
            // the race's line: the riding speed every 2 m from the gate (the AI's), the doubles jumped or rolled
            var pw = BraxChampionsTorKladow.ProfilWyscigu();
            using (var wr = new System.IO.StreamWriter("wyscig_profil.csv"))
            {
                wr.WriteLine("k,x,z,y,v_kmh,skok");
                for (int k = 0; k < pw.p.Length; k++) wr.WriteLine(k + "," + F(pw.p[k].x) + "," + F(pw.p[k].z) + "," + F(pw.p[k].y) + "," + F(pw.v[k] * 3.6f) + "," + (pw.skok[k] ? 1 : 0));
            }
            float vmin = float.MaxValue, vmax = 0f, czas = 0f; foreach (var v in pw.v) { vmin = Math.Min(vmin, v); vmax = Math.Max(vmax, v); czas += BraxChampionsTorKladow.KrokWyscigu / v; }
            Console.WriteLine("WYSCIG punkty=" + pw.p.Length + " meta=" + F(pw.sMety) + "m v=" + F(vmin * 3.6f) + ".." + F(vmax * 3.6f) + "km/h okrazenie_AI=" + F(czas) + "s");
            foreach (var d in pw.doubles) Console.WriteLine("WYSCIG " + d);
            return;
        }
        var root = new GameObject("preview tor").transform;
        BraxChampionsTorKladow.Zbuduj(root);
        MeshDump.Write("tor_geometry.json", root, BraxClaudeOsiedle.K, go => go.name.EndsWith(" LOD1") || go.name.EndsWith(" LOD2"));   // the tyres' near level only
        foreach (var m in BraxChampionsKlady.Miejsca) Console.WriteLine("KLAD " + m.napis + " at " + F(m.p.x / BraxClaudeOsiedle.K) + "," + F(m.p.y / BraxClaudeOsiedle.K) + "," + F(m.p.z / BraxClaudeOsiedle.K) + " yaw " + m.yaw);
        Console.WriteLine("DUMP tor length=" + F(o.dlugosc) + " triangles=" + BraxChampionsTorKladow.Trojkaty + " colliders=" + BraxChampionsTorKladow.Kolizje + " fence=" + BraxChampionsTorKladow.Ogrodzenie + " quads=" + Rec.Quads.Count);
    }
}
