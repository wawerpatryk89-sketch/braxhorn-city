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
// Build 14 (właściciel 2.11: park i osiedle B pojawiały się kilkanaście metrów przed postacią): obszar z progiem
// „wczytaj” ≥ OdStartu wczytuje się OD RAZU po starcie sceny rejonu (w tle, bez czekania na podejście gracza), a z progiem
// „zwolnij” ≥ Nigdy nie jest nigdy zwalniany. Osiedle: BraxhornEstate_Poludnie (≈71 MB) — od startu i na stałe.
// Miasto (nabrzeże) bez zmian: progi metrowe jak dotąd.
public sealed class BraxObszary : MonoBehaviour
{
    public const float OdStartu = 1000000f, Nigdy = 10000000f;
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
        if (BraxStartTest.Wylaczony("obszary")) return;
        // obszary „od startu”: wczytywanie zaczyna się w pierwszej klatce sceny, jeden po drugim (bez czekania na gracza)
        StartCoroutine(OdStartuWszystkie());
    }

    IEnumerator OdStartuWszystkie()
    {
        foreach (var o in obszary)
        {
            if (o == null || o.wczytaj < OdStartu || o.wczytany || string.IsNullOrEmpty(o.scena)) continue;
            if (!Application.CanStreamedLevelBeLoaded(o.scena)) { Debug.Log("BRAXHORN_OBSZAR od startu: brak sceny " + o.scena + " w paczce"); continue; }
            while (zajety) yield return null;
            if (o.wczytany) continue;
            float t0 = Time.realtimeSinceStartup;
            Debug.Log("BRAXHORN_OBSZAR od startu: " + o.scena + " — wczytywanie w tle (zwalnianie: " + (o.zwolnij >= Nigdy ? "nigdy" : o.zwolnij + " m") + ")");
            // na czas pierwszego wczytania więcej czasu klatki na włączanie obiektów (10 ms zamiast 2 ms) — park ma być gotowy,
            // zanim gracz dojdzie od tunelu do bramy parku (≈190 m); potem z powrotem niski priorytet
            Application.backgroundLoadingPriority = ThreadPriority.Normal;
            yield return StartCoroutine(Wczytaj(o));
            Application.backgroundLoadingPriority = ThreadPriority.Low;
            Debug.Log("BRAXHORN_OBSZAR od startu: " + o.scena + (o.wczytany ? " wczytany po " + (Time.realtimeSinceStartup - t0).ToString("0.0") + " s" : " — NIE wczytany"));
        }
    }

    bool zamrozony;

    void Update()
    {
        if (BraxStartTest.Wylaczony("obszary")) return;   // test miasta (ekran startowy): bez doczytywania nabrzeża i wyspy
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
            if (o.wczytany && o.zwolnij < Nigdy && d > o.zwolnij) { StartCoroutine(Zwolnij(o)); return; }
        }
    }

    IEnumerator Wczytaj(Obszar o)
    {
        double startWyspy = Time.realtimeSinceStartupAsDouble;
        zajety = true; o.w_toku = true; Laduje++;
        BraxStartLog.Etap("obszar: wczytywanie " + o.scena);
        var op = SceneManager.LoadSceneAsync(o.scena, LoadSceneMode.Additive);
        if (op != null) { op.priority = 0; yield return op; }
        o.wczytany = SceneManager.GetSceneByName(o.scena).isLoaded;
        BraxStartLog.Etap("obszar wczytany " + o.scena);
        if (o.scena == "BraxhornCity_Wyspa") Debug.Log("BRAXHORN_WYSPA_STREAM load_seconds=" +
            (Time.realtimeSinceStartupAsDouble - startWyspy).ToString("F3") + " loaded=" + o.wczytany +
            " allocated_bytes=" + UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong());
        Laduje--; o.w_toku = false; zajety = false;
    }

    IEnumerator Zwolnij(Obszar o)
    {
        double startWyspy = Time.realtimeSinceStartupAsDouble;
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
        if (o.scena == "BraxhornCity_Wyspa") Debug.Log("BRAXHORN_WYSPA_STREAM unload_seconds=" +
            (Time.realtimeSinceStartupAsDouble - startWyspy).ToString("F3") +
            " allocated_bytes=" + UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong());
        Laduje--; zajety = false;
    }

    // Przejście tunelem: wszystkie doczytane obszary idą razem ze sceną rejonu (BraxSceneTransit zwalnia wszystkie sceny).
    void OnDestroy() { if (zajety) Laduje = Mathf.Max(0, Laduje - 1); }
}

