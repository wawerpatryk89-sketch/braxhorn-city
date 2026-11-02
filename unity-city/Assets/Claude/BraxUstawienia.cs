using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// USTAWIENIA GRACZA (build 14, MENU → USTAWIENIA): głośność muzyki i efektów osobno, czułość kamery, rozmiar przycisków,
// licznik kl./s. Zapis w PlayerPrefs przy zamknięciu menu (nie przy każdym ruchu suwaka). Jakość grafiki: BraxGrafika,
// język: BraxTeksty — tu tylko przekazanie.
//
// DŹWIĘK — dwa kanały bez miksera (projekt nie ma zasobu AudioMixer, a w grze nie da się go utworzyć):
//  • efekty = AudioListener.volume (dotyczy wszystkiego, także krótkich dźwięków tworzonych w locie: drzwi, piłki, kroki),
//  • muzyka = źródła muzyki (BraxClaudeMuzyka, BraxClaudeGrajek, NOVA) dostają filtr BraxGlosnoscKanalu, który mnoży
//    próbki przez muzyka / efekty — wynik na wyjściu = dokładnie „muzyka”, niezależnie od suwaka efektów.
//    Efekty nie schodzą poniżej 2 %, gdy muzyka gra (inaczej słuchacz wyciszyłby też muzykę) — 2 % to −34 dB, praktycznie cisza.
//  • przeglądarka (WebGL) nie obsługuje filtrów DSP → tam muzyka idzie za suwakiem efektów (gra jest na telefon).
// Szukanie nowych źródeł: przy każdej scenie i co 3 s (jedna lista, bez pracy co klatkę).
public static class BraxUstawienia
{
    const string KMuzyka = "braxhorn_ui_muzyka", KEfekty = "braxhorn_ui_efekty", KCzulosc = "braxhorn_ui_czulosc",
                 KPrzyciski = "braxhorn_ui_przyciski", KLicznik = "braxhorn_ui_licznik", KZaPlecami = "braxhorn_ui_kamera_za_plecami";
    static bool wczytane;
    static float muzyka = 1f, efekty = 1f, czulosc = 1f;   // domyślnie jak dotąd (100 % / 100 %): bez zmiany brzmienia, dopóki gracz nie ruszy suwaków
    static int przyciski = 1;
    static bool licznik, zaPlecami = true;

    public static float Muzyka { get { Wczytaj(); return muzyka; } set { Wczytaj(); muzyka = Mathf.Clamp01(value); ZastosujDzwiek(); } }
    public static float Efekty { get { Wczytaj(); return efekty; } set { Wczytaj(); efekty = Mathf.Clamp01(value); ZastosujDzwiek(); } }
    public static float Czulosc { get { Wczytaj(); return czulosc; } set { Wczytaj(); czulosc = Mathf.Clamp(value, .3f, 2.5f); } }
    public static int Przyciski { get { Wczytaj(); return przyciski; } set { Wczytaj(); przyciski = Mathf.Clamp(value, 0, 2); Wersja++; } }
    public static bool Licznik { get { Wczytaj(); return licznik; } set { Wczytaj(); licznik = value; } }
    // Kamera za plecami (właściciel 2.11 „kamera ZAWSZE za plecami”, wzór z osiedla 755cabe): domyślnie WŁ.
    public static bool KameraZaPlecami { get { Wczytaj(); return zaPlecami; } set { Wczytaj(); zaPlecami = value; BraxGymPlayer.AutoCamera = value; } }
    public static int Wersja { get; private set; }   // zmiana rozmiaru przycisków = nowy układ ekranu
    public static float SkalaPrzyciskow => Przyciski == 0 ? .8f : Przyciski == 2 ? 1.25f : 1f;
    public static string NazwaPrzyciskow => BraxTeksty.T(Przyciski == 0 ? "ust.male" : Przyciski == 2 ? "ust.duze" : "ust.srednie");

    // wzmocnienie filtra muzyki (czytane z wątku dźwięku — tylko liczba, bez API Unity)
    public static volatile float WzmocnienieMuzyki = 1f;

    static void Wczytaj()
    {
        if (wczytane) return;
        wczytane = true;
        try
        {
            muzyka = Mathf.Clamp01(PlayerPrefs.GetFloat(KMuzyka, 1f));
            efekty = Mathf.Clamp01(PlayerPrefs.GetFloat(KEfekty, 1f));
            czulosc = Mathf.Clamp(PlayerPrefs.GetFloat(KCzulosc, 1f), .3f, 2.5f);
            przyciski = Mathf.Clamp(PlayerPrefs.GetInt(KPrzyciski, 1), 0, 2);
            licznik = PlayerPrefs.GetInt(KLicznik, 0) == 1;
            zaPlecami = PlayerPrefs.GetInt(KZaPlecami, 1) == 1; BraxGymPlayer.AutoCamera = zaPlecami;
        }
        catch { }
    }

    public static void Zapisz()
    {
        Wczytaj();
        try
        {
            PlayerPrefs.SetFloat(KMuzyka, muzyka); PlayerPrefs.SetFloat(KEfekty, efekty); PlayerPrefs.SetFloat(KCzulosc, czulosc);
            PlayerPrefs.SetInt(KPrzyciski, przyciski); PlayerPrefs.SetInt(KLicznik, licznik ? 1 : 0); PlayerPrefs.SetInt(KZaPlecami, zaPlecami ? 1 : 0); PlayerPrefs.Save();
        }
        catch { }
    }

    public static void ZastosujDzwiek()
    {
        Wczytaj();
        float s = efekty;
        if (muzyka > .001f) s = Mathf.Max(s, .02f);
        AudioListener.volume = s;
        WzmocnienieMuzyki = s > .0001f ? muzyka / s : 0f;
    }

    // ---- źródła muzyki: filtr BraxGlosnoscKanalu (raz na źródło) ----
    static readonly HashSet<int> znane = new HashSet<int>();
    static float nastSkan;
    static int scena = -1;
    public static int FiltryMuzyki { get; private set; }

    // Wołane z BraxTouchControls.Update (telefon). Praca tylko przy nowej scenie i co 3 s.
    public static void Pilnuj()
    {
        var sc = SceneManager.GetActiveScene().handle;
        if (sc != scena) { scena = sc; znane.Clear(); nastSkan = 0f; ZastosujDzwiek(); }
        if (Time.unscaledTime < nastSkan) return;
        nastSkan = Time.unscaledTime + 3f;
        if (Application.platform == RuntimePlatform.WebGLPlayer) return;
        var nova = Object.FindObjectsByType<BraxNovaMusic>(FindObjectsSortMode.None);
        foreach (var src in Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
        {
            if (!src || !znane.Add(src.GetInstanceID())) continue;
            if (!JestMuzyka(src, nova) || src.GetComponent<BraxGlosnoscKanalu>()) continue;
            src.gameObject.AddComponent<BraxGlosnoscKanalu>();
            FiltryMuzyki++;
        }
    }

    static bool JestMuzyka(AudioSource src, BraxNovaMusic[] nova)
    {
        foreach (var n in nova) if (n && n.venueSource == src) return true;
        foreach (var mb in src.GetComponents<MonoBehaviour>())
        {
            if (!mb) continue;
            var n = mb.GetType().Name;
            if (n == "BraxClaudeMuzyka" || n == "BraxClaudeGrajek" || n == "BraxNovaMusic") return true;
        }
        return false;
    }
}
