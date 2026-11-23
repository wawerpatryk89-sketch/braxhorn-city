using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// A casino game played from its seat (owner 23.11 17:35: "przy maszynach w kasynie ... siadasz na krzesło i od razu pokazuje
// Ci się sama maszyna i kręcisz maszyną. I normalnie wygrana, przegrana, wszystko ... Tak samo jak siadasz przy pokerze, tak
// samo widać tylko stół, karty"). One implementation per game component (the machines, the card tables, the wheels).
public interface IBraxStanowisko
{
    /// <summary>The top bar: the casino and the game.</summary>
    string Tytul(int i);
    /// <summary>Where the camera looks from while the player plays (world); asked every frame, it may move (the wheel, the ball).</summary>
    void Kadr(int i, Camera kamera, out Vector3 oko, out Quaternion obrot);
    /// <summary>The game's own controls in the bottom panel and over the picture (IMGUI, BraxChampionsStanowisko.Ui).</summary>
    void Panel(int i, BraxChampionsStanowisko.Ui ui);
    /// <summary>The player has stood up (a bet not yet played comes back; a hand already dealt plays out by the rules).</summary>
    void Wstal(int i);
}

// THE PLAYER'S PLACE AT A GAME. The way the big games do it (GTA Online's casino, the casino apps):
//  - a machine's stool and a card table's chair are seats (BraxSeat, AKCJA "USIĄDŹ · <gra>"); sitting down at one opens its
//    game: the camera flies (about a second, eased) to the machine's face or over the player's spot at the table, the
//    player's body leaves the picture (his shadow stays), the walking controls rest and the game's buttons take the fingers;
//  - the standing games (the roulette, sic bo, the money wheel) open the same way from AKCJA at their rail;
//  - WSTAŃ (or the phone's back button) stands up: the camera flies back behind the player and the walking controls return.
// The casino's seats stand only for the floor the player is on: a seat answers within 28 m and a storey (a stool of the sky
// casino is never offered to a player in the lobby under it), and its height is taken from where it really stands (the
// generator built the casinos at the origin and then moved and scaled them: the height written then is not the seat's).
// Runs after the player's camera (DefaultExecutionOrder), so its view is the one drawn.
[DefaultExecutionOrder(5000)]
public sealed class BraxChampionsStanowisko : MonoBehaviour
{
    sealed class Wpis { public BraxSeat seat; public IBraxStanowisko gra; public int i; }
    static BraxChampionsStanowisko inst;
    readonly List<Wpis> wpisy = new List<Wpis>();
    readonly Dictionary<BraxSeat, Wpis> mapa = new Dictionary<BraxSeat, Wpis>();
    IBraxStanowisko gra; int indeks; BraxSeat seat; Vector3 stal; bool aktywne, wraca, symulacja; float wracaOd, nastSkan, od;
    Camera kam; Vector3 kPos; Quaternion kRot; bool kOk;
    BraxGymPlayer gracz; BraxTouchControls tc;
    Renderer[] ukryte; ShadowCastingMode[] tryby;
    readonly Ui ui = new Ui();

    public const float Zasieg = 28f, PietroNad = 2.6f, PietroPod = 1.5f;
    public static bool Aktywne => inst && inst.aktywne;
    /// <summary>Is the player playing this game (this machine, this table) now?</summary>
    public static bool Gra(IBraxStanowisko g, int i) => Aktywne && ReferenceEquals(inst.gra, g) && inst.indeks == i;
    /// <summary>Seconds since the player sat down (the games wait for the camera before they deal).</summary>
    public static float OdWejscia => Aktywne ? Time.unscaledTime - inst.od : 0f;

    static BraxChampionsStanowisko Inst()
    {
        if (inst) return inst;
        var g = new GameObject("KASYNO: stanowisko gracza (siedzi i gra)");
        inst = g.AddComponent<BraxChampionsStanowisko>(); return inst;
    }

