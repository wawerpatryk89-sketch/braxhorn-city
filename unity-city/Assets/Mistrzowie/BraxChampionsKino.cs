using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Video;

// G1 KINO MULTIPLEX - the cinema PLAYS (owner 11.11 16:33: "kino po prostu dostosuj, żeby było funkcjonalne, tak? Żeby
// leciały nasze filmy. I tam tą całą recepcję kina, żeby można było ... wybór tych repertuarów, jakie lecą. Tylko ci potem
// dostarczę zdjęcia, które wstawimy, i filmy"). One component for the whole cinema (BraxChampionsG1Kino writes it):
//   - the repertoire is a FILE, not the scene: StreamingAssets/Mistrzowie/Kino/repertuar.json lists the owner's films
//     (title, the MP4 in the same folder or an http(s) address, the poster image, minutes, age rating, a line of text).
//     Adding a film = the MP4 + the poster into that folder and one entry in the JSON - the cinema is not rebuilt;
//   - hall h shows film h % n (8 halls); the showtimes come from the phone's clock: from 10:00 every film + 15 min of
//     cleaning, the last one starting before 23:30;
//   - the lobby's repertoire boards (LED, the region's pixel font) list the films: hall, the next two showtimes, length,
//     rating; the poster light boxes show the owner's posters (or a painted WKRÓTCE card); the screens over the hall
//     doors show SALA n, the film, the next showtime;
//   - the reception: AKCJA at the film kiosk = the next film (highlighted on the boards), AKCJA at the till = a ticket
//     for that film's hall with a seat (on the boards: BILET · SALA 3 · RZĄD 7 · MIEJSCE 12), again = the other film;
//     the films' posters (landscape banners) on the screens over the tills;
//   - free shows for now (owner 11.11 19:09, zaDarmo): walking into a hall or AKCJA in it starts its film, no ticket needed;
//   - walking into the hall with the ticket: the hall goes dark (3 s - its surfaces' ambient light, the region's interiors are
//     lit by their materials, plus any lamps of the hall), the film starts on the screen (VideoPlayer into a
//     RenderTexture, the sound in 3D from behind the screen); AKCJA in the hall = pause / resume; walking out pauses it,
//     coming back resumes it; at the end the lights come up and the screen says KONIEC SEANSU;
//   - no films yet (or a file missing): the screens and boards say WKRÓTCE / BRAK PLIKU - nothing breaks.
// One VideoPlayer for the whole cinema (only the hall with the player plays); nothing runs further than 150 m away.
public sealed class BraxChampionsKino : MonoBehaviour, IBraxInterakcja
{
    [Serializable] public sealed class Film { public string tytul = ""; public string plik = ""; public string url = ""; public string plakat = ""; public string baner = ""; public int minuty; public string wiek = ""; public string opis = ""; }
    [Serializable] sealed class Repertuar { public Film[] filmy = new Film[0]; }

    // ---- configuration (BraxChampionsG1Kino, world space)
    public Renderer[] ekrany = new Renderer[0];                       // hall screens (uv 0..1 = the picture), hall order
    public Vector3[] salaMin = new Vector3[0], salaMax = new Vector3[0];   // the halls' bounds
    public Vector3[] glosnik = new Vector3[0];                        // the sound's point per hall (behind the screen)
    public int[] rzedy = new int[0], miejsca = new int[0];           // rows and seats per row of every hall
    public Renderer[] widok = new Renderer[0]; public int[] widokSala = new int[0];   // the halls' surfaces (dimmed) and their hall
    public Light[] swiatla = new Light[0]; public int[] swiatlaSala = new int[0];   // optional lamps of the halls
    public Renderer[] fotele = new Renderer[0]; public int[] foteleSala = new int[0]; // the seats of each hall: drawn only in / at that hall
    public Renderer[] tablice = new Renderer[0];                      // the lobby's repertoire boards (all the same)
    public Renderer[] plakaty = new Renderer[0]; public int[] plakatFilm = new int[0];   // >= 0 lobby slot k (film k), < 0 hall door -(h+1)
    public Renderer[] drzwiEkrany = new Renderer[0];                  // the small screens over the hall doors, hall order
    public Vector3 kiosk, kasa; public float zasieg = 1.8f;
    // owner 11.11 19:09 ("żebym sobie wszedł do kina i mógł odpalić normalnie za darmo na razie te filmy"): free shows - the
    // till gives a free ticket for the chosen film, and in any hall AKCJA starts its film without one
    public bool zaDarmo = true;
    public string folder = "Mistrzowie/Kino", plikRepertuaru = "repertuar.json";

