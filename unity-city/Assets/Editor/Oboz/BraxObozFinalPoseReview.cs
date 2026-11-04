using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

public static partial class BraxObozCharacters
{
    // Inspect the actual clips attached to the final scene, including reactions and kicks.
    // CPU snapshots avoid the stale GPU skinning problem in batch Camera.Render.
    static void ReviewFinalPoses(BraxObozSceneInfo scene)
    {
        string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../build/CampLifeReview"));
        Directory.CreateDirectory(output);
        var cameraGo=new GameObject("Final pose camera");var camera=cameraGo.AddComponent<Camera>();
        camera.cullingMask=1<<27;camera.orthographic=true;camera.nearClipPlane=.01f;camera.farClipPlane=100;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.14f,.17f);
        var sunGo=new GameObject("Final pose light");var light=sunGo.AddComponent<Light>();
        light.type=LightType.Directional;light.intensity=1.3f;light.cullingMask=1<<27;sunGo.transform.rotation=Quaternion.Euler(35,-30,0);
        var oldAmbient=RenderSettings.ambientLight;var oldMode=RenderSettings.ambientMode;
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.5f,.5f,.5f);
        var sources=scene.npcs.Select(n=>new KeyValuePair<string,Animation>(n.characterId,n.clips)).ToList();
        sources.Add(new KeyValuePair<string,Animation>("monika",scene.player.clips));
        var reports=new List<MotionReview>();
        try
        {
            foreach(var source in sources)
            {
                var hero=Object.Instantiate(source.Value.gameObject);
                hero.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                try
                {
                    var clips=hero.GetComponent<Animation>().Cast<AnimationState>().Select(s=>s.clip).ToArray();
                    Object.DestroyImmediate(hero.GetComponent<Animation>());
                    foreach(var t in hero.GetComponentsInChildren<Transform>())t.gameObject.layer=27;
                    var bones=hero.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("mixamorig",StringComparison.OrdinalIgnoreCase)).ToArray();
                    if(bones.Length!=65||clips.Length!=14)throw new InvalidOperationException(source.Key+": niepełna obsada kości/klipów");
                    var rest=bones.Select(t=>new Rest{t=t,p=t.localPosition,r=t.localRotation,s=t.localScale}).ToArray();
                    var cases=new List<MotionCase>();
                    foreach(var clip in clips)
                    {
                        var frames=new List<MotionFrame>();var captures=new List<string>();
                        for(int i=0;i<30;i++)
                        {
                            Restore(rest);float time=clip.length*i/30f;clip.SampleAnimation(hero,time);
                            frames.Add(MeasureMotion(bones,time));
                            if(i%5!=0)continue;
                            using(var snapshot=new MotionSnapshot(hero))
                            foreach(string view in new[]{"front","back","side"})
                            {
                                string file="final-"+source.Key+"-"+clip.name+"-"+view+"-"+i.ToString("D2")+".png";
                                CaptureMotion(snapshot.renderers,camera,view,Path.Combine(output,file));captures.Add(file);
                            }
                        }
                        cases.Add(new MotionCase{variant="final-scene",clip=clip.name,frames=frames.ToArray(),captures=captures.ToArray()});
                    }
                    reports.Add(new MotionReview{character=source.Key,unity=Application.unityVersion,commit=Environment.GetEnvironmentVariable("GITHUB_SHA"),
                        complete=true,experimentOnly=false,cases=cases.ToArray(),errors=Array.Empty<string>()});
                    Debug.Log("BRAXHORN_FINAL_POSE "+source.Key+" clips=14 measured=420 captures=252 appearance_approved=false");
                }
                finally{Object.DestroyImmediate(hero);}
            }
            File.WriteAllText(Path.Combine(output,"final-pose-review.json"),JsonUtility.ToJson(new NpcMotionReview{
                unity=Application.unityVersion,commit=Environment.GetEnvironmentVariable("GITHUB_SHA"),complete=reports.Count==7,
                characters=reports.ToArray(),errors=Array.Empty<string>()},true));
        }
        finally
        {
            RenderSettings.ambientLight=oldAmbient;RenderSettings.ambientMode=oldMode;
            Object.DestroyImmediate(cameraGo);Object.DestroyImmediate(sunGo);
        }
    }
}
