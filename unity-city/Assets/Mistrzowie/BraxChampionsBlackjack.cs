using System;
using System.Collections.Generic;
using UnityEngine;
using U = BraxChampionsStanowisko.Ui;

// MIASTO MISTRZÓW - the casinos' blackjack tables, playable (owner 16.11: "blackjack grywalny w obu kasynach"; the cards are
// his - the king, the queen, the jack and the back of his card art in the card atlas, BraxChampionsAutomatyGry.PokerGra). The
// rules of the real tables (six decks in the shoe, the cut card at three quarters, the dealer stands on every 17, blackjack
// pays 3 to 2, a push returns the bet): AKCJA at a player's spot puts a 10 chip on its circle and the dealer deals - two
// cards up to the player, one up and one down to himself; while the hand is open AKCJA is DOBIERZ (hit), and standing still
// for five seconds is STÓJ (the panel counts it down) - the one button of the phone; 21 stands by itself. A ten or an ace
// up: the dealer peeks, his blackjack ends the hand at once. Then he turns his card and draws to 17. The cards fly from the
// shoe and turn over as a dealer's do. The money is the casino credit (BraxChampionsKasyno, the slots' and roulette's).
// FROM THE CHAIR (owner 23.11 17:35, BraxChampionsStanowisko): sitting down at a spot shows the felt from above the chair
// (his cards, the dealer's) and the table's buttons: the bet (the table's minimum, 2x, 5x, 10x), ROZDAJ, then DOBIERZ, STÓJ,
// PODWÓJ (two cards: the bet doubled, one card, done) and ROZDZIEL (two cards of one value: a second hand with the same bet;
// split aces take one card each; 21 after a split is 21, not blackjack) - no clock while he sits, as at a real table.
public sealed class BraxChampionsBlackjack : MonoBehaviour, IBraxInterakcja, IBraxStanowisko
{
    [Serializable] public sealed class Stol
    {
        public Transform stol;                                   // origin on the floor under the felt's middle, +z the players' side
        public Vector3 but;                                      // the shoe's mouth (table frame)
        public Vector3[] miejsca = new Vector3[0];               // the betting circles on the felt (table frame)
        public Vector3 krupier;                                  // the dealer's first card (table frame), his row runs along +x
        public Transform[] zetony = new Transform[0];            // a chip stack on each circle (shown while bet)
        public int stawka;                                       // this table's stake (0: the component's; the SALON VIP's high limit)
        public string nazwa;                                     // this table's name (null: the component's)
        public BraxSeat[] siedziska = new BraxSeat[0];           // the chair behind each spot: sitting down plays it
    }
    public Stol[] stoly = new Stol[0];
    public Material karty;                                       // the card atlas (his J / Q / K, his back), lit as card stock
    public string nazwa = "BLACKJACK";
    public int stawka = 10;
    int St(Stol t) => t != null && t.stawka > 0 ? t.stawka : stawka;
    string Nz(Stol t) => t != null && !string.IsNullOrEmpty(t.nazwa) ? t.nazwa : nazwa;
    public float decyzja = 5f, filcY = .80f;
    public const float KartaSz = BraxChampionsKarty.Szer, KartaDl = BraxChampionsKarty.Dl;   // a casino card: 63 x 88 mm

