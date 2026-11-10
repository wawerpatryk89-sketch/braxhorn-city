using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// OSIEDLE 10.11 — ruchy właściciela PROSTO na kości Patryka (właściciel 19:19: „jak można tak postać zje… — ja pod niego
// animacje zrobiłem”). Podgląd klatek z APK dd490e9 pokazał, że przeniesienie przez Unity Humanoid (HumanPoseHandler + IK stóp)
// psuje ruchy zrobione pod Patryka: lewa stopa w biegu bokiem do 89°, lewe kolano w chodzie do tyłu −22°, ramiona skręcone
// o ~50–75° (dłonie wnętrzem do przodu). Ruchy z paczki „Patryk anima(6).zip” mają ten sam szkielet Mixamo co Patryk, więc
// idą bez konwersji: w każdej klatce obrót kości w świecie T(t) = S(t)·S0⁻¹·T0 (S = kość źródła po nazwie, S0/T0 = poza
// spoczynkowa źródła/Patryka; przy identycznym szkielecie to po prostu S(t)), biodra = przesunięcie źródła × (wysokość bioder
// Patryka / źródła), w miejscu bez dryfu poziomego. Ten sam wzór liczy podgląd (scratchpad patryk_pv/rt.html).
// Kości, długości i bindposes Patryka bez zmian — tylko obroty i wysokość bioder.
public static partial class BraxClaudeOsiedle
{
    static bool PatrykRuchWlasciciela(string sciezka) => sciezka.Replace('\\', '/').Contains("/" + RuchyWlasciciela);

    static AnimationClip PatrykKlipZrodla(string path) =>
        AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__preview__")).OrderByDescending(c => c.length).FirstOrDefault();

    static Dictionary<string, Transform> PatrykKosciPoNazwie(IEnumerable<Transform> ts)
    {
        var d = new Dictionary<string, Transform>();
        foreach (var t in ts) { var k = PatrykKosc(t.name); if (!d.ContainsKey(k)) d[k] = t; }
        return d;
    }

    /// <summary>Ruch właściciela (Generic, ten sam szkielet) → klip Legacy Patryka bez konwersji Humanoid.</summary>
    static AnimationClip PatrykBezposrednio(GameObject hero, Transform hips, List<Transform> bones, List<PatrykStan> rest,
        string path, PatrykKlip spec, out string info)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var clip = PatrykKlipZrodla(path);
        if (!prefab || !clip || clip.length <= 0f) throw new InvalidOperationException("Brak klipu w " + path);
        if (clip.humanMotion) throw new InvalidOperationException("Ruch właściciela zaimportowany jako Humanoid (oczekiwany Generic): " + path);
        var source = Object.Instantiate(prefab);
        try
        {
            source.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity); source.transform.localScale = Vector3.one;
            foreach (var a in source.GetComponentsInChildren<Animator>(true)) a.enabled = false;
            var src = PatrykKosciPoNazwie(source.GetComponentsInChildren<Transform>(true));
            if (!src.TryGetValue("Hips", out var sHips) || !src.TryGetValue("LeftFoot", out var sFoot))
                throw new InvalidOperationException("Źródło bez kości Hips/LeftFoot: " + path);
            var S0 = src.ToDictionary(p => p.Key, p => p.Value.rotation);   // poza spoczynkowa źródła (przed próbkowaniem)
            Vector3 sh0 = sHips.position; float hSrc = sh0.y - sFoot.position.y;

            PatrykSpoczynek(rest);
            var tFoot = PatrykZnajdz(bones, "LeftFoot");
            if (!tFoot) throw new InvalidOperationException("Patryk bez kości LeftFoot");
            var T0 = bones.ToDictionary(b => b, b => b.rotation);
            Vector3 hipsRest = hips.localPosition; float hTgt = hips.position.y - tFoot.position.y;
            float skala = hSrc > 1e-5f ? hTgt / hSrc : 1f;

