using System;
using System.Collections.Generic;
using UnityEngine;

// RUCH AUT — RDZEŃ SYMULACJI (build 14, 2.11; zdjęcia właściciela z buildu 12: auta jeżdżą tyłem, obracają się w miejscu,
// zakleszczają przy rondzie, nachodzą na siebie). Czysta symulacja bez komponentów Unity — te same obliczenia w grze
// (BraxRuchAut), w teście przy buildzie w CI (BRAXHORN_RUCH_TEST) i w teście lokalnym poza Unity.
//  • Trasa = gęsta linia (co ~0,5 m) środkiem prawego pasa; łuki na skrzyżowaniach, rondo przeciwnie do wskazówek zegara.
//  • Auto = model rowerowy: tylna oś na linii trasy w punkcie s, przednia oś na linii o rozstaw osi dalej — nadwozie
//    ustawione wzdłuż cięciwy (auto skręca jak prawdziwe, nie obraca się w miejscu); kąt skrętu przednich kół z różnicy
//    kierunku nadwozia i kierunku trasy pod przednią osią.
//  • Prędkość: profil z krzywizny trasy (zwalnia przed łukiem i rondem, przyspiesza po), czujnik auta z przodu (odstęp),
//    bramki pierwszeństwa: rondo — wjeżdżający ustępuje jadącym po rondzie; skrzyżowanie — prawa ręka, skręcający w lewo
//    ustępuje jadącym z naprzeciwka, nie wjeżdża, jeśli nie zdąży zjechać; strefy BraxStrefyZatrzymania (światła,
//    przejścia); gracz na jezdni — hamuje i trąbi.
//  • Twarde zabezpieczenie: krok, po którym auto nachodziłoby na inne, nie jest wykonywany (auta nigdy nie nachodzą).
//  • Zaklinowane > 10 s → znika i pojawia się w innym miejscu trasy poza widokiem kamery; koniec trasy otwartej (tunel,
//    ślepa ulica osiedla B) → nowy przejazd od początku innej trasy otwartej.
[Serializable] public struct BraxTrasaAut { public string nazwa; public int start, ile, obszar; public bool petla; public float dl, krok; }
// typ: 0 skrzyżowanie (s0 linia zatrzymania — przód, s1 wyjazd ze skrzyżowania — tył), 1 rondo (s0 linia ustąpienia,
// s1 zjazd z pierścienia; kąty wjazdu/zjazdu na pierścieniu w stopniach), 2 kierunkowskaz (kier −1 lewy, +1 prawy),
// 3 koniec trasy otwartej (kier 1 = koniec ukryty, np. w tunelu)
[Serializable] public struct BraxZdarzenieAut { public int trasa, typ, wezel, zwrot, kier; public float s0, s1, katWjazdu, katZjazdu; public Vector3 wlot; }
// rodzaj: 0 zwykłe (prawa ręka), 1 rondo, 2 ze światłami (bez prawej ręki — decydują strefy świateł)
[Serializable] public struct BraxWezelAut { public Vector3 srodek; public int rodzaj; }

public sealed class BraxRuchAutSym
{
    public const int Z_WEZEL = 0, Z_RONDO = 1, Z_MIGACZ = 2, Z_KONIEC = 3;
    public const int P_WOLNA = 0, P_AUTO = 1, P_USTEP = 2, P_STREFA = 3, P_GRACZ = 4, P_KONIEC = 5;
    const float Ham = 2.6f;          // planowe hamowanie [m/s²]
    const float PrzyspDuze = 2.4f, PrzyspMale = 1.5f;
    public static float RondoWlaczony = 10.6f;   // środek auta bliżej środka ronda = już na pierścieniu

    public sealed class Auto
    {
        public int id, trasa, obszar;
        public float s, v, a, vCel, zapas, vmax;
        public float dl, szer, rozstaw, osTyl, przodOd, tylOd;
        public Vector3 poz, kier = Vector3.forward;
        public float skret;
        public int powod, lider = -1, bramka = -1, rez = -1, migacz, blokada = -1, rondo = -1;
        public float katWj, katZj; public int czekaNa = -1; public string czekaCzemu; public bool czekaPierwsz;
        public float tStrefa; public bool zolte;
        public bool uprawnione, hamuje, klakson, przestawiony, wTunelu;
        public float tStoi, tCzeka, tGracz, tKlakson, tRez;
        public bool klaksonUzytyPrzyBlokadzie;  // jedno ostrzeżenie na zdarzenie, nie pętla co 6 sekund
        internal float vPrev;
    }

    readonly Vector3[] pkt; readonly BraxTrasaAut[] tr; readonly BraxZdarzenieAut[] zd; readonly BraxWezelAut[] wz;
    readonly float[][] vProf;
    readonly int[][] wezlyTrasy, migaczeTrasy;
    readonly int[] koniecTrasy;
    readonly HashSet<long> konflikty = new HashSet<long>();
    readonly List<int>[] wSrodku;      // per węzeł: indeksy aut w środku (albo z rezerwacją tuż przed linią)
    readonly List<int>[] wSrodkuZd;    // równolegle: zdarzenie, przez które auto jest w węźle
    public readonly List<Auto> auta = new List<Auto>();
    public Action<string> naZakleszczenie;   // opis zakleszczenia (log CI)
    public Func<Vector3, bool> widoczne;   // kamera (null = test bez kamery: nic nie jest widoczne)
    public bool graczJest; public Vector3 gracz;
    public struct ObrysZewnetrzny { public Vector3 srodek, przod; public float polSzer, polDl; }
    public readonly List<ObrysZewnetrzny> zewnetrzne = new List<ObrysZewnetrzny>();
    // Scene adapter: static geometry, parked vehicles and pedestrians. Null in pure route tests.
    public Func<Vector3, Vector3, float, float, bool> przeszkodaSceny;
    public bool ZajeteZewnetrznie(Vector3 p, Vector3 f, float hw, float hl)
    {
        foreach (var b in zewnetrzne)
            if (Mathf.Abs(p.y-b.srodek.y)<3f && Prost(p,f,hw,hl,b.srodek,b.przod,b.polSzer,b.polDl)) return true;
        return przeszkodaSceny != null && przeszkodaSceny(p,f,hw,hl);
    }
    public bool pytajStrefy = true;
    public int zakleszczenia, przestawienia, nowePrzejazdy, przejazdyWezlow;
    public float czas;
    readonly System.Random rnd;
    float akum = 1f;
    int ostatniKolizyjny = -1;
    public float krokLogiki = .1f;
    readonly Vector3[] probki = new Vector3[48];

