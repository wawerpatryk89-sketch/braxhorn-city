using System;
using System.Collections.Generic;
using UnityEngine;

// BRAXHORN CITY (Unity 6) — tramwaj na JEDNEJ zamknietej trasie, bez cofania.
// Wszystkie punkty trasy i przystanki sa serializowane przez generator miasta.
// Wsiadanie przez AKCJA z peronu, wysiadanie wylacznie na postoju.
// Niczego nie doklejamy do starej nawierzchni: fizyczne szyny buduje generator.
[RequireComponent(typeof(Rigidbody))]
public sealed class BraxTramwajT1 : MonoBehaviour, IBraxInterakcja
{
    public Vector3[] trasa = Array.Empty<Vector3>();
    public int[] przystankiIndeksy = Array.Empty<int>();
    public string[] nazwyPrzystankow = Array.Empty<string>();
    public float szybkosc = 8f;
    public float postojSekundy = 9f;
    public float promienWejscia = 5.8f;
    public float szerokoscWagonu = 3.5f;
    // Dlugosc osi prefab CAF: niektore importy sa zwrocone lokalnym X, inne Z.
    public Vector3 modelForward = Vector3.right;
    public BraxSeat miejsceGracza;
    // Potwierdzone oddzielne elementy drzwi glTF. Jesli w modelu ich brak, QA raportuje to,
    // a wejscie AKCJA nadal dziala. Nie rysujemy na oryginale udawanych drzwi.
    public Transform[] skrzydlaDrzwi = Array.Empty<Transform>();
    public Vector3[] kierunkiDrzwi = Array.Empty<Vector3>();
    public bool maAnimowaneDrzwi;

