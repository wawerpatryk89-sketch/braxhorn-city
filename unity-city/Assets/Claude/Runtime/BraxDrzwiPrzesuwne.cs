using UnityEngine;

// DRZWI AUTOMATYCZNE PRZESUWNE (wnętrza miasta 2.11; właściciel: „w Tower drzwi się nawet nie otwierają — przechodzę przez
// nie”). Jak w prawdziwym biurowcu / metrze: czujnik nad drzwiami — gracz w promieniu `promien` (i na tym samym poziomie)
// → skrzydła odjeżdżają na boki (lokalne przesunięcie `przesuniecie[i]`), po odejściu same się zamykają.
// Kolizja `blokada` jest włączona, dopóki drzwi nie są otwarte w ponad połowie — przez zamknięte szkło nie da się przejść.
// Sprawdzanie co 0.1 s, ruch skrzydeł tylko w trakcie otwierania/zamykania (bez pracy w spoczynku).
// Tylko pola publiczne bez #if (zasada 2), klasa w pliku o tej samej nazwie (zasada 1).
public sealed class BraxDrzwiPrzesuwne : MonoBehaviour
{
    public string etykieta = "DRZWI";
    public Transform[] skrzydla = new Transform[0];
    public Vector3[] przesuniecie = new Vector3[0];
    public Collider[] blokady = new Collider[0];
    public float promien = 3.4f;
    public float czasRuchu = .7f;
    public bool zawszeOtwarte;          // np. drzwi wagonu metra na postoju

    Vector3[] start;
    float stan, cel, nast;
    bool ruch = true;
    static Transform gracz; static float szukaj;

    void Awake()
    {
        start = new Vector3[skrzydla.Length];
        for (int i = 0; i < skrzydla.Length; i++) if (skrzydla[i]) start[i] = skrzydla[i].localPosition;
        if (zawszeOtwarte) { stan = cel = 1f; Ustaw(); }
    }

    void Update()
    {
        if (!zawszeOtwarte && Time.time >= nast)
        {
            nast = Time.time + .1f + Random.value * .02f;
            float nowy = Blisko() ? 1f : 0f;
            if (nowy != cel)
            {
                cel = nowy; ruch = true;
                BraxDzwiekiSwiata.Drzwi(transform.position, cel > .5f);
                Debug.Log("BRAXHORN_DRZWI_AUTO " + (cel > .5f ? "otwarte" : "zamknięte") + ": " + etykieta);
            }
        }
        if (!ruch) return;
        stan = Mathf.MoveTowards(stan, cel, Time.deltaTime / Mathf.Max(.05f, czasRuchu));
        Ustaw();
        if (stan == cel) ruch = false;
    }

    void Ustaw()
    {
        float t = stan * stan * (3f - 2f * stan);   // łagodny start i hamowanie
        for (int i = 0; i < skrzydla.Length; i++)
            if (skrzydla[i] && i < przesuniecie.Length) skrzydla[i].localPosition = start[i] + przesuniecie[i] * t;
        foreach (var b in blokady) if (b) b.enabled = stan < .55f;
    }

    bool Blisko()
    {
        if (!gracz && Time.unscaledTime >= szukaj)
        {
            szukaj = Time.unscaledTime + 1f;
            var p = FindAnyObjectByType<BraxGymPlayer>(); if (p) gracz = p.transform;
        }
        if (!gracz) return false;
        var v = gracz.position - transform.position;
        if (Mathf.Abs(v.y) > 2.6f) return false;
        v.y = 0f;
        return v.sqrMagnitude < promien * promien;
    }
}
