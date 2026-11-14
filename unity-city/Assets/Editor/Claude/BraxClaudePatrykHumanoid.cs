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
    // Model geometry and skin bind poses never change: only animation rotations.
    // The resting toe direction distinguishes a genuine foot twist from an
    // inverted FBX forward axis.
    static Vector3 PatrykPrzodStop(List<Transform> bones)
    {
        Transform B(string name) => PatrykZnajdz(bones,name);
        var left=B("LeftUpLeg"); var rightBone=B("RightUpLeg");
        if(!left||!rightBone)return Vector3.forward;
        Vector3 r=rightBone.position-left.position; r.y=0;
        if(r.sqrMagnitude<1e-8f)return Vector3.forward;
        Vector3 f=Vector3.Cross(r.normalized,Vector3.up);
        Vector3 toes=Vector3.zero;
        foreach(string side in new[]{"Left","Right"})
        {
            var foot=B(side+"Foot");var toe=B(side+"ToeBase");
            if(!foot||!toe)continue;
            var dir=toe.position-foot.position;dir.y=0;
            if(dir.sqrMagnitude>1e-8f)toes+=dir.normalized;
        }
        if(toes.sqrMagnitude>1e-8f&&Vector3.Dot(toes,f)<0f)f=-f;
        return f.normalized;
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
        List<Transform> bones, List<PatrykStan> rest, AnimationClip clip, PatrykKlip spec, out string info)
    {
        if (!avatar || !avatar.isValid || !avatar.isHuman || !clip || !clip.humanMotion)
            throw new InvalidOperationException("Brak poprawnego avatara lub klipu Humanoid: " + spec.nazwa);
        PlayableGraph graph = default;
        var animator = hero.AddComponent<Animator>();
        try
        {
            animator.avatar = avatar;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind();
            graph = PlayableGraph.Create("Patryk Humanoid");
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var playable = AnimationClipPlayable.Create(graph, clip);
            playable.SetApplyFootIK(false);
            AnimationPlayableOutput.Create(graph, "pose", animator).SetSourcePlayable(playable);
            graph.Play();
            int count = Mathf.Max(2, Mathf.CeilToInt(clip.length * clip.frameRate) + 1);
            var times = new float[count];
            var rotations = bones.Select(_ => new Quaternion[count]).ToArray();
            var positions = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                float t = Mathf.Min(i / clip.frameRate, clip.length); times[i] = t;
                playable.SetTime(t);
                graph.Evaluate(0);
                for (int b = 0; b < bones.Count; b++)
                {
                    var q = bones[b].localRotation;
                    if (i > 0 && Quaternion.Dot(q, rotations[b][i-1]) < 0) q = new Quaternion(-q.x,-q.y,-q.z,-q.w);
                    rotations[b][i] = q;
                }
                positions[i] = hips.localPosition;
            }
            var result = new AnimationClip { name = spec.nazwa, legacy = true, frameRate = clip.frameRate,
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
            if (graph.IsValid()) graph.Destroy();
            Object.DestroyImmediate(animator); PatrykSpoczynek(rest);
        }
    }

    // Pomiar nóg w bieżącej pozie: kąt stopy (Foot→ToeBase) od kierunku bioder, zgięcie kolana ze znakiem (+ = do przodu:
    // oś udo×łydka zgodna z osią „w prawo” bioder), odchyłka osi zgięcia od osi bioder (kolano w bok).
    sealed class NogiWynik
    {
        public int klatki, stopaPonad, koloTyl, przeprost, wBok; public float stopaMax, przeprostMax, wBokMax, zgMin = float.MaxValue, zgMax = float.MinValue;
        public int Kara => stopaPonad + koloTyl + przeprost + wBok;
        public string Opis() => "klatek=" + klatki + " stopa_max=" + stopaMax.ToString("F1") + "° (>35°: " + stopaPonad + ") kolano_do_tyłu=" + koloTyl
            + " zgięcie " + (klatki > 0 ? zgMin.ToString("F0") + "…" + zgMax.ToString("F0") : "—") + "° przeprost_max=" + przeprostMax.ToString("F1") + "° (>10°: " + przeprost + ")"
            + " kolano_w_bok_max=" + wBokMax.ToString("F1") + "° (>35°: " + wBok + ")";
    }
    static void PatrykNogiKlatka(List<Transform> kosci, NogiWynik w, Vector3 originalForward)
    {
        Transform K(string n) => PatrykZnajdz(kosci, n);
        var lu = K("LeftUpLeg"); var ru = K("RightUpLeg"); if (!lu || !ru) return;
        var R = ru.position - lu.position; R.y = 0f; if (R.sqrMagnitude < 1e-8f) return; R.Normalize(); var F = Vector3.Cross(R, Vector3.up);
        if(Vector3.Dot(F,originalForward)<0f) F=-F;
        w.klatki++;
        foreach (var s in new[] { "Left", "Right" })
        {
            var up = K(s + "UpLeg"); var leg = K(s + "Leg"); var foot = K(s + "Foot"); var toe = K(s + "ToeBase");
            if (!up || !leg || !foot) continue;
            var thigh = leg.position - up.position; var shin = foot.position - leg.position;
            float bend = Vector3.Angle(thigh, shin); var os = Vector3.Cross(thigh, shin);
            // 10.11: kierunek stopy liczony tylko na nodze podporowej (kolano < 35°, łydka do 45° od pionu) — tam stopa bokiem
            // jest błędem; w fazie wymachu biegu pięta przy pośladku i stopa do tyłu to ruch prawidłowy (dawne 178,8° w Run)
            bool podpora = bend < 35f && Vector3.Angle(shin, Vector3.down) < 45f;
            if (toe && podpora)
            {
                var f = toe.position - foot.position; var fh = f; fh.y = 0f;
                if (fh.magnitude > .3f * f.magnitude) { float a = Vector3.Angle(fh, F); w.stopaMax = Mathf.Max(w.stopaMax, a); if (a > 35f) w.stopaPonad++; }
            }
            float znak = os.sqrMagnitude > 1e-12f && Vector3.Dot(os, R) < 0f ? -1f : 1f, sz = znak * bend;
            w.zgMin = Mathf.Min(w.zgMin, sz); w.zgMax = Mathf.Max(w.zgMax, sz);
            if (sz < -3f) w.koloTyl++;
            if (sz < -10f) { w.przeprost++; } if (sz < 0f) w.przeprostMax = Mathf.Max(w.przeprostMax, -sz);
            if (bend > 15f && os.sqrMagnitude > 1e-12f)
            {
                float b = Mathf.Min(Vector3.Angle(os, R), Vector3.Angle(os, -R)); w.wBokMax = Mathf.Max(w.wBokMax, b); if (b > 35f) w.wBok++;
            }
        }
    }
    static NogiWynik PatrykTestNog(GameObject hero, List<Transform> kosci, List<PatrykStan> rest, AnimationClip clip, int klatek = 16)
    {
        var w = new NogiWynik();
        PatrykSpoczynek(rest);
        Vector3 originalForward=PatrykPrzodStop(kosci);
        for (int i = 0; i < klatek; i++) { clip.SampleAnimation(hero, clip.length * i / klatek); PatrykNogiKlatka(kosci, w, originalForward); }
        PatrykSpoczynek(rest);
        return w;
    }
}