    const int TW = BraxChampionsKinoEkrany.TW, TH = BraxChampionsKinoEkrany.TH, EW = BraxChampionsKinoEkrany.EW, EH = BraxChampionsKinoEkrany.EH,
        DW = BraxChampionsKinoEkrany.DW, DH = BraxChampionsKinoEkrany.DH, PW = BraxChampionsKinoEkrany.PW, PH = BraxChampionsKinoEkrany.PH;
    static readonly Color32 Ciemny = BraxChampionsKinoEkrany.Ciemny, Pasek = BraxChampionsKinoEkrany.Pasek, Bursztyn = BraxChampionsKinoEkrany.Bursztyn,
        Wybor = BraxChampionsKinoEkrany.Wybor;
    float[][] widokBaza = new float[0][]; Color[][] emisjaBaza = new Color[0][]; MaterialPropertyBlock mpb;
    static readonly int Ambient = Shader.PropertyToID("_AmbientIntensity"), Emisja = Shader.PropertyToID("_EmissionColor");

    readonly List<Film> filmy = new List<Film>();
    bool wczytane;
    int wybor, bilet = -1, biletSala = -1, biletRzad, biletMiejsce;
    enum Stan { Nic, Laduje, Gra, Pauza, Koniec, Blad }
    Stan stan = Stan.Nic; int salaGra = -1; float jasnosc = 1f;
    float[] swiatlaBaza = new float[0];
    VideoPlayer vp; RenderTexture rt; AudioSource glos;
    sealed class Ekran { public Texture2D tex; public Color32[] px; public Material mat; }
    Ekran tab; Ekran[] ekr, drz; Material[] ekrBaza;
    readonly Dictionary<string, Texture2D> plakatTex = new Dictionary<string, Texture2D>();
    Transform gracz; float szukaj, nastepny; bool blisko = true; int salaGracza = -1;

    void OnEnable() { BraxInterakcje.Dodaj(this); }
    void OnDisable() { BraxInterakcje.Usun(this); }

    void Start()
    {
        swiatlaBaza = new float[swiatla.Length];
        for (int i = 0; i < swiatla.Length; i++) swiatlaBaza[i] = swiatla[i] ? swiatla[i].intensity : 1f;
        mpb = new MaterialPropertyBlock();
        widokBaza = new float[widok.Length][]; emisjaBaza = new Color[widok.Length][];
        for (int i = 0; i < widok.Length; i++)
        {
            var mats = widok[i] ? widok[i].sharedMaterials : new Material[0];
            widokBaza[i] = new float[mats.Length]; emisjaBaza[i] = new Color[mats.Length];
            for (int j = 0; j < mats.Length; j++)
            {
                widokBaza[i][j] = mats[j] && mats[j].HasProperty(Ambient) ? mats[j].GetFloat(Ambient) : -1f;
                // the lit fittings (wall lights, the starry ceiling, the LED lines) go dark with the film; the exit signs stay
                var e = mats[j] && mats[j].HasProperty(Emisja) ? mats[j].GetColor(Emisja) : Color.black;
                bool wyjscie = mats[j] && mats[j].name.ToLowerInvariant().Contains("exit");
                emisjaBaza[i][j] = !wyjscie && e.maxColorComponent > .02f ? e : new Color(0f, 0f, 0f, -1f);
            }
        }
        tab = Nowy(TW, TH, tablice, "G1 kino repertuar");
        ekr = new Ekran[ekrany.Length]; ekrBaza = new Material[ekrany.Length];
        for (int h = 0; h < ekrany.Length; h++) { ekr[h] = Nowy(EW, EH, new[] { ekrany[h] }, "G1 kino ekran sali " + (h + 1)); ekrBaza[h] = ekr[h] != null ? ekr[h].mat : null; }
        drz = new Ekran[drzwiEkrany.Length];
        for (int h = 0; h < drzwiEkrany.Length; h++) drz[h] = Nowy(DW, DH, new[] { drzwiEkrany[h] }, "G1 kino sala " + (h + 1) + " nad drzwiami");
        Maluj();
        StartCoroutine(Wczytaj());
    }

