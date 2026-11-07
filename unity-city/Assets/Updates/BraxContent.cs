using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

// Opt-in: legacy APKs have no BraxContentBase resource and use SceneManager.
// No GitHub credentials, network host or public assets are embedded in the player.
public sealed class BraxContent : MonoBehaviour
{
    public const string VirtualRoot = "https://brax-content.invalid/";
    static BraxContent instance;
    static BraxContentManifest basis, active;
    static IResourceLocator locator;
    static string root, current;
    static bool initialized, initializing, failed;
    static readonly Dictionary<string, AsyncOperationHandle<SceneInstance>> handles = new Dictionary<string, AsyncOperationHandle<SceneInstance>>();
    public static string Message { get; private set; }
    public static bool Importing { get; private set; }
    public static bool Enabled => basis != null;
    public static string FirstScene => active != null ? active.firstScene : (SceneManager.sceneCountInBuildSettings > 1 ? Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(1)) : "");

    static void Ensure()
    {
        if (instance) return;
        var go = new GameObject("BRAXHORN aktualizacje"); DontDestroyOnLoad(go);
        instance = go.AddComponent<BraxContent>();
        SceneManager.sceneLoaded += (scene, mode) => { if (Enabled && scene.name == FirstScene) FirstSceneReady(); };
        root = Path.Combine(Application.persistentDataPath, "BraxContent");
        var resource = Resources.Load<TextAsset>("BraxContentBase");
        if (!resource) return;
        basis = JsonUtility.FromJson<BraxContentManifest>(resource.text);
        BraxContentPackage.Validate(basis); active = basis;
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "inbox"));
    }

    public static IEnumerator Initialize()
    {
        Ensure();
        if (initialized || !Enabled) yield break;
        if (initializing) { while (initializing) yield return null; yield break; }
        initializing = true;
        try
        {
            current = ReadPointer("active");
            if (File.Exists(Path.Combine(root, "trial")))
            {
                current = ReadPointer("previous"); WritePointer("active", current);
                WritePointer("pending", "");
                File.Delete(Path.Combine(root, "trial"));
                Message = "Przywrócono poprzednią wersję po nieudanym starcie.";
            }
            string pending = ReadPointer("pending");
            if (pending.Length > 0)
            {
                WritePointer("previous", current);
                // Journal BEFORE switching the active version. A process killed
                // at any following step can roll back and won't retry the patch.
                WritePointer("trial", pending); WritePointer("active", pending);
                WritePointer("pending", ""); current = pending;
            }
            if (current.Length > 0)
            {
                try
                {
                    var selected = JsonUtility.FromJson<BraxContentManifest>(File.ReadAllText(Path.Combine(root, current, "manifest.json")));
                    BraxContentPackage.Compatible(basis, selected);
                    var check = Task.Run(() => CheckInstalled(selected, Path.Combine(root, current)));
                    // Await below, outside catch (C# iterators disallow yield in try/catch).
                    validation = check; candidate = selected;
                }
                catch (Exception e) { Reject(e.Message); }
            }
            if (validation != null)
            {
                while (!validation.IsCompleted) yield return null;
                if (validation.IsFaulted) Reject(validation.Exception.GetBaseException().Message);
                else active = candidate;
                validation = null; candidate = null;
            }
            Addressables.InternalIdTransformFunc = Transform;
            var init = Addressables.InitializeAsync(false);
            yield return init;
            if (init.Status != AsyncOperationStatus.Succeeded)
            {
                Message = "Nie udało się wczytać bazy zawartości."; failed = true;
                Addressables.Release(init); yield break;
            }
            locator = init.Result; Addressables.Release(init);
            if (active != basis)
            {
                var catalog = Addressables.LoadContentCatalogAsync(new Uri(Path.Combine(root, current, active.catalog)).AbsoluteUri, false);
                yield return catalog;
                if (catalog.Status == AsyncOperationStatus.Succeeded) locator = catalog.Result;
                else Reject("Nie udało się wczytać katalogu aktualizacji. Używam bazy APK.");
                Addressables.Release(catalog);
            }
            initialized = true;
        }
        finally { initializing = false; }
    }
    static Task validation;
    static BraxContentManifest candidate;

    static void CheckInstalled(BraxContentManifest manifest, string directory)
    {
        var baseline = basis.files.ToDictionary(f => f.name);
        foreach (var f in manifest.files)
        {
            string path = Path.Combine(directory, f.name);
            if (File.Exists(path))
            {
                if (new FileInfo(path).Length != f.bytes || BraxContentPackage.Sha(path) != f.sha256)
                    throw new InvalidDataException("Uszkodzony plik: " + f.name);
            }
            else if (f.name == manifest.catalog || !baseline.TryGetValue(f.name, out var b) || !BraxContentPackage.Same(f, b))
                throw new InvalidDataException("Brak pliku: " + f.name);
        }
    }

    static string ReadPointer(string name)
    {
        string path = Path.Combine(root, name);
        string value = File.Exists(path) ? File.ReadAllText(path).Trim() : "";
        return BraxContentPackage.Safe(value) && value.StartsWith("v-", StringComparison.Ordinal) ? value : "";
    }
    static void WritePointer(string name, string value)
    {
        string path = Path.Combine(root, name), temp = path + ".tmp";
        File.WriteAllText(temp, value ?? "");
        if (File.Exists(path)) File.Replace(temp, path, null);
        else File.Move(temp, path);
    }
    static void Reject(string reason)
    {
        Message = reason; Debug.LogWarning("BRAXHORN_CONTENT " + reason);
        active = basis; current = ""; WritePointer("active", "");
        if (File.Exists(Path.Combine(root, "trial"))) File.Delete(Path.Combine(root, "trial"));
    }

    static string Transform(IResourceLocation location)
    {
        string id = location.InternalId;
        if (!id.StartsWith(VirtualRoot, StringComparison.Ordinal)) return id;
        string name = Path.GetFileName(new Uri(id).AbsolutePath);
        if (!BraxContentPackage.Safe(name)) throw new InvalidDataException("Nieprawidłowa ścieżka zasobu.");
        // Initialization always reads the APK's baseline catalog. The selected
        // update catalog is loaded explicitly from a unique installed directory.
        if (name.EndsWith(".bundle", StringComparison.Ordinal) && current.Length > 0 && active.files.Any(f => f.name == name))
        {
            string installed = Path.Combine(root, current, name);
            if (File.Exists(installed)) return new Uri(installed).AbsoluteUri;
        }
        if (!basis.files.Any(f => f.name == name)) throw new FileNotFoundException("Zasób spoza bazy i aktualizacji", name);
        return Application.streamingAssetsPath + "/BraxContent/" + name;
    }

    public static bool Available(string name) => Enabled ? !failed && active.scenes.Contains(name) : Application.CanStreamedLevelBeLoaded(name);

    public sealed class SceneLoad
    {
        internal AsyncOperation legacy;
        internal AsyncOperationHandle<SceneInstance> handle;
        internal bool addressable;
        public bool Ready => addressable ? handle.IsDone : legacy == null || legacy.progress >= .9f;
        public bool Failed => addressable ? handle.IsDone && handle.Status != AsyncOperationStatus.Succeeded : legacy == null;
        public float Progress => addressable ? handle.PercentComplete * .9f : legacy != null ? legacy.progress : 0f;
        public void ReleaseFailure() { if (addressable && handle.IsValid() && Failed) Addressables.Release(handle); }
        public IEnumerator Activate()
        {
            if (Failed) yield break;
            if (addressable) { yield return handle; if (!Failed) yield return handle.Result.ActivateAsync(); }
            else { legacy.allowSceneActivation = true; yield return legacy; }
        }
    }
    public static SceneLoad Begin(string name, LoadSceneMode mode, bool activate = true)
    {
        if (!Enabled) { var old = SceneManager.LoadSceneAsync(name, mode); if (old != null) old.allowSceneActivation = activate; return new SceneLoad { legacy = old }; }
        if (!initialized || !Available(name) || !locator.Locate(name, typeof(SceneInstance), out var locations) || locations.Count != 1)
            throw new InvalidOperationException("Brak zgodnej sceny: " + name);
        if (handles.ContainsKey(name)) throw new InvalidOperationException("Scena już wczytana lub wczytywana: " + name);
        var h = Addressables.LoadSceneAsync(locations[0], mode, activate);
        handles.Add(name, h);
        h.Completed += result =>
        {
            if (result.Status != AsyncOperationStatus.Succeeded)
            {
                failed = true; Message = "Błąd wczytywania sceny. Uruchom grę ponownie, aby wycofać aktualizację.";
                handles.Remove(name); // SceneLoad still owns the failed handle until Wait.
            }
        };
        return new SceneLoad { handle = h, addressable = true };
    }
    public static IEnumerator Load(string name, LoadSceneMode mode)
    {
        var op = Begin(name, mode);
        if (op.addressable) yield return op.handle; else if (op.legacy != null) yield return op.legacy;
        if (op.Failed) { if (op.addressable && op.handle.IsValid()) Addressables.Release(op.handle); Debug.LogError("BRAXHORN_CONTENT scena niewczytana: " + name); }
    }
    public static IEnumerator Unload(Scene scene)
    {
        if (handles.TryGetValue(scene.name, out var handle))
        {
            handles.Remove(scene.name);
            if (handle.IsValid()) { var unload = Addressables.UnloadSceneAsync(handle, false); yield return unload; Addressables.Release(unload); }
        }
        else { var unload = SceneManager.UnloadSceneAsync(scene); if (unload != null) yield return unload; }
    }

    public static void FirstSceneReady() { if (instance && Enabled) instance.StartCoroutine(ConfirmAfterStart()); }
    static IEnumerator ConfirmAfterStart()
    {
        yield return new WaitForSecondsRealtime(30);
        while (handles.Values.Any(h => h.IsValid() && !h.IsDone)) yield return null;
        if (!failed && File.Exists(Path.Combine(root, "trial"))) File.Delete(Path.Combine(root, "trial"));
        if (!failed && !Importing)
        {
            var keep = new HashSet<string> { current, ReadPointer("previous"), ReadPointer("pending") };
            foreach (string directory in Directory.GetDirectories(root, "v-*"))
            {
                string name = Path.GetFileName(directory);
                if (name.Length == 34 && Guid.TryParseExact(name.Substring(2), "N", out _) && !keep.Contains(name))
                    try { Directory.Delete(directory, true); } catch (IOException) { /* retry after a later successful start */ }
            }
        }
    }

    public static void ChooseUpdate()
    {
        Ensure(); if (!Enabled || Importing) return;
#if UNITY_ANDROID && !UNITY_EDITOR
        using (var picker = new AndroidJavaClass("com.braxhorn.content.BraxContentPicker"))
            picker.CallStatic("open", instance.gameObject.name, Path.Combine(root, "inbox"));
#else
        Message = "Umieść plik update.zip w " + Path.Combine(root, "inbox") + ", a następnie wybierz aktualizację ponownie.";
        string path = Path.Combine(root, "inbox", "update.zip");
        if (File.Exists(path)) instance.OnUpdatePicked(path);
#endif
    }
    public void OnUpdatePicked(string value)
    {
        if (Importing || !Enabled) return;
        if (value.StartsWith("ERROR:", StringComparison.Ordinal)) { Message = value.Substring(6); return; }
        if (string.IsNullOrEmpty(value)) return;
        string full = Path.GetFullPath(value), inbox = Path.GetFullPath(Path.Combine(root, "inbox")) + Path.DirectorySeparatorChar;
        if (!full.StartsWith(inbox, StringComparison.Ordinal)) { Message = "Nieprawidłowy plik aktualizacji."; return; }
        StartCoroutine(Import(full));
    }
    IEnumerator Import(string path)
    {
        Importing = true; Message = "Sprawdzanie aktualizacji…";
        var work = Task.Run(() =>
        {
            var m = BraxContentPackage.ReadZipManifest(path);
            string id = "v-" + Guid.NewGuid().ToString("N"), staging = Path.Combine(root, "staging-" + Guid.NewGuid().ToString("N"));
            BraxContentPackage.Extract(path, staging, basis, m);
            Directory.Move(staging, Path.Combine(root, id)); return id;
        });
        while (!work.IsCompleted) yield return null;
        try
        {
            if (work.IsFaulted) Message = "Aktualizacja odrzucona: " + work.Exception.GetBaseException().Message;
            else { WritePointer("pending", work.Result); Message = "Aktualizacja gotowa. Zamknij i uruchom grę ponownie."; }
            File.Delete(path);
        }
        finally { Importing = false; }
    }
}
