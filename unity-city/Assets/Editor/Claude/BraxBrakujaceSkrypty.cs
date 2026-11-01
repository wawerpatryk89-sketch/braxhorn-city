using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// BRAMKA „BRAKUJĄCE SKRYPTY” (1.11, 22:20). Diagnoza awarii: osiedle 12 — 0 brakujących skryptów, działa; osiedle 13 — 4,
// pada przy wczytywaniu (80%, przed pierwszym Awake); miasto — 30–36 od wielu buildów, pada tak samo. Brakujący skrypt
// powstaje, gdy klasa MonoBehaviour leży w pliku o innej nazwie (Unity nie zapisuje wtedy odwołania do skryptu w scenie),
// a dane takiego komponentu w APK nie mają opisu typu → czytanie sceny na telefonie się wykoleja.
// Przed każdym BuildPlayer: każda scena paczki jest otwierana, komponenty bez skryptu usuwane, scena zapisywana.
// Dziennik CI: BRAXHORN_BRAKUJACE_SKRYPTY — liczba i nazwy obiektów (od teraz ma być 0; inna liczba = do poprawy w kodzie).
public static class BraxBrakujaceSkrypty
{
    public static int Usun(IEnumerable<string> sceny)
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
            if (!System.IO.File.Exists(path)) continue;
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            int n = 0; var nazwy = new List<string>();
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    int m = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                    if (m <= 0) continue;
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                    n += m; if (nazwy.Count < 12) nazwy.Add(t.name);
                }
            if (n > 0) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
            Debug.Log("BRAXHORN_BRAKUJACE_SKRYPTY " + System.IO.Path.GetFileNameWithoutExtension(path) + ": " + n +
                      (n > 0 ? " usunięte (DO POPRAWKI w kodzie: klasa w pliku o innej nazwie) — " + string.Join(" | ", nazwy) : ""));
            razem += n;
        }
        AssetDatabase.SaveAssets();
        return razem;
    }
}
