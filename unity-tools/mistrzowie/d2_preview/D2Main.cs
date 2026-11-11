// D2 "Dworzec kolejowo-autobusowy" preview without Unity (owner: "renderowane zdjęcia przed buildem"): the real foundation
// round the block X70..X400 / Z70..Z330 (roads, sidewalks, lawns, the D2 cut), the real D2 generator (BraxChampionsD2) and
// the railway hill west of the region (BraxChampionsHorizon.RailHill, drawn in grass here: the game draws it in the far
// land's vertex albedo), dumped in nominal metres for ../hotel_sobczak_preview/geom2bin.py + render3d.js.
// usage: d2_preview <Foundation.json> [out.json]
using System;
using System.Globalization;
using System.IO;
using UnityEngine;

public static class D2Program
{
    static Material Mat(string role)
    {
        string T(string id, string slug, float tile) => id + "|" + slug + "|" + tile.ToString(CultureInfo.InvariantCulture);
        switch (role)
        {
            case "paving": case "plot_base": return new Material { name = T("fnd_paving", "concrete_pavement_02", 1.8f), textured = true, roughness = .8f };
            case "asphalt": return new Material { name = T("fnd_asphalt", "clean_asphalt", 2.1f), textured = true, roughness = .85f };
            case "grass": return new Material { name = T("fnd_grass", "Grass004", 1.4f), textured = true, roughness = .9f };
            case "cycle_lane": return new Material { name = T("fnd_cycle_lane", "clean_asphalt", 2f), color = new Color(.74f, .12f, .16f), textured = true, roughness = .8f };
            case "cycle_mark": return new Material { name = T("fnd_cycle_mark", "concrete_pavement_02", 1.8f), color = new Color(.96f, .96f, .91f), textured = true, roughness = .7f };
            default: return null;
        }
    }

    public static void Main(string[] args)
    {
        Rec.Clear();
        var d = JsonUtility.FromJson<BraxChampionsFoundation.Foundation>(File.ReadAllText(args[0]));
        float K = BraxClaudeOsiedle.K;
        var root = new GameObject("BRAXHORN | MIASTO MISTRZOW | D2 + surroundings").transform;
        var mats = new System.Collections.Generic.Dictionary<string, Material>();
        int meshes = 0;
        foreach (var m in d.meshes)
        {
            var parts = m.id.Split('_'); if (parts.Length < 3) continue;
            int xi = int.Parse(parts[0].Substring(1)), zi = int.Parse(parts[1].Substring(1));
            if (xi > 1 || zi < 1 || zi > 3) continue;
            if (!mats.TryGetValue(m.role, out var mat)) mats[m.role] = mat = Mat(m.role);
            if (mat == null) continue;
            var v = new Vector3[m.positions.Length / 3];
            for (int i = 0; i < v.Length; i++) v[i] = new Vector3(m.positions[3 * i], m.positions[3 * i + 1], m.positions[3 * i + 2]);
            var mesh = new Mesh { name = m.id, vertices = v, triangles = m.triangles };
            var o = new GameObject("FOUNDATION " + m.id); o.transform.SetParent(root, false);
            o.AddComponent<MeshFilter>().sharedMesh = mesh; o.AddComponent<MeshRenderer>().sharedMaterial = mat; meshes++;
        }
        Console.WriteLine("FOUNDATION_MESHES " + meshes);
        // the hill (and a plain far-land stand-in round it at ~2 m)
        var hill = BraxChampionsHorizon.RailHill(K);
        var hm = new Mesh { name = "D2 hill", vertices = hill.v.ToArray(), triangles = hill.t.ToArray() };
        var ho = new GameObject("HORIZON D2 hill"); ho.transform.SetParent(root, false);
        ho.AddComponent<MeshFilter>().sharedMesh = hm;
        ho.AddComponent<MeshRenderer>().sharedMaterial = new Material { name = "hill_grass|Grass004|3", color = new Color(.85f, .95f, .80f), textured = true, roughness = .95f };
        var far = new Mesh { name = "far land", vertices = new[] { new Vector3(-900, 1.7f, -150) * K, new Vector3(-900, 1.7f, 700) * K, new Vector3(-.5f, 1.7f, 700) * K, new Vector3(-.5f, 1.7f, -150) * K }, triangles = new[] { 0, 1, 2, 0, 2, 3 } };
        var fo = new GameObject("HORIZON far land stand-in"); fo.transform.SetParent(root, false);
        fo.AddComponent<MeshFilter>().sharedMesh = far;
        fo.AddComponent<MeshRenderer>().sharedMaterial = new Material { name = "far_grass|Grass004|4", color = new Color(.80f, .90f, .74f), textured = true, roughness = .95f };
        Console.WriteLine("HILL vertices=" + hill.v.Count + " triangles=" + hill.t.Count / 3);
        BraxChampionsFoundation.Optional("D2 dworzec", () => BraxChampionsD2.Generate("out"));
        var d2 = GameObject.Registry.Find(g => g.transform.parent == null && g.name.StartsWith("D2 | "));
        if (d2 != null) d2.transform.SetParent(root, true);
        Console.WriteLine("D2 doors=" + BraxChampionsD2.Doors + " bays=" + BraxChampionsD2.Bays + " accessible=" + BraxChampionsD2.AccessibleBays + " colliders=" + BraxChampionsD2.Colliders + " sleepers=" + BraxChampionsD2.Sleepers + " seats=" + BraxChampionsD2.Seats);
        MeshDump.Write(args.Length > 1 ? args[1] : "d2_geometry.json", root, K);
    }
}
