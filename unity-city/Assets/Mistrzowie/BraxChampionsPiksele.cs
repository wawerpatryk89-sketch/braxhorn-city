using UnityEngine;

// Owner 10.11 21:22 ("monitorki mają być monitorkami, mają wyświetlać, punktację normalnie w kręgielni ... wszystko ma być
// funkcjonalne"): what the screens of E1 Centrum Rozrywki show, painted pixel by pixel (no fonts, no UI in the world):
//   - the lane scorecard (the lane's overhead screen and its settee console) - real ten-pin scoring,
//   - the status board (reception screen wall, the office monitors): every lane and table free / in play with its score,
//   - the ticket kiosk: free lanes and tables, the arcade attract screen, the air hockey score.
// Pure functions on a Color32 buffer: the runtime (BraxChampionsKregle, BraxChampionsE1Ekrany) repaints them live, the
// generator (BraxChampionsE1) paints the starting state into the screens' own textures.
public static class BraxChampionsPiksele
{
    // ---- 5 x 7 LED font (rows top to bottom, 5 bits, bit 4 = left column): A-Z, Polish letters, digits, signs
    const string Znaki = "ABCDEFGHIJKLMNOPQRSTUVWXYZĄĆĘŁŃÓŚŹŻ0123456789 :.,-/·!?+%()=<>*#";
    static readonly int[][] Wzory =
    {
        new[]{14,17,17,31,17,17,17}, new[]{30,17,17,30,17,17,30}, new[]{14,17,16,16,16,17,14}, new[]{28,18,17,17,17,18,28},
        new[]{31,16,16,30,16,16,31}, new[]{31,16,16,30,16,16,16}, new[]{14,17,16,23,17,17,15}, new[]{17,17,17,31,17,17,17},
        new[]{14,4,4,4,4,4,14},      new[]{7,2,2,2,2,18,12},      new[]{17,18,20,24,20,18,17}, new[]{16,16,16,16,16,16,31},
        new[]{17,27,21,21,17,17,17}, new[]{17,17,25,21,19,17,17}, new[]{14,17,17,17,17,17,14}, new[]{30,17,17,30,16,16,16},
        new[]{14,17,17,17,21,18,13}, new[]{30,17,17,30,20,18,17}, new[]{15,16,16,14,1,1,30},  new[]{31,4,4,4,4,4,4},
        new[]{17,17,17,17,17,17,14}, new[]{17,17,17,17,17,10,4},  new[]{17,17,17,21,21,21,10}, new[]{17,17,10,4,10,17,17},
        new[]{17,17,17,10,4,4,4},    new[]{31,1,2,4,8,16,31},
        new[]{14,17,17,31,17,17,19}, new[]{2,4,15,16,16,16,15},  new[]{31,16,30,16,31,4,6},  new[]{16,16,20,24,16,16,31},
        new[]{2,4,17,25,21,19,17},   new[]{2,4,14,17,17,17,14},  new[]{2,4,15,16,14,1,30},   new[]{2,4,31,2,4,8,31},
        new[]{4,0,31,2,4,8,31},
        new[]{14,17,19,21,25,17,14}, new[]{4,12,4,4,4,4,14},     new[]{14,17,1,2,4,8,31},    new[]{31,2,4,2,1,17,14},
        new[]{2,6,10,18,31,2,2},     new[]{31,16,30,1,1,17,14},  new[]{6,8,16,30,17,17,14},  new[]{31,1,2,4,8,8,8},
        new[]{14,17,17,14,17,17,14}, new[]{14,17,17,15,1,2,12},
        new[]{0,0,0,0,0,0,0},        new[]{0,12,12,0,12,12,0},   new[]{0,0,0,0,0,12,12},     new[]{0,0,0,0,12,4,8},
        new[]{0,0,0,31,0,0,0},       new[]{1,1,2,4,8,16,16},     new[]{0,0,4,14,4,0,0},      new[]{4,4,4,4,4,0,4},
        new[]{14,17,1,2,4,0,4},      new[]{0,4,4,31,4,4,0},      new[]{24,25,2,4,8,19,3},    new[]{2,4,8,8,8,4,2},
        new[]{8,4,2,2,2,4,8},        new[]{0,0,31,0,31,0,0},     new[]{2,4,8,16,8,4,2},      new[]{8,4,2,1,2,4,8},
        new[]{0,4,21,14,21,4,0},     new[]{10,10,31,10,31,10,10}
    };
    public static void Tekst(Color32[] px, int w, int h, string s, int x, int y, int skala, Color32 kolor)
    {
        foreach (char c in s.ToUpperInvariant())
        {
            int i = Znaki.IndexOf(c); if (i < 0) i = Znaki.IndexOf(' ');
            var wz = Wzory[i];
            for (int r = 0; r < 7; r++)
                for (int b = 0; b < 5; b++)
                    if ((wz[r] >> (4 - b) & 1) != 0)
                        for (int dy = 0; dy < skala; dy++)
                            for (int dx = 0; dx < skala; dx++)
                            {
                                int px0 = x + b * skala + dx, py0 = y + (6 - r) * skala + dy;   // texture rows go up
                                if (px0 >= 0 && px0 < w && py0 >= 0 && py0 < h) px[py0 * w + px0] = kolor;
                            }
            x += 6 * skala;
        }
    }
    public static int Szer(string s, int skala) => s.Length * 6 * skala - skala;
    public static void Srodek(Color32[] px, int w, int h, string s, int cx, int y, int skala, Color32 kolor) => Tekst(px, w, h, s, cx - Szer(s, skala) / 2, y, skala, kolor);
    public static void Prost(Color32[] px, int w, int h, int x0, int y0, int x1, int y1, Color32 c)
    {
        for (int y = Mathf.Max(0, y0); y < Mathf.Min(h, y1); y++) for (int x = Mathf.Max(0, x0); x < Mathf.Min(w, x1); x++) px[y * w + x] = c;
    }
    public static void Wypelnij(Color32[] px, Color32 c) { for (int i = 0; i < px.Length; i++) px[i] = c; }

