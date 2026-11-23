using System;
using System.Collections.Generic;
using UnityEngine;
using U = BraxChampionsStanowisko.Ui;

// MIASTO MISTRZÓW - the casinos' baccarat (punto banco) tables, playable (owner 16.11: "Stoły w kasynach ... bakarat"; his
// BAKARAT sign hangs over them, his cards and chips). The game as the casinos deal it: eight decks in the shoe, the cut card
// at the last sixth; two cards to GRACZ and two to BANKIER, the totals modulo 10 (an ace 1, the tens and the faces 0); a
// natural 8 or 9 ends the coup, otherwise the third card by the house tableau (the player draws on 0-5; the banker by his
// total and the player's third card). The player has no decision in baccarat - a perfect one-button game: AKCJA at one of the
// three betting boxes along the rail (BANKIER, REMIS, GRACZ: the box in front of the player, as at the roulette) bets 20 and
// the croupier deals. GRACZ pays 1:1, BANKIER 1:1 less 5 % commission (20 -> 19), REMIS 8:1; a tie returns the bets on GRACZ
// and BANKIER. The money is the casino credit (BraxChampionsKasyno).
// FROM THE CHAIR (owner 23.11 17:35, BraxChampionsStanowisko): the felt from above the chair, the chip chosen (the table's
// stake, 2x, 5x, 10x), chips put on GRACZ, BANKIER and REMIS as at the real table (a touch on the box, on the felt or on its
// button; more than one box may carry a bet), ROZDAJ deals the coup, each box is paid by its own rule.
public sealed class BraxChampionsBakarat : MonoBehaviour, IBraxInterakcja, IBraxStanowisko
{
    [Serializable] public sealed class Stol
    {
        public Transform stol;                                   // origin on the floor under the dealer's edge, +z the players' side
        public Vector3 but;                                      // the shoe's mouth (table frame)
        public Vector3[] pola = new Vector3[3];                  // the three betting boxes: BANKIER, REMIS, GRACZ (table frame)
        public Vector3 gracz, bankier;                           // the first card of each hand on the dealer's side (rows along -x / +x)
        public Transform[] zetony = new Transform[3];            // a chip stack on each box (shown while bet)
        public int stawka;                                       // this table's stake (0: the component's; the SALON VIP's high limit)
        public string nazwa;                                     // this table's name (null: the component's)
        public BraxSeat[] siedziska = new BraxSeat[0];           // the chairs at the boxes: sitting down plays the table
    }
    public const int Bankier = 0, Remis = 1, Gracz = 2;
    public static readonly string[] Pola = { "BANKIER", "REMIS", "GRACZ" };
    public Stol[] stoly = new Stol[0];
    public Material karty;
    public string nazwa = "BAKARAT";
    public int stawka = 20;
    int St(Stol t) => t != null && t.stawka > 0 ? t.stawka : stawka;
    string Nz(Stol t) => t != null && !string.IsNullOrEmpty(t.nazwa) ? t.nazwa : nazwa;
    public float filcY = .80f;

    const int Wolny = 0, Rozdanie = 1, Wynik = 2;
    sealed class Karta { public Transform t; public int c; public bool gracz; public Vector3 z, cel; public Quaternion rz, rcel; public float start, lot = .35f; }
    sealed class Stan
    {
        public int faza, pole = -1, wygrana, krok2; public float czas; public bool siedzi; public string wynik = "";
        public readonly int[] zaklad = new int[3];              // the chips on BANKIER, REMIS, GRACZ
        public readonly List<int> gracz = new List<int>(), bankier = new List<int>();
        public readonly List<Karta> karty = new List<Karta>();
        public int[] but; public int wBucie;
    }
    Stan[] stan; System.Random los; Mesh[] siatki; readonly Stack<Transform> wolne = new Stack<Transform>();

    void OnEnable() { BraxInterakcje.Dodaj(this); }
    void OnDisable() { BraxInterakcje.Usun(this); }
    void Start()
    {
        los = new System.Random(Environment.TickCount ^ GetInstanceID());
        stan = new Stan[stoly.Length];
        for (int i = 0; i < stoly.Length; i++)
        {
            stan[i] = new Stan(); Tasuj(stan[i]); foreach (var z in stoly[i].zetony) if (z) z.gameObject.SetActive(false);
            if (stoly[i].siedziska != null) for (int k = 0; k < stoly[i].siedziska.Length; k++) BraxChampionsStanowisko.Rejestruj(this, stoly[i].siedziska[k], i * 8 + k);
        }
        siatki = BraxChampionsKarty.Siatki();
    }

