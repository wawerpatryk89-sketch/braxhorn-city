using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Explicit edits to persistent scenes. Paths are unique hierarchy paths, not
// transient instance IDs. Missing/ambiguous targets fail the update.
public static class BraxContentEdits
{
    [Serializable] public sealed class Patch { public Edit[] edits; }
    [Serializable] public sealed class Edit
    {
        public string scene, path, operation, prefab;
        public Vector3 position, rotation, scale = Vector3.one;
    }
    public static void ApplyFile(string path, string[] allowed, string recorded = null)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("Brak jawnej listy zmian content-patch.json", path);
        var patch = JsonUtility.FromJson<Patch>(File.ReadAllText(path));
        if (patch == null || patch.edits == null) throw new InvalidDataException("Brak listy zmian.");
        var prior = recorded != null && File.Exists(recorded) ? JsonUtility.FromJson<Patch>(File.ReadAllText(recorded)).edits : new Edit[0];
        if (patch.edits.Length < prior.Length || prior.Where((edit, i) => JsonUtility.ToJson(edit) != JsonUtility.ToJson(patch.edits[i])).Any())
            throw new InvalidDataException("Nie zmieniaj historii zapisanej w bazowym APK. Dopisz nową operację na końcu listy.");
        foreach (var group in patch.edits.Skip(prior.Length).GroupBy(e => e.scene))
        {
            if (!allowed.Contains(group.Key)) throw new InvalidDataException("Scena spoza bazy: " + group.Key);
            var scene = EditorSceneManager.OpenScene(group.Key, OpenSceneMode.Single);
            foreach (var edit in group)
            {
                var matches = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Where(t => Hierarchy(t) == edit.path).ToArray();
                if (matches.Length != 1) throw new InvalidDataException("Cel niejednoznaczny lub nie istnieje: " + edit.path);
                var target = matches[0];
                if (edit.operation != "delete" && (!Finite(edit.position) || !Finite(edit.rotation) || !Finite(edit.scale) ||
                    Mathf.Abs(edit.scale.x) < .00001f || Mathf.Abs(edit.scale.y) < .00001f || Mathf.Abs(edit.scale.z) < .00001f))
                    throw new InvalidDataException("Podaj skończoną pozycję, obrót i niezerową skalę: " + edit.path);
                if (edit.operation == "transform")
                {
                    target.localPosition = edit.position; target.localEulerAngles = edit.rotation; target.localScale = edit.scale;
                }
                else if (edit.operation == "add-prefab")
                {
                    if (string.IsNullOrEmpty(edit.prefab) || !edit.prefab.StartsWith("Assets/", StringComparison.Ordinal)) throw new InvalidDataException("Nieprawidłowy prefab.");
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(edit.prefab);
                    if (!prefab) throw new InvalidDataException("Brak prefabu: " + edit.prefab);
                    var added = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                    added.transform.SetParent(target, false); added.transform.localPosition = edit.position;
                    added.transform.localEulerAngles = edit.rotation; added.transform.localScale = edit.scale;
                }
                else if (edit.operation == "delete") UnityEngine.Object.DestroyImmediate(target.gameObject);
                else throw new InvalidDataException("Nieznana operacja: " + edit.operation);
            }
            if (EditorSceneManager.DetectCrossSceneReferences(scene)) throw new InvalidDataException("Niedozwolone odwołanie między scenami: " + group.Key);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) != 0)
                        throw new InvalidDataException("Brakujący skrypt po aktualizacji: " + Hierarchy(t));
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Nie zapisano sceny: " + group.Key);
        }
    }
    static bool Finite(Vector3 v) => !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) || float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z));
    static string Hierarchy(Transform t) => t.parent ? Hierarchy(t.parent) + "/" + t.name : t.name;
}
