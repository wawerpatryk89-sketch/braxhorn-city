using System;
using UnityEngine;
using U = BraxChampionsStanowisko.Ui;

// MIASTO MISTRZÓW - SIC BO, the three-dice game of the casinos (owner 16.11, the casinos' tables: "Sic Bo"). Three dice in a
// glass dome on the dealer's side: AKCJA at one of the three boxes along the players' edge (MAŁE 4-10, DOWOLNA TRÓJKA, DUŻE
// 11-17 - the box in front of the player, as at the roulette) bets 10; the croupier presses the shaker, the dice jump in the
// dome and settle. MAŁE and DUŻE pay 1:1 and lose on any triple (the house's edge, 2.78 %); DOWOLNA TRÓJKA pays 30:1. The dice
// are the casinos' (the one and the four red); each throw is three fair dice.
// AT THE TABLE (owner 23.11 17:35/17:54, BraxChampionsStanowisko): AKCJA shows the felt; chips of the chosen value on MAŁE,
// DUŻE and DOWOLNA TRÓJKA (a touch on the box or its button, more than one may carry a bet), RZUĆ KOŚCI: the camera leans
// over the glass dome while the dice jump, the sum and the boxes' pays follow.
public sealed class BraxChampionsSicBo : MonoBehaviour, IBraxInterakcja, IBraxStanowisko
{
    public const int Male = 0, Trojka = 1, Duze = 2;
    public static readonly string[] Pola = { "MAŁE 4-10", "DOWOLNA TRÓJKA", "DUŻE 11-17" };
    public Transform stol;                                   // origin on the floor under the dealer's edge, +z the players
    public Vector3[] pola = new Vector3[3];                  // the boxes (table frame)
    public Transform[] zetony = new Transform[3];
    public Transform[] kostki = new Transform[3];            // the dice, children of the dome's base (their rest: on its floor)
    public Vector3[] spoczynek = new Vector3[3];             // the dice's places on the dome's floor (dome frame)
    public float kostkaBok = .025f, kopulaR = .14f;
    public string nazwa = "SIC BO";
    public int stawka = 10;

    // the faces: value -> the die's local direction showing it (a real die: opposite faces sum to 7)
    public static readonly Vector3[] Sciany = { Vector3.up, Vector3.forward, Vector3.right, Vector3.left, Vector3.back, Vector3.down };
    public static int Wyplata(int pole, int a, int b, int c, int st)
    {
        bool trojka = a == b && b == c; int s = a + b + c;
        if (pole == Trojka) return trojka ? st * 31 : 0;
        if (trojka) return 0;
        if (pole == Male) return s >= 4 && s <= 10 ? st * 2 : 0;
        return s >= 11 && s <= 17 ? st * 2 : 0;
    }

