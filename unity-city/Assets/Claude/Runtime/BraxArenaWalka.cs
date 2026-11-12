using System.Collections.Generic;
using UnityEngine;

// BRAXHORN ARENA — WALKA W KLATCE (właściciel 12.11.2026 16:34: „zobacz tych dwóch, co tam walczą … trzeba ich tam zrobić,
// żeby się napierdalali, a nie stoją w miejscu”; 16:48: „są animacje T dla nich, wystarczy tylko dodać, bez kombinowania”).
//
// Dwóch zawodników z paczki globalnej postaci (BraxPostacieGlobalne) i klipy walki Mixamo z paczki właściciela „pozycja T
// animacje”, położone WPROST na ten sam szkielet (bez mostu Humanoid, który w obozie deformował postacie).
//
// To nie jest zegar z ustaloną choreografią: walka toczy się wymianami. Atakujący wybiera cios, podchodzi na taki dystans,
// żeby pięść / stopa doszła do celu dokładnie w klatce uderzenia (zasięg każdego ciosu zmierzony na plikach źródłowych —
// ułamek wzrostu od osi postaci), broniący przyjmuje (reakcja + odrzut), blokuje albo schodzi krokiem w tył i kontruje.
// Między wymianami krążą po macie krokiem dostawnym i trzymają dystans. Cała para zostaje w klatce.
public sealed class BraxArenaWalka : MonoBehaviour
{
    public Transform zawodnikA, zawodnikB;
    public Animation animacjaA, animacjaB;
    public Vector3 srodek;
    public float promien = 3.6f;     // promień maty, w którym trzymają się zawodnicy (m)
    public float wzrost = 2.33f;     // wzrost zawodników w grze (m) — skala dystansów
    public float zasieg = 70f;       // dalej od kamery walka stoi (telefon)
    public int ziarno = 20261112;

    // liczniki do odbioru (log co 30 s)
    public int trafieniaA, trafieniaB, bloki, uniki, wymiany;

    struct Cios { public float t, f; public bool glowa; public int sila; public Cios(float t, float f, bool glowa, int sila) { this.t = t; this.f = f; this.glowa = glowa; this.sila = sila; } }
    sealed class Atak { public string klip; public float dl, tempo; public bool kop; public Cios[] ciosy; }
    static Atak A(string klip, float dl, float tempo, bool kop, params Cios[] c) => new Atak { klip = klip, dl = dl, tempo = tempo, kop = kop, ciosy = c };
    static Cios G(float t, float f, int sila = 1) => new Cios(t, f, true, sila);
    static Cios K(float t, float f, int sila = 1) => new Cios(t, f, false, sila);

    // t = moment uderzenia (ułamek klipu), f = zasięg końcówki przed oś postaci (ułamek wzrostu) — z analizy plików źródłowych
    static readonly Atak[] Ataki =
    {
        A("JabCross", 1.95f, 1.25f, false, G(.20f, .41f), G(.34f, .47f, 2)),
        A("JabCross2", 2.12f, 1.25f, false, G(.22f, .48f), G(.33f, .57f, 2)),
        A("Cross", 2.00f, 1.45f, false, G(.57f, .37f, 2)),
        A("Seria", 2.18f, 1.2f, false, G(.29f, .34f), G(.39f, .33f), G(.52f, .33f), G(.62f, .29f, 2)),
        A("SeriaDluga", 2.95f, 1.2f, false, G(.12f, .29f), G(.21f, .31f), G(.33f, .27f), G(.42f, .28f), K(.56f, .30f), K(.64f, .28f), K(.73f, .28f), K(.80f, .28f, 2)),
        A("Jab", 1.33f, 1.2f, false, G(.30f, .45f)),
        A("Korpus", 1.37f, 1.2f, false, K(.27f, .38f), K(.44f, .35f, 2)),
        A("Prosty", 1.25f, 1.15f, false, G(.45f, .37f)),
        A("Sierp", 2.20f, 1.35f, false, G(.46f, .34f, 2)),
        A("Dol", 2.18f, 1.3f, false, K(.32f, .49f, 2), G(.44f, .38f)),
        A("Kop", 1.58f, 1.15f, true, K(.42f, .73f, 3)),
        A("KopWysoki", 1.67f, 1.15f, true, G(.46f, .61f, 3)),
        A("KopFront", 2.15f, 1.3f, true, K(.58f, .43f, 2)),
        A("KopPchniecie", 1.35f, 1.1f, true, K(.46f, .68f, 3)),
        A("KopOkrezny", 2.50f, 1.3f, true, K(.44f, .78f, 3)),
        A("KopBoczny", 2.00f, 1.25f, true, K(.38f, .69f, 3)),
    };
    static readonly string[] Wymagane = { "Garda", "KrokP", "KrokT", "KrokB", "JabCross", "Glowa", "GlowaLekko", "Brzuch", "Blok" };
    // długości klipów pomocniczych (s) i przesunięcie korzenia w klipie źródłowym (ułamek wzrostu) — ruch korzenia robi skrypt
    const float DlKrokP = 1.00f, DlKrokT = .90f, DlKrokB = .83f, DlZaczepka = 1.63f;
    const float PrzKrokP = .29f, PrzKrokT = .18f, PrzKrokB = .20f;
    const float MarginesPiesc = .16f, MarginesKop = .06f;

