using System;
using System.Collections.Generic;
using UnityEngine;
using Z = BraxChampionsAutomatyZasady;

// MIASTO MISTRZÓW - the casinos' machine games besides the two reel slots (owner 16.11 20:28: "musiałbyś zrobić różne gry w
// automatach"), pure rules and the screens' live layers, shared by the game (BraxChampionsAutomaty) and the generator
// (BraxChampionsKasynoSprzet). Five games now stand in the banks and on the carousels:
//  0 BRAXHORN CLASSIC - the three-reel stepper (BraxChampionsAutomatyZasady), on the linked jackpot;
//  1 BRAXHORN VIDEO   - the five-reel video slot (BraxChampionsAutomatyZasady), on the linked jackpot;
//  2 VIDEO POKER      - "Jacks or Better" 9/6, the full-pay table of the real machines (99.54 % with the best play). With one
//                       button the machine plays its AUTO HOLD (as the real ones offer): the cards of the simple strategy
//                       (Wizard of Odds, 16 rules, 99.46 %); AKCJA deals, AKCJA again draws;
//  3 KENO             - 6 numbers of 80, twenty balls drawn (QUICK PICK each game); our table returns 92.0 % (computed
//                       below; the Las Vegas video keno averages 90-91 %);
//  4 KOŁO MISTRZÓW    - the three-reel stepper with the wheel on top (as the wheel carousels of the real casinos): the KOŁO
//                       symbol on the third reel's line spins the wheel - 22 segments, 20 to 500 credits; three SEVENS pay a
//                       fixed 250 x (the wheel machine is not on the linked jackpot). 65.7 % base + 29.8 % the wheel = 95.5 %.
public static class BraxChampionsAutomatyGry
{
    public const int Mech = 0, Video = 1, Poker = 2, Keno = 3, Kolo = 4, Ile = 5;
    public static readonly string[] Nazwy = { "BRAXHORN CLASSIC", "BRAXHORN VIDEO", "VIDEO POKER", "KENO", "KOŁO MISTRZÓW" };
    // the cabinet: the screen upright (video, poker, keno) or the stepper (classic, koło)
    public static bool Ekranowa(int g) => g == Video || g == Poker || g == Keno;
    // a reel's strip for a game (the wheel machine's third reel carries the KOŁO symbol)
    public static int[] Pasek(int gra, int reel) => gra == Kolo && reel == 2 ? KoloGra.Pasek3 : Z.Pasek(reel, gra == Video);

    // ================================================================ a screen's live layer: quads cut from one atlas
    public sealed class Warstwa
    {
        readonly float aw, ah;
        readonly List<Vector3> v = new List<Vector3>(); readonly List<Vector2> uv = new List<Vector2>(); readonly List<int> tr = new List<int>();
        readonly List<int> tlo = new List<int>();
        public Warstwa(int atlasW, int atlasH) { aw = atlasW; ah = atlasH; }
        public void Czysc() { v.Clear(); uv.Clear(); tr.Clear(); tlo.Clear(); }
        public int Ile => v.Count / 4;
        // cx / cy in the viewer's frame (x to his right, y up), the quad facing him (+z), z toward him; the atlas rectangle in
        // pixels (x from the left, y from the bottom)
        public void Q(float cx, float cy, float w, float h, int px, int py, int pw, int ph, float z = 0f) => Dodaj(tr, cx, cy, w, h, px, py, pw, ph, z);
        // 23.11 (owner's photo of the LOTOTO board: balls cut in half and missing): the screen's background goes to its own
        // submesh, drawn first by a material that does not write depth, and everything else lies in the SAME plane over it.
        // Two layers a millimetre apart fight over depth on the phone in vertical bands; a background that writes no depth
        // cannot win that fight at any distance or depth-buffer precision.
        public void Tlo(float cx, float cy, float w, float h, int px, int py, int pw, int ph) => Dodaj(tlo, cx, cy, w, h, px, py, pw, ph, 0f);
        void Dodaj(List<int> t, float cx, float cy, float w, float h, int px, int py, int pw, int ph, float z)
        {
            int i = v.Count; float u0 = (px + .5f) / aw, u1 = (px + pw - .5f) / aw, v0 = (py + .5f) / ah, v1 = (py + ph - .5f) / ah;
            // the viewer's right is the screen's -x
            v.Add(new Vector3(-(cx - w * .5f), cy - h * .5f, z)); v.Add(new Vector3(-(cx + w * .5f), cy - h * .5f, z));
            v.Add(new Vector3(-(cx + w * .5f), cy + h * .5f, z)); v.Add(new Vector3(-(cx - w * .5f), cy + h * .5f, z));
            uv.Add(new Vector2(u0, v0)); uv.Add(new Vector2(u1, v0)); uv.Add(new Vector2(u1, v1)); uv.Add(new Vector2(u0, v1));
            t.Add(i); t.Add(i + 2); t.Add(i + 1); t.Add(i); t.Add(i + 3); t.Add(i + 2);
        }
        // with a background: submesh 0 = the background (its renderer's first material), submesh 1 = the rest
        public void Zapisz(Mesh m)
        {
            m.Clear(); m.SetVertices(v); m.SetUVs(0, uv);
            if (tlo.Count > 0) { m.subMeshCount = 2; m.SetTriangles(tlo, 0); m.SetTriangles(tr, 1); }
            else m.SetTriangles(tr, 0);
            var nr = new Vector3[v.Count]; for (int i = 0; i < nr.Length; i++) nr[i] = Vector3.forward; m.normals = nr; m.RecalculateBounds();
        }
    }

