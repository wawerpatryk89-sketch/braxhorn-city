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
        Transform Bone(string n) => PatrykZnajdz(rest.Select(s=>s.t),n);
        void Aim(string a, string b, Vector3 target)
        {
            var from=Bone(a);var to=Bone(b);
            if(!from||!to)throw new InvalidOperationException("Brak kości pozy: "+a+" / "+b);
            from.rotation=Quaternion.FromToRotation(to.position-from.position,target)*from.rotation;
        }
        if (driving)
        {
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
        else
        {
            var left=Bone("LeftArm"); var right=Bone("RightArm");
            if(!left||!right)throw new InvalidOperationException("Brak ramion nowego Patryka");
            Vector3 rightAxis=Vector3.ProjectOnPlane(right.position-left.position,Vector3.up).normalized;
            Vector3 forward=Vector3.Cross(rightAxis,Vector3.up);
            foreach(var side in new[]{"Left","Right"})
            {
                Vector3 outward=rightAxis*(side=="Right"?1f:-1f);
                Aim(side+"Arm",side+"ForeArm",Vector3.down+outward*.18f);
                Aim(side+"ForeArm",side+"Hand",Vector3.down+outward*.10f+forward*.12f);
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
        List<Transform> bones, List<PatrykStan> rest, string path, PatrykKlip spec, out string info, bool zapisz = true)
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
            PatrykSpoczynek(rest);
            HumanPose targetRest = new HumanPose();
            writer.GetHumanPose(ref targetRest);
            Vector3 hipsRest = hips.localPosition;
            HumanPose pose = new HumanPose();
            Vector3 Sample(float time)
            {
                playable.SetTime(time); graph.Evaluate(0);
                reader.GetHumanPose(ref pose);
                return pose.bodyPosition;
            }
            Vector3 first = Sample(0);
            int count = Mathf.CeilToInt(clip.length * 30) + 1;
            var times = new float[count];
            var rotations = bones.Select(_ => new Quaternion[count]).ToArray();
            var positions = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                float t = Mathf.Min(i / 30f, clip.length); times[i] = t;
                Vector3 motion = Sample(t) - first;
                if (spec.wMiejscu) { motion.x = 0; motion.z = 0; }
                pose.bodyPosition = targetRest.bodyPosition + motion;
                PatrykSpoczynek(rest);
                writer.SetHumanPose(ref pose);
                if(spec.wMiejscu) hips.localPosition=new Vector3(hipsRest.x,hips.localPosition.y,hipsRest.z);
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
            if (!zapisz) { info = " (próba, bez zapisu)"; return result; }
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

    // ---------------- E2 (build 45): awatar z WYMUSZONEJ T-pozy ----------------
    // Nagranie #44: kolana i stopy wykręcone w ruchu. Awatar z importu powstał z pozy modelu (ramiona 24° od pionu — swobodna,
    // nie T): Unity liczy osie mięśni od pozy odniesienia, więc z pozy nie-T mięśnie nóg i rąk przenoszą się z błędem (skręt uda,
    // stopy na boki). Tu: kopia hierarchii ustawiona w T-pozie (nogi pionowo, cała noga obrócona wokół pionu tak, by stopa szła
    // do przodu, stopa z zachowanym nachyleniem, ramiona i przedramiona poziomo w bok, kciuk do przodu = dłoń w dół),
    // HumanDescription.skeleton z tej pozy, to samo mapowanie kości (human) co z importu. Szkielet i długości kości bez zmian —
    // obracamy tylko kopię do pomiaru pozy odniesienia; Patryk w grze ma oryginalne kości i bindposes.
    static Avatar PatrykAwatarTPoza(GameObject wzor, Avatar importowany, string model, out string opis)
    {
        HumanDescription hd = default; bool ma = false;
        try { hd = importowany.humanDescription; ma = hd.human != null && hd.human.Length > 0; } catch { ma = false; }
        if (!ma) { var mi = AssetImporter.GetAtPath(model) as ModelImporter; if (mi != null) { hd = mi.humanDescription; ma = hd.human != null && hd.human.Length > 0; } }
        if (!ma) throw new InvalidOperationException("brak mapowania kości Humanoid (humanDescription) dla " + model);
        var kopia = Object.Instantiate(wzor); kopia.name = "Patryk";
        try
        {
            kopia.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity); kopia.transform.localScale = Vector3.one;
            var wszystkie = kopia.GetComponentsInChildren<Transform>(true);
            var mapa = new Dictionary<string, string>();
            foreach (var h in hd.human) if (!string.IsNullOrEmpty(h.humanName) && !string.IsNullOrEmpty(h.boneName)) mapa[h.humanName] = h.boneName;
            Transform B(string human) => mapa.TryGetValue(human, out var n) ? wszystkie.FirstOrDefault(t => t.name == n) : null;
            void Aim(Transform a, Transform b, Vector3 cel)
            {
                if (!a || !b) return; var d = b.position - a.position; if (d.sqrMagnitude < 1e-10f || cel.sqrMagnitude < 1e-10f) return;
                a.rotation = Quaternion.FromToRotation(d, cel) * a.rotation;
            }
            var lUp = B("LeftUpperLeg"); var rUp = B("RightUpperLeg");
            if (!lUp || !rUp) throw new InvalidOperationException("brak kości ud w mapowaniu");
            var prawo = rUp.position - lUp.position; prawo.y = 0f;
            if (prawo.sqrMagnitude < 1e-8f) throw new InvalidOperationException("uda w jednym punkcie");
            prawo.Normalize(); var przod = Vector3.Cross(prawo, Vector3.up);
            float stopyPrzed = 0f, ramionaPrzed = 0f; int ns = 0, nr = 0;
            foreach (var s in new[] { "Left", "Right" })
            {
                var up = B(s + "UpperLeg"); var lo = B(s + "LowerLeg"); var foot = B(s + "Foot"); var toes = B(s + "Toes");
                if (foot && toes) { var f0 = toes.position - foot.position; f0.y = 0; if (f0.sqrMagnitude > 1e-8f) { stopyPrzed += Vector3.Angle(f0, przod); ns++; } }
                Aim(up, lo, Vector3.down); Aim(lo, foot, Vector3.down);
                if (up && foot && toes)
                {   // cała noga (wyprostowana, oś pionowa) obrócona wokół pionu: stopa do przodu — kolano zgina się w płaszczyźnie ruchu
                    var f = toes.position - foot.position; f.y = 0f;
                    if (f.sqrMagnitude > 1e-8f) up.rotation = Quaternion.AngleAxis(Vector3.SignedAngle(f, przod, Vector3.up), Vector3.up) * up.rotation;
                    var g = toes.position - foot.position; float dy = Mathf.Clamp(g.normalized.y, -.95f, .95f);
                    Aim(foot, toes, przod * Mathf.Sqrt(1f - dy * dy) + Vector3.up * dy);
                }
                var arm = B(s + "UpperArm"); var fore = B(s + "LowerArm"); var hand = B(s + "Hand");
                if (arm && fore) { ramionaPrzed += Vector3.Angle(fore.position - arm.position, Vector3.down); nr++; }
                var bok = s == "Left" ? -prawo : prawo;
                Aim(arm, fore, bok); Aim(fore, hand, bok);
                var mid = B(s + "MiddleProximal"); if (hand && mid) Aim(hand, mid, bok);
                var kciuk = B(s + "ThumbProximal");
                if (arm && hand && kciuk)
                {   // skręt ramienia wokół jego osi: kciuk do przodu (dłoń w dół, jak T-poza Mixamo / Unity)
                    var k = Vector3.ProjectOnPlane(kciuk.position - hand.position, bok);
                    if (k.sqrMagnitude > 1e-8f) arm.rotation = Quaternion.AngleAxis(Vector3.SignedAngle(k, przod, bok), bok) * arm.rotation;
                }
            }
            var szk = new List<SkeletonBone>();
            foreach (var t in wszystkie) szk.Add(new SkeletonBone { name = t.name, position = t.localPosition, rotation = t.localRotation, scale = t.localScale });
            hd.skeleton = szk.ToArray();
            var av = AvatarBuilder.BuildHumanAvatar(kopia, hd);
            if (!av || !av.isValid || !av.isHuman) throw new InvalidOperationException("AvatarBuilder: awatar nieprawidłowy");
            av.name = "Patryk (T-poza wymuszona)";
            opis = "T-poza: stopy przed " + (ns > 0 ? (stopyPrzed / ns).ToString("F0") : "?") + "° od przodu → 0°, ramiona przed " + (nr > 0 ? (ramionaPrzed / nr).ToString("F0") : "?") + "° od pionu → 90°, kości " + szk.Count;
            return av;
        }
        finally { Object.DestroyImmediate(kopia); }
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
    static void PatrykNogiKlatka(List<Transform> kosci, NogiWynik w)
    {
        Transform K(string n) => PatrykZnajdz(kosci, n);
        var lu = K("LeftUpLeg"); var ru = K("RightUpLeg"); if (!lu || !ru) return;
        var R = ru.position - lu.position; R.y = 0f; if (R.sqrMagnitude < 1e-8f) return; R.Normalize(); var F = Vector3.Cross(R, Vector3.up);
        w.klatki++;
        foreach (var s in new[] { "Left", "Right" })
        {
            var up = K(s + "UpLeg"); var leg = K(s + "Leg"); var foot = K(s + "Foot"); var toe = K(s + "ToeBase");
            if (!up || !leg || !foot) continue;
            if (toe)
            {
                var f = toe.position - foot.position; var fh = f; fh.y = 0f;
                if (fh.magnitude > .3f * f.magnitude) { float a = Vector3.Angle(fh, F); w.stopaMax = Mathf.Max(w.stopaMax, a); if (a > 35f) w.stopaPonad++; }
            }
            var thigh = leg.position - up.position; var shin = foot.position - leg.position;
            float bend = Vector3.Angle(thigh, shin); var os = Vector3.Cross(thigh, shin);
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
        for (int i = 0; i < klatek; i++) { clip.SampleAnimation(hero, clip.length * i / klatek); PatrykNogiKlatka(kosci, w); }
        PatrykSpoczynek(rest);
        return w;
    }

    // Wybór awatara: próbne przeniesienie Walk i Run oboma (bez zapisu) — wygrywa mniejsza liczba naruszeń (remis: T-poza).
    static Avatar PatrykWybierzAwatar(GameObject hero, Avatar importowany, Avatar tPoza, Transform hips, List<Transform> kosci, List<PatrykStan> rest, out string raport)
    {
        var linie = new List<string>(); int karaI = 0, karaT = 0;
        foreach (var k in KlipyPatryka.Where(q => q.nazwa == "Walk" || q.nazwa == "Run"))
        {
            string zr = k.zrodla.Select(z => PatrykRuchy + "/" + z).FirstOrDefault(System.IO.File.Exists); if (zr == null) continue;
            foreach (var (av, nazwa) in new[] { (importowany, "import"), (tPoza, "T-poza") })
            {
                if (!av) continue;
                AnimationClip c = null;
                try
                {
                    c = PatrykHumanoid(hero, av, hips, kosci, rest, zr, k, out _, false);
                    var w = PatrykTestNog(hero, kosci, rest, c);
                    if (nazwa == "import") karaI += w.Kara; else karaT += w.Kara;
                    linie.Add(k.nazwa + "/" + nazwa + ": " + w.Opis());
                }
                catch (Exception e) { linie.Add(k.nazwa + "/" + nazwa + ": błąd " + e.Message); if (nazwa == "import") karaI += 1000; else karaT += 1000; }
                finally { if (c) Object.DestroyImmediate(c); PatrykSpoczynek(rest); }
            }
        }
        var wybor = tPoza && karaT <= karaI ? tPoza : importowany;
        raport = "wybór=" + (wybor == tPoza ? "T-poza" : "import") + " naruszeń import=" + karaI + " T-poza=" + (tPoza ? karaT.ToString() : "—") + " | " + string.Join(" | ", linie);
        return wybor;
    }
}
