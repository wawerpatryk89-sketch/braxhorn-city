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

    // Keep the model's relaxed bind pose. No fighting clip is used before an attack.
    // Wzorzec Moniki: identyczna oryginalna hierarchia, siatka i bindposes.
    // Idle = pozycja spoczynkowa WŁASNEGO MODELU Patryka, bez obracania
    // ramion, dłoni, stóp lub kręgosłupa przez kod (brak klipu Idle w paczce).
    static AnimationClip PatrykSpokojny(GameObject hero, List<PatrykStan> rest)
    {
        PatrykSpoczynek(rest);
        return PatrykZapiszWlasnaPoze(hero, rest, "Idle");
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
    static readonly string[] PatrykRuchyWlasne =
    {
        "Strut Walking.fbx", "Fast Run.fbx", "Jump.fbx", "Entering Car.fbx",
        "Goalkeeper Diving Save.fbx", "Goalkeeper Catch.fbx"
    };

    static void PatrykPrzygotujWlasneRuchy()
    {
        System.IO.Directory.CreateDirectory(PatrykBezRetargetu);
        // Dodatkowe 16 FBX nie są jeszcze w prywatnym repo. Gdy zostaną
        // bezpiecznie odszyfrowane, importer użyje WYŁĄCZNIE ich oryginalnych bajtów.
        var dodatkowe = DodatkoweKlipyPatryka
            .SelectMany(k => k.zrodla)
            .Select(System.IO.Path.GetFileName)
            .Where(name => System.IO.File.Exists(PatrykRuchy + "/patryk_owner/" + name));
        var dostepne = PatrykRuchyWlasne.Concat(dodatkowe).Distinct(StringComparer.Ordinal).ToArray();
        foreach (string name in dostepne)
        {
            string src = PatrykRuchy + "/patryk_owner/" + name;
            string dst = PatrykBezRetargetu + "/" + name;
            if (!System.IO.File.Exists(src))
                throw new InvalidOperationException("Brak przeslanej animacji Patryka: " + src);
            bool inne = !System.IO.File.Exists(dst);
            if (!inne)
            {
                using (var sha = System.Security.Cryptography.SHA256.Create())
                using (var s = System.IO.File.OpenRead(src))
                using (var d = System.IO.File.OpenRead(dst))
                    inne = !sha.ComputeHash(s).SequenceEqual(sha.ComputeHash(d));
            }
            if (inne) System.IO.File.Copy(src, dst, true);
        }
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach (string name in dostepne)
        {
            string path = PatrykBezRetargetu + "/" + name;
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
                throw new InvalidOperationException("Nie mozna zaimportowac FBX Patryka: " + path);
            if (importer.animationType != ModelImporterAnimationType.Legacy || !importer.importAnimation)
            {
                importer.animationType = ModelImporterAnimationType.Legacy;
                importer.importAnimation = true;
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                importer.SaveAndReimport();
            }
        }
        Debug.Log("BRAXHORN_PATRYK_WLASNE_RUCHY: import=" + dostepne.Length +
            " oryginalnych FBX (Legacy, bezp. kości jak Monika); bez Humanoid i sztucznych klipów");
    }

    static AnimationClip PatrykWlasnyFBX(GameObject hero, List<Transform> kosci,
        List<PatrykStan> spoczynek, PatrykKlip spec, out string opis)
    {
        string rel = spec.zrodla.FirstOrDefault(s => s.StartsWith("patryk_owner/", StringComparison.Ordinal));
        if (rel == null)
            throw new InvalidOperationException("Nie zdefiniowano animacji wlasciciela dla " + spec.nazwa);
        string path = PatrykBezRetargetu + "/" + System.IO.Path.GetFileName(rel);
        // Legacy zachowuje lokalne krzywe kosci. Zrodlo ma nazwy mixamorig
        // zgodne 1:1 z modelem (65 kosci); podmieniamy tylko sciezki transformow.
        var sources = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
            .Where(c => c && !c.name.StartsWith("__preview__", StringComparison.Ordinal) && c.length > 0f)
            .OrderByDescending(c => AnimationUtility.GetCurveBindings(c).Count(b => b.type == typeof(Transform)))
            .ThenByDescending(c => c.length).ToArray();
        var source = sources.FirstOrDefault();
        if (!source)
            throw new InvalidOperationException("Brak animacji Legacy FBX po imporcie: " + path);

        var targets = kosci.GroupBy(t => PatrykKosc(t.name), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var rest = spoczynek.ToDictionary(s => s.t, s => s.p);
        var bindings = AnimationUtility.GetCurveBindings(source);
        var clip = new AnimationClip { name = spec.nazwa, legacy = true,
            frameRate = source.frameRate > 0f ? source.frameRate : 30f,
            wrapMode = spec.petla ? WrapMode.Loop : WrapMode.Once };
        var mapped = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int curves = 0;
        try
        {
            foreach (var binding in bindings)
            {
                if (binding.type != typeof(Transform)) continue;
                string field = binding.propertyName;
                bool rotation = field.IndexOf("Rotation", StringComparison.OrdinalIgnoreCase) >= 0
                    || field.IndexOf("Euler", StringComparison.OrdinalIgnoreCase) >= 0;
                bool position = field.IndexOf("Position", StringComparison.OrdinalIgnoreCase) >= 0;
                if (!rotation && !position) continue;
                string[] fragments = (binding.path ?? "").Split('/');
                string rawBone = fragments.Length == 0 || string.IsNullOrEmpty(fragments[fragments.Length - 1])
                    ? "Hips" : fragments[fragments.Length - 1];
                string bone = PatrykKosc(rawBone);
                if (!targets.TryGetValue(bone, out Transform target))
                    throw new InvalidOperationException("Nieznana kosc animacji FBX " + rawBone +
                        ", klip=" + spec.nazwa + ", path=" + binding.path);
                // Zrodlo w ruchu ma tylko oryginalna poze; inne materialy
                // i skale nie sa kopiowane ani modyfikowane.
                if (position && !bone.Equals("Hips", StringComparison.OrdinalIgnoreCase))
                    continue;
                var curve = AnimationUtility.GetEditorCurve(source, binding);
                if (curve == null) continue;
                if (position)
                {
                    int dot = field.LastIndexOf('.');
                    char axis = dot >= 0 && dot + 1 < field.Length ? char.ToLowerInvariant(field[dot + 1]) : ' ';
                    int channel = axis == 'x' ? 0 : axis == 'y' ? 1 : axis == 'z' ? 2 : -1;
                    if (channel < 0) continue;
                    float original = rest[target][channel];
                    if (spec.wMiejscu && axis != 'y')
                    {
                        // Gracza przesuwa CharacterController, nie transport bioder w pliku FBX.
                        curve = AnimationCurve.Constant(0f, Mathf.Max(.01f, source.length), original);
                    }
                    else
                    {
                        float startValue = curve.Evaluate(0f);
                        var keyframes = curve.keys;
                        for (int i = 0; i < keyframes.Length; i++)
                        {
                            var key = keyframes[i];
                            key.value += original - startValue;
                            keyframes[i] = key;
                        }
                        curve = new AnimationCurve(keyframes) {
                            preWrapMode = curve.preWrapMode, postWrapMode = curve.postWrapMode
                        };
                    }
                }
                var mappedBinding = binding;
                mappedBinding.path = AnimationUtility.CalculateTransformPath(target, hero.transform);
                AnimationUtility.SetEditorCurve(clip, mappedBinding, curve);
                mapped.Add(bone);
                curves++;
            }
            // Pliki Mixamo mogą przekazywać ruch korzenia bez osobnej krzywej
            // Transform Hips. Nie odrzucaj z tego powodu oryginalnych ruchów nóg:
            // brakujące Hips pozostają w oryginalnej pozie szkieletu Patryka.
            if (!mapped.Contains("Hips"))
            {
                var hips = kosci.FirstOrDefault(t => PatrykKosc(t.name) == "Hips");
                if (!hips) throw new InvalidOperationException("Patryk: brak Hips w oryginalnym szkielecie");
                var pose = spoczynek.First(t => t.t == hips);
                string hipsPath = AnimationUtility.CalculateTransformPath(hips, hero.transform);
                float duration = Mathf.Max(.01f, source.length);
                for (int axis = 0; axis < 4; axis++)
                    AnimationUtility.SetEditorCurve(clip,
                        EditorCurveBinding.FloatCurve(hipsPath, typeof(Transform), "localRotation." + "xyzw"[axis]),
                        AnimationCurve.Constant(0f, duration, pose.r[axis]));
                for (int axis = 0; axis < 3; axis++)
                    AnimationUtility.SetEditorCurve(clip,
                        EditorCurveBinding.FloatCurve(hipsPath, typeof(Transform), "localPosition." + "xyz"[axis]),
                        AnimationCurve.Constant(0f, duration, pose.p[axis]));
                mapped.Add("Hips");
                Debug.LogWarning("BRAXHORN_PATRYK_OWNER: Hips bez krzywych w FBX " + spec.nazwa +
                    ", zachowana oryginalna pozycja Hips modelu; animację sprawdzić w Unity");
            }
            foreach (string required in new[] { "Hips", "LeftUpLeg", "RightUpLeg", "LeftLeg", "RightLeg" })
                if (!mapped.Contains(required))
                    throw new InvalidOperationException("FBX " + spec.nazwa +
                        " nie ma wymaganych krzywych kosci " + required);
            if (mapped.Count < 25)
                throw new InvalidOperationException("FBX " + spec.nazwa +
                    " ma za malo zgodnych kosci: " + mapped.Count);
            clip.EnsureQuaternionContinuity();
            ZapiszKlipPatryka(clip);
            clip = null;
            opis = spec.nazwa + "←" + rel + " BEZ RETARGETINGU; kosci=" +
                mapped.Count + " krzywe=" + curves;
            Debug.Log("BRAXHORN_PATRYK_OWNER_DIRECT " + opis);
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(PatrykWynik + "/" + spec.nazwa + ".anim");
        }
        finally
        {
            if (clip) Object.DestroyImmediate(clip);
            PatrykSpoczynek(spoczynek);
        }
    }

    // Chod/bieg awaryjny na oryginalnym szkielecie; tylko niewielkie rotacje kosci.
    // Zero zmian siatki, skal, bindpose i root motion (ruch daje CharacterController).
    static AnimationClip PatrykSpacerAwaryjny(GameObject hero, List<PatrykStan> rest, string name)
    {
        bool szybki = name == "Run";
        float czas = szybki ? .68f : 1.1f;
        float udo = szybki ? 20f : 11f, kolano = szybki ? 21f : 11f, ramie = szybki ? 14f : 8f;
        var clip = new AnimationClip { name = name, legacy = true, frameRate = 30, wrapMode = WrapMode.Loop };
        foreach (string kosc in new[] { "LeftUpLeg", "RightUpLeg", "LeftLeg", "RightLeg", "LeftArm", "RightArm" })
        {
            var pose = rest.FirstOrDefault(x => PatrykKosc(x.t.name) == kosc);
            if (pose == null) throw new InvalidOperationException("Brak kosci chodu Patryka: " + kosc);
            Vector3 os = pose.t.parent
                ? pose.t.parent.InverseTransformDirection(hero.transform.right).normalized
                : Vector3.right;
            if (os.sqrMagnitude < .5f) os = Vector3.right;
            string sciezka = AnimationUtility.CalculateTransformPath(pose.t, hero.transform);
            var kanaly = new Keyframe[4][];
            for (int kanal = 0; kanal < 4; kanal++) kanaly[kanal] = new Keyframe[9];
            for (int klatka = 0; klatka <= 8; klatka++)
            {
                float faza = 2f * Mathf.PI * klatka / 8f;
                float l = Mathf.Sin(faza), r = -l;
                float kat = kosc == "LeftUpLeg" ? l * udo :
                            kosc == "RightUpLeg" ? r * udo :
                            kosc == "LeftLeg" ? Mathf.Max(0f, -l) * kolano :
                            kosc == "RightLeg" ? Mathf.Max(0f, -r) * kolano :
                            kosc == "LeftArm" ? -l * ramie : -r * ramie;
                Quaternion q = Quaternion.AngleAxis(kat, os) * pose.r;
                for (int kanal = 0; kanal < 4; kanal++)
                    kanaly[kanal][klatka] = new Keyframe(czas * klatka / 8f, q[kanal]);
            }
            for (int kanal = 0; kanal < 4; kanal++)
                clip.SetCurve(sciezka, typeof(Transform), "localRotation." + "xyzw"[kanal],
                    new AnimationCurve(kanaly[kanal]));
        }
        clip.EnsureQuaternionContinuity();
        ZapiszKlipPatryka(clip);
        return AssetDatabase.LoadAssetAtPath<AnimationClip>(PatrykWynik + "/" + name + ".anim");
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
