using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

// A/B experiment on the original Monika, not an APK builder or an accepted fix.
public static partial class BraxObozCharacters
{
    [Serializable] sealed class MotionFrame
    {
        public float time, leftFootYaw, rightFootYaw, leftKneePlane, rightKneePlane;
        public float leftArmFromDown, rightArmFromDown, leftForearmFromDown, rightForearmFromDown;
        public float leftFootPitch, rightFootPitch;
        public bool leftFootMeasurable, rightFootMeasurable;
    }
    [Serializable] sealed class MotionCase
    {
        public string variant, clip, source;
        public MotionFrame[] frames;
        public string[] captures;
    }
    [Serializable] sealed class MotionReview
    {
        public string unity, commit, character="monika", graphics, captureMethod="cpu-baked-mesh";
        public bool phoneTested=false, appearanceApproved=false, experimentOnly=true, complete=false;
        public MotionCase[] cases;
        public string[] errors;
    }

    [MenuItem("BRAXHORN/Obóz/Monika — porównaj ruch bez APK")]
    public static void ReviewMonika()
    {
        var manifest=ReadManifest();var spec=manifest.characters.Single(c=>c.id=="monika");
        string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../build/MonikaReview"));
        Directory.CreateDirectory(output);
        var report=new MotionReview{unity=Application.unityVersion,commit=Environment.GetEnvironmentVariable("GITHUB_SHA")??"local",graphics=SystemInfo.graphicsDeviceType.ToString()};
        var cases=new List<MotionCase>();var errors=new List<string>();
        var reviewClips=spec.clips.Where(c=>new[]{"Punch","Training"}.Contains(c.name)).ToList();
        reviewClips.Insert(0,new ClipSpec{name="Idle",path="Assets/Source/Prywatne/Oboz/animations/brax/Idle.fbx",donor="brax",loop=true,inPlace=true});
        reviewClips.Insert(1,new ClipSpec{name="Walk",path="Assets/Source/Prywatne/Oboz/animations/nyxa/Walking.fbx",donor="nyxa",loop=true,inPlace=true});
        reviewClips.Insert(2,new ClipSpec{name="Run",path="Assets/Source/Prywatne/Oboz/animations/brax/Running.fbx",donor="brax",loop=true,inPlace=true});
        foreach(string name in new[]{"Idle","Walk","Run"})
            reviewClips.Add(new ClipSpec{name="Casual"+name,path="Assets/Source/Prywatne/Oboz/animations/casual/"+name+".anim",loop=true,inPlace=true,donor="Basic Motions"});
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(spec.modelPath);
        var imported=AssetDatabase.LoadAllAssetsAtPath(spec.modelPath).OfType<Avatar>().FirstOrDefault();
        if(!source||!imported||!imported.isValid||!imported.isHuman)throw new InvalidOperationException("Brak modelu/awatara Moniki");
        var hero=Object.Instantiate(source);hero.name="Monika motion comparison";
        hero.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);hero.transform.localScale=Vector3.one;
        var temp=new List<Object>();temp.Add(hero);
        try
        {
            foreach(var a in hero.GetComponentsInChildren<Animator>(true))Object.DestroyImmediate(a);
            foreach(var a in hero.GetComponentsInChildren<Animation>(true))Object.DestroyImmediate(a);
            var bones=hero.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("mixamorig",StringComparison.OrdinalIgnoreCase)).ToArray();
            if(bones.Length!=65)throw new InvalidOperationException("Monika: oczekiwano 65 kości");
            var rest=bones.Select(t=>new Rest{t=t,p=t.localPosition,r=t.localRotation,s=t.localScale}).ToArray();
            var hips=bones.Single(t=>BoneName(t.name)=="Hips");
            Directory.CreateDirectory(Generated+"/monika");AssetDatabase.Refresh();
            var material=Material(spec,Generated+"/monika");
            foreach(var renderer in hero.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                renderer.sharedMaterials=Enumerable.Repeat(material,renderer.sharedMesh.subMeshCount).ToArray();
                renderer.updateWhenOffscreen=true;renderer.shadowCastingMode=ShadowCastingMode.Off;
            }
            Avatar calibrated=null;
            if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null)throw new InvalidOperationException("Brak urządzenia graficznego do dowodu wizualnego");
            var cameraGo=new GameObject("Motion review camera");temp.Add(cameraGo);var camera=cameraGo.AddComponent<Camera>();
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.14f,.17f);camera.orthographic=true;camera.nearClipPlane=.01f;camera.farClipPlane=100;
            camera.cullingMask=1<<27;
            foreach(var t in hero.GetComponentsInChildren<Transform>(true))t.gameObject.layer=27;
            var sun=new GameObject("Motion review light");temp.Add(sun);var light=sun.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.3f;light.cullingMask=1<<27;sun.transform.rotation=Quaternion.Euler(35,-30,0);
            var previousAmbient=RenderSettings.ambientLight;var previousMode=RenderSettings.ambientMode;
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.5f,.5f,.5f);
            try
            {
                foreach(var clipSpec in reviewClips)
                foreach(string variant in clipSpec.name.StartsWith("Casual",StringComparison.Ordinal)?new[]{"direct","calibrated-direct"}:new[]{"baseline","direct","calibrated-direct"})
                {
                    Restore(rest);AnimationClip baked=null;
                    try
                    {
                        if(variant=="calibrated-direct"&&!calibrated){calibrated=CalibratedMotionAvatar(hero,imported);temp.Add(calibrated);}
                        baked=variant=="baseline"?Bake(hero,imported,hips,rest,clipSpec):BakeDirectMotion(hero,variant=="direct"?imported:calibrated,hips,rest,clipSpec);
                        Validate(hero,hips,rest,baked);
                        var frames=new List<MotionFrame>();var captures=new List<string>();
                        for(int i=0;i<60;i++)
                        {
                            Restore(rest);float time=baked.length*i/60f;baked.SampleAnimation(hero,time);
                            frames.Add(MeasureMotion(bones,time));
                            if(i%5==0)
                            {
                                using(var snapshot=new MotionSnapshot(hero))
                                foreach(var view in new[]{"front","back","side"})
                                {
                                    string file=variant+"-"+clipSpec.name+"-"+view+"-"+i.ToString("D2")+".png";
                                    CaptureMotion(snapshot.renderers,camera,view,Path.Combine(output,file));captures.Add(file);
                                }
                            }
                        }
                        cases.Add(new MotionCase{variant=variant,clip=clipSpec.name,source=clipSpec.path,frames=frames.ToArray(),captures=captures.ToArray()});
                        Debug.Log("BRAXHORN_MONIKA_REVIEW "+variant+" "+clipSpec.name+" samples="+frames.Count);
                    }
                    catch(Exception ex){errors.Add(variant+"/"+clipSpec.name+": "+ex.GetType().Name+": "+ex.Message);Debug.LogException(ex);}
                    finally{if(baked)Object.DestroyImmediate(baked);Restore(rest);}
                }
            }
            finally{RenderSettings.ambientLight=previousAmbient;RenderSettings.ambientMode=previousMode;}
            report.complete=cases.Count==21&&errors.Count==0;
        }
        finally
        {
            report.cases=cases.ToArray();report.errors=errors.ToArray();File.WriteAllText(Path.Combine(output,"motion-review.json"),JsonUtility.ToJson(report,true));
            foreach(var obj in temp)if(obj)Object.DestroyImmediate(obj);
        }
        if(!report.complete)throw new InvalidOperationException("Niepełne porównanie ruchu Moniki: "+cases.Count+"/21; "+string.Join("; ",errors));
        Debug.Log("BRAXHORN_MONIKA_REVIEW complete=true phone_tested=false accepted_fix=false");
    }

    static MotionFrame MeasureMotion(Transform[] bones,float time)
    {
        Transform Bone(string name)=>bones.Single(t=>BoneName(t.name)==name);
        var result=new MotionFrame{time=time};var right=Bone("RightUpLeg").position-Bone("LeftUpLeg").position;right.y=0;right.Normalize();var forward=Vector3.Cross(right,Vector3.up);
        foreach(string side in new[]{"Left","Right"})
        {
            var foot=Bone(side+"ToeBase").position-Bone(side+"Foot").position;var flat=Vector3.ProjectOnPlane(foot,Vector3.up);bool measurable=flat.magnitude>foot.magnitude*.3f;
            var thigh=Bone(side+"Leg").position-Bone(side+"UpLeg").position;var shin=Bone(side+"Foot").position-Bone(side+"Leg").position;var axis=Vector3.Cross(thigh,shin);
            float plane=Vector3.Angle(thigh,shin)>15?Mathf.Min(Vector3.Angle(axis,right),Vector3.Angle(axis,-right)):0;
            float arm=Vector3.Angle(Bone(side+"ForeArm").position-Bone(side+"Arm").position,Vector3.down);
            float forearm=Vector3.Angle(Bone(side+"Hand").position-Bone(side+"ForeArm").position,Vector3.down);
            float pitch=Mathf.Asin(Mathf.Clamp(foot.normalized.y,-1,1))*Mathf.Rad2Deg;
            if(side=="Left"){result.leftFootMeasurable=measurable;result.leftFootYaw=measurable?Vector3.Angle(flat,forward):0;result.leftFootPitch=pitch;result.leftKneePlane=plane;result.leftArmFromDown=arm;result.leftForearmFromDown=forearm;}
            else{result.rightFootMeasurable=measurable;result.rightFootYaw=measurable?Vector3.Angle(flat,forward):0;result.rightFootPitch=pitch;result.rightKneePlane=plane;result.rightArmFromDown=arm;result.rightForearmFromDown=forearm;}
        }
        return result;
    }
    // Camera.Render calls in one editor tick can reuse stale GPU skinning.
    // BakeMesh computes the currently measured pose on CPU. Render only this
    // temporary snapshot with the original material; never modify source meshes.
    sealed class MotionSnapshot : IDisposable
    {
        readonly SkinnedMeshRenderer[] sources;
        readonly bool[] enabled;
        readonly List<GameObject> objects=new List<GameObject>();
        readonly List<Mesh> meshes=new List<Mesh>();
        public readonly List<MeshRenderer> renderers=new List<MeshRenderer>();
        public MotionSnapshot(GameObject hero)
        {
            sources=hero.GetComponentsInChildren<SkinnedMeshRenderer>();
            enabled=sources.Select(r=>r.enabled).ToArray();
            try
            {
                foreach(var source in sources)
                {
                    var mesh=new Mesh();meshes.Add(mesh);source.BakeMesh(mesh,false);mesh.RecalculateBounds();
                    if(mesh.vertexCount!=source.sharedMesh.vertexCount)throw new InvalidOperationException("Niepełna migawka siatki Moniki");
                    var go=new GameObject("Measured pose snapshot");objects.Add(go);go.layer=27;
                    go.transform.SetParent(source.transform,false);
                    go.AddComponent<MeshFilter>().sharedMesh=mesh;
                    var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterials=source.sharedMaterials;
                    renderer.shadowCastingMode=ShadowCastingMode.Off;renderers.Add(renderer);source.enabled=false;
                }
            }
            catch{Dispose();throw;}
        }
        public void Dispose()
        {
            foreach(var go in objects)if(go)Object.DestroyImmediate(go);
            foreach(var mesh in meshes)if(mesh)Object.DestroyImmediate(mesh);
            for(int i=0;i<sources.Length;i++)if(sources[i])sources[i].enabled=enabled[i];
        }
    }
    static void CaptureMotion(List<MeshRenderer> renderers,Camera camera,string view,string output)
    {
        Bounds bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
        var center=bounds.center;
        // Include horizontal extent: prone GettingUp poses were cropped by a height-only frame.
        float width=view=="side"?bounds.size.z:bounds.size.x;
        float span=Mathf.Max(bounds.size.y,width/(512f/640f),1f)*1.25f;
        Vector3 direction=view=="front"?Vector3.forward:view=="back"?Vector3.back:Vector3.right;
        camera.transform.position=center+direction*span*3;camera.transform.LookAt(center);camera.orthographicSize=span*.5f;
        var rt=RenderTexture.GetTemporary(512,640,24,RenderTextureFormat.ARGB32);var previous=RenderTexture.active;
        var image=new Texture2D(512,640,TextureFormat.RGB24,false);
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,512,640),0,0);image.Apply();File.WriteAllBytes(output,image.EncodeToPNG());}
        finally{camera.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(image);}
    }
}
