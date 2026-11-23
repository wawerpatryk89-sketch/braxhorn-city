using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// Owner 23.11 17:59-18:38 ("tak samo trawa ... zmienić globalnie", "żeby ta trawa nie była płaska", "długa, nie jest krótka",
// "to jest dobra trawa"): every lawn of the region grows real 3D grass, the long Bermuda lawn the owner chose (the low
// picture of 18:37). Run once on the finished exploration scene, before it is saved and split into the areas:
//   - the card: Poly Haven Grass Bermuda 01 plants (trawa_bermuda.py, imported by BraxChampionsTrawaKartaImport);
//   - the clump: three cards crossed every 60 degrees, 48 x 24 cm (real size x K), normals up (lit as the lawn);
//   - the material: the card brought to the lawn's own green (the colour the owner accepted, BraxChampionsGrass.LawnLinear,
//     with the shader's root shade measured in), the lawn's own patches and mowing stripes;
//   - where: BraxChampionsTrawa3DMaska over every lawn surface of the scene (the lawn shader, or the same grass photo),
//     less everything that stands on it, the soil under the trees and a 25 cm margin at the edges;
//   - the old near-camera blades (BraxClaudeTrawa: around the plant beds and on the aquapark's lawns) are replaced.
public static class BraxChampionsTrawa3DBuild
{
    const string Source = "Assets/Source/Mistrzowie/", Output = "Assets/PlayableGenerated/Mistrzowie/";
    // nominal metres (the world is x K): the clump's card, the spacing of the clumps
    public const float KartaSzer = .48f, KartaWys = .24f, Krok = .22f;
    // game units: full density near the camera, the farthest clump, the band in which one shrinks into the ground
    public const float Zasieg = 15f, Blisko = 7f, Pas = 3f;
    public const float Kawalek = 8f, Przeszkody = .8f, ZiemiaOd = .3f;
    static float K => BraxClaudeOsiedle.K;

    sealed class Karta { public float[] linear, linear_nasada; public float nasada = .55f, nasada_pas = 1.6f; public int plants; }

    static IEnumerable<T> All<T>(Scene scene) where T : Component
    {
        foreach (var r in scene.GetRootGameObjects()) foreach (var c in r.GetComponentsInChildren<T>(true)) yield return c;
    }

    // a lawn: the lawn shader (not its blade mode), or any material painted with the same grass photo
    static bool Trawnik(Material m, Texture zdjecie)
    {
        if (!m) return false;
        if (m.shader && m.shader.name == "Braxhorn/ChampionsGrass") return !m.HasProperty("_Blade") || m.GetFloat("_Blade") < .5f;
        if (zdjecie && m.HasProperty("_MainTex") && m.GetTexture("_MainTex") == zdjecie) return true;
        string n = m.name;
        return n.EndsWith("willa_trawnik", StringComparison.Ordinal) || n.EndsWith("prom_planter_lawn", StringComparison.Ordinal) ||
               n.EndsWith("tower_lawn", StringComparison.Ordinal);
    }

