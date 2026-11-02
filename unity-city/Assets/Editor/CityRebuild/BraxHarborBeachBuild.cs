using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// BRAXHORN CITY — NABRZEŻE (plaża, marina, port): wejście z BraxCityRebuild.Apply. Przebudowa 2.11 (wykonawca „nabrzeże”):
// stary kod tego pliku (płaska plaża na dwóch siatkach, pomost na palach z łodziami nachodzącymi na palce, parasole
// stawiane „tylko na piasku”, którego nie znajdowały, budka ratownika przodem do mariny) — USUNIĘTY w całości.
// Teraz tylko: korzeń obszaru (nazwa = obszar doczytywany „Nabrzeże” w BraxGymBuild), modele CC0 z Source/HarborBeach
// i kolejne etapy, każdy osobno zabezpieczony (błąd etapu nie przerywa buildu miasta):
//   BraxNabrzezePlaza  — plaża miejska z dostępem (schody, rampa), bulwar plażowy, kąpielisko;
//   BraxNabrzezeMarina — molo, trap, pomosty pływające, łodzie przy pomostach, bosmanat;
//   dźwig portowy (etap przejściowy — port od nowa w BraxNabrzezePort).
// Punkty pod misje trafiają do BraxCityWorld.points (id „nabrzeze_*”, TravelPoint w QA).
public static class BraxHarborBeachBuild
{
    const string Source = "Assets/Source/HarborBeach/";
    const string Models = Source + "Models/";
    const string Generated = "Assets/PlayableGenerated/HarborBeach";
    public const string RootName = "BRAXHORN PORT + BEACH [LIBRARY 3D UNITY6]";

