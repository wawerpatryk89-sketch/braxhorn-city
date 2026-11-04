using System.Collections.Generic;
using UnityEngine;

// BRAXHORN — WINDA WIELOPIĘTROWA (wspólna klasa A i B, 2.11; uzgodnienie: scratchpad/plan/WINDA.md).
// Właściciel 15:10: „winda z przyciskami, na które piętro jedziesz — w każdym wieżowcu; dostępne tylko przyciski pięter użytkowych”.
// - przy drzwiach przystanku: AKCJA = przywołanie (kabina przyjeżdża, drzwi kabiny i przystanku rozsuwają się razem);
// - w kabinie: AKCJA = panel z WSZYSTKIMI piętrami; aktywne tylko użytkowe (reszta wyszarzona, nieklikalna);
//   wybór → drzwi się zamykają, ciemny ekran ≤ 0.8 s, kabina i gracz przeniesieni, drzwi otwierają się na wybranym piętrze;
//   z otwartym panelem AKCJA = „JEDŹ · następne piętro użytkowe” (działa bez dotyku — zdjęcia CI, WebGL).
// Rejestracja w BraxInterakcje (bez zmian w BraxCityWorld / BraxGymPlayer). Bez świateł, bez cząsteczek, bez Canvas (IMGUI).
// Klasa = plik (BraxWinda.cs); pola serializowane poza #if.
public sealed class BraxWinda : MonoBehaviour, IBraxInterakcja
{
    public string nazwa = "WINDA";
    public Transform kabina;
    public Transform kabinaL, kabinaP;
    public float[] pietraY = new float[0];
    public string[] pietraNazwy = new string[0];
    public bool[] uzytkowe = new bool[0];
    public Transform[] przystanekL = new Transform[0], przystanekP = new Transform[0];
    public Vector3[] wezwanie = new Vector3[0];
    public Vector2 szyb;
    public Vector2 wnetrze = new Vector2(.9f, .9f);
    public float przesuw = .75f;
    public int start;
    public Renderer[] przyciski = new Renderer[0];
    public Material przyciskAktywny, przyciskWybrany, przyciskNieaktywny;

    enum Faza { Zamknieta, Otwiera, Otwarta, Zamyka, Jedzie }
    Faza faza = Faza.Zamknieta;
    int pietro, cel, wybor;
    float otw, tFazy, otwartaOd;
    bool pasazer, panel;
    BraxGymPlayer gracz; float szukaj;
    Vector3 kL0, kP0; Vector3[] pL0, pP0;
    static Texture2D bialy;
    const float CzasDrzwi = 1.1f, Zaciemnienie = .38f, Zasieg = 2.6f;

    public int Pietro => pietro;
    public bool Jedzie => faza == Faza.Jedzie;

