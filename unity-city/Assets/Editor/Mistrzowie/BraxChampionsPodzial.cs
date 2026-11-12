using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// THE REGION SPLIT INTO AREA SCENES (owner 12.11.2026 20:46: "masz zobaczyć, jak miasto R1 jest zrobione, i zrobić tak
// samo ... jedna ta sama gra, tylko że podzielona"). The method is the city R1's (BraxClaudeObszary.Wydziel +
// BraxClaudeZDaleka by the R1 author, read on claude/miasto-opus-20261031 @ 0e92029), written for this region's own
// loader (BraxChampionsObszary, which loads through the APK's content bundles):
//   - every big building (hotel, arena, gallery + cinema, station, entertainment centre, casino, the four apartment
//     blocks, the aquapark) and every metro station leaves the exploration scene for a scene of its own;
//   - before it leaves, a far copy is made of its big surfaces (one mesh, the same materials, vertices clustered) and
//     stays in the main scene: the building is seen from any distance, the real one replaces the copy on approach;
//   - the ground, the streets, the waterfront, the boats, the car, the walking characters and the player stay.
// What makes it safe:
//   - an area leaves only if no saved reference crosses its border (every component's references are read first);
//     an area with such a reference stays in the main scene whole and is named in the log (A1_PODZIAL_SKIPPED);
//   - after the move Unity itself is asked (DetectCrossSceneReferences); a hit moves the area back;
//   - any error in one area leaves that area in the main scene; the build goes on with the rest.
public static class BraxChampionsPodzial
{
    public const string Folder = "Assets/PlayableGenerated/Mistrzowie/Obszary";
    const float MinRozmiar = 2.5f;
    const int MaxTrisLodNaObiekt = 1500, MinTrisNaMaterial = 24;

    struct Plan { public string scena, poczatek, koniec; public float wczytaj, zwolnij; public int maxTris; }
    static Plan Strefa(string scena, string poczatek, float wczytaj = 150f, float zwolnij = 280f, int maxTris = 60000, string koniec = null)
        => new Plan { scena = scena, poczatek = poczatek, koniec = koniec, wczytaj = wczytaj, zwolnij = zwolnij, maxTris = maxTris };
    static readonly Plan[] Plany =
    {
        Strefa("Mistrzowie_T03_HotelSobczak", "T03 | HOTEL SOBCZAK", 150f, 280f, 80000),
        Strefa("Mistrzowie_G1_Galeria", "G1 | GALERIA", 150f, 280f, 80000),
        Strefa("Mistrzowie_A1_Arena", "A1 | BRAXHORN CHAMPION ARENA", 150f, 280f, 80000),
        Strefa("Mistrzowie_D2_Dworzec", "D2 | DWORZEC", 150f, 280f, 80000),
        Strefa("Mistrzowie_E1_Rozrywka", "E1 | CENTRUM ROZRYWKI"),
        Strefa("Mistrzowie_T11_Kasyno", "T11 | KASYNO WAWER"),
        Strefa("Mistrzowie_T04_Apartamenty", "T04 | APARTAMENTY"),
        Strefa("Mistrzowie_T05_Apartamenty", "T05 | APARTAMENTY"),
        Strefa("Mistrzowie_T06_Apartamenty", "T06 | APARTAMENTY"),
        Strefa("Mistrzowie_T07_Apartamenty", "T07 | APARTAMENTY"),
        Strefa("Mistrzowie_Aquapark", "AQUAPARK BRAXHORN"),
        Strefa("Mistrzowie_M1_Metro", "M1 | ", 120f, 250f, 30000, "| reused city metro"),
        Strefa("Mistrzowie_M2_Metro", "M2 | ", 120f, 250f, 30000, "| reused city metro"),
        Strefa("Mistrzowie_M3_Metro", "M3 | ", 120f, 250f, 30000, "| reused city metro"),
        Strefa("Mistrzowie_M4_Metro", "M4 | ", 120f, 250f, 30000, "| reused city metro"),
        Strefa("Mistrzowie_M5_Metro", "M5 | ", 120f, 250f, 30000, "| reused city metro"),
    };

