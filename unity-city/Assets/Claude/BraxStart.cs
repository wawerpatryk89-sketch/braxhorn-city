using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// EKRAN STARTOWY APK (1.11, Opus; porady Google/Unity: „najpierw lekka scena, ciężka doczytywana w tle”).
// Scena 0 w APK jest pusta (ta kamera + ten skrypt), więc ekran pojawia się od razu, zanim telefon zacznie wczytywać
// miasto / osiedle (scena 1).
// BUILD 14 (właściciel 21:45: „co ty zrobiłeś” — ekran TEST MIASTA z 8 przełącznikami nic nie dawał, bo wyłączanie działało
// dopiero po wczytaniu, a awaria była w trakcie): przełączniki USUNIĘTE (razem z BraxStartTest). Zamiast nich ładny ekran
// wczytywania (BraxEkranWczytywania): logo, nazwa rejonu, pasek postępu, wskazówka. Diagnostyka (dziennik poprzedniego
// startu, model telefonu, RAM, grafika) UKRYTA — pokazuje się po przytrzymaniu palca 3 s albo sama, gdy poprzedni start
// się nie udał (wtedy jak dotąd: czeka na dotyk, max 20 s — czas na zrzut ekranu). Dziennik startu (BraxStartLog) zostaje.
// Wczytywanie z allowSceneActivation = false: dane sceny wczytane do 90% → wpis „2b 90% dane wczytane” → dopiero wtedy
// włączenie sceny → „2- … pierwsze Awake” (BraxStartZnacznik). Wiadomo dokładnie, czy telefon pada przy wczytywaniu
// danych, czy przy włączaniu sceny (tekstury/siatki na GPU, Awake).
public sealed class BraxStart : MonoBehaviour
{
    public static bool Pokazuje { get; private set; }
    string status = "", podtytul = "", wskazowka = "", urzadzenie = "", diag = "";
    float postep, trzyma, nastDiag;
    bool problem, czekaNaDotyk, pokazDiag;
    // 3.11 (właściciel 14:56): ekran startowy PRZED grą — muzyka, START, USTAWIENIA, pełny ekran; scena wczytuje się w tle
    bool ekranStartowy, ustawienia; AudioSource muzyka; GUIStyle stPrz, stTyt;

    IEnumerator Start()
    {
        Pokazuje = true;
        Application.targetFrameRate = 30;
        status = BraxTeksty.T("start.przygotowanie");
        urzadzenie = SystemInfo.deviceModel + " · RAM " + SystemInfo.systemMemorySize + " MB · " + SystemInfo.graphicsDeviceType +
                     " · " + SystemInfo.graphicsDeviceName + " (" + SystemInfo.graphicsMemorySize + " MB) · " + SystemInfo.operatingSystem;
        wskazowka = BraxTeksty.Wskazowka(Random.Range(0, BraxTeksty.LiczbaWskazowek));
        problem = !string.IsNullOrEmpty(BraxStartLog.Poprzedni);
        BraxStartLog.Etap("1b ekran startowy");
        yield return BraxContent.Initialize();
        string nazwa = BraxContent.FirstScene;
        podtytul = nazwa.StartsWith("BraxhornEstate") ? BraxTeksty.T("start.osiedle") : nazwa.StartsWith("BraxhornCity") ? BraxTeksty.T("start.miasto") : "";
        Debug.Log("BRAXHORN_UI ekran wczytywania: scena " + nazwa + (problem ? " · poprzedni start NIEUDANY — diagnostyka widoczna, czeka na dotyk" : " · diagnostyka ukryta (przytrzymanie 3 s)") +
                  " · język " + BraxTeksty.Aktualny + " · " + urzadzenie);
        OdswiezDiag();
        if (problem)
        {
            pokazDiag = true; czekaNaDotyk = true; status = BraxTeksty.T("start.nieudany");
            float until = Time.realtimeSinceStartup + 20f;
            while (czekaNaDotyk && Time.realtimeSinceStartup < until) { if (Input.touchCount > 0 || Input.GetMouseButtonDown(0)) break; yield return null; }
            czekaNaDotyk = false;
        }
        else yield return new WaitForSecondsRealtime(.35f);
        if (!BraxContent.Available(nazwa)) { status = BraxTeksty.T("start.brak_sceny"); yield break; }
        status = podtytul.Length > 0 ? BraxTeksty.T("start.wczytywanie") : BraxTeksty.T("start.wczytywanie") + " · " + nazwa;   // nazwa rejonu jest już pod logo
        BraxStartLog.Etap("2a wczytywanie sceny " + nazwa);
        yield return Resources.UnloadUnusedAssets();
        var op = BraxContent.Begin(nazwa, LoadSceneMode.Single, false);
        if (op == null) { status = BraxTeksty.F("start.blad_sceny", nazwa); yield break; }
        // ekran startowy (APK): obraz na pełny ekran + muzyka; scena gry wczytuje się w tle (bez włączania), START ją włącza
        if (Application.platform != RuntimePlatform.WebGLPlayer && BraxEkranStartowy.Zaladuj())
        {
            ekranStartowy = true; MuzykaStart();
            BraxStartLog.Etap("1e ekran startowy (START / USTAWIENIA), scena wczytuje się w tle");
            Debug.Log("BRAXHORN_START ekran startowy przed grą: pokazany, muzyka=" + (muzyka && muzyka.clip ? muzyka.clip.name : "brak"));
            while (ekranStartowy)
            {
                if (op.Progress < .9f) postep = op.Progress / .9f * .92f;
                yield return null;
            }
            BraxEkranStartowy.PokazanyNaStarcie = true; BraxEkranStartowy.Zwolnij();
            BraxStartLog.Etap("1f START — dalej wczytywanie");
        }
        // postęp co 10% do dziennika (build 9 pokazał tylko „2a” — widać, przy ilu procentach i przy jakiej pamięci pada)
        int zapisane = -1;
        while (!op.Ready)
        {
            postep = op.Progress / .9f * .92f;
            int dz = Mathf.FloorToInt(op.Progress * 10f);
            if (dz > zapisane) { zapisane = dz; BraxStartLog.Etap("2a postęp " + dz * 10 + "%"); }
            yield return null;
        }
        if (op.Failed) { op.ReleaseFailure(); status = BraxTeksty.F("start.blad_sceny", nazwa); yield break; }
        postep = .95f;
        BraxStartLog.Etap("2b 90% dane wczytane: " + nazwa);
        status = BraxTeksty.T("start.uruchamianie");
        yield return null; yield return null;   // klatka z paskiem 95% i wpisem w dzienniku, zanim telefon zajmie się włączaniem
        postep = 1f;
        if (muzyka) { for (float v = muzyka.volume; v > .01f; v -= Time.unscaledDeltaTime * 1.6f) { muzyka.volume = v; yield return null; } muzyka.Stop(); }
        BraxStartLog.Etap("2c włączanie sceny " + nazwa + " (allowSceneActivation)");
        yield return op.Activate();
    }