    void Awake()
    {
        int n = pietraY.Length;
        if (kabinaL) kL0 = kabinaL.localPosition;
        if (kabinaP) kP0 = kabinaP.localPosition;
        pL0 = new Vector3[n]; pP0 = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            if (i < przystanekL.Length && przystanekL[i]) pL0[i] = przystanekL[i].localPosition;
            if (i < przystanekP.Length && przystanekP[i]) pP0[i] = przystanekP[i].localPosition;
        }
        pietro = Mathf.Clamp(start, 0, Mathf.Max(0, n - 1)); cel = pietro;
        if (kabina && n > 0) { var p = kabina.position; p.y = pietraY[pietro]; kabina.position = p; }
        UstawDrzwi(0f); Podswietl();
        var u = new List<string>(); for (int i = 0; i < n; i++) if (Uzytkowe(i)) u.Add(Nazwa(i));
        Debug.Log("BRAXHORN_WINDA " + nazwa + " pięter=" + n + " użytkowe=[" + string.Join(", ", u) + "]");
    }

    // Globalne odwołanie bez FindObjectsByType co klatkę: kamera Braxa sprawdza
    // tylko aktywne kabiny. Domyślny tryb 3. osoby poza windą bez zmian.
    static readonly List<BraxWinda> aktywne = new List<BraxWinda>();
    public static bool WewnatrzKabiny(Vector3 p)
    {
        foreach (var w in aktywne)
            if (w && w.isActiveAndEnabled && w.kabina && w.pietraY.Length > 0 && w.WKabinie(p))
                return true;
        return false;
    }

    void OnEnable() { BraxInterakcje.Dodaj(this); if (!aktywne.Contains(this)) aktywne.Add(this); }
    void OnDisable() { BraxInterakcje.Usun(this); aktywne.Remove(this); panel = false; }

    bool Uzytkowe(int i) => i >= 0 && i < uzytkowe.Length && uzytkowe[i];
    string Nazwa(int i) => i < pietraNazwy.Length && !string.IsNullOrEmpty(pietraNazwy[i]) ? pietraNazwy[i] : i.ToString();
    float Y => kabina ? kabina.position.y : (pietraY.Length > 0 ? pietraY[pietro] : 0f);

    BraxGymPlayer Gracz()
    {
        if (!gracz && Time.unscaledTime > szukaj) { szukaj = Time.unscaledTime + 1f; gracz = FindFirstObjectByType<BraxGymPlayer>(); }
        return gracz;
    }

    bool WKabinie(Vector3 p)
    {
        if (Mathf.Abs(p.y - Y) > 1.4f) return false;
        return Mathf.Abs(p.x - szyb.x) < wnetrze.x + .15f && Mathf.Abs(p.z - szyb.y) < wnetrze.y + .15f;
    }

    int PrzyWezwaniu(Vector3 p)
    {
        for (int i = 0; i < wezwanie.Length && i < pietraY.Length; i++)
        {
            if (wezwanie[i] == Vector3.zero) continue;
            if (Mathf.Abs(p.y - pietraY[i]) > 2.3f) continue;
            if (Vector2.Distance(new Vector2(p.x, p.z), new Vector2(wezwanie[i].x, wezwanie[i].z)) < Zasieg) return i;
        }
        return -1;
    }

    int NastepneUzytkowe(int od)
    {
        int n = pietraY.Length;
        for (int k = 1; k <= n; k++) { int i = (od + k) % n; if (Uzytkowe(i) && i != pietro) return i; }
        return pietro;
    }

    // ---------------------------------------------------------------- AKCJA (BraxInterakcje)
    public string Kontekst(Vector3 g)
    {
        if (pietraY.Length == 0 || !kabina) return "";
        if (faza == Faza.Jedzie && pasazer) return "WINDA — JAZDA";
        if (WKabinie(g))
        {
            if (faza == Faza.Zamyka || faza == Faza.Otwiera) return "WINDA — DRZWI";
            if (panel) return "JEDŹ · PIĘTRO " + Nazwa(wybor);
            return "WINDA · WYBIERZ PIĘTRO";
        }
        int w = PrzyWezwaniu(g);
        if (w < 0) return "";
        if (faza == Faza.Jedzie) return "WINDA JEDZIE…";
        if (w == pietro && (faza == Faza.Otwarta || faza == Faza.Otwiera)) return "";   // drzwi otwarte — wchodzi się idąc
        return "PRZYWOŁAJ WINDĘ · " + nazwa;
    }

    public bool Wykonaj(Vector3 g)
    {
        if (pietraY.Length == 0 || !kabina) return false;
        if (faza == Faza.Jedzie) return true;
        if (WKabinie(g))
        {
            if (!panel) { panel = true; wybor = NastepneUzytkowe(pietro); return true; }
            Jedz(wybor); return true;
        }
        int w = PrzyWezwaniu(g);
        if (w < 0) return false;
        if (w == pietro) { if (faza != Faza.Otwarta) { faza = Faza.Otwiera; } return true; }
        cel = w; pasazer = false;
        faza = otw > .001f ? Faza.Zamyka : Faza.Jedzie; tFazy = 0f;
        return true;
    }

    void Jedz(int i)
    {
        panel = false;
        if (!Uzytkowe(i)) return;
        if (i == pietro) { faza = Faza.Otwiera; return; }
        cel = i; pasazer = true; tFazy = 0f;
        var p = Gracz(); if (p) p.MoveInput("0,0");
        faza = otw > .001f ? Faza.Zamyka : Faza.Jedzie;
    }

    // ---------------------------------------------------------------- ruch
    void UstawDrzwi(float f)
    {
        otw = Mathf.Clamp01(f);
        float s = Mathf.SmoothStep(0f, 1f, otw) * przesuw;
        if (kabinaL) kabinaL.localPosition = kL0 + Vector3.left * s;
        if (kabinaP) kabinaP.localPosition = kP0 + Vector3.right * s;
        for (int i = 0; i < pL0.Length; i++)
        {
            float si = i == pietro && faza != Faza.Jedzie ? s : 0f;
            if (i < przystanekL.Length && przystanekL[i]) przystanekL[i].localPosition = pL0[i] + Vector3.left * si;
            if (i < przystanekP.Length && przystanekP[i]) przystanekP[i].localPosition = pP0[i] + Vector3.right * si;
        }
    }

    void Update()
    {
        if (!kabina || pietraY.Length == 0) return;
        float dt = Mathf.Min(Time.deltaTime, .05f);
        tFazy += dt;
        var g = Gracz();
        switch (faza)
        {
            case Faza.Otwiera:
                UstawDrzwi(otw + dt / CzasDrzwi);
                if (otw >= .999f) { UstawDrzwi(1f); faza = Faza.Otwarta; otwartaOd = Time.time; }
                break;
            case Faza.Otwarta:
                // samo się zamyka po 7 s, gdy nikt nie stoi w progu ani w kabinie
                if (Time.time - otwartaOd > 7f && !(g && (WKabinie(g.transform.position) || W_Progu(g.transform.position)))) { faza = Faza.Zamyka; tFazy = 0f; cel = pietro; }
                break;
            case Faza.Zamyka:
                if (g && W_Progu(g.transform.position) && !pasazer) { faza = Faza.Otwiera; break; }   // fotokomórka
                UstawDrzwi(otw - dt / CzasDrzwi);
                if (otw <= .001f)
                {
                    UstawDrzwi(0f);
                    if (cel != pietro)
                    {
                        if (pasazer && !(g && WKabinie(g.transform.position))) { pasazer = false; cel = pietro; faza = Faza.Otwiera; break; }
                        faza = Faza.Jedzie; tFazy = 0f;
                    }
                    else faza = Faza.Zamknieta;
                }
                break;
            case Faza.Jedzie:
                {
                    // pasażer: zaciemnienie, przeniesienie w najciemniejszej chwili, rozjaśnienie; bez pasażera: czas przejazdu bez ekranu
                    float przejazd = pasazer ? Zaciemnienie : Mathf.Clamp(Mathf.Abs(pietraY[cel] - Y) / 6f, .8f, 4f);
                    if (tFazy >= przejazd && Mathf.Abs(Y - pietraY[cel]) > .001f)
                    {
                        float dy = pietraY[cel] - Y;
                        kabina.position += Vector3.up * dy;
                        if (pasazer && g)
                        {
                            var cc = g.body; bool on = cc && cc.enabled;
                            if (cc) cc.enabled = false;
                            g.transform.position += Vector3.up * dy;
                            Physics.SyncTransforms(); if (cc) cc.enabled = on;
                            g.MoveInput("0,0");
                        }
                        pietro = cel; Podswietl();
                    }
                    if (tFazy >= przejazd + (pasazer ? Zaciemnienie : 0f)) { pasazer = false; faza = Faza.Otwiera; tFazy = 0f; }
                    break;
                }
        }
        if (panel)
        {
            if (!(g && WKabinie(g.transform.position)) || faza == Faza.Jedzie) panel = false;
            else Dotyk();
        }
    }

    bool W_Progu(Vector3 p)
    {
        if (Mathf.Abs(p.y - Y) > 1.4f) return false;
        return Mathf.Abs(p.x - szyb.x) < wnetrze.x + 1.4f && Mathf.Abs(p.z - szyb.y) < wnetrze.y + 1.4f && !WKabinie(p);
    }

    void Podswietl()
    {
        for (int i = 0; i < przyciski.Length; i++)
        {
            var r = przyciski[i]; if (!r) continue;
            var m = i == pietro && przyciskWybrany ? przyciskWybrany : Uzytkowe(i) && przyciskAktywny ? przyciskAktywny : przyciskNieaktywny;
            if (m) r.sharedMaterial = m;
        }
    }

    // ---------------------------------------------------------------- panel pięter (IMGUI rysuje, dotyk liczony tutaj)
    Rect Okno()
    {
        float w = Mathf.Min(Screen.width * .56f, 760f), h = Mathf.Min(Screen.height * .78f, 720f);
        return new Rect((Screen.width - w) * .5f, (Screen.height - h) * .5f, w, h);
    }
    int Kolumny => pietraY.Length <= 12 ? 3 : pietraY.Length <= 30 ? 5 : 8;
    Rect Przycisk(int i)
    {
        var o = Okno(); int kol = Kolumny; int rz = Mathf.CeilToInt(pietraY.Length / (float)kol);
        float top = o.y + o.height * .14f, dol = o.yMax - o.height * .15f;
        float bw = (o.width - 24f) / kol, bh = Mathf.Min((dol - top) / Mathf.Max(1, rz), o.height * .16f);
        int r = rz - 1 - i / kol, c = i % kol;   // parter na dole, jak w prawdziwej windzie
        return new Rect(o.x + 12f + c * bw + 4f, top + r * bh + 3f, bw - 8f, bh - 6f);
    }
    Rect Zamknij() { var o = Okno(); return new Rect(o.x + o.width * .3f, o.yMax - o.height * .13f, o.width * .4f, o.height * .1f); }

    void Dotyk()
    {
        var punkty = new List<Vector2>();
        for (int i = 0; i < Input.touchCount; i++) { var t = Input.GetTouch(i); if (t.phase == TouchPhase.Began) punkty.Add(t.position); }
        if (Input.touchCount == 0 && Input.GetMouseButtonDown(0)) punkty.Add(Input.mousePosition);
        foreach (var sp in punkty)
        {
            var q = new Vector2(sp.x, Screen.height - sp.y);
            if (Zamknij().Contains(q)) { panel = false; return; }
            for (int i = 0; i < pietraY.Length; i++)
                if (Przycisk(i).Contains(q)) { if (Uzytkowe(i)) { wybor = i; Jedz(i); } return; }
        }
    }

    static Texture2D Bialy() { if (!bialy) { bialy = new Texture2D(1, 1); bialy.SetPixel(0, 0, Color.white); bialy.Apply(); } return bialy; }
    static void Pole(Rect r, Color c) { var s = GUI.color; GUI.color = c; GUI.DrawTexture(r, Bialy()); GUI.color = s; }

    void OnGUI()
    {
        GUI.depth = -50;
        if (faza == Faza.Jedzie && pasazer)
        {
            float a = tFazy < Zaciemnienie ? tFazy / Zaciemnienie : 1f - (tFazy - Zaciemnienie) / Zaciemnienie;
            Pole(new Rect(0, 0, Screen.width, Screen.height), new Color(0, 0, 0, Mathf.Clamp01(a)));
        }
        if (!panel) return;
        var o = Okno();
        Pole(o, new Color(.06f, .07f, .08f, .92f));
        var tyt = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(o.height * .055f), fontStyle = FontStyle.Bold };
        tyt.normal.textColor = Color.white;
        GUI.Label(new Rect(o.x, o.y + o.height * .02f, o.width, o.height * .1f), nazwa + " · WYBIERZ PIĘTRO", tyt);
        var st = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(o.height * .05f), fontStyle = FontStyle.Bold };
        for (int i = 0; i < pietraY.Length; i++)
        {
            var r = Przycisk(i); bool u = Uzytkowe(i);
            Color tlo = i == pietro ? new Color(.95f, .62f, .12f) : u ? (i == wybor ? new Color(.42f, .85f, .55f) : new Color(.22f, .55f, .34f)) : new Color(.22f, .22f, .23f);
            Pole(r, tlo);
            st.normal.textColor = u || i == pietro ? Color.white : new Color(.5f, .5f, .5f);
            GUI.Label(r, Nazwa(i), st);
        }
        var z = Zamknij(); Pole(z, new Color(.35f, .12f, .12f));
        st.normal.textColor = Color.white; GUI.Label(z, "ZAMKNIJ", st);
    }
}