    public static void Generate(Scene scene, Transform parent)
    {
        var grass = AssetDatabase.LoadAssetAtPath<Material>(Output + "Materials/grass.mat");
        string kartaPng = BraxChampionsTrawaKartaImport.Sciezka, kartaJson = Source + "trawa-karta.json";
        if (!grass || !File.Exists(kartaPng) || !File.Exists(kartaJson))
        {
            BraxBuildQuality.Warn("A1_TRAWA_3D_MISSING grass.mat=" + (bool)grass + " card=" + File.Exists(kartaPng) + " - the lawns keep the flat photo only");
            return;
        }
        var info = JsonUtility.FromJson<Karta>(File.ReadAllText(kartaJson));
        var zdjecie = grass.GetTexture("_MainTex");

        // 1. every lawn surface (upward triangles of lawn materials) and its extent
        var renderers = All<MeshRenderer>(scene).Where(r => r.enabled && r.gameObject.activeInHierarchy && r.shadowCastingMode != ShadowCastingMode.ShadowsOnly).ToList();
        var lawnMats = new SortedDictionary<string, int>(); var suspects = new SortedSet<string>();
        var lawnR = new List<MeshRenderer>(); var bounds = new Bounds(); bool any = false;
        foreach (var r in renderers)
        {
            var mats = r.sharedMaterials; bool lawn = false;
            foreach (var m in mats)
            {
                if (Trawnik(m, zdjecie)) { lawn = true; lawnMats[m.name] = lawnMats.TryGetValue(m.name, out int c) ? c + 1 : 1; }
                else if (m && (m.name.IndexOf("lawn", StringComparison.OrdinalIgnoreCase) >= 0 || m.name.IndexOf("trawn", StringComparison.OrdinalIgnoreCase) >= 0 ||
                               m.name.IndexOf("grass", StringComparison.OrdinalIgnoreCase) >= 0)) suspects.Add(m.name + " (" + (m.shader ? m.shader.name : "-") + ")");
            }
            if (!lawn) continue;
            lawnR.Add(r);
            if (!any) { bounds = r.bounds; any = true; } else bounds.Encapsulate(r.bounds);
        }
        if (!any) { BraxBuildQuality.Warn("A1_TRAWA_3D_NO_LAWN no lawn surface in the scene"); return; }
        var maska = new BraxChampionsTrawa3DMaska(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z, Kawalek, Przeszkody);
        int unreadable = 0;
        var cache = new Dictionary<Mesh, (Vector3[] v, int[][] t)>();
        (Vector3[] v, int[][] t) Siatka(Mesh mesh)
        {
            if (cache.TryGetValue(mesh, out var d)) return d;
            try
            {
                var t = new int[mesh.subMeshCount][];
                for (int i = 0; i < t.Length; i++) t[i] = mesh.GetTriangles(i);
                d = (mesh.vertices, t);
            }
            catch (Exception) { unreadable++; d = (null, null); }
            if (mesh.vertexCount < 60000) cache[mesh] = d;
            return d;
        }
        foreach (var r in lawnR)
        {
            var mf = r.GetComponent<MeshFilter>(); if (!mf || !mf.sharedMesh) continue;
            var (v, t) = Siatka(mf.sharedMesh); if (v == null) continue;
            var mats = r.sharedMaterials; var m = r.localToWorldMatrix;
            for (int s = 0; s < t.Length && s < mats.Length; s++)
            {
                if (!Trawnik(mats[s], zdjecie)) continue;
                bool zMaska = mats[s].HasProperty("_SoilMix") && mats[s].GetFloat("_SoilMix") > 0f && mats[s].GetTexture("_Mask");
                var tri = t[s];
                for (int i = 0; i + 2 < tri.Length; i += 3)
                {
                    Vector3 a = m.MultiplyPoint3x4(v[tri[i]]), b = m.MultiplyPoint3x4(v[tri[i + 1]]), c = m.MultiplyPoint3x4(v[tri[i + 2]]);
                    var n = Vector3.Cross(b - a, c - a); float len = n.magnitude;
                    if (len < 1e-8f || Mathf.Abs(n.y) / len < .95f) continue;          // the lawn's top only
                    maska.Trawnik(a, b, c, zMaska);
                }
            }
        }
        // 2. everything else standing on a lawn (renderers reaching from the lawn up to Przeszkody over it)
        var old = new HashSet<GameObject>(All<BraxClaudeTrawa>(scene).Select(c => c.gameObject));
        var players = All<BraxGymPlayer>(scene).Select(c => c.transform).ToList();      // the player walks through the grass
        int obstacleRenderers = 0;
        foreach (var r in renderers)
        {
            var b = r.bounds;
            if (b.max.y <= maska.minY + .005f || b.min.y >= maska.maxY + Przeszkody) continue;
            if (b.max.x < bounds.min.x || b.min.x > bounds.max.x || b.max.z < bounds.min.z || b.min.z > bounds.max.z) continue;
            if (old.Contains(r.gameObject) || players.Any(p => r.transform.IsChildOf(p))) continue;
            var mf = r.GetComponent<MeshFilter>(); if (!mf || !mf.sharedMesh) continue;
            var mats = r.sharedMaterials; var m = r.localToWorldMatrix;
            var (v, t) = Siatka(mf.sharedMesh);
            if (v == null)
            {
                // unreadable model: its box, if it stands on the lawn at all
                var c = b.center; var e = b.extents;
                Vector3 p0 = new Vector3(c.x - e.x, b.min.y, c.z - e.z), p1 = new Vector3(c.x + e.x, b.min.y, c.z - e.z),
                        p2 = new Vector3(c.x + e.x, b.max.y, c.z + e.z), p3 = new Vector3(c.x - e.x, b.max.y, c.z + e.z);
                maska.Przeszkoda(p0, p1, p2); maska.Przeszkoda(p0, p2, p3); obstacleRenderers++;
                continue;
            }
            bool counted = false;
            for (int s = 0; s < t.Length; s++)
            {
                if (s < mats.Length && Trawnik(mats[s], zdjecie)) continue;           // a lawn is no obstacle to itself
                var tri = t[s];
                for (int i = 0; i + 2 < tri.Length; i += 3)
                {
                    Vector3 a = m.MultiplyPoint3x4(v[tri[i]]), bb = m.MultiplyPoint3x4(v[tri[i + 1]]), c = m.MultiplyPoint3x4(v[tri[i + 2]]);
                    maska.Przeszkoda(a, bb, c);
                }
                counted = true;
            }
            if (counted) obstacleRenderers++;
        }
        // 3. the soil under the trees: the lawn shader's own mask, read the way the shader samples it
        var maskTex = grass.GetTexture("_Mask") as Texture2D; Texture2D gleba = null;
        if (maskTex)
        {
            string path = AssetDatabase.GetAssetPath(maskTex);
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                gleba = new Texture2D(2, 2, TextureFormat.RGBA32, false, true) { wrapMode = TextureWrapMode.Clamp };
                if (!gleba.LoadImage(File.ReadAllBytes(path))) gleba = null;
            }
        }
        if (gleba)
        {
            var rect = grass.GetVector("_MaskRect");
            maska.Gleba((x, z) => gleba.GetPixelBilinear((x - rect.x) * rect.z, (z - rect.y) * rect.w).r > ZiemiaOd);
        }
        else BraxBuildQuality.Warn("A1_TRAWA_3D_SOIL_MASK unreadable - grass may grow on the soil rings under the trees");
        maska.Zakoncz();

