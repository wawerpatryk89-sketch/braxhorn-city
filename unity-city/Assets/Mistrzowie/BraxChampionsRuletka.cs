using System;
using UnityEngine;

// MIASTO MISTRZÓW - the casinos' roulette tables, working (owner 16.11 19:13: "ruletki porobić prawdziwe, wszystko porób",
// in both casinos). A European single-zero wheel as the real ones: the rotor turns slowly all the time; AKCJA at the table
// puts a 10 chip on the outside box in front of the player (1-18, PARZYSTE, CZERWONE, CZARNE, NIEPARZYSTE, 19-36 - the box is
// the one the player stands at, as at a real table) and the croupier launches the ball: it runs against the rotor on the
// bowl's track, slows, drops down the cone past the deflectors into the pocket ring, hops over a few frets and stays in the
// pocket of the number drawn; the board at the wheel shows it with the last numbers; even money pays 1:1, zero takes the
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
        public Renderer tablica;                                 // the history board (painted)
    }
    public Stol[] stoly = new Stol[0];
    public string nazwa = "RULETKA";
    public int stawka = 10;
    // the wheel's sizes (local metres, from the wheel's centre): the ball's track on the bowl and the pockets on the rotor
    public float torR = .37f, torY = .052f, kieszenR = .215f, kieszenY = .012f, kulkaR = .0095f;

    sealed class Stan
    {
        public bool kreci; public float start, rotorKat, wynikKat, skala = 1f; public int wynik = -1, zaklad = -1, pole = -1;
        public int[] historia = new int[12]; public int ile;
        public Texture2D tex; public Color32[] px;
    }
    Stan[] stan; System.Random los;
    public const int TabW = 64, TabH = 128;
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
            if (t.tablica)
            {
                s.tex = new Texture2D(TabW, TabH, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "ruletka tablica" };
                var mat = new Material(t.tablica.sharedMaterial) { name = "ruletka tablica" };
                mat.SetTexture("_MainTex", s.tex); if (mat.HasProperty("_EmissionMap")) mat.SetTexture("_EmissionMap", s.tex);
                t.tablica.sharedMaterial = mat; s.px = new Color32[TabW * TabH];
                for (int k = 0; k < s.historia.Length; k++) s.historia[k] = los.Next(37);
                s.ile = s.historia.Length; Tablica(s);
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
        int wyg = Zasady.Wygrywa(s.pole, s.wynik) ? s.zaklad * 2 : 0;
        if (wyg > 0) BraxChampionsKasyno.Wplac(wyg);
        BraxChampionsGry.Pokaz(Zasady.Opis(s.wynik) + (wyg > 0 ? " · WYGRANA " + wyg : " · " + Zasady.Pola[s.pole] + " PRZEGRYWA"), 3f);
        if (s.pole >= 0 && s.pole < t.zetony.Length && t.zetony[s.pole]) t.zetony[s.pole].gameObject.SetActive(false);
        s.zaklad = 0; s.pole = -1;
        Tablica(s);
    }

    // the history board: the last number big, the others under it, red / black / green as on the wheel
    static readonly Color32 Czarny = new Color32(4, 4, 6, 255), Czerwony = new Color32(255, 50, 40, 255), Bialy = new Color32(235, 238, 245, 255),
        Zielony = new Color32(40, 230, 90, 255), Zloty = new Color32(255, 200, 60, 255);
    void Tablica(Stan s) { if (!s.tex) return; PaintTablica(s.px, s.historia, s.ile); s.tex.SetPixels32(s.px); s.tex.Apply(false); }
    public static void PaintTablica(Color32[] px, int[] h, int ile)
    {
        BraxChampionsPiksele.Wypelnij(px, Czarny);
        BraxChampionsPiksele.Srodek(px, TabW, TabH, "BRAXHORN", TabW / 2, TabH - 10, 1, Zloty);
        Color32 K(int n) => n == 0 ? Zielony : Zasady.Czerwony(n) ? Czerwony : Bialy;
        if (ile > 0) BraxChampionsPiksele.Srodek(px, TabW, TabH, h[0].ToString(), TabW / 2, TabH - 36, 3, K(h[0]));
        for (int k = 1; k < Mathf.Min(ile, h.Length); k++)
        {
            int y = TabH - 44 - k * 8; bool lewo = h[k] == 0 ? false : Zasady.Czerwony(h[k]);
            // red to the left, black to the right, zero in the middle (as the real boards)
            int x = h[k] == 0 ? TabW / 2 : lewo ? 18 : TabW - 18;
            BraxChampionsPiksele.Srodek(px, TabW, TabH, h[k].ToString(), x, y, 1, K(h[k]));
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
