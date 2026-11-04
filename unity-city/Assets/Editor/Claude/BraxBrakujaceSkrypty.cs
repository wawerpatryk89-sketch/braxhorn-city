using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Kontrola kontraktu komponentów przed generowaniem oraz po ponownym otwarciu scen.
// Sam licznik brakujących skryptów nie wykrywa komponentu wskazującego istniejący,
// ale niewłaściwy MonoScript (build 58: BraxMorzeZaKamera w BraxAkwen.cs).
public static class BraxBrakujaceSkrypty
{
    // Typy gry muszą mieć własny, prawidłowo zaimportowany MonoScript.
    // Nie tworzymy instancji i nie uruchamiamy Awake/OnEnable podczas tej kontroli.
    public static void SprawdzTypy()
    {
        var typySkryptow = new HashSet<System.Type>(MonoImporter.GetAllRuntimeMonoScripts()
            .Where(s => s.GetClass() != null && PoprawnaNazwa(s, s.GetClass()))
            .Select(s => s.GetClass()));
        var bledy = TypeCache.GetTypesDerivedFrom<MonoBehaviour>()
            .Where(t => t.Assembly.GetName().Name == "Assembly-CSharp" && !t.IsAbstract && !t.ContainsGenericParameters)
            .Where(t => !typySkryptow.Contains(t)).Select(t => t.FullName).OrderBy(n => n).ToArray();
        if (bledy.Length > 0)
            throw new System.InvalidOperationException("BRAXHORN_TYPY_SKRYPTOW: brak jednoznacznego MonoScript: " + string.Join(", ", bledy));
        Debug.Log("BRAXHORN_TYPY_SKRYPTOW: OK");
    }

    static bool PoprawnaNazwa(MonoScript script, System.Type type)
    {
        string path = AssetDatabase.GetAssetPath(script);
        if (string.IsNullOrEmpty(path)) return false; // transient MonoScript cannot supply player serialization data
        return !path.EndsWith(".cs", System.StringComparison.OrdinalIgnoreCase) ||
            System.IO.Path.GetFileNameWithoutExtension(path) == type.Name;
    }

    public static string BladKomponentu(MonoBehaviour component)
    {
        var script = MonoScript.FromMonoBehaviour(component);
        var actual = component.GetType();
        if (!script) return actual.FullName + ": brak MonoScript";
        var imported = script.GetClass();
        if (imported != actual)
            return actual.FullName + ": MonoScript wskazuje " + (imported == null ? "null" : imported.FullName) +
                " (" + AssetDatabase.GetAssetPath(script) + ")";
        // Unity 6000.0.58f2 accepts the old binding in the editor but writes only
        // the 32-byte base header into an Android bundle. Require explicit files.
        if (!PoprawnaNazwa(script, actual))
            return actual.FullName + ": brak trwałego pliku MonoScript zgodnego z klasą (" + AssetDatabase.GetAssetPath(script) + ")";
        var path = AssetDatabase.GetAssetPath(script);
        if (path.EndsWith(".cs", System.StringComparison.OrdinalIgnoreCase) &&
            !UnityEditor.Compilation.CompilationPipeline.GetAssemblies(UnityEditor.Compilation.AssembliesType.Player)
                .Any(a => a.name == actual.Assembly.GetName().Name))
            return actual.FullName + ": skrypt nie należy do kompilacji gracza";
        return null;
    }

    public static int Sprawdz(IEnumerable<string> sceny)
    {
        int razem = 0;
        // otwarte, zapisane wcześniej sceny z niezapisanymi zmianami — zapis, żeby otwarcie kolejnej ich nie porzuciło
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
        {
            var s = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
            if (s.isLoaded && s.isDirty && !string.IsNullOrEmpty(s.path)) EditorSceneManager.SaveScene(s);
        }
        foreach (var path in sceny.Where(p => !string.IsNullOrEmpty(p)).Distinct())
        {
            if (!System.IO.File.Exists(path)) throw new System.IO.FileNotFoundException("Brak sceny do kontroli", path);
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            int n = 0; var nazwy = new List<string>();
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    int m = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                    n += m;
                    if (m > 0 && nazwy.Count < 20) nazwy.Add(t.name + ": brak skryptu");
                    foreach (var component in t.GetComponents<MonoBehaviour>())
                    {
                        if (!component) continue; // policzony wyżej
                        string blad = BladKomponentu(component);
                        if (blad == null) continue;
                        n++;
                        if (nazwy.Count < 20) nazwy.Add(t.name + ": " + blad);
                    }
                }
            Debug.Log("BRAXHORN_BRAKUJACE_SKRYPTY " + System.IO.Path.GetFileNameWithoutExtension(path) + ": " + n +
                      (n > 0 ? " — BŁĄD przypisania skryptu: " + string.Join(" | ", nazwy) : ""));
            razem += n;
        }
        Debug.Log("BRAXHORN_BRAKUJACE_SKRYPTY razem: " + razem);
        if (razem > 0) throw new System.InvalidOperationException("BRAXHORN_BRAKUJACE_SKRYPTY: " + razem + " komponentów z nieprawidłowym skryptem — build ZATRZYMANY przed pakowaniem");
        return razem;
    }
}
