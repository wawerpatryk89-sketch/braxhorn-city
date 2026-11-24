using System.Collections.Generic;
using UnityEngine;
using R = BraxChampionsWynikiKladow;

// THE QUAD RACE OF THE BRAXHORN TRACK (owner 24.11.2026 12:57, his pick "Wyścig z rywalami": "4 kłady AI, odliczanie, bramka
// opada, 3 okrążenia, pozycje, tablica LED w padoku"; 20:31 his picks "Turniej z rywalami", "Podium i puchar", "Tablica TOP 10").
// Riding a quad behind the start gate the player gets WYŚCIG (a button at the top, T on a desktop) and PUCHAR BRAXHORN (the
// button under it, Y): his quad and the four rivals' (BraxChampionsKladRywal: the riders' park beside the paddock) take the
// gate's five places, the bars come up, 3 - 2 - 1, the bars drop and everybody rides. Every rider's place on the race's line
// (the track's centre every 2 m from the gate) is followed from where it was, never jumping to a section beside it - a cut
// across the infield gains nothing; the positions are the distances ridden.
//   - The race: the gate to the finish line, then two laps. When the player is over the line the order is set (a rival still
//     riding: its time from its own pace), the results come up on the LED board beside the start straight and the real times
//     go on the TOP 10 board (BraxChampionsWynikiKladow, BraxChampionsTablicaTop).
//   - The cup: three races of one lap each with the same four rivals, the points of the motocross world championship (MXGP:
//     25, 22, 20, 18, 16 for the first five), a tie going to the better place in the last race (as in motocross); the
//     standings between the races on the LED board and on the screen, the next race from the gate 12 s after the last; the
//     winner on the cup's board, a cup won counted for the player.
//   - The podium (by the finish line, BraxChampionsPodiumKladow): after a race or the cup the first three - on their quads -
//     stand on its steps facing the paddock, the gold, the silver and the bronze cup on the steps before them; the player not
//     among them watches from before it. 14 s, then the rivals are back in their park and the player rides off.
// Daytime only, as the whole track (owner 20:31 "nie do jazdy w nocy"). Getting off the quad stops the race and the cup.
[DisallowMultipleComponent]
[DefaultExecutionOrder(5080)]
public sealed class BraxChampionsWyscig : MonoBehaviour
{
    // ---------------------------------------------------------------- set by the builder (BraxChampionsTorKladow)
    public Vector3[] punkty = new Vector3[0];    // the centre line, world, every `krok` nominal metres, index 0 at the gate
    public Vector3[] lewo = new Vector3[0];      // the left normal there (flat, unit)
    public float[] vCel = new float[0];          // the riding speed there, real m/s (corners, berms, obstacles, braking ahead)
    public bool[] skok = new bool[0];            // a double's run-up and flight: its own speed, whatever the rider's skill
    public float[] szer = new float[0];          // the riding width there, nominal metres (8 the start, 7 the field, 5 the woods)
    public float krok = 2f;                      // nominal metres between the points
    public float dlugosc = 1000f;                // the loop, nominal metres
    public float sMety;                          // the finish line from the gate, nominal metres
    public float polSzerokosci = 5.2f;           // half the track + its shoulder, nominal
    public float K = 1f;
    public int okrazen = 3;                      // a race's laps
    public int okrazenPucharu = 1;               // a cup race's laps (three races: ~4 min each)
    public int wyscigowPucharu = 3;
    public Vector3[] sloty = new Vector3[5];     // the gate's five places (the quads' roots), world
    public Quaternion kierunekStartu = Quaternion.identity;
    public Transform[] belki = new Transform[0]; // the drop bars' hinges
    public Quaternion belkaGora = Quaternion.identity, belkaDol = Quaternion.identity;
    public BraxChampionsKladRywal[] rywale = new BraxChampionsKladRywal[0];
    public Renderer tablica;                     // the LED board's screen
    public Vector3 strefaStartu;                 // world: behind the gate, where WYŚCIG is offered
    public float strefaPromien = 24f;            // nominal metres
    public const int SlotGracza = 2;             // the middle place
    // ---- the podium (BraxChampionsPodiumKladow)
    public Vector3[] podium = new Vector3[0];    // the steps' tops (the quads' roots), world: the 1st, the 2nd, the 3rd
    public Quaternion podiumObrot = Quaternion.identity;        // on them: facing the paddock
    public Vector3 podiumWidz;                   // where the player watches from when he is not on it (world)
    public Quaternion podiumWidzObrot = Quaternion.identity;
    public Transform[] puchary = new Transform[0];             // the gold, the silver, the bronze cup on their table
    public Vector3[] pucharyNaPodium = new Vector3[0];         // where each stands on its step at the ceremony (world)