    public static readonly Color32 Tlo = new Color32(6, 10, 28, 255), Siatka = new Color32(40, 86, 190, 255), Bialy = new Color32(240, 244, 255, 255),
        Zloty = new Color32(255, 72, 84, 255), Cyjan = new Color32(90, 220, 255, 255), Aktywny = new Color32(18, 30, 72, 255),
        Zielony = new Color32(70, 230, 120, 255), Czerwony = new Color32(255, 70, 80, 255), Fiolet = new Color32(190, 90, 255, 255);

    // ---------------- ten-pin scoring ----------------
    // frame totals (cumulative), -1 while a frame's bonus balls are not yet thrown
    public static int[] Ramki(int[] r, int n)
    {
        var sum = new int[10]; for (int f = 0; f < 10; f++) sum[f] = -1;
        int i = 0, total = 0;
        for (int f = 0; f < 10 && i < n; f++)
        {
            if (f < 9)
            {
                if (r[i] == 10) { if (i + 2 >= n) break; total += 10 + r[i + 1] + r[i + 2]; sum[f] = total; i += 1; }
                else
                {
                    if (i + 1 >= n) break;
                    if (r[i] + r[i + 1] == 10) { if (i + 2 >= n) break; total += 10 + r[i + 2]; }
                    else total += r[i] + r[i + 1];
                    sum[f] = total; i += 2;
                }
            }
            else
            {
                if (i + 1 >= n) break;
                bool bonus = r[i] == 10 || r[i] + r[i + 1] == 10;
                if (bonus) { if (i + 2 >= n) break; total += r[i] + r[i + 1] + r[i + 2]; }
                else total += r[i] + r[i + 1];
                sum[f] = total;
            }
        }
        return sum;
    }
    // where the game stands after n balls: frame (0..9), ball in the frame, whether the next ball gets a full rack, game over
    public static void Pozycja(int[] r, int n, out int ramka, out int wRamce, out bool komplet, out bool koniec)
    {
        int i = 0, f = 0; koniec = false;
        while (f < 9 && i < n)
        {
            if (r[i] == 10) { i++; f++; }
            else if (i + 1 < n) { i += 2; f++; }
            else { ramka = f; wRamce = 1; komplet = false; return; }
        }
        ramka = f;
        if (f < 9) { wRamce = 0; komplet = true; return; }
        int k = n - i; wRamce = k;
        if (k == 0) { komplet = true; return; }
        if (k == 1) { komplet = r[i] == 10; return; }
        if (k == 2)
        {
            bool bonus = r[i] == 10 || r[i] + r[i + 1] == 10;
            if (!bonus) { koniec = true; komplet = true; return; }
            komplet = r[i] == 10 ? r[i + 1] == 10 : true;     // X X -> full; X 7 -> the 3 left; 3 / -> full
            return;
        }
        koniec = true; komplet = true;
    }
    public static bool Koniec(int[] r, int n) { Pozycja(r, n, out _, out _, out _, out bool k); return k; }
    public static int Suma(int[] r, int n) { var s = Ramki(r, n); int t = 0; foreach (var v in s) if (v >= 0) t = v; return t; }
    // marks of a frame: X, /, -, digits (10th frame up to three)
    public static string ZnakiRamki(int[] r, int n, int ramka)
    {
        int i = 0, f = 0;
        while (f < ramka && i < n) { if (f < 9 && r[i] == 10) i++; else i += 2; f++; }
        if (i >= n) return "";
        string s = "";
        if (ramka < 9)
        {
            if (r[i] == 10) return "X";
            s += r[i] == 0 ? "-" : r[i].ToString();
            if (i + 1 < n) s += r[i] + r[i + 1] == 10 ? "/" : r[i + 1] == 0 ? "-" : r[i + 1].ToString();
            return s;
        }
        int prev = -1;
        for (int k = 0; k < 3 && i + k < n; k++)
        {
            int v = r[i + k];
            if (v == 10 && prev < 0) { s += "X"; continue; }
            if (prev >= 0 && prev + v == 10) { s += "/"; prev = -1; continue; }
            s += v == 0 ? "-" : v.ToString(); prev = prev >= 0 ? -1 : v;
        }
        return s;
    }

