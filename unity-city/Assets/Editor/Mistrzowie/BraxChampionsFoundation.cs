using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// BRAXHORN CITY: separate region, existing scale and source vegetation.
// Built by the dedicated region CI; no runtime manager or change to another region.
public static class BraxChampionsFoundation
{
    const string Source = "Assets/Source/Mistrzowie";
    const string Output = "Assets/PlayableGenerated/Mistrzowie";
    public const string ScenePath = Output + "/Mistrzowie_Foundation.unity";
    const string WaterTemplate = "Assets/Editor/CityRebuild/Environment/Water/HarbourWater.shader.txt";
    static float K => BraxClaudeOsiedle.K;

    [Serializable] public sealed class Foundation
    {
        public string version, regionId, sourceCommit;
        public float scaleK;
        public MeshData[] meshes;
        public Plant[] plants;
        public Building[] buildings;
        public Stop[] tramStops, metroStations, busStops;
        public Berth[] berths;
        public Pier[] piers;
    }
    [Serializable] public sealed class MeshData { public string id, role; public float[] positions, uv; public int[] triangles; }
    [Serializable] public sealed class Plant { public string id, kind, band; public float x,z,yaw; public int variant; }
    [Serializable] public sealed class Building { public string id,name; public float x,z,w,d,height; }
    [Serializable] public sealed class Stop { public float x,z; }
    [Serializable] public sealed class Pier
    {
        public string id,role;
        public float x,z,w,d;
    }
    [Serializable] public sealed class Berth
    {
        public string id,kind,dock;
        public float x,z,length,width,accessX,accessZ,yaw;
    }
    [Serializable] sealed class Report
    {
        public string status = "GENERATED_REQUIRES_VISUAL_AND_PHONE_REVIEW";
        public string sourceCommit;
        public float scaleK;
        public int meshes, triangles, palms, trees, berths;
        public bool interiorsBuilt=false, yachtsPlaced=false, trafficEnabled=false, visualAccepted=false, phoneTested=false;
    }
    sealed class MapSet { public string color,normal,arm; public float tile; }

    static Foundation Read()
    {
        var d=JsonUtility.FromJson<Foundation>(File.ReadAllText(Source+"/Foundation.json"));
        if(d==null || d.regionId!="mistrzowie" || Mathf.Abs(d.scaleK-K)>.00001f)
            throw new InvalidOperationException("Region scale differs from BraxClaudeOsiedle.K; regenerate nominal geometry, never rescale imports twice.");
        if(d.meshes==null || d.meshes.Length==0 || d.plants==null || d.berths==null || d.berths.Length!=38 || d.piers==null || d.piers.Length!=56
            || d.berths.Count(b=>b.kind=="yacht")!=15
            || d.berths.Count(b=>b.kind=="motorboat")!=10
            || d.berths.Count(b=>b.kind=="jetski")!=10
            || d.berths.Count(b=>b.kind=="cruise_ship")!=2
            || d.berths.Count(b=>b.kind=="ferry")!=1)
            throw new InvalidOperationException("Incomplete foundation manifest");
        foreach(var m in d.meshes)
        {
            if(m.positions==null || m.positions.Length%3!=0 || m.uv.Length!=m.positions.Length/3*2 || m.triangles.Length%3!=0)
                throw new InvalidOperationException("Malformed mesh: "+m.id);
            if(m.positions.Any(x=>float.IsNaN(x)||float.IsInfinity(x)) || m.triangles.Any(i=>i<0 || i>=m.positions.Length/3))
                throw new InvalidOperationException("Invalid mesh data: "+m.id);
        }
        return d;
    }

