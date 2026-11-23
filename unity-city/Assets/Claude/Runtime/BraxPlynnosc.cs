using System.Globalization;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;

// BRAXHORN — PŁYNNOŚĆ: JEDEN STANDARD W KAŻDYM REJONIE (ZASADY, rozdział 10).
//
// Właściciel 23.11.2026, 01:13: „Mamy ustalić jeden konkretny zespół konkretnych zasad. […]
// żeby ta gra […] nie zamulała. Minimum 30 FPS, minimum.”
//
// Do 23.11 zasady mówiły, jak gra ma WYGLĄDAĆ, i nie miały ani jednej liczby o tym, jak ma
// CHODZIĆ. Limit 0,8 mln trójkątów w kadrze był w dokumencie z 1.11 — i żaden build go nie
// mierzył; w pomiarach wychodziło 4,5 mln (park Osiedla) i 8,3 mln (galeria w Mistrzach).
//
// Ten plik robi TRZY rzeczy i żadnej więcej:
//
//  1. TRZYMA LICZBY (stałe niżej). Rejon bierze je stąd, nie wpisuje własnych.
//
//  2. MIERZY I POKAZUJE. Licznik na górze ekranu: klatki na sekundę, najgorsza klatka
//     z ostatniej sekundy, trójkąty i wywołania rysowania w kadrze. Zielony = w normie,
//     czerwony = poniżej 30 klatek albo ponad budżet kadru. Cztery palce przytrzymane na
//     ekranie przez sekundę chowają go i przywracają (sekunda — żeby nie przełączał się sam
//     w trakcie gry, gdy na ekranie są akurat oba kciuki i dwa przyciski). Do logu idzie
//     wiersz BRAX_PLYNNOSC: co 30 s zwykły, a przy przekroczeniu od razu (najwyżej co 10 s),
//     z pozycją kamery — żeby wykonawca wiedział GDZIE jest ciężko, a nie tylko że jest.
//
//  3. USTAWIA ZASIĘG DROBIAZGÓW na kamerze: warstwa 20 (obiekty poniżej 4 m) rysowana do 260
//     jednostek, warstwa 21 (poniżej 1,2 m) do 90. To jest metoda rejonu Mistrzów
//     (BraxChampionsWydajnosc) — te same warstwy i te same liczby, więc
//     oba pliki się nie gryzą. Samo ustawienie zasięgu NIC NIE ZMIENIA, dopóki rejon nie
//     posortuje obiektów na te warstwy przy buildzie (BraxZasiegSortowanie.Posortuj).
//
// CZEGO TEN PLIK NIE ROBI — celowo:
//  • nie dotyka QualitySettings (ZASADY 4.5: robią to trzy pliki standardu obrazu i żaden inny);
//  • nie zmienia rozdzielczości ani MSAA w trakcie gry — to odtwarza bufor ekranu i na Vulkanie
//    zamykało aplikację (ZASADY 2.4);
//  • nie obniża sam jakości. Bezpiecznik z ZASAD 10.7 powstanie po pierwszym pomiarze na
//    telefonie; pisany na ślepo robiłby migające drobiazgi zamiast płynności.
//
// Liczniki renderowania czytane są przez ProfilerRecorder. Dokumentacja Unity („Rendering
// Profiler module reference”) podaje „Triangles Count”, „Draw Calls Count”, „SetPass Calls
// Count” i „Batches Count” jako dostępne w buildach wydaniowych. Gdyby któregoś nie było,
// licznik pokaże kreskę zamiast liczby — nie zero, które wyglądałoby jak dobry wynik.
public sealed class BraxPlynnosc : MonoBehaviour
{
    // ---- LICZBY STANDARDU (ZASADY 10) ------------------------------------------------------
    /// <summary>10.1 — podłoga płynności na telefonie odniesienia (moto g86, tryb ŚREDNI).</summary>
    public const int KlatkiMin = 30;

    /// <summary>10.2 — najwyżej tyle trójkątów w jednej klatce, razem z przebiegami cieni.</summary>
    public const int BudzetTrojkatow = 800000;

    /// <summary>10.2 — najwyżej tyle wywołań rysowania w jednej klatce.</summary>
    public const int BudzetWywolan = 400;

