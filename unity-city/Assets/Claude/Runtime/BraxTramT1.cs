using System;
using System.Collections.Generic;
using UnityEngine;

// TRAMWAJ T1 — LINIA W PĘTLI, SKŁAD PRZEGUBOWY (9.11, właściciel 18:21: „drzwi się źle otwierają”, „nie łamie się na
// zakrętach”, „żeby elegancko kursował”). Dotąd: sztywny model 44 m wahadłowo na prostym odcinku 130 m (rewers na kozłach),
// drzwi CAF nierozpoznane (audyt sceny: „drzwi CAF: 0”) — nic się nie otwierało.
// Teraz (dane liczone przy buildzie, BraxRegionMiastoB.NdTramwaj):
//  • trasa: zamknięta pętla po prawych pasach ulic wokół kwartału C2–C3 (Tramwajowa → C2 → Aleja Centralna → C3), łuki R 16 m
//    na skrzyżowaniach, punkty co 0.5 m (`trasa`, `trasaS`), limit prędkości w łukach (`vLimit`, przyspieszenie boczne ≤ 1.1 m/s²);
//  • skład: człony modelu CAF rozcięte w przegubach (harmonijkach); każdy człon stoi końcami NA TORZE w odległościach
//    `czlonPrzod`/`czlonTyl` od czoła — na łuku człony łamią się w przegubach jak prawdziwy tramwaj;
//  • jazda: rozruch 1.0 m/s², hamowanie 1.3 m/s², 25 km/h na prostych, w łukach wolniej (patrzy przed czoło i pod cały skład),
//    zatrzymanie co do centymetra na przystankach (`przystanki`), postój z drzwiami po prawej stronie;
//  • drzwi odskokowo-przesuwne (jak w CAF Urbos): najpierw odsunięcie na zewnątrz (`drzwiWysun`), potem przesuw wzdłuż
//    pudła (`drzwiPrzesun`), zamykanie odwrotnie, dźwięk drzwi;
//  • skrzyżowania: przed wjazdem tramwaj czeka, aż auta z pola skrzyżowania zjadą, potem auta stoją (strefa zatrzymania
//    BraxStrefyZatrzymania — ta sama, której słuchają auta i piesi), dopóki tył składu nie opuści skrzyżowania;
//  • gracz na torze: hamuje przed nim (dzwonek = dźwięk ostrzegawczy drzwi), rusza, gdy zejdzie.
public sealed class BraxTramT1 : MonoBehaviour
{
    public Vector3[] trasa = new Vector3[0];
    public float[] trasaS = new float[0];
    public float[] vLimit = new float[0];
    public float dlugosc;
    public Transform[] czlony = new Transform[0];
    public float[] czlonPrzod = new float[0], czlonTyl = new float[0];
    public float s;
    public float[] przystanki = new float[0];
    public string[] przystankiNazwy = new string[0];
    public float[] skrzyzowanieOd = new float[0], skrzyzowanieDo = new float[0];
    public Vector3[] skrzyzowanieSrodek = new Vector3[0];
    public float skrzyzowaniePromien = 9.5f;
    public float vmax = 7f, przysp = 1f, ham = 1.3f, postoj = 14f;
    public Transform[] drzwi = new Transform[0];
    public Vector3[] drzwiDom = new Vector3[0], drzwiWysun = new Vector3[0], drzwiPrzesun = new Vector3[0];

    // zgodność ze starymi kontrolami sceny (jeden odcinek, oś na torze)
    public float zachodX, wschodX, torZ, peronX0, peronX1, peronZ;

    float v, postojDo = -1f, drzwiStan, nastAuta, nastSzukaj;
    int celPrzystanek = -1;
    bool naPostoju, drzwiOtwarte;
    BraxAuto[] auta = new BraxAuto[0];
    Transform gracz;
    int[] strefaAut, strefaPieszych;
    bool[] zajete;

    public float DlugoscSkladu => czlonTyl.Length > 0 ? czlonTyl[czlonTyl.Length - 1] : 0f;

