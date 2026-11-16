using System;
using UnityEngine;

// MIASTO MISTRZÓW - the casinos' roulette tables, working (owner 16.11 19:13: "ruletki porobić prawdziwe, wszystko porób",
// in both casinos). A European single-zero wheel as the real ones: the rotor turns slowly all the time; AKCJA at the table
// puts a 10 chip on the outside box in front of the player (1-18, PARZYSTE, CZERWONE, CZARNE, NIEPARZYSTE, 19-36 - the box is
// the one the player stands at, as at a real table) and the croupier launches the ball: it runs against the rotor on the
// bowl's track, slows, drops down the cone past the deflectors into the pocket ring, hops over a few frets and stays in the
// pocket of the number drawn; the screen on its column by the wheel shows it with the 12 before it, red / zero / black of the last 100 and the hot and cold numbers; even money pays 1:1, zero takes the
// bets. The money is the casino credit (BraxChampionsKasyno, the slot machines' too).
public sealed class BraxChampionsRuletka : MonoBehaviour, IBraxInterakcja
{
    [Serializable] public sealed class Stol
    {
        public Transform stol;                                   // the table: origin on the floor, +z the players' side
        public Transform rotor;                                  // turns about its local y (the wheel's centre)
        public Transform kulka;                                  // the ball, a child of the table
        public Vector3 kolo;                                     // the wheel's centre in the table's frame (the rotor's base)
        public Transform[] zetony = new Transform[0];            // a chip on each of the six outside boxes (shown while bet)
        public float[] polaX = new float[0];                     // the six boxes' centres along the table (local x)
        public MeshFilter wyswietlacz;                           // the winning-number display's live layer (Wyswietlacz)
    }
    public Stol[] stoly = new Stol[0];
    public string nazwa = "RULETKA";
    public int stawka = 10;
    // the wheel's sizes (local metres, from the wheel's centre): the ball's track on the bowl and the pockets on the rotor
    public float torR = .37f, torY = .052f, kieszenR = .215f, kieszenY = .012f, kulkaR = .0095f;

    sealed class Stan
    {
        public bool kreci; public float start, rotorKat, wynikKat, skala = 1f; public int wynik = -1, zaklad = -1, pole = -1;
        public int[] historia = new int[13]; public int ile;
        public int[] sto = new int[100]; public int stoN, stoI;           // the last 100 numbers (the display's statistics)
        public Mesh siatka;
    }
    Stan[] stan; System.Random los;
    const float Obrot = .35f * 360f;                             // the rotor: .35 turns a second (counter-clockwise)
    const float T1 = 5.2f, T2 = 6.6f, T3 = 8.4f;                 // on the track; falling in; settling; the end
    const float Om0 = 2.6f * 360f, Tau = 3.2f;                   // the ball's start speed (deg/s, clockwise) and its decay

    void OnEnable() { BraxInterakcje.Dodaj(this); }
    void OnDisable() { BraxInterakcje.Usun(this); }

    void Start()
    {
        los = new System.Random(Environment.TickCount ^ GetInstanceID());
        stan = new Stan[stoly.Length];
        for (int i = 0; i < stoly.Length; i++)
        {
            var s = new Stan(); stan[i] = s; var t = stoly[i]; if (t == null || !t.stol) continue;
            s.rotorKat = (float)los.NextDouble() * 360f;
            foreach (var z in t.zetony) if (z) z.gameObject.SetActive(false);
            Wyswietlacz.Poczatek(los, s.historia, s.sto);
            s.ile = s.historia.Length; s.stoN = s.sto.Length; s.stoI = 0;
            if (t.wyswietlacz)
            {
                s.siatka = new Mesh { name = "ruletka wyświetlacz" }; s.siatka.MarkDynamic();
                t.wyswietlacz.sharedMesh = s.siatka; Wyswietlacz.Zbuduj(s.siatka, s.historia, s.sto, s.stoN);
            }
            Kulka(t, s, 0f);
        }
    }