    void MuzykaStart()
    {
        var clip = Resources.Load<AudioClip>("BraxStartMuzyka");
        if (!clip) return;
        if (!FindAnyObjectByType<AudioListener>()) gameObject.AddComponent<AudioListener>();   // scena 0 ma tylko czarną kamerę — bez słuchacza nie byłoby dźwięku
        muzyka = gameObject.AddComponent<AudioSource>();
        muzyka.clip = clip; muzyka.loop = true; muzyka.playOnAwake = false; muzyka.spatialBlend = 0f;
        muzyka.volume = BraxUstawienia.Muzyka; muzyka.Play();
    }

    void Update()
    {
        if (muzyka && ekranStartowy) muzyka.volume = BraxUstawienia.Muzyka;
        if (ekranStartowy)
        {
            if (ustawienia) return;
            int w = BraxEkranStartowy.Dotyk();
            if (w == 1) { ekranStartowy = false; Debug.Log("BRAXHORN_START wybrano: START"); }
            else if (w == 2) { ustawienia = true; Debug.Log("BRAXHORN_START wybrano: USTAWIENIA"); }
            return;
        }
        // przytrzymanie palca (albo myszy) 3 s: pokaż / ukryj diagnostykę
        if (pokazDiag && Time.unscaledTime > nastDiag) OdswiezDiag();
        bool dotyk = Input.touchCount > 0 || Input.GetMouseButton(0);
        if (!dotyk) { trzyma = 0f; return; }
        float przed = trzyma; trzyma += Time.unscaledDeltaTime;
        if (przed < 3f && trzyma >= 3f) { pokazDiag = !pokazDiag; OdswiezDiag(); BraxStartLog.Etap("1d diagnostyka " + (pokazDiag ? "pokazana" : "ukryta") + " (przytrzymanie 3 s)"); }
    }

    // Powód zamknięcia wg Androida (ApplicationExitInfo.getReason, Android 11+) — do wpięcia, gdy w projekcie wróci moduł
    // AndroidJNI (dziś wyłączony: włączenie = pełny reimport biblioteki w CI). Tabela gotowa, żeby nie pisać jej drugi raz.
    public static string PowodZamkniecia(int r)
    {
        switch (r)
        {
            case 1: return "ZAMKNIĘTA PRZEZ GRĘ";
            case 2: return "ZABITA SYGNAŁEM (często: brak pamięci)";
            case 3: return "BRAK PAMIĘCI — system zabił grę";
            case 4: return "AWARIA JAVA";
            case 5: return "AWARIA NATYWNA (silnik / sterownik grafiki)";
            case 6: return "ZAWIESZENIE (ANR)";
            case 7: return "BŁĄD STARTU";
            case 8: return "ZMIANA UPRAWNIEŃ";
            case 9: return "ZA DUŻE ZUŻYCIE ZASOBÓW";
            case 10: return "ZAMKNIĘTA PRZEZ CIEBIE";
            case 11: return "ZATRZYMANA PRZEZ CIEBIE";
            case 12: return "ZALEŻNOŚĆ PRZESTAŁA DZIAŁAĆ";
            case 13: return "INNY POWÓD";
            case 14: return "ZAMROŻONA";
            case 15: case 16: return "AKTUALIZACJA / ZMIANA PAKIETU";
            default: return "NIEZNANY POWÓD (" + r + ")";
        }
    }