    /// <summary>The MXGP points of a race's places (FIM Motocross World Championship: 25, 22, 20, 18, 16, 15 ...).</summary>
    public static readonly int[] PunktyMXGP = { 25, 22, 20, 18, 16, 15, 14, 13, 12, 11 };
    public const float CzasWynikow = 8f, CzasPrzerwy = 12f, CzasPodium = 14f;

    // ---------------------------------------------------------------- state
    enum Faza { Wolny, Odliczanie, Jazda, Wyniki, Podium }
    Faza faza = Faza.Wolny; float fazaOd, start, odswiez;
    sealed class Uczestnik { public BraxChampionsKlad klad; public BraxChampionsKladRywal rywal; public string imie; public int idx; public float dystans, czas; public bool meta, szac; }
    readonly List<Uczestnik> lista = new List<Uczestnik>();
    readonly List<BraxChampionsKlad> kwady = new List<BraxChampionsKlad>();
    Uczestnik ja;
    int wynikMiejsce, wynikTop; bool wynikRekord; float wynikCzas;
    readonly List<string> ostatnie = new List<string>();
    string komunikat = ""; float komunikatDo;
    public List<BraxChampionsKlad> Kwady() => kwady;
    int Okr => puchar ? okrazenPucharu : okrazen;
    public float Dystans => sMety + Okr * dlugosc;
    public bool Trwa => faza == Faza.Odliczanie || faza == Faza.Jazda;

    // the cup
    bool puchar, stronaPucharu; int ukonczone;
    sealed class Zawodnik { public string imie, klad; public int punkty; public readonly List<int> miejsca = new List<int>(); }
    readonly List<Zawodnik> tabela = new List<Zawodnik>();
    string zwyciezca = ""; bool wygrales;
    public bool PucharTrwa => puchar && faza != Faza.Wolny;
    public int WyscigPucharu => ukonczone;                     // the cup's races ridden
    public int WersjaPucharu { get; private set; }             // bumps when its standings change (the TOP 10 board repaints)
    // the podium
    readonly List<Uczestnik> naPodium = new List<Uczestnik>();
    Vector3[] stolPoz = new Vector3[0]; Quaternion[] stolObr = new Quaternion[0];

    const string KluczRekordu = "BRAX_TOR_WYSCIG_REKORD";
    float rekord = -1f;
    public float Rekord { get { if (rekord < 0f) { try { rekord = PlayerPrefs.GetFloat(KluczRekordu, 0f); } catch { rekord = 0f; } } return rekord; } }

    void Start()
    {
        foreach (var r in rywale) if (r) { r.wyscig = this; r.Parkuj(); }
        stolPoz = new Vector3[puchary.Length]; stolObr = new Quaternion[puchary.Length];
        for (int i = 0; i < puchary.Length; i++) if (puchary[i]) { stolPoz[i] = puchary[i].position; stolObr[i] = puchary[i].rotation; }
        Belki(0f);
        Tablica();
    }

    // ---------------------------------------------------------------- the flow
    bool MozeStart()
    {
        var k = BraxChampionsKlad.Jedzie;
        if (faza != Faza.Wolny || !k || k.tylkoRywal || rywale.Length == 0 || punkty.Length < 10) return false;
        var d = k.transform.position - strefaStartu; d.y = 0f;
        return d.magnitude < strefaPromien * K && Mathf.Abs(k.PredkoscKmh) < 15f;
    }

