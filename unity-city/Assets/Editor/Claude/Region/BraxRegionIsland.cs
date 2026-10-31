using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// WYSPA na morzu (decyzja właściciela 31.10, `ETAP-WYSPA.md`): oktagon Ligi Miasta, dopływa się skuterem/łodzią.
/// Pierwsza wersja (build „port + morze + wyspa”): teren z kodu (wzór: generator Vacation Island — wzgórze, zatoka,
/// plaża, skały), piasek / trawa / skała po wysokości i nachyleniu, palmy ze źródeł miasta + drzewa/krzewy/skały
/// z biblioteki, pomost do cumowania od strony miasta, płaska polana pod oktagon i trybuny. Wyspa OTWARTA (nic nie zamykamy).
/// Środek (400, 0, 60), promień ok. 130 m — w siatce falującej wody (x 84..560, z -120..260).
/// </summary>
public static class BraxRegionIsland
{
    public const float CX = 400f, CZ = 60f, R = 130f;
    const float Water = -1.7f, Step = 3f;
    const int N = 100;                       // 100 × 100 pól po 3 m = 300 × 300 m
    const string Out = "Assets/PlayableGenerated/ClaudeRegion/Island";

    // Polana pod oktagon: płasko na 3.5 m, 36 × 26 m, na wschód od pomostu.
    static readonly Rect Arena = new Rect(CX - 118f, CZ + 8f, 36f, 26f);

    static float Noise(float x, float z, float s) { return Mathf.PerlinNoise(x * s + 17.3f, z * s + 41.7f); }

