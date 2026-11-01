using System;
using UnityEngine;

// MAPA REJONU (właściciel 1.11, 17:02: „jakąś mapkę trzeba zrobić, żeby wiedzieć, gdzie iść”). Obraz z góry malowany przy
// buildzie z tych samych danych, z których powstają drogi i budynki (BraxClaudeOsiedleMapa), plus podpisane miejsca.
// Wyświetla ją MENU → MAPA (BraxTouchControls): pozycja i kierunek Braxa, podpisy, zamknięcie.
public sealed class BraxMapa : MonoBehaviour
{
    [Serializable] public sealed class Miejsce { public string nazwa; public Vector2 xz; }
    public string tytul = "MAPA";
    public Texture2D obraz;
    public Rect swiat;                 // x, z (min) i rozmiar w metrach odpowiadające całemu obrazowi
    public Miejsce[] miejsca = Array.Empty<Miejsce>();
    public static BraxMapa Aktywna
    {
        get
        {
            foreach (var m in FindObjectsByType<BraxMapa>(FindObjectsSortMode.None))
                if (m && m.obraz && m.gameObject.scene == UnityEngine.SceneManagement.SceneManager.GetActiveScene()) return m;
            return FindAnyObjectByType<BraxMapa>();
        }
    }
    public Vector2 NaObraz(Vector3 p) => new Vector2((p.x - swiat.x) / swiat.width, (p.z - swiat.y) / swiat.height);
}
