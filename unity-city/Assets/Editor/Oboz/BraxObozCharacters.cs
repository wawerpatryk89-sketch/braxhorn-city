using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

// Camp-only authoring. Uses the same Humanoid -> Legacy bridge as the estate
// player, without touching the original mesh, bind poses or bone lengths.
public static partial class BraxObozCharacters
{
    public const string ManifestPath = "Assets/Editor/Oboz/CharacterManifest.json";
    public const string Generated = "Assets/PlayableGenerated/Oboz";
    [Serializable] public sealed class ClipSpec { public string name, path, donor; public bool loop, inPlace, directCalibrated; }
    [Serializable] public sealed class CharacterSpec
    {
        public string id, name, role, modelPath, colorPath, normalPath, roughnessPath, metallicPath;
        public float realHeight, previewHeight;
        public bool hasGloves, glovesVerified, qualityExceptionApproved, appearanceApproved;
        public string[] sourceIssues;
        public ClipSpec[] clips;
    }
    [Serializable] public sealed class Manifest { public int schema; public float worldScale; public string unityVersion; public CharacterSpec[] characters; }
    sealed class Rest { public Transform t; public Vector3 p, s; public Quaternion r; }
    [Serializable] sealed class BakeReport { public string character, model; public int clips; public bool runtimeVerified; public string[] remaining; }

    public static Manifest ReadManifest()
    {
        if (Application.unityVersion != "6000.0.58f2")
            throw new InvalidOperationException("Odbudowa obozu wymaga Unity 6000.0.58f2; uruchomiono " + Application.unityVersion);
        if (!File.Exists(ManifestPath)) throw new InvalidOperationException("Brak manifestu źródeł obozu");
        var m = JsonUtility.FromJson<Manifest>(File.ReadAllText(ManifestPath));
        var wanted = new[] { "monika", "vorn", "zyro", "rakk", "kade", "gorr", "mis" };
        if (m == null || m.schema != 1 || m.characters == null || m.characters.Length != wanted.Length ||
            !m.characters.Select(c => c.id).OrderBy(x => x).SequenceEqual(wanted.OrderBy(x => x)))
            throw new InvalidOperationException("Obsada obozu musi zawierać dokładnie siedem uzgodnionych postaci");
        if (Mathf.Abs(m.worldScale - 1.294f) > .00001f || m.unityVersion != "6000.0.58f2")
            throw new InvalidOperationException("Niezgodna skala lub wersja projektu obozu");
        return m;
    }

    [MenuItem("BRAXHORN/Obóz/Przygotuj Monikę (bez APK)")]
    public static void PrepareMonika()
    {
        var m = ReadManifest();
        var c = m.characters.Single(x => x.id == "monika");
        var go = CreateCached(c, m.worldScale);
        try { PrefabUtility.SaveAsPrefabAsset(go, Generated + "/monika/Monika.prefab"); }
        finally { Object.DestroyImmediate(go); }
        AssetDatabase.SaveAssets();
    }

