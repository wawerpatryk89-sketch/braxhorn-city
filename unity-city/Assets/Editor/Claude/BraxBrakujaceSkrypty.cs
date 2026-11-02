using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// BRAMKA „BRAKUJĄCE SKRYPTY” (1.11, 22:20). Diagnoza awarii: osiedle 12 — 0 brakujących skryptów, działa; osiedle 13 — 4,
// pada przy wczytywaniu (80%, przed pierwszym Awake); miasto — 30–36 od wielu buildów, pada tak samo. Brakujący skrypt
// powstaje, gdy klasa MonoBehaviour leży w pliku o innej nazwie (Unity nie zapisuje wtedy odwołania do skryptu w scenie),
// a dane takiego komponentu w APK nie mają opisu typu → czytanie sceny na telefonie się wykoleja.
// Przed każdym BuildPlayer: każda scena paczki (także obszary doczytywane i kwadraty) jest otwierana i sprawdzana.
// 2.11, 01:10 (decyzja dyspozytora po konsultacji właściciela): BRAMKA TWARDA — jeśli jakakolwiek scena ma komponent bez
// skryptu, build się ZATRZYMUJE (wyjątek, bez APK), zamiast po cichu usuwać komponent (usunięcie zabrałoby funkcję, np.
// siedzisko). Dziennik CI: BRAXHORN_BRAKUJACE_SKRYPTY <scena>: N — nazwy obiektów; wymagane 0 we wszystkich scenach.
public static class BraxBrakujaceSkrypty
{
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
            if (!System.IO.File.Exists(path)) continue;
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            int n = 0; var nazwy = new List<string>();
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    int m = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                    if (m <= 0) continue;
                    n += m; if (nazwy.Count < 20) nazwy.Add(t.name);
                }
            Debug.Log("BRAXHORN_BRAKUJACE_SKRYPTY " + System.IO.Path.GetFileNameWithoutExtension(path) + ": " + n +
                      (n > 0 ? " — BŁĄD (klasa MonoBehaviour w pliku o innej nazwie albo usunięta): " + string.Join(" | ", nazwy) : ""));
            razem += n;
        }
        Debug.Log("BRAXHORN_BRAKUJACE_SKRYPTY razem: " + razem);
        if (razem > 0) throw new System.InvalidOperationException("BRAXHORN_BRAKUJACE_SKRYPTY: " + razem + " komponentów bez skryptu — build ZATRZYMANY (telefon nie wczyta takiej sceny: „level is corrupted [Position out of bounds]”)");
        return razem;
    }
}
