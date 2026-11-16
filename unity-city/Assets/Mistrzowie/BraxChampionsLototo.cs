using System;
using System.Collections.Generic;
using UnityEngine;

// MIASTO MISTRZÓW - LOTOTO BRAX CASH (owner 16.11 22:52-23:08: "Kolektura ... i w telewizorach losowanie", "taki pokoik i
// gościa, który będzie przy tym bębnie ... automatycznie ... 2 razy w tygodniu", the name "Lototo Brax Cash", the studio "w
// tym BRAXHORN BET", his poster: "DO WYGRANIA 10 000 000 ZŁ · DWA LOSOWANIA W TYGODNIU · WYNIKI NA ŻYWO W TELEWIZJI ·
// CODZIENNIE O 20:00"). A lottery 6 of 49 as the real ones:
//  - the kolektura (its counter in the BRAXHORN BET): AKCJA buys a coupon "chybił trafił" for the next draw (3 zł, the price
//    of a bet of the real 6 of 49); after the draw AKCJA checks the player's coupons and pays: 3 hits 24 zł (as the real
//    three), 4 hits 170 zł, 5 hits 5 000 zł, 6 hits the KUMULACJA (from 10 000 000 zł, the poster; it grows by 2 500 000 a
//    draw nobody hits, someone in the country hits it on one draw in nine);
//  - the draws: Wednesday and Saturday at 20:00 by the phone's clock, live in the studio behind the glass - the host by
//    the machine, the 49 balls mixing in the glass drum, six coming out one by one down the chute onto the rail; the
//    studio camera's picture on every screen of the BET (a live picture of the game, not a film); on the other days at
//    20:00 (and between the draws) the camera shows the results board;
//  - the numbers come from the draw's date: the same on every phone, as one real draw; a draw the player missed is on the
//    board, and AKCJA at the studio's glass replays it.
// The money is the casino's credit (BraxChampionsKasyno) until the game has its wallet. Zasady is pure (automaty_sim).
public sealed class BraxChampionsLototo : MonoBehaviour, IBraxInterakcja
{
    public static class Zasady
    {
        public const int Pula = 49, Typy = 6, Cena = 3, Godzina = 20, MaxKuponow = 10;
        public const int KumulacjaStart = 10000000, KumulacjaWzrost = 2500000, KumulacjaMax = 200000000;
        public static readonly int[] Wygrane = { 0, 0, 0, 24, 170, 5000 };              // by hits; 6 = the kumulacja
        public static readonly DayOfWeek[] Dni = { DayOfWeek.Wednesday, DayOfWeek.Saturday };
        public static readonly DateTime Pierwsze = new DateTime(2026, 11, 18, Godzina, 0, 0);   // the first draw
        // the show: the intro, the mixing, a ball every 8 s (2 s out of the drum, 3 s down the chute), the board
        public const float Intro = 6f, Mieszanie = 12f, Odstep = 8f, Wyjscie = 2f, Toczenie = 3f, Koniec = 70f, Program = 90f;
        public static float StartKuli(int k) => Intro + Mieszanie + k * Odstep;

