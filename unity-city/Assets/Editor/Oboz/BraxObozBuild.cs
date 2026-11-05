using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// Isolated first-device REVIEW. Does not call city/estate builders or claim content-update support.
public static class BraxObozBuild
{
    const string Package = "com.braxhorn.oboz";
    public const string StartScene = "Assets/PlayableGenerated/Oboz/BraxhornObozStart.unity";
    const string StateFile = "Assets/PlayableGenerated/Oboz/build-state.json";
    const string Profile = "oboz/android/arm64/il2cpp/gles3/astc/linear/v1";
    static readonly string[] Scenes = { StartScene, BraxObozActors.ScenePath };
    [Serializable] sealed class State { public string profile, inputs; public BraxObozUpdateRegistry.FileRecord[] files; }
    [Serializable] sealed class Receipt
    {
        public string kind = "first-device-review", applicationId = Package, unity, sourceCommit, profile = Profile;
        public int versionCode; public bool generatedSceneReused, compilationSucceeded, apkBuilt;
        public bool deviceTested = false, contentUpdatesReady = false, ownerAppearanceApproved = false;
        public double generationSeconds, buildSeconds; public string[] scenes = Scenes, sourceIssues;
    }

    public static void BuildAndroidReview()
    {
        if (Application.unityVersion != "6000.0.58f2" || EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            throw new InvalidOperationException("Uruchom Unity 6000.0.58f2 z platformą Android");
        if (!File.Exists("braxhorn.keystore") || !File.Exists("braxhorn.keystore.pass"))
            throw new InvalidOperationException("Brak stałego podpisu APK obozu; podpis debug jest niedozwolony");
        if (!int.TryParse(File.ReadAllText("braxhorn.versioncode").Trim(), out int version) || version <= 0 || version > 2100000000)
            throw new InvalidOperationException("Nieprawidłowy versionCode obozu");
        string password = File.ReadAllText("braxhorn.keystore.pass").Trim();
        if (string.IsNullOrEmpty(password)) throw new InvalidOperationException("Brak hasła podpisu obozu");
        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../build/ObozAndroid"));
        Directory.CreateDirectory(output);
        string apk = Path.Combine(output, "braxhorn-oboz.apk");
        if (File.Exists(apk)) throw new IOException("W katalogu wyjściowym już jest APK; nie nadpisano wydania");
        var receipt = new Receipt { unity = Application.unityVersion, sourceCommit = Environment.GetEnvironmentVariable("GITHUB_SHA") ?? "local", versionCode = version };
        try
        {
            SaveUntitledBatchScenes();
            Configure(version, password);
            var started = DateTime.UtcNow;
            receipt.generatedSceneReused = TryReuse();
            if (!receipt.generatedSceneReused)
            {
                BraxObozEnvironment.Prepare();
                BraxObozActors.Assemble();
                CreateStart();
            }
            receipt.generationSeconds = (DateTime.UtcNow - started).TotalSeconds;
            var report = InspectReview();
            receipt.sourceIssues = report.sourceIssues;
            File.WriteAllText(Path.Combine(output, "scene-validation.json"), JsonUtility.ToJson(report, true));
            if (!report.structuralPassed) BraxBuildQuality.Issue(new InvalidOperationException("Błędy sceny obozu: " + string.Join("; ", report.structuralErrors)));
            foreach (string issue in report.sourceIssues) Debug.LogWarning("BRAXHORN_OBOZ_TEST Źródło do oceny na telefonie: " + issue);
            BraxObozUpdateRegistry.Export();
            SaveState();
            // Explicit scene list; build never falls back to a city or estate scene.
            started = DateTime.UtcNow;
            var built = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = Scenes, target = BuildTarget.Android, locationPathName = apk, options = BuildOptions.CompressWithLz4 });
            receipt.buildSeconds = (DateTime.UtcNow - started).TotalSeconds;
            if (built.summary.result != BuildResult.Succeeded || !File.Exists(apk))
                throw new InvalidOperationException("Build obozu nieudany: " + built.summary.result);
            receipt.compilationSucceeded = receipt.apkBuilt = true;
            Debug.Log("BRAXHORN_OBOZ_APK gotowe_do_testu=true sprawdzone_na_telefonie=false content_update=false");
        }
        finally
        {
            PlayerSettings.Android.keystorePass = ""; PlayerSettings.Android.keyaliasPass = "";
            AssetDatabase.SaveAssets();
            if (receipt.apkBuilt) BraxObozUpdateRegistry.Export();
            File.WriteAllText(Path.Combine(output, "build-receipt.json"), JsonUtility.ToJson(receipt, true));
        }
    }
    static void SaveUntitledBatchScenes()
    {
        // A fresh batch editor opens an untitled scene. Unity refuses additive
        // NewScene/OpenScene until it has a path. Preserve it outside the APK's
        // explicit scene list instead of discarding any scene objects.
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded || !string.IsNullOrEmpty(scene.path)) continue;
            if (!Application.isBatchMode)
                throw new InvalidOperationException("Zapisz otwartą scenę przed budowaniem obozu");
            Directory.CreateDirectory("Assets/PlayableGenerated/Oboz");
            AssetDatabase.Refresh();
            string path = AssetDatabase.GenerateUniqueAssetPath("Assets/PlayableGenerated/Oboz/BuildSession.unity");
            if (!EditorSceneManager.SaveScene(scene, path))
                throw new IOException("Nie zapisano początkowej sceny sesji CI");
            Debug.Log("BRAXHORN_OBOZ_SESJA zapisano scenę początkową przed generowaniem addytywnym");
        }
    }
    static void Configure(int version, string password)
    {
        if (GraphicsSettings.currentRenderPipeline) throw new InvalidOperationException("Obóz wymaga Built-in RP");
        PlayerSettings.colorSpace = ColorSpace.Linear;
        PlayerSettings.companyName = "Braxhorn"; PlayerSettings.productName = "Braxhorn Obóz";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, Package);
        PlayerSettings.Android.bundleVersionCode = version; PlayerSettings.bundleVersion = "0.1." + version;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.Android, Il2CppCodeGeneration.OptimizeSize);
        PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.Android, Il2CppCompilerConfiguration.Release);
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
        PlayerSettings.Android.useAPKExpansionFiles = false;
        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = Path.GetFullPath("braxhorn.keystore");
        PlayerSettings.Android.keystorePass = password; PlayerSettings.Android.keyaliasName = "braxhorn"; PlayerSettings.Android.keyaliasPass = password;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
        EditorUserBuildSettings.overrideMaxTextureSize = 0;
        EditorUserBuildSettings.overrideTextureCompression = OverrideTextureCompression.NoOverride;
        // OTOCZENIE: blisko maks. 2K, w oddali mniejsze mipmapy wg kamery.
        // 256MB to punkt startowy do weryfikacji na docelowym Androidzie,
        // nie deklaracja stalego zuzycia RAM/VRAM.
        int previousQuality = QualitySettings.GetQualityLevel();
        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.streamingMipmapsActive = true;
            QualitySettings.streamingMipmapsMemoryBudget = 256;
            QualitySettings.streamingMipmapsMaxLevelReduction = 3;
            QualitySettings.streamingMipmapsAddAllCameras = true;
        }
        QualitySettings.SetQualityLevel(previousQuality, false);
        AssetDatabase.SaveAssets();
    }
    static void CreateStart()
    {
        var previous = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try
        {
            var camera = new GameObject("Kamera wczytywania").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
            camera.cullingMask = 0; camera.allowMSAA = camera.allowHDR = false;
            new GameObject("Wczytywanie obozu").AddComponent<BraxStart>().regionTitle = "OBÓZ MISIA";
            if (!EditorSceneManager.SaveScene(scene, StartScene)) throw new IOException("Nie zapisano sceny startowej obozu");
        }
        finally { EditorSceneManager.CloseScene(scene, true); if (previous.IsValid()) SceneManager.SetActiveScene(previous); }
    }
    static BraxObozValidation.Report InspectReview()
    {
        var previous = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.OpenScene(BraxObozActors.ScenePath, OpenSceneMode.Additive);
        try
        {
            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) != 0)
                        throw new InvalidOperationException("Brak skryptu: " + t.name);
            if (roots.SelectMany(r => r.GetComponentsInChildren<BraxGymPlayer>(true)).Count() != 1 ||
                roots.SelectMany(r => r.GetComponentsInChildren<Camera>(true)).Count() != 1)
                BraxBuildQuality.Issue(new InvalidOperationException("Obóz wymaga jednego gracza i jednej kamery"));
            return BraxObozValidation.Check(roots.SelectMany(r => r.GetComponentsInChildren<BraxObozSceneInfo>(true)).Single());
        }
        finally { EditorSceneManager.CloseScene(scene, true); if (previous.IsValid()) SceneManager.SetActiveScene(previous); }
    }
    static string Hash(string path)
    { using (var sha = SHA256.Create()) using (var s = File.OpenRead(path)) return BitConverter.ToString(sha.ComputeHash(s)).Replace("-", "").ToLowerInvariant(); }
    static string Inputs()
    {
        var data = new StringBuilder(Profile + "|" + Application.unityVersion);
        var paths = Directory.GetFiles("Assets", "*", SearchOption.AllDirectories)
            .Select(p => p.Replace('\\', '/')).Where(p => !p.StartsWith("Assets/PlayableGenerated/", StringComparison.Ordinal) &&
                (p.StartsWith("Assets/Source/Prywatne/Oboz/", StringComparison.Ordinal) || p.StartsWith("Assets/Source/Public/Oboz/", StringComparison.Ordinal) || p.StartsWith("Assets/Editor/Oboz/", StringComparison.Ordinal) ||
                 new[] { ".cs", ".shader", ".cginc", ".hlsl", ".asmdef", ".asmref", ".rsp" }.Contains(Path.GetExtension(p))))
            .Concat(new[] { "Packages/manifest.json", "Packages/packages-lock.json" }).OrderBy(p => p, StringComparer.Ordinal);
        foreach (string p in paths) data.Append('|').Append(p).Append('=').Append(Hash(p));
        return Hash128.Compute(data.ToString()).ToString();
    }
    static bool TryReuse()
    {
        if (!File.Exists(StateFile)) { Debug.Log("BRAXHORN_OBOZ_SCENA cache=miss powod=pierwszy_eksport"); return false; }
        try
        {
            var saved = JsonUtility.FromJson<State>(File.ReadAllText(StateFile));
            if (saved.profile != Profile || saved.inputs != Inputs() || saved.files == null || saved.files.Length == 0 || Scenes.Any(p => !File.Exists(p)))
            { Debug.Log("BRAXHORN_OBOZ_SCENA cache=miss powod=zmienione_wejscia_lub_brak_sceny"); return false; }
            foreach (var file in saved.files)
                if (!file.path.StartsWith("Assets/", StringComparison.Ordinal) || file.path.Contains("..") || !File.Exists(file.path) || Hash(file.path) != file.sha256)
                { Debug.Log("BRAXHORN_OBOZ_SCENA cache=miss powod=zmieniona_zaleznosc"); return false; }
            Debug.Log("BRAXHORN_OBOZ_SCENA cache=hit"); return true;
        }
        catch (Exception e) { Debug.LogWarning("BRAXHORN_OBOZ_SCENA cache=miss " + e.GetType().Name); return false; }
    }
    static void SaveState()
    {
        var registry = JsonUtility.FromJson<BraxObozUpdateRegistry.Registry>(File.ReadAllText(BraxObozUpdateRegistry.Output));
        var files = registry.files.Where(f => f.path.StartsWith("Assets/", StringComparison.Ordinal)).ToList();
        foreach (var p in new[] { StartScene, StartScene + ".meta" })
            files.Add(new BraxObozUpdateRegistry.FileRecord { path = p, sha256 = Hash(p), bytes = new FileInfo(p).Length });
        File.WriteAllText(StateFile, JsonUtility.ToJson(new State { profile = Profile, inputs = Inputs(), files = files.ToArray() }, true));
        AssetDatabase.ImportAsset(StateFile, ImportAssetOptions.ForceSynchronousImport);
    }
}
