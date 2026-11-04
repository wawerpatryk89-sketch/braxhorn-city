using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

// Copied ONLY to the synthetic validation project, never into a shipped game.
public sealed class BraxContentRuntimeProbe : MonoBehaviour
{
    float started;
    string mode;
    void Awake() { DontDestroyOnLoad(gameObject); started = Time.realtimeSinceStartup; }
    void Update() { if (Time.realtimeSinceStartup - started > 100) Finish(false, "timeout " + mode); }
    string Arg(string key)
    {
        var args = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(args, key); return i >= 0 && i + 1 < args.Length ? args[i + 1] : "";
    }
    IEnumerator Start()
    {
        mode = Arg("-probeMode");
        string root = Path.Combine(Application.persistentDataPath, "BraxContent");
        if (mode == "base" && Directory.Exists(root)) Directory.Delete(root, true);
        yield return BraxContent.Initialize();
        if (!BraxContent.Enabled) { Finish(false, "baseline absent"); yield break; }
        if (mode == "stage")
        {
            string inbox = Path.Combine(root, "inbox", "probe.zip"); File.Copy(Arg("-probeZip"), inbox, true);
            FindAnyObjectByType<BraxContent>().OnUpdatePicked(inbox);
            while (BraxContent.Importing) yield return null;
            Finish(File.Exists(Path.Combine(root, "pending")) && File.ReadAllText(Path.Combine(root, "pending")).StartsWith("v-"), BraxContent.Message);
            yield break;
        }
        if (mode == "interrupted") { Finish(File.Exists(Path.Combine(root, "trial")), "trial persisted before scene load"); yield break; }
        var load = BraxContent.Begin("Smoke_A", LoadSceneMode.Single, false);
        while (!load.Ready) yield return null;
        if (load.Failed) { Finish(false, "scene A failed"); yield break; }
        yield return load.Activate();
        var main = SceneManager.GetSceneByName("Smoke_A");
        var target = main.GetRootGameObjects().Single(g => g.name == "Target");
        Vector3 expected = mode == "update" ? new Vector3(1, 2, 3) : Vector3.zero;
        if (Vector3.Distance(target.transform.position, expected) > .001f) { Finish(false, "wrong scene version " + target.transform.position); yield break; }
        yield return BraxContent.Load("Smoke_B", LoadSceneMode.Additive);
        var second = SceneManager.GetSceneByName("Smoke_B");
        if (!second.isLoaded) { Finish(false, "scene B absent"); yield break; }
        yield return BraxContent.Unload(second);
        if (SceneManager.GetSceneByName("Smoke_B").isLoaded || !main.isLoaded) { Finish(false, "scene handle unload"); yield break; }
        if (mode == "update")
        {
            yield return new WaitForSecondsRealtime(32);
            if (File.Exists(Path.Combine(root, "trial"))) { Finish(false, "successful update not confirmed"); yield break; }
        }
        Finish(true, mode + " scene load / additive unload OK");
    }
    void Finish(bool pass, string message)
    {
        Debug.Log("BRAX_CONTENT_RUNTIME_" + (pass ? "PASS " : "FAIL ") + mode + " " + message);
        Application.Quit(pass ? 0 : 1);
    }
}