    // Resolves actual source files. Missing PBR maps are an error, never flat-colour substitutes.
    static string FileIn(string root,params string[] names)
    {
        if(!Directory.Exists(root))throw new FileNotFoundException("Restore verified source directory: "+root);
        var files=Directory.GetFiles(root,"*",SearchOption.AllDirectories);
        foreach(var name in names)
        {
            var matches=files.Where(p=>string.Equals(Path.GetFileName(p),name,StringComparison.OrdinalIgnoreCase)).ToArray();
            if(matches.Length==1)return matches[0].Replace('\\','/');
            if(matches.Length>1)throw new InvalidOperationException("Ambiguous source "+name+" in "+root);
        }
        throw new FileNotFoundException("Missing PBR map in "+root+": "+string.Join(", ",names));
    }
    static Dictionary<string,MapSet> Sources()
    {
        string tex=Source+"/Textures";
        var maps=new Dictionary<string,MapSet>();
        foreach(string pair in new[]{"sand_dry|coast_sand_02","sand_wet|damp_sand"})
        {
            var a=pair.Split('|');string slug=a[1];
            maps[a[0]]=new MapSet{color=FileIn(tex,slug+"_diff_4k.jpg"),normal=FileIn(tex,slug+"_nor_gl_4k.exr"),arm=FileIn(tex,slug+"_arm_4k.png"),tile=a[0]=="sand_dry"?15f:2f};
        }
        maps["grass"]=new MapSet{color=FileIn(tex,"Grass004_4K-JPG_Color.jpg"),normal=FileIn(tex,"Grass004_4K-JPG_NormalGL.jpg"),arm=FileIn(tex,"Grass004_ARM_4k.png"),tile=1.4f};
        foreach(string pair in new[]{"asphalt|clean_asphalt|2.1","paving|concrete_pavement_02|1.8"})
        {
            var a=pair.Split('|');string slug=a[1];
            maps[a[0]]=new MapSet{color=FileIn(tex,slug+"_diff_4k.jpg"),normal=FileIn(tex,slug+"_nor_gl_4k.exr"),arm=FileIn(tex,slug+"_arm_4k.png"),tile=a[0]=="asphalt"?2.1f:1.8f};
        }
        // Real Poly Haven PBR for working docks: hard wearing timber on
        // floating pontoons, granite on high passenger/cruise berths.
        maps["quay_wood"]=new MapSet{
            color=FileIn(tex,"teak_veneer_diff_4k.jpg"),
            normal=FileIn(tex,"teak_veneer_nor_gl_4k.exr"),
            arm=FileIn(tex,"teak_veneer_arm_4k.png"),tile=1.4f};
        maps["quay_jetski"]=maps["quay_wood"];
        maps["quay_ramp"]=maps["quay_wood"];
        maps["quay_ramp_jetski"]=maps["quay_wood"];
        maps["quay_cruise"]=new MapSet{
            color=FileIn(tex,"granite_tile_diff_4k.jpg"),
            normal=FileIn(tex,"granite_tile_nor_gl_4k.exr"),
            arm=FileIn(tex,"granite_tile_arm_4k.png"),tile=1.7f};
        maps["quay_ferry"]=maps["quay_cruise"];
        maps["quay_wall"]=new MapSet{
            color=FileIn(tex,"concrete_wall_008_diff_4k.jpg"),
            normal=FileIn(tex,"concrete_wall_008_nor_gl_4k.exr"),
            arm=FileIn(tex,"concrete_wall_008_arm_4k.png"),tile=1.5f};
        // The red asphalt replaces the original grey cobbles across the
        // whole 3m width. The inset markings are a DIFFERENT adjoining mesh,
        // not decals on top. Both use authentic >=2K Android PBR maps.
        maps["cycle_lane"]=new MapSet{
            color=FileIn(tex,"clean_asphalt_diff_4k.jpg"),
            normal=FileIn(tex,"clean_asphalt_nor_gl_4k.exr"),
            arm=FileIn(tex,"clean_asphalt_arm_4k.png"),tile=2.0f};
        maps["cycle_mark"]=new MapSet{
            color=FileIn(tex,"concrete_pavement_02_diff_4k.jpg"),
            normal=FileIn(tex,"concrete_pavement_02_nor_gl_4k.exr"),
            arm=FileIn(tex,"concrete_pavement_02_arm_4k.png"),tile=1.8f};
        maps["plot_base"]=maps["paving"];
        return maps;
    }
    static GameObject[] Palms()
    {
        var p=new[]{BraxMiastoAssety.Model("miasto_palmy","palma"),BraxMiastoAssety.Model("miasto_palmy","palma2")};
        if(p.Any(x=>!x))throw new FileNotFoundException("Restore the existing private miasto_palmy package (palma + palma2). No procedural palm replacement is permitted.");
        return p;
    }
    static GameObject Tree()
    {
        var t=BraxRegion.Model("drzewo_verdant");
        if(!t)throw new FileNotFoundException("Existing drzewo_verdant model is missing");
        return t;
    }
    [MenuItem("BRAXHORN CITY/Miasto Mistrzow/1 - Verify foundation inputs")]
    public static void VerifyInputs()
    {
        var d=Read();Sources();Palms();Tree();
        if(!Shader.Find(BraxCityMaterials.ShaderName))throw new InvalidOperationException("Existing CityStaticPBR shader missing");
        if(!File.Exists(WaterTemplate))throw new FileNotFoundException("Existing harbour water shader source missing",WaterTemplate);
        Debug.Log("BRAX_MISTRZOWIE_INPUTS_OK geometry="+d.meshes.Length+" K="+K+"; visual acceptance pending");
    }

