using UnityEngine;

// BRAXHORN CITY — sterowanie dotykowe w aplikacji Android (bez warstwy HTML).
// Ten sam protokół co strona WWW: MoveInput "x,y", LookInput "dx,dy", Command (run/punch/kick/jump/action/pause/reset).
// Lewa połowa ekranu: joystick (gdzie dotkniesz, tam jest środek). Prawa połowa: obrót kamery.
// Przyciski IMGUI po prawej: BIEG (przełącznik), CIOS, KOP, SKOK; nad nimi podpowiedź kontekstu (np. OTWÓRZ DRZWI · NOVA)
// jako duży przycisk akcji. Pauza w prawym górnym rogu (WRÓĆ / OD NOWA).
public sealed class BraxTouchControls : MonoBehaviour
{
    const float LookScale = .55f;          // piksele telefonu -> jednostki LookInput jak w przeglądarce
    BraxGymPlayer player;
    BraxCityWorld world;
    int joyId = -1, lookId = -1;
    Vector2 joyOrigin, lookLast;
    float radius;
    bool running, paused;
    GUIStyle button, hint, small;
    Rect rRun, rPunch, rKick, rJump, rAction, rPause, rResume, rReset;
    float nextFind;

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
        Layout();
    }

    void Layout()
    {
        float w = Screen.width, h = Screen.height, s = Mathf.Min(w, h);
        radius = s * .13f;
        float b = s * .17f, gap = s * .03f, m = s * .04f;
        rPunch = new Rect(w - m - b, h - m - b, b, b);
        rKick = new Rect(w - m - b * 2 - gap, h - m - b, b, b);
        rJump = new Rect(w - m - b, h - m - b * 2 - gap, b, b);
        rRun = new Rect(w - m - b * 2 - gap, h - m - b * 2 - gap, b, b);
        rAction = new Rect(w * .5f - s * .35f, h - m - b * .7f, s * .7f, b * .6f);
        rPause = new Rect(w - m - b * .55f, m, b * .55f, b * .4f);
        rResume = new Rect(w * .5f - s * .3f, h * .5f - b * .5f, s * .6f, b * .6f);
        rReset = new Rect(w * .5f - s * .3f, h * .5f + b * .3f, s * .6f, b * .6f);
    }

    void Find()
    {
        if (player && world) return;
        if (Time.unscaledTime < nextFind) return;
        nextFind = Time.unscaledTime + .5f;
        player = FindAnyObjectByType<BraxGymPlayer>();
        world = FindAnyObjectByType<BraxCityWorld>();
    }

    bool OnButton(Vector2 p)
    {
        var g = new Vector2(p.x, Screen.height - p.y);   // GUI ma Y w dół
        return rRun.Contains(g) || rPunch.Contains(g) || rKick.Contains(g) || rJump.Contains(g) || rAction.Contains(g) || rPause.Contains(g);
    }

    void Update()
    {
        Find();
        if (!player) return;
        if (paused) { joyId = lookId = -1; return; }
        for (int i = 0; i < Input.touchCount; i++)
        {
            var t = Input.GetTouch(i);
            if (t.phase == TouchPhase.Began)
            {
                if (OnButton(t.position)) continue;
                if (t.position.x < Screen.width * .45f && joyId < 0) { joyId = t.fingerId; joyOrigin = t.position; }
                else if (t.position.x >= Screen.width * .45f && lookId < 0) { lookId = t.fingerId; lookLast = t.position; }
            }
            else if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
            {
                if (t.fingerId == joyId)
                {
                    var d = (t.position - joyOrigin) / radius;
                    d = Vector2.ClampMagnitude(d, 1f);
                    player.MoveInput(d.x.ToString("F4", System.Globalization.CultureInfo.InvariantCulture) + "," + d.y.ToString("F4", System.Globalization.CultureInfo.InvariantCulture));
                }
                else if (t.fingerId == lookId && t.phase == TouchPhase.Moved)
                {
                    var d = (t.position - lookLast) * LookScale;
                    lookLast = t.position;
                    player.LookInput(d.x.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + "," + (-d.y).ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
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

    void Styles()
    {
        if (button != null) return;
        float s = Mathf.Min(Screen.width, Screen.height);
        button = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(s * .045f), fontStyle = FontStyle.Bold };
        button.normal.textColor = Color.white;
        hint = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(s * .04f), fontStyle = FontStyle.Bold, wordWrap = true };
        hint.normal.textColor = new Color(.85f, 1f, .7f);
        small = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(s * .03f) };
        small.normal.textColor = new Color(1f, 1f, 1f, .8f);
    }

    void OnGUI()
    {
        if (!player) return;
        Styles();
        if (Event.current.type == EventType.Layout) Layout();
        GUI.color = new Color(1, 1, 1, .85f);
        if (paused)
        {
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "");
            GUI.Label(new Rect(0, Screen.height * .25f, Screen.width, Screen.height * .1f), "BRAXHORN CITY · PAUZA", button);
            if (GUI.Button(rResume, "WRÓĆ DO GRY", button)) { paused = false; player.Command("pause"); }
            if (GUI.Button(rReset, "OD NOWA (SIŁOWNIA)", button)) { paused = false; player.Command("pause"); player.Command("reset"); }
            return;
        }
        if (GUI.Button(rPause, "MENU", button)) { paused = true; player.MoveInput("0,0"); player.Command("pause"); }
        string context = world ? world.Context : "";
        if (!string.IsNullOrEmpty(context) && GUI.Button(rAction, context, hint)) player.Command("action");
        GUI.color = running ? new Color(.6f, 1f, .5f, .95f) : new Color(1, 1, 1, .85f);
        if (GUI.Button(rRun, running ? "BIEG ✓" : "BIEG", button)) { running = !running; player.Command("run"); }
        GUI.color = new Color(1, 1, 1, .85f);
        if (GUI.Button(rPunch, "CIOS", button)) player.Command("punch");
        if (GUI.Button(rKick, "KOP", button)) player.Command("kick");
        if (GUI.Button(rJump, "SKOK", button)) player.Command("jump");
        if (joyId >= 0)
        {
            float r = radius;
            var c = new Vector2(joyOrigin.x, Screen.height - joyOrigin.y);
            GUI.Box(new Rect(c.x - r, c.y - r, r * 2, r * 2), "");
        }
        GUI.Label(new Rect(Screen.width * .02f, Screen.height * .02f, Screen.width * .5f, Screen.height * .06f), "lewa strona: ruch · prawa: kamera", small);
    }
}
