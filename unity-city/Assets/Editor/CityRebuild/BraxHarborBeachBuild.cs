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

        // 3–4) Marina (decyzja właściciela 31.10, 16:34): zejście z plaży na pomost, pomost 70 m w zatokę, palce,
        //      3 jachty + 3 motorówki Riva + 3 skutery — każdy prosto, równolegle do pomostu, z miejscem wejścia (BraxVessel).
        //      Boje na linie wokół kąpieliska. Pojazdy można prowadzić (BraxVessel.Drive).
        var jb = Marina(root);

        // 5) Budka ratownika na suchym piasku, przodem do zatoki (południe).
        var tower = Put(root, "lifeguard_tower.glb", "Plaza: budka ratownika", 132f, .02f, 127f, 6.2f / 31.0f, 0f);
        StaticMeshCollision(tower);

        // 6) Parasole, leżaki, ręczniki (geometria z teksturami Poly Haven; w katalogu nie ma takich modeli).
        int furniture = 0;
        Physics.SyncTransforms();   // 31.10: kolizje piasku muszą być zarejestrowane, inaczej OnSand nic nie znajduje (0 parasoli)
        try { furniture = BraxBeachFurniture.Place(root); }
        catch (Exception e) { Debug.LogWarning("BRAXHORN_HARBOR plaża: wyposażenie: " + e.Message); }

        // 7) Dźwig portowy w skali rzeczywistej (ok. 40 m) na nabrzeżu, wysięgnik nad wodą.
        //    Decyzja właściciela (31.10): dźwig stoi przy placu kontenerowym (kontenery x 66..84, z -19..-7) → podstawa z=-25.
        const float CraneZ = -25f;
        var crane = Put(root, "harbor_crane.glb", "Port: dźwig portowy", 74f, .17f, CraneZ, 1f, -90f);
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
        baseCollider.center = new Vector3(76f, 1.2f, CraneZ); baseCollider.size = new Vector3(8f, 2.4f, 8f);

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


    // ===== MARINA (31.10) =====
    const float PierX = CapeJettyX, PierZ0 = 113f, PierZ1 = 44f, PierW = 3.2f;
    static float Deck { get { return Water + 1.1f; } }   // deska 1,1 m nad lustrem wody

    static Material Planks(string name)
    {
        var m = BraxCityMaterials.Surface(name, new Color(.45f, .36f, .26f), .8f);
        try { m = BraxRegion.Pbr(name, 1.5f, null, "wood_planks_grey", "wooden_planks", "wood_planks"); } catch (Exception) { }
        return m;
    }

    static void Pile(Transform root, Material m, float x, float z)
    {
        float top = Deck + .25f, bottom = -3.2f;
        BraxCityArchitecture.Box("Marina: pal", root, new Vector3(x, (top + bottom) * .5f, z), new Vector3(.34f, top - bottom, .34f), m, false);
    }

    /// <summary>Deska pomostu z palami i krawężnikiem-stoperem (nie spada się z deski).</summary>
    static void Walkway(Transform root, string name, Material planks, Material piles, float x0, float x1, float z0, float z1)
    {
        var c = new Vector3((x0 + x1) * .5f, Deck - .14f, (z0 + z1) * .5f);
        BraxCityArchitecture.Box(name, root, c, new Vector3(x1 - x0, .28f, z1 - z0), planks, true);
        bool alongZ = (z1 - z0) > (x1 - x0);
        if (alongZ) for (float z = z0 + 1f; z <= z1 - .5f; z += 6f) { Pile(root, piles, x0 + .25f, z); Pile(root, piles, x1 - .25f, z); }
        else for (float x = x0 + 1f; x <= x1 - .5f; x += 6f) { Pile(root, piles, x, z0 + .25f); Pile(root, piles, x, z1 - .25f); }
    }

    /// <summary>Obraca model tak, by dłuższa oś kadłuba leżała wzdłuż Z (równolegle do pomostu); zwraca bounds po obrocie.</summary>
    static Bounds AlignAlongZ(GameObject go)
    {
        var b = RendererBounds(go);
        if (b.size.x > b.size.z) { go.transform.RotateAround(b.center, Vector3.up, 90f); b = RendererBounds(go); }
        return b;
    }

    /// <summary>Łódź zacumowana prosto przy pomoście + sterowanie (BraxVessel). Środek kadłuba w (x, z).</summary>
    static GameObject Moor(Transform root, string file, string name, string label, float x, float z, float scale, float sink,
                           float maxSpeed, float turn, float seatFrac, float seatBack, float length, float width)
    {
        var model = Put(root, file, name + " model", x, Water - sink, z, scale, 0f);
        var b = AlignAlongZ(model);
        model.transform.position += new Vector3(x - b.center.x, 0, z - b.center.z);
        b = RendererBounds(model);
        var v = new GameObject(name); v.transform.SetParent(root, false);
        v.transform.position = new Vector3(x, Water, z); v.transform.rotation = Quaternion.identity;
        model.transform.SetParent(v.transform, true);
        var vessel = v.AddComponent<BraxVessel>();
        vessel.label = label; vessel.waterY = Water; vessel.draft = 0f;
        vessel.maxSpeed = maxSpeed; vessel.turnRate = turn; vessel.accel = maxSpeed * .45f;
        vessel.length = length; vessel.width = width;
        vessel.seat = new Vector3(0, Mathf.Max(.3f, (b.max.y - Water) * seatFrac), seatBack);
        vessel.moor = v.transform.position; vessel.moorYaw = 0f;
        return v;
    }

    static Bounds Marina(Transform root)
    {
        var planks = Planks("Marina: deski pomostu");
        var piles = BraxCityMaterials.Surface("Marina: pale", new Color(.28f, .22f, .16f), .9f);
        var concrete = BraxCityMaterials.Surface("Marina: beton zejścia", new Color(.62f, .6f, .56f), .9f);
        try { concrete = BraxRegion.Pbr("Marina: beton zejścia", 2f, null, "concrete_floor_02", "concrete_wall_008"); } catch (Exception) { }

        // Zejście z plaży: podest na piasku (y .02) i 2 stopnie w dół na deskę (Deck = -0.6).
        BraxCityArchitecture.Box("Marina: podest zejścia", root, new Vector3(PierX, -.12f, PierZ0 + 1.6f), new Vector3(PierW + 1f, .3f, 3.2f), concrete, true);
        int steps = 2; float rise = (.02f - Deck) / steps, run = .5f;
        for (int i = 0; i < steps; i++)
        {
            float top = .02f - rise * (i + 1), z = PierZ0 - run * i - run * .5f;
            BraxCityArchitecture.Box("Marina: stopień " + (i + 1), root, new Vector3(PierX, top - .15f, z), new Vector3(PierW + .6f, .3f, run + .02f), concrete, true);
        }
        // Główny pomost wzdłuż -Z.
        float mainZ0 = PierZ1, mainZ1 = PierZ0 - run * steps;
        Walkway(root, "Marina: pomost główny", planks, piles, PierX - PierW * .5f, PierX + PierW * .5f, mainZ0, mainZ1);
        // Palce wschód (jachty): z 106, 84, 62; jachty między palcami.
        float ex0 = PierX + PierW * .5f;
        foreach (float z in new[] { 106f, 84f, 62f }) Walkway(root, "Marina: palec E " + z, planks, piles, ex0, ex0 + 14f, z - .8f, z + .8f);
        float yachtX = ex0 + 4.6f;
        int k = 0;
        foreach (float z in new[] { 95f, 73f, 51f })
            Moor(root, "yacht.glb", "Marina: jacht " + (++k), "JACHT", yachtX, z, .7f, 1.3f, 7f, 28f, .42f, -1.5f, 17f, 5f);
        // Palce zachód (Rivy i skutery): z 108, 99, 90, 78.
        float wx1 = PierX - PierW * .5f;
        foreach (float z in new[] { 108f, 99f, 90f }) Walkway(root, "Marina: palec W " + z, planks, piles, wx1 - 9f, wx1, z - .7f, z + .7f);
        Walkway(root, "Marina: palec W skutery", planks, piles, wx1 - 6f, wx1, 78f - .6f, 78f + .6f);
        float rivaX = wx1 - 2.1f;
        k = 0;
        foreach (float z in new[] { 103.5f, 94.5f, 85.5f })
            Moor(root, "bateau_riva_boat.glb", "Marina: motorówka Riva " + (++k), "MOTORÓWKA", rivaX, z, 1f, .5f, 10f, 48f, .55f, -1.2f, 8f, 2.6f);
        float skiX = wx1 - 1.5f;
        k = 0;
        foreach (float z in new[] { 74.5f, 70f, 65.5f })
        {
            Moor(root, k % 2 == 0 ? "jetski_01.glb" : "jetski_02.glb", "Marina: skuter " + (k + 1), "SKUTER", skiX, z, 1.2f, .22f, 12f, 85f, .78f, -.35f, 3.2f, 1.2f); k++;
            BraxCityArchitecture.Box("Marina: stopień do skutera", root, new Vector3(wx1 - .45f, Deck - .32f, z), new Vector3(.9f, .14f, 1.2f), planks, true);
        }
        // Stopery na krawędziach głównego pomostu — tylko tam, gdzie nie ma palca (przejście na palce wolne).
        var fingersE = new[] { 106f, 84f, 62f }; var fingersW = new[] { 108f, 99f, 90f, 78f };
        EdgeStops(root, "E", ex0 - .15f, mainZ0, mainZ1, fingersE);
        EdgeStops(root, "W", wx1 + .15f, mainZ0, mainZ1, fingersW);
        Stop(root, "Marina: koniec pomostu", new Vector3(PierX, 0, mainZ0 + .15f), new Vector3(PierW, 4f, .3f));
        // Boje na linie: granica kąpieliska (południe z=92, wschód x=172), przerwa na kanał mariny x 112..128.
        var yellow = BraxCityMaterials.Surface("Boja: żółta", new Color(1f, .82f, .1f), .5f);
        var rope = BraxCityMaterials.Surface("Boja: lina", new Color(.9f, .9f, .85f), .9f);
        int buoys = 0;
        for (float x = 86f; x <= 172f; x += 6f) if (x < 112f || x > 128f) buoys += Buoy(root, yellow, rope, x, 92f, x + 6f <= 172f && (x + 6f < 112f || x > 128f) ? new Vector3(x + 6f, 0, 92f) : Vector3.zero);
        for (float z = 98f; z <= 162f; z += 6f) buoys += Buoy(root, yellow, rope, 172f, z, z + 6f <= 162f ? new Vector3(172f, 0, z + 6f) : Vector3.zero);
        Debug.Log("BRAXHORN_MARINA pomost z=" + mainZ0 + ".." + mainZ1 + " jachty=3 rivy=3 skutery=3 boje=" + buoys);
        var jb = new Bounds(new Vector3(PierX, Deck, (mainZ0 + mainZ1) * .5f), new Vector3(PierW, .3f, mainZ1 - mainZ0));
        return jb;
    }

    static void EdgeStops(Transform root, string side, float x, float z0, float z1, float[] fingers)
    {
        var cuts = new List<float>(fingers); cuts.Sort();
        float from = z0;
        foreach (float f in cuts)
        {
            float to = f - 1.0f;
            if (to > from + .2f) Stop(root, "Marina: krawędź " + side, new Vector3(x, 0, (from + to) * .5f), new Vector3(.3f, 4f, to - from));
            from = f + 1.0f;
        }
        if (z1 > from + .2f) Stop(root, "Marina: krawędź " + side, new Vector3(x, 0, (from + z1) * .5f), new Vector3(.3f, 4f, z1 - from));
    }

    static int Buoy(Transform root, Material yellow, Material rope, float x, float z, Vector3 next)
    {
        var b = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        b.name = "Boja"; b.transform.SetParent(root, false);
        b.transform.position = new Vector3(x, Water + .12f, z); b.transform.localScale = Vector3.one * .55f;
        UnityEngine.Object.DestroyImmediate(b.GetComponent<Collider>());
        var r = b.GetComponent<Renderer>(); r.sharedMaterial = yellow; r.shadowCastingMode = ShadowCastingMode.Off;
        if (next != Vector3.zero)
        {
            var mid = new Vector3((x + next.x) * .5f, Water + .03f, (z + next.z) * .5f);
            var seg = BraxCityArchitecture.Box("Boja: lina", root, mid, new Vector3(Mathf.Abs(next.x - x) > .1f ? Mathf.Abs(next.x - x) : .06f, .05f, Mathf.Abs(next.z - z) > .1f ? Mathf.Abs(next.z - z) : .06f), rope, false);
            foreach (var rr in seg.GetComponentsInChildren<Renderer>()) rr.shadowCastingMode = ShadowCastingMode.Off;
        }
        return 1;
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
