// Runs the real apartment generator (T04-T07) on the stubs and dumps each building in the hotel preview format
// (quads, models, colours, per-material ambient, lights) for ../hotel_sobczak_preview/geom2bin.py + render3d.js.
// W-kit quads are nominal metres in the building frame (front = -Z); the root is at the origin while they are built.
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
    static string V(Vector3 v) => F(v.x) + "," + F(v.y) + "," + F(v.z);
    public static void Main(string[] args)
    {
        var ext = new Dictionary<string, Material>();
        foreach (var n in new[] { "arena_black", "light", "steel", "clear_glass", "facade_glass", "arena_gold" }) ext[n] = new Material { name = n };
        ext["arena_black"].color = new Color(.05f, .05f, .06f); ext["steel"].color = new Color(.70f, .72f, .74f);
        ext["clear_glass"].color = new Color(.80f, .88f, .92f, .2f); ext["facade_glass"].color = new Color(.36f, .42f, .47f, .45f);
        ext["arena_gold"].color = new Color(.95f, .70f, .26f); ext["light"].color = new Color(1f, .93f, .80f);
        foreach (var id in args.Length > 0 ? args : BraxChampionsApartments.Ids)
        {
            Rec.Clear(); GameObject.Registry.Clear();
            BraxChampionsApartments.Generate(id, ext, "out");
            var root = GameObject.Registry.First(g => g.transform.parent == null && g.name.StartsWith(id + " | ")).transform;
            var sb = new StringBuilder();
            sb.Append("{\"id\":\"" + id + "\",\"levels\":[" + string.Join(",", Enumerable.Range(0, 8).Select(f => F(BraxChampionsApartments.Level(f)))) + "],");
            sb.Append("\"colliders\":" + Rec.Cols.Count + ",\"texts\":[" + string.Join(",", Rec.Texts.Select(t => "\"" + t + "\"")) + "],\"quads\":[");
            sb.Append(string.Join(",", Rec.Quads.Select(q => "[\"" + q.g.Replace("\"", "'") + "\",\"" + q.m + "\"," + (q.inside ? 1 : 0) + "," +
                string.Join(",", new[] { q.a, q.b, q.c, q.d }.Select(V)) + "]")));
            sb.Append("],\"meshes\":[],\"models\":[");
            var models = new List<string>();
            foreach (var g in GameObject.Registry)
            {
                var pm = g.GetComponent<PreviewModel>(); if (pm == null) continue;
                var top = g.transform; while (top.parent != null) top = top.parent;
                if (top != root) continue;
                var pos = root.InverseTransformPoint(g.transform.position);
                var fw = Quaternion.Inverse(root.rotation) * (g.transform.rotation * Vector3.forward); float yaw = (float)(Math.Atan2(fw.x, fw.z) * 180 / Math.PI);
                models.Add("{\"path\":\"" + pm.path + "\",\"pos\":[" + V(pos) + "],\"yaw\":" + F(yaw) + ",\"template\":false,\"name\":\"" + g.name.Replace("\"", "'") + "\"}");
            }
            sb.Append(string.Join(",", models) + "],\"colours\":{");
            sb.Append(string.Join(",", Rec.Colours.Select(kv => "\"" + kv.Key + "\":[" + F(kv.Value.r) + "," + F(kv.Value.g) + "," + F(kv.Value.b) + "," + F(kv.Value.a) + "]")));
            sb.Append("},\"ambient\":{");
            sb.Append(string.Join(",", Rec.Ambient.Select(kv => "\"" + kv.Key + "\":[" + string.Join(",", kv.Value.Select(F)) + "]")));
            sb.Append("},\"lights\":[");
            var lights = new List<string>();
            foreach (var l in root.gameObject.GetComponentsInChildren<Light>(true))
                lights.Add("[" + V(root.InverseTransformPoint(l.transform.position)) + "," + F(l.range / BraxClaudeOsiedle.K) + "," + F(l.intensity) + ",0]");
            sb.Append(string.Join(",", lights) + "]}");
            File.WriteAllText(id + "_geometry.json", sb.ToString());
            Console.WriteLine("DUMP " + id + " quads=" + Rec.Quads.Count + " models=" + models.Count + " lights=" + lights.Count + " colliders=" + Rec.Cols.Count + " texts=" + Rec.Texts.Count + " palms=" + Rec.Palms.Count);
        }
    }
}
