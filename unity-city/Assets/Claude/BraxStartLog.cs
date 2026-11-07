using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

// DZIENNIK STARTU NA TELEFONIE (31.10, Opus; przebudowa 1.11 po buildzie 8). Właściciel: „wypierdala mnie po instalacji
// przy włączaniu” — bez kabla i logcatu nie wiemy, w którym miejscu. Gra zapisuje etapy startu; jeśli poprzednie
// uruchomienie nie doszło do końca, przy następnym starcie ekran startowy (BraxStart) pokazuje ostatnie etapy.
// Build 8: po wywaleniu miasta ekran NIE pokazał poprzedniego etapu. Dwie przyczyny naprawione tutaj:
//  1) „OK” zapisywało się 10 s po pierwszej klatce sceny STARTOWEJ, także w trakcie wczytywania miasta w tle
//     (scena startowa jest aktywna, dopóki miasto się nie włączy) — wywalenie przy włączaniu miasta zostawiało „OK”;
//  2) PlayerPrefs na Androidzie zapisuje się asynchronicznie — przy zabiciu procesu ostatni wpis mógł nie dojść.
// Teraz: osobny plik w pamięci aplikacji, dopisywany wierszami i od razu zrzucany na dysk (Flush(true) = fsync),
// z godziną, etapem i pamięcią (zarezerwowana / używana / sterownik grafiki). „OK” tylko 10 s po pierwszej klatce
// sceny gry (nie startowej) i tylko gdy nic się nie wczytuje. Do tego sygnał systemu „mało pamięci”
// (Application.lowMemory = onTrimMemory Androida) i znaczniki włączania sceny oraz pierwszego renderu kamery.
public sealed class BraxStartLog : MonoBehaviour
{
    const string KEtap = "braxhorn_start_etap", KPamiec = "braxhorn_start_pamiec";
    // An isolated camp APK uses a different boot scene from the city.
    // Neither loading-screen camera is evidence of the GAME'S first render.
    static bool IsLoadingScene(string sceneName) =>
        sceneName == "BraxhornStart" || sceneName == "BraxhornObozStart";
    static BraxStartLog inst;
    static string poprzedni, sciezka;
    static bool androidExitChecked;
    static readonly System.Collections.Generic.List<string> wiersze = new System.Collections.Generic.List<string>();
    static int maxMb;
    static bool render0, render1;
    public static string Poprzedni
    {
        get
        {
            // Query after Unity's Android Activity exists, only on a failed start.
            // This reports an OS reason; it does not claim to identify a native
            // asset or a specific faulty scene object without Android logcat.
            if (!androidExitChecked && !string.IsNullOrEmpty(poprzedni))
            {
                androidExitChecked = true;
                string last = LastAndroidProcessExit();
                if (!string.IsNullOrEmpty(last)) poprzedni += "\n" + last;
            }
            return poprzedni;
        }
    }