    // ---------------- screens ----------------
    // lane scorecard (400 x 128): lane number, total, ten frames with marks and running totals, the current frame lit
    public const int TorW = 400, TorH = 128;
    public static void TabelaToru(Color32[] px, int lane, int[] r, int n)
    {
        const int W = TorW, H = TorH;
        Wypelnij(px, Tlo);
        Pozycja(r, n, out int ramka, out _, out _, out bool koniec);
        var sumy = Ramki(r, n);
        Tekst(px, W, H, "TOR " + (lane + 1), 10, 106, 2, Bialy);
        string suma = n == 0 ? "WOLNY" : koniec ? "KONIEC " + Suma(r, n) : "SUMA " + Suma(r, n);
        Tekst(px, W, H, suma, W - 10 - Szer(suma, 2), 106, 2, n == 0 ? Zielony : Zloty);
        Prost(px, W, H, 6, 100, W - 6, 102, Siatka);
        for (int f = 0; f < 10; f++)
        {
            int x0 = 10 + f * 38, x1 = x0 + 38;
            if (n > 0 && !koniec && f == ramka) Prost(px, W, H, x0, 6, x1, 98, Aktywny);
            Prost(px, W, H, x0, 6, x0 + 1, 98, Siatka);
            Srodek(px, W, H, (f + 1).ToString(), x0 + 19, 88, 1, Siatka);
            string z = ZnakiRamki(r, n, f);
            int zx = x0 + 19 - Szer(z, 2) / 2;
            foreach (char c in z) { Tekst(px, W, H, c.ToString(), zx, 58, 2, c == 'X' ? Zloty : c == '/' ? Cyjan : Bialy); zx += 12; }
            Prost(px, W, H, x0 + 2, 50, x1 - 1, 51, Siatka);
            if (sumy[f] >= 0) Srodek(px, W, H, sumy[f].ToString(), x0 + 19, 20, 2, Bialy);
        }
        Prost(px, W, H, 390, 6, 391, 98, Siatka);
    }

