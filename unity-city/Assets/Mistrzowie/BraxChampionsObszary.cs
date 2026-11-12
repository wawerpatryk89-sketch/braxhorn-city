using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// THE REGION IN PARTS (owner 12.11.2026 20:42-20:58: "długo się ładuje, wywala ... miasto R1 jest wielkie, a się ładuje
// szybko ... masz zobaczyć, jak miasto R1 jest zrobione, i zrobić tak samo ... jedna ta sama gra, tylko że podzielona").
// APK 39 kept the whole region in one scene: 83 635 objects, 2 GB of content, 1.8 GB of memory - minutes of loading, and
// Android killed the game. As in the city R1 (BraxObszary), every big building is its own scene now
// (BraxChampionsPodzial at build time): the main scene holds the ground, the streets, the waterfront and the player; a
// building's scene is read in the background when the player comes near and let go when he is far. Until it is read, its
// far copy (BraxChampionsObszarZDaleka, one simplified mesh) stands in its place, so nothing pops out of the air.
//   - one operation at a time, the nearest area first; the place the player is heading to counts too (2.5 s ahead, so a
//     car at speed does not arrive before the walls);
//   - at the start the areas around the player are read behind a black card, then the game is shown;
//   - a player inside an area that is not read yet stands still until it is (no falling through a floor that is coming);
//   - a scene that fails to load is tried no more: the far copy stays, the player is never left frozen;
//   - scenes are loaded through BraxContent (the APK's content bundles; plain SceneManager in a player without them).
public sealed class BraxChampionsObszary : MonoBehaviour
{
    [Serializable]
    public sealed class Obszar
    {
        public string scena;
        public Bounds granice;
        public float wczytaj = 150f, zwolnij = 280f;
        [NonSerialized] public bool wczytany, blad;
        [NonSerialized] public float odleglosc;
    }

    public Obszar[] obszary = Array.Empty<Obszar>();
    /// <summary>areas being read or let go right now (0 = the streamer is idle)</summary>
    public static int Laduje { get; private set; }
    /// <summary>true once the areas around the start point are in (the black card is off)</summary>
    public static bool Gotowe { get; private set; }
    /// <summary>the review player (photos in CI): every area is read and none is let go, so each scene is proven to load</summary>
    public static bool Wszystko;
    public static int Wczytane { get; private set; }
    public static int Bledy { get; private set; }
    public static int Razem { get; private set; }
    /// <summary>nothing left to read (review mode: every area is in or has failed)</summary>
    public static bool Spokoj { get; private set; }

    // at the start only what the player stands in or right beside (40 m) is read behind the black card; the rest of the
    // 150 m comes in the background with its far copy standing meanwhile, so the game is shown as soon as it can be
    const float Wyprzedzenie = 2.5f, MaxWyprzedzenie = 160f, CoIleSprzatanie = 15f, StartPromien = 40f;
    BraxGymPlayer gracz; Vector3 poprzednia, predkosc; bool maPoprzednia;
    bool zajety, zamrozony, start = true, sprzatac;
    int startRazem, startGotowe; float nastepne, ostatnieSprzatanie = -100f, szukaj;
    GUIStyle styl;

    void Awake() { Gotowe = false; Laduje = 0; Wczytane = 0; Bledy = 0; Spokoj = false; Razem = obszary.Length; }

    void Start()
    {
        foreach (var o in obszary)
            if (o != null && !string.IsNullOrEmpty(o.scena)) o.wczytany = SceneManager.GetSceneByName(o.scena).isLoaded;
        Application.backgroundLoadingPriority = ThreadPriority.High;      // the start: as fast as the phone reads
        Debug.Log("A1_OBSZARY areas=" + obszary.Length);
    }

    bool Dostepny(Obszar o)
    {
        if (o == null || o.blad || string.IsNullOrEmpty(o.scena)) return false;
        try { return BraxContent.Available(o.scena); } catch (Exception) { return false; }
    }

    Vector3 Pozycja() => gracz ? gracz.transform.position : (Camera.main ? Camera.main.transform.position : transform.position);

