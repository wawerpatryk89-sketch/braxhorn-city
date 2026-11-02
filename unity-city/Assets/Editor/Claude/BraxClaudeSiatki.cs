using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// SIATKI Z KODU BEZ KOPII W PAMIĘCI TELEFONU (audyt miasta 31.10, Opus). Siatka „do odczytu” (isReadable) jest trzymana
// w telefonie dwa razy: w karcie graficznej i w zwykłej pamięci. Wszystkie siatki tworzone przez build (cięcia, wyspa,
// plaża, osiedle, BraxCitySystemMesh) były do odczytu. Ostatni krok przed zapisem sceny: siatki samych rendererów
// dostają isReadable = false. Zostają do odczytu: siatki kolizji (MeshCollider), siatki postaci (SkinnedMeshRenderer)
// i siatki z importu modeli (ustawia je importer, zmiana nie byłaby zapisana).
// SIATKI Z IMPORTU (.gltf/.glb przez glTFast, .fbx) — importer zostawia je „do odczytu” i nie da się tego zmienić
// (glTFast 6.20: brak ustawienia). Build 7: osiedle braxhorn-estate.gltf 258 MB siatek, siłownia .glb 82 MB, łącznie
// 636 (miasto) + 478 (osiedle) siatek trzymanych w telefonie podwójnie. Krok KopieZImportu: dla siatek samych rendererów
// powstaje kopia .asset bez odczytu (PlayableGenerated/SiatkiBezOdczytu), a MeshFilter w scenie wskazuje na kopię;
// oryginał nie trafia wtedy do paczki. Siatki kolizji i postaci zostają oryginalne.
// Kolizje: PlayerSettings.bakeCollisionMeshes — dane kolizji liczone przy buildzie, nie przy wczytywaniu sceny.
public static class BraxClaudeSiatki
{
    public static void Apply(Scene scene, string label)
    {
        var keep = new HashSet<Mesh>();
        var roots = scene.GetRootGameObjects();
        foreach (var root in roots)
        {
            foreach (var c in root.GetComponentsInChildren<MeshCollider>(true)) if (c.sharedMesh) keep.Add(c.sharedMesh);
            foreach (var s in root.GetComponentsInChildren<SkinnedMeshRenderer>(true)) if (s.sharedMesh) keep.Add(s.sharedMesh);
        }
        var done = new HashSet<Mesh>();
        int changed = 0, kept = 0, imported = 0; long bytes = 0;
        foreach (var root in roots)
        foreach (var f in root.GetComponentsInChildren<MeshFilter>(true))
        {
            var m = f.sharedMesh;
            if (!m || !done.Add(m)) continue;
            if (keep.Contains(m)) { kept++; continue; }
            string path = AssetDatabase.GetAssetPath(m);
            if (!string.IsNullOrEmpty(path) && (AssetImporter.GetAtPath(path) is ModelImporter || !path.EndsWith(".asset") && !path.EndsWith(".unity")))
            { imported++; continue; }
            var so = new SerializedObject(m);
            var p = so.FindProperty("m_IsReadable");
            if (p == null || !p.boolValue) continue;
            p.boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
            if (!string.IsNullOrEmpty(path)) EditorUtility.SetDirty(m);
            bytes += (long)m.vertexCount * 32 + (long)Indices(m) * 4;
            changed++;
        }
        PlayerSettings.bakeCollisionMeshes = true;
        AssetDatabase.SaveAssets();
        Debug.Log("BRAXHORN_SIATKI " + label + ": bez kopii do odczytu " + changed + " (ok. " + (bytes / (1024 * 1024)) +
                  " MB mniej w pamięci telefonu), zostawione do odczytu: kolizje/postać " + kept + ", z importu modeli " + imported);
        try { KopieZImportu(roots, keep, label); }
        catch (System.Exception e) { Debug.LogWarning("DO POPRAWKI: kopie siatek z importu (" + label + "): " + e.Message); }
    }

    const string Folder = "Assets/PlayableGenerated/SiatkiBezOdczytu";
    static readonly Dictionary<Mesh, Mesh> kopie = new Dictionary<Mesh, Mesh>();

    static void KopieZImportu(GameObject[] roots, HashSet<Mesh> keep, string label)
    {
        if (!AssetDatabase.IsValidFolder(Folder))
        {
            if (!AssetDatabase.IsValidFolder("Assets/PlayableGenerated")) AssetDatabase.CreateFolder("Assets", "PlayableGenerated");
            AssetDatabase.CreateFolder("Assets/PlayableGenerated", "SiatkiBezOdczytu");
        }
        int swapped = 0, created = 0, skipped = 0; long bytes = 0;
        var policzone = new HashSet<Mesh>();
        foreach (var root in roots)
        foreach (var f in root.GetComponentsInChildren<MeshFilter>(true))
        {
            var m = f.sharedMesh;
            if (!m || keep.Contains(m) || !m.isReadable) continue;
            string path = AssetDatabase.GetAssetPath(m);
            if (string.IsNullOrEmpty(path) || path.EndsWith(".asset") || path.EndsWith(".unity")) continue;   // siatki z kodu: krok wyżej
            if (m.vertexCount == 0) { skipped++; continue; }
            if (!kopie.TryGetValue(m, out var c) || !c)
            {
                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(m, out string guid, out long lid)) { skipped++; continue; }
                string file = Folder + "/" + guid + "_" + lid.ToString("x") + ".asset";
                c = AssetDatabase.LoadAssetAtPath<Mesh>(file);
                if (!c)
                {
                    c = UnityEngine.Object.Instantiate(m);
                    c.name = m.name;
                    var so = new SerializedObject(c);
                    var p = so.FindProperty("m_IsReadable");
                    if (p != null) { p.boolValue = false; so.ApplyModifiedPropertiesWithoutUndo(); }
                    BraxZapisAssetu.Utworz(c, file);
                    created++;
                }
                kopie[m] = c;
            }
            if (policzone.Add(m)) bytes += (long)m.vertexCount * 32 + Indices(m) * 4;
            f.sharedMesh = c;
            swapped++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log("BRAXHORN_SIATKI_IMPORT " + label + ": siatki z importu podmienione na kopie bez odczytu: " + policzone.Count +
                  " siatek, " + swapped + " rendererów (nowych kopii " + created + ", pominięte " + skipped + "), ok. " +
                  (bytes / (1024 * 1024)) + " MB mniej w pamięci telefonu");
    }

    static long Indices(Mesh m)
    {
        long n = 0;
        for (int i = 0; i < m.subMeshCount; i++) n += m.GetIndexCount(i);
        return n;
    }
}