        // 4. the data, the clump, the material
        Directory.CreateDirectory(Output + "Meshes"); Directory.CreateDirectory(Output + "Materials");
        var dane = ScriptableObject.CreateInstance<BraxChampionsTrawa3DDane>();
        int latki = maska.Zapisz(dane, out int pelne, out int czesciowe);
        string danePath = Output + "trawa3d_dane.asset"; AssetDatabase.DeleteAsset(danePath); AssetDatabase.CreateAsset(dane, danePath);
        var mesh = Kepa(KartaSzer * K, KartaWys * K);
        string meshPath = Output + "Meshes/trawa3d_kepa.asset"; AssetDatabase.DeleteAsset(meshPath); AssetDatabase.CreateAsset(mesh, meshPath);
        var mat = Material(grass, info);
        string matPath = Output + "Materials/trawa3d_karta.mat"; AssetDatabase.DeleteAsset(matPath); AssetDatabase.CreateAsset(mat, matPath);

        // 5. the old blades go, the new grass comes
        int oldTufts = 0, oldObjects = 0;
        foreach (var c in All<BraxClaudeTrawa>(scene).ToList())
        {
            oldTufts += c.dane && c.dane.kepy != null ? c.dane.kepy.Length : 0; oldObjects++;
            var g = c.gameObject; UnityEngine.Object.DestroyImmediate(c);
            if (g.transform.childCount == 0 && g.GetComponents<Component>().Length == 1) UnityEngine.Object.DestroyImmediate(g);
        }
        var o = new GameObject("Miasto Mistrzow | trawa 3D (Poly Haven Grass Bermuda 01)");
        if (parent) o.transform.SetParent(parent, false);
        var draw = o.AddComponent<BraxChampionsTrawa3D>();
        draw.dane = dane; draw.siatka = mesh; draw.material = mat;
        draw.krok = Krok * K; draw.zasieg = Zasieg; draw.skalaMin = .85f; draw.skalaMax = 1.2f; draw.naKlatke = 8;
        EditorUtility.SetDirty(draw);

