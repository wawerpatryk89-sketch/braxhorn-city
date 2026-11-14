using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

public static class BraxKontrolaPostaciT
{
    [MenuItem("Braxhorn/Postacie/Kontrola T w Unity")]
    public static void Run()
    {
        var errors = new List<string>();
        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../KontrolaPostaciT"));
        Directory.CreateDirectory(output);
        var profiles = Directory.GetFiles(BraxMixamoT.Root, "*.json").OrderBy(p => p).ToArray();
        if (profiles.Length == 0) throw new InvalidDataException("Brak przygotowanych profili T");
        foreach (string profile in profiles)
        {
            string id = Path.GetFileNameWithoutExtension(profile);
            GameObject instance = null, player = null;
            try
            {
                string folder = "Assets/Source/Prywatne/Postacie/" + id;
                string path = Directory.GetFiles(folder, "*.fbx").Single().Replace('\\', '/');
                if (id == "patryk")
                {
                    player = new GameObject("Test Patryka");
                    instance = BraxClaudeOsiedle.PatrykBohater(player.transform, null);
                }
                else
                {
                    BraxGitaraSetup.CreateFromProfile(id, Path.GetFileNameWithoutExtension(path));
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Path.ChangeExtension(path, ".prefab"));
                    instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                }
                var animator = instance.GetComponent<Animator>();
                if (animator)
                {
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    animator.Rebind();
                }
                var legacy = instance.GetComponent<Animation>();
                var profileClips = BraxMixamoT.Read(id);
                var actions = new[] { "Idle", "Walk", "Run", "Wave", "Jab", "Punch" }.Where(profileClips.ContainsKey);
                var skins = instance.GetComponentsInChildren<SkinnedMeshRenderer>();
                if (skins.Length == 0) throw new InvalidDataException("Brak skóry");
                foreach (string action in actions)
                {
                    AnimationClip clip = legacy ? legacy.GetClip(action) :
                        animator.runtimeAnimatorController.animationClips.First(c => c.name.StartsWith(action + "_", StringComparison.Ordinal));
                    if (!clip || clip.length <= 0) throw new InvalidDataException(action);
                    var graph = PlayableGraph.Create("QA");
                    try
                    {
                        AnimationClipPlayable playable = default;
                        if (animator)
                        {
                            playable = AnimationClipPlayable.Create(graph, clip);
                            playable.SetApplyFootIK(BraxMixamoT.FootIK(action));
                            AnimationPlayableOutput.Create(graph, "test", animator).SetSourcePlayable(playable);
                            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                            graph.Play();
                        }
                        void Sample(float t)
                        {
                            if (legacy) clip.SampleAnimation(instance, t);
                            else { playable.SetTime(t); graph.Evaluate(0); }
                        }
                        if (action == "Walk" || action == "Run")
                        {
                            var source = BraxMixamoT.Load(profileClips[action], action, true);
                            MeasureHands(instance, profileClips[action], source, id, action, Sample, output);
                        }
                        for (int i = 0; i < 5; i++)
                        {
                            float t = clip.length * i / 5f;
                            Sample(t);
                            Capture(instance, skins, output, id, action, i);
                        }
                    }
                    finally { graph.Destroy(); }
                }
                Debug.Log("POSTAC_T_RENDER " + id);
            }
            catch (Exception e) { errors.Add(id + " " + e.Message); Debug.LogError(id + " " + e); }
            finally { if (instance) Object.DestroyImmediate(instance); if (player) Object.DestroyImmediate(player); }
        }
        File.WriteAllLines(Path.Combine(output, "bledy.txt"), errors);
        if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
    }

    static Dictionary<string, Transform> BoneMap(GameObject actor)
    {
        var result = new Dictionary<string, Transform>();
        foreach (var t in actor.GetComponentsInChildren<Transform>(true))
        {
            var name = t.name.Substring(t.name.LastIndexOf(':') + 1);
            if (name.StartsWith("mixamorig", StringComparison.OrdinalIgnoreCase))
                name = name.Substring(9).TrimStart('0','1','2','3','4','5','6','7','8','9','_');
            if (!result.ContainsKey(name)) result.Add(name, t);
        }
        return result;
    }

    static Vector3 InBodySpace(Vector3 vector, Dictionary<string, Transform> bones)
    {
        Vector3 right = bones["RightUpLeg"].position - bones["LeftUpLeg"].position;
        Vector3 up = bones["Neck"].position - bones["Hips"].position;
        Quaternion axes = Quaternion.LookRotation(Vector3.Cross(right, up), up);
        return Quaternion.Inverse(axes) * vector.normalized;
    }

    static Vector3 HandDirection(Dictionary<string, Transform> bones, string side, bool normal)
    {
        Vector3 along = bones[side + "HandMiddle1"].position - bones[side + "Hand"].position;
        Vector3 across = bones[side + "HandIndex1"].position - bones[side + "HandPinky1"].position;
        return InBodySpace(normal ? Vector3.Cross(along, across) : along, bones);
    }

    // Sample the source Avatar and the actual game rig at the same clip time,
    // using each actor's torso as reference. Geometry and rig proportions differ.
    static void MeasureHands(GameObject actor, string path, AnimationClip source, string id, string action,
        Action<float> sampleTarget, string output)
    {
        var reference = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
        var graph = PlayableGraph.Create("QA source");
        try
        {
            var animator = reference.GetComponent<Animator>();
            animator.applyRootMotion = false;
            animator.Rebind();
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var clip = AnimationClipPlayable.Create(graph, source);
            clip.SetApplyFootIK(false);
            AnimationPlayableOutput.Create(graph, "source", animator).SetSourcePlayable(clip);
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            graph.Play();
            var src = BoneMap(reference);
            var dst = BoneMap(actor);
            var rows = new List<string> {"sekunda,strona,kierunek_dloni_stopnie,normalna_dloni_stopnie,udo_stopnie,lydka_stopnie,stopa_stopnie"};
            var first = dst.ToDictionary(pair => pair.Key, pair => pair.Value.localRotation);
            float maxChange = 0f;
            var vectors = new List<string> {"faza,strona,odcinek,postac_prawo,postac_gora,postac_przod,humanoid_prawo,humanoid_gora,humanoid_przod"};
            for (int i = 0; i < 32; i++)
            {
                float t = source.length * i / 32f;
                clip.SetTime(t); graph.Evaluate(0);
                sampleTarget(t);
                foreach (string side in new[] {"Left", "Right"})
                {
                    float hand = Vector3.Angle(HandDirection(src, side, false), HandDirection(dst, side, false));
                    float palm = Vector3.Angle(HandDirection(src, side, true), HandDirection(dst, side, true));
                    float Angle(string a, string b)
                    {
                        Vector3 Direction(Dictionary<string, Transform> m) => InBodySpace(m[side + b].position - m[side + a].position, m);
                        Vector3 u = Direction(dst), v = Direction(src);
                        vectors.Add(FormattableString.Invariant($"{i},{side},{b},{u.x:F5},{u.y:F5},{u.z:F5},{v.x:F5},{v.y:F5},{v.z:F5}"));
                        return Vector3.Angle(v, u);
                    }
                    float thigh = Angle("UpLeg", "Leg"), shin = Angle("Leg", "Foot"), foot = Angle("Foot", "ToeBase");
                    var leg = side + "UpLeg";
                    if (i == 0) first[leg] = dst[leg].localRotation;
                    else maxChange = Mathf.Max(maxChange, Quaternion.Angle(first[leg], dst[leg].localRotation));
                    rows.Add(FormattableString.Invariant($"{t:F4},{side},{hand:F2},{palm:F2},{thigh:F2},{shin:F2},{foot:F2}"));
                }
            }
            File.WriteAllLines(Path.Combine(output, id + "_" + action + ".csv"), rows);
            File.WriteAllLines(Path.Combine(output, id + "_" + action + "_wektory.csv"), vectors);
            if (maxChange < 1f) throw new InvalidDataException(id + "/" + action + ": nieruchome nogi");
        }
        finally { graph.Destroy(); Object.DestroyImmediate(reference); }
    }

    static void Capture(GameObject actor, SkinnedMeshRenderer[] skins, string output, string id, string action, int index)
    {
        var meshes = new List<Mesh>();
        var preview = new PreviewRenderUtility();
        try
        {
            // Snapshot the evaluated skin itself. Re-instantiating a prefab can reset its pose.
            var previewRoot = new GameObject("Ujęcie");
            preview.AddSingleGO(previewRoot);
            Bounds bounds = default;
            for (int i = 0; i < skins.Length; i++)
            {
                var skin = skins[i];
                var mesh = new Mesh();
                meshes.Add(mesh);
                skin.BakeMesh(mesh);
                var go = new GameObject(skin.name, typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(previewRoot.transform, false);
                go.transform.SetPositionAndRotation(skin.transform.position, skin.transform.rotation);
                go.transform.localScale = skin.transform.lossyScale;
                go.GetComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = go.GetComponent<MeshRenderer>();
                renderer.sharedMaterials = skin.sharedMaterials;
                if (i == 0) bounds = renderer.bounds;
                else bounds.Encapsulate(renderer.bounds);
            }
            var camera = preview.camera;
            camera.orthographic = true;
            camera.orthographicSize = bounds.extents.y * 1.18f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.26f, .29f, .32f);
            Vector3 direction = new Vector3(.4f, .08f, 1).normalized;
            var landmarks = BoneMap(actor);
            Vector3 across = landmarks["RightUpLeg"].position - landmarks["LeftUpLeg"].position;
            Vector3 front = Vector3.Cross(across.normalized, Vector3.up);
            Vector3 toes = (landmarks["LeftToeBase"].position - landmarks["LeftFoot"].position) +
                (landmarks["RightToeBase"].position - landmarks["RightFoot"].position);
            if (Vector3.Dot(front, toes) < 0) front = -front;
            if (front.sqrMagnitude > .0001f)
                direction = (front.normalized + across.normalized * .4f + Vector3.up * .08f).normalized;
            camera.transform.position = bounds.center + direction * bounds.size.magnitude * 2f;
            camera.transform.LookAt(bounds.center);
            camera.nearClipPlane = .001f;
            camera.farClipPlane = Mathf.Max(100f, bounds.size.magnitude * 5f);
            preview.lights[0].intensity = 1.2f;
            preview.lights[0].transform.rotation = camera.transform.rotation * Quaternion.Euler(25, -30, 0);
            preview.lights[1].intensity = .65f;
            preview.ambientColor = Color.gray;
            preview.BeginStaticPreview(new Rect(0, 0, 384, 512));
            camera.Render();
            var image = preview.EndStaticPreview();
            try
            {
                Color32[] pixels = image.GetPixels32();
                if (pixels.Length == 0 || !pixels.Any(p => Mathf.Abs(p.r - pixels[0].r) +
                    Mathf.Abs(p.g - pixels[0].g) + Mathf.Abs(p.b - pixels[0].b) > 24))
                    throw new InvalidDataException("Pusta klatka: " + id + "/" + action);
                File.WriteAllBytes(Path.Combine(output, id + "_" + action + "_" + index + ".png"), image.EncodeToPNG());
            }
            finally { Object.DestroyImmediate(image); }
        }
        finally
        {
            preview.Cleanup();
            foreach (var mesh in meshes) Object.DestroyImmediate(mesh);
        }
    }
}
