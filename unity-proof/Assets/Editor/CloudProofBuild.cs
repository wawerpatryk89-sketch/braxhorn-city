using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CloudProofBuild
{
    public static void Build()
    {
        string commit = ReadArgument("-proofCommit") ?? "local";
        if (commit != "local" && !System.Text.RegularExpressions.Regex.IsMatch(commit, "^[a-f0-9]{40}$"))
            throw new InvalidOperationException("Invalid proof commit identifier.");

        Directory.CreateDirectory("Assets/Generated");
        AssetDatabase.Refresh();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camera = new GameObject("Proof Camera").AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.transform.position = new Vector3(5f, 4f, -7f);
        camera.transform.LookAt(new Vector3(0f, 0.7f, 0f));
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.045f, 0.06f, 0.065f);

        var light = new GameObject("Proof Light").AddComponent<Light>();
        light.type = LightType.Directional;
        light.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
        light.intensity = 1.25f;
        RenderSettings.ambientLight = new Color(0.5f, 0.5f, 0.5f);

        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Proof Floor";
        floor.transform.position = new Vector3(0f, -0.15f, 0f);
        floor.transform.localScale = new Vector3(12f, 0.2f, 12f);
        floor.GetComponent<Renderer>().sharedMaterial = CreateMaterial("Floor", new Color(0.12f, 0.14f, 0.14f));

        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Interactive Proof Cube";
        cube.transform.position = new Vector3(0f, 1f, 0f);
        cube.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
        cube.GetComponent<Renderer>().sharedMaterial = CreateMaterial("Cube", new Color(0.55f, 1f, 0.23f));
        var proof = new GameObject("Cloud Proof Controller").AddComponent<CloudProof>();
        proof.target = cube.GetComponent<Renderer>();
        proof.sourceCommit = commit;

        const string scenePath = "Assets/Generated/CloudProof.unity";
        if (!EditorSceneManager.SaveScene(scene, scenePath))
            throw new InvalidOperationException("Failed to save the proof scene.");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };
        PlayerSettings.companyName = "Braxhorn";
        PlayerSettings.productName = "Braxhorn Unity Cloud Test";
        PlayerSettings.bundleVersion = "0.0.1";
        PlayerSettings.defaultScreenWidth = 960;
        PlayerSettings.defaultScreenHeight = 540;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.template = "APPLICATION:Default";
        AssetDatabase.SaveAssets();

        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../build/WebGL"));
        Directory.CreateDirectory(output);
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { scenePath },
            locationPathName = output,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException("WebGL proof build failed: " + report.summary.result);

        File.WriteAllText(Path.Combine(output, "proof.json"),
            "{\"commit\":\"" + commit + "\",\"unity\":\"" + Application.unityVersion +
            "\",\"purpose\":\"cloud-validation\",\"builtAtUtc\":\"" + DateTime.UtcNow.ToString("O") + "\"}\n");
        Debug.Log("BRAXHORN_UNITY_PROOF_BUILD_SUCCEEDED " + commit);
    }

    private static Material CreateMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null) throw new InvalidOperationException("Standard shader unavailable.");
        var material = new Material(shader) { name = name, color = color };
        material.SetFloat("_Glossiness", 0.15f);
        AssetDatabase.CreateAsset(material, "Assets/Generated/" + name + ".mat");
        return material;
    }

    private static string ReadArgument(string key)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == key) return args[i + 1];
        return null;
    }
}
