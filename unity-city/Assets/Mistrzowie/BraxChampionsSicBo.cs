using System;
using UnityEngine;

// MIASTO MISTRZÓW - SIC BO, the three-dice game of the casinos (owner 16.11, the casinos' tables: "Sic Bo"). Three dice in a
// glass dome on the dealer's side: AKCJA at one of the three boxes along the players' edge (MAŁE 4-10, DOWOLNA TRÓJKA, DUŻE
// 11-17 - the box in front of the player, as at the roulette) bets 10; the croupier presses the shaker, the dice jump in the
// dome and settle. MAŁE and DUŻE pay 1:1 and lose on any triple (the house's edge, 2.78 %); DOWOLNA TRÓJKA pays 30:1. The dice
// are the casinos' (the one and the four red); each throw is three fair dice.
public sealed class BraxChampionsSicBo : MonoBehaviour, IBraxInterakcja
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

    System.Random los; bool gra; float start; int pole = -1; readonly int[] wynik = new int[3]; Quaternion[] koniec = new Quaternion[3];
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
        int k = Najblizsze(g); if (k < 0) return "";
        if (gra) return nazwa + " · KOŚCI W GRZE";
        return BraxChampionsKasyno.Kredyt >= stawka ? "POSTAW " + stawka + " · " + Pola[k] : "BRAK KREDYTU";
    }
    public bool Wykonaj(Vector3 g)
    {
        int k = Najblizsze(g); if (k < 0) return false;
        if (gra) return true;
        if (!BraxChampionsKasyno.Wydaj(stawka)) { BraxChampionsGry.Pokaz("BRAK KREDYTU · KASA", 1.6f); return true; }
        pole = k; if (zetony[k]) zetony[k].gameObject.SetActive(true);
        for (int i = 0; i < 3; i++) { wynik[i] = 1 + los.Next(6); koniec[i] = Obrot(wynik[i]); }
        gra = true; start = Time.time;
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
        if (t < Trzesie + Opada + .3f) { if (Blisko()) BraxChampionsGry.Ustaw(nazwa + " · " + Pola[pole] + " " + stawka + " · KREDYT " + BraxChampionsKasyno.Kredyt); return; }
        gra = false;
        int s = wynik[0] + wynik[1] + wynik[2], wyg = Wyplata(pole, wynik[0], wynik[1], wynik[2], stawka);
        if (wyg > 0) BraxChampionsKasyno.Wplac(wyg);
        bool tr = wynik[0] == wynik[1] && wynik[1] == wynik[2];
        BraxChampionsGry.Pokaz("SIC BO · " + wynik[0] + " · " + wynik[1] + " · " + wynik[2] + " = " + s + (tr ? " · TRÓJKA" : s <= 10 ? " · MAŁE" : " · DUŻE") +
            (wyg > 0 ? " · WYGRANA " + wyg : ""), 3.2f);
        if (zetony[pole]) zetony[pole].gameObject.SetActive(false);
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
}
