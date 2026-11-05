using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Camp authoring inventory, NOT an Addressables catalog or a content release.
// Does not generate scenes, change Build Settings, publish or build a player.
public static class BraxObozUpdateRegistry
{
    public const string Output = "ObozUpdateExport/registry.json";
    const string StandardCommit = "38422c5f031b5f9b85a3e1981c1a8199c9f75731";
    [Serializable] public sealed class FileRecord { public string path, sha256; public long bytes; }
    [Serializable] public sealed class Entry
    {
        public string address, path, guid;
        public string[] dependencies;
    }
    [Serializable] public sealed class Registry
    {
        public int schema = 1;
        public string zone = "oboz", kind = "authoring-snapshot", standardCommit = StandardCommit;
        public string unityVersion, buildTarget, androidAbi, androidBackend, colorSpace;
        public string[] androidGraphicsApis;
        public bool readyForContentUpdate = false, runtimeVerified = false;
        public Entry[] entries;
        public FileRecord[] files;
        public string[] packageDependencies, blockers;
    }

    [MenuItem("BRAXHORN/Obóz/Aktualizacje/Zapisz rejestr scen i zależności")]
    public static void Export()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            throw new InvalidOperationException("Eksport wymaga zakończenia Play Mode i kompilacji");
        BraxObozCharacters.ReadManifest();
        // Never silently save or export a stale disk version of a dirty scene.
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Najpierw zapisz zmienione sceny");
        AssetDatabase.SaveAssets();
        var entries = new List<Entry> { Register("oboz/scenes/wejscie", BraxObozActors.ScenePath) };
        foreach (string id in new[] { "monika", "vorn", "zyro", "rakk", "kade", "gorr", "mis" })
            entries.Add(Register("oboz/characters/" + id, BraxObozCharacters.Generated + "/" + id + "/CharacterCached.prefab"));

        var paths = new SortedSet<string>(StringComparer.Ordinal);
        var packages = new SortedSet<string>(StringComparer.Ordinal);
        foreach (string path in entries.SelectMany(e => e.dependencies).Append(BraxObozEnvironment.ScenePath))
            AddAsset(path, paths, packages);
        // The authoring scene can have dependencies no longer used by the review scene.
        foreach (string path in AssetDatabase.GetDependencies(BraxObozEnvironment.ScenePath, true))
            AddAsset(path, paths, packages);
        if (File.Exists(BraxObozBuild.StartScene))
            foreach (string path in AssetDatabase.GetDependencies(BraxObozBuild.StartScene, true))
                AddAsset(path, paths, packages);
        // Capture all runtime AND editor source: defaults/methods can affect generated scenes.
        foreach (string path in Directory.GetFiles("Assets", "*", SearchOption.AllDirectories)
            .Where(p => new[] { ".cs", ".shader", ".cginc", ".hlsl", ".asmdef", ".asmref", ".rsp" }.Contains(Path.GetExtension(p))))
            AddAsset(path.Replace('\\', '/'), paths, packages);
        foreach (string path in new[] { BraxObozCharacters.ManifestPath, "Assets/Editor/Oboz/VegetationLayout.json", BraxObozCampusLayout.Path })
            AddAsset(path, paths, packages);
        foreach (string path in Directory.GetFiles("ProjectSettings", "*", SearchOption.AllDirectories)
            .Where(p => p.EndsWith(".asset", StringComparison.Ordinal) || p.EndsWith(".txt", StringComparison.Ordinal)))
            paths.Add(path.Replace('\\', '/'));
        paths.Add("Packages/manifest.json"); paths.Add("Packages/packages-lock.json");
        var result = new Registry
        {
            unityVersion = Application.unityVersion, buildTarget = EditorUserBuildSettings.activeBuildTarget.ToString(),
            androidAbi = PlayerSettings.Android.targetArchitectures.ToString(),
            androidBackend = PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android).ToString(),
            colorSpace = PlayerSettings.colorSpace.ToString(),
            androidGraphicsApis = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android).Select(a => a.ToString()).ToArray(),
            entries = entries.OrderBy(e => e.address, StringComparer.Ordinal).ToArray(),
            files = paths.Select(Describe).ToArray(), packageDependencies = packages.ToArray(),
            blockers = new[] {
                "Authoring scene has not yet been divided into independently streamed site sectors",
                "Shared Addressables 2.7.6 sources are integrated but not yet Unity-compiled or accepted",
                "A content baseline candidate is not an approved signed BRAXHORN CITY APK; content-state must come from the actual installed base",
                "HTTPS/CDN hosting, phone import, offline, interruption, rollback and memory tests are not complete"
            }
        };
        Directory.CreateDirectory(Path.GetDirectoryName(Output));
        string json = JsonUtility.ToJson(result, true) + "\n";
        if (!File.Exists(Output) || File.ReadAllText(Output) != json) File.WriteAllText(Output, json);
        Debug.Log("BRAXHORN_OBOZ rejestr zapisany: " + Output + "; to kopia przygotowania, nie gotowa aktualizacja telefonu");
    }

    static Entry Register(string address, string path)
    {
        if (!File.Exists(path) || !File.Exists(path + ".meta"))
            throw new FileNotFoundException("Brak trwałej sceny/prefabu lub jego .meta", path);
        string guid = AssetDatabase.AssetPathToGUID(path, AssetPathToGUIDOptions.OnlyExistingAssets);
        if (string.IsNullOrEmpty(guid)) throw new InvalidOperationException("Brak GUID: " + path);
        return new Entry { address = address, path = path, guid = guid,
            dependencies = AssetDatabase.GetDependencies(path, true).OrderBy(p => p, StringComparer.Ordinal).ToArray() };
    }
    static void AddAsset(string path, SortedSet<string> paths, SortedSet<string> packages)
    {
        if (path.StartsWith("Packages/", StringComparison.Ordinal)) { packages.Add(path); return; }
        if (path == "Resources/unity_builtin_extra" || path == "Library/unity default resources") return;
        if (!path.StartsWith("Assets/", StringComparison.Ordinal))
            throw new InvalidOperationException("Nieznany rodzaj zależności: " + path);
        if (path.EndsWith(".unity", StringComparison.Ordinal) && !path.StartsWith(BraxObozCharacters.Generated + "/", StringComparison.Ordinal))
            throw new InvalidOperationException("Obóz nie może eksportować scen innej strefy: " + path);
        paths.Add(path); paths.Add(path + ".meta");
        string folder = Path.GetDirectoryName(path).Replace('\\', '/');
        while (folder != "Assets")
        {
            paths.Add(folder + ".meta");
            folder = Path.GetDirectoryName(folder).Replace('\\', '/');
        }
    }
    static FileRecord Describe(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("Brak zależności lub metadanych", path);
        using (var sha = SHA256.Create())
        using (var stream = File.OpenRead(path))
            return new FileRecord { path = path, bytes = stream.Length,
                sha256 = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant() };
    }
}