    sealed class Zaw
    {
        public Transform t; public Animation an; public Vector3 p; public string klip = ""; public float koniec, blokDo;
        public Vector3 ruch; public float ruchDo;     // prędkość korzenia (m/s) do chwili ruchDo
        public Vector3 odrzut; public float odrzutDo; // odrzut po trafieniu
    }
    struct Zdarzenie { public float czas; public int typ, kto, a; public float f; }   // typ: 0 trafienie, 1 klip, 2 koniec wymiany
    readonly Zaw[] z = new Zaw[2];
    readonly List<Zdarzenie> kolejka = new List<Zdarzenie>();
    readonly List<string>[] bezKlipu = { new List<string>(), new List<string>() };
    System.Random los;
    Transform kamera;
    float czas, nastepna, krazenieDo, logDo = 30f, zaczepkaPo = 22f;
    int ostatni = -1, zRzedu;
    bool gotowe, wWymianie;

    void Awake()
    {
        if (!zawodnikA || !zawodnikB || !animacjaA || !animacjaB)
        {
            Debug.LogError("BRAXHORN_ARENA_WALKA brak obu zawodników lub animacji", this); enabled = false; return;
        }
        foreach (string n in Wymagane)
            if (!animacjaA.GetClip(n) || !animacjaB.GetClip(n))
            {
                Debug.LogError("BRAXHORN_ARENA_WALKA brakuje klipu " + n, this); enabled = false; return;
            }
        los = new System.Random(ziarno);
        z[0] = new Zaw { t = zawodnikA, an = animacjaA }; z[1] = new Zaw { t = zawodnikB, an = animacjaB };
        float d = .72f * wzrost;
        z[0].p = srodek + Vector3.back * (d * .5f); z[1].p = srodek + Vector3.forward * (d * .5f);
        for (int i = 0; i < 2; i++)
        {
            foreach (AnimationState s in z[i].an) { s.wrapMode = s.name == "Garda" ? WrapMode.Loop : WrapMode.ClampForever; s.layer = 0; }
            z[i].an.playAutomatically = false; z[i].an.cullingType = AnimationCullingType.AlwaysAnimate;
            Graj(i, "Garda", 1f, .0f, 9999f);
        }
        Ustaw();
        nastepna = 1.2f; gotowe = true;
        Debug.Log("BRAXHORN_ARENA_WALKA start: " + zawodnikA.name + " kontra " + zawodnikB.name + ", ataki=" + Ataki.Length + ", wzrost=" + wzrost.ToString("F2") + " m, promień maty=" + promien.ToString("F1") + " m");
    }

    bool Ma(int i, string klip) => z[i].an.GetClip(klip);
    float Los(float a, float b) => a + (float)los.NextDouble() * (b - a);

    // klip od początku w zadanym tempie; doKonca = po ilu sekundach wraca garda (0 = długość klipu)
    void Graj(int i, string klip, float tempo, float przejscie, float doKonca)
    {
        var s = z[i].an[klip];
        if (s == null) { if (!bezKlipu[i].Contains(klip)) bezKlipu[i].Add(klip); return; }
        s.time = 0f; s.speed = tempo;
        if (przejscie <= 0f) z[i].an.Play(klip); else z[i].an.CrossFade(klip, przejscie);
        z[i].klip = klip; z[i].koniec = czas + (doKonca > 0f ? doKonca : s.length / Mathf.Max(.1f, tempo));
    }