    /// <summary>A game's seat: sitting on it opens the game i. Its height is set from where it stands.</summary>
    public static void Rejestruj(IBraxStanowisko g, BraxSeat s, int i)
    {
        if (!s) return;
        var k = Inst(); s.surfaceY = s.transform.position.y;
        if (k.mapa.TryGetValue(s, out var w)) { w.gra = g; w.i = i; return; }
        w = new Wpis { seat = s, gra = g, i = i }; k.wpisy.Add(w); k.mapa[s] = w;
    }
    /// <summary>The casino's other seats (the bar's stools, the salon's sofas): their height from where they stand, offered
    /// only on their floor like the games'.</summary>
    public static void Siedziska(Transform root)
    {
        if (!root) return;
        foreach (var s in root.GetComponentsInChildren<BraxSeat>(true))
            if (!inst || !inst.mapa.ContainsKey(s)) Rejestruj(null, s, -1);
    }
    /// <summary>A standing game (the roulette, sic bo, the wheel): the player plays where he stands.</summary>
    public static void Wejdz(IBraxStanowisko g, int i)
    {
        var k = Inst(); if (k.aktywne || g == null) return;
        if (!k.gracz) k.gracz = BraxChampionsGry.Gracz;
        if (k.gracz) k.Zacznij(g, i, null);
    }
    /// <summary>WSTAŃ: the player stands up and walks on.</summary>
    public static void Wstan() { if (inst) inst.Koniec(true); }

    void Update()
    {
        if (!gracz) gracz = BraxChampionsGry.Gracz;
        if (!gracz) return;
        if (Time.unscaledTime >= nastSkan) { nastSkan = Time.unscaledTime + .3f; Pietro(); }
        if (!aktywne)
        {
            var s = gracz.seat;
            if (s && mapa.TryGetValue(s, out var w) && Zywa(w.gra)) Zacznij(w.gra, w.i, s);
            return;
        }
        if (!Zywa(gra)) { Koniec(false); return; }
        // stood up some other way (the keyboard, the game's reset), or walked away from a standing game
        if (seat ? gracz.seat != seat : (gracz.transform.position - stal).sqrMagnitude > 1f) { Koniec(false); return; }
        if (Input.GetKeyDown(KeyCode.Escape)) Koniec(true);
    }
    static bool Zywa(IBraxStanowisko g) => g != null && !(g is UnityEngine.Object o && !o);

    // the seats answer only on the player's floor and near him
    void Pietro()
    {
        var p = gracz.transform.position; bool zmiana = false;
        for (int k = wpisy.Count - 1; k >= 0; k--)
        {
            var w = wpisy[k];
            if (!w.seat) { wpisy.RemoveAt(k); zmiana = true; continue; }
            if (w.seat == gracz.seat) continue;
            var d = w.seat.transform.position - p;
            bool on = d.y < PietroNad && d.y > -PietroPod && d.x * d.x + d.z * d.z < Zasieg * Zasieg;
            if (w.seat.gameObject.activeSelf != on) w.seat.gameObject.SetActive(on);
        }
        if (zmiana) { mapa.Clear(); foreach (var w in wpisy) mapa[w.seat] = w; }
    }

    void Zacznij(IBraxStanowisko g, int i, BraxSeat s)
    {
        gra = g; indeks = i; seat = s; aktywne = true; wraca = false; od = Time.unscaledTime;
        stal = gracz.transform.position;
        kam = gracz.view ? gracz.view : Camera.main;
        if (kam) { kPos = kam.transform.position; kRot = kam.transform.rotation; kOk = true; }
        // the player leaves the picture (his shadow stays)
        var v = gracz.visual ? gracz.visual : gracz.transform;
        ukryte = v.GetComponentsInChildren<Renderer>(true); tryby = new ShadowCastingMode[ukryte.Length];
        for (int k = 0; k < ukryte.Length; k++) { tryby[k] = ukryte[k].shadowCastingMode; ukryte[k].shadowCastingMode = ShadowCastingMode.ShadowsOnly; }
        // the walking controls rest (their joystick, AKCJA and menu would sit under the game's buttons); IMGUI takes the fingers
        gracz.MoveInput("0,0");
        tc = FindAnyObjectByType<BraxTouchControls>();
        if (tc && tc.enabled) tc.enabled = false; else tc = null;
        symulacja = Input.simulateMouseWithTouches; Input.simulateMouseWithTouches = true;
        ui.Nowa();                                                       // the finger that pressed AKCJA lifts on the game's buttons
        Debug.Log("KASYNO_STANOWISKO siada " + g.Tytul(i) + (s ? " · siedzisko " + s.label : " · na stojąco"));
    }

