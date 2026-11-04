using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// Same region authoring for editable exports and the separately installed exploration APK.
public static class BraxChampionsBuild
{
    [Serializable] sealed class Report
    {
        public string status="STARTED",sourceCommit,unity;
        public bool visualAccepted=false,deviceTested=false,trafficEnabled=false,sceneReloadPassed=false;
        public int rendererCount,walkSamples,buildingExteriors,arenaSeats;
        public bool foundationPassedBeforeBuildings=false;public string sculptedBull="SOURCE_MODEL_REQUIRED";
        public string patrykSourceCommit=BraxChampionsExplorer.SourceCommit;
        public int patrykClips;
        public bool explorationSceneReloadPassed=false,playerRuntimeTested=false;
        public BraxChampionsInfrastructure.Counts counts;
        public List<string> checks=new List<string>();
        public List<string> failures=new List<string>();
    }
    public static void Build() => BuildInternal(false);
    public static void BuildAndroid() => BuildInternal(true);

    // Called only by the optional, manual review step AFTER APK delivery.
    public static void BuildVisualReview()
    {
        if(!File.Exists(BraxChampionsExplorer.ScenePath))throw new IOException("Build the exploration scene first");
        EditorSceneManager.OpenScene(BraxChampionsExplorer.ScenePath,OpenSceneMode.Single);
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone,ScriptingImplementation.Mono2x);
        string output=Path.GetFullPath("../build/VisualReview/Mistrzowie.x86_64");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{BraxChampionsExplorer.ScenePath},
            target=BuildTarget.StandaloneLinux64,locationPathName=output,options=BuildOptions.None});
        if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Visual review player build failed");
    }

    static void BuildInternal(bool apk)
    {
        string reports=Path.GetFullPath("../mistrzowie-evidence");Directory.CreateDirectory(reports);
        // The manual Actions launcher can live on main; record the checked-out game.
        var report=new Report{unity=Application.unityVersion,sourceCommit=File.Exists("mistrzowie.source-commit")
            ?File.ReadAllText("mistrzowie.source-commit").Trim():Environment.GetEnvironmentVariable("GITHUB_SHA")??"local"};
        try
        {
            // Same linear lighting as the main game's Android build.
            PlayerSettings.colorSpace=ColorSpace.Linear;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            BraxChampionsFoundation.Generate();
            var scene=EditorSceneManager.OpenScene(BraxChampionsFoundation.ScenePath,OpenSceneMode.Single);
            var d=BraxChampionsInfrastructure.Generate();report.counts=d.counts;
            Physics.SyncTransforms();
            CheckStations(BraxChampionsInfrastructure.Stations,report);
            CheckAssets(scene);report.foundationPassedBeforeBuildings=true;
            report.checks.Add("Foundation and all three complete metro stations validated before building generation");
            report.buildingExteriors=BraxChampionsBuildings.Generate();Physics.SyncTransforms();
            report.arenaSeats=BraxChampionsArena.SeatCount;
            CheckExteriorAccess();CheckAssets(scene);AssetDatabase.SaveAssets();
            if(!EditorSceneManager.SaveScene(scene))throw new IOException("Scene save failed");
            // Reopen the actual serialized scene; live editor objects are not proof of saved references.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            scene=EditorSceneManager.OpenScene(BraxChampionsFoundation.ScenePath,OpenSceneMode.Single);
            Physics.SyncTransforms();
            var stations=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Transform>(true)).Where(t=>t.name.EndsWith("| reused city metro")).ToArray();
            if(stations.Length!=3)throw new InvalidOperationException("Three saved metro stations required");
            CheckStations(stations,report);
            CheckExteriorAccess();report.rendererCount=CheckAssets(scene);report.sceneReloadPassed=true;
            report.checks.Add("Serialized scene reopened: meshes, scripts, colliders and materials present");
            BraxChampionsExplorer.Generate(report.sourceCommit);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            scene=EditorSceneManager.OpenScene(BraxChampionsExplorer.ScenePath,OpenSceneMode.Single);
            Physics.SyncTransforms();
            report.patrykClips=BraxChampionsExplorer.Validate(scene);
            CheckAssets(scene);report.explorationSceneReloadPassed=true;
            report.checks.Add("Exploration scene reopened: estate Patryk, 4K textures, saved animations, camera and clear spawn; PlayMode pending");
            if(apk)
            {
                BuildExplorationApk();
                report.status="ANDROID_APK_COMPILED_DEVICE_AND_VISUAL_REVIEW_PENDING";
                report.checks.Add("Signed ARM64 exploration APK compiled; delivery verification runs next in CI");
                return; // APK delivery precedes any optional export/cache work.
            }
            string payload=Path.GetFullPath("../mistrzowie-payload");Directory.CreateDirectory(payload+"/Android");
            string graph="Assets/Source/Mistrzowie/Transit.json";
            // Split scene and source vehicle assets into separate bundles; gameplay traffic is deferred.
            var builds=new[]{new AssetBundleBuild{assetBundleName="mistrzowie_foundation",assetNames=new[]{BraxChampionsFoundation.ScenePath,BraxChampionsExplorer.ScenePath}},
                new AssetBundleBuild{assetBundleName="mistrzowie_transit_sources",assetNames=new[]{"Assets/Source/Claude/Metro/metro_train.glb",graph}}};
            var manifest=BuildPipeline.BuildAssetBundles(payload+"/Android",builds,BuildAssetBundleOptions.ChunkBasedCompression|BuildAssetBundleOptions.StrictMode,BuildTarget.Android);
            if(!manifest)throw new InvalidOperationException("Android region bundles failed");
            report.checks.Add("Unity Android scene and transit bundles built");
            AssetDatabase.ExportPackage(new[]{BraxChampionsFoundation.ScenePath,BraxChampionsExplorer.ScenePath,"Assets/Source/Claude/Metro/metro_train.glb",graph,"Assets/Source/Mistrzowie/SignFont-LICENSE.txt"},payload+"/Mistrzowie_Foundation.unitypackage",ExportPackageOptions.IncludeDependencies);
            File.Copy("Assets/Source/Claude/Metro/CREDITS.md",payload+"/METRO_CREDITS.md",true);
            report.status="UNITY_SCENE_AND_ANDROID_BUNDLES_PASS_VISUAL_REVIEW_PENDING";
        }
        catch(Exception e){report.status="FAILED: "+e.Message;Debug.LogException(e);throw;}
        finally {File.WriteAllText(reports+"/unity-validation.json",JsonUtility.ToJson(report,true));Debug.Log("BRAX_MISTRZOWIE_UNITY "+JsonUtility.ToJson(report));}
    }

    static void BuildExplorationApk()
    {
        if(!File.Exists("braxhorn.keystore")||!File.Exists("braxhorn.keystore.pass"))
            throw new InvalidOperationException("Brak stałego klucza podpisu APK");
        string password=File.ReadAllText("braxhorn.keystore.pass").Trim();
        if(password.Length==0||!File.Exists("braxhorn.versioncode")||
            !int.TryParse(File.ReadAllText("braxhorn.versioncode").Trim(),out int version)||version<=0)
            throw new InvalidOperationException("Brak konfiguracji podpisu/wersji APK");
        PlayerSettings.companyName="Braxhorn"; PlayerSettings.productName="Braxhorn Miasto Mistrzow";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android,"com.braxhorn.mistrzowie");
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android,ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
        PlayerSettings.SetIl2CppCodeGeneration(UnityEditor.Build.NamedBuildTarget.Android,UnityEditor.Build.Il2CppCodeGeneration.OptimizeSize);
        PlayerSettings.SetIl2CppCompilerConfiguration(UnityEditor.Build.NamedBuildTarget.Android,Il2CppCompilerConfiguration.Release);
        PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel24;
        PlayerSettings.Android.useAPKExpansionFiles=false;
        PlayerSettings.Android.useCustomKeystore=true;
        PlayerSettings.Android.keystoreName=Path.GetFullPath("braxhorn.keystore");
        PlayerSettings.Android.keystorePass=password; PlayerSettings.Android.keyaliasName="braxhorn";
        PlayerSettings.Android.keyaliasPass=password; PlayerSettings.Android.bundleVersionCode=version;
        PlayerSettings.bundleVersion="1.0."+version;
        PlayerSettings.defaultInterfaceOrientation=UIOrientation.LandscapeLeft;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{GraphicsDeviceType.OpenGLES3});
        EditorUserBuildSettings.buildAppBundle=false;
        EditorUserBuildSettings.androidBuildSubtarget=MobileTextureSubtarget.ETC2;

        // Reuse the game's lightweight loader, then only the Champions exploration
        // scene. Its existing BraxTouchControls bootstrap supplies Android input.
        var start=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var camera=new GameObject("Kamera ekranu startowego").AddComponent<Camera>();
        camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=Color.black;
        camera.cullingMask=0; camera.allowHDR=false; camera.allowMSAA=false;
        new GameObject("BRAXHORN ekran startowy").AddComponent<BraxStart>();
        string startPath="Assets/PlayableGenerated/Mistrzowie/Mistrzowie_Start.unity";
        if(!EditorSceneManager.SaveScene(start,startPath))throw new IOException("Nie zapisano ekranu startowego");
        var scenes=new[]{startPath,BraxChampionsExplorer.ScenePath};
        EditorBuildSettings.scenes=scenes.Select(p=>new EditorBuildSettingsScene(p,true)).ToArray();
        string output=Path.GetFullPath("../build/Android"); Directory.CreateDirectory(output);
        AssetDatabase.SaveAssets();
        // Apply to every selectable quality tier of this separate APK, not to other region builders.
        int previousQuality=QualitySettings.GetQualityLevel();
        for(int i=0;i<QualitySettings.names.Length;i++)
        {
            QualitySettings.SetQualityLevel(i,false);
            QualitySettings.streamingMipmapsActive=true;
            QualitySettings.streamingMipmapsMemoryBudget=512;
            QualitySettings.streamingMipmapsMaxLevelReduction=1; // 4K source: lowest resident level 2K.
            QualitySettings.streamingMipmapsAddAllCameras=true;
        }
        QualitySettings.SetQualityLevel(previousQuality,false);
        var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=scenes,target=BuildTarget.Android,
            locationPathName=Path.Combine(output,"braxhorn-mistrzowie.apk"),options=BuildOptions.None});
        if(result.summary.result!=BuildResult.Succeeded)
            throw new InvalidOperationException("APK Miasta Mistrzów: "+result.summary.result+"; błędów="+result.summary.totalErrors);
        // Like the existing city builder: mark only a completed BuildPlayer output.
        // CI can rescue it even if Unity shutdown or a later report fails.
        File.WriteAllText(Path.Combine(output,"braxhorn-mistrzowie.gotowe"),version.ToString());
        Debug.Log("BRAX_MISTRZOWIE_APK id=com.braxhorn.mistrzowie versionCode="+version+" bytes="+result.summary.totalSize);
    }
    static void CheckMetro(Transform station)
    {
        if((station.localScale-Vector3.one).sqrMagnitude>.000001f)throw new InvalidOperationException("Metro source was scaled twice: "+station.name);
        if(!station.Find("Existing entrance")||!station.Find("Existing station and open tunnels"))throw new InvalidOperationException("Metro entrance/underground missing");
        var gates=station.GetComponentsInChildren<BraxDrzwiPrzesuwne>(true);
        if(gates.Length!=7||gates.Any(g=>g.skrzydla==null||g.skrzydla.Length!=4||g.skrzydla.Any(s=>!s)
            ||g.przesuniecie==null||g.przesuniecie.Length!=4||g.blokady==null||g.blokady.Length==0||g.blokady.Any(c=>!c)))
            throw new InvalidOperationException("Seven complete ticket gates required: "+station.name);
        var steps=station.GetComponentsInChildren<BraxSchodyRuchome>(true);
        var drives=station.GetComponentsInChildren<BraxSchodyRuchomeJazda>(true);
        if(steps.Length!=2||drives.Length!=2||steps.Any(x=>!x.enabled)||drives.Any(x=>!x.enabled))throw new InvalidOperationException("Two working escalator drives required: "+station.name);
        foreach(var d in drives)
        {
            var upper=station.InverseTransformPoint(d.gora);var lower=station.InverseTransformPoint(d.dol);
            if(upper.y<=lower.y||upper.x<40||upper.x>90||lower.x<30||lower.x>90)throw new InvalidOperationException("Escalator world endpoints were not rebased: "+station.name);
        }
        if(station.name.StartsWith("M1 "))
        {
            var train=station.Find("Existing station and open tunnels/Parked metro train | original two-car city model");
            if(!train||train.Cast<Transform>().Count(t=>t.name.Contains("(model metro_train,"))!=2)throw new InvalidOperationException("Original two-car train missing at M1");
            foreach(var car in train.Cast<Transform>().Where(t=>t.name.Contains("(model metro_train,")))
            {
                var renderers=car.GetComponentsInChildren<Renderer>(true);
                if(renderers.Length==0)throw new InvalidOperationException("Empty train: "+car.name);
                var bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
                var size=bounds.size;
                if(size.y>8f||size.x<12f||size.z>5f)
                    throw new InvalidOperationException("Train orientation/clearance invalid: "+car.name+" size="+size);
            }
        }
    }
    static void CheckStations(IEnumerable<Transform> stations,Report report)
    {
        int before=report.failures.Count;
        foreach(var station in stations)
        {
            // Each station and each kind of check is independent. Preserve all
            // findings in one failed run instead of stopping at the first station.
            try { CheckMetro(station); }
            catch(Exception e) { report.failures.Add(station.name+" structure: "+e.Message); }
            try { report.walkSamples+=CheckWalk(station);report.checks.Add(station.name+": surface-to-platform path clear"); }
            catch(Exception e) { report.failures.Add(station.name+" walk: "+e.Message); }
        }
        if(report.failures.Count>before)throw new InvalidOperationException(string.Join("\n",report.failures.Skip(before)));
    }
    static void CheckExteriorAccess()
    {
        float k=BraxClaudeOsiedle.K;
        // Actual ground-to-door routes, including the arena approach and the hotel street crossing.
        foreach(var route in new[]{new[]{new Vector3(600,.16f,667),new Vector3(600,.16f,740.7f)},
            new[]{new Vector3(783,.16f,815),new Vector3(828,.16f,815)},
            new[]{new Vector3(828,.16f,815),new Vector3(832.5f,.16f,811)},
            // The centre line crosses the 6.4m rear of the seating stairs.
            // Enter through the existing door, use the clear side of the central
            // aisle, then return to the octagon approach. Keep all collision checks.
            new[]{new Vector3(600,.16f,740.7f),new Vector3(600,.16f,766.5f)},
            new[]{new Vector3(600,.16f,766.5f),new Vector3(602,.16f,766.5f)},
            new[]{new Vector3(602,.16f,766.5f),new Vector3(602,.16f,787)},
            new[]{new Vector3(602,.16f,787),new Vector3(600,.16f,787)},
            new[]{new Vector3(600,.16f,787),new Vector3(600,.16f,800)}})
        {
            var a=route[0]*k;var b=route[1]*k;int n=Mathf.CeilToInt(Vector3.Distance(a,b)/.25f);
            float previous=a.y;
            for(int i=0;i<=n;i++)
            {
                var p=Vector3.Lerp(a,b,i/(float)n);p.y=previous;
                if(!Physics.Raycast(p+Vector3.up,Vector3.down,out var hit,2f,~0,QueryTriggerInteraction.Ignore)||Mathf.Abs(hit.point.y-previous)>.3f)
                    throw new InvalidOperationException("Exterior approach has a gap or excessive step at "+p);
                previous=hit.point.y;p.y=previous;
                if(Physics.CheckCapsule(p+Vector3.up*.73f,p+Vector3.up*2.02f,.35f,~0,QueryTriggerInteraction.Ignore))
                    throw new InvalidOperationException("Exterior approach obstructed at "+p);
            }
        }
    }
    static int CheckAssets(Scene scene)
    {
        int renderers=0;
        foreach(var root in scene.GetRootGameObjects())
        {
            foreach(var t in root.GetComponentsInChildren<Transform>(true))
                if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)throw new InvalidOperationException("Missing script: "+t.name);
            foreach(var r in root.GetComponentsInChildren<Renderer>(true))
            {
                renderers++;
                foreach(var m in r.sharedMaterials)
                    if(!m||!m.shader||ShaderUtil.ShaderHasError(m.shader))throw new InvalidOperationException("Missing/invalid material or shader: "+r.name);
            }
            foreach(var grass in root.GetComponentsInChildren<BraxClaudeTrawa>(true))
                if(!grass.dane||!grass.siatka||!grass.material||!grass.material.enableInstancing||!grass.material.shader||
                    ShaderUtil.ShaderHasError(grass.material.shader)||grass.dane.y.Length!=grass.dane.kepy.Length)
                    throw new InvalidOperationException("Invalid saved grass instancing assets: "+grass.name);
            foreach(var f in root.GetComponentsInChildren<MeshFilter>(true))
                if(!f.sharedMesh||f.sharedMesh.vertexCount==0||string.IsNullOrEmpty(AssetDatabase.GetAssetPath(f.sharedMesh)))throw new InvalidOperationException("Unsaved/empty mesh: "+f.name);
            foreach(var c in root.GetComponentsInChildren<MeshCollider>(true))if(!c.sharedMesh)throw new InvalidOperationException("Empty collision mesh: "+c.name);
        }
        if(renderers<100)throw new InvalidOperationException("Infrastructure scene is incomplete");
        return renderers;
    }
    static int CheckWalk(Transform station)
    {
        var local=BraxClaudeMetro.ChampionsWalkingRoute();
        var gate=station.GetComponentsInChildren<BraxDrzwiPrzesuwne>(true)
            .Where(g=>g.blokady!=null&&g.blokady.Length>0&&g.blokady[0])
            .OrderBy(g=>Mathf.Abs(station.InverseTransformPoint(g.blokady[0].bounds.center).z-28.45f)).FirstOrDefault();
        if(!gate)throw new InvalidOperationException(station.name+" has no original ticket gates");
        // The imported model origin is not necessarily the passage centre.
        float gateZ=station.InverseTransformPoint(gate.blokady[0].bounds.center).z;
        local[5].z=gateZ;local[6].z=gateZ;
        var route=local.Select(station.TransformPoint).ToArray();
        int count=0;var problems=new List<string>();
        const int mask=~((1<<8)|(1<<2)|(1<<29)|(1<<31));
        for(int i=0;i<route.Length-1;i++)
        {
            var a=route[i];var b=route[i+1];var delta=b-a;delta.y=0;float len=delta.magnitude;
            if(len<.01f)continue;
            int steps=Mathf.CeilToInt(len/.25f);float y=a.y;
            var obstacles=new HashSet<Collider>();
            for(int k=0;k<=steps;k++)
            {
                var q=Vector3.Lerp(a,b,k/(float)steps);q.y=y;
                if(!Physics.Raycast(q+Vector3.up*.8f,Vector3.down,out var hit,2.0f,mask,QueryTriggerInteraction.Ignore))
                { problems.Add("segment "+i+" no walk floor at "+q);break; }
                if(hit.point.y>y+.45f||hit.point.y<y-.55f)
                { problems.Add("segment "+i+" walk discontinuity at "+hit.point+" / "+ColliderPath(hit.collider));break; }
                y=hit.point.y;q.y=y;
                foreach(var c in Physics.OverlapCapsule(q+Vector3.up*.73f,q+Vector3.up*2.02f,.35f,mask,QueryTriggerInteraction.Ignore))
                {
                    var door=c.GetComponentInParent<BraxDrzwiPrzesuwne>();
                    if(door&&door.blokady!=null&&door.blokady.Contains(c))continue; // only the opening leaf blocker; keep fixed housing collisions
                    if(obstacles.Add(c))problems.Add("segment "+i+" blocked walkway at "+q+" local="+station.InverseTransformPoint(q)
                        +" / "+ColliderPath(c)+" bounds="+c.bounds);
                }
                count++;
            }
        }
        if(problems.Count>0)throw new InvalidOperationException(string.Join("\n",problems));
        return count;
    }
    static string ColliderPath(Collider c)
    {
        var names=new List<string>();for(var t=c.transform;t;t=t.parent)names.Add(t.name);
        names.Reverse();return string.Join("/",names);
    }
}