    // ---------------- the shoe: eight decks
    void Tasuj(Stan s)
    {
        s.but = new int[416]; for (int i = 0; i < 416; i++) s.but[i] = i % 52;
        for (int i = 415; i > 0; i--) { int j = los.Next(i + 1); int t = s.but[i]; s.but[i] = s.but[j]; s.but[j] = t; }
        s.wBucie = 0;
    }
    int Ciagnij(Stan s) { if (s.wBucie >= s.but.Length) Tasuj(s); return s.but[s.wBucie++]; }

    // ---------------- the rules (punto banco)
    public static int Punkty(int c) { int r = c % 13; return r == 12 ? 1 : r >= 8 ? 0 : r + 2; }                // A 1, 2..9, 10 J Q K 0
    public static int Suma(List<int> reka) { int s = 0; foreach (var c in reka) s += Punkty(c); return s % 10; }
    public static bool GraczDobiera(int gracz) => gracz <= 5;
    // the banker's tableau: his total b, the player's third card's points p3 (-1 when the player stood)
    public static bool BankierDobiera(int b, int p3)
    {
        if (p3 < 0) return b <= 5;
        if (b <= 2) return true;
        if (b == 3) return p3 != 8;
        if (b == 4) return p3 >= 2 && p3 <= 7;
        if (b == 5) return p3 >= 4 && p3 <= 7;
        if (b == 6) return p3 == 6 || p3 == 7;
        return false;
    }
    public static int Zwyciezca(int gracz, int bankier) => gracz > bankier ? Gracz : bankier > gracz ? Bankier : Remis;
    // what a bet on `pole` gets back after the coup won by `wygral`: the commission on the banker
    public static int Wyplata(int pole, int wygral, int b)
    {
        if (wygral == Remis) return pole == Remis ? b * 9 : b;                       // 8:1 on the tie; the other bets push
        if (pole != wygral) return 0;
        return pole == Gracz ? b * 2 : b + b * 95 / 100;                             // 1:1; 1:1 less 5 %
    }

