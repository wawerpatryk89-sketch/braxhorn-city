using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class BraxGymBuild
{
    const string Generated="Assets/PlayableGenerated";
    [Serializable] class WorldData { public BoxData[] boxes; public CircleData[] circles; }
    [Serializable] class BoxData { public float minX,maxX,minZ,maxZ,top,bottom; public string tag; }
    [Serializable] class CircleData { public float x,z,r,top,bottom; public string tag; }
    [Serializable] class Report { public string purpose="playable-original-gym",unity,commit; public int gymMeshes,originalClips,colliders,matteSurfaces; public bool originalBrax=true,originalGym=true,touchControls=true,productionChanged=false; public string[] animationNames; }
    static string Clean(string n){return new string(n.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();}
    static GameObject ImportModel(string path)
    {
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(path)??AssetDatabase.LoadAllAssetsAtPath(path).OfType<GameObject>().FirstOrDefault();
        if(!model)throw new InvalidOperationException("Missing actual model: "+path);
        var instance=UnityEngine.Object.Instantiate(model);instance.name=Path.GetFileNameWithoutExtension(path);return instance;
    }
    static Transform Find(Transform root,string name){return root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==name);}
    static void Layer(Transform root,int layer){foreach(Transform t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=layer;}
    static void AddBox(Transform parent,string name,Vector3 center,Vector3 size)
    {var obj=new GameObject(name);obj.transform.SetParent(parent,false);obj.transform.position=center;obj.AddComponent<BoxCollider>().size=size;}
    public static void Build(){BuildInternal(false,false);}
    // Preserve the existing estate-only R2 path and its accepted gym appearance.
    public static void BuildCity(){BuildInternal(true,false);}
    // Only the isolated Unity6 integration branch calls this: gym + city + estate.
    public static void BuildIntegrated(){BuildInternal(true,true);}
    public static void BuildRenovation(){BuildInternal(true,true,true);}
    private static void BuildInternal(bool fullCity,bool integrated,bool renovation=false)
    {
        if(fullCity)EditorUserBuildSettings.webGLBuildSubtarget=WebGLTextureSubtarget.ETC2;
        Directory.CreateDirectory(Generated);AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        if(renovation)BraxNutritionShopBuild.ValidateSources();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var gym=ImportModel("Assets/Source/braxhorn-original-gym.glb");
        var world=JsonUtility.FromJson<WorldData>(File.ReadAllText("Assets/Source/gym-collisions.json"));
        if(world.boxes.Length<40)throw new Exception("Incomplete original collisions");
        var renderers=gym.GetComponentsInChildren<MeshRenderer>(true);
        if(renderers.Length<100)throw new Exception("Incomplete original gym");
        int tuned=0;var cache=new Dictionary<Material,Material>();
        Shader matte=Shader.Find("Braxhorn/BakedPremium");if(!matte)throw new Exception("Baked shader missing");
        foreach(var r in renderers)
        {
            r.shadowCastingMode=ShadowCastingMode.Off;
            var source=r.sharedMaterials;var materialSlots=new Material[source.Length];
            for(int k=0;k<source.Length;k++)
            {
                var m=source[k];materialSlots[k]=m;
                if(!m||!m.name.Contains("_baked")||m.renderQueue>=2450)continue;
                if(!cache.TryGetValue(m,out var t))
                {
                    t=new Material(matte){name=m.name};
                    string textureName=m.GetTexturePropertyNames().FirstOrDefault(n=>n.ToLowerInvariant().Contains("basecolor")||n=="_MainTex");
                    if(textureName!=null){t.mainTexture=m.GetTexture(textureName);t.mainTextureScale=m.GetTextureScale(textureName);t.mainTextureOffset=m.GetTextureOffset(textureName);}
                    foreach(var name in new[]{"_BaseColor","_Color","baseColorFactor","_BaseColorFactor"})if(m.HasProperty(name)){t.color=m.GetColor(name);break;}
                    foreach(var name in new[]{"_Cull","_CullMode","_CullModeForward"})if(m.HasProperty(name)){t.SetFloat("_Cull",m.GetFloat(name));break;}
                    t.SetFloat("_VertexColor",1);
                    string assetName=new string(m.name.Select(c=>char.IsLetterOrDigit(c)||c=='_'?c:'_').ToArray());
                    AssetDatabase.CreateAsset(t,Generated+"/"+assetName+".mat");
                    t.name=m.name;EditorUtility.SetDirty(t);cache[m]=t;tuned++;
                    Debug.Log("GYM_MATERIAL "+m.name+" shader="+m.shader.name+" texture="+textureName+" color="+t.color);
                }
                materialSlots[k]=t;
            }
            r.sharedMaterials=materialSlots;
        }
        var solids=new GameObject("Original collisions (glTF X conversion)");int n=0;GameObject doorCollider=null;
        foreach(var b in world.boxes)
        {
            if(b.top<=b.bottom||b.maxX<=b.minX||b.maxZ<=b.minZ)continue;
            string name="Collider "+n++ +" "+b.tag;
            AddBox(solids.transform,name,new Vector3(-(b.minX+b.maxX)/2,(b.top+b.bottom)/2,(b.minZ+b.maxZ)/2),new Vector3(b.maxX-b.minX,b.top-b.bottom,b.maxZ-b.minZ));
            if(b.tag=="door")doorCollider=solids.transform.GetChild(solids.transform.childCount-1).gameObject;
        }
        foreach(var c in world.circles)
        {
            AddBox(solids.transform,"Original round obstacle "+n++,new Vector3(-c.x,(c.top+c.bottom)/2,c.z),new Vector3(c.r*2,c.top-c.bottom,c.r*2));
        }
        AddBox(solids.transform,"Hall ground",new Vector3(0,-.10f,-2.5f),new Vector3(37.2f,.20f,28.2f));
        AddBox(solids.transform,"Alley ground",new Vector3(6.75f,-.10f,16.4f),new Vector3(16.7f,.20f,9.6f));
        var door=new GameObject("Original opening door");door.transform.position=new Vector3(6.8f,0,11.56f);
        for(int i=24;i<=31;i++){var part=Find(gym.transform,"Gym_Mesh_"+i);if(!part)throw new Exception("Original door part missing "+i);part.SetParent(door.transform,true);}
        if(doorCollider)doorCollider.transform.SetParent(door.transform,true);
        var bags=new List<Transform>();
        foreach(float z in new[]{-8.4f,-5.4f,-2.4f,.6f})
        {
            var bag=new GameObject("Original training bag "+z);bag.transform.position=new Vector3(14,4.6f,z);
            foreach(var r in renderers)
            {
                var b=r.bounds;
                if(Mathf.Abs(b.center.x-14)<.45f && Mathf.Abs(b.center.z-z)<.5f && b.size.x<1.4f && b.size.z<1.4f && b.center.y>1f && b.max.y<4.65f)r.transform.SetParent(bag.transform,true);
            }
            var bagBody=Find(bag.transform,"Gym_Mesh_"+(81+bags.Count*8));
            var bagRenderer=bagBody?bagBody.GetComponent<MeshRenderer>():null;
            if(!bagRenderer)throw new Exception("Original bag collider source missing "+bags.Count);
            AddBox(bag.transform,"Bag collision",bagRenderer.bounds.center,bagRenderer.bounds.size);
            bags.Add(bag.transform);
        }
        var player=new GameObject("BraxhornPlayer");player.transform.position=new Vector3(1.5f,.08f,2.8f);player.transform.rotation=Quaternion.Euler(0,180,0);player.layer=8;
        var body=player.AddComponent<CharacterController>();body.height=2.25f;body.radius=.35f;body.center=new Vector3(0,1.125f,0);body.stepOffset=.36f;body.slopeLimit=48;body.skinWidth=.035f;
        var hero=ImportModel("Assets/Source/BraxOriginal.glb");hero.transform.SetParent(player.transform,false);hero.transform.localScale=Vector3.one*.024f;Layer(hero.transform,8);
        foreach(var r in hero.GetComponentsInChildren<SkinnedMeshRenderer>(true)){r.updateWhenOffscreen=true;r.shadowCastingMode=ShadowCastingMode.Off;}
        var animation=hero.GetComponentInChildren<Animation>(true);if(!animation)animation=hero.AddComponent<Animation>();
        animation.playAutomatically=false;
        var imported=AssetDatabase.LoadAllAssetsAtPath("Assets/Source/BraxOriginal.glb").OfType<AnimationClip>().ToArray();
        Debug.Log("BRAX_SOURCE_CLIPS "+string.Join(" | ",imported.Select(c=>c.name)));
        var mappings=new Dictionary<string,string>{{"Idle","Idle"},{"Walk","Walking"},{"Run","Running"},{"Punch","Jab Cross (1)"},{"Kick","Mma Kick (1)"},{"Jump","Jump"}};
        foreach(var kv in mappings)
        {
            var source=imported.FirstOrDefault(c=>c.name==kv.Value)??imported.FirstOrDefault(c=>Clean(c.name)==Clean(kv.Value));
            if(!source)throw new Exception("Original animation absent: "+kv.Value);
            var clip=UnityEngine.Object.Instantiate(source);clip.name=kv.Key;clip.legacy=true;clip.wrapMode=kv.Key=="Idle"||kv.Key=="Walk"||kv.Key=="Run"?WrapMode.Loop:WrapMode.Once;
            AssetDatabase.CreateAsset(clip,Generated+"/"+kv.Key+".anim");animation.AddClip(clip,kv.Key);
        }
        animation.clip=animation.GetClip("Idle");animation.Play("Idle");animation.Sample();
        var camera=new GameObject("Player camera").AddComponent<Camera>();camera.tag="MainCamera";camera.fieldOfView=55;camera.nearClipPlane=.1f;camera.farClipPlane=100;camera.allowHDR=false;camera.allowMSAA=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.14f,.18f,.20f);camera.gameObject.AddComponent<AudioListener>();
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.68f,.70f,.72f);RenderSettings.ambientEquatorColor=new Color(.43f,.44f,.44f);RenderSettings.ambientGroundColor=new Color(.25f,.23f,.20f);RenderSettings.fog=false;
        var light=new GameObject("Soft neutral light for moving character").AddComponent<Light>();light.type=LightType.Directional;light.color=new Color(1,.96f,.9f);light.intensity=.75f;light.transform.rotation=Quaternion.Euler(55,-25,0);light.shadows=LightShadows.None;
        var fill=new GameObject("Soft interior fill").AddComponent<Light>();fill.type=LightType.Point;fill.transform.position=new Vector3(0,5,0);fill.range=26;fill.intensity=.6f;fill.color=new Color(.87f,.91f,1f);fill.shadows=LightShadows.None;
        var tex=new Texture2D(64,64,TextureFormat.RGBA32,false);tex.wrapMode=TextureWrapMode.Clamp;
        for(int y=0;y<64;y++)for(int x=0;x<64;x++){float r=new Vector2((x-31.5f)/31.5f,(y-31.5f)/31.5f).magnitude;tex.SetPixel(x,y,new Color(0,0,0,Mathf.Pow(Mathf.Clamp01(1-r),1.6f)*.5f));}tex.Apply();AssetDatabase.CreateAsset(tex,Generated+"/ContactShadow.asset");
        var shadowMat=new Material(Shader.Find("Unlit/Transparent"));shadowMat.mainTexture=tex;AssetDatabase.CreateAsset(shadowMat,Generated+"/ContactShadow.mat");
        var shadow=GameObject.CreatePrimitive(PrimitiveType.Quad);shadow.name="Soft player contact shadow";UnityEngine.Object.DestroyImmediate(shadow.GetComponent<Collider>());shadow.transform.rotation=Quaternion.Euler(90,0,0);shadow.transform.localScale=new Vector3(1.6f,1.4f,1);shadow.GetComponent<Renderer>().sharedMaterial=shadowMat;shadow.layer=8;
        var runtime=player.AddComponent<BraxGymPlayer>();runtime.visual=hero.transform;runtime.body=body;runtime.view=camera;runtime.clips=animation;runtime.door=door.transform;runtime.bags=bags.ToArray();runtime.contactShadow=shadow.transform;runtime.buildId=Environment.GetEnvironmentVariable("GITHUB_SHA")??"unity-gym";
        QualitySettings.antiAliasing=4;QualitySettings.pixelLightCount=2;QualitySettings.shadows=ShadowQuality.Disable;QualitySettings.anisotropicFiltering=AnisotropicFiltering.ForceEnable;
        PlayerSettings.colorSpace=ColorSpace.Linear;PlayerSettings.companyName="Braxhorn";PlayerSettings.productName="Braxhorn Gym";PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;
        PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Gzip;PlayerSettings.WebGL.decompressionFallback=true;PlayerSettings.WebGL.template="APPLICATION:Default";
        BraxGymPremium.Apply(gym,hero,bags.ToArray(),camera,light,fill,Generated);
        BraxHeroRefinements.Apply(hero,bags.ToArray(),camera,Generated);
        if(fullCity)BraxCityBuild.AttachCity(runtime,integrated,renovation);
        if(renovation)
        {
            // Exact accepted mural/ring/annex authoring; one original player.
            BraxGymReferenceRebuild.Apply(gym,solids.transform,runtime,Generated);
            BraxGymReferenceEquipment.Apply(gym,runtime,Generated);
            BraxNovaMusicBuild.Attach(runtime);
        }
        string scenePath=Generated+(fullCity?"/BraxhornCity.unity":"/BraxhornGym.unity");if(!EditorSceneManager.SaveScene(scene,scenePath))throw new Exception("Scene save failed");
        string sourceCommit=runtime.buildId;
        // Estate review boots directly into the EXISTING city estate scene with the
        // SAME BraxGymPlayer, original rig, material, clips and mobile controls.
        // City remains in the build for future travel; no duplicate player code.
        // iPhone ESTATE preview only: exclude gym and city scenes from the WebGL payload.
        // The estate scene itself still uses the UNMODIFIED city's BraxGymPlayer,
        // BraxOriginal.glb, BraxSourceORM.mat and original animations.
        // This branch is never merged into the connected city build.
        // Built scenes are the same original models and the same BraxGymPlayer.
        // R2 remains estate-only; unified TEST has BOTH real scenes and a validated registry.
        string[] scenePaths=fullCity
            ? (integrated
                ? BraxUnityDistrictRegistry.Resolve(scenePath,BraxCityBuild.CreateEstate(sourceCommit))
                : new[]{BraxCityBuild.CreateEstate(sourceCommit)})
            : new[]{scenePath};
        EditorBuildSettings.scenes=scenePaths.Select(p=>new EditorBuildSettingsScene(p,true)).ToArray();AssetDatabase.SaveAssets();
        var output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../build/WebGL"));Directory.CreateDirectory(output);
        var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=scenePaths,target=BuildTarget.WebGL,locationPathName=output,options=BuildOptions.None});
        if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Playable Unity build failed "+result.summary.result);
        var report=new Report{unity=Application.unityVersion,commit=sourceCommit,gymMeshes=renderers.Length,originalClips=imported.Length,colliders=n,matteSurfaces=tuned,animationNames=mappings.Values.ToArray()};
        File.WriteAllText(Path.Combine(output,"playable.json"),JsonUtility.ToJson(report,true));
        if(fullCity)BraxCityBuild.WriteReport(output,sourceCommit);
        if(integrated)BraxUnityDistrictRegistry.WriteBuildReceipt(output,sourceCommit,scenePaths);
        if(renovation)BraxCityRebuild.WriteReport(output);
        Debug.Log("BRAXHORN_PLAYABLE_BUILD_SUCCESS "+JsonUtility.ToJson(report));
    }
}