    static string LastAndroidProcessExit()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                if (activity == null) return "";
                using (var manager = activity.Call<AndroidJavaObject>("getSystemService", "activity"))
                {
                    if (manager == null) return "";
                    // Android 11+ ApplicationExitInfo. Only the app's own recent
                    // process is requested; there are no new permissions.
                    using (var exits = manager.Call<AndroidJavaObject>(
                        "getHistoricalProcessExitReasons",
                        activity.Call<string>("getPackageName"), 0, 1))
                    {
                        if (exits == null || exits.Call<int>("size") == 0) return "";
                        using (var entry = exits.Call<AndroidJavaObject>("get", 0))
                        {
                            long ageMs = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() -
                                entry.Call<long>("getTimestamp");
                            if (ageMs < 0 || ageMs > 172800000L) return "";
                            int reason = entry.Call<int>("getReason");
                            string description = entry.Call<string>("getDescription") ?? "";
                            description = description.Replace('\n', ' ').Replace('\r', ' ');
                            if (description.Length > 160) description = description.Substring(0, 160);
                            return "OSTATNIE ZAMKNIĘCIE WG ANDROIDA: " +
                                BraxStart.PowodZamkniecia(reason) + " (kod " + reason + ")" +
                                (description.Length > 0 ? " — " + description : "");
                        }
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            // A missing API or device-specific permission is diagnostic-only.
            Debug.LogWarning("BRAXHORN_ANDROID_EXIT_INFO_UNAVAILABLE " + ex.GetType().Name);
        }
#endif
        return "";
    }
    float pokazDo;
    GUIStyle st;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        if (inst) return;
        try
        {
            sciezka = Path.Combine(Application.persistentDataPath, "braxhorn_start.txt");
            if (File.Exists(sciezka))
            {
                var all = File.ReadAllLines(sciezka);
                if (all.Length > 0 && !all[all.Length - 1].Contains(" OK") && !all[all.Length - 1].Contains(" obszar wczytany"))
                {
                    int od = Mathf.Max(0, all.Length - 7);
                    poprzedni = "POPRZEDNI START SIĘ NIE UDAŁ. OSTATNIE ETAPY:\n" + string.Join("\n", all, od, all.Length - od);
                }
            }
            else
            {
                // pierwsze uruchomienie tej wersji dziennika: stary zapis z PlayerPrefs, jeśli był
                string last = PlayerPrefs.GetString(KEtap, "");
                if (!string.IsNullOrEmpty(last) && last != "OK")
                    poprzedni = "POPRZEDNI START ZATRZYMAŁ SIĘ: " + last + " · zajęte " + PlayerPrefs.GetInt(KPamiec, 0) + " MB";
            }
            File.WriteAllText(sciezka, "");   // nowy start = nowy dziennik
        }
        catch { }
        var g = new GameObject("BRAXHORN dziennik startu");
        DontDestroyOnLoad(g);
        inst = g.AddComponent<BraxStartLog>();
        Application.lowMemory += () => Etap("!!! SYSTEM: MAŁO PAMIĘCI");
        Camera.onPreRender += c => { if (!render0 && Ready()) { render0 = true; Etap("4 pierwszy render kamery " + c.name); } };
        Camera.onPostRender += c => { if (render0 && !render1) { render1 = true; Etap("5 po pierwszym renderze"); } };
        Etap("1 start aplikacji · RAM telefonu " + SystemInfo.systemMemorySize + " MB · " + SystemInfo.graphicsDeviceType);
    }

    static bool Ready() => !IsLoadingScene(SceneManager.GetActiveScene().name) &&
                           SceneManager.GetActiveScene().name != "";

    public static void Etap(string etap)
    {
        try
        {
            long res = UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong() / (1024 * 1024);
            long used = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024);
            long gpu = UnityEngine.Profiling.Profiler.GetAllocatedMemoryForGraphicsDriver() / (1024 * 1024);
            maxMb = (int)System.Math.Max(maxMb, res + gpu);
            string w = System.DateTime.Now.ToString("HH:mm:ss") + " " + etap + " · pamięć " + res + " MB (używane " + used + ", grafika " + gpu + ")";
            wiersze.Add(w);
            if (sciezka != null)
                using (var f = new FileStream(sciezka, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                using (var sw = new StreamWriter(f))
                { sw.WriteLine(w); sw.Flush(); f.Flush(true); }
            PlayerPrefs.SetString(KEtap, etap);
            PlayerPrefs.SetInt(KPamiec, maxMb);
        }
        catch { }
    }

    void Awake()
    {
        SceneManager.sceneLoaded += (s, m) =>
        {
            Etap("2 włączona scena " + s.name + (m == LoadSceneMode.Additive ? " (obszar w tle)" : ""));
            if (!IsLoadingScene(s.name) && m == LoadSceneMode.Single)
            {
                render0 = render1 = false;
                StartCoroutine(PoKlatce(s.name));
            }
        };
        if (poprzedni != null) pokazDo = Time.realtimeSinceStartup + 25f;
    }

    IEnumerator PoKlatce(string scena)
    {
        yield return new WaitForEndOfFrame();
        Etap("3 pierwsza klatka " + scena);
        float until = Time.realtimeSinceStartup + 10f;
        while (Time.realtimeSinceStartup < until || BraxObszary.Laduje > 0) yield return null;
        if (SceneManager.GetActiveScene().name == scena) { Etap("OK"); PlayerPrefs.Save(); }
    }

    public static string Ostatnie(int n)
    {
        int od = Mathf.Max(0, wiersze.Count - n);
        return string.Join("\n", wiersze.GetRange(od, wiersze.Count - od));
    }

    void OnGUI()
    {
        if (poprzedni == null || Time.realtimeSinceStartup > pokazDo || BraxStart.Pokazuje) return;
        if (st == null)
        {
            st = new GUIStyle(GUI.skin.box) { wordWrap = true, alignment = TextAnchor.MiddleLeft, fontSize = Mathf.Max(14, Mathf.Min(Screen.width, Screen.height) / 40) };
            st.normal.textColor = new Color(1f, .93f, .75f);
        }
        string info = poprzedni + "\nTelefon: " + SystemInfo.deviceModel + " · RAM " + SystemInfo.systemMemorySize + " MB · " +
                      SystemInfo.graphicsDeviceType + " · " + SystemInfo.graphicsDeviceName + " (" + SystemInfo.graphicsMemorySize + " MB)";
        float w = Screen.width * .92f;
        GUI.Box(new Rect((Screen.width - w) * .5f, Screen.height * .015f, w, st.CalcHeight(new GUIContent(info), w) + 12f), info, st);
    }
}
