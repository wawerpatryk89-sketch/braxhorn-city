using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// BRAXHORN CITY: usunięcie „śmiesznych” drzew (płaska zielona kula na pniu) i posadzenie
// w ich miejscu prawdziwych drzew z Poly Haven (CC0), odchudzonych pod telefon w buildzie.
// Las na cyplu, palmy i modele z biblioteki zostają nietknięte.
public static class BraxClaudeTrees
{
    const string PH = "Assets/Source/Claude/PolyHaven/models/";
    static readonly string[] Species = { "tree_small_02", "island_tree_01", "island_tree_02", "jacaranda_tree" };

    [Serializable] sealed class Report { public int removed, planted, trunksRemoved, collidersCut, withLod; public List<string> notes = new List<string>(); public List<Vector3> spots = new List<Vector3>(); }

    static bool FlatGreen(Material m)
    {
        if (!m) return false;
        bool textured = m.HasProperty("_MainTex") && m.GetTexture("_MainTex");
        if (textured) return false;
        var c = m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
        return c.g > .12f && c.g > c.r * 1.15f && c.g > c.b * 1.15f;
    }
    static bool FlatBrown(Material m)
    {
        if (!m) return false;
        bool textured = m.HasProperty("_MainTex") && m.GetTexture("_MainTex");
        var c = m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
        return !textured && c.r > c.b * 1.1f && c.r < .7f && c.g < .55f;
    }
    static bool Protected(Transform t)
    {
        for (var p = t; p; p = p.parent)
        {
            string n = p.name.ToLowerInvariant();
            if (n.StartsWith("claude") || n.Contains("port + beach") || n.Contains("palm") || n.StartsWith("las") || n.Contains("las_") ||
                n.Contains("forest") || n.Contains("gym") || n.Contains("nutrition") || n.Contains("nova") || n.Contains("estate"))
                return true;
        }
        return false;
    }

