// E1 "Centrum Rozrywki" preview without Unity (owner: "renderowane zdjęcia przed buildem"): the real foundation round the
// plot (roads Z-70 / Z70, sidewalks, the promenade, the beach, the sea) and the real E1 generator (BraxChampionsE1),
// dumped in nominal metres for ../hotel_sobczak_preview/geom2bin.py + render3d.js.
// usage: e1_preview <Foundation.json> [out.json]
using System;
using System.Globalization;
using System.IO;
using UnityEngine;

public static class E1Program
{
    static Material Mat(string role)
    {
        string T(string id, string slug, float tile) => id + "|" + slug + "|" + tile.ToString(CultureInfo.InvariantCulture);
        switch (role)
        {
            case "sand_dry": return new Material { name = T("fnd_sand_dry", "coast_sand_02", 15f), color = new Color(1.59f, 1.68f, 1.81f), textured = true, roughness = .9f };
            case "sand_wet": return new Material { name = T("fnd_sand_wet", "coast_sand_02", 15f), color = new Color(1.30f, 1.38f, 1.50f), textured = true, roughness = .6f };
            case "paving": case "plot_base": return new Material { name = T("fnd_paving", "concrete_pavement_02", 1.8f), textured = true, roughness = .8f };
            case "prom_paving": return new Material { name = T("fnd_prom_paving", "PavingStones126A", 4.5f), textured = true, roughness = .7f };
            case "asphalt": return new Material { name = T("fnd_asphalt", "clean_asphalt", 2.1f), textured = true, roughness = .85f };
            case "grass": return new Material { name = T("fnd_grass", "Grass004", 1.4f), textured = true, roughness = .9f };
            case "cycle_lane": return new Material { name = T("fnd_cycle_lane", "clean_asphalt", 2f), color = new Color(.74f, .12f, .16f), textured = true, roughness = .8f };
            case "cycle_mark": return new Material { name = T("fnd_cycle_mark", "concrete_pavement_02", 1.8f), color = new Color(.96f, .96f, .91f), textured = true, roughness = .7f };
            case "sea": return new Material { name = "fnd_sea", color = new Color(.12f, .25f, .27f), roughness = .12f, metallic = .1f };
            default: return null;
        }
    }

    public static void Main(string[] args)
    {
        Rec.Clear();
        var d = JsonUtility.FromJson<BraxChampionsFoundation.Foundation>(File.ReadAllText(args[0]));
        float K = BraxClaudeOsiedle.K;
        var root = new GameObject("BRAXHORN | MIASTO MISTRZOW | E1 + surroundings").transform;
        var mats = new System.Collections.Generic.Dictionary<string, Material>();
        int meshes = 0;
        foreach (var m in d.meshes)
        {
            // chunks round the plot: x 300..900 (index 01..02), z -500..250 (00..02)
            var parts = m.id.Split('_'); if (parts.Length < 3) continue;
            int xi = int.Parse(parts[0].Substring(1)), zi = int.Parse(parts[1].Substring(1));
            if (xi < 1 || xi > 2 || zi > 2) continue;
            if (!mats.TryGetValue(m.role, out var mat)) mats[m.role] = mat = Mat(m.role);
            if (mat == null) continue;
            var v = new Vector3[m.positions.Length / 3];
            for (int i = 0; i < v.Length; i++) v[i] = new Vector3(m.positions[3 * i], m.positions[3 * i + 1], m.positions[3 * i + 2]);
            var mesh = new Mesh { name = m.id, vertices = v, triangles = m.triangles };
            var o = new GameObject("FOUNDATION " + m.id); o.transform.SetParent(root, false);
            o.AddComponent<MeshFilter>().sharedMesh = mesh; o.AddComponent<MeshRenderer>().sharedMaterial = mat; meshes++;
        }
        Console.WriteLine("FOUNDATION_MESHES " + meshes);
        BraxChampionsFoundation.Optional("promenada K1-K12", () => BraxChampionsPromenade.Build(root, d.promenade));
        BraxChampionsFoundation.Optional("promenada: kwatery palm, ławki, latarnie", () => BraxChampionsPromenadeFurniture.Build(root, d.palmBeds, d.furniture, d.dining));
        BraxChampionsFoundation.Optional("E1 centrum rozrywki", () => BraxChampionsE1.Generate("out"));
        var e1 = GameObject.Registry.Find(g => g.transform.parent == null && g.name.StartsWith("E1 | "));
        if (e1 != null) e1.transform.SetParent(root, true);
        Console.WriteLine("E1 doors=" + BraxChampionsE1.Doors + " bays=" + BraxChampionsE1.Bays + " pins=" + BraxChampionsE1.Pins + " tables=" + BraxChampionsE1.BilliardTables + " colliders=" + BraxChampionsE1.Colliders);
        MeshDump.Write(args.Length > 1 ? args[1] : "e1_geometry.json", root, K);
    }
}