        public static bool Dzien(DateTime d) { foreach (var x in Dni) if (d.DayOfWeek == x) return true; return false; }
        public static DateTime Termin(DateTime day) => new DateTime(day.Year, day.Month, day.Day, Godzina, 0, 0);
        /// <summary>The first draw starting at t or later.</summary>
        public static DateTime Nastepne(DateTime t)
        {
            var d = t.Date;
            for (int i = 0; i < 9; i++, d = d.AddDays(1)) { var s = Termin(d); if (Dzien(d) && s >= t && s >= Pierwsze) return s; }
            return Pierwsze;
        }
        /// <summary>The last draw that started at t or before (DateTime.MinValue before the first).</summary>
        public static DateTime Ostatnie(DateTime t)
        {
            if (t < Pierwsze) return DateTime.MinValue;
            var d = t.Date;
            for (int i = 0; i < 9; i++, d = d.AddDays(-1)) { var s = Termin(d); if (Dzien(d) && s <= t) return s; }
            return DateTime.MinValue;
        }
        public static int Id(DateTime d) => d.Year * 10000 + d.Month * 100 + d.Day;
        public static DateTime Data(int id) => new DateTime(id / 10000, id / 100 % 100, id % 100, Godzina, 0, 0);
        static int[] Szesc(System.Random r)
        {
            var a = new int[Pula]; for (int i = 0; i < Pula; i++) a[i] = i + 1;
            for (int i = 0; i < Typy; i++) { int j = i + r.Next(Pula - i); int t = a[i]; a[i] = a[j]; a[j] = t; }
            var w = new int[Typy]; Array.Copy(a, w, Typy); return w;
        }
        /// <summary>The draw's six numbers in the order they come out (from its date: the same on every phone).</summary>
        public static int[] Losuj(int id) => Szesc(new System.Random(unchecked(id * 7919 + 1049)));
        /// <summary>A "chybił trafił" bet, ascending.</summary>
        public static int[] ChybilTrafil(System.Random r) { var w = Szesc(r); Array.Sort(w); return w; }
        public static int Trafienia(int[] kupon, int[] wynik) { int n = 0; foreach (int a in kupon) if (Array.IndexOf(wynik, a) >= 0) n++; return n; }
        /// <summary>Someone in the country hits the six in this draw (one draw in nine).</summary>
        public static bool Szostka(int id) => new System.Random(unchecked(id * 31 + 7)).Next(9) == 0;
        /// <summary>The kumulacja played for in the draw starting at termin.</summary>
        public static int Kumulacja(DateTime termin)
        {
            long k = KumulacjaStart;
            for (var d = Pierwsze; d < termin; d = Nastepne(d.AddMinutes(1)))
                k = Szostka(Id(d)) ? KumulacjaStart : Math.Min(KumulacjaMax, k + KumulacjaWzrost);
            return (int)k;
        }
        public static int Wygrana(int trafienia, DateTime termin) => trafienia >= Typy ? Kumulacja(termin) : trafienia >= 0 ? Wygrane[Math.Min(trafienia, 5)] : 0;
        static readonly string[] DniNazwy = { "NIEDZIELA", "PONIEDZIAŁEK", "WTOREK", "ŚRODA", "CZWARTEK", "PIĄTEK", "SOBOTA" };
        public static string Nazwa(DateTime d) => DniNazwy[(int)d.DayOfWeek];
        public static string Liczby(int[] w) => string.Join(" ", Array.ConvertAll(w, x => x.ToString()));
        // the coupons (PlayerPrefs "lototo_kupony"): "id:a,b,c,d,e,f;" per bet
        public static List<(int id, int[] liczby)> Czytaj(string s)
        {
            var l = new List<(int, int[])>();
            foreach (var e in (s ?? "").Split(';'))
            {
                var p = e.Split(':'); if (p.Length != 2 || !int.TryParse(p[0], out int id)) continue;
                var n = p[1].Split(','); if (n.Length != Typy) continue;
                var w = new int[Typy]; bool ok = true;
                for (int i = 0; i < Typy; i++) ok &= int.TryParse(n[i], out w[i]) && w[i] >= 1 && w[i] <= Pula;
                if (ok) l.Add((id, w));
            }
            return l;
        }
        public static string Zapisz(List<(int id, int[] liczby)> l)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var (id, w) in l) sb.Append(id).Append(':').Append(string.Join(",", Array.ConvertAll(w, x => x.ToString()))).Append(';');
            return sb.ToString();
        }
    }

    // the board's atlas (made by the generator, 2048 x 2048, rectangles from the bottom-left): the digits 0-9, ':' and '.',
    // the board's colour, the balls 1-49 with their numbers, an empty ball, the words
    public static class Tablica
    {
        public const int W = 2048, H = 2048, CyfraW = 80, CyfraH = 120;
        public static readonly string[] Slowa = { "NA ŻYWO", "POWTÓRKA", "WYNIKI", "KUMULACJA", "NASTĘPNE LOSOWANIE", "ZŁ", "NIEDZIELA", "PONIEDZIAŁEK",
            "WTOREK", "ŚRODA", "CZWARTEK", "PIĄTEK", "SOBOTA", "LOSOWANIE", "LOTOTO BRAX CASH" };
        public const int NaZywo = 0, Powtorka = 1, Wyniki = 2, Kumul = 3, Nastepne = 4, Zl = 5, Niedziela = 6, Losowanie = 13, Marka = 14;
        public static RectInt Cyfra(int d) => new RectInt(d * CyfraW, 0, CyfraW, CyfraH);       // 10 = ':', 11 = '.'
        public static readonly RectInt Tlo = new RectInt(984, 44, 24, 24);                       // inside a plain 40 px cell
        public static RectInt Kula(int n) => new RectInt(1024 + (n - 1) % 7 * 128, (n - 1) / 7 * 128, 128, 128);
        public static readonly RectInt Pusta = new RectInt(1920, 0, 128, 128);
        public static RectInt Slowo(int i) => new RectInt(0, 128 + i * 112, 1000, 104);
    }

    // ---------------- the scene (the generator wires it)
    public Transform beben;                                   // the drum's centre; the balls are its children
    public Transform[] kule = new Transform[0];               // [n - 1] = the ball n
    public Transform[] tor = new Transform[0];                // the way out: the outlet, then the chute
    public Transform[] miejsca = new Transform[0];            // the six cups on the rail
    public Transform lada, szyba, prowadzacy;                 // the counter, the studio's glass (replay), the host
    public Camera kamera; public Transform ujecieBeben, ujecieTablica;
    public Material ekran;                                    // every screen showing the studio (the camera's picture)
    public MeshFilter tablica; public float tablicaW = 2.4f, tablicaH = 1.35f;
    public float[] slowaSzer = new float[0];                  // each word's ink width / its cell's (the generator measures them)
    public float kulaR = .035f, bebenR = .36f;                // metres
    public string nazwa = "LOTOTO BRAX CASH";
    const string KluczKupony = "braxhorn_lototo_kupony";

    // ---------------- the show
    bool pokaz, powtorka; float t; int[] wynik; int pokazId; readonly bool[] wyszla = new bool[6];
    Vector3[] p, v; bool[] wDrodze; Vector3[] droga; float[] drogaL; Vector3[] cele = new Vector3[6];
    RenderTexture rt; float nastepnaKlatka; bool brudna = true; float zegar; int ostatniStan = -1;
    readonly System.Random los = new System.Random();
    Mesh tablicaMesh; BraxChampionsAutomatyGry.Warstwa warstwa;

    void OnEnable() { BraxInterakcje.Dodaj(this); }
    void OnDisable() { BraxInterakcje.Usun(this); }

    void Start()
    {
        int n = kule.Length; p = new Vector3[n]; v = new Vector3[n]; wDrodze = new bool[n];
        // the way out in the drum's frame (it stays put: the studio is static)
        droga = new Vector3[tor.Length]; drogaL = new float[tor.Length];
        for (int i = 0; i < tor.Length; i++) { droga[i] = beben.InverseTransformPoint(tor[i].position); if (i > 0) drogaL[i] = drogaL[i - 1] + Vector3.Distance(droga[i], droga[i - 1]); }
        for (int k = 0; k < miejsca.Length && k < 6; k++) cele[k] = beben.InverseTransformPoint(miejsca[k].position);
        Rozsyp();
        if (kamera)
        {
            kamera.enabled = false;
            rt = new RenderTexture(640, 360, 16, RenderTextureFormat.ARGB32) { name = "LOTOTO studio kamera", antiAliasing = 1 };
            kamera.targetTexture = rt;
            if (ekran) { ekran.mainTexture = rt; if (ekran.HasProperty("_EmissionMap")) ekran.SetTexture("_EmissionMap", rt); }
        }
        if (tablica) { tablicaMesh = new Mesh { name = "LOTOTO tablica wyników" }; tablicaMesh.MarkDynamic(); tablica.sharedMesh = tablicaMesh; warstwa = new BraxChampionsAutomatyGry.Warstwa(Tablica.W, Tablica.H); }
        var last = Zasady.Ostatnie(DateTime.Now);
        if (last != DateTime.MinValue) PolozWyniki(Zasady.Losuj(Zasady.Id(last)));
        Tablicuj();
    }
    void OnDestroy() { if (rt) { rt.Release(); Destroy(rt); } }

    float U() => (float)los.NextDouble() * 2f - 1f;
    // the balls lie in the drum's bottom (before a show and after it)
    void Rozsyp()
    {
        float r = bebenR - kulaR;
        for (int i = 0; i < kule.Length; i++)
        {
            var d = new Vector3(U(), -Mathf.Abs(U()) - .3f, U());
            p[i] = d.normalized * r * Mathf.Lerp(.55f, 1f, (float)los.NextDouble()); v[i] = Vector3.zero; wDrodze[i] = false;
            if (kule[i]) { kule[i].localPosition = p[i]; kule[i].localRotation = Quaternion.Euler(U() * 180f, U() * 180f, U() * 180f); }
        }
        for (int k = 0; k < 6; k++) wyszla[k] = false;
    }
    // after a draw: its six in the cups (numbers to the camera), the others in the drum
    void PolozWyniki(int[] w)
    {
        Rozsyp(); wynik = w;
        for (int k = 0; k < w.Length && k < cele.Length; k++)
        {
            int i = w[k] - 1; if (i < 0 || i >= kule.Length || !kule[i]) continue;
            wDrodze[i] = true; wyszla[k] = true; p[i] = cele[k]; kule[i].localPosition = cele[k]; Twarz(i);
        }
    }
    void Twarz(int i)
    {
        if (!kamera || !kule[i]) return;
        var d = kamera.transform.position - kule[i].position; d.y = 0f;
        if (d.sqrMagnitude > 1e-6f) kule[i].rotation = Quaternion.LookRotation(d.normalized, Vector3.up);
    }

    /// <summary>The show of the draw id (its numbers from its date), from second od on; replay = POWTÓRKA, else live.</summary>
    public void Pokaz(int id, float od, bool replay)
    {
        var w = Zasady.Losuj(id);
        PolozWyniki(new int[0]); pokazId = id; wynik = w; t = od; pokaz = true; powtorka = replay; brudna = true;
        // joining a running show: the balls already out are in their cups
        for (int k = 0; k < 6; k++)
            if (t >= Zasady.StartKuli(k) + Zasady.Wyjscie + Zasady.Toczenie) { int i = w[k] - 1; wDrodze[i] = true; wyszla[k] = true; p[i] = cele[k]; kule[i].localPosition = cele[k]; Twarz(i); }
    }

    void Update()
    {
        float dt = Mathf.Min(Time.deltaTime, .05f);
        // the clock: the live draw (Wednesday, Saturday 20:00); checked once a second
        zegar -= dt;
        if (zegar <= 0f)
        {
            zegar = 1f; var now = DateTime.Now; var ost = Zasady.Ostatnie(now);
            if (ost != DateTime.MinValue)
            {
                float od = (float)(now - ost).TotalSeconds; int id = Zasady.Id(ost);
                if (od < Zasady.Program && !(pokaz && !powtorka && pokazId == id)) Pokaz(id, od, false);
                int stan = id; if (stan != ostatniStan) { ostatniStan = stan; brudna = true; }
            }
        }
        if (pokaz)
        {
            t += dt;
            Mieszaj(dt, t > Zasady.Intro && t < Zasady.StartKuli(5) + Zasady.Wyjscie);
            for (int k = 0; k < 6; k++)
            {
                float s = Zasady.StartKuli(k); int i = wynik[k] - 1;
                if (t < s || wyszla[k] || i < 0 || i >= kule.Length) continue;
                wDrodze[i] = true;
                float a = t - s;
                if (a < Zasady.Wyjscie) p[i] = Vector3.Lerp(p[i], droga.Length > 0 ? droga[0] : p[i], 1f - Mathf.Exp(-dt * 5f));
                else if (a < Zasady.Wyjscie + Zasady.Toczenie)
                {
                    float f = Mathf.SmoothStep(0f, 1f, (a - Zasady.Wyjscie) / Zasady.Toczenie);
                    p[i] = Po(f, cele[k]);
                    kule[i].Rotate(Vector3.right, 720f * dt, Space.Self);
                }
                else { p[i] = cele[k]; wyszla[k] = true; Twarz(i); brudna = true; Debug.Log("LOTOTO_KULA " + (k + 1) + " = " + wynik[k]); }
                kule[i].localPosition = p[i];
            }
            if (t >= Zasady.Program) { pokaz = false; brudna = true; }
            Tablicuj();
        }
        else if (!Spokoj()) Mieszaj(dt, false);
        // the camera: 15 frames a second while a show runs near the player; one frame when the board changes
        if (kamera && rt)
        {
            bool blisko = Blisko(60f);
            if (pokaz && blisko && Time.unscaledTime >= nastepnaKlatka) { nastepnaKlatka = Time.unscaledTime + 1f / 15f; Klatka(t < Zasady.Koniec ? ujecieBeben : ujecieTablica); }
            else if (!pokaz && brudna && blisko) { brudna = false; Tablicuj(); Klatka(ujecieTablica); }
        }
    }
    bool Spokoj() { for (int i = 0; i < v.Length; i++) if (!wDrodze[i] && v[i].sqrMagnitude > 1e-4f) return false; return true; }
    bool Blisko(float m)
    {
        var g = BraxChampionsGry.Gracz; if (!g) return false;
        float s = Mathf.Max(.01f, transform.lossyScale.y);
        return (g.transform.position - (beben ? beben.position : transform.position)).sqrMagnitude < m * m * s * s;
    }
    void Klatka(Transform u)
    {
        if (u) kamera.transform.SetPositionAndRotation(u.position, u.rotation);
        kamera.Render();
    }
    // a point f (0..1) down the chute, then on to the cup
    Vector3 Po(float f, Vector3 cup)
    {
        int n = droga.Length; if (n == 0) return cup;
        float total = drogaL[n - 1] + Vector3.Distance(droga[n - 1], cup), s = f * total;
        for (int i = 1; i < n; i++) if (s <= drogaL[i]) return Vector3.Lerp(droga[i - 1], droga[i], (s - drogaL[i - 1]) / Mathf.Max(1e-5f, drogaL[i] - drogaL[i - 1]));
        return Vector3.Lerp(droga[n - 1], cup, (s - drogaL[n - 1]) / Mathf.Max(1e-5f, total - drogaL[n - 1]));
    }

    // the balls in the drum (its frame, metres): gravity, the air jets from the bottom while it mixes, the glass wall, the
    // balls on each other
    public void Mieszaj(float dt, bool dmuch)
    {
        int n = kule.Length; float lim = bebenR - kulaR, d2 = 4f * kulaR * kulaR;
        for (int sub = 0; sub < 2; sub++)
        {
            float h = dt * .5f;
            for (int i = 0; i < n; i++)
            {
                if (wDrodze[i]) continue;
                v[i] += Vector3.down * 9.81f * h;
                if (dmuch && p[i].y < -lim * .35f)
                    v[i] += new Vector3((float)los.NextDouble() * 2f - 1f, 1.6f + (float)los.NextDouble(), (float)los.NextDouble() * 2f - 1f) * 26f * h;
                p[i] += v[i] * h;
                float m = p[i].magnitude;
                if (m > lim) { var nn = p[i] / m; p[i] = nn * lim; float vn = Vector3.Dot(v[i], nn); if (vn > 0f) v[i] -= 1.55f * vn * nn; v[i] *= .97f; }
            }
            for (int i = 0; i < n; i++)
            {
                if (wDrodze[i]) continue;
                for (int j = i + 1; j < n; j++)
                {
                    if (wDrodze[j]) continue;
                    var d = p[j] - p[i]; float q = d.sqrMagnitude;
                    if (q >= d2 || q < 1e-10f) continue;
                    float dl = Mathf.Sqrt(q); var nn = d / dl; float push = (2f * kulaR - dl) * .5f;
                    p[i] -= nn * push; p[j] += nn * push;
                    float rel = Vector3.Dot(v[j] - v[i], nn);
                    if (rel < 0f) { var imp = nn * rel * .9f; v[i] += imp; v[j] -= imp; }
                }
            }
            // the glass again (a push from the others never puts a ball through it)
            for (int i = 0; i < n; i++) { if (wDrodze[i]) continue; float m = p[i].magnitude; if (m > lim) p[i] *= lim / m; }
        }
        for (int i = 0; i < n; i++)
        {
            if (wDrodze[i] || !kule[i]) continue;
            if (!dmuch && v[i].sqrMagnitude < 1e-3f && p[i].y < -lim * .3f) v[i] = Vector3.zero;
            kule[i].localPosition = p[i];
            if (v[i].sqrMagnitude > 1e-4f) kule[i].Rotate(new Vector3(v[i].z, 0f, -v[i].x), v[i].magnitude / kulaR * Mathf.Rad2Deg * dt, Space.World);
        }
    }
    public Vector3 Pozycja(int i) => p[i];
    public bool WDrodze(int i) => wDrodze[i];

    // ---------------- the results board (the studio's screen, the atlas's quads)
    void Tablicuj()
    {
        if (warstwa == null) return;
        warstwa.Czysc();
        var now = DateTime.Now; var nast = Zasady.Nastepne(now.AddSeconds(1));
        float W = tablicaW, H = tablicaH;
        { var r = Tablica.Tlo; warstwa.Q(0f, 0f, W, H, r.x, r.y, r.width, r.height, -.001f); }
        // a line of words and numbers, centred: a word is its ink only (its cell's navy round it would cover its neighbours)
        float Szer(int i) { var r = Tablica.Slowo(i); float f = i < slowaSzer.Length && slowaSzer[i] > .01f ? slowaSzer[i] : 1f; return Mathf.Min(r.width, Mathf.Ceil(r.width * f) + 10f); }
        float Znaki(string t) { float n = 0f; foreach (char c in t) n += c == ' ' ? .45f : 1f; return n; }        // a thin space between thousands
        float Wid(object o, float h) => o is int i ? h * Szer(i) / Tablica.Slowo(i).height : h * Tablica.CyfraW / Tablica.CyfraH * Znaki((string)o);
        void Linia(float cy, float h, params object[] items)
        {
            float gap = h * .45f, tot = -gap; foreach (var o in items) tot += Wid(o, h) + gap;
            float x = -tot * .5f;
            foreach (var o in items)
            {
                float w = Wid(o, h);
                if (o is int i) { var r = Tablica.Slowo(i); float pw = Szer(i); warstwa.Q(x + w * .5f, cy, w, h, r.x + Mathf.RoundToInt((r.width - pw) * .5f), r.y, Mathf.RoundToInt(pw), r.height); }
                else
                {
                    float cw = h * Tablica.CyfraW / Tablica.CyfraH, cx = x + cw * .5f;   // the centre of the next digit
                    foreach (char c in (string)o)
                    {
                        int d = c == ':' ? 10 : c == '.' ? 11 : c - '0';
                        if (c == ' ') { cx += cw * .45f; continue; }
                        if (d >= 0 && d <= 11) { var r = Tablica.Cyfra(d); warstwa.Q(cx, cy, cw, h, r.x, r.y, r.width, r.height, .001f); }
                        cx += cw;
                    }
                }
                x += w + gap;
            }
        }
        // the title with the draw's day, the six balls (in the order they came out while live, ascending after), the
        // kumulacja, the next draw
        var dzien = pokaz ? Zasady.Data(pokazId) : Zasady.Ostatnie(now);
        int tytul = pokaz ? (powtorka ? Tablica.Powtorka : Tablica.NaZywo) : Tablica.Wyniki;
        if (dzien != DateTime.MinValue) Linia(H * .36f, H * .11f, tytul, Tablica.Niedziela + (int)dzien.DayOfWeek); else Linia(H * .36f, H * .11f, Tablica.Marka);
        int[] pokazane = new int[0];
        if (wynik != null)
        {
            var l = new List<int>(); for (int k = 0; k < wynik.Length; k++) if (!pokaz || wyszla[k]) l.Add(wynik[k]);
            pokazane = l.ToArray(); if (!pokaz) Array.Sort(pokazane);
        }
        float kd = H * .19f, kx = -kd * 1.18f * 2.5f;
        for (int k = 0; k < 6; k++)
        {
            float cx = kx + k * kd * 1.18f, cy = H * .12f; var r = k < pokazane.Length ? Tablica.Kula(pokazane[k]) : Tablica.Pusta;
            warstwa.Q(cx, cy, kd, kd, r.x, r.y, r.width, r.height);
        }
        string kum = Zasady.Kumulacja(pokaz ? Zasady.Data(pokazId) : nast).ToString("N0", System.Globalization.CultureInfo.InvariantCulture).Replace(',', ' ');
        Linia(-H * .14f, H * .09f, Tablica.Kumul, kum, Tablica.Zl);
        Linia(-H * .34f, H * .07f, Tablica.Nastepne, Tablica.Niedziela + (int)nast.DayOfWeek, "20:00");
        warstwa.Zapisz(tablicaMesh);
    }

    // ---------------- AKCJA: the counter (buy / check the coupons), the glass (replay)
    float Odl(Transform t, Vector3 g)
    {
        if (!t) return float.MaxValue; float s = Mathf.Max(.01f, t.lossyScale.y); var d = g - t.position;
        if (Mathf.Abs(d.y) > 1.6f * s) return float.MaxValue; d.y = 0f; return d.magnitude / s;
    }
    List<(int id, int[] liczby)> Kupony => Zasady.Czytaj(PlayerPrefs.GetString(KluczKupony, ""));
    int DoSprawdzenia(List<(int id, int[] liczby)> l, DateTime now)
    {
        int n = 0; foreach (var c in l) if (Zasady.Data(c.id).AddSeconds(Zasady.Program) <= now) n++; return n;
    }
    public string Kontekst(Vector3 g)
    {
        var now = DateTime.Now;
        if (Odl(lada, g) < 1.3f)
        {
            var l = Kupony; int n = DoSprawdzenia(l, now);
            if (n > 0) return "LOTOTO · SPRAWDŹ KUPONY (" + n + ")";
            var nast = Zasady.Nastepne(now);
            return "LOTOTO BRAX CASH · KUPON " + Zasady.Cena + " ZŁ · " + Zasady.Nazwa(nast) + " 20:00";
        }
        if (Odl(szyba, g) < 1.6f && !pokaz && Zasady.Ostatnie(now) != DateTime.MinValue) return "LOTOTO · POWTÓRKA LOSOWANIA (" + Zasady.Nazwa(Zasady.Ostatnie(now)) + ")";
        return "";
    }
    public bool Wykonaj(Vector3 g)
    {
        var now = DateTime.Now;
        if (Odl(lada, g) < 1.3f)
        {
            var l = Kupony; int n = DoSprawdzenia(l, now);
            if (n > 0)
            {
                long suma = 0; int best = 0; var zostaja = new List<(int, int[])>();
                foreach (var c in l)
                {
                    var termin = Zasady.Data(c.id);
                    if (termin.AddSeconds(Zasady.Program) > now) { zostaja.Add(c); continue; }
                    int h = Zasady.Trafienia(c.liczby, Zasady.Losuj(c.id)); best = Mathf.Max(best, h); suma += Zasady.Wygrana(h, termin);
                }
                PlayerPrefs.SetString(KluczKupony, Zasady.Zapisz(zostaja)); PlayerPrefs.Save();
                if (suma > 0) BraxChampionsKasyno.Wplac((int)Math.Min(int.MaxValue, suma));
                BraxChampionsGry.Pokaz(suma > 0 ? (best >= 6 ? "SZÓSTKA! KUMULACJA " : "WYGRANA · " + best + " TRAFIENIA · ") + suma + " ZŁ" : "LOTOTO · " + n + " KUPON(Y) BEZ WYGRANEJ · NAJWIĘCEJ TRAFIEŃ: " + best, 3.5f);
                Debug.Log("LOTOTO_SPRAWDZENIE kupony=" + n + " trafienia_max=" + best + " wygrana=" + suma);
                return true;
            }
            var nast = Zasady.Nastepne(now); int id = Zasady.Id(nast), naTo = 0;
            foreach (var c in l) if (c.id == id) naTo++;
            if (naTo >= Zasady.MaxKuponow) { BraxChampionsGry.Pokaz("LOTOTO · NA TO LOSOWANIE MASZ JUŻ " + naTo + " KUPONÓW", 2.6f); return true; }
            if (!BraxChampionsKasyno.Wydaj(Zasady.Cena)) { BraxChampionsGry.Pokaz("LOTOTO · ZA MAŁO ŚRODKÓW · KREDYT " + BraxChampionsKasyno.Kredyt, 2.6f); return true; }
            var w = Zasady.ChybilTrafil(los); l.Add((id, w));
            PlayerPrefs.SetString(KluczKupony, Zasady.Zapisz(l)); PlayerPrefs.Save();
            BraxChampionsGry.Pokaz("KUPON " + Zasady.Nazwa(nast) + " 20:00 · " + Zasady.Liczby(w) + " · POWODZENIA!", 3.5f);
            return true;
        }
        if (Odl(szyba, g) < 1.6f && !pokaz)
        {
            var last = Zasady.Ostatnie(now); if (last == DateTime.MinValue) return false;
            Pokaz(Zasady.Id(last), 0f, true);
            BraxChampionsGry.Pokaz("LOTOTO · POWTÓRKA LOSOWANIA · " + Zasady.Nazwa(last), 2.4f);
            return true;
        }
        return false;
    }
}
