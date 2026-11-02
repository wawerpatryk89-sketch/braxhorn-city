using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// PRZECHODZIEŃ (właściciel 1.11: „ludzie mają tylko żyć — chodzić, nic nie atakować”; build 14: ludzie Rocketbox).
// TRASA: punkty po osiach chodników (generator BraxClaudeOsiedleLudzie — sieć chodników osiedla A i B, alejki parku),
// przez jezdnię tylko po zebrach (odcinki „przejscie”): na krawężniku przystaje, rozgląda się w lewo i w prawo, czeka,
// gdy BraxStrefyZatrzymania.CzyStop(…, pieszy: true) (czerwone) albo auto blisko przejścia (najwyżej kilka sekund —
// auto czeka na pieszego). Idzie prawą stroną chodnika, omija przeszkody i ludzi (sprawdzenie przed sobą co 0.3 s),
// przed graczem staje. Na końcu trasy zawraca. Czasem przystaje (rozgląda się, telefon, kawa).
// ROZMOWY (18:35): mija gracza — czasem się wita; AKCJA = POGADAJ; dwóch przechodniów obok siebie czasem staje
// i rozmawia — z gestami (klipy rozmowy Rocketbox: mówi / słucha na zmianę) i dymkami (BraxDymki).
// CIOSY (21:59 / 22:00): trafiony — odrzut w kierunku ciosu, odchylenie tułowia (tylko obrót kości), łapie się za głowę
// / zgina (klip reakcji wylosowany dla osoby — każdy reaguje inaczej), okrzyk; potem UCIECZKA biegiem od gracza
// z oglądaniem się za siebie (obrót głowy), czasem krzyk z daleka; przez minutę omija gracza szerokim łukiem.
// Świadkowie do 12 m: odwracają się i patrzą, część krzyczy, część ucieka. Ludzie nie oddają (zasada właściciela).
// KOSZT (telefon): animacja tylko w promieniu BraxLudzie.Promien od kamery (do 60 m, limit osób), dalej ruch co 0.25 s,
// bardzo daleko co 1 s; ruchy procedur (głowa, tułów) tylko blisko.
public sealed class BraxPieszy : MonoBehaviour, IBraxInterakcja, IBraxOsoba
{
    // --- trasa ---
    public Vector3 a, b;                        // tryb zapasowy (przechodnie Mixamo z build 13): odcinek tam i z powrotem
    public Vector3[] trasa = new Vector3[0];    // punkty po chodnikach
    public bool[] przejscie = new bool[0];      // odcinek i → i+1 przez jezdnię (po zebrze)
    public float bok = .35f;                    // odsunięcie w prawo od osi chodnika (m)
    public float predkosc = 1.25f;
    // --- animacje (Legacy) ---
    public Animation anim;
    public string chod = "Walking", stoi = "Idle", bieg = "";
    public string[] postoje = new string[0], reakcje = new string[0], mowi = new string[0], slucha = new string[0];
    public float predkoscKlipu = 1.3f, predkoscKlipuBiegu = 3.5f;   // m/s klipu chodu / biegu w skali tej postaci
    // --- ruchy procedur (tylko obroty kości) ---
    public Transform glowa, szyja, kregoslup;
    public float ton = 1f, wzrost = 1.8f;

    enum Stan { Idzie, Postoj, Czeka, Rozmowa, Trafiony, Ucieka, Wraca, Patrzy }
    Stan stan = Stan.Idzie;
    int i, kier = 1, zablokowany, wariant;
    float doKiedy, nast, ostatni, nastRozmowa, nastPowitanie, nastSprawdz, przeszedl, objazd, objazdDo, unikaDo, czekaOd;
    float lean, leanCel, patrzWaga, patrzCel, nastObejrzenie, predkoscUcieczki, nastGrunt;
    Vector3 leanOs = Vector3.right, patrzNa, odrzut, kierUcieczki;
    bool blisko = true, krzyczZDaleka;
    Transform gracz, rozmowca;
    AudioSource glos;
    static readonly List<BraxPieszy> wszyscy = new List<BraxPieszy>();

    public Vector3 Pozycja => transform.position;
    public bool MozeReagowac => isActiveAndEnabled;

