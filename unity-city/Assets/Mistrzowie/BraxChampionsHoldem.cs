using System;
using System.Collections.Generic;
using UnityEngine;

// MIASTO MISTRZÓW - CASINO HOLD'EM, the casinos' poker against the house (owner 16.11, the casinos' tables: "poker Casino
// Hold'em"; his POKER sign over the tables, his cards and chips). The game as the casinos deal it (the rules of play of the
// Nevada Gaming Control Board, Wikipedia "Casino hold 'em"): one deck shuffled for every hand. AKCJA at a spot puts the ANTE
// (10); two cards to the player, two face down to the dealer, three to the board (the flop). Then the one decision: AKCJA =
// CALL (twice the ante, 20), or - 5 s without it - PAS (the ante is lost). On a call the turn and the river come and the
// dealer shows; he plays from a pair of fours. Not qualified: the ante pays by its table, the call comes back. Qualified:
// the better best-five of the seven cards wins - the ante by its table and the call 1:1; a tie returns both. The ante's
// table: royal flush 100:1, straight flush 20:1, four of a kind 10:1, full house 3:1, flush 2:1, a straight or less 1:1
// (the house edge 2.16 %). The money is the casino credit (BraxChampionsKasyno).
public sealed class BraxChampionsHoldem : MonoBehaviour, IBraxInterakcja
{
    [Serializable] public sealed class Stol
    {
        public Transform stol;                                   // origin on the floor under the dealer's edge, +z the players' side
        public Vector3 but;                                      // where the cards come from (table frame)
        public Vector3[] miejsca = new Vector3[5];               // each spot's ANTE circle (table frame)
        public Vector3[] call = new Vector3[5];                  // its CALL box
        public Vector3 krupier, plansza;                         // the dealer's first card; the board's middle card
        public Transform[] ante = new Transform[5], calle = new Transform[5];   // the chip stacks (shown while bet)
    }
    public Stol[] stoly = new Stol[0];
    public Material karty;
    public string nazwa = "CASINO HOLD'EM";
    public int stawka = 10;
    public float filcY = .80f, decyzja = 5f;

