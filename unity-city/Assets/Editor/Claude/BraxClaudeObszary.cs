using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// PODZIAŁ REJONÓW NA 2 OBSZARY (właściciel 1.11, 01:04–01:06: „miasto podzielić na dwa obszary tak samo osiedle…
// jak dojdę do granicy drugiej to dopiero się wczytać… tylko bez blokady… obszar nie może być większy niż 500”).
//   MIASTO:  scena BraxhornCity (siłownia, ulica, NOVA + mieszkanie Braxa, Nutrition, wieżowce, tyły, boiska, tunel)
//            + BraxhornCity_Nabrzeze (port, plaża, las za plażą, marina, metro, wyspa) — doczytywane przy podejściu.
//   OSIEDLE: scena BraxhornEstate (wjazd z tunelu, Poznańska z domem Wawerów, hala walk, kasyno, siłownia A — z > −102)
//            + BraxhornEstate_Poludnie (park, osiedle B, wille, sklepy, kort — wszystko w całości poniżej z = −102).
// Przenoszone są całe poddrzewa (odwołania wewnątrz zostają w jednej scenie). W scenie głównej zostają zawsze: gracz,
// kamery, świat (BraxCityWorld), łodzie (BraxVessel — gracz na łodzi nie może zniknąć z obszarem), windy, lada Nutrition,
// woda i słońce. Drzwi działają przez rejestr BraxCityDoor.All. Doczytywanie: BraxObszary (runtime).
public static class BraxClaudeObszary
{
    static readonly Type[] Zostaja = { typeof(BraxGymPlayer), typeof(BraxCityWorld), typeof(Camera), typeof(BraxObszary), typeof(BraxCityElevator), typeof(BraxVessel), typeof(BraxNutritionCounter) };

    static bool MaZostajace(Transform t) => Zostaja.Any(z => t.GetComponentInChildren(z, true));

