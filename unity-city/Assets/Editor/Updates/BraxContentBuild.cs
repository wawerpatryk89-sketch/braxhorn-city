using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BraxContentBuild
{
    public const string Work = "BraxContentBuild", Output = Work + "/output", Base = Work + "/base";
    public const string Config = "Assets/Resources/BraxContentBase.json";
    public static bool Requested => File.Exists("content-mode.txt") && File.ReadAllText("content-mode.txt").Trim() == "baseline";

    public static void Baseline(string zone, string[] scenes)
    {
        if (Directory.Exists(Base)) throw new InvalidOperationException("Baza już istnieje. Nie nadpisuj content state wydanego APK.");
        if (!BraxContentPackage.Safe(zone) || scenes.Length == 0 || scenes.Any(p => !File.Exists(p))) throw new ArgumentException("Nieprawidłowa strefa lub sceny.");
        string id = zone + "-" + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(Output);
        var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
        settings.BuildRemoteCatalog = true;
        settings.DisableCatalogUpdateOnStartup = true;
        settings.OverridePlayerVersion = id;
        settings.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer;
        string profile = settings.activeProfileId;
        settings.profileSettings.SetValue(profile, AddressableAssetSettings.kRemoteBuildPath, Output);
        settings.profileSettings.SetValue(profile, AddressableAssetSettings.kRemoteLoadPath, BraxContent.VirtualRoot + id);
        settings.RemoteCatalogBuildPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteBuildPath);
        settings.RemoteCatalogLoadPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteLoadPath);
        // One stable group per existing scene; no regeneration or renumbering.
        foreach (var scene in scenes)
        {
            var group = Group(settings, zone + "-" + Path.GetFileNameWithoutExtension(scene));
            settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(scene), group).address = Path.GetFileNameWithoutExtension(scene);
        }
        // Explicit separate dependencies prevent a shared model/texture being
        // duplicated into every scene bundle. Sub-assets stay with their source.
        var shared = Group(settings, zone + "-shared");
        settings.DefaultGroup = shared;
        foreach (string path in AssetDatabase.GetDependencies(scenes, true).OrderBy(p => p, StringComparer.Ordinal))
        {
            if (!path.StartsWith("Assets/", StringComparison.Ordinal) || scenes.Contains(path) ||
                path.Contains("/Resources/") || path.Contains("/Editor/") ||
                path.EndsWith(".cs") || path.EndsWith(".dll") || AssetDatabase.IsValidFolder(path)) continue;
            string guid = AssetDatabase.AssetPathToGUID(path);
            if (!string.IsNullOrEmpty(guid)) settings.CreateOrMoveEntry(guid, shared).address = "asset/" + guid;
        }
        AssetDatabase.SaveAssets();
        AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
        Check(result);
        var m = Inventory(zone, id, "base", scenes, RuntimeId());
        Directory.CreateDirectory(Base);
        File.WriteAllText(Base + "/manifest.json", JsonUtility.ToJson(m, true));
        string state = ContentUpdateScript.GetContentStateDataPath(false, settings);
        if (!File.Exists(state)) throw new FileNotFoundException("Brak content state", state);
        File.Copy(state, Base + "/addressables_content_state.bin", false);
        var guids = scenes.Select(p => p + "=" + AssetDatabase.AssetPathToGUID(p)).ToArray();
        File.WriteAllLines(Base + "/scene-guids.txt", guids);
        string bundled = "Assets/StreamingAssets/BraxContent";
        Directory.CreateDirectory(bundled); Directory.CreateDirectory(Path.GetDirectoryName(Config));
        foreach (var f in m.files) File.Copy(Output + "/" + f.name, bundled + "/" + f.name, true);
        File.WriteAllText(Config, JsonUtility.ToJson(m, true));
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.Log("BRAX_CONTENT_BASE " + id + " files=" + m.files.Length + " bytes=" + m.files.Sum(f => f.bytes));
    }

    static AddressableAssetGroup Group(AddressableAssetSettings settings, string name)
    {
        var group = settings.FindGroup(name) ?? settings.CreateGroup(name, false, false, false, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
        var schema = group.GetSchema<BundledAssetGroupSchema>();
        schema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteBuildPath);
        schema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteLoadPath);
        schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
        schema.BundleNaming = BundledAssetGroupSchema.BundleNamingStyle.AppendHash;
        schema.Compression = BundledAssetGroupSchema.BundleCompressionMode.LZ4;
        group.GetSchema<ContentUpdateGroupSchema>().StaticContent = false;
        EditorUtility.SetDirty(group); EditorUtility.SetDirty(schema);
        return group;
    }

    public static void UpdateFromBaseline()
    {
        var basis = JsonUtility.FromJson<BraxContentManifest>(File.ReadAllText(Base + "/manifest.json"));
        BraxContentPackage.Validate(basis);
        if (RuntimeId() != basis.runtimeId) throw new InvalidOperationException("Zmienił się kod, pakiety lub ustawienia silnika. Potrzebny nowy bazowy APK.");
        var scenePaths = File.ReadAllLines(Base + "/scene-guids.txt").Select(line => line.Split('=')).ToArray();
        foreach (var pair in scenePaths)
            if (pair.Length != 2 || AssetDatabase.AssetPathToGUID(pair[0]) != pair[1]) throw new InvalidOperationException("Zmieniono GUID sceny bazy.");
        // A patch edits saved scenes/assets. It never invokes the world generator.
        BraxContentEdits.ApplyFile("content-patch.json");
        AssetDatabase.SaveAssets();
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null) throw new InvalidOperationException("Przywróć ustawienia Addressables ze snapshotu bazy.");
        if (Directory.Exists(Output)) Directory.Delete(Output, true);
        Directory.CreateDirectory(Output);
        var result = ContentUpdateScript.BuildContentUpdate(settings, Base + "/addressables_content_state.bin");
        Check(result);
        var update = Inventory(basis.zone, basis.baseId, "u-" + Guid.NewGuid().ToString("N"), scenePaths.Select(p => p[0]).ToArray(), basis.runtimeId, basis);
        string zip = Work + "/" + basis.zone + "-" + update.version + ".braxupdate.zip";
        Package(basis, update, zip);
        Debug.Log("BRAX_CONTENT_UPDATE_READY " + zip + " bytes=" + new FileInfo(zip).Length + " APK_NOT_BUILT");
    }

    static void Check(AddressablesPlayerBuildResult result)
    {
        if (result == null || !string.IsNullOrEmpty(result.Error)) throw new InvalidOperationException("Addressables: " + (result == null ? "brak wyniku" : result.Error));
    }

    static BraxContentManifest Inventory(string zone, string id, string version, string[] scenes, string runtime, BraxContentManifest basis = null)
    {
        string[] catalogs = Directory.GetFiles(Output, "catalog*", SearchOption.TopDirectoryOnly).Where(p => p.EndsWith(".json") || p.EndsWith(".bin")).ToArray();
        if (catalogs.Length != 1) throw new InvalidOperationException("Oczekiwano dokładnie jednego katalogu Addressables.");
        string catalog = Path.GetFileName(catalogs[0]);
        // Unchanged bundles may be omitted by BuildContentUpdate. Retain baseline
        // inventory; including unreferenced baseline entries costs no transfer.
        var files = basis == null ? new Dictionary<string, BraxContentFile>() : basis.files.Where(f => f.name.EndsWith(".bundle")).ToDictionary(f => f.name);
        foreach (string path in Directory.GetFiles(Output).Where(p => p.EndsWith(".bundle") || p == catalogs[0] || p == Path.ChangeExtension(catalogs[0], ".hash")))
            files[Path.GetFileName(path)] = new BraxContentFile { name = Path.GetFileName(path), bytes = new FileInfo(path).Length, sha256 = BraxContentPackage.Sha(path) };
        var m = new BraxContentManifest { zone = zone, baseId = id, runtimeId = runtime, version = version, catalog = catalog,
            firstScene = Path.GetFileNameWithoutExtension(scenes[0]), scenes = scenes.Select(Path.GetFileNameWithoutExtension).ToArray(), files = files.Values.OrderBy(f => f.name, StringComparer.Ordinal).ToArray() };
        BraxContentPackage.Validate(m); return m;
    }

    public static void Package(BraxContentManifest basis, BraxContentManifest update, string zip)
    {
        BraxContentPackage.Compatible(basis, update);
        var original = basis.files.ToDictionary(f => f.name);
        using (var archive = ZipFile.Open(zip, ZipArchiveMode.Create))
        {
            var manifest = archive.CreateEntry("manifest.json");
            using (var writer = new StreamWriter(manifest.Open())) writer.Write(JsonUtility.ToJson(update, true));
            foreach (var f in update.files)
                if (f.name == update.catalog || !original.TryGetValue(f.name, out var b) || !BraxContentPackage.Same(b, f))
                    archive.CreateEntryFromFile(Output + "/" + f.name, f.name, System.IO.Compression.CompressionLevel.NoCompression);
        }
        // Use the exact player validator before delivering the package.
        string check = Work + "/verify-" + Guid.NewGuid().ToString("N");
        BraxContentPackage.Extract(zip, check, basis, update); Directory.Delete(check, true);
    }

    public static string RuntimeId()
    {
        var paths = Directory.GetFiles("Assets", "*", SearchOption.AllDirectories)
            .Where(p => !p.Replace('\\', '/').Contains("/Editor/"))
            .Where(p => new[] { ".cs", ".asmdef", ".asmref", ".dll", ".so", ".aar", ".java", ".shader", ".compute" }.Contains(Path.GetExtension(p)) ||
                (p.Replace('\\', '/').Contains("/Resources/") && p != Config && p != Config + ".meta" && !p.EndsWith(".meta")))
            .Concat(new[] { "Packages/manifest.json", "Packages/packages-lock.json" }.Where(File.Exists))
            .OrderBy(p => p, StringComparer.Ordinal);
        var text = new StringBuilder(Application.unityVersion + "\n" + EditorUserBuildSettings.activeBuildTarget + "\n" +
            PlayerSettings.colorSpace + "\n" + PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android) + "\n" +
            PlayerSettings.Android.targetArchitectures + "\n" + PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android));
        foreach (string p in paths)
        {
            text.Append('\n').Append(p.Replace('\\', '/')).Append(':').Append(BraxContentPackage.Sha(p));
            if (File.Exists(p + ".meta")) text.Append(':').Append(BraxContentPackage.Sha(p + ".meta"));
        }
        using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text.ToString()))).Replace("-", "").ToLowerInvariant();
    }
}