    void Zaplanuj(float kiedy, int typ, int kto, int a = 0, float f = 0f) => kolejka.Add(new Zdarzenie { czas = kiedy, typ = typ, kto = kto, a = a, f = f });

    void Update()
    {
        if (!gotowe) return;
        if (!kamera && Camera.main) kamera = Camera.main.transform;
        bool blisko = kamera && (kamera.position - srodek).sqrMagnitude < zasieg * zasieg;
        if (animacjaA.enabled != blisko) { animacjaA.enabled = blisko; animacjaB.enabled = blisko; }
        if (!blisko) return;

        float dt = Mathf.Min(Time.deltaTime, .05f); czas += dt;

        // zdarzenia (trafienia, klipy obrony, koniec wymiany)
        for (int q = kolejka.Count - 1; q >= 0; q--)
        {
            var e = kolejka[q]; if (e.czas > czas) continue;
            kolejka.RemoveAt(q);
            if (e.typ == 0) Trafienie(e.kto, e.a, e.f);
            else if (e.typ == 1) Obrona(e.kto, e.a, e.f);
            else { wWymianie = false; nastepna = czas + Los(.25f, .8f); }
        }
        // powrót do gardy po skończonym klipie
        for (int i = 0; i < 2; i++) if (z[i].klip != "Garda" && czas >= z[i].koniec) Graj(i, "Garda", 1f, .2f, 9999f);

        if (!wWymianie && czas >= nastepna) Decyzja();

        // ruch korzeni: krok / doskok, odrzut po trafieniu
        for (int i = 0; i < 2; i++)
        {
            if (czas < z[i].ruchDo) z[i].p += z[i].ruch * dt;
            if (czas < z[i].odrzutDo) z[i].p += z[i].odrzut * dt;
        }
        Ustaw();

        if (czas >= logDo)
        {
            logDo = czas + 30f;
            Debug.Log("BRAXHORN_ARENA_WALKA wymiany=" + wymiany + " trafienia A=" + trafieniaA + " B=" + trafieniaB + " bloki=" + bloki + " uniki=" + uniki
                + (bezKlipu[0].Count + bezKlipu[1].Count > 0 ? " brak_klipow=" + string.Join(",", bezKlipu[0]) + "|" + string.Join(",", bezKlipu[1]) : ""));
        }
    }

    Vector3 Os() { var o = z[1].p - z[0].p; o.y = 0f; return o.sqrMagnitude > 1e-6f ? o.normalized : Vector3.forward; }
    float Dystans() { var o = z[1].p - z[0].p; o.y = 0f; return o.magnitude; }

    // pozycje na macie: para w klatce, minimalny odstęp, twarzą do siebie
    void Ustaw()
    {
        var os = Os(); float d = Dystans(), min = .30f * wzrost;
        if (d < min) { var mid0 = (z[0].p + z[1].p) * .5f; z[0].p = mid0 - os * (min * .5f); z[1].p = mid0 + os * (min * .5f); }
        // środek pary nie wychodzi poza matę (przesuwamy oboje naraz — układ ciosu się nie zmienia)
        var mid = (z[0].p + z[1].p) * .5f; var odSrodka = mid - srodek; odSrodka.y = 0f;
        float maks = Mathf.Max(.2f, promien - Dystans() * .5f - .35f * wzrost);
        if (odSrodka.magnitude > maks) { var kor = odSrodka.normalized * (odSrodka.magnitude - maks); z[0].p -= kor; z[1].p -= kor; }
        for (int i = 0; i < 2; i++)
        {
            z[i].p.y = srodek.y;
            z[i].t.position = z[i].p;
            z[i].t.rotation = Quaternion.LookRotation(i == 0 ? os : -os, Vector3.up);
        }
    }