    void Koniec(bool wstan)
    {
        if (!aktywne) return;
        var g = gra; int i = indeks; aktywne = false; gra = null;
        if (wstan && seat && gracz && gracz.seat == seat) seat.Leave();
        seat = null;
        if (ukryte != null) for (int k = 0; k < ukryte.Length; k++) if (ukryte[k]) ukryte[k].shadowCastingMode = tryby[k];
        ukryte = null;
        // the touch controls read fingers themselves (no mouse simulation) whenever they run
        if (tc) { tc.enabled = true; Input.simulateMouseWithTouches = false; } else Input.simulateMouseWithTouches = symulacja;
        tc = null;
        wraca = kOk; wracaOd = Time.unscaledTime;
        if (Zywa(g)) { try { g.Wstal(i); } catch (Exception e) { Debug.LogException(e); } }
        Debug.Log("KASYNO_STANOWISKO wstaje");
    }

    void OnDisable() { Koniec(false); }

    // the camera: eased to the game's view (5.5 a second: there in about a second) and back behind the player after
    void LateUpdate()
    {
        if (!kam) return;
        float dt = Mathf.Min(Time.unscaledDeltaTime, .1f);
        if (aktywne && Zywa(gra))
        {
            ui.Uklad(kam);
            gra.Kadr(indeks, kam, out var p, out var r);
            if (!kOk) { kPos = p; kRot = r; kOk = true; }
            float a = 1f - Mathf.Exp(-dt * 5.5f);
            kPos = Vector3.Lerp(kPos, p, a); kRot = Quaternion.Slerp(kRot, r, a);
            kam.transform.SetPositionAndRotation(kPos, kRot);
            return;
        }
        if (!wraca) return;
        // the player's own camera has been placed this frame: ease from the game's view to it
        var cp = kam.transform.position; var cr = kam.transform.rotation;
        float b = 1f - Mathf.Exp(-dt * 6.5f);
        kPos = Vector3.Lerp(kPos, cp, b); kRot = Quaternion.Slerp(kRot, cr, b);
        if (((kPos - cp).sqrMagnitude < 1e-4f && Quaternion.Angle(kRot, cr) < .5f) || Time.unscaledTime - wracaOd > 1.6f) { wraca = false; kOk = false; return; }
        kam.transform.SetPositionAndRotation(kPos, kRot);
    }

    void OnGUI()
    {
        if (!aktywne || !Zywa(gra)) return;
        GUI.depth = -20;
        ui.Zacznij(kam);
        ui.Rama(gra.Tytul(indeks));
        if (ui.Przycisk(ui.wstan, "WSTAŃ", Ui.Szklo, Ui.Krem, true, false, Ui.Zloto)) { Koniec(true); if (Event.current.isMouse) Event.current.Use(); return; }
        try { gra.Panel(indeks, ui); } catch (Exception e) { Debug.LogException(e); }
        ui.Blysk();
        if (Event.current.isMouse) Event.current.Use();                   // every tap belongs to the game
    }

    /// <summary>Puts the camera so that a flat thing (its centre c, its face's normal n toward the player, the up of its picture)
    /// w x h metres fills the screen between the top bar and the bottom panel.</summary>
    public static void Kadruj(Camera cam, Vector3 c, Vector3 n, Vector3 gora, float w, float h, out Vector3 oko, out Quaternion obrot)
    {
        n = n.sqrMagnitude > 1e-8f ? n.normalized : Vector3.forward;
        var u = gora - n * Vector3.Dot(gora, n); u = u.sqrMagnitude > 1e-8f ? u.normalized : Vector3.up;
        float tv = Mathf.Tan((cam ? cam.fieldOfView : 55f) * .5f * Mathf.Deg2Rad);
        float asp = Screen.height > 0 ? (float)Screen.width / Screen.height : 16f / 9f;
        float g = inst ? inst.ui.pasGora : .12f, d0 = inst ? inst.ui.pasDol : .76f, f = Mathf.Max(.3f, d0 - g);
        float d = Mathf.Max(h / (2f * f * tv), w / (2f * .94f * asp * tv));
        float hv = 2f * d * tv, m = (g + d0) * .5f;
        var L = c - u * ((.5f - m) * hv);
        oko = L + n * d; obrot = Quaternion.LookRotation(-n, u);
    }

