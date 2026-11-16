using System;
using UnityEngine;
using Z = BraxChampionsAutomatyZasady;

// MIASTO MISTRZÓW - the casinos' slot machines, working (owner 16.11 18:12-18:22: "porządne maszyny do grania ...
// mechaniczne i komputerowe ... w nich komputer i mogą normalnie działać", "te maszyny mają funkcjonować"). The same machines
// stand in the SKY KASYNO (T09) and in KASYNO WAWER (T11); one component per casino holds its machines:
//  - MECHANICAL (a three-reel stepper as the real ones): AKCJA pulls the lever (down and back), the three real drums behind
//    the reel glass spin up, stop one after another with the reel brake's small kick and pay on the centre line; the red LED
//    meter under the glass shows the credit and the win;
//  - SCREEN (an upright video slot): AKCJA presses SPIN, the game runs on the main screen - five reels of three symbols
//    scroll, stop one after another, five lines pay; the meter under the reels shows the credit and the win.
// The rules and the pay tables are BraxChampionsAutomatyZasady; the symbols are the owner's reel strips (an atlas a theme). The linked
// progressive jackpot of all the region's machines shows on every topper and on the boards over the banks (one picture
// each, painted when it changes). A machine gets its own meter picture when it is first played (idle ones share the
// casino's). The money is the casino credit (BraxChampionsKasyno) until the game has its wallet.
public sealed class BraxChampionsAutomaty : MonoBehaviour, IBraxInterakcja
{
    [Serializable] public sealed class Maszyna
    {
        public Transform korpus;                               // the machine: origin on the floor, +z towards the player
        public bool ekran;                                     // false = drums, true = screen
        public int motyw;                                      // its theme (cabinet colour, the owner's art per theme)
        public Transform[] bebny = new Transform[0];           // mechanical: drums turning about their local x
        public Transform dzwignia;                             // mechanical: the lever swinging about its local x
        public Transform przycisk;                             // the SPIN button (pressed in when played)
        public MeshFilter[] kolumny = new MeshFilter[0];       // screen: per reel 4 symbol cells (rebuilt while spinning)
        public float komorka = .09f;                           // screen: one cell (local metres, square)
        public Renderer licznik;                               // credit / win
    }
    public Maszyna[] maszyny = new Maszyna[0];
    public Renderer[] jackpoty = new Renderer[0];              // the toppers' jackpot screens (one picture)
    public Renderer[] tablice = new Renderer[0];               // the jackpot boards over the banks (one picture)
    public string nazwa = "KASYNO";
    public int stawka = 10;

    sealed class Stan
    {
        public bool kreci; public float start, dzwigniaOd = -9f, przyciskOd = -9f;
        public float[] poz, cel, startCel, stop; public bool[] staje, stoi;
        public int wygrana; public Vector3 przyciskZ;
        public Texture2D tex; public Color32[] px;
        public Mesh[] siatki; public Vector3[][] wierzcholki; public Vector2[][] uv;
    }
    Stan[] stan;
    System.Random los;
    Texture2D jTex, tTex; Color32[] jPx, tPx;
    float odswiez; long ostatniJackpot = -1;
    const float Zasieg = 40f;
    public const int LicznikW = 192, LicznikH = 24, JackW = 192, JackH = 64, TabW = 256, TabH = 64;

    void OnEnable() { BraxInterakcje.Dodaj(this); }
    void OnDisable() { BraxInterakcje.Usun(this); }