    // ================================================================ VIDEO POKER (Jacks or Better 9/6)
    public static class PokerGra
    {
        // a card 0..51: rank c % 13 (0 the two ... 8 the ten, 9 J, 10 Q, 11 K, 12 A), suit c / 13 (0 spades, 1 hearts,
        // 2 diamonds, 3 clubs)
        public const int Dziesiatka = 8, Walet = 9, As = 12;
        public static int Ranga(int c) => c % 13;
        public static int Kolor(int c) => c / 13;
        public const int Nic = 0, ParaOdWaleta = 1, DwiePary = 2, Trojka = 3, Strit = 4, KolorU = 5, Full = 6, Kareta = 7, PokerU = 8, Krolewski = 9;
        public static readonly string[] Uklady = { "", "PARA OD WALETA", "DWIE PARY", "TRÓJKA", "STRIT", "KOLOR", "FULL", "KARETA", "POKER", "POKER KRÓLEWSKI" };
        // 9/6 "for one" at the max bet (the stake back included): royal 800, straight flush 50, quads 25, full house 9,
        // flush 6, straight 4, trips 3, two pair 2, jacks or better 1
        public static readonly int[] Wyplata = { 0, 1, 2, 3, 4, 6, 9, 25, 50, 800 };

        public static int Ocen(int[] k)
        {
            var n = new int[13]; bool kolor = true;
            for (int i = 0; i < 5; i++) { n[Ranga(k[i])]++; if (Kolor(k[i]) != Kolor(k[0])) kolor = false; }
            int pary = 0, trojki = 0, karety = 0, para = -1;
            for (int r = 0; r < 13; r++) { if (n[r] == 4) karety++; else if (n[r] == 3) trojki++; else if (n[r] == 2) { pary++; para = r; } }
            bool strit = false; int wys = -1;
            if (pary == 0 && trojki == 0 && karety == 0)
            {
                int lo = 13, hi = -1; for (int r = 0; r < 13; r++) if (n[r] > 0) { lo = Mathf.Min(lo, r); hi = Mathf.Max(hi, r); }
                if (hi - lo == 4) { strit = true; wys = hi; }
                else if (n[As] == 1 && n[0] == 1 && n[1] == 1 && n[2] == 1 && n[3] == 1) { strit = true; wys = 3; }   // A-2-3-4-5
            }
            if (strit && kolor) return wys == As ? Krolewski : PokerU;
            if (karety > 0) return Kareta;
            if (trojki > 0 && pary > 0) return Full;
            if (kolor) return KolorU;
            if (strit) return Strit;
            if (trojki > 0) return Trojka;
            if (pary == 2) return DwiePary;
            if (pary == 1 && para >= Walet) return ParaOdWaleta;
            return Nic;
        }