    /// <summary>A table's stakes: its minimum and 2, 5, 10 times it (the chips a player puts on its felt).</summary>
    public static int[] Stawki(int min)
    {
        min = Mathf.Max(1, min);
        if (!stawki.TryGetValue(min, out var k)) stawki[min] = k = new[] { min, 2 * min, 5 * min, 10 * min };
        return k;
    }
    static readonly Dictionary<int, int[]> stawki = new Dictionary<int, int[]>();

    /// <summary>A card table seen from its player's chair: the felt between his spot and the dealer's cards (the table's frame:
    /// origin under the dealer's edge, +z to the players), looked at from above the chair, the dealer at the top of the screen.
    /// szer x glab metres of the felt fill the picture (glab along the line from the spot to the dealer).</summary>
    public static void KadrStolu(Camera cam, Transform stol, Vector3 miejsce, Vector3 krupier, float szer, float glab, out Vector3 oko, out Quaternion obrot,
        float pochylenie = 50f, float kuKrupierowi = .45f)
    {
        var o = new Vector3(miejsce.x, 0f, miejsce.z); o = o.sqrMagnitude > 1e-4f ? o.normalized : Vector3.forward;
        float a = pochylenie * Mathf.Deg2Rad, k = Mathf.Abs(stol.lossyScale.x);
        var n = Vector3.up * Mathf.Cos(a) + o * Mathf.Sin(a);
        var c = Vector3.Lerp(miejsce, krupier, kuKrupierowi);
        Kadruj(cam, stol.TransformPoint(c), stol.TransformDirection(n), stol.TransformDirection(-o), szer * k, glab * Mathf.Cos(a) * k, out oko, out obrot);
    }
    /// <summary>A flat stretch of felt seen from the players' side (+z of the table): its centre, its size, the view's tilt.</summary>
    public static void KadrBlatu(Camera cam, Transform stol, Vector3 srodek, float szer, float glab, float pochylenie, out Vector3 oko, out Quaternion obrot)
    {
        float a = pochylenie * Mathf.Deg2Rad, k = Mathf.Abs(stol.lossyScale.x);
        var n = Vector3.up * Mathf.Cos(a) + Vector3.forward * Mathf.Sin(a);
        Kadruj(cam, stol.TransformPoint(srodek), stol.TransformDirection(n), stol.TransformDirection(Vector3.back), szer * k, glab * Mathf.Cos(a) * k, out oko, out obrot);
    }

    // ================================================================ the screen's look (casino: black glass, gold, cream)
    public sealed class Ui
    {
        public static readonly Color Zloto = new Color(.95f, .77f, .33f), ZlotoCiemne = new Color(.50f, .36f, .10f), Szklo = new Color(.03f, .03f, .045f, .93f),
            Krem = new Color(.99f, .97f, .91f), Czerwien = new Color(.74f, .07f, .10f), Zielen = new Color(.08f, .50f, .28f), Szary = new Color(.24f, .25f, .29f, .95f),
            Granat = new Color(.06f, .10f, .26f), Przygasly = new Color(1f, 1f, 1f, .55f);
        public Rect ekran, gora, dol, wstan, kredyt;
        public float s, m, pasGora = .12f, pasDol = .76f;
        public Camera kam;
        bool trzyma; Vector2 dotyk; float od;
        // a new game: a tap counts only when it was also pressed here (the finger that pressed USIĄDŹ / GRAJ lifts over the
        // ZAKRĘĆ button - it must not spin) and not in the first moment
        public void Nowa() { trzyma = false; od = Time.unscaledTime; }
        bool Gotowy => trzyma && Time.unscaledTime - od > .35f;

