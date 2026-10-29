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

        [MenuItem("BRAXHORN/Osiedle - utwórz kontener sceny (pełne źródła wymagane)")]
        public static void PrepareScene()
        {
            ValidateInputs();
            if (!Directory.Exists("Assets/EstateMigration/Scenes"))
                Directory.CreateDirectory("Assets/EstateMigration/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var root = new GameObject("BRAXHORN_ESTATE_ORIGINAL_MIGRATION");
            root.AddComponent<EstateMigrationBootstrap>();
            EditorSceneManager.SaveScene(scene,ScenePath);
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
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
                throw new BuildFailedException("Switch to WebGL inside the isolated Unity project first; never change shared target silently.");
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
                data.collision == null || data.collision.Length!=1013 ||
                data.binaryAssets == null || data.missingOriginalModels == null)
                throw new BuildFailedException("Source manifest not matching original estate");
            if (data.missingOriginalModels.Length != 0)
                throw new BuildFailedException("Original sources missing; no 1:1 build: " + string.Join(", ",data.missingOriginalModels));
            foreach (var model in data.binaryAssets)
                if (!model.present || !File.Exists(Path.Combine(directory,model.name+".glb")))
                    throw new BuildFailedException("Required original asset missing: " + model.name + ".glb");
            Debug.Log("[Estate] original manifest preflight PASS (not a visual Unity test)");
        }
    }
}
