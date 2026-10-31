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
    // Aplikacja Android (APK) z tej samej sceny: przeglądarka na telefonie nie dźwiga 750 MB danych.
    // Sterowanie dotykowe w Unity (BraxTouchControls) zamiast warstwy HTML.
    public static void BuildRenovationAndroid(){BuildInternal(true,true,true,BuildTarget.Android);}
    // Tekstury elewacji/placów/billboardów → 1K dla danej platformy (platform override, format bez zmian, kompresja normalna).
    static void CapFacadeTextures(BuildTarget target)
    {
        string platform=target==BuildTarget.Android?"Android":"WebGL";
        string[] folders={"Assets/Source/City/Images","Assets/PlayableGenerated/CityRebuild/Textures","Assets/Source/CityRebuild/textures","Assets/Source/Claude/PolyHaven/textures","Assets/Source/HarborBeach/Textures","Assets/CityRebuild/Brand"};
        folders=folders.Where(AssetDatabase.IsValidFolder).ToArray();
        if(folders.Length==0)return;
        int changed=0,kept=0;
        foreach(var guid in AssetDatabase.FindAssets("t:Texture2D",folders))
        {
            string path=AssetDatabase.GUIDToAssetPath(guid);string lower=path.ToLowerInvariant();
            if(lower.Contains("sand")||lower.Contains("tatami")||lower.Contains("leather")||lower.Contains("mural")||lower.Contains("terry")||lower.Contains("linen")||lower.Contains("gingham")){kept++;continue;}
            var imp=AssetImporter.GetAtPath(path) as TextureImporter;if(!imp)continue;
            var ps=imp.GetPlatformTextureSettings(platform);
            if(ps.overridden&&ps.maxTextureSize<=1024)continue;
            ps.name=platform;ps.overridden=true;ps.maxTextureSize=1024;
            if(ps.compressionQuality>50)ps.compressionQuality=50;
            imp.SetPlatformTextureSettings(ps);imp.SaveAndReimport();changed++;
        }
        Debug.Log("BRAXHORN_TEXTURE_CAP "+platform+": elewacje do 1K zmienione="+changed+", zostawione 2K (z bliska)="+kept);
    }

    // Raport rozmiaru danych (Android: AAR > 4 GB, WebGL: webgl.data > 2 GB). Co i ile waży, po typie i po pliku.
    static void SizeReport(UnityEditor.Build.Reporting.BuildReport result)
    {
        try
        {
            var byType=new Dictionary<string,long>();var byAsset=new Dictionary<string,long>();long total=0;
            foreach(var pack in result.packedAssets)foreach(var c in pack.contents)
            {
                string t=c.type!=null?c.type.Name:"?";string a=string.IsNullOrEmpty(c.sourceAssetPath)?"(scena/wbudowane)":c.sourceAssetPath;
                long sz=(long)c.packedSize;total+=sz;
                byType[t]=(byType.ContainsKey(t)?byType[t]:0)+sz;byAsset[a]=(byAsset.ContainsKey(a)?byAsset[a]:0)+sz;
            }
            var sb=new System.Text.StringBuilder();
            sb.Append("BRAXHORN_BUILD_SIZE total=").Append(total/1048576).Append(" MB, result=").Append(result.summary.result).Append('\n');
            foreach(var kv in byType.OrderByDescending(k=>k.Value).Take(12))sb.Append("  TYPE ").Append(kv.Value/1048576).Append(" MB  ").Append(kv.Key).Append('\n');
            foreach(var kv in byAsset.OrderByDescending(k=>k.Value).Take(60))sb.Append("  ASSET ").Append(kv.Value/1048576).Append(" MB  ").Append(kv.Key).Append('\n');
            Debug.Log(sb.ToString());
            File.WriteAllText(Path.Combine(Generated,"build-size.txt"),sb.ToString());
        }
        catch(Exception e){Debug.LogWarning("BRAXHORN_BUILD_SIZE raport nieudany: "+e.Message);}
    }

    private static void BuildInternal(bool fullCity,bool integrated,bool renovation=false,BuildTarget target=BuildTarget.WebGL)
    {
        if(fullCity&&target==BuildTarget.WebGL)EditorUserBuildSettings.webGLBuildSubtarget=WebGLTextureSubtarget.ETC2;
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
            // Siłownia jako wizytówka (Claude, region R1): tatami judo i dodatki w wolnym miejscu hali.
            BraxRegionGym.Apply(gym,runtime);
            // Muzyka w siłowni (właściciel 31.10, 18:48: „to daj do siłki”): gra tylko w hali (prostopadłościan siatek hali
            // przed ścianą z drzwiami z=11.4), cichnie 3 m za drzwiami. Pętla 3:57 (Assets/Claude/Audio, BraxClaudeMuzyka).
            BraxClaudeOsiedle.StreamAudio("Assets/Claude/Audio/silownia_muzyka.ogg");   // strumieniowo, nie 40 MB w RAM
            var gymClip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Claude/Audio/silownia_muzyka.ogg");
            if(gymClip)
            {
                var hall=new Bounds();bool any=false;
                foreach(var r in gym.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var rb=r.bounds;if(rb.size.x>60f||rb.size.z>60f||rb.center.z>11.4f)continue;
                    if(!any){hall=rb;any=true;}else hall.Encapsulate(rb);
                }
                if(any){var mg=new GameObject("Muzyka siłowni");var mz=mg.AddComponent<BraxClaudeMuzyka>();mz.utwor=gymClip;mz.wnetrze=hall;mz.glosnosc=.4f;mz.wygaszenie=3f;Debug.Log("BRAXHORN_SILOWNIA_MUZYKA "+hall);}
            }
            else Debug.LogWarning("DO POPRAWKI: brak Assets/Claude/Audio/silownia_muzyka.ogg");
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
        // BRAX_DISTRICTS=city: paczka samego regionu R1 (miasto bez sceny osiedla) — budżet mobilny, osiedle idzie osobną paczką.
        bool cityOnly=renovation&&BraxCityBuild.CityOnly();
        if(cityOnly)Debug.Log("BRAXHORN_DISTRICTS city-only: scena osiedla pominięta w tej paczce");
        string[] scenePaths=fullCity
            ? (cityOnly
                ? new[]{scenePath}
                : integrated
                    ? BraxUnityDistrictRegistry.Resolve(scenePath,BraxCityBuild.CreateEstate(sourceCommit))
                    : new[]{BraxCityBuild.CreateEstate(sourceCommit)})
            : new[]{scenePath};
        EditorBuildSettings.scenes=scenePaths.Select(p=>new EditorBuildSettingsScene(p,true)).ToArray();AssetDatabase.SaveAssets();
        string output,location;
        if(target==BuildTarget.Android)
        {
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android,"com.braxhorn.city");
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android,ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
            // Build 62be06c: kompilacja IL2CPP (clang arm64) zabita przez OOM na 16 GB (exit 137).
            // Mniej C++ (OptimizeSize) = mniej pamięci i czasu; dla podglądu na telefonie wystarczy.
            PlayerSettings.SetIl2CppCodeGeneration(UnityEditor.Build.NamedBuildTarget.Android,UnityEditor.Build.Il2CppCodeGeneration.OptimizeSize);
            PlayerSettings.SetIl2CppCompilerConfiguration(UnityEditor.Build.NamedBuildTarget.Android,Il2CppCompilerConfiguration.Release);
            PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.useAPKExpansionFiles=false;
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.LandscapeLeft;
            PlayerSettings.productName="Braxhorn City";
            EditorUserBuildSettings.buildAppBundle=false;
            // Build 5 (właściciel 21:34–21:36: gra zamyka się na starcie; raport d260250: 1.1 GB tekstur + 0.6 GB siatek w pamięci naraz).
            // Audyt 31.10: globalny limit tekstur i tak był zerowany niżej (i reimportowałby ~1400 tekstur), a tekstury wewnątrz .glb
            // (jacht, skutery, dźwig, pociąg) są nieskompresowane — na telefonie tnie je limit mip w BraxGrafika (ustawiany przed
            // pierwszą sceną). Kanały siatek bez zmian (stripUnusedMeshComponents niesprawdzone na telefonie — trawa z kodu).
            EditorUserBuildSettings.androidBuildSubtarget=MobileTextureSubtarget.ETC2;   // build 5: z powrotem ETC2 jak w działającym APK 5739547 (d260250 z ASTC zamykał się na starcie u właściciela); ASTC wróci po teście na telefonie
            output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../build/Android"));Directory.CreateDirectory(output);
            location=Path.Combine(output,"braxhorn-city.apk");
        }
        else
        {
            output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../build/WebGL"));Directory.CreateDirectory(output);
            location=output;
        }
        // Budżet tekstur (decyzja właściciela 31.10): 2K tylko z bliska (rośliny, piasek, worki, tatami, skóra, mural),
        // elewacje, place i billboardy oglądane z daleka do 1K. Globalny override (overrideMaxTextureSize) reimportował
        // WSZYSTKIE ~1400 tekstur i build zdjęć przekroczył limit 2 h — dlatego per plik, tylko ~190 tekstur.
        EditorUserBuildSettings.overrideMaxTextureSize=0;
        EditorUserBuildSettings.overrideTextureCompression=UnityEditor.Build.OverrideTextureCompression.NoOverride;
        CapFacadeTextures(target);
        var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=scenePaths,target=target,locationPathName=location,options=BuildOptions.None});
        SizeReport(result);
        if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Playable Unity build failed "+result.summary.result);
        var report=new Report{unity=Application.unityVersion,commit=sourceCommit,gymMeshes=renderers.Length,originalClips=imported.Length,colliders=n,matteSurfaces=tuned,animationNames=mappings.Values.ToArray()};
        File.WriteAllText(Path.Combine(output,"playable.json"),JsonUtility.ToJson(report,true));
        if(fullCity)BraxCityBuild.WriteReport(output,sourceCommit);
        if(integrated&&!cityOnly)BraxUnityDistrictRegistry.WriteBuildReceipt(output,sourceCommit,scenePaths);
        if(renovation)BraxCityRebuild.WriteReport(output);
        Debug.Log("BRAXHORN_PLAYABLE_BUILD_SUCCESS "+JsonUtility.ToJson(report));
    }
}
