// Dump for generators that build plain Unity meshes (casino T11, arena): every MeshFilter under a root, in root-local
// nominal metres, with the material's colour, ambient and metallic / roughness / emission, in the hotel preview format.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public static class MeshDump
{
    static string F(float v) => v.ToString("0.####", CultureInfo.InvariantCulture);
    static string V(Vector3 v) => F(v.x) + "," + F(v.y) + "," + F(v.z);
    // scale: world units per nominal metre (K when the generator multiplies positions by K itself)
    public static void Write(string file, Transform root, float scale, Func<GameObject, bool> skip = null)
    {
        var sb = new StringBuilder("{\"levels\":[0],\"quads\":[");
        // W-kit quads are already recorded in root-local nominal metres
        sb.Append(string.Join(",", Rec.Quads.Select(q => "[\"" + q.g.Replace("\"", "'") + "\",\"" + q.m + "\"," + (q.inside ? 1 : 0) + "," + string.Join(",", new[] { q.a, q.b, q.c, q.d }.Select(V)) + "]")));
        sb.Append("],\"meshes\":[");
        var parts = new List<string>(); var mats = new Dictionary<string, Material>(Rec.Mats); int tris = 0, objects = 0;
        foreach (var mf in root.gameObject.GetComponentsInChildren<MeshFilter>(true))
        {
            var mr = mf.GetComponent<MeshRenderer>();
            if (mf.sharedMesh == null || mr == null || mr.sharedMaterial == null || !mr.enabled || (skip != null && skip(mf.gameObject))) continue;
            bool active = true; for (var t = mf.transform; t != null; t = t.parent) active &= t.gameObject.activeSelf; if (!active) continue;
            var m = mr.sharedMaterial; mats[m.name] = m;
            var v = mf.sharedMesh.vertices.Select(p => root.InverseTransformPoint(mf.transform.TransformPoint(p)) / scale).ToArray();
            var tr = mf.sharedMesh.triangles; tris += tr.Length / 3; objects++;
            string uv = m.picture != null && mf.sharedMesh.uv != null && mf.sharedMesh.uv.Length == v.Length ? ",[" + string.Join(",", mf.sharedMesh.uv.Select(u => F(u.x) + "," + F(u.y))) + "]" : "";
            parts.Add("[\"" + mf.gameObject.name.Replace("\"", "'").Replace("\\", "/") + "\",\"" + m.name + "\",[" + string.Join(",", v.Select(V)) + "],[" + string.Join(",", tr) + "]" + uv + "]");
        }
        sb.Append(string.Join(",", parts) + "],\"models\":[],\"colours\":{");
        sb.Append(string.Join(",", mats.Select(kv => "\"" + kv.Key + "\":[" + F(kv.Value.color.r) + "," + F(kv.Value.color.g) + "," + F(kv.Value.color.b) + "," + F(kv.Value.color.a) + "]")));
        sb.Append("},\"ambient\":{");
        sb.Append(string.Join(",", mats.Select(kv => "\"" + kv.Key + "\":[" + string.Join(",", new[] { kv.Value.sky.r, kv.Value.sky.g, kv.Value.sky.b, kv.Value.ground.r, kv.Value.ground.g, kv.Value.ground.b, kv.Value.ambient }.Select(F)) + "]")));
        sb.Append("},\"pbr\":{");
        sb.Append(string.Join(",", mats.Select(kv => "\"" + kv.Key + "\":{\"metallic\":" + F(kv.Value.metallic) + ",\"roughness\":" + F(kv.Value.roughness) + ",\"emission\":[" +
            F(kv.Value.emission.r) + "," + F(kv.Value.emission.g) + "," + F(kv.Value.emission.b) + "]" + (kv.Value.picture != null ? ",\"picture\":\"" + kv.Value.picture + "\"" : "") + "}")));
        sb.Append("},\"lights\":[");
        var lights = new List<string>();
        foreach (var l in root.gameObject.GetComponentsInChildren<Light>(true))
        { var p = root.InverseTransformPoint(l.transform.position) / scale; lights.Add("[" + V(p) + "," + F(l.range / scale) + "," + F(l.intensity) + ",0]"); }
        sb.Append(string.Join(",", lights) + "]}");
        File.WriteAllText(file, sb.ToString());
        Console.WriteLine("DUMP " + file + " objects=" + objects + " meshTris=" + tris + " materials=" + mats.Count + " lights=" + lights.Count);
    }
}