        // AUTO HOLD: the cards the simple strategy keeps (a bit per card), its rules in their order
        public static int Trzymaj(int[] k)
        {
            int u = Ocen(k), m;
            if (u == Krolewski || u == PokerU || u == Kareta) return 31;                       // 1 the made hands
            if ((m = DoKrolewskiego(k, 4)) != 0) return m;                                     // 2 four to a royal
            if (u == Full || u == KolorU || u == Strit) return 31;                             // 3 full house, flush, straight
            if (u == Trojka) return Takie(k, 3);                                               //   three of a kind
            if ((m = DoPokera(k, 4)) != 0) return m;                                           // 4 four to a straight flush
            if (u == DwiePary) return Takie(k, 2);                                             // 5 two pair
            if (u == ParaOdWaleta) return Takie(k, 2);                                         // 6 a high pair
            if ((m = DoKrolewskiego(k, 3)) != 0) return m;                                     // 7 three to a royal
            if ((m = DoKoloru(k)) != 0) return m;                                              // 8 four to a flush
            if ((m = Takie(k, 2)) != 0) return m;                                              // 9 a low pair
            if ((m = Otwarty(k)) != 0) return m;                                               // 10 four to an outside straight
            if ((m = Wysokie(k, true)) != 0) return m;                                         // 11 two suited high cards
            if ((m = DoPokera(k, 3)) != 0) return m;                                           // 12 three to a straight flush
            if ((m = Wysokie(k, false)) != 0) return m;                                        // 13 two unsuited high cards (the lowest two)
            if ((m = Dziesiatka10(k)) != 0) return m;                                          // 14 suited 10 / J, 10 / Q, 10 / K
            for (int i = 0; i < 5; i++) if (Ranga(k[i]) >= Walet) return 1 << i;               // 15 one high card
            return 0;                                                                          // 16 draw five
        }
        static int Takie(int[] k, int ile)
        {
            int m = 0; var n = new int[13]; for (int i = 0; i < 5; i++) n[Ranga(k[i])]++;
            for (int i = 0; i < 5; i++) if (n[Ranga(k[i])] == ile) m |= 1 << i;
            return m;
        }
        static int Bity(int m) { int c = 0; while (m != 0) { c += m & 1; m >>= 1; } return c; }
        static int DoKrolewskiego(int[] k, int ile)
        {
            for (int s = 0; s < 4; s++)
            {
                int m = 0; for (int i = 0; i < 5; i++) if (Kolor(k[i]) == s && Ranga(k[i]) >= Dziesiatka) m |= 1 << i;
                if (Bity(m) == ile) return m;
            }
            return 0;
        }
        // ile cards of one suit within five ranks in a row (the ace high or low)
        static int DoPokera(int[] k, int ile)
        {
            for (int s = 0; s < 4; s++)
                for (int lo = -1; lo <= 8; lo++)
                {
                    int m = 0;
                    for (int i = 0; i < 5; i++)
                    {
                        if (Kolor(k[i]) != s) continue; int r = Ranga(k[i]); if (lo < 0 && r == As) r = -1;
                        if (r >= lo && r <= lo + 4) m |= 1 << i;
                    }
                    if (Bity(m) == ile) return m;
                }
            return 0;
        }
        static int DoKoloru(int[] k)
        {
            for (int s = 0; s < 4; s++) { int m = 0; for (int i = 0; i < 5; i++) if (Kolor(k[i]) == s) m |= 1 << i; if (Bity(m) == 4) return m; }
            return 0;
        }
        // four ranks in a row open at both ends (2-3-4-5 up to 10-J-Q-K; with the ace it is inside)
        static int Otwarty(int[] k)
        {
            for (int lo = 0; lo + 3 <= 11; lo++)
            {
                int m = 0;
                for (int r = lo; r <= lo + 3; r++)
                {
                    int jest = -1; for (int i = 0; i < 5; i++) if (Ranga(k[i]) == r) { jest = i; break; }
                    if (jest < 0) { m = 0; break; }
                    m |= 1 << jest;
                }
                if (m != 0) return m;
            }
            return 0;
        }
        // two high cards (J, Q, K, A): suited ones, or any two - with more than two the lowest two
        static int Wysokie(int[] k, bool wKolorze)
        {
            if (wKolorze)
            {
                for (int s = 0; s < 4; s++)
                {
                    int m = 0; for (int i = 0; i < 5; i++) if (Kolor(k[i]) == s && Ranga(k[i]) >= Walet) m |= 1 << i;
                    if (Bity(m) == 2) return m;
                }
                return 0;
            }
            int a = -1, b = -1;
            for (int i = 0; i < 5; i++)
            {
                if (Ranga(k[i]) < Walet) continue;
                if (a < 0 || Ranga(k[i]) < Ranga(k[a])) { b = a; a = i; }
                else if (b < 0 || Ranga(k[i]) < Ranga(k[b])) b = i;
            }
            return a >= 0 && b >= 0 ? (1 << a) | (1 << b) : 0;
        }
        static int Dziesiatka10(int[] k)
        {
            for (int i = 0; i < 5; i++)
            {
                if (Ranga(k[i]) != Dziesiatka) continue;
                for (int j = 0; j < 5; j++) if (j != i && Kolor(k[j]) == Kolor(k[i]) && Ranga(k[j]) >= Walet && Ranga(k[j]) < As) return (1 << i) | (1 << j);
            }
            return 0;
        }
        // a fresh shuffled deck (Fisher-Yates)
        public static void Tasuj(System.Random los, int[] talia)
        {
            for (int i = 0; i < 52; i++) talia[i] = i;
            for (int i = 51; i > 0; i--) { int j = los.Next(i + 1); int t = talia[i]; talia[i] = talia[j]; talia[j] = t; }
        }

