using System;
using System.Collections.Generic;
using UnityEngine;
using Z = BraxChampionsAutomatyZasady;
using G = BraxChampionsAutomatyGry;
using P = BraxChampionsAutomatyGry.PokerGra;
using K = BraxChampionsAutomatyGry.KenoGra;
using KG = BraxChampionsAutomatyGry.KoloGra;
using U = BraxChampionsStanowisko.Ui;

// MIASTO MISTRZÓW - the casinos' slot machines, working (owner 16.11 18:12-18:22: "porządne maszyny do grania ...
// mechaniczne i komputerowe ... w nich komputer i mogą normalnie działać", "te maszyny mają funkcjonować"). The same machines
// stand in the SKY KASYNO (T09) and in KASYNO WAWER (T11); one component per casino holds its machines:
//  - MECHANICAL (a three-reel stepper as the real ones): AKCJA pulls the lever (down and back), the three real drums behind
//    the reel glass spin up, stop one after another with the reel brake's small kick and pay on the centre line; the red LED
//    meter under the glass shows the credit and the win;
//  - SCREEN (an upright video slot): AKCJA presses SPIN, the game runs on the main screen - five reels of three symbols
//    scroll, stop one after another, five lines pay; the meter under the reels shows the credit and the win.
//  - VIDEO POKER (the screen upright): AKCJA deals five cards, the AUTO HOLD marks TRZYMAJ, AKCJA again draws, the hand pays;
//  - KENO (the screen upright): AKCJA quick-picks six numbers of 80 and draws twenty balls one by one, the catches pay;
//  - KOŁO MISTRZÓW (the stepper with the wheel on top): as the mechanical one; KOŁO on the third reel's line spins the wheel.
// The rules and the pay tables are BraxChampionsAutomatyZasady and BraxChampionsAutomatyGry; the symbols are the owner's reel
// strips (an atlas a theme). The linked
// progressive jackpot of all the region's machines shows on every topper and on the boards over the banks (one picture
// each, painted when it changes). A machine gets its own meter picture when it is first played (idle ones share the
// casino's). The money is the casino credit (BraxChampionsKasyno) until the game has its wallet.
public sealed class BraxChampionsAutomaty : MonoBehaviour, IBraxInterakcja, IBraxStanowisko
{
    [Serializable] public sealed class Maszyna
    {
        public Transform korpus;                               // the machine: origin on the floor, +z towards the player
        public bool ekran;                                     // false = drums, true = screen
        public int gra;                                        // BraxChampionsAutomatyGry: 0 classic, 1 video, 2 poker, 3 keno, 4 koło
        public int motyw;                                      // its theme (cabinet colour, the owner's art per theme)
        public Transform[] bebny = new Transform[0];           // mechanical: drums turning about their local x
        public Transform dzwignia;                             // mechanical: the lever swinging about its local x
        public Transform przycisk;                             // the SPIN button (pressed in when played)
        public MeshFilter[] kolumny = new MeshFilter[0];       // screen: per reel 4 symbol cells (rebuilt while spinning)
        public float komorka = .09f;                           // screen: one cell (local metres, square)
        public MeshFilter warstwa;                             // poker / keno: the main screen's live layer
        public Transform kolo, wskaznik;                       // koło: the wheel (turning about its local z) and its pointer
        public Renderer licznik;                               // credit / win
        public BraxSeat siedzisko;                             // its stool: sitting down opens the game (BraxChampionsStanowisko)
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
        // poker: 0 waiting for the deal, 1 dealt (waiting for the draw), 2 drawn; the cards turn over one by one
        public int faza, trzymaj, uklad, wTalii;
        public readonly int[] karty = new int[5], talia = new int[52]; public readonly bool[] odkryte = new bool[5]; public readonly float[] odkryj = new float[5];
        // keno: the six numbers, the twenty balls, how many are shown
        public readonly int[] typy = new int[K.Typy], kule = new int[K.Kule]; public int pokazane; public float nastepna;
        public Mesh warstwa;
        // koło: the wheel's angle (clockwise seen from the front), its spin from start to target, the segment drawn
        public bool koloKreci; public float koloOd, koloStart, koloCel, koloKat, koloKoniec = -9f; public int koloSeg;
        // the stake of the game in play (chosen on the stool, the machine's own when played standing)
        public int st;
        // keno from the stool: the player's six numbers (kept for the next draws), true while he is changing them
        public readonly List<int> wybor = new List<int>(); public bool wybieram;
    }
    Stan[] stan;
    System.Random los;
    Texture2D jTex, tTex; Color32[] jPx, tPx;
    static G.Warstwa wPoker, wKeno;
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
            if (m.ekran && m.gra == G.Mech) m.gra = G.Video;
            if (m.gra == G.Poker || m.gra == G.Keno) { StartEkranu(m, s); continue; }
            int n = m.gra == G.Video ? 5 : 3;
            s.poz = new float[n]; s.cel = new float[n]; s.startCel = new float[n]; s.stop = new float[n]; s.staje = new bool[n]; s.stoi = new bool[n];
            for (int r = 0; r < n; r++) s.poz[r] = los.Next(Z.Stopy);
            if (m.przycisk) s.przyciskZ = m.przycisk.localPosition;
            if (m.kolo) { s.koloKat = los.Next(KG.Segmenty.Length) * KG.Kat; m.kolo.localRotation = Quaternion.AngleAxis(s.koloKat, Vector3.forward); }
            if (m.gra == G.Video)
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
        // the stools: sitting down opens the machine's game; the casino's other seats (the bar, the salon) stand only on their floor
        BraxChampionsStanowisko.Siedziska(transform);
        for (int i = 0; i < maszyny.Length; i++) if (maszyny[i] != null && maszyny[i].korpus) BraxChampionsStanowisko.Rejestruj(this, maszyny[i].siedzisko, i);
        jTex = Wspolna(jackpoty, JackW, JackH, out jPx);
        tTex = Wspolna(tablice, TabW, TabH, out tPx);
        Jackpoty(true);
    }
    // poker and keno: the machine's own layer mesh; the idle screen shows a hand / six numbers (as a machine left by a player)
    void StartEkranu(Maszyna m, Stan s)
    {
        if (m.przycisk) s.przyciskZ = m.przycisk.localPosition;
        if (!m.warstwa) return;
        s.warstwa = new Mesh { name = m.gra == G.Poker ? "video poker ekran" : "keno ekran" }; s.warstwa.MarkDynamic();
        m.warstwa.sharedMesh = s.warstwa;
        if (m.gra == G.Poker) { P.Tasuj(los, s.talia); for (int k = 0; k < 5; k++) { s.karty[k] = s.talia[k]; s.odkryte[k] = true; } }
        else K.Losuj(los, s.typy);
        Ekran(m, s);
    }
    void Ekran(Maszyna m, Stan s)
    {
        if (!s.warstwa) return;
        if (m.gra == G.Poker) P.Zbuduj(wPoker ??= new G.Warstwa(P.AtlasW, P.AtlasH), s.warstwa, s.karty, s.odkryte, s.trzymaj, s.uklad);
        else if (s.wybieram) K.Zbuduj(wKeno ??= new G.Warstwa(K.AtlasW, K.AtlasH), s.warstwa, s.wybor.ToArray(), s.kule, 0);
        else K.Zbuduj(wKeno ??= new G.Warstwa(K.AtlasW, K.AtlasH), s.warstwa, s.typy, s.kule, s.pokazane);
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
        var s = stan[i]; var m = maszyny[i];
        if (m.siedzisko) return "";                                // played from its stool (USIĄDŹ, BraxChampionsStanowisko)
        if (s.koloKreci) return "KOŁO MISTRZÓW · KRĘCI SIĘ";
        if (s.kreci) return m.gra == G.Keno ? "KENO · LOSOWANIE" : m.gra == G.Poker ? "VIDEO POKER · ROZDAJE" : "AUTOMAT · KRĘCI";
        if (m.gra == G.Poker && s.faza == 1) return "WYMIEŃ KARTY · TRZYMA " + Bity(s.trzymaj);
        if (BraxChampionsKasyno.Kredyt < stawka) return "BRAK KREDYTU";
        switch (m.gra)
        {
            case G.Poker: return "VIDEO POKER · ROZDAJ · " + stawka;
            case G.Keno: return "KENO 6 Z 80 · GRAJ · " + stawka;
            case G.Video: return "GRAJ · " + stawka;
            default: return "POCIĄGNIJ DŹWIGNIĘ · " + stawka;
        }
    }
    static int Bity(int v) { int c = 0; while (v != 0) { c += v & 1; v >>= 1; } return c; }
    public bool Wykonaj(Vector3 gracz)
    {
        int i = Najblizsza(gracz); if (i < 0 || stan == null || maszyny[i].siedzisko) return false;
        return Graj(i, stawka);
    }
    // one game of machine i for the stake st: the spin, the deal / the draw of video poker, the keno draw
    bool Graj(int i, int st)
    {
        var s = stan[i]; var m = maszyny[i];
        if (s.kreci || s.koloKreci) return true;
        if (m.gra == G.Poker) return PokerAkcja(i, m, s, st);
        if (m.gra == G.Keno && s.wybor.Count > 0 && s.wybor.Count < K.Typy) { BraxChampionsGry.Pokaz("SKREŚL JESZCZE " + (K.Typy - s.wybor.Count) + " Z " + K.Typy, 1.6f); return true; }
        if (!BraxChampionsKasyno.Wydaj(st)) { BraxChampionsGry.Pokaz("BRAK KREDYTU", 1.6f); return true; }
        s.st = st;
        if (m.gra == G.Keno) return KenoAkcja(m, s);
        if (m.gra != G.Kolo) BraxChampionsKasyno.Dorzuc(st);           // the linked jackpot: the classic and the video machines
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

    // ---- VIDEO POKER: the deal (the stake), then the draw of the cards not held
    bool PokerAkcja(int i, Maszyna m, Stan s, int st)
    {
        float now = Time.time;
        if (s.faza == 1)
        {
            s.przyciskOd = now; int j = 0;
            for (int k = 0; k < 5; k++) if ((s.trzymaj >> k & 1) == 0) { s.karty[k] = s.talia[s.wTalii++]; s.odkryte[k] = false; s.odkryj[k] = now + .12f + .11f * j++; }
            s.faza = 2; s.kreci = true; Ekran(m, s); return true;
        }
        if (!BraxChampionsKasyno.Wydaj(st)) { BraxChampionsGry.Pokaz("BRAK KREDYTU", 1.6f); return true; }
        s.st = st; s.przyciskOd = now; P.Tasuj(los, s.talia); s.wTalii = 0;
        for (int k = 0; k < 5; k++) { s.karty[k] = s.talia[s.wTalii++]; s.odkryte[k] = false; s.odkryj[k] = now + .12f + .11f * k; }
        s.trzymaj = 0; s.uklad = 0; s.wygrana = 0; s.faza = 0; s.kreci = true;
        Ekran(m, s); Licznik(m, s); return true;
    }
    void PokerKrok(int i, Maszyna m, Stan s)
    {
        float now = Time.time; bool zmiana = false, wszystkie = true;
        for (int k = 0; k < 5; k++)
            if (!s.odkryte[k]) { if (now >= s.odkryj[k]) { s.odkryte[k] = true; zmiana = true; } else wszystkie = false; }
        if (wszystkie)
        {
            s.kreci = false; zmiana = true; s.uklad = P.Ocen(s.karty);
            // a dealt hand shows its name; standing: AUTO HOLD; on the stool the player holds the cards himself (or AUTO)
            if (s.faza == 0) { s.trzymaj = BraxChampionsStanowisko.Gra(this, i) ? 0 : P.Trzymaj(s.karty); s.faza = 1; }
            else
            {
                s.trzymaj = 0; s.faza = 0; s.wygrana = P.Wyplata[s.uklad] * s.st;
                if (s.wygrana > 0) { BraxChampionsKasyno.Wplac(s.wygrana); BraxChampionsGry.Pokaz(P.Uklady[s.uklad] + " · WYGRANA " + s.wygrana + "!", s.uklad >= P.Kareta ? 4f : 2.2f); }
                Licznik(m, s);
            }
        }
        if (zmiana) Ekran(m, s);
    }
    // ---- KENO: QUICK PICK six, twenty balls one by one
    bool KenoAkcja(Maszyna m, Stan s)
    {
        float now = Time.time; s.przyciskOd = now;
        // the player's six numbers, or QUICK PICK (they become his for the next draws)
        if (s.wybor.Count == K.Typy) s.wybor.CopyTo(s.typy);
        else { K.Losuj(los, s.typy); s.wybor.Clear(); s.wybor.AddRange(s.typy); }
        s.wybieram = false; K.Losuj(los, s.kule);
        s.pokazane = 0; s.nastepna = now + .45f; s.kreci = true; s.wygrana = 0;
        Ekran(m, s); Licznik(m, s); return true;
    }
    void KenoKrok(Maszyna m, Stan s)
    {
        if (Time.time < s.nastepna) return;
        s.pokazane++; s.nastepna = Time.time + .16f;
        if (s.pokazane >= K.Kule)
        {
            s.kreci = false; int t = K.Trafione(s.typy, s.kule, K.Kule); s.wygrana = K.Wyplata[t] * s.st;
            if (s.wygrana > 0) { BraxChampionsKasyno.Wplac(s.wygrana); BraxChampionsGry.Pokaz("KENO · TRAFIONE " + t + " Z 6 · WYGRANA " + s.wygrana + "!", t >= 5 ? 4f : 2.2f); }
            Licznik(m, s);
        }
        Ekran(m, s);
    }
    // ---- KOŁO MISTRZÓW: the wheel runs 6.5 s down to the segment drawn; the pointer flaps on the pegs
    const float KoloCzas = 6.5f;
    void KoloStart(Stan s)
    {
        s.koloKreci = true; s.koloOd = Time.time + .5f; s.koloSeg = los.Next(KG.Segmenty.Length); s.koloStart = s.koloKat;
        s.koloCel = s.koloKat + 3f * 360f + Mathf.Repeat(KG.KatSegmentu(s.koloSeg) - s.koloKat, 360f);
    }
    void KoloKrok(Maszyna m, Stan s)
    {
        float t = Time.time - s.koloOd; if (t < 0f) return;
        float u = Mathf.Clamp01(t / KoloCzas), e = 1f - Mathf.Pow(1f - u, 3f);
        s.koloKat = Mathf.Lerp(s.koloStart, s.koloCel, e);
        if (m.kolo) m.kolo.localRotation = Quaternion.AngleAxis(s.koloKat, Vector3.forward);
        if (m.wskaznik)
        {
            // a peg reaches the pointer when the angle passes a segment border: pushed to the right, it snaps back
            float f = Mathf.Repeat(s.koloKat + .5f * KG.Kat, KG.Kat) / KG.Kat;
            float d = f > .7f ? (f - .7f) / .3f : f < .12f ? 1f - f / .12f : 0f;
            m.wskaznik.localRotation = Quaternion.AngleAxis(-22f * d, Vector3.forward);
        }
        if (u >= 1f)
        {
            s.koloKreci = false; s.koloKat = Mathf.Repeat(s.koloCel, 360f);
            if (m.kolo) m.kolo.localRotation = Quaternion.AngleAxis(s.koloKat, Vector3.forward);
            if (m.wskaznik) m.wskaznik.localRotation = Quaternion.identity;
            int w = KG.Wygrana(s.koloSeg, s.st); s.wygrana += w; BraxChampionsKasyno.Wplac(w); s.koloKoniec = Time.time;
            BraxChampionsGry.Pokaz("KOŁO MISTRZÓW · " + w + "!", 3.2f); Licznik(m, s);
        }
    }

    // ================================================================ from the stool (owner 23.11 17:35, BraxChampionsStanowisko)
    // The player sits down and sees the machine: the stake (5 .. 100) with − / +, ZAKRĘĆ (the lever / SPIN), the win and the
    // jackpot; VIDEO POKER: the cards held by touching them on the machine's screen (or AUTO), WYMIEŃ draws; KENO: the six
    // numbers crossed on the board itself (or LOSUJ), kept for the next draws; KOŁO MISTRZÓW: the camera rises to the wheel
    // while it runs. Everything else is the machine's own game above (the same rules, pay tables and jackpot).
    public static readonly int[] Kroki = { 5, 10, 20, 50, 100 };
    int krok = -1;
    int Krok { get { if (krok < 0) { krok = Array.IndexOf(Kroki, stawka); if (krok < 0) krok = 1; } return krok; } }
    public string Tytul(int i) => nazwa + " · " + G.Nazwy[maszyny[i].gra];

    // what the seated player sees (the cabinet's metres, as BraxChampionsKasynoSprzet builds it): the reel glass with the meter
    // and the lever; the main screen with its meter (tilted 15 degrees to him); the wheel on the top while it runs
    public static readonly Vector3 KadrMech = new Vector3(-.03f, 1.19f, .22f), KadrKolo = new Vector3(0f, 2.03f, .13f), EkranSrodek = new Vector3(0f, 1.18f, .25f);
    public const float KadrMechW = .86f, KadrMechH = .54f, KadrEkranW = .72f, KadrEkranH = .47f, KadrKoloW = 1.02f, KadrKoloH = 1.02f, EkranPochylenie = -15f;
    public void Kadr(int i, Camera cam, out Vector3 oko, out Quaternion obrot)
    {
        var m = maszyny[i]; var s = stan[i]; var t = m.korpus; float k = Mathf.Abs(t.lossyScale.x);
        if (m.kolo && (s.koloKreci || Time.time - s.koloKoniec < 2.4f))
        {
            // set lower than the band's middle: its pointer at the top stays clear of the flash under the top bar
            BraxChampionsStanowisko.Kadruj(cam, t.TransformPoint(KadrKolo) + t.up * (.08f * k), t.TransformDirection(new Vector3(0f, -.20f, .98f)), t.up, KadrKoloW * k, KadrKoloH * k, out oko, out obrot);
            return;
        }
        if (m.ekran)
        {
            var e = EkranGry(m); var q = e ? e.rotation : t.rotation * Quaternion.Euler(EkranPochylenie, 0f, 0f);
            var c = e ? e.position : t.TransformPoint(EkranSrodek);
            BraxChampionsStanowisko.Kadruj(cam, c + q * new Vector3(0f, -.03f * k, 0f), q * Vector3.forward, q * Vector3.up, KadrEkranW * k, KadrEkranH * k, out oko, out obrot);
            return;
        }
        BraxChampionsStanowisko.Kadruj(cam, t.TransformPoint(KadrMech), t.TransformDirection(new Vector3(0f, .12f, .99f)), t.up, KadrMechW * k, KadrMechH * k, out oko, out obrot);
    }
    // the main screen's group ("ekran gry", its +z the screen's face)
    static Transform EkranGry(Maszyna m) => m.warstwa ? m.warstwa.transform.parent
        : m.kolumny != null && m.kolumny.Length > 0 && m.kolumny[0] ? m.kolumny[0].transform.parent : null;
    // a rectangle of the screen's layer (the viewer's frame: x to his right, y up) in the world
    static void Prostokat(Maszyna m, float cx, float cy, float w, float h, out Vector3 a, out Vector3 b, out Vector3 c, out Vector3 d)
    {
        var t = m.warstwa.transform;
        a = t.TransformPoint(new Vector3(-(cx - w * .5f), cy - h * .5f, 0f)); b = t.TransformPoint(new Vector3(-(cx + w * .5f), cy - h * .5f, 0f));
        c = t.TransformPoint(new Vector3(-(cx + w * .5f), cy + h * .5f, 0f)); d = t.TransformPoint(new Vector3(-(cx - w * .5f), cy + h * .5f, 0f));
    }

    public void Panel(int i, U ui)
    {
        var m = maszyny[i]; var s = stan[i]; var r = ui.dol; float p = ui.m;
        bool gra = s.kreci || s.koloKreci, wymiana = m.gra == G.Poker && s.faza == 1;
        float h = r.height - 2f * p, y = r.y + p;
        var stR = new Rect(r.x + p, y, r.width * .29f, h);
        var glR = new Rect(r.xMax - p - r.width * .25f, y, r.width * .25f, h);
        var srR = new Rect(stR.xMax + p, y, glR.x - stR.xMax - 2f * p, h);
        krok = ui.Stawka(stR, Kroki, Krok, !gra && !wymiana);
        int st = Kroki[krok]; bool stac = BraxChampionsKasyno.Kredyt >= st;
        float pol = (srR.width - p) * .5f;
        var a = new Rect(srR.x, y, pol, h); var b = new Rect(a.xMax + p, y, pol, h);
        string glowny = stac ? "ZAKRĘĆ" : "BRAK KREDYTU"; bool mozna = !gra && stac;
        switch (m.gra)
        {
            case G.Poker:
            {
                string uk = s.uklad > 0 && s.uklad < P.Uklady.Length ? P.Uklady[s.uklad] : "";
                if (wymiana)
                {
                    ui.Pole(a, "DOTKNIJ KART · TRZYMAJ", uk.Length > 0 ? uk : "BEZ UKŁADU", uk.Length > 0 ? U.Zloto : U.Krem);
                    if (ui.Przycisk(b, "AUTO TRZYMAJ", U.Granat, U.Krem, !s.kreci, false, U.Zloto)) { s.trzymaj = P.Trzymaj(s.karty); Ekran(m, s); }
                    glowny = "WYMIEŃ"; mozna = !s.kreci;
                }
                else
                {
                    ui.Pole(a, "WYGRANA", Kwota(s.wygrana), s.wygrana > 0 ? U.Zloto : U.Krem);
                    ui.Pole(b, "UKŁAD", s.faza == 0 && !s.kreci && uk.Length > 0 ? uk : "JACKS OR BETTER", U.Krem);
                    glowny = stac ? "ROZDAJ" : "BRAK KREDYTU";
                }
                // the cards on the machine's own screen: a touch holds or frees one; the held ones carry a gold frame too
                if (m.warstwa)
                    for (int k = 0; k < 5; k++)
                    {
                        Prostokat(m, (k - 2) * P.Rozstaw, P.KartyY, P.KartaSz, P.KartaWys, out var c0, out var c1, out var c2, out var c3);
                        bool trz = (s.trzymaj >> k & 1) != 0;
                        if (wymiana && trz) ui.Obrys(c0, c1, c2, c3, U.Zloto, ui.s * .008f);
                        if (wymiana && !s.kreci && ui.Dotyk(c0, c1, c2, c3)) { s.trzymaj ^= 1 << k; Ekran(m, s); }
                    }
                break;
            }
            case G.Keno:
            {
                int t = !s.wybieram && s.pokazane > 0 ? K.Trafione(s.typy, s.kule, s.pokazane) : -1;
                if (t >= 0) ui.Pole(a, s.kreci ? "LOSOWANIE · KULA " + s.pokazane : "TRAFIONE · WYGRANA " + Kwota(s.wygrana), t + " Z " + K.Typy, t >= 3 ? U.Zloto : U.Krem);
                else ui.Pole(a, s.wybor.Count == 0 ? "SKREŚL NA EKRANIE" : "TWOJE LICZBY", s.wybor.Count + " / " + K.Typy, s.wybor.Count == K.Typy ? U.Zloto : U.Krem);
                var b1 = new Rect(b.x, b.y, b.width, (b.height - p * .5f) * .5f); var b2 = new Rect(b.x, b1.yMax + p * .5f, b.width, b1.height);
                if (ui.Przycisk(b1, "LOSUJ 6", U.Granat, U.Krem, !gra, false, U.Zloto))
                {
                    var nowe = new int[K.Typy]; K.Losuj(los, nowe); s.wybor.Clear(); s.wybor.AddRange(nowe); s.wybieram = true; Ekran(m, s);
                }
                if (ui.Przycisk(b2, "WYCZYŚĆ", U.Szary, U.Krem, !gra && s.wybor.Count > 0)) { s.wybor.Clear(); s.wybieram = true; Ekran(m, s); }
                glowny = stac ? (s.wybor.Count == 0 ? "GRAJ · LOSUJ" : "GRAJ") : "BRAK KREDYTU";
                mozna = !gra && stac && (s.wybor.Count == 0 || s.wybor.Count == K.Typy);
                // the board on the machine's screen: a touch crosses a number or frees it
                if (!gra && m.warstwa && Event.current.type == EventType.MouseUp)
                    for (int n = 1; n <= K.Pola; n++)
                    {
                        Prostokat(m, K.PoleX(n), K.PoleY(n), K.PoleSz, K.PoleWys, out var c0, out var c1, out var c2, out var c3);
                        if (!ui.Dotyk(c0, c1, c2, c3)) continue;
                        if (s.wybor.Contains(n)) s.wybor.Remove(n);
                        else if (s.wybor.Count < K.Typy) s.wybor.Add(n);
                        else BraxChampionsGry.Pokaz("SKREŚLASZ " + K.Typy + " LICZB · ODZNACZ JEDNĄ", 1.6f);
                        s.wybieram = true; Ekran(m, s); break;
                    }
                break;
            }
            default:
                ui.Pole(a, "WYGRANA", Kwota(s.wygrana), s.wygrana > 0 ? U.Zloto : U.Krem);
                if (m.gra == G.Kolo) ui.Pole(b, s.koloKreci ? "KOŁO MISTRZÓW" : "KOŁO MISTRZÓW · DO", s.koloKreci ? "KRĘCI SIĘ" : Kwota(KG.Wygrana(0, st)), U.Zloto);
                else ui.Pole(b, "JACKPOT", Kwota(BraxChampionsKasyno.Jackpot), U.Zloto);
                break;
        }
        if (gra && m.gra != G.Poker) glowny = s.koloKreci ? "KOŁO SIĘ KRĘCI" : m.gra == G.Keno ? "LOSOWANIE" : "KRĘCI SIĘ";
        if (ui.Przycisk(glR, glowny, U.Czerwien, U.Krem, mozna, true)) Graj(i, st);
    }
    // standing up: a dealt video-poker hand is drawn with the cards held (and paid), keno's changes are kept
    public void Wstal(int i)
    {
        var m = maszyny[i]; var s = stan[i];
        if (m.gra == G.Poker && s.faza == 1 && !s.kreci) PokerAkcja(i, m, s, s.st);
        if (m.gra == G.Keno && s.wybieram && !s.kreci) { s.wybieram = false; Ekran(m, s); }
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
            if (s.kreci) { if (m.gra == G.Poker) PokerKrok(i, m, s); else if (m.gra == G.Keno) KenoKrok(m, s); else Krec(m, s); }
            if (s.koloKreci) KoloKrok(m, s);
        }
        int bliska = g ? Najblizsza(gp) : -1;
        if (bliska >= 0)
        {
            var s = stan[bliska];
            BraxChampionsGry.Ustaw(nazwa + " · " + G.Nazwy[maszyny[bliska].gra] + " · KREDYT " + BraxChampionsKasyno.Kredyt + " · STAWKA " + stawka + (s.wygrana > 0 ? " · WYGRANA " + s.wygrana : ""));
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
            wygrana = mnoznik * s.st / 5;
        }
        else
        {
            int a = Z.Symbol(G.Pasek(m.gra, 0), Mathf.RoundToInt(s.poz[0])), b = Z.Symbol(G.Pasek(m.gra, 1), Mathf.RoundToInt(s.poz[1])),
                c = Z.Symbol(G.Pasek(m.gra, 2), Mathf.RoundToInt(s.poz[2]));
            if (m.gra == G.Kolo)
            {
                // the wheel machine: its own line pays (three SEVENS fixed), KOŁO on the third reel spins the wheel
                jackpot = false; wygrana = KG.Linia(a, b, c) * s.st;
                if (c == KG.Symbol) KoloStart(s);
            }
            else { int mn = Z.Mechaniczny(a, b, c); jackpot = mn < 0; wygrana = mn > 0 ? mn * s.st : 0; }
        }
        if (jackpot) wygrana = BraxChampionsKasyno.WezJackpot();
        s.wygrana = wygrana;
        if (wygrana > 0)
        {
            BraxChampionsKasyno.Wplac(wygrana);
            BraxChampionsGry.Pokaz(jackpot ? "JACKPOT! " + wygrana : "WYGRANA " + wygrana + "!", jackpot ? 4f : 2.2f);
        }
        else if (s.koloKreci) BraxChampionsGry.Pokaz("KOŁO MISTRZÓW!", 2.2f);
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
