using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// DZIENNIK STARTU NA TELEFONIE (31.10, Opus). Właściciel: „wypierdala mnie po instalacji przy włączaniu” — bez kabla i
// logcatu nie wiemy, w którym miejscu. Gra zapisuje etapy startu (PlayerPrefs, od razu na dysk); jeśli poprzednie
// uruchomienie nie doszło do końca (10 s gry po pierwszej klatce), przy następnym starcie u góry ekranu pojawia się
// pasek: na którym etapie się zatrzymała, ile RAM-u ma telefon, jaka grafika (Vulkan / OpenGL) i ile pamięci zajęła gra.
// Zrzut ekranu tego paska wystarczy, żeby wiedzieć, co naprawiać. Ten sam zapis przy przejściu tunelem między rejonami.
public sealed class BraxStartLog : MonoBehaviour
{
    const string KEtap = "braxhorn_start_etap", KPamiec = "braxhorn_start_pamiec";
    static BraxStartLog inst;
    static string poprzedni;
    public static string Poprzedni => poprzedni;   // pokazywane też na ekranie startowym (BraxStart)
    float pokazDo;
    GUIStyle st;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        if (inst) return;
        try
        {
            string last = PlayerPrefs.GetString(KEtap, "");
            if (!string.IsNullOrEmpty(last) && last != "OK")
                poprzedni = "POPRZEDNI START ZATRZYMAŁ SIĘ: " + last + " · zajęte " + PlayerPrefs.GetInt(KPamiec, 0) + " MB";
        }
        catch { }
        var g = new GameObject("BRAXHORN dziennik startu");
        DontDestroyOnLoad(g);
        inst = g.AddComponent<BraxStartLog>();
        Etap("1 start aplikacji");
    }

    public static void Etap(string etap)
    {
        try
        {
            long mb = UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong() / (1024 * 1024);
            PlayerPrefs.SetString(KEtap, etap);
            int prev = etap.StartsWith("1 ") ? 0 : PlayerPrefs.GetInt(KPamiec, 0);   // największe zajęcie pamięci od startu
            PlayerPrefs.SetInt(KPamiec, (int)System.Math.Max(mb, prev));
            PlayerPrefs.Save();
        }
        catch { }
    }

    void Awake()
    {
        SceneManager.sceneLoaded += (s, m) => { Etap("2 wczytana scena " + s.name); StartCoroutine(PoKlatce(s.name)); };
        if (poprzedni != null) pokazDo = Time.realtimeSinceStartup + 25f;
    }

    IEnumerator PoKlatce(string scena)
    {
        yield return new WaitForEndOfFrame();
        Etap("3 pierwsza klatka " + scena);
        yield return new WaitForSecondsRealtime(10f);
        if (SceneManager.GetActiveScene().name == scena) Etap("OK");
    }

    void OnGUI()
    {
        if (poprzedni == null || Time.realtimeSinceStartup > pokazDo || BraxStart.Pokazuje) return;
        if (st == null)
        {
            st = new GUIStyle(GUI.skin.box) { wordWrap = true, alignment = TextAnchor.MiddleCenter, fontSize = Mathf.Max(14, Mathf.Min(Screen.width, Screen.height) / 34) };
            st.normal.textColor = new Color(1f, .93f, .75f);
        }
        string info = poprzedni + "\nTelefon: " + SystemInfo.deviceModel + " · RAM " + SystemInfo.systemMemorySize + " MB · " +
                      SystemInfo.graphicsDeviceType + " · " + SystemInfo.graphicsDeviceName + " (" + SystemInfo.graphicsMemorySize + " MB)";
        float w = Screen.width * .92f;
        GUI.Box(new Rect((Screen.width - w) * .5f, Screen.height * .015f, w, st.CalcHeight(new GUIContent(info), w) + 12f), info, st);
    }
}