        // ---- the screen: the atlas (2048 x 1024) and the layout on the main screen (.598 x .336 m)
        public const int AtlasW = 2048, AtlasH = 1024, KartaW = 144, KartaH = 200, TrzymajW = 160, TrzymajH = 48, NapisW = 512, NapisH = 56;
        // the 52 faces 13 a row (a suit a row from the top), the back and TRZYMAJ in the 14th column, the hands' names at the bottom
        public static void KartaPx(int c, out int x, out int y) { x = Ranga(c) * KartaW; y = AtlasH - (Kolor(c) + 1) * KartaH; }
        public static void RewersPx(out int x, out int y) { x = 13 * KartaW; y = AtlasH - KartaH; }
        public static void TrzymajPx(out int x, out int y) { x = 13 * KartaW; y = AtlasH - KartaH - 8 - TrzymajH; }
        public static void NapisPx(int u, out int x, out int y) { int i = u - 1; x = i % 4 * NapisW; y = 4 * NapisH - (i / 4 + 1) * NapisH; }
        public const float KartaSz = .098f, KartaWys = .136f, Rozstaw = .110f, KartyY = .004f, TrzymajY = .092f, TrzymajSz = .090f, TrzymajWys = .027f,
            NapisY = -.094f, NapisSz = .30f, NapisWys = .0328f;
        // the five cards (face or back), TRZYMAJ over the held ones, the hand's name under them
        public static void Zbuduj(Warstwa w, Mesh m, int[] karty, bool[] odkryte, int trzymaj, int uklad)
        {
            w.Czysc();
            for (int i = 0; i < 5; i++)
            {
                float x = (i - 2) * Rozstaw; int px, py;
                if (odkryte[i]) KartaPx(karty[i], out px, out py); else RewersPx(out px, out py);
                w.Q(x, KartyY, KartaSz, KartaWys, px, py, KartaW, KartaH);
                if ((trzymaj >> i & 1) != 0) { TrzymajPx(out px, out py); w.Q(x, TrzymajY, TrzymajSz, TrzymajWys, px, py, TrzymajW, TrzymajH); }
            }
            if (uklad > 0 && uklad < Uklady.Length) { NapisPx(uklad, out int nx, out int ny); w.Q(0f, NapisY, NapisSz, NapisWys, nx, ny, NapisW, NapisH); }
            w.Zapisz(m);
        }
    }