    readonly float szerAut, dlAut;   // największe wymiary aut (obrysy przy liczeniu konfliktów i linii zatrzymania)
    // E3d/e (runda 4): prędkość w zakręcie (łuk o promieniu < 40 m) nie większa niż vZakret — 15 km/h × K (0 = tylko z przyspieszenia bocznego)
    readonly float vZakret;
    public BraxRuchAutSym(Vector3[] punkty, BraxTrasaAut[] trasy, BraxZdarzenieAut[] zdarzenia, BraxWezelAut[] wezly, int ziarno, float szerMax = 2.05f, float dlMax = 5f, float vZakret = 0f)
    {
        this.vZakret = vZakret;
        szerAut = Mathf.Max(szerMax, 1.6f); dlAut = Mathf.Max(dlMax, 3.5f);
        pkt = punkty ?? new Vector3[0]; tr = trasy ?? new BraxTrasaAut[0]; wz = wezly ?? new BraxWezelAut[0];
        zd = zdarzenia != null ? (BraxZdarzenieAut[])zdarzenia.Clone() : new BraxZdarzenieAut[0];   // kopia — linie zatrzymania są tu przesuwane
        rnd = new System.Random(ziarno);
        vProf = new float[tr.Length][]; wezlyTrasy = new int[tr.Length][]; migaczeTrasy = new int[tr.Length][]; koniecTrasy = new int[tr.Length];
        for (int r = 0; r < tr.Length; r++)
        {
            var w = new List<int>(); var m = new List<int>(); koniecTrasy[r] = -1;
            for (int i = 0; i < zd.Length; i++)
            {
                if (zd[i].trasa != r) continue;
                if (zd[i].typ == Z_WEZEL || zd[i].typ == Z_RONDO) w.Add(i); else if (zd[i].typ == Z_MIGACZ) m.Add(i); else if (zd[i].typ == Z_KONIEC) koniecTrasy[r] = i;
            }
            w.Sort((a, b) => zd[a].s0.CompareTo(zd[b].s0));
            wezlyTrasy[r] = w.ToArray(); migaczeTrasy[r] = m.ToArray();
            vProf[r] = Profil(r);
        }
        wSrodku = new List<int>[wz.Length]; wSrodkuZd = new List<int>[wz.Length];
        for (int n = 0; n < wz.Length; n++) { wSrodku[n] = new List<int>(); wSrodkuZd[n] = new List<int>(); }
        CofnijLinie();
        Konflikty();
    }

    // ---------------- geometria trasy ----------------
    static float Rep(float x, float m) { x %= m; return x < 0f ? x + m : x; }
    public float Dl(int r) => tr[r].dl;
    public bool Petla(int r) => tr[r].petla;
    public int Trasy => tr.Length;

    public Vector3 P(int r, float s)
    {
        var t = tr[r];
        if (t.petla)
        {
            s = Rep(s, t.dl); float f = s / t.krok; int i = (int)f; if (i >= t.ile) i = t.ile - 1; float u = f - i;
            return Vector3.Lerp(pkt[t.start + i], pkt[t.start + (i + 1) % t.ile], u);
        }
        if (s <= 0f) { var d = pkt[t.start + 1] - pkt[t.start]; return pkt[t.start] + d.normalized * s; }
        if (s >= t.dl) { var a = pkt[t.start + t.ile - 1]; var d = a - pkt[t.start + t.ile - 2]; return a + d.normalized * (s - t.dl); }
        { float f = s / t.krok; int i = Math.Min((int)f, t.ile - 2); float u = f - i; return Vector3.Lerp(pkt[t.start + i], pkt[t.start + i + 1], u); }
    }
    public Vector3 T(int r, float s)
    {
        var d = P(r, s + .6f) - P(r, s - .6f); d.y = 0f;
        return d.sqrMagnitude > 1e-8f ? d.normalized : Vector3.forward;
    }
    // względna odległość wzdłuż trasy (pętla: najkrótsza w przód/tył)
    float Rel(int r, float x) { if (!tr[r].petla) return x; float L = tr[r].dl; x = Rep(x, L); return x > L * .5f ? x - L : x; }

    // Profil prędkości: z krzywizny (przyspieszenie boczne ≤ 2,1 m/s²), potem wstecz — hamowanie przed łukiem z wyprzedzeniem.
    float[] Profil(int r)
    {
        var t = tr[r]; int n = t.ile; var v = new float[n];
        for (int i = 0; i < n; i++)
        {
            float s = i * t.krok;
            var a = T(r, s - 2.5f); var b = T(r, s + 2.5f);
            float kat = Vector3.Angle(a, b) * Mathf.Deg2Rad; float kr = kat / 5f;
            v[i] = kr < 1e-4f ? 16f : Mathf.Min(16f, Mathf.Sqrt(2.1f / kr));
            if (vZakret > 0f && kr > 1f / 40f) v[i] = Mathf.Min(v[i], vZakret * .95f);   // zakręt: ~15 km/h × K (5 % zapasu na opóźnienie regulatora)
        }
        int przebiegi = t.petla ? 2 : 1;
        for (int p = 0; p < przebiegi; p++)
            for (int i = n - 1; i >= 0; i--)
            {
                int j = i + 1; if (j >= n) { if (!t.petla) continue; j = 0; }
                v[i] = Mathf.Min(v[i], Mathf.Sqrt(v[j] * v[j] + 2f * 1.8f * t.krok));
            }
        return v;
    }
    float VProf(int r, float s)
    {
        var t = tr[r]; int i = (int)Math.Round((t.petla ? Rep(s, t.dl) : Mathf.Clamp(s, 0f, t.dl)) / t.krok);
        if (i >= t.ile) i = t.petla ? 0 : t.ile - 1;
        return vProf[r][i];
    }

