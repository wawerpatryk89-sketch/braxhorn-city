using System;
using UnityEngine;

// METRO M1, Unity 6. Wizualny dojazd i odjazd jednego istniejącego składu
// po odcinku już zbudowanych tuneli. Bez tworzenia drugiej stacji lub teleportowania
// pociągu na oczach gracza: reset wyłącznie głęboko za końcem tunelu.
public sealed class BraxMetroKurs : MonoBehaviour
{
    public Transform[] skrzydla = Array.Empty<Transform>();
    public Vector3[] kierunkiSkrzydel = Array.Empty<Vector3>();
    public Transform[] wejscia = Array.Empty<Transform>();
    public float wjazdZ = 124f;
    public float wyjazdX = 124f;
    public float dojazdSekundy = 12f;
    public float postojSekundy = 18f;
    public float odjazdSekundy = 12f;
    public float ukrytySekundy = 18f;
    public float rozsuwanieMetry = .77f;

    Vector3 baza;
    Vector3[] polozeniaSkrzydel;
    float poczatek;
    int poprzedniCykl = -1;
    float otwarcie;
    bool wystartowal;

    public bool NaPeronie { get; private set; }
    public float Otwarcie => otwarcie;
    public bool MoznaWsiadac => NaPeronie && otwarcie > .86f;
    public int NumerCyklu { get; private set; }
    public float Okres => Mathf.Max(1f, dojazdSekundy + postojSekundy + odjazdSekundy + ukrytySekundy);

    void Start()
    {
        baza = transform.position; // w generatorze: pozycja prawdziwego postoju
        polozeniaSkrzydel = new Vector3[skrzydla == null ? 0 : skrzydla.Length];
        for (int i = 0; i < polozeniaSkrzydel.Length; i++)
            if (skrzydla[i]) polozeniaSkrzydel[i] = skrzydla[i].localPosition;
        poczatek = Time.time;
        wystartowal = wejscia != null && wejscia.Length >= 2 && polozeniaSkrzydel.Length >= 4;
        if (!wystartowal)
        {
            Debug.LogError("BRAXHORN_METRO_M1_NIEGOTOWE: brak prawdziwych otworów/drzwi w modelu");
            enabled = false; return;
        }
        transform.position = baza + Vector3.right * wjazdZ;
        Debug.Log("BRAXHORN_METRO_M1_KURS_ZAPISANY cykl=" + Okres.ToString("F0") +
                  "s, dojazd=" + dojazdSekundy + "s, postój=" + postojSekundy +
                  "s, odjazd=" + odjazdSekundy + "s, wejścia=" + wejscia.Length +
                  ", panele=" + skrzydla.Length);
    }

    void Update()
    {
        if (!wystartowal) return;
        float elapsed = Mathf.Max(0f, Time.time - poczatek);
        NumerCyklu = Mathf.FloorToInt(elapsed / Okres);
        float t = Mathf.Repeat(elapsed, Okres);
        float t1 = dojazdSekundy, t2 = t1 + postojSekundy;
        float t3 = t2 + odjazdSekundy;
        NaPeronie = t >= t1 && t < t2;
        float offsetX;
        if (t < t1)
        {
            // Przyjazd od wschodu z tunelem 1: płynne hamowanie przy peronie.
            float k = Mathf.Clamp01(t / Mathf.Max(1f, t1));
            offsetX = Mathf.Lerp(wjazdZ, 0f, Mathf.SmoothStep(0f, 1f, k));
        }
        else if (t < t2) offsetX = 0f;
        else if (t < t3)
        {
            // Odjazd na zachód: wagon całkowicie znika w tunelu zanim zostanie
            // przeniesiony z powrotem na daleki wschód (poza widok kamery).
            float k = Mathf.Clamp01((t - t2) / Mathf.Max(1f, odjazdSekundy));
            offsetX = Mathf.Lerp(0f, -wyjazdX, Mathf.SmoothStep(0f, 1f, k));
        }
        else offsetX = wjazdZ;
        transform.position = baza + Vector3.right * offsetX;

        float cel = NaPeronie ?
            Mathf.Min(Mathf.Clamp01((t - t1 - 1f) / 1.3f),
                      Mathf.Clamp01((t2 - t - 1f) / 1.3f)) : 0f;
        otwarcie = Mathf.Clamp01(cel);
        for (int i = 0; i < polozeniaSkrzydel.Length; i++)
        {
            if (!skrzydla[i]) continue;
            Vector3 kier = i < kierunkiSkrzydel.Length ?
                kierunkiSkrzydel[i] : (i % 2 == 0 ? Vector3.left : Vector3.right);
            skrzydla[i].localPosition = polozeniaSkrzydel[i] +
                kier * (rozsuwanieMetry * otwarcie);
        }
        if (poprzedniCykl != NumerCyklu)
        {
            poprzedniCykl = NumerCyklu;
            Debug.Log("BRAXHORN_METRO_M1_CYKL nr=" + NumerCyklu +
                      " czas=" + Okres.ToString("F0") + "s; bez budowania tuneli i stacji");
        }
    }

    public Vector3 Drzwi(int i)
    {
        if (wejscia == null || i < 0 || i >= wejscia.Length || !wejscia[i]) return transform.position;
        return wejscia[i].position;
    }
}
