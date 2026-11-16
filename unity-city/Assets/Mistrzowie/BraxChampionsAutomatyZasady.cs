using UnityEngine;

// MIASTO MISTRZÓW - the rules of the casinos' slot machines (owner 16.11 18:19-18:22: "te maszyny mają funkcjonować"), one
// place for the game (BraxChampionsAutomaty) and the generator (BraxChampionsKasynoSprzet: the drums, the pay tables, the
// atlases). Pure data and arithmetic, no scene.
//  - the symbols are the owner's reel strips (16.11 19:13, six strips, one per machine theme): 0 SEVEN (the jackpot),
//    1 DIAMOND, 2 STAR (the joker), 3 BELL, 4 WATERMELON, 5 GRAPES, 6 CHERRIES - the seven every strip of his carries;
//  - MECHANICAL (a three-reel stepper, symbol after symbol as the European fruit reels): 20 stops a reel, one pay line (the
//    centre); the STAR only on the middle reel, standing for any symbol but the SEVEN;
//  - SCREEN (a five-reel video slot): 20 symbols a reel, three rows, five lines, 3 / 4 / 5 of a kind from the left, the STAR
//    standing for any symbol but the SEVEN.
// The pay tables were worked out on the full cycle (8 000 / 3 200 000 combinations, 16.11): the mechanical one returns
// 64.4 % in the base game + about 31 % through the progressive jackpot (three SEVENS, 1 in 8 000 spins), a win on 28.5 %
// of the spins; the screen one 93.9 % - as real machines (85-97 %).
public static class BraxChampionsAutomatyZasady
{
    public const int Symbole = 7, Stopy = 20;
    public static readonly string[] Nazwy = { "SIÓDEMKA", "DIAMENT", "GWIAZDA", "DZWONEK", "ARBUZ", "WINOGRONA", "WIŚNIE" };

    // ---- the strips (no two equal symbols side by side)
    public static readonly int[][] PaskiMech =
    {
        new[]{6,5,4,3,6,5,1,4,6,5,0,4,6,3,5,1,4,6,5,3},
        new[]{5,6,3,4,5,2,6,1,4,5,3,6,0,4,5,1,3,6,5,4},
        new[]{4,6,5,3,4,1,6,5,0,6,4,5,3,6,1,5,4,6,3,5},
    };
    public static readonly int[][] PaskiEkran =
    {
        new[]{6,4,5,3,6,1,4,2,6,5,3,4,0,6,5,1,4,6,3,5},
        new[]{5,6,3,4,1,6,5,0,4,6,3,5,2,4,6,1,5,3,6,4},
        new[]{4,5,6,1,3,6,4,5,2,6,0,3,4,6,5,1,6,4,3,5},
    };
    static readonly int[] EkranBeben = { 0, 1, 2, 1, 0 };
    public static int[] Pasek(int reel, bool ekran) => ekran ? PaskiEkran[EkranBeben[reel]] : PaskiMech[reel];
    public static int Symbol(int[] pasek, int stop) => pasek[((stop % Stopy) + Stopy) % Stopy];

    // ---- the atlas of a theme (BraxChampionsKasynoSprzet.Atlas): 2 columns x 4 rows of 2:1 cells (512 x 256 px of 1024 x
    // 1024), each the owner's strip round one symbol (his band from the middle between it and the symbol above to the middle
    // to the one below, his side frame included). The whole cell is a drum stop (as wide as two stops are tall); the
    // middle .6 of its width, squeezed to a square, a screen cell. Symbol s at column s % 2, row s / 2 from the top.
    public const int AtlasW = 1024, AtlasH = 1024, KomorkaW = 512, KomorkaH = 256, Kolumny = 2, Wiersze = 4;
    public const float Kwadrat = .6f;
    public static void Komorka(int s, bool kwadrat, out float u0, out float v0, out float u1, out float v1)
    {
        int c = s % Kolumny, r = s / Kolumny; const float mu = 3f / AtlasW, mv = 3f / AtlasH;
        u0 = (float)c / Kolumny + mu; u1 = (float)(c + 1) / Kolumny - mu;
        v1 = 1f - (float)r / Wiersze - mv; v0 = 1f - (float)(r + 1) / Wiersze + mv;
        if (kwadrat) { float m = (u0 + u1) * .5f, h = Kwadrat * .5f / Kolumny; u0 = m - h; u1 = m + h; }
    }
    // the cell's pixel rectangle (x from the left, y from the BOTTOM - texture rows go up)
    public static void KomorkaPx(int s, out int x0, out int y0)
    {
        x0 = s % Kolumny * KomorkaW; y0 = AtlasH - (s / Kolumny + 1) * KomorkaH;
    }

