using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

// EKRAN STARTOWY APK (1.11, Opus; porady Google/Unity: „najpierw lekka scena, ciężka doczytywana w tle”).
// Scena 0 w APK jest pusta (ta kamera + ten skrypt), więc ekran pojawia się od razu, zanim telefon zacznie wczytywać
// miasto / osiedle (scena 1). Na ekranie:
//   • powód poprzedniego zamknięcia aplikacji wg Androida (ApplicationExitInfo, Android 11+): BRAK PAMIĘCI (system zabił
//     grę), AWARIA NATYWNA (np. sterownik grafiki), AWARIA JAVA, ZAWIESZENIE (ANR), ZABITA SYGNAŁEM… + zajęta pamięć
//     (PSS/RSS) w chwili zamknięcia — diagnoza bez kabla i bez logcat;
//   • etap, na którym zatrzymał się poprzedni start (BraxStartLog);
//   • model telefonu, RAM, grafika (Vulkan / OpenGL ES), pamięć karty;
//   • pasek wczytywania sceny 1 (LoadSceneAsync).
// Gdy poprzedni start się nie udał, ekran czeka 8 s (czas na zrzut ekranu) albo do dotknięcia.
public sealed class BraxStart : MonoBehaviour
{
    public static bool Pokazuje { get; private set; }
    string wyjscie = "", urzadzenie = "", status = "PRZYGOTOWANIE…";
    float postep;
    bool czekaNaDotyk;
    GUIStyle tytul, tekst, maly;

    IEnumerator Start()
    {
        Pokazuje = true;
        Application.targetFrameRate = 30;
        wyjscie = PowodyZamkniecia();
        urzadzenie = SystemInfo.deviceModel + " · RAM " + SystemInfo.systemMemorySize + " MB · " + SystemInfo.graphicsDeviceType +
                     " · " + SystemInfo.graphicsDeviceName + " (" + SystemInfo.graphicsMemorySize + " MB) · Android API " + Api();
        bool problem = !string.IsNullOrEmpty(BraxStartLog.Poprzedni) || wyjscie.Contains("BRAK PAMIĘCI") || wyjscie.Contains("AWARIA") || wyjscie.Contains("ZAWIESZENIE") || wyjscie.Contains("SYGNAŁ");
        BraxStartLog.Etap("1b ekran startowy");
        if (problem)
        {
            czekaNaDotyk = true; status = "POPRZEDNI START SIĘ NIE UDAŁ — ZRÓB ZRZUT EKRANU. DOTKNIJ, ŻEBY WCZYTAĆ GRĘ";
            float until = Time.realtimeSinceStartup + 8f;
            while (czekaNaDotyk && Time.realtimeSinceStartup < until) { if (Input.touchCount > 0 || Input.GetMouseButtonDown(0)) break; yield return null; }
        }
        else yield return new WaitForSecondsRealtime(.4f);
        czekaNaDotyk = false;
        if (SceneManager.sceneCountInBuildSettings < 2) { status = "BRAK SCENY GRY W PACZCE"; yield break; }
        string nazwa = System.IO.Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(1));
        status = "WCZYTYWANIE: " + nazwa;
        BraxStartLog.Etap("2a wczytywanie sceny " + nazwa);
        yield return Resources.UnloadUnusedAssets();
        var op = SceneManager.LoadSceneAsync(1, LoadSceneMode.Single);
        if (op == null) { status = "NIE MOŻNA WCZYTAĆ SCENY " + nazwa; yield break; }
        while (!op.isDone) { postep = op.progress; yield return null; }
    }

    void OnDestroy() { Pokazuje = false; }

    void OnGUI()
    {
        float s = Mathf.Min(Screen.width, Screen.height);
        if (tytul == null)
        {
            tytul = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = Mathf.RoundToInt(s * .07f) };
            tekst = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperLeft, wordWrap = true, fontSize = Mathf.RoundToInt(s * .032f) };
            maly = new GUIStyle(tekst) { fontSize = Mathf.RoundToInt(s * .026f) };
            tytul.normal.textColor = new Color(.55f, .9f, .25f); tekst.normal.textColor = new Color(.92f, .9f, .86f); maly.normal.textColor = new Color(.7f, .7f, .68f);
        }
        var old = GUI.color; GUI.color = Color.black; GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture); GUI.color = old;
        float w = Screen.width * .9f, x = (Screen.width - w) * .5f, y = Screen.height * .05f;
        GUI.Label(new Rect(0, y, Screen.width, s * .1f), "BRAXHORN CITY", tytul); y += s * .12f;
        GUI.Label(new Rect(x, y, w, s * .08f), status, tekst); y += s * .09f;
        // pasek wczytywania
        GUI.color = new Color(.2f, .2f, .2f); GUI.DrawTexture(new Rect(x, y, w, s * .02f), Texture2D.whiteTexture);
        GUI.color = new Color(.55f, .9f, .25f); GUI.DrawTexture(new Rect(x, y, w * Mathf.Clamp01(postep / .9f), s * .02f), Texture2D.whiteTexture);
        GUI.color = old; y += s * .05f;
        string diag = "";
        if (!string.IsNullOrEmpty(BraxStartLog.Poprzedni)) diag += BraxStartLog.Poprzedni + "\n";
        if (!string.IsNullOrEmpty(wyjscie)) diag += wyjscie;
        if (diag.Length > 0) { GUI.Label(new Rect(x, y, w, Screen.height - y - s * .12f), diag, tekst); }
        GUI.Label(new Rect(x, Screen.height - s * .1f, w, s * .09f), urzadzenie, maly);
    }

    // Poziom API Androida z SystemInfo.operatingSystem („Android OS 15 / API-35 (…)”) — bez JNI: moduł AndroidJNI jest
    // w tym projekcie wyłączony (build 7: CS1069 AndroidJavaClass → APK się nie zbudowało).
    static int Api()
    {
        var m = System.Text.RegularExpressions.Regex.Match(SystemInfo.operatingSystem ?? "", @"API-(\d+)");
        return m.Success ? int.Parse(m.Groups[1].Value) : 0;
    }

    static string Nazwa(int r)
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

    // Powód zamknięcia wg Androida (ApplicationExitInfo) wymaga modułu AndroidJNI (com.unity.modules.androidjni), który jest
    // w projekcie wyłączony; włączenie zmienia manifest pakietów = pełny reimport biblioteki w CI (+1–1.5 h). Do zrobienia razem
    // z następną zmianą pakietów (Addressables). Do tego czasu: etap poprzedniego startu z BraxStartLog + dane telefonu.
    static string PowodyZamkniecia() => "";
}
