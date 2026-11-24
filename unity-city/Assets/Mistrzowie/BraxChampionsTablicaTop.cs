using System.Collections.Generic;
using UnityEngine;
using R = BraxChampionsWynikiKladow;

// THE TOP 10 BOARD IN THE PADDOCK (owner 24.11.2026 20:31, his pick "Tablica TOP 10"; the cup and the podium beside it taken
// back at 22:06: "to miało być wypożyczalna rozrywka"): an LED board (256 x 128) in the paddock's west, its pages turning every
// 6 s as a results board's do: TOP 10 · WYŚCIG, TOP 10 · OKRĄŻENIE, TOP 10 · SKOK. Each row: the place, the value, who (TY in
// green), his quad's colour, the day. Painted pixel by pixel in the region's LED font (BraxChampionsPiksele); repainted when a
// page turns or a result comes in. Its picture has its mipmaps (no shimmer from afar), the LEDs' squares kept near (point
// filtering).
[DisallowMultipleComponent]
public sealed class BraxChampionsTablicaTop : MonoBehaviour
{
    public Renderer ekran;                       // the board's screen
    public float naStrone = 6f;

    public const int TW = 256, TH = 128, Wierszy = 10, Stron = 3;
    static readonly Color32 Czern = new Color32(4, 4, 6, 255), Zloto = new Color32(255, 196, 48, 255), Bialy = new Color32(240, 244, 255, 255),
        Czerwony = new Color32(255, 52, 40, 255), Zielony = new Color32(60, 255, 110, 255), Szary = new Color32(70, 74, 86, 255);
    // the columns (px): the place right-aligned to x 22, the value (8 marks: 1240 PKT), who (8), the quad (12: POMARAŃCZOWY), the day
    public const int KolMiejsce = 22, KolWartosc = 28, KolKto = 82, KolKlad = 134, KolData = 212;

    Texture2D tex; Color32[] px;
    int strona = -1, wersja = -1; float nastepna;

    void Start() { Odswiez(true); }

    void Update()
    {
        if (Time.time >= nastepna) Odswiez(true);
        else if (wersja != R.Wersja) Odswiez(false);
    }

    void Odswiez(bool nastepnaStrona)
    {
        if (!ekran) return;
        if (nastepnaStrona) { strona = (strona + 1) % Stron; nastepna = Time.time + naStrone; }
        wersja = R.Wersja;
        if (!tex)
        {
            tex = new Texture2D(TW, TH, TextureFormat.RGBA32, true) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, anisoLevel = 2, name = "tor kładów tablica TOP 10" };
            px = new Color32[TW * TH];
            var mat = new Material(ekran.sharedMaterial) { name = "tor kładów tablica TOP 10" };
            mat.SetTexture("_MainTex", tex); if (mat.HasProperty("_EmissionMap")) mat.SetTexture("_EmissionMap", tex);
            ekran.sharedMaterial = mat;
        }
        var r = strona == 0 ? R.Rodzaj.Wyscig : strona == 1 ? R.Rodzaj.Okrazenie : R.Rodzaj.Skok;
        string tytul = strona == 0 ? "TOP 10 · WYŚCIG" : strona == 1 ? "TOP 10 · OKRĄŻENIE" : "TOP 10 · SKOK";
        var w = new List<string[]>();
        var l = R.Lista(r);
        for (int i = 0; i < l.Count && i < Wierszy; i++) w.Add(new[] { (i + 1) + ".", R.Wartosc(r, l[i].w), l[i].kto, l[i].klad, l[i].data });
        if (l.Count == 0) w.Add(new[] { "", "", r == R.Rodzaj.Skok ? "SKOCZ NA FREESTYLE" : "BRAK WYNIKÓW · JEDŹ!", "", "" });
        Maluj(px, tytul, w, strona, Stron);
        tex.SetPixels32(px); tex.Apply(true);
    }

    /// <summary>A page: the title in gold over a red rule, ten rows in their columns (the player's green), the page's dots.</summary>
    public static void Maluj(Color32[] px, string tytul, IList<string[]> wiersze, int strona, int stron)
    {
        BraxChampionsPiksele.Wypelnij(px, Czern);
        BraxChampionsPiksele.Srodek(px, TW, TH, tytul, TW / 2, TH - 18, 2, Zloto);
        BraxChampionsPiksele.Prost(px, TW, TH, 4, TH - 23, TW - 4, TH - 22, Czerwony);
        for (int i = 0; i < wiersze.Count && i < Wierszy; i++)
        {
            var r = wiersze[i]; if (r == null) continue;
            int y = TH - 34 - i * 9;
            var c = r[2] == R.Gracz ? Zielony : Bialy;
            if (r[0].Length > 0) BraxChampionsPiksele.Tekst(px, TW, TH, r[0], KolMiejsce - BraxChampionsPiksele.Szer(r[0], 1), y, 1, c);
            BraxChampionsPiksele.Tekst(px, TW, TH, r[1], KolWartosc, y, 1, c);
            BraxChampionsPiksele.Tekst(px, TW, TH, r[2], KolKto, y, 1, c);
            BraxChampionsPiksele.Tekst(px, TW, TH, r[3], KolKlad, y, 1, c);
            BraxChampionsPiksele.Tekst(px, TW, TH, r[4], KolData, y, 1, c);
        }
        // the page's dots under the rows
        for (int s = 0; s < stron; s++)
        {
            int x = TW / 2 - stron * 4 + s * 8 + 2;
            BraxChampionsPiksele.Prost(px, TW, TH, x, 3, x + 4, 6, s == strona ? Zloto : Szary);
        }
    }
}
