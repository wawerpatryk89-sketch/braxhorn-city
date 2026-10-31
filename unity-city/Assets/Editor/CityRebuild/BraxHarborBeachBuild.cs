using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// BRAXHORN CITY — isolated Unity 6 waterfront extension.
// Only owner-supplied 3D scans/models and genuine coast/damp sand PBR textures.
// NEVER called by main/production without BRAX_HARBOR_BEACH_SOURCE_READY=1.
// The original gym, city, Brax, shop, source geometry, pier and water stay untouched.
public static class BraxHarborBeachBuild
{
    const string Source = "Assets/Source/HarborBeach/";
    const string Models = Source + "Models/";
    const string Textures = Source + "Textures/";
    const string Generated = "Assets/PlayableGenerated/HarborBeach";
    const string RootName = "BRAXHORN PORT + BEACH [LIBRARY 3D UNITY6]";
    const float Shore = 84f;
    const float Water = -1.7f;

    static readonly string[] Required = {
        Models+"yacht.glb", Models+"bateau_riva_boat.glb",
        Models+"jetski_01.glb", Models+"jetski_02.glb",
        Models+"harbor_crane.glb", Models+"rustic_wooden_dock.glb",
        Models+"lifeguard_tower.glb",
        Textures+"coast_sand_02_diff_2k.jpg",
        Textures+"coast_sand_02_nor_2k.png",
        Textures+"coast_sand_02_metal_smooth_2k.png",
        Textures+"coast_sand_02_disp_2k.png",
        Textures+"damp_sand_diff_2k.jpg",
        Textures+"damp_sand_nor_2k.png",
        Textures+"damp_sand_metal_smooth_2k.png",
        Textures+"damp_sand_disp_2k.png"
    };

    [Serializable] sealed class Review
    {
        public string engine="Unity 6",source="Owner BRAXHORN CITY Asset Library";
        public string status="ASSEMBLED_IN_EDITOR_AWAITING_RENDER_VISUAL_PHONE_TEST";
        public bool originalCityUntouched=true, originalGymUntouched=true, nutritionUntouched=true;
        public bool sourceModelsVerified=true, visualAccepted=false, iPhoneAccepted=false;
        public string[] models, sourcePaths;
        public int active3dModels, meshRenderers, sandTriangles, sandColliders;
        public float cityShoreX=Shore, cityWaterY=Water, dryStartZ=113, beachEndZ=153;
        public string caution="Source PBR models imported; runtime display, scale, contact, and frame rate still require Unity 6 TEST.";
    }

    [MenuItem("BRAXHORN CITY / Port and Beach / Verify required library assets")]
    public static void VerifySource()
    {
        var missing=Required.Where(p=>!File.Exists(p)).ToArray();
        if(missing.Length>0)
            throw new FileNotFoundException("Harbor source package missing: "+string.Join("; ",missing));
        foreach(var file in Required.Where(p=>p.EndsWith(".glb",StringComparison.OrdinalIgnoreCase)))
        {
            AssetDatabase.ImportAsset(file,ImportAssetOptions.ForceSynchronousImport);
            if(!AssetDatabase.LoadAssetAtPath<GameObject>(file))
                throw new InvalidOperationException("glTFast importer did not produce a GameObject: "+file);
        }
        Debug.Log("BRAXHORN_HARBOR_SOURCE_CHECK PASS original real GLB imported="+
                  Required.Count(p=>p.EndsWith(".glb")));
    }

    static Texture2D Tex(string stem, string suffix, bool normal=false, bool read=false)
    {
        string file=Textures+stem+suffix;
        if(!File.Exists(file))throw new FileNotFoundException("Authentic PBR source missing",file);
        AssetDatabase.ImportAsset(file,ImportAssetOptions.ForceSynchronousImport);
        var importer=AssetImporter.GetAtPath(file) as TextureImporter;
        if(!importer)throw new InvalidOperationException("Texture importer unavailable: "+file);
        var requestedType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
        bool changed=importer.textureType!=requestedType || importer.isReadable!=read ||
                     importer.maxTextureSize!=2048;
        if(changed)
        {
            importer.textureType=requestedType;
            importer.isReadable=read;
            importer.maxTextureSize=2048;
            importer.mipmapEnabled=true;
            importer.textureCompression=TextureImporterCompression.Compressed;
            if(normal)importer.sRGBTexture=false;
            importer.SaveAndReimport();
        }
        var result=AssetDatabase.LoadAssetAtPath<Texture2D>(file);
        if(!result)throw new InvalidOperationException("PBR texture not imported: "+file);
        return result;
    }

