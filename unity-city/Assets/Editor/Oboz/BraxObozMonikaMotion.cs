using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

// Monika locomotion: calibrated avatar on a copy, original mesh and bind poses.
public static partial class BraxObozCharacters
{
    static AnimationClip BakeDirectMotion(GameObject hero,Avatar avatar,Transform hips,Rest[] rest,ClipSpec spec)
    {
        var sourceClip=AssetDatabase.LoadAllAssetsAtPath(spec.path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).OrderByDescending(c=>c.length).First();
        if(!sourceClip.humanMotion||sourceClip.length<=0)throw new InvalidOperationException("Nieprawidłowy klip Humanoid");
        var animator=hero.AddComponent<Animator>();animator.avatar=avatar;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        var graph=PlayableGraph.Create("Monika direct retarget comparison");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        try
        {
            var playable=AnimationClipPlayable.Create(graph,sourceClip);playable.SetApplyFootIK(true);
            AnimationPlayableOutput.Create(graph,"Monika",animator).SetSourcePlayable(playable);graph.Play();
            int count=Mathf.CeilToInt(sourceClip.length*30)+1;var times=new float[count];var positions=new Vector3[count];var rotations=rest.Select(_=>new Quaternion[count]).ToArray();
            for(int i=0;i<count;i++)
            {
                Restore(rest);float time=Mathf.Min(i/30f,sourceClip.length);playable.SetTime(time);graph.Evaluate(0);times[i]=time;positions[i]=hips.localPosition;
                for(int j=0;j<rest.Length;j++)
                {
                    var q=rest[j].t.localRotation;
                    if(i>0&&Quaternion.Dot(q,rotations[j][i-1])<0)q=new Quaternion(-q.x,-q.y,-q.z,-q.w);
                    rotations[j][i]=q;
                }
            }
            if(spec.inPlace)
            {
                var drift=positions[count-1]-positions[0];drift.y=0;
                for(int i=0;i<count;i++)positions[i]-=drift*(times[i]/sourceClip.length);
            }
            var result=new AnimationClip{name=spec.name,legacy=true,frameRate=30,wrapMode=spec.loop?WrapMode.Loop:WrapMode.Once};
            for(int j=0;j<rest.Length;j++)for(int axis=0;axis<4;axis++)
                result.SetCurve(AnimationUtility.CalculateTransformPath(rest[j].t,hero.transform),typeof(Transform),"localRotation."+"xyzw"[axis],Curve(times,rotations[j].Select(q=>q[axis]).ToArray()));
            for(int axis=0;axis<3;axis++)result.SetCurve(AnimationUtility.CalculateTransformPath(hips,hero.transform),typeof(Transform),"localPosition."+"xyz"[axis],Curve(times,positions.Select(p=>p[axis]).ToArray()));
            result.EnsureQuaternionContinuity();return result;
        }
        finally{graph.Destroy();Object.DestroyImmediate(animator);Restore(rest);}
    }

    static Avatar CalibratedMotionAvatar(GameObject source,Avatar imported)
    {
        var copy=Object.Instantiate(source);copy.name=source.name;
        try
        {
            var hd=imported.humanDescription;var transforms=copy.GetComponentsInChildren<Transform>(true);
            // HumanDescription uses spaces in finger names (e.g. Left Middle Proximal).
            Transform Bone(string human,bool optional=false)
            {
                var maps=hd.human.Where(h=>h.humanName.Replace(" ","")==human).ToArray();
                if(maps.Length==0&&optional)return null;
                if(maps.Length!=1)throw new InvalidOperationException("Niejednoznaczne mapowanie kości: "+human);
                return transforms.Single(t=>t.name==maps[0].boneName);
            }
            void Aim(Transform from,Transform to,Vector3 direction){from.rotation=Quaternion.FromToRotation(to.position-from.position,direction)*from.rotation;}
            var right=Bone("RightUpperLeg").position-Bone("LeftUpperLeg").position;right.y=0;right.Normalize();var forward=Vector3.Cross(right,Vector3.up);
            foreach(string side in new[]{"Left","Right"})
            {
                var thigh=Bone(side+"UpperLeg");var shin=Bone(side+"LowerLeg");var foot=Bone(side+"Foot");var toes=Bone(side+"Toes");
                Aim(thigh,shin,Vector3.down);Aim(shin,foot,Vector3.down);
                var toeDirection=toes.position-foot.position;toeDirection.y=0;
                thigh.rotation=Quaternion.AngleAxis(Vector3.SignedAngle(toeDirection,forward,Vector3.up),Vector3.up)*thigh.rotation;
                var toe=toes.position-foot.position;float pitch=Mathf.Clamp(toe.normalized.y,-.95f,.95f);
                Aim(foot,toes,forward*Mathf.Sqrt(1-pitch*pitch)+Vector3.up*pitch);
                var arm=Bone(side+"UpperArm");var forearm=Bone(side+"LowerArm");var hand=Bone(side+"Hand");var outward=side=="Left"?-right:right;
                Aim(arm,forearm,outward);Aim(forearm,hand,outward);
                var middle=Bone(side+"MiddleProximal",true);if(middle)Aim(hand,middle,outward);
                var thumbBone=Bone(side+"ThumbProximal",true);
                if(thumbBone)
                {
                    var thumb=Vector3.ProjectOnPlane(thumbBone.position-hand.position,outward);
                    if(thumb.sqrMagnitude>1e-10f)arm.rotation=Quaternion.AngleAxis(Vector3.SignedAngle(thumb,forward,outward),outward)*arm.rotation;
                }
            }
            hd.skeleton=transforms.Select(t=>new SkeletonBone{name=t.name,position=t.localPosition,rotation=t.localRotation,scale=t.localScale}).ToArray();
            var avatar=AvatarBuilder.BuildHumanAvatar(copy,hd);
            if(!avatar||!avatar.isValid||!avatar.isHuman)throw new InvalidOperationException("Nieprawidłowy awatar porównawczy");
            avatar.name="Monika calibrated comparison";return avatar;
        }
        finally{Object.DestroyImmediate(copy);}
    }

}