    // status board (512 x 256): 24 lanes and 18 tables. lanes: -1 free, else the running total; done = finished game.
    // tables: -1 free, else balls potted
    public const int TablicaW = 512, TablicaH = 256;
    public static void Tablica(Color32[] px, int[] tory, bool[] skonczone, int[] stoly, string czas)
    {
        const int W = TablicaW, H = TablicaH;
        Wypelnij(px, Tlo);
        Tekst(px, W, H, "E1 CENTRUM ROZRYWKI", 10, 236, 2, Zloty);
        if (!string.IsNullOrEmpty(czas)) Tekst(px, W, H, czas, W - 10 - Szer(czas, 2), 236, 2, Bialy);
        Prost(px, W, H, 6, 230, W - 6, 232, Siatka);
        int wolneT = 0, wolneS = 0;
        foreach (var t in tory) if (t < 0) wolneT++;
        foreach (var s in stoly) if (s < 0) wolneS++;
        Tekst(px, W, H, "KRĘGLE · WOLNE " + wolneT + "/" + tory.Length, 10, 214, 1, Cyjan);
        for (int i = 0; i < tory.Length; i++)
        {
            int col = i / 12, row = i % 12, x = 10 + col * 128, y = 198 - row * 16;
            string st = tory[i] < 0 ? "WOLNY" : (skonczone != null && i < skonczone.Length && skonczone[i] ? "KONIEC " : "GRA ") + tory[i];
            Tekst(px, W, H, "TOR " + (i + 1).ToString().PadLeft(2), x, y, 1, Bialy);
            Tekst(px, W, H, st, x + 48, y, 1, tory[i] < 0 ? Zielony : Zloty);
        }
        Prost(px, W, H, 266, 8, 268, 224, Siatka);
        Tekst(px, W, H, "BILARD · WOLNE " + wolneS + "/" + stoly.Length, 278, 214, 1, Cyjan);
        for (int i = 0; i < stoly.Length; i++)
        {
            int col = i / 9, row = i % 9, x = 278 + col * 118, y = 198 - row * 16;
            string st = stoly[i] < 0 ? "WOLNY" : "GRA " + stoly[i] + "/15";
            Tekst(px, W, H, "STÓŁ " + (i + 1).ToString().PadLeft(2), x, y, 1, Bialy);
            Tekst(px, W, H, st, x + 54, y, 1, stoly[i] < 0 ? Zielony : Zloty);
        }
        Tekst(px, W, H, "WEŹ KULĘ PRZY TORZE · PODEJDŹ DO STOŁU", 278, 40, 1, Siatka);
        Tekst(px, W, H, "GRA OD RAZU · BEZ KOLEJKI", 278, 24, 1, Siatka);
    }