    // ---------------- the player: the box in front of him
    int Najblizszy(Vector3 p, out int pole)
    {
        pole = -1; int best = -1; float bd = float.MaxValue;
        for (int i = 0; i < stoly.Length; i++)
        {
            var t = stoly[i]; if (t == null || !t.stol) continue;
            var l = t.stol.InverseTransformPoint(p); if (Mathf.Abs(l.y) > 1.2f || l.z < 0f) continue;
            for (int k = 0; k < t.pola.Length; k++)
            {
                float d = new Vector2(l.x - t.pola[k].x, l.z - t.pola[k].z).magnitude;
                if (d < 1.5f && d < bd) { bd = d; best = i; pole = k; }
            }
        }
        return best;
    }
    public string Kontekst(Vector3 g)
    {
        int i = Najblizszy(g, out int pole); if (i < 0 || stan == null) return "";
        var t = stoly[i];
        if (MaSiedziska(t)) return "";                            // played from its chairs (USIĄDŹ, BraxChampionsStanowisko)
        if (stan[i].faza != Wolny) return Nz(t) + " · ROZDANIE";
        return BraxChampionsKasyno.Kredyt >= St(t) ? "POSTAW " + St(t) + " · " + Pola[pole] : "BRAK KREDYTU";
    }
    public bool Wykonaj(Vector3 g)
    {
        int i = Najblizszy(g, out int pole); if (i < 0 || stan == null || MaSiedziska(stoly[i])) return false;
        var s = stan[i]; var t = stoly[i]; if (s.faza != Wolny) return true;
        if (!BraxChampionsKasyno.Wydaj(St(t))) { BraxChampionsGry.Pokaz("BRAK KREDYTU · KASA", 1.6f); return true; }
        System.Array.Clear(s.zaklad, 0, 3); s.zaklad[pole] = St(t); s.pole = pole; s.siedzi = false;
        return Rozdaj(t, s);
    }
    static bool MaSiedziska(Stol t) { if (t.siedziska != null) foreach (var x in t.siedziska) if (x) return true; return false; }
    // the coup on the bets lying on the boxes (already taken from the credit)
    bool Rozdaj(Stol t, Stan s)
    {
        if (s.faza != Wolny) return true;
        if (s.wBucie > 346) Tasuj(s);                                // the cut card: the last sixth of the shoe stays
        Sprzatnij(s); s.gracz.Clear(); s.bankier.Clear(); s.wygrana = 0; s.wynik = "";
        for (int k = 0; k < 3 && k < t.zetony.Length; k++) if (t.zetony[k]) t.zetony[k].gameObject.SetActive(s.zaklad[k] > 0);
        // the coup: player, banker, player, banker - then the third cards by the tableau
        float d = 0f;
        Daj(t, s, true, d); d += .45f; Daj(t, s, false, d); d += .45f; Daj(t, s, true, d); d += .45f; Daj(t, s, false, d); d += .8f;
        int g2 = Suma(s.gracz), b2 = Suma(s.bankier);
        if (g2 < 8 && b2 < 8)
        {
            int p3 = -1;
            if (GraczDobiera(g2)) { p3 = Punkty(Daj(t, s, true, d)); d += .9f; }
            if (BankierDobiera(Suma(s.bankier), p3)) { Daj(t, s, false, d); d += .9f; }
        }
        s.faza = Rozdanie; s.czas = Time.time + d + .5f;
        return true;
    }
    int Daj(Stol t, Stan s, bool gracz, float opoznienie)
    {
        int c = Ciagnij(s); var reka = gracz ? s.gracz : s.bankier; int n = reka.Count; reka.Add(c);
        var k = new Karta { t = Wez(c), c = c, gracz = gracz, start = Time.time + opoznienie };
        k.z = t.but + Vector3.up * .01f; k.rz = Quaternion.Euler(0f, 0f, 180f);
        // the player's hand to the dealer's left, the banker's to his right; the third card turned across, as dealt
        var cel = (gracz ? t.gracz + Vector3.left * (n * .07f) : t.bankier + Vector3.right * (n * .07f));
        if (n == 2) cel += Vector3.forward * .01f;
        k.cel = new Vector3(cel.x, filcY + .0006f, cel.z);
        k.rcel = Quaternion.Euler(0f, n == 2 ? 270f : 180f, 0f);          // upright for the players across the felt
        k.t.SetParent(t.stol, false); k.t.localPosition = k.z; k.t.localRotation = k.rz; k.t.gameObject.SetActive(opoznienie <= 0f);
        s.karty.Add(k); return c;
    }
    void Update()
    {
        if (stan == null) return;
        for (int i = 0; i < stoly.Length; i++)
        {
            var t = stoly[i]; var s = stan[i]; if (t == null || !t.stol) continue;
            foreach (var k in s.karty) Lec(k);
            if (s.faza == Rozdanie && Time.time >= s.czas) Koniec(t, s);
            else if (s.faza == Wynik && !s.siedzi && Time.time >= s.czas) { Sprzatnij(s); s.faza = Wolny; }
            if (s.faza != Wolny && !s.siedzi && s.pole >= 0 && Blisko(t))
                BraxChampionsGry.Ustaw(Nz(t) + " · " + Pola[s.pole] + " " + St(t) + " · GRACZ " + Widac(s, true) + " · BANKIER " + Widac(s, false) + " · KREDYT " + BraxChampionsKasyno.Kredyt);
        }
    }
    // the total of a hand's cards already on the felt
    int Widac(Stan s, bool gracz)
    {
        int sum = 0; foreach (var k in s.karty) if (k.gracz == gracz && Time.time >= k.start + k.lot) sum += Punkty(k.c);
        return sum % 10;
    }
    bool Blisko(Stol t)
    {
        var g = BraxChampionsGry.Gracz; if (!g) return false;
        var l = t.stol.InverseTransformPoint(g.transform.position); return l.z > -.5f && l.z < 2.8f && Mathf.Abs(l.x) < 2.8f;
    }
    void Koniec(Stol t, Stan s)
    {
        int g = Suma(s.gracz), b = Suma(s.bankier), w = Zwyciezca(g, b);
        int st = 0, wyplata = 0;
        for (int k = 0; k < 3; k++) { st += s.zaklad[k]; if (s.zaklad[k] > 0) wyplata += Wyplata(k, w, s.zaklad[k]); }
        if (wyplata > 0) BraxChampionsKasyno.Wplac(wyplata);
        string wynik = w == Remis ? "REMIS " + g + " : " + b : (w == Gracz ? "GRACZ" : "BANKIER") + " WYGRYWA " + Mathf.Max(g, b) + " NA " + Mathf.Min(g, b);
        s.wynik = wynik; s.wygrana = wyplata;
        BraxChampionsGry.Pokaz(wynik + (wyplata > st ? " · WYGRANA " + wyplata : wyplata == st ? " · ZWROT " + wyplata : ""), 3.2f);
        s.faza = Wynik; s.czas = Time.time + 3.4f;
        System.Array.Clear(s.zaklad, 0, 3);
        foreach (var z in t.zetony) if (z) z.gameObject.SetActive(false);
    }

