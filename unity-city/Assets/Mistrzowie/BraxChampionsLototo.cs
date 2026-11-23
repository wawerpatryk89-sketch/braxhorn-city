using System;
using System.Collections.Generic;
using UnityEngine;

// MIASTO MISTRZÓW - LOTOTO BRAX CASH (owner 16.11 22:52-23:08: "Kolektura ... i w telewizorach losowanie", "taki pokoik i
// gościa, który będzie przy tym bębnie ... automatycznie ... 2 razy w tygodniu", the name "Lototo Brax Cash", the studio "w
// tym BRAXHORN BET", his poster: "DO WYGRANIA 10 000 000 ZŁ · DWA LOSOWANIA W TYGODNIU · WYNIKI NA ŻYWO W TELEWIZJI ·
// CODZIENNIE O 20:00"; 23:42 "60 kuleczek ... losujemy 20", 17.11 ten numbers on the coupon). The lottery of the BET:
//  - the kolektura (its counter in the BRAXHORN BET): AKCJA buys a coupon "chybił trafił" of ten numbers of 60 for the next
//    draw (3 zł); after the draw AKCJA checks the player's coupons and pays by the hits of the 20 drawn (Zasady.BraxCash):
//    5 hits the stake back, 6 hits 6 zł, 7 hits 25 zł, 8 hits 200 zł, 9 hits 5 000 zł, all ten the KUMULACJA (from
//    10 000 000 zł, his poster; it grows by 2 500 000 a draw nobody hits, someone in the country hits it on one draw in nine);
//  - the draws: Wednesday and Saturday at 20:00 by the phone's clock, live in the studio behind the glass - the host by
//    the machine, the 60 balls mixing in the glass drum, twenty coming out one by one down the chute into the cups of the
//    rail (two rows of ten); the studio camera's picture on every screen of the BET (a live picture of the game, not a
//    film); on the other days at 20:00 (and between the draws) the camera shows the results board;
//  - the numbers come from the draw's date: the same on every phone, as one real draw; a draw the player missed is on the
//    board, and AKCJA at the studio's glass replays it.
// The same component with gra = 1 is BRAX SZÓSTKA (6 of 40) for the kolektura of the city R1, placed there by its builder.
// The money is the casino's credit (BraxChampionsKasyno) until the game has its wallet. Zasady is pure (automaty_sim).
public sealed class BraxChampionsLototo : MonoBehaviour, IBraxInterakcja
{
    // The rules of one game. LOTOTO BRAX CASH is the BET's (owner 16.11 23:42 "W jednej 60 kuleczek", "60 losujemy 20", 17.11
    // "10 liczb" on the coupon - a game of the kind of the real Multi Multi, where 20 of 80 are drawn); BRAX SZÓSTKA is the
    // second kolektura's, in the city R1 (owner 16.11 23:42 "a 40 tylko 6", the name 17.11): a classic lotto 6 of 40, as the
    // real NZ Lotto. R1 is another contractor's region: there this component is placed by its own builder (gra = 1); its
    // prize table, kumulacja and schedule below are a proposal until the owner says otherwise.
    public sealed class Zasady
    {
        public string nazwa, klucz; public int pula, losowane, typy, cena, maxKuponow = 10, godzina = 20, marka;
        public int[] wygrane;                                   // by hits (0 .. typy - 1); all typy = the kumulacja
        // MAŁY LOTEK (owner 23.11 16:59 "wybierasz mały lotek czy duży lotek"): fewer numbers on the same draw, cheaper,
        // smaller prizes, no kumulacja; 0 = the game has none
        public int malyTypy, malyCena; public int[] malyWygrane = new int[0];
        public int kumulacjaStart, kumulacjaWzrost, kumulacjaMax, krajCo;   // someone in the country hits it on one draw in krajCo
        public DayOfWeek[] dni; public DateTime pierwsze;
        // the show: the intro, the mixing, a ball every odstep s (out of the drum, then down the chute), the board
        public float intro = 6f, mieszanie = 12f, odstep = 8f, wyjscie = 2f, toczenie = 3f;
        public float StartKuli(int k) => intro + mieszanie + k * odstep;
        public float Koniec => StartKuli(losowane - 1) + wyjscie + toczenie + 12f;         // then the camera shows the board
        public float Program => Koniec + 20f;

