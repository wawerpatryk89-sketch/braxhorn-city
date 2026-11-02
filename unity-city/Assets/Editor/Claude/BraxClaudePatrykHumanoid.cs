using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

public static partial class BraxClaudeOsiedle
{
    // Keep the model's relaxed bind pose. No fighting clip is used before an attack.
    static AnimationClip PatrykSpokojny(GameObject hero, List<PatrykStan> rest)
        => PatrykPozaStala(hero, rest, "Idle", false);

    static AnimationClip PatrykPozaStala(GameObject hero, List<PatrykStan> rest, string name, bool driving)
    {
        PatrykSpoczynek(rest);
        if (driving)
        {
            Transform Bone(string n) => PatrykZnajdz(rest.Select(s=>s.t),n);
            void Aim(string a, string b, Vector3 target)
            {
                var from=Bone(a);var to=Bone(b);
                if(!from||!to)throw new InvalidOperationException("Brak kości pozy kierowcy: "+a+" / "+b);
                from.rotation=Quaternion.FromToRotation(to.position-from.position,target)*from.rotation;
            }
            // Bake rotations once in the original hierarchy. Bone translations and lengths are unchanged.
            Aim("Spine","Spine1",new Vector3(0,.98f,.16f));
            foreach(var side in new[]{"Left","Right"})
            {
                float x=side=="Left"?-.08f:.08f;
                Aim(side+"UpLeg",side+"Leg",new Vector3(x,-.25f,.95f));
                Aim(side+"Leg",side+"Foot",new Vector3(0,-.86f,.48f));
                Aim(side+"Arm",side+"ForeArm",new Vector3(x,-.65f,.65f));
                Aim(side+"ForeArm",side+"Hand",new Vector3(0,.12f,.98f));
                var foot=Bone(side+"Foot");var toe=Bone(side+"ToeBase");
                if(foot&&toe)Aim(side+"Foot",side+"ToeBase",Vector3.forward);
            }
        }
        var clip = new AnimationClip { name = name, legacy = true, frameRate = 30, wrapMode = WrapMode.Loop };
        foreach (var s in rest)
        {
            string path = AnimationUtility.CalculateTransformPath(s.t, hero.transform);
            for (int axis = 0; axis < 4; axis++)
                clip.SetCurve(path, typeof(Transform), "localRotation." + "xyzw"[axis], AnimationCurve.Constant(0, 2, s.t.localRotation[axis]));
            for (int axis = 0; axis < 3; axis++)
                clip.SetCurve(path, typeof(Transform), "localPosition." + "xyz"[axis], AnimationCurve.Constant(0, 2, s.p[axis]));
        }
        clip.EnsureQuaternionContinuity();
        ZapiszKlipPatryka(clip);
        PatrykSpoczynek(rest);
        return AssetDatabase.LoadAssetAtPath<AnimationClip>(PatrykWynik + "/"+name+".anim");
    }

    static void ZapiszKlipPatryka(AnimationClip clip)
    {
        string path = PatrykWynik + "/" + clip.name + ".anim";
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (existing) { EditorUtility.CopySerialized(clip, existing); Object.DestroyImmediate(clip); }
        else AssetDatabase.CreateAsset(clip, path);
    }

    // Bake Unity's anatomical retargeting to the existing Legacy runtime. The avatar,
    // original hierarchy and bind matrices stay intact; no direction-only bone alignment.
    static AnimationClip PatrykHumanoid(GameObject hero, Avatar avatar, Transform hips,
        List<Transform> bones, List<PatrykStan> rest, string path, PatrykKlip spec, out string info)
    {
        if (!avatar || !avatar.isValid || !avatar.isHuman) throw new InvalidOperationException("Patryk: nieprawidłowy awatar Humanoid");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__preview__")).OrderByDescending(c => c.length).FirstOrDefault();
        if (!prefab || !clip || !clip.humanMotion || clip.length <= 0)
            throw new InvalidOperationException("Źródło nie jest klipem Humanoid: " + path);
        var source = Object.Instantiate(prefab);
        PlayableGraph graph = default;
        HumanPoseHandler reader = null, writer = null;
        try
        {
            source.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            source.transform.localScale = Vector3.one;
            var animator = source.GetComponent<Animator>();
            if (!animator || !animator.avatar || !animator.avatar.isValid || !animator.isHuman)
                throw new InvalidOperationException("Źródło ma nieprawidłowy awatar: " + path);
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            graph = PlayableGraph.Create("Patryk humanoid bake");
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var playable = AnimationClipPlayable.Create(graph, clip);
            playable.SetApplyFootIK(true);
            AnimationPlayableOutput.Create(graph, "pose", animator).SetSourcePlayable(playable);
            graph.Play();
            reader = new HumanPoseHandler(animator.avatar, source.transform);
            writer = new HumanPoseHandler(avatar, hero.transform);
            HumanPose pose = new HumanPose();
            Vector3 Sample(float time)
            {
                playable.SetTime(time); graph.Evaluate(0);
                reader.GetHumanPose(ref pose);
                return pose.bodyPosition;
            }
            Vector3 first = Sample(0), last = Sample(clip.length);
            Vector3 drift = spec.wMiejscu ? new Vector3(last.x-first.x, 0, last.z-first.z) : Vector3.zero;
            int count = Mathf.CeilToInt(clip.length * 30) + 1;
            var times = new float[count];
            var rotations = bones.Select(_ => new Quaternion[count]).ToArray();
            var positions = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                float t = Mathf.Min(i / 30f, clip.length); times[i] = t;
                Sample(t); pose.bodyPosition -= drift * (t / clip.length);
                PatrykSpoczynek(rest);
                writer.SetHumanPose(ref pose);
                for (int b = 0; b < bones.Count; b++)
                {
                    var q = bones[b].localRotation;
                    if (i > 0 && Quaternion.Dot(q, rotations[b][i-1]) < 0) q = new Quaternion(-q.x,-q.y,-q.z,-q.w);
                    rotations[b][i] = q;
                }
                positions[i] = hips.localPosition;
            }
            var result = new AnimationClip { name = spec.nazwa, legacy = true, frameRate = 30,
                wrapMode = spec.petla ? WrapMode.Loop : WrapMode.Once };
            for (int b = 0; b < bones.Count; b++)
            {
                string bonePath = AnimationUtility.CalculateTransformPath(bones[b], hero.transform);
                for (int axis = 0; axis < 4; axis++)
                    result.SetCurve(bonePath, typeof(Transform), "localRotation." + "xyzw"[axis],
                        PatrykKrzywaZ(times, rotations[b].Select(q => q[axis]).ToArray(), false));
            }
            string hipsPath = AnimationUtility.CalculateTransformPath(hips, hero.transform);
            for (int axis = 0; axis < 3; axis++) result.SetCurve(hipsPath, typeof(Transform), "localPosition." + "xyz"[axis],
                PatrykKrzywaZ(times, positions.Select(p => p[axis]).ToArray(), false));
            result.EnsureQuaternionContinuity();
            ZapiszKlipPatryka(result);
            info = " Unity Humanoid → Legacy, " + count + " próbek";
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(PatrykWynik + "/" + spec.nazwa + ".anim");
        }
        finally
        {
            reader?.Dispose(); writer?.Dispose();
            if (graph.IsValid()) graph.Destroy();
            Object.DestroyImmediate(source); PatrykSpoczynek(rest);
        }
    }
}