    void Start()
    {
        los = new System.Random(Environment.TickCount ^ GetInstanceID());
        stan = new Stan[maszyny.Length];
        for (int i = 0; i < maszyny.Length; i++)
        {
            var m = maszyny[i]; var s = new Stan(); stan[i] = s; if (m == null || !m.korpus) continue;
            int n = m.ekran ? 5 : 3;
            s.poz = new float[n]; s.cel = new float[n]; s.startCel = new float[n]; s.stop = new float[n]; s.staje = new bool[n]; s.stoi = new bool[n];
            for (int r = 0; r < n; r++) s.poz[r] = los.Next(Z.Stopy);
            if (m.przycisk) s.przyciskZ = m.przycisk.localPosition;
            if (m.ekran)
            {
                s.siatki = new Mesh[n]; s.wierzcholki = new Vector3[n][]; s.uv = new Vector2[n][];
                for (int r = 0; r < n && r < m.kolumny.Length; r++)
                {
                    if (!m.kolumny[r]) continue;
                    var mesh = new Mesh { name = "automat bęben " + r }; mesh.MarkDynamic();
                    s.wierzcholki[r] = new Vector3[16]; s.uv[r] = new Vector2[16];
                    Z.Kolumna(s.wierzcholki[r], s.uv[r], m.komorka, Z.Pasek(r, true), s.poz[r]);
                    mesh.vertices = s.wierzcholki[r]; mesh.uv = s.uv[r]; mesh.triangles = Z.KolumnaTrojkaty;
                    var nrm = new Vector3[16]; for (int k = 0; k < 16; k++) nrm[k] = Vector3.forward; mesh.normals = nrm;
                    mesh.RecalculateBounds();
                    m.kolumny[r].sharedMesh = mesh; s.siatki[r] = mesh;
                }
            }
            else for (int r = 0; r < n && r < m.bebny.Length; r++) Beben(m, s, r);
        }
        jTex = Wspolna(jackpoty, JackW, JackH, out jPx);
        tTex = Wspolna(tablice, TabW, TabH, out tPx);
        Jackpoty(true);
    }
    // one picture for a set of displays: a copy of the first one's material with its own texture, given to all of them
    static Texture2D Wspolna(Renderer[] r, int w, int h, out Color32[] px)
    {
        px = null; Renderer first = null; foreach (var x in r) if (x) { first = x; break; }
        if (!first) return null;
        var t = Tekstura(w, h); var mat = new Material(first.sharedMaterial) { name = "automaty wspólny ekran" };
        mat.SetTexture("_MainTex", t); if (mat.HasProperty("_EmissionMap")) mat.SetTexture("_EmissionMap", t);
        foreach (var x in r) if (x) x.sharedMaterial = mat;
        px = new Color32[w * h]; return t;
    }
    static Texture2D Tekstura(int w, int h) =>
        new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "automat ekran" };

    // ---------------- interakcja ----------------
    int Najblizsza(Vector3 p)
    {
        int best = -1; float bd = float.MaxValue;
        for (int i = 0; i < maszyny.Length; i++)
        {
            var m = maszyny[i]; if (m == null || !m.korpus) continue;
            float s = m.korpus.lossyScale.x;
            var d = p - m.korpus.TransformPoint(new Vector3(0f, 0f, 1.0f));      // the player stands behind the stool
            if (Mathf.Abs(d.y) > 1.2f * s) continue; d.y = 0f;
            float dd = d.magnitude; if (dd < .70f * s && dd < bd) { bd = dd; best = i; }
        }
        return best;
    }
    public string Kontekst(Vector3 gracz)
    {
        int i = Najblizsza(gracz); if (i < 0 || stan == null) return "";
        if (stan[i].kreci) return "AUTOMAT · KRĘCI";
        return BraxChampionsKasyno.Kredyt >= stawka ? (maszyny[i].ekran ? "GRAJ · " : "POCIĄGNIJ DŹWIGNIĘ · ") + stawka : "BRAK KREDYTU";
    }
    public bool Wykonaj(Vector3 gracz)
    {
        int i = Najblizsza(gracz); if (i < 0 || stan == null) return false;
        var s = stan[i]; var m = maszyny[i];
        if (s.kreci) return true;
        if (!BraxChampionsKasyno.Wydaj(stawka)) { BraxChampionsGry.Pokaz("BRAK KREDYTU", 1.6f); return true; }
        BraxChampionsKasyno.Dorzuc(stawka);
        int n = m.ekran ? 5 : 3; float now = Time.time;
        s.kreci = true; s.wygrana = 0; s.start = now; s.przyciskOd = now; s.dzwigniaOd = m.dzwignia ? now : -9f;
        float poczatek = m.ekran ? .05f : .35f;                   // the drums start when the lever is down
        for (int r = 0; r < n; r++)
        {
            s.staje[r] = false; s.stoi[r] = false;
            s.stop[r] = now + poczatek + (m.ekran ? .9f + .28f * r : 1.0f + .45f * r);
            s.cel[r] = los.Next(Z.Stopy);
        }
        Licznik(m, s);
        return true;
    }

    // ---------------- per frame ----------------
    void Update()
    {
        if (stan == null) return;
        var g = BraxChampionsGry.Gracz;
        Vector3 gp = g ? g.transform.position : Vector3.one * 1e6f;
        for (int i = 0; i < maszyny.Length; i++)
        {
            var s = stan[i]; var m = maszyny[i]; if (m == null || !m.korpus) continue;
            if (s.przyciskOd > 0f) Przycisk(m, s);
            if (s.dzwigniaOd > 0f) Dzwignia(m, s);
            if (s.kreci) Krec(m, s);
        }
        int bliska = g ? Najblizsza(gp) : -1;
        if (bliska >= 0)
        {
            var s = stan[bliska];
            BraxChampionsGry.Ustaw(nazwa + " · KREDYT " + BraxChampionsKasyno.Kredyt + " · STAWKA " + stawka + (s.wygrana > 0 ? " · WYGRANA " + s.wygrana : ""));
        }
        if (Time.time >= odswiez && g && Blisko(gp)) { odswiez = Time.time + .5f; Jackpoty(false); }
    }
    bool Blisko(Vector3 p) { foreach (var m in maszyny) if (m != null && m.korpus && (m.korpus.position - p).sqrMagnitude < Zasieg * Zasieg) return true; return false; }

    void Dzwignia(Maszyna m, Stan s)
    {
        float t = Time.time - s.dzwigniaOd, k;
        if (t < .30f) k = Mathf.SmoothStep(0f, 1f, t / .30f);
        else if (t < .85f) k = 1f - Mathf.SmoothStep(0f, 1f, (t - .30f) / .55f);
        else { k = 0f; s.dzwigniaOd = -9f; }
        if (m.dzwignia) m.dzwignia.localRotation = Quaternion.AngleAxis(k * 62f, Vector3.right);
    }
    void Przycisk(Maszyna m, Stan s)
    {
        float t = Time.time - s.przyciskOd; bool wcisniety = t < .16f;
        if (!wcisniety) s.przyciskOd = -9f;
        if (m.przycisk) m.przycisk.localPosition = s.przyciskZ + (wcisniety ? m.przycisk.localRotation * new Vector3(0f, -.006f, 0f) : Vector3.zero);
    }

    void Krec(Maszyna m, Stan s)
    {
        int n = s.poz.Length; float now = Time.time; bool wszystkie = true;
        float predkosc = m.ekran ? 16f : 20f;                     // stops a second: 16 on the screen, 20 on the drums (360 deg/s)
        float rusza = m.ekran ? .05f : .35f;
        for (int r = 0; r < n; r++)
        {
            if (s.stoi[r]) continue;
            wszystkie = false;
            if (now < s.start + rusza) continue;
            if (!s.staje[r])
            {
                s.poz[r] += predkosc * Time.deltaTime;
                if (now >= s.stop[r])
                {
                    // the target: the chosen stop, at least two stops ahead (a reel never runs backwards)
                    float baza = Mathf.Floor(s.poz[r] / Z.Stopy) * Z.Stopy + s.cel[r];
                    while (baza < s.poz[r] + 2f) baza += Z.Stopy;
                    s.staje[r] = true; s.startCel[r] = s.poz[r]; s.cel[r] = baza; s.stop[r] = now;
                }
            }
            else
            {
                // slow down into the stop, a small kick past it and back (the reel's brake)
                float t = now - s.stop[r]; const float D = .38f, B = .14f, kick = .18f;
                if (t < D) { float e = 1f - Mathf.Pow(1f - t / D, 3f); s.poz[r] = Mathf.Lerp(s.startCel[r], s.cel[r] + kick, e); }
                else if (t < D + B) s.poz[r] = Mathf.Lerp(s.cel[r] + kick, s.cel[r], Mathf.SmoothStep(0f, 1f, (t - D) / B));
                else { s.poz[r] = s.cel[r]; s.stoi[r] = true; }
            }
            if (m.ekran) Kolumna(m, s, r);
            else Beben(m, s, r);
        }
        if (wszystkie) Koniec(m, s);
    }
    // a drum's angle from its position within one turn (the position itself is brought back into 0..20 after every spin, so
    // the angle never loses precision however long the machine is played)
    static void Beben(Maszyna m, Stan s, int r)
    {
        if (r < m.bebny.Length && m.bebny[r]) m.bebny[r].localRotation = Quaternion.AngleAxis(s.poz[r] % Z.Stopy * 18f, Vector3.right);
    }
    void Kolumna(Maszyna m, Stan s, int r)
    {
        if (s.siatki == null || r >= s.siatki.Length || !s.siatki[r]) return;
        Z.Kolumna(s.wierzcholki[r], s.uv[r], m.komorka, Z.Pasek(r, true), s.poz[r]);
        s.siatki[r].vertices = s.wierzcholki[r]; s.siatki[r].uv = s.uv[r]; s.siatki[r].RecalculateBounds();
    }

    void Koniec(Maszyna m, Stan s)
    {
        s.kreci = false;
        for (int r = 0; r < s.poz.Length; r++) s.poz[r] = ((Mathf.RoundToInt(s.poz[r]) % Z.Stopy) + Z.Stopy) % Z.Stopy;
        int wygrana; bool jackpot;
        if (m.ekran)
        {
            var st = new int[5]; for (int r = 0; r < 5; r++) st[r] = Mathf.RoundToInt(s.poz[r]);
            int mnoznik = Z.Ekran(Z.Okno(st), out _, out jackpot);
            wygrana = mnoznik * stawka / 5;
        }
        else
        {
            int a = Z.Symbol(Z.Pasek(0, false), Mathf.RoundToInt(s.poz[0])), b = Z.Symbol(Z.Pasek(1, false), Mathf.RoundToInt(s.poz[1])),
                c = Z.Symbol(Z.Pasek(2, false), Mathf.RoundToInt(s.poz[2]));
            int mn = Z.Mechaniczny(a, b, c); jackpot = mn < 0; wygrana = mn > 0 ? mn * stawka : 0;
        }
        if (jackpot) wygrana = BraxChampionsKasyno.WezJackpot();
        s.wygrana = wygrana;
        if (wygrana > 0)
        {
            BraxChampionsKasyno.Wplac(wygrana);
            BraxChampionsGry.Pokaz(jackpot ? "JACKPOT! " + wygrana : "WYGRANA " + wygrana + "!", jackpot ? 4f : 2.2f);
        }
        Licznik(m, s); Jackpoty(true);
    }

    // ---- the displays (the region's LED font)
    static readonly Color32 Czerwony = new Color32(255, 40, 30, 255), Zloty = new Color32(255, 200, 40, 255), Czarny = new Color32(4, 4, 6, 255),
        Bialy = new Color32(240, 244, 255, 255), Zielony = new Color32(60, 255, 110, 255);
    void Licznik(Maszyna m, Stan s)
    {
        if (!m.licznik) return;
        if (!s.tex)
        {
            // the machine's own meter from now on (idle machines share the casino's picture)
            s.tex = Tekstura(LicznikW, LicznikH); s.px = new Color32[LicznikW * LicznikH];
            var mat = new Material(m.licznik.sharedMaterial) { name = "automat licznik" };
            mat.SetTexture("_MainTex", s.tex); if (mat.HasProperty("_EmissionMap")) mat.SetTexture("_EmissionMap", s.tex);
            m.licznik.sharedMaterial = mat;
        }
        PaintLicznik(s.px, m.ekran, BraxChampionsKasyno.Kredyt, s.wygrana, -1);
        s.tex.SetPixels32(s.px); s.tex.Apply(false);
    }
    // the meter: KREDYT and WYGRANA (or, idle, the stake); shared with the generator, which paints the idle picture
    public static void PaintLicznik(Color32[] px, bool ekran, int kredyt, int wygrana, int stawkaBezczynna)
    {
        BraxChampionsPiksele.Wypelnij(px, Czarny);
        var kol = ekran ? Bialy : Czerwony;
        if (stawkaBezczynna > 0)
        {
            BraxChampionsPiksele.Srodek(px, LicznikW, LicznikH, "BRAXHORN · STAWKA " + stawkaBezczynna, LicznikW / 2, 8, 1, kol);
            return;
        }
        BraxChampionsPiksele.Tekst(px, LicznikW, LicznikH, "KREDYT " + kredyt, 4, 8, 1, kol);
        BraxChampionsPiksele.Tekst(px, LicznikW, LicznikH, "WYGRANA " + wygrana, 104, 8, 1, wygrana > 0 ? Zielony : kol);
    }
    void Jackpoty(bool zawsze)
    {
        long j = BraxChampionsKasyno.Jackpot; if (!zawsze && j == ostatniJackpot) return; ostatniJackpot = j;
        if (jTex) { PaintJackpot(jPx, j); jTex.SetPixels32(jPx); jTex.Apply(false); }
        if (tTex) { PaintTablica(tPx, j); tTex.SetPixels32(tPx); tTex.Apply(false); }
    }
    public static void PaintJackpot(Color32[] px, long j)
    {
        BraxChampionsPiksele.Wypelnij(px, Czarny);
        BraxChampionsPiksele.Srodek(px, JackW, JackH, "JACKPOT", JackW / 2, 40, 2, Zloty);
        BraxChampionsPiksele.Srodek(px, JackW, JackH, Kwota(j), JackW / 2, 10, 2, Bialy);
    }
    public static void PaintTablica(Color32[] px, long j)
    {
        BraxChampionsPiksele.Wypelnij(px, Czarny);
        BraxChampionsPiksele.Srodek(px, TabW, TabH, "BRAXHORN JACKPOT", TabW / 2, 44, 2, Zloty);
        BraxChampionsPiksele.Srodek(px, TabW, TabH, Kwota(j), TabW / 2, 8, 3, Czerwony);
    }
    public static string Kwota(long v)
    {
        var s = v.ToString(); var o = new System.Text.StringBuilder();
        for (int i = 0; i < s.Length; i++) { if (i > 0 && (s.Length - i) % 3 == 0) o.Append(' '); o.Append(s[i]); }
        return o.ToString();
    }
}

