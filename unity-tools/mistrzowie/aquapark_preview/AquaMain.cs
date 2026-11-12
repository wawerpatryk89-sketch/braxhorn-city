// AQUAPARK BRAXHORN preview without Unity (the G1 preview, retargeted) (owner: "renderowane zdjęcia przed buildem"): the real
// foundation round the lot (roads, sidewalks, the cut), the real generator (BraxChampionsAquapark), dumped in nominal metres
// for ../hotel_sobczak_preview/geom2bin.py + render3d.js, and the ride lines of the slides (aquapark_slides.json).
// usage: aquapark_preview <Foundation.json> [out.json] [slides.json]
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

public static class AquaProgram
{
    static Material Mat(string role)
    {
        string T(string id, string slug, float tile) => id + "|" + slug + "|" + tile.ToString(CultureInfo.InvariantCulture);
        switch (role)
        {
            case "paving": case "plot_base": return new Material { name = T("fnd_paving", "concrete_pavement_02", 1.8f), textured = true, roughness = .8f };
            case "asphalt": return new Material { name = T("fnd_asphalt", "clean_asphalt", 2.1f), textured = true, roughness = .85f };
            case "grass": return new Material { name = T("fnd_grass", "Grass004", 1.4f), textured = true, roughness = .9f };
            default: return null;
        }
    }

    public static void Main(string[] args)
    {
        Rec.Clear();
        var d = JsonUtility.FromJson<BraxChampionsFoundation.Foundation>(File.ReadAllText(args[0]));
        float K = BraxClaudeOsiedle.K;
        var root = new GameObject("BRAXHORN | MIASTO MISTRZOW | AQUAPARK + surroundings").transform;
        var mats = new System.Collections.Generic.Dictionary<string, Material>();
        int meshes = 0;
        foreach (var m in d.meshes)
        {
            var parts = m.id.Split('_'); if (parts.Length < 3) continue;
            int xi = int.Parse(parts[0].Substring(1)), zi = int.Parse(parts[1].Substring(1));
            if (xi < 1 || xi > 2 || zi < 1 || zi > 4) continue;
            if (!mats.TryGetValue(m.role, out var mat)) mats[m.role] = mat = Mat(m.role);
            if (mat == null) continue;
            var v = new Vector3[m.positions.Length / 3];
            for (int i = 0; i < v.Length; i++) v[i] = new Vector3(m.positions[3 * i], m.positions[3 * i + 1], m.positions[3 * i + 2]);
            var mesh = new Mesh { name = m.id, vertices = v, triangles = m.triangles };
            var o = new GameObject("FOUNDATION " + m.id); o.transform.SetParent(root, false);
            o.AddComponent<MeshFilter>().sharedMesh = mesh; o.AddComponent<MeshRenderer>().sharedMaterial = mat; meshes++;
        }
        Console.WriteLine("FOUNDATION_MESHES " + meshes);
        BraxChampionsFoundation.Optional("aquapark", () => BraxChampionsAquapark.Generate());
        var aq = GameObject.Registry.Find(g => g.transform.parent == null && g.name.StartsWith("AQUAPARK"));
        if (aq != null) aq.transform.SetParent(root, true);
        // the slides' ride lines (world nominal) for the ride check
        var sb = new System.Text.StringBuilder("[");
        foreach (var go in GameObject.Registry.ToArray())
        {
            var z = go.GetComponent<BraxChampionsZjezdzalnia>(); if (z == null) continue;
            if (sb.Length > 1) sb.Append(',');
            sb.Append("{\"name\":\"" + z.nazwa + "\",\"start\":" + V(z.start / K) + ",\"exit\":" + V(z.wyjscie / K) + ",\"line\":[" + string.Join(",", z.trasa.Select(p => V(p / K))) + "]}");
        }
        sb.Append(']');
        File.WriteAllText(args.Length > 2 ? args[2] : "aquapark_slides.json", sb.ToString());
        // preview only: the pool shader is Unity's - here the water gets its own blue, see-through colour; one frame of
        // every waterfall / fountain (BraxChampionsFontanna runs BraxChampionsFontannaKlatka every frame in the game)
        foreach (var go in GameObject.Registry.ToArray())
        {
            var r = go.GetComponent<MeshRenderer>(); var mm = r != null ? r.sharedMaterial : null;
            if (mm != null && mm.name != null && (mm.name.Contains("pool_water") || mm.name.Contains("kids_water") || mm.name.Contains("jacuzzi_water"))) { mm.color = mm.name.Contains("kids") ? new Color(.30f, .78f, .90f, .62f) : new Color(.10f, .55f, .80f, .70f); mm.roughness = .08f; }
        }
        var wm = new Material { name = "preview_fountain_water", color = new Color(.90f, .95f, 1f, .62f), roughness = .1f, emission = new Color(.30f, .32f, .34f) };
        foreach (var go in GameObject.Registry.ToArray())
        {
            var f = go.GetComponent<BraxChampionsFontanna>(); if (f == null) continue;
            var js = new BraxChampionsFontannaKlatka.Strumien[f.a.Length];
            for (int i = 0; i < js.Length; i++) js[i] = new BraxChampionsFontannaKlatka.Strumien { a = f.a[i], b = f.b[i], h = f.h[i], w = f.w[i], n = f.n[i], plusk = f.plusk[i] };
            var V3 = new System.Collections.Generic.List<Vector3>(); var N = new System.Collections.Generic.List<Vector3>(); var U = new System.Collections.Generic.List<Vector2>();
            var C = new System.Collections.Generic.List<Color>(); var T0 = new System.Collections.Generic.List<int>(); var T1 = new System.Collections.Generic.List<int>();
            BraxChampionsFontannaKlatka.Klatka(js, f.lustroC, f.lustroR, f.g, f.lift, 1.3f, V3, N, U, C, T0, T1);
            var T = new System.Collections.Generic.List<int>();
            for (int q = 0; q + 2 < T0.Count; q += 3) if (U[T0[q]].x < .5f || U[T0[q + 1]].x < .5f || U[T0[q + 2]].x < .5f) { T.Add(T0[q]); T.Add(T0[q + 1]); T.Add(T0[q + 2]); }
            go.GetComponent<MeshFilter>().sharedMesh = new Mesh { name = "woda klatka", vertices = V3.ToArray(), triangles = T.ToArray() };
            go.GetComponent<MeshRenderer>().sharedMaterial = wm;
        }
        // the dump is relative to the lot's centre (views in local metres; the preview's palm filter is +-220 m)
        var top = new GameObject("DUMP | aquapark lot centre").transform; top.position = new Vector3(BraxChampionsAquapark.CX, 0, BraxChampionsAquapark.CZ) * K;
        root.SetParent(top, false); root.localPosition = new Vector3(-BraxChampionsAquapark.CX, 0, -BraxChampionsAquapark.CZ) * K;
        MeshDump.Write(args.Length > 1 ? args[1] : "aquapark_geometry.json", top, K);
    }
    static string V(Vector3 p) => "[" + p.x.ToString("0.###", CultureInfo.InvariantCulture) + "," + p.y.ToString("0.###", CultureInfo.InvariantCulture) + "," + p.z.ToString("0.###", CultureInfo.InvariantCulture) + "]";
}