    void Update()
    {
        float t = Time.time - fazaOd;
        switch (faza)
        {
            case Faza.Wolny:
                if (MozeStart())
                {
                    if (Input.GetKeyDown(KeyCode.T) || Dotkniety(PrzyciskStartu())) Zacznij(false);
                    else if (Input.GetKeyDown(KeyCode.Y) || Dotkniety(PrzyciskPucharu())) Zacznij(true);
                }
                break;
            case Faza.Odliczanie:
                Belki(Mathf.Clamp01(t / .8f));
                if (!ja.klad || BraxChampionsKlad.Jedzie != ja.klad) { Przerwij(); break; }
                if (t >= 3.6f) Puszczaj();
                break;
            case Faza.Jazda:
                Belki(1f - Mathf.Clamp01(t / .25f));
                if (!ja.klad || BraxChampionsKlad.Jedzie != ja.klad) { Przerwij(); break; }
                foreach (var z in lista) Postep(z);
                if (ja.meta) { Koniec(); break; }
                odswiez -= Time.deltaTime; if (odswiez <= 0f) { odswiez = .5f; Tablica(); }
                break;
            case Faza.Wyniki:
                foreach (var z in lista) if (z.rywal) Postep(z);                 // the rivals roll over the line and slow
                if (puchar) { odswiez -= Time.deltaTime; if (odswiez <= 0f) { odswiez = 4f; stronaPucharu = !stronaPucharu; Tablica(); } }   // the race / the standings
                if (puchar && ukonczone < wyscigowPucharu)
                {
                    if (!ja.klad || BraxChampionsKlad.Jedzie != ja.klad) { Przerwij(); break; }
                    if (t > CzasPrzerwy) Ustawienie();                           // the cup's next race, from the gate
                }
                else if (t > CzasWynikow) { if (podium.Length >= 3) Podium(); else Sprzatnij(); }
                break;
            case Faza.Podium:
                if (t > CzasPodium) Sprzatnij();
                break;
        }
    }

    void Zacznij(bool kubek)
    {
        puchar = kubek; ukonczone = 0; tabela.Clear(); zwyciezca = ""; wygrales = false; WersjaPucharu++;
        Ustawienie();
    }

    // everybody to the gate (a race, or the cup's next one)
    void Ustawienie()
    {
        var k = BraxChampionsKlad.Jedzie; if (!k) { Sprzatnij(); return; }
        lista.Clear(); kwady.Clear();
        ja = new Uczestnik { klad = k, imie = R.Gracz }; lista.Add(ja); kwady.Add(k);
        k.Trzymaj(true); k.Ustaw(sloty[SlotGracza], kierunekStartu, Vector3.zero);
        int s = 0;
        foreach (var r in rywale)
        {
            if (!r || !r.Klad) continue;
            if (s == SlotGracza) s++;
            if (s >= sloty.Length) break;
            r.NaStart(this, sloty[s], kierunekStartu);
            lista.Add(new Uczestnik { klad = r.Klad, rywal = r, imie = r.imie }); kwady.Add(r.Klad);
            s++;
        }
        if (puchar) foreach (var z in lista) ZawodnikDla(z);
        faza = Faza.Odliczanie; fazaOd = Time.time;
        Tablica();
        Debug.Log("BRAX_WYSCIG ustawienie rywali=" + (lista.Count - 1) + (puchar ? " puchar wyscig=" + (ukonczone + 1) + "/" + wyscigowPucharu : ""));
    }

    void Puszczaj()
    {
        foreach (var z in lista)
        {
            z.idx = 0; z.dystans = 0f; z.meta = z.szac = false; z.czas = 0f;
            z.klad.Trzymaj(false);
            if (z.rywal) { z.rywal.Idx = 0; z.rywal.Meta = false; z.rywal.Aktywny = true; }
        }
        start = Time.time; faza = Faza.Jazda; fazaOd = Time.time; odswiez = 0f;
        Pokaz("START!", 1.2f);
        Debug.Log("BRAX_WYSCIG start");
    }

    // a rider's place on the line: the nearest point a little behind or ahead of the last (never across the infield)
    void Postep(Uczestnik z)
    {
        if (!z.klad) return;
        int n = punkty.Length; if (n < 10) return;
        var p = z.klad.transform.position;
        int best = z.idx; float bd = float.MaxValue;
        for (int k = -4; k <= 12; k++)
        {
            int i = ((z.idx + k) % n + n) % n; var d = punkty[i] - p; d.y *= .3f;
            float dd = d.sqrMagnitude; if (dd < bd) { bd = dd; best = i; }
        }
        int dk = best - z.idx; if (dk < -n / 2) dk += n; else if (dk > n / 2) dk -= n;
        z.idx = best; z.dystans += dk * krok;
        if (z.rywal) z.rywal.Idx = best;
        if (!z.meta && z.dystans >= Dystans)
        {
            z.meta = true; z.czas = Time.time - start; if (z.rywal) z.rywal.Meta = true;
            Debug.Log("BRAX_WYSCIG meta " + z.imie + " " + z.czas.ToString("F1"));
        }
    }