    /// <summary>10.4 — warstwy zasięgu: MAŁE (obiekty poniżej 4 m) i DROBNE (poniżej 1,2 m).</summary>
    public const int WarstwaMale = 20, WarstwaDrobne = 21;

    /// <summary>10.4 — do ilu jednostek gry kamera rysuje warstwę MAŁE i DROBNE (kula wokół kamery).</summary>
    public const float ZasiegMale = 260f, ZasiegDrobne = 90f;

    /// <summary>10.4 — progi wielkości w jednostkach gry: 4 m i 1,2 m razy mnożnik świata 1,294.</summary>
    public const float RozmiarMale = 5.18f, RozmiarDrobne = 1.55f;

    /// <summary>Warstwa zasięgu dla obiektu o największym wymiarze <paramref name="wymiar"/> (jednostki gry).
    /// Zwraca -1, gdy obiekt jest duży i zostaje na swojej warstwie (rysowany do pełnego zasięgu).</summary>
    public static int WarstwaDlaRozmiaru(float wymiar)
    {
        if (wymiar <= 0f) return -1;
        if (wymiar < RozmiarDrobne) return WarstwaDrobne;
        if (wymiar < RozmiarMale) return WarstwaMale;
        return -1;
    }

    /// <summary>Przebieg zdjęć (ZASADY 8.8): zdjęcia są bez HUD i bez licznika. Przebieg ustawia true.</summary>
    public static bool Zdjecia;

    // ---- STAN ------------------------------------------------------------------------------
    const string KluczLicznika = "brax_licznik_plynnosci";
    const float OkresLogu = 30f, OdstepAlarmu = 10f, OkresKamer = .5f, PrzytrzymanieLicznika = 1f;

    static BraxPlynnosc inst;
    static readonly CultureInfo Liczby = CultureInfo.InvariantCulture;

    ProfilerRecorder recTrojkaty, recWywolania, recPrzejscia, recPaczki;
    bool recordery;

