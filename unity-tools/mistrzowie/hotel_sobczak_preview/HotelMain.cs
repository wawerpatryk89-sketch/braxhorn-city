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
            if (mf.sharedMesh == null) continue;
            var mr = mf.GetComponent<MeshRenderer>(); var m = mr != null && mr.sharedMaterial != null ? mr.sharedMaterial.name : "?";
            var v = mf.sharedMesh.vertices.Select(p => root.InverseTransformPoint(mf.transform.TransformPoint(p))).ToArray();
            var t = mf.sharedMesh.triangles; tris += t.Length / 3;
            parts.Add("[\"" + mf.gameObject.name.Replace("\"", "'") + "\",\"" + m + "\",[" + string.Join(",", v.Select(V)) + "],[" + string.Join(",", t) + "]]");
        }
        sb.Append(string.Join(",", parts) + "]}");
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