    // ---------------- the hands: the best five of up to seven cards as one number (bigger beats smaller, equal ties)
    public const int WysokaKarta = 0, Para = 1, DwiePary = 2, Trojka = 3, Strit = 4, Kolor = 5, Full = 6, Kareta = 7, Poker = 8, PokerKrolewski = 9;
    public static readonly string[] Uklady = { "WYSOKA KARTA", "PARA", "DWIE PARY", "TRÓJKA", "STRIT", "KOLOR", "FULL", "KARETA", "POKER", "POKER KRÓLEWSKI" };
    static readonly string[] Pary = { "DWÓJEK", "TRÓJEK", "CZWÓREK", "PIĄTEK", "SZÓSTEK", "SIÓDEMEK", "ÓSEMEK", "DZIEWIĄTEK", "DZIESIĄTEK", "WALETÓW", "DAM", "KRÓLI", "ASÓW" };
    // a card c = colour * 13 + rank (rank 0 the two .. 12 the ace), as the owner's card atlas
    public static int Ocena(IList<int> karty)
    {
        var ile = new int[13]; var wKolorze = new int[4]; var maskaKoloru = new int[4]; int maska = 0;
        foreach (int c in karty) { int r = c % 13, k = c / 13; ile[r]++; wKolorze[k]++; maskaKoloru[k] |= 1 << r; maska |= 1 << r; }
        int kolor = -1; for (int k = 0; k < 4; k++) if (wKolorze[k] >= 5) kolor = k;
        if (kolor >= 0) { int sf = NajwyzszyStrit(maskaKoloru[kolor]); if (sf >= 0) return Pakuj(sf == 12 ? PokerKrolewski : Poker, sf); }
        int kareta = -1; var trojki = new List<int>(); var pary = new List<int>();
        for (int r = 12; r >= 0; r--) { if (ile[r] == 4) kareta = r; else if (ile[r] == 3) trojki.Add(r); else if (ile[r] == 2) pary.Add(r); }
        if (kareta >= 0) return Pakuj(Kareta, kareta, Gorne(maska & ~(1 << kareta), 1));
        if (trojki.Count > 0)
        {
            int p = Mathf.Max(trojki.Count > 1 ? trojki[1] : -1, pary.Count > 0 ? pary[0] : -1);
            if (p >= 0) return Pakuj(Full, trojki[0], p);
        }
        if (kolor >= 0) return Pakuj(Kolor, Gorne(maskaKoloru[kolor], 5));
        int st = NajwyzszyStrit(maska); if (st >= 0) return Pakuj(Strit, st);
        if (trojki.Count > 0) return Pakuj(Trojka, trojki[0], Gorne(maska & ~(1 << trojki[0]), 2));
        if (pary.Count > 1) return Pakuj(DwiePary, pary[0], pary[1], Gorne(maska & ~(1 << pary[0]) & ~(1 << pary[1]), 1));
        if (pary.Count > 0) return Pakuj(Para, pary[0], Gorne(maska & ~(1 << pary[0]), 3));
        return Pakuj(WysokaKarta, Gorne(maska, 5));
    }
    // the highest card of a straight in the ranks' mask (the wheel A-2-3-4-5 is the five high), -1 for none
    public static int NajwyzszyStrit(int m)
    {
        for (int hi = 12; hi >= 4; hi--) if ((m >> (hi - 4) & 31) == 31) return hi;
        return (m & 0b1_0000_0000_1111) == 0b1_0000_0000_1111 ? 3 : -1;
    }
    static int[] Gorne(int m, int n)
    {
        var r = new List<int>(); for (int i = 12; i >= 0 && r.Count < n; i--) if ((m >> i & 1) != 0) r.Add(i);
        return r.ToArray();
    }
    static int Pakuj(int uklad, params object[] czesci)
    {
        var r = new List<int>(); foreach (var o in czesci) { if (o is int i) r.Add(i); else foreach (int j in (int[])o) r.Add(j); }
        int v = uklad; for (int i = 0; i < 5; i++) v = v << 4 | (i < r.Count ? r[i] + 1 : 0);
        return v;
    }
    public static int Uklad(int ocena) => ocena >> 20;
    public static string Nazwa(int ocena)
    {
        int u = Uklad(ocena);
        return u == Para ? "PARA " + Pary[(ocena >> 16 & 15) - 1] : Uklady[u];
    }
    // the dealer plays from a pair of fours
    public static bool Kwalifikuje(int krupier) => krupier >= Pakuj(Para, 2);
    public static int Mnoznik(int uklad) => uklad == PokerKrolewski ? 100 : uklad == Poker ? 20 : uklad == Kareta ? 10 : uklad == Full ? 3 : uklad == Kolor ? 2 : 1;
    // what the ANTE and the CALL (2 x ante) get back, the stakes included
    public static int Wyplata(int ante, int gracz, int krupier)
    {
        int call = 2 * ante, a = ante * (1 + Mnoznik(Uklad(gracz)));
        if (!Kwalifikuje(krupier)) return a + call;
        if (gracz > krupier) return a + 2 * call;
        if (gracz == krupier) return ante + call;
        return 0;
    }

