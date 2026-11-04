using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Isolated Unity 6 regression: old source layout and repaired source in one small
// project, with no city assets or player build. Includes real Android scene bundles.
// This is not a phone gameplay test.
public static class BraxSerializationProbe
{
    public static void Run()
    {
        Directory.CreateDirectory("Assets/ProbeOutput");
        Directory.CreateDirectory("probe-report");
        AssetDatabase.Refresh();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var brokenObject = new GameObject("Baseline build 58");
        var broken = brokenObject.AddComponent<BraxProbeBaseline.BraxMorzeZaKamera>();
        string rejected = BraxBrakujaceSkrypty.BladKomponentu(broken);
        Debug.Log("BRAXHORN_PROBE_BASELINE_BINDING " + (rejected ?? "accepted by editor"));
        broken.komorka = 123.5f;
        broken.pol = 13;
        broken.bezWody = new Rect(7, 11, 31, 47);
        const string baselinePath = "Assets/ProbeOutput/Baseline.unity";
        if (!EditorSceneManager.SaveScene(brokenObject.scene, baselinePath)) throw new Exception("Baseline save failed");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

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
        File.WriteAllText("probe-report/serialization.txt", "Unity " + Application.unityVersion +
            "\nBaseline editor binding: " + (rejected ?? "accepted") + "\nFixed scene roundtrip: all four fields preserved\n" +
            "Scope: editor serialization regression, not Android runtime validation\n");
        var manifest = BuildPipeline.BuildAssetBundles("probe-report", new[] {
            new AssetBundleBuild { assetBundleName = "baseline", assetNames = new[] {baselinePath} },
            new AssetBundleBuild { assetBundleName = "fixed", assetNames = new[] {scenePath} }
        }, BuildAssetBundleOptions.ForceRebuildAssetBundle | BuildAssetBundleOptions.DisableWriteTypeTree, BuildTarget.Android);
        if (!manifest) throw new Exception("Android scene bundle build failed");
        Debug.Log("BRAXHORN_PROBE_ANDROID_BUNDLES_READY");
    }
}
