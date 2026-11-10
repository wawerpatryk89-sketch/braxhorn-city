using UnityEngine;

// Owner 10.11 12:41 (APK 37): "Tu nie ma żadnego hamulca ręcznego, żaden się przycisk nie pokazuje." The city touch
// controls (BraxTouchControls, R1, unchanged) draw and route the fight buttons only on foot, so the JUMP relay of
// BraxChampionsCarCommands never fired in the car. While the player drives the A1 car this draws its own HAMULEC
// button in the city's button style (BraxGui.PrzyciskKolowy, glass circle + white icon) exactly where the big PUNCH
// button sits on foot (bottom right, under the thumb), and holds the handbrake while a finger stays on it.
// The icon is the standard parking-brake pictogram drawn as a signed distance field: (P) inside a ring between
// two arcs - no text, no bitmap font.
[DisallowMultipleComponent]
public sealed class BraxChampionsCarHud : MonoBehaviour
{
    public BraxChampionsCar car;
    static Texture2D icon;
    int finger = -1;
    float flashUntil;

    static float Ring(Vector2 p, float r, float w) => Mathf.Abs(p.magnitude - r) - w;
    static float Segment(Vector2 p, Vector2 a, Vector2 b, float r)
    {
        Vector2 pa = p - a, ba = b - a;
        float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
        return (pa - ba * h).magnitude - r;
    }
    // ring + letter P + two outer arcs (|angle| < 50 deg from the horizontal axis)
    static float Sdf(Vector2 p)
    {
        float d = Ring(p, .50f, .055f);
        d = Mathf.Min(d, Segment(p, new Vector2(-.13f, -.26f), new Vector2(-.13f, .25f), .06f));          // P stem
        var bowl = p - new Vector2(-.02f, .115f);                                                            // P bowl
        float bowlRing = Mathf.Abs(new Vector2(Mathf.Max(bowl.x, 0f) * 1.0f, bowl.y).magnitude - .135f) - .055f;
        if (bowl.x < 0f) bowlRing = Mathf.Min(Segment(p, new Vector2(-.13f, .25f), new Vector2(-.02f, .25f), .055f),
                                              Segment(p, new Vector2(-.13f, -.02f), new Vector2(-.02f, -.02f), .055f));
        d = Mathf.Min(d, bowlRing);
        float a = Mathf.Abs(Mathf.Atan2(p.y, Mathf.Abs(p.x))) * Mathf.Rad2Deg;
        if (a < 50f) d = Mathf.Min(d, Ring(p, .80f, .055f));                                                  // ( )
        return d;
    }
    static Texture2D Icon()
    {
        if (icon) return icon;
        const int n = 128;
        icon = new Texture2D(n, n, TextureFormat.RGBA32, true) { name = "ikona hamulec ręczny", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear };
        var px = new Color32[n * n];
        float texel = 2f / n;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                var p = new Vector2((x + .5f) / n * 2f - 1f, (y + .5f) / n * 2f - 1f) / .92f;
                byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(.5f - Sdf(p) / texel) * 255f);
                px[y * n + x] = new Color32(255, 255, 255, a);
            }
        icon.SetPixels32(px); icon.Apply(true, true);
        return icon;
    }

    // Same arithmetic as BraxTouchControls.Uklad for the PUNCH button (screen pixels, y up).
    bool Layout(out Vector2 centre, out float radius)
    {
        var safe = BraxGui.BezpiecznyProstokat();
        int w = Screen.width, h = Screen.height;
        var sa = new Rect(safe.x, h - safe.yMax, safe.width, safe.height);
        float s = Mathf.Min(w, h), k = BraxUstawienia.SkalaPrzyciskow, m = s * .045f;
        radius = s * .074f * k;
        centre = new Vector2(sa.xMax - m - radius, sa.yMin + m + radius);
        return s > 0f;
    }

    bool Active => car && car.Driver && !car.Transitioning && !BraxChampionsCarCommands.Paused;

    void Update()
    {
        if (!Active) { finger = -1; return; }
        if (!Layout(out var c, out var r)) return;
        bool held = false;
        for (int i = 0; i < Input.touchCount; i++)
        {
            var t = Input.GetTouch(i);
            if (t.phase == TouchPhase.Began && finger < 0 && (t.position - c).sqrMagnitude <= r * r * 1.35f * 1.35f) finger = t.fingerId;
            if (t.fingerId != finger) continue;
            if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) { finger = -1; continue; }
            held = true;
        }
        if (Input.touchCount == 0) finger = -1;
#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButton(0) && ((Vector2)Input.mousePosition - c).sqrMagnitude <= r * r) held = true;
#endif
        if (held) { car.HoldHandbrake(.15f); flashUntil = Time.unscaledTime + .12f; }
    }

    void OnGUI()
    {
        if (!Active || Event.current.type != EventType.Repaint) return;
        if (!Layout(out var c, out var r)) return;
        GUI.depth = 1;
        bool pressed = finger >= 0 || Time.unscaledTime < flashUntil || car.HandbrakeOn;
        BraxGui.PrzyciskKolowy(new Vector2(c.x, Screen.height - c.y), r, Icon(), pressed, BraxGui.Trafienie);
    }
}
