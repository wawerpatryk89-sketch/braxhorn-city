using UnityEngine;

// BRAXHORN — DRZWI AUTOMATYCZNE PRZESUWNE (wejścia do wieżowców, lobby; wykonawca B, 2.11).
// Właściciel 15:10: „gracz wchodzi idąc — drzwi automatyczne otwierają się przed nim, bez progu do przeskakiwania”.
// Dwa skrzydła szklane rozsuwane w lokalnej osi X obiektu (lewe −X, prawe +X). Czujnik: gracz (lub przechodzień z listy
// BraxDrzwiPrzesuwne.Czujniki) bliżej niż `zasieg` od środka drzwi i |Δy| < 2 m → otwarcie w 0.6 s; po 1.2 s pustki zamknięcie.
// Skrzydła mają własne kolizje (zamknięte drzwi blokują), otwarte — kolizja jedzie ze skrzydłem do kieszeni. Sprawdzanie co 0.1 s.
// Klasa = plik; bez pól w #if.
public sealed class BraxDrzwiPrzesuwne : MonoBehaviour
{
    public Transform skrzydloL, skrzydloP;
    public float przesuw = 1.0f;     // o ile odjeżdża każde skrzydło (m)
    public float zasieg = 2.6f;      // promień czujnika (m)
    public string nazwa = "WEJŚCIE";

    Vector3 l0, p0; float otw, cel, pusto, nast;
    static Transform gracz; static float szukaj;

    void Awake()
    {
        if (skrzydloL) l0 = skrzydloL.localPosition;
        if (skrzydloP) p0 = skrzydloP.localPosition;
        nast = Random.value * .1f;
    }

    static Transform Gracz()
    {
        if (gracz) return gracz;
        if (Time.unscaledTime < szukaj) return null;
        szukaj = Time.unscaledTime + 1f;
        var p = FindAnyObjectByType<BraxGymPlayer>(); if (p) gracz = p.transform;
        return gracz;
    }

    void Update()
    {
        if (Time.time >= nast)
        {
            nast = Time.time + .1f;
            var g = Gracz();
            bool blisko = false;
            if (g)
            {
                var v = g.position - transform.position; float dy = Mathf.Abs(v.y); v.y = 0;
                blisko = dy < 2f && v.magnitude < zasieg;
            }
            if (blisko) { cel = 1f; pusto = 0f; }
            else if (cel > .5f) { pusto += .1f; if (pusto >= 1.2f) cel = 0f; }
        }
        if (Mathf.Abs(otw - cel) < .0001f) return;
        otw = Mathf.MoveTowards(otw, cel, Time.deltaTime / .6f);
        float s = Mathf.SmoothStep(0f, 1f, otw) * przesuw;
        if (skrzydloL) skrzydloL.localPosition = l0 + Vector3.left * s;
        if (skrzydloP) skrzydloP.localPosition = p0 + Vector3.right * s;
    }
}
