using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Isolated movement test, never a substitute for the camp and never an APK build.
public static class BraxObozReviewScene
{
    public const string Path = "Assets/PlayableGenerated/Oboz/MonikaMovementReview.unity";
    [MenuItem("BRAXHORN/Obóz/Scena kontroli ruchu Moniki (bez APK)")]
    public static void Create()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Zakończ Play Mode");
        var manifest = BraxObozCharacters.ReadManifest();
        var character = manifest.characters.Single(c => c.id == "monika");
        var previous = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try
        {
            var root = new GameObject("Kontrola Moniki — NIE pełny obóz");
            var world = root.AddComponent<BraxCityWorld>(); world.area = "oboz-review";
            var actor = new GameObject("Monika — wspólne sterowanie"); actor.layer = 8;
            actor.transform.SetParent(root.transform); actor.transform.position = Vector3.up * .04f;
            var hero = BraxObozCharacters.CreateCached(character, manifest.worldScale);
            hero.transform.SetParent(actor.transform, false);
            var controller = actor.AddComponent<CharacterController>();
            controller.height = character.realHeight * manifest.worldScale;
            controller.radius = .27f * manifest.worldScale; controller.center = Vector3.up * controller.height * .5f;
            controller.stepOffset = .25f * manifest.worldScale; controller.skinWidth = .035f;
            var cameraObject = new GameObject("Main Camera"); cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>(); camera.nearClipPlane = .1f; camera.farClipPlane = 150; camera.fieldOfView = 58;
            cameraObject.AddComponent<AudioListener>();
            var player = actor.AddComponent<BraxGymPlayer>();
            player.visual = hero.transform; player.clips = hero.GetComponent<Animation>();
            player.body = controller; player.view = camera; player.bags = Array.Empty<Transform>();
            player.cityWorld = world; player.worldSpawn = actor.transform.position; player.buildId = "OBOZ_RECOVERY_MOVEMENT_REVIEW_NOT_RELEASE";
            world.player = player;
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane); floor.name = "Neutralna podłoga techniczna — tylko kontrola ruchu";
            floor.transform.SetParent(root.transform); floor.transform.localScale = new Vector3(3, 1, 3);
            var material = new Material(Shader.Find("Standard")) { color = new Color(.26f, .29f, .27f) };
            material.SetFloat("_Glossiness", .15f);
            string materialPath = BraxObozCharacters.Generated + "/ReviewFloor.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (existing) { EditorUtility.CopySerialized(material, existing); Object.DestroyImmediate(material); material = existing; }
            else AssetDatabase.CreateAsset(material, materialPath);
            floor.GetComponent<Renderer>().sharedMaterial = material;
            var light = new GameObject("Światło kontroli").AddComponent<Light>(); light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(38, -28, 0); light.intensity = 1;
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
            if (!EditorSceneManager.SaveScene(scene, Path)) throw new IOException("Nie zapisano sceny kontroli Moniki");
            Debug.Log("BRAXHORN_OBOZ scena kontroli zapisana; brak potwierdzenia Play Mode, brak buildu");
        }
        catch { EditorSceneManager.CloseScene(scene, true); if (previous.IsValid()) SceneManager.SetActiveScene(previous); throw; }
        // Do not leave two players/cameras in additive scenes. Open the saved
        // review scene by itself for Play Mode; existing scenes stay untouched.
        EditorSceneManager.CloseScene(scene, true);
        if (previous.IsValid()) SceneManager.SetActiveScene(previous);
        AssetDatabase.SaveAssets();
    }
}