        // the frame's rectangles (from the screen alone: the camera asks before OnGUI runs)
        public void Uklad(Camera k)
        {
            kam = k;
            var sa = Screen.safeArea; if (sa.width < 10f || sa.height < 10f) sa = new Rect(0, 0, Screen.width, Screen.height);
            ekran = new Rect(sa.x, Screen.height - sa.yMax, sa.width, sa.height);
            s = Mathf.Max(200f, Mathf.Min(Screen.width, Screen.height)); m = s * .024f;
            gora = new Rect(ekran.x + m, ekran.y + m * .6f, ekran.width - 2f * m, s * .078f);
            wstan = new Rect(gora.x, gora.y, s * .24f, gora.height);
            kredyt = new Rect(gora.xMax - s * .40f, gora.y, s * .40f, gora.height);
            float hd = s * .20f;
            dol = new Rect(ekran.x + m, ekran.yMax - hd - m * .5f, ekran.width - 2f * m, hd);
            float H = Mathf.Max(1f, Screen.height);
            pasGora = (gora.yMax + m) / H; pasDol = (dol.y - m * .6f) / H;
        }
        public void Zacznij(Camera k)
        {
            Uklad(k);
            var e = Event.current;
            if (e.type == EventType.MouseDown && Time.unscaledTime - od > .2f) { trzyma = true; dotyk = e.mousePosition; }
            else if (e.type == EventType.MouseUp) { dotyk = e.mousePosition; }
        }
        bool Wcisniety(Rect r) => trzyma && r.Contains(dotyk) && Event.current.type == EventType.Repaint && (Input.GetMouseButton(0) || Input.touchCount > 0);
        /// <summary>A tap released inside r.</summary>
        public bool Klik(Rect r)
        {
            var e = Event.current;
            if (e.type != EventType.MouseUp || !Gotowy || !r.Contains(e.mousePosition)) return false;
            trzyma = false; return true;
        }

        // the top bar: WSTAŃ (drawn by the caller as a button), the game's name, the credit
        public void Rama(string tytul)
        {
            // a soft dark band behind the top bar: its words read over a bright topper or the felt
            BraxGui.Prostokat(new Rect(0f, 0f, Screen.width, gora.yMax + m * .5f), new Color(0f, 0f, 0f, .30f));
            BraxGui.Zaokraglony(new Rect(kredyt.x, kredyt.y + s * .004f, kredyt.width, kredyt.height), kredyt.height * .5f, new Color(0, 0, 0, .25f));
            BraxGui.Pigulka(kredyt, Szklo); BraxGui.ObrysZaokraglony(kredyt, kredyt.height * .5f, new Color(Zloto.r, Zloto.g, Zloto.b, .7f));
            var mal = BraxGui.Styl(Mathf.RoundToInt(s * .022f), true, TextAnchor.MiddleLeft);
            var duz = BraxGui.Styl(Mathf.RoundToInt(s * .040f), true, TextAnchor.MiddleRight);
            BraxGui.Napis(new Rect(kredyt.x + kredyt.height * .5f, kredyt.y, kredyt.width * .4f, kredyt.height), "KREDYT", mal, Przygasly, false);
            BraxGui.Napis(new Rect(kredyt.x, kredyt.y, kredyt.width - kredyt.height * .5f, kredyt.height), BraxChampionsAutomaty.Kwota(BraxChampionsKasyno.Kredyt), duz, Zloto, false);
            var t = new Rect(wstan.xMax + m, gora.y, kredyt.x - wstan.xMax - 2f * m, gora.height);
            BraxGui.Napis(t, tytul, BraxGui.Styl(Mathf.RoundToInt(s * .028f), true, TextAnchor.MiddleCenter), Krem);
            // the bottom panel: black glass, a gold line on its top
            BraxGui.Zaokraglony(new Rect(dol.x, dol.y + s * .006f, dol.width, dol.height), s * .03f, new Color(0, 0, 0, .30f));
            BraxGui.Zaokraglony(dol, s * .03f, Szklo);
            BraxGui.ObrysZaokraglony(dol, s * .03f, new Color(Zloto.r, Zloto.g, Zloto.b, .55f));
        }

