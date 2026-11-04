using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Focused additive region content build in the same Unity project, not a replacement APK.
public static class BraxChampionsBuild
{
    [Serializable] sealed class Report
    {
        public string status="STARTED",sourceCommit,unity;
        public bool visualAccepted=false,deviceTested=false,trafficEnabled=false,sceneReloadPassed=false;
        public int rendererCount,walkSamples;public BraxChampionsInfrastructure.Counts counts;
        public List<string> checks=new List<string>();
    }
    public static void Build()
    {
        string reports=Path.GetFullPath("../mistrzowie-evidence");Directory.CreateDirectory(reports);
        var report=new Report{unity=Application.unityVersion,sourceCommit=Environment.GetEnvironmentVariable("GITHUB_SHA")??"local"};
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            BraxChampionsFoundation.Generate();
            var scene=EditorSceneManager.OpenScene(BraxChampionsFoundation.ScenePath,OpenSceneMode.Single);
            var d=BraxChampionsInfrastructure.Generate();report.counts=d.counts;
            Physics.SyncTransforms();
            foreach(var t in BraxChampionsInfrastructure.Stations)
            {
                report.walkSamples+=CheckWalk(t);report.checks.Add(t.name+": surface-to-platform path clear");
            }
            CheckAssets(scene);AssetDatabase.SaveAssets();
            if(!EditorSceneManager.SaveScene(scene))throw new IOException("Scene save failed");
            // Reopen the actual serialized scene; live editor objects are not proof of saved references.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            scene=EditorSceneManager.OpenScene(BraxChampionsFoundation.ScenePath,OpenSceneMode.Single);
            report.rendererCount=CheckAssets(scene);report.sceneReloadPassed=true;
            report.checks.Add("Serialized scene reopened: meshes, scripts, colliders and materials present");
            string payload=Path.GetFullPath("../mistrzowie-payload");Directory.CreateDirectory(payload+"/Android");
            string graph="Assets/Source/Mistrzowie/Transit.json";
            // Split scene and source vehicle assets into separate bundles; gameplay traffic is deferred.
            var builds=new[]{new AssetBundleBuild{assetBundleName="mistrzowie_foundation",assetNames=new[]{BraxChampionsFoundation.ScenePath}},
                new AssetBundleBuild{assetBundleName="mistrzowie_transit_sources",assetNames=new[]{"Assets/Source/Claude/Metro/metro_train.glb",graph}}};
            var manifest=BuildPipeline.BuildAssetBundles(payload+"/Android",builds,BuildAssetBundleOptions.ChunkBasedCompression|BuildAssetBundleOptions.StrictMode,BuildTarget.Android);
            if(!manifest)throw new InvalidOperationException("Android region bundles failed");
            report.checks.Add("Unity Android scene and transit bundles built");
            AssetDatabase.ExportPackage(new[]{BraxChampionsFoundation.ScenePath,"Assets/Source/Claude/Metro/metro_train.glb",graph},payload+"/Mistrzowie_Foundation.unitypackage",ExportPackageOptions.IncludeDependencies);
            File.Copy("Assets/Source/Claude/Metro/CREDITS.md",payload+"/METRO_CREDITS.md",true);
            report.status="UNITY_SCENE_AND_ANDROID_BUNDLES_PASS_VISUAL_REVIEW_PENDING";
        }
        catch(Exception e){report.status="FAILED: "+e.Message;Debug.LogException(e);throw;}
        finally {File.WriteAllText(reports+"/unity-validation.json",JsonUtility.ToJson(report,true));Debug.Log("BRAX_MISTRZOWIE_UNITY "+JsonUtility.ToJson(report));}
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
            .OrderBy(g=>Mathf.Abs(station.InverseTransformPoint(g.transform.position).z-28.45f)).FirstOrDefault();
        if(!gate)throw new InvalidOperationException(station.name+" has no original ticket gates");
        float gateZ=station.InverseTransformPoint(gate.transform.position).z;
        local[5].z=gateZ;local[6].z=gateZ;
        var route=local.Select(station.TransformPoint).ToArray();
        float y=route[0].y;int count=0;
        const int mask=~((1<<8)|(1<<2)|(1<<29)|(1<<31));
        for(int i=0;i<route.Length-1;i++)
        {
            var a=route[i];var b=route[i+1];var delta=b-a;delta.y=0;float len=delta.magnitude;
            if(len<.01f)continue;
            int steps=Mathf.CeilToInt(len/.25f);
            for(int k=0;k<=steps;k++)
            {
                var q=Vector3.Lerp(a,b,k/(float)steps);q.y=y;
                if(!Physics.Raycast(q+Vector3.up*.8f,Vector3.down,out var hit,2.0f,mask,QueryTriggerInteraction.Ignore))
                    throw new InvalidOperationException(station.name+" no walk floor at "+q);
                if(hit.point.y>y+.45f||hit.point.y<y-.55f)throw new InvalidOperationException(station.name+" walk discontinuity at "+hit.point+" / "+hit.collider.name);
                y=hit.point.y;q.y=y;
                foreach(var c in Physics.OverlapCapsule(q+Vector3.up*.73f,q+Vector3.up*1.92f,.33f,mask,QueryTriggerInteraction.Ignore))
                {
                    if(c.GetComponentInParent<BraxDrzwiPrzesuwne>())continue; // existing automatic ticket gate
                    throw new InvalidOperationException(station.name+" blocked walkway at "+q+" / "+c.name);
                }
                count++;
            }
        }
        return count;
    }
}