    static bool Granice(Transform t, out Bounds b)
    {
        b = default; bool any = false;
        foreach (var r in t.GetComponentsInChildren<Renderer>(true)) { if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
        foreach (var c in t.GetComponentsInChildren<Collider>(true)) { if (!c.enabled) continue; if (!any) { b = c.bounds; any = true; } else b.Encapsulate(c.bounds); }
        return any;
    }

    /// <summary>Węzły po nazwie (dokładnej albo początku), w dowolnym miejscu hierarchii sceny.</summary>
    public static List<Transform> PoNazwach(Scene scene, params string[] nazwy)
    {
        var res = new List<Transform>();
        foreach (var root in scene.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (nazwy.Any(n => t.name == n || (n.EndsWith("*") && t.name.StartsWith(n.TrimEnd('*')))))
                    if (!res.Any(x => t.IsChildOf(x))) res.Add(t);
        return res;
    }

    /// <summary>Największe poddrzewa, których cała bryła (renderery + kolizje) mieści się w obszarze.</summary>
    public static List<Transform> WObszarze(Scene scene, Bounds obszar)
    {
        var res = new List<Transform>();
        void Zbierz(Transform t)
        {
            if (!Granice(t, out var b)) return;
            if (obszar.Contains(b.min) && obszar.Contains(b.max) && !MaZostajace(t) && !(t.GetComponent<Light>() && t.GetComponent<Light>().type == LightType.Directional))
            { res.Add(t); return; }
            if (!b.Intersects(obszar)) return;
            // Build 8: 64 drzewa (Verdant, LOD) na granicy osiedla — pień przeniesiony do obszaru, korona została
            // (LODGroup traci odwołanie między scenami → korona wisi w powietrzu, dopóki obszar się nie wczyta).
            // LODGroup is atomic; so is ANY scripted controller with child
            // renderers/colliders. Splitting its descendants between scenes
            // destroys serialized Renderer/Transform references on SaveScene.
            // City #97/#98: BraxSwiatloDrogowe at (-750,29) remained in
            // BraxhornCity while three signal lenses moved to K_x-5_z0.
            // Full subtree inside cell is moved as one unit by the clause above;
            // otherwise leave it INTACT in the primary scene.
            if (t.GetComponent<LODGroup>() || t.GetComponent<MonoBehaviour>()) return;
            foreach (Transform c in t) Zbierz(c);
        }
        foreach (var root in scene.GetRootGameObjects()) Zbierz(root.transform);
        return res;
    }

    /// <summary>
    /// Early, real-Unity regression for the #97/#98 city failure. This creates
    /// disposable traffic lights in the currently empty authoring scene,
    /// without changing geometry, assets, scene names or final game visuals.
    /// </summary>
    public static void SprawdzNiepodzielnoscSterownikow(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded || scene.rootCount != 0)
            throw new InvalidOperationException("BRAXHORN_STREAMING_TEST requires an empty authoring scene");
        var root = new GameObject("__BRAXHORN_STREAMING_ATOMIC_TEST__");
        GameObject ordinary = null;
        try
        {
            var controller = root.AddComponent<BraxSwiatloDrogowe>();
            var red = GameObject.CreatePrimitive(PrimitiveType.Cube);
            red.name = "szkla czerwone (test)";
            red.transform.SetParent(root.transform, false);
            red.transform.localPosition = Vector3.zero;
            var green = GameObject.CreatePrimitive(PrimitiveType.Cube);
            green.name = "szkla zielone (test)";
            green.transform.SetParent(root.transform, false);
            green.transform.localPosition = new Vector3(9f, 0f, 0f);
            controller.czerwoneA = red.GetComponent<Renderer>();
            controller.zieloneA = green.GetComponent<Renderer>();
            Physics.SyncTransforms();

            // Red lens is wholly inside the cell, green lens outside.
            // The controller spans the boundary, so NEVER split either lens.
            var partial = WObszarze(scene,
                new Bounds(Vector3.zero, new Vector3(4f, 6f, 6f)));
            if (partial.Any(t => t == red.transform || t == green.transform || t == root.transform))
                throw new InvalidOperationException("BRAXHORN_STREAMING_TEST_SPLIT_CONTROLLER");

            // In a large cell, the complete scripted subtree must move together.
            var full = WObszarze(scene,
                new Bounds(new Vector3(4.5f, 0f, 0f), new Vector3(24f, 6f, 6f)));
            if (full.Count != 1 || full[0] != root.transform
                || controller.czerwoneA != red.GetComponent<Renderer>()
                || controller.zieloneA != green.GetComponent<Renderer>())
                throw new InvalidOperationException("BRAXHORN_STREAMING_TEST_LOST_REFERENCES");

            // Non-scripted meshes must STILL partition normally: this fix
            // cannot silently keep the entire city in the primary APK scene.
            ordinary = new GameObject("__BRAXHORN_STREAMING_ORDINARY_TEST__");
            var inCell = GameObject.CreatePrimitive(PrimitiveType.Cube);
            inCell.transform.SetParent(ordinary.transform, false);
            inCell.transform.localPosition = Vector3.zero;
            var outCell = GameObject.CreatePrimitive(PrimitiveType.Cube);
            outCell.transform.SetParent(ordinary.transform, false);
            outCell.transform.localPosition = new Vector3(9f, 0f, 0f);
            Physics.SyncTransforms();
            var ordinaryParts = WObszarze(scene,
                new Bounds(Vector3.zero, new Vector3(4f, 6f, 6f)));
            if (!ordinaryParts.Contains(inCell.transform)
                || ordinaryParts.Contains(outCell.transform)
                || ordinaryParts.Contains(ordinary.transform))
                throw new InvalidOperationException("BRAXHORN_STREAMING_TEST_DISABLED_ORDINARY_PARTITION");

            Debug.Log("BRAXHORN_STREAMING_PREFLIGHT_PASS: scripted signals remain atomic; ordinary meshes still partition");
        }
        finally
        {
            if (ordinary) UnityEngine.Object.DestroyImmediate(ordinary);
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    /// <summary>Przenosi węzły do nowej sceny (dodatkowej), zapisuje ją i dopisuje obszar do BraxObszary. Zwraca ścieżkę sceny albo null.</summary>
    public static string Wydziel(Scene main, string nazwaSceny, List<Transform> wezly, float wczytaj, float zwolnij, string folder)
    {
        wezly = wezly.Where(t => t).ToList();
        if (wezly.Count == 0) { Debug.LogWarning("DO POPRAWKI: obszar " + nazwaSceny + " — nic do przeniesienia"); return null; }
        // Build 7: „Cannot create a new scene additively with an untitled scene unsaved” — scena główna musi być już zapisana.
        if (string.IsNullOrEmpty(main.path)) throw new InvalidOperationException("Scena główna niezapisana — wywołaj Wydziel po SaveScene");
        // 1) zostające obiekty wyjęte z przenoszonych poddrzew (do korzenia sceny głównej)
        int zostawione = 0;
        foreach (var w in wezly.ToArray())
            foreach (var z in Zostaja)
                foreach (var comp in w.GetComponentsInChildren(z, true))
                {
                    var t = ((Component)comp).transform;
                    if (t == w) { wezly.Remove(w); continue; }
                    t.SetParent(null, true); zostawione++;
                }
        // 2) drzwi z przenoszonych poddrzew znikają z tablicy świata (działają przez rejestr BraxCityDoor.All)
        foreach (var world in main.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<BraxCityWorld>(true)))
            world.doors = world.doors.Where(d => d && !wezly.Any(w => d.transform.IsChildOf(w))).ToArray();
        // 3) granice obszaru i statystyka
        Bounds gr = default; bool any = false; int rend = 0; long tris = 0;
        foreach (var w in wezly)
        {
            if (Granice(w, out var b)) { if (!any) { gr = b; any = true; } else gr.Encapsulate(b); }
            foreach (var mf in w.GetComponentsInChildren<MeshFilter>(true))
            {
                rend++;
                if (mf.sharedMesh) for (int s = 0; s < mf.sharedMesh.subMeshCount; s++) tris += mf.sharedMesh.GetIndexCount(s) / 3;
            }
        }
        // nazwy przed zamknięciem sceny obszaru (build 8: MissingReferenceException przy dzienniku po CloseScene)
        string nazwy = string.Join(", ", wezly.Take(12).Select(w => w.name));
        // 4) nowa scena, przeniesienie, zapis
        foreach (var w in wezly) w.SetParent(null, true);
        var part = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        foreach (var w in wezly) SceneManager.MoveGameObjectToScene(w.gameObject, part);
        bool krzyzMain = EditorSceneManager.DetectCrossSceneReferences(main), krzyzPart = EditorSceneManager.DetectCrossSceneReferences(part);
        // Unity nie zapisze odwołań pomiędzy niezależnymi scenami. Nie wolno wydać
        // APK z automatycznie wyzerowanymi referencjami — najpierw naprawić rejestrację runtime.
        if (krzyzMain || krzyzPart)
            throw new InvalidOperationException("BRAXHORN_OBSZAR_REFERENCJE_MIEDZY_SCENAMI " + nazwaSceny +
                " main=" + krzyzMain + " part=" + krzyzPart + " — nie zapisano uszkodzonej sceny");
        string path = folder + "/" + nazwaSceny + ".unity";
        if (!EditorSceneManager.SaveScene(part, path)) throw new Exception("Zapis sceny obszaru nieudany: " + path);
        EditorSceneManager.CloseScene(part, true);
        SceneManager.SetActiveScene(main);
        // 5) doczytywanie w scenie głównej
        var holder = main.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<BraxObszary>(true)).FirstOrDefault();
        if (!holder)
        {
            var g = new GameObject("BRAXHORN obszary (doczytywanie w tle)");
            SceneManager.MoveGameObjectToScene(g, main);
            holder = g.AddComponent<BraxObszary>();
        }
        var lista = holder.obszary.ToList();
        lista.Add(new BraxObszary.Obszar { scena = nazwaSceny, granice = gr, wczytaj = wczytaj, zwolnij = zwolnij });
        holder.obszary = lista.ToArray();
        EditorUtility.SetDirty(holder);
        if (!EditorSceneManager.SaveScene(main)) throw new Exception("Ponowny zapis sceny głównej nieudany: " + main.path);
        Debug.Log("BRAXHORN_OBSZAR " + nazwaSceny + ": poddrzew " + wezly.Count + ", siatek " + rend + ", trójkątów " + tris +
                  ", granice " + gr.min.ToString("F0") + " … " + gr.max.ToString("F0") + ", wczytaj " + wczytaj + " m / zwolnij " + zwolnij + " m" +
                  ", zostawione w scenie głównej " + zostawione + (krzyzMain || krzyzPart ? " — UWAGA: odwołania między scenami (wyzerowane przy zapisie)" : "") +
                  " | " + nazwy);
        return path;
    }
}
