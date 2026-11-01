using UnityEngine;

// Osobny plik (1.11, 22:20): Unity zapisuje skrypt w scenie tylko, gdy nazwa pliku = nazwa klasy; inaczej w APK „brakujący skrypt” → awaria przy wczytywaniu sceny (osiedle 13, miasto od 9).
// Odbicie: każde ciało z fizyką (piłki) — dźwięk przy uderzeniu, siła z prędkości zderzenia.
public sealed class BraxOdbicie : MonoBehaviour
{
    public int rodzaj;             // 0 miękkie, 1 twarde (tenis), 2 ciężkie (lekarska)
    float nast;
    void OnCollisionEnter(Collision c)
    {
        float v = c.relativeVelocity.magnitude; if (v < 1.1f || Time.time < nast) return;
        nast = Time.time + .06f;
        BraxDzwiekiSwiata.Uderzenie(transform.position, v, rodzaj);
    }
}
