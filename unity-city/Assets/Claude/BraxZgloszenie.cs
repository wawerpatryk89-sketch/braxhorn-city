using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

// MENU → ZGŁOŚ (build 14; propozycja zatwierdzona przez właściciela 21:27: „jedno dotknięcie = zrzut ekranu + współrzędne +
// nazwa miejsca — zamiast nagrywania filmów; wiem dokładnie, gdzie jest błąd”).
// Zrzut ekranu GRY (bez menu i przycisków: na jedną klatkę interfejs się chowa) czytany z bufora ekranu na końcu klatki
// (ReadPixels — moduł ScreenCapture nie jest w pakietach projektu), zapis JPG + opis TXT do
//   Application.persistentDataPath/zgloszenia/  (telefon: Android/data/<id aplikacji>/files/zgloszenia/).
// Nazwa pliku: zgloszenie_<scena>_x.._y.._z.._<miejsce>_<data>.jpg. Galeria telefonu wymaga MediaStore przez JNI — moduł
// AndroidJNI jest w projekcie wyłączony, więc na razie tylko folder aplikacji (opis w raporcie zlecenia).
public static class BraxZgloszenie
{
    public static string Folder => Path.Combine(Application.persistentDataPath, "zgloszenia");

    // ukryj: wywoływane, żeby interfejs nie rysował się w klatce zrzutu; gotowe: komunikat dla gracza
    public static IEnumerator Zrob(BraxGymPlayer g, Action<bool> ukryj, Action<string> gotowe)
    {
        ukryj(true);
        yield return null;                       // pełna klatka bez interfejsu
        yield return new WaitForEndOfFrame();
        string msg;
        Texture2D t = null;
        try
        {
            int w = Screen.width, h = Screen.height;
            t = new Texture2D(w, h, TextureFormat.RGB24, false);
            t.ReadPixels(new Rect(0, 0, w, h), 0, 0, false);
            t.Apply(false, false);
            ukryj(false);
            var jpg = t.EncodeToJPG(88);
            Directory.CreateDirectory(Folder);
            var p = g ? g.transform.position : Vector3.zero;
            string scena = SceneManager.GetActiveScene().name, miejsce = Miejsce(g, p);
            var ci = CultureInfo.InvariantCulture;
            string nazwa = "zgloszenie_" + Bezpieczne(scena) + "_x" + p.x.ToString("F1", ci) + "_y" + p.y.ToString("F1", ci) + "_z" + p.z.ToString("F1", ci) +
                           (miejsce.Length > 0 ? "_" + Bezpieczne(miejsce) : "") + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", ci);
            File.WriteAllBytes(Path.Combine(Folder, nazwa + ".jpg"), jpg);
            var opis = new StringBuilder();
            opis.AppendLine("BRAXHORN CITY — zgłoszenie z gry");
            opis.AppendLine("czas: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", ci));
            opis.AppendLine("scena: " + scena + " · obszar: " + (g && g.cityWorld ? g.cityWorld.area : "-"));
            opis.AppendLine("pozycja: " + p.x.ToString("F2", ci) + ", " + p.y.ToString("F2", ci) + ", " + p.z.ToString("F2", ci) + (miejsce.Length > 0 ? " · najbliżej: " + miejsce : ""));
            if (g && g.view) { var c = g.view.transform; opis.AppendLine("kamera: " + c.position.ToString("F2") + " · kierunek (yaw) " + c.eulerAngles.y.ToString("F0", ci) + "° · pochylenie " + c.eulerAngles.x.ToString("F0", ci) + "°"); }
            opis.AppendLine("build: " + (g ? g.buildId : "") + " · wersja " + Application.version);
            opis.AppendLine("telefon: " + SystemInfo.deviceModel + " · " + SystemInfo.graphicsDeviceName + " · " + SystemInfo.graphicsDeviceType + " · RAM " + SystemInfo.systemMemorySize + " MB");
            opis.AppendLine("grafika: " + BraxGrafika.Nazwy[(int)BraxGrafika.Aktualny] + " · ekran " + w + "×" + h + " · kl./s " + Application.targetFrameRate);
            opis.AppendLine("pamięć: " + (UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() >> 20) + " MB używane, " + (UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong() >> 20) + " MB zarezerwowane");
            File.WriteAllText(Path.Combine(Folder, nazwa + ".txt"), opis.ToString());
            Debug.Log("BRAXHORN_UI zgłoszenie zapisane: " + Path.Combine(Folder, nazwa + ".jpg"));
            msg = BraxTeksty.F("msg.zapisano", nazwa + ".jpg") + "\n" + BraxTeksty.F("msg.folder", Folder);
        }
        catch (Exception e)
        {
            ukryj(false);
            msg = BraxTeksty.F("msg.blad_zapisu", e.GetType().Name + ": " + e.Message);
            Debug.LogWarning("BRAXHORN_UI zgłoszenie: błąd zapisu " + e);
        }
        finally { if (t) UnityEngine.Object.Destroy(t); }
        gotowe(msg);
    }

    // najbliższe podpisane miejsce: mapa rejonu (podpisy) albo punkt podróży, do 60 m
    static string Miejsce(BraxGymPlayer g, Vector3 p)
    {
        string best = ""; float bd = 60f * 60f;
        var m = BraxMapa.Aktywna;
        if (m && m.miejsca != null)
            foreach (var mi in m.miejsca) { if (mi == null) continue; float d = (new Vector2(p.x, p.z) - mi.xz).sqrMagnitude; if (d < bd) { bd = d; best = mi.nazwa; } }
        if (g && g.cityWorld && g.cityWorld.points != null)
            foreach (var pt in g.cityWorld.points) { if (pt == null) continue; var v = pt.position - p; v.y = 0; if (v.sqrMagnitude < bd) { bd = v.sqrMagnitude; best = string.IsNullOrEmpty(pt.label) ? pt.id : pt.label; } }
        return best;
    }

    // nazwa pliku: bez polskich znaków, spacji i znaków specjalnych, najwyżej 24 znaki
    static string Bezpieczne(string s)
    {
        const string pl = "ąćęłńóśźżĄĆĘŁŃÓŚŹŻ", en = "acelnoszzACELNOSZZ";
        var sb = new StringBuilder();
        foreach (char ch0 in s)
        {
            char ch = ch0; int i = pl.IndexOf(ch); if (i >= 0) ch = en[i];
            if (char.IsLetterOrDigit(ch) && ch < 128) sb.Append(char.ToLowerInvariant(ch));
            else if (sb.Length > 0 && sb[sb.Length - 1] != '-') sb.Append('-');
            if (sb.Length >= 24) break;
        }
        return sb.ToString().Trim('-');
    }
}