    // never leave the main scene (the player must not vanish with an area; these move through several areas)
    static readonly Type[] Zostaja =
    {
        typeof(BraxGymPlayer), typeof(BraxCityWorld), typeof(AudioListener), typeof(BraxVessel), typeof(BraxChampionsCar), typeof(BraxPieszy),
        typeof(BraxChampionsObszary), typeof(BraxChampionsObszarZDaleka), typeof(BraxChampionsWydajnosc), typeof(BraxChampionsVisualReview)
    };

    /// <summary>the area scenes saved by the last split (empty when the region was not split)</summary>
    public static string[] Sceny() => Directory.Exists(Folder)
        ? Directory.GetFiles(Folder, "Mistrzowie_*.unity").Select(p => p.Replace('\\', '/')).OrderBy(p => p, StringComparer.Ordinal).ToArray() : new string[0];

    /// <summary>Splits the saved exploration scene. Returns the number of area scenes. Never throws for one area's sake.</summary>
    public static int Podziel(string mainPath)
    {
        var t0 = DateTime.UtcNow;
        var main = EditorSceneManager.OpenScene(mainPath, OpenSceneMode.Single);
        if (All<BraxChampionsObszary>(main).Length != 0) throw new InvalidOperationException("The exploration scene is already split.");
        BraxChampionsMeshBatch.Folder(Folder);
        Wyczysc();                                                              // no area scene of an earlier run beside this one
        // the world's door table is not used (doors work through BraxCityDoor.All); a saved door there would tie its building to the main scene
        foreach (var w in All<BraxCityWorld>(main)) if (w.doors != null && w.doors.Length != 0) { w.doors = Array.Empty<BraxCityDoor>(); EditorUtility.SetDirty(w); }
        var krawedzie = Odwolania(main);
        var holderGo = new GameObject("MIASTO MISTRZÓW | obszary (doczytywanie w tle)");
        SceneManager.MoveGameObjectToScene(holderGo, main);
        var holder = holderGo.AddComponent<BraxChampionsObszary>();
        var lista = new List<BraxChampionsObszary.Obszar>(); int pominiete = 0; long daleko = 0;
        foreach (var plan in Plany)
        {
            try
            {
                var wezly = Wezly(main, plan);
                if (wezly.Count == 0) { Debug.Log("A1_PODZIAL " + plan.scena + ": not in this scene"); continue; }
                var o = Wydziel(main, plan, wezly, krawedzie, ref daleko);
                if (o != null) lista.Add(o); else pominiete++;
            }
            catch (Exception e) { pominiete++; Debug.LogWarning("A1_PODZIAL_SKIPPED " + plan.scena + ": " + e); }
            if (SceneManager.GetActiveScene() != main) SceneManager.SetActiveScene(main);
        }
        holder.obszary = lista.ToArray(); EditorUtility.SetDirty(holder);
        if (EditorSceneManager.DetectCrossSceneReferences(main)) Debug.LogWarning("A1_PODZIAL main scene reports cross-scene references after the split");
        AssetDatabase.SaveAssets();
        if (!EditorSceneManager.SaveScene(main)) throw new IOException("The split exploration scene was not saved.");
        int zostalo = main.GetRootGameObjects().Sum(g => g.GetComponentsInChildren<Transform>(true).Length);
        Debug.Log("A1_PODZIAL areas=" + lista.Count + " skipped=" + pominiete + " mainObjects=" + zostalo + " farTriangles=" + daleko +
                  " t=" + (DateTime.UtcNow - t0).TotalSeconds.ToString("F0") + " s | " + string.Join(", ", lista.Select(x => x.scena)));
        return lista.Count;
    }

