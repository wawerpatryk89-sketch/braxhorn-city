using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Braxhorn.Characters;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

internal static class BraxGitaraSetup
{
    internal const string Root = "Assets/Source/Prywatne/Postacie/gitara/";
    internal static readonly string[] Actions = {
        "Idle", "Walk", "Run", "Jump", "SitDown", "SeatedIdle", "StandUp", "Talk", "Wave", "Point",
        "CombatIdle", "Jab", "Cross", "Hook", "SideKick", "Knee", "Dodge", "Block", "HitHead", "HitBody"
    };
    static readonly string[] Attacks = { "Jab", "Cross", "Hook", "SideKick", "Knee" };
    internal static readonly HashSet<string> Loops = new HashSet<string> { "Idle", "Walk", "Run", "SeatedIdle", "CombatIdle" };

    [MenuItem("Braxhorn/Postacie/Przygotuj Gitarę")]
    internal static void Create()
    {
        var clips = new Dictionary<string, AnimationClip>();
        var model = Root + "Gitara.fbx";
        ConfigureModel(model, null);
        foreach (string id in Actions)
        {
            string path = Root + "Ruchy/" + id + ".fbx";
            ConfigureModel(path, id);
            var clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .SingleOrDefault(c => c.name == id);
            if (!clip || !clip.humanMotion) throw new InvalidOperationException("Brak klipu Humanoid: " + id);
            clips.Add(id, clip);
        }

        const string prefabPath = Root + "Gitara.prefab";
        const string controllerPath = Root + "Gitara.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (!controller)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.AddParameter("Combat", AnimatorControllerParameterType.Bool);
            var machine = controller.layers[0].stateMachine;
            var states = new Dictionary<string, AnimatorState>();
            foreach (var id in Actions)
            {
                var state = machine.AddState(id);
                state.motion = clips[id];
                states.Add(id, state);
            }
            machine.defaultState = states["Idle"];
            foreach (var id in Actions)
            {
                if (Loops.Contains(id)) continue;
                if (id == "SitDown") AddExit(states[id], states["SeatedIdle"]);
                else if (Attacks.Contains(id) || id == "Dodge" || id == "Block" || id.StartsWith("Hit", StringComparison.Ordinal))
                {
                    AddExit(states[id], states["CombatIdle"]).AddCondition(AnimatorConditionMode.If, 0, "Combat");
                    AddExit(states[id], states["Idle"]).AddCondition(AnimatorConditionMode.IfNot, 0, "Combat");
                }
                else AddExit(states[id], states["Idle"]);
            }
        }
        var avatar = AssetDatabase.LoadAllAssetsAtPath(model).OfType<Avatar>().FirstOrDefault(a => a.isValid && a.isHuman);
        if (!avatar) throw new InvalidOperationException("Brak prawidłowego avatara Gitary.");
        var mat = ConfigureMaterial();
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(model));
        try
        {
            var animator = instance.GetComponent<Animator>() ?? instance.AddComponent<Animator>();
            animator.avatar = avatar;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            instance.AddComponent<BraxResidentMotion>().SetAttackList(Attacks);
            foreach (var skin in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                skin.sharedMaterials = Enumerable.Repeat(mat, skin.sharedMaterials.Length).ToArray();
            if (!PrefabUtility.SaveAsPrefabAsset(instance, prefabPath))
                throw new IOException("Nie zapisano prefabu Gitary.");
        }
        finally { UnityEngine.Object.DestroyImmediate(instance); }
        AssetDatabase.SaveAssets();
        Debug.Log("Gitara: prefab globalny, 20 klipów Humanoid. " + prefabPath);
    }

    static AnimatorStateTransition AddExit(AnimatorState from, AnimatorState to)
    {
        var transition = from.AddTransition(to);
        transition.hasExitTime = true;
        transition.exitTime = 0.97f;
        transition.hasFixedDuration = true;
        transition.duration = 0.10f;
        return transition;
    }

    static void ConfigureModel(string path, string id)
    {
        var importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (!importer) throw new FileNotFoundException(path);
        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.optimizeGameObjects = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.importAnimation = id != null;
        importer.SaveAndReimport();
        if (id == null) return;
        var clips = importer.defaultClipAnimations;
        if (clips.Length != 1) throw new InvalidOperationException("Liczba ujęć w " + path + ": " + clips.Length);
        var clip = clips[0];
        clip.name = id;
        clip.loopTime = Loops.Contains(id);
        // Vertical hip movement is kept on the body for standing up, sitting and jumping.
        clip.lockRootHeightY = true;
        clip.keepOriginalPositionY = true;
        clip.lockRootRotation = true;
        clip.keepOriginalOrientation = true;
        // Horizontal locomotion is supplied by the character motor.
        clip.lockRootPositionXZ = false;
        clip.keepOriginalPositionXZ = true;
        if (id == "StandUp")
        {
            float first = clip.firstFrame, last = clip.lastFrame;
            clip.firstFrame = Mathf.Round(Mathf.Lerp(first, last, 0.82f));
            clip.lastFrame = last - Mathf.Round(0.025f * (last - clip.firstFrame));
        }
        importer.clipAnimations = clips;
        importer.SaveAndReimport();
    }

    static Texture2D Texture(string file, TextureImporterType type, bool srgb)
    {
        string path = Root + file;
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (!importer) throw new FileNotFoundException(path);
        importer.textureType = type;
        importer.sRGBTexture = srgb;
        importer.mipmapEnabled = true;
        importer.streamingMipmaps = true;
        var android = importer.GetPlatformTextureSettings("Android");
        android.name = "Android";
        android.maxTextureSize = 1024;
        android.overridden = true;
        importer.SetPlatformTextureSettings(android);
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static Material ConfigureMaterial()
    {
        var color = Texture("Gitara_BaseColor.png", TextureImporterType.Default, true);
        var normal = Texture("Gitara_Normal.png", TextureImporterType.NormalMap, false);
        var packed = Texture("Gitara_MetallicSmoothness.png", TextureImporterType.Default, false);
        var path = Root + "Gitara.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!mat)
        {
            var shader = Shader.Find("Standard");
            if (!shader) throw new InvalidOperationException("Brak shadera Standard.");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetTexture("_MainTex", color);
        mat.SetTexture("_BumpMap", normal);
        mat.EnableKeyword("_NORMALMAP");
        mat.SetTexture("_MetallicGlossMap", packed);
        mat.SetFloat("_GlossMapScale", 1f);
        mat.EnableKeyword("_METALLICGLOSSMAP");
        EditorUtility.SetDirty(mat);
        return mat;
    }
}