        /// <summary>A button: rounded, its colour, pressed darker; a gold rim on the main ones. True when tapped.</summary>
        public bool Przycisk(Rect r, string t, Color tlo, Color tekst, bool aktywny = true, bool glowny = false, Color? obrys = null)
        {
            float pr = Mathf.Min(r.height, r.width) * .28f;
            var c = aktywny ? (Wcisniety(r) ? new Color(tlo.r * .7f, tlo.g * .7f, tlo.b * .7f, tlo.a) : tlo) : new Color(tlo.r * .45f + .1f, tlo.g * .45f + .1f, tlo.b * .45f + .1f, tlo.a * .8f);
            BraxGui.Zaokraglony(new Rect(r.x, r.y + s * .005f, r.width, r.height), pr, new Color(0, 0, 0, .28f));
            BraxGui.Zaokraglony(r, pr, c);
            if (glowny && aktywny) BraxGui.ObrysZaokraglony(r, pr, Zloto);
            else if (obrys.HasValue) BraxGui.ObrysZaokraglony(r, pr, new Color(obrys.Value.r, obrys.Value.g, obrys.Value.b, aktywny ? .8f : .35f));
            int fs = Mathf.RoundToInt(Mathf.Min(r.height * .36f, s * (glowny ? .042f : .030f)));
            var st = BraxGui.Styl(fs, true, TextAnchor.MiddleCenter, true);
            BraxGui.Napis(new Rect(r.x + r.width * .04f, r.y, r.width * .92f, r.height), t, st, aktywny ? tekst : new Color(tekst.r, tekst.g, tekst.b, .45f));
            return aktywny && Klik(r);
        }
        /// <summary>A display: its small caption over its big value.</summary>
        public void Pole(Rect r, string podpis, string wartosc, Color kolor)
        {
            BraxGui.Zaokraglony(r, r.height * .16f, new Color(0f, 0f, 0f, .45f));
            BraxGui.ObrysZaokraglony(r, r.height * .16f, new Color(1f, 1f, 1f, .12f));
            BraxGui.Napis(new Rect(r.x, r.y + r.height * .06f, r.width, r.height * .34f), podpis, Mieszczacy(podpis, r.height * .19f, r.width * .92f), Przygasly, false);
            BraxGui.Napis(new Rect(r.x, r.y + r.height * .36f, r.width, r.height * .58f), wartosc, Mieszczacy(wartosc, Mathf.Min(r.height * .40f, s * .05f), r.width * .92f), kolor, false);
        }
        // a bold centred style whose text fits the width (a long hand's name is set smaller, never over the next button)
        static GUIStyle Mieszczacy(string t, float rozmiar, float szer)
        {
            var st = BraxGui.Styl(Mathf.RoundToInt(rozmiar), true, TextAnchor.MiddleCenter);
            float w = BraxGui.Szerokosc(t, st);
            return w > szer && w > 1f ? BraxGui.Styl(Mathf.Max(8, Mathf.FloorToInt(rozmiar * szer / w)), true, TextAnchor.MiddleCenter) : st;
        }
        /// <summary>The stake: − value + (only between the games). Returns the new index.</summary>
        public int Stawka(Rect r, int[] kroki, int idx, bool mozna, string podpis = "STAWKA")
        {
            idx = Mathf.Clamp(idx, 0, kroki.Length - 1);
            float b = r.height * .78f;
            var minus = new Rect(r.x, r.y + (r.height - b) * .5f, b, b); var plus = new Rect(r.xMax - b, minus.y, b, b);
            Pole(new Rect(minus.xMax + m * .4f, r.y, plus.x - minus.xMax - m * .8f, r.height), podpis, BraxChampionsAutomaty.Kwota(kroki[idx]), Zloto);
            if (Przycisk(minus, "−", Szary, Krem, mozna && idx > 0)) idx--;
            if (Przycisk(plus, "+", Szary, Krem, mozna && idx < kroki.Length - 1)) idx++;
            return idx;
        }
        /// <summary>A text in the panel or over the picture.</summary>
        public void Tekst(Rect r, string t, float rozmiar, Color c, TextAnchor a = TextAnchor.MiddleCenter)
            => BraxGui.Napis(r, t, BraxGui.Styl(Mathf.RoundToInt(s * rozmiar), true, a, true), c);