    void OnEnable() { wszyscy.Add(this); BraxInterakcje.Dodaj(this); BraxLudzie.Dodaj(this); }
    void OnDisable() { wszyscy.Remove(this); BraxInterakcje.Usun(this); BraxLudzie.Usun(this); }

    void Start()
    {
        if (trasa == null || trasa.Length < 2) { trasa = new[] { a, b }; przejscie = new bool[2]; }
        if (przejscie == null || przejscie.Length < trasa.Length) { var p = new bool[trasa.Length]; if (przejscie != null) System.Array.Copy(przejscie, p, przejscie.Length); przejscie = p; }
        // start w losowym miejscu trasy (nie na jezdni), w losowym kierunku
        int s = Random.Range(0, trasa.Length - 1);
        for (int k = 0; k < 8 && przejscie[s]; k++) s = Random.Range(0, trasa.Length - 1);
        transform.position = Vector3.Lerp(trasa[s], trasa[s + 1], Random.Range(.1f, .9f));
        kier = Random.value > .5f ? 1 : -1; i = kier > 0 ? s + 1 : s;
        if (ton <= 0f) ton = 1f;
        wariant = Random.Range(0, 64);
        predkoscUcieczki = Random.Range(3.0f, 4.3f);
        krzyczZDaleka = Random.value < .35f;
        nastRozmowa = Time.time + Random.Range(5f, 25f);
        Graj(chod, 0f, predkosc / Mathf.Max(.3f, predkoscKlipu));
        if (anim && anim[chod]) anim[chod].time = Random.value * anim[chod].length;
        ostatni = Time.time;
    }

    // ---------------- animacja ----------------
    void Graj(string klip, float fade, float szybkosc = 1f)
    {
        if (!anim || string.IsNullOrEmpty(klip)) return;
        var st = anim[klip]; if (!st) return;
        st.speed = szybkosc;
        if (!anim.IsPlaying(klip)) { if (fade > 0f) anim.CrossFade(klip, fade); else anim.Play(klip); }
    }
    string Los(string[] z, string zapas) => z != null && z.Length > 0 ? z[Random.Range(0, z.Length)] : zapas;

    Vector3 Prawo(Vector3 d) { d.y = 0f; return Vector3.Cross(Vector3.up, d).normalized; }

    // ---------------- główna pętla ----------------
    void Update()
    {
        float d2 = BraxLudzie.OdlKamery2(transform.position);
        bool b2 = BraxLudzie.Animuj(d2);
        if (b2 != blisko) { blisko = b2; if (anim) anim.enabled = blisko; }
        float dt;
        if (!blisko)
        {
            if (Time.time < nast) return;
            nast = Time.time + (d2 > 150f * 150f ? 1f : .25f);
        }
        dt = Mathf.Min(Time.time - ostatni, 1.2f); ostatni = Time.time;
        if (!gracz) gracz = BraxLudzie.Gracz;
        lean = Mathf.MoveTowards(lean, leanCel, 120f * dt); leanCel = Mathf.MoveTowards(leanCel, 0f, 14f * dt);
        patrzWaga = Mathf.MoveTowards(patrzWaga, patrzCel, 2.5f * dt);

        switch (stan)
        {
            case Stan.Rozmowa: KrokRozmowa(dt); return;
            case Stan.Trafiony: KrokTrafiony(dt); return;
            case Stan.Ucieka: KrokUcieka(dt); return;
            case Stan.Patrzy: KrokPatrzy(dt); return;
            case Stan.Postoj:
                if (Time.time > doKiedy) { stan = Stan.Idzie; patrzCel = 0f; Graj(chod, .3f, predkosc / predkoscKlipu); }
                return;
            case Stan.Czeka: KrokCzeka(dt); return;
        }
        // Idzie / Wraca
        if (gracz && Time.time > nastPowitanie)
        {
            var g = gracz.position - transform.position; g.y = 0f;
            if (g.magnitude < 3f) { nastPowitanie = Time.time + 40f; if (Time.time > unikaDo && Random.value < .35f) Powiedz(BraxDymki.Los(BraxDymki.Powitania), 2.6f); }
        }
        // trafiony wcześniej: omija gracza szerokim łukiem
        if (gracz && Time.time < unikaDo)
        {
            var g = transform.position - gracz.position; g.y = 0f;
            if (g.magnitude < 3.5f) { ZacznijUcieczke(2.5f); return; }
        }
        if (stan == Stan.Idzie && Time.time > nastRozmowa) { nastRozmowa = Time.time + .6f; SzukajRozmowcy(); if (stan == Stan.Rozmowa) return; }
        Idz(dt, stan == Stan.Wraca);
    }