    const int Wolny = 0, Rozdanie = 1, Decyzja = 2, Krupier = 3, Wynik = 4;
    sealed class Karta { public Transform t; public int c, reka, n; public bool gracza; public Vector3 z, cel; public Quaternion rz, rcel; public float start, lot = .35f, yaw; public bool zakryta; }
    sealed class Stan
    {
        public int faza, miejsce = -1, reka, wygrana; public float czas, krok; public bool siedzi, rozdzielone; public string wynik = "";
        public readonly List<int> gracz = new List<int>(), gracz2 = new List<int>(), krupier = new List<int>();
        public readonly int[] zaklad = new int[2];               // the bet of each hand (the second only after a split)
        public readonly List<Karta> karty = new List<Karta>();
        public int[] but; public int wBucie;
        public int krok2 = -1;                                   // the bet the seated player chose (index of the table's stakes)
        public List<int> Reka(int r) => r == 0 ? gracz : gracz2;
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
            stan[i] = new Stan(); Tasuj(stan[i]);
            foreach (var z in stoly[i].zetony) if (z) z.gameObject.SetActive(false);
            if (stoly[i].siedziska != null) for (int k = 0; k < stoly[i].siedziska.Length; k++) BraxChampionsStanowisko.Rejestruj(this, stoly[i].siedziska[k], i * 8 + k);
        }
        Siatki();
    }

    // ---------------- the shoe: six decks, shuffled; the cut card at three quarters
    void Tasuj(Stan s)
    {
        s.but = new int[312]; for (int i = 0; i < 312; i++) s.but[i] = i % 52;
        for (int i = 311; i > 0; i--) { int j = los.Next(i + 1); int t = s.but[i]; s.but[i] = s.but[j]; s.but[j] = t; }
        s.wBucie = 0;
    }
    int Ciagnij(Stan s) { if (s.wBucie >= s.but.Length) Tasuj(s); return s.but[s.wBucie++]; }

    // ---------------- the rules
    public static int Wartosc(int c) { int r = c % 13; return r == 12 ? 11 : r >= 8 ? 10 : r + 2; }     // 2..10, J Q K = 10, A = 11
    public static int Suma(List<int> reka, out bool miekka)
    {
        int s = 0, asy = 0; foreach (var c in reka) { s += Wartosc(c); if (c % 13 == 12) asy++; }
        while (s > 21 && asy > 0) { s -= 10; asy--; }
        miekka = asy > 0; return s;
    }
    public static bool Blackjack(List<int> reka) => reka.Count == 2 && Suma(reka, out _) == 21;
    // what the hand pays back for a bet b: 0 lost, b push, 2b won, 2.5b blackjack
    public static int Wyplata(List<int> gracz, List<int> krupier, int b) => Wyplata(gracz, krupier, b, false);
    // after a split two cards of 21 are 21, not a blackjack
    public static int Wyplata(List<int> gracz, List<int> krupier, int b, bool poRozdzieleniu)
    {
        int g = Suma(gracz, out _), k = Suma(krupier, out _);
        bool gbj = !poRozdzieleniu && Blackjack(gracz), kbj = Blackjack(krupier);
        if (gbj && kbj) return b;
        if (gbj) return b * 5 / 2;
        if (kbj || g > 21) return 0;
        if (k > 21 || g > k) return b * 2;
        return g == k ? b : 0;
    }
    public static bool KrupierDobiera(List<int> krupier) => Suma(krupier, out _) < 17;        // stands on every 17 (soft too)
    public static bool Para(List<int> reka) => reka.Count == 2 && Wartosc(reka[0]) == Wartosc(reka[1]);

    // ---------------- the player's spot (standing behind a chair that has no seat)
    int Najblizszy(Vector3 p, out int miejsce)
    {
        miejsce = -1; int best = -1; float bd = float.MaxValue;
        for (int i = 0; i < stoly.Length; i++)
        {
            var t = stoly[i]; if (t == null || !t.stol || t.miejsca.Length == 0) continue;
            var l = t.stol.InverseTransformPoint(p);
            if (Mathf.Abs(l.y) > 1.2f || l.z < 0f) continue;                              // on the players' side
            for (int k = 0; k < t.miejsca.Length; k++)
            {
                var d = new Vector2(l.x - t.miejsca[k].x, l.z - t.miejsca[k].z).magnitude;
                if (d < 1.4f && d < bd) { bd = d; best = i; miejsce = k; }                  // standing behind its chair
            }
        }
        if (best >= 0 && stan != null && stan[best].faza != Wolny) miejsce = stan[best].miejsce;   // the hand stays at its spot
        return best;
    }
    static bool MaSiedziska(Stol t) { if (t.siedziska != null) foreach (var x in t.siedziska) if (x) return true; return false; }
    public string Kontekst(Vector3 gracz)
    {
        int i = Najblizszy(gracz, out _); if (i < 0 || stan == null) return "";
        var s = stan[i];
        var t = stoly[i];
        if (MaSiedziska(t)) return "";                              // played from its chairs (USIĄDŹ, BraxChampionsStanowisko)
        if (s.faza == Wolny) return BraxChampionsKasyno.Kredyt >= St(t) ? Nz(t) + " · POSTAW " + St(t) : "BRAK KREDYTU";
        if (s.faza == Decyzja) return "DOBIERZ · " + Suma(s.gracz, out _);
        return Nz(t) + " · ROZDANIE";
    }
    public bool Wykonaj(Vector3 gracz)
    {
        int i = Najblizszy(gracz, out int miejsce); if (i < 0 || stan == null || MaSiedziska(stoly[i])) return false;
        var s = stan[i]; var t = stoly[i];
        if (s.faza == Decyzja) { Dobierz(t, s); s.czas = Time.time + decyzja; return true; }
        if (s.faza != Wolny) return true;
        return Rozdaj(t, s, miejsce, St(t), false);
    }

    // ---------------- a hand: the bet on the circle, the deal as at the table (player, dealer up, player, dealer down)
    bool Rozdaj(Stol t, Stan s, int miejsce, int bet, bool siedzi)
    {
        if (s.faza == Wynik) { Sprzatnij(s); s.faza = Wolny; }
        if (s.faza != Wolny) return true;
        if (!BraxChampionsKasyno.Wydaj(bet)) { BraxChampionsGry.Pokaz("BRAK KREDYTU · KASA", 1.6f); return true; }
        if (s.wBucie > 234) Tasuj(s);                               // the cut card came out: a new shoe
        Sprzatnij(s); s.gracz.Clear(); s.gracz2.Clear(); s.krupier.Clear();
        s.miejsce = Mathf.Clamp(miejsce, 0, t.miejsca.Length - 1); s.faza = Rozdanie; s.krok = Time.time; s.czas = 0f;
        s.siedzi = siedzi; s.reka = 0; s.rozdzielone = false; s.zaklad[0] = bet; s.zaklad[1] = 0; s.wygrana = 0; s.wynik = "";
        if (s.miejsce < t.zetony.Length && t.zetony[s.miejsce]) t.zetony[s.miejsce].gameObject.SetActive(true);
        Daj(t, s, true, 0, true, 0f); Daj(t, s, false, 0, true, .45f); Daj(t, s, true, 1, true, .9f); Daj(t, s, false, 1, false, 1.35f);
        s.czas = Time.time + 1.9f;
        return true;
    }
    // where a player's card lies: fanned at his circle, each over the last; after a split the two hands side by side
    Vector3 Cel(Stol t, Stan s, int reka, int n)
    {
        var m = t.miejsca[s.miejsce]; var doKrupiera = new Vector3(-m.x, 0f, -m.z).normalized;
        var bok = Vector3.Cross(Vector3.up, doKrupiera);
        float r = s.rozdzielone ? (reka == 0 ? -.075f : .075f) : 0f;
        var c = m + doKrupiera * .11f + bok * (r + n * .018f) - doKrupiera * (n * .012f);
        return new Vector3(c.x, filcY + .0006f + n * .0004f, c.z);
    }
    // a card from the shoe to the player's hand (reka) or the dealer's row, up or down, after a delay
    void Daj(Stol t, Stan s, bool gracz, int n, bool odkryta, float opoznienie = 0f, int reka = 0)
    {
        int c = Ciagnij(s); (gracz ? s.Reka(reka) : s.krupier).Add(c);
        var k = new Karta { c = c, t = Wez(c), start = Time.time + opoznienie, gracza = gracz, reka = reka, n = n };
        k.z = t.but + Vector3.up * .01f; k.rz = Quaternion.Euler(0f, 0f, 180f);                       // out of the shoe face down
        if (gracz) k.cel = Cel(t, s, reka, n);
        else { var cel = t.krupier + Vector3.right * (n * .075f); k.cel = new Vector3(cel.x, filcY + .0006f + n * .0004f, cel.z); }
        // the card's top (the atlas' up, its local +z) to the dealer on the player's cards: upright for the player; on the
        // dealer's row to the dealer too (yaw 180): upright for the players across the felt
        k.yaw = gracz ? Mathf.Atan2(-t.miejsca[s.miejsce].x, -t.miejsca[s.miejsce].z) * Mathf.Rad2Deg : 180f;
        k.zakryta = !odkryta; k.rcel = Quaternion.Euler(0f, k.yaw, odkryta ? 0f : 180f);
        k.t.SetParent(t.stol, false); k.t.localPosition = k.z; k.t.localRotation = k.rz; k.t.gameObject.SetActive(opoznienie <= 0f);
        s.karty.Add(k);
    }

    // ---------------- the player's decisions
    void Dobierz(Stol t, Stan s)
    {
        var r = s.Reka(s.reka); Daj(t, s, true, r.Count, true, 0f, s.reka);
        if (Suma(r, out _) >= 21) Dalej(t, s);                    // 21 stands by itself, over 21 the hand is lost
    }
    void Stoj(Stol t, Stan s) => Dalej(t, s);
    bool MozePodwoic(Stan s) => s.faza == Decyzja && s.Reka(s.reka).Count == 2 && BraxChampionsKasyno.Kredyt >= s.zaklad[s.reka];
    void Podwoj(Stol t, Stan s)
    {
        if (!MozePodwoic(s) || !BraxChampionsKasyno.Wydaj(s.zaklad[s.reka])) return;
        s.zaklad[s.reka] *= 2;
        var r = s.Reka(s.reka); Daj(t, s, true, r.Count, true, 0f, s.reka);
        BraxChampionsGry.Pokaz("PODWOJENIE · " + s.zaklad[s.reka], 1.2f);
        Dalej(t, s);
    }
    bool MozeRozdzielic(Stan s) => s.faza == Decyzja && !s.rozdzielone && Para(s.gracz) && BraxChampionsKasyno.Kredyt >= s.zaklad[0];
    void Rozdziel(Stol t, Stan s)
    {
        if (!MozeRozdzielic(s) || !BraxChampionsKasyno.Wydaj(s.zaklad[0])) return;
        s.rozdzielone = true; s.zaklad[1] = s.zaklad[0]; s.reka = 0;
        int c = s.gracz[1]; s.gracz.RemoveAt(1); s.gracz2.Add(c);
        // the second card slides to the new hand, the first one a little aside: two hands at the circle
        foreach (var k in s.karty)
        {
            if (!k.gracza || !k.t) continue;
            if (k.reka == 0 && k.n == 1) { k.reka = 1; k.n = 0; }
            k.z = k.t.localPosition; k.rz = k.t.localRotation; k.cel = Cel(t, s, k.reka, k.n); k.start = Time.time; k.lot = .3f;
        }
        bool asy = Wartosc(c) == 11;
        Daj(t, s, true, 1, true, .35f, 0); Daj(t, s, true, 1, true, .8f, 1);
        BraxChampionsGry.Pokaz(asy ? "ROZDZIELONE ASY · PO JEDNEJ KARCIE" : "ROZDZIELONE · DWIE RĘCE", 1.6f);
        if (asy) { s.reka = 1; s.faza = Krupier; s.krok = Time.time + 1.5f; return; }
        if (Suma(s.gracz, out _) == 21) Dalej(t, s);
    }
    // the next hand of a split, or the dealer (all hands over 21: the hand ends at once)
    void Dalej(Stol t, Stan s)
    {
        if (s.rozdzielone && s.reka == 0) { s.reka = 1; if (Suma(s.gracz2, out _) >= 21) Dalej(t, s); return; }
        bool zywa = Suma(s.gracz, out _) <= 21 || (s.rozdzielone && Suma(s.gracz2, out _) <= 21);
        if (!zywa) { Koniec(t, s, "PRZEBICIE · " + Suma(s.Reka(s.reka), out _)); return; }
        s.faza = Krupier; s.krok = Time.time + .6f;
    }

    void Update()
    {
        if (stan == null) return;
        for (int i = 0; i < stoly.Length; i++)
        {
            var t = stoly[i]; var s = stan[i]; if (t == null || !t.stol) continue;
            foreach (var k in s.karty) Lec(k);
            switch (s.faza)
            {
                case Rozdanie:
                    if (Time.time < s.czas) break;
                    // the peek: an ace or a ten up, his blackjack ends the hand; the player's blackjack is paid at once
                    if (Blackjack(s.krupier) || Blackjack(s.gracz)) { Odkryj(s); Koniec(t, s, null); break; }
                    s.faza = Decyzja; s.czas = Time.time + decyzja; break;
                case Decyzja:
                    if (s.siedzi) break;                                // at the chair the dealer waits for the player
                    if (Time.time >= s.czas) { s.faza = Krupier; s.krok = Time.time + .4f; }
                    break;
                case Krupier:
                    if (Time.time < s.krok) break;
                    if (Odkryj(s)) { s.krok = Time.time + .7f; break; }
                    if (KrupierDobiera(s.krupier)) { Daj(t, s, false, s.krupier.Count, true); s.krok = Time.time + .8f; break; }
                    Koniec(t, s, null); break;
                case Wynik:
                    if (!s.siedzi && Time.time >= s.czas) { Sprzatnij(s); s.faza = Wolny; }
                    break;
            }
            if (s.faza != Wolny && !s.siedzi && Gracz(t)) PanelGorny(t, s);
        }
    }
    bool Gracz(Stol t)
    {
        var g = BraxChampionsGry.Gracz; if (!g) return false;
        var l = t.stol.InverseTransformPoint(g.transform.position); return l.z > -.5f && l.z < 2.4f && Mathf.Abs(l.x) < 2.4f;
    }
    static string Opis(List<int> r) { int g = Suma(r, out bool m); return m && g < 21 ? (g - 10) + "/" + g : g.ToString(); }
    void PanelGorny(Stol t, Stan s)
    {
        int widac = s.faza >= Krupier ? Suma(s.krupier, out _) : (s.krupier.Count > 0 ? Wartosc(s.krupier[0]) : 0);
        string txt = Nz(t) + " · TY " + Opis(s.gracz) + " · KRUPIER " + widac;
        if (s.faza == Decyzja) txt += " · AKCJA = DOBIERZ · STÓJ ZA " + Mathf.CeilToInt(Mathf.Max(0f, s.czas - Time.time));
        BraxChampionsGry.Ustaw(txt + " · KREDYT " + BraxChampionsKasyno.Kredyt);
    }
    // the hole card turned over (true when it was still down)
    bool Odkryj(Stan s)
    {
        foreach (var k in s.karty)
            if (k.zakryta) { k.zakryta = false; k.z = k.cel; k.rz = k.rcel; k.rcel = Quaternion.Euler(0f, k.yaw, 0f); k.start = Time.time; k.lot = .3f; return true; }
        return false;
    }
    void Koniec(Stol t, Stan s, string przebicie)
    {
        int rak = s.rozdzielone ? 2 : 1, suma = 0, kr = Suma(s.krupier, out _); var opis = new System.Text.StringBuilder();
        for (int r = 0; r < rak; r++)
        {
            var reka = s.Reka(r); int b = s.zaklad[r], g = Suma(reka, out _);
            int w = przebicie != null || g > 21 ? 0 : Wyplata(reka, s.krupier, b, s.rozdzielone);
            suma += w;
            string jak = g > 21 ? "PRZEBICIE " + g : w == b * 5 / 2 && !s.rozdzielone && Blackjack(reka) ? "BLACKJACK!" : w == b * 2 ? "WYGRANA " + w + " · " + g + " NA " + kr
                       : w == b ? "REMIS · " + g : Blackjack(s.krupier) ? "BLACKJACK KRUPIERA" : "KRUPIER WYGRYWA · " + kr + " NA " + g;
            if (rak > 1) opis.Append(r == 0 ? "RĘKA 1: " : " · RĘKA 2: ");
            opis.Append(jak);
        }
        if (suma > 0) BraxChampionsKasyno.Wplac(suma);
        if (rak > 1 && suma > 0) opis.Append(" · RAZEM " + suma);
        else if (rak == 1 && suma == s.zaklad[0] * 5 / 2 && Blackjack(s.gracz)) opis.Append(" · WYGRANA " + suma);
        s.wygrana = suma; s.wynik = opis.ToString();
        BraxChampionsGry.Pokaz(s.wynik, 3f);
        s.faza = Wynik; s.czas = Time.time + 3.2f;
        if (s.miejsce >= 0 && s.miejsce < t.zetony.Length && t.zetony[s.miejsce]) t.zetony[s.miejsce].gameObject.SetActive(false);
    }

    // ================================================================ from the chair (BraxChampionsStanowisko): index = table x 8 + spot
    public string Tytul(int i) => Nz(stoly[i / 8]) + " · MIEJSCE " + (i % 8 + 1) + " · MIN. " + St(stoly[i / 8]);
    public void Kadr(int i, Camera cam, out Vector3 oko, out Quaternion obrot)
    {
        var t = stoly[i / 8]; int m = Mathf.Clamp(i % 8, 0, t.miejsca.Length - 1);
        BraxChampionsStanowisko.KadrStolu(cam, t.stol, t.miejsca[m], t.krupier + Vector3.right * .07f, 1.05f, .74f, out oko, out obrot);
    }
    public void Panel(int i, U ui)
    {
        var t = stoly[i / 8]; var s = stan[i / 8]; int m = Mathf.Clamp(i % 8, 0, t.miejsca.Length - 1);
        var r = ui.dol; float p = ui.m, h = r.height - 2f * p, y = r.y + p;
        var kroki = BraxChampionsStanowisko.Stawki(St(t)); if (s.krok2 < 0) s.krok2 = 0;
        bool wolny = s.faza == Wolny || s.faza == Wynik;
        var stR = new Rect(r.x + p, y, r.width * .27f, h);
        s.krok2 = ui.Stawka(stR, kroki, s.krok2, wolny, "ZAKŁAD");
        int bet = kroki[s.krok2];
        var info = new Rect(stR.xMax + p, y, r.width * .20f, h);
        var akcje = new Rect(info.xMax + p, y, r.xMax - p - info.xMax - p, h);
        bool mojaReka = s.miejsce == m && s.faza != Wolny;
        if (mojaReka)
        {
            int widac = s.faza >= Krupier ? Suma(s.krupier, out _) : (s.krupier.Count > 0 ? Wartosc(s.krupier[0]) : 0);
            string ty = s.rozdzielone ? Opis(s.gracz) + " | " + Opis(s.gracz2) : Opis(s.gracz);
            ui.Pole(info, "TY · KRUPIER " + widac, ty, U.Zloto);
            // the totals on the felt, by the cards
            var st = t.stol;
            for (int k = 0; k < (s.rozdzielone ? 2 : 1); k++)
                if (s.Reka(k).Count > 0) ui.Etykieta(st.TransformPoint(Cel(t, s, k, 0) - new Vector3(t.miejsca[m].x, 0f, t.miejsca[m].z).normalized * .07f),
                    Opis(s.Reka(k)), new Color(0f, 0f, 0f, .7f), s.reka == k && s.faza == Decyzja ? U.Zloto : U.Krem);
            if (s.krupier.Count > 0) ui.Etykieta(st.TransformPoint(t.krupier + new Vector3(.04f, 0f, -.08f)), "KRUPIER " + widac, new Color(0f, 0f, 0f, .7f), U.Krem);
        }
        else ui.Pole(info, "BLACKJACK", "3 : 2", U.Zloto);
        if (s.faza == Decyzja && s.miejsce == m)
        {
            float bw = (akcje.width - 3f * p) / 4f;
            Rect B(int k) => new Rect(akcje.x + k * (bw + p), y, bw, h);
            if (ui.Przycisk(B(0), "DOBIERZ", U.Zielen, U.Krem, true, true)) Dobierz(t, s);
            else if (ui.Przycisk(B(1), "STÓJ", U.Czerwien, U.Krem, true, true)) Stoj(t, s);
            else if (ui.Przycisk(B(2), "PODWÓJ", U.Granat, U.Krem, MozePodwoic(s), false, U.Zloto)) Podwoj(t, s);
            else if (ui.Przycisk(B(3), "ROZDZIEL", U.Granat, U.Krem, MozeRozdzielic(s), false, U.Zloto)) Rozdziel(t, s);
            return;
        }
        string g = s.faza == Rozdanie ? "ROZDANIE" : s.faza == Krupier ? "KRUPIER DOBIERA" : BraxChampionsKasyno.Kredyt >= bet ? "ROZDAJ · " + bet : "BRAK KREDYTU";
        bool mozna = wolny && BraxChampionsKasyno.Kredyt >= bet;
        var wynik = new Rect(akcje.x, y, akcje.width * .52f - p * .5f, h);
        var gl = new Rect(wynik.xMax + p, y, akcje.xMax - wynik.xMax - p, h);
        if (s.faza == Wynik && s.miejsce == m) ui.Pole(wynik, "WYNIK", s.wygrana > 0 ? "+" + s.wygrana : "—", s.wygrana > 0 ? U.Zloto : U.Krem);
        else ui.Pole(wynik, "KRUPIER STOI NA 17", "MIN. " + St(t), U.Krem);
        if (ui.Przycisk(gl, g, U.Czerwien, U.Krem, mozna, true)) Rozdaj(t, s, m, bet, true);
    }
    // standing up: a hand in play goes on as if he stood (his cards stay and are paid), the next one waits for a player
    public void Wstal(int i)
    {
        var t = stoly[i / 8]; var s = stan[i / 8];
        s.siedzi = false;
        if (s.faza == Decyzja) { s.reka = s.rozdzielone ? 1 : 0; Dalej(t, s); }
    }

    // ---------------- the cards: two-sided quads cut from the atlas (face up = his art, face down = his back)
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
    void Siatki() { siatki = BraxChampionsKarty.Siatki(); }
}
