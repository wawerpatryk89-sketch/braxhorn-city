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

    // Patryk w źródłowym FBX ma T-pose (~83° od pionu) — nie używaj jej
    // jako Idle, bo wygląda jak nieruchomy posąg. Oryginalny klip chodu
    // właściciela dostarcza naturalne pozy; wybieramy jej najspokojniejszą
    // klatkę (bez retargetu, bez edycji siatki/kości i bez cudzych animacji).
    static AnimationClip PatrykSpokojny(GameObject hero, List<PatrykStan> rest, AnimationClip wlascicielWalk)
    {
        if (!wlascicielWalk || wlascicielWalk.length <= 0f)
            throw new InvalidOperationException("Patryk Idle: brak oryginalnego Strut Walking właściciela");
        var bones = rest.Select(x => x.t).ToList();
        Transform B(string name) => PatrykZnajdz(bones, name);
        var lu=B("LeftUpLeg"); var ll=B("LeftLeg"); var lf=B("LeftFoot");
        var ru=B("RightUpLeg");var rl=B("RightLeg");var rf=B("RightFoot");
        if (!lu||!ll||!lf||!ru||!rl||!rf)
            throw new InvalidOperationException("Patryk Idle: niekompletny oryginalny szkielet nóg");
        float best = float.PositiveInfinity, time = 0f;
        float bestArms = float.PositiveInfinity, bestLegs = float.PositiveInfinity;
        for (int i=0; i<60; i++)
        {
            PatrykSpoczynek(rest);
            float t = wlascicielWalk.length * i / 60f;
            wlascicielWalk.SampleAnimation(hero, t);
            float arms = PatrykKatRamion(bones);
            float legs = (Vector3.Angle(ll.position-lu.position,Vector3.down)
                         +Vector3.Angle(lf.position-ll.position,Vector3.down)
                         +Vector3.Angle(rl.position-ru.position,Vector3.down)
                         +Vector3.Angle(rf.position-rl.position,Vector3.down))*.25f;
            float feet = Mathf.Abs(lf.position.y-rf.position.y);
            float score = arms*1.3f + legs*.7f + feet*75f;
            if (score < best) { best=score; time=t; bestArms=arms; bestLegs=legs; }
        }
        // Gdy żaden fragment własnego chodu nie daje spokojnej pozycji,
        // naprawić klip — nie wystawiać z powrotem T-pose.
        if (bestArms > 45f || bestLegs > 48f)
            throw new InvalidOperationException("Patryk Idle: oryginalny Walk nie daje naturalnej pozy, " +
                "ramiona=" + bestArms.ToString("F1") + "°, nogi=" + bestLegs.ToString("F1") + "°");
        PatrykSpoczynek(rest);
        wlascicielWalk.SampleAnimation(hero, time);
        try
        {
            var idle = PatrykZapiszWlasnaPoze(hero,rest,"Idle");
            Debug.Log("BRAXHORN_PATRYK_IDLE_OWNER: nieruchoma klatka z oryginalnego Strut Walking t="+
                time.ToString("F3") + " ramiona="+bestArms.ToString("F1")+"° nogi="+bestLegs.ToString("F1")+"°");
            return idle;
        }
        finally { PatrykSpoczynek(rest); }
    }

    // Kierowca: ostatnia poza z ORYGINALNEGO FBX "Entering Car" Patryka.
    // Nie generuj sztucznego siadu przez Aim()/obroty kości. Należy
    // osobno skontrolować na filmie, że końcowa poza odpowiada fotelowi.
    static AnimationClip PatrykCarIdleZWlasnegoWejscia(GameObject hero,
        List<PatrykStan> rest, AnimationClip wejscie)
    {
        if (!wejscie || wejscie.length <= 0f)
            throw new InvalidOperationException("Patryk: brak oryginalnego Entering Car dla CarIdle");
        PatrykSpoczynek(rest);
        try
        {
            wejscie.SampleAnimation(hero, Mathf.Max(0f, wejscie.length - 1f / 60f));
            var result = PatrykZapiszWlasnaPoze(hero, rest, "CarIdle");
            Debug.Log("BRAXHORN_PATRYK_CAR_IDLE: ostatnia poza oryginalnego Entering Car, bez sztucznego Aim/retargetu");
            return result;
        }
        finally { PatrykSpoczynek(rest); }
    }

    static AnimationClip PatrykZapiszWlasnaPoze(GameObject hero,
        List<PatrykStan> rest, string name)
    {
        var clip = new AnimationClip { name = name, legacy = true, frameRate = 30, wrapMode = WrapMode.Loop };
        foreach (var s in rest)
        {
            string path = AnimationUtility.CalculateTransformPath(s.t, hero.transform);
            for (int axis = 0; axis < 4; axis++)
                clip.SetCurve(path, typeof(Transform), "localRotation." + "xyzw"[axis],
                    AnimationCurve.Constant(0f, 2f, s.t.localRotation[axis]));
            for (int axis = 0; axis < 3; axis++)
                clip.SetCurve(path, typeof(Transform), "localPosition." + "xyz"[axis],
                    AnimationCurve.Constant(0f, 2f, s.t.localPosition[axis]));
        }
        clip.EnsureQuaternionContinuity();
        ZapiszKlipPatryka(clip);
        return AssetDatabase.LoadAssetAtPath<AnimationClip>(PatrykWynik + "/" + name + ".anim");
    }

    static void ZapiszKlipPatryka(AnimationClip clip)
    {
        string path = PatrykWynik + "/" + clip.name + ".anim";
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (existing) { EditorUtility.CopySerialized(clip, existing); Object.DestroyImmediate(clip); }
        else AssetDatabase.CreateAsset(clip, path);
    }

    // Ruchy pochodzą z sześciu FBX dostarczonych razem z MODELEM Patryka.
    // Nie wykonujemy ponownego HumanPoseHandler/Animator retargetingu — to on
    // produkował wykręcone kolana przy chodzeniu w #93. Kopie są prywatne, lokalne
    // dla generowania osiedla; nie zmieniamy oryginalnych importów i innych postaci.
    static readonly string PatrykBezRetargetu = PrywatneZrodla + "/patryk_owner_direct";
    // Bezwarunkowo 22 dokładne pliki FBX z Patryk anima(6).zip.
    // SHA-256 chronią przed przypadkową podmianą na inny klip Mixamo.
    static readonly Dictionary<string, string> Patryk22Hashes = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        { "Entering Car.fbx", "6434eb2b74763c6ff6f7b1a62073dd9f6894ce2dd2ee58c9552325e1dc5d04bb" },
        { "Goalkeeper Diving Save.fbx", "1fc12271df529b15b399d2e696d38fdc04c87f11ccac4335f0675d3a01f3d09d" },
        { "Goalkeeper Catch.fbx", "b3a5649e0dad2ef55b28e9999fce6a48929147d9b9753b0449949695e11aabf4" },
        { "Jump.fbx", "eb7a754cadfe2fe2cd2af9c0ab75488a3b29a39fbee01eb1b7d9ac9da9a10776" },
        { "Strut Walking.fbx", "a87c79f903222e09fc6b56ef11d7154715cb5d58825432d816f1b6061422cb67" },
        { "Fast Run.fbx", "2bb5d20016a64fd95ea2c0733960b6133ccd7468870449cde8bc9200303f190f" },
        { "Female Sitting Pose.fbx", "9239183ef24c1d6e034a95bf133de0961e98e41db89c42175f84e112bf2a87df" },
        { "Male Sitting Pose.fbx", "f638b939b39fbc3a48b3910b3ae326b8a2cbf60941edab3b8d09e23e285cefea" },
        { "Angry Gesture.fbx", "351118baa3a8b04138d4d86cff2ae96b3e55c8fa6c4d7c2b5ccce8316e20c2a2" },
        { "Sit To Stand.fbx", "1d337c91ba86ee75600bc386874e7c6f6e90ee9a477a7aab2b5278ebc9d3bfc0" },
        { "Sitting Disbelief.fbx", "cbc5c2788f6be3360ed8c4b474789300236c8efce03ff30c271e142aaac083d4" },
        { "Sitting.fbx", "ef509c47a3379cd0efff273a9938ad0c4bed6b92c4bb86ca03de8e804621fe51" },
        { "Typing.fbx", "3ab1ca71da703d155a2ed5e746abd354cd06cb01b36e8cfbf4b1638b0d2e358d" },
        { "Drinking.fbx", "520bd16016b4ecfaf90019ed3ef926216ee6fc961a2a87b587a137d9e93bfe53" },
        { "Dribble.fbx", "7cb078e63db839e478184eb3b9a69e94c3fd70953892e652cb3fa029d2065f09" },
        { "Goalkeeper Placing Ball (1).fbx", "55751ccec9abaaf70505a2d48280172913dfe2b0796f71f45e0c8442c656b882" },
        { "Header Soccerball.fbx", "ebdee23493920f90a6b686db99b0ee258a6086f63b1dcac54c69b1811dd10962" },
        { "Goalkeeper Drop Kick.fbx", "4797374117d125536c109579a94ed5683303aab094e39aa4c9f30a003aa4ebc2" },
        { "Stall Soccerball.fbx", "150bc0f27a16745667096e7700c12eb3bdeba3d7308f9e56b2a98947daa86eca" },
        { "Goalkeeper Placing Ball.fbx", "37f0b3c305aa02335092ef9f68f152689b0a55786656245810c96466675f8812" },
        { "Treading Water.fbx", "6a76f88ec663b7275810d912e1111b10838cd64c8e1e8e912530a5f2011797c1" },
        { "Swimming.fbx", "ea2ea27438b4df87591ae285038b067da2551fa10b82714664d5af11b69d1771" },
    };

    static void PatrykPrzygotujWlasneRuchy()
    {
        var all = KlipyPatryka.Concat(DodatkoweKlipyPatryka)
            .Select(k => System.IO.Path.GetFileName(k.zrodla.Single())).ToArray();
        if (all.Length != 22 || all.Distinct(StringComparer.Ordinal).Count() != 22 ||
            all.Any(name => !Patryk22Hashes.ContainsKey(name)))
            throw new InvalidOperationException("PATRYK_OWNER_22: katalog animacji różni się od oryginalnej paczki 22 FBX");

        // Pierwsza faza: sprawdzenie CAŁEGO zestawu przed jakąkolwiek podmianą.
        // Nie dopuszczamy przypadkowego zmieszania paczek lub tylko sześciu plików.
        foreach (string name in all)
        {
            string src = PatrykRuchy + "/patryk_owner/" + name;
            if (!System.IO.File.Exists(src))
                throw new InvalidOperationException("PATRYK_OWNER_22: brak oryginalnej animacji " + name);
            using (var sha = System.Security.Cryptography.SHA256.Create())
            using (var stream = System.IO.File.OpenRead(src))
            {
                string actual = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
                if (actual != Patryk22Hashes[name])
                    throw new InvalidOperationException("PATRYK_OWNER_22: plik został podmieniony: " + name);
            }
        }
        System.IO.Directory.CreateDirectory(PatrykBezRetargetu);
        foreach (string name in all)
        {
            string src = PatrykRuchy + "/patryk_owner/" + name;
            string dst = PatrykBezRetargetu + "/" + name;
            bool changed = !System.IO.File.Exists(dst);
            if (!changed)
            {
                using (var sha = System.Security.Cryptography.SHA256.Create())
                using (var source = System.IO.File.OpenRead(src))
                using (var dest = System.IO.File.OpenRead(dst))
                    changed = !sha.ComputeHash(source).SequenceEqual(sha.ComputeHash(dest));
            }
            if (changed) System.IO.File.Copy(src, dst, true);
        }
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach (string name in all)
        {
            string path = PatrykBezRetargetu + "/" + name;
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (!importer)
                throw new InvalidOperationException("PATRYK_OWNER_22: nie można zaimportować FBX " + name);
            if (importer.animationType != ModelImporterAnimationType.Legacy ||
                !importer.importAnimation || importer.materialImportMode != ModelImporterMaterialImportMode.None)
            {
                importer.animationType = ModelImporterAnimationType.Legacy;
                importer.importAnimation = true;
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                importer.SaveAndReimport();
            }
        }
        Debug.Log("BRAXHORN_PATRYK_OWNER_22: 22/22 oryginalnych plików z ostatniej paczki, " +
            "SHA-256 PASS, Legacy, oryginalna siatka/kości nietknięte");
    }

    static AnimationClip PatrykWlasnyFBX(GameObject hero, List<Transform> kosci,
        List<PatrykStan> spoczynek, PatrykKlip spec, out string opis)
    {
        string rel = spec.zrodla.FirstOrDefault(z => z.StartsWith("patryk_owner/", StringComparison.Ordinal));
        if (rel == null) throw new InvalidOperationException("Brak oryginalnego źródła Patryka: " + spec.nazwa);
        string path = PatrykBezRetargetu + "/" + System.IO.Path.GetFileName(rel);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var sourceClip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
            .Where(c => c && !c.name.StartsWith("__preview__", StringComparison.Ordinal) && c.length > .01f)
            .OrderByDescending(c => AnimationUtility.GetCurveBindings(c).Count(b => b.type == typeof(Transform)))
            .ThenByDescending(c => c.length).FirstOrDefault();
        if (!prefab || !sourceClip)
            throw new InvalidOperationException("Nie zaimportowano oryginalnego FBX Legacy: " + path);
        var sourceInstance = Object.Instantiate(prefab);
        AnimationClip output = null;
        try
        {
            sourceInstance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            sourceInstance.transform.localScale = Vector3.one;
            var sourceBones = sourceInstance.GetComponentsInChildren<Transform>(true)
                .Where(t => !string.IsNullOrEmpty(t.name))
                .GroupBy(t => PatrykKosc(t.name), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            var targetBones = kosci
                .GroupBy(t => PatrykKosc(t.name), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            var rest = spoczynek.ToDictionary(x => x.t, x => x);
            foreach (string name in new[] { "Hips", "LeftUpLeg", "RightUpLeg", "LeftLeg", "RightLeg" })
                if (!sourceBones.ContainsKey(name) || !targetBones.ContainsKey(name))
                    throw new InvalidOperationException("Patryk: kość " + name + " nie występuje w obu szkieletach");
            var names = kosci.Select(t => PatrykKosc(t.name))
                .Where(sourceBones.ContainsKey).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (names.Length < 60)
                throw new InvalidOperationException("Patryk: mniej niż 60 kości dopasowanych: " + names.Length);
            var bindings = AnimationUtility.GetCurveBindings(sourceClip);
            int animated = bindings.Where(b => b.type == typeof(Transform) &&
                (b.propertyName.IndexOf("Rotation", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 b.propertyName.IndexOf("Euler", StringComparison.OrdinalIgnoreCase) >= 0))
                .Select(b => PatrykKosc(b.path.Split('/').LastOrDefault() ?? "Hips"))
                .Distinct(StringComparer.OrdinalIgnoreCase).Count();
            if (animated < 25)
                throw new InvalidOperationException("Patryk: oryginalny klip nie animuje dość kości: " + animated);

            var sourceRest = names.ToDictionary(n => n, n => sourceBones[n].localRotation, StringComparer.OrdinalIgnoreCase);
            var srcHips = sourceBones["Hips"];
            var hipsRest = srcHips.localPosition;
            // Obie długości mierzymy w jednostkach importu z prefabów, nie z
            // rozmiaru FBX ani mnożnika skali sceny. Zmiana rozmiaru postaci
            // na ekranie nie zmienia proporcji / geometrii kości.
            float LegLength(System.Collections.Generic.Dictionary<string, Transform> dict)
            {
                var up = dict["LeftUpLeg"]; var knee = dict["LeftLeg"]; var foot = dict["LeftFoot"];
                return Vector3.Distance(up.position, knee.position) + Vector3.Distance(knee.position, foot.position);
            }
            float sourceLeg = LegLength(sourceBones);
            float targetLeg = LegLength(targetBones);
            if (sourceLeg < 1e-5f || targetLeg < 1e-5f)
                throw new InvalidOperationException("Patryk: nie można obliczyć proporcji długości nóg");
            float motionScale = targetLeg / sourceLeg;
            if (float.IsNaN(motionScale) || float.IsInfinity(motionScale) || motionScale <= 0)
                throw new InvalidOperationException("Patryk: błędna skala ruchu bioder");

            int count = Mathf.Max(2, Mathf.CeilToInt(sourceClip.length * 30f) + 1);
            var times = new float[count]; var hipsPos = new Vector3[count];
            var rotations = names.ToDictionary(n => n, n => new Quaternion[count], StringComparer.OrdinalIgnoreCase);
            int sourcePoseDifferences = names.Count(n => Quaternion.Angle(sourceRest[n], rest[targetBones[n]].r) > 2f);
            for (int i = 0; i < count; i++)
            {
                float t = Mathf.Min(sourceClip.length, i / 30f); times[i] = t;
                sourceClip.SampleAnimation(sourceInstance, t);
                foreach (string name in names)
                {
                    var srcRot = sourceBones[name].localRotation;
                    var q = rest[targetBones[name]].r * Quaternion.Inverse(sourceRest[name]) * srcRot;
                    var arr = rotations[name];
                    if (i > 0 && Quaternion.Dot(q, arr[i - 1]) < 0f)
                        q = new Quaternion(-q.x, -q.y, -q.z, -q.w);
                    arr[i] = q;
                }
                // Tylko Hips ma animowaną translację; pozostałe kości zachowują
                // oryginalne położenia i długości modelu. Bez root-motion X/Z
                // kiedy gracza przesuwa CharacterController.
                Vector3 delta = (srcHips.localPosition - hipsRest) * motionScale;
                var original = rest[targetBones["Hips"]].p;
                hipsPos[i] = original + new Vector3(
                    spec.wMiejscu ? 0f : delta.x, delta.y,
                    spec.wMiejscu ? 0f : delta.z);
            }
            output = new AnimationClip
            {
                name = spec.nazwa, legacy = true, frameRate = 30f,
                wrapMode = spec.petla ? WrapMode.Loop : WrapMode.Once
            };
            foreach (string name in names)
            {
                var bonePath = AnimationUtility.CalculateTransformPath(targetBones[name], hero.transform);
                var arr = rotations[name];
                for (int axis = 0; axis < 4; axis++)
                    output.SetCurve(bonePath, typeof(Transform), "localRotation." + "xyzw"[axis],
                        PatrykKrzywaZ(times, arr.Select(q => q[axis]).ToArray(), false));
            }
            string hipsPath = AnimationUtility.CalculateTransformPath(targetBones["Hips"], hero.transform);
            for (int axis = 0; axis < 3; axis++)
                output.SetCurve(hipsPath, typeof(Transform), "localPosition." + "xyz"[axis],
                    PatrykKrzywaZ(times, hipsPos.Select(v => v[axis]).ToArray(), false));
            output.EnsureQuaternionContinuity();
            ZapiszKlipPatryka(output);
            output = null;
            opis = spec.nazwa + "←" + rel + " własny FBX, 65 kości; różnice pozy bazowej=" +
                sourcePoseDifferences + ", długość_nóg_ratio=" + motionScale.ToString("F4") +
                ", klatki=" + count + ", animowane_kości=" + animated +
                "; ORYGINALNY SZKIELET I SIATKA BEZ ZMIAN";
            Debug.Log("BRAXHORN_PATRYK_OWNER_22_RIG " + opis);
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(PatrykWynik + "/" + spec.nazwa + ".anim");
        }
        finally
        {
            if (output) Object.DestroyImmediate(output);
            Object.DestroyImmediate(sourceInstance);
            PatrykSpoczynek(spoczynek);
        }
    }

    // Brak sztucznych animacji zastępczych: wyłącznie oryginalne FBX właściciela.

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
            if (!zapisz) { info = " Unity Humanoid → Legacy, bez korekty kostek"; return result; }
            ZapiszKlipPatryka(result);
            info = " Unity Humanoid → Legacy, " + count + " próbek; bez korekty kostek";
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
            Vector3 naturalToes=Vector3.zero;
            foreach(var side in new[]{"Left","Right"})
            {
                var foot=B(side+"Foot");var toe=B(side+"Toes");
                if(!foot||!toe)continue;
                var dir=toe.position-foot.position;dir.y=0;
                if(dir.sqrMagnitude>1e-8f)naturalToes+=dir.normalized;
            }
            if(naturalToes.sqrMagnitude>1e-8f&&Vector3.Dot(naturalToes,przod)<0f)
                przod=-przod;
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
        PatrykSpoczynek(rest);
        Vector3 originalForward=PatrykPrzodStop(kosci);
        for (int i = 0; i < klatek; i++) { clip.SampleAnimation(hero, clip.length * i / Mathf.Max(1, klatek - 1)); PatrykNogiKlatka(kosci, w, originalForward); }
        PatrykSpoczynek(rest);
        return w;
    }

    // Celowo usunięto stary wybór animacji Walk/Run przez HumanPoseHandler.
    // Ruchy wlasciciela importuje wprost PatrykWlasnyFBX() z oryginalnego szkieletu.

}