    // Konflikty ruchów na tym samym skrzyżowaniu: obrysy auta (5 × 2,05 m, model rowerowy jak w jeździe) wzdłuż obu
    // torów — od przodu na linii zatrzymania do tyłu za wyjazdem — gdzieś się przecinają, a auta wjeżdżają z różnych
    // wlotów (ten sam wlot — kolejność załatwia czujnik odstępu).
    void Konflikty()
    {
        var lista = new List<int>[wz.Length];
        for (int i = 0; i < zd.Length; i++) if (zd[i].typ == Z_WEZEL) { if (lista[zd[i].wezel] == null) lista[zd[i].wezel] = new List<int>(); lista[zd[i].wezel].Add(i); }
        float dl = dlAut, szer = szerAut, wb = dlAut * .58f, osT = -dlAut * .28f;
        float przod = dl * .5f - osT, tyl = dl * .5f + osT;
        var obrysy = new Dictionary<int, List<(Vector3 c, Vector3 f)>>();
        List<(Vector3 c, Vector3 f)> Obrys(int e)
        {
            if (obrysy.TryGetValue(e, out var l)) return l;
            l = new List<(Vector3, Vector3)>(); var z = zd[e];
            float s1 = z.s1; if (tr[z.trasa].petla && s1 < z.s0) s1 += tr[z.trasa].dl;
            for (float s = z.s0 - przod; s <= s1 + tyl + 1.5f; s += .5f)
            {
                var r = P(z.trasa, s); var f = P(z.trasa, s + wb) - r; f.y = 0f; f = f.sqrMagnitude > 1e-6f ? f.normalized : T(z.trasa, s);
                l.Add((r - f * osT, f));
            }
            obrysy[e] = l; return l;
        }
        foreach (var l in lista)
        {
            if (l == null) continue;
            for (int i = 0; i < l.Count; i++)
                for (int j = i + 1; j < l.Count; j++)
                {
                    int a = l[i], b = l[j];
                    if (Vector3.Dot(zd[a].wlot, zd[b].wlot) > .9f) continue;
                    var oa = Obrys(a); var ob = Obrys(b); bool k = false;
                    foreach (var p in oa)
                    {
                        foreach (var q in ob)
                        {
                            float dx = p.c.x - q.c.x, dz = p.c.z - q.c.z;
                            if (dx * dx + dz * dz > (dl + .2f) * (dl + .2f) + (szer + .2f) * (szer + .2f)) continue;
                            if (Prost(p.c, p.f, szer * .5f + .1f, dl * .5f + .1f, q.c, q.f, szer * .5f + .1f, dl * .5f + .1f)) { k = true; break; }
                        }
                        if (k) break;
                    }
                    if (k) { konflikty.Add((long)a * 100000 + b); konflikty.Add((long)b * 100000 + a); }
                }
        }
    }
    // Linia zatrzymania cofana (do 4 m), dopóki auto czekające przed nią stoi w drodze skręcającym z innych wlotów
    // (np. przy ciasnym skręcie w prawo z szerokiej ulicy w węższą — tył i przód skręcającego zahaczałyby o czekającego).
    void CofnijLinie()
    {
        float dl = dlAut, szer = szerAut + .1f, wb = dlAut * .58f, osT = -dlAut * .28f;
        float przod = dl * .5f - osT, tyl = dl * .5f + osT;
        (Vector3 c, Vector3 f) Poz(int r, float s)
        {
            var a = P(r, s); var f = P(r, s + wb) - a; f.y = 0f; f = f.sqrMagnitude > 1e-6f ? f.normalized : T(r, s);
            return (a - f * osT, f);
        }
        for (int e = 0; e < zd.Length; e++)
        {
            if (zd[e].typ != Z_WEZEL) continue;
            for (int krok = 0; krok < 8; krok++)
            {
                var w = Poz(zd[e].trasa, zd[e].s0 - przod); bool kol = false;
                for (int f = 0; f < zd.Length && !kol; f++)
                {
                    if (f == e || zd[f].typ != Z_WEZEL || zd[f].wezel != zd[e].wezel || Vector3.Dot(zd[f].wlot, zd[e].wlot) > .9f) continue;
                    float s1 = zd[f].s1; if (tr[zd[f].trasa].petla && s1 < zd[f].s0) s1 += tr[zd[f].trasa].dl;
                    for (float s = zd[f].s0 - przod + 1f; s <= s1 + tyl; s += .5f)
                    {
                        var q = Poz(zd[f].trasa, s);
                        float dx = q.c.x - w.c.x, dz = q.c.z - w.c.z;
                        if (dx * dx + dz * dz > (dl + .3f) * (dl + .3f) + (szer + .3f) * (szer + .3f)) continue;
                        if (Prost(w.c, w.f, szer * .5f + .15f, dl * .5f + .15f, q.c, q.f, szer * .5f + .15f, dl * .5f + .15f)) { kol = true; break; }
                    }
                }
                if (!kol) break;
                zd[e].s0 -= .5f;
            }
        }
    }
    bool Konflikt(int a, int b) => a != b && konflikty.Contains((long)a * 100000 + b);
    public int LiczbaKonfliktow => konflikty.Count / 2;

    // ---------------- auta ----------------
    public Auto Dodaj(int trasa, float s, float dl, float szer, float rozstaw, float osTyl, float vmax)
    {
        if (tr.Length == 0) return null;
        trasa = Mathf.Clamp(trasa, 0, tr.Length - 1);
        var a = new Auto { id = auta.Count, trasa = trasa, obszar = tr[trasa].obszar, s = s, dl = dl, szer = szer, rozstaw = Mathf.Max(1.5f, rozstaw), osTyl = osTyl, vmax = vmax };
        a.przodOd = dl * .5f - osTyl; a.tylOd = dl * .5f + osTyl;
        auta.Add(a); Poza(a, a.s, out a.poz, out a.kier);
        return a;
    }

    void Poza(Auto a, float s, out Vector3 poz, out Vector3 kier) => PozaNa(a.trasa, a, s, out poz, out kier);
    void PozaNa(int trasa, Auto a, float s, out Vector3 poz, out Vector3 kier)
    {
        var r = P(trasa, s); var f = P(trasa, s + a.rozstaw);
        var k = f - r; float y = (r.y + f.y) * .5f; k.y = 0f;
        k = k.sqrMagnitude > 1e-6f ? k.normalized : T(trasa, s);
        poz = r - k * a.osTyl; poz.y = y; kier = k;
    }

    // Prostokąty aut (2D, test osi rozdzielających).
    public static bool Prost(Vector3 c1, Vector3 f1, float hw1, float hl1, Vector3 c2, Vector3 f2, float hw2, float hl2)
    {
        float dx = c2.x - c1.x, dz = c2.z - c1.z;
        float r1x = f1.z, r1z = -f1.x, r2x = f2.z, r2z = -f2.x;
        for (int k = 0; k < 4; k++)
        {
            float ax, az;
            if (k == 0) { ax = f1.x; az = f1.z; } else if (k == 1) { ax = r1x; az = r1z; } else if (k == 2) { ax = f2.x; az = f2.z; } else { ax = r2x; az = r2z; }
            float p1 = hw1 * Math.Abs(r1x * ax + r1z * az) + hl1 * Math.Abs(f1.x * ax + f1.z * az);
            float p2 = hw2 * Math.Abs(r2x * ax + r2z * az) + hl2 * Math.Abs(f2.x * ax + f2.z * az);
            if (Math.Abs(dx * ax + dz * az) > p1 + p2) return false;
        }
        return true;
    }
    bool Nachodzi(Auto a, Vector3 p, Vector3 k, float zapasBok, float zapasDl, Auto pomin)
    {
        foreach (var b in auta)
        {
            if (b == a || b == pomin) continue;
            float dx = b.poz.x - p.x, dz = b.poz.z - p.z, r = (a.dl + b.dl) * .5f + zapasDl + 1f;
            if (dx * dx + dz * dz > r * r) continue;
            if (Prost(p, k, a.szer * .5f + zapasBok, a.dl * .5f + zapasDl, b.poz, b.kier, b.szer * .5f, b.dl * .5f)) { ostatniKolizyjny = b.id; return true; }
        }
        return false;
    }
    // auto, na które a nachodziłoby po przesunięciu do (p, k), a teraz nie nachodzi (−1 = brak)
    int NoweNachodzenie(Auto a, Vector3 p, Vector3 k)
    {
        foreach (var b in auta)
        {
            if (b == a) continue;
            float dx = b.poz.x - p.x, dz = b.poz.z - p.z, r = (a.dl + b.dl) * .5f + 1f;
            if (dx * dx + dz * dz > r * r) continue;
            // Nie dopuść do wejścia głębiej w inne auto. Zakleszczenia rozwiązuje kontrolowane przestawienie poza widokiem.
            if (Prost(p, k, a.szer * .5f - .03f, a.dl * .5f - .03f, b.poz, b.kier, b.szer * .5f, b.dl * .5f)) return b.id;
        }
        return -1;
    }
    public bool NachodziTeraz(Auto a, Auto b, float zmniejsz) =>
        Prost(a.poz, a.kier, a.szer * .5f - zmniejsz, a.dl * .5f - zmniejsz, b.poz, b.kier, b.szer * .5f - zmniejsz, b.dl * .5f - zmniejsz);