    void Start()
    {
        if (trasa.Length < 4 || dlugosc < 50f || czlony.Length == 0)
        { Debug.LogWarning("BRAXHORN_TRAM_T1 DO POPRAWKI: brak trasy lub członów — tramwaj stoi"); enabled = false; return; }
        int n = skrzyzowanieOd.Length;
        strefaAut = new int[n]; strefaPieszych = new int[n]; zajete = new bool[n];
        for (int i = 0; i < n; i++)
        {
            int k = i;
            strefaAut[i] = BraxStrefyZatrzymania.Dodaj(new BraxStrefyZatrzymania.Strefa { srodek = skrzyzowanieSrodek[i], promien = skrzyzowaniePromien + 4f, dlaPieszych = false, aktywna = () => zajete[k] });
            strefaPieszych[i] = BraxStrefyZatrzymania.Dodaj(new BraxStrefyZatrzymania.Strefa { srodek = skrzyzowanieSrodek[i], promien = skrzyzowaniePromien + 6f, dlaPieszych = true, aktywna = () => zajete[k] });
        }
        UstawCzlony();
        // start na przystanku: postój z otwartymi drzwiami, potem kurs
        for (int i = 0; i < przystanki.Length; i++)
            if (Mathf.Abs(Mathf.DeltaAngle(0f, (s - przystanki[i]) / dlugosc * 360f)) * dlugosc / 360f < .15f) { naPostoju = true; postojDo = Time.time + postoj; celPrzystanek = i; s = Zawin(przystanki[i]); }
        Debug.Log("BRAXHORN_TRAM_T1 pętla " + dlugosc.ToString("F0") + " m, członów " + czlony.Length + " (skład " + DlugoscSkladu.ToString("F1") + " m), drzwi " + drzwi.Length +
                  ", przystanki " + przystanki.Length + ", skrzyżowania " + n);
    }

    void OnDestroy()
    {
        if (strefaAut != null) foreach (var id in strefaAut) BraxStrefyZatrzymania.Usun(id);
        if (strefaPieszych != null) foreach (var id in strefaPieszych) BraxStrefyZatrzymania.Usun(id);
    }

    float Zawin(float x) { x %= dlugosc; return x < 0f ? x + dlugosc : x; }
    float DoPrzodu(float cel) => Zawin(cel - s);   // odległość czoła do punktu trasy w kierunku jazdy

    int Indeks(float x)
    {
        x = Zawin(x);
        int lo = 0, hi = trasaS.Length - 1;
        while (hi - lo > 1) { int m = (lo + hi) >> 1; if (trasaS[m] <= x) lo = m; else hi = m; }
        return lo;
    }

    public Vector3 Punkt(float x)
    {
        x = Zawin(x);
        int i = Indeks(x); int j = (i + 1) % trasa.Length;
        float a = trasaS[i], b = j == 0 ? dlugosc : trasaS[j];
        return Vector3.LerpUnclamped(trasa[i], trasa[j], b > a ? (x - a) / (b - a) : 0f);
    }

    float Limit(float x) => vLimit.Length == trasa.Length ? vLimit[Indeks(x)] : vmax;

    void UstawCzlony()
    {
        for (int i = 0; i < czlony.Length; i++)
        {
            if (!czlony[i]) continue;
            var f = Punkt(s - czlonPrzod[i]); var r = Punkt(s - czlonTyl[i]);
            var d = f - r; d.y = 0f;
            czlony[i].position = (f + r) * .5f;
            if (d.sqrMagnitude > 1e-6f) czlony[i].rotation = Quaternion.LookRotation(d.normalized, Vector3.up);
        }
    }

