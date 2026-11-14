using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

internal static class BraxMixamoT
{
    internal const string Root = "Assets/Source/Prywatne/MixamoT/";
    [Serializable] private sealed class Profile { public Entry[] clips; }
    [Serializable] private sealed class Entry { public string state; public string file; }

    internal static Dictionary<string, string> Read(string character)
    {
        string path = Root + character + ".json";
        var profile = JsonUtility.FromJson<Profile>(File.ReadAllText(path));
        return profile.clips.ToDictionary(entry => entry.state, entry => Root + entry.file);
    }

    // Measure the reference pose configured by the Unity importer. The owner's
    // FBX, skin bind matrices and animation curves are never written.
    internal static void PrepareModel(string modelPath)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        var importer = (ModelImporter)AssetImporter.GetAtPath(modelPath);
        var description = model.GetComponent<Animator>().avatar.humanDescription;
        var skeleton = description.skeleton;
        var instance = UnityEngine.Object.Instantiate(model);
        bool changed = false;
        try
        {
            var animator = instance.GetComponent<Animator>();
            animator.enabled = false;
            var transforms = instance.GetComponentsInChildren<Transform>(true);
            var names = transforms.Skip(1).GroupBy(t => t.name)
                .Where(g => g.Count() == 1).ToDictionary(g => g.Key, g => g.First());
            for (int i = 0; i < skeleton.Length; i++)
            {
                if (!names.TryGetValue(skeleton[i].name, out var joint)) continue;
                joint.localPosition = skeleton[i].position;
                joint.localRotation = skeleton[i].rotation;
                joint.localScale = skeleton[i].scale;
            }
            var map = description.human.Where(b => names.ContainsKey(b.boneName)).ToDictionary(b => b.humanName, b => names[b.boneName]);
            Transform Bone(HumanBodyBones b) => map[HumanTrait.BoneName[(int)b]];
            var right = (Bone(HumanBodyBones.RightUpperLeg).position - Bone(HumanBodyBones.LeftUpperLeg).position).normalized;
            var up = Vector3.ProjectOnPlane(Bone(HumanBodyBones.Neck).position - Bone(HumanBodyBones.Hips).position, right).normalized;
            var forward = Vector3.Cross(right, up).normalized;
            var feet = Bone(HumanBodyBones.LeftToes).position - Bone(HumanBodyBones.LeftFoot).position +
                       Bone(HumanBodyBones.RightToes).position - Bone(HumanBodyBones.RightFoot).position;
            if (Vector3.Dot(forward, feet) < 0) forward = -forward;
            void AlignHand(HumanBodyBones h, HumanBodyBones m, HumanBodyBones idx, HumanBodyBones little)
            {
                var hand = Bone(h);
                var along = (Bone(m).position - hand.position).normalized;
                var across = Vector3.ProjectOnPlane(Bone(idx).position - Bone(little).position, along).normalized;
                var goal = Vector3.ProjectOnPlane(forward, along).normalized;
                if (across.sqrMagnitude < .9f || goal.sqrMagnitude < .9f)
                    throw new InvalidDataException(modelPath + ": nierozpoznany kierunek dłoni");
                float twist = Vector3.SignedAngle(across, goal, along);
                if (Mathf.Abs(twist) < 1f) return;
                hand.rotation = Quaternion.AngleAxis(twist, along) * hand.rotation;
                int i = Array.FindIndex(skeleton, entry => entry.name == hand.name);
                if (i < 0) throw new InvalidDataException(modelPath + ": brak kości " + hand.name);
                skeleton[i].rotation = hand.localRotation;
                changed = true;
                Debug.Log("MIXAMO_T_DŁOŃ " + modelPath + " " + h + " " + twist.ToString("F1"));
            }
            AlignHand(HumanBodyBones.LeftHand, HumanBodyBones.LeftMiddleProximal, HumanBodyBones.LeftIndexProximal, HumanBodyBones.LeftLittleProximal);
            AlignHand(HumanBodyBones.RightHand, HumanBodyBones.RightMiddleProximal, HumanBodyBones.RightIndexProximal, HumanBodyBones.RightLittleProximal);
        }
        finally { UnityEngine.Object.DestroyImmediate(instance); }
        if (!changed) return;
        description.skeleton = skeleton;
        importer.humanDescription = description;
        importer.SaveAndReimport();
    }

    internal static bool FootIK(string state) => state == "Walk" || state == "Run" || state == "TrainingJog";

    // Validate the two hands as well as mandatory Humanoid bones. Unity considers
    // fingers optional, but gesturing and the measured palm orientation require them.
    internal static Avatar CheckAvatar(GameObject model)
    {
        var animator = model.GetComponent<Animator>();
        if (!animator || !animator.avatar || !animator.avatar.isHuman || !animator.avatar.isValid)
            throw new InvalidDataException("Niepoprawny Avatar Humanoid: " + model.name);
        foreach (var bone in new[] { HumanBodyBones.LeftHand, HumanBodyBones.RightHand,
            HumanBodyBones.LeftIndexProximal, HumanBodyBones.RightIndexProximal,
            HumanBodyBones.LeftMiddleProximal, HumanBodyBones.RightMiddleProximal,
            HumanBodyBones.LeftLittleProximal, HumanBodyBones.RightLittleProximal })
            if (!animator.GetBoneTransform(bone))
                throw new InvalidDataException(model.name + ": brak mapowania " + bone);
        return animator.avatar;
    }

    // Each source file has one original take. Distinct uses of the same take
    // (for example full/truncated warm-up) are separate subclips of that one FBX.
    internal static AnimationClip Load(string path, string state, bool loop, bool shortWarmUp = false)
    {
        var importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (!importer || importer.animationType != ModelImporterAnimationType.Human)
            throw new InvalidDataException("Wymagany FBX Humanoid: " + path);
        var defaults = importer.defaultClipAnimations;
        if (defaults.Length != 1) throw new InvalidDataException("Wymagane pojedyncze ujęcie: " + path);
        string name = state + (shortWarmUp ? "_Fitness" : "") + (loop ? "_Loop" : "_Once");
        var desired = defaults[0];
        desired.name = name;
        desired.loopTime = loop;
        desired.lockRootHeightY = desired.keepOriginalPositionY = true;
        desired.lockRootRotation = desired.keepOriginalOrientation = true;
        desired.lockRootPositionXZ = false;
        desired.keepOriginalPositionXZ = true;
        if (state == "Idle" || state == "KickBall") desired.firstFrame += 1;
        if (state == "StandUp")
        {
            desired.firstFrame = Mathf.Round(Mathf.Lerp(desired.firstFrame, desired.lastFrame, .82f));
            desired.lastFrame -= Mathf.Round(.025f * (desired.lastFrame - desired.firstFrame));
        }
        if (shortWarmUp)
        {
            var take = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c => !c.name.StartsWith("__preview__"));
            desired.lastFrame = Mathf.Min(desired.lastFrame, desired.firstFrame + Mathf.Round(2.1f * take.frameRate));
        }
        var configured = importer.clipAnimations.ToList();
        var existing = configured.Find(c => c.name == name);
        if (existing == null || existing.firstFrame != desired.firstFrame || existing.lastFrame != desired.lastFrame ||
            existing.loopTime != loop || !existing.lockRootHeightY || !existing.keepOriginalPositionY ||
            !existing.lockRootRotation || !existing.keepOriginalOrientation || existing.lockRootPositionXZ || !existing.keepOriginalPositionXZ)
        {
            configured.RemoveAll(c => c.name == name);
            configured.Add(desired);
            importer.clipAnimations = configured.ToArray();
            importer.SaveAndReimport();
        }
        var clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Single(c => c.name == name);
        if (!clip.humanMotion || clip.length <= 0) throw new InvalidDataException("Pusty klip Humanoid: " + path);
        return clip;
    }
}