    static bool Przed(Uczestnik a, Uczestnik b) => a.meta ? (!b.meta || a.czas < b.czas) : (!b.meta && a.dystans > b.dystans);
    int Miejsce(Uczestnik z) { int m = 1; foreach (var o in lista) if (o != z && Przed(o, z)) m++; return m; }
    int Okrazenie(Uczestnik z) => Mathf.Clamp(Mathf.FloorToInt((z.dystans - sMety) / Mathf.Max(1f, dlugosc)) + 1, 1, Okr);

    void Koniec()
    {
        float teraz = Time.time - start;
        // a rival still riding: over the line at its own pace so far
        foreach (var z in lista) if (!z.meta) { float vs = z.dystans / Mathf.Max(1f, teraz); z.czas = teraz + (Dystans - z.dystans) / Mathf.Max(3f, vs); z.szac = true; }
        var kolejnosc = new List<Uczestnik>(lista); kolejnosc.Sort((a, b) => a.czas.CompareTo(b.czas));
        wynikMiejsce = kolejnosc.IndexOf(ja) + 1; wynikCzas = ja.czas; wynikTop = 0; wynikRekord = false;
        if (!puchar)
        {
            wynikRekord = Rekord <= 0f || ja.czas < Rekord;
            if (wynikRekord) { rekord = ja.czas; try { PlayerPrefs.SetFloat(KluczRekordu, ja.czas); PlayerPrefs.Save(); } catch { } }
            // the TOP 10 of the race (its distance: the gate + two laps): every real time over the line, the rivals' too
            foreach (var z in kolejnosc)
                if (z.meta && !z.szac) { int m = R.Dodaj(R.Rodzaj.Wyscig, z.czas, z.imie, R.Kolor(z.klad)); if (z == ja) wynikTop = m; }
        }
        ostatnie.Clear();
        for (int i = 0; i < kolejnosc.Count; i++) ostatnie.Add(Wiersz(i + 1, kolejnosc[i].imie, BraxChampionsTorCzas.Czas(kolejnosc[i].czas) + (kolejnosc[i].szac ? "*" : "")));   // * still riding: its own pace
        if (puchar)
        {
            for (int i = 0; i < kolejnosc.Count; i++) { var zz = ZawodnikDla(kolejnosc[i]); zz.punkty += i < PunktyMXGP.Length ? PunktyMXGP[i] : 0; zz.miejsca.Add(i + 1); }
            ukonczone++; WersjaPucharu++; stronaPucharu = false; odswiez = 4f;
            if (ukonczone >= wyscigowPucharu)
            {
                var k = Klasyfikuj(); zwyciezca = k[0].imie; wygrales = zwyciezca == R.Gracz;
                R.Dodaj(R.Rodzaj.Puchar, k[0].punkty, k[0].imie, k[0].klad);
                if (wygrales) R.DodajPuchar();
                Debug.Log("BRAX_WYSCIG puchar zwyciezca=" + zwyciezca + " punkty=" + k[0].punkty + " twoje_puchary=" + R.Puchary);
            }
        }
        faza = Faza.Wyniki; fazaOd = Time.time;
        Tablica();
        Debug.Log("BRAX_WYSCIG koniec miejsce=" + wynikMiejsce + "/" + lista.Count + " czas=" + ja.czas.ToString("F1") + " rekord=" + wynikRekord + " top10=" + wynikTop + (puchar ? " puchar=" + ukonczone + "/" + wyscigowPucharu : ""));
    }