    static Material Sand(string name,string slug)
    {
        var shader=Shader.Find("Standard");
        if(!shader)throw new InvalidOperationException("Unity 6 Built-in Standard shader missing");
        var mat=new Material(shader){name=name};
        mat.SetTexture("_MainTex",Tex(slug,"_diff_2k.jpg"));
        mat.SetTexture("_BumpMap",Tex(slug,"_nor_2k.png",normal:true));
        mat.SetFloat("_BumpScale",0.7f);
        mat.EnableKeyword("_NORMALMAP");
        mat.SetTexture("_MetallicGlossMap",Tex(slug,"_metal_smooth_2k.png"));
        mat.SetFloat("_Metallic",0f);
        mat.SetFloat("_Glossiness",1f);
        mat.EnableKeyword("_METALLICGLOSSMAP");
        mat.mainTextureScale=new Vector2(5.5f,12f);
        string path=Generated+"/"+slug+".mat";
        AssetDatabase.CreateAsset(mat,path);
        return mat;
    }

    // ===== Plaża i marina (przepisane przez Claude po obejrzeniu pierwszego buildu) =====
    // Ląd x 70..84 (trawa wycięta), morze od x=84. Piasek na poziomie terenu,
    // od brzegu łagodnie schodzi pod wodę (lustro wody -1.7 m ok. x=96).
    const float Z0 = 113f, Z1 = 153f, LandX = 70f, SeaX = 104f;
    static float BeachX0 = 70f, BeachZ0 = 113f, BeachZ1 = 153f;

    static float BeachY(float x, float z, Texture2D disp)
    {
        float y;
        if (x <= Shore) y = .02f;
        else
        {
            float t = Mathf.Clamp01((x - Shore) / (SeaX - Shore));
            y = .02f - 2.75f * Mathf.Pow(Mathf.SmoothStep(0, 1, t), 1.05f);
        }
        // Boki plaży nad wodą schodzą łagodnie pod wodę (bez pionowej krawędzi).
        if (x > Shore)
        {
            float edge = Mathf.Min(z - BeachZ0, BeachZ1 - z);
            float k = 1f - Mathf.Clamp01(edge / 5f);
            y = Mathf.Lerp(y, Water - 1.2f, k * Mathf.Clamp01((x - Shore) / 3f));
        }
        float u = Mathf.Repeat(x / 11f, 1), v = Mathf.Repeat(z / 13f, 1);
        float detail = (disp.GetPixelBilinear(u, v).grayscale - .5f) * .06f;
        return y + (x <= Shore - .5f ? Mathf.Max(0, detail) : detail);
    }