    // ================================================================ KENO (6 of 80)
    public static class KenoGra
    {
        public const int Pola = 80, Kule = 20, Typy = 6;
        // for one, by the numbers caught of six: 3 the stake back, 4 x 6, 5 x 100, 6 x 2400 - 92.0 % (the hypergeometric odds
        // of 20 of 80: .12982, .02854, .003096, .000129)
        public static readonly int[] Wyplata = { 0, 0, 0, 1, 6, 100, 2400 };
        // different numbers 1..80 (a partial shuffle)
        public static void Losuj(System.Random los, int[] wynik)
        {
            var p = new int[Pola]; for (int i = 0; i < Pola; i++) p[i] = i + 1;
            for (int i = 0; i < wynik.Length; i++) { int j = i + los.Next(Pola - i); int t = p[i]; p[i] = p[j]; p[j] = t; wynik[i] = p[i]; }
        }
        public static int Trafione(int[] typy, int[] kule, int ile)
        {
            int t = 0; for (int k = 0; k < ile && k < kule.Length; k++) if (Array.IndexOf(typy, kule[k]) >= 0) t++;
            return t;
        }
        // ---- the screen: the atlas (2048 x 1024): the 80 numbers in three states (picked gold, drawn white ball, caught red
        // gold) in three blocks of 10 x 8 cells, the digits under them
        public const int AtlasW = 2048, AtlasH = 1024, PoleW = 64, PoleH = 42, Blok = 680, CyfraW = 48, CyfraH = 64;
        public const int Typowany = 0, Wylosowany = 1, Trafiony = 2;
        public static void PolePx(int n, int stan, out int x, out int y) { int i = n - 1; x = stan * Blok + i % 10 * PoleW; y = AtlasH - (i / 10 + 1) * PoleH; }
        public static void CyfraPx(int d, out int x, out int y) { x = d * CyfraW; y = AtlasH - 8 * PoleH - 24 - CyfraH; }
        // the board: 10 x 8 cells left of the middle, the counters on the right
        public const float PoleSz = .046f, PoleWys = .030f, SiatkaX = -.045f, SiatkaY = .010f, PanelX = .238f, TrafY = .040f, KuleY = -.060f, CyfraSz = .026f, CyfraWys = .035f;
        public static float PoleX(int n) => SiatkaX + ((n - 1) % 10 - 4.5f) * PoleSz;
        public static float PoleY(int n) => SiatkaY + (3.5f - (n - 1) / 10) * PoleWys;
        public static void Zbuduj(Warstwa w, Mesh m, int[] typy, int[] kule, int ile)
        {
            w.Czysc(); int px, py;
            for (int k = 0; k < ile && k < kule.Length; k++)
            {
                int n = kule[k]; PolePx(n, Array.IndexOf(typy, n) >= 0 ? Trafiony : Wylosowany, out px, out py);
                w.Q(PoleX(n), PoleY(n), PoleSz, PoleWys, px, py, PoleW, PoleH);
            }
            foreach (int t in typy)
            {
                bool wylosowany = false; for (int k = 0; k < ile && k < kule.Length; k++) if (kule[k] == t) wylosowany = true;
                if (wylosowany) continue;
                PolePx(t, Typowany, out px, out py); w.Q(PoleX(t), PoleY(t), PoleSz, PoleWys, px, py, PoleW, PoleH);
            }
            Liczba(w, Trafione(typy, kule, ile), PanelX, TrafY); Liczba(w, Mathf.Min(ile, Kule), PanelX, KuleY);
            w.Zapisz(m);
        }
        static void Liczba(Warstwa w, int v, float cx, float cy)
        {
            string t = v.ToString(); float x = cx - (t.Length - 1) * CyfraSz * .5f;
            foreach (char c in t) { CyfraPx(c - '0', out int px, out int py); w.Q(x, cy, CyfraSz, CyfraWys, px, py, CyfraW, CyfraH); x += CyfraSz; }
        }
    }

    // ================================================================ KOŁO MISTRZÓW (the wheel on the stepper)
    public static class KoloGra
    {
        public const int Symbol = 7;                                     // the atlas' eighth cell
        // the mechanical third reel with the grapes at stop 11 turned into KOŁO (1 stop of 20: the wheel every 20 spins)
        public static readonly int[] Pasek3 = { 4, 6, 5, 3, 4, 1, 6, 5, 0, 6, 4, 7, 3, 6, 1, 5, 4, 6, 3, 5 };
        // the wheel's 22 segments clockwise from the top, the credits at the stake of 10 (an average of 59.5: 5.95 x the
        // stake once every 20 spins = 29.8 %)
        public static readonly int[] Segmenty = { 500, 20, 50, 20, 30, 80, 20, 40, 20, 30, 150, 20, 40, 20, 30, 60, 20, 50, 20, 30, 40, 20 };
        public const int Siodemki = 250;                                  // three SEVENS: 250 x the stake, fixed
        public static int Wygrana(int seg, int stawka) => Segmenty[seg] * stawka / 10;
        // the line pay (x the stake): the stepper's table, three SEVENS fixed; KOŁO is no line symbol
        public static int Linia(int a, int b, int c) => a == 0 && b == 0 && c == 0 ? Siodemki : Mathf.Max(0, Z.Mechaniczny(a, b, c));
        public static float Kat => 360f / Segmenty.Length;
        // the wheel turned clockwise (seen from the front) by this brings segment s under the pointer at the top
        public static float KatSegmentu(int s) => -s * Kat;
    }
}