    // Rozstawienie startowe: od zadanego s do przodu, aż auto stoi z dala od skrzyżowań i od innych aut.
    public void UstawStart()
    {
        foreach (var a in auta) a.poz = new Vector3(1e6f + a.id * 100f, 0f, 1e6f);
        foreach (var a in auta)
        {
            float L = tr[a.trasa].dl; float s0 = a.s; bool znalezione = false;
            for (int k = 0; k < 80; k++)
            {
                float s = tr[a.trasa].petla ? Rep(s0 + k * 6f, L) : Mathf.Clamp(s0 + k * 6f, 2f, L - 20f);
                Poza(a, s, out var p, out var kk);
                if (!DalekoOdWezlow(p, 14f)) continue;
                if (Nachodzi(a, p, kk, .5f, 4f, null)) continue;
                // Validate the same static obstacles, parked cars and external vehicles as at runtime.
                // Otherwise a car can be spawned inside a wall or a parked vehicle on frame one.
                if (ZajeteZewnetrznie(p, kk, a.szer * .5f + .35f, a.dl * .5f + .5f)) continue;
                a.s = s; znalezione = true; break;
            }
            if (!znalezione) throw new InvalidOperationException("Brak bezkolizyjnego miejsca startowego auta " + a.id + " na trasie " + a.trasa);
            Poza(a, a.s, out a.poz, out a.kier);
            a.v = Mathf.Min(a.vmax, VProf(a.trasa, a.s + a.rozstaw)) * .6f;
        }
    }
    bool DalekoOdWezlow(Vector3 p, float min)
    {
        foreach (var w in wz)
        {
            float r = w.rodzaj == 1 ? min + 14f : min; float dx = w.srodek.x - p.x, dz = w.srodek.z - p.z;
            if (dx * dx + dz * dz < r * r) return false;
        }
        return true;
    }

    // ---------------- krok ----------------
    public void Krok(float dt)
    {
        if (auta.Count == 0 || dt <= 0f) return;
        czas += dt; akum += dt;
        foreach (var a in auta) a.przestawiony = false;
        if (akum >= krokLogiki - 1e-4f) { float t = Mathf.Min(akum, .3f); akum = 0f; Logika(t); }
        Ruch(dt);
    }

    void Logika(float t)
    {
        // 1) kto jest w środku skrzyżowań / na rondzie
        for (int n = 0; n < wz.Length; n++) { wSrodku[n].Clear(); wSrodkuZd[n].Clear(); }
        foreach (var a in auta)
        {
            a.bramka = -1; a.rondo = -1; float best = 1e9f;
            float sF = a.s + a.przodOd, sT = a.s - a.tylOd;
            foreach (int e in wezlyTrasy[a.trasa])
            {
                var z = zd[e];
                float rel0 = Rel(a.trasa, z.s0 - sF), relW = Rel(a.trasa, z.s1 + (z.typ == Z_WEZEL ? 1.5f : 0f) - sT);   // +1,5 m: tył w pełni poza skrzyżowaniem
                if (rel0 < -.02f && relW > 0f && relW < (z.s1 - z.s0) + a.dl + 2f)
                {
                    wSrodku[z.wezel].Add(a.id); wSrodkuZd[z.wezel].Add(e); if (a.rez == e) { a.rez = -1; przejazdyWezlow++; }
                    if (z.typ == Z_RONDO) { a.rondo = z.wezel; a.katWj = z.katWjazdu; a.katZj = z.katZjazdu; }
                }
                else if (rel0 >= -.02f && rel0 < best) { best = rel0; a.bramka = e; }   // (granica jak „w środku” — bez przerwy na linii)
            }
            if (a.bramka >= 0 && best > 60f) a.bramka = -1;
        }
        // rezerwacje tuż przed linią liczą się jak zajęcie
        foreach (var a in auta)
            if (a.rez >= 0 && a.rez == a.bramka && czas - a.tRez < 3f)
            {
                wSrodku[zd[a.rez].wezel].Add(a.id); wSrodkuZd[zd[a.rez].wezel].Add(a.rez);
            }
        // 2) decyzje
        foreach (var a in auta) Decyzja(a, t);
        // 3) zaklinowane, koniec trasy
        foreach (var a in auta)
        {
            bool stoi = a.v < .2f && (a.vCel < .3f || a.blokada >= 0);   // stoi z wyboru albo zablokowane obrysem innego auta
            if (a.powod == P_KONIEC && stoi)
            {
                if (Przestaw(a, true)) nowePrzejazdy++;
                continue;
            }
            if (stoi && !a.uprawnione) a.tStoi += t; else a.tStoi = 0f;
            if (a.tStoi > 10f)
            {
                string opis = naZakleszczenie != null ? Opis(a) : null;
                if (Przestaw(a, false)) { zakleszczenia++; naZakleszczenie?.Invoke(opis); } else a.tStoi = 8f;
            }
        }
    }

    public string Opis(Auto a)
    {
        string b = a.bramka >= 0 ? " węzeł " + zd[a.bramka].wezel + " (" + wz[zd[a.bramka].wezel].srodek.x.ToString("F0") + ", " + wz[zd[a.bramka].wezel].srodek.z.ToString("F0") + ") typ " + zd[a.bramka].typ + " zwrot " + zd[a.bramka].zwrot : "";
        string l = a.lider >= 0 ? " lider " + a.lider + " (powód " + auta[a.lider].powod + ", v " + auta[a.lider].v.ToString("F1") + ")" : "";
        string o = "auto " + a.id + " trasa " + tr[a.trasa].nazwa + " s " + a.s.ToString("F1") + " poz (" + a.poz.x.ToString("F1") + ", " + a.poz.z.ToString("F1") + ") powód " + a.powod + l + b + " czeka " + a.tCzeka.ToString("F1") + (a.blokada >= 0 ? " zablokowane obrysem auta " + a.blokada : "") +
            (a.powod == P_USTEP && a.czekaNa >= 0 ? " ustępuje autu " + a.czekaNa + " (" + a.czekaCzemu + ", poz " + auta[a.czekaNa].poz.x.ToString("F1") + ", " + auta[a.czekaNa].poz.z.ToString("F1") + ", powód " + auta[a.czekaNa].powod + ", v " + auta[a.czekaNa].v.ToString("F1") + ", " + tr[auta[a.czekaNa].trasa].nazwa + ")" : "");
        // czoło kolejki
        var h = a; int g = 0;
        while (h.lider >= 0 && g++ < 30) h = auta[h.lider];
        if (h != a) o += " | czoło: auto " + h.id + " " + tr[h.trasa].nazwa + " poz (" + h.poz.x.ToString("F1") + ", " + h.poz.z.ToString("F1") + ") powód " + h.powod + " v " + h.v.ToString("F1") + (h.blokada >= 0 ? " obrys auta " + h.blokada + " (" + auta[h.blokada].poz.x.ToString("F1") + ", " + auta[h.blokada].poz.z.ToString("F1") + " " + tr[auta[h.blokada].trasa].nazwa + ")" : "") + (h.bramka >= 0 ? " bramka węzeł (" + wz[zd[h.bramka].wezel].srodek.x.ToString("F0") + ", " + wz[zd[h.bramka].wezel].srodek.z.ToString("F0") + ")" : "");
        return o;
    }