    Vector3 Punkt(int k)
    {
        k = Mathf.Clamp(k, 0, trasa.Length - 1);
        int prev = Mathf.Clamp(k - kier, 0, trasa.Length - 1);
        var sd = trasa[k] - trasa[prev]; if (sd.sqrMagnitude < .01f) sd = transform.forward;
        int seg = kier > 0 ? k - 1 : k;
        bool przez = seg >= 0 && seg < przejscie.Length && przejscie[seg];
        float o = (przez ? bok * .5f : bok) + (Time.time < objazdDo ? objazd : 0f);
        return trasa[k] + Prawo(sd) * o;
    }

    void Idz(float dt, bool wraca)
    {
        Vector3 cel = Punkt(i), d = cel - transform.position; d.y = 0f;
        if (d.magnitude < .35f)
        {
            if (wraca) { stan = Stan.Idzie; patrzCel = 0f; }
            // koniec trasy: zawróć (czasem z postojem)
            int nastepny = i + kier;
            if (nastepny < 0 || nastepny >= trasa.Length) { kier = -kier; nastepny = i + kier; if (Random.value < .6f) { Postoj(Random.Range(2f, 7f)); i = nastepny; return; } }
            int seg = kier > 0 ? i : i - 1;
            i = nastepny;
            if (seg >= 0 && seg < przejscie.Length && przejscie[seg]) { stan = Stan.Czeka; czekaOd = Time.time; doKiedy = Time.time + Random.Range(.6f, 1.4f); Graj(stoi, .3f); return; }
            if (Random.value < .04f) Postoj(Random.Range(3f, 9f));
            return;
        }
        var dir = d.normalized;
        // gracz na drodze: stanąć i poczekać (po chwili obejść)
        if (gracz)
        {
            var g = gracz.position - transform.position; g.y = 0f;
            if (g.magnitude < 1.8f && Vector3.Dot(g.normalized, dir) > .4f)
            {
                Graj(stoi, .25f);
                Obroc(g, dt, 3f);
                if (Time.time > objazdDo + 2f) { objazd = Random.value < .5f ? .9f : -.9f; objazdDo = Time.time + 2.5f; }
                return;
            }
        }
        // przeszkoda przed sobą (blisko kamery): objazd w bok albo postój
        if (blisko && Time.time > nastSprawdz)
        {
            nastSprawdz = Time.time + .3f;
            if (Przeszkoda(dir, 1.1f))
            {
                var r = Prawo(dir);
                if (!Zajete(transform.position + r * .7f + dir * .6f)) { objazd = .7f; objazdDo = Time.time + 1.4f; }
                else if (!Zajete(transform.position - r * .7f + dir * .6f)) { objazd = -.7f; objazdDo = Time.time + 1.4f; }
                else if (++zablokowany > 3) { zablokowany = 0; kier = -kier; i = Mathf.Clamp(i + kier, 0, trasa.Length - 1); }
                else { Postoj(Random.Range(.8f, 1.6f)); return; }
            }
            else zablokowany = 0;
        }
        float v = predkosc * (wraca ? 1.05f : 1f) * (NaPrzejsciu() ? 1.15f : 1f);
        Graj(chod, .25f, v / Mathf.Max(.3f, predkoscKlipu));
        Obroc(dir, dt, 6f);
        float krok = Mathf.Min(v * dt, d.magnitude);
        var p = transform.position + dir * krok;
        if (wraca && Time.time > nastGrunt) { nastGrunt = Time.time + .3f; p.y = Grunt(p, p.y); }
        else if (!wraca) p.y = Mathf.Lerp(p.y, cel.y, Mathf.Clamp01(dt * 4f));
        transform.position = p;
        Kroki(krok);
    }

    bool NaPrzejsciu()
    {
        int seg = kier > 0 ? i - 1 : i;
        return seg >= 0 && seg < przejscie.Length && przejscie[seg];
    }

