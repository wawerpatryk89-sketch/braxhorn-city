// Waterfront preview without Unity (owner 9.11: "renderowane zdjęcia przed buildem"): the real foundation geometry
// (Foundation.json from legacy_foundation.py: promenade paving, palm beds, cycle lane, beach, quays, pontoons, sea)
// plus the real generators of the harbour (owner's boat GLBs, fittings, gates, railings), the promenade furniture,
// the kiosks and the beach, dumped in nominal metres for render3d.js.
// usage: promenade_preview <Foundation.json> [out.json]
using System;
using System.Globalization;
using System.IO;
using UnityEngine;

public static class PromenadeProgram
{
    static Material Mat(string role)
    {
        string T(string id, string slug, float tile) => id + "|" + slug + "|" + tile.ToString(CultureInfo.InvariantCulture);
        switch (role)
        {
            case "sand_dry": return new Material { name = T("fnd_sand_dry", "coast_sand_02", 15f), color = new Color(1.46f, 1.37f, 1.17f), textured = true, roughness = .9f };
            case "sand_wet": return new Material { name = T("fnd_sand_wet", "coast_sand_02", 15f), color = new Color(1.16f, 1.07f, .88f), textured = true, roughness = .6f };
            case "paving": case "plot_base": return new Material { name = T("fnd_paving", "concrete_pavement_02", 1.8f), textured = true, roughness = .8f };
            case "prom_paving": return new Material { name = T("fnd_prom_paving", "PavingStones126A", 4.5f), textured = true, roughness = .7f };
            case "asphalt": return new Material { name = T("fnd_asphalt", "clean_asphalt", 2.1f), textured = true, roughness = .85f };
            case "grass": return new Material { name = T("fnd_grass", "Grass004", 1.4f), textured = true, roughness = .9f };
            case "cycle_lane": return new Material { name = T("fnd_cycle_lane", "clean_asphalt", 2f), color = new Color(.74f, .12f, .16f), textured = true, roughness = .8f };
            case "cycle_mark": return new Material { name = T("fnd_cycle_mark", "concrete_pavement_02", 1.8f), color = new Color(.96f, .96f, .91f), textured = true, roughness = .7f };
            case "quay_wood": case "quay_jetski": case "quay_ramp": case "quay_ramp_jetski": case "quay_ramp_along":
                return new Material { name = T("fnd_deck", "Planks040", 2f), color = new Color(.86f, .89f, .94f), textured = true, roughness = .8f };
            case "quay_cruise": case "quay_ferry": return new Material { name = T("fnd_quay_granite", "granite_tile", 1.7f), color = new Color(1.28f, 1.28f, 1.26f), textured = true, roughness = .7f };
            case "quay_wall": return new Material { name = T("fnd_quay_wall", "concrete_wall_008", 1.5f), textured = true, roughness = .8f };
            case "pontoon_side": return new Material { name = T("fnd_pontoon_side", "concrete_wall_008", 1.5f), color = new Color(.20f, .21f, .22f), textured = true, roughness = .6f };
            case "sea": return new Material { name = "fnd_sea", color = new Color(.12f, .25f, .27f), roughness = .12f, metallic = .1f };
            default: return null;
        }
    }

    public static void Main(string[] args)
    {
        Rec.Clear();
        var d = JsonUtility.FromJson<BraxChampionsFoundation.Foundation>(File.ReadAllText(args[0]));
        float K = BraxClaudeOsiedle.K;
        var root = new GameObject("BRAXHORN | MIASTO MISTRZOW | EXTERIOR FOUNDATION").transform;
        var mats = new System.Collections.Generic.Dictionary<string, Material>();
        int meshes = 0;
        foreach (var m in d.meshes)
        {
            // the waterfront: chunks of z -500..0 (z index 00, 01) only
            var parts = m.id.Split('_'); if (parts.Length < 3 || !(parts[1] == "z00" || parts[1] == "z01")) continue;
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
        BraxChampionsFoundation.Optional("plaża (leżaki, wieża ratownika)", () => BraxChampionsBeachFurnishings.Build(root));
        BraxChampionsFoundation.Optional("flota w porcie", () => BraxChampionsHarbour.PlaceOriginalFleet(root, d.berths, d.piers, d.openings));
        BraxChampionsFoundation.Optional("promenada: kwatery palm, ławki, latarnie, kładki", () => BraxChampionsPromenadeFurniture.Build(root, d.palmBeds, d.furniture));
        // palm positions of the waterfront (the owner's palm pack is encrypted: drawn only with the real model)
        foreach (var p in d.plants)
            if (p.kind == "palm" && p.z < -100f)
            { var g = new GameObject(p.id); g.transform.SetParent(root, false); g.transform.position = new Vector3(p.x, .16f, p.z) * K; Rec.PalmObjs.Add(g.transform); }
        MeshDump.Write(args.Length > 1 ? args[1] : "promenade_geometry.json", root, K);
    }
}
