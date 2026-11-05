using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

// Monika locomotion: calibrated avatar on a copy, original mesh and bind poses.
public static partial class BraxObozCharacters
{
    const string MonikaOwnerSource = "Assets/Source/Prywatne/Oboz/animations/monika-owner/";
    const string MonikaDirectSources = "Assets/PlayableGenerated/Oboz/MonikaOwnerDirect/";

    // Tylko te trzy pliki Mixamo wykonano na szkielecie Moniki: 65 wspólnych
    // nazw kości. Humanoid SetHumanPose powodował obroty kolan i stóp.
    // Nie poprawiamy deformacji fałszywym skręcaniem kostek — zachowujemy
    // bezpośrednie lokalne krzywe rotacji FBX.
    static bool IsMonikaOwnerClip(ClipSpec spec)
        => spec.path.StartsWith(MonikaOwnerSource,StringComparison.Ordinal) &&
            new[]{"Brutal To Happy Walking.fbx","Running.fbx","Jump.fbx"}
                .Contains(System.IO.Path.GetFileName(spec.path));

    static AnimationClip BakeMonikaOwnRig(GameObject hero,Rest[] rest,ClipSpec spec)
    {
        if (!System.IO.File.Exists(spec.path))
            throw new InvalidOperationException("Brak oryginalnego FBX Moniki: "+spec.path);
        System.IO.Directory.CreateDirectory(MonikaDirectSources);
        string imported=MonikaDirectSources+System.IO.Path.GetFileName(spec.path);
        bool needsCopy=!System.IO.File.Exists(imported);
        if(!needsCopy)
        {
            using(var sha=System.Security.Cryptography.SHA256.Create())
            using(var a=System.IO.File.OpenRead(spec.path))
            using(var b=System.IO.File.OpenRead(imported))
                needsCopy=!sha.ComputeHash(a).SequenceEqual(sha.ComputeHash(b));
        }
        if(needsCopy)
        {
            System.IO.File.Copy(spec.path,imported,true);
            AssetDatabase.ImportAsset(imported,ImportAssetOptions.ForceSynchronousImport);
        }
        var importer=AssetImporter.GetAtPath(imported) as ModelImporter;
        if(!importer)throw new InvalidOperationException("Brak importera ruchu Moniki: "+imported);
        if(importer.animationType!=ModelImporterAnimationType.Legacy || !importer.importAnimation)
        {
            importer.animationType=ModelImporterAnimationType.Legacy;
            importer.importAnimation=true;
            importer.materialImportMode=ModelImporterMaterialImportMode.None;
            importer.SaveAndReimport();
        }
        var source=AssetDatabase.LoadAllAssetsAtPath(imported).OfType<AnimationClip>()
            .Where(v=>v && !v.name.StartsWith("__preview__") && v.length>.01f)
            .OrderByDescending(v=>AnimationUtility.GetCurveBindings(v).Length)
            .ThenByDescending(v=>v.length).FirstOrDefault();
        if(!source)
            throw new InvalidOperationException("Monika: pusty FBX Legacy "+imported);
        var bones=rest.GroupBy(r=>BoneName(r.t.name),StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g=>g.Key,g=>g.First(),StringComparer.OrdinalIgnoreCase);
        var clip=new AnimationClip {
            name=spec.name,legacy=true,frameRate=source.frameRate>0?source.frameRate:30,
            wrapMode=spec.loop?WrapMode.Loop:WrapMode.Once
        };
        var mapped=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int curves=0;
        try
        {
            foreach(var bind in AnimationUtility.GetCurveBindings(source))
            {
                if(bind.type!=typeof(Transform))continue;
                string property=bind.propertyName;
                bool rotation=property.IndexOf("Rotation",StringComparison.OrdinalIgnoreCase)>=0 ||
                    property.IndexOf("Euler",StringComparison.OrdinalIgnoreCase)>=0;
                bool position=property.IndexOf("Position",StringComparison.OrdinalIgnoreCase)>=0;
                if(!rotation&&!position)continue;
                string[] parts=(bind.path??"").Split('/');
                string name=BoneName(parts.Length>0 && !string.IsNullOrEmpty(parts[parts.Length-1])
                    ? parts[parts.Length-1] : "Hips");
                if(!bones.TryGetValue(name,out Rest target))
                    throw new InvalidOperationException("Monika: kość z animacji nie występuje w modelu: "+name+" / "+spec.name);
                if(position&&!string.Equals(name,"Hips",StringComparison.OrdinalIgnoreCase))
                    continue;
                var curve=AnimationUtility.GetEditorCurve(source,bind);
                if(curve==null)continue;
                if(position)
                {
                    int dot=property.LastIndexOf('.');
                    char axis=dot>=0 && dot+1<property.Length ? char.ToLowerInvariant(property[dot+1]) : ' ';
                    int channel=axis=='x'?0:axis=='y'?1:axis=='z'?2:-1;
                    if(channel<0)continue;
                    float original=target.p[channel];
                    if(spec.inPlace&&axis!='y')
                        curve=AnimationCurve.Constant(0f,Mathf.Max(.01f,source.length),original);
                    else
                    {
                        float first=curve.Evaluate(0f);
                        var keys=curve.keys;
                        for(int k=0;k<keys.Length;k++)
                        {
                            var value=keys[k];value.value+=original-first;keys[k]=value;
                        }
                        curve=new AnimationCurve(keys) {
                            preWrapMode=curve.preWrapMode,postWrapMode=curve.postWrapMode
                        };
                    }
                }
                var dst=bind;
                dst.path=AnimationUtility.CalculateTransformPath(target.t,hero.transform);
                AnimationUtility.SetEditorCurve(clip,dst,curve);
                mapped.Add(name);curves++;
            }
            foreach(string required in new[]{"Hips","LeftUpLeg","RightUpLeg","LeftLeg","RightLeg"})
                if(!mapped.Contains(required))
                    throw new InvalidOperationException("Monika: brak kości "+required+" w "+spec.name);
            if(mapped.Count<25)throw new InvalidOperationException(
                "Monika: za mało animowanych kości "+mapped.Count+" w "+spec.name);
            clip.EnsureQuaternionContinuity();
            Debug.Log("BRAXHORN_MONIKA_OWNER_DIRECT nazwa="+spec.name+
                " kości="+mapped.Count+" krzywe="+curves+" źródło="+imported+
                " BEZ retargetingu Humanoid, model i skóra niezmienione");
            return clip;
        }
        catch
        {
            Object.DestroyImmediate(clip);
            throw;
        }
    }

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
