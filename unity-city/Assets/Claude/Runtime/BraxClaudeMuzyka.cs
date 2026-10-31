using UnityEngine;

// BRAXHORN R2 — muzyka w budynku (kasyno): gra tylko, gdy gracz jest w środku; przy wyjściu cichnie na `wygaszenie` metrach
// za ścianą (odległość od prostopadłościanu wnętrza), na ulicy cisza. Dźwięk 2D (bez zanikania z odległością od głośnika —
// cała sala gra równo, jak nagłośnienie). Pętla bez przerwy (plik przycięty i zszyty na początku frazy).
// Gracz szukany po nazwie typu (BraxGymPlayer jest w innym zestawie skryptów), raz na sekundę, dopóki go nie ma.
public class BraxClaudeMuzyka : MonoBehaviour
{
    public AudioClip utwor;
    public Bounds wnetrze;                 // świat: sala z muzyką
    public float glosnosc = .5f, wygaszenie = 4f, szybkosc = .6f;
    AudioSource src; Transform gracz; float szukaj;

    void Start()
    {
        src = gameObject.AddComponent<AudioSource>();
        src.clip = utwor; src.loop = true; src.playOnAwake = false; src.spatialBlend = 0f; src.volume = 0f; src.priority = 64;
    }

    internal static Transform Gracz(ref float nastepnaProba)
    {
        if (Time.unscaledTime < nastepnaProba) return null;
        nastepnaProba = Time.unscaledTime + 1f;
        foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            if (mb && mb.GetType().Name == "BraxGymPlayer") return mb.transform;
        return null;
    }

    void Update()
    {
        if (!src || !utwor) return;
        if (!gracz) { gracz = Gracz(ref szukaj); if (!gracz) return; }
        float d = Mathf.Sqrt(wnetrze.SqrDistance(gracz.position));
        float cel = glosnosc * Mathf.Clamp01(1f - d / wygaszenie);
        src.volume = Mathf.MoveTowards(src.volume, cel, Time.deltaTime * szybkosc);
        if (src.volume > .001f) { if (!src.isPlaying) src.Play(); }
        else if (src.isPlaying) src.Pause();   // pauza, nie stop: po powrocie gra dalej od tego miejsca
    }
}