    // ---------------- the table's play
    const int Wolny = 0, Rozdanie = 1, Decyzja = 2, Dokladanie = 3, Wynik = 4;
    sealed class Karta { public Transform t; public int c; public Vector3 z, cel; public Quaternion rz, rcel; public float start, lot = .35f, yaw; public bool zakryta; }
    sealed class Stan
    {
        public int faza, miejsce = -1; public float czas, krok; public bool call;
        public readonly List<int> gracz = new List<int>(), krupier = new List<int>(), plansza = new List<int>();
        public readonly List<Karta> karty = new List<Karta>();
        public readonly int[] talia = new int[52]; public int wTalii;
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
            stan[i] = new Stan();
            foreach (var z in stoly[i].ante) if (z) z.gameObject.SetActive(false);
            foreach (var z in stoly[i].calle) if (z) z.gameObject.SetActive(false);
        }
        siatki = BraxChampionsKarty.Siatki();
    }
    // one deck, shuffled for every hand
    void Tasuj(Stan s)
    {
        for (int i = 0; i < 52; i++) s.talia[i] = i;
        for (int i = 51; i > 0; i--) { int j = los.Next(i + 1); int t = s.talia[i]; s.talia[i] = s.talia[j]; s.talia[j] = t; }
        s.wTalii = 0;
    }

    int Najblizszy(Vector3 p, out int miejsce)
    {
        miejsce = -1; int best = -1; float bd = float.MaxValue;
        for (int i = 0; i < stoly.Length; i++)
        {
            var t = stoly[i]; if (t == null || !t.stol) continue;
            var l = t.stol.InverseTransformPoint(p); if (Mathf.Abs(l.y) > 1.2f || l.z < 0f) continue;
            for (int k = 0; k < t.miejsca.Length; k++)
            {
                float d = new Vector2(l.x - t.miejsca[k].x, l.z - t.miejsca[k].z).magnitude;
                if (d < 1.4f && d < bd) { bd = d; best = i; miejsce = k; }
            }
        }
        if (best >= 0 && stan != null && stan[best].faza != Wolny) miejsce = stan[best].miejsce;   // the hand stays at its spot
        return best;
    }
    public string Kontekst(Vector3 g)
    {
        int i = Najblizszy(g, out _); if (i < 0 || stan == null) return "";
        var s = stan[i];
        if (s.faza == Wolny) return BraxChampionsKasyno.Kredyt >= stawka ? nazwa + " · ANTE " + stawka : "BRAK KREDYTU";
        if (s.faza == Decyzja) return "CALL " + 2 * stawka + " · " + Nazwa(Widac(s));
        return nazwa + " · ROZDANIE";
    }
    public bool Wykonaj(Vector3 g)
    {
        int i = Najblizszy(g, out int miejsce); if (i < 0 || stan == null) return false;
        var s = stan[i]; var t = stoly[i];
        if (s.faza == Decyzja)
        {
            if (!BraxChampionsKasyno.Wydaj(2 * stawka)) { BraxChampionsGry.Pokaz("BRAK KREDYTU NA CALL · PAS", 1.6f); Pas(t, s); return true; }
            s.call = true; Zeton(t.calle, s.miejsce, true);
            // the turn and the river, then the dealer turns his two over
            Daj(t, s, 2, 3, true, 0f); Daj(t, s, 2, 4, true, .5f);
            s.faza = Dokladanie; s.krok = Time.time + 1.3f;
            return true;
        }
        if (s.faza != Wolny) return true;
        if (!BraxChampionsKasyno.Wydaj(stawka)) { BraxChampionsGry.Pokaz("BRAK KREDYTU · KASA", 1.6f); return true; }
        Sprzatnij(s); Tasuj(s); s.gracz.Clear(); s.krupier.Clear(); s.plansza.Clear(); s.call = false;
        s.miejsce = Mathf.Clamp(miejsce, 0, t.miejsca.Length - 1);
        Zeton(t.ante, s.miejsce, true);
        // the deal: player, dealer (down), player, dealer (down), the flop
        float d = 0f;
        Daj(t, s, 0, 0, true, d); d += .4f; Daj(t, s, 1, 0, false, d); d += .4f; Daj(t, s, 0, 1, true, d); d += .4f; Daj(t, s, 1, 1, false, d); d += .6f;
        for (int n = 0; n < 3; n++) { Daj(t, s, 2, n, true, d); d += .35f; }
        s.faza = Rozdanie; s.czas = Time.time + d + .3f;
        return true;
    }
    // a card to the player (0), the dealer (1) or the board (2), its n-th, up or down, after a delay
    void Daj(Stol t, Stan s, int komu, int n, bool odkryta, float opoznienie)
    {
        int c = s.talia[s.wTalii++]; (komu == 0 ? s.gracz : komu == 1 ? s.krupier : s.plansza).Add(c);
        var k = new Karta { c = c, t = Wez(c), start = Time.time + opoznienie };
        k.z = t.but + Vector3.up * .01f; k.rz = Quaternion.Euler(0f, 0f, 180f);                       // off the deck face down
        Vector3 cel;
        if (komu == 0)
        {
            // before the player's ANTE circle toward the rail, side by side; their tops to the dealer (upright for him)
            var m = t.miejsca[s.miejsce]; var odKrupiera = new Vector3(m.x, 0f, m.z).normalized; var bok = Vector3.Cross(odKrupiera, Vector3.up);
            cel = m + odKrupiera * .16f + bok * ((n - .5f) * .068f);
            k.yaw = Mathf.Atan2(-m.x, -m.z) * Mathf.Rad2Deg;
        }
        else
        {
            cel = komu == 1 ? t.krupier + Vector3.right * (n * .07f) : t.plansza + Vector3.right * ((n - 2) * .07f);
            k.yaw = 180f;                                                                                  // upright for the players across the felt
        }
        k.cel = new Vector3(cel.x, filcY + .0006f, cel.z);
        k.zakryta = !odkryta; k.rcel = Quaternion.Euler(0f, k.yaw, odkryta ? 0f : 180f);
        k.t.SetParent(t.stol, false); k.t.localPosition = k.z; k.t.localRotation = k.rz; k.t.gameObject.SetActive(opoznienie <= 0f);
        s.karty.Add(k);
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
                case Rozdanie: if (Time.time >= s.czas) { s.faza = Decyzja; s.czas = Time.time + decyzja; } break;
                case Decyzja: if (Time.time >= s.czas) Pas(t, s); break;
                case Dokladanie:
                    if (Time.time < s.krok) break;
                    if (Odkryj(s)) { s.krok = Time.time + .55f; break; }
                    Koniec(t, s); break;
                case Wynik: if (Time.time >= s.czas) { Sprzatnij(s); s.faza = Wolny; } break;
            }
            if (s.faza != Wolny && s.faza != Wynik && Blisko(t)) Panel(s);
        }
    }
    // the player's best hand of the cards already on the felt (his two and the board)
    int Widac(Stan s)
    {
        var v = new List<int>(); foreach (var k in s.karty) if (!k.zakryta && Time.time >= k.start + k.lot && !s.krupier.Contains(k.c)) v.Add(k.c);
        return v.Count > 0 ? Ocena(v) : 0;
    }
    void Panel(Stan s)
    {
        int w = Widac(s); string txt = nazwa + " · ANTE " + stawka + (s.call ? " · CALL " + 2 * stawka : "") + (w > 0 ? " · MASZ " + Nazwa(w) : "");
        if (s.faza == Decyzja) txt += " · AKCJA = CALL " + 2 * stawka + " · PAS ZA " + Mathf.CeilToInt(Mathf.Max(0f, s.czas - Time.time));
        BraxChampionsGry.Ustaw(txt + " · KREDYT " + BraxChampionsKasyno.Kredyt);
    }
    void Pas(Stol t, Stan s)
    {
        BraxChampionsGry.Pokaz("PAS · ANTE " + stawka + " DLA KASYNA", 2.4f);
        Zeton(t.ante, s.miejsce, false); Zeton(t.calle, s.miejsce, false);
        s.faza = Wynik; s.czas = Time.time + 1.6f;
    }
    void Koniec(Stol t, Stan s)
    {
        var g = new List<int>(s.gracz); g.AddRange(s.plansza); var k = new List<int>(s.krupier); k.AddRange(s.plansza);
        int og = Ocena(g), ok = Ocena(k), wyplata = Wyplata(stawka, og, ok);
        if (wyplata > 0) BraxChampionsKasyno.Wplac(wyplata);
        string w = !Kwalifikuje(ok) ? "KRUPIER NIE GRA (" + Nazwa(ok) + ") · ANTE " + Mnoznik(Uklad(og)) + ":1 · WYGRANA " + wyplata
                 : og > ok ? "TY " + Nazwa(og) + " · KRUPIER " + Nazwa(ok) + " · WYGRANA " + wyplata
                 : og == ok ? "REMIS · " + Nazwa(og) + " · ZWROT " + wyplata
                 : "KRUPIER " + Nazwa(ok) + " · TY " + Nazwa(og);
        BraxChampionsGry.Pokaz(w, 3.6f);
        Zeton(t.ante, s.miejsce, false); Zeton(t.calle, s.miejsce, false);
        s.faza = Wynik; s.czas = Time.time + 4f;
    }
    bool Blisko(Stol t)
    {
        var g = BraxChampionsGry.Gracz; if (!g) return false;
        var l = t.stol.InverseTransformPoint(g.transform.position); return l.z > -.5f && l.z < 2.6f && Mathf.Abs(l.x) < 2.6f;
    }
    static void Zeton(Transform[] z, int m, bool widac) { if (m >= 0 && m < z.Length && z[m]) z[m].gameObject.SetActive(widac); }
    // the dealer's cards turned over one by one (true while one was still down)
    bool Odkryj(Stan s)
    {
        foreach (var k in s.karty)
            if (k.zakryta) { k.zakryta = false; k.z = k.cel; k.rz = k.rcel; k.rcel = Quaternion.Euler(0f, k.yaw, 0f); k.start = Time.time; k.lot = .3f; return true; }
        return false;
    }

    // ---------------- the cards: his atlas, two-sided (BraxChampionsKarty)
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
