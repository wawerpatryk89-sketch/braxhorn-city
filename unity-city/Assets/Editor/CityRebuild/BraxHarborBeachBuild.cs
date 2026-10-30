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
        go.transform.rotation = Quaternion.Euler(0, yaw, 0);
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

        // 1) Cały zielony trawnik nad wodą staje się plażą: zasięg z prawdziwych siatek trawy
        //    przy brzegu (ograniczony do pasa nadmorskiego), trawa wycięta razem z kolizją.
        float lx0 = LandX, lz0 = Z0, lz1 = Z1;
        foreach (var r in cityRoot.GetComponentsInChildren<MeshRenderer>(true))
        {
            var b = r.bounds;
            if (b.max.x < LandX || b.min.x > Shore || b.max.z < Z0 || b.min.z > Z1 || b.size.y > 3f || b.max.y > 1.5f) continue;
            if (!r.sharedMaterials.Any(BraxClaudeGround.LooksLikeGrass)) continue;
            lx0 = Mathf.Min(lx0, b.min.x); lz0 = Mathf.Min(lz0, b.min.z); lz1 = Mathf.Max(lz1, b.max.z);
        }
        lx0 = Mathf.Clamp(lx0, 50f, LandX); lz0 = Mathf.Clamp(lz0, 104f, Z0); lz1 = Mathf.Clamp(lz1, Z1, 190f);
        BeachX0 = lx0; BeachZ0 = lz0; BeachZ1 = lz1;
        BraxClaudeGround.Cut(cityRoot, new Rect(lx0 - .5f, lz0 - .5f, Shore - lx0 + 1.5f, lz1 - lz0 + 1f), -.6f, .7f, root);

        // 2) Piasek: matowy fotoskan (bez plastikowego połysku), skala w metrach świata.
        var dry = BraxClaudeGround.WorldMaterial("Plaza: suchy piasek coast_sand_02",
            Tex("coast_sand_02", "_diff_2k.jpg"), Tex("coast_sand_02", "_nor_2k.png", normal: true), null, 2.4f, new Color(1.12f, .96f, .70f), .28f, .9f);
        var wet = BraxClaudeGround.WorldMaterial("Plaza: mokry piasek damp_sand",
            Tex("damp_sand", "_diff_2k.jpg"), Tex("damp_sand", "_nor_2k.png", normal: true), null, 2.4f, new Color(.98f, .84f, .60f), .2f, .8f);
        var dryDisp = Tex("coast_sand_02", "_disp_2k.png", read: true);
        int nz = Mathf.Clamp(Mathf.RoundToInt((BeachZ1 - BeachZ0) / .45f), 60, 190);
        int nxd = Mathf.Clamp(Mathf.RoundToInt((92f - BeachX0) / .45f), 30, 100);
        int tris = BeachStrip(root, "Plaza suchy piasek", BeachX0, 92f, BeachZ0, BeachZ1, nxd, nz, dryDisp, dry);
        tris += BeachStrip(root, "Plaza mokry piasek przy wodzie", 92f, SeaX, BeachZ0, BeachZ1, 30, nz, dryDisp, wet);

        // 3) Pomost z plaży w morze (natywna skala 25.8 m), deska ok. 1.5 m nad wodą.
        var jetty = Put(root, "rustic_wooden_dock.glb", "Marina: drewniany pomost", 101f, Water + 1.55f + 1.5f, 133f, 1f, 90f, 1);
        StaticMeshCollision(jetty);
        var jb = RendererBounds(jetty);

        // 4) Jacht (24 m) przycumowany po północnej stronie pomostu, Riva po południowej.
        float yachtScale = .6f;
        var yacht = Put(root, "yacht.glb", "Marina: jacht przy pomoście", 114f, Water - 1.5f, jb.max.z + 3.2f, yachtScale, 90f);
        var riva = Put(root, "bateau_riva_boat.glb", "Marina: motorówka Riva", 106f, Water - .55f, jb.min.z - 2.4f, 1f, 90f);
        Put(root, "bateau_riva_boat.glb", "Marina: motorówka Riva 2", 118.5f, Water - .55f, jb.min.z - 2.4f, 1f, 90f);
        Put(root, "yacht.glb", "Marina: jacht na kotwicy 30 m", 132f, Water - 1.8f, jb.min.z - 17f, .75f, 72f);
        Put(root, "yacht.glb", "Marina: jacht na kotwicy 20 m", 130f, Water - 1.2f, jb.max.z + 19f, .5f, 108f);

        // 5) Skutery przy brzegu (lekko wyciągnięte na płyciznę).
        Put(root, "jetski_01.glb", "Plaza: skuter wodny 1", 99f, Water - .22f, 146f, 1.2f, 62f);
        Put(root, "jetski_02.glb", "Plaza: skuter wodny 2", 100.5f, Water - .22f, 149.5f, 1.2f, 31f);
        Put(root, "jetski_01.glb", "Plaza: skuter wodny 3", 97.8f, Water - .22f, 151.8f, 1.2f, 12f);

        // 6) Budka ratownika na suchym piasku, przodem do morza.
        var tower = Put(root, "lifeguard_tower.glb", "Plaza: budka ratownika", 79f, .02f, 141f, 6.2f / 31.0f, 270f);
        StaticMeshCollision(tower);

        // 7) Dźwig portowy w skali rzeczywistej (ok. 40 m) na nabrzeżu, wysięgnik nad wodą.
        var crane = Put(root, "harbor_crane.glb", "Port: dźwig portowy", 74f, .17f, -37f, 1f, -90f);
        var cb = RendererBounds(crane);
        // Podstawa = środek ciężkości geometrii; ma stać na lądzie (x < 82), wysięgnik nad morzem.
        Vector3 mass = Vector3.zero; float wsum = 0;
        foreach (var f in crane.GetComponentsInChildren<MeshFilter>(true))
        {
            if (!f.sharedMesh) continue; var rr = f.GetComponent<Renderer>(); if (!rr) continue;
            float wv = f.sharedMesh.vertexCount; mass += rr.bounds.center * wv; wsum += wv;
        }
        if (wsum > 0) mass /= wsum;
        if (mass.x > cb.center.x) { crane.transform.RotateAround(cb.center, Vector3.up, 180f); cb = RendererBounds(crane); mass = 2 * cb.center - mass; mass.y = cb.center.y; }
        crane.transform.position += new Vector3(76f - mass.x, 0, 0);
        // Podstawa dźwigu (natywne y=0) na nabrzeżu; chwytak może wisieć niżej, nad wodą.
        { var cp = crane.transform.position; cp.y = .17f; crane.transform.position = cp; }
        var baseCollider = new GameObject("Dźwig: kolizja podstawy").AddComponent<BoxCollider>();
        baseCollider.transform.SetParent(root, false);
        baseCollider.center = new Vector3(76f, 1.2f, -37f); baseCollider.size = new Vector3(8f, 2.4f, 8f);

        // 8) Granica kąpieliska (po kostki w wodzie), z przerwą na wejście na pomost.
        float stopX = 97.5f;
        Stop(root, "Plaza: granica wody N", new Vector3(stopX, 0, (jb.max.z + BeachZ1) * .5f), new Vector3(.5f, 5f, BeachZ1 - jb.max.z));
        Stop(root, "Plaza: granica wody S", new Vector3(stopX, 0, (BeachZ0 + jb.min.z) * .5f), new Vector3(.5f, 5f, jb.min.z - BeachZ0));
        Stop(root, "Plaza: bok N", new Vector3((Shore + stopX) * .5f, 0, BeachZ1 + .2f), new Vector3(stopX - Shore, 5f, .5f));
        Stop(root, "Plaza: bok S", new Vector3((Shore + stopX) * .5f, 0, BeachZ0 - .2f), new Vector3(stopX - Shore, 5f, .5f));
        Stop(root, "Pomost: krawędź N", new Vector3(jb.center.x, 0, jb.max.z - .3f), new Vector3(jb.size.x, 5f, .3f));
        Stop(root, "Pomost: krawędź S", new Vector3(jb.center.x, 0, jb.min.z + .3f), new Vector3(jb.size.x, 5f, .3f));
        Stop(root, "Pomost: koniec", new Vector3(jb.max.x - .3f, 0, jb.center.z), new Vector3(.3f, 5f, jb.size.z));

        var meshs = root.GetComponentsInChildren<MeshRenderer>(true);
        var report = new Review
        {
            models = new[] { "yacht.glb", "bateau_riva_boat.glb", "jetski_01.glb", "jetski_02.glb", "harbor_crane.glb", "rustic_wooden_dock.glb", "lifeguard_tower.glb" },
            sourcePaths = Required, active3dModels = 7, meshRenderers = meshs.Length, sandTriangles = tris, sandColliders = 2
        };
        File.WriteAllText(Generated + "/harbor-beach-review.json", JsonUtility.ToJson(report, true));
        AssetDatabase.SaveAssets();
        Debug.Log("BRAXHORN_HARBOR_UNITY6_STAGED beach x=" + BeachX0 + ".." + Shore + " z=" + BeachZ0 + ".." + BeachZ1 + " jetty=" + jb + " " + JsonUtility.ToJson(report));
    }
}