    // ---------------- decyzje między wymianami ----------------
    void Decyzja()
    {
        float d = Dystans(), neutralny = .72f * wzrost;
        // co jakiś czas zaczepka (jeden prowokuje, drugi krąży)
        if (czas > zaczepkaPo && Ma(0, "Zaczepka") && Ma(1, "Zaczepka"))
        {
            zaczepkaPo = czas + Los(24f, 38f);
            int kto = los.Next(2);
            Graj(kto, "Zaczepka", 1f, .15f, DlZaczepka);
            Krok(1 - kto, "KrokT", DlKrokT, 1.1f, -PrzKrokT);
            nastepna = czas + DlZaczepka + Los(.1f, .4f); return;
        }
        // za daleko → krok do przodu; za blisko → krok w tył; czasem krążenie
        if (d > neutralny + .22f * wzrost) { int kto = los.Next(2); Krok(kto, "KrokP", DlKrokP, 1.25f, Mathf.Min(PrzKrokP * 1.3f, (d - neutralny) / wzrost)); nastepna = czas + DlKrokP / 1.25f * .8f; return; }
        if (d < neutralny - .22f * wzrost) { int kto = los.Next(2); Krok(kto, "KrokT", DlKrokT, 1.25f, -PrzKrokT); nastepna = czas + DlKrokT / 1.25f * .8f; return; }
        if (czas > krazenieDo && los.NextDouble() < .45)
        {
            krazenieDo = czas + Los(1.5f, 4f);
            Krazenie(); nastepna = czas + DlKrokB / 1.15f * .9f; return;
        }
        Wymiana();
    }

    // krok w osi walki: przod > 0 do przeciwnika, < 0 od niego (ułamek wzrostu); klip w miejscu, korzeń przesuwa skrypt w tempie klipu
    void Krok(int i, string klip, float dl, float tempo, float przod)
    {
        if (!Ma(i, klip)) return;
        float t = dl / tempo; var os = Os(); var kier = i == 0 ? os : -os;
        Graj(i, klip, tempo, .12f, t);
        z[i].ruch = kier * (przod * wzrost / t); z[i].ruchDo = czas + t;
    }

    // krok dostawny w lewo obu naraz — para obraca się wokół swojego środka
    void Krazenie()
    {
        float tempo = 1.15f, t = DlKrokB / tempo;
        var os = Os();
        for (int i = 0; i < 2; i++)
        {
            if (!Ma(i, "KrokB")) continue;
            var przod = i == 0 ? os : -os; var lewo = Vector3.Cross(przod, Vector3.up);   // lewa strona zawodnika
            Graj(i, "KrokB", tempo, .12f, t);
            z[i].ruch = lewo * (PrzKrokB * wzrost / t); z[i].ruchDo = czas + t;
        }
    }

    // ---------------- wymiana ciosów ----------------
    void Wymiana()
    {
        int kto = los.Next(2);
        if (kto == ostatni && ++zRzedu >= 2) { kto = 1 - kto; zRzedu = 0; } else if (kto != ostatni) zRzedu = 0;
        Atakuj(kto, false);
    }

    void Atakuj(int kto, bool kontra)
    {
        ostatni = kto; int cel = 1 - kto;
        Atak at = null;
        for (int proba = 0; proba < 8 && at == null; proba++) { var k = Ataki[los.Next(Ataki.Length)]; if (Ma(kto, k.klip) && !(kontra && k.dl / k.tempo > 1.9f)) at = k; }
        if (at == null) { nastepna = czas + .5f; return; }
        wWymianie = true; wymiany++;
        float trwa = at.dl / at.tempo, t1 = at.ciosy[0].t * trwa;
        float fmax = 0f; foreach (var c in at.ciosy) fmax = Mathf.Max(fmax, c.f);
        // dystans, przy którym końcówka dochodzi do celu, a nie przebija go: zasięg + margines na pięść i głębokość tułowia przeciwnika
        // (sprawdzone na podglądzie wymian z plików źródłowych: pięści .16, stopy .06 wzrostu)
        float cel_d = (fmax + (at.kop ? MarginesKop : MarginesPiesc)) * wzrost, d = Dystans();
        var os = Os(); var doCelu = kto == 0 ? os : -os;

        // wynik obrony losowany na początku: trafiony / blok / zejście w tył (kontra po udanej obronie)
        double r = los.NextDouble(); int wynik = r < .52 ? 0 : r < .82 ? 1 : 2;
        if (wynik == 2 && !Ma(cel, "KrokT")) wynik = 1;

        Graj(kto, at.klip, at.tempo, .12f, trwa);
        // doskok: atakujący wyrównuje dystans do chwili pierwszego ciosu (najwyżej 1.7 wzrostu na sekundę)
        float roznica = d - cel_d, czasDoskoku = Mathf.Max(.12f, t1 - .03f), v = Mathf.Clamp(roznica / czasDoskoku, -1.7f * wzrost, 1.7f * wzrost);
        z[kto].ruch = doCelu * v; z[kto].ruchDo = czas + czasDoskoku;

        if (wynik == 0)
        {
            for (int j = 0; j < at.ciosy.Length; j++) Zaplanuj(czas + at.ciosy[j].t * trwa - .04f, 0, cel, (at.ciosy[j].glowa ? 1 : 0) + at.ciosy[j].sila * 2, 0f);
        }
        else if (wynik == 1)
        {
            // blok gotowy ~0.35 s przed pierwszym ciosem i trzymany do ostatniego; ostatni mocny cios serii czasem przechodzi
            float tOst = at.ciosy[at.ciosy.Length - 1].t * trwa;
            Zaplanuj(czas + Mathf.Max(0f, t1 - .38f), 1, cel, at.ciosy.Length > 2 ? 1 : 0, tOst - Mathf.Max(0f, t1 - .38f) + .25f);
            bloki++;
            if (at.ciosy.Length > 2 && los.NextDouble() < .45) { var c = at.ciosy[at.ciosy.Length - 1]; Zaplanuj(czas + c.t * trwa - .04f, 0, cel, (c.glowa ? 1 : 0) + c.sila * 2, 0f); }
        }
        else
        {
            Zaplanuj(czas + Mathf.Max(0f, t1 - .42f), 1, cel, 2, 0f);
            uniki++;
        }
        // koniec wymiany; po udanej obronie często natychmiastowa kontra
        bool kontruj = wynik != 0 && !kontra && los.NextDouble() < .6;
        if (kontruj) Zaplanuj(czas + trwa * .82f, 1, cel, 3, 0f);
        else Zaplanuj(czas + trwa + .05f, 2, 0);
    }

