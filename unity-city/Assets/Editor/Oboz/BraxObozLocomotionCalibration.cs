using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

// Measure the actual imported/baked clips on a disposable visual clone. No
// changes to owner FBX, source curves, avatar, model dimensions or shared player.
public static class BraxObozLocomotionCalibration
{
    const float K=1.294f;
    [Serializable] public sealed class ClipResult
    {
        public string actor,clip,source,sourceHash,status;
        public bool measured;
        public int samples,leftStanceSamples,rightStanceSamples;
        public float duration,worldUnitsPerSecond,metresPerSecond,relativeMedianDeviation,medianLateralRatio;
    }
    [Serializable] sealed class Report
    {
        public string revision;
        public bool unityAnimationSampled=true,phoneVerified=false;
        public ClipResult[] clips;
    }
    struct Pose
    {
        public Transform transform;
        public Vector3 position,scale;
        public Quaternion rotation;
    }

    static float Median(List<float> values)
    {
        values.Sort();int count=values.Count;
        return count%2==0?(values[count/2-1]+values[count/2])*.5f:values[count/2];
    }

    public static ClipResult Measure(Transform sourceVisual,Vector3 actorScale,AnimationClip clip,string actor)
    {
        var result=new ClipResult{actor=actor,clip=clip?clip.name:"missing",status="not sampled"};
        if(!sourceVisual || !clip || clip.length<=.01f)
        {result.status="missing visual or nonempty clip";return result;}
        result.source=AssetDatabase.GetAssetPath(clip);
        result.sourceHash=string.IsNullOrEmpty(result.source)?"temporary regression clip":
            AssetDatabase.GetAssetDependencyHash(result.source).ToString();
        result.duration=clip.length;
        GameObject probe=null;
        try
        {
            probe=new GameObject("temporary camp stride measurement"){hideFlags=HideFlags.HideAndDontSave};
            probe.transform.localScale=actorScale;
            var capsule=probe.AddComponent<CharacterController>();capsule.enabled=false;
            var model=Object.Instantiate(sourceVisual.gameObject,probe.transform,false);
            var animation=model.GetComponent<Animation>();
            if(!animation)animation=model.AddComponent<Animation>();
            animation.Stop();
            var contact=probe.AddComponent<BraxObozIdleFootContact>();
            contact.Configure(capsule,model.transform,animation);
            var transforms=model.GetComponentsInChildren<Transform>(true);
            var rest=new Pose[transforms.Length];
            for(int i=0;i<rest.Length;i++)rest[i]=new Pose {
                transform=transforms[i],position=transforms[i].localPosition,
                rotation=transforms[i].localRotation,scale=transforms[i].localScale};
            void Restore()
            {
                foreach(var pose in rest)
                {
                    pose.transform.localPosition=pose.position;
                    pose.transform.localRotation=pose.rotation;
                    pose.transform.localScale=pose.scale;
                }
            }
            int count=Mathf.Clamp(Mathf.CeilToInt(clip.length*60f),24,600);
            float dt=clip.length/count;
            var left=new Vector3[count+1];var right=new Vector3[count+1];
            float leftFloor=float.PositiveInfinity,rightFloor=float.PositiveInfinity;
            for(int i=0;i<=count;i++)
            {
                Restore();clip.SampleAnimation(model,dt*i);
                if(!contact.TryGetSolePosition(true,out left[i]) ||
                    !contact.TryGetSolePosition(false,out right[i]))
                {result.status="weighted foot geometry unavailable";return result;}
                leftFloor=Mathf.Min(leftFloor,left[i].y);
                rightFloor=Mathf.Min(rightFloor,right[i].y);
            }
            result.samples=count+1;
            var speeds=new List<float>();var lateralRatios=new List<float>();
            // The probe root represents the actor. Retain the visual's own
            // rotation and reject sideways/backwards imports instead of hiding
            // a facing error by measuring along the visual's arbitrary Z axis.
            Vector3 actorForward=probe.transform.forward,actorRight=probe.transform.right;
            int Collect(Vector3[] foot,float floor)
            {
                int accepted=0;
                for(int i=0;i<count;i++)
                {
                    // Stance only: both ends of the interval stay within 3 cm
                    // of this foot's lowest pose in the original clip.
                    if(Mathf.Max(foot[i].y,foot[i+1].y)>floor+.03f*K)continue;
                    Vector3 velocity=(foot[i+1]-foot[i])/dt;
                    float backwards=-Vector3.Dot(velocity,actorForward);
                    if(backwards<=.01f*K || float.IsNaN(backwards) || float.IsInfinity(backwards))continue;
                    speeds.Add(backwards);
                    lateralRatios.Add(Mathf.Abs(Vector3.Dot(velocity,actorRight))/backwards);
                    accepted++;
                }
                return accepted;
            }
            result.leftStanceSamples=Collect(left,leftFloor);
            result.rightStanceSamples=Collect(right,rightFloor);
            if(result.leftStanceSamples<2 || result.rightStanceSamples<2)
            {result.status="too few backward stance samples on both feet";return result;}
            result.medianLateralRatio=Median(lateralRatios);
            if(result.medianLateralRatio>.35f)
            {result.status="stance motion does not follow actor forward; source rotation retained";return result;}
            float median=Median(speeds);
            var deviations=new List<float>(speeds.Count);
            foreach(float speed in speeds)deviations.Add(Mathf.Abs(speed-median));
            float deviation=Median(deviations);
            result.relativeMedianDeviation=deviation/median;
            // A very inconsistent clip is reported, not assigned an invented speed.
            if(result.relativeMedianDeviation>.5f)
            {result.status="inconsistent stance speeds; requires pose review";return result;}
            result.worldUnitsPerSecond=median;
            result.metresPerSecond=median/K;
            result.measured=true;result.status="measured stance median; phone motion review pending";
            return result;
        }
        finally{if(probe)Object.DestroyImmediate(probe);}
    }

