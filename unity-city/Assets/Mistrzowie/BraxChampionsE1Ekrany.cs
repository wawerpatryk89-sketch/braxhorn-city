using UnityEngine;

// Owner 10.11 21:22: "monitorki mają być monitorkami, mają wyświetlać ... wszystko ma być funkcjonalne". The screens of E1
// Centrum Rozrywki that are not lane scorecards (those belong to BraxChampionsKregle) show live state:
//   - status board (reception screen wall, the two office monitors): every lane and table, free / in play / score, the time,
//   - ticket kiosks (hall): free lanes and tables, the first free lane,
//   - arcade cabinets: their attract screens (title, marching invaders, blinking START), five titles,
//   - air hockey tables: the score.
// One texture per kind (shared by its screens), repainted once a second (arcades 12 frames a second) while the player is
// within 90 m; asleep otherwise. BraxChampionsE1 paints the same pictures into the screens' starting textures.
public sealed class BraxChampionsE1Ekrany : MonoBehaviour
{
    public BraxChampionsKregle kregle;
    public BraxChampionsBilard bilard;
    public Renderer[] tablice = new Renderer[0];
    public Renderer[] kioski = new Renderer[0];
    public Renderer[] automaty = new Renderer[0];     // automat i shows title i % 5
    public Renderer[] cymbergaj = new Renderer[0];

    sealed class Ekran { public Texture2D tex; public Color32[] px; }
    Ekran tablica, kiosk, wynik; Ekran[] gry = new Ekran[5];
    float nastepny, klatkaOd, sprawdz; int klatka; bool blisko;

    void Start()
    {
        tablica = Nowy(BraxChampionsPiksele.TablicaW, BraxChampionsPiksele.TablicaH, tablice, "E1 tablica stanu");
        kiosk = Nowy(BraxChampionsPiksele.KioskW, BraxChampionsPiksele.KioskH, kioski, "E1 kiosk");
        wynik = Nowy(BraxChampionsPiksele.CymbW, BraxChampionsPiksele.CymbH, cymbergaj, "E1 cymbergaj wynik");
        for (int v = 0; v < gry.Length; v++)
        {
            var moje = new System.Collections.Generic.List<Renderer>();
            for (int i = v; i < automaty.Length; i += gry.Length) moje.Add(automaty[i]);
            gry[v] = Nowy(BraxChampionsPiksele.AutomatW, BraxChampionsPiksele.AutomatH, moje.ToArray(), "E1 automat " + (v + 1));
        }
        Odswiez(); Automaty();
        if (wynik != null) { BraxChampionsPiksele.Cymbergaj(wynik.px, 0, 0); Zapisz(wynik); }
    }

    static Ekran Nowy(int w, int h, Renderer[] na, string nazwa)
    {
        if (na == null || na.Length == 0) return null;
        Material baza = null; foreach (var r in na) if (r) { baza = r.sharedMaterial; break; }
        if (!baza) return null;
        var e = new Ekran { tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = nazwa, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp }, px = new Color32[w * h] };
        var m = new Material(baza) { name = nazwa };
        m.SetTexture("_MainTex", e.tex); m.SetTexture("_EmissionMap", e.tex);
        m.SetColor("_Color", Color.white); m.SetColor("_EmissionColor", new Color(1.05f, 1.05f, 1.1f));
        foreach (var r in na) if (r) r.sharedMaterial = m;
        return e;
    }
    static void Zapisz(Ekran e) { e.tex.SetPixels32(e.px); e.tex.Apply(false); }

    void Update()
    {
        if (Time.unscaledTime >= sprawdz)
        {
            sprawdz = Time.unscaledTime + .5f;
            var g = BraxChampionsGry.Gracz;
            blisko = g && (g.transform.position - transform.position).sqrMagnitude < 90f * 90f;
        }
        if (!blisko) return;
        if (Time.unscaledTime >= nastepny) { nastepny = Time.unscaledTime + 1f; Odswiez(); }
        if (Time.unscaledTime >= klatkaOd) { klatkaOd = Time.unscaledTime + 1f / 12f; klatka++; Automaty(); }
    }

    void Odswiez()
    {
        int nt = kregle ? kregle.Tory : 24, ns = bilard ? bilard.Stoly : 18;
        var tory = new int[nt]; var koniec = new bool[nt]; var stoly = new int[ns];
        for (int i = 0; i < nt; i++) { tory[i] = kregle ? kregle.Wynik(i) : -1; koniec[i] = kregle && kregle.Skonczona(i); }
        for (int i = 0; i < ns; i++) stoly[i] = bilard ? bilard.Wbite(i) : -1;
        var t = System.DateTime.Now;
        if (tablica != null) { BraxChampionsPiksele.Tablica(tablica.px, tory, koniec, stoly, t.ToString("HH:mm")); Zapisz(tablica); }
        if (kiosk != null) { BraxChampionsPiksele.Kiosk(kiosk.px, tory, stoly); Zapisz(kiosk); }
    }

    void Automaty()
    {
        for (int v = 0; v < gry.Length; v++)
            if (gry[v] != null) { BraxChampionsPiksele.Automat(gry[v].px, v, klatka + v * 7); Zapisz(gry[v]); }
    }
}
