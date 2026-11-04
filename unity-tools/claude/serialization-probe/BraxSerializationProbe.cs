using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Isolated Unity 6 regression: old source layout and repaired source in one small
// project, with no city assets or player build. This is not a phone gameplay test.
public static class BraxSerializationProbe
{
    public static void Run()
    {
        Directory.CreateDirectory("Assets/ProbeOutput");
        AssetDatabase.Refresh();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var brokenObject = new GameObject("Baseline build 58");
        var broken = brokenObject.AddComponent<BraxProbeBaseline.BraxMorzeZaKamera>();
        string rejected = BraxBrakujaceSkrypty.BladKomponentu(broken);
        if (rejected == null)
            throw new Exception("Baseline was not rejected; root-cause hypothesis is not reproduced.");
        Debug.Log("BRAXHORN_PROBE_BASELINE_REJECTED " + rejected);
        UnityEngine.Object.DestroyImmediate(brokenObject);

        var go = new GameObject("Fixed sea component");
        var sea = go.AddComponent<BraxMorzeZaKamera>();
        string error = BraxBrakujaceSkrypty.BladKomponentu(sea);
        if (error != null) throw new Exception(error);
        var shader = Shader.Find("Unlit/Color");
        if (!shader) throw new Exception("Missing probe shader");
        var material = new Material(shader);
        AssetDatabase.CreateAsset(material, "Assets/ProbeOutput/Water.mat");
        sea.woda = material;
        sea.komorka = 123.5f;
        sea.pol = 13;
        sea.bezWody = new Rect(7, 11, 31, 47);
        const string scenePath = "Assets/ProbeOutput/Sea.unity";
        if (!EditorSceneManager.SaveScene(go.scene, scenePath)) throw new Exception("Scene save failed");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        AssetDatabase.ImportAsset(scenePath, ImportAssetOptions.ForceUpdate);
        var reopened = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        var loaded = reopened.GetRootGameObjects()[0].GetComponent<BraxMorzeZaKamera>();
        if (!loaded || loaded.woda != material || loaded.komorka != 123.5f ||
            loaded.pol != 13 || loaded.bezWody != new Rect(7, 11, 31, 47))
            throw new Exception("Scene roundtrip lost sea settings/material reference");
        BraxBrakujaceSkrypty.Sprawdz(new[] {scenePath});
        Debug.Log("BRAXHORN_PROBE_FIXED_ROUNDTRIP_OK: woda, komorka, pol, bezWody");
        Directory.CreateDirectory("probe-report");
        File.WriteAllText("probe-report/serialization.txt", "Unity " + Application.unityVersion +
            "\nBaseline rejected: " + rejected + "\nFixed scene roundtrip: all four fields preserved\n" +
            "Scope: editor serialization regression, not Android runtime validation\n");
    }
}