    // a: bit 0 = głowa, reszta = siła × 2
    void Trafienie(int cel, int a, float f)
    {
        bool glowa = (a & 1) == 1; int sila = Mathf.Max(1, a >> 1);
        if (czas < z[cel].blokDo && sila < 3) { Odrzut(cel, .012f); return; }   // w bloku — tylko lekki odrzut
        string klip; float tempo = 1f;
        if (glowa) klip = sila >= 3 && Ma(cel, "GlowaMocno") ? "GlowaMocno" : sila == 2 && Ma(cel, "GlowaSrednio") ? "GlowaSrednio" : (los.Next(2) == 0 || !Ma(cel, "GlowaLekko")) ? "Glowa" : "GlowaLekko";
        else { klip = sila >= 3 || !Ma(cel, "Bok") ? "Brzuch" : los.Next(2) == 0 ? "Bok" : "Brzuch"; tempo = sila >= 3 ? 1f : 1.35f; }
        Graj(cel, klip, tempo, .06f, 0f);
        Odrzut(cel, .025f + .03f * sila);
        if (cel == 0) trafieniaB++; else trafieniaA++;
    }

    void Odrzut(int cel, float ulamek)
    {
        var os = Os(); var od = cel == 0 ? -os : os;
        z[cel].odrzut = od * (ulamek * wzrost / .22f); z[cel].odrzutDo = czas + .22f;
    }

    // a: 0 blok pojedynczy, 1 blok długi (seria), 2 zejście w tył, 3 kontra
    void Obrona(int kto, int a, float f)
    {
        if (a == 3) { Atakuj(kto, true); return; }
        if (a == 2) { Krok(kto, "KrokT", DlKrokT, 1.35f, -(PrzKrokT + .12f)); return; }
        string klip = a == 1 && Ma(kto, "BlokDlugi") ? "BlokDlugi" : (los.Next(3) == 0 && Ma(kto, "BlokL")) ? "BlokL" : (los.Next(2) == 0 && Ma(kto, "BlokP")) ? "BlokP" : "Blok";
        var s = z[kto].an[klip]; float dl = s != null ? s.length : 1.5f;
        float trzymaj = Mathf.Max(f, .5f);
        // klip bloku dopasowany tempem do czasu, przez który trzeba trzymać ręce w górze (ręce są wysoko od ~15 % do ~85 % klipu)
        float tempo = Mathf.Clamp(dl * .8f / (trzymaj + .35f), .7f, 1.8f);
        Graj(kto, klip, tempo, .1f, dl / tempo);
        z[kto].blokDo = czas + .35f + trzymaj;
    }

    void OnDisable()
    {
        if (animacjaA) animacjaA.Stop();
        if (animacjaB) animacjaB.Stop();
    }
}
