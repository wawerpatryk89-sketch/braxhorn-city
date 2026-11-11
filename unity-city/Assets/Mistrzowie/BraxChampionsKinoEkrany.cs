using UnityEngine;

// G1 KINO: what the cinema's screens show when nothing is loaded yet (the editor paints these into the generated
// textures, BraxChampionsKino repaints them live from the repertoire). The region's 5 x 7 LED font (BraxChampionsPiksele).
public static class BraxChampionsKinoEkrany
{
    public const int TW = 320, TH = 144, EW = 256, EH = 144, DW = 128, DH = 32, PW = 64, PH = 96;
    public static readonly Color32 Ciemny = new Color32(5, 5, 8, 255), Pasek = new Color32(120, 14, 22, 255), Bursztyn = new Color32(255, 176, 40, 255),
        Wybor = new Color32(36, 10, 14, 255), Sala = new Color32(18, 18, 22, 255), Karta = new Color32(70, 8, 14, 255), KartaSrodek = new Color32(20, 4, 8, 255);

    public static void T(Color32[] px, int w, int h, string s, int x, int yTop, int sk, Color32 c) => BraxChampionsPiksele.Tekst(px, w, h, s, x, h - yTop - 7 * sk, sk, c);
    public static void S(Color32[] px, int w, int h, string s, int yTop, int sk, Color32 c) => BraxChampionsPiksele.Srodek(px, w, h, s, w / 2, h - yTop - 7 * sk, sk, c);
    public static string Krotko(string s, int n) => s.Length <= n ? s : s.Substring(0, n - 1) + ".";
    public static string Hm(System.DateTime t) => t.Hour.ToString("00") + ":" + t.Minute.ToString("00");

    // the lobby's repertoire board with no films yet
    public static void TablicaWkrotce(Color32[] px, string godzina)
    {
        int w = TW, h = TH; BraxChampionsPiksele.Wypelnij(px, Ciemny);
        BraxChampionsPiksele.Prost(px, w, h, 0, h - 16, w, h, Pasek);
        T(px, w, h, "BRAXHORN KINO · REPERTUAR", 4, 4, 1, BraxChampionsPiksele.Bialy);
        if (!string.IsNullOrEmpty(godzina)) T(px, w, h, godzina, w - 34, 4, 1, BraxChampionsPiksele.Bialy);
        S(px, w, h, "NOWE FILMY", 46, 2, BraxChampionsPiksele.Bialy);
        S(px, w, h, "WKRÓTCE", 70, 3, Bursztyn);
        S(px, w, h, "BRAXHORN CITY · 8 SAL", 112, 1, BraxChampionsPiksele.Cyjan);
    }
    // a hall's screen between the shows with no film
    public static void EkranWkrotce(Color32[] px, int sala)
    {
        int w = EW, h = EH; BraxChampionsPiksele.Wypelnij(px, Sala);
        S(px, w, h, "BRAXHORN KINO", 12, 1, BraxChampionsPiksele.Cyjan);
        S(px, w, h, "SALA " + (sala + 1), 26, 2, BraxChampionsPiksele.Bialy);
        S(px, w, h, "WKRÓTCE", 60, 4, Bursztyn);
        S(px, w, h, "NOWE FILMY BRAXHORN", 104, 1, BraxChampionsPiksele.Bialy);
    }
    // the small screen over a hall's door
    public static void DrzwiWkrotce(Color32[] px, int sala)
    {
        int w = DW, h = DH; BraxChampionsPiksele.Wypelnij(px, Ciemny);
        BraxChampionsPiksele.Prost(px, w, h, 0, 0, 22, h, Pasek);
        T(px, w, h, (sala + 1).ToString(), 8, 12, 1, BraxChampionsPiksele.Bialy);
        T(px, w, h, "WKRÓTCE", 26, 5, 1, Bursztyn);
        T(px, w, h, "NOWY FILM", 26, 18, 1, BraxChampionsPiksele.Cyjan);
    }
    // the bar's three menu screens (owner 19:30: "trzy monitorki. Co to jest"): POPCORN / NAPOJE / ZESTAWY with prices
    public static void MenuBaru(Color32[] px, int k)
    {
        int w = EW, h = EH; BraxChampionsPiksele.Wypelnij(px, Ciemny);
        BraxChampionsPiksele.Prost(px, w, h, 0, h - 22, w, h, Pasek);
        string[] tyt = { "POPCORN", "NAPOJE", "ZESTAWY KINO" };
        string[][] poz = {
            new[] { "MAŁY 1,5 L", "14 ZŁ", "ŚREDNI 3 L", "19 ZŁ", "DUŻY 5 L", "24 ZŁ", "KARMEL / SER", "+3 ZŁ" },
            new[] { "COLA 0,5 L", "9 ZŁ", "WODA 0,5 L", "6 ZŁ", "SOK 0,3 L", "8 ZŁ", "KAWA LATTE", "11 ZŁ" },
            new[] { "ZESTAW 1: POPCORN + COLA", "26 ZŁ", "ZESTAW 2: 2 X POPCORN", "45 ZŁ", "NACHOS + SOS", "17 ZŁ", "HOT DOG", "12 ZŁ" } };
        k = Mathf.Clamp(k, 0, 2);
        S(px, w, h, tyt[k], 5, 2, BraxChampionsPiksele.Bialy);
        for (int i = 0; i < 4; i++)
        {
            int y = 34 + i * 24;
            T(px, w, h, poz[k][2 * i], 10, y, 1, Bursztyn);
            string c = poz[k][2 * i + 1]; T(px, w, h, c, w - 10 - BraxChampionsPiksele.Szer(c, 2), y - 3, 2, BraxChampionsPiksele.Bialy);
            if (i < 3) BraxChampionsPiksele.Prost(px, w, h, 10, h - y - 16, w - 10, h - y - 15, new Color32(60, 60, 70, 255));
        }
        S(px, w, h, "BAR · BRAXHORN KINO", h - 14, 1, BraxChampionsPiksele.Cyjan);
    }
    // a poster light box with no poster: the WKRÓTCE card
    public static void KartaWkrotce(Color32[] px)
    {
        int w = PW, h = PH; BraxChampionsPiksele.Wypelnij(px, Karta);
        BraxChampionsPiksele.Prost(px, w, h, 2, 2, w - 2, h - 2, KartaSrodek);
        S(px, w, h, "BRAXHORN", 14, 1, BraxChampionsPiksele.Bialy); S(px, w, h, "KINO", 26, 1, BraxChampionsPiksele.Cyjan);
        S(px, w, h, "WKRÓTCE", 50, 1, Bursztyn); S(px, w, h, "NOWY FILM", 66, 1, BraxChampionsPiksele.Bialy);
    }

