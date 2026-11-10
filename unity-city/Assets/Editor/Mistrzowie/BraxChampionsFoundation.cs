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
        public Pavilion[] promenade;
        // owner 10.11 promenade: palm beds of the seaside row, benches / lamps between them, gangway openings
        public PalmBed[] palmBeds;
        public Furniture[] furniture;
        public Opening[] openings;
    }
    [Serializable] public sealed class PalmBed { public float x,z,w,d; }
    [Serializable] public sealed class Furniture { public string kind; public float x,z; }
    [Serializable] public sealed class Opening { public float x0,x1; }
    [Serializable] public sealed class MeshData { public string id, role; public float[] positions, uv; public int[] triangles; }
    [Serializable] public sealed class Plant { public string id, kind, band; public float x,z,yaw; public int variant; }
    [Serializable] public sealed class Building { public string id,name; public float x,z,w,d,height; }
    [Serializable] public sealed class Stop { public float x,z; }
    [Serializable] public sealed class Pavilion
    {
        public string id,name,sign,style;
        public float x,z,w,d,h;
        public bool walkIn;
    }
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
        if(d.meshes==null || d.meshes.Length==0 || d.plants==null || d.berths==null || d.berths.Length!=38 || d.piers==null || d.piers.Length!=48 || d.promenade==null || d.promenade.Length!=12 || d.palmBeds==null || d.palmBeds.Length<45 || d.openings==null || d.openings.Length!=5
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
        // Owner 10.11 09:01: "plażę poprawić ... kawałek jest chujowy, wygląda niby mokra" - the outer quarter of the beach
        // was damp_sand at a 2 m tile (dark pebbly, repeating). One golden coast_sand_02 to the water now; the last ~7 m
        // (sand_wet) is the SAME sand, only darker (a separate MapSet = its own material tint below).
        foreach(string role in new[]{"sand_dry","sand_wet"})
            maps[role]=new MapSet{color=FileIn(tex,"coast_sand_02_diff_4k.jpg"),normal=FileIn(tex,"coast_sand_02_nor_gl_4k.exr"),arm=FileIn(tex,"coast_sand_02_arm_4k.png"),tile=15f};
        maps["grass"]=new MapSet{color=FileIn(tex,"Grass004_4K-JPG_Color.jpg"),normal=FileIn(tex,"Grass004_4K-JPG_NormalGL.jpg"),arm=FileIn(tex,"Grass004_ARM_4k.png"),tile=1.4f};
        foreach(string pair in new[]{"asphalt|clean_asphalt|2.1","paving|concrete_pavement_02|1.8"})
        {
            var a=pair.Split('|');string slug=a[1];
            maps[a[0]]=new MapSet{color=FileIn(tex,slug+"_diff_4k.jpg"),normal=FileIn(tex,slug+"_nor_gl_4k.exr"),arm=FileIn(tex,slug+"_arm_4k.png"),tile=a[0]=="asphalt"?2.1f:1.8f};
        }
        // Real Poly Haven PBR for working docks: hard wearing timber on
        // floating pontoons, granite on high passenger/cruise berths.
        // Owner 10.11: the pontoons were furniture veneer (teak_veneer, read pink in the game). Real marina decking:
        // ambientCG Planks040 (weathered grey deck boards with gaps and screw lines, CC0), 2 m tile.
        maps["quay_wood"]=new MapSet{
            color=FileIn(tex,"Planks040_4K-JPG_Color.jpg"),
            normal=FileIn(tex,"Planks040_4K-JPG_NormalGL.jpg"),
            arm=FileIn(tex,"Planks040_ARM_4k.png"),tile=2.0f};
        maps["quay_jetski"]=maps["quay_wood"];
        maps["quay_ramp"]=maps["quay_wood"];
        maps["quay_ramp_jetski"]=maps["quay_wood"];
        maps["quay_ramp_along"]=maps["quay_wood"];
        // Premium promenade paving (owner 10.11 "promenady ładne, ekskluzywne"): ambientCG PavingStones126A, large
        // vein-cut light stone slabs (~0.9 m, CC0), on the promenade and the kiosk pads only; city sidewalks unchanged.
        maps["prom_paving"]=new MapSet{
            color=FileIn(tex,"PavingStones126A_4K-JPG_Color.jpg"),
            normal=FileIn(tex,"PavingStones126A_4K-JPG_NormalGL.jpg"),
            arm=FileIn(tex,"PavingStones126A_ARM_4k.png"),tile=4.5f};
        maps["quay_cruise"]=new MapSet{
            color=FileIn(tex,"granite_tile_diff_4k.jpg"),
            normal=FileIn(tex,"granite_tile_nor_gl_4k.exr"),
            arm=FileIn(tex,"granite_tile_arm_4k.png"),tile=1.7f};
        maps["quay_ferry"]=maps["quay_cruise"];
        // floating pontoon sides down to just below the water: dark composite floats, not a concrete pier wall
        maps["pontoon_side"]=new MapSet{
            color=FileIn(tex,"concrete_wall_008_diff_4k.jpg"),
            normal=FileIn(tex,"concrete_wall_008_nor_gl_4k.exr"),
            arm=FileIn(tex,"concrete_wall_008_arm_4k.png"),tile=1.5f};
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
    public static void Optional(string what,Action build)
    {
        // Global rule 6.11 §4: a decoration/building that throws must not leave a
        // half-built object behind (build 32 left one VIP chair lying on its side
        // and shipped its whole 2K GLB). Snapshot the scene, and on failure remove
        // every NEW subtree root created by this step.
        var before=new HashSet<int>();
        foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            before.Add(t.gameObject.GetInstanceID());
        try{build();}
        catch(Exception e)
        {
            int removed=0;
            foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(!t||before.Contains(t.gameObject.GetInstanceID()))continue;
                // Only the top of each new subtree: its parent existed before.
                if(t.parent&&!before.Contains(t.parent.gameObject.GetInstanceID()))continue;
                UnityEngine.Object.DestroyImmediate(t.gameObject);removed++;
            }
            BraxBuildQuality.Warn("Miasto Mistrzów: pominięto "+what+": "+e.GetType().Name+": "+e.Message+
                " | usunięto pół-ustawione obiekty: "+removed);
            Debug.LogException(e);
        }
    }
    [MenuItem("BRAXHORN CITY/Miasto Mistrzow/1 - Verify foundation inputs")]
    public static void VerifyInputs()
    {
        var d=Read();Sources();Palms();Tree();
        // Source-only verification before creating expensive Unity scenes.
        // Downloaded originals must include full 2K albedo/normal/ARM.
        // 10.11: the kiosk names are solid 3D letters (HarbourSigns.bytes), the provisional 3x4 sign atlas is no longer used.
        BraxChampionsPromenadeGoods.Verify25OriginalModels();
        if(!File.Exists("Assets/Source/HarborBeach/Models/lifeguard_tower.glb"))
            throw new FileNotFoundException("Missing owner R1 lifeguard tower; no substitute");
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
        // owner 10.11 "piasek zrobić ładny": light cream-golden, less orange than the 6.11 tint (1.52,1.40,1.12)
        // review 10.11 10:00: the old tint (1.46,1.37,1.17) gave orange sand (188,156,106); light Mediterranean sand now
        materials["sand_dry"].SetColor("_Color",new Color(1.59f,1.68f,1.81f,1f));
        materials["sand_wet"].SetColor("_Color",new Color(1.30f,1.38f,1.50f,1f));
        // marina decking: silver-grey weathered boards (Planks040 alone is pink-brown 155,132,118 -> ~146,143,139), light granite quays
        materials["quay_wood"].SetColor("_Color",new Color(.94f,1.08f,1.18f,1f));
        materials["quay_cruise"].SetColor("_Color",new Color(1.28f,1.28f,1.26f,1f));
        EditorUtility.SetDirty(materials["quay_wood"]);EditorUtility.SetDirty(materials["quay_cruise"]);
        materials["pontoon_side"].SetColor("_Color",new Color(.20f,.21f,.22f,1f));
        EditorUtility.SetDirty(materials["pontoon_side"]);
        // concrete_wall_008 is khaki (141,134,112): neutral grey harbour concrete
        materials["quay_wall"].SetColor("_Color",new Color(.93f,.97f,1.15f,1f));
        EditorUtility.SetDirty(materials["quay_wall"]);
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
            // Instantiate 12 actual 2K-PBR premium shop shells on REMOVED
            // inland lawn patches, with no duplicate floor under their slab.
            // Dekoracje (promenada, plaża, flota) nie mogą zatrzymać całego miasta:
            // błąd = ostrzeżenie w raporcie, reszta świata buduje się dalej.
            Optional("promenada K1-K12",()=>BraxChampionsPromenade.Build(root,d.promenade));
            // Original first-city tower and R1-size loungers. No staff/visitors.
            Optional("plaża: beach club (kładki, prysznice, leżaki z parasolami, ratownik, boje)",()=>BraxChampionsBeachFurnishings.Build(root));
            // Place the ORIGINAL user-library yacht, Riva and jetski models.
            // No fabricated cruise/ferry vessel is allowed without its source.
            int boats=0;
            Optional("flota w porcie",()=>boats=BraxChampionsHarbour.PlaceOriginalFleet(root,d.berths,d.piers,d.openings));
            // owner 10.11: palms in planted beds along the promenade, benches, lamps, bins, beach boardwalks
            Optional("promenada: kwatery palm, ławki, latarnie, kładki na plażę",()=>BraxChampionsPromenadeFurniture.Build(root,d.palmBeds,d.furniture));
            if(boats!=35)BraxBuildQuality.Warn("Port: ustawiono "+boats+" łodzi zamiast 35 (15 jachtów + 10 Riva + 10 skuterów)");
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
        if(!texture||importer.textureType!=TextureImporterType.Default
            ||importer.sRGBTexture!=(role=="color")||!android.overridden||android.maxTextureSize!=2048
            ||android.format!=BraxChampionsTextureImport.AndroidFormat(destination)||android.compressionQuality!=100)
            throw new InvalidOperationException("Generated PBR import contract failed: "+destination);
        // The original provider file must remain genuine 4K. Android's
        // compressed/imported Texture2D dimensions are not source dimensions.
        importer.GetSourceTextureWidthAndHeight(out int sourceWidth,out int sourceHeight);
        // Poly Haven provider-native 4K maps can preserve a non-square captured
        // aspect ratio (for example velour_velvet diffuse is 4096x3941).
        // prepare_assets.py already pins the exact provider bytes/checksums, so
        // this Unity-side guard verifies the 4K long edge without resampling.
        // Official provider-native "4K" maps may preserve non-square capture
        // dimensions, including a long edge slightly above 4096 (e.g. 4096x4181).
        // prepare_assets.py verifies the downloaded provider bytes/metadata first.
        if(Mathf.Max(sourceWidth,sourceHeight)<4096||Mathf.Min(sourceWidth,sourceHeight)<2048)
            throw new InvalidOperationException("Original 4K PBR source changed: "+destination+
                " source="+sourceWidth+"x"+sourceHeight);
        if(texture.width!=2048||texture.height!=2048)
            BraxBuildQuality.Warn("Original PBR source remains 4K; Unity imported "+
                texture.width+"x"+texture.height+" for "+destination+
                ". Android ASTC override remains 2K; inspect on device.");
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
    // Polished black marble with thin white veins (owner vitrine photo 9.11).
    // ambientCG Marble016 4K (CC0), prepared by prepare_assets.py like Leather037.
    public static Material LibraryMarble(string id,Color tint,float metres)
    {
        string tex=Source+"/Textures",prefix="Marble016_4K-JPG_";
        var m=MaterialFor(id,new MapSet{color=FileIn(tex,prefix+"Color.jpg"),normal=FileIn(tex,prefix+"NormalGL.jpg"),
            arm=FileIn(tex,"Marble016_ARM_4k.png"),tile=metres});
        m.SetColor("_Color",tint);EditorUtility.SetDirty(m);return m;
    }
    // Polished WHITE Carrara-type marble in seamless slabs (owner 9.11 23:00: marble_01 read as beige bathroom
    // tiles). Poly Haven has no white marble; ambientCG Marble021 4K (CC0), prepared like Marble016.
    public static Material LibraryMarbleWhite(string id,Color tint,float metres)
    {
        string tex=Source+"/Textures",prefix="Marble021_4K-JPG_";
        var m=MaterialFor(id,new MapSet{color=FileIn(tex,prefix+"Color.jpg"),normal=FileIn(tex,prefix+"NormalGL.jpg"),
            arm=FileIn(tex,"Marble021_ARM_4k.png"),tile=metres});
        m.SetColor("_Color",tint);EditorUtility.SetDirty(m);return m;
    }
    // Vein-cut travertine (ambientCG Travertine009 4K, CC0) for the Hotel Sobczak podium: owner 9.11 23:29
    // "ta z dołu kondygnacja taka szara ... jakiejś tekstury dobrej materiału".
    public static Material LibraryTravertine(string id,Color tint,float metres)
    {
        string tex=Source+"/Textures",prefix="Travertine009_4K-JPG_";
        var m=MaterialFor(id,new MapSet{color=FileIn(tex,prefix+"Color.jpg"),normal=FileIn(tex,prefix+"NormalGL.jpg"),
            arm=FileIn(tex,"Travertine009_ARM_4k.png"),tile=metres});
        m.SetColor("_Color",tint);EditorUtility.SetDirty(m);return m;
    }
    // Any other ambientCG 4K pack chosen by the owner from the material board (10.11: Leather013 hall walls, Tiles012
    // lobby floor). prepare_assets.py extracts <asset>_4K-JPG_{Color,NormalGL,Roughness}.jpg and packs <asset>_ARM_4k.png.
    public static Material LibraryAcg(string id,string asset,Color tint,float metres)
    {
        string tex=Source+"/Textures",prefix=asset+"_4K-JPG_";
        var m=MaterialFor(id,new MapSet{color=FileIn(tex,prefix+"Color.jpg"),normal=FileIn(tex,prefix+"NormalGL.jpg"),
            arm=FileIn(tex,asset+"_ARM_4k.png"),tile=metres});
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

