using System;
using System.Collections.Generic;
using UnityEngine;

// MIASTO MISTRZÓW - the casinos' blackjack tables, playable (owner 16.11: "blackjack grywalny w obu kasynach"; the cards are
// his - the king, the queen, the jack and the back of his card art in the card atlas, BraxChampionsAutomatyGry.PokerGra). The
// rules of the real tables (six decks in the shoe, the cut card at three quarters, the dealer stands on every 17, blackjack
// pays 3 to 2, a push returns the bet): AKCJA at a player's spot puts a 10 chip on its circle and the dealer deals - two
// cards up to the player, one up and one down to himself; while the hand is open AKCJA is DOBIERZ (hit), and standing still
// for five seconds is STÓJ (the panel counts it down) - the one button of the phone; 21 stands by itself. A ten or an ace
// up: the dealer peeks, his blackjack ends the hand at once. Then he turns his card and draws to 17. The cards fly from the
// shoe and turn over as a dealer's do. The money is the casino credit (BraxChampionsKasyno, the slots' and roulette's).
public sealed class BraxChampionsBlackjack : MonoBehaviour, IBraxInterakcja
{
    [Serializable] public sealed class Stol
    {
        public Transform stol;                                   // origin on the floor under the felt's middle, +z the players' side
        public Vector3 but;                                      // the shoe's mouth (table frame)
        public Vector3[] miejsca = new Vector3[0];               // the betting circles on the felt (table frame)
        public Vector3 krupier;                                  // the dealer's first card (table frame), his row runs along +x
        public Transform[] zetony = new Transform[0];            // a chip stack on each circle (shown while bet)
    }
    public Stol[] stoly = new Stol[0];
    public Material karty;                                       // the card atlas (his J / Q / K, his back), lit as card stock
    public string nazwa = "BLACKJACK";
    public int stawka = 10;
    public float decyzja = 5f, filcY = .80f;
    public const float KartaSz = .063f, KartaDl = .088f;         // a casino card: 63 x 88 mm

    const int Wolny = 0, Rozdanie = 1, Decyzja = 2, Krupier = 3, Wynik = 4;
    sealed class Karta { public Transform t; public int c; public Vector3 z, cel; public Quaternion rz, rcel; public float start, lot = .35f, yaw; public bool zakryta; }
    sealed class Stan
    {
        public int faza, miejsce = -1; public float czas, krok;
        public readonly List<int> gracz = new List<int>(), krupier = new List<int>();
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
            stan[i] = new Stan(); Tasuj(stan[i]);
            foreach (var z in stoly[i].zetony) if (z) z.gameObject.SetActive(false);
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
    public static int Wyplata(List<int> gracz, List<int> krupier, int b)
    {
        int g = Suma(gracz, out _), k = Suma(krupier, out _);
        bool gbj = Blackjack(gracz), kbj = Blackjack(krupier);
        if (gbj && kbj) return b;
        if (gbj) return b * 5 / 2;
        if (kbj || g > 21) return 0;
        if (k > 21 || g > k) return b * 2;
        return g == k ? b : 0;
    }
    public static bool KrupierDobiera(List<int> krupier) => Suma(krupier, out _) < 17;        // stands on every 17 (soft too)

    // ---------------- the player's spot
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
    public string Kontekst(Vector3 gracz)
    {
        int i = Najblizszy(gracz, out _); if (i < 0 || stan == null) return "";
        var s = stan[i];
        if (s.faza == Wolny) return BraxChampionsKasyno.Kredyt >= stawka ? nazwa + " · POSTAW " + stawka : "BRAK KREDYTU";
        if (s.faza == Decyzja) return "DOBIERZ · " + Suma(s.gracz, out _);
        return nazwa + " · ROZDANIE";
    }
    public bool Wykonaj(Vector3 gracz)
    {
        int i = Najblizszy(gracz, out int miejsce); if (i < 0 || stan == null) return false;
        var s = stan[i]; var t = stoly[i];
        if (s.faza == Decyzja) { Daj(t, s, true, s.gracz.Count, true); s.czas = Time.time + decyzja; Po(t, s); return true; }
        if (s.faza != Wolny) return true;
        if (!BraxChampionsKasyno.Wydaj(stawka)) { BraxChampionsGry.Pokaz("BRAK KREDYTU · KASA", 1.6f); return true; }
        if (s.wBucie > 234) Tasuj(s);                               // the cut card came out: a new shoe
        Sprzatnij(s); s.gracz.Clear(); s.krupier.Clear();
        s.miejsce = Mathf.Clamp(miejsce, 0, t.miejsca.Length - 1); s.faza = Rozdanie; s.krok = Time.time; s.czas = 0f;
        if (s.miejsce < t.zetony.Length && t.zetony[s.miejsce]) t.zetony[s.miejsce].gameObject.SetActive(true);
        // the deal as at the table: player, dealer (up), player, dealer (down)
        Daj(t, s, true, 0, true, 0f); Daj(t, s, false, 0, true, .45f); Daj(t, s, true, 1, true, .9f); Daj(t, s, false, 1, false, 1.35f);
        s.czas = Time.time + 1.9f;
        return true;
    }

