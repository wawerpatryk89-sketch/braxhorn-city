using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// All visible buildings are imported original geometry. Generated boxes and
// cylinders below are invisible source physics, never substitute buildings.
public static class BraxCityBuild
{
    const string Source="Assets/Source/City/",Generated="Assets/PlayableGenerated",Output=Generated+"/City";
    [Serializable] public sealed class Box { public float minX,maxX,minZ,maxZ,top,bottom;public string tag; }
    [Serializable] public sealed class Circle { public float x,z,r,top,bottom;public string tag; }
    [Serializable] public sealed class Pit { public float minX,maxX,minZ,maxZ,y; }
    [Serializable] public sealed class Walkable { public float x0,x1,z0,z1,x,z,radius,y; }
    [Serializable] public sealed class Collision { public Box[] boxes;public Circle[] circles;public Pit[] pits;public Walkable[] walkable; }
    [Serializable] public sealed class BoundsData { public float[] min,max; }
    [Serializable] public sealed class MeshData { public string name,originalName;public int vertices;public float triangles;public bool collider,ground,dynamicDoor; }
    [Serializable] public sealed class MaterialData { public string name,originalName,lightMap;public bool baked,vertexColors,transparent,doubleSided;public float opacity,alphaTest,lightMapGain; }
    [Serializable] public sealed class Point { public string id,label;public float x,y,z,face; }
    [Serializable] public sealed class Leaf { public string name;public float side,width,height,center; }
    [Serializable] public sealed class Door { public string id,label;public float x,y,z,yaw,width,height;public bool single;public Leaf[] leaves; }
    [Serializable] public sealed class Manifest { public string area;public int meshCount;public float triangles;public MaterialData[] materials;public MeshData[] meshes;public Door[] doors;public Point[] points;public Collision collision;public BoundsData bounds; }
    [Serializable] public sealed class AreaReport { public string area;public int originalMeshes,importedMeshes,originalTriangles,sourceBoxes,sourceCircles,sourcePits,meshColliders,doors,bakedMaterials,sourcePoints; }
    [Serializable] public sealed class Report { public string commit,unity,browserSource="260a5897472d42d099766da48e708859bba6c3dc",scope="original-city-and-estate-exploration";public bool originalCityReplaced=false,originalModels=true,originalBraxRig=true;public AreaReport[] areas;public string[] notYetPorted={"estate NPC schedules and combat AI","estate story and economy","additional playable character selection"}; }
    static readonly List<AreaReport> reports=new List<AreaReport>();
    static GameObject Import(string path)
    {
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(path)??AssetDatabase.LoadAllAssetsAtPath(path).OfType<GameObject>().FirstOrDefault();
        if(!source)throw new InvalidOperationException("Missing original model: "+path);
        var result=UnityEngine.Object.Instantiate(source);result.name=Path.GetFileNameWithoutExtension(path);return result;
    }
    static Vector3 Convert(float x,float y,float z)=>new Vector3(-x,y,z);
    static void PhysicsBox(Transform root,string name,float minX,float maxX,float minZ,float maxZ,float bottom,float top)
    {
        if(maxX<=minX||maxZ<=minZ||top<=bottom)return;
        var o=new GameObject(name);o.transform.SetParent(root,false);o.transform.position=Convert((minX+maxX)/2,(bottom+top)/2,(minZ+maxZ)/2);
        o.AddComponent<BoxCollider>().size=new Vector3(maxX-minX,top-bottom,maxZ-minZ);
    }
    static Texture2D LinearTexture(string file)
    {
        string path=Source+file;var importer=AssetImporter.GetAtPath(path) as TextureImporter;
        if(importer==null)throw new Exception("Original lightmap not imported: "+file);
        importer.sRGBTexture=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=true;importer.maxTextureSize=4096;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static int ConvertBaked(GameObject root,Manifest data)
    {
        var shader=Shader.Find("Braxhorn/OriginalCityBaked");if(!shader)throw new Exception("Source-preserving city shader is missing");
        var roles=data.materials.ToDictionary(m=>m.name,m=>m);
        var converted=new Dictionary<Material,Material>();
        foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            var mats=renderer.sharedMaterials;
            for(int i=0;i<mats.Length;i++)
            {
                var original=mats[i];if(!original)throw new Exception("Original renderer material absent: "+renderer.name);
                string name=original.name.Replace(" (Instance)","");
                if(!roles.TryGetValue(name,out var role)||!role.baked)continue;
                if(!converted.TryGetValue(original,out var material))
                {
                    material=new Material(shader){name=name};
                    string map=original.GetTexturePropertyNames().FirstOrDefault(n=>n=="_MainTex"||n.ToLowerInvariant().Contains("basecolor"));
                    if(map!=null){material.mainTexture=original.GetTexture(map);material.mainTextureScale=original.GetTextureScale(map);material.mainTextureOffset=original.GetTextureOffset(map);}
                    foreach(string prop in new[]{"_BaseColor","_Color","baseColorFactor","_BaseColorFactor"})if(original.HasProperty(prop)){material.color=original.GetColor(prop);break;}
                    material.SetFloat("_VertexColor",role.vertexColors?1:0);material.SetFloat("_Cull",role.doubleSided?0:2);material.SetFloat("_Cutoff",role.alphaTest);
                    if(!string.IsNullOrEmpty(role.lightMap)){material.SetTexture("_OriginalLight",LinearTexture(role.lightMap));material.SetFloat("_UseLight",1);material.SetFloat("_LightGain",role.lightMapGain);}
                    if(role.transparent){material.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);material.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);material.SetFloat("_ZWrite",0);material.renderQueue=3000;material.SetOverrideTag("RenderType","Transparent");}
                    else if(role.alphaTest>0){material.renderQueue=2450;material.SetOverrideTag("RenderType","TransparentCutout");}
                    AssetDatabase.CreateAsset(material,Output+"/"+name+".mat");converted[original]=material;
                }
                mats[i]=material;
            }
            renderer.sharedMaterials=mats;
        }
        return converted.Count;
    }
    static void BaseFloor(Manifest data,Transform root)
    {
        // Partition World.baseAt() at the exact original negative-height pit
        // edges. A solid y=0 plane would seal the NOVA stairwell and cellar.
        var pits=data.collision.pits??Array.Empty<Pit>();
        float x0=data.bounds.min[0],x1=data.bounds.max[0],z0=data.bounds.min[2],z1=data.bounds.max[2];
        var xs=new SortedSet<float>{x0,x1};var zs=new SortedSet<float>{z0,z1};
        foreach(var p in pits){xs.Add(p.minX);xs.Add(p.maxX);zs.Add(p.minZ);zs.Add(p.maxZ);}
        float[] x=xs.ToArray(),z=zs.ToArray();
        for(int i=0;i<x.Length-1;i++)for(int j=0;j<z.Length-1;j++)
        {
            float cx=(x[i]+x[i+1])/2,cz=(z[j]+z[j+1])/2,y=0;
            foreach(var p in pits)if(cx>=p.minX&&cx<=p.maxX&&cz>=p.minZ&&cz<=p.maxZ){y=p.y;break;}
            PhysicsBox(root,"Original virtual floor",x[i],x[i+1],z[j],z[j+1],y-.18f,y);
        }
    }
    static BraxCityWorld AddArea(string area,BraxGymPlayer player)
    {
        Directory.CreateDirectory(Output);AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var data=JsonUtility.FromJson<Manifest>(File.ReadAllText(Source+area+"-manifest.json"));
        if(data==null||data.meshCount<100||data.meshes==null||data.collision==null)throw new Exception("Incomplete original city manifest: "+area);
        var root=Import(Source+"braxhorn-"+area+".gltf");
        if(area=="city"&&Environment.GetEnvironmentVariable("BRAX_CITY_MOBILE_REVIEW")=="1"
            &&PrefabUtility.IsPartOfPrefabInstance(root))
        {
            // The original city glTF asset remains completely untouched.
            // The generated review scene must not retain a prefab reference to
            // hundreds of vehicle meshes/materials after those renderers are
            // removed from the scene. This only unpacks this temporary instance.
            PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.Completely,
                InteractionMode.AutomatedAction);
            Debug.Log("CITY_IPHONE_REVIEW_UNPACKED_TEMPORARY_CITY_INSTANCE sourcePrefabPreserved=true");
        }
        var transforms=root.GetComponentsInChildren<Transform>(true).GroupBy(t=>t.name).ToDictionary(g=>g.Key,g=>g.First());
        int count=0,meshColliders=0;
        foreach(var mesh in data.meshes)
        {
            if(!transforms.TryGetValue(mesh.name,out var t)||t.GetComponentsInChildren<MeshFilter>(true).Length==0)throw new Exception("Original geometry missing after Unity import: "+mesh.name);
            count++;
            if(mesh.collider&&!mesh.dynamicDoor){foreach(var mf in t.GetComponentsInChildren<MeshFilter>(true)){var c=mf.gameObject.AddComponent<MeshCollider>();c.sharedMesh=mf.sharedMesh;meshColliders++;}}
        }
        if(count!=data.meshCount)throw new Exception("Original city mesh count differs after Unity import");
        int baked=ConvertBaked(root,data);
        if(area=="city"&&baked<10)throw new Exception("Original baked city materials did not survive import");
        var physics=new GameObject("Original "+area+" collisions").transform;
        BaseFloor(data,physics);
        foreach(var b in data.collision.boxes??Array.Empty<Box>())PhysicsBox(physics,"Source "+b.tag,b.minX,b.maxX,b.minZ,b.maxZ,b.bottom,b.top);
        foreach(var c in data.collision.circles??Array.Empty<Circle>())
        {
            if(c.r<=0||c.top<=c.bottom)continue;
            var o=GameObject.CreatePrimitive(PrimitiveType.Cylinder);o.name="Original round collider "+c.tag;o.transform.SetParent(physics,false);o.transform.position=Convert(c.x,(c.top+c.bottom)/2,c.z);o.transform.localScale=new Vector3(c.r*2,(c.top-c.bottom)/2,c.r*2);
            var mesh=o.GetComponent<MeshFilter>().sharedMesh;UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());UnityEngine.Object.DestroyImmediate(o.GetComponent<MeshRenderer>());o.AddComponent<MeshCollider>().sharedMesh=mesh;
        }
        foreach(var w in data.collision.walkable??Array.Empty<Walkable>())
        {
            if(w.radius>0){var o=GameObject.CreatePrimitive(PrimitiveType.Cylinder);o.name="Original circular walkable surface";o.transform.SetParent(physics,false);o.transform.position=Convert(w.x,w.y-.05f,w.z);o.transform.localScale=new Vector3(w.radius*2,.05f,w.radius*2);var mesh=o.GetComponent<MeshFilter>().sharedMesh;UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());UnityEngine.Object.DestroyImmediate(o.GetComponent<MeshRenderer>());o.AddComponent<MeshCollider>().sharedMesh=mesh;}
            else PhysicsBox(physics,"Original walkable surface",w.x0,w.x1,w.z0,w.z1,w.y-.1f,w.y);
        }
        var doors=new List<BraxCityDoor>();
        foreach(var d in data.doors??Array.Empty<Door>())
        {
            var doorObject=new GameObject("Original door "+d.id);doorObject.transform.position=Convert(d.x,d.y,d.z);doorObject.transform.rotation=Quaternion.Euler(0,-d.yaw*Mathf.Rad2Deg,0);
            var behaviour=doorObject.AddComponent<BraxCityDoor>();behaviour.id=d.id;behaviour.label=d.label;behaviour.leaves=new Transform[d.leaves.Length];behaviour.sides=new float[d.leaves.Length];
            for(int i=0;i<d.leaves.Length;i++)
            {
                var leaf=d.leaves[i];if(!transforms.TryGetValue(leaf.name,out var t))throw new Exception("Original door hinge missing "+leaf.name);
                t.SetParent(doorObject.transform,true);behaviour.leaves[i]=t;behaviour.sides[i]=leaf.side;
                var collider=t.gameObject.AddComponent<BoxCollider>();collider.center=new Vector3(-leaf.center,leaf.height/2,0);collider.size=new Vector3(leaf.width,leaf.height,.14f);
            }
            doors.Add(behaviour);
        }
        var world=new GameObject("Original "+area+" navigation").AddComponent<BraxCityWorld>();world.area=area;world.player=player;world.doors=doors.ToArray();world.originalMeshes=count;world.originalTriangles=(int)data.triangles;
        world.sourceColliders=(data.collision.boxes?.Length??0)+(data.collision.circles?.Length??0)+meshColliders;
        world.points=(data.points??Array.Empty<Point>()).Where(p=>p.id!=null&&!p.id.Contains("camera")&&!p.id.Contains("enemy")&&!p.id.Contains("item")).Select(p=>new BraxCityPoint{id=p.id,label=p.label,position=Convert(p.x,p.y,p.z)}).ToArray();
        player.cityWorld=world;
        if(area=="city")BraxCityRebuild.Apply(world,root.transform,physics);
        reports.Add(new AreaReport{area=area,originalMeshes=data.meshCount,importedMeshes=count,originalTriangles=(int)data.triangles,sourceBoxes=data.collision.boxes?.Length??0,sourceCircles=data.collision.circles?.Length??0,sourcePits=data.collision.pits?.Length??0,meshColliders=meshColliders,doors=doors.Count,bakedMaterials=baked,sourcePoints=world.points.Length});
        Debug.Log("ORIGINAL_CITY_AREA_IMPORTED "+JsonUtility.ToJson(reports.Last()));
        return world;
    }
    public static void AttachCity(BraxGymPlayer player)
    {
        reports.Clear();AddArea("city",player);
        player.worldSpawn=new Vector3(1.5f,.08f,2.8f);
        player.view.farClipPlane=420;
        player.view.backgroundColor=new Color(.67f,.75f,.81f);
        // City scene only: actual visible cloud sky and solar disc from a
        // single compressed 1K panorama. Built-In gym/estate remain untouched.
        BraxCitySkyAuthoring.ApplyToCity(player.view);
    }
    public static string CreateEstate(string commit)
    {
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var player=new GameObject("BraxhornPlayer");player.layer=8;player.transform.position=new Vector3(0,.08f,92);
        var body=player.AddComponent<CharacterController>();body.height=2.25f;body.radius=.35f;body.center=new Vector3(0,1.125f,0);body.stepOffset=.36f;body.slopeLimit=48;body.skinWidth=.035f;
        var hero=Import("Assets/Source/BraxOriginal.glb");hero.transform.SetParent(player.transform,false);hero.transform.localScale=Vector3.one*.024f;
        foreach(var t in hero.GetComponentsInChildren<Transform>(true))t.gameObject.layer=8;
        var heroMaterial=AssetDatabase.LoadAssetAtPath<Material>(Generated+"/Premium/BraxSourceORM.mat");if(!heroMaterial)throw new Exception("Original Brax premium material missing");
        foreach(var r in hero.GetComponentsInChildren<SkinnedMeshRenderer>(true)){r.sharedMaterials=Enumerable.Repeat(heroMaterial,r.sharedMaterials.Length).ToArray();r.updateWhenOffscreen=true;r.shadowCastingMode=ShadowCastingMode.Off;}
        var animation=hero.GetComponentInChildren<Animation>(true);if(!animation)animation=hero.AddComponent<Animation>();animation.playAutomatically=false;
        foreach(var name in new[]{"Idle","Walk","Run","Punch","Kick","Jump"}){var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Generated+"/"+name+".anim");if(!clip)throw new Exception("Original animation absent "+name);animation.AddClip(clip,name);}
        animation.clip=animation.GetClip("Idle");animation.Play("Idle");animation.Sample();
        var camera=new GameObject("Estate manual player camera").AddComponent<Camera>();camera.tag="MainCamera";camera.fieldOfView=55;camera.nearClipPlane=.1f;camera.farClipPlane=330;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.74f,.80f,.82f);camera.allowMSAA=true;camera.allowHDR=false;camera.gameObject.AddComponent<AudioListener>();
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.64f,.70f,.77f);RenderSettings.ambientEquatorColor=new Color(.43f,.46f,.48f);RenderSettings.ambientGroundColor=new Color(.24f,.25f,.23f);RenderSettings.fog=false;
        var sun=new GameObject("Estate daylight").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.2f;sun.color=new Color(1,.96f,.89f);sun.transform.rotation=Quaternion.Euler(48,-35,0);sun.shadows=LightShadows.Soft;sun.shadowStrength=.55f;sun.shadowBias=.04f;sun.shadowNormalBias=.25f;
        QualitySettings.shadows=ShadowQuality.All;QualitySettings.shadowDistance=42;QualitySettings.shadowResolution=ShadowResolution.Medium;QualitySettings.shadowCascades=2;
        var shadow=GameObject.CreatePrimitive(PrimitiveType.Quad);shadow.name="Original Brax contact shadow";UnityEngine.Object.DestroyImmediate(shadow.GetComponent<Collider>());shadow.transform.rotation=Quaternion.Euler(90,0,0);shadow.transform.localScale=new Vector3(1.6f,1.4f,1);shadow.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Generated+"/ContactShadow.mat");shadow.layer=8;
        var runtime=player.AddComponent<BraxGymPlayer>();runtime.visual=hero.transform;runtime.body=body;runtime.clips=animation;runtime.view=camera;runtime.contactShadow=shadow.transform;runtime.bags=Array.Empty<Transform>();runtime.buildId=commit;runtime.worldSpawn=player.transform.position;
        AddArea("estate",runtime);
        const string path=Generated+"/BraxhornEstate.unity";if(!EditorSceneManager.SaveScene(scene,path))throw new Exception("Estate scene save failed");AssetDatabase.SaveAssets();return path;
    }
    public static void WriteReport(string output,string commit,bool cityReviewOnly=false,bool segmentReview=false)
    {
        if(cityReviewOnly)
        {
            if(reports.Count!=1||reports[0].area!="city")
                throw new Exception("iPhone street review must include only authored city scene");
        }
        else if(reports.Count!=2)
            throw new Exception("The original full-city build must retain both city and estate");
        var report=new Report{commit=commit,unity=Application.unityVersion,areas=reports.ToArray()};
        if(cityReviewOnly)
        {
            report.scope=segmentReview?"city-segment-mobile-architecture-review":"city-only-mobile-architecture-review";
            report.notYetPorted=new[]{
                "Separate original estate scene deliberately excluded from this review player (source untouched)",
                segmentReview?"Outside world deliberately excluded from this playable street segment (original source untouched)":"City architecture remains complete in this mobile city-only build",
                "City-street NPC and vehicle traffic not activated",
                "Estate story/economy and future character selection",
            };
        }
        File.WriteAllText(Path.Combine(output,"city-migration.json"),JsonUtility.ToJson(report,true));
        BraxCityRebuild.WriteReport(output);
    }
}