    void Obroc(Vector3 dir, float dt, float szyb)
    {
        dir.y = 0f; if (dir.sqrMagnitude < 1e-4f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Mathf.Clamp01(dt * szyb));
    }

    void Kroki(float krok)
    {
        przeszedl += krok;
        if (przeszedl > .72f) { przeszedl = 0f; if (BraxLudzie.OdlKamery2(transform.position) < 14f * 14f) BraxDzwiekiSwiata.Krok(transform.position, stan == Stan.Ucieka ? .2f : .13f); }
    }

    void Postoj(float czas)
    {
        stan = Stan.Postoj; doKiedy = Time.time + czas;
        Graj(Los(postoje, stoi), .35f);
    }

    // przed przejściem: rozejrzeć się, poczekać na zielone / aż auto przejedzie (najwyżej ok. 6 s)
    void KrokCzeka(float dt)
    {
        int seg = kier > 0 ? i - 1 : i;
        var cel = Punkt(i) - transform.position; cel.y = 0f;
        Obroc(cel, dt, 4f);
        float t = Time.time - czekaOd;
        patrzCel = .9f; patrzNa = transform.position + Prawo(cel) * ((int)(t / .9f) % 2 == 0 ? -6f : 6f) + cel.normalized * 2f;
        if (Time.time < doKiedy || Time.time < nastSprawdz) return;
        nastSprawdz = Time.time + .3f;
        bool stop = BraxStrefyZatrzymania.CzyStop(transform.position, cel, true);
        if (!stop && t < 6f && seg >= 0 && seg < trasa.Length - 1) stop = AutoBlisko((trasa[seg] + trasa[seg + 1]) * .5f);
        if (stop && t < 25f) return;
        stan = Stan.Idzie; patrzCel = 0f;
        Graj(chod, .25f, predkosc / Mathf.Max(.3f, predkoscKlipu));
    }

    static bool AutoBlisko(Vector3 srodek)
    {
        foreach (var a in BraxAuto.Wszystkie)
        {
            if (!a) continue;
            var d = srodek - a.transform.position; d.y = 0f; float m = d.magnitude;
            if (m < 3.5f || m > 16f) continue;                       // stoi tuż przed zebrą (czeka na pieszych) albo daleko
            if (Vector3.Dot(a.transform.forward, d / m) > .55f) return true;   // jedzie w stronę przejścia
        }
        return false;
    }

    bool Przeszkoda(Vector3 dir, float dl)
    {
        if (!Physics.SphereCast(transform.position + Vector3.up * 1.0f, .22f, dir, out var hit, dl, ~0, QueryTriggerInteraction.Ignore)) return false;
        if (gracz && hit.transform.IsChildOf(gracz)) return false;
        return !hit.transform.IsChildOf(transform);
    }
    bool Zajete(Vector3 p) => Physics.CheckCapsule(p + Vector3.up * .45f, p + Vector3.up * 1.5f, .24f, ~0, QueryTriggerInteraction.Ignore) && !TylkoJa(p);
    bool TylkoJa(Vector3 p)
    {
        foreach (var c in Physics.OverlapCapsule(p + Vector3.up * .45f, p + Vector3.up * 1.5f, .24f, ~0, QueryTriggerInteraction.Ignore))
            if (!c.transform.IsChildOf(transform)) return false;
        return true;
    }
    float Grunt(Vector3 p, float y)
    {
        if (Physics.Raycast(p + Vector3.up * 1.4f, Vector3.down, out var h, 3f, ~0, QueryTriggerInteraction.Ignore) && !h.transform.IsChildOf(transform)) return h.point.y;
        return y;
    }

