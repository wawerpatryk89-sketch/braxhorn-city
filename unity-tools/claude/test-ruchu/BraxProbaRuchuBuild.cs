using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BraxProbaRuchuBuild
{
    public static void Zbuduj()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.transform.localScale = Vector3.one * 14f;
        var light = new GameObject("Światło").AddComponent<Light>();
        light.type = LightType.Directional;
        light.transform.rotation = Quaternion.Euler(38, -34, 0);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.62f,.65f,.70f);
        var player = new GameObject("Patryk Osiedla");
        player.transform.position = new Vector3(0,.04f,-10);
        var body = player.AddComponent<CharacterController>();
        body.radius = .32f;
        var hero = BraxClaudeOsiedle.NowyPatrykOsiedla(player.transform, body);
        var runtime = player.AddComponent<BraxGymPlayer>();
        runtime.visual = hero.transform;
        runtime.body = body;
        runtime.clips = hero.GetComponent<Animation>();
        runtime.bags = Array.Empty<Transform>();
        runtime.view = new GameObject("Kamera gracza").AddComponent<Camera>();
        runtime.view.tag = "MainCamera";
        runtime.view.enabled = false;
        BraxCityBuild.UstawSterowanieOsiedla(runtime);
        var camera = new GameObject("Kamera kontrolna").AddComponent<Camera>();
        camera.gameObject.AddComponent<AudioListener>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.18f,.21f,.25f);
        camera.fieldOfView = 32;
        var probe = new GameObject("Próba joysticka").AddComponent<BraxProbaRuchu>();
        probe.player = runtime;
        probe.kamera = camera;
        const string path = "Assets/ProbaRuchu.unity";
        if (!EditorSceneManager.SaveScene(scene,path)) throw new IOException(path);
        var dest = Path.GetFullPath(Path.Combine(Application.dataPath, "../../build/proba/Osiedle.x86_64"));
        Directory.CreateDirectory(Path.GetDirectoryName(dest));
        var result = BuildPipeline.BuildPlayer(new[] { path }, dest, BuildTarget.StandaloneLinux64, BuildOptions.None);
        if (result.summary.result != BuildResult.Succeeded) throw new Exception(result.summary.result.ToString());
    }
}