    readonly List<float> dlugosci = new List<float>();
    float obwod, metry, predkosc, czekaj, otwarcie, sprawdz, rejestracja;
    int kolejnyPrzystanek, naPrzystanku = -1;
    bool gotowe, logowaneDrzwi;
    Rigidbody rb;
    Vector3[] restDrzwi;
    BraxGymPlayer pasazer;
    public bool Stoi => gotowe && naPrzystanku >= 0 && predkosc < .08f && czekaj > 0f;
    public bool MoznaWysiasc => Stoi && otwarcie > .75f;
    // Strona peronu jest PRAWA względem kierunku jazdy (nie względem lokalnej osi
    // modelu CAF, którą importer może zapisać jako +X albo +Z).
    Vector3 NaPeron
    {
        get
        {
            if (!gotowe || obwod < .1f) return -transform.forward;
            var d = Punkt(metry + 1.25f) - Punkt(metry - 1.25f);
            d.y = 0f;
            return d.sqrMagnitude > .01f ?
                Vector3.Cross(Vector3.up, d.normalized) : -transform.forward;
        }
    }
    public Vector3 DrzwiSwiat => transform.position + NaPeron * (szerokoscWagonu * .5f + .25f);
    public Vector3 WyjscieSwiat => DrzwiSwiat + NaPeron * 1.15f + Vector3.up * .2f;
    public int IndeksPrzystanku => naPrzystanku;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        // Komponent jest dodawany w generatorze i dopiero WTEDY otrzymuje punkty.
        // Awake nie może walidować pustej trasy — wykonujemy to na Start,
        // po serializacji parametrów w wygenerowanej scenie.
    }

    void Start()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
        dlugosci.Clear();
        if (trasa == null || trasa.Length < 16)
        {
            Debug.LogError("BRAXHORN_TRAM_T1_BRAK_TRASY: minimum 16 punktow zamknietej petli");
            enabled = false; return;
        }
        dlugosci.Add(0f);
        for (int i = 0; i < trasa.Length; i++)
            dlugosci.Add(dlugosci[i] + Vector3.Distance(trasa[i], trasa[(i + 1) % trasa.Length]));
        obwod = dlugosci[dlugosci.Count - 1];
        if (obwod < 150f || przystankiIndeksy == null || przystankiIndeksy.Length < 4)
        {
            Debug.LogError("BRAXHORN_TRAM_T1_ZLA_TRASA: obwod=" + obwod + " przystanki=" +
                           (przystankiIndeksy == null ? 0 : przystankiIndeksy.Length));
            enabled = false; return;
        }
        restDrzwi = new Vector3[skrzydlaDrzwi == null ? 0 : skrzydlaDrzwi.Length];
        for (int i = 0; i < restDrzwi.Length; i++)
            if (skrzydlaDrzwi[i]) restDrzwi[i] = skrzydlaDrzwi[i].localPosition;
        maAnimowaneDrzwi = restDrzwi.Length > 0;
        if (miejsceGracza)
        {
            miejsceGracza.tramOwner = this;
            miejsceGracza.label = "TRAMWAJ T1";
        }
        gotowe = true;
        metry = dlugosci[0];
        kolejnyPrzystanek = 0;
        rb.position = Punkt(metry);
        rb.rotation = Kierunek(metry);
        Debug.Log("BRAXHORN_TRAM_T1 petla=" + obwod.ToString("F1") + "m, punkty=" +
                  trasa.Length + ", przystanki=" + przystankiIndeksy.Length +
                  ", prawdziwe_skrzydla_drzwi=" + restDrzwi.Length);
        if (!maAnimowaneDrzwi)
            Debug.LogWarning("BRAXHORN_TRAM_T1_DRZWI: model CAF nie ma rozpoznanych oddzielnych skrzydel; wymagane sprawdzenie wizualne, nie udawac animacji");
    }

    void OnEnable() { BraxInterakcje.Dodaj(this); }
    void OnDisable() { BraxInterakcje.Usun(this); }

    float MetryPrzystanku(int i)
    {
        int n = Mathf.Clamp(przystankiIndeksy[i], 0, trasa.Length - 1);
        return dlugosci[n];
    }

    float Przed(float m, float cel) => Mathf.Repeat(cel - m + obwod, obwod);
    Vector3 Punkt(float m)
    {
        m = Mathf.Repeat(m, obwod);
        int lo = 0, hi = trasa.Length;
        while (lo + 1 < hi) { int md = (lo + hi) >> 1; if (dlugosci[md] <= m) lo = md; else hi = md; }
        float seg = Mathf.Max(.001f, dlugosci[lo + 1] - dlugosci[lo]);
        return Vector3.Lerp(trasa[lo], trasa[(lo + 1) % trasa.Length],
                            Mathf.Clamp01((m - dlugosci[lo]) / seg));
    }
    Quaternion Kierunek(float m)
    {
        Vector3 d = Punkt(m + 1.5f) - Punkt(m - 1.5f);
        d.y = 0f;
        return d.sqrMagnitude < .001f ? transform.rotation :
            Quaternion.FromToRotation(modelForward.sqrMagnitude > .001f ? modelForward.normalized : Vector3.right, d.normalized);
    }

    void FixedUpdate()
    {
        if (!gotowe) return;
        float dt = Time.fixedDeltaTime;
        if (czekaj > 0f)
        {
            czekaj -= dt;
            predkosc = 0f;
            if (czekaj <= 0f)
            {
                if (pasazer && !miejsceGracza) Debug.LogError("BRAXHORN_TRAM_T1 siedzisko niezainstalowane");
                naPrzystanku = -1;
                kolejnyPrzystanek = (kolejnyPrzystanek + 1) % przystankiIndeksy.Length;
                BraxDzwiekiSwiata.Drzwi(transform.position, false);
            }
        }
        else
        {
            float od = Przed(metry, MetryPrzystanku(kolejnyPrzystanek));
            if (od < .16f && predkosc < .5f)
            {
                metry = MetryPrzystanku(kolejnyPrzystanek);
                predkosc = 0f;
                czekaj = Mathf.Max(5f, postojSekundy);
                naPrzystanku = kolejnyPrzystanek;
                BraxDzwiekiSwiata.Drzwi(transform.position, true);
                Debug.Log("BRAXHORN_TRAM_T1_POSTOJ " +
                          (nazwyPrzystankow.Length > naPrzystanku ?
                           nazwyPrzystankow[naPrzystanku] : naPrzystanku.ToString()));
            }
            else
            {
                // Normalny hamulec przed przystankiem, wolniejszy przejazd przez luki.
                Vector3 a = Punkt(metry + 4f) - Punkt(metry - 4f);
                Vector3 b = Punkt(metry + 12f) - Punkt(metry + 4f);
                a.y = b.y = 0;
                float zakret = a.sqrMagnitude > 1f && b.sqrMagnitude > 1f ?
                    Vector3.Angle(a, b) : 0f;
                float vmax = zakret > 8f ? 3.4f : Mathf.Max(3f, szybkosc);
                float hamulec = Mathf.Sqrt(Mathf.Max(0f, 2f * 2.4f * od));
                float cel = Mathf.Min(vmax, hamulec);
                predkosc = Mathf.MoveTowards(predkosc, cel, (cel < predkosc ? 2.4f : 1.25f) * dt);
                // Przy pominięciu stopu nie wolno utknąć 0.3m od platformy:
                // ustawiamy najpierw metry dokładnie na peronie, następny FixedUpdate
                // otworzy drzwi zamiast co klatkę odtwarzać odległość > 0.
                if (od < .3f && predkosc < 1.15f)
                {
                    metry = MetryPrzystanku(kolejnyPrzystanek);
                    predkosc = 0f;
                }
                else metry = Mathf.Repeat(metry + Mathf.Min(predkosc * dt, od), obwod);
            }
        }
        rb.MovePosition(Punkt(metry));
        rb.MoveRotation(Kierunek(metry));
    }

    void Update()
    {
        if (!gotowe) return;
        float t = Stoi ? 1f : 0f;
        otwarcie = Mathf.MoveTowards(otwarcie, t, Time.deltaTime * 1.2f);
        for (int i = 0; i < restDrzwi.Length; i++)
            if (skrzydlaDrzwi[i])
            {
                Vector3 kier = i < kierunkiDrzwi.Length ? kierunkiDrzwi[i] : Vector3.right;
                skrzydlaDrzwi[i].localPosition = restDrzwi[i] + kier * (.65f * otwarcie);
            }
        if (Time.time > sprawdz)
        {
            sprawdz = Time.time + 1f;
            if (!pasazer && miejsceGracza && miejsceGracza.Occupied)
                pasazer = FindAnyObjectByType<BraxGymPlayer>();
            if (pasazer && pasazer.seat != miejsceGracza) pasazer = null;
        }
    }

    void LateUpdate()
    {
        if (miejsceGracza && miejsceGracza.Occupied) miejsceGracza.SyncTram();
    }

    public string Kontekst(Vector3 p)
    {
        if (!gotowe) return "";
        if (pasazer && pasazer.seat == miejsceGracza)
            return MoznaWysiasc ? "WYSIADZ · TRAMWAJ T1" : "TRAMWAJ T1 · NASTEPNY PRZYSTANEK";
        if (!MoznaWysiasc) return "";
        Vector3 d = DrzwiSwiat - p; d.y = 0;
        return d.sqrMagnitude < promienWejscia * promienWejscia ?
            "WSIADZ · TRAMWAJ T1" : "";
    }

    public bool Wykonaj(Vector3 p)
    {
        if (!gotowe || !miejsceGracza) return false;
        if (pasazer && pasazer.seat == miejsceGracza)
        {
            if (!MoznaWysiasc) return true;
            miejsceGracza.Leave();
            pasazer = null;
            return true;
        }
        if (!MoznaWysiasc) return false;
        Vector3 d = DrzwiSwiat - p; d.y = 0f;
        if (d.sqrMagnitude > promienWejscia * promienWejscia) return false;
        var gracz = FindAnyObjectByType<BraxGymPlayer>();
        if (!gracz || gracz.seat || gracz.vessel) return false;
        miejsceGracza.Use(gracz);
        pasazer = gracz.seat == miejsceGracza ? gracz : null;
        return pasazer;
    }

    void OnDestroy()
    {
        if (pasazer && pasazer.seat == miejsceGracza && miejsceGracza)
            miejsceGracza.ForceLeave();
    }
}
