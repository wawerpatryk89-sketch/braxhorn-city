using UnityEngine;

// Filtr głośności kanału MUZYKA (BraxUstawienia): dokładany w grze do obiektu ze źródłem muzyki, za AudioSource
// (kolejność komponentów = kolejność filtrów). Mnoży próbki przez BraxUstawienia.WzmocnienieMuzyki, z płynnym przejściem
// w obrębie bufora (bez trzasków przy ruszaniu suwakiem). Działa w wątku dźwięku — bez API Unity, bez alokacji.
// Osobny plik = nazwa klasy (twarda reguła 1); komponent nie jest zapisywany w scenach (dodawany tylko w grze).
public sealed class BraxGlosnoscKanalu : MonoBehaviour
{
    float poprzednie = 1f;

    void OnAudioFilterRead(float[] data, int channels)
    {
        float cel = BraxUstawienia.WzmocnienieMuzyki;
        float g0 = poprzednie;
        poprzednie = cel;
        if (g0 == 1f && cel == 1f) return;
        int n = data.Length;
        if (n == 0) return;
        float krok = (cel - g0) / n, g = g0;
        for (int i = 0; i < n; i++) { g += krok; data[i] *= g; }
    }
}
