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
        ResidentBasics.Concat(new[] { "Swim", "SwimToEdge" }).ToArray(), Array.Empty<string>());

    [MenuItem("Braxhorn/Postacie/Zadymiarz z głową psa")]
    internal static void CreateZadymiarz021fef69() => Prepare("zadymiarz_021fef69", "Zadymiarz021fef69",
        ResidentBasics.Concat(new[] { "Run", "Jump", "CombatIdle", "Jab", "Cross", "Hook", "SideKick", "Knee", "Dodge", "Block", "HitHead", "HitBody" }).ToArray(), Attacks);

    internal static void CreateFromProfile(string id, string model)
    {
        var actions = BraxMixamoT.Read(id).Keys.ToArray();
        Prepare(id, model, actions, Attacks.Where(actions.Contains).ToArray());
    }

    static void Prepare(string characterId, string displayName) => Prepare(characterId, displayName, Actions, Attacks);

    static void Prepare(string characterId, string displayName, string[] actions, string[] attacks)
    {
        string root = Characters + characterId + "/";
        string prefix = root + displayName;
        var clips = new Dictionary<string, AnimationClip>();
        var model = prefix + ".fbx";
        BraxMixamoT.PrepareModel(model);
        var profile = BraxMixamoT.Read(characterId);
        if (!new HashSet<string>(actions).SetEquals(profile.Keys))
            throw new InvalidDataException("Niezgodny profil ruchów: " + characterId);
        var gym = BraxGymProfiles.For(characterId);
        var allActions = actions.Concat(gym.Select(id => "Gym" + id)).ToArray();
        foreach (string id in allActions)
        {
            string path = id.StartsWith("Gym", StringComparison.Ordinal)
                ? BraxGymProfiles.ResidentClips + id.Substring(3) + ".fbx" : profile[id];
            clips.Add(id, BraxMixamoT.Load(path, id, Loops.Contains(id), characterId == "aneta" && id == "WarmUp"));
        }

        string prefabPath = prefix + ".prefab";
        string controllerPath = prefix + ".controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (!controller) controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        if (attacks.Length > 0 && !controller.parameters.Any(p => p.name == "Combat"))
            controller.AddParameter("Combat", AnimatorControllerParameterType.Bool);
        var machine = controller.layers[0].stateMachine;
        // Remove retired gameplay states when upgrading an existing generated prefab.
        foreach (var child in machine.states)
            if (!allActions.Contains(child.state.name)) machine.RemoveState(child.state);
        var states = machine.states.ToDictionary(s => s.state.name, s => s.state);
        foreach (string id in allActions)
        {
            if (!states.TryGetValue(id, out var state))
            {
                state = machine.AddState(id);
                states.Add(id, state);
            }
            if (characterId == "kapitan_02d85d24" && id == "TrainingJog") state.speed = 0.80f;
            state.iKOnFeet = BraxMixamoT.FootIK(id);
            state.motion = clips[id];
        }
        machine.defaultState = states["Idle"];
        foreach (string id in allActions)
        {
            foreach (var previous in states[id].transitions) states[id].RemoveTransition(previous);
            if (Loops.Contains(id)) continue;
            if (id == "SitDown") AddExit(states[id], states["SeatedIdle"]);
            else if (id == "SwimToEdge") AddExit(states[id], states["Swim"]);
            else if (id == "DrunkTurn") AddExit(states[id], states["DrunkIdle"]);
            else if (attacks.Contains(id) || id == "Dodge" || id == "Block" || id.StartsWith("Hit", StringComparison.Ordinal))
            {
                AddExit(states[id], states["CombatIdle"]).AddCondition(AnimatorConditionMode.If, 0, "Combat");
                AddExit(states[id], states["Idle"]).AddCondition(AnimatorConditionMode.IfNot, 0, "Combat");
            }
            else AddExit(states[id], states["Idle"]);
        }
        EditorUtility.SetDirty(controller);
        var mat = ConfigureMaterial(prefix);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(model));
        try
        {
            var animator = instance.GetComponent<Animator>();
            animator.avatar = BraxMixamoT.CheckAvatar(instance);
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