// The casino credit (until the game has its wallet): kept on the phone, never below zero; the linked progressive jackpot
// grows by 2 % of every stake in every casino of the region and starts again from 25 000 after a win.
public static class BraxChampionsKasyno
{
    // the jackpot is kept in hundredths, so 2 % of a stake of 10 (0.20) is not lost
    const string KluczKredyt = "braxhorn_kasyno_kredyt", KluczJackpot = "braxhorn_kasyno_jackpot_setne";
    public const int Start = 1000; public const long JackpotStart = 25000;
    public static int Kredyt
    {
        get => PlayerPrefs.GetInt(KluczKredyt, Start);
        private set { PlayerPrefs.SetInt(KluczKredyt, Mathf.Max(0, value)); PlayerPrefs.Save(); }
    }
    public static bool Wydaj(int ile) { if (ile <= 0 || Kredyt < ile) return false; Kredyt -= ile; return true; }
    public static void Wplac(int ile) { if (ile > 0) Kredyt = (int)Math.Min(int.MaxValue, (long)Kredyt + ile); }
    static long Setne => long.TryParse(PlayerPrefs.GetString(KluczJackpot, ""), out var j) && j >= JackpotStart * 100 ? j : JackpotStart * 100;
    public static long Jackpot => Setne / 100;
    public static void Dorzuc(int stawka) { PlayerPrefs.SetString(KluczJackpot, (Setne + 2L * Math.Max(0, stawka)).ToString()); }
    public static int WezJackpot()
    {
        long j = Jackpot; PlayerPrefs.SetString(KluczJackpot, (JackpotStart * 100).ToString()); PlayerPrefs.Save();
        return (int)Math.Min(int.MaxValue, j);
    }
}