    // Czujnik: wolna droga przed autem wzdłuż jego trasy — pierwsze położenie (co 1 m, cała bryła auta wydłużona o 0,6 m
    // z przodu i poszerzona o 0,15 m), w którym zahaczyłoby o inne auto (także stojące w poprzek — np. włączające się).
    readonly Vector3[] probkiK = new Vector3[48];
    float Przod(Auto a, int n, out int lider, int pomin = -1)
    {
        lider = -1; float sF = a.s + a.przodOd;
        for (int k = 0; k <= n; k++)
        {
            probki[k] = P(a.trasa, sF + k);
            var r = P(a.trasa, a.s + k); var f = P(a.trasa, a.s + k + a.rozstaw) - r; f.y = 0f;
            f = f.sqrMagnitude > 1e-6f ? f.normalized : a.kier;
            probkiK[k] = f; probkiS[k] = r - f * a.osTyl + f * .3f;
        }
        float hw = a.szer * .5f + .06f, hl = a.dl * .5f + .3f;
        float best = 1e9f;
        foreach (var b in auta)
        {
            if (b == a || b.id == pomin) continue;
            float dx = b.poz.x - a.poz.x, dz = b.poz.z - a.poz.z, r = n + a.dl + b.dl;
            if (dx * dx + dz * dz > r * r) continue;
            float hlB = hl;
            if (a.rondo >= 0 && b.rondo == a.rondo)
            {
                var c = wz[a.rondo].srodek; bool aNa = (a.poz - c).magnitude <= RondoWlaczony, bNa = (b.poz - c).magnitude <= RondoWlaczony;
                if (aNa && !bNa) hlB = hl + 1f;   // jadący po rondzie: szerszy margines przed włączającym się
            }
            if (a.kier.x * b.kier.x + a.kier.z * b.kier.z < -.3f)
            {
                // auto z naprzeciwka: liczy się tylko, gdy stoi na naszym torze (środek naszej drogi w jego obrysie)
                float fx = b.kier.x, fz = b.kier.z, rx = fz, rz = -fx, hx = b.szer * .5f + a.szer * .4f, hz = b.dl * .5f + .2f;
                for (int k = 0; k <= n && k < best; k++)
                {
                    float px = probki[k].x - b.poz.x, pz = probki[k].z - b.poz.z;
                    if (Math.Abs(px * rx + pz * rz) < hx && Math.Abs(px * fx + pz * fz) < hz) { best = k; lider = b.id; break; }
                }
                continue;
            }
            for (int k = 0; k <= n && k < best; k++)
            {
                float ex = probkiS[k].x - b.poz.x, ez = probkiS[k].z - b.poz.z, rr = hl + b.dl * .5f + 1f;
                if (ex * ex + ez * ez > rr * rr) continue;
                if (Prost(probkiS[k] + probkiK[k] * (hlB - hl), probkiK[k], hw, hlB, b.poz, b.kier, b.szer * .5f, b.dl * .5f)) { best = k; lider = b.id; break; }
            }
        }
        return best;
    }
    readonly Vector3[] probkiS = new Vector3[48];

