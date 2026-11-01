using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// OBSZARY DOCZYTYWANE W TLE (właściciel 1.11, 01:04: „miasto podzielić na dwa obszary, tak samo osiedle… jak dojdę do
// granicy drugiej, to dopiero się wczytać… tylko bez blokady”). Komponent w scenie startowej rejonu (miasto / osiedle).
// Każdy obszar = osobna scena w paczce (BraxhornCity_Nabrzeze, BraxhornEstate_Poludnie) + jej prostopadłościan.
// Gdy gracz podejdzie bliżej niż „wczytaj” do granicy obszaru, scena wczytuje się w tle (LoadSceneAsync Additive, niski
// priorytet wątku — gra idzie dalej, bez czarnego ekranu); gdy odejdzie dalej niż „zwolnij”, scena jest zwalniana
// i pamięć oddawana (Resources.UnloadUnusedAssets). Jedna operacja naraz. Gracz nigdy nie zostaje w zwalnianej scenie.
public sealed class BraxObszary : MonoBehaviour
{
    [Serializable] public sealed class Obszar
    {
        public string scena;
        public Bounds granice;
        public float wczytaj = 45f, zwolnij = 110f;
        [NonSerialized] public bool wczytany, w_toku;
    }
    public Obszar[] obszary = Array.Empty<Obszar>();
    public static int Laduje { get; private set; }
    BraxGymPlayer player;
    float next;
    bool zajety;

    void Start()
    {
        Application.backgroundLoadingPriority = ThreadPriority.Low;
        foreach (var o in obszary) o.wczytany = SceneManager.GetSceneByName(o.scena).isLoaded;
    }

    bool zamrozony;

    void Update()
    {
        if (!player) player = FindAnyObjectByType<BraxGymPlayer>();
        if (!player) return;
        // Bezpiecznik: gracz przeniesiony (punkt podróży, zdjęcia) do środka obszaru, który jeszcze się wczytuje —
        // zatrzymany w miejscu, żeby nie spadł przez brak podłoża; puszczony zaraz po wczytaniu.
        bool wSrodku = false;
        foreach (var o in obszary)
            if (o != null && !o.wczytany && Application.CanStreamedLevelBeLoaded(o.scena) && o.granice.Contains(player.transform.position)) wSrodku = true;
        if (wSrodku && !zamrozony && !player.vessel && player.body && player.body.enabled) { player.body.enabled = false; zamrozony = true; next = 0; }
        if (!wSrodku && zamrozony) { if (player.body) player.body.enabled = true; zamrozony = false; }
        if (Time.unscaledTime < next || zajety) return;
        next = Time.unscaledTime + .25f;
        // kamera przeglądu (zdjęcia) albo gracz — bliżej granicy z nich dwóch
        Vector3 p = player.transform.position;
        var cam = Camera.main;
        foreach (var o in obszary)
        {
            if (o == null || string.IsNullOrEmpty(o.scena) || !Application.CanStreamedLevelBeLoaded(o.scena)) continue;
            float d = Mathf.Sqrt(o.granice.SqrDistance(p));
            if (cam) d = Mathf.Min(d, Mathf.Sqrt(o.granice.SqrDistance(cam.transform.position)));
            if (!o.wczytany && d < o.wczytaj) { StartCoroutine(Wczytaj(o)); return; }
            if (o.wczytany && d > o.zwolnij) { StartCoroutine(Zwolnij(o)); return; }
        }
    }

    IEnumerator Wczytaj(Obszar o)
    {
        zajety = true; o.w_toku = true; Laduje++;
        BraxStartLog.Etap("obszar: wczytywanie " + o.scena);
        var op = SceneManager.LoadSceneAsync(o.scena, LoadSceneMode.Additive);
        if (op != null) { op.priority = 0; yield return op; }
        o.wczytany = SceneManager.GetSceneByName(o.scena).isLoaded;
        BraxStartLog.Etap("obszar wczytany " + o.scena);
        Laduje--; o.w_toku = false; zajety = false;
    }

    IEnumerator Zwolnij(Obszar o)
    {
        var sc = SceneManager.GetSceneByName(o.scena);
        if (!sc.isLoaded) { o.wczytany = false; yield break; }
        zajety = true; Laduje++;
        // gracz (np. po zejściu z łodzi) nie może zostać w zwalnianej scenie
        if (player && player.gameObject.scene == sc && !player.transform.parent)
            SceneManager.MoveGameObjectToScene(player.gameObject, gameObject.scene);
        var op = SceneManager.UnloadSceneAsync(sc);
        if (op != null) yield return op;
        yield return Resources.UnloadUnusedAssets();
        o.wczytany = false;
        Laduje--; zajety = false;
    }

    // Przejście tunelem: wszystkie doczytane obszary idą razem ze sceną rejonu (BraxSceneTransit zwalnia wszystkie sceny).
    void OnDestroy() { if (zajety) Laduje = Mathf.Max(0, Laduje - 1); }
}
