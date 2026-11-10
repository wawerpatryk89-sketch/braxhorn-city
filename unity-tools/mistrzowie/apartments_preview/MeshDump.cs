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
    // a generator may repaint a model (roof dollar): the preview then draws it in that material, not the file's own
    static string Override(GameObject g)
    {
        var m = g.GetComponent<MeshRenderer>()?.sharedMaterial; if (m == null || m.name == "gltf") return "";
        return ",\"material\":{\"colour\":[" + F(m.color.r) + "," + F(m.color.g) + "," + F(m.color.b) + "],\"metallic\":" + F(m.metallic) + ",\"roughness\":" + F(m.roughness) +
            ",\"emission\":[" + F(m.emission.r) + "," + F(m.emission.g) + "," + F(m.emission.b) + "]}";
    }
    // scale: world units per nominal metre (K when the generator multiplies positions by K itself)
    public static void Write(string file, Transform root, float scale, Func<GameObject, bool> skip = null)
    {
        var sb = new StringBuilder("{\"levels\":[0],\"quads\":[");
        // W-kit quads: Unity keeps the build-time world coordinates in a mesh object at the world origin under the group,
        // so they follow the group's later moves: final = group_final * group_at_build^-1 * p. The stub records p in the
        // group's frame at build time (la..ld); take that through the group's final transform into root-local metres.
        // (A hinged door leaf group sits at its hinge at build time: p itself would count the hinge twice.)
        bool Under(Transform t) { for (; t != null; t = t.parent) if (t == root) return true; return false; }
        Vector3 L(Rec.Q q, Vector3 p) => root.InverseTransformPoint(q.t.TransformPoint(p)) / scale;
        var mine = Rec.Quads.Where(q => q.t != null && Under(q.t)).ToList();
        sb.Append(string.Join(",", mine.Select(q => "[\"" + q.g.Replace("\"", "'") + "\",\"" + q.m + "\"," + (q.inside ? 1 : 0) + "," + string.Join(",", new[] { L(q, q.la), L(q, q.lb), L(q, q.lc), L(q, q.ld) }.Select(V)) +
            (q.hasUv ? "," + string.Join(",", new[] { q.ua, q.ub, q.uc, q.ud }.Select(u => F(u.x) + "," + F(u.y))) : "") + "]")));
        sb.Append("],\"meshes\":[");
        var parts = new List<string>(); var mats = new Dictionary<string, Material>(); foreach (var q in mine) mats[q.m] = Rec.Mats[q.m]; int tris = 0, objects = 0;
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
        sb.Append(string.Join(",", parts) + "],\"models\":[");
        // glTF models placed under the root (planters, lobby furniture): position, yaw and uniform scale
        var models = new List<string>();
        foreach (var g in GameObject.Registry.ToArray())
        {
            var pm = g.GetComponent<PreviewModel>(); if (pm == null || !Under(g.transform) || !(pm.path.EndsWith(".gltf") || (pm.path.EndsWith(".glb") && File.Exists(pm.path)))) continue;
            var pos = root.InverseTransformPoint(g.transform.position) / scale;
            var f = g.transform.rotation * Vector3.forward; float yaw = (float)(Math.Atan2(f.x, f.z) * 180 / Math.PI);
            float size = g.transform.TransformVector(Vector3.up).magnitude / scale;   // nominal metres per model metre
            models.Add("{\"path\":\"" + pm.path + "\",\"pos\":[" + V(pos) + "],\"yaw\":" + F(yaw) + ",\"scale\":" + F(size) + ",\"template\":false,\"name\":\"" + g.name.Replace("\"", "'") + "\"" + Override(g) + "}");
        }
        sb.Append(string.Join(",", models) + "],\"palms\":[");
        // city palms (a model pack the preview does not have): positions only, drawn as plain stand-ins
        sb.Append(string.Join(",", Rec.PalmObjs.Select(t => root.InverseTransformPoint(t.position) / scale).Where(p => Math.Abs(p.x) < 220 && Math.Abs(p.z) < 220).Select(p => "[" + V(p) + "]")));
        sb.Append("],\"colours\":{");
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
        Console.WriteLine("DUMP " + file + " quads=" + mine.Count + " objects=" + objects + " meshTris=" + tris + " models=" + models.Count + " materials=" + mats.Count + " lights=" + lights.Count);
    }
}