    static int BeachStrip(Transform parent, string name, float x0, float x1, float z0, float z1,
                          int nx, int nz, Texture2D disp, Material material)
    {
        var verts = new Vector3[(nx + 1) * (nz + 1)];
        var uv = new Vector2[verts.Length];
        for (int z = 0; z <= nz; z++)
            for (int x = 0; x <= nx; x++)
            {
                int i = z * (nx + 1) + x;
                float wx = Mathf.Lerp(x0, x1, (float)x / nx), wz = Mathf.Lerp(z0, z1, (float)z / nz);
                verts[i] = new Vector3(wx, BeachY(wx, wz, disp), wz);
                uv[i] = new Vector2(wx * .3f, wz * .3f);
            }
        var idx = new int[nx * nz * 6]; int t = 0;
        for (int z = 0; z < nz; z++)
            for (int x = 0; x < nx; x++)
            {
                int a = z * (nx + 1) + x, b = a + 1, c = a + nx + 1, d = c + 1;
                idx[t++] = a; idx[t++] = c; idx[t++] = b; idx[t++] = b; idx[t++] = c; idx[t++] = d;
            }
        var mesh = new Mesh { name = name, indexFormat = verts.Length > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
        mesh.vertices = verts; mesh.uv = uv; mesh.triangles = idx;
        mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, Generated + "/" + name.Replace(' ', '_') + ".asset");
        var obj = new GameObject(name); obj.transform.SetParent(parent, false);
        obj.AddComponent<MeshFilter>().sharedMesh = mesh;
        var ren = obj.AddComponent<MeshRenderer>(); ren.sharedMaterial = material;
        ren.shadowCastingMode = ShadowCastingMode.Off; ren.receiveShadows = true;
        obj.AddComponent<MeshCollider>().sharedMesh = mesh;
        return idx.Length / 3;
    }