    // ================================================================ from the chair (BraxChampionsStanowisko): index = table x 8 + chair
    public string Tytul(int i) => Nz(stoly[i / 8]) + " · MIN. " + St(stoly[i / 8]);
    public void Kadr(int i, Camera cam, out Vector3 oko, out Quaternion obrot)
    {
        var t = stoly[i / 8]; int m = Mathf.Clamp(i % 8, 0, t.pola.Length - 1);
        BraxChampionsStanowisko.KadrStolu(cam, t.stol, t.pola[m], (t.gracz + t.bankier) * .5f, 1.25f, .82f, out oko, out obrot, 50f, .5f);
    }
    static readonly int[] Kolejnosc = { Gracz, Remis, Bankier };
    static readonly string[] Kursy = { "0,95 : 1", "8 : 1", "1 : 1" };
    // a chip of the chosen value onto a box (a finished coup is cleared first, as the dealer does)
    void Postaw(Stol t, Stan s, int pole, int zeton)
    {
        if (s.faza == Wynik) { Sprzatnij(s); s.faza = Wolny; }
        if (s.faza != Wolny) return;
        if (!BraxChampionsKasyno.Wydaj(zeton)) { BraxChampionsGry.Pokaz("BRAK KREDYTU · KASA", 1.6f); return; }
        s.zaklad[pole] += zeton; s.pole = pole;
        if (pole < t.zetony.Length && t.zetony[pole]) t.zetony[pole].gameObject.SetActive(true);
    }
    void Zdejmij(Stol t, Stan s)
    {
        int suma = 0; for (int k = 0; k < 3; k++) { suma += s.zaklad[k]; s.zaklad[k] = 0; }
        if (suma > 0) BraxChampionsKasyno.Wplac(suma);
        foreach (var z in t.zetony) if (z) z.gameObject.SetActive(false);
    }
    public void Panel(int i, U ui)
    {
        var t = stoly[i / 8]; var s = stan[i / 8]; s.siedzi = true;      // the coup's result waits for him (no clearing clock)
        var r = ui.dol; float p = ui.m, h = r.height - 2f * p, y = r.y + p;
        var kroki = BraxChampionsStanowisko.Stawki(St(t));
        bool wolny = s.faza == Wolny || s.faza == Wynik;
        var stR = new Rect(r.x + p, y, r.width * .24f, h);
        s.krok2 = ui.Stawka(stR, kroki, s.krok2, wolny, "ŻETON");
        int zeton = kroki[Mathf.Clamp(s.krok2, 0, kroki.Length - 1)];
        var glR = new Rect(r.xMax - p - r.width * .19f, y, r.width * .19f, h);
        var srR = new Rect(stR.xMax + p, y, glR.x - stR.xMax - 2f * p, h);
        // the three boxes: GRACZ, REMIS, BANKIER - a touch puts a chip
        float bw = (srR.width - 2f * p) / 3f;
        for (int k = 0; k < 3; k++)
        {
            int pole = Kolejnosc[k]; var b = new Rect(srR.x + k * (bw + p), y, bw, h);
            string txt = Pola[pole] + "\n" + (s.zaklad[pole] > 0 ? Kwota(s.zaklad[pole]) : Kursy[pole]);
            if (ui.Przycisk(b, txt, pole == Gracz ? new Color(.06f, .16f, .38f) : pole == Bankier ? new Color(.42f, .05f, .08f) : new Color(.06f, .30f, .16f), U.Krem,
                wolny && BraxChampionsKasyno.Kredyt >= zeton, false, U.Zloto)) Postaw(t, s, pole, zeton);
        }
        // the boxes on the felt answer a touch too; their bets are written by them
        var st = t.stol; float rr = .15f;
        for (int pole = 0; pole < 3; pole++)
        {
            var c = t.pola[pole];
            if (s.zaklad[pole] > 0) ui.Etykieta(st.TransformPoint(c + new Vector3(0f, 0f, -.07f)), Kwota(s.zaklad[pole]), new Color(0f, 0f, 0f, .7f), U.Zloto);
            if (wolny && ui.Dotyk(st.TransformPoint(c + new Vector3(-rr, 0f, -rr)), st.TransformPoint(c + new Vector3(rr, 0f, -rr)), st.TransformPoint(c + new Vector3(rr, 0f, rr)), st.TransformPoint(c + new Vector3(-rr, 0f, rr))))
                Postaw(t, s, pole, zeton);
        }
        if (s.faza != Wolny && s.gracz.Count > 0)
        {
            ui.Etykieta(st.TransformPoint(t.gracz + new Vector3(-.04f, 0f, -.09f)), "GRACZ " + Widac(s, true), new Color(0f, 0f, 0f, .7f), U.Krem);
            ui.Etykieta(st.TransformPoint(t.bankier + new Vector3(.04f, 0f, -.09f)), "BANKIER " + Widac(s, false), new Color(0f, 0f, 0f, .7f), U.Krem);
        }
        int razem = s.zaklad[0] + s.zaklad[1] + s.zaklad[2];
        var g1 = new Rect(glR.x, y, glR.width, h * .62f - p * .25f); var g2 = new Rect(glR.x, g1.yMax + p * .5f, glR.width, h - g1.height - p * .5f);
        string g = s.faza == Rozdanie ? "ROZDANIE" : razem > 0 ? "ROZDAJ · " + Kwota(razem) : s.faza == Wynik && s.wygrana > 0 ? "WYGRANA " + Kwota(s.wygrana) : "POSTAW ŻETON";
        if (ui.Przycisk(g1, g, U.Czerwien, U.Krem, s.faza == Wolny && razem > 0, true)) Rozdaj(t, s);
        if (ui.Przycisk(g2, "ZDEJMIJ", U.Szary, U.Krem, s.faza == Wolny && razem > 0)) Zdejmij(t, s);
    }
    static string Kwota(int v) => BraxChampionsAutomaty.Kwota(v);
    // standing up: the chips not yet played come back
    public void Wstal(int i)
    {
        var t = stoly[i / 8]; var s = stan[i / 8];
        if (s.faza == Wolny) Zdejmij(t, s);
        s.siedzi = false;
    }