    System.Random los; bool gra; float start, koniecOd = -99f; int pole = -1, wygrana = -1, krok2; readonly int[] wynik = new int[3]; Quaternion[] koniec = new Quaternion[3];
    readonly int[] zaklady = new int[3];                     // the chips on MAŁE, TRÓJKA, DUŻE
    const float Trzesie = 2.4f, Opada = .6f;
    void OnEnable() { BraxInterakcje.Dodaj(this); }
    void OnDisable() { BraxInterakcje.Usun(this); }
    void Start()
    {
        los = new System.Random(Environment.TickCount ^ GetInstanceID());
        foreach (var z in zetony) if (z) z.gameObject.SetActive(false);
        for (int i = 0; i < 3; i++) { wynik[i] = 1 + los.Next(6); koniec[i] = Obrot(wynik[i]); Polozenie(i, 1f); }
    }
    // the die turned so that `v` is up, a random turn about the vertical
    Quaternion Obrot(int v) => Quaternion.AngleAxis((float)los.NextDouble() * 360f, Vector3.up) * Quaternion.FromToRotation(Sciany[v - 1], Vector3.up);
    int Najblizsze(Vector3 p)
    {
        if (!stol) return -1;
        var l = stol.InverseTransformPoint(p); if (Mathf.Abs(l.y) > 1.2f || l.z < 0f || l.z > 2.4f) return -1;
        int best = -1; float bd = 1.3f;
        for (int k = 0; k < pola.Length; k++) { float d = Mathf.Abs(l.x - pola[k].x); if (d < bd) { bd = d; best = k; } }
        return best;
    }
    public string Kontekst(Vector3 g)
    {
        int k = Najblizsze(g); if (k < 0 || BraxChampionsStanowisko.Aktywne) return "";
        return gra ? nazwa + " · KOŚCI W GRZE" : "GRAJ · " + nazwa;
    }
    public bool Wykonaj(Vector3 g)
    {
        int k = Najblizsze(g); if (k < 0) return false;
        BraxChampionsStanowisko.Wejdz(this, 0);
        return true;
    }
    public int Postawione => zaklady[0] + zaklady[1] + zaklady[2];
    /// <summary>A chip of `kwota` on a box (MAŁE, TRÓJKA, DUŻE).</summary>
    public bool Postaw(int k, int kwota)
    {
        if (gra || k < 0 || k > 2) return false;
        if (!BraxChampionsKasyno.Wydaj(kwota)) { BraxChampionsGry.Pokaz("BRAK KREDYTU · KASA", 1.6f); return false; }
        zaklady[k] += kwota; pole = k; if (zetony[k]) zetony[k].gameObject.SetActive(true);
        return true;
    }
    public void Zdejmij()
    {
        if (gra) return; int suma = Postawione;
        for (int k = 0; k < 3; k++) { zaklady[k] = 0; if (zetony[k]) zetony[k].gameObject.SetActive(false); }
        if (suma > 0) BraxChampionsKasyno.Wplac(suma);
    }
    /// <summary>RZUĆ KOŚCI: the croupier presses the shaker.</summary>
    public bool Rzuc()
    {
        if (gra) return false;
        if (Postawione <= 0) { BraxChampionsGry.Pokaz("POŁÓŻ ŻETONY NA POLACH", 1.6f); return false; }
        for (int i = 0; i < 3; i++) { wynik[i] = 1 + los.Next(6); koniec[i] = Obrot(wynik[i]); }
        gra = true; start = Time.time; wygrana = -1;
        BraxChampionsGry.Pokaz("SIC BO · KOŚCI!", 1.2f);
        return true;
    }
    void Update()
    {
        if (!gra) return;
        float t = Time.time - start;
        for (int i = 0; i < 3; i++)
        {
            var d = kostki[i]; if (!d) continue;
            if (t < Trzesie)
            {
                // the shaker: the dice jump and tumble in the dome (a few cm), each its own rhythm
                float f = 9f + i * 1.7f, ph = i * 2.1f;
                float h = kostkaBok * .5f + Mathf.Abs(Mathf.Sin(t * f + ph)) * .045f, r = kopulaR * .45f;     // on the floor .. 4.5 cm up
                d.localPosition = spoczynek[i] + new Vector3(Mathf.Sin(t * 5.3f + ph) * r * .5f, h, Mathf.Cos(t * 4.7f + ph * 1.3f) * r * .5f);
                d.localRotation = Quaternion.Euler(t * (420f + 60f * i), t * (310f - 40f * i), t * (530f + 25f * i));
            }
            else Polozenie(i, Mathf.Clamp01((t - Trzesie) / Opada));
        }
        if (t < Trzesie + Opada + .3f) return;
        gra = false;
        int s = wynik[0] + wynik[1] + wynik[2], wyg = 0;
        for (int k = 0; k < 3; k++) if (zaklady[k] > 0) wyg += Wyplata(k, wynik[0], wynik[1], wynik[2], zaklady[k]);
        if (wyg > 0) BraxChampionsKasyno.Wplac(wyg);
        wygrana = wyg; koniecOd = Time.time;
        bool tr = wynik[0] == wynik[1] && wynik[1] == wynik[2];
        BraxChampionsGry.Pokaz("SIC BO · " + wynik[0] + " · " + wynik[1] + " · " + wynik[2] + " = " + s + (tr ? " · TRÓJKA" : s <= 10 ? " · MAŁE" : " · DUŻE") +
            (wyg > 0 ? " · WYGRANA " + wyg : ""), 3.2f);
        for (int k = 0; k < 3; k++) { zaklady[k] = 0; if (zetony[k]) zetony[k].gameObject.SetActive(false); }
    }
    // settling: from wherever the shaking left it down onto its place, turned to its value
    void Polozenie(int i, float u)
    {
        var d = kostki[i]; if (!d) return; float e = 1f - (1f - u) * (1f - u);
        var cel = spoczynek[i] + Vector3.up * (kostkaBok * .5f);
        d.localPosition = Vector3.Lerp(d.localPosition, cel, u >= 1f ? 1f : e * .5f + .5f * u);
        d.localRotation = Quaternion.Slerp(d.localRotation, koniec[i], u >= 1f ? 1f : e);
    }
    bool Blisko()
    {
        var g = BraxChampionsGry.Gracz; if (!g || !stol) return false;
        var l = stol.InverseTransformPoint(g.transform.position); return l.z > -1f && l.z < 3.5f && Mathf.Abs(l.x) < 3f;
    }