    void Update()
    {
        float dt = Mathf.Min(Time.deltaTime, .1f);
        if (Time.unscaledTime > nastSzukaj) { nastSzukaj = Time.unscaledTime + 1.5f; auta = FindObjectsByType<BraxAuto>(FindObjectsSortMode.None); if (!gracz) { var p = FindAnyObjectByType<BraxGymPlayer>(); if (p) gracz = p.transform; } }
        float L = DlugoscSkladu;
        // ---- skrzyżowania: zajęte, gdy czoło jest 18 m przed polem albo skład jeszcze w nim ----
        bool czekajSkrz = false; float doSkrz = 1e9f;
        for (int i = 0; i < skrzyzowanieOd.Length; i++)
        {
            float doWjazdu = DoPrzodu(skrzyzowanieOd[i]);
            float dlPola = Zawin(skrzyzowanieDo[i] - skrzyzowanieOd[i]);
            bool czoloWPolu = Zawin(s - skrzyzowanieOd[i]) <= dlPola;
            bool tylWPolu = Zawin(s - L - skrzyzowanieOd[i]) <= dlPola;
            bool skladWPolu = czoloWPolu || tylWPolu || (Zawin(s - skrzyzowanieOd[i]) < dlPola + L);
            bool blisko = doWjazdu < 18f;
            if (!skladWPolu && blisko && !zajete[i])
            {
                // przed wjazdem: auta, które już są w polu skrzyżowania, muszą z niego zjechać
                bool autoWPolu = false;
                foreach (var a in auta)
                {
                    if (!a) continue;
                    var d = a.transform.position - skrzyzowanieSrodek[i]; d.y = 0f;
                    if (d.sqrMagnitude < skrzyzowaniePromien * skrzyzowaniePromien) { autoWPolu = true; break; }
                }
                if (autoWPolu) { czekajSkrz = true; doSkrz = Mathf.Min(doSkrz, doWjazdu); }
                else zajete[i] = true;
            }
            else if (!skladWPolu && !blisko) zajete[i] = false;
            else if (skladWPolu) zajete[i] = true;
        }
        // ---- przystanek ----
        if (naPostoju)
        {
            float t = Time.time;
            bool otworz = t < postojDo - 2.2f && t > postojDo - postoj + .8f;
            Drzwi(otworz ? 1f : 0f, dt);
            if (t < postojDo || drzwiStan > .01f) { UstawCzlony(); return; }
            naPostoju = false; celPrzystanek = -1;
        }
        Drzwi(0f, dt);
        if (drzwiStan > .01f) return;
        // ---- prędkość docelowa: łuki przed czołem i pod składem, przystanek, skrzyżowanie, gracz ----
        float cel = vmax;
        for (float d = -L; d <= 0f; d += 2f) cel = Mathf.Min(cel, Limit(s + d));
        for (float d = 2f; d <= 40f; d += 2f) cel = Mathf.Min(cel, Mathf.Sqrt(Limit(s + d) * Limit(s + d) + 2f * ham * d));
        int nast = -1; float doPrz = 1e9f;
        for (int i = 0; i < przystanki.Length; i++) { float d = DoPrzodu(przystanki[i]); if (d > .02f && d < doPrz) { doPrz = d; nast = i; } }
        if (nast >= 0 && doPrz < 60f) cel = Mathf.Min(cel, Mathf.Sqrt(2f * ham * Mathf.Max(0f, doPrz - .03f)));
        if (czekajSkrz) cel = Mathf.Min(cel, Mathf.Sqrt(2f * ham * Mathf.Max(0f, doSkrz - 1.5f)));
        if (gracz)
        {
            for (float d = 1f; d <= 20f; d += 1f)
            {
                var q = Punkt(s + d) - gracz.position; q.y = 0f;
                if (q.sqrMagnitude < 2.1f * 2.1f) { cel = Mathf.Min(cel, Mathf.Sqrt(2f * ham * Mathf.Max(0f, d - 3f))); break; }
            }
        }
        float a2 = cel > v ? przysp : ham * 1.4f;
        v = Mathf.MoveTowards(v, cel, a2 * dt);
        float krok = v * dt;
        if (nast >= 0 && doPrz < 1f && (doPrz < .12f || krok >= doPrz))
        {
            s = Zawin(przystanki[nast]); v = 0f; naPostoju = true; postojDo = Time.time + postoj; celPrzystanek = nast;
            Debug.Log("BRAXHORN_TRAM_T1 przystanek " + (nast < przystankiNazwy.Length ? przystankiNazwy[nast] : nast.ToString()));
        }
        else s = Zawin(s + krok);
        UstawCzlony();
    }

    void Drzwi(float cel, float dt)
    {
        float przed = drzwiStan;
        drzwiStan = Mathf.MoveTowards(drzwiStan, cel, dt / 1.6f);
        if (przed <= .001f && drzwiStan > .001f && drzwi.Length > 0) BraxDzwiekiSwiata.Drzwi(czlony.Length > 0 && czlony[0] ? czlony[0].position : transform.position, true);
        if (przed >= .999f && drzwiStan < .999f && drzwi.Length > 0) BraxDzwiekiSwiata.Drzwi(czlony.Length > 0 && czlony[0] ? czlony[0].position : transform.position, false);
        // odskok (0–30 %), przesuw (30–100 %)
        float w = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(drzwiStan / .3f));
        float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((drzwiStan - .3f) / .7f));
        for (int i = 0; i < drzwi.Length; i++)
            if (drzwi[i]) drzwi[i].localPosition = drzwiDom[i] + drzwiWysun[i] * w + drzwiPrzesun[i] * p;
    }
}