    static Bounds RendererBounds(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) throw new InvalidOperationException("3D GLB has no renderers: " + go.name);
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }

    // Model w skali rzeczywistej (scale × natywne metry). yMode: 0 = spód na y, 1 = góra na y.
    static GameObject Put(Transform parent, string file, string name, float x, float y, float z,
                          float scale, float yaw, int yMode = 0)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Models + file);
        if (!prefab) throw new InvalidOperationException("Not imported original GLB: " + file);
        var go = UnityEngine.Object.Instantiate(prefab);
        go.name = name; go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.rotation = Quaternion.Euler(0, yaw, 0) * go.transform.rotation; // zachowany obrót z importu (Sketchfab Z-up)
        go.transform.localScale = go.transform.localScale * scale;
        var b = RendererBounds(go);
        float dy = yMode == 0 ? y - b.min.y : y - b.max.y;
        go.transform.position += new Vector3(x - b.center.x, dy, z - b.center.z);
        foreach (var r in go.GetComponentsInChildren<Renderer>(true)) { r.shadowCastingMode = ShadowCastingMode.On; r.receiveShadows = true; }
        return go;
    }

    static void StaticMeshCollision(GameObject go)
    {
        foreach (var filter in go.GetComponentsInChildren<MeshFilter>(true))
        {
            if (!filter.sharedMesh) continue;
            var col = filter.gameObject.GetComponent<MeshCollider>();
            if (!col) col = filter.gameObject.AddComponent<MeshCollider>();
            col.sharedMesh = filter.sharedMesh; col.convex = false;
        }
    }

    static void Stop(Transform root, string name, Vector3 center, Vector3 size)
    {
        var c = new GameObject(name).AddComponent<BoxCollider>();
        c.transform.SetParent(root, false); c.center = center; c.size = size;
    }


    // ===== Plaża na narożniku cypla (kształt z prawdziwego terenu) =====
    // Siatka 1 m: promienie w dół szukają lądu (trawnik ~0 m). Czubek cypla = najdalej na wschód.
    // Strefa plaży: ląd w promieniu 35 m od czubka + pas morza do 14 m od brzegu, łagodnie pod wodę.
    static bool CornerBeach(Transform cityRoot, Transform root, Material dry, Material wet, Texture2D disp,
                            out Vector3 tip, out int tris, out Rect zoneRect)
    {
        tip = Vector3.zero; tris = 0; zoneRect = default;
        Physics.SyncTransforms();
        const float gx0 = 60f, gz0 = 98f, step = 1f; const int nx = 150, nz = 120;
        var land = new float[nx + 1, nz + 1]; var isLand = new bool[nx + 1, nz + 1];
        int east = 0;
        for (int i = 0; i <= nx; i++)
            for (int k = 0; k <= nz; k++)
            {
                float x = gx0 + i * step, z = gz0 + k * step; float best = float.NegativeInfinity;
                foreach (var h in Physics.RaycastAll(new Vector3(x, 40f, z), Vector3.down, 45f, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (h.collider.transform.IsChildOf(root)) continue;
                    if (h.normal.y < .6f || h.point.y < -.6f || h.point.y > 1.2f) continue;
                    if (best == float.NegativeInfinity || h.point.y < best) best = h.point.y;
                }
                if (best != float.NegativeInfinity) { isLand[i, k] = true; land[i, k] = best; if (x > Shore + 2f) east++; }
            }
        if (east < 60) return false;
        float tipX = float.NegativeInfinity; double zs = 0; int zc = 0;
        for (int i = nx; i >= 0 && tipX == float.NegativeInfinity; i--)
            for (int k = 0; k <= nz; k++) if (isLand[i, k] && gx0 + i * step > Shore) { tipX = gx0 + i * step; }
        int ti = Mathf.RoundToInt((tipX - gx0) / step);
        for (int k = 0; k <= nz; k++) if (isLand[ti, k]) { zs += gz0 + k * step; zc++; }
        float tipZ = (float)(zs / Mathf.Max(1, zc));
        float xMin = tipX - 35f, zMin = tipZ - 45f, zMax = tipZ + 45f;
        bool Zone(int i, int k) { float x = gx0 + i * step, z = gz0 + k * step; return isLand[i, k] && x >= xMin && z >= zMin && z <= zMax; }
        // brzeg strefy: ląd strefy sąsiadujący z morzem
        var shore = new List<Vector2>(); float zr0 = float.MaxValue, zr1 = float.MinValue;
        for (int i = 1; i < nx; i++)
            for (int k = 1; k < nz; k++)
            {
                if (!Zone(i, k)) continue;
                float z = gz0 + k * step; zr0 = Mathf.Min(zr0, z); zr1 = Mathf.Max(zr1, z);
                if (!isLand[i + 1, k] || !isLand[i - 1, k] || !isLand[i, k + 1] || !isLand[i, k - 1]) shore.Add(new Vector2(gx0 + i * step, z));
            }
        if (shore.Count < 10) return false;
        zoneRect = new Rect(xMin, zr0, tipX - xMin, zr1 - zr0);
        float Dist(float x, float z) { float d = float.MaxValue; foreach (var p in shore) { float dx = p.x - x, dz = p.y - z; float q = dx * dx + dz * dz; if (q < d) d = q; } return Mathf.Sqrt(d); }
        // wysokości wierzchołków
        var h2 = new float[nx + 1, nz + 1]; var use = new bool[nx + 1, nz + 1]; var sea = new bool[nx + 1, nz + 1];
        for (int i = 0; i <= nx; i++)
            for (int k = 0; k <= nz; k++)
            {
                float x = gx0 + i * step, z = gz0 + k * step;
                float detail = (disp.GetPixelBilinear(Mathf.Repeat(x / 11f, 1), Mathf.Repeat(z / 13f, 1)).grayscale - .5f) * .05f;
                if (Zone(i, k)) { h2[i, k] = land[i, k] + .02f + Mathf.Max(0, detail); use[i, k] = true; continue; }
                if (isLand[i, k] || x < xMin - 1f || z < zMin - 14f || z > zMax + 14f) continue;
                float d = Dist(x, z); if (d > 14f) continue;
                h2[i, k] = .02f - 2.9f * Mathf.SmoothStep(0, 1, d / 12f) + detail; use[i, k] = true; sea[i, k] = true;
                if (d >= 11f && d < 12f)
                    Stop(root, "Plaza: granica kąpieliska", new Vector3(x, 0, z), new Vector3(1.05f, 5f, 1.05f));
            }
        // siatki: suchy (ląd) i mokry (pod wodą)
        foreach (bool wetPart in new[] { false, true })
        {
            var verts = new List<Vector3>(); var idx = new List<int>(); var map = new Dictionary<int, int>();
            int V(int i, int k)
            {
                int key = i * (nz + 1) + k;
                if (!map.TryGetValue(key, out int v)) { v = verts.Count; map[key] = v; verts.Add(new Vector3(gx0 + i * step, h2[i, k], gz0 + k * step)); }
                return v;
            }
            for (int i = 0; i < nx; i++)
                for (int k = 0; k < nz; k++)
                {
                    if (!use[i, k] || !use[i + 1, k] || !use[i, k + 1] || !use[i + 1, k + 1]) continue;
                    bool anySea = sea[i, k] || sea[i + 1, k] || sea[i, k + 1] || sea[i + 1, k + 1];
                    if (anySea != wetPart) continue;
                    int a = V(i, k), b = V(i + 1, k), c = V(i, k + 1), d = V(i + 1, k + 1);
                    idx.Add(a); idx.Add(c); idx.Add(b); idx.Add(b); idx.Add(c); idx.Add(d);
                }
            if (idx.Count == 0) continue;
            var mesh = new Mesh { name = wetPart ? "Plaza mokry piasek" : "Plaza suchy piasek", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(verts); mesh.SetTriangles(idx, 0);
            mesh.SetUVs(0, verts.Select(v => new Vector2(v.x * .3f, v.z * .3f)).ToList());
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, Generated + "/" + (wetPart ? "corner_wet" : "corner_dry") + ".asset");
            var o = new GameObject(mesh.name); o.transform.SetParent(root, false);
            o.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = o.AddComponent<MeshRenderer>(); r.sharedMaterial = wetPart ? wet : dry; r.shadowCastingMode = ShadowCastingMode.Off;
            o.AddComponent<MeshCollider>().sharedMesh = mesh;
            tris += idx.Count / 3;
        }
        tip = new Vector3(tipX, land[ti, Mathf.Clamp(Mathf.RoundToInt((tipZ - gz0) / step), 0, nz)], tipZ);
        return true;
    }

    public static void Apply(Transform cityRoot)
    {
        if (!cityRoot || cityRoot.name != "BRAXHORN CITY reference renovation")
            throw new InvalidOperationException("Harbor is restricted to actual Unity 6 city renovation root");
        if (Mathf.Abs(BraxCityWaterBuild.WaterY - Water) > .0001f || Mathf.Abs(BraxCityWaterBuild.ShoreX - Shore) > .0001f)
            throw new InvalidOperationException("Original city water footprint changed; refuse port placement");
        VerifySource();
        var previous = cityRoot.Find(RootName);
        if (previous) UnityEngine.Object.DestroyImmediate(previous.gameObject);
        if (AssetDatabase.IsValidFolder(Generated)) AssetDatabase.DeleteAsset(Generated);
        Directory.CreateDirectory(Generated);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var root = new GameObject(RootName).transform;
        root.SetParent(cityRoot, false); root.position = Vector3.zero;

        // 2) Piasek: matowy fotoskan (bez plastikowego połysku), skala w metrach świata.
        var dry = BraxClaudeGround.WorldMaterial("Plaza: suchy piasek coast_sand_02",
            Tex("coast_sand_02", "_diff_2k.jpg"), Tex("coast_sand_02", "_nor_2k.png", normal: true), null, 2.4f, new Color(1.0f, .98f, .93f), .28f, .9f);
        var wet = BraxClaudeGround.WorldMaterial("Plaza: mokry piasek damp_sand",
            Tex("damp_sand", "_diff_2k.jpg"), Tex("damp_sand", "_nor_2k.png", normal: true), null, 2.4f, new Color(.94f, .90f, .82f), .2f, .8f);
        var dryDisp = Tex("coast_sand_02", "_disp_2k.png", read: true);

        // ===== PLAŻA NA CAŁYM NAROŻNIKU CYPLA (decyzja właściciela 31.10, film z promenady) =====
        // Cypel = łąka starego miasta leżąca nad wodą: x 84..150, z 112.5..160, plus pas lądu x 66..84 za końcem
        // promenady. Cały ten narożnik to piasek; brzeg (południe: zatoka, wschód: otwarte morze) schodzi łagodnie
        // pod wodę. Marina w zatoce na południe od cypla, pomost prostopadle do brzegu. Las za plażą (północ) zostaje,
        // dogęszczany drzewami w BraxRegionSea (po wycięciu starej łąki i sosen z prostokąta plaży).
        int tris = CapeBeach(root, dry, wet, dryDisp);

        // 3) Pomost z południowego brzegu cypla w zatokę (wzdłuż −Z, natywna długość 25.8 m), deska nad wodą.
        float jx = CapeJettyX, jz0 = CapeZ0 + 3.5f;
        var jetty = Put(root, "rustic_wooden_dock.glb", "Marina: drewniany pomost", jx, Water + 1.55f + 1.5f, jz0 - 12.9f, 1f, 0f, 1);
        StaticMeshCollision(jetty);
        var jb = RendererBounds(jetty);
        JettySteps(root, jb);

        // 4) Jacht (24 m) przy pomoście od wschodu, Rivy od zachodu, jachty na kotwicy w zatoce, skutery na płyciźnie przy plaży.
        Put(root, "yacht.glb", "Marina: jacht przy pomoście", jb.max.x + 3.4f, Water - 1.5f, jb.center.z - 1.5f, .6f, 0f);
        Put(root, "bateau_riva_boat.glb", "Marina: motorówka Riva", jb.min.x - 2.4f, Water - .55f, jb.center.z + 4.5f, 1f, 0f);
        Put(root, "bateau_riva_boat.glb", "Marina: motorówka Riva 2", jb.min.x - 2.4f, Water - .55f, jb.center.z - 4.5f, 1f, 0f);
        Put(root, "yacht.glb", "Marina: jacht na kotwicy 30 m", 143f, Water - 1.8f, 88f, .75f, 155f);
        Put(root, "yacht.glb", "Marina: jacht na kotwicy 20 m", 108f, Water - 1.2f, 72f, .5f, 25f);
        Put(root, "jetski_01.glb", "Plaza: skuter wodny 1", 99f, Water - .22f, 108.5f, 1.2f, 150f);
        Put(root, "jetski_02.glb", "Plaza: skuter wodny 2", 102.2f, Water - .22f, 107f, 1.2f, 175f);
        Put(root, "jetski_01.glb", "Plaza: skuter wodny 3", 105.2f, Water - .22f, 108.8f, 1.2f, 205f);

        // 5) Budka ratownika na suchym piasku, przodem do zatoki (południe).
        var tower = Put(root, "lifeguard_tower.glb", "Plaza: budka ratownika", 132f, .02f, 127f, 6.2f / 31.0f, 0f);
        StaticMeshCollision(tower);

        // 6) Parasole, leżaki, ręczniki (geometria z teksturami Poly Haven; w katalogu nie ma takich modeli).
        int furniture = 0;
        try { furniture = BraxBeachFurniture.Place(root); }
        catch (Exception e) { Debug.LogWarning("BRAXHORN_HARBOR plaża: wyposażenie: " + e.Message); }

        // 7) Dźwig portowy w skali rzeczywistej (ok. 40 m) na nabrzeżu, wysięgnik nad wodą.
        var crane = Put(root, "harbor_crane.glb", "Port: dźwig portowy", 74f, .17f, -37f, 1f, -90f);
        var cb = RendererBounds(crane);
        Vector3 mass = Vector3.zero; float wsum = 0;
        foreach (var f in crane.GetComponentsInChildren<MeshFilter>(true))
        {
            if (!f.sharedMesh) continue; var rr = f.GetComponent<Renderer>(); if (!rr) continue;
            float wv = f.sharedMesh.vertexCount; mass += rr.bounds.center * wv; wsum += wv;
        }
        if (wsum > 0) mass /= wsum;
        if (mass.x > cb.center.x) { crane.transform.RotateAround(cb.center, Vector3.up, 180f); cb = RendererBounds(crane); mass = 2 * cb.center - mass; mass.y = cb.center.y; }
        crane.transform.position += new Vector3(76f - mass.x, 0, 0);
        { var cp = crane.transform.position; cp.y = .17f; crane.transform.position = cp; }
        var baseCollider = new GameObject("Dźwig: kolizja podstawy").AddComponent<BoxCollider>();
        baseCollider.transform.SetParent(root, false);
        baseCollider.center = new Vector3(76f, 1.2f, -37f); baseCollider.size = new Vector3(8f, 2.4f, 8f);

        // 8) Krawędzie pomostu (nie spada się z deski).
        Stop(root, "Pomost: krawędź E", new Vector3(jb.max.x - .3f, 0, jb.center.z), new Vector3(.3f, 5f, jb.size.z));
        Stop(root, "Pomost: krawędź W", new Vector3(jb.min.x + .3f, 0, jb.center.z), new Vector3(.3f, 5f, jb.size.z));
        Stop(root, "Pomost: koniec", new Vector3(jb.center.x, 0, jb.min.z + .3f), new Vector3(jb.size.x, 5f, .3f));

        var meshs = root.GetComponentsInChildren<MeshRenderer>(true);
        var report = new Review
        {
            models = new[] { "yacht.glb", "bateau_riva_boat.glb", "jetski_01.glb", "jetski_02.glb", "harbor_crane.glb", "rustic_wooden_dock.glb", "lifeguard_tower.glb" },
            sourcePaths = Required, active3dModels = 7, meshRenderers = meshs.Length, sandTriangles = tris, sandColliders = 2,
            dryStartZ = CapeZ0, beachEndZ = CapeZ1
        };
        File.WriteAllText(Generated + "/harbor-beach-review.json", JsonUtility.ToJson(report, true));
        AssetDatabase.SaveAssets();
        Debug.Log("BRAXHORN_HARBOR_UNITY6_STAGED cape x=" + CapeX0 + ".." + CapeX1 + " z=" + CapeZ0 + ".." + CapeZ1 + " jetty=" + jb + " furniture=" + furniture + " " + JsonUtility.ToJson(report));
    }

    // ===== Plaża na cyplu: pole wysokości 1 m. Plateau 0.02 m + mikrorelief z mapy przemieszczeń; od 6 m przed brzegiem
    // (południe z=112.5 dla x>84, wschód x=150) łagodny spadek do −2.9 m 12 m za brzegiem (lustro wody −1.7 m ok. 4 m za brzegiem).
    public const float CapeX0 = 66f, CapeShoreX = 84f, CapeX1 = 150f, CapeZ0 = 112.5f, CapeZ1 = 160f, CapeJettyX = 120f;
    const float CapeGridX0 = 64f, CapeGridZ0 = 97f; const int CapeNX = 104, CapeNZ = 68;   // x 64..168, z 97..165

    /// <summary>Odległość ze znakiem od brzegu cypla: ujemna na piasku (plateau), dodatnia w stronę wody.</summary>
    public static float CapeShoreDistance(float x, float z)
    {
        float dxE = x - CapeX1, dzS = CapeZ0 - z;
        if (x < CapeShoreX) dzS = CapeZ0 - z - (CapeShoreX - x);   // za promenadą (ląd) brzeg jest daleko
        return (dxE > 0 && dzS > 0) ? Mathf.Sqrt(dxE * dxE + dzS * dzS) : Mathf.Max(dxE, dzS);
    }

    public static float CapeSandHeight(float x, float z, Texture2D disp)
    {
        float d = CapeShoreDistance(x, z);
        float detail = disp ? (disp.GetPixelBilinear(Mathf.Repeat(x / 11f, 1), Mathf.Repeat(z / 13f, 1)).grayscale - .5f) * .06f : 0f;
        if (d <= -6f) return .02f + Mathf.Max(0, detail);
        return .02f - 2.9f * Mathf.SmoothStep(0, 1, (d + 6f) / 18f) + detail;
    }

    static int CapeBeach(Transform root, Material dry, Material wet, Texture2D disp)
    {
        var h = new float[CapeNX + 1, CapeNZ + 1]; var use = new bool[CapeNX + 1, CapeNZ + 1]; var dist = new float[CapeNX + 1, CapeNZ + 1];
        for (int i = 0; i <= CapeNX; i++)
            for (int k = 0; k <= CapeNZ; k++)
            {
                float x = CapeGridX0 + i, z = CapeGridZ0 + k;
                bool plateau = z >= CapeZ0 && z <= CapeZ1 && x >= CapeX0 && x <= CapeX1;
                bool bay = z < CapeZ0 && x >= 90f && x <= CapeX1 + 14f;            // zatoka na południe (bez rogu przy nabrzeżu)
                bool east = x > CapeX1 && z >= CapeZ0 && z <= CapeZ1;                // otwarte morze na wschód
                if (!plateau && !bay && !east) continue;
                float d = CapeShoreDistance(x, z); if (d > 13f) continue;
                dist[i, k] = d; h[i, k] = CapeSandHeight(x, z, disp); use[i, k] = true;
                if (d >= 10.5f && d < 11.5f && ((i + k) % 2 == 0))
                    Stop(root, "Plaza: granica kąpieliska", new Vector3(x, 0, z), new Vector3(2.1f, 5f, 2.1f));
            }
        int total = 0;
        foreach (bool wetPart in new[] { false, true })
        {
            var verts = new List<Vector3>(); var idx = new List<int>(); var map = new Dictionary<int, int>();
            int V(int i, int k)
            {
                int key = i * (CapeNZ + 1) + k; int v;
                if (!map.TryGetValue(key, out v)) { v = verts.Count; map[key] = v; verts.Add(new Vector3(CapeGridX0 + i, h[i, k], CapeGridZ0 + k)); }
                return v;
            }
            for (int i = 0; i < CapeNX; i++)
                for (int k = 0; k < CapeNZ; k++)
                {
                    if (!use[i, k] || !use[i + 1, k] || !use[i, k + 1] || !use[i + 1, k + 1]) continue;
                    float dmax = Mathf.Max(Mathf.Max(dist[i, k], dist[i + 1, k]), Mathf.Max(dist[i, k + 1], dist[i + 1, k + 1]));
                    if ((dmax > -1f) != wetPart) continue;   // mokry piasek od 1 m przed linią wody
                    int a = V(i, k), b = V(i + 1, k), c = V(i, k + 1), d2 = V(i + 1, k + 1);
                    idx.Add(a); idx.Add(c); idx.Add(b); idx.Add(b); idx.Add(c); idx.Add(d2);
                }
            if (idx.Count == 0) continue;
            var mesh = new Mesh { name = wetPart ? "Plaza mokry piasek" : "Plaza suchy piasek", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(verts); mesh.SetTriangles(idx, 0);
            mesh.SetUVs(0, verts.Select(v => new Vector2(v.x * .3f, v.z * .3f)).ToList());
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, Generated + "/" + (wetPart ? "cape_wet" : "cape_dry") + ".asset");
            var o = new GameObject(mesh.name); o.transform.SetParent(root, false);
            o.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = o.AddComponent<MeshRenderer>(); r.sharedMaterial = wetPart ? wet : dry; r.shadowCastingMode = ShadowCastingMode.Off;
            o.AddComponent<MeshCollider>().sharedMesh = mesh;
            total += idx.Count / 3;
        }
        return total;
    }

    // Schodki z piasku na deskę pomostu (deska ok. 1.3 m nad piaskiem): 4 stopnie po 0.33 m.
    static void JettySteps(Transform root, Bounds jb)
    {
        var planks = BraxCityMaterials.Surface("Pomost: schodki z desek", new Color(.45f, .36f, .26f), .8f);
        try { planks = BraxRegion.Pbr("Pomost: schodki z desek", 1.5f, null, "wood_planks_grey", "wooden_planks", "wood_planks"); } catch (Exception) { }
        float top = jb.max.y, bottom = .02f; int n = 4; float rise = (top - bottom) / n, run = .42f;
        for (int i = 0; i < n; i++)
        {
            float y = bottom + rise * (i + 1), z = jb.max.z + run * (n - i) - run * .5f;
            BraxCityArchitecture.Box("Pomost: stopień " + (i + 1), root, new Vector3(jb.center.x, y - rise * .5f - .02f, z), new Vector3(jb.size.x * .9f, rise, run + .04f), planks, true);
        }
    }
}