        // ---- the picture: what the player touches on the machine's screen, the felt, the wheel
        /// <summary>A world point on the screen (IMGUI, y down); false behind the camera.</summary>
        public bool Rzut(Vector3 w, out Vector2 g)
        {
            g = Vector2.zero; if (!kam) return false;
            var p = kam.WorldToScreenPoint(w); if (p.z <= .01f) return false;
            g = new Vector2(p.x, Screen.height - p.y); return true;
        }
        /// <summary>A tap inside a quad of the world (its corners in turn), never through the panels.</summary>
        public bool Dotyk(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            var e = Event.current; if (e.type != EventType.MouseUp || !Gotowy) return false;
            var p = e.mousePosition; if (dol.Contains(p) || gora.Contains(p)) return false;
            if (!Rzut(a, out var A) || !Rzut(b, out var B) || !Rzut(c, out var C) || !Rzut(d, out var D)) return false;
            bool w = W(p, A, B, C) || W(p, A, C, D);
            if (w) trzyma = false;
            return w;
        }
        static bool W(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y), d2 = (p.x - c.x) * (b.y - c.y) - (b.x - c.x) * (p.y - c.y), d3 = (p.x - a.x) * (c.y - a.y) - (c.x - a.x) * (p.y - a.y);
            bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0;
            return !(neg && pos);
        }
        /// <summary>A quad of the world outlined on the screen (a held card, the winning number).</summary>
        public void Obrys(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color kol, float grub)
        {
            if (Event.current.type != EventType.Repaint) return;
            if (!Rzut(a, out var A) || !Rzut(b, out var B) || !Rzut(c, out var C) || !Rzut(d, out var D)) return;
            Linia(A, B, kol, grub); Linia(B, C, kol, grub); Linia(C, D, kol, grub); Linia(D, A, kol, grub);
        }
        public static void Linia(Vector2 a, Vector2 b, Color kol, float grub)
        {
            var o = GUI.matrix; var d = b - a; float len = d.magnitude; if (len < .5f) return;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, a);
            BraxGui.Prostokat(new Rect(a.x, a.y - grub * .5f, len, grub), kol);
            GUI.matrix = o;
        }
        /// <summary>A small label pinned to a world point (a chip's amount, a total).</summary>
        public void Etykieta(Vector3 w, string t, Color tlo, Color tekst, float rozmiar = .024f)
        {
            if (!Rzut(w, out var p) || dol.Contains(p)) return;
            var st = BraxGui.Styl(Mathf.RoundToInt(s * rozmiar), true, TextAnchor.MiddleCenter);
            float sw = BraxGui.Szerokosc(t, st) + s * .03f, sh = s * rozmiar * 1.7f;
            var r = new Rect(p.x - sw * .5f, p.y - sh * .5f, sw, sh);
            BraxGui.Pigulka(r, tlo); BraxGui.Napis(r, t, st, tekst, false);
        }

        // the flash of the games (a win, BLACKJACK!, RIEN NE VA PLUS) over the bottom panel, gold on black glass
        public void Blysk()
        {
            if (Time.unscaledTime >= BraxChampionsGry.BlyskDo || string.IsNullOrEmpty(BraxChampionsGry.Blysk) || Event.current.type != EventType.Repaint) return;
            float zostalo = BraxChampionsGry.BlyskDo - Time.unscaledTime, a = Mathf.Clamp01(zostalo / .35f);
            var st = BraxGui.Styl(Mathf.RoundToInt(s * .046f), true, TextAnchor.MiddleCenter);
            float w = Mathf.Min(ekran.width - 2f * m, BraxGui.Szerokosc(BraxChampionsGry.Blysk, st) + s * .12f), h = s * .095f;
            var r = new Rect(ekran.center.x - w * .5f, gora.yMax + m * .7f, w, h);      // under the top bar: never over the reels or the felt's bets
            BraxGui.Pigulka(r, new Color(0f, 0f, 0f, .72f * a));
            BraxGui.ObrysZaokraglony(r, h * .5f, new Color(Zloto.r, Zloto.g, Zloto.b, a));
            BraxGui.Napis(r, BraxChampionsGry.Blysk, st, new Color(Zloto.r, Zloto.g, Zloto.b, a));
        }
    }
}
