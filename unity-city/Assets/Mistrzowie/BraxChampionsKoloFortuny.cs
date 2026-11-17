using System;
using UnityEngine;

// MIASTO MISTRZÓW - the BIG SIX "KOŁO FORTUNY" of KASYNO WAWER (owner 16.11, the casinos' tables: Big Six "Koło Fortuny").
// The money wheel of the casinos: 54 segments - 1 x24, 2 x15, 5 x7, 10 x4, 20 x2 and the two specials, the owner's BRAXHORN
// logo and his coin (his art 1:1) - between chrome pins, a leather clapper at the top. AKCJA at a box of the betting table
// (the box in front of the player, as at the roulette) bets 10 on its symbol and the croupier spins: the wheel runs fast,
// the clapper ticks over the pins, it slows and stops; the segment under the clapper wins. A symbol pays its number to one
// (1:1 .. 20:1), the logo and the coin 40:1. Every segment is equally likely, as on the real wheel (the house edge 11-24 %).
public sealed class BraxChampionsKoloFortuny : MonoBehaviour, IBraxInterakcja
{
    public const int Segmenty = 54, Logo = 50, Moneta = 51;            // the specials' values (40:1)
    public static readonly int[] Symbole = { 1, 2, 5, 10, 20, Logo, Moneta };
    public static readonly string[] Nazwy = { "1", "2", "5", "10", "20", "LOGO BRAXHORN", "MONETA BRAXHORN" };

    public Transform stol;                                   // the betting table: origin on the floor at its middle, +z the players
    public Vector3[] pola = new Vector3[7];                  // the boxes (table frame), in the order of Symbole
    public Transform[] zetony = new Transform[7];
    public Transform kolo;                                   // turns about its local z (its face to local -z... the hall)
    public Transform klapka;                                 // the clapper, turns about its local z
    public string nazwa = "KOŁO FORTUNY";
    public int stawka = 10;
    public float czasObrotu = 8f;

    // ---------------- the wheel's layout (fixed, as a real wheel's): the specials opposite, the 20s opposite, the rest spread
    public static int[] Uklad()
    {
        var u = new int[Segmenty];
        u[0] = Logo; u[27] = Moneta; u[13] = 20; u[40] = 20;
        foreach (int i in new[] { 6, 20, 34, 47 }) u[i] = 10;
        foreach (int i in new[] { 3, 10, 17, 24, 31, 38, 51 }) u[i] = 5;
        int ones = 0, twos = 0, free = 0; for (int i = 0; i < Segmenty; i++) if (u[i] == 0) free++;
        // the 39 free segments: 24 ones and 15 twos spread evenly (a two every 2.6)
        float acc = 0f;
        for (int i = 0; i < Segmenty; i++)
        {
            if (u[i] != 0) continue;
            acc += 15f / 39f;
            if (acc >= 1f - 1e-4f && twos < 15) { u[i] = 2; twos++; acc -= 1f; } else { u[i] = 1; ones++; }
        }
        return u;
    }
    public static int Wyplata(int symbol, int wynik, int b)
    {
        if (symbol != wynik) return 0;
        return symbol >= Logo ? b * 41 : b * (symbol + 1);
    }

