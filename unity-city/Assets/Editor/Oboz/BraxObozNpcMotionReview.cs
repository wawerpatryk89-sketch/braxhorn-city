using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public static partial class BraxObozCharacters
{
    [Serializable] sealed class NpcMotionReview
    {
        public string unity, commit, captureMethod = "cpu-baked-mesh";
        public bool complete, phoneTested = false, appearanceApproved = false;
        public MotionReview[] characters;
        public string[] errors;
    }

    // CI-only comparison: never saves a scene, replaces a source or publishes cache/APK.
    public static void ReviewNpcMotion()
    {
        if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Próba NPC wymaga izolowanej sesji batch Unity");
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            throw new InvalidOperationException("Brak urządzenia graficznego do kontroli postaci");
        var manifest = ReadManifest();
        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../build/NpcMotionReview"));
        Directory.CreateDirectory(output);
        var report = new NpcMotionReview { unity = Application.unityVersion,
            commit = Environment.GetEnvironmentVariable("GITHUB_SHA") ?? "local" };
        var characters = new List<MotionReview>(); var errors = new List<string>();
        foreach (var spec in manifest.characters.Where(c => c.id != "monika"))
        {
            try { characters.Add(ReviewNpcCharacter(spec, output)); }
            catch (Exception ex) { errors.Add(spec.id + ": " + ex.GetType().Name + ": " + ex.Message); Debug.LogException(ex); }
        }
        report.characters = characters.ToArray(); report.errors = errors.ToArray();
        report.complete = characters.Count == 6 && characters.All(c => c.complete) && errors.Count == 0;
        File.WriteAllText(Path.Combine(output, "npc-motion-review.json"), JsonUtility.ToJson(report, true));
        if (!report.complete) throw new InvalidOperationException("Niepełny audyt ruchu sześciu NPC");
        Debug.Log("BRAXHORN_NPC_REVIEW complete=true characters=6 cases=60 phone_tested=false appearance_approved=false");
    }

    static MotionReview ReviewNpcCharacter(CharacterSpec spec, string output)
    {
        var report = new MotionReview { unity = Application.unityVersion, character = spec.id,
            commit = Environment.GetEnvironmentVariable("GITHUB_SHA") ?? "local", graphics = SystemInfo.graphicsDeviceType.ToString() };
        var cases = new List<MotionCase>(); var errors = new List<string>(); var temp = new List<Object>();
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(spec.modelPath);
        var imported = AssetDatabase.LoadAllAssetsAtPath(spec.modelPath).OfType<Avatar>().FirstOrDefault();
        if (!source || !imported || !imported.isValid || !imported.isHuman)
            throw new InvalidOperationException(spec.id + ": brak modelu/awatara");
        var hero = Object.Instantiate(source); temp.Add(hero); hero.name = spec.id + " motion review";
        hero.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity); hero.transform.localScale = Vector3.one;
        var previousAmbient = RenderSettings.ambientLight; var previousMode = RenderSettings.ambientMode;
        try
        {
            foreach (var a in hero.GetComponentsInChildren<Animator>(true)) Object.DestroyImmediate(a);
            foreach (var a in hero.GetComponentsInChildren<Animation>(true)) Object.DestroyImmediate(a);
            var bones = hero.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("mixamorig", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (bones.Length != 65) throw new InvalidOperationException(spec.id + ": oczekiwano 65 kości");
            var rest = bones.Select(t => new Rest { t = t, p = t.localPosition, r = t.localRotation, s = t.localScale }).ToArray();
            var hips = bones.Single(t => BoneName(t.name) == "Hips");
            string folder = Generated + "/" + spec.id;
            Directory.CreateDirectory(folder); AssetDatabase.Refresh();
            var material = Material(spec, folder); int index = 0;
            foreach (var renderer in hero.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (spec.id == "gorr") PrepareHandMask(renderer, folder + "/Hands_" + index + ".asset");
                index++;
                renderer.sharedMaterials = Enumerable.Repeat(material, renderer.sharedMesh.subMeshCount).ToArray();
                renderer.updateWhenOffscreen = true; renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
            var calibrated = CalibratedMotionAvatar(hero, imported); temp.Add(calibrated);
            var cameraGo = new GameObject("NPC motion review camera"); temp.Add(cameraGo); var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.12f,.14f,.17f);
            camera.orthographic = true; camera.nearClipPlane = .01f; camera.farClipPlane = 100; camera.cullingMask = 1 << 27;
            foreach (var t in hero.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 27;
            var sun = new GameObject("NPC motion review light"); temp.Add(sun); var light = sun.AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.3f; light.cullingMask = 1 << 27; sun.transform.rotation = Quaternion.Euler(35,-30,0);
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.5f,.5f,.5f);
            foreach (string name in new[] { "Idle", "Walk", "Run", "Punch", "Training" })
            foreach (string variant in new[] { "baseline", "candidate" })
            {
                var original = spec.clips.Single(c => c.name == name);
                bool locomotion = name == "Idle" || name == "Walk" || name == "Run";
                var clipSpec = variant == "candidate" && locomotion
                    ? new ClipSpec { name = name, path = "Assets/Source/Prywatne/Oboz/animations/casual/" + name + ".anim", donor = "Basic Motions", loop = true, inPlace = true }
                    : original;
                AnimationClip baked = null; Restore(rest);
                try
                {
                    baked = variant == "baseline" ? Bake(hero, imported, hips, rest, clipSpec) : BakeDirectMotion(hero, calibrated, hips, rest, clipSpec);
                    Validate(hero, hips, rest, baked);
                    var frames = new List<MotionFrame>(); var captures = new List<string>();
                    for (int i = 0; i < 60; i++)
                    {
                        Restore(rest); float time = baked.length * i / 60f; baked.SampleAnimation(hero, time);
                        frames.Add(MeasureMotion(bones, time));
                        if (i % 5 != 0) continue;
                        using (var snapshot = new MotionSnapshot(hero))
                        foreach (string view in new[] { "front", "back", "side" })
                        {
                            string file = spec.id + "-" + variant + "-" + name + "-" + view + "-" + i.ToString("D2") + ".png";
                            CaptureMotion(snapshot.renderers, camera, view, Path.Combine(output, file)); captures.Add(file);
                        }
                    }
                    cases.Add(new MotionCase { variant = variant, clip = name, source = clipSpec.path, frames = frames.ToArray(), captures = captures.ToArray() });
                    Debug.Log("BRAXHORN_NPC_REVIEW " + spec.id + " " + variant + " " + name + " samples=" + frames.Count);
                }
                catch (Exception ex) { errors.Add(variant + "/" + name + ": " + ex.GetType().Name + ": " + ex.Message); Debug.LogException(ex); }
                finally { if (baked) Object.DestroyImmediate(baked); Restore(rest); }
            }
            report.complete = cases.Count == 10 && errors.Count == 0;
        }
        finally
        {
            RenderSettings.ambientLight = previousAmbient; RenderSettings.ambientMode = previousMode;
            report.cases = cases.ToArray(); report.errors = errors.ToArray();
            File.WriteAllText(Path.Combine(output, spec.id + "-motion-review.json"), JsonUtility.ToJson(report, true));
            foreach (var obj in temp) if (obj) Object.DestroyImmediate(obj);
        }
        return report;
    }
}
