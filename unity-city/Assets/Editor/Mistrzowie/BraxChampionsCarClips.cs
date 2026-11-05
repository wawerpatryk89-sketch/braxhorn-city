using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// The owner's THREE original Mixamo 65-bone animations are rebound to Brax's
// ORIGINAL Mixamo-named skeleton. No replacement mesh, T-pose conversion,
// guessed bone orientation or root-motion teleport is allowed.
public static class BraxChampionsCarClips
{
    const string Source = "Assets/Source/Mistrzowie/Vehicles/Animations/";
    const string Output = "Assets/PlayableGenerated/Mistrzowie/Brax/";
    static readonly (string fbx, string name, bool loop)[] Clips = {
        ("Entering Car.fbx", "EnteringCar", false),
        ("Driving.fbx", "Driving", true),
        ("Exiting Car.fbx", "ExitingCar", false)
    };

    static string Bone(string full)
    {
        int colon = full.LastIndexOf(':');
        string s = colon >= 0 ? full.Substring(colon + 1) : full;
        return s.Trim();
    }

    public static void Attach(GameObject originalBrax, Animation animation)
    {
        if (!originalBrax || !animation)
            throw new InvalidOperationException("Original Brax rig/legacy Animation is required");
        // Do not touch Patryk's skeleton or any existing basic Brax clips.
        var targets = new Dictionary<string, Transform>(StringComparer.OrdinalIgnoreCase);
        foreach (var t in animation.GetComponentsInChildren<Transform>(true))
        {
            string key = Bone(t.name);
            if (!targets.ContainsKey(key)) targets.Add(key, t);
        }
        foreach (string required in new[] { "Hips", "Spine", "LeftUpLeg",
                     "RightUpLeg", "LeftLeg", "RightLeg", "LeftArm", "RightArm" })
            if (!targets.ContainsKey(required))
                throw new InvalidOperationException("Original Brax Mixamo bone missing: " + required);

        foreach (var spec in Clips)
        {
            string path = Source + spec.fbx;
            if (!File.Exists(path))
                throw new FileNotFoundException("Owner's original Mixamo car FBX not restored", path);
            var source = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .Where(c => c && c.length > .01f && AnimationUtility.GetCurveBindings(c).Length > 0)
                .OrderByDescending(c => AnimationUtility.GetCurveBindings(c).Length)
                .FirstOrDefault();
            if (!source)
                throw new InvalidOperationException("Unity has no real curves in original Mixamo " + path);
            var clip = new AnimationClip {
                name = spec.name, legacy = true, frameRate = source.frameRate > 0 ? source.frameRate : 30f,
                wrapMode = spec.loop ? WrapMode.Loop : WrapMode.Once
            };
            var mappedBones = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int mapped = 0, skipped = 0;
            foreach (var binding in AnimationUtility.GetCurveBindings(source))
            {
                // Mixamo FBX supplies native animated bone rotations. Animation
                // translations belong to the standalone FBX's size/origin, NOT
                // to the original Brax GLB; apply ingress/egress motion in car code.
                string property = binding.propertyName;
                if (!property.Contains("Rotation") && !property.Contains("rotation"))
                    continue;
                string[] parts = binding.path.Replace('\\', '/').Split('/');
                string bone = Bone(parts[parts.Length - 1]);
                if (!targets.TryGetValue(bone, out var t)) { skipped++; continue; }
                var mappedBinding = binding;
                mappedBinding.path = AnimationUtility.CalculateTransformPath(t, animation.transform);
                mappedBinding.type = typeof(Transform);
                AnimationUtility.SetEditorCurve(clip, mappedBinding, AnimationUtility.GetEditorCurve(source, binding));
                mappedBones.Add(bone);
                mapped++;
            }
            if (mapped < 100 || mappedBones.Count < 35 ||
                !mappedBones.Contains("Hips") || !mappedBones.Contains("LeftUpLeg") ||
                !mappedBones.Contains("RightUpLeg") || !mappedBones.Contains("Spine"))
                throw new InvalidOperationException("Mixamo skeleton/curve mismatch for " +
                    spec.name + ": mapped=" + mapped + " bones=" + mappedBones.Count +
                    " skipped=" + skipped);
            clip.EnsureQuaternionContinuity();
            BraxZapisAssetu.Utworz(clip, Output + spec.name + ".anim");
            var saved = AssetDatabase.LoadAssetAtPath<AnimationClip>(Output + spec.name + ".anim");
            if (!saved || saved.length <= .01f)
                throw new InvalidOperationException("Owner's Mixamo car clip not saved: " + spec.name);
            saved.legacy = true;
            animation.AddClip(saved, spec.name);
            Debug.Log("A1_OWNER_MIXAMO_CAR " + spec.name + " source=" + spec.fbx +
                " originalRig=BRAX mappedBones=" + mappedBones.Count +
                " mappedCurves=" + mapped + " skipped=" + skipped);
        }
    }
}

public sealed class BraxChampionsMixamoCarImporter : AssetPostprocessor
{
    void OnPreprocessModel()
    {
        if (!assetPath.StartsWith("Assets/Source/Mistrzowie/Vehicles/Animations/",
                StringComparison.Ordinal) || !assetPath.EndsWith(".fbx",
                StringComparison.OrdinalIgnoreCase)) return;
        var model = assetImporter as ModelImporter;
        if (model == null) throw new InvalidOperationException("Owner original Mixamo FBX importer missing");
        model.importAnimation = true;
        model.animationType = ModelImporterAnimationType.Legacy;
        model.animationCompression = ModelImporterAnimationCompression.Optimal;
    }
}