    bool pokaz, przelaczono; float czteryOd = -1f;
    float oknoCzas, oknoNajdluzsza; int oknoKlatki;            // bieżąca sekunda
    float logCzas, logNajgorszaSekunda; int logKlatki;         // od ostatniego wiersza logu
    long logTrojkatyMaks, logWywolaniaMaks;
    float nastepnyLog, nastepneKamery, ostatniAlarm = -99f;
    int kameryPoScenie;
    string napis = "";
    bool ponad;
    GUIStyle styl; int stylRozmiar;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        if (inst) return;
        var g = new GameObject("BRAXHORN plynnosc (jeden standard)");
        DontDestroyOnLoad(g);
        inst = g.AddComponent<BraxPlynnosc>();
    }

    void Awake()
    {
        useGUILayout = false;                                  // sam napis, bez przebiegu układu
        // Na telefonie licznik jest domyślnie WIDOCZNY (właściciel ma go widzieć bez szukania),
        // na komputerze i w przeglądarce ukryty — tam robi się zdjęcia do przeglądu.
        pokaz = PlayerPrefs.GetInt(KluczLicznika, Application.isMobilePlatform ? 1 : 0) == 1;
        SceneManager.sceneLoaded += PoScenie;
        kameryPoScenie = 3;
        ZerujLog();
    }

    void OnEnable()
    {
        if (recordery) return;
        recTrojkaty = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
        recWywolania = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
        recPrzejscia = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
        recPaczki = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
        recordery = true;
    }

    void OnDisable()
    {
        if (!recordery) return;
        recTrojkaty.Dispose(); recWywolania.Dispose(); recPrzejscia.Dispose(); recPaczki.Dispose();
        recordery = false;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= PoScenie;
        if (inst == this) inst = null;
    }

    // Trzy klatki po wczytaniu sceny, tak jak BraxObrazGlobalny i BraxGrafika: skrypty rejonu
    // ustawiają kamerę w Start/Awake, więc jednorazowe ustawienie dałoby się nadpisać.
    void PoScenie(Scene s, LoadSceneMode m)
    {
        kameryPoScenie = 3;
        ZerujSekunde();                                        // szarpnięcie wczytania to nie spadek klatek w grze
    }

    // Powrót z tła: jedna klatka trwa tyle, ile aplikacja była uśpiona — to nie jest pomiar.
    void OnApplicationPause(bool pauza) { ZerujSekunde(); }

    void ZerujSekunde() { oknoCzas = 0f; oknoKlatki = 0; oknoNajdluzsza = 0f; }

    void ZerujLog()
    {
        logCzas = 0f; logKlatki = 0; logNajgorszaSekunda = float.MaxValue;
        logTrojkatyMaks = -1; logWywolaniaMaks = -1;
        nastepnyLog = Time.unscaledTime + OkresLogu;
    }

    static long Wartosc(ProfilerRecorder r) { return r.Valid ? r.LastValue : -1; }

    void Update()
    {
        // przełącznik licznika: cztery palce przytrzymane przez sekundę (zapamiętany między uruchomieniami)
        if (Input.touchCount >= 4)
        {
            if (czteryOd < 0f) czteryOd = Time.unscaledTime;
            else if (!przelaczono && Time.unscaledTime - czteryOd >= PrzytrzymanieLicznika)
            {
                przelaczono = true;
                pokaz = !pokaz;
                PlayerPrefs.SetInt(KluczLicznika, pokaz ? 1 : 0);
            }
        }
        else { czteryOd = -1f; przelaczono = false; }

        float dt = Time.unscaledDeltaTime;
        if (dt <= 0f) return;
        oknoCzas += dt; oknoKlatki++;
        if (dt > oknoNajdluzsza) oknoNajdluzsza = dt;
        if (oknoCzas < 1f) return;

        // ---- pełna sekunda: jedna liczba na ekran, jedna do logu --------------------------
        float klatki = oknoKlatki / oknoCzas;
        float najgorsza = 1f / oknoNajdluzsza;
        long trojkaty = recordery ? Wartosc(recTrojkaty) : -1;
        long wywolania = recordery ? Wartosc(recWywolania) : -1;

        logCzas += oknoCzas; logKlatki += oknoKlatki;
        if (klatki < logNajgorszaSekunda) logNajgorszaSekunda = klatki;
        if (trojkaty > logTrojkatyMaks) logTrojkatyMaks = trojkaty;
        if (wywolania > logWywolaniaMaks) logWywolaniaMaks = wywolania;
        ZerujSekunde();

        // Limit klatek 30 daje odczyt 29,5–30,5, więc „poniżej 30” liczymy od 29.
        bool zaMaloKlatek = klatki < KlatkiMin - 1f;
        bool zaDuzoTrojkatow = trojkaty > BudzetTrojkatow;
        bool zaDuzoWywolan = wywolania > BudzetWywolan;
        ponad = zaMaloKlatek || zaDuzoTrojkatow || zaDuzoWywolan;

        napis = klatki.ToString("0", Liczby) + " kl/s  (najgorsza " + najgorsza.ToString("0", Liczby) + ")   " +
                (trojkaty < 0 ? "—" : (trojkaty / 1000).ToString(Liczby)) + " tys. trójk.   " +
                (wywolania < 0 ? "—" : wywolania.ToString(Liczby)) + " wyw.";

        float teraz = Time.unscaledTime;
        if (ponad && teraz - ostatniAlarm >= OdstepAlarmu)
        {
            ostatniAlarm = teraz;
            Wypisz("PONAD", klatki, klatki, trojkaty, wywolania,
                   (zaMaloKlatek ? "klatki " : "") + (zaDuzoTrojkatow ? "trojkaty " : "") + (zaDuzoWywolan ? "wywolania" : ""));
        }
        if (teraz >= nastepnyLog)
        {
            if (logCzas > 0f)
                Wypisz("OKRES", logKlatki / logCzas, logNajgorszaSekunda, logTrojkatyMaks, logWywolaniaMaks, "");
            ZerujLog();
        }
    }

    void Wypisz(string rodzaj, float srednia, float najgorszaSekunda, long trojkaty, long wywolania, string powod)
    {
        var cam = Camera.main;
        var p = cam ? cam.transform.position : Vector3.zero;
        Debug.Log("BRAX_PLYNNOSC " + rodzaj +
                  " scena=" + SceneManager.GetActiveScene().name +
                  " tryb=" + QualitySettings.names[QualitySettings.GetQualityLevel()] +
                  " limit_klatek=" + Application.targetFrameRate.ToString(Liczby) +
                  " kl_sr=" + srednia.ToString("0.0", Liczby) +
                  " kl_najgorsza_sekunda=" + najgorszaSekunda.ToString("0.0", Liczby) +
                  " trojkaty_maks=" + trojkaty.ToString(Liczby) + "/" + BudzetTrojkatow.ToString(Liczby) +
                  " wywolania_maks=" + wywolania.ToString(Liczby) + "/" + BudzetWywolan.ToString(Liczby) +
                  " przejscia=" + (recordery ? Wartosc(recPrzejscia) : -1).ToString(Liczby) +
                  " paczki=" + (recordery ? Wartosc(recPaczki) : -1).ToString(Liczby) +
                  " zasieg=" + (cam ? cam.farClipPlane : 0f).ToString("0", Liczby) +
                  " kamera=(" + p.x.ToString("0", Liczby) + "," + p.y.ToString("0", Liczby) + "," + p.z.ToString("0", Liczby) + ")" +
                  (powod.Length > 0 ? " powod=" + powod.Trim().Replace(' ', '+') : ""));
    }

    void LateUpdate()
    {
        if (kameryPoScenie > 0)
        {
            kameryPoScenie--;
            foreach (var c in FindObjectsByType<Camera>(FindObjectsSortMode.None)) UstawZasieg(c);
            return;
        }
        // Tryby jakości i skrypty rejonu potrafią podmienić kamerę w trakcie gry — dwa razy na
        // sekundę wystarcza, a odczyt layerCullDistances co klatkę to 32 liczby śmieci na klatkę.
        if (Time.unscaledTime < nastepneKamery) return;
        nastepneKamery = Time.unscaledTime + OkresKamer;
        UstawZasieg(Camera.main);
    }

    // Tylko kamery rysujące na ekran. Kamery do tekstury (lustra, sześciany odbić) zostają
    // rejonowi — lustro, które przestaje pokazywać drobiazgi, to decyzja o wyglądzie.
    // Zasięgu NIE przycinamy do farClipPlane: zero znaczy „do końca”, a wartość większa niż
    // daleka płaszczyzna i tak nic nie dorysuje. Dzięki temu liczby są identyczne z tymi
    // z BraxChampionsWydajnosc i oba pliki nie przestawiają sobie kamery nawzajem.
    static void UstawZasieg(Camera c)
    {
        if (!c || c.targetTexture != null) return;
        var d = c.layerCullDistances;
        if (d[WarstwaMale] == ZasiegMale && d[WarstwaDrobne] == ZasiegDrobne && c.layerCullSpherical) return;
        d[WarstwaMale] = ZasiegMale; d[WarstwaDrobne] = ZasiegDrobne;
        c.layerCullDistances = d;
        c.layerCullSpherical = true;
    }

    void OnGUI()
    {
        if (!pokaz || Zdjecia || napis.Length == 0 || Event.current.type != EventType.Repaint) return;

        int rozmiar = Mathf.Max(12, Screen.height / 42);
        if (styl == null || stylRozmiar != rozmiar)
        {
            styl = new GUIStyle(GUI.skin.label);
            styl.fontSize = rozmiar;
            styl.fontStyle = FontStyle.Bold;
            styl.alignment = TextAnchor.MiddleCenter;
            styl.normal.textColor = Color.white;
            stylRozmiar = rozmiar;
        }

        // Obszar bezpieczny (wcięcie aparatu): safeArea liczy y od dołu, IMGUI od góry.
        var sa = Screen.safeArea;
        float w = rozmiar * 26f, h = rozmiar * 1.6f;
        var r = new Rect(sa.x + (sa.width - w) * .5f, Screen.height - (sa.y + sa.height) + 2f, w, h);

        var kolorByl = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, .55f);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = ponad ? new Color(1f, .38f, .32f) : new Color(.55f, 1f, .62f);
        GUI.Label(r, napis, styl);
        GUI.color = kolorByl;
    }
}
