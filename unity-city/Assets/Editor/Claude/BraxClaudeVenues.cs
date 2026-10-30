using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// BRAXHORN CITY: drzwi do sklepu Nutrition (szklane) i do klubu NOVA (stalowe klubowe)
// zamiast przeskalowanych drzwi z modułu fasady; podłoga NOVA bez świecącej kraty.
// Mechanika otwierania (BraxCityDoor, zawias, kolizja) bez zmian — podmieniamy tylko wygląd skrzydła.
public static class BraxClaudeVenues
{
    [System.Serializable] sealed class Report { public bool shopDoor, clubDoor; public int novaFloorSwapped, novaEmissionOff; public List<string> notes = new List<string>(); }

    static void Box(string name, Transform parent, Vector3 at, Vector3 size, Material m)
    {
        var g = BraxCityArchitecture.Box(name, parent, at, size, m, false);
        g.transform.localPosition = Vector3.zero; g.transform.localRotation = Quaternion.identity; g.transform.localScale = Vector3.one;
    }

    static Transform Leaf(string entranceLabel)
    {
        var root = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(t => t.name == "Source-kit working entrance " + entranceLabel);
        return root ? root.Find("Actual hinged source door leaf") : null;
    }

    static void HideOld(Transform leaf)
    {
        foreach (var r in leaf.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
    }

    public static void Apply()
    {
        if (!BraxClaudeTowerTerrace.Enabled) return;
        var rep = new Report();
        var glass = BraxCityMaterials.Surface("Drzwi: szkło hartowane sklepu", new Color(.70f, .80f, .82f, .22f), .05f, 0f);
        var brushed = BraxCityMaterials.Surface("Drzwi: szczotkowana stal", new Color(.62f, .63f, .64f), .32f, .9f);
        var black = BraxCityMaterials.Surface("Drzwi: czarna stal klubowa", new Color(.06f, .06f, .065f), .55f, .6f);

        // Sklep: szklane skrzydło w stalowej ramie, pionowy uchwyt. Skrzydło 2.1 × 2.7 m (jak kolizja).
        var shop = Leaf("BRAXHORN NUTRITION");
        if (shop)
        {
            HideOld(shop);
            Box("Sklep: szyba drzwi", shop, new Vector3(1.05f, 1.37f, 0), new Vector3(1.94f, 2.56f, .02f), glass);
            Box("Sklep: rama góra", shop, new Vector3(1.05f, 2.67f, 0), new Vector3(2.1f, .06f, .06f), brushed);
            Box("Sklep: rama dół", shop, new Vector3(1.05f, .06f, 0), new Vector3(2.1f, .12f, .06f), brushed);
            Box("Sklep: rama lewa", shop, new Vector3(.03f, 1.37f, 0), new Vector3(.06f, 2.62f, .06f), brushed);
            Box("Sklep: rama prawa", shop, new Vector3(2.07f, 1.37f, 0), new Vector3(.06f, 2.62f, .06f), brushed);
            foreach (float side in new[] { -.06f, .06f })
                Box("Sklep: uchwyt", shop, new Vector3(1.85f, 1.1f, side), new Vector3(.035f, 1.0f, .035f), brushed);
            rep.shopDoor = true;
        }
        else rep.notes.Add("brak drzwi sklepu");

        // NOVA: ciężkie czarne stalowe drzwi klubowe z antypaniką i bulajem.
        var club = Leaf("NOVA");
        if (club)
        {
            HideOld(club);
            Box("NOVA: skrzydło stalowe", club, new Vector3(1.05f, 1.35f, 0), new Vector3(2.08f, 2.68f, .07f), black);
            Box("NOVA: listwa antypaniki", club, new Vector3(1.05f, 1.02f, -.07f), new Vector3(1.5f, .06f, .05f), brushed);
            Box("NOVA: listwa antypaniki (środek)", club, new Vector3(1.05f, 1.02f, .07f), new Vector3(1.5f, .06f, .05f), brushed);
            Box("NOVA: bulaj", club, new Vector3(1.05f, 1.85f, 0), new Vector3(.42f, .42f, .08f), glass);
            Box("NOVA: rama bulaja", club, new Vector3(1.05f, 1.85f, 0), new Vector3(.5f, .5f, .075f), brushed);
            rep.clubDoor = true;
        }
        else rep.notes.Add("brak drzwi NOVA");

        // NOVA: podłoga — świecąca fioletowa krata Codexa zastąpiona ciemnym parkietem;
        // wyłączone świecenie wszystkich powierzchni sali (poza kulą i laserami Claude).
        var hall = new Bounds(new Vector3(43.5f, 1.6f, 48.5f), new Vector3(25f, 3.8f, 15f));
        Material parquet = null;
        string d = "Assets/Source/Claude/PolyHaven/textures/wood_planks/";
        if (File.Exists(d + "diffuse.jpg"))
        {
            var t = new[] { "diffuse.jpg", "normal.jpg", "arm.jpg" }.Select(f => { AssetDatabase.ImportAsset(d + f); return AssetDatabase.LoadAssetAtPath<Texture2D>(d + f); }).ToArray();
            parquet = BraxCityMaterials.Textured("NOVA: ciemny parkiet", t[0], t[1], t[2], 1f / 1.2f);
            parquet.SetColor("_Color", new Color(.30f, .24f, .20f)); EditorUtility.SetDirty(parquet);
        }
        foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!hall.Intersects(r.bounds) || r.name.StartsWith("NOVA:") || r.transform.root.name.StartsWith("CLAUDE")) continue;
            bool claude = false; for (var p = r.transform; p; p = p.parent) if (p.name.StartsWith("CLAUDE")) { claude = true; break; }
            if (claude) continue;
            var mats = r.sharedMaterials; bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i]; if (!m) continue;
                bool emissive = m.IsKeywordEnabled("_EMISSION") || (m.HasProperty("_EmissionColor") && m.GetColor("_EmissionColor").maxColorComponent > .02f);
                var b = r.bounds;
                bool floor = b.size.y < .35f && b.max.y < hall.min.y + .6f && b.size.x * b.size.z > 2f;
                if (floor && parquet && (emissive || m.name.ToLowerInvariant().Contains("dance") || m.name.ToLowerInvariant().Contains("floor") || m.name.ToLowerInvariant().Contains("grid")))
                { mats[i] = parquet; changed = true; rep.novaFloorSwapped++; continue; }
                if (emissive && b.max.y > hall.min.y + 2.6f) continue; // lampy sufitowe zostają
                if (emissive)
                {
                    var c = new Material(m) { name = m.name + " (bez świecenia)" };
                    c.DisableKeyword("_EMISSION"); if (c.HasProperty("_EmissionColor")) c.SetColor("_EmissionColor", Color.black);
                    mats[i] = c; changed = true; rep.novaEmissionOff++;
                }
            }
            if (changed) r.sharedMaterials = mats;
        }
        Directory.CreateDirectory("Assets/PlayableGenerated/ClaudeNova");
        File.WriteAllText("Assets/PlayableGenerated/ClaudeNova/venues.json", JsonUtility.ToJson(rep, true));
        Debug.Log("BRAXHORN_CLAUDE_VENUES " + JsonUtility.ToJson(rep));
    }
}
