using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// BRAXHORN CITY — sterowanie dotykowe w aplikacji Android (bez warstwy HTML).
// Przebudowa 31.10 (właściciel 18:35: „nie ma joysticka, bieg/skok/kop/cios chujowo”), BUILD 14 od nowa wygląd
// (właściciel 1.11 20:49, zdjęcie przy kasynie: „przyciski za duże i brzydkie → mniejsze, nowocześniejsze, jak w topowych
// grach mobilnych: półprzezroczyste koła z ikonami, cienka obwódka, delikatny cień, 60–70 % obecnego rozmiaru; joystick
// dyskretny; MENU jako mała ikona; nie zasłaniać postaci i drogi”):
//  • Joystick (lewa część ekranu): sam pierścień + półprzezroczysta gałka; w spoczynku w lewym dolnym rogu, po dotknięciu
//    przeskakuje pod kciuk. Lekkie wychylenie = chód, mocne (do krawędzi) = bieg — płynnie, bez przycisku BIEG.
//  • Kamera: przeciąganie palcem po prawej części ekranu (czułość z USTAWIEŃ); sama wraca za plecy (BraxGymPlayer).
//  • Prawy róg: CIOS (pięść, największy), KOP (but), SKOK (strzałka) w ćwiartce koła; AKCJA = pigułka z dłonią i nazwą
//    czynności z gry (POGADAJ, USIĄDŹ · SOFA, OTWÓRZ DRZWI…), tylko gdy jest co zrobić. Na łodzi / siedząc: tylko AKCJA.
//  • MENU: mała ikona w prawym górnym rogu (albo przycisk „wstecz” telefonu) → BraxMenuGry.
//  • Rozmiar przycisków: MENU → USTAWIENIA (małe / średnie / duże). Wszystko w strefie bezpiecznej (Screen.safeArea).
// Obsługa z Input.touches (każdy palec osobno), NIE z IMGUI — IMGUI widzi tylko pierwszy palec. IMGUI tylko rysuje
// (i obsługuje menu). Polecenia idą przez SendMessage do obiektu gracza — tak samo jak ze strony WWW (BraxClaudeAkcja:
// wieża w willi, BraxNovaMusic: pauza muzyki dostają ten sam przycisk).
// Wydajność: układ liczony tylko przy zmianie ekranu / rozmiaru, napis AKCJI odpytywany co 0.12 s, ruch joysticka wysyłany
// tylko przy zmianie (siatka 0.02, napisy z pamięci podręcznej), HUD rysowany tylko w zdarzeniu Repaint.
// Przeglądarka (WebGL) z ?qa=1: komponent tworzony do zdjęć CI, ale nic nie rysuje, dopóki strona nie wyśle
// BraxSend('ReviewUi', tryb) — BraxInterfejsQa.
public sealed class BraxTouchControls : MonoBehaviour
{
    static BraxTouchControls inst;
    public static bool Qa { get; private set; }
    static string podglad = "";