    // ticket kiosk (256 x 320, portrait): free lanes and tables, how to start
    public const int KioskW = 256, KioskH = 320;
    public static void Kiosk(Color32[] px, int[] tory, int[] stoly)
    {
        const int W = KioskW, H = KioskH;
        Wypelnij(px, Tlo);
        Srodek(px, W, H, "E1", W / 2, 268, 5, Zloty);
        Srodek(px, W, H, "CENTRUM ROZRYWKI", W / 2, 246, 1, Bialy);
        Prost(px, W, H, 12, 236, W - 12, 238, Siatka);
        int wolneT = 0, wolneS = 0;
        foreach (var t in tory) if (t < 0) wolneT++;
        foreach (var s in stoly) if (s < 0) wolneS++;
        Srodek(px, W, H, "KRĘGLE", W / 2, 206, 2, Cyjan);
        Srodek(px, W, H, "WOLNE TORY " + wolneT, W / 2, 184, 2, wolneT > 0 ? Zielony : Czerwony);
        Srodek(px, W, H, "BILARD", W / 2, 148, 2, Cyjan);
        Srodek(px, W, H, "WOLNE STOŁY " + wolneS, W / 2, 126, 2, wolneS > 0 ? Zielony : Czerwony);
        Prost(px, W, H, 12, 108, W - 12, 110, Siatka);
        string pierwszy = ""; for (int i = 0; i < tory.Length; i++) if (tory[i] < 0) { pierwszy = "TOR " + (i + 1); break; }
        Srodek(px, W, H, pierwszy.Length > 0 ? "ZAPRASZAMY · " + pierwszy : "WSZYSTKIE TORY ZAJĘTE", W / 2, 84, 1, Bialy);
        Srodek(px, W, H, "KULA NA ZWROTNICY TORU", W / 2, 64, 1, Siatka);
        Srodek(px, W, H, "KIJE NA ŚCIANIE PRZY STOŁACH", W / 2, 48, 1, Siatka);
    }

    // arcade attract screen (160 x 120): the cabinet's title, a marching invader row, blinking START
    public const int AutomatW = 160, AutomatH = 120;
    static readonly string[] Tytuly = { "GALAXY E1", "NEON RACE", "BOX CHAMP", "PIXEL KONG", "STAR DEFEND" };
    static readonly int[] Najezdzca = { 0x24, 0x7E, 0xDB, 0xFF, 0xA5, 0x24 };
    public static void Automat(Color32[] px, int wariant, int klatka)
    {
        const int W = AutomatW, H = AutomatH;
        Wypelnij(px, new Color32(2, 2, 8, 255));
        for (int i = 0; i < 40; i++) { int sx = (i * 37 + klatka * (1 + i % 3)) % W, sy = (i * 53) % H; px[sy * W + sx] = new Color32(120, 120, 160, 255); }
        var kol = wariant % 3 == 0 ? Fiolet : wariant % 3 == 1 ? Cyjan : Zloty;
        Srodek(px, W, H, Tytuly[wariant % Tytuly.Length], W / 2, 92, 2, kol);
        int off = (klatka / 4) % 16 - 8;
        for (int k = 0; k < 5; k++)
            for (int r = 0; r < Najezdzca.Length; r++)
                for (int b = 0; b < 8; b++)
                    if ((Najezdzca[r] >> (7 - b) & 1) != 0)
                        Prost(px, W, H, 20 + k * 26 + off + b * 2, 70 - r * 2, 22 + k * 26 + off + b * 2, 72 - r * 2, (klatka / 8 + k) % 2 == 0 ? Zielony : Fiolet);
        Prost(px, W, H, W / 2 - 8 + off, 18, W / 2 + 8 + off, 22, Cyjan);
        Prost(px, W, H, W / 2 - 2 + off, 22, W / 2 + 2 + off, 26, Cyjan);
        if ((klatka / 10) % 2 == 0) Srodek(px, W, H, "WCIŚNIJ START", W / 2, 34, 1, Bialy);
        Tekst(px, W, H, "HI 25000", 4, 110, 1, Zloty);
    }

    // air hockey score (128 x 48)
    public const int CymbW = 128, CymbH = 48;
    public static void Cymbergaj(Color32[] px, int a, int b)
    {
        Wypelnij(px, Tlo);
        Srodek(px, CymbW, CymbH, a + " : " + b, CymbW / 2, 14, 3, Czerwony);
    }
}
