using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// TEST MIASTA NA TELEFONIE (właściciel 1.11, 17:38: „musimy dojść do ładu, żeby odpalić to miasto, co tam się stało”).
// Osiedle (670 MB danych) startuje, miasto (853 MB, a stare 1.5 GB działało) pada — winna jest jedna konkretna rzecz dodana
// po a05fc1a, nie rozmiar. Zamiast pięciu buildów po 2 h: na ekranie startowym APK miasta właściciel zaznacza, co WYŁĄCZYĆ,
// i wczytuje miasto. Wyłączenie działa w pierwszym Awake sceny (BraxStartZnacznik, kolejność −32000) — zanim obiekty
// podejrzanych części się włączą i zanim karta graficzna narysuje pierwszą klatkę. Wybór trafia do dziennika startu,
// więc po wywaleniu ekran startowy pokazuje: „test bez: …” + etap, na którym padło.
public static class BraxStartTest
{
    public static readonly (string klucz, string opis)[] Czesci =
    {
        ("morze", "MORZE I FALE"),
        ("obszary", "NABRZEŻE I WYSPA"),
        ("lodzie", "ŁODZIE I SKUTERY"),
        ("lustro", "LUSTRO W SIŁOWNI"),
        ("mieszkanie", "MIESZKANIE BRAXA, NOVA"),
        ("elewacje", "NOWE ELEWACJE Z OKNAMI"),
        ("swiatla", "LAMPY I ODBICIA"),
        ("dzwiek", "MUZYKA I DŹWIĘK"),
    };
    public static readonly HashSet<string> Wylaczone = new HashSet<string>();
    public static bool Wylaczony(string klucz) => Wylaczone.Contains(klucz);
    public static string Opis() => Wylaczone.Count == 0 ? "nic (normalnie)" : string.Join(", ", Wylaczone);

    // Doczytane obszary (nabrzeże, wyspa) nie mają znacznika — ich obiekty wyłączane w sceneLoaded (po Awake, przed renderem).
    static readonly HashSet<int> zrobione = new HashSet<int>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init() { SceneManager.sceneLoaded += (s, m) => Zastosuj(s); }

    // Wywoływane z BraxStartZnacznik.Awake (pierwsze Awake w scenie gry) i z sceneLoaded (każda scena, raz).
    public static void Zastosuj(Scene scena)
    {
        if (Wylaczone.Count == 0 || !scena.IsValid() || !zrobione.Add(scena.handle)) return;
        int n = 0;
        try
        {
            var prefiksy = new List<string>();
            if (Wylaczony("morze")) prefiksy.Add("Braxhorn harbour water");
            if (Wylaczony("mieszkanie")) { prefiksy.Add("CLAUDE mieszkanie Braxa"); prefiksy.Add("CLAUDE wyposażenie klubu NOVA"); }
            if (Wylaczony("elewacje")) prefiksy.Add("CLAUDE elewacje ulicy");
            if (Wylaczony("obszary")) prefiksy.Add("CLAUDE wyspa");
            var korzenie = scena.GetRootGameObjects();
            if (prefiksy.Count > 0)
                foreach (var r in korzenie)
                    foreach (var t in r.GetComponentsInChildren<Transform>(true))
                    {
                        if (!t) continue;
                        foreach (var p in prefiksy)
                            if (t.name.StartsWith(p, StringComparison.Ordinal)) { t.gameObject.SetActive(false); n++; break; }
                    }
            foreach (var r in korzenie)
            {
                if (Wylaczony("lodzie")) foreach (var v in r.GetComponentsInChildren<BraxVessel>(true)) { v.gameObject.SetActive(false); n++; }
                if (Wylaczony("lustro")) foreach (var m in r.GetComponentsInChildren<BraxGymPlanarMirror>(true)) { m.gameObject.SetActive(false); n++; }
                if (Wylaczony("swiatla"))
                {
                    foreach (var l in r.GetComponentsInChildren<Light>(true)) if (l.type != LightType.Directional) { l.enabled = false; n++; }
                    foreach (var rp in r.GetComponentsInChildren<ReflectionProbe>(true)) { rp.enabled = false; n++; }
                }
                if (Wylaczony("dzwiek")) foreach (var a in r.GetComponentsInChildren<AudioSource>(true)) { a.enabled = false; n++; }
            }
        }
        catch (Exception e) { BraxStartLog.Etap("test: błąd wyłączania " + e.GetType().Name); }
        BraxStartLog.Etap("test w " + scena.name + ": wyłączone " + Opis() + " (" + n + " obiektów)");
    }
}