    public static GameObject CreateCached(CharacterSpec spec, float scale, bool sourceReviewOnly = false)
    {
        var dependencies = new[] { spec.modelPath, spec.colorPath, spec.normalPath, spec.roughnessPath, spec.metallicPath,
            "Assets/Editor/Oboz/BraxObozCharacters.cs", "Assets/Editor/Oboz/BraxObozImport.cs", "Assets/Oboz/ObozCharacter.shader" }
            .Concat(spec.clips.Any(c=>c.directCalibrated)?new[]{"Assets/Editor/Oboz/BraxObozMonikaMotion.cs"}:Array.Empty<string>())
            .Concat((spec.clips ?? Array.Empty<ClipSpec>()).Select(c => c.path)).Distinct().OrderBy(p => p);
        string input = Application.unityVersion + "|" + scale.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + sourceReviewOnly + "|" + JsonUtility.ToJson(spec);
        foreach (string path in dependencies)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) throw new InvalidOperationException("Brak zależności postaci: " + path);
            input += "|" + path + "=" + AssetDatabase.GetAssetDependencyHash(path);
        }
        string hash = Hash128.Compute(input).ToString(), folder = Generated + "/" + spec.id;
        string prefabPath = folder + "/CharacterCached.prefab", hashPath = folder + "/cache-hash.txt";
        var cached = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (cached && File.Exists(hashPath) && File.ReadAllText(hashPath) == hash)
        {
            Debug.Log("BRAXHORN_OBOZ pamięć postaci=" + spec.id + " trafienie"); return Object.Instantiate(cached);
        }
        var result = Create(spec, scale, sourceReviewOnly);
        try
        {
            if (!PrefabUtility.SaveAsPrefabAsset(result, prefabPath)) throw new IOException("Nie zapisano prefabu " + spec.name);
            AssetDatabase.SaveAssets(); File.WriteAllText(hashPath, hash);
            Debug.Log("BRAXHORN_OBOZ pamięć postaci=" + spec.id + " przeliczone"); return result;
        }
        catch { Object.DestroyImmediate(result); throw; }
    }

    public static GameObject Create(CharacterSpec spec, float scale, bool sourceReviewOnly = false)
    {
        if (spec.sourceIssues != null && spec.sourceIssues.Length != 0 && !spec.qualityExceptionApproved)
        {
            string issues = spec.name + ": rozbieżności źródła wymagają decyzji właściciela: " + string.Join("; ", spec.sourceIssues);
            if (!sourceReviewOnly) BraxBuildQuality.Issue(new InvalidOperationException(issues));
            Debug.LogWarning("TYLKO PODGLĄD ŹRÓDŁA, nie akceptacja do publikacji: " + issues);
        }
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(spec.modelPath);
        var avatar = AssetDatabase.LoadAllAssetsAtPath(spec.modelPath).OfType<Avatar>().FirstOrDefault();
        if (!prefab || !avatar || !avatar.isHuman || !avatar.isValid)
            throw new InvalidOperationException(spec.name + ": brak prawidłowego modelu/awatara Humanoid");
        if (spec.clips == null || spec.clips.Select(c => c.name).Distinct().Count() != spec.clips.Length)
            throw new InvalidOperationException(spec.name + ": brak lub powtórzone nazwy klipów");
        foreach (string required in new[] { "Idle", "CombatIdle", "Walk", "Run", "Punch", "Kick", "Jump", "GettingUp", "HeadHit" })
            if (!spec.clips.Any(c => c.name == required)) throw new InvalidOperationException(spec.name + ": brak " + required);
        string folder = Generated + "/" + spec.id;
        Directory.CreateDirectory(folder); AssetDatabase.Refresh();
        var hero = Object.Instantiate(prefab); hero.name = spec.name;
        Avatar calibratedMotion=null;
        hero.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        hero.transform.localScale = Vector3.one;
        try
        {
            foreach (var a in hero.GetComponentsInChildren<Animator>(true)) Object.DestroyImmediate(a);
            foreach (var a in hero.GetComponentsInChildren<Animation>(true)) Object.DestroyImmediate(a);
            var bones = hero.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("mixamorig", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (bones.Length != 65) BraxBuildQuality.Issue(new InvalidOperationException(spec.name + ": oczekiwano 65 kości mixamorig, znaleziono " + bones.Length));
            var rest = bones.Select(t => new Rest { t = t, p = t.localPosition, r = t.localRotation, s = t.localScale }).ToArray();
            var hips = bones.SingleOrDefault(t => BoneName(t.name) == "Hips");
            if (!hips) throw new InvalidOperationException("Brak Hips: " + spec.name);
            var skinned = hero.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (skinned.Length == 0) throw new InvalidOperationException("Brak skórowanej siatki: " + spec.name);
            float minY = float.PositiveInfinity, maxY = float.NegativeInfinity;
            foreach (var renderer in skinned)
            {
                if (!renderer.sharedMesh || renderer.sharedMesh.vertexCount == 0) throw new InvalidOperationException("Pusta siatka: " + spec.name);
                foreach (var v in renderer.sharedMesh.vertices)
                {
                    float y = renderer.transform.TransformPoint(v).y;
                    minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
                }
            }
            float sourceHeight = maxY - minY;
            if (!(sourceHeight > .05f) || float.IsInfinity(sourceHeight)) throw new InvalidOperationException("Nieprawidłowy wymiar modelu");
            // Monika has an agreed height; other models retain their authored size
            // after the FBX importer applies its unit conversion. Reject implausible units.
            // The source FBX models are normalized to about one metre. They
            // do not carry an agreed NPC height. Review-only previews use an
            // explicitly provisional baseline, never treat that as approval.
            float realHeight = spec.realHeight > 0f ? spec.realHeight : sourceReviewOnly && spec.previewHeight > 0f ? spec.previewHeight : 0;
            if (realHeight <= 0) throw new InvalidOperationException(spec.name + ": brak uzgodnionego wzrostu");
            if (!(realHeight >= .8f && realHeight <= 3f)) BraxBuildQuality.Issue(new InvalidOperationException(spec.name + ": niejednoznaczne jednostki/wzrost źródła " + realHeight));
            var material = Material(spec, folder);
            int rendererIndex = 0;
            foreach (var renderer in skinned)
            {
                if (spec.id == "gorr") PrepareHandMask(renderer, folder + "/Hands_" + rendererIndex + ".asset");
                rendererIndex++;
                renderer.sharedMaterials = Enumerable.Repeat(material, Mathf.Max(1, renderer.sharedMesh.subMeshCount)).ToArray();
                renderer.quality = SkinQuality.Bone4; renderer.updateWhenOffscreen = true;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
            var animation = hero.AddComponent<Animation>(); animation.playAutomatically = false;
            animation.cullingType = AnimationCullingType.AlwaysAnimate;
            if(spec.clips.Any(c=>c.directCalibrated))calibratedMotion=CalibratedMotionAvatar(hero,avatar);
            foreach (var clip in spec.clips)
            {
                var baked = clip.directCalibrated?BakeDirectMotion(hero,calibratedMotion,hips,rest,clip):Bake(hero, avatar, hips, rest, clip);
                Validate(hero, hips, rest, baked);
                Save(baked, folder + "/" + clip.name + ".anim");
                animation.AddClip(AssetDatabase.LoadAssetAtPath<AnimationClip>(folder + "/" + clip.name + ".anim"), clip.name);
            }
            Restore(rest);
            animation.clip = animation.GetClip("Idle"); animation.Play("Idle"); animation.Sample();
            float height = realHeight * scale;
            hero.transform.localScale = Vector3.one * (height / sourceHeight);
            hero.transform.localPosition = Vector3.up * (-minY * height / sourceHeight);
            foreach (var t in hero.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 8;
            File.WriteAllText(folder + "/bake-report.json", JsonUtility.ToJson(new BakeReport {
                character = spec.id, model = spec.modelPath, clips = spec.clips.Length,
                runtimeVerified = false, remaining = new[] { "Unity Play Mode: stopy, poślizg, przejścia", "Ręce względem tułowia", "Wygląd stroju i logotypy", "Telefon i wydajność" }
            }, true));
            return hero;
        }
        catch { Object.DestroyImmediate(hero); throw; }
        finally { if(calibratedMotion)Object.DestroyImmediate(calibratedMotion); }
    }

    static string BoneName(string name)
    {
        int colon = name.LastIndexOf(':');
        return colon >= 0 ? name.Substring(colon + 1) : name.StartsWith("mixamorig", StringComparison.OrdinalIgnoreCase) ? name.Substring(9).TrimStart('0','1','2','3','4','5','6','7','8','9','_') : name;
    }
    static void Restore(IEnumerable<Rest> rest) { foreach (var s in rest) { s.t.localPosition = s.p; s.t.localRotation = s.r; s.t.localScale = s.s; } }
    static void Save(Object value, string path)
    {
        var old = AssetDatabase.LoadAssetAtPath<Object>(path);
        if (old) { EditorUtility.CopySerialized(value, old); Object.DestroyImmediate(value); }
        else AssetDatabase.CreateAsset(value, path);
    }
    static Material Material(CharacterSpec c, string folder)
    {
        var shader = Shader.Find("Braxhorn/ObozCharacter");
        if (!shader) throw new InvalidOperationException("Brak shadera postaci obozu");
        var mat = new Material(shader) { name = c.name + " — oryginalne mapy PBR" };
        mat.SetFloat("_DarkHands", c.id == "gorr" ? 1 : 0);
        var props = new[] { "_MainTex", "_BumpMap", "_RoughnessTex", "_MetallicTex" };
        var paths = new[] { c.colorPath, c.normalPath, c.roughnessPath, c.metallicPath };
        for (int i = 0; i < paths.Length; i++)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(paths[i]);
            if (!texture) { Object.DestroyImmediate(mat); throw new InvalidOperationException("Brak tekstury " + paths[i]); }
            mat.SetTexture(props[i], texture);
        }
        Save(mat, folder + "/Character.mat");
        return AssetDatabase.LoadAssetAtPath<Material>(folder + "/Character.mat");
    }
    static void PrepareHandMask(SkinnedMeshRenderer renderer, string path)
    {
        var source = renderer.sharedMesh;
        var weights = source.boneWeights;
        if (weights.Length != source.vertexCount) throw new InvalidOperationException("GORR: brak wag do maski dłoni");
        var handBones = renderer.bones.Select(b => b && (BoneName(b.name).StartsWith("LeftHand", StringComparison.Ordinal) ||
            BoneName(b.name).StartsWith("RightHand", StringComparison.Ordinal))).ToArray();
        float Weight(int index, float weight) => index >= 0 && index < handBones.Length && handBones[index] ? weight : 0;
        var colors = new Color[source.vertexCount]; int marked = 0;
        for (int i = 0; i < weights.Length; i++)
        {
            var w = weights[i]; float value = Weight(w.boneIndex0,w.weight0) + Weight(w.boneIndex1,w.weight1) +
                Weight(w.boneIndex2,w.weight2) + Weight(w.boneIndex3,w.weight3);
            colors[i] = new Color(1,1,1,Mathf.Clamp01(value)); if (value > .05f) marked++;
        }
        if (marked == 0) throw new InvalidOperationException("GORR: nie znaleziono wierzchołków dłoni");
        // Only vertex colours are added to a generated copy. Positions, UVs,
        // normals, topology, weights and bind poses remain source-identical.
        var copy = Object.Instantiate(source); copy.name = "GORR — maska dłoni, geometria oryginalna"; copy.colors = colors;
        Save(copy, path); renderer.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        Debug.Log("BRAXHORN_OBOZ maska dłoni GORR: " + marked + " wierzchołków; źródło bez zmian");
    }
    static AnimationClip Bake(GameObject hero, Avatar avatar, Transform hips, Rest[] rest, ClipSpec spec)
    {
        var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(spec.path);
        var sourceClip = AssetDatabase.LoadAllAssetsAtPath(spec.path).OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__preview__")).OrderByDescending(c => c.length).FirstOrDefault();
        if (!sourcePrefab || !sourceClip || !sourceClip.humanMotion || sourceClip.length <= 0)
            throw new InvalidOperationException("Brak animacji Humanoid: " + spec.path);
        var source = Object.Instantiate(sourcePrefab);
        PlayableGraph graph = default; HumanPoseHandler reader = null, writer = null;
        try
        {
            source.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity); source.transform.localScale = Vector3.one;
            var animator = source.GetComponent<Animator>();
            if (!animator || !animator.avatar || !animator.isHuman || !animator.avatar.isValid) throw new InvalidOperationException("Nieprawidłowy dawca: " + spec.path);
            animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            graph = PlayableGraph.Create("Oboz bake " + spec.name); graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var playable = AnimationClipPlayable.Create(graph, sourceClip); playable.SetApplyFootIK(true);
            AnimationPlayableOutput.Create(graph, "pose", animator).SetSourcePlayable(playable); graph.Play();
            reader = new HumanPoseHandler(animator.avatar, source.transform); writer = new HumanPoseHandler(avatar, hero.transform);
            HumanPose pose = new HumanPose();
            Vector3 Sample(float time) { playable.SetTime(time); graph.Evaluate(0); reader.GetHumanPose(ref pose); return pose.bodyPosition; }
            Vector3 start = Sample(0), end = Sample(sourceClip.length);
            Vector3 drift = spec.inPlace ? new Vector3(end.x - start.x, 0, end.z - start.z) : Vector3.zero;
            int count = Mathf.CeilToInt(sourceClip.length * 30) + 1;
            var rotations = rest.Select(_ => new Quaternion[count]).ToArray();
            var positions = new Vector3[count]; var times = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = Mathf.Min(i / 30f, sourceClip.length); times[i] = t;
                Sample(t); pose.bodyPosition -= drift * (t / sourceClip.length);
                Restore(rest); writer.SetHumanPose(ref pose);
                positions[i] = hips.localPosition;
                for (int j = 0; j < rest.Length; j++)
                {
                    Quaternion q = rest[j].t.localRotation;
                    if (i > 0 && Quaternion.Dot(q, rotations[j][i - 1]) < 0) q = new Quaternion(-q.x, -q.y, -q.z, -q.w);
                    rotations[j][i] = q;
                }
            }
            Restore(rest);
            var clip = new AnimationClip { name = spec.name, legacy = true, frameRate = 30, wrapMode = spec.loop ? WrapMode.Loop : WrapMode.Once };
            for (int j = 0; j < rest.Length; j++)
            {
                string path = AnimationUtility.CalculateTransformPath(rest[j].t, hero.transform);
                for (int a = 0; a < 4; a++)
                    clip.SetCurve(path, typeof(Transform), "localRotation." + "xyzw"[a], Curve(times, rotations[j].Select(q => q[a]).ToArray()));
            }
            for (int a = 0; a < 3; a++) clip.SetCurve(AnimationUtility.CalculateTransformPath(hips, hero.transform), typeof(Transform), "localPosition." + "xyz"[a], Curve(times, positions.Select(p => p[a]).ToArray()));
            clip.EnsureQuaternionContinuity(); return clip;
        }
        finally { reader?.Dispose(); writer?.Dispose(); if (graph.IsValid()) graph.Destroy(); Object.DestroyImmediate(source); Restore(rest); }
    }
    static AnimationCurve Curve(float[] times, float[] values)
    {
        var curve = new AnimationCurve(times.Select((t,i) => new Keyframe(t, values[i])).ToArray());
        for (int i = 0; i < curve.length; i++) { AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear); AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear); }
        return curve;
    }
    static void Validate(GameObject hero, Transform hips, Rest[] rest, AnimationClip clip)
    {
        try
        {
            for (int i = 0; i <= 30; i++)
            {
                Restore(rest); clip.SampleAnimation(hero, clip.length * i / 30f);
                foreach (var s in rest)
                {
                    var p = s.t.localPosition; var q = s.t.localRotation;
                    if (float.IsNaN(p.sqrMagnitude) || float.IsInfinity(p.sqrMagnitude) || float.IsNaN(q.x+q.y+q.z+q.w) || float.IsInfinity(q.x+q.y+q.z+q.w)) throw new InvalidOperationException("Nieprawidłowe liczby w " + clip.name);
                    if (s.t != hips && (p - s.p).magnitude > Mathf.Max(.0001f, s.p.magnitude * .02f)) BraxBuildQuality.Issue(new InvalidOperationException("Zmieniona długość kości w " + clip.name));
                }
            }
        }
        finally { Restore(rest); }
    }
}
