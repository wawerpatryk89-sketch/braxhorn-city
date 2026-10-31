using UnityEngine;

// BRAXHORN CITY — sterowanie dotykowe w aplikacji Android (bez warstwy HTML). Przebudowa 31.10 (właściciel 18:35:
// „nie ma joysticka, bieg/skok/kop/cios chujowo, nie mogę wyjść z siłowni, zamula”).
// Ten sam protokół co strona WWW: MoveInput "x,y", LookInput "dx,dy", Command (punch/kick/jump/action/pause/reset).
//  • Joystick (lewa część ekranu): okrągła podstawa i gałka; w spoczynku półprzezroczysty w lewym dolnym rogu,
//    po dotknięciu przeskakuje pod kciuk. Wychylenie do krawędzi = bieg (gracz biegnie od 76% wychylenia).
//  • Kamera: przeciąganie palcem po prawej części ekranu (poza przyciskami).
//  • Przyciski okrągłe po prawej: CIOS (duży), KOP, SKOK, BIEG (blokada biegu: każde wychylenie = pełny bieg).
//    Obsługa z Input.touches (każdy palec osobno), NIE z IMGUI — IMGUI widzi tylko pierwszy palec, więc stary
//    układ nie reagował na przycisk, gdy drugi palec trzymał joystick.
//  • AKCJA: duży przycisk nad prawymi przyciskami z napisem z gry (OTWÓRZ DRZWI, WSIĄDŹ · MOTORÓWKA, USIĄDŹ · SOFA,
//    WEJDŹ NA OSIEDLE…) — pojawia się tylko, gdy jest co zrobić (BraxGymPlayer.Context: drzwi siłowni też).
//  • Na łodzi / skuterze / jachcie: joystick = gaz i ster, zamiast ciosów jest ZEJDŹ. Siedząc / leżąc: WSTAŃ.
//  • MENU: wróć do gry, tryb grafiki NISKA / ŚREDNIA / WYSOKA (BraxGrafika), licznik FPS, od nowa.
public sealed class BraxTouchControls : MonoBehaviour
{
    const float LookScale = .55f;
    BraxGymPlayer player;
    int joyId = -1, lookId = -1;
    Vector2 joyOrigin, joyNow, lookLast;      // współrzędne ekranu (Y w górę)
    float R, B, b, m, gap;
    bool runLock, paused, showFps;
    float fpsT, fpsShown; int fpsN;
    float nextFind, hintUntil;
    Texture2D disc, ring;
    GUIStyle lab, labBig, menuBtn, menuTitle, small;
    // przyciski: środek w ekranie (Y w górę) i promień
    Vector2 cPunch, cKick, cJump, cRun, cMenu; float rMenu;
    Rect rAction;                             // GUI (Y w dół)
    float flashPunch, flashKick, flashJump, flashAction;
    readonly int[] btnFinger = { -1, -1, -1, -1, -1, -1 };

#if UNITY_ANDROID && !UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (FindAnyObjectByType<BraxTouchControls>()) return;
        var g = new GameObject("BRAXHORN sterowanie dotykowe (Android)");
        DontDestroyOnLoad(g);
        g.AddComponent<BraxTouchControls>();
    }