    void Decyzja(Auto a, float t)
    {
        float sF = a.s + a.przodOd;
        float vc = Mathf.Min(a.vmax, VProf(a.trasa, a.s + a.rozstaw));
        int n = Mathf.Clamp((int)(a.v * a.v / (2f * Ham) + 14f), 14, probki.Length - 1);
        float vO = 1e9f, twardo = 1e9f; int powod = P_WOLNA; bool upr = false;
        void Przeszkoda(float wolne, float odstep, int p, bool u, float tw)
        {
            float x = wolne - odstep; float vv = x <= 0f ? 0f : Mathf.Sqrt(2f * Ham * x);
            if (vv < vO) { vO = vv; powod = p; upr = u; }
            twardo = Mathf.Min(twardo, wolne - tw);
        }
        // a) auto przed nami (czujnik)
        float wolneAuto = Przod(a, n, out int lid);
        // dwa auta widzą się nawzajem (np. włączające się na rondo i nadjeżdżające po pierścieniu, oba stoją):
        // jedzie to, które jest bardziej „z przodu” względem drugiego — drugie czeka (inaczej stałyby w nieskończoność)
        if (lid >= 0 && auta[lid].blokada == a.id && a.v < .5f && auta[lid].v < .5f)
            wolneAuto = Przod(a, n, out lid, lid);   // tamto auto stoi, bo zahaczyłoby o nas — ruszamy my (twarde zabezpieczenie i tak pilnuje)
        else if (lid >= 0 && auta[lid].lider == a.id && a.v < .5f && auta[lid].v < .5f)
        {
            var b = auta[lid];
            float jaPrzed = Vector3.Dot(a.poz - b.poz, b.kier), onPrzed = Vector3.Dot(b.poz - a.poz, a.kier);
            if (jaPrzed > onPrzed + .05f || (Math.Abs(jaPrzed - onPrzed) <= .05f && a.id < b.id)) wolneAuto = Przod(a, n, out lid, b.id);
        }
        a.lider = lid;
        if (lid >= 0)
        {
            var L = auta[lid];
            bool u = L.uprawnione || L.powod == P_GRACZ || L.powod == P_STREFA;
            Przeszkoda(Mathf.Max(0f, wolneAuto - .5f), 1.4f, P_AUTO, u, .3f);
        }
        // b) bramka skrzyżowania / ronda
        // Full player car footprint, including a parked car. A point at the rider's hips is not sufficient.
        for (int k = 0; k <= n; k++)
            if (ZajeteZewnetrznie(probkiS[k], probkiK[k], a.szer*.5f+.15f, a.dl*.5f+.3f))
            { Przeszkoda(Mathf.Max(0,k-.5f),1.4f,P_GRACZ,true,.3f); break; }
        bool czekaNaLinii = false;
        if (a.bramka >= 0)
        {
            var z = zd[a.bramka]; float rel0 = Rel(a.trasa, z.s0 - sF);
            float zasiegDecyzji = Mathf.Max(12f, a.v * a.v / (2f * Ham) + 5f);
            if (rel0 < zasiegDecyzji)
            {
                // przyznany wjazd trwa (do 3 s): na skrzyżowaniu zawsze, na rondzie tuż przed linią — bez „pełzania” na zmianę
                bool trwa = a.rez == a.bramka && czas - a.tRez < 3f && (z.typ == Z_WEZEL || rel0 < 1.5f);
                bool ok = trwa || Pozwolenie(a, a.bramka, sF, wolneAuto, lid);
                if (ok && !trwa) { a.rez = a.bramka; a.tRez = czas; wSrodku[z.wezel].Add(a.id); wSrodkuZd[z.wezel].Add(a.bramka); }
                if (!ok)
                {
                    if (a.rez == a.bramka) a.rez = -1;
                    // czeka na auto w ruchu (zwykłe czekanie na lukę) — nie zakleszczenie
                    bool ruch = a.czekaNa >= 0 && a.czekaNa < auta.Count && auta[a.czekaNa].v > .3f;
                    Przeszkoda(Mathf.Max(0f, rel0), .45f, P_USTEP, ruch, .25f);
                    czekaNaLinii = rel0 < 2.5f && a.v < .4f;
                }
            }
        }
        if (czekaNaLinii) a.tCzeka += t; else if (a.v > 1f || a.bramka < 0) a.tCzeka = 0f;
        // b2) na rondzie: przepuść auto, które długo czeka na wlocie przed nami (jak uprzejmy kierowca)
        if (a.rondo >= 0)
        {
            var c = wz[a.rondo].srodek; var dc = a.poz - c;
            if (dc.magnitude <= RondoWlaczony)
            {
                float thA = Kat(dc), doZj = Rep(a.katZj - thA, 360f);
                foreach (var b in auta)
                {
                    if (b == a) continue;
                    // auto już włączające się (za linią, jeszcze nie na pierścieniu) przed nami — czekamy, aż wjedzie
                    if (b.rondo == a.rondo && (b.poz - c).magnitude > RondoWlaczony)
                    {
                        float d = Rep(b.katWj - thA, 360f);
                        if (d > 12f && d < 110f && doZj > d) Przeszkoda(Mathf.Max(0f, (d - 40f) * Mathf.Deg2Rad * 9.4f), .3f, P_USTEP, true, .2f);
                        continue;
                    }
                    if (b.bramka < 0 || b.tCzeka < 6f || !b.czekaPierwsz) continue;
                    var zb = zd[b.bramka]; if (zb.typ != Z_RONDO || zb.wezel != a.rondo) continue;
                    float d2 = Rep(zb.katWjazdu - thA, 360f);
                    if (d2 > 50f && d2 < 95f && doZj > d2) Przeszkoda((d2 - 50f) * Mathf.Deg2Rad * 9.4f, .3f, P_USTEP, true, .2f);
                }
            }
        }
        // c) strefy zatrzymania (światła, przejścia) — próbki wzdłuż trasy; ostatnia „stój” = linia zatrzymania
        if (pytajStrefy)
        {
            int m = Mathf.Min(n, 30), pierwszy = -1, ostatni = -1;
            for (int k = 0; k <= m; k++)
            {
                var q = P(a.trasa, sF + k);
                if (BraxStrefyZatrzymania.CzyStop(q, T(a.trasa, sF + k), false)) { if (pierwszy < 0) pierwszy = k; ostatni = k; }
                else if (pierwszy >= 0) break;
            }
            if (pierwszy >= 0)
            {
                float wolne = Mathf.Max(0f, ostatni - .5f);
                // żółte: strefa pojawiła się, gdy auto było za blisko, żeby bezpiecznie stanąć — przejeżdża (i nie zmienia zdania)
                if (a.tStrefa < .25f && a.v * a.v > 2f * 5.5f * wolne && wolne < 6f) a.zolte = true;
                a.tStrefa += t;
                if (!a.zolte) Przeszkoda(wolne, .2f, P_STREFA, true, 0f);
            }
            else { a.tStrefa = 0f; a.zolte = false; }
        }
        // d) gracz na jezdni: hamuje (2,5 m przed nim) i trąbi
        if (graczJest)
        {
            float dx = gracz.x - a.poz.x, dz = gracz.z - a.poz.z, r = n + a.dl;
            if (dx * dx + dz * dz < r * r)
                for (int k = 0; k <= n; k++)
                {
                    float px = probki[k].x - gracz.x, pz = probki[k].z - gracz.z;
                    if (px * px + pz * pz < (a.szer * .5f + .7f) * (a.szer * .5f + .7f)) { Przeszkoda(Mathf.Max(0f, k - 1f), 2.5f, P_GRACZ, true, .3f); break; }
                }
        }
        // e) koniec trasy otwartej
        if (!tr[a.trasa].petla)
        {
            float wolne = tr[a.trasa].dl - .5f - sF;
            if (wolne < 40f) Przeszkoda(Mathf.Max(0f, wolne), .2f, P_KONIEC, true, 0f);
        }
        // Nie trąb bez końca na tego samego gracza / na samochód gracza.
        // Maks. jedno krótkie ostrzeżenie podczas pojedynczej blokady; ponowienie
        // dopiero po opuszczeniu pasa ruchu i co najmniej 30 s od poprzedniego.
        if (powod == P_GRACZ && a.v < .5f)
        {
            a.tGracz += t;
            if (a.tGracz > 3f && !a.klaksonUzytyPrzyBlokadzie && czas >= a.tKlakson)
            {
                a.klakson = true;
                a.klaksonUzytyPrzyBlokadzie = true;
                a.tKlakson = czas + 30f;
            }
        }
        else
        {
            a.tGracz = 0f;
            a.klaksonUzytyPrzyBlokadzie = false;
        }
        a.vCel = Mathf.Min(vc, vO); a.zapas = Mathf.Max(0f, twardo); a.powod = powod; a.uprawnione = upr;
        // kierunkowskaz
        a.migacz = 0;
        foreach (int e in migaczeTrasy[a.trasa])
        {
            var z = zd[e]; float r0 = Rel(a.trasa, sF - z.s0);
            if (r0 >= 0f && r0 <= z.s1 - z.s0) { a.migacz = z.kier; break; }
        }
    }