    static Ekran Nowy(int w, int h, Renderer[] na, string nazwa)
    {
        if (na == null || na.Length == 0) return null;
        Material baza = null; foreach (var r in na) if (r) { baza = r.sharedMaterial; break; }
        if (!baza) return null;
        var e = new Ekran { tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = nazwa, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp }, px = new Color32[w * h] };
        e.mat = new Material(baza) { name = nazwa };
        e.mat.SetTexture("_MainTex", e.tex); e.mat.SetTexture("_EmissionMap", e.tex);
        foreach (var r in na) if (r) r.sharedMaterial = e.mat;
        return e;
    }
    static void Zapisz(Ekran e) { if (e == null) return; e.tex.SetPixels32(e.px); e.tex.Apply(false); }

    // ---------------- the repertoire (StreamingAssets: a jar on Android, a folder elsewhere) ----------------
    string Sciezka(string plik)
    {
        string p = Application.streamingAssetsPath.TrimEnd('/') + "/" + folder.Trim('/') + "/" + plik;
        return p.Contains("://") ? p : "file://" + p;
    }
    IEnumerator Wczytaj()
    {
        using (var rq = UnityWebRequest.Get(Sciezka(plikRepertuaru)))
        {
            yield return rq.SendWebRequest();
            if (rq.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var r = JsonUtility.FromJson<Repertuar>(rq.downloadHandler.text);
                    if (r != null && r.filmy != null) foreach (var f in r.filmy) if (f != null && !string.IsNullOrEmpty(f.tytul)) filmy.Add(f);
                }
                catch (Exception e) { Debug.LogWarning("BRAXHORN_KINO repertuar.json DO POPRAWKI " + e.Message); }
            }
            else Debug.Log("BRAXHORN_KINO brak repertuaru (" + rq.error + ") - ekrany: WKRÓTCE");
        }
        wczytane = true;
        Debug.Log("BRAXHORN_KINO repertuar filmy=" + filmy.Count);
        Maluj();
        var obrazy = new List<string>();
        foreach (var f in filmy) { if (!string.IsNullOrEmpty(f.plakat)) obrazy.Add(f.plakat); if (!string.IsNullOrEmpty(f.baner)) obrazy.Add(f.baner); }
        foreach (var plik in obrazy)
        {
            if (plakatTex.ContainsKey(plik)) continue;
            string adres = plik.StartsWith("http") ? plik : Sciezka(plik);
            using (var rq = UnityWebRequest.Get(adres))
            {
                yield return rq.SendWebRequest();
                if (rq.result != UnityWebRequest.Result.Success) continue;
                var t = new Texture2D(2, 2, TextureFormat.RGBA32, true) { name = "plakat " + plik, wrapMode = TextureWrapMode.Clamp };
                if (t.LoadImage(rq.downloadHandler.data)) plakatTex[plik] = t; else Destroy(t);
            }
        }
        Plakaty();
    }

    int N => filmy.Count;
    int FilmSali(int h) => N > 0 ? h % N : -1;
    int SalaFilmu(int f) => f >= 0 && f < ekrany.Length ? f : -1;
    static string Tytul(Film f) => (f.tytul ?? "").ToUpperInvariant();
    int Minuty(Film f) => f.minuty > 0 ? f.minuty : 30;

    // the showtimes of a hall: from 10:00 every film + 15 min (rounded up to 5 min), the last start before 23:30
    List<DateTime> Seanse(int h, DateTime dzien)
    {
        var l = new List<DateTime>(); int f = FilmSali(h); if (f < 0) return l;
        int krok = Mathf.Max(30, Mathf.CeilToInt((Minuty(filmy[f]) + 15) / 5f) * 5);
        var t = dzien.Date.AddHours(10).AddMinutes((h * 10) % 30);
        while (t.TimeOfDay < new TimeSpan(23, 30, 0)) { l.Add(t); t = t.AddMinutes(krok); }
        return l;
    }
    List<DateTime> Nastepne(int h, int ile)
    {
        var now = DateTime.Now; var w = new List<DateTime>();
        foreach (var t in Seanse(h, now)) if (t > now.AddMinutes(-5)) { w.Add(t); if (w.Count == ile) return w; }
        foreach (var t in Seanse(h, now.AddDays(1))) { w.Add(t); if (w.Count == ile) break; }
        return w;
    }
    static string Hm(DateTime t) => BraxChampionsKinoEkrany.Hm(t);
    static void T(Color32[] px, int w, int h, string s, int x, int yTop, int sk, Color32 c) => BraxChampionsKinoEkrany.T(px, w, h, s, x, yTop, sk, c);
    static void S(Color32[] px, int w, int h, string s, int yTop, int sk, Color32 c) => BraxChampionsKinoEkrany.S(px, w, h, s, yTop, sk, c);
    static string Krotko(string s, int n) => BraxChampionsKinoEkrany.Krotko(s, n);

    // ---------------- painting ----------------
    void Maluj()
    {
        if (tab != null) { MalujRepertuar(tab.px); Zapisz(tab); }
        for (int h = 0; h < ekr.Length; h++) if (ekr[h] != null) { EkranSali(ekr[h].px, h); Zapisz(ekr[h]); }   // while the film runs the material shows the film, not this
        for (int h = 0; h < drz.Length; h++) if (drz[h] != null) { NadDrzwiami(drz[h].px, h); Zapisz(drz[h]); }
        Plakaty();
    }
    void MalujRepertuar(Color32[] px)
    {
        int w = TW, h = TH;
        if (!wczytane || N == 0) { BraxChampionsKinoEkrany.TablicaWkrotce(px, Hm(DateTime.Now)); return; }
        BraxChampionsPiksele.Wypelnij(px, Ciemny);
        BraxChampionsPiksele.Prost(px, w, h, 0, h - 16, w, h, Pasek);
        T(px, w, h, "BRAXHORN KINO · REPERTUAR", 4, 4, 1, BraxChampionsPiksele.Bialy);
        T(px, w, h, Hm(DateTime.Now), w - 34, 4, 1, BraxChampionsPiksele.Bialy);
        T(px, w, h, "SALA", 4, 20, 1, BraxChampionsPiksele.Cyjan); T(px, w, h, "FILM", 34, 20, 1, BraxChampionsPiksele.Cyjan);
        T(px, w, h, "SEANSE", 196, 20, 1, BraxChampionsPiksele.Cyjan); T(px, w, h, "MIN", 276, 20, 1, BraxChampionsPiksele.Cyjan);
        int wiersze = Mathf.Min(N, ekrany.Length, 8);
        for (int f = 0; f < wiersze; f++)
        {
            int y = 31 + f * 12, sala = SalaFilmu(f);
            if (f == wybor) BraxChampionsPiksele.Prost(px, w, h, 0, h - y - 10, w, h - y + 2, Wybor);
            var c = f == wybor ? BraxChampionsPiksele.Bialy : Bursztyn;
            var nx = Nastepne(sala, 2);
            T(px, w, h, (sala + 1).ToString(), 10, y, 1, c);
            T(px, w, h, Krotko(Tytul(filmy[f]), 26), 34, y, 1, c);
            T(px, w, h, (nx.Count > 0 ? Hm(nx[0]) : "") + (nx.Count > 1 ? " " + Hm(nx[1]) : ""), 196, y, 1, c);
            T(px, w, h, Minuty(filmy[f]) + (string.IsNullOrEmpty(filmy[f].wiek) ? "" : " " + filmy[f].wiek), 276, y, 1, c);
        }
        BraxChampionsPiksele.Prost(px, w, h, 0, 0, w, 14, new Color32(16, 16, 22, 255));
        string dol = bilet >= 0 ? "BILET: " + Krotko(Tytul(filmy[bilet]), 16) + " · SALA " + (biletSala + 1) + " · RZĄD " + biletRzad + " · M " + biletMiejsce
                                : zaDarmo ? "SEANSE GRATIS · BILET W KASIE ALBO AKCJA W SALI" : "WYBÓR FILMU I BILETY: RECEPCJA";
        T(px, w, h, dol, 4, h - 11, 1, bilet >= 0 ? BraxChampionsPiksele.Zielony : BraxChampionsPiksele.Cyjan);
    }
    void EkranSali(Color32[] px, int h)
    {
        int w = EW, hh = EH; BraxChampionsPiksele.Wypelnij(px, new Color32(18, 18, 22, 255));
        S(px, w, hh, "BRAXHORN KINO", 12, 1, BraxChampionsPiksele.Cyjan);
        S(px, w, hh, "SALA " + (h + 1), 26, 2, BraxChampionsPiksele.Bialy);
        int f = FilmSali(h);
        if (h == salaGra && stan == Stan.Koniec) { S(px, w, hh, "KONIEC SEANSU", 62, 2, Bursztyn); S(px, w, hh, "DZIĘKUJEMY", 90, 1, BraxChampionsPiksele.Bialy); return; }
        if (h == salaGra && stan == Stan.Blad) { S(px, w, hh, "BRAK PLIKU FILMU", 62, 2, BraxChampionsPiksele.Czerwony); S(px, w, hh, Krotko(f >= 0 ? Tytul(filmy[f]) : "", 40), 90, 1, BraxChampionsPiksele.Bialy); return; }
        if (h == salaGra && stan == Stan.Laduje) { S(px, w, hh, "SEANS ZA CHWILĘ", 62, 2, Bursztyn); S(px, w, hh, Krotko(f >= 0 ? Tytul(filmy[f]) : "", 40), 90, 1, BraxChampionsPiksele.Bialy); return; }
        if (f < 0) { BraxChampionsKinoEkrany.EkranWkrotce(px, h); return; }
        S(px, w, hh, Krotko(Tytul(filmy[f]), 40), 62, 1, Bursztyn);
        var nx = Nastepne(h, 1);
        S(px, w, hh, "NASTĘPNY SEANS " + (nx.Count > 0 ? Hm(nx[0]) : ""), 80, 1, BraxChampionsPiksele.Bialy);
        S(px, w, hh, bilet >= 0 && biletSala == h ? "TWÓJ BILET: RZĄD " + biletRzad + " · MIEJSCE " + biletMiejsce : zaDarmo ? "AKCJA: ODPAL FILM" : "BILETY W RECEPCJI", 100, 1, bilet >= 0 && biletSala == h || zaDarmo ? BraxChampionsPiksele.Zielony : BraxChampionsPiksele.Cyjan);
    }
    void NadDrzwiami(Color32[] px, int h)
    {
        int w = DW, hh = DH; int f = FilmSali(h);
        if (f < 0) { BraxChampionsKinoEkrany.DrzwiWkrotce(px, h); return; }
        BraxChampionsPiksele.Wypelnij(px, Ciemny);
        BraxChampionsPiksele.Prost(px, w, hh, 0, 0, 22, hh, Pasek);
        T(px, w, hh, (h + 1).ToString(), 8, 12, 1, BraxChampionsPiksele.Bialy);
        T(px, w, hh, f >= 0 ? Krotko(Tytul(filmy[f]), 16) : "WKRÓTCE", 26, 5, 1, Bursztyn);
        bool tu = h == salaGra && (stan == Stan.Gra || stan == Stan.Pauza);
        var nx = Nastepne(h, 1);
        T(px, w, hh, tu ? "SEANS TRWA" : f >= 0 && nx.Count > 0 ? "SEANS " + Hm(nx[0]) : "", 26, 18, 1, tu ? BraxChampionsPiksele.Czerwony : BraxChampionsPiksele.Cyjan);
    }
    Texture2D karta;
    void Plakaty()
    {
        if (plakaty == null) return;
        for (int i = 0; i < plakaty.Length; i++)
        {
            var r = plakaty[i]; if (!r) continue;
            int k = i < plakatFilm.Length ? plakatFilm[i] : i;
            bool poziomy = k >= 100; if (poziomy) k -= 100;
            int f = k >= 0 ? (k < N ? k : -1) : FilmSali(-k - 1);
            Texture2D t = null;
            string obraz = f >= 0 ? (poziomy && !string.IsNullOrEmpty(filmy[f].baner) ? filmy[f].baner : filmy[f].plakat) : "";
            if (!string.IsNullOrEmpty(obraz)) plakatTex.TryGetValue(obraz, out t);
            if (!t) t = Karta();
            var mpb = new MaterialPropertyBlock(); r.GetPropertyBlock(mpb);
            mpb.SetTexture("_MainTex", t); mpb.SetTexture("_EmissionMap", t); r.SetPropertyBlock(mpb);
        }
    }
    Texture2D Karta()
    {
        if (karta) return karta;
        var px = new Color32[PW * PH]; BraxChampionsKinoEkrany.KartaWkrotce(px);
        karta = new Texture2D(PW, PH, TextureFormat.RGBA32, false) { name = "plakat WKRÓTCE", filterMode = FilterMode.Point };
        karta.SetPixels32(px); karta.Apply(false); return karta;
    }

    // ---------------- interaction ----------------
    bool Przy(Vector3 p, Vector3 c) { var d = p - c; return Mathf.Abs(d.y) < 2.5f && new Vector2(d.x, d.z).magnitude < zasieg; }
    int Sala(Vector3 p)
    {
        for (int h = 0; h < salaMin.Length && h < salaMax.Length; h++)
            if (p.x > salaMin[h].x && p.x < salaMax[h].x && p.z > salaMin[h].z && p.z < salaMax[h].z && p.y > salaMin[h].y - 1f && p.y < salaMax[h].y) return h;
        return -1;
    }

    public string Kontekst(Vector3 p)
    {
        if (Przy(p, kiosk))
        {
            if (!wczytane) return "";
            if (N == 0) return "KINO · NOWE FILMY WKRÓTCE";
            return "KINO · WYBIERZ FILM: " + Krotko(Tytul(filmy[wybor]), 24) + " (" + (wybor + 1) + "/" + Mathf.Min(N, ekrany.Length) + ")";
        }
        if (Przy(p, kasa))
        {
            if (N == 0) return "";
            int s = SalaFilmu(wybor); if (s < 0) return "";
            if (bilet == wybor) return "KINO · BILET: SALA " + (s + 1) + " · RZĄD " + biletRzad + " · M " + biletMiejsce + (Mathf.Min(N, ekrany.Length) > 1 ? " (AKCJA: INNY FILM)" : "");
            return (zaDarmo ? "KINO · BILET GRATIS: " : "KINO · KUP BILET: ") + Krotko(Tytul(filmy[wybor]), 18) + " · SALA " + (s + 1);
        }
        int h = Sala(p);
        if (h >= 0 && h == salaGra)
        {
            if (stan == Stan.Gra) return "KINO · PAUZA";
            if (stan == Stan.Pauza) return "KINO · WZNÓW SEANS";
        }
        if (h >= 0 && N > 0 && zaDarmo && FilmSali(h) >= 0 && !(h == salaGra && (stan == Stan.Gra || stan == Stan.Pauza || stan == Stan.Laduje)))
            return "KINO · ODPAL FILM: " + Krotko(Tytul(filmy[FilmSali(h)]), 24);
        if (h >= 0 && N > 0 && (bilet < 0 || biletSala != h) && stan != Stan.Gra) return "KINO · BILETY W RECEPCJI";
        return "";
    }

    public bool Wykonaj(Vector3 p)
    {
        if (Przy(p, kiosk))
        {
            if (N == 0) return false;
            wybor = (wybor + 1) % Mathf.Min(N, ekrany.Length);
            Maluj(); return true;
        }
        if (Przy(p, kasa))
        {
            if (N == 0) return false;
            if (bilet == wybor) { if (Mathf.Min(N, ekrany.Length) < 2) return false; wybor = (wybor + 1) % Mathf.Min(N, ekrany.Length); }
            int s = SalaFilmu(wybor); if (s < 0) return false;
            bilet = wybor; biletSala = s;
            int r = s < rzedy.Length ? Mathf.Max(1, rzedy[s]) : 10, m = s < miejsca.Length ? Mathf.Max(1, miejsca[s]) : 20;
            biletRzad = Mathf.Clamp(Mathf.RoundToInt(r * .6f) + UnityEngine.Random.Range(-1, 2), 1, r);
            biletMiejsce = Mathf.Clamp(m / 2 + UnityEngine.Random.Range(-3, 4), 1, m);
            if (salaGra >= 0 && salaGra != s) Zatrzymaj();
            Maluj(); return true;
        }
        int h = Sala(p);
        if (h >= 0 && h == salaGra && vp)
        {
            if (stan == Stan.Gra) { vp.Pause(); stan = Stan.Pauza; Maluj(); return true; }
            if (stan == Stan.Pauza) { vp.Play(); stan = Stan.Gra; Maluj(); return true; }
        }
        if (h >= 0 && N > 0 && zaDarmo && FilmSali(h) >= 0 && !(h == salaGra && stan == Stan.Laduje)) { Rozpocznij(h); return true; }
        return false;
    }

    // ---------------- the show ----------------
    void Update()
    {
        if (Time.unscaledTime >= szukaj)
        {
            szukaj = Time.unscaledTime + .25f;
            if (!gracz) { var g = FindFirstObjectByType<BraxGymPlayer>(); if (g) gracz = g.transform; }
            var pos = gracz ? gracz.position : Camera.main ? Camera.main.transform.position : transform.position;
            blisko = (pos - transform.position).sqrMagnitude < 150f * 150f;
            int h = blisko ? Sala(pos) : -1;
            if (h != salaGracza) { Weszla(salaGracza, h); salaGracza = h; }
            Fotele(blisko ? pos : new Vector3(1e6f, 0f, 1e6f));
        }
        if (!blisko) return;
        // the house lights: down while the film runs in that hall, up otherwise (3 s)
        // owner 21:47 "jak się film odpala, to wszystkie światła gasną ... tylko film widać": the house lights and the lit
        // fittings fade out over 3 s (the exit signs stay), back up at the end, half up on a pause
        float cel = stan == Stan.Gra || stan == Stan.Laduje ? .03f : stan == Stan.Pauza ? .35f : 1f;
        if (!Mathf.Approximately(jasnosc, cel))
        {
            jasnosc = Mathf.MoveTowards(jasnosc, cel, Time.deltaTime / 3f);
            for (int i = 0; i < swiatla.Length; i++)
            {
                if (!swiatla[i]) continue;
                bool tej = i < swiatlaSala.Length && swiatlaSala[i] == salaGra;
                swiatla[i].intensity = swiatlaBaza[i] * (tej ? jasnosc : 1f);
            }
            Przyciemnij(salaGra, jasnosc);
        }
        if (Time.unscaledTime >= nastepny) { nastepny = Time.unscaledTime + 30f; Maluj(); }
    }

    // a hall's 200+ upholstered seats are drawn only when the player is in that hall or at its door (5 m round its walls):
    // from the lobby, the corridor or another hall they are behind walls anyway
    int[] foteleStan = new int[0];
    void Fotele(Vector3 p)
    {
        if (foteleStan.Length != salaMin.Length) { foteleStan = new int[salaMin.Length]; for (int i = 0; i < foteleStan.Length; i++) foteleStan[i] = -1; }
        for (int h = 0; h < salaMin.Length && h < salaMax.Length; h++)
        {
            bool widac = p.x > salaMin[h].x - 5f && p.x < salaMax[h].x + 5f && p.z > salaMin[h].z - 5f && p.z < salaMax[h].z + 5f && p.y > salaMin[h].y - 3f && p.y < salaMax[h].y;
            int st = widac ? 1 : 0; if (foteleStan[h] == st) continue; foteleStan[h] = st;
            for (int i = 0; i < fotele.Length; i++) if (fotele[i] && i < foteleSala.Length && foteleSala[i] == h) fotele[i].enabled = widac;
        }
    }

    void Weszla(int z, int doSali)
    {
        if (z >= 0 && z == salaGra && stan == Stan.Gra && vp) { vp.Pause(); stan = Stan.Pauza; Maluj(); }
        if (doSali < 0) return;
        if (doSali == salaGra && stan == Stan.Pauza && vp) { vp.Play(); stan = Stan.Gra; Maluj(); return; }
        if ((zaDarmo || (bilet >= 0 && biletSala == doSali)) && FilmSali(doSali) >= 0 && (salaGra != doSali || stan == Stan.Nic || stan == Stan.Koniec || stan == Stan.Blad)) Rozpocznij(doSali);
    }

    void Rozpocznij(int h)
    {
        int f = FilmSali(h); if (f < 0 || h >= ekrany.Length || !ekrany[h]) return;
        if (salaGra >= 0 && salaGra != h) Zatrzymaj();
        var film = filmy[f];
        string adres = !string.IsNullOrEmpty(film.url) ? film.url : !string.IsNullOrEmpty(film.plik) ? Sciezka(film.plik) : "";
        if (adres.StartsWith("file://")) adres = adres.Substring(7);
        salaGra = h;
        if (string.IsNullOrEmpty(adres)) { stan = Stan.Blad; Maluj(); return; }
        if (!vp)
        {
            vp = gameObject.AddComponent<VideoPlayer>();
            vp.playOnAwake = false; vp.isLooping = false; vp.skipOnDrop = true; vp.waitForFirstFrame = true;
            vp.renderMode = VideoRenderMode.RenderTexture; vp.aspectRatio = VideoAspectRatio.FitInside;
            var go = new GameObject("G1 kino dźwięk sali"); go.transform.SetParent(transform, false);
            glos = go.AddComponent<AudioSource>();
            glos.playOnAwake = false; glos.spatialBlend = .75f; glos.rolloffMode = AudioRolloffMode.Linear; glos.minDistance = 8f; glos.maxDistance = 34f; glos.dopplerLevel = 0f; glos.volume = 1f;
            vp.audioOutputMode = VideoAudioOutputMode.AudioSource; vp.controlledAudioTrackCount = 1; vp.EnableAudioTrack(0, true); vp.SetTargetAudioSource(0, glos);
            vp.prepareCompleted += Gotowe; vp.loopPointReached += Koniec; vp.errorReceived += Blad;
        }
        if (!rt) { rt = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32) { name = "G1 kino obraz" }; rt.Create(); }
        vp.targetTexture = rt;
        if (h < glosnik.Length) glos.transform.position = glosnik[h];
        vp.source = VideoSource.Url; vp.url = adres;
        stan = Stan.Laduje; Maluj();
        vp.Prepare();
        Debug.Log("BRAXHORN_KINO seans sala=" + (h + 1) + " film=" + film.tytul + " adres=" + adres);
    }
    void Gotowe(VideoPlayer v)
    {
        if (salaGra < 0) return;
        var m = ekr[salaGra] != null ? ekr[salaGra].mat : null;
        if (m) { m.SetTexture("_MainTex", rt); m.SetTexture("_EmissionMap", rt); }
        if (salaGracza == salaGra) { v.Play(); stan = Stan.Gra; } else { stan = Stan.Pauza; }
        Maluj();
    }
    void Koniec(VideoPlayer v) { stan = Stan.Koniec; bilet = -1; biletSala = -1; Obraz(); Maluj(); }
    void Blad(VideoPlayer v, string msg) { Debug.LogWarning("BRAXHORN_KINO film DO POPRAWKI " + msg); stan = Stan.Blad; Obraz(); Maluj(); }
    void Zatrzymaj() { if (vp) vp.Stop(); Przyciemnij(salaGra, 1f); stan = Stan.Nic; Obraz(); salaGra = -1; jasnosc = 1f; for (int i = 0; i < swiatla.Length; i++) if (swiatla[i]) swiatla[i].intensity = swiatlaBaza[i]; }
    // the hall's surfaces: their ambient (the region's interior light) scaled by k through property blocks
    void Przyciemnij(int sala, float k)
    {
        if (sala < 0 || mpb == null) return;
        for (int i = 0; i < widok.Length; i++)
        {
            if (!widok[i] || i >= widokSala.Length || widokSala[i] != sala) continue;
            var b = widokBaza[i];
            for (int j = 0; j < b.Length; j++)
            {
                var e = i < emisjaBaza.Length && j < emisjaBaza[i].Length ? emisjaBaza[i][j] : new Color(0f, 0f, 0f, -1f);
                if (b[j] < 0f && e.a < 0f) continue;
                widok[i].GetPropertyBlock(mpb, j);
                if (b[j] >= 0f) mpb.SetFloat(Ambient, b[j] * Mathf.Max(k, .04f));
                if (e.a >= 0f) mpb.SetColor(Emisja, new Color(e.r, e.g, e.b, 1f) * Mathf.Max(k * k, .015f));   // the lights fade faster than the room
                widok[i].SetPropertyBlock(mpb, j);
            }
        }
    }
    void Obraz()
    {
        if (salaGra < 0 || salaGra >= ekr.Length || ekr[salaGra] == null) return;
        ekr[salaGra].mat.SetTexture("_MainTex", ekr[salaGra].tex); ekr[salaGra].mat.SetTexture("_EmissionMap", ekr[salaGra].tex);
    }

    void OnDestroy() { if (rt) rt.Release(); }
}