    void Lec(Karta k)
    {
        if (!k.t) return;
        float u = (Time.time - k.start) / k.lot; if (u < 0f) return;
        if (!k.t.gameObject.activeSelf) k.t.gameObject.SetActive(true);
        u = Mathf.Clamp01(u); float e = 1f - (1f - u) * (1f - u);
        k.t.localPosition = Vector3.Lerp(k.z, k.cel, e) + Vector3.up * (.05f * Mathf.Sin(u * Mathf.PI));
        k.t.localRotation = Quaternion.Slerp(k.rz, k.rcel, e);
    }
    Transform Wez(int c)
    {
        var t = wolne.Count > 0 ? wolne.Pop() : null;
        if (!t)
        {
            t = new GameObject("karta").transform; t.gameObject.AddComponent<MeshFilter>();
            var r = t.gameObject.AddComponent<MeshRenderer>(); r.sharedMaterial = karty; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        t.GetComponent<MeshFilter>().sharedMesh = siatki != null && c >= 0 && c < siatki.Length ? siatki[c] : null;
        t.gameObject.SetActive(true); return t;
    }
    void Sprzatnij(Stan s)
    {
        foreach (var k in s.karty) if (k.t) { k.t.gameObject.SetActive(false); k.t.SetParent(transform, false); wolne.Push(k.t); }
        s.karty.Clear();
    }
}
