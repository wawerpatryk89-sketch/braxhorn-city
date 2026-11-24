using System.Collections.Generic;
using UnityEngine;

// THE QUAD RACE OF THE BRAXHORN TRACK (owner 24.11.2026 12:57, his pick "Wyścig z rywalami": "4 kłady AI, odliczanie, bramka
// opada, 3 okrążenia, pozycje, tablica LED w padoku").
// Riding a quad behind the start gate the player gets WYŚCIG (a button at the top, T on a desktop): his quad and the four
// rivals' (BraxChampionsKladRywal: the riders' park beside the paddock) take the gate's five places, the bars come up, 3 - 2 - 1,
// the bars drop and everybody rides. Every rider's place on the race's line (the track's centre every 2 m from the gate) is
// followed from where it was, never jumping to a section beside it - a cut across the infield gains nothing; the positions
// are the distances ridden. Three laps: the gate to the finish line, then three times round. When the player is over the
// line the order is set (a rival still riding: its time from its own pace), the results come up and stay on the LED board
// beside the start straight with the best race time (on the phone: PlayerPrefs, a per-player convenience); after them the
// rivals are back in their park. Getting off the quad stops the race.
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
    public int okrazen = 3;
    public Vector3[] sloty = new Vector3[5];     // the gate's five places (the quads' roots), world
    public Quaternion kierunekStartu = Quaternion.identity;
    public Transform[] belki = new Transform[0]; // the drop bars' hinges
    public Quaternion belkaGora = Quaternion.identity, belkaDol = Quaternion.identity;
    public BraxChampionsKladRywal[] rywale = new BraxChampionsKladRywal[0];
    public Renderer tablica;                     // the LED board's screen
    public Vector3 strefaStartu;                 // world: behind the gate, where WYŚCIG is offered
    public float strefaPromien = 24f;            // nominal metres
    public const int SlotGracza = 2;             // the middle place

    // ---------------------------------------------------------------- state
    enum Faza { Wolny, Odliczanie, Jazda, Wyniki }
    Faza faza = Faza.Wolny; float fazaOd, start, odswiez;
    sealed class Uczestnik { public BraxChampionsKlad klad; public BraxChampionsKladRywal rywal; public string imie; public int idx; public float dystans, czas; public bool meta, szac; }
    readonly List<Uczestnik> lista = new List<Uczestnik>();
    readonly List<BraxChampionsKlad> kwady = new List<BraxChampionsKlad>();
    Uczestnik ja;
    int wynikMiejsce; bool wynikRekord; float wynikCzas;
    readonly List<string> ostatnie = new List<string>();
    string komunikat = ""; float komunikatDo;
    public List<BraxChampionsKlad> Kwady() => kwady;
    public float Dystans => sMety + okrazen * dlugosc;
    public bool Trwa => faza == Faza.Odliczanie || faza == Faza.Jazda;

    const string KluczRekordu = "BRAX_TOR_WYSCIG_REKORD";
    float rekord = -1f;
    public float Rekord { get { if (rekord < 0f) { try { rekord = PlayerPrefs.GetFloat(KluczRekordu, 0f); } catch { rekord = 0f; } } return rekord; } }

    void Start()
    {
        foreach (var r in rywale) if (r) { r.wyscig = this; r.Parkuj(); }
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
                if (MozeStart() && (Input.GetKeyDown(KeyCode.T) || Dotkniety(PrzyciskStartu()))) Zacznij();
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
                if (t > 12f) Sprzatnij();
                break;
        }
    }

    void Zacznij()
    {
        var k = BraxChampionsKlad.Jedzie; if (!k) return;
        lista.Clear(); kwady.Clear();
        ja = new Uczestnik { klad = k, imie = "TY" }; lista.Add(ja); kwady.Add(k);
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
        faza = Faza.Odliczanie; fazaOd = Time.time;
        Debug.Log("BRAX_WYSCIG ustawienie rywali=" + (lista.Count - 1));
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
    int Okrazenie(Uczestnik z) => Mathf.Clamp(Mathf.FloorToInt((z.dystans - sMety) / Mathf.Max(1f, dlugosc)) + 1, 1, okrazen);

    void Koniec()
    {
        float teraz = Time.time - start;
        // a rival still riding: over the line at its own pace so far
        foreach (var z in lista) if (!z.meta) { float vs = z.dystans / Mathf.Max(1f, teraz); z.czas = teraz + (Dystans - z.dystans) / Mathf.Max(3f, vs); z.szac = true; }
        var kolejnosc = new List<Uczestnik>(lista); kolejnosc.Sort((a, b) => a.czas.CompareTo(b.czas));
        wynikMiejsce = kolejnosc.IndexOf(ja) + 1; wynikCzas = ja.czas;
        wynikRekord = Rekord <= 0f || ja.czas < Rekord;
        if (wynikRekord) { rekord = ja.czas; try { PlayerPrefs.SetFloat(KluczRekordu, ja.czas); PlayerPrefs.Save(); } catch { } }
        ostatnie.Clear();
        for (int i = 0; i < kolejnosc.Count; i++) ostatnie.Add(Wiersz(i + 1, kolejnosc[i].imie, BraxChampionsTorCzas.Czas(kolejnosc[i].czas) + (kolejnosc[i].szac ? "*" : "")));   // * still riding: its own pace
        faza = Faza.Wyniki; fazaOd = Time.time;
        Tablica();
        Debug.Log("BRAX_WYSCIG koniec miejsce=" + wynikMiejsce + "/" + lista.Count + " czas=" + ja.czas.ToString("F1") + " rekord=" + wynikRekord);
    }

    void Przerwij() { Pokaz("WYŚCIG PRZERWANY", 3f); Debug.Log("BRAX_WYSCIG przerwany"); Sprzatnij(); }

    void Sprzatnij()
    {
        foreach (var r in rywale) if (r) r.Parkuj();
        if (ja != null && ja.klad && BraxChampionsKlad.Jedzie == ja.klad) ja.klad.Trzymaj(false);
        lista.Clear(); kwady.Clear(); ja = null;
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
            tex = new Texture2D(TW, TH, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "tor kładów tablica LED" };
            px = new Color32[TW * TH];
            var mat = new Material(tablica.sharedMaterial) { name = "tor kładów tablica LED" };
            mat.SetTexture("_MainTex", tex); if (mat.HasProperty("_EmissionMap")) mat.SetTexture("_EmissionMap", tex);
            tablica.sharedMaterial = mat;
        }
        var w = new List<string>(); var kol = new List<Color32>();
        string rek = "REKORD " + (Rekord > 0f ? BraxChampionsTorCzas.Czas(Rekord) : "-");
        if (faza == Faza.Jazda || faza == Faza.Odliczanie)
        {
            var kolejnosc = new List<Uczestnik>(lista); kolejnosc.Sort((a, b) => Przed(a, b) ? -1 : Przed(b, a) ? 1 : 0);
            for (int i = 0; i < kolejnosc.Count; i++)
            {
                var z = kolejnosc[i];
                w.Add(Wiersz(i + 1, z.imie, z.meta ? "META" : "OKR " + Okrazenie(z) + "/" + okrazen)); kol.Add(z == ja ? Zielony : Bialy);
            }
        }
        else if (ostatnie.Count > 0) { foreach (var s in ostatnie) { w.Add(s); kol.Add(s.Contains(" TY ") ? Zielony : Bialy); } }
        else { w.Add(okrazen + " OKRĄŻENIA · " + rywale.Length + " RYWALI"); w.Add("PODJEDŹ KŁADEM"); w.Add("POD MASZYNĘ STARTOWĄ"); w.Add(""); kol.Add(Bialy); kol.Add(Bialy); kol.Add(Bialy); kol.Add(Bialy); }
        while (w.Count < 5) { w.Add(""); kol.Add(Bialy); }
        w.Add(rek); kol.Add(Zloto);
        Maluj(px, "WYŚCIG BRAXHORN", w, kol);
        tex.SetPixels32(px); tex.Apply(false);
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
        var zloto = new Color(.93f, .76f, .36f); var szklo = new Color(0f, 0f, 0f, .55f);
        float t = Time.time - fazaOd;
        switch (faza)
        {
            case Faza.Wolny:
                if (mozna)
                {
                    var r = PrzyciskStartu();
                    Pole(r, new Color(.85f, .5f, .05f, .9f));
                    Tekst(r, "WYŚCIG · " + okrazen + " OKRĄŻENIA · " + rywale.Length + " RYWALI", sredni, Color.white);
                }
                break;
            case Faza.Odliczanie:
            {
                string c = t < .6f ? "" : t < 1.6f ? "3" : t < 2.6f ? "2" : "1";
                if (c.Length > 0) Tekst(new Rect(W * .5f - 150f * u, H * .22f, 300f * u, 180f * u), c, duzy, zloto);
                Tekst(new Rect(W * .5f - 250f * u, 18f * u, 500f * u, 60f * u), "UWAGA NA MASZYNĘ STARTOWĄ", sredni, Color.white);
                break;
            }
            case Faza.Jazda:
            {
                var r = new Rect(W * .5f - 250f * u, 18f * u, 500f * u, 76f * u);
                Pole(r, szklo);
                Tekst(new Rect(r.x, r.y, r.width, 44f * u), "POZYCJA " + Miejsce(ja) + "/" + lista.Count + "   OKR. " + Okrazenie(ja) + "/" + okrazen, sredni, zloto);
                Tekst(new Rect(r.x, r.y + 40f * u, r.width, 34f * u), BraxChampionsTorCzas.Czas(Time.time - start), maly, Color.white);
                break;
            }
            case Faza.Wyniki:
            {
                var r = new Rect(W * .5f - 280f * u, H * .16f, 560f * u, (96f + 34f * ostatnie.Count) * u);
                Pole(r, new Color(0f, 0f, 0f, .7f));
                Tekst(new Rect(r.x, r.y + 8f * u, r.width, 44f * u), "META! " + wynikMiejsce + ". MIEJSCE · " + BraxChampionsTorCzas.Czas(wynikCzas), sredni, wynikMiejsce == 1 ? zloto : Color.white);
                if (wynikRekord) Tekst(new Rect(r.x, r.y + 48f * u, r.width, 34f * u), "NOWY REKORD WYŚCIGU!", maly, zloto);
                for (int i = 0; i < ostatnie.Count; i++)
                    Tekst(new Rect(r.x, r.y + (86f + 34f * i) * u, r.width, 34f * u), ostatnie[i], maly, ostatnie[i].Contains(" TY ") ? new Color(.4f, 1f, .55f) : Color.white);
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