        public static readonly Zasady BraxCash = new Zasady
        {
            nazwa = "LOTOTO BRAX CASH", klucz = "braxhorn_lototo_kupony", marka = Tablica.Marka, pula = 60, losowane = 20, typy = 10, cena = 3,
            // 3 zł a coupon of ten numbers: 5 hits give the stake back, 6 hits 6 zł, 7 hits 25 zł, 8 hits 200 zł, 9 hits 5 000 zł,
            // all ten the KUMULACJA from 10 000 000 zł (his poster) - one coupon in five wins something, 55 % back without it
            wygrane = new[] { 0, 0, 0, 0, 0, 3, 6, 25, 200, 5000 },
            // MAŁY LOTEK: five of 60 on the same twenty, 2 zł; 3 hits the stake back (1 in 6), 4 hits 10 zł (1 in 28), all five
            // 200 zł (1 in 352): 62 % back, as real lotteries
            malyTypy = 5, malyCena = 2, malyWygrane = new[] { 0, 0, 0, 2, 10, 200 },
            kumulacjaStart = 10000000, kumulacjaWzrost = 2500000, kumulacjaMax = 200000000, krajCo = 9,
            dni = new[] { DayOfWeek.Wednesday, DayOfWeek.Saturday }, pierwsze = new DateTime(2026, 11, 18, 20, 0, 0),
            odstep = 5f, wyjscie = 1.5f, toczenie = 2.5f,
        };
        public static readonly Zasady BraxSzostka = new Zasady
        {
            nazwa = "BRAX SZÓSTKA", klucz = "braxhorn_szostka_kupony", marka = Tablica.MarkaSzostka, pula = 40, losowane = 6, typy = 6, cena = 3,
            wygrane = new[] { 0, 0, 0, 12, 120, 3000 },        // a proposal: 3 hits 12 zł (1 in 32), 4 hits 120, 5 hits 3 000
            kumulacjaStart = 2000000, kumulacjaWzrost = 500000, kumulacjaMax = 50000000, krajCo = 9,
            dni = new[] { DayOfWeek.Wednesday, DayOfWeek.Saturday }, pierwsze = new DateTime(2026, 11, 18, 20, 0, 0),
        };
        public static Zasady Gra(int i) => i == 1 ? BraxSzostka : BraxCash;