    // ---------------- ciosy ----------------
    public void Trafiony(Vector3 g, Vector3 kierunek, bool kop)
    {
        stan = Stan.Trafiony; rozmowca = null;
        doKiedy = Time.time + (kop ? 1.4f : 1.0f) + Random.Range(0f, .4f);
        odrzut = kierunek * (kop ? 1.4f : .8f) * Random.Range(.8f, 1.2f);
        leanOs = Vector3.Cross(Vector3.up, kierunek); leanCel = (kop ? 26f : 18f) * Random.Range(.8f, 1.2f);
        unikaDo = Time.time + 60f;
        var doGracza = g - transform.position; doGracza.y = 0f; if (doGracza.sqrMagnitude > .01f) transform.rotation = Quaternion.LookRotation(doGracza);
        if (reakcje != null && reakcje.Length > 0) { var k = reakcje[wariant % reakcje.Length]; if (anim && anim[k]) { anim[k].time = 0f; anim.CrossFade(k, .1f); anim[k].speed = 1.4f; } }
        else Graj(stoi, .1f);
        Powiedz(BraxLudzie.Los(BraxLudzie.Okrzyki), 1.8f, BraxLudzie.Krzyk(wariant));
        BraxDzwiekiSwiata.Uderzenie(transform.position + Vector3.up * wzrost * .75f, kop ? 7f : 5f, 2);
        patrzCel = 1f; patrzNa = g;
    }

    void KrokTrafiony(float dt)
    {
        if (odrzut.sqrMagnitude > 1e-4f)
        {
            var krok = odrzut * Mathf.Min(1f, dt / .3f);
            var p = transform.position + krok;
            if (!Zajete(p)) { p.y = Grunt(p, transform.position.y); transform.position = p; }
            odrzut -= krok;
        }
        if (Time.time > doKiedy) ZacznijUcieczke(Random.Range(5f, 9f));
    }

    void ZacznijUcieczke(float czas)
    {
        stan = Stan.Ucieka; doKiedy = Time.time + czas; nastObejrzenie = Time.time + Random.Range(.8f, 1.6f);
        kierUcieczki = Quaternion.Euler(0, Random.Range(-30f, 30f), 0) * Vector3.forward;   // odchylenie od kierunku „od gracza”
        patrzCel = 0f;
        string k = string.IsNullOrEmpty(bieg) ? chod : bieg;
        Graj(k, .2f, string.IsNullOrEmpty(bieg) ? predkoscUcieczki / Mathf.Max(.3f, predkoscKlipu) : predkoscUcieczki / Mathf.Max(.5f, predkoscKlipuBiegu));
    }

    void KrokUcieka(float dt)
    {
        Vector3 od = gracz ? transform.position - gracz.position : transform.forward; od.y = 0f;
        if (od.sqrMagnitude < .01f) od = transform.forward;
        var dir = Quaternion.LookRotation(od.normalized) * kierUcieczki;
        if (blisko && Time.time > nastSprawdz)
        {
            nastSprawdz = Time.time + .25f;
            if (Przeszkoda(dir, 1.6f))
                foreach (float a in new[] { 45f, -45f, 90f, -90f, 135f, -135f })
                {
                    var d2 = Quaternion.Euler(0, a, 0) * dir;
                    if (!Przeszkoda(d2, 1.6f)) { kierUcieczki = Quaternion.Euler(0, a, 0) * kierUcieczki; dir = d2; break; }
                }
        }
        // oglądanie się za siebie (sama głowa)
        if (Time.time > nastObejrzenie) { patrzCel = patrzCel > .5f ? 0f : 1f; nastObejrzenie = Time.time + (patrzCel > .5f ? .6f : Random.Range(1.2f, 2.2f)); }
        if (gracz) patrzNa = gracz.position;
        Obroc(dir, dt, 7f);
        var p = transform.position + transform.forward * predkoscUcieczki * dt;
        if (Time.time > nastGrunt) { nastGrunt = Time.time + .3f; p.y = Grunt(p, p.y); } else p.y = transform.position.y;
        transform.position = p;
        Kroki(predkoscUcieczki * dt * .7f);
        bool daleko = !gracz || od.magnitude > 20f;
        if (Time.time > doKiedy || daleko)
        {
            if (krzyczZDaleka && gracz && od.magnitude < 24f)
            {
                krzyczZDaleka = false;
                stan = Stan.Patrzy; doKiedy = Time.time + 2.2f; patrzCel = 1f; patrzNa = gracz.position;
                Graj(Los(reakcje, stoi), .3f);
                Powiedz(BraxLudzie.Los(BraxLudzie.ZDystansu), 2.2f, BraxLudzie.Krzyk(wariant + 1));
                return;
            }
            Wroc();
        }
    }