    BraxGymPlayer player;
    public BraxGymPlayer Gracz => player;
    BraxMenuGry menu;
    bool paused, trybZdjec, ukryty;
    float trybZdjecOd, nextFind, podpowiedzDo;
    // palce
    int joyId = -1, lookId = -1;
    Vector2 joyOrigin, joyNow, lookLast;            // ekran (y w górę)
    const int PCios = 0, PKop = 1, PSkok = 2, PAkcja = 3, PMenu = 4;
    readonly int[] palec = { -1, -1, -1, -1, -1 };
    readonly float[] blysk = new float[5];
    // układ (ekran, y w górę)
    float s, m, gap, rC, rS, R, rMenu;
    Vector2 cCios, cKop, cSkok, cMenu, joyRest;
    Rect akcjaG;                                     // pigułka AKCJA (IMGUI, y w dół)
    int ukW, ukH, ukWersja = -1; Rect ukSafe;
    // napis AKCJI
    string kontekst = "", kontekstEkran = "", kontekstZmierzony; float kontekstSzer, nastKontekst; bool grajek;
    // ruch
    int ruchX, ruchY;
    readonly Dictionary<int, string> ruchTeksty = new Dictionary<int, string>();
    // licznik kl./s, pamięć, straż klatek (WYSOKA na telefonie)
    float fpsT, fpsPokaz, strazT; int fpsN, strazN, obnizenie;
    public string LicznikTekst { get; private set; } = "";
    // komunikat
    string komunikat = ""; float komunikatDo, komunikatOd;
    int scenaZalogowana = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        bool android = Application.platform == RuntimePlatform.Android;
        Qa = Application.platform == RuntimePlatform.WebGLPlayer && UrlQa();
        if (!android && !Qa) return;
        if (FindAnyObjectByType<BraxTouchControls>()) return;
        var g = new GameObject("BRAXHORN sterowanie dotykowe");
        DontDestroyOnLoad(g);
        g.AddComponent<BraxTouchControls>();
    }

    static bool UrlQa()
    {
        var u = Application.absoluteURL ?? "";
        int q = u.IndexOf('?'); if (q < 0) return false;
        foreach (var v in u.Substring(q + 1).Split('&', '#')) if (v == "qa=1") return true;
        return false;
    }

    void Awake()
    {
        inst = this;
        useGUILayout = false;   // bez GUILayout: IMGUI pomija przebieg Layout (mniej pracy i alokacji co klatkę)
        menu = new BraxMenuGry(this);
        if (Qa) return;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Input.multiTouchEnabled = true;
        Input.simulateMouseWithTouches = false;   // przyciski gry liczone z dotyku; IMGUI tylko rysuje (i menu)
        BraxUstawienia.ZastosujDzwiek();
    }

    // ---------------- podgląd do zdjęć CI (WebGL ?qa=1) ----------------
    public static void Podglad(string tryb)
    {
        podglad = (tryb ?? "").Trim().ToLowerInvariant();
        if (podglad == "wyl" || podglad == "off") podglad = "";
        if (!inst) return;
        var mn = inst.menu;
        switch (podglad)
        {
            case "menu": mn.Otworz(BraxMenuGry.Ekran.Glowne); break;
            case "ustawienia": mn.Otworz(BraxMenuGry.Ekran.Ustawienia); break;
            case "test": BraxUstawienia.Licznik = true; mn.Otworz(BraxMenuGry.Ekran.Test); break;
            case "teleport": mn.Otworz(BraxMenuGry.Ekran.Teleport); break;
            case "mapa": mn.Otworz(BraxMenuGry.Ekran.Mapa); break;
            case "zdjecia": inst.trybZdjecOd = Time.unscaledTime; break;
        }
        Debug.Log("BRAXHORN_UI podgląd interfejsu: " + (podglad.Length > 0 ? podglad : "wyłączony"));
    }
    bool PodgladMenu => Qa && (podglad == "menu" || podglad == "ustawienia" || podglad == "test" || podglad == "teleport" || podglad == "mapa");

    // ---------------- szukanie gracza, dziennik sceny ----------------
    void Find()
    {
        if (player || Time.unscaledTime < nextFind) return;
        nextFind = Time.unscaledTime + .5f;
        player = FindAnyObjectByType<BraxGymPlayer>();
        if (!player) return;
        joyId = lookId = -1; paused = false; trybZdjec = false; ruchX = ruchY = 0;
        for (int i = 0; i < palec.Length; i++) palec[i] = -1;
        if (podpowiedzDo <= 0f) podpowiedzDo = Time.unscaledTime + 9f;
        if (Qa && !player.GetComponent<BraxInterfejsQa>()) player.gameObject.AddComponent<BraxInterfejsQa>();
    }

    void LogSceny()
    {
        int h = player.gameObject.scene.handle;
        if (h == scenaZalogowana) return;
        scenaZalogowana = h;
        var sa = Screen.safeArea;
        int miejsc = 0; try { miejsc = BraxMiejscaTestowe.Lista(player).Count; } catch { }
        Debug.Log("BRAXHORN_UI scena " + player.gameObject.scene.name + ": sterowanie " + (Qa ? "podgląd CI (WebGL ?qa=1)" : "dotykowe") +
                  " · ekran " + Screen.width + "×" + Screen.height + " (bezpieczny " + Mathf.RoundToInt(sa.x) + "," + Mathf.RoundToInt(sa.y) + " " + Mathf.RoundToInt(sa.width) + "×" + Mathf.RoundToInt(sa.height) + ")" +
                  " · język " + BraxTeksty.Aktualny + " · przyciski " + BraxUstawienia.NazwaPrzyciskow + " ×" + BraxUstawienia.SkalaPrzyciskow.ToString("0.00") +
                  " · czułość " + BraxUstawienia.Czulosc.ToString("0.0") + " · muzyka " + Mathf.RoundToInt(BraxUstawienia.Muzyka * 100) + "% · efekty " + Mathf.RoundToInt(BraxUstawienia.Efekty * 100) + "%" +
                  " · grafika " + BraxGrafika.Nazwy[(int)BraxGrafika.Aktualny] + (BraxGrafika.Reczny ? " (ręcznie)" : " (auto: " + BraxProfilTelefonu.Powod + ")") +
                  " · kamera: za plecami po " + BraxGymPlayer.AutoCameraDelay + " s, near ≥ " + BraxClaudeKamera.MinBlisko +
                  " · drzwi " + BraxCityDoor.All.Count + " (same się zamykają po " + BraxCityDoor.ZamykanieS + " s)" +
                  " · miejsca teleportu " + miejsc + " · menu: WRÓĆ, USTAWIENIA, MAPA, ZGŁOŚ, UTKNĄŁEM, TRYB ZDJĘĆ, TEST, AUTORZY");
    }

    // ---------------- układ ----------------
    void Uklad()
    {
        var saG = BraxGui.BezpiecznyProstokat();
        int w = Screen.width, h = Screen.height, ver = BraxUstawienia.Wersja;
        if (w == ukW && h == ukH && ver == ukWersja && saG == ukSafe) return;
        ukW = w; ukH = h; ukWersja = ver; ukSafe = saG;
        var sa = new Rect(saG.x, h - saG.yMax, saG.width, saG.height);   // ekran, y w górę
        s = Mathf.Min(w, h);
        float k = BraxUstawienia.SkalaPrzyciskow;
        m = s * .045f; gap = s * .024f * k;
        rC = s * .074f * k;            // CIOS: średnica 0.148 s (dawniej 0.22 s → 67 %)
        rS = s * .056f * k;            // KOP / SKOK: 0.112 s (dawniej 0.165 s → 68 %)
        R = s * .125f * k;             // joystick (dawniej 0.16 s)
        rMenu = s * .036f * Mathf.Sqrt(k);
        cCios = new Vector2(sa.xMax - m - rC, sa.yMin + m + rC);
        float d = rC + gap + rS;
        cKop = cCios + new Vector2(Mathf.Cos(188f * Mathf.Deg2Rad), Mathf.Sin(188f * Mathf.Deg2Rad)) * d;
        cSkok = cCios + new Vector2(Mathf.Cos(97f * Mathf.Deg2Rad), Mathf.Sin(97f * Mathf.Deg2Rad)) * d;
        joyRest = new Vector2(sa.xMin + m + R * 1.05f, sa.yMin + m + R * 1.05f);
        cMenu = new Vector2(sa.xMax - m * .6f - rMenu, sa.yMax - m * .6f - rMenu);
        kontekstZmierzony = null;   // szerokość pigułki do przeliczenia
    }

    bool OnFoot => player && !player.vessel && !player.seat && !player.autoGracza;
    bool AkcjaWidoczna => kontekstEkran.Length > 0;
    static bool W(Vector2 p, Vector2 c, float r) => (p - c).sqrMagnitude <= r * r;
    bool WAkcji(Vector2 p) { var g = new Vector2(p.x, Screen.height - p.y); var r = akcjaG; r.xMin -= gap * .4f; r.yMin -= gap * .4f; r.xMax += gap * .4f; r.yMax += gap * .4f; return r.Contains(g); }

    // ---------------- pętla ----------------
    void Update()
    {
        Find();
        if (!Qa) { BraxUstawienia.Pilnuj(); StrazKlatek(); }
        Licznik();
        if (!player) return;
        LogSceny();
        Uklad();
        if (Time.unscaledTime >= nastKontekst)
        {
            nastKontekst = Time.unscaledTime + .12f;
            kontekst = player.Context ?? "";
            grajek = kontekst.Length == 0 && BraxClaudeGrajek.Blisko;
            if (Qa && podglad == "przyciski" && kontekst.Length == 0) kontekst = "POGADAJ";
            kontekstEkran = grajek ? BraxTeksty.T("hud.muzyka") : BraxTeksty.Kontekst(kontekst);
        }
        if (Qa) return;
        if (Input.GetKeyDown(KeyCode.Escape)) { if (!paused) Pauza(true); else if (menu.Wstecz()) Pauza(false); return; }
        if (paused) { joyId = lookId = -1; return; }
        if (trybZdjec)
        {
            for (int i = 0; i < Input.touchCount; i++)
                if (Input.GetTouch(i).phase == TouchPhase.Began && Time.unscaledTime > trybZdjecOd + .3f) { trybZdjec = false; Debug.Log("BRAXHORN_UI tryb zdjęć: koniec"); break; }
            return;
        }
        for (int i = 0; i < Input.touchCount; i++)
        {
            var t = Input.GetTouch(i); var p = t.position;
            if (t.phase == TouchPhase.Began)
            {
                if (W(p, cMenu, rMenu * 1.6f)) { blysk[PMenu] = Time.unscaledTime + .15f; Pauza(true); return; }
                if (AkcjaWidoczna && WAkcji(p)) { palec[PAkcja] = t.fingerId; Polecenie("action", PAkcja); continue; }
                if (OnFoot)
                {
                    int b = Przycisk(p);
                    if (b >= 0) { palec[b] = t.fingerId; Polecenie(b == PCios ? "punch" : b == PKop ? "kick" : "jump", b); continue; }
                }
                float granica = ukSafe.x + ukSafe.width * .45f;
                if (p.x < granica && joyId < 0) { joyId = t.fingerId; joyOrigin = p; joyNow = p; }
                else if (p.x >= granica && lookId < 0) { lookId = t.fingerId; lookLast = p; }
            }
            else if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
            {
                if (t.fingerId == joyId) { joyNow = p; WyslijRuch(); }
                else if (t.fingerId == lookId && t.phase == TouchPhase.Stationary) player.LookDelta(0f, 0f);   // palec trzyma kamerę: bez powrotu za plecy
                else if (t.fingerId == lookId)
                {
                    var d = p - lookLast; lookLast = p;
                    // 1.11: przesunięcie palca w prawo obraca widok w prawo; czułość 1.0 = 0.121°/px przy ekranie 1080 px (jak dotąd)
                    float k = .121f * 1080f / Mathf.Max(320f, s) * BraxUstawienia.Czulosc;
                    player.LookDelta(d.x * k, -d.y * k * .727f);
                }
            }
            else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            {
                if (t.fingerId == joyId) { joyId = -1; Stop(); }
                else if (t.fingerId == lookId) lookId = -1;
                for (int b = 0; b < palec.Length; b++) if (palec[b] == t.fingerId) palec[b] = -1;
            }
        }
        if (Input.touchCount == 0)
        {
            if (joyId >= 0) { joyId = -1; Stop(); }
            lookId = -1; for (int b = 0; b < palec.Length; b++) palec[b] = -1;
        }
    }

    // najbliższy przycisk walki pod palcem (strefy dotyku nieco większe niż koła, ale się nie dublują)
    int Przycisk(Vector2 p)
    {
        int best = -1; float bd = 1f;
        float dc = (p - cCios).magnitude / (rC * 1.18f), dk = (p - cKop).magnitude / (rS * 1.28f), ds = (p - cSkok).magnitude / (rS * 1.28f);
        if (dc < bd) { bd = dc; best = PCios; }
        if (dk < bd) { bd = dk; best = PKop; }
        if (ds < bd) { best = PSkok; }
        return best;
    }

    void Polecenie(string key, int przycisk)
    {
        blysk[przycisk] = Time.unscaledTime + .16f;
        player.gameObject.SendMessage("Command", key, SendMessageOptions.DontRequireReceiver);
    }

    void WyslijRuch()
    {
        var d = Vector2.ClampMagnitude((joyNow - joyOrigin) / R, 1f);
        if (d.magnitude < .08f) d = Vector2.zero;
        int qx = Mathf.RoundToInt(d.x * 50f), qy = Mathf.RoundToInt(d.y * 50f);
        if (qx == ruchX && qy == ruchY) return;
        ruchX = qx; ruchY = qy;
        int klucz = (qx + 50) * 101 + (qy + 50);
        if (!ruchTeksty.TryGetValue(klucz, out var tekst)) ruchTeksty[klucz] = tekst = F(qx / 50f) + "," + F(qy / 50f);
        player.MoveInput(tekst);
    }
    void Stop() { ruchX = ruchY = 0; if (player) player.MoveInput("0,0"); }
    static string F(float v) => v.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

    // ---------------- menu, tryb zdjęć, zgłoszenie, komunikaty ----------------
    public void Pauza(bool on)
    {
        if (Qa) { if (!on) podglad = ""; return; }
        if (paused == on || !player) return;
        paused = on; joyId = lookId = -1; Stop();
        for (int b = 0; b < palec.Length; b++) palec[b] = -1;
        player.gameObject.SendMessage("Command", "pause", SendMessageOptions.DontRequireReceiver);
        if (on) { menu.Otworz(); Input.simulateMouseWithTouches = true; }    // w menu IMGUI ma reagować na dotyk
        else { BraxUstawienia.Zapisz(); Input.simulateMouseWithTouches = false; }
    }

    public void TrybZdjec()
    {
        Pauza(false);
        trybZdjec = true; trybZdjecOd = Time.unscaledTime;
        Debug.Log("BRAXHORN_UI tryb zdjęć: interfejs ukryty do dotknięcia");
    }

    public void Zglos()
    {
        if (ukryty) return;
        StartCoroutine(BraxZgloszenie.Zrob(player, h => ukryty = h, msg => Komunikat(msg, 6f)));
    }

    public void Komunikat(string tekst, float sekundy)
    {
        if (string.IsNullOrEmpty(tekst)) return;
        komunikat = tekst; komunikatOd = Time.unscaledTime; komunikatDo = Time.unscaledTime + sekundy;
    }

    public void ResetStrazy() { obnizenie = 0; strazT = 0; strazN = 0; }

    // licznik kl./s i pamięci: liczony co 0.5 s (napis tworzony tylko wtedy, nie co klatkę)
    void Licznik()
    {
        fpsN++; fpsT += Time.unscaledDeltaTime;
        if (fpsT < .5f) return;
        fpsPokaz = fpsN / fpsT; fpsN = 0; fpsT = 0;
        if (!BraxUstawienia.Licznik) { if (LicznikTekst.Length > 0) LicznikTekst = ""; return; }
        long uzyw = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() >> 20, rez = UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong() >> 20;
        LicznikTekst = BraxTeksty.F("test.pamiec", Mathf.RoundToInt(fpsPokaz), uzyw + " / " + rez + " MB") + " · " + BraxGrafika.Nazwy[(int)BraxGrafika.Aktualny] +
                       " · cel " + Application.targetFrameRate + " · RAM " + SystemInfo.systemMemorySize + " MB";
    }

    // WYSOKA na telefonie: Dimensity 8350 po kilku minutach się grzeje (test Beebom: 32 % stabilności) — średnia z 8 s
    // poniżej 50 kl./s → cel 45, potem poniżej 38 → 30. Po zmianie sceny BraxGrafika wraca do 60 — obniżenie wraca też.
    void StrazKlatek()
    {
        strazT += Time.unscaledDeltaTime; strazN++;
        if (strazT < 8f) return;
        float fps = strazN / strazT; strazT = 0; strazN = 0;
        if (!Application.isMobilePlatform || BraxGrafika.Aktualny != BraxGrafika.Tryb.Wysoka || !player || paused) return;
        int cel = Application.targetFrameRate;
        if (obnizenie > 0 && cel > obnizenie) { Application.targetFrameRate = obnizenie; return; }
        int nowy = cel >= 60 && fps < 50f ? 45 : cel == 45 && fps < 38f ? 30 : 0;
        if (nowy == 0) return;
        obnizenie = nowy; Application.targetFrameRate = nowy;
        Debug.Log("BRAXHORN_UI grafika WYSOKA: średnio " + fps.ToString("0") + " kl./s przy celu " + cel + " → cel " + nowy + " kl./s (telefon się grzeje)");
    }

    // ---------------- rysowanie ----------------
    void OnGUI()
    {
        if (ukryty) return;
        if (Qa && podglad == "wczytywanie")
        {
            GUI.depth = -30;
            BraxEkranWczytywania.Rysuj(.62f, BraxTeksty.T("start.wczytywanie"), BraxTeksty.T("start.osiedle"), BraxTeksty.Wskazowka(1), "", false, true);
            return;
        }
        if (!player) return;
        if (Qa && podglad.Length == 0) return;   // przeglądarka bez podglądu: strona ma własne sterowanie
        if (paused || PodgladMenu)
        {
            GUI.depth = -10;
            menu.Rysuj(player);
            Toast();
            return;
        }
        GUI.depth = 0;
        if (Event.current.type != EventType.Repaint) return;
        if (trybZdjec || (Qa && podglad == "zdjecia"))
        {
            float a = Mathf.Clamp01(1.6f - (Time.unscaledTime - trybZdjecOd));   // podpowiedź znika po 1.6 s (czyste zdjęcie)
            if (a > 0f) Pasek(BraxTeksty.T("hud.zdjecia"), ukSafe.y + s * .04f, a);
            return;
        }
        Hud();
        Toast();
    }

    Vector2 G(Vector2 ekran) => new Vector2(ekran.x, Screen.height - ekran.y);
    bool Wcis(int b) => palec[b] >= 0 || Time.unscaledTime < blysk[b];

    void Hud()
    {
        // MENU (prawy górny róg)
        BraxGui.PrzyciskKolowy(G(cMenu), rMenu, BraxIkony.Menu, Wcis(PMenu), BraxGui.Akcent, .92f);
        // joystick
        bool akt = joyId >= 0;
        Vector2 jc = akt ? joyOrigin : joyRest;
        Vector2 knob = akt ? jc + Vector2.ClampMagnitude(joyNow - joyOrigin, R) : jc;
        float a = akt ? 1f : .6f;
        BraxGui.Kolo(G(jc), R * 1.08f, BraxIkony.Cien, new Color(0f, 0f, 0f, .16f * a));
        BraxGui.Kolo(G(jc), R, BraxIkony.Kolo, new Color(BraxGui.Szklo.r, BraxGui.Szklo.g, BraxGui.Szklo.b, .2f * a));
        BraxGui.Kolo(G(jc), R, BraxIkony.PierscienCienki, new Color(1f, 1f, 1f, .5f * a));
        BraxGui.Kolo(G(knob), R * .42f, BraxIkony.Kolo, new Color(1f, 1f, 1f, (akt ? .42f : .3f)));
        BraxGui.Kolo(G(knob), R * .42f, BraxIkony.PierscienCienki, new Color(1f, 1f, 1f, .85f * a));
        if (player.vessel) Pasek(BraxTeksty.T("hud.lodz"), Screen.height - jc.y - R * 1.75f, .9f, jc.x);
        // przyciski walki
        if (OnFoot)
        {
            BraxGui.PrzyciskKolowy(G(cCios), rC, BraxIkony.Piesc, Wcis(PCios), BraxGui.Trafienie);
            BraxGui.PrzyciskKolowy(G(cKop), rS, BraxIkony.But, Wcis(PKop), BraxGui.Trafienie);
            BraxGui.PrzyciskKolowy(G(cSkok), rS, BraxIkony.Skok, Wcis(PSkok), BraxGui.Akcent);
        }
        // AKCJA: pigułka z dłonią i nazwą czynności
        if (AkcjaWidoczna) Akcja();
        else akcjaG = new Rect(-1000, -1000, 0, 0);
        // podpowiedź na start, licznik
        float y = ukSafe.y + m * .5f;
        if (BraxUstawienia.Licznik && LicznikTekst.Length > 0) { Pasek(LicznikTekst, y, 1f); y += s * .055f; }
        float pa = Mathf.Clamp01(podpowiedzDo - Time.unscaledTime);
        if (pa > 0f) Pasek(BraxTeksty.T("hud.podpowiedz"), y, pa);
    }

    void Akcja()
    {
        float k = BraxUstawienia.SkalaPrzyciskow, h = s * .078f * k;
        var st = BraxGui.Styl(Mathf.RoundToInt(h * .36f), true, TextAnchor.MiddleLeft);
        if (kontekstZmierzony != kontekstEkran) { kontekstZmierzony = kontekstEkran; kontekstSzer = BraxGui.Szerokosc(kontekstEkran, st); }   // porównanie treści (nowy napis co 0.12 s)
        float prawy = cSkok.x - rS - gap, lewyMin = ukSafe.x + ukSafe.width * .47f;
        float w = Mathf.Clamp(kontekstSzer + h * 1.45f, h * 2.6f, Mathf.Max(h * 2.6f, prawy - lewyMin));
        float yc = Screen.height - cSkok.y;
        akcjaG = new Rect(prawy - w, yc - h * .5f, w, h);
        bool wc = Wcis(PAkcja);
        BraxGui.Zaokraglony(new Rect(akcjaG.x - h * .06f, akcjaG.y + h * .04f, akcjaG.width + h * .12f, akcjaG.height + h * .08f), h * .56f, new Color(0f, 0f, 0f, .2f));
        BraxGui.Pigulka(akcjaG, wc ? new Color(BraxGui.Akcent.r, BraxGui.Akcent.g, BraxGui.Akcent.b, .75f) : new Color(.1f, .2f, .05f, .55f));
        BraxGui.ObrysZaokraglony(akcjaG, h * .5f, new Color(BraxGui.Akcent.r, BraxGui.Akcent.g, BraxGui.Akcent.b, wc ? 1f : .75f));
        float ik = h * .56f;
        BraxGui.Tekstura(new Rect(akcjaG.x + h * .3f, akcjaG.center.y - ik * .5f, ik, ik), BraxIkony.Dlon, BraxGui.Biel);
        var old = st.clipping; st.clipping = TextClipping.Clip;
        BraxGui.Napis(new Rect(akcjaG.x + h * .3f + ik + h * .18f, akcjaG.y, akcjaG.width - ik - h * .78f, akcjaG.height), kontekstEkran, st, BraxGui.Biel);
        st.clipping = old;
    }

    // ciemna pigułka z jednym wierszem tekstu, wyśrodkowana w poziomie (albo wokół x)
    void Pasek(string tekst, float y, float alfa, float x = -1f)
    {
        var st = BraxGui.Styl(Mathf.RoundToInt(s * .026f), true, TextAnchor.MiddleCenter);
        float w = BraxGui.Szerokosc(tekst, st) + s * .05f, h = s * .046f;
        float cx = x < 0f ? ukSafe.center.x : x;
        var r = new Rect(cx - w * .5f, y, w, h);
        BraxGui.Pigulka(r, new Color(0f, 0f, 0f, .42f * alfa));
        BraxGui.Napis(r, tekst, st, new Color(1f, 1f, 1f, .95f * alfa), false);
    }

    // komunikat (zapisano zgłoszenie, przeniesiono…): pod górną krawędzią, gaśnie w 0.4 s
    void Toast()
    {
        float t = Time.unscaledTime;
        if (t > komunikatDo || komunikat.Length == 0 || Event.current.type != EventType.Repaint) return;
        float a = Mathf.Clamp01((komunikatDo - t) / .4f) * Mathf.Clamp01((t - komunikatOd) / .15f);
        var st = BraxGui.Styl(Mathf.RoundToInt(s * .028f), true, TextAnchor.MiddleCenter, true);
        float w = Mathf.Min(ukSafe.width * .86f, s * 1.6f), h = BraxGui.Wysokosc(komunikat, st, w - s * .06f) + s * .04f;
        var r = new Rect(ukSafe.center.x - w * .5f, ukSafe.y + s * .12f, w, h);
        BraxGui.Zaokraglony(r, s * .022f, new Color(.06f, .09f, .05f, .9f * a));
        BraxGui.ObrysZaokraglony(r, s * .022f, new Color(BraxGui.Akcent.r, BraxGui.Akcent.g, BraxGui.Akcent.b, .8f * a));
        BraxGui.Napis(new Rect(r.x + s * .03f, r.y, r.width - s * .06f, r.height), komunikat, st, new Color(1f, 1f, 1f, a), false);
    }

    void OnApplicationPause(bool pauza)
    {
        if (!pauza || Qa) return;
        BraxUstawienia.Zapisz();
        if (player && joyId >= 0) { joyId = -1; Stop(); }
    }
}