        public bool Dzien(DateTime d) { foreach (var x in dni) if (d.DayOfWeek == x) return true; return false; }
        public DateTime Termin(DateTime day) => new DateTime(day.Year, day.Month, day.Day, godzina, 0, 0);
        /// <summary>The first draw starting at t or later.</summary>
        public DateTime Nastepne(DateTime t)
        {
            var d = t.Date;
            for (int i = 0; i < 9; i++, d = d.AddDays(1)) { var s = Termin(d); if (Dzien(d) && s >= t && s >= pierwsze) return s; }
            return pierwsze;
        }
        /// <summary>The last draw that started at t or before (DateTime.MinValue before the first).</summary>
        public DateTime Ostatnie(DateTime t)
        {
            if (t < pierwsze) return DateTime.MinValue;
            var d = t.Date;
            for (int i = 0; i < 9; i++, d = d.AddDays(-1)) { var s = Termin(d); if (Dzien(d) && s <= t) return s; }
            return DateTime.MinValue;
        }
        public static int Id(DateTime d) => d.Year * 10000 + d.Month * 100 + d.Day;
        public DateTime Data(int id) => new DateTime(id / 10000, id / 100 % 100, id % 100, godzina, 0, 0);
        int[] Wybierz(System.Random r, int ile)
        {
            var a = new int[pula]; for (int i = 0; i < pula; i++) a[i] = i + 1;
            for (int i = 0; i < ile; i++) { int j = i + r.Next(pula - i); int t = a[i]; a[i] = a[j]; a[j] = t; }
            var w = new int[ile]; Array.Copy(a, w, ile); return w;
        }
        int Ziarno => pula * 100 + losowane;                   // two games never draw the same numbers on the same day
        /// <summary>The draw's numbers in the order they come out (from its date: the same on every phone).</summary>
        public int[] Losuj(int id) => Wybierz(new System.Random(unchecked(id * 7919 + 1049 + Ziarno)), losowane);
        /// <summary>A "chybił trafił" coupon, ascending.</summary>
        public int[] ChybilTrafil(System.Random r) { var w = Wybierz(r, typy); Array.Sort(w); return w; }
        public static int Trafienia(int[] kupon, int[] wynik) { int n = 0; foreach (int a in kupon) if (Array.IndexOf(wynik, a) >= 0) n++; return n; }
        /// <summary>Someone in the country hits the kumulacja in this draw (one draw in krajCo).</summary>
        public bool Glowna(int id) => new System.Random(unchecked(id * 31 + 7 + Ziarno)).Next(krajCo) == 0;
        /// <summary>The kumulacja played for in the draw starting at termin.</summary>
        public int Kumulacja(DateTime termin)
        {
            long k = kumulacjaStart;
            for (var d = pierwsze; d < termin; d = Nastepne(d.AddMinutes(1)))
                k = Glowna(Id(d)) ? kumulacjaStart : Math.Min(kumulacjaMax, k + kumulacjaWzrost);
            return (int)k;
        }
        public int Wygrana(int trafienia, DateTime termin) => trafienia >= typy ? Kumulacja(termin) : trafienia >= 0 ? wygrane[Math.Min(trafienia, wygrane.Length - 1)] : 0;
        /// <summary>A coupon's prize: ten numbers by the big table and the kumulacja, five by the MAŁY LOTEK table.</summary>
        public int Wygrana(int[] kupon, int trafienia, DateTime termin)
            => Maly(kupon) ? (trafienia >= 0 && malyWygrane.Length > 0 ? malyWygrane[Math.Min(trafienia, malyWygrane.Length - 1)] : 0) : Wygrana(trafienia, termin);
        public bool Maly(int[] kupon) => malyTypy > 0 && kupon != null && kupon.Length == malyTypy;
        public int Cena(bool maly) => maly && malyTypy > 0 ? malyCena : cena;
        public int Typy(bool maly) => maly && malyTypy > 0 ? malyTypy : typy;
        static readonly string[] DniNazwy = { "NIEDZIELA", "PONIEDZIAŁEK", "WTOREK", "ŚRODA", "CZWARTEK", "PIĄTEK", "SOBOTA" };
        public static string Nazwa(DateTime d) => DniNazwy[(int)d.DayOfWeek];
        public static string Liczby(int[] w) => string.Join(" ", Array.ConvertAll(w, x => x.ToString()));
        // the coupons (PlayerPrefs klucz): "id:a,b,c,...;" per coupon
        public List<(int id, int[] liczby)> Czytaj(string s)
        {
            var l = new List<(int, int[])>();
            foreach (var e in (s ?? "").Split(';'))
            {
                var p = e.Split(':'); if (p.Length != 2 || !int.TryParse(p[0], out int id)) continue;
                var n = p[1].Split(','); if (n.Length != typy && (malyTypy <= 0 || n.Length != malyTypy)) continue;
                var w = new int[n.Length]; bool ok = true;
                for (int i = 0; i < n.Length; i++) ok &= int.TryParse(n[i], out w[i]) && w[i] >= 1 && w[i] <= pula;
                if (ok && new HashSet<int>(w).Count == w.Length) l.Add((id, w));
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
    // the board's colour, the balls 1-63 with their numbers (7 a row), an empty ball, the words
    public static class Tablica
    {
        public const int W = 2048, H = 2048, CyfraW = 80, CyfraH = 120;
        public static readonly string[] Slowa = { "NA ŻYWO", "POWTÓRKA", "WYNIKI", "KUMULACJA", "NASTĘPNE LOSOWANIE", "ZŁ", "NIEDZIELA", "PONIEDZIAŁEK",
            "WTOREK", "ŚRODA", "CZWARTEK", "PIĄTEK", "SOBOTA", "LOSOWANIE", "LOTOTO BRAX CASH", "BRAX SZÓSTKA" };
        public const int NaZywo = 0, Powtorka = 1, Wyniki = 2, Kumul = 3, Nastepne = 4, Zl = 5, Niedziela = 6, Losowanie = 13, Marka = 14, MarkaSzostka = 15;
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
    public Transform[] miejsca = new Transform[0];            // the cups on the rail, one for each ball drawn
    public int gra;                                           // 0 = LOTOTO BRAX CASH (the BET), 1 = BRAX SZÓSTKA (R1)
    public Zasady Z => Zasady.Gra(gra);
    public Transform lada, szyba, prowadzacy;                 // the counter, the studio's glass (replay), the host
    public Transform pulpit;                                  // the coupon desk (AKCJA: the coupon on the whole screen, 23.11)
    public Camera kamera; public Transform ujecieBeben, ujecieTablica;
    public Material ekran;                                    // every screen showing the studio (the camera's picture)
    public MeshFilter tablica; public float tablicaW = 2.4f, tablicaH = 1.35f;
    public float[] slowaSzer = new float[0];                  // each word's ink width / its cell's (the generator measures them)
    public float kulaR = .035f, bebenR = .36f;                // metres
    public string nazwa = "LOTOTO BRAX CASH";

    // ---------------- the show
    bool pokaz, powtorka; float t; int[] wynik; int pokazId; bool[] wyszla = new bool[0];
    Vector3[] p, v; bool[] wDrodze; Vector3[] droga; float[] drogaL; Vector3[] cele = new Vector3[0];
    RenderTexture rt; float nastepnaKlatka; bool brudna = true; float zegar; int ostatniStan = -1;
    readonly System.Random los = new System.Random();
    Mesh tablicaMesh; BraxChampionsAutomatyGry.Warstwa warstwa;

    void OnEnable() { BraxInterakcje.Dodaj(this); }
    void OnDisable() { BraxInterakcje.Usun(this); }

    void Start()
    {
        int n = kule.Length; p = new Vector3[n]; v = new Vector3[n]; wDrodze = new bool[n];
        wyszla = new bool[Z.losowane]; cele = new Vector3[Z.losowane];
        // the way out in the drum's frame (it stays put: the studio is static)
        droga = new Vector3[tor.Length]; drogaL = new float[tor.Length];
        for (int i = 0; i < tor.Length; i++) { droga[i] = beben.InverseTransformPoint(tor[i].position); if (i > 0) drogaL[i] = drogaL[i - 1] + Vector3.Distance(droga[i], droga[i - 1]); }
        for (int k = 0; k < miejsca.Length && k < cele.Length; k++) cele[k] = beben.InverseTransformPoint(miejsca[k].position);
        Rozsyp();
        if (kamera)
        {
            kamera.enabled = false;
            rt = new RenderTexture(640, 360, 24, RenderTextureFormat.ARGB32) { name = "LOTOTO studio kamera", antiAliasing = 1 };   // 24-bit depth: at 16 the board flickered on the screens
            kamera.targetTexture = rt;
            if (ekran) { ekran.mainTexture = rt; if (ekran.HasProperty("_EmissionMap")) ekran.SetTexture("_EmissionMap", rt); }
        }
        if (tablica) { tablicaMesh = new Mesh { name = "LOTOTO tablica wyników" }; tablicaMesh.MarkDynamic(); tablica.sharedMesh = tablicaMesh; warstwa = new BraxChampionsAutomatyGry.Warstwa(Tablica.W, Tablica.H); }
        var last = Z.Ostatnie(DateTime.Now);
        if (last != DateTime.MinValue) PolozWyniki(Z.Losuj(Zasady.Id(last)));
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
        for (int k = 0; k < wyszla.Length; k++) wyszla[k] = false;
    }
    // after a draw: its numbers in the cups (to the camera), the others in the drum
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
        var w = Z.Losuj(id);
        PolozWyniki(new int[0]); pokazId = id; wynik = w; t = od; pokaz = true; powtorka = replay; brudna = true;
        // joining a running show: the balls already out are in their cups
        for (int k = 0; k < w.Length && k < cele.Length; k++)
            if (t >= Z.StartKuli(k) + Z.wyjscie + Z.toczenie) { int i = w[k] - 1; wDrodze[i] = true; wyszla[k] = true; p[i] = cele[k]; kule[i].localPosition = cele[k]; Twarz(i); }
    }

    void Update()
    {
        float dt = Mathf.Min(Time.deltaTime, .05f);
        // the clock: the live draw (Wednesday, Saturday 20:00); checked once a second
        zegar -= dt;
        if (zegar <= 0f)
        {
            zegar = 1f; var now = DateTime.Now; var ost = Z.Ostatnie(now);
            if (ost != DateTime.MinValue)
            {
                float od = (float)(now - ost).TotalSeconds; int id = Zasady.Id(ost);
                if (od < Z.Program && !(pokaz && !powtorka && pokazId == id)) Pokaz(id, od, false);
                int stan = id; if (stan != ostatniStan) { ostatniStan = stan; brudna = true; }
            }
        }
        if (pokaz)
        {
            t += dt;
            Mieszaj(dt, t > Z.intro && t < Z.StartKuli(Z.losowane - 1) + Z.wyjscie);
            for (int k = 0; k < wynik.Length && k < cele.Length; k++)
            {
                float s = Z.StartKuli(k); int i = wynik[k] - 1;
                if (t < s || wyszla[k] || i < 0 || i >= kule.Length) continue;
                wDrodze[i] = true;
                float a = t - s;
                if (a < Z.wyjscie) p[i] = Vector3.Lerp(p[i], droga.Length > 0 ? droga[0] : p[i], 1f - Mathf.Exp(-dt * 5f));
                else if (a < Z.wyjscie + Z.toczenie)
                {
                    float f = Mathf.SmoothStep(0f, 1f, (a - Z.wyjscie) / Z.toczenie);
                    p[i] = Po(f, cele[k]);
                    kule[i].Rotate(Vector3.right, 720f * dt, Space.Self);
                }
                else { p[i] = cele[k]; wyszla[k] = true; Twarz(i); brudna = true; Debug.Log("LOTOTO_KULA " + (k + 1) + " = " + wynik[k]); }
                kule[i].localPosition = p[i];
            }
            if (t >= Z.Program) { pokaz = false; brudna = true; }
            Tablicuj();
        }
        else if (!Spokoj()) Mieszaj(dt, false);
        // the camera: 15 frames a second while a show runs near the player; one frame when the board changes
        if (kamera && rt)
        {
            bool blisko = Blisko(60f);
            if (pokaz && blisko && Time.unscaledTime >= nastepnaKlatka) { nastepnaKlatka = Time.unscaledTime + 1f / 15f; Klatka(t < Z.Koniec ? ujecieBeben : ujecieTablica); }
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
        int subs = n > 50 ? 4 : 2;                             // 60 balls (the BET) need finer steps than 40 to stay apart
        for (int sub = 0; sub < subs; sub++)
        {
            float h = dt / subs;
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
        var now = DateTime.Now; var nast = Z.Nastepne(now.AddSeconds(1));
        float W = tablicaW, H = tablicaH;
        { var r = Tablica.Tlo; warstwa.Tlo(0f, 0f, W, H, r.x, r.y, r.width, r.height); }   // its own submesh, no depth (Warstwa.Tlo)
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
                        if (d >= 0 && d <= 11) { var r = Tablica.Cyfra(d); warstwa.Q(cx, cy, cw, h, r.x, r.y, r.width, r.height); }
                        cx += cw;
                    }
                }
                x += w + gap;
            }
        }
        // the title with the draw's day, the balls drawn (in the order they came out while live, ascending after; ten a row),
        // the kumulacja, the next draw
        var dzien = pokaz ? Z.Data(pokazId) : Z.Ostatnie(now);
        int tytul = pokaz ? (powtorka ? Tablica.Powtorka : Tablica.NaZywo) : Tablica.Wyniki;
        int rzedy = (Z.losowane + 9) / 10, wRzedzie = Mathf.Min(10, Z.losowane); bool dwa = rzedy > 1;
        float yT = dwa ? .39f : .36f, hT = dwa ? .09f : .11f;
        if (dzien != DateTime.MinValue) Linia(H * yT, H * hT, tytul, Tablica.Niedziela + (int)dzien.DayOfWeek); else Linia(H * yT, H * hT, Z.marka);
        int[] pokazane = new int[0];
        if (wynik != null)
        {
            var l = new List<int>(); for (int k = 0; k < wynik.Length; k++) if (!pokaz || wyszla[k]) l.Add(wynik[k]);
            pokazane = l.ToArray(); if (!pokaz) Array.Sort(pokazane);
        }
        float kd = dwa ? Mathf.Min(H * .14f, W * .9f / (wRzedzie * 1.12f)) : H * .19f, kx = -kd * 1.12f * (wRzedzie - 1) * .5f;
        for (int k = 0; k < Z.losowane; k++)
        {
            int rz = k / wRzedzie, kk = k % wRzedzie;
            float cx = kx + kk * kd * 1.12f, cy = dwa ? H * (.21f - rz * .155f) : H * .12f;
            var r = k < pokazane.Length ? Tablica.Kula(pokazane[k]) : Tablica.Pusta;
            warstwa.Q(cx, cy, kd, kd, r.x, r.y, r.width, r.height);
        }
        string kum = Z.Kumulacja(pokaz ? Z.Data(pokazId) : nast).ToString("N0", System.Globalization.CultureInfo.InvariantCulture).Replace(',', ' ');
        Linia(dwa ? -H * .17f : -H * .14f, H * (dwa ? .08f : .09f), Tablica.Kumul, kum, Tablica.Zl);
        Linia(dwa ? -H * .36f : -H * .34f, H * (dwa ? .065f : .07f), Tablica.Nastepne, Tablica.Niedziela + (int)nast.DayOfWeek, Z.godzina + ":00");
        warstwa.Zapisz(tablicaMesh);
    }

