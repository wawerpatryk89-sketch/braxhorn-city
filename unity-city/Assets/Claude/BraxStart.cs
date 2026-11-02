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
        string nazwa = SceneManager.sceneCountInBuildSettings >= 2 ? System.IO.Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(1)) : "";
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
        if (SceneManager.sceneCountInBuildSettings < 2) { status = BraxTeksty.T("start.brak_sceny"); yield break; }
        status = podtytul.Length > 0 ? BraxTeksty.T("start.wczytywanie") : BraxTeksty.T("start.wczytywanie") + " · " + nazwa;   // nazwa rejonu jest już pod logo
        BraxStartLog.Etap("2a wczytywanie sceny " + nazwa);
        yield return Resources.UnloadUnusedAssets();
        var op = SceneManager.LoadSceneAsync(1, LoadSceneMode.Single);
        if (op == null) { status = BraxTeksty.F("start.blad_sceny", nazwa); yield break; }
        op.allowSceneActivation = false;
        // postęp co 10% do dziennika (build 9 pokazał tylko „2a” — widać, przy ilu procentach i przy jakiej pamięci pada)
        int zapisane = -1;
        while (op.progress < .9f)
        {
            postep = op.progress / .9f * .92f;
            int dz = Mathf.FloorToInt(op.progress * 10f);
            if (dz > zapisane) { zapisane = dz; BraxStartLog.Etap("2a postęp " + dz * 10 + "%"); }
            yield return null;
        }
        postep = .95f;
        BraxStartLog.Etap("2b 90% dane wczytane: " + nazwa);
        status = BraxTeksty.T("start.uruchamianie");
        yield return null; yield return null;   // klatka z paskiem 95% i wpisem w dzienniku, zanim telefon zajmie się włączaniem
        postep = 1f;
        BraxStartLog.Etap("2c włączanie sceny " + nazwa + " (allowSceneActivation)");
        op.allowSceneActivation = true;
        while (!op.isDone) yield return null;
    }

    void Update()
    {
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

    void OnGUI()
    {
        GUI.depth = -20;
        BraxEkranWczytywania.Rysuj(postep, status, podtytul, wskazowka, diag, pokazDiag, !problem);
    }
}