    bool Pozwolenie(Auto a, int e, float sF, float wolneAuto, int lid)
    {
        var z = zd[e]; var w = wz[z.wezel];
        // nie wjeżdżaj, jeśli nie zdążysz zjechać (stojące auto za skrzyżowaniem)
        a.czekaPierwsz = false;
        float potrzeba = Rel(a.trasa, z.s1 - sF) + a.dl + 1.5f;
        if (lid >= 0 && wolneAuto < potrzeba && auta[lid].v < 3f) { a.czekaNa = lid; a.czekaCzemu = "brak miejsca za"; return false; }
        if (z.typ == Z_RONDO) return PozwolenieRondo(a, z);
        var wsr = wSrodku[z.wezel]; var wsz = wSrodkuZd[z.wezel];
        for (int i = 0; i < wsr.Count; i++) if (wsr[i] != a.id && Konflikt(e, wsz[i])) { a.czekaNa = wsr[i]; a.czekaCzemu = "zajęte"; return false; }
        // pierwszeństwo między nadjeżdżającymi (kto czeka na linii > 6 s, ten jedzie — reszta go przepuszcza)
        foreach (var b in auta)
        {
            if (b == a || b.bramka < 0 || b.bramka == e) continue;
            var zb = zd[b.bramka]; if (zb.wezel != z.wezel || zb.typ != Z_WEZEL || !Konflikt(e, b.bramka)) continue;
            if (b.tCzeka > 6f && b.czekaPierwsz && b.tCzeka > a.tCzeka) { a.czekaNa = b.id; a.czekaCzemu = "przepuszcza czekającego"; a.czekaPierwsz = true; return false; }
            if (a.tCzeka > 6f) continue;
            float relB = Rel(b.trasa, zb.s0 - (b.s + b.przodOd)); if (relB > 13f + b.v * 1.5f) continue;
            bool zPrawej = z.wlot.x * zb.wlot.z - z.wlot.z * zb.wlot.x > .5f;
            bool lewoNaprzeciw = z.zwrot < 0 && Vector3.Dot(z.wlot, zb.wlot) < -.7f && zb.zwrot >= 0;
            bool ustap = (w.rodzaj == 0 && zPrawej) || lewoNaprzeciw;
            if (!ustap) continue;
            if (a.tCzeka > 2.5f && NajdluzejCzeka(a, z.wezel)) continue;   // zator „każdy ma kogoś z prawej” — rusza najdłużej czekający
            a.czekaNa = b.id; a.czekaCzemu = zPrawej ? "z prawej" : "z naprzeciwka"; a.czekaPierwsz = true; return false;
        }
        return true;
    }
    bool NajdluzejCzeka(Auto a, int wezel)
    {
        foreach (var b in auta)
        {
            if (b == a || b.bramka < 0 || zd[b.bramka].wezel != wezel || b.tCzeka <= 0f) continue;
            if (b.tCzeka > a.tCzeka + 1e-3f || (Math.Abs(b.tCzeka - a.tCzeka) <= 1e-3f && b.id < a.id)) return false;
        }
        return true;
    }
    static float Kat(Vector3 d) { float k = Mathf.Atan2(d.z, d.x) * Mathf.Rad2Deg; return k < 0f ? k + 360f : k; }
    // Rondo: wjeżdżający ustępuje jadącym po pierścieniu (z lewej, do ~125° przed punktem włączenia) i nie wjeżdża,
    // gdy auto z sąsiedniego wlotu właśnie się włącza (przed nim albo za nim na jego drodze) — strefa włączenia jak
    // na prawdziwym rondzie: albo jeden, albo drugi.
    bool PozwolenieRondo(Auto a, BraxZdarzenieAut z)
    {
        var c = wz[z.wezel].srodek;
        var wsr = wSrodku[z.wezel]; var wsz = wSrodkuZd[z.wezel];
        float mojZjazd = Rep(z.katZjazdu - z.katWjazdu, 360f);
        for (int i = 0; i < wsr.Count; i++)
        {
            if (wsr[i] == a.id) continue;
            var b = auta[wsr[i]]; var zb = zd[wsz[i]];
            float th = Kat(b.poz - c);
            bool wlacza = (b.poz - c).magnitude > RondoWlaczony || Rep(th - zb.katWjazdu, 360f) > 300f;
            if (wlacza)
            {
                if (Math.Abs(Mathf.DeltaAngle(zb.katWjazdu, z.katWjazdu)) < 5f) continue;          // ten sam wlot — kolejka
                float przed = Rep(zb.katWjazdu - z.katWjazdu, 360f);                                // jego włączenie przed nami na naszej drodze
                if (przed < 110f && przed < mojZjazd) { a.czekaNa = b.id; a.czekaCzemu = "włącza się przed"; return false; }
                if (Rep(z.katWjazdu - zb.katWjazdu, 360f) < 125f && Rep(zb.katZjazdu - zb.katWjazdu, 360f) > Rep(z.katWjazdu - zb.katWjazdu, 360f)) { a.czekaNa = b.id; a.czekaCzemu = "włącza się za"; return false; }   // minie nasz wlot
                continue;
            }
            float d = Rep(z.katWjazdu - th, 360f);              // ile stopni do punktu włączenia (pod prąd ruchu)
            float okno = a.v < 2f ? 160f : 125f;                // ruszając z miejsca potrzeba więcej czasu
            if (a.tCzeka > 6f && b.v < .3f && d > 45f && d < okno) continue;   // stanęło, żeby nas wpuścić
            if (d < okno)
            {
                float doZjazdu = Rep(zb.katZjazdu - th, 360f);
                if (doZjazdu + 12f < d) continue;               // zjedzie wcześniej, nie minie naszego wlotu
                a.czekaNa = b.id; a.czekaCzemu = "na rondzie"; a.czekaPierwsz = true; return false;
            }
            if (d > 335f) { a.czekaNa = b.id; a.czekaCzemu = "mija wlot"; a.czekaPierwsz = true; return false; }   // właśnie mija punkt włączenia
        }
        return true;
    }

    void Ruch(float dt)
    {
        foreach (var a in auta)
        {
            float acc = (a.vCel - a.v) / (a.vCel < a.v ? .3f : .6f);   // hamowanie szybciej niż przyspieszanie
            acc = Mathf.Clamp(acc, -7.5f, a.v < 4f ? PrzyspDuze : PrzyspMale);
            float v = Mathf.Max(0f, a.v + acc * dt);
            // The vehicle can enter a tighter curve between 0.1 s decision ticks.
            // Enforce the authored corner cap at both ends of this movement step.
            // This also avoids the old 30 overspeed samples in the 600 s QA route.
            if (vZakret > 0f && (Krzywizna(a.trasa, a.s) > 1f / 25f
                || Krzywizna(a.trasa, a.s + v * dt) > 1f / 25f))
                v = Mathf.Min(v, vZakret);
            float ds = v * dt;
            if (ds > a.zapas) { ds = a.zapas; v = Mathf.Min(v, ds / dt); }
            if (ds > 1e-5f)
            {
                // Substeps prevent crossing a narrow obstacle or another car on a slow frame.
                int steps=Mathf.Max(1,Mathf.CeilToInt(ds/.15f));
                var np=a.poz;var nk=a.kier;bool stopped=false;
                for(int step=1;step<=steps;step++)
                {
                    Poza(a,a.s+ds*step/steps,out var cp,out var ck);
                    int kol=NoweNachodzenie(a,cp,ck);
                    if(ZajeteZewnetrznie(cp,ck,a.szer*.5f+.08f,a.dl*.5f+.08f))
                    { a.powod=P_GRACZ;a.uprawnione=true;a.blokada=-1;stopped=true;break; }
                    if(kol>=0){a.blokada=kol;stopped=true;break;}
                    np=cp;nk=ck;
                }
                if(stopped){ds=0;v=0;}
                else {a.poz=np;a.kier=nk;a.blokada=-1;}
            }
            a.s += ds; a.zapas -= ds;
            if (tr[a.trasa].petla) a.s = Rep(a.s, tr[a.trasa].dl);
            a.a = Mathf.Lerp(a.a, (v - a.vPrev) / dt, Mathf.Clamp01(dt * 6f)); a.vPrev = v; a.v = v;
            a.hamuje = a.a < -.7f || (a.v < .3f && a.vCel < .3f);
            var tf = T(a.trasa, a.s + a.rozstaw);
            float cr = a.kier.x * tf.z - a.kier.z * tf.x, dot = a.kier.x * tf.x + a.kier.z * tf.z;
            a.skret = Mathf.Clamp(-Mathf.Atan2(cr, dot) * Mathf.Rad2Deg * 2f, -38f, 38f);
        }
    }

