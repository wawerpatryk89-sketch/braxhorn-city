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

    // A natural relief sampled from the delivered real Poly Haven displacement map.
    // Only original user textures determine the relief detail; no random/fake noise.
    static float BeachY(float x,float z,Texture2D displacement)
    {
        float t=Mathf.Clamp01((x-70f)/(Shore-70f));
        float descending=Mathf.SmoothStep(0,1,t);
        float baseY=Mathf.Lerp(.17f,Water-.26f,descending);
        float u=Mathf.Repeat((x-70f)/11f,1);
        float v=Mathf.Repeat((z-113f)/13f,1);
        float sample=displacement.GetPixelBilinear(u,v).grayscale;
        float seamGuard=Mathf.Clamp01(Mathf.Abs(x-80f)/1.25f);
        float detail=(sample-.5f)*.10f*Mathf.Sin(Mathf.PI*t)*seamGuard;
        return baseY+detail;
    }

    static int BeachStrip(Transform parent,string name,float x0,float x1,
                          float z0,float z1,int segmentsX,int segmentsZ,
                          Texture2D displacement,Material material)
    {
        var verts=new Vector3[(segmentsX+1)*(segmentsZ+1)];
        var uv=new Vector2[verts.Length];
        var indices=new int[segmentsX*segmentsZ*6];
        for(int z=0;z<=segmentsZ;z++)
        for(int x=0;x<=segmentsX;x++)
        {
            int i=z*(segmentsX+1)+x;
            float worldX=Mathf.Lerp(x0,x1,(float)x/segmentsX);
            float worldZ=Mathf.Lerp(z0,z1,(float)z/segmentsZ);
            verts[i]=new Vector3(worldX,BeachY(worldX,worldZ,displacement),worldZ);
            uv[i]=new Vector2((worldX-70f)*.30f,(worldZ-113f)*.30f);
        }
        int t=0;
        for(int z=0;z<segmentsZ;z++)
        for(int x=0;x<segmentsX;x++)
        {
            int a=z*(segmentsX+1)+x, b=a+1, c=a+segmentsX+1, d=c+1;
            // Upward winding for a Unity X/Z height field.
            indices[t++]=a;indices[t++]=c;indices[t++]=b;
            indices[t++]=b;indices[t++]=c;indices[t++]=d;
        }
        var mesh=new Mesh{name=name,indexFormat=verts.Length>65535?IndexFormat.UInt32:IndexFormat.UInt16};
        mesh.vertices=verts;mesh.uv=uv;mesh.triangles=indices;
        mesh.RecalculateNormals();mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,Generated+"/"+name.Replace(' ','_')+".asset");
        var obj=new GameObject(name);
        obj.transform.SetParent(parent,false);
        obj.AddComponent<MeshFilter>().sharedMesh=mesh;
        var ren=obj.AddComponent<MeshRenderer>();ren.sharedMaterial=material;
        ren.shadowCastingMode=ShadowCastingMode.Off;
        // Walkable realistic sand, anchored only in the new beach zone.
        obj.AddComponent<MeshCollider>().sharedMesh=mesh;
        return indices.Length/3;
    }

    static Bounds RendererBounds(GameObject go)
    {
        var rs=go.GetComponentsInChildren<Renderer>(true);
        if(rs.Length==0)throw new InvalidOperationException("3D GLB has no renderers: "+go.name);
        Bounds b=rs[0].bounds;
        for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);
        if(b.size.sqrMagnitude<.001f)throw new InvalidOperationException("3D GLB has invalid bounds: "+go.name);
        return b;
    }

    static GameObject Place(Transform parent,string file,string name,
                            Vector3 target,float height,float yaw,
                            bool alignBottom=true,float submerge=0f)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Models+file);
        if(!prefab)throw new InvalidOperationException("Not imported original GLB: "+file);
        var go=UnityEngine.Object.Instantiate(prefab);
        go.name=name;go.transform.SetParent(parent,false);
        go.transform.localPosition=Vector3.zero;
        go.transform.localScale=Vector3.one;
        go.transform.rotation=Quaternion.Euler(0,yaw,0);
        var b=RendererBounds(go);
        if(b.size.y<=.001f)throw new InvalidOperationException("Model has no height: "+file);
        float scale=height/b.size.y;
        if(float.IsNaN(scale)||float.IsInfinity(scale)||scale<=0)
            throw new InvalidOperationException("GLB scale invalid: "+file);
        go.transform.localScale=Vector3.one*scale;
        b=RendererBounds(go);
        var delta=new Vector3(target.x-b.center.x,0,target.z-b.center.z);
        // For static land objects, the original ground-supported base is preserved.
        // For boats, submerge only a known fraction of its measured hull bounding height.
        delta.y=(alignBottom?target.y-b.min.y:target.y-go.transform.position.y)-submerge;
        go.transform.position+=delta;
        return go;
    }

    static void StaticMeshCollision(GameObject go)
    {
        foreach(var filter in go.GetComponentsInChildren<MeshFilter>(true))
        {
            if(!filter.sharedMesh)continue;
            var col=filter.gameObject.GetComponent<MeshCollider>();
            if(!col)col=filter.gameObject.AddComponent<MeshCollider>();
            col.sharedMesh=filter.sharedMesh;
            col.convex=false;
        }
    }

    public static void Apply(Transform cityRoot)
    {
        if(!cityRoot || cityRoot.name!="BRAXHORN CITY reference renovation")
            throw new InvalidOperationException("Harbor is restricted to actual Unity 6 city renovation root");
        if(Mathf.Abs(BraxCityWaterBuild.WaterY-Water)>.0001f ||
           Mathf.Abs(BraxCityWaterBuild.ShoreX-Shore)>.0001f)
            throw new InvalidOperationException("Original city water footprint changed; refuse port placement");
        VerifySource();
        var previous=cityRoot.Find(RootName);
        if(previous)UnityEngine.Object.DestroyImmediate(previous.gameObject);
        if(AssetDatabase.IsValidFolder(Generated))AssetDatabase.DeleteAsset(Generated);
        Directory.CreateDirectory(Generated);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        var root=new GameObject(RootName).transform;
        root.SetParent(cityRoot,false);
        root.position=Vector3.zero;
        var dry=Sand("Braxhorn dry coastal sand 2K genuine PBR","coast_sand_02");
        var wet=Sand("Braxhorn wet tidal sand 2K genuine PBR","damp_sand");
        var dryDisp=Tex("coast_sand_02","_disp_2k.png",read:true);
        var wetDisp=Tex("damp_sand","_disp_2k.png",read:true);
        int dryTriangles=BeachStrip(root,"Dry sand authentic coastal relief",70,80,113,153,40,80,dryDisp,dry);
        int wetTriangles=BeachStrip(root,"Wet sand natural waterline",80,84.35f,113,153,18,80,wetDisp,wet);

        // Current concrete quay / old timber pier stay in the original scene.
        // New original wooden jetty links the existing shore to the small marina.
        var jetty=Place(root,"rustic_wooden_dock.glb","Library original wooden jetty",
                        new Vector3(99,.1f,122),9,90,alignBottom:false);
        StaticMeshCollision(jetty);

        // Two different licensed real boats; no synthetic substitutes.
        Place(root,"yacht.glb","Berth A — real yacht GLB",
              new Vector3(118,Water,100),8,0,true,2.3f);
        Place(root,"bateau_riva_boat.glb","Berth B — Riva boat GLB",
              new Vector3(104,Water,48),2.55f,0,true,.72f);

        // Two individual variants derived solely from user's five-model GLB.
        Place(root,"jetski_01.glb","Jet ski 01 source geometry",
              new Vector3(103,Water,84),1.30f,90,true,.32f);
        Place(root,"jetski_02.glb","Jet ski 02 source geometry",
              new Vector3(108,Water,85.5f),1.30f,90,true,.32f);

        var crane=Place(root,"harbor_crane.glb","Original industrial harbour crane",
              new Vector3(78,.17f,-37),22,-90);
        // Use only a base collision envelope, never 255k-triangle solid mesh physics.
        var baseCollider=new GameObject("Real crane pedestal collision").AddComponent<BoxCollider>();
        baseCollider.transform.SetParent(crane.transform,false);
        baseCollider.center=Vector3.zero;
        baseCollider.size=new Vector3(5,.65f,5);

        var tower=Place(root,"lifeguard_tower.glb","Real lifeguard observation tower",
              new Vector3(74,BeachY(74,138,dryDisp),138),6.6f,30);
        StaticMeshCollision(tower);

        var meshs=root.GetComponentsInChildren<MeshRenderer>(true);
        var report=new Review{
            models=new[]{"yacht.glb","bateau_riva_boat.glb","jetski_01.glb",
                         "jetski_02.glb","harbor_crane.glb","rustic_wooden_dock.glb","lifeguard_tower.glb"},
            sourcePaths=Required,active3dModels=7,
            meshRenderers=meshs.Length,sandTriangles=dryTriangles+wetTriangles,sandColliders=2
        };
        File.WriteAllText(Generated+"/harbor-beach-review.json",JsonUtility.ToJson(report,true));
        AssetDatabase.SaveAssets();
        Debug.Log("BRAXHORN_HARBOR_UNITY6_STAGED "+JsonUtility.ToJson(report));
    }
}