    // ---------------- interakcja: the box in front of the player
    int Najblizszy(Vector3 p, out int pole)
    {
        pole = -1; int best = -1; float bd = float.MaxValue;
        for (int i = 0; i < stoly.Length; i++)
        {
            var t = stoly[i]; if (t == null || !t.stol || t.polaX.Length == 0) continue;
            var l = t.stol.InverseTransformPoint(p);
            if (l.z < .55f || l.z > 1.8f || Mathf.Abs(l.y) > 1.2f) continue;           // in front of the table
            float lo = Mathf.Min(t.polaX[0], t.polaX[t.polaX.Length - 1]), hi = Mathf.Max(t.polaX[0], t.polaX[t.polaX.Length - 1]);
            if (l.x < lo - .5f || l.x > hi + .5f) continue;
            float d = l.z; if (d >= bd) continue;
            int k = 0; for (int j = 1; j < t.polaX.Length; j++) if (Mathf.Abs(l.x - t.polaX[j]) < Mathf.Abs(l.x - t.polaX[k])) k = j;
            bd = d; best = i; pole = k;
        }
        return best;
    }
    public string Kontekst(Vector3 gracz)
    {
        int i = Najblizszy(gracz, out int pole); if (i < 0 || stan == null) return "";
        if (stan[i].kreci) return "RULETKA · KULKA W GRZE";
        return BraxChampionsKasyno.Kredyt >= stawka ? "POSTAW " + stawka + " · " + Zasady.Pola[pole] : "BRAK KREDYTU";
    }
    public bool Wykonaj(Vector3 gracz)
    {
        int i = Najblizszy(gracz, out int pole); if (i < 0 || stan == null) return false;
        var s = stan[i]; var t = stoly[i];
        if (s.kreci) return true;
        if (!BraxChampionsKasyno.Wydaj(stawka)) { BraxChampionsGry.Pokaz("BRAK KREDYTU", 1.6f); return true; }
        s.kreci = true; s.start = Time.time; s.zaklad = stawka; s.pole = pole; s.wynik = los.Next(37);
        if (pole < t.zetony.Length && t.zetony[pole]) t.zetony[pole].gameObject.SetActive(true);
        // where the ball has to stop: the drawn pocket, a pocket or two of chance either way as the ball settles
        float rotorEnd = s.rotorKat + Obrot * T3;
        s.wynikKat = Zasady.KatKieszeni(s.wynik);
        // the ball's angle when it reaches the pockets, unscaled, and the scale that brings it over the drawn pocket
        float bez = Kat(T2, 1f), rotorT2 = s.rotorKat + Obrot * T2;
        float chce = rotorT2 + s.wynikKat + (float)(los.NextDouble() * 2 - 1) * 20f;     // the pocket (+- a hop) in the table's frame
        float d = Mathf.DeltaAngle(90f - bez, chce);                                    // the ball runs clockwise (negative)
        s.skala = 1f - d / Mathf.Max(1f, Mathf.Abs(bez));
        BraxChampionsGry.Pokaz("RIEN NE VA PLUS", 1.4f);
        return true;
    }

    // the ball's travelled angle (deg, clockwise) at t: an exponential slowing, held from T2 on (it rides in the rotor)
    static float Kat(float t, float skala) { t = Mathf.Min(t, T2); return skala * Om0 * Tau * (1f - Mathf.Exp(-t / Tau)); }

    void Update()
    {
        if (stan == null) return;
        var g = BraxChampionsGry.Gracz; Vector3 gp = g ? g.transform.position : Vector3.one * 1e6f;
        for (int i = 0; i < stoly.Length; i++)
        {
            var t = stoly[i]; var s = stan[i]; if (t == null || !t.stol) continue;
            bool blisko = (t.stol.position - gp).sqrMagnitude < 45f * 45f;
            if (!blisko && !s.kreci) continue;
            s.rotorKat = Mathf.Repeat(s.rotorKat + Obrot * Time.deltaTime, 360f);
            if (t.rotor) t.rotor.localRotation = Quaternion.AngleAxis(-s.rotorKat, Vector3.up);   // counter-clockwise seen from above
            if (s.kreci)
            {
                float el = Time.time - s.start; Kulka(t, s, el);
                if (el >= T3) Koniec(t, s);
            }
            else Kulka(t, s, -1f);
        }
        int pole; int st = g ? Najblizszy(gp, out pole) : -1;
        if (st >= 0) BraxChampionsGry.Ustaw(nazwa + " · KREDYT " + BraxChampionsKasyno.Kredyt + " · OSTATNI " + Zasady.Opis(stan[st].historia[0]));
    }

