using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// BRAXHORN CITY: prawdziwe podłoże.
// 1) Cała płaska zielona „trawa” miasta dostaje fotoskan trawy (ambientCG Grass004, CC0),
//    rzutowany w metrach świata, bez widocznego powtarzania.
// 2) Wycinanie trójkątów terenu (np. trawy pod plażą), razem z kolizją.
// Tylko testowy build Claude (ENABLE_TEST_BUILD.txt).
public static class BraxClaudeGround
{
    const string PH = "Assets/Source/Claude/PolyHaven/";
    const string Out = "Assets/PlayableGenerated/ClaudeGround";

    [Serializable] public sealed class Report
    {
        public int grassRenderers, grassMaterialsReplaced, cutTriangles, cutMeshes;
        public List<string> grassMaterials = new List<string>(), cutMeshNames = new List<string>(), notes = new List<string>();
    }
    public static readonly Report report = new Report();

    static Texture2D Tex(string path, bool normal = false)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("brak tekstury", path);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        bool dirty = false;
        if (normal && imp.textureType != TextureImporterType.NormalMap) { imp.textureType = TextureImporterType.NormalMap; dirty = true; }
        if (imp.maxTextureSize != 2048) { imp.maxTextureSize = 2048; dirty = true; }
        if (!imp.mipmapEnabled) { imp.mipmapEnabled = true; dirty = true; }
        if (imp.anisoLevel < 4) { imp.anisoLevel = 4; dirty = true; }
        if (dirty) imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    public static Material WorldMaterial(string name, Texture2D albedo, Texture2D normal, Texture2D arm,
                                         float tile, Color tint, float macroMix = .35f, float bump = 1f)
    {
        var sh = Shader.Find("Braxhorn/ClaudeWorldGround");
        if (!sh) throw new InvalidOperationException("Brak shadera Braxhorn/ClaudeWorldGround");
        Directory.CreateDirectory(Out);
        var m = new Material(sh) { name = name };
        m.SetTexture("_MainTex", albedo);
        if (normal) m.SetTexture("_BumpMap", normal);
        if (arm) m.SetTexture("_ARM", arm);
        m.SetFloat("_Tile", tile); m.SetColor("_Color", tint);
        m.SetFloat("_MacroMix", macroMix); m.SetFloat("_BumpScale", bump);
        string p = Out + "/" + name.Replace(' ', '_').Replace(':', '_') + ".mat";
        if (File.Exists(p)) AssetDatabase.DeleteAsset(p);
        AssetDatabase.CreateAsset(m, p);
        return m;
    }

    public static Material Grass()
    {
        string d = PH + "textures/grass004/";
        return WorldMaterial("Ziemia: prawdziwa trawa Grass004", Tex(d + "diffuse.jpg"), Tex(d + "normal.jpg", true),
            Tex(d + "arm.jpg"), 2.2f, new Color(.92f, .95f, .88f), .38f, 1.1f);
    }

    // Średni kolor materiału: tekstura (jeśli to plik obrazu) × kolor.
    static readonly Dictionary<Texture, Color> avgCache = new Dictionary<Texture, Color>();
    static Color Average(Material m)
    {
        Color tint = m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
        var t = m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
        if (!t) return tint;
        if (!avgCache.TryGetValue(t, out var avg))
        {
            avg = Color.white;
            string p = AssetDatabase.GetAssetPath(t);
            if (!string.IsNullOrEmpty(p) && (p.EndsWith(".png") || p.EndsWith(".jpg") || p.EndsWith(".jpeg")) && File.Exists(p))
            {
                var tmp = new Texture2D(2, 2);
                if (tmp.LoadImage(File.ReadAllBytes(p)))
                {
                    var px = tmp.GetPixels32(); double r = 0, g = 0, b = 0; int n = 0;
                    for (int i = 0; i < px.Length; i += Mathf.Max(1, px.Length / 4096)) { r += px[i].r; g += px[i].g; b += px[i].b; n++; }
                    avg = new Color((float)(r / n / 255), (float)(g / n / 255), (float)(b / n / 255));
                }
                UnityEngine.Object.DestroyImmediate(tmp);
            }
            avgCache[t] = avg;
        }
        return new Color(avg.r * tint.r, avg.g * tint.g, avg.b * tint.b);
    }

    static bool LooksLikeGrass(Material m)
    {
        if (!m) return false;
        string n = (m.name + " " + (m.HasProperty("_MainTex") && m.GetTexture("_MainTex") ? m.GetTexture("_MainTex").name : "")).ToLowerInvariant();
        if (n.Contains("turf") || n.Contains("gym") || n.Contains("braxhorn") || n.Contains("logo")) return false;
        if (n.Contains("grass") || n.Contains("lawn") || n.Contains("trawa") || n.Contains("trawn")) return true;
        var c = Average(m);
        return c.g > .10f && c.g > c.r * 1.12f && c.g > c.b * 1.18f;
    }