    // Przestawienie auta w nowe miejsce (poza widokiem, wolne, z dala od skrzyżowań). zKonca: auto dojechało do końca
    // trasy otwartej — nowy przejazd od początku trasy otwartej tego samego obszaru.
    bool Przestaw(Auto a, bool zKonca)
    {
        var otwarte = new List<int>(); var petle = new List<int>();
        for (int r = 0; r < tr.Length; r++) if (tr[r].obszar == a.obszar) (tr[r].petla ? petle : otwarte).Add(r);
        for (int proba = 0; proba < 40; proba++)
        {
            int r; float s; bool ukryty = false;
            if (otwarte.Count > 0 && (zKonca || petle.Count == 0 || rnd.NextDouble() < .35))
            {
                r = otwarte[rnd.Next(otwarte.Count)]; s = 1f + a.tylOd + (float)rnd.NextDouble() * 2f;
                if (!zKonca && petle.Count == 0) s = 5f + (float)rnd.NextDouble() * Mathf.Max(1f, tr[r].dl - 60f);
                ukryty = koniecTrasy[r] >= 0 && zd[koniecTrasy[r]].kier == 1 && s < 15f;
            }
            else if (petle.Count > 0) { r = petle[rnd.Next(petle.Count)]; s = (float)rnd.NextDouble() * tr[r].dl; }
            else return false;
            PozaNa(r, a, s, out var p, out var k);
            if (!ukryty && !DalekoOdWezlow(p, 16f)) continue;
            if (!ukryty && widoczne != null && widoczne(p)) continue;
            if (Nachodzi(a, p, k, .4f, 6f, a)) continue;
            if (ZajeteZewnetrznie(p,k,a.szer*.5f+.4f,a.dl*.5f+3f)) continue;
            a.trasa = r; a.s = s; a.poz = p; a.kier = k; a.obszar = tr[r].obszar;
            a.v = ukryty ? Mathf.Min(a.vmax, 6f) : 0f; a.vPrev = a.v; a.a = 0f; a.vCel = a.v; a.zapas = 0f;
            a.rez = -1; a.bramka = -1; a.tStoi = a.tCzeka = a.tGracz = 0f; a.przestawiony = true; a.powod = P_WOLNA;
            przestawienia++;
            return true;
        }
        return false;
    }

    // ---------------- test (CI przy buildzie i lokalnie) ----------------
    public struct WynikTestu { public int zakleszczenia, nachodzenia, pozaDroga, przestawienia, nowePrzejazdy, przejazdyWezlow, konflikty, probkiZakretu, zaSzybkoWZakrecie; public float sredniaV, czasStania, vZakretMax; }
    // krzywizna trasy r w punkcie s (kąt między kierunkami 2.5 m przed i za / 5 m)
    public float Krzywizna(int r, float s) => Vector3.Angle(T(r, s - 2.5f), T(r, s + 2.5f)) * Mathf.Deg2Rad / 5f;

    // Symulacja `sekundy` czasu gry krokiem `krok` bez renderu. naJezdni(p) — czy punkt (styk koła z jezdnią) leży na
    // jezdni. Liczy: zakleszczenia (auta przestawione po > 10 s stania), nachodzenia (nowe pary aut, których obrysy
    // pomniejszone o 5 cm się przecinają), poza_droga (wyjazdy koła poza jezdnię o > 20 cm).
    public WynikTestu Test(float sekundy, float krok, Func<Vector3, bool> naJezdni)
    {
        var w = new WynikTestu { konflikty = LiczbaKonfliktow };
        int z0 = zakleszczenia, p0 = przestawienia, n0 = nowePrzejazdy, x0 = przejazdyWezlow;
        var pary = new HashSet<long>(); var poza = new bool[auta.Count];
        double sumaV = 0; long probek = 0; double stanie = 0;
        int kroki = (int)(sekundy / krok);
        for (int i = 0; i < kroki; i++)
        {
            Krok(krok);
            for (int x = 0; x < auta.Count; x++)
            {
                var a = auta[x]; sumaV += a.v; probek++; if (a.v < .2f) stanie += krok;
                if (Krzywizna(a.trasa, a.s) > 1f / 25f) { w.probkiZakretu++; w.vZakretMax = Mathf.Max(w.vZakretMax, a.v); if (vZakret > 0f && a.v > vZakret + .3f) w.zaSzybkoWZakrecie++; }
                for (int y = x + 1; y < auta.Count; y++)
                {
                    var b = auta[y]; long key = (long)x * 10000 + y;
                    float dx = a.poz.x - b.poz.x, dz = a.poz.z - b.poz.z;
                    bool nach = dx * dx + dz * dz < 64f && NachodziTeraz(a, b, .05f);
                    if (nach) { if (pary.Add(key)) { w.nachodzenia++; naZakleszczenie?.Invoke("NACHODZENIE t=" + czas.ToString("F1") + " " + Opis(a) + " || " + Opis(b)); } } else pary.Remove(key);
                }
                if (naJezdni != null)
                {
                    bool off = false; var r = new Vector3(a.kier.z, 0, -a.kier.x);
                    var tyl = a.poz + a.kier * a.osTyl; var przod = tyl + a.kier * a.rozstaw; float bok = a.szer * .42f;
                    if (!naJezdni(tyl + r * bok) || !naJezdni(tyl - r * bok) || !naJezdni(przod + r * bok) || !naJezdni(przod - r * bok)) off = true;
                    if (off && !poza[x] && !a.przestawiony) w.pozaDroga++;
                    poza[x] = off;
                }
            }
        }
        w.zakleszczenia = zakleszczenia - z0; w.przestawienia = przestawienia - p0; w.nowePrzejazdy = nowePrzejazdy - n0; w.przejazdyWezlow = przejazdyWezlow - x0;
        w.sredniaV = probek > 0 ? (float)(sumaV / probek) : 0f; w.czasStania = auta.Count > 0 ? (float)(stanie / auta.Count) : 0f;
        return w;
    }
}