#endif

    void Awake()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Input.multiTouchEnabled = true;
        Input.simulateMouseWithTouches = false;   // przyciski gry liczone z dotyku; IMGUI tylko rysuje (i menu)
        disc = Circle(128, false); ring = Circle(128, true);
        hintUntil = Time.unscaledTime + 10f;
    }

    static Texture2D Circle(int n, bool hollow)
    {
        var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var px = new Color32[n * n]; float c = (n - 1) * .5f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;    // 0 środek, 1 krawędź
                float a = Mathf.Clamp01((1f - d) * c * .5f);                          // wygładzona krawędź ~2 px
                if (hollow) a *= Mathf.Clamp01((d - .86f) * c * .5f);                // pierścień 14% promienia
                px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
        t.SetPixels32(px); t.Apply(false, true); return t;
    }

    void Layout()
    {
        float w = Screen.width, h = Screen.height, s = Mathf.Min(w, h);
        m = s * .05f; gap = s * .03f; R = s * .16f; B = s * .22f; b = s * .165f;
        cPunch = new Vector2(w - m - B * .5f, m + B * .5f);
        cKick = new Vector2(cPunch.x - B * .5f - gap - b * .5f, m + b * .5f);
        cJump = new Vector2(w - m - b * .5f, cPunch.y + B * .5f + gap + b * .5f);
        cRun = new Vector2(cKick.x - b * .15f, cJump.y + b * .1f);
        float aw = s * .62f, ah = b * .62f;
        rAction = new Rect(w - m - aw, h - (cJump.y + b * .5f + gap * 1.3f) - ah, aw, ah);
        rMenu = s * .065f; cMenu = new Vector2(w - m - rMenu, h - m * .6f - rMenu);
    }

    void Find()
    {
        if (player || Time.unscaledTime < nextFind) return;
        nextFind = Time.unscaledTime + .5f;
        player = FindAnyObjectByType<BraxGymPlayer>();
    }

    bool InCircle(Vector2 p, Vector2 c, float r) => (p - c).sqrMagnitude <= r * r;
    bool InRect(Vector2 p, Rect r) => r.Contains(new Vector2(p.x, Screen.height - p.y));
    bool OnFoot => player && !player.vessel && !player.seat;
    string Context => player ? player.Context : "";

    void Update()
    {
        Find();
        Layout();
        fpsN++; fpsT += Time.unscaledDeltaTime; if (fpsT >= .5f) { fpsShown = fpsN / fpsT; fpsN = 0; fpsT = 0; }
        if (!player) return;
        if (paused) { joyId = lookId = -1; return; }
        for (int i = 0; i < Input.touchCount; i++)
        {
            var t = Input.GetTouch(i); var p = t.position;
            if (t.phase == TouchPhase.Began)
            {
                if (InCircle(p, cMenu, rMenu * 1.4f)) { Pause(true); return; }
                if (!string.IsNullOrEmpty(Context) && InRect(p, rAction)) { player.Command("action"); flashAction = Time.unscaledTime + .18f; continue; }
                if (OnFoot)
                {
                    if (InCircle(p, cPunch, B * .55f)) { player.Command("punch"); flashPunch = Time.unscaledTime + .15f; continue; }
                    if (InCircle(p, cKick, b * .58f)) { player.Command("kick"); flashKick = Time.unscaledTime + .15f; continue; }
                    if (InCircle(p, cJump, b * .58f)) { player.Command("jump"); flashJump = Time.unscaledTime + .15f; continue; }
                    if (InCircle(p, cRun, b * .58f)) { runLock = !runLock; continue; }
                }
                if (p.x < Screen.width * .45f && joyId < 0) { joyId = t.fingerId; joyOrigin = p; joyNow = p; }
                else if (p.x >= Screen.width * .45f && lookId < 0) { lookId = t.fingerId; lookLast = p; }
            }
            else if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
            {
                if (t.fingerId == joyId) { joyNow = p; SendMove(); }
                else if (t.fingerId == lookId && t.phase == TouchPhase.Moved)
                {
                    var d = (p - lookLast) * LookScale; lookLast = p;
                    player.LookInput(F(d.x) + "," + F(-d.y));
                }
            }
            else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            {
                if (t.fingerId == joyId) { joyId = -1; player.MoveInput("0,0"); }
                else if (t.fingerId == lookId) lookId = -1;
            }
        }
        if (Input.touchCount == 0 && joyId >= 0) { joyId = -1; player.MoveInput("0,0"); }
    }

    void SendMove()
    {
        var d = Vector2.ClampMagnitude((joyNow - joyOrigin) / R, 1f);
        if (d.magnitude < .08f) d = Vector2.zero;
        else if (runLock && OnFoot) d = d.normalized;          // blokada biegu: każde wychylenie = pełny bieg
        player.MoveInput(F(d.x) + "," + F(d.y));
    }

    static string F(float v) => v.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);

    void Pause(bool on)
    {
        if (paused == on || !player) return;
        paused = on; joyId = lookId = -1; player.MoveInput("0,0"); player.Command("pause");
    }

    void Styles()
    {
        if (lab != null) return;
        float s = Mathf.Min(Screen.width, Screen.height);
        lab = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = Mathf.RoundToInt(s * .036f) };
        lab.normal.textColor = Color.white;
        labBig = new GUIStyle(lab) { fontSize = Mathf.RoundToInt(s * .042f), wordWrap = true };
        small = new GUIStyle(lab) { fontSize = Mathf.RoundToInt(s * .028f), fontStyle = FontStyle.Normal };
        menuBtn = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(s * .042f), fontStyle = FontStyle.Bold };
        menuBtn.normal.textColor = Color.white; menuBtn.hover.textColor = Color.white; menuBtn.active.textColor = new Color(.7f, 1f, .4f);
        menuTitle = new GUIStyle(lab) { fontSize = Mathf.RoundToInt(s * .06f) };
    }

    // kolory: biel z cieniem, akcent zielony BRAXHORN (jak na stronie), czerwień ciosu
    static readonly Color Ink = new Color(1f, 1f, 1f, .9f), Glass = new Color(.06f, .07f, .07f, .45f),
        Accent = new Color(.55f, 1f, .2f, .95f), Hot = new Color(1f, .35f, .25f, .95f);

    void DrawDisc(Vector2 c, float r, Color fill, Color edge, string text, GUIStyle st)
    {
        var rect = new Rect(c.x - r, Screen.height - c.y - r, r * 2, r * 2);
        var old = GUI.color;
        GUI.color = fill; GUI.DrawTexture(rect, disc);
        GUI.color = edge; GUI.DrawTexture(rect, ring);
        GUI.color = Ink; if (!string.IsNullOrEmpty(text)) GUI.Label(rect, text, st);
        GUI.color = old;
    }

    void OnGUI()
    {
        if (!player) return;
        Styles(); Layout();
        float now = Time.unscaledTime, s = Mathf.Min(Screen.width, Screen.height);
        if (paused) { Menu(s); return; }
        // MENU (prawy górny róg)
        DrawDisc(cMenu, rMenu, Glass, Ink, "MENU", small);
        // joystick: w spoczynku w lewym dolnym rogu, przy dotyku pod kciukiem
        Vector2 jc = joyId >= 0 ? joyOrigin : new Vector2(m + R * 1.25f, m + R * 1.25f);
        Vector2 knob = joyId >= 0 ? jc + Vector2.ClampMagnitude(joyNow - joyOrigin, R) : jc;
        float alpha = joyId >= 0 ? 1f : .55f;
        DrawDisc(jc, R, new Color(.06f, .07f, .07f, .35f * alpha), new Color(1, 1, 1, .7f * alpha), "", lab);
        DrawDisc(knob, R * .42f, new Color(1, 1, 1, .55f * alpha), new Color(1, 1, 1, .9f * alpha), "", lab);
        if (player.vessel) GUI.Label(new Rect(jc.x - R, Screen.height - jc.y - R * 1.55f, R * 2, R * .4f), "GAZ: w górę · STER: w bok", small);
        // prawa strona
        if (OnFoot)
        {
            DrawDisc(cPunch, B * .5f, now < flashPunch ? Hot : Glass, Ink, "CIOS", labBig);
            DrawDisc(cKick, b * .5f, now < flashKick ? Hot : Glass, Ink, "KOP", lab);
            DrawDisc(cJump, b * .5f, now < flashJump ? Accent : Glass, Ink, "SKOK", lab);
            DrawDisc(cRun, b * .5f, runLock ? new Color(.25f, .55f, .1f, .8f) : Glass, runLock ? Accent : Ink, runLock ? "BIEG: WŁ" : "BIEG", lab);
        }
        string ctx = Context;
        if (!string.IsNullOrEmpty(ctx))
        {
            var old = GUI.color;
            GUI.color = now < flashAction ? new Color(.4f, .8f, .15f, .95f) : new Color(.12f, .3f, .05f, .82f);
            GUI.DrawTexture(new Rect(rAction.x - rAction.height * .5f, rAction.y, rAction.height, rAction.height), disc);
            GUI.DrawTexture(new Rect(rAction.xMax - rAction.height * .5f, rAction.y, rAction.height, rAction.height), disc);
            GUI.DrawTexture(new Rect(rAction.x, rAction.y, rAction.width, rAction.height), Texture2D.whiteTexture);
            GUI.color = Ink; GUI.Label(rAction, ctx, labBig); GUI.color = old;
        }
        if (now < hintUntil) GUI.Label(new Rect(0, s * .03f, Screen.width, s * .06f), "lewy kciuk: ruch (do krawędzi = bieg)  ·  prawy: kamera", small);
        if (showFps) GUI.Label(new Rect(m * .5f, m * .4f, s * .4f, s * .06f), Mathf.RoundToInt(fpsShown) + " FPS · " + BraxGrafika.Nazwy[(int)BraxGrafika.Aktualny], small);
    }

    void Menu(float s)
    {
        Input.simulateMouseWithTouches = true;   // w menu IMGUI ma reagować na dotyk
        var old = GUI.color;
        GUI.color = new Color(0, 0, 0, .72f); GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture); GUI.color = old;
        float w = s * 1.1f, x = (Screen.width - w) * .5f, y = Screen.height * .1f, h = s * .12f, g = s * .03f;
        GUI.Label(new Rect(0, y, Screen.width, h), "BRAXHORN CITY · PAUZA", menuTitle); y += h + g;
        if (GUI.Button(new Rect(x, y, w, h), "WRÓĆ DO GRY", menuBtn)) { Input.simulateMouseWithTouches = false; Pause(false); return; }
        y += h + g;
        GUI.Label(new Rect(x, y, w, h * .6f), "GRAFIKA", lab); y += h * .6f;
        float bw = (w - 2 * g) / 3f;
        for (int i = 0; i < 3; i++)
        {
            bool on = (int)BraxGrafika.Aktualny == i;
            GUI.color = on ? Accent : Color.white;
            if (GUI.Button(new Rect(x + i * (bw + g), y, bw, h), on ? "[ " + BraxGrafika.Nazwy[i] + " ]" : BraxGrafika.Nazwy[i], menuBtn)) BraxGrafika.Ustaw((BraxGrafika.Tryb)i);
            GUI.color = old;
        }
        y += h + g * .5f;
        GUI.Label(new Rect(x, y, w, h * .5f), "NISKA: słabszy telefon · ŚREDNIA: telefon 30 kl./s · WYSOKA: komputer", small); y += h * .5f + g;
        if (GUI.Button(new Rect(x, y, (w - g) * .5f, h), showFps ? "FPS: WŁ." : "FPS: WYŁ.", menuBtn)) showFps = !showFps;
        if (GUI.Button(new Rect(x + (w + g) * .5f, y, (w - g) * .5f, h), "OD NOWA (SIŁOWNIA)", menuBtn)) { Input.simulateMouseWithTouches = false; Pause(false); player.Command("reset"); }
    }
}