    // ---------------- AKCJA: the counter (buy / check the coupons), the glass (replay)
    float Odl(Transform t, Vector3 g)
    {
        if (!t) return float.MaxValue; float s = Mathf.Max(.01f, t.lossyScale.y); var d = g - t.position;
        if (Mathf.Abs(d.y) > 1.6f * s) return float.MaxValue; d.y = 0f; return d.magnitude / s;
    }
    List<(int id, int[] liczby)> Kupony => Z.Czytaj(PlayerPrefs.GetString(Z.klucz, ""));
    int DoSprawdzenia(List<(int id, int[] liczby)> l, DateTime now)
    {
        int n = 0; foreach (var c in l) if (Z.Data(c.id).AddSeconds(Z.Program) <= now) n++; return n;
    }
    // a coupon filled at the desk and not paid yet (BraxChampionsLototoKupon "DO KASY"): its numbers, ascending
    string KluczDoKasy => Z.klucz + "_do_kasy";
    public int[] DoKasy
    {
        get
        {
            var l = Z.Czytaj("0:" + PlayerPrefs.GetString(KluczDoKasy, "") + ";");
            return l.Count == 1 ? l[0].liczby : null;
        }
        set
        {
            if (value == null) PlayerPrefs.DeleteKey(KluczDoKasy);
            else PlayerPrefs.SetString(KluczDoKasy, string.Join(",", Array.ConvertAll(value, x => x.ToString())));
            PlayerPrefs.Save();
        }
    }
    /// <summary>Pays a coupon (ten numbers: DUŻY LOTEK, five: MAŁY LOTEK) for the next draw; the till prints its receipt.</summary>
    public bool Kup(int[] liczby, out string blad)
    {
        blad = "";
        var now = DateTime.Now; var l = Kupony; var nast = Z.Nastepne(now); int id = Zasady.Id(nast), naTo = 0;
        bool maly = Z.Maly(liczby);
        if (liczby == null || liczby.Length != Z.Typy(maly)) { blad = "KUPON NIEPEŁNY"; return false; }
        foreach (var c in l) if (c.id == id) naTo++;
        if (naTo >= Z.maxKuponow) { blad = "NA TO LOSOWANIE MASZ JUŻ " + naTo + " KUPONÓW"; return false; }
        int cena = Z.Cena(maly);
        if (!BraxChampionsKasyno.Wydaj(cena)) { blad = "ZA MAŁO ŚRODKÓW · KREDYT " + BraxChampionsKasyno.Kredyt + " ZŁ"; return false; }
        var w = (int[])liczby.Clone(); Array.Sort(w); l.Add((id, w));
        PlayerPrefs.SetString(Z.klucz, Zasady.Zapisz(l)); PlayerPrefs.Save();
        int numer = PlayerPrefs.GetInt(Z.klucz + "_numer", 0) + 1; PlayerPrefs.SetInt(Z.klucz + "_numer", numer); PlayerPrefs.Save();
        BraxChampionsLototoKupon.Paragon(new[]
        {
            Z.nazwa, "KOLEKTURA BRAXHORN BET",
            (maly ? "MAŁY LOTEK · " : "DUŻY LOTEK · ") + w.Length + " Z " + Z.pula,
            "LOSOWANIE " + Zasady.Nazwa(nast) + " " + nast.ToString("dd.MM.yyyy") + " " + Z.godzina + ":00",
            string.Join("  ", Array.ConvertAll(w, x => x.ToString("00"))),
            "CENA " + cena + ",00 ZŁ · KUPON NR " + nast.ToString("MMdd") + "-" + numer.ToString("0000"),
            "POWODZENIA!",
        });
        BraxChampionsGry.Pokaz("KUPON KUPIONY · " + Zasady.Nazwa(nast) + " " + Z.godzina + ":00 · POWODZENIA!", 3f);
        Debug.Log("LOTOTO_KUPON kupiony gra=" + (maly ? "MALY" : "DUZY") + " liczby=" + Zasady.Liczby(w) + " losowanie=" + id + " cena=" + cena);
        return true;
    }
    public string Kontekst(Vector3 g)
    {
        var now = DateTime.Now;
        if (BraxChampionsLototoKupon.Otwarty) return "";
        if (Odl(pulpit, g) < 1.3f) return Z.nazwa + " · WYPEŁNIJ KUPON";
        if (Odl(lada, g) < 1.3f)
        {
            var l = Kupony; int n = DoSprawdzenia(l, now);
            if (n > 0) return Z.nazwa + " · SPRAWDŹ KUPONY (" + n + ")";
            var czeka = DoKasy;
            if (czeka != null) return Z.nazwa + " · ZAPŁAĆ KUPON · " + Z.Cena(Z.Maly(czeka)) + " ZŁ";
            return Z.nazwa + " · KUPON · " + Zasady.Nazwa(Z.Nastepne(now)) + " " + Z.godzina + ":00";
        }
        if (Odl(szyba, g) < 1.6f && !pokaz && Z.Ostatnie(now) != DateTime.MinValue) return Z.nazwa + " · POWTÓRKA LOSOWANIA (" + Zasady.Nazwa(Z.Ostatnie(now)) + ")";
        return "";
    }
    public bool Wykonaj(Vector3 g)
    {
        var now = DateTime.Now;
        if (BraxChampionsLototoKupon.Otwarty) return true;
        if (Odl(pulpit, g) < 1.3f) { BraxChampionsLototoKupon.Otworz(this, false); return true; }
        if (Odl(lada, g) < 1.3f)
        {
            var l = Kupony; int n = DoSprawdzenia(l, now);
            if (n > 0)
            {
                long suma = 0; int best = 0; bool kumulacja = false; var zostaja = new List<(int, int[])>();
                foreach (var c in l)
                {
                    var termin = Z.Data(c.id);
                    if (termin.AddSeconds(Z.Program) > now) { zostaja.Add(c); continue; }
                    int h = Zasady.Trafienia(c.liczby, Z.Losuj(c.id)); best = Mathf.Max(best, h); suma += Z.Wygrana(c.liczby, h, termin);
                    kumulacja |= !Z.Maly(c.liczby) && h >= Z.typy;
                }
                PlayerPrefs.SetString(Z.klucz, Zasady.Zapisz(zostaja)); PlayerPrefs.Save();
                if (suma > 0) BraxChampionsKasyno.Wplac((int)Math.Min(int.MaxValue, suma));
                BraxChampionsGry.Pokaz(suma > 0 ? (kumulacja ? "KUMULACJA! " + best + " TRAFIEŃ · " : "WYGRANA · " + best + " TRAFIEŃ · ") + suma + " ZŁ" : Z.nazwa + " · " + n + " KUPON(Y) BEZ WYGRANEJ · NAJWIĘCEJ TRAFIEŃ: " + best, 3.5f);
                Debug.Log("LOTOTO_SPRAWDZENIE kupony=" + n + " trafienia_max=" + best + " wygrana=" + suma);
                return true;
            }
            var czeka = DoKasy;
            if (czeka != null)
            {
                if (Kup(czeka, out string blad)) DoKasy = null;
                else BraxChampionsGry.Pokaz(Z.nazwa + " · " + blad, 2.6f);
                return true;
            }
            BraxChampionsLototoKupon.Otworz(this, true);             // at the till: filled and paid on the spot
            return true;
        }
        if (Odl(szyba, g) < 1.6f && !pokaz)
        {
            var last = Z.Ostatnie(now); if (last == DateTime.MinValue) return false;
            Pokaz(Zasady.Id(last), 0f, true);
            BraxChampionsGry.Pokaz(Z.nazwa + " · POWTÓRKA LOSOWANIA · " + Zasady.Nazwa(last), 2.4f);
            return true;
        }
        return false;
    }
}
