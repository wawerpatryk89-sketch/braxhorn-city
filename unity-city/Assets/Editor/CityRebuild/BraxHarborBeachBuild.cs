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
        Krok("porządki: PUNKT DOSTAW, garaże za murem, mur podwórka z cegły PBR (właściciel 3.11)", () => BraxNabrzezePorzadki.Apply(root));
        Krok("A: port kontenerowy od nowa (nabrzeże, kontenerowiec, kontenery, biuro, brama)", () => BraxMiastoPort.Buduj(root, cityRoot));
        Krok("port: dźwig nad statkiem", () => Dzwig(root));
        Krok("punkty misji", () => Punkty(cityRoot));
        AssetDatabase.SaveAssets();
        Debug.Log("BRAXHORN_HARBOR nabrzeże od nowa: woda y " + BraxNabrzeze.Woda + ", k " + BraxNabrzeze.K.ToString("F3") +
                  ", łodzi " + BraxVesselCount(root) + ", usunięte przed budową: " + string.Join(" | ", BraxNabrzeze.Usuniete));
    }

    static int BraxVesselCount(Transform root) => root.GetComponentsInChildren<BraxVessel>(true).Length;

    // ---------------- dźwig portowy: stoi kołami na nabrzeżu przy placu kontenerowym ----------------
    // 9.11 (właściciel, zdjęcie z portu: „ten dźwig stoi w powietrzu… ma na ziemi leżeć… stać na ziemi normalnie”): wysokość
    // liczona była z Renderer.bounds — to pudło siatki w jej układzie obrócone do świata; wysięgnik (Crane_Mast) jest w modelu
    // pochylony, więc jego pudło sięgało ~2.5 m POD ziemię i cały dźwig podnosiło o tyle w górę (koła wisiały nad placem).
    // Teraz: najniższy prawdziwy wierzchołek PODWOZIA (Base_*: platforma i zestawy kół) stawiany na nawierzchni portu
    // zmierzonej promieniem w dół pod każdym narożnikiem podwozia (bez zgadywania poziomu), kolizja z dokładnego obrysu podwozia
    // i wieży. Log BRAXHORN_DZWIG: prześwit pod kołami przed i po (ma być 0 cm).
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
        // podwozie = węzły „Base…” (platforma, zestawy kół); gdyby model ich nie nazwał — cały dźwig (dokładne wierzchołki)
        bool Podwozie(Transform t) { for (var p = t; p && p != crane.transform; p = p.parent) if (p.name.StartsWith("Base", StringComparison.OrdinalIgnoreCase) || p.name.IndexOf("Wheel", StringComparison.OrdinalIgnoreCase) >= 0) return true; return false; }
        var podw = Dokladnie(crane, Podwozie);
        if (podw.size == Vector3.zero) podw = Dokladnie(crane, null);
        var calosc = Dokladnie(crane, null);
        float przedYmin = podw.min.y;
        // nawierzchnia pod podwoziem: promienie w dół w narożnikach i środku (dźwig nie ma jeszcze kolizji — nie trafi sam w siebie)
        Physics.SyncTransforms();
        float grunt = float.MinValue; int trafien = 0;
        foreach (var (px, pz) in new[] { (podw.min.x + .3f, podw.min.z + .3f), (podw.max.x - .3f, podw.min.z + .3f), (podw.min.x + .3f, podw.max.z - .3f), (podw.max.x - .3f, podw.max.z - .3f), (podw.center.x, podw.center.z) })
            if (Physics.Raycast(new Vector3(px, 6f, pz), Vector3.down, out var hit, 9f, ~0, QueryTriggerInteraction.Ignore) && hit.point.y > -1f && hit.point.y < 1.5f) { grunt = Mathf.Max(grunt, hit.point.y); trafien++; }
        if (trafien == 0) grunt = .15f;   // poziom placu portu (BraxMiastoPort.Y)
        crane.transform.position += Vector3.up * (grunt - przedYmin);
        podw = Dokladnie(crane, Podwozie); if (podw.size == Vector3.zero) podw = Dokladnie(crane, null);
        calosc = Dokladnie(crane, null);
        // kolizja: podwozie (pełna wysokość podwozia, min. 2.4 m) + wieża nad nim (pudło dokładnego obrysu bez wysięgnika)
        float hp = Mathf.Max(2.4f, podw.size.y);
        BraxNabrzeze.Kol(root, "Dźwig: podwozie", new Vector3(podw.center.x, grunt + hp * .5f, podw.center.z), new Vector3(podw.size.x, hp, podw.size.z));
        var wieza = Dokladnie(crane, t => { for (var p = t; p && p != crane.transform; p = p.parent) if (p.name.IndexOf("Tower", StringComparison.OrdinalIgnoreCase) >= 0 || p.name.IndexOf("Platform_Body", StringComparison.OrdinalIgnoreCase) >= 0) return true; return false; });
        if (wieza.size != Vector3.zero && wieza.max.y > grunt + hp)
        {
            float y0 = grunt + hp;
            BraxNabrzeze.Kol(root, "Dźwig: wieża", new Vector3(wieza.center.x, (y0 + wieza.max.y) * .5f, wieza.center.z), new Vector3(wieza.size.x, wieza.max.y - y0, wieza.size.z));
        }
        BraxNabrzeze.Log("BRAXHORN_DZWIG na nabrzeżu: nawierzchnia y " + grunt.ToString("F3") + " (" + (trafien > 0 ? "zmierzona, promieni " + trafien : "BRAK trafień — poziom placu 0.15") + ")" +
                         ", koła przed: y " + przedYmin.ToString("F3") + " (dawny prześwit " + ((przedYmin - grunt) * 100f).ToString("F0") + " cm)" +
                         " → po: y " + podw.min.y.ToString("F3") + " (prześwit " + ((podw.min.y - grunt) * 100f).ToString("F0") + " cm)" +
                         ", obrys dźwigu " + calosc.size.x.ToString("F1") + " × " + calosc.size.y.ToString("F1") + " × " + calosc.size.z.ToString("F1") + " m, pudło Renderer.bounds sięgało y " + BraxRegion.Bounds(crane).min.y.ToString("F2"));
    }

    /// <summary>Dokładny obrys (świat) z wierzchołków siatek — nie z Renderer.bounds (pudło siatki obrócone do świata
    /// jest większe od bryły, przy pochylonych częściach o metry). filtr: które węzły liczyć (null = wszystkie).</summary>
    static Bounds Dokladnie(GameObject go, Func<Transform, bool> filtr)
    {
        bool jest = false; var b = new Bounds();
        foreach (var f in go.GetComponentsInChildren<MeshFilter>(true))
        {
            var m = f.sharedMesh; if (!m || (filtr != null && !filtr(f.transform))) continue;
            Vector3[] vs; try { vs = m.vertices; } catch (Exception) { continue; }
            var M = f.transform.localToWorldMatrix;
            foreach (var v in vs)
            {
                var w = M.MultiplyPoint3x4(v);
                if (!jest) { b = new Bounds(w, Vector3.zero); jest = true; } else b.Encapsulate(w);
            }
        }
        return b;
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