    void OdswiezDiag()
    {
        nastDiag = Time.unscaledTime + 1f;
        diag = (string.IsNullOrEmpty(BraxStartLog.Poprzedni) ? BraxTeksty.T("start.poprzedni_ok") : BraxStartLog.Poprzedni) + "\n\n" +
               BraxTeksty.T("start.dziennik") + "\n" + BraxStartLog.Ostatnie(6) + "\n\n" + urzadzenie;
    }

    void OnDestroy() { Pokazuje = false; }

    // USTAWIENIA na ekranie startowym: muzyka, efekty, grafika, język, POWRÓT (zapis jak w menu gry)
    void PanelUstawien()
    {
        float H = Screen.height, W = Screen.width, bh = Mathf.Round(H * .1f), bw = Mathf.Min(W * .5f, H * 1.1f), gap = bh * .22f;
        if (stPrz == null)
        {
            stPrz = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(bh * .38f), fontStyle = FontStyle.Bold };
            stTyt = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(bh * .5f), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            stTyt.normal.textColor = Color.white;
        }
        bool en = BraxTeksty.Aktualny == BraxTeksty.Jezyk.EN;
        float panH = bh * (BraxContent.Enabled ? 7.8f : 6.6f), x = (W - bw) * .5f, y = (H - panH) * .5f;
        Color c0 = GUI.color; GUI.color = new Color(0f, 0f, 0f, .78f);
        GUI.DrawTexture(new Rect(x - bh * .4f, y - bh * .3f, bw + bh * .8f, panH + bh * .6f), Texture2D.whiteTexture); GUI.color = c0;
        GUI.Label(new Rect(x, y, bw, bh), en ? "SETTINGS" : "USTAWIENIA", stTyt); y += bh + gap;
        string wl = en ? "ON" : "WŁ.", wyl = en ? "OFF" : "WYŁ.";
        if (GUI.Button(new Rect(x, y, bw, bh), (en ? "MUSIC: " : "MUZYKA: ") + (BraxUstawienia.Muzyka > .01f ? wl : wyl), stPrz))
        { BraxUstawienia.Muzyka = BraxUstawienia.Muzyka > .01f ? 0f : .8f; BraxUstawienia.Zapisz(); }
        y += bh + gap;
        if (GUI.Button(new Rect(x, y, bw, bh), (en ? "SOUND FX: " : "EFEKTY: ") + (BraxUstawienia.Efekty > .01f ? wl : wyl), stPrz))
        { BraxUstawienia.Efekty = BraxUstawienia.Efekty > .01f ? 0f : 1f; BraxUstawienia.Zapisz(); }
        y += bh + gap;
        var g = BraxGrafika.Aktualny;
        string gn = g == BraxGrafika.Tryb.Niska ? (en ? "LOW" : "NISKA") : g == BraxGrafika.Tryb.Srednia ? (en ? "MEDIUM" : "ŚREDNIA") : (en ? "HIGH" : "WYSOKA");
        if (GUI.Button(new Rect(x, y, bw, bh), (en ? "GRAPHICS: " : "GRAFIKA: ") + gn, stPrz))
            BraxGrafika.Ustaw((BraxGrafika.Tryb)(((int)g + 1) % 3));
        y += bh + gap;
        if (GUI.Button(new Rect(x, y, bw, bh), (en ? "LANGUAGE: ENGLISH" : "JĘZYK: POLSKI"), stPrz))
            BraxTeksty.Ustaw(en ? BraxTeksty.Jezyk.PL : BraxTeksty.Jezyk.EN);
        y += bh + gap;
        if (BraxContent.Enabled)
        {
            GUI.enabled = !BraxContent.Importing;
            if (GUI.Button(new Rect(x, y, bw, bh), en ? "IMPORT UPDATE" : "WCZYTAJ AKTUALIZACJĘ", stPrz)) BraxContent.ChooseUpdate();
            GUI.enabled = true; y += bh + gap;
        }
        if (GUI.Button(new Rect(x, y, bw, bh), en ? "BACK" : "POWRÓT", stPrz)) { ustawienia = false; Debug.Log("BRAXHORN_START ustawienia: powrót"); }
    }

    void OnGUI()
    {
        GUI.depth = -20;
        if (ekranStartowy)
        {
            BraxEkranStartowy.RysujObraz(!ustawienia);
            if (ustawienia) PanelUstawien();
            if (!string.IsNullOrEmpty(BraxContent.Message)) GUI.Label(new Rect(16, Screen.height - 48, Screen.width - 32, 40), BraxContent.Message);
            return;
        }
        BraxEkranWczytywania.Rysuj(postep, status, podtytul, wskazowka, diag, pokazDiag, !problem);
    }
}
