using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

// NOGI PATRYKA OD NOWA (właściciel 3.11 14:59: „zwalasz te animacje, robisz od nowa” — build 6142ac2: stopy obrócone do 156–174°,
// kolana do tyłu w chodzie i biegu przy obu awatarach Humanoid). Druga metoda przenoszenia, bez awatara: RÓŻNICA OD POZY
// SPOCZYNKOWEJ w przestrzeni świata. Patryk jest najpierw wyrównany do T-pozy źródła (kierunek każdej kości kończyn i kręgosłupa
// = kierunek tej samej kości źródła w jego T-pozie, skręt własny Patryka zostaje), potem w każdej klatce:
//   obrót kości Patryka = A · (obrót źródła(t) · obrót źródła w T-pozie⁻¹) · A⁻¹ · obrót kości Patryka w T-pozie,
// gdzie A obraca układ źródła (przód z bioder) na układ Patryka. Inne osie lokalne kości (szkielet Tripo vs Mixamo) przestają
// mieć znaczenie, bo przenoszony jest tylko ruch względem pozy spoczynkowej. Biodra: przesunięcie źródła × (wysokość bioder
// Patryka / źródła). Dla każdego klipu liczone są OBA warianty (Humanoid i różnica) i zostaje ten z mniejszą liczbą naruszeń
// testu nóg (stopa > 35° od przodu, kolano do tyłu, przeprost, kolano w bok) — log BRAXHORN_PATRYK_NOGI klip=… wybór=….
public static partial class BraxClaudeOsiedle
{
    static readonly (string kosc, string dziecko)[] PatrykLancuchy =
    {
        ("Spine", "Spine1"), ("Spine1", "Spine2"), ("Spine2", "Neck"), ("Neck", "Head"),
        ("LeftShoulder", "LeftArm"), ("LeftArm", "LeftForeArm"), ("LeftForeArm", "LeftHand"),
        ("RightShoulder", "RightArm"), ("RightArm", "RightForeArm"), ("RightForeArm", "RightHand"),
        ("LeftUpLeg", "LeftLeg"), ("LeftLeg", "LeftFoot"), ("LeftFoot", "LeftToeBase"),
        ("RightUpLeg", "RightLeg"), ("RightLeg", "RightFoot"), ("RightFoot", "RightToeBase"),
    };

    static Quaternion PatrykUklad(Func<string, Transform> k)
    {
        var l = k("LeftUpLeg"); var r = k("RightUpLeg");
        if (!l || !r) return Quaternion.identity;
        var R = r.position - l.position; R.y = 0f; if (R.sqrMagnitude < 1e-8f) return Quaternion.identity;
        return Quaternion.LookRotation(Vector3.Cross(R.normalized, Vector3.up), Vector3.up);
    }

