// Unity Editor integration for the ISOLATED osiedle project.
// Never touches main city scenes or the gym builder.
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Braxhorn.EstateMigration.Editor
{
    public static class EstateSceneBuilder
    {
        public const string ScenePath = "Assets/EstateMigration/Scenes/EstateMigration.unity";

        [MenuItem("BRAXHORN/Osiedle - zbuduj scene 1:1")]
        public static void PrepareScene()
        {
            if (!Directory.Exists("Assets/EstateMigration/Scenes"))
                Directory.CreateDirectory("Assets/EstateMigration/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var root = new GameObject("BRAXHORN_ESTATE_ORIGINAL_MIGRATION");
            root.AddComponent<EstateMigrationBootstrap>();
            EditorSceneManager.SaveScene(scene,ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath,true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Estate] Isolated scene created at "+ScenePath);
        }

        // Invocation: Unity -batchmode -quit -projectPath unity-city
        //   -executeMethod Braxhorn.EstateMigration.Editor.EstateSceneBuilder.BuildWebGL
        public static void BuildWebGL()
        {
            ValidateInputs();
            PrepareScene();
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL,BuildTarget.WebGL);
            PlayerSettings.productName = "BRAXHORN CITY - Osiedle Unity6 Preview";
            PlayerSettings.companyName = "BRAXHORN";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            var options = new BuildPlayerOptions
            {
                scenes = new[] {ScenePath},
                locationPathName = "build/EstateWebGL",
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("Estate WebGL: "+report.summary.result);
            Debug.Log("[Estate] WebGL created; VISUAL ACCEPTANCE STILL REQUIRED");
        }

        public static void ValidateInputs()
        {
            var directory = Path.Combine(Application.dataPath,"StreamingAssets/Estate");
            var mandatory = new [] {"EstateSourceManifest.json","EstateProceduralAndDoors.glb",
                "dom_suburban.glb","dom_nowoczesny.glb","willa_nowoczesna.glb",
                "dom_z_wnetrzem.glb","dom_wnetrze_zewn.glb",
                "garaze_sowieckie.glb","brax.glb.gz","brax_color.png","brax_normal.png"};
            foreach (var name in mandatory)
                if (!File.Exists(Path.Combine(directory,name)))
                    throw new BuildFailedException("Source locked estate missing: "+name);
            var json = File.ReadAllText(Path.Combine(directory,"EstateSourceManifest.json"));
            var data = JsonUtility.FromJson<EstateManifest>(json);
            if (data?.sourceModels?.Length != 21 || data.doorCount != 24 ||
                data.collision == null || data.collision.Length!=1013)
                throw new BuildFailedException("Source manifest not matching original estate");
            Debug.Log("[Estate] original manifest preflight PASS (not a visual Unity test)");
        }
    }
}