    // the ball: el < 0 = resting in its last pocket (it rides with the rotor)
    void Kulka(Stol t, Stan s, float el)
    {
        if (!t.kulka) return;
        float kat, r, y;
        if (el < 0f || s.wynik < 0)
        {
            kat = s.rotorKat + (s.wynik >= 0 ? s.wynikKat : Zasady.KatKieszeni(s.historia[0])); r = kieszenR; y = kieszenY;
        }
        else if (el < T1)
        {
            kat = 90f - Kat(el, s.skala); r = torR; y = torY;                       // on the track (the start at the dealer's side)
        }
        else if (el < T2)
        {
            // down the cone: radius and height to the pockets, a bounce off a deflector on the way
            float u = (el - T1) / (T2 - T1), e = u * u * (3f - 2f * u);
            kat = 90f - Kat(el, s.skala);
            r = Mathf.Lerp(torR, kieszenR, e) + .010f * Mathf.Sin(u * Mathf.PI * 3f) * (1f - u);
            y = Mathf.Lerp(torY, kieszenY, e) + .012f * Mathf.Abs(Mathf.Sin(u * Mathf.PI * 2f)) * (1f - u);
        }
        else
        {
            // in the pockets: it rides with the rotor and hops from fret to fret into the drawn pocket
            float u = Mathf.Clamp01((el - T2) / (T3 - T2));
            float a0 = Mathf.DeltaAngle(0f, (90f - Kat(T2, s.skala)) - (s.rotorKat - Obrot * (el - T2)));
            float rel = Mathf.LerpAngle(a0, s.wynikKat, 1f - Mathf.Pow(1f - u, 3f));
            kat = s.rotorKat + rel;
            float hop = Mathf.Abs(Mathf.Sin(u * Mathf.PI * 4f)) * (1f - u);
            r = kieszenR + .006f * hop; y = kieszenY + .010f * hop;
        }
        float a = kat * Mathf.Deg2Rad;
        // angles counter-clockwise seen from above: x = cos, z = sin (the rotor turned by AngleAxis(-angle, up) agrees)
        t.kulka.localPosition = t.kolo + new Vector3(r * Mathf.Cos(a), y + kulkaR, r * Mathf.Sin(a));
    }

    void Koniec(Stol t, Stan s)
    {
        s.kreci = false;
        for (int k = s.historia.Length - 1; k > 0; k--) s.historia[k] = s.historia[k - 1];
        s.historia[0] = s.wynik; if (s.ile < s.historia.Length) s.ile++;
        s.sto[s.stoI] = s.wynik; s.stoI = (s.stoI + 1) % s.sto.Length; if (s.stoN < s.sto.Length) s.stoN++;
        int wyg = Zasady.Wygrywa(s.pole, s.wynik) ? s.zaklad * 2 : 0;
        if (wyg > 0) BraxChampionsKasyno.Wplac(wyg);
        BraxChampionsGry.Pokaz(Zasady.Opis(s.wynik) + (wyg > 0 ? " · WYGRANA " + wyg : " · " + Zasady.Pola[s.pole] + " PRZEGRYWA"), 3f);
        if (s.pole >= 0 && s.pole < t.zetony.Length && t.zetony[s.pole]) t.zetony[s.pole].gameObject.SetActive(false);
        s.zaklad = 0; s.pole = -1;
        if (s.siatka) Wyswietlacz.Zbuduj(s.siatka, s.historia, s.sto, s.stoN);
    }