    /// <summary>Wysokość wyspy w metrach (lustro wody -1.7).</summary>
    public static float Height(float x, float z)
    {
        float dx = x - CX, dz = z - CZ;
        float ang = Mathf.Atan2(dz, dx);
        // brzeg nie jest kołem: promień zależy od kąta (dwie zatoczki)
        float r1 = R * (1f + .12f * Mathf.Sin(ang * 3f + .6f) + .07f * Mathf.Sin(ang * 5f - 1.1f));
        float r = Mathf.Sqrt(dx * dx + dz * dz) / r1;              // 0 = środek, 1 = linia wody
        float h;
        if (r >= 1f) h = Water - 1.2f - (r - 1f) * 18f;              // dno opada
        else
        {
            float u = 1f - r;                                        // 1 w środku
            float hill = 46f * Mathf.Pow(Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((u - .18f) / .82f)), 1.35f);
            float ridge = 9f * (Noise(x, z, .012f) - .5f) * Mathf.Clamp01((u - .2f) * 2.5f);
            float beach = Mathf.Lerp(Water + .25f, 1.6f, Mathf.Clamp01(u / .14f)); // piasek 0..18 m od wody
            h = Mathf.Max(beach, hill + ridge + 1.6f);
            h += .25f * (Noise(x, z, .09f) - .5f) * Mathf.Clamp01((u - .14f) * 4f);
            if (u < .14f) h = beach + .18f * (Noise(x, z, .2f) - .5f);
        }
        if (Arena.Contains(new Vector2(x, z))) h = 3.5f;
        else
        {
            // miękkie przejście 6 m wokół polany
            float ex = Mathf.Max(Arena.xMin - x, 0, x - Arena.xMax), ez = Mathf.Max(Arena.yMin - z, 0, z - Arena.yMax);
            float d = Mathf.Sqrt(ex * ex + ez * ez);
            if (d < 6f) h = Mathf.Lerp(3.5f, h, Mathf.SmoothStep(0, 1, d / 6f));
        }
        return h;
    }

    public static void Apply(Transform cityRoot)
    {
        if (AssetDatabase.IsValidFolder(Out)) AssetDatabase.DeleteAsset(Out);
        Directory.CreateDirectory(Out);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var root = BraxRegion.Root(cityRoot, "CLAUDE wyspa (oktagon Ligi, otwarta)");

        var sand = Mat("Wyspa: piasek", 2.4f, new Color(1f, .97f, .9f), "coast_sand_02", "sand_01", "coast_sand_rocks_02", "damp_sand");
        var grass = Mat("Wyspa: trawa", 1.6f, new Color(.78f, .9f, .62f), "grass004", "grass_medium_01");
        var rock = Mat("Wyspa: skała", 3f, new Color(.9f, .88f, .84f), "rock_face_03", "rock_wall_08", "rocky_terrain_02");

        // 1) Teren: trzy siatki (piasek, trawa, skała) według wysokości i nachylenia; każda z MeshCollider.
        int w = N + 1;
        var h = new float[w, w];
        float x0 = CX - N * Step * .5f, z0 = CZ - N * Step * .5f;
        for (int j = 0; j < w; j++) for (int i = 0; i < w; i++) h[i, j] = Height(x0 + i * Step, z0 + j * Step);
        var parts = new Dictionary<int, List<int>> { { 0, new List<int>() }, { 1, new List<int>() }, { 2, new List<int>() } };
        var verts = new Vector3[w * w]; var uv = new Vector2[w * w];
        for (int j = 0; j < w; j++) for (int i = 0; i < w; i++) { verts[j * w + i] = new Vector3(x0 + i * Step, h[i, j], z0 + j * Step); uv[j * w + i] = new Vector2(x0 + i * Step, z0 + j * Step); }
        int above = 0;
        for (int j = 0; j < N; j++) for (int i = 0; i < N; i++)
        {
            int a = j * w + i, b = a + 1, c = a + w, d = c + 1;
            float hm = (h[i, j] + h[i + 1, j] + h[i, j + 1] + h[i + 1, j + 1]) * .25f;
            if (hm < Water - 14f) continue;                                  // głębokie dno poza wyspą — bez siatki
            float sx = Mathf.Abs(h[i + 1, j] - h[i, j]) / Step, sz = Mathf.Abs(h[i, j + 1] - h[i, j]) / Step;
            float slope = Mathf.Max(sx, sz);
            int kind = hm < 2.2f ? 0 : slope > .62f ? 2 : 1;
            if (hm > Water) above++;
            var t = parts[kind];
            t.Add(a); t.Add(c); t.Add(b); t.Add(b); t.Add(c); t.Add(d);
        }
        int tris = 0;
        foreach (var kv in parts)
        {
            if (kv.Value.Count == 0) continue;
            var mesh = new Mesh { name = "Wyspa teren " + kv.Key, indexFormat = IndexFormat.UInt32 };
            mesh.vertices = verts; mesh.uv = uv; mesh.triangles = kv.Value.ToArray();
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, Out + "/teren_" + kv.Key + ".asset");
            var o = new GameObject(kv.Key == 0 ? "Wyspa: piasek" : kv.Key == 1 ? "Wyspa: trawa" : "Wyspa: skały");
            o.transform.SetParent(root, false);
            o.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = o.AddComponent<MeshRenderer>(); r.sharedMaterial = kv.Key == 0 ? sand : kv.Key == 1 ? grass : rock;
            r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = true;
            o.AddComponent<MeshCollider>().sharedMesh = mesh;
            GameObjectUtility.SetStaticEditorFlags(o, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
            tris += kv.Value.Count / 3;
        }
        Physics.SyncTransforms();

        // 2) Pomost do cumowania od strony miasta (zachód): od plaży 28 m w morze, deska 1,1 m nad wodą, pale.
        float pierZ = CZ + 2f;
        float shoreX = CX; for (float x = CX; x > CX - R * 1.4f; x -= 1f) { if (Height(x, pierZ) < Water + .3f) { shoreX = x; break; } }
        var planks = BraxRegion.Pbr("Wyspa: pomost", 1.5f, null, "wood_planks_grey", "wooden_planks", "wood_planks");
        if (!planks) planks = BraxCityMaterials.Surface("Wyspa: pomost", new Color(.45f, .36f, .26f), .8f);
        var piles = BraxCityMaterials.Surface("Wyspa: pale", new Color(.28f, .22f, .16f), .9f);
        float deck = Water + 1.1f, px0 = shoreX - 28f, px1 = shoreX + 4f;
        BraxCityArchitecture.Box("Wyspa: pomost", root, new Vector3((px0 + px1) * .5f, deck - .14f, pierZ), new Vector3(px1 - px0, .28f, 3f), planks, true);
        for (float x = px0 + 1f; x <= px1 - .5f; x += 6f) foreach (float dz in new[] { -1.25f, 1.25f })
            BraxCityArchitecture.Box("Wyspa: pal", root, new Vector3(x, (deck + .25f - 6f) * .5f, pierZ + dz), new Vector3(.34f, deck + .25f + 6f, .34f), piles, false);
        BraxCityArchitecture.Box("Wyspa: zejście z pomostu", root, new Vector3(px1 + 1.2f, deck - .02f, pierZ), new Vector3(2.4f, .3f, 3.4f), planks, true);
        foreach (float dz in new[] { -1.65f, 1.65f })
            BraxRegionBackyardStop(root, "Wyspa: krawędź pomostu", new Vector3((px0 + px1) * .5f, 0, pierZ + dz), new Vector3(px1 - px0, 4f, .3f));
        BraxRegionBackyardStop(root, "Wyspa: koniec pomostu", new Vector3(px0 - .15f, 0, pierZ), new Vector3(.3f, 4f, 3f));

        // 3) Roślinność: palmy ze źródeł miasta (jeśli są), drzewa, krzewy; skały przy brzegu.
        var palms = new List<GameObject>();
        foreach (var mf in BraxClaudeGround.All<MeshFilter>(cityRoot))
        {
            if (!mf || !mf.sharedMesh) continue;
            string n = (mf.name + "/" + (mf.transform.parent ? mf.transform.parent.name : "")).ToLowerInvariant();
            if (!n.Contains("palm")) continue;
            var r = mf.GetComponent<MeshRenderer>(); if (!r) continue;
            if (mf.sharedMesh.bounds.size.y < 2.5f) continue;
            palms.Add(mf.gameObject);
            if (palms.Count >= 6) break;
        }
        var trees = new List<GameObject>();
        foreach (var id in new[] { "drzewo_verdant" }) { var m = BraxRegion.Model(id); if (m) trees.Add(m); }
        var shrubs = new List<GameObject>();
        foreach (var id in new[] { "shrub_01", "shrub_02", "shrub_03", "park_shrub" }) { var m = BraxRegion.Model(id); if (m) shrubs.Add(m); }
        var rocks = new List<GameObject>();
        foreach (var id in new[] { "coast_rocks_01", "coast_rocks_03", "boulder_01", "park_rock" }) { var m = BraxRegion.Model(id); if (m) rocks.Add(m); }
        int nPalm = 0, nTree = 0, nShrub = 0, nRock = 0;
        var veg = BraxRegion.Root(root, "Wyspa: zieleń");
        for (int i = 0; i < 900; i++)
        {
            float x = CX + BraxRegion.Range(-R * 1.1f, R * 1.1f), z = CZ + BraxRegion.Range(-R * 1.1f, R * 1.1f);
            float hh = Height(x, z);
            if (hh < Water + .1f) continue;
            if (Arena.Contains(new Vector2(x, z))) continue;
            if (Mathf.Abs(z - pierZ) < 6f && x < shoreX + 12f && x > px0 - 4f) continue;
            float slope = Mathf.Max(Mathf.Abs(Height(x + 1f, z) - hh), Mathf.Abs(Height(x, z + 1f) - hh));
            var at = new Vector3(x, hh, z);
            if (hh < 2.2f)
            {
                // plaża: palmy w pasie 4..16 m od wody, pojedyncze skały przy samej wodzie
                if (hh > .6f && palms.Count > 0 && nPalm < 70 && i % 2 == 0 && BraxRegion.Free(at + Vector3.up * 5f, new Vector3(3f, 9f, 3f)))
                { if (ClonePalm(veg, BraxRegion.Pick(palms), at)) nPalm++; }
                else if (hh < .9f && rocks.Count > 0 && nRock < 28 && i % 7 == 0)
                { if (BraxRegion.Put(veg, BraxRegion.Pick(rocks), "Wyspa: skała przy brzegu", at - Vector3.up * .3f, BraxRegion.Range(0, 360), BraxRegion.Range(.8f, 1.6f), true, 4f)) nRock++; }
                continue;
            }
            if (slope > .62f) continue;
            if (hh < 30f && trees.Count > 0 && nTree < 90 && i % 3 != 0 && BraxRegion.Free(at + Vector3.up * 5f, new Vector3(2.5f, 9f, 2.5f)))
            { if (BraxRegion.Put(veg, BraxRegion.Pick(trees), "Wyspa: drzewo", at, BraxRegion.Range(0, 360), BraxRegion.Range(.9f, 1.25f), false, 16f)) nTree++; }
            else if (shrubs.Count > 0 && nShrub < 160 && BraxRegion.Free(at + Vector3.up * 1f, new Vector3(1.6f, 1.4f, 1.6f)))
            { if (BraxRegion.Put(veg, BraxRegion.Pick(shrubs), "Wyspa: krzew", at, BraxRegion.Range(0, 360), BraxRegion.Range(.8f, 1.3f), false, 2.2f)) nShrub++; }
        }
        // 4) Znacznik polany (oktagon i trybuny przyjdą prefabem w etapie 1.3): krawężnik z kamienia wokół polany.
        var kerb = BraxCityMaterials.Surface("Wyspa: obrzeże polany", new Color(.6f, .58f, .54f), .9f);
        BraxCityArchitecture.Box("Wyspa: polana N", root, new Vector3(Arena.center.x, 3.55f, Arena.yMax), new Vector3(Arena.width, .12f, .3f), kerb, false);
        BraxCityArchitecture.Box("Wyspa: polana S", root, new Vector3(Arena.center.x, 3.55f, Arena.yMin), new Vector3(Arena.width, .12f, .3f), kerb, false);
        BraxCityArchitecture.Box("Wyspa: polana E", root, new Vector3(Arena.xMax, 3.55f, Arena.center.y), new Vector3(.3f, .12f, Arena.height), kerb, false);
        BraxCityArchitecture.Box("Wyspa: polana W", root, new Vector3(Arena.xMin, 3.55f, Arena.center.y), new Vector3(.3f, .12f, Arena.height), kerb, false);

        BraxRegion.Note("wyspa: trójkąty terenu " + tris + ", pól nad wodą " + above + ", pomost x " + px0.ToString("F0") + ".." + px1.ToString("F0") +
                        ", palmy " + nPalm + " (źródła: " + palms.Count + "), drzewa " + nTree + ", krzewy " + nShrub + ", skały " + nRock);
    }

    static bool ClonePalm(Transform parent, GameObject src, Vector3 at)
    {
        var mf = src.GetComponent<MeshFilter>(); var mr = src.GetComponent<MeshRenderer>();
        if (!mf || !mr) return false;
        var o = new GameObject("Wyspa: palma"); o.transform.SetParent(parent, false);
        o.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
        var r = o.AddComponent<MeshRenderer>(); r.sharedMaterials = mr.sharedMaterials; r.shadowCastingMode = ShadowCastingMode.On; r.receiveShadows = true;
        o.transform.rotation = src.transform.rotation * Quaternion.Euler(0, 0, 0);
        o.transform.localScale = src.transform.lossyScale * BraxRegion.Range(.9f, 1.2f);
        o.transform.RotateAround(o.transform.position, Vector3.up, BraxRegion.Range(0, 360));
        var b = BraxRegion.Bounds(o);
        o.transform.position += at - new Vector3(b.center.x, b.min.y + .15f, b.center.z);
        var c = new GameObject("Wyspa: pień palmy").AddComponent<CapsuleCollider>();
        c.transform.SetParent(parent, false); c.transform.position = at + Vector3.up * 1.5f; c.radius = .3f; c.height = 3f;
        return true;
    }

    static Material Mat(string name, float metres, Color tint, params string[] slugs)
    {
        Material m = null;
        try { m = BraxRegion.Pbr(name, metres, tint, slugs); } catch (Exception e) { BraxRegion.Note(name + ": " + e.Message); }
        return m ? m : BraxCityMaterials.Surface(name, tint * .8f, .9f);
    }

    static void BraxRegionBackyardStop(Transform root, string name, Vector3 center, Vector3 size)
    {
        var c = new GameObject(name).AddComponent<BoxCollider>();
        c.transform.SetParent(root, false); c.transform.position = center + Vector3.up * size.y * .5f; c.size = size;
    }
}