    static bool Excluded(Transform t)
    {
        for (var p = t; p; p = p.parent)
        {
            string n = p.name.ToLowerInvariant();
            if (n.Contains("gym") || n.Contains("kickbox") || n.Contains("nutrition") || n.Contains("nova") ||
                n.StartsWith("claude") || n.Contains("port + beach") || n.Contains("estate") || n.Contains("osiedle"))
                return true;
        }
        return false;
    }

    public static void ReplaceGrass(Transform cityRoot)
    {
        if (!BraxClaudeTowerTerrace.Enabled) return;
        Material grass;
        try { grass = Grass(); }
        catch (Exception e) { report.notes.Add("trawa: " + e.Message); Debug.LogError("BRAXHORN_CLAUDE_GROUND " + e); return; }
        foreach (var r in cityRoot.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (Excluded(r.transform)) continue;
            var b = r.bounds;
            if (b.size.y > 3f || Mathf.Max(b.size.x, b.size.z) < 4f || b.max.y > 2.5f || b.min.y < -3f) continue;
            var mats = r.sharedMaterials; bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                if (!LooksLikeGrass(mats[i])) continue;
                if (!report.grassMaterials.Contains(mats[i].name)) report.grassMaterials.Add(mats[i].name);
                mats[i] = grass; changed = true; report.grassMaterialsReplaced++;
            }
            if (changed) { r.sharedMaterials = mats; report.grassRenderers++; }
        }
    }

    // Usuwa trójkąty płaskiego terenu (normalna w górę, wysokość y0..y1) w prostokącie XZ.
    public static void Cut(Transform cityRoot, Rect area, float y0, float y1, Transform except)
    {
        foreach (var mf in cityRoot.GetComponentsInChildren<MeshFilter>(true))
        {
            if (!mf.sharedMesh || (except && mf.transform.IsChildOf(except))) continue;
            var mr = mf.GetComponent<MeshRenderer>(); if (!mr) continue;
            var b = mr.bounds;
            if (b.max.x < area.xMin || b.min.x > area.xMax || b.max.z < area.yMin || b.min.z > area.yMax) continue;
            if (b.min.y > y1 || b.max.y < y0 || b.size.y > 6f) continue;
            var src = mf.sharedMesh; Vector3[] v;
            try { v = src.vertices; } catch { report.notes.Add("nieczytelna siatka " + src.name); continue; }
            if (v == null || v.Length == 0) continue;
            var M = mf.transform.localToWorldMatrix;
            var w = v.Select(p => M.MultiplyPoint3x4(p)).ToArray();
            var copy = UnityEngine.Object.Instantiate(src); copy.name = src.name + "_bez_trawy_pod_plaza";
            int removed = 0;
            for (int s = 0; s < src.subMeshCount; s++)
            {
                var tris = src.GetTriangles(s); var keep = new List<int>(tris.Length);
                for (int i = 0; i < tris.Length; i += 3)
                {
                    Vector3 a = w[tris[i]], bb = w[tris[i + 1]], c = w[tris[i + 2]];
                    var ce = (a + bb + c) / 3f; var nrm = Vector3.Cross(bb - a, c - a).normalized;
                    bool inside = ce.x > area.xMin && ce.x < area.xMax && ce.z > area.yMin && ce.z < area.yMax;
                    if (inside && Mathf.Abs(nrm.y) > .8f && ce.y > y0 && ce.y < y1) { removed++; continue; }
                    keep.Add(tris[i]); keep.Add(tris[i + 1]); keep.Add(tris[i + 2]);
                }
                copy.SetTriangles(keep, s);
            }
            if (removed == 0) { UnityEngine.Object.DestroyImmediate(copy); continue; }
            Directory.CreateDirectory(Out);
            string p = Out + "/" + Guid.NewGuid().ToString("N").Substring(0, 8) + "_cut.asset";
            AssetDatabase.CreateAsset(copy, p);
            foreach (var col in mf.GetComponents<MeshCollider>()) if (col.sharedMesh == src) col.sharedMesh = copy;
            mf.sharedMesh = copy;
            report.cutTriangles += removed; report.cutMeshes++; report.cutMeshNames.Add(mf.name);
        }
        // Osobne siatki kolizji (bez renderera) nad plażą: też wycinamy.
        foreach (var col in cityRoot.GetComponentsInChildren<MeshCollider>(true))
        {
            if (!col.sharedMesh || col.GetComponent<MeshRenderer>() || (except && col.transform.IsChildOf(except))) continue;
            var b = col.bounds;
            if (b.max.x < area.xMin || b.min.x > area.xMax || b.max.z < area.yMin || b.min.z > area.yMax) continue;
            report.notes.Add("osobna kolizja nad plażą zostawiona: " + col.name);
        }
    }

    public static void Save()
    {
        Directory.CreateDirectory(Out);
        File.WriteAllText(Out + "/ground.json", JsonUtility.ToJson(report, true));
        Debug.Log("BRAXHORN_CLAUDE_GROUND " + JsonUtility.ToJson(report));
    }
}
