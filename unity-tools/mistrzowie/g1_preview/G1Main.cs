// G1 "Galeria Główna + kino" preview without Unity (the D2 preview, retargeted) (owner: "renderowane zdjęcia przed buildem"): the real foundation
// round the block X70..X400 / Z70..Z330 (roads, sidewalks, lawns, the D2 cut), the real D2 generator (BraxChampionsD2) and
// the railway hill west of the region (BraxChampionsHorizon.RailHill, drawn in grass here: the game draws it in the far
// land's vertex albedo), dumped in nominal metres for ../hotel_sobczak_preview/geom2bin.py + render3d.js.
// usage: d2_preview <Foundation.json> [out.json]
using System;
using System.Globalization;
using System.IO;
using UnityEngine;

public static class G1Program
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
        var root = new GameObject("BRAXHORN | MIASTO MISTRZOW | G1 + surroundings").transform;
        var mats = new System.Collections.Generic.Dictionary<string, Material>();
        int meshes = 0;
        foreach (var m in d.meshes)
        {
            var parts = m.id.Split('_'); if (parts.Length < 3) continue;
            int xi = int.Parse(parts[0].Substring(1)), zi = int.Parse(parts[1].Substring(1));
            if (xi < 2 || xi > 3 || zi < 1 || zi > 4) continue;
            if (!mats.TryGetValue(m.role, out var mat)) mats[m.role] = mat = Mat(m.role);
            if (mat == null) continue;
            var v = new Vector3[m.positions.Length / 3];
            for (int i = 0; i < v.Length; i++) v[i] = new Vector3(m.positions[3 * i], m.positions[3 * i + 1], m.positions[3 * i + 2]);
            var mesh = new Mesh { name = m.id, vertices = v, triangles = m.triangles };
            var o = new GameObject("FOUNDATION " + m.id); o.transform.SetParent(root, false);
            o.AddComponent<MeshFilter>().sharedMesh = mesh; o.AddComponent<MeshRenderer>().sharedMaterial = mat; meshes++;
        }
        Console.WriteLine("FOUNDATION_MESHES " + meshes);
        BraxChampionsFoundation.Optional("G1 galeria", () => BraxChampionsD2.GenerateG1("out"));
        var g1 = GameObject.Registry.Find(g => g.transform.parent == null && g.name.StartsWith("G1 | "));
        if (g1 != null) g1.transform.SetParent(root, true);
        Console.WriteLine("G1 doors=" + BraxChampionsD2.Doors + " bays=" + BraxChampionsD2.G1Bays + " accessible=" + BraxChampionsD2.G1AccessibleBays + " colliders=" + BraxChampionsD2.Colliders + " seats=" + BraxChampionsD2.Seats + " shops=" + BraxChampionsD2.G1Shops);
        // preview only (owner 11.11 19:30 "rób skalę wszystkiego"): 1.80 m reference figures (nominal, x K like the game) at the
        // points of figures.json [[x, floor y, z, yaw], ...] (nominal world metres) - never part of the game
        if (args.Length > 2 && File.Exists(args[2]))
        {
            var fm = new Material { name = "preview_scale_figure_180cm", color = new Color(.62f, .64f, .68f), roughness = .6f };
            var fb = new BraxChampionsMeshBatch();
            var txt = File.ReadAllText(args[2]).Replace("[", " ").Replace("]", " ").Split(new[] { ',', ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i + 3 < txt.Length; i += 4)
            {
                float x = float.Parse(txt[i], CultureInfo.InvariantCulture), y = float.Parse(txt[i + 1], CultureInfo.InvariantCulture), z = float.Parse(txt[i + 2], CultureInfo.InvariantCulture), yaw = float.Parse(txt[i + 3], CultureInfo.InvariantCulture);
                var b = new Vector3(x - 940f, y, z - 175f); var r = Quaternion.Euler(0, yaw, 0);
                Vector3 P(float a, float h, float d) => b + r * new Vector3(a, h, d);
                foreach (float s in new[] { -1f, 1f }) { fb.Cylinder(fm, P(s * .10f, 0f, 0f), .075f, .86f, 12, Quaternion.identity, true, true, .09f); fb.Cylinder(fm, P(s * .25f, .86f, 0f), .045f, .58f, 10, Quaternion.identity, true, true, .055f); }
                fb.RoundedBox(fm, P(0f, 1.16f, 0f), new Vector3(.40f, .62f, .22f), .08f, r, 2);
                fb.Cylinder(fm, P(0f, 1.47f, 0f), .055f, .09f, 10, Quaternion.identity, true, true);
                var prof = new System.Collections.Generic.List<Vector2>(); for (int k = 0; k <= 8; k++) { float t = Mathf.PI * k / 8f; prof.Add(new Vector2(Mathf.Max(.002f, Mathf.Sin(t) * .115f), .115f - Mathf.Cos(t) * .115f)); }
                fb.Lathe(fm, P(0f, 1.565f, 0f), prof.ToArray(), 16, Quaternion.identity, false, false);
            }
            var fg = new GameObject("PREVIEW | skala: człowiek 1,80 m").transform; fg.SetParent(g1 != null ? g1.transform : root, false);
            fb.Build(fg, "PREVIEW skala 1,80 m", "out", false);
        }
        MeshDump.Write(args.Length > 1 ? args[1] : "g1_geometry.json", root, K);
    }
}
