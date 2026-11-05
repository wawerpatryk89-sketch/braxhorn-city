using UnityEngine;

// WYSPA Z DALEKA (wykonawca A, 2.11): sylwetka wyspy i areny w scenie głównej (prawdziwa wyspa to obszar doczytywany —
// wczytuje się dopiero przy podpływaniu). Bliżej niż `blizej` m od środka wyspy sylwetka gaśnie (prawdziwa wyspa jest już
// wczytana), dalej świeci. Sprawdzanie co 0.5 s, bez pracy w każdej klatce. Klasa = plik, pola bez #if (zasady 1–2).
public sealed class BraxWyspaDaleko : MonoBehaviour
{
    public Vector3 srodek;
    public float blizej = 230f;
    public Renderer[] renderery = new Renderer[0];
    // C2/M10: sylwetka gaśnie, gdy scena wyspy jest wczytana (nigdy obie naraz — „latające płyty”); dystans = zapas
    public string scena = "";

    Transform gracz; float nast;

    void Update()
    {
        if (Time.unscaledTime < nast) return;
        nast = Time.unscaledTime + .5f;
        if (!gracz) { var p = FindAnyObjectByType<BraxGymPlayer>(); if (p) gracz = p.transform; }
        var cam = Camera.main ? Camera.main.transform : gracz;
        if (!cam) return;
        var d = cam.position - srodek; d.y = 0f;
        // R1: nie wyłączaj zdalnej sylwetki według samego dystansu.
        // Przy wolnym doczytywaniu na telefonie prowadziło to do pustego morza,
        // po którym nagle pojawiała się pełna wyspa.
        bool polaczonaScena = !string.IsNullOrEmpty(scena);
        bool wczytana = polaczonaScena &&
            UnityEngine.SceneManagement.SceneManager.GetSceneByName(scena).isLoaded;
        // Dopóki realna scena wyspy nie jest gotowa, uproszczony brzeg pozostaje widoczny.
        // Bez przypisanej sceny zachowujemy dawny próg, by nie dublować geometrii.
        bool widac = polaczonaScena ? !wczytana : d.sqrMagnitude > blizej * blizej;
        foreach (var r in renderery)
            if (r && r.enabled != widac) r.enabled = widac;
    }
}
