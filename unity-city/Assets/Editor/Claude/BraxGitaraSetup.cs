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
    private const string Characters = "Assets/Source/Prywatne/Postacie/";
    internal static readonly string[] Actions = {
        "Idle", "Walk", "Run", "Jump", "SitDown", "SeatedIdle", "StandUp", "Talk", "Wave", "Point",
        "CombatIdle", "Jab", "Cross", "Hook", "SideKick", "Knee", "Dodge", "Block", "HitHead", "HitBody",
        "DanceHouse", "DanceHipHop", "Drink", "DrunkIdle", "DrunkWalk", "DrunkTurn"
    };
    static readonly string[] Attacks = { "Jab", "Cross", "Hook", "SideKick", "Knee" };
    internal static readonly HashSet<string> Loops = new HashSet<string> { "Idle", "Walk", "Run", "SeatedIdle", "CombatIdle", "DanceHouse", "DanceHipHop", "DrunkIdle", "DrunkWalk", "TrainingJog", "Swim", "TreadWater", "Helm", "LoungerRest" };

    [MenuItem("Braxhorn/Postacie/Przygotuj Gitarę")]
    internal static void Create() => Prepare("gitara", "Gitara");

    [MenuItem("Braxhorn/Postacie/Przygotuj Kilera")]
    internal static void CreateKiler() => Prepare("kiler", "Kiler");

    [MenuItem("Braxhorn/Postacie/Przygotuj Kamila")]
    internal static void CreateKamil() => Prepare("kamil", "Kamil");

    [MenuItem("Braxhorn/Postacie/Przygotuj Mateusza")]
    internal static void CreateMateusz() => Prepare("mateusz", "Mateusz");

    static readonly string[] FitnessActions = {
        "Idle", "Walk", "Run", "Jump", "SitDown", "SeatedIdle", "StandUp", "Talk", "Wave", "Point",
        "Drink", "WarmUp", "TrainingJog", "DanceHouse", "DanceHipHop"
    };

    [MenuItem("Braxhorn/Postacie/Przygotuj Anetę")]
    internal static void CreateAneta() => Prepare("aneta", "Aneta", FitnessActions, Array.Empty<string>());

    static readonly string[] EverydayAndDance = {
        "Idle", "Walk", "Run", "Jump", "SitDown", "SeatedIdle", "StandUp", "Talk", "Wave", "Point",
        "Drink", "DanceHouse", "DanceHipHop"
    };

    [MenuItem("Braxhorn/Postacie/Przygotuj Monikę")]
    internal static void CreateMonika() => Prepare("monika", "Monika", EverydayAndDance, Array.Empty<string>());

    [MenuItem("Braxhorn/Postacie/Przygotuj Wiecha")]
    internal static void CreateWiechu() => Prepare("wiechu", "Wiechu", EverydayAndDance, Array.Empty<string>());

    [MenuItem("Braxhorn/Postacie/Przygotuj Ewę")]
    internal static void CreateEwa() => Prepare("ewa", "Ewa", EverydayAndDance, Array.Empty<string>());

    [MenuItem("Braxhorn/Postacie/Przygotuj Martynę")]
    internal static void CreateMartyna() => Prepare("martyna", "Martyna", EverydayAndDance, Array.Empty<string>());

    static readonly string[] ResidentBasics = { "Idle", "Walk", "SitDown", "SeatedIdle", "StandUp", "Talk", "Wave", "Point" };

    [MenuItem("Braxhorn/Postacie/Chłopiec w okularach")]
    internal static void CreateChlopiec003f4e46() => Prepare("dziecko_003f4e46", "Chlopiec003f4e46",
        ResidentBasics.Concat(new[] { "Run", "Jump", "KickBall" }).ToArray(), Array.Empty<string>());

    [MenuItem("Braxhorn/Postacie/Kapitan w białym mundurze")]
    internal static void CreateKapitan02d85d24() => Prepare("kapitan_02d85d24", "Kapitan02d85d24",
        ResidentBasics.Concat(new[] { "Helm", "TrainingJog" }).ToArray(), Array.Empty<string>());

    [MenuItem("Braxhorn/Postacie/Plażowicz w granatowych kąpielówkach")]
    internal static void CreatePlazowicz0493f781() => Prepare("plazowicz_0493f781", "Plazowicz0493f781",
        ResidentBasics.Concat(new[] { "Swim", "TreadWater", "SwimToEdge", "LoungerRest" }).ToArray(), Array.Empty<string>());

    [MenuItem("Braxhorn/Postacie/Zadymiarz z głową psa")]
    internal static void CreateZadymiarz021fef69() => Prepare("zadymiarz_021fef69", "Zadymiarz021fef69",
        ResidentBasics.Concat(new[] { "Run", "Jump", "CombatIdle", "Jab", "Cross", "Hook", "SideKick", "Knee", "Dodge", "Block", "HitHead", "HitBody" }).ToArray(), Attacks);

    static void Prepare(string characterId, string displayName) => Prepare(characterId, displayName, Actions, Attacks);

    static void Prepare(string characterId, string displayName, string[] actions, string[] attacks)
    {
        string root = Characters + characterId + "/";
        string prefix = root + displayName;
        var clips = new Dictionary<string, AnimationClip>();
        var model = prefix + ".fbx";
        ConfigureModel(model, null);
        var gym = BraxGymProfiles.For(characterId);
        var allActions = actions.Concat(gym.Select(id => "Gym" + id)).ToArray();
        foreach (string id in allActions)
        {
            string path = id.StartsWith("Gym", StringComparison.Ordinal)
                ? BraxGymProfiles.ResidentClips + id.Substring(3) + ".fbx"
                : root + "Ruchy/" + id + ".fbx";
            ConfigureModel(path, id);
            var clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .SingleOrDefault(c => c.name == id);
            if (!clip || !clip.humanMotion) throw new InvalidOperationException("Brak klipu Humanoid: " + id);
            clips.Add(id, clip);
        }

        string prefabPath = prefix + ".prefab";
        string controllerPath = prefix + ".controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (!controller) controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        if (attacks.Length > 0 && !controller.parameters.Any(p => p.name == "Combat"))
            controller.AddParameter("Combat", AnimatorControllerParameterType.Bool);
        var machine = controller.layers[0].stateMachine;
        var states = machine.states.ToDictionary(s => s.state.name, s => s.state);
        var added = new List<string>();
        foreach (string id in allActions)
        {
            if (!states.TryGetValue(id, out var state))
            {
                state = machine.AddState(id);
                states.Add(id, state);
                added.Add(id);
            }
            if (characterId == "kapitan_02d85d24" && id == "TrainingJog") state.speed = 0.80f;
            state.motion = clips[id];
        }
        machine.defaultState = states["Idle"];
        foreach (string id in added)
        {
            if (Loops.Contains(id)) continue;
            if (id == "SitDown") AddExit(states[id], states["SeatedIdle"]);
            else if (id == "SwimToEdge") AddExit(states[id], states["TreadWater"]);
            else if (id == "DrunkTurn") AddExit(states[id], states["DrunkIdle"]);
            else if (attacks.Contains(id) || id == "Dodge" || id == "Block" || id.StartsWith("Hit", StringComparison.Ordinal))
            {
                AddExit(states[id], states["CombatIdle"]).AddCondition(AnimatorConditionMode.If, 0, "Combat");
                AddExit(states[id], states["Idle"]).AddCondition(AnimatorConditionMode.IfNot, 0, "Combat");
            }
            else AddExit(states[id], states["Idle"]);
        }
        EditorUtility.SetDirty(controller);
        var avatar = AssetDatabase.LoadAllAssetsAtPath(model).OfType<Avatar>().FirstOrDefault(a => a.isValid && a.isHuman);
        if (!avatar) throw new InvalidOperationException("Brak prawidłowego avatara: " + displayName);
        var mat = ConfigureMaterial(prefix);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(model));
        try
        {
            var animator = instance.GetComponent<Animator>() ?? instance.AddComponent<Animator>();
            animator.avatar = avatar;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            instance.AddComponent<BraxResidentMotion>().SetAttackList(attacks);
            foreach (var skin in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                skin.sharedMaterials = Enumerable.Repeat(mat, skin.sharedMaterials.Length).ToArray();
            if (!PrefabUtility.SaveAsPrefabAsset(instance, prefabPath))
                throw new IOException("Nie zapisano prefabu: " + displayName);
        }
        finally { UnityEngine.Object.DestroyImmediate(instance); }
        AssetDatabase.SaveAssets();
        Debug.Log(displayName + ": " + allActions.Length + " klipów Humanoid. " + prefabPath);
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

    static Texture2D Texture(string path, TextureImporterType type, bool srgb)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (!importer) throw new FileNotFoundException(path);
        importer.textureType = type;
        importer.sRGBTexture = srgb;
        importer.maxTextureSize = Math.Min(importer.maxTextureSize, 2048);
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

    static Material ConfigureMaterial(string prefix)
    {
        var color = Texture(prefix + "_BaseColor.png", TextureImporterType.Default, true);
        var normal = Texture(prefix + "_Normal.png", TextureImporterType.NormalMap, false);
        var packed = Texture(prefix + "_MetallicSmoothness.png", TextureImporterType.Default, false);
        var path = prefix + ".mat";
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
