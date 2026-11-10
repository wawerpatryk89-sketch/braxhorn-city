// Runs the real Hotel Sobczak T03 generator on the stubs and dumps the geometry for render.py.
// W-kit quads are recorded in nominal metres (the root is still at the origin while they are built);
// mesh objects (sign, lift landing doors) are converted back to root-local nominal metres.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public static class HotelProgram
{
    static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);
    static string V(Vector3 v) => F(v.x) + "," + F(v.y) + "," + F(v.z);
    public static void Main(string[] args)
    {
        Rec.Clear();
        BraxChampionsHotelSobczakT03.Generate();
        var root = GameObject.Find(BraxChampionsHotelSobczakT03.RootName).transform;
        var sb = new StringBuilder();
        var levels = Enumerable.Range(0, BraxChampionsHotelSobczakT03.Stops).Select(s => F(BraxChampionsHotelSobczakT03.Level(s)));
        sb.Append("{\"levels\":[" + string.Join(",", levels) + "],\"quads\":[");
        sb.Append(string.Join(",", Rec.Quads.Select(q => "[\"" + q.g.Replace("\"", "'") + "\",\"" + q.m + "\"," + (q.inside ? 1 : 0) + "," +
            string.Join(",", new[] { q.a, q.b, q.c, q.d }.Select(V)) + "]")));
        sb.Append("],\"meshes\":[");
        var parts = new List<string>(); int tris = 0;
        foreach (var mf in root.gameObject.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh == null || mf.sharedMesh.vertices.Length == 0) continue;
            var mr = mf.GetComponent<MeshRenderer>(); var m = mr != null && mr.sharedMaterial != null ? mr.sharedMaterial.name : "?";
            var v = mf.sharedMesh.vertices.Select(p => root.InverseTransformPoint(mf.transform.TransformPoint(p))).ToArray();
            var t = mf.sharedMesh.triangles; tris += t.Length / 3;
            parts.Add("[\"" + mf.gameObject.name.Replace("\"", "'") + "\",\"" + m + "\",[" + string.Join(",", v.Select(V)) + "],[" + string.Join(",", t) + "]]");
        }
        sb.Append(string.Join(",", parts) + "],\"models\":[");
        var models = new List<string>();
        foreach (var g in GameObject.Registry)
        {
            var pm = g.GetComponent<PreviewModel>(); if (pm == null) continue;
            var top = g.transform; while (top.parent != null) top = top.parent;
            bool tmpl = top.gameObject.name.StartsWith("HOTEL SOBCZAK | GUEST FLOOR TEMPLATE");
            if (!tmpl && top != root) continue;
            var pos = tmpl ? g.transform.position : root.InverseTransformPoint(g.transform.position);
            var f = g.transform.rotation * Vector3.forward; float yaw = (float)(Math.Atan2(f.x, f.z) * 180 / Math.PI);
            models.Add("{\"path\":\"" + pm.path + "\",\"pos\":[" + V(pos) + "],\"yaw\":" + F(yaw) + ",\"template\":" + (tmpl ? "true" : "false") + ",\"name\":\"" + g.name.Replace("\"", "'") + "\"}");
        }
        sb.Append(string.Join(",", models) + "],\"colours\":{");
        sb.Append(string.Join(",", Rec.Colours.Select(kv => "\"" + kv.Key + "\":[" + F(kv.Value.r) + "," + F(kv.Value.g) + "," + F(kv.Value.b) + "," + F(kv.Value.a) + "]")));
        sb.Append("},\"ambient\":{");
        sb.Append(string.Join(",", Rec.Ambient.Select(kv => "\"" + kv.Key + "\":[" + string.Join(",", kv.Value.Select(F)) + "]")));
        sb.Append("},\"lights\":[");
        var lights = new List<string>();
        foreach (var g in GameObject.Registry)
        {
            var l = g.GetComponent<Light>(); if (l == null) continue;
            var top = g.transform; while (top.parent != null) top = top.parent;
            bool tmpl = top.gameObject.name.StartsWith("HOTEL SOBCZAK | GUEST FLOOR TEMPLATE");
            if (!tmpl && top != root) continue;
            var pos = tmpl ? g.transform.position : root.InverseTransformPoint(g.transform.position);
            float range = tmpl ? l.range : l.range / BraxClaudeOsiedle.K;
            lights.Add("[" + V(pos) + "," + F(range) + "," + F(l.intensity) + "," + (tmpl ? 1 : 0) + "]");
        }
        sb.Append(string.Join(",", lights) + "]}");
        Console.WriteLine("MODELS placed=" + models.Count);
        File.WriteAllText("hotel_geometry.json", sb.ToString());

        var groups = Rec.Quads.GroupBy(q => q.g.Split('|')[0].Trim().Split('/')[0].Trim()).OrderByDescending(g => g.Count());
        foreach (var g in groups) Console.WriteLine("QUADS " + g.Key + " = " + g.Count() + " (" + g.Select(q => q.m).Distinct().Count() + " materials)");
        Console.WriteLine("DUMP quads=" + Rec.Quads.Count + " tris=" + (Rec.Quads.Count * 2) + " meshTris=" + tris + " colliders=" + Rec.Cols.Count + " texts=" + Rec.Texts.Count);
        var lifts = root.gameObject.GetComponentsInChildren<BraxWinda>(true);
        foreach (var w in lifts)
            Console.WriteLine("LIFT " + w.nazwa + " stops=" + w.pietraY.Length + " first=" + F(w.pietraY[0]) + " last=" + F(w.pietraY[w.pietraY.Length - 1]) +
                " doorsL=" + w.przystanekL.Count(x => x) + " doorsR=" + w.przystanekP.Count(x => x) + " cabinY=" + F(w.kabina.position.y) +
                " shaft=" + F(w.szyb.x) + "," + F(w.szyb.y) + " names=" + string.Join("/", w.pietraNazwy.Take(4)) + "../" + w.pietraNazwy.Last());
        foreach (var d in root.gameObject.GetComponentsInChildren<BraxDrzwiAutomatyczne>(true))
            Console.WriteLine("AUTODOOR " + d.gameObject.name + " przesuw=" + F(d.przesuw) + " zasieg=" + F(d.zasieg) + " L=" + (d.skrzydloL ? "ok" : "MISSING") + " P=" + (d.skrzydloP ? "ok" : "MISSING"));
        var st = root.gameObject.GetComponent<BraxHotelSobczakFloorStreamer>();
        Console.WriteLine("STREAMER floors=" + st.guestInteriors.Length + " base=" + F(st.guestBaseY) + " h=" + F(st.floorHeight) + " scale=" + F(st.metreScale));
        Console.WriteLine("LIGHTS " + root.gameObject.GetComponentsInChildren<Light>(true).Length + " (template lights recorded separately)");
        Console.WriteLine("TEXTS " + string.Join(" ", Rec.Texts.Take(12)));
    }
}