    void Wroc()
    {
        // najbliższy punkt trasy, dalej zwykły chód
        int best = 0; float bd = float.MaxValue;
        for (int k = 0; k < trasa.Length; k++) { float m = (trasa[k] - transform.position).sqrMagnitude; if (m < bd) { bd = m; best = k; } }
        i = best; kier = Random.value > .5f ? 1 : -1; if (i + kier < 0 || i + kier >= trasa.Length) kier = -kier;
        stan = Stan.Wraca; patrzCel = 0f; leanCel = 0f;
        Graj(chod, .3f, predkosc / Mathf.Max(.3f, predkoscKlipu));
    }

    public void Swiadek(Vector3 g, float odl, bool ktosTrafiony)
    {
        if (stan == Stan.Trafiony || stan == Stan.Ucieka) return;
        if (stan == Stan.Rozmowa && rozmowca == gracz) return;
        rozmowca = null;
        if (ktosTrafiony && odl < 7f && Random.value < .35f) { unikaDo = Time.time + 30f; ZacznijUcieczke(Random.Range(3f, 6f)); return; }
        stan = Stan.Patrzy; doKiedy = Time.time + Random.Range(2f, 4.5f); patrzCel = 1f; patrzNa = g;
        Graj(Los(postoje, stoi), .3f);
        if (ktosTrafiony && odl < 10f && Random.value < .3f) Powiedz(BraxLudzie.Los(BraxLudzie.Swiadkowie), 2f, BraxLudzie.Krzyk(wariant + 2));
    }

    void KrokPatrzy(float dt)
    {
        var g = patrzNa - transform.position; g.y = 0f;
        Obroc(g, dt, 3f);
        if (Time.time > doKiedy)
        {
            if (Vector3.Distance(transform.position, NajblizszyPunkt()) > 1.5f) Wroc();
            else if (NaPrzejsciu()) { stan = Stan.Czeka; czekaOd = Time.time; doKiedy = Time.time + .5f; patrzCel = 0f; }   // stał przy zebrze: znów się rozejrzy
            else { stan = Stan.Idzie; patrzCel = 0f; Graj(chod, .3f, predkosc / Mathf.Max(.3f, predkoscKlipu)); }
        }
    }

    Vector3 NajblizszyPunkt()
    {
        var best = trasa[Mathf.Clamp(i, 0, trasa.Length - 1)]; float bd = float.MaxValue;
        for (int k = 0; k < trasa.Length; k++) { float m = (trasa[k] - transform.position).sqrMagnitude; if (m < bd) { bd = m; best = trasa[k]; } }
        // odległość od odcinka (nie od samych punktów)
        for (int k = 0; k + 1 < trasa.Length; k++)
        {
            var q = Vector3.Project(transform.position - trasa[k], trasa[k + 1] - trasa[k]) + trasa[k];
            float u = Vector3.Dot(q - trasa[k], trasa[k + 1] - trasa[k]) / Mathf.Max(1e-4f, (trasa[k + 1] - trasa[k]).sqrMagnitude);
            if (u < 0f || u > 1f) continue;
            float m = (q - transform.position).sqrMagnitude; if (m < bd) { bd = m; best = q; }
        }
        return best;
    }

    // ---------------- ruchy procedur (po animacji) ----------------
    void LateUpdate()
    {
        if (!blisko) return;
        if (lean > .2f && kregoslup) kregoslup.rotation = Quaternion.AngleAxis(lean, leanOs) * kregoslup.rotation;
        if (patrzWaga > .01f && glowa)
        {
            var cel = patrzNa - glowa.position; cel.y = 0f; var przod = transform.forward; przod.y = 0f;
            if (cel.sqrMagnitude < .01f || przod.sqrMagnitude < .01f) return;
            float kat = Mathf.Clamp(Vector3.SignedAngle(przod, cel, Vector3.up), -75f, 75f) * patrzWaga;
            if (szyja) szyja.rotation = Quaternion.AngleAxis(kat * .4f, Vector3.up) * szyja.rotation;
            glowa.rotation = Quaternion.AngleAxis(kat * (szyja ? .6f : 1f), Vector3.up) * glowa.rotation;
        }
    }

    // ---------------- mowa ----------------
    void Powiedz(string tekst, float czas, AudioClip klip = null)
    {
        BraxDymki.Pokaz(transform, tekst, czas, wzrost + .4f);
        BraxLudzie.Glos(gameObject, ref glos, klip ? klip : BraxDymki.Gwar(), ton, klip ? .7f : .5f, klip ? 0f : Mathf.Min(czas * .55f, 1.1f));
    }

