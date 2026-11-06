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
        public bool walkChecksSkipped=false;
        public bool qualityGatesAdvisory=BraxBuildQuality.Advisory;
        public int rendererCount,walkSamples,buildingExteriors,arenaSeats,arenaVipSeats,sourceImportErrors;
        public bool ownerVip2kSourcePresent=false;
        public bool foundationPassedBeforeBuildings=false;public string sculptedBull="SOURCE_MODEL_REQUIRED";
        public string playerSourceCommit=BraxChampionsExplorer.SourceCommit;
        public int playerClips;
        public string playerCharacter="Brax";
        public bool explorationSceneReloadPassed=false,playerRuntimeTested=false;
        public BraxChampionsInfrastructure.Counts counts;
        public List<string> checks=new List<string>();
        public List<string> failures=new List<string>();
    }
    [Serializable] sealed class ImportReport
    {
        public string status="SOURCE_IMPORT_DIAGNOSTICS_NOT_SCENE_ACCEPTANCE";
        public List<ImportIssue> issues=new List<ImportIssue>();
    }
    [Serializable] sealed class ImportIssue
    {
        public string path,type,message;public int code;
    }
    public static void Build() => BuildInternal(false);
    public static void BuildAndroid() => BuildInternal(true);
    // Explicit owner-requested diagnostic build; scene colliders remain unchanged.
    public static void BuildAndroidWithoutWalkChecks() => BuildInternal(true,false,true);
    // Explicit manual validation: generate/reopen/check scenes without player or bundles.
    public static void ValidateScenes() => BuildInternal(false,true);

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

    static void BuildInternal(bool apk,bool validationOnly=false,bool skipWalkChecks=false)
    {
        string reports=Path.GetFullPath("../mistrzowie-evidence");Directory.CreateDirectory(reports);
        // The manual Actions launcher can live on main; record the checked-out game.
        var report=new Report{walkChecksSkipped=skipWalkChecks,unity=Application.unityVersion,sourceCommit=File.Exists("mistrzowie.source-commit")
            ?File.ReadAllText("mistrzowie.source-commit").Trim():Environment.GetEnvironmentVariable("GITHUB_SHA")??"local"};
        try
        {
            if(skipWalkChecks)
                report.checks.Add("OWNER_REQUESTED_DIAGNOSTIC: metro and exterior walking checks SKIPPED; physical colliders unchanged; traversability NOT VALIDATED");
            report.sourceImportErrors=RecordSourceImportDiagnostics(reports);
            // Fail fast on 21 original casino maps before generating the world.
            BraxChampionsCasinoPbr2K.VerifyOriginalSourceMaps();
            BraxChampionsCasinoT11.VerifyOriginalSigns();
            BraxChampionsSourceReadiness.VerifyBeforeScene();
            report.checks.Add("Original car, 2 vegetation GLBs, Brax rig, 6 clips, 3 Mixamo FBXs and owner signage verified");
            report.checks.Add("All 21 Poly Haven 2K source images verified before expensive scene generation");
            // Same linear lighting as the main game's Android build.
            PlayerSettings.colorSpace=ColorSpace.Linear;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            BraxChampionsFoundation.Generate();
            var scene=EditorSceneManager.OpenScene(BraxChampionsFoundation.ScenePath,OpenSceneMode.Single);
            var d=BraxChampionsInfrastructure.Generate();report.counts=d.counts;
            Physics.SyncTransforms();
            CheckStations(BraxChampionsInfrastructure.Stations,report);
            report.foundationPassedBeforeBuildings=CheckAssets(scene)>=0;
            report.checks.Add("Foundation and metro structure checked; walking validation status is recorded separately");
            report.buildingExteriors=BraxChampionsBuildings.Generate();Physics.SyncTransforms();
            report.arenaSeats=BraxChampionsArena.SeatCount;
            report.arenaVipSeats=BraxChampionsVIPSeats.SeatCount;
            report.ownerVip2kSourcePresent=BraxChampionsVIPSeats.HasSource;
            if(!report.ownerVip2kSourcePresent)
                BraxBuildQuality.Issue(new InvalidOperationException(
                    "The owner-supplied premium VIP 2K GLB must be imported before final arena acceptance: "+BraxChampionsVIPSeats.SourcePath));
            if(!skipWalkChecks)CheckExteriorAccess();CheckAssets(scene);AssetDatabase.SaveAssets();
            if(!EditorSceneManager.SaveScene(scene))throw new IOException("Scene save failed");
            // Reopen the actual serialized scene; live editor objects are not proof of saved references.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            scene=EditorSceneManager.OpenScene(BraxChampionsFoundation.ScenePath,OpenSceneMode.Single);
            Physics.SyncTransforms();
            var stations=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Transform>(true)).Where(t=>t.name.EndsWith("| reused city metro")).ToArray();
            if(stations.Length!=3)BraxBuildQuality.Issue(new InvalidOperationException("Three saved metro stations required"));
            CheckStations(stations,report);
            if(!skipWalkChecks)CheckExteriorAccess();report.rendererCount=CheckAssets(scene);report.sceneReloadPassed=true;
            report.checks.Add("Serialized scene reopened; consult quality-review.json for inspection warnings");
            BraxChampionsExplorer.Generate(report.sourceCommit);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            scene=EditorSceneManager.OpenScene(BraxChampionsExplorer.ScenePath,OpenSceneMode.Single);
            Physics.SyncTransforms();
            report.playerClips=BraxChampionsExplorer.Validate(scene);
            CheckAssets(scene);report.explorationSceneReloadPassed=true;
            report.checks.Add("Exploration scene reopened; player quality and traversability require manual device review");
            if(validationOnly)
            {
                report.status="UNITY_SCENES_GENERATED_APK_NOT_BUILT_DEVICE_REVIEW_PENDING";
                return;
            }
            if(apk)
            {
                BuildExplorationApk();
                report.status="ANDROID_APK_COMPILED_DEVICE_AND_VISUAL_REVIEW_PENDING";
                report.checks.Add("ARM64 exploration APK compiled; repository signing certificate verification is still required in CI");
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
        finally {if(BraxBuildQuality.Advisory)report.status+="_QUALITY_ADVISORY_MANUAL_REVIEW_REQUIRED";if(skipWalkChecks)report.status+="_WALK_CHECKS_SKIPPED";File.WriteAllText(reports+"/unity-validation.json",JsonUtility.ToJson(report,true));Debug.Log("BRAX_MISTRZOWIE_UNITY "+JsonUtility.ToJson(report));}
    }

    static int RecordSourceImportDiagnostics(string reports)
    {
        // glTFast 6.20 stores the useful messages in the importer's serialized
        // reportItems. Its normal Console message only says "see inspector".
        // Read the same data as its Inspector; do not reimport or alter models.
        var result=new ImportReport();int errors=0;
        foreach(var path in AssetDatabase.GetAllAssetPaths().Where(p=>
            // A1/T11 report only: main City and its HarborBeach port are a separate game zone.
            (p.StartsWith("Assets/Source/Mistrzowie/",StringComparison.Ordinal) ||
             p=="Assets/Source/Claude/Metro/metro_train.glb") &&
            (p.EndsWith(".glb",StringComparison.OrdinalIgnoreCase)||p.EndsWith(".gltf",StringComparison.OrdinalIgnoreCase))))
        {
            var importer=AssetImporter.GetAtPath(path);
            if(!importer||importer.GetType().FullName!="GLTFast.Editor.GltfImporter")continue;
            var serialized=new SerializedObject(importer);
            var items=serialized.FindProperty("reportItems");
            if(items==null||!items.isArray)continue;
            for(int i=0;i<items.arraySize;i++)
            {
                var item=items.GetArrayElementAtIndex(i);
                var type=item.FindPropertyRelative("type");var code=item.FindPropertyRelative("code");
                var messages=item.FindPropertyRelative("messages");
                if(type==null||code==null||messages==null)continue;
                var args=new string[messages.arraySize];
                for(int j=0;j<args.Length;j++)args[j]=messages.GetArrayElementAtIndex(j).stringValue;
                var level=(LogType)type.intValue;
                var text=new GLTFast.Logging.LogItem(level,(GLTFast.Logging.LogCode)code.intValue,args).ToString();
                result.issues.Add(new ImportIssue{path=path,type=level.ToString(),code=code.intValue,message=text});
                if(level==LogType.Error||level==LogType.Exception||level==LogType.Assert)errors++;
                Debug.Log("BRAX_SOURCE_IMPORT "+path+" ["+level+"] "+text);
            }
        }
        File.WriteAllText(Path.Combine(reports,"source-imports.json"),JsonUtility.ToJson(result,true));
        return errors;
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
        // Match Champions PBR import overrides on modern ASTC-capable Android GPUs.
        EditorUserBuildSettings.androidBuildSubtarget=MobileTextureSubtarget.ASTC;

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
        // Immutable first baseline: the exploration scene (entire world + player)
        // is delivered as a bundled scene. Future scene/material revisions build
        // .braxupdate.zip against this saved state without BuildPlayer.
        if (!BraxContentBuild.Requested)
            throw new InvalidOperationException("Miasto Mistrzów wymaga nowego APK bazowego z Addressables: content-mode.txt=baseline");
        BraxContentBuild.Baseline("mistrzowie", new[] { BraxChampionsExplorer.ScenePath });
        // Apply to every selectable quality tier of this separate APK, not to other region builders.
        int previousQuality=QualitySettings.GetQualityLevel();
        for(int i=0;i<QualitySettings.names.Length;i++)
        {
            QualitySettings.SetQualityLevel(i,false);
            QualitySettings.streamingMipmapsActive=true;
            QualitySettings.streamingMipmapsMemoryBudget=256; // budzet testowy; pomiar GPU na telefonie wymagany
            QualitySettings.streamingMipmapsMaxLevelReduction=3; // 2K blisko; do 256px przy ograniczonym budzecie i w oddali
            QualitySettings.streamingMipmapsAddAllCameras=true;
        }
        QualitySettings.SetQualityLevel(previousQuality,false);
        var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{startPath},target=BuildTarget.Android,
            locationPathName=Path.Combine(output,"braxhorn-mistrzowie.apk"),options=BuildOptions.CompressWithLz4});
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
            if(report.walkChecksSkipped)continue;
            try { report.walkSamples+=CheckWalk(station);report.checks.Add(station.name+": surface-to-platform path clear"); }
            catch(Exception e) { report.failures.Add(station.name+" walk: "+e.Message); }
        }
        if(report.failures.Count>before)BraxBuildQuality.Issue(new InvalidOperationException(string.Join("\n",report.failures.Skip(before))));
    }
    static void CheckExteriorAccess()
    {
        try
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
        catch (System.InvalidOperationException qualityError)
        {
            BraxBuildQuality.Issue(qualityError);
        }
    }
    static int CheckAssets(Scene scene)
    {
        try
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
        catch (System.InvalidOperationException qualityError)
        {
            BraxBuildQuality.Issue(qualityError); return -1;
        }
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