    // ---- the cup's standings
    Zawodnik ZawodnikDla(Uczestnik u)
    {
        foreach (var z in tabela) if (z.imie == u.imie) return z;
        var n = new Zawodnik { imie = u.imie, klad = R.Kolor(u.klad) }; tabela.Add(n); return n;
    }
    static int Ostatnie(Zawodnik z) => z.miejsca.Count > 0 ? z.miejsca[z.miejsca.Count - 1] : 99;
    /// <summary>The cup's order: the points, a tie to the better place in the last race.</summary>
    List<Zawodnik> Klasyfikuj()
    {
        var k = new List<Zawodnik>(tabela);
        k.Sort((a, b) => a.punkty != b.punkty ? b.punkty.CompareTo(a.punkty) : Ostatnie(a).CompareTo(Ostatnie(b)));
        return k;
    }
    static string Miejsca(Zawodnik z) => string.Join("-", z.miejsca);
    /// <summary>The cup's standings for the TOP 10 board: the place, the points, who, his places race by race.</summary>
    public void Klasyfikacja(List<string[]> w)
    {
        var k = Klasyfikuj();
        for (int i = 0; i < k.Count; i++) w.Add(new[] { (i + 1) + ".", k[i].punkty + " PKT", k[i].imie, Miejsca(k[i]), "" });
    }

    // ---- the podium
    void Podium()
    {
        naPodium.Clear();
        var kolej = new List<Uczestnik>();
        if (puchar) { foreach (var z in Klasyfikuj()) { var u = lista.Find(x => x.imie == z.imie); if (u != null) kolej.Add(u); } }
        else { kolej.AddRange(lista); kolej.Sort((a, b) => a.czas.CompareTo(b.czas)); }
        bool jaJedzie = ja != null && ja.klad && BraxChampionsKlad.Jedzie == ja.klad;
        for (int i = 0; i < kolej.Count; i++)
        {
            var z = kolej[i]; bool na = i < 3 && i < podium.Length;
            if (z.rywal)
            {
                if (na) { z.rywal.Aktywny = false; z.klad.Sam(false); z.klad.Ustaw(podium[i], podiumObrot, Vector3.zero); }
                else z.rywal.Parkuj();
            }
            else if (z == ja && jaJedzie)
            {
                z.klad.Trzymaj(true);
                z.klad.Ustaw(na ? podium[i] : podiumWidz, na ? podiumObrot : podiumWidzObrot, Vector3.zero);
            }
            if (!na) continue;
            naPodium.Add(z);
            if (i < puchary.Length && puchary[i] && i < pucharyNaPodium.Length) puchary[i].SetPositionAndRotation(pucharyNaPodium[i], podiumObrot);
        }
        faza = Faza.Podium; fazaOd = Time.time;
        Tablica();
        Debug.Log("BRAX_WYSCIG podium " + string.Join(", ", naPodium.ConvertAll(z => z.imie)) + (puchar ? " puchar=" + zwyciezca : ""));
    }

    void Przerwij() { Pokaz(puchar ? "PUCHAR PRZERWANY" : "WYŚCIG PRZERWANY", 3f); Debug.Log("BRAX_WYSCIG przerwany"); Sprzatnij(); }

    void Sprzatnij()
    {
        foreach (var r in rywale) if (r) r.Parkuj();
        if (ja != null && ja.klad && BraxChampionsKlad.Jedzie == ja.klad) ja.klad.Trzymaj(false);
        for (int i = 0; i < puchary.Length && i < stolPoz.Length; i++) if (puchary[i]) puchary[i].SetPositionAndRotation(stolPoz[i], stolObr[i]);
        lista.Clear(); kwady.Clear(); naPodium.Clear(); ja = null;
        bool bylPuchar = puchar; puchar = false; if (bylPuchar) WersjaPucharu++;
        faza = Faza.Wolny; fazaOd = Time.time; Belki(0f);
        Tablica();
    }

    void Belki(float gora) { foreach (var b in belki) if (b) b.localRotation = Quaternion.Slerp(belkaDol, belkaGora, gora); }
    void Pokaz(string t, float s) { komunikat = t; komunikatDo = Time.time + s; }

    public static string Wiersz(int miejsce, string imie, string czas)
    {
        string n = imie.Length > 8 ? imie.Substring(0, 8) : imie.PadRight(8);
        return miejsce + ". " + n + " " + czas;
    }
    string Wynik(Uczestnik z)
    {
        if (!puchar) return BraxChampionsTorCzas.Czas(z.czas);
        foreach (var t in tabela) if (t.imie == z.imie) return t.punkty + " PKT";
        return "";
    }

