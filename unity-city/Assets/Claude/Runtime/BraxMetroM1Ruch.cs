using System;
using UnityEngine;

// METRO BRAXHORN M1 — inscenizowany kurs co około minutę bez drugiej stacji.
// Jeden autentyczny skład kane_sk06 (CC BY) w tej SAMEJ hali peronowej.
// Wjazd z tunelu -> łagodne hamowanie -> drzwi -> postój -> odjazd -> powtórka.
// Brak pełnej sieci kolejowej i brak przebudowy podziemi (zgodnie z właścicielem).
[DefaultExecutionOrder(-110)]
[RequireComponent(typeof(Rigidbody))]
public sealed class BraxMetroM1Ruch : MonoBehaviour
{
    public float dojazdSekundy = 10f;
    public float otwieranieSekundy = 1.4f;
    public float postojSekundy = 15f;
    public float zamykanieSekundy = 1.4f;
    public float odjazdSekundy = 10f;
    public float ciszaSekundy = 22f; // cykl 59.8 s
    public float wejscieOffsetX = 95f;
    public float wyjazdOffsetX = -85f;
    public Transform[] drzwiLewe = Array.Empty<Transform>();
    public Transform[] drzwiPrawe = Array.Empty<Transform>();
    public Vector3[] punktyWejscia = Array.Empty<Vector3>(); // lokalne do korzenia składu
    public float rozsuw = .65f;

    public bool DrzwiOtwarte => etap == Etap.Postoj && rozwarcie >= .97f;
    public bool NaPeronie => etap == Etap.Otwieranie || etap == Etap.Postoj || etap == Etap.Zamykanie;
    public int NumerKursu => numerKursu;
    public int DrzwiLiczba => Mathf.Min(drzwiLewe.Length, drzwiPrawe.Length);
    public Vector3 DrzwiSwiat(int i)
    {
        if (punktyWejscia == null || punktyWejscia.Length == 0) return transform.position;
        return transform.TransformPoint(punktyWejscia[Mathf.Clamp(i, 0, punktyWejscia.Length - 1)]);
    }