        long cells = maska.komorekTrawy; float area = cells * maska.komorka * maska.komorka;
        Debug.Log("A1_TRAWA_3D lawnMaterials=[" + string.Join(", ", lawnMats.Select(p => p.Key + " x" + p.Value)) + "] lawnRenderers=" + lawnR.Count +
                  " lawnTriangles=" + maska.trojkatyTrawnika + " lawnCells=" + maska.komorekTrawnika + " blocked=" + maska.komorekZablokowanych +
                  " soil=" + maska.komorekGleby + " edge=" + maska.komorekKrawedzi + " grassCells=" + cells + " grassArea=" + Mathf.RoundToInt(area) +
                  " units2 obstacleRenderers=" + obstacleRenderers + " obstacleTriangles=" + maska.trojkatyPrzeszkod + " unreadable=" + unreadable +
                  " patches=" + latki + " full=" + pelne + " partial=" + czesciowe + " bytes=" + dane.bity.Length +
                  " card=" + info.plants + " plants replacedOldBlades=" + oldObjects + " (" + oldTufts + " tufts)");
        if (suspects.Count > 0) Debug.Log("A1_TRAWA_3D_NOT_LAWN (named like a lawn, not grown): " + string.Join(", ", suspects));
        if (cells == 0) BraxBuildQuality.Warn("A1_TRAWA_3D_EMPTY no lawn cell left for the grass");
        if (gleba) UnityEngine.Object.DestroyImmediate(gleba);
    }

    // three cards crossed every 60 degrees, roots 3 cm under the ground (a leaning clump never floats), normals up
    static Mesh Kepa(float w, float h)
    {
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
        float pod = .03f * K;
        for (int q = 0; q < 3; q++)
        {
            float a = q * Mathf.PI / 3f; var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (w * .5f);
            int b = v.Count;
            v.Add(-d + Vector3.down * pod); v.Add(d + Vector3.down * pod); v.Add(d + Vector3.up * h); v.Add(-d + Vector3.up * h);
            uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(1, 0)); uv.Add(new Vector2(1, 1)); uv.Add(new Vector2(0, 1));
            tri.AddRange(new[] { b, b + 2, b + 1, b, b + 3, b + 2 });
        }
        var mesh = new Mesh { name = "Mistrzowie trawa 3D kepa" };
        mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetTriangles(tri, 0);
        mesh.SetNormals(Enumerable.Repeat(Vector3.up, v.Count).ToList());
        mesh.RecalculateBounds();
        var bb = mesh.bounds; bb.Expand(new Vector3(.1f, .1f, .1f) * K); mesh.bounds = bb;
        return mesh;
    }

    static Material Material(Material grass, Karta info)
    {
        var shader = Shader.Find("Braxhorn/ChampionsTrawaKarta");
        if (!shader) throw new InvalidOperationException("Champions 3D grass shader missing");
        var karta = AssetDatabase.LoadAssetAtPath<Texture2D>(BraxChampionsTrawaKartaImport.Sciezka);
        var m = new Material(shader) { name = "Mistrzowie trawa 3D", enableInstancing = true, renderQueue = (int)RenderQueue.AlphaTest };
        m.SetTexture("_MainTex", karta);
        // the card's mean (with the root shade) brought per channel to the lawn's green
        var cel = BraxChampionsGrass.LawnLinear();
        var k = info.linear_nasada != null && info.linear_nasada.Length == 3 ? info.linear_nasada : info.linear;
        m.color = new Color(Mathf.Min(4f, cel.x / Mathf.Max(k[0], .001f)), Mathf.Min(4f, cel.y / Mathf.Max(k[1], .001f)), Mathf.Min(4f, cel.z / Mathf.Max(k[2], .001f))).gamma;
        m.SetFloat("_Cutoff", .5f);
        m.SetFloat("_Zasieg", Zasieg); m.SetFloat("_Blisko", Blisko); m.SetFloat("_Pas", Pas);
        m.SetFloat("_Nasada", info.nasada); m.SetFloat("_NasadaPas", info.nasada_pas);
        m.SetFloat("_Wiatr", .02f * K); m.SetFloat("_Gloss", .12f);
        // the lawn's own patches, dry spots and mowing stripes (the same photo and values as the flat lawn)
        m.SetTexture("_LawnTex", grass.GetTexture("_MainTex"));
        foreach (var p in new[] { "_MacroTile", "_MacroMix", "_MacroMean", "_DryMix", "_DryMean", "_DryThr", "_DryGain", "_Stripe", "_StripeMix" })
            if (grass.HasProperty(p)) m.SetFloat(p, grass.GetFloat(p));
        if (grass.HasProperty("_Dry")) m.SetVector("_Dry", grass.GetVector("_Dry"));
        return m;
    }
}

// the card: sRGB with its alpha, mipmaps that keep the plants' coverage (no thinning or shimmering far away), clamped,
// 2048 px; ASTC 6x6 on the phone like the region's other maps
public sealed class BraxChampionsTrawaKartaImport : AssetPostprocessor
{
    public const string Sciezka = "Assets/Source/Mistrzowie/Textures/grass_bermuda_01_karta.png";
    public override uint GetVersion() => 1;
    void OnPreprocessTexture()
    {
        if (assetPath != Sciezka) return;
        var t = (TextureImporter)assetImporter;
        t.textureType = TextureImporterType.Default; t.textureShape = TextureImporterShape.Texture2D;
        t.sRGBTexture = true; t.alphaSource = TextureImporterAlphaSource.FromInput; t.alphaIsTransparency = true;
        t.mipmapEnabled = true; t.mipMapsPreserveCoverage = true; t.alphaTestReferenceValue = .5f;
        t.npotScale = TextureImporterNPOTScale.None; t.isReadable = false; t.streamingMipmaps = false;
        t.wrapMode = TextureWrapMode.Clamp; t.filterMode = FilterMode.Trilinear; t.anisoLevel = 4;
        t.maxTextureSize = 2048; t.textureCompression = TextureImporterCompression.CompressedHQ;
        var a = t.GetPlatformTextureSettings("Android");
        a.name = "Android"; a.overridden = true; a.maxTextureSize = 2048; a.format = TextureImporterFormat.ASTC_6x6; a.compressionQuality = 100;
        t.SetPlatformTextureSettings(a);
    }
}