    // ---------------------------------------------------------------- the LED board (the region's LED font, BraxChampionsPiksele)
    public const int TW = 192, TH = 96;
    Texture2D tex; Color32[] px;
    static readonly Color32 Czern = new Color32(4, 4, 6, 255), Zloto = new Color32(255, 196, 48, 255), Bialy = new Color32(240, 244, 255, 255),
        Czerwony = new Color32(255, 52, 40, 255), Zielony = new Color32(60, 255, 110, 255);
    void Tablica()
    {
        if (!tablica) return;
        if (!tex)
        {
            // its mipmaps: the LEDs' squares near (point filtering), no shimmer from the grandstand
            tex = new Texture2D(TW, TH, TextureFormat.RGBA32, true) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, anisoLevel = 2, name = "tor kładów tablica LED" };
            px = new Color32[TW * TH];
            var mat = new Material(tablica.sharedMaterial) { name = "tor kładów tablica LED" };
            mat.SetTexture("_MainTex", tex); if (mat.HasProperty("_EmissionMap")) mat.SetTexture("_EmissionMap", tex);
            tablica.sharedMaterial = mat;
        }
        var w = new List<string>(); var kol = new List<Color32>();
        string tytul = puchar ? "WYŚCIG " + Mathf.Min(ukonczone + (faza == Faza.Wyniki || faza == Faza.Podium ? 0 : 1), wyscigowPucharu) + "/" + wyscigowPucharu : "WYŚCIG BRAXHORN";
        string dol = puchar ? "PUNKTY " + PunktyMXGP[0] + "-" + PunktyMXGP[1] + "-" + PunktyMXGP[2] + "-" + PunktyMXGP[3] + "-" + PunktyMXGP[4]
                            : "TWÓJ REKORD " + (Rekord > 0f ? BraxChampionsTorCzas.Czas(Rekord) : "-");
        if (faza == Faza.Jazda || faza == Faza.Odliczanie)
        {
            var kolejnosc = new List<Uczestnik>(lista); kolejnosc.Sort((a, b) => Przed(a, b) ? -1 : Przed(b, a) ? 1 : 0);
            for (int i = 0; i < kolejnosc.Count; i++)
            {
                var z = kolejnosc[i];
                w.Add(Wiersz(i + 1, z.imie, z.meta ? "META" : "OKR " + Okrazenie(z) + "/" + Okr)); kol.Add(z == ja ? Zielony : Bialy);
            }
        }
        else if (faza == Faza.Podium)
        {
            tytul = puchar ? "PUCHAR BRAXHORN" : "PODIUM";
            for (int i = 0; i < naPodium.Count; i++) { w.Add(Wiersz(i + 1, naPodium[i].imie, Wynik(naPodium[i]))); kol.Add(naPodium[i] == ja ? Zielony : Bialy); }
            if (puchar) { w.Add(""); kol.Add(Bialy); w.Add("ZWYCIĘZCA: " + zwyciezca); kol.Add(Zloto); }
        }
        else if (faza == Faza.Wyniki && puchar && stronaPucharu)
        {
            tytul = "PUCHAR · PO " + ukonczone + "/" + wyscigowPucharu;
            var k = Klasyfikuj();
            for (int i = 0; i < k.Count; i++) { w.Add(Wiersz(i + 1, k[i].imie, k[i].punkty + " PKT " + Miejsca(k[i]))); kol.Add(k[i].imie == R.Gracz ? Zielony : Bialy); }
        }
        else if (ostatnie.Count > 0) { foreach (var s in ostatnie) { w.Add(s); kol.Add(s.Contains(" " + R.Gracz + " ") ? Zielony : Bialy); } }
        else
        {
            w.Add(okrazen + " OKRĄŻENIA · " + rywale.Length + " RYWALI"); w.Add("PUCHAR BRAXHORN: " + wyscigowPucharu + " WYŚCIGI"); w.Add("PODJEDŹ KŁADEM"); w.Add("POD MASZYNĘ STARTOWĄ");
            for (int i = 0; i < 4; i++) kol.Add(Bialy);
        }
        while (w.Count < 5) { w.Add(""); kol.Add(Bialy); }
        if (w.Count < 6) { w.Add(dol); kol.Add(Zloto); }
        Maluj(px, tytul, w, kol);
        tex.SetPixels32(px); tex.Apply(true);
    }
    /// <summary>The board's picture: the title in gold over a red rule, six rows under it (the generator's test paints it too).</summary>
    public static void Maluj(Color32[] px, string tytul, IList<string> wiersze, IList<Color32> kolory)
    {
        BraxChampionsPiksele.Wypelnij(px, Czern);
        BraxChampionsPiksele.Srodek(px, TW, TH, tytul, TW / 2, TH - 18, 2, Zloto);
        BraxChampionsPiksele.Prost(px, TW, TH, 4, TH - 23, TW - 4, TH - 22, Czerwony);
        for (int i = 0; i < wiersze.Count && i < 6; i++)
            BraxChampionsPiksele.Tekst(px, TW, TH, wiersze[i], 6, TH - 34 - i * 11, 1, i < kolory.Count ? kolory[i] : Bialy);
    }

    // ---------------------------------------------------------------- the HUD
    float Jedn => Mathf.Max(1f, Screen.height / 720f);
    Rect PrzyciskStartu() { float u = Jedn; return new Rect(Screen.width * .5f - 230f * u, 18f * u, 460f * u, 84f * u); }
    Rect PrzyciskPucharu() { float u = Jedn; return new Rect(Screen.width * .5f - 230f * u, 110f * u, 460f * u, 84f * u); }
    static bool Dotkniety(Rect r)
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            var t = Input.GetTouch(i);
            if (t.phase == TouchPhase.Began && r.Contains(new Vector2(t.position.x, Screen.height - t.position.y))) return true;
        }
        return Input.touchCount == 0 && Input.GetMouseButtonDown(0) && r.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y));
    }

    static GUIStyle duzy, sredni, maly; static Texture2D biel;
    void OnGUI()
    {
        bool mozna = faza == Faza.Wolny && MozeStart();
        if (faza == Faza.Wolny && !mozna && Time.time >= komunikatDo) return;
        GUI.depth = -26;
        float u = Jedn, W = Screen.width, H = Screen.height;
        if (!biel) { biel = new Texture2D(1, 1); biel.SetPixel(0, 0, Color.white); biel.Apply(); }
        if (duzy == null)
        {
            duzy = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            sredni = new GUIStyle(duzy); maly = new GUIStyle(duzy);
        }
        duzy.fontSize = Mathf.RoundToInt(120 * u); sredni.fontSize = Mathf.RoundToInt(28 * u); maly.fontSize = Mathf.RoundToInt(21 * u);
        var zloto = new Color(.93f, .76f, .36f); var szklo = new Color(0f, 0f, 0f, .55f); var zielony = new Color(.4f, 1f, .55f);
        float t = Time.time - fazaOd;
        string nrPucharu = "PUCHAR BRAXHORN · WYŚCIG " + Mathf.Min(ukonczone + 1, wyscigowPucharu) + "/" + wyscigowPucharu;
        switch (faza)
        {
            case Faza.Wolny:
                if (mozna)
                {
                    var r = PrzyciskStartu();
                    Pole(r, new Color(.85f, .5f, .05f, .9f));
                    Tekst(r, "WYŚCIG · " + okrazen + " OKRĄŻENIA · " + rywale.Length + " RYWALI", sredni, Color.white);
                    var rp = PrzyciskPucharu();
                    Pole(rp, new Color(.05f, .05f, .06f, .88f));
                    Tekst(rp, "PUCHAR BRAXHORN · " + wyscigowPucharu + " WYŚCIGI", sredni, zloto);
                }
                break;
            case Faza.Odliczanie:
            {
                string c = t < .6f ? "" : t < 1.6f ? "3" : t < 2.6f ? "2" : "1";
                if (c.Length > 0) Tekst(new Rect(W * .5f - 150f * u, H * .22f, 300f * u, 180f * u), c, duzy, zloto);
                Tekst(new Rect(W * .5f - 250f * u, 18f * u, 500f * u, 60f * u), "UWAGA NA MASZYNĘ STARTOWĄ", sredni, Color.white);
                if (puchar) Tekst(new Rect(W * .5f - 300f * u, 70f * u, 600f * u, 40f * u), nrPucharu, maly, zloto);
                break;
            }
            case Faza.Jazda:
            {
                var r = new Rect(W * .5f - 250f * u, 18f * u, 500f * u, (puchar ? 108f : 76f) * u);
                Pole(r, szklo);
                Tekst(new Rect(r.x, r.y, r.width, 44f * u), "POZYCJA " + Miejsce(ja) + "/" + lista.Count + "   OKR. " + Okrazenie(ja) + "/" + Okr, sredni, zloto);
                Tekst(new Rect(r.x, r.y + 40f * u, r.width, 34f * u), BraxChampionsTorCzas.Czas(Time.time - start), maly, Color.white);
                if (puchar) Tekst(new Rect(r.x, r.y + 72f * u, r.width, 34f * u), nrPucharu, maly, zloto);
                break;
            }
            case Faza.Wyniki:
            {
                var k = puchar ? Klasyfikuj() : null;
                int wierszy = puchar ? k.Count : ostatnie.Count;
                var r = new Rect(W * .5f - 280f * u, H * .14f, 560f * u, (130f + 34f * wierszy) * u);
                Pole(r, new Color(0f, 0f, 0f, .7f));
                Tekst(new Rect(r.x, r.y + 8f * u, r.width, 44f * u), "META! " + wynikMiejsce + ". MIEJSCE · " + BraxChampionsTorCzas.Czas(wynikCzas), sredni, wynikMiejsce == 1 ? zloto : Color.white);
                string pod = puchar ? "PUCHAR · PO " + ukonczone + "/" + wyscigowPucharu + " WYŚCIGACH"
                           : wynikRekord ? "NOWY REKORD WYŚCIGU!" : wynikTop > 0 ? "TABLICA TOP 10: " + wynikTop + ". MIEJSCE" : "";
                if (pod.Length > 0) Tekst(new Rect(r.x, r.y + 48f * u, r.width, 34f * u), pod, maly, zloto);
                for (int i = 0; i < wierszy; i++)
                {
                    string s = puchar ? Wiersz(i + 1, k[i].imie, k[i].punkty + " PKT   " + Miejsca(k[i])) : ostatnie[i];
                    bool moj = puchar ? k[i].imie == R.Gracz : s.Contains(" " + R.Gracz + " ");
                    Tekst(new Rect(r.x, r.y + (86f + 34f * i) * u, r.width, 34f * u), s, maly, moj ? zielony : Color.white);
                }
                float zostalo = Mathf.Max(0f, (puchar && ukonczone < wyscigowPucharu ? CzasPrzerwy : CzasWynikow) - t);
                string dalej = puchar && ukonczone < wyscigowPucharu ? "WYŚCIG " + (ukonczone + 1) + "/" + wyscigowPucharu + " ZA " + Mathf.CeilToInt(zostalo) + " S"
                             : podium.Length >= 3 ? "PODIUM ZA " + Mathf.CeilToInt(zostalo) + " S" : "";
                if (dalej.Length > 0) Tekst(new Rect(r.x, r.y + (90f + 34f * wierszy) * u, r.width, 34f * u), dalej, maly, zloto);
                break;
            }
            case Faza.Podium:
            {
                var r = new Rect(W * .5f - 280f * u, 18f * u, 560f * u, (64f + 34f * naPodium.Count + (wygrales ? 36f : 0f)) * u);
                Pole(r, new Color(0f, 0f, 0f, .6f));
                Tekst(new Rect(r.x, r.y + 6f * u, r.width, 44f * u), puchar ? "PUCHAR BRAXHORN · " + zwyciezca : "PODIUM", sredni, zloto);
                for (int i = 0; i < naPodium.Count; i++)
                    Tekst(new Rect(r.x, r.y + (50f + 34f * i) * u, r.width, 34f * u), Wiersz(i + 1, naPodium[i].imie, Wynik(naPodium[i])), maly, naPodium[i] == ja ? zielony : Color.white);
                if (wygrales) Tekst(new Rect(r.x, r.y + (54f + 34f * naPodium.Count) * u, r.width, 34f * u), "WYGRAŁEŚ PUCHAR! TWOJE PUCHARY: " + R.Puchary, maly, zloto);
                break;
            }
        }
        if (Time.time < komunikatDo && komunikat.Length > 0)
        {
            var r = new Rect(W * .5f - 260f * u, H * .5f, 520f * u, 80f * u);
            Pole(r, new Color(0f, 0f, 0f, .6f)); Tekst(r, komunikat, sredni, zloto);
        }
    }
    static void Pole(Rect r, Color c) { var o = GUI.color; GUI.color = c; GUI.DrawTexture(r, biel); GUI.color = o; }
    static void Tekst(Rect r, string s, GUIStyle st, Color c) { var o = GUI.color; GUI.color = c; GUI.Label(r, s, st); GUI.color = o; }
}
