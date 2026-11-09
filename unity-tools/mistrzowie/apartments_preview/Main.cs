using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public static class Program
{
    static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);
    public static void Main(string[] args)
    {
        var ext = new Dictionary<string, Material>();
        foreach (var n in new[] { "arena_black", "light", "steel", "clear_glass", "facade_glass", "arena_gold" }) ext[n] = new Material { name = n };
        foreach (var id in args.Length > 0 ? args : BraxChampionsApartments.Ids)
        {
            Rec.Clear();
            BraxChampionsApartments.Generate(id, ext, "out");
            var sb = new StringBuilder();
            sb.Append("{\"id\":\"" + id + "\",\"colliders\":" + Rec.Cols.Count + ",\"texts\":[" + string.Join(",", Rec.Texts.Select(t => "\"" + t + "\"")) + "],\"quads\":[");
            sb.Append(string.Join(",", Rec.Quads.Select(q => "[\"" + q.g + "\",\"" + q.m + "\"," + (q.inside ? 1 : 0) + "," +
                string.Join(",", new[] { q.a, q.b, q.c, q.d }.Select(v => F(v.x) + "," + F(v.y) + "," + F(v.z))) + "]")));
            sb.Append("]}");
            File.WriteAllText(id + "_geometry.json", sb.ToString());
            Console.WriteLine("DUMP " + id + " quads=" + Rec.Quads.Count + " colliders=" + Rec.Cols.Count + " texts=" + Rec.Texts.Count + " palms=" + Rec.Palms.Count);
        }
    }
}