    int[] uklad; System.Random los; bool kreci; float start, katStart, katCel, katTeraz; int zaklad = -1, wynik;
    void OnEnable() { BraxInterakcje.Dodaj(this); }
    void OnDisable() { BraxInterakcje.Usun(this); }
    void Start()
    {
        uklad = Uklad(); los = new System.Random(Environment.TickCount ^ GetInstanceID());
        foreach (var z in zetony) if (z) z.gameObject.SetActive(false);
        katTeraz = (float)los.NextDouble() * 360f; Ustaw(katTeraz);
    }
    int Pole(Vector3 p)
    {
        if (!stol) return -1;
        var l = stol.InverseTransformPoint(p); if (Mathf.Abs(l.y) > 1.2f || l.z < 0f || l.z > 1.8f) return -1;
        int best = -1; float bd = 1.2f;
        for (int k = 0; k < pola.Length; k++) { float d = Mathf.Abs(l.x - pola[k].x); if (d < bd) { bd = d; best = k; } }
        return best;
    }
    public string Kontekst(Vector3 g)
    {
        int k = Pole(g); if (k < 0) return "";
        if (kreci) return nazwa + " · KOŁO SIĘ KRĘCI";
        return BraxChampionsKasyno.Kredyt >= stawka ? "POSTAW " + stawka + " · " + Nazwy[k] : "BRAK KREDYTU";
    }
    public bool Wykonaj(Vector3 g)
    {
        int k = Pole(g); if (k < 0) return false;
        if (kreci) return true;
        if (!BraxChampionsKasyno.Wydaj(stawka)) { BraxChampionsGry.Pokaz("BRAK KREDYTU · KASA", 1.6f); return true; }
        zaklad = k; if (zetony[k]) zetony[k].gameObject.SetActive(true);
        // the segment the wheel stops on (each equally likely), then a spin that brings it under the clapper after 4-5 turns
        wynik = los.Next(Segmenty); float cel = -wynik * 360f / Segmenty + (float)(los.NextDouble() - .5) * (360f / Segmenty) * .7f;
        float obroty = 4 + los.Next(2);
        katStart = katTeraz; katCel = katStart + obroty * 360f + Mathf.Repeat(cel - katStart, 360f);
        start = Time.time; kreci = true;
        BraxChampionsGry.Pokaz("KOŁO FORTUNY · KRĘCI!", 1.2f);
        return true;
    }
    void Update()
    {
        if (!kreci) { Klapka(katTeraz, 0f); return; }
        float u = Mathf.Clamp01((Time.time - start) / czasObrotu), e = 1f - Mathf.Pow(1f - u, 3f);
        float poprz = katTeraz; katTeraz = Mathf.Lerp(katStart, katCel, e); Ustaw(katTeraz);
        Klapka(katTeraz, katTeraz - poprz);
        if (u < 1f) { if (Blisko()) BraxChampionsGry.Ustaw(nazwa + " · " + Nazwy[zaklad] + " " + stawka + " · KREDYT " + BraxChampionsKasyno.Kredyt); return; }
        kreci = false;
        int symbol = Symbole[zaklad], wyg = Wyplata(symbol, uklad[wynik], stawka);
        if (wyg > 0) BraxChampionsKasyno.Wplac(wyg);
        string jest = uklad[wynik] == Logo ? "LOGO BRAXHORN" : uklad[wynik] == Moneta ? "MONETA BRAXHORN" : uklad[wynik].ToString();
        BraxChampionsGry.Pokaz("KOŁO FORTUNY · " + jest + (wyg > 0 ? " · WYGRANA " + wyg : ""), 3.2f);
        if (zetony[zaklad]) zetony[zaklad].gameObject.SetActive(false);
    }
    void Ustaw(float kat) { if (kolo) kolo.localRotation = Quaternion.AngleAxis(kat, Vector3.forward); }
    // the clapper rides up on a pin and drops after it (the pins every 360/54 degrees, the flap's tip over the rim)
    void Klapka(float kat, float krok)
    {
        if (!klapka) return;
        float faza = Mathf.Repeat(kat, 360f / Segmenty) / (360f / Segmenty);
        float wychyl = krok == 0f ? 0f : Mathf.Sign(krok) * 22f * Mathf.Clamp01(1f - faza * 3f);
        klapka.localRotation = Quaternion.AngleAxis(wychyl, Vector3.forward);
    }
    bool Blisko()
    {
        var g = BraxChampionsGry.Gracz; if (!g || !stol) return false;
        var l = stol.InverseTransformPoint(g.transform.position); return l.z > -1f && l.z < 4f && Mathf.Abs(l.x) < 3.5f;
    }
}