    // ================================================================ G1 GALERIA GŁÓWNA screens (owner 11.11 19:49: the blue screens were empty)
    public const int PGW = 256, PGH = 384, ML = 256, MH = 128, LW = 256, LH = 144, FW = 64, FH = 128;
    static Color32 Kategoria(string t)
    {
        switch (t)
        {
            case "fashion": case "bags": return new Color32(190, 90, 255, 255);
            case "shoes": return new Color32(255, 130, 60, 255);
            case "electronics": case "phones": case "games": return new Color32(60, 150, 255, 255);
            case "cosmetics": case "pharmacy": case "optician": return new Color32(70, 230, 120, 255);
            case "jewellery": return new Color32(240, 244, 255, 255);
            case "food": case "cafe": return new Color32(255, 176, 40, 255);
            case "toys": case "books": return new Color32(255, 220, 60, 255);
            case "home": case "flowers": return new Color32(170, 120, 80, 255);
            default: return new Color32(150, 150, 165, 255);
        }
    }
    // the directory: PARTER / PIĘTRO in two columns, a colour per trade, the number of the unit, "TU JESTEŚ"
    public static void PlanGalerii(Color32[] px, string[] n0, string[] t0, string[] n1, string[] t1, string tu)
    {
        int w = PGW, h = PGH; BraxChampionsPiksele.Wypelnij(px, Ciemny);
        BraxChampionsPiksele.Prost(px, w, h, 0, h - 36, w, h, Pasek);
        S(px, w, h, "PLAN GALERII", 5, 2, BraxChampionsPiksele.Bialy);
        S(px, w, h, "GALERIA GŁÓWNA · BRAXHORN", 24, 1, BraxChampionsPiksele.Cyjan);
        void Kol(string[] n, string[] t, int x0, string tyt, int nr0)
        {
            T(px, w, h, tyt, x0 + 4, 44, 1, Bursztyn);
            BraxChampionsPiksele.Prost(px, w, h, x0 + 4, h - 55, x0 + 122, h - 54, new Color32(80, 80, 92, 255));
            for (int i = 0; i < n.Length; i++)
            {
                int y = 60 + i * 13;
                BraxChampionsPiksele.Prost(px, w, h, x0 + 4, h - y - 7, x0 + 9, h - y, Kategoria(t[i]));
                T(px, w, h, (nr0 + i).ToString(), x0 + 12, y, 1, new Color32(150, 150, 165, 255));
                T(px, w, h, Krotko(n[i], 15), x0 + 31, y, 1, BraxChampionsPiksele.Bialy);
            }
        }
        Kol(n0, t0, 0, "PARTER", 1); Kol(n1, t1, 128, "PIĘTRO", 101);
        BraxChampionsPiksele.Prost(px, w, h, 128, 40, 129, h - 48, new Color32(60, 60, 70, 255));
        BraxChampionsPiksele.Prost(px, w, h, 0, 0, w, 22, new Color32(18, 18, 24, 255));
        BraxChampionsPiksele.Prost(px, w, h, 8, 7, 16, 15, BraxChampionsPiksele.Czerwony);
        T(px, w, h, "TU JESTEŚ: " + tu, 22, h - 15, 1, BraxChampionsPiksele.Bialy);
    }
    // a food court counter's three menu screens: k the counter (0..7), e the screen (0 ZESTAWY, 1 DANIA, 2 NAPOJE)
    static readonly string[] Lokale = { "PIZZA NAPOLI", "SUSHI ZEN", "BURGER BAR", "KEBAB GRILL", "PIEROGI U BABCI", "THAI WOK", "SAŁATKI FRESH", "CAFE DOLCE" };
    static readonly string[][] Dania = {
        new[] { "MARGHERITA 32 CM", "29 ZŁ", "PEPPERONI", "34 ZŁ", "CAPRICCIOSA", "36 ZŁ", "QUATTRO FORMAGGI", "38 ZŁ" },
        new[] { "ZESTAW MAKI 12 SZT.", "36 ZŁ", "NIGIRI ŁOSOŚ 6 SZT.", "32 ZŁ", "CALIFORNIA ROLL", "29 ZŁ", "ZUPA MISO", "12 ZŁ" },
        new[] { "CLASSIC BURGER", "27 ZŁ", "CHEESEBURGER", "29 ZŁ", "BACON BBQ", "33 ZŁ", "FRYTKI", "10 ZŁ" },
        new[] { "KEBAB W BUŁCE", "24 ZŁ", "KEBAB TALERZ", "32 ZŁ", "DURUM", "26 ZŁ", "FALAFEL", "22 ZŁ" },
        new[] { "RUSKIE 10 SZT.", "24 ZŁ", "Z MIĘSEM 10 SZT.", "27 ZŁ", "Z KAPUSTĄ", "25 ZŁ", "BARSZCZ", "11 ZŁ" },
        new[] { "PAD THAI", "33 ZŁ", "KURCZAK CURRY", "31 ZŁ", "WOK WOŁOWINA", "36 ZŁ", "SAJGONKI 4 SZT.", "14 ZŁ" },
        new[] { "CEZAR Z KURCZAKIEM", "28 ZŁ", "GRECKA", "25 ZŁ", "BOWL Z ŁOSOSIEM", "34 ZŁ", "KREM DNIA", "14 ZŁ" },
        new[] { "ESPRESSO", "9 ZŁ", "CAPPUCCINO", "13 ZŁ", "SERNIK", "16 ZŁ", "CROISSANT", "9 ZŁ" } };
    public static void MenuLokalu(Color32[] px, int k, int e)
    {
        int w = ML, h = MH; k = Mathf.Clamp(k, 0, 7); e = Mathf.Clamp(e, 0, 2); BraxChampionsPiksele.Wypelnij(px, Ciemny);
        BraxChampionsPiksele.Prost(px, w, h, 0, h - 20, w, h, Pasek);
        S(px, w, h, Lokale[k] + (e == 0 ? " · ZESTAWY" : e == 1 ? " · MENU" : " · NAPOJE"), 6, 1, BraxChampionsPiksele.Bialy);
        string[] poz = e == 1 ? Dania[k] : e == 0
            ? new[] { "ZESTAW DNIA + NAPÓJ", "35 ZŁ", "ZESTAW DUŻY", "42 ZŁ", "ZESTAW DZIECIĘCY", "21 ZŁ", "DOKŁADKA", "+6 ZŁ" }
            : new[] { "COLA 0,5 L", "9 ZŁ", "WODA 0,5 L", "6 ZŁ", "LEMONIADA", "12 ZŁ", "HERBATA", "8 ZŁ" };
        for (int i = 0; i < 4; i++)
        {
            int y = 28 + i * 24;
            T(px, w, h, Krotko(poz[2 * i], 22), 8, y + 3, 1, Bursztyn);
            string c = poz[2 * i + 1]; T(px, w, h, c, w - 8 - BraxChampionsPiksele.Szer(c, 2), y, 2, BraxChampionsPiksele.Bialy);
            if (i < 3) BraxChampionsPiksele.Prost(px, w, h, 8, h - y - 19, w - 8, h - y - 18, new Color32(60, 60, 70, 255));
        }
    }
    // the hall's LOMBARD GSM kiosk price screen (owner 11.11 19:49: "lombard z telefonami ... mały")
    public static void EkranLombardu(Color32[] px)
    {
        int w = LW, h = LH; BraxChampionsPiksele.Wypelnij(px, Ciemny);
        BraxChampionsPiksele.Prost(px, w, h, 0, h - 22, w, h, Pasek);
        S(px, w, h, "LOMBARD GSM · CENNIK", 7, 1, BraxChampionsPiksele.Bialy);
        string[] poz = { "SMARTFON BX 15 PRO", "2399 ZŁ", "SMARTFON BX 14", "1699 ZŁ", "TABLET BX TAB 11", "899 ZŁ", "ZEGAREK BX WATCH", "549 ZŁ", "WYMIANA EKRANU", "OD 199 ZŁ" };
        for (int i = 0; i < 5; i++)
        {
            int y = 28 + i * 21;
            T(px, w, h, poz[2 * i], 8, y + 4, 1, Bursztyn);
            string c = poz[2 * i + 1]; T(px, w, h, c, w - 8 - BraxChampionsPiksele.Szer(c, 2), y, 2, BraxChampionsPiksele.Bialy);
        }
        S(px, w, h, "SKUP TELEFONÓW · WYCENA OD RĘKI", h - 12, 1, BraxChampionsPiksele.Zielony);
    }
    // a phone's home screen (the kiosk's and the shops' phones): a gradient, the clock, a grid of app tiles, the dock
    public static void EkranTelefonu(Color32[] px, int wariant)
    {
        int w = FW, h = FH;
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                float t = y / (float)h; var a = wariant % 2 == 0 ? new Color(.10f, .16f, .45f) : new Color(.45f, .08f, .20f); var b = new Color(.02f, .02f, .05f);
                var c = Color.Lerp(b, a, t); px[y * w + x] = new Color32((byte)(c.r * 255), (byte)(c.g * 255), (byte)(c.b * 255), 255);
            }
        T(px, w, h, "12:00", 17, 8, 1, BraxChampionsPiksele.Bialy);
        Color32[] ik = { new Color32(70, 230, 120, 255), new Color32(60, 150, 255, 255), new Color32(255, 176, 40, 255), new Color32(255, 70, 80, 255), new Color32(190, 90, 255, 255), new Color32(240, 244, 255, 255) };
        for (int r = 0; r < 4; r++) for (int c = 0; c < 4; c++)
            {
                int x0 = 5 + c * 14, y0 = h - 30 - r * 16;
                BraxChampionsPiksele.Prost(px, w, h, x0, y0 - 10, x0 + 10, y0, ik[(r * 4 + c + wariant) % ik.Length]);
            }
        BraxChampionsPiksele.Prost(px, w, h, 3, 4, w - 3, 18, new Color32(40, 44, 62, 255));
        for (int c = 0; c < 4; c++) BraxChampionsPiksele.Prost(px, w, h, 7 + c * 14, 6, 15 + c * 14, 15, ik[(c + 2) % ik.Length]);
    }

    // a portrait advertising screen's still picture (until the owner's 9:16 films play on it): a dark gradient, the brand,
    // the offer of the week, the gallery's name
    public const int VW = 144, VH = 256;
    public static void ReklamaPion(Color32[] px, int k)
    {
        int w = VW, h = VH;
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                float t = y / (float)h; var a = k % 2 == 0 ? new Color(.55f, .04f, .07f) : new Color(.05f, .10f, .30f);
                var c = Color.Lerp(new Color(.02f, .02f, .03f), a, t);
                px[y * w + x] = new Color32((byte)(c.r * 255), (byte)(c.g * 255), (byte)(c.b * 255), 255);
            }
        string[][] txt = { new[] { "BRAXHORN", "HOME", "-20%", "MEBLE", "DO NIEDZIELI" }, new[] { "KINO", "MULTIPLEX", "2 FILMY", "BILETY", "W KASACH" }, new[] { "TECHNO", "SPOT", "4K TV", "BRAXHORN", "OLED 65 CALI" } };
        var t0 = txt[k % txt.Length];
        S(px, w, h, t0[0], 30, 2, BraxChampionsPiksele.Bialy);
        S(px, w, h, t0[1], 52, 2, BraxChampionsPiksele.Bialy);
        S(px, w, h, t0[2], 112, 3, Bursztyn);
        S(px, w, h, t0[3], 170, 2, BraxChampionsPiksele.Bialy);
        S(px, w, h, t0[4], 192, 1, BraxChampionsPiksele.Cyjan);
        BraxChampionsPiksele.Prost(px, w, h, 0, 0, w, 18, new Color32(10, 10, 14, 255));
        S(px, w, h, "GALERIA GŁÓWNA", h - 13, 1, BraxChampionsPiksele.Bialy);
    }
}