    public static void ConfigureAll(BraxObozSceneInfo info)
    {
        var results=new List<ClipResult>();
        void ConfigureActor(GameObject actor,CharacterController body,Animation clips,BraxObozNpc npc)
        {
            if(!actor)return;
            try
            {
                var contact=actor.GetComponent<BraxObozIdleFootContact>();
                if(!contact || !contact.visual || !clips || !body)
                {BraxBuildQuality.Warn("OBOZ_STRIDE_MISSING_BINDING — "+actor.name);return;}
                var walk=Measure(contact.visual,actor.transform.lossyScale,clips.GetClip("Walk"),actor.name);
                var run=Measure(contact.visual,actor.transform.lossyScale,clips.GetClip("Run"),actor.name);
                results.Add(walk);results.Add(run);
                var match=actor.GetComponent<BraxObozStrideMatch>();
                if(!match)match=actor.AddComponent<BraxObozStrideMatch>();
                match.Configure(info.player,npc,body,clips,
                    walk.measured?walk.worldUnitsPerSecond:0f,run.measured?run.worldUnitsPerSecond:0f);
                foreach(var sample in new[]{walk,run})
                {
                    if(!sample.measured)BraxBuildQuality.Warn("OBOZ_STRIDE_UNMEASURED — "+
                        sample.actor+"/"+sample.clip+": "+sample.status);
                    else Debug.Log("BRAXHORN_OBOZ_STRIDE actor="+sample.actor+" clip="+sample.clip+
                        " nominal_world_speed="+sample.worldUnitsPerSecond+
                        " median_deviation="+sample.relativeMedianDeviation+" phone_tested=false");
                }
            }
            catch(Exception ex){BraxBuildQuality.Warn("OBOZ_STRIDE_CALIBRATION — "+actor.name+": "+ex);}
        }
        if(info.player)ConfigureActor(info.player.gameObject,info.player.body,info.player.clips,null);
        foreach(var npc in info.npcs??Array.Empty<BraxObozNpc>())
            if(npc)ConfigureActor(npc.gameObject,npc.body,npc.clips,npc);
        string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../audit"));
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder,"oboz-krok-postaci.json"),JsonUtility.ToJson(new Report {
            revision=Environment.GetEnvironmentVariable("GITHUB_SHA")??"local",clips=results.ToArray()},true));
    }
}