    // ---- MECHANICAL pay (x the stake), -1 = the jackpot
    public const int Wisnie2 = 2, Wisnia1 = 1;
    public static readonly int[] Trzy = { -1, 50, 0, 15, 8, 5, 3 };      // three of a symbol on the line (the STAR counts)
    public static int Mechaniczny(int a, int b, int c)
    {
        if (a == 0 && b == 0 && c == 0) return -1;
        for (int k = 1; k < Symbole; k++)
        {
            if (k == 2) continue;
            if ((a == k || a == 2) && (b == k || b == 2) && (c == k || c == 2) && (a == k || b == k || c == k)) return Trzy[k];
        }
        if (a == 6 && (b == 6 || b == 2)) return Wisnie2;
        if (a == 6) return Wisnia1;
        return 0;
    }

    // ---- SCREEN pay: the lines (row 0 top, 1 middle, 2 bottom per reel) and 3 / 4 / 5 of a kind (x the stake / 5 lines)
    public static readonly int[][] Linie = { new[]{1,1,1,1,1}, new[]{0,0,0,0,0}, new[]{2,2,2,2,2}, new[]{0,1,2,1,0}, new[]{2,1,0,1,2} };
    public static readonly int[,] Ekranowy = {
        {100,500,5000},{25,100,400},{20,75,250},{10,30,100},{8,25,80},{6,15,50},{4,12,40} };
    // okno[reel, row]; returns the sum of the lines' multipliers; linie = a bit per winning line; jackpot = five SEVENS
    public static int Ekran(int[,] okno, out int linie, out bool jackpot)
    {
        int suma = 0; linie = 0; jackpot = false;
        for (int li = 0; li < Linie.Length; li++)
        {
            var l = Linie[li]; int pierwszy = -1, n = 0;
            if (okno[0, l[0]] == 0)
            {
                // the SEVEN pays only as itself from the first reel (the STAR never stands for it)
                pierwszy = 0; n = 1; while (n < 5 && okno[n, l[n]] == 0) n++;
            }
            else
                for (int r = 0; r < 5; r++)
                {
                    int s = okno[r, l[r]];
                    if (s == 0) break;
                    if (s == 2) { n++; continue; }
                    if (pierwszy < 0) { pierwszy = s; n++; continue; }
                    if (s == pierwszy) n++; else break;
                }
            if (pierwszy < 0) pierwszy = 2;
            if (n >= 3) { suma += Ekranowy[pierwszy, n - 3]; linie |= 1 << li; if (pierwszy == 0 && n == 5) jackpot = true; }
        }
        return suma;
    }
    // the screen window round the stops: okno[r, 0] the top row (the stop above), 1 the middle, 2 the bottom
    public static int[,] Okno(int[] stopy)
    {
        var o = new int[5, 3];
        for (int r = 0; r < 5; r++) { var p = Pasek(r, true); o[r, 0] = Symbol(p, stopy[r] + 1); o[r, 1] = Symbol(p, stopy[r]); o[r, 2] = Symbol(p, stopy[r] - 1); }
        return o;
    }

    // ---- a screen reel: 4 cells round the scroll position p (in stops), clipped to the window of 3 (the cells above and
    // below are cut off as on the real screen). Local x across the reel (the player sees +x on his LEFT: the machine faces
    // +z), y = 0 the middle row, h = one cell (square). 16 vertices, 4 quads wound clockwise seen from +z.
    public static void Kolumna(Vector3[] v, Vector2[] uv, float h, int[] pasek, float p)
    {
        float w = h * .5f, lo = -1.5f * h, hi = 1.5f * h; int baza = Mathf.FloorToInt(p);
        for (int q = 0; q < 4; q++)
        {
            int stop = baza - 1 + q; float c = (stop - p) * h, y0 = c - h * .5f, y1 = c + h * .5f;
            float a0 = Mathf.Clamp(y0, lo, hi), a1 = Mathf.Clamp(y1, lo, hi);
            Komorka(Symbol(pasek, stop), true, out float u0, out float v0, out float u1, out float v1);
            float t0 = v0 + (a0 - y0) / h * (v1 - v0), t1 = v0 + (a1 - y0) / h * (v1 - v0);
            int b = q * 4;
            // seen from +z the reader's right is -x: the cell's left edge (u0) at +x
            v[b] = new Vector3(w, a0, 0); v[b + 1] = new Vector3(-w, a0, 0); v[b + 2] = new Vector3(-w, a1, 0); v[b + 3] = new Vector3(w, a1, 0);
            uv[b] = new Vector2(u0, t0); uv[b + 1] = new Vector2(u1, t0); uv[b + 2] = new Vector2(u1, t1); uv[b + 3] = new Vector2(u0, t1);
        }
    }
    public static readonly int[] KolumnaTrojkaty = { 0, 2, 1, 0, 3, 2, 4, 6, 5, 4, 7, 6, 8, 10, 9, 8, 11, 10, 12, 14, 13, 12, 15, 14 };
}