    [MenuItem("BRAXHORN CITY/Miasto Mistrzow/2 - Generate foundation and vegetation")]
    public static void Generate()
    {
        // CI starts with one untitled empty scene. Unity refuses additive creation
        // while an untitled scene is open; reuse it without discarding user objects.
        var previous=SceneManager.GetActiveScene();
        bool reuseEmptyScene=SceneManager.sceneCount==1&&previous.IsValid()&&previous.isLoaded
            &&string.IsNullOrEmpty(previous.path)&&previous.rootCount==0;
        if(!reuseEmptyScene)
            for(int i=0;i<SceneManager.sceneCount;i++)
                if(string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path))
                    throw new InvalidOperationException("Save untitled scenes before generating the foundation; existing objects will not be discarded.");
        // All source requirements checked before creating a scene or changing assets.
        VerifyInputs();var d=Read();var sources=Sources();var palms=Palms();var tree=Tree();
        if(File.Exists(ScenePath))throw new InvalidOperationException("Foundation scene already exists. Keep its GUID and manual edits; use a reviewed update instead of overwriting it.");
        Directory.CreateDirectory(Output+"/Meshes");Directory.CreateDirectory(Output+"/Materials");Directory.CreateDirectory(Output+"/Textures");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var materials=new Dictionary<string,Material>();
        var shared=new Dictionary<MapSet,Material>();
        foreach(var pair in sources)
        {
            if(!shared.TryGetValue(pair.Value,out var material)){material=MaterialFor(pair.Key,pair.Value);shared.Add(pair.Value,material);}
            materials[pair.Key]=material;
        }
        // Original Poly Haven coast_sand_02 and damp_sand maps retain their
        // real normals and ARM. Only the albedo tint is brought to the
        // requested very light golden beach, wet sand remains slightly darker.
        materials["cycle_lane"].SetColor("_Color",new Color(.74f,.12f,.16f,1f)); // muted warm red cycle asphalt
        materials["cycle_mark"].SetColor("_Color",new Color(.96f,.96f,.91f,1f)); // off-white paint, physically inlaid
        EditorUtility.SetDirty(materials["cycle_lane"]);
        EditorUtility.SetDirty(materials["cycle_mark"]);
        materials["sand_dry"].SetColor("_Color",new Color(1.52f,1.40f,1.12f,1f));
        materials["sand_wet"].SetColor("_Color",new Color(1.13f,1.06f,.88f,1f));
        materials["grass"]=BraxChampionsGrass.CreateMaterial(materials["grass"]);
        materials["sea"]=SeaMaterial();
        materials["foam"]=FoamMaterial();
        var scene=reuseEmptyScene?previous:EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        var report=new Report{sourceCommit=d.sourceCommit,scaleK=K,berths=d.berths.Length};
        try
        {
            var root=new GameObject("BRAXHORN | MIASTO MISTRZOW | EXTERIOR FOUNDATION").transform;
            // Geometry is already in game units. Root transform MUST stay at identity.
            foreach(var m in d.meshes)
            {
                var mesh=new Mesh{name=m.id,indexFormat=m.positions.Length/3>65535?IndexFormat.UInt32:IndexFormat.UInt16};
                var v=new Vector3[m.positions.Length/3];var uv=new Vector2[v.Length];
                for(int i=0;i<v.Length;i++){v[i]=new Vector3(m.positions[i*3],m.positions[i*3+1],m.positions[i*3+2]);uv[i]=new Vector2(m.uv[i*2],m.uv[i*2+1]);}
                mesh.vertices=v;mesh.uv=uv;mesh.triangles=m.triangles;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
                if(m.role=="sea"||m.role=="foam"){var b=mesh.bounds;b.Expand(new Vector3(0,.5f,0));mesh.bounds=b;}
                mesh=Save(mesh,Output+"/Meshes/"+m.id+".asset");
                var o=new GameObject(m.id);o.transform.SetParent(root,false);o.AddComponent<MeshFilter>().sharedMesh=mesh;
                var r=o.AddComponent<MeshRenderer>();r.sharedMaterial=materials[m.role];
                if(m.role!="sea"&&m.role!="foam")o.AddComponent<MeshCollider>().sharedMesh=mesh;
                else {r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;}
                GameObjectUtility.SetStaticEditorFlags(o,StaticEditorFlags.OccludeeStatic);
                report.meshes++;report.triangles+=m.triangles.Length/3;
            }
            BraxChampionsGrass.AddClumps(root,materials["grass"]);
            var vegetation=new GameObject("Zielen - ziemia i trawniki poza chodnikami").transform;vegetation.SetParent(root,false);
            foreach(var p in d.plants)
            {
                Vector3 at=new Vector3(p.x*K,.16f*K,p.z*K);GameObject o;
                if(p.kind=="palm")
                {
                    // Source palms are in real metres; Postaw applies the shared K once.
                    o=BraxMiastoAssety.Postaw(vegetation,palms[p.variant%palms.Length],p.id+" Palma "+p.band,at,p.yaw,1f,false,true);
                    BraxMiastoAssety.LodyZWezlow(o,new[]{.25f,.08f,.012f});report.palms++;
                }
                else
                {
                    o=BraxRegion.Put(vegetation,tree,p.id+" Drzewo "+p.band,at,p.yaw,1f,false,6.5f*K);report.trees++;
                }
                if(!o)throw new InvalidOperationException("Vegetation placement failed: "+p.id);
                // A separate trunk capsule cannot turn the whole crown into an obstacle.
                if(o.GetComponentsInChildren<Collider>().Length==0)
                {
                    var trunk=new GameObject(p.id+" trunk collision");trunk.transform.SetParent(vegetation,false);trunk.transform.position=at+Vector3.up*2*K;
                    var cc=trunk.AddComponent<CapsuleCollider>();cc.radius=.3f*K;cc.height=4*K;
                }
                foreach(var r in o.GetComponentsInChildren<Renderer>(true))GameObjectUtility.SetStaticEditorFlags(r.gameObject,StaticEditorFlags.OccludeeStatic);
            }
            // Place the ORIGINAL user-library yacht, Riva and jetski models.
            // No fabricated cruise/ferry vessel is allowed without its source.
            int boats=BraxChampionsHarbour.PlaceOriginalFleet(root,d.berths,d.piers);
            if(boats!=35)throw new InvalidOperationException("Champions harbour must place 15 yachts + 10 Rivas + 10 jetskis, got "+boats);
            report.yachtsPlaced=true;
            var reserves=new GameObject("PLAN - działki, przystanki, miejsca promów i wycieczkowców").transform;reserves.SetParent(root,false);
            foreach(var b in d.buildings)Marker(reserves,b.id+" "+b.name+" / exterior next",b.x,b.z);
            foreach(var b in d.berths)
                if(b.kind=="ferry"||b.kind=="cruise_ship")
                    Marker(reserves,b.id+" "+b.kind+" / reserved berth "+b.length+"x"+b.width,b.x,b.z,-2);
            for(int i=0;i<d.tramStops.Length;i++)Marker(reserves,"TR"+(i+1),d.tramStops[i].x,d.tramStops[i].z);
            for(int i=0;i<d.busStops.Length;i++)Marker(reserves,"U"+(i+1),d.busStops[i].x,d.busStops[i].z);
            for(int i=0;i<d.metroStations.Length;i++)Marker(reserves,"M"+(i+1)+" underground reserve",d.metroStations[i].x,d.metroStations[i].z,-12);
            Marker(reserves,"WORLD_CONNECTOR_ROAD",0,330);
            Marker(reserves,"RAIL_CORRIDOR_UNDERGROUND",600,32,-12);
            if(!EditorSceneManager.SaveScene(scene,ScenePath))throw new IOException("Foundation scene was not saved");
            File.WriteAllText(Output+"/foundation-report.json",JsonUtility.ToJson(report,true));AssetDatabase.Refresh();AssetDatabase.SaveAssets();
            Debug.Log("BRAX_MISTRZOWIE_FOUNDATION "+JsonUtility.ToJson(report));
        }
        finally
        {
            // Keep the reused scene open (also on failure, for inspection).
            if(!reuseEmptyScene)EditorSceneManager.CloseScene(scene,true);
            if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
        }
    }
    static void Marker(Transform parent,string name,float x,float z,float y=0)
    {var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=new Vector3(x,y,z)*K;}

    static T Save<T>(T value,string path) where T:UnityEngine.Object
    {
        var old=AssetDatabase.LoadAssetAtPath<T>(path);
        if(old){EditorUtility.CopySerialized(value,old);UnityEngine.Object.DestroyImmediate(value);EditorUtility.SetDirty(old);return old;}
        AssetDatabase.CreateAsset(value,path);return value;
    }
    static Texture2D CopyMap(string source,string role,string id)
    {
        string destination=Output+"/Textures/"+id+"_"+role+Path.GetExtension(source);
        // Preserve an existing GUID. New copies receive final settings in
        // BraxChampionsTextureImport before compression, not in a second import.
        if(!File.Exists(destination))File.Copy(source,destination);
        AssetDatabase.ImportAsset(destination,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(destination);
        if(!importer)throw new InvalidOperationException("Generated PBR importer missing: "+destination);
        var android=importer.GetPlatformTextureSettings("Android");
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(destination);
        if(!texture||texture.width!=2048||texture.height!=2048||importer.textureType!=TextureImporterType.Default
            ||importer.sRGBTexture!=(role=="color")||!android.overridden||android.maxTextureSize!=2048
            ||android.format!=TextureImporterFormat.ASTC_4x4||android.compressionQuality!=100)
            throw new InvalidOperationException("Generated PBR import contract failed: "+destination);
        return texture;
    }
    static readonly Dictionary<string,Material> textureSets=new Dictionary<string,Material>();
    static Material MaterialFor(string id,MapSet source)
    {
        if(textureSets.TryGetValue(source.color,out var shared)&&shared)
        {
            var copy=new Material(shared){name="Mistrzowie "+id};
            foreach(string property in new[]{"_MainTex","_BumpMap","_ARM"})copy.SetTextureScale(property,Vector2.one/source.tile);
            return Save(copy,Output+"/Materials/"+id+".mat");
        }
        var m=new Material(Shader.Find(BraxCityMaterials.ShaderName)){name="Mistrzowie "+id,enableInstancing=true};
        m.SetTexture("_MainTex",CopyMap(source.color,"color",id));m.SetTexture("_BumpMap",CopyMap(source.normal,"normal",id));
        m.SetFloat("_VertexTint",0);m.SetFloat("_NormalEncoding",0);m.SetFloat("_Roughness",1);m.SetFloat("_Metallic",1);m.SetFloat("_UseARMForAO",1);
        m.SetVector("_SunDirection",BraxCityMaterials.SunDirection);
        if(string.IsNullOrEmpty(source.arm))throw new InvalidOperationException("Missing prepared ARM map: "+id);
        var arm=CopyMap(source.arm,"arm",id);
        m.SetTexture("_ARM",arm);
        foreach(string property in new[]{"_MainTex","_BumpMap","_ARM"})m.SetTextureScale(property,Vector2.one/source.tile);
        var saved=Save(m,Output+"/Materials/"+id+".mat");textureSets[source.color]=saved;return saved;
    }
    public static Material SourceMaterial(string id,string slug,float metres,Color tint)
    {
        string tex=Source+"/Textures";
        var m=MaterialFor(id,new MapSet{color=FileIn(tex,slug+"_diff_4k.jpg"),normal=FileIn(tex,slug+"_nor_gl_4k.exr"),arm=FileIn(tex,slug+"_arm_4k.png"),tile=metres});
        m.SetColor("_Color",tint);EditorUtility.SetDirty(m);return m;
    }
    public static Material LibraryMetal(string id,Color tint)
    {
        string tex=Source+"/Textures",prefix="Metal048A_4K-JPG_";
        var m=MaterialFor(id,new MapSet{color=FileIn(tex,prefix+"Color.jpg"),normal=FileIn(tex,prefix+"NormalGL.jpg"),
            arm=FileIn(tex,"Metal048A_ARM_4k.png"),tile=.65f});
        m.SetColor("_Color",tint);EditorUtility.SetDirty(m);return m;
    }
    public static Material LibraryLeather(string id,Color tint)
    {
        string tex=Source+"/Textures",prefix="Leather037_4K-JPG_";
        var m=MaterialFor(id,new MapSet{color=FileIn(tex,prefix+"Color.jpg"),normal=FileIn(tex,prefix+"NormalGL.jpg"),
            arm=FileIn(tex,"Leather037_ARM_4k.png"),tile=.6f});
        m.SetColor("_Color",tint);EditorUtility.SetDirty(m);return m;
    }

    static Material FoamMaterial()
    {
        // Shader must live outside the Editor-only directory for Android:
        // preserve the source template, publish one GUID-stable player shader.
        string template="Assets/Editor/Mistrzowie/BraxChampionsBeachFoam.shader";
        string path=Output+"/MistrzowieBeachFoam.shader";
        if(!File.Exists(template))throw new FileNotFoundException("Beach foam source shader missing",template);
        const string sourceName="Shader \"Braxhorn/MistrzowieBeachFoam\"";
        string source=File.ReadAllText(template);
        if(!source.Contains(sourceName))throw new InvalidOperationException("Beach foam source identity changed");
        File.WriteAllText(path,source.Replace(sourceName,"Shader \"Braxhorn/MistrzowieBeachFoamMobile\""));
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var shader=AssetDatabase.LoadAssetAtPath<Shader>(path);
        if(!shader||ShaderUtil.ShaderHasError(shader))
            throw new InvalidOperationException("Generated Android beach foam shader failed import: "+path);
        var material=new Material(shader){name="Mistrzowie | natural shallow surf and foam",enableInstancing=true};
        material.SetColor("_FoamTint",new Color(.98f,.99f,.96f,1f));
        material.SetFloat("_WaveStrength",.72f);
        return Save(material,Output+"/Materials/beach_foam.mat");
    }
    static Material SeaMaterial()
    {
        string path=Output+"/MistrzowieSea.shader";
        string source=File.ReadAllText(WaterTemplate);
        const string original="Shader \"Braxhorn/HarbourWaterMobile\"";
        if(!source.Contains(original))throw new InvalidOperationException("Water shader contract changed; review before reuse");
        File.WriteAllText(path,source.Replace(original,"Shader \"Braxhorn/MistrzowieSea\""));
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var shader=AssetDatabase.LoadAssetAtPath<Shader>(path);
        if(!shader||ShaderUtil.ShaderHasError(shader))throw new InvalidOperationException("Mistrzowie sea shader import failed");
        var m=new Material(shader){name="Mistrzowie sea"};
        m.SetVector("_NearRectangle",new Vector4(0,-500*K,1200*K,-150*K));
        m.SetFloat("_SwellStrength",.72f);
        m.SetFloat("_RippleStrength",.86f);
        m.SetFloat("_ReflectionStrength",.55f);
        if(m.HasProperty("_EdgeColor"))
            m.SetColor("_EdgeColor",new Color(.12f,.25f,.27f,1f));
        m.SetVector("_SunDirection",BraxCityMaterials.SunDirection);
        return Save(m,Output+"/Materials/sea.mat");
    }
}