    // ================================================================ at the table (BraxChampionsStanowisko)
    public string Tytul(int i) => nazwa + " · MIN. " + stawka;
    public void Kadr(int i, Camera cam, out Vector3 oko, out Quaternion obrot)
    {
        var dno = kostki.Length > 0 && kostki[0] ? kostki[0].parent : null;
        if (dno && (gra || Time.time - koniecOd < 2.6f))
        {
            // over the glass dome: the dice jumping, then lying with their values up
            var c = stol.InverseTransformPoint(dno.position);
            BraxChampionsStanowisko.KadrBlatu(cam, stol, c + Vector3.up * .03f, kopulaR * 3.2f, kopulaR * 3.2f, 34f, out oko, out obrot);
            return;
        }
        BraxChampionsStanowisko.KadrBlatu(cam, stol, new Vector3(0f, pola.Length > 0 ? pola[0].y : .8f, .52f), 2.45f, 1.05f, 48f, out oko, out obrot);
    }
    static readonly string[] Kursy = { "1 : 1", "30 : 1", "1 : 1" };
    public void Panel(int i, U ui)
    {
        var r = ui.dol; float p = ui.m, h = r.height - 2f * p, y = r.y + p;
        var kroki = BraxChampionsStanowisko.Stawki(stawka);
        var stR = new Rect(r.x + p, y, r.width * .24f, h);
        krok2 = ui.Stawka(stR, kroki, krok2, !gra, "ŻETON");
        int zeton = kroki[Mathf.Clamp(krok2, 0, kroki.Length - 1)], suma = Postawione;
        var glR = new Rect(r.xMax - p - r.width * .19f, y, r.width * .19f, h);
        var srR = new Rect(stR.xMax + p, y, glR.x - stR.xMax - 2f * p, h);
        float bw = (srR.width - 2f * p) / 3f;
        for (int k = 0; k < 3; k++)
        {
            var b = new Rect(srR.x + k * (bw + p), y, bw, h);
            string txt = Pola[k] + "\n" + (zaklady[k] > 0 ? BraxChampionsAutomaty.Kwota(zaklady[k]) : Kursy[k]);
            if (ui.Przycisk(b, txt, k == Trojka ? new Color(.42f, .05f, .08f) : new Color(.06f, .16f, .38f), U.Krem, !gra && BraxChampionsKasyno.Kredyt >= zeton, false, U.Zloto)) Postaw(k, zeton);
        }
        // the boxes on the felt answer a touch too
        for (int k = 0; k < 3 && k < pola.Length; k++)
        {
            var c = pola[k];
            if (!gra && ui.Dotyk(stol.TransformPoint(c + new Vector3(-.34f, 0f, -.22f)), stol.TransformPoint(c + new Vector3(.34f, 0f, -.22f)),
                                 stol.TransformPoint(c + new Vector3(.34f, 0f, .22f)), stol.TransformPoint(c + new Vector3(-.34f, 0f, .22f)))) Postaw(k, zeton);
            if (zaklady[k] > 0) ui.Etykieta(stol.TransformPoint(c + new Vector3(0f, .01f, -.12f)), BraxChampionsAutomaty.Kwota(zaklady[k]), new Color(0f, 0f, 0f, .72f), U.Zloto);
        }
        // the dice's sum over the dome once they lie
        if (!gra && wygrana >= 0 && kostki.Length > 0 && kostki[0])
        {
            int sm = wynik[0] + wynik[1] + wynik[2]; bool tr = wynik[0] == wynik[1] && wynik[1] == wynik[2];
            // under the dome, toward the player (the flash runs under the top bar)
            ui.Etykieta(kostki[0].parent.position + stol.TransformDirection(Vector3.forward) * (kopulaR * 1.05f), wynik[0] + " · " + wynik[1] + " · " + wynik[2] + " = " + sm + (tr ? " · TRÓJKA" : sm <= 10 ? " · MAŁE" : " · DUŻE"),
                new Color(0f, 0f, 0f, .75f), U.Zloto, .03f);
        }
        var g1 = new Rect(glR.x, y, glR.width, h * .62f - p * .25f); var g2 = new Rect(glR.x, g1.yMax + p * .5f, glR.width, h - g1.height - p * .5f);
        if (ui.Przycisk(g1, gra ? "KOŚCI W GRZE" : "RZUĆ KOŚCI", U.Czerwien, U.Krem, !gra && suma > 0, true)) Rzuc();
        if (ui.Przycisk(g2, "ZDEJMIJ", U.Szary, U.Krem, !gra && suma > 0)) Zdejmij();
    }
    public void Wstal(int i) { if (!gra) Zdejmij(); }
}
