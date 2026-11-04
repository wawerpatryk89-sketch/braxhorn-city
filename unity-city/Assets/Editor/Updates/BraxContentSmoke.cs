using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Synthetic assets only. Compiles all project scripts, builds Android bundles
// twice, and exercises the same ZIP validation used by the actual player.
public static class BraxContentSmoke
{
    static int checks;
    static void Check(bool pass, string name)
    {
        if (!pass) throw new Exception("CONTENT TEST FAILED: " + name);
        checks++; Debug.Log("CONTENT TEST PASS: " + name);
    }
    static void Reject(Action action, string name)
    {
        bool rejected = false;
        try { action(); } catch (InvalidDataException) { rejected = true; }
        Check(rejected, name);
    }
    static void CopyTree(string source, string destination)
    {
        foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            string target = Path.Combine(destination, file.Substring(source.Length + 1));
            Directory.CreateDirectory(Path.GetDirectoryName(target)); File.Copy(file, target, true);
        }
    }
    public static void Run()
    {
        Directory.CreateDirectory("Assets/Smoke");
        var scenes = new[] { "Assets/Smoke/Smoke_A.unity", "Assets/Smoke/Smoke_B.unity" };
        for (int i = 0; i < scenes.Length; i++)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var mesh = new Mesh(); var random = new System.Random(17 + i);
            var vertices = new Vector3[30000]; var indices = new int[30000];
            for (int v = 0; v < vertices.Length; v++) { vertices[v] = new Vector3((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble()); indices[v] = v; }
            mesh.vertices = vertices; mesh.triangles = indices; mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, "Assets/Smoke/mesh-" + i + ".asset");
            var cube = new GameObject("Target"); cube.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = cube.AddComponent<MeshRenderer>();
            var material = new Material(Shader.Find("Unlit/Color"));
            AssetDatabase.CreateAsset(material, "Assets/Smoke/material-" + i + ".mat"); renderer.sharedMaterial = material;
            Check(EditorSceneManager.SaveScene(scene, scenes[i]), "save scene " + i);
        }
        AssetDatabase.SaveAssets();
        BraxContentBuild.Baseline("smoke", scenes);
        var basis = JsonUtility.FromJson<BraxContentManifest>(File.ReadAllText(BraxContentBuild.Base + "/manifest.json"));
        CopyTree("Library/com.unity.addressables/aa", BraxContentBuild.Work + "/baseline-aa");
        string stateHash = BraxContentPackage.Sha(BraxContentBuild.Base + "/addressables_content_state.bin");
        File.WriteAllText("content-patch.json", "{\"edits\":[{\"scene\":\"Assets/Smoke/Smoke_A.unity\",\"path\":\"Target\",\"operation\":\"transform\",\"position\":{\"x\":1,\"y\":2,\"z\":3},\"rotation\":{\"x\":0,\"y\":0,\"z\":0},\"scale\":{\"x\":1,\"y\":1,\"z\":1}}]}");
        BraxContentBuild.UpdateFromBaseline();
        var zip = Directory.GetFiles(BraxContentBuild.Work, "*.braxupdate.zip").Single();
        var update = BraxContentPackage.ReadZipManifest(zip);
        Check(stateHash == BraxContentPackage.Sha(BraxContentBuild.Base + "/addressables_content_state.bin"), "immutable baseline content state");
        Check(new FileInfo(zip).Length < basis.files.Sum(f => f.bytes), "delta smaller than baseline");
        using (var archive = ZipFile.OpenRead(zip))
        {
            var unchanged = basis.files.Where(b => b.name.EndsWith(".bundle") && update.files.Any(f => BraxContentPackage.Same(b, f))).ToArray();
            Check(unchanged.Length > 0, "unchanged bundles retained");
            Check(unchanged.All(b => archive.GetEntry(b.name) == null), "unchanged bundles not transferred");
        }
        string install = BraxContentBuild.Work + "/installed";
        BraxContentPackage.Extract(zip, install, basis, update);
        Check(File.Exists(install + "/" + update.catalog), "valid package install");
        var wrong = JsonUtility.FromJson<BraxContentManifest>(JsonUtility.ToJson(update)); wrong.zone = "other";
        Reject(() => BraxContentPackage.Compatible(basis, wrong), "wrong zone rejected");
        wrong.zone = basis.zone; wrong.runtimeId = new string('a', 64);
        Reject(() => BraxContentPackage.Compatible(basis, wrong), "wrong runtime rejected");
        string bad = BraxContentBuild.Work + "/bad.zip";
        File.Copy(zip, bad);
        using (var archive = ZipFile.Open(bad, ZipArchiveMode.Update)) archive.CreateEntry("../escape.bundle");
        Reject(() => BraxContentPackage.Extract(bad, install + "-bad", basis, update), "ZIP traversal rejected");
        Check(!Directory.Exists(install + "-bad"), "failed staging removed");
        File.Delete(bad); File.Copy(zip, bad);
        using (var archive = ZipFile.Open(bad, ZipArchiveMode.Update))
        {
            var entry = archive.Entries.First(e => e.FullName == update.catalog); string name = entry.FullName; long bytes = entry.Length; entry.Delete();
            using (var output = archive.CreateEntry(name).Open()) for (long i = 0; i < bytes; i++) output.WriteByte(32);
        }
        Reject(() => BraxContentPackage.Extract(bad, install + "-bad", basis, update), "corrupt catalog rejected");
        Directory.CreateDirectory("../build/content-smoke");
        File.WriteAllText("../build/content-smoke/result.json", "{\"checks\":" + checks + ",\"baselineBytes\":" + basis.files.Sum(f => f.bytes) + ",\"updateBytes\":" + new FileInfo(zip).Length + ",\"unity\":\"" + Application.unityVersion + "\",\"deviceTest\":false}");
        Debug.Log("BRAX_CONTENT_SMOKE_PASS checks=" + checks);
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneLinux64)
        {
            Directory.Delete("Library/com.unity.addressables/aa", true);
            CopyTree(BraxContentBuild.Work + "/baseline-aa", "Library/com.unity.addressables/aa");
            var bootstrap = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var probeType = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("BraxContentRuntimeProbe")).First(t => t != null);
            new GameObject("Runtime probe").AddComponent(probeType);
            EditorSceneManager.SaveScene(bootstrap, "Assets/Smoke/Bootstrap.unity");
            PlayerSettings.companyName = "BraxhornValidation"; PlayerSettings.productName = "ContentProbe";
            var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { "Assets/Smoke/Bootstrap.unity" },
                target = BuildTarget.StandaloneLinux64, locationPathName = "../build/content-probe/Probe.x86_64", options = BuildOptions.Development });
            Check(result.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded, "Linux runtime probe player");
            File.Copy(zip, "../build/content-probe/update.zip", true);
        }
    }
}