    enum Etap { Dojazd, Otwieranie, Postoj, Zamykanie, Odjazd, Tunel }
    Etap etap = Etap.Dojazd;
    float czas, rozwarcie;
    int numerKursu;
    Rigidbody rb;
    Vector3 centrum; // pozycja korzenia, gdy pociąg idealnie stoi przy peronie
    Vector3[] lStart, pStart;
    Renderer[] renderery;
    Collider[] kolizje;
    bool[] rendereryPoczatkowo, kolizjePoczatkowo;
    bool widoczny = true;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        centrum = transform.position;
        lStart = new Vector3[drzwiLewe.Length];
        pStart = new Vector3[drzwiPrawe.Length];
        for (int i = 0; i < lStart.Length; i++)
            if (drzwiLewe[i]) lStart[i] = drzwiLewe[i].localPosition;
        for (int i = 0; i < pStart.Length; i++)
            if (drzwiPrawe[i]) pStart[i] = drzwiPrawe[i].localPosition;
        renderery = GetComponentsInChildren<Renderer>(true);
        kolizje = GetComponentsInChildren<Collider>(true);
        rendereryPoczatkowo = Array.ConvertAll(renderery, x => x && x.enabled);
        kolizjePoczatkowo = Array.ConvertAll(kolizje, x => x && x.enabled);
        if (DrzwiLiczba == 0 || renderery.Length == 0)
        {
            Debug.LogError("BRAXHORN_METRO_M1_NIEGOTOWY brak scenicznych par drzwi lub rendererów");
            enabled = false;
            return;
        }
        rb.position = centrum + Vector3.right * wejscieOffsetX;
        numerKursu = 1;
        Debug.Log("BRAXHORN_METRO_M1_RUCH pojemny model z galerii kane_sk06, pary_drzwi=" +
            DrzwiLiczba + " cykl_s=" + (dojazdSekundy + otwieranieSekundy +
            postojSekundy + zamykanieSekundy + odjazdSekundy + ciszaSekundy).ToString("F1") +
            " — sztuczny wjazd, BEZ nowego metra");
    }

    void Widocznosc(bool wl)
    {
        if (wl == widoczny) return;
        widoczny = wl;
        for (int i = 0; i < renderery.Length; i++)
            if (renderery[i]) renderery[i].enabled = wl && rendereryPoczatkowo[i];
        for (int i = 0; i < kolizje.Length; i++)
            if (kolizje[i]) kolizje[i].enabled = wl && kolizjePoczatkowo[i];
    }

    void Przejdz(Etap nowy)
    {
        czas = 0f;
        etap = nowy;
        if (nowy == Etap.Postoj)
        {
            Debug.Log("BRAXHORN_METRO_M1_POSTOJ kurs=" + numerKursu + " drzwi otwarte, pasażerowie przy peronie");
        }
        else if (nowy == Etap.Zamykanie)
        {
            Debug.Log("BRAXHORN_METRO_M1_ODJAZD kurs=" + numerKursu + " zamykanie drzwi");
        }
        else if (nowy == Etap.Tunel) Widocznosc(false);
        else if (nowy == Etap.Dojazd)
        {
            numerKursu++;
            // Teleport z tunelu ZANIM skórki zostaną pokazane.
            // Nie ma jednej klatki z modelem składu migniętym na zachodzie.
            if (rb) rb.position = centrum + Vector3.right * wejscieOffsetX;
            Widocznosc(true);
        }
    }

    void FixedUpdate()
    {
        if (!rb || !enabled) return;
        czas += Time.fixedDeltaTime;
        float x = 0f;
        switch (etap)
        {
            case Etap.Dojazd:
                x = Mathf.Lerp(wejscieOffsetX, 0f,
                    Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(czas / Mathf.Max(.2f, dojazdSekundy))));
                if (czas >= dojazdSekundy) { x = 0f; Przejdz(Etap.Otwieranie); }
                break;
            case Etap.Otwieranie:
                if (czas >= otwieranieSekundy) Przejdz(Etap.Postoj);
                break;
            case Etap.Postoj:
                if (czas >= postojSekundy) Przejdz(Etap.Zamykanie);
                break;
            case Etap.Zamykanie:
                if (czas >= zamykanieSekundy) Przejdz(Etap.Odjazd);
                break;
            case Etap.Odjazd:
                x = Mathf.Lerp(0f, wyjazdOffsetX,
                    Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(czas / Mathf.Max(.2f, odjazdSekundy))));
                if (czas >= odjazdSekundy) { x = wyjazdOffsetX; Przejdz(Etap.Tunel); }
                break;
            case Etap.Tunel:
                x = wyjazdOffsetX;
                if (czas >= ciszaSekundy) { x = wejscieOffsetX; Przejdz(Etap.Dojazd); }
                break;
        }
        rb.MovePosition(centrum + Vector3.right * x);
    }

    void Update()
    {
        float cel = etap == Etap.Postoj || etap == Etap.Otwieranie ? 1f : 0f;
        float dt = Mathf.Max(.01f, cel > rozwarcie ? otwieranieSekundy : zamykanieSekundy);
        rozwarcie = Mathf.MoveTowards(rozwarcie, cel, Time.deltaTime / dt);
        // Drzwi sceniczne są parami PRZED oryginalną ścianą modelu.
        // Nie niszczymy siatek modelu ani nie udajemy fizycznego wejścia do kabiny.
        for (int i = 0; i < lStart.Length; i++)
            if (drzwiLewe[i]) drzwiLewe[i].localPosition = lStart[i] + Vector3.left * (rozsuw * rozwarcie);
        for (int i = 0; i < pStart.Length; i++)
            if (drzwiPrawe[i]) drzwiPrawe[i].localPosition = pStart[i] + Vector3.right * (rozsuw * rozwarcie);
    }
}