    // ---------------- the winning-number display (after the TCS John Huxley column displays): a portrait screen .36 x .64 m
    // on a column at the wheel. Its fixed picture (frame, BRAXHORN, the labels) is the generator's; this live layer is quads
    // cut from one atlas of sharp pictures (the 37 numbers in their red / black / green discs in the region's sign typeface,
    // the digits, colour cells): the last number big, the twelve before it, red / zero / black over the last 100 in bars
    // with their percentages, the four hottest and the four coldest numbers.
    public static class Wyswietlacz
    {
        public const float W = .36f, H = .64f;
        public const int AtlasW = 1024, AtlasH = 1024, Krazek = 128, CyfraW = 64, CyfraH = 80;
        // atlas: discs 0-36 in 8 columns from the top-left; digits 0-9 and % in a row under them; colour cells under those
        public static void KrazekPx(int n, out int x, out int y) { x = n % 8 * Krazek; y = AtlasH - (n / 8 + 1) * Krazek; }
        public static void CyfraPx(int c, out int x, out int y) { x = c * CyfraW; y = AtlasH - 5 * Krazek - CyfraH; }
        public static void KolorPx(int k, out int x, out int y) { x = k * 32; y = AtlasH - 5 * Krazek - CyfraH - 32; }
        public static readonly float[] HistX = { -.125f, -.075f, -.025f, .025f, .075f, .125f };
        public const float DuzyY = .165f, DuzyD = .15f, HistY0 = .045f, HistY1 = -.005f, HistD = .044f,
            BarX0 = -.035f, BarL = .12f, BarH = .016f, ProcX = .128f, GoraceY = -.205f, ZimneY = -.262f, MalyD = .040f;
        public static readonly float[] BarY = { -.075f, -.103f, -.131f };          // red, zero, black
        public static readonly float[] HotX = { -.02f, .025f, .07f, .115f };

        static readonly System.Collections.Generic.List<Vector3> v = new System.Collections.Generic.List<Vector3>();
        static readonly System.Collections.Generic.List<Vector2> uv = new System.Collections.Generic.List<Vector2>();
        static readonly System.Collections.Generic.List<int> tr = new System.Collections.Generic.List<int>();
        // a picture's quad: cx / cy in the viewer's frame (x to his right, y up), facing +z; the atlas rectangle in pixels
        static void Q(float cx, float cy, float w, float h, int px, int py, int pw, int ph)
        {
            int i = v.Count; float u0 = (px + .5f) / AtlasW, u1 = (px + pw - .5f) / AtlasW, v0 = (py + .5f) / AtlasH, v1 = (py + ph - .5f) / AtlasH;
            // the viewer's right is the display's -x
            v.Add(new Vector3(-(cx - w * .5f), cy - h * .5f, 0)); v.Add(new Vector3(-(cx + w * .5f), cy - h * .5f, 0));
            v.Add(new Vector3(-(cx + w * .5f), cy + h * .5f, 0)); v.Add(new Vector3(-(cx - w * .5f), cy + h * .5f, 0));
            uv.Add(new Vector2(u0, v0)); uv.Add(new Vector2(u1, v0)); uv.Add(new Vector2(u1, v1)); uv.Add(new Vector2(u0, v1));
            tr.Add(i); tr.Add(i + 2); tr.Add(i + 1); tr.Add(i); tr.Add(i + 3); tr.Add(i + 2);     // facing +z (as the screen reels)
        }
        static void Dysk(int n, float cx, float cy, float d) { KrazekPx(n, out int x, out int y); Q(cx, cy, d, d, x, y, Krazek, Krazek); }
        static void Liczba(int val, float right, float cy, float h)
        {
            string t = val + "%"; float w = h * CyfraW / CyfraH, x = right - w * (t.Length - .5f);
            foreach (char c in t) { CyfraPx(c == '%' ? 10 : c - '0', out int px, out int py); Q(x, cy, w, h, px, py, CyfraW, CyfraH); x += w * .82f; }
        }
        // a table that has been played: 100 numbers in the order they fell (sto[99] the newest, so the next goes to sto[0]),
        // the history their last 13 (historia[0] the newest) - the game's start and the generator's sample alike
        public static void Poczatek(System.Random los, int[] historia, int[] sto)
        {
            for (int k = 0; k < sto.Length; k++) sto[k] = los.Next(37);
            for (int k = 0; k < historia.Length; k++) historia[k] = sto[sto.Length - 1 - k];
        }
        // historia[0] = the last number; sto = the last up to 100 (any order), n of them filled
        public static void Zbuduj(Mesh m, int[] historia, int[] sto, int n)
        {
            v.Clear(); uv.Clear(); tr.Clear();
            Dysk(historia[0], 0f, DuzyY, DuzyD);
            for (int k = 1; k < historia.Length && k <= 12; k++) Dysk(historia[k], HistX[(k - 1) % 6], k <= 6 ? HistY0 : HistY1, HistD);
            var licz = new int[37]; int red = 0, zero = 0, all = Mathf.Max(1, n);
            for (int i = 0; i < n; i++) { licz[sto[i]]++; if (sto[i] == 0) zero++; else if (Zasady.Czerwony(sto[i])) red++; }
            int[] p = { Mathf.RoundToInt(100f * red / all), Mathf.RoundToInt(100f * zero / all), 0 }; p[2] = Mathf.Max(0, 100 - p[0] - p[1]);
            int[] kol = { 0, 2, 1 };
            for (int b = 0; b < 3; b++)
            {
                float l = BarL * p[b] / 100f; KolorPx(kol[b], out int cx, out int cy);
                if (l > .001f) Q(BarX0 + l * .5f, BarY[b], l, BarH, cx + 8, cy + 8, 16, 16);
                Liczba(p[b], ProcX + .03f, BarY[b], BarH * 1.1f);
            }
            // the hottest (most often) and the coldest (least often) of the last 100; ties: the lower number first
            var hot = new int[37]; var cold = new int[37]; for (int i = 0; i < 37; i++) { hot[i] = i; cold[i] = i; }
            System.Array.Sort(hot, (a, b) => licz[b] != licz[a] ? licz[b].CompareTo(licz[a]) : a.CompareTo(b));
            System.Array.Sort(cold, (a, b) => licz[a] != licz[b] ? licz[a].CompareTo(licz[b]) : a.CompareTo(b));
            for (int i = 0; i < 4; i++) { Dysk(hot[i], HotX[i], GoraceY, MalyD); Dysk(cold[i], HotX[i], ZimneY, MalyD); }
            m.Clear(); m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(tr, 0);
            var nr = new Vector3[v.Count]; for (int i = 0; i < nr.Length; i++) nr[i] = Vector3.forward; m.normals = nr; m.RecalculateBounds();
        }
    }