    /// <summary>removes every saved area scene and far copy (the region is then whole in the exploration scene, or re-split)</summary>
    public static void Wyczysc()
    {
        if (!Directory.Exists(Folder)) return;
        foreach (var f in Directory.GetFiles(Folder).Where(f => f.EndsWith(".unity") || f.EndsWith(".asset"))) AssetDatabase.DeleteAsset(f.Replace('\\', '/'));
    }

    static T[] All<T>(Scene s) where T : Component => s.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).ToArray();

    static List<Transform> Wezly(Scene main, Plan plan)
    {
        var res = new List<Transform>();
        foreach (var root in main.GetRootGameObjects())
        {
            if (plan.koniec == null) { if (root.name.StartsWith(plan.poczatek, StringComparison.Ordinal)) res.Add(root.transform); continue; }
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith(plan.poczatek, StringComparison.Ordinal) && t.name.EndsWith(plan.koniec, StringComparison.Ordinal) && !res.Any(x => t.IsChildOf(x))) res.Add(t);
        }
        return res;
    }

    // every saved reference from one scene object to another (owner object -> target object), read once
    static List<(GameObject z, GameObject d, string co)> Odwolania(Scene main)
    {
        var res = new List<(GameObject, GameObject, string)>();
        foreach (var root in main.GetRootGameObjects())
            foreach (var c in root.GetComponentsInChildren<Component>(true))
            {
                if (!c || c is Transform || c is MeshFilter || c is MeshRenderer || c is Collider || c is Light || c is Rigidbody) continue;
                var so = new SerializedObject(c); var it = so.GetIterator(); bool wejdz = true;
                while (it.Next(wejdz))
                {
                    wejdz = true;
                    if (it.propertyType == SerializedPropertyType.String) { wejdz = false; continue; }
                    if (it.isArray && it.arraySize > 0)
                    {
                        var e = it.GetArrayElementAtIndex(0).propertyType;      // long arrays of numbers are not walked
                        if (e != SerializedPropertyType.ObjectReference && e != SerializedPropertyType.Generic && e != SerializedPropertyType.ManagedReference) { wejdz = false; continue; }
                    }
                    if (it.propertyType != SerializedPropertyType.ObjectReference) continue;
                    var v = it.objectReferenceValue; if (!v) continue;
                    GameObject cel = v as GameObject; if (!cel && v is Component vc) cel = vc.gameObject;
                    if (!cel || cel == c.gameObject || cel.scene != main) continue;
                    res.Add((c.gameObject, cel, c.GetType().Name + "." + it.propertyPath));
                }
            }
        Debug.Log("A1_PODZIAL references between scene objects: " + res.Count);
        return res;
    }

    static bool Granice(Transform t, ref Bounds b, ref bool any)
    {
        foreach (var r in t.GetComponentsInChildren<Renderer>(true)) { if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
        foreach (var c in t.GetComponentsInChildren<Collider>(true)) { if (!c.enabled || c.isTrigger) continue; if (!any) { b = c.bounds; any = true; } else b.Encapsulate(c.bounds); }
        return any;
    }

    static BraxChampionsObszary.Obszar Wydziel(Scene main, Plan plan, List<Transform> wezly, List<(GameObject z, GameObject d, string co)> krawedzie, ref long daleko)
    {
        // 1) what must stay is taken out of the leaving subtrees (to the main scene's root); a node that itself must stay, stays
        wezly.RemoveAll(w => Zostaja.Any(typ => w.GetComponent(typ)));
        foreach (var w in wezly.ToArray())
            foreach (var typ in Zostaja)
                foreach (var comp in w.GetComponentsInChildren(typ, true))
                {
                    var t = comp.transform;
                    if (t == w || !t) continue;
                    var poza = PrefabUtility.GetOutermostPrefabInstanceRoot(t.gameObject);
                    if (poza && poza != t.gameObject) PrefabUtility.UnpackPrefabInstance(poza, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    t.SetParent(null, true);
                }
        if (wezly.Count == 0) { Debug.LogWarning("A1_PODZIAL_SKIPPED " + plan.scena + ": nothing may leave"); return null; }
        // a node inside a prefab instance cannot be re-parented: its instance is unpacked (the build expands instances anyway)
        foreach (var w in wezly)
        {
            var poza = PrefabUtility.GetOutermostPrefabInstanceRoot(w.gameObject);
            if (poza && poza != w.gameObject) PrefabUtility.UnpackPrefabInstance(poza, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        }
        // 2) no saved reference may cross the border
        var wSrodku = new HashSet<GameObject>();
        foreach (var w in wezly) foreach (var t in w.GetComponentsInChildren<Transform>(true)) wSrodku.Add(t.gameObject);
        var przez = krawedzie.Where(k => k.z && k.d && wSrodku.Contains(k.z) != wSrodku.Contains(k.d)).ToList();
        if (przez.Count > 0)
        {
            Debug.LogWarning("A1_PODZIAL_SKIPPED " + plan.scena + ": " + przez.Count + " references cross its border, e.g. " +
                string.Join(" ; ", przez.Take(8).Select(k => k.z.name + " -[" + k.co + "]-> " + k.d.name)));
            return null;
        }
        // 3) the far copy from the same nodes, before they leave (a failed copy does not stop the split)
        GameObject kopia = null;
        try { kopia = ZDaleka(main, plan.scena, wezly, .5f, plan.maxTris, ref daleko); }
        catch (Exception e) { Debug.LogWarning("A1_PODZIAL far copy " + plan.scena + ": " + e.Message); }
        // 4) bounds and numbers
        Bounds gr = default; bool any = false; int siatki = 0, obiekty = wSrodku.Count;
        foreach (var w in wezly) { Granice(w, ref gr, ref any); siatki += w.GetComponentsInChildren<MeshFilter>(true).Length; }
        if (!any) { if (kopia) UnityEngine.Object.DestroyImmediate(kopia); Debug.LogWarning("A1_PODZIAL_SKIPPED " + plan.scena + ": no bounds"); return null; }
        // 5) the new scene, the move, Unity's own check, the save
        var rodzice = wezly.Select(w => w.parent).ToArray(); var kolejnosc = wezly.Select(w => w.GetSiblingIndex()).ToArray();
        foreach (var w in wezly) w.SetParent(null, true);
        var part = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        foreach (var w in wezly) SceneManager.MoveGameObjectToScene(w.gameObject, part);
        string path = Folder + "/" + plan.scena + ".unity"; bool zapisano = false; string powod = null;
        if (EditorSceneManager.DetectCrossSceneReferences(part) || EditorSceneManager.DetectCrossSceneReferences(main)) powod = "Unity found a cross-scene reference";
        else { zapisano = EditorSceneManager.SaveScene(part, path); if (!zapisano) powod = "scene save failed"; }
        if (!zapisano)
        {
            // back where it was: the main scene is whole again
            for (int i = 0; i < wezly.Count; i++)
            {
                SceneManager.MoveGameObjectToScene(wezly[i].gameObject, main);
                if (rodzice[i]) { wezly[i].SetParent(rodzice[i], true); wezly[i].SetSiblingIndex(kolejnosc[i]); }
            }
            EditorSceneManager.CloseScene(part, true); SceneManager.SetActiveScene(main);
            if (kopia) UnityEngine.Object.DestroyImmediate(kopia);
            Debug.LogWarning("A1_PODZIAL_SKIPPED " + plan.scena + ": " + powod + " (moved back)");
            return null;
        }
        EditorSceneManager.CloseScene(part, true); SceneManager.SetActiveScene(main);
        Debug.Log("A1_PODZIAL_AREA " + plan.scena + " objects=" + obiekty + " meshes=" + siatki + " bounds=" + gr.min.ToString("F0") + ".." + gr.max.ToString("F0") +
                  " load=" + plan.wczytaj + " release=" + plan.zwolnij + " farCopy=" + (kopia ? "yes" : "no") + " bytes=" + new FileInfo(path).Length);
        return new BraxChampionsObszary.Obszar { scena = plan.scena, granice = gr, wczytaj = plan.wczytaj, zwolnij = plan.zwolnij };
    }

    // ================================================================ the far copy (the R1 author's method, BraxClaudeZDaleka)
    // Big surfaces only (largest side >= 2.5 m): vertices in one cell with one normal direction become one vertex
    // (Rossignac-Borrel clustering), degenerate triangles drop out; objects with a LODGroup give their last level as it is;
    // the same materials; one mesh per area, one sub-mesh per material; the cell grows until the area has at most maxTris.
    struct Zrodlo { public Renderer r; public Mesh m; public Matrix4x4 l2w; public bool dokladne; }
    sealed class Kubel
    {
        public Material mat;
        public readonly List<Vector3> v = new List<Vector3>(), n = new List<Vector3>();
        public readonly List<Vector4> t = new List<Vector4>();
        public readonly List<Vector2> uv = new List<Vector2>();
        public readonly List<int> i = new List<int>();
        public readonly Dictionary<long, int> mapa = new Dictionary<long, int>();
    }
    sealed class Dane { public Vector3[] v, n; public Vector4[] t; public Vector2[] uv; public int[][] sub; }

    static bool Pomin(Renderer r, List<Transform> wezly)
    {
        if (r.GetComponentInParent<Rigidbody>(true) || r.GetComponentInParent<Animator>(true) || r.GetComponentInParent<Animation>(true)
            || r.GetComponentInParent<BraxChampionsObrot>(true) || r.GetComponentInParent<BraxChampionsD2Pociag>(true)) return true;
        // an interior drawn only from inside (rooms, halls, the underground station) is not part of the view from far
        var wn = r.GetComponentInParent<BraxWidocznoscWnetrza>(true);
        return wn && !wezly.Contains(wn.transform);
    }
    static bool Alfa(Material m)
    {
        if (!m) return false;
        int q = m.renderQueue;                                                  // cut-out only: glass is clustered like a wall
        return q >= 2450 && q < 3000 || m.IsKeywordEnabled("_ALPHATEST_ON");
    }
    static int Tris(Mesh m) { long s = 0; for (int i = 0; i < m.subMeshCount; i++) s += m.GetIndexCount(i) / 3; return (int)Math.Min(s, int.MaxValue); }

    static List<Zrodlo> Zbierz(List<Transform> wezly)
    {
        var res = new List<Zrodlo>();
        var wLod = new HashSet<Renderer>(); var ostatni = new HashSet<Renderer>();
        foreach (var w in wezly)
            foreach (var g in w.GetComponentsInChildren<LODGroup>(true))
            {
                if (!g.gameObject.activeInHierarchy || !g.enabled) continue;
                var lods = g.GetLODs();
                foreach (var l in lods) foreach (var r in l.renderers) if (r) wLod.Add(r);
                for (int k = lods.Length - 1; k >= 0; k--)
                {
                    var rs = lods[k].renderers.Where(r => r).ToArray();
                    if (rs.Length == 0) continue;
                    foreach (var r in rs) ostatni.Add(r);
                    break;
                }
            }
        foreach (var w in wezly)
            foreach (var r in w.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy || Pomin(r, wezly)) continue;
                var mf = r.GetComponent<MeshFilter>(); if (!mf || !mf.sharedMesh) continue;
                var mats = r.sharedMaterials;
                if (mats.Length == 0 || mats.All(m => !m)) continue;
                bool lod = wLod.Contains(r);
                if (lod && !ostatni.Contains(r)) continue;                       // nearer LOD levels are not seen from far
                var b = r.bounds; if (Mathf.Max(b.size.x, b.size.y, b.size.z) < MinRozmiar) continue;
                bool dokladne = lod || mats.Any(Alfa);
                if (dokladne && Tris(mf.sharedMesh) > MaxTrisLodNaObiekt) continue;   // glass and cut-out surfaces only when light
                res.Add(new Zrodlo { r = r, m = mf.sharedMesh, l2w = r.transform.localToWorldMatrix, dokladne = dokladne });
            }
        return res;
    }

    static Dane Czytaj(Mesh m, Dictionary<Mesh, Dane> cache)
    {
        if (cache.TryGetValue(m, out var d)) return d;
        d = new Dane { v = m.vertices, n = m.normals, t = m.tangents, uv = m.uv, sub = new int[m.subMeshCount][] };
        for (int s = 0; s < m.subMeshCount; s++) d.sub[s] = m.GetTopology(s) == MeshTopology.Triangles ? m.GetTriangles(s) : new int[0];
        if (d.v.Length <= 30000) cache[m] = d;                                  // big merged meshes are read when used and let go
        return d;
    }
    static int KlasaNormalnej(Vector3 n)
    {
        float ax = Mathf.Abs(n.x), ay = Mathf.Abs(n.y), az = Mathf.Abs(n.z);
        if (ax >= ay && ax >= az) return n.x >= 0 ? 0 : 1;
        if (ay >= az) return n.y >= 0 ? 2 : 3;
        return n.z >= 0 ? 4 : 5;
    }
    static long Klucz(Vector3 p, float kom, int klasa)
    {
        const long M = (1L << 20) - 1, O = 1L << 19;
        long x = (Mathf.FloorToInt(p.x / kom) + O) & M, y = (Mathf.FloorToInt(p.y / kom) + O) & M, z = (Mathf.FloorToInt(p.z / kom) + O) & M;
        return x | (y << 20) | (z << 40) | ((long)klasa << 60);
    }
    static Dictionary<Material, Kubel> Grupuj(List<Zrodlo> zr, float kom, Dictionary<Mesh, Dane> cache, out long tris)
    {
        var kub = new Dictionary<Material, Kubel>(); tris = 0;
        foreach (var z in zr)
        {
            var d = Czytaj(z.m, cache); var mats = z.r.sharedMaterials; var l2w = z.l2w; bool odwroc = l2w.determinant < 0f;
            int nv = d.v.Length; bool maN = d.n.Length == nv, maT = d.t.Length == nv, maUv = d.uv.Length == nv;
            for (int s = 0; s < d.sub.Length; s++)
            {
                var mat = s < mats.Length ? mats[s] : null;
                if (!mat || d.sub[s].Length == 0) continue;
                if (!kub.TryGetValue(mat, out var k)) { k = new Kubel { mat = mat }; kub[mat] = k; }
                var idx = new Dictionary<int, int>();
                int Wez(int i)
                {
                    if (idx.TryGetValue(i, out int w)) return w;
                    var p = l2w.MultiplyPoint3x4(d.v[i]);
                    var n = maN ? l2w.MultiplyVector(d.n[i]).normalized : Vector3.up;
                    if (!z.dokladne)
                    {
                        long key = Klucz(p, kom, KlasaNormalnej(n));
                        if (k.mapa.TryGetValue(key, out w)) { idx[i] = w; return w; }
                        w = k.v.Count; k.mapa[key] = w;
                    }
                    else w = k.v.Count;
                    k.v.Add(p); k.n.Add(n);
                    if (maT) { var t = d.t[i]; var tw = l2w.MultiplyVector(new Vector3(t.x, t.y, t.z)).normalized; k.t.Add(new Vector4(tw.x, tw.y, tw.z, odwroc ? -t.w : t.w)); }
                    else k.t.Add(new Vector4(1, 0, 0, 1));
                    k.uv.Add(maUv ? d.uv[i] : Vector2.zero);
                    idx[i] = w; return w;
                }
                var tri = d.sub[s];
                for (int j = 0; j + 2 < tri.Length; j += 3)
                {
                    int a = Wez(tri[j]), b = Wez(tri[j + 1]), c = Wez(tri[j + 2]);
                    if (a == b || b == c || a == c) continue;
                    if (odwroc) { k.i.Add(a); k.i.Add(c); k.i.Add(b); } else { k.i.Add(a); k.i.Add(b); k.i.Add(c); }
                    tris++;
                }
            }
        }
        return kub;
    }

    static GameObject ZDaleka(Scene main, string scena, List<Transform> wezly, float komorka, int maxTris, ref long suma)
    {
        var zr = Zbierz(wezly);
        if (zr.Count == 0) { Debug.Log("A1_Z_DALEKA " + scena + ": no big surfaces, no copy"); return null; }
        var cache = new Dictionary<Mesh, Dane>();
        long zrodlo = zr.Sum(z => (long)Tris(z.m));
        float kom = Mathf.Max(.05f, komorka), uzyta = kom; Dictionary<Material, Kubel> kub = null; long tris = 0;
        for (int przejscie = 0; przejscie < 7; przejscie++)
        {
            uzyta = kom; kub = Grupuj(zr, kom, cache, out tris);
            if (tris <= maxTris) break;
            kom *= Mathf.Max(1.3f, Mathf.Sqrt(tris / (float)maxTris) * 1.1f);     // triangles ~ 1 / cell^2
        }
        cache.Clear();
        var kubly = kub.Values.Where(k => k.i.Count / 3 >= MinTrisNaMaterial).ToList();
        if (kubly.Count == 0) { Debug.Log("A1_Z_DALEKA " + scena + ": nothing left after clustering, no copy"); return null; }
        var mesh = new Mesh { name = "Z daleka: " + scena };
        int nv = kubly.Sum(k => k.v.Count);
        mesh.indexFormat = nv > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        var V = new List<Vector3>(nv); var N = new List<Vector3>(nv); var T = new List<Vector4>(nv); var U = new List<Vector2>(nv);
        var subs = new List<int[]>();
        foreach (var k in kubly)
        {
            int o = V.Count; V.AddRange(k.v); N.AddRange(k.n); T.AddRange(k.t); U.AddRange(k.uv);
            var ii = new int[k.i.Count]; for (int j = 0; j < ii.Length; j++) ii[j] = k.i[j] + o;
            subs.Add(ii);
        }
        mesh.SetVertices(V); mesh.SetNormals(N); mesh.SetTangents(T); mesh.SetUVs(0, U);
        mesh.subMeshCount = subs.Count;
        for (int s = 0; s < subs.Count; s++) mesh.SetTriangles(subs[s], s, false);
        mesh.RecalculateBounds();
        string path = Folder + "/ZDaleka_" + scena + ".asset";
        if (AssetDatabase.LoadAssetAtPath<Mesh>(path)) AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(mesh, path);
        // GPU only: no second copy of the mesh in the phone's memory (nothing reads it in the game)
        var so = new SerializedObject(mesh); var pr = so.FindProperty("m_IsReadable");
        if (pr != null) { pr.boolValue = false; so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(mesh); }

        var root = main.GetRootGameObjects().FirstOrDefault(g => g.name == "MIASTO MISTRZÓW | obszary z daleka");
        if (!root) { root = new GameObject("MIASTO MISTRZÓW | obszary z daleka"); SceneManager.MoveGameObjectToScene(root, main); }
        var go = new GameObject("Z daleka: " + scena);
        go.transform.SetParent(root.transform, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterials = kubly.Select(k => k.mat).ToArray();
        mr.shadowCastingMode = ShadowCastingMode.On; mr.receiveShadows = true;
        mr.lightProbeUsage = LightProbeUsage.Off; mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
        var zd = go.AddComponent<BraxChampionsObszarZDaleka>(); zd.scena = scena; zd.renderery = new Renderer[] { mr };
        long koniec = subs.Sum(x => (long)x.Length / 3); suma += koniec;
        Debug.Log("A1_Z_DALEKA " + scena + ": surfaces " + zr.Count + " (source triangles " + zrodlo + ") -> " + koniec + " triangles, vertices " + nv +
                  ", materials " + kubly.Count + ", cell " + uzyta.ToString("0.00") + " m");
        return go;
    }
}
