using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// SIATKI Z KODU BEZ KOPII W PAMIĘCI TELEFONU (audyt miasta 31.10, Opus). Siatka „do odczytu” (isReadable) jest trzymana
// w telefonie dwa razy: w karcie graficznej i w zwykłej pamięci. Wszystkie siatki tworzone przez build (cięcia, wyspa,
// plaża, osiedle, BraxCitySystemMesh) były do odczytu. Ostatni krok przed zapisem sceny: siatki samych rendererów
// dostają isReadable = false. Zostają do odczytu: siatki kolizji (MeshCollider), siatki postaci (SkinnedMeshRenderer)
// i siatki z importu modeli (ustawia je importer, zmiana nie byłaby zapisana).
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
    }

    static long Indices(Mesh m)
    {
        long n = 0;
        for (int i = 0; i < m.subMeshCount; i++) n += m.GetIndexCount(i);
        return n;
    }
}