    // dwóch przechodniów blisko siebie (do 2.4 m), obaj wolni — czasem staną i pogadają
    void SzukajRozmowcy()
    {
        if (Random.value > .25f) return;
        foreach (var o in wszyscy)
        {
            if (o == this || o.stan != Stan.Idzie || Time.time < o.nastRozmowa - 30f) continue;
            if ((o.transform.position - transform.position).sqrMagnitude > 2.4f * 2.4f) continue;
            StartCoroutine(Rozmowa(this, o, BraxDymki.MiedzySoba[Random.Range(0, BraxDymki.MiedzySoba.Length)]));
            return;
        }
    }

    static bool Rozmawia(BraxPieszy p) => p && p.stan == Stan.Rozmowa;

    static IEnumerator Rozmowa(BraxPieszy x, BraxPieszy y, string[] kwestie)
    {
        x.stan = y.stan = Stan.Rozmowa; x.rozmowca = y.transform; y.rozmowca = x.transform;
        x.Graj(x.stoi, .3f); y.Graj(y.stoi, .3f);
        yield return new WaitForSeconds(.6f);
        for (int k = 0; k < kwestie.Length; k++)
        {
            if (!Rozmawia(x) || !Rozmawia(y)) break;
            var kto = k % 2 == 0 ? x : y; var sluch = k % 2 == 0 ? y : x;
            float czas = 1.6f + kwestie[k].Length * .045f;
            kto.Graj(kto.Los(kto.mowi, kto.stoi), .3f); sluch.Graj(sluch.Los(sluch.slucha, sluch.stoi), .3f);
            kto.Powiedz(kwestie[k], czas);
            yield return new WaitForSeconds(czas + .3f);
        }
        foreach (var p in new[] { x, y })
            if (Rozmawia(p)) { p.stan = Stan.Idzie; p.rozmowca = null; p.nastRozmowa = Time.time + Random.Range(40f, 90f); p.Graj(p.chod, .3f, p.predkosc / Mathf.Max(.3f, p.predkoscKlipu)); }
    }

    void KrokRozmowa(float dt)
    {
        if (rozmowca) { var g = rozmowca.position - transform.position; g.y = 0f; Obroc(g, dt, 4f); }
    }

    // AKCJA: POGADAJ (rejestr BraxInterakcje)
    public string Kontekst(Vector3 g)
    {
        if (!isActiveAndEnabled || stan == Stan.Rozmowa || stan == Stan.Trafiony || stan == Stan.Ucieka || Time.time < unikaDo) return "";
        var d = g - transform.position; d.y = 0f;
        return d.sqrMagnitude < 2.3f * 2.3f ? "POGADAJ" : "";
    }

    public bool Wykonaj(Vector3 g)
    {
        if (stan == Stan.Rozmowa || !gracz) return false;
        StartCoroutine(PogadajZGraczem());
        return true;
    }

    IEnumerator PogadajZGraczem()
    {
        stan = Stan.Rozmowa; rozmowca = gracz;
        string w = BraxDymki.Los(BraxDymki.Wiesci); float c1 = 2f + w.Length * .045f;
        Graj(Los(mowi, stoi), .3f);
        Powiedz(w, c1); yield return new WaitForSeconds(c1 + .2f);
        if (stan != Stan.Rozmowa) yield break;
        Graj(Los(slucha, stoi), .3f);
        string o = BraxDymki.Los(BraxDymki.OdpowiedziBraxa); BraxDymki.Pokaz(gracz, o, 2.4f, 2.55f); yield return new WaitForSeconds(2.5f);
        if (stan != Stan.Rozmowa) yield break;
        Powiedz(BraxDymki.Los(BraxDymki.Pozegnania), 2f); yield return new WaitForSeconds(1.8f);
        if (stan != Stan.Rozmowa) yield break;
        stan = Stan.Idzie; rozmowca = null; nastPowitanie = Time.time + 60f; nastRozmowa = Time.time + 30f;
        Graj(chod, .3f, predkosc / Mathf.Max(.3f, predkoscKlipu));
    }
}