    /// <summary>Model CC0 z biblioteki nabrzeża (glTF importowany przez glTFast). null = brak pliku.</summary>
    public static GameObject Model(string file)
    {
        string p = Models + file;
        if (!File.Exists(p)) return null;
        AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceSynchronousImport);
        return AssetDatabase.LoadAssetAtPath<GameObject>(p) ?? AssetDatabase.LoadAllAssetsAtPath(p).OfType<GameObject>().FirstOrDefault();
    }

    /// <summary>Model postawiony podstawą na „floor”, przeskalowany do wysokości (m), obrócony o yaw; materiały miasta.</summary>
    public static GameObject Postaw(Transform parent, string file, string name, Vector3 floor, float height, float yaw)
    {
        var src = Model(file);
        if (!src) { BraxNabrzeze.Log("brak modelu " + file); return null; }
        var go = (GameObject)UnityEngine.Object.Instantiate(src);
        go.name = name; go.transform.SetParent(parent, false);
        go.transform.rotation = Quaternion.Euler(0, yaw, 0) * go.transform.rotation;
        var b = BraxRegion.Bounds(go);
        if (height > 0 && b.size.y > .01f) { go.transform.localScale *= height / b.size.y; b = BraxRegion.Bounds(go); }
        go.transform.position += floor - new Vector3(b.center.x, b.min.y, b.center.z);
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            r.sharedMaterials = r.sharedMaterials.Select(BraxRegion.CityMaterial).ToArray();
            r.shadowCastingMode = ShadowCastingMode.On; r.receiveShadows = true;
        }
        return go;
    }

    static void Krok(string nazwa, Action a)
    {
        try { a(); BraxNabrzeze.Log("etap " + nazwa + ": OK"); }
        catch (Exception e) { Debug.LogWarning("DO POPRAWKI: BRAXHORN_NABRZEZE etap " + nazwa + ": " + e); }
    }

    public static void Apply(Transform cityRoot)
    {
        if (!cityRoot) throw new ArgumentNullException(nameof(cityRoot));
        var previous = cityRoot.Find(RootName);
        if (previous) UnityEngine.Object.DestroyImmediate(previous.gameObject);
        foreach (var dir in new[] { Generated, BraxNabrzeze.Out }) BraxZapisAssetu.UsunFolder(dir, "nabrzeże");
        BraxNabrzeze.Usuniete.Clear();
        BraxNabrzeze.Przygotuj();
        var root = new GameObject(RootName).transform;
        root.SetParent(cityRoot, false); root.position = Vector3.zero;

        Krok("plaża miejska", () => BraxNabrzezePlaza.Buduj(root));
        Krok("marina", () => BraxNabrzezeMarina.Buduj(root, cityRoot));
        Krok("A: port kontenerowy od nowa (nabrzeże, kontenerowiec, kontenery, biuro, brama)", () => BraxMiastoPort.Buduj(root, cityRoot));
        Krok("port: dźwig nad statkiem", () => Dzwig(root));
        Krok("punkty misji", () => Punkty(cityRoot));
        AssetDatabase.SaveAssets();
        Debug.Log("BRAXHORN_HARBOR nabrzeże od nowa: woda y " + BraxNabrzeze.Woda + ", k " + BraxNabrzeze.K.ToString("F3") +
                  ", łodzi " + BraxVesselCount(root) + ", usunięte przed budową: " + string.Join(" | ", BraxNabrzeze.Usuniete));
    }

    static int BraxVesselCount(Transform root) => root.GetComponentsInChildren<BraxVessel>(true).Length;

    // ---------------- dźwig portowy (bez zmian względem 31.10: stoi na nabrzeżu przy placu kontenerowym) ----------------
    static void Dzwig(Transform root)
    {
        const float CraneZ = -25f;
        var crane = Postaw(root, "harbor_crane.glb", "Port: dźwig portowy", new Vector3(74f, .17f, CraneZ), 0f, -90f);
        if (!crane) return;
        var cb = BraxRegion.Bounds(crane);
        Vector3 mass = Vector3.zero; float wsum = 0;
        foreach (var f in crane.GetComponentsInChildren<MeshFilter>(true))
        {
            if (!f.sharedMesh) continue; var rr = f.GetComponent<Renderer>(); if (!rr) continue;
            float wv = f.sharedMesh.vertexCount; mass += rr.bounds.center * wv; wsum += wv;
        }
        if (wsum > 0) mass /= wsum;
        if (mass.x > cb.center.x) { crane.transform.RotateAround(cb.center, Vector3.up, 180f); cb = BraxRegion.Bounds(crane); mass = 2 * cb.center - mass; mass.y = cb.center.y; }
        crane.transform.position += new Vector3(76f - mass.x, 0, 0);
        var cp = crane.transform.position; cp.y += .17f - BraxRegion.Bounds(crane).min.y; crane.transform.position = cp;
        BraxNabrzeze.Kol(root, "Dźwig: podstawa", new Vector3(76f, 1.2f, CraneZ), new Vector3(8f, 2.4f, 8f));
    }

    // ---------------- punkty pod misje (zleceniodawcy, cele) → BraxCityWorld.points ----------------
    static void Punkty(Transform cityRoot)
    {
        var world = UnityEngine.Object.FindObjectsByType<BraxCityWorld>(FindObjectsSortMode.None).FirstOrDefault(w => w.area == "city");
        if (!world) { BraxNabrzeze.Log("brak BraxCityWorld — punkty misji pominięte"); return; }
        var lista = world.points.Where(p => p != null && !p.id.StartsWith("nabrzeze_")).ToList();
        int n = 0;
        foreach (var src in new[] { BraxNabrzezePlaza.Miejsca, BraxNabrzezeMarina.Miejsca })
            foreach (var kv in src) { lista.Add(new BraxCityPoint { id = "nabrzeze_" + kv.Key, label = kv.Key.Replace('_', ' '), position = kv.Value }); n++; }
        world.points = lista.ToArray();
        EditorUtility.SetDirty(world);
        BraxNabrzeze.Log("punkty misji: " + n + " (" + string.Join(", ", BraxNabrzezePlaza.Miejsca.Keys.Concat(BraxNabrzezeMarina.Miejsca.Keys)) + ")");
    }
}