    // a card from the shoe to the player's (gracz) or the dealer's row, up or down, after a delay
    void Daj(Stol t, Stan s, bool gracz, int n, bool odkryta, float opoznienie = 0f)
    {
        int c = Ciagnij(s); (gracz ? s.gracz : s.krupier).Add(c);
        var k = new Karta { c = c, t = Wez(c), start = Time.time + opoznienie };
        k.z = t.but + Vector3.up * .01f; k.rz = Quaternion.Euler(0f, 0f, 180f);                       // out of the shoe face down
        Vector3 cel;
        if (gracz)
        {
            var m = t.miejsca[s.miejsce]; var doKrupiera = new Vector3(-m.x, 0f, -m.z).normalized;
            var bok = Vector3.Cross(Vector3.up, doKrupiera);
            cel = m + doKrupiera * .11f + bok * (n * .022f) - doKrupiera * (n * .012f);              // fanned, each over the last
        }
        else cel = t.krupier + Vector3.right * (n * .075f);
        k.cel = new Vector3(cel.x, filcY + .0006f + n * .0004f, cel.z);
        // the card's top (the atlas' up, its local +z) to the dealer on the player's cards: upright for the player; on the
        // dealer's row to the dealer too (yaw 180): upright for the players across the felt
        k.yaw = gracz ? Mathf.Atan2(-t.miejsca[s.miejsce].x, -t.miejsca[s.miejsce].z) * Mathf.Rad2Deg : 180f;
        k.zakryta = !odkryta; k.rcel = Quaternion.Euler(0f, k.yaw, odkryta ? 0f : 180f);
        k.t.SetParent(t.stol, false); k.t.localPosition = k.z; k.t.localRotation = k.rz; k.t.gameObject.SetActive(opoznienie <= 0f);
        s.karty.Add(k);
    }
    // after a hit: 21 stands, over 21 is the end
    void Po(Stol t, Stan s)
    {
        int g = Suma(s.gracz, out _);
        if (g > 21) { Koniec(t, s, "PRZEBICIE · " + g); return; }
        if (g == 21) { s.faza = Krupier; s.krok = Time.time + .6f; }
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
                    if (Time.time >= s.czas) { s.faza = Krupier; s.krok = Time.time + .4f; }
                    break;
                case Krupier:
                    if (Time.time < s.krok) break;
                    if (Odkryj(s)) { s.krok = Time.time + .7f; break; }
                    if (KrupierDobiera(s.krupier)) { Daj(t, s, false, s.krupier.Count, true); s.krok = Time.time + .8f; break; }
                    Koniec(t, s, null); break;
                case Wynik:
                    if (Time.time >= s.czas) { Sprzatnij(s); s.faza = Wolny; }
                    break;
            }
            if (s.faza != Wolny && Gracz(t)) Panel(s);
        }
    }
    bool Gracz(Stol t)
    {
        var g = BraxChampionsGry.Gracz; if (!g) return false;
        var l = t.stol.InverseTransformPoint(g.transform.position); return l.z > -.5f && l.z < 2.4f && Mathf.Abs(l.x) < 2.4f;
    }
    void Panel(Stan s)
    {
        int g = Suma(s.gracz, out bool gm), widac = s.faza >= Krupier ? Suma(s.krupier, out _) : (s.krupier.Count > 0 ? Wartosc(s.krupier[0]) : 0);
        string txt = nazwa + " · TY " + (gm && g < 21 ? (g - 10) + "/" + g : g.ToString()) + " · KRUPIER " + widac;
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
        int wyplata = przebicie != null ? 0 : Wyplata(s.gracz, s.krupier, stawka);
        if (wyplata > 0) BraxChampionsKasyno.Wplac(wyplata);
        int g = Suma(s.gracz, out _), k = Suma(s.krupier, out _);
        string w = przebicie ?? (wyplata == stawka * 5 / 2 ? "BLACKJACK! · WYGRANA " + wyplata
                 : wyplata == stawka * 2 ? "WYGRANA " + wyplata + " · " + g + " NA " + k
                 : wyplata == stawka ? "REMIS · " + g
                 : Blackjack(s.krupier) ? "BLACKJACK KRUPIERA" : "KRUPIER WYGRYWA · " + k + " NA " + g);
        BraxChampionsGry.Pokaz(w, 3f);
        s.faza = Wynik; s.czas = Time.time + 3.2f;
        if (s.miejsce >= 0 && s.miejsce < t.zetony.Length && t.zetony[s.miejsce]) t.zetony[s.miejsce].gameObject.SetActive(false);
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
    void Siatki()
    {
        const int AW = BraxChampionsAutomatyGry.PokerGra.AtlasW, AH = BraxChampionsAutomatyGry.PokerGra.AtlasH, KW = BraxChampionsAutomatyGry.PokerGra.KartaW, KH = BraxChampionsAutomatyGry.PokerGra.KartaH;
        BraxChampionsAutomatyGry.PokerGra.RewersPx(out int rx, out int ry);
        siatki = new Mesh[52];
        for (int c = 0; c < 52; c++)
        {
            BraxChampionsAutomatyGry.PokerGra.KartaPx(c, out int px, out int py);
            float w = KartaSz * .5f, h = KartaDl * .5f, d = .0003f;
            Vector2 U(int x, int y) => new Vector2((float)x / AW, (float)y / AH);
            var m = new Mesh { name = "karta " + c };
            m.vertices = new[] { new Vector3(-w, d, -h), new Vector3(-w, d, h), new Vector3(w, d, h), new Vector3(w, d, -h),
                                 new Vector3(w, -d, -h), new Vector3(w, -d, h), new Vector3(-w, -d, h), new Vector3(-w, -d, -h) };
            m.uv = new[] { U(px, py), U(px, py + KH), U(px + KW, py + KH), U(px + KW, py),
                           U(rx, ry), U(rx, ry + KH), U(rx + KW, ry + KH), U(rx + KW, ry) };
            m.triangles = new[] { 0, 1, 2, 0, 2, 3, 4, 5, 6, 4, 6, 7 };
            m.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up, Vector3.down, Vector3.down, Vector3.down, Vector3.down };
            m.RecalculateBounds(); siatki[c] = m;
        }
    }
}
