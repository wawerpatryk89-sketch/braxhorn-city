using System.Collections.Generic;
using UnityEngine;

// MRUGANIE LASERÓW I STOŻKÓW (właściciel 3.11 15:18 i 15:58: „mrugające lasery, zmienne tempo… żeby mrugały lepiej, w jednej
// i drugiej imprezie”). Wspólny wzór dla NOVA (miasto) i PULS CLUB (osiedle): tempo 100–150 BPM płynnie zmienne (dwie fale),
// co 8 uderzeń inny układ: błysk na uderzenie, stroboskop 1/8, lewa–prawa na zmianę, pełne światło z krótkim wygaszeniem,
// fala (kolejne źródła po sobie), przerwa (ciemno) przed „dropem”. Przełączane tylko renderery wiązek — oprawy zostają.
public sealed class BraxStrob
{
    readonly List<Renderer[]> zrodla = new List<Renderer[]>();
    float faza;
    public float Bpm { get; private set; } = 124f;

    // wiązki = renderery dzieci, których nazwa obiektu lub materiału zawiera jedno ze słów
    public static Renderer[] Wiazki(Transform t, params string[] slowa)
    {
        var l = new List<Renderer>();
        if (!t) return l.ToArray();
        foreach (var r in t.GetComponentsInChildren<Renderer>(true))
        {
            string n = (r.gameObject.name + " " + (r.sharedMaterial ? r.sharedMaterial.name : "")).ToLowerInvariant();
            foreach (var s in slowa) if (n.Contains(s)) { l.Add(r); break; }
        }
        return l.ToArray();
    }
    public void Dodaj(Renderer[] r) { if (r != null && r.Length > 0) zrodla.Add(r); }
    public int Ile => zrodla.Count;

    public void Krok(float t, float dt)
    {
        Bpm = 125f + 17f * Mathf.Sin(t * .052f) + 8f * Mathf.Sin(t * .23f + 1.3f);
        faza += dt * Bpm / 60f;
        int takt = (int)(faza / 8f), u = (int)faza % 8; float f = faza - Mathf.Floor(faza);
        int uklad = (takt * 7 + 3) % 6;
        int n = zrodla.Count;
        for (int i = 0; i < n; i++)
        {
            bool on;
            switch (uklad)
            {
                case 0: on = f < .35f; break;                                              // błysk na każde uderzenie
                case 1: on = (f * 8f) % 1f < .45f; break;                                  // stroboskop 1/8
                case 2: on = ((u + i) % 2 == 0) ? f < .8f : false; break;                  // lewa–prawa na zmianę
                case 3: on = !(u == 7 && f > .5f); break;                                  // pełne, wygaszenie przed taktem
                case 4: on = ((int)(f * n) == i) || (u % 4 == 0 && f < .25f); break;       // fala po źródłach + akcent
                default: on = u < 6 ? (f * 4f) % 1f < .5f : (u == 7 && f > .75f); break;  // strob, przerwa przed „dropem”
            }
            foreach (var r in zrodla[i]) if (r && r.enabled != on) r.enabled = on;
        }
    }
    public void Wszystkie(bool on) { foreach (var z in zrodla) foreach (var r in z) if (r && r.enabled != on) r.enabled = on; }
}