    // ---------------- the rules (pure: the wheel, the colours, the bets)
    public static class Zasady
    {
        // the European wheel from the zero, CLOCKWISE seen from above (the real order: 0, 32, 15, 19, 4 ...)
        public static readonly int[] Kolo = { 0, 32, 15, 19, 4, 21, 2, 25, 17, 34, 6, 27, 13, 36, 11, 30, 8, 23, 10, 5, 24, 16, 33, 1, 20, 14, 31, 9, 22, 18, 29, 7, 28, 12, 35, 3, 26 };
        static readonly int[] Czerwone = { 1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 23, 25, 27, 30, 32, 34, 36 };
        public static bool Czerwony(int n) => Array.IndexOf(Czerwone, n) >= 0;
        public static readonly string[] Pola = { "1-18", "PARZYSTE", "CZERWONE", "CZARNE", "NIEPARZYSTE", "19-36" };
        public static bool Wygrywa(int pole, int n)
        {
            if (n <= 0) return false;                                             // the zero takes the outside bets
            switch (pole)
            {
                case 0: return n <= 18;
                case 1: return n % 2 == 0;
                case 2: return Czerwony(n);
                case 3: return !Czerwony(n);
                case 4: return n % 2 == 1;
                case 5: return n >= 19;
            }
            return false;
        }
        // the pocket's angle on the rotor (deg counter-clockwise from the zero's pocket: the order runs clockwise)
        public static float KatKieszeni(int n) => -Array.IndexOf(Kolo, n) * 360f / 37f;
        public static string Opis(int n) => n < 0 ? "-" : n + (n == 0 ? " ZERO" : Czerwony(n) ? " CZERWONE" : " CZARNE");
    }
}