    static GameObject Load(string id)
    {
        foreach (var p in new[] { PH + id + "/" + id + ".glb", PH + id + "/" + id + ".gltf" })
        {
            if (!File.Exists(p)) continue;
            AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceSynchronousImport);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(p) ?? AssetDatabase.LoadAllAssetsAtPath(p).OfType<GameObject>().FirstOrDefault();
            if (go) return go;
        }
        return null;
    }

    public static void Apply(Transform cityRoot)
    {
        if (!BraxClaudeTowerTerrace.Enabled) return;
        var rep = new Report();
        try { Run(cityRoot, rep); }
        catch (Exception e) { rep.notes.Add("BŁĄD: " + e.Message); Debug.LogError("BRAXHORN_CLAUDE_TREES " + e); }
        Directory.CreateDirectory("Assets/PlayableGenerated/ClaudeGround");
        File.WriteAllText("Assets/PlayableGenerated/ClaudeGround/trees.json", JsonUtility.ToJson(rep, true));
        Debug.Log("BRAXHORN_CLAUDE_TREES " + JsonUtility.ToJson(rep));
    }

    static void Run(Transform cityRoot, Report rep)
    {
        var crowns = new List<Bounds>();
        foreach (var r in BraxClaudeGround.All<MeshRenderer>(cityRoot))
        {
            if (!r.enabled || Protected(r.transform)) continue;
            var mf = r.GetComponent<MeshFilter>(); if (!mf || !mf.sharedMesh) continue;
            if (!r.sharedMaterials.Any(FlatGreen)) continue;
            var b = r.bounds; float w = Mathf.Max(b.size.x, b.size.z), ratio = b.size.x / Mathf.Max(.01f, b.size.z);
            if (ratio < .6f || ratio > 1.65f || mf.sharedMesh.vertexCount > 4000) continue;
            bool crownOnly = b.min.y > .8f && b.size.y > 1.2f && b.size.y < 7f && w > 1.2f && w < 7f;
            bool wholeTree = b.min.y < .5f && b.size.y > 3f && b.size.y < 10f && w > 1.5f && w < 7f;
            if (!crownOnly && !wholeTree) continue;
            r.enabled = false; foreach (var col in r.GetComponents<Collider>()) col.enabled = false;
            crowns.Add(b); rep.removed++;
        }
        // Pnie pod usuniętymi koronami.
        foreach (var r in BraxClaudeGround.All<MeshRenderer>(cityRoot))
        {
            if (!r.enabled || Protected(r.transform) || !r.sharedMaterials.Any(FlatBrown)) continue;
            var b = r.bounds;
            if (Mathf.Max(b.size.x, b.size.z) > 1.2f || b.size.y < .8f || b.size.y > 6f) continue;
            if (!crowns.Any(c => Mathf.Abs(c.center.x - b.center.x) < .8f && Mathf.Abs(c.center.z - b.center.z) < .8f)) continue;
            r.enabled = false; foreach (var col in r.GetComponents<Collider>()) col.enabled = false; rep.trunksRemoved++;
        }
        // Fizyka starego miasta to osobna hierarchia skrzynek: bez tego po usuniętym drzewie
        // zostaje niewidzialny słup. Wycinamy tylko wąski pień nad chodnikiem (y > 0.35 m).
        if (BraxClaudeGround.SourcePhysics)
            foreach (var c in crowns)
            {
                var vol = new Bounds(new Vector3(c.center.x, (.35f + c.max.y) * .5f, c.center.z),
                                     new Vector3(Mathf.Min(c.size.x, 1.4f), Mathf.Max(.2f, c.max.y - .35f), Mathf.Min(c.size.z, 1.4f)));
                try { rep.collidersCut += BraxCityMeshCut.SubtractBoxColliders(BraxClaudeGround.SourcePhysics, vol); }
                catch (Exception e) { rep.notes.Add("kolizja drzewa " + c.center + ": " + e.Message); }
            }
        // Pełna jakość z bliska (oryginał Poly Haven), uproszczona kopia (gltfpack, _lod1) tylko z daleka.
        var sources = Species.Select(id => (full: Load(id), far: Load(id + "_lod1"))).Where(p => p.full).ToArray();
        if (sources.Length == 0) { rep.notes.Add("brak modeli drzew Poly Haven"); return; }
        rep.notes.Add("LOD daleki dostępny dla " + sources.Count(p => p.far) + "/" + sources.Length + " gatunków");
        var root = new GameObject("CLAUDE prawdziwe drzewa").transform; root.SetParent(cityRoot, false); root.position = Vector3.zero;
        Physics.SyncTransforms();
        var rnd = new System.Random(1977);
        foreach (var c in crowns.Take(60))
        {
            var spot = new Vector3(c.center.x, 0, c.center.z);
            float gy = .15f;
            foreach (var h in Physics.RaycastAll(spot + Vector3.up * 30f, Vector3.down, 40f, ~0, QueryTriggerInteraction.Ignore))
                if (h.normal.y > .7f && h.point.y < 1.5f && h.point.y > -1f) { gy = h.point.y; break; }
            var pair = sources[rnd.Next(sources.Length)]; var src = pair.full; int yaw = rnd.Next(360);
            var o = (GameObject)UnityEngine.Object.Instantiate(src, root, false);
            o.name = "Drzewo: " + src.name; o.transform.rotation = Quaternion.Euler(0, yaw, 0) * o.transform.rotation;
            var rs = o.GetComponentsInChildren<Renderer>(true); if (rs.Length == 0) { UnityEngine.Object.DestroyImmediate(o); continue; }
            var b = rs[0].bounds; foreach (var r in rs.Skip(1)) b.Encapsulate(r.bounds);
            float target = Mathf.Clamp(Mathf.Max(c.max.y, 4.5f) * 1.25f, 5f, 9f);
            if (b.size.y > .1f && (b.size.y > 14f || b.size.y < 3f)) { o.transform.localScale *= target / b.size.y; }
            b = rs[0].bounds; foreach (var r in rs.Skip(1)) b.Encapsulate(r.bounds);
            o.transform.position += new Vector3(spot.x - b.center.x, gy - b.min.y, spot.z - b.center.z);
            foreach (var r in rs) { r.shadowCastingMode = ShadowCastingMode.On; r.receiveShadows = true; }
            if (pair.far)
            {
                // Kopia daleka dokładnie w tym samym miejscu i skali co pełny model.
                var f = (GameObject)UnityEngine.Object.Instantiate(pair.far, root, false); f.name = "LOD1 daleki";
                f.transform.rotation = Quaternion.Euler(0, yaw, 0) * f.transform.rotation;
                var frs = f.GetComponentsInChildren<Renderer>(true);
                if (frs.Length > 0)
                {
                    var fb0 = frs[0].bounds; foreach (var r in frs.Skip(1)) fb0.Encapsulate(r.bounds);
                    if (fb0.size.y > .01f) f.transform.localScale *= b.size.y / fb0.size.y;
                    var fb = frs[0].bounds; foreach (var r in frs.Skip(1)) fb.Encapsulate(r.bounds);
                    var nb = rs[0].bounds; foreach (var r in rs.Skip(1)) nb.Encapsulate(r.bounds);
                    f.transform.position += nb.center - fb.center;
                    f.transform.SetParent(o.transform, true);
                    foreach (var r in frs) { r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = true; }
                    var lg = o.AddComponent<LODGroup>();
                    lg.SetLODs(new[] { new LOD(.30f, rs), new LOD(.015f, frs) });
                    lg.RecalculateBounds(); rep.withLod++;
                }
                else UnityEngine.Object.DestroyImmediate(f);
            }
            var trunk = new GameObject("Drzewo: kolizja pnia").AddComponent<CapsuleCollider>();
            trunk.transform.SetParent(root, false); trunk.transform.position = new Vector3(spot.x, gy + 1.5f, spot.z);
            trunk.radius = .25f; trunk.height = 3f;
            rep.planted++; rep.spots.Add(new Vector3(spot.x, gy, spot.z));
        }
    }
}
