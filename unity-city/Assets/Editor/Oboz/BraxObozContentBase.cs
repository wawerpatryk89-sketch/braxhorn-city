using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Owner-approved content-only preparation for the EXISTING Braxhorn project.
// Does NOT change package id, build player, install APK or touch city/estate.
// Original camp model/rig/animation files are never overwritten by this entry.
public static class BraxObozContentBase
{
    const string EditorVersion = "6000.0.58f2";
    public const string Zone = "oboz";

    [MenuItem("BRAXHORN/Obóz/Aktualizacje/Przygotuj bazę zawartości (BEZ APK)")]
    public static void Export()
    {
        if (Application.unityVersion != EditorVersion)
            throw new InvalidOperationException("Baza obozu wymaga sprawdzonego Unity " + EditorVersion);
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            throw new InvalidOperationException("Bazę trzeba przygotować w projekcie Unity Android");
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            throw new InvalidOperationException("Nie eksportuj podczas Play Mode / kompilacji");
        if (Directory.Exists(BraxContentBuild.Base) ||
            File.Exists(BraxContentBuild.Config))
            throw new InvalidOperationException("Baza Addressables już istnieje. Nie zmieniaj jej; kolejne poprawki korzystają z UpdateFromBaseline");
        if (File.Exists("content-patch.json"))
            throw new InvalidOperationException("Przed pierwszą bazą usuń niezwiązany content-patch.json; nie przenoś zmian innych regionów");
        if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID("Assets/Editor/Oboz/CharacterManifest.json")))
            throw new InvalidOperationException("Brakuje manifestu postaci obozu (nie generuj modeli zastępczych)");

        // The first Editor scene is sometimes untitled on CI; preserve rather
        // than silently throw it away before additive authoring.
        SaveUntitledSessions();
        BraxObozEnvironment.Prepare();
        BraxObozActors.Assemble();

        if (!File.Exists(BraxObozActors.ScenePath) ||
            !File.Exists(BraxObozActors.ScenePath + ".meta"))
            throw new FileNotFoundException("Brak zapisanej sceny obozu z trwałym GUID", BraxObozActors.ScenePath);
        var sceneGuid = AssetDatabase.AssetPathToGUID(BraxObozActors.ScenePath);
        if (string.IsNullOrEmpty(sceneGuid))
            throw new InvalidOperationException("Brak trwałego GUID sceny obozu");

        var previous = SceneManager.GetActiveScene();
        var loaded = EditorSceneManager.OpenScene(BraxObozActors.ScenePath, OpenSceneMode.Additive);
        try
        {
            var info = loaded.GetRootGameObjects()
                .SelectMany(o => o.GetComponentsInChildren<BraxObozSceneInfo>(true)).Single();
            var report = BraxObozValidation.Check(info);
            if (!report.structuralPassed)
                throw new InvalidOperationException("Obóz nie przechodzi walidacji struktury sceny: " +
                    string.Join("; ", report.structuralErrors));
        }
        finally
        {
            EditorSceneManager.CloseScene(loaded, true);
            if (previous.IsValid()) SceneManager.SetActiveScene(previous);
        }

        // Shared content implementation builds the immutable Addressables
        // baseline and content state. It DOES NOT call BuildPipeline.BuildPlayer.
        BraxContentBuild.Baseline(Zone, new[] { BraxObozActors.ScenePath });
        BraxObozUpdateRegistry.Export();
        string manifest = Path.Combine(BraxContentBuild.Base, "manifest.json");
        string state = Path.Combine(BraxContentBuild.Base, "addressables_content_state.bin");
        if (!File.Exists(manifest) || !File.Exists(state) || new FileInfo(state).Length == 0)
            throw new IOException("Nie powstała kompletna baza Addressables obozu");
        Debug.Log("BRAXHORN_OBOZ_CONTENT_BASE_READY zone=oboz APK_BUILT=NO phoneTested=NO " +
                  "sceneGuid=" + sceneGuid + " baseManifest=" + manifest);
    }

    // The Unity CI exporter is not an APK builder. game-ci uses manualExit
    // for validation-only methods and requires an explicit Editor exit code.
    public static void ExportInCI()
    {
        try
        {
            Export();
            AssetDatabase.SaveAssets();
            EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }

    static void SaveUntitledSessions()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded || !string.IsNullOrEmpty(scene.path)) continue;
            if (!Application.isBatchMode)
                throw new InvalidOperationException("Zapisz otwartą scenę Unity przed eksportem obozu");
            Directory.CreateDirectory("Assets/PlayableGenerated/Oboz");
            AssetDatabase.Refresh();
            var path = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/PlayableGenerated/Oboz/ContentBuildSession.unity");
            if (!EditorSceneManager.SaveScene(scene, path))
                throw new IOException("Nie zapisano sesji przed generowaniem obozu");
        }
    }
}