    static AnimationClip PatrykRoznica(GameObject hero, Transform hips, List<Transform> bones, List<PatrykStan> rest, string path, PatrykKlip spec, out string info)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__preview__")).OrderByDescending(c => c.length).FirstOrDefault();
        if (!prefab || !clip || clip.length <= 0) throw new InvalidOperationException("brak klipu: " + path);
        var source = Object.Instantiate(prefab);
        PlayableGraph graph = default;
        try
        {
            source.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity); source.transform.localScale = Vector3.one;
            var zr = new Dictionary<string, Transform>();
            foreach (var t in source.GetComponentsInChildren<Transform>(true)) { var n = PatrykKosc(t.name); if (!zr.ContainsKey(n)) zr[n] = t; }
            Transform Z(string n) => zr.TryGetValue(n, out var t) ? t : null;
            Transform C(string n) => PatrykZnajdz(bones, n);
            var pary = bones.Select(b => (cel: b, zrodlo: Z(PatrykKosc(b.name)))).Where(p => p.zrodlo).ToList();
            if (pary.Count < 15 || !Z("Hips")) throw new InvalidOperationException("za mało wspólnych kości (" + pary.Count + ")");
            // T-poza źródła = poza z pliku przed odtwarzaniem
            var zrRest = pary.ToDictionary(p => p.cel, p => p.zrodlo.rotation);
            var zrHips = Z("Hips"); Vector3 zrHipsRest = zrHips.position;
            PatrykSpoczynek(rest);
            Quaternion A = PatrykUklad(C) * Quaternion.Inverse(PatrykUklad(Z)), Ai = Quaternion.Inverse(A);
            // Patryk wyrównany do T-pozy źródła (rodzic przed dzieckiem)
            foreach (var (k, d) in PatrykLancuchy)
            {
                var b = C(k); var bc = C(d); var s = Z(k); var sc = Z(d);
                if (!b || !bc || !s || !sc) continue;
                var cel = A * (sc.position - s.position); var jest = bc.position - b.position;
                if (cel.sqrMagnitude < 1e-10f || jest.sqrMagnitude < 1e-10f) continue;
                b.rotation = Quaternion.FromToRotation(jest, cel) * b.rotation;
            }
            var celRest = pary.ToDictionary(p => p.cel, p => p.cel.rotation);
            Vector3 hipsRest = hips.position;
            float stopy = Mathf.Min(C("LeftFoot") ? C("LeftFoot").position.y : hipsRest.y, C("RightFoot") ? C("RightFoot").position.y : hipsRest.y);
            float skala = Mathf.Max(.05f, hipsRest.y - stopy) / Mathf.Max(.05f, zrHipsRest.y - Mathf.Min(Z("LeftFoot") ? Z("LeftFoot").position.y : 0f, Z("RightFoot") ? Z("RightFoot").position.y : 0f));
            // odtwarzanie źródła: Humanoid przez Animator, inaczej SampleAnimation
            var animator = source.GetComponent<Animator>(); AnimationClipPlayable playable = default;
            bool human = clip.humanMotion && animator && animator.avatar && animator.isHuman;
            if (human)
            {
                animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                graph = PlayableGraph.Create("Patryk różnica"); graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                playable = AnimationClipPlayable.Create(graph, clip); playable.SetApplyFootIK(true);
                AnimationPlayableOutput.Create(graph, "pose", animator).SetSourcePlayable(playable); graph.Play();
            }
            void Probka(float t) { if (human) { playable.SetTime(t); graph.Evaluate(0); } else clip.SampleAnimation(source, t); }
            Probka(0f); Vector3 p0 = zrHips.position; Probka(clip.length); Vector3 p1 = zrHips.position;
            Vector3 dryf = spec.wMiejscu ? new Vector3(p1.x - p0.x, 0f, p1.z - p0.z) : Vector3.zero;
            int count = Mathf.CeilToInt(clip.length * 30) + 1;
            var times = new float[count]; var rot = bones.Select(_ => new Quaternion[count]).ToArray(); var pos = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                float t = Mathf.Min(i / 30f, clip.length); times[i] = t; Probka(t);
                foreach (var (b, s) in pary) b.rotation = A * (s.rotation * Quaternion.Inverse(zrRest[b])) * Ai * celRest[b];
                var d = A * ((zrHips.position - zrHipsRest) - dryf * (t / clip.length)) * skala;
                hips.position = hipsRest + d;
                for (int b = 0; b < bones.Count; b++)
                {
                    var q = bones[b].localRotation;
                    if (i > 0 && Quaternion.Dot(q, rot[b][i - 1]) < 0) q = new Quaternion(-q.x, -q.y, -q.z, -q.w);
                    rot[b][i] = q;
                }
                pos[i] = hips.localPosition;
            }
            var result = new AnimationClip { name = spec.nazwa, legacy = true, frameRate = 30, wrapMode = spec.petla ? WrapMode.Loop : WrapMode.Once };
            for (int b = 0; b < bones.Count; b++)
            {
                string bp = AnimationUtility.CalculateTransformPath(bones[b], hero.transform);
                for (int axis = 0; axis < 4; axis++)
                    result.SetCurve(bp, typeof(Transform), "localRotation." + "xyzw"[axis], PatrykKrzywaZ(times, rot[b].Select(q => q[axis]).ToArray(), false));
            }
            string hp = AnimationUtility.CalculateTransformPath(hips, hero.transform);
            for (int axis = 0; axis < 3; axis++) result.SetCurve(hp, typeof(Transform), "localPosition." + "xyz"[axis], PatrykKrzywaZ(times, pos.Select(p => p[axis]).ToArray(), false));
            result.EnsureQuaternionContinuity();
            info = " różnica od T-pozy (kości wspólne " + pary.Count + ", skala bioder " + skala.ToString("F2") + ", " + count + " próbek)";
            return result;
        }
        finally
        {
            if (graph.IsValid()) graph.Destroy();
            Object.DestroyImmediate(source); PatrykSpoczynek(rest);
        }
    }
}