    void Update()
    {
        if (!gracz && Time.unscaledTime >= szukaj) { szukaj = Time.unscaledTime + .5f; gracz = FindAnyObjectByType<BraxGymPlayer>(); }
        Vector3 p = Pozycja();
        if (maPoprzednia && Time.unscaledDeltaTime > 1e-4f)
        {
            var v = (p - poprzednia) / Time.unscaledDeltaTime;
            if (v.sqrMagnitude > 400f * 400f) v = Vector3.zero;             // a teleport, not a speed
            predkosc = Vector3.Lerp(predkosc, v, .2f);
        }
        poprzednia = p; maPoprzednia = true;

        // inside an area that is still coming: stand still (released the moment it is in, or if it cannot come)
        bool wSrodku = false;
        foreach (var o in obszary)
            if (o != null && !o.wczytany && Dostepny(o) && o.granice.Contains(p)) { wSrodku = true; break; }
        if (gracz && gracz.body)
        {
            if ((wSrodku || start) && !zamrozony && !gracz.vessel && gracz.body.enabled) { gracz.body.enabled = false; zamrozony = true; nastepne = 0f; }
            else if (!wSrodku && !start && zamrozony) { gracz.body.enabled = true; zamrozony = false; }
        }

        if (zajety || Time.unscaledTime < nastepne) return;
        nastepne = Time.unscaledTime + (start ? 0f : .25f);

        // distances: the player, where he will be in 2.5 s, and the camera (a review camera far from the player counts)
        var naprzod = predkosc * Wyprzedzenie; if (naprzod.sqrMagnitude > MaxWyprzedzenie * MaxWyprzedzenie) naprzod = naprzod.normalized * MaxWyprzedzenie;
        var cam = Camera.main;
        Obszar doWczytania = null, doZwolnienia = null;
        foreach (var o in obszary)
        {
            if (o == null || string.IsNullOrEmpty(o.scena)) continue;
            float d2 = Mathf.Min(o.granice.SqrDistance(p), o.granice.SqrDistance(p + naprzod));
            if (cam) d2 = Mathf.Min(d2, o.granice.SqrDistance(cam.transform.position));
            o.odleglosc = Wszystko ? 0f : Mathf.Sqrt(d2);
            float zasieg = start ? Mathf.Min(o.wczytaj, StartPromien) : o.wczytaj;
            if (!o.wczytany && o.odleglosc < zasieg && Dostepny(o) && (doWczytania == null || o.odleglosc < doWczytania.odleglosc)) doWczytania = o;
            if (o.wczytany && o.odleglosc > o.zwolnij && (doZwolnienia == null || o.odleglosc > doZwolnienia.odleglosc)) doZwolnienia = o;
        }
        Spokoj = doWczytania == null;
        if (start)
        {
            if (startRazem == 0) foreach (var o in obszary) if (o != null && !o.wczytany && o.odleglosc < Mathf.Min(o.wczytaj, StartPromien) && Dostepny(o)) startRazem++;
            if (doWczytania == null)
            {
                start = false; Gotowe = true;
                Application.backgroundLoadingPriority = ThreadPriority.BelowNormal;   // from now on: in the background, 4 ms a frame
                Debug.Log("A1_OBSZARY start areas in=" + startGotowe + "/" + startRazem + " t=" + Time.realtimeSinceStartup.ToString("F1") + " s");
                return;
            }
        }
        if (doWczytania != null) { StartCoroutine(Wczytaj(doWczytania)); return; }
        if (doZwolnienia != null) { StartCoroutine(Zwolnij(doZwolnienia)); return; }
        if (sprzatac && Time.unscaledTime - ostatnieSprzatanie > CoIleSprzatanie) StartCoroutine(Sprzataj());
    }

    // A failed request never leaves the streamer locked or the player frozen, and is not repeated.
    IEnumerator Wczytaj(Obszar o)
    {
        zajety = true; Laduje++;
        try
        {
            BraxContent.SceneLoad op = null;
            try { op = BraxContent.Begin(o.scena, LoadSceneMode.Additive); }
            catch (Exception e) { Debug.LogError("A1_OBSZAR_LOAD_FAILED " + o.scena + ": " + e.Message); }
            if (op == null || op.Failed) { o.blad = true; Bledy++; if (op != null) op.ReleaseFailure(); yield break; }
            float limit = Time.realtimeSinceStartup + 180f;
            while (!op.Ready && Time.realtimeSinceStartup < limit) yield return null;
            // the scene activates by itself; wait until it is really in
            while (!op.Failed && !SceneManager.GetSceneByName(o.scena).isLoaded && Time.realtimeSinceStartup < limit) yield return null;
            o.wczytany = SceneManager.GetSceneByName(o.scena).isLoaded;
            if (!o.wczytany) { o.blad = true; Bledy++; op.ReleaseFailure(); Debug.LogError("A1_OBSZAR_LOAD_FAILED " + o.scena + ": not loaded"); }
            else { Wczytane++; if (start) startGotowe++; Debug.Log("A1_OBSZAR in " + o.scena + " d=" + o.odleglosc.ToString("F0")); }
        }
        finally { Laduje = Mathf.Max(0, Laduje - 1); zajety = false; }
    }

    IEnumerator Zwolnij(Obszar o)
    {
        var sc = SceneManager.GetSceneByName(o.scena);
        if (!sc.IsValid() || !sc.isLoaded) { o.wczytany = false; yield break; }
        zajety = true; Laduje++;
        try
        {
            // the player never goes with a scene that is let go
            if (gracz && gracz.gameObject.scene == sc && !gracz.transform.parent) SceneManager.MoveGameObjectToScene(gracz.gameObject, gameObject.scene);
            yield return BraxContent.Unload(sc);
            o.wczytany = SceneManager.GetSceneByName(o.scena).isLoaded;
            if (!o.wczytany) { Wczytane = Mathf.Max(0, Wczytane - 1); sprzatac = true; Debug.Log("A1_OBSZAR out " + o.scena + " d=" + o.odleglosc.ToString("F0")); }
            else { o.zwolnij = float.MaxValue; Debug.LogError("A1_OBSZAR_UNLOAD_FAILED " + o.scena); }   // stays in, not tried again
        }
        finally { Laduje = Mathf.Max(0, Laduje - 1); zajety = false; }
    }

    // meshes and textures of the scenes let go leave the memory (not after every scene: the sweep itself costs a moment)
    IEnumerator Sprzataj()
    {
        zajety = true; sprzatac = false; ostatnieSprzatanie = Time.unscaledTime;
        try { yield return Resources.UnloadUnusedAssets(); }
        finally { zajety = false; }
    }

    void OnGUI()
    {
        if (!start) return;
        GUI.depth = -2000;
        var c = GUI.color; GUI.color = Color.black;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture); GUI.color = c;
        if (styl == null) { styl = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(Screen.height * .045f), fontStyle = FontStyle.Bold }; styl.normal.textColor = Color.white; }
        GUI.Label(new Rect(0, Screen.height * .42f, Screen.width, Screen.height * .16f),
            "MIASTO MISTRZÓW\nwczytywanie okolicy " + Mathf.Min(startGotowe + 1, Mathf.Max(1, startRazem)) + " / " + Mathf.Max(1, startRazem), styl);
    }

    void OnDestroy()
    {
        if (zamrozony && gracz && gracz.body) gracz.body.enabled = true;
        Laduje = 0;
    }
}
