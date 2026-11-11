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
    // a poster light box with no poster: the WKRÓTCE card
    public static void KartaWkrotce(Color32[] px)
    {
        int w = PW, h = PH; BraxChampionsPiksele.Wypelnij(px, Karta);
        BraxChampionsPiksele.Prost(px, w, h, 2, 2, w - 2, h - 2, KartaSrodek);
        S(px, w, h, "BRAXHORN", 14, 1, BraxChampionsPiksele.Bialy); S(px, w, h, "KINO", 26, 1, BraxChampionsPiksele.Cyjan);
        S(px, w, h, "WKRÓTCE", 50, 1, Bursztyn); S(px, w, h, "NOWY FILM", 66, 1, BraxChampionsPiksele.Bialy);
    }
}
