using UnityEngine;
using UnityEngine.SceneManagement;

// AN AREA SEEN FROM FAR (as the city R1's BraxObszarZDaleka): the simplified copy of a building that stays in the main
// scene. It is drawn while the building's own scene is not loaded and hidden in the very frame that scene is in
// (sceneLoaded), so the two never overlap and the building never vanishes; when the scene is let go the copy is back.
// A spare check runs every half second.
public sealed class BraxChampionsObszarZDaleka : MonoBehaviour
{
    public string scena;
    public Renderer[] renderery = new Renderer[0];

    float nast; int stan = -1;

    void OnEnable() { SceneManager.sceneLoaded += Wczytano; SceneManager.sceneUnloaded += Zwolniono; Synchronizuj(); }
    void OnDisable() { SceneManager.sceneLoaded -= Wczytano; SceneManager.sceneUnloaded -= Zwolniono; }
    void Wczytano(Scene s, LoadSceneMode m) { if (s.name == scena) Synchronizuj(); }
    void Zwolniono(Scene s) { if (s.name == scena) Synchronizuj(); }

    void Update()
    {
        if (Time.unscaledTime < nast) return;
        nast = Time.unscaledTime + .5f;
        Synchronizuj();
    }

    void Synchronizuj()
    {
        if (string.IsNullOrEmpty(scena)) return;
        var s = SceneManager.GetSceneByName(scena);
        int nowy = s.IsValid() && s.isLoaded ? 1 : 0;
        if (nowy == stan) return;
        stan = nowy;
        foreach (var r in renderery) if (r) r.enabled = nowy == 0;
    }
}