            int count = Mathf.CeilToInt(clip.length * 30f) + 1, dopasowane = 0;
            var times = new float[count];
            var rotations = bones.Select(_ => new Quaternion[count]).ToArray();
            var positions = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                float t = Mathf.Min(i / 30f, clip.length); times[i] = t;
                clip.SampleAnimation(source, t);
                PatrykSpoczynek(rest);
                int n = 0;
                foreach (var b in bones)    // kolejność z GetComponentsInChildren: rodzic przed dzieckiem
                {
                    if (!src.TryGetValue(PatrykKosc(b.name), out var s) || !S0.TryGetValue(PatrykKosc(b.name), out var s0)) continue;
                    b.rotation = s.rotation * Quaternion.Inverse(s0) * T0[b]; n++;
                }
                if (i == 0) dopasowane = n;
                var d = (sHips.position - sh0) * skala;
                if (spec.wMiejscu) { d.x = 0f; d.z = 0f; }
                hips.localPosition = hipsRest + (hips.parent ? hips.parent.InverseTransformVector(d) : d);
                for (int b = 0; b < bones.Count; b++)
                {
                    var q = bones[b].localRotation;
                    if (i > 0 && Quaternion.Dot(q, rotations[b][i - 1]) < 0) q = new Quaternion(-q.x, -q.y, -q.z, -q.w);
                    rotations[b][i] = q;
                }
                positions[i] = hips.localPosition;
            }
            if (dopasowane < 40) throw new InvalidOperationException("Szkielet źródła nie pasuje do Patryka: " + dopasowane + " kości wspólnych (" + path + ")");
            var result = new AnimationClip { name = spec.nazwa, legacy = true, frameRate = 30, wrapMode = spec.petla ? WrapMode.Loop : WrapMode.Once };
            for (int b = 0; b < bones.Count; b++)
            {
                string bonePath = AnimationUtility.CalculateTransformPath(bones[b], hero.transform);
                for (int axis = 0; axis < 4; axis++)
                    result.SetCurve(bonePath, typeof(Transform), "localRotation." + "xyzw"[axis], PatrykKrzywaZ(times, rotations[b].Select(q => q[axis]).ToArray(), false));
            }
            string hipsPath = AnimationUtility.CalculateTransformPath(hips, hero.transform);
            for (int axis = 0; axis < 3; axis++)
                result.SetCurve(hipsPath, typeof(Transform), "localPosition." + "xyz"[axis], PatrykKrzywaZ(times, positions.Select(p => p[axis]).ToArray(), false));
            result.EnsureQuaternionContinuity();
            ZapiszKlipPatryka(result);
            info = " bezpośrednio na kości Patryka (bez Humanoid), " + count + " próbek, kości wspólnych " + dopasowane + ", biodra ×" + skala.ToString("F3");
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(PatrykWynik + "/" + spec.nazwa + ".anim");
        }
        finally { Object.DestroyImmediate(source); PatrykSpoczynek(rest); }
    }

    static float PatrykKatWokol(Vector3 a, Vector3 b, Vector3 n)   // kąt ze znakiem (radiany) od a do b wokół osi n, po rzucie na płaszczyznę
    {
        a -= Vector3.Dot(a, n) * n; b -= Vector3.Dot(b, n) * n;
        if (a.sqrMagnitude < 1e-12f || b.sqrMagnitude < 1e-12f) return 0f;
        return Mathf.Atan2(Vector3.Dot(Vector3.Cross(a, b), n), Vector3.Dot(a, b));
    }

    /// <summary>Spokojna poza (Idle, z kodu — w paczce właściciela nie ma stania): dłonie wnętrzem do ciała, kciuk do przodu.
    /// APK dd490e9: kciuk 73–77° na zewnątrz = dłonie odwrócone do przodu. Ramię obracane wokół osi bark–dłoń (dłoń zostaje),
    /// reszta na przedramię (60 %) i dłoń (40 %), maks. ±50°.</summary>
    static void PatrykRece(List<Transform> bones, Vector3 przod0)
    {
        Transform K(string n) => PatrykZnajdz(bones, n);
        foreach (var s in new[] { "Left", "Right" })
        {
            var lu = K("LeftUpLeg"); var ru = K("RightUpLeg");
            var arm = K(s + "Arm"); var fore = K(s + "ForeArm"); var hand = K(s + "Hand"); var kciuk = K(s + "HandThumb1");
            if (!lu || !ru || !arm || !fore || !hand || !kciuk) continue;
            var Rr = ru.position - lu.position; Rr.y = 0f; if (Rr.sqrMagnitude < 1e-10f) continue; Rr.Normalize();
            var F = Vector3.Cross(Rr, Vector3.up); if (Vector3.Dot(F, przod0) < 0f) F = -F;
            var o = s == "Right" ? Rr : -Rr;
            Vector3 S = arm.position, E = fore.position, Hn = hand.position;
            var sh = Hn - S; if (sh.sqrMagnitude < 1e-10f) continue; var u = sh.normalized;
            float zg = Vector3.Angle(E - S, Hn - E), w = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(10f, 25f, zg));
            float al = w * PatrykKatWokol(E - S, -F + .35f * o, u) + (1f - w) * PatrykKatWokol(kciuk.position - Hn, F + .5f * Vector3.up, u);
            arm.rotation = Quaternion.AngleAxis(al * Mathf.Rad2Deg, u) * arm.rotation;
            E = fore.position; Hn = hand.position; var fa = Hn - E; if (fa.sqrMagnitude < 1e-10f) continue; var ax = fa.normalized;
            float be = Mathf.Clamp(PatrykKatWokol(kciuk.position - Hn, F + .5f * Vector3.up, ax), -50f * Mathf.Deg2Rad, 50f * Mathf.Deg2Rad);
            fore.rotation = Quaternion.AngleAxis(.6f * be * Mathf.Rad2Deg, ax) * fore.rotation;
            hand.rotation = Quaternion.AngleAxis(.4f * be * Mathf.Rad2Deg, ax) * hand.rotation;
        }
    }
}
