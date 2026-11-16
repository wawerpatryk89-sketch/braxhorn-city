// DOWNTOWN preview without Unity (owner: "renderowane zdjęcia przed buildem"): the real tower generator
// (BraxChampionsWiezowce, T08-T13) on the apartments stubs. Every tower is dumped in WORLD nominal metres (its plot, its
// yaw) into one file in the hotel preview format for ../hotel_sobczak_preview/geom2bin.py + render3d.js, with the region's
// foundation tiles round downtown (roads, sidewalks, lawns) from Foundation.json.
// usage: wiezowce_preview <Foundation.json> [out.json] [ids...]
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
    static readonly Dictionary<string, (float x, float z, float yaw)> Plots = new Dictionary<string, (float, float, float)>
    { { "T08", (495, 570, 180) }, { "T09", (610, 570, 180) }, { "T10", (725, 570, 180) }, { "T12", (610, 465, 0) }, { "T13", (725, 465, 0) } };
    static string Fnd(string role)
    {
        switch (role)
        {
            case "paving": case "plot_base": return "fnd_paving|concrete_pavement_02|1.8";
            case "asphalt": return "fnd_asphalt|clean_asphalt|2.1";
            case "grass": return "fnd_grass|sparse_grass|2";
            default: return null;
        }
    }
    public static void Main(string[] args)
    {
        var ext = new Dictionary<string, Material>();
        foreach (var n in new[] { "arena_black", "light", "steel", "clear_glass", "facade_glass", "arena_gold" }) ext[n] = new Material { name = n };
        ext["arena_black"].color = new Color(.05f, .05f, .06f); ext["steel"].color = new Color(.70f, .72f, .74f); ext["steel"].metallic = .9f; ext["steel"].roughness = .3f;
        ext["clear_glass"].color = new Color(.80f, .88f, .92f, .2f); ext["facade_glass"].color = new Color(.30f, .34f, .38f, .48f);
        ext["arena_gold"].color = new Color(.95f, .70f, .26f); ext["light"].color = new Color(1f, .93f, .80f);
        string outFile = args.Length > 1 ? args[1] : "downtown_geometry.json";
        var ids = args.Length > 2 ? args.Skip(2).ToArray() : BraxChampionsWiezowce.Ids;
        var quads = new List<string>(); var meshes = new List<string>(); var models = new List<string>(); var lights = new List<string>();
        var colours = new Dictionary<string, Color>(); var ambient = new Dictionary<string, float[]>();
        float K = BraxClaudeOsiedle.K;
        foreach (var id in ids)
        {
            Rec.Clear(); GameObject.Registry.Clear();
            BraxChampionsWiezowce.Generate(id, ext, "out");
            var root = GameObject.Registry.First(g => g.transform.parent == null && g.name.StartsWith(id + " | ")).transform;
            var p = Plots[id]; var rot = Quaternion.Euler(0, p.yaw, 0); var at = new Vector3(p.x, 0, p.z);
            Vector3 Wv(Vector3 v) => rot * v + at;
            foreach (var q in Rec.Quads)
                quads.Add("[\"" + (id + " " + q.g).Replace("\"", "'") + "\",\"" + q.m + "\"," + (q.inside ? 1 : 0) + "," + string.Join(",", new[] { q.a, q.b, q.c, q.d }.Select(v => V(Wv(v)))) + "]");
            foreach (var kv in Rec.Colours) colours[kv.Key] = kv.Value;
            foreach (var kv in Rec.Ambient) ambient[kv.Key] = kv.Value;
            foreach (var g in GameObject.Registry)
            {
                var pm = g.GetComponent<PreviewModel>(); if (pm == null) continue;
                var top = g.transform; while (top.parent != null) top = top.parent;
                if (top != root) continue;
                var pos = root.InverseTransformPoint(g.transform.position);
                var fw = Quaternion.Inverse(root.rotation) * (g.transform.rotation * Vector3.forward); float yaw = (float)(Math.Atan2(fw.x, fw.z) * 180 / Math.PI) + p.yaw;
                models.Add("{\"path\":\"" + pm.path + "\",\"pos\":[" + V(Wv(pos)) + "],\"yaw\":" + F(yaw) + ",\"template\":false,\"name\":\"" + g.name.Replace("\"", "'") + "\"}");
            }
            foreach (var l in root.gameObject.GetComponentsInChildren<Light>(true))
                lights.Add("[" + V(Wv(root.InverseTransformPoint(l.transform.position))) + "," + F(l.range / K) + "," + F(l.intensity) + ",0]");
            // plain meshes (3D letters, catalogue screens and pieces) in root-local nominal metres -> world
            string tmp = id + "_meshdump.json"; MeshDump.Write(tmp, root, 1f);
            var md = File.ReadAllText(tmp);
            meshes.Add(id + "|" + p.yaw.ToString(CultureInfo.InvariantCulture) + "|" + p.x.ToString(CultureInfo.InvariantCulture) + "|" + p.z.ToString(CultureInfo.InvariantCulture) + "|" + tmp);
            Console.WriteLine("DUMP " + id + " quads=" + Rec.Quads.Count + " height=" + F(BraxChampionsWiezowce.Height(id)) + " colliders=" + Rec.Cols.Count + " texts=" + Rec.Texts.Count);
        }
        var sb = new StringBuilder();
        sb.Append("{\"levels\":[0],\"quads\":[" + string.Join(",", quads) + "],\"meshes\":[],\"models\":[" + string.Join(",", models) + "],\"colours\":{");
        sb.Append(string.Join(",", colours.Select(kv => "\"" + kv.Key + "\":[" + F(kv.Value.r) + "," + F(kv.Value.g) + "," + F(kv.Value.b) + "," + F(kv.Value.a) + "]")));
        sb.Append("},\"ambient\":{" + string.Join(",", ambient.Select(kv => "\"" + kv.Key + "\":[" + string.Join(",", kv.Value.Select(F)) + "]")) + "},\"lights\":[" + string.Join(",", lights) + "],");
        sb.Append("\"meshdumps\":[" + string.Join(",", meshes.Select(m => "\"" + m + "\"")) + "]");
        // the region's ground round downtown (tiles x 300..900, z 250..750), positions already x K in the file
        if (args.Length > 0 && File.Exists(args[0]))
        {
            var d = JsonUtility.FromJson<BraxChampionsFoundation.Foundation>(File.ReadAllText(args[0]));
            var fm = new List<string>();
            foreach (var m in d.meshes)
            {
                var parts = m.id.Split('_'); if (parts.Length < 3) continue;
                int xi = int.Parse(parts[0].Substring(1)), zi = int.Parse(parts[1].Substring(1));
                if (xi < 1 || xi > 2 || zi < 3 || zi > 4) continue;
                var mat = Fnd(m.role); if (mat == null) continue;
                var pts = new List<string>(); for (int i = 0; i < m.positions.Length; i++) pts.Add(F(m.positions[i] / K));
                fm.Add("[\"FOUNDATION " + m.id + "\",\"" + mat + "\",[" + string.Join(",", pts) + "],[" + string.Join(",", m.triangles) + "]]");
            }
            sb.Append(",\"foundation\":[" + string.Join(",", fm) + "]");
        }
        sb.Append("}");
        File.WriteAllText(outFile, sb.ToString());
        Console.WriteLine("DUMP_ALL " + outFile + " quads=" + quads.Count + " models=" + models.Count + " lights=" + lights.Count);
    }
}
