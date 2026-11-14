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
        string output = Path.GetFullPath("../KontrolaPostaciT");
        Directory.CreateDirectory(output);
        foreach (string profile in Directory.GetFiles(BraxMixamoT.Root, "*.json").OrderBy(p => p))
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
                            playable.SetApplyFootIK(false);
                            AnimationPlayableOutput.Create(graph, "test", animator).SetSourcePlayable(playable);
                            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                            graph.Play();
                        }
                        for (int i = 0; i < 5; i++)
                        {
                            float t = clip.length * i / 5f;
                            if (legacy) clip.SampleAnimation(instance, t);
                            else { playable.SetTime(t); graph.Evaluate(0); }
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

    static void Capture(GameObject actor, SkinnedMeshRenderer[] skins, string output, string id, string action, int index)
    {
        var preview = new PreviewRenderUtility();
        try
        {
            // Add an independent copy to avoid moving the live animated skeleton between scenes.
            var clone = Object.Instantiate(actor);
            preview.AddSingleGO(clone);
            var original = actor.GetComponentsInChildren<Transform>(true);
            var copy = clone.GetComponentsInChildren<Transform>(true);
            foreach (var a in clone.GetComponentsInChildren<Animator>()) a.enabled = false;
            foreach (var a in clone.GetComponentsInChildren<Animation>()) a.enabled = false;
            for (int i = 0; i < copy.Length; i++)
            {
                copy[i].localPosition = original[i].localPosition;
                copy[i].localRotation = original[i].localRotation;
                copy[i].localScale = original[i].localScale;
            }
            var bounds = skins[0].bounds;
            foreach (var mesh in skins.Skip(1)) bounds.Encapsulate(mesh.bounds);
            var camera = preview.camera;
            camera.orthographic = true;
            camera.orthographicSize = bounds.extents.y * 1.18f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.26f, .29f, .32f);
            Vector3 direction = new Vector3(.4f, .08f, 1).normalized;
            var face = original.FirstOrDefault(t => t.name.EndsWith("LeftArm"));
            var right = original.FirstOrDefault(t => t.name.EndsWith("RightArm"));
            if (face && right)
            {
                Vector3 side = (right.position - face.position).normalized;
                direction = (Vector3.Cross(side, Vector3.up) + side * .4f + Vector3.up * .08f).normalized;
            }
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
            try { File.WriteAllBytes(Path.Combine(output, id + "_" + action + "_" + index + ".png"), image.EncodeToPNG()); }
            finally { Object.DestroyImmediate(image); }
        }
        finally { preview.Cleanup(); }
    }
}